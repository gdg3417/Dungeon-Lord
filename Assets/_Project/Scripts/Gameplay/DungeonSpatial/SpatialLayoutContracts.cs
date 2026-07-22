using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    [Serializable]
    public sealed class FloorSpatialConfiguration
    {
        public string FloorDefinitionId;
        public int FloorIndex;
        public RectangularFloorBounds Bounds;
        public int FinalFloorSpaceCapacity;
        public int OptionalBranchAllowance;
    }

    [Serializable]
    public sealed class RoomSpatialDefinition
    {
        public string RoomDefinitionId;
        public RectangularFootprintDefinition GrossFootprint;
        public TileCoordinate[] ReservedTileOffsets = Array.Empty<TileCoordinate>();
        public int MaximumConnectionCount;
        public int MonsterCapacity;
        public int TrapCapacity;
        public int LootCapacity;

        public bool TryResolveGrossTiles(TileCoordinate anchor, CardinalOrientation orientation, out ResolvedTileFootprint footprint) =>
            TileFootprintResolver.TryResolveRectangle(GrossFootprint, anchor, orientation, out footprint);

        public TileCoordinate[] ResolveReservedTiles(TileCoordinate anchor, CardinalOrientation orientation)
        {
            if (GrossFootprint == null || ReservedTileOffsets == null) return Array.Empty<TileCoordinate>();
            return ReservedTileOffsets.Select(offset => TransformOffset(offset, anchor, orientation)).OrderBy(tile => tile).ToArray();
        }

        public TileCoordinate[] ResolveUsableTiles(TileCoordinate anchor, CardinalOrientation orientation)
        {
            if (!TryResolveGrossTiles(anchor, orientation, out ResolvedTileFootprint gross)) return Array.Empty<TileCoordinate>();
            var reserved = new HashSet<TileCoordinate>(ResolveReservedTiles(anchor, orientation));
            return gross.OccupiedTiles.Where(tile => !reserved.Contains(tile)).ToArray();
        }

        private TileCoordinate TransformOffset(TileCoordinate offset, TileCoordinate anchor, CardinalOrientation orientation)
        {
            switch (orientation)
            {
                case CardinalOrientation.Ninety: return new TileCoordinate(anchor.X + GrossFootprint.Height - 1 - offset.Y, anchor.Y + offset.X);
                case CardinalOrientation.OneEighty: return new TileCoordinate(anchor.X + GrossFootprint.Width - 1 - offset.X, anchor.Y + GrossFootprint.Height - 1 - offset.Y);
                case CardinalOrientation.TwoSeventy: return new TileCoordinate(anchor.X + offset.Y, anchor.Y + GrossFootprint.Width - 1 - offset.X);
                default: return new TileCoordinate(anchor.X + offset.X, anchor.Y + offset.Y);
            }
        }
    }

    [Serializable]
    public sealed class CorridorSpatialDefinition { public string CorridorDefinitionId; }

    [Serializable]
    public sealed class RoomSpatialInstance
    {
        public string RoomInstanceId;
        public string RoomDefinitionId;
        public string FloorId;
        public TileCoordinate Anchor;
        public CardinalOrientation Orientation;
    }

    [Serializable]
    public sealed class FloorSpatialLayout
    {
        public string FloorId;
        public RoomSpatialInstance[] Rooms = Array.Empty<RoomSpatialInstance>();
        public FloorRouteNode[] Nodes = Array.Empty<FloorRouteNode>();
        public FloorRouteEdge[] Edges = Array.Empty<FloorRouteEdge>();

        public FloorSpatialLayout Canonicalized()
        {
            return new FloorSpatialLayout
            {
                FloorId = FloorId,
                Rooms = (Rooms ?? Array.Empty<RoomSpatialInstance>()).Select(CopyRoom)
                    .OrderBy(room => room?.RoomInstanceId, StringComparer.Ordinal).ToArray(),
                Nodes = (Nodes ?? Array.Empty<FloorRouteNode>()).Select(CopyNode)
                    .OrderBy(node => node == null ? 0 : (int)node.Kind)
                    .ThenBy(node => node?.NodeId, StringComparer.Ordinal).ToArray(),
                Edges = (Edges ?? Array.Empty<FloorRouteEdge>()).Select(CopyEdge)
                    .OrderBy(edge => edge == null ? 0 : (int)edge.Classification)
                    .ThenBy(edge => edge?.SourceNodeId, StringComparer.Ordinal)
                    .ThenBy(edge => edge?.DestinationNodeId, StringComparer.Ordinal)
                    .ThenBy(edge => edge?.EdgeId, StringComparer.Ordinal).ToArray()
            };
        }

        private static RoomSpatialInstance CopyRoom(RoomSpatialInstance room) => room == null ? null : new RoomSpatialInstance
        {
            RoomInstanceId = room.RoomInstanceId, RoomDefinitionId = room.RoomDefinitionId, FloorId = room.FloorId,
            Anchor = room.Anchor, Orientation = room.Orientation
        };

        private static FloorRouteNode CopyNode(FloorRouteNode node) => node == null ? null : new FloorRouteNode
        {
            NodeId = node.NodeId, FloorId = node.FloorId, Kind = node.Kind, RoomInstanceId = node.RoomInstanceId ?? string.Empty
        };

        private static FloorRouteEdge CopyEdge(FloorRouteEdge edge) => edge == null ? null : new FloorRouteEdge
        {
            EdgeId = edge.EdgeId,
            CorridorDefinitionId = edge.ConnectionKind == FloorRouteConnectionKind.DirectDoorway
                ? string.Empty : edge.CorridorDefinitionId,
            FloorId = edge.FloorId,
            SourceNodeId = edge.SourceNodeId, DestinationNodeId = edge.DestinationNodeId,
            Footprint = edge.ConnectionKind == FloorRouteConnectionKind.DirectDoorway || edge.Footprint == null
                ? null : new ResolvedTileFootprint(edge.Footprint.OccupiedTiles),
            Classification = edge.Classification, OptionalBranchId = edge.OptionalBranchId ?? string.Empty,
            ConnectionKind = edge.ConnectionKind
        };
    }

    public static class FloorSpatialConfigurationOrdering
    {
        public static FloorSpatialConfiguration[] Canonicalize(IEnumerable<FloorSpatialConfiguration> floors) =>
            (floors ?? Enumerable.Empty<FloorSpatialConfiguration>()).OrderBy(floor => floor?.FloorIndex ?? 0)
                .ThenBy(floor => floor?.FloorDefinitionId, StringComparer.Ordinal).ToArray();
    }
}
