using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Gameplay.MvpDungeonPlacements
{
    /// <summary>
    /// The single MVP compatibility projection from schema-7 graph/content authority. It never
    /// reads any legacy route representation and therefore cannot make legacy evidence playable.
    /// </summary>
    public static class CanonicalMvpRouteProjection
    {
        public static bool IsCanonical(SaveData save) => save?.canonicalSpatialAuthority != null;

        public static MvpOrderedRouteRoom[] Resolve(SaveData save, RunSimulationConfig config)
        {
            if (!IsCanonical(save)) return null;
            SavedSpatialFloor floor = (save.spatialFloors ?? Array.Empty<SavedSpatialFloor>())
                .SingleOrDefault(value => value != null && value.FloorIndex == 0);
            if (floor == null) return Array.Empty<MvpOrderedRouteRoom>();
            FloorRouteNode[] nodes = floor.Layout?.Nodes ?? Array.Empty<FloorRouteNode>();
            FloorRouteEdge[] edges = floor.Layout?.Edges ?? Array.Empty<FloorRouteEdge>();
            RoomSpatialInstance[] rooms = floor.Layout?.Rooms ?? Array.Empty<RoomSpatialInstance>();
            FloorRouteNode entrance = nodes.SingleOrDefault(node => node != null &&
                node.Kind == FloorRouteNodeKind.Entrance);
            if (entrance == null) return Array.Empty<MvpOrderedRouteRoom>();

            var nodeById = nodes.Where(node => node != null && !string.IsNullOrWhiteSpace(node.NodeId))
                .ToDictionary(node => node.NodeId, StringComparer.Ordinal);
            var roomById = rooms.Where(room => room != null && !string.IsNullOrWhiteSpace(room.RoomInstanceId))
                .ToDictionary(room => room.RoomInstanceId, StringComparer.Ordinal);
            var semantics = (floor.RoomContents?.RoomSemantics ?? Array.Empty<CanonicalRoomSemantics>())
                .Where(value => value != null).ToDictionary(value => value.RoomInstanceId,
                    value => value.LegacyRoomOriginKind, StringComparer.Ordinal);
            RoomContentAssignment[] assignments = floor.RoomContents?.Assignments ??
                Array.Empty<RoomContentAssignment>();

            var result = new List<MvpOrderedRouteRoom>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            FloorRouteNode current = entrance;
            while (current != null && visited.Add(current.NodeId))
            {
                if (current.Kind == FloorRouteNodeKind.Room &&
                    roomById.TryGetValue(current.RoomInstanceId ?? string.Empty, out RoomSpatialInstance room))
                {
                    RoomContentAssignment[] owned = assignments.Where(value => value != null &&
                        string.Equals(value.RoomInstanceId, room.RoomInstanceId, StringComparison.Ordinal))
                        .OrderBy(value => CategoryRank(value.CategoryId)).ThenBy(value => value.Sequence)
                        .ThenBy(value => value.AssignmentId, StringComparer.Ordinal).ToArray();
                    semantics.TryGetValue(room.RoomInstanceId, out LegacyRoomOriginKind origin);
                    string roomOption = RoomOption(room.RoomDefinitionId);
                    result.Add(new MvpOrderedRouteRoom
                    {
                        FloorIndex = floor.FloorIndex,
                        RoomIndex = result.Count,
                        RoomOptionId = roomOption,
                        IncludeRoomPlacement = origin != LegacyRoomOriginKind.ImplicitCompatibilityContainer,
                        AssignedMonsterOptionIds = Options(owned, CanonicalSpatialSaveContracts.MonsterCategoryId),
                        AssignedTrapOptionIds = Options(owned, CanonicalSpatialSaveContracts.TrapCategoryId),
                        AssignedLootNodeOptionIds = Options(owned, CanonicalSpatialSaveContracts.LootNodeCategoryId),
                        Capacity = MvpRoomSlotLayoutResolver.ResolveCapacity(roomOption, config),
                        HasActiveContent = owned.Length != 0
                    });
                }

                FloorRouteEdge[] outgoing = edges.Where(edge => edge != null &&
                    edge.Classification == RouteClassification.Required &&
                    string.Equals(edge.SourceNodeId, current.NodeId, StringComparison.Ordinal))
                    .OrderBy(edge => edge.EdgeId, StringComparer.Ordinal).ToArray();
                if (outgoing.Length == 0) break;
                if (outgoing.Length != 1 || !nodeById.TryGetValue(outgoing[0].DestinationNodeId ?? string.Empty,
                    out current)) return Array.Empty<MvpOrderedRouteRoom>();
            }
            return result.ToArray();
        }

        public static MvpDungeonPlacementEntry[] ResolveActivePlacements(SaveData save,
            RunSimulationConfig config)
        {
            MvpOrderedRouteRoom[] route = Resolve(save, config);
            if (route == null) return null;
            var result = new List<MvpDungeonPlacementEntry>();
            foreach (MvpOrderedRouteRoom room in route)
                result.AddRange(room.ToOrderedPlacements());
            return result.ToArray();
        }

        private static string[] Options(IEnumerable<RoomContentAssignment> values, string category) =>
            values.Where(value => string.Equals(value.CategoryId, category, StringComparison.Ordinal))
                .Select(value => value.OptionId).ToArray();

        private static int CategoryRank(string category) =>
            category == CanonicalSpatialSaveContracts.MonsterCategoryId ? 0 :
            category == CanonicalSpatialSaveContracts.TrapCategoryId ? 1 :
            category == CanonicalSpatialSaveContracts.LootNodeCategoryId ? 2 : int.MaxValue;

        private static string RoomOption(string definitionId) =>
            string.Equals(definitionId, "spatial.room.basic", StringComparison.Ordinal)
                ? MvpDungeonPlacementIds.BasicRoomOptionId : string.Empty;
    }
}
