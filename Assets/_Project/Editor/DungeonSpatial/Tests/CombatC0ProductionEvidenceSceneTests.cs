#if UNITY_EDITOR
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests
{
    public abstract partial class PhaseSevenA4ProductionSceneTests
    {
        // Test artifact only: same production startup/manual transaction as the preserved
        // 18-event input reproduction. No owner save is loaded and no input UI is exercised.
        [UnityTest]
        public IEnumerator CombatC0ProductionEvidenceExport()
        {
            var root = GameRoot.Instance;
            root.Save.structureRuntime.ManaReserve = 1000; // Existing isolated fixture QA funding.
            var config = root.RunSimulationConfig.CombatC0;
            Assert.That(root.SimulateRunOnce(), Is.True);
            var run = root.Save.runHistory.LatestOutcome;
            var selected = root.Save.runHistory.RecentOutcomes.Last();
            var events = CombatC0Diagnostics.Events(run);
            var monsters = CombatC0Diagnostics.Monsters(run);
            Assert.That(events.Count, Is.EqualTo(18));
            Assert.That(selected.RunId, Is.EqualTo(run.RunId));
            CollectionAssert.AreEqual(events, CombatC0Diagnostics.Events(selected));
            Assert.That(events.All(e => e.RunId == run.RunId && e.ExecutableId == MonsterRecoveryRules.Executable), Is.True);
            Assert.That(events.Select(e => e.Ordinal), Is.Ordered.And.Unique);
            Assert.That(events.Any(e => e.Kind == CombatC0EventKind.Attack && e.Damage > 0), Is.True);
            Assert.That(events.Any(e => e.Kind == CombatC0EventKind.Attack && e.ActorId.Contains("/monster/")), Is.True);
            Assert.That(events.Last().Kind, Is.EqualTo(CombatC0EventKind.EncounterComplete));
            Assert.That(events.Last().Reason, Is.EqualTo(CombatC0Reason.DefendersDefeated));
            var hp = run.Party.Members.ToDictionary(m => CombatC0Kernel.MemberId(run.Party, m.MemberOrdinal), m => m.MaxHealth);
            foreach (var monster in monsters) hp.Add(CombatC0Kernel.MonsterId(run.Party, monster.AssignmentId), monster.InitialHealth);
            foreach (var e in events.Where(e => e.Kind == CombatC0EventKind.Attack))
            {
                Assert.That(hp[e.ActorId], Is.GreaterThan(0), "A defeated actor cannot attack");
                Assert.That(e.HealthBefore, Is.EqualTo(hp[e.TargetId]), "Authoritative target HP must join the preceding event");
                Assert.That(e.HealthAfter, Is.EqualTo(e.HealthBefore - e.Damage).And.GreaterThanOrEqualTo(0));
                hp[e.TargetId] = e.HealthAfter;
            }
            foreach (var member in run.Party.Members)
                Assert.That(hp[CombatC0Kernel.MemberId(run.Party, member.MemberOrdinal)], Is.EqualTo(member.CurrentHealth));
            foreach (var monster in monsters)
                Assert.That(hp[CombatC0Kernel.MonsterId(run.Party, monster.AssignmentId)], Is.EqualTo(monster.Health));
            var report = new StringBuilder();
            void Line(FormattableString value) => report.AppendLine(value.ToString(CultureInfo.InvariantCulture));
            string Position(TileCoordinate? tile) => tile.HasValue
                ? FormattableString.Invariant($"({tile.Value.X},{tile.Value.Y})") : "<none>";
            report.AppendLine("Combat C0 authoritative production-path evidence — automated disposable fixture");
            report.AppendLine("Not owner run-68/69/71/72. Input-reproduction findings are archived separately.");
            Line($"Unity={Application.unityVersion}; scene={root.gameObject.scene.name}; save={root.SaveService.SavePath}");
            Line($"run={run.RunId}; events={events.Count}; chargedWork={CombatC0Diagnostics.ChargedWork(run)}");
            report.AppendLine("Loaded production combat configuration (unchanged during this run):");
            report.AppendLine(JsonUtility.ToJson(config, true));
            report.AppendLine("Participants: full canonical IDs, initiative inputs and final HP");
            foreach (var member in run.Party.Members)
            {
                var profile = config.Classes.Single(p => p.Id == member.ClassId);
                Line($"party id={CombatC0Kernel.MemberId(run.Party, member.MemberOrdinal)}; memberOrdinal={member.MemberOrdinal}; class={member.ClassId}; initiative={profile.Initiative}; maxHP={member.MaxHealth}; finalHP={member.CurrentHealth}; active={member.IsActive}");
            }
            foreach (var monster in monsters)
            {
                var profile = config.Monsters.Single(p => p.Id == monster.OptionId);
                var assignment = root.Save.spatialFloors.Single(f => f.FloorInstanceId == monster.FloorId)
                    .RoomContents.Assignments.Single(a => a.AssignmentId == monster.AssignmentId);
                Line($"monster id={CombatC0Kernel.MonsterId(run.Party, monster.AssignmentId)}; assignment={monster.AssignmentId}; option={monster.OptionId}; sequence={assignment.Sequence}; initiative={profile.Initiative}; floor={monster.FloorId}; room={monster.RoomId}; placed={Position(assignment.RoomLocalPosition)}; maxHP={monster.MaxHealth}; finalHP={monster.Health}; defeated={monster.Defeated}");
            }
            report.AppendLine("Chronological events: exact API enumeration, no sorting, filtering or truncation.");
            foreach (var e in events)
                Line($"ordinal={e.Ordinal}; runOrdinal={e.RunOrdinal}; actionOrdinal={e.ActionOrdinal}; round={e.Round}; run={e.RunId}; encounter={e.EncounterId ?? "<none>"}; executable={e.ExecutableId}; rules={e.RulesId}; ruleSource={e.RuleSourceId}; floor={e.FloorId}; room={e.RoomId}; kind={e.Kind}; reason={e.Reason}; actor={e.ActorId ?? "<none>"}; target={e.TargetId ?? "<none>"}; attack={e.AttackId ?? "<none>"}; targeting={e.TargetingId ?? "<none>"}; space={e.PositionSpace}; source={Position(e.SourcePosition)}; destination={Position(e.TargetPosition)}; beforeHP={e.HealthBefore}; afterHP={e.HealthAfter}; damage={e.Damage}; defeated={e.Defeated}");
            report.AppendLine("Resulting canonical aggregate run outcome (transient properties intentionally excluded):");
            report.AppendLine(JsonUtility.ToJson(run, true));
            report.AppendLine("Economy/history after the existing atomic manual transaction:");
            Line($"mana={root.Save.structureRuntime.ManaReserve}; heat={root.Save.structureRuntime.Heat}; nextRunSequence={root.Save.runHistory.NextRunSequence}; latest={run.RunId}; selected={selected.RunId}");
            string text = report.ToString();
            Assert.That(text.Split('\n').Count(l => l.StartsWith("ordinal=", StringComparison.Ordinal)), Is.EqualTo(events.Count));
            string directory = Path.GetFullPath("TestResults/combat-c0-authoritative");
            Directory.CreateDirectory(directory);
            // Scene saves already use a unique fixture identity; reuse it for artifacts so
            // later suite/rerun evidence cannot overwrite or collide with an earlier report.
            string path = Path.Combine(directory, "production-path-" + run.RunId + "-" +
                Path.GetFileNameWithoutExtension(root.SaveService.SavePath) + ".txt");
            using (var writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write), new UTF8Encoding(false)))
                writer.Write(text);
            TestContext.WriteLine("Complete authoritative evidence: " + path);
            yield return null;
        }
    }
}
#endif
