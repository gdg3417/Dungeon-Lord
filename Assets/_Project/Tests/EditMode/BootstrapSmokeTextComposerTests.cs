using DungeonBuilder.M0;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class BootstrapSmokeTextComposerTests
    {
        [Test]
        public void BuildLoopSummarySectionText_AppliedAdjustmentIncludesUpdatedGuidedAction()
        {
            var context = new BootstrapSmokeTextComposer.Context(
                AnalysisSummary(),
                new GuidedMvpActionPathSummary
                {
                    RuleResolved = true,
                    CurrentStepId = GuidedMvpActionPathPresenter.StepTestPlacementChangeId,
                    CurrentStepStatusKey = GuidedMvpActionPathPresenter.StatusAppliedAnalysisAdjustmentKey,
                    NextActionKey = GuidedMvpActionPathPresenter.ActionRunAgainToTestChangeKey,
                    IsComplete = true,
                    HasAppliedAnalysisAdjustment = true,
                    AppliedAnalysisAdjustmentKey = BasicRunAnalysisAppliedAdjustmentPresenter.DangerLowerKey
                },
                null,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                null,
                string.Empty,
                string.Empty,
                false,
                string.Empty,
                0,
                1);

            string text = BootstrapSmokeTextComposer.BuildLoopSummarySectionText(context, Localize);

            Assert.That(text, Does.Contain("Suggested Next Action: Next: run again to test the placement change."));
            Assert.That(text, Does.Contain("Applied adjustment: Danger is lower than the latest visit. Run again to test the change."));
            Assert.That(text, Does.Contain("Guided MVP Action"));
            Assert.That(text, Does.Contain("Step: Test placement change"));
            Assert.That(text, Does.Contain("Status: A relevant placement adjustment has been applied."));
            Assert.That(text, Does.Contain("Next action: Run again to test the placement change."));
            Assert.That(text, Does.Contain("Path complete: Yes"));
        }


        [Test]
        public void BuildLoopSummarySectionText_CompleteFirstContractIncludesPayoffAndNextObjective()
        {
            var context = new BootstrapSmokeTextComposer.Context(
                AnalysisSummary(),
                new GuidedMvpActionPathSummary
                {
                    RuleResolved = true,
                    CurrentStepId = GuidedMvpActionPathPresenter.StepRunOrObserveId,
                    CurrentStepStatusKey = GuidedMvpActionPathPresenter.StatusRunOrObserveKey,
                    NextActionKey = GuidedMvpActionPathPresenter.ActionRunDungeonKey,
                    IsComplete = false
                },
                new MvpFirstSessionObjectiveSummary
                {
                    RuleResolved = true,
                    PathComplete = true,
                    RunObservedComplete = true,
                    RecoveredLootValue = 52,
                    RequiredRecoveredLootValue = 10,
                    AllowedMaxHeatTierId = CurrentHeatTierResolver.PeaceTierId,
                    CurrentHeatTierId = CurrentHeatTierResolver.PeaceTierId,
                    AnalysisComplete = true,
                    IsComplete = true
                },
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                null,
                string.Empty,
                string.Empty,
                false,
                string.Empty,
                0,
                1);

            string text = BootstrapSmokeTextComposer.BuildLoopSummarySectionText(context, Localize);

            Assert.That(text, Does.Contain("Contract status: Complete. Try a riskier setup or improve loot recovery."));
            Assert.That(text, Does.Contain("Completion: First contract complete. Your dungeon can attract adventurers, recover loot, control heat, and use analysis."));
            Assert.That(text, Does.Contain("Next objective: Test a greedier reward setup while keeping heat under control."));
        }

        private static MvpPlayerLoopSummary AnalysisSummary()
        {
            return new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasPlacementContext = true,
                HasRunOutcome = true,
                RunSucceeded = false,
                AnalysisUnlocked = true,
                HeatTierId = CurrentHeatTierResolver.NoticeTierId,
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey,
                AnalysisAdviceKey = BasicRunAnalysisRecommendationPresenter.ReduceDangerKey,
                PlacementEffects = new MvpPlacementEffectsSummary { RuleResolved = true, Danger = 3 },
                LatestRunPlacementEffects = new MvpPlacementEffectsSummary { RuleResolved = true, Danger = 4 }
            };
        }

        private static string Localize(string key, string fallback)
        {
            switch (key)
            {
                case "ui.mvp_smoke.section.status_format": return "{0} ({1}/{2})";
                case "ui.mvp_smoke.section.loop_summary": return "Loop Summary";
                case MvpLoopSummaryPanelPresenter.TitleKey: return "MVP Loop Summary";
                case MvpLoopSummaryPanelPresenter.SectionLineFormatKey: return "{0}: {1}";
                case MvpLoopSummaryPanelPresenter.CurrentDungeonSectionKey: return "Current Dungeon";
                case MvpLoopSummaryPanelPresenter.AdventurerIntentSectionKey: return "Expected next adventurer intent";
                case MvpLoopSummaryPanelPresenter.AdventurerPressureSectionKey: return "Adventurer pressure";
                case MvpLoopSummaryPanelPresenter.LatestRunSectionKey: return "Latest Adventurer Visit";
                case MvpLoopSummaryPanelPresenter.WhyItHappenedSectionKey: return "Why It Happened";
                case MvpLoopSummaryPanelPresenter.RewardsAndRiskSectionKey: return "Rewards and Risk";
                case MvpLoopSummaryPanelPresenter.ResearchSectionKey: return "Research";
                case MvpLoopSummaryPanelPresenter.SuggestedNextActionSectionKey: return "Suggested Next Action";
                case MvpLoopSummaryPanelPresenter.PlacementFormatKey: return "Dungeon composition: {0}";
                case MvpLoopSummaryPanelPresenter.PlacementEffectsFormatKey: return "Effects: {0}";
                case MvpLoopSummaryPanelPresenter.ValueNoPlacementKey: return "No dungeon placements yet";
                case MvpLoopSummaryPanelPresenter.RunFailedKey: return "Failed";
                case MvpLoopSummaryPanelPresenter.AnalysisRunFormatKey: return "Main reason: {0}. Analysis: {1}";
                case MvpLoopSummaryPanelPresenter.WhyDangerKey: return "danger pressure drove the result";
                case MvpLoopSummaryPanelPresenter.AnalysisDangerKey: return "Danger drove this visit.";
                case MvpLoopSummaryPanelPresenter.LootFormatKey: return "Loot: generated {0}, extracted {1}, tradeable {2}";
                case MvpLoopSummaryPanelPresenter.HeatFormatKey: return "Heat: {0:0.##} -> {1:0.##} ({2}). {3}";
                case MvpLoopSummaryPanelPresenter.ValueNoResearchKey: return "No research";
                case MvpLoopSummaryPanelPresenter.SuggestionFormatKey: return "{0}";
                case MvpLoopSummaryPanelPresenter.InlineSeparatorKey: return " | ";
                case MvpLoopSummaryPanelPresenter.RiskStableKey: return "Risk stayed steady.";
                case CurrentHeatTierResolver.NoticeTierId: return "Notice";
                case MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey: return "Next: adjust one placement before the next adventurer visit.";
                case BasicRunAnalysisAppliedAdjustmentPresenter.RunAgainToTestChangeKey: return "Next: run again to test the placement change.";
                case BasicRunAnalysisRecommendationPresenter.RecommendationFormatKey: return "Analysis recommendation: {0}";
                case BasicRunAnalysisRecommendationPresenter.ReduceDangerKey: return "Next: reduce danger or use a safer posture before pushing for more loot.";
                case BasicRunAnalysisPlacementTargetPresenter.AdjustmentTargetFormatKey: return "Adjustment target: {0}";
                case BasicRunAnalysisPlacementTargetPresenter.ReduceDangerTargetKey: return "Monster or trap first. Danger drove the recommendation; lower danger before pushing for more loot.";
                case BasicRunAnalysisAppliedAdjustmentPresenter.AppliedAdjustmentFormatKey: return "Applied adjustment: {0}";
                case BasicRunAnalysisAppliedAdjustmentPresenter.DangerLowerKey: return "Danger is lower than the latest visit. Run again to test the change.";
                case GuidedMvpActionPathPanelPresenter.TitleKey: return "Guided MVP Action";
                case GuidedMvpActionPathPanelPresenter.StepFormatKey: return "Step: {0}";
                case GuidedMvpActionPathPanelPresenter.StatusFormatKey: return "Status: {0}";
                case GuidedMvpActionPathPanelPresenter.NextActionFormatKey: return "Next action: {0}";
                case GuidedMvpActionPathPanelPresenter.CompleteFormatKey: return "Path complete: {0}";
                case GuidedMvpActionPathPanelPresenter.CompleteYesKey: return "Yes";
                case GuidedMvpActionPathPresenter.StepTestPlacementChangeId: return "Test placement change";
                case GuidedMvpActionPathPresenter.StatusAppliedAnalysisAdjustmentKey: return "A relevant placement adjustment has been applied.";
                case GuidedMvpActionPathPresenter.ActionRunAgainToTestChangeKey: return "Run again to test the placement change.";
                case GuidedMvpActionPathPresenter.StepRunOrObserveId: return "Observe adventurer visit";
                case GuidedMvpActionPathPresenter.StatusRunOrObserveKey: return "Visit observed. Review activity analysis.";
                case GuidedMvpActionPathPresenter.ActionRunDungeonKey: return "Review Adventurer Activity Analysis.";
                case MvpFirstSessionObjectivePresenter.TitleKey: return "First Dungeon Contract";
                case MvpFirstSessionObjectivePresenter.PathBuiltFormatKey: return "Path built: {0}";
                case MvpFirstSessionObjectivePresenter.RunObservedFormatKey: return "Visit observed: {0}";
                case MvpFirstSessionObjectivePresenter.LootRecoveredFormatKey: return "Loot recovered: {0} / {1}";
                case MvpFirstSessionObjectivePresenter.HeatTargetFormatKey: return "Heat target: {0} (current: {1})";
                case MvpFirstSessionObjectivePresenter.AnalysisFormatKey: return "Analysis: {0}";
                case MvpFirstSessionObjectivePresenter.StatusFormatKey: return "Contract status: {0}";
                case MvpFirstSessionObjectivePresenter.CompleteKey: return "complete";
                case MvpFirstSessionObjectivePresenter.AnalysisUnlockedKey: return "Adventurer Activity Analysis unlocked";
                case MvpFirstSessionObjectivePresenter.StatusCompleteKey: return "Complete. Try a riskier setup or improve loot recovery.";
                case MvpFirstSessionObjectivePresenter.CompletionFormatKey: return "Completion: {0}";
                case MvpFirstSessionObjectivePresenter.CompletionFirstContractCompleteKey: return "First contract complete. Your dungeon can attract adventurers, recover loot, control heat, and use analysis.";
                case MvpFirstSessionObjectivePresenter.NextObjectiveFormatKey: return "Next objective: {0}";
                case MvpFirstSessionObjectivePresenter.NextObjectiveGreedierRewardSetupKey: return "Test a greedier reward setup while keeping heat under control.";
                case CurrentHeatTierResolver.PeaceTierId: return "Peace";
                default: return fallback;
            }
        }
    }
}
