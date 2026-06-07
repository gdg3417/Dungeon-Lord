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
                        ? "Preview: improves mana reserve for future runs."
                        : fallback;
                });

            Assert.That(text, Is.EqualTo("Preview: improves mana reserve for future runs."));
            Assert.That(requestedKeys, Does.Contain(MvpStructureImpactPreviewPresenter.ManaGeneratorPreviewKey));
        }

        [Test]
        public void HeatScrubberPreview_ResolvesThroughLocalization()
        {
            string text = MvpStructureImpactPreviewPresenter.BuildPreviewText(
                StructureSimulationPass.HeatScrubberBasicId,
                (key, fallback) => key == MvpStructureImpactPreviewPresenter.HeatScrubberPreviewKey
                    ? "Preview: helps reduce heat pressure after runs."
                    : fallback);

            Assert.That(text, Is.EqualTo("Preview: helps reduce heat pressure after runs."));
        }

        [Test]
        public void RiskLabPreview_ResolvesThroughLocalization()
        {
            string text = MvpStructureImpactPreviewPresenter.BuildPreviewText(
                StructureSimulationPass.RiskLabBasicId,
                (key, fallback) => key == MvpStructureImpactPreviewPresenter.RiskLabPreviewKey
                    ? "Preview: supports research visibility and risk analysis."
                    : fallback);

            Assert.That(text, Is.EqualTo("Preview: supports research visibility and risk analysis."));
        }

        [Test]
        public void UnknownStructurePreview_UsesLocalizedFallback()
        {
            string text = MvpStructureImpactPreviewPresenter.BuildPreviewText(
                "structure.debug.not_player_facing",
                (key, fallback) => key == MvpStructureImpactPreviewPresenter.UnknownPreviewKey
                    ? "Preview unavailable for this structure."
                    : fallback);

            Assert.That(text, Is.EqualTo("Preview unavailable for this structure."));
        }

        [TestCase(StructureSimulationPass.ManaGeneratorBasicId, "Preview: improves mana reserve for future runs.")]
        [TestCase(StructureSimulationPass.HeatScrubberBasicId, "Preview: helps reduce heat pressure after runs.")]
        [TestCase(StructureSimulationPass.RiskLabBasicId, "Preview: supports research visibility and risk analysis.")]
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
