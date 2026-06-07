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
