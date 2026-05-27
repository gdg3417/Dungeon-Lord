using System;

namespace DungeonBuilder.M0
{
    public static class AdventurerInterestForecastResolver
    {
        private const string BandNone = "adventurer_interest.none";
        private const string BandLow = "adventurer_interest.low";
        private const string BandMedium = "adventurer_interest.medium";
        private const string BandHigh = "adventurer_interest.high";

        public static RunAdventurerInterestForecastSummary Resolve(RunSimulationConfig config, RunAdventurerAttractionSummary attractionSummary, int deterministicSeed)
        {
            var summary = new RunAdventurerInterestForecastSummary
            {
                RuleSourceId = config != null ? config.AdventurerInterestForecastRuleSourceId : null,
                DeterministicSeed = deterministicSeed,
                ForecastBandId = BandNone
            };

            if (attractionSummary == null ||
                !attractionSummary.RuleResolved ||
                attractionSummary.DeterministicErrorCode != (int)RunAdventurerAttractionSummaryErrorCode.None)
            {
                summary.DeterministicErrorCode = (int)RunAdventurerInterestForecastSummaryErrorCode.AttractionSummaryMissingOrFailed;
                return summary;
            }

            if (!IsValidConfig(config))
            {
                summary.DeterministicErrorCode = (int)RunAdventurerInterestForecastSummaryErrorCode.InvalidForecastConfig;
                return summary;
            }

            summary.AttractionSignalUsed = attractionSummary.AttractionSignalValue;
            double forecastScore = summary.AttractionSignalUsed * config.AdventurerInterestScorePerAttractionSignal;
            if (double.IsNaN(forecastScore) || double.IsInfinity(forecastScore))
            {
                summary.DeterministicErrorCode = (int)RunAdventurerInterestForecastSummaryErrorCode.AggregateOverflow;
                return summary;
            }

            summary.ForecastInterestScore = forecastScore;
            summary.ForecastBandId = ResolveBandId(forecastScore, config);
            summary.RuleResolved = true;
            summary.DeterministicErrorCode = (int)RunAdventurerInterestForecastSummaryErrorCode.None;
            return summary;
        }

        private static string ResolveBandId(double score, RunSimulationConfig config)
        {
            if (score >= config.AdventurerInterestHighThreshold)
            {
                return BandHigh;
            }

            if (score >= config.AdventurerInterestMediumThreshold)
            {
                return BandMedium;
            }

            if (score >= config.AdventurerInterestLowThreshold)
            {
                return BandLow;
            }

            return BandNone;
        }

        private static bool IsValidConfig(RunSimulationConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.AdventurerInterestForecastRuleSourceId))
            {
                return false;
            }

            if (config.AdventurerInterestLowThreshold < 0d ||
                config.AdventurerInterestMediumThreshold < 0d ||
                config.AdventurerInterestHighThreshold < 0d ||
                config.AdventurerInterestScorePerAttractionSignal < 0d)
            {
                return false;
            }

            if (double.IsNaN(config.AdventurerInterestLowThreshold) ||
                double.IsInfinity(config.AdventurerInterestLowThreshold) ||
                double.IsNaN(config.AdventurerInterestMediumThreshold) ||
                double.IsInfinity(config.AdventurerInterestMediumThreshold) ||
                double.IsNaN(config.AdventurerInterestHighThreshold) ||
                double.IsInfinity(config.AdventurerInterestHighThreshold) ||
                double.IsNaN(config.AdventurerInterestScorePerAttractionSignal) ||
                double.IsInfinity(config.AdventurerInterestScorePerAttractionSignal))
            {
                return false;
            }

            return config.AdventurerInterestLowThreshold <= config.AdventurerInterestMediumThreshold &&
                   config.AdventurerInterestMediumThreshold <= config.AdventurerInterestHighThreshold;
        }
    }
}
