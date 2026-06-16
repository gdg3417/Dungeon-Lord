using System.Collections.Generic;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpDungeonLayoutPresenterTests
    {
        [Test]
        public void BuildLayoutText_FullStarterLayoutDisplaysFourNodesInDeterministicOrder()
        {
            SaveData save = SaveWithLayout(FullStarterLayout());

            string text = MvpDungeonLayoutPresenter.BuildLayoutText(save, Config(), Localize);

            Assert.That(text, Is.EqualTo("Dungeon layout: Floor 0: Room: Basic Room -> Monster: Skeleton -> Trap: Spike Trap -> Loot node: Basic Loot Node\nRoom slot layout: Floor 0: Room 1: Basic Room (Monster slots: 1/1; Trap slots: 1/1; Loot slots: 1/1)"));
        }

        [Test]
        public void BuildLayoutText_EmptyStarterLayoutDisplaysFourAvailableNodesSafely()
        {
            SaveData save = SaveWithLayout(MvpDungeonFloorLayoutState.CreateEmptyStarterFloor());

            string text = MvpDungeonLayoutPresenter.BuildLayoutText(save, Config(), Localize);

            Assert.That(text, Is.EqualTo("Dungeon layout: Floor 0: Room: Empty / available -> Monster: Empty / available -> Trap: Empty / available -> Loot node: Empty / available\nRoom slot layout: Floor 0: Room 1: Basic Room (Monster slots: 0/1; Trap slots: 0/1; Loot slots: 0/1)"));
        }

        [Test]
        public void BuildLayoutText_PartialLayoutDisplaysAssignedAndEmptyNodesSafely()
        {
            MvpDungeonFloorLayoutState layout = MvpDungeonFloorLayoutState.CreateEmptyStarterFloor();
            layout.Nodes[0].CategoryId = MvpDungeonPlacementIds.RoomCategoryId;
            layout.Nodes[0].OptionId = MvpDungeonPlacementIds.BasicRoomOptionId;
            layout.Nodes[0].Revision = 1;
            layout.Nodes[2].CategoryId = MvpDungeonPlacementIds.TrapCategoryId;
            layout.Nodes[2].OptionId = MvpDungeonPlacementIds.SpikeTrapOptionId;
            layout.Nodes[2].Revision = 2;

            string text = MvpDungeonLayoutPresenter.BuildLayoutText(SaveWithLayout(layout), Config(), Localize);

            Assert.That(text, Is.EqualTo("Dungeon layout: Floor 0: Room: Basic Room -> Monster: Empty / available -> Trap: Spike Trap -> Loot node: Empty / available\nRoom slot layout: Floor 0: Room 1: Basic Room (Monster slots: 0/1; Trap slots: 1/1; Loot slots: 0/1)"));
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

            Assert.That(text, Is.EqualTo("Dungeon layout: Floor 0: Room: Empty / available -> Monster: Skeleton -> Trap: Empty / available -> Loot node: Basic Loot Node\nRoom slot layout: Floor 0: Room 1: Basic Room (Monster slots: 1/1; Trap slots: 0/1; Loot slots: 1/1)"));
        }

        [Test]
        public void BuildLayoutText_UsesLocalizedNamesWithoutRawIdsOrKeys()
        {
            string text = MvpDungeonLayoutPresenter.BuildLayoutText(SaveWithLayout(FullStarterLayout()), Config(), Localize);

            Assert.That(text, Does.Not.Contain("placement.category"));
            Assert.That(text, Does.Not.Contain("placement.option"));
            Assert.That(text, Does.Not.Contain("ui.mvp_dungeon_layout"));
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

            Assert.That(text, Does.Contain("Room: Empty / available"));
            Assert.That(text, Does.Contain("Loot node: Empty / available"));
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

            Assert.That(text, Does.Contain("Room 1: Narrow Hall (Monster slots: 1/1; Trap slots: 1/1; Loot slots: 0/0)"));
            Assert.That(text, Does.Contain("Room 2: Basic Room (Monster slots: 0/1; Trap slots: 0/1; Loot slots: 1/1)"));
            Assert.That(text, Does.Not.Contain("placement.option"));
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
                case MvpDungeonLayoutPresenter.RoomSlotFloorFormatKey: return "Floor {0}: {1}";
                case MvpDungeonLayoutPresenter.RoomSlotRoomFormatKey: return "Room {0}: {1} ({2}; {3}; {4})";
                case MvpDungeonLayoutPresenter.MonsterSlotsFormatKey: return "Monster slots: {0}/{1}";
                case MvpDungeonLayoutPresenter.TrapSlotsFormatKey: return "Trap slots: {0}/{1}";
                case MvpDungeonLayoutPresenter.LootSlotsFormatKey: return "Loot slots: {0}/{1}";
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
