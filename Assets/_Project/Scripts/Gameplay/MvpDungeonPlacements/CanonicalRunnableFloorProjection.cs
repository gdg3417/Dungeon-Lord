using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Gameplay.MvpDungeonPlacements
{
    // Sole floor-local executable route interpretation. Inputs have already passed canonical
    // and production validation. Edges leaving Completion are never executed.
    public static class CanonicalRunnableFloorProjection
    {
        private const string ContradictoryAuthorityReason = CanonicalMvpRouteProjection.ContradictoryAuthorityReason;
        public static CanonicalMvpRouteProjectionResult Resolve(
            SavedSpatialFloor floor, RunSimulationConfig config,
            ProductionSpatialContentSnapshot production)
        {
            try
            {
                if (floor.Layout == null || floor.RoomContents == null)
                    return Contradictory();
                RoomSpatialInstance[] rooms = floor.Layout.Rooms;
                FloorRouteNode[] nodes = floor.Layout.Nodes;
                FloorRouteEdge[] edges = floor.Layout.Edges;
                CanonicalRoomSemantics[] semanticValues = floor.RoomContents.RoomSemantics;
                RoomContentAssignment[] assignments = floor.RoomContents.Assignments;
                if (rooms == null || nodes == null || edges == null || semanticValues == null ||
                    assignments == null) return Contradictory();

                var roomById = new Dictionary<string, RoomSpatialInstance>(StringComparer.Ordinal);
                foreach (RoomSpatialInstance room in rooms)
                    if (room == null || string.IsNullOrWhiteSpace(room.RoomInstanceId) ||
                        roomById.ContainsKey(room.RoomInstanceId)) return Contradictory();
                    else roomById.Add(room.RoomInstanceId, room);
                var nodeById = new Dictionary<string, FloorRouteNode>(StringComparer.Ordinal);
                var roomNodeIds = new HashSet<string>(StringComparer.Ordinal);
                FloorRouteNode entrance = null, completion = null;
                foreach (FloorRouteNode node in nodes)
                {
                    if (node == null || string.IsNullOrWhiteSpace(node.NodeId) ||
                        nodeById.ContainsKey(node.NodeId)) return Contradictory();
                    nodeById.Add(node.NodeId, node);
                    if (node.Kind == FloorRouteNodeKind.Entrance)
                    { if (entrance != null) return Contradictory(); entrance = node; }
                    if (node.Kind == FloorRouteNodeKind.Completion)
                    { if (completion != null) return Contradictory(); completion = node; }
                    if (node.Kind == FloorRouteNodeKind.Room &&
                        (string.IsNullOrWhiteSpace(node.RoomInstanceId) ||
                         !roomNodeIds.Add(node.RoomInstanceId))) return Contradictory();
                    if (node.Kind != FloorRouteNodeKind.Entrance &&
                        node.Kind != FloorRouteNodeKind.Room &&
                        node.Kind != FloorRouteNodeKind.Completion &&
                        node.Kind != FloorRouteNodeKind.DeadEnd) return Contradictory();
                }
                if (entrance == null || completion == null ||
                    roomNodeIds.Count != roomById.Count ||
                    roomNodeIds.Any(id => !roomById.ContainsKey(id))) return Contradictory();

                var semantics = new Dictionary<string, LegacyRoomOriginKind>(StringComparer.Ordinal);
                foreach (CanonicalRoomSemantics value in semanticValues)
                    if (value == null || string.IsNullOrWhiteSpace(value.RoomInstanceId) ||
                        semantics.ContainsKey(value.RoomInstanceId)) return Contradictory();
                    else semantics.Add(value.RoomInstanceId, value.LegacyRoomOriginKind);
                if (semantics.Count != roomById.Count ||
                    semantics.Keys.Any(id => !roomById.ContainsKey(id))) return Contradictory();

                if (!RequiredFloorTraversal.TryResolve(floor.Layout, out FloorRouteNode[] terminalRoute))
                    return Contradictory();
                var result = new List<MvpOrderedRouteRoom>();
                var visitedNodes = new HashSet<string>(StringComparer.Ordinal);
                var visitedRooms = new HashSet<string>(StringComparer.Ordinal);
                foreach (FloorRouteNode current in terminalRoute)
                {
                    if (!visitedNodes.Add(current.NodeId)) return Contradictory();
                    if (current.Kind == FloorRouteNodeKind.Completion) break;
                    if (current.Kind == FloorRouteNodeKind.Room)
                    {
                        if (!roomById.TryGetValue(current.RoomInstanceId ?? string.Empty,
                            out RoomSpatialInstance room) || !visitedRooms.Add(room.RoomInstanceId) ||
                            !semantics.TryGetValue(room.RoomInstanceId, out LegacyRoomOriginKind origin) ||
                            !RoomDefinitionIsAllowed(floor, room, production)) return Contradictory();
                        RoomContentAssignment[] owned = assignments.Where(value => value != null &&
                            string.Equals(value.RoomInstanceId, room.RoomInstanceId,
                                StringComparison.Ordinal)).OrderBy(value => CategoryRank(value.CategoryId))
                            .ThenBy(value => value.Sequence).ThenBy(value => value.AssignmentId,
                                StringComparer.Ordinal).ToArray();
                        if (owned.Any(value => CategoryRank(value.CategoryId) == int.MaxValue))
                            return Contradictory();
                        result.Add(new MvpOrderedRouteRoom
                        {
                            FloorIndex = floor.FloorIndex, RoomIndex = result.Count,
                            RoomInstanceId = room.RoomInstanceId,
                            Assignments = owned.Select(value => new RunRoomAssignment {
                                AssignmentId = value.AssignmentId, CategoryId = value.CategoryId,
                                OptionId = value.OptionId, Sequence = value.Sequence,
                                RoomLocalPosition = value.RoomLocalPosition }).ToArray(),
                            RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                            IncludeRoomPlacement = origin !=
                                LegacyRoomOriginKind.ImplicitCompatibilityContainer,
                            AssignedMonsterOptionIds = Options(owned,
                                CanonicalSpatialSaveContracts.MonsterCategoryId),
                            AssignedTrapOptionIds = Options(owned,
                                CanonicalSpatialSaveContracts.TrapCategoryId),
                            AssignedLootNodeOptionIds = Options(owned,
                                CanonicalSpatialSaveContracts.LootNodeCategoryId),
                            Capacity = ResolveCapacity(room, config, production),
                            HasActiveContent = owned.Length != 0
                        });
                    }
                }
                if (assignments.Any(value => value == null || !roomById.ContainsKey(value.RoomInstanceId ?? string.Empty)))
                    return Contradictory();
                return Valid(result.ToArray());
            }
            catch
            {
                // Projection is a trust boundary. Malformed state is classified, never surfaced.
                return Contradictory();
            }
        }

        private static MvpRoomSlotCapacity ResolveCapacity(RoomSpatialInstance room,
            RunSimulationConfig legacyConfig, ProductionSpatialContentSnapshot production)
        {
            if (production != null)
            {
                if (!CanonicalRoomCapacityResolver.TryResolve(production, room.RoomDefinitionId,
                    out MvpRoomSlotCapacity capacity, out string ignored))
                    throw new InvalidOperationException();
                capacity.RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId;
                return capacity;
            }
            // Inactive compatibility overload only. Final live cutover injects production content.
            return MvpRoomSlotLayoutResolver.ResolveCapacity(
                MvpDungeonPlacementIds.BasicRoomOptionId, legacyConfig);
        }

        private static bool RoomDefinitionIsAllowed(SavedSpatialFloor floor,
            RoomSpatialInstance room, ProductionSpatialContentSnapshot production)
        {
            if (production == null)
                return string.Equals(room.RoomDefinitionId, "spatial.room.basic", StringComparison.Ordinal);
            RoomSpatialDefinition[] rooms = (production.Catalog.Rooms ?? Array.Empty<RoomSpatialDefinition>())
                .Where(value => value != null && value.RoomDefinitionId == room.RoomDefinitionId).ToArray();
            FloorSpatialConfiguration[] floors = (production.Catalog.Floors ?? Array.Empty<FloorSpatialConfiguration>())
                .Where(value => value != null && value.FloorDefinitionId == floor.FloorDefinitionId &&
                    value.FloorIndex == floor.FloorIndex).ToArray();
            return rooms.Length == 1 && floors.Length == 1 &&
                (floors[0].AllowedRoomDefinitionIds ?? Array.Empty<string>()).Contains(
                    room.RoomDefinitionId);
        }

        private static CanonicalMvpRouteProjectionResult Valid(MvpOrderedRouteRoom[] rooms) =>
            new CanonicalMvpRouteProjectionResult(
                CanonicalMvpRuntimeAuthorityState.ValidatedCanonical, rooms, null);
        private static CanonicalMvpRouteProjectionResult Contradictory() =>
            new CanonicalMvpRouteProjectionResult(
                CanonicalMvpRuntimeAuthorityState.ContradictoryCanonical,
                Array.Empty<MvpOrderedRouteRoom>(), ContradictoryAuthorityReason);
        private static string[] Options(IEnumerable<RoomContentAssignment> values, string category) =>
            values.Where(value => string.Equals(value.CategoryId, category, StringComparison.Ordinal))
                .Select(value => value.OptionId).ToArray();
        private static int CategoryRank(string category) =>
            category == CanonicalSpatialSaveContracts.MonsterCategoryId ? 0 :
            category == CanonicalSpatialSaveContracts.TrapCategoryId ? 1 :
            category == CanonicalSpatialSaveContracts.LootNodeCategoryId ? 2 : int.MaxValue;
    }
}
