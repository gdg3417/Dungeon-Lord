using System;
using System.Linq;
using System.IO;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    // Versioned execution controls. These do not mutate the captured C2 iterator inputs.
    internal sealed class TransferControl
    {
        internal string Source, Destination;
        internal long DecisionOrdinal, BlockOrdinal, Deadline;
        internal int BlockerSequence;
        internal long ReleaseOrdinal = -1, ReleaseTime = -1, VacancyOrdinal = -1, VacancyTime = -1;
        internal int VacancySequence;
        internal TransferControl Copy() => (TransferControl)MemberwiseClone();
    }

    public sealed partial class RunExecution
    {
        internal bool TransferContract { get; private set; }
        internal TransferControl TransferGate { get; private set; }
        private int currentFloor;
        internal bool TransferWaiting => TransferContract && TransferGate != null && TransferGate.ReleaseOrdinal < 0;
        internal bool TransferReleased => TransferContract && last?.Kind == RunStepKind.FloorTransferReleased;
        internal string OccupiedFloor => Snapshot?.Floors[currentFloor].FloorInstanceId;
        internal string TransferDestination => TransferContract && currentFloor + 1 < Snapshot.Floors.Count
            ? Snapshot.Floors[currentFloor + 1].FloorInstanceId : null;

        internal void EnableTransfers()
        {
            if (TransferContract || ordinal != 0 || Combat == null || StartInputs == null || Snapshot == null ||
                Snapshot.Floors.Count > 2) throw new InvalidOperationException("living.transfer.invalid");
            TransferContract = true;
            maximumEvents = checked(maximumEvents + 2 * (Snapshot.Floors.Count - 1));
            StartInputs = TransferCheckpointCodec.WrapStart(StartInputs, maximumEvents);
            PrefixDigest = RunCheckpointCodec.Hash(StartInputs);
        }

        internal RunStep BlockTransfer(long deadline, int blocker)
        {
            if (!TransferContract || TransferGate != null || blocker <= 0 || NextDueTime() != deadline ||
                pending?.Kind != RunStepKind.FloorTransfer || last?.Kind != RunStepKind.FloorDecision ||
                last.Value.ContinueChosen != true || TransferDestination == null)
                throw new InvalidOperationException("living.transfer.invalid");
            TransferGate = new TransferControl { Source = OccupiedFloor, Destination = TransferDestination,
                DecisionOrdinal = last.Value.Ordinal, BlockOrdinal = ordinal, Deadline = deadline, BlockerSequence = blocker };
            return CommitTransferControl(RunStepKind.FloorTransferBlocked, deadline);
        }

        // The canonical writer must authorize vacancy before calling this transition.
        // Replay calls the same routine with the pinned, consumed control evidence.
        internal RunStep ReleaseTransfer(long time, int causeSequence, long causeOrdinal, long causeTime)
        {
            var gate = TransferGate;
            if (!TransferWaiting || pending?.Kind != RunStepKind.FloorTransfer || time < logicalTime ||
                causeSequence != gate.BlockerSequence || causeOrdinal < 0 || causeTime < gate.Deadline || causeTime != time)
                throw new InvalidOperationException("living.transfer.invalid_release");
            gate.ReleaseOrdinal = ordinal; gate.ReleaseTime = time;
            gate.VacancySequence = causeSequence; gate.VacancyOrdinal = causeOrdinal; gate.VacancyTime = causeTime;
            return CommitTransferControl(RunStepKind.FloorTransferReleased, time);
        }

        private RunStep CommitTransferControl(RunStepKind kind, long time)
        {
            if (advancing || failure != null || taken || IsTerminal || ordinal >= maximumEvents || time < acceptedDeadline)
                throw new InvalidOperationException("living.transfer.invalid");
            logicalTime = time; acceptedDeadline = time;
            last = new RunStep(kind, Snapshot.Floors[currentFloor].FloorIndex, null)
                .Stamp(Party.RunId, ordinal++, time, CarriedLootValue);
            PrefixDigest = TransferCheckpointCodec.Fold(PrefixDigest, last.Value, this);
            PresentationSteps?.Add(last.Value);
            return last.Value;
        }

        private void CompleteTransfer(RunStep step)
        {
            if (currentFloor + 1 >= Snapshot.Floors.Count || step.FloorIndex != Snapshot.Floors[currentFloor + 1].FloorIndex ||
                TransferWaiting) throw new InvalidOperationException("living.transfer.invalid");
            currentFloor++;
        }
    }
}
