using System;

namespace DungeonBuilder.M0
{
    public static class ResearchStatusPresenter
    {
        private const string NoResearchStatusKey = "ui.research.status.no_research";
        private const string ActiveInProgressStatusKey = "ui.research.status.active_in_progress";
        private const string ActiveCompletionPendingStatusKey = "ui.research.status.active_completion_pending";
        private const string VerificationRequiredStatusKey = "ui.research.status.verification_required";
        private const string CompletedStatusKey = "ui.research.status.completed";
        private const string BlockedOrInvalidStatusKey = "ui.research.status.blocked_or_invalid";

        public static ResearchStatusPresentation Present(
            ResearchPendingState pendingState,
            ResearchProgressState progressState,
            CompletedResearchState completedState,
            ResearchCompletionEligibilityScaffoldConfig config)
        {
            CompletedResearchStateSummary completed = CompletedResearchStateResolver.Resolve(completedState);
            if (!IsValidConfig(config))
            {
                return Blocked(completed.HasCompletedState);
            }

            if (pendingState == null)
            {
                if (HasProgressState(progressState))
                {
                    return Blocked(completed.HasCompletedState, config.ruleSourceId);
                }

                return HasCompletedProject(completedState, config.projectId)
                    ? Create(ResearchStatusPresentationState.Completed, false, false, completed.HasCompletedState,
                        string.Empty, config.projectId, 0d, config.requiredProgressUnits, false, false,
                        CompletedStatusKey, config.ruleSourceId)
                    : Create(ResearchStatusPresentationState.NoResearch, false, false, completed.HasCompletedState,
                        string.Empty, string.Empty, 0d, config.requiredProgressUnits, false, false,
                        NoResearchStatusKey, config.ruleSourceId);
            }

            ResearchCompletionEligibilitySummary eligibility = ResearchCompletionEligibilityResolver.Resolve(
                pendingState,
                progressState,
                config);
            if (!eligibility.RuleResolved)
            {
                return Blocked(completed.HasCompletedState, eligibility.RuleSourceIdUsed);
            }

            if (progressState.CompletionPending)
            {
                return Create(ResearchStatusPresentationState.VerificationRequired, true, true, completed.HasCompletedState,
                    eligibility.SlotId, eligibility.ProjectId, eligibility.ProgressUnits, eligibility.RequiredProgressUnits,
                    true, eligibility.EligibleForCompletion, VerificationRequiredStatusKey, eligibility.RuleSourceIdUsed);
            }

            ResearchStatusPresentationState state = eligibility.EligibleForCompletion
                ? ResearchStatusPresentationState.ActiveCompletionPending
                : ResearchStatusPresentationState.ActiveInProgress;
            string statusKey = eligibility.EligibleForCompletion
                ? ActiveCompletionPendingStatusKey
                : ActiveInProgressStatusKey;
            return Create(state, true, true, completed.HasCompletedState,
                eligibility.SlotId, eligibility.ProjectId, eligibility.ProgressUnits, eligibility.RequiredProgressUnits,
                false, eligibility.EligibleForCompletion, statusKey, eligibility.RuleSourceIdUsed);
        }

        private static ResearchStatusPresentation Create(
            ResearchStatusPresentationState state,
            bool pending,
            bool hasProgressState,
            bool hasCompletedState,
            string slotId,
            string projectId,
            double progressUnits,
            double requiredProgressUnits,
            bool completionPending,
            bool eligibleForCompletion,
            string statusLocalizationKey,
            string ruleSourceIdUsed)
        {
            return new ResearchStatusPresentation
            {
                State = state,
                Pending = pending,
                HasProgressState = hasProgressState,
                HasCompletedState = hasCompletedState,
                SlotId = slotId ?? string.Empty,
                ProjectId = projectId ?? string.Empty,
                ProgressUnits = progressUnits,
                RequiredProgressUnits = requiredProgressUnits,
                CompletionPending = completionPending,
                EligibleForCompletion = eligibleForCompletion,
                VerificationRequired = state == ResearchStatusPresentationState.VerificationRequired,
                ReadyToClaim = false,
                Completed = state == ResearchStatusPresentationState.Completed,
                BlockedOrInvalid = state == ResearchStatusPresentationState.BlockedOrInvalid,
                CanClaimProduction = false,
                WouldGrantRewards = false,
                WouldUnlockContent = false,
                WouldChargeCosts = false,
                WouldProcessOfflineProgress = false,
                StatusLocalizationKey = statusLocalizationKey,
                RuleSourceIdUsed = ruleSourceIdUsed ?? string.Empty
            };
        }

        private static ResearchStatusPresentation Blocked(bool hasCompletedState, string ruleSourceIdUsed = "")
        {
            return Create(ResearchStatusPresentationState.BlockedOrInvalid, false, false, hasCompletedState,
                string.Empty, string.Empty, 0d, 0d, false, false, BlockedOrInvalidStatusKey, ruleSourceIdUsed);
        }

        private static bool IsValidConfig(ResearchCompletionEligibilityScaffoldConfig config)
        {
            return config != null &&
                   config.enabled &&
                   !string.IsNullOrWhiteSpace(config.ruleSourceId) &&
                   !string.IsNullOrWhiteSpace(config.projectId) &&
                   !double.IsNaN(config.requiredProgressUnits) &&
                   !double.IsInfinity(config.requiredProgressUnits) &&
                   config.requiredProgressUnits > 0d;
        }

        private static bool HasProgressState(ResearchProgressState progressState)
        {
            return progressState != null &&
                   (!string.IsNullOrWhiteSpace(progressState.SlotId) ||
                    !string.IsNullOrWhiteSpace(progressState.ProjectId) ||
                    progressState.ProgressUnits != 0d ||
                    progressState.CompletionPending ||
                    !string.IsNullOrWhiteSpace(progressState.RuleSourceIdUsed));
        }

        private static bool HasCompletedProject(CompletedResearchState completedState, string projectId)
        {
            if (completedState?.ProjectIds == null)
            {
                return false;
            }

            for (int i = 0; i < completedState.ProjectIds.Length; i++)
            {
                if (string.Equals(completedState.ProjectIds[i], projectId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
