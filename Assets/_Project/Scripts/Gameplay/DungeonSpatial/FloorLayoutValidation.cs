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
        OptionalBranchAllowanceExceeded = 26, RoomNodeMissingRoom = 27
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
        public static FloorLayoutValidationResult Validate(FloorSpatialLayout layout, FloorSpatialConfiguration floor,
            IEnumerable<RoomSpatialDefinition> roomDefinitions, IEnumerable<CorridorSpatialDefinition> corridorDefinitions)
        {
            var issues = new List<FloorLayoutValidationIssue>();
            layout = layout ?? new FloorSpatialLayout();
            RoomSpatialInstance[] rooms = layout.Rooms ?? Array.Empty<RoomSpatialInstance>();
            FloorRouteNode[] nodes = layout.Nodes ?? Array.Empty<FloorRouteNode>();
            CorridorEdge[] edges = layout.Edges ?? Array.Empty<CorridorEdge>();
            var roomDefs = (roomDefinitions ?? Enumerable.Empty<RoomSpatialDefinition>()).Where(x => x != null && !string.IsNullOrWhiteSpace(x.RoomDefinitionId)).GroupBy(x => x.RoomDefinitionId, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
            var corridorDefs = (corridorDefinitions ?? Enumerable.Empty<CorridorSpatialDefinition>()).Where(x => x != null && !string.IsNullOrWhiteSpace(x.CorridorDefinitionId)).GroupBy(x => x.CorridorDefinitionId, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
            if (floor == null) Add(issues, FloorLayoutValidationReason.MissingFloorConfiguration, layout.FloorId);
            CheckId(layout.FloorId, layout.FloorId, issues);
            CheckDuplicates(rooms.Select(x => x?.RoomInstanceId), FloorLayoutValidationReason.DuplicateRoomId, issues);
            CheckDuplicates(nodes.Select(x => x?.NodeId), FloorLayoutValidationReason.DuplicateNodeId, issues);
            CheckDuplicates(edges.Select(x => x?.EdgeId), FloorLayoutValidationReason.DuplicateEdgeId, issues);
            foreach (var room in rooms) if (room != null) CheckId(room.RoomInstanceId, room.RoomInstanceId, issues);
            foreach (var node in nodes) if (node != null) CheckId(node.NodeId, node.NodeId, issues);
            foreach (var edge in edges) if (edge != null) CheckId(edge.EdgeId, edge.EdgeId, issues);

            int final = floor?.FinalFloorSpaceCapacity ?? 0, used = 0;
            if (floor != null && (floor.FinalFloorSpaceCapacity < 0 || floor.OptionalBranchAllowance < 0)) Add(issues, FloorLayoutValidationReason.NegativeConfigurationValue, floor.FloorDefinitionId);
            var occupied = new Dictionary<TileCoordinate, string>();
            foreach (var room in rooms.Where(x => x != null))
            {
                if (!roomDefs.TryGetValue(room.RoomDefinitionId ?? string.Empty, out RoomSpatialDefinition definition)) { Add(issues, FloorLayoutValidationReason.MissingRoomDefinition, room.RoomInstanceId, room.RoomDefinitionId); continue; }
                if (definition.GrossFootprint == null || definition.GrossFootprint.Width <= 0 || definition.GrossFootprint.Height <= 0) Add(issues, FloorLayoutValidationReason.InvalidFootprintDimensions, definition.RoomDefinitionId);
                if (definition.FloorSpaceCost < 0 || definition.MaximumConnectionCount < 0 || definition.MonsterCapacity < 0 || definition.TrapCapacity < 0 || definition.LootCapacity < 0) Add(issues, FloorLayoutValidationReason.NegativeConfigurationValue, definition.RoomDefinitionId);
                used += definition.FloorSpaceCost;
                ValidateReserved(definition, room, issues);
                if (definition.TryResolveGrossTiles(room.Anchor, room.Orientation, out ResolvedTileFootprint footprint)) AddOccupancy(footprint, room.RoomInstanceId, occupied, issues);
            }
            foreach (var edge in edges.Where(x => x != null))
            {
                if (!corridorDefs.TryGetValue(edge.CorridorDefinitionId ?? string.Empty, out CorridorSpatialDefinition definition)) Add(issues, FloorLayoutValidationReason.MissingCorridorDefinition, edge.EdgeId, edge.CorridorDefinitionId);
                else { used += definition.FloorSpaceCost; if (definition.FloorSpaceCost < 0) Add(issues, FloorLayoutValidationReason.NegativeConfigurationValue, definition.CorridorDefinitionId); }
                AddOccupancy(edge.Footprint, edge.EdgeId, occupied, issues);
            }
            if (used > final) Add(issues, FloorLayoutValidationReason.CapacityExceeded, layout.FloorId);
            ValidateGraph(layout, floor, rooms, nodes, edges, roomDefs, issues);
            return new FloorLayoutValidationResult { Capacity = new FloorCapacitySummary { FinalFloorSpaceCapacity = final, UsedFloorSpaceCapacity = used, RemainingFloorSpaceCapacity = final - used }, Issues = Order(issues) };
        }

        private static void ValidateReserved(RoomSpatialDefinition definition, RoomSpatialInstance room, List<FloorLayoutValidationIssue> issues)
        {
            var seen = new HashSet<TileCoordinate>();
            foreach (TileCoordinate offset in definition.ReservedTileOffsets ?? Array.Empty<TileCoordinate>())
            {
                if (!seen.Add(offset)) Add(issues, FloorLayoutValidationReason.DuplicateReservedTile, definition.RoomDefinitionId, room.RoomInstanceId, offset);
                if (definition.GrossFootprint == null || offset.X < 0 || offset.Y < 0 || offset.X >= definition.GrossFootprint.Width || offset.Y >= definition.GrossFootprint.Height)
                    Add(issues, FloorLayoutValidationReason.ReservedTileOutsideFootprint, definition.RoomDefinitionId, room.RoomInstanceId, offset);
            }
        }

        private static void ValidateGraph(FloorSpatialLayout layout, FloorSpatialConfiguration floor, RoomSpatialInstance[] rooms, FloorRouteNode[] nodes, CorridorEdge[] edges, Dictionary<string, RoomSpatialDefinition> roomDefs, List<FloorLayoutValidationIssue> issues)
        {
            var nodeById = nodes.Where(x => x != null && !string.IsNullOrWhiteSpace(x.NodeId)).GroupBy(x => x.NodeId, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
            foreach (var node in nodes.Where(x => x != null && x.Kind == FloorRouteNodeKind.Room)) if (string.IsNullOrWhiteSpace(node.RoomInstanceId) || !rooms.Any(r => r != null && string.Equals(r.RoomInstanceId, node.RoomInstanceId, StringComparison.Ordinal))) Add(issues, FloorLayoutValidationReason.RoomNodeMissingRoom, node.NodeId, node.RoomInstanceId);
            foreach (var edge in edges.Where(x => x != null))
            {
                if (!nodeById.TryGetValue(edge.SourceNodeId ?? string.Empty, out FloorRouteNode source)) Add(issues, FloorLayoutValidationReason.MissingSourceNode, edge.EdgeId, edge.SourceNodeId);
                if (!nodeById.TryGetValue(edge.DestinationNodeId ?? string.Empty, out FloorRouteNode destination)) Add(issues, FloorLayoutValidationReason.MissingDestinationNode, edge.EdgeId, edge.DestinationNodeId);
                if (string.Equals(edge.SourceNodeId, edge.DestinationNodeId, StringComparison.Ordinal)) Add(issues, FloorLayoutValidationReason.SelfEdge, edge.EdgeId, edge.SourceNodeId);
                if (!string.Equals(edge.FloorId, layout.FloorId, StringComparison.Ordinal)) Add(issues, FloorLayoutValidationReason.EdgeFloorMismatch, edge.EdgeId, edge.FloorId);
                if ((source != null && !string.Equals(source.FloorId, layout.FloorId, StringComparison.Ordinal)) || (destination != null && !string.Equals(destination.FloorId, layout.FloorId, StringComparison.Ordinal))) Add(issues, FloorLayoutValidationReason.CrossFloorEdge, edge.EdgeId);
                if (edge.Classification == RouteClassification.Optional && string.IsNullOrWhiteSpace(edge.OptionalBranchId)) Add(issues, FloorLayoutValidationReason.OptionalEdgeMissingBranchId, edge.EdgeId);
                if (edge.Classification == RouteClassification.Required && !string.IsNullOrWhiteSpace(edge.OptionalBranchId)) Add(issues, FloorLayoutValidationReason.RequiredEdgeHasBranchId, edge.EdgeId, edge.OptionalBranchId);
            }
            var entrances = nodes.Where(x => x != null && x.Kind == FloorRouteNodeKind.Entrance && string.Equals(x.FloorId, layout.FloorId, StringComparison.Ordinal)).ToArray();
            if (entrances.Length == 0) Add(issues, FloorLayoutValidationReason.MissingEntrance, layout.FloorId);
            if (entrances.Length > 1) Add(issues, FloorLayoutValidationReason.MultipleEntrances, layout.FloorId);
            var reachable = entrances.Length == 1 ? Reachable(entrances[0].NodeId, edges, null) : new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in nodes.Where(x => x != null && x.Kind == FloorRouteNodeKind.Room && !reachable.Contains(x.NodeId))) Add(issues, FloorLayoutValidationReason.UnreachableRoom, node.NodeId, node.RoomInstanceId);
            foreach (var node in nodes.Where(x => x != null && x.Kind == FloorRouteNodeKind.Room))
            {
                var room = rooms.FirstOrDefault(x => x != null && string.Equals(x.RoomInstanceId, node.RoomInstanceId, StringComparison.Ordinal));
                if (room != null && roomDefs.TryGetValue(room.RoomDefinitionId ?? string.Empty, out RoomSpatialDefinition definition) && edges.Count(x => x != null && (string.Equals(x.SourceNodeId, node.NodeId, StringComparison.Ordinal) || string.Equals(x.DestinationNodeId, node.NodeId, StringComparison.Ordinal))) > definition.MaximumConnectionCount) Add(issues, FloorLayoutValidationReason.ConnectionLimitExceeded, node.NodeId, room.RoomInstanceId);
            }
            var requiredReachable = entrances.Length == 1 ? Reachable(entrances[0].NodeId, edges, RouteClassification.Required) : new HashSet<string>(StringComparer.Ordinal);
            var terminals = new HashSet<string>(nodes.Where(x => x != null && (x.Kind == FloorRouteNodeKind.Exit || x.Kind == FloorRouteNodeKind.Descent || x.Kind == FloorRouteNodeKind.Completion)).Select(x => x.NodeId), StringComparer.Ordinal);
            foreach (string id in requiredReachable) if (!Reachable(id, edges, RouteClassification.Required).Overlaps(terminals)) Add(issues, FloorLayoutValidationReason.RequiredRouteWithoutTerminal, id);
            int branchCount = edges.Where(x => x != null && x.Classification == RouteClassification.Optional && !string.IsNullOrWhiteSpace(x.OptionalBranchId)).Select(x => x.OptionalBranchId).Distinct(StringComparer.Ordinal).Count();
            if (floor != null && branchCount > floor.OptionalBranchAllowance) Add(issues, FloorLayoutValidationReason.OptionalBranchAllowanceExceeded, layout.FloorId);
        }

        private static HashSet<string> Reachable(string start, CorridorEdge[] edges, RouteClassification? classification)
        {
            var result = new HashSet<string>(StringComparer.Ordinal); var pending = new Queue<string>(); result.Add(start); pending.Enqueue(start);
            while (pending.Count > 0) { string current = pending.Dequeue(); foreach (var edge in edges.Where(x => x != null && (!classification.HasValue || x.Classification == classification.Value) && string.Equals(x.SourceNodeId, current, StringComparison.Ordinal)).OrderBy(x => x.DestinationNodeId, StringComparer.Ordinal).ThenBy(x => x.EdgeId, StringComparer.Ordinal)) if (result.Add(edge.DestinationNodeId)) pending.Enqueue(edge.DestinationNodeId); }
            return result;
        }

        private static void AddOccupancy(ResolvedTileFootprint footprint, string subject, Dictionary<TileCoordinate, string> occupied, List<FloorLayoutValidationIssue> issues)
        { foreach (var tile in footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>()) if (occupied.TryGetValue(tile, out string other)) Add(issues, FloorLayoutValidationReason.FootprintOverlap, subject, other, tile); else occupied.Add(tile, subject); }
        private static void CheckId(string id, string subject, List<FloorLayoutValidationIssue> issues) { if (string.IsNullOrWhiteSpace(id)) Add(issues, FloorLayoutValidationReason.MissingStableId, subject); }
        private static void CheckDuplicates(IEnumerable<string> ids, FloorLayoutValidationReason reason, List<FloorLayoutValidationIssue> issues) { foreach (string id in ids.Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(x => x, StringComparer.Ordinal).Where(x => x.Count() > 1).Select(x => x.Key)) Add(issues, reason, id); }
        private static void Add(List<FloorLayoutValidationIssue> issues, FloorLayoutValidationReason reason, string subject = null, string related = null, TileCoordinate? coordinate = null) { issues.Add(new FloorLayoutValidationIssue { Reason = reason, SubjectId = subject, RelatedId = related, HasCoordinate = coordinate.HasValue, Coordinate = coordinate ?? default(TileCoordinate) }); }
        private static FloorLayoutValidationIssue[] Order(IEnumerable<FloorLayoutValidationIssue> issues) => issues.OrderBy(x => (int)x.Reason).ThenBy(x => x.SubjectId, StringComparer.Ordinal).ThenBy(x => x.RelatedId, StringComparer.Ordinal).ThenBy(x => x.HasCoordinate ? 1 : 0).ThenBy(x => x.Coordinate).ToArray();
    }
}
