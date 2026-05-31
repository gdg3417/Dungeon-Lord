using System;

namespace DungeonBuilder.M0
{
    public static class ResearchProgressResolver
    {
        public static ResearchProgressSummary Resolve(
            ResearchPendingState pendingState,
            ResearchProgressScaffoldConfig config,
            long activeSessionElapsedSeconds)
        {
            if (pendingState == null)
            {
                return Error(ResearchProgressSummaryErrorCode.NoPendingResearch);
            }

            if (string.IsNullOrWhiteSpace(pendingState.SlotId) || string.IsNullOrWhiteSpace(pendingState.ProjectId))
            {
                return Error(ResearchProgressSummaryErrorCode.InvalidPendingState);
            }

            if (config == null)
            {
                return Error(ResearchProgressSummaryErrorCode.MissingConfig, pendingState);
            }

            if (!config.enabled)
            {
                return Error(ResearchProgressSummaryErrorCode.DisabledConfig, pendingState);
            }

            if (string.IsNullOrWhiteSpace(config.ruleSourceId) ||
                config.maxActiveSessionElapsedSeconds < 0 ||
                double.IsNaN(config.progressPerActiveSecond) ||
                double.IsInfinity(config.progressPerActiveSecond) ||
                config.progressPerActiveSecond < 0d)
            {
                return Error(ResearchProgressSummaryErrorCode.InvalidConfig, pendingState);
            }

            if (activeSessionElapsedSeconds < 0)
            {
                return Error(ResearchProgressSummaryErrorCode.InvalidElapsedTime, pendingState, config.ruleSourceId);
            }

            long elapsedSecondsUsed = Math.Min(activeSessionElapsedSeconds, config.maxActiveSessionElapsedSeconds);
            double progressDeltaPreview = elapsedSecondsUsed * config.progressPerActiveSecond;
            if (double.IsNaN(progressDeltaPreview) || double.IsInfinity(progressDeltaPreview))
            {
                return Error(ResearchProgressSummaryErrorCode.InvalidConfig, pendingState, config.ruleSourceId);
            }

            return new ResearchProgressSummary
            {
                RuleResolved = true,
                Pending = true,
                SlotId = pendingState.SlotId,
                ProjectId = pendingState.ProjectId,
                ElapsedSecondsUsed = elapsedSecondsUsed,
                ProgressDeltaPreview = progressDeltaPreview,
                WouldCompleteResearch = false,
                RuleSourceIdUsed = config.ruleSourceId
            };
        }

        private static ResearchProgressSummary Error(
            ResearchProgressSummaryErrorCode errorCode,
            ResearchPendingState pendingState = null,
            string ruleSourceId = "")
        {
            return new ResearchProgressSummary
            {
                DeterministicErrorCode = (int)errorCode,
                Pending = pendingState != null,
                SlotId = pendingState != null ? pendingState.SlotId ?? string.Empty : string.Empty,
                ProjectId = pendingState != null ? pendingState.ProjectId ?? string.Empty : string.Empty,
                WouldCompleteResearch = false,
                RuleSourceIdUsed = ruleSourceId ?? string.Empty
            };
        }
    }
}
