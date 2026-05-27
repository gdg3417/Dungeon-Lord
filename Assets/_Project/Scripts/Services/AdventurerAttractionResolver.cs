using System;

namespace DungeonBuilder.M0
{
    public static class AdventurerAttractionResolver
    {
        public static RunAdventurerAttractionSummary Resolve(RunSimulationConfig config, RunLootExtractionSummary extractionSummary, int deterministicSeed)
        {
            var summary = new RunAdventurerAttractionSummary
            {
                RuleSourceId = config != null ? config.AdventurerAttractionRuleSourceId : null,
                DeterministicSeed = deterministicSeed
            };

            if (config == null ||
                string.IsNullOrWhiteSpace(config.AdventurerAttractionRuleSourceId) ||
                config.AdventurerAttractionPerExtractedWorldValue < 0d ||
                double.IsNaN(config.AdventurerAttractionPerExtractedWorldValue) ||
                double.IsInfinity(config.AdventurerAttractionPerExtractedWorldValue))
            {
                summary.RuleResolved = false;
                summary.DeterministicErrorCode = (int)RunAdventurerAttractionSummaryErrorCode.InvalidAttractionConfig;
                return summary;
            }

            if (extractionSummary == null || !extractionSummary.RuleResolved || extractionSummary.DeterministicErrorCode != (int)RunLootExtractionSummaryErrorCode.None)
            {
                summary.RuleResolved = false;
                summary.DeterministicErrorCode = (int)RunAdventurerAttractionSummaryErrorCode.ExtractionSummaryMissingOrFailed;
                return summary;
            }

            summary.ExtractedWorldValueUsed = extractionSummary.TotalExtractedWorldValue;
            summary.AttractionPerExtractedWorldValueUsed = config.AdventurerAttractionPerExtractedWorldValue;

            double attraction = summary.ExtractedWorldValueUsed * summary.AttractionPerExtractedWorldValueUsed;
            if (double.IsNaN(attraction) || double.IsInfinity(attraction))
            {
                summary.RuleResolved = false;
                summary.DeterministicErrorCode = (int)RunAdventurerAttractionSummaryErrorCode.AggregateOverflow;
                return summary;
            }

            summary.RuleResolved = true;
            summary.DeterministicErrorCode = (int)RunAdventurerAttractionSummaryErrorCode.None;
            summary.AttractionSignalValue = attraction;
            return summary;
        }
    }
}
