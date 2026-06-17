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

    [Serializable]
    public sealed class MvpRoomSlotAssignmentState
    {
        public int FloorIndex;
        public int RoomIndex;
        public string RoomOptionId;
        public string[] MonsterOptionIds = Array.Empty<string>();
        public string[] TrapOptionIds = Array.Empty<string>();
        public string[] LootNodeOptionIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class MvpRoomSlotAssignmentCollection
    {
        public List<MvpRoomSlotAssignmentState> Rooms = new List<MvpRoomSlotAssignmentState>();
        public int NextRevision = 1;
    }

    public static class MvpRoomSlotLayoutResolver
    {
        public static MvpDungeonFloorSlotLayout ResolveDefaultFloor(SaveData save, RunSimulationConfig config)
        {
            MvpDungeonFloorSlotLayout persisted = ResolvePersistedDefaultFloor(save, config);
            if (persisted != null)
            {
                return persisted;
            }

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

        public static MvpDungeonPlacementEntry[] ResolveActivePlacements(SaveData save, RunSimulationConfig config)
        {
            if (config == null)
            {
                return MvpDungeonLayoutResolver.ResolveOrderedPlacements(save?.mvpDungeonFloorLayout, save?.mvpDungeonPlacements);
            }

            MvpDungeonFloorSlotLayout slotLayout = ResolveDefaultFloor(save, config);
            if (slotLayout == null || slotLayout.Rooms == null || slotLayout.Rooms.Length == 0)
            {
                return MvpDungeonLayoutResolver.ResolveOrderedPlacements(save?.mvpDungeonFloorLayout, save?.mvpDungeonPlacements);
            }

            var placements = new List<MvpDungeonPlacementEntry>();
            bool hasPersistedRooms = save?.mvpRoomSlotAssignments?.Rooms != null && save.mvpRoomSlotAssignments.Rooms.Count > 0;
            bool hasExplicitRoomPlacement = MvpDungeonLayoutResolver.ResolveOrderedPlacements(save?.mvpDungeonFloorLayout, save?.mvpDungeonPlacements)
                .Any(placement => placement != null && string.Equals(placement.CategoryId, MvpDungeonPlacementIds.RoomCategoryId, StringComparison.Ordinal));
            MvpDungeonRoomInstance primaryRoom = slotLayout.Rooms.FirstOrDefault(room => room != null && !string.IsNullOrWhiteSpace(room.RoomOptionId));
            if (primaryRoom != null && (hasPersistedRooms || hasExplicitRoomPlacement))
            {
                placements.Add(new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, primaryRoom.RoomOptionId, 0));
            }

            AddFirstAssignedPlacement(placements, slotLayout, MvpDungeonPlacementIds.MonsterCategoryId, room => room.AssignedMonsterOptionIds);
            AddFirstAssignedPlacement(placements, slotLayout, MvpDungeonPlacementIds.TrapCategoryId, room => room.AssignedTrapOptionIds);
            AddFirstAssignedPlacement(placements, slotLayout, MvpDungeonPlacementIds.LootNodeCategoryId, room => room.AssignedLootNodeOptionIds);
            return placements.ToArray();
        }

        public static bool TryAssignToPersistedRoom(SaveData save, RunSimulationConfig config, int roomIndex, string categoryId, string optionId)
        {
            if (save == null ||
                string.Equals(categoryId, MvpDungeonPlacementIds.RoomCategoryId, StringComparison.Ordinal) ||
                !MvpDungeonPlacementIds.IsAllowedCategory(categoryId) ||
                !MvpDungeonPlacementIds.IsAllowedOption(optionId))
            {
                return false;
            }

            MvpDungeonFloorSlotLayout current = ResolveDefaultFloor(save, config);
            int clampedIndex = current?.Rooms == null || current.Rooms.Length == 0
                ? 0
                : Math.Min(Math.Max(0, roomIndex), current.Rooms.Length - 1);
            MvpDungeonRoomInstance target = current?.Rooms != null && current.Rooms.Length > clampedIndex ? current.Rooms[clampedIndex] : null;
            if (target == null || !CanAccept(target, categoryId))
            {
                return false;
            }

            if (save.mvpRoomSlotAssignments == null)
            {
                save.mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection();
            }

            if (save.mvpRoomSlotAssignments.Rooms == null)
            {
                save.mvpRoomSlotAssignments.Rooms = new List<MvpRoomSlotAssignmentState>();
            }

            MvpRoomSlotAssignmentState room = save.mvpRoomSlotAssignments.Rooms.FirstOrDefault(entry => entry != null && entry.FloorIndex == 0 && entry.RoomIndex == clampedIndex);
            if (room == null)
            {
                room = new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = clampedIndex };
                save.mvpRoomSlotAssignments.Rooms.Add(room);
            }

            room.RoomOptionId = string.IsNullOrWhiteSpace(target.RoomOptionId) ? MvpDungeonPlacementIds.BasicRoomOptionId : target.RoomOptionId;
            room.MonsterOptionIds = Normalize(room.MonsterOptionIds);
            room.TrapOptionIds = Normalize(room.TrapOptionIds);
            room.LootNodeOptionIds = Normalize(room.LootNodeOptionIds);

            switch (categoryId)
            {
                case MvpDungeonPlacementIds.MonsterCategoryId:
                    room.MonsterOptionIds = new[] { optionId };
                    break;
                case MvpDungeonPlacementIds.TrapCategoryId:
                    room.TrapOptionIds = new[] { optionId };
                    break;
                case MvpDungeonPlacementIds.LootNodeCategoryId:
                    room.LootNodeOptionIds = new[] { optionId };
                    break;
            }

            save.mvpRoomSlotAssignments.NextRevision = Math.Max(1, save.mvpRoomSlotAssignments.NextRevision + 1);
            return true;
        }

        public static void SetPersistedRoomOptionIfPresent(SaveData save, RunSimulationConfig config, int roomIndex, string roomOptionId)
        {
            List<MvpRoomSlotAssignmentState> rooms = save?.mvpRoomSlotAssignments?.Rooms;
            if (rooms == null ||
                rooms.Count == 0 ||
                !MvpDungeonPlacementIds.IsAllowedOption(roomOptionId) ||
                !MvpDungeonPlacementIds.TryGetCategoryForOption(roomOptionId, out string categoryId) ||
                !string.Equals(categoryId, MvpDungeonPlacementIds.RoomCategoryId, StringComparison.Ordinal))
            {
                return;
            }

            int clampedIndex = Math.Max(0, roomIndex);
            MvpRoomSlotAssignmentState room = rooms.FirstOrDefault(entry => entry != null && entry.FloorIndex == 0 && entry.RoomIndex == clampedIndex);
            if (room == null)
            {
                return;
            }

            room.RoomOptionId = roomOptionId;
            MvpRoomSlotCapacity capacity = ResolveCapacity(roomOptionId, config);
            room.MonsterOptionIds = Clamp(room.MonsterOptionIds, capacity.MonsterCapacity);
            room.TrapOptionIds = Clamp(room.TrapOptionIds, capacity.TrapCapacity);
            room.LootNodeOptionIds = Clamp(room.LootNodeOptionIds, capacity.LootCapacity);
            save.mvpRoomSlotAssignments.NextRevision = Math.Max(1, save.mvpRoomSlotAssignments.NextRevision + 1);
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

        private static void AddFirstAssignedPlacement(
            List<MvpDungeonPlacementEntry> placements,
            MvpDungeonFloorSlotLayout slotLayout,
            string categoryId,
            Func<MvpDungeonRoomInstance, string[]> assignedOptionIds)
        {
            for (int i = 0; i < slotLayout.Rooms.Length; i++)
            {
                MvpDungeonRoomInstance room = slotLayout.Rooms[i];
                if (room == null)
                {
                    continue;
                }

                string optionId = assignedOptionIds(room)?.FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));
                if (!string.IsNullOrWhiteSpace(optionId))
                {
                    placements.Add(new MvpDungeonPlacementEntry(categoryId, optionId, 0));
                    return;
                }
            }
        }

        private static MvpDungeonFloorSlotLayout ResolvePersistedDefaultFloor(SaveData save, RunSimulationConfig config)
        {
            List<MvpRoomSlotAssignmentState> assignedRooms = save?.mvpRoomSlotAssignments?.Rooms;
            if (assignedRooms == null || assignedRooms.Count == 0)
            {
                return null;
            }

            MvpDungeonRoomInstance[] rooms = assignedRooms
                .Where(room => room != null && room.FloorIndex == 0 && room.RoomIndex >= 0)
                .GroupBy(room => room.RoomIndex)
                .OrderBy(group => group.Key)
                .Select(group => group.Last())
                .Select(room =>
                {
                    string roomOptionId = ResolveValidRoomOptionId(room.RoomOptionId);
                    MvpRoomSlotCapacity capacity = ResolveCapacity(roomOptionId, config);
                    return new MvpDungeonRoomInstance
                    {
                        RoomOptionId = roomOptionId,
                        Capacity = capacity,
                        AssignedMonsterOptionIds = Clamp(Normalize(room.MonsterOptionIds, MvpDungeonPlacementIds.MonsterCategoryId), capacity.MonsterCapacity),
                        AssignedTrapOptionIds = Clamp(Normalize(room.TrapOptionIds, MvpDungeonPlacementIds.TrapCategoryId), capacity.TrapCapacity),
                        AssignedLootNodeOptionIds = Clamp(Normalize(room.LootNodeOptionIds, MvpDungeonPlacementIds.LootNodeCategoryId), capacity.LootCapacity)
                    };
                })
                .ToArray();

            return rooms.Length == 0 ? null : new MvpDungeonFloorSlotLayout { FloorIndex = 0, Rooms = rooms };
        }

        private static bool CanAccept(MvpDungeonRoomInstance room, string categoryId)
        {
            if (room?.Capacity == null) return false;
            switch (categoryId)
            {
                case MvpDungeonPlacementIds.MonsterCategoryId: return room.Capacity.MonsterCapacity > 0;
                case MvpDungeonPlacementIds.TrapCategoryId: return room.Capacity.TrapCapacity > 0;
                case MvpDungeonPlacementIds.LootNodeCategoryId: return room.Capacity.LootCapacity > 0;
                default: return false;
            }
        }

        private static string ResolveValidRoomOptionId(string roomOptionId)
        {
            return MvpDungeonPlacementIds.IsAllowedOption(roomOptionId) &&
                   MvpDungeonPlacementIds.TryGetCategoryForOption(roomOptionId, out string categoryId) &&
                   string.Equals(categoryId, MvpDungeonPlacementIds.RoomCategoryId, StringComparison.Ordinal)
                ? roomOptionId
                : MvpDungeonPlacementIds.BasicRoomOptionId;
        }

        private static string[] Normalize(string[] optionIds)
        {
            return optionIds == null ? Array.Empty<string>() : optionIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
        }

        private static string[] Normalize(string[] optionIds, string expectedCategoryId)
        {
            return Normalize(optionIds)
                .Where(id => MvpDungeonPlacementIds.TryGetCategoryForOption(id, out string categoryId) && string.Equals(categoryId, expectedCategoryId, StringComparison.Ordinal))
                .ToArray();
        }

        private static string[] Clamp(string[] optionIds, int capacity)
        {
            return capacity <= 0 ? Array.Empty<string>() : Normalize(optionIds).Take(capacity).ToArray();
        }
    }
}
