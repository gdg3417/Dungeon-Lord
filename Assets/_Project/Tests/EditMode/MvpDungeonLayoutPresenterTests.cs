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

            string text = MvpDungeonLayoutPresenter.BuildLayoutText(save, Localize);

            Assert.That(text, Is.EqualTo("Dungeon layout: Floor 0: Room: Basic Room -> Monster: Skeleton -> Trap: Spike Trap -> Loot node: Basic Loot Node"));
        }

        [Test]
        public void BuildLayoutText_EmptyStarterLayoutDisplaysFourAvailableNodesSafely()
        {
            SaveData save = SaveWithLayout(MvpDungeonFloorLayoutState.CreateEmptyStarterFloor());

            string text = MvpDungeonLayoutPresenter.BuildLayoutText(save, Localize);

            Assert.That(text, Is.EqualTo("Dungeon layout: Floor 0: Room: Empty / available -> Monster: Empty / available -> Trap: Empty / available -> Loot node: Empty / available"));
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

            string text = MvpDungeonLayoutPresenter.BuildLayoutText(SaveWithLayout(layout), Localize);

            Assert.That(text, Is.EqualTo("Dungeon layout: Floor 0: Room: Basic Room -> Monster: Empty / available -> Trap: Spike Trap -> Loot node: Empty / available"));
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

            string text = MvpDungeonLayoutPresenter.BuildLayoutText(save, Localize);

            Assert.That(text, Is.EqualTo("Dungeon layout: Floor 0: Room: Empty / available -> Monster: Skeleton -> Trap: Empty / available -> Loot node: Basic Loot Node"));
        }

        [Test]
        public void BuildLayoutText_UsesLocalizedNamesWithoutRawIdsOrKeys()
        {
            string text = MvpDungeonLayoutPresenter.BuildLayoutText(SaveWithLayout(FullStarterLayout()), Localize);

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

            string text = MvpDungeonLayoutPresenter.BuildLayoutText(save, Localize);

            Assert.That(text, Does.Contain("Room: Empty / available"));
            Assert.That(text, Does.Contain("Loot node: Empty / available"));
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
                case MvpDungeonPlacementPresenter.RoomCategoryKey: return "Room";
                case MvpDungeonPlacementPresenter.MonsterCategoryKey: return "Monster";
                case MvpDungeonPlacementPresenter.TrapCategoryKey: return "Trap";
                case MvpDungeonPlacementPresenter.LootNodeCategoryKey: return "Loot node";
                case MvpDungeonPlacementPresenter.BasicRoomOptionKey: return "Basic Room";
                case MvpDungeonPlacementPresenter.SkeletonOptionKey: return "Skeleton";
                case MvpDungeonPlacementPresenter.SpikeTrapOptionKey: return "Spike Trap";
                case MvpDungeonPlacementPresenter.BasicLootNodeOptionKey: return "Basic Loot Node";
                default: return fallback;
            }
        }
    }
}
