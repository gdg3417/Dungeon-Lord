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
            AppendSection(builder, localize, TopStatusKey);
            AppendLine(builder, Localize(localize, PlayerViewStatusKey));
            if (!string.IsNullOrWhiteSpace(bannerMessage))
            {
                AppendLine(builder, bannerMessage);
            }
            AppendLine(builder, BuildPathCompleteLine(guidedPath, localize));
            AppendLine(builder, string.Format(Localize(localize, MvpLoopSummaryPanelPresenter.ManaFormatKey), summary != null && summary.RuleResolved ? summary.ManaReserve : 0d));
            AppendLine(builder, BuildHeatAndPressureLine(summary, localize));
            AppendLine(builder, BuildResearchLine(summary, localize));

            AppendLine(builder, MvpFirstSessionObjectivePresenter.BuildCompactStatusLine(firstSessionObjective, localize));
            string greedTrialText = MvpPostContractGreedTrialPresenter.BuildPanelText(greedTrial, localize);
            if (!string.IsNullOrWhiteSpace(greedTrialText)) AppendLine(builder, greedTrialText);

            AppendSection(builder, localize, CurrentDungeonKey);
            AppendLine(builder, BuildPlayableCurrentDungeonLine(summary, dungeonLayoutText, localize));

            AppendSection(builder, localize, LatestRunKey);
            AppendLine(builder, ResolveRunOutcomeLine(summary, localize));
            AppendLine(builder, BuildPartyLine(summary, localize));
            AppendLine(builder, BuildLootLine(summary, localize));
            string spoilsText = MvpRecentSpoilsLedgerPresenter.BuildPanelText(recentSpoilsLedger, localize);
            if (!string.IsNullOrWhiteSpace(spoilsText)) AppendLine(builder, spoilsText);
            AppendLine(builder, BuildHeatLine(summary, localize));

            AppendSection(builder, localize, AnalysisNextActionKey);
            AppendLine(builder, BuildAnalysisLine(summary, localize));
            string suggestionKey = summary?.AnalysisUnlocked == true && !string.IsNullOrWhiteSpace(summary.AnalysisAdviceKey)
                ? summary.AnalysisAdviceKey
                : summary?.NextOptimizationSuggestionKey;
            suggestionKey = BasicRunAnalysisAppliedAdjustmentPresenter.ResolveNextActionKey(summary, suggestionKey);
            AppendLine(builder, string.Format(
                Localize(localize, MvpLoopSummaryPanelPresenter.SuggestionFormatKey),
                ResolveKeyOrFallback(suggestionKey, localize, MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey)));
            string appliedAdjustmentLine = BasicRunAnalysisAppliedAdjustmentPresenter.BuildAppliedAdjustmentLine(summary, localize);
            if (!string.IsNullOrWhiteSpace(appliedAdjustmentLine)) AppendLine(builder, appliedAdjustmentLine);

            AppendSection(builder, localize, RunSetupKey);
            AppendLine(builder, AdventurerRunIntentPresenter.BuildDebugPostureLine(summary?.AdventurerRunIntent, selectedRunPostureName, localize));
            AppendLine(builder, selectedRunPlanPreview);

            AppendSection(builder, localize, BuildChoiceKey);
            AppendLine(builder, string.Format(Localize(localize, SelectedPlacementFormatKey), selectedCategoryName, selectedOptionName));
            AppendLine(builder, selectedPlacementPreview);
            AppendLine(builder, string.IsNullOrWhiteSpace(selectedPlacementComparison) ? Localize(localize, NoComparisonKey) : selectedPlacementComparison);
            AppendLine(builder, string.IsNullOrWhiteSpace(placementFeedback) ? Localize(localize, PlacePromptKey) : placementFeedback);
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

        private static string BuildHeatAndPressureLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            return JoinInline(
                localize,
                BuildHeatLine(summary, localize),
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
            string tier = ResolveKeyOrFallback(summary?.HeatTierId, localize, MvpLoopSummaryPanelPresenter.ValueUnknownKey);
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
            string status = summary != null && summary.RuleResolved && summary.HasResearchStatus
                ? MvpPlayerFacingLabelResolver.ResolveResearchStatusLabel(summary.ResearchStatusKey, localize)
                : Localize(localize, MvpLoopSummaryPanelPresenter.ValueNoResearchKey);
            return string.Format(Localize(localize, ResearchFormatKey), status);
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
