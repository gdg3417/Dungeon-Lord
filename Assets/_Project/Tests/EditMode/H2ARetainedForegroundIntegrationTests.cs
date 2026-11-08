#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;
using Metrics = DungeonBuilder.M0.ForegroundQualificationMetrics;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;
using Operation = DungeonBuilder.M0.Tests.EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public partial class H2ARetainedForegroundTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static Dictionary<int, RunExecution> PrivateSet(LivingDungeonForegroundExecutions owner) =>
            (Dictionary<int, RunExecution>)typeof(LivingDungeonForegroundExecutions).GetField("executions", Hidden).GetValue(owner);
        private sealed class ThrowingDisposal : IEnumerator<RunStep>
        {
            private readonly IEnumerator<RunStep> inner;
            internal int Disposals;
            internal ThrowingDisposal(RunExecution execution)
            {
                var field = typeof(RunExecution).GetField("cursor", Hidden);
                inner = (IEnumerator<RunStep>)field.GetValue(execution); field.SetValue(execution, this);
            }
            public RunStep Current => inner.Current;
            object IEnumerator.Current => Current;
            public bool MoveNext() => inner.MoveNext();
            public void Reset() => throw new NotSupportedException();
            public void Dispose() { Disposals++; inner.Dispose(); throw new InvalidOperationException("test.only.disposal"); }
        }
        [Test] public void ThrowingPrivateCleanupAfterDurableCommitCannotReportFailure()
        {
            var f = LivingDungeonB3B2Tests.Dungeon(); var owner = Owner(); Admit(f, false, owner);
            var execution = PrivateSet(owner).Values.Single(); var disposal = new ThrowingDisposal(execution);
            var result = PhaseFourTestSupport.PumpForeground(f, Policy(f, false), Due(f),
                candidatePrepared: (_, __) => owner.Clear(), retained: owner);
            Assert.That(result.IsSuccess, Is.True, result.Reason); f.Accept(result);
            CollectionAssert.AreEqual(result.GetPersistedBytes(), f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(owner.Count, Is.Zero); Assert.That(disposal.Disposals, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => execution.ExportCheckpoint(LivingDungeonContracts.Profile(f.Runtime.livingDungeon, f.Context.LivingDungeonProfiles).Checkpoints));
            execution.Abandon(); Assert.That(disposal.Disposals, Is.EqualTo(1));
        }
        [Test] public void ClearRevokesEveryBindingAndDisposesAllPartiesDespiteDisposalFailure()
        {
            var f = LivingDungeonTwoFloorTests.Waiting(); var owner = Owner();
            Pump(f, true, f.Runtime.livingDungeon.Scheduler.ClosedThrough, owner);
            var executions = PrivateSet(owner).Values.ToArray();
            var disposals = executions.Select(e => new ThrowingDisposal(e)).ToArray();
            Assert.DoesNotThrow(owner.Clear); Assert.That(owner.Count, Is.Zero);
            foreach (var e in executions) Assert.Throws<InvalidOperationException>(() => e.ExportCheckpoint(LivingDungeonTwoFloorTests.Profile(f).Checkpoints));
            Assert.That(disposals.All(d => d.Disposals == 1), Is.True);
            Pump(f, true, f.Runtime.livingDungeon.Scheduler.ClosedThrough, owner);
            Assert.That(PrivateSet(owner).Values.All(e => !executions.Contains(e)), Is.True);
        }
        private sealed class ServiceClock : IPreciseTimeSource
        {
            internal double Logical;
            public long UtcNowUnixSeconds() => UtcNow().Seconds;
            public UnixTimePoint UtcNow()
            {
                long ticks = (long)Math.Round(Logical * TimeSpan.TicksPerSecond);
                return new UnixTimePoint(2000 + ticks / TimeSpan.TicksPerSecond, (int)(ticks % TimeSpan.TicksPerSecond));
            }
        }
        private sealed class ServiceCase
        {
            internal readonly Fixture F;
            internal readonly ServiceClock Clock = new ServiceClock();
            internal readonly SaveService Service;
            internal readonly TimeService Time;
            internal readonly LivingDungeonForegroundExecutions Owner;
            internal readonly long Anchor;
            internal int Publications;
            internal SaveData Published;
            internal ServiceCase(Fixture fixture)
            {
                F = fixture;
                Service = LivingDungeonB3B2Tests.Service(F, Clock, out Time, () => Clock.Logical);
                Anchor = Math.Max(0, F.Runtime.livingDungeon.Scheduler?.AcceptedThrough ?? -1);
                Owner = (LivingDungeonForegroundExecutions)typeof(SaveService).GetField("foregroundExecutions", Hidden).GetValue(Service);
                Service.CanonicalRuntimePublished += save => {
                    Publications++; Published = save;
                    Assert.That(Time.AttachedSaveForTests, Is.SameAs(save));
                    CollectionAssert.AreEqual(Service.CanonicalSession.GetCurrentBytes(), F.FileSystem.ReadAllBytes(Service.SavePath));
                };
            }
            internal DetachedCanonicalWriteResult Advance(long horizon)
            {
                Clock.Logical = (horizon - Anchor) / 1000d;
                return Service.AdvanceForeground(F.Runtime, Time.CaptureForegroundObservation(Service.CanonicalSession),
                    F.Context.RecoveryInputs.Bootstrap.tickSeconds, (_, __) => { });
            }
            internal void Pump(long horizon)
            {
                var result = Advance(horizon); Assert.That(result.IsSuccess, Is.True, result.Reason);
                Assert.That(Service.CanonicalSession, Is.SameAs(result.Session));
                Assert.That(Published, Is.SameAs(result.RuntimeProjection)); F.Accept(result);
                Assert.That(Time.AttachedSaveForTests, Is.SameAs(F.Runtime));
            }
            internal void Admit()
            { Pump(0); Pump(F.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value); Assert.That(Owner.Count, Is.EqualTo(1)); }
            internal void FreshFaultFileSystem()
            {
                var fresh = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
                fresh.Seed(Service.SavePath, Service.CanonicalSession.GetCurrentBytes()); F.FileSystem = fresh;
                typeof(SaveService).GetField("_canonicalFileSystem", Hidden).SetValue(Service, fresh);
            }
        }
        private ServiceCase LiveService()
        { var h = new ServiceCase(LivingDungeonB3B2Tests.Dungeon()); owners.Add(h.Owner); h.Admit(); return h; }
        [Test] public void ServiceInstallsExactReadbackAndPublishesClockAcrossReuseAndActiveCanonicalEdit()
        {
            var h = LiveService(); var f = h.F;
            var first = PrivateSet(h.Owner).Values.Single(); var start = f.Runtime.livingDungeon.Parties.Single().StartIdentity;
            Metrics.Reset(); h.Pump(Due(f));
            Assert.That(PrivateSet(h.Owner).Values.Single(), Is.SameAs(first));
            Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.RetainedExecutionReuse], Is.EqualTo(1));
            var checkpoint = f.Runtime.livingDungeon.Parties.Single().Checkpoint;
            var loot = f.State.Floors[0].RoomContents.Assignments.Single(a => a.CategoryId == MvpDungeonPlacementIds.LootNodeCategoryId);
            var edited = h.Service.ExecuteCanonicalMutation(f.Runtime,
                DetachedCanonicalMutationRequest.Unassign(loot.AssignmentId, f.State.Floors[0].FloorInstanceId));
            f.Accept(edited); Assert.That(h.Owner.Count, Is.Zero);
            Assert.That(f.Runtime.livingDungeon.Parties.Single().StartIdentity, Is.EqualTo(start));
            CollectionAssert.AreEqual(checkpoint, f.Runtime.livingDungeon.Parties.Single().Checkpoint);
            Metrics.Reset(); h.Pump(Due(f));
            Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.RetainedExecutionReuse], Is.Zero);
            Assert.That(PrivateSet(h.Owner).Values.Single(), Is.Not.SameAs(first));
            Assert.That(f.Runtime.livingDungeon.Parties.Single().StartIdentity, Is.EqualTo(start));
            Assert.That(h.Publications, Is.EqualTo(5));
            Assert.That(h.Time.HasForegroundPersistenceStall, Is.False);
        }
        [TestCase(false)] [TestCase(true)]
        public void ServiceRollbackRetriesExplicitlyAndUncertainPublicationStaysBlocked(bool uncertain)
        {
            var h = LiveService(); h.FreshFaultFileSystem(); var f = h.F;
            int published = h.Publications; var predecessor = h.Service.CanonicalSession;
            if (uncertain) f.FileSystem.EnableFailureSequence(Operation.Flush, 1, Operation.Replace, 2);
            else f.FileSystem.EnableFailure(Operation.Replace, 1);
            long due = Due(f); var failed = h.Advance(due); f.FileSystem.DisableFailure();
            Assert.That(failed.IsSuccess, Is.False); Assert.That(h.Owner.Count, Is.Zero);
            Assert.That(h.Service.CanonicalSession, Is.SameAs(predecessor)); Assert.That(h.Publications, Is.EqualTo(published));
            Assert.That(failed.Reason, Is.EqualTo(uncertain ? DetachedCanonicalWriteAuthority.RecoveryRequiredReason : DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            Metrics.Reset();
            if (uncertain) {
                Assert.That(h.Service.RequiresCanonicalRecovery, Is.True);
                Assert.That(h.Advance(due).IsSuccess, Is.False);
                Assert.That(Metrics.Calls[(int)Metrics.Area.AtomicWrite], Is.Zero);
            } else {
                Assert.That(h.Time.HasForegroundPersistenceStall, Is.True);
                h.Pump(due); Assert.That(h.Time.HasForegroundPersistenceStall, Is.False);
                Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.RetainedExecutionReuse], Is.Zero);
                Assert.That(h.Publications, Is.EqualTo(published + 1));
            }
        }
        [Test] public void ServiceReopenOfIdenticalBytesDropsProcessLocalExecutions()
        {
            var h = LiveService(); var before = h.Service.CanonicalSession.GetCurrentBytes();
            var execution = PrivateSet(h.Owner).Values.Single();
            var reopened = h.Service.LoadOrCreate("foreground-test", out string reason);
            Assert.That(reopened, Is.Not.Null, reason); Assert.That(h.Owner.Count, Is.Zero);
            CollectionAssert.AreEqual(before, h.Service.CanonicalSession.GetCurrentBytes());
            h.F.Runtime = reopened; h.Time.AttachSave(reopened); Metrics.Reset(); h.Pump(Due(h.F));
            Assert.That(PrivateSet(h.Owner).Values.Single(), Is.Not.SameAs(execution));
            Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.RetainedExecutionReuse], Is.Zero);
        }
        [TestCase("configure")] [TestCase("recovery")] [TestCase("delete")] [TestCase("disable")] [TestCase("quit")]
        public void ServiceAndRootLifecycleInvalidateRetainedExecutions(string boundary)
        {
            var h = LiveService(); var f = h.F;
            if (boundary == "configure") h.Service.ConfigureCanonical(f.Profile, f.Production, f.Compatibility, f.Configuration,
                LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.PositionProfiles, f.Occupancy);
            else if (boundary == "recovery") h.Service.BeginCanonicalRecovery(f.Runtime);
            else if (boundary == "delete") Assert.That(h.Service.DeleteSave(out _), Is.True);
            else {
                var host = new GameObject("h2-a-lifecycle-fixture"); host.SetActive(false); var root = host.AddComponent<GameRoot>();
                try {
                    typeof(GameRoot).GetProperty(nameof(GameRoot.Save)).SetValue(root, f.Runtime);
                    typeof(GameRoot).GetProperty(nameof(GameRoot.SaveService)).SetValue(root, h.Service);
                    typeof(GameRoot).GetProperty(nameof(GameRoot.TimeService)).SetValue(root, h.Time);
                    typeof(GameRoot).GetField("_configuredSimulationTickSeconds", Hidden).SetValue(root, f.Context.RecoveryInputs.Bootstrap.tickSeconds);
                    if (boundary == "quit") root.ApplyApplicationQuit();
                    else typeof(GameRoot).GetMethod("OnDisable", Hidden).Invoke(root, null);
                } finally { UnityEngine.Object.DestroyImmediate(host); }
            }
            Assert.That(h.Owner.Count, Is.Zero);
        }
        [TestCase("start")] [TestCase("ordinal")] [TestCase("prefix")] [TestCase("executable")]
        public void SuperficiallyMatchingPrivateExecutionMustReconstruct(string changed)
        {
            var f = LivingDungeonB3B2Tests.Dungeon(); var owner = Owner(); Admit(f, false, owner);
            var stale = PrivateSet(owner).Values.Single();
            if (changed == "start") stale.StartInputs[0] ^= 1;
            else if (changed == "ordinal") typeof(RunExecution).GetField("ordinal", Hidden).SetValue(stale, stale.NextOrdinal + 1);
            else if (changed == "prefix") stale.PrefixDigest[0] ^= 1;
            else typeof(CombatC0Kernel).GetField("<RulesId>k__BackingField", Hidden).SetValue(stale.Combat.Kernel, "test.only.incompatible.executable");
            Metrics.Reset(); Pump(f, false, Due(f), owner);
            Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.RetainedExecutionReuse], Is.Zero);
            Assert.That(PrivateSet(owner).Values.Single(), Is.Not.SameAs(stale));
        }
        [TestCase(false)] [TestCase(true)] public void CorruptCheckpointAndChangedConsumedCombatConfigFailClosed(bool configuration)
        {
            var h = LiveService(); var before = h.Service.CanonicalSession.GetCurrentBytes();
            if (configuration) h.F.Configuration.CombatC0.MaximumEvents++;
            else {
                var p = h.F.Runtime.livingDungeon.Parties.Single();
                var text = System.Text.Encoding.UTF8.GetString(before);
                string chunk = Convert.ToBase64String(p.Checkpoint.Take(Math.Min(LivingDungeonContracts.BinaryChunkBytes, p.Checkpoint.Length)).ToArray());
                string corrupt = (chunk[0] == 'A' ? "B" : "A") + chunk.Substring(1);
                Assert.That(text.Contains(chunk), Is.True);
                h.F.FileSystem.Seed(h.Service.SavePath, System.Text.Encoding.UTF8.GetBytes(text.Replace(chunk, corrupt)));
            }
            try { Assert.That(h.Advance(Due(h.F)).IsSuccess, Is.False); Assert.That(h.Owner.Count, Is.Zero); }
            finally { if (configuration) h.F.Configuration.CombatC0.MaximumEvents--; }
        }
        [Test] public void TwoRetainedPartiesHaveIndependentMutableExecutionGraphs()
        {
            var f = LivingDungeonTwoFloorTests.Waiting(); var owner = Owner();
            Pump(f, true, f.Runtime.livingDungeon.Scheduler.ClosedThrough, owner);
            var pair = PrivateSet(owner).Values.OrderBy(e => e.Inspect().RunId).ToArray();
            Assert.That(pair.Length, Is.EqualTo(2)); Assert.That(pair[0].Party, Is.Not.SameAs(pair[1].Party));
            Assert.That(pair[0].Party.Members[0], Is.Not.SameAs(pair[1].Party.Members[0]));
            var second = pair[1].ExportCheckpoint(LivingDungeonTwoFloorTests.Profile(f).Checkpoints).GetBytes();
            pair[0].Party.Members[0].ApplyDamage(1);
            CollectionAssert.AreEqual(second, pair[1].ExportCheckpoint(LivingDungeonTwoFloorTests.Profile(f).Checkpoints).GetBytes());
            owner.Clear();
        }
        [TestCase(false)] [TestCase(true)] public void PumpPartitionsPreserveExactReferenceBytesAndCanonicalOutcomes(bool two)
        {
            var f = two ? LivingDungeonTwoFloorTests.Dungeon() : LivingDungeonB3B2Tests.Dungeon(); var seed = f.Session.GetCurrentBytes();
            string outcome = null, wallet = null, monsters = null;
            foreach (bool split in new[] { false, true }) {
                var snapshots = new List<byte[]>();
                foreach (bool retain in new[] { false, true }) {
                    f.FileSystem.Seed(f.ActivePath, seed); f.Reopen(); var owner = retain ? Owner() : null;
                    Pump(f, two, 0, owner); Pump(f, two, f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value, owner);
                    long finish = Finish(f); int index = 0;
                    if (split) for (int i = 0; i < 3; i++) {
                        Pump(f, two, Due(f), owner);
                        if (!retain) snapshots.Add(f.Session.GetCurrentBytes()); else CollectionAssert.AreEqual(snapshots[index++], f.Session.GetCurrentBytes());
                    }
                    Pump(f, two, finish, owner);
                    if (!retain) snapshots.Add(f.Session.GetCurrentBytes()); else CollectionAssert.AreEqual(snapshots[index], f.Session.GetCurrentBytes());
                }
                // Partition-dependent revision bookkeeping is not normalized out
                // of a save. Compare full bytes to each partition's own reference,
                // and compare the canonical consequences across partitions.
                string result = JsonUtility.ToJson(f.Runtime.runHistory), balance = JsonUtility.ToJson(f.Runtime.structureRuntime), condition = JsonUtility.ToJson(f.Runtime.monsterContinuity);
                if (!split) { outcome = result; wallet = balance; monsters = condition; }
                else { Assert.That(result, Is.EqualTo(outcome)); Assert.That(balance, Is.EqualTo(wallet)); Assert.That(condition, Is.EqualTo(monsters)); }
            }
        }
        [MethodImpl(MethodImplOptions.NoInlining)] private static long CalibrateHeap()
        {
            long before = GC.GetTotalMemory(true);
            var calibration = new byte[4 * 1024 * 1024]; // test-only counter calibration, not a retained-state policy
            long after = GC.GetTotalMemory(true); GC.KeepAlive(calibration); return after - before;
        }
        [MethodImpl(MethodImplOptions.NoInlining)] private static WeakReference[] WeakExecutions(LivingDungeonForegroundExecutions owner) =>
            PrivateSet(owner).Values.Select(e => new WeakReference(e)).ToArray();
        [MethodImpl(MethodImplOptions.NoInlining)] private static string ResourcesHeld(LivingDungeonForegroundExecutions owner) =>
            string.Join(";", PrivateSet(owner).OrderBy(p => p.Key).Select(p => p.Key + "/" + p.Value.NextOrdinal + "/" +
                p.Value.PresentationSteps.Count + "/" + p.Value.Combat.Events.Count + "/" + p.Value.StartInputs.Length));
        [Test] public void RepeatedPumpsDoNotAccumulateExecutionsOrBuffersAndAbandonmentReleasesManagedGraphs()
        {
            long calibration = CalibrateHeap(); Assert.That(calibration, Is.GreaterThan(0), "Managed heap probe must respond to a live allocation");
            long allocationBefore = GC.GetAllocatedBytesForCurrentThread(); var allocationProbe = new byte[4096];
            bool allocationAvailable = GC.GetAllocatedBytesForCurrentThread() > allocationBefore; GC.KeepAlive(allocationProbe);
            var f = LivingDungeonTwoFloorTests.Waiting(); var owner = Owner(); long closed = f.Runtime.livingDungeon.Scheduler.ClosedThrough;
            var deltas = new List<long>(); int collections = GC.CollectionCount(0);
            for (int sample = 0; sample < 3; sample++) {
                Pump(f, true, closed, owner); string resources = ResourcesHeld(owner);
                for (int pump = 0; pump < 12; pump++) {
                    Pump(f, true, closed, owner); Assert.That(owner.Count, Is.EqualTo(2));
                    Assert.That(ResourcesHeld(owner), Is.EqualTo(resources));
                }
                var references = WeakExecutions(owner); long retained = GC.GetTotalMemory(true);
                owner.Clear(); long released = GC.GetTotalMemory(true); deltas.Add(retained - released);
                Assert.That(references.All(r => !r.IsAlive), Is.True, "No hidden execution archive may survive Clear");
                TestContext.WriteLine("H2-A retained heap pair=" + sample + " retained=" + retained + " released=" + released + " resources=" + resources);
            }
            TestContext.WriteLine("H2-A heap calibration=" + calibration + " releaseDeltas=" + string.Join(",", deltas) +
                " threadAllocationAvailable=" + allocationAvailable + " gen0CollectionsIncludingExplicitGC=" + (GC.CollectionCount(0) - collections));
            // Global heap differences include unrelated Editor/JIT noise. Weak
            // reachability and stable resource counts are the correctness checks.
        }
    }
}
#endif
