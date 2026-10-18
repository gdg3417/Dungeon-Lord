#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using B = DungeonBuilder.M0.Tests.EditMode.LivingDungeonPhaseABaselineTests;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class LivingDungeonB3B1Tests
    {
        private static readonly RunLogicalTiming Timing = new RunLogicalTiming(3, 7, 5, 2, 11); // synthetic, injected
        private static RunCheckpointLimits Limits => new RunCheckpointLimits(65536, 2048, 1000000);
        private static RunSimulationService Service => new RunSimulationService(CombatC0IntegrationTests.Config(), PhaseSixA4Tests.Loot());
        private static Fixture Dungeon(bool two = false, int hp = 8, bool revival = false)
        {
            var f = two ? PhaseSixA4Tests.Eligible() : Fixture.Create(null);
            f.Configuration = CombatC0IntegrationTests.Config();
            if (!two) f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)));
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            if (two) {
                PhaseSixA4Tests.AddContent(f, 1, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
                f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, PhaseSixA4Tests.FloorTwo(f)));
            }
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);
            if (!two) CombatC2Tests.SetState(f, hp, revival, 0);
            return f;
        }
        private static RunExecution Start(ActiveFloorRunSnapshot snapshot, bool persistent = true, int sequence = 1) =>
            Service.StartTimedCombatSnapshot(sequence, snapshot, Timing, persistent);
        private static RunExecution Restore(RunExecution e)
        {
            var c = e.ExportCheckpoint(Limits);
            var result = Service.RestoreCheckpoint(c.GetBytes(), c.Identity, c.StartIdentity, c.NextOrdinal, Limits);
            Assert.That(result.IsValid, Is.True, result.FailureReason);
            CollectionAssert.AreEqual(c.GetBytes(), result.Execution.ExportCheckpoint(Limits).GetBytes());
            CollectionAssert.AreEqual(CombatCheckpointCodec.Inspection(e), CombatCheckpointCodec.Inspection(result.Execution));
            return result.Execution;
        }
        private static List<RunStep> Drain(RunExecution e, int budget)
        { var steps = new List<RunStep>(); while (!e.IsTerminal) steps.AddRange(e.Advance(budget, e.NextOrdinal)); return steps; }
        private static void EqualOutcome(RunOutcomeRecord a, RunOutcomeRecord b)
        {
            Assert.That(B.Structured(a), Is.EqualTo(B.Structured(b)));
            Assert.That(B.Structured(CombatC0Diagnostics.Events(a)), Is.EqualTo(B.Structured(CombatC0Diagnostics.Events(b))));
            Assert.That(B.Structured(CombatC0Diagnostics.Monsters(a)), Is.EqualTo(B.Structured(CombatC0Diagnostics.Monsters(b))));
            Assert.That(CombatC0Diagnostics.ChargedWork(a), Is.EqualTo(CombatC0Diagnostics.ChargedWork(b)));
        }
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(true, true)]
        public void EveryCommittedPrefixRestoresExactFutureReceiptsAndOutcome(bool persistent, bool twoFloors)
        {
            var f = Dungeon(twoFloors); var before = f.Session.GetCurrentBytes(); var snapshot = PhaseSixA4Tests.Snapshot(f);
            var reference = Start(snapshot, persistent); var steps = Drain(reference, 7); var outcome = reference.TakeOutcome();
            var original = Start(snapshot, persistent);
            for (int i = 0; i <= steps.Count; i++) {
                if (i > 0) original.Advance(1, original.NextOrdinal);
                var restored = Restore(original);
                Assert.That(B.Structured(Drain(restored, i % 7 + 1)), Is.EqualTo(B.Structured(steps.Skip(i).ToArray())), "prefix " + i);
                EqualOutcome(restored.TakeOutcome(), outcome);
            }
            CollectionAssert.AreEqual(before, f.Session.GetCurrentBytes());
            Assert.That(outcome.CombatExecutable, Is.EqualTo(persistent ? MonsterRecoveryRules.Executable : CombatC0Config.Executable));
        }
        [TestCase(false)] [TestCase(true)]
        public void RepeatedLookaheadInsufficientDeadlineExportReopenAndEventualCommitChargeExactlyOnce(bool persistent)
        {
            var s = PhaseSixA4Tests.Snapshot(Dungeon()); var e = Start(s, persistent); var control = Start(s, persistent);
            var steps = Drain(control, 1); var expected = control.TakeOutcome();
            foreach (var step in steps) {
                var committed = CombatCheckpointCodec.Inspection(e); var checkpoint = e.ExportCheckpoint(Limits);
                long due = e.NextDueTime().Value; long charged = e.Combat.Kernel.WorkCount;
                for (int probe = 0; probe < 9; probe++) Assert.That(e.NextDueTime(), Is.EqualTo(due));
                Assert.That(e.Combat.Kernel.WorkCount, Is.EqualTo(charged));
                CollectionAssert.AreEqual(committed, CombatCheckpointCodec.Inspection(e));
                CollectionAssert.AreEqual(checkpoint.GetBytes(), e.ExportCheckpoint(Limits).GetBytes());
                if (due > e.Inspect().LogicalTime) {
                    Assert.That(e.AdvanceThrough(due - 1, 1, e.NextOrdinal), Is.Empty);
                    CollectionAssert.AreEqual(committed, CombatCheckpointCodec.Inspection(e));
                }
                e = Restore(e); Assert.That(e.NextDueTime(), Is.EqualTo(due));
                Assert.That(e.Combat.Kernel.WorkCount, Is.EqualTo(charged));
                var batch = e.AdvanceThrough(due, 1, e.NextOrdinal);
                Assert.That(B.Structured(batch.Single()), Is.EqualTo(B.Structured(step)));
            }
            EqualOutcome(e.TakeOutcome(), expected); e = Restore(e);
            Assert.Throws<InvalidOperationException>(() => e.TakeOutcome());
        }
        [TestCase(0, true)] [TestCase(2, true)] [TestCase(1, false)] [TestCase(8, false)]
        public void InjuredDefeatedAndRevivalStartsMatchManualC2(int hp, bool revival)
        {
            var s = PhaseSixA4Tests.Snapshot(Dungeon(false, hp, revival));
            var manual = Service.StartManualSnapshot(1, s).DrainToCompletion(); var e = Start(s);
            while (!e.IsTerminal) { e.Advance(1, e.NextOrdinal); e = Restore(e); }
            EqualOutcome(e.TakeOutcome(), manual);
        }
        [Test] public void CapturedInputsSurviveConfigAssignmentsAndContinuityEditsWithoutCanonicalPublication()
        {
            var f = Dungeon(); var s = PhaseSixA4Tests.Snapshot(f); var e = Start(s); var expected = Start(s).DrainToCompletion();
            e.Advance(2, 0); f.Configuration.CombatC0.Monsters[0].Damage = int.MaxValue;
            f.Runtime.monsterContinuity.Records[0].Health = 0; f.Runtime.monsterContinuity.Records[0].RevivalRequired = true;
            f.Runtime.structureRuntime.Heat = 99; f.Runtime.structureRuntime.ManaReserve = 99;
            f.Runtime.spatialFloors[0].RoomContents.Assignments[0].RoomLocalPosition = new TileCoordinate(999, 999);
            e = Restore(e); EqualOutcome(e.DrainToCompletion(), expected);
        }
        [TestCase("identity")] [TestCase("start")] [TestCase("ordinal")] [TestCase("format")]
        [TestCase("rules")] [TestCase("evidence")] [TestCase("length")] [TestCase("prefix")]
        [TestCase("inspection")] [TestCase("truncated")] [TestCase("hash")]
        public void ContradictoryAndMalformedBytesNeverExposeAnExecution(string mode)
        {
            var e = Start(PhaseSixA4Tests.Snapshot(Dungeon())); Drain(e, 7); var c = e.ExportCheckpoint(Limits); var bytes = c.GetBytes();
            string identity = c.Identity, start = c.StartIdentity; long ordinal = c.NextOrdinal;
            using (var r = new BinaryReader(new MemoryStream(bytes))) {
                r.ReadInt32(); int rules = checked((int)r.BaseStream.Position + 4); RunCheckpointCodec.Text(r);
                int evidence = checked((int)r.BaseStream.Position + 4); RunCheckpointCodec.Text(r);
                int length = checked((int)r.BaseStream.Position); RunCheckpointCodec.Blob(r);
                r.ReadInt64(); r.ReadInt64(); r.ReadInt64(); r.ReadBoolean(); r.ReadBoolean();
                int prefix = checked((int)r.BaseStream.Position + 4); RunCheckpointCodec.Blob(r);
                int inspection = checked((int)r.BaseStream.Position + 4);
                if (mode == "identity") identity = new string('0', 64);
                else if (mode == "start") start = new string('0', 64);
                else if (mode == "ordinal") ordinal--;
                else {
                    if (mode == "format") bytes[0] = 77;
                    if (mode == "rules") bytes[rules] ^= 1;
                    if (mode == "evidence") bytes[evidence] ^= 1;
                    if (mode == "length") Array.Copy(BitConverter.GetBytes(int.MaxValue), 0, bytes, length, 4);
                    if (mode == "prefix") bytes[prefix] ^= 1;
                    if (mode == "inspection") bytes[inspection] ^= 1;
                    if (mode == "truncated") bytes = bytes.Take(bytes.Length - 1).ToArray();
                    if (mode == "hash") bytes[10] ^= 1;
                    else if (mode != "truncated") Array.Copy(RunCheckpointCodec.Hash(bytes.Take(bytes.Length - 32).ToArray()), 0, bytes, bytes.Length - 32, 32);
                    identity = RunCheckpointCodec.Identity(bytes); // synthetic independently accepted pin exercises replay checks
                }
            }
            var result = Service.RestoreCheckpoint(bytes, identity, start, ordinal, Limits);
            Assert.That(result.IsValid, Is.False); Assert.That(result.Execution, Is.Null); Assert.That(result.FailureReason, Is.Not.Empty);
        }
        [Test] public void TechnicalEnvelopesAndExplicitExecutableOptInFailClosed()
        {
            var e = Start(PhaseSixA4Tests.Snapshot(Dungeon())); Drain(e, 1); var c = e.ExportCheckpoint(Limits);
            foreach (var limit in new[] { new RunCheckpointLimits(c.GetBytes().Length - 1, 2048, 1000000),
                new RunCheckpointLimits(65536, c.NextOrdinal - 1, 1000000), new RunCheckpointLimits(65536, 2048),
                new RunCheckpointLimits(65536, 2048, 999999) }) {
                var result = Service.RestoreCheckpoint(c.GetBytes(), c.Identity, c.StartIdentity, c.NextOrdinal, limit);
                Assert.That(result.IsValid, Is.False); Assert.That(result.Execution, Is.Null);
            }
            var watch = Stopwatch.StartNew(); var restored = Restore(e); watch.Stop();
            TestContext.WriteLine("B3B1 checkpoint bytes=" + c.GetBytes().Length + " events=" + c.NextOrdinal +
                " combatWork=" + e.CommittedCombatWork + " restoreAndVerifyMs=" + watch.Elapsed.TotalMilliseconds);
            Assert.Throws<InvalidOperationException>(() => e.Advance(1, e.NextOrdinal - 1));
            Assert.Throws<ArgumentException>(() => e.AdvanceThrough(-1, 1, e.NextOrdinal));
            CollectionAssert.AreEqual(c.GetBytes(), e.ExportCheckpoint(Limits).GetBytes());
        }
        public static void VerifyForPlayMode() => new LivingDungeonB3B1Tests().RepeatedLookaheadInsufficientDeadlineExportReopenAndEventualCommitChargeExactlyOnce(true);

        [Test] public void MixedB1C0C2RecordsReopenOnlyWithExplicitCompatibleProfileAndCannotSettleB3A()
        {
            var f = Dungeon(); var snapshot = PhaseSixA4Tests.Snapshot(f);
            var profile = new LivingDungeonProfile("test.b3b1.compatible.v1", 2, 4, 65536, 2048, 8192, 262144, 357996,
                Timing, RunPostureResolver.BalancedId, maximumCombatWork: 1000000);
            var b1 = new LivingDungeonProfile(profile.Id, 2, 4, 65536, 2048, 8192, 262144, 357996,
                Timing, RunPostureResolver.BalancedId);
            var owner = LivingDungeonState.Empty(); LivingDungeonScheduling.Activate(owner, profile, 1, f.Runtime.runHistory);
            for (int index = 0; index < 3; index++) {
                LivingDungeonScheduling.Install(owner, index, index * 10000);
                LivingDungeonScheduling.Accept(owner, index * 10000);
                LivingDungeonScheduling.Advance(owner, profile, f.Runtime.runHistory, 1000,
                    sequence => index == 0 ? Service.StartSnapshot(sequence, snapshot, Timing) : Start(snapshot, index == 2, sequence));
                LivingDungeonScheduling.Accept(owner, index * 10000 + 9999);
                while (!owner.IsClosed) LivingDungeonScheduling.Advance(owner, profile, f.Runtime.runHistory, 7, sequence => null);
            }
            Assert.That(owner.Parties.Count, Is.EqualTo(3));
            Assert.That(LivingDungeonContracts.Validate(owner, new[] { profile }), Is.True);
            Assert.That(LivingDungeonContracts.Validate(owner, new[] { b1 }), Is.False);
            Assert.Throws<InvalidOperationException>(() => LivingDungeonTerminalSettlement.Select(owner, profile));
            var original = owner.Parties.Select(p => p.Checkpoint).ToArray();
            f.Context = new DetachedCurrentTargetValidationContext(f.Compatibility, f.Production,
                LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.Profile.Canonical,
                f.Occupancy, livingDungeonProfiles: new[] { profile });
            f.Reopen(); // owner was still dormant on disk
            var recognized = DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile);
            // The shared allocator was advanced by the real scheduler above.
            f.Runtime.runHistory.NextRunSequence = 4;
            recognized = DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile);
            var candidate = f.Session.PrepareLivingDungeonReplacement(recognized, f.State, owner);
            Assert.That(candidate.IsSuccess, Is.True, candidate.Reason);
            f.FileSystem.Seed(f.ActivePath, candidate.Update.GetBytes()); f.Reopen();
            var reopened = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context);
            Assert.That(reopened.IsValid, Is.True, reopened.Reason);
            for (int i = 0; i < original.Length; i++) CollectionAssert.AreEqual(original[i], reopened.LivingDungeon.Parties[i].Checkpoint);
            Assert.That(reopened.LivingDungeon.Parties.Select(p => BitConverter.ToInt32(p.Checkpoint, 0)), Is.EqualTo(new[] { 1, 2, 2 }));
            TestContext.WriteLine("Mixed owner completeSaveBytes=" + f.Session.GetCurrentBytes().Length + " aggregateReplay=" + owner.Parties.Sum(p => p.NextOrdinal));
        }

        [Test] public void PendingGeometryWorkExhaustionIsTheSameBeforeAndAfterReopen()
        {
            var f = Dungeon(); var s = PhaseSixA4Tests.Snapshot(f); var reference = Start(s);
            Drain(reference, 1); long work = reference.CommittedCombatWork;
            f.Configuration.CombatC0.MaximumWork = work - 1;
            var limited = PhaseSixA4Tests.Snapshot(f); var e = Start(limited); var restored = Restore(e);
            string Failure(RunExecution run, bool probe) {
                try { while (!run.IsTerminal) { if (probe) { run.NextDueTime(); run.NextDueTime(); } run.Advance(1, run.NextOrdinal); } }
                catch (InvalidOperationException error) { Assert.Throws<InvalidOperationException>(() => run.ExportCheckpoint(Limits)); return error.Message + ":" + run.NextOrdinal; }
                Assert.Fail("Expected workload exhaustion"); return null;
            }
            Assert.That(Failure(e, true), Is.EqualTo(Failure(restored, false)));
        }
    }
}
#endif
