using System;

namespace DungeonBuilder.M0
{
    public static class ResearchCompletionClaimReadinessResolver
    {
        public static ResearchCompletionClaimReadinessSummary Resolve(
            ResearchPendingState pendingState,
            ResearchProgressState progressState,
            ResearchCompletionEligibilityScaffoldConfig config)
        {
            if (pendingState == null)
            {
                return Error(ResearchCompletionClaimReadinessSummaryErrorCode.NoPendingResearch);
            }

            if (string.IsNullOrWhiteSpace(pendingState.SlotId) || string.IsNullOrWhiteSpace(pendingState.ProjectId))
            {
                return Error(ResearchCompletionClaimReadinessSummaryErrorCode.InvalidPendingState, pendingState);
            }

            if (IsMissingProgressState(progressState))
            {
                return Error(ResearchCompletionClaimReadinessSummaryErrorCode.MissingProgressState, pendingState);
            }

            if (!string.Equals(progressState.SlotId, pendingState.SlotId, StringComparison.Ordinal))
            {
                return Error(ResearchCompletionClaimReadinessSummaryErrorCode.ProgressStateSlotMismatch, pendingState, progressState);
            }

            if (!string.Equals(progressState.ProjectId, pendingState.ProjectId, StringComparison.Ordinal))
            {
                return Error(ResearchCompletionClaimReadinessSummaryErrorCode.ProgressStateProjectMismatch, pendingState, progressState);
            }

            if (double.IsNaN(progressState.ProgressUnits) ||
                double.IsInfinity(progressState.ProgressUnits) ||
                progressState.ProgressUnits < 0d)
            {
                return Error(ResearchCompletionClaimReadinessSummaryErrorCode.InvalidProgressUnits, pendingState, progressState);
            }

            if (config == null)
            {
                return Error(ResearchCompletionClaimReadinessSummaryErrorCode.MissingConfig, pendingState, progressState);
            }

            if (!config.enabled)
            {
                return Error(ResearchCompletionClaimReadinessSummaryErrorCode.DisabledConfig, pendingState, progressState);
            }

            if (string.IsNullOrWhiteSpace(config.ruleSourceId) || string.IsNullOrWhiteSpace(config.projectId))
            {
                return Error(ResearchCompletionClaimReadinessSummaryErrorCode.InvalidConfig, pendingState, progressState);
            }

            if (double.IsNaN(config.requiredProgressUnits) ||
                double.IsInfinity(config.requiredProgressUnits) ||
                config.requiredProgressUnits <= 0d)
            {
                return Error(ResearchCompletionClaimReadinessSummaryErrorCode.InvalidRequiredProgressUnits, pendingState, progressState, config.ruleSourceId);
            }

            if (!string.Equals(config.projectId, pendingState.ProjectId, StringComparison.Ordinal))
            {
                return Error(ResearchCompletionClaimReadinessSummaryErrorCode.ConfigProjectMismatch, pendingState, progressState, config.ruleSourceId);
            }

            bool eligibleForCompletion = progressState.ProgressUnits >= config.requiredProgressUnits;
            return new ResearchCompletionClaimReadinessSummary
            {
                RuleResolved = true,
                Pending = true,
                HasProgressState = true,
                SlotId = pendingState.SlotId,
                ProjectId = pendingState.ProjectId,
                ProgressUnits = progressState.ProgressUnits,
                RequiredProgressUnits = config.requiredProgressUnits,
                CompletionPending = progressState.CompletionPending,
                EligibleForCompletion = eligibleForCompletion,
                ReadyForClaim = eligibleForCompletion && progressState.CompletionPending,
                RuleSourceIdUsed = config.ruleSourceId
            };
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

        private static ResearchCompletionClaimReadinessSummary Error(
            ResearchCompletionClaimReadinessSummaryErrorCode errorCode,
            ResearchPendingState pendingState = null,
            ResearchProgressState progressState = null,
            string ruleSourceId = "")
        {
            bool hasProgressState = progressState != null && !IsMissingProgressState(progressState);
            return new ResearchCompletionClaimReadinessSummary
            {
                DeterministicErrorCode = (int)errorCode,
                Pending = pendingState != null,
                HasProgressState = hasProgressState,
                SlotId = hasProgressState ? progressState.SlotId ?? string.Empty : pendingState != null ? pendingState.SlotId ?? string.Empty : string.Empty,
                ProjectId = hasProgressState ? progressState.ProjectId ?? string.Empty : pendingState != null ? pendingState.ProjectId ?? string.Empty : string.Empty,
                ProgressUnits = hasProgressState ? progressState.ProgressUnits : 0d,
                CompletionPending = hasProgressState && progressState.CompletionPending,
                RuleSourceIdUsed = ruleSourceId ?? string.Empty
            };
        }
    }
}
