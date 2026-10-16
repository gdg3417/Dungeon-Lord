using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public readonly struct DungeonReplayActorState
    {
        public readonly int Frame, Health;
        public readonly Vector2 Position;
        internal DungeonReplayActorState(int frame, int health, Vector2 position)
        { Frame = frame; Health = health; Position = position; }
    }

    public sealed class DungeonReplayActor
    {
        public string Id { get; }
        public string ContentId { get; }
        public string AssignmentId { get; }
        public string FloorId { get; }
        public string RoomId { get; }
        public int Ordinal { get; }
        public int MaxHealth { get; }
        public bool IsMonster => AssignmentId != null;
        private readonly DungeonReplayActorState[] states;
        internal DungeonReplayActor(string id, string content, string assignment, string floor, string room,
            int ordinal, int maximum, DungeonReplayActorState[] states)
        { Id = id; ContentId = content; AssignmentId = assignment; FloorId = floor; RoomId = room;
          Ordinal = ordinal; MaxHealth = maximum; this.states = states; }
        // Bounded binary lookup. No retained mutable ledger, source party, tuning or resolver.
        public DungeonReplayActorState At(int frame)
        {
            int lo = 0, hi = states.Length;
            while (lo + 1 < hi) { int mid = lo + (hi - lo) / 2; if (states[mid].Frame <= frame) lo = mid; else hi = mid; }
            return states[lo];
        }
    }

    public sealed class DungeonCombatReplay
    {
        public IReadOnlyList<DungeonReplayActor> Actors { get; }
        public IReadOnlyList<CombatC0Event> Events { get; }
        private readonly Dictionary<string, int> actorIndices;
        private readonly Dictionary<string, CombatC0Event[]> denials;
        private readonly Dictionary<string, int> encounters;
        internal DungeonCombatReplay(DungeonReplayActor[] actors, CombatC0Event[] events,
            Dictionary<string, CombatC0Event[]> denials)
        { Actors = Array.AsReadOnly(actors); Events = Array.AsReadOnly(events); this.denials = denials;
          encounters = denials.Keys.Select((id,i)=>(id,i)).ToDictionary(e=>e.id,e=>e.i+1,StringComparer.Ordinal);
          actorIndices = actors.Select((a,i) => (a.Id,i)).ToDictionary(a => a.Id, a => a.i, StringComparer.Ordinal); }
        public int ActorIndex(string id) => id != null && actorIndices.TryGetValue(id, out int i) ? i : -1;
        public int EncounterNumber(string id) => id != null && encounters.TryGetValue(id,out int number) ? number : 0;
        public IReadOnlyList<CombatC0Event> Denials(string encounter) => encounter != null && denials.TryGetValue(encounter, out var values)
            ? Array.AsReadOnly(values) : Array.Empty<CombatC0Event>();
    }

    public sealed partial class DungeonRunReplay
    {
        private sealed class ActorCapture
        {
            internal string Id, Content, Assignment, Floor, Room;
            internal int Ordinal, Maximum, Health, Final;
            internal Vector2 Position;
            internal readonly List<DungeonReplayActorState> States = new List<DungeonReplayActorState>();
            internal bool Monster => Assignment != null;
            internal void Record(int frame) => States.Add(new DungeonReplayActorState(frame, Health, Position));
            internal DungeonReplayActor Freeze() => new DungeonReplayActor(Id, Content, Assignment, Floor, Room,
                Ordinal, Maximum, States.ToArray());
        }

        private static bool TryCreateCombat(RunOutcomeRecord outcome, DetachedCanonicalSpatialSaveState state,
            SpatialContentCatalog catalog, string fingerprint, string corridorFingerprint, DungeonPresentationPolicy policy,
            out DungeonRunReplay replay, out string reason)
        {
            replay = null; reason = Missing;
            var steps = outcome.PresentationSteps;
            if (steps == null || steps.Length == 0 || outcome.Party == null || outcome.CombatMonsters == null || outcome.CombatEvents == null) return false;
            reason = Layout;
            if (string.IsNullOrEmpty(outcome.PresentationLayoutFingerprint) || outcome.PresentationLayoutFingerprint != fingerprint ||
                string.IsNullOrEmpty(outcome.PresentationCorridorFingerprint) || outcome.PresentationCorridorFingerprint != corridorFingerprint) return false;
            reason = Capacity;
            var events = outcome.CombatEvents;
            if (policy == null || steps.Length > policy.MaximumReplaySteps || events.Length > policy.MaximumReplayCombatEvents ||
                (long)outcome.Party.Members.Count + outcome.CombatMonsters.Length > policy.MaximumReplayCombatActors ||
                outcome.CombatMonsters.Length > policy.MaximumReplayMarkers ||
                (outcome.SpatialEvents?.Length ?? 0) > (long)policy.MaximumReplaySteps + policy.MaximumReplayMarkers) return false;
            reason = Invalid;
            // Corrupt transient data fails closed. Nothing below has any gameplay reference capable of a write.
            try { return BuildCombat(outcome, state, catalog, policy, out replay, ref reason); }
            catch (ArgumentException) { return false; }
            catch (InvalidOperationException) { return false; }
            catch (OverflowException) { return false; }
            catch (NullReferenceException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
        }

        private static bool BuildCombat(RunOutcomeRecord outcome, DetachedCanonicalSpatialSaveState state,
            SpatialContentCatalog catalog, DungeonPresentationPolicy policy, out DungeonRunReplay replay, ref string reason)
        {
            replay = null;
            bool Positive(float x) => float.IsFinite(x) && x > 0;
            if (state?.Floors == null || catalog == null || outcome.Party.RunId != outcome.RunId || outcome.Party.Members.Count == 0 ||
                !Positive(policy.ReplayMovementSeconds) || !Positive(policy.ReplayInteractionSeconds) || !Positive(policy.ReplayCombatActionSeconds) ||
                !Positive(policy.ReplayDamageSeconds) || !Positive(policy.ReplayAttackWidth) ||
                policy.MaximumReplayCombatActors <= 0 || policy.MaximumReplayCombatEvents <= 0 || policy.MaximumReplayMarkers <= 0 ||
                policy.MaximumReplayCombatStateChanges <= 0) return false;
            var floors = state.Floors.ToDictionary(f => f.FloorIndex);
            var floorIds = state.Floors.ToDictionary(f => f.FloorInstanceId, StringComparer.Ordinal);
            var floorBounds = state.Floors.ToDictionary(f => f.FloorIndex, f => catalog.Floors.Single(d =>
                d.FloorDefinitionId == f.FloorDefinitionId && d.FloorIndex == f.FloorIndex).Bounds);
            var actors = new List<ActorCapture>();
            foreach (var m in outcome.Party.Members.OrderBy(m => m.MemberOrdinal))
            {
                if (m.MemberOrdinal < 0 || m.MaxHealth <= 0 || m.CurrentHealth < 0 || m.CurrentHealth > m.MaxHealth || string.IsNullOrEmpty(m.ClassId)) return false;
                actors.Add(new ActorCapture { Id = outcome.RunId + "/member/" + m.MemberOrdinal.ToString(CultureInfo.InvariantCulture),
                    Content = m.ClassId, Ordinal = m.MemberOrdinal, Maximum = m.MaxHealth, Health = m.MaxHealth, Final = m.CurrentHealth });
            }
            int monsterOrdinal = 0;
            foreach (var m in outcome.CombatMonsters.OrderBy(m => m.AssignmentId, StringComparer.Ordinal))
            {
                if (m == null || string.IsNullOrEmpty(m.AssignmentId) || !floorIds.TryGetValue(m.FloorId, out var floor) ||
                    m.MaxHealth <= 0 || m.Health < 0 || m.Health > m.MaxHealth) return false;
                var assignment = floor.RoomContents.Assignments.SingleOrDefault(a => a.AssignmentId == m.AssignmentId);
                if (assignment == null || assignment.RoomInstanceId != m.RoomId || assignment.OptionId != m.OptionId ||
                    assignment.CategoryId != CanonicalSpatialSaveContracts.MonsterCategoryId ||
                    !TryProject(assignment.RoomLocalPosition, RunPositionSpace.RoomLocal, m.RoomId, floor, catalog, out var position)) return false;
                actors.Add(new ActorCapture { Id = outcome.RunId + "/monster/" + m.AssignmentId, Content = m.OptionId, Assignment = m.AssignmentId,
                    Floor = m.FloorId, Room = m.RoomId, Ordinal = monsterOrdinal++, Maximum = m.MaxHealth, Health = m.MaxHealth, Final = m.Health, Position = position });
            }
            var actorById = actors.Select((a,i) => (a.Id,i)).ToDictionary(a => a.Id, a => a.i, StringComparer.Ordinal);
            foreach (var a in actors) a.Record(-1);
            var steps = outcome.PresentationSteps;
            var events = (CombatC0Event[])outcome.CombatEvents.Clone();
            var frames = new List<DungeonReplayFrame>();
            var spatial = new Dictionary<(int,string,string),RunSpatialEvent>();
            foreach (var e in outcome.SpatialEvents ?? Array.Empty<RunSpatialEvent>())
                if (e == null) return false;
                else if (e.AssignmentId != null && !spatial.TryAdd((e.FloorIndex,e.RoomInstanceId,e.AssignmentId),e)) return false;
            var traps = new HashSet<RunEncounterEvent>();
            var corridorOptions = new Dictionary<(int,string),string>();
            var corridorPositions = new Dictionary<(int,string),TileCoordinate>();
            var corridor = new List<DungeonReplayCorridorContent>();
            foreach (var branch in outcome.BranchOutcomes ?? Array.Empty<BranchOutcomeEvidence>())
                if (branch.Traversed && branch.Fork != null)
                    foreach (var a in branch.Fork.Assignments)
                    {
                        if (!floors.TryGetValue(branch.Fork.FloorIndex, out var floor) || floor.FloorInstanceId != a.FloorInstanceId ||
                            !corridorOptions.TryAdd((branch.Fork.FloorIndex,a.AssignmentId),a.OptionId)) return false;
                        if (corridor.Count >= policy.MaximumReplayMarkers) { reason=Capacity; return false; }
                        corridorPositions.Add((branch.Fork.FloorIndex,a.AssignmentId),a.Tile);
                        corridor.Add(new DungeonReplayCorridorContent(a));
                    }
            if ((long)corridor.Count + spatial.Count > policy.MaximumReplayMarkers)
            { reason = Capacity; return false; }
            int cursor = 0, encounterOrdinal = 0, changes = actors.Count;
            string lastFloor = null; Vector2 lastPosition = default; bool lastLocated = false, wiped = false;
            var denials = new Dictionary<string,CombatC0Event[]>(StringComparer.Ordinal);
            var seenSpatial = new HashSet<(int,string,string)>();
            for (int s = 0; s < steps.Length; s++)
            {
                var step = steps[s];
                if (step.Ordinal != s || step.RunId != outcome.RunId || step.IsAnnouncement || !Enum.IsDefined(typeof(RunStepKind),step.Kind) ||
                    (s == steps.Length - 1) != (step.Kind == RunStepKind.Terminal)) return false;
                if (wiped && step.Kind != RunStepKind.Terminal && step.Kind != RunStepKind.RoomComplete) return false;
                floors.TryGetValue(step.FloorIndex, out var floor);
                string floorId = floor?.FloorInstanceId;
                bool located = step.PositionSpace != RunPositionSpace.None;
                Vector2 position = default;
                if (step.PositionSpace == RunPositionSpace.Floor && (floor == null ||
                    floorBounds[step.FloorIndex]?.Contains(step.Position) != true)) return false;
                if (located && (floor == null || !TryProject(step.Position,step.PositionSpace,step.RoomId,floor,catalog,out position))) return false;
                if (!located && step.Kind != RunStepKind.FloorTransfer) { floorId = lastFloor; position = lastPosition; located = lastLocated; }
                var attached = step.CombatEvents;
                int count = attached?.Count ?? 0;
                if (cursor < events.Length && events[cursor].RunOrdinal < s) return false;
                if (count > 0)
                {
                    if (!located || step.PositionSpace != RunPositionSpace.RoomLocal || step.Encounter != null ||
                        (step.Kind != RunStepKind.MonsterEngagement && step.Kind != RunStepKind.UnreachableInteraction) || count > events.Length - cursor) return false;
                    int start = cursor, end = cursor + count;
                    bool bypass = step.Kind == RunStepKind.UnreachableInteraction;
                    string encounter = bypass ? null : outcome.RunId + "/combat/" + encounterOrdinal.ToString(CultureInfo.InvariantCulture);
                    if (bypass && count != 1) return false;
                    if (!bypass && (events[start].Kind != CombatC0EventKind.EncounterStart || events[end-1].Kind != CombatC0EventKind.EncounterComplete)) return false;
                    var links = new Dictionary<(int,int),CombatC0Reason>();
                    var positions = new Dictionary<int,TileCoordinate>();
                    var denied = new List<CombatC0Event>();
                    var acted = new HashSet<(int,int)>();
                    var participating = new HashSet<int>();
                    int action = 0, round = 0; bool actionsStarted = false;
                    for (int i = start; i < end; i++)
                    {
                        var e = events[i]; var original = attached[i-start];
                        if (!SameCombatEvent(e,original) || e.Ordinal != i || e.RunOrdinal != s || e.RunId != outcome.RunId ||
                            e.ExecutableId != CombatC0Config.Executable || e.EncounterId != encounter || e.FloorId != floorId || e.RoomId != step.RoomId ||
                            !Enum.IsDefined(typeof(CombatC0EventKind),e.Kind) || !Enum.IsDefined(typeof(CombatC0Reason),e.Reason)) return false;
                        int actor = e.ActorId != null && actorById.TryGetValue(e.ActorId,out int ai) ? ai : -1;
                        int target = e.TargetId != null && actorById.TryGetValue(e.TargetId,out int ti) ? ti : -1;
                        if (e.ActorId != null && actor < 0 || e.TargetId != null && target < 0) return false;
                        bool pair = e.Kind == CombatC0EventKind.Eligibility || e.Kind == CombatC0EventKind.Attack || e.Kind == CombatC0EventKind.Bypass;
                        if ((pair || e.Kind == CombatC0EventKind.Skipped) && (actor < 0 || string.IsNullOrEmpty(e.AttackId) || string.IsNullOrEmpty(e.TargetingId))) return false;
                        if (pair && (target < 0 || actors[actor].Monster == actors[target].Monster || !e.SourcePosition.HasValue || !e.TargetPosition.HasValue)) return false;
                        if (e.Kind == CombatC0EventKind.Skipped && (target != -1 || e.TargetPosition.HasValue || !e.SourcePosition.HasValue)) return false;
                        if (!pair && e.Kind != CombatC0EventKind.Skipped && (actor != -1 || target != -1 || e.SourcePosition.HasValue || e.TargetPosition.HasValue)) return false;
                        if (actor >= 0 && !CapturePosition(actor,e.SourcePosition)) return false;
                        if (target >= 0 && !CapturePosition(target,e.TargetPosition)) return false;
                        if (e.Kind == CombatC0EventKind.Eligibility)
                        {
                            if (bypass || actionsStarted || e.HealthBefore != 0 || e.HealthAfter != 0 || e.Round != 0 || e.ActionOrdinal != -1 || !PositionReason(e.Reason) ||
                                actors[actors[actor].Monster ? actor : target].Health <= 0 || !links.TryAdd((actor,target),e.Reason)) return false;
                            if (e.Reason != CombatC0Reason.Legal) denied.Add(e);
                            else if (actors[actor].Monster ? actors[target].Health > 0 : actors[actor].Health > 0)
                                participating.Add(actors[actor].Monster ? actor : target);
                            continue;
                        }
                        if (e.Kind == CombatC0EventKind.Attack || e.Kind == CombatC0EventKind.Skipped)
                        {
                            actionsStarted = true;
                            if (bypass || e.ActionOrdinal != action++ || e.Round <= 0 || e.Round < round || e.Round > round+1 || wiped || !acted.Add((e.Round,actor))) return false;
                            round = e.Round;
                            if (e.Kind == CombatC0EventKind.Attack)
                            {
                                if (e.Reason != CombatC0Reason.Legal || !links.TryGetValue((actor,target),out var legal) || legal != CombatC0Reason.Legal ||
                                    actors[actor].Health <= 0 || e.HealthBefore != actors[target].Health || e.HealthBefore <= 0 ||
                                    e.HealthAfter < 0 || e.HealthAfter > e.HealthBefore) return false;
                                actors[target].Health = e.HealthAfter; actors[target].Record(frames.Count); changes++;
                            }
                            else if (e.Reason == CombatC0Reason.DefeatedBeforeAction ? actors[actor].Health != 0 :
                                e.Reason != CombatC0Reason.NoLivingLegalTarget || actors[actor].Health <= 0 ||
                                links.Any(l => l.Key.Item1 == actor && l.Value == CombatC0Reason.Legal && actors[l.Key.Item2].Health > 0)) return false;
                        }
                        else
                        {
                            if (e.ActionOrdinal != -1 || e.HealthBefore != 0 || e.HealthAfter != 0) return false;
                            if (e.Kind == CombatC0EventKind.EncounterStart && (i != start || e.Round != 0 || e.Reason != CombatC0Reason.Legal)) return false;
                            if (e.Kind == CombatC0EventKind.Bypass && (!bypass || !PositionReason(e.Reason) || e.Reason == CombatC0Reason.Legal ||
                                !actors[actor].Monster || actors[actor].Assignment != step.AssignmentId || e.Round != 0)) return false;
                            if (e.Kind == CombatC0EventKind.EncounterComplete && (i != end-1 || e.Round != round ||
                                (e.Reason != CombatC0Reason.PartyWiped && e.Reason != CombatC0Reason.DefendersDefeated && e.Reason != CombatC0Reason.NoParticipants))) return false;
                        }
                        if (e.Kind != CombatC0EventKind.Attack && (e.HealthBefore != 0 || e.HealthAfter != 0)) return false;
                        int survivors = actors.Count(a => !a.Monster && a.Health > 0);
                        if (e.Kind == CombatC0EventKind.EncounterComplete)
                        {
                            if ((e.Reason == CombatC0Reason.PartyWiped) != (survivors == 0)) return false;
                            if (e.Reason == CombatC0Reason.DefendersDefeated && (participating.Count == 0 || participating.Any(a => actors[a].Health > 0))) return false;
                            if (e.Reason == CombatC0Reason.NoParticipants && participating.Count != 0) return false;
                        }
                        frames.Add(new DungeonReplayFrame(step,floorId,position,true,survivors,null,null,position,false,Array.Empty<Vector2>(),e,actor,target));
                        if (survivors == 0) wiped = true;
                    }
                    // Each candidate has the complete directed party matrix. Positions at the encounter
                    // boundary come solely from these immutable records, including ineligible defenders.
                    if (!bypass)
                    {
                        int memberCount = actors.Count(a => !a.Monster), monsterCount = positions.Keys.Count(a => actors[a].Monster);
                        if (links.Count != checked(2*memberCount*monsterCount) || monsterCount == 0) return false;
                        foreach (int a in positions.Keys.Where(a => actors[a].Monster))
                        {
                            if (links.Where(l => l.Key.Item1 == a || l.Key.Item2 == a).Count() != 2*memberCount) return false;
                            if (participating.Contains(a))
                            {
                                var key=(step.FloorIndex,step.RoomId,actors[a].Assignment);
                                if (!spatial.TryGetValue(key,out var reached) || reached.Kind!=RunSpatialEventKind.MonsterEngaged ||
                                    reached.EncounterOrdinal!=encounterOrdinal || reached.FloorInstanceId!=floorId || reached.OptionId!=actors[a].Content ||
                                    !reached.Position.Equals(step.Position) || !seenSpatial.Add(key) || reached.Path==null || reached.Path.Length==0 ||
                                    !reached.Path[reached.Path.Length-1].Equals(positions[a])) return false;
                            }
                        }
                        if (!actorById.TryGetValue(outcome.RunId+"/monster/"+step.AssignmentId,out var first) || !positions.ContainsKey(first)) return false;
                        int boundaryFrame = frames.FindLastIndex(f => f.Combat?.Kind == CombatC0EventKind.EncounterStart);
                        foreach (var p in positions)
                        {
                            var a=actors[p.Key];
                            if (!TryProject(p.Value,RunPositionSpace.RoomLocal,step.RoomId,floor,catalog,out var point)) return false;
                            a.Position=point;
                            // Position is a separate captured transition; preserve chronological HP states.
                            for (int h=a.States.Count-1;h>=0 && a.States[h].Frame>=boundaryFrame;h--)
                                a.States[h]=new DungeonReplayActorState(a.States[h].Frame,a.States[h].Health,point);
                            int insert=a.States.FindIndex(v=>v.Frame>=boundaryFrame);
                            a.States.Insert(insert<0?a.States.Count:insert,new DungeonReplayActorState(boundaryFrame,
                                insert<0?a.Health:(insert==0?a.Maximum:a.States[insert-1].Health),point)); changes++;
                        }
                        denials.Add(encounter,denied.ToArray()); encounterOrdinal++;
                    }
                    else
                    {
                        var e=events[start]; var key=(step.FloorIndex,step.RoomId,step.AssignmentId);
                        if (!spatial.TryGetValue(key,out var skipped) || skipped.Kind!=RunSpatialEventKind.MonsterUnreachable ||
                            skipped.FloorInstanceId!=floorId || skipped.OptionId!=actors[actorById[e.ActorId]].Content || !seenSpatial.Add(key)) return false;
                        foreach (var p in positions.Where(p=>actors[p.Key].Monster))
                        {
                            var a=actors[p.Key];
                            if (!TryProject(p.Value,RunPositionSpace.RoomLocal,step.RoomId,floor,catalog,out a.Position)) return false;
                            a.Record(frames.Count-1); changes++;
                        }
                    }
                    cursor=end;

                    bool CapturePosition(int index, TileCoordinate? tile)
                    {
                        if (!tile.HasValue) return false;
                        var a=actors[index];
                        if (a.Monster && (a.Floor!=floorId || a.Room!=step.RoomId) || !a.Monster && !tile.Value.Equals(step.Position)) return false;
                        if (positions.TryGetValue(index,out var previous) && !previous.Equals(tile.Value)) return false;
                        positions[index]=tile.Value; return TryProject(tile.Value,RunPositionSpace.RoomLocal,step.RoomId,floor,catalog,out _);
                    }
                }
                else
                {
                    if (step.Kind == RunStepKind.MonsterEngagement || cursor < events.Length && events[cursor].RunOrdinal == s) return false;
                    if (step.PositionSpace == RunPositionSpace.Floor &&
                        (step.Kind == RunStepKind.TrapTrigger || step.Kind == RunStepKind.LootInteraction) &&
                        (!corridorPositions.TryGetValue((step.FloorIndex,step.AssignmentId),out var recordedTile) ||
                         !recordedTile.Equals(step.Position))) return false;
                    spatial.TryGetValue((step.FloorIndex,step.RoomId,step.AssignmentId ?? string.Empty),out var evidence);
                    if (evidence != null && (!seenSpatial.Add((step.FloorIndex,step.RoomId,step.AssignmentId)) ||
                        evidence.FloorInstanceId!=floorId || !evidence.Position.Equals(step.Position))) return false;
                    var trap=step.Encounter;
                    if (trap!=null)
                    {
                        string id=outcome.RunId+"/member/"+trap.MemberOrdinal.ToString(CultureInfo.InvariantCulture);
                        if (step.Kind!=RunStepKind.TrapTrigger || !actorById.TryGetValue(id,out int target) ||
                            trap.FloorIndex!=step.FloorIndex || trap.AssignmentId!=step.AssignmentId || !traps.Add(trap) ||
                            !(outcome.EncounterEvents??Array.Empty<RunEncounterEvent>()).Contains(trap) || trap.Damage<0 ||
                            trap.HealthBefore<=0 || trap.HealthBefore!=actors[target].Health || trap.HealthAfter<0 || trap.HealthAfter>trap.HealthBefore ||
                            trap.HealthBefore-trap.HealthAfter!=trap.Damage) return false;
                        actors[target].Health=trap.HealthAfter; actors[target].Record(frames.Count); changes++;
                    }
                    if (step.Kind==RunStepKind.TrapTrigger && trap==null) return false;
                    string category=evidence?.CategoryId;
                    if (category==CanonicalSpatialSaveContracts.MonsterCategoryId) return false;
                    string option=evidence?.OptionId ?? trap?.OptionId;
                    if (option==null && corridorOptions.TryGetValue((step.FloorIndex,step.AssignmentId??string.Empty),out var value)) option=value;
                    var assignment=floor?.RoomContents.Assignments.SingleOrDefault(a=>a.AssignmentId==step.AssignmentId);
                    option=option??assignment?.OptionId; category=category??assignment?.CategoryId;
                    if (step.Kind==RunStepKind.TrapTrigger) category=CanonicalSpatialSaveContracts.TrapCategoryId;
                    if (step.Kind==RunStepKind.LootInteraction) category=CanonicalSpatialSaveContracts.LootNodeCategoryId;
                    Vector2 marker=position; bool marked=located&&category!=null;
                    if (step.Kind==RunStepKind.UnreachableInteraction &&
                        (evidence==null || !TryProject(evidence.ConfiguredStart,RunPositionSpace.RoomLocal,step.RoomId,floor,catalog,out marker))) return false;
                    int survivors=actors.Count(a=>!a.Monster&&a.Health>0);
                    frames.Add(new DungeonReplayFrame(step,floorId,position,located,survivors,category,option,marker,marked,Array.Empty<Vector2>()));
                    if (survivors==0) wiped=true;
                }
                lastFloor=floorId; lastPosition=position; lastLocated=located;
                if (changes>policy.MaximumReplayCombatStateChanges) { reason=Capacity; return false; }
            }
            if (cursor!=events.Length || actors.Any(a=>a.Health!=a.Final) || !frames.Any(f=>f.HasPosition) ||
                traps.Count!=(outcome.EncounterEvents??Array.Empty<RunEncounterEvent>()).Length || outcome.SurvivalSummary==null ||
                outcome.SurvivalSummary.PartySize!=actors.Count(a=>!a.Monster) ||
                wiped&&(outcome.Success||outcome.SurvivalSummary.SuccessAtResolution) ||
                actors.Count(a=>!a.Monster&&a.Health>0)!=outcome.SurvivalSummary.SurvivorCount ||
                outcome.SurvivalSummary.DeathCount!=actors.Count(a=>!a.Monster&&a.Health==0)) return false;
            // Every aggregate monster engagement/bypass must belong to the corresponding route boundary.
            if (spatial.Any(p=>!seenSpatial.Contains(p.Key))) return false;
            replay=new DungeonRunReplay(frames.ToArray(),corridor.ToArray(),policy) {
                Combat=new DungeonCombatReplay(actors.Select(a=>a.Freeze()).ToArray(),events,denials) };
            reason=null; return true;
        }

        private static bool PositionReason(CombatC0Reason reason) => reason==CombatC0Reason.Legal || reason==CombatC0Reason.MeleePositionUnavailable ||
            reason==CombatC0Reason.OutOfRange || reason==CombatC0Reason.LineOfSightBlocked || reason==CombatC0Reason.Unreachable || reason==CombatC0Reason.PartyAnchorConflict;
        private static bool SameCombatEvent(CombatC0Event a, CombatC0Event b) => a!=null && b!=null &&
            a.Ordinal==b.Ordinal && a.ActionOrdinal==b.ActionOrdinal && a.Round==b.Round && a.RunId==b.RunId && a.EncounterId==b.EncounterId &&
            a.FloorId==b.FloorId && a.RoomId==b.RoomId && a.Kind==b.Kind && a.Reason==b.Reason && a.ActorId==b.ActorId && a.TargetId==b.TargetId &&
            a.AttackId==b.AttackId && a.TargetingId==b.TargetingId && a.ExecutableId==b.ExecutableId && a.SourcePosition.Equals(b.SourcePosition) &&
            a.TargetPosition.Equals(b.TargetPosition) && a.HealthBefore==b.HealthBefore && a.HealthAfter==b.HealthAfter;
    }
}
