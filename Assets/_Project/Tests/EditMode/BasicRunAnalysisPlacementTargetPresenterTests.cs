#if UNITY_EDITOR
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests
{
    public class BasicRunAnalysisPlacementTargetPresenterTests
    {
        [Test]
        public void ResolveTargetKey_ReduceDangerTargetsMonsterOrTrap()
        {
            Assert.That(BasicRunAnalysisPlacementTargetPresenter.ResolveTargetKey(BasicRunAnalysisRecommendationPresenter.ReduceDangerKey), Is.EqualTo(BasicRunAnalysisPlacementTargetPresenter.ReduceDangerTargetKey));
        }

        [Test]
        public void ResolveTargetKey_ReduceHeatTargetsTrapOrLootNode()
        {
            Assert.That(BasicRunAnalysisPlacementTargetPresenter.ResolveTargetKey(BasicRunAnalysisRecommendationPresenter.ReduceHeatKey), Is.EqualTo(BasicRunAnalysisPlacementTargetPresenter.ReduceHeatTargetKey));
        }

        [Test]
        public void ResolveTargetKey_ImproveExtractionTargetsRoomOrMonster()
        {
            Assert.That(BasicRunAnalysisPlacementTargetPresenter.ResolveTargetKey(BasicRunAnalysisRecommendationPresenter.ImproveExtractionKey), Is.EqualTo(BasicRunAnalysisPlacementTargetPresenter.ImproveExtractionTargetKey));
        }

        [Test]
        public void ResolveTargetKey_TestGreedierTargetsLootNode()
        {
            Assert.That(BasicRunAnalysisPlacementTargetPresenter.ResolveTargetKey(BasicRunAnalysisRecommendationPresenter.TestGreedierKey), Is.EqualTo(BasicRunAnalysisPlacementTargetPresenter.TestGreedierTargetKey));
        }

        [Test]
        public void ResolveTargetKey_UnknownRecommendationTargetsAnyOnePlacement()
        {
            Assert.That(BasicRunAnalysisPlacementTargetPresenter.ResolveTargetKey("unknown"), Is.EqualTo(BasicRunAnalysisPlacementTargetPresenter.FallbackTargetKey));
        }

        [Test]
        public void ResolveTargetKey_BasicRunAnalysisLockedReturnsSafeEmptyTarget()
        {
            var summary = UnlockedSummary();
            summary.AnalysisUnlocked = false;

            Assert.That(BasicRunAnalysisPlacementTargetPresenter.ResolveTargetKey(summary), Is.EqualTo(string.Empty));
            Assert.That(BasicRunAnalysisPlacementTargetPresenter.BuildTargetLine(summary, Localize), Is.EqualTo(string.Empty));
        }

        [Test]
        public void ResolveTargetKey_NoRunOutcomeReturnsSafeEmptyTarget()
        {
            var summary = UnlockedSummary();
            summary.HasRunOutcome = false;

            Assert.That(BasicRunAnalysisPlacementTargetPresenter.ResolveTargetKey(summary), Is.EqualTo(string.Empty));
        }

        [Test]
        public void ResolveTargetKey_UsesCurrentRecommendationDeterministicallyWithoutMutation()
        {
            var summary = UnlockedSummary();
            summary.LootGeneratedWorldValue = 8;
            summary.LootExtractedWorldValue = 2;
            string before = UnityEngine.JsonUtility.ToJson(summary);

            string first = BasicRunAnalysisPlacementTargetPresenter.ResolveTargetKey(summary);
            string second = BasicRunAnalysisPlacementTargetPresenter.ResolveTargetKey(summary);

            Assert.That(first, Is.EqualTo(BasicRunAnalysisPlacementTargetPresenter.ImproveExtractionTargetKey));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(UnityEngine.JsonUtility.ToJson(summary), Is.EqualTo(before));
        }

        [Test]
        public void BuildTargetLine_UsesLocalizedFormatAndTargetText()
        {
            var summary = UnlockedSummary();
            summary.LatestRunDeathCount = 1;

            string line = BasicRunAnalysisPlacementTargetPresenter.BuildTargetLine(summary, Localize);

            Assert.That(line, Is.EqualTo("Adjustment target: LOC[" + BasicRunAnalysisPlacementTargetPresenter.ReduceDangerTargetKey + "]"));
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

        private static string Localize(string key, string fallback)
        {
            return key == BasicRunAnalysisPlacementTargetPresenter.AdjustmentTargetFormatKey ? "Adjustment target: {0}" : "LOC[" + key + "]";
        }
    }
}
#endif
