using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public static class RoomLocalCoordinateTransform
    {
        public static bool TryToFloor(TileCoordinate roomLocalPosition,
            RectangularFootprintDefinition roomFootprint, TileCoordinate roomAnchor,
            CardinalOrientation orientation, out TileCoordinate floorPosition)
        {
            floorPosition = default(TileCoordinate);
            if (!TryToOriented(roomLocalPosition, roomFootprint, orientation,
                    out TileCoordinate oriented)) return false;
            long x = (long)roomAnchor.X + oriented.X;
            long y = (long)roomAnchor.Y + oriented.Y;
            if (x < int.MinValue || x > int.MaxValue || y < int.MinValue || y > int.MaxValue)
                return false;
            floorPosition = new TileCoordinate((int)x, (int)y);
            return true;
        }

        public static bool TryToOriented(TileCoordinate roomLocalPosition,
            RectangularFootprintDefinition baseFootprint, CardinalOrientation orientation,
            out TileCoordinate orientedPosition)
        {
            orientedPosition = default(TileCoordinate);
            if (!IsValid(baseFootprint, orientation) || roomLocalPosition.X < 0 ||
                roomLocalPosition.X >= baseFootprint.Width || roomLocalPosition.Y < 0 ||
                roomLocalPosition.Y >= baseFootprint.Height) return false;
            long x;
            long y;
            switch (orientation)
            {
                case CardinalOrientation.Ninety:
                    x = (long)baseFootprint.Height - 1 - roomLocalPosition.Y;
                    y = roomLocalPosition.X;
                    break;
                case CardinalOrientation.OneEighty:
                    x = (long)baseFootprint.Width - 1 - roomLocalPosition.X;
                    y = (long)baseFootprint.Height - 1 - roomLocalPosition.Y;
                    break;
                case CardinalOrientation.TwoSeventy:
                    x = roomLocalPosition.Y;
                    y = (long)baseFootprint.Width - 1 - roomLocalPosition.X;
                    break;
                default:
                    x = roomLocalPosition.X;
                    y = roomLocalPosition.Y;
                    break;
            }
            if (x < int.MinValue || x > int.MaxValue || y < int.MinValue || y > int.MaxValue)
                return false;
            orientedPosition = new TileCoordinate((int)x, (int)y);
            return true;
        }

        public static bool TryFromOriented(TileCoordinate orientedPosition,
            RectangularFootprintDefinition orientedFootprint, CardinalOrientation orientation,
            out TileCoordinate roomLocalPosition)
        {
            roomLocalPosition = default(TileCoordinate);
            if (!IsValid(orientedFootprint, orientation) || orientedPosition.X < 0 ||
                orientedPosition.X >= orientedFootprint.Width || orientedPosition.Y < 0 ||
                orientedPosition.Y >= orientedFootprint.Height) return false;
            long baseWidth = orientation == CardinalOrientation.Ninety ||
                orientation == CardinalOrientation.TwoSeventy
                ? orientedFootprint.Height : orientedFootprint.Width;
            long baseHeight = orientation == CardinalOrientation.Ninety ||
                orientation == CardinalOrientation.TwoSeventy
                ? orientedFootprint.Width : orientedFootprint.Height;
            long x;
            long y;
            switch (orientation)
            {
                case CardinalOrientation.Ninety:
                    x = orientedPosition.Y;
                    y = baseHeight - 1 - orientedPosition.X;
                    break;
                case CardinalOrientation.OneEighty:
                    x = baseWidth - 1 - orientedPosition.X;
                    y = baseHeight - 1 - orientedPosition.Y;
                    break;
                case CardinalOrientation.TwoSeventy:
                    x = baseWidth - 1 - orientedPosition.Y;
                    y = orientedPosition.X;
                    break;
                default:
                    x = orientedPosition.X;
                    y = orientedPosition.Y;
                    break;
            }
            if (x < 0 || x >= baseWidth || y < 0 || y >= baseHeight ||
                x > int.MaxValue || y > int.MaxValue) return false;
            roomLocalPosition = new TileCoordinate((int)x, (int)y);
            return true;
        }

        private static bool IsValid(RectangularFootprintDefinition footprint,
            CardinalOrientation orientation) => footprint != null && footprint.Width > 0 &&
            footprint.Height > 0 && Enum.IsDefined(typeof(CardinalOrientation), orientation);
    }

    [Serializable]
    public sealed class FloorSpatialConfiguration
    {
        public string FloorDefinitionId;
        public int FloorIndex;
        public RectangularFloorBounds Bounds;
        public int FinalFloorSpaceCapacity;
        public int OptionalBranchAllowance;
        public string[] AllowedRoomDefinitionIds = Array.Empty<string>();
        public string[] AllowedCorridorDefinitionIds = Array.Empty<string>();
        public string EntranceStructureDefinitionId;
        public string CompletionStructureDefinitionId;
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
        public string LocalizationKey;
        public CardinalOrientation[] AllowedOrientations = Array.Empty<CardinalOrientation>();
        public SpatialConnectionPointDefinition[] ConnectionPoints = Array.Empty<SpatialConnectionPointDefinition>();

        public bool TryResolveGrossTiles(TileCoordinate anchor, CardinalOrientation orientation,
            SpatialValidationWorkloadLimits limits, out ResolvedTileFootprint footprint) =>
            TileFootprintResolver.TryResolveRectangle(GrossFootprint, anchor, orientation, limits, out footprint);

        public TileCoordinate[] ResolveReservedTiles(TileCoordinate anchor, CardinalOrientation orientation,
            SpatialValidationWorkloadLimits limits)
        {
            if (GrossFootprint == null || ReservedTileOffsets == null || !limits.Allows(ReservedTileOffsets.LongLength))
                return Array.Empty<TileCoordinate>();
            var resolved = new List<TileCoordinate>(ReservedTileOffsets.Length);
            foreach (TileCoordinate offset in ReservedTileOffsets)
            {
                if (!RoomLocalCoordinateTransform.TryToFloor(offset, GrossFootprint, anchor,
                        orientation, out TileCoordinate tile)) return Array.Empty<TileCoordinate>();
                resolved.Add(tile);
            }
            return resolved.OrderBy(tile => tile).ToArray();
        }

        public TileCoordinate[] ResolveUsableTiles(TileCoordinate anchor, CardinalOrientation orientation,
            SpatialValidationWorkloadLimits limits)
        {
            if (ReservedTileOffsets != null && !limits.Allows(ReservedTileOffsets.LongLength)) return Array.Empty<TileCoordinate>();
            if (!TryResolveGrossTiles(anchor, orientation, limits, out ResolvedTileFootprint gross)) return Array.Empty<TileCoordinate>();
            var reserved = new HashSet<TileCoordinate>(ResolveReservedTiles(anchor, orientation, limits));
            return gross.OccupiedTiles.Where(tile => !reserved.Contains(tile)).ToArray();
        }

    }

    [Serializable]
    public sealed class CorridorSpatialDefinition
    {
        public string CorridorDefinitionId;
        public string LocalizationKey;
        public CorridorSpatialCategory Category;
        public int MinimumLength;
        public int MaximumLength;
        public int Width;
        public int MonsterCapacity;
        public int TrapCapacity;
        public int LootCapacity;
        public CardinalOrientation[] AllowedOrientations = Array.Empty<CardinalOrientation>();
        public string[] CompatibleSocketTypeIds = Array.Empty<string>();
    }

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

        public bool TryCanonicalize(SpatialValidationWorkloadLimits limits, out FloorSpatialLayout canonical)
        {
            canonical = null;
            if (!limits.IsValid) return false;
            FloorRouteEdge[] suppliedEdges = Edges ?? Array.Empty<FloorRouteEdge>();
            foreach (FloorRouteEdge edge in suppliedEdges)
            {
                if (edge?.Footprint?.OccupiedTiles != null && !limits.Allows(edge.Footprint.OccupiedTiles.LongLength))
                    return false;
            }

            FloorRouteEdge[] copiedEdges = new FloorRouteEdge[suppliedEdges.Length];
            for (int index = 0; index < suppliedEdges.Length; index++)
                if (!TryCopyEdge(suppliedEdges[index], limits, out copiedEdges[index])) return false;

            canonical = new FloorSpatialLayout
            {
                FloorId = FloorId,
                Rooms = (Rooms ?? Array.Empty<RoomSpatialInstance>()).Select(CopyRoom)
                    .OrderBy(room => room?.RoomInstanceId, StringComparer.Ordinal).ToArray(),
                Nodes = (Nodes ?? Array.Empty<FloorRouteNode>()).Select(CopyNode)
                    .OrderBy(node => node == null ? 0 : (int)node.Kind)
                    .ThenBy(node => node?.NodeId, StringComparer.Ordinal).ToArray(),
                Edges = copiedEdges
                    .OrderBy(edge => edge == null ? 0 : (int)edge.Classification)
                    .ThenBy(edge => edge?.SourceNodeId, StringComparer.Ordinal)
                    .ThenBy(edge => edge?.DestinationNodeId, StringComparer.Ordinal)
                    .ThenBy(edge => edge?.EdgeId, StringComparer.Ordinal).ToArray()
            };
            return true;
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

        private static bool TryCopyEdge(FloorRouteEdge edge, SpatialValidationWorkloadLimits limits, out FloorRouteEdge copy)
        {
            copy = null;
            if (edge == null) return true;
            ResolvedTileFootprint footprint = null;
            if (edge.Footprint != null)
            {
                TileCoordinate[] suppliedTiles = edge.Footprint.OccupiedTiles;
                long tileCount = suppliedTiles?.LongLength ?? 0L;
                if (!limits.Allows(tileCount)) return false;
                TileCoordinate[] copiedTiles = suppliedTiles == null ? Array.Empty<TileCoordinate>() : (TileCoordinate[])suppliedTiles.Clone();
                Array.Sort(copiedTiles);
                footprint = new ResolvedTileFootprint { OccupiedTiles = copiedTiles };
            }
            copy = new FloorRouteEdge
            {
                EdgeId = edge.EdgeId,
                CorridorDefinitionId = edge.ConnectionKind == FloorRouteConnectionKind.DirectDoorway &&
                    string.IsNullOrWhiteSpace(edge.CorridorDefinitionId) ? string.Empty : edge.CorridorDefinitionId ?? string.Empty,
                FloorId = edge.FloorId,
                SourceNodeId = edge.SourceNodeId, DestinationNodeId = edge.DestinationNodeId,
                Footprint = footprint,
                Classification = edge.Classification, OptionalBranchId = edge.OptionalBranchId ?? string.Empty,
                ConnectionKind = edge.ConnectionKind
            };
            return true;
        }
    }

    public static class FloorSpatialConfigurationOrdering
    {
        public static FloorSpatialConfiguration[] Canonicalize(IEnumerable<FloorSpatialConfiguration> floors) =>
            (floors ?? Enumerable.Empty<FloorSpatialConfiguration>()).OrderBy(floor => floor?.FloorIndex ?? 0)
                .ThenBy(floor => floor?.FloorDefinitionId, StringComparer.Ordinal).ToArray();
    }
}
