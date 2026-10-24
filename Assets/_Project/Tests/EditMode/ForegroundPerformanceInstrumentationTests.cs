#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine;
using Metrics = DungeonBuilder.M0.ForegroundQualificationMetrics;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    // Small deterministic in-memory campaigns. No production SaveService, scene,
    // player namespace, native output or sustained qualification entry point.
    public class ForegroundPerformanceInstrumentationTests
    {
        [TearDown] public void Disable()
        {
            Metrics.Enabled = Metrics.DetailedEnabled = false;
            Assert.That(Metrics.ActiveDepth, Is.Zero);
            Metrics.Reset();
        }

        [Test] public void DisabledResetBoundsAndExceptionNesting()
        {
            Metrics.Enabled = false; Metrics.DetailedEnabled = true; Metrics.Reset();
            using (Metrics.Measure(Metrics.Area.Stage))
            using (Metrics.Trace(Metrics.Boundary.Pump)) { }
            Assert.That(Metrics.Calls.Sum(), Is.Zero); Assert.That(Metrics.RecordCount, Is.Zero);
            Metrics.Enabled = true; Metrics.DetailedEnabled = false;
            using (Metrics.Trace(Metrics.Boundary.Pump)) { }
            Assert.That(Metrics.RecordCount, Is.Zero);
            Metrics.DetailedEnabled = true;
            try {
                using (Metrics.Trace(Metrics.Boundary.Pump, Metrics.Origin.Pump))
                using (Metrics.Trace(Metrics.Boundary.Restoration, Metrics.Origin.Execution, 7, 11))
                    throw new InvalidOperationException("test-only exception");
            } catch (InvalidOperationException) { }
            Assert.That(Metrics.ActiveDepth, Is.Zero);
            Assert.That(Metrics.Records[1].Parent, Is.EqualTo(0));
            Assert.That(Metrics.Records[1].PrefixDepth, Is.EqualTo(11));
            Assert.That(Metrics.Records[1].Sequence, Is.EqualTo(7));
            Assert.That(Metrics.Records[0].InclusiveTicks,
                Is.EqualTo(Metrics.Records[0].SelfTicks + Metrics.Records[1].InclusiveTicks));
            Assert.That(Metrics.SelfTicks.All(t => t >= 0), Is.True);
            Metrics.Reset();
            for (int i = 0; i < Metrics.MaximumRecords + 3; i++)
                using (Metrics.Trace(Metrics.Boundary.SupportedReader, Metrics.Origin.LifetimeNormal, depth: i)) { }
            Assert.That(Metrics.RecordCount, Is.EqualTo(Metrics.MaximumRecords));
            Assert.That(Metrics.DroppedRecords, Is.EqualTo(3));
            Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.SupportedReader], Is.EqualTo(Metrics.MaximumRecords + 3));
            Metrics.Reset(); Nest(Metrics.MaximumNesting + 2);
            Assert.That(Metrics.DroppedScopes, Is.EqualTo(2));
            Metrics.Reset(); Assert.That(Metrics.BoundaryCalls.Sum(), Is.Zero);
            Assert.That(Metrics.OriginCalls.Cast<long>().Sum(), Is.Zero);
            Assert.That(Metrics.RecordCount + Metrics.DroppedRecords + Metrics.DroppedScopes, Is.Zero);
        }
        private static void Nest(int depth)
        {
            if (depth == 0) return;
            using (Metrics.Trace(Metrics.Boundary.Pump)) Nest(depth - 1);
        }

        [Test] public void NormalAdmissionEventAndSettlementAreExactlyNeutral()
        {
            var f = LivingDungeonTwoFloorTests.Dungeon(); var seed = f.Session.GetCurrentBytes();
            var plain = Normal(f, false);
            f.FileSystem.Seed(f.ActivePath, seed); f.Reopen();
            var detailed = Normal(f, true);
            CollectionAssert.AreEqual(plain, detailed);
        }
        private static List<string> Normal(Fixture f, bool enabled)
        {
            var snapshots = new List<string>();
            Run("activation", enabled, () => LivingDungeonTwoFloorTests.Through(f, 0)); Snapshot(f, snapshots);
            long admission = f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value;
            Run("admission", enabled, () => LivingDungeonTwoFloorTests.Through(f, admission)); Snapshot(f, snapshots);
            if (enabled) Assert.That(Metrics.OriginCalls[(int)Metrics.Boundary.SupportedReader, (int)Metrics.Origin.LifetimeNormal], Is.GreaterThan(0));
            var p = f.Runtime.livingDungeon.Parties.Single();
            var e = LivingDungeonContracts.Restore(p, LivingDungeonTwoFloorTests.Profile(f));
            long next = p.AdmittedAt + e.NextDueTime().Value;
            e.DrainToCompletion(); long finish = p.AdmittedAt + e.Inspect().LogicalTime; e.Abandon();
            Run("event", enabled, () => LivingDungeonTwoFloorTests.Through(f, next)); Snapshot(f, snapshots);
            Run("settlement", enabled, () => LivingDungeonTwoFloorTests.Through(f, finish)); Snapshot(f, snapshots);
            return snapshots;
        }

        [Test] public void BlockedReleaseAndFailedPersistenceAreExactlyNeutral()
        {
            var f = LivingDungeonTwoFloorTests.Waiting(); var seed = f.Session.GetCurrentBytes();
            var plain = Release(f, false);
            f.FileSystem.Seed(f.ActivePath, seed); f.Reopen();
            var detailed = Release(f, true);
            CollectionAssert.AreEqual(plain, detailed);
        }
        private static List<string> Release(Fixture f, bool enabled)
        {
            var snapshots = new List<string>(); Snapshot(f, snapshots);
            var a = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties[0], LivingDungeonTwoFloorTests.Profile(f));
            a.DrainToCompletion(); long finish = a.Inspect().LogicalTime; a.Abandon();
            var before = f.Session.GetCurrentBytes(); string live = JsonUtility.ToJson(f.Runtime);
            f.FileSystem.EnableFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 1);
            DetachedCanonicalWriteResult failed = null;
            Run("failed-release", enabled, () => failed = Pump(f, finish));
            f.FileSystem.DisableFailure();
            Assert.That(failed.IsSuccess, Is.False); Assert.That(failed.RuntimeProjection, Is.Null);
            Assert.That(failed.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(live)); snapshots.Add(failed.Reason);
            Run("release", enabled, () => LivingDungeonTwoFloorTests.Through(f, finish)); Snapshot(f, snapshots);
            if (enabled) Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.TransferReleased], Is.GreaterThan(0));
            return snapshots;
        }
        private static DetachedCanonicalWriteResult Pump(Fixture f, long through) => PhaseSixA4Tests.Writer(f).AdvanceForeground(
            f.ActivePath, f.FileSystem, f.Session, f.Runtime, LivingDungeonTwoFloorTests.Policy, through, 10, (save, tick) => { }, 1000);

        [Test] public void ForegroundBlockedTransferIsAttributedAndExactlyNeutral()
        {
            // Same authentic two-party setup as Waiting(), pinned just before Blocked.
            var f = LivingDungeonTwoFloorTests.Dungeon(); var profile = LivingDungeonTwoFloorTests.Profile(f);
            var a = LivingDungeonTwoFloorTests.Start(f, 1); var b = LivingDungeonTwoFloorTests.Start(f, 2);
            long deadline;
            try {
                deadline = LivingDungeonTwoFloorTests.ToTransfer(b);
                a.AdvanceThrough(LivingDungeonTwoFloorTests.ToTransfer(a), 1, a.NextOrdinal);
                while (a.NextDueTime() < deadline) a.AdvanceThrough(a.NextDueTime().Value, 1, a.NextOrdinal);
                var owner = LivingDungeonState.Empty(); LivingDungeonScheduling.Activate(owner, profile,
                    LivingDungeonTwoFloorTests.Policy.MinimumArrival, f.Runtime.runHistory);
                owner.Scheduler.AcceptedThrough = deadline; owner.Scheduler.ClosedThrough = deadline - 1;
                foreach (var pair in new[] { (sequence: 1, execution: a), (sequence: 2, execution: b) }) {
                    var party = new LivingDungeonParty { Sequence = pair.sequence, AdmittedAt = 0 };
                    LivingDungeonScheduling.Pin(party, pair.execution, profile); owner.Parties.Add(party);
                }
                f.Runtime.runHistory.NextRunSequence = 3;
                var prepared = f.Session.PrepareLivingDungeonReplacement(DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile), f.State, owner);
                Assert.That(prepared.IsSuccess, Is.True, prepared.Reason);
                f.FileSystem.Seed(f.ActivePath, prepared.Update.GetBytes()); f.Reopen();
            } finally { a.Abandon(); b.Abandon(); }
            var seed = f.Session.GetCurrentBytes(); byte[] plain = null;
            foreach (bool enabled in new[] { false, true }) {
                f.FileSystem.Seed(f.ActivePath, seed); f.Reopen();
                Run("blocked", enabled, () => LivingDungeonTwoFloorTests.Through(f, deadline));
                if (!enabled) plain = f.Session.GetCurrentBytes();
                else {
                    CollectionAssert.AreEqual(plain, f.Session.GetCurrentBytes());
                    Assert.That(Metrics.OriginCalls[(int)Metrics.Boundary.TransferBlocked, (int)Metrics.Origin.Execution], Is.EqualTo(1));
                }
            }
        }

        [Test] public void PostPublicationInspectionAttributesEveryPartyWithoutChangingBytes()
        {
            var f = LivingDungeonTwoFloorTests.Waiting(); var before = f.Session.GetCurrentBytes();
            var service = new SaveService(null, new SaveConfig(), Path.GetTempPath());
            service.ConfigureCanonical(f.Profile, f.Production, f.Compatibility, f.Configuration,
                LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.PositionProfiles, f.Occupancy);
            var values = new List<string[]>();
            foreach (bool enabled in new[] { false, true }) {
                LivingDungeonForegroundInspection view = null;
                Run("inspection", enabled, () => view = service.InspectForeground(f.Runtime, 10));
                values.Add(view.Parties.Select(p => p.RunId + "/" + p.Ordinal + "/" + p.FloorId + "/" + p.Waiting + "/" +
                    string.Join(",", p.Steps.Select(s => s.Ordinal + "/" + s.Kind + "/" + s.LogicalTime))).ToArray());
                CollectionAssert.AreEqual(before, f.Session.GetCurrentBytes());
                CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            }
            CollectionAssert.AreEqual(values[0], values[1]);
            Assert.That(Metrics.OriginCalls[(int)Metrics.Boundary.SupportedReader, (int)Metrics.Origin.Inspection], Is.EqualTo(2));
        }

        [Test] public void RejectedSemanticReadbackPreservesTheExactPredecessorInBothModes()
        {
            var f = LivingDungeonTwoFloorTests.Dungeon(); var original = f.Session.GetCurrentBytes();
            var candidate = (byte[])original.Clone(); candidate[candidate.Length - 1] ^= 1;
            foreach (bool enabled in new[] { false, true }) {
                string reason = null;
                Run("semantic-readback-failure", enabled, () => reason = ExactCompleteSaveAtomicPersistence.Persist(
                    f.ActivePath, f.FileSystem, original, candidate, f.Profile.Canonical.Serialized.MaximumCollectionRecords, _ => false));
                Assert.That(reason, Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
                CollectionAssert.AreEqual(original, f.FileSystem.ReadAllBytes(f.ActivePath));
                CollectionAssert.AreEqual(original, f.Session.GetCurrentBytes());
            }
        }

        [Test] public void StalePredecessorIsExactlyNeutral()
        {
            var f = LivingDungeonTwoFloorTests.Dungeon(); var original = f.Session.GetCurrentBytes();
            var reasons = new List<string>();
            foreach (bool enabled in new[] { false, true }) {
                var foreign = (byte[])original.Clone(); foreign[0] ^= 1;
                f.FileSystem.Seed(f.ActivePath, foreign); string live = JsonUtility.ToJson(f.Runtime);
                DetachedCanonicalWriteResult result = null;
                Run("stale", enabled, () => result = Pump(f, 0));
                Assert.That(result.IsSuccess, Is.False); Assert.That(result.RuntimeProjection, Is.Null);
                CollectionAssert.AreEqual(foreign, f.FileSystem.ReadAllBytes(f.ActivePath));
                Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(live)); reasons.Add(result.Reason);
            }
            Assert.That(reasons[0], Is.EqualTo(reasons[1]));
        }

        [TestCase("historical-b1-format1.json")]
        [TestCase("historical-c2-format2.json")]
        public void HistoricalReadersAreExactlyNeutral(string file)
        {
            var frozen = JsonUtility.FromJson<Historical>(File.ReadAllText("Tools/Presentation/Fixtures/" + file));
            var bytes = Convert.FromBase64String(frozen.Base64);
            var limits = SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
            var profile = LivingDungeonForegroundConfig.Load().CreateProfile(limits.Canonical.ForSchema(13).Serialized);
            foreach (bool enabled in new[] { false, true }) {
                RunCheckpointRestoreResult restored = null;
                Run(file, enabled, () => restored = CombatCheckpointCodec.RestoreSupported(bytes, frozen.CheckpointIdentity,
                    frozen.StartIdentity, frozen.NextOrdinal, profile.Checkpoints));
                Assert.That(restored.IsValid, Is.True, restored.FailureReason);
                try { CollectionAssert.AreEqual(bytes, restored.Execution.ExportCheckpoint(profile.Checkpoints).GetBytes()); }
                finally { restored.Execution.Abandon(); }
                var corrupt = (byte[])bytes.Clone(); corrupt[corrupt.Length - 1] ^= 1;
                var rejected = CombatCheckpointCodec.RestoreSupported(corrupt, frozen.CheckpointIdentity, frozen.StartIdentity, frozen.NextOrdinal, profile.Checkpoints);
                Assert.That(rejected.IsValid, Is.False); Assert.That(rejected.FailureReason, Is.EqualTo("run.checkpoint.commit_mismatch"));
            }
        }
        [Serializable] private sealed class Historical { public string Base64, CheckpointIdentity, StartIdentity; public long NextOrdinal; }

        [Test] public void EmptyScopeOverheadIsReportedWithoutASpeedupClaim()
        {
            // Test-local sample count; median/interleaving prevents treating warmup order as a speedup.
            const int samples = 4096;
            foreach (bool enabled in new[] { false, true, true, false }) {
                Metrics.Reset(); Metrics.Enabled = Metrics.DetailedEnabled = enabled;
                using (Metrics.Trace(Metrics.Boundary.Pump)) { } // warm fixed marker initialization
                var timer = Stopwatch.StartNew();
                for (int i = 0; i < samples; i++) using (Metrics.Trace(Metrics.Boundary.SupportedReader, depth: i)) { }
                timer.Stop(); Metrics.Enabled = Metrics.DetailedEnabled = false;
                TestContext.WriteLine("empty-scope enabled=" + enabled + " samples=" + samples + " elapsed_ms=" + timer.Elapsed.TotalMilliseconds);
            }
        }

        internal static void Run(string label, bool enabled, Action action)
        {
            Metrics.Reset(); Metrics.Enabled = Metrics.DetailedEnabled = enabled;
            var timer = Stopwatch.StartNew();
            try { action(); }
            finally { timer.Stop(); Metrics.Enabled = Metrics.DetailedEnabled = false; }
            Assert.That(Metrics.ActiveDepth, Is.Zero);
            TestContext.WriteLine("case=" + label + " enabled=" + enabled + " elapsed_ms=" + timer.Elapsed.TotalMilliseconds +
                " records=" + Metrics.RecordCount + " dropped_records=" + Metrics.DroppedRecords + " dropped_scopes=" + Metrics.DroppedScopes);
            if (!enabled) { Assert.That(Metrics.RecordCount, Is.Zero); return; }
            Assert.That(Metrics.DroppedScopes, Is.Zero);
            if (Metrics.DroppedRecords == 0) {
                var children = new long[Metrics.RecordCount];
                for (int i = 0; i < Metrics.RecordCount; i++) {
                    var record = Metrics.Records[i];
                    if (record.Parent >= 0) children[record.Parent] += record.InclusiveTicks;
                }
                for (int i = 0; i < Metrics.RecordCount; i++)
                    Assert.That(Metrics.Records[i].InclusiveTicks, Is.EqualTo(Metrics.Records[i].SelfTicks + children[i]));
            }
            for (int i = 0; i < Metrics.BoundaryCalls.Length; i++) {
                if (Metrics.BoundaryCalls[i] == 0) continue;
                TestContext.WriteLine("boundary=" + (Metrics.Boundary)i + " calls=" + Metrics.BoundaryCalls[i] +
                    " inclusive_ms=" + Ms(Metrics.InclusiveTicks[i]) + " selected_self_ms=" + Ms(Metrics.SelfTicks[i]));
                for (int j = 0; j < Metrics.OriginCalls.GetLength(1); j++) if (Metrics.OriginCalls[i, j] != 0)
                    TestContext.WriteLine("origin=" + (Metrics.Boundary)i + "/" + (Metrics.Origin)j + " calls=" + Metrics.OriginCalls[i, j]);
            }
            // Formatting/allocation occurs only after the intact action has finished.
            foreach (var group in Metrics.Records.Take(Metrics.RecordCount).Where(r => r.Boundary == Metrics.Boundary.SupportedReader)
                .GroupBy(r => new { r.Origin, r.Sequence }))
                TestContext.WriteLine("reader=" + group.Key.Origin + " party=" + group.Key.Sequence + " count=" + group.Count() +
                    " prefix_min=" + group.Min(r => r.PrefixDepth) + " prefix_max=" + group.Max(r => r.PrefixDepth));
        }
        private static double Ms(long ticks) => ticks * 1000d / Stopwatch.Frequency;
        private static void Snapshot(Fixture f, List<string> target)
        {
            target.Add(Convert.ToBase64String(f.Session.GetCurrentBytes()));
            foreach (var party in f.Runtime.livingDungeon.Parties) {
                target.Add(party.StartIdentity); target.Add(Convert.ToBase64String(party.Checkpoint));
                var e = LivingDungeonContracts.Restore(party, LivingDungeonTwoFloorTests.Profile(f), true);
                try { foreach (var s in e.PresentationSteps) target.Add(s.Ordinal + "/" + s.Kind + "/" + s.LogicalTime + "/" + s.FloorIndex); }
                finally { e.Abandon(); }
            }
        }
    }
}
#endif
