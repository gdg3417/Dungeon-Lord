using System;

namespace DungeonBuilder.M0
{
    public static class BasicRunAnalysisRecommendationPresenter
    {
        public const string RunForAnalysisKey = "mvp_loop.suggestion.analysis.run_for_analysis";
        public const string ReduceDangerKey = "mvp_loop.suggestion.analysis.reduce_danger";
        public const string ReduceHeatKey = "mvp_loop.suggestion.analysis.reduce_heat";
        public const string ImproveExtractionKey = "mvp_loop.suggestion.analysis.improve_extraction";
        public const string TestGreedierKey = "mvp_loop.suggestion.analysis.test_greedier";
        public const string AdjustAndRunAgainKey = "mvp_loop.suggestion.repeat_or_improve_placement";
        public const string RecommendationFormatKey = "ui.mvp_loop.analysis.recommendation_format";

        public static string ResolveKey(MvpPlayerLoopSummary summary)
        {
            if (summary == null || !summary.RuleResolved)
            {
                return AdjustAndRunAgainKey;
            }

            if (!summary.HasRunOutcome)
            {
                return RunForAnalysisKey;
            }

            if (summary.LatestRunDeathCount > 0 || DangerIsDominantPressure(summary.LatestRunPlacementEffects))
            {
                return ReduceDangerKey;
            }

            if (summary.HeatAfter > summary.HeatBefore || string.Equals(summary.HeatTierId, CurrentHeatTierResolver.ConcernTierId, StringComparison.Ordinal))
            {
                return ReduceHeatKey;
            }

            if (summary.LootGeneratedWorldValue > 0 && summary.LootExtractedWorldValue < summary.LootGeneratedWorldValue)
            {
                return ImproveExtractionKey;
            }

            if (summary.LootGeneratedWorldValue > 0 && summary.LootExtractedWorldValue >= summary.LootGeneratedWorldValue && HeatIsControlled(summary))
            {
                return TestGreedierKey;
            }

            return AdjustAndRunAgainKey;
        }

        public static string BuildRecommendationLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.AnalysisUnlocked)
            {
                return string.Empty;
            }

            return string.Format(Localize(localize, RecommendationFormatKey), Localize(localize, ResolveKey(summary)));
        }

        private static bool DangerIsDominantPressure(MvpPlacementEffectsSummary effects)
        {
            if (effects == null || !effects.RuleResolved || effects.Danger <= 0) return false;
            int danger = Math.Abs(effects.Danger);
            return danger >= Math.Abs(effects.PathCapacity) &&
                   danger >= Math.Abs(effects.ManaPressure) &&
                   danger >= Math.Abs(effects.HeatPressure) &&
                   danger >= Math.Abs(effects.LootBonus) &&
                   danger >= Math.Abs(effects.Attraction);
        }

        private static bool HeatIsControlled(MvpPlayerLoopSummary summary)
        {
            return summary != null && summary.HeatAfter <= summary.HeatBefore &&
                   !string.Equals(summary.HeatTierId, CurrentHeatTierResolver.ConcernTierId, StringComparison.Ordinal);
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            return localize != null ? localize(key, key) : key;
        }
    }
}
