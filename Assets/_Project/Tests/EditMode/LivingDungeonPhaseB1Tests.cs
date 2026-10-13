#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using B = DungeonBuilder.M0.Tests.EditMode.LivingDungeonPhaseABaselineTests;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class LivingDungeonPhaseB1Tests
    {
        private static RunCheckpointLimits Limits(long events = 10000) => new RunCheckpointLimits(4000000, events);
        private static RunSimulationService Service() => new RunSimulationService(B.Configuration("clear"), PhaseSixA4Tests.Loot());
        public static RunExecution CreateForPlayMode() => Route("clear", 2);
        public static string CompleteForPlayMode(object state) => B.Structured(((RunExecution)state).DrainToCompletion());
        public static RunExecution RestoreForPlayMode(object state) => Restore((RunExecution)state);
        public static string InspectForPlayMode(object state) => B.Structured(((RunExecution)state).Inspect());
        public static long ElapsedForPlayMode(object state) => ((RunExecution)state).Inspect().LogicalTime;
        public static long DueForPlayMode(object state) => ((RunExecution)state).NextDueTime().Value;
        public static int AdvanceForPlayMode(object state, long deadline)
        { var execution = (RunExecution)state; return execution.AdvanceThrough(deadline, 1, execution.NextOrdinal).Length; }
        private static RunExecution Route(string mode, int rooms, bool spatial = true, RunLogicalTiming timing = null)
        {
            var config = B.Configuration(mode);
            if (rooms == 100) foreach (var c in config.PhaseFiveB.Classes) c.LevelOneMaxHealth = 1000000;
            var assignments = mode == "wipe" ? Enumerable.Range(0, 3).Select(i => B.Assignment("killer" + i,
                MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 0, 1, i))
                .Concat(B.Content().Where(a => a.CategoryId != MvpDungeonPlacementIds.MonsterCategoryId)).ToArray() : B.Content();
            return new RunSimulationService(config, PhaseSixA4Tests.Loot()).StartRoute(new StructureRuntimeState { Heat = 9.75, ManaReserve = 100 },
                42, 1, RunPostureResolver.BalancedId, Enumerable.Range(0, rooms).Select(i => B.Room(i, spatial, assignments)).ToArray(),
                spatial ? timing ?? LivingDungeonPhaseAExecutionTests.Timing() : null);
        }
        private static RunExecution Restore(RunExecution execution)
        {
            var checkpoint = execution.ExportCheckpoint(Limits()); var bytes = checkpoint.GetBytes();
            var result = Service().RestoreCheckpoint(bytes, checkpoint.Identity, checkpoint.StartIdentity, checkpoint.NextOrdinal, Limits());
            Assert.That(result.IsValid, Is.True, result.FailureReason); Assert.That(result.Execution, Is.Not.SameAs(execution));
            Assert.That(B.Structured(result.Execution.Inspect()), Is.EqualTo(B.Structured(execution.Inspect())));
            CollectionAssert.AreEqual(bytes, result.Execution.ExportCheckpoint(Limits()).GetBytes());
            bytes[0] ^= 1;
            CollectionAssert.AreEqual(checkpoint.GetBytes(), result.Execution.ExportCheckpoint(Limits()).GetBytes());
            return result.Execution;
        }
        private static List<RunStep> Drain(RunExecution execution)
        {
            var steps = new List<RunStep>();
            while (!execution.IsTerminal) steps.AddRange(execution.Advance(7, execution.NextOrdinal));
            return steps;
        }
        private static void EveryBoundary(Func<RunExecution> create)
        {
            var reference = create(); var all = Drain(reference); var outcome = B.Structured(reference.TakeOutcome());
            var original = create();
            for (int position = 0; position <= all.Count; position++)
            {
                if (position > 0) original.Advance(1, original.NextOrdinal);
                var restored = Restore(original); var remaining = Drain(restored);
                Assert.That(B.Structured(remaining), Is.EqualTo(B.Structured(all.Skip(position).ToArray())), "position " + position);
                Assert.That(B.Structured(restored.TakeOutcome()), Is.EqualTo(outcome), "position " + position);
                Assert.Throws<InvalidOperationException>(() => restored.TakeOutcome());
            }
            TestContext.WriteLine("Verified every event boundary: " + all.Count);
        }
        [TestCase("clear", true)] [TestCase("wipe", true)] [TestCase("clear", false)] [TestCase("retreat", true)]
        public void EveryRoomCombatTrapLootRewardAndPartialTerminalWipeBoundaryRestoresExactly(string mode, bool spatial)
            => EveryBoundary(() => Route(mode, 2, spatial));

        private static ActiveFloorRunSnapshot Canonical(bool twoFloors, out Action unchanged)
        {
            var f = twoFloors ? PhaseSixA4Tests.Eligible() : PhaseFiveBBranchIntegrationTests.Fixture();
            if (twoFloors) {
                PhaseSixA4Tests.ForceDescent(f.Configuration, true);
                PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
                PhaseSixA4Tests.AddContent(f, 1, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
                f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                    FloorLifecycleAction.Activate, PhaseSixA4Tests.FloorTwo(f)));
            }
            string before = B.Structured(f.Runtime);
            unchanged = () => Assert.That(B.Structured(f.Runtime), Is.EqualTo(before));
            return PhaseSixA4Tests.Snapshot(f);
        }
        [TestCase(false)] [TestCase(true)]
        public void EveryBranchApproachChoiceReturnAndFloorTransferBoundaryRestoresWithoutPublication(bool twoFloors)
        {
            var snapshot = Canonical(twoFloors, out var unchanged);
            EveryBoundary(() => Service().StartSnapshot(1, snapshot, LivingDungeonPhaseAExecutionTests.Timing())); unchanged();
        }
        [Test]
        public void RepeatedCyclesAndConsumedTerminalOutcomeStayExact()
        {
            var state = CreateForPlayMode(); var reference = CreateForPlayMode();
            while (!state.IsTerminal) {
                var expected = reference.Advance(1, reference.NextOrdinal);
                CollectionAssert.AreEqual(expected.Select(s => B.Structured(s)), state.Advance(1, state.NextOrdinal).Select(s => B.Structured(s)));
                state = Restore(state);
            }
            Assert.That(B.Structured(state.TakeOutcome()), Is.EqualTo(B.Structured(reference.TakeOutcome())));
            state = Restore(state); Assert.Throws<InvalidOperationException>(() => state.TakeOutcome());
            Assert.That(state.AdvanceThrough(long.MaxValue, 1, state.NextOrdinal), Is.Empty);
        }
        [Test]
        public void OriginalDungeonAndConfigurationEditsCannotReplaceRestoredInputs()
        {
            var f = PhaseFiveBBranchIntegrationTests.Fixture(); var snapshot = PhaseSixA4Tests.Snapshot(f);
            var state = Service().StartSnapshot(1, snapshot, LivingDungeonPhaseAExecutionTests.Timing());
            state.Advance(1, 0); var checkpoint = state.ExportCheckpoint(Limits());
            var expected = B.Structured(state.DrainToCompletion());
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
            foreach (var profile in f.Configuration.PhaseFiveB.DamageProfiles) profile.MinimumDamage = profile.MaximumDamage = int.MaxValue;
            var afterEdit = B.Structured(f.Runtime);
            var restored = Service().RestoreCheckpoint(checkpoint.GetBytes(), checkpoint.Identity, checkpoint.StartIdentity, checkpoint.NextOrdinal, Limits());
            Assert.That(restored.IsValid, Is.True, restored.FailureReason);
            Assert.That(B.Structured(restored.Execution.DrainToCompletion()), Is.EqualTo(expected));
            Assert.That(B.Structured(f.Runtime), Is.EqualTo(afterEdit));
        }
        [TestCase("clear")] [TestCase("wipe")]
        public void EveryTimedBoundaryChecksEligibilityBeforeConsequences(string mode)
        {
            var reference = Route(mode, 2); var all = Drain(reference); var state = Route(mode, 2);
            foreach (var expected in all) {
                var before = B.Structured(state.Inspect()); Assert.That(state.NextDueTime(), Is.EqualTo(expected.LogicalTime));
                Assert.That(B.Structured(state.Inspect()), Is.EqualTo(before));
                if (expected.LogicalTime > state.Inspect().LogicalTime) {
                    Assert.That(state.AdvanceThrough(expected.LogicalTime - 1, 7, state.NextOrdinal), Is.Empty);
                    Assert.That(B.Structured(state.Inspect()), Is.EqualTo(before));
                    state = Restore(state);
                }
                var actual = state.AdvanceThrough(expected.LogicalTime, 1, state.NextOrdinal);
                Assert.That(B.Structured(actual.Single()), Is.EqualTo(B.Structured(expected)));
            }
            Assert.That(B.Structured(state.TakeOutcome()), Is.EqualTo(B.Structured(reference.TakeOutcome())));
        }
        [TestCase(false)] [TestCase(true)]
        public void BranchAndTransferCannotCommitBeyondDeadline(bool twoFloors)
        {
            var snapshot = Canonical(twoFloors, out var unchanged);
            var reference = Service().StartSnapshot(1, snapshot, LivingDungeonPhaseAExecutionTests.Timing());
            var state = Service().StartSnapshot(1, snapshot, LivingDungeonPhaseAExecutionTests.Timing());
            foreach (var expected in Drain(reference)) {
                var before = B.Structured(state.Inspect()); Assert.That(state.NextDueTime(), Is.EqualTo(expected.LogicalTime));
                if (expected.LogicalTime > state.Inspect().LogicalTime) {
                    Assert.That(state.AdvanceThrough(expected.LogicalTime - 1, 7, state.NextOrdinal), Is.Empty);
                    Assert.That(B.Structured(state.Inspect()), Is.EqualTo(before));
                }
                Assert.That(B.Structured(state.AdvanceThrough(expected.LogicalTime, 1, state.NextOrdinal).Single()), Is.EqualTo(B.Structured(expected)));
            }
            Assert.That(B.Structured(state.TakeOutcome()), Is.EqualTo(B.Structured(reference.TakeOutcome()))); unchanged();
        }
        [TestCase(1)] [TestCase(2)] [TestCase(7)]
        public void SplitDeadlinesEqualTimesAndWorkBudgetsPreserveEveryEvent(int budget)
        {
            var timing = new RunLogicalTiming(1, 0, 0, 0, 0); var reference = Route("clear", 2, timing: timing);
            var all = Drain(reference); var state = Route("clear", 2, timing: timing); var actual = new List<RunStep>();
            foreach (long deadline in all.Select(s => s.LogicalTime).Distinct()) {
                while (!state.IsTerminal && state.NextDueTime() <= deadline) {
                    var batch = state.AdvanceThrough(deadline, budget, state.NextOrdinal);
                    Assert.That(batch.Length, Is.InRange(1, budget)); actual.AddRange(batch);
                }
                state = Restore(state);
            }
            Assert.That(B.Structured(actual), Is.EqualTo(B.Structured(all)));
            Assert.That(B.Structured(state.TakeOutcome()), Is.EqualTo(B.Structured(reference.TakeOutcome())));
        }
        [Test]
        public void InvalidBackwardDeadlinesAndOverflowDoNotApplyFutureDamage()
        {
            var state = CreateForPlayMode(); state.AdvanceThrough(1, 1, 0); var before = B.Structured(state.Inspect());
            Assert.Throws<ArgumentException>(() => state.AdvanceThrough(-1, 1, state.NextOrdinal));
            Assert.Throws<ArgumentException>(() => state.AdvanceThrough(0, 1, state.NextOrdinal));
            Assert.Throws<ArgumentException>(() => state.AdvanceThrough(1, 0, state.NextOrdinal));
            Assert.That(B.Structured(state.Inspect()), Is.EqualTo(before));
            var restored = Restore(state); Assert.Throws<ArgumentException>(() => restored.AdvanceThrough(0, 1, restored.NextOrdinal));
            var overflow = Route("clear", 2, timing: new RunLogicalTiming(long.MaxValue, long.MaxValue, 0, 0, 0));
            try { while (!overflow.IsTerminal) overflow.AdvanceThrough(long.MaxValue, 1, overflow.NextOrdinal); Assert.Fail("Expected overflow"); }
            catch (OverflowException) { Assert.That(overflow.Inspect().ResolvedEncounters, Is.Zero); }
            Assert.Throws<InvalidOperationException>(() => overflow.ExportCheckpoint(Limits()));
            Assert.Throws<InvalidOperationException>(() => overflow.TakeOutcome());
            Assert.Throws<InvalidOperationException>(() => Route("clear", 2, false).AdvanceThrough(1, 1, 0));
        }
        private static byte[] Rehash(byte[] value)
        {
            using (var hash = SHA256.Create()) Array.Copy(hash.ComputeHash(value, 0, value.Length - 32), 0, value, value.Length - 32, 32);
            return value;
        }
        [TestCase("format")] [TestCase("rules")] [TestCase("snapshot")] [TestCase("damaged")]
        [TestCase("truncated")] [TestCase("ordinal")] [TestCase("hp")] [TestCase("time")]
        public void InvalidCheckpointsFailAtomicallyWithStableReason(string change)
        {
            var state = CreateForPlayMode(); state.Advance(7, 0); var checkpoint = state.ExportCheckpoint(Limits()); var bytes = checkpoint.GetBytes();
            string original = B.Structured(state.Inspect()); string expected = "run.checkpoint.encoding";
            using (var r = new BinaryReader(new MemoryStream(bytes))) {
                r.ReadInt32(); int rulesSize = r.ReadInt32(); long rulesOffset = r.BaseStream.Position; r.BaseStream.Position += rulesSize;
                int startSize = r.ReadInt32(); long startOffset = r.BaseStream.Position; r.BaseStream.Position += startSize;
                long ordinalOffset = r.BaseStream.Position;
                switch (change) {
                    case "format": bytes[0] = 99; Rehash(bytes); expected = "run.checkpoint.unsupported_format"; break;
                    case "rules": bytes[rulesOffset] ^= 1; Rehash(bytes); expected = "run.checkpoint.unsupported_rules"; break;
                    case "snapshot": bytes[startOffset + 4] ^= 1; Rehash(bytes); expected = "run.checkpoint.start_mismatch"; break;
                    case "damaged": bytes[20] ^= 1; break;
                    case "truncated": bytes = bytes.Take(bytes.Length - 1).ToArray(); break;
                    case "ordinal": bytes[ordinalOffset] ^= 1; Rehash(bytes); expected = "run.checkpoint.stale_ordinal"; break;
                    case "time": bytes[ordinalOffset + 8] ^= 1; Rehash(bytes); expected = "run.checkpoint.evidence_mismatch"; break;
                    case "hp": bytes[bytes.Length - 40] ^= 1; Rehash(bytes); expected = "run.checkpoint.evidence_mismatch"; break;
                }
            }
            for (int repeat = 0; repeat < 2; repeat++) {
                // A synthetic independently accepted identity exercises structural/replay checks too.
                var result = Service().RestoreCheckpoint(bytes, Identity(bytes), checkpoint.StartIdentity, checkpoint.NextOrdinal, Limits());
                Assert.That(result.Execution, Is.Null); Assert.That(result.FailureReason, Is.EqualTo(expected));
            }
            Assert.That(B.Structured(state.Inspect()), Is.EqualTo(original)); Assert.That(state.DrainToCompletion(), Is.Not.Null);
        }
        [Test]
        public void ByteAndReplayBoundsRejectBeforeExposingAHandle()
        {
            var state = CreateForPlayMode(); state.Advance(7, 0); var checkpoint = state.ExportCheckpoint(Limits());
            Assert.Throws<InvalidOperationException>(() => state.ExportCheckpoint(new RunCheckpointLimits(1, 100)));
            Assert.That(Service().RestoreCheckpoint(checkpoint.GetBytes(), checkpoint.Identity, checkpoint.StartIdentity, checkpoint.NextOrdinal, Limits(6)).FailureReason,
                Is.EqualTo("run.checkpoint.replay_limit"));
            Assert.That(Service().RestoreCheckpoint(checkpoint.GetBytes(), checkpoint.Identity, checkpoint.StartIdentity, checkpoint.NextOrdinal, new RunCheckpointLimits(1, 100)).FailureReason,
                Is.EqualTo("run.checkpoint.byte_limit"));
        }
        [TestCase(false)] [TestCase(true)]
        public void LegacyNullAndEmptyAssignmentDiscriminatorsSurviveNewObjectReconstruction(bool absent)
        {
            var room = B.Room(0, false); room.Assignments = absent ? null : Array.Empty<RunRoomAssignment>();
            room.AssignedMonsterOptionIds = new[] { MvpDungeonPlacementIds.SkeletonOptionId };
            var config = B.Configuration("clear"); config.MvpCompositionOutcomeTuning = null; config.PhaseSix = null;
            EveryBoundary(() => new RunSimulationService(config, PhaseSixA4Tests.Loot()).StartRoute(
                new StructureRuntimeState { ManaReserve = 100 }, 42, 1, RunPostureResolver.BalancedId, new[] { room }));
        }
        [Test]
        public void AlteredOrdinalWithMatchingCallerExpectationStillFailsReplayEvidence()
        {
            var state = CreateForPlayMode(); state.Advance(7, 0); var checkpoint = state.ExportCheckpoint(Limits()); var bytes = checkpoint.GetBytes();
            using (var r = new BinaryReader(new MemoryStream(bytes))) {
                r.ReadInt32(); int size = r.ReadInt32(); r.BaseStream.Position += size;
                size = r.ReadInt32(); r.BaseStream.Position += size;
                long offset = r.BaseStream.Position; Array.Copy(BitConverter.GetBytes(6L), 0, bytes, offset, 8);
            }
            Rehash(bytes);
            var result = Service().RestoreCheckpoint(bytes, Identity(bytes), checkpoint.StartIdentity, 6, Limits());
            Assert.That(result.IsValid, Is.False); Assert.That(result.FailureReason, Is.EqualTo("run.checkpoint.evidence_mismatch"));
        }
        private static string Identity(byte[] value)
        { using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(value)).Replace("-", "").ToLowerInvariant(); }
        [Test]
        public void RecomputedChecksumCannotChangeCommittedDeadlineOrConsumedOutcome()
        {
            var state = CreateForPlayMode(); state.DrainToCompletion(); var checkpoint = state.ExportCheckpoint(Limits()); var bytes = checkpoint.GetBytes();
            using (var r = new BinaryReader(new MemoryStream(bytes))) {
                r.ReadInt32(); int size = r.ReadInt32(); r.BaseStream.Position += size; size = r.ReadInt32(); r.BaseStream.Position += size;
                r.BaseStream.Position += 25; bytes[r.BaseStream.Position] = 0; // consumed-outcome flag
            }
            Rehash(bytes);
            var result = Service().RestoreCheckpoint(bytes, checkpoint.Identity, checkpoint.StartIdentity, checkpoint.NextOrdinal, Limits());
            Assert.That(result.Execution, Is.Null); Assert.That(result.FailureReason, Is.EqualTo("run.checkpoint.commit_mismatch"));
            Assert.Throws<InvalidOperationException>(() => state.TakeOutcome());
        }
        [Test]
        public void OrdinalCompletionAfterTimedAdvancementRetainsRestorableAcceptedTimeBasis()
        {
            var state = CreateForPlayMode(); state.AdvanceThrough(1, 1, 0);
            while (!state.IsTerminal) { state.Advance(1, state.NextOrdinal); state = Restore(state); }
            var expected = CreateForPlayMode().DrainToCompletion();
            Assert.That(B.Structured(state.TakeOutcome()), Is.EqualTo(B.Structured(expected)));
            state = Restore(state); Assert.Throws<InvalidOperationException>(() => state.TakeOutcome());
        }
        [TestCase(false)] [TestCase(true)]
        public void MeasureCanonicalBranchAndTwoFloorRestore(bool twoFloors)
        {
            var snapshot = Canonical(twoFloors, out var unchanged); var state = Service().StartSnapshot(1, snapshot, LivingDungeonPhaseAExecutionTests.Timing());
            var clock = System.Diagnostics.Stopwatch.StartNew(); var steps = Drain(state); double advanceMs = clock.Elapsed.TotalMilliseconds;
            clock.Restart(); var checkpoint = state.ExportCheckpoint(Limits()); double encodeMs = clock.Elapsed.TotalMilliseconds;
            clock.Restart(); var restored = Restore(state); double restoreMs = clock.Elapsed.TotalMilliseconds;
            Assert.That(B.Structured(restored.TakeOutcome()), Is.EqualTo(B.Structured(state.TakeOutcome()))); unchanged();
            TestContext.WriteLine("canonicalTwoFloors=" + twoFloors + "; events=" + steps.Count + "; checkpointBytes=" + checkpoint.GetBytes().Length +
                "; advanceMs=" + advanceMs + "; encodeMs=" + encodeMs + "; restoreAndVerifyMs=" + restoreMs);
        }
        [TestCase(1)] [TestCase(100)]
        public void MeasureCheckpointSizeRestoreAndAdvancement(int rooms)
        {
            var state = Route("clear", rooms); var clock = System.Diagnostics.Stopwatch.StartNew(); var all = Drain(state);
            double advanceMs = clock.Elapsed.TotalMilliseconds; clock.Restart(); var checkpoint = state.ExportCheckpoint(Limits());
            double encodeMs = clock.Elapsed.TotalMilliseconds; clock.Restart(); var restored = Restore(state); double restoreMs = clock.Elapsed.TotalMilliseconds;
            Assert.That(B.Structured(restored.TakeOutcome()), Is.EqualTo(B.Structured(state.TakeOutcome())));
            TestContext.WriteLine("rooms=" + rooms + "; events=" + all.Count + "; checkpointBytes=" + checkpoint.GetBytes().Length +
                "; advanceMs=" + advanceMs + "; encodeMs=" + encodeMs + "; restoreAndVerifyMs=" + restoreMs);
        }
    }
}
#endif
