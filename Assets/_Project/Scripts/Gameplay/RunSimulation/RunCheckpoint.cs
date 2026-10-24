using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    // Work policy is supplied by the caller. These are not gameplay tuning or production defaults.
    public sealed class RunCheckpointLimits
    {
        public int MaximumBytes { get; }
        public long MaximumReplayEvents { get; }
        public long MaximumCombatWork { get; }
        public RunCheckpointLimits(int maximumBytes, long maximumReplayEvents, long maximumCombatWork = 0)
        {
            if (maximumBytes <= 0 || maximumReplayEvents < 0 || maximumCombatWork < 0) throw new ArgumentException("run.checkpoint.invalid_limits");
            MaximumBytes = maximumBytes; MaximumReplayEvents = maximumReplayEvents; MaximumCombatWork = maximumCombatWork;
        }
    }

    public sealed class RunCheckpoint
    {
        private readonly byte[] bytes;
        public string StartIdentity { get; }
        public string Identity => RunCheckpointCodec.Identity(bytes);
        public long NextOrdinal { get; }
        internal RunCheckpoint(byte[] bytes, string identity, long ordinal)
        { this.bytes = (byte[])bytes.Clone(); StartIdentity = identity; NextOrdinal = ordinal; }
        public byte[] GetBytes() => (byte[])bytes.Clone();
    }

    public sealed class RunCheckpointRestoreResult
    {
        public RunExecution Execution { get; }
        public string FailureReason { get; }
        public bool IsValid => Execution != null;
        internal RunCheckpointRestoreResult(RunExecution execution, string failure)
        { Execution = execution; FailureReason = failure; }
    }

    internal static class RunCheckpointCodec
    {
        internal const int Format = 1;
        // Supported executable implementation, not a promise to emulate historical code.
        internal const string Rules = "run.executable.living-dungeon-b1.v1";
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        internal static byte[] Hash(byte[] value) { using (var hash = SHA256.Create()) return hash.ComputeHash(value); }
        internal static string Identity(byte[] value) => BitConverter.ToString(Hash(value)).Replace("-", "").ToLowerInvariant();
        internal static byte[] Bytes(Action<BinaryWriter> write)
        { using (var stream = new MemoryStream()) { using (var writer = new BinaryWriter(stream, Utf8, true)) write(writer); return stream.ToArray(); } }
        internal static void Text(BinaryWriter writer, string value)
        { if (value == null) { writer.Write(-1); return; } Blob(writer, Utf8.GetBytes(value)); }
        internal static string Text(BinaryReader reader)
        { var bytes = Blob(reader, true); return bytes == null ? null : Utf8.GetString(bytes); }
        internal static void Blob(BinaryWriter writer, byte[] value)
        { writer.Write(value.Length); writer.Write(value); }
        internal static byte[] Blob(BinaryReader reader, bool nullable = false)
        {
            int length = reader.ReadInt32();
            if (nullable && length == -1) return null;
            if (length < 0 || length > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new InvalidDataException("run.checkpoint.encoding");
            return reader.ReadBytes(length);
        }
        internal static bool Flag(BinaryReader reader)
        { byte value = reader.ReadByte(); if (value > 1) throw new InvalidDataException("run.checkpoint.encoding"); return value == 1; }
        internal static void End(BinaryReader reader)
        { if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("run.checkpoint.encoding"); }

        private static byte[] Inspection(RunExecutionInspection view) => Bytes(w => {
            Text(w, view.RunId); w.Write(view.NextOrdinal); w.Write(view.LogicalTime); w.Write(view.HasTiming); w.Write(view.Terminal);
            w.Write(view.CarriedLootValue); w.Write(view.ResolvedEncounters); w.Write(view.ReachedLootInteractions);
            w.Write(view.Members.Count);
            foreach (var m in view.Members) { w.Write(m.Ordinal); Text(w, m.ClassId); w.Write(m.Health); w.Write(m.MaxHealth); }
        });
        internal static byte[] Fold(byte[] prefix, RunStep step, RunExecutionInspection view) => Hash(Bytes(w => {
            w.Write(prefix); Text(w, step.RunId); w.Write(step.Ordinal); w.Write(step.LogicalTime); w.Write((int)step.Kind);
            w.Write(step.FloorIndex); Text(w, step.RoomId); Text(w, step.AssignmentId); w.Write(step.Position.X); w.Write(step.Position.Y);
            w.Write(step.Distance); w.Write((int)step.PositionSpace); w.Write(step.ContinueChosen.HasValue);
            if (step.ContinueChosen.HasValue) w.Write(step.ContinueChosen.Value);
            w.Write(step.CarriedLootValueAfter); var e = step.Encounter; w.Write(e != null);
            if (e != null) {
                Text(w, e.RuleSourceId); w.Write(e.FloorIndex); w.Write(e.RoomIndex); Text(w, e.AssignmentId); Text(w, e.OptionId);
                w.Write(e.MemberOrdinal); w.Write(e.Severity); w.Write(e.TrapExpertise); w.Write(e.UnmitigatedDamage); w.Write(e.Damage);
                w.Write(e.HealthBefore); w.Write(e.HealthAfter);
            }
            Blob(w, Inspection(view));
        }));

        internal static RunCheckpoint Export(RunExecution execution, long deadline, bool taken, byte[] prefix, RunCheckpointLimits limits)
        {
            if (limits == null) throw new ArgumentNullException(nameof(limits));
            if (execution.NextOrdinal > limits.MaximumReplayEvents) throw new InvalidOperationException("run.checkpoint.replay_limit");
            if (execution.StartInputs.Length > limits.MaximumBytes) throw new InvalidOperationException("run.checkpoint.byte_limit");
            var view = execution.Inspect();
            var payload = Bytes(w => {
                w.Write(Format); Text(w, Rules); Blob(w, execution.StartInputs);
                w.Write(view.NextOrdinal); w.Write(view.LogicalTime); w.Write(deadline); w.Write(view.Terminal); w.Write(taken);
                Blob(w, prefix); Blob(w, Inspection(view));
            });
            if (payload.LongLength + 32 > limits.MaximumBytes) throw new InvalidOperationException("run.checkpoint.byte_limit");
            var bytes = Bytes(w => { w.Write(payload); w.Write(Hash(payload)); });
            return new RunCheckpoint(bytes, execution.StartIdentity, view.NextOrdinal);
        }

        internal static RunCheckpointRestoreResult Restore(byte[] source, string expectedCheckpointIdentity,
            string expectedStartIdentity, long expectedOrdinal, RunCheckpointLimits limits)
        {
            RunExecution candidate = null;
            try
            {
                if (limits == null || source == null || source.Length < 32 || source.Length > limits.MaximumBytes)
                    return Failed("run.checkpoint.byte_limit");
                // Own bytes before validation; the caller cannot mutate the parse/replay input.
                var bytes = (byte[])source.Clone();
                if (expectedCheckpointIdentity == null || Identity(bytes) != expectedCheckpointIdentity)
                    return Failed("run.checkpoint.commit_mismatch");
                var payload = bytes.Take(bytes.Length - 32).ToArray();
                if (!Hash(payload).SequenceEqual(bytes.Skip(payload.Length))) return Failed("run.checkpoint.encoding");
                using (var reader = new BinaryReader(new MemoryStream(payload), Utf8))
                {
                    if (reader.ReadInt32() != Format) return Failed("run.checkpoint.unsupported_format");
                    if (Text(reader) != Rules) return Failed("run.checkpoint.unsupported_rules");
                    var start = Blob(reader);
                    if (expectedStartIdentity == null || Identity(start) != expectedStartIdentity) return Failed("run.checkpoint.start_mismatch");
                    long ordinal = reader.ReadInt64(), time = reader.ReadInt64(), deadline = reader.ReadInt64();
                    bool terminal = Flag(reader), taken = Flag(reader);
                    var prefix = Blob(reader); var inspection = Blob(reader); End(reader);
                    if (ordinal != expectedOrdinal || ordinal < 0) return Failed("run.checkpoint.stale_ordinal");
                    if (ordinal > limits.MaximumReplayEvents) return Failed("run.checkpoint.replay_limit");
                    if (time < 0 || deadline < -1 || (deadline >= 0 && deadline < time) || (taken && !terminal) || prefix.Length != 32)
                        return Failed("run.checkpoint.evidence_mismatch");
                    using (ForegroundQualificationMetrics.Trace(ForegroundQualificationMetrics.Boundary.StartRecreation))
                        candidate = RunStartCapture.Recreate(start);
                    using (ForegroundQualificationMetrics.Trace(ForegroundQualificationMetrics.Boundary.PrefixReplay))
                    for (long i = 0; i < ordinal; i++)
                    {
                        if (candidate.IsTerminal) return Failed("run.checkpoint.evidence_mismatch", candidate);
                        candidate.Advance(1, i);
                    }
                    var view = candidate.Inspect();
                    if (view.Terminal != terminal || view.LogicalTime != time || !candidate.PrefixDigest.SequenceEqual(prefix) ||
                        !Inspection(view).SequenceEqual(inspection) || (deadline >= 0 && !view.HasTiming))
                        return Failed("run.checkpoint.evidence_mismatch", candidate);
                    candidate.RestoreMetadata(deadline, taken);
                    if (!candidate.ExportCheckpoint(limits).GetBytes().SequenceEqual(bytes)) return Failed("run.checkpoint.encoding", candidate);
                    return new RunCheckpointRestoreResult(candidate, null);
                }
            }
            catch (Exception error) when (error is ArgumentException || error is IOException || error is InvalidOperationException ||
                error is OverflowException || error is NullReferenceException || error is IndexOutOfRangeException)
            { return Failed("run.checkpoint.invalid_inputs", candidate); }
        }
        private static RunCheckpointRestoreResult Failed(string reason, RunExecution candidate = null)
        { candidate?.Abandon(); return new RunCheckpointRestoreResult(null, reason); }
    }
}
