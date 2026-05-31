using System;

namespace DungeonBuilder.M0
{
    public static class ResearchProgressApplyResolver
    {
        public static ResearchProgressApplySummary Resolve(
            ResearchPendingState pendingState,
            ResearchProgressState progressState,
            ResearchProgressScaffoldConfig config,
            long activeTickElapsedSeconds)
        {
            if (pendingState == null)
            {
                return Error(ResearchProgressApplySummaryErrorCode.NoPendingResearch);
            }

            if (string.IsNullOrWhiteSpace(pendingState.SlotId) || string.IsNullOrWhiteSpace(pendingState.ProjectId))
            {
                return Error(ResearchProgressApplySummaryErrorCode.InvalidPendingState, pendingState);
            }

            if (IsMissingProgressState(progressState))
            {
                return Error(ResearchProgressApplySummaryErrorCode.MissingProgressState, pendingState);
            }

            if (!string.Equals(progressState.SlotId, pendingState.SlotId, StringComparison.Ordinal))
            {
                return Error(ResearchProgressApplySummaryErrorCode.ProgressStateSlotMismatch, pendingState, progressState);
            }

            if (!string.Equals(progressState.ProjectId, pendingState.ProjectId, StringComparison.Ordinal))
            {
                return Error(ResearchProgressApplySummaryErrorCode.ProgressStateProjectMismatch, pendingState, progressState);
            }

            if (double.IsNaN(progressState.ProgressUnits) ||
                double.IsInfinity(progressState.ProgressUnits) ||
                progressState.ProgressUnits < 0d)
            {
                return Error(ResearchProgressApplySummaryErrorCode.InvalidProgressUnits, pendingState, progressState);
            }

            if (progressState.CompletionPending)
            {
                return Error(ResearchProgressApplySummaryErrorCode.CompletionPendingNotActive, pendingState, progressState);
            }

            if (config == null)
            {
                return Error(ResearchProgressApplySummaryErrorCode.MissingConfig, pendingState, progressState);
            }

            if (!config.enabled)
            {
                return Error(ResearchProgressApplySummaryErrorCode.DisabledConfig, pendingState, progressState);
            }

            if (string.IsNullOrWhiteSpace(config.ruleSourceId) ||
                config.maxActiveSessionElapsedSeconds < 0 ||
                double.IsNaN(config.progressPerActiveSecond) ||
                double.IsInfinity(config.progressPerActiveSecond) ||
                config.progressPerActiveSecond < 0d)
            {
                return Error(ResearchProgressApplySummaryErrorCode.InvalidConfig, pendingState, progressState);
            }

            if (activeTickElapsedSeconds < 0)
            {
                return Error(ResearchProgressApplySummaryErrorCode.InvalidElapsedTime, pendingState, progressState, config.ruleSourceId);
            }

            double progressDeltaApplied = activeTickElapsedSeconds * config.progressPerActiveSecond;
            double nextProgressUnits = progressState.ProgressUnits + progressDeltaApplied;
            if (double.IsNaN(progressDeltaApplied) ||
                double.IsInfinity(progressDeltaApplied) ||
                double.IsNaN(nextProgressUnits) ||
                double.IsInfinity(nextProgressUnits))
            {
                return Error(ResearchProgressApplySummaryErrorCode.InvalidProgressDelta, pendingState, progressState, config.ruleSourceId);
            }

            return new ResearchProgressApplySummary
            {
                RuleResolved = true,
                Pending = true,
                HasProgressState = true,
                SlotId = pendingState.SlotId,
                ProjectId = pendingState.ProjectId,
                ElapsedSecondsUsed = activeTickElapsedSeconds,
                ProgressDeltaApplied = progressDeltaApplied,
                PreviousProgressUnits = progressState.ProgressUnits,
                NextProgressUnits = nextProgressUnits,
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

        private static ResearchProgressApplySummary Error(
            ResearchProgressApplySummaryErrorCode errorCode,
            ResearchPendingState pendingState = null,
            ResearchProgressState progressState = null,
            string ruleSourceId = "")
        {
            bool hasProgressState = progressState != null;
            return new ResearchProgressApplySummary
            {
                DeterministicErrorCode = (int)errorCode,
                Pending = pendingState != null,
                HasProgressState = hasProgressState,
                SlotId = hasProgressState ? progressState.SlotId ?? string.Empty : pendingState != null ? pendingState.SlotId ?? string.Empty : string.Empty,
                ProjectId = hasProgressState ? progressState.ProjectId ?? string.Empty : pendingState != null ? pendingState.ProjectId ?? string.Empty : string.Empty,
                PreviousProgressUnits = hasProgressState ? progressState.ProgressUnits : 0d,
                NextProgressUnits = hasProgressState ? progressState.ProgressUnits : 0d,
                WouldCompleteResearch = false,
                RuleSourceIdUsed = ruleSourceId ?? string.Empty
            };
        }
    }
}
