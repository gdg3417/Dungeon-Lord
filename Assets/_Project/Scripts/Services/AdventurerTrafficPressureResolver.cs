using System;

namespace DungeonBuilder.M0
{
    public static class AdventurerTrafficPressureResolver
    {
        public const string BandNoneId = "none";
        public const string BandLowId = "low";
        public const string BandBuildingId = "building";
        public const string BandSteadyId = "steady";
        public const string BandHeavyId = "heavy";
        public const string BandDangerousChurnId = "dangerous_churn";

        public const string PartyBandNoneId = "estimated_party_count.none";
        public const string PartyBandLowId = "estimated_party_count.low";
        public const string PartyBandMediumId = "estimated_party_count.medium";
        public const string PartyBandHighId = "estimated_party_count.high";

        public const string ReasonIncompletePathWeakLootKey = "ui.adventurer_traffic.reason.incomplete_path_weak_loot";
        public const string ReasonHighLootLowHeatKey = "ui.adventurer_traffic.reason.high_loot_low_heat";
        public const string ReasonBalancedKey = "ui.adventurer_traffic.reason.balanced";
        public const string ReasonDeathsCautionKey = "ui.adventurer_traffic.reason.deaths_caution";
        public const string ReasonStrongLootAttractionKey = "ui.adventurer_traffic.reason.strong_loot_attraction";
        public const string ReasonFallbackKey = "ui.adventurer_traffic.reason.fallback";

        public static AdventurerTrafficPressureSummary Resolve(RunSimulationConfig config, AdventurerArrivalPressureSummary arrival, AdventurerRunIntentSummary intent)
        {
            var summary = new AdventurerTrafficPressureSummary
            {
                RuleSourceId = config?.AdventurerTrafficPressureRuleSourceId ?? string.Empty,
                TrafficBandId = BandNoneId,
                EstimatedConcurrentPartyBandId = PartyBandNoneId,
                PrimaryReasonKey = ReasonFallbackKey,
                PressureBandIdUsed = arrival?.PressureBandId ?? string.Empty,
                IntentIdUsed = intent?.IntentId ?? string.Empty,
                WouldMutateState = false
            };

            if (!HasValidConfig(config))
            {
                summary.DeterministicErrorCode = (int)AdventurerTrafficPressureSummaryErrorCode.MissingOrInvalidConfig;
                return summary;
            }
            if (arrival == null || !arrival.RuleResolved)
            {
                summary.DeterministicErrorCode = (int)AdventurerTrafficPressureSummaryErrorCode.MissingArrivalPressure;
                return summary;
            }
            if (intent == null || !intent.RuleResolved)
            {
                summary.DeterministicErrorCode = (int)AdventurerTrafficPressureSummaryErrorCode.MissingRunIntent;
                return summary;
            }

            summary.PathComplete = arrival.PathComplete;
            summary.LootSignal = arrival.LootSignal;
            summary.AttractionSignal = arrival.AttractionSignal;
            summary.DangerSignal = arrival.DangerSignal;
            summary.HeatPressureSignal = arrival.HeatPressureSignal;
            summary.RecentDeathCount = arrival.RecentDeathCount;
            summary.RecentRecoveredLoot = arrival.RecentRecoveredLoot;

            double score = (arrival.Score * config.TrafficScoreWeightArrivalPressure) +
                           (summary.LootSignal * config.TrafficScoreWeightLootSignal) +
                           (summary.AttractionSignal * config.TrafficScoreWeightAttractionSignal) +
                           (summary.DangerSignal * config.TrafficScoreWeightDangerSignal) +
                           (summary.HeatPressureSignal * config.TrafficScoreWeightHeatPressureSignal) +
                           (summary.RecentRecoveredLoot * config.TrafficScoreWeightRecentRecoveredLoot) +
                           (summary.PathComplete ? config.TrafficPathCompleteBonus : -config.TrafficIncompletePathPenalty) -
                           (summary.RecentDeathCount * config.TrafficRecentDeathCautionModifier) -
                           (summary.HeatPressureSignal * config.TrafficHeatCautionModifier);

            if (!IsFinite(score))
            {
                summary.DeterministicErrorCode = (int)AdventurerTrafficPressureSummaryErrorCode.AggregateOverflow;
                return summary;
            }

            summary.TrafficScore = score;
            summary.TrafficBandId = ResolveBand(config, summary);
            summary.PrimaryReasonKey = ResolveReason(summary);
            summary.EstimatedConcurrentPartyCount = Clamp(EstimatePartyCount(config, score), config.TrafficMinimumEstimatedConcurrentParties, config.TrafficMaximumEstimatedConcurrentParties);
            summary.EstimatedConcurrentPartyBandId = ResolvePartyBand(config, summary.EstimatedConcurrentPartyCount);
            summary.RuleResolved = true;
            summary.DeterministicErrorCode = (int)AdventurerTrafficPressureSummaryErrorCode.None;
            return summary;
        }

        private static string ResolveBand(RunSimulationConfig config, AdventurerTrafficPressureSummary summary)
        {
            if (summary.PathComplete && summary.RecentDeathCount >= config.TrafficDangerousChurnRecentDeathThreshold && summary.TrafficScore >= config.TrafficDangerousChurnMinimumInterestScore) return BandDangerousChurnId;
            if (!summary.PathComplete) return summary.TrafficScore >= config.TrafficLowThreshold ? BandLowId : BandNoneId;
            if (summary.TrafficScore >= config.TrafficHeavyThreshold) return BandHeavyId;
            if (summary.TrafficScore >= config.TrafficSteadyThreshold) return BandSteadyId;
            if (summary.TrafficScore >= config.TrafficBuildingThreshold) return BandBuildingId;
            if (summary.TrafficScore >= config.TrafficLowThreshold) return BandLowId;
            return BandNoneId;
        }

        private static string ResolveReason(AdventurerTrafficPressureSummary summary)
        {
            if (!summary.PathComplete || summary.LootSignal <= 0) return ReasonIncompletePathWeakLootKey;
            if (string.Equals(summary.TrafficBandId, BandDangerousChurnId, StringComparison.Ordinal)) return ReasonDeathsCautionKey;
            if (string.Equals(summary.TrafficBandId, BandHeavyId, StringComparison.Ordinal)) return ReasonStrongLootAttractionKey;
            if (string.Equals(summary.TrafficBandId, BandBuildingId, StringComparison.Ordinal)) return ReasonHighLootLowHeatKey;
            if (string.Equals(summary.TrafficBandId, BandSteadyId, StringComparison.Ordinal)) return ReasonBalancedKey;
            return ReasonFallbackKey;
        }

        private static int EstimatePartyCount(RunSimulationConfig config, double score) => (int)Math.Floor((score / config.TrafficEstimatedPartyCountScoreDivisor) * config.TrafficEstimatedPartyCountMultiplier);
        private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
        private static string ResolvePartyBand(RunSimulationConfig config, int count) => count <= 0 ? PartyBandNoneId : count >= config.TrafficEstimatedPartyCountMediumThreshold ? PartyBandHighId : count >= config.TrafficEstimatedPartyCountLowThreshold ? PartyBandMediumId : PartyBandLowId;

        private static bool HasValidConfig(RunSimulationConfig config) => config != null && !string.IsNullOrWhiteSpace(config.AdventurerTrafficPressureRuleSourceId) &&
            IsFinite(config.TrafficScoreWeightArrivalPressure) && IsFinite(config.TrafficScoreWeightLootSignal) && IsFinite(config.TrafficScoreWeightAttractionSignal) && IsFinite(config.TrafficScoreWeightDangerSignal) && IsFinite(config.TrafficScoreWeightHeatPressureSignal) && IsFinite(config.TrafficScoreWeightRecentRecoveredLoot) &&
            IsFinite(config.TrafficPathCompleteBonus) && IsFinite(config.TrafficIncompletePathPenalty) && IsFinite(config.TrafficRecentDeathCautionModifier) && IsFinite(config.TrafficHeatCautionModifier) &&
            IsFinite(config.TrafficNoneThreshold) && IsFinite(config.TrafficLowThreshold) && IsFinite(config.TrafficBuildingThreshold) && IsFinite(config.TrafficSteadyThreshold) && IsFinite(config.TrafficHeavyThreshold) &&
            IsFinite(config.TrafficDangerousChurnMinimumInterestScore) && IsFinite(config.TrafficEstimatedPartyCountMultiplier) && IsFinite(config.TrafficEstimatedPartyCountScoreDivisor) &&
            config.TrafficNoneThreshold <= config.TrafficLowThreshold && config.TrafficLowThreshold <= config.TrafficBuildingThreshold && config.TrafficBuildingThreshold <= config.TrafficSteadyThreshold && config.TrafficSteadyThreshold <= config.TrafficHeavyThreshold &&
            config.TrafficDangerousChurnRecentDeathThreshold >= 1 && config.TrafficEstimatedPartyCountScoreDivisor > 0d && config.TrafficEstimatedPartyCountMultiplier >= 0d &&
            config.TrafficMinimumEstimatedConcurrentParties >= 0 && config.TrafficMaximumEstimatedConcurrentParties >= config.TrafficMinimumEstimatedConcurrentParties && config.TrafficMaximumEstimatedConcurrentParties > 0 &&
            config.TrafficEstimatedPartyCountLowThreshold >= 1 && config.TrafficEstimatedPartyCountMediumThreshold >= config.TrafficEstimatedPartyCountLowThreshold;

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
