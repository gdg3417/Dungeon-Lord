using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    internal static class CombatCheckpointCodec
    {
        internal const int Format = 2;
        // Independent of both checkpoint and executable versions. B1 Fold is frozen.
        internal const string Evidence = "run.combat.prefix.v1";
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        internal static byte[] Inspection(RunExecution execution) => RunCheckpointCodec.Bytes(w => {
            var v = execution.Inspect();
            RunCheckpointCodec.Text(w, Evidence); RunCheckpointCodec.Text(w, v.RunId);
            w.Write(v.NextOrdinal); w.Write(v.LogicalTime); w.Write(v.HasTiming); w.Write(v.Terminal);
            w.Write(v.CarriedLootValue); w.Write(v.ResolvedEncounters); w.Write(v.ReachedLootInteractions);
            w.Write(v.Members.Count);
            foreach (var m in v.Members) { w.Write(m.Ordinal); RunCheckpointCodec.Text(w, m.ClassId); w.Write(m.Health); w.Write(m.MaxHealth); }
            var c = execution.Combat; RunCheckpointCodec.Text(w, c.Kernel.RulesId);
            w.Write(execution.CommittedCombatWork); w.Write(c.Kernel.EventCount); w.Write(c.Kernel.CompletedCount); w.Write(c.Events.Count);
            var monsters = c.Kernel.InspectMonsters(); w.Write(monsters.Length);
            foreach (var m in monsters) {
                RunCheckpointCodec.Text(w, m.AssignmentId); RunCheckpointCodec.Text(w, m.OptionId);
                RunCheckpointCodec.Text(w, m.FloorId); RunCheckpointCodec.Text(w, m.RoomId);
                w.Write(m.MaxHealth); w.Write(m.Health); w.Write(m.InitialHealth); w.Write(m.InitialRevivalRequired); w.Write(m.RevivalRequired); w.Write(m.Defeated);
            }
        });
        internal static byte[] Fold(byte[] prefix, RunStep step, RunExecution execution) => Fold(prefix, step, execution, out _);
        internal static byte[] Fold(byte[] prefix, RunStep step, RunExecution execution, out byte[] inspection)
        {
            byte[] capturedInspection = null;
            var folded = RunCheckpointCodec.Hash(RunCheckpointCodec.Bytes(w => {
            RunCheckpointCodec.Text(w, Evidence);
            // Includes all established route/trap/loot/branch/floor evidence unchanged.
            RunCheckpointCodec.Blob(w, RunCheckpointCodec.Fold(prefix, step, execution.Inspect()));
            var events = step.CombatEvents; w.Write(events?.Count ?? 0);
            if (events != null) foreach (var e in events) {
                RunCheckpointCodec.Text(w, e.ExecutableId); RunCheckpointCodec.Text(w, e.RuleSourceId);
                RunCheckpointCodec.Text(w, e.RunId); RunCheckpointCodec.Text(w, e.EncounterId);
                // Step receipts acquire their committed boundary in Combat.Record.
                w.Write(step.Ordinal); w.Write(e.Ordinal); w.Write(e.ActionOrdinal); w.Write(e.Round);
                RunCheckpointCodec.Text(w, e.FloorId); RunCheckpointCodec.Text(w, e.RoomId);
                w.Write((int)e.Kind); w.Write((int)e.Reason); RunCheckpointCodec.Text(w, e.ActorId); RunCheckpointCodec.Text(w, e.TargetId);
                RunCheckpointCodec.Text(w, e.AttackId); RunCheckpointCodec.Text(w, e.TargetingId);
                w.Write((int)e.PositionSpace); w.Write(e.SourcePosition.HasValue);
                if (e.SourcePosition.HasValue) { w.Write(e.SourcePosition.Value.X); w.Write(e.SourcePosition.Value.Y); }
                w.Write(e.TargetPosition.HasValue);
                if (e.TargetPosition.HasValue) { w.Write(e.TargetPosition.Value.X); w.Write(e.TargetPosition.Value.Y); }
                w.Write(e.HealthBefore); w.Write(e.HealthAfter); w.Write(e.Damage); w.Write(e.Defeated);
            }
            capturedInspection = Inspection(execution);
            RunCheckpointCodec.Blob(w, capturedInspection);
            }));
            inspection = capturedInspection;
            return folded;
        }
        internal static RunCheckpoint Export(RunExecution execution, long deadline, bool taken, RunCheckpointLimits limits)
        {
            if (limits == null) throw new ArgumentNullException(nameof(limits));
            if (execution.Combat.Kernel.Configuration.MaximumWork > limits.MaximumCombatWork)
                throw new InvalidOperationException("run.checkpoint.combat_limit");
            if (execution.NextOrdinal > limits.MaximumReplayEvents) throw new InvalidOperationException("run.checkpoint.replay_limit");
            if (execution.StartInputs.Length > limits.MaximumBytes) throw new InvalidOperationException("run.checkpoint.byte_limit");
            var v = execution.Inspect();
            var payload = RunCheckpointCodec.Bytes(w => {
                w.Write(Format); RunCheckpointCodec.Text(w, execution.Combat.Kernel.RulesId); RunCheckpointCodec.Text(w, Evidence);
                RunCheckpointCodec.Blob(w, execution.StartInputs);
                w.Write(v.NextOrdinal); w.Write(v.LogicalTime); w.Write(deadline); w.Write(v.Terminal); w.Write(taken);
                RunCheckpointCodec.Blob(w, execution.PrefixDigest); RunCheckpointCodec.Blob(w, Inspection(execution));
            });
            if (payload.LongLength + 32 > limits.MaximumBytes) throw new InvalidOperationException("run.checkpoint.byte_limit");
            return new RunCheckpoint(RunCheckpointCodec.Bytes(w => { w.Write(payload); w.Write(RunCheckpointCodec.Hash(payload)); }), execution.StartIdentity, v.NextOrdinal);
        }
        internal static RunCheckpointRestoreResult RestoreSupported(byte[] source, string checkpointIdentity, string startIdentity,
            long expectedOrdinal, RunCheckpointLimits limits, bool retainPresentation = false,
            CombatStartCapture.AdmissionPreparation preparation = null)
        {
            using var diagnostic = ForegroundQualificationMetrics.Trace(ForegroundQualificationMetrics.Boundary.SupportedReader, depth: expectedOrdinal);
            using var replayMeasurement = ForegroundQualificationMetrics.Measure(ForegroundQualificationMetrics.Area.CheckpointReplay);
            // Dispatch only after the same independent byte/work envelope as the historical reader.
            if (source == null || limits == null || source.Length < 32 || source.Length > limits.MaximumBytes)
                return Failed("run.checkpoint.byte_limit");
            var bytes = (byte[])source.Clone();
            int format = BitConverter.ToInt32(bytes, 0);
            if (format == TransferCheckpointCodec.Format) return TransferCheckpointCodec.Restore(bytes, checkpointIdentity, startIdentity, expectedOrdinal, limits, retainPresentation, preparation);
            if (format == RunCheckpointCodec.Format) return RunCheckpointCodec.Restore(bytes, checkpointIdentity, startIdentity, expectedOrdinal, limits);
            if (format != Format) return Failed("run.checkpoint.unsupported_format");
            RunExecution candidate = null;
            try {
                if (checkpointIdentity == null || RunCheckpointCodec.Identity(bytes) != checkpointIdentity) return Failed("run.checkpoint.commit_mismatch");
                if (limits.MaximumCombatWork == 0) return Failed("run.checkpoint.unsupported_rules");
                var payload = bytes.Take(bytes.Length - 32).ToArray();
                if (!RunCheckpointCodec.Hash(payload).SequenceEqual(bytes.Skip(payload.Length))) return Failed("run.checkpoint.encoding");
                using (var r = new BinaryReader(new MemoryStream(payload), Utf8)) {
                    r.ReadInt32(); var executable = RunCheckpointCodec.Text(r);
                    if (executable != CombatC0Config.Executable && executable != MonsterRecoveryRules.Executable) return Failed("run.checkpoint.unsupported_rules");
                    if (RunCheckpointCodec.Text(r) != Evidence) return Failed("run.checkpoint.unsupported_evidence");
                    var start = RunCheckpointCodec.Blob(r);
                    if (startIdentity == null || RunCheckpointCodec.Identity(start) != startIdentity) return Failed("run.checkpoint.start_mismatch");
                    long ordinal = r.ReadInt64(), time = r.ReadInt64(), deadline = r.ReadInt64();
                    bool terminal = RunCheckpointCodec.Flag(r), taken = RunCheckpointCodec.Flag(r);
                    var prefix = RunCheckpointCodec.Blob(r); var inspection = RunCheckpointCodec.Blob(r); RunCheckpointCodec.End(r);
                    if (ordinal < 0 || ordinal != expectedOrdinal) return Failed("run.checkpoint.stale_ordinal");
                    if (ordinal > limits.MaximumReplayEvents) return Failed("run.checkpoint.replay_limit");
                    if (time < 0 || deadline < -1 || deadline >= 0 && deadline < time || taken && !terminal || prefix.Length != 32)
                        return Failed("run.checkpoint.evidence_mismatch");
                    candidate = CombatStartCapture.Recreate(start, limits, executable);
                    if (retainPresentation) candidate.PresentationSteps = new System.Collections.Generic.List<RunStep>();
                    using (ForegroundQualificationMetrics.Trace(ForegroundQualificationMetrics.Boundary.PrefixReplay))
                    for (long i = 0; i < ordinal; i++) {
                        if (candidate.IsTerminal) return Failed("run.checkpoint.evidence_mismatch", candidate);
                        candidate.Advance(1, i);
                    }
                    var v = candidate.Inspect();
                    if (v.Terminal != terminal || v.LogicalTime != time || !prefix.SequenceEqual(candidate.PrefixDigest) ||
                        !inspection.SequenceEqual(Inspection(candidate))) return Failed("run.checkpoint.evidence_mismatch", candidate);
                    candidate.RestoreMetadata(deadline, taken);
                    if (!bytes.SequenceEqual(candidate.ExportCheckpoint(limits).GetBytes())) return Failed("run.checkpoint.encoding", candidate);
                    return new RunCheckpointRestoreResult(candidate, null);
                }
            }
            catch (Exception e) when (e is ArgumentException || e is IOException || e is InvalidDataException || e is InvalidOperationException ||
                e is OverflowException || e is NullReferenceException || e is IndexOutOfRangeException || e is System.Collections.Generic.KeyNotFoundException)
            { return Failed("run.checkpoint.invalid_inputs", candidate); }
        }
        private static RunCheckpointRestoreResult Failed(string reason, RunExecution execution = null)
        { execution?.Abandon(); return new RunCheckpointRestoreResult(null, reason); }
    }
}
