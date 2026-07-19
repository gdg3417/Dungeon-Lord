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

        public const string CompactLineFormatKey = "ui.mvp_primary_next_action.compact_format";
        public const string SourceFirstContractKey = "ui.mvp_primary_next_action.source.first_contract";
        public const string SourceGreedTrialKey = "ui.mvp_primary_next_action.source.greed_trial";
        public const string SourceAppliedAdjustmentKey = "ui.mvp_primary_next_action.source.applied_adjustment";
        public const string SourceAnalysisKey = "ui.mvp_primary_next_action.source.activity_analysis";
        public const string SourceGuidedPathKey = "ui.mvp_primary_next_action.source.guided_path";
        public const string SourceSummaryKey = "ui.mvp_primary_next_action.source.summary";
        public const string FirstContractIncompleteActionKey = "ui.mvp_primary_next_action.first_contract.incomplete";
        public const string StartResearchActionKey = "ui.mvp_primary_next_action.research.start";
        public const string ContinueResearchActionKey = "ui.mvp_primary_next_action.research.continue";
        public const string ClaimResearchActionKey = "ui.mvp_primary_next_action.research.claim";
        public const string BlockedResearchActionKey = "ui.mvp_primary_next_action.research.blocked";

        public static MvpPrimaryNextActionSummary Resolve(
            MvpPlayerLoopSummary loopSummary,
            GuidedMvpActionPathSummary guidedPath,
            MvpFirstSessionObjectiveSummary firstContract,
            MvpPostContractGreedTrialSummary greedTrial)
        {
            if (firstContract == null || !firstContract.RuleResolved || !firstContract.IsComplete)
            {
                PlayerResearchAuthoritySummary research = loopSummary?.PlayerResearchAuthority;
                if (firstContract != null && firstContract.RuleResolved && firstContract.RunObservedComplete && !firstContract.AnalysisComplete && research != null)
                {
                    if (research.State == PlayerResearchAuthorityState.Available && research.CanStart)
                    {
                        return Create(RuleFirstContractIncomplete, StartResearchActionKey, SourceFirstContract, SourceFirstContractKey);
                    }
                    if (research.State == PlayerResearchAuthorityState.InProgress)
                    {
                        return Create(RuleFirstContractIncomplete, ContinueResearchActionKey, SourceFirstContract, SourceFirstContractKey);
                    }
                    if (research.State == PlayerResearchAuthorityState.ReadyForLocalMvpClaim && research.CanClaimLocalMvp)
                    {
                        return Create(RuleFirstContractIncomplete, ClaimResearchActionKey, SourceFirstContract, SourceFirstContractKey);
                    }
                    if (research.State == PlayerResearchAuthorityState.Blocked || research.State == PlayerResearchAuthorityState.ClaimBlocked)
                    {
                        string blockedActionKey = string.IsNullOrWhiteSpace(research.FeedbackLocalizationKey)
                            ? BlockedResearchActionKey
                            : research.FeedbackLocalizationKey;
                        return Create(RuleFirstContractIncomplete, blockedActionKey, SourceFirstContract, SourceFirstContractKey, research.FeedbackLocalizationKey);
                    }
                }
                return Create(RuleFirstContractIncomplete, FirstContractIncompleteActionKey, SourceFirstContract, SourceFirstContractKey);
            }

            BasicRunAnalysisAppliedAdjustmentResult applied = BasicRunAnalysisAppliedAdjustmentPresenter.Resolve(loopSummary);
            if (applied != null && applied.Applied && !string.IsNullOrWhiteSpace(applied.NextActionKey))
            {
                return Create(RuleAppliedAdjustment, applied.NextActionKey, SourceAppliedAdjustment, SourceAppliedAdjustmentKey, applied.AdjustmentKey);
            }

            if (greedTrial != null && greedTrial.IsActive && !greedTrial.IsComplete)
            {
                string actionKey = string.IsNullOrWhiteSpace(greedTrial.NextActionKey)
                    ? MvpPostContractGreedTrialPresenter.NextActionTestGreedierSetupKey
                    : greedTrial.NextActionKey;
                return Create(RuleGreedTrialIncomplete, actionKey, SourceGreedTrial, SourceGreedTrialKey);
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
            string actionKey = string.IsNullOrWhiteSpace(summary.PrimaryActionKey) ? MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey : summary.PrimaryActionKey;
            string action = NormalizePrimaryActionText(actionKey, Localize(localize, actionKey));
            string source = Localize(localize, string.IsNullOrWhiteSpace(summary.PrimaryActionSourceLabelKey) ? SourceSummaryKey : summary.PrimaryActionSourceLabelKey);
            return string.Format(Localize(localize, CompactLineFormatKey), action, source);
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

        private static string NormalizePrimaryActionText(string actionKey, string action)
        {
            if (string.IsNullOrWhiteSpace(action) || !PrimaryActionBodyMayHaveLegacyNextPrefix(actionKey))
            {
                return action;
            }

            const string nextPrefix = "Next:";
            return action.StartsWith(nextPrefix, StringComparison.Ordinal)
                ? action.Substring(nextPrefix.Length).TrimStart()
                : action;
        }

        private static bool PrimaryActionBodyMayHaveLegacyNextPrefix(string actionKey)
        {
            return string.Equals(actionKey, BasicRunAnalysisRecommendationPresenter.RunForAnalysisKey, StringComparison.Ordinal) ||
                   string.Equals(actionKey, BasicRunAnalysisRecommendationPresenter.ReduceDangerKey, StringComparison.Ordinal) ||
                   string.Equals(actionKey, BasicRunAnalysisRecommendationPresenter.ReduceHeatKey, StringComparison.Ordinal) ||
                   string.Equals(actionKey, BasicRunAnalysisRecommendationPresenter.ImproveExtractionKey, StringComparison.Ordinal) ||
                   string.Equals(actionKey, BasicRunAnalysisRecommendationPresenter.TestGreedierKey, StringComparison.Ordinal) ||
                   string.Equals(actionKey, BasicRunAnalysisRecommendationPresenter.AdjustAndRunAgainKey, StringComparison.Ordinal) ||
                   string.Equals(actionKey, BasicRunAnalysisAppliedAdjustmentPresenter.RunAgainToTestChangeKey, StringComparison.Ordinal) ||
                   string.Equals(actionKey, MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey, StringComparison.Ordinal) ||
                   string.Equals(actionKey, MvpPlayerLoopSummaryPresenter.SuggestReduceHeatPressureKey, StringComparison.Ordinal) ||
                   string.Equals(actionKey, MvpPlayerLoopSummaryPresenter.SuggestImproveSurvivabilityOrLayoutKey, StringComparison.Ordinal) ||
                   string.Equals(actionKey, MvpPlayerLoopSummaryPresenter.SuggestVerifyResearchStatusKey, StringComparison.Ordinal) ||
                   string.Equals(actionKey, MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey, StringComparison.Ordinal) ||
                   string.Equals(actionKey, GuidedMvpActionPathPresenter.ActionRunAgainToTestChangeKey, StringComparison.Ordinal);
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
