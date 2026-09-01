using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class StructuralConstructionRequest
    {
        public string RoomDefinitionId;
        public TileCoordinate Anchor;
        public CardinalOrientation Orientation;
        public string TerminalConnectionPointId;
    }

    public sealed class StructuralEditPreview
    {
        internal DetachedCanonicalSpatialSaveState DetachedCandidate { get; set; }
        internal object Intent { get; set; }
        internal string BaselineFingerprint { get; set; }
        public string RoomDefinitionId { get; internal set; }
        public string TargetRoomInstanceId { get; internal set; }
        public string PreviousRoomDefinitionId { get; internal set; }
        public TileCoordinate PreviousAnchor { get; internal set; }
        public CardinalOrientation PreviousOrientation { get; internal set; }
        public StructuralEditOperation Operation { get; internal set; }
        public int PreviousUsedFloorSpace { get; internal set; }
        public string[] PreservedAssignmentIds { get; internal set; } = Array.Empty<string>();
        public TileCoordinate Anchor { get; internal set; }
        public CardinalOrientation Orientation { get; internal set; }
        public TileCoordinate[] OccupiedTiles { get; internal set; } = Array.Empty<TileCoordinate>();
        public TileCoordinate[] IncomingConnectionTiles { get; internal set; }
            = Array.Empty<TileCoordinate>();
        public int ProspectiveFloorSpace { get; internal set; }
        public int ResultingUsedFloorSpace { get; internal set; }
        public int ResultingRemainingFloorSpace { get; internal set; }
        public FloorRouteConnectionKind ConnectionKind { get; internal set; }
        public string[] ReasonCodes { get; internal set; } = Array.Empty<string>();
        public StructuralChange[] Consequences { get; internal set; } = Array.Empty<StructuralChange>();
        public bool IsValid => DetachedCandidate != null && ReasonCodes.Length == 0;
    }

    public enum StructuralEditOperation { Construction = 1, Movement = 2, Replacement = 3, Deletion = 4 }
    public enum StructuralChangeKind { RoomAdded = 1, FixedStructureMoved = 2, EdgeAdded = 3, EdgeRemoved = 4,
        RoomMoved = 5, RoomReplaced = 6, EdgeReconnected = 7, CorridorMoved = 8, ContentPreserved = 9,
        RoomRemoved = 10, ContentReturned = 11 }
    public sealed class StructuralChange
    {
        public StructuralChangeKind Kind { get; internal set; }
        public string StableId { get; internal set; }
        public TileCoordinate From { get; internal set; }
        public TileCoordinate To { get; internal set; }
        public string PreviousDefinitionId { get; internal set; }
        public string ProposedDefinitionId { get; internal set; }
        public FloorRouteConnectionKind PreviousConnectionKind { get; internal set; }
        public FloorRouteConnectionKind ProposedConnectionKind { get; internal set; }
        public TileCoordinate[] PreviousFootprint { get; internal set; } = Array.Empty<TileCoordinate>();
        public TileCoordinate[] ProposedFootprint { get; internal set; } = Array.Empty<TileCoordinate>();
    }

    public static class StructuralEditService
    {
        public const string InvalidContextReason = "structural.edit.invalid_context";
        public const string InvalidIdentityReason = "structural.edit.invalid_identity";
        public const string LayoutInvalidReason = "structural.edit.layout_invalid";
        public const string StalePreviewReason = "structural.edit.stale_preview";
        public const string RoomDefinitionInvalidReason = "structural.edit.room_definition_invalid";
        public const string ReplacementSameDefinitionReason = "structural.edit.replacement_same_definition";
        public const string RoomNotAllowedReason = "structural.edit.room_not_allowed";
        public const string OrientationInvalidReason = "structural.edit.orientation_invalid";
        public const string OutOfBoundsReason = "structural.edit.out_of_bounds";
        public const string RoomOverlapReason = "structural.edit.room_overlap";
        public const string FixedOverlapReason = "structural.edit.fixed_structure_overlap";
        public const string CorridorOverlapReason = "structural.edit.corridor_overlap";
        public const string CapacityReason = "structural.edit.capacity_exceeded";
        public const string ConnectionUnavailableReason = "structural.edit.connection_unavailable";
        public const string ConnectionAmbiguousReason = "structural.edit.connection_ambiguous";
        public const string SocketIncompatibleReason = "structural.edit.socket_incompatible";
        public const string ConnectionPointInvalidReason = "structural.edit.connection_point_invalid";
        public const string TerminalPlacementInvalidReason = "structural.edit.terminal_placement_invalid";
        public const string RequiredRouteReason = "structural.edit.required_route_disconnected";
        public const string CompletionUnreachableReason = "structural.edit.completion_unreachable";
        public const string WorkloadReason = "structural.edit.workload_exceeded";
        public const string CorridorDefinitionReason = "structural.edit.corridor_definition_invalid";
        public const string CorridorLengthReason = "structural.edit.corridor_length_invalid";
        public const string TargetRoomNotFoundReason = "structural.edit.target_room_not_found";
        public const string TargetRoomNotBuildableReason = "structural.edit.target_room_not_buildable";
        public const string RequiredRouteAmbiguousReason = "structural.edit.required_route_ambiguous";
        public const string ContentCapacityReason = "structural.edit.content_capacity_exceeded";

        public static StructuralEditPreview InvalidPreview(string reason,
            StructuralConstructionRequest request)
        {
            var result = new StructuralEditPreview
            {
                RoomDefinitionId = request?.RoomDefinitionId,
                Anchor = request?.Anchor ?? default,
                Orientation = request?.Orientation ?? default,
                ReasonCodes = new[] { reason }
            };
            return result;
        }

        public static StructuralEditPreview Preview(DetachedCanonicalSpatialSaveState current,
            StructuralConstructionRequest request, ProductionSpatialContentSnapshot production,
            SpatialLayoutCompatibilitySnapshot compatibility, RunSimulationConfig configuration,
            CanonicalSpatialSerializationLimits limits)
        {
            var result = new StructuralEditPreview { RoomDefinitionId = request?.RoomDefinitionId,
                Anchor = request?.Anchor ?? default, Orientation = request?.Orientation ?? default,
                Operation = StructuralEditOperation.Construction };
            if (current?.Authority == null || request == null || production == null || compatibility == null ||
                configuration == null || !limits.IsValid || current.Floors?.Length != 1)
                return Fail(result, InvalidContextReason);
            if (!TryFingerprint(current, limits, out string baseline)) return Fail(result, InvalidContextReason);
            result.BaselineFingerprint = baseline;
            result.Intent = new StructuralConstructionRequest { RoomDefinitionId = request.RoomDefinitionId,
                Anchor = request.Anchor, Orientation = request.Orientation,
                TerminalConnectionPointId = request.TerminalConnectionPointId };

            SpatialContentCatalog catalog = production.Catalog;
            SavedSpatialFloor sourceFloor = current.Floors[0];
            FloorSpatialConfiguration floorDefinition = (catalog.Floors ?? Array.Empty<FloorSpatialConfiguration>())
                .SingleOrDefault(value => value != null && value.FloorDefinitionId == sourceFloor.FloorDefinitionId &&
                    value.FloorIndex == sourceFloor.FloorIndex);
            RoomSpatialDefinition[] roomMatches = (catalog.Rooms ?? Array.Empty<RoomSpatialDefinition>())
                .Where(value => value != null && value.RoomDefinitionId == request.RoomDefinitionId).ToArray();
            if (roomMatches.Length != 1) return Fail(result, RoomDefinitionInvalidReason);
            RoomSpatialDefinition roomDefinition = roomMatches[0];
            if (floorDefinition == null || !(floorDefinition.AllowedRoomDefinitionIds ?? Array.Empty<string>())
                    .Contains(roomDefinition.RoomDefinitionId)) return Fail(result, RoomNotAllowedReason);
            if (!(roomDefinition.AllowedOrientations ?? Array.Empty<CardinalOrientation>()).Contains(request.Orientation))
                return Fail(result, OrientationInvalidReason);
            var workload = new SpatialValidationWorkloadLimits(limits.Spatial.MaximumMaterializedTiles);
            if (!roomDefinition.TryResolveGrossTiles(request.Anchor, request.Orientation, workload,
                    out ResolvedTileFootprint roomFootprint)) return Fail(result, WorkloadReason);
            result.OccupiedTiles = roomFootprint.OccupiedTiles.OrderBy(value => value).ToArray();
            result.ProspectiveFloorSpace = result.OccupiedTiles.Length;
            if (result.OccupiedTiles.Any(tile => !floorDefinition.Bounds.Contains(tile))) return Fail(result, OutOfBoundsReason);

            if (!Clone(current, limits, out DetachedCanonicalSpatialSaveState candidate)) return Fail(result, InvalidContextReason);
            SavedSpatialFloor floor = candidate.Floors[0];
            if (!NativeStructuralIdentity.TryAllocateConstructionIdentity(candidate, floor.FloorInstanceId,
                    out NativeRoomConstructionIdentity identity, out string identityReason))
                return Fail(result, identityReason);
            FloorStructuralIdentityLifecycle lifecycle = candidate.LifecycleAndOwnership.Floors
                .Single(value => value.FloorInstanceId == floor.FloorInstanceId);
            lifecycle.NextNativeRoomOrdinal++;
            string roomId = identity.RoomInstanceId;
            if (OverlapsRooms(result.OccupiedTiles, floor.Layout.Rooms, catalog.Rooms, workload)) return Fail(result, RoomOverlapReason);
            if (OverlapsFixed(result.OccupiedTiles, floor.FixedStructures, catalog.FixedStructures, workload)) return Fail(result, FixedOverlapReason);
            if (OverlapsCorridors(result.OccupiedTiles, floor.Layout.Edges)) return Fail(result, CorridorOverlapReason);

            FloorRouteNode completionNode = floor.Layout.Nodes.SingleOrDefault(value => value?.Kind == FloorRouteNodeKind.Completion);
            FloorRouteEdge oldTerminalEdge = floor.Layout.Edges.SingleOrDefault(value => value != null &&
                value.Classification == RouteClassification.Required && value.DestinationNodeId == completionNode?.NodeId);
            FloorRouteNode previousNode = floor.Layout.Nodes.SingleOrDefault(value => value?.NodeId == oldTerminalEdge?.SourceNodeId &&
                value.Kind == FloorRouteNodeKind.Room);
            RoomSpatialInstance previousRoom = floor.Layout.Rooms.SingleOrDefault(value => value?.RoomInstanceId == previousNode?.RoomInstanceId);
            RoomSpatialDefinition previousDefinition = catalog.Rooms.SingleOrDefault(value =>
                value?.RoomDefinitionId == previousRoom?.RoomDefinitionId);
            if (completionNode == null || oldTerminalEdge == null || previousRoom == null || previousDefinition == null)
                return Fail(result, RequiredRouteReason);

            var newRoom = new RoomSpatialInstance { RoomInstanceId = roomId, RoomDefinitionId = roomDefinition.RoomDefinitionId,
                FloorId = floor.FloorInstanceId, Anchor = request.Anchor, Orientation = request.Orientation };
            DoorPair[] incoming = Pairs(previousRoom, previousDefinition, newRoom, roomDefinition, catalog).ToArray();
            CorridorSpatialDefinition corridor = null;
            if (incoming.Length == 0)
            {
                CorridorSpatialDefinition[] corridors = (catalog.Corridors ?? Array.Empty<CorridorSpatialDefinition>())
                    .Where(value => value != null && (floorDefinition.AllowedCorridorDefinitionIds ?? Array.Empty<string>())
                        .Contains(value.CorridorDefinitionId) && value.Category == CorridorSpatialCategory.Straight).ToArray();
                if (corridors.Length != 1) return Fail(result, CorridorDefinitionReason);
                corridor = corridors[0];
                CorridorAnalysis analysis = AnalyzeCorridors(previousRoom, previousDefinition, newRoom,
                    roomDefinition, catalog, corridor);
                incoming = analysis.ValidPairs;
                if (incoming.Length == 0) return Fail(result, analysis.HasLengthInvalidPair
                    ? CorridorLengthReason : ConnectionUnavailableReason);
            }
            if (incoming.Length != 1) return Fail(result, ConnectionAmbiguousReason);

            SpatialConnectionPointDefinition outgoing = (roomDefinition.ConnectionPoints ??
                Array.Empty<SpatialConnectionPointDefinition>()).SingleOrDefault(value =>
                    value != null && value.ConnectionPointId == request.TerminalConnectionPointId);
            if (outgoing == null) return Fail(result, ConnectionPointInvalidReason);
            SavedFixedSpatialStructure terminal = floor.FixedStructures.SingleOrDefault(value =>
                value?.Kind == FixedSpatialStructureKind.CompletionTerminal);
            FixedSpatialStructureDefinition terminalDefinition = catalog.FixedStructures.SingleOrDefault(value =>
                value?.StructureDefinitionId == terminal?.FixedStructureDefinitionId);
            SpatialConnectionPointDefinition terminalPoint = (terminalDefinition?.ConnectionPoints ??
                Array.Empty<SpatialConnectionPointDefinition>()).SingleOrDefault();
            if (terminal == null || terminalDefinition == null || terminalPoint == null ||
                !Compatible(outgoing.SocketTypeId, terminalPoint.SocketTypeId, catalog)) return Fail(result, SocketIncompatibleReason);
            CardinalOrientation outgoingFacing = Rotate(outgoing.Facing, request.Orientation);
            CardinalOrientation terminalOrientation = (terminalDefinition.AllowedOrientations ?? Array.Empty<CardinalOrientation>())
                .Where(value => Rotate(terminalPoint.Facing, value) == Opposite(outgoingFacing)).SingleOrDefaultInvalid(out bool unique);
            if (!unique) return Fail(result, TerminalPlacementInvalidReason);
            TileCoordinate outgoingWorld = World(outgoing.Offset, request.Anchor, request.Orientation,
                roomDefinition.GrossFootprint);
            TileCoordinate terminalSocketWorld = Step(outgoingWorld, outgoingFacing);
            TileCoordinate terminalAnchor = AnchorFor(terminalSocketWorld, terminalPoint.Offset,
                terminalOrientation, terminalDefinition.GrossFootprint);
            if (!TileFootprintResolver.TryResolveRectangle(terminalDefinition.GrossFootprint, terminalAnchor,
                    terminalOrientation, workload, out ResolvedTileFootprint terminalFootprint)) return Fail(result, WorkloadReason);
            if (terminalFootprint.OccupiedTiles.Any(tile => !floorDefinition.Bounds.Contains(tile)))
                return Fail(result, TerminalPlacementInvalidReason);
            TileCoordinate oldTerminalAnchor = terminal.Anchor;
            terminal.Anchor = terminalAnchor; terminal.Orientation = terminalOrientation;

            if (corridor != null)
            {
                TileCoordinate[] corridorTiles = incoming[0].Tiles;
                if (corridorTiles.Any(tile => !floorDefinition.Bounds.Contains(tile)))
                    return Fail(result, OutOfBoundsReason);
                if (OverlapsRooms(corridorTiles, floor.Layout.Rooms.Concat(new[] { newRoom }),
                        catalog.Rooms, workload) || OverlapsFixed(corridorTiles, floor.FixedStructures,
                        catalog.FixedStructures, workload) || Overlaps(corridorTiles, terminalFootprint.OccupiedTiles) ||
                        OverlapsCorridors(corridorTiles, floor.Layout.Edges))
                    return Fail(result, CorridorOverlapReason);
            }

            floor.Layout.Rooms = floor.Layout.Rooms.Concat(new[] { newRoom }).ToArray();
            var newNode = new FloorRouteNode { NodeId = identity.RoomNodeId, FloorId = floor.FloorInstanceId,
                Kind = FloorRouteNodeKind.Room, RoomInstanceId = roomId };
            floor.Layout.Nodes = floor.Layout.Nodes.Concat(new[] { newNode }).ToArray();
            var incomingEdge = corridor == null
                ? Direct(identity.IncomingRequiredEdgeId, floor.FloorInstanceId, previousNode.NodeId, newNode.NodeId)
                : Physical(identity.IncomingRequiredEdgeId, floor.FloorInstanceId, previousNode.NodeId,
                    newNode.NodeId, corridor.CorridorDefinitionId, incoming[0].Tiles);
            var outgoingEdge = Direct(identity.TerminalRequiredEdgeId, floor.FloorInstanceId,
                newNode.NodeId, completionNode.NodeId);
            floor.Layout.Edges = floor.Layout.Edges.Where(value => value?.EdgeId != oldTerminalEdge.EdgeId)
                .Concat(new[] { incomingEdge, outgoingEdge }).ToArray();
            floor.RoomContents.RoomSemantics = (floor.RoomContents.RoomSemantics ?? Array.Empty<CanonicalRoomSemantics>())
                .Concat(new[] { new CanonicalRoomSemantics { RoomInstanceId = roomId,
                    LegacyRoomOriginKind = LegacyRoomOriginKind.CanonicalPlayerPlaced } }).ToArray();

            if (!CanonicalSpatialSaveContracts.TryCanonicalize(candidate, limits.Spatial, out candidate))
                return Fail(result, WorkloadReason);
            FloorLayoutValidationResult validation = FloorLayoutValidator.Validate(candidate.Floors[0].Layout,
                floorDefinition, catalog.Rooms, catalog.Corridors, workload, candidate.Floors[0].FixedStructures,
                catalog.FixedStructures);
            if (!validation.IsValid) return Fail(result, Map(validation.Issues));
            if (!CanonicalSpatialSaveContracts.Validate(candidate, limits.Spatial, true).IsValid ||
                !DetachedCanonicalProductionSemanticValidation.Validate(candidate, production, configuration,
                    limits.Spatial).IsValid) return Fail(result, LayoutInvalidReason);
            result.ResultingUsedFloorSpace = validation.Capacity.UsedFloorSpaceCapacity;
            result.ResultingRemainingFloorSpace = validation.Capacity.RemainingFloorSpaceCapacity;
            result.ConnectionKind = incomingEdge.ConnectionKind;
            result.IncomingConnectionTiles = incomingEdge.Footprint?.OccupiedTiles?.ToArray()
                ?? Array.Empty<TileCoordinate>();
            result.Consequences = new[] { new StructuralChange { Kind = StructuralChangeKind.RoomAdded,
                    StableId = roomId, To = request.Anchor }, new StructuralChange { Kind = StructuralChangeKind.FixedStructureMoved,
                    StableId = terminal.FixedStructureInstanceId, From = oldTerminalAnchor, To = terminal.Anchor },
                new StructuralChange { Kind = StructuralChangeKind.EdgeRemoved, StableId = oldTerminalEdge.EdgeId },
                new StructuralChange { Kind = StructuralChangeKind.EdgeAdded, StableId = incomingEdge.EdgeId },
                new StructuralChange { Kind = StructuralChangeKind.EdgeAdded, StableId = outgoingEdge.EdgeId } }
                .OrderBy(value => value.Kind).ThenBy(value => value.StableId, StringComparer.Ordinal).ToArray();
            result.DetachedCandidate = candidate;
            return result;
        }

        private sealed class DoorPair
        { internal TileCoordinate[] Tiles = Array.Empty<TileCoordinate>(); }
        private sealed class CorridorAnalysis
        { internal DoorPair[] ValidPairs; internal bool HasLengthInvalidPair; }
        private static IEnumerable<DoorPair> Pairs(RoomSpatialInstance a, RoomSpatialDefinition ad,
            RoomSpatialInstance b, RoomSpatialDefinition bd, SpatialContentCatalog catalog)
        {
            foreach (SpatialConnectionPointDefinition ap in ad.ConnectionPoints ?? Array.Empty<SpatialConnectionPointDefinition>())
            foreach (SpatialConnectionPointDefinition bp in bd.ConnectionPoints ?? Array.Empty<SpatialConnectionPointDefinition>())
            {
                CardinalOrientation af = Rotate(ap.Facing, a.Orientation), bf = Rotate(bp.Facing, b.Orientation);
                if (bf == Opposite(af) && Compatible(ap.SocketTypeId, bp.SocketTypeId, catalog) &&
                    Step(World(ap.Offset, a.Anchor, a.Orientation, ad.GrossFootprint), af).Equals(
                        World(bp.Offset, b.Anchor, b.Orientation, bd.GrossFootprint))) yield return new DoorPair();
            }
        }
        private static CorridorAnalysis AnalyzeCorridors(RoomSpatialInstance a, RoomSpatialDefinition ad,
            RoomSpatialInstance b, RoomSpatialDefinition bd, SpatialContentCatalog catalog,
            CorridorSpatialDefinition corridor)
        {
            var valid = new List<DoorPair>(); bool lengthInvalid = false;
            foreach (SpatialConnectionPointDefinition ap in ad.ConnectionPoints ?? Array.Empty<SpatialConnectionPointDefinition>())
            foreach (SpatialConnectionPointDefinition bp in bd.ConnectionPoints ?? Array.Empty<SpatialConnectionPointDefinition>())
            {
                CardinalOrientation af = Rotate(ap.Facing, a.Orientation), bf = Rotate(bp.Facing, b.Orientation);
                if (bf != Opposite(af) || !Compatible(ap.SocketTypeId, bp.SocketTypeId, catalog) ||
                    !(corridor.CompatibleSocketTypeIds ?? Array.Empty<string>()).Contains(ap.SocketTypeId) ||
                    !(corridor.CompatibleSocketTypeIds ?? Array.Empty<string>()).Contains(bp.SocketTypeId)) continue;
                TileCoordinate source = World(ap.Offset, a.Anchor, a.Orientation, ad.GrossFootprint);
                TileCoordinate destination = World(bp.Offset, b.Anchor, b.Orientation, bd.GrossFootprint);
                bool horizontal = source.Y == destination.Y &&
                    (af == CardinalOrientation.Ninety || af == CardinalOrientation.TwoSeventy);
                bool vertical = source.X == destination.X &&
                    (af == CardinalOrientation.Zero || af == CardinalOrientation.OneEighty);
                if (!horizontal && !vertical) continue;
                int distance = Math.Abs(destination.X - source.X) + Math.Abs(destination.Y - source.Y);
                int length = distance - 1;
                CardinalOrientation axis = horizontal ? CardinalOrientation.Ninety : CardinalOrientation.Zero;
                if (!Step(source, af).Equals(StepToward(source, destination))) continue;
                if (length < corridor.MinimumLength || length > corridor.MaximumLength)
                { lengthInvalid = true; continue; }
                if (corridor.Width != 1 ||
                    !(corridor.AllowedOrientations ?? Array.Empty<CardinalOrientation>()).Contains(axis)) continue;
                var tiles = new List<TileCoordinate>();
                TileCoordinate value = StepToward(source, destination);
                while (!value.Equals(destination)) { tiles.Add(value); value = StepToward(value, destination); }
                valid.Add(new DoorPair { Tiles = tiles.OrderBy(tile => tile).ToArray() });
            }
            return new CorridorAnalysis { ValidPairs = valid.ToArray(), HasLengthInvalidPair = lengthInvalid };
        }
        private static bool Compatible(string a, string b, SpatialContentCatalog catalog) =>
            (catalog.SocketTypes ?? Array.Empty<SpatialSocketTypeDefinition>()).Any(value => value != null &&
                value.SocketTypeId == a && (value.CompatibleSocketTypeIds ?? Array.Empty<string>()).Contains(b));
        internal static CardinalOrientation Rotate(CardinalOrientation facing, CardinalOrientation rotation) =>
            (CardinalOrientation)(((int)facing + (int)rotation) % 4);
        private static CardinalOrientation Opposite(CardinalOrientation value) => (CardinalOrientation)(((int)value + 2) % 4);
        private static TileCoordinate Step(TileCoordinate value, CardinalOrientation facing) => facing == CardinalOrientation.Zero
            ? new TileCoordinate(value.X, value.Y + 1) : facing == CardinalOrientation.Ninety
            ? new TileCoordinate(value.X + 1, value.Y) : facing == CardinalOrientation.OneEighty
            ? new TileCoordinate(value.X, value.Y - 1) : new TileCoordinate(value.X - 1, value.Y);
        internal static TileCoordinate TransformConnectionPointOffset(TileCoordinate offset,
            CardinalOrientation orientation, RectangularFootprintDefinition footprint) =>
            orientation == CardinalOrientation.Ninety
            ? new TileCoordinate(offset.Y, footprint.Width - 1 - offset.X)
            : orientation == CardinalOrientation.OneEighty
            ? new TileCoordinate(footprint.Width - 1 - offset.X, footprint.Height - 1 - offset.Y)
            : orientation == CardinalOrientation.TwoSeventy
            ? new TileCoordinate(footprint.Height - 1 - offset.Y, offset.X)
            : offset;
        private static TileCoordinate World(TileCoordinate offset, TileCoordinate anchor, CardinalOrientation orientation,
            RectangularFootprintDefinition footprint)
        { TileCoordinate transformed = TransformConnectionPointOffset(offset, orientation, footprint);
          return new TileCoordinate(anchor.X + transformed.X, anchor.Y + transformed.Y); }
        private static TileCoordinate AnchorFor(TileCoordinate world, TileCoordinate offset, CardinalOrientation orientation,
            RectangularFootprintDefinition footprint)
        { TileCoordinate transformed = World(offset, new TileCoordinate(0, 0), orientation, footprint);
          return new TileCoordinate(world.X - transformed.X, world.Y - transformed.Y); }
        private static FloorRouteEdge Direct(string id, string floor, string source, string destination) =>
            new FloorRouteEdge { EdgeId = id, CorridorDefinitionId = string.Empty, FloorId = floor,
                SourceNodeId = source, DestinationNodeId = destination, Footprint = null,
                Classification = RouteClassification.Required, OptionalBranchId = string.Empty,
                ConnectionKind = FloorRouteConnectionKind.DirectDoorway };
        private static FloorRouteEdge Physical(string id, string floor, string source, string destination,
            string definition, TileCoordinate[] tiles) => new FloorRouteEdge { EdgeId = id,
                CorridorDefinitionId = definition, FloorId = floor, SourceNodeId = source,
                DestinationNodeId = destination, Footprint = new ResolvedTileFootprint(tiles),
                Classification = RouteClassification.Required, OptionalBranchId = string.Empty,
                ConnectionKind = FloorRouteConnectionKind.PhysicalCorridor };
        private static TileCoordinate StepToward(TileCoordinate from, TileCoordinate to) =>
            new TileCoordinate(from.X == to.X ? from.X : from.X + Math.Sign(to.X - from.X),
                from.Y == to.Y ? from.Y : from.Y + Math.Sign(to.Y - from.Y));
        private static bool OverlapsRooms(IEnumerable<TileCoordinate> tiles, IEnumerable<RoomSpatialInstance> rooms,
            IEnumerable<RoomSpatialDefinition> definitions, SpatialValidationWorkloadLimits limits) =>
            Overlaps(tiles, (rooms ?? Enumerable.Empty<RoomSpatialInstance>()).SelectMany(room =>
            { RoomSpatialDefinition d = definitions.Single(value => value.RoomDefinitionId == room.RoomDefinitionId);
              return d.TryResolveGrossTiles(room.Anchor, room.Orientation, limits, out ResolvedTileFootprint f)
                ? f.OccupiedTiles : Array.Empty<TileCoordinate>(); }));
        private static bool OverlapsFixed(IEnumerable<TileCoordinate> tiles, IEnumerable<SavedFixedSpatialStructure> values,
            IEnumerable<FixedSpatialStructureDefinition> definitions, SpatialValidationWorkloadLimits limits) =>
            Overlaps(tiles, (values ?? Enumerable.Empty<SavedFixedSpatialStructure>()).Where(value => value.Kind != FixedSpatialStructureKind.CompletionTerminal).SelectMany(value =>
            { FixedSpatialStructureDefinition d = definitions.Single(item => item.StructureDefinitionId == value.FixedStructureDefinitionId);
              return TileFootprintResolver.TryResolveRectangle(d.GrossFootprint, value.Anchor, value.Orientation, limits,
                    out ResolvedTileFootprint f) ? f.OccupiedTiles : Array.Empty<TileCoordinate>(); }));
        private static bool OverlapsCorridors(IEnumerable<TileCoordinate> tiles, IEnumerable<FloorRouteEdge> edges) =>
            Overlaps(tiles, (edges ?? Enumerable.Empty<FloorRouteEdge>()).Where(value => value?.ConnectionKind ==
                FloorRouteConnectionKind.PhysicalCorridor).SelectMany(value => value.Footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>()));
        private static bool Overlaps(IEnumerable<TileCoordinate> a, IEnumerable<TileCoordinate> b) =>
            new HashSet<TileCoordinate>(a).Overlaps(b);
        private static string Map(IEnumerable<FloorLayoutValidationIssue> issues)
        {
            FloorLayoutValidationReason[] reasons = issues.Select(value => value.Reason).ToArray();
            if (reasons.Contains(FloorLayoutValidationReason.CapacityExceeded)) return CapacityReason;
            if (reasons.Contains(FloorLayoutValidationReason.StructureTileOutsideFloorBounds)) return TerminalPlacementInvalidReason;
            if (reasons.Contains(FloorLayoutValidationReason.FootprintOverlap)) return TerminalPlacementInvalidReason;
            if (reasons.Contains(FloorLayoutValidationReason.CorridorDefinitionGeometryMismatch)) return CorridorLengthReason;
            if (reasons.Contains(FloorLayoutValidationReason.UnreachableRoom)) return RequiredRouteReason;
            if (reasons.Contains(FloorLayoutValidationReason.RequiredRouteWithoutTerminal)) return CompletionUnreachableReason;
            return LayoutInvalidReason;
        }
        private static bool Clone(DetachedCanonicalSpatialSaveState state, CanonicalSpatialSerializationLimits limits,
            out DetachedCanonicalSpatialSaveState clone)
        { clone = null; SpatialContractResult<byte[]> bytes = CanonicalSpatialSaveSerializer.Serialize(state, limits);
          if (!bytes.IsValid) return false; SpatialContractResult<DetachedCanonicalSpatialSaveState> parsed =
              CanonicalSpatialSaveSerializer.Parse(bytes.Value, limits); clone = parsed.Value; return parsed.IsValid; }
        internal static bool TryFingerprint(DetachedCanonicalSpatialSaveState state,
            CanonicalSpatialSerializationLimits limits, out string fingerprint)
        { fingerprint = null; SpatialContractResult<byte[]> serialized = CanonicalSpatialSaveSerializer.Serialize(state, limits);
          if (!serialized.IsValid) return false; fingerprint = SpatialContractSha256.Compute(serialized.Value);
          return !string.IsNullOrEmpty(fingerprint); }
        private static StructuralEditPreview Fail(StructuralEditPreview result, string reason)
        { result.ReasonCodes = new[] { reason }; return result; }
    }

    internal static class StructuralEditEnumerableExtensions
    {
        internal static CardinalOrientation SingleOrDefaultInvalid(this IEnumerable<CardinalOrientation> values,
            out bool unique)
        { CardinalOrientation[] array = values.ToArray(); unique = array.Length == 1;
          return unique ? array[0] : default; }
    }
}
