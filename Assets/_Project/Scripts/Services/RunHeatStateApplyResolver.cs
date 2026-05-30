using System;

namespace DungeonBuilder.M0
{
    public static class RunHeatStateApplyResolver
    {
        public const string PeaceTierId = "heat_tier.peace";
        public const string NoticeTierId = "heat_tier.notice";
        public const string ConcernTierId = "heat_tier.concern";

        public static RunHeatApplicationSummary Resolve(RunSimulationConfig config, double currentHeat, RunHeatDeltaSummary heatDeltaSummary)
        {
            var result = new RunHeatApplicationSummary
            {
                HeatBefore = IsFinite(currentHeat) ? currentHeat : 0d,
                HeatAfter = IsFinite(currentHeat) ? currentHeat : 0d,
                RuleSourceIdUsed = config != null ? config.RunHeatApplicationRuleSourceId : null
            };

            if (heatDeltaSummary == null)
            {
                result.DeterministicErrorCode = (int)RunHeatApplicationSummaryErrorCode.HeatDeltaSummaryMissing;
                return result;
            }

            if (!heatDeltaSummary.RuleResolved)
            {
                result.DeterministicErrorCode = (int)RunHeatApplicationSummaryErrorCode.HeatDeltaSummaryUnresolved;
                return result;
            }

            if (!IsFinite(heatDeltaSummary.FinalHeatDelta))
            {
                result.DeterministicErrorCode = (int)RunHeatApplicationSummaryErrorCode.InvalidHeatDeltaSummary;
                return result;
            }

            if (!IsValidConfig(config))
            {
                result.DeterministicErrorCode = (int)RunHeatApplicationSummaryErrorCode.InvalidHeatApplicationConfig;
                return result;
            }

            if (!IsFinite(currentHeat))
            {
                result.DeterministicErrorCode = (int)RunHeatApplicationSummaryErrorCode.InvalidCurrentHeat;
                return result;
            }

            string tierBefore = ResolveTierId(config, currentHeat);
            double rawHeatAfter = currentHeat + heatDeltaSummary.FinalHeatDelta;
            if (!IsFinite(rawHeatAfter))
            {
                result.DeterministicErrorCode = (int)RunHeatApplicationSummaryErrorCode.AggregateOverflow;
                return result;
            }

            double heatAfter = Math.Max(config.HeatPeaceMinimum, Math.Min(config.HeatConcernMaximum, rawHeatAfter));
            string tierAfter = ResolveTierId(config, heatAfter);
            result.RuleResolved = true;
            result.DeterministicErrorCode = (int)RunHeatApplicationSummaryErrorCode.None;
            result.AppliedDelta = heatAfter - currentHeat;
            result.HeatAfter = heatAfter;
            result.TierBefore = tierBefore;
            result.TierAfter = tierAfter;
            result.TierChanged = !string.Equals(tierBefore, tierAfter, StringComparison.Ordinal);
            return result;
        }

        private static string ResolveTierId(RunSimulationConfig config, double heat)
        {
            if (heat < config.HeatNoticeMinimum)
            {
                return PeaceTierId;
            }

            if (heat < config.HeatConcernMinimum)
            {
                return NoticeTierId;
            }

            return ConcernTierId;
        }

        private static bool IsValidConfig(RunSimulationConfig config)
        {
            return config != null &&
                   IsFinite(config.HeatPeaceMinimum) &&
                   IsFinite(config.HeatPeaceMaximum) &&
                   IsFinite(config.HeatNoticeMinimum) &&
                   IsFinite(config.HeatNoticeMaximum) &&
                   IsFinite(config.HeatConcernMinimum) &&
                   IsFinite(config.HeatConcernMaximum) &&
                   !string.IsNullOrWhiteSpace(config.RunHeatApplicationRuleSourceId) &&
                   config.HeatPeaceMinimum <= config.HeatPeaceMaximum &&
                   config.HeatPeaceMaximum < config.HeatNoticeMinimum &&
                   config.HeatNoticeMinimum <= config.HeatNoticeMaximum &&
                   config.HeatNoticeMaximum < config.HeatConcernMinimum &&
                   config.HeatConcernMinimum <= config.HeatConcernMaximum;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
