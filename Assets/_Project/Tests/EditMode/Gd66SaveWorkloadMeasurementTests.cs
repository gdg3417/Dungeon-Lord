#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    // Measurement-only evidence. These values are never production configuration authority.
    public sealed class Gd66SaveWorkloadMeasurementTests
    {
        private const int High = 2000000;

        [Test]
        public void RepositoryOwnedMigrationFixtures_EmitExactWorkloadMeasurements()
        {
            var rows = new List<string>();
            for (int schema = 1; schema <= 6; schema++)
            {
                Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                    Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(schema);
                Assert.That(fixture.Result.IsSuccess, Is.True, fixture.Result.Reason);
                rows.Add(Measure("wrapped-empty-v" + schema, fixture.Original,
                    fixture.Classification, fixture.Result.Attempt.Candidate.GetBytes(),
                    ParseState(fixture.Result.Attempt.Candidate.GetBytes(), fixture.Limits)));
            }

            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture unwrapped =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(1, true);
            rows.Add(Measure("unwrapped-v1", unwrapped.Original, unwrapped.Classification,
                unwrapped.Result.Attempt.Candidate.GetBytes(),
                ParseState(unwrapped.Result.Attempt.Candidate.GetBytes(), unwrapped.Limits)));

            const string r1 = "\"mvpRoomSlotAssignments\":{\"Rooms\":[" +
                "{\"FloorIndex\":0,\"RoomIndex\":0,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[\"placement.option.monster.skeleton\"]," +
                "\"TrapOptionIds\":[\"placement.option.trap.spike\"]," +
                "\"LootNodeOptionIds\":[\"placement.option.loot_node.basic\"]}],\"NextRevision\":4}";
            AddSemantic(rows, "populated-r1", r1);

            const string r2 = "\"mvpRoomSlotAssignments\":{\"Rooms\":[" +
                "{\"FloorIndex\":0,\"RoomIndex\":0,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[\"placement.option.monster.skeleton\",\"placement.option.monster.goblin\"]," +
                "\"TrapOptionIds\":[\"placement.option.trap.spike\",\"placement.option.trap.snare\"]," +
                "\"LootNodeOptionIds\":[\"placement.option.loot_node.basic\",\"placement.option.loot_node.hidden_cache\"]}," +
                "{\"FloorIndex\":0,\"RoomIndex\":1,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[\"placement.option.monster.skeleton\",\"placement.option.monster.goblin\"]," +
                "\"TrapOptionIds\":[\"placement.option.trap.spike\",\"placement.option.trap.snare\"]," +
                "\"LootNodeOptionIds\":[\"placement.option.loot_node.basic\",\"placement.option.loot_node.hidden_cache\"]}],\"NextRevision\":13}";
            AddSemantic(rows, "maximum-content-r2", r2);

            const string implicitContainer = "\"mvpDungeonPlacements\":{\"Entries\":[" +
                "{\"CategoryId\":\"placement.category.monster\",\"OptionId\":\"placement.option.monster.skeleton\",\"Revision\":1}," +
                "{\"CategoryId\":\"placement.category.trap\",\"OptionId\":\"placement.option.trap.spike\",\"Revision\":2}," +
                "{\"CategoryId\":\"placement.category.loot_node\",\"OptionId\":\"placement.option.loot_node.basic\",\"Revision\":3}],\"NextRevision\":4}";
            AddSemantic(rows, "implicit-content-container", implicitContainer);

            string outcomes = string.Join(",", Enumerable.Range(1, 10).Select(index =>
                "{\"RunId\":\"run-" + index + "\",\"TickStarted\":" + index +
                ",\"Success\":true,\"Score\":" + (index * 10) +
                ",\"ReasonKey\":\"run.result.success\",\"FeedbackTagKeys\":[]," +
                "\"LootBreakdown\":[],\"RoomResolutions\":[]}"));
            string independent = "\"dungeonLayout\":{\"FloorCount\":1,\"SlotsPerFloor\":4,\"Slots\":[]}," +
                "\"structureRuntime\":{\"ManaReserve\":123.5,\"Heat\":17.25}," +
                "\"runHistory\":{\"NextRunSequence\":11,\"LatestOutcome\":null,\"RecentOutcomes\":[" + outcomes + "]}," +
                "\"researchPending\":{\"SlotId\":\"research.slot.1\",\"ProjectId\":\"research.project.sample\"}," +
                "\"researchProgress\":null,\"completedResearch\":{\"ProjectIds\":[]}," +
                "\"completedObjectives\":{\"ObjectiveIds\":[\"objective.mvp.first_session\"]}," +
                "\"lastOfflineSummary\":{\"RuleResolved\":true},\"lastPausedUtcUnix\":10,\"lastResumedUtcUnix\":20";
            AddSemantic(rows, "independent-state-and-history", r1 + "," + independent);

            const string unknownJson = "{\"rootBefore\":[1,{\"x\":true}],\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"saveVersion\":6,\"unknownPrimary\":{\"note\":\"preserve\"}},\"rootAfter\":false}";
            byte[] unknownBytes = Encoding.UTF8.GetBytes(unknownJson);
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture unknown =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, unknownBytes);
            Assert.That(unknown.Result.IsSuccess, Is.True, unknown.Result.Reason);
            rows.Add(Measure("unknown-root-primary", unknown.Original, unknown.Classification,
                unknown.Result.Attempt.Candidate.GetBytes(),
                ParseState(unknown.Result.Attempt.Candidate.GetBytes(), unknown.Limits)));

            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture sidecarFixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            DetachedPreparedSpatialMigrationAttempt attempt = sidecarFixture.Result.Attempt;
            SpatialContractResult<byte[]> descriptorBytes = SpatialMigrationDescriptorContracts.Serialize(
                attempt.Descriptor, sidecarFixture.Limits.Serialized);
            Assert.That(descriptorBytes.IsValid, Is.True);
            rows.Add(MeasureArtifact("descriptor", descriptorBytes.Value));
            string identity = attempt.TransactionIdentity;
            SpatialMigrationSidecarNames names = SpatialMigrationSidecarPaths.Derive(
                "save.json", attempt.TransactionId).Value;
            foreach (SpatialMigrationJournalStage stage in Enum.GetValues(typeof(SpatialMigrationJournalStage)))
            {
                bool hasBackup = stage != SpatialMigrationJournalStage.DescriptorPinned;
                bool hasCandidate = stage == SpatialMigrationJournalStage.CandidateVerified ||
                    stage == SpatialMigrationJournalStage.Replaced ||
                    stage == SpatialMigrationJournalStage.DurableVerified ||
                    stage == SpatialMigrationJournalStage.Finalized;
                var journal = new SpatialMigrationJournal(SpatialMigrationContractIdentity.JournalSchemaVersion,
                    attempt.Descriptor, attempt.DescriptorFingerprint, identity, attempt.TransactionId,
                    names.Journal, names.OriginalBackup, names.CandidateStaging,
                    stage == SpatialMigrationJournalStage.Finalized ? names.FinalizedReceipt : null,
                    attempt.Descriptor.OriginalPayloadSha256,
                    hasBackup ? attempt.Descriptor.OriginalPayloadSha256 : null,
                    hasCandidate ? attempt.CandidateSha256 : null, stage);
                SpatialContractResult<byte[]> journalBytes = SpatialMigrationJournalContracts.Serialize(
                    journal, sidecarFixture.Limits.Serialized);
                Assert.That(journalBytes.IsValid, Is.True, stage.ToString());
                rows.Add(MeasureArtifact("journal-" + stage, journalBytes.Value));
            }
            byte[] receipt = DetachedFinalizationReceiptContract.Serialize(
                new DetachedFinalizationReceipt(attempt.TransactionId, attempt.DescriptorFingerprint,
                    attempt.CandidateSha256), sidecarFixture.Limits.Serialized);
            rows.Add(MeasureArtifact("finalization-receipt", receipt));
            byte[] restoration = DetachedRestorationIntentContract.Serialize(
                new DetachedRestorationIntent(attempt.TransactionId, attempt.DescriptorFingerprint,
                    attempt.Descriptor.OriginalPayloadSha256, attempt.Descriptor.OriginalPayloadSha256,
                    names.Journal, (int)SpatialMigrationJournalStage.DurableVerified),
                sidecarFixture.Limits.Serialized);
            rows.Add(MeasureArtifact("restoration-intent", restoration));

            foreach (string row in rows) TestContext.Progress.WriteLine("GD66_LIMIT_MEASUREMENT " + row);
            Assert.That(rows.Count, Is.EqualTo(22));
        }

        [Test]
        public void ProposedCrossFieldProfile_HasReachableWholeCandidateBoundary()
        {
            const int serializedInput = 262144;
            const int candidate = 262144;
            Assert.That(candidate, Is.LessThanOrEqualTo(serializedInput),
                "Complete candidate parsing makes a larger candidate success boundary unreachable.");

            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            byte[] bytes = fixture.Result.Attempt.Candidate.GetBytes();
            Assert.That(bytes.Length, Is.LessThanOrEqualTo(candidate));
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes,
                new CanonicalSpatialSerializationLimits(
                    new SpatialSerializedInputLimits(serializedInput, 8192, 2048, 131072, 64),
                    new CanonicalSpatialSaveWorkloadLimits(64, 64))).IsValid, Is.True);
        }

        private static void AddSemantic(List<string> rows, string name, string members)
        {
            Gd66DetachedSpatialMigrationTransactionTests.SemanticFixtureExecution fixture =
                Gd66DetachedSpatialMigrationTransactionTests.RunPopulatedSemanticFixture(name, 6, members);
            rows.Add(Measure(name, fixture.Attempt.GetOriginalBytes(), fixture.Classification,
                fixture.Attempt.Candidate.GetBytes(), fixture.State));
        }

        private static DetachedCanonicalSpatialSaveState ParseState(byte[] candidate,
            CanonicalSpatialSerializationLimits limits)
        {
            DetachedCompleteSaveValidationResult result =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(candidate, limits);
            Assert.That(result.IsValid, Is.True, result.Reason);
            return result.State;
        }

        private static string MeasureArtifact(string name, byte[] bytes) => name +
            ":strictBytes=" + bytes.Length + ",strictNodes=" + MinimumContract(bytes, 0) +
            ",strictRecords=" + MinimumContract(bytes, 1) +
            ",strictStringChars=" + MinimumContract(bytes, 2);

        private static int MinimumContract(byte[] bytes, int dimension) => Minimum(limit =>
        {
            var limits = new SpatialSerializedInputLimits(bytes.Length,
                dimension == 0 ? limit : High, dimension == 1 ? limit : High,
                dimension == 2 ? limit : High, 64);
            var issues = new SpatialIssueCollector(64);
            return ContractJson.TryParse(bytes, limits, issues, out _);
        });

        private static string Measure(string name, byte[] raw, RawSavePayloadClassification classification,
            byte[] candidate, DetachedCanonicalSpatialSaveState state)
        {
            int copied = classification.Members.Where(value => value.State != RawSaveMemberState.Absent)
                .Sum(value => value.ByteLength);
            int unknownCount = classification.UnknownRootMembers.Count + classification.UnknownPrimaryMembers.Count;
            int unknownBytes = classification.UnknownRootMembers.Sum(value => value.ByteLength) +
                classification.UnknownPrimaryMembers.Sum(value => value.ByteLength);
            int records = CountCanonicalRecords(state);
            int tiles = CountCanonicalTiles(state);
            string rawMetrics = raw == null ? "raw=not-retained" :
                "rawBytes=" + raw.Length + ",rawDepth=" + MinimumRaw(raw, 0) +
                ",rawMembers=" + MinimumRaw(raw, 1) + ",rawElements=" + MinimumRaw(raw, 2) +
                ",rawStringBytes=" + MinimumRaw(raw, 3) + ",rawScanWork=" + MinimumRaw(raw, 4);
            return name + ":" + rawMetrics + ",candidateBytes=" + candidate.Length +
                ",strictNodes=" + MinimumStrict(candidate, state, 0) +
                ",strictRecords=" + MinimumStrict(candidate, state, 1) +
                ",strictStringChars=" + MinimumStrict(candidate, state, 2) +
                ",canonicalRecords=" + records + ",canonicalTiles=" + tiles +
                ",copiedBytes=" + copied + ",unknownCount=" + unknownCount +
                ",unknownBytes=" + unknownBytes;
        }

        private static int MinimumRaw(byte[] bytes, int dimension) => Minimum(limit =>
        {
            int depth = dimension == 0 ? limit : 256;
            int members = dimension == 1 ? limit : High;
            int elements = dimension == 2 ? limit : High;
            int strings = dimension == 3 ? limit : High;
            int work = dimension == 4 ? limit : High;
            return RawSavePayloadClassifier.Classify(bytes,
                new RawSavePayloadClassificationLimits(bytes.Length, depth, members, elements, strings, work),
                new RawSaveEnvelopeVersionContract(1, 6), BlankFloor()).IsSuccess;
        });

        private static int MinimumStrict(byte[] bytes, DetachedCanonicalSpatialSaveState state, int dimension) =>
            Minimum(limit => DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes,
                new CanonicalSpatialSerializationLimits(new SpatialSerializedInputLimits(bytes.Length,
                    dimension == 0 ? limit : High, dimension == 1 ? limit : High,
                    dimension == 2 ? limit : High, 64),
                    new CanonicalSpatialSaveWorkloadLimits(Math.Max(1, CountCanonicalRecords(state)),
                        Math.Max(1, CountCanonicalTiles(state))))).IsValid);

        private static int Minimum(Func<int, bool> accepts)
        {
            if (accepts(0)) return 0;
            int low = 0, high = 1;
            while (high < High && !accepts(high)) high *= 2;
            Assert.That(accepts(high), Is.True, "Measurement ceiling was insufficient.");
            while (low + 1 < high)
            {
                int middle = low + (high - low) / 2;
                if (accepts(middle)) high = middle; else low = middle;
            }
            return high;
        }

        private static int CountCanonicalRecords(DetachedCanonicalSpatialSaveState state) =>
            (state?.Floors ?? Array.Empty<SavedSpatialFloor>()).Sum(floor => floor == null ? 1 : 1 +
                (floor.Layout?.Rooms?.Length ?? 0) + (floor.Layout?.Nodes?.Length ?? 0) +
                (floor.Layout?.Edges?.Length ?? 0) + (floor.FixedStructures?.Length ?? 0) +
                (floor.RoomContents?.Assignments?.Length ?? 0) +
                (floor.RoomContents?.RoomSemantics?.Length ?? 0));

        private static int CountCanonicalTiles(DetachedCanonicalSpatialSaveState state) =>
            (state?.Floors ?? Array.Empty<SavedSpatialFloor>()).Where(floor => floor?.Layout?.Edges != null)
                .SelectMany(floor => floor.Layout.Edges).Sum(edge => edge?.Footprint?.OccupiedTiles?.Length ?? 0);

        private static RawLegacyBlankFloorContract BlankFloor() => new RawLegacyBlankFloorContract(1,
            Enumerable.Range(0, 4).Select(index => new RawLegacyBlankFloorNodeContract(
                0, index, "slot." + index, "", "", 0)), true, true,
            new[] { "Nodes", "NextRevision" },
            new[] { "FloorIndex", "NodeIndex", "SlotId", "CategoryId", "OptionId", "Revision" });
    }
}
#endif
