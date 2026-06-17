using System;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0
{
    public static class MvpRoomSlotTargetPresenter
    {
        public const string SelectedTargetFormatKey = "ui.mvp_room_slots.selected_target_format";
        public const string NoValidSlotFormatKey = "ui.mvp_room_slots.no_valid_slot_format";

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
