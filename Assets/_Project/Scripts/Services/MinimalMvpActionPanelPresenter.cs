using System;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.M0
{
    public static class MinimalMvpActionPanelPresenter
    {
        public const string TitleKey = "ui.mvp_action.panel.title";
        public const string PlacementButtonKey = "ui.mvp_action.button.place_or_modify";
        public const string RunButtonKey = "ui.mvp_action.button.run_or_observe";
        public const string ShowDiagnosticsButtonKey = "ui.mvp_action.button.show_diagnostics";
        public const string HideDiagnosticsButtonKey = "ui.mvp_action.button.hide_diagnostics";
        public const string CollapsePanelButtonKey = "ui.mvp_action.button.collapse_panel";
        public const string ExpandPanelButtonKey = "ui.mvp_action.button.expand_panel";
        public const string CompactFormatKey = "ui.mvp_action.panel.compact_format";
        public const string SelectionLabelKey = "ui.mvp_action.selection.label";
        public const string PostureLabelKey = "ui.mvp_action.posture.label";
        public const string CategoryLabelKey = "ui.mvp_action.category.label";
        public const string CautiousPostureKey = "run.posture.cautious.name";
        public const string BalancedPostureKey = "run.posture.balanced.name";
        public const string GreedyPostureKey = "run.posture.greedy.name";
        public const string ManaGeneratorSelectionKey = "ui.mvp_action.selection.mana_generator";
        public const string HeatScrubberSelectionKey = "ui.mvp_action.selection.heat_scrubber";
        public const string RiskLabSelectionKey = "ui.mvp_action.selection.risk_lab";

        public static MinimalMvpActionPanelLabels BuildLabels(Func<string, string, string> localize)
        {
            return BuildLabels(localize, ManaGeneratorSelectionKey, StructureSimulationPass.ManaGeneratorBasicId);
        }

        public static MinimalMvpActionPanelLabels BuildLabels(Func<string, string, string> localize, string selectedStructureNameKey)
        {
            return BuildLabels(localize, selectedStructureNameKey, ResolveStructureIdFromSelectionKey(selectedStructureNameKey));
        }

        public static MinimalMvpActionPanelLabels BuildLabels(Func<string, string, string> localize, string selectedStructureNameKey, string selectedStructureId)
        {
            return BuildLabels(localize, selectedStructureNameKey, selectedStructureId, BalancedPostureKey);
        }

        public static MinimalMvpActionPanelLabels BuildLabels(Func<string, string, string> localize, string selectedStructureNameKey, string selectedStructureId, string selectedPostureNameKey)
        {
            string selectedName = Localize(localize, string.IsNullOrWhiteSpace(selectedStructureNameKey) ? ManaGeneratorSelectionKey : selectedStructureNameKey);
            string selectionFormat = Localize(localize, SelectionLabelKey);
            string selectedPostureName = Localize(localize, string.IsNullOrWhiteSpace(selectedPostureNameKey) ? BalancedPostureKey : selectedPostureNameKey);
            string postureFormat = Localize(localize, PostureLabelKey);
            return CreateLabels(
                localize,
                string.Format(selectionFormat, selectedName),
                string.Empty,
                string.Format(postureFormat, selectedPostureName),
                MvpStructureImpactPreviewPresenter.BuildRunPlanPreviewText(selectedStructureId, selectedPostureNameKey, localize),
                MvpStructureImpactPreviewPresenter.BuildRunPlanPreviewText(selectedStructureId, selectedPostureNameKey, localize));
        }

        public static MinimalMvpActionPanelLabels BuildPlacementLabels(
            Func<string, string, string> localize,
            string selectedCategoryId,
            string selectedOptionId,
            string selectedPostureNameKey)
        {
            return BuildPlacementLabels(localize, selectedCategoryId, selectedOptionId, StructureSimulationPass.ManaGeneratorBasicId, selectedPostureNameKey);
        }

        public static MinimalMvpActionPanelLabels BuildPlacementLabels(
            Func<string, string, string> localize,
            string selectedCategoryId,
            string selectedOptionId,
            string selectedStructureId,
            string selectedPostureNameKey)
        {
            string categoryLabel = string.Format(
                Localize(localize, CategoryLabelKey),
                MvpDungeonPlacementPresenter.ResolveCategoryName(selectedCategoryId, localize));
            string selectedLabel = string.Format(
                Localize(localize, SelectionLabelKey),
                MvpDungeonPlacementPresenter.ResolveOptionName(selectedOptionId, localize));
            string selectedPostureName = Localize(localize, string.IsNullOrWhiteSpace(selectedPostureNameKey) ? BalancedPostureKey : selectedPostureNameKey);
            string postureLabel = string.Format(Localize(localize, PostureLabelKey), selectedPostureName);
            string placementPreview = MvpDungeonPlacementPresenter.BuildPreviewText(selectedOptionId, localize);
            string runPlanPreview = MvpStructureImpactPreviewPresenter.BuildRunPlanPreviewText(selectedStructureId, selectedPostureNameKey, localize);
            return CreateLabels(localize, selectedLabel, categoryLabel, postureLabel, placementPreview, runPlanPreview);
        }

        public static string BuildPanelText(Func<string, string, string> localize)
        {
            return BuildPanelText(localize, ManaGeneratorSelectionKey);
        }

        public static string BuildPanelText(Func<string, string, string> localize, string selectedStructureNameKey)
        {
            MinimalMvpActionPanelLabels labels = BuildLabels(localize, selectedStructureNameKey);
            string format = Localize(localize, CompactFormatKey);
            return string.Format(format, labels.Title, labels.SelectedStructureLabel, labels.PreviewText, labels.PlacementButton, labels.RunButton);
        }

        private static MinimalMvpActionPanelLabels CreateLabels(
            Func<string, string, string> localize,
            string selectedLabel,
            string categoryLabel,
            string postureLabel,
            string previewText,
            string runPlanPreviewText)
        {
            return new MinimalMvpActionPanelLabels(
                Localize(localize, TitleKey),
                selectedLabel,
                categoryLabel,
                postureLabel,
                previewText,
                runPlanPreviewText,
                Localize(localize, CautiousPostureKey),
                Localize(localize, BalancedPostureKey),
                Localize(localize, GreedyPostureKey),
                Localize(localize, MvpDungeonPlacementPresenter.RoomCategoryKey),
                Localize(localize, MvpDungeonPlacementPresenter.MonsterCategoryKey),
                Localize(localize, MvpDungeonPlacementPresenter.TrapCategoryKey),
                Localize(localize, MvpDungeonPlacementPresenter.LootNodeCategoryKey),
                Localize(localize, MvpDungeonPlacementPresenter.BasicRoomOptionKey),
                Localize(localize, MvpDungeonPlacementPresenter.NarrowHallOptionKey),
                Localize(localize, MvpDungeonPlacementPresenter.SkeletonOptionKey),
                Localize(localize, MvpDungeonPlacementPresenter.GoblinOptionKey),
                Localize(localize, MvpDungeonPlacementPresenter.SpikeTrapOptionKey),
                Localize(localize, MvpDungeonPlacementPresenter.SnareTrapOptionKey),
                Localize(localize, MvpDungeonPlacementPresenter.BasicLootNodeOptionKey),
                Localize(localize, MvpDungeonPlacementPresenter.HiddenCacheOptionKey),
                Localize(localize, ManaGeneratorSelectionKey),
                Localize(localize, HeatScrubberSelectionKey),
                Localize(localize, RiskLabSelectionKey),
                Localize(localize, PlacementButtonKey),
                Localize(localize, RunButtonKey),
                Localize(localize, ShowDiagnosticsButtonKey),
                Localize(localize, HideDiagnosticsButtonKey),
                Localize(localize, CollapsePanelButtonKey),
                Localize(localize, ExpandPanelButtonKey));
        }

        private static string ResolveStructureIdFromSelectionKey(string selectedStructureNameKey)
        {
            if (string.IsNullOrWhiteSpace(selectedStructureNameKey))
            {
                return StructureSimulationPass.ManaGeneratorBasicId;
            }

            switch (selectedStructureNameKey)
            {
                case HeatScrubberSelectionKey:
                    return StructureSimulationPass.HeatScrubberBasicId;
                case RiskLabSelectionKey:
                    return StructureSimulationPass.RiskLabBasicId;
                case ManaGeneratorSelectionKey:
                    return StructureSimulationPass.ManaGeneratorBasicId;
                default:
                    return null;
            }
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            return localize == null ? key : localize(key, key);
        }
    }

    public struct MinimalMvpActionPanelLabels
    {
        public MinimalMvpActionPanelLabels(
            string title,
            string selectedStructureLabel,
            string categoryLabel,
            string postureLabel,
            string previewText,
            string runPlanPreviewText,
            string cautiousPosture,
            string balancedPosture,
            string greedyPosture,
            string roomCategory,
            string monsterCategory,
            string trapCategory,
            string lootNodeCategory,
            string basicRoomSelection,
            string narrowHallSelection,
            string skeletonSelection,
            string goblinSelection,
            string spikeTrapSelection,
            string snareTrapSelection,
            string basicLootNodeSelection,
            string hiddenCacheSelection,
            string manaGeneratorSelection,
            string heatScrubberSelection,
            string riskLabSelection,
            string placementButton,
            string runButton,
            string showDiagnosticsButton,
            string hideDiagnosticsButton,
            string collapsePanelButton,
            string expandPanelButton)
        {
            Title = title;
            SelectedStructureLabel = selectedStructureLabel;
            CategoryLabel = categoryLabel;
            PostureLabel = postureLabel;
            PreviewText = previewText;
            RunPlanPreviewText = runPlanPreviewText;
            CautiousPosture = cautiousPosture;
            BalancedPosture = balancedPosture;
            GreedyPosture = greedyPosture;
            RoomCategory = roomCategory;
            MonsterCategory = monsterCategory;
            TrapCategory = trapCategory;
            LootNodeCategory = lootNodeCategory;
            BasicRoomSelection = basicRoomSelection;
            NarrowHallSelection = narrowHallSelection;
            SkeletonSelection = skeletonSelection;
            GoblinSelection = goblinSelection;
            SpikeTrapSelection = spikeTrapSelection;
            SnareTrapSelection = snareTrapSelection;
            BasicLootNodeSelection = basicLootNodeSelection;
            HiddenCacheSelection = hiddenCacheSelection;
            ManaGeneratorSelection = manaGeneratorSelection;
            HeatScrubberSelection = heatScrubberSelection;
            RiskLabSelection = riskLabSelection;
            PlacementButton = placementButton;
            RunButton = runButton;
            ShowDiagnosticsButton = showDiagnosticsButton;
            HideDiagnosticsButton = hideDiagnosticsButton;
            CollapsePanelButton = collapsePanelButton;
            ExpandPanelButton = expandPanelButton;
        }

        public string Title { get; }
        public string SelectedStructureLabel { get; }
        public string CategoryLabel { get; }
        public string PostureLabel { get; }
        public string PreviewText { get; }
        public string RunPlanPreviewText { get; }
        public string CautiousPosture { get; }
        public string BalancedPosture { get; }
        public string GreedyPosture { get; }
        public string RoomCategory { get; }
        public string MonsterCategory { get; }
        public string TrapCategory { get; }
        public string LootNodeCategory { get; }
        public string BasicRoomSelection { get; }
        public string NarrowHallSelection { get; }
        public string SkeletonSelection { get; }
        public string GoblinSelection { get; }
        public string SpikeTrapSelection { get; }
        public string SnareTrapSelection { get; }
        public string BasicLootNodeSelection { get; }
        public string HiddenCacheSelection { get; }
        public string ManaGeneratorSelection { get; }
        public string HeatScrubberSelection { get; }
        public string RiskLabSelection { get; }
        public string PlacementButton { get; }
        public string RunButton { get; }
        public string ShowDiagnosticsButton { get; }
        public string HideDiagnosticsButton { get; }
        public string CollapsePanelButton { get; }
        public string ExpandPanelButton { get; }
    }
}
