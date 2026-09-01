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
                s.LegacyRoomOriginKind == LegacyRoomOriginKind.CanonicalPlayerPlaced).ToArray();
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
            if (semantics.Count(s => s?.LegacyRoomOriginKind == LegacyRoomOriginKind.CanonicalPlayerPlaced) <= 1)
                return Fail(result, MinimumRoomReason);

            RoomContentAssignment[] assigned = (floor.RoomContents?.Assignments ?? Array.Empty<RoomContentAssignment>())
                .Where(a => a?.RoomInstanceId == target.RoomInstanceId).OrderBy(a => a.AssignmentId, StringComparer.Ordinal).ToArray();
            var dispositions = new List<Tuple<RoomContentAssignment, StructuralContentRemovalPolicy>>();
            foreach (RoomContentAssignment assignment in assigned)
            {
                if (!StructuralContentRemovalPolicyAuthority.TryResolve(policy, assignment.CategoryId,
                        assignment.OptionId, out StructuralContentRemovalPolicy disposition, out string reason))
                    return Fail(result, reason);
                dispositions.Add(Tuple.Create(assignment, disposition));
            }
            RoomSpatialDefinition predecessorDefinition = catalog.Rooms.SingleOrDefault(r =>
                r?.RoomDefinitionId == predecessorRoom.RoomDefinitionId);
            FixedSpatialStructureDefinition terminalDefinition = null;
            SavedFixedSpatialStructure terminal = floor.FixedStructures.SingleOrDefault(f =>
                f?.Kind == FixedSpatialStructureKind.CompletionTerminal);
            if (terminal != null) terminalDefinition = catalog.FixedStructures.SingleOrDefault(f =>
                f?.StructureDefinitionId == terminal.FixedStructureDefinitionId);
            SpatialConnectionPointDefinition terminalPoint = terminalDefinition?.ConnectionPoints?.SingleOrDefault();
            FloorSpatialConfiguration floorDefinition = catalog.Floors.SingleOrDefault(f =>
                f?.FloorDefinitionId == floor.FloorDefinitionId && f.FloorIndex == floor.FloorIndex);
            if (predecessorDefinition == null || terminal == null || terminalDefinition == null ||
                terminalPoint == null || floorDefinition == null) return Fail(result, StructuralEditService.LayoutInvalidReason);

            var candidates = new List<StructuralEditPreview>(); bool lengthInvalid = false;
            CorridorSpatialDefinition[] corridors = (catalog.Corridors ?? Array.Empty<CorridorSpatialDefinition>())
                .Where(c => c != null && c.Category == CorridorSpatialCategory.Straight &&
                    (floorDefinition.AllowedCorridorDefinitionIds ?? Array.Empty<string>()).Contains(c.CorridorDefinitionId)).ToArray();
            foreach (SpatialConnectionPointDefinition point in predecessorDefinition.ConnectionPoints ?? Array.Empty<SpatialConnectionPointDefinition>())
            foreach (CardinalOrientation orientation in terminalDefinition.AllowedOrientations ?? Array.Empty<CardinalOrientation>())
            {
                CardinalOrientation facing = StructuralEditService.Rotate(point.Facing, predecessorRoom.Orientation);
                if (StructuralEditService.Rotate(terminalPoint.Facing, orientation) != Opposite(facing) ||
                    !Compatible(point.SocketTypeId, terminalPoint.SocketTypeId, catalog)) continue;
                TryCandidate(0, null, point, orientation);
                foreach (CorridorSpatialDefinition corridor in corridors)
                    for (int length = corridor.MinimumLength; length <= corridor.MaximumLength; length++)
                        TryCandidate(length, corridor, point, orientation);
            }
            if (candidates.Count == 0) return Fail(result, lengthInvalid ? StructuralEditService.CorridorLengthReason :
                StructuralEditService.ConnectionUnavailableReason);
            if (candidates.Count != 1) return Fail(result, StructuralEditService.ConnectionAmbiguousReason);
            return candidates[0];

            void TryCandidate(int length, CorridorSpatialDefinition corridor, SpatialConnectionPointDefinition point,
                CardinalOrientation terminalOrientation)
            {
                if (corridor != null && (corridor.Width != 1 || !(corridor.AllowedOrientations ??
                    Array.Empty<CardinalOrientation>()).Contains(facingAxis(point, predecessorRoom)))) { lengthInvalid = true; return; }
                if (!Clone(seed, limits, out DetachedCanonicalSpatialSaveState candidate)) return;
                SavedSpatialFloor cf = candidate.Floors[0]; SavedFixedSpatialStructure ct = cf.FixedStructures.Single(f =>
                    f.FixedStructureInstanceId == terminal.FixedStructureInstanceId);
                TileCoordinate source = World(point.Offset, predecessorRoom.Anchor, predecessorRoom.Orientation,
                    predecessorDefinition.GrossFootprint); CardinalOrientation direction = StructuralEditService.Rotate(point.Facing, predecessorRoom.Orientation);
                TileCoordinate socket = source; var tiles = new List<TileCoordinate>();
                for (int i = 0; i < length; i++) { socket = Step(socket, direction); tiles.Add(socket); }
                socket = Step(socket, direction); ct.Anchor = AnchorFor(socket, terminalPoint.Offset,
                    terminalOrientation, terminalDefinition.GrossFootprint); ct.Orientation = terminalOrientation;
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
                    out string edgeId, out long next, out _)) return;
                candidate.LifecycleAndOwnership.Floors.Single(l => l.FloorInstanceId == cf.FloorInstanceId).NextNativeEdgeOrdinal = next;
                cf.Layout.Edges = cf.Layout.Edges.Concat(new[] { new FloorRouteEdge { EdgeId = edgeId,
                    FloorId = cf.FloorInstanceId, SourceNodeId = predecessor.NodeId, DestinationNodeId = completion.NodeId,
                    Classification = RouteClassification.Required, OptionalBranchId = string.Empty,
                    ConnectionKind = corridor == null ? FloorRouteConnectionKind.DirectDoorway : FloorRouteConnectionKind.PhysicalCorridor,
                    CorridorDefinitionId = corridor?.CorridorDefinitionId ?? string.Empty,
                    Footprint = corridor == null ? null : new ResolvedTileFootprint(tiles.OrderBy(t => t).ToArray()) }}).ToArray();
                if (!CanonicalSpatialSaveContracts.TryCanonicalize(candidate, limits.Spatial, out candidate)) return;
                var workload = new SpatialValidationWorkloadLimits(limits.Spatial.MaximumMaterializedTiles);
                FloorLayoutValidationResult validation = FloorLayoutValidator.Validate(candidate.Floors[0].Layout,
                    floorDefinition, catalog.Rooms, catalog.Corridors, workload, candidate.Floors[0].FixedStructures,
                    catalog.FixedStructures);
                if (!validation.IsValid || !CanonicalSpatialSaveContracts.Validate(candidate, limits.Spatial, true).IsValid ||
                    !DetachedCanonicalProductionSemanticValidation.Validate(candidate, production, configuration, limits.Spatial).IsValid) return;
                var preview = new StructuralEditPreview { Operation = StructuralEditOperation.Deletion,
                    TargetRoomInstanceId = target.RoomInstanceId, RoomDefinitionId = target.RoomDefinitionId,
                    BaselineFingerprint = fingerprint, Intent = result.Intent, DetachedCandidate = candidate,
                    PreviousUsedFloorSpace = Used(seed, floorDefinition, catalog, workload),
                    ResultingUsedFloorSpace = validation.Capacity.UsedFloorSpaceCapacity,
                    ResultingRemainingFloorSpace = validation.Capacity.RemainingFloorSpaceCapacity,
                    ConnectionKind = corridor == null ? FloorRouteConnectionKind.DirectDoorway : FloorRouteConnectionKind.PhysicalCorridor,
                    IncomingConnectionTiles = tiles.ToArray(), ReasonCodes = Array.Empty<string>() };
                preview.Consequences = new[] { new StructuralChange { Kind = StructuralChangeKind.RoomRemoved, StableId = target.RoomInstanceId,
                    PreviousDefinitionId = target.RoomDefinitionId }, new StructuralChange { Kind = StructuralChangeKind.EdgeRemoved, StableId = incoming[0].EdgeId },
                    new StructuralChange { Kind = StructuralChangeKind.EdgeRemoved, StableId = outgoing[0].EdgeId },
                    new StructuralChange { Kind = StructuralChangeKind.EdgeReconnected, StableId = edgeId, ProposedConnectionKind = preview.ConnectionKind,
                        ProposedFootprint = tiles.ToArray() }, new StructuralChange { Kind = StructuralChangeKind.FixedStructureMoved,
                        StableId = terminal.FixedStructureInstanceId, From = terminal.Anchor, To = ct.Anchor } }.Concat(dispositions.Select(d =>
                    new StructuralChange { Kind = StructuralChangeKind.ContentReturned, StableId = d.Item1.AssignmentId,
                        ProposedDefinitionId = d.Item1.OptionId })).OrderBy(c => c.Kind).ThenBy(c => c.StableId, StringComparer.Ordinal).ToArray();
                candidates.Add(preview);
            }
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
        private static CardinalOrientation facingAxis(SpatialConnectionPointDefinition p, RoomSpatialInstance r) { var f=StructuralEditService.Rotate(p.Facing,r.Orientation); return f==CardinalOrientation.Ninety||f==CardinalOrientation.TwoSeventy?CardinalOrientation.Ninety:CardinalOrientation.Zero; }
        private static int Used(DetachedCanonicalSpatialSaveState s, FloorSpatialConfiguration f, SpatialContentCatalog c, SpatialValidationWorkloadLimits w) => FloorLayoutValidator.Validate(s.Floors[0].Layout,f,c.Rooms,c.Corridors,w,s.Floors[0].FixedStructures,c.FixedStructures).Capacity.UsedFloorSpaceCapacity;
    }
}
