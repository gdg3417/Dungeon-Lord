using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using System.Collections.Generic;

namespace DungeonBuilder.Tests.EditMode
{
    public class MinimalMvpActionPanelPresenterTests
    {
        [Test]
        public void BuildPanelText_UsesLocalizationKeysForTitleSelectionPlanPreviewAndButtons()
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
                if (key == MvpStructureImpactPreviewPresenter.RunPlanFormatKey)
                {
                    return "Plan={0}+{1}";
                }
                if (key == MvpStructureImpactPreviewPresenter.RunTradeoffFormatKey)
                {
                    return "Tradeoff={0}";
                }
                if (key == MvpStructureImpactPreviewPresenter.RunPlanCombinedFormatKey)
                {
                    return "{0}/{1}";
                }
                if (key == MvpStructureImpactPreviewPresenter.BalancedRunTradeoffKey)
                {
                    return "balanced-tradeoff";
                }

                return "LOC[" + key + "]";
            }, MinimalMvpActionPanelPresenter.HeatScrubberSelectionKey);

            Assert.That(text, Is.EqualTo("LOC[ui.mvp_action.panel.title]|Selected=LOC[ui.mvp_action.selection.heat_scrubber]|Plan=LOC[structure.heat_scrubber.basic.display_name]+LOC[run.posture.balanced.name]/Tradeoff=balanced-tradeoff|LOC[ui.mvp_action.button.place_or_modify]|LOC[ui.mvp_action.button.run_or_observe]"));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.TitleKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.SelectionLabelKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.PostureLabelKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.BalancedPostureKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.CautiousPostureKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.GreedyPostureKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.RoomsGroupHeaderKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.MonstersGroupHeaderKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.TrapsGroupHeaderKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.LootGroupHeaderKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.ManaGeneratorSelectionKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.HeatScrubberSelectionKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.RiskLabSelectionKey));
            Assert.That(requestedKeys, Does.Contain("structure.heat_scrubber.basic.display_name"));
            Assert.That(requestedKeys, Does.Contain(MvpStructureImpactPreviewPresenter.RunPlanFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpStructureImpactPreviewPresenter.RunTradeoffFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpStructureImpactPreviewPresenter.RunPlanCombinedFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpStructureImpactPreviewPresenter.BalancedRunTradeoffKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.PlacementButtonKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.RunButtonKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.ShowDiagnosticsButtonKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.HideDiagnosticsButtonKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.CompactFormatKey));
        }

        [Test]
        public void BuildLabels_UsesLocalizedPlanPreviewForSelectedPosture()
        {
            MinimalMvpActionPanelLabels labels = MinimalMvpActionPanelPresenter.BuildLabels(
                Localized,
                MinimalMvpActionPanelPresenter.ManaGeneratorSelectionKey,
                StructureSimulationPass.ManaGeneratorBasicId,
                MinimalMvpActionPanelPresenter.CautiousPostureKey);

            Assert.That(labels.PreviewText, Is.EqualTo("Plan: Mana Generator + Cautious adventurer challenge.\nExpected tradeoff: lower loot, safer heat pressure."));
        }

        [Test]
        public void BuildLabels_NullLocalizerFallsBackToStableKeys()
        {
            MinimalMvpActionPanelLabels labels = MinimalMvpActionPanelPresenter.BuildLabels(null);

            Assert.That(labels.Title, Is.EqualTo(MinimalMvpActionPanelPresenter.TitleKey));
            Assert.That(labels.SelectedStructureLabel, Is.EqualTo(MinimalMvpActionPanelPresenter.SelectionLabelKey));
            Assert.That(labels.PostureLabel, Is.EqualTo(MinimalMvpActionPanelPresenter.PostureLabelKey));
            Assert.That(labels.PreviewText, Is.EqualTo(MvpStructureImpactPreviewPresenter.RunPlanCombinedFormatKey));
            Assert.That(labels.CautiousPosture, Is.EqualTo(MinimalMvpActionPanelPresenter.CautiousPostureKey));
            Assert.That(labels.BalancedPosture, Is.EqualTo(MinimalMvpActionPanelPresenter.BalancedPostureKey));
            Assert.That(labels.GreedyPosture, Is.EqualTo(MinimalMvpActionPanelPresenter.GreedyPostureKey));
            Assert.That(labels.RoomsGroupHeader, Is.EqualTo(MinimalMvpActionPanelPresenter.RoomsGroupHeaderKey));
            Assert.That(labels.MonstersGroupHeader, Is.EqualTo(MinimalMvpActionPanelPresenter.MonstersGroupHeaderKey));
            Assert.That(labels.TrapsGroupHeader, Is.EqualTo(MinimalMvpActionPanelPresenter.TrapsGroupHeaderKey));
            Assert.That(labels.LootGroupHeader, Is.EqualTo(MinimalMvpActionPanelPresenter.LootGroupHeaderKey));
            Assert.That(labels.ManaGeneratorSelection, Is.EqualTo(MinimalMvpActionPanelPresenter.ManaGeneratorSelectionKey));
            Assert.That(labels.HeatScrubberSelection, Is.EqualTo(MinimalMvpActionPanelPresenter.HeatScrubberSelectionKey));
            Assert.That(labels.RiskLabSelection, Is.EqualTo(MinimalMvpActionPanelPresenter.RiskLabSelectionKey));
            Assert.That(labels.PlacementButton, Is.EqualTo(MinimalMvpActionPanelPresenter.PlacementButtonKey));
            Assert.That(labels.RunButton, Is.EqualTo(MinimalMvpActionPanelPresenter.RunButtonKey));
            Assert.That(labels.ShowDiagnosticsButton, Is.EqualTo(MinimalMvpActionPanelPresenter.ShowDiagnosticsButtonKey));
            Assert.That(labels.HideDiagnosticsButton, Is.EqualTo(MinimalMvpActionPanelPresenter.HideDiagnosticsButtonKey));
        }

        [Test]
        public void BuildPlacementLabels_IncludesLocalizedComparisonTextWhenProvided()
        {
            MinimalMvpActionPanelLabels labels = MinimalMvpActionPanelPresenter.BuildPlacementLabels(
                Localized,
                DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.MvpDungeonPlacementIds.RoomCategoryId,
                DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.MvpDungeonPlacementIds.NarrowHallOptionId,
                StructureSimulationPass.ManaGeneratorBasicId,
                MinimalMvpActionPanelPresenter.BalancedPostureKey,
                "Compared with Basic Room: lower path capacity, better as a connector.");

            Assert.That(labels.ComparisonText, Is.EqualTo("Compared with Basic Room: lower path capacity, better as a connector."));
            Assert.That(labels.ComparisonText, Does.Not.Contain("placement.option"));
            Assert.That(labels.ComparisonText, Does.Not.Contain("ui.mvp"));
        }

        private static string Localized(string key, string fallback)
        {
            var map = new Dictionary<string, string>
            {
                [MvpStructureImpactPreviewPresenter.RunPlanFormatKey] = "Plan: {0} + {1} adventurer challenge.",
                [MvpStructureImpactPreviewPresenter.RunTradeoffFormatKey] = "Expected tradeoff: {0}",
                [MvpStructureImpactPreviewPresenter.RunPlanCombinedFormatKey] = "{0}\n{1}",
                [MvpStructureImpactPreviewPresenter.CautiousRunTradeoffKey] = "lower loot, safer heat pressure.",
                [MvpStructureImpactPreviewPresenter.BalancedRunTradeoffKey] = "standard loot and heat pressure.",
                [MvpStructureImpactPreviewPresenter.GreedyRunTradeoffKey] = "higher loot, higher heat pressure.",
                [MinimalMvpActionPanelPresenter.CautiousPostureKey] = "Cautious",
                [MinimalMvpActionPanelPresenter.BalancedPostureKey] = "Balanced",
                [MinimalMvpActionPanelPresenter.GreedyPostureKey] = "Greedy",
                ["structure.mana_generator.basic.display_name"] = "Mana Generator",
                ["structure.heat_scrubber.basic.display_name"] = "Heat Scrubber",
                ["structure.risk_lab.basic.display_name"] = "Risk Lab"
            };

            return map.TryGetValue(key, out string value) ? value : fallback;
        }
    }
}
