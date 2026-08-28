#if UNITY_EDITOR
using System.Text;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66DetachedCompleteSaveContractTests
    {
        [Test]
        public void CompleteSave_ParsesAndRoundTripsByteIdentically()
        {
            byte[] bytes = CompleteSave();
            var limits = Limits();

            DetachedCompleteSaveValidationResult result =
                DetachedCompleteSaveContract.ParseValidateFrozenSchemaSevenAndRoundTrip(bytes, limits);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.GetBytes(), Is.EqualTo(bytes));
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, limits).IsValid,
                Is.False);
            Assert.That(SchemaSevenToEightUpgrade.TryPrepare(bytes, limits, out byte[] schemaEight),
                Is.True);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(schemaEight, limits).IsValid,
                Is.True);
        }

        [Test]
        public void CompleteSave_RejectsCaseAmbiguousReservedMember()
        {
            string text = Encoding.UTF8.GetString(CompleteSave()).Replace("\"spatialFloors\":[]",
                "\"SpatialFloors\":[],\"spatialFloors\":[]");

            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaSevenAndRoundTrip(
                Encoding.UTF8.GetBytes(text), Limits()).IsValid, Is.False);
        }

        [Test]
        public void CompleteSave_RejectsMarkerOwnershipMismatch()
        {
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaSevenAndRoundTrip(
                CompleteSave(), Limits(), "gd66-" + new string('3', 64),
                new string('2', 64)).IsValid, Is.False);
        }

        [Test]
        public void CompleteSave_LaterCanonicalMutationRemainsSelfValidWithoutHistoricalHash()
        {
            string initial = Encoding.UTF8.GetString(CompleteSave());
            string mutated = initial.Replace("\"primary\":{",
                "\"primary\":{\"futureAudit\":{\"sequence\":2},");

            DetachedCompleteSaveValidationResult result =
                DetachedCompleteSaveContract.ParseValidateFrozenSchemaSevenAndRoundTrip(
                    Encoding.UTF8.GetBytes(mutated), Limits());

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.GetBytes(), Is.EqualTo(Encoding.UTF8.GetBytes(mutated)));
        }

        [Test]
        public void CompleteSave_UnknownNestedDuplicateIsRejected()
        {
            string initial = Encoding.UTF8.GetString(CompleteSave());
            string malformed = initial.Replace("\"primary\":{",
                "\"primary\":{\"futureAudit\":{\"value\":1,\"Value\":2},");

            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaSevenAndRoundTrip(
                Encoding.UTF8.GetBytes(malformed), Limits()).IsValid, Is.False);
        }

        [Test]
        public void FrozenSchemaSevenUpgrade_IsDeterministicAndProducesStrictSchemaEight()
        {
            Assert.That(SchemaSevenToEightUpgrade.TryPrepare(CompleteSave(), Limits(),
                out byte[] first), Is.True);
            Assert.That(SchemaSevenToEightUpgrade.TryPrepare(CompleteSave(), Limits(),
                out byte[] second), Is.True);
            CollectionAssert.AreEqual(first, second);
            Assert.That(Encoding.UTF8.GetString(first), Does.Contain("\"schemaVersion\":8"));
            Assert.That(Encoding.UTF8.GetString(first), Does.Contain(
                "\"structuralLifecycleAndOwnership\":{\"Floors\":[],\"ReturnedContents\":[]}"));
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(first, Limits()).IsValid,
                Is.True);
        }

        [Test]
        public void FrozenSchemaSevenUpgrade_RejectsNoncanonicalSource()
        {
            byte[] malformed = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(CompleteSave()) + " ");
            Assert.That(SchemaSevenToEightUpgrade.TryPrepare(malformed, Limits(), out _), Is.False);
        }

        [Test]
        public void FrozenSchemaSevenUpgrade_PreservesPopulatedCanonicalSpatialMembersAndAssignments()
        {
            const string members = "\"mvpRoomSlotAssignments\":{\"Rooms\":[" +
                "{\"FloorIndex\":0,\"RoomIndex\":0,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[\"placement.option.monster.skeleton\"]," +
                "\"TrapOptionIds\":[\"placement.option.trap.spike\"]," +
                "\"LootNodeOptionIds\":[\"placement.option.loot_node.basic\"]}," +
                "{\"FloorIndex\":0,\"RoomIndex\":1,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[],\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}]," +
                "\"NextRevision\":4},\"futurePrimary\":{\"value\":1}";
            Gd66DetachedSpatialMigrationTransactionTests.SemanticFixtureExecution fixture =
                Gd66DetachedSpatialMigrationTransactionTests.RunPopulatedSemanticFixture(
                    "schema-8-populated-preservation", 6, members);
            byte[] schemaSeven = fixture.Attempt.Candidate.GetBytes();
            DetachedCompleteSaveValidationResult before =
                DetachedCompleteSaveContract.ParseValidateFrozenSchemaSevenAndRoundTrip(schemaSeven,
                    fixture.Limits);
            Assert.That(before.IsValid, Is.True);
            Assert.That(SchemaSevenToEightUpgrade.TryPrepare(schemaSeven, fixture.Limits,
                out byte[] schemaEight), Is.True);
            DetachedCompleteSaveValidationResult after =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(schemaEight, fixture.Limits);
            Assert.That(after.IsValid, Is.True);

            SpatialContractResult<CanonicalSpatialSaveSerializer.SerializedMembers> beforeMembers =
                CanonicalSpatialSaveSerializer.SerializeFrozenSchemaSevenMembers(before.State, fixture.Limits);
            SpatialContractResult<CanonicalSpatialSaveSerializer.SerializedMembers> afterMembers =
                CanonicalSpatialSaveSerializer.SerializeMembers(after.State, fixture.Limits);
            CollectionAssert.AreEqual(beforeMembers.Value.Authority, afterMembers.Value.Authority);
            CollectionAssert.AreEqual(beforeMembers.Value.Floors, afterMembers.Value.Floors);
            Assert.That(after.State.LifecycleAndOwnership.ReturnedContents, Is.Empty);
            Assert.That(after.State.LifecycleAndOwnership.Floors.All(value =>
                value.NextNativeEdgeOrdinal == 0), Is.True);
            CollectionAssert.AreEqual(before.State.Floors.SelectMany(value =>
                value.RoomContents.Assignments).Select(value => value.AssignmentId).ToArray(),
                after.State.Floors.SelectMany(value => value.RoomContents.Assignments)
                    .Select(value => value.AssignmentId).ToArray());
        }

        [Test]
        public void FrozenSchemaSevenUpgrade_PreservesIssuedNativeRoomIdentityAndAdvancesHighWater()
        {
            const string members = "\"mvpRoomSlotAssignments\":{\"Rooms\":[" +
                "{\"FloorIndex\":0,\"RoomIndex\":0,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[],\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}," +
                "{\"FloorIndex\":0,\"RoomIndex\":1,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[\"placement.option.monster.skeleton\"]," +
                "\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}],\"NextRevision\":3}";
            Gd66DetachedSpatialMigrationTransactionTests.SemanticFixtureExecution fixture =
                Gd66DetachedSpatialMigrationTransactionTests.RunPopulatedSemanticFixture(
                    "schema-8-native-identity", 6, members);
            DetachedCanonicalSpatialSaveState state = fixture.State;
            SavedSpatialFloor floor = state.Floors[0];
            RoomSpatialInstance room = floor.Layout.Rooms.Last();
            TileCoordinate originalAnchor = room.Anchor;
            CardinalOrientation originalOrientation = room.Orientation;
            string oldRoom = room.RoomInstanceId;
            string nativeRoom = "compat.floor.00.room.player.0000";
            FloorRouteNode node = floor.Layout.Nodes.Single(value => value.RoomInstanceId == oldRoom);
            string oldNode = node.NodeId;
            room.RoomInstanceId = nativeRoom;
            node.RoomInstanceId = nativeRoom; node.NodeId = nativeRoom + ".node";
            FloorRouteEdge incoming = floor.Layout.Edges.Single(value => value.DestinationNodeId == oldNode);
            FloorRouteEdge terminal = floor.Layout.Edges.Single(value => value.SourceNodeId == oldNode);
            incoming.DestinationNodeId = node.NodeId; incoming.EdgeId = nativeRoom + ".edge.incoming";
            terminal.SourceNodeId = node.NodeId; terminal.EdgeId = nativeRoom + ".edge.terminal";
            foreach (RoomContentAssignment assignment in floor.RoomContents.Assignments.Where(value =>
                value.RoomInstanceId == oldRoom)) assignment.RoomInstanceId = nativeRoom;
            string[] assignmentEvidence = floor.RoomContents.Assignments.Where(value =>
                value.RoomInstanceId == nativeRoom).Select(value => value.AssignmentId + ":" + value.Sequence)
                .ToArray();
            floor.RoomContents.RoomSemantics.Single(value => value.RoomInstanceId == oldRoom)
                .RoomInstanceId = nativeRoom;
            state.LifecycleAndOwnership = null;
            DetachedWholeSaveResult rebuilt = DetachedWholeSaveCandidateSerializer.BuildPrepared(
                fixture.Classification, state, fixture.Limits, fixture.WholeLimits);
            Assert.That(rebuilt.IsSuccess, Is.True, rebuilt.Reason);
            Assert.That(SchemaSevenToEightUpgrade.TryPrepare(rebuilt.Candidate.GetBytes(), fixture.Limits,
                out byte[] schemaEight), Is.True);
            DetachedCompleteSaveValidationResult upgraded =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(schemaEight, fixture.Limits);
            Assert.That(upgraded.IsValid, Is.True);
            Assert.That(upgraded.State.Floors[0].Layout.Rooms.Any(value =>
                value.RoomInstanceId == nativeRoom), Is.True);
            RoomSpatialInstance upgradedRoom = upgraded.State.Floors[0].Layout.Rooms.Single(value =>
                value.RoomInstanceId == nativeRoom);
            Assert.That(upgradedRoom.Anchor, Is.EqualTo(originalAnchor));
            Assert.That(upgradedRoom.Orientation, Is.EqualTo(originalOrientation));
            Assert.That(upgraded.State.Floors[0].Layout.Nodes.Any(value =>
                value.NodeId == nativeRoom + ".node"), Is.True);
            Assert.That(upgraded.State.Floors[0].Layout.Edges.Select(value => value.EdgeId),
                Does.Contain(nativeRoom + ".edge.incoming").And.Contain(nativeRoom + ".edge.terminal"));
            Assert.That(upgraded.State.LifecycleAndOwnership.Floors[0].NextNativeRoomOrdinal,
                Is.EqualTo(1));
            Assert.That(upgraded.State.LifecycleAndOwnership.Floors[0].NextNativeEdgeOrdinal,
                Is.EqualTo(0));
            Assert.That(upgraded.State.LifecycleAndOwnership.ReturnedContents, Is.Empty);
            CollectionAssert.AreEqual(assignmentEvidence, upgraded.State.Floors[0].RoomContents.Assignments
                .Where(value => value.RoomInstanceId == nativeRoom)
                .Select(value => value.AssignmentId + ":" + value.Sequence).ToArray());
            DetachedCompleteSaveValidationResult contextual =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(schemaEight, fixture.CurrentContext);
            Assert.That(contextual.IsValid, Is.True, contextual.Reason);
            CompatibilitySelectionResult<CanonicalLayoutContractSelection> selected =
                fixture.Compatibility.SelectContract(CanonicalSaveSchemaVersions.CurrentWritableTarget);
            Assert.That(selected.Success, Is.True, selected.Code);
            var profile = new SaveSpatialMigrationLimitsProfile(
                Gd66DetachedSpatialMigrationTransactionTests.RawLimitsForCoordinator,
                fixture.Limits, fixture.WholeLimits);
            DetachedCanonicalSaveSessionResult opened = DetachedCanonicalSaveSession.Open(schemaEight,
                fixture.CurrentContext, profile);
            Assert.That(opened.IsSuccess, Is.True, opened.Reason);
            Assert.That(CanonicalMvpRouteProjection.TryPublishValidated(contextual, fixture.Production,
                out SaveData published, out string reason), Is.True, reason);
            Assert.That(published.validatedCanonicalSpatialState.Floors[0].Layout.Rooms.Any(value =>
                value.RoomInstanceId == nativeRoom), Is.True);
        }

        private static CanonicalSpatialSerializationLimits Limits() =>
            new CanonicalSpatialSerializationLimits(new SpatialSerializedInputLimits(100000, 10000,
                1000, 10000, 20), new CanonicalSpatialSaveWorkloadLimits(1000, 1000));

        private static byte[] CompleteSave() => Encoding.UTF8.GetBytes(
            "{\"schema\":\"save_root\",\"schemaVersion\":7,\"primary\":{" +
            "\"canonicalSpatialAuthority\":{\"CanonicalLayoutContractVersion\":1," +
            "\"CreationKind\":2,\"MigrationTransactionId\":\"gd66-" + new string('1', 64) +
            "\",\"MigrationDescriptorFingerprint\":\"" + new string('2', 64) + "\"}," +
            "\"spatialFloors\":[]}}");

        [Test]
        public void CandidateInvalidReason_UsesTransactionRegistry()
        {
            Assert.That(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason,
                Is.EqualTo("gd66.transaction.candidate_invalid"));
        }
    }
}
#endif
