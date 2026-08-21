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
            RunSimulationConfig configuration = LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes);
            DetachedCanonicalMutationResult r1 = DetachedCanonicalSpatialMutation.Prepare(parsed.State,
                DetachedCanonicalMutationRequest.Place("placement.category.room", "placement.option.room.basic"),
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            Assert.That(r1.IsSuccess, Is.True, r1.Reason);
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            FloorLayoutValidationResult r1Layout = FloorLayoutValidator.Validate(r1.State.Floors[0].Layout,
                catalog.Floors.Single(), catalog.Rooms, catalog.Corridors,
                new SpatialValidationWorkloadLimits(fixture.Limits.Spatial.MaximumMaterializedTiles),
                r1.State.Floors[0].FixedStructures, catalog.FixedStructures);
            Assert.That(r1Layout.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(26));
            CompatibilityLayoutGeometryRecord geometry = fixture.Compatibility.Value.GeometryRecords.Single();
            CompatibilityLayoutPlacement placement = geometry.Layouts.Single(layout =>
                layout.Placements.Any(value => value.Role == CompatibilityRouteRole.BasicRoom1))
                .Placements.Single(value => value.Role == CompatibilityRouteRole.BasicRoom1);
            var request = new StructuralConstructionRequest { RoomDefinitionId = geometry.BasicRoomDefinitionId,
                Anchor = placement.Anchor, Orientation = placement.Orientation };

            StructuralEditPreview first = StructuralEditService.Preview(r1.State, request,
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            StructuralEditPreview second = StructuralEditService.Preview(r1.State, request,
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);

            Assert.That(first.IsValid, Is.True, string.Join(",", first.ReasonCodes));
            Assert.That(first.ProspectiveFloorSpace, Is.EqualTo(first.OccupiedTiles.Length));
            Assert.That(first.ResultingUsedFloorSpace, Is.EqualTo(42));
            Assert.That(first.ResultingRemainingFloorSpace, Is.EqualTo(18));
            Assert.That(first.Consequences.Any(value => value.Kind == StructuralChangeKind.FixedStructureMoved), Is.True);
            CollectionAssert.AreEqual(first.OccupiedTiles, second.OccupiedTiles);
            DetachedCanonicalMutationResult mutation = DetachedCanonicalSpatialMutation.Prepare(r1.State,
                DetachedCanonicalMutationRequest.Construct(first), fixture.Production, fixture.Compatibility,
                configuration, fixture.Limits);
            Assert.That(mutation.IsSuccess, Is.True, mutation.Reason);
            Assert.That(mutation.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(2));
            Assert.That(r1.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(1));

            DetachedCanonicalMutationResult intervening = DetachedCanonicalSpatialMutation.Prepare(r1.State,
                DetachedCanonicalMutationRequest.Place("placement.category.monster",
                    "placement.option.monster.skeleton"), fixture.Production, fixture.Compatibility,
                configuration, fixture.Limits);
            Assert.That(intervening.IsSuccess, Is.True, intervening.Reason);
            DetachedCanonicalMutationResult stale = DetachedCanonicalSpatialMutation.Prepare(intervening.State,
                DetachedCanonicalMutationRequest.Construct(first), fixture.Production, fixture.Compatibility,
                configuration, fixture.Limits);
            Assert.That(stale.IsSuccess, Is.False);
            Assert.That(stale.Reason, Is.EqualTo(StructuralEditService.StalePreviewReason));
            Assert.That(intervening.State.Floors[0].RoomContents.Assignments.Length, Is.EqualTo(1));
        }

        [Test]
        public void InvalidPlacement_DoesNotExposeCandidate()
        {
            var fixture = Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            DetachedCompleteSaveValidationResult parsed = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                fixture.Result.Attempt.Candidate.GetBytes(), new DetachedCurrentTargetValidationContext(
                    fixture.Compatibility, fixture.Production, fixture.LegacyBytes, fixture.Limits));
            CompatibilityLayoutGeometryRecord geometry = fixture.Compatibility.Value.GeometryRecords.Single();
            RunSimulationConfig configuration = LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes);
            DetachedCanonicalMutationResult r1 = DetachedCanonicalSpatialMutation.Prepare(parsed.State,
                DetachedCanonicalMutationRequest.Place("placement.category.room", "placement.option.room.basic"),
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            var request = new StructuralConstructionRequest { RoomDefinitionId = geometry.BasicRoomDefinitionId,
                Anchor = new TileCoordinate(-1, -1), Orientation = CardinalOrientation.Zero };
            StructuralEditPreview preview = StructuralEditService.Preview(r1.State, request,
                fixture.Production, fixture.Compatibility,
                configuration, fixture.Limits);
            Assert.That(preview.IsValid, Is.False);
            Assert.That(preview.ReasonCodes[0], Is.EqualTo(StructuralEditService.PlacementMismatchReason));
            Assert.That(r1.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(1));
        }
    }
}
#endif
