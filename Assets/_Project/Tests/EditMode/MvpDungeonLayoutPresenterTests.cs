#if UNITY_EDITOR
using System.Collections.Generic;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpDungeonLayoutPresenterTests
    {
        [Test]
        public void BuildLayoutText_FullStarterLayoutDisplaysRoomSlotViewInDeterministicOrder()
        {
            SaveData save = SaveWithLayout(FullStarterLayout());

            string text = MvpDungeonLayoutPresenter.BuildLayoutText(save, Config(), Localize);

            Assert.That(text, Is.EqualTo("Selected room target: Room 1: Basic Room\nSelected room capacity: Monsters 1/1; Traps 1/1; Loot 1/1\nRoom slot layout: Floor 0: Room 1: Basic Room (Monsters: Skeleton 1/1; Traps: Spike Trap 1/1; Loot: Basic Loot Node 1/1)"));
        }

        [Test]
        public void BuildLayoutText_EmptyStarterLayoutDisplaysRoomSlotViewSafely()
        {
            SaveData save = SaveWithLayout(MvpDungeonFloorLayoutState.CreateEmptyStarterFloor());

            string text = MvpDungeonLayoutPresenter.BuildLayoutText(save, Config(), Localize);

            Assert.That(text, Is.EqualTo("Selected room target: Room 1: Basic Room\nSelected room capacity: Monsters 0/1; Traps 0/1; Loot 0/1\nRoom slot layout: Floor 0: Room 1: Basic Room (Monsters: empty 0/1; Traps: empty 0/1; Loot: empty 0/1)"));
        }

        [Test]
        public void BuildLayoutText_PartialLayoutDisplaysRoomSlotViewSafely()
        {
            MvpDungeonFloorLayoutState layout = MvpDungeonFloorLayoutState.CreateEmptyStarterFloor();
            layout.Nodes[0].CategoryId = MvpDungeonPlacementIds.RoomCategoryId;
            layout.Nodes[0].OptionId = MvpDungeonPlacementIds.BasicRoomOptionId;
            layout.Nodes[0].Revision = 1;
            layout.Nodes[2].CategoryId = MvpDungeonPlacementIds.TrapCategoryId;
            layout.Nodes[2].OptionId = MvpDungeonPlacementIds.SpikeTrapOptionId;
            layout.Nodes[2].Revision = 2;

            string text = MvpDungeonLayoutPresenter.BuildLayoutText(SaveWithLayout(layout), Config(), Localize);

            Assert.That(text, Is.EqualTo("Selected room target: Room 1: Basic Room\nSelected room capacity: Monsters 0/1; Traps 1/1; Loot 0/1\nRoom slot layout: Floor 0: Room 1: Basic Room (Monsters: empty 0/1; Traps: Spike Trap 1/1; Loot: empty 0/1)"));
        }

        [Test]
        public void BuildLayoutText_LegacyPlacementStateDisplaysThroughResolverFallback()
        {
            var save = new SaveData
            {
                mvpDungeonFloorLayout = null,
                mvpDungeonPlacements = new MvpDungeonPlacementState
                {
                    Entries = new List<MvpDungeonPlacementEntry>
                    {
                        new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 1),
                        new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId, 2)
                    }
                }
            };

            string text = MvpDungeonLayoutPresenter.BuildLayoutText(save, Config(), Localize);

            Assert.That(text, Is.EqualTo("Selected room target: Room 1: Basic Room\nSelected room capacity: Monsters 1/1; Traps 0/1; Loot 1/1\nRoom slot layout: Floor 0: Room 1: Basic Room (Monsters: Skeleton 1/1; Traps: empty 0/1; Loot: Basic Loot Node 1/1)"));
        }

        [Test]
        public void BuildLayoutText_UsesLocalizedNamesWithoutRawIdsOrKeys()
        {
            string text = MvpDungeonLayoutPresenter.BuildLayoutText(SaveWithLayout(FullStarterLayout()), Config(), Localize);

            Assert.That(text, Does.Not.Contain("placement.category"));
            Assert.That(text, Does.Not.Contain("placement.option"));
            Assert.That(text, Does.Not.Contain("ui.mvp_dungeon_layout"));
            Assert.That(text, Does.Not.Contain("ui.mvp_room_slots"));
        }

        [Test]
        public void BuildLayoutText_CleanResetEmptyLayoutResolvesSafely()
        {
            var save = new SaveData
            {
                mvpDungeonPlacements = new MvpDungeonPlacementState(),
                mvpDungeonFloorLayout = MvpDungeonFloorLayoutState.CreateEmptyStarterFloor()
            };

            string text = MvpDungeonLayoutPresenter.BuildLayoutText(save, Config(), Localize);

            Assert.That(text, Does.Contain("Selected room target: Room 1: Basic Room"));
            Assert.That(text, Does.Contain("Room slot layout: Floor 0: Room 1: Basic Room"));
            Assert.That(text, Does.Not.Contain("Dungeon layout:"));
        }


        [Test]
        public void BuildLayoutText_NarrowHallCapacityAndBasicRoomLootFallbackAreDeterministic()
        {
            MvpDungeonFloorLayoutState layout = MvpDungeonFloorLayoutState.CreateStarterFloorFromLegacyPlacements(new MvpDungeonPlacementState
            {
                Entries = new List<MvpDungeonPlacementEntry>
                {
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId, 1),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId, 2),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SnareTrapOptionId, 3),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId, 4)
                }
            });

            string text = MvpDungeonLayoutPresenter.BuildLayoutText(SaveWithLayout(layout), Config(), Localize);

            Assert.That(text, Does.Not.Contain("Dungeon layout:"));
            Assert.That(text, Does.Contain("Room 1: Narrow Hall (Monsters: Goblin 1/1; Traps: Snare Trap 1/1; Loot: unavailable 0/0)"));
            Assert.That(text, Does.Contain("Room 2: Basic Room (Monsters: empty 0/1; Traps: empty 0/1; Loot: Hidden Cache 1/1)"));
            Assert.That(text, Does.Not.Contain("placement.option"));
        }


        [Test]
        public void BuildLayoutText_DefaultSelectedRoomTargetIsRoomOne()
        {
            string text = MvpDungeonLayoutPresenter.BuildLayoutText(SaveWithLayout(FullStarterLayout()), Config(), Localize);

            Assert.That(text, Does.Contain("Selected room target: Room 1: Basic Room"));
        }

        [Test]
        public void BuildLayoutText_SelectedRoomTargetCanShowRoomTwoWhenFallbackRoomExists()
        {
            SaveData save = SaveWithLayout(NarrowHallHiddenCacheLayout());
            save.mvpSelectedRoomSlotIndex = 1;

            string text = MvpDungeonLayoutPresenter.BuildLayoutText(save, Config(), Localize);

            Assert.That(text, Does.Contain("Selected room target: Room 2: Basic Room"));
            Assert.That(text, Does.Not.Contain("placement.option"));
            Assert.That(text, Does.Not.Contain("ui.mvp_room_slots"));
        }


        [Test]
        public void BuildLayoutText_SelectedPlacementFitReportsMonsterTrapLootFitsAndLootBlocked()
        {
            Assert.That(MvpDungeonLayoutPresenter.BuildLayoutText(SaveWithLayout(FullStarterLayout()), Config(), MvpDungeonPlacementIds.MonsterCategoryId, Localize), Does.Contain("Selected placement fit: Monster fits Room 1."));
            Assert.That(MvpDungeonLayoutPresenter.BuildLayoutText(SaveWithLayout(FullStarterLayout()), Config(), MvpDungeonPlacementIds.TrapCategoryId, Localize), Does.Contain("Selected placement fit: Trap fits Room 1."));

            SaveData roomTwo = SaveWithLayout(NarrowHallHiddenCacheLayout());
            roomTwo.mvpSelectedRoomSlotIndex = 1;
            Assert.That(MvpDungeonLayoutPresenter.BuildLayoutText(roomTwo, Config(), MvpDungeonPlacementIds.LootNodeCategoryId, Localize), Does.Contain("Selected placement fit: Loot node fits Room 2."));

            Assert.That(MvpDungeonLayoutPresenter.BuildLayoutText(SaveWithLayout(NarrowHallHiddenCacheLayout()), Config(), MvpDungeonPlacementIds.LootNodeCategoryId, Localize), Does.Contain("Selected placement fit: Loot node cannot fit Room 1 because this room has no loot slot."));
        }

        [Test]
        public void BuildLayoutText_SelectedCapacityReportsOccupiedAndUnavailableCounts()
        {
            string occupied = MvpDungeonLayoutPresenter.BuildLayoutText(SaveWithLayout(FullStarterLayout()), Config(), Localize);
            Assert.That(occupied, Does.Contain("Selected room capacity: Monsters 1/1; Traps 1/1; Loot 1/1"));

            string unavailable = MvpDungeonLayoutPresenter.BuildLayoutText(SaveWithLayout(NarrowHallHiddenCacheLayout()), Config(), Localize);
            Assert.That(unavailable, Does.Contain("Selected room capacity: Monsters 1/1; Traps 1/1; Loot unavailable 0/0"));
        }

        [Test]
        public void BuildNoValidSlotText_UsesLocalizedLabelsWithoutRawIdsOrKeys()
        {
            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(SaveWithLayout(NarrowHallHiddenCacheLayout()), Config());

            string text = MvpRoomSlotTargetPresenter.BuildNoValidSlotText(layout, 0, MvpDungeonPlacementIds.LootNodeCategoryId, Localize);

            Assert.That(text, Is.EqualTo("No valid Loot node slot in Room 1: Narrow Hall."));
            Assert.That(text, Does.Not.Contain("placement."));
            Assert.That(text, Does.Not.Contain("ui.mvp_room_slots"));
        }

        [Test]
        public void ResolveDefaultFloor_NarrowHallLootFallbackCreatesTwoRoomsInDeterministicOrder()
        {
            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(
                SaveWithLayout(NarrowHallHiddenCacheLayout()),
                Config());

            Assert.That(layout.FloorIndex, Is.EqualTo(0));
            Assert.That(layout.Rooms, Has.Length.EqualTo(2));
            Assert.That(layout.Rooms[0].RoomOptionId, Is.EqualTo(MvpDungeonPlacementIds.NarrowHallOptionId));
            Assert.That(layout.Rooms[0].Capacity.MonsterCapacity, Is.EqualTo(1));
            Assert.That(layout.Rooms[0].Capacity.TrapCapacity, Is.EqualTo(1));
            Assert.That(layout.Rooms[0].Capacity.LootCapacity, Is.EqualTo(0));
            Assert.That(layout.Rooms[0].AssignedMonsterOptionIds, Is.EqualTo(new[] { MvpDungeonPlacementIds.GoblinOptionId }));
            Assert.That(layout.Rooms[0].AssignedTrapOptionIds, Is.EqualTo(new[] { MvpDungeonPlacementIds.SnareTrapOptionId }));
            Assert.That(layout.Rooms[0].AssignedLootNodeOptionIds, Is.Empty);
            Assert.That(layout.Rooms[1].RoomOptionId, Is.EqualTo(MvpDungeonPlacementIds.BasicRoomOptionId));
            Assert.That(layout.Rooms[1].Capacity.MonsterCapacity, Is.EqualTo(1));
            Assert.That(layout.Rooms[1].Capacity.TrapCapacity, Is.EqualTo(1));
            Assert.That(layout.Rooms[1].Capacity.LootCapacity, Is.EqualTo(1));
            Assert.That(layout.Rooms[1].AssignedLootNodeOptionIds, Is.EqualTo(new[] { MvpDungeonPlacementIds.HiddenCacheOptionId }));
        }

        [Test]
        public void ResolveDefaultFloor_DoesNotCreateFallbackRoomWhenNoLootCapacityExists()
        {
            var config = new RunSimulationConfig
            {
                MvpRoomSlotCapacities = new[]
                {
                    new MvpRoomSlotCapacityConfig { RoomOptionId = MvpDungeonPlacementIds.NarrowHallOptionId, MonsterCapacity = 1, TrapCapacity = 1, LootCapacity = 0 },
                    new MvpRoomSlotCapacityConfig { RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId, MonsterCapacity = 1, TrapCapacity = 1, LootCapacity = 0 }
                }
            };

            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(
                SaveWithLayout(NarrowHallHiddenCacheLayout()),
                config);

            Assert.That(layout.Rooms, Has.Length.EqualTo(1));
            Assert.That(layout.Rooms[0].RoomOptionId, Is.EqualTo(MvpDungeonPlacementIds.NarrowHallOptionId));
            Assert.That(layout.Rooms[0].AssignedLootNodeOptionIds, Is.Empty);
        }

        [Test]
        public void BuildRoomSlotLayoutText_JoinsMultipleAssignmentsWithLocalizedSeparator()
        {
            var layout = new MvpDungeonFloorSlotLayout
            {
                FloorIndex = 0,
                Rooms = new[]
                {
                    new MvpDungeonRoomInstance
                    {
                        RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                        Capacity = new MvpRoomSlotCapacity { RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId, MonsterCapacity = 2, TrapCapacity = 1, LootCapacity = 1 },
                        AssignedMonsterOptionIds = new[] { MvpDungeonPlacementIds.SkeletonOptionId, MvpDungeonPlacementIds.GoblinOptionId },
                        AssignedTrapOptionIds = new[] { MvpDungeonPlacementIds.SpikeTrapOptionId },
                        AssignedLootNodeOptionIds = new[] { MvpDungeonPlacementIds.BasicLootNodeOptionId }
                    }
                }
            };

            string text = MvpDungeonLayoutPresenter.BuildRoomSlotLayoutText(layout, Localize);

            Assert.That(text, Does.Contain("Monsters: Skeleton, Goblin 2/2"));
            Assert.That(text, Does.Not.Contain("placement.option"));
            Assert.That(text, Does.Not.Contain("ui.mvp_room_slots"));
        }

        private static SaveData SaveWithLayout(MvpDungeonFloorLayoutState layout)
        {
            return new SaveData
            {
                mvpDungeonFloorLayout = layout,
                mvpDungeonPlacements = new MvpDungeonPlacementState()
            };
        }

        private static MvpDungeonFloorLayoutState FullStarterLayout()
        {
            return MvpDungeonFloorLayoutState.CreateStarterFloorFromLegacyPlacements(new MvpDungeonPlacementState
            {
                Entries = new List<MvpDungeonPlacementEntry>
                {
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId, 1),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 2),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId, 3),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId, 4)
                }
            });
        }

        private static MvpDungeonFloorLayoutState NarrowHallHiddenCacheLayout()
        {
            return MvpDungeonFloorLayoutState.CreateStarterFloorFromLegacyPlacements(new MvpDungeonPlacementState
            {
                Entries = new List<MvpDungeonPlacementEntry>
                {
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId, 1),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId, 2),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SnareTrapOptionId, 3),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId, 4)
                }
            });
        }

        private static RunSimulationConfig Config()
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
                case MvpRoomSlotTargetPresenter.NoValidSlotFormatKey: return "No valid {0} slot in Room {1}: {2}.";
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
#endif
