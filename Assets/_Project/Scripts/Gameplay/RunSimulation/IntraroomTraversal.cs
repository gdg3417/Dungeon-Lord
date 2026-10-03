using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public enum RunSpatialEventKind
    {
        RoomEntered, AdventurerTraversal, MonsterEngaged, TrapTriggered, LootReached,
        RoomEgressReached, MonsterUnreachable, LootUnreachable
    }

    // All coordinates are canonical base-local. Path is actual traversed evidence; monster
    // movement is attached only when its encounter executes. No duplicate damage owner.
    public sealed class RunSpatialEvent
    {
        public string FloorInstanceId { get; internal set; }
        public int FloorIndex { get; internal set; }
        public string RoomInstanceId { get; internal set; }
        public int Ordinal { get; internal set; }
        public RunSpatialEventKind Kind { get; internal set; }
        public int PathStep { get; internal set; }
        public string AssignmentId { get; internal set; }
        public string CategoryId { get; internal set; }
        public string OptionId { get; internal set; }
        public TileCoordinate Position { get; internal set; }
        public TileCoordinate ConfiguredStart { get; internal set; }
        public TileCoordinate[] Path { get; internal set; }
        public int EncounterOrdinal { get; internal set; } = -1;
    }

    internal sealed class ScheduledSpatialInteraction
    {
        internal RunRoomAssignment Assignment;
        internal int Step;
        internal RunSpatialEventKind Kind;
        internal TileCoordinate[] Movement;
        internal bool Reached => Kind == RunSpatialEventKind.MonsterEngaged ||
            Kind == RunSpatialEventKind.TrapTriggered || Kind == RunSpatialEventKind.LootReached;
    }

    internal sealed class IntraroomTraversal
    {
        internal TileCoordinate[] Path;
        internal ScheduledSpatialInteraction[] Interactions;

        internal static IntraroomTraversal Plan(MvpOrderedRouteRoom room)
        {
            var spatial = room.Spatial;
            if (spatial == null || spatial.Assignments == null) IntraroomSnapshot.Invalid();
            var paths = new DeterministicRoomPaths(spatial.TraversableTiles, spatial.MaximumMaterializedTiles);
            var assignments = room.OrderedAssignments();
            if (assignments.Length != spatial.Assignments.Length ||
                spatial.Assignments.Select(a => a.AssignmentId).Distinct(StringComparer.Ordinal).Count() != assignments.Length)
                IntraroomSnapshot.Invalid();
            var footprints = spatial.Assignments.ToDictionary(a => a.AssignmentId, a => a.OccupiedTiles,
                StringComparer.Ordinal);
            foreach (var a in assignments)
                if (!footprints.TryGetValue(a.AssignmentId, out var cells) || cells == null || cells.Length == 0 ||
                    cells.Any(t => !spatial.TraversableTiles.Contains(t))) IntraroomSnapshot.Invalid();
            var path = new List<TileCoordinate> { spatial.Ingress };
            var events = new List<ScheduledSpatialInteraction>();
            foreach (var a in assignments.Where(a => a.CategoryId == MvpDungeonPlacementIds.LootNodeCategoryId)
                .OrderBy(a => a.Sequence).ThenBy(a => a.AssignmentId, StringComparer.Ordinal))
            {
                bool reached = paths.TryShortest(path[path.Count - 1], footprints[a.AssignmentId], out var segment);
                if (reached) path.AddRange(segment.Skip(1));
                events.Add(new ScheduledSpatialInteraction { Assignment = a, Step = path.Count - 1,
                    Kind = reached ? RunSpatialEventKind.LootReached : RunSpatialEventKind.LootUnreachable });
            }
            if (!paths.TryShortest(path[path.Count - 1], new[] { spatial.Egress }, out var exit))
                IntraroomSnapshot.Invalid();
            path.AddRange(exit.Skip(1));
            // One BFS per monster, then scan the planned path for minimum distance / earliest step.
            // Duplicate route cells keep their earliest step. No per-step path searches/allocations.
            foreach (var a in assignments.Where(a => a.CategoryId != MvpDungeonPlacementIds.LootNodeCategoryId))
            {
                if (a.CategoryId == MvpDungeonPlacementIds.TrapCategoryId)
                {
                    var occupied = new HashSet<TileCoordinate>(footprints[a.AssignmentId]);
                    int step = path.FindIndex(t => occupied.Contains(t));
                    if (step >= 0) events.Add(new ScheduledSpatialInteraction { Assignment = a, Step = step,
                        Kind = RunSpatialEventKind.TrapTriggered });
                    continue;
                }
                paths.Search(a.RoomLocalPosition);
                int best = int.MaxValue, engagement = -1;
                for (int step = 0; step < path.Count; step++)
                {
                    int d = paths.Distance(path[step]);
                    if (d >= 0 && d < best) { best = d; engagement = step; }
                }
                events.Add(new ScheduledSpatialInteraction { Assignment = a, Step = Math.Max(0, engagement),
                    Kind = engagement < 0 ? RunSpatialEventKind.MonsterUnreachable : RunSpatialEventKind.MonsterEngaged,
                    Movement = engagement < 0 ? new[] { a.RoomLocalPosition } : paths.PathTo(path[engagement]) });
            }
            return new IntraroomTraversal { Path = path.ToArray(), Interactions = events.OrderBy(e => e.Step)
                .ThenBy(e => CategoryRank(e.Assignment.CategoryId)).ThenBy(e => e.Assignment.Sequence)
                .ThenBy(e => e.Assignment.AssignmentId, StringComparer.Ordinal).ToArray() };
        }
        private static int CategoryRank(string category) => category == MvpDungeonPlacementIds.MonsterCategoryId ? 0 :
            category == MvpDungeonPlacementIds.TrapCategoryId ? 1 : 2;
    }
}
