using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // Shared by eligibility and runnable projection. Completion is terminal even if authored
    // required edges leave it. Ambiguity or a cycle before Completion is not runnable.
    public static class RequiredFloorTraversal
    {
        public static bool TryResolve(FloorSpatialLayout layout, out FloorRouteNode[] route)
        {
            route = Array.Empty<FloorRouteNode>();
            if (layout?.Nodes == null || layout.Edges == null || layout.Nodes.Any(n => n == null ||
                string.IsNullOrWhiteSpace(n.NodeId)) || layout.Nodes.Select(n => n.NodeId).Distinct(StringComparer.Ordinal).Count() != layout.Nodes.Length)
                return false;
            var nodes = layout.Nodes.ToDictionary(n => n.NodeId, StringComparer.Ordinal);
            var entrance = layout.Nodes.Where(n => n.Kind == FloorRouteNodeKind.Entrance).ToArray();
            if (entrance.Length != 1 || layout.Nodes.Count(n => n.Kind == FloorRouteNodeKind.Completion) != 1) return false;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<FloorRouteNode>();
            var current = entrance[0];
            while (visited.Add(current.NodeId))
            {
                result.Add(current);
                if (current.Kind == FloorRouteNodeKind.Completion) { route = result.ToArray(); return true; }
                var edges = layout.Edges.Where(e => e != null && e.Classification == RouteClassification.Required &&
                    e.SourceNodeId == current.NodeId).ToArray();
                if (edges.Length != 1 || !nodes.TryGetValue(edges[0].DestinationNodeId ?? string.Empty, out current) ||
                    current.Kind == FloorRouteNodeKind.DeadEnd) return false;
            }
            return false;
        }
    }
}
