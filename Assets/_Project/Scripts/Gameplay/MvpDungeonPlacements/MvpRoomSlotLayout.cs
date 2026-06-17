using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0;

namespace DungeonBuilder.M0.Gameplay.MvpDungeonPlacements
{
    public sealed class MvpRoomSlotCapacity
    {
        public string RoomOptionId;
        public int MonsterCapacity;
        public int TrapCapacity;
        public int LootCapacity;
    }

    public sealed class MvpDungeonRoomInstance
    {
        public string RoomOptionId;
        public MvpRoomSlotCapacity Capacity;
        public string[] AssignedMonsterOptionIds = Array.Empty<string>();
        public string[] AssignedTrapOptionIds = Array.Empty<string>();
        public string[] AssignedLootNodeOptionIds = Array.Empty<string>();
    }

    public sealed class MvpDungeonFloorSlotLayout
    {
        public int FloorIndex;
        public MvpDungeonRoomInstance[] Rooms = Array.Empty<MvpDungeonRoomInstance>();
    }

    public static class MvpRoomSlotLayoutResolver
    {
        public static MvpDungeonFloorSlotLayout ResolveDefaultFloor(SaveData save, RunSimulationConfig config)
        {
            MvpDungeonPlacementEntry[] placements = MvpDungeonLayoutResolver.ResolveOrderedPlacements(save?.mvpDungeonFloorLayout, save?.mvpDungeonPlacements);
            Dictionary<string, MvpDungeonPlacementEntry> byCategory = placements
                .Where(entry => entry != null)
                .GroupBy(entry => entry.CategoryId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(entry => entry.Revision).First(), StringComparer.Ordinal);

            string roomId = byCategory.TryGetValue(MvpDungeonPlacementIds.RoomCategoryId, out MvpDungeonPlacementEntry room)
                ? room.OptionId
                : MvpDungeonPlacementIds.BasicRoomOptionId;

            MvpRoomSlotCapacity primaryCapacity = ResolveCapacity(roomId, config);
            MvpRoomSlotCapacity basicCapacity = ResolveCapacity(MvpDungeonPlacementIds.BasicRoomOptionId, config);
            var rooms = new List<MvpDungeonRoomInstance>
            {
                new MvpDungeonRoomInstance { RoomOptionId = roomId, Capacity = primaryCapacity }
            };

            string monsterId = byCategory.TryGetValue(MvpDungeonPlacementIds.MonsterCategoryId, out MvpDungeonPlacementEntry monster) ? monster.OptionId : string.Empty;
            string trapId = byCategory.TryGetValue(MvpDungeonPlacementIds.TrapCategoryId, out MvpDungeonPlacementEntry trap) ? trap.OptionId : string.Empty;
            string lootId = byCategory.TryGetValue(MvpDungeonPlacementIds.LootNodeCategoryId, out MvpDungeonPlacementEntry loot) ? loot.OptionId : string.Empty;

            Assign(rooms[0], monsterId, trapId, lootId);
            if (!string.IsNullOrWhiteSpace(lootId) && rooms[0].AssignedLootNodeOptionIds.Length == 0 && basicCapacity.LootCapacity > 0)
            {
                var fallback = new MvpDungeonRoomInstance { RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId, Capacity = basicCapacity };
                Assign(fallback, string.Empty, string.Empty, lootId);
                rooms.Add(fallback);
            }

            return new MvpDungeonFloorSlotLayout { FloorIndex = 0, Rooms = rooms.ToArray() };
        }

        public static MvpRoomSlotCapacity ResolveCapacity(string roomOptionId, RunSimulationConfig config)
        {
            MvpRoomSlotCapacityConfig match = config?.MvpRoomSlotCapacities?.FirstOrDefault(entry => entry != null && string.Equals(entry.RoomOptionId, roomOptionId, StringComparison.Ordinal));
            return new MvpRoomSlotCapacity
            {
                RoomOptionId = roomOptionId ?? string.Empty,
                MonsterCapacity = Math.Max(0, match?.MonsterCapacity ?? 0),
                TrapCapacity = Math.Max(0, match?.TrapCapacity ?? 0),
                LootCapacity = Math.Max(0, match?.LootCapacity ?? 0)
            };
        }

        private static void Assign(MvpDungeonRoomInstance room, string monsterId, string trapId, string lootId)
        {
            room.AssignedMonsterOptionIds = !string.IsNullOrWhiteSpace(monsterId) && room.Capacity.MonsterCapacity > 0 ? new[] { monsterId } : Array.Empty<string>();
            room.AssignedTrapOptionIds = !string.IsNullOrWhiteSpace(trapId) && room.Capacity.TrapCapacity > 0 ? new[] { trapId } : Array.Empty<string>();
            room.AssignedLootNodeOptionIds = !string.IsNullOrWhiteSpace(lootId) && room.Capacity.LootCapacity > 0 ? new[] { lootId } : Array.Empty<string>();
        }
    }
}
