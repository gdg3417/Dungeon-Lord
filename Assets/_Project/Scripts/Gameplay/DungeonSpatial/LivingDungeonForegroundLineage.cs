using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.RunSimulation;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // Transaction-local proof, never serialized. Only a verified terminal predecessor
    // followed by its exact authorized settlement can establish a vacancy.
    internal sealed class LivingDungeonForegroundLineage
    {
        private byte[] latest;
        private readonly Dictionary<int, Vacancy> vacancies = new Dictionary<int, Vacancy>();
        private readonly Dictionary<int, string> released = new Dictionary<int, string>();
        private sealed class Vacancy
        {
            internal string Floor;
            internal int Sequence;
            internal long Ordinal, Time;
        }
        internal LivingDungeonForegroundLineage(byte[] before) { latest = (byte[])before.Clone(); }
        internal void Advance(byte[] before, byte[] after)
        {
            if (!latest.SequenceEqual(before)) throw new InvalidOperationException("living.transfer.stale_lineage");
            latest = (byte[])after.Clone();
        }
        internal void BeginBoundary() { vacancies.Clear(); released.Clear(); }
        internal void Retired(DetachedCompleteSaveValidationResult before, DetachedCompleteSaveValidationResult after,
            LivingDungeonProfile profile, int sequence, RunExecution terminal)
        {
            if (!latest.SequenceEqual(after.GetBytes()) || after.LivingDungeon.Parties.Any(p => p.Sequence == sequence) ||
                UnityEngine.JsonUtility.FromJson<SaveRoot>(System.Text.Encoding.UTF8.GetString(after.GetBytes())).primary.runHistory.LatestOutcome?.RunId != "run-" + sequence)
                throw new InvalidOperationException("living.transfer.vacancy_unproven");
            var party = before.LivingDungeon.Parties.Single(p => p.Sequence == sequence);
            var verified = LivingDungeonContracts.Restore(party, profile);
            try {
                var step = verified.Inspect().LastStep;
                if (!verified.IsTerminal || verified.OutcomeTaken || !terminal.OutcomeTaken || step?.Kind != RunStepKind.Terminal ||
                    verified.StartIdentity != terminal.StartIdentity || verified.NextOrdinal != terminal.NextOrdinal ||
                    !verified.PrefixDigest.SequenceEqual(terminal.PrefixDigest))
                    throw new InvalidOperationException("living.transfer.vacancy_unproven");
                vacancies.Add(sequence, new Vacancy { Sequence = sequence, Ordinal = step.Value.Ordinal,
                    Time = checked(party.AdmittedAt + step.Value.LogicalTime),
                    Floor = verified.TransferContract ? verified.OccupiedFloor : verified.Snapshot.Floors[0].FloorInstanceId });
            } finally { verified.Abandon(); }
        }
        internal bool Release(byte[] predecessor, LivingDungeonParty party, RunExecution execution, long time,
            ISet<string> occupied, LivingDungeonProfile profile)
        {
            if (!latest.SequenceEqual(predecessor)) throw new InvalidOperationException("living.transfer.stale_lineage");
            if (!execution.TransferWaiting || occupied.Contains(execution.TransferGate.Destination)) return false;
            var gate = execution.TransferGate;
            if (!vacancies.TryGetValue(gate.BlockerSequence, out var cause) || cause.Floor != gate.Destination ||
                cause.Time > time || cause.Time < checked(party.AdmittedAt + gate.Deadline))
                throw new InvalidOperationException("living.transfer.vacancy_unproven");
            execution.ReleaseTransfer(checked(time - party.AdmittedAt), cause.Sequence, cause.Ordinal,
                checked(cause.Time - party.AdmittedAt));
            // Released is a real replayable execution prefix, but never a durable
            // half-published canonical boundary. Complete its pending transfer now.
            execution.AdvanceThrough(checked(time - party.AdmittedAt), 1, execution.NextOrdinal);
            LivingDungeonScheduling.Pin(party, execution, profile);
            released[party.Sequence] = party.CheckpointIdentity;
            return true;
        }
        internal ReleasePermit Permit(byte[] predecessor) => latest.SequenceEqual(predecessor)
            ? new ReleasePermit(predecessor, released) : throw new InvalidOperationException("living.transfer.stale_lineage");
        internal sealed class ReleasePermit
        {
            private readonly byte[] before;
            private readonly Dictionary<int, string> identities;
            internal ReleasePermit(byte[] source, Dictionary<int, string> values)
            { before = (byte[])source.Clone(); identities = new Dictionary<int, string>(values); }
            internal bool Allows(byte[] predecessor, LivingDungeonParty p) => before.SequenceEqual(predecessor) &&
                identities.TryGetValue(p.Sequence, out string identity) && identity == p.CheckpointIdentity;
        }
    }
}
