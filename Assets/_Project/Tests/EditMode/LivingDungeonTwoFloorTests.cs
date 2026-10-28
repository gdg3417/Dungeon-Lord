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
        // Frozen pre-correction format-3 composition, kept independent of the
        // optimized inspection hand-off. Compare every live/reopened prefix.
        private static byte[] OriginalTransferInspection(RunExecution e) => RunCheckpointCodec.Bytes(w => {
            RunCheckpointCodec.Text(w, TransferCheckpointCodec.Evidence);
            RunCheckpointCodec.Blob(w, CombatCheckpointCodec.Inspection(e));
            RunCheckpointCodec.Text(w, e.OccupiedFloor); w.Write(e.TransferWaiting); w.Write(e.TransferReleased);
            w.Write(TransferCheckpointCodec.ControlBytes(e.TransferGate));
        });
        private static byte[] OriginalTransferFold(byte[] prefix, RunStep step, RunExecution e) =>
            RunCheckpointCodec.Hash(RunCheckpointCodec.Bytes(w => {
                RunCheckpointCodec.Text(w, TransferCheckpointCodec.Evidence);
                RunCheckpointCodec.Blob(w, CombatCheckpointCodec.Fold(prefix, step, e));
                RunCheckpointCodec.Blob(w, OriginalTransferInspection(e));
            }));
        private static byte[] OriginalTransferCheckpoint(RunExecution e) {
            var payload = RunCheckpointCodec.Bytes(w => {
                w.Write(3); RunCheckpointCodec.Text(w, TransferCheckpointCodec.Contract);
                RunCheckpointCodec.Text(w, TransferCheckpointCodec.Evidence);
                RunCheckpointCodec.Blob(w, e.StartInputs); w.Write(e.NextOrdinal); w.Write(e.Inspect().LogicalTime);
                w.Write(e.AcceptedDeadline); w.Write(e.IsTerminal); w.Write(e.OutcomeTaken);
                w.Write(TransferCheckpointCodec.ControlBytes(e.TransferGate));
                RunCheckpointCodec.Blob(w, e.PrefixDigest); RunCheckpointCodec.Blob(w, OriginalTransferInspection(e));
            });
            return RunCheckpointCodec.Bytes(w => { w.Write(payload); w.Write(RunCheckpointCodec.Hash(payload)); });
        }
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(true, true)]
        public void InspectionReusePreservesOriginalFoldAndCheckpointAtEveryPrefix(bool descend, bool waiting)
        {
            var f = Dungeon(descend); var profile = Profile(f); var e = Start(f); string start = e.StartIdentity;
            bool blocked = false;
            try {
                CollectionAssert.AreEqual(OriginalTransferCheckpoint(e), e.ExportCheckpoint(profile.Checkpoints).GetBytes());
                while (!e.IsTerminal) {
                    var prefix = (byte[])e.PrefixDigest.Clone(); RunStep step;
                    if (e.TransferWaiting) step = e.ReleaseTransfer(e.TransferGate.Deadline + 5000, 2, 40, e.TransferGate.Deadline + 5000);
                    else {
                        long due = e.NextDueTime().Value;
                        if (waiting && !blocked && e.AnnouncedAction == RunStepKind.FloorTransfer) {
                            step = e.BlockTransfer(due, 2); blocked = true;
                        } else step = e.AdvanceThrough(due, 1, e.NextOrdinal).Single();
                    }
                    CollectionAssert.AreEqual(OriginalTransferFold(prefix, step, e), e.PrefixDigest);
                    var cp = e.ExportCheckpoint(profile.Checkpoints);
                    CollectionAssert.AreEqual(OriginalTransferCheckpoint(e), cp.GetBytes());
                    var restored = CombatCheckpointCodec.RestoreSupported(cp.GetBytes(), cp.Identity, cp.StartIdentity, cp.NextOrdinal, profile.Checkpoints);
                    Assert.That(restored.IsValid, Is.True, restored.FailureReason);
                    CollectionAssert.AreEqual(cp.GetBytes(), restored.Execution.ExportCheckpoint(profile.Checkpoints).GetBytes());
                    restored.Execution.Abandon(); Assert.That(e.StartIdentity, Is.EqualTo(start));
                }
                Assert.That(blocked, Is.EqualTo(waiting));
                // Diagnostic only: equivalent folds of one frozen terminal view.
                // No gameplay work, timing or pass threshold is changed.
                var last = e.Inspect().LastStep.Value; var p = e.PrefixDigest; const int samples = 512;
                long work = e.CommittedCombatWork;
                var timer = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < samples; i++) OriginalTransferFold(p, last, e);
                timer.Stop(); double original = timer.Elapsed.TotalMilliseconds; timer.Restart();
                for (int i = 0; i < samples; i++) TransferCheckpointCodec.Fold(p, last, e);
                timer.Stop(); TestContext.WriteLine("Identical-view fold diagnostic: original_ms=" + original + " reused_ms=" + timer.Elapsed.TotalMilliseconds);
                Assert.That(e.CommittedCombatWork, Is.EqualTo(work));
            } finally { e.Abandon(); }
        }
        [Serializable] private sealed class HistoricalCheckpoint
        {
            public string Source, SourceSha256, StartIdentity, CheckpointIdentity, Base64;
            public int Format;
            public long NextOrdinal;
        }
        [TestCase("historical-b1-format1.json", 1)]
        [TestCase("historical-c2-format2.json", 2)]
        public void HistoricalGoldenRestoresAndReencodesExactPinnedBytes(string filename, int format)
        {
            var frozen = JsonUtility.FromJson<HistoricalCheckpoint>(File.ReadAllText("Tools/Presentation/Fixtures/" + filename));
            var bytes = Convert.FromBase64String(frozen.Base64);
            Assert.That(BitConverter.ToInt32(bytes, 0), Is.EqualTo(format)); Assert.That(frozen.Format, Is.EqualTo(format));
            Assert.That(RunCheckpointCodec.Identity(bytes), Is.EqualTo(frozen.CheckpointIdentity));
            var limits = SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
            var profile = LivingDungeonForegroundConfig.Load().CreateProfile(limits.Canonical.ForSchema(13).Serialized);
            var restored = CombatCheckpointCodec.RestoreSupported(bytes, frozen.CheckpointIdentity, frozen.StartIdentity, frozen.NextOrdinal, profile.Checkpoints);
            Assert.That(restored.IsValid, Is.True, restored.FailureReason);
            try {
                Assert.That(restored.Execution.TransferContract, Is.False);
                CollectionAssert.AreEqual(bytes, restored.Execution.ExportCheckpoint(profile.Checkpoints).GetBytes());
                restored.Execution.DrainToCompletion();
            } finally { restored.Execution.Abandon(); }
        }
        internal static Fixture Dungeon(bool descend = true, bool oneBoundary = false)
        {
            var inputs = PhaseFourTestSupport.AuthoredInputs((config, _) => {
                PhaseSixA4Tests.ForceDescent(config, descend);
                foreach (var c in config.PhaseFiveB.Classes) c.LevelOneMaxHealth = 100; // isolated survival fixture
            });
            if (oneBoundary) PhaseFourTestSupport.OneBoundaryInputs(inputs);
            var f = PhaseSixA4Tests.Eligible(inputs);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);
            PhaseSixA4Tests.AddContent(f, 1, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
            PhaseSixA4Tests.AddContent(f, 1, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);
            f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                FloorLifecycleAction.Activate, PhaseSixA4Tests.FloorTwo(f)));
            f.ReopenWithSupportedForegroundProfiles(); return f;
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
                var result = PhaseFourTestSupport.PumpForeground(f, f.Context.RecoveryInputs.TwoFloor, time);
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
        [Test, Timeout(60000)] public void SynchronousWaitingHelpersFailWithoutConsumingOrPoisoningThePrefix()
        {
            var f = Dungeon(); var e = Start(f); long b = ToTransfer(e); e.BlockTransfer(b, 2);
            var bytes = e.ExportCheckpoint(Profile(f).Checkpoints).GetBytes(); long work = e.CommittedCombatWork;
            string source = e.OccupiedFloor, start = e.StartIdentity; long ordinal = e.NextOrdinal;
            Assert.That(Assert.Throws<InvalidOperationException>(() => e.DrainToCompletion(true)).Message, Is.EqualTo("living.transfer.waiting"));
            Assert.That(Assert.Throws<InvalidOperationException>(() => e.AdvanceToNextEvent(e.NextOrdinal)).Message, Is.EqualTo("living.transfer.waiting"));
            CollectionAssert.AreEqual(bytes, e.ExportCheckpoint(Profile(f).Checkpoints).GetBytes());
            Assert.That(e.CommittedCombatWork, Is.EqualTo(work)); Assert.That(e.NextOrdinal, Is.EqualTo(ordinal));
            Assert.That(e.StartIdentity, Is.EqualTo(start)); Assert.That(e.OccupiedFloor, Is.EqualTo(source));
            e.ReleaseTransfer(b + 1, 2, 40, b + 1); e.AdvanceThrough(b + 1, 1, e.NextOrdinal);
            Assert.That(e.OccupiedFloor, Is.EqualTo(PhaseSixA4Tests.FloorTwo(f))); e.Abandon();
        }
        [Test] public void VacantFloorRetainsEligibilityFeedbackWhileAnotherFloorHasAParty()
        {
            var active = new LivingDungeonForegroundInspection { RunId = "run-1", FloorId = "floor-2", StateKey = "ui.living.active" };
            active.Parties = new[] { active };
            active.SetVacantReason("ui.living.unsupported_layout");
            Assert.That(active.ForFloor("floor-2"), Is.SameAs(active)); Assert.That(active.StateKey, Is.EqualTo("ui.living.active"));
            var vacant = active.ForFloor("floor-1"); Assert.That(vacant.StateKey, Is.EqualTo("ui.living.unsupported_layout"));
            active.SetVacantReason("ui.living.admission_unavailable");
            Assert.That(active.ForFloor("floor-1"), Is.SameAs(vacant)); Assert.That(vacant.StateKey, Is.EqualTo("ui.living.admission_unavailable"));
            Assert.That(active.ForFloor("floor-2").RunId, Is.EqualTo("run-1"));
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
        internal static Fixture Waiting(int? capturedBHealth = null, bool oneBoundary = false)
        {
            var f = Dungeon(oneBoundary: oneBoundary); var profile = Profile(f); var a = Start(f, 1);
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
            var failed = PhaseFourTestSupport.PumpForeground(f, f.Context.RecoveryInputs.TwoFloor, finish); f.FileSystem.DisableFailure();
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
        [Test] public void ReopenOneUnitBeforeVacancyAndImmediatelyAfterReleasePreservesExactPrefixes()
        {
            var f = Waiting(); var a = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties[0], Profile(f));
            a.DrainToCompletion(); long finish = a.Inspect().LogicalTime; a.Abandon();
            Through(f, finish - 1);
            string waitingIdentity = f.Runtime.livingDungeon.Parties.Single(p => p.Sequence == 2).CheckpointIdentity;
            f.Reopen();
            var b = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties.Single(p => p.Sequence == 2), Profile(f));
            Assert.That(b.TransferWaiting, Is.True); Assert.That(b.OccupiedFloor, Is.EqualTo(f.State.Floors[0].FloorInstanceId)); b.Abandon();
            Assert.That(f.Runtime.livingDungeon.Parties.Single(p => p.Sequence == 2).CheckpointIdentity, Is.EqualTo(waitingIdentity));
            Through(f, finish);
            string releasedIdentity = f.Runtime.livingDungeon.Parties.Single(p => p.Sequence == 2).CheckpointIdentity;
            f.Reopen();
            b = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties.Single(p => p.Sequence == 2), Profile(f));
            Assert.That(b.TransferGate.ReleaseTime, Is.EqualTo(finish)); Assert.That(b.OccupiedFloor, Is.EqualTo(PhaseSixA4Tests.FloorTwo(f))); b.Abandon();
            Through(f, finish); Assert.That(f.Runtime.livingDungeon.Parties.Single(p => p.Sequence == 2).CheckpointIdentity, Is.EqualTo(releasedIdentity));
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
            // Capture the original nullable Research owners rather than Unity's
            // null-class clone defaults. Only this synthetic fixture wallet changes.
            double before = f.Runtime.structureRuntime.ManaReserve;
            DetachedCanonicalWriteResult funded;
            f.Runtime.structureRuntime.ManaReserve = wallet;
            try { funded = PhaseSixA4Tests.Writer(f).SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime); }
            finally { f.Runtime.structureRuntime.ManaReserve = before; }
            f.Accept(funded);
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
        [TestCase(false)] [TestCase(true)]
        public void TerminalPrefixOnlyFitsAnUnclosedV2Boundary(bool closed)
        {
            var f = Dungeon(); var profile = Profile(f); var e = Start(f);
            try {
                while (!e.IsTerminal) e.AdvanceThrough(e.NextDueTime().Value, 1, e.NextOrdinal);
                Assert.That(e.OutcomeTaken, Is.False);
                var checkpoint = e.ExportCheckpoint(profile.Checkpoints);
                var replay = CombatCheckpointCodec.RestoreSupported(checkpoint.GetBytes(), checkpoint.Identity,
                    checkpoint.StartIdentity, checkpoint.NextOrdinal, profile.Checkpoints);
                Assert.That(replay.IsValid, Is.True, replay.FailureReason); replay.Execution.Abandon();
                var owner = LivingDungeonState.Empty(); LivingDungeonScheduling.Activate(owner, profile, Policy.MinimumArrival, f.Runtime.runHistory);
                long time = e.Inspect().LogicalTime;
                owner.Scheduler.AcceptedThrough = time; owner.Scheduler.ClosedThrough = closed ? time : time - 1;
                var party = new LivingDungeonParty { Sequence = 1, AdmittedAt = 0 }; LivingDungeonScheduling.Pin(party, e, profile); owner.Parties.Add(party);
                f.Runtime.runHistory.NextRunSequence = 2;
                var before = f.Session.GetCurrentBytes();
                var candidate = f.Session.PrepareLivingDungeonReplacement(DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile), f.State, owner);
                Assert.That(candidate.IsSuccess, Is.EqualTo(!closed), "A closed timestamp cannot retain an unsettled terminal; private open staging remains valid");
                CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            } finally { e.Abandon(); }
        }
        [TestCase(true)] [TestCase(false)]
        public void EqualTimeOrdinaryTransferAndBlockedReleaseRespectSequenceAndRecoveryPhases(bool terminalFirst)
        {
            var f = Dungeon(); var profile = Profile(f);
            int departing = terminalFirst ? 1 : 2, incoming = terminalFirst ? 2 : 1;
            var reference = Start(f, departing); RunCheckpoint finalPrefix = null; long finish = -1;
            while (!reference.IsTerminal) {
                long due = reference.NextDueTime().Value;
                if (due != finish) { finalPrefix = reference.ExportCheckpoint(profile.Checkpoints); finish = due; }
                reference.AdvanceThrough(due, 1, reference.NextOrdinal);
            }
            long terminalOrdinal = reference.Inspect().LastStep.Value.Ordinal; reference.Abandon();
            var restored = CombatCheckpointCodec.RestoreSupported(finalPrefix.GetBytes(), finalPrefix.Identity,
                finalPrefix.StartIdentity, finalPrefix.NextOrdinal, profile.Checkpoints);
            Assert.That(restored.IsValid, Is.True, restored.FailureReason);
            var a = restored.Execution; var b = Start(f, incoming); long original = ToTransfer(b);
            long admitted = finish - original; Assert.That(admitted, Is.GreaterThanOrEqualTo(0));
            Assert.That(a.OccupiedFloor, Is.EqualTo(PhaseSixA4Tests.FloorTwo(f)));
            var owner = LivingDungeonState.Empty(); LivingDungeonScheduling.Activate(owner, profile, Policy.MinimumArrival, f.Runtime.runHistory);
            owner.Scheduler.AcceptedThrough = owner.Scheduler.ClosedThrough = finish - 1;
            foreach (int sequence in new[] { 1, 2 }) {
                var party = new LivingDungeonParty { Sequence = sequence, AdmittedAt = sequence == departing ? 0 : admitted };
                LivingDungeonScheduling.Pin(party, sequence == departing ? a : b, profile); owner.Parties.Add(party);
            }
            f.Runtime.runHistory.NextRunSequence = 3;
            var prepared = f.Session.PrepareLivingDungeonReplacement(DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile), f.State, owner);
            Assert.That(prepared.IsSuccess, Is.True, prepared.Reason);
            f.FileSystem.Seed(f.ActivePath, prepared.Update.GetBytes()); a.Abandon(); b.Abandon(); f.Reopen();
            Through(f, finish);
            b = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties.Single(p => p.Sequence == incoming), profile);
            try {
                Assert.That(b.OccupiedFloor, Is.EqualTo(PhaseSixA4Tests.FloorTwo(f)));
                string monster = f.State.Floors[1].RoomContents.Assignments.Single(m => m.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId).AssignmentId;
                var current = f.Runtime.monsterContinuity.Records.Single(m => m.AssignmentId == monster);
                if (terminalFirst) {
                    Assert.That(b.TransferGate, Is.Null, "An ordinary transfer participates before vacant-floor recovery");
                    Assert.That(current.Health, Is.Zero); Assert.That(current.RevivalRequired, Is.True);
                } else {
                    Assert.That(b.TransferGate, Is.Not.Null); Assert.That(b.TransferGate.VacancySequence, Is.EqualTo(departing));
                    Assert.That(b.TransferGate.VacancyOrdinal, Is.EqualTo(terminalOrdinal));
                    Assert.That(admitted + b.TransferGate.ReleaseTime, Is.EqualTo(finish));
                    Assert.That(b.TransferGate.ReleaseOrdinal, Is.EqualTo(b.TransferGate.BlockOrdinal + 1));
                    Assert.That(current.Health, Is.GreaterThan(0), "A blocked transfer releases after eligible destination recovery");
                    Assert.That(current.RevivalRequired, Is.False);
                }
                Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(o => o.RunId == "run-" + departing), Is.EqualTo(1));
                Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(3));
            } finally { b.Abandon(); }
        }
        [Test] public void UnprovenWaitingReleaseRollbackRequiresRecoveryWithoutPublishingEitherParty()
        {
            var f = Waiting(); var a = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties[0], Profile(f));
            a.DrainToCompletion(); long finish = a.Inspect().LogicalTime; a.Abandon();
            var original = f.Runtime; string structured = JsonUtility.ToJson(original);
            f.FileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            f.FileSystem.Seed(f.ActivePath, f.Session.GetCurrentBytes());
            f.FileSystem.EnableFailureSequence(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush, 1,
                Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 2);
            var failed = PhaseFourTestSupport.PumpForeground(f, f.Context.RecoveryInputs.TwoFloor, finish); f.FileSystem.DisableFailure();
            Assert.That(failed.IsSuccess, Is.False); Assert.That(failed.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
            Assert.That(failed.RuntimeProjection, Is.Null); Assert.That(failed.Session, Is.Null);
            Assert.That(f.Runtime, Is.SameAs(original)); Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(structured));
            Assert.That(f.Runtime.livingDungeon.Parties.Select(p => p.Sequence), Is.EqualTo(new[] { 1, 2 }));
            Assert.That((f.Runtime.runHistory.RecentOutcomes ?? Array.Empty<RunOutcomeRecord>()).Any(o => o.RunId == "run-1"), Is.False);
        }
        [Test] public void OneBoundaryPumpsAndReopenMatchTheSameAcceptedTwoPartyHorizon()
        {
            var direct = Waiting(); var partitioned = Waiting(oneBoundary: true);
            var a = LivingDungeonContracts.Restore(direct.Runtime.livingDungeon.Parties[0], Profile(direct));
            a.DrainToCompletion(); long finish = a.Inspect().LogicalTime; a.Abandon(); Through(direct, finish);
            int pumps = 0;
            do {
                Assert.That(++pumps, Is.LessThan(1000));
                var result = PhaseFourTestSupport.PumpForeground(partitioned, partitioned.Context.RecoveryInputs.TwoFloor, finish);
                Assert.That(result.IsSuccess, Is.True, result.Reason); partitioned.Accept(result); partitioned.Reopen();
            } while (!partitioned.Runtime.livingDungeon.IsClosed);
            Assert.That(pumps, Is.GreaterThan(1));
            string Stable(Fixture f) => Encoding.UTF8.GetString(f.Session.GetCurrentBytes()).Replace(
                "\"livingDungeon\":{\"FormatVersion\":1,\"Revision\":" + f.Runtime.livingDungeon.Revision + ",",
                "\"livingDungeon\":{\"FormatVersion\":1,\"Revision\":0,")
                .Replace(f.Context.RecoveryInputs.Identity.Digest, new string('0', 64));
            Assert.That(direct.Runtime.timeProvenance.RecoveryContextSha256, Is.EqualTo(direct.Context.RecoveryInputs.Identity.Digest));
            Assert.That(partitioned.Runtime.timeProvenance.RecoveryContextSha256, Is.EqualTo(partitioned.Context.RecoveryInputs.Identity.Digest));
            Assert.That(direct.Context.RecoveryInputs.Identity.Digest, Is.Not.EqualTo(partitioned.Context.RecoveryInputs.Identity.Digest));
            Assert.That(Stable(partitioned), Is.EqualTo(Stable(direct)));
        }

        [Test] public void EmptyUnclosedV1DrainConsumesDueOpportunityWithoutReplacementAndRetainsTheFutureIdentity()
        {
            var f = Dungeon(oneBoundary: true); string second = PhaseSixA4Tests.FloorTwo(f);
            f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Deactivate, second));
            var oldPolicy = LivingDungeonForegroundConfig.Load(); var oldProfile = oldPolicy.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized);
            var e = new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot()).StartTimedCombatSnapshot(1, PhaseSixA4Tests.Snapshot(f), oldPolicy.Timing);
            while (e.NextDueTime() == 0) e.AdvanceThrough(0, 1, e.NextOrdinal);
            var owner = LivingDungeonState.Empty(); LivingDungeonScheduling.Activate(owner, oldProfile, oldPolicy.MinimumArrival, f.Runtime.runHistory);
            owner.Scheduler.AcceptedThrough = owner.Scheduler.ClosedThrough = 0;
            var party = new LivingDungeonParty { Sequence = 1, AdmittedAt = 0 }; LivingDungeonScheduling.Pin(party, e, oldProfile); owner.Parties.Add(party);
            f.Runtime.runHistory.NextRunSequence = 2;
            var prepared = f.Session.PrepareLivingDungeonReplacement(DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile), f.State, owner);
            Assert.That(prepared.IsSuccess, Is.True, prepared.Reason); f.FileSystem.Seed(f.ActivePath, prepared.Update.GetBytes()); f.Reopen();
            f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, second));
            Through(f, 0); long firstOpportunity = f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value;
            e.DrainToCompletion(); long finish = e.Inspect().LogicalTime; e.Abandon();
            Assert.That(firstOpportunity, Is.GreaterThan(finish), "The installed opportunity must occur after retirement during the accepted drain");
            long target = checked(firstOpportunity + 1);
            bool emptyDraining = false; long? futureTime = null, futureOrdinal = null;
            for (int pump = 0; pump < 100; pump++) {
                var result = PhaseFourTestSupport.PumpForeground(f, f.Context.RecoveryInputs.TwoFloor, target);
                Assert.That(result.IsSuccess, Is.True, result.Reason); f.Accept(result); f.Reopen(); owner = f.Runtime.livingDungeon;
                Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(2), "Compatibility draining cannot allocate replacement identities");
                if (owner.Scheduler.ProfileId == oldPolicy.Id && owner.Parties.Count == 0 && !owner.IsClosed) {
                    emptyDraining = true;
                    if (owner.Scheduler.ClosedThrough == firstOpportunity) {
                        Assert.That(owner.Scheduler.LastOpportunityTime, Is.EqualTo(firstOpportunity));
                        Assert.That(owner.Scheduler.NextOpportunityOrdinal, Is.EqualTo(1));
                        futureTime = owner.Scheduler.NextOpportunityTime; futureOrdinal = owner.Scheduler.NextOpportunityOrdinal;
                    }
                }
                if (owner.IsClosed) break;
            }
            Assert.That(emptyDraining, Is.True); Assert.That(futureTime.HasValue, Is.True);
            Assert.That(owner.IsClosed, Is.True); Assert.That(owner.Scheduler.ProfileId, Is.EqualTo(Policy.Id));
            Assert.That(owner.Parties, Is.Empty); Assert.That(owner.Scheduler.NextOpportunityTime, Is.EqualTo(futureTime));
            Assert.That(owner.Scheduler.NextOpportunityOrdinal, Is.EqualTo(futureOrdinal));
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(o => o.RunId == "run-1"), Is.EqualTo(1));
        }

        [Test, Timeout(600000)] public void AutomaticProductionPartyCanChooseASurvivingFloorOneExitWithTwoCapturedFloors()
        {
            var f = ProductionDungeon(); string second = PhaseSixA4Tests.FloorTwo(f);
            f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, second));
            Through(f, 0);
            for (int opportunity = 0; opportunity < 128; opportunity++) {
                long time = f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value;
                Through(f, time - 1);
                // A legal player edit invalidates old shared Floor 2 knowledge,
                // without rewriting the immutable inputs of existing parties.
                var monster = f.State.Floors[1].RoomContents.Assignments.Single(a => a.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId);
                var pinned = f.Runtime.livingDungeon.Parties.Select(p => p.CheckpointIdentity).ToArray();
                var draft = TransactionalDungeonDraft.Create(f.State, PhaseSevenA4TransactionalEditorTests.Context(f), new PhaseSevenA4TransactionalEditorTests.Store());
                var position = monster.RoomLocalPosition.Equals(new TileCoordinate(1, 1)) ? new TileCoordinate(2, 1) : new TileCoordinate(1, 1);
                Assert.That(draft.Move(second, monster.RoomInstanceId, monster.AssignmentId, position), Is.True);
                while (draft.Durability == DraftDurability.Pending) Assert.That(draft.FlushNext(), Is.True);
                f.Accept(PhaseSixA4Tests.Writer(f).CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, draft));
                CollectionAssert.AreEqual(pinned, f.Runtime.livingDungeon.Parties.Select(p => p.CheckpointIdentity));
                int next = f.Runtime.runHistory.NextRunSequence; Through(f, time);
                if (f.Runtime.runHistory.NextRunSequence == next) continue; // authentic occupancy throttle
                var party = f.Runtime.livingDungeon.Parties.Single(p => p.Sequence == next);
                var e = LivingDungeonContracts.Restore(party, Profile(f));
                try {
                    Assert.That(e.Snapshot.Floors.Count, Is.EqualTo(2));
                    var outcome = e.DrainToCompletion();
                    var choice = outcome.FloorTransitions?.SingleOrDefault(t => t.CurrentFloorInstanceId == f.State.Floors[0].FloorInstanceId);
                    TestContext.WriteLine($"Automatic production {outcome.RunId} choice={choice?.Reason} survivors={outcome.SurvivalSummary.SurvivorCount}");
                    if (choice == null || choice.NextFloorInstanceId == null || choice.Descend || outcome.SurvivalSummary.SurvivorCount == 0) continue;
                    Assert.That(e.TransferGate, Is.Null);
                    long finish = checked(party.AdmittedAt + e.Inspect().LogicalTime);
                    Through(f, finish);
                    Assert.That(f.Runtime.livingDungeon.Parties.Any(p => p.Sequence == next), Is.False);
                    Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(o => o.RunId == outcome.RunId), Is.EqualTo(1));
                    Assert.That(f.Runtime.runHistory.LatestOutcome.RunId, Is.EqualTo(outcome.RunId));
                    Assert.That(f.Runtime.runHistory.LatestOutcome.ReachedRoomCount, Is.EqualTo(1));
                    Assert.That(f.Runtime.runHistory.LatestOutcome.FinalRouteOutcomeKey, Is.EqualTo(outcome.FinalRouteOutcomeKey));
                    return;
                } finally { e.Abandon(); }
            }
            Assert.Fail("The bounded automatic production arrivals must exercise a surviving Floor 1 EXIT; no decision/HP/timing tuning may be overridden");
        }
        internal static Fixture ProductionDungeon(long? initialTime = null)
        {
            var f = PhaseSixA4Tests.Eligible(initialTime: initialTime);
            CollectionAssert.AreEqual(File.ReadAllBytes("Assets/_Project/Data/Bootstrap/run_simulation_config.json"),
                f.Context.RecoveryInputs.Identity.GetInput(0));
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
            f.ReopenWithSupportedForegroundProfiles();
            return f;
        }
        [Test] public void ExportProductionTwoFloorStandaloneInputsUsingApprovedContentAndTiming()
        {
            // This current-schema producer must not write into retained historical
            // inputs. The explicit supervisor stage owns this fresh output root.
            string export = Environment.GetEnvironmentVariable("DUNGEON_H1F4_TEST_EXPORT_ROOT");
            Assert.That(Application.isBatchMode, Is.True);
            Assert.That(Application.productName, Does.StartWith("Dungeon Lord B3B2 Smoke H1F4 "));
            Assert.That(export, Is.Not.Null.And.Not.Empty);
            var output = new DirectoryInfo(Path.GetFullPath(export));
            Assert.That(output.Name, Is.EqualTo("fixture-inputs"));
            var stage = output.Parent; var campaign = stage?.Parent; var supervisor = campaign?.Parent;
            Assert.That(supervisor?.FullName, Is.EqualTo(Path.GetFullPath(Path.Combine(Application.dataPath,
                "../TestResults/qualification-supervisor"))).IgnoreCase);
            Assert.That(Guid.TryParseExact(campaign?.Name, "N", out _), Is.True);
            Assert.That(stage?.Name, Is.EqualTo("living-regressions").Or.EqualTo("full-edit").Or.EqualTo("full-play"));
            foreach (var directoryInfo in new[] { supervisor, campaign, stage, output }) {
                Assert.That(directoryInfo.Exists, Is.True);
                Assert.That((directoryInfo.Attributes & FileAttributes.ReparsePoint) == 0, Is.True);
            }
            string directory = Path.Combine(output.FullName, "two-floor");
            Assert.That(Directory.Exists(directory), Is.False); Directory.CreateDirectory(directory);
            var utc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var f = ProductionDungeon(utc); string second = PhaseSixA4Tests.FloorTwo(f);
            f.Runtime.lastPausedUtcUnix = utc; // W was authored before the synthetic owner, not moved by this metadata write.
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
            string token = Guid.NewGuid().ToString("N"); string id = variant + "-" + token; string path = Path.Combine(directory, id + ".save.json");
            var bytes = f.Session.GetCurrentBytes();
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write)) stream.Write(bytes, 0, bytes.Length);
            var manifest = new TwoFloorSeedManifest { Product = Application.productName,
                SaveFileName = "living-dungeon-b3b2-qualification-two-floor-" + token + ".json",
                SeedPath = path, SeedSha256 = RunCheckpointCodec.Identity(bytes), HistoricalStartIdentity = historicalStart,
                HistoricalOrdinal = historicalOrdinal, DescendingSamples = descents, LongestFloorTwoDuration = longest };
            string draftCandidate = Path.Combine(Application.persistentDataPath, manifest.SaveFileName) +
                ".editor-draft.draft-" + new string('0', 32) + ".000000.candidate";
            Assert.That(draftCandidate.Length, Is.LessThanOrEqualTo(SpatialMigrationSidecarPaths.WindowsMaximumAbsolutePathCharacters),
                "The isolated save must also fit the existing draft sidecar path envelope");
            using (var stream = new FileStream(Path.Combine(directory, id + ".manifest.json"), FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream)) writer.Write(JsonUtility.ToJson(manifest, true));
        }
    }
}
#endif
