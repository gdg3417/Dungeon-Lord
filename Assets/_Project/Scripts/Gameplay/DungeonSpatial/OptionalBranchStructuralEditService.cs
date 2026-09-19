using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class OptionalBranchConstructionRequest
    {
        public string FloorInstanceId;
        public string OriginNodeId;
        public string OriginConnectionPointId;
        public int CorridorLength;
    }

    public sealed class OptionalBranchRemovalRequest
    {
        public string FloorInstanceId;
        public string OptionalBranchId;
    }

    public enum OptionalBranchEditOperation { Construction = 1, Removal = 2 }

    public sealed class OptionalBranchEditPreview
    {
        internal DetachedCanonicalSpatialSaveState DetachedCandidate { get; set; }
        internal object Intent { get; set; }
        internal string BaselineFingerprint { get; set; }
        public OptionalBranchEditOperation Operation { get; internal set; }
        public string FloorInstanceId { get; internal set; }
        public string OriginNodeId { get; internal set; }
        public string OriginConnectionPointId { get; internal set; }
        public string OptionalBranchId { get; internal set; }
        public string EdgeId { get; internal set; }
        public string DeadEndNodeId { get; internal set; }
        public TileCoordinate[] OccupiedTiles { get; internal set; } = Array.Empty<TileCoordinate>();
        public int TrapCapacity { get; internal set; }
        public int LootCapacity { get; internal set; }
        public int ResultingUsedFloorSpace { get; internal set; }
        public int ResultingRemainingFloorSpace { get; internal set; }
        public string[] ReasonCodes { get; internal set; } = Array.Empty<string>();
        public bool IsValid => DetachedCandidate != null && ReasonCodes.Length == 0;
    }

    public static class OptionalBranchStructuralEditService
    {
        public const string CorridorDefinitionId = "spatial.corridor.straight_stone";
        public const string InvalidContextReason = "branch.edit.invalid_context";
        public const string InvalidOriginReason = "branch.edit.invalid_origin";
        public const string ConnectionPointReason = "branch.edit.connection_point_invalid";
        public const string CorridorLengthReason = "branch.edit.corridor_length_invalid";
        public const string AllowanceExceededReason = "branch.edit.allowance_exceeded";
        public const string LayoutInvalidReason = "branch.edit.layout_invalid";
        public const string BranchNotFoundReason = "branch.edit.branch_not_found";
        public const string AssignedContentReason = "branch.edit.assigned_content";
        public const string StalePreviewReason = "branch.edit.stale_preview";

        public static OptionalBranchEditPreview InvalidConstruction(string reason,
            OptionalBranchConstructionRequest request) => Fail(new OptionalBranchEditPreview
            {
                Operation = OptionalBranchEditOperation.Construction,
                FloorInstanceId = request?.FloorInstanceId,
                OriginNodeId = request?.OriginNodeId,
                OriginConnectionPointId = request?.OriginConnectionPointId
            }, reason);

        public static OptionalBranchEditPreview InvalidRemoval(string reason,
            OptionalBranchRemovalRequest request) => Fail(new OptionalBranchEditPreview
            {
                Operation = OptionalBranchEditOperation.Removal,
                FloorInstanceId = request?.FloorInstanceId,
                OptionalBranchId = request?.OptionalBranchId
            }, reason);

        public static OptionalBranchEditPreview PreviewConstruction(
            DetachedCanonicalSpatialSaveState current, OptionalBranchConstructionRequest request,
            CompletedResearchState completedResearch, BasicBranchingResearchSnapshot research,
            ProductionSpatialContentSnapshot production, RunSimulationConfig configuration,
            CanonicalSpatialSerializationLimits limits)
        {
            var result = new OptionalBranchEditPreview
            {
                Operation = OptionalBranchEditOperation.Construction,
                FloorInstanceId = request?.FloorInstanceId,
                OriginNodeId = request?.OriginNodeId,
                OriginConnectionPointId = request?.OriginConnectionPointId
            };
            if (current?.Authority == null || request == null || production == null ||
                configuration == null || !limits.IsValid ||
                !StructuralEditService.TryFingerprint(current, limits, out string baseline))
                return Fail(result, InvalidContextReason);
            result.BaselineFingerprint = baseline;
            result.Intent = new OptionalBranchConstructionRequest
            {
                FloorInstanceId = request.FloorInstanceId,
                OriginNodeId = request.OriginNodeId,
                OriginConnectionPointId = request.OriginConnectionPointId,
                CorridorLength = request.CorridorLength
            };
            SavedSpatialFloor floor = (current.Floors ?? Array.Empty<SavedSpatialFloor>()).SingleOrDefault(
                value => value != null && value.FloorInstanceId == request.FloorInstanceId);
            FloorSpatialConfiguration floorDefinition = (production.Catalog.Floors ??
                Array.Empty<FloorSpatialConfiguration>()).SingleOrDefault(value => value != null &&
                value.FloorDefinitionId == floor?.FloorDefinitionId && value.FloorIndex == floor.FloorIndex);
            CorridorSpatialDefinition corridor = (production.Catalog.Corridors ??
                Array.Empty<CorridorSpatialDefinition>()).SingleOrDefault(value => value != null &&
                value.CorridorDefinitionId == CorridorDefinitionId &&
                (floorDefinition?.AllowedCorridorDefinitionIds ?? Array.Empty<string>()).Contains(value.CorridorDefinitionId));
            if (floor == null || floorDefinition == null || corridor == null)
                return Fail(result, InvalidContextReason);
            BasicBranchingAllowanceResult allowance = BasicBranchingResearchAuthority.Resolve(
                completedResearch, research, floorDefinition);
            if (!allowance.IsResolved) return Fail(result, allowance.Reason);
            int active = (floor.Layout?.Edges ?? Array.Empty<FloorRouteEdge>()).Where(edge =>
                edge?.Classification == RouteClassification.Optional).Select(edge => edge.OptionalBranchId)
                .Distinct(StringComparer.Ordinal).Count();
            if (active >= allowance.EffectiveAllowance)
                return Fail(result, allowance.Reason ?? AllowanceExceededReason);
            if (request.CorridorLength < corridor.MinimumLength ||
                request.CorridorLength > corridor.MaximumLength || corridor.Width != 1)
                return Fail(result, CorridorLengthReason);
            FloorRouteNode origin = (floor.Layout?.Nodes ?? Array.Empty<FloorRouteNode>()).SingleOrDefault(
                node => node != null && node.NodeId == request.OriginNodeId);
            if (origin == null || (origin.Kind != FloorRouteNodeKind.Room &&
                    origin.Kind != FloorRouteNodeKind.Entrance) || !OnRequiredRoute(floor, origin.NodeId))
                return Fail(result, InvalidOriginReason);
            if (!OptionalBranchGeometry.TryResolvePoint(floor, origin, request.OriginConnectionPointId,
                    production.Catalog, out SpatialConnectionPointDefinition point,
                    out TileCoordinate socket, out CardinalOrientation facing, out int maximumConnections))
                return Fail(result, ConnectionPointReason);
            int incident = (floor.Layout.Edges ?? Array.Empty<FloorRouteEdge>()).Count(edge => edge != null &&
                (edge.SourceNodeId == origin.NodeId || edge.DestinationNodeId == origin.NodeId));
            if (incident >= maximumConnections) return Fail(result, ConnectionPointReason);
            CardinalOrientation axis = facing == CardinalOrientation.Zero ||
                facing == CardinalOrientation.OneEighty ? CardinalOrientation.Zero : CardinalOrientation.Ninety;
            if (!(corridor.AllowedOrientations ?? Array.Empty<CardinalOrientation>()).Contains(axis) ||
                !(corridor.CompatibleSocketTypeIds ?? Array.Empty<string>()).Contains(point.SocketTypeId))
                return Fail(result, ConnectionPointReason);
            TileCoordinate[] tiles = Enumerable.Range(1, request.CorridorLength)
                .Select(distance => Step(socket, facing, distance)).OrderBy(value => value).ToArray();
            if (!Clone(current, limits, out DetachedCanonicalSpatialSaveState candidate))
                return Fail(result, InvalidContextReason);
            if (!NativeStructuralIdentity.TryAllocateFreshEdgeIdentity(candidate, floor.FloorInstanceId,
                    out string edgeId, out long nextOrdinal, out string identityReason))
                return Fail(result, identityReason ?? InvalidContextReason);
            string branchId = edgeId + ".branch";
            string deadEndId = edgeId + ".dead-end";
            SavedSpatialFloor target = candidate.Floors.Single(value =>
                value.FloorInstanceId == floor.FloorInstanceId);
            target.Layout.Nodes = target.Layout.Nodes.Concat(new[] { new FloorRouteNode
            {
                NodeId = deadEndId, FloorId = target.FloorInstanceId,
                Kind = FloorRouteNodeKind.DeadEnd, RoomInstanceId = null
            }}).ToArray();
            target.Layout.Edges = target.Layout.Edges.Concat(new[] { new FloorRouteEdge
            {
                EdgeId = edgeId, CorridorDefinitionId = corridor.CorridorDefinitionId,
                FloorId = target.FloorInstanceId, SourceNodeId = origin.NodeId,
                DestinationNodeId = deadEndId, Footprint = new ResolvedTileFootprint(tiles),
                Classification = RouteClassification.Optional, OptionalBranchId = branchId,
                ConnectionKind = FloorRouteConnectionKind.PhysicalCorridor
            }}).ToArray();
            candidate.LifecycleAndOwnership.Floors.Single(value =>
                value.FloorInstanceId == target.FloorInstanceId).NextNativeEdgeOrdinal = nextOrdinal;
            if (!CanonicalSpatialSaveContracts.TryCanonicalize(candidate, limits.Spatial, out candidate))
                return Fail(result, LayoutInvalidReason);
            FloorLayoutValidationResult validation = FloorLayoutValidator.Validate(target.Layout,
                floorDefinition, production.Catalog.Rooms, production.Catalog.Corridors,
                new SpatialValidationWorkloadLimits(limits.Spatial.MaximumMaterializedTiles),
                target.FixedStructures, production.Catalog.FixedStructures);
            if (!validation.IsValid || !CanonicalSpatialSaveContracts.Validate(candidate,
                    limits.Spatial, true).IsValid ||
                !DetachedCanonicalProductionSemanticValidation.Validate(candidate, production,
                    configuration, limits.Spatial).IsValid)
                return Fail(result, LayoutInvalidReason);
            result.DetachedCandidate = candidate; result.OptionalBranchId = branchId;
            result.EdgeId = edgeId; result.DeadEndNodeId = deadEndId; result.OccupiedTiles = tiles;
            result.TrapCapacity = corridor.TrapCapacity; result.LootCapacity = corridor.LootCapacity;
            result.ResultingUsedFloorSpace = validation.Capacity.UsedFloorSpaceCapacity;
            result.ResultingRemainingFloorSpace = validation.Capacity.RemainingFloorSpaceCapacity;
            return result;
        }

        public static OptionalBranchEditPreview PreviewRemoval(
            DetachedCanonicalSpatialSaveState current, CorridorContentAuthority corridorContent,
            OptionalBranchRemovalRequest request, ProductionSpatialContentSnapshot production,
            RunSimulationConfig configuration, CanonicalSpatialSerializationLimits limits)
        {
            var result = new OptionalBranchEditPreview { Operation = OptionalBranchEditOperation.Removal,
                FloorInstanceId = request?.FloorInstanceId, OptionalBranchId = request?.OptionalBranchId };
            if (current == null || corridorContent == null || request == null || production == null ||
                configuration == null || !limits.IsValid ||
                !StructuralEditService.TryFingerprint(current, limits, out string baseline))
                return Fail(result, InvalidContextReason);
            result.BaselineFingerprint = baseline;
            result.Intent = new OptionalBranchRemovalRequest { FloorInstanceId = request.FloorInstanceId,
                OptionalBranchId = request.OptionalBranchId };
            SavedSpatialFloor floor = (current.Floors ?? Array.Empty<SavedSpatialFloor>()).SingleOrDefault(
                value => value != null && value.FloorInstanceId == request.FloorInstanceId);
            FloorRouteEdge edge = (floor?.Layout?.Edges ?? Array.Empty<FloorRouteEdge>()).SingleOrDefault(
                value => value != null && value.Classification == RouteClassification.Optional &&
                value.OptionalBranchId == request.OptionalBranchId);
            FloorRouteNode deadEnd = (floor?.Layout?.Nodes ?? Array.Empty<FloorRouteNode>()).SingleOrDefault(
                value => value != null && value.NodeId == edge?.DestinationNodeId &&
                value.Kind == FloorRouteNodeKind.DeadEnd);
            if (floor == null || edge == null || deadEnd == null)
                return Fail(result, BranchNotFoundReason);
            if ((corridorContent.Assignments ?? Array.Empty<CorridorContentAssignment>()).Any(value =>
                    value != null && value.FloorInstanceId == floor.FloorInstanceId &&
                    value.OptionalBranchId == edge.OptionalBranchId))
                return Fail(result, AssignedContentReason);
            if (!Clone(current, limits, out DetachedCanonicalSpatialSaveState candidate))
                return Fail(result, InvalidContextReason);
            SavedSpatialFloor target = candidate.Floors.Single(value =>
                value.FloorInstanceId == floor.FloorInstanceId);
            target.Layout.Edges = target.Layout.Edges.Where(value => value.EdgeId != edge.EdgeId).ToArray();
            target.Layout.Nodes = target.Layout.Nodes.Where(value => value.NodeId != deadEnd.NodeId).ToArray();
            if (!CanonicalSpatialSaveContracts.TryCanonicalize(candidate, limits.Spatial, out candidate))
                return Fail(result, LayoutInvalidReason);
            FloorSpatialConfiguration definition = production.Catalog.Floors.Single(value => value != null &&
                value.FloorDefinitionId == target.FloorDefinitionId && value.FloorIndex == target.FloorIndex);
            FloorLayoutValidationResult validation = FloorLayoutValidator.Validate(target.Layout, definition,
                production.Catalog.Rooms, production.Catalog.Corridors,
                new SpatialValidationWorkloadLimits(limits.Spatial.MaximumMaterializedTiles),
                target.FixedStructures, production.Catalog.FixedStructures);
            if (!validation.IsValid || !CanonicalSpatialSaveContracts.Validate(candidate,
                    limits.Spatial, true).IsValid ||
                !DetachedCanonicalProductionSemanticValidation.Validate(candidate, production,
                    configuration, limits.Spatial).IsValid)
                return Fail(result, LayoutInvalidReason);
            result.DetachedCandidate = candidate; result.EdgeId = edge.EdgeId;
            result.DeadEndNodeId = deadEnd.NodeId; result.OccupiedTiles =
                edge.Footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>();
            result.ResultingUsedFloorSpace = validation.Capacity.UsedFloorSpaceCapacity;
            result.ResultingRemainingFloorSpace = validation.Capacity.RemainingFloorSpaceCapacity;
            return result;
        }

        private static bool OnRequiredRoute(SavedSpatialFloor floor, string nodeId)
        {
            FloorRouteNode[] nodes = floor.Layout?.Nodes ?? Array.Empty<FloorRouteNode>();
            FloorRouteEdge[] edges = (floor.Layout?.Edges ?? Array.Empty<FloorRouteEdge>()).Where(value =>
                value?.Classification == RouteClassification.Required).ToArray();
            FloorRouteNode current = nodes.SingleOrDefault(value => value?.Kind == FloorRouteNodeKind.Entrance);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (current != null && visited.Add(current.NodeId))
            {
                if (current.NodeId == nodeId) return true;
                FloorRouteEdge[] next = edges.Where(value => value.SourceNodeId == current.NodeId).ToArray();
                if (next.Length != 1) return false;
                current = nodes.SingleOrDefault(value => value?.NodeId == next[0].DestinationNodeId);
            }
            return false;
        }

        private static TileCoordinate Step(TileCoordinate value, CardinalOrientation facing, int distance) =>
            facing == CardinalOrientation.Zero ? new TileCoordinate(value.X, value.Y + distance) :
            facing == CardinalOrientation.Ninety ? new TileCoordinate(value.X + distance, value.Y) :
            facing == CardinalOrientation.OneEighty ? new TileCoordinate(value.X, value.Y - distance) :
            new TileCoordinate(value.X - distance, value.Y);
        private static bool Clone(DetachedCanonicalSpatialSaveState state,
            CanonicalSpatialSerializationLimits limits, out DetachedCanonicalSpatialSaveState clone)
        {
            clone = null; SpatialContractResult<byte[]> bytes = CanonicalSpatialSaveSerializer.Serialize(state, limits);
            if (!bytes.IsValid) return false;
            SpatialContractResult<DetachedCanonicalSpatialSaveState> parsed =
                CanonicalSpatialSaveSerializer.Parse(bytes.Value, limits);
            clone = parsed.Value; return parsed.IsValid;
        }
        private static OptionalBranchEditPreview Fail(OptionalBranchEditPreview value, string reason)
        { value.DetachedCandidate = null; value.ReasonCodes = new[] { reason }; return value; }
    }

    internal static class OptionalBranchGeometry
    {
        internal static bool TryResolvePoint(SavedSpatialFloor floor, FloorRouteNode node,
            string pointId, SpatialContentCatalog catalog, out SpatialConnectionPointDefinition point,
            out TileCoordinate world, out CardinalOrientation facing, out int maximumConnections)
        {
            point = null; world = default; facing = default; maximumConnections = 0;
            TileCoordinate anchor; CardinalOrientation orientation;
            RectangularFootprintDefinition footprint; SpatialConnectionPointDefinition[] points;
            if (node?.Kind == FloorRouteNodeKind.Room)
            {
                RoomSpatialInstance instance = (floor.Layout.Rooms ?? Array.Empty<RoomSpatialInstance>())
                    .SingleOrDefault(value => value?.RoomInstanceId == node.RoomInstanceId);
                RoomSpatialDefinition definition = (catalog.Rooms ?? Array.Empty<RoomSpatialDefinition>())
                    .SingleOrDefault(value => value?.RoomDefinitionId == instance?.RoomDefinitionId);
                if (instance == null || definition == null) return false;
                anchor = instance.Anchor; orientation = instance.Orientation;
                footprint = definition.GrossFootprint; points = definition.ConnectionPoints;
                maximumConnections = definition.MaximumConnectionCount;
            }
            else if (node?.Kind == FloorRouteNodeKind.Entrance)
            {
                SavedFixedSpatialStructure instance = (floor.FixedStructures ??
                    Array.Empty<SavedFixedSpatialStructure>()).SingleOrDefault(value => value != null &&
                    value.Kind == FixedSpatialStructureKind.Entrance);
                FixedSpatialStructureDefinition definition = (catalog.FixedStructures ??
                    Array.Empty<FixedSpatialStructureDefinition>()).SingleOrDefault(value => value != null &&
                    value.StructureDefinitionId == instance?.FixedStructureDefinitionId);
                if (instance == null || definition == null) return false;
                anchor = instance.Anchor; orientation = instance.Orientation;
                footprint = definition.GrossFootprint; points = definition.ConnectionPoints;
                maximumConnections = definition.MaximumConnectionCount;
            }
            else return false;
            point = (points ?? Array.Empty<SpatialConnectionPointDefinition>()).SingleOrDefault(value =>
                value != null && value.ConnectionPointId == pointId);
            if (point == null || footprint == null) return false;
            TileCoordinate offset = StructuralEditService.TransformConnectionPointOffset(point.Offset,
                orientation, footprint);
            world = new TileCoordinate(anchor.X + offset.X, anchor.Y + offset.Y);
            facing = StructuralEditService.Rotate(point.Facing, orientation);
            return true;
        }

        internal static bool IsPersistedBranchGeometryValid(SavedSpatialFloor floor,
            FloorRouteEdge edge, SpatialContentCatalog catalog)
        {
            FloorRouteNode source = (floor?.Layout?.Nodes ?? Array.Empty<FloorRouteNode>())
                .SingleOrDefault(value => value?.NodeId == edge?.SourceNodeId);
            TileCoordinate[] tiles = edge?.Footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>();
            if (source == null || tiles.Length == 0) return false;
            int matches = 0;
            IEnumerable<string> pointIds;
            if (source.Kind == FloorRouteNodeKind.Room)
            {
                RoomSpatialInstance instance = floor.Layout.Rooms.Single(value => value.RoomInstanceId == source.RoomInstanceId);
                pointIds = (catalog.Rooms.Single(value => value.RoomDefinitionId == instance.RoomDefinitionId)
                    .ConnectionPoints ?? Array.Empty<SpatialConnectionPointDefinition>()).Select(value => value.ConnectionPointId);
            }
            else if (source.Kind == FloorRouteNodeKind.Entrance)
            {
                SavedFixedSpatialStructure instance = floor.FixedStructures.Single(value => value.Kind == FixedSpatialStructureKind.Entrance);
                pointIds = (catalog.FixedStructures.Single(value => value.StructureDefinitionId == instance.FixedStructureDefinitionId)
                    .ConnectionPoints ?? Array.Empty<SpatialConnectionPointDefinition>()).Select(value => value.ConnectionPointId);
            }
            else return false;
            foreach (string pointId in pointIds)
                if (TryResolvePoint(floor, source, pointId, catalog, out _, out TileCoordinate socket,
                        out CardinalOrientation facing, out _) &&
                    tiles.Contains(Step(socket, facing, 1)) && Enumerable.Range(1, tiles.Length)
                        .Select(distance => Step(socket, facing, distance)).OrderBy(value => value)
                        .SequenceEqual(tiles.OrderBy(value => value))) matches++;
            return matches == 1;
        }

        internal static bool TryResolveSourceTile(SavedSpatialFloor floor, FloorRouteEdge edge,
            SpatialContentCatalog catalog, out TileCoordinate sourceTile)
        {
            sourceTile = default;
            FloorRouteNode source = (floor?.Layout?.Nodes ?? Array.Empty<FloorRouteNode>())
                .SingleOrDefault(value => value?.NodeId == edge?.SourceNodeId);
            if (source == null) return false;
            IEnumerable<string> pointIds;
            if (source.Kind == FloorRouteNodeKind.Room)
            {
                RoomSpatialInstance instance = floor.Layout.Rooms.Single(value =>
                    value.RoomInstanceId == source.RoomInstanceId);
                pointIds = (catalog.Rooms.Single(value => value.RoomDefinitionId ==
                    instance.RoomDefinitionId).ConnectionPoints ??
                    Array.Empty<SpatialConnectionPointDefinition>()).Select(value => value.ConnectionPointId);
            }
            else if (source.Kind == FloorRouteNodeKind.Entrance)
            {
                SavedFixedSpatialStructure instance = floor.FixedStructures.Single(value =>
                    value.Kind == FixedSpatialStructureKind.Entrance);
                pointIds = (catalog.FixedStructures.Single(value => value.StructureDefinitionId ==
                    instance.FixedStructureDefinitionId).ConnectionPoints ??
                    Array.Empty<SpatialConnectionPointDefinition>()).Select(value => value.ConnectionPointId);
            }
            else return false;
            TileCoordinate[] tiles = edge.Footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>();
            TileCoordinate[] matches = pointIds.Select(pointId =>
            {
                bool resolved = TryResolvePoint(floor, source, pointId, catalog, out _,
                    out TileCoordinate socket, out CardinalOrientation facing, out _);
                return new { resolved, tile = Step(socket, facing, 1) };
            }).Where(value => value.resolved && tiles.Contains(value.tile)).Select(value => value.tile)
                .Distinct().ToArray();
            if (matches.Length != 1) return false;
            sourceTile = matches[0]; return true;
        }

        private static TileCoordinate Step(TileCoordinate value, CardinalOrientation facing,
            int distance) => facing == CardinalOrientation.Zero
            ? new TileCoordinate(value.X, value.Y + distance)
            : facing == CardinalOrientation.Ninety
                ? new TileCoordinate(value.X + distance, value.Y)
                : facing == CardinalOrientation.OneEighty
                    ? new TileCoordinate(value.X, value.Y - distance)
                    : new TileCoordinate(value.X - distance, value.Y);
    }
}
