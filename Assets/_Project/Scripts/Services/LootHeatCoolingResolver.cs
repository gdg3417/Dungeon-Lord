using System;

namespace DungeonBuilder.M0
{
    public static class LootHeatCoolingResolver
    {
        public static RunLootHeatCoolingSummary Resolve(RunSimulationConfig config, RunLootExtractionSummary extractionSummary, double currentHeat, int deterministicSeed)
        {
            var summary = new RunLootHeatCoolingSummary
            {
                RuleSourceId = config != null ? config.LootHeatCoolingRuleSourceId : string.Empty,
                DeterministicSeed = deterministicSeed,
                RuleResolved = false,
                DeterministicErrorCode = (int)RunLootHeatCoolingSummaryErrorCode.InvalidCoolingConfig,
                HeatBeforeCooling = currentHeat,
                HeatAfterCooling = currentHeat
            };

            if (config == null || string.IsNullOrWhiteSpace(config.LootHeatCoolingRuleSourceId) ||
                config.LootHeatCoolingPerTradeableWorldValue < 0d || config.MaxLootHeatCoolingPerRun < 0d ||
                double.IsNaN(config.LootHeatCoolingPerTradeableWorldValue) || double.IsInfinity(config.LootHeatCoolingPerTradeableWorldValue) ||
                double.IsNaN(config.MaxLootHeatCoolingPerRun) || double.IsInfinity(config.MaxLootHeatCoolingPerRun))
            {
                return summary;
            }

            if (extractionSummary == null || !extractionSummary.RuleResolved || extractionSummary.DeterministicErrorCode != (int)RunLootExtractionSummaryErrorCode.None)
            {
                summary.DeterministicErrorCode = (int)RunLootHeatCoolingSummaryErrorCode.ExtractionSummaryMissingOrFailed;
                return summary;
            }

            double extractedTradeable = extractionSummary.TotalExtractedTradeableWorldValue;
            summary.ExtractedTradeableWorldValueUsed = extractedTradeable;
            summary.CoolingPerTradeableWorldValueUsed = config.LootHeatCoolingPerTradeableWorldValue;

            if (double.IsNaN(extractedTradeable) || double.IsInfinity(extractedTradeable))
            {
                summary.DeterministicErrorCode = (int)RunLootHeatCoolingSummaryErrorCode.AggregateOverflow;
                return summary;
            }

            if (extractedTradeable <= 0d)
            {
                summary.RuleResolved = true;
                summary.DeterministicErrorCode = (int)RunLootHeatCoolingSummaryErrorCode.None;
                return summary;
            }

            double coolingMagnitude = extractedTradeable * config.LootHeatCoolingPerTradeableWorldValue;
            if (double.IsNaN(coolingMagnitude) || double.IsInfinity(coolingMagnitude))
            {
                summary.DeterministicErrorCode = (int)RunLootHeatCoolingSummaryErrorCode.AggregateOverflow;
                return summary;
            }

            double clampedMagnitude = Math.Min(coolingMagnitude, config.MaxLootHeatCoolingPerRun);
            if (double.IsNaN(clampedMagnitude) || double.IsInfinity(clampedMagnitude))
            {
                summary.DeterministicErrorCode = (int)RunLootHeatCoolingSummaryErrorCode.AggregateOverflow;
                return summary;
            }

            summary.UnclampedHeatDelta = -coolingMagnitude;
            summary.AppliedHeatDelta = -Math.Max(0d, clampedMagnitude);
            summary.HeatAfterCooling = currentHeat + summary.AppliedHeatDelta;
            summary.RuleResolved = true;
            summary.DeterministicErrorCode = (int)RunLootHeatCoolingSummaryErrorCode.None;
            return summary;
        }
    }
}
