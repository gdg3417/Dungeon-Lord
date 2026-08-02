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

        [Test]
        public void AuthorityMarker_RequiresExactNativeAndMigratedIdentities()
        {
            AssertAuthority(CanonicalSpatialCreationKind.NativeCanonical, null, null, true);
            AssertAuthority(CanonicalSpatialCreationKind.NativeCanonical, "stable.id", null, false);
            AssertAuthority(CanonicalSpatialCreationKind.NativeCanonical, null, H1, false);
            string transaction = SpatialMigrationTransactionIdentity.CreateTransactionId(H2);
            AssertAuthority(CanonicalSpatialCreationKind.Migrated, transaction, H1, true);
            AssertAuthority(CanonicalSpatialCreationKind.Migrated, null, null, false);
            AssertAuthority(CanonicalSpatialCreationKind.Migrated, transaction, null, false);
            AssertAuthority(CanonicalSpatialCreationKind.Migrated, null, H1, false);
            AssertAuthority(CanonicalSpatialCreationKind.Migrated, "wrong-" + H2, H1, false);
            AssertAuthority(CanonicalSpatialCreationKind.Migrated, "gd66-" + new string('A', 64), H1, false);
            AssertAuthority(CanonicalSpatialCreationKind.Migrated, "gd66-" + H2.Substring(1), H1, false);
            AssertAuthority(CanonicalSpatialCreationKind.Migrated, "stable.looking.id", H1, false);
            AssertAuthority(CanonicalSpatialCreationKind.Migrated, transaction, new string('A', 64), false);
            AssertAuthority(CanonicalSpatialCreationKind.Migrated, transaction, H1.Substring(1), false);
            AssertAuthority(CanonicalSpatialCreationKind.Migrated, transaction, "stable.looking.id", false);
        }

        [Test]
        public void DeepJson_UsesExplicitStackAtExactNodeBoundaryAndOneOver()
        {
            const int depth = 512;
            string json = new string('[', depth) + "0" + new string(']', depth);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            var exact = new SpatialSerializedInputLimits(bytes.Length, depth + 1, depth, 0, 10);
            SpatialContractResult<SpatialMigrationInputDescriptor> exactResult =
                SpatialMigrationDescriptorContracts.Parse(bytes, exact);
            Assert.IsFalse(exactResult.IsValid);
            CollectionAssert.Contains(exactResult.Issues, SpatialContractIssue.WrongFieldType);
            var oneOver = new SpatialSerializedInputLimits(bytes.Length, depth, depth, 0, 10);
            SpatialContractResult<SpatialMigrationInputDescriptor> overResult =
                SpatialMigrationDescriptorContracts.Parse(bytes, oneOver);
            Assert.IsFalse(overResult.IsValid);
            CollectionAssert.Contains(overResult.Issues, SpatialContractIssue.WorkloadExceeded);
        }

        [Test]
        public void SerializationBudgets_EnforceNodesRecordsAndStringsForEveryContract()
        {
            AssertSerializationBoundaries(
                limits => CanonicalSpatialSaveSerializer.Serialize(Populated(2, true),
                    new CanonicalSpatialSerializationLimits(limits, SaveLimits.Spatial)).IsValid);
            AssertSerializationBoundaries(
                limits => SpatialMigrationDescriptorContracts.Serialize(Descriptor(), limits).IsValid);
            AssertSerializationBoundaries(
                limits => SpatialMigrationJournalContracts.Serialize(
                    ValidJournal(SpatialMigrationJournalStage.Finalized), limits).IsValid);
        }

        [Test]
        public void SerializationByteBudgets_AreExactForAllContracts()
        {
            byte[] spatial = CanonicalSpatialSaveSerializer.Serialize(Populated(1, false), SaveLimits).Value;
            Assert.IsTrue(CanonicalSpatialSaveSerializer.Serialize(Populated(1, false),
                new CanonicalSpatialSerializationLimits(
                    new SpatialSerializedInputLimits(spatial.Length, 20000, 2000, 20000, 10),
                    SaveLimits.Spatial)).IsValid);
            CollectionAssert.Contains(CanonicalSpatialSaveSerializer.Serialize(Populated(1, false),
                new CanonicalSpatialSerializationLimits(
                    new SpatialSerializedInputLimits(spatial.Length - 1, 20000, 2000, 20000, 10),
                    SaveLimits.Spatial)).Issues, SpatialContractIssue.InputByteLimitExceeded);

            byte[] descriptor = SpatialMigrationDescriptorContracts.Serialize(Descriptor(), Limits).Value;
            Assert.IsTrue(SpatialMigrationDescriptorContracts.Serialize(Descriptor(),
                new SpatialSerializedInputLimits(descriptor.Length, 20000, 2000, 20000, 10)).IsValid);
            CollectionAssert.Contains(SpatialMigrationDescriptorContracts.Serialize(Descriptor(),
                new SpatialSerializedInputLimits(descriptor.Length - 1, 20000, 2000, 20000, 10)).Issues,
                SpatialContractIssue.InputByteLimitExceeded);

            SpatialMigrationJournal journal = ValidJournal(SpatialMigrationJournalStage.Finalized);
            byte[] journalBytes = SpatialMigrationJournalContracts.Serialize(journal, Limits).Value;
            Assert.IsTrue(SpatialMigrationJournalContracts.Serialize(journal,
                new SpatialSerializedInputLimits(journalBytes.Length, 20000, 2000, 20000, 10)).IsValid);
            CollectionAssert.Contains(SpatialMigrationJournalContracts.Serialize(journal,
                new SpatialSerializedInputLimits(journalBytes.Length - 1, 20000, 2000, 20000, 10)).Issues,
                SpatialContractIssue.InputByteLimitExceeded);
        }

        [Test]
        public void JournalDescriptor_IsParsedOnceAtExactCombinedWorkloadBoundaries()
        {
            SpatialMigrationJournal journal = ValidJournal(SpatialMigrationJournalStage.Finalized);
            const int high = 200000;
            int nodes = Minimum(limit => SpatialMigrationJournalContracts.Serialize(journal,
                new SpatialSerializedInputLimits(high, limit, high, high, 10)).IsValid);
            int records = Minimum(limit => SpatialMigrationJournalContracts.Serialize(journal,
                new SpatialSerializedInputLimits(high, high, limit, high, 10)).IsValid);
            int strings = Minimum(limit => SpatialMigrationJournalContracts.Serialize(journal,
                new SpatialSerializedInputLimits(high, high, high, limit, 10)).IsValid);
            var exact = new SpatialSerializedInputLimits(high, nodes, records, strings, 10);
            byte[] bytes = SpatialMigrationJournalContracts.Serialize(journal, exact).Value;
            Assert.IsTrue(SpatialMigrationJournalContracts.Parse(bytes, exact).IsValid);
            var oneNodeOver = new SpatialSerializedInputLimits(high, nodes - 1, records, strings, 10);
            CollectionAssert.Contains(SpatialMigrationJournalContracts.Parse(bytes, oneNodeOver).Issues,
                SpatialContractIssue.WorkloadExceeded);
        }

        [Test]
        public void WindowsReservedDeviceNames_AreRejectedPlatformIndependently()
        {
            var names = new List<string> { "CON", "PRN", "AUX", "NUL" };
            for (int index = 1; index <= 9; index++)
            { names.Add("COM" + index); names.Add("LPT" + index); }
            string transaction = SpatialMigrationTransactionIdentity.CreateTransactionId(H1);
            foreach (string name in names)
            {
                Assert.IsFalse(SpatialMigrationSidecarPaths.IsValidRelativeFilename(name, 180), name);
                Assert.IsFalse(SpatialMigrationSidecarPaths.IsValidRelativeFilename(name + ".json", 180), name);
                Assert.IsFalse(SpatialMigrationSidecarPaths.Derive(name + ".json", transaction).IsValid, name);
            }
            Assert.IsFalse(SpatialMigrationSidecarPaths.IsValidRelativeFilename("Con.json", 180));
            Assert.IsTrue(SpatialMigrationSidecarPaths.IsValidRelativeFilename("console.json", 180));
            Assert.IsTrue(SpatialMigrationSidecarPaths.IsValidRelativeFilename("com10.json", 180));
        }

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
            SavedSpatialFloor[] floorArray = source.Floors;
            FloorSpatialLayout layout = source.Floors[0].Layout;
            RoomSpatialInstance[] sourceRooms = layout.Rooms;
            FloorRouteNode[] sourceNodes = layout.Nodes;
            FloorRouteEdge[] sourceEdges = layout.Edges;
            SavedFixedSpatialStructure[] sourceFixed = source.Floors[0].FixedStructures;
            RoomContentAssignment[] sourceAssignments = source.Floors[0].RoomContents.Assignments;
            CanonicalRoomSemantics[] sourceSemantics = source.Floors[0].RoomContents.RoomSemantics;
            ResolvedTileFootprint footprint = physicalCorridor ? sourceEdges[0].Footprint : null;
            TileCoordinate[] tiles = footprint == null ? null : footprint.OccupiedTiles;
            string snapshot = Snapshot(source);
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
            Assert.AreSame(floorArray, source.Floors);
            Assert.AreSame(layout, source.Floors[0].Layout);
            Assert.AreSame(sourceRooms, layout.Rooms);
            Assert.AreSame(sourceNodes, layout.Nodes);
            Assert.AreSame(sourceEdges, layout.Edges);
            Assert.AreSame(sourceFixed, source.Floors[0].FixedStructures);
            Assert.AreSame(sourceAssignments, source.Floors[0].RoomContents.Assignments);
            Assert.AreSame(sourceSemantics, source.Floors[0].RoomContents.RoomSemantics);
            if (physicalCorridor)
            { Assert.AreSame(footprint, sourceEdges[0].Footprint); Assert.AreSame(tiles, footprint.OccupiedTiles); }
            Assert.AreEqual(snapshot, Snapshot(source));
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
        public void DescriptorEveryFieldMutation_ChangesCanonicalBytesAndFingerprint()
        {
            byte[] baseline = SpatialMigrationDescriptorContracts.Serialize(Descriptor(), Limits).Value;
            string json = Encoding.UTF8.GetString(baseline);
            var mutations = new[]
            {
                new[] { "\"OriginalPayloadSha256\":\"" + H0, "\"OriginalPayloadSha256\":\"" + H2 },
                new[] { "\"RawSourceSchemaVersion\":6", "\"RawSourceSchemaVersion\":5" },
                new[] { "\"RawEnvelopeClassification\":1", "\"RawEnvelopeClassification\":2" },
                new[] { "\"SelectedTargetSchemaVersion\":7", "\"SelectedTargetSchemaVersion\":8" },
                new[] { "\"AuthorityMarkerContractVersion\":1", "\"AuthorityMarkerContractVersion\":2" },
                new[] { "\"MigrationContractVersion\":1", "\"MigrationContractVersion\":2" },
                new[] { "compat.profile.migration.schema_1_6_to_7.contract_1", "compat.profile.changed" },
                new[] { "\"MigrationProfileVersion\":1", "\"MigrationProfileVersion\":2" },
                new[] { "\"MigrationProfileCanonicalHash\":\"" + H1, "\"MigrationProfileCanonicalHash\":\"" + H2 },
                new[] { "compat.geometry.r1-r2", "compat.geometry.changed" },
                new[] { "\"SharedGeometryVersion\":1", "\"SharedGeometryVersion\":2" },
                new[] { "\"SharedGeometryCanonicalHash\":\"" + H2, "\"SharedGeometryCanonicalHash\":\"" + H0 },
                new[] { "\"ProductionManifestSha256\":\"" + H1, "\"ProductionManifestSha256\":\"" + H0 },
                new[] { "\"ProductionCatalogSha256\":\"" + H2, "\"ProductionCatalogSha256\":\"" + H0 },
                new[] { "validation.limits", "validation.changed" },
                new[] { "\"Sha256\":\"" + H0, "\"Sha256\":\"" + H2 },
                new[] { "\"LegacyGameplayConfigurationSha256\":\"" + H1, "\"LegacyGameplayConfigurationSha256\":\"" + H0 },
                new[] { SpatialMigrationContractIdentity.CanonicalSerializerId, "gd66.serializer.changed" },
                new[] { "\"CanonicalSerializerVersion\":1", "\"CanonicalSerializerVersion\":2" }
            };
            string baselineFingerprint = SpatialContractSha256.Compute(baseline);
            foreach (string[] mutation in mutations)
            {
                string changed = json.Replace(mutation[0], mutation[1]);
                Assert.AreNotEqual(json, changed, mutation[0]);
                byte[] changedBytes = Encoding.UTF8.GetBytes(changed);
                Assert.IsFalse(baseline.SequenceEqual(changedBytes), mutation[0]);
                Assert.AreNotEqual(baselineFingerprint, SpatialContractSha256.Compute(changedBytes), mutation[0]);
            }
            string transactionIdentity = SpatialMigrationTransactionIdentity.ComputeIdentity(H0, baselineFingerprint);
            Assert.AreNotEqual(H0, baselineFingerprint);
            Assert.AreNotEqual(H0, transactionIdentity);
            Assert.AreNotEqual(baselineFingerprint, transactionIdentity);
        }

        [Test]
        public void DescriptorNoncanonicalValidationRecordOrder_IsRejected()
        {
            var descriptor = new SpatialMigrationInputDescriptor(H0, 6,
                SpatialRawEnvelopeClassification.WrappedSaveRoot, 7, 1, 1,
                "compat.profile.migration.schema_1_6_to_7.contract_1", 1, H1,
                "compat.geometry.r1-r2", 1, H2, H1, H2,
                new[] { new SpatialValidationInputHash("validation.a", H0),
                    new SpatialValidationInputHash("validation.z", H1) }, H1,
                SpatialMigrationContractIdentity.CanonicalSerializerId, 1);
            string json = Encoding.UTF8.GetString(
                SpatialMigrationDescriptorContracts.Serialize(descriptor, Limits).Value);
            string first = "{\"InputId\":\"validation.a\",\"Sha256\":\"" + H0 + "\"}";
            string second = "{\"InputId\":\"validation.z\",\"Sha256\":\"" + H1 + "\"}";
            string reversed = json.Replace(first + "," + second, second + "," + first);
            Assert.IsFalse(SpatialMigrationDescriptorContracts.Parse(
                Encoding.UTF8.GetBytes(reversed), Limits).IsValid);
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

        private static void AssertAuthority(CanonicalSpatialCreationKind kind, string transaction,
            string fingerprint, bool expected)
        {
            DetachedCanonicalSpatialSaveState state = EmptyState();
            state.Authority.CreationKind = kind;
            state.Authority.MigrationTransactionId = transaction;
            state.Authority.MigrationDescriptorFingerprint = fingerprint;
            SpatialContractResult<byte[]> serialized = CanonicalSpatialSaveSerializer.Serialize(state, SaveLimits);
            Assert.AreEqual(expected, serialized.IsValid, kind + " serialize " + transaction + " " + fingerprint);
            byte[] valid = CanonicalSpatialSaveSerializer.Serialize(EmptyState(), SaveLimits).Value;
            string json = Encoding.UTF8.GetString(valid)
                .Replace("\"CreationKind\":1", "\"CreationKind\":" + (int)kind)
                .Replace("\"MigrationTransactionId\":null", "\"MigrationTransactionId\":" + JsonValue(transaction))
                .Replace("\"MigrationDescriptorFingerprint\":null",
                    "\"MigrationDescriptorFingerprint\":" + JsonValue(fingerprint));
            Assert.AreEqual(expected, CanonicalSpatialSaveSerializer.Parse(
                Encoding.UTF8.GetBytes(json), SaveLimits).IsValid, kind + " parse");
        }

        private static string JsonValue(string value) => value == null ? "null" : "\"" + value + "\"";

        private static void AssertSerializationBoundaries(Func<SpatialSerializedInputLimits, bool> serialize)
        {
            const int high = 200000;
            int nodes = Minimum(limit => serialize(new SpatialSerializedInputLimits(high, limit, high, high, 10)));
            int records = Minimum(limit => serialize(new SpatialSerializedInputLimits(high, high, limit, high, 10)));
            int strings = Minimum(limit => serialize(new SpatialSerializedInputLimits(high, high, high, limit, 10)));
            Assert.IsTrue(serialize(new SpatialSerializedInputLimits(high, nodes, records, strings, 10)));
            Assert.IsFalse(serialize(new SpatialSerializedInputLimits(high, nodes - 1, records, strings, 10)));
            if (records > 0) Assert.IsFalse(serialize(new SpatialSerializedInputLimits(high, nodes, records - 1, strings, 10)));
            Assert.IsFalse(serialize(new SpatialSerializedInputLimits(high, nodes, records, strings - 1, 10)));
        }

        private static int Minimum(Func<int, bool> accepts)
        {
            int low = 0, high = 200000;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (accepts(middle)) high = middle; else low = middle + 1;
            }
            return low;
        }

        private static string Snapshot(DetachedCanonicalSpatialSaveState state)
        {
            var text = new StringBuilder();
            CanonicalSpatialAuthorityMarker authority = state.Authority;
            text.Append(authority.CanonicalLayoutContractVersion).Append('|').Append((int)authority.CreationKind)
                .Append('|').Append(authority.MigrationTransactionId).Append('|')
                .Append(authority.MigrationDescriptorFingerprint);
            foreach (SavedSpatialFloor floor in state.Floors)
            {
                text.Append("|F:").Append(floor.FloorInstanceId).Append(':').Append(floor.FloorDefinitionId)
                    .Append(':').Append(floor.FloorIndex).Append(':').Append(floor.Layout.FloorId);
                foreach (RoomSpatialInstance room in floor.Layout.Rooms)
                    text.Append("|R:").Append(room.RoomInstanceId).Append(':').Append(room.RoomDefinitionId)
                        .Append(':').Append(room.FloorId).Append(':').Append(room.Anchor.X).Append(':')
                        .Append(room.Anchor.Y).Append(':').Append((int)room.Orientation);
                foreach (FloorRouteNode node in floor.Layout.Nodes)
                    text.Append("|N:").Append(node.NodeId).Append(':').Append(node.FloorId).Append(':')
                        .Append((int)node.Kind).Append(':').Append(node.RoomInstanceId);
                foreach (FloorRouteEdge edge in floor.Layout.Edges)
                {
                    text.Append("|E:").Append(edge.EdgeId).Append(':').Append(edge.CorridorDefinitionId)
                        .Append(':').Append(edge.FloorId).Append(':').Append(edge.SourceNodeId).Append(':')
                        .Append(edge.DestinationNodeId).Append(':').Append((int)edge.Classification).Append(':')
                        .Append(edge.OptionalBranchId).Append(':').Append((int)edge.ConnectionKind);
                    if (edge.Footprint != null) foreach (TileCoordinate tile in edge.Footprint.OccupiedTiles)
                        text.Append(':').Append(tile.X).Append(',').Append(tile.Y);
                }
                foreach (SavedFixedSpatialStructure fixedStructure in floor.FixedStructures)
                    text.Append("|X:").Append(fixedStructure.FixedStructureInstanceId).Append(':')
                        .Append(fixedStructure.FixedStructureDefinitionId).Append(':')
                        .Append(fixedStructure.FloorInstanceId).Append(':').Append(fixedStructure.Anchor.X)
                        .Append(':').Append(fixedStructure.Anchor.Y).Append(':')
                        .Append((int)fixedStructure.Orientation).Append(':').Append((int)fixedStructure.Kind);
                foreach (RoomContentAssignment assignment in floor.RoomContents.Assignments)
                    text.Append("|A:").Append(assignment.AssignmentId).Append(':').Append(assignment.RoomInstanceId)
                        .Append(':').Append(assignment.CategoryId).Append(':').Append(assignment.OptionId)
                        .Append(':').Append(assignment.Sequence);
                foreach (CanonicalRoomSemantics semantics in floor.RoomContents.RoomSemantics)
                    text.Append("|S:").Append(semantics.RoomInstanceId).Append(':')
                        .Append((int)semantics.LegacyRoomOriginKind);
                text.Append("|Q:").Append(floor.RoomContents.NextSequence);
            }
            return text.ToString();
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
