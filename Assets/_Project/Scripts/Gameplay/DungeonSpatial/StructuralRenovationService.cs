using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class StructuralMovementRequest
    {
        public string RoomInstanceId;
        public TileCoordinate Anchor;
    }

    public sealed class StructuralReplacementRequest
    {
        public string RoomInstanceId;
        public string RoomDefinitionId;
    }

    /// <summary>
    /// Pure Phase 3B1 renovation preparation. It edits only a serialized detached clone and leaves
    /// persistence/publication to DetachedCanonicalWriteAuthority.
    /// </summary>
    public static class StructuralRenovationService
    {
        private sealed class Path
        {
            internal FloorRouteNode[] Nodes;
            internal FloorRouteEdge[] Edges;
        }

        private sealed class Connection
        {
            internal FloorRouteConnectionKind Kind;
            internal string CorridorDefinitionId;
            internal TileCoordinate[] Tiles = Array.Empty<TileCoordinate>();
        }

        public static StructuralEditPreview InvalidMovement(string reason, StructuralMovementRequest request) =>
            Fail(new StructuralEditPreview { Operation = StructuralEditOperation.Movement,
                TargetRoomInstanceId = request?.RoomInstanceId, Anchor = request?.Anchor ?? default }, reason);

        public static StructuralEditPreview InvalidReplacement(string reason, StructuralReplacementRequest request) =>
            Fail(new StructuralEditPreview { Operation = StructuralEditOperation.Replacement,
                TargetRoomInstanceId = request?.RoomInstanceId, RoomDefinitionId = request?.RoomDefinitionId }, reason);

        public static StructuralEditPreview PreviewMovement(DetachedCanonicalSpatialSaveState current,
            StructuralMovementRequest request, ProductionSpatialContentSnapshot production,
            SpatialLayoutCompatibilitySnapshot compatibility, RunSimulationConfig configuration,
            CanonicalSpatialSerializationLimits limits)
        {
            var preview = new StructuralEditPreview { Operation = StructuralEditOperation.Movement,
                TargetRoomInstanceId = request?.RoomInstanceId, Anchor = request?.Anchor ?? default };
            if (!TryContext(current, request, production, compatibility, configuration, limits,
                    preview, out DetachedCanonicalSpatialSaveState candidate, out SavedSpatialFloor floor,
                    out FloorSpatialConfiguration floorDefinition, out SpatialValidationWorkloadLimits workload,
                    out Path path, out int targetIndex)) return preview;

            RoomSpatialInstance target = Room(floor, path.Nodes[targetIndex]);
            preview.RoomDefinitionId = preview.PreviousRoomDefinitionId = target.RoomDefinitionId;
            preview.Orientation = preview.PreviousOrientation = target.Orientation;
            preview.PreviousAnchor = target.Anchor;
            preview.Intent = new StructuralMovementRequest { RoomInstanceId = request.RoomInstanceId,
                Anchor = request.Anchor };
            TileCoordinate delta = Delta(target.Anchor, request.Anchor);
            return Apply(preview, candidate, floor, floorDefinition, production, configuration, limits,
                workload, path, targetIndex, delta, null);
        }

        public static StructuralEditPreview PreviewReplacement(DetachedCanonicalSpatialSaveState current,
            StructuralReplacementRequest request, ProductionSpatialContentSnapshot production,
            SpatialLayoutCompatibilitySnapshot compatibility, RunSimulationConfig configuration,
            CanonicalSpatialSerializationLimits limits)
        {
            var preview = new StructuralEditPreview { Operation = StructuralEditOperation.Replacement,
                TargetRoomInstanceId = request?.RoomInstanceId, RoomDefinitionId = request?.RoomDefinitionId };
            if (!TryContext(current, request, production, compatibility, configuration, limits,
                    preview, out DetachedCanonicalSpatialSaveState candidate, out SavedSpatialFloor floor,
                    out FloorSpatialConfiguration floorDefinition, out SpatialValidationWorkloadLimits workload,
                    out Path path, out int targetIndex)) return preview;
            RoomSpatialInstance target = Room(floor, path.Nodes[targetIndex]);
            RoomSpatialDefinition oldDefinition = Definition(production.Catalog, target.RoomDefinitionId);
            RoomSpatialDefinition replacement = UniqueDefinition(production.Catalog, request.RoomDefinitionId);
            preview.PreviousRoomDefinitionId = target.RoomDefinitionId;
            preview.PreviousAnchor = preview.Anchor = target.Anchor;
            preview.PreviousOrientation = preview.Orientation = target.Orientation;
            preview.Intent = new StructuralReplacementRequest { RoomInstanceId = request.RoomInstanceId,
                RoomDefinitionId = request.RoomDefinitionId };
            if (replacement == null) return Fail(preview, StructuralEditService.RoomDefinitionInvalidReason);
            if (!(floorDefinition.AllowedRoomDefinitionIds ?? Array.Empty<string>()).Contains(request.RoomDefinitionId))
                return Fail(preview, StructuralEditService.RoomNotAllowedReason);
            if (!(replacement.AllowedOrientations ?? Array.Empty<CardinalOrientation>()).Contains(target.Orientation))
                return Fail(preview, StructuralEditService.OrientationInvalidReason);
            if (!CapacityPermits(floor, target.RoomInstanceId, replacement))
                return Fail(preview, StructuralEditService.ContentCapacityReason);

            TileCoordinate downstreamDelta = default;
            if (!TryEndpoint(floor, path.Nodes[targetIndex + 1], production.Catalog,
                    out TileEndpoint downstreamEndpoint) ||
                !TryUniqueEndpointShift(target, oldDefinition, replacement, downstreamEndpoint,
                    production.Catalog, out downstreamDelta))
                return Fail(preview, StructuralEditService.ConnectionAmbiguousReason);
            target.RoomDefinitionId = replacement.RoomDefinitionId;
            // The replaced room stays anchored. Only strict descendants translate.
            return Apply(preview, candidate, floor, floorDefinition, production, configuration, limits,
                workload, path, targetIndex + 1, downstreamDelta, targetIndex);
        }

        private static StructuralEditPreview Apply(StructuralEditPreview preview,
            DetachedCanonicalSpatialSaveState candidate, SavedSpatialFloor floor,
            FloorSpatialConfiguration floorDefinition, ProductionSpatialContentSnapshot production,
            RunSimulationConfig configuration, CanonicalSpatialSerializationLimits limits,
            SpatialValidationWorkloadLimits workload, Path path, int moveStart, TileCoordinate delta,
            int? replacedIndex)
        {
            SpatialContentCatalog catalog = production.Catalog;
            var changes = new List<StructuralChange>();
            var movedNodeIds = new HashSet<string>(StringComparer.Ordinal);
            int firstRoomIndex = Math.Max(1, moveStart);
            for (int index = firstRoomIndex; index < path.Nodes.Length - 1; index++)
            {
                RoomSpatialInstance room = Room(floor, path.Nodes[index]);
                if (room == null) return Fail(preview, StructuralEditService.RequiredRouteReason);
                TileCoordinate old = room.Anchor;
                room.Anchor = Add(room.Anchor, delta);
                movedNodeIds.Add(path.Nodes[index].NodeId);
                if (!delta.Equals(default(TileCoordinate))) changes.Add(new StructuralChange
                { Kind = StructuralChangeKind.RoomMoved, StableId = room.RoomInstanceId, From = old, To = room.Anchor });
            }
            FloorRouteNode completionNode = path.Nodes[path.Nodes.Length - 1];
            movedNodeIds.Add(completionNode.NodeId);
            SavedFixedSpatialStructure terminal = floor.FixedStructures.SingleOrDefault(value =>
                value?.Kind == FixedSpatialStructureKind.CompletionTerminal);
            if (terminal == null) return Fail(preview, StructuralEditService.RequiredRouteReason);
            TileCoordinate oldTerminal = terminal.Anchor;
            terminal.Anchor = Add(terminal.Anchor, delta);
            if (!delta.Equals(default(TileCoordinate))) changes.Add(new StructuralChange
            { Kind = StructuralChangeKind.FixedStructureMoved, StableId = terminal.FixedStructureInstanceId,
                From = oldTerminal, To = terminal.Anchor });

            if (replacedIndex.HasValue)
            {
                RoomSpatialInstance replacement = Room(floor, path.Nodes[replacedIndex.Value]);
                changes.Add(new StructuralChange { Kind = StructuralChangeKind.RoomReplaced,
                    StableId = replacement.RoomInstanceId, From = replacement.Anchor, To = replacement.Anchor,
                    PreviousDefinitionId = preview.PreviousRoomDefinitionId,
                    ProposedDefinitionId = replacement.RoomDefinitionId });
            }

            // Rigidly translate only internal physical corridors. The sole boundary is resolved below.
            foreach (FloorRouteEdge edge in path.Edges)
            {
                bool sourceMoved = movedNodeIds.Contains(edge.SourceNodeId);
                bool destinationMoved = movedNodeIds.Contains(edge.DestinationNodeId);
                if (sourceMoved && destinationMoved && edge.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor)
                {
                    TileCoordinate[] old = edge.Footprint?.OccupiedTiles?.ToArray() ?? Array.Empty<TileCoordinate>();
                    TileCoordinate[] translated = old.Select(value => Add(value, delta)).OrderBy(value => value).ToArray();
                    edge.Footprint = new ResolvedTileFootprint(translated);
                    if (!delta.Equals(default(TileCoordinate))) changes.Add(EdgeChange(
                        StructuralChangeKind.CorridorMoved, edge, edge.ConnectionKind, old));
                }
            }

            int[] boundaryIndices = replacedIndex.HasValue
                ? new[] { replacedIndex.Value - 1, replacedIndex.Value }
                : new[] { moveStart - 1 };
            Connection resolved = null;
            foreach (int boundaryIndex in boundaryIndices.Distinct().OrderBy(value => value))
            {
                if (boundaryIndex < 0 || boundaryIndex >= path.Edges.Length)
                    return Fail(preview, StructuralEditService.RequiredRouteReason);
                FloorRouteEdge boundary = path.Edges[boundaryIndex];
                if (!TryResolveConnection(floor, path.Nodes[boundaryIndex], path.Nodes[boundaryIndex + 1],
                        floorDefinition, catalog, out resolved, out string reason)) return Fail(preview, reason);
                FloorRouteConnectionKind previousKind = boundary.ConnectionKind;
                TileCoordinate[] previousTiles = boundary.Footprint?.OccupiedTiles?.ToArray() ?? Array.Empty<TileCoordinate>();
                boundary.ConnectionKind = resolved.Kind;
                boundary.CorridorDefinitionId = resolved.CorridorDefinitionId;
                boundary.Footprint = resolved.Kind == FloorRouteConnectionKind.PhysicalCorridor
                    ? new ResolvedTileFootprint(resolved.Tiles) : null;
                changes.Add(EdgeChange(StructuralChangeKind.EdgeReconnected, boundary, previousKind, previousTiles));
            }

            preview.PreservedAssignmentIds = (floor.RoomContents.Assignments ?? Array.Empty<RoomContentAssignment>())
                .Where(value => value != null).Select(value => value.AssignmentId)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            changes.AddRange(preview.PreservedAssignmentIds.Select(value => new StructuralChange
                { Kind = StructuralChangeKind.ContentPreserved, StableId = value }));
            string geometryReason = InspectGeometry(floor, floorDefinition, catalog, workload);
            if (geometryReason != null) return Fail(preview, geometryReason);
            if (!CanonicalSpatialSaveContracts.TryCanonicalize(candidate, limits.Spatial, out candidate))
                return Fail(preview, StructuralEditService.WorkloadReason);
            FloorLayoutValidationResult validation = FloorLayoutValidator.Validate(candidate.Floors[0].Layout,
                floorDefinition, catalog.Rooms, catalog.Corridors, workload,
                candidate.Floors[0].FixedStructures, catalog.FixedStructures);
            if (!validation.IsValid) return Fail(preview, Map(validation.Issues));
            if (!CanonicalSpatialSaveContracts.Validate(candidate, limits.Spatial, true).IsValid ||
                !DetachedCanonicalProductionSemanticValidation.Validate(candidate, production,
                    configuration, limits.Spatial).IsValid)
                return Fail(preview, StructuralEditService.LayoutInvalidReason);
            preview.ResultingUsedFloorSpace = validation.Capacity.UsedFloorSpaceCapacity;
            preview.ResultingRemainingFloorSpace = validation.Capacity.RemainingFloorSpaceCapacity;
            preview.ConnectionKind = resolved.Kind;
            preview.IncomingConnectionTiles = resolved.Tiles;
            preview.Consequences = changes.OrderBy(value => value.Kind)
                .ThenBy(value => value.StableId, StringComparer.Ordinal).ToArray();
            preview.DetachedCandidate = candidate;
            return preview;
        }

        private static bool TryContext(DetachedCanonicalSpatialSaveState current, object request,
            ProductionSpatialContentSnapshot production, SpatialLayoutCompatibilitySnapshot compatibility,
            RunSimulationConfig configuration, CanonicalSpatialSerializationLimits limits,
            StructuralEditPreview preview, out DetachedCanonicalSpatialSaveState candidate,
            out SavedSpatialFloor floor, out FloorSpatialConfiguration floorDefinition,
            out SpatialValidationWorkloadLimits workload, out Path path, out int targetIndex)
        {
            candidate = null; floor = null; floorDefinition = null; path = null; targetIndex = -1;
            workload = new SpatialValidationWorkloadLimits(limits.Spatial.MaximumMaterializedTiles);
            if (current?.Authority == null || request == null || production == null || compatibility == null ||
                configuration == null || !limits.IsValid || current.Floors?.Length != 1)
            { Fail(preview, StructuralEditService.InvalidContextReason); return false; }
            if (!StructuralEditService.TryFingerprint(current, limits, out string fingerprint) ||
                !Clone(current, limits, out candidate))
            { Fail(preview, StructuralEditService.InvalidContextReason); return false; }
            preview.BaselineFingerprint = fingerprint;
            floor = candidate.Floors[0];
            floorDefinition = (production.Catalog.Floors ?? Array.Empty<FloorSpatialConfiguration>())
                .SingleOrDefault(value => value != null && value.FloorDefinitionId == floor.FloorDefinitionId &&
                    value.FloorIndex == floor.FloorIndex);
            if (floorDefinition == null || !TryPath(floor, out path))
            { Fail(preview, StructuralEditService.RequiredRouteAmbiguousReason); return false; }
            string targetId = request is StructuralMovementRequest movement ? movement.RoomInstanceId :
                ((StructuralReplacementRequest)request).RoomInstanceId;
            targetIndex = Array.FindIndex(path.Nodes, value => value?.Kind == FloorRouteNodeKind.Room &&
                value.RoomInstanceId == targetId);
            if (targetIndex < 1 || targetIndex >= path.Nodes.Length - 1)
            { Fail(preview, StructuralEditService.TargetRoomNotFoundReason); return false; }
            CanonicalRoomSemantics semantics = (floor.RoomContents.RoomSemantics ?? Array.Empty<CanonicalRoomSemantics>())
                .SingleOrDefault(value => value?.RoomInstanceId == targetId);
            if (semantics == null || semantics.LegacyRoomOriginKind == LegacyRoomOriginKind.ImplicitCompatibilityContainer)
            { Fail(preview, StructuralEditService.TargetRoomNotBuildableReason); return false; }
            FloorLayoutValidationResult prior = FloorLayoutValidator.Validate(floor.Layout, floorDefinition,
                production.Catalog.Rooms, production.Catalog.Corridors, workload,
                floor.FixedStructures, production.Catalog.FixedStructures);
            if (!prior.IsValid) { Fail(preview, StructuralEditService.LayoutInvalidReason); return false; }
            preview.PreviousUsedFloorSpace = prior.Capacity.UsedFloorSpaceCapacity;
            return true;
        }

        private static bool TryPath(SavedSpatialFloor floor, out Path path)
        {
            path = null;
            FloorRouteNode[] nodes = floor.Layout.Nodes ?? Array.Empty<FloorRouteNode>();
            FloorRouteEdge[] required = (floor.Layout.Edges ?? Array.Empty<FloorRouteEdge>())
                .Where(value => value?.Classification == RouteClassification.Required).ToArray();
            FloorRouteNode current = nodes.SingleOrDefault(value => value?.Kind == FloorRouteNodeKind.Entrance);
            FloorRouteNode completion = nodes.SingleOrDefault(value => value?.Kind == FloorRouteNodeKind.Completion);
            if (current == null || completion == null) return false;
            var orderedNodes = new List<FloorRouteNode> { current };
            var orderedEdges = new List<FloorRouteEdge>();
            var visited = new HashSet<string>(StringComparer.Ordinal) { current.NodeId };
            while (current.NodeId != completion.NodeId)
            {
                FloorRouteEdge[] outgoing = required.Where(value => value.SourceNodeId == current.NodeId).ToArray();
                if (outgoing.Length != 1) return false;
                FloorRouteNode next = nodes.SingleOrDefault(value => value?.NodeId == outgoing[0].DestinationNodeId);
                if (next == null || !visited.Add(next.NodeId)) return false;
                orderedEdges.Add(outgoing[0]); orderedNodes.Add(next); current = next;
            }
            if (orderedEdges.Count != required.Length || orderedNodes.Skip(1).Take(orderedNodes.Count - 2)
                    .Any(value => value.Kind != FloorRouteNodeKind.Room)) return false;
            path = new Path { Nodes = orderedNodes.ToArray(), Edges = orderedEdges.ToArray() };
            return true;
        }

        private static bool TryResolveConnection(SavedSpatialFloor floor, FloorRouteNode sourceNode,
            FloorRouteNode destinationNode, FloorSpatialConfiguration floorDefinition,
            SpatialContentCatalog catalog, out Connection result, out string reason)
        {
            result = null; reason = StructuralEditService.ConnectionUnavailableReason;
            if (!TryEndpoint(floor, sourceNode, catalog, out TileEndpoint source) ||
                !TryEndpoint(floor, destinationNode, catalog, out TileEndpoint destination)) return false;
            var direct = Pairs(source, destination, catalog, null, false).ToArray();
            if (direct.Length == 1) { result = new Connection
            { Kind = FloorRouteConnectionKind.DirectDoorway, CorridorDefinitionId = string.Empty }; reason = null; return true; }
            if (direct.Length > 1) { reason = StructuralEditService.ConnectionAmbiguousReason; return false; }
            CorridorSpatialDefinition[] definitions = (catalog.Corridors ?? Array.Empty<CorridorSpatialDefinition>())
                .Where(value => value != null && value.Category == CorridorSpatialCategory.Straight &&
                    (floorDefinition.AllowedCorridorDefinitionIds ?? Array.Empty<string>()).Contains(value.CorridorDefinitionId))
                .OrderBy(value => value.CorridorDefinitionId, StringComparer.Ordinal).ToArray();
            if (definitions.Length != 1) { reason = StructuralEditService.CorridorDefinitionReason; return false; }
            Connection[] corridors = Pairs(source, destination, catalog, definitions[0], true).ToArray();
            if (corridors.Length == 1) { result = corridors[0]; reason = null; return true; }
            if (corridors.Length > 1) reason = StructuralEditService.ConnectionAmbiguousReason;
            else if (HasAlignedPair(source, destination, catalog)) reason = StructuralEditService.CorridorLengthReason;
            return false;
        }

        private sealed class TileEndpoint
        {
            internal TileCoordinate Anchor; internal CardinalOrientation Orientation;
            internal RectangularFootprintDefinition Footprint;
            internal SpatialConnectionPointDefinition[] Points;
        }

        private static bool TryEndpoint(SavedSpatialFloor floor, FloorRouteNode node, SpatialContentCatalog catalog,
            out TileEndpoint endpoint)
        {
            endpoint = null;
            if (node.Kind == FloorRouteNodeKind.Room)
            {
                RoomSpatialInstance room = Room(floor, node); RoomSpatialDefinition definition =
                    Definition(catalog, room?.RoomDefinitionId);
                if (room == null || definition == null) return false;
                endpoint = new TileEndpoint { Anchor = room.Anchor, Orientation = room.Orientation,
                    Footprint = definition.GrossFootprint, Points = definition.ConnectionPoints }; return true;
            }
            FixedSpatialStructureKind kind = node.Kind == FloorRouteNodeKind.Entrance
                ? FixedSpatialStructureKind.Entrance : FixedSpatialStructureKind.CompletionTerminal;
            SavedFixedSpatialStructure fixedValue = floor.FixedStructures.SingleOrDefault(value => value?.Kind == kind);
            FixedSpatialStructureDefinition definitionValue = catalog.FixedStructures.SingleOrDefault(value =>
                value?.StructureDefinitionId == fixedValue?.FixedStructureDefinitionId);
            if (fixedValue == null || definitionValue == null) return false;
            endpoint = new TileEndpoint { Anchor = fixedValue.Anchor, Orientation = fixedValue.Orientation,
                Footprint = definitionValue.GrossFootprint, Points = definitionValue.ConnectionPoints }; return true;
        }

        private static IEnumerable<Connection> Pairs(TileEndpoint a, TileEndpoint b, SpatialContentCatalog catalog,
            CorridorSpatialDefinition corridor, bool physical)
        {
            foreach (SpatialConnectionPointDefinition ap in a.Points ?? Array.Empty<SpatialConnectionPointDefinition>())
            foreach (SpatialConnectionPointDefinition bp in b.Points ?? Array.Empty<SpatialConnectionPointDefinition>())
            {
                CardinalOrientation af = StructuralEditService.Rotate(ap.Facing, a.Orientation);
                CardinalOrientation bf = StructuralEditService.Rotate(bp.Facing, b.Orientation);
                if (bf != Opposite(af) || !Compatible(ap.SocketTypeId, bp.SocketTypeId, catalog)) continue;
                TileCoordinate aw = World(ap, a), bw = World(bp, b);
                int distance = Math.Abs(bw.X - aw.X) + Math.Abs(bw.Y - aw.Y);
                if (!physical)
                {
                    if (distance == 1 && Step(aw, af).Equals(bw)) yield return new Connection
                    { Kind = FloorRouteConnectionKind.DirectDoorway, CorridorDefinitionId = string.Empty };
                    continue;
                }
                bool horizontal = aw.Y == bw.Y && (af == CardinalOrientation.Ninety || af == CardinalOrientation.TwoSeventy);
                bool vertical = aw.X == bw.X && (af == CardinalOrientation.Zero || af == CardinalOrientation.OneEighty);
                int length = distance - 1;
                CardinalOrientation axis = horizontal ? CardinalOrientation.Ninety : CardinalOrientation.Zero;
                if ((!horizontal && !vertical) || !Step(aw, af).Equals(StepToward(aw, bw)) || corridor.Width != 1 ||
                    length < corridor.MinimumLength || length > corridor.MaximumLength ||
                    !(corridor.AllowedOrientations ?? Array.Empty<CardinalOrientation>()).Contains(axis) ||
                    !(corridor.CompatibleSocketTypeIds ?? Array.Empty<string>()).Contains(ap.SocketTypeId) ||
                    !(corridor.CompatibleSocketTypeIds ?? Array.Empty<string>()).Contains(bp.SocketTypeId)) continue;
                var tiles = new List<TileCoordinate>(); TileCoordinate tile = StepToward(aw, bw);
                while (!tile.Equals(bw)) { tiles.Add(tile); tile = StepToward(tile, bw); }
                yield return new Connection { Kind = FloorRouteConnectionKind.PhysicalCorridor,
                    CorridorDefinitionId = corridor.CorridorDefinitionId, Tiles = tiles.OrderBy(value => value).ToArray() };
            }
        }

        private static bool HasAlignedPair(TileEndpoint a, TileEndpoint b, SpatialContentCatalog catalog) =>
            (a.Points ?? Array.Empty<SpatialConnectionPointDefinition>()).Any(ap =>
                (b.Points ?? Array.Empty<SpatialConnectionPointDefinition>()).Any(bp =>
                {
                    CardinalOrientation af = StructuralEditService.Rotate(ap.Facing, a.Orientation);
                    CardinalOrientation bf = StructuralEditService.Rotate(bp.Facing, b.Orientation);
                    TileCoordinate aw = World(ap, a), bw = World(bp, b);
                    return bf == Opposite(af) && Compatible(ap.SocketTypeId, bp.SocketTypeId, catalog) &&
                        (aw.X == bw.X || aw.Y == bw.Y) && Step(aw, af).Equals(StepToward(aw, bw));
                }));

        private static bool TryUniqueEndpointShift(RoomSpatialInstance room, RoomSpatialDefinition oldDefinition,
            RoomSpatialDefinition replacement, TileEndpoint nextEndpoint, SpatialContentCatalog catalog,
            out TileCoordinate delta)
        {
            delta = default;
            var oldEndpoint = new TileEndpoint { Anchor = room.Anchor, Orientation = room.Orientation,
                Footprint = oldDefinition.GrossFootprint, Points = oldDefinition.ConnectionPoints };
            var shifts = new List<TileCoordinate>();
            foreach (SpatialConnectionPointDefinition oldPoint in oldEndpoint.Points ?? Array.Empty<SpatialConnectionPointDefinition>())
            foreach (SpatialConnectionPointDefinition nextPoint in nextEndpoint.Points ?? Array.Empty<SpatialConnectionPointDefinition>())
            {
                CardinalOrientation oldFacing = StructuralEditService.Rotate(oldPoint.Facing, room.Orientation);
                CardinalOrientation nextFacing = StructuralEditService.Rotate(nextPoint.Facing, nextEndpoint.Orientation);
                if (nextFacing != Opposite(oldFacing) || !Compatible(oldPoint.SocketTypeId, nextPoint.SocketTypeId, catalog)) continue;
                TileCoordinate oldWorld = World(oldPoint, oldEndpoint), nextWorld = World(nextPoint, nextEndpoint);
                if (oldWorld.X != nextWorld.X && oldWorld.Y != nextWorld.Y) continue;
                SpatialConnectionPointDefinition newPoint = (replacement.ConnectionPoints ?? Array.Empty<SpatialConnectionPointDefinition>())
                    .SingleOrDefault(value => value?.ConnectionPointId == oldPoint.ConnectionPointId);
                if (newPoint == null || StructuralEditService.Rotate(newPoint.Facing, room.Orientation) != oldFacing) continue;
                var replacementEndpoint = new TileEndpoint { Anchor = room.Anchor, Orientation = room.Orientation,
                    Footprint = replacement.GrossFootprint, Points = replacement.ConnectionPoints };
                TileCoordinate newWorld = World(newPoint, replacementEndpoint);
                shifts.Add(Delta(oldWorld, newWorld));
            }
            TileCoordinate[] unique = shifts.Distinct().ToArray();
            if (unique.Length != 1) return false; delta = unique[0]; return true;
        }

        private static bool CapacityPermits(SavedSpatialFloor floor, string roomId, RoomSpatialDefinition definition)
        {
            RoomContentAssignment[] values = floor.RoomContents.Assignments ?? Array.Empty<RoomContentAssignment>();
            return values.Count(value => value?.RoomInstanceId == roomId && value.CategoryId ==
                    MvpDungeonPlacementIds.MonsterCategoryId) <= definition.MonsterCapacity &&
                values.Count(value => value?.RoomInstanceId == roomId && value.CategoryId ==
                    MvpDungeonPlacementIds.TrapCategoryId) <= definition.TrapCapacity &&
                values.Count(value => value?.RoomInstanceId == roomId && value.CategoryId ==
                    MvpDungeonPlacementIds.LootNodeCategoryId) <= definition.LootCapacity;
        }

        private static StructuralChange EdgeChange(StructuralChangeKind kind, FloorRouteEdge edge,
            FloorRouteConnectionKind previousKind, TileCoordinate[] previousTiles) => new StructuralChange
        { Kind = kind, StableId = edge.EdgeId, PreviousConnectionKind = previousKind,
          ProposedConnectionKind = edge.ConnectionKind, PreviousFootprint = previousTiles,
          ProposedFootprint = edge.Footprint?.OccupiedTiles?.ToArray() ?? Array.Empty<TileCoordinate>() };
        private static string InspectGeometry(SavedSpatialFloor floor, FloorSpatialConfiguration floorDefinition,
            SpatialContentCatalog catalog, SpatialValidationWorkloadLimits workload)
        {
            var rooms = new List<HashSet<TileCoordinate>>();
            foreach (RoomSpatialInstance room in floor.Layout.Rooms ?? Array.Empty<RoomSpatialInstance>())
            {
                RoomSpatialDefinition definition = Definition(catalog, room.RoomDefinitionId);
                if (definition == null || !definition.TryResolveGrossTiles(room.Anchor, room.Orientation,
                        workload, out ResolvedTileFootprint footprint)) return StructuralEditService.WorkloadReason;
                if (footprint.OccupiedTiles.Any(value => !floorDefinition.Bounds.Contains(value)))
                    return StructuralEditService.OutOfBoundsReason;
                rooms.Add(new HashSet<TileCoordinate>(footprint.OccupiedTiles));
            }
            for (int a = 0; a < rooms.Count; a++) for (int b = a + 1; b < rooms.Count; b++)
                if (rooms[a].Overlaps(rooms[b])) return StructuralEditService.RoomOverlapReason;
            var fixedSets = new List<HashSet<TileCoordinate>>();
            foreach (SavedFixedSpatialStructure value in floor.FixedStructures ?? Array.Empty<SavedFixedSpatialStructure>())
            {
                FixedSpatialStructureDefinition definition = (catalog.FixedStructures ??
                    Array.Empty<FixedSpatialStructureDefinition>()).SingleOrDefault(item =>
                        item?.StructureDefinitionId == value.FixedStructureDefinitionId);
                if (definition == null || !TileFootprintResolver.TryResolveRectangle(definition.GrossFootprint,
                        value.Anchor, value.Orientation, workload, out ResolvedTileFootprint footprint))
                    return StructuralEditService.WorkloadReason;
                if (footprint.OccupiedTiles.Any(tile => !floorDefinition.Bounds.Contains(tile)))
                    return value.Kind == FixedSpatialStructureKind.CompletionTerminal
                        ? StructuralEditService.TerminalPlacementInvalidReason : StructuralEditService.OutOfBoundsReason;
                var set = new HashSet<TileCoordinate>(footprint.OccupiedTiles);
                if (rooms.Any(room => room.Overlaps(set))) return StructuralEditService.FixedOverlapReason;
                fixedSets.Add(set);
            }
            var corridors = new List<HashSet<TileCoordinate>>();
            foreach (FloorRouteEdge edge in (floor.Layout.Edges ?? Array.Empty<FloorRouteEdge>()).Where(value =>
                value?.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor))
            {
                var set = new HashSet<TileCoordinate>(edge.Footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>());
                if (set.Any(tile => !floorDefinition.Bounds.Contains(tile))) return StructuralEditService.OutOfBoundsReason;
                if (rooms.Any(room => room.Overlaps(set)) || fixedSets.Any(value => value.Overlaps(set)) ||
                    corridors.Any(value => value.Overlaps(set))) return StructuralEditService.CorridorOverlapReason;
                corridors.Add(set);
            }
            return null;
        }
        private static RoomSpatialInstance Room(SavedSpatialFloor floor, FloorRouteNode node) =>
            floor.Layout.Rooms.SingleOrDefault(value => value?.RoomInstanceId == node?.RoomInstanceId);
        private static RoomSpatialDefinition Definition(SpatialContentCatalog catalog, string id) =>
            (catalog.Rooms ?? Array.Empty<RoomSpatialDefinition>()).SingleOrDefault(value => value?.RoomDefinitionId == id);
        private static RoomSpatialDefinition UniqueDefinition(SpatialContentCatalog catalog, string id)
        { RoomSpatialDefinition[] values = (catalog.Rooms ?? Array.Empty<RoomSpatialDefinition>())
            .Where(value => value?.RoomDefinitionId == id).ToArray(); return values.Length == 1 ? values[0] : null; }
        private static bool Compatible(string a, string b, SpatialContentCatalog catalog) =>
            (catalog.SocketTypes ?? Array.Empty<SpatialSocketTypeDefinition>()).Any(value => value?.SocketTypeId == a &&
                (value.CompatibleSocketTypeIds ?? Array.Empty<string>()).Contains(b));
        private static TileCoordinate World(SpatialConnectionPointDefinition point, TileEndpoint endpoint)
        { TileCoordinate offset = StructuralEditService.TransformConnectionPointOffset(point.Offset,
            endpoint.Orientation, endpoint.Footprint); return Add(endpoint.Anchor, offset); }
        private static CardinalOrientation Opposite(CardinalOrientation value) => (CardinalOrientation)(((int)value + 2) % 4);
        private static TileCoordinate Step(TileCoordinate value, CardinalOrientation facing) => facing == CardinalOrientation.Zero
            ? new TileCoordinate(value.X, value.Y + 1) : facing == CardinalOrientation.Ninety
            ? new TileCoordinate(value.X + 1, value.Y) : facing == CardinalOrientation.OneEighty
            ? new TileCoordinate(value.X, value.Y - 1) : new TileCoordinate(value.X - 1, value.Y);
        private static TileCoordinate StepToward(TileCoordinate from, TileCoordinate to) =>
            new TileCoordinate(from.X == to.X ? from.X : from.X + Math.Sign(to.X - from.X),
                from.Y == to.Y ? from.Y : from.Y + Math.Sign(to.Y - from.Y));
        private static TileCoordinate Add(TileCoordinate a, TileCoordinate b) => new TileCoordinate(a.X + b.X, a.Y + b.Y);
        private static TileCoordinate Delta(TileCoordinate from, TileCoordinate to) => new TileCoordinate(to.X - from.X, to.Y - from.Y);
        private static bool Clone(DetachedCanonicalSpatialSaveState state, CanonicalSpatialSerializationLimits limits,
            out DetachedCanonicalSpatialSaveState clone)
        { clone = null; SpatialContractResult<byte[]> bytes = CanonicalSpatialSaveSerializer.Serialize(state, limits);
          if (!bytes.IsValid) return false; SpatialContractResult<DetachedCanonicalSpatialSaveState> parsed =
              CanonicalSpatialSaveSerializer.Parse(bytes.Value, limits); clone = parsed.Value; return parsed.IsValid; }
        private static StructuralEditPreview Fail(StructuralEditPreview preview, string reason)
        { preview.DetachedCandidate = null; preview.ReasonCodes = new[] { reason }; return preview; }
        private static string Map(IEnumerable<FloorLayoutValidationIssue> issues)
        {
            FloorLayoutValidationReason[] reasons = issues.Select(value => value.Reason).ToArray();
            if (reasons.Contains(FloorLayoutValidationReason.CapacityExceeded)) return StructuralEditService.CapacityReason;
            if (reasons.Contains(FloorLayoutValidationReason.StructureTileOutsideFloorBounds)) return StructuralEditService.OutOfBoundsReason;
            if (reasons.Contains(FloorLayoutValidationReason.FootprintOverlap)) return StructuralEditService.RoomOverlapReason;
            if (reasons.Contains(FloorLayoutValidationReason.CorridorDefinitionGeometryMismatch)) return StructuralEditService.CorridorLengthReason;
            if (reasons.Contains(FloorLayoutValidationReason.UnreachableRoom)) return StructuralEditService.RequiredRouteReason;
            if (reasons.Contains(FloorLayoutValidationReason.RequiredRouteWithoutTerminal)) return StructuralEditService.CompletionUnreachableReason;
            return StructuralEditService.LayoutInvalidReason;
        }
    }
}
