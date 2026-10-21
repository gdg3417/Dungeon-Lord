#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class LivingDungeonTwoFloorTests
    {
        internal static LivingDungeonForegroundConfig Policy => LivingDungeonForegroundConfig.LoadTwoFloor();
        [Serializable] private sealed class HistoricalCheckpoint
        {
            public string Source, SourceSha256, StartIdentity, CheckpointIdentity, Base64;
            public int Format;
            public long NextOrdinal;
        }
        [Test] public void Pr245HistoricalFormatTwoGoldenRestoresAndReencodesExactOriginalBytes()
        {
            var frozen = JsonUtility.FromJson<HistoricalCheckpoint>(File.ReadAllText("Tools/Presentation/Fixtures/historical-c2-format2.json"));
            var bytes = Convert.FromBase64String(frozen.Base64);
            Assert.That(BitConverter.ToInt32(bytes, 0), Is.EqualTo(2));
            Assert.That(RunCheckpointCodec.Identity(bytes), Is.EqualTo(frozen.CheckpointIdentity));
            var profile = LivingDungeonForegroundConfig.Load().CreateProfile(SaveSpatialMigrationLimits.Canonical.ForSchema(13).Serialized);
            var restored = CombatCheckpointCodec.RestoreSupported(bytes, frozen.CheckpointIdentity, frozen.StartIdentity, frozen.NextOrdinal, profile.Checkpoints);
            Assert.That(restored.IsValid, Is.True, restored.FailureReason);
            try {
                Assert.That(restored.Execution.TransferContract, Is.False);
                CollectionAssert.AreEqual(bytes, restored.Execution.ExportCheckpoint(profile.Checkpoints).GetBytes());
                restored.Execution.DrainToCompletion();
            } finally { restored.Execution.Abandon(); }
        }
        internal static Fixture Dungeon(bool descend = true)
        {
            var f = PhaseSixA4Tests.Eligible(); f.Configuration = CombatC0IntegrationTests.Config();
            PhaseSixA4Tests.ForceDescent(f.Configuration, descend);
            foreach (var c in f.Configuration.PhaseFiveB.Classes) c.LevelOneMaxHealth = 100; // isolated survival fixture
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);
            PhaseSixA4Tests.AddContent(f, 1, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
            PhaseSixA4Tests.AddContent(f, 1, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);
            f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                FloorLifecycleAction.Activate, PhaseSixA4Tests.FloorTwo(f)));
            f.Profile = SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
            f.Context = new DetachedCurrentTargetValidationContext(f.Compatibility, f.Production,
                LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.Profile.Canonical, f.Occupancy,
                livingDungeonProfiles: new[] { Profile(f), LivingDungeonForegroundConfig.Load().CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized) },
                monsterRecovery: MonsterRecoveryRules.Capture(f.Configuration));
            f.Reopen(); return f;
        }
        internal static LivingDungeonProfile Profile(Fixture f) => Policy.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized);
        internal static RunExecution Start(Fixture f, int sequence = 1)
        {
            var e = new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot()).StartTimedCombatSnapshot(sequence, PhaseSixA4Tests.Snapshot(f), Policy.Timing);
            e.EnableTransfers(); return e;
        }
        internal static RunExecution Reopen(RunExecution e, LivingDungeonProfile profile)
        {
            var cp = e.ExportCheckpoint(profile.Checkpoints);
            var r = CombatCheckpointCodec.RestoreSupported(cp.GetBytes(), cp.Identity, cp.StartIdentity, cp.NextOrdinal, profile.Checkpoints, true);
            Assert.That(r.IsValid, Is.True, r.FailureReason);
            CollectionAssert.AreEqual(cp.GetBytes(), r.Execution.ExportCheckpoint(profile.Checkpoints).GetBytes()); return r.Execution;
        }
        internal static long ToTransfer(RunExecution e)
        {
            while (!e.IsTerminal) {
                long due = e.NextDueTime().Value;
                if (e.AnnouncedAction == RunStepKind.FloorTransfer) return due;
                e.AdvanceThrough(due, 1, e.NextOrdinal);
            }
            Assert.Fail("Fixture must choose authentic DESCEND"); return -1;
        }
        internal static void Through(Fixture f, long time)
        {
            for (int i = 0; i < 100; i++) {
                var result = PhaseSixA4Tests.Writer(f).AdvanceForeground(f.ActivePath, f.FileSystem, f.Session,
                    f.Runtime, Policy, time, 10, (save, tick) => { }, 1000);
                Assert.That(result.IsSuccess, Is.True, result.Reason); f.Accept(result);
                if (f.Runtime.livingDungeon.IsClosed && f.Runtime.livingDungeon.Scheduler.AcceptedThrough == time) return;
            }
            Assert.Fail("Fixture exceeded bounded pump limit");
        }
        [Test] public void AuthoredTimingAndFrozenEventIdentitiesRemainExplicit()
        {
            Assert.That((int)RunStepKind.Terminal, Is.EqualTo(12)); Assert.That((int)RunStepKind.FloorTransfer, Is.EqualTo(11));
            Assert.That((int)RunStepKind.FloorTransferBlocked, Is.EqualTo(13)); Assert.That((int)RunStepKind.FloorTransferReleased, Is.EqualTo(14));
            Assert.That(Policy.Timing.FloorTransferUnits, Is.EqualTo(1000));
            Assert.That(LivingDungeonForegroundConfig.Load().Timing.FloorTransferUnits, Is.Zero);
        }
        [Test] public void BlockReleaseAndTransferReplayRetainSourceUntilCompletionWithoutCombatWork()
        {
            var f = Dungeon(); var profile = Profile(f); var e = Start(f);
            long b = ToTransfer(e), ordinal = e.NextOrdinal, work = e.CommittedCombatWork;
            string start = e.StartIdentity, source = e.OccupiedFloor, destination = e.TransferDestination;
            e.BlockTransfer(b, 2);
            Assert.That(e.NextOrdinal, Is.EqualTo(ordinal + 1)); Assert.That(e.OccupiedFloor, Is.EqualTo(source)); Assert.That(e.NextDueTime(), Is.Null);
            e = Reopen(e, profile); Assert.That(e.TransferWaiting, Is.True); Assert.That(e.StartIdentity, Is.EqualTo(start));
            e.ReleaseTransfer(b + 5000, 2, 40, b + 5000);
            Assert.That(e.OccupiedFloor, Is.EqualTo(source)); Assert.That(e.CommittedCombatWork, Is.EqualTo(work));
            e = Reopen(e, profile); Assert.That(e.NextDueTime(), Is.EqualTo(b + 5000));
            e.AdvanceThrough(b + 5000, 1, e.NextOrdinal);
            Assert.That(e.NextOrdinal, Is.EqualTo(ordinal + 3)); Assert.That(e.OccupiedFloor, Is.EqualTo(destination));
            Assert.That(e.Inspect().LastStep.Value.LogicalTime, Is.EqualTo(b + 5000));
            e = Reopen(e, profile); Assert.That(e.StartIdentity, Is.EqualTo(start));
            Assert.That(e.TransferGate.Deadline, Is.EqualTo(b)); e.Abandon();
        }
        [TestCase(false)] [TestCase(true)] public void LifetimeAssuranceUsesAuthenticExitOrDescentAndFitsAllPrefixes(bool descend)
        {
            var f = Dungeon(descend); var e = Start(f); LivingDungeonLifetimeAssurance.Check(e, Profile(f));
            Assert.That(e.IsTerminal, Is.True); Assert.That(e.NextOrdinal, Is.LessThan(Profile(f).MaximumPartyReplay)); e.Abandon();
        }
        [Test] public void AutomaticTwoFloorAdmissionCompletesAndRetiresExactlyOnce()
        {
            var f = Dungeon(); Through(f, 0); long a = f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value;
            Through(f, a); Assert.That(f.Runtime.livingDungeon.Parties.Count, Is.EqualTo(1));
            var p = f.Runtime.livingDungeon.Parties.Single(); var e = LivingDungeonContracts.Restore(p, Profile(f));
            long b = ToTransfer(e); Through(f, a + b);
            var actual = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties.Single(), Profile(f));
            Assert.That(actual.OccupiedFloor, Is.EqualTo(PhaseSixA4Tests.FloorTwo(f))); actual.Abandon();
            e.DrainToCompletion(); long finish = a + e.Inspect().LogicalTime; e.Abandon();
            f.Reopen(); Through(f, finish); Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(o => o.RunId == "run-1"), Is.EqualTo(1));
            var bytes = f.Session.GetCurrentBytes(); f.Reopen(); Through(f, finish); CollectionAssert.AreEqual(bytes, f.Session.GetCurrentBytes());
        }
        internal static Fixture Waiting(int? capturedBHealth = null)
        {
            var f = Dungeon(); var profile = Profile(f); var a = Start(f, 1);
            if (capturedBHealth.HasValue) {
                var condition = f.Runtime.monsterContinuity.Copy();
                string id = f.State.Floors[1].RoomContents.Assignments.Single(m => m.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId).AssignmentId;
                condition.Records.Single(m => m.AssignmentId == id).Health = capturedBHealth.Value;
                var updated = f.Session.PrepareLiveReplacement(DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile), f.State, monsterContinuity: condition);
                Assert.That(updated.IsSuccess, Is.True, updated.Reason); f.FileSystem.Seed(f.ActivePath, updated.Update.GetBytes()); f.Reopen();
            }
            var b = Start(f, 2);
            long deadline = ToTransfer(b); long aDeadline = ToTransfer(a); a.AdvanceThrough(aDeadline, 1, a.NextOrdinal);
            while (a.NextDueTime() <= deadline) a.AdvanceThrough(a.NextDueTime().Value, 1, a.NextOrdinal);
            b.BlockTransfer(deadline, 1);
            var owner = LivingDungeonState.Empty(); LivingDungeonScheduling.Activate(owner, profile, Policy.MinimumArrival, f.Runtime.runHistory);
            owner.Scheduler.AcceptedThrough = owner.Scheduler.ClosedThrough = deadline;
            foreach (var pair in new[] { (sequence: 1, execution: a), (sequence: 2, execution: b) }) {
                var p = new LivingDungeonParty { Sequence = pair.sequence, AdmittedAt = 0 };
                LivingDungeonScheduling.Pin(p, pair.execution, profile); owner.Parties.Add(p);
            }
            f.Runtime.runHistory.NextRunSequence = 3;
            var prepared = f.Session.PrepareLivingDungeonReplacement(DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile), f.State, owner);
            Assert.That(prepared.IsSuccess, Is.True, prepared.Reason);
            f.FileSystem.Seed(f.ActivePath, prepared.Update.GetBytes()); f.Reopen(); a.Abandon(); b.Abandon(); return f;
        }
        [Test] public void WaitingReopensAndRealRetirementReleasesItWithAuthenticatedCause()
        {
            var f = Waiting(); var before = f.Runtime.livingDungeon.Parties[1];
            var e = LivingDungeonContracts.Restore(before, Profile(f)); string start = e.StartIdentity;
            Assert.That(e.OccupiedFloor, Is.EqualTo(f.State.Floors[0].FloorInstanceId)); e.Abandon();
            var a = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties[0], Profile(f)); a.DrainToCompletion();
            long finish = a.Inspect().LogicalTime; a.Abandon(); f.Reopen(); Through(f, finish);
            var remaining = f.Runtime.livingDungeon.Parties.Single(p => p.Sequence == 2); e = LivingDungeonContracts.Restore(remaining, Profile(f));
            Assert.That(e.StartIdentity, Is.EqualTo(start)); Assert.That(e.OccupiedFloor, Is.EqualTo(PhaseSixA4Tests.FloorTwo(f)));
            Assert.That(e.TransferGate.VacancySequence, Is.EqualTo(1)); Assert.That(e.TransferGate.VacancyTime, Is.EqualTo(finish));
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(o => o.RunId == "run-1"), Is.EqualTo(1)); e.Abandon(); f.Reopen();
        }
        [Test] public void SelfConsistentReleaseWithoutVacancyCannotPublishThroughSession()
        {
            var f = Waiting(); var owner = f.Runtime.livingDungeon; var p = owner.Parties[1]; var e = LivingDungeonContracts.Restore(p, Profile(f));
            long r = owner.Scheduler.AcceptedThrough + 1; e.ReleaseTransfer(r, 1, 99, r); LivingDungeonScheduling.Pin(p, e, Profile(f));
            owner.Scheduler.AcceptedThrough = r;
            var before = f.Session.GetCurrentBytes();
            var rejected = f.Session.PrepareLivingDungeonReplacement(DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile), f.State, owner);
            Assert.That(rejected.IsSuccess, Is.False); CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath)); e.Abandon();
        }
        [Test] public void EmptyProjectionAndStaleLineageCannotAuthorizeRelease()
        {
            var f = Waiting(); var p = f.Runtime.livingDungeon.Parties[1]; var e = LivingDungeonContracts.Restore(p, Profile(f));
            var lineage = new LivingDungeonForegroundLineage(f.Session.GetCurrentBytes());
            Assert.Throws<InvalidOperationException>(() => lineage.Release(f.Session.GetCurrentBytes(), p, e,
                f.Runtime.livingDungeon.Scheduler.AcceptedThrough + 1, new System.Collections.Generic.HashSet<string>(), Profile(f)));
            Assert.Throws<InvalidOperationException>(() => lineage.Release(new byte[] { 0 }, p, e,
                f.Runtime.livingDungeon.Scheduler.AcceptedThrough + 1, new System.Collections.Generic.HashSet<string>(), Profile(f))); e.Abandon();
        }
        [TestCase(false)] [TestCase(true)] public void HistoricalV1DrainsOnceAndUpgradesWithoutEligibility(bool removeLoot)
        {
            var f = Dungeon(); string second = PhaseSixA4Tests.FloorTwo(f);
            f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Deactivate, second));
            var oldPolicy = LivingDungeonForegroundConfig.Load(); var oldProfile = oldPolicy.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized);
            var e = new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot()).StartTimedCombatSnapshot(1, PhaseSixA4Tests.Snapshot(f), oldPolicy.Timing);
            var owner = LivingDungeonState.Empty(); LivingDungeonScheduling.Activate(owner, oldProfile, oldPolicy.MinimumArrival, f.Runtime.runHistory);
            owner.Scheduler.AcceptedThrough = 0;
            var party = new LivingDungeonParty { Sequence = 1, AdmittedAt = 0 }; LivingDungeonScheduling.Pin(party, e, oldProfile); owner.Parties.Add(party);
            f.Runtime.runHistory.NextRunSequence = 2;
            var prepared = f.Session.PrepareLivingDungeonReplacement(DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile), f.State, owner);
            Assert.That(prepared.IsSuccess, Is.True, prepared.Reason); f.FileSystem.Seed(f.ActivePath, prepared.Update.GetBytes()); f.Reopen(); Through(f, 0);
            string identity = f.Runtime.livingDungeon.Parties.Single().StartIdentity;
            long? opportunity = f.Runtime.livingDungeon.Scheduler.NextOpportunityTime;
            f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, second));
            if (removeLoot) {
                string loot = f.State.Floors[0].RoomContents.Assignments.Single(a => a.CategoryId == MvpDungeonPlacementIds.LootNodeCategoryId).AssignmentId;
                f.Accept(PhaseSixA4Tests.Writer(f).Execute(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                    DetachedCanonicalMutationRequest.Unassign(loot, f.State.Floors[0].FloorInstanceId)));
            }
            Assert.That(f.Runtime.livingDungeon.Parties.Single().StartIdentity, Is.EqualTo(identity));
            e.DrainToCompletion(); long finish = e.Inspect().LogicalTime; e.Abandon(); f.Reopen(); Through(f, finish);
            Assert.That(f.Runtime.livingDungeon.Scheduler.ProfileId, Is.EqualTo(Policy.Id));
            Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(2));
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(o => o.RunId == "run-1"), Is.EqualTo(1));
            Assert.That(f.Runtime.livingDungeon.Scheduler.NextOpportunityTime, Is.EqualTo(opportunity));
            var bytes = f.Session.GetCurrentBytes(); f.Reopen(); Through(f, finish); CollectionAssert.AreEqual(bytes, f.Session.GetCurrentBytes());
        }
        [Test] public void CompletedForgedReleaseCannotRetireItsBlockerOrBypassVacancyPermit()
        {
            var f = Waiting(); var owner = f.Runtime.livingDungeon; var p = owner.Parties[1]; var e = LivingDungeonContracts.Restore(p, Profile(f));
            long r = owner.Scheduler.AcceptedThrough + 1; e.ReleaseTransfer(r, 1, 99, r); e.AdvanceThrough(r, 1, e.NextOrdinal);
            LivingDungeonScheduling.Pin(p, e, Profile(f)); owner.Parties.RemoveAt(0); owner.Scheduler.AcceptedThrough = r;
            var before = f.Session.GetCurrentBytes();
            var rejected = f.Session.PrepareLivingDungeonReplacement(DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile), f.State, owner);
            Assert.That(rejected.IsSuccess, Is.False); Assert.That(rejected.Reason, Is.EqualTo("living.transfer.vacancy_unproven"));
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath)); e.Abandon();
        }
        [Test] public void WaitingProtectsBothFloorsFromRecoveryAndThrottlesAdmissionAfterReopen()
        {
            var f = Waiting(); var injured = f.Runtime.monsterContinuity.Copy();
            foreach (var m in injured.Records) { m.Health = 1; m.RevivalRequired = false; }
            var owner = f.Runtime.livingDungeon; long due = owner.Scheduler.ClosedThrough + 1;
            LivingDungeonScheduling.Install(owner, 0, due); // explicit first-opportunity test input
            var prepared = f.Session.PrepareForegroundReplacement(DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile),
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context), owner, injured);
            Assert.That(prepared.IsSuccess, Is.True, prepared.Reason); f.FileSystem.Seed(f.ActivePath, prepared.Update.GetBytes()); f.Reopen();
            Through(f, due); Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(3));
            Assert.That(f.Runtime.livingDungeon.Parties.Count, Is.EqualTo(2));
            Assert.That(f.Runtime.monsterContinuity.Records.All(m => m.Health == 1), Is.True);
            var bytes = f.Session.GetCurrentBytes(); f.Reopen(); Through(f, due); CollectionAssert.AreEqual(bytes, f.Session.GetCurrentBytes());
        }
        [Test] public void CurrentDeactivationDoesNotFabricateVacancyOrRewriteCapturedRoute()
        {
            var f = Waiting(); string identity = f.Runtime.livingDungeon.Parties[1].StartIdentity;
            f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                FloorLifecycleAction.Deactivate, PhaseSixA4Tests.FloorTwo(f))); f.Reopen();
            var b = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties[1], Profile(f));
            Assert.That(b.TransferWaiting, Is.True); Assert.That(b.OccupiedFloor, Is.EqualTo(f.State.Floors[0].FloorInstanceId)); b.Abandon();
            var a = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties[0], Profile(f)); a.DrainToCompletion();
            long finish = a.Inspect().LogicalTime; a.Abandon(); Through(f, finish);
            b = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties.Single(p => p.Sequence == 2), Profile(f));
            Assert.That(b.StartIdentity, Is.EqualTo(identity)); Assert.That(b.OccupiedFloor, Is.EqualTo(PhaseSixA4Tests.FloorTwo(f))); b.Abandon();
        }
        [TestCase(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write, 2)]
        [TestCase(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 1)]
        [TestCase(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush, 1)]
        public void FailedReleaseTransactionPublishesNoSettlementOrTransferAndRetryIsExactlyOnce(
            Gd66DetachedSpatialMigrationTransactionTests.OperationType operation, int occurrence)
        {
            var f = Waiting(); var a = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties[0], Profile(f));
            a.DrainToCompletion(); long finish = a.Inspect().LogicalTime; a.Abandon();
            var bytes = f.Session.GetCurrentBytes(); var live = JsonUtility.ToJson(f.Runtime);
            f.FileSystem.EnableFailure(operation, occurrence);
            var failed = PhaseSixA4Tests.Writer(f).AdvanceForeground(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                Policy, finish, 10, (save, tick) => { }, 1000); f.FileSystem.DisableFailure();
            Assert.That(failed.IsSuccess, Is.False); Assert.That(failed.RuntimeProjection, Is.Null);
            CollectionAssert.AreEqual(bytes, f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(live));
            f.Reopen(); Through(f, finish);
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(o => o.RunId == "run-1"), Is.EqualTo(1));
            var b = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties.Single(p => p.Sequence == 2), Profile(f));
            Assert.That(b.TransferGate.ReleaseTime, Is.EqualTo(finish)); b.Abandon();
        }
        [Test] public void WaitingReleaseIsEquivalentAcrossForegroundPartitionsAndReopen()
        {
            var direct = Waiting(); var split = Waiting();
            var a = LivingDungeonContracts.Restore(direct.Runtime.livingDungeon.Parties[0], Profile(direct)); a.DrainToCompletion();
            long finish = a.Inspect().LogicalTime; a.Abandon();
            Through(direct, finish);
            for (long time = split.Runtime.livingDungeon.Scheduler.ClosedThrough + 1; time < finish; time += 997) { Through(split, time); split.Reopen(); }
            Through(split, finish);
            string Stable(Fixture f) => Encoding.UTF8.GetString(f.Session.GetCurrentBytes()).Replace(
                "\"livingDungeon\":{\"FormatVersion\":1,\"Revision\":" + f.Runtime.livingDungeon.Revision + ",",
                "\"livingDungeon\":{\"FormatVersion\":1,\"Revision\":0,");
            Assert.That(Stable(split), Is.EqualTo(Stable(direct)));
        }
        [TestCase(false)] [TestCase(true)] public void OverlappingSnapshotsPreservePriorDamageAndPartialRecoveryAtBothSettlements(bool partial)
        {
            var f = Waiting(partial ? 2 : (int?)null); var profile = Profile(f);
            string id = f.State.Floors[1].RoomContents.Assignments.Single(m => m.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId).AssignmentId;
            var b = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties[1], profile);
            int captured = b.Snapshot.MonsterStarts.Records.Single(m => m.AssignmentId == id).Health;
            string start = b.StartIdentity; b.Abandon();
            if (partial) Fund(f, 5); // test-only available balance; real vacant-floor recovery spends it
            var a = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties[0], profile); var outcomeA = a.DrainToCompletion();
            long aFinish = a.Inspect().LogicalTime; a.Abandon(); Through(f, aFinish);
            Assert.That(outcomeA.CombatMonsters.Single(m => m.AssignmentId == id).Health, Is.Zero);
            Assert.That(f.Runtime.monsterContinuity.Records.Single(m => m.AssignmentId == id).Health, Is.EqualTo(partial ? 5 : 7));
            b = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties.Single(p => p.Sequence == 2), profile);
            Assert.That(b.StartIdentity, Is.EqualTo(start));
            Assert.That(b.Snapshot.MonsterStarts.Records.Single(m => m.AssignmentId == id).Health, Is.EqualTo(captured));
            Fund(f, 0); var before = f.Runtime.monsterContinuity.Copy();
            var outcomeB = b.DrainToCompletion(); long bFinish = b.Inspect().LogicalTime; b.Abandon();
            var expected = before.Records.ToDictionary(m => m.AssignmentId, m => m.Health);
            foreach (var monster in outcomeB.CombatMonsters)
                expected[monster.AssignmentId] = Math.Max(0, expected[monster.AssignmentId] - monster.InitialHealth + monster.Health);
            f.Reopen(); Through(f, bFinish);
            foreach (var monster in f.Runtime.monsterContinuity.Records) Assert.That(monster.Health, Is.EqualTo(expected[monster.AssignmentId]));
            if (partial) { Assert.That(expected[id], Is.EqualTo(3)); Assert.That(f.Runtime.monsterContinuity.Records.Single(m => m.AssignmentId == id).RevivalRequired, Is.True); }
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Select(o => o.RunId), Is.EqualTo(new[] { "run-1", "run-2" }));
            Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(3));
            var bytes = f.Session.GetCurrentBytes(); f.Reopen(); Through(f, bFinish); CollectionAssert.AreEqual(bytes, f.Session.GetCurrentBytes());
        }
        private static void Fund(Fixture f, double wallet)
        {
            var current = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(f.Runtime)); current.structureRuntime.ManaReserve = wallet;
            if (f.Runtime.runHistory.LatestOutcome == null) current.runHistory.LatestOutcome = null;
            if (f.Runtime.runHistory.RecentOutcomes == null) current.runHistory.RecentOutcomes = null;
            f.Accept(PhaseSixA4Tests.Writer(f).SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, current));
        }
        [Test] public void CorruptFormatThreeReturnsFailureWithoutThrowingOrPublishing()
        {
            var f = Dungeon(); var e = Start(f); long b = ToTransfer(e); e.BlockTransfer(b, 2);
            var cp = e.ExportCheckpoint(Profile(f).Checkpoints); var bytes = cp.GetBytes(); bytes[bytes.Length - 1] ^= 1;
            Assert.DoesNotThrow(() => {
                var failed = CombatCheckpointCodec.RestoreSupported(bytes, RunCheckpointCodec.Identity(bytes), cp.StartIdentity, cp.NextOrdinal, Profile(f).Checkpoints);
                Assert.That(failed.IsValid, Is.False);
            });
            e.Abandon();
        }
        [Test] public void ExportProductionTwoFloorStandaloneInputsUsingApprovedContentAndTiming()
        {
            var f = PhaseSixA4Tests.Eligible();
            Assert.That(BootstrapConfigValidationService.TryParseRunSimulationConfig(
                File.ReadAllText("Assets/_Project/Data/Bootstrap/run_simulation_config.json"), out f.Configuration), Is.True);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);
            string second = PhaseSixA4Tests.FloorTwo(f);
            foreach (var anchor in new[] { new TileCoordinate(1, 8), new TileCoordinate(7, 8) }) {
                var preview = StructuralEditService.Preview(f.State, new StructuralConstructionRequest { FloorInstanceId = second,
                    RoomDefinitionId = "spatial.room.basic", Anchor = anchor, Orientation = CardinalOrientation.Zero,
                    TerminalConnectionPointId = "east" }, f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
                Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
                f.Accept(PhaseSixA4Tests.Writer(f).Execute(f.ActivePath, f.FileSystem, f.Session, f.Runtime, DetachedCanonicalMutationRequest.Construct(preview)));
            }
            var rooms = f.State.Floors[1].Layout.Rooms;
            foreach (var room in rooms)
                foreach (var position in new[] { new TileCoordinate(0, 0), new TileCoordinate(3, 3) })
                    f.Accept(PhaseSixA4Tests.Writer(f).Execute(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                        DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId,
                            room.RoomInstanceId, second, position)));
            f.Accept(PhaseSixA4Tests.Writer(f).Execute(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId,
                    rooms.Last().RoomInstanceId, second, new TileCoordinate(1, 1))));
            f.Profile = SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
            f.Context = new DetachedCurrentTargetValidationContext(f.Compatibility, f.Production,
                LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.Profile.Canonical, f.Occupancy,
                livingDungeonProfiles: new[] { Profile(f), LivingDungeonForegroundConfig.Load().CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized) },
                monsterRecovery: MonsterRecoveryRules.Capture(f.Configuration)); f.Reopen();
            string directory = Path.GetFullPath("TestResults/living-dungeon-two-floor-20261020/inputs"); Directory.CreateDirectory(directory);
            var utc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); f.Runtime.lastSavedUtcUnix = utc; f.Runtime.lastPausedUtcUnix = utc;
            f.Accept(PhaseSixA4Tests.Writer(f).SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
            WriteSeed(f, directory, "owner-inactive", null, 0);
            f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, second));
            int descending = 0; long longest = 0;
            for (int sequence = 1; sequence <= 32; sequence++) {
                var e = Start(f, sequence); long transfer = -1;
                try {
                    while (!e.IsTerminal) {
                        var step = e.AdvanceThrough(e.NextDueTime().Value, 1, e.NextOrdinal).Single();
                        if (step.Kind == RunStepKind.FloorTransfer) transfer = step.LogicalTime;
                        e.ExportCheckpoint(Profile(f).Checkpoints);
                    }
                    if (transfer >= 0) { descending++; longest = Math.Max(longest, e.Inspect().LogicalTime - transfer); }
                } finally { e.Abandon(); }
            }
            Assert.That(descending, Is.GreaterThan(0), "The unchanged production decision must genuinely choose DESCEND");
            Assert.That(longest, Is.GreaterThan(Policy.MinimumArrival), "Author legal content long enough for concurrent waiting");
            WriteSeed(f, directory, "two-floor", null, 0, descending, longest);
            f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Deactivate, second));
            var oldPolicy = LivingDungeonForegroundConfig.Load(); var oldProfile = oldPolicy.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized);
            var old = new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot()).StartTimedCombatSnapshot(1, PhaseSixA4Tests.Snapshot(f), oldPolicy.Timing);
            while (old.NextDueTime() == 0) old.AdvanceThrough(0, 1, old.NextOrdinal);
            var owner = LivingDungeonState.Empty(); LivingDungeonScheduling.Activate(owner, oldProfile, oldPolicy.MinimumArrival, f.Runtime.runHistory);
            owner.Scheduler.AcceptedThrough = owner.Scheduler.ClosedThrough = 0;
            var party = new LivingDungeonParty { Sequence = 1, AdmittedAt = 0 }; LivingDungeonScheduling.Pin(party, old, oldProfile); owner.Parties.Add(party);
            f.Runtime.runHistory.NextRunSequence = 2;
            var prepared = f.Session.PrepareLivingDungeonReplacement(DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile), f.State, owner);
            Assert.That(prepared.IsSuccess, Is.True, prepared.Reason); f.FileSystem.Seed(f.ActivePath, prepared.Update.GetBytes()); f.Reopen();
            f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, second));
            WriteSeed(f, directory, "v1", old.StartIdentity, old.NextOrdinal); old.Abandon();
            TestContext.WriteLine("Production input descents=" + descending + "/32 longest Floor2 units=" + longest);
        }
        private static void WriteSeed(Fixture f, string directory, string variant, string historicalStart, long historicalOrdinal,
            int descents = 0, long longest = 0)
        {
            string id = variant + "-" + Guid.NewGuid().ToString("N"); string path = Path.Combine(directory, id + ".save.json");
            var bytes = f.Session.GetCurrentBytes();
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write)) stream.Write(bytes, 0, bytes.Length);
            var manifest = new TwoFloorSeedManifest { Product = Application.productName,
                SaveFileName = "living-dungeon-b3b2-qualification-two-floor-" + id + ".json",
                SeedPath = path, SeedSha256 = RunCheckpointCodec.Identity(bytes), HistoricalStartIdentity = historicalStart,
                HistoricalOrdinal = historicalOrdinal, DescendingSamples = descents, LongestFloorTwoDuration = longest };
            using (var stream = new FileStream(Path.Combine(directory, id + ".manifest.json"), FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream)) writer.Write(JsonUtility.ToJson(manifest, true));
        }
    }
}
#endif
