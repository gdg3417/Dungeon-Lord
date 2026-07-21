using System;
using System.Text;

namespace DungeonBuilder.M0
{
    public static class MvpPlayableScreenPresenter
    {
        public const string TitleKey = "ui.mvp_screen.title";
        public const string TopStatusKey = "ui.mvp_screen.section.top_status";
        public const string CurrentDungeonKey = "ui.mvp_screen.section.current_dungeon";
        public const string BuildChoiceKey = "ui.mvp_screen.section.build_choice";
        public const string RunSetupKey = "ui.mvp_screen.section.run_setup";
        public const string LatestRunKey = "ui.mvp_screen.section.latest_run";
        public const string AnalysisNextActionKey = "ui.mvp_screen.section.analysis_next_action";
        public const string FirstContractKey = "ui.mvp_screen.section.first_contract";
        public const string ActionControlsKey = "ui.mvp_screen.section.action_controls";
        public const string LatestResultKey = "ui.mvp_screen.section.latest_result";
        public const string DetailsHintKey = "ui.mvp_screen.details_hint";
        public const string RoomTargetControlFormatKey = "ui.mvp_screen.control.room_target_format";
        public const string PlacementControlFormatKey = "ui.mvp_screen.control.placement_format";
        public const string PlaceButtonControlKey = "ui.mvp_screen.control.place_button";
        public const string RunPostureControlFormatKey = "ui.mvp_screen.control.run_posture_format";
        public const string RunButtonControlKey = "ui.mvp_screen.control.run_button";
        public const string LatestResultFormatKey = "ui.mvp_screen.latest_result_format";
        public const string LatestResultNoRunKey = "ui.mvp_screen.latest_result.no_run";
        public const string CurrentHeatFormatKey = "ui.mvp_screen.current_heat_format";
        public const string SectionHeaderFormatKey = "ui.mvp_screen.section.header_format";
        public const string LineFormatKey = "ui.mvp_screen.line_format";
        public const string SelectedCategoryFormatKey = "ui.mvp_screen.selected_category_format";
        public const string SelectedOptionFormatKey = "ui.mvp_screen.selected_option_format";
        public const string SelectedPlacementFormatKey = "ui.mvp_screen.selected_placement_format";
        public const string RunPostureFormatKey = "ui.mvp_screen.run_posture_format";
        public const string PlacePromptKey = "ui.mvp_screen.prompt.place_or_modify";
        public const string RunPromptKey = "ui.mvp_screen.prompt.run_or_observe";
        public const string NoPlacementFeedbackKey = "ui.mvp_screen.feedback.no_placement";
        public const string NoRunFeedbackKey = "ui.mvp_screen.feedback.no_run";
        public const string NoComparisonKey = "ui.mvp_screen.comparison.none";
        public const string NoAnalysisKey = "ui.mvp_screen.analysis.no_run";
        public const string PartyUnavailableKey = "ui.mvp_screen.party.unavailable";
        public const string PartyFormatKey = "ui.mvp_screen.party.format";
        public const string ResearchFormatKey = "ui.mvp_screen.research_format";
        public const string AnalysisFormatKey = "ui.mvp_screen.analysis.format";
        public const string PathCompleteFormatKey = "ui.mvp_screen.path_complete_format";
        public const string PlayerViewStatusKey = "ui.mvp_view.player_mode.status";

        public static string BuildScreenText(
            MvpPlayerLoopSummary summary,
            GuidedMvpActionPathSummary guidedPath,
            string dungeonLayoutText,
            string selectedCategoryName,
            string selectedOptionName,
            string selectedPlacementPreview,
            string selectedPlacementComparison,
            string selectedRunPostureName,
            string selectedRunPlanPreview,
            string placementFeedback,
            string runFeedback,
            string bannerMessage,
            MvpFirstSessionObjectiveSummary firstSessionObjective,
            MvpPostContractGreedTrialSummary greedTrial,
            MvpRecentSpoilsLedgerSummary recentSpoilsLedger,
            Func<string, string, string> localize)
        {
            var builder = new StringBuilder();
            AppendLine(builder, Localize(localize, TitleKey));
            AppendLine(builder, MvpPrimaryNextActionPresenter.BuildPanelText(MvpPrimaryNextActionPresenter.Resolve(summary, guidedPath, firstSessionObjective, greedTrial), localize));
            if (!string.IsNullOrWhiteSpace(bannerMessage)) AppendLine(builder, bannerMessage);

            AppendSection(builder, localize, TopStatusKey);
            AppendLine(builder, BuildCurrentHeatStatusLine(summary, localize));
            AppendLine(builder, BuildPressureLine(summary, localize));
            AppendLine(builder, BuildPlayableCurrentDungeonLine(summary, dungeonLayoutText, localize));
            AppendLine(builder, MvpFirstSessionObjectivePresenter.BuildCompactStatusLine(firstSessionObjective, localize));
            string greedTrialText = MvpPostContractGreedTrialPresenter.BuildStatusPanelText(greedTrial, localize);
            if (!string.IsNullOrWhiteSpace(greedTrialText)) AppendLine(builder, greedTrialText);
            string researchLine = BuildResearchLine(summary, localize);
            if (summary != null && summary.HasResearchStatus) AppendLine(builder, researchLine);

            AppendSection(builder, localize, ActionControlsKey);
            AppendLine(builder, string.Format(Localize(localize, RoomTargetControlFormatKey), ExtractLine(dungeonLayoutText, MvpRoomSlotTargetPresenter.SelectedTargetFormatKey, localize), ExtractLine(dungeonLayoutText, MvpRoomSlotTargetPresenter.SelectedCapacityFormatKey, localize)));
            AppendLine(builder, string.Format(Localize(localize, PlacementControlFormatKey), selectedCategoryName, selectedOptionName));
            AppendLine(builder, Localize(localize, PlaceButtonControlKey));
            AppendLine(builder, string.Format(Localize(localize, RunPostureControlFormatKey), selectedRunPostureName));
            if (!string.IsNullOrWhiteSpace(selectedRunPlanPreview)) AppendLine(builder, selectedRunPlanPreview);
            AppendLine(builder, Localize(localize, RunButtonControlKey));
            if (!string.IsNullOrWhiteSpace(placementFeedback)) AppendLine(builder, placementFeedback);

            AppendSection(builder, localize, LatestResultKey);
            AppendLine(builder, BuildLatestResultLine(summary, localize));
            string routeResult = MvpRouteResultPresenter.BuildCompactText(summary, localize);
            if (!string.IsNullOrWhiteSpace(routeResult)) AppendLine(builder, routeResult);
            string appliedAdjustmentLine = BasicRunAnalysisAppliedAdjustmentPresenter.BuildAppliedAdjustmentLine(summary, localize);
            if (!string.IsNullOrWhiteSpace(appliedAdjustmentLine)) AppendLine(builder, appliedAdjustmentLine);
            AppendLine(builder, Localize(localize, DetailsHintKey));
            return builder.ToString();
        }

        private static string BuildPlayableCurrentDungeonLine(MvpPlayerLoopSummary summary, string dungeonLayoutText, Func<string, string, string> localize)
        {
            string composition = BuildCurrentDungeonCompositionLine(summary, localize);
            string selectedTarget = ExtractLine(dungeonLayoutText, MvpRoomSlotTargetPresenter.SelectedTargetFormatKey, localize);
            string selectedCapacity = ExtractLine(dungeonLayoutText, MvpRoomSlotTargetPresenter.SelectedCapacityFormatKey, localize);
            string selectedFit = ExtractLine(dungeonLayoutText, MvpRoomSlotTargetPresenter.SelectedPlacementFitFormatKey, localize);
            string roomSlotLayout = ExtractLine(dungeonLayoutText, MvpDungeonLayoutPresenter.RoomSlotLayoutFormatKey, localize);
            return JoinInline(localize, composition, selectedTarget, selectedCapacity, selectedFit, roomSlotLayout);
        }

        private static string ExtractLine(string dungeonLayoutText, string key, Func<string, string, string> localize)
        {
            if (string.IsNullOrWhiteSpace(dungeonLayoutText))
            {
                return string.Empty;
            }

            string roomSlotPrefix = Localize(localize, key).Split('{')[0];
            foreach (string line in dungeonLayoutText.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(roomSlotPrefix) && line.StartsWith(roomSlotPrefix, StringComparison.Ordinal))
                {
                    return line;
                }
            }

            return string.Empty;
        }

        private static string BuildCurrentHeatStatusLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            double currentHeat = summary != null && summary.RuleResolved ? summary.CurrentHeat : 0d;
            string tierId = summary != null && summary.RuleResolved && !string.IsNullOrWhiteSpace(summary.CurrentHeatTierId)
                ? summary.CurrentHeatTierId
                : summary?.HeatTierId;
            string tier = ResolveKeyOrFallback(tierId, localize, MvpLoopSummaryPanelPresenter.ValueUnknownKey);
            return string.Format(Localize(localize, CurrentHeatFormatKey), currentHeat, tier);
        }

        private static string BuildPressureLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            return JoinInline(
                localize,
                AdventurerArrivalPressurePresenter.BuildSummaryLine(summary?.AdventurerArrivalPressure, localize),
                AdventurerTrafficPressurePresenter.BuildSummaryLine(summary?.AdventurerTrafficPressure, localize));
        }

        private static string BuildPathCompleteLine(GuidedMvpActionPathSummary guidedPath, Func<string, string, string> localize)
        {
            return string.Format(
                Localize(localize, PathCompleteFormatKey),
                Localize(localize, guidedPath != null && guidedPath.IsComplete ? GuidedMvpActionPathPanelPresenter.CompleteYesKey : GuidedMvpActionPathPanelPresenter.CompleteNoKey));
        }

        private static string BuildHeatLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            double before = summary != null && summary.RuleResolved ? summary.HeatBefore : 0d;
            double after = summary != null && summary.RuleResolved ? summary.HeatAfter : 0d;
            string tierId = !string.IsNullOrWhiteSpace(summary?.LatestRunHeatTierId) ? summary.LatestRunHeatTierId : summary?.HeatTierId;
            string tier = ResolveKeyOrFallback(tierId, localize, MvpLoopSummaryPanelPresenter.ValueUnknownKey);
            string riskKey = summary == null || !summary.RuleResolved || !summary.HasRunOutcome ? MvpLoopSummaryPanelPresenter.RiskNoRunKey : after > before ? MvpLoopSummaryPanelPresenter.RiskIncreasedKey : after < before ? MvpLoopSummaryPanelPresenter.RiskReducedKey : MvpLoopSummaryPanelPresenter.RiskStableKey;
            return string.Format(Localize(localize, MvpLoopSummaryPanelPresenter.HeatFormatKey), before, after, tier, Localize(localize, riskKey));
        }

        private static string BuildCurrentDungeonCompositionLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            string composition;
            if (summary == null || !summary.RuleResolved)
            {
                composition = Localize(localize, MvpLoopSummaryPanelPresenter.ValueNoPlacementKey);
            }
            else if (summary.DungeonPlacements != null && summary.DungeonPlacements.Length > 0)
            {
                composition = MvpDungeonPlacementPresenter.BuildCompositionText(summary.DungeonPlacements, localize);
            }
            else if (summary.HasPlacementContext && !string.IsNullOrWhiteSpace(summary.SelectedStructureId))
            {
                composition = MvpPlayerFacingLabelResolver.ResolveStructureDisplayName(summary.SelectedStructureId, localize);
            }
            else
            {
                composition = Localize(localize, MvpLoopSummaryPanelPresenter.ValueNoPlacementKey);
            }

            return string.Format(Localize(localize, MvpLoopSummaryPanelPresenter.CompositionFormatKey), composition);
        }

        private static string BuildResearchLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            string status;
            if (summary == null || !summary.RuleResolved || !summary.HasResearchStatus)
            {
                status = Localize(localize, MvpLoopSummaryPanelPresenter.ValueNoResearchKey);
            }
            else if (summary.PlayerResearchAuthority != null && summary.PlayerResearchAuthority.RuleResolved)
            {
                status = PlayerResearchStatusTextPresenter.Present(
                    summary.ResearchStatusKey,
                    summary.PlayerResearchAuthority,
                    localize);
            }
            else
            {
                status = MvpPlayerFacingLabelResolver.ResolveResearchStatusLabel(summary.ResearchStatusKey, localize);
            }

            return string.Format(Localize(localize, ResearchFormatKey), status);
        }

        private static string BuildLatestResultLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved || !summary.HasRunOutcome)
            {
                return Localize(localize, LatestResultNoRunKey);
            }

            return string.Format(
                Localize(localize, LatestResultFormatKey),
                ResolveRunOutcomeLine(summary, localize),
                BuildPartyLine(summary, localize),
                BuildLootLine(summary, localize),
                BuildHeatLine(summary, localize),
                BuildAnalysisLine(summary, localize));
        }

        private static string ResolveRunOutcomeLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved || !summary.HasRunOutcome)
            {
                return Localize(localize, MvpLoopSummaryPanelPresenter.ValueNoRunKey);
            }

            return Localize(localize, summary.RunSucceeded ? MvpLoopSummaryPanelPresenter.RunSucceededKey : MvpLoopSummaryPanelPresenter.RunFailedKey);
        }

        private static string BuildPartyLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.AdventurerPartyPreviewResolved || summary.AdventurerPartyClassIds == null || summary.AdventurerPartyClassIds.Length == 0)
            {
                return Localize(localize, PartyUnavailableKey);
            }

            string[] labels = new string[summary.AdventurerPartyClassIds.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = AdventurerPartyCompositionResolver.ResolveClassLabel(summary.AdventurerPartyClassIds[i], localize);
            }

            return string.Format(Localize(localize, PartyFormatKey), string.Join(", ", labels));
        }

        private static string BuildLootLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            int generated = summary != null && summary.RuleResolved ? summary.LootGeneratedWorldValue : 0;
            int extracted = summary != null && summary.RuleResolved ? summary.LootExtractedWorldValue : 0;
            int tradeable = summary != null && summary.RuleResolved ? summary.LootExtractedTradeableWorldValue : 0;
            string namedLoot = summary != null && summary.RuleResolved ? MvpLoopSummaryPanelPresenter.BuildNamedLootText(summary.LootBreakdown, localize) : string.Empty;
            return !string.IsNullOrWhiteSpace(namedLoot)
                ? string.Format(Localize(localize, MvpLoopSummaryPanelPresenter.LootNamedFormatKey), generated, extracted, tradeable, namedLoot)
                : string.Format(Localize(localize, MvpLoopSummaryPanelPresenter.LootFormatKey), generated, extracted, tradeable);
        }

        private static string BuildAnalysisLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved || !summary.HasRunOutcome)
            {
                return summary != null && summary.AnalysisUnlocked
                    ? Localize(localize, MvpLoopSummaryPanelPresenter.AnalysisNoRunKey)
                    : Localize(localize, NoAnalysisKey);
            }

            string panelText = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, localize);
            string header = Localize(localize, MvpLoopSummaryPanelPresenter.WhyItHappenedSectionKey);
            foreach (string line in panelText.Split('\n'))
            {
                if (line.StartsWith(header + ":", StringComparison.Ordinal))
                {
                    return string.Format(Localize(localize, AnalysisFormatKey), line.Substring(header.Length + 1).Trim());
                }
            }

            return Localize(localize, NoAnalysisKey);
        }

        private static void AppendSection(StringBuilder builder, Func<string, string, string> localize, string titleKey)
        {
            AppendLine(builder, string.Format(Localize(localize, SectionHeaderFormatKey), Localize(localize, titleKey)));
        }

        private static string JoinInline(Func<string, string, string> localize, params string[] parts)
        {
            if (parts == null || parts.Length == 0)
            {
                return string.Empty;
            }

            string separator = Localize(localize, MvpLoopSummaryPanelPresenter.InlineSeparatorKey);
            var builder = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(parts[i]))
                {
                    continue;
                }

                if (builder.Length > 0) builder.Append(separator);
                builder.Append(parts[i]);
            }

            return builder.ToString();
        }

        private static string ResolveKeyOrFallback(string key, Func<string, string, string> localize, string fallbackKey)
        {
            return string.IsNullOrWhiteSpace(key) ? Localize(localize, fallbackKey) : Localize(localize, key);
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            return localize == null ? key : localize(key, key);
        }

        private static void AppendLine(StringBuilder builder, string line)
        {
            if (builder.Length > 0) builder.Append('\n');
            builder.Append(line ?? string.Empty);
        }
    }
}
