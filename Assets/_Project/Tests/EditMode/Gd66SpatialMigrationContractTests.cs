#if UNITY_EDITOR

using System;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66SpatialMigrationContractTests
    {
        private const string H0 = "0000000000000000000000000000000000000000000000000000000000000000";
        private const string H1 = "1111111111111111111111111111111111111111111111111111111111111111";
        private static readonly SpatialSerializedInputLimits JsonLimits =
            new SpatialSerializedInputLimits(100000, 10000, 1000, 10000, 20);
        private static readonly CanonicalSpatialSerializationLimits SaveLimits =
            new CanonicalSpatialSerializationLimits(JsonLimits,
                new CanonicalSpatialSaveWorkloadLimits(1000, 1000));

        [Test]
        public void ContractIdentities_AreExact()
        {
            Assert.AreEqual("gd66.serializer.canonical_spatial_save",
                SpatialMigrationContractIdentity.CanonicalSerializerId);
            Assert.AreEqual(1, SpatialMigrationContractIdentity.CanonicalSerializerVersion);
            Assert.AreEqual(1, SpatialMigrationContractIdentity.AuthorityMarkerContractVersion);
            Assert.AreEqual(1, SpatialMigrationContractIdentity.MigrationContractVersion);
            Assert.AreEqual(1, SpatialMigrationContractIdentity.JournalSchemaVersion);
        }

        [TestCase(CanonicalSpatialCreationKind.NativeCanonical)]
        [TestCase(CanonicalSpatialCreationKind.Migrated)]
        public void EmptyCanonicalSave_RoundTripsCanonicalBytes(CanonicalSpatialCreationKind kind)
        {
            string identity = SpatialMigrationTransactionIdentity.ComputeIdentity(H0, H1);
            var state = new DetachedCanonicalSpatialSaveState
            {
                Authority = new CanonicalSpatialAuthorityMarker
                {
                    CanonicalLayoutContractVersion = 1,
                    CreationKind = kind,
                    MigrationTransactionId = kind == CanonicalSpatialCreationKind.Migrated
                        ? SpatialMigrationTransactionIdentity.CreateTransactionId(identity) : null,
                    MigrationDescriptorFingerprint = kind == CanonicalSpatialCreationKind.Migrated ? H1 : null
                },
                Floors = Array.Empty<SavedSpatialFloor>()
            };

            SpatialContractResult<byte[]> first = CanonicalSpatialSaveSerializer.Serialize(state, SaveLimits);
            Assert.IsTrue(first.IsValid);
            Assert.AreNotEqual(0xef, first.Value[0]);
            string json = Encoding.UTF8.GetString(first.Value);
            Assert.IsFalse(json.EndsWith("\n", StringComparison.Ordinal));
            Assert.IsFalse(json.Contains("Candidate"));
            SpatialContractResult<DetachedCanonicalSpatialSaveState> parsed =
                CanonicalSpatialSaveSerializer.Parse(first.Value, SaveLimits);
            Assert.IsTrue(parsed.IsValid);
            CollectionAssert.AreEqual(first.Value,
                CanonicalSpatialSaveSerializer.Serialize(parsed.Value, SaveLimits).Value);
        }

        [Test]
        public void CanonicalSaveParser_RejectsUnknownDuplicateAndBoundaryWhitespace()
        {
            Assert.IsFalse(CanonicalSpatialSaveSerializer.Parse(
                Encoding.UTF8.GetBytes("{\"Unknown\":1,\"Floors\":[]}"), SaveLimits).IsValid);
            CollectionAssert.Contains(CanonicalSpatialSaveSerializer.Parse(
                Encoding.UTF8.GetBytes("{\"Authority\":null,\"Authority\":null,\"Floors\":[]}"),
                SaveLimits).Issues, SpatialContractIssue.DuplicateField);
            CollectionAssert.Contains(CanonicalSpatialSaveSerializer.Parse(
                Encoding.UTF8.GetBytes(" {\"Authority\":null,\"Floors\":[]}"), SaveLimits).Issues,
                SpatialContractIssue.LeadingOrTrailingWhitespace);
        }

        [Test]
        public void Descriptor_CanonicalizesValidationRecordOrderAndBindsFingerprint()
        {
            SpatialMigrationInputDescriptor reversed = Descriptor(new[]
            {
                new SpatialValidationInputHash("validation.z", H1),
                new SpatialValidationInputHash("validation.a", H0)
            });
            SpatialMigrationInputDescriptor ordered = Descriptor(new[]
            {
                new SpatialValidationInputHash("validation.a", H0),
                new SpatialValidationInputHash("validation.z", H1)
            });
            byte[] reversedBytes = SpatialMigrationDescriptorContracts.Serialize(reversed, JsonLimits).Value;
            byte[] orderedBytes = SpatialMigrationDescriptorContracts.Serialize(ordered, JsonLimits).Value;
            CollectionAssert.AreEqual(reversedBytes, orderedBytes);
            Assert.AreEqual(SpatialContractSha256.Compute(reversedBytes),
                SpatialMigrationDescriptorContracts.ComputeInputFingerprint(reversed, JsonLimits));
            Assert.IsTrue(SpatialMigrationDescriptorContracts.Parse(reversedBytes, JsonLimits).IsValid);
        }

        [Test]
        public void Descriptor_RejectsDuplicateIdsAndNoncanonicalHashes()
        {
            Assert.IsFalse(SpatialMigrationDescriptorContracts.Serialize(Descriptor(new[]
            {
                new SpatialValidationInputHash("validation.a", H0),
                new SpatialValidationInputHash("validation.a", H1)
            }), JsonLimits).IsValid);
            Assert.IsFalse(SpatialContractSha256.IsCanonical(new string('A', 64)));
            Assert.IsFalse(SpatialContractSha256.IsCanonical(H0.Substring(1)));
        }

        [Test]
        public void TransactionIdentity_UsesExactCanonicalObjectAndCompactId()
        {
            byte[] bytes = SpatialMigrationTransactionIdentity.CanonicalIdentityBytes(H0, H1);
            Assert.AreEqual("{\"OriginalPayloadSha256\":\"" + H0 +
                "\",\"InputFingerprintSha256\":\"" + H1 + "\"}", Encoding.UTF8.GetString(bytes));
            string id = SpatialMigrationTransactionIdentity.CreateTransactionId(
                SpatialContractSha256.Compute(bytes));
            Assert.AreEqual(69, id.Length);
            Assert.IsTrue(id.StartsWith("gd66-", StringComparison.Ordinal));
            Assert.IsTrue(SpatialMigrationTransactionIdentity.IsCanonicalTransactionId(id));
            Assert.IsFalse(SpatialMigrationTransactionIdentity.IsCanonicalTransactionId(
                "GD66-" + id.Substring(5)));
        }

        private static SpatialMigrationInputDescriptor Descriptor(SpatialValidationInputHash[] hashes) =>
            new SpatialMigrationInputDescriptor(H0, 6, SpatialRawEnvelopeClassification.WrappedSaveRoot,
                7, 1, 1, "compat.profile.migration.schema_1_6_to_7.contract_1", 1, H1,
                "compat.geometry.r1-r2", 1, H0, H1, H0, hashes, H1,
                SpatialMigrationContractIdentity.CanonicalSerializerId, 1);
    }
}

#endif
