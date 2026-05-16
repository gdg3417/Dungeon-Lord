using System;

namespace DungeonBuilder.M0
{
    public readonly struct KpiSnapshot
    {
        public double AverageManaPerTick { get; }
        public int BlockedGateCount { get; }
        public int TotalGateEvaluations { get; }

        public KpiSnapshot(double averageManaPerTick, int blockedGateCount, int totalGateEvaluations)
        {
            AverageManaPerTick = averageManaPerTick;
            BlockedGateCount = blockedGateCount;
            TotalGateEvaluations = totalGateEvaluations;
        }
    }

    public interface IKpiService
    {
        void RecordManaTick(double generatedMana);
        void RecordGateEvaluation(bool allowed);
        KpiSnapshot Snapshot();
    }

    public sealed class KpiService : IKpiService
    {
        private double _totalGeneratedMana;
        private int _manaTicks;
        private int _blockedGateCount;
        private int _totalGateEvaluations;

        public void RecordManaTick(double generatedMana)
        {
            _totalGeneratedMana += Math.Max(0d, generatedMana);
            _manaTicks++;
        }

        public void RecordGateEvaluation(bool allowed)
        {
            _totalGateEvaluations++;
            if (!allowed)
            {
                _blockedGateCount++;
            }
        }

        public KpiSnapshot Snapshot()
        {
            double avg = _manaTicks > 0 ? _totalGeneratedMana / _manaTicks : 0d;
            return new KpiSnapshot(avg, _blockedGateCount, _totalGateEvaluations);
        }
    }
}
