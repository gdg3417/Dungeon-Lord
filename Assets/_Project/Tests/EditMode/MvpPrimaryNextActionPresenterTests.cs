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

            Assert.That(text, Does.Contain("Primary Next Action"));
            Assert.That(text, Does.Contain("Do next: Complete the First Dungeon Contract."));
            Assert.That(text, Does.Contain("Priority source: First Dungeon Contract"));
            Assert.That(text, Does.Not.Contain("ui.mvp_"));
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

        private static string Localize(string key, string fallback) => Strings.TryGetValue(key, out string value) ? value : fallback;

        private static readonly Dictionary<string, string> Strings = new Dictionary<string, string>
        {
            [MvpPrimaryNextActionPresenter.TitleKey] = "Primary Next Action",
            [MvpPrimaryNextActionPresenter.ActionFormatKey] = "Do next: {0}",
            [MvpPrimaryNextActionPresenter.SourceFormatKey] = "Priority source: {0}",
            [MvpPrimaryNextActionPresenter.SourceFirstContractKey] = "First Dungeon Contract",
            [MvpPrimaryNextActionPresenter.FirstContractIncompleteActionKey] = "Complete the First Dungeon Contract."
        };
    }
}
