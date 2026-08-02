using System;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66SpatialMigrationContractTests
    {
        private const string H0="0000000000000000000000000000000000000000000000000000000000000000";
        private const string H1="1111111111111111111111111111111111111111111111111111111111111111";
        private static readonly SpatialSerializedInputLimits JsonLimits=new SpatialSerializedInputLimits(100000,10000,1000,10000,20);
        private static readonly CanonicalSpatialSerializationLimits SaveLimits=new CanonicalSpatialSerializationLimits(JsonLimits,new CanonicalSpatialSaveWorkloadLimits(1000,1000));

        [Test]
        public void ContractIdentities_AreExact()
        {
            Assert.AreEqual("gd66.serializer.canonical_spatial_save",SpatialMigrationContractIdentity.CanonicalSerializerId);
            Assert.AreEqual(1,SpatialMigrationContractIdentity.CanonicalSerializerVersion);Assert.AreEqual(1,SpatialMigrationContractIdentity.AuthorityMarkerContractVersion);Assert.AreEqual(1,SpatialMigrationContractIdentity.MigrationContractVersion);Assert.AreEqual(1,SpatialMigrationContractIdentity.JournalSchemaVersion);
        }

        [TestCase(CanonicalSpatialCreationKind.NativeCanonical)] [TestCase(CanonicalSpatialCreationKind.Migrated)]
        public void EmptyCanonicalSave_RoundTripsCanonicalBytes(CanonicalSpatialCreationKind kind)
        {
            string identity=SpatialMigrationTransactionIdentity.ComputeIdentity(H0,H1);var state=new DetachedCanonicalSpatialSaveState{Authority=new CanonicalSpatialAuthorityMarker{CanonicalLayoutContractVersion=1,CreationKind=kind,MigrationTransactionId=kind==CanonicalSpatialCreationKind.Migrated?SpatialMigrationTransactionIdentity.CreateTransactionId(identity):null,MigrationDescriptorFingerprint=kind==CanonicalSpatialCreationKind.Migrated?H1:null},Floors=Array.Empty<SavedSpatialFloor>()};
            var first=CanonicalSpatialSaveSerializer.Serialize(state,SaveLimits);Assert.IsTrue(first.IsValid);Assert.AreNotEqual(0xef,first.Value[0]);string json=Encoding.UTF8.GetString(first.Value);Assert.IsFalse(json.EndsWith("\n",StringComparison.Ordinal));Assert.IsFalse(json.Contains("Candidate"));
            var parsed=CanonicalSpatialSaveSerializer.Parse(first.Value,SaveLimits);Assert.IsTrue(parsed.IsValid);var second=CanonicalSpatialSaveSerializer.Serialize(parsed.Value,SaveLimits);CollectionAssert.AreEqual(first.Value,second.Value);
        }

        [Test]
        public void CanonicalSaveParser_RejectsShapeAndCanonicalViolations()
        {
            byte[] unknown=Encoding.UTF8.GetBytes("{\"Unknown\":1,\"Floors\":[]}");Assert.IsFalse(CanonicalSpatialSaveSerializer.Parse(unknown,SaveLimits).IsValid);
            byte[] duplicate=Encoding.UTF8.GetBytes("{\"Authority\":null,\"Authority\":null,\"Floors\":[]}");CollectionAssert.Contains(CanonicalSpatialSaveSerializer.Parse(duplicate,SaveLimits).Issues,SpatialContractIssue.DuplicateField);
            byte[] whitespace=Encoding.UTF8.GetBytes(" {\"Authority\":null,\"Floors\":[]}");CollectionAssert.Contains(CanonicalSpatialSaveSerializer.Parse(whitespace,SaveLimits).Issues,SpatialContractIssue.LeadingOrTrailingWhitespace);
        }

        [Test]
        public void Descriptor_IsOrderIndependentAndFingerprintBound()
        {
            var a=Descriptor(new[]{new SpatialValidationInputHash("validation.z",H1),new SpatialValidationInputHash("validation.a",H0)});var b=Descriptor(new[]{new SpatialValidationInputHash("validation.a",H0),new SpatialValidationInputHash("validation.z",H1)});
            var ab=SpatialMigrationDescriptorContracts.Serialize(a,JsonLimits);var bb=SpatialMigrationDescriptorContracts.Serialize(b,JsonLimits);Assert.IsTrue(ab.IsValid);CollectionAssert.AreEqual(ab.Value,bb.Value);Assert.AreEqual(SpatialContractSha256.Compute(ab.Value),SpatialMigrationDescriptorContracts.ComputeInputFingerprint(a,JsonLimits));
            Assert.IsTrue(SpatialMigrationDescriptorContracts.Parse(ab.Value,JsonLimits).IsValid);
            var changed=Descriptor(new[]{new SpatialValidationInputHash("validation.a",H1)});Assert.AreNotEqual(SpatialMigrationDescriptorContracts.ComputeInputFingerprint(a,JsonLimits),SpatialMigrationDescriptorContracts.ComputeInputFingerprint(changed,JsonLimits));
        }

        [Test]
        public void Descriptor_RejectsDuplicateIdsAndNoncanonicalHashes()
        {
            Assert.IsFalse(SpatialMigrationDescriptorContracts.Serialize(Descriptor(new[]{new SpatialValidationInputHash("validation.a",H0),new SpatialValidationInputHash("validation.a",H1)}),JsonLimits).IsValid);
            Assert.IsFalse(SpatialContractSha256.IsCanonical(new string('A',64)));Assert.IsFalse(SpatialContractSha256.IsCanonical(H0.Substring(1)));
        }

        [Test]
        public void TransactionIdentity_UsesExactCanonicalObjectAndCompactId()
        {
            byte[] bytes=SpatialMigrationTransactionIdentity.CanonicalIdentityBytes(H0,H1);Assert.AreEqual("{\"OriginalPayloadSha256\":\""+H0+"\",\"InputFingerprintSha256\":\""+H1+"\"}",Encoding.UTF8.GetString(bytes));string id=SpatialMigrationTransactionIdentity.CreateTransactionId(SpatialContractSha256.Compute(bytes));Assert.AreEqual(69,id.Length);Assert.IsTrue(id.StartsWith("gd66-",StringComparison.Ordinal));Assert.IsTrue(SpatialMigrationTransactionIdentity.IsCanonicalTransactionId(id));Assert.IsFalse(SpatialMigrationTransactionIdentity.IsCanonicalTransactionId("GD66-"+id.Substring(5)));
        }

        [Test]
        public void SidecarNames_AreExactAndPure()
        {
            string tx=SpatialMigrationTransactionIdentity.CreateTransactionId(H1);var r=SpatialMigrationSidecarPaths.Derive("save.primary.json",tx);Assert.IsTrue(r.IsValid);Assert.AreEqual("save.primary."+tx+".journal.json",r.Value.Journal);Assert.AreEqual("save.primary."+tx+".original.bak",r.Value.OriginalBackup);Assert.AreEqual("save.primary."+tx+".candidate.tmp",r.Value.CandidateStaging);Assert.AreEqual("save.primary."+tx+".finalized",r.Value.FinalizedReceipt);
            Assert.IsFalse(SpatialMigrationSidecarPaths.Derive("../save.json",tx).IsValid);Assert.IsFalse(SpatialMigrationSidecarPaths.Derive("C:save.json",tx).IsValid);Assert.IsFalse(SpatialMigrationSidecarPaths.Derive(new string('a',81)+".json",tx).IsValid);
        }

        [Test]
        public void Journal_RoundTripsEveryValidStage_AndBindsIdentityAndNames()
        {
            foreach(SpatialMigrationJournalStage stage in Enum.GetValues(typeof(SpatialMigrationJournalStage)))
            {var j=Journal(stage);var bytes=SpatialMigrationJournalContracts.Serialize(j,JsonLimits);Assert.IsTrue(bytes.IsValid,stage.ToString());Assert.IsTrue(SpatialMigrationJournalContracts.Parse(bytes.Value,JsonLimits).IsValid,stage.ToString());}
        }

        [Test]
        public void JournalTransitions_AreExplicitAndTerminal()
        {
            Assert.IsTrue(SpatialMigrationJournalContracts.IsAllowedTransition(SpatialMigrationJournalStage.DescriptorPinned,SpatialMigrationJournalStage.BackupVerified));Assert.IsTrue(SpatialMigrationJournalContracts.IsAllowedTransition(SpatialMigrationJournalStage.BackupVerified,SpatialMigrationJournalStage.OriginalRestored));Assert.IsTrue(SpatialMigrationJournalContracts.IsAllowedTransition(SpatialMigrationJournalStage.DurableVerified,SpatialMigrationJournalStage.Finalized));Assert.IsFalse(SpatialMigrationJournalContracts.IsAllowedTransition(SpatialMigrationJournalStage.Finalized,SpatialMigrationJournalStage.OriginalRestored));Assert.IsFalse(SpatialMigrationJournalContracts.IsAllowedTransition(SpatialMigrationJournalStage.OriginalRestored,SpatialMigrationJournalStage.DescriptorPinned));Assert.IsFalse(SpatialMigrationJournalContracts.IsAllowedTransition(SpatialMigrationJournalStage.DescriptorPinned,SpatialMigrationJournalStage.CandidateVerified));
        }

        [Test]
        public void WorkloadLimits_EnforceExactByteBoundary()
        {
            byte[] bytes=SpatialMigrationDescriptorContracts.Serialize(Descriptor(Array.Empty<SpatialValidationInputHash>()),JsonLimits).Value;var exact=new SpatialSerializedInputLimits(bytes.Length,1000,100,1000,10);Assert.IsTrue(SpatialMigrationDescriptorContracts.Parse(bytes,exact).IsValid);var shortLimit=new SpatialSerializedInputLimits(bytes.Length-1,1000,100,1000,10);CollectionAssert.Contains(SpatialMigrationDescriptorContracts.Parse(bytes,shortLimit).Issues,SpatialContractIssue.InputByteLimitExceeded);
        }

        private static SpatialMigrationInputDescriptor Descriptor(SpatialValidationInputHash[] hashes)=>new SpatialMigrationInputDescriptor(H0,6,SpatialRawEnvelopeClassification.WrappedSaveRoot,7,1,1,"compat.profile.migration.schema_1_6_to_7.contract_1",1,H1,"compat.geometry.r1-r2",1,H0,H1,H0,hashes,H1,SpatialMigrationContractIdentity.CanonicalSerializerId,1);
        private static SpatialMigrationJournal Journal(SpatialMigrationJournalStage stage)
        {var d=Descriptor(Array.Empty<SpatialValidationInputHash>());string fp=SpatialMigrationDescriptorContracts.ComputeInputFingerprint(d,JsonLimits);string identity=SpatialMigrationTransactionIdentity.ComputeIdentity(d.OriginalPayloadSha256,fp);string tx=SpatialMigrationTransactionIdentity.CreateTransactionId(identity);var n=SpatialMigrationSidecarPaths.Derive("save.json",tx).Value;bool backup=stage!=SpatialMigrationJournalStage.DescriptorPinned;bool candidate=stage==SpatialMigrationJournalStage.CandidateVerified||stage==SpatialMigrationJournalStage.Replaced||stage==SpatialMigrationJournalStage.DurableVerified||stage==SpatialMigrationJournalStage.Finalized;string receipt=stage==SpatialMigrationJournalStage.Finalized?n.FinalizedReceipt:null;return new SpatialMigrationJournal(1,d,fp,identity,tx,n.Journal,n.OriginalBackup,n.CandidateStaging,receipt,d.OriginalPayloadSha256,backup?d.OriginalPayloadSha256:null,candidate?H1:null,stage);}
    }
}
