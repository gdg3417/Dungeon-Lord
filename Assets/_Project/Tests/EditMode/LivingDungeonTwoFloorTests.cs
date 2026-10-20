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
        internal static Fixture Waiting()
        {
            var f = Dungeon(); var profile = Profile(f); var a = Start(f, 1); var b = Start(f, 2);
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
                f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(loot)));
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
    }
}
#endif
