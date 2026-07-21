using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum CardinalOrientation { Zero = 0, Ninety = 1, OneEighty = 2, TwoSeventy = 3 }

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
            OccupiedTiles = (tiles ?? Enumerable.Empty<TileCoordinate>()).Distinct().OrderBy(tile => tile).ToArray();
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
            var tiles = new List<TileCoordinate>(width * height);
            for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) tiles.Add(new TileCoordinate(anchor.X + x, anchor.Y + y));
            footprint = new ResolvedTileFootprint(tiles);
            return true;
        }

        public static bool TryResolveStraightCorridor(TileCoordinate start, TileCoordinate end, out ResolvedTileFootprint footprint)
        {
            footprint = new ResolvedTileFootprint();
            if (start.Equals(end) || (start.X != end.X && start.Y != end.Y)) return false;
            int dx = Math.Sign(end.X - start.X), dy = Math.Sign(end.Y - start.Y);
            int length = Math.Max(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));
            var tiles = new TileCoordinate[length + 1];
            for (int i = 0; i <= length; i++) tiles[i] = new TileCoordinate(start.X + dx * i, start.Y + dy * i);
            footprint = new ResolvedTileFootprint(tiles);
            return true;
        }
    }
}
