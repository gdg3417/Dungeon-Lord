using System.Collections.Generic;
using DungeonBuilder.M0;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpPrimaryNextActionPresenterTests
    {
        [Test]
        public void Resolve_FirstContractIncomplete_OwnsPrimaryAction()
        {
            MvpPrimaryNextActionSummary result = MvpPrimaryNextActionPresenter.Resolve(
                AnalysisSummary(BasicRunAnalysisRecommendationPresenter.ReduceDangerKey),
                new GuidedMvpActionPathSummary { NextActionKey = GuidedMvpActionPathPanelPresenter.FallbackActionKey },
                new MvpFirstSessionObjectiveSummary { RuleResolved = true, IsComplete = false },
                new MvpPostContractGreedTrialSummary { IsActive = true, NextActionKey = MvpPostContractGreedTrialPresenter.NextActionTestGreedierSetupKey });

            Assert.That(result.ResolvedRule, Is.EqualTo(MvpPrimaryNextActionPresenter.RuleFirstContractIncomplete));
            Assert.That(result.PrimaryActionSource, Is.EqualTo(MvpPrimaryNextActionPresenter.SourceFirstContract));
            Assert.That(result.PrimaryActionKey, Is.EqualTo(MvpPrimaryNextActionPresenter.FirstContractIncompleteActionKey));
        }

        [Test]
        public void Resolve_GreedTrialIncomplete_OwnsAfterContractComplete()
        {
            MvpPrimaryNextActionSummary result = MvpPrimaryNextActionPresenter.Resolve(
                AnalysisSummary(BasicRunAnalysisRecommendationPresenter.ReduceDangerKey),
                null,
                CompleteContract(),
                new MvpPostContractGreedTrialSummary { IsActive = true, IsComplete = false, NextActionKey = MvpPostContractGreedTrialPresenter.NextActionStabilizeHeatKey });

            Assert.That(result.ResolvedRule, Is.EqualTo(MvpPrimaryNextActionPresenter.RuleGreedTrialIncomplete));
            Assert.That(result.PrimaryActionSource, Is.EqualTo(MvpPrimaryNextActionPresenter.SourceGreedTrial));
            Assert.That(result.PrimaryActionKey, Is.EqualTo(MvpPostContractGreedTrialPresenter.NextActionStabilizeHeatKey));
        }

        [Test]
        public void Resolve_AppliedAdjustment_BeatsAnalysisRecommendationAfterGreedTrialComplete()
        {
            MvpPrimaryNextActionSummary result = MvpPrimaryNextActionPresenter.Resolve(
                AppliedDangerAdjustmentSummary(),
                null,
                CompleteContract(),
                CompleteGreedTrial());

            Assert.That(result.ResolvedRule, Is.EqualTo(MvpPrimaryNextActionPresenter.RuleAppliedAdjustment));
            Assert.That(result.PrimaryActionSource, Is.EqualTo(MvpPrimaryNextActionPresenter.SourceAppliedAdjustment));
            Assert.That(result.PrimaryActionKey, Is.EqualTo(BasicRunAnalysisAppliedAdjustmentPresenter.RunAgainToTestChangeKey));
        }

        [Test]
        public void Resolve_AppliedAdjustment_BeatsIncompleteGreedTrialSoChangeCanBeTested()
        {
            MvpPrimaryNextActionSummary result = MvpPrimaryNextActionPresenter.Resolve(
                AppliedDangerAdjustmentSummary(),
                null,
                CompleteContract(),
                new MvpPostContractGreedTrialSummary { IsActive = true, IsComplete = false, NextActionKey = MvpPostContractGreedTrialPresenter.NextActionStabilizeHeatKey });

            Assert.That(result.ResolvedRule, Is.EqualTo(MvpPrimaryNextActionPresenter.RuleAppliedAdjustment));
            Assert.That(result.PrimaryActionSource, Is.EqualTo(MvpPrimaryNextActionPresenter.SourceAppliedAdjustment));
            Assert.That(result.PrimaryActionKey, Is.EqualTo(BasicRunAnalysisAppliedAdjustmentPresenter.RunAgainToTestChangeKey));
        }


        [Test]
        public void Resolve_AnalysisRecommendation_BeatsGuidedFallbackAfterGreedTrialComplete()
        {
            MvpPrimaryNextActionSummary result = MvpPrimaryNextActionPresenter.Resolve(
                AnalysisSummary(BasicRunAnalysisRecommendationPresenter.ReduceHeatKey),
                new GuidedMvpActionPathSummary { NextActionKey = GuidedMvpActionPathPanelPresenter.FallbackActionKey },
                CompleteContract(),
                CompleteGreedTrial());

            Assert.That(result.ResolvedRule, Is.EqualTo(MvpPrimaryNextActionPresenter.RuleAnalysisRecommendation));
            Assert.That(result.PrimaryActionSource, Is.EqualTo(MvpPrimaryNextActionPresenter.SourceAnalysis));
            Assert.That(result.PrimaryActionKey, Is.EqualTo(BasicRunAnalysisRecommendationPresenter.ReduceHeatKey));
        }

        [Test]
        public void BuildPanelText_LocalizesPrimaryActionWithoutLeakingKeys()
        {
            string text = MvpPrimaryNextActionPresenter.BuildPanelText(
                MvpPrimaryNextActionPresenter.Resolve(null, null, new MvpFirstSessionObjectiveSummary { RuleResolved = true, IsComplete = false }, null),
                Localize);

            Assert.That(text, Does.Contain("Next: Complete the First Dungeon Contract. (First Dungeon Contract)"));
            Assert.That(text, Does.Not.Contain("ui.mvp_"));
        }


        [TestCase(MvpPrimaryNextActionPresenter.FirstContractIncompleteActionKey, MvpPrimaryNextActionPresenter.SourceFirstContractKey, "Next: Complete the First Dungeon Contract. (First Dungeon Contract)")]
        [TestCase(MvpPostContractGreedTrialPresenter.NextActionStabilizeHeatKey, MvpPrimaryNextActionPresenter.SourceGreedTrialKey, "Next: Stabilize heat back to Peace while keeping the greed setup. (Post-Contract Greed Trial)")]
        [TestCase(BasicRunAnalysisAppliedAdjustmentPresenter.RunAgainToTestChangeKey, MvpPrimaryNextActionPresenter.SourceAppliedAdjustmentKey, "Next: Run again to test the placement change. (Applied activity-analysis change)")]
        [TestCase(BasicRunAnalysisRecommendationPresenter.ReduceDangerKey, MvpPrimaryNextActionPresenter.SourceAnalysisKey, "Next: Reduce danger or use a safer posture before pushing for more loot. (Adventurer Activity Analysis)")]
        public void BuildPanelText_PrimaryPresenterOwnsSingleNextPrefix(string actionKey, string sourceKey, string expected)
        {
            string text = MvpPrimaryNextActionPresenter.BuildPanelText(
                new MvpPrimaryNextActionSummary
                {
                    RuleResolved = true,
                    PrimaryActionKey = actionKey,
                    PrimaryActionSourceLabelKey = sourceKey
                },
                Localize);

            Assert.That(text, Is.EqualTo(expected));
            Assert.That(CountOccurrences(text, "Next:"), Is.EqualTo(1));
            Assert.That(text, Does.Not.Contain("Next: Next:"));
        }

        private static MvpFirstSessionObjectiveSummary CompleteContract() => new MvpFirstSessionObjectiveSummary { RuleResolved = true, IsComplete = true };
        private static MvpPostContractGreedTrialSummary CompleteGreedTrial() => new MvpPostContractGreedTrialSummary { RuleResolved = true, IsActive = true, IsComplete = true };

        private static MvpPlayerLoopSummary AnalysisSummary(string adviceKey) => new MvpPlayerLoopSummary
        {
            RuleResolved = true,
            AnalysisUnlocked = true,
            HasRunOutcome = true,
            AnalysisAdviceKey = adviceKey,
            PlacementEffects = new MvpPlacementEffectsSummary { RuleResolved = true },
            LatestRunPlacementEffects = new MvpPlacementEffectsSummary { RuleResolved = true }
        };

        private static MvpPlayerLoopSummary AppliedDangerAdjustmentSummary() => new MvpPlayerLoopSummary
        {
            RuleResolved = true,
            AnalysisUnlocked = true,
            HasRunOutcome = true,
            AnalysisAdviceKey = BasicRunAnalysisRecommendationPresenter.ReduceDangerKey,
            PlacementEffects = new MvpPlacementEffectsSummary { RuleResolved = true, Danger = 1 },
            LatestRunPlacementEffects = new MvpPlacementEffectsSummary { RuleResolved = true, Danger = 2 }
        };

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }
            return count;
        }

        private static string Localize(string key, string fallback) => Strings.TryGetValue(key, out string value) ? value : fallback;

        private static readonly Dictionary<string, string> Strings = new Dictionary<string, string>
        {
            [MvpPrimaryNextActionPresenter.CompactLineFormatKey] = "Next: {0} ({1})",
            [MvpPrimaryNextActionPresenter.SourceFirstContractKey] = "First Dungeon Contract",
            [MvpPrimaryNextActionPresenter.SourceGreedTrialKey] = "Post-Contract Greed Trial",
            [MvpPrimaryNextActionPresenter.SourceAppliedAdjustmentKey] = "Applied activity-analysis change",
            [MvpPrimaryNextActionPresenter.SourceAnalysisKey] = "Adventurer Activity Analysis",
            [MvpPrimaryNextActionPresenter.FirstContractIncompleteActionKey] = "Complete the First Dungeon Contract.",
            [MvpPostContractGreedTrialPresenter.NextActionStabilizeHeatKey] = "Stabilize heat back to Peace while keeping the greed setup.",
            [BasicRunAnalysisAppliedAdjustmentPresenter.RunAgainToTestChangeKey] = "Next: Run again to test the placement change.",
            [BasicRunAnalysisRecommendationPresenter.ReduceDangerKey] = "Next: Reduce danger or use a safer posture before pushing for more loot."
        };
    }
}
