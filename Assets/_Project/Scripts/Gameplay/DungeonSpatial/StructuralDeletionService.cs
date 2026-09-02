using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class StructuralDeletionRequest { public string TargetRoomInstanceId; }

    /// <summary>Pure schema-8 leaf deletion preview. All mutation occurs on detached candidates.</summary>
    public static class StructuralDeletionService
    {
        public const string NotLeafReason = "structural.edit.deletion_target_not_leaf";
        public const string MinimumRoomReason = "structural.edit.deletion_minimum_room";
        public const string PolicyUnavailableReason = "structural.edit.removal_policy_unavailable";

        public static StructuralEditPreview Invalid(string reason, StructuralDeletionRequest request) =>
            new StructuralEditPreview { Operation = StructuralEditOperation.Deletion,
                TargetRoomInstanceId = request?.TargetRoomInstanceId, ReasonCodes = new[] { reason } };

        public static StructuralEditPreview Preview(DetachedCanonicalSpatialSaveState current,
            StructuralDeletionRequest request, StructuralContentRemovalPolicySnapshot policy,
            ProductionSpatialContentSnapshot production, RunSimulationConfig configuration,
            CanonicalSpatialSerializationLimits limits)
        {
            var result = Invalid(StructuralEditService.InvalidContextReason, request);
            if (current?.Authority == null || request == null || policy == null || production == null ||
                configuration == null || !limits.IsValid || current.Floors?.Length != 1) return result;
            if (!StructuralEditService.TryFingerprint(current, limits, out string fingerprint) ||
                !Clone(current, limits, out DetachedCanonicalSpatialSaveState seed)) return result;
            result.BaselineFingerprint = fingerprint; result.Intent = new StructuralDeletionRequest
                { TargetRoomInstanceId = request.TargetRoomInstanceId };
            SavedSpatialFloor floor = seed.Floors[0]; SpatialContentCatalog catalog = production.Catalog;
            FloorRouteNode[] nodes = floor.Layout?.Nodes ?? Array.Empty<FloorRouteNode>();
            FloorRouteEdge[] edges = floor.Layout?.Edges ?? Array.Empty<FloorRouteEdge>();
            RoomSpatialInstance[] rooms = floor.Layout?.Rooms ?? Array.Empty<RoomSpatialInstance>();
            FloorRouteNode[] targetNodes = nodes.Where(n => n?.Kind == FloorRouteNodeKind.Room &&
                n.RoomInstanceId == request.TargetRoomInstanceId).ToArray();
            RoomSpatialInstance[] targets = rooms.Where(r => r?.RoomInstanceId == request.TargetRoomInstanceId).ToArray();
            if (targetNodes.Length != 1 || targets.Length != 1)
                return Fail(result, StructuralEditService.TargetRoomNotFoundReason);
            FloorRouteNode targetNode = targetNodes[0]; RoomSpatialInstance target = targets[0];
            CanonicalRoomSemantics[] semantics = floor.RoomContents?.RoomSemantics ?? Array.Empty<CanonicalRoomSemantics>();
            CanonicalRoomSemantics[] targetSemantics = semantics.Where(s => s?.RoomInstanceId == target.RoomInstanceId &&
                s.LegacyRoomOriginKind != LegacyRoomOriginKind.ImplicitCompatibilityContainer).ToArray();
            if (targetSemantics.Length != 1) return Fail(result, StructuralEditService.TargetRoomNotBuildableReason);
            FloorRouteNode completion = nodes.SingleOrDefault(n => n?.Kind == FloorRouteNodeKind.Completion);
            FloorRouteEdge[] outgoing = edges.Where(e => e?.Classification == RouteClassification.Required &&
                e.SourceNodeId == targetNode.NodeId && e.DestinationNodeId == completion?.NodeId).ToArray();
            FloorRouteEdge[] incoming = edges.Where(e => e?.Classification == RouteClassification.Required &&
                e.DestinationNodeId == targetNode.NodeId).ToArray();
            if (completion == null || incoming.Length != 1 || outgoing.Length != 1 || edges.Any(e => e != null &&
                e.SourceNodeId == targetNode.NodeId && e.EdgeId != outgoing[0].EdgeId)) return Fail(result, NotLeafReason);
            FloorRouteNode predecessor = nodes.SingleOrDefault(n => n?.NodeId == incoming[0].SourceNodeId);
            RoomSpatialInstance predecessorRoom = rooms.SingleOrDefault(r => r?.RoomInstanceId == predecessor?.RoomInstanceId);
            if (predecessorRoom == null) return Fail(result, MinimumRoomReason);
            if (!TryRequiredPathRoomIds(floor, out HashSet<string> requiredRoomIds))
                return Fail(result, StructuralEditService.RequiredRouteAmbiguousReason);
            if (semantics.Count(s => s != null && requiredRoomIds.Contains(s.RoomInstanceId) &&
                    s.LegacyRoomOriginKind != LegacyRoomOriginKind.ImplicitCompatibilityContainer) <= 1)
                return Fail(result, MinimumRoomReason);

            RoomContentAssignment[] assigned = (floor.RoomContents?.Assignments ?? Array.Empty<RoomContentAssignment>())
                .Where(a => a?.RoomInstanceId == target.RoomInstanceId).OrderBy(a => a.AssignmentId, StringComparer.Ordinal).ToArray();
            var dispositions = new List<Tuple<RoomContentAssignment, StructuralContentRemovalPolicy>>();
            var blockers = new List<string>();
            foreach (RoomContentAssignment assignment in assigned)
            {
                if (!StructuralContentRemovalPolicyAuthority.TryResolve(policy, assignment.CategoryId,
                        assignment.OptionId, out StructuralContentRemovalPolicy disposition, out string reason))
                { blockers.Add(assignment.OptionId); continue; }
                dispositions.Add(Tuple.Create(assignment, disposition));
            }
            if (blockers.Count != 0)
            { result.BlockingContentOptionIds = blockers.OrderBy(value => value, StringComparer.Ordinal).ToArray();
              return Fail(result, StructuralContentRemovalPolicyAuthority.MissingOrUnresolvedReason); }
            RoomSpatialDefinition predecessorDefinition = catalog.Rooms.SingleOrDefault(r =>
                r?.RoomDefinitionId == predecessorRoom.RoomDefinitionId);
            RoomSpatialDefinition targetDefinition = catalog.Rooms.SingleOrDefault(r =>
                r?.RoomDefinitionId == target.RoomDefinitionId);
            FixedSpatialStructureDefinition terminalDefinition = null;
            SavedFixedSpatialStructure terminal = floor.FixedStructures.SingleOrDefault(f =>
                f?.Kind == FixedSpatialStructureKind.CompletionTerminal);
            if (terminal != null) terminalDefinition = catalog.FixedStructures.SingleOrDefault(f =>
                f?.StructureDefinitionId == terminal.FixedStructureDefinitionId);
            SpatialConnectionPointDefinition terminalPoint = terminalDefinition?.ConnectionPoints?.SingleOrDefault();
            FloorSpatialConfiguration floorDefinition = catalog.Floors.SingleOrDefault(f =>
                f?.FloorDefinitionId == floor.FloorDefinitionId && f.FloorIndex == floor.FloorIndex);
            if (predecessorDefinition == null || targetDefinition == null || terminal == null || terminalDefinition == null ||
                terminalPoint == null || floorDefinition == null) return Fail(result, StructuralEditService.LayoutInvalidReason);
            var workload = new SpatialValidationWorkloadLimits(limits.Spatial.MaximumMaterializedTiles);
            string sourceGeometryReason = StructuralRenovationService.InspectGeometry(floor, floorDefinition,
                catalog, workload, true);
            if (sourceGeometryReason != null) return Fail(result, sourceGeometryReason);
            FloorLayoutValidationResult sourceValidation = FloorLayoutValidator.Validate(floor.Layout,
                floorDefinition, catalog.Rooms, catalog.Corridors, workload, floor.FixedStructures,
                catalog.FixedStructures);
            if (!sourceValidation.IsValid)
                return Fail(result, StructuralRenovationService.Map(sourceValidation.Issues));
            ExistingConnection[] relationships = ExistingRelationships(predecessorRoom, predecessorDefinition,
                target, targetDefinition, incoming[0], catalog).ToArray();
            if (relationships.Length == 0) return Fail(result, StructuralEditService.ConnectionUnavailableReason);
            if (relationships.Length != 1) return Fail(result, StructuralEditService.ConnectionAmbiguousReason);
            ExistingConnection relationship = relationships[0];
            SpatialConnectionPointDefinition predecessorPoint = relationship.PredecessorPoint;
            CardinalOrientation direction = StructuralEditService.Rotate(predecessorPoint.Facing, predecessorRoom.Orientation);
            CardinalOrientation[] terminalOrientations = (terminalDefinition.AllowedOrientations ??
                Array.Empty<CardinalOrientation>()).Where(orientation =>
                    StructuralEditService.Rotate(terminalPoint.Facing, orientation) == Opposite(direction) &&
                    Compatible(predecessorPoint.SocketTypeId, terminalPoint.SocketTypeId, catalog)).ToArray();
            if (terminalOrientations.Length == 0) return Fail(result, StructuralEditService.ConnectionUnavailableReason);
            if (terminalOrientations.Length != 1) return Fail(result, StructuralEditService.ConnectionAmbiguousReason);
            if (!Clone(seed, limits, out DetachedCanonicalSpatialSaveState candidate)) return result;
            SavedSpatialFloor cf = candidate.Floors[0]; SavedFixedSpatialStructure ct = cf.FixedStructures.Single(f =>
                    f.FixedStructureInstanceId == terminal.FixedStructureInstanceId);
            TileCoordinate terminalSocket = relationship.TargetWorld;
            ct.Anchor = AnchorFor(terminalSocket, terminalPoint.Offset, terminalOrientations[0],
                terminalDefinition.GrossFootprint); ct.Orientation = terminalOrientations[0];
                cf.Layout.Rooms = cf.Layout.Rooms.Where(r => r.RoomInstanceId != target.RoomInstanceId).ToArray();
                cf.Layout.Nodes = cf.Layout.Nodes.Where(n => n.NodeId != targetNode.NodeId).ToArray();
                cf.Layout.Edges = cf.Layout.Edges.Where(e => e.EdgeId != incoming[0].EdgeId && e.EdgeId != outgoing[0].EdgeId).ToArray();
                cf.RoomContents.Assignments = cf.RoomContents.Assignments.Where(a => a.RoomInstanceId != target.RoomInstanceId).ToArray();
                cf.RoomContents.RoomSemantics = cf.RoomContents.RoomSemantics.Where(s => s.RoomInstanceId != target.RoomInstanceId).ToArray();
                foreach (var disposition in dispositions)
                    if (disposition.Item2 == StructuralContentRemovalPolicy.ReturnToPlayerCustody)
                        candidate.LifecycleAndOwnership.ReturnedContents = candidate.LifecycleAndOwnership.ReturnedContents.Concat(new[] {
                            new ReturnedStructuralContent { AssignmentId = disposition.Item1.AssignmentId,
                                CategoryId = disposition.Item1.CategoryId, OptionId = disposition.Item1.OptionId,
                                Sequence = disposition.Item1.Sequence, RemovalDisposition = StructuralContentRemovalDisposition.ReturnToPlayerCustody }}).ToArray();
            if (!NativeStructuralIdentity.TryAllocateFreshEdgeIdentity(candidate, cf.FloorInstanceId,
                    out string edgeId, out long next, out string identityReason)) return Fail(result, identityReason);
                candidate.LifecycleAndOwnership.Floors.Single(l => l.FloorInstanceId == cf.FloorInstanceId).NextNativeEdgeOrdinal = next;
            var replacementEdge = new FloorRouteEdge { EdgeId = edgeId,
                    FloorId = cf.FloorInstanceId, SourceNodeId = predecessor.NodeId, DestinationNodeId = completion.NodeId,
                    Classification = RouteClassification.Required, OptionalBranchId = string.Empty };
            cf.Layout.Edges = cf.Layout.Edges.Concat(new[] { replacementEdge }).ToArray();
            if (!StructuralRenovationService.TryResolveConnection(cf, predecessor, completion, floorDefinition,
                    catalog, out StructuralRenovationService.Connection connection, out string connectionReason))
                return Fail(result, connectionReason);
            replacementEdge.ConnectionKind = connection.Kind;
            replacementEdge.CorridorDefinitionId = connection.CorridorDefinitionId;
            replacementEdge.Footprint = connection.Kind == FloorRouteConnectionKind.PhysicalCorridor
                ? new ResolvedTileFootprint(connection.Tiles) : null;
            string geometryReason = StructuralRenovationService.InspectGeometry(cf, floorDefinition, catalog,
                workload, true);
            if (geometryReason != null) return Fail(result, geometryReason);
            if (!CanonicalSpatialSaveContracts.TryCanonicalize(candidate, limits.Spatial, out candidate))
                return Fail(result, StructuralEditService.WorkloadReason);
                replacementEdge = candidate.Floors[0].Layout.Edges.Single(e => e.EdgeId == edgeId);
                FloorLayoutValidationResult validation = FloorLayoutValidator.Validate(candidate.Floors[0].Layout,
                    floorDefinition, catalog.Rooms, catalog.Corridors, workload, candidate.Floors[0].FixedStructures,
                    catalog.FixedStructures);
            if (!validation.IsValid) return Fail(result, StructuralRenovationService.Map(validation.Issues));
            if (!CanonicalSpatialSaveContracts.Validate(candidate, limits.Spatial, true).IsValid ||
                !DetachedCanonicalProductionSemanticValidation.Validate(candidate, production, configuration,
                    limits.Spatial).IsValid) return Fail(result, StructuralEditService.LayoutInvalidReason);
                var preview = new StructuralEditPreview { Operation = StructuralEditOperation.Deletion,
                    TargetRoomInstanceId = target.RoomInstanceId, RoomDefinitionId = target.RoomDefinitionId,
                    BaselineFingerprint = fingerprint, Intent = result.Intent, DetachedCandidate = candidate,
                    PreviousUsedFloorSpace = sourceValidation.Capacity.UsedFloorSpaceCapacity,
                    ResultingUsedFloorSpace = validation.Capacity.UsedFloorSpaceCapacity,
                    ResultingRemainingFloorSpace = validation.Capacity.RemainingFloorSpaceCapacity,
                    ConnectionKind = replacementEdge.ConnectionKind,
                    IncomingConnectionTiles = replacementEdge.Footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>(),
                    ReasonCodes = Array.Empty<string>() };
                preview.Consequences = new[] { new StructuralChange { Kind = StructuralChangeKind.RoomRemoved, StableId = target.RoomInstanceId,
                    PreviousDefinitionId = target.RoomDefinitionId }, new StructuralChange { Kind = StructuralChangeKind.EdgeRemoved, StableId = incoming[0].EdgeId },
                    new StructuralChange { Kind = StructuralChangeKind.EdgeRemoved, StableId = outgoing[0].EdgeId },
                    new StructuralChange { Kind = StructuralChangeKind.EdgeReconnected, StableId = edgeId, ProposedConnectionKind = preview.ConnectionKind,
                        ProposedFootprint = replacementEdge.Footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>() }, new StructuralChange { Kind = StructuralChangeKind.FixedStructureMoved,
                        StableId = terminal.FixedStructureInstanceId, From = terminal.Anchor, To = ct.Anchor } }.Concat(dispositions.Select(d =>
                    new StructuralChange { Kind = d.Item2 == StructuralContentRemovalPolicy.ReturnToPlayerCustody
                            ? StructuralChangeKind.ContentReturned : StructuralChangeKind.ContentRemoved,
                        StableId = d.Item1.AssignmentId,
                        ProposedDefinitionId = d.Item1.OptionId })).OrderBy(c => c.Kind).ThenBy(c => c.StableId, StringComparer.Ordinal).ToArray();
            return preview;
        }

        private sealed class ExistingConnection
        { internal SpatialConnectionPointDefinition PredecessorPoint; internal TileCoordinate TargetWorld; }

        private static IEnumerable<ExistingConnection> ExistingRelationships(RoomSpatialInstance predecessor,
            RoomSpatialDefinition predecessorDefinition, RoomSpatialInstance target,
            RoomSpatialDefinition targetDefinition, FloorRouteEdge edge, SpatialContentCatalog catalog)
        {
            foreach (SpatialConnectionPointDefinition predecessorPoint in predecessorDefinition.ConnectionPoints ??
                Array.Empty<SpatialConnectionPointDefinition>())
            {
            TileCoordinate source = World(predecessorPoint.Offset, predecessor.Anchor, predecessor.Orientation,
                predecessorDefinition.GrossFootprint);
            CardinalOrientation facing = StructuralEditService.Rotate(predecessorPoint.Facing, predecessor.Orientation);
            foreach (SpatialConnectionPointDefinition targetPoint in targetDefinition.ConnectionPoints ?? Array.Empty<SpatialConnectionPointDefinition>())
            {
                if (!Compatible(predecessorPoint.SocketTypeId, targetPoint.SocketTypeId, catalog) ||
                    StructuralEditService.Rotate(targetPoint.Facing, target.Orientation) != Opposite(facing)) continue;
                TileCoordinate destination = World(targetPoint.Offset, target.Anchor, target.Orientation, targetDefinition.GrossFootprint);
                if (edge.ConnectionKind == FloorRouteConnectionKind.DirectDoorway)
                { if (Step(source, facing).Equals(destination)) yield return new ExistingConnection
                    { PredecessorPoint = predecessorPoint, TargetWorld = destination }; continue; }
                TileCoordinate[] tiles = edge.Footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>();
                CorridorSpatialDefinition corridor = (catalog.Corridors ?? Array.Empty<CorridorSpatialDefinition>())
                    .SingleOrDefault(value => value?.CorridorDefinitionId == edge.CorridorDefinitionId);
                int length = Math.Abs(destination.X - source.X) + Math.Abs(destination.Y - source.Y) - 1;
                bool horizontal = source.Y == destination.Y &&
                    (facing == CardinalOrientation.Ninety || facing == CardinalOrientation.TwoSeventy);
                CardinalOrientation axis = horizontal ? CardinalOrientation.Ninety : CardinalOrientation.Zero;
                if (corridor != null && corridor.Width == 1 && length >= corridor.MinimumLength &&
                    length <= corridor.MaximumLength && (corridor.AllowedOrientations ??
                        Array.Empty<CardinalOrientation>()).Contains(axis) &&
                    (corridor.CompatibleSocketTypeIds ?? Array.Empty<string>()).Contains(predecessorPoint.SocketTypeId) &&
                    (corridor.CompatibleSocketTypeIds ?? Array.Empty<string>()).Contains(targetPoint.SocketTypeId) &&
                    tiles.Contains(Step(source, facing)) &&
                    tiles.Contains(Step(destination, Opposite(facing)))) yield return new ExistingConnection
                        { PredecessorPoint = predecessorPoint, TargetWorld = destination };
            }
            }
        }

        private static bool TryRequiredPathRoomIds(SavedSpatialFloor floor, out HashSet<string> roomIds)
        {
            roomIds = new HashSet<string>(StringComparer.Ordinal);
            FloorRouteNode[] nodes = floor.Layout?.Nodes ?? Array.Empty<FloorRouteNode>();
            FloorRouteEdge[] required = (floor.Layout?.Edges ?? Array.Empty<FloorRouteEdge>()).Where(edge =>
                edge?.Classification == RouteClassification.Required).ToArray();
            FloorRouteNode current = nodes.SingleOrDefault(node => node?.Kind == FloorRouteNodeKind.Entrance);
            FloorRouteNode completion = nodes.SingleOrDefault(node => node?.Kind == FloorRouteNodeKind.Completion);
            if (current == null || completion == null) return false;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (current.NodeId != completion.NodeId && visited.Add(current.NodeId))
            {
                FloorRouteEdge[] outgoing = required.Where(edge => edge.SourceNodeId == current.NodeId).ToArray();
                if (outgoing.Length != 1) return false;
                current = nodes.SingleOrDefault(node => node?.NodeId == outgoing[0].DestinationNodeId);
                if (current == null) return false;
                if (current.Kind == FloorRouteNodeKind.Room) roomIds.Add(current.RoomInstanceId);
            }
            return current.NodeId == completion.NodeId && required.Length == visited.Count;
        }

        private static StructuralEditPreview Fail(StructuralEditPreview value, string reason) { value.DetachedCandidate = null; value.ReasonCodes = new[] { reason }; return value; }
        private static bool Clone(DetachedCanonicalSpatialSaveState state, CanonicalSpatialSerializationLimits limits, out DetachedCanonicalSpatialSaveState clone)
        { clone = null; var bytes = CanonicalSpatialSaveSerializer.Serialize(state, limits); if (!bytes.IsValid) return false;
          var parsed = CanonicalSpatialSaveSerializer.Parse(bytes.Value, limits); clone = parsed.Value; return parsed.IsValid; }
        private static CardinalOrientation Opposite(CardinalOrientation v) => (CardinalOrientation)(((int)v + 2) % 4);
        private static bool Compatible(string a, string b, SpatialContentCatalog c) => (c.SocketTypes ?? Array.Empty<SpatialSocketTypeDefinition>()).Any(s => s != null && s.SocketTypeId == a && (s.CompatibleSocketTypeIds ?? Array.Empty<string>()).Contains(b));
        private static TileCoordinate Step(TileCoordinate v, CardinalOrientation f) => f == CardinalOrientation.Zero ? new TileCoordinate(v.X,v.Y+1) : f == CardinalOrientation.Ninety ? new TileCoordinate(v.X+1,v.Y) : f == CardinalOrientation.OneEighty ? new TileCoordinate(v.X,v.Y-1) : new TileCoordinate(v.X-1,v.Y);
        private static TileCoordinate World(TileCoordinate o, TileCoordinate a, CardinalOrientation r, RectangularFootprintDefinition f) { var t=StructuralEditService.TransformConnectionPointOffset(o,r,f); return new TileCoordinate(a.X+t.X,a.Y+t.Y); }
        private static TileCoordinate AnchorFor(TileCoordinate w, TileCoordinate o, CardinalOrientation r, RectangularFootprintDefinition f) { var t=StructuralEditService.TransformConnectionPointOffset(o,r,f); return new TileCoordinate(w.X-t.X,w.Y-t.Y); }
    }
}
