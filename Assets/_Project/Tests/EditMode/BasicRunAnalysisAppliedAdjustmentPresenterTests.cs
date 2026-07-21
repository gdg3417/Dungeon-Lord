#if UNITY_EDITOR
using DungeonBuilder.M0;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class BasicRunAnalysisAppliedAdjustmentPresenterTests
    {
        [Test]
        public void Resolve_NullOrUnresolvedSummary_ReturnsNoAppliedAdjustment()
        {
            Assert.That(BasicRunAnalysisAppliedAdjustmentPresenter.Resolve(null), Is.Null);
            Assert.That(BasicRunAnalysisAppliedAdjustmentPresenter.Resolve(new MvpPlayerLoopSummary()), Is.Null);
        }

        [Test]
        public void Resolve_NoLatestRun_ReturnsNoAppliedAdjustment()
        {
            MvpPlayerLoopSummary summary = Summary(BasicRunAnalysisRecommendationPresenter.ReduceDangerKey, Effects(danger: 3), Effects(danger: 4));
            summary.HasRunOutcome = false;

            Assert.That(BasicRunAnalysisAppliedAdjustmentPresenter.Resolve(summary), Is.Null);
        }

        [Test]
        public void Resolve_ReduceDangerAndCurrentDangerLower_ReturnsDangerLowerResult()
        {
            BasicRunAnalysisAppliedAdjustmentResult result = BasicRunAnalysisAppliedAdjustmentPresenter.Resolve(
                Summary(BasicRunAnalysisRecommendationPresenter.ReduceDangerKey, Effects(danger: 3), Effects(danger: 4)));

            Assert.That(result, Is.Not.Null);
            Assert.That(result.AdjustmentKey, Is.EqualTo(BasicRunAnalysisAppliedAdjustmentPresenter.DangerLowerKey));
            Assert.That(result.NextActionKey, Is.EqualTo(BasicRunAnalysisAppliedAdjustmentPresenter.RunAgainToTestChangeKey));
        }

        [TestCase(4, 4)]
        [TestCase(5, 4)]
        public void Resolve_ReduceDangerAndCurrentDangerUnchangedOrHigher_ReturnsNoAppliedAdjustment(int currentDanger, int latestDanger)
        {
            Assert.That(BasicRunAnalysisAppliedAdjustmentPresenter.Resolve(
                Summary(BasicRunAnalysisRecommendationPresenter.ReduceDangerKey, Effects(danger: currentDanger), Effects(danger: latestDanger))), Is.Null);
        }

        [Test]
        public void Resolve_ReduceHeatAndCurrentHeatPressureLower_ReturnsHeatPressureLowerResult()
        {
            BasicRunAnalysisAppliedAdjustmentResult result = BasicRunAnalysisAppliedAdjustmentPresenter.Resolve(
                Summary(BasicRunAnalysisRecommendationPresenter.ReduceHeatKey, Effects(heatPressure: 1), Effects(heatPressure: 3)));

            Assert.That(result.AdjustmentKey, Is.EqualTo(BasicRunAnalysisAppliedAdjustmentPresenter.HeatPressureLowerKey));
        }

        [Test]
        public void Resolve_ImproveExtractionAndCurrentPathCapacityHigher_ReturnsPathCapacityHigherResult()
        {
            BasicRunAnalysisAppliedAdjustmentResult result = BasicRunAnalysisAppliedAdjustmentPresenter.Resolve(
                Summary(BasicRunAnalysisRecommendationPresenter.ImproveExtractionKey, Effects(pathCapacity: 3), Effects(pathCapacity: 1)));

            Assert.That(result.AdjustmentKey, Is.EqualTo(BasicRunAnalysisAppliedAdjustmentPresenter.PathCapacityHigherKey));
        }

        [TestCase(2, 1, 0, 0)]
        [TestCase(0, 0, 2, 1)]
        public void Resolve_TestGreedierAndCurrentLootOrAttractionHigher_ReturnsLootOrAttractionHigherResult(int currentLoot, int latestLoot, int currentAttraction, int latestAttraction)
        {
            BasicRunAnalysisAppliedAdjustmentResult result = BasicRunAnalysisAppliedAdjustmentPresenter.Resolve(
                Summary(BasicRunAnalysisRecommendationPresenter.TestGreedierKey, Effects(lootBonus: currentLoot, attraction: currentAttraction), Effects(lootBonus: latestLoot, attraction: latestAttraction)));

            Assert.That(result.AdjustmentKey, Is.EqualTo(BasicRunAnalysisAppliedAdjustmentPresenter.LootOrAttractionHigherKey));
        }

        private static MvpPlayerLoopSummary Summary(string recommendationKey, MvpPlacementEffectsSummary current, MvpPlacementEffectsSummary latest)
        {
            return new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                AnalysisUnlocked = true,
                HasRunOutcome = true,
                AnalysisAdviceKey = recommendationKey,
                PlacementEffects = current,
                LatestRunPlacementEffects = latest,
                LatestRunConfiguredPlacementEffects = latest
            };
        }

        private static MvpPlacementEffectsSummary Effects(int pathCapacity = 0, int danger = 0, int heatPressure = 0, int lootBonus = 0, int attraction = 0)
        {
            return new MvpPlacementEffectsSummary
            {
                RuleResolved = true,
                PathCapacity = pathCapacity,
                Danger = danger,
                HeatPressure = heatPressure,
                LootBonus = lootBonus,
                Attraction = attraction
            };
        }
    }
}
#endif
