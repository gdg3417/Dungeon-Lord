using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    [Serializable]
    public sealed class StructuralRouteInsertionRequest
    {
        public string FloorInstanceId;
        public string ReplacedEdgeId;
        public string RoomInstanceId;
        public AuthoredRouteGeometry Incoming;
        public AuthoredRouteGeometry Outgoing;
    }

    public sealed class StructuralRouteInsertionPreview
    {
        public string Reason { get; internal set; }
        public bool IsValid => Candidate != null && Reason == null;
        public int UsedFloorSpace { get; internal set; }
        public int FloorCapacity { get; internal set; }
        public string[] RequiredRoomOrder { get; internal set; } = Array.Empty<string>();
        internal string IncomingEdgeId { get; set; }
        internal string OutgoingEdgeId { get; set; }
        internal DetachedCanonicalSpatialSaveState Candidate { get; set; }
    }

    internal static class StructuralRouteInsertionService
    {
        internal const string RetirementUnavailable = "structural.edit.insertion_retirement_unavailable";

        internal static bool RetirementEligible(DetachedCompleteSaveValidationResult owned, string floorId, string edgeId)
        {
            if (owned?.CurrentTargetValidated != true || !owned.IsValid ||
                owned.Investment == null || owned.CorridorContent?.Assignments == null) return false;
            var floor = owned.State.Floors.SingleOrDefault(f => f.FloorInstanceId == floorId);
            var edge = FindEdge(floor, edgeId);
            if (!RequiredDirect(edge) || owned.CorridorContent.Assignments.Any(a => a.EdgeId == edgeId)) return false;
            var investment = owned.Investment.Where(i => i.StructureId == edgeId).ToArray();
            // A direct doorway can carry investment from earlier work. Its footprint
            // gives no indication of whether there is an investment to retire.
            return investment.Length == 1 && investment[0].ConstructionMana == 0d &&
                investment[0].RenovationMana == 0d;
        }

        internal static StructuralRouteInsertionPreview Preview(DungeonDraftContext context,
            DetachedCanonicalSpatialSaveState baseline, DetachedCanonicalSpatialSaveState presented,
            StructuralRouteInsertionRequest request, DungeonDraftRouteInsertion previous,
            string[] disconnectedRoomIds)
        {
            if (request?.Incoming?.Passage == null || request.Outgoing?.Passage == null ||
                !CanonicalSaveSchemaVersions.HasAuthoredRoutes(CanonicalSaveSchemaVersions.CurrentWritableTarget))
                return Fail(StructuralEditService.InvalidContextReason);
            var baseFloor = baseline.Floors.SingleOrDefault(f => f.FloorInstanceId == request.FloorInstanceId);
            var original = FindEdge(baseFloor, request.ReplacedEdgeId);
            if (!RequiredDirect(original)) return Fail(RetirementUnavailable);

            var candidate = context.Copy(presented);
            var floor = candidate?.Floors.SingleOrDefault(f => f.FloorInstanceId == request.FloorInstanceId);
            var node = floor?.Layout.Nodes.SingleOrDefault(n => n.RoomInstanceId == request.RoomInstanceId);
            if (node == null) return Fail(StructuralEditService.TargetRoomNotFoundReason);
            if (previous != null && !RestorePreviousCandidate(baseFloor, floor, previous))
                return Fail(RetirementUnavailable);
            var replaced = FindEdge(floor, original.EdgeId);
            if (!RequiredDirect(replaced) || !SameDirectConnection(original, replaced) ||
                floor.Layout.Edges.Any(e => e.SourceNodeId == node.NodeId || e.DestinationNodeId == node.NodeId))
                return Fail(RetirementUnavailable);
            if (!RequiredFloorTraversal.TryResolve(floor.Layout, out var oldRoute) ||
                !oldRoute.Zip(oldRoute.Skip(1), (a, b) => (a.NodeId, b.NodeId)).Contains((replaced.SourceNodeId, replaced.DestinationNodeId)))
                return Fail(StructuralEditService.RequiredRouteReason);

            var lifecycle = candidate.LifecycleAndOwnership.Floors.Single(f => f.FloorInstanceId == floor.FloorInstanceId);
            if (!TryAdd(candidate, floor, lifecycle, replaced.SourceNodeId, node.NodeId, request.Incoming, out var incoming) ||
                !TryAdd(candidate, floor, lifecycle, node.NodeId, replaced.DestinationNodeId, request.Outgoing, out var outgoing))
                return Fail(StructuralEditService.InvalidIdentityReason);
            floor.Layout.Edges = floor.Layout.Edges.Where(e => e.EdgeId != replaced.EdgeId).ToArray();

            var catalog = context.Production.Catalog;
            if (!StructuralRenovationService.TryResolveSavedConnection(baseFloor, original, catalog, out _, out _))
                return Fail(RetirementUnavailable);
            if (!incoming.AuthoredGeometry.TryResolve(floor, incoming, catalog, out _, out _, out _) ||
                !outgoing.AuthoredGeometry.TryResolve(floor, outgoing, catalog, out _, out _, out _))
                return Fail(StructuralEditService.ConnectionUnavailableReason);
            if (!RequiredFloorTraversal.TryResolve(floor.Layout, out var route) || route.Length != oldRoute.Length + 1 ||
                !route.Where(n => n.NodeId != node.NodeId).Select(n => n.NodeId).SequenceEqual(oldRoute.Select(n => n.NodeId)))
                return Fail(StructuralEditService.RequiredRouteReason);
            var floorDefinition = catalog.Floors.Single(f => f.FloorDefinitionId == floor.FloorDefinitionId);
            var workload = new SpatialValidationWorkloadLimits(context.Limits.Canonical.Spatial.MaximumMaterializedTiles);
            var layout = FloorLayoutValidator.Validate(floor.Layout, floorDefinition, catalog.Rooms, catalog.Corridors,
                workload, floor.FixedStructures, catalog.FixedStructures, CanonicalEditFloorTarget.Mode(floor));
            var remaining = disconnectedRoomIds.Where(id => id != request.RoomInstanceId).ToArray();
            var issues = layout.Issues.Where(issue => !AllowedIncomplete(issue, floor, remaining)).ToArray();
            if (issues.Any(i => i.Reason == FloorLayoutValidationReason.CapacityExceeded))
                return Fail(StructuralEditService.CapacityReason);
            if (issues.Any(i => i.Reason == FloorLayoutValidationReason.StructureTileOutsideFloorBounds))
                return Fail(StructuralEditService.OutOfBoundsReason);
            if (issues.Any(i => i.Reason == FloorLayoutValidationReason.FootprintOverlap))
                return Fail(StructuralEditService.CorridorOverlapReason);
            if (issues.Length > 0 || !context.Validate(candidate, remaining))
                return Fail(StructuralEditService.LayoutInvalidReason);

            var canonical = context.Copy(candidate);
            if (canonical == null) return Fail(StructuralEditService.WorkloadReason);
            return new StructuralRouteInsertionPreview
            {
                Candidate = canonical, IncomingEdgeId = incoming.EdgeId, OutgoingEdgeId = outgoing.EdgeId,
                UsedFloorSpace = layout.Capacity.UsedFloorSpaceCapacity, FloorCapacity = layout.Capacity.FinalFloorSpaceCapacity,
                RequiredRoomOrder = route.Where(n => n.Kind == FloorRouteNodeKind.Room).Select(n => n.RoomInstanceId).ToArray()
            };
        }

        // Called only with IDs of independently placed rooms from the bound draft.
        internal static bool AllowedIncomplete(FloorLayoutValidationIssue issue, SavedSpatialFloor floor, string[] allowed) =>
            issue.Reason == FloorLayoutValidationReason.UnreachableRoom && allowed != null &&
            allowed.Contains(issue.RelatedId) &&
            !floor.Layout.Edges.Any(e => e?.SourceNodeId == issue.SubjectId || e?.DestinationNodeId == issue.SubjectId);

        // A bounded geometry suggestion. The complete net-price comparison is a separate settlement contract.
        private const int MaximumSuggestionPreviews = 16;
        private static readonly TileCoordinate[] CardinalSteps =
        {
            new TileCoordinate(-1, 0), new TileCoordinate(0, -1),
            new TileCoordinate(0, 1), new TileCoordinate(1, 0)
        };

        internal static StructuralRouteInsertionRequest Suggest(DungeonDraftContext context,
            DetachedCanonicalSpatialSaveState presented, FloorRouteEdge edge, string roomId,
            Func<StructuralRouteInsertionRequest, (bool Valid, double? GrossCost)> evaluate)
        {
            var floor = presented.Floors.SingleOrDefault(value => value.FloorInstanceId == edge.FloorId);
            var node = floor?.Layout.Nodes.SingleOrDefault(n => n.RoomInstanceId == roomId);
            if (node == null) return null;
            var catalog = context.Production.Catalog;
            var configured = catalog.Floors.Single(f => f.FloorDefinitionId == floor.FloorDefinitionId);
            var corridor = catalog.Corridors.SingleOrDefault(c => c.CorridorDefinitionId ==
                OptionalBranchStructuralEditService.CorridorDefinitionId);
            var limits = new SpatialValidationWorkloadLimits(context.Limits.Canonical.Spatial.MaximumMaterializedTiles);
            if (corridor?.Category != CorridorSpatialCategory.Orthogonal || !configured.Bounds.IsValid ||
                !limits.Allows(corridor.MaximumLength) || !configured.AllowedCorridorDefinitionIds.Contains(corridor.CorridorDefinitionId))
                return null;

            var occupied = new HashSet<TileCoordinate>();
            foreach (var room in floor.Layout.Rooms)
            {
                var definition = catalog.Rooms.Single(d => d.RoomDefinitionId == room.RoomDefinitionId);
                if (!definition.TryResolveGrossTiles(room.Anchor, room.Orientation, limits, out var footprint)) return null;
                occupied.UnionWith(footprint.OccupiedTiles);
            }
            foreach (var structure in floor.FixedStructures)
            {
                var definition = catalog.FixedStructures.Single(d => d.StructureDefinitionId == structure.FixedStructureDefinitionId);
                if (!TileFootprintResolver.TryResolveRectangle(definition.GrossFootprint, structure.Anchor,
                    structure.Orientation, limits, out var footprint)) return null;
                occupied.UnionWith(footprint.OccupiedTiles);
            }
            foreach (var connection in floor.Layout.Edges)
                occupied.UnionWith(connection.CorridorTiles ?? Array.Empty<TileCoordinate>());

            var from = AuthoredRouteGeometry.Openings(floor, edge.SourceNodeId, catalog);
            var to = AuthoredRouteGeometry.Openings(floor, edge.DestinationNodeId, catalog);
            var roomOpenings = AuthoredRouteGeometry.Openings(floor, node.NodeId, catalog);
            var candidates = new List<StructuralRouteInsertionRequest>();
            foreach (var start in from)
            foreach (var roomEntry in roomOpenings)
            {
                var incoming = SuggestLink(start, roomEntry, catalog, corridor, configured.Bounds, limits, occupied);
                if (incoming == null) continue;
                var withIncoming = new HashSet<TileCoordinate>(occupied);
                withIncoming.UnionWith(incoming.Passage);
                foreach (var roomExit in roomOpenings.Where(p => p.Id != roomEntry.Id))
                foreach (var end in to)
                {
                    var outgoing = SuggestLink(roomExit, end, catalog, corridor, configured.Bounds, limits, withIncoming);
                    if (outgoing == null) continue;
                    candidates.Add(new StructuralRouteInsertionRequest
                    {
                        FloorInstanceId = floor.FloorInstanceId, RoomInstanceId = roomId,
                        ReplacedEdgeId = edge.EdgeId, Incoming = incoming, Outgoing = outgoing
                    });
                }
            }
            // Point choices are enumerated by ordinal ID and BFS neighbors by coordinates.
            // Stable sorting preserves those ties while preferring less travel.
            var evaluated = candidates.OrderBy(value => value.Incoming.Passage.Length + value.Outgoing.Passage.Length)
                .Take(MaximumSuggestionPreviews)
                .Select(candidate => (Candidate: candidate, Assessment: evaluate(candidate)))
                .Where(value => value.Assessment.Valid).ToArray();
            if (evaluated.Length == 0) return null;
            return evaluated.All(value => value.Assessment.GrossCost.HasValue)
                ? evaluated.OrderBy(value => value.Assessment.GrossCost.Value).First().Candidate
                : evaluated[0].Candidate;
        }

        private static AuthoredRouteGeometry SuggestLink(RouteOpening from, RouteOpening to,
            SpatialContentCatalog catalog, CorridorSpatialDefinition corridor, RectangularFloorBounds bounds,
            SpatialValidationWorkloadLimits limits, HashSet<TileCoordinate> occupied)
        {
            string firstSocket = from.SocketTypeId, secondSocket = to.SocketTypeId;
            if (!catalog.SocketTypes.Any(socket => socket.SocketTypeId == firstSocket &&
                socket.CompatibleSocketTypeIds.Contains(secondSocket))) return null;
            var link = new AuthoredRouteGeometry { SourcePointId = from.Id, DestinationPointId = to.Id };
            if (from.OpensToward(to.World) && to.OpensToward(from.World)) return link;
            if (!corridor.CompatibleSocketTypeIds.Contains(firstSocket) || !corridor.CompatibleSocketTypeIds.Contains(secondSocket) ||
                !corridor.AllowedOrientations.Contains((CardinalOrientation)((int)from.Facing % 2)) ||
                !corridor.AllowedOrientations.Contains((CardinalOrientation)((int)to.Facing % 2)) ||
                !TryOutside(from, out var start) || !TryOutside(to, out var end) ||
                !bounds.Contains(start) || !bounds.Contains(end) || occupied.Contains(start) || occupied.Contains(end))
                return null;

            var parents = new Dictionary<TileCoordinate, TileCoordinate> { [start] = start };
            var pending = new Queue<(TileCoordinate Cell, int Length)>();
            pending.Enqueue((start, 1));
            while (pending.Count != 0)
            {
                var current = pending.Dequeue();
                if (current.Cell.Equals(end))
                {
                    if (current.Length < corridor.MinimumLength) return null;
                    link.Passage = new TileCoordinate[current.Length];
                    var cell = current.Cell;
                    for (int i = link.Passage.Length - 1; i >= 0; i--)
                    {
                        link.Passage[i] = cell;
                        cell = parents[cell];
                    }
                    return link;
                }
                if (current.Length == corridor.MaximumLength) continue;
                foreach (var step in CardinalSteps)
                {
                    if (!corridor.AllowedOrientations.Contains(step.X == 0 ? CardinalOrientation.Zero : CardinalOrientation.Ninety) ||
                        !TryOffset(current.Cell, step, out var next) || !bounds.Contains(next) ||
                        occupied.Contains(next) || parents.ContainsKey(next)) continue;
                    if (!limits.Allows((long)parents.Count + 1)) return null;
                    parents.Add(next, current.Cell);
                    pending.Enqueue((next, current.Length + 1));
                }
            }
            return null;
        }

        private static bool TryOutside(RouteOpening opening, out TileCoordinate cell)
        {
            TileCoordinate step;
            switch (opening.Facing)
            {
                case CardinalOrientation.Zero: step = new TileCoordinate(0, 1); break;
                case CardinalOrientation.Ninety: step = new TileCoordinate(1, 0); break;
                case CardinalOrientation.OneEighty: step = new TileCoordinate(0, -1); break;
                default: step = new TileCoordinate(-1, 0); break;
            }
            return TryOffset(opening.World, step, out cell);
        }

        private static bool TryOffset(TileCoordinate cell, TileCoordinate step, out TileCoordinate next)
        {
            long x = (long)cell.X + step.X, y = (long)cell.Y + step.Y;
            next = default;
            if (x < int.MinValue || x > int.MaxValue || y < int.MinValue || y > int.MaxValue) return false;
            next = new TileCoordinate((int)x, (int)y);
            return true;
        }

        private static bool RestorePreviousCandidate(SavedSpatialFloor baseline, SavedSpatialFloor floor, DungeonDraftRouteInsertion previous)
        {
            var old = FindEdge(baseline, previous.Request.ReplacedEdgeId);
            var incoming = FindEdge(floor, previous.ExpectedIncomingEdgeId);
            var outgoing = FindEdge(floor, previous.ExpectedOutgoingEdgeId);
            if (!RequiredDirect(old) || incoming?.AuthoredGeometry == null || outgoing?.AuthoredGeometry == null ||
                FindEdge(floor, old.EdgeId) != null) return false;
            floor.Layout.Edges = floor.Layout.Edges.Where(e => e != incoming && e != outgoing).Append(old).ToArray();
            return true;
        }

        private static bool TryAdd(DetachedCanonicalSpatialSaveState candidate, SavedSpatialFloor floor,
            FloorStructuralIdentityLifecycle lifecycle, string source, string destination, AuthoredRouteGeometry geometry,
            out FloorRouteEdge edge)
        {
            edge = null;
            if (!NativeStructuralIdentity.TryAllocateFreshEdgeIdentity(candidate, floor.FloorInstanceId,
                out string id, out long next, out _)) return false;
            lifecycle.NextNativeEdgeOrdinal = next;
            bool direct = geometry.Passage.Length == 0;
            edge = new FloorRouteEdge
            {
                EdgeId = id, FloorId = floor.FloorInstanceId, SourceNodeId = source, DestinationNodeId = destination,
                Classification = RouteClassification.Required, OptionalBranchId = string.Empty,
                ConnectionKind = direct ? FloorRouteConnectionKind.DirectDoorway : FloorRouteConnectionKind.PhysicalCorridor,
                CorridorDefinitionId = direct ? string.Empty : OptionalBranchStructuralEditService.CorridorDefinitionId,
                AuthoredGeometry = geometry.Copy()
            };
            floor.Layout.Edges = floor.Layout.Edges.Append(edge).ToArray();
            return true;
        }

        private static FloorRouteEdge FindEdge(SavedSpatialFloor floor, string edgeId) =>
            floor?.Layout.Edges.SingleOrDefault(e => e.EdgeId == edgeId);

        private static bool RequiredDirect(FloorRouteEdge edge) =>
            edge != null && edge.Classification == RouteClassification.Required &&
            edge.ConnectionKind == FloorRouteConnectionKind.DirectDoorway && string.IsNullOrEmpty(edge.OptionalBranchId) &&
            string.IsNullOrEmpty(edge.CorridorDefinitionId) && edge.Footprint == null;

        private static bool SameDirectConnection(FloorRouteEdge first, FloorRouteEdge second) =>
            first.SourceNodeId == second.SourceNodeId && first.DestinationNodeId == second.DestinationNodeId &&
            first.AuthoredGeometry?.SourcePointId == second.AuthoredGeometry?.SourcePointId &&
            first.AuthoredGeometry?.DestinationPointId == second.AuthoredGeometry?.DestinationPointId;

        internal static StructuralRouteInsertionPreview Fail(string reason) =>
            new StructuralRouteInsertionPreview { Reason = reason };
    }
}
