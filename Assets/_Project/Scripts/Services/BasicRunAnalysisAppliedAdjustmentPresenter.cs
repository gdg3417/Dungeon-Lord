using System;

namespace DungeonBuilder.M0
{
    public sealed class BasicRunAnalysisAppliedAdjustmentResult
    {
        public bool Applied;
        public string AdjustmentKey;
        public string NextActionKey;
    }

    public static class BasicRunAnalysisAppliedAdjustmentPresenter
    {
        public const string AppliedAdjustmentFormatKey = "ui.mvp_loop.analysis.applied_adjustment_format";
        public const string DangerLowerKey = "mvp_loop.analysis.applied_adjustment.danger_lower";
        public const string HeatPressureLowerKey = "mvp_loop.analysis.applied_adjustment.heat_pressure_lower";
        public const string PathCapacityHigherKey = "mvp_loop.analysis.applied_adjustment.path_capacity_higher";
        public const string LootOrAttractionHigherKey = "mvp_loop.analysis.applied_adjustment.loot_or_attraction_higher";
        public const string EffectsChangedKey = "mvp_loop.analysis.applied_adjustment.effects_changed";
        public const string RunAgainToTestChangeKey = "mvp_loop.suggestion.analysis.run_again_to_test_change";

        public static BasicRunAnalysisAppliedAdjustmentResult Resolve(MvpPlayerLoopSummary summary)
        {
            if (summary == null || !summary.RuleResolved || !summary.AnalysisUnlocked || !summary.HasRunOutcome)
            {
                return null;
            }

            MvpPlacementEffectsSummary current = summary.PlacementEffects;
            MvpPlacementEffectsSummary latest = summary.LatestRunPlacementEffects;
            if (current == null || latest == null || !current.RuleResolved || !latest.RuleResolved)
            {
                return null;
            }

            string recommendationKey = !string.IsNullOrWhiteSpace(summary.AnalysisAdviceKey)
                ? summary.AnalysisAdviceKey
                : BasicRunAnalysisRecommendationPresenter.ResolveKey(summary);
            string adjustmentKey = ResolveAdjustmentKey(recommendationKey, current, latest);
            if (string.IsNullOrWhiteSpace(adjustmentKey))
            {
                return null;
            }

            return new BasicRunAnalysisAppliedAdjustmentResult
            {
                Applied = true,
                AdjustmentKey = adjustmentKey,
                NextActionKey = RunAgainToTestChangeKey
            };
        }

        public static string ResolveNextActionKey(MvpPlayerLoopSummary summary, string fallbackKey)
        {
            BasicRunAnalysisAppliedAdjustmentResult result = Resolve(summary);
            return result != null && result.Applied && !string.IsNullOrWhiteSpace(result.NextActionKey)
                ? result.NextActionKey
                : fallbackKey;
        }

        public static string BuildAppliedAdjustmentLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            BasicRunAnalysisAppliedAdjustmentResult result = Resolve(summary);
            if (result == null || !result.Applied || string.IsNullOrWhiteSpace(result.AdjustmentKey))
            {
                return string.Empty;
            }

            return string.Format(Localize(localize, AppliedAdjustmentFormatKey), Localize(localize, result.AdjustmentKey));
        }

        private static string ResolveAdjustmentKey(string recommendationKey, MvpPlacementEffectsSummary current, MvpPlacementEffectsSummary latest)
        {
            switch (recommendationKey)
            {
                case BasicRunAnalysisRecommendationPresenter.ReduceDangerKey:
                    return current.Danger < latest.Danger ? DangerLowerKey : string.Empty;
                case BasicRunAnalysisRecommendationPresenter.ReduceHeatKey:
                    return current.HeatPressure < latest.HeatPressure ? HeatPressureLowerKey : string.Empty;
                case BasicRunAnalysisRecommendationPresenter.ImproveExtractionKey:
                    return current.PathCapacity > latest.PathCapacity ? PathCapacityHigherKey : string.Empty;
                case BasicRunAnalysisRecommendationPresenter.TestGreedierKey:
                    return current.LootBonus > latest.LootBonus || current.Attraction > latest.Attraction ? LootOrAttractionHigherKey : string.Empty;
                default:
                    return EffectsDiffer(current, latest) ? EffectsChangedKey : string.Empty;
            }
        }

        private static bool EffectsDiffer(MvpPlacementEffectsSummary current, MvpPlacementEffectsSummary latest)
        {
            return current.PathCapacity != latest.PathCapacity ||
                   current.Danger != latest.Danger ||
                   current.ManaPressure != latest.ManaPressure ||
                   current.HeatPressure != latest.HeatPressure ||
                   current.LootBonus != latest.LootBonus ||
                   current.Attraction != latest.Attraction;
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            return localize != null ? localize(key, key) : key;
        }
    }
}
