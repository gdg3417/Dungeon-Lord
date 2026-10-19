#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;
using B = DungeonBuilder.M0.Tests.EditMode.LivingDungeonPhaseABaselineTests;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class LivingDungeonB3B2Tests
    {
        private static LivingDungeonForegroundConfig Policy => LivingDungeonForegroundConfig.Load();
        private static Fixture Dungeon()
        {
            var f = Fixture.Create(null); f.Configuration = CombatC0IntegrationTests.Config();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)));
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);
            f.Profile = SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
            f.Context = new DetachedCurrentTargetValidationContext(f.Compatibility, f.Production,
                LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.Profile.Canonical, f.Occupancy,
                livingDungeonProfiles: new[] { Policy.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized) },
                monsterRecovery: MonsterRecoveryRules.Capture(f.Configuration));
            f.Reopen(); return f;
        }
        private static RunExecution Start(Fixture f) => new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot())
            .StartTimedCombatSnapshot(1, PhaseSixA4Tests.Snapshot(f), Policy.Timing);
        private static DetachedCanonicalWriteResult Pump(Fixture f, long target) => PhaseSixA4Tests.Writer(f).AdvanceForeground(
            f.ActivePath, f.FileSystem, f.Session, f.Runtime, Policy, target, 10, (save, tick) => { }, 1000);
        private static void Through(Fixture f, long target)
        {
            for (int i = 0; i < 100; i++) {
                f.Accept(Pump(f, target));
                if (f.Runtime.livingDungeon.IsClosed && f.Runtime.livingDungeon.Scheduler.AcceptedThrough == target) return;
            }
            Assert.Fail("bounded test pump did not close");
        }
        private static long Admitted(Fixture f)
        {
            Through(f, 0); long arrival = f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value;
            Through(f, arrival); return arrival;
        }
        private static long Finish(Fixture f)
        {
            var p = f.Runtime.livingDungeon.Parties.Single();
            var e = LivingDungeonContracts.Restore(p, Policy.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized));
            try { e.DrainToCompletion(); return checked(p.AdmittedAt + e.Inspect().LogicalTime); } finally { e.Abandon(); }
        }

        [Test] public void AcceptedButUnclosedHorizonReopensWithoutDroppingTimeOrDuplicatingHistory()
        {
            var f = Dungeon(); f.Accept(Pump(f, 1800000));
            Assert.That(f.Runtime.livingDungeon.IsClosed, Is.False);
            Assert.That(f.Runtime.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(1800000));
            f.Reopen(); Through(f, 1800000);
            Assert.That(f.Runtime.totalTicks, Is.EqualTo(180));
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Select(r => r.RunId).Distinct().Count(), Is.EqualTo(f.Runtime.runHistory.RecentOutcomes.Length));
        }

        [TestCase(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush, 1)]
        [TestCase(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 1)]
        [TestCase(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write, 2)]
        public void FailedTerminalPumpPublishesNothingAndProvenRollbackRetriesExactlyOnce(Gd66DetachedSpatialMigrationTransactionTests.OperationType op, int occurrence)
        {
            var f = Dungeon(); Admitted(f); long finish = Finish(f);
            var before = f.Session.GetCurrentBytes(); string state = B.Structured(f.Runtime);
            f.FileSystem.EnableFailure(op, occurrence); var result = Pump(f, finish); f.FileSystem.DisableFailure();
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.RuntimeProjection, Is.Null);
            Assert.That(result.Session, Is.Null); CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(B.Structured(f.Runtime), Is.EqualTo(state)); f.Reopen(); Through(f, finish);
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(r => r.RunId == "run-1"), Is.EqualTo(1));
        }

        [Test] public void UnprovenRollbackRequiresRecoveryAndDoesNotPublishTerminalEffects()
        {
            var f = Dungeon(); Admitted(f); long finish = Finish(f); var old = f.Runtime;
            f.FileSystem.EnableFailureSequence(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush, 1,
                Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 2);
            var failed = Pump(f, finish); f.FileSystem.DisableFailure();
            Assert.That(failed.IsSuccess, Is.False); Assert.That(failed.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
            Assert.That(failed.RuntimeProjection, Is.Null); Assert.That(f.Runtime, Is.SameAs(old));
            Assert.That(f.Runtime.runHistory.LatestOutcome?.RunId, Is.Not.EqualTo("run-1"));
        }

        [Test] public void CurrentRecoveryBeforeSettlementIsChargedOnceAndDamageIsThenAppliedToRecoveredIdentity()
        {
            var f = Dungeon(); CombatC2Tests.SetState(f, 2, false, 0); Admitted(f);
            var start = f.Runtime.livingDungeon.Parties.Single().Checkpoint;
            // Authorized condition change during occupancy, representing a completed
            // manual action. The foreground producer cannot recover an occupied floor.
            CombatC2Tests.SetState(f, 3, false, 100);
            Through(f, f.Runtime.livingDungeon.Scheduler.ClosedThrough + 1);
            CollectionAssert.AreEqual(start, f.Runtime.livingDungeon.Parties.Single().Checkpoint);
            long finish = Finish(f); Through(f, finish);
            var m = f.Runtime.runHistory.LatestOutcome.CombatMonsters.Single();
            int damage = m.InitialHealth - m.Health;
            // Vacant recovery uses the existing configured living-first resolver.
            int ready = MonsterRecoveryRules.Capture(f.Configuration).Readiness(m.OptionId);
            Assert.That(f.Runtime.monsterContinuity.Records.Single().Health, Is.EqualTo(ready));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(100 - (ready - Math.Max(0, 3 - damage))));
            double wallet = f.Runtime.structureRuntime.ManaReserve; f.Reopen(); Through(f, finish);
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(wallet));
        }

        [Test] public void MalformedProductionConfigurationRejectsOverflowAndUnsupportedProfile()
        {
            string json = File.ReadAllText("Assets/_Project/Resources/living_dungeon_foreground.json");
            Assert.Throws<ArgumentException>(() => LivingDungeonForegroundConfig.Parse(System.Text.Encoding.UTF8.GetBytes(json.Replace("1000", "2147483648"))));
            Assert.Throws<ArgumentException>(() => LivingDungeonForegroundConfig.Parse(System.Text.Encoding.UTF8.GetBytes(json.Replace(LivingDungeonForegroundConfig.ProfileId, "unknown"))));
        }

        [Test] public void PublishedPacingIsExplicitApprovedAndSeparateFromQualificationProfile()
        {
            var p = Policy; Assert.That(p.UnitsPerSecond, Is.EqualTo(1000));
            Assert.That(p.Timing.UnitsPerTileEdge, Is.EqualTo(1000)); Assert.That(p.Timing.MonsterInteractionUnits, Is.EqualTo(3000));
            Assert.That(p.Timing.TrapInteractionUnits, Is.EqualTo(1000)); Assert.That(p.Timing.LootInteractionUnits, Is.EqualTo(1000));
            Assert.That(new[] { p.Interval("low"), p.Interval("building"), p.Interval("steady"), p.Interval("heavy"), p.Interval("dangerous_churn") },
                Is.EqualTo(new long[] { 90000, 60000, 45000, 30000, 60000 }));
            Assert.That(p.Interval("none"), Is.Zero);
            Assert.That(p.CreateProfile(default).Id, Is.Not.EqualTo("living.qualification.profile_a.v1"));
        }

        [Test] public void ActualAdmissionRetainsAnUnfinishedTimedC2PrefixThenSettlesAndRetiresExactlyOnce()
        {
            var f = Dungeon(); Through(f, 0);
            long arrival = f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value;
            Assert.That(arrival, Is.InRange(30000, 90000));
            Through(f, arrival - 1); Assert.That(f.Runtime.livingDungeon.Parties, Is.Empty);
            Through(f, arrival); var party = f.Runtime.livingDungeon.Parties.Single();
            Assert.That(party.Sequence, Is.EqualTo(1)); Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(2));
            Assert.That(f.Runtime.runHistory.LatestOutcome?.RunId, Is.Not.EqualTo("run-1"));
            var e = LivingDungeonContracts.Restore(party, Policy.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized));
            Assert.That(e.Combat.Kernel.RulesId, Is.EqualTo(MonsterRecoveryRules.Executable)); Assert.That(e.IsTerminal, Is.False);
            long finish; try { e.DrainToCompletion(); finish = arrival + e.Inspect().LogicalTime; } finally { e.Abandon(); }
            Through(f, finish); Assert.That(f.Runtime.livingDungeon.Parties, Is.Empty);
            Assert.That(f.Runtime.runHistory.LatestOutcome.RunId, Is.EqualTo("run-1"));
            var state = B.Structured(f.Runtime); var bytes = f.Session.GetCurrentBytes(); f.Reopen(); Through(f, finish);
            CollectionAssert.AreEqual(bytes, f.Session.GetCurrentBytes()); Assert.That(B.Structured(f.Runtime), Is.EqualTo(state));
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(r => r.RunId == "run-1"), Is.EqualTo(1));
        }

        [Test] public void FirstOpportunityAndAllOutcomesAreIndependentOfForegroundPartitionAndReopen()
        {
            var direct = Dungeon(); var split = Dungeon();
            Through(direct, 120000);
            foreach (long t in new long[] { 0, 1, 9999, 10000, 10001, 29999, 30000, 30001, 45000, 60000, 89999, 90000, 120000 }) {
                Through(split, t); split.Reopen();
            }
            CollectionAssert.AreEqual(direct.Session.GetCurrentBytes(), split.Session.GetCurrentBytes());
            Assert.That(B.Structured(direct.Runtime), Is.EqualTo(B.Structured(split.Runtime)));
        }

        [TestCase("unchanged")] [TestCase("recovered")] [TestCase("injured")] [TestCase("revival")]
        public void CapturedDefeatAppliesNetDamageToCurrentConditionWithoutHistoricalOverwrite(string action)
        {
            var f = Dungeon(); CombatC2Tests.SetState(f, 2, false, 0);
            var e = Start(f); var outcome = e.DrainToCompletion(); var monster = outcome.CombatMonsters.Single();
            Assert.That(monster.Health, Is.Zero); int damage = monster.InitialHealth - monster.Health;
            if (action == "recovered") CombatC2Tests.SetState(f, 8, false, 0);
            if (action == "injured") CombatC2Tests.SetState(f, 1, false, 0);
            if (action == "revival") CombatC2Tests.SetState(f, 3, true, 0);
            var before = f.Session.GetCurrentBytes(); var current = f.Runtime.monsterContinuity.Records.Single().Copy();
            var applied = LivingDungeonForegroundReconciliation.Apply(e, outcome, f.Runtime.monsterContinuity, f.State, MonsterRecoveryRules.Capture(f.Configuration));
            var r = applied.Records.Single(); Assert.That(r.Health, Is.EqualTo(Math.Max(0, current.Health - damage)));
            Assert.That(r.RevivalRequired, Is.EqualTo(r.Health == 0 || current.RevivalRequired));
            if (action == "recovered") { Assert.That(r.Health, Is.EqualTo(6)); Assert.That(r.RevivalRequired, Is.False); }
            CollectionAssert.AreEqual(before, f.Session.GetCurrentBytes()); Assert.That(f.Runtime.monsterContinuity.Records.Single().Health, Is.EqualTo(current.Health));
        }

        [TestCase(false)] [TestCase(true)]
        public void UnassignedAndRedeployedIdentitiesKeepDamageWithoutDuplicatingOwnership(bool redeploy)
        {
            var f = Dungeon(); var e = Start(f); var outcome = e.DrainToCompletion(); var m = outcome.CombatMonsters.Single();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(m.AssignmentId)));
            if (redeploy) f.Accept(f.Execute(DetachedCanonicalMutationRequest.Redeploy(m.AssignmentId,
                f.State.Floors[0].Layout.Rooms[0].RoomInstanceId, null, new TileCoordinate(3, 2))));
            var applied = LivingDungeonForegroundReconciliation.Apply(e, outcome, f.Runtime.monsterContinuity, f.State, MonsterRecoveryRules.Capture(f.Configuration));
            Assert.That(applied.Records.Single().AssignmentId, Is.EqualTo(m.AssignmentId));
            Assert.That(applied.Records.Single().Health, Is.EqualTo(Math.Max(0, f.Runtime.monsterContinuity.Records.Single().Health - m.InitialHealth + m.Health)));
        }

        [Test] public void RetiredCapturedIdentityIsNeverRecreatedAndReplacementIsUntouched()
        {
            var f = Dungeon(); var e = Start(f); var outcome = e.DrainToCompletion(); var m = outcome.CombatMonsters.Single();
            // Controlled valid post-retirement ownership; no new player retirement action.
            var spatial = JsonUtility.FromJson<DetachedCanonicalSpatialSaveState>(JsonUtility.ToJson(f.State));
            spatial.Floors[0].RoomContents.Assignments = spatial.Floors[0].RoomContents.Assignments.Where(a => a.AssignmentId != m.AssignmentId).ToArray();
            var retired = MonsterContinuity.Reconcile(f.Runtime.monsterContinuity, f.State, spatial, MonsterRecoveryRules.Capture(f.Configuration));
            Assert.That(retired.Records, Is.Empty);
            Assert.That(LivingDungeonForegroundReconciliation.Apply(e, outcome, retired, spatial, MonsterRecoveryRules.Capture(f.Configuration)).Records, Is.Empty);
            Assert.That(f.Runtime.monsterContinuity.Records.Single().AssignmentId, Is.EqualTo(m.AssignmentId));
        }

        [Test] public void ActualActiveRunAllowsSameFloorDraftCommitAndUsesSnapshotUntilSettlement()
        {
            var f = Dungeon(); Through(f, 0); long arrival = f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value; Through(f, arrival);
            var bytes = f.Runtime.livingDungeon.Parties.Single().Checkpoint;
            var id = f.Runtime.monsterContinuity.Records.Single().AssignmentId;
            var draft = TransactionalDungeonDraft.Create(f.State, PhaseSevenA4TransactionalEditorTests.Context(f), new PhaseSevenA4TransactionalEditorTests.Store());
            Assert.That(draft.Move(f.State.Floors[0].FloorInstanceId, f.State.Floors[0].Layout.Rooms[0].RoomInstanceId, id, new TileCoordinate(3, 2)), Is.True);
            while (draft.Durability == DraftDurability.Pending) Assert.That(draft.FlushNext(), Is.True);
            f.Accept(PhaseSixA4Tests.Writer(f).CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, draft));
            CollectionAssert.AreEqual(bytes, f.Runtime.livingDungeon.Parties.Single().Checkpoint);
            var restored = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties.Single(), Policy.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized));
            long finish; try { restored.DrainToCompletion(); finish = arrival + restored.Inspect().LogicalTime; } finally { restored.Abandon(); }
            Through(f, finish);
            Assert.That(f.State.Floors[0].RoomContents.Assignments.Single(a => a.AssignmentId == id).RoomLocalPosition, Is.EqualTo(new TileCoordinate(3, 2)));
            Assert.That(f.Runtime.runHistory.LatestOutcome.RunId, Is.EqualTo("run-1"));
        }

        private sealed class Utc : ITimeSource { public long UtcNowUnixSeconds() => 1000; }
        [Test] public void SharedMonotonicClockPreservesWholeUnitsAcrossPartitionFocusPauseAndRestart()
        {
            double now = 100;
            var time = new TimeService(new SimpleLogger(false), 10, 300, new Utc(), () => now);
            time.AttachSave(new SaveData()); time.BeginForeground(1000, 123);
            now = 101; Assert.That(time.SampleForeground(), Is.EqualTo(1123));
            time.SetForegroundFocus(false); now = 1000; time.SetForegroundFocus(true);
            Assert.That(time.SampleForeground(), Is.EqualTo(1123));
            now = 1001; time.OnPause(); now = 10000; time.SetForegroundFocus(false); time.OnResume(); time.SetForegroundFocus(true);
            Assert.That(time.SampleForeground(), Is.EqualTo(2123));
            now = 10001; Assert.That(time.SampleForeground(), Is.EqualTo(3123));
            var reopened = new TimeService(new SimpleLogger(false), 10, 300, new Utc(), () => now);
            reopened.AttachSave(new SaveData()); reopened.BeginForeground(1000, 3123);
            now += .001; Assert.That(reopened.SampleForeground(), Is.EqualTo(3124));
        }
    }
}
#endif
