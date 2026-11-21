using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum FloorRouteNodeKind
    {
        Entrance = 1,
        Room = 2,
        Exit = 3,
        Descent = 4,
        Completion = 5,
        DeadEnd = 6
    }
    public enum RouteClassification { Required = 1, Optional = 2 }
    public enum FloorRouteConnectionKind { DirectDoorway = 1, PhysicalCorridor = 2 }

    [Serializable]
    public sealed class FloorRouteNode
    {
        public string NodeId;
        public string FloorId;
        public FloorRouteNodeKind Kind;
        public string RoomInstanceId;
    }

    [Serializable]
    public sealed class FloorRouteEdge : ISerializationCallbackReceiver
    {
        private enum FootprintPresence { UnknownOrLegacy = 0, Absent = 1, Present = 2 }

        public string EdgeId;
        public string CorridorDefinitionId;
        public string FloorId;
        public string SourceNodeId;
        public string DestinationNodeId;
        public ResolvedTileFootprint Footprint;
        public RouteClassification Classification;
        public string OptionalBranchId;
        public FloorRouteConnectionKind ConnectionKind;
        public AuthoredRouteGeometry AuthoredGeometry;

        // Occupancy consumers can enumerate either representation without owning another copy.
        internal TileCoordinate[] CorridorTiles => AuthoredGeometry == null ? Footprint?.OccupiedTiles : AuthoredGeometry.Passage;

        [SerializeField] private FootprintPresence footprintPresence;
        [SerializeField] private bool hasAuthoredGeometry;

        public void OnBeforeSerialize()
        {
            footprintPresence = Footprint == null ? FootprintPresence.Absent : FootprintPresence.Present;
            hasAuthoredGeometry = AuthoredGeometry != null;
        }

        public void OnAfterDeserialize()
        {
            if (footprintPresence == FootprintPresence.Absent) Footprint = null;
            if (!hasAuthoredGeometry) AuthoredGeometry = null;
        }
    }

    [Serializable]
    public sealed class AuthoredRouteGeometry
    {
        public string SourcePointId;
        public string DestinationPointId;
        public TileCoordinate[] Passage = Array.Empty<TileCoordinate>();

        internal AuthoredRouteGeometry Copy() => new AuthoredRouteGeometry
        {
            SourcePointId = SourcePointId,
            DestinationPointId = DestinationPointId,
            Passage = Passage == null ? null : (TileCoordinate[])Passage.Clone()
        };

        internal bool HasValidPassage(CorridorSpatialDefinition definition)
        {
            if (definition?.Category != CorridorSpatialCategory.Orthogonal || definition.Width != 1 ||
                Passage == null || Passage.Length == 0 ||
                Passage.Length < definition.MinimumLength || Passage.Length > definition.MaximumLength ||
                !IsContinuous(Passage)) return false;
            for (int i = 1; i < Passage.Length; i++)
                if (!AllowsAxis(definition, Passage[i - 1], Passage[i])) return false;
            return true;
        }

        internal static bool IsContinuous(TileCoordinate[] tiles)
        {
            var occupied = new HashSet<TileCoordinate>();
            for (int i = 0; i < tiles.Length; i++)
                if (!occupied.Add(tiles[i]) ||
                    (i > 0 && !TryFacing(tiles[i - 1], tiles[i], out _))) return false;
            return true;
        }

        internal bool TryResolve(SavedSpatialFloor floor, FloorRouteEdge edge, SpatialContentCatalog catalog,
            out RouteOpening source, out RouteOpening destination, out TileCoordinate[] path)
        {
            source = destination = default;
            path = null;
            if (Passage == null || edge.Footprint != null ||
                !TryOpening(floor, edge.SourceNodeId, SourcePointId, catalog, out source)) return false;
            var node = floor.Layout.Nodes.SingleOrDefault(n => n.NodeId == edge.DestinationNodeId);
            if (node == null) return false;
            bool cap = node.Kind == FloorRouteNodeKind.DeadEnd;
            if (cap)
            {
                if (DestinationPointId != string.Empty || edge.Classification != RouteClassification.Optional)
                    return false;
            }
            else
            {
                if (!TryOpening(floor, edge.DestinationNodeId, DestinationPointId, catalog, out destination))
                    return false;
                string sourceSocket = source.SocketTypeId, destinationSocket = destination.SocketTypeId;
                if (!catalog.SocketTypes.Any(s => s.SocketTypeId == sourceSocket &&
                    s.CompatibleSocketTypeIds.Contains(destinationSocket))) return false;
            }

            if (edge.ConnectionKind == FloorRouteConnectionKind.DirectDoorway)
            {
                if (cap || Passage.Length != 0 || !source.OpensToward(destination.World) ||
                    !destination.OpensToward(source.World)) return false;
                path = new[] { source.World, destination.World };
                return true;
            }
            if (edge.ConnectionKind != FloorRouteConnectionKind.PhysicalCorridor) return false;
            var definition = catalog.Corridors.SingleOrDefault(c => c.CorridorDefinitionId == edge.CorridorDefinitionId);
            if (!HasValidPassage(definition) || !source.OpensToward(Passage[0]) ||
                !AllowsAxis(definition, source.World, Passage[0]) ||
                !definition.CompatibleSocketTypeIds.Contains(source.SocketTypeId)) return false;
            TileCoordinate end = Passage[Passage.Length - 1];
            if (!cap && (!destination.OpensToward(end) || !AllowsAxis(definition, end, destination.World) ||
                !definition.CompatibleSocketTypeIds.Contains(destination.SocketTypeId))) return false;

            path = new TileCoordinate[Passage.Length + (cap ? 1 : 2)];
            path[0] = source.World;
            Passage.CopyTo(path, 1);
            if (!cap) path[path.Length - 1] = destination.World;
            return IsContinuous(path);
        }

        internal static RouteOpening[] Openings(SavedSpatialFloor floor, string nodeId, SpatialContentCatalog catalog)
        {
            var node = floor.Layout.Nodes.SingleOrDefault(n => n.NodeId == nodeId);
            if (node == null || !StructuralRenovationService.TryEndpoint(floor, node, catalog, out var endpoint))
                return Array.Empty<RouteOpening>();
            return endpoint.Points.OrderBy(p => p.ConnectionPointId, StringComparer.Ordinal)
                .Select(p => new RouteOpening(p.ConnectionPointId, p.Offset,
                    StructuralRenovationService.World(p, endpoint),
                    StructuralEditService.Rotate(p.Facing, endpoint.Orientation), p.SocketTypeId)).ToArray();
        }

        private static bool TryOpening(SavedSpatialFloor floor, string nodeId, string pointId, SpatialContentCatalog catalog,
            out RouteOpening opening)
        {
            opening = default;
            var matches = Openings(floor, nodeId, catalog).Where(p => p.Id == pointId).ToArray();
            if (matches.Length != 1) return false;
            opening = matches[0];
            return true;
        }

        private static bool AllowsAxis(CorridorSpatialDefinition definition, TileCoordinate from, TileCoordinate to) =>
            TryFacing(from, to, out var facing) && (definition.AllowedOrientations ?? Array.Empty<CardinalOrientation>())
                .Contains((CardinalOrientation)((int)facing % 2));

        internal static bool TryFacing(TileCoordinate from, TileCoordinate to, out CardinalOrientation facing)
        {
            long dx = (long)to.X - from.X, dy = (long)to.Y - from.Y;
            facing = default;
            if (Math.Abs(dx) + Math.Abs(dy) != 1) return false;
            if (dy < 0) facing = CardinalOrientation.OneEighty;
            else if (dx > 0) facing = CardinalOrientation.Ninety;
            else if (dx < 0) facing = CardinalOrientation.TwoSeventy;
            return true;
        }
    }

    internal readonly struct RouteOpening
    {
        internal readonly string Id;
        internal readonly TileCoordinate Local;
        internal readonly TileCoordinate World;
        internal readonly CardinalOrientation Facing;
        internal readonly string SocketTypeId;

        internal RouteOpening(string id, TileCoordinate local, TileCoordinate world, CardinalOrientation facing, string socketTypeId)
        { Id = id; Local = local; World = world; Facing = facing; SocketTypeId = socketTypeId; }

        internal bool OpensToward(TileCoordinate cell) =>
            AuthoredRouteGeometry.TryFacing(World, cell, out var facing) && facing == Facing;
    }
}
