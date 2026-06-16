using System;
using System.Collections.Generic;
using System.Text;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0
{
    public static class MvpDungeonLayoutPresenter
    {
        public const string LayoutFormatKey = "ui.mvp_dungeon_layout.panel.layout_format";
        public const string FloorFormatKey = "ui.mvp_dungeon_layout.panel.floor_format";
        public const string AssignedNodeFormatKey = "ui.mvp_dungeon_layout.panel.assigned_node_format";
        public const string EmptyNodeFormatKey = "ui.mvp_dungeon_layout.panel.empty_node_format";
        public const string NodeSeparatorKey = "ui.mvp_dungeon_layout.panel.node_separator";
        public const string EmptyAvailableKey = "ui.mvp_dungeon_layout.value.empty_available";
        public const string RoomSlotLayoutFormatKey = "ui.mvp_room_slots.panel.layout_format";
        public const string RoomSlotFloorFormatKey = "ui.mvp_room_slots.panel.floor_format";
        public const string RoomSlotRoomFormatKey = "ui.mvp_room_slots.panel.room_format";
        public const string MonsterSlotsFormatKey = "ui.mvp_room_slots.panel.monster_slots_format";
        public const string TrapSlotsFormatKey = "ui.mvp_room_slots.panel.trap_slots_format";
        public const string LootSlotsFormatKey = "ui.mvp_room_slots.panel.loot_slots_format";
        public const string RoomSlotSeparatorKey = "ui.mvp_room_slots.panel.room_separator";

        public static string BuildLayoutText(SaveData save, Func<string, string, string> localize)
        {
            return BuildLayoutText(save, null, localize);
        }

        public static string BuildLayoutText(SaveData save, RunSimulationConfig config, Func<string, string, string> localize)
        {
            MvpDungeonFloorLayoutState layout = save?.mvpDungeonFloorLayout;
            MvpDungeonPlacementState legacyPlacements = save?.mvpDungeonPlacements;
            MvpDungeonPlacementEntry[] resolvedPlacements = MvpDungeonLayoutResolver.ResolveOrderedPlacements(layout, legacyPlacements);
            var placementsByCategory = new Dictionary<string, MvpDungeonPlacementEntry>(StringComparer.Ordinal);
            foreach (MvpDungeonPlacementEntry placement in resolvedPlacements)
            {
                if (placement != null && !placementsByCategory.ContainsKey(placement.CategoryId))
                {
                    placementsByCategory.Add(placement.CategoryId, placement);
                }
            }

            var nodes = new StringBuilder();
            string separator = Localize(localize, NodeSeparatorKey);
            for (int nodeIndex = 0; nodeIndex < MvpDungeonPlacementIds.OrderedCategoryIds.Length; nodeIndex++)
            {
                string categoryId = MvpDungeonPlacementIds.OrderedCategoryIds[nodeIndex];
                if (nodes.Length > 0)
                {
                    nodes.Append(separator);
                }

                string categoryName = MvpDungeonPlacementPresenter.ResolveCategoryName(categoryId, localize);
                if (placementsByCategory.TryGetValue(categoryId, out MvpDungeonPlacementEntry placement))
                {
                    nodes.Append(string.Format(
                        Localize(localize, AssignedNodeFormatKey),
                        categoryName,
                        MvpDungeonPlacementPresenter.ResolveOptionName(placement.OptionId, localize)));
                }
                else
                {
                    nodes.Append(string.Format(
                        Localize(localize, EmptyNodeFormatKey),
                        categoryName,
                        Localize(localize, EmptyAvailableKey)));
                }
            }

            string floorText = string.Format(Localize(localize, FloorFormatKey), 0, nodes.ToString());
            string legacyLayout = string.Format(Localize(localize, LayoutFormatKey), floorText);
            string roomSlotLayout = config == null ? string.Empty : BuildRoomSlotLayoutText(MvpRoomSlotLayoutResolver.ResolveDefaultFloor(save, config), localize);
            return string.IsNullOrWhiteSpace(roomSlotLayout) ? legacyLayout : legacyLayout + "\n" + roomSlotLayout;
        }

        public static string BuildRoomSlotLayoutText(MvpDungeonFloorSlotLayout layout, Func<string, string, string> localize)
        {
            if (layout == null || layout.Rooms == null || layout.Rooms.Length == 0)
            {
                return string.Empty;
            }

            var rooms = new StringBuilder();
            string separator = Localize(localize, RoomSlotSeparatorKey);
            for (int i = 0; i < layout.Rooms.Length; i++)
            {
                MvpDungeonRoomInstance room = layout.Rooms[i];
                if (room == null) continue;
                if (rooms.Length > 0) rooms.Append(separator);
                string roomName = MvpDungeonPlacementPresenter.ResolveOptionName(room.RoomOptionId, localize);
                rooms.Append(string.Format(
                    Localize(localize, RoomSlotRoomFormatKey),
                    i + 1,
                    roomName,
                    string.Format(Localize(localize, MonsterSlotsFormatKey), Count(room.AssignedMonsterOptionIds), room.Capacity?.MonsterCapacity ?? 0),
                    string.Format(Localize(localize, TrapSlotsFormatKey), Count(room.AssignedTrapOptionIds), room.Capacity?.TrapCapacity ?? 0),
                    string.Format(Localize(localize, LootSlotsFormatKey), Count(room.AssignedLootNodeOptionIds), room.Capacity?.LootCapacity ?? 0)));
            }

            string floor = string.Format(Localize(localize, RoomSlotFloorFormatKey), layout.FloorIndex, rooms.ToString());
            return string.Format(Localize(localize, RoomSlotLayoutFormatKey), floor);
        }

        private static int Count(string[] values)
        {
            return values == null ? 0 : values.Length;
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            return localize == null ? key : localize(key, key);
        }
    }
}
