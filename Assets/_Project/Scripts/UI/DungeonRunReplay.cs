using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public readonly struct DungeonReplayCorridorContent
    {
        public readonly string FloorId, AssignmentId, Category, Option;
        public readonly Vector2 Position;
        internal DungeonReplayCorridorContent(CorridorContentAssignment value)
        { FloorId = value.FloorInstanceId; AssignmentId = value.AssignmentId; Category = value.CategoryId;
          Option = value.OptionId; Position = new Vector2(value.Tile.X + .5f, value.Tile.Y + .5f); }
    }
    public readonly struct DungeonReplayFrame
    {
        public readonly RunStep Step;
        public readonly string FloorId;
        public readonly Vector2 Position;
        public readonly bool HasPosition;
        public readonly int Survivors;
        public readonly string Category, Option;
        public readonly Vector2 MarkerPosition;
        public readonly bool HasMarker;
        public readonly IReadOnlyList<Vector2> MonsterPath;
        public readonly CombatC0Event Combat;
        public readonly int ActorIndex, TargetIndex;
        internal DungeonReplayFrame(RunStep step, string floor, Vector2 position, bool hasPosition,
            int survivors, string category, string option, Vector2 marker, bool hasMarker, Vector2[] monsterPath,
            CombatC0Event combat = null, int actor = -1, int target = -1)
        { Step = step; FloorId = floor; Position = position; HasPosition = hasPosition;
          Survivors = survivors; Category = category; Option = option; MarkerPosition = marker; HasMarker = hasMarker;
          MonsterPath = Array.AsReadOnly(monsterPath); Combat = combat; ActorIndex = actor; TargetIndex = target; }
    }

    // A detached display sequence of already committed evidence. No simulation/service/save reference.
    public sealed partial class DungeonRunReplay
    {
        public const string Missing = "ui.dungeon.replay.unavailable";
        public const string NoRun = "ui.dungeon.replay.no_run";
        public const string Historical = "ui.dungeon.replay.historical";
        public const string Invalid = "ui.dungeon.replay.invalid_evidence";
        public const string Capacity = "ui.dungeon.replay.capacity";
        public const string Layout = "ui.dungeon.replay.layout_changed";
        public DungeonCombatReplay Combat { get; private set; }
        private readonly DungeonReplayFrame[] frames;
        private readonly DungeonPresentationPolicy policy;
        private float elapsed;
        public int Index { get; private set; }
        public bool Paused { get; private set; }
        public bool Closed { get; private set; }
        public bool Complete => Index == frames.Length - 1;
        public DungeonReplayFrame Current => frames[Index];
        public int FrameCount => frames.Length;
        public int CurrentFloorIndex
        {
            get { for (int i = Index; i >= 0; i--) if (frames[i].Step.FloorIndex >= 0) return frames[i].Step.FloorIndex; return -1; }
        }
        public IReadOnlyList<DungeonReplayFrame> Frames { get; }
        public IReadOnlyList<int> InteractionIndices { get; }
        public IReadOnlyList<DungeonReplayCorridorContent> CorridorContents { get; }
        public float Elapsed => elapsed;
        public float CurrentDuration => Duration(Current);
        public float ApproachSeconds => Mathf.Max(0, Current.MonsterPath.Count - 1) * policy.ReplayMonsterTileSeconds;
        public bool DamageApplied => Current.Combat?.Kind == CombatC0EventKind.Attack || Current.Step.Encounter != null && elapsed >= ApproachSeconds;
        public bool DamageVisible => !Closed && !Complete && DamageApplied && elapsed < ApproachSeconds + policy.ReplayDamageSeconds;
        public int DisplaySurvivors => Current.Survivors + (!DamageApplied && Current.Step.Encounter?.HealthAfter == 0 ? 1 : 0);
        public Vector2 MonsterPosition
        {
            get
            {
                var path = Current.MonsterPath;
                if (path.Count == 0) return Current.MarkerPosition;
                float progress = elapsed / policy.ReplayMonsterTileSeconds;
                int segment = Mathf.Min((int)progress, path.Count - 1);
                return segment == path.Count - 1 ? path[segment] : Vector2.Lerp(path[segment], path[segment + 1], progress - segment);
            }
        }
        public Vector2 DisplayPosition
        {
            get
            {
                var current = Current;
                if (current.Step.Kind != RunStepKind.Movement || Index == 0) return current.Position;
                var previous = frames[Index - 1];
                // Never interpolate a floor transfer or an unrecorded gap.
                if (!previous.HasPosition || previous.FloorId != current.FloorId ||
                    Vector2.Distance(previous.Position, current.Position) > 1) return current.Position;
                return Vector2.Lerp(previous.Position, current.Position, elapsed / Duration(current));
            }
        }
        private DungeonRunReplay(DungeonReplayFrame[] frames, DungeonReplayCorridorContent[] corridorContents, DungeonPresentationPolicy policy)
        { this.frames = frames; this.policy = policy; Frames = Array.AsReadOnly(frames);
          CorridorContents = Array.AsReadOnly(corridorContents);
          InteractionIndices = Array.AsReadOnly(Enumerable.Range(0, frames.Length).Where(i => frames[i].HasMarker).ToArray()); }
        private float Duration(DungeonReplayFrame frame) => frame.Combat != null ? policy.ReplayCombatActionSeconds : frame.Step.Kind == RunStepKind.Movement ?
            policy.ReplayMovementSeconds : Mathf.Max(0, frame.MonsterPath.Count - 1) * policy.ReplayMonsterTileSeconds +
            (frame.Step.Encounter != null ? Mathf.Max(policy.ReplayInteractionSeconds, policy.ReplayDamageSeconds) : policy.ReplayInteractionSeconds);
        public void TogglePause() { if (!Closed && !Complete) Paused = !Paused; }
        public void Suspend() { if (!Closed && !Complete) Paused = true; }
        public void Close() { Closed = true; }
        public void Next()
        { if (!Closed && !Complete) { Paused = true; elapsed = 0; Index++; } }
        public bool Advance(float seconds)
        {
            if (Closed || Paused || Complete || !float.IsFinite(seconds) || seconds <= 0) return false;
            elapsed += seconds;
            int before = Index;
            // At most one configured sequence's worth of work, including long frame stalls.
            while (!Complete && elapsed >= Duration(Current))
            { elapsed -= Duration(Current); Index++; }
            return before != Index;
        }
        public static bool TryCreate(RunOutcomeRecord outcome, DetachedCanonicalSpatialSaveState state,
            SpatialContentCatalog catalog, string fingerprint, string corridorFingerprint, DungeonPresentationPolicy policy,
            out DungeonRunReplay replay, out string reason)
        {
            replay = null; reason = Missing;
            if (outcome == null) { reason = NoRun; return false; }
            if (CombatC0Diagnostics.IsCombatC0(outcome))
                return TryCreateCombat(outcome, state, catalog, fingerprint, corridorFingerprint, policy, out replay, out reason);
            if (outcome?.CombatExecutable != null || outcome?.CombatEvents?.Length > 0) { reason = Invalid; return false; }
            var steps = outcome?.PresentationSteps;
            if (steps == null || steps.Length == 0 || steps.Length > policy.MaximumReplaySteps ||
                outcome.Party == null || policy.ReplayMovementSeconds <= 0 || policy.ReplayInteractionSeconds <= 0 ||
                !float.IsFinite(policy.ReplayMovementSeconds) || !float.IsFinite(policy.ReplayInteractionSeconds) ||
                policy.ReplayMonsterTileSeconds <= 0 || !float.IsFinite(policy.ReplayMonsterTileSeconds) ||
                policy.ReplayDamageSeconds <= 0 || !float.IsFinite(policy.ReplayDamageSeconds) || policy.MaximumReplayMarkers <= 0 ||
                steps[steps.Length - 1].Kind != RunStepKind.Terminal) return false;
            if (string.IsNullOrEmpty(outcome.PresentationLayoutFingerprint) ||
                outcome.PresentationLayoutFingerprint != fingerprint || string.IsNullOrEmpty(outcome.PresentationCorridorFingerprint) ||
                outcome.PresentationCorridorFingerprint != corridorFingerprint) { reason = Historical; return false; }
            var list = new List<DungeonReplayFrame>(steps.Length);
            var interactions = new Dictionary<(int, string, string), RunSpatialEvent>();
            foreach (var value in outcome.SpatialEvents ?? Array.Empty<RunSpatialEvent>())
                if (value != null && value.AssignmentId != null)
                    if (!interactions.TryAdd((value.FloorIndex, value.RoomInstanceId, value.AssignmentId), value)) return false;
            var deaths = new HashSet<int>();
            var markerIds = new HashSet<string>(StringComparer.Ordinal);
            var corridorOptions = new Dictionary<(int, string), string>();
            var corridorContents = new List<DungeonReplayCorridorContent>();
            foreach (var branch in outcome.BranchOutcomes ?? Array.Empty<BranchOutcomeEvidence>())
                if (branch.Traversed && branch.Fork != null)
                    foreach (var assignment in branch.Fork.Assignments)
                    {
                        if (corridorContents.Count >= policy.MaximumReplayMarkers || assignment.FloorInstanceId != branch.Fork.FloorInstanceId ||
                            !state.Floors.Any(f => f.FloorIndex == branch.Fork.FloorIndex && f.FloorInstanceId == assignment.FloorInstanceId)) return false;
                        corridorContents.Add(new DungeonReplayCorridorContent(assignment));
                        if (!corridorOptions.TryAdd((branch.Fork.FloorIndex, assignment.AssignmentId), assignment.OptionId)) return false;
                    }
            int survivors = outcome.Party.Members.Count;
            int markerCount = 0;
            int pathPoints = 0;
            string lastFloor = null; Vector2 lastPosition = default; bool lastHasPosition = false;
            for (int i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                if (step.Ordinal != i || step.RunId != outcome.RunId) return false;
                if (step.Encounter != null && ((step.Kind != RunStepKind.MonsterEngagement && step.Kind != RunStepKind.TrapTrigger) ||
                    step.Encounter.AssignmentId != step.AssignmentId || step.Encounter.FloorIndex != step.FloorIndex ||
                    step.Encounter.MemberOrdinal < 0 || step.Encounter.MemberOrdinal >= outcome.Party.Members.Count ||
                    step.Encounter.Damage < 0 || step.Encounter.HealthBefore <= 0 || step.Encounter.HealthAfter < 0 ||
                    step.Encounter.HealthAfter > step.Encounter.HealthBefore ||
                    !(outcome.EncounterEvents ?? Array.Empty<RunEncounterEvent>()).Contains(step.Encounter))) return false;
                var floor = state.Floors.SingleOrDefault(f => f.FloorIndex == step.FloorIndex);
                bool located = step.PositionSpace != RunPositionSpace.None;
                Vector2 position = default;
                if (located && (floor == null || !TryProject(step.Position, step.PositionSpace, step.RoomId, floor, catalog, out position)))
                { reason = Historical; return false; }
                string floorId = floor?.FloorInstanceId;
                if (!located && step.Kind != RunStepKind.FloorTransfer)
                { position = lastPosition; floorId = lastFloor; located = lastHasPosition; }
                if (step.Encounter?.HealthAfter == 0 && deaths.Add(step.Encounter.MemberOrdinal)) survivors--;
                string category = step.Kind == RunStepKind.MonsterEngagement ? CanonicalSpatialSaveContracts.MonsterCategoryId :
                    step.Kind == RunStepKind.TrapTrigger ? CanonicalSpatialSaveContracts.TrapCategoryId :
                    step.Kind == RunStepKind.LootInteraction ? CanonicalSpatialSaveContracts.LootNodeCategoryId : null;
                interactions.TryGetValue((step.FloorIndex, step.RoomId, step.AssignmentId), out var spatial);
                if (spatial != null && spatial.FloorInstanceId != floorId) { reason = Historical; return false; }
                category = spatial?.CategoryId ?? category;
                Vector2 marker = position; bool hasMarker = category != null && located;
                if (step.Kind == RunStepKind.UnreachableInteraction)
                {
                    hasMarker = spatial != null && floor != null && TryProject(spatial.ConfiguredStart,
                        RunPositionSpace.RoomLocal, step.RoomId, floor, catalog, out marker);
                    if (!hasMarker) return false;
                }
                var assignment = floor?.RoomContents.Assignments.FirstOrDefault(a => a.AssignmentId == step.AssignmentId);
                corridorOptions.TryGetValue((step.FloorIndex, step.AssignmentId ?? string.Empty), out var corridorOption);
                var option = spatial?.OptionId ?? step.Encounter?.OptionId ?? assignment?.OptionId ?? corridorOption;
                var monsterPath = Array.Empty<Vector2>();
                if (step.Kind == RunStepKind.MonsterEngagement)
                {
                    if (spatial?.Kind != RunSpatialEventKind.MonsterEngaged || spatial.Path == null || spatial.Path.Length == 0 ||
                        spatial.Path.Length > policy.MaximumReplaySteps || !spatial.Path[0].Equals(spatial.ConfiguredStart) ||
                        !spatial.Path[spatial.Path.Length - 1].Equals(step.Position) || !spatial.Position.Equals(step.Position)) return false;
                    monsterPath = new Vector2[spatial.Path.Length];
                    pathPoints += monsterPath.Length;
                    if (pathPoints > policy.MaximumReplaySteps) return false;
                    for (int p = 0; p < monsterPath.Length; p++)
                    {
                        if (!TryProject(spatial.Path[p], RunPositionSpace.RoomLocal, step.RoomId, floor, catalog, out monsterPath[p]) ||
                            p > 0 && Mathf.Abs(monsterPath[p].x - monsterPath[p - 1].x) + Mathf.Abs(monsterPath[p].y - monsterPath[p - 1].y) != 1) return false;
                    }
                }
                if (hasMarker && ++markerCount > policy.MaximumReplayMarkers) return false;
                if (hasMarker) markerIds.Add(step.AssignmentId);
                list.Add(new DungeonReplayFrame(step, floorId, position, located, survivors, category, option, marker, hasMarker, monsterPath));
                lastPosition = position; lastFloor = floorId; lastHasPosition = located;
            }
            if (!list.Any(f => f.HasPosition) || survivors != outcome.SurvivalSummary?.SurvivorCount) return false;
            if (markerCount + corridorContents.Count(c => !markerIds.Contains(c.AssignmentId)) > policy.MaximumReplayMarkers) return false;
            replay = new DungeonRunReplay(list.ToArray(), corridorContents.ToArray(), policy); reason = null; return true;
        }
        public static bool TryProject(TileCoordinate position, RunPositionSpace space, string roomId,
            SavedSpatialFloor floor, SpatialContentCatalog catalog, out Vector2 result)
        {
            result = default;
            TileCoordinate tile = position;
            if (space == RunPositionSpace.RoomLocal)
            {
                var room = floor.Layout.Rooms.SingleOrDefault(r => r.RoomInstanceId == roomId);
                var definition = catalog.Rooms.SingleOrDefault(r => r.RoomDefinitionId == room?.RoomDefinitionId);
                if (definition == null || !RoomLocalCoordinateTransform.TryToFloor(position, definition.GrossFootprint,
                    room.Anchor, room.Orientation, out tile)) return false;
            }
            else if (space != RunPositionSpace.Floor) return false;
            result = new Vector2(tile.X + .5f, tile.Y + .5f); return true;
        }
    }
}
