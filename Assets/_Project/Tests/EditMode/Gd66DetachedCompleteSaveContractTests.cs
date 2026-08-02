#if UNITY_EDITOR
using System.Text;
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
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, limits);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.GetBytes(), Is.EqualTo(bytes));
        }

        [Test]
        public void CompleteSave_RejectsCaseAmbiguousReservedMember()
        {
            string text = Encoding.UTF8.GetString(CompleteSave()).Replace("\"spatialFloors\":[]",
                "\"SpatialFloors\":[],\"spatialFloors\":[]");

            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                Encoding.UTF8.GetBytes(text), Limits()).IsValid, Is.False);
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
