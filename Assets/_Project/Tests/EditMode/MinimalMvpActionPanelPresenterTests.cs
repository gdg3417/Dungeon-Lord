using DungeonBuilder.M0;
using NUnit.Framework;
using System.Collections.Generic;

namespace DungeonBuilder.Tests.EditMode
{
    public class MinimalMvpActionPanelPresenterTests
    {
        [Test]
        public void BuildPanelText_UsesLocalizationKeysForTitleSelectionAndButtons()
        {
            var requestedKeys = new List<string>();

            string text = MinimalMvpActionPanelPresenter.BuildPanelText((key, fallback) =>
            {
                requestedKeys.Add(key);
                if (key == MinimalMvpActionPanelPresenter.CompactFormatKey)
                {
                    return "{0}|{1}|{2}|{3}|{4}";
                }
                if (key == MinimalMvpActionPanelPresenter.SelectionLabelKey)
                {
                    return "Selected={0}";
                }
                if (key == MinimalMvpActionPanelPresenter.PostureLabelKey)
                {
                    return "Posture={0}";
                }

                return "LOC[" + key + "]";
            }, MinimalMvpActionPanelPresenter.HeatScrubberSelectionKey);

            Assert.That(text, Is.EqualTo("LOC[ui.mvp_action.panel.title]|Selected=LOC[ui.mvp_action.selection.heat_scrubber]|LOC[ui.mvp_structure_preview.heat_scrubber]|LOC[ui.mvp_action.button.place_or_modify]|LOC[ui.mvp_action.button.run_or_observe]"));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.TitleKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.SelectionLabelKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.PostureLabelKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.BalancedPostureKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.CautiousPostureKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.GreedyPostureKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.ManaGeneratorSelectionKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.HeatScrubberSelectionKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.RiskLabSelectionKey));
            Assert.That(requestedKeys, Does.Contain(MvpStructureImpactPreviewPresenter.HeatScrubberPreviewKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.PlacementButtonKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.RunButtonKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.ShowDiagnosticsButtonKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.HideDiagnosticsButtonKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.CompactFormatKey));
        }

        [Test]
        public void BuildLabels_NullLocalizerFallsBackToStableKeys()
        {
            MinimalMvpActionPanelLabels labels = MinimalMvpActionPanelPresenter.BuildLabels(null);

            Assert.That(labels.Title, Is.EqualTo(MinimalMvpActionPanelPresenter.TitleKey));
            Assert.That(labels.SelectedStructureLabel, Is.EqualTo(MinimalMvpActionPanelPresenter.SelectionLabelKey));
            Assert.That(labels.PostureLabel, Is.EqualTo(MinimalMvpActionPanelPresenter.PostureLabelKey));
            Assert.That(labels.PreviewText, Is.EqualTo(MvpStructureImpactPreviewPresenter.ManaGeneratorPreviewKey));
            Assert.That(labels.CautiousPosture, Is.EqualTo(MinimalMvpActionPanelPresenter.CautiousPostureKey));
            Assert.That(labels.BalancedPosture, Is.EqualTo(MinimalMvpActionPanelPresenter.BalancedPostureKey));
            Assert.That(labels.GreedyPosture, Is.EqualTo(MinimalMvpActionPanelPresenter.GreedyPostureKey));
            Assert.That(labels.ManaGeneratorSelection, Is.EqualTo(MinimalMvpActionPanelPresenter.ManaGeneratorSelectionKey));
            Assert.That(labels.HeatScrubberSelection, Is.EqualTo(MinimalMvpActionPanelPresenter.HeatScrubberSelectionKey));
            Assert.That(labels.RiskLabSelection, Is.EqualTo(MinimalMvpActionPanelPresenter.RiskLabSelectionKey));
            Assert.That(labels.PlacementButton, Is.EqualTo(MinimalMvpActionPanelPresenter.PlacementButtonKey));
            Assert.That(labels.RunButton, Is.EqualTo(MinimalMvpActionPanelPresenter.RunButtonKey));
            Assert.That(labels.ShowDiagnosticsButton, Is.EqualTo(MinimalMvpActionPanelPresenter.ShowDiagnosticsButtonKey));
            Assert.That(labels.HideDiagnosticsButton, Is.EqualTo(MinimalMvpActionPanelPresenter.HideDiagnosticsButtonKey));
        }
    }
}
