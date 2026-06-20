using System;

namespace DungeonBuilder.M0
{
    public static class MvpPrimaryNextActionPresenter
    {
        public const string RuleFirstContractIncomplete = "first_contract_incomplete";
        public const string RuleGreedTrialIncomplete = "greed_trial_incomplete";
        public const string RuleAppliedAdjustment = "applied_analysis_adjustment";
        public const string RuleAnalysisRecommendation = "analysis_recommendation";
        public const string RuleGuidedOrSummaryFallback = "guided_or_summary_fallback";

        public const string SourceFirstContract = "first_contract";
        public const string SourceGreedTrial = "greed_trial";
        public const string SourceAppliedAdjustment = "applied_adjustment";
        public const string SourceAnalysis = "activity_analysis";
        public const string SourceGuidedPath = "guided_path";
        public const string SourceSummary = "summary";

        public const string TitleKey = "ui.mvp_primary_next_action.title";
        public const string ActionFormatKey = "ui.mvp_primary_next_action.action_format";
        public const string SourceFormatKey = "ui.mvp_primary_next_action.source_format";
        public const string SourceFirstContractKey = "ui.mvp_primary_next_action.source.first_contract";
        public const string SourceGreedTrialKey = "ui.mvp_primary_next_action.source.greed_trial";
        public const string SourceAppliedAdjustmentKey = "ui.mvp_primary_next_action.source.applied_adjustment";
        public const string SourceAnalysisKey = "ui.mvp_primary_next_action.source.activity_analysis";
        public const string SourceGuidedPathKey = "ui.mvp_primary_next_action.source.guided_path";
        public const string SourceSummaryKey = "ui.mvp_primary_next_action.source.summary";
        public const string FirstContractIncompleteActionKey = "ui.mvp_primary_next_action.first_contract.incomplete";

        public static MvpPrimaryNextActionSummary Resolve(
            MvpPlayerLoopSummary loopSummary,
            GuidedMvpActionPathSummary guidedPath,
            MvpFirstSessionObjectiveSummary firstContract,
            MvpPostContractGreedTrialSummary greedTrial)
        {
            if (firstContract == null || !firstContract.RuleResolved || !firstContract.IsComplete)
            {
                return Create(RuleFirstContractIncomplete, FirstContractIncompleteActionKey, SourceFirstContract, SourceFirstContractKey);
            }

            if (greedTrial != null && greedTrial.IsActive && !greedTrial.IsComplete)
            {
                string actionKey = string.IsNullOrWhiteSpace(greedTrial.NextActionKey)
                    ? MvpPostContractGreedTrialPresenter.NextActionTestGreedierSetupKey
                    : greedTrial.NextActionKey;
                return Create(RuleGreedTrialIncomplete, actionKey, SourceGreedTrial, SourceGreedTrialKey);
            }

            BasicRunAnalysisAppliedAdjustmentResult applied = BasicRunAnalysisAppliedAdjustmentPresenter.Resolve(loopSummary);
            if (applied != null && applied.Applied && !string.IsNullOrWhiteSpace(applied.NextActionKey))
            {
                return Create(RuleAppliedAdjustment, applied.NextActionKey, SourceAppliedAdjustment, SourceAppliedAdjustmentKey, applied.AdjustmentKey);
            }

            if (loopSummary != null && loopSummary.AnalysisUnlocked && !string.IsNullOrWhiteSpace(loopSummary.AnalysisAdviceKey))
            {
                return Create(RuleAnalysisRecommendation, loopSummary.AnalysisAdviceKey, SourceAnalysis, SourceAnalysisKey);
            }

            if (guidedPath != null && !string.IsNullOrWhiteSpace(guidedPath.NextActionKey))
            {
                return Create(RuleGuidedOrSummaryFallback, guidedPath.NextActionKey, SourceGuidedPath, SourceGuidedPathKey);
            }

            return Create(RuleGuidedOrSummaryFallback, loopSummary?.NextOptimizationSuggestionKey, SourceSummary, SourceSummaryKey);
        }

        public static string BuildPanelText(MvpPrimaryNextActionSummary summary, Func<string, string, string> localize)
        {
            if (summary == null) summary = Resolve(null, null, null, null);
            string action = Localize(localize, string.IsNullOrWhiteSpace(summary.PrimaryActionKey) ? MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey : summary.PrimaryActionKey);
            string source = Localize(localize, string.IsNullOrWhiteSpace(summary.PrimaryActionSourceLabelKey) ? SourceSummaryKey : summary.PrimaryActionSourceLabelKey);
            return Localize(localize, TitleKey) + "\n" +
                   string.Format(Localize(localize, ActionFormatKey), action) + "\n" +
                   string.Format(Localize(localize, SourceFormatKey), source);
        }

        private static MvpPrimaryNextActionSummary Create(string rule, string actionKey, string source, string sourceLabelKey, string detailKey = null)
        {
            return new MvpPrimaryNextActionSummary
            {
                RuleResolved = true,
                ResolvedRule = rule,
                PrimaryActionKey = string.IsNullOrWhiteSpace(actionKey) ? MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey : actionKey,
                PrimaryActionSource = source,
                PrimaryActionSourceLabelKey = sourceLabelKey,
                SupportingDetailKey = detailKey
            };
        }

        private static string Localize(Func<string, string, string> localize, string key) => localize == null ? key : localize(key, key);
    }

    [Serializable]
    public sealed class MvpPrimaryNextActionSummary
    {
        public bool RuleResolved;
        public string ResolvedRule;
        public string PrimaryActionKey;
        public string PrimaryActionSource;
        public string PrimaryActionSourceLabelKey;
        public string SupportingDetailKey;
    }
}
