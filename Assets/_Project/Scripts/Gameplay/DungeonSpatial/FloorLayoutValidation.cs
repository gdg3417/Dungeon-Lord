using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum FloorLayoutValidationReason
    {
        MissingStableId = 1, DuplicateRoomId = 2, DuplicateNodeId = 3, DuplicateEdgeId = 4, MissingFloorConfiguration = 5,
        MissingRoomDefinition = 6, MissingCorridorDefinition = 7, InvalidFootprintDimensions = 8, NegativeConfigurationValue = 9,
        ReservedTileOutsideFootprint = 10, DuplicateReservedTile = 11, CapacityExceeded = 12, FootprintOverlap = 13,
        MissingSourceNode = 14, MissingDestinationNode = 15, SelfEdge = 16, CrossFloorEdge = 17, EdgeFloorMismatch = 18,
        MissingEntrance = 19, MultipleEntrances = 20, UnreachableRoom = 21, ConnectionLimitExceeded = 22,
        RequiredRouteWithoutTerminal = 23, OptionalEdgeMissingBranchId = 24, RequiredEdgeHasBranchId = 25,
        OptionalBranchAllowanceExceeded = 26, RoomNodeMissingRoom = 27,
        DuplicateRoomDefinitionId = 28, DuplicateCorridorDefinitionId = 29, RoomMissingNode = 30,
        MultipleNodesForRoom = 31, RoomNodeFloorMismatch = 32, RoomFloorMismatch = 33, NodeFloorMismatch = 34,
        InvalidOrientation = 35, InvalidNodeKind = 36, InvalidRouteClassification = 37,
        InvalidRoomFootprint = 38, InvalidCorridorFootprint = 39
    }

    [Serializable]
    public sealed class FloorLayoutValidationIssue
    {
        public FloorLayoutValidationReason Reason;
        public string SubjectId;
        public string RelatedId;
        public bool HasCoordinate;
        public TileCoordinate Coordinate;
    }

    [Serializable]
    public sealed class FloorCapacitySummary
    {
        public int FinalFloorSpaceCapacity;
        public int UsedFloorSpaceCapacity;
        public int RemainingFloorSpaceCapacity;
    }

    public sealed class FloorLayoutValidationResult
    {
        public FloorCapacitySummary Capacity { get; internal set; }
        public FloorLayoutValidationIssue[] Issues { get; internal set; }
        public bool IsValid => Issues.Length == 0;
    }

    public static class FloorLayoutValidator
    {
        public static FloorLayoutValidationResult Validate(FloorSpatialLayout suppliedLayout, FloorSpatialConfiguration floor,
            IEnumerable<RoomSpatialDefinition> suppliedRoomDefinitions, IEnumerable<CorridorSpatialDefinition> suppliedCorridorDefinitions)
        {
            var issues = new List<FloorLayoutValidationIssue>();
            FloorSpatialLayout layout = suppliedLayout ?? new FloorSpatialLayout();
            RoomSpatialInstance[] rooms = layout.Rooms ?? Array.Empty<RoomSpatialInstance>();
            FloorRouteNode[] nodes = layout.Nodes ?? Array.Empty<FloorRouteNode>();
            CorridorEdge[] edges = layout.Edges ?? Array.Empty<CorridorEdge>();
            RoomSpatialDefinition[] roomDefinitions = (suppliedRoomDefinitions ?? Enumerable.Empty<RoomSpatialDefinition>()).Where(x => x != null).ToArray();
            CorridorSpatialDefinition[] corridorDefinitions = (suppliedCorridorDefinitions ?? Enumerable.Empty<CorridorSpatialDefinition>()).Where(x => x != null).ToArray();

            ValidateIdentities(layout, floor, rooms, nodes, edges, roomDefinitions, corridorDefinitions, issues);
            Dictionary<string, RoomSpatialDefinition> roomDefinitionById = Unambiguous(roomDefinitions, x => x.RoomDefinitionId);
            Dictionary<string, CorridorSpatialDefinition> corridorDefinitionById = Unambiguous(corridorDefinitions, x => x.CorridorDefinitionId);
            Dictionary<string, RoomSpatialInstance> roomById = Unambiguous(rooms.Where(x => x != null), x => x.RoomInstanceId);
            Dictionary<string, FloorRouteNode> nodeById = Unambiguous(nodes.Where(x => x != null), x => x.NodeId);
            HashSet<string> uniqueEdgeIds = UniqueIds(edges.Where(x => x != null).Select(x => x.EdgeId));

            int finalCapacity = floor?.FinalFloorSpaceCapacity ?? 0;
            if (floor != null && (floor.FloorIndex < 0 || floor.FinalFloorSpaceCapacity < 0 || floor.OptionalBranchAllowance < 0))
                Add(issues, FloorLayoutValidationReason.NegativeConfigurationValue, floor.FloorDefinitionId);

            int usedCapacity = 0;
            var occupancy = new Dictionary<TileCoordinate, List<string>>();
            foreach (RoomSpatialInstance room in rooms.Where(x => x != null).OrderBy(x => x.RoomInstanceId, StringComparer.Ordinal))
            {
                bool definitionResolved = roomDefinitionById.TryGetValue(room.RoomDefinitionId ?? string.Empty, out RoomSpatialDefinition definition);
                if (!definitionResolved) Add(issues, FloorLayoutValidationReason.MissingRoomDefinition, room.RoomInstanceId, room.RoomDefinitionId);
                else
                {
                    ValidateRoomDefinition(definition, room, issues);
                    usedCapacity += Math.Max(0, definition.FloorSpaceCost);
                    if (!Enum.IsDefined(typeof(CardinalOrientation), room.Orientation))
                        Add(issues, FloorLayoutValidationReason.InvalidOrientation, room.RoomInstanceId);
                    if (!definition.TryResolveGrossTiles(room.Anchor, room.Orientation, out ResolvedTileFootprint footprint))
                        Add(issues, FloorLayoutValidationReason.InvalidRoomFootprint, room.RoomInstanceId, room.RoomDefinitionId);
                    else AddOccupants(occupancy, footprint.OccupiedTiles, room.RoomInstanceId);
                }
            }

            var validEdges = new List<CorridorEdge>();
            foreach (CorridorEdge edge in edges.Where(x => x != null).OrderBy(x => x.EdgeId, StringComparer.Ordinal))
            {
                bool definitionResolved = corridorDefinitionById.TryGetValue(edge.CorridorDefinitionId ?? string.Empty, out CorridorSpatialDefinition definition);
                if (!definitionResolved) Add(issues, FloorLayoutValidationReason.MissingCorridorDefinition, edge.EdgeId, edge.CorridorDefinitionId);
                else
                {
                    if (definition.FloorSpaceCost < 0) Add(issues, FloorLayoutValidationReason.NegativeConfigurationValue, definition.CorridorDefinitionId);
                    usedCapacity += Math.Max(0, definition.FloorSpaceCost);
                }
                bool footprintValid = ValidateCorridorFootprint(edge, issues);
                if (edge.Footprint != null) AddOccupants(occupancy, edge.Footprint.OccupiedTiles, edge.EdgeId);
                bool sourceResolved = ResolveEndpoint(edge.SourceNodeId, edge.EdgeId, true, nodeById, issues, out FloorRouteNode source);
                bool destinationResolved = ResolveEndpoint(edge.DestinationNodeId, edge.EdgeId, false, nodeById, issues, out FloorRouteNode destination);
                bool self = !string.IsNullOrWhiteSpace(edge.SourceNodeId) && string.Equals(edge.SourceNodeId, edge.DestinationNodeId, StringComparison.Ordinal);
                if (self) Add(issues, FloorLayoutValidationReason.SelfEdge, edge.EdgeId, edge.SourceNodeId);
                bool edgeFloorValid = string.Equals(edge.FloorId, layout.FloorId, StringComparison.Ordinal);
                if (!edgeFloorValid) Add(issues, FloorLayoutValidationReason.EdgeFloorMismatch, edge.EdgeId, edge.FloorId);
                bool endpointsOnFloor = (!sourceResolved || string.Equals(source.FloorId, layout.FloorId, StringComparison.Ordinal)) &&
                    (!destinationResolved || string.Equals(destination.FloorId, layout.FloorId, StringComparison.Ordinal));
                if ((sourceResolved || destinationResolved) && !endpointsOnFloor) Add(issues, FloorLayoutValidationReason.CrossFloorEdge, edge.EdgeId);
                bool classificationValid = Enum.IsDefined(typeof(RouteClassification), edge.Classification);
                if (!classificationValid) Add(issues, FloorLayoutValidationReason.InvalidRouteClassification, edge.EdgeId);
                bool branchValid = ValidateBranch(edge, classificationValid, issues);
                if (uniqueEdgeIds.Contains(edge.EdgeId) && definitionResolved && sourceResolved && destinationResolved && !self &&
                    edgeFloorValid && endpointsOnFloor && classificationValid && branchValid && footprintValid) validEdges.Add(edge);
            }

            EmitOverlaps(occupancy, issues);
            if (usedCapacity > finalCapacity) Add(issues, FloorLayoutValidationReason.CapacityExceeded, layout.FloorId);
            ValidateRoomNodeBijection(layout, rooms, nodes, roomById, issues);
            ValidateGraph(layout, floor, rooms, nodes, roomById, nodeById, roomDefinitionById, validEdges.ToArray(), issues);
            return new FloorLayoutValidationResult
            {
                Capacity = new FloorCapacitySummary { FinalFloorSpaceCapacity = finalCapacity, UsedFloorSpaceCapacity = usedCapacity, RemainingFloorSpaceCapacity = finalCapacity - usedCapacity },
                Issues = Order(issues)
            };
        }

        private static void ValidateIdentities(FloorSpatialLayout layout, FloorSpatialConfiguration floor, RoomSpatialInstance[] rooms,
            FloorRouteNode[] nodes, CorridorEdge[] edges, RoomSpatialDefinition[] roomDefinitions,
            CorridorSpatialDefinition[] corridorDefinitions, List<FloorLayoutValidationIssue> issues)
        {
            CheckId(layout.FloorId, layout.FloorId, issues);
            if (floor == null) Add(issues, FloorLayoutValidationReason.MissingFloorConfiguration, layout.FloorId);
            else CheckId(floor.FloorDefinitionId, floor.FloorDefinitionId, issues);
            foreach (RoomSpatialDefinition definition in roomDefinitions) CheckId(definition.RoomDefinitionId, definition.RoomDefinitionId, issues);
            foreach (CorridorSpatialDefinition definition in corridorDefinitions) CheckId(definition.CorridorDefinitionId, definition.CorridorDefinitionId, issues);
            CheckDuplicates(roomDefinitions.Select(x => x.RoomDefinitionId), FloorLayoutValidationReason.DuplicateRoomDefinitionId, issues);
            CheckDuplicates(corridorDefinitions.Select(x => x.CorridorDefinitionId), FloorLayoutValidationReason.DuplicateCorridorDefinitionId, issues);
            CheckDuplicates(rooms.Select(x => x?.RoomInstanceId), FloorLayoutValidationReason.DuplicateRoomId, issues);
            CheckDuplicates(nodes.Select(x => x?.NodeId), FloorLayoutValidationReason.DuplicateNodeId, issues);
            CheckDuplicates(edges.Select(x => x?.EdgeId), FloorLayoutValidationReason.DuplicateEdgeId, issues);
            foreach (RoomSpatialInstance room in rooms.Where(x => x != null))
            {
                CheckId(room.RoomInstanceId, room.RoomInstanceId, issues); CheckId(room.RoomDefinitionId, room.RoomInstanceId, issues); CheckId(room.FloorId, room.RoomInstanceId, issues);
                if (!string.Equals(room.FloorId, layout.FloorId, StringComparison.Ordinal)) Add(issues, FloorLayoutValidationReason.RoomFloorMismatch, room.RoomInstanceId, room.FloorId);
            }
            foreach (FloorRouteNode node in nodes.Where(x => x != null))
            {
                CheckId(node.NodeId, node.NodeId, issues); CheckId(node.FloorId, node.NodeId, issues);
                if (!Enum.IsDefined(typeof(FloorRouteNodeKind), node.Kind)) Add(issues, FloorLayoutValidationReason.InvalidNodeKind, node.NodeId);
                if (!string.Equals(node.FloorId, layout.FloorId, StringComparison.Ordinal)) Add(issues, FloorLayoutValidationReason.NodeFloorMismatch, node.NodeId, node.FloorId);
                if (node.Kind == FloorRouteNodeKind.Room) CheckId(node.RoomInstanceId, node.NodeId, issues);
            }
            foreach (CorridorEdge edge in edges.Where(x => x != null))
            {
                CheckId(edge.EdgeId, edge.EdgeId, issues); CheckId(edge.CorridorDefinitionId, edge.EdgeId, issues); CheckId(edge.FloorId, edge.EdgeId, issues);
                CheckId(edge.SourceNodeId, edge.EdgeId, issues); CheckId(edge.DestinationNodeId, edge.EdgeId, issues);
            }
        }

        private static void ValidateRoomDefinition(RoomSpatialDefinition definition, RoomSpatialInstance room, List<FloorLayoutValidationIssue> issues)
        {
            if (definition.GrossFootprint == null || definition.GrossFootprint.Width <= 0 || definition.GrossFootprint.Height <= 0)
                Add(issues, FloorLayoutValidationReason.InvalidFootprintDimensions, definition.RoomDefinitionId);
            if (definition.FloorSpaceCost < 0 || definition.MaximumConnectionCount < 0 || definition.MonsterCapacity < 0 || definition.TrapCapacity < 0 || definition.LootCapacity < 0)
                Add(issues, FloorLayoutValidationReason.NegativeConfigurationValue, definition.RoomDefinitionId);
            var seen = new HashSet<TileCoordinate>();
            foreach (TileCoordinate offset in definition.ReservedTileOffsets ?? Array.Empty<TileCoordinate>())
            {
                if (!seen.Add(offset)) Add(issues, FloorLayoutValidationReason.DuplicateReservedTile, definition.RoomDefinitionId, room.RoomInstanceId, offset);
                if (definition.GrossFootprint == null || offset.X < 0 || offset.Y < 0 || offset.X >= definition.GrossFootprint.Width || offset.Y >= definition.GrossFootprint.Height)
                    Add(issues, FloorLayoutValidationReason.ReservedTileOutsideFootprint, definition.RoomDefinitionId, room.RoomInstanceId, offset);
            }
        }

        private static bool ValidateCorridorFootprint(CorridorEdge edge, List<FloorLayoutValidationIssue> issues)
        {
            TileCoordinate[] tiles = edge.Footprint?.OccupiedTiles;
            bool valid = tiles != null && tiles.Length >= 2 && tiles.Distinct().Count() == tiles.Length &&
                tiles.SequenceEqual(tiles.OrderBy(x => x));
            if (valid)
            {
                bool horizontal = tiles.All(x => x.Y == tiles[0].Y), vertical = tiles.All(x => x.X == tiles[0].X);
                if (!horizontal && !vertical) valid = false;
                else
                {
                    TileCoordinate[] canonical = tiles.OrderBy(x => x).ToArray();
                    int extent = horizontal ? canonical[canonical.Length - 1].X - canonical[0].X : canonical[canonical.Length - 1].Y - canonical[0].Y;
                    valid = extent + 1 == canonical.Length;
                }
            }
            if (!valid) Add(issues, FloorLayoutValidationReason.InvalidCorridorFootprint, edge.EdgeId);
            return valid;
        }

        private static bool ResolveEndpoint(string nodeId, string edgeId, bool source, Dictionary<string, FloorRouteNode> nodeById,
            List<FloorLayoutValidationIssue> issues, out FloorRouteNode node)
        {
            node = null;
            bool resolved = !string.IsNullOrWhiteSpace(nodeId) && nodeById.TryGetValue(nodeId, out node);
            if (!resolved) Add(issues, source ? FloorLayoutValidationReason.MissingSourceNode : FloorLayoutValidationReason.MissingDestinationNode, edgeId, nodeId);
            return resolved;
        }

        private static bool ValidateBranch(CorridorEdge edge, bool classificationValid, List<FloorLayoutValidationIssue> issues)
        {
            if (!classificationValid) return false;
            if (edge.Classification == RouteClassification.Optional && string.IsNullOrWhiteSpace(edge.OptionalBranchId))
            { Add(issues, FloorLayoutValidationReason.OptionalEdgeMissingBranchId, edge.EdgeId); return false; }
            if (edge.Classification == RouteClassification.Required && !string.IsNullOrWhiteSpace(edge.OptionalBranchId))
            { Add(issues, FloorLayoutValidationReason.RequiredEdgeHasBranchId, edge.EdgeId, edge.OptionalBranchId); return false; }
            return true;
        }

        private static void ValidateRoomNodeBijection(FloorSpatialLayout layout, RoomSpatialInstance[] rooms, FloorRouteNode[] nodes,
            Dictionary<string, RoomSpatialInstance> roomById, List<FloorLayoutValidationIssue> issues)
        {
            FloorRouteNode[] roomNodes = nodes.Where(x => x != null && x.Kind == FloorRouteNodeKind.Room).ToArray();
            foreach (RoomSpatialInstance room in rooms.Where(x => x != null && !string.IsNullOrWhiteSpace(x.RoomInstanceId)).OrderBy(x => x.RoomInstanceId, StringComparer.Ordinal))
            {
                FloorRouteNode[] matches = roomNodes.Where(x => string.Equals(x.RoomInstanceId, room.RoomInstanceId, StringComparison.Ordinal)).ToArray();
                if (matches.Length == 0) Add(issues, FloorLayoutValidationReason.RoomMissingNode, room.RoomInstanceId);
                if (matches.Length > 1) Add(issues, FloorLayoutValidationReason.MultipleNodesForRoom, room.RoomInstanceId);
                foreach (FloorRouteNode node in matches.Where(x => !string.Equals(x.FloorId, room.FloorId, StringComparison.Ordinal)))
                    Add(issues, FloorLayoutValidationReason.RoomNodeFloorMismatch, room.RoomInstanceId, node.NodeId);
            }
            foreach (FloorRouteNode node in roomNodes.OrderBy(x => x.NodeId, StringComparer.Ordinal))
                if (string.IsNullOrWhiteSpace(node.RoomInstanceId) || !roomById.ContainsKey(node.RoomInstanceId))
                    Add(issues, FloorLayoutValidationReason.RoomNodeMissingRoom, node.NodeId, node.RoomInstanceId);
        }

        private static void ValidateGraph(FloorSpatialLayout layout, FloorSpatialConfiguration floor, RoomSpatialInstance[] rooms,
            FloorRouteNode[] nodes, Dictionary<string, RoomSpatialInstance> roomById, Dictionary<string, FloorRouteNode> nodeById,
            Dictionary<string, RoomSpatialDefinition> roomDefinitions, CorridorEdge[] validEdges, List<FloorLayoutValidationIssue> issues)
        {
            FloorRouteNode[] entrances = nodes.Where(x => x != null && x.Kind == FloorRouteNodeKind.Entrance &&
                UniqueIds(nodes.Where(y => y != null).Select(y => y.NodeId)).Contains(x.NodeId) && string.Equals(x.FloorId, layout.FloorId, StringComparison.Ordinal)).ToArray();
            if (entrances.Length == 0) Add(issues, FloorLayoutValidationReason.MissingEntrance, layout.FloorId);
            if (entrances.Length > 1) Add(issues, FloorLayoutValidationReason.MultipleEntrances, layout.FloorId);
            HashSet<string> reachable = entrances.Length == 1 ? Reachable(entrances[0].NodeId, validEdges, null) : new HashSet<string>(StringComparer.Ordinal);
            foreach (FloorRouteNode node in nodes.Where(x => x != null && x.Kind == FloorRouteNodeKind.Room && !reachable.Contains(x.NodeId)))
                Add(issues, FloorLayoutValidationReason.UnreachableRoom, node.NodeId, node.RoomInstanceId);
            foreach (FloorRouteNode node in nodes.Where(x => x != null && x.Kind == FloorRouteNodeKind.Room))
                if (roomById.TryGetValue(node.RoomInstanceId ?? string.Empty, out RoomSpatialInstance room) && roomDefinitions.TryGetValue(room.RoomDefinitionId ?? string.Empty, out RoomSpatialDefinition definition) &&
                    validEdges.Count(x => string.Equals(x.SourceNodeId, node.NodeId, StringComparison.Ordinal) || string.Equals(x.DestinationNodeId, node.NodeId, StringComparison.Ordinal)) > definition.MaximumConnectionCount)
                    Add(issues, FloorLayoutValidationReason.ConnectionLimitExceeded, node.NodeId, room.RoomInstanceId);
            HashSet<string> requiredReachable = entrances.Length == 1 ? Reachable(entrances[0].NodeId, validEdges, RouteClassification.Required) : new HashSet<string>(StringComparer.Ordinal);
            var terminals = new HashSet<string>(nodes.Where(x => x != null && (x.Kind == FloorRouteNodeKind.Exit || x.Kind == FloorRouteNodeKind.Descent || x.Kind == FloorRouteNodeKind.Completion) && string.Equals(x.FloorId, layout.FloorId, StringComparison.Ordinal)).Select(x => x.NodeId), StringComparer.Ordinal);
            foreach (string nodeId in requiredReachable.OrderBy(x => x, StringComparer.Ordinal))
                if (!Reachable(nodeId, validEdges, RouteClassification.Required).Overlaps(terminals)) Add(issues, FloorLayoutValidationReason.RequiredRouteWithoutTerminal, nodeId);
            int branchCount = validEdges.Where(x => x.Classification == RouteClassification.Optional).Select(x => x.OptionalBranchId).Distinct(StringComparer.Ordinal).Count();
            if (floor != null && branchCount > floor.OptionalBranchAllowance) Add(issues, FloorLayoutValidationReason.OptionalBranchAllowanceExceeded, layout.FloorId);
        }

        private static HashSet<string> Reachable(string start, CorridorEdge[] edges, RouteClassification? classification)
        {
            var result = new HashSet<string>(StringComparer.Ordinal) { start }; var queue = new Queue<string>(); queue.Enqueue(start);
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                foreach (CorridorEdge edge in edges.Where(x => (!classification.HasValue || x.Classification == classification.Value) && string.Equals(x.SourceNodeId, current, StringComparison.Ordinal)).OrderBy(x => x.DestinationNodeId, StringComparer.Ordinal).ThenBy(x => x.EdgeId, StringComparer.Ordinal))
                    if (result.Add(edge.DestinationNodeId)) queue.Enqueue(edge.DestinationNodeId);
            }
            return result;
        }

        private static void AddOccupants(Dictionary<TileCoordinate, List<string>> occupancy, IEnumerable<TileCoordinate> tiles, string id)
        {
            foreach (TileCoordinate tile in tiles ?? Enumerable.Empty<TileCoordinate>())
            { if (!occupancy.TryGetValue(tile, out List<string> occupants)) occupancy.Add(tile, occupants = new List<string>()); occupants.Add(id ?? string.Empty); }
        }

        private static void EmitOverlaps(Dictionary<TileCoordinate, List<string>> occupancy, List<FloorLayoutValidationIssue> issues)
        {
            foreach (KeyValuePair<TileCoordinate, List<string>> entry in occupancy.OrderBy(x => x.Key))
            {
                string[] occupants = entry.Value.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
                for (int first = 0; first < occupants.Length; first++) for (int second = first + 1; second < occupants.Length; second++)
                    Add(issues, FloorLayoutValidationReason.FootprintOverlap, occupants[first], occupants[second], entry.Key);
            }
        }

        private static Dictionary<string, T> Unambiguous<T>(IEnumerable<T> records, Func<T, string> idSelector) => records
            .Where(x => !string.IsNullOrWhiteSpace(idSelector(x))).GroupBy(idSelector, StringComparer.Ordinal).Where(x => x.Count() == 1)
            .ToDictionary(x => x.Key, x => x.Single(), StringComparer.Ordinal);
        private static HashSet<string> UniqueIds(IEnumerable<string> ids) => new HashSet<string>(ids.Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(x => x, StringComparer.Ordinal).Where(x => x.Count() == 1).Select(x => x.Key), StringComparer.Ordinal);
        private static void CheckId(string id, string subject, List<FloorLayoutValidationIssue> issues) { if (string.IsNullOrWhiteSpace(id)) Add(issues, FloorLayoutValidationReason.MissingStableId, subject); }
        private static void CheckDuplicates(IEnumerable<string> ids, FloorLayoutValidationReason reason, List<FloorLayoutValidationIssue> issues) { foreach (string id in ids.Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(x => x, StringComparer.Ordinal).Where(x => x.Count() > 1).Select(x => x.Key).OrderBy(x => x, StringComparer.Ordinal)) Add(issues, reason, id); }
        private static void Add(List<FloorLayoutValidationIssue> issues, FloorLayoutValidationReason reason, string subject = null, string related = null, TileCoordinate? coordinate = null) => issues.Add(new FloorLayoutValidationIssue { Reason = reason, SubjectId = subject, RelatedId = related, HasCoordinate = coordinate.HasValue, Coordinate = coordinate ?? default });
        private static FloorLayoutValidationIssue[] Order(IEnumerable<FloorLayoutValidationIssue> issues) => issues.OrderBy(x => (int)x.Reason).ThenBy(x => x.SubjectId, StringComparer.Ordinal).ThenBy(x => x.RelatedId, StringComparer.Ordinal).ThenBy(x => x.HasCoordinate ? 1 : 0).ThenBy(x => x.Coordinate).ToArray();
    }
}
