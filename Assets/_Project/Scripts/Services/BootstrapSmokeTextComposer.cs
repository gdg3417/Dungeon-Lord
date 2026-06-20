using System;
using System.Text;

namespace DungeonBuilder.M0
{
    public static class BootstrapSmokeTextComposer
    {
        public const string CompactSmokeAdventurersUnavailableKey = "ui.mvp_smoke.adventurers_unavailable";

        public readonly struct Context
        {
            public Context(
                MvpPlayerLoopSummary summary,
                GuidedMvpActionPathSummary guidedPath,
                MvpFirstSessionObjectiveSummary firstSessionObjective,
                MvpPostContractGreedTrialSummary greedTrial,
                MvpRecentSpoilsLedgerSummary recentSpoilsLedger,
                string dungeonLayoutText,
                string selectedCategoryName,
                string selectedPlacementCategoryId,
                string selectedOptionName,
                string selectedPlacementPreview,
                string selectedPlacementComparison,
                string selectedRunPostureName,
                string selectedRunPlanPreview,
                string placementFeedback,
                string runFeedback,
                string bannerMessage,
                AdventurerRunIntentSummary lastRunIntentSummary,
                string lastRunPostureUsedId,
                string lastRunDebugPostureId,
                bool lastRunIntentFallbackUsed,
                string smokeViewportStatusMessage,
                int playerFacingSectionIndex,
                int playerFacingSectionCount)
            {
                Summary = summary;
                GuidedPath = guidedPath;
                FirstSessionObjective = firstSessionObjective;
                GreedTrial = greedTrial;
                RecentSpoilsLedger = recentSpoilsLedger;
                DungeonLayoutText = dungeonLayoutText;
                SelectedCategoryName = selectedCategoryName;
                SelectedPlacementCategoryId = selectedPlacementCategoryId;
                SelectedOptionName = selectedOptionName;
                SelectedPlacementPreview = selectedPlacementPreview;
                SelectedPlacementComparison = selectedPlacementComparison;
                SelectedRunPostureName = selectedRunPostureName;
                SelectedRunPlanPreview = selectedRunPlanPreview;
                PlacementFeedback = placementFeedback;
                RunFeedback = runFeedback;
                BannerMessage = bannerMessage;
                LastRunIntentSummary = lastRunIntentSummary;
                LastRunPostureUsedId = lastRunPostureUsedId;
                LastRunDebugPostureId = lastRunDebugPostureId;
                LastRunIntentFallbackUsed = lastRunIntentFallbackUsed;
                SmokeViewportStatusMessage = smokeViewportStatusMessage;
                PlayerFacingSectionIndex = playerFacingSectionIndex;
                PlayerFacingSectionCount = playerFacingSectionCount;
            }

            public MvpPlayerLoopSummary Summary { get; }
            public GuidedMvpActionPathSummary GuidedPath { get; }
            public MvpFirstSessionObjectiveSummary FirstSessionObjective { get; }
            public MvpPostContractGreedTrialSummary GreedTrial { get; }
            public MvpRecentSpoilsLedgerSummary RecentSpoilsLedger { get; }
            public string DungeonLayoutText { get; }
            public string SelectedCategoryName { get; }
            public string SelectedPlacementCategoryId { get; }
            public string SelectedOptionName { get; }
            public string SelectedPlacementPreview { get; }
            public string SelectedPlacementComparison { get; }
            public string SelectedRunPostureName { get; }
            public string SelectedRunPlanPreview { get; }
            public string PlacementFeedback { get; }
            public string RunFeedback { get; }
            public string BannerMessage { get; }
            public AdventurerRunIntentSummary LastRunIntentSummary { get; }
            public string LastRunPostureUsedId { get; }
            public string LastRunDebugPostureId { get; }
            public bool LastRunIntentFallbackUsed { get; }
            public string SmokeViewportStatusMessage { get; }
            public int PlayerFacingSectionIndex { get; }
            public int PlayerFacingSectionCount { get; }
        }

        public static string BuildFullPlayerFacingSmokeText(Context context, Func<string, string, string> localize)
        {
            var builder = new StringBuilder();
            AppendMvpLoopSummaryPanel(builder, context, localize);
            AppendLine(builder, AdventurerRunIntentPresenter.BuildScoreSummaryLine(context.Summary?.AdventurerRunIntent, localize));
            AppendLatestRunIntentEvidence(builder, context, localize);
            AppendLine(builder, AdventurerArrivalPressurePresenter.BuildDetailLine(context.Summary?.AdventurerArrivalPressure, localize));
            AppendLine(builder, AdventurerTrafficPressurePresenter.BuildDetailLine(context.Summary?.AdventurerTrafficPressure, localize));
            AppendPlayerFacingStatus(builder, context, localize);
            AppendMvpDungeonLayoutText(builder, context);
            return builder.ToString();
        }

        public static string BuildMvpLoopSummaryPanelText(Context context, Func<string, string, string> localize)
        {
            var builder = new StringBuilder();
            AppendMvpLoopSummaryPanel(builder, context, localize);
            return builder.ToString();
        }

        public static string BuildPlayableMvpScreenText(Context context, Func<string, string, string> localize)
        {
            var builder = new StringBuilder();
            AppendLine(builder, MvpPlayableScreenPresenter.BuildScreenText(
                context.Summary,
                context.GuidedPath,
                context.DungeonLayoutText,
                context.SelectedCategoryName,
                context.SelectedOptionName,
                context.SelectedPlacementPreview,
                context.SelectedPlacementComparison,
                context.SelectedRunPostureName,
                context.SelectedRunPlanPreview,
                context.PlacementFeedback,
                context.RunFeedback,
                context.BannerMessage,
                context.FirstSessionObjective,
                context.GreedTrial,
                context.RecentSpoilsLedger,
                localize));
            return builder.ToString();
        }

        public static string BuildCompactSmokeText(Context context, Func<string, string, string> localize)
        {
            var body = new StringBuilder();
            string panelText = MvpLoopSummaryPanelPresenter.BuildPanelText(context.Summary, context.SelectedPlacementCategoryId, localize);
            AppendCompactLoopSummaryLines(body, panelText, localize);
            AppendMvpDungeonLayoutText(body, context);
            AppendLine(body, MvpFirstSessionObjectivePresenter.BuildCompactStatusLine(context.FirstSessionObjective, localize));
            string greedTrialText = MvpPostContractGreedTrialPresenter.BuildPanelText(context.GreedTrial, localize);
            if (!string.IsNullOrEmpty(greedTrialText)) AppendLine(body, greedTrialText);
            string spoilsText = MvpRecentSpoilsLedgerPresenter.BuildPanelText(context.RecentSpoilsLedger, localize);
            if (!string.IsNullOrEmpty(spoilsText)) AppendLine(body, spoilsText);
            AppendLine(body, AdventurerRunIntentPresenter.BuildScoreSummaryLine(context.Summary?.AdventurerRunIntent, localize));
            AppendCompactAdventurersFallbackIfMissing(body, localize);
            AppendSelectedPlacementAndRunPlanPreviews(body, context);
            if (!string.IsNullOrEmpty(context.RunFeedback))
            {
                AppendLine(body, context.RunFeedback);
            }
            AppendLine(body, string.Format(
                localize(GuidedMvpActionPathPanelPresenter.CompleteFormatKey, GuidedMvpActionPathPanelPresenter.CompleteFormatKey),
                localize(context.GuidedPath != null && context.GuidedPath.IsComplete
                    ? GuidedMvpActionPathPanelPresenter.CompleteYesKey
                    : GuidedMvpActionPathPanelPresenter.CompleteNoKey,
                    context.GuidedPath != null && context.GuidedPath.IsComplete
                        ? GuidedMvpActionPathPanelPresenter.CompleteYesKey
                        : GuidedMvpActionPathPanelPresenter.CompleteNoKey)));

            return BuildSectionText("ui.mvp_smoke.section.compact", body.ToString(), context, localize);
        }

        public static string BuildLoopSummarySectionText(Context context, Func<string, string, string> localize)
        {
            var body = new StringBuilder();
            AppendLine(body, MvpLoopSummaryPanelPresenter.BuildPanelText(context.Summary, context.SelectedPlacementCategoryId, localize));
            AppendMvpDungeonLayoutText(body, context);
            string guidedText = GuidedMvpActionPathPanelPresenter.BuildPanelText(context.GuidedPath, localize);
            if (!string.IsNullOrEmpty(guidedText)) { AppendLine(body, string.Empty); AppendLine(body, guidedText); }
            string firstSessionText = FirstSessionMvpCompletionPresenter.BuildStatusLine(context.Summary, context.GuidedPath, localize);
            if (!string.IsNullOrEmpty(firstSessionText)) { AppendLine(body, string.Empty); AppendLine(body, firstSessionText); }
            string firstContractText = MvpFirstSessionObjectivePresenter.BuildPanelText(context.FirstSessionObjective, localize);
            if (!string.IsNullOrEmpty(firstContractText)) { AppendLine(body, string.Empty); AppendLine(body, firstContractText); }
            string greedTrialText = MvpPostContractGreedTrialPresenter.BuildPanelText(context.GreedTrial, localize);
            if (!string.IsNullOrEmpty(greedTrialText)) { AppendLine(body, string.Empty); AppendLine(body, greedTrialText); }
            string spoilsText = MvpRecentSpoilsLedgerPresenter.BuildPanelText(context.RecentSpoilsLedger, localize);
            if (!string.IsNullOrEmpty(spoilsText)) { AppendLine(body, string.Empty); AppendLine(body, spoilsText); }
            return BuildSectionText("ui.mvp_smoke.section.loop_summary", body.ToString(), context, localize);
        }

        public static string BuildPlanAndActionSectionText(Context context, Func<string, string, string> localize)
        {
            var body = new StringBuilder();
            AppendSelectedPlacementAndRunPlanPreviews(body, context);
            AppendLine(body, localize("ui.mvp_view.player_mode.status", "ui.mvp_view.player_mode.status"));
            if (!string.IsNullOrEmpty(context.PlacementFeedback)) AppendLine(body, context.PlacementFeedback);
            return BuildSectionText("ui.mvp_smoke.section.plan_and_action", body.ToString(), context, localize);
        }

        public static string BuildLatestRunFeedbackSectionText(Context context, Func<string, string, string> localize)
        {
            var body = new StringBuilder();
            AppendLine(body, !string.IsNullOrEmpty(context.RunFeedback) ? context.RunFeedback : localize("ui.mvp_run_feedback.unavailable", "ui.mvp_run_feedback.unavailable"));
            return BuildSectionText("ui.mvp_smoke.section.latest_run_feedback", body.ToString(), context, localize);
        }

        private static void AppendMvpLoopSummaryPanel(StringBuilder builder, Context context, Func<string, string, string> localize)
        {
            string panelText = MvpLoopSummaryPanelPresenter.BuildPanelText(context.Summary, context.SelectedPlacementCategoryId, localize);
            if (!string.IsNullOrEmpty(panelText)) AppendLine(builder, panelText);
            string guidedText = GuidedMvpActionPathPanelPresenter.BuildPanelText(context.GuidedPath, localize);
            if (!string.IsNullOrEmpty(guidedText)) { AppendLine(builder, string.Empty); AppendLine(builder, guidedText); }
            string firstSessionText = FirstSessionMvpCompletionPresenter.BuildStatusLine(context.Summary, context.GuidedPath, localize);
            if (!string.IsNullOrEmpty(firstSessionText)) { AppendLine(builder, string.Empty); AppendLine(builder, firstSessionText); }
            string firstContractText = MvpFirstSessionObjectivePresenter.BuildPanelText(context.FirstSessionObjective, localize);
            if (!string.IsNullOrEmpty(firstContractText)) { AppendLine(builder, string.Empty); AppendLine(builder, firstContractText); }
            string greedTrialText = MvpPostContractGreedTrialPresenter.BuildPanelText(context.GreedTrial, localize);
            if (!string.IsNullOrEmpty(greedTrialText)) { AppendLine(builder, string.Empty); AppendLine(builder, greedTrialText); }
            string spoilsText = MvpRecentSpoilsLedgerPresenter.BuildPanelText(context.RecentSpoilsLedger, localize);
            if (!string.IsNullOrEmpty(spoilsText)) { AppendLine(builder, string.Empty); AppendLine(builder, spoilsText); }
            AppendLine(builder, string.Empty);
            AppendSelectedPlacementAndRunPlanPreviews(builder, context);
        }

        private static void AppendCompactLoopSummaryLines(StringBuilder builder, string panelText, Func<string, string, string> localize)
        {
            if (string.IsNullOrEmpty(panelText)) return;
            string suggestionPrefix = localize(MvpLoopSummaryPanelPresenter.SuggestionFormatKey, MvpLoopSummaryPanelPresenter.SuggestionFormatKey).Split('{')[0];
            string title = localize(MvpLoopSummaryPanelPresenter.TitleKey, MvpLoopSummaryPanelPresenter.TitleKey);
            string[] lines = panelText.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrEmpty(line) || line == title || (!string.IsNullOrEmpty(suggestionPrefix) && line.StartsWith(suggestionPrefix))) continue;
                AppendLine(builder, line);
            }
        }

        private static void AppendCompactAdventurersFallbackIfMissing(StringBuilder builder, Func<string, string, string> localize)
        {
            if (!ContainsAdventurerLine(builder, localize)) AppendLine(builder, localize(CompactSmokeAdventurersUnavailableKey, CompactSmokeAdventurersUnavailableKey));
        }

        private static bool ContainsAdventurerLine(StringBuilder builder, Func<string, string, string> localize)
        {
            string partyPreviewPrefix = localize(MvpRunResultFeedbackPresenter.PartyPreviewFormatKey, MvpRunResultFeedbackPresenter.PartyPreviewFormatKey).Split('{')[0];
            if (string.IsNullOrEmpty(partyPreviewPrefix)) return false;
            string[] lines = builder.ToString().Split('\n');
            for (int i = 0; i < lines.Length; i++) if (lines[i].StartsWith(partyPreviewPrefix)) return true;
            return false;
        }

        private static string BuildSectionText(string sectionNameKey, string body, Context context, Func<string, string, string> localize)
        {
            var builder = new StringBuilder();
            AppendLine(builder, string.Format(localize("ui.mvp_smoke.section.status_format", "ui.mvp_smoke.section.status_format"), localize(sectionNameKey, sectionNameKey), context.PlayerFacingSectionIndex + 1, context.PlayerFacingSectionCount));
            if (!string.IsNullOrEmpty(context.SmokeViewportStatusMessage)) AppendLine(builder, context.SmokeViewportStatusMessage);
            if (!string.IsNullOrEmpty(body)) AppendLine(builder, body);
            return builder.ToString();
        }

        private static void AppendMvpDungeonLayoutText(StringBuilder builder, Context context)
        {
            if (!string.IsNullOrEmpty(context.DungeonLayoutText)) AppendLine(builder, context.DungeonLayoutText);
        }

        private static void AppendSelectedPlacementAndRunPlanPreviews(StringBuilder builder, Context context)
        {
            if (!string.IsNullOrEmpty(context.SelectedPlacementPreview)) AppendLine(builder, context.SelectedPlacementPreview);
            if (!string.IsNullOrEmpty(context.SelectedRunPlanPreview)) AppendLine(builder, context.SelectedRunPlanPreview);
        }

        private static void AppendPlayerFacingStatus(StringBuilder builder, Context context, Func<string, string, string> localize)
        {
            AppendLine(builder, string.Empty);
            AppendLine(builder, localize("ui.mvp_view.player_mode.status", "ui.mvp_view.player_mode.status"));
            if (!string.IsNullOrEmpty(context.BannerMessage)) AppendLine(builder, context.BannerMessage);
            if (!string.IsNullOrEmpty(context.PlacementFeedback)) AppendLine(builder, context.PlacementFeedback);
            if (!string.IsNullOrEmpty(context.RunFeedback)) AppendLine(builder, context.RunFeedback);
        }

        private static void AppendLatestRunIntentEvidence(StringBuilder builder, Context context, Func<string, string, string> localize)
        {
            if (string.IsNullOrWhiteSpace(context.LastRunPostureUsedId)) return;
            AppendLine(builder, AdventurerRunIntentPresenter.BuildSmokeEvidenceLine(context.LastRunIntentSummary, context.LastRunPostureUsedId, context.LastRunDebugPostureId, context.LastRunIntentFallbackUsed, localize));
        }

        private static void AppendLine(StringBuilder builder, string line)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }
            builder.Append(line ?? string.Empty);
        }
    }
}
