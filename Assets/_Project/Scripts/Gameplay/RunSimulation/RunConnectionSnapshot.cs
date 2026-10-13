using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    [Serializable] internal sealed class RunConnectionPath
    { public string Id; public string RoomId; public TileCoordinate[] Path; }
    [Serializable] internal sealed class RunConnectionSnapshot
    {
        public RunConnectionPath[] Required;
        public RunConnectionPath[] BranchApproach;

        internal static RunConnectionSnapshot Capture(SavedSpatialFloor floor, PhaseFiveBRoutePlan plan,
            SpatialContentCatalog catalog, int maximumTiles)
        {
            if (!RequiredFloorTraversal.TryResolve(floor.Layout, out var nodes)) IntraroomSnapshot.Invalid();
            var required = new List<RunConnectionPath>();
            for (int i = 1; i < nodes.Length; i++)
            {
                var edge = floor.Layout.Edges.Single(e => e.Classification == RouteClassification.Required &&
                    e.SourceNodeId == nodes[i - 1].NodeId && e.DestinationNodeId == nodes[i].NodeId);
                if (!StructuralRenovationService.TryResolveSavedConnection(floor, edge, catalog,
                    out var source, out var destination)) IntraroomSnapshot.Invalid();
                source = World(floor, nodes[i - 1], catalog, source);
                destination = World(floor, nodes[i], catalog, destination);
                var cells = (edge.Footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>()).Concat(new[] { source, destination }).Distinct().ToArray();
                if (!new DeterministicRoomPaths(cells, maximumTiles).TryShortest(source, new[] { destination }, out var path))
                    IntraroomSnapshot.Invalid();
                required.Add(new RunConnectionPath { Id = edge.EdgeId, RoomId = nodes[i].RoomInstanceId, Path = path });
            }
            var approaches = new List<RunConnectionPath>();
            foreach (var fork in plan.Forks)
            {
                var edge = floor.Layout.Edges.Single(e => e.EdgeId == fork.EdgeId);
                if (!OptionalBranchGeometry.TryResolveSourceTile(floor, edge, catalog, out var sourceWorld)) IntraroomSnapshot.Invalid();
                var room = floor.Layout.Rooms.Single(r => r.RoomInstanceId == fork.RoomInstanceId);
                var definition = catalog.Rooms.Single(d => d.RoomDefinitionId == room.RoomDefinitionId);
                var spatial = plan.RequiredRooms.Single(r => r.Room.RoomInstanceId == room.RoomInstanceId).Room.Spatial;
                var sources = definition.ConnectionPoints.Select(p => p.Offset).Where(t => {
                    var offset = StructuralEditService.TransformConnectionPointOffset(t, room.Orientation, definition.GrossFootprint);
                    var world = new TileCoordinate(checked(room.Anchor.X + offset.X), checked(room.Anchor.Y + offset.Y));
                    return Math.Abs((long)world.X - sourceWorld.X) + Math.Abs((long)world.Y - sourceWorld.Y) == 1;
                }).ToArray();
                if (sources.Length != 1) IntraroomSnapshot.Invalid();
                var sourceLocal = sources[0];
                if (!new DeterministicRoomPaths(spatial.TraversableTiles, maximumTiles).TryShortest(spatial.Egress,
                    new[] { sourceLocal }, out var path)) IntraroomSnapshot.Invalid();
                // Approach remains room-local; corridor positions are floor-global. Both use exact
                // legal geometry. No revisited content effects or additional gameplay decision.
                approaches.Add(new RunConnectionPath { Id = fork.OptionalBranchId, RoomId = room.RoomInstanceId, Path = path });
            }
            return new RunConnectionSnapshot { Required = required.ToArray(), BranchApproach = approaches.ToArray() };
        }

        private static TileCoordinate World(SavedSpatialFloor floor, FloorRouteNode node, SpatialContentCatalog catalog, TileCoordinate local)
        {
            RectangularFootprintDefinition shape; TileCoordinate anchor; CardinalOrientation orientation;
            if (node.Kind == FloorRouteNodeKind.Room)
            {
                var room = floor.Layout.Rooms.Single(r => r.RoomInstanceId == node.RoomInstanceId);
                shape = catalog.Rooms.Single(d => d.RoomDefinitionId == room.RoomDefinitionId).GrossFootprint;
                anchor = room.Anchor; orientation = room.Orientation;
            }
            else
            {
                var kind = node.Kind == FloorRouteNodeKind.Entrance ? FixedSpatialStructureKind.Entrance : FixedSpatialStructureKind.CompletionTerminal;
                var fixedStructure = floor.FixedStructures.Single(f => f.Kind == kind);
                shape = catalog.FixedStructures.Single(d => d.StructureDefinitionId == fixedStructure.FixedStructureDefinitionId).GrossFootprint;
                anchor = fixedStructure.Anchor; orientation = fixedStructure.Orientation;
            }
            // Saved structural connections own socket rotation. Reuse their transform exactly.
            var offset = StructuralEditService.TransformConnectionPointOffset(local, orientation, shape);
            return new TileCoordinate(checked(anchor.X + offset.X), checked(anchor.Y + offset.Y));
        }
    }
}
