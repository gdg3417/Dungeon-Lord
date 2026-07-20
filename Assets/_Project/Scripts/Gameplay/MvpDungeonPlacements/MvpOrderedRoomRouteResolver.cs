using System;
using System.Collections.Generic;

namespace DungeonBuilder.M0.Gameplay.MvpDungeonPlacements
{
    public sealed class MvpOrderedRouteRoom
    {
        public int FloorIndex;
        public int RoomIndex;
        public string RoomOptionId;
        public string[] AssignedMonsterOptionIds = Array.Empty<string>();
        public string[] AssignedTrapOptionIds = Array.Empty<string>();
        public string[] AssignedLootNodeOptionIds = Array.Empty<string>();
        public MvpRoomSlotCapacity Capacity;
        public bool HasActiveContent;

        public MvpDungeonPlacementEntry[] ToOrderedPlacements()
        {
            var result = new List<MvpDungeonPlacementEntry>();
            Add(result, MvpDungeonPlacementIds.RoomCategoryId, new[] { RoomOptionId });
            Add(result, MvpDungeonPlacementIds.MonsterCategoryId, AssignedMonsterOptionIds);
            Add(result, MvpDungeonPlacementIds.TrapCategoryId, AssignedTrapOptionIds);
            Add(result, MvpDungeonPlacementIds.LootNodeCategoryId, AssignedLootNodeOptionIds);
            return result.ToArray();
        }

        private static void Add(List<MvpDungeonPlacementEntry> target, string category, string[] ids)
        {
            if (ids == null) return;
            for (int i = 0; i < ids.Length; i++)
                if (!string.IsNullOrWhiteSpace(ids[i])) target.Add(new MvpDungeonPlacementEntry(category, ids[i], i));
        }
    }

    public static class MvpOrderedRoomRouteResolver
    {
        public static MvpOrderedRouteRoom[] Resolve(SaveData save, RunSimulationConfig config)
        {
            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(save, config);
            if (layout?.Rooms == null) return Array.Empty<MvpOrderedRouteRoom>();
            var route = new List<MvpOrderedRouteRoom>();
            // The slot resolver already applies the save convention “last duplicate wins”.
            // GD60 admits only floor zero and its first two ordered room indices.
            for (int roomIndex = 0; roomIndex < layout.Rooms.Length && roomIndex <= MvpRoomSlotLayoutResolver.MvpSecondRoomSlotIndex; roomIndex++)
            {
                MvpDungeonRoomInstance room = layout.Rooms[roomIndex];
                if (room == null) continue;
                route.Add(new MvpOrderedRouteRoom {
                    FloorIndex = layout.FloorIndex, RoomIndex = roomIndex, RoomOptionId = room.RoomOptionId,
                    AssignedMonsterOptionIds = room.AssignedMonsterOptionIds ?? Array.Empty<string>(),
                    AssignedTrapOptionIds = room.AssignedTrapOptionIds ?? Array.Empty<string>(),
                    AssignedLootNodeOptionIds = room.AssignedLootNodeOptionIds ?? Array.Empty<string>(), Capacity = room.Capacity,
                    HasActiveContent = (room.AssignedMonsterOptionIds?.Length ?? 0) + (room.AssignedTrapOptionIds?.Length ?? 0) + (room.AssignedLootNodeOptionIds?.Length ?? 0) > 0
                });
            }
            return route.ToArray();
        }
    }
}
