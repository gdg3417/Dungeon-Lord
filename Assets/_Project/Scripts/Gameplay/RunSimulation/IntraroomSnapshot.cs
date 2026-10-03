using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    // Derived, privately serialized by RunnableFloorSnapshot. Never a writable geometry/save owner.
    [Serializable]
    public sealed class IntraroomSnapshot
    {
        public string FloorInstanceId;
        public string RoomInstanceId;
        public string RoomDefinitionId;
        public TileCoordinate Ingress;
        public TileCoordinate Egress;
        public TileCoordinate[] TraversableTiles;
        public IntraroomAssignment[] Assignments;
        public int MaximumMaterializedTiles;

        internal static IntraroomSnapshot Capture(SavedSpatialFloor floor, string nodeId,
            SpatialContentCatalog catalog, RoomContentSpatialOccupancySnapshot occupancy,
            CanonicalSpatialSaveWorkloadLimits limits)
        {
            if (!RequiredFloorTraversal.TryResolve(floor.Layout, out FloorRouteNode[] route)) Invalid();
            int index = Array.FindIndex(route, n => n.NodeId == nodeId);
            if (index <= 0 || index >= route.Length - 1) Invalid();
            var room = floor.Layout.Rooms.Single(r => r.RoomInstanceId == route[index].RoomInstanceId);
            var definition = catalog.Rooms.Single(d => d.RoomDefinitionId == room.RoomDefinitionId);
            var incoming = floor.Layout.Edges.Single(e => e.Classification == RouteClassification.Required &&
                e.SourceNodeId == route[index - 1].NodeId && e.DestinationNodeId == nodeId);
            var outgoing = floor.Layout.Edges.Single(e => e.Classification == RouteClassification.Required &&
                e.SourceNodeId == nodeId && e.DestinationNodeId == route[index + 1].NodeId);
            if (!StructuralRenovationService.TryResolveSavedConnection(floor, incoming, catalog,
                    out _, out TileCoordinate ingress)) Invalid();
            if (!StructuralRenovationService.TryResolveSavedConnection(floor, outgoing, catalog,
                    out TileCoordinate egress, out _)) Invalid();
            var tiles = DeriveTraversableTiles(definition, limits.MaximumMaterializedTiles);
            var legal = new HashSet<TileCoordinate>(tiles);
            if (!legal.Contains(ingress) || !legal.Contains(egress)) Invalid();
            var assignments = new List<IntraroomAssignment>();
            long materialized = tiles.LongLength;
            foreach (var a in floor.RoomContents.Assignments.Where(a => a.RoomInstanceId == room.RoomInstanceId)
                .OrderBy(a => a.Sequence).ThenBy(a => a.AssignmentId, StringComparer.Ordinal))
            {
                if (!RoomContentSpatialOccupancyAuthority.TryResolve(occupancy, a.CategoryId, a.OptionId,
                    out RoomContentSpatialOccupancyRecord record)) Invalid();
                materialized += record.OccupiedTileOffsets.LongLength;
                if (materialized > limits.MaximumMaterializedTiles) Workload();
                var footprint = record.OccupiedTileOffsets.Select(offset => new TileCoordinate(
                    checked(a.RoomLocalPosition.X + offset.X), checked(a.RoomLocalPosition.Y + offset.Y)))
                    .OrderBy(t => t).ToArray();
                if (footprint.Length == 0 || footprint.Any(t => !legal.Contains(t)) ||
                    !legal.Contains(a.RoomLocalPosition)) Invalid();
                assignments.Add(new IntraroomAssignment { AssignmentId = a.AssignmentId,
                    OccupiedTiles = footprint });
            }
            var result = new IntraroomSnapshot { FloorInstanceId = floor.FloorInstanceId,
                RoomInstanceId = room.RoomInstanceId, RoomDefinitionId = room.RoomDefinitionId,
                Ingress = ingress, Egress = egress, TraversableTiles = tiles,
                Assignments = assignments.ToArray(), MaximumMaterializedTiles = limits.MaximumMaterializedTiles };
            // Fail before party formation or any outcome/settlement publication.
            if (!new DeterministicRoomPaths(tiles, limits.MaximumMaterializedTiles)
                .TryShortest(ingress, new[] { egress }, out _)) Invalid();
            return result;
        }

        public static TileCoordinate[] DeriveTraversableTiles(RoomSpatialDefinition definition, int maximumTiles)
        {
            var shape = definition?.GrossFootprint;
            if (shape == null || shape.Width <= 0 || shape.Height <= 0 || definition.ReservedTileOffsets == null)
                Invalid();
            if (maximumTiles <= 0 || (long)shape.Width * shape.Height > maximumTiles ||
                definition.ReservedTileOffsets.LongLength > maximumTiles) Workload();
            var reserved = new HashSet<TileCoordinate>(definition.ReservedTileOffsets);
            if (reserved.Count != definition.ReservedTileOffsets.Length || reserved.Any(t =>
                t.X < 0 || t.Y < 0 || t.X >= shape.Width || t.Y >= shape.Height)) Invalid();
            var tiles = new List<TileCoordinate>();
            for (int x = 0; x < shape.Width; x++)
                for (int y = 0; y < shape.Height; y++)
                {
                    var tile = new TileCoordinate(x, y);
                    if (!reserved.Contains(tile)) tiles.Add(tile);
                }
            return tiles.ToArray();
        }
        internal static void Invalid() => throw new ArgumentException("run.spatial.invalid_geometry");
        internal static void Workload() => throw new ArgumentException("run.spatial.workload_exceeded");
    }

    [Serializable]
    public sealed class IntraroomAssignment
    {
        public string AssignmentId;
        public TileCoordinate[] OccupiedTiles;
    }

    // Four-neighbour tile topology. No timing inputs; arbitrary authored legal-cell providers can
    // use this API. BFS visits neighbours in TileCoordinate.CompareTo order, never hash order.
    public sealed class DeterministicRoomPaths
    {
        private readonly TileCoordinate[] tiles;
        private readonly Dictionary<TileCoordinate, int> index;
        private readonly int[][] neighbours;
        private readonly int[] queue, parent, distance;

        public DeterministicRoomPaths(IEnumerable<TileCoordinate> cells, int maximumTiles)
        {
            if (cells == null || maximumTiles <= 0) IntraroomSnapshot.Invalid();
            // Bound enumeration before sorting/allocation, including arbitrary future providers.
            var bounded = new List<TileCoordinate>();
            foreach (var cell in cells)
            {
                if (bounded.Count >= maximumTiles) IntraroomSnapshot.Workload();
                bounded.Add(cell);
            }
            tiles = bounded.OrderBy(t => t).ToArray();
            index = new Dictionary<TileCoordinate, int>();
            for (int i = 0; i < tiles.Length; i++)
            {
                if (index.ContainsKey(tiles[i])) IntraroomSnapshot.Invalid();
                index.Add(tiles[i], i);
            }
            neighbours = new int[tiles.Length][];
            for (int i = 0; i < tiles.Length; i++)
            {
                var adjacent = new List<int>();
                var t = tiles[i];
                AddNeighbour(adjacent, (long)t.X - 1, t.Y);
                AddNeighbour(adjacent, t.X, (long)t.Y - 1);
                AddNeighbour(adjacent, t.X, (long)t.Y + 1);
                AddNeighbour(adjacent, (long)t.X + 1, t.Y);
                neighbours[i] = adjacent.ToArray();
            }
            queue = new int[tiles.Length]; parent = new int[tiles.Length]; distance = new int[tiles.Length];
        }

        private void AddNeighbour(List<int> target, long x, long y)
        {
            if (x >= int.MinValue && x <= int.MaxValue && y >= int.MinValue && y <= int.MaxValue &&
                index.TryGetValue(new TileCoordinate((int)x, (int)y), out int i)) target.Add(i);
        }

        internal bool Search(TileCoordinate start)
        {
            for (int i = 0; i < distance.Length; i++) { distance[i] = -1; parent[i] = -1; }
            if (!index.TryGetValue(start, out int first)) return false;
            int head = 0, tail = 0; queue[tail++] = first; distance[first] = 0;
            while (head < tail)
            {
                int current = queue[head++];
                foreach (int next in neighbours[current])
                {
                    if (distance[next] >= 0) continue;
                    distance[next] = distance[current] + 1; parent[next] = current;
                    queue[tail++] = next;
                }
            }
            return true;
        }
        internal int Distance(TileCoordinate tile) => index.TryGetValue(tile, out int i) ? distance[i] : -1;
        internal TileCoordinate[] PathTo(TileCoordinate end)
        {
            int last = index[end];
            if (distance[last] < 0) return Array.Empty<TileCoordinate>();
            var path = new TileCoordinate[distance[last] + 1];
            for (int i = path.Length - 1; i >= 0; i--) { path[i] = tiles[last]; last = parent[last]; }
            return path;
        }
        public bool TryShortest(TileCoordinate start, IEnumerable<TileCoordinate> destinations,
            out TileCoordinate[] path)
        {
            path = Array.Empty<TileCoordinate>();
            if (!Search(start)) return false;
            bool found = false; TileCoordinate target = default; int best = int.MaxValue;
            foreach (var tile in destinations)
            {
                int d = Distance(tile);
                if (d < 0 || d > best || (d == best && found && tile.CompareTo(target) >= 0)) continue;
                found = true; best = d; target = tile;
            }
            if (found) path = PathTo(target);
            return found;
        }
    }
}
