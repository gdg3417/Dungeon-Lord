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
        public const string MonstersFormatKey = "ui.mvp_room_slots.panel.monsters_format";
        public const string TrapsFormatKey = "ui.mvp_room_slots.panel.traps_format";
        public const string LootFormatKey = "ui.mvp_room_slots.panel.loot_format";
        public const string EmptyAssignmentKey = "ui.mvp_room_slots.panel.empty";
        public const string UnavailableAssignmentKey = "ui.mvp_room_slots.panel.unavailable";
        public const string AssignmentSeparatorKey = "ui.mvp_room_slots.panel.assignment_separator";
        public const string RoomSlotSeparatorKey = "ui.mvp_room_slots.panel.room_separator";

        public static string BuildLayoutText(SaveData save, Func<string, string, string> localize)
        {
            return BuildLayoutText(save, null, localize);
        }

        public static string BuildLayoutText(SaveData save, RunSimulationConfig config, Func<string, string, string> localize)
        {
            MvpDungeonFloorLayoutState layout = save?.mvpDungeonFloorLayout;
            MvpDungeonPlacementState legacyPlacements = save?.mvpDungeonPlacements;
            MvpDungeonPlacementEntry[] resolvedPlacements = config == null
                ? MvpDungeonLayoutResolver.ResolveOrderedPlacements(layout, legacyPlacements)
                : MvpRoomSlotLayoutResolver.ResolveActivePlacements(save, config);
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
            MvpDungeonFloorSlotLayout slotLayout = config == null ? null : MvpRoomSlotLayoutResolver.ResolveDefaultFloor(save, config);
            string selectedTarget = slotLayout == null ? string.Empty : MvpRoomSlotTargetPresenter.BuildSelectedTargetText(slotLayout, MvpRoomSlotTargetResolver.ResolveClampedSelectedRoomIndex(save, slotLayout), localize);
            string roomSlotLayout = slotLayout == null ? string.Empty : BuildRoomSlotLayoutText(slotLayout, localize);
            var fullLayout = new StringBuilder(legacyLayout);
            if (!string.IsNullOrWhiteSpace(selectedTarget)) fullLayout.Append('\n').Append(selectedTarget);
            if (!string.IsNullOrWhiteSpace(roomSlotLayout)) fullLayout.Append('\n').Append(roomSlotLayout);
            return fullLayout.ToString();
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
                    BuildSlotText(MonstersFormatKey, room.AssignedMonsterOptionIds, room.Capacity?.MonsterCapacity ?? 0, localize),
                    BuildSlotText(TrapsFormatKey, room.AssignedTrapOptionIds, room.Capacity?.TrapCapacity ?? 0, localize),
                    BuildSlotText(LootFormatKey, room.AssignedLootNodeOptionIds, room.Capacity?.LootCapacity ?? 0, localize)));
            }

            string floor = string.Format(Localize(localize, RoomSlotFloorFormatKey), layout.FloorIndex, rooms.ToString());
            return string.Format(Localize(localize, RoomSlotLayoutFormatKey), floor);
        }

        private static string BuildSlotText(string formatKey, string[] assignedOptionIds, int capacity, Func<string, string, string> localize)
        {
            int assignedCount = Count(assignedOptionIds);
            string assignmentText = capacity <= 0
                ? Localize(localize, UnavailableAssignmentKey)
                : assignedCount == 0
                    ? Localize(localize, EmptyAssignmentKey)
                    : BuildAssignedNames(assignedOptionIds, localize);

            return string.Format(Localize(localize, formatKey), assignmentText, assignedCount, capacity);
        }

        private static string BuildAssignedNames(string[] assignedOptionIds, Func<string, string, string> localize)
        {
            if (assignedOptionIds == null || assignedOptionIds.Length == 0)
            {
                return Localize(localize, EmptyAssignmentKey);
            }

            var names = new List<string>();
            for (int i = 0; i < assignedOptionIds.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(assignedOptionIds[i]))
                {
                    continue;
                }

                names.Add(MvpDungeonPlacementPresenter.ResolveOptionName(assignedOptionIds[i], localize));
            }

            return names.Count == 0 ? Localize(localize, EmptyAssignmentKey) : string.Join(Localize(localize, AssignmentSeparatorKey), names);
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
