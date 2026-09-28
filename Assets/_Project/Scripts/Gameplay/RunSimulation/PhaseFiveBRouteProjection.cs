using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public sealed class PhaseFiveBRequiredRoom
    {
        public MvpOrderedRouteRoom Room { get; internal set; }
        public string FloorInstanceId { get; internal set; }
        public string NodeId { get; internal set; }
    }

    public sealed class PhaseFiveBRoutePlan
    {
        public PhaseFiveBRequiredRoom[] RequiredRooms { get; internal set; }
        public PhaseFiveBFork[] Forks { get; internal set; }
    }

    public sealed class PhaseFiveBFork
    {
        public int FloorIndex { get; internal set; }
        public int RoomIndex { get; internal set; }
        public string FloorInstanceId { get; internal set; }
        public string RoomInstanceId { get; internal set; }
        public string OriginNodeId { get; internal set; }
        public string OptionalBranchId { get; internal set; }
        public string EdgeId { get; internal set; }
        public string DeadEndNodeId { get; internal set; }
        public string Fingerprint { get; internal set; }
        public TileCoordinate[] Tiles { get; internal set; }
        public CorridorContentAssignment[] Assignments { get; internal set; }
        public BranchKnowledgeRecord Knowledge { get; internal set; }
        public bool Applicable { get; internal set; }
        public double RemainingRequiredDanger { get; internal set; }
        public PhaseFiveBRequiredRoom[] RequiredSuffix { get; internal set; }
    }

    // Read-only derived view. Required-route order still comes exclusively from CanonicalMvpRouteProjection.
    public static class PhaseFiveBRouteProjection
    {
        public const string InvalidRoute = "branch.run.invalid_route";
        public static PhaseFiveBRoutePlan Resolve(DetachedCompleteSaveValidationResult owned,
            SaveData runtime, ProductionSpatialContentSnapshot production, RunSimulationConfig config)
        {
            if (owned?.CurrentTargetValidated != true || !owned.IsValid || production == null) throw new ArgumentException(InvalidRoute);
            var projection = CanonicalMvpRouteProjection.InspectWithProductionContent(runtime, production);
            if (projection.AuthorityState != CanonicalMvpRuntimeAuthorityState.ValidatedCanonical) throw new ArgumentException(InvalidRoute);
            var required = projection.Rooms.Select(room => {
                var floor = owned.State.Floors.Single(f => f.FloorIndex == room.FloorIndex);
                var node = floor.Layout.Nodes.Single(n => n.Kind == FloorRouteNodeKind.Room && n.RoomInstanceId == room.RoomInstanceId);
                return new PhaseFiveBRequiredRoom { Room = room, FloorInstanceId = floor.FloorInstanceId, NodeId = node.NodeId };
            }).ToArray();
            var result = new System.Collections.Generic.List<PhaseFiveBFork>();
            foreach (var floor in owned.State.Floors.OrderBy(f => f.FloorIndex).ThenBy(f => f.FloorInstanceId, StringComparer.Ordinal))
            {
                var edges = floor.Layout.Edges.Where(e => e.Classification == RouteClassification.Optional)
                    .OrderBy(e => e.OptionalBranchId, StringComparer.Ordinal).ToArray();
                if (edges.Length > 1) throw new ArgumentException(InvalidRoute);
                foreach (var edge in edges)
                {
                    var origin = floor.Layout.Nodes.Single(n => n.NodeId == edge.SourceNodeId);
                    var destination = floor.Layout.Nodes.Single(n => n.NodeId == edge.DestinationNodeId);
                    var room = projection.Rooms.SingleOrDefault(r => r.FloorIndex == floor.FloorIndex && r.RoomInstanceId == origin.RoomInstanceId);
                    if (origin.Kind != FloorRouteNodeKind.Room || room == null || destination.Kind != FloorRouteNodeKind.DeadEnd ||
                        edge.CorridorDefinitionId != OptionalBranchStructuralEditService.CorridorDefinitionId ||
                        !OptionalBranchGeometry.TryResolveSourceTile(floor, edge, production.Catalog, out TileCoordinate source) ||
                        !BranchTopologyFingerprint.TryCompute(owned.State, floor.FloorInstanceId, edge.OptionalBranchId, out string fingerprint))
                        throw new ArgumentException(InvalidRoute);
                    var assignments = owned.CorridorContent.Assignments.Where(a => a.FloorInstanceId == floor.FloorInstanceId &&
                        a.OptionalBranchId == edge.OptionalBranchId && a.EdgeId == edge.EdgeId).ToArray();
                    if (assignments.Any(a => a.CategoryId != MvpDungeonPlacementIds.TrapCategoryId && a.CategoryId != MvpDungeonPlacementIds.LootNodeCategoryId))
                        throw new ArgumentException(InvalidRoute);
                    var knowledge = owned.BranchKnowledge.Records.SingleOrDefault(k => k.FloorInstanceId == floor.FloorInstanceId && k.OptionalBranchId == edge.OptionalBranchId);
                    int requiredIndex = Array.FindIndex(required, r => r.FloorInstanceId == floor.FloorInstanceId && r.NodeId == origin.NodeId);
                    if (requiredIndex < 0) throw new ArgumentException(InvalidRoute);
                    PhaseFiveBRequiredRoom[] suffix = required.Skip(requiredIndex + 1).ToArray();
                    result.Add(new PhaseFiveBFork { FloorIndex = floor.FloorIndex, RoomIndex = room.RoomIndex,
                        FloorInstanceId = floor.FloorInstanceId, RoomInstanceId = room.RoomInstanceId, OriginNodeId = origin.NodeId,
                        OptionalBranchId = edge.OptionalBranchId, EdgeId = edge.EdgeId, DeadEndNodeId = destination.NodeId,
                        Fingerprint = fingerprint, Tiles = edge.Footprint.OccupiedTiles.OrderBy(t => Distance(source, t)).ToArray(),
                        Assignments = Order(assignments, source), Knowledge = knowledge,
                        Applicable = BranchTopologyFingerprint.IsApplicable(knowledge, owned.State),
                        RequiredSuffix = suffix,
                        RemainingRequiredDanger = suffix.Sum(r =>
                            MvpPlacementEffectsResolver.ResolvePlacements(r.Room.ToOrderedPlacements(), config).Danger) });
                }
            }
            return new PhaseFiveBRoutePlan { RequiredRooms = required, Forks = result.ToArray() };
        }
        public static CorridorContentAssignment[] Order(CorridorContentAssignment[] values, TileCoordinate source) =>
            values.OrderBy(a => Distance(source, a.Tile)).ThenBy(a => a.Sequence).ThenBy(a => a.AssignmentId, StringComparer.Ordinal).ToArray();
        private static long Distance(TileCoordinate a, TileCoordinate b) => Math.Abs((long)a.X - b.X) + Math.Abs((long)a.Y - b.Y);
    }
}
