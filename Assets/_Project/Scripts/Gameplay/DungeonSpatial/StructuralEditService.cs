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
    }

    public sealed class StructuralEditPreview
    {
        internal DetachedCanonicalSpatialSaveState DetachedCandidate { get; set; }
        internal StructuralConstructionRequest Intent { get; set; }
        internal string BaselineFingerprint { get; set; }
        public string RoomDefinitionId { get; internal set; }
        public TileCoordinate Anchor { get; internal set; }
        public CardinalOrientation Orientation { get; internal set; }
        public TileCoordinate[] OccupiedTiles { get; internal set; } = Array.Empty<TileCoordinate>();
        public int ProspectiveFloorSpace { get; internal set; }
        public int ResultingUsedFloorSpace { get; internal set; }
        public int ResultingRemainingFloorSpace { get; internal set; }
        public FloorRouteConnectionKind ConnectionKind { get; internal set; }
        public string[] ReasonCodes { get; internal set; } = Array.Empty<string>();
        public StructuralChange[] Consequences { get; internal set; } = Array.Empty<StructuralChange>();
        public bool IsValid => DetachedCandidate != null && ReasonCodes.Length == 0;
    }

    public enum StructuralChangeKind { RoomAdded = 1, FixedStructureMoved = 2, EdgeAdded = 3, EdgeRemoved = 4 }
    public sealed class StructuralChange
    {
        public StructuralChangeKind Kind { get; internal set; }
        public string StableId { get; internal set; }
        public TileCoordinate From { get; internal set; }
        public TileCoordinate To { get; internal set; }
    }

    /// <summary>
    /// Pure construction preview. The current production compatibility geometry is the authority
    /// for the native R1-to-R2 transition; no migration entry point or live save object is touched.
    /// </summary>
    public static class StructuralEditService
    {
        public const string InvalidContextReason = "structural.edit.invalid_context";
        public const string UnsupportedGeometryReason = "structural.edit.unsupported_geometry";
        public const string InvalidIdentityReason = "structural.edit.invalid_identity";
        public const string PlacementMismatchReason = "structural.edit.placement_mismatch";
        public const string LayoutInvalidReason = "structural.edit.layout_invalid";
        public const string StalePreviewReason = "structural.edit.stale_preview";

        public static StructuralEditPreview Preview(DetachedCanonicalSpatialSaveState current,
            StructuralConstructionRequest request, ProductionSpatialContentSnapshot production,
            SpatialLayoutCompatibilitySnapshot compatibility, RunSimulationConfig configuration,
            CanonicalSpatialSerializationLimits limits)
        {
            var result = new StructuralEditPreview
            {
                RoomDefinitionId = request?.RoomDefinitionId,
                Anchor = request?.Anchor ?? default,
                Orientation = request?.Orientation ?? default
            };
            if (current?.Authority == null || request == null || production == null || compatibility == null ||
                configuration == null || !limits.IsValid || current.Floors?.Length != 1)
                return Fail(result, InvalidContextReason);
            if (!TryFingerprint(current, limits, out string baseline)) return Fail(result, InvalidContextReason);
            result.BaselineFingerprint = baseline;
            result.Intent = new StructuralConstructionRequest { RoomDefinitionId = request.RoomDefinitionId,
                Anchor = request.Anchor, Orientation = request.Orientation };

            CompatibilitySelectionResult<CanonicalStarterLayoutProfile> selection = compatibility.SelectStarter(
                DetachedWholeSaveCandidateSerializer.TargetSchemaVersion,
                current.Authority.CanonicalLayoutContractVersion);
            CompatibilityLayoutGeometryRecord geometry = selection.Success
                ? (compatibility.Value.GeometryRecords ?? Array.Empty<CompatibilityLayoutGeometryRecord>())
                    .SingleOrDefault(value => value != null && value.GeometryId == selection.Value.GeometryId &&
                        value.GeometryVersion == selection.Value.GeometryVersion &&
                        value.CanonicalHash == selection.Value.GeometryCanonicalHash)
                : null;
            CompatibilityLayoutVariant r2 = (geometry?.Layouts ?? Array.Empty<CompatibilityLayoutVariant>())
                .SingleOrDefault(value => value != null && value.Placements.Count(item =>
                    item != null && item.Role == CompatibilityRouteRole.BasicRoom1) == 1);
            CompatibilityLayoutPlacement placement = r2?.Placements.SingleOrDefault(value =>
                value != null && value.Role == CompatibilityRouteRole.BasicRoom1);
            if (geometry == null || r2 == null || placement == null ||
                request.RoomDefinitionId != geometry.BasicRoomDefinitionId)
                return Fail(result, UnsupportedGeometryReason);
            if (!request.Anchor.Equals(placement.Anchor) || request.Orientation != placement.Orientation)
                return Fail(result, PlacementMismatchReason);

            SpatialContentCatalog catalog = production.Catalog;
            RoomSpatialDefinition definition = (catalog.Rooms ?? Array.Empty<RoomSpatialDefinition>())
                .SingleOrDefault(value => value != null && value.RoomDefinitionId == request.RoomDefinitionId);
            if (definition == null || !(definition.AllowedOrientations ?? Array.Empty<CardinalOrientation>())
                    .Contains(request.Orientation) || !definition.TryResolveGrossTiles(request.Anchor,
                    request.Orientation, new SpatialValidationWorkloadLimits(limits.Spatial.MaximumMaterializedTiles),
                    out ResolvedTileFootprint footprint))
                return Fail(result, LayoutInvalidReason);
            result.OccupiedTiles = footprint.OccupiedTiles.OrderBy(value => value).ToArray();
            result.ProspectiveFloorSpace = result.OccupiedTiles.Length;
            result.ConnectionKind = r2.Connections.Select(value => value.ConnectionKind).Distinct().Single();

            if (!Clone(current, limits, out DetachedCanonicalSpatialSaveState candidate))
                return Fail(result, InvalidContextReason);
            SavedSpatialFloor floor = candidate.Floors[0];
            if ((floor.Layout.Rooms ?? Array.Empty<RoomSpatialInstance>()).Length != 1)
                return Fail(result, InvalidIdentityReason);

            string floorId = floor.FloorInstanceId;
            if (!NativeStructuralIdentity.TryAllocateRoomId(candidate, floor.FloorInstanceId,
                    out string roomInstanceId,
                    out string identityReason)) return Fail(result, identityReason);
            RoomSpatialInstance existing = floor.Layout.Rooms[0];
            var newRoom = new RoomSpatialInstance { RoomInstanceId = roomInstanceId,
                RoomDefinitionId = request.RoomDefinitionId, FloorId = floorId,
                Anchor = request.Anchor, Orientation = request.Orientation };
            floor.Layout.Rooms = new[] { existing, newRoom };
            floor.Layout.Nodes = r2.Placements.Select(value => Node(floorId, value,
                existing.RoomInstanceId, roomInstanceId)).ToArray();
            floor.Layout.Edges = r2.Connections.Select(value => Edge(floorId, value)).ToArray();
            CompatibilityLayoutPlacement completion = r2.Placements.Single(value =>
                value.Role == CompatibilityRouteRole.Completion);
            SavedFixedSpatialStructure terminal = floor.FixedStructures.Single(value =>
                value.Kind == FixedSpatialStructureKind.CompletionTerminal);
            TileCoordinate previousTerminalAnchor = terminal.Anchor;
            terminal.Anchor = completion.Anchor; terminal.Orientation = completion.Orientation;
            floor.RoomContents.RoomSemantics = (floor.RoomContents.RoomSemantics ??
                Array.Empty<CanonicalRoomSemantics>()).Concat(new[] { new CanonicalRoomSemantics
                { RoomInstanceId = roomInstanceId,
                  LegacyRoomOriginKind = LegacyRoomOriginKind.CanonicalPlayerPlaced } }).ToArray();

            if (!CanonicalSpatialSaveContracts.TryCanonicalize(candidate, limits.Spatial, out candidate) ||
                !CanonicalSpatialSaveContracts.Validate(candidate, limits.Spatial, true).IsValid ||
                !DetachedCanonicalProductionSemanticValidation.Validate(candidate, production,
                    configuration, limits.Spatial).IsValid)
                return Fail(result, LayoutInvalidReason);
            FloorSpatialConfiguration floorDefinition = catalog.Floors.Single(value =>
                value.FloorDefinitionId == floor.FloorDefinitionId && value.FloorIndex == floor.FloorIndex);
            FloorLayoutValidationResult validation = FloorLayoutValidator.Validate(candidate.Floors[0].Layout,
                floorDefinition, catalog.Rooms, catalog.Corridors,
                new SpatialValidationWorkloadLimits(limits.Spatial.MaximumMaterializedTiles),
                candidate.Floors[0].FixedStructures, catalog.FixedStructures);
            result.ResultingUsedFloorSpace = validation.Capacity.UsedFloorSpaceCapacity;
            result.ResultingRemainingFloorSpace = validation.Capacity.RemainingFloorSpaceCapacity;
            string[] oldEdges = current.Floors[0].Layout.Edges.Select(value => value.EdgeId).ToArray();
            string[] newEdges = candidate.Floors[0].Layout.Edges.Select(value => value.EdgeId).ToArray();
            result.Consequences = new[] { new StructuralChange { Kind = StructuralChangeKind.RoomAdded,
                    StableId = roomInstanceId, To = request.Anchor },
                new StructuralChange { Kind = StructuralChangeKind.FixedStructureMoved,
                    StableId = terminal.FixedStructureInstanceId, From = previousTerminalAnchor, To = terminal.Anchor } }
                .Concat(oldEdges.Except(newEdges, StringComparer.Ordinal).Select(id => new StructuralChange
                    { Kind = StructuralChangeKind.EdgeRemoved, StableId = id }))
                .Concat(newEdges.Except(oldEdges, StringComparer.Ordinal).Select(id => new StructuralChange
                    { Kind = StructuralChangeKind.EdgeAdded, StableId = id }))
                .OrderBy(value => value.Kind).ThenBy(value => value.StableId, StringComparer.Ordinal).ToArray();
            result.DetachedCandidate = candidate;
            return result;
        }

        private static FloorRouteNode Node(string floorId, CompatibilityLayoutPlacement placement,
            string firstRoomId, string secondRoomId) => new FloorRouteNode
        {
            NodeId = floorId + ".node." + Role(placement.Role), FloorId = floorId,
            Kind = placement.Role == CompatibilityRouteRole.Entrance ? FloorRouteNodeKind.Entrance :
                placement.Role == CompatibilityRouteRole.Completion ? FloorRouteNodeKind.Completion : FloorRouteNodeKind.Room,
            RoomInstanceId = placement.Role == CompatibilityRouteRole.BasicRoom0 ? firstRoomId :
                placement.Role == CompatibilityRouteRole.BasicRoom1 ? secondRoomId : null
        };

        private static FloorRouteEdge Edge(string floorId, CompatibilityLayoutConnection connection) =>
            new FloorRouteEdge { EdgeId = floorId + ".edge.direct." + Role(connection.SourceRole) + "." + Role(connection.DestinationRole),
                CorridorDefinitionId = connection.CorridorDefinitionId ?? string.Empty, FloorId = floorId,
                SourceNodeId = floorId + ".node." + Role(connection.SourceRole),
                DestinationNodeId = floorId + ".node." + Role(connection.DestinationRole),
                Classification = RouteClassification.Required, OptionalBranchId = string.Empty,
                ConnectionKind = connection.ConnectionKind, Footprint = null };

        private static string Role(CompatibilityRouteRole role) => role == CompatibilityRouteRole.Entrance
            ? "entrance" : role == CompatibilityRouteRole.Completion ? "completion" :
            role == CompatibilityRouteRole.BasicRoom0 ? "legacy-room.00" : "legacy-room.01";

        private static bool Clone(DetachedCanonicalSpatialSaveState state, CanonicalSpatialSerializationLimits limits,
            out DetachedCanonicalSpatialSaveState clone)
        {
            clone = null;
            SpatialContractResult<byte[]> bytes = CanonicalSpatialSaveSerializer.Serialize(state, limits);
            if (!bytes.IsValid) return false;
            SpatialContractResult<DetachedCanonicalSpatialSaveState> parsed = CanonicalSpatialSaveSerializer.Parse(bytes.Value, limits);
            clone = parsed.Value; return parsed.IsValid;
        }

        internal static bool TryFingerprint(DetachedCanonicalSpatialSaveState state,
            CanonicalSpatialSerializationLimits limits, out string fingerprint)
        {
            fingerprint = null;
            SpatialContractResult<byte[]> serialized = CanonicalSpatialSaveSerializer.Serialize(state, limits);
            if (!serialized.IsValid) return false;
            fingerprint = SpatialContractSha256.Compute(serialized.Value);
            return !string.IsNullOrEmpty(fingerprint);
        }
        private static StructuralEditPreview Fail(StructuralEditPreview result, string reason)
        { result.ReasonCodes = new[] { reason }; return result; }
    }
}
