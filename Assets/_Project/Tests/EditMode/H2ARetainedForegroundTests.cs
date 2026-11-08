#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using Metrics = DungeonBuilder.M0.ForegroundQualificationMetrics;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;
using Operation = DungeonBuilder.M0.Tests.EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public partial class H2ARetainedForegroundTests
    {
        private readonly List<LivingDungeonForegroundExecutions> owners = new List<LivingDungeonForegroundExecutions>();
        private bool enabled, detailed;
        [SetUp] public void CaptureMetrics()
        { enabled = Metrics.Enabled; detailed = Metrics.DetailedEnabled; Metrics.Enabled = Metrics.DetailedEnabled = true; Metrics.Reset(); }
        [TearDown] public void Release()
        { foreach (var owner in owners) owner.Clear(); owners.Clear(); Metrics.Reset(); Metrics.Enabled = enabled; Metrics.DetailedEnabled = detailed; }
        private LivingDungeonForegroundExecutions Owner()
        { var owner = new LivingDungeonForegroundExecutions(); owners.Add(owner); return owner; }
        private static LivingDungeonForegroundConfig Policy(Fixture f, bool two) => two ? f.Context.RecoveryInputs.TwoFloor : f.Context.RecoveryInputs.OneFloor;
        private static void Pump(Fixture f, bool two, long through, LivingDungeonForegroundExecutions owner = null)
        {
            var result = PhaseFourTestSupport.PumpForeground(f, Policy(f, two), through, retained: owner);
            Assert.That(result.IsSuccess, Is.True, result.Reason); f.Accept(result);
            Assert.That(f.Runtime.livingDungeon.IsClosed, Is.True);
            if (owner != null) Assert.That(owner.Count, Is.LessThanOrEqualTo(two ? 2 : 1));
        }
        private static void Admit(Fixture f, bool two, LivingDungeonForegroundExecutions owner)
        { Pump(f, two, 0, owner); Pump(f, two, f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value, owner); Assert.That(owner.Count, Is.EqualTo(1)); }
        private static long Due(Fixture f)
        {
            var profile = LivingDungeonContracts.Profile(f.Runtime.livingDungeon, f.Context.LivingDungeonProfiles);
            var party = f.Runtime.livingDungeon.Parties.First(); var e = LivingDungeonContracts.Restore(party, profile);
            try { return party.AdmittedAt + e.NextDueTime().Value; } finally { e.Abandon(); }
        }
        private static long Finish(Fixture f)
        {
            var party = f.Runtime.livingDungeon.Parties.First();
            var e = LivingDungeonContracts.Restore(party, LivingDungeonContracts.Profile(f.Runtime.livingDungeon, f.Context.LivingDungeonProfiles));
            try { e.DrainToCompletion(); return party.AdmittedAt + e.Inspect().LogicalTime; } finally { e.Abandon(); }
        }
        [TestCase(false)] [TestCase(true)]
        public void SuccessivePumpsReuseRealExecutionsAndMatchEveryCanonicalByte(bool two)
        {
            var f = two ? LivingDungeonTwoFloorTests.Dungeon() : LivingDungeonB3B2Tests.Dungeon();
            var seed = f.Session.GetCurrentBytes(); var reference = new List<byte[]>();
            long restorations = 0, replays = 0, writes = 0, validations = 0;
            foreach (bool retain in new[] { false, true }) {
                f.FileSystem.Seed(f.ActivePath, seed); f.Reopen(); var owner = retain ? Owner() : null;
                Metrics.Reset(); var watch = System.Diagnostics.Stopwatch.StartNew();
                long allocation = GC.GetAllocatedBytesForCurrentThread(); int snapshot = 0;
                void Step(long horizon) {
                    Pump(f, two, horizon, owner);
                    var bytes = f.Session.GetCurrentBytes();
                    if (!retain) reference.Add(bytes); else CollectionAssert.AreEqual(reference[snapshot], bytes);
                    snapshot++;
                }
                Step(0); Step(f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value);
                Step(Due(f)); Step(Due(f)); Step(Due(f)); Step(Finish(f));
                Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(o => o.RunId == "run-1"), Is.EqualTo(1));
                long executionRestores = Metrics.BoundaryCalls[(int)Metrics.Boundary.ExecutionReconstruction];
                long replayCount = Metrics.Calls[(int)Metrics.Area.CheckpointReplay];
                TestContext.WriteLine("H2-A bounded " + (two ? "two-floor" : "one-floor") + " retained=" + retain +
                    " executionRestores=" + executionRestores + " totalReplays=" + replayCount +
                    " elapsedMs=" + watch.Elapsed.TotalMilliseconds + " allocated=" + (GC.GetAllocatedBytesForCurrentThread() - allocation) +
                    " retainedPopulation=" + (owner?.Count ?? 0));
                if (!retain) {
                    restorations = executionRestores; replays = replayCount;
                    writes = Metrics.Calls[(int)Metrics.Area.AtomicWrite]; validations = Metrics.Calls[(int)Metrics.Area.Validate];
                } else {
                    Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.RetainedExecutionReuse], Is.GreaterThanOrEqualTo(3));
                    Assert.That(executionRestores, Is.LessThan(restorations)); Assert.That(replayCount, Is.LessThan(replays));
                    Assert.That(Metrics.Calls[(int)Metrics.Area.AtomicWrite], Is.EqualTo(writes));
                    Assert.That(Metrics.Calls[(int)Metrics.Area.Validate], Is.EqualTo(validations));
                    Assert.That(owner.Count, Is.Zero, "Settled executions must not remain in an archive");
                }
            }
        }
        [Test] public void WaitingSurvivesRebindAndGenuineRetirementReleasesExactlyOnce()
        {
            var f = LivingDungeonTwoFloorTests.Waiting(); var seed = f.Session.GetCurrentBytes();
            byte[] expected = null;
            foreach (bool retain in new[] { false, true }) {
                f.FileSystem.Seed(f.ActivePath, seed); f.Reopen(); var owner = retain ? Owner() : null;
                long closed = f.Runtime.livingDungeon.Scheduler.ClosedThrough;
                Pump(f, true, closed, owner); Pump(f, true, closed, owner);
                if (retain) Assert.That(owner.Count, Is.EqualTo(2));
                Pump(f, true, Finish(f), owner);
                if (!retain) expected = f.Session.GetCurrentBytes(); else CollectionAssert.AreEqual(expected, f.Session.GetCurrentBytes());
                var p = f.Runtime.livingDungeon.Parties.Single(); var e = LivingDungeonContracts.Restore(p, LivingDungeonTwoFloorTests.Profile(f));
                try { Assert.That(e.TransferWaiting, Is.False); Assert.That(e.TransferGate.VacancySequence, Is.EqualTo(1)); }
                finally { e.Abandon(); }
                Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(o => o.RunId == "run-1"), Is.EqualTo(1));
            }
        }
        [TestCase(false)] [TestCase(true)] public void FailedWriteAbandonsSpeculationAndNeverReusesIt(bool uncertain)
        {
            var f = LivingDungeonB3B2Tests.Dungeon(); var owner = Owner(); Admit(f, false, owner);
            var predecessor = f.Session.GetCurrentBytes(); long due = Due(f);
            // Match the established uncertain-rollback fixture: earlier synthetic
            // transaction evidence must not consume the injected first flush.
            f.FileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            f.FileSystem.Seed(f.ActivePath, predecessor);
            if (uncertain) f.FileSystem.EnableFailureSequence(Operation.Flush, 1, Operation.Replace, 2);
            else f.FileSystem.EnableFailure(Operation.Replace, 1);
            var failed = PhaseFourTestSupport.PumpForeground(f, Policy(f, false), due, retained: owner);
            f.FileSystem.DisableFailure(); Assert.That(failed.IsSuccess, Is.False); Assert.That(failed.RuntimeProjection, Is.Null);
            Assert.That(failed.Reason, Is.EqualTo(uncertain ? DetachedCanonicalWriteAuthority.RecoveryRequiredReason : DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            Assert.That(owner.Count, Is.Zero); CollectionAssert.AreEqual(predecessor, f.Session.GetCurrentBytes());
            if (uncertain) return; // Preserve the uncertain state; no guessed retry.
            CollectionAssert.AreEqual(predecessor, f.FileSystem.ReadAllBytes(f.ActivePath));
            Metrics.Reset(); Pump(f, false, due, owner);
            Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.RetainedExecutionReuse], Is.Zero);
            Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.ExecutionReconstruction], Is.GreaterThan(0));
        }
        [Test] public void ReopenEvenWithIdenticalBytesDropsTheOldSessionBinding()
        {
            var f = LivingDungeonB3B2Tests.Dungeon(); var owner = Owner(); Admit(f, false, owner);
            f.Reopen(); Metrics.Reset(); Pump(f, false, Due(f), owner);
            Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.RetainedExecutionReuse], Is.Zero);
            owner.InvalidateUnlessBoundTo(null); Assert.That(owner.Count, Is.Zero);
        }
        [Test] public void InvalidationWhilePublicationIsPendingCancelsSuccessfulRebind()
        {
            var f = LivingDungeonB3B2Tests.Dungeon(); var owner = Owner(); Admit(f, false, owner);
            long due = Due(f);
            var result = PhaseFourTestSupport.PumpForeground(f, Policy(f, false), due,
                candidatePrepared: (_, __) => owner.Clear(), retained: owner);
            Assert.That(result.IsSuccess, Is.True, result.Reason); f.Accept(result);
            Assert.That(owner.Count, Is.Zero, "A late durable success must respect lifecycle invalidation during IO");
            Metrics.Reset(); Pump(f, false, Due(f), owner);
            Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.RetainedExecutionReuse], Is.Zero);
            Assert.That(owner.Count, Is.EqualTo(1));
        }
        [Test] public void ChangedDiskPredecessorAndConsumedInputsInvalidateBeforeAnyWrite()
        {
            var f = LivingDungeonB3B2Tests.Dungeon(); var owner = Owner(); Admit(f, false, owner);
            var original = f.Session.GetCurrentBytes(); var foreign = (byte[])original.Clone(); foreign[0] ^= 1;
            f.FileSystem.Seed(f.ActivePath, foreign);
            var rejected = PhaseFourTestSupport.PumpForeground(f, Policy(f, false), Due(f), retained: owner);
            Assert.That(rejected.IsSuccess, Is.False); Assert.That(owner.Count, Is.Zero);
            CollectionAssert.AreEqual(foreign, f.FileSystem.ReadAllBytes(f.ActivePath));
            f.FileSystem.Seed(f.ActivePath, original); Pump(f, false, f.Runtime.livingDungeon.Scheduler.ClosedThrough, owner);
            Assert.That(owner.Count, Is.EqualTo(1));
            int tick = f.Context.RecoveryInputs.Bootstrap.tickSeconds;
            f.Context.RecoveryInputs.Bootstrap.tickSeconds++;
            try {
                rejected = PhaseFourTestSupport.PumpForeground(f, Policy(f, false), f.Runtime.livingDungeon.Scheduler.ClosedThrough, retained: owner);
                Assert.That(rejected.IsSuccess, Is.False); Assert.That(owner.Count, Is.Zero);
                CollectionAssert.AreEqual(f.Session.GetCurrentBytes(), f.FileSystem.ReadAllBytes(f.ActivePath));
            } finally { f.Context.RecoveryInputs.Bootstrap.tickSeconds = tick; }
        }
    }
}
#endif
