#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66DetachedWholeSaveCandidateTests
    {
        private static readonly SpatialSerializedInputLimits JsonLimits =
            new SpatialSerializedInputLimits(100000, 10000, 1000, 10000, 20);
        private static readonly CanonicalSpatialSerializationLimits SpatialLimits =
            new CanonicalSpatialSerializationLimits(JsonLimits, new CanonicalSpatialSaveWorkloadLimits(1000, 1000));
        private static readonly DetachedWholeSaveLimits WholeLimits =
            new DetachedWholeSaveLimits(100000, 50000, 100, 50000);

        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
        public void WrappedSchemas_PreserveRecognizedAndUnknownValues(int schema)
        {
            string json = "{\"rootBefore\":[1,{\"x\":true}],\"schema\":\"save_root\",\"schemaVersion\":" + schema +
                ",\"primary\":{\"saveVersion\":null,\"dungeonLayout\":{\"Slots\":[]},\"unknown\":1.00},\"rootAfter\":false}";
            RawSavePayloadClassification classification = Classify(json);
            DetachedWholeSaveResult result = DetachedWholeSaveCandidateSerializer.BuildPrepared(
                classification, EmptySpatial(), SpatialLimits, WholeLimits);
            Assert.That(result.IsSuccess, Is.True);
            string candidate = Encoding.UTF8.GetString(result.Candidate.GetBytes());
            Assert.That(candidate, Does.StartWith("{\"schema\":\"save_root\",\"schemaVersion\":7,\"primary\":{"));
            Assert.That(candidate, Does.Contain("\"saveVersion\":null"));
            Assert.That(candidate, Does.Contain("\"dungeonLayout\":{\"Slots\":[]}"));
            Assert.That(candidate, Does.Contain("\"unknown\":1.00"));
            Assert.That(candidate, Does.EndWith("\"rootBefore\":[1,{\"x\":true}],\"rootAfter\":false}"));
            Assert.That(result.Candidate.Sha256, Is.EqualTo(SpatialContractSha256.Compute(result.Candidate.GetBytes())));
        }

        [Test]
        public void PrettyCopiedValuesBecomeCanonicalWithoutChangingRecognizedOrUnknownEvidence()
        {
            const string json = "{\n  \"rootObject\": { \"number\": 1.00, \"items\": [ true, false ] },\n" +
                "  \"schema\": \"save_root\",\n  \"schemaVersion\": 6,\n  \"primary\": {\n" +
                "    \"dungeonLayout\": { \"Slots\": [ ] },\n" +
                "    \"unknownObject\": { \"message\": \"keep spaces \\\" and \\\\ here\", \"n\": 1.00 },\n" +
                "    \"unknownArray\": [ { \"x\": 1 }, 2 ]\n  },\n" +
                "  \"rootArray\": [ 1, { \"y\": 2 } ]\n}";
            RawSavePayloadClassification classification = Classify(json);
            RawSaveMemberEvidence recognized = classification.Members.Single(value =>
                value.Name == "dungeonLayout");
            byte[] recognizedBefore = recognized.GetRawValueBytes();
            byte[][] primaryUnknownBefore = classification.UnknownPrimaryMembers
                .Select(value => value.GetRawValueBytes()).ToArray();
            byte[][] rootUnknownBefore = classification.UnknownRootMembers
                .Select(value => value.GetRawValueBytes()).ToArray();

            DetachedWholeSaveResult result = DetachedWholeSaveCandidateSerializer.BuildPrepared(
                classification, EmptySpatial(), SpatialLimits, WholeLimits);

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            string candidate = Encoding.UTF8.GetString(result.Candidate.GetBytes());
            Assert.That(candidate, Does.Contain("\"dungeonLayout\":{\"Slots\":[]}"));
            Assert.That(candidate, Does.Contain(
                "\"unknownObject\":{\"message\":\"keep spaces \\\" and \\\\ here\",\"n\":1.00}"));
            Assert.That(candidate, Does.Contain("\"unknownArray\":[{\"x\":1},2]"));
            Assert.That(candidate, Does.Contain("\"rootObject\":{\"number\":1.00,\"items\":[true,false]}"));
            Assert.That(candidate, Does.Contain("\"rootArray\":[1,{\"y\":2}]"));
            Assert.That(recognized.GetRawValueBytes(), Is.EqualTo(recognizedBefore));
            for (int index = 0; index < primaryUnknownBefore.Length; index++)
                Assert.That(classification.UnknownPrimaryMembers[index].GetRawValueBytes(),
                    Is.EqualTo(primaryUnknownBefore[index]));
            for (int index = 0; index < rootUnknownBefore.Length; index++)
                Assert.That(classification.UnknownRootMembers[index].GetRawValueBytes(),
                    Is.EqualTo(rootUnknownBefore[index]));
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                result.Candidate.GetBytes(), SpatialLimits).IsValid, Is.True);
        }

        [Test]
        public void UnwrappedSchemaOne_RemainsHistoricalAndDoesNotAcquireCurrentDefaults()
        {
            DetachedWholeSaveResult result = DetachedWholeSaveCandidateSerializer.BuildPrepared(
                Classify("{\"saveVersion\":1,\"custom\":null}"), EmptySpatial(), SpatialLimits, WholeLimits);
            Assert.That(result.IsSuccess, Is.True);
            string candidate = Encoding.UTF8.GetString(result.Candidate.GetBytes());
            Assert.That(candidate, Does.Contain("\"saveVersion\":1,\"custom\":null"));
            Assert.That(candidate, Does.Not.Contain("contentVersion"));
            Assert.That(candidate, Does.Not.Contain("structureRuntime"));
        }

        [Test]
        public void ReservedUnknownMember_FailsWithoutCandidateBytes()
        {
            DetachedWholeSaveResult result = DetachedWholeSaveCandidateSerializer.BuildPrepared(
                Classify("{\"saveVersion\":1,\"spatialFloors\":[]}"), EmptySpatial(), SpatialLimits, WholeLimits);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Candidate, Is.Null);
            Assert.That(result.Reason, Is.EqualTo("gd66.payload.unknown_member_unpreservable"));
        }

        [Test]
        public void UnknownNonBmpMemberName_IsPreservedAsUtf8()
        {
            const string name = "unknown_\U0001F409";
            DetachedWholeSaveResult result = DetachedWholeSaveCandidateSerializer.BuildPrepared(
                Classify("{\"saveVersion\":1,\"" + name + "\":true}"), EmptySpatial(), SpatialLimits, WholeLimits);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(Encoding.UTF8.GetString(result.Candidate.GetBytes()), Does.Contain("\"" + name + "\":true"));
        }

        [Test]
        public void InvalidCandidate_UsesRegisteredTransactionReason()
        {
            DetachedWholeSaveResult result = DetachedWholeSaveCandidateSerializer.BuildPrepared(
                null, EmptySpatial(), SpatialLimits, WholeLimits);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo("gd66.transaction.candidate_invalid"));
        }

        [Test]
        public void GenericRuntimeFileSystem_FailsClosedForDirectoryDurability()
        {
            Assert.Throws<PlatformNotSupportedException>(() =>
                new RuntimeSpatialMigrationFileSystem().FlushDirectory(System.IO.Path.GetTempPath()));
        }

        private static DetachedCanonicalSpatialSaveState EmptySpatial() => new DetachedCanonicalSpatialSaveState
        {
            Authority = new CanonicalSpatialAuthorityMarker
            {
                CanonicalLayoutContractVersion = 1,
                CreationKind = CanonicalSpatialCreationKind.Migrated,
                MigrationTransactionId = "gd66-" + new string('1', 64),
                MigrationDescriptorFingerprint = new string('2', 64)
            },
            Floors = Array.Empty<SavedSpatialFloor>(),
            LifecycleAndOwnership = NativeStructuralIdentity.CreateInitialLifecycle(
                Array.Empty<SavedSpatialFloor>())
        };

        private static RawSavePayloadClassification Classify(string json) => RawSavePayloadClassifier.Classify(
            Encoding.UTF8.GetBytes(json), new RawSavePayloadClassificationLimits(100000, 32, 100, 100, 10000, 500000),
            new RawSaveEnvelopeVersionContract(1, 6), new RawLegacyBlankFloorContract(1,
                Enumerable.Range(0, 4).Select(i => new RawLegacyBlankFloorNodeContract(0, i, "slot." + i, "", "", 0)),
                true, true, new[] { "Nodes", "NextRevision" },
                new[] { "FloorIndex", "NodeIndex", "SlotId", "CategoryId", "OptionId", "Revision" }));
    }
}
#endif
