using System;

namespace DungeonBuilder.M0
{
    public static class CurrentHeatTierResolver
    {
        public const string PeaceTierId = "heat_tier.peace";
        public const string NoticeTierId = "heat_tier.notice";
        public const string ConcernTierId = "heat_tier.concern";

        public static CurrentHeatTierSummary Resolve(RunSimulationConfig config, double currentHeat)
        {
            var result = new CurrentHeatTierSummary
            {
                CurrentHeat = currentHeat,
                RuleSourceIdUsed = config != null ? config.RunHeatApplicationRuleSourceId : null
            };

            if (!IsValidConfig(config))
            {
                result.DeterministicErrorCode = (int)CurrentHeatTierSummaryErrorCode.InvalidHeatTierConfig;
                return result;
            }

            if (!IsFinite(currentHeat))
            {
                result.DeterministicErrorCode = (int)CurrentHeatTierSummaryErrorCode.InvalidCurrentHeat;
                return result;
            }

            if (currentHeat < config.HeatPeaceMinimum || currentHeat > config.HeatConcernMaximum)
            {
                result.DeterministicErrorCode = (int)CurrentHeatTierSummaryErrorCode.CurrentHeatOutOfRange;
                return result;
            }

            if (currentHeat < config.HeatNoticeMinimum)
            {
                ResolveTier(result, PeaceTierId, config.HeatPeaceMinimum, config.HeatPeaceMaximum);
            }
            else if (currentHeat < config.HeatConcernMinimum)
            {
                ResolveTier(result, NoticeTierId, config.HeatNoticeMinimum, config.HeatNoticeMaximum);
            }
            else
            {
                ResolveTier(result, ConcernTierId, config.HeatConcernMinimum, config.HeatConcernMaximum);
            }

            return result;
        }

        private static void ResolveTier(CurrentHeatTierSummary result, string tierId, double minimum, double maximum)
        {
            result.RuleResolved = true;
            result.DeterministicErrorCode = (int)CurrentHeatTierSummaryErrorCode.None;
            result.TierId = tierId;
            result.TierMinimum = minimum;
            result.TierMaximum = maximum;
            result.IsAtTierMinimum = result.CurrentHeat == minimum;
            result.IsAtTierMaximum = result.CurrentHeat == maximum;
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
