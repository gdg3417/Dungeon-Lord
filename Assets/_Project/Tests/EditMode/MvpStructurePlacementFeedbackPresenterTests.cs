using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using System.Collections.Generic;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpStructurePlacementFeedbackPresenterTests
    {
        [Test]
        public void EmptySlotToManaGenerator_UsesLocalizedLabelsAndPreviewRole()
        {
            var requestedKeys = new List<string>();

            string text = MvpStructurePlacementFeedbackPresenter.BuildFeedbackText(
                string.Empty,
                StructureSimulationPass.ManaGeneratorBasicId,
                StructureSimulationPass.ManaGeneratorBasicId,
                (key, fallback) =>
                {
                    requestedKeys.Add(key);
                    return Localized(key, fallback);
                });

            Assert.That(text, Is.EqualTo("Changed: Empty slot -> Mana Generator. Role: improves mana reserve."));
            Assert.That(requestedKeys, Does.Contain(MvpStructurePlacementFeedbackPresenter.EmptySlotKey));
            Assert.That(requestedKeys, Does.Contain(MvpStructurePlacementFeedbackPresenter.ChangedFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpStructureImpactPreviewPresenter.ManaGeneratorPreviewKey));
        }

        [Test]
        public void ManaGeneratorToHeatScrubber_UsesLocalizedLabelsAndPreviewRole()
        {
            string text = MvpStructurePlacementFeedbackPresenter.BuildFeedbackText(
                StructureSimulationPass.ManaGeneratorBasicId,
                StructureSimulationPass.HeatScrubberBasicId,
                StructureSimulationPass.HeatScrubberBasicId,
                Localized);

            Assert.That(text, Is.EqualTo("Changed: Mana Generator -> Heat Scrubber. Role: lowers heat pressure."));
        }

        [Test]
        public void HeatScrubberToRiskLab_UsesLocalizedLabelsAndPreviewRole()
        {
            string text = MvpStructurePlacementFeedbackPresenter.BuildFeedbackText(
                StructureSimulationPass.HeatScrubberBasicId,
                StructureSimulationPass.RiskLabBasicId,
                StructureSimulationPass.RiskLabBasicId,
                Localized);

            Assert.That(text, Is.EqualTo("Changed: Heat Scrubber -> Risk Lab. Role: clarifies research risk."));
        }

        [Test]
        public void Feedback_DoesNotExposeRawStructureIds()
        {
            string text = MvpStructurePlacementFeedbackPresenter.BuildFeedbackText(
                StructureSimulationPass.ManaGeneratorBasicId,
                StructureSimulationPass.HeatScrubberBasicId,
                StructureSimulationPass.HeatScrubberBasicId,
                Localized);

            Assert.That(text, Does.Not.Contain(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.HeatScrubberBasicId));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("structure.debug.not_player_facing")]
        public void EmptyOrUnknownPriorPlacement_UsesSafeLocalizedEmptySlotFallback(string priorStructureId)
        {
            string text = MvpStructurePlacementFeedbackPresenter.BuildFeedbackText(
                priorStructureId,
                StructureSimulationPass.ManaGeneratorBasicId,
                StructureSimulationPass.ManaGeneratorBasicId,
                Localized);

            Assert.That(text, Does.StartWith("Changed: Empty slot -> Mana Generator."));
            Assert.That(text, Does.Not.Contain("structure.debug.not_player_facing"));
        }

        [Test]
        public void NullLocalizer_FallsBackToStableLocalizationKeys()
        {
            string text = MvpStructurePlacementFeedbackPresenter.BuildFeedbackText(
                string.Empty,
                StructureSimulationPass.ManaGeneratorBasicId,
                StructureSimulationPass.ManaGeneratorBasicId,
                null);

            Assert.That(text, Is.EqualTo(MvpStructurePlacementFeedbackPresenter.ChangedFormatKey));
        }

        private static string Localized(string key, string fallback)
        {
            var map = new Dictionary<string, string>
            {
                [MvpStructurePlacementFeedbackPresenter.EmptySlotKey] = "Empty slot",
                [MvpStructurePlacementFeedbackPresenter.ChangedFormatKey] = "Changed: {0} -> {1}. {2}",
                ["structure.mana_generator.basic.display_name"] = "Mana Generator",
                ["structure.heat_scrubber.basic.display_name"] = "Heat Scrubber",
                ["structure.risk_lab.basic.display_name"] = "Risk Lab",
                [MvpPlayerFacingLabelResolver.UnknownStructureKey] = "Unknown structure",
                [MvpStructureImpactPreviewPresenter.ManaGeneratorPreviewKey] = "Role: improves mana reserve.",
                [MvpStructureImpactPreviewPresenter.HeatScrubberPreviewKey] = "Role: lowers heat pressure.",
                [MvpStructureImpactPreviewPresenter.RiskLabPreviewKey] = "Role: clarifies research risk.",
                [MvpStructureImpactPreviewPresenter.UnknownPreviewKey] = "Role unavailable."
            };

            return map.TryGetValue(key, out string value) ? value : fallback;
        }
    }
}
