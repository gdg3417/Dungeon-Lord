#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine;
using B = DungeonBuilder.M0.Tests.EditMode.LivingDungeonPhaseABaselineTests;
using T = DungeonBuilder.M0.Tests.EditMode.LivingDungeonTwoFloorTests;
using Metrics = DungeonBuilder.M0.ForegroundQualificationMetrics;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class AdmissionStartReconstructionTests
    {
        private static RunExecution Restore(RunCheckpoint cp, RunCheckpointLimits limits,
            CombatStartCapture.AdmissionPreparation preparation = null)
        {
            var r = CombatCheckpointCodec.RestoreSupported(cp.GetBytes(), cp.Identity, cp.StartIdentity, cp.NextOrdinal,
                limits, true, preparation);
            Assert.That(r.IsValid, Is.True, r.FailureReason); return r.Execution;
        }
        private static List<RunStep> Finish(RunExecution e, int budget)
        {
            var steps = new List<RunStep>();
            while (!e.IsTerminal) {
                if (e.TransferWaiting) {
                    long release = e.TransferGate.Deadline + 5000; // isolated synthetic vacancy
                    steps.Add(e.ReleaseTransfer(release, e.TransferGate.BlockerSequence, 40, release));
                } else steps.AddRange(e.Advance(budget, e.NextOrdinal));
            }
            return steps;
        }
        private static void Equal(RunExecution a, RunExecution b, RunCheckpointLimits limits)
        {
            CollectionAssert.AreEqual(a.StartInputs, b.StartInputs);
            Assert.That(a.StartIdentity, Is.EqualTo(b.StartIdentity));
            CollectionAssert.AreEqual(a.ExportCheckpoint(limits).GetBytes(), b.ExportCheckpoint(limits).GetBytes());
            Assert.That(B.Structured(a.Inspect()), Is.EqualTo(B.Structured(b.Inspect())));
            Assert.That(B.Structured(a.PresentationSteps), Is.EqualTo(B.Structured(b.PresentationSteps)));
            Assert.That(B.Structured(a.Combat.Events), Is.EqualTo(B.Structured(b.Combat.Events)));
            Assert.That(B.Structured(a.Combat.Kernel.InspectMonsters()), Is.EqualTo(B.Structured(b.Combat.Kernel.InspectMonsters())));
            Assert.That(B.Structured(a.Outcome), Is.EqualTo(B.Structured(b.Outcome)));
        }
        [TestCase(false, false, false, false)]
        [TestCase(true, false, false, false)]
        [TestCase(true, true, false, false)]
        [TestCase(true, true, true, false)]
        [TestCase(true, true, false, true)]
        public void EveryPrefixAndPartitionedFutureMatchesReference(bool twoFloors, bool descend, bool waiting, bool casualties)
        {
            var f = T.Dungeon(descend);
            if (!twoFloors) f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session,
                f.Runtime, FloorLifecycleAction.Deactivate, PhaseSixA4Tests.FloorTwo(f)));
            if (casualties) foreach (var c in f.Configuration.PhaseFiveB.Classes) c.LevelOneMaxHealth = 1;
            var before = f.Session.GetCurrentBytes(); var limits = T.Profile(f).Checkpoints;
            using var preparation = new CombatStartCapture.AdmissionPreparation();
            var source = T.Start(f); bool blocked = false; int prefixes = 0;
            try {
                while (true) {
                    var cp = source.ExportCheckpoint(limits);
                    var reused = Restore(cp, limits, preparation); var reference = Restore(cp, limits);
                    try {
                        Equal(reused, reference, limits);
                        Assert.That(B.Structured(Finish(reused, 1)), Is.EqualTo(B.Structured(Finish(reference, 7))), "prefix=" + prefixes);
                        Equal(reused, reference, limits);
                    } finally { reused.Abandon(); reference.Abandon(); }
                    prefixes++;
                    if (source.IsTerminal) break;
                    if (source.TransferWaiting) {
                        long release = source.TransferGate.Deadline + 5000;
                        source.ReleaseTransfer(release, 2, 40, release);
                    } else {
                        long due = source.NextDueTime().Value;
                        if (waiting && !blocked && source.AnnouncedAction == RunStepKind.FloorTransfer) {
                            source.BlockTransfer(due, 2); blocked = true;
                        } else source.AdvanceThrough(due, 1, source.NextOrdinal);
                    }
                }
                Assert.That(blocked, Is.EqualTo(waiting));
                if (casualties) Assert.That(source.Inspect().Members.Any(m => m.Health == 0), Is.True);
                CollectionAssert.AreEqual(before, f.Session.GetCurrentBytes());
                Assert.That(prefixes, Is.GreaterThan(2));
            } finally { source.Abandon(); }
        }
        [Test] public void MutableExecutionStateAndDisposalCannotAffectAnotherRestoration()
        {
            var f = T.Dungeon(); var limits = T.Profile(f).Checkpoints; var source = T.Start(f);
            var cp = source.ExportCheckpoint(limits); source.Abandon();
            var preparation = new CombatStartCapture.AdmissionPreparation();
            var a = Restore(cp, limits, preparation); var b = Restore(cp, limits, preparation); var reference = Restore(cp, limits);
            try {
                Assert.That(a.Party, Is.Not.SameAs(b.Party)); Assert.That(a.Combat.Kernel, Is.Not.SameAs(b.Combat.Kernel));
                Assert.That(a.Snapshot, Is.Not.SameAs(b.Snapshot)); Assert.That(a.Snapshot.CombatBounds, Is.Not.SameAs(b.Snapshot.CombatBounds));
                Assert.That(a.Traversal, Is.Not.SameAs(b.Traversal)); Assert.That(a.StartInputs, Is.Not.SameAs(b.StartInputs));
                T.ToTransfer(a); // actual movement/combat/loot mutates only a
                Equal(b, reference, limits);
                a.BlockTransfer(a.NextDueTime().Value, 2); a.Abandon();
                preparation.Dispose(); // executions own no borrowed writable state
                Assert.That(B.Structured(Finish(b, 1)), Is.EqualTo(B.Structured(Finish(reference, 7)))); Equal(b, reference, limits);
                var rejected = CombatCheckpointCodec.RestoreSupported(cp.GetBytes(), cp.Identity, cp.StartIdentity, cp.NextOrdinal,
                    limits, preparation: preparation);
                Assert.That(rejected.IsValid, Is.False); Assert.That(rejected.FailureReason, Is.EqualTo("run.checkpoint.transfer_evidence_mismatch"));
            } finally { a.Abandon(); b.Abandon(); reference.Abandon(); preparation.Dispose(); }
        }
        [Test] public void RejectedIntermediatePrefixDoesNotPoisonPreparationOrAuthorizeLaterEvidence()
        {
            var f = T.Dungeon(); var limits = T.Profile(f).Checkpoints; var e = T.Start(f);
            using var preparation = new CombatStartCapture.AdmissionPreparation();
            try {
                var initial = e.ExportCheckpoint(limits); Restore(initial, limits, preparation).Abandon();
                T.ToTransfer(e); e.BlockTransfer(e.NextDueTime().Value, 2);
                var blocked = e.ExportCheckpoint(limits); var forged = blocked.GetBytes();
                // Re-sign the envelope while corrupting committed inspection, after a valid preparation exists.
                forged[forged.Length - 33] ^= 1; Resign(forged);
                CompareFailure(forged, RunCheckpointCodec.Identity(forged), blocked.StartIdentity, blocked.NextOrdinal, limits, preparation);
                Restore(blocked, limits, preparation).Abandon();
                long release = e.TransferGate.Deadline + 5000; e.ReleaseTransfer(release, 2, 40, release);
                var released = e.ExportCheckpoint(limits); var badGate = released.GetBytes();
                using (var r = new BinaryReader(new MemoryStream(badGate))) {
                    r.ReadInt32(); RunCheckpointCodec.Text(r); RunCheckpointCodec.Text(r); RunCheckpointCodec.Blob(r);
                    r.ReadInt64(); r.ReadInt64(); r.ReadInt64(); r.ReadBoolean(); r.ReadBoolean(); r.ReadBoolean();
                    RunCheckpointCodec.Text(r); RunCheckpointCodec.Text(r);
                    r.ReadInt64(); r.ReadInt64(); r.ReadInt64(); r.ReadInt32(); r.ReadInt64(); r.ReadInt64();
                    Array.Copy(BitConverter.GetBytes(3), 0, badGate, (int)r.BaseStream.Position, 4); // forged vacancy sequence
                }
                Resign(badGate); CompareFailure(badGate, RunCheckpointCodec.Identity(badGate), released.StartIdentity, released.NextOrdinal, limits, preparation);
                var good = Restore(released, limits, preparation); var reference = Restore(released, limits);
                try { Equal(good, reference, limits); } finally { good.Abandon(); reference.Abandon(); }
            } finally { e.Abandon(); }
        }
        private static void Resign(byte[] bytes) => Array.Copy(RunCheckpointCodec.Hash(bytes.Take(bytes.Length - 32).ToArray()), 0, bytes, bytes.Length - 32, 32);
        private static void CompareFailure(byte[] bytes, string identity, string start, long ordinal, RunCheckpointLimits limits,
            CombatStartCapture.AdmissionPreparation preparation)
        {
            var original = CombatCheckpointCodec.RestoreSupported(bytes, identity, start, ordinal, limits);
            var reused = CombatCheckpointCodec.RestoreSupported(bytes, identity, start, ordinal, limits, preparation: preparation);
            Assert.That(original.IsValid, Is.False); Assert.That(reused.IsValid, Is.False);
            Assert.That(reused.FailureReason, Is.EqualTo(original.FailureReason));
            Assert.That(reused.Execution, Is.Null);
        }
        [Test] public void WarmPreparationPreservesEnvelopeStartAndWorkLimitRejections()
        {
            var f = T.Dungeon(); var limits = T.Profile(f).Checkpoints; var e = T.Start(f);
            using var preparation = new CombatStartCapture.AdmissionPreparation();
            try {
                e.Advance(2, 0); var cp = e.ExportCheckpoint(limits); var bytes = cp.GetBytes();
                Restore(cp, limits, preparation).Abandon();
                CompareFailure(bytes, new string('0', 64), cp.StartIdentity, cp.NextOrdinal, limits, preparation);
                CompareFailure(bytes, cp.Identity, new string('0', 64), cp.NextOrdinal, limits, preparation);
                CompareFailure(bytes, cp.Identity, cp.StartIdentity, cp.NextOrdinal - 1, limits, preparation);
                foreach (var limit in new[] { new RunCheckpointLimits(bytes.Length - 1, limits.MaximumReplayEvents, limits.MaximumCombatWork),
                    new RunCheckpointLimits(limits.MaximumBytes, cp.NextOrdinal - 1, limits.MaximumCombatWork),
                    new RunCheckpointLimits(limits.MaximumBytes, limits.MaximumReplayEvents, 0),
                    new RunCheckpointLimits(limits.MaximumBytes, limits.MaximumReplayEvents, e.Combat.Kernel.Configuration.MaximumWork - 1) })
                    CompareFailure(bytes, cp.Identity, cp.StartIdentity, cp.NextOrdinal, limit, preparation);
                var exact = new RunCheckpointLimits(bytes.Length, cp.NextOrdinal, limits.MaximumCombatWork);
                Restore(cp, exact, preparation).Abandon(); // different reader context must decode, remains valid at exact boundary
                foreach (string mode in new[] { "format", "hash", "prefix", "start", "rules", "count", "wrapper" }) {
                    var corrupt = cp.GetBytes(); string start = cp.StartIdentity;
                    if (mode == "format") corrupt[0] = 77;
                    else if (mode == "hash") corrupt[corrupt.Length - 1] ^= 1;
                    else if (mode == "prefix") { corrupt[corrupt.Length - 33] ^= 1; Resign(corrupt); }
                    else {
                        using var r = new BinaryReader(new MemoryStream(corrupt));
                        r.ReadInt32(); RunCheckpointCodec.Text(r); RunCheckpointCodec.Text(r);
                        int offset = (int)r.BaseStream.Position + 4; var wrapped = RunCheckpointCodec.Blob(r);
                        using var sr = new BinaryReader(new MemoryStream(wrapped));
                        RunCheckpointCodec.Text(sr); sr.ReadInt32(); int maximum = (int)sr.BaseStream.Position; sr.ReadInt64();
                        int inner = (int)sr.BaseStream.Position + 4; var combat = RunCheckpointCodec.Blob(sr);
                        if (mode == "wrapper") wrapped[maximum] ^= 1;
                        else if (mode == "start") wrapped[inner] ^= 1;
                        else if (mode == "rules") wrapped[inner + 8] ^= 1; // executable text
                        else Array.Copy(BitConverter.GetBytes(int.MaxValue), 0, wrapped, inner + 4, 4); // untrusted string count
                        Array.Copy(wrapped, 0, corrupt, offset, wrapped.Length); start = RunCheckpointCodec.Identity(wrapped); Resign(corrupt);
                    }
                    CompareFailure(corrupt, RunCheckpointCodec.Identity(corrupt), start, cp.NextOrdinal, limits, preparation);
                }
                Restore(cp, limits, preparation).Abandon();
            } finally { e.Abandon(); }
        }
        [Test] public void DifferentCapturedBytesAndLimitsUseIndependentDecoding()
        {
            var f = T.Dungeon(); var limits = T.Profile(f).Checkpoints;
            using var preparation = new CombatStartCapture.AdmissionPreparation();
            var original = T.Start(f); var cp = original.ExportCheckpoint(limits); original.Abandon();
            Restore(cp, limits, preparation).Abandon();
            var snapshot = PhaseSixA4Tests.Snapshot(f);
            var conditions = snapshot.MonsterStarts; conditions.Records[0].Health--;
            snapshot.RestoreCombatInputs(snapshot.CombatRules, snapshot.CombatBounds, conditions, snapshot.MonsterStartLevel,
                snapshot.MonsterReadinessNumerator, snapshot.MonsterReadinessDenominator, snapshot.MonsterStartOptions);
            var variants = new List<RunExecution> {
                new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot()).StartTimedCombatSnapshot(1, snapshot, T.Policy.Timing),
                new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot()).StartTimedCombatSnapshot(1, PhaseSixA4Tests.Snapshot(f), new RunLogicalTiming(3, 5, 7, 11, 13)),
                new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot()).StartTimedCombatSnapshot(2, PhaseSixA4Tests.Snapshot(f), T.Policy.Timing),
            };
            f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                FloorLifecycleAction.Deactivate, PhaseSixA4Tests.FloorTwo(f)));
            variants.Add(new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot()).StartTimedCombatSnapshot(1, PhaseSixA4Tests.Snapshot(f), T.Policy.Timing));
            foreach (var variant in variants) {
                variant.EnableTransfers(); var different = variant.ExportCheckpoint(limits);
                Assert.That(different.StartIdentity, Is.Not.EqualTo(cp.StartIdentity));
                var a = Restore(different, limits, preparation); var b = Restore(different, limits);
                try { Finish(a, 1); Finish(b, 7); Equal(a, b, limits); }
                finally { a.Abandon(); b.Abandon(); variant.Abandon(); }
            }
            Restore(cp, limits, preparation).Abandon(); // the original entry was never replaced
        }
        [TestCase(false)] [TestCase(true)] public void ActualLifetimeOperationReusesPreparationAndRetainsEveryReader(bool descend)
        {
            var f = T.Dungeon(descend); var e = T.Start(f); var before = f.Session.GetCurrentBytes();
            bool enabled = Metrics.Enabled, detailed = Metrics.DetailedEnabled;
            try {
                Metrics.Reset(); Metrics.Enabled = Metrics.DetailedEnabled = true;
                LivingDungeonLifetimeAssurance.Check(e, T.Profile(f));
                Assert.That(e.IsTerminal, Is.True);
                long readers = Metrics.BoundaryCalls[(int)Metrics.Boundary.SupportedReader];
                Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.StartPreparation], Is.EqualTo(1));
                Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.AdmissionStartReuse], Is.EqualTo(readers - 1));
                Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.PrefixReplay], Is.EqualTo(readers));
                Assert.That(Metrics.OriginCalls[(int)Metrics.Boundary.SupportedReader, (int)Metrics.Origin.LifetimeNormal], Is.EqualTo(e.NextOrdinal));
                if (descend) Assert.That(Metrics.OriginCalls[(int)Metrics.Boundary.SupportedReader, (int)Metrics.Origin.LifetimeAlternative], Is.GreaterThan(2));
                else Assert.That(Metrics.OriginCalls[(int)Metrics.Boundary.SupportedReader, (int)Metrics.Origin.LifetimeAlternative], Is.Zero);
                CollectionAssert.AreEqual(before, f.Session.GetCurrentBytes());
            } finally { Metrics.Enabled = enabled; Metrics.DetailedEnabled = detailed; e.Abandon(); }
        }
        [Serializable] private sealed class Frozen
        { public string Base64, StartIdentity, CheckpointIdentity; public long NextOrdinal; }
        [TestCase("historical-b1-format1.json")] [TestCase("historical-c2-format2.json")]
        public void WarmAdmissionPreparationDoesNotChangeHistoricalReaders(string file)
        {
            var f = T.Dungeon(); var limits = T.Profile(f).Checkpoints; var source = T.Start(f);
            using var preparation = new CombatStartCapture.AdmissionPreparation();
            try {
                Restore(source.ExportCheckpoint(limits), limits, preparation).Abandon();
                var golden = JsonUtility.FromJson<Frozen>(File.ReadAllText("Tools/Presentation/Fixtures/" + file));
                var bytes = Convert.FromBase64String(golden.Base64);
                var historical = CombatCheckpointCodec.RestoreSupported(bytes, golden.CheckpointIdentity, golden.StartIdentity,
                    golden.NextOrdinal, limits, preparation: preparation);
                Assert.That(historical.IsValid, Is.True, historical.FailureReason);
                try { CollectionAssert.AreEqual(bytes, historical.Execution.ExportCheckpoint(limits).GetBytes()); }
                finally { historical.Execution.Abandon(); }
                bytes[bytes.Length - 1] ^= 1;
                CompareFailure(bytes, RunCheckpointCodec.Identity(bytes), golden.StartIdentity, golden.NextOrdinal, limits, preparation);
            } finally { source.Abandon(); }
        }
        [Test] public void AssuranceExceptionCleansUpBeforeTheNextOwnedOperation()
        {
            var f = T.Dungeon(); var profile = T.Profile(f); var e = T.Start(f);
            var bounded = new LivingDungeonProfile(profile.Id, profile.MaximumActive, profile.MaximumRetained,
                profile.MaximumCheckpointBytes, 2, profile.MaximumTotalReplay, profile.MaximumBaseSaveBytes,
                profile.MaximumOwnerBytes, profile.QualificationTiming, profile.QualificationPostureId, profile.BaseBudget, profile.MaximumCombatWork);
            try {
                Assert.That(Assert.Throws<InvalidOperationException>(() => LivingDungeonLifetimeAssurance.Check(e, bounded)).Message,
                    Is.EqualTo("run.checkpoint.replay_limit"));
            } finally { e.Abandon(); }
            ActualLifetimeOperationReusesPreparationAndRetainsEveryReader(true);
        }
    }
}
#endif
