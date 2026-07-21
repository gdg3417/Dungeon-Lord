#if UNITY_EDITOR
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class DungeonSpatialContractTests
    {
        private const int RectangleWidth = 2;
        private const int RectangleHeight = 3;

        [Test]
        public void Rectangle_Resolves_OneTile_And_RotatedNonSquare_InCanonicalOrder()
        {
            Assert.That(TileFootprintResolver.TryResolveRectangle(new RectangularFootprintDefinition(1, 1), new TileCoordinate(4, 7), CardinalOrientation.Zero, out ResolvedTileFootprint single), Is.True);
            CollectionAssert.AreEqual(new[] { new TileCoordinate(4, 7) }, single.OccupiedTiles);
            Assert.That(TileFootprintResolver.TryResolveRectangle(new RectangularFootprintDefinition(RectangleWidth, RectangleHeight), new TileCoordinate(0, 0), CardinalOrientation.Ninety, out ResolvedTileFootprint rotated), Is.True);
            Assert.That(rotated.OccupiedTiles.Length, Is.EqualTo(RectangleWidth * RectangleHeight));
            Assert.That(rotated.OccupiedTiles.Last(), Is.EqualTo(new TileCoordinate(2, 1)));
            CollectionAssert.AreEqual(rotated.OccupiedTiles.OrderBy(tile => tile).ToArray(), rotated.OccupiedTiles);
        }

        [TestCase(0, 1)] [TestCase(1, -1)]
        public void Rectangle_Rejects_NonpositiveDimensions(int width, int height)
        {
            Assert.That(TileFootprintResolver.TryResolveRectangle(new RectangularFootprintDefinition(width, height), default, CardinalOrientation.Zero, out _), Is.False);
        }

        [Test]
        public void StraightCorridor_RejectsDiagonal_AndIncludesBothEndpoints()
        {
            Assert.That(TileFootprintResolver.TryResolveStraightCorridor(new TileCoordinate(0, 0), new TileCoordinate(1, 1), out _), Is.False);
            Assert.That(TileFootprintResolver.TryResolveStraightCorridor(new TileCoordinate(1, 2), new TileCoordinate(3, 2), out ResolvedTileFootprint footprint), Is.True);
            CollectionAssert.AreEqual(new[] { new TileCoordinate(1, 2), new TileCoordinate(2, 2), new TileCoordinate(3, 2) }, footprint.OccupiedTiles);
        }

        [Test]
        public void RoomCapacitiesAndReservedAreas_AreIndependentOfIdenticalFootprints()
        {
            var first = Definition("a", new[] { new TileCoordinate(0, 0) }, 1, 2, 3);
            var second = Definition("b", new[] { new TileCoordinate(1, 1) }, 4, 5, 6);
            Assert.That(first.ResolveUsableTiles(default, CardinalOrientation.Zero).Length, Is.EqualTo(3));
            Assert.That(second.ResolveUsableTiles(default, CardinalOrientation.Zero).Length, Is.EqualTo(3));
            Assert.That(first.MonsterCapacity, Is.Not.EqualTo(second.MonsterCapacity));
            Assert.That(first.ReservedTileOffsets.Single(), Is.Not.EqualTo(second.ReservedTileOffsets.Single()));
        }

        [Test]
        public void Canonicalization_AndUnityJsonRoundTrip_PreserveStandaloneGraph()
        {
            FloorSpatialLayout layout = FloorLayoutValidatorTests.ValidLayout();
            layout.Rooms = layout.Rooms.Reverse().ToArray(); layout.Nodes = layout.Nodes.Reverse().ToArray(); layout.Edges = layout.Edges.Reverse().ToArray();
            FloorSpatialLayout canonical = layout.Canonicalized();
            string json = JsonUtility.ToJson(canonical);
            FloorSpatialLayout restored = JsonUtility.FromJson<FloorSpatialLayout>(json);
            Assert.That(restored.FloorId, Is.EqualTo(FloorLayoutValidatorTests.FloorId));
            CollectionAssert.AreEqual(canonical.Rooms.Select(x => x.RoomInstanceId), restored.Rooms.Select(x => x.RoomInstanceId));
            CollectionAssert.AreEqual(canonical.Nodes.Select(x => x.NodeId), restored.Nodes.Select(x => x.NodeId));
            CollectionAssert.AreEqual(canonical.Edges.Select(x => x.EdgeId), restored.Edges.Select(x => x.EdgeId));
            Assert.That(restored.Edges[0].Footprint.OccupiedTiles[0], Is.EqualTo(canonical.Edges[0].Footprint.OccupiedTiles[0]));
            Assert.That(FloorLayoutValidator.Validate(restored, FloorLayoutValidatorTests.Configuration(), FloorLayoutValidatorTests.Definitions(), FloorLayoutValidatorTests.CorridorDefinitions()).IsValid, Is.True);
        }

        private static RoomSpatialDefinition Definition(string id, TileCoordinate[] reserved, int monster, int trap, int loot) => new RoomSpatialDefinition
        { RoomDefinitionId = id, GrossFootprint = new RectangularFootprintDefinition(2, 2), ReservedTileOffsets = reserved, MonsterCapacity = monster, TrapCapacity = trap, LootCapacity = loot };
    }
}
#endif
