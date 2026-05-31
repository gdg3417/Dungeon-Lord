using System;

namespace DungeonBuilder.M0
{
    public static class ResearchProgressStateResolver
    {
        public static ResearchProgressStateSummary Resolve(
            ResearchPendingState pendingState,
            ResearchProgressState progressState)
        {
            if (pendingState == null || string.IsNullOrWhiteSpace(pendingState.ProjectId))
            {
                return Error(ResearchProgressStateSummaryErrorCode.NoPendingResearch);
            }

            if (string.IsNullOrWhiteSpace(pendingState.SlotId))
            {
                return Error(ResearchProgressStateSummaryErrorCode.InvalidPendingState, pendingState);
            }

            if (progressState == null)
            {
                return Error(ResearchProgressStateSummaryErrorCode.MissingProgressState, pendingState);
            }

            if (!string.Equals(progressState.SlotId, pendingState.SlotId, StringComparison.Ordinal))
            {
                return Error(ResearchProgressStateSummaryErrorCode.ProgressStateSlotMismatch, pendingState, progressState);
            }

            if (!string.Equals(progressState.ProjectId, pendingState.ProjectId, StringComparison.Ordinal))
            {
                return Error(ResearchProgressStateSummaryErrorCode.ProgressStateProjectMismatch, pendingState, progressState);
            }

            if (double.IsNaN(progressState.ProgressUnits) ||
                double.IsInfinity(progressState.ProgressUnits) ||
                progressState.ProgressUnits < 0d)
            {
                return Error(ResearchProgressStateSummaryErrorCode.InvalidProgressUnits, pendingState, progressState);
            }

            if (progressState.CompletionPending)
            {
                return Error(ResearchProgressStateSummaryErrorCode.CompletionPendingNotActive, pendingState, progressState);
            }

            return Summary(pendingState, progressState, ruleResolved: true, ResearchProgressStateSummaryErrorCode.None);
        }

        private static ResearchProgressStateSummary Error(
            ResearchProgressStateSummaryErrorCode errorCode,
            ResearchPendingState pendingState = null,
            ResearchProgressState progressState = null)
        {
            return Summary(pendingState, progressState, ruleResolved: false, errorCode);
        }

        private static ResearchProgressStateSummary Summary(
            ResearchPendingState pendingState,
            ResearchProgressState progressState,
            bool ruleResolved,
            ResearchProgressStateSummaryErrorCode errorCode)
        {
            bool pending = pendingState != null && !string.IsNullOrWhiteSpace(pendingState.ProjectId);
            bool hasProgressState = progressState != null;
            return new ResearchProgressStateSummary
            {
                RuleResolved = ruleResolved,
                DeterministicErrorCode = (int)errorCode,
                Pending = pending,
                HasProgressState = hasProgressState,
                SlotId = hasProgressState ? progressState.SlotId ?? string.Empty : string.Empty,
                ProjectId = hasProgressState ? progressState.ProjectId ?? string.Empty : string.Empty,
                ProgressUnits = hasProgressState ? progressState.ProgressUnits : 0d,
                CompletionPending = hasProgressState && progressState.CompletionPending,
                StateMatchesPending = pending && hasProgressState &&
                    string.Equals(progressState.SlotId, pendingState.SlotId, StringComparison.Ordinal) &&
                    string.Equals(progressState.ProjectId, pendingState.ProjectId, StringComparison.Ordinal),
                RuleSourceIdUsed = hasProgressState ? progressState.RuleSourceIdUsed ?? string.Empty : string.Empty
            };
        }
    }
}
