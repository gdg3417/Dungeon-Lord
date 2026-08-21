#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.Linq;
using NUnit.Framework;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class StructuralEditServiceTests
    {
        [Test]
        public void ApprovedBasicRoomPreview_IsDeterministicAndProducesCanonicalR2()
        {
            var fixture = Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            DetachedCompleteSaveValidationResult parsed = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                fixture.Result.Attempt.Candidate.GetBytes(), new DetachedCurrentTargetValidationContext(
                    fixture.Compatibility, fixture.Production, fixture.LegacyBytes, fixture.Limits));
            Assert.That(parsed.IsValid, Is.True);
            CompatibilityLayoutGeometryRecord geometry = fixture.Compatibility.Value.GeometryRecords.Single();
            CompatibilityLayoutPlacement placement = geometry.Layouts.Single(layout =>
                layout.Placements.Any(value => value.Role == CompatibilityRouteRole.BasicRoom1))
                .Placements.Single(value => value.Role == CompatibilityRouteRole.BasicRoom1);
            var request = new StructuralConstructionRequest { RoomDefinitionId = geometry.BasicRoomDefinitionId,
                RoomInstanceId = parsed.State.Floors[0].FloorInstanceId + ".room.player.0001",
                Anchor = placement.Anchor, Orientation = placement.Orientation };
            RunSimulationConfig configuration = LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes);

            StructuralEditPreview first = StructuralEditService.Preview(parsed.State, request,
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            StructuralEditPreview second = StructuralEditService.Preview(parsed.State, request,
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);

            Assert.That(first.IsValid, Is.True, string.Join(",", first.ReasonCodes));
            Assert.That(first.ProspectiveFloorSpace, Is.EqualTo(first.OccupiedTiles.Length));
            CollectionAssert.AreEqual(first.OccupiedTiles, second.OccupiedTiles);
            DetachedCanonicalMutationResult mutation = DetachedCanonicalSpatialMutation.Prepare(parsed.State,
                DetachedCanonicalMutationRequest.Construct(first), fixture.Production, fixture.Compatibility,
                configuration, fixture.Limits);
            Assert.That(mutation.IsSuccess, Is.True, mutation.Reason);
            Assert.That(mutation.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(2));
            Assert.That(parsed.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(1));
        }

        [Test]
        public void InvalidPlacement_DoesNotExposeCandidate()
        {
            var fixture = Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            DetachedCompleteSaveValidationResult parsed = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                fixture.Result.Attempt.Candidate.GetBytes(), new DetachedCurrentTargetValidationContext(
                    fixture.Compatibility, fixture.Production, fixture.LegacyBytes, fixture.Limits));
            CompatibilityLayoutGeometryRecord geometry = fixture.Compatibility.Value.GeometryRecords.Single();
            var request = new StructuralConstructionRequest { RoomDefinitionId = geometry.BasicRoomDefinitionId,
                RoomInstanceId = "compat.floor.00.room.player.0001", Anchor = new TileCoordinate(-1, -1),
                Orientation = CardinalOrientation.Zero };
            StructuralEditPreview preview = StructuralEditService.Preview(parsed.State, request,
                fixture.Production, fixture.Compatibility,
                LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes), fixture.Limits);
            Assert.That(preview.IsValid, Is.False);
            Assert.That(preview.ReasonCodes[0], Is.EqualTo(StructuralEditService.PlacementMismatchReason));
            Assert.That(parsed.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(1));
        }
    }
}
#endif
