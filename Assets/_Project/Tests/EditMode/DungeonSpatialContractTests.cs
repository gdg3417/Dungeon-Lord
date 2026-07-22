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
        public void RectangularFloorBounds_UsesInclusiveMinimumExclusiveMaximum()
        {
            var bounds = new RectangularFloorBounds(new TileCoordinate(-2, 3), 4, 2);
            Assert.That(bounds.IsValid, Is.True);
            Assert.That(bounds.TileCount, Is.EqualTo(8));
            Assert.That(bounds.Contains(new TileCoordinate(-2, 3)), Is.True);
            Assert.That(bounds.Contains(new TileCoordinate(1, 4)), Is.True);
            Assert.That(bounds.Contains(new TileCoordinate(-3, 3)), Is.False);
            Assert.That(bounds.Contains(new TileCoordinate(2, 4)), Is.False);
            Assert.That(new RectangularFloorBounds(default, 0, 2).Contains(default), Is.False);
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
            Assert.That(canonical.Nodes.Where(x => x.Kind != FloorRouteNodeKind.Room).Select(x => x.RoomInstanceId), Is.All.EqualTo(string.Empty));
            Assert.That(canonical.Edges.Where(x => x.Classification == RouteClassification.Required).Select(x => x.OptionalBranchId), Is.All.EqualTo(string.Empty));
            string json = JsonUtility.ToJson(canonical);
            FloorSpatialLayout restored = JsonUtility.FromJson<FloorSpatialLayout>(json);
            AssertLayoutsEqual(canonical, restored);
            Assert.That(FloorLayoutValidator.Validate(restored, FloorLayoutValidatorTests.Configuration(), FloorLayoutValidatorTests.Definitions(), FloorLayoutValidatorTests.CorridorDefinitions()).IsValid, Is.True);
        }

        [Test]
        public void CanonicalizationAndUnityJson_PreserveMixedConnectionKindsAndDoorwayNulls()
        {
            FloorSpatialLayout source = FloorLayoutValidatorTests.ValidLayout();
            FloorRouteEdge doorway = source.Edges[1];
            doorway.ConnectionKind = FloorRouteConnectionKind.DirectDoorway;
            doorway.CorridorDefinitionId = null;
            doorway.Footprint = null;
            FloorSpatialLayout canonical = source.Canonicalized();
            FloorRouteEdge canonicalDoorway = canonical.Edges.Single(edge => edge.EdgeId == doorway.EdgeId);
            Assert.That(canonicalDoorway.CorridorDefinitionId, Is.EqualTo(string.Empty));
            Assert.That(canonicalDoorway.Footprint, Is.Null);
            Assert.That(canonicalDoorway.ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.DirectDoorway));
            Assert.That(source.Edges[1].CorridorDefinitionId, Is.Null);
            Assert.That(source.Edges[1].Footprint, Is.Null);
            FloorSpatialLayout restored = JsonUtility.FromJson<FloorSpatialLayout>(JsonUtility.ToJson(canonical));
            AssertLayoutsEqual(canonical, restored);
            CollectionAssert.AreEqual(canonical.Edges.Select(edge => edge.EdgeId), restored.Canonicalized().Edges.Select(edge => edge.EdgeId));
        }

        [Test]
        public void CanonicalizationAndUnityJson_PreserveInvalidDirectDoorwayData()
        {
            FloorSpatialLayout source = FloorLayoutValidatorTests.ValidLayout();
            FloorRouteEdge doorway = source.Edges[0];
            doorway.ConnectionKind = FloorRouteConnectionKind.DirectDoorway;
            doorway.CorridorDefinitionId = "corridor.invalid";
            TileCoordinate[] sourceTiles = { new TileCoordinate(9, 4), new TileCoordinate(8, 4) };
            doorway.Footprint = new ResolvedTileFootprint { OccupiedTiles = sourceTiles };

            FloorSpatialLayout canonical = source.Canonicalized();
            FloorSpatialLayout canonicalAgain = canonical.Canonicalized();
            FloorSpatialLayout restored = JsonUtility.FromJson<FloorSpatialLayout>(JsonUtility.ToJson(canonical));

            foreach (FloorSpatialLayout candidate in new[] { source, canonical, restored })
            {
                FloorLayoutValidationReason[] reasons = FloorLayoutValidator.Validate(candidate,
                    FloorLayoutValidatorTests.Configuration(), FloorLayoutValidatorTests.Definitions(),
                    FloorLayoutValidatorTests.CorridorDefinitions()).Issues.Select(issue => issue.Reason).ToArray();
                Assert.That(reasons, Does.Contain(FloorLayoutValidationReason.DirectDoorwayHasCorridorDefinition));
                Assert.That(reasons, Does.Contain(FloorLayoutValidationReason.DirectDoorwayHasFootprint));
            }

            FloorRouteEdge canonicalDoorway = canonical.Edges.Single(edge => edge.EdgeId == doorway.EdgeId);
            Assert.That(canonicalDoorway.CorridorDefinitionId, Is.EqualTo("corridor.invalid"));
            Assert.That(canonicalDoorway.Footprint, Is.Not.SameAs(doorway.Footprint));
            Assert.That(canonicalDoorway.Footprint.OccupiedTiles, Is.Not.SameAs(sourceTiles));
            CollectionAssert.AreEqual(new[] { new TileCoordinate(8, 4), new TileCoordinate(9, 4) }, canonicalDoorway.Footprint.OccupiedTiles);
            Assert.That(doorway.CorridorDefinitionId, Is.EqualTo("corridor.invalid"));
            Assert.That(doorway.Footprint.OccupiedTiles, Is.SameAs(sourceTiles));
            CollectionAssert.AreEqual(new[] { new TileCoordinate(9, 4), new TileCoordinate(8, 4) }, sourceTiles);
            Assert.That(JsonUtility.ToJson(canonicalAgain), Is.EqualTo(JsonUtility.ToJson(canonical)));
            AssertLayoutsEqual(canonical, restored);
        }

        [Test]
        public void Canonicalization_MixedKindsUseEstablishedKeysAndRoundTripDeterministically()
        {
            FloorSpatialLayout source = FloorLayoutValidatorTests.ValidLayout();
            source.Edges[0].ConnectionKind = FloorRouteConnectionKind.DirectDoorway;
            source.Edges[0].CorridorDefinitionId = null;
            source.Edges[0].Footprint = null;
            source.Edges[1].Classification = RouteClassification.Optional;
            source.Edges[1].OptionalBranchId = "branch.test";
            source.Edges = new[] { source.Edges[2], source.Edges[1], source.Edges[0] };

            FloorSpatialLayout canonical = source.Canonicalized();
            CollectionAssert.AreEqual(new[] { "edge.0", "edge.2", "edge.1" }, canonical.Edges.Select(edge => edge.EdgeId));
            Assert.That(canonical.Edges[0].ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.DirectDoorway));
            Assert.That(canonical.Edges[1].ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.PhysicalCorridor));
            Assert.That(canonical.Edges, Is.Not.SameAs(source.Edges));
            Assert.That(canonical.Edges[1], Is.Not.SameAs(source.Edges[0]));

            FloorLayoutValidationResult before = FloorLayoutValidator.Validate(canonical,
                FloorLayoutValidatorTests.Configuration(branches: 1), FloorLayoutValidatorTests.Definitions(),
                FloorLayoutValidatorTests.CorridorDefinitions());
            FloorSpatialLayout restored = JsonUtility.FromJson<FloorSpatialLayout>(JsonUtility.ToJson(canonical));
            FloorLayoutValidationResult after = FloorLayoutValidator.Validate(restored,
                FloorLayoutValidatorTests.Configuration(branches: 1), FloorLayoutValidatorTests.Definitions(),
                FloorLayoutValidatorTests.CorridorDefinitions());
            AssertLayoutsEqual(canonical, restored);
            CollectionAssert.AreEqual(before.Issues.Select(IssueKey), after.Issues.Select(IssueKey));
            Assert.That(after.Capacity.FinalFloorSpaceCapacity, Is.EqualTo(before.Capacity.FinalFloorSpaceCapacity));
            Assert.That(after.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(before.Capacity.UsedFloorSpaceCapacity));
            Assert.That(after.Capacity.RemainingFloorSpaceCapacity, Is.EqualTo(before.Capacity.RemainingFloorSpaceCapacity));
        }

        [Test]
        public void Canonicalization_NormalizesOnlyCopyOptionalStrings_WithoutChangingValidation()
        {
            FloorSpatialLayout source = FloorLayoutValidatorTests.ValidLayout();
            FloorRouteNode sourceEntrance = source.Nodes.Single(x => x.Kind == FloorRouteNodeKind.Entrance);
            FloorRouteEdge sourceRequiredEdge = source.Edges.Single(x => x.EdgeId == "edge.0");
            Assert.That(sourceEntrance.RoomInstanceId, Is.Null);
            Assert.That(sourceRequiredEdge.OptionalBranchId, Is.Null);
            string[] sourceIssues = FloorLayoutValidator.Validate(source, FloorLayoutValidatorTests.Configuration(),
                FloorLayoutValidatorTests.Definitions(), FloorLayoutValidatorTests.CorridorDefinitions()).Issues
                .Select(IssueKey).ToArray();

            FloorSpatialLayout canonical = source.Canonicalized();
            FloorSpatialLayout canonicalAgain = canonical.Canonicalized();

            Assert.That(sourceEntrance.RoomInstanceId, Is.Null);
            Assert.That(sourceRequiredEdge.OptionalBranchId, Is.Null);
            Assert.That(canonical.Nodes.Single(x => x.NodeId == sourceEntrance.NodeId).RoomInstanceId, Is.EqualTo(string.Empty));
            Assert.That(canonical.Edges.Single(x => x.EdgeId == sourceRequiredEdge.EdgeId).OptionalBranchId, Is.EqualTo(string.Empty));
            Assert.That(canonical.Nodes.Single(x => x.NodeId == sourceEntrance.NodeId), Is.Not.SameAs(sourceEntrance));
            Assert.That(canonical.Edges.Single(x => x.EdgeId == sourceRequiredEdge.EdgeId), Is.Not.SameAs(sourceRequiredEdge));
            Assert.That(JsonUtility.ToJson(canonicalAgain), Is.EqualTo(JsonUtility.ToJson(canonical)));
            CollectionAssert.AreEqual(sourceIssues, FloorLayoutValidator.Validate(canonical, FloorLayoutValidatorTests.Configuration(),
                FloorLayoutValidatorTests.Definitions(), FloorLayoutValidatorTests.CorridorDefinitions()).Issues.Select(IssueKey).ToArray());
        }

        [Test]
        public void Canonicalization_IsDetachedPreservesDuplicatesAndIsIdempotent()
        {
            FloorSpatialLayout source = FloorLayoutValidatorTests.ValidLayout();
            source.Edges[0].Footprint = new ResolvedTileFootprint(new[] { new TileCoordinate(2, 1), new TileCoordinate(1, 1), new TileCoordinate(1, 1) });
            FloorSpatialLayout first = source.Canonicalized(); FloorSpatialLayout second = first.Canonicalized();
            Assert.That(first.Rooms[0], Is.Not.SameAs(source.Rooms[0])); Assert.That(first.Nodes[0], Is.Not.SameAs(source.Nodes[0])); Assert.That(first.Edges[0], Is.Not.SameAs(source.Edges[0])); Assert.That(first.Edges[0].Footprint, Is.Not.SameAs(source.Edges[0].Footprint));
            Assert.That(first.Rooms, Is.Not.SameAs(source.Rooms)); Assert.That(first.Nodes, Is.Not.SameAs(source.Nodes)); Assert.That(first.Edges, Is.Not.SameAs(source.Edges));
            Assert.That(first.Edges[0].Footprint.OccupiedTiles, Is.Not.SameAs(source.Edges[0].Footprint.OccupiedTiles));
            CollectionAssert.AreEqual(new[] { new TileCoordinate(1, 1), new TileCoordinate(1, 1), new TileCoordinate(2, 1) }, first.Edges.Single(x => x.EdgeId == "edge.0").Footprint.OccupiedTiles);
            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
            first.Rooms[0].RoomInstanceId = "changed"; Assert.That(source.Rooms.Any(x => x.RoomInstanceId == "changed"), Is.False);
        }

        private static void AssertLayoutsEqual(FloorSpatialLayout expected, FloorSpatialLayout actual)
        {
            Assert.That(actual.FloorId, Is.EqualTo(expected.FloorId));
            Assert.That(actual.Rooms.Length, Is.EqualTo(expected.Rooms.Length));
            for (int index = 0; index < expected.Rooms.Length; index++)
            {
                Assert.That(actual.Rooms[index].RoomInstanceId, Is.EqualTo(expected.Rooms[index].RoomInstanceId));
                Assert.That(actual.Rooms[index].RoomDefinitionId, Is.EqualTo(expected.Rooms[index].RoomDefinitionId));
                Assert.That(actual.Rooms[index].FloorId, Is.EqualTo(expected.Rooms[index].FloorId));
                Assert.That(actual.Rooms[index].Anchor, Is.EqualTo(expected.Rooms[index].Anchor));
                Assert.That(actual.Rooms[index].Orientation, Is.EqualTo(expected.Rooms[index].Orientation));
            }
            Assert.That(actual.Nodes.Length, Is.EqualTo(expected.Nodes.Length));
            for (int index = 0; index < expected.Nodes.Length; index++)
            {
                Assert.That(actual.Nodes[index].NodeId, Is.EqualTo(expected.Nodes[index].NodeId));
                Assert.That(actual.Nodes[index].FloorId, Is.EqualTo(expected.Nodes[index].FloorId));
                Assert.That(actual.Nodes[index].Kind, Is.EqualTo(expected.Nodes[index].Kind));
                Assert.That(actual.Nodes[index].RoomInstanceId, Is.EqualTo(expected.Nodes[index].RoomInstanceId));
            }
            Assert.That(actual.Edges.Length, Is.EqualTo(expected.Edges.Length));
            for (int index = 0; index < expected.Edges.Length; index++)
            {
                Assert.That(actual.Edges[index].EdgeId, Is.EqualTo(expected.Edges[index].EdgeId));
                Assert.That(actual.Edges[index].CorridorDefinitionId, Is.EqualTo(expected.Edges[index].CorridorDefinitionId));
                Assert.That(actual.Edges[index].FloorId, Is.EqualTo(expected.Edges[index].FloorId));
                Assert.That(actual.Edges[index].SourceNodeId, Is.EqualTo(expected.Edges[index].SourceNodeId));
                Assert.That(actual.Edges[index].DestinationNodeId, Is.EqualTo(expected.Edges[index].DestinationNodeId));
                Assert.That(actual.Edges[index].Classification, Is.EqualTo(expected.Edges[index].Classification));
                Assert.That(actual.Edges[index].OptionalBranchId, Is.EqualTo(expected.Edges[index].OptionalBranchId));
                Assert.That(actual.Edges[index].ConnectionKind, Is.EqualTo(expected.Edges[index].ConnectionKind));
                if (expected.Edges[index].Footprint == null) Assert.That(actual.Edges[index].Footprint, Is.Null);
                else CollectionAssert.AreEqual(expected.Edges[index].Footprint.OccupiedTiles, actual.Edges[index].Footprint.OccupiedTiles);
            }
        }

        private static string IssueKey(FloorLayoutValidationIssue issue) =>
            $"{(int)issue.Reason}|{issue.SubjectId}|{issue.RelatedId}|{issue.HasCoordinate}|{issue.Coordinate.X}|{issue.Coordinate.Y}";

        private static RoomSpatialDefinition Definition(string id, TileCoordinate[] reserved, int monster, int trap, int loot) => new RoomSpatialDefinition
        { RoomDefinitionId = id, GrossFootprint = new RectangularFootprintDefinition(2, 2), ReservedTileOffsets = reserved, MonsterCapacity = monster, TrapCapacity = trap, LootCapacity = loot };
    }
}
#endif
