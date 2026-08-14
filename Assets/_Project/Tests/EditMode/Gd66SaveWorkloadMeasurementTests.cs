#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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

            SaveData runHistorySave = RepresentativeLegacySaveWithTenPersistedRuns();
            AddSerializedSave(rows, "legacy-runtime-ten-run-history", runHistorySave);
            SaveData canonicalTargetRunHistory = CanonicalTargetSaveWithTenPersistedRuns();
            AddSerializedSave(rows, "canonical-target-ten-run-history", canonicalTargetRunHistory);

            ContentBootstrap bootstrap = ResearchBootstrap();
            SaveData pendingResearch = RepresentativeSave();
            pendingResearch.researchPending = Pending(bootstrap);
            Assert.That(ResearchPendingResolver.Resolve(pendingResearch.researchPending,
                bootstrap.researchPendingScaffold).RuleResolved, Is.True);
            AddSerializedSave(rows, "research-pending", pendingResearch);

            SaveData activeResearch = RepresentativeSave();
            activeResearch.researchPending = Pending(bootstrap);
            activeResearch.researchProgress = Progress(bootstrap,
                bootstrap.researchCompletionEligibilityScaffold.requiredProgressUnits / 2d, false);
            Assert.That(ResearchProgressStateResolver.Resolve(activeResearch.researchPending,
                activeResearch.researchProgress).RuleResolved, Is.True);
            ResearchCompletionEligibilitySummary activeEligibility =
                ResearchCompletionEligibilityResolver.Resolve(activeResearch.researchPending,
                    activeResearch.researchProgress, bootstrap.researchCompletionEligibilityScaffold);
            Assert.That(activeEligibility.RuleResolved, Is.True);
            Assert.That(activeEligibility.EligibleForCompletion, Is.False);
            AddSerializedSave(rows, "research-active-progress", activeResearch);

            SaveData completionPending = RepresentativeSave();
            completionPending.researchPending = Pending(bootstrap);
            completionPending.researchProgress = Progress(bootstrap,
                bootstrap.researchCompletionEligibilityScaffold.requiredProgressUnits, true);
            ResearchCompletionClaimReadinessSummary readiness =
                ResearchCompletionClaimReadinessResolver.Resolve(completionPending.researchPending,
                    completionPending.researchProgress, bootstrap.researchCompletionEligibilityScaffold);
            Assert.That(readiness.RuleResolved, Is.True);
            Assert.That(readiness.ReadyForClaim, Is.True);
            AddSerializedSave(rows, "research-completion-pending", completionPending);

            SaveData completed = RepresentativeSave();
            completed.completedResearch = new CompletedResearchState
            {
                ProjectIds = new[] { bootstrap.researchPendingScaffold.projectId },
                LastCompletedProjectId = bootstrap.researchPendingScaffold.projectId,
                LastCompletionRuleSourceId = bootstrap.researchCompletionClaimScaffold.ruleSourceId
            };
            completed.completedObjectives = CompletedObjective();
            Assert.That(CompletedResearchStateResolver.Resolve(completed.completedResearch)
                .RuleResolved, Is.True);
            AddSerializedSave(rows, "research-and-objective-completed", completed);

            SaveData activeHighWater = CanonicalTargetSaveWithTenPersistedRuns();
            activeHighWater.researchPending = Pending(bootstrap);
            activeHighWater.researchProgress = Progress(bootstrap,
                bootstrap.researchCompletionEligibilityScaffold.requiredProgressUnits / 2d, false);
            Assert.That(ResearchProgressStateResolver.Resolve(activeHighWater.researchPending,
                activeHighWater.researchProgress).RuleResolved, Is.True);
            AddSerializedSave(rows, "canonical-target-full-save-high-water-active-research", activeHighWater);

            SaveData completedHighWater = CanonicalTargetSaveWithTenPersistedRuns();
            completedHighWater.completedResearch = completed.completedResearch;
            completedHighWater.completedObjectives = CompletedObjective();
            Assert.That(CompletedResearchStateResolver.Resolve(completedHighWater.completedResearch)
                .RuleResolved, Is.True);
            AddSerializedSave(rows, "canonical-target-full-save-high-water-completed-research-objective",
                completedHighWater);

            const string unknownJson = "{\"rootBefore\":[1,{\"x\":true}],\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"saveVersion\":6,\"unknownPrimary\":{\"note\":\"preserve\"}},\"rootAfter\":false}";
            byte[] unknownBytes = Encoding.UTF8.GetBytes(unknownJson);
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture unknown =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, unknownBytes);
            Assert.That(unknown.Result.IsSuccess, Is.True, unknown.Result.Reason);
            rows.Add(Measure("unknown-root-primary", unknown.Original, unknown.Classification,
                unknown.Result.Attempt.Candidate.GetBytes(),
                ParseState(unknown.Result.Attempt.Candidate.GetBytes(), unknown.Limits)));

            AddNativeCanonical(rows, unknown);

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

            foreach (string row in rows)
            {
                string line = "GD66_LIMIT_MEASUREMENT " + row;
                Debug.Log(line);
                TestContext.Progress.WriteLine(line);
            }
            Assert.That(rows.Count, Is.EqualTo(30));
        }

        [Test]
        public void LegacyPersistenceBytes_MatchPrettyPrintedSaveRootRepresentation()
        {
            SaveData save = RepresentativeSave();
            byte[] measured = SerializeLegacyPersistence(save);
            string expected = JsonUtility.ToJson(new SaveRoot
            {
                schemaVersion = SaveMigration.LegacyCompatibilitySchemaVersion,
                primary = save
            }, true);

            Assert.That(Encoding.UTF8.GetString(measured), Is.EqualTo(expected));
            Assert.That(expected, Does.Contain("\n"));
            Assert.That(expected, Is.Not.EqualTo(JsonUtility.ToJson(new SaveRoot
            {
                schemaVersion = SaveMigration.LegacyCompatibilitySchemaVersion,
                primary = save
            })));
        }

        [Test]
        public void RawMinimumSearch_StartsAtValidOneAndFindsKnownFixtureDimensions()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(
                "{\"schema\":\"save_root\",\"schemaVersion\":6,\"primary\":{}}");
            Assert.That(MinimumRaw(bytes, 0), Is.EqualTo(2));
            Assert.That(MinimumRaw(bytes, 1), Is.EqualTo(3));
            Assert.That(MinimumRaw(bytes, 2), Is.EqualTo(1));
            Assert.That(MinimumRaw(bytes, 3), Is.EqualTo(13));
            Assert.That(MinimumRaw(bytes, 4), Is.GreaterThan(0));
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

        private static SaveData RepresentativeSave()
        {
            DungeonLayoutState expectedLayout = PopulatedDungeonLayout();
            MvpRoomSlotAssignmentCollection expectedAssignments = LegacyRuntimeR2Assignments();
            var root = new SaveRoot
            {
                schemaVersion = SaveMigration.LegacyCompatibilitySchemaVersion,
                primary = new SaveData
                {
                    saveVersion = 6, contentVersion = "gd66-measurement",
                    createdUtcUnix = 1700000000L, lastPausedUtcUnix = 1700086400L,
                    lastResumedUtcUnix = 1700086460L, lastSavedUtcUnix = 1700172800L,
                    totalTicks = 987654L, lastKnownAppState = "Paused",
                    dungeonLayout = expectedLayout,
                    structureRuntime = new StructureRuntimeState { ManaReserve = 250d, Heat = 37d },
                    mvpRoomSlotAssignments = expectedAssignments,
                    lastOfflineSummary = new OfflineSummary
                    {
                        RuleResolved = true, OfflineSecondsObserved = 60,
                        RuleSourceIdUsed = "offline.rule.measurement"
                    }
                }
            };

            root = SaveMigration.MigrateToLatest(root);
            SaveData save = root.primary;
            Assert.That(root.schemaVersion, Is.EqualTo(SaveMigration.LegacyCompatibilitySchemaVersion));
            Assert.That(save.dungeonLayout, Is.SameAs(expectedLayout));
            Assert.That(save.dungeonLayout.Slots.Count, Is.EqualTo(30));
            Assert.That(save.mvpRoomSlotAssignments, Is.SameAs(expectedAssignments));
            Assert.That(save.mvpRoomSlotAssignments.Rooms.Count, Is.EqualTo(2));
            Assert.That(save.mvpDungeonPlacements, Is.Not.Null);
            Assert.That(save.mvpDungeonPlacements.Entries, Is.Not.Null);
            Assert.That(save.mvpDungeonFloorLayout, Is.Not.Null);
            Assert.That(save.mvpDungeonFloorLayout.Nodes, Is.Not.Null);
            Assert.That(save.structureRuntime, Is.Not.Null);
            Assert.That(save.runHistory, Is.Not.Null);
            Assert.That(save.runHistory.RecentOutcomes, Is.Not.Null);
            Assert.That(save.completedObjectives, Is.Not.Null);
            Assert.That(save.completedObjectives.ObjectiveIds, Is.Not.Null);
            return save;
        }

        private static SaveData RepresentativeLegacySaveWithTenPersistedRuns()
        {
            SaveData save = RepresentativeSave();
            RunSimulationConfig config = ProductionRunConfig();
            LootConfig loot = ProductionLootConfig();
            var simulation = new RunSimulationService(config, loot);
            MvpOrderedRouteRoom[] route = MvpOrderedRoomRouteResolver.Resolve(save, config);
            Assert.That(route.Length, Is.EqualTo(2));
            foreach (MvpOrderedRouteRoom room in route)
            {
                Assert.That(room.Capacity.MonsterCapacity, Is.EqualTo(1));
                Assert.That(room.Capacity.TrapCapacity, Is.EqualTo(1));
                Assert.That(room.Capacity.LootCapacity, Is.EqualTo(1));
                Assert.That(room.AssignedMonsterOptionIds.Length, Is.EqualTo(1));
                Assert.That(room.AssignedTrapOptionIds.Length, Is.EqualTo(1));
                Assert.That(room.AssignedLootNodeOptionIds.Length, Is.EqualTo(1));
            }
            PopulateTenRuns(save, simulation, route, config.MaxRunHistoryEntries);
            return save;
        }

        private static SaveData CanonicalTargetSaveWithTenPersistedRuns()
        {
            SaveData save = RepresentativeSave();
            save.mvpRoomSlotAssignments = CanonicalTargetR2Assignments();
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture spatialFixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            string basicDefinitionId = spatialFixture.Compatibility.Value.GeometryRecords.Single()
                .BasicRoomDefinitionId;
            RoomSpatialDefinition definition = spatialFixture.Production.Catalog.Rooms.Single(
                room => room.RoomDefinitionId == basicDefinitionId);
            Assert.That(definition.MonsterCapacity, Is.EqualTo(2));
            Assert.That(definition.TrapCapacity, Is.EqualTo(2));
            Assert.That(definition.LootCapacity, Is.EqualTo(2));

            RunSimulationConfig config = ProductionRunConfig();
            LootConfig loot = ProductionLootConfig();
            var simulation = new RunSimulationService(config, loot);
            MvpOrderedRouteRoom[] route = Enumerable.Range(0, 2).Select(index =>
                new MvpOrderedRouteRoom
                {
                    FloorIndex = 0, RoomIndex = index,
                    RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                    IncludeRoomPlacement = true, HasActiveContent = true,
                    Capacity = new MvpRoomSlotCapacity
                    {
                        RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                        MonsterCapacity = definition.MonsterCapacity,
                        TrapCapacity = definition.TrapCapacity,
                        LootCapacity = definition.LootCapacity
                    },
                    AssignedMonsterOptionIds = new[] { MvpDungeonPlacementIds.SkeletonOptionId,
                        MvpDungeonPlacementIds.GoblinOptionId },
                    AssignedTrapOptionIds = new[] { MvpDungeonPlacementIds.SpikeTrapOptionId,
                        MvpDungeonPlacementIds.SnareTrapOptionId },
                    AssignedLootNodeOptionIds = new[] { MvpDungeonPlacementIds.BasicLootNodeOptionId,
                        MvpDungeonPlacementIds.HiddenCacheOptionId }
                }).ToArray();
            PopulateTenRuns(save, simulation, route, config.MaxRunHistoryEntries);
            return save;
        }

        private static MvpRoomSlotAssignmentCollection LegacyRuntimeR2Assignments() =>
            Assignments(new[] { MvpDungeonPlacementIds.SkeletonOptionId },
                new[] { MvpDungeonPlacementIds.SpikeTrapOptionId },
                new[] { MvpDungeonPlacementIds.BasicLootNodeOptionId }, 7);

        private static MvpRoomSlotAssignmentCollection CanonicalTargetR2Assignments() =>
            Assignments(new[] { MvpDungeonPlacementIds.SkeletonOptionId,
                    MvpDungeonPlacementIds.GoblinOptionId },
                new[] { MvpDungeonPlacementIds.SpikeTrapOptionId,
                    MvpDungeonPlacementIds.SnareTrapOptionId },
                new[] { MvpDungeonPlacementIds.BasicLootNodeOptionId,
                    MvpDungeonPlacementIds.HiddenCacheOptionId }, 13);

        private static MvpRoomSlotAssignmentCollection Assignments(string[] monsters,
            string[] traps, string[] loot, int nextRevision) => new MvpRoomSlotAssignmentCollection
        {
            NextRevision = nextRevision,
            Rooms = Enumerable.Range(0, 2).Select(index => new MvpRoomSlotAssignmentState
            {
                FloorIndex = 0, RoomIndex = index,
                RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                MonsterOptionIds = (string[])monsters.Clone(),
                TrapOptionIds = (string[])traps.Clone(),
                LootNodeOptionIds = (string[])loot.Clone()
            }).ToList()
        };

        private static DungeonLayoutState PopulatedDungeonLayout()
        {
            DungeonLayoutState layout = DungeonLayoutState.CreateEmpty(
                SaveMigration.DefaultFloorCount, SaveMigration.DefaultSlotsPerFloor);
            string[] structures = { StructureSimulationPass.ManaGeneratorBasicId,
                StructureSimulationPass.HeatScrubberBasicId, StructureSimulationPass.RiskLabBasicId };
            for (int index = 0; index < layout.Slots.Count; index++)
            {
                DungeonSlot slot = layout.Slots[index];
                layout.Slots[index] = new DungeonSlot(slot.FloorIndex, slot.SlotIndex,
                    structures[index % structures.Length]);
            }
            Assert.That(layout.Slots.Count, Is.EqualTo(30));
            Assert.That(layout.Slots.All(slot => slot.IsOccupied), Is.True);
            return layout;
        }

        private static RunSimulationConfig ProductionRunConfig() => LoadAsset<RunSimulationConfig>(
            "Assets/_Project/Data/Bootstrap/run_simulation_config.json");

        private static LootConfig ProductionLootConfig() => LoadAsset<LootConfig>(
            "Assets/_Project/Data/Bootstrap/loot_config.json");

        private static T LoadAsset<T>(string path) where T : class
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            Assert.That(asset, Is.Not.Null, path);
            return JsonUtility.FromJson<T>(asset.text);
        }

        private static void PopulateTenRuns(SaveData save, RunSimulationService simulation,
            MvpOrderedRouteRoom[] route, int maximumHistory)
        {
            Assert.That(maximumHistory, Is.EqualTo(10));
            save.runHistory = new RunHistoryState();
            for (int sequence = 1; sequence <= maximumHistory; sequence++)
            {
                RunOutcomeRecord outcome = simulation.SimulateRoute(save.structureRuntime,
                    1700000000L + sequence * 60L, sequence, RunPostureResolver.BalancedId, route);
                save.runHistory.AppendOutcome(outcome, maximumHistory);
                save.runHistory.NextRunSequence = sequence + 1;
            }
            Assert.That(save.runHistory.RecentOutcomes.Length, Is.EqualTo(maximumHistory));
        }

        private static ContentBootstrap ResearchBootstrap()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_Project/Data/Bootstrap/content_bootstrap.json");
            Assert.That(asset, Is.Not.Null);
            ContentBootstrap bootstrap = JsonUtility.FromJson<ContentBootstrap>(asset.text);
            Assert.That(bootstrap.researchPendingScaffold, Is.Not.Null);
            Assert.That(bootstrap.researchProgressScaffold, Is.Not.Null);
            Assert.That(bootstrap.researchCompletionEligibilityScaffold, Is.Not.Null);
            return bootstrap;
        }

        private static ResearchPendingState Pending(ContentBootstrap bootstrap) =>
            new ResearchPendingState
            {
                SlotId = bootstrap.researchPendingScaffold.slotId,
                ProjectId = bootstrap.researchPendingScaffold.projectId
            };

        private static ResearchProgressState Progress(ContentBootstrap bootstrap,
            double units, bool completionPending) => new ResearchProgressState
        {
            SlotId = bootstrap.researchPendingScaffold.slotId,
            ProjectId = bootstrap.researchPendingScaffold.projectId,
            ProgressUnits = units,
            CompletionPending = completionPending,
            RuleSourceIdUsed = bootstrap.researchProgressScaffold.ruleSourceId
        };

        private static CompletedObjectiveState CompletedObjective() => new CompletedObjectiveState
        {
            ObjectiveIds = new[] { "objective.first_dungeon_contract" },
            LastCompletedObjectiveId = "objective.first_dungeon_contract",
            LastCompletionRuleSourceId = CompletedObjectiveStateResolver.FirstSessionObjectiveCompletionRuleSourceId
        };

        // Permissive classification ceiling for measurement preparation only. Actual requirements
        // are still discovered independently through MinimumRaw and never use these values as evidence.
        private static RawSavePayloadClassificationLimits MeasurementPreparationRawLimits() =>
            new RawSavePayloadClassificationLimits(1000000, 128, 1000, 1000, 100000, 5000000);

        // Permissive, bounded preparation ceiling for measurement only. These values are not
        // measurements or proposed production authority; each workload is measured independently.
        private static DetachedWholeSaveLimits MeasurementPreparationWholeLimits() =>
            new DetachedWholeSaveLimits(2000000, 1000000, 10000, 1000000);

        private static CanonicalSpatialSerializationLimits MeasurementPreparationSerializationLimits() =>
            new CanonicalSpatialSerializationLimits(
                new SpatialSerializedInputLimits(2000000, 200000, 20000, 200000, 100),
                new CanonicalSpatialSaveWorkloadLimits(20000, 20000));

        private static byte[] SerializeLegacyPersistence(SaveData save)
        {
            var root = new SaveRoot
            {
                schemaVersion = SaveMigration.LegacyCompatibilitySchemaVersion,
                primary = save
            };
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(root, true));
        }

        private static void AddSerializedSave(List<string> rows, string name, SaveData save)
        {
            byte[] raw = SerializeLegacyPersistence(save);
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, raw,
                    MeasurementPreparationRawLimits(), MeasurementPreparationWholeLimits(),
                    MeasurementPreparationSerializationLimits());
            Assert.That(fixture.Result.IsSuccess, Is.True, fixture.Result.Reason);
            rows.Add(Measure(name, fixture.Original, fixture.Classification,
                fixture.Result.Attempt.Candidate.GetBytes(),
                ParseState(fixture.Result.Attempt.Candidate.GetBytes(), fixture.Limits)));
        }

        private static void AddNativeCanonical(List<string> rows,
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture)
        {
            int contractVersion = fixture.Compatibility.SelectContract(7).Value.CanonicalLayoutContractVersion;
            CompatibilitySelectionResult<CanonicalStarterLayoutProfile> starter =
                fixture.Compatibility.SelectStarter(7, contractVersion);
            Assert.That(starter.Success, Is.True, starter.Code);
            Assert.That(starter.Value.CanonicalLayoutContractVersion, Is.EqualTo(contractVersion));
            var state = new DetachedCanonicalSpatialSaveState
            {
                Authority = new CanonicalSpatialAuthorityMarker
                {
                    CanonicalLayoutContractVersion = contractVersion,
                    CreationKind = CanonicalSpatialCreationKind.NativeCanonical
                },
                Floors = Array.Empty<SavedSpatialFloor>()
            };
            SpatialContractResult<CanonicalSpatialSaveSerializer.SerializedMembers> members =
                CanonicalSpatialSaveSerializer.SerializeMembers(state, fixture.Limits);
            Assert.That(members.IsValid, Is.True);
            byte[] candidate = Encoding.UTF8.GetBytes(
                "{\"schema\":\"save_root\",\"schemaVersion\":7,\"primary\":{\"canonicalSpatialAuthority\":" +
                Encoding.UTF8.GetString(members.Value.Authority) + ",\"spatialFloors\":" +
                Encoding.UTF8.GetString(members.Value.Floors) + "}}");
            DetachedCompleteSaveValidationResult validated =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(candidate, fixture.Limits);
            Assert.That(validated.IsValid, Is.True, validated.Reason);
            rows.Add("native-canonical-empty:raw=not-applicable,candidateBytes=" + candidate.Length +
                ",strictNodes=" + MinimumStrict(candidate, validated.State, 0) +
                ",strictRecords=" + MinimumStrict(candidate, validated.State, 1) +
                ",strictStringChars=" + MinimumStrict(candidate, validated.State, 2) +
                ",canonicalRecords=" + CountCanonicalRecords(validated.State) +
                ",canonicalTiles=" + CountCanonicalTiles(validated.State) +
                ",copiedBytes=0,unknownCount=0,unknownBytes=0");
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
            if (limit < 1) return false;
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
