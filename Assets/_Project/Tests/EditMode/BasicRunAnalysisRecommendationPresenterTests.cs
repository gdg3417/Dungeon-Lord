#if UNITY_EDITOR
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests
{
    public class BasicRunAnalysisRecommendationPresenterTests
    {
        [Test]
        public void ResolveKey_DeathsRecommendSurvivabilityBeforeOtherPressures()
        {
            var summary = UnlockedSummary();
            summary.LatestRunDeathCount = 1;
            summary.HeatBefore = 1d;
            summary.HeatAfter = 5d;

            Assert.That(BasicRunAnalysisRecommendationPresenter.ResolveKey(summary), Is.EqualTo(BasicRunAnalysisRecommendationPresenter.ReduceDangerKey));
        }

        [Test]
        public void ResolveKey_HeatIncreaseRecommendsHeatPressureReduction()
        {
            var summary = UnlockedSummary();
            summary.HeatBefore = 2d;
            summary.HeatAfter = 4d;

            Assert.That(BasicRunAnalysisRecommendationPresenter.ResolveKey(summary), Is.EqualTo(BasicRunAnalysisRecommendationPresenter.ReduceHeatKey));
        }

        [Test]
        public void ResolveKey_PoorLootExtractionRecommendsExtractionImprovement()
        {
            var summary = UnlockedSummary();
            summary.LootGeneratedWorldValue = 10;
            summary.LootExtractedWorldValue = 3;
            summary.HeatBefore = 4d;
            summary.HeatAfter = 4d;

            Assert.That(BasicRunAnalysisRecommendationPresenter.ResolveKey(summary), Is.EqualTo(BasicRunAnalysisRecommendationPresenter.ImproveExtractionKey));
        }

        [Test]
        public void ResolveKey_ControlledLootAndHeatRecommendGreedierRewardTest()
        {
            var summary = UnlockedSummary();
            summary.LootGeneratedWorldValue = 10;
            summary.LootExtractedWorldValue = 10;
            summary.HeatBefore = 4d;
            summary.HeatAfter = 3d;
            summary.HeatTierId = CurrentHeatTierResolver.PeaceTierId;

            Assert.That(BasicRunAnalysisRecommendationPresenter.ResolveKey(summary), Is.EqualTo(BasicRunAnalysisRecommendationPresenter.TestGreedierKey));
        }

        [Test]
        public void ResolveKey_NullOrMissingSummaryReturnsSafeDeterministicFallback()
        {
            Assert.That(BasicRunAnalysisRecommendationPresenter.ResolveKey(null), Is.EqualTo(BasicRunAnalysisRecommendationPresenter.AdjustAndRunAgainKey));
            Assert.That(BasicRunAnalysisRecommendationPresenter.ResolveKey(new MvpPlayerLoopSummary()), Is.EqualTo(BasicRunAnalysisRecommendationPresenter.AdjustAndRunAgainKey));
        }

        [Test]
        public void ResolveKey_DoesNotMutateSummaryState()
        {
            var summary = UnlockedSummary();
            summary.LootGeneratedWorldValue = 5;
            summary.LootExtractedWorldValue = 1;
            string before = UnityEngine.JsonUtility.ToJson(summary);

            string first = BasicRunAnalysisRecommendationPresenter.ResolveKey(summary);
            string second = BasicRunAnalysisRecommendationPresenter.ResolveKey(summary);

            Assert.That(first, Is.EqualTo(BasicRunAnalysisRecommendationPresenter.ImproveExtractionKey));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(UnityEngine.JsonUtility.ToJson(summary), Is.EqualTo(before));
        }

        [Test]
        public void BuildRecommendationLine_UsesLocalizedFormatAndRecommendationText()
        {
            var summary = UnlockedSummary();
            summary.LatestRunDeathCount = 1;

            string line = BasicRunAnalysisRecommendationPresenter.BuildRecommendationLine(summary, (key, fallback) => key == BasicRunAnalysisRecommendationPresenter.RecommendationFormatKey ? "Analysis recommendation: {0}" : "LOC[" + key + "]");

            Assert.That(line, Is.EqualTo("Analysis recommendation: LOC[" + BasicRunAnalysisRecommendationPresenter.ReduceDangerKey + "]"));
        }

        private static MvpPlayerLoopSummary UnlockedSummary()
        {
            return new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                AnalysisUnlocked = true,
                HasRunOutcome = true,
                HeatTierId = CurrentHeatTierResolver.PeaceTierId
            };
        }
    }
}
#endif
