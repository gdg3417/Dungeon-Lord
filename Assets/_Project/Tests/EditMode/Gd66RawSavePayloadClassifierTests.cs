#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests
{
    public sealed class Gd66RawSavePayloadClassifierTests
    {
        private static RawSavePayloadClassificationLimits Limits(int bytes = 10000, int depth = 32,
            int members = 100, int elements = 100, int strings = 1000, int diagnostics = 1, int work = 50000) =>
            new RawSavePayloadClassificationLimits(bytes, depth, members, elements, strings, diagnostics, work);
        private static readonly RawSaveEnvelopeVersionContract Versions = new RawSaveEnvelopeVersionContract(1, 6);
        private static RawSavePayloadClassification Classify(string json, RawSavePayloadClassificationLimits? limits = null) =>
            RawSavePayloadClassifier.Classify(Encoding.UTF8.GetBytes(json), limits ?? Limits(), Versions);

        [Test]
        public void WrappedAndUnwrapped_AreDistinguishedWithoutInventingUnwrappedVersion()
        {
            RawSavePayloadClassification wrapped = Classify("{\"schema\":\"save_root\",\"schemaVersion\":1,\"primary\":{\"saveVersion\":null}}");
            RawSavePayloadClassification unwrapped = Classify("{\"saveVersion\":1}");
            Assert.That(wrapped.Envelope, Is.EqualTo(RawSaveEnvelopeKind.WrappedSaveRoot));
            Assert.That(wrapped.SchemaVersion, Is.EqualTo(1));
            Assert.That(unwrapped.Envelope, Is.EqualTo(RawSaveEnvelopeKind.UnwrappedSaveData));
            Assert.That(unwrapped.SchemaVersion.HasValue, Is.False);
        }

        [TestCase("{\"unknown\":1}", "gd66.payload.ambiguous_envelope")]
        [TestCase("[]", "gd66.payload.ambiguous_envelope")]
        [TestCase("{\"schemaVersion\":1,\"primary\":{}}", "gd66.payload.missing_schema")]
        [TestCase("{\"schema\":\"wrong\",\"schemaVersion\":1,\"primary\":{}}", "gd66.payload.invalid_schema")]
        [TestCase("{\"schema\":\"save_root\",\"primary\":{}}", "gd66.payload.missing_schema_version")]
        [TestCase("{\"schema\":\"save_root\",\"schemaVersion\":1.5,\"primary\":{}}", "gd66.payload.nonintegral_schema_version")]
        [TestCase("{\"schema\":\"save_root\",\"schemaVersion\":0,\"primary\":{}}", "gd66.payload.unsupported_legacy_version")]
        [TestCase("{\"schema\":\"save_root\",\"schemaVersion\":7,\"primary\":{}}", "gd66.payload.newer_than_application")]
        [TestCase("{\"schema\":\"save_root\",\"schemaVersion\":1}", "gd66.payload.missing_primary")]
        [TestCase("{\"schema\":\"save_root\",\"schemaVersion\":1,\"primary\":null}", "gd66.payload.null_primary")]
        public void EnvelopeFailures_HavePinnedReasons(string json, string reason)
        { Assert.That(Classify(json).FailureReason, Is.EqualTo(reason)); }

        [TestCase("\"text\"")]
        [TestCase("[]")]
        [TestCase("1")]
        [TestCase("true")]
        public void NonObjectPrimary_UsesClarifiedReason(string primary)
        {
            Assert.That(Classify("{\"schema\":\"save_root\",\"schemaVersion\":1,\"primary\":" + primary + "}").FailureReason,
                Is.EqualTo(RawSavePayloadClassifier.InvalidPrimaryReason));
        }

        [TestCase("{\"saveVersion\":01}")]
        [TestCase("{\"saveVersion\":1.}")]
        [TestCase("{\"saveVersion\":1e}")]
        [TestCase("{\"saveVersion\":\"\\x\"}")]
        [TestCase("{\"saveVersion\":\"\\uD800\"}")]
        [TestCase("{\"saveVersion\":\"\\uDC00\"}")]
        [TestCase("{\"saveVersion\":1}junk")]
        [TestCase("{\"saveVersion\":1,\"saveVersion\":2}")]
        [TestCase("{\"saveVersion\":{\"x\":1,\"x\":2}}")]
        public void MalformedOrDuplicateJson_IsUnreadable(string json)
        { Assert.That(Classify(json).FailureReason, Is.EqualTo(RawSavePayloadClassifier.UnreadableReason)); }

        [Test]
        public void Utf8BomAndMalformedUtf8_AreRejected()
        {
            Assert.That(RawSavePayloadClassifier.Classify(new byte[] { 0xef, 0xbb, 0xbf, (byte)'{' , (byte)'}' }, Limits(), Versions).IsSuccess, Is.False);
            Assert.That(RawSavePayloadClassifier.Classify(new byte[] { (byte)'{', (byte)'\"', 0xc0, 0xaf, (byte)'\"', (byte)':', (byte)'1', (byte)'}' }, Limits(), Versions).IsSuccess, Is.False);
        }

        [Test]
        public void WhitespaceEscapesAndSurrogatePair_AreAccepted()
        {
            RawSavePayloadClassification result = Classify(" \n { \"saveVersion\" : 1, \"contentVersion\" : \"a\\n\\uD83D\\uDE00\" } \t");
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Members.Single(x => x.Name == "contentVersion").Kind, Is.EqualTo(RawJsonValueKind.String));
        }

        [Test]
        public void RecognizedEvidence_PreservesAbsentNullValueSpanAndOwnership()
        {
            byte[] input = Encoding.UTF8.GetBytes("{\"saveVersion\":null,\"contentVersion\":\"v\"}");
            RawSavePayloadClassification result = RawSavePayloadClassifier.Classify(input, Limits(), Versions);
            RawSaveMemberEvidence absent = result.Members.Single(x => x.Name == "totalTicks");
            RawSaveMemberEvidence nil = result.Members.Single(x => x.Name == "saveVersion");
            RawSaveMemberEvidence value = result.Members.Single(x => x.Name == "contentVersion");
            Assert.That(absent.State, Is.EqualTo(RawSaveMemberState.Absent));
            Assert.That(nil.State, Is.EqualTo(RawSaveMemberState.Null));
            Assert.That(value.State, Is.EqualTo(RawSaveMemberState.NonNull));
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("\"v\""), value.GetRawValueBytes());
            int offset = value.ByteOffset; input[offset] = (byte)'x';
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("\"v\""), value.GetRawValueBytes());
            byte[] returned = value.GetRawValueBytes(); returned[0] = 0;
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("\"v\""), value.GetRawValueBytes());
        }

        [Test]
        public void UnknownEvidence_UsesEncounterOrderAtWrappedBoundaries()
        {
            RawSavePayloadClassification result = Classify("{\"before\":1,\"schema\":\"save_root\",\"schemaVersion\":1,\"primary\":{\"z\":null,\"saveVersion\":1,\"a\":[]},\"after\":false}");
            CollectionAssert.AreEqual(new[] { "before", "after" }, result.UnknownRootMembers.Select(x => x.Name));
            CollectionAssert.AreEqual(new[] { "z", "a" }, result.UnknownPrimaryMembers.Select(x => x.Name));
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("[]"), result.UnknownPrimaryMembers[1].GetRawValueBytes());
        }

        [TestCase("mvpRoomSlotAssignments", "Rooms")]
        [TestCase("mvpDungeonPlacements", "Entries")]
        public void ArrayRoutePresence_IsBasedOnRawNonNullElements(string outer, string inner)
        {
            Assert.That(Classify("{\"" + outer + "\":null}").RoomSlotAssignmentsPresence == RawLegacyRoutePresence.Present ||
                        Classify("{\"" + outer + "\":null}").DungeonPlacementsPresence == RawLegacyRoutePresence.Present, Is.False);
            RawSavePayloadClassification allNull = Classify("{\"" + outer + "\":{\"" + inner + "\":[null,null]}}");
            RawSavePayloadClassification malformed = Classify("{\"" + outer + "\":{\"" + inner + "\":[null,42]}}");
            RawLegacyRoutePresence absent = outer == "mvpRoomSlotAssignments" ? allNull.RoomSlotAssignmentsPresence : allNull.DungeonPlacementsPresence;
            RawLegacyRoutePresence present = outer == "mvpRoomSlotAssignments" ? malformed.RoomSlotAssignmentsPresence : malformed.DungeonPlacementsPresence;
            Assert.That(absent, Is.EqualTo(RawLegacyRoutePresence.Absent)); Assert.That(present, Is.EqualTo(RawLegacyRoutePresence.Present));
        }

        [Test]
        public void ExactOrderedFourNodeStarterShell_IsAbsent_AndDeviationIsPresent()
        {
            string nodes = string.Join(",", Enumerable.Range(0, 4).Select(i => "{\"FloorIndex\":0,\"NodeIndex\":" + i + ",\"SlotId\":\"mvp.floor.00.node." + i.ToString("D2") + "\",\"CategoryId\":\"\",\"OptionId\":\"\",\"Revision\":0}"));
            string shell = "{\"mvpDungeonFloorLayout\":{\"Nodes\":[" + nodes + "],\"NextRevision\":1}}";
            Assert.That(Classify(shell).FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Absent));
            Assert.That(Classify(shell.Replace("node.03", "node.xx")).FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
            Assert.That(Classify("{\"mvpDungeonFloorLayout\":{\"Nodes\":[null],\"NextRevision\":1}}").FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
        }

        [Test]
        public void Limits_AreBoundaryCheckedAndFailuresDeterministic()
        {
            byte[] input = Encoding.UTF8.GetBytes("{\"saveVersion\":1}");
            Assert.That(RawSavePayloadClassifier.Classify(input, Limits(input.Length), Versions).IsSuccess, Is.True);
            Assert.That(RawSavePayloadClassifier.Classify(input, Limits(input.Length - 1), Versions).FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            Assert.That(Classify("{\"saveVersion\":[1,2]}", Limits(elements: 1)).FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            Assert.That(Classify("{\"saveVersion\":1,\"contentVersion\":2}", Limits(members: 1)).FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            Assert.That(Classify("{\"saveVersion\":\"ab\"}", Limits(strings: 1)).FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            string nested = "{\"saveVersion\":" + new string('[', 40) + "null" + new string(']', 40) + "}";
            RawSavePayloadClassification first = Classify(nested, Limits(depth: 8)); RawSavePayloadClassification second = Classify(nested, Limits(depth: 8));
            Assert.That(first.FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            Assert.That(second.FailureByteOffset, Is.EqualTo(first.FailureByteOffset));
        }

        [Test]
        public void RepeatedClassification_IsDeterministicAndInputUnchanged()
        {
            byte[] input = Encoding.UTF8.GetBytes("{\"saveVersion\":1,\"unknown\":true}"); byte[] before = (byte[])input.Clone();
            RawSavePayloadClassification a = RawSavePayloadClassifier.Classify(input, Limits(), Versions);
            RawSavePayloadClassification b = RawSavePayloadClassifier.Classify(input, Limits(), Versions);
            Assert.That(b.Envelope, Is.EqualTo(a.Envelope)); Assert.That(b.UnknownPrimaryMembers[0].ByteOffset, Is.EqualTo(a.UnknownPrimaryMembers[0].ByteOffset));
            CollectionAssert.AreEqual(before, input);
        }
    }
}
#endif
