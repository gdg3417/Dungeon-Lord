using System;

namespace DungeonBuilder.M0
{
    public enum RunHeatBoundaryReason { Applied, InvalidConfiguration, InvalidHeat, InvalidDelta, InvalidCapturedTier, Overflow }

    public readonly struct RunHeatBoundaryResult
    {
        public RunHeatBoundaryReason Reason { get; }
        public bool Accepted => Reason == RunHeatBoundaryReason.Applied;
        public double Before { get; }
        public double After { get; }
        public double RequestedDelta { get; }
        public double AppliedDelta => Accepted ? After - Before : 0d;
        // Signed: positive prevented heating, negative prevented cooling. Never retained as credit.
        public double DiscardedDelta => Accepted ? RequestedDelta - AppliedDelta : 0d;
        public string TierBefore { get; }
        public string TierAfter { get; }
        public string RuleSourceId { get; }
        internal RunHeatBoundaryResult(RunHeatBoundaryReason reason, double before, double after,
            double delta, string tierBefore = null, string tierAfter = null, string rule = null)
        { Reason = reason; Before = before; After = after; RequestedDelta = delta;
          TierBefore = tierBefore; TierAfter = tierAfter; RuleSourceId = rule; }
    }

    public readonly struct RunHeatBoundaryStages
    {
        public RunHeatBoundaryResult First { get; }
        public RunHeatBoundaryResult Second { get; }
        public bool Accepted => First.Accepted && Second.Accepted;
        internal RunHeatBoundaryStages(RunHeatBoundaryResult first, RunHeatBoundaryResult second)
        { First = first; Second = second; }
    }

    // Pure application of resolved deltas, not a new Heat composition or settlement authority.
    public static class RunHeatBoundaryCalculation
    {
        public static RunHeatBoundaryResult ApplyStage(RunSimulationConfig config, double heat,
            double resolvedEligibleDelta, double? capturedTierHeat = null)
        {
            var tier = CurrentHeatTierResolver.Resolve(config, heat);
            if (tier.DeterministicErrorCode == (int)CurrentHeatTierSummaryErrorCode.InvalidHeatTierConfig ||
                config == null || config.HeatPeaceMinimum != 0d)
                return Reject(RunHeatBoundaryReason.InvalidConfiguration, heat, resolvedEligibleDelta);
            if (!tier.RuleResolved) return Reject(RunHeatBoundaryReason.InvalidHeat, heat, resolvedEligibleDelta);
            if (!Finite(resolvedEligibleDelta)) return Reject(RunHeatBoundaryReason.InvalidDelta, heat, resolvedEligibleDelta);
            double lower = config.HeatPeaceMinimum, upper = config.HeatConcernMaximum;
            if (capturedTierHeat.HasValue)
            {
                var captured = CurrentHeatTierResolver.Resolve(config, capturedTierHeat.Value);
                if (!captured.RuleResolved) return Reject(RunHeatBoundaryReason.InvalidCapturedTier, heat, resolvedEligibleDelta);
                lower = captured.TierMinimum;
                upper = captured.TierId == CurrentHeatTierResolver.PeaceTierId ? PreviousRepresentable(config.HeatNoticeMinimum) :
                    captured.TierId == CurrentHeatTierResolver.NoticeTierId ? PreviousRepresentable(config.HeatConcernMinimum) : config.HeatConcernMaximum;
                // A future mixed-period authority must order contributions. Never silently drag a
                // foreground state outside the captured tier back inside it.
                if (heat < lower || heat > upper) return Reject(RunHeatBoundaryReason.InvalidCapturedTier, heat, resolvedEligibleDelta);
            }
            double raw = heat + resolvedEligibleDelta;
            if (!Finite(raw)) return Reject(RunHeatBoundaryReason.Overflow, heat, resolvedEligibleDelta);
            double after = Math.Max(lower, Math.Min(upper, raw));
            return new RunHeatBoundaryResult(RunHeatBoundaryReason.Applied, heat, after, resolvedEligibleDelta,
                tier.TierId, CurrentHeatTierResolver.Resolve(config, after).TierId, config.RunHeatApplicationRuleSourceId);
        }

        public static RunHeatBoundaryStages ApplyStages(RunSimulationConfig config, double heat,
            double firstResolvedDelta, double secondResolvedDelta, double? capturedTierHeat = null)
        {
            var first = ApplyStage(config, heat, firstResolvedDelta, capturedTierHeat);
            var second = first.Accepted ? ApplyStage(config, first.After, secondResolvedDelta, capturedTierHeat) :
                Reject(first.Reason, heat, secondResolvedDelta);
            return new RunHeatBoundaryStages(first, second);
        }
        // IEEE-754 binary64 predecessor of a validated strictly positive next-tier threshold.
        // Threshold membership remains exclusive; no gameplay epsilon or quantization is used.
        internal static double PreviousRepresentable(double positive) =>
            BitConverter.Int64BitsToDouble(checked(BitConverter.DoubleToInt64Bits(positive) - 1));
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static RunHeatBoundaryResult Reject(RunHeatBoundaryReason reason, double heat, double delta) =>
            new RunHeatBoundaryResult(reason, heat, heat, delta);
    }
}
