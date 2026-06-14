using System;

namespace DungeonBuilder.M0
{
    public static class AdventurerArrivalPressureResolver
    {
        public const string BandNotYetId = "adventurer_arrival_pressure.band.not_yet";
        public const string BandLowId = "adventurer_arrival_pressure.band.low";
        public const string BandCautiousId = "adventurer_arrival_pressure.band.cautious_interest";
        public const string BandBuildingId = "adventurer_arrival_pressure.band.building_slowly";
        public const string BandLikelySoonId = "adventurer_arrival_pressure.band.likely_soon";

        public const string ReasonHighLootLowHeatKey = "ui.adventurer_pressure.reason.high_loot_low_heat";
        public const string ReasonModestLootLowAttractionKey = "ui.adventurer_pressure.reason.modest_loot_low_attraction";
        public const string ReasonDeathsHeatKey = "ui.adventurer_pressure.reason.deaths_heat";
        public const string ReasonIncompletePathWeakLootKey = "ui.adventurer_pressure.reason.incomplete_path_weak_loot";
        public const string ReasonNotYetKey = "ui.adventurer_pressure.reason.not_yet";

        public static AdventurerArrivalPressureSummary Resolve(RunSimulationConfig config, MvpPlacementEffectsSummary effects, CurrentHeatTierSummary heatTier, RunOutcomeRecord latestRun)
        {
            var summary = new AdventurerArrivalPressureSummary
            {
                RuleSourceId = config?.AdventurerArrivalPressureRuleSourceId ?? string.Empty,
                PressureBandId = BandNotYetId,
                PrimaryReasonKey = ReasonNotYetKey,
                WouldMutateState = false
            };

            if (!HasValidConfig(config))
            {
                summary.DeterministicErrorCode = (int)AdventurerArrivalPressureSummaryErrorCode.MissingOrInvalidConfig;
                return summary;
            }

            int heatRank = ResolveHeatTierRank(heatTier);
            if (heatRank < 0)
            {
                summary.DeterministicErrorCode = (int)AdventurerArrivalPressureSummaryErrorCode.InvalidHeatTier;
                return summary;
            }

            int loot = effects != null && effects.RuleResolved ? effects.LootBonus : 0;
            int attraction = effects != null && effects.RuleResolved ? effects.Attraction : 0;
            int danger = effects != null && effects.RuleResolved ? effects.Danger : 0;
            int heatPressure = effects != null && effects.RuleResolved ? effects.HeatPressure : 0;
            bool pathComplete = effects != null && effects.RuleResolved && effects.PathCapacity > 0;
            int recentDeaths = latestRun?.SurvivalSummary?.DeathCount ?? 0;
            int recoveredLoot = latestRun?.LootExtractionSummary?.TotalExtractedWorldValue ?? latestRun?.LootSummary?.TotalGeneratedWorldValue ?? 0;

            double score = (loot * config.ArrivalPressureScorePerLoot) +
                           (attraction * config.ArrivalPressureScorePerAttraction) +
                           (danger * config.ArrivalPressureScorePerDanger) +
                           (heatPressure * config.ArrivalPressureScorePerHeatPressure) +
                           (recoveredLoot * config.ArrivalPressureScorePerRecentRecoveredLoot) +
                           (pathComplete ? config.ArrivalPressurePathCompleteBonus : -config.ArrivalPressureIncompletePathPenalty) -
                           (recentDeaths * config.ArrivalPressureRecentDeathPenalty) -
                           ResolveHeatPenalty(config, heatRank) +
                           ResolveOutcomeAdjustment(config, latestRun);

            if (!IsFinite(score))
            {
                summary.DeterministicErrorCode = (int)AdventurerArrivalPressureSummaryErrorCode.AggregateOverflow;
                return summary;
            }

            summary.Score = score;
            summary.HeatTierRank = heatRank;
            summary.PathComplete = pathComplete;
            summary.LootSignal = loot;
            summary.AttractionSignal = attraction;
            summary.DangerSignal = danger;
            summary.HeatPressureSignal = heatPressure;
            summary.RecentDeathCount = recentDeaths;
            summary.RecentRecoveredLoot = recoveredLoot;
            summary.LatestRunOutcomeId = latestRun == null ? string.Empty : latestRun.Success ? "run.outcome.success" : "run.outcome.failure";
            summary.PressureBandId = ResolveBand(config, score, pathComplete);
            summary.PrimaryReasonKey = ResolveReason(summary.PressureBandId, pathComplete, loot, attraction, recentDeaths, heatRank, heatPressure);
            summary.RuleResolved = true;
            summary.DeterministicErrorCode = (int)AdventurerArrivalPressureSummaryErrorCode.None;
            return summary;
        }

        private static string ResolveBand(RunSimulationConfig config, double score, bool pathComplete)
        {
            if (!pathComplete) return score >= config.ArrivalPressureLowThreshold ? BandLowId : BandNotYetId;
            if (score >= config.ArrivalPressureLikelySoonThreshold) return BandLikelySoonId;
            if (score >= config.ArrivalPressureBuildingThreshold) return BandBuildingId;
            if (score >= config.ArrivalPressureCautiousThreshold) return BandCautiousId;
            if (score >= config.ArrivalPressureLowThreshold) return BandLowId;
            return BandNotYetId;
        }

        private static string ResolveReason(string bandId, bool pathComplete, int loot, int attraction, int recentDeaths, int heatRank, int heatPressure)
        {
            if (!pathComplete || loot <= 0) return ReasonIncompletePathWeakLootKey;
            if (recentDeaths > 0 || heatRank > 0 || heatPressure > 0) return ReasonDeathsHeatKey;
            if (string.Equals(bandId, BandLikelySoonId, StringComparison.Ordinal)) return ReasonHighLootLowHeatKey;
            if (loot > 0 && attraction <= 1) return ReasonModestLootLowAttractionKey;
            return ReasonNotYetKey;
        }

        private static double ResolveHeatPenalty(RunSimulationConfig config, int heatRank)
        {
            if (heatRank >= 2) return config.ArrivalPressureHeatConcernPenalty;
            if (heatRank == 1) return config.ArrivalPressureHeatNoticePenalty;
            return 0d;
        }

        private static double ResolveOutcomeAdjustment(RunSimulationConfig config, RunOutcomeRecord latestRun)
        {
            if (latestRun == null) return 0d;
            return latestRun.Success ? config.ArrivalPressureLatestSuccessBonus : -config.ArrivalPressureLatestFailurePenalty;
        }

        private static int ResolveHeatTierRank(CurrentHeatTierSummary heatTier)
        {
            string tierId = heatTier != null && heatTier.RuleResolved ? heatTier.TierId : string.Empty;
            if (string.Equals(tierId, "heat_tier.peace", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(tierId)) return 0;
            if (string.Equals(tierId, "heat_tier.notice", StringComparison.Ordinal)) return 1;
            if (string.Equals(tierId, "heat_tier.concern", StringComparison.Ordinal)) return 2;
            return -1;
        }

        private static bool HasValidConfig(RunSimulationConfig config)
        {
            return config != null && !string.IsNullOrWhiteSpace(config.AdventurerArrivalPressureRuleSourceId) &&
                   IsFinite(config.ArrivalPressureScorePerLoot) && IsFinite(config.ArrivalPressureScorePerAttraction) && IsFinite(config.ArrivalPressureScorePerDanger) && IsFinite(config.ArrivalPressureScorePerHeatPressure) && IsFinite(config.ArrivalPressureScorePerRecentRecoveredLoot) &&
                   IsFinite(config.ArrivalPressureLatestSuccessBonus) && IsFinite(config.ArrivalPressureLatestFailurePenalty) && IsFinite(config.ArrivalPressurePathCompleteBonus) && IsFinite(config.ArrivalPressureIncompletePathPenalty) &&
                   IsFinite(config.ArrivalPressureHeatNoticePenalty) && IsFinite(config.ArrivalPressureHeatConcernPenalty) && IsFinite(config.ArrivalPressureRecentDeathPenalty) &&
                   IsFinite(config.ArrivalPressureNoneThreshold) && IsFinite(config.ArrivalPressureLowThreshold) && IsFinite(config.ArrivalPressureCautiousThreshold) && IsFinite(config.ArrivalPressureBuildingThreshold) && IsFinite(config.ArrivalPressureLikelySoonThreshold) &&
                   config.ArrivalPressureNoneThreshold <= config.ArrivalPressureLowThreshold && config.ArrivalPressureLowThreshold <= config.ArrivalPressureCautiousThreshold && config.ArrivalPressureCautiousThreshold <= config.ArrivalPressureBuildingThreshold && config.ArrivalPressureBuildingThreshold <= config.ArrivalPressureLikelySoonThreshold;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
