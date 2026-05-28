using System;

namespace DungeonBuilder.M0
{
    public static class AdventurerDemandBudgetResolver
    {
        private const string BandNone = "adventurer_demand.none";
        private const string BandLow = "adventurer_demand.low";
        private const string BandMedium = "adventurer_demand.medium";
        private const string BandHigh = "adventurer_demand.high";

        public static RunAdventurerDemandBudgetSummary Resolve(RunSimulationConfig config, RunAdventurerInterestForecastSummary forecastSummary, int deterministicSeed)
        {
            var summary = new RunAdventurerDemandBudgetSummary
            {
                RuleSourceId = config != null ? config.AdventurerDemandBudgetRuleSourceId : null,
                DeterministicSeed = deterministicSeed,
                DemandBudgetBandId = BandNone,
                ForecastBandIdUsed = forecastSummary != null ? forecastSummary.ForecastBandId : null
            };

            if (forecastSummary == null ||
                !forecastSummary.RuleResolved ||
                forecastSummary.DeterministicErrorCode != (int)RunAdventurerInterestForecastSummaryErrorCode.None)
            {
                summary.DeterministicErrorCode = (int)RunAdventurerDemandBudgetSummaryErrorCode.InterestForecastMissingOrFailed;
                return summary;
            }

            if (!IsValidConfig(config))
            {
                summary.DeterministicErrorCode = (int)RunAdventurerDemandBudgetSummaryErrorCode.InvalidDemandBudgetConfig;
                return summary;
            }

            summary.ForecastBandIdUsed = forecastSummary.ForecastBandId;
            summary.ForecastInterestScoreUsed = forecastSummary.ForecastInterestScore;
            double demandBudgetScore = summary.ForecastInterestScoreUsed * config.AdventurerDemandBudgetScorePerForecastScore;
            if (double.IsNaN(demandBudgetScore) || double.IsInfinity(demandBudgetScore))
            {
                summary.DeterministicErrorCode = (int)RunAdventurerDemandBudgetSummaryErrorCode.AggregateOverflow;
                return summary;
            }

            summary.DemandBudgetScore = demandBudgetScore;
            summary.DemandBudgetBandId = ResolveBandId(demandBudgetScore, config);
            summary.RuleResolved = true;
            summary.DeterministicErrorCode = (int)RunAdventurerDemandBudgetSummaryErrorCode.None;
            return summary;
        }

        private static string ResolveBandId(double score, RunSimulationConfig config)
        {
            if (score >= config.AdventurerDemandBudgetHighThreshold) return BandHigh;
            if (score >= config.AdventurerDemandBudgetMediumThreshold) return BandMedium;
            if (score >= config.AdventurerDemandBudgetLowThreshold) return BandLow;
            return BandNone;
        }

        private static bool IsValidConfig(RunSimulationConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.AdventurerDemandBudgetRuleSourceId)) return false;
            if (config.AdventurerDemandBudgetLowThreshold < 0d ||
                config.AdventurerDemandBudgetMediumThreshold < 0d ||
                config.AdventurerDemandBudgetHighThreshold < 0d ||
                config.AdventurerDemandBudgetScorePerForecastScore < 0d) return false;

            if (double.IsNaN(config.AdventurerDemandBudgetLowThreshold) ||
                double.IsInfinity(config.AdventurerDemandBudgetLowThreshold) ||
                double.IsNaN(config.AdventurerDemandBudgetMediumThreshold) ||
                double.IsInfinity(config.AdventurerDemandBudgetMediumThreshold) ||
                double.IsNaN(config.AdventurerDemandBudgetHighThreshold) ||
                double.IsInfinity(config.AdventurerDemandBudgetHighThreshold) ||
                double.IsNaN(config.AdventurerDemandBudgetScorePerForecastScore) ||
                double.IsInfinity(config.AdventurerDemandBudgetScorePerForecastScore)) return false;

            return config.AdventurerDemandBudgetLowThreshold <= config.AdventurerDemandBudgetMediumThreshold &&
                   config.AdventurerDemandBudgetMediumThreshold <= config.AdventurerDemandBudgetHighThreshold;
        }
    }
}
