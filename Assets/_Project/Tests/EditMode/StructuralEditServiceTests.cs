#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.Linq;
using NUnit.Framework;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class StructuralEditServiceTests
    {
        [Test]
        public void Preview_DerivedIdentityCollisionFailsBeforeCandidatePublication()
        {
            var fixture = Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            DetachedCompleteSaveValidationResult parsed = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                fixture.Result.Attempt.Candidate.GetBytes(), new DetachedCurrentTargetValidationContext(
                    fixture.Compatibility, fixture.Production, fixture.LegacyBytes, fixture.Limits));
            RunSimulationConfig configuration = LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes);
            DetachedCanonicalMutationResult r1 = DetachedCanonicalSpatialMutation.Prepare(parsed.State,
                DetachedCanonicalMutationRequest.Place("placement.category.room", "placement.option.room.basic"),
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            DetachedCanonicalMutationResult content = DetachedCanonicalSpatialMutation.Prepare(r1.State,
                DetachedCanonicalMutationRequest.Place("placement.category.monster",
                    "placement.option.monster.skeleton"), fixture.Production, fixture.Compatibility,
                configuration, fixture.Limits);
            content.State.Floors[0].RoomContents.Assignments[0].AssignmentId =
                "compat.floor.00.room.player.0000.node";
            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(content.State, fixture.Limits.Spatial,
                out DetachedCanonicalSpatialSaveState source), Is.True);
            SpatialContractResult<byte[]> before = CanonicalSpatialSaveSerializer.Serialize(source, fixture.Limits);
            StructuralEditPreview preview = StructuralEditService.Preview(source,
                new StructuralConstructionRequest { RoomDefinitionId = "spatial.room.basic",
                    Anchor = new TileCoordinate(0, 6), Orientation = CardinalOrientation.Zero,
                    TerminalConnectionPointId = "north" }, fixture.Production, fixture.Compatibility,
                configuration, fixture.Limits);
            Assert.That(preview.IsValid, Is.False);
            Assert.That(preview.ReasonCodes[0], Is.EqualTo(StructuralEditService.InvalidIdentityReason));
            Assert.That(preview.DetachedCandidate, Is.Null);
            SpatialContractResult<byte[]> after = CanonicalSpatialSaveSerializer.Serialize(source, fixture.Limits);
            CollectionAssert.AreEqual(before.Value, after.Value);
        }

        [TestCase("spatial.room.rectangle", CardinalOrientation.Ninety, 4, 2, "west", 6, 5, 41, 19)]
        [TestCase("spatial.room.large_chamber", CardinalOrientation.Ninety, 4, 1, "west", 6, 6, 56, 4)]
        [TestCase("spatial.room.basic", CardinalOrientation.Zero, 4, 2, "east", 8, 2, 42, 18)]
        public void RotatedConnectionGeometry_DerivesExpectedTerminal(string definitionId,
            CardinalOrientation orientation, int x, int y, string pointId, int terminalX,
            int terminalY, int used, int remaining)
        {
            var fixture = Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            DetachedCompleteSaveValidationResult parsed = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                fixture.Result.Attempt.Candidate.GetBytes(), new DetachedCurrentTargetValidationContext(
                    fixture.Compatibility, fixture.Production, fixture.LegacyBytes, fixture.Limits));
            RunSimulationConfig configuration = LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes);
            DetachedCanonicalMutationResult r1 = DetachedCanonicalSpatialMutation.Prepare(parsed.State,
                DetachedCanonicalMutationRequest.Place("placement.category.room", "placement.option.room.basic"),
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            var request = new StructuralConstructionRequest { RoomDefinitionId = definitionId,
                Anchor = new TileCoordinate(x, y), Orientation = orientation,
                TerminalConnectionPointId = pointId };
            StructuralEditPreview first = StructuralEditService.Preview(r1.State, request,
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            StructuralEditPreview second = StructuralEditService.Preview(r1.State, request,
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            Assert.That(first.IsValid, Is.True, string.Join(",", first.ReasonCodes));
            SavedFixedSpatialStructure terminal = first.DetachedCandidate.Floors[0].FixedStructures.Single(value =>
                value.Kind == FixedSpatialStructureKind.CompletionTerminal);
            Assert.That(terminal.Anchor.X, Is.EqualTo(terminalX));
            Assert.That(terminal.Anchor.Y, Is.EqualTo(terminalY));
            Assert.That(terminal.Orientation, Is.EqualTo(pointId == "east"
                ? CardinalOrientation.Ninety : CardinalOrientation.Zero));
            Assert.That(first.ResultingUsedFloorSpace, Is.EqualTo(used));
            Assert.That(first.ResultingRemainingFloorSpace, Is.EqualTo(remaining));
            SpatialContractResult<byte[]> firstBytes = CanonicalSpatialSaveSerializer.Serialize(
                first.DetachedCandidate, fixture.Limits);
            SpatialContractResult<byte[]> secondBytes = CanonicalSpatialSaveSerializer.Serialize(
                second.DetachedCandidate, fixture.Limits);
            Assert.That(firstBytes.IsValid, Is.True);
            Assert.That(secondBytes.IsValid, Is.True);
            CollectionAssert.AreEqual(firstBytes.Value, secondBytes.Value);
        }

        [Test]
        public void ConnectionPointTransforms_StayOnRotatedFacingBoundary()
        {
            var fixture = Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            foreach (RoomSpatialDefinition room in catalog.Rooms)
            foreach (CardinalOrientation orientation in new[] { CardinalOrientation.Zero,
                CardinalOrientation.Ninety, CardinalOrientation.OneEighty, CardinalOrientation.TwoSeventy })
            foreach (SpatialConnectionPointDefinition point in room.ConnectionPoints)
            {
                TileCoordinate transformed = StructuralEditService.TransformConnectionPointOffset(
                    point.Offset, orientation, room.GrossFootprint);
                CardinalOrientation facing = StructuralEditService.Rotate(point.Facing, orientation);
                int width = orientation == CardinalOrientation.Ninety || orientation == CardinalOrientation.TwoSeventy
                    ? room.GrossFootprint.Height : room.GrossFootprint.Width;
                int height = orientation == CardinalOrientation.Ninety || orientation == CardinalOrientation.TwoSeventy
                    ? room.GrossFootprint.Width : room.GrossFootprint.Height;
                bool onBoundary = facing == CardinalOrientation.Zero ? transformed.Y == height - 1 :
                    facing == CardinalOrientation.Ninety ? transformed.X == width - 1 :
                    facing == CardinalOrientation.OneEighty ? transformed.Y == 0 : transformed.X == 0;
                Assert.That(onBoundary, Is.True, room.RoomDefinitionId + ":" + point.ConnectionPointId + ":" + orientation);
            }
            foreach (FixedSpatialStructureDefinition structure in catalog.FixedStructures)
            foreach (CardinalOrientation orientation in structure.AllowedOrientations)
            foreach (SpatialConnectionPointDefinition point in structure.ConnectionPoints)
            {
                TileCoordinate transformed = StructuralEditService.TransformConnectionPointOffset(
                    point.Offset, orientation, structure.GrossFootprint);
                CardinalOrientation facing = StructuralEditService.Rotate(point.Facing, orientation);
                int width = orientation == CardinalOrientation.Ninety || orientation == CardinalOrientation.TwoSeventy
                    ? structure.GrossFootprint.Height : structure.GrossFootprint.Width;
                int height = orientation == CardinalOrientation.Ninety || orientation == CardinalOrientation.TwoSeventy
                    ? structure.GrossFootprint.Width : structure.GrossFootprint.Height;
                bool onBoundary = facing == CardinalOrientation.Zero ? transformed.Y == height - 1 :
                    facing == CardinalOrientation.Ninety ? transformed.X == width - 1 :
                    facing == CardinalOrientation.OneEighty ? transformed.Y == 0 : transformed.X == 0;
                Assert.That(onBoundary, Is.True, structure.StructureDefinitionId + ":" + orientation);
            }
        }

        [TestCase("spatial.room.rectangle", CardinalOrientation.Zero, 4, 1, "north", true)]
        [TestCase("spatial.room.rectangle", CardinalOrientation.Ninety, 4, 2, "west", true)]
        [TestCase("spatial.room.large_chamber", CardinalOrientation.Zero, 4, 1, "north", true)]
        [TestCase("spatial.room.large_chamber", CardinalOrientation.Ninety, 4, 1, "west", true)]
        public void NativeRoomAppend_UsesProductionGeometry(string definitionId,
            CardinalOrientation orientation, int x, int y, string terminalPoint, bool expectedValid)
        {
            var fixture = Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            DetachedCompleteSaveValidationResult parsed = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                fixture.Result.Attempt.Candidate.GetBytes(), new DetachedCurrentTargetValidationContext(
                    fixture.Compatibility, fixture.Production, fixture.LegacyBytes, fixture.Limits));
            RunSimulationConfig configuration = LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes);
            DetachedCanonicalMutationResult r1 = DetachedCanonicalSpatialMutation.Prepare(parsed.State,
                DetachedCanonicalMutationRequest.Place("placement.category.room", "placement.option.room.basic"),
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            StructuralEditPreview preview = StructuralEditService.Preview(r1.State,
                new StructuralConstructionRequest { RoomDefinitionId = definitionId,
                    Anchor = new TileCoordinate(x, y), Orientation = orientation,
                    TerminalConnectionPointId = terminalPoint }, fixture.Production,
                fixture.Compatibility, configuration, fixture.Limits);
            Assert.That(preview.IsValid, Is.EqualTo(expectedValid), string.Join(",", preview.ReasonCodes));
            if (!expectedValid) return;
            Assert.That(preview.ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.DirectDoorway));
            Assert.That(preview.Consequences.Any(value => value.Kind == StructuralChangeKind.FixedStructureMoved), Is.True);
            DetachedCanonicalSpatialSaveState candidate = preview.DetachedCandidate;
            Assert.That(candidate.Floors[0].Layout.Edges.Where(value =>
                value.EdgeId.Contains("room.player.0000")).All(value =>
                    value.ConnectionKind == FloorRouteConnectionKind.DirectDoorway &&
                    value.Footprint == null && value.CorridorDefinitionId == string.Empty), Is.True);
            Assert.That(r1.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(1));
        }

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
                Anchor = placement.Anchor, Orientation = placement.Orientation, TerminalConnectionPointId = "north" };

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
            Assert.That(mutation.State.Floors[0].Layout.Rooms.Any(value =>
                value.RoomInstanceId == "compat.floor.00.room.player.0000"), Is.True);
            SpatialContractResult<byte[]> canonicalBytes = CanonicalSpatialSaveSerializer.Serialize(
                mutation.State, fixture.Limits);
            Assert.That(canonicalBytes.IsValid, Is.True);
            SpatialContractResult<DetachedCanonicalSpatialSaveState> reopened =
                CanonicalSpatialSaveSerializer.Parse(canonicalBytes.Value, fixture.Limits);
            Assert.That(reopened.IsValid, Is.True);
            Assert.That(reopened.Value.Floors[0].Layout.Rooms.Any(value =>
                value.RoomInstanceId == "compat.floor.00.room.player.0000"), Is.True);
            Assert.That(mutation.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(2));
            Assert.That(r1.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(1));

            DetachedCanonicalMutationResult intervening = DetachedCanonicalSpatialMutation.Prepare(r1.State,
                DetachedCanonicalMutationRequest.Place("placement.category.monster",
                    "placement.option.monster.skeleton"), fixture.Production, fixture.Compatibility,
                configuration, fixture.Limits);
            Assert.That(intervening.IsSuccess, Is.True, intervening.Reason);
            StructuralEditPreview afterContent = StructuralEditService.Preview(intervening.State, request,
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            Assert.That(afterContent.IsValid, Is.True, string.Join(",", afterContent.ReasonCodes));
            Assert.That(afterContent.Consequences.Single(value => value.Kind == StructuralChangeKind.RoomAdded).StableId,
                Is.EqualTo(first.Consequences.Single(value => value.Kind == StructuralChangeKind.RoomAdded).StableId));
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
                Anchor = new TileCoordinate(-1, -1), Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "north" };
            StructuralEditPreview preview = StructuralEditService.Preview(r1.State, request,
                fixture.Production, fixture.Compatibility,
                configuration, fixture.Limits);
            Assert.That(preview.IsValid, Is.False);
            Assert.That(preview.ReasonCodes[0], Is.EqualTo(StructuralEditService.OutOfBoundsReason));
            Assert.That(r1.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(1));
        }
    }
}
#endif
