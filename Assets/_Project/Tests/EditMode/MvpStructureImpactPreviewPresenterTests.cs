#if UNITY_EDITOR
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using System.Collections.Generic;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpStructureImpactPreviewPresenterTests
    {
        [Test]
        public void ManaGeneratorPreview_ResolvesThroughLocalization()
        {
            var requestedKeys = new List<string>();

            string text = MvpStructureImpactPreviewPresenter.BuildPreviewText(
                StructureSimulationPass.ManaGeneratorBasicId,
                (key, fallback) =>
                {
                    requestedKeys.Add(key);
                    return key == MvpStructureImpactPreviewPresenter.ManaGeneratorPreviewKey
                        ? "Role: improves mana reserve."
                        : fallback;
                });

            Assert.That(text, Is.EqualTo("Role: improves mana reserve."));
            Assert.That(requestedKeys, Does.Contain(MvpStructureImpactPreviewPresenter.ManaGeneratorPreviewKey));
        }

        [Test]
        public void HeatScrubberPreview_ResolvesThroughLocalization()
        {
            string text = MvpStructureImpactPreviewPresenter.BuildPreviewText(
                StructureSimulationPass.HeatScrubberBasicId,
                (key, fallback) => key == MvpStructureImpactPreviewPresenter.HeatScrubberPreviewKey
                    ? "Role: lowers heat pressure."
                    : fallback);

            Assert.That(text, Is.EqualTo("Role: lowers heat pressure."));
        }

        [Test]
        public void RiskLabPreview_ResolvesThroughLocalization()
        {
            string text = MvpStructureImpactPreviewPresenter.BuildPreviewText(
                StructureSimulationPass.RiskLabBasicId,
                (key, fallback) => key == MvpStructureImpactPreviewPresenter.RiskLabPreviewKey
                    ? "Role: clarifies research risk."
                    : fallback);

            Assert.That(text, Is.EqualTo("Role: clarifies research risk."));
        }

        [Test]
        public void UnknownStructurePreview_UsesLocalizedFallback()
        {
            string text = MvpStructureImpactPreviewPresenter.BuildPreviewText(
                "structure.debug.not_player_facing",
                (key, fallback) => key == MvpStructureImpactPreviewPresenter.UnknownPreviewKey
                    ? "Role unavailable."
                    : fallback);

            Assert.That(text, Is.EqualTo("Role unavailable."));
        }

        [TestCase(StructureSimulationPass.ManaGeneratorBasicId, "Role: improves mana reserve.")]
        [TestCase(StructureSimulationPass.HeatScrubberBasicId, "Role: lowers heat pressure.")]
        [TestCase(StructureSimulationPass.RiskLabBasicId, "Role: clarifies research risk.")]
        public void PreviewText_DoesNotExposeRawStructureIds(string structureId, string localizedText)
        {
            string text = MvpStructureImpactPreviewPresenter.BuildPreviewText(
                structureId,
                (key, fallback) => localizedText);

            Assert.That(text, Does.Not.Contain(structureId));
        }


        [TestCase(MinimalMvpActionPanelPresenter.BalancedPostureKey, "Plan: Mana Generator + Balanced adventurer challenge.\nExpected tradeoff: standard loot and heat pressure.")]
        [TestCase(MinimalMvpActionPanelPresenter.CautiousPostureKey, "Plan: Mana Generator + Cautious adventurer challenge.\nExpected tradeoff: lower loot, safer heat pressure.")]
        [TestCase(MinimalMvpActionPanelPresenter.GreedyPostureKey, "Plan: Mana Generator + Greedy adventurer challenge.\nExpected tradeoff: higher loot, higher heat pressure.")]
        public void RunPlanPreview_ResolvesPostureTradeoffThroughLocalization(string postureNameKey, string expected)
        {
            string text = MvpStructureImpactPreviewPresenter.BuildRunPlanPreviewText(
                StructureSimulationPass.ManaGeneratorBasicId,
                postureNameKey,
                LocalizedRunPlan);

            Assert.That(text, Is.EqualTo(expected));
        }

        [Test]
        public void RunPlanPreview_DoesNotExposeRawStructureOrPostureIds()
        {
            string text = MvpStructureImpactPreviewPresenter.BuildRunPlanPreviewText(
                StructureSimulationPass.ManaGeneratorBasicId,
                MinimalMvpActionPanelPresenter.GreedyPostureKey,
                LocalizedRunPlan);

            Assert.That(text, Does.Not.Contain(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(text, Does.Not.Contain(RunPostureResolver.GreedyId));
            Assert.That(text, Does.Not.Contain("structure."));
            Assert.That(text, Does.Not.Contain("run.posture"));
        }

        private static string LocalizedRunPlan(string key, string fallback)
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
                ["ui.mvp_label.structure.unknown"] = "Unknown structure"
            };

            return map.TryGetValue(key, out string value) ? value : fallback;
        }

        [Test]
        public void NullLocalizer_FallsBackToStableLocalizationKey()
        {
            string text = MvpStructureImpactPreviewPresenter.BuildPreviewText(
                StructureSimulationPass.ManaGeneratorBasicId,
                null);

            Assert.That(text, Is.EqualTo(MvpStructureImpactPreviewPresenter.ManaGeneratorPreviewKey));
        }
    }
}
#endif
