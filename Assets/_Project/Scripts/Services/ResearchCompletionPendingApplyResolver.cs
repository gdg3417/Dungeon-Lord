using System;

namespace DungeonBuilder.M0
{
    public static class ResearchCompletionPendingApplyResolver
    {
        public static ResearchCompletionPendingApplySummary Resolve(
            ResearchPendingState pendingState,
            ResearchProgressState progressState,
            ResearchCompletionEligibilityScaffoldConfig config)
        {
            if (pendingState == null)
            {
                return Error(ResearchCompletionPendingApplySummaryErrorCode.NoPendingResearch);
            }

            if (string.IsNullOrWhiteSpace(pendingState.SlotId) || string.IsNullOrWhiteSpace(pendingState.ProjectId))
            {
                return Error(ResearchCompletionPendingApplySummaryErrorCode.InvalidPendingState, pendingState);
            }

            if (IsMissingProgressState(progressState))
            {
                return Error(ResearchCompletionPendingApplySummaryErrorCode.MissingProgressState, pendingState);
            }

            if (!string.Equals(progressState.SlotId, pendingState.SlotId, StringComparison.Ordinal))
            {
                return Error(ResearchCompletionPendingApplySummaryErrorCode.ProgressStateSlotMismatch, pendingState, progressState);
            }

            if (!string.Equals(progressState.ProjectId, pendingState.ProjectId, StringComparison.Ordinal))
            {
                return Error(ResearchCompletionPendingApplySummaryErrorCode.ProgressStateProjectMismatch, pendingState, progressState);
            }

            if (double.IsNaN(progressState.ProgressUnits) ||
                double.IsInfinity(progressState.ProgressUnits) ||
                progressState.ProgressUnits < 0d)
            {
                return Error(ResearchCompletionPendingApplySummaryErrorCode.InvalidProgressUnits, pendingState, progressState);
            }

            if (config == null)
            {
                return Error(ResearchCompletionPendingApplySummaryErrorCode.MissingConfig, pendingState, progressState);
            }

            if (!config.enabled)
            {
                return Error(ResearchCompletionPendingApplySummaryErrorCode.DisabledConfig, pendingState, progressState);
            }

            if (string.IsNullOrWhiteSpace(config.ruleSourceId) || string.IsNullOrWhiteSpace(config.projectId))
            {
                return Error(ResearchCompletionPendingApplySummaryErrorCode.InvalidConfig, pendingState, progressState);
            }

            if (double.IsNaN(config.requiredProgressUnits) ||
                double.IsInfinity(config.requiredProgressUnits) ||
                config.requiredProgressUnits <= 0d)
            {
                return Error(ResearchCompletionPendingApplySummaryErrorCode.InvalidRequiredProgressUnits, pendingState, progressState, config.ruleSourceId);
            }

            if (!string.Equals(config.projectId, pendingState.ProjectId, StringComparison.Ordinal))
            {
                return Error(ResearchCompletionPendingApplySummaryErrorCode.ConfigProjectMismatch, pendingState, progressState, config.ruleSourceId);
            }

            bool eligibleForCompletion = progressState.ProgressUnits >= config.requiredProgressUnits;
            return new ResearchCompletionPendingApplySummary
            {
                RuleResolved = true,
                Pending = true,
                HasProgressState = true,
                SlotId = pendingState.SlotId,
                ProjectId = pendingState.ProjectId,
                ProgressUnits = progressState.ProgressUnits,
                RequiredProgressUnits = config.requiredProgressUnits,
                EligibleForCompletion = eligibleForCompletion,
                AlreadyCompletionPending = progressState.CompletionPending,
                WouldSetCompletionPending = eligibleForCompletion && !progressState.CompletionPending,
                WouldCompleteResearch = false,
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

        private static ResearchCompletionPendingApplySummary Error(
            ResearchCompletionPendingApplySummaryErrorCode errorCode,
            ResearchPendingState pendingState = null,
            ResearchProgressState progressState = null,
            string ruleSourceId = "")
        {
            bool hasProgressState = progressState != null && !IsMissingProgressState(progressState);
            return new ResearchCompletionPendingApplySummary
            {
                DeterministicErrorCode = (int)errorCode,
                Pending = pendingState != null,
                HasProgressState = hasProgressState,
                SlotId = hasProgressState ? progressState.SlotId ?? string.Empty : pendingState != null ? pendingState.SlotId ?? string.Empty : string.Empty,
                ProjectId = hasProgressState ? progressState.ProjectId ?? string.Empty : pendingState != null ? pendingState.ProjectId ?? string.Empty : string.Empty,
                ProgressUnits = hasProgressState ? progressState.ProgressUnits : 0d,
                AlreadyCompletionPending = hasProgressState && progressState.CompletionPending,
                WouldSetCompletionPending = false,
                WouldCompleteResearch = false,
                RuleSourceIdUsed = ruleSourceId ?? string.Empty
            };
        }
    }
}
