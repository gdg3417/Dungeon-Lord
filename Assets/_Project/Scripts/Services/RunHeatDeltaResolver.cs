using System;

namespace DungeonBuilder.M0
{
    public static class RunHeatDeltaResolver
    {
        public static RunHeatDeltaSummary Resolve(RunSimulationConfig config, RunSurvivalSummary survivalSummary, RunLootExtractionSummary extractionSummary, RunAdventurerDemandBudgetSummary demandBudgetSummary, int deterministicSeed)
        {
            var summary = new RunHeatDeltaSummary
            {
                RuleSourceIdUsed = config != null ? config.RunHeatDeltaRuleSourceId : null,
                DeterministicSeed = deterministicSeed,
                RuleResolved = false,
                DeterministicErrorCode = (int)RunHeatDeltaSummaryErrorCode.InvalidHeatDeltaConfig
            };

            if (!IsValidConfig(config)) return summary;
            if (survivalSummary == null || !survivalSummary.RuleResolved)
            {
                summary.DeterministicErrorCode = (int)RunHeatDeltaSummaryErrorCode.SurvivalSummaryMissingOrFailed;
                return summary;
            }

            if (extractionSummary == null || !extractionSummary.RuleResolved)
            {
                summary.DeterministicErrorCode = (int)RunHeatDeltaSummaryErrorCode.ExtractionSummaryMissingOrFailed;
                return summary;
            }

            int deathCount = Math.Max(0, survivalSummary.DeathCount);
            int eliteDeathCount = demandBudgetSummary != null && demandBudgetSummary.RuleResolved && demandBudgetSummary.DemandBudgetBandId == "adventurer_demand.high" ? deathCount : 0;

            summary.DeathHeatDelta = deathCount * config.RunHeatNormalDeathDelta;
            summary.EliteDeathHeatDelta = eliteDeathCount * (config.RunHeatEliteDeathDelta - config.RunHeatNormalDeathDelta);
            summary.MultipleDeathBonusDelta = deathCount > 1 ? config.RunHeatMultipleDeathBonusDelta : 0d;
            summary.SurvivorCoolingDelta = -Math.Max(0, survivalSummary.SurvivorCount) * config.RunHeatSurvivorCoolingPerSurvivor;
            summary.LootCoolingDelta = survivalSummary.SurvivorCount == 0 ? 0d : -Math.Max(0d, extractionSummary.TotalExtractedTradeableWorldValue) * config.RunHeatLootCoolingPerExtractedValue;

            double unclampedDelta = summary.DeathHeatDelta + summary.EliteDeathHeatDelta + summary.MultipleDeathBonusDelta + summary.SurvivorCoolingDelta + summary.LootCoolingDelta;
            if (double.IsNaN(unclampedDelta) || double.IsInfinity(unclampedDelta))
            {
                summary.DeterministicErrorCode = (int)RunHeatDeltaSummaryErrorCode.AggregateOverflow;
                return summary;
            }

            summary.FinalHeatDelta = Math.Max(config.RunHeatDeltaMinimum, Math.Min(config.RunHeatDeltaMaximum, unclampedDelta));
            summary.RuleResolved = true;
            summary.DeterministicErrorCode = (int)RunHeatDeltaSummaryErrorCode.None;
            return summary;
        }

        private static bool IsValidConfig(RunSimulationConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.RunHeatDeltaRuleSourceId)) return false;
            if (config.RunHeatNormalDeathDelta < 0d || config.RunHeatEliteDeathDelta < config.RunHeatNormalDeathDelta || config.RunHeatMultipleDeathBonusDelta < 0d || config.RunHeatSurvivorCoolingPerSurvivor < 0d || config.RunHeatLootCoolingPerExtractedValue < 0d) return false;
            if (double.IsNaN(config.RunHeatNormalDeathDelta) || double.IsInfinity(config.RunHeatNormalDeathDelta) ||
                double.IsNaN(config.RunHeatEliteDeathDelta) || double.IsInfinity(config.RunHeatEliteDeathDelta) ||
                double.IsNaN(config.RunHeatMultipleDeathBonusDelta) || double.IsInfinity(config.RunHeatMultipleDeathBonusDelta) ||
                double.IsNaN(config.RunHeatSurvivorCoolingPerSurvivor) || double.IsInfinity(config.RunHeatSurvivorCoolingPerSurvivor) ||
                double.IsNaN(config.RunHeatLootCoolingPerExtractedValue) || double.IsInfinity(config.RunHeatLootCoolingPerExtractedValue) ||
                double.IsNaN(config.RunHeatDeltaMinimum) || double.IsInfinity(config.RunHeatDeltaMinimum) ||
                double.IsNaN(config.RunHeatDeltaMaximum) || double.IsInfinity(config.RunHeatDeltaMaximum)) return false;

            return config.RunHeatDeltaMinimum <= config.RunHeatDeltaMaximum;
        }
    }
}
