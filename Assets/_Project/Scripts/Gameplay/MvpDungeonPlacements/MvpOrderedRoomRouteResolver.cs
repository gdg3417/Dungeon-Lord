using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Gameplay.MvpDungeonPlacements
{
    [Serializable]
    public sealed class RunRoomAssignment
    {
        public string AssignmentId;
        public string CategoryId;
        public string OptionId;
        public long Sequence;
    }

    public sealed class MvpOrderedRouteRoom
    {
        // Canonical projection retains persisted ordering evidence, never writes it back.
        public RunRoomAssignment[] Assignments;
        public int FloorIndex;
        public int RoomIndex;
        public string RoomInstanceId;
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
            if (Assignments != null)
            {
                result.AddRange(OrderedAssignments().Select((a, i) => new MvpDungeonPlacementEntry(a.CategoryId, a.OptionId, i)));
                return result.ToArray();
            }
            Add(result, MvpDungeonPlacementIds.MonsterCategoryId, AssignedMonsterOptionIds);
            Add(result, MvpDungeonPlacementIds.TrapCategoryId, AssignedTrapOptionIds);
            Add(result, MvpDungeonPlacementIds.LootNodeCategoryId, AssignedLootNodeOptionIds);
            return result.ToArray();
        }

        public RunRoomAssignment[] OrderedAssignments()
        {
            if (Assignments != null)
                return Assignments.OrderBy(a => CategoryRank(a.CategoryId)).ThenBy(a => a.Sequence)
                    .ThenBy(a => a.AssignmentId, StringComparer.Ordinal).ToArray();
            // Legacy/test-only adapters lack persisted assignment identities; array order was their contract.
            return ToOrderedPlacements().Where(p => p.CategoryId != MvpDungeonPlacementIds.RoomCategoryId)
                .Select((p, i) => new RunRoomAssignment { CategoryId = p.CategoryId, OptionId = p.OptionId,
                    Sequence = i, AssignmentId = "legacy.assignment." + i.ToString(System.Globalization.CultureInfo.InvariantCulture) }).ToArray();
        }

        private static int CategoryRank(string id) => id == MvpDungeonPlacementIds.MonsterCategoryId ? 0 :
            id == MvpDungeonPlacementIds.TrapCategoryId ? 1 : id == MvpDungeonPlacementIds.LootNodeCategoryId ? 2 :
            throw new ArgumentException("run.phase5b.invalid_assignment");

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
            => Resolve(save, config, null);

        public static MvpOrderedRouteRoom[] Resolve(SaveData save, RunSimulationConfig config,
            ProductionSpatialContentSnapshot production)
        {
            MvpOrderedRouteRoom[] canonical = CanonicalMvpRouteProjection.Resolve(save, config, production);
            if (canonical != null) return canonical;

            List<MvpRoomSlotAssignmentState> persistedRooms = save?.mvpRoomSlotAssignments?.Rooms;
            bool hasPersistedRecords = persistedRooms != null && persistedRooms.Count > 0;
            bool hasSupportedPersistedRecord = hasPersistedRecords && persistedRooms.Any(room => room != null && room.FloorIndex == 0 && room.RoomIndex >= 0 && room.RoomIndex <= MvpRoomSlotLayoutResolver.MvpSecondRoomSlotIndex);
            if (hasPersistedRecords && !hasSupportedPersistedRecord) return Array.Empty<MvpOrderedRouteRoom>();
            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(save, config);
            if (layout?.Rooms == null) return Array.Empty<MvpOrderedRouteRoom>();
            bool persisted = save?.mvpRoomSlotAssignments?.Rooms != null && save.mvpRoomSlotAssignments.Rooms.Count > 0;
            MvpDungeonPlacementEntry[] legacyPlacements = MvpDungeonLayoutResolver.ResolveOrderedPlacements(save?.mvpDungeonFloorLayout, save?.mvpDungeonPlacements);
            bool explicitRoom = legacyPlacements.Any(p => p != null && string.Equals(p.CategoryId, MvpDungeonPlacementIds.RoomCategoryId, StringComparison.Ordinal));
            if (!persisted)
            {
                MvpDungeonRoomInstance displayRoom = layout.Rooms.FirstOrDefault(r => r != null);
                MvpDungeonPlacementEntry[] active = MvpRoomSlotLayoutResolver.ResolveActivePlacements(
                    save, config, production);
                return new[] { new MvpOrderedRouteRoom { FloorIndex = 0, RoomIndex = 0, RoomOptionId = displayRoom?.RoomOptionId,
                    IncludeRoomPlacement = explicitRoom, Capacity = displayRoom?.Capacity,
                    AssignedMonsterOptionIds = Options(active, MvpDungeonPlacementIds.MonsterCategoryId),
                    AssignedTrapOptionIds = Options(active, MvpDungeonPlacementIds.TrapCategoryId),
                    AssignedLootNodeOptionIds = Options(active, MvpDungeonPlacementIds.LootNodeCategoryId),
                    HasActiveContent = active.Any(p => p != null && !string.Equals(p.CategoryId, MvpDungeonPlacementIds.RoomCategoryId, StringComparison.Ordinal)) } };
            }
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

        private static string[] Options(MvpDungeonPlacementEntry[] placements, string categoryId)
        {
            return placements == null ? Array.Empty<string>() : placements.Where(p => p != null && string.Equals(p.CategoryId, categoryId, StringComparison.Ordinal)).Select(p => p.OptionId).ToArray();
        }
    }
}
