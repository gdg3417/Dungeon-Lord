using System;

namespace DungeonBuilder.M0
{
    public static class ResearchCompletionClaimApplyResolver
    {
        public static ResearchCompletionClaimApplySummary Resolve(
            ResearchPendingState pendingState,
            ResearchProgressState progressState,
            CompletedResearchState completedState,
            ResearchCompletionEligibilityScaffoldConfig eligibilityConfig,
            ResearchCompletionClaimScaffoldConfig claimConfig)
        {
            CompletedResearchStateSummary completed = CompletedResearchStateResolver.Resolve(completedState, pendingState, progressState);
            if (pendingState == null)
            {
                return Error(ResearchCompletionClaimApplySummaryErrorCode.NoPendingResearch, completed: completed);
            }
            if (string.IsNullOrWhiteSpace(pendingState.SlotId) || string.IsNullOrWhiteSpace(pendingState.ProjectId))
            {
                return Error(ResearchCompletionClaimApplySummaryErrorCode.InvalidPendingState, pendingState, completed: completed);
            }
            if (IsMissingProgressState(progressState))
            {
                return Error(ResearchCompletionClaimApplySummaryErrorCode.MissingProgressState, pendingState, completed: completed);
            }
            if (!string.Equals(progressState.SlotId, pendingState.SlotId, StringComparison.Ordinal))
            {
                return Error(ResearchCompletionClaimApplySummaryErrorCode.ProgressStateSlotMismatch, pendingState, progressState, completed: completed);
            }
            if (!string.Equals(progressState.ProjectId, pendingState.ProjectId, StringComparison.Ordinal))
            {
                return Error(ResearchCompletionClaimApplySummaryErrorCode.ProgressStateProjectMismatch, pendingState, progressState, completed: completed);
            }
            if (!IsValidProgress(progressState.ProgressUnits))
            {
                return Error(ResearchCompletionClaimApplySummaryErrorCode.InvalidProgressUnits, pendingState, progressState, completed: completed);
            }
            if (eligibilityConfig == null)
            {
                return Error(ResearchCompletionClaimApplySummaryErrorCode.MissingEligibilityConfig, pendingState, progressState, completed: completed);
            }
            if (!eligibilityConfig.enabled)
            {
                return Error(ResearchCompletionClaimApplySummaryErrorCode.DisabledEligibilityConfig, pendingState, progressState, completed: completed);
            }
            if (string.IsNullOrWhiteSpace(eligibilityConfig.ruleSourceId) || string.IsNullOrWhiteSpace(eligibilityConfig.projectId))
            {
                return Error(ResearchCompletionClaimApplySummaryErrorCode.InvalidEligibilityConfig, pendingState, progressState, completed: completed);
            }
            if (!IsValidRequirement(eligibilityConfig.requiredProgressUnits))
            {
                return Error(ResearchCompletionClaimApplySummaryErrorCode.InvalidRequiredProgressUnits, pendingState, progressState, eligibilityConfig.requiredProgressUnits, completed: completed);
            }
            if (!string.Equals(eligibilityConfig.projectId, pendingState.ProjectId, StringComparison.Ordinal))
            {
                return Error(ResearchCompletionClaimApplySummaryErrorCode.EligibilityConfigProjectMismatch, pendingState, progressState, eligibilityConfig.requiredProgressUnits, completed: completed);
            }
            if (claimConfig == null)
            {
                return Error(ResearchCompletionClaimApplySummaryErrorCode.MissingClaimConfig, pendingState, progressState, eligibilityConfig.requiredProgressUnits, completed: completed);
            }
            if (!claimConfig.enabled)
            {
                return Error(ResearchCompletionClaimApplySummaryErrorCode.DisabledClaimConfig, pendingState, progressState, eligibilityConfig.requiredProgressUnits, completed: completed);
            }
            if (string.IsNullOrWhiteSpace(claimConfig.ruleSourceId))
            {
                return Error(ResearchCompletionClaimApplySummaryErrorCode.InvalidClaimConfig, pendingState, progressState, eligibilityConfig.requiredProgressUnits, completed: completed);
            }

            bool eligible = progressState.ProgressUnits >= eligibilityConfig.requiredProgressUnits;
            bool readyForClaim = eligible && progressState.CompletionPending;
            bool wouldApply = readyForClaim && !completed.CurrentProjectAlreadyCompleted;
            return CreateSummary(pendingState, progressState, completed, eligibilityConfig.requiredProgressUnits, eligible, claimConfig.ruleSourceId, wouldApply, ruleResolved: true);
        }

        private static bool IsMissingProgressState(ResearchProgressState progressState)
        {
            return progressState == null ||
                   (string.IsNullOrWhiteSpace(progressState.SlotId) &&
                    string.IsNullOrWhiteSpace(progressState.ProjectId) &&
                    progressState.ProgressUnits == 0d &&
                    !progressState.CompletionPending &&
                    string.IsNullOrWhiteSpace(progressState.RuleSourceIdUsed));
        }

        private static bool IsValidProgress(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
        private static bool IsValidRequirement(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0d;

        private static ResearchCompletionClaimApplySummary Error(
            ResearchCompletionClaimApplySummaryErrorCode code,
            ResearchPendingState pendingState = null,
            ResearchProgressState progressState = null,
            double requiredProgressUnits = 0d,
            bool eligible = false,
            string ruleSourceId = "",
            CompletedResearchStateSummary completed = null)
        {
            return CreateSummary(pendingState, progressState, completed, requiredProgressUnits, eligible, ruleSourceId, false, code);
        }

        private static ResearchCompletionClaimApplySummary CreateSummary(
            ResearchPendingState pendingState,
            ResearchProgressState progressState,
            CompletedResearchStateSummary completed,
            double requiredProgressUnits,
            bool eligible,
            string ruleSourceId,
            bool wouldApply,
            ResearchCompletionClaimApplySummaryErrorCode code = ResearchCompletionClaimApplySummaryErrorCode.None,
            bool ruleResolved = false)
        {
            bool hasProgress = progressState != null && !IsMissingProgressState(progressState);
            bool completionPending = hasProgress && progressState.CompletionPending;
            return new ResearchCompletionClaimApplySummary
            {
                RuleResolved = ruleResolved || wouldApply,
                DeterministicErrorCode = (int)code,
                Pending = pendingState != null,
                HasProgressState = hasProgress,
                HasCompletedState = completed != null && completed.HasCompletedState,
                SlotId = hasProgress ? progressState.SlotId ?? string.Empty : pendingState?.SlotId ?? string.Empty,
                ProjectId = hasProgress ? progressState.ProjectId ?? string.Empty : pendingState?.ProjectId ?? string.Empty,
                ProgressUnits = hasProgress ? progressState.ProgressUnits : 0d,
                RequiredProgressUnits = requiredProgressUnits,
                CompletionPending = completionPending,
                EligibleForCompletion = eligible,
                ReadyForClaim = eligible && completionPending,
                AlreadyCompleted = completed != null && completed.CurrentProjectAlreadyCompleted,
                WouldRecordCompletedResearch = wouldApply,
                WouldClearPending = wouldApply,
                WouldClearProgress = wouldApply,
                RuleSourceIdUsed = ruleSourceId ?? string.Empty
            };
        }
    }
}
