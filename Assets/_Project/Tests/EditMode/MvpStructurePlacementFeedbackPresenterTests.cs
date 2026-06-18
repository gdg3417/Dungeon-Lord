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

        [Test]
        public void RoomTargetedPlacementFeedback_UsesRoomCategoryAndEmptyValue()
        {
            string text = MvpStructurePlacementFeedbackPresenter.BuildRoomTargetedPlacementFeedbackText(
                1,
                DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.MvpDungeonPlacementIds.LootNodeCategoryId,
                string.Empty,
                DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.MvpDungeonPlacementIds.BasicLootNodeOptionId,
                Localized);

            Assert.That(text, Is.EqualTo("Changed Room 2 Loot node: Empty -> Basic Loot Node."));
            Assert.That(text, Does.Not.Contain(DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.MvpDungeonPlacementIds.BasicLootNodeOptionId));
        }


        [Test]
        public void RoomTargetedPlacementFeedback_WhenPriorAndNewOptionMatch_UsesAlreadySetFormat()
        {
            string text = MvpStructurePlacementFeedbackPresenter.BuildRoomTargetedPlacementFeedbackText(
                0,
                DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.MvpDungeonPlacementIds.MonsterCategoryId,
                DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.MvpDungeonPlacementIds.SkeletonOptionId,
                DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.MvpDungeonPlacementIds.SkeletonOptionId,
                Localized);

            Assert.That(text, Is.EqualTo("Room 1 Monster already set to Skeleton."));
            Assert.That(text, Does.Not.Contain("Changed Room"));
            Assert.That(text, Does.Not.Contain(DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.MvpDungeonPlacementIds.SkeletonOptionId));
        }

        [Test]
        public void RoomTargetedPlacementFeedback_WhenPriorAndNewOptionDiffer_UsesChangedFormat()
        {
            string text = MvpStructurePlacementFeedbackPresenter.BuildRoomTargetedPlacementFeedbackText(
                0,
                DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.MvpDungeonPlacementIds.MonsterCategoryId,
                DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.MvpDungeonPlacementIds.GoblinOptionId,
                DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.MvpDungeonPlacementIds.SkeletonOptionId,
                Localized);

            Assert.That(text, Is.EqualTo("Changed Room 1 Monster: Goblin -> Skeleton."));
            Assert.That(text, Does.Not.Contain(DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.MvpDungeonPlacementIds.SkeletonOptionId));
        }

        private static string Localized(string key, string fallback)
        {
            var map = new Dictionary<string, string>
            {
                [MvpStructurePlacementFeedbackPresenter.EmptySlotKey] = "Empty slot",
                [MvpStructurePlacementFeedbackPresenter.EmptyPlacementValueKey] = "Empty",
                [MvpStructurePlacementFeedbackPresenter.RoomTargetedPlacementChangedFormatKey] = "Changed Room {0} {1}: {2} -> {3}.",
                [MvpStructurePlacementFeedbackPresenter.RoomTargetedPlacementAlreadySetFormatKey] = "Room {0} {1} already set to {2}.",
                [MvpStructurePlacementFeedbackPresenter.ChangedFormatKey] = "Changed: {0} -> {1}. {2}",
                ["structure.mana_generator.basic.display_name"] = "Mana Generator",
                ["structure.heat_scrubber.basic.display_name"] = "Heat Scrubber",
                ["structure.risk_lab.basic.display_name"] = "Risk Lab",
                [MvpPlayerFacingLabelResolver.UnknownStructureKey] = "Unknown structure",
                [MvpStructureImpactPreviewPresenter.ManaGeneratorPreviewKey] = "Role: improves mana reserve.",
                [MvpStructureImpactPreviewPresenter.HeatScrubberPreviewKey] = "Role: lowers heat pressure.",
                [MvpStructureImpactPreviewPresenter.RiskLabPreviewKey] = "Role: clarifies research risk.",
                [MvpStructureImpactPreviewPresenter.UnknownPreviewKey] = "Role unavailable.",
                [MvpDungeonPlacementPresenter.LootNodeCategoryKey] = "Loot node",
                [MvpDungeonPlacementPresenter.MonsterCategoryKey] = "Monster",
                [MvpDungeonPlacementPresenter.BasicLootNodeOptionKey] = "Basic Loot Node",
                [MvpDungeonPlacementPresenter.SkeletonOptionKey] = "Skeleton",
                [MvpDungeonPlacementPresenter.GoblinOptionKey] = "Goblin"
            };

            return map.TryGetValue(key, out string value) ? value : fallback;
        }
    }
}
