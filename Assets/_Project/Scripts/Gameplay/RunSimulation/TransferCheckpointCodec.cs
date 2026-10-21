using System;
using System.IO;
using System.Linq;
using System.Text;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    internal static class TransferCheckpointCodec
    {
        internal const int Format = 3;
        internal const string Contract = "run.execution.foreground-transfer.v2";
        internal const string Evidence = "run.combat.transfer-prefix.v2";

        internal static byte[] WrapStart(byte[] combatStart, long maximum) => RunCheckpointCodec.Bytes(w => {
            RunCheckpointCodec.Text(w, Contract); w.Write(Format); w.Write(maximum); RunCheckpointCodec.Blob(w, combatStart);
        });
        private static void Gate(BinaryWriter w, TransferControl g)
        {
            w.Write(g != null); if (g == null) return;
            RunCheckpointCodec.Text(w, g.Source); RunCheckpointCodec.Text(w, g.Destination);
            w.Write(g.DecisionOrdinal); w.Write(g.BlockOrdinal); w.Write(g.Deadline); w.Write(g.BlockerSequence);
            w.Write(g.ReleaseOrdinal); w.Write(g.ReleaseTime); w.Write(g.VacancySequence); w.Write(g.VacancyOrdinal); w.Write(g.VacancyTime);
        }
        private static TransferControl Gate(BinaryReader r) => !RunCheckpointCodec.Flag(r) ? null : new TransferControl {
            Source = RunCheckpointCodec.Text(r), Destination = RunCheckpointCodec.Text(r),
            DecisionOrdinal = r.ReadInt64(), BlockOrdinal = r.ReadInt64(), Deadline = r.ReadInt64(), BlockerSequence = r.ReadInt32(),
            ReleaseOrdinal = r.ReadInt64(), ReleaseTime = r.ReadInt64(), VacancySequence = r.ReadInt32(),
            VacancyOrdinal = r.ReadInt64(), VacancyTime = r.ReadInt64() };
        internal static byte[] ControlBytes(TransferControl gate) => RunCheckpointCodec.Bytes(w => Gate(w, gate));
        private static byte[] Inspection(RunExecution e) => RunCheckpointCodec.Bytes(w => {
            RunCheckpointCodec.Text(w, Evidence); RunCheckpointCodec.Blob(w, CombatCheckpointCodec.Inspection(e));
            RunCheckpointCodec.Text(w, e.OccupiedFloor); w.Write(e.TransferWaiting); w.Write(e.TransferReleased); Gate(w, e.TransferGate);
        });
        internal static byte[] Fold(byte[] prefix, RunStep step, RunExecution e) => RunCheckpointCodec.Hash(RunCheckpointCodec.Bytes(w => {
            RunCheckpointCodec.Text(w, Evidence);
            RunCheckpointCodec.Blob(w, CombatCheckpointCodec.Fold(prefix, step, e));
            RunCheckpointCodec.Blob(w, Inspection(e));
        }));
        internal static RunCheckpoint Export(RunExecution e, RunCheckpointLimits limits)
        {
            if (limits == null || e.NextOrdinal > limits.MaximumReplayEvents || e.Combat.Kernel.Configuration.MaximumWork > limits.MaximumCombatWork)
                throw new InvalidOperationException("run.checkpoint.replay_limit");
            var payload = RunCheckpointCodec.Bytes(w => {
                w.Write(Format); RunCheckpointCodec.Text(w, Contract); RunCheckpointCodec.Text(w, Evidence);
                RunCheckpointCodec.Blob(w, e.StartInputs); w.Write(e.NextOrdinal); w.Write(e.Inspect().LogicalTime);
                w.Write(e.AcceptedDeadline); w.Write(e.IsTerminal); w.Write(e.OutcomeTaken); Gate(w, e.TransferGate);
                RunCheckpointCodec.Blob(w, e.PrefixDigest); RunCheckpointCodec.Blob(w, Inspection(e));
            });
            if (payload.LongLength + 32 > limits.MaximumBytes) throw new InvalidOperationException("run.checkpoint.byte_limit");
            return new RunCheckpoint(RunCheckpointCodec.Bytes(w => { w.Write(payload); w.Write(RunCheckpointCodec.Hash(payload)); }), e.StartIdentity, e.NextOrdinal);
        }
        internal static RunCheckpointRestoreResult Restore(byte[] bytes, string identity, string startIdentity,
            long expectedOrdinal, RunCheckpointLimits limits, bool presentation)
        {
            RunExecution e = null;
            try {
                if (RunCheckpointCodec.Identity(bytes) != identity || limits.MaximumCombatWork <= 0)
                    throw new InvalidDataException();
                var payload = bytes.Take(bytes.Length - 32).ToArray();
                if (!RunCheckpointCodec.Hash(payload).SequenceEqual(bytes.Skip(payload.Length))) throw new InvalidDataException();
                using var r = new BinaryReader(new MemoryStream(payload), new UTF8Encoding(false, true));
                if (r.ReadInt32() != Format || RunCheckpointCodec.Text(r) != Contract || RunCheckpointCodec.Text(r) != Evidence)
                    throw new InvalidDataException();
                var start = RunCheckpointCodec.Blob(r);
                if (RunCheckpointCodec.Identity(start) != startIdentity) throw new InvalidDataException();
                long ordinal = r.ReadInt64(), time = r.ReadInt64(), deadline = r.ReadInt64();
                bool terminal = RunCheckpointCodec.Flag(r), consumed = RunCheckpointCodec.Flag(r);
                var gate = Gate(r); var digest = RunCheckpointCodec.Blob(r); var inspection = RunCheckpointCodec.Blob(r); RunCheckpointCodec.End(r);
                if (ordinal != expectedOrdinal || ordinal < 0 || ordinal > limits.MaximumReplayEvents || time < 0 ||
                    deadline < -1 || deadline >= 0 && deadline < time || consumed && !terminal || digest.Length != 32)
                    throw new InvalidDataException();
                if (gate?.ReleaseOrdinal >= 0 && gate.VacancyOrdinal >= limits.MaximumReplayEvents) throw new InvalidDataException();
                using (var sr = new BinaryReader(new MemoryStream(start), new UTF8Encoding(false, true))) {
                    if (RunCheckpointCodec.Text(sr) != Contract || sr.ReadInt32() != Format) throw new InvalidDataException();
                    long maximum = sr.ReadInt64(); var inner = RunCheckpointCodec.Blob(sr); RunCheckpointCodec.End(sr);
                    e = CombatStartCapture.Recreate(inner, limits, DungeonSpatial.MonsterRecoveryRules.Executable);
                    e.EnableTransfers();
                    if (e.MaximumEvents != maximum || !e.StartInputs.SequenceEqual(start)) throw new InvalidDataException();
                }
                if (presentation) e.PresentationSteps = new System.Collections.Generic.List<RunStep>();
                for (long i = 0; i < ordinal; i++) {
                    if (e.IsTerminal) throw new InvalidDataException();
                    if (gate != null && i == gate.BlockOrdinal) {
                        e.BlockTransfer(gate.Deadline, gate.BlockerSequence);
                        if (e.TransferGate.DecisionOrdinal != gate.DecisionOrdinal || e.TransferGate.Source != gate.Source ||
                            e.TransferGate.Destination != gate.Destination) throw new InvalidDataException();
                    } else if (gate != null && i == gate.ReleaseOrdinal) {
                        e.ReleaseTransfer(gate.ReleaseTime, gate.VacancySequence, gate.VacancyOrdinal, gate.VacancyTime);
                    } else {
                        if (e.TransferWaiting || e.Advance(1, i).Length != 1) throw new InvalidDataException();
                    }
                }
                if (e.IsTerminal != terminal || e.Inspect().LogicalTime != time || !e.PrefixDigest.SequenceEqual(digest) ||
                    !Inspection(e).SequenceEqual(inspection)) throw new InvalidDataException();
                e.RestoreMetadata(deadline, consumed);
                if (!Export(e, limits).GetBytes().SequenceEqual(bytes)) throw new InvalidDataException();
                return new RunCheckpointRestoreResult(e, null);
            } catch (Exception error) when (error is IOException || error is InvalidDataException || error is ArgumentException || error is InvalidOperationException ||
                error is OverflowException || error is NullReferenceException || error is IndexOutOfRangeException ||
                error is System.Collections.Generic.KeyNotFoundException || error is FormatException) {
                e?.Abandon(); return new RunCheckpointRestoreResult(null, "run.checkpoint.transfer_evidence_mismatch");
            }
        }
    }
}
