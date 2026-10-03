#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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
            foreach (CardinalOrientation orientation in Enum.GetValues(typeof(CardinalOrientation)))
            {
                Assert.That(RoomLocalCoordinateTransform.TryToOriented(local, footprint, orientation,
                    out TileCoordinate oriented), Is.True);
                var orientedFootprint = orientation == CardinalOrientation.Ninety ||
                    orientation == CardinalOrientation.TwoSeventy
                    ? new RectangularFootprintDefinition(footprint.Height, footprint.Width)
                    : new RectangularFootprintDefinition(footprint.Width, footprint.Height);
                Assert.That(RoomLocalCoordinateTransform.TryFromOriented(oriented, orientedFootprint,
                    orientation, out TileCoordinate roundTrip), Is.True);
                Assert.That(roundTrip, Is.EqualTo(local));
            }
        }

        [Test]
        public void CurrentSchemaRecoveryUsesExplicitOccupancyAndFailsClosedWhenItIsMissing()
        {
            Fixture fixture = WithOneAssignment(out DetachedCanonicalSpatialSaveState state).Rebase(state);
            byte[] before = fixture.Session.GetCurrentBytes();
            byte[] legacy = LegacyGameplayConfigurationContract.SerializeCanonical(fixture.Configuration);
            var context = new DetachedSpatialMigrationRecoveryContext(fixture.Compatibility,
                fixture.Production, new Dictionary<string, byte[]>(), legacy,
                fixture.Profile.Canonical, fixture.Profile.Raw,
                new RawSaveEnvelopeVersionContract(1, 6),
                Gd66DetachedSpatialMigrationTransactionTests.BlankFloorForCoordinator,
                fixture.Profile.Whole, fixture.Occupancy);

            DetachedSpatialMigrationOutcome recovered =
                new DetachedSpatialMigrationTransaction(fixture.FileSystem, context)
                    .Recover(fixture.ActivePath);

            Assert.That(recovered.IsSuccess, Is.True, recovered.Reason);
            Assert.That(recovered.Reason,
                Is.EqualTo(DetachedSpatialMigrationTransaction.AlreadyCommittedReason));
            Assert.That(recovered.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            CollectionAssert.AreEqual(before, fixture.FileSystem.ReadAllBytes(fixture.ActivePath));
            DetachedCompleteSaveValidationResult reopened =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(before, fixture.Context);
            Assert.That(reopened.IsValid && reopened.CurrentTargetValidated, Is.True);
            Assert.That(reopened.State.Floors.SelectMany(value => value.RoomContents.Assignments)
                .Select(IdentityAndPosition), Is.EqualTo(state.Floors.SelectMany(
                    value => value.RoomContents.Assignments).Select(IdentityAndPosition)));

            var missingFileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string missingPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "phase7a2-missing-occupancy.json"));
            missingFileSystem.Seed(missingPath, before);
            var missingContext = new DetachedSpatialMigrationRecoveryContext(fixture.Compatibility,
                fixture.Production, new Dictionary<string, byte[]>(), legacy,
                fixture.Profile.Canonical, fixture.Profile.Raw,
                new RawSaveEnvelopeVersionContract(1, 6),
                Gd66DetachedSpatialMigrationTransactionTests.BlankFloorForCoordinator,
                fixture.Profile.Whole);
            DetachedSpatialMigrationOutcome rejected =
                new DetachedSpatialMigrationTransaction(missingFileSystem, missingContext)
                    .Recover(missingPath);
            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(rejected.Reason,
                Is.EqualTo(DetachedSpatialMigrationTransaction.ContradictoryAuthorityReason));
            CollectionAssert.AreEqual(before, missingFileSystem.ReadAllBytes(missingPath));
        }

        [TestCase("spatial.room.basic", CardinalOrientation.Zero, 4, 2, "east")]
        [TestCase("spatial.room.rectangle", CardinalOrientation.Zero, 4, 1, "north")]
        [TestCase("spatial.room.rectangle", CardinalOrientation.Ninety, 4, 2, "west")]
        [TestCase("spatial.room.large_chamber", CardinalOrientation.Zero, 4, 1, "north")]
        [TestCase("spatial.room.large_chamber", CardinalOrientation.Ninety, 4, 1, "west")]
        public void MaximumFrozenProfileEnvelopeMigratesFromOrientedSlotsToCanonicalPositions(
            string roomDefinitionId, CardinalOrientation orientation, int anchorX, int anchorY,
            string terminalPoint)
        {
            Fixture fixture = Fixture.Create(null);
            DetachedCanonicalMutationResult firstRoom = fixture.Prepare(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId, null, null, null));
            Assert.That(firstRoom.IsSuccess, Is.True, firstRoom.Reason);
            DetachedCanonicalSpatialSaveState state = firstRoom.State;
            RoomSpatialInstance target = state.Floors[0].Layout.Rooms.Single();
            if (roomDefinitionId != "spatial.room.basic")
            {
                StructuralEditPreview construction = StructuralEditService.Preview(state,
                    new StructuralConstructionRequest { RoomDefinitionId = roomDefinitionId,
                        Anchor = new TileCoordinate(anchorX, anchorY), Orientation = orientation,
                        TerminalConnectionPointId = terminalPoint }, fixture.Production,
                    fixture.Compatibility, fixture.Configuration, fixture.Profile.Canonical);
                Assert.That(construction.IsValid, Is.True, string.Join(",", construction.ReasonCodes));
                state = construction.DetachedCandidate;
                target = state.Floors[0].Layout.Rooms.Single(value =>
                    value.RoomDefinitionId == roomDefinitionId);
            }
            Assert.That(fixture.PositionProfiles.TryGetProfile(roomDefinitionId, orientation,
                out ValidatedRoomContentPositionMigrationProfile validated), Is.True);
            RoomContentPositionMigrationProfile profile = fixture.PositionProfiles.Value.Profiles.Single(
                value => value.RoomDefinitionId == roomDefinitionId && value.Orientation == orientation);
            RoomContentAssignment[] assignments = MaximumAssignments(profile, target.RoomInstanceId);
            RoomContentPositionMigrationPlanResult plan =
                RoomContentPositionMigrationPlanner.Plan(validated, target.RoomInstanceId, assignments);
            Assert.That(plan.Success, Is.True, profile.ProfileId);
            var expectedOriented = plan.Entries.ToDictionary(value => value.AssignmentId,
                value => value.RoomLocalPosition, StringComparer.Ordinal);
            RoomSpatialDefinition roomDefinition = fixture.Production.Catalog.Rooms.Single(value =>
                value.RoomDefinitionId == roomDefinitionId);
            foreach (RoomContentAssignment assignment in assignments)
            {
                Assert.That(RoomLocalCoordinateTransform.TryFromOriented(
                    expectedOriented[assignment.AssignmentId], profile.FrozenFootprint, orientation,
                    out TileCoordinate canonical), Is.True);
                assignment.RoomLocalPosition = canonical;
            }
            state.Floors[0].RoomContents.Assignments = assignments;
            state.Floors[0].RoomContents.NextSequence = assignments.Max(value => value.Sequence) + 1;
            DetachedCanonicalProductionSemanticValidationResult semantic =
                DetachedCanonicalProductionSemanticValidation.Validate(state, fixture.Production,
                    fixture.Configuration, fixture.Profile.Canonical.Spatial, fixture.Occupancy, true);
            Assert.That(semantic.IsValid, Is.True, string.Join(",", semantic.Issues));
            DetachedCanonicalSaveSessionResult replacement =
                fixture.Session.PrepareSpatialOnlyReplacement(state, StructuralInvestment.Zero(state));
            Assert.That(replacement.IsSuccess, Is.True, replacement.Reason);
            byte[] current = replacement.Update.GetBytes();
            byte[] schemaTwelve = PhaseFourTestSupport.FrozenTwelve(current);
            byte[] protectedSource = (byte[])schemaTwelve.Clone();

            Assert.That(SchemaTwelveToThirteenUpgrade.TryPrepare(schemaTwelve,
                fixture.Profile.Canonical, fixture.PositionProfiles, out byte[] first), Is.True);
            Assert.That(SchemaTwelveToThirteenUpgrade.TryPrepare(schemaTwelve,
                fixture.Profile.Canonical, fixture.PositionProfiles, out byte[] second), Is.True);
            CollectionAssert.AreEqual(protectedSource, schemaTwelve);
            CollectionAssert.AreEqual(first, second);
            DetachedCompleteSaveValidationResult frozen =
                DetachedCompleteSaveContract.ParseValidateFrozenSchemaTwelveAndRoundTrip(
                    schemaTwelve, fixture.Profile.Canonical);
            DetachedCompleteSaveValidationResult upgraded =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(first, fixture.Context);
            Assert.That(frozen.IsValid, Is.True);
            Assert.That(upgraded.IsValid && upgraded.CurrentTargetValidated, Is.True,
                upgraded.Reason);
            RoomContentAssignment[] migrated = upgraded.State.Floors[0].RoomContents.Assignments;
            Assert.That(migrated, Has.Length.EqualTo(assignments.Length));
            Assert.That(upgraded.State.LifecycleAndOwnership.ReturnedContents,
                Has.Length.EqualTo(state.LifecycleAndOwnership.ReturnedContents.Length));
            foreach (RoomContentAssignment assignment in migrated)
            {
                RoomContentAssignment source = assignments.Single(value =>
                    value.AssignmentId == assignment.AssignmentId);
                Assert.That(assignment.RoomInstanceId, Is.EqualTo(source.RoomInstanceId));
                Assert.That(assignment.CategoryId, Is.EqualTo(source.CategoryId));
                Assert.That(assignment.OptionId, Is.EqualTo(source.OptionId));
                Assert.That(assignment.Sequence, Is.EqualTo(source.Sequence));
                Assert.That(RoomLocalCoordinateTransform.TryToOriented(
                    assignment.RoomLocalPosition, roomDefinition.GrossFootprint, orientation,
                    out TileCoordinate oriented), Is.True);
                Assert.That(oriented, Is.EqualTo(expectedOriented[assignment.AssignmentId]));
            }
            if (roomDefinitionId == "spatial.room.rectangle" &&
                orientation == CardinalOrientation.Ninety)
                Assert.That(migrated.Where(value => value.CategoryId ==
                    MvpDungeonPlacementIds.MonsterCategoryId).Select(value =>
                    expectedOriented[value.AssignmentId]), Does.Contain(new TileCoordinate(3, 1)));
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

        [Test]
        public void ProductionOccupancyIsCompleteAndMaximumCurrentEnvelopeFitsApprovedWorkload()
        {
            ProductionSpatialContentWorkloadLimitParseResult workload =
                ProductionSpatialContentWorkloadLimitParser.Parse(
                    UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(
                        "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json"));
            Assert.That(workload.Success, Is.True);
            Fixture fixture = Fixture.Create(null);
            Assert.That(RoomContentSpatialOccupancyAuthority.TryParse(File.ReadAllBytes(
                RoomContentSpatialOccupancyAuthority.ProductionPath), workload.Limits,
                fixture.Configuration, out RoomContentSpatialOccupancySnapshot occupancy), Is.True);
            Assert.That(occupancy.MaximumValidationMaterializedTiles,
                Is.EqualTo(workload.Limits.MaximumMaterializedTiles));
            string[] configured = fixture.Configuration.MvpPlacementEffects.Where(value =>
                    value != null && value.CategoryId != MvpDungeonPlacementIds.RoomCategoryId)
                .Select(value => value.OptionId).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            string[] covered = occupancy.Value.Records.Select(value => value.OptionId)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(configured, covered);

            int[] perFloorEnvelopes = fixture.Production.Catalog.Floors.Select(floor =>
                MaximumMaterializedRoomContentEnvelope(floor, fixture.Production.Catalog.Rooms)).ToArray();
            CollectionAssert.AreEqual(new[] { 84, 110 }, perFloorEnvelopes);
            Assert.That(perFloorEnvelopes.Sum(), Is.EqualTo(194));
            Assert.That(perFloorEnvelopes.All(value =>
                value <= occupancy.MaximumValidationMaterializedTiles), Is.True);
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

        private static RoomContentAssignment[] MaximumAssignments(
            RoomContentPositionMigrationProfile profile, string roomInstanceId)
        {
            var result = new List<RoomContentAssignment>();
            foreach (RoomContentMigrationCategoryCapacity capacity in profile.CategoryCapacities)
            {
                string option = capacity.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId
                    ? MvpDungeonPlacementIds.SkeletonOptionId
                    : capacity.CategoryId == MvpDungeonPlacementIds.TrapCategoryId
                        ? MvpDungeonPlacementIds.SpikeTrapOptionId
                        : MvpDungeonPlacementIds.BasicLootNodeOptionId;
                for (int index = 0; index < capacity.MaximumAssignments; index++)
                    result.Add(new RoomContentAssignment
                    {
                        AssignmentId = "p7." + profile.RoomDefinitionId + "." +
                            (int)profile.Orientation + "." + CategoryToken(capacity.CategoryId) + "." + index,
                        RoomInstanceId = roomInstanceId, CategoryId = capacity.CategoryId,
                        OptionId = option, Sequence = index
                    });
            }
            return result.ToArray();
        }

        private static string CategoryToken(string categoryId) =>
            categoryId == MvpDungeonPlacementIds.MonsterCategoryId ? "m" :
            categoryId == MvpDungeonPlacementIds.TrapCategoryId ? "t" : "l";

        private static string IdentityAndPosition(RoomContentAssignment value) =>
            value.AssignmentId + "|" + value.RoomInstanceId + "|" + value.CategoryId + "|" +
            value.OptionId + "|" + value.Sequence + "|" + value.RoomLocalPosition.X + "," +
            value.RoomLocalPosition.Y;

        private static int MaximumMaterializedRoomContentEnvelope(
            FloorSpatialConfiguration floor, IEnumerable<RoomSpatialDefinition> rooms)
        {
            RoomSpatialDefinition[] allowed = rooms.Where(value => value != null &&
                floor.AllowedRoomDefinitionIds.Contains(value.RoomDefinitionId)).ToArray();
            var maximum = new int[floor.FinalFloorSpaceCapacity + 1];
            for (int used = 1; used <= floor.FinalFloorSpaceCapacity; used++)
            foreach (RoomSpatialDefinition room in allowed)
            {
                int tiles = checked(room.GrossFootprint.Width * room.GrossFootprint.Height);
                if (tiles > used) continue;
                int assignments = checked(room.MonsterCapacity + room.TrapCapacity + room.LootCapacity);
                maximum[used] = Math.Max(maximum[used], maximum[used - tiles] + tiles + assignments);
            }
            return maximum.Max();
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
                    Records = new[]
                {
                    new RoomContentSpatialOccupancyRecord
                    {
                        CategoryId = category, OptionId = option,
                        OccupiedTileOffsets = offsets,
                        ShareableCategoryIds = Array.Empty<string>()
                    }
                }
            }, 256);
    }
}
#endif
