using System;

namespace DungeonBuilder.M0
{
    public static class FirstSessionMvpCompletionPresenter
    {
        public const string NotStartedKey = "ui.first_session.status.not_started";
        public const string PlaceStructureKey = "ui.first_session.status.place_structure";
        public const string RunDungeonKey = "ui.first_session.status.run_dungeon";
        public const string ObserveSummaryKey = "ui.first_session.status.observe_summary";
        public const string CompleteKey = "ui.first_session.status.complete";

        public static FirstSessionMvpCompletionSummary Resolve(
            MvpPlayerLoopSummary loopSummary,
            GuidedMvpActionPathSummary guidedPath)
        {
            if (loopSummary == null || !loopSummary.RuleResolved)
            {
                return Create(NotStartedKey, isComplete: false);
            }

            if (!loopSummary.HasPlacementContext)
            {
                return Create(PlaceStructureKey, isComplete: false);
            }

            if (!loopSummary.HasRunOutcome)
            {
                return Create(RunDungeonKey, isComplete: false);
            }

            if (!loopSummary.HasResearchStatus)
            {
                return Create(ObserveSummaryKey, isComplete: false);
            }

            return IsGuidedActionRepeatOrCoherentAfterRun(guidedPath)
                ? Create(CompleteKey, isComplete: true)
                : Create(ObserveSummaryKey, isComplete: false);
        }

        public static string BuildStatusLine(
            MvpPlayerLoopSummary loopSummary,
            GuidedMvpActionPathSummary guidedPath,
            Func<string, string, string> localize)
        {
            FirstSessionMvpCompletionSummary summary = Resolve(loopSummary, guidedPath);
            return Localize(localize, summary.StatusKey);
        }

        private static bool IsGuidedActionRepeatOrCoherentAfterRun(GuidedMvpActionPathSummary guidedPath)
        {
            if (guidedPath == null || !guidedPath.RuleResolved)
            {
                return false;
            }

            if (guidedPath.IsComplete || string.Equals(guidedPath.CurrentStepId, GuidedMvpActionPathPresenter.StepRepeatOrImproveId, StringComparison.Ordinal))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(guidedPath.NextActionKey)
                && !string.Equals(guidedPath.CurrentStepId, GuidedMvpActionPathPresenter.StepPlaceOrModifyStructureId, StringComparison.Ordinal)
                && !string.Equals(guidedPath.CurrentStepId, GuidedMvpActionPathPresenter.StepRunOrObserveId, StringComparison.Ordinal);
        }

        private static FirstSessionMvpCompletionSummary Create(string statusKey, bool isComplete)
        {
            return new FirstSessionMvpCompletionSummary
            {
                StatusKey = statusKey,
                IsComplete = isComplete,
                WouldMutateState = false,
                WouldGrantRewards = false,
                WouldUnlockContent = false,
                WouldChargeCosts = false,
                WouldCallServer = false,
                WouldProcessOfflineResearch = false,
                WouldProcessOfflineHeat = false,
                WouldStartRaid = false
            };
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            if (localize == null)
            {
                return key;
            }

            return localize(key, key);
        }
    }

    public sealed class FirstSessionMvpCompletionSummary
    {
        public string StatusKey;
        public bool IsComplete;
        public bool WouldMutateState;
        public bool WouldGrantRewards;
        public bool WouldUnlockContent;
        public bool WouldChargeCosts;
        public bool WouldCallServer;
        public bool WouldProcessOfflineResearch;
        public bool WouldProcessOfflineHeat;
        public bool WouldStartRaid;
    }
}
