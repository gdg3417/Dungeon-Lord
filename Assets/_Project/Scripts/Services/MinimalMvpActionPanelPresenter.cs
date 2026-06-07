using System;
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
        public const string CompactFormatKey = "ui.mvp_action.panel.compact_format";
        public const string SelectionLabelKey = "ui.mvp_action.selection.label";
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
            string selectedName = Localize(localize, string.IsNullOrWhiteSpace(selectedStructureNameKey) ? ManaGeneratorSelectionKey : selectedStructureNameKey);
            string selectionFormat = Localize(localize, SelectionLabelKey);
            return new MinimalMvpActionPanelLabels(
                Localize(localize, TitleKey),
                string.Format(selectionFormat, selectedName),
                MvpStructureImpactPreviewPresenter.BuildPreviewText(selectedStructureId, localize),
                Localize(localize, ManaGeneratorSelectionKey),
                Localize(localize, HeatScrubberSelectionKey),
                Localize(localize, RiskLabSelectionKey),
                Localize(localize, PlacementButtonKey),
                Localize(localize, RunButtonKey),
                Localize(localize, ShowDiagnosticsButtonKey),
                Localize(localize, HideDiagnosticsButtonKey));
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
            if (localize == null)
            {
                return key;
            }

            return localize(key, key);
        }
    }

    public struct MinimalMvpActionPanelLabels
    {
        public MinimalMvpActionPanelLabels(
            string title,
            string selectedStructureLabel,
            string previewText,
            string manaGeneratorSelection,
            string heatScrubberSelection,
            string riskLabSelection,
            string placementButton,
            string runButton,
            string showDiagnosticsButton,
            string hideDiagnosticsButton)
        {
            Title = title;
            SelectedStructureLabel = selectedStructureLabel;
            PreviewText = previewText;
            ManaGeneratorSelection = manaGeneratorSelection;
            HeatScrubberSelection = heatScrubberSelection;
            RiskLabSelection = riskLabSelection;
            PlacementButton = placementButton;
            RunButton = runButton;
            ShowDiagnosticsButton = showDiagnosticsButton;
            HideDiagnosticsButton = hideDiagnosticsButton;
        }

        public string Title { get; }
        public string SelectedStructureLabel { get; }
        public string PreviewText { get; }
        public string ManaGeneratorSelection { get; }
        public string HeatScrubberSelection { get; }
        public string RiskLabSelection { get; }
        public string PlacementButton { get; }
        public string RunButton { get; }
        public string ShowDiagnosticsButton { get; }
        public string HideDiagnosticsButton { get; }
    }
}
