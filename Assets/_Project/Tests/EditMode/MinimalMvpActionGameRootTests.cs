using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.Structures;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class MinimalMvpActionGameRootTests
    {
        [Test]
        public void TryMvpPlaceOrModifySelectedStructure_ReusesPlacementPathAndAllowsSelectedSlotReplacement()
        {
            var go = new GameObject("GameRoot_MvpActionPlacement_Test");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SaveData save = new SaveData
                {
                    dungeonLayout = DungeonLayoutState.CreateEmpty(1, 1),
                    structureRuntime = new StructureRuntimeState()
                };
                new PlacementService().PlaceStructure(save.dungeonLayout, 0, 0, StructureSimulationPass.HeatScrubberBasicId);
                typeof(GameRoot).GetProperty(nameof(GameRoot.Save))?.SetValue(root, save);

                bool devPanelPlacement = root.TryPlaceSelectedStructure(StructureSimulationPass.ManaGeneratorBasicId, out string devBannerKey);
                bool playerPlacement = root.TryMvpPlaceOrModifySelectedStructure(StructureSimulationPass.ManaGeneratorBasicId, out string playerBannerKey);

                Assert.That(devPanelPlacement, Is.False);
                Assert.That(devBannerKey, Is.EqualTo("ui.banner.place_failed"));
                Assert.That(playerPlacement, Is.True);
                Assert.That(playerBannerKey, Is.EqualTo("ui.banner.place_success"));
                Assert.That(root.GetSelectedSlotStructureId(), Is.EqualTo(StructureSimulationPass.ManaGeneratorBasicId));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TryMvpPlaceOrModifySelectedPlacement_RoutesBasicRoomAndContentsIntoRoomSlotLayout()
        {
            var go = new GameObject("GameRoot_MvpActionRoomSlots_Test");
            try
            {
                var root = go.AddComponent<GameRoot>();
                var save = new SaveData();
                typeof(GameRoot).GetProperty(nameof(GameRoot.Save))?.SetValue(root, save);

                AssertPlacement(root, MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId);
                AssertPlacement(root, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
                AssertPlacement(root, MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SnareTrapOptionId);
                AssertPlacement(root, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);

                string text = MvpDungeonLayoutPresenter.BuildLayoutText(save, RoomSlotConfig(), Localize);

                Assert.That(text, Does.Contain("Dungeon layout: Floor 0: Room: Basic Room -> Monster: Goblin -> Trap: Snare Trap -> Loot node: Hidden Cache"));
                Assert.That(text, Does.Contain("Room slot layout: Floor 0: Room 1: Basic Room (Monsters: Goblin 1/1; Traps: Snare Trap 1/1; Loot: Hidden Cache 1/1)"));
                Assert.That(text, Does.Not.Contain("placement.option"));
                Assert.That(text, Does.Not.Contain("ui.mvp_room_slots"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TryMvpPlaceOrModifySelectedPlacement_NarrowHallLootUsesDeterministicBasicRoomFallback()
        {
            var go = new GameObject("GameRoot_MvpActionNarrowHallLoot_Test");
            try
            {
                var root = go.AddComponent<GameRoot>();
                var save = new SaveData();
                typeof(GameRoot).GetProperty(nameof(GameRoot.Save))?.SetValue(root, save);

                AssertPlacement(root, MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId);
                AssertPlacement(root, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);

                string text = MvpDungeonLayoutPresenter.BuildLayoutText(save, RoomSlotConfig(), Localize);

                Assert.That(text, Does.Contain("Room 1: Narrow Hall (Monsters: empty 0/1; Traps: empty 0/1; Loot: unavailable 0/0)"));
                Assert.That(text, Does.Contain("Room 2: Basic Room (Monsters: empty 0/1; Traps: empty 0/1; Loot: Hidden Cache 1/1)"));
                Assert.That(text, Does.Not.Contain("placement.option"));
                Assert.That(text, Does.Not.Contain("ui.mvp_room_slots"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void AssertPlacement(GameRoot root, string categoryId, string optionId)
        {
            bool placed = root.TryMvpPlaceOrModifySelectedPlacement(categoryId, optionId, out _, out MvpDungeonPlacementEntry newEntry, out string bannerKey);

            Assert.That(placed, Is.True);
            Assert.That(bannerKey, Is.EqualTo("ui.banner.place_success"));
            Assert.That(newEntry, Is.Not.Null);
            Assert.That(newEntry.CategoryId, Is.EqualTo(categoryId));
            Assert.That(newEntry.OptionId, Is.EqualTo(optionId));
        }

        private static RunSimulationConfig RoomSlotConfig()
        {
            return new RunSimulationConfig
            {
                MvpRoomSlotCapacities = new[]
                {
                    new MvpRoomSlotCapacityConfig { RoomOptionId = MvpDungeonPlacementIds.NarrowHallOptionId, MonsterCapacity = 1, TrapCapacity = 1, LootCapacity = 0 },
                    new MvpRoomSlotCapacityConfig { RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId, MonsterCapacity = 1, TrapCapacity = 1, LootCapacity = 1 }
                }
            };
        }

        private static string Localize(string key, string fallback)
        {
            switch (key)
            {
                case MvpDungeonLayoutPresenter.LayoutFormatKey: return "Dungeon layout: {0}";
                case MvpDungeonLayoutPresenter.FloorFormatKey: return "Floor {0}: {1}";
                case MvpDungeonLayoutPresenter.AssignedNodeFormatKey: return "{0}: {1}";
                case MvpDungeonLayoutPresenter.EmptyNodeFormatKey: return "{0}: {1}";
                case MvpDungeonLayoutPresenter.NodeSeparatorKey: return " -> ";
                case MvpDungeonLayoutPresenter.EmptyAvailableKey: return "Empty / available";
                case MvpDungeonLayoutPresenter.RoomSlotLayoutFormatKey: return "Room slot layout: {0}";
                case MvpRoomSlotTargetPresenter.SelectedTargetFormatKey: return "Selected room target: Room {0}: {1}";
                case MvpRoomSlotTargetPresenter.SelectedCapacityFormatKey: return "Selected room capacity: {0}";
                case MvpRoomSlotTargetPresenter.SelectedCapacityCategoryFormatKey: return "{0} {1}/{2}";
                case MvpRoomSlotTargetPresenter.SelectedCapacityUnavailableCategoryFormatKey: return "{0} unavailable {1}/{2}";
                case MvpRoomSlotTargetPresenter.SelectedCapacitySeparatorKey: return "; ";
                case MvpRoomSlotTargetPresenter.SelectedPlacementFitFormatKey: return "Selected placement fit: {0}";
                case MvpRoomSlotTargetPresenter.SelectedPlacementFitsFormatKey: return "{0} fits Room {1}.";
                case MvpRoomSlotTargetPresenter.SelectedPlacementCannotFitNoSlotFormatKey: return "{0} cannot fit Room {1} because this room has no {2}.";
                case MvpRoomSlotTargetPresenter.CapacityMonstersLabelKey: return "Monsters";
                case MvpRoomSlotTargetPresenter.CapacityTrapsLabelKey: return "Traps";
                case MvpRoomSlotTargetPresenter.CapacityLootLabelKey: return "Loot";
                case MvpRoomSlotTargetPresenter.LootSlotReasonLabelKey: return "loot slot";
                case MvpRoomSlotTargetPresenter.NoValidSlotFormatKey: return "No valid {0} slot in Room {1}: {2}.";
                case MvpDungeonLayoutPresenter.RoomSlotFloorFormatKey: return "Floor {0}: {1}";
                case MvpDungeonLayoutPresenter.RoomSlotRoomFormatKey: return "Room {0}: {1} ({2}; {3}; {4})";
                case MvpDungeonLayoutPresenter.MonstersFormatKey: return "Monsters: {0} {1}/{2}";
                case MvpDungeonLayoutPresenter.TrapsFormatKey: return "Traps: {0} {1}/{2}";
                case MvpDungeonLayoutPresenter.LootFormatKey: return "Loot: {0} {1}/{2}";
                case MvpDungeonLayoutPresenter.EmptyAssignmentKey: return "empty";
                case MvpDungeonLayoutPresenter.UnavailableAssignmentKey: return "unavailable";
                case MvpDungeonLayoutPresenter.AssignmentSeparatorKey: return ", ";
                case MvpDungeonLayoutPresenter.RoomSlotSeparatorKey: return " | ";
                case MvpDungeonPlacementPresenter.RoomCategoryKey: return "Room";
                case MvpDungeonPlacementPresenter.MonsterCategoryKey: return "Monster";
                case MvpDungeonPlacementPresenter.TrapCategoryKey: return "Trap";
                case MvpDungeonPlacementPresenter.LootNodeCategoryKey: return "Loot node";
                case MvpDungeonPlacementPresenter.BasicRoomOptionKey: return "Basic Room";
                case MvpDungeonPlacementPresenter.NarrowHallOptionKey: return "Narrow Hall";
                case MvpDungeonPlacementPresenter.SkeletonOptionKey: return "Skeleton";
                case MvpDungeonPlacementPresenter.GoblinOptionKey: return "Goblin";
                case MvpDungeonPlacementPresenter.SpikeTrapOptionKey: return "Spike Trap";
                case MvpDungeonPlacementPresenter.SnareTrapOptionKey: return "Snare Trap";
                case MvpDungeonPlacementPresenter.BasicLootNodeOptionKey: return "Basic Loot Node";
                case MvpDungeonPlacementPresenter.HiddenCacheOptionKey: return "Hidden Cache";
                default: return fallback;
            }
        }
    }
}
