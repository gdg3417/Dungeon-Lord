using System;

namespace DungeonBuilder.M0
{
    public static class BasicRunAnalysisPlacementTargetPresenter
    {
        public const string ReduceDangerTargetKey = "mvp_loop.analysis.target.reduce_danger";
        public const string ReduceHeatTargetKey = "mvp_loop.analysis.target.reduce_heat";
        public const string ImproveExtractionTargetKey = "mvp_loop.analysis.target.improve_extraction";
        public const string TestGreedierTargetKey = "mvp_loop.analysis.target.test_greedier";
        public const string FallbackTargetKey = "mvp_loop.analysis.target.fallback";
        public const string AdjustmentTargetFormatKey = "ui.mvp_loop.analysis.adjustment_target_format";

        public static string ResolveTargetKey(MvpPlayerLoopSummary summary)
        {
            if (summary == null || !summary.RuleResolved || !summary.AnalysisUnlocked || !summary.HasRunOutcome)
            {
                return string.Empty;
            }

            return ResolveTargetKey(BasicRunAnalysisRecommendationPresenter.ResolveKey(summary));
        }

        public static string ResolveTargetKey(string recommendationKey)
        {
            switch (recommendationKey)
            {
                case BasicRunAnalysisRecommendationPresenter.ReduceDangerKey:
                    return ReduceDangerTargetKey;
                case BasicRunAnalysisRecommendationPresenter.ReduceHeatKey:
                    return ReduceHeatTargetKey;
                case BasicRunAnalysisRecommendationPresenter.ImproveExtractionKey:
                    return ImproveExtractionTargetKey;
                case BasicRunAnalysisRecommendationPresenter.TestGreedierKey:
                    return TestGreedierTargetKey;
                default:
                    return FallbackTargetKey;
            }
        }

        public static string BuildTargetLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            string targetKey = ResolveTargetKey(summary);
            if (string.IsNullOrEmpty(targetKey))
            {
                return string.Empty;
            }

            return string.Format(Localize(localize, AdjustmentTargetFormatKey), Localize(localize, targetKey));
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            return localize != null ? localize(key, key) : key;
        }
    }
}
