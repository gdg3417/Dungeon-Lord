using System;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0
{
    public static class MvpRoomSlotTargetPresenter
    {
        public const string SelectedTargetFormatKey = "ui.mvp_room_slots.selected_target_format";
        public const string NoValidSlotFormatKey = "ui.mvp_room_slots.no_valid_slot_format";
        public const string SelectedCapacityFormatKey = "ui.mvp_room_slots.selected_capacity_format";
        public const string SelectedCapacityCategoryFormatKey = "ui.mvp_room_slots.selected_capacity_category_format";
        public const string SelectedCapacityUnavailableCategoryFormatKey = "ui.mvp_room_slots.selected_capacity_unavailable_category_format";
        public const string SelectedCapacitySeparatorKey = "ui.mvp_room_slots.selected_capacity_separator";
        public const string SelectedPlacementFitFormatKey = "ui.mvp_room_slots.selected_placement_fit_format";
        public const string SelectedPlacementFitsFormatKey = "ui.mvp_room_slots.selected_placement_fits_format";
        public const string SelectedPlacementCannotFitNoSlotFormatKey = "ui.mvp_room_slots.selected_placement_cannot_fit_no_slot_format";
        public const string CapacityMonstersLabelKey = "ui.mvp_room_slots.capacity.monsters";
        public const string CapacityTrapsLabelKey = "ui.mvp_room_slots.capacity.traps";
        public const string CapacityLootLabelKey = "ui.mvp_room_slots.capacity.loot";
        public const string MonsterSlotReasonLabelKey = "ui.mvp_room_slots.reason.monster_slot";
        public const string TrapSlotReasonLabelKey = "ui.mvp_room_slots.reason.trap_slot";
        public const string LootSlotReasonLabelKey = "ui.mvp_room_slots.reason.loot_slot";

        public static string BuildSelectedTargetText(SaveData save, RunSimulationConfig config, Func<string, string, string> localize)
        {
            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(save, config);
            int index = MvpRoomSlotTargetResolver.ResolveClampedSelectedRoomIndex(save, layout);
            return BuildSelectedTargetText(layout, index, localize);
        }

        public static string BuildSelectedTargetText(MvpDungeonFloorSlotLayout layout, int roomIndex, Func<string, string, string> localize)
        {
            MvpDungeonRoomInstance room = ResolveRoom(layout, roomIndex);
            string roomName = room == null ? string.Empty : MvpDungeonPlacementPresenter.ResolveOptionName(room.RoomOptionId, localize);
            return string.Format(Localize(localize, SelectedTargetFormatKey), roomIndex + 1, roomName);
        }

        public static string BuildNoValidSlotText(MvpDungeonFloorSlotLayout layout, int roomIndex, string categoryId, Func<string, string, string> localize)
        {
            MvpDungeonRoomInstance room = ResolveRoom(layout, roomIndex);
            string category = MvpDungeonPlacementPresenter.ResolveCategoryName(categoryId, localize);
            string roomName = room == null ? string.Empty : MvpDungeonPlacementPresenter.ResolveOptionName(room.RoomOptionId, localize);
            return string.Format(Localize(localize, NoValidSlotFormatKey), category, roomIndex + 1, roomName);
        }

        public static string BuildSelectedCapacityText(MvpDungeonFloorSlotLayout layout, int roomIndex, Func<string, string, string> localize)
        {
            MvpDungeonRoomInstance room = ResolveRoom(layout, roomIndex);
            if (room?.Capacity == null)
            {
                return string.Empty;
            }

            string separator = Localize(localize, SelectedCapacitySeparatorKey);
            string monsters = BuildCapacityCategoryText(MvpDungeonPlacementIds.MonsterCategoryId, Count(room.AssignedMonsterOptionIds), room.Capacity.MonsterCapacity, localize);
            string traps = BuildCapacityCategoryText(MvpDungeonPlacementIds.TrapCategoryId, Count(room.AssignedTrapOptionIds), room.Capacity.TrapCapacity, localize);
            string loot = BuildCapacityCategoryText(MvpDungeonPlacementIds.LootNodeCategoryId, Count(room.AssignedLootNodeOptionIds), room.Capacity.LootCapacity, localize);
            return string.Format(Localize(localize, SelectedCapacityFormatKey), string.Join(separator, new[] { monsters, traps, loot }));
        }

        public static string BuildSelectedPlacementFitText(MvpDungeonFloorSlotLayout layout, int roomIndex, string categoryId, Func<string, string, string> localize)
        {
            if (string.IsNullOrWhiteSpace(categoryId) || string.Equals(categoryId, MvpDungeonPlacementIds.RoomCategoryId, StringComparison.Ordinal))
            {
                return string.Empty;
            }

            MvpDungeonRoomInstance room = ResolveRoom(layout, roomIndex);
            if (room?.Capacity == null || !MvpDungeonPlacementIds.IsAllowedCategory(categoryId))
            {
                return string.Empty;
            }

            string category = MvpDungeonPlacementPresenter.ResolveCategoryName(categoryId, localize);
            string detail = MvpRoomSlotTargetResolver.CanAccept(room, categoryId)
                ? string.Format(Localize(localize, SelectedPlacementFitsFormatKey), category, roomIndex + 1)
                : string.Format(Localize(localize, SelectedPlacementCannotFitNoSlotFormatKey), category, roomIndex + 1, ResolveUnavailableReasonCategoryName(categoryId, localize));
            return string.Format(Localize(localize, SelectedPlacementFitFormatKey), detail);
        }

        private static string BuildCapacityCategoryText(string categoryId, int assignedCount, int capacity, Func<string, string, string> localize)
        {
            string category = ResolveCapacityCategoryName(categoryId, localize);
            string formatKey = capacity <= 0 ? SelectedCapacityUnavailableCategoryFormatKey : SelectedCapacityCategoryFormatKey;
            return string.Format(Localize(localize, formatKey), category, assignedCount, capacity);
        }

        private static string ResolveCapacityCategoryName(string categoryId, Func<string, string, string> localize)
        {
            switch (categoryId)
            {
                case MvpDungeonPlacementIds.MonsterCategoryId: return Localize(localize, CapacityMonstersLabelKey);
                case MvpDungeonPlacementIds.TrapCategoryId: return Localize(localize, CapacityTrapsLabelKey);
                case MvpDungeonPlacementIds.LootNodeCategoryId: return Localize(localize, CapacityLootLabelKey);
                default: return MvpDungeonPlacementPresenter.ResolveCategoryName(categoryId, localize);
            }
        }

        private static string ResolveUnavailableReasonCategoryName(string categoryId, Func<string, string, string> localize)
        {
            switch (categoryId)
            {
                case MvpDungeonPlacementIds.MonsterCategoryId: return Localize(localize, MonsterSlotReasonLabelKey);
                case MvpDungeonPlacementIds.TrapCategoryId: return Localize(localize, TrapSlotReasonLabelKey);
                case MvpDungeonPlacementIds.LootNodeCategoryId: return Localize(localize, LootSlotReasonLabelKey);
                default: return MvpDungeonPlacementPresenter.ResolveCategoryName(categoryId, localize);
            }
        }

        private static int Count(string[] values)
        {
            return values == null ? 0 : values.Length;
        }

        private static MvpDungeonRoomInstance ResolveRoom(MvpDungeonFloorSlotLayout layout, int roomIndex)
        {
            if (layout?.Rooms == null || layout.Rooms.Length == 0) return null;
            int clamped = Math.Max(0, Math.Min(roomIndex, layout.Rooms.Length - 1));
            return layout.Rooms[clamped];
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            return localize == null ? key : localize(key, key);
        }
    }

    public static class MvpRoomSlotTargetResolver
    {
        public static int ResolveClampedSelectedRoomIndex(SaveData save, MvpDungeonFloorSlotLayout layout)
        {
            int requested = Math.Max(0, save?.mvpSelectedRoomSlotIndex ?? 0);
            int count = layout?.Rooms == null ? 0 : layout.Rooms.Length;
            return count <= 0 ? 0 : Math.Min(requested, count - 1);
        }

        public static bool HasKnownCapacity(MvpDungeonRoomInstance room)
        {
            return room?.Capacity != null &&
                   (room.Capacity.MonsterCapacity > 0 || room.Capacity.TrapCapacity > 0 || room.Capacity.LootCapacity > 0);
        }

        public static bool CanAccept(MvpDungeonRoomInstance room, string categoryId)
        {
            if (room?.Capacity == null) return false;
            switch (categoryId)
            {
                case MvpDungeonPlacementIds.MonsterCategoryId: return room.Capacity.MonsterCapacity > 0;
                case MvpDungeonPlacementIds.TrapCategoryId: return room.Capacity.TrapCapacity > 0;
                case MvpDungeonPlacementIds.LootNodeCategoryId: return room.Capacity.LootCapacity > 0;
                case MvpDungeonPlacementIds.RoomCategoryId: return true;
                default: return false;
            }
        }
    }
}
