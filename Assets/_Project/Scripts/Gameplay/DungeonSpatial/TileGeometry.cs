using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum CardinalOrientation { Zero = 0, Ninety = 1, OneEighty = 2, TwoSeventy = 3 }

    [Serializable]
    public sealed class RectangularFloorBounds
    {
        public TileCoordinate Minimum;
        public int Width;
        public int Height;

        public RectangularFloorBounds() { }
        public RectangularFloorBounds(TileCoordinate minimum, int width, int height)
        {
            Minimum = minimum;
            Width = width;
            Height = height;
        }

        public bool IsValid => Width > 0 && Height > 0 &&
            (long)Minimum.X + Width - 1 <= int.MaxValue &&
            (long)Minimum.Y + Height - 1 <= int.MaxValue;
        public long TileCount => IsValid ? (long)Width * Height : 0L;

        public bool Contains(TileCoordinate coordinate) => IsValid &&
            coordinate.X >= Minimum.X && coordinate.Y >= Minimum.Y &&
            (long)coordinate.X < (long)Minimum.X + Width &&
            (long)coordinate.Y < (long)Minimum.Y + Height;
    }

    [Serializable]
    public struct TileCoordinate : IEquatable<TileCoordinate>, IComparable<TileCoordinate>
    {
        public int X;
        public int Y;

        public TileCoordinate(int x, int y) { X = x; Y = y; }
        public int CompareTo(TileCoordinate other) { int x = X.CompareTo(other.X); return x != 0 ? x : Y.CompareTo(other.Y); }
        public bool Equals(TileCoordinate other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is TileCoordinate other && Equals(other);
        public override int GetHashCode() { unchecked { return (X * 397) ^ Y; } }
    }

    [Serializable]
    public sealed class RectangularFootprintDefinition
    {
        public int Width;
        public int Height;
        public RectangularFootprintDefinition() { }
        public RectangularFootprintDefinition(int width, int height) { Width = width; Height = height; }
    }

    [Serializable]
    public sealed class ResolvedTileFootprint
    {
        public TileCoordinate[] OccupiedTiles = Array.Empty<TileCoordinate>();

        public ResolvedTileFootprint() { }
        public ResolvedTileFootprint(IEnumerable<TileCoordinate> tiles)
        {
            OccupiedTiles = (tiles ?? Enumerable.Empty<TileCoordinate>()).OrderBy(tile => tile).ToArray();
        }
    }

    public static class TileFootprintResolver
    {
        public static bool TryResolveRectangle(RectangularFootprintDefinition definition, TileCoordinate anchor,
            CardinalOrientation orientation, out ResolvedTileFootprint footprint)
        {
            footprint = new ResolvedTileFootprint();
            if (definition == null || definition.Width <= 0 || definition.Height <= 0 || !Enum.IsDefined(typeof(CardinalOrientation), orientation)) return false;
            int width = orientation == CardinalOrientation.Ninety || orientation == CardinalOrientation.TwoSeventy ? definition.Height : definition.Width;
            int height = orientation == CardinalOrientation.Ninety || orientation == CardinalOrientation.TwoSeventy ? definition.Width : definition.Height;
            long area = (long)width * height;
            long maximumX = (long)anchor.X + width - 1;
            long maximumY = (long)anchor.Y + height - 1;
            if (area > int.MaxValue || maximumX > int.MaxValue || maximumY > int.MaxValue) return false;
            var tiles = new List<TileCoordinate>((int)area);
            for (int x = 0; x < width; x++) for (int y = 0; y < height; y++)
                tiles.Add(new TileCoordinate((int)((long)anchor.X + x), (int)((long)anchor.Y + y)));
            footprint = new ResolvedTileFootprint(tiles);
            return true;
        }

        public static bool TryResolveStraightCorridor(TileCoordinate start, TileCoordinate end, out ResolvedTileFootprint footprint)
        {
            footprint = new ResolvedTileFootprint();
            long deltaX = (long)end.X - start.X, deltaY = (long)end.Y - start.Y;
            if (deltaX != 0 && deltaY != 0) return false;
            long length = Math.Max(Math.Abs(deltaX), Math.Abs(deltaY));
            long requiredLength = length + 1;
            if (requiredLength > int.MaxValue) return false;
            int dx = Math.Sign(deltaX), dy = Math.Sign(deltaY);
            var tiles = new TileCoordinate[(int)requiredLength];
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = new TileCoordinate((int)((long)start.X + (long)dx * i), (int)((long)start.Y + (long)dy * i));
            footprint = new ResolvedTileFootprint(tiles);
            return true;
        }
    }
}
