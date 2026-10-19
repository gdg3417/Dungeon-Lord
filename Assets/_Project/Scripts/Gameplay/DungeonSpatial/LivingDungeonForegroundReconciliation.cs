using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.RunSimulation;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // One detached terminal candidate. The canonical continuity owner remains sole authority.
    internal static class LivingDungeonForegroundReconciliation
    {
        internal const string Conflict = "living.foreground.condition_conflict";

        internal static MonsterContinuityState Apply(RunExecution execution, RunOutcomeRecord outcome,
            MonsterContinuityState current, DetachedCanonicalSpatialSaveState spatial, MonsterRecoveryRules rules)
        {
            if (execution?.Snapshot == null || !execution.IsTerminal || !execution.OutcomeTaken ||
                outcome?.RunId != execution.Inspect().RunId || !CombatC0Diagnostics.IsCombatC2(outcome) ||
                !MonsterContinuity.Valid(current, spatial, rules, int.MaxValue)) Fail();
            var snapshot = execution.Snapshot;
            var initialRecords = snapshot.MonsterStarts.Records;
            var starts = initialRecords.ToDictionary(r => r.AssignmentId, StringComparer.Ordinal);
            var initialOptions = initialRecords.Select(r => r.AssignmentId).Zip(snapshot.MonsterStartOptions, (id, option) => (id, option))
                .ToDictionary(p => p.id, p => p.option, StringComparer.Ordinal);
            var owned = MonsterContinuity.Owned(spatial).ToDictionary(p => p.Id, p => p.Option, StringComparer.Ordinal);
            var result = current.Copy(); var records = result.Records.ToDictionary(r => r.AssignmentId, StringComparer.Ordinal);
            var monsters = CombatC0Diagnostics.Monsters(outcome);
            if (monsters.Select(m => m.AssignmentId).Distinct(StringComparer.Ordinal).Count() != monsters.Count) Fail();
            var verified = execution.Combat.Kernel.InspectMonsters();
            if (verified.Length != monsters.Count) Fail();
            int priorEvent = -1; long priorBoundary = -1;
            var hp = monsters.ToDictionary(m => m.AssignmentId, m => m.InitialHealth, StringComparer.Ordinal);
            var byActor = monsters.ToDictionary(m => CombatC0Kernel.MonsterId(execution.Party, m.AssignmentId), m => m.AssignmentId, StringComparer.Ordinal);
            var receipts = CombatC0Diagnostics.Events(outcome);
            if (receipts.Count != execution.Combat.Events.Count) Fail();
            foreach (var e in receipts) {
                if (e.Ordinal != ++priorEvent || !ReferenceEquals(e, execution.Combat.Events[priorEvent]) || e.RunOrdinal < priorBoundary || e.RunOrdinal < 0 ||
                    e.RunOrdinal >= execution.NextOrdinal || e.RunId != outcome.RunId || e.ExecutableId != MonsterRecoveryRules.Executable) Fail();
                priorBoundary = e.RunOrdinal;
                if (e.Kind != CombatC0EventKind.Attack || !byActor.TryGetValue(e.TargetId, out var id)) continue;
                var monster = monsters.Single(m => m.AssignmentId == id);
                if (e.FloorId != monster.FloorId || e.RoomId != monster.RoomId || hp[id] != e.HealthBefore ||
                    e.HealthAfter < 0 || e.HealthAfter > e.HealthBefore || e.Damage != e.HealthBefore - e.HealthAfter) Fail();
                hp[id] = e.HealthAfter;
            }
            foreach (var m in monsters) {
                var exact = verified.SingleOrDefault(v => v.AssignmentId == m.AssignmentId);
                if (exact == null || !starts.TryGetValue(m.AssignmentId, out var initial) || initialOptions[m.AssignmentId] != m.OptionId ||
                    m.InitialHealth != initial.Health || m.InitialRevivalRequired != initial.RevivalRequired ||
                    m.Health != exact.Health || m.InitialHealth != exact.InitialHealth || m.MaxHealth != exact.MaxHealth ||
                    m.InitialRevivalRequired != exact.InitialRevivalRequired || m.RevivalRequired != exact.RevivalRequired ||
                    m.FloorId != exact.FloorId || m.RoomId != exact.RoomId || m.OptionId != exact.OptionId || hp[m.AssignmentId] != m.Health ||
                    m.Health < 0 || m.Health > m.InitialHealth || initial.Level != snapshot.MonsterStartLevel) Fail();
                bool exists = owned.TryGetValue(m.AssignmentId, out var option);
                bool retained = records.TryGetValue(m.AssignmentId, out var record);
                if (exists != retained) Fail();
                if (!exists) continue; // legitimate retirement; never recreate or charge it
                if (option != m.OptionId || record.Level != initial.Level || record.Level != rules.InitialLevel) Fail();
                int damage = checked(m.InitialHealth - m.Health);
                record.Health = Math.Max(0, record.Health - damage);
                record.RevivalRequired = record.Health == 0 || record.RevivalRequired && record.Health < rules.Readiness(option);
            }
            if (!MonsterContinuity.Valid(result, spatial, rules, int.MaxValue)) Fail();
            return result;
        }

        private static void Fail() => throw new InvalidOperationException(Conflict);
    }
}
