using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.MvpDungeonPlacements
{
    public sealed class MvpOrderedRouteRoom
    {
        public int FloorIndex;
        public int RoomIndex;
        public string RoomOptionId;
        public bool IncludeRoomPlacement;
        public string[] AssignedMonsterOptionIds = Array.Empty<string>();
        public string[] AssignedTrapOptionIds = Array.Empty<string>();
        public string[] AssignedLootNodeOptionIds = Array.Empty<string>();
        public MvpRoomSlotCapacity Capacity;
        public bool HasActiveContent;

        public MvpDungeonPlacementEntry[] ToOrderedPlacements()
        {
            var result = new List<MvpDungeonPlacementEntry>();
            if (IncludeRoomPlacement) Add(result, MvpDungeonPlacementIds.RoomCategoryId, new[] { RoomOptionId });
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
            bool persisted = save?.mvpRoomSlotAssignments?.Rooms != null && save.mvpRoomSlotAssignments.Rooms.Count > 0;
            bool explicitRoom = MvpDungeonLayoutResolver.ResolveOrderedPlacements(save?.mvpDungeonFloorLayout, save?.mvpDungeonPlacements)
                .Any(p => p != null && string.Equals(p.CategoryId, MvpDungeonPlacementIds.RoomCategoryId, StringComparison.Ordinal));
            var route = new List<MvpOrderedRouteRoom>();
            // Persisted normalization retains actual indices and uses the established last-record-wins rule.
            foreach (MvpDungeonRoomInstance room in layout.Rooms.Where(r => r != null && r.FloorIndex == 0 && r.RoomIndex >= 0 && r.RoomIndex <= MvpRoomSlotLayoutResolver.MvpSecondRoomSlotIndex).OrderBy(r => r.RoomIndex))
            {
                route.Add(new MvpOrderedRouteRoom {
                    FloorIndex = room.FloorIndex, RoomIndex = room.RoomIndex, RoomOptionId = room.RoomOptionId,
                    IncludeRoomPlacement = persisted || explicitRoom,
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
