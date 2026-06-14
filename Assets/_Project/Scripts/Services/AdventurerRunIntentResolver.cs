using System;

namespace DungeonBuilder.M0
{
    public static class AdventurerRunIntentResolver
    {
        public const string ReasonLootHighHeatLowKey = "ui.adventurer_intent.reason.loot_high_heat_low";
        public const string ReasonDeathsHeatKey = "ui.adventurer_intent.reason.deaths_heat";
        public const string ReasonModerateKey = "ui.adventurer_intent.reason.moderate";
        public const string ReasonDangerKey = "ui.adventurer_intent.reason.danger";
        public const string ReasonFallbackKey = "ui.adventurer_intent.reason.fallback";

        public static AdventurerRunIntentSummary Resolve(RunSimulationConfig config, MvpPlacementEffectsSummary effects, double currentHeat, CurrentHeatTierSummary heatTier, RunOutcomeRecord latestRun)
        {
            var summary = new AdventurerRunIntentSummary
            {
                RuleSourceId = config?.AdventurerIntentRuleSourceId ?? string.Empty,
                IntentId = RunPostureResolver.BalancedId,
                PostureId = RunPostureResolver.BalancedId,
                PrimaryReasonKey = ReasonFallbackKey,
                SecondaryReasonKey = string.Empty,
                WouldMutateState = false
            };

            if (!HasValidConfig(config))
            {
                summary.DeterministicErrorCode = (int)AdventurerRunIntentSummaryErrorCode.MissingOrInvalidConfig;
                return summary;
            }

            int heatRank = ResolveHeatTierRank(heatTier);
            if (heatRank < 0)
            {
                summary.DeterministicErrorCode = (int)AdventurerRunIntentSummaryErrorCode.InvalidHeatTier;
                return summary;
            }

            int danger = effects != null && effects.RuleResolved ? effects.Danger : 0;
            int loot = effects != null && effects.RuleResolved ? effects.LootBonus : 0;
            int attraction = effects != null && effects.RuleResolved ? effects.Attraction : 0;
            int heatPressure = effects != null && effects.RuleResolved ? effects.HeatPressure : 0;
            int pathCapacity = effects != null && effects.RuleResolved ? effects.PathCapacity : 0;
            int recentDeaths = latestRun?.SurvivalSummary?.DeathCount ?? 0;

            double greedy = (loot * config.IntentGreedyScorePerLoot) + (attraction * config.IntentGreedyScorePerAttraction) - (heatRank * config.IntentGreedyPenaltyPerHeatTierRank) - (recentDeaths * config.IntentGreedyPenaltyPerRecentDeath) - (danger * config.IntentGreedyPenaltyPerDanger);
            double cautious = (danger * config.IntentCautiousScorePerDanger) + (heatPressure * config.IntentCautiousScorePerHeatPressure) + (heatRank * config.IntentCautiousScorePerHeatTierRank) + (recentDeaths * config.IntentCautiousScorePerRecentDeath) - (pathCapacity * config.IntentCautiousReductionPerPathCapacity);
            double risk = danger + heatPressure + heatRank + recentDeaths;
            double reward = loot + attraction;
            double balanced = config.IntentBalancedBaseScore - (Math.Abs(risk - config.IntentModerateRiskTarget) + Math.Abs(reward - config.IntentModerateRewardTarget)) * config.IntentBalancedPenaltyPerModerateDistance;
            balanced -= Math.Abs(greedy - cautious) * config.IntentBalancedPenaltyPerExtremeScoreDelta;

            if (!IsFinite(greedy) || !IsFinite(cautious) || !IsFinite(balanced))
            {
                summary.DeterministicErrorCode = (int)AdventurerRunIntentSummaryErrorCode.AggregateOverflow;
                return summary;
            }

            summary.GreedyScore = Clamp(greedy, config.IntentMinimumScore, config.IntentMaximumScore);
            summary.CautiousScore = Clamp(cautious, config.IntentMinimumScore, config.IntentMaximumScore);
            summary.BalancedScore = Clamp(balanced, config.IntentMinimumScore, config.IntentMaximumScore);
            summary.IntentId = SelectIntent(summary.CautiousScore, summary.BalancedScore, summary.GreedyScore);
            summary.PostureId = summary.IntentId;
            summary.ConfidenceScore = Math.Max(summary.CautiousScore, Math.Max(summary.BalancedScore, summary.GreedyScore));
            summary.PrimaryReasonKey = ResolveReason(summary.IntentId, recentDeaths, heatRank, danger);
            summary.SecondaryReasonKey = string.Empty;
            summary.RuleResolved = true;
            summary.DeterministicErrorCode = (int)AdventurerRunIntentSummaryErrorCode.None;
            return summary;
        }

        private static string SelectIntent(double cautious, double balanced, double greedy)
        {
            if (greedy > cautious && greedy > balanced) return RunPostureResolver.GreedyId;
            if (cautious > greedy && cautious > balanced) return RunPostureResolver.CautiousId;
            return RunPostureResolver.BalancedId;
        }

        private static string ResolveReason(string intentId, int recentDeaths, int heatRank, int danger)
        {
            if (string.Equals(intentId, RunPostureResolver.GreedyId, StringComparison.Ordinal)) return ReasonLootHighHeatLowKey;
            if (string.Equals(intentId, RunPostureResolver.CautiousId, StringComparison.Ordinal)) return recentDeaths > 0 || heatRank > 0 ? ReasonDeathsHeatKey : ReasonDangerKey;
            return ReasonModerateKey;
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
            return config != null && !string.IsNullOrWhiteSpace(config.AdventurerIntentRuleSourceId) &&
                   IsFinite(config.IntentMinimumScore) && IsFinite(config.IntentMaximumScore) && config.IntentMinimumScore <= config.IntentMaximumScore &&
                   IsFinite(config.IntentGreedyScorePerLoot) && IsFinite(config.IntentGreedyScorePerAttraction) && IsFinite(config.IntentGreedyPenaltyPerHeatTierRank) && IsFinite(config.IntentGreedyPenaltyPerRecentDeath) && IsFinite(config.IntentGreedyPenaltyPerDanger) &&
                   IsFinite(config.IntentCautiousScorePerDanger) && IsFinite(config.IntentCautiousScorePerHeatPressure) && IsFinite(config.IntentCautiousScorePerHeatTierRank) && IsFinite(config.IntentCautiousScorePerRecentDeath) && IsFinite(config.IntentCautiousReductionPerPathCapacity) &&
                   IsFinite(config.IntentBalancedBaseScore) && IsFinite(config.IntentBalancedPenaltyPerExtremeScoreDelta) && IsFinite(config.IntentModerateRiskTarget) && IsFinite(config.IntentModerateRewardTarget) && IsFinite(config.IntentBalancedPenaltyPerModerateDistance);
        }

        private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));
        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
