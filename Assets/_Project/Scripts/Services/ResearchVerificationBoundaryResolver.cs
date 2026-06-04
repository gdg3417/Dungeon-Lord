using System;

namespace DungeonBuilder.M0
{
    public static class ResearchVerificationBoundaryResolver
    {
        public const string DisabledVerificationMode = "disabled";
        public const string UnavailableVerificationMode = "unavailable";
        public const string LocalDevPlaceholderVerificationMode = "localDevPlaceholder";

        public static ResearchVerificationBoundarySummary Resolve(
            ResearchPendingState pendingState,
            ResearchProgressState progressState,
            CompletedResearchState completedState,
            ResearchCompletionEligibilityScaffoldConfig eligibilityConfig,
            ResearchVerificationScaffoldConfig verificationConfig)
        {
            CompletedResearchStateSummary completed = CompletedResearchStateResolver.Resolve(completedState, pendingState, progressState);
            if (pendingState == null)
            {
                return Error(ResearchVerificationBoundarySummaryErrorCode.NoPendingResearch, null, null, completed: completed);
            }
            if (string.IsNullOrWhiteSpace(pendingState.SlotId) || string.IsNullOrWhiteSpace(pendingState.ProjectId))
            {
                return Error(ResearchVerificationBoundarySummaryErrorCode.InvalidPendingState, pendingState, progressState, completed: completed);
            }
            if (IsMissingProgressState(progressState))
            {
                return Error(ResearchVerificationBoundarySummaryErrorCode.MissingProgressState, pendingState, progressState, completed: completed);
            }
            if (!string.Equals(pendingState.SlotId, progressState.SlotId, StringComparison.Ordinal))
            {
                return Error(ResearchVerificationBoundarySummaryErrorCode.ProgressStateSlotMismatch, pendingState, progressState, completed: completed);
            }
            if (!string.Equals(pendingState.ProjectId, progressState.ProjectId, StringComparison.Ordinal))
            {
                return Error(ResearchVerificationBoundarySummaryErrorCode.ProgressStateProjectMismatch, pendingState, progressState, completed: completed);
            }
            if (!IsValidProgress(progressState.ProgressUnits))
            {
                return Error(ResearchVerificationBoundarySummaryErrorCode.InvalidProgressUnits, pendingState, progressState, completed: completed);
            }
            if (eligibilityConfig == null)
            {
                return Error(ResearchVerificationBoundarySummaryErrorCode.MissingEligibilityConfig, pendingState, progressState, completed: completed);
            }
            if (!eligibilityConfig.enabled)
            {
                return Error(ResearchVerificationBoundarySummaryErrorCode.DisabledEligibilityConfig, pendingState, progressState, completed: completed);
            }
            if (string.IsNullOrWhiteSpace(eligibilityConfig.ruleSourceId) || string.IsNullOrWhiteSpace(eligibilityConfig.projectId))
            {
                return Error(ResearchVerificationBoundarySummaryErrorCode.InvalidEligibilityConfig, pendingState, progressState, completed: completed);
            }
            if (!IsValidRequirement(eligibilityConfig.requiredProgressUnits))
            {
                return Error(ResearchVerificationBoundarySummaryErrorCode.InvalidRequiredProgressUnits, pendingState, progressState, eligibilityConfig.requiredProgressUnits, completed: completed);
            }
            if (!string.Equals(eligibilityConfig.projectId, pendingState.ProjectId, StringComparison.Ordinal))
            {
                return Error(ResearchVerificationBoundarySummaryErrorCode.EligibilityConfigProjectMismatch, pendingState, progressState, eligibilityConfig.requiredProgressUnits, completed: completed);
            }
            bool eligible = progressState.ProgressUnits >= eligibilityConfig.requiredProgressUnits;
            if (completed.CurrentProjectAlreadyCompleted)
            {
                return Error(ResearchVerificationBoundarySummaryErrorCode.AlreadyCompleted, pendingState, progressState, eligibilityConfig.requiredProgressUnits, eligible, completed: completed);
            }
            if (verificationConfig == null)
            {
                return Error(ResearchVerificationBoundarySummaryErrorCode.MissingVerificationConfig, pendingState, progressState, eligibilityConfig.requiredProgressUnits, eligible, completed: completed);
            }
            string mode = verificationConfig.verificationMode ?? string.Empty;
            if (!verificationConfig.enabled || string.Equals(mode, DisabledVerificationMode, StringComparison.Ordinal))
            {
                return Error(ResearchVerificationBoundarySummaryErrorCode.DisabledVerificationConfig, pendingState, progressState, eligibilityConfig.requiredProgressUnits, eligible, mode, verificationConfig.ruleSourceId, completed);
            }
            if (string.IsNullOrWhiteSpace(verificationConfig.ruleSourceId) || string.IsNullOrWhiteSpace(mode))
            {
                return Error(ResearchVerificationBoundarySummaryErrorCode.InvalidVerificationConfig, pendingState, progressState, eligibilityConfig.requiredProgressUnits, eligible, mode, verificationConfig.ruleSourceId, completed);
            }
            bool verificationAvailable = string.Equals(mode, LocalDevPlaceholderVerificationMode, StringComparison.Ordinal);
            if (!verificationAvailable && !string.Equals(mode, UnavailableVerificationMode, StringComparison.Ordinal))
            {
                return Error(ResearchVerificationBoundarySummaryErrorCode.InvalidVerificationConfig, pendingState, progressState, eligibilityConfig.requiredProgressUnits, eligible, mode, verificationConfig.ruleSourceId, completed);
            }
            ResearchVerificationBoundarySummaryErrorCode code = string.Equals(mode, UnavailableVerificationMode, StringComparison.Ordinal)
                ? ResearchVerificationBoundarySummaryErrorCode.UnavailableVerificationMode
                : ResearchVerificationBoundarySummaryErrorCode.None;
            return CreateSummary(pendingState, progressState, completed, eligibilityConfig.requiredProgressUnits, eligible, mode, verificationConfig.ruleSourceId, verificationAvailable, code, code == ResearchVerificationBoundarySummaryErrorCode.None);
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

        private static ResearchVerificationBoundarySummary Error(
            ResearchVerificationBoundarySummaryErrorCode code,
            ResearchPendingState pendingState = null,
            ResearchProgressState progressState = null,
            double requiredProgressUnits = 0d,
            bool eligible = false,
            string verificationMode = "",
            string ruleSourceId = "",
            CompletedResearchStateSummary completed = null)
        {
            return CreateSummary(pendingState, progressState, completed, requiredProgressUnits, eligible, verificationMode, ruleSourceId, false, code, false);
        }

        private static ResearchVerificationBoundarySummary CreateSummary(
            ResearchPendingState pendingState,
            ResearchProgressState progressState,
            CompletedResearchStateSummary completed,
            double requiredProgressUnits,
            bool eligible,
            string verificationMode,
            string ruleSourceId,
            bool verificationAvailable,
            ResearchVerificationBoundarySummaryErrorCode code,
            bool ruleResolved)
        {
            bool hasProgress = progressState != null && !IsMissingProgressState(progressState);
            bool completionPending = hasProgress && progressState.CompletionPending;
            return new ResearchVerificationBoundarySummary
            {
                RuleResolved = ruleResolved,
                DeterministicErrorCode = (int)code,
                Pending = pendingState != null && !string.IsNullOrWhiteSpace(pendingState.ProjectId),
                HasProgressState = hasProgress,
                HasCompletedState = completed != null && completed.HasCompletedState,
                SlotId = hasProgress ? progressState.SlotId ?? string.Empty : pendingState?.SlotId ?? string.Empty,
                ProjectId = hasProgress ? progressState.ProjectId ?? string.Empty : pendingState?.ProjectId ?? string.Empty,
                ProgressUnits = hasProgress ? progressState.ProgressUnits : 0d,
                RequiredProgressUnits = requiredProgressUnits,
                CompletionPending = completionPending,
                EligibleForCompletion = eligible,
                AlreadyCompleted = completed != null && completed.CurrentProjectAlreadyCompleted,
                VerificationRequired = completionPending,
                VerificationAvailable = verificationAvailable,
                VerificationSatisfied = false,
                CanClaimProduction = false,
                WouldCallServer = false,
                WouldGrantRewards = false,
                WouldUnlockContent = false,
                WouldChargeCosts = false,
                WouldProcessOfflineProgress = false,
                VerificationModeUsed = verificationMode ?? string.Empty,
                RuleSourceIdUsed = ruleSourceId ?? string.Empty
            };
        }
    }
}
