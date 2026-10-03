#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class PhaseSevenA2PositionalCanonicalStateTests
    {
        [Test]
        public void FrozenSchemaTwelveFiveFieldAssignmentMigratesDeterministicallyWithoutMutatingSource()
        {
            Fixture fixture = WithOneAssignment(out DetachedCanonicalSpatialSaveState positional);
            fixture = fixture.Rebase(positional);
            byte[] schemaThirteen = fixture.Session.GetCurrentBytes();
            byte[] schemaTwelve = PhaseFourTestSupport.FrozenTwelve(schemaThirteen);
            byte[] protectedSource = (byte[])schemaTwelve.Clone();

            DetachedCompleteSaveValidationResult frozen =
                DetachedCompleteSaveContract.ParseValidateFrozenSchemaTwelveAndRoundTrip(
                    schemaTwelve, fixture.Profile.Canonical);
            Assert.That(frozen.IsValid, Is.True);
            Assert.That(Encoding.UTF8.GetString(schemaTwelve), Does.Not.Contain("RoomLocalPosition"));
            Assert.That(SchemaTwelveToThirteenUpgrade.TryPrepare(schemaTwelve,
                fixture.Profile.Canonical, fixture.PositionProfiles, out byte[] first), Is.True);
            Assert.That(SchemaTwelveToThirteenUpgrade.TryPrepare(schemaTwelve,
                fixture.Profile.Canonical, fixture.PositionProfiles, out byte[] second), Is.True);
            CollectionAssert.AreEqual(protectedSource, schemaTwelve);
            CollectionAssert.AreEqual(first, second);

            DetachedCompleteSaveValidationResult upgraded =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(first, fixture.Context);
            Assert.That(upgraded.IsValid && upgraded.CurrentTargetValidated, Is.True);
            AssertPreserved(frozen.State, upgraded.State);
            TileCoordinate migratedPosition =
                upgraded.State.Floors[0].RoomContents.Assignments[0].RoomLocalPosition;
            Assert.That(migratedPosition.X, Is.EqualTo(1));
            Assert.That(migratedPosition.Y, Is.EqualTo(1));
            Assert.That(Encoding.UTF8.GetString(first), Does.Contain("\"schemaVersion\":13"));
            Assert.That(Encoding.UTF8.GetString(first), Does.Contain("RoomLocalPosition"));
        }

        [Test]
        public void SchemaThirteenPositionRoundTripsCanonicallyAndDoesNotRemigrate()
        {
            Fixture fixture = WithOneAssignment(out DetachedCanonicalSpatialSaveState state).Rebase(state);
            byte[] bytes = fixture.Session.GetCurrentBytes();
            SpatialContractResult<byte[]> spatial = CanonicalSpatialSaveSerializer.Serialize(
                fixture.State, fixture.Profile.Canonical);
            Assert.That(spatial.IsValid, Is.True);
            Assert.That(Encoding.UTF8.GetString(spatial.Value), Does.Contain(
                "\"RoomLocalPosition\":{\"X\":0,\"Y\":0}"));
            SpatialContractResult<DetachedCanonicalSpatialSaveState> parsed =
                CanonicalSpatialSaveSerializer.Parse(spatial.Value, fixture.Profile.Canonical);
            Assert.That(parsed.IsValid, Is.True);
            CollectionAssert.AreEqual(spatial.Value,
                CanonicalSpatialSaveSerializer.Serialize(parsed.Value, fixture.Profile.Canonical).Value);
            Assert.That(SchemaTwelveToThirteenUpgrade.TryPrepare(bytes, fixture.Profile.Canonical,
                fixture.PositionProfiles, out byte[] ignored), Is.False);
        }

        [Test]
        public void ExplicitPlacementAndRedeploymentFailAtomicallyForMissingInvalidOrOccupiedPosition()
        {
            Fixture fixture = Fixture.Create(null);
            DetachedCanonicalMutationResult room = fixture.Prepare(DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId,
                null, null, null));
            Assert.That(room.IsSuccess, Is.True, room.Reason);
            DetachedCanonicalMutationResult missing = Prepare(fixture, room.State,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId, null, null, null));
            Assert.That(missing.Reason, Is.EqualTo(DetachedCanonicalSpatialMutation.PositionRequiredReason));

            DetachedCanonicalMutationResult first = Prepare(fixture, room.State,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId, null, null, new TileCoordinate(0, 0)));
            Assert.That(first.IsSuccess, Is.True, first.Reason);
            byte[] before = CanonicalSpatialSaveSerializer.Serialize(first.State,
                fixture.Profile.Canonical).Value;
            DetachedCanonicalMutationResult overlap = Prepare(fixture, first.State,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.TrapCategoryId,
                    MvpDungeonPlacementIds.SpikeTrapOptionId, null, null, new TileCoordinate(0, 0)));
            Assert.That(overlap.Reason, Is.EqualTo(DetachedCanonicalSpatialMutation.PositionInvalidReason));
            CollectionAssert.AreEqual(before, CanonicalSpatialSaveSerializer.Serialize(first.State,
                fixture.Profile.Canonical).Value);

            DetachedCanonicalMutationResult unassigned = DetachedCanonicalSpatialMutation.Prepare(
                first.State, DetachedCanonicalMutationRequest.Unassign(
                    first.State.Floors[0].RoomContents.Assignments[0].AssignmentId),
                fixture.Production, fixture.Compatibility, fixture.Configuration,
                fixture.Profile.Canonical, fixture.RemovalPolicy, null, null, null, null,
                fixture.Occupancy);
            Assert.That(unassigned.IsSuccess, Is.True, unassigned.Reason);
            string returnedId = unassigned.State.LifecycleAndOwnership.ReturnedContents.Single().AssignmentId;
            DetachedCanonicalMutationResult invalidRedeployment = Prepare(fixture, unassigned.State,
                DetachedCanonicalMutationRequest.Redeploy(returnedId,
                    unassigned.State.Floors[0].Layout.Rooms[0].RoomInstanceId, null,
                    new TileCoordinate(-1, 0)));
            Assert.That(invalidRedeployment.Reason,
                Is.EqualTo(DetachedCanonicalSpatialMutation.PositionInvalidReason));
            Assert.That(unassigned.State.LifecycleAndOwnership.ReturnedContents.Single().AssignmentId,
                Is.EqualTo(returnedId));
        }

        [Test]
        public void SharedOrientationTransformMovesWithRoomAndIsDeterministic()
        {
            var footprint = new RectangularFootprintDefinition(4, 3);
            TileCoordinate local = new TileCoordinate(1, 0);
            AssertFloor(local, footprint, new TileCoordinate(10, 20), CardinalOrientation.Zero, 11, 20);
            AssertFloor(local, footprint, new TileCoordinate(10, 20), CardinalOrientation.Ninety, 12, 21);
            AssertFloor(local, footprint, new TileCoordinate(10, 20), CardinalOrientation.OneEighty, 12, 22);
            AssertFloor(local, footprint, new TileCoordinate(10, 20), CardinalOrientation.TwoSeventy, 10, 22);
            AssertFloor(local, footprint, new TileCoordinate(15, 27), CardinalOrientation.Ninety, 17, 28);
        }

        [Test]
        public void PositionChangesFloorKnowledgeButActivationOnlyChangesDoNot()
        {
            Fixture fixture = WithOneAssignment(out DetachedCanonicalSpatialSaveState state);
            string floorId = state.Floors[0].FloorInstanceId;
            var corridor = new CorridorContentAuthority();
            Assert.That(FloorKnowledgeApplicability.TryCompute(state, corridor,
                fixture.Profile.Canonical, floorId, out string original), Is.True);
            DetachedCanonicalSpatialSaveState moved = Clone(state, fixture);
            moved.Floors[0].RoomContents.Assignments[0].RoomLocalPosition = new TileCoordinate(2, 0);
            Assert.That(FloorKnowledgeApplicability.TryCompute(moved, corridor,
                fixture.Profile.Canonical, floorId, out string movedHash), Is.True);
            Assert.That(movedHash, Is.Not.EqualTo(original));
            Fixture activationFixture = PhaseSixA3FloorActivationEligibilityTests.Eligible();
            SavedSpatialFloor secondFloor = activationFixture.State.Floors.Single(value =>
                value.FloorIndex == 1);
            Assert.That(secondFloor.ActivationState, Is.EqualTo(FloorActivationState.Inactive));
            Assert.That(FloorKnowledgeApplicability.TryCompute(activationFixture.State, corridor,
                activationFixture.Profile.Canonical, secondFloor.FloorInstanceId,
                out string inactiveHash), Is.True);
            DetachedCanonicalSpatialSaveState activation = Clone(
                activationFixture.State, activationFixture);
            activation.Floors.Single(value => value.FloorIndex == 1).ActivationState =
                FloorActivationState.Active;
            Assert.That(FloorKnowledgeApplicability.TryCompute(activation, corridor,
                activationFixture.Profile.Canonical, secondFloor.FloorInstanceId,
                out string activeHash), Is.True);
            Assert.That(activeHash, Is.EqualTo(inactiveHash));
        }

        [Test]
        public void SyntheticMultiTileOccupancyRejectsUnsupportedAndOverlappingTiles()
        {
            Fixture fixture = WithOneAssignment(out DetachedCanonicalSpatialSaveState state);
            RoomContentSpatialOccupancySnapshot multi = SyntheticOccupancy(
                MvpDungeonPlacementIds.SkeletonOptionId,
                MvpDungeonPlacementIds.MonsterCategoryId,
                new TileCoordinate(0, 0), new TileCoordinate(1, 0));
            DetachedCanonicalProductionSemanticValidationResult valid =
                DetachedCanonicalProductionSemanticValidation.Validate(state, fixture.Production,
                    fixture.Configuration, fixture.Profile.Canonical.Spatial, multi, true);
            Assert.That(valid.IsValid, Is.True, string.Join(",", valid.Issues));
            state.Floors[0].RoomContents.Assignments[0].RoomLocalPosition = new TileCoordinate(3, 0);
            DetachedCanonicalProductionSemanticValidationResult invalid =
                DetachedCanonicalProductionSemanticValidation.Validate(state, fixture.Production,
                    fixture.Configuration, fixture.Profile.Canonical.Spatial, multi, true);
            Assert.That(invalid.Issues,
                Does.Contain(DetachedCanonicalProductionSemanticIssue.AssignmentPosition));
        }

        private static Fixture WithOneAssignment(out DetachedCanonicalSpatialSaveState state)
        {
            Fixture fixture = Fixture.Create(null);
            DetachedCanonicalMutationResult room = fixture.Prepare(DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId,
                null, null, null));
            Assert.That(room.IsSuccess, Is.True, room.Reason);
            DetachedCanonicalMutationResult content = Prepare(fixture, room.State,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId, null, null, new TileCoordinate(0, 0)));
            Assert.That(content.IsSuccess, Is.True, content.Reason);
            state = content.State;
            return fixture;
        }

        private static DetachedCanonicalMutationResult Prepare(Fixture fixture,
            DetachedCanonicalSpatialSaveState state, DetachedCanonicalMutationRequest request) =>
            DetachedCanonicalSpatialMutation.Prepare(state, request, fixture.Production,
                fixture.Compatibility, fixture.Configuration, fixture.Profile.Canonical,
                null, null, null, null, null, fixture.Occupancy);

        private static void AssertPreserved(DetachedCanonicalSpatialSaveState before,
            DetachedCanonicalSpatialSaveState after)
        {
            SavedSpatialFloor left = before.Floors.Single();
            SavedSpatialFloor right = after.Floors.Single();
            Assert.That(right.FloorInstanceId, Is.EqualTo(left.FloorInstanceId));
            Assert.That(right.FloorDefinitionId, Is.EqualTo(left.FloorDefinitionId));
            Assert.That(right.FloorIndex, Is.EqualTo(left.FloorIndex));
            Assert.That(right.Layout.Rooms.Select(value => value.RoomInstanceId),
                Is.EqualTo(left.Layout.Rooms.Select(value => value.RoomInstanceId)));
            RoomContentAssignment oldAssignment = left.RoomContents.Assignments.Single();
            RoomContentAssignment newAssignment = right.RoomContents.Assignments.Single();
            Assert.That(newAssignment.AssignmentId, Is.EqualTo(oldAssignment.AssignmentId));
            Assert.That(newAssignment.RoomInstanceId, Is.EqualTo(oldAssignment.RoomInstanceId));
            Assert.That(newAssignment.CategoryId, Is.EqualTo(oldAssignment.CategoryId));
            Assert.That(newAssignment.OptionId, Is.EqualTo(oldAssignment.OptionId));
            Assert.That(newAssignment.Sequence, Is.EqualTo(oldAssignment.Sequence));
        }

        private static DetachedCanonicalSpatialSaveState Clone(
            DetachedCanonicalSpatialSaveState state, Fixture fixture)
        {
            SpatialContractResult<byte[]> bytes = CanonicalSpatialSaveSerializer.Serialize(
                state, fixture.Profile.Canonical);
            return CanonicalSpatialSaveSerializer.Parse(bytes.Value, fixture.Profile.Canonical).Value;
        }

        private static void AssertFloor(TileCoordinate local,
            RectangularFootprintDefinition footprint, TileCoordinate anchor,
            CardinalOrientation orientation, int x, int y)
        {
            Assert.That(RoomLocalCoordinateTransform.TryToFloor(local, footprint, anchor,
                orientation, out TileCoordinate actual), Is.True);
            Assert.That(actual, Is.EqualTo(new TileCoordinate(x, y)));
        }

        private static RoomContentSpatialOccupancySnapshot SyntheticOccupancy(string option,
            string category, params TileCoordinate[] offsets) =>
            new RoomContentSpatialOccupancySnapshot(new RoomContentSpatialOccupancyConfiguration
            {
                    Schema = "room_content_spatial_occupancy", SchemaVersion = 1,
                    MaximumValidationMaterializedTiles = 256,
                    Records = new[]
                {
                    new RoomContentSpatialOccupancyRecord
                    {
                        CategoryId = category, OptionId = option,
                        OccupiedTileOffsets = offsets,
                        ShareableCategoryIds = Array.Empty<string>()
                    }
                }
            });
    }
}
#endif
