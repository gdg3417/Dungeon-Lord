using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    // Format 2 owns this explicit input contract. Never append fields to the B1 capture.
    internal static class CombatStartCapture
    {
        internal const int Version = 1;
        internal static byte[] Capture(int sequence, ActiveFloorRunSnapshot snapshot, RunLogicalTiming timing,
            long? limit, bool persistent) => RunCheckpointCodec.Bytes(w => {
                w.Write(Version); RunCheckpointCodec.Text(w, persistent ? MonsterRecoveryRules.Executable : CombatC0Config.Executable);
                w.Write(sequence); w.Write(limit.HasValue); if (limit.HasValue) w.Write(limit.Value);
                w.Write(timing.UnitsPerTileEdge); w.Write(timing.MonsterInteractionUnits); w.Write(timing.TrapInteractionUnits);
                w.Write(timing.LootInteractionUnits); w.Write(timing.FloorTransferUnits);
                snapshot.WriteCheckpoint(w);
                var c = snapshot.CombatRules;
                RunCheckpointCodec.Text(w, c.RulesId); RunCheckpointCodec.Text(w, c.TargetingId);
                w.Write(c.MaximumRounds); w.Write(c.MaximumParticipants); w.Write(c.MaximumEncounters); w.Write(c.MaximumEvents); w.Write(c.MaximumWork);
                Profiles(w, c.Classes.ToArray()); Profiles(w, c.Monsters.ToArray());
                w.Write(snapshot.CombatBounds.Length);
                foreach (var b in snapshot.CombatBounds) {
                    RunCheckpointCodec.Text(w, b.FloorId); RunCheckpointCodec.Text(w, b.RoomId); w.Write(b.Width); w.Write(b.Height);
                }
                var starts = persistent ? snapshot.MonsterStarts.Records : null;
                w.Write(starts != null);
                if (starts != null) {
                    w.Write(snapshot.MonsterStartLevel); w.Write(snapshot.MonsterReadinessNumerator); w.Write(snapshot.MonsterReadinessDenominator);
                    w.Write(starts.Length);
                    var options = snapshot.MonsterStartOptions;
                    for (int i = 0; i < starts.Length; i++) {
                        var m = starts[i]; RunCheckpointCodec.Text(w, m.AssignmentId); RunCheckpointCodec.Text(w, options[i]);
                        w.Write(m.Level); w.Write(m.Health); w.Write(m.RevivalRequired);
                    }
                }
            });

        private static void Profiles(BinaryWriter w, CombatC0AttackProfile[] profiles)
        {
            w.Write(profiles.Length);
            foreach (var p in profiles) {
                RunCheckpointCodec.Text(w, p.Id); RunCheckpointCodec.Text(w, p.AttackId);
                w.Write(p.Damage); w.Write(p.Initiative); w.Write(p.MonsterHealth); RunCheckpointCodec.Text(w, p.AttackType);
                w.Write(p.Range.HasValue); if (p.Range.HasValue) w.Write(p.Range.Value);
                w.Write(p.TargetClassOrder.Count); foreach (var id in p.TargetClassOrder) RunCheckpointCodec.Text(w, id);
            }
        }
        private static int Count(BinaryReader r, long maximum)
        {
            int n = r.ReadInt32();
            if (n < 0 || n > maximum || n > r.BaseStream.Length - r.BaseStream.Position) throw new InvalidDataException("run.checkpoint.encoding");
            return n;
        }
        private static CombatC0AttackProfile[] Profiles(BinaryReader r)
        {
            var profiles = new CombatC0AttackProfile[Count(r, r.BaseStream.Length)];
            for (int i = 0; i < profiles.Length; i++) {
                string id = RunCheckpointCodec.Text(r), attack = RunCheckpointCodec.Text(r);
                int damage = r.ReadInt32(), initiative = r.ReadInt32(), hp = r.ReadInt32(); string type = RunCheckpointCodec.Text(r);
                int? range = RunCheckpointCodec.Flag(r) ? r.ReadInt32() : (int?)null;
                var targets = new string[Count(r, r.BaseStream.Length)];
                for (int j = 0; j < targets.Length; j++) targets[j] = RunCheckpointCodec.Text(r);
                profiles[i] = new CombatC0AttackProfile(id, attack, damage, initiative, hp, targets, type, range);
            }
            return profiles;
        }
        internal static RunExecution Recreate(byte[] bytes, RunCheckpointLimits limits, string executable)
        {
            using (var r = new BinaryReader(new MemoryStream(bytes), new UTF8Encoding(false, true))) {
                if (r.ReadInt32() != Version || RunCheckpointCodec.Text(r) != executable) throw new InvalidDataException("run.checkpoint.unsupported_rules");
                int sequence = r.ReadInt32(); long? limit = RunCheckpointCodec.Flag(r) ? r.ReadInt64() : (long?)null;
                var timing = new RunLogicalTiming(r.ReadInt64(), r.ReadInt64(), r.ReadInt64(), r.ReadInt64(), r.ReadInt64());
                var snapshot = ActiveFloorRunSnapshot.ReadCheckpoint(r);
                string rules = RunCheckpointCodec.Text(r), targeting = RunCheckpointCodec.Text(r);
                int rounds = r.ReadInt32(), participants = r.ReadInt32(), encounters = r.ReadInt32(), events = r.ReadInt32(); long work = r.ReadInt64();
                if (rules != CombatC0Config.Executable || work <= 0 || work > limits.MaximumCombatWork)
                    throw new InvalidDataException("run.checkpoint.combat_limit");
                var combat = new CombatC0QualificationConfig(rules, targeting, CombatC0Config.InitiativePolicy,
                    rounds, participants, encounters, events, work, Profiles(r), Profiles(r));
                var bounds = new CombatC0RoomBounds[Count(r, work)];
                for (int i = 0; i < bounds.Length; i++) bounds[i] = new CombatC0RoomBounds(
                    RunCheckpointCodec.Text(r), RunCheckpointCodec.Text(r), r.ReadInt32(), r.ReadInt32());
                MonsterContinuityState starts = null;
                string[] options = null;
                int level = 0, numerator = 0, denominator = 0;
                if (RunCheckpointCodec.Flag(r)) {
                    level = r.ReadInt32(); numerator = r.ReadInt32(); denominator = r.ReadInt32();
                    var records = new MonsterContinuityRecord[Count(r, work)];
                    options = new string[records.Length];
                    for (int i = 0; i < records.Length; i++) {
                        string id = RunCheckpointCodec.Text(r); options[i] = RunCheckpointCodec.Text(r);
                        records[i] = new MonsterContinuityRecord(id, r.ReadInt32(), r.ReadInt32(), RunCheckpointCodec.Flag(r));
                    }
                    starts = new MonsterContinuityState(records);
                }
                RunCheckpointCodec.End(r);
                bool persistent = executable == MonsterRecoveryRules.Executable;
                if (persistent != (starts != null)) throw new InvalidDataException("run.checkpoint.invalid_inputs");
                snapshot.RestoreCombatInputs(combat, bounds, starts, level, numerator, denominator, options);
                Validate(snapshot, persistent);
                var engine = new RunSimulationService(snapshot.Configuration, snapshot.LootConfiguration, snapshot.LootTableId);
                var execution = engine.StartTimedCombatSnapshot(sequence, snapshot, timing, persistent, limit);
                if (!bytes.SequenceEqual(execution.StartInputs)) { execution.Abandon(); throw new InvalidDataException("run.checkpoint.encoding"); }
                return execution;
            }
        }

        internal static void Validate(ActiveFloorRunSnapshot snapshot, bool persistent)
        {
            var c = snapshot?.CombatRules;
            if (c == null || !c.IsProduction || snapshot.CombatBounds == null) throw new ArgumentException(CombatC0Kernel.InvalidState);
            for (int i = 0; i < snapshot.Floors.Count; i++) {
                if (snapshot.Floors[i].FloorIndex != i) throw new ArgumentException(CombatC0Kernel.InvalidState);
                snapshot.Floors[i].ValidateCombatCapture(c.MaximumWork);
            }
            var rooms = snapshot.Floors.SelectMany(f => f.MaterializePlan().RequiredRooms.Select(r => r.Room)).ToArray();
            if (rooms.Length != snapshot.CombatBounds.Length || rooms.Length > c.MaximumWork) throw new ArgumentException(CombatC0Kernel.InvalidState);
            var bounds = snapshot.CombatBounds.ToDictionary(b => (b.FloorId, b.RoomId));
            long cells = 0;
            foreach (var room in rooms) {
                var s = room.Spatial;
                if (s == null || !bounds.TryGetValue((s.FloorInstanceId, room.RoomInstanceId), out var b) ||
                    b.Width <= 0 || b.Height <= 0 || s.MaximumMaterializedTiles <= 0 ||
                    (long)b.Width * b.Height > s.MaximumMaterializedTiles ||
                    s.TraversableTiles == null || s.TraversableTiles.Length > s.MaximumMaterializedTiles ||
                    s.TraversableTiles.Any(t => t.X < 0 || t.Y < 0 || t.X >= b.Width || t.Y >= b.Height))
                    throw new ArgumentException(CombatC0Kernel.InvalidState);
                cells = checked(cells + s.TraversableTiles.Length);
                if (cells > c.MaximumWork) throw new ArgumentException(CombatC0Kernel.Workload);
            }
            if (persistent) {
                var starts = snapshot.MonsterStarts?.Records ?? throw new ArgumentException(CombatC0Kernel.InvalidState);
                if (snapshot.MonsterStartLevel <= 0 || snapshot.MonsterReadinessNumerator <= 0 ||
                    snapshot.MonsterReadinessDenominator < snapshot.MonsterReadinessNumerator) throw new ArgumentException(CombatC0Kernel.InvalidState);
                if (starts.Length > c.MaximumWork || starts.Any(m => m == null || string.IsNullOrEmpty(m.AssignmentId) ||
                    m.Level != snapshot.MonsterStartLevel || m.Health < 0 || (m.Health == 0 && !m.RevivalRequired)) ||
                    !starts.Select(m => m.AssignmentId).SequenceEqual(starts.Select(m => m.AssignmentId).Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal)))
                    throw new ArgumentException(CombatC0Kernel.InvalidState);
                var states = starts.ToDictionary(m => m.AssignmentId, StringComparer.Ordinal);
                var options = snapshot.MonsterStartOptions;
                if (options == null || options.Length != starts.Length) throw new ArgumentException(CombatC0Kernel.InvalidState);
                for (int i = 0; i < starts.Length; i++) {
                    var profile = c.Monsters.SingleOrDefault(p => p.Id == options[i]);
                    if (profile == null || starts[i].Health > profile.MonsterHealth || starts[i].RevivalRequired &&
                        starts[i].Health >= checked(((long)profile.MonsterHealth * snapshot.MonsterReadinessNumerator +
                            snapshot.MonsterReadinessDenominator - 1) / snapshot.MonsterReadinessDenominator))
                        throw new ArgumentException(CombatC0Kernel.InvalidState);
                }
                var optionById = starts.Select((m, i) => (m.AssignmentId, Option: options[i])).ToDictionary(m => m.AssignmentId, m => m.Option, StringComparer.Ordinal);
                foreach (var a in rooms.SelectMany(r => r.OrderedAssignments()).Where(a => a.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId))
                    if (!states.ContainsKey(a.AssignmentId) || optionById[a.AssignmentId] != a.OptionId)
                        throw new ArgumentException(CombatC0Kernel.InvalidState);
            }
        }
    }
}
