using System;

namespace DungeonBuilder.M0
{
    public static class ResearchCompletionEligibilityResolver
    {
        public static ResearchCompletionEligibilitySummary Resolve(
            ResearchPendingState pendingState,
            ResearchProgressState progressState,
            ResearchCompletionEligibilityScaffoldConfig config)
        {
            if (pendingState == null)
            {
                return Error(ResearchCompletionEligibilitySummaryErrorCode.NoPendingResearch);
            }

            if (string.IsNullOrWhiteSpace(pendingState.SlotId) || string.IsNullOrWhiteSpace(pendingState.ProjectId))
            {
                return Error(ResearchCompletionEligibilitySummaryErrorCode.InvalidPendingState, pendingState);
            }

            if (IsMissingProgressState(progressState))
            {
                return Error(ResearchCompletionEligibilitySummaryErrorCode.MissingProgressState, pendingState);
            }

            if (!string.Equals(progressState.SlotId, pendingState.SlotId, StringComparison.Ordinal))
            {
                return Error(ResearchCompletionEligibilitySummaryErrorCode.ProgressStateSlotMismatch, pendingState, progressState);
            }

            if (!string.Equals(progressState.ProjectId, pendingState.ProjectId, StringComparison.Ordinal))
            {
                return Error(ResearchCompletionEligibilitySummaryErrorCode.ProgressStateProjectMismatch, pendingState, progressState);
            }

            if (double.IsNaN(progressState.ProgressUnits) ||
                double.IsInfinity(progressState.ProgressUnits) ||
                progressState.ProgressUnits < 0d)
            {
                return Error(ResearchCompletionEligibilitySummaryErrorCode.InvalidProgressUnits, pendingState, progressState);
            }

            if (config == null)
            {
                return Error(ResearchCompletionEligibilitySummaryErrorCode.MissingConfig, pendingState, progressState);
            }

            if (!config.enabled)
            {
                return Error(ResearchCompletionEligibilitySummaryErrorCode.DisabledConfig, pendingState, progressState);
            }

            if (string.IsNullOrWhiteSpace(config.ruleSourceId) || string.IsNullOrWhiteSpace(config.projectId))
            {
                return Error(ResearchCompletionEligibilitySummaryErrorCode.InvalidConfig, pendingState, progressState);
            }

            if (double.IsNaN(config.requiredProgressUnits) ||
                double.IsInfinity(config.requiredProgressUnits) ||
                config.requiredProgressUnits <= 0d)
            {
                return Error(ResearchCompletionEligibilitySummaryErrorCode.InvalidRequiredProgressUnits, pendingState, progressState, config.ruleSourceId);
            }

            if (!string.Equals(config.projectId, pendingState.ProjectId, StringComparison.Ordinal))
            {
                return Error(ResearchCompletionEligibilitySummaryErrorCode.ConfigProjectMismatch, pendingState, progressState, config.ruleSourceId);
            }

            double remainingProgressUnits = Math.Max(0d, config.requiredProgressUnits - progressState.ProgressUnits);
            return new ResearchCompletionEligibilitySummary
            {
                RuleResolved = true,
                Pending = true,
                HasProgressState = true,
                SlotId = pendingState.SlotId,
                ProjectId = pendingState.ProjectId,
                ProgressUnits = progressState.ProgressUnits,
                RequiredProgressUnits = config.requiredProgressUnits,
                RemainingProgressUnits = remainingProgressUnits,
                EligibleForCompletion = progressState.ProgressUnits >= config.requiredProgressUnits,
                WouldSetCompletionPending = false,
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

        private static ResearchCompletionEligibilitySummary Error(
            ResearchCompletionEligibilitySummaryErrorCode errorCode,
            ResearchPendingState pendingState = null,
            ResearchProgressState progressState = null,
            string ruleSourceId = "")
        {
            bool hasProgressState = progressState != null && !IsMissingProgressState(progressState);
            return new ResearchCompletionEligibilitySummary
            {
                DeterministicErrorCode = (int)errorCode,
                Pending = pendingState != null,
                HasProgressState = hasProgressState,
                SlotId = hasProgressState ? progressState.SlotId ?? string.Empty : pendingState != null ? pendingState.SlotId ?? string.Empty : string.Empty,
                ProjectId = hasProgressState ? progressState.ProjectId ?? string.Empty : pendingState != null ? pendingState.ProjectId ?? string.Empty : string.Empty,
                ProgressUnits = hasProgressState ? progressState.ProgressUnits : 0d,
                WouldSetCompletionPending = false,
                WouldCompleteResearch = false,
                RuleSourceIdUsed = ruleSourceId ?? string.Empty
            };
        }
    }
}
