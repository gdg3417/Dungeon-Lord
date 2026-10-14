using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    // Isolated bounded reciprocal kernel, not an alternate route/settlement simulator.
    // Production uses the approved C0 identity; historical B1 never calls this kernel.
    internal sealed class CombatC0Kernel
    {
        internal const string InvalidState = "combat.c0.invalid_state";
        internal const string Workload = "combat.c0.workload_exceeded";
        private readonly CombatC0QualificationConfig config;
        private readonly RunParty party;
        private readonly Actor[] members, monsters;
        private readonly Dictionary<string, Actor> monstersByAssignment;
        private readonly Dictionary<string, CombatC0AttackProfile> classProfiles;
        private readonly Dictionary<(string, string, int), CombatC0EncounterResult> completed = new Dictionary<(string, string, int), CombatC0EncounterResult>();
        private readonly Dictionary<(string, string, int), CombatC0AttackLink[]> completedLinks = new Dictionary<(string, string, int), CombatC0AttackLink[]>();
        private int eventCount;
        private long workCount;
        private bool resolving;
        private sealed class Actor
        {
            internal string Id, ClassId;
            internal long Order;
            internal int Health;
            internal RunPartyMember Member;
            internal CombatC0MonsterPlacement Placement;
            internal CombatC0AttackProfile Profile;
            internal bool IsMonster => Placement != null;
        }
        internal string RulesId => config.RulesId;
        internal int EventCount => eventCount;
        internal long WorkCount => workCount;
        internal CombatC0QualificationConfig Configuration => config;
        internal int CompletedCount => completed.Count;
        internal RunParty Party => party;
        internal void ChargeGeometry(long units)
        {
            if (units < 0 || units > config.MaximumWork - workCount) throw new InvalidOperationException(Workload);
            workCount += units;
        }
        internal CombatC0AttackProfile MonsterProfile(string assignment) => monstersByAssignment[assignment].Profile;
        internal CombatC0AttackProfile MemberProfile(string classId) => classProfiles[classId];
        internal CombatC0MonsterState[] InspectMonsters() => monsters.Select(m => new CombatC0MonsterState(
            m.Placement, m.Profile.MonsterHealth, m.Health)).ToArray();
        internal static string MemberId(RunParty party, int ordinal) => party.RunId + "/member/" + ordinal.ToString(CultureInfo.InvariantCulture);
        internal static string MonsterId(RunParty party, string assignment) => party.RunId + "/monster/" + assignment;
        internal int MonsterHealth(string assignment) => monstersByAssignment[assignment].Health;

        internal CombatC0Kernel(RunParty party, CombatC0QualificationConfig config, CombatC0MonsterPlacement[] placements)
        {
            this.party = party; this.config = config;
            if (party == null || config == null || placements == null || string.IsNullOrWhiteSpace(party.RunId) ||
                party.Members == null || party.Members.Count == 0 ||
                (long)party.Members.Count + placements.Length > config.MaximumParticipants) throw new ArgumentException(InvalidState);
            ValidateParty();
            if (placements.Any(p => p == null || string.IsNullOrWhiteSpace(p.AssignmentId) ||
                string.IsNullOrWhiteSpace(p.FloorId) || string.IsNullOrWhiteSpace(p.RoomId) || p.Sequence < 0 ||
                !config.Monsters.Any(c => c.Id == p.OptionId)) ||
                placements.Select(p => p.AssignmentId).Distinct(StringComparer.Ordinal).Count() != placements.Length)
                throw new ArgumentException(InvalidState);
            members = party.Members.OrderBy(m => m.MemberOrdinal).Select(m => new Actor { Id = MemberId(party, m.MemberOrdinal),
                ClassId = m.ClassId, Member = m, Order = m.MemberOrdinal, Profile = config.Classes.Single(p => p.Id == m.ClassId) }).ToArray();
            monsters = placements.OrderBy(p => p.Sequence).ThenBy(p => p.AssignmentId, StringComparer.Ordinal).Select(p => {
                var profile = config.Monsters.Single(c => c.Id == p.OptionId);
                return new Actor { Id = MonsterId(party, p.AssignmentId), Order = p.Sequence, Placement = p, Profile = profile,
                    Health = profile.MonsterHealth };
            }).ToArray();
            monstersByAssignment = monsters.ToDictionary(m => m.Placement.AssignmentId, StringComparer.Ordinal);
            classProfiles = config.Classes.ToDictionary(c => c.Id, StringComparer.Ordinal);
            if (config.IsProduction) ChargeGeometry(checked((long)placements.Length * placements.Length +
                (long)members.Length * members.Length + placements.Length + members.Length));
        }
        private void ValidateParty()
        {
            if (party.Members.Any(m => m == null || m.MemberOrdinal < 0 || m.MaxHealth <= 0 || m.CurrentHealth < 0 ||
                m.CurrentHealth > m.MaxHealth || !config.Classes.Any(c => c.Id == m.ClassId)) ||
                party.Members.Select(m => m.MemberOrdinal).Distinct().Count() != party.Members.Count) throw new ArgumentException(InvalidState);
        }

        // Legacy qualification callers use encounter zero per room. Production supplies
        // a run-global ordinal, so multiple distinct encounters in a room retain HP.
        // All work is staged; invalid state or exhausted evidence/work budgets publish no HP.
        internal CombatC0EncounterResult Resolve(string floor, string room, string[] assignments, CombatC0AttackLink[] links)
            => Resolve(floor, room, 0, assignments, links);
        internal CombatC0EncounterResult Resolve(string floor, string room, int encounterOrdinal, string[] assignments, CombatC0AttackLink[] links)
        {
            if (resolving) throw new InvalidOperationException(InvalidState);
            if (string.IsNullOrWhiteSpace(floor) || string.IsNullOrWhiteSpace(room) || encounterOrdinal < 0 || assignments == null || links == null)
                throw new ArgumentException(InvalidState);
            var key = (floor, room, encounterOrdinal);
            bool repeated = completed.TryGetValue(key, out var receipt);
            if (!repeated && completed.Count >= config.MaximumEncounters) throw new InvalidOperationException(Workload);
            ValidateParty();
            if (assignments.Length > monsters.Length || assignments.Distinct(StringComparer.Ordinal).Count() != assignments.Length)
                throw new ArgumentException(InvalidState);
            var candidates = assignments.Select(id => monsters.SingleOrDefault(m => m.Placement.AssignmentId == id)).ToArray();
            if (candidates.Any(m => m == null || m.Placement.FloorId != floor || m.Placement.RoomId != room)) throw new ArgumentException(InvalidState);
            var actors = members.Concat(candidates).OrderByDescending(a => a.Profile.Initiative)
                .ThenBy(a => a.IsMonster).ThenBy(a => a.Order).ThenBy(a => a.Id, StringComparer.Ordinal).ToArray();
            // Exactly one directed pair per opposing participant, including explicit denials.
            if (links.LongLength != checked(2L * members.Length * candidates.Length)) throw new ArgumentException(InvalidState);
            var byId = actors.ToDictionary(a => a.Id, StringComparer.Ordinal);
            var matrix = new Dictionary<(string, string), CombatC0AttackLink>();
            var positions = new Dictionary<string, TileCoordinate>(StringComparer.Ordinal);
            foreach (var link in links)
            {
                if (link == null || link.ActorId == null || link.TargetId == null ||
                    !byId.TryGetValue(link.ActorId, out var actor) || !byId.TryGetValue(link.TargetId, out var target) ||
                    actor.IsMonster == target.IsMonster || link.Reason < CombatC0Reason.Legal ||
                    (link.Reason > CombatC0Reason.Unreachable && link.Reason != CombatC0Reason.PartyAnchorConflict) ||
                    matrix.ContainsKey((link.ActorId, link.TargetId))) throw new ArgumentException(InvalidState);
                matrix.Add((link.ActorId, link.TargetId), link);
                CheckPosition(positions, link.ActorId, link.Source); CheckPosition(positions, link.TargetId, link.Target);
            }
            var canonicalLinks = links.OrderBy(l => l.ActorId, StringComparer.Ordinal).ThenBy(l => l.TargetId, StringComparer.Ordinal).ToArray();
            if (repeated)
            {
                var previous = completedLinks[key];
                if (previous.Length != canonicalLinks.Length || previous.Where((p, i) => p.ActorId != canonicalLinks[i].ActorId ||
                    p.TargetId != canonicalLinks[i].TargetId || !p.Source.Equals(canonicalLinks[i].Source) ||
                    !p.Target.Equals(canonicalLinks[i].Target) || p.Reason != canonicalLinks[i].Reason).Any())
                    throw new ArgumentException(InvalidState);
                return receipt;
            }
            resolving = true;
            try
            {
                var result = ResolveStaged(floor, room, encounterOrdinal, actors, matrix, positions);
                completedLinks.Add(key, canonicalLinks);
                return result;
            }
            finally { resolving = false; }
        }
        private static void CheckPosition(Dictionary<string, TileCoordinate> positions, string id, TileCoordinate tile)
        {
            if (positions.TryGetValue(id, out var previous) && !previous.Equals(tile)) throw new ArgumentException(InvalidState);
            positions[id] = tile;
        }

        private CombatC0EncounterResult ResolveStaged(string floor, string room, int encounterOrdinal, Actor[] actors,
            Dictionary<(string, string), CombatC0AttackLink> links, Dictionary<string, TileCoordinate> positions)
        {
            long work = workCount;
            void Charge(long amount) { work = checked(work + amount); if (work > config.MaximumWork) throw new InvalidOperationException(Workload); }
            Charge(checked(actors.LongLength + links.Count));
            // Conservative charges for validation, canonical pair sorting, actor lookups
            // and target-order construction. Existing qualification counters stay historical.
            if (config.IsProduction) Charge(checked(actors.LongLength * actors.LongLength * actors.LongLength +
                (long)links.Count * links.Count + (long)links.Count * actors.Length));
            var health = actors.ToDictionary(a => a.Id, a => a.IsMonster ? a.Health : a.Member.CurrentHealth, StringComparer.Ordinal);
            var initial = new Dictionary<string, int>(health, StringComparer.Ordinal);
            string encounter = party.RunId + "/combat/" + completed.Count.ToString(CultureInfo.InvariantCulture);
            var events = new List<CombatC0Event>();
            int actionOrdinal = 0;
            void Emit(CombatC0EventKind kind, CombatC0Reason reason, int round, Actor actor = null, Actor target = null, int before = 0, int after = 0)
            {
                if ((long)eventCount + events.Count >= config.MaximumEvents) throw new InvalidOperationException(Workload);
                events.Add(new CombatC0Event(party.RunId, encounter, checked(eventCount + events.Count),
                    kind == CombatC0EventKind.Attack || kind == CombatC0EventKind.Skipped ? actionOrdinal++ : -1,
                    round, floor, room, kind, reason, actor?.Id, target?.Id, actor?.Profile.AttackId,
                    actor != null && positions.TryGetValue(actor.Id, out var source) ? source : (TileCoordinate?)null,
                    target != null && positions.TryGetValue(target.Id, out var destination) ? destination : (TileCoordinate?)null, before, after,
                    executable: config.RulesId, targeting: config.TargetingId));
            }
            Emit(CombatC0EventKind.EncounterStart, CombatC0Reason.Legal, 0);
            foreach (var link in links.Values.OrderBy(l => l.ActorId, StringComparer.Ordinal).ThenBy(l => l.TargetId, StringComparer.Ordinal))
                Emit(CombatC0EventKind.Eligibility, link.Reason, 0, actors.Single(a => a.Id == link.ActorId), actors.Single(a => a.Id == link.TargetId));
            var participating = actors.Where(a => !a.IsMonster || links.Values.Any(l => l.Reason == CombatC0Reason.Legal &&
                (l.ActorId == a.Id || l.TargetId == a.Id))).ToArray();
            var targetOrder = participating.ToDictionary(a => a.Id, a => participating.Where(t => t.IsMonster != a.IsMonster)
                .OrderBy(t => a.IsMonster ? ClassRank(a.Profile.TargetClassOrder, t.ClassId) : 0)
                .ThenBy(t => t.Order).ThenBy(t => t.Id, StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
            CombatC0Reason? Terminal()
            {
                if (!members.Any(a => health[a.Id] > 0)) return CombatC0Reason.PartyWiped;
                if (!participating.Any(a => a.IsMonster)) return CombatC0Reason.NoParticipants;
                if (!participating.Any(a => a.IsMonster && health[a.Id] > 0)) return CombatC0Reason.DefendersDefeated;
                return null;
            }
            var completion = Terminal();
            int round = 0;
            while (!completion.HasValue && round < config.MaximumRounds)
            {
                round++; bool damaged = false;
                foreach (var actor in participating)
                {
                    Charge(1);
                    if (health[actor.Id] == 0) { Emit(CombatC0EventKind.Skipped, CombatC0Reason.DefeatedBeforeAction, round, actor); continue; }
                    Actor target = null;
                    foreach (var candidate in targetOrder[actor.Id])
                    {
                        Charge(1);
                        if (health[candidate.Id] > 0 && links[(actor.Id, candidate.Id)].Reason == CombatC0Reason.Legal) { target = candidate; break; }
                    }
                    if (target == null) { Emit(CombatC0EventKind.Skipped, CombatC0Reason.NoLivingLegalTarget, round, actor); continue; }
                    int before = health[target.Id];
                    health[target.Id] = actor.Profile.Damage >= before ? 0 : before - actor.Profile.Damage;
                    damaged |= health[target.Id] < before;
                    Emit(CombatC0EventKind.Attack, CombatC0Reason.Legal, round, actor, target, before, health[target.Id]);
                    completion = Terminal();
                    if (completion.HasValue) break;
                }
                if (!completion.HasValue && !damaged) completion = CombatC0Reason.ZeroDamageRound;
            }
            if (!completion.HasValue) completion = CombatC0Reason.RoundLimit;
            if (config.IsProduction && (completion == CombatC0Reason.ZeroDamageRound || completion == CombatC0Reason.RoundLimit))
                throw new InvalidOperationException(completion == CombatC0Reason.ZeroDamageRound ? "combat.c0.stalemate" : "combat.c0.round_limit");
            Emit(CombatC0EventKind.EncounterComplete, completion.Value, round);
            var result = new CombatC0EncounterResult(completion.Value, events.ToArray());
            // No skills/healing, so stage -> existing party damage authority is exact.
            foreach (var actor in actors)
                if (actor.IsMonster) actor.Health = health[actor.Id];
                else actor.Member.ApplyDamage(initial[actor.Id] - health[actor.Id]);
            eventCount = checked(eventCount + events.Count); workCount = work;
            completed.Add((floor, room, encounterOrdinal), result);
            return result;
        }
        internal CombatC0Event RecordBypass(string floor, string room, string assignment, CombatC0AttackLink link)
        {
            if (eventCount >= config.MaximumEvents) throw new InvalidOperationException(Workload);
            ChargeGeometry(1);
            return new CombatC0Event(party.RunId, null, eventCount++, -1, 0, floor, room, CombatC0EventKind.Bypass,
                link.Reason, link.ActorId, link.TargetId, MonsterProfile(assignment).AttackId, link.Source, link.Target,
                executable: config.RulesId, targeting: config.TargetingId);
        }
        private static int ClassRank(IReadOnlyList<string> order, string classId)
        { for (int i = 0; i < order.Count; i++) if (order[i] == classId) return i; throw new ArgumentException(InvalidState); }

        // Explicitly incompatible with the durable B1 API; no C0 checkpoint format is invented.
        internal RunCheckpoint ExportCheckpoint(RunCheckpointLimits limits) =>
            throw new InvalidOperationException("combat.c0.checkpoint_contract_unapproved");
    }
}
