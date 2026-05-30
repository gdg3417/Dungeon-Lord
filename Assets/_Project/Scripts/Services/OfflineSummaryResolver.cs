using System;

namespace DungeonBuilder.M0
{
    public sealed class OfflineSummaryResolver
    {
        private readonly ITimeSource _timeSource;

        public OfflineSummaryResolver(ITimeSource timeSource)
        {
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
        }

        public OfflineSummary Resolve(SaveData save, TimeRules timeRules)
        {
            var summary = new OfflineSummary
            {
                ResearchPending = HasPendingResearch(save != null ? save.researchPending : null),
                ResearchSlotId = save != null && save.researchPending != null ? save.researchPending.SlotId : string.Empty,
                ResearchProjectId = save != null && save.researchPending != null ? save.researchPending.ProjectId : string.Empty,
                WouldProcessOfflineProgress = false,
                RuleSourceIdUsed = timeRules != null ? timeRules.offlineSummaryRuleSourceId : string.Empty
            };

            if (save == null)
            {
                return WithError(summary, OfflineSummaryErrorCode.SaveMissing);
            }

            if (timeRules == null || timeRules.maxOfflineSeconds < 0 || string.IsNullOrWhiteSpace(timeRules.offlineSummaryRuleSourceId))
            {
                return WithError(summary, OfflineSummaryErrorCode.TimeRulesMissingOrInvalid);
            }

            long currentTimestamp = _timeSource.UtcNowUnixSeconds();
            if (currentTimestamp <= 0)
            {
                return WithError(summary, OfflineSummaryErrorCode.CurrentTimestampInvalid);
            }

            if (save.lastSavedUtcUnix <= 0)
            {
                return WithError(summary, OfflineSummaryErrorCode.LastKnownTimestampInvalid);
            }

            if (currentTimestamp < save.lastSavedUtcUnix)
            {
                return WithError(summary, OfflineSummaryErrorCode.CurrentTimestampBeforeLastKnownTimestamp);
            }

            long elapsedSeconds = currentTimestamp - save.lastSavedUtcUnix;
            summary.OfflineWindowClamped = elapsedSeconds > timeRules.maxOfflineSeconds;
            summary.OfflineSecondsObserved = summary.OfflineWindowClamped ? timeRules.maxOfflineSeconds : elapsedSeconds;
            summary.RuleResolved = true;
            return summary;
        }

        private static bool HasPendingResearch(ResearchPendingState researchPending)
        {
            return researchPending != null && !string.IsNullOrWhiteSpace(researchPending.ProjectId);
        }

        private static OfflineSummary WithError(OfflineSummary summary, OfflineSummaryErrorCode errorCode)
        {
            summary.DeterministicErrorCode = (int)errorCode;
            return summary;
        }
    }
}
