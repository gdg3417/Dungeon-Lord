#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests
{
    public sealed class Gd66RawSavePayloadClassifierTests
    {
        private static RawSavePayloadClassificationLimits Limits(int bytes = 10000, int depth = 32,
            int members = 100, int elements = 100, int strings = 1000, int work = 50000) =>
            new RawSavePayloadClassificationLimits(bytes, depth, members, elements, strings, work);
        private static readonly RawSaveEnvelopeVersionContract Versions = new RawSaveEnvelopeVersionContract(1, 6);
        private static RawLegacyBlankFloorContract BlankFloor()
        {
            MvpDungeonFloorLayoutState starter = MvpDungeonFloorLayoutState.CreateEmptyStarterFloor();
            return new RawLegacyBlankFloorContract(starter.NextRevision,
                starter.Nodes.Select(node => new RawLegacyBlankFloorNodeContract(node.FloorIndex, node.NodeIndex,
                    node.SlotId, node.CategoryId, node.OptionId, node.Revision)), true, true,
                new[] { "Nodes", "NextRevision" },
                new[] { "FloorIndex", "NodeIndex", "SlotId", "CategoryId", "OptionId", "Revision" });
        }
        private static RawSavePayloadClassification Classify(string json, RawSavePayloadClassificationLimits? limits = null) =>
            RawSavePayloadClassifier.Classify(Encoding.UTF8.GetBytes(json), limits ?? Limits(), Versions, BlankFloor());

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
            Assert.That(RawSavePayloadClassifier.Classify(new byte[] { 0xef, 0xbb, 0xbf, (byte)'{' , (byte)'}' }, Limits(), Versions, BlankFloor()).IsSuccess, Is.False);
            Assert.That(RawSavePayloadClassifier.Classify(new byte[] { (byte)'{', (byte)'\"', 0xc0, 0xaf, (byte)'\"', (byte)':', (byte)'1', (byte)'}' }, Limits(), Versions, BlankFloor()).IsSuccess, Is.False);
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
            RawSavePayloadClassification result = RawSavePayloadClassifier.Classify(input, Limits(), Versions, BlankFloor());
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
            MvpDungeonFloorLayoutState starter = MvpDungeonFloorLayoutState.CreateEmptyStarterFloor();
            string[] nodeRecords = starter.Nodes.Select(node => "{\"FloorIndex\":" + node.FloorIndex + ",\"NodeIndex\":" + node.NodeIndex +
                ",\"SlotId\":\"" + node.SlotId + "\",\"CategoryId\":\"" + node.CategoryId + "\",\"OptionId\":\"" + node.OptionId +
                "\",\"Revision\":" + node.Revision + "}").ToArray();
            string nodes = string.Join(",", nodeRecords);
            string shell = "{\"mvpDungeonFloorLayout\":{\"Nodes\":[" + nodes + "],\"NextRevision\":" + starter.NextRevision + "}}";
            Assert.That(Classify(shell).FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Absent));
            Assert.That(Classify(shell.Replace("node.03", "node.xx")).FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
            Assert.That(Classify("{\"mvpDungeonFloorLayout\":{\"Nodes\":[null],\"NextRevision\":1}}").FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
            Assert.That(Classify(shell.Replace("\"FloorIndex\":0", "\"FloorIndex\":1")).FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
            Assert.That(Classify(shell.Replace("\"NodeIndex\":0", "\"NodeIndex\":9")).FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
            Assert.That(Classify(shell.Replace("\"CategoryId\":\"\"", "\"CategoryId\":\"x\"")).FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
            Assert.That(Classify(shell.Replace("\"OptionId\":\"\"", "\"OptionId\":\"x\"")).FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
            Assert.That(Classify(shell.Replace("\"Revision\":0", "\"Revision\":1")).FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
            Assert.That(Classify(shell.Replace(",\"Revision\":0", "")).FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
            Assert.That(Classify(shell.Replace("\"Revision\":0", "\"Revision\":0,\"Extra\":0")).FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
            Assert.That(Classify(shell.Replace("[" + nodes + "]", "[" + string.Join(",", nodeRecords.Reverse()) + "]")).FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
            Assert.That(Classify(shell.Replace("," + nodeRecords[3], "")).FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
            Assert.That(Classify(shell.Replace(nodes, nodes + "," + nodeRecords[3])).FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
            Assert.That(Classify("{\"mvpDungeonFloorLayout\":[]}").FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
            Assert.That(Classify("{\"mvpDungeonFloorLayout\":{\"Nodes\":42,\"NextRevision\":1}}").FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
            Assert.That(Classify("{\"mvpDungeonFloorLayout\":{\"Nodes\":[],\"NextRevision\":2}}").FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
            Assert.That(Classify("{\"mvpDungeonFloorLayout\":{\"Nodes\":[]}}").FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
        }

        [Test]
        public void Limits_AreBoundaryCheckedAndFailuresDeterministic()
        {
            byte[] input = Encoding.UTF8.GetBytes("{\"saveVersion\":1}");
            Assert.That(RawSavePayloadClassifier.Classify(input, Limits(input.Length), Versions, BlankFloor()).IsSuccess, Is.True);
            Assert.That(RawSavePayloadClassifier.Classify(input, Limits(input.Length - 1), Versions, BlankFloor()).FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            Assert.That(Classify("{\"saveVersion\":[1,2]}", Limits(elements: 1)).FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            Assert.That(Classify("{\"saveVersion\":1,\"contentVersion\":2}", Limits(members: 1)).FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            Assert.That(Classify("{\"saveVersion\":\"ab\"}", Limits(strings: 1)).FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            string nested = "{\"saveVersion\":" + new string('[', 40) + "null" + new string(']', 40) + "}";
            RawSavePayloadClassification first = Classify(nested, Limits(depth: 8)); RawSavePayloadClassification second = Classify(nested, Limits(depth: 8));
            Assert.That(first.FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            Assert.That(second.FailureByteOffset, Is.EqualTo(first.FailureByteOffset));
        }

        [TestCase("{\"saveVersion\":1,}")]
        [TestCase("{\"saveVersion\":[1,]}")]
        [TestCase("{\"saveVersion\":{\"x\":1,}}")]
        [TestCase("{\"saveVersion\":[{\"x\":1,}]}")]
        [TestCase("{\"saveVersion\":[,1]}")]
        [TestCase("{\"saveVersion\" 1}")]
        [TestCase("{\"saveVersion\":}")]
        [TestCase("{\"saveVersion\":[1")]
        public void StructuralJsonErrors_AreUnreadable(string json)
        { Assert.That(Classify(json).FailureReason, Is.EqualTo(RawSavePayloadClassifier.UnreadableReason)); }

        [TestCase("1", true, null)]
        [TestCase("1.0", true, null)]
        [TestCase("10e-1", true, null)]
        [TestCase("1e0", true, null)]
        [TestCase("1.5", false, "gd66.payload.nonintegral_schema_version")]
        [TestCase("15e-1", false, "gd66.payload.nonintegral_schema_version")]
        [TestCase("2147483648", false, "gd66.payload.newer_than_application")]
        [TestCase("999999999999999999999999999999999999999999", false, "gd66.payload.newer_than_application")]
        [TestCase("-999999999999999999999999999999999999999999", false, "gd66.payload.unsupported_legacy_version")]
        public void VersionTokens_AreClassifiedWithoutFixedWidthParsing(string token, bool success, string reason)
        {
            RawSavePayloadClassification result = Classify("{\"schema\":\"save_root\",\"schemaVersion\":" + token + ",\"primary\":{}}");
            Assert.That(result.IsSuccess, Is.EqualTo(success));
            if (!success) Assert.That(result.FailureReason, Is.EqualTo(reason));
        }

        [Test]
        public void RootEnvelopeEvidence_IsExplicitRawAndDefensivelyOwned()
        {
            byte[] source = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":1.0,\"primary\":{\"saveVersion\":1}}");
            RawSavePayloadClassification wrapped = RawSavePayloadClassifier.Classify(source, Limits(), Versions, BlankFloor());
            Assert.That(wrapped.RootSchemaEvidence.Kind, Is.EqualTo(RawJsonValueKind.String));
            Assert.That(wrapped.RootSchemaVersionEvidence.Kind, Is.EqualTo(RawJsonValueKind.Number));
            Assert.That(wrapped.RootPrimaryEvidence.Kind, Is.EqualTo(RawJsonValueKind.Object));
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("1.0"), wrapped.RootSchemaVersionEvidence.GetRawValueBytes());
            int offset = wrapped.RootSchemaVersionEvidence.ByteOffset; source[offset] = (byte)'9';
            byte[] returned = wrapped.RootSchemaVersionEvidence.GetRawValueBytes(); returned[0] = (byte)'8';
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("1.0"), wrapped.RootSchemaVersionEvidence.GetRawValueBytes());
            RawSavePayloadClassification unwrapped = Classify("{\"saveVersion\":1}");
            Assert.That(unwrapped.RootSchemaEvidence.State, Is.EqualTo(RawSaveMemberState.Absent));
            Assert.That(unwrapped.RootSchemaVersionEvidence.State, Is.EqualTo(RawSaveMemberState.Absent));
            Assert.That(unwrapped.RootPrimaryEvidence.State, Is.EqualTo(RawSaveMemberState.Absent));
        }

        [TestCase("mvpRoomSlotAssignments", "Rooms")]
        [TestCase("mvpDungeonPlacements", "Entries")]
        public void MalformedNonNullRouteShapes_ArePresent(string outer, string inner)
        {
            string[] outerValues = { "false", "1", "\"x\"", "[]" };
            foreach (string value in outerValues) AssertRoute(outer, Classify("{\"" + outer + "\":" + value + "}"), RawLegacyRoutePresence.Present);
            string[] nestedValues = { "false", "1", "\"x\"", "{}" };
            foreach (string value in nestedValues) AssertRoute(outer, Classify("{\"" + outer + "\":{\"" + inner + "\":" + value + "}}"), RawLegacyRoutePresence.Present);
            AssertRoute(outer, Classify("{\"" + outer + "\":null}"), RawLegacyRoutePresence.Absent);
            AssertRoute(outer, Classify("{\"" + outer + "\":{\"" + inner + "\":null}}"), RawLegacyRoutePresence.Absent);
            AssertRoute(outer, Classify("{\"" + outer + "\":{\"" + inner + "\":[]}}"), RawLegacyRoutePresence.Absent);
            AssertRoute(outer, Classify("{\"" + outer + "\":{\"" + inner + "\":[null,{}]}}"), RawLegacyRoutePresence.Present);
        }

        [TestCase("aaaaaaaaaaaa", 12)]
        [TestCase("éééééé", 12)]
        [TestCase("\\n\\n\\n\\n\\n\\n", 12)]
        [TestCase("\\u0061\\u0061", 12)]
        [TestCase("\\uD83D\\uDE00", 12)]
        public void MaximumStringBytes_CountsRawInteriorBytes(string rawJsonString, int rawBytes)
        {
            string json = "{\"s\":\"" + rawJsonString + "\",\"saveVersion\":1}";
            Assert.That(Classify(json, Limits(strings: rawBytes)).IsSuccess, Is.True);
            Assert.That(Classify(json, Limits(strings: rawBytes - 1)).FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
        }

        [Test]
        public void InvalidUtf8Forms_AreUnreadable()
        {
            byte[][] invalid = {
                new byte[] { (byte)'{', (byte)'\"', 0x80, (byte)'\"', (byte)':', (byte)'1', (byte)'}' },
                new byte[] { (byte)'{', (byte)'\"', 0xf5, 0x80, 0x80, 0x80, (byte)'\"', (byte)':', (byte)'1', (byte)'}' },
                new byte[] { (byte)'{', (byte)'\"', 0xed, 0xa0, 0x80, (byte)'\"', (byte)':', (byte)'1', (byte)'}' }
            };
            foreach (byte[] bytes in invalid)
                Assert.That(RawSavePayloadClassifier.Classify(bytes, Limits(), Versions, BlankFloor()).FailureReason, Is.EqualTo(RawSavePayloadClassifier.UnreadableReason));
        }

        private static void AssertRoute(string outer, RawSavePayloadClassification result, RawLegacyRoutePresence expected)
        {
            RawLegacyRoutePresence actual = outer == "mvpRoomSlotAssignments" ? result.RoomSlotAssignmentsPresence : result.DungeonPlacementsPresence;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void EveryPinnedMember_HasAbsentNullAndNonNullRawEvidence()
        {
            RawSavePayloadClassification absent = Classify("{\"schema\":\"save_root\",\"schemaVersion\":1,\"primary\":{}}");
            Assert.That(absent.Members.Count, Is.EqualTo(RawSavePayloadClassifier.RecognizedSaveDataMemberNames.Count));
            foreach (string name in RawSavePayloadClassifier.RecognizedSaveDataMemberNames)
            {
                Assert.That(absent.Members.Single(x => x.Name == name).State, Is.EqualTo(RawSaveMemberState.Absent));
                RawSaveMemberEvidence nil = Classify("{\"" + name + "\":null}").Members.Single(x => x.Name == name);
                Assert.That(nil.State, Is.EqualTo(RawSaveMemberState.Null)); Assert.That(nil.Kind, Is.EqualTo(RawJsonValueKind.Null));
                CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("null"), nil.GetRawValueBytes());
                RawSaveMemberEvidence value = Classify("{\"" + name + "\":[]}").Members.Single(x => x.Name == name);
                Assert.That(value.State, Is.EqualTo(RawSaveMemberState.NonNull)); Assert.That(value.Kind, Is.EqualTo(RawJsonValueKind.Array));
                CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("[]"), value.GetRawValueBytes());
            }
        }

        [Test]
        public void UnknownMembers_PreserveEveryKindOrderOwnershipAndReadOnlyCollections()
        {
            string json = "{\"saveVersion\":1,\"o\":{},\"a\":[],\"s\":\"x\",\"n\":2,\"t\":true,\"f\":false,\"z\":null}";
            RawSavePayloadClassification result = Classify(json);
            CollectionAssert.AreEqual(new[] { "o", "a", "s", "n", "t", "f", "z" }, result.UnknownPrimaryMembers.Select(x => x.Name));
            CollectionAssert.AreEqual(new[] { RawJsonValueKind.Object, RawJsonValueKind.Array, RawJsonValueKind.String,
                RawJsonValueKind.Number, RawJsonValueKind.Boolean, RawJsonValueKind.Boolean, RawJsonValueKind.Null }, result.UnknownPrimaryMembers.Select(x => x.Kind));
            byte[] bytes = result.UnknownPrimaryMembers[0].GetRawValueBytes(); bytes[0] = 0;
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("{}"), result.UnknownPrimaryMembers[0].GetRawValueBytes());
            Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<RawUnknownMemberEvidence>)result.UnknownPrimaryMembers).Add(result.UnknownPrimaryMembers[0]));
        }

        [Test]
        public void EveryWorkloadLimit_HasExactAndOneOverBoundaries()
        {
            byte[] one = Encoding.UTF8.GetBytes("{\"saveVersion\":1}");
            Assert.That(RawSavePayloadClassifier.Classify(one, Limits(bytes: one.Length), Versions, BlankFloor()).IsSuccess, Is.True);
            Assert.That(RawSavePayloadClassifier.Classify(one, Limits(bytes: one.Length - 1), Versions, BlankFloor()).FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            Assert.That(Classify("{\"saveVersion\":1}", Limits(members: 1)).IsSuccess, Is.True);
            Assert.That(Classify("{\"saveVersion\":1,\"contentVersion\":2}", Limits(members: 1)).FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            Assert.That(Classify("{\"saveVersion\":[null]}", Limits(elements: 1)).IsSuccess, Is.True);
            Assert.That(Classify("{\"saveVersion\":[null,null]}", Limits(elements: 1)).FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            Assert.That(Classify("{\"saveVersion\":[]}", Limits(depth: 2)).IsSuccess, Is.True);
            Assert.That(Classify("{\"saveVersion\":[]}", Limits(depth: 1)).FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            Assert.That(Classify("{\"saveVersion\":1}", Limits(work: 19)).IsSuccess, Is.True);
            Assert.That(Classify("{\"saveVersion\":1}", Limits(work: 18)).FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
        }

        [Test]
        public void OversizedInput_IsRejectedBeforeParsingOrEvidenceConstruction()
        {
            byte[] oversized = Encoding.UTF8.GetBytes("{\"saveVersion\":1}");
            RawSavePayloadClassification result = RawSavePayloadClassifier.Classify(oversized,
                Limits(bytes: oversized.Length - 1), Versions, BlankFloor());
            Assert.That(result.FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            Assert.That(result.Members, Is.Empty);
            Assert.That(result.RootSchemaEvidence.State, Is.EqualTo(RawSaveMemberState.Absent));
        }

        [Test]
        public void BlankFloorContract_IsInjectedValidatedAndDefensivelyOwned()
        {
            RawLegacyBlankFloorContract current = BlankFloor(); string shell = Shell(current);
            Assert.That(Classify(shell).FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Absent));
            var changedNodes = current.OrderedNodes.Select((node, index) => new RawLegacyBlankFloorNodeContract(
                node.FloorIndex, node.NodeIndex, index == 0 ? node.SlotId + ".different" : node.SlotId,
                node.CategoryId, node.OptionId, node.Revision)).ToArray();
            var changed = Contract(changedNodes, current.ExpectedNextRevision);
            Assert.That(RawSavePayloadClassifier.Classify(Encoding.UTF8.GetBytes(shell), Limits(), Versions, changed).FloorLayoutPresence,
                Is.EqualTo(RawLegacyRoutePresence.Present));

            var fiveNodes = Enumerable.Range(0, 5).Select(index => new RawLegacyBlankFloorNodeContract(
                2, index, "test.slot." + index, "", "", 0)).ToArray();
            var five = Contract(fiveNodes, 3);
            Assert.That(RawSavePayloadClassifier.Classify(Encoding.UTF8.GetBytes(Shell(five)), Limits(), Versions, five).FloorLayoutPresence,
                Is.EqualTo(RawLegacyRoutePresence.Absent));

            var mutable = fiveNodes.ToList(); var owned = Contract(mutable, 3); mutable.Clear();
            Assert.That(owned.OrderedNodes.Count, Is.EqualTo(5));
            var invalid = Contract(new[] { new RawLegacyBlankFloorNodeContract(0, 0, null, "", "", 0) }, 1);
            Assert.That(invalid.IsValid, Is.False);
            var duplicate = Contract(new[] { fiveNodes[0], fiveNodes[0] }, 3);
            Assert.That(duplicate.IsValid, Is.False);
            Assert.That(RawSavePayloadClassifier.Classify(Encoding.UTF8.GetBytes("{\"saveVersion\":1}"), Limits(), Versions, duplicate).FailureReason,
                Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
        }

        [TestCase("                    {\"saveVersion\":1}")]
        [TestCase("{\"saveVersion\":1}                    ")]
        [TestCase("{\"averyveryverylongmembername\":0,\"saveVersion\":1}")]
        [TestCase("{\"saveVersion\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"}")]
        [TestCase("{\"saveVersion\":\"éééééééééééééééé\"}")]
        [TestCase("{\"saveVersion\":\"\\n\\n\\n\\n\\n\\n\\n\\n\\n\\n\\n\\n\"}")]
        [TestCase("{\"saveVersion\":123456789012345678901234567890}")]
        [TestCase("{\"saveVersion\":123456789012345678901234567890e+}")]
        [TestCase("{\"saveVersion\":truX}")]
        [TestCase("{\"saveVersion\":[[[[[[[[[[null]]]]]]]]]]}")]
        [TestCase("{\"saveVersion\":1,}")]
        public void ScanWork_IsFailFastAtEveryTokenFamily(string json)
        {
            int exact = FirstNonWorkloadBudget(json);
            RawSavePayloadClassification atBoundary = Classify(json, Limits(work: exact));
            Assert.That(atBoundary.FailureReason, Is.Not.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            RawSavePayloadClassification first = Classify(json, Limits(work: exact - 1));
            RawSavePayloadClassification second = Classify(json, Limits(work: exact - 1));
            Assert.That(first.FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            Assert.That(second.FailureByteOffset, Is.EqualTo(first.FailureByteOffset));
        }

        private static int FirstNonWorkloadBudget(string json)
        {
            for (int work = 1; work < 10000; work++)
                if (Classify(json, Limits(work: work)).FailureReason != RawSavePayloadClassifier.WorkloadExceededReason) return work;
            Assert.Fail("test scan budget search did not converge"); return -1;
        }

        private static RawLegacyBlankFloorContract Contract(System.Collections.Generic.IEnumerable<RawLegacyBlankFloorNodeContract> nodes, int nextRevision) =>
            new RawLegacyBlankFloorContract(nextRevision, nodes, true, true,
                new[] { "Nodes", "NextRevision" },
                new[] { "FloorIndex", "NodeIndex", "SlotId", "CategoryId", "OptionId", "Revision" });

        private static string Shell(RawLegacyBlankFloorContract contract)
        {
            string nodes = string.Join(",", contract.OrderedNodes.Select(node => "{\"FloorIndex\":" + node.FloorIndex +
                ",\"NodeIndex\":" + node.NodeIndex + ",\"SlotId\":\"" + node.SlotId + "\",\"CategoryId\":\"" + node.CategoryId +
                "\",\"OptionId\":\"" + node.OptionId + "\",\"Revision\":" + node.Revision + "}"));
            return "{\"mvpDungeonFloorLayout\":{\"Nodes\":[" + nodes + "],\"NextRevision\":" + contract.ExpectedNextRevision + "}}";
        }

        [Test]
        public void RepeatedClassification_IsDeterministicAndInputUnchanged()
        {
            byte[] input = Encoding.UTF8.GetBytes("{\"saveVersion\":1,\"unknown\":true}"); byte[] before = (byte[])input.Clone();
            RawSavePayloadClassification a = RawSavePayloadClassifier.Classify(input, Limits(), Versions, BlankFloor());
            RawSavePayloadClassification b = RawSavePayloadClassifier.Classify(input, Limits(), Versions, BlankFloor());
            Assert.That(b.Envelope, Is.EqualTo(a.Envelope)); Assert.That(b.UnknownPrimaryMembers[0].ByteOffset, Is.EqualTo(a.UnknownPrimaryMembers[0].ByteOffset));
            CollectionAssert.AreEqual(before, input);
        }
    }
}
#endif
