#if UNITY_EDITOR
using System.Text;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
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
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            byte[] schemaSeven = fixture.Result.Attempt.Candidate.GetBytes();
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
                CanonicalSpatialSaveSerializer.SerializeMembers(before.State, fixture.Limits);
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
