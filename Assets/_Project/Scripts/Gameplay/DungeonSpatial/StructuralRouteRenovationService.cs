using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum StructuralRouteRenovationKind
    {
        Reset = 1,
        Disconnect = 2,
        Connect = 3,
        MoveStructure = 4,
        Retire = 5,
        ConnectBranch = 6,
        ReassignCorridorContent = 7,
        ReturnCorridorContent = 8,
        CancelCorridorDisposition = 9
    }

    [Serializable]
    public sealed class StructuralRouteRenovation
    {
        public StructuralRouteRenovationKind Kind;
        public string FloorInstanceId;
        public string TargetId;
        public string SourceNodeId;
        public string DestinationNodeId;
        public TileCoordinate Anchor;
        public CardinalOrientation Orientation;
        public AuthoredRouteGeometry[] Connections = Array.Empty<AuthoredRouteGeometry>();
        public string ExpectedConnectionId;

        internal AuthoredRouteGeometry Connection => Connections?.Length == 1 ? Connections[0] : null;
        internal bool IsCorridorDisposition => Kind == StructuralRouteRenovationKind.ReassignCorridorContent ||
            Kind == StructuralRouteRenovationKind.ReturnCorridorContent || Kind == StructuralRouteRenovationKind.CancelCorridorDisposition;

        internal bool HasExactPayload
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FloorInstanceId) || Connections == null) return false;
                if (Kind == StructuralRouteRenovationKind.Connect || Kind == StructuralRouteRenovationKind.ConnectBranch)
                    return Connection?.Passage != null && !string.IsNullOrWhiteSpace(SourceNodeId) &&
                        (Kind == StructuralRouteRenovationKind.Connect
                            ? !string.IsNullOrWhiteSpace(DestinationNodeId)
                            : string.IsNullOrEmpty(DestinationNodeId) && Connection.Passage.Length > 0) &&
                        Anchor.Equals(default) && Orientation == default;
                if (IsCorridorDisposition)
                    return Connections.Length == 0 && !string.IsNullOrWhiteSpace(TargetId) &&
                        string.IsNullOrEmpty(SourceNodeId) && string.IsNullOrEmpty(ExpectedConnectionId) &&
                        Orientation == default && (Kind == StructuralRouteRenovationKind.ReassignCorridorContent
                            ? !string.IsNullOrWhiteSpace(DestinationNodeId)
                            : string.IsNullOrEmpty(DestinationNodeId) && Anchor.Equals(default));
                if (Connections.Length != 0 || !string.IsNullOrEmpty(SourceNodeId) ||
                    !string.IsNullOrEmpty(DestinationNodeId) || !string.IsNullOrEmpty(ExpectedConnectionId)) return false;
                if (Kind == StructuralRouteRenovationKind.MoveStructure)
                    return !string.IsNullOrWhiteSpace(TargetId) && Enum.IsDefined(typeof(CardinalOrientation), Orientation);
                if (!Anchor.Equals(default) || Orientation != default) return false;
                return Kind == StructuralRouteRenovationKind.Reset ? string.IsNullOrEmpty(TargetId) :
                    (Kind == StructuralRouteRenovationKind.Disconnect || Kind == StructuralRouteRenovationKind.Retire) &&
                    !string.IsNullOrWhiteSpace(TargetId);
            }
        }
    }

    internal sealed class StructuralRouteRenovationPreview
    {
        internal DetachedCanonicalSpatialSaveState Candidate;
        internal string ConnectionId;
        internal string Reason;
        internal int UsedFloorSpace;
        internal int FloorCapacity;
        internal bool IsValid => Candidate != null && Reason == null;
    }

    internal static class StructuralRouteRenovationService
    {
        internal static bool AllowsDuringRebuild(FloorLayoutValidationReason reason) =>
            reason == FloorLayoutValidationReason.UnreachableRoom ||
            reason == FloorLayoutValidationReason.RequiredRouteWithoutTerminal ||
            reason == FloorLayoutValidationReason.OptionalBranchSourceNotOnRequiredRoute ||
            reason == FloorLayoutValidationReason.DeadEndUnreachable;

        internal static StructuralRouteRenovationPreview Preview(DungeonDraftContext context,
            DetachedCanonicalSpatialSaveState baseline, DetachedCanonicalSpatialSaveState presented,
            StructuralRouteRenovation intent, string[] disconnectedRooms, IEnumerable<string> otherFloors)
        {
            if (intent?.HasExactPayload != true ||
                !CanonicalEditFloorTarget.TryResolve(presented, intent.FloorInstanceId, out var before) ||
                before.ActivationState != FloorActivationState.Active)
                return Fail(StructuralEditService.InvalidContextReason);

            var candidate = context.Copy(presented);
            if (candidate == null) return Fail(StructuralEditService.WorkloadReason);
            var floor = candidate.Floors.Single(value => value.FloorInstanceId == intent.FloorInstanceId);
            var canonicalFloor = baseline.Floors.Single(value => value.FloorInstanceId == intent.FloorInstanceId);
            var catalog = context.Production.Catalog;
            string connectionId = null;
            switch (intent.Kind)
            {
                case StructuralRouteRenovationKind.Reset:
                    Remove(floor, floor.Layout.Edges);
                    break;
                case StructuralRouteRenovationKind.Disconnect:
                case StructuralRouteRenovationKind.Retire:
                    var edge = floor.Layout.Edges.SingleOrDefault(value => value.EdgeId == intent.TargetId);
                    if (edge == null) return Fail(StructuralEditService.ConnectionUnavailableReason);
                    if (intent.Kind == StructuralRouteRenovationKind.Retire &&
                        !canonicalFloor.Layout.Edges.Any(value => value.EdgeId == edge.EdgeId &&
                            value.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor))
                        return Fail(StructuralRouteInsertionService.RetirementUnavailable);
                    Remove(floor, new[] { edge });
                    break;
                case StructuralRouteRenovationKind.MoveStructure:
                    if (!MoveStructure(floor, intent))
                        return Fail(StructuralEditService.TargetRoomNotFoundReason);
                    break;
                case StructuralRouteRenovationKind.Connect:
                    var reason = Connect(candidate, canonicalFloor, floor, intent, catalog, out connectionId);
                    if (reason != null) return Fail(reason);
                    break;
                case StructuralRouteRenovationKind.ConnectBranch:
                    var branchReason = ConnectBranch(candidate, canonicalFloor, floor, intent, catalog, out connectionId);
                    if (branchReason != null) return Fail(branchReason);
                    break;
                case StructuralRouteRenovationKind.ReassignCorridorContent:
                    if (!floor.Layout.Edges.Any(edge => edge.Classification == RouteClassification.Optional &&
                        edge.OptionalBranchId == intent.DestinationNodeId && edge.CorridorTiles.Contains(intent.Anchor)))
                        return Fail(CorridorContentMutationService.InvalidTargetReason);
                    break;
                case StructuralRouteRenovationKind.ReturnCorridorContent:
                case StructuralRouteRenovationKind.CancelCorridorDisposition:
                    break;
            }

            if (!IsLegalPartialRoute(floor.Layout))
                return Fail(StructuralEditService.RequiredRouteReason);
            var configured = catalog.Floors.Single(value => value.FloorDefinitionId == floor.FloorDefinitionId);
            var workload = new SpatialValidationWorkloadLimits(context.Limits.Canonical.Spatial.MaximumMaterializedTiles);
            var layout = FloorLayoutValidator.Validate(floor.Layout, configured, catalog.Rooms, catalog.Corridors,
                workload, floor.FixedStructures, catalog.FixedStructures, CanonicalEditFloorTarget.Mode(floor));
            var issues = layout.Issues.Where(issue => !AllowsDuringRebuild(issue.Reason)).ToArray();
            if (issues.Any(issue => issue.Reason == FloorLayoutValidationReason.CapacityExceeded))
                return Fail(StructuralEditService.CapacityReason);
            if (issues.Any(issue => issue.Reason == FloorLayoutValidationReason.StructureTileOutsideFloorBounds))
                return Fail(StructuralEditService.OutOfBoundsReason);
            if (issues.Any(issue => issue.Reason == FloorLayoutValidationReason.FootprintOverlap))
                return Fail(StructuralEditService.CorridorOverlapReason);
            var rebuildingFloors = new HashSet<string>(otherFloors, StringComparer.Ordinal) { intent.FloorInstanceId };
            if (issues.Length != 0 || !context.Validate(candidate, disconnectedRooms, rebuildingFloors))
                return Fail(StructuralEditService.LayoutInvalidReason);

            return new StructuralRouteRenovationPreview
            {
                Candidate = context.Copy(candidate), ConnectionId = connectionId,
                UsedFloorSpace = layout.Capacity.UsedFloorSpaceCapacity,
                FloorCapacity = layout.Capacity.FinalFloorSpaceCapacity
            };
        }

        private static void Remove(SavedSpatialFloor floor, IEnumerable<FloorRouteEdge> removed)
        {
            var edges = removed.ToArray();
            var removedIds = new HashSet<string>(edges.Select(edge => edge.EdgeId), StringComparer.Ordinal);
            var removedCaps = new HashSet<string>(edges.Where(edge => edge.Classification == RouteClassification.Optional)
                .Select(edge => edge.DestinationNodeId), StringComparer.Ordinal);
            floor.Layout.Edges = floor.Layout.Edges.Where(edge => !removedIds.Contains(edge.EdgeId)).ToArray();
            floor.Layout.Nodes = floor.Layout.Nodes.Where(node => !removedCaps.Contains(node.NodeId)).ToArray();
        }

        private static bool MoveStructure(SavedSpatialFloor floor, StructuralRouteRenovation intent)
        {
            var room = floor.Layout.Rooms.SingleOrDefault(value => value.RoomInstanceId == intent.TargetId);
            if (room != null)
            {
                if (room.Anchor.Equals(intent.Anchor) && room.Orientation == intent.Orientation) return false;
                var node = floor.Layout.Nodes.Single(value => value.RoomInstanceId == room.RoomInstanceId);
                Remove(floor, Incident(floor, node.NodeId));
                room.Anchor = intent.Anchor;
                room.Orientation = intent.Orientation;
                return true;
            }

            var structure = floor.FixedStructures.SingleOrDefault(value => value.FixedStructureInstanceId == intent.TargetId);
            if (structure == null || (structure.Anchor.Equals(intent.Anchor) && structure.Orientation == intent.Orientation))
                return false;
            var kind = structure.Kind == FixedSpatialStructureKind.Entrance ? FloorRouteNodeKind.Entrance : FloorRouteNodeKind.Completion;
            var fixedNode = floor.Layout.Nodes.Single(value => value.Kind == kind);
            Remove(floor, Incident(floor, fixedNode.NodeId));
            structure.Anchor = intent.Anchor;
            structure.Orientation = intent.Orientation;
            return true;
        }

        private static IEnumerable<FloorRouteEdge> Incident(SavedSpatialFloor floor, string nodeId) =>
            floor.Layout.Edges.Where(edge => edge.SourceNodeId == nodeId || edge.DestinationNodeId == nodeId);

        private static string Connect(DetachedCanonicalSpatialSaveState candidate,
            SavedSpatialFloor canonical, SavedSpatialFloor floor, StructuralRouteRenovation intent,
            SpatialContentCatalog catalog, out string edgeId)
        {
            edgeId = null;
            var source = floor.Layout.Nodes.SingleOrDefault(node => node.NodeId == intent.SourceNodeId);
            var destination = floor.Layout.Nodes.SingleOrDefault(node => node.NodeId == intent.DestinationNodeId);
            if (source == null || destination == null || source == destination ||
                (source.Kind != FloorRouteNodeKind.Entrance && source.Kind != FloorRouteNodeKind.Room) ||
                (destination.Kind != FloorRouteNodeKind.Room && destination.Kind != FloorRouteNodeKind.Completion))
                return StructuralEditService.RequiredRouteReason;
            var selected = floor.Layout.Edges.SingleOrDefault(edge => edge.EdgeId == intent.TargetId);
            if (!string.IsNullOrEmpty(intent.TargetId) &&
                (selected == null || selected.Classification != RouteClassification.Required ||
                selected.SourceNodeId != source.NodeId || selected.DestinationNodeId != destination.NodeId))
                return StructuralEditService.ConnectionUnavailableReason;
            if (selected != null)
            {
                edgeId = selected.EdgeId;
                Remove(floor, new[] { selected });
            }
            else
            {
                var prior = canonical.Layout.Edges.SingleOrDefault(edge =>
                    edge.Classification == RouteClassification.Required &&
                    edge.SourceNodeId == source.NodeId && edge.DestinationNodeId == destination.NodeId);
                if (prior != null && !floor.Layout.Edges.Any(edge => edge.EdgeId == prior.EdgeId))
                    edgeId = prior.EdgeId;
                else
                {
                    if (!NativeStructuralIdentity.TryAllocateFreshEdgeIdentity(candidate, floor.FloorInstanceId,
                        out edgeId, out long next, out string reason)) return reason;
                    candidate.LifecycleAndOwnership.Floors.Single(value =>
                        value.FloorInstanceId == floor.FloorInstanceId).NextNativeEdgeOrdinal = next;
                }
            }
            bool direct = intent.Connection.Passage.Length == 0;
            var proposed = new FloorRouteEdge
            {
                EdgeId = edgeId, FloorId = floor.FloorInstanceId,
                SourceNodeId = source.NodeId, DestinationNodeId = destination.NodeId,
                CorridorDefinitionId = direct ? string.Empty : OptionalBranchStructuralEditService.CorridorDefinitionId,
                ConnectionKind = direct ? FloorRouteConnectionKind.DirectDoorway : FloorRouteConnectionKind.PhysicalCorridor,
                Classification = RouteClassification.Required, OptionalBranchId = string.Empty,
                AuthoredGeometry = intent.Connection.Copy()
            };
            if (!proposed.AuthoredGeometry.TryResolve(floor, proposed, catalog, out _, out _, out _))
                return StructuralEditService.ConnectionUnavailableReason;
            floor.Layout.Edges = floor.Layout.Edges.Append(proposed).ToArray();
            return null;
        }

        private static string ConnectBranch(DetachedCanonicalSpatialSaveState candidate,
            SavedSpatialFloor canonical, SavedSpatialFloor floor, StructuralRouteRenovation intent,
            SpatialContentCatalog catalog, out string edgeId)
        {
            edgeId = null;
            var source = floor.Layout.Nodes.SingleOrDefault(node => node.NodeId == intent.SourceNodeId);
            if (source?.Kind != FloorRouteNodeKind.Room)
                return OptionalBranchStructuralEditService.InvalidOriginReason;
            var selected = floor.Layout.Edges.SingleOrDefault(edge => edge.EdgeId == intent.TargetId);
            if (!string.IsNullOrEmpty(intent.TargetId) &&
                (selected == null || selected.Classification != RouteClassification.Optional || selected.SourceNodeId != source.NodeId))
                return StructuralEditService.ConnectionUnavailableReason;

            var prior = selected ?? canonical.Layout.Edges.SingleOrDefault(edge =>
                edge.Classification == RouteClassification.Optional && edge.SourceNodeId == source.NodeId &&
                !floor.Layout.Edges.Any(current => current.EdgeId == edge.EdgeId));
            if (selected != null) Remove(floor, new[] { selected });
            if (prior != null) edgeId = prior.EdgeId;
            else
            {
                if (!NativeStructuralIdentity.TryAllocateFreshEdgeIdentity(candidate, floor.FloorInstanceId,
                    out edgeId, out long next, out string reason)) return reason;
                candidate.LifecycleAndOwnership.Floors.Single(value =>
                    value.FloorInstanceId == floor.FloorInstanceId).NextNativeEdgeOrdinal = next;
            }
            var cap = new FloorRouteNode
            {
                NodeId = prior?.DestinationNodeId ?? edgeId + ".dead-end",
                FloorId = floor.FloorInstanceId, Kind = FloorRouteNodeKind.DeadEnd
            };
            var proposed = new FloorRouteEdge
            {
                EdgeId = edgeId, FloorId = floor.FloorInstanceId, SourceNodeId = source.NodeId,
                DestinationNodeId = cap.NodeId, CorridorDefinitionId = OptionalBranchStructuralEditService.CorridorDefinitionId,
                ConnectionKind = FloorRouteConnectionKind.PhysicalCorridor, Classification = RouteClassification.Optional,
                OptionalBranchId = prior?.OptionalBranchId ?? edgeId + ".branch", AuthoredGeometry = intent.Connection.Copy()
            };
            floor.Layout.Nodes = floor.Layout.Nodes.Append(cap).ToArray();
            if (!proposed.AuthoredGeometry.TryResolve(floor, proposed, catalog, out _, out _, out _))
                return StructuralEditService.ConnectionUnavailableReason;
            floor.Layout.Edges = floor.Layout.Edges.Append(proposed).ToArray();
            return null;
        }

        private static bool IsLegalPartialRoute(FloorSpatialLayout layout)
        {
            var required = layout.Edges.Where(edge => edge.Classification == RouteClassification.Required).ToArray();
            if (required.GroupBy(edge => edge.SourceNodeId, StringComparer.Ordinal).Any(group => group.Count() > 1) ||
                required.GroupBy(edge => edge.DestinationNodeId, StringComparer.Ordinal).Any(group => group.Count() > 1))
                return false;

            var next = required.ToDictionary(edge => edge.SourceNodeId, edge => edge.DestinationNodeId, StringComparer.Ordinal);
            var destinations = new HashSet<string>(next.Values, StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (string head in next.Keys.Where(id => !destinations.Contains(id)))
                for (var node = head; next.TryGetValue(node, out var successor); node = successor)
                    if (!visited.Add(node)) return false;
            return visited.Count == required.Length;
        }

        private static StructuralRouteRenovationPreview Fail(string reason) =>
            new StructuralRouteRenovationPreview { Reason = reason };
    }

    public sealed partial class TransactionalDungeonDraft
    {
        private string[] RebuildingFloors => commands.Where(command => command.Kind == DungeonDraftCommandKind.RouteRenovation)
            .Select(command => command.RouteRenovation.FloorInstanceId).Distinct(StringComparer.Ordinal).ToArray();

        private bool HasIncompleteReconstruction => RebuildingFloors.Any(id =>
        {
            var layout = presented.Floors.Single(floor => floor.FloorInstanceId == id).Layout;
            return !RequiredFloorTraversal.TryResolve(layout, out var route) ||
                route.Count(node => node.Kind == FloorRouteNodeKind.Room) != layout.Rooms.Length ||
                route.Length - 1 != layout.Edges.Count(edge => edge.Classification == RouteClassification.Required);
        });

        internal bool HasRouteRenovation => RebuildingFloors.Length != 0;
        internal bool IsRebuildingFloor(string id) => RebuildingFloors.Contains(id);

        private IEnumerable<StructuralRouteRenovation> RenovationIntents(StructuralRouteRenovation prospective) =>
            commands.Select(command => command.RouteRenovation).Append(prospective).Where(intent => intent != null);

        internal StructuralRouteRenovation[] CorridorDispositions(StructuralRouteRenovation prospective = null) =>
            RenovationIntents(prospective).Where(intent => intent.IsCorridorDisposition)
            .GroupBy(intent => intent.TargetId, StringComparer.Ordinal).Select(group => group.Last())
            .Where(intent => intent.Kind != StructuralRouteRenovationKind.CancelCorridorDisposition)
            .OrderBy(intent => intent.TargetId, StringComparer.Ordinal).ToArray();

        internal AuthoredRouteGeometry ReconnectRenovation(StructuralRouteRenovation intent,
            AuthoredRouteGeometry geometry, TileCoordinate? waypoint, TileCoordinate? replacedTile, bool requireTurn,
            out string reason)
        {
            reason = StructuralEditService.ConnectionUnavailableReason;
            var floor = presented.Floors.SingleOrDefault(value => value.FloorInstanceId == intent.FloorInstanceId);
            return floor == null ? null : StructuralRouteInsertionService.Reconnect(context, floor,
                intent.SourceNodeId, intent.DestinationNodeId, geometry, new AuthoredRouteGeometry(),
                intent.TargetId, null, waypoint, replacedTile, requireTurn, out reason);
        }

        internal AuthoredRouteGeometry SuggestRenovation(StructuralRouteRenovation intent,
            Func<StructuralRouteRenovationPreview, double?> netCost)
        {
            var floor = presented.Floors.SingleOrDefault(value => value.FloorInstanceId == intent.FloorInstanceId);
            return floor == null ? null : StructuralRouteInsertionService.SuggestConnection(context, floor,
                intent.SourceNodeId, intent.DestinationNodeId, intent.TargetId, geometry =>
                {
                    var preview = PreviewRenovation(new StructuralRouteRenovation
                    {
                        Kind = intent.Kind, FloorInstanceId = intent.FloorInstanceId, TargetId = intent.TargetId,
                        SourceNodeId = intent.SourceNodeId, DestinationNodeId = intent.DestinationNodeId,
                        Connections = new[] { geometry }
                    });
                    return (preview.IsValid, preview.IsValid ? netCost(preview) : null);
                });
        }

        internal string[] ExplicitStructuralRetirements => EconomicRetirements();

        internal string[] EconomicRetirements(StructuralRouteRenovation prospective = null) =>
            RenovationIntents(prospective).Where(intent => intent.Kind == StructuralRouteRenovationKind.Retire)
            .Select(intent => intent.TargetId).Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal).ToArray();

        private IEnumerable<string> IndependentlyMovedRooms(DetachedCanonicalSpatialSaveState after,
            StructuralRouteRenovation prospective) => RenovationIntents(prospective)
            .Where(intent => intent.Kind == StructuralRouteRenovationKind.MoveStructure)
            .Select(intent => intent.TargetId).Distinct(StringComparer.Ordinal)
            .Where(id =>
            {
                var before = baseline.Floors.SelectMany(floor => floor.Layout.Rooms)
                    .SingleOrDefault(room => room.RoomInstanceId == id);
                var final = after.Floors.SelectMany(floor => floor.Layout.Rooms)
                    .SingleOrDefault(room => room.RoomInstanceId == id);
                return before != null && final != null &&
                    (!before.Anchor.Equals(final.Anchor) || before.Orientation != final.Orientation);
            }).OrderBy(id => id, StringComparer.Ordinal).ToArray();

        internal string[] FinalEconomicMovementTargets(DetachedCanonicalSpatialSaveState after,
            StructuralRouteRenovation prospective = null)
        {
            var legacy = NormalizedMovementTargets(after);
            if (legacy == null) return null;
            return legacy.Concat(IndependentlyMovedRooms(after, prospective)).Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal).ToArray();
        }

        internal StructuralRouteRenovationPreview PreviewRenovation(StructuralRouteRenovation intent)
        {
            if (intent?.Kind == StructuralRouteRenovationKind.CancelCorridorDisposition &&
                !CorridorDispositions().Any(previous => previous.TargetId == intent.TargetId &&
                    previous.FloorInstanceId == intent.FloorInstanceId))
                return new StructuralRouteRenovationPreview { Reason = CorridorRenovationConsequences.InvalidDispositionReason };
            return StructuralRouteRenovationService.Preview(context, baseline, presented, intent, DisconnectedRoomIds, RebuildingFloors);
        }

        internal bool AcceptRenovation(StructuralRouteRenovation intent)
        {
            if (IsClosed || Durability != DraftDurability.Acknowledged) return false;
            if (commands.Count >= context.Limits.Raw.MaximumArrayElements) { Reason = LimitReason; return false; }
            var command = new DungeonDraftCommand
            {
                Sequence = commands.Count + 1L, Kind = DungeonDraftCommandKind.RouteRenovation,
                RouteRenovations = new[] { intent }
            }.Copy();
            var preview = PreviewRenovation(command.RouteRenovation);
            if (!preview.IsValid) { Reason = preview.Reason; return false; }
            command.RouteRenovation.ExpectedConnectionId = preview.ConnectionId;
            presented = preview.Candidate;
            commands.Add(command);
            formatVersion = CurrentFormatVersion;
            ruleIdentity = context.RuleIdentity(formatVersion);
            Durability = DraftDurability.Pending;
            Reason = PendingReason;
            return true;
        }

        private bool ApplyRenovation(StructuralRouteRenovation intent)
        {
            var preview = PreviewRenovation(intent);
            if (!preview.IsValid || !string.Equals(preview.ConnectionId ?? string.Empty,
                intent.ExpectedConnectionId ?? string.Empty, StringComparison.Ordinal))
                return false;
            presented = preview.Candidate;
            return true;
        }
    }
}
