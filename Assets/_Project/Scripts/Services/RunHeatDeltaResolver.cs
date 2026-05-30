using System;

namespace DungeonBuilder.M0
{
    public static class RunHeatDeltaResolver
    {
        public static RunHeatDeltaSummary Resolve(RunSimulationConfig config, RunSurvivalSummary survivalSummary, RunLootExtractionSummary extractionSummary, int deterministicSeed)
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
            // M6-B0 has no explicit elite-death source on the run outcome/survival model yet.
            // Keep elite heat inactive until actual elite death data exists instead of inferring it from forecast/planning signals.
            int eliteDeathCount = 0;
            int survivorCount = Math.Max(0, survivalSummary.SurvivorCount);
            bool fullPartySurvived = survivalSummary.PartySize > 0 && survivorCount == survivalSummary.PartySize;
            double extractedTradeableValue = Math.Max(0d, extractionSummary.TotalExtractedTradeableWorldValue);

            if (!TryMultiply(deathCount, config.RunHeatNormalDeathDelta, out double deathHeatDelta) ||
                !TryMultiply(eliteDeathCount, config.RunHeatEliteDeathDelta, out double eliteDeathHeatDelta) ||
                !TryMultiply(fullPartySurvived ? survivorCount : 0, -config.RunHeatSurvivorCoolingPerSurvivor, out double survivorCoolingDelta) ||
                !TryMultiply(survivorCount == 0 ? 0d : extractedTradeableValue, -config.RunHeatLootCoolingPerExtractedValue, out double lootCoolingDelta))
            {
                summary.DeterministicErrorCode = (int)RunHeatDeltaSummaryErrorCode.AggregateOverflow;
                return summary;
            }

            double multipleDeathBonusDelta = deathCount > 1 ? config.RunHeatMultipleDeathBonusDelta : 0d;
            if (!TryAdd(deathHeatDelta, eliteDeathHeatDelta, out double unclampedDelta) ||
                !TryAdd(unclampedDelta, multipleDeathBonusDelta, out unclampedDelta) ||
                !TryAdd(unclampedDelta, survivorCoolingDelta, out unclampedDelta) ||
                !TryAdd(unclampedDelta, lootCoolingDelta, out unclampedDelta))
            {
                summary.DeterministicErrorCode = (int)RunHeatDeltaSummaryErrorCode.AggregateOverflow;
                return summary;
            }

            summary.DeathHeatDelta = deathHeatDelta;
            summary.EliteDeathHeatDelta = eliteDeathHeatDelta;
            summary.MultipleDeathBonusDelta = multipleDeathBonusDelta;
            summary.SurvivorCoolingDelta = survivorCoolingDelta;
            summary.LootCoolingDelta = lootCoolingDelta;
            summary.FinalHeatDelta = Math.Max(config.RunHeatDeltaMinimum, Math.Min(config.RunHeatDeltaMaximum, unclampedDelta));
            summary.RuleResolved = true;
            summary.DeterministicErrorCode = (int)RunHeatDeltaSummaryErrorCode.None;
            return summary;
        }

        private static bool TryMultiply(double left, double right, out double result)
        {
            result = left * right;
            return IsFinite(result);
        }

        private static bool TryAdd(double left, double right, out double result)
        {
            result = left + right;
            return IsFinite(result);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
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
