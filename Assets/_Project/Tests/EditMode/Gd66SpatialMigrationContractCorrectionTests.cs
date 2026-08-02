using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66SpatialMigrationContractCorrectionTests
    {
        private const string H0 = "0000000000000000000000000000000000000000000000000000000000000000";
        private const string H1 = "1111111111111111111111111111111111111111111111111111111111111111";
        private const string H2 = "2222222222222222222222222222222222222222222222222222222222222222";
        private static readonly SpatialSerializedInputLimits Limits = new SpatialSerializedInputLimits(200000, 20000, 2000, 20000, 20);
        private static readonly CanonicalSpatialSerializationLimits SaveLimits = new CanonicalSpatialSerializationLimits(
            Limits, new CanonicalSpatialSaveWorkloadLimits(2000, 2000));

        [TestCase(null, null, true)]
        [TestCase(H2, null, true)]
        [TestCase("bad", null, false)]
        [TestCase(null, "receipt", false)]
        public void OriginalRestored_EnforcesOptionalCanonicalCandidateAndProhibitsReceipt(
            string candidate, string receiptMarker, bool expected)
        {
            SpatialMigrationJournal valid = Journal(SpatialMigrationJournalStage.OriginalRestored, H0,
                candidate, receiptMarker == null ? null : Names().FinalizedReceipt);
            Assert.AreEqual(expected, SpatialMigrationJournalContracts.Serialize(valid, Limits).IsValid);
        }

        [TestCase(null)]
        [TestCase(H1)]
        public void OriginalRestored_RequiresMatchingBackup(string backup)
        {
            Assert.IsFalse(SpatialMigrationJournalContracts.Serialize(
                Journal(SpatialMigrationJournalStage.OriginalRestored, backup, null, null), Limits).IsValid);
        }

        [Test]
        public void DiagnosticBudget_One_ReplacesFirstAdditionalIssueWithWorkloadExceeded()
        {
            byte[] wrongOrder = Encoding.UTF8.GetBytes("{\"Floors\":[],\"Authority\":null}");
            var one = new CanonicalSpatialSerializationLimits(
                new SpatialSerializedInputLimits(wrongOrder.Length, 100, 10, 100, 1), SaveLimits.Spatial);
            SpatialContractResult<DetachedCanonicalSpatialSaveState> result =
                CanonicalSpatialSaveSerializer.Parse(wrongOrder, one);
            Assert.AreEqual(1, result.Issues.Length);
            Assert.AreEqual(SpatialContractIssue.WorkloadExceeded, result.Issues[0]);
        }

        [Test]
        public void EveryParserBudget_HasExactBoundaryAndOneOverBehavior()
        {
            byte[] bytes = SpatialMigrationDescriptorContracts.Serialize(Descriptor(), Limits).Value;
            Assert.IsTrue(SpatialMigrationDescriptorContracts.Parse(bytes,
                new SpatialSerializedInputLimits(bytes.Length, 1000, 10, 1000, 10)).IsValid);
            AssertIssue(bytes, new SpatialSerializedInputLimits(bytes.Length - 1, 1000, 10, 1000, 10),
                SpatialContractIssue.InputByteLimitExceeded);
            AssertIssue(bytes, new SpatialSerializedInputLimits(bytes.Length, 1, 10, 1000, 10),
                SpatialContractIssue.WorkloadExceeded);
            AssertIssue(bytes, new SpatialSerializedInputLimits(bytes.Length, 1000, 0, 1000, 10),
                SpatialContractIssue.WorkloadExceeded);
            AssertIssue(bytes, new SpatialSerializedInputLimits(bytes.Length, 1000, 10, 0, 10),
                SpatialContractIssue.WorkloadExceeded);
        }

        [Test]
        public void StringBudget_CountsPropertyNamesAndValuesAsUtf16CodeUnits()
        {
            byte[] json = Encoding.UTF8.GetBytes("{\"A\":\"😀\"}"); // one property unit + two value units
            Assert.IsTrue(ParseLowLevelThroughDescriptorShape(json, 3).Contains(SpatialContractIssue.UnknownField));
            CollectionAssert.Contains(ParseLowLevelThroughDescriptorShape(json, 2), SpatialContractIssue.WorkloadExceeded);
        }

        [TestCase("\\ud800")]
        [TestCase("\\udc00")]
        [TestCase("\\ud800\\u0041")]
        public void MalformedSurrogates_FailClosedAcrossAllPublicParsers(string escape)
        {
            byte[] save = CanonicalSpatialSaveSerializer.Serialize(EmptyState(), SaveLimits).Value;
            save = Replace(save, "\"MigrationTransactionId\":null", "\"MigrationTransactionId\":\"" + escape + "\"");
            CollectionAssert.Contains(CanonicalSpatialSaveSerializer.Parse(save, SaveLimits).Issues,
                SpatialContractIssue.MalformedJson);

            byte[] descriptor = SpatialMigrationDescriptorContracts.Serialize(Descriptor(), Limits).Value;
            descriptor = Replace(descriptor, "compat.geometry.r1-r2", escape);
            CollectionAssert.Contains(SpatialMigrationDescriptorContracts.Parse(descriptor, Limits).Issues,
                SpatialContractIssue.MalformedJson);

            byte[] journal = SpatialMigrationJournalContracts.Serialize(
                Journal(SpatialMigrationJournalStage.BackupVerified, H0, null, null), Limits).Value;
            journal = Replace(journal, "save.", escape);
            CollectionAssert.Contains(SpatialMigrationJournalContracts.Parse(journal, Limits).Issues,
                SpatialContractIssue.MalformedJson);
        }

        [Test]
        public void ValidNonBmpPair_IsAcceptedByStrictParserThenRejectedOnlyByStableIdGrammar()
        {
            byte[] descriptor = SpatialMigrationDescriptorContracts.Serialize(Descriptor(), Limits).Value;
            descriptor = Replace(descriptor, "compat.geometry.r1-r2", "\\ud83d\\ude00");
            SpatialContractResult<SpatialMigrationInputDescriptor> result =
                SpatialMigrationDescriptorContracts.Parse(descriptor, Limits);
            Assert.IsFalse(result.IsValid);
            CollectionAssert.DoesNotContain(result.Issues, SpatialContractIssue.MalformedJson);
            CollectionAssert.Contains(result.Issues, SpatialContractIssue.InvalidStableId);
        }

        [TestCase(1, false)]
        [TestCase(2, true)]
        public void PopulatedR1AndR2_RoundTripWithoutMutation(int rooms, bool physicalCorridor)
        {
            DetachedCanonicalSpatialSaveState source = Populated(rooms, physicalCorridor);
            RoomSpatialInstance[] sourceRooms = source.Floors[0].Layout.Rooms;
            string[] order = sourceRooms.Select(room => room.RoomInstanceId).ToArray();
            SpatialContractResult<byte[]> serialized = CanonicalSpatialSaveSerializer.Serialize(source, SaveLimits);
            Assert.IsTrue(serialized.IsValid);
            Assert.AreNotEqual(0xef, serialized.Value[0]);
            string json = Encoding.UTF8.GetString(serialized.Value);
            Assert.IsFalse(json.EndsWith("\n", StringComparison.Ordinal));
            Assert.IsFalse(json.Contains("CandidatePayloadSha256"));
            Assert.Less(json.IndexOf("\"Authority\"", StringComparison.Ordinal),
                json.IndexOf("\"Floors\"", StringComparison.Ordinal));
            SpatialContractResult<DetachedCanonicalSpatialSaveState> parsed =
                CanonicalSpatialSaveSerializer.Parse(serialized.Value, SaveLimits);
            Assert.IsTrue(parsed.IsValid);
            CollectionAssert.AreEqual(serialized.Value,
                CanonicalSpatialSaveSerializer.Serialize(parsed.Value, SaveLimits).Value);
            Assert.AreSame(sourceRooms, source.Floors[0].Layout.Rooms);
            CollectionAssert.AreEqual(order, source.Floors[0].Layout.Rooms.Select(room => room.RoomInstanceId));
        }

        [Test]
        public void SerializerVersionOne_DeclaresEveryPublicSerializableField()
        {
            Assert.IsTrue(CanonicalSpatialSaveSerializer.DeclaredFieldsMatchSerializableFields());
        }

        [Test]
        public void DiagnosticBudget_FirstIssueBeyondTwoStopsAtStableExhaustion()
        {
            byte[] reversed = Encoding.UTF8.GetBytes("{\"Floors\":[],\"Authority\":null,\"Extra\":0}");
            var limits = new CanonicalSpatialSerializationLimits(
                new SpatialSerializedInputLimits(reversed.Length, 100, 10, 100, 2), SaveLimits.Spatial);
            SpatialContractResult<DetachedCanonicalSpatialSaveState> result =
                CanonicalSpatialSaveSerializer.Parse(reversed, limits);
            Assert.AreEqual(2, result.Issues.Length);
            Assert.AreEqual(SpatialContractIssue.WorkloadExceeded, result.Issues[1]);
        }

        [Test]
        public void EveryJournalStage_EnforcesRequiredAndProhibitedEvidence()
        {
            foreach (SpatialMigrationJournalStage stage in Enum.GetValues(typeof(SpatialMigrationJournalStage)))
                Assert.IsTrue(SpatialMigrationJournalContracts.Serialize(ValidJournal(stage), Limits).IsValid, stage.ToString());

            Assert.IsFalse(SpatialMigrationJournalContracts.Serialize(
                Journal(SpatialMigrationJournalStage.DescriptorPinned, H0, null, null), Limits).IsValid);
            Assert.IsFalse(SpatialMigrationJournalContracts.Serialize(
                Journal(SpatialMigrationJournalStage.BackupVerified, H0, H2, null), Limits).IsValid);
            foreach (SpatialMigrationJournalStage stage in new[] { SpatialMigrationJournalStage.CandidateVerified,
                SpatialMigrationJournalStage.Replaced, SpatialMigrationJournalStage.DurableVerified,
                SpatialMigrationJournalStage.Finalized })
                Assert.IsFalse(SpatialMigrationJournalContracts.Serialize(Journal(stage, H0, null, null), Limits).IsValid);
            Assert.IsFalse(SpatialMigrationJournalContracts.Serialize(Journal(
                SpatialMigrationJournalStage.CandidateVerified, H0, H2, Names().FinalizedReceipt), Limits).IsValid);
        }

        [Test]
        public void JournalParser_RejectsIdentityPathHashDescriptorAndStageTampering()
        {
            byte[] valid = SpatialMigrationJournalContracts.Serialize(
                ValidJournal(SpatialMigrationJournalStage.Finalized), Limits).Value;
            string json = Encoding.UTF8.GetString(valid);
            string transaction = ValidJournal(SpatialMigrationJournalStage.Finalized).TransactionId;
            string fingerprint = ValidJournal(SpatialMigrationJournalStage.Finalized).DescriptorFingerprintSha256;
            string identity = ValidJournal(SpatialMigrationJournalStage.Finalized).TransactionIdentitySha256;
            string[] tampered =
            {
                json.Replace("compat.geometry.r1-r2", "compat.geometry.changed"),
                json.Replace("\"DescriptorFingerprintSha256\":\"" + fingerprint, "\"DescriptorFingerprintSha256\":\"" + H2),
                json.Replace("\"TransactionIdentitySha256\":\"" + identity, "\"TransactionIdentitySha256\":\"" + H2),
                json.Replace(transaction, "gd66-" + H2),
                json.Replace("\"OriginalPayloadSha256\":\"" + H0, "\"OriginalPayloadSha256\":\"" + H2),
                json.Replace("\"BackupPayloadSha256\":\"" + H0, "\"BackupPayloadSha256\":\"" + H2),
                json.Replace("\"ExpectedCandidateSha256\":\"" + H2, "\"ExpectedCandidateSha256\":\"bad"),
                json.Replace(".journal.json", ".wrong.json"),
                json.Replace(".original.bak", ".wrong.bak"),
                json.Replace(".candidate.tmp", ".wrong.tmp"),
                json.Replace(".finalized", ".wrong.finalized"),
                json.Replace("\"Stage\":6", "\"Stage\":99")
            };
            foreach (string candidate in tampered)
                Assert.IsFalse(SpatialMigrationJournalContracts.Parse(Encoding.UTF8.GetBytes(candidate), Limits).IsValid);
        }

        [Test]
        public void MalformedSurrogateInsideNestedDescriptorAndJournalPathFailsClosed()
        {
            string json = Encoding.UTF8.GetString(SpatialMigrationJournalContracts.Serialize(
                ValidJournal(SpatialMigrationJournalStage.BackupVerified), Limits).Value);
            string nested = json.Replace("compat.geometry.r1-r2", "\\ud800");
            CollectionAssert.Contains(SpatialMigrationJournalContracts.Parse(
                Encoding.UTF8.GetBytes(nested), Limits).Issues, SpatialContractIssue.MalformedJson);
            string path = json.Replace("save.", "\\udc00");
            CollectionAssert.Contains(SpatialMigrationJournalContracts.Parse(
                Encoding.UTF8.GetBytes(path), Limits).Issues, SpatialContractIssue.MalformedJson);
        }

        [Test]
        public void CompleteJournalTransitionMatrix_IsExact()
        {
            SpatialMigrationJournalStage[] stages = (SpatialMigrationJournalStage[])
                Enum.GetValues(typeof(SpatialMigrationJournalStage));
            foreach (SpatialMigrationJournalStage from in stages)
                foreach (SpatialMigrationJournalStage to in stages)
                    Assert.AreEqual(ExpectedTransition(from, to),
                        SpatialMigrationJournalContracts.IsAllowedTransition(from, to), from + " -> " + to);
            Assert.IsFalse(SpatialMigrationJournalContracts.IsAllowedTransition(
                (SpatialMigrationJournalStage)99, SpatialMigrationJournalStage.Finalized));
        }

        [Test]
        public void SidecarPathGrammarAndContainmentBoundaries_ArePure()
        {
            string transaction = SpatialMigrationTransactionIdentity.CreateTransactionId(H1);
            Assert.IsTrue(SpatialMigrationSidecarPaths.Derive(new string('a', 80) + ".json", transaction).IsValid);
            Assert.IsFalse(SpatialMigrationSidecarPaths.Derive(new string('a', 81) + ".json", transaction).IsValid);
            foreach (string invalid in new[] { "", ".", "..", "a/b", "a\\b", "/root", "C:save.json",
                "https:save", "../save", "save?.json", "save ." })
                Assert.IsFalse(SpatialMigrationSidecarPaths.Derive(invalid, transaction).IsValid, invalid);

            string directory = System.IO.Path.GetFullPath("save-contract-root");
            string expected = System.IO.Path.Combine(directory, "save.json");
            Assert.IsTrue(SpatialMigrationSidecarPaths.TryResolveContained(directory, "save.json",
                expected.Length, out string actual));
            Assert.AreEqual(expected, actual);
            Assert.IsFalse(SpatialMigrationSidecarPaths.TryResolveContained(directory, "save.json",
                expected.Length - 1, out _));
            Assert.IsFalse(SpatialMigrationSidecarPaths.TryResolveContained(directory, "../save-contract-root2/save.json",
                1000, out _));
            Assert.IsTrue(SpatialMigrationSidecarPaths.IsValidRelativeFilename(
                new string('a', SpatialMigrationSidecarPaths.MaximumGeneratedFilenameCharacters),
                SpatialMigrationSidecarPaths.MaximumGeneratedFilenameCharacters));
            Assert.IsFalse(SpatialMigrationSidecarPaths.IsValidRelativeFilename(
                new string('a', SpatialMigrationSidecarPaths.MaximumGeneratedFilenameCharacters + 1),
                SpatialMigrationSidecarPaths.MaximumGeneratedFilenameCharacters));
            Assert.AreEqual(240, SpatialMigrationSidecarPaths.WindowsMaximumAbsolutePathCharacters);
            // Pure lexical validation intentionally defers symlink/reparse-point verification to the executor.
        }

        private static SpatialContractIssue[] ParseLowLevelThroughDescriptorShape(byte[] bytes, int strings)
        {
            var limits = new SpatialSerializedInputLimits(bytes.Length, 100, 10, strings, 10);
            return SpatialMigrationDescriptorContracts.Parse(bytes, limits).Issues;
        }

        private static void AssertIssue(byte[] bytes, SpatialSerializedInputLimits limits,
            SpatialContractIssue issue)
        { CollectionAssert.Contains(SpatialMigrationDescriptorContracts.Parse(bytes, limits).Issues, issue); }

        private static bool ExpectedTransition(SpatialMigrationJournalStage from,
            SpatialMigrationJournalStage to)
        {
            if (from == SpatialMigrationJournalStage.DescriptorPinned)
                return to == SpatialMigrationJournalStage.BackupVerified;
            if (from == SpatialMigrationJournalStage.BackupVerified)
                return to == SpatialMigrationJournalStage.CandidateVerified || to == SpatialMigrationJournalStage.OriginalRestored;
            if (from == SpatialMigrationJournalStage.CandidateVerified)
                return to == SpatialMigrationJournalStage.Replaced || to == SpatialMigrationJournalStage.OriginalRestored;
            if (from == SpatialMigrationJournalStage.Replaced)
                return to == SpatialMigrationJournalStage.DurableVerified || to == SpatialMigrationJournalStage.OriginalRestored;
            if (from == SpatialMigrationJournalStage.DurableVerified)
                return to == SpatialMigrationJournalStage.Finalized || to == SpatialMigrationJournalStage.OriginalRestored;
            return false;
        }

        private static byte[] Replace(byte[] bytes, string oldValue, string newValue)
        { return Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace(oldValue, newValue)); }

        private static DetachedCanonicalSpatialSaveState EmptyState() => new DetachedCanonicalSpatialSaveState
        {
            Authority = new CanonicalSpatialAuthorityMarker
            { CanonicalLayoutContractVersion = 1, CreationKind = CanonicalSpatialCreationKind.NativeCanonical },
            Floors = Array.Empty<SavedSpatialFloor>()
        };

        private static SpatialMigrationInputDescriptor Descriptor() => new SpatialMigrationInputDescriptor(
            H0, 6, SpatialRawEnvelopeClassification.WrappedSaveRoot, 7, 1, 1,
            "compat.profile.migration.schema_1_6_to_7.contract_1", 1, H1,
            "compat.geometry.r1-r2", 1, H2, H1, H2,
            new[] { new SpatialValidationInputHash("validation.limits", H0) }, H1,
            SpatialMigrationContractIdentity.CanonicalSerializerId, 1);

        private static SpatialMigrationSidecarNames Names()
        {
            SpatialMigrationInputDescriptor descriptor = Descriptor();
            string fingerprint = SpatialMigrationDescriptorContracts.ComputeInputFingerprint(descriptor, Limits);
            string identity = SpatialMigrationTransactionIdentity.ComputeIdentity(H0, fingerprint);
            return SpatialMigrationSidecarPaths.Derive("save.json",
                SpatialMigrationTransactionIdentity.CreateTransactionId(identity)).Value;
        }

        private static SpatialMigrationJournal ValidJournal(SpatialMigrationJournalStage stage)
        {
            bool backup = stage != SpatialMigrationJournalStage.DescriptorPinned;
            bool candidate = stage == SpatialMigrationJournalStage.CandidateVerified ||
                stage == SpatialMigrationJournalStage.Replaced ||
                stage == SpatialMigrationJournalStage.DurableVerified ||
                stage == SpatialMigrationJournalStage.Finalized;
            string receipt = stage == SpatialMigrationJournalStage.Finalized ? Names().FinalizedReceipt : null;
            return Journal(stage, backup ? H0 : null, candidate ? H2 : null, receipt);
        }

        private static SpatialMigrationJournal Journal(SpatialMigrationJournalStage stage, string backup,
            string candidate, string receipt)
        {
            SpatialMigrationInputDescriptor descriptor = Descriptor();
            string fingerprint = SpatialMigrationDescriptorContracts.ComputeInputFingerprint(descriptor, Limits);
            string identity = SpatialMigrationTransactionIdentity.ComputeIdentity(H0, fingerprint);
            string transaction = SpatialMigrationTransactionIdentity.CreateTransactionId(identity);
            SpatialMigrationSidecarNames names = SpatialMigrationSidecarPaths.Derive("save.json", transaction).Value;
            return new SpatialMigrationJournal(1, descriptor, fingerprint, identity, transaction, names.Journal,
                names.OriginalBackup, names.CandidateStaging, receipt, H0, backup, candidate, stage);
        }

        private static DetachedCanonicalSpatialSaveState Populated(int roomCount, bool physical)
        {
            string floor = "floor.a";
            RoomSpatialInstance[] rooms = Enumerable.Range(0, roomCount).Select(index => new RoomSpatialInstance
            {
                RoomInstanceId = "room." + (char)('a' + index), RoomDefinitionId = "spatial.room.basic",
                FloorId = floor, Anchor = new TileCoordinate(index, 2 + index * 4),
                Orientation = index == 0 ? CardinalOrientation.Ninety : CardinalOrientation.TwoSeventy
            }).ToArray();
            var nodes = new List<FloorRouteNode>
            { new FloorRouteNode { NodeId = "node.entrance", FloorId = floor, Kind = FloorRouteNodeKind.Entrance, RoomInstanceId = string.Empty } };
            nodes.AddRange(rooms.Select(room => new FloorRouteNode
            { NodeId = room.RoomInstanceId + ".node", FloorId = floor, Kind = FloorRouteNodeKind.Room,
                RoomInstanceId = room.RoomInstanceId }));
            nodes.Add(new FloorRouteNode { NodeId = "node.completion", FloorId = floor,
                Kind = FloorRouteNodeKind.Completion, RoomInstanceId = string.Empty });
            FloorRouteEdge[] edges = Enumerable.Range(0, nodes.Count - 1).Select(index => new FloorRouteEdge
            {
                EdgeId = "edge." + index, FloorId = floor, SourceNodeId = nodes[index].NodeId,
                DestinationNodeId = nodes[index + 1].NodeId, Classification = RouteClassification.Required,
                ConnectionKind = physical && index == 0 ? FloorRouteConnectionKind.PhysicalCorridor : FloorRouteConnectionKind.DirectDoorway,
                CorridorDefinitionId = physical && index == 0 ? "spatial.corridor.straight_stone" : string.Empty,
                Footprint = physical && index == 0 ? new ResolvedTileFootprint
                { OccupiedTiles = new[] { new TileCoordinate(1, 0), new TileCoordinate(2, 0) } } : null,
                OptionalBranchId = string.Empty
            }).ToArray();
            RoomContentAssignment[] assignments = rooms.Select((room, index) => new RoomContentAssignment
            {
                AssignmentId = room.RoomInstanceId + ".content.monster.0001", RoomInstanceId = room.RoomInstanceId,
                CategoryId = CanonicalSpatialSaveContracts.MonsterCategoryId,
                OptionId = "placement.option.monster.goblin", Sequence = (long)index + 1
            }).ToArray();
            return new DetachedCanonicalSpatialSaveState
            {
                Authority = new CanonicalSpatialAuthorityMarker
                { CanonicalLayoutContractVersion = 1, CreationKind = CanonicalSpatialCreationKind.NativeCanonical },
                Floors = new[] { new SavedSpatialFloor
                {
                    FloorInstanceId = floor, FloorDefinitionId = "spatial.floor.01", FloorIndex = 0,
                    Layout = new FloorSpatialLayout { FloorId = floor, Rooms = rooms,
                        Nodes = nodes.ToArray(), Edges = edges },
                    FixedStructures = new[]
                    {
                        new SavedFixedSpatialStructure { FixedStructureInstanceId = "fixed.entrance",
                            FixedStructureDefinitionId = "spatial.fixed.entrance_hall", FloorInstanceId = floor,
                            Anchor = new TileCoordinate(0, 0), Orientation = CardinalOrientation.OneEighty,
                            Kind = FixedSpatialStructureKind.Entrance }
                    },
                    RoomContents = new FloorRoomContentState { Assignments = assignments,
                        RoomSemantics = rooms.Select(room => new CanonicalRoomSemantics
                        { RoomInstanceId = room.RoomInstanceId,
                            LegacyRoomOriginKind = LegacyRoomOriginKind.CanonicalPlayerPlaced }).ToArray(),
                        NextSequence = long.MaxValue - 1 }
                } }
            };
        }
    }
}
