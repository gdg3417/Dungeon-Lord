#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class DetachedSpatialSaveLoadCoordinatorTests
    {
        [Test]
        public void NarrowHallRepairChangesOnlyRecognizedSpatialEvidenceAndPreservesUnknownBytes()
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"contentVersion\":\"repair\",\"mvpDungeonPlacements\":{" +
                "\"Entries\":[{\"CategoryId\":\"placement.category.room\"," +
                "\"OptionId\":\"placement.option.room.narrow_hall\",\"Revision\":1," +
                "\"note\":\"placement.option.room.narrow_hall\"}]," +
                "\"NextRevision\":2},\"unknownPrimary\":{\"n\":1.00}}," +
                "\"unknownRoot\":[true,null]}");
            RawSavePayloadClassification classification = RawSavePayloadClassifier.Classify(original,
                Gd66DetachedSpatialMigrationTransactionTests.RawLimitsForCoordinator,
                new RawSaveEnvelopeVersionContract(1, 6),
                Gd66DetachedSpatialMigrationTransactionTests.BlankFloorForCoordinator);

            LegacyNarrowHallRepairResult result = LegacyNarrowHallRepair.Prepare(original,
                classification, Gd66DetachedSpatialMigrationTransactionTests.RawLimitsForCoordinator,
                new RawSaveEnvelopeVersionContract(1, 6),
                Gd66DetachedSpatialMigrationTransactionTests.BlankFloorForCoordinator);

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            string repaired = Encoding.UTF8.GetString(result.GetBytes());
            Assert.That(repaired, Does.Contain("\"OptionId\":\"placement.option.room.basic\""));
            Assert.That(repaired, Does.Contain(
                "\"note\":\"placement.option.room.narrow_hall\""));
            Assert.That(repaired, Does.Contain("\"unknownPrimary\":{\"n\":1.00}"));
            Assert.That(repaired, Does.Contain("\"unknownRoot\":[true,null]"));
            Assert.That(repaired, Does.Contain("\"contentVersion\":\"repair\""));
        }

        [Test]
        public void NarrowHallRepairPatchesOnlyEffectivePlacementWinner()
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"mvpDungeonPlacements\":{\"Entries\":[" +
                "{\"CategoryId\":\"placement.category.room\",\"OptionId\":\"placement.option.room.narrow_hall\",\"Revision\":1}," +
                "{\"CategoryId\":\"placement.category.room\",\"OptionId\":\"placement.option.room.narrow_hall\",\"Revision\":3}],\"NextRevision\":4}}}");
            RawSavePayloadClassification classification = ClassifyRepair(original);

            LegacyNarrowHallRepairResult result = LegacyNarrowHallRepair.Prepare(original,
                classification, Gd66DetachedSpatialMigrationTransactionTests.RawLimitsForCoordinator,
                new RawSaveEnvelopeVersionContract(1, 6),
                Gd66DetachedSpatialMigrationTransactionTests.BlankFloorForCoordinator, 0);

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            string repaired = Encoding.UTF8.GetString(result.GetBytes());
            Assert.That(repaired, Does.Contain("\"OptionId\":\"placement.option.room.narrow_hall\",\"Revision\":1"));
            Assert.That(repaired, Does.Contain("\"OptionId\":\"placement.option.room.basic\",\"Revision\":3"));
        }

        [Test]
        public void NarrowHallRepairTargetsOnlyRequestedEffectiveR2Assignment()
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"mvpSelectedRoomSlotIndex\":1,\"unknown\":{\"n\":1.00}," +
                "\"mvpRoomSlotAssignments\":{\"Rooms\":[" +
                "{\"FloorIndex\":0,\"RoomIndex\":0,\"RoomOptionId\":\"placement.option.room.narrow_hall\"}," +
                "{\"FloorIndex\":0,\"RoomIndex\":1,\"RoomOptionId\":\"placement.option.room.narrow_hall\"}]}}}");
            RawSavePayloadClassification classification = ClassifyRepair(original);
            Assert.That(LegacyNarrowHallRepair.FindRepairTargets(classification), Is.EqualTo(new[] { 0, 1 }));

            LegacyNarrowHallRepairResult result = LegacyNarrowHallRepair.Prepare(original,
                classification, Gd66DetachedSpatialMigrationTransactionTests.RawLimitsForCoordinator,
                new RawSaveEnvelopeVersionContract(1, 6),
                Gd66DetachedSpatialMigrationTransactionTests.BlankFloorForCoordinator, 0);

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            string repaired = Encoding.UTF8.GetString(result.GetBytes());
            Assert.That(repaired, Does.Contain("\"RoomIndex\":0,\"RoomOptionId\":\"placement.option.room.basic\""));
            Assert.That(repaired, Does.Contain("\"RoomIndex\":1,\"RoomOptionId\":\"placement.option.room.narrow_hall\""));
            Assert.That(repaired, Does.Contain("\"unknown\":{\"n\":1.00}"));
        }

        private static RawSavePayloadClassification ClassifyRepair(byte[] bytes) =>
            RawSavePayloadClassifier.Classify(bytes,
                Gd66DetachedSpatialMigrationTransactionTests.RawLimitsForCoordinator,
                new RawSaveEnvelopeVersionContract(1, 6),
                Gd66DetachedSpatialMigrationTransactionTests.BlankFloorForCoordinator);

        [Test]
        public void RepairBytesUseSharedAtomicAuthorityAndRetryAfterPartialEvidenceWrite()
        {
            byte[] original = Encoding.UTF8.GetBytes("original");
            byte[] candidate = Encoding.UTF8.GetBytes("candidate");
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string active = Path.GetFullPath(Path.Combine(Path.GetTempPath(),
                "gd66-repair-atomic-" + Guid.NewGuid().ToString("N") + ".json"));
            fileSystem.Seed(active, original);
            fileSystem.EnablePartialWriteFailure(3);

            string interrupted = ExactCompleteSaveAtomicPersistence.Persist(active, fileSystem,
                original, candidate, 64);
            fileSystem.DisableFailure();
            string retried = ExactCompleteSaveAtomicPersistence.Persist(active, fileSystem,
                original, candidate, 64);

            Assert.That(interrupted, Is.Not.Null);
            Assert.That(retried, Is.Null);
            Assert.That(fileSystem.ReadAllBytes(active), Is.EqualTo(candidate));
            Assert.That(fileSystem.Paths.Any(path => path.Contains(".canonical-write-")), Is.False);
        }

        [Test]
        public void OrdinaryEvidenceDiscoveryIgnoresPrefixLookalikesAndOwnsExactHashGrammar()
        {
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string active = Path.GetFullPath(Path.Combine(Path.GetTempPath(),
                "save_primary.json"));
            string exact = active + ".canonical-write-0123456789abcdef-fedcba9876543210.rollback";
            string lookalike = active + ".canonical-write-not-owned";
            fileSystem.Seed(exact, Encoding.UTF8.GetBytes("evidence"));
            fileSystem.Seed(lookalike, Encoding.UTF8.GetBytes("unrelated"));

            IReadOnlyList<string> evidence = ExactCompleteSaveAtomicPersistence.DiscoverOwnedEvidence(
                active, fileSystem, 8);

            Assert.That(evidence, Is.EqualTo(new[] { exact }));
            Assert.That(fileSystem.ReadAllBytes(lookalike), Is.EqualTo(Encoding.UTF8.GetBytes("unrelated")));
        }

        [Test]
        public void LiveNewGameGateUsesExactMigrationAndOrdinaryEvidenceGrammar()
        {
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string active = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "save_primary.json"));
            string directory = Path.GetDirectoryName(active);
            fileSystem.Seed(Path.Combine(directory, "save_primary.gd66-not-owned.txt"), new byte[] { 1 });
            fileSystem.Seed(active + ".canonical-write-not-owned", new byte[] { 2 });
            Assert.That(SaveService.HasOwnedRecoveryEvidence(active, fileSystem, 8), Is.False);

            string transaction = "gd66-" + new string('a', 64);
            SpatialMigrationSidecarNames names = SpatialMigrationSidecarPaths.Derive(
                Path.GetFileName(active), transaction).Value;
            fileSystem.Seed(Path.Combine(directory, names.Journal), new byte[] { 3 });
            Assert.That(SaveService.HasOwnedRecoveryEvidence(active, fileSystem, 8), Is.True);
        }

        [TestCase("invalid-id.journal.json", true)]
        [TestCase("invalid-id.journal.json.next", true)]
        [TestCase("invalid-id.original.bak", true)]
        [TestCase("invalid-id.original.bak.restore.intent", true)]
        [TestCase("invalid-id.original.bak.restore", true)]
        [TestCase("invalid-id.candidate.tmp", true)]
        [TestCase("invalid-id.finalized", false)]
        [TestCase("invalid-id.evidence", false)]
        [TestCase("not-owned.txt", false)]
        public void LiveNewGameGateSharesTransactionRecoveryRelevantSuffixes(string suffix,
            bool expected)
        {
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string active = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "save_primary.json"));
            fileSystem.Seed(Path.Combine(Path.GetDirectoryName(active), "save_primary.gd66-" + suffix),
                Encoding.UTF8.GetBytes("malformed-or-orphan-evidence"));

            Assert.That(SaveService.HasOwnedRecoveryEvidence(active, fileSystem, 8), Is.EqualTo(expected));
        }

        [Test]
        public void LiveNewGameGateFailsClosedOnEvidenceOverflowAndRedirection()
        {
            string active = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "save_primary.json"));
            string directory = Path.GetDirectoryName(active);
            var overflow = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            overflow.Seed(Path.Combine(directory, "save_primary.gd66-a.original.bak"), new byte[] { 1 });
            overflow.Seed(Path.Combine(directory, "save_primary.gd66-b.original.bak"), new byte[] { 2 });
            Assert.Throws<IOException>(() => SaveService.HasOwnedRecoveryEvidence(active, overflow, 1));

            var redirected = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem(
                Gd66DetachedSpatialMigrationTransactionTests.OperationType.Containment, 1);
            redirected.Seed(Path.Combine(directory, "save_primary.gd66-a.original.bak"), new byte[] { 1 });
            Assert.Throws<IOException>(() => SaveService.HasOwnedRecoveryEvidence(active, redirected, 8));
        }

        [Test]
        public void SaveServiceCleanNoSaveCreatesAndReloadsNativeCanonicalAuthority()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            SaveService first = ConfiguredService(fixture, fileSystem, "service-native.json");

            SaveData created = first.LoadOrCreate("gd66-live", out string createBanner);

            Assert.That(created, Is.Not.Null, createBanner);
            Assert.That(created.validatedCanonicalSpatialState, Is.Not.Null);
            Assert.That(first.CanonicalSession, Is.Not.Null);
            Assert.That(fileSystem.ReadAllBytes(first.SavePath),
                Is.EqualTo(first.CanonicalSession.GetCurrentBytes()));
            SaveService reopened = ConfiguredService(fixture, fileSystem, "service-native.json");
            SaveData loaded = reopened.LoadOrCreate("gd66-live", out string loadBanner);
            Assert.That(loaded, Is.Not.Null, loadBanner);
            Assert.That(reopened.CanonicalSession.GetCurrentBytes(),
                Is.EqualTo(fileSystem.ReadAllBytes(reopened.SavePath)));
        }

        [Test]
        public void SaveServiceSchemaSixUsesGd66MigrationAndRetainsCanonicalSession()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            SaveService service = ConfiguredService(fixture, fileSystem, "service-legacy.json");
            fileSystem.Seed(service.SavePath, fixture.Original);

            SaveData loaded = service.LoadOrCreate("gd66-live", out string banner);

            Assert.That(loaded, Is.Not.Null, banner);
            Assert.That(loaded.validatedCanonicalSpatialState, Is.Not.Null);
            Assert.That(service.CanonicalSession, Is.Not.Null);
            Assert.That(Encoding.UTF8.GetString(service.CanonicalSession.GetCurrentBytes()),
                Does.Contain("\"schemaVersion\":7"));
        }

        [Test]
        public void SaveServiceRecoveryRelevantEvidenceBlocksNativeButLookalikeDoesNot()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            var blockedFileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            SaveService blocked = ConfiguredService(fixture, blockedFileSystem, "service-evidence.json");
            blockedFileSystem.Seed(Path.Combine(Path.GetDirectoryName(blocked.SavePath),
                "service-evidence.gd66-invalid-id.journal.json"), Encoding.UTF8.GetBytes("malformed"));

            LogAssert.Expect(LogType.Error, "[ERROR] GD66 load failed: " +
                DetachedSpatialMigrationTransaction.PathInvalidReason);
            Assert.That(blocked.LoadOrCreate("gd66-live", out _), Is.Null);
            Assert.That(blockedFileSystem.Exists(blocked.SavePath), Is.False);

            var cleanFileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            SaveService clean = ConfiguredService(fixture, cleanFileSystem, "service-lookalike.json");
            cleanFileSystem.Seed(Path.Combine(Path.GetDirectoryName(clean.SavePath),
                "service-lookalike.gd66-not-owned.txt"), new byte[] { 1 });
            Assert.That(clean.LoadOrCreate("gd66-live", out string banner), Is.Not.Null, banner);
            Assert.That(cleanFileSystem.Exists(clean.SavePath), Is.True);
        }

        [Test]
        public void SaveServiceExplicitDeleteClearsExactEvidenceAndAllowsNativeRecreation()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            SaveService service = ConfiguredService(fixture, fileSystem, "service-delete.json");
            Assert.That(service.LoadOrCreate("gd66-live", out string createBanner), Is.Not.Null,
                createBanner);
            string exactOrdinary = service.SavePath +
                ".canonical-write-0123456789abcdef-fedcba9876543210.rollback";
            fileSystem.Seed(exactOrdinary, Encoding.UTF8.GetBytes("evidence"));
            string directory = Path.GetDirectoryName(service.SavePath);
            string stem = Path.GetFileNameWithoutExtension(service.SavePath);
            string[] recognized =
            {
                stem + ".gd66-invalid-id.journal.json",
                stem + ".gd66-invalid-id.original.bak",
                stem + ".gd66-invalid-id.candidate.tmp",
                stem + ".gd66-invalid-id.original.bak.restore",
                stem + ".gd66-invalid-id.original.bak.restore.intent",
                stem + ".gd66-invalid-id.finalized",
                stem + ".gd66-invalid-id.evidence"
            };
            foreach (string filename in recognized)
                fileSystem.Seed(Path.Combine(directory, filename), Encoding.UTF8.GetBytes("evidence"));
            SpatialMigrationSidecarNames validNames = SpatialMigrationSidecarPaths.Derive(
                Path.GetFileName(service.SavePath), "gd66-" + new string('a', 64)).Value;
            string validJournal = Path.Combine(directory, validNames.Journal);
            fileSystem.Seed(validJournal, Encoding.UTF8.GetBytes("valid-name-evidence"));
            string unrelated = Path.Combine(directory, stem + ".gd66-not-owned.txt");
            fileSystem.Seed(unrelated, Encoding.UTF8.GetBytes("unrelated"));
            Assert.That(SaveService.HasOwnedRecoveryEvidence(service.SavePath, fileSystem, 64), Is.True);

            service.DeleteSave(out string deleteBanner);

            Assert.That(fileSystem.Exists(service.SavePath), Is.False, deleteBanner);
            Assert.That(fileSystem.Exists(exactOrdinary), Is.False);
            foreach (string filename in recognized)
                Assert.That(fileSystem.Exists(Path.Combine(directory, filename)), Is.False, filename);
            Assert.That(fileSystem.Exists(validJournal), Is.False);
            Assert.That(fileSystem.Exists(unrelated), Is.True);
            Assert.That(SaveService.HasOwnedRecoveryEvidence(service.SavePath, fileSystem, 64), Is.False);
            Assert.That(service.CanonicalSession, Is.Null);
            Assert.That(service.NarrowHallRepairAvailable, Is.False);
            Assert.That(service.LoadOrCreate("gd66-live", out string recreateBanner), Is.Not.Null,
                recreateBanner);
            Assert.That(service.CanonicalSession, Is.Not.Null);
        }

        [Test]
        public void CanonicalDeleteQualifiesFilesystemBeforeAnyCleanupWhenNotCached()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            SaveService service = ConfiguredService(fixture, fileSystem, "service-delete-preflight.json");
            bool evaluated = false;
            service.SetPreflightEvaluatorForTests(path =>
            {
                evaluated = true;
                return Supported(fileSystem);
            });
            fileSystem.Seed(service.SavePath, fixture.Original);
            string directory = Path.GetDirectoryName(service.SavePath);
            string stem = Path.GetFileNameWithoutExtension(service.SavePath);
            string journal = Path.Combine(directory, stem + ".gd66-invalid-id.journal.json");
            string audit = Path.Combine(directory, stem + ".gd66-invalid-id.evidence");
            string ordinary = service.SavePath +
                ".canonical-write-0123456789abcdef-fedcba9876543210.candidate";
            string unrelated = Path.Combine(directory, stem + ".gd66-not-owned.txt");
            fileSystem.Seed(journal, new byte[] { 1 });
            fileSystem.Seed(audit, new byte[] { 2 });
            fileSystem.Seed(ordinary, new byte[] { 3 });
            fileSystem.Seed(unrelated, new byte[] { 4 });

            service.DeleteSave(out string banner);

            Assert.That(evaluated, Is.True);
            Assert.That(fileSystem.Exists(service.SavePath), Is.False, banner);
            Assert.That(fileSystem.Exists(journal), Is.False);
            Assert.That(fileSystem.Exists(audit), Is.False);
            Assert.That(fileSystem.Exists(ordinary), Is.False);
            Assert.That(fileSystem.Exists(unrelated), Is.True);
            Assert.That(SaveService.HasOwnedRecoveryEvidence(service.SavePath, fileSystem, 64), Is.False);
        }

        [Test]
        public void CanonicalDeleteUnsupportedPreflightNeverFallsBackToSystemFileDelete()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            string directory = Path.Combine(Path.GetTempPath(), "gd66-delete-unsupported-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var service = new SaveService(new SimpleLogger(false),
                    new SaveConfig { fileName = "active.json" }, directory);
                service.ConfigureCanonical(new SaveSpatialMigrationLimitsProfile(
                        Gd66DetachedSpatialMigrationTransactionTests.RawLimitsForCoordinator,
                        fixture.Limits, fixture.WholeLimits), fixture.Production, fixture.Compatibility,
                    LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes), fixture.LegacyBytes);
                service.SetPreflightEvaluatorForTests(path => new SpatialMigrationActivationPreflight(false,
                    SpatialMigrationCapabilityReason.PlatformUnsupported,
                    SpatialMigrationPlatform.Unsupported, null));
                byte[] active = Encoding.UTF8.GetBytes("trusted-active");
                string evidence = Path.Combine(directory, "active.gd66-invalid-id.original.bak");
                File.WriteAllBytes(service.SavePath, active);
                File.WriteAllBytes(evidence, new byte[] { 7 });

                LogAssert.Expect(LogType.Error, "[ERROR] Delete save failed. Exception: " +
                    "GD66 delete preflight failed: " + SpatialMigrationCapabilityReason.PlatformUnsupported);
                service.DeleteSave(out string banner);

                Assert.That(File.ReadAllBytes(service.SavePath), Is.EqualTo(active), banner);
                Assert.That(File.Exists(evidence), Is.True);
                Assert.That(banner, Does.Contain("Failed"));
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void LegacyUnconfiguredDeleteRetainsHistoricalSystemFileBehavior()
        {
            string directory = Path.Combine(Path.GetTempPath(), "gd66-delete-legacy-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var service = new SaveService(new SimpleLogger(false),
                    new SaveConfig { fileName = "legacy.json" }, directory);
                File.WriteAllText(service.SavePath, "legacy");

                service.DeleteSave(out string banner);

                Assert.That(File.Exists(service.SavePath), Is.False, banner);
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void SaveServiceNarrowHallRepairRerunsGd66AndPreservesUnknownEvidence()
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"contentVersion\":\"repair\",\"unknownPrimary\":{\"n\":1.00}," +
                "\"mvpDungeonPlacements\":{\"Entries\":[{\"CategoryId\":\"placement.category.room\"," +
                "\"OptionId\":\"placement.option.room.narrow_hall\",\"Revision\":1}],\"NextRevision\":2}}," +
                "\"unknownRoot\":[true,null]}");
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, original);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            SaveService service = ConfiguredService(fixture, fileSystem, "service-repair-r1.json");
            fileSystem.Seed(service.SavePath, original);

            LogAssert.Expect(LogType.Error, "[ERROR] GD66 load failed: " +
                DetachedSpatialMigrationPreparer.NarrowHallReason);
            Assert.That(service.LoadOrCreate("gd66-live", out _), Is.Null);
            Assert.That(service.NarrowHallRepairAvailable, Is.True);
            Assert.That(service.NarrowHallRepairTargets, Is.EqualTo(new[] { 0 }));
            SaveData repaired = service.RepairNarrowHallToBasicAndRetry("gd66-live", out string banner);

            Assert.That(repaired, Is.Not.Null, banner);
            Assert.That(service.CanonicalSession, Is.Not.Null);
            string persisted = Encoding.UTF8.GetString(service.CanonicalSession.GetCurrentBytes());
            Assert.That(persisted, Does.Contain("\"unknownPrimary\":{\"n\":1.00}"));
            Assert.That(persisted, Does.Contain("\"unknownRoot\":[true,null]"));
        }

        [Test]
        public void SaveServiceTwoNarrowR2RepairsOneTargetAtATime()
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"mvpSelectedRoomSlotIndex\":1,\"mvpRoomSlotAssignments\":{\"Rooms\":[" +
                Assignment(0, "narrow_hall") + "," + Assignment(1, "narrow_hall") +
                "],\"NextRevision\":3}}}");
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, original);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            SaveService service = ConfiguredService(fixture, fileSystem, "service-repair-r2.json");
            fileSystem.Seed(service.SavePath, original);

            LogAssert.Expect(LogType.Error, "[ERROR] GD66 load failed: " +
                DetachedSpatialMigrationPreparer.NarrowHallReason);
            Assert.That(service.LoadOrCreate("gd66-live", out _), Is.Null);
            Assert.That(service.NarrowHallRepairTargets, Is.EqualTo(new[] { 0, 1 }));
            service.SelectNarrowHallRepairTarget(0);
            LogAssert.Expect(LogType.Error, "[ERROR] GD66 load failed: " +
                DetachedSpatialMigrationPreparer.NarrowHallReason);
            Assert.That(service.RepairNarrowHallToBasicAndRetry("gd66-live", out _), Is.Null);
            Assert.That(service.NarrowHallRepairAvailable, Is.True);
            Assert.That(service.NarrowHallRepairTargets, Is.EqualTo(new[] { 1 }));
            SaveData repaired = service.RepairNarrowHallToBasicAndRetry("gd66-live", out string banner);

            Assert.That(repaired, Is.Not.Null, banner);
            Assert.That(service.CanonicalSession, Is.Not.Null);
            Assert.That(repaired.validatedCanonicalSpatialState.Floors[0].Layout.Rooms.Length,
                Is.EqualTo(2));
        }

        [Test]
        public void SaveServiceChangedTrustedOriginalInvalidatesRepairAndReevaluatesAuthority()
        {
            byte[] narrow = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"mvpDungeonPlacements\":{\"Entries\":[{" +
                "\"CategoryId\":\"placement.category.room\",\"OptionId\":\"placement.option.room.narrow_hall\"," +
                "\"Revision\":1}],\"NextRevision\":2}}}");
            byte[] changed = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(narrow).Replace(
                "placement.option.room.narrow_hall", "placement.option.room.basic"));
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, narrow);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            SaveService service = ConfiguredService(fixture, fileSystem, "service-repair-auth.json");
            fileSystem.Seed(service.SavePath, narrow);
            LogAssert.Expect(LogType.Error, "[ERROR] GD66 load failed: " +
                DetachedSpatialMigrationPreparer.NarrowHallReason);
            Assert.That(service.LoadOrCreate("gd66-live", out _), Is.Null);
            fileSystem.RemoveSeededEvidence(service.SavePath);
            fileSystem.Seed(service.SavePath, changed);

            SaveData result = service.RepairNarrowHallToBasicAndRetry("gd66-live", out string banner);

            Assert.That(result, Is.Not.Null, banner);
            Assert.That(service.NarrowHallRepairAvailable, Is.False);
            Assert.That(service.CanonicalSession, Is.Not.Null);
        }

        private static string Assignment(int room, string option) =>
            "{\"FloorIndex\":0,\"RoomIndex\":" + room +
            ",\"RoomOptionId\":\"placement.option.room." + option +
            "\",\"MonsterOptionIds\":[],\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}";
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void Load_WrappedLegacySchemas_MigrateBeforeRuntimeProjection(int schema)
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(schema);
            Assert.That(fixture.Result.IsSuccess, Is.True, fixture.Result.Reason);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string activePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(),
                "gd66-coordinator-" + schema + ".json"));
            fileSystem.Seed(activePath, fixture.Original);

            DetachedSpatialSaveLoadResult result = Coordinator(fixture).Load(activePath,
                Supported(fileSystem));

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.Disposition, Is.EqualTo(DetachedSpatialSaveLoadDisposition.Migrated));
            Assert.That(result.Transaction, Is.Not.Null);
            Assert.That(result.RuntimeProjection, Is.Not.Null);
            Assert.That(result.RuntimeProjection.validatedCanonicalSpatialState, Is.Not.Null);
            Assert.That(result.Session.GetCurrentBytes(), Is.EqualTo(fileSystem.ReadAllBytes(activePath)));
            Assert.That(result.GetValidatedBytes(), Is.EqualTo(fileSystem.ReadAllBytes(activePath)));
        }

        [Test]
        public void Load_UnwrappedLegacy_Migrates()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(1, true);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string activePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-unwrapped.json"));
            fileSystem.Seed(activePath, fixture.Original);

            DetachedSpatialSaveLoadResult result = Coordinator(fixture).Load(activePath,
                Supported(fileSystem));

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.Disposition, Is.EqualTo(DetachedSpatialSaveLoadDisposition.Migrated));
        }

        [Test]
        public void Load_AlreadyCommittedCandidate_DoesNotRunAnotherMigration()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string activePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-current.json"));
            fileSystem.Seed(activePath, fixture.Result.Attempt.Candidate.GetBytes());

            DetachedSpatialSaveLoadResult result = Coordinator(fixture).Load(activePath,
                Supported(fileSystem));

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.Disposition, Is.EqualTo(DetachedSpatialSaveLoadDisposition.AlreadyCommitted));
            Assert.That(result.Transaction, Is.Null);
            Assert.That(result.RuntimeProjection, Is.Not.Null);
            Assert.That(result.Session, Is.Not.Null);
        }

        [Test]
        public void Load_UnfinishedVerifiedCandidateRecoversBeforePublication()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string activePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-recover-candidate.json"));
            Gd66DetachedSpatialMigrationTransactionTests.MaterializeJournal(fileSystem, fixture, activePath,
                SpatialMigrationJournalStage.CandidateVerified, includeBackup: true,
                includeStaging: true, activeCandidate: false);

            DetachedSpatialSaveLoadResult result = Coordinator(fixture).Load(activePath,
                Supported(fileSystem));

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.Recovery.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
            Assert.That(result.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(result.RuntimeProjection, Is.Not.Null);
            Assert.That(result.Session.GetCurrentBytes(), Is.EqualTo(fileSystem.ReadAllBytes(activePath)));
        }

        [Test]
        public void Load_LegacyUnknownEvidence_RemainsInValidatedSessionBytes()
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"unknownPrimary\":[1,{\"n\":1.00}]}," +
                "\"unknownRoot\":{\"nested\":[true,null]}}");
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, original);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string activePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-unknown.json"));
            fileSystem.Seed(activePath, original);

            DetachedSpatialSaveLoadResult result = Coordinator(fixture).Load(activePath,
                Supported(fileSystem));

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            string sessionJson = Encoding.UTF8.GetString(result.Session.GetCurrentBytes());
            Assert.That(sessionJson, Does.Contain("\"unknownPrimary\":[1,{\"n\":1.00}]"));
            Assert.That(sessionJson, Does.Contain("\"unknownRoot\":{\"nested\":[true,null]}"));
        }

        [Test]
        public void Load_UnsupportedPreflight_FailsBeforeReadingOrPublishing()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            var result = Coordinator(fixture).Load(Path.GetFullPath(Path.Combine(Path.GetTempPath(),
                "gd66-unsupported.json")), new SpatialMigrationActivationPreflight(false,
                    SpatialMigrationCapabilityReason.PlatformUnsupported,
                    SpatialMigrationPlatform.Unsupported, null));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo(SpatialMigrationCapabilityReason.PlatformUnsupported));
            Assert.That(result.RuntimeProjection, Is.Null);
            Assert.That(result.Session, Is.Null);
            Assert.That(result.GetValidatedBytes(), Is.Null);
        }

        [Test]
        public void Load_MalformedRawPayload_FailsWithoutRuntimeProjection()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string activePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-malformed.json"));
            fileSystem.Seed(activePath, System.Text.Encoding.UTF8.GetBytes("{not-json"));

            DetachedSpatialSaveLoadResult result = Coordinator(fixture).Load(activePath,
                Supported(fileSystem));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.RuntimeProjection, Is.Null);
            Assert.That(result.Session, Is.Null);
        }

        [Test]
        public void Load_TrustedLegacySemanticMismatchRetainsOriginalWithoutCanonicalPublication()
        {
            byte[] original = Encoding.UTF8.GetBytes(
                "{\"schema\":\"save_root\",\"schemaVersion\":6,\"primary\":{" +
                "\"mvpDungeonPlacements\":{\"Entries\":{},\"NextRevision\":0}}}");
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string activePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(),
                "gd66-semantic-mismatch.json"));
            fileSystem.Seed(activePath, original);

            DetachedSpatialSaveLoadResult result = Coordinator(fixture).Load(activePath,
                Supported(fileSystem));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo(DetachedSpatialMigrationPreparer.OutcomeMismatchReason));
            Assert.That(result.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(result.RuntimeProjection, Is.Null);
            Assert.That(result.Session, Is.Null);
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(original));
            Assert.That(Encoding.UTF8.GetString(fileSystem.ReadAllBytes(activePath)),
                Does.Not.Contain("\"schemaVersion\":7"));
        }

        [Test]
        public void Load_NarrowHallBlocksMigrationButRetainsByteExactTrustedOriginal()
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"mvpDungeonPlacements\":{\"Entries\":[{" +
                "\"CategoryId\":\"placement.category.room\"," +
                "\"OptionId\":\"placement.option.room.narrow_hall\",\"Revision\":1}]," +
                "\"NextRevision\":2}}}");
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, original);
            Assert.That(fixture.Result.IsSuccess, Is.False);
            Assert.That(fixture.Result.Reason,
                Is.EqualTo(DetachedSpatialMigrationPreparer.NarrowHallReason));
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string activePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-narrow.json"));
            fileSystem.Seed(activePath, original);

            DetachedSpatialSaveLoadResult result = Coordinator(fixture).Load(activePath,
                Supported(fileSystem));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo(DetachedSpatialMigrationPreparer.NarrowHallReason));
            Assert.That(result.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(original));
            Assert.That(result.Transaction, Is.Null);
            Assert.That(result.RuntimeProjection, Is.Null);
            Assert.That(result.Session, Is.Null);
        }

        [Test]
        public void Load_InvalidBlankFloorFailsBeforePreflightEvaluation()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            var invalidBlank = new RawLegacyBlankFloorContract(1,
                Array.Empty<RawLegacyBlankFloorNodeContract>(), true, true,
                new[] { "Nodes", "NextRevision" },
                new[] { "FloorIndex", "NodeIndex", "SlotId", "CategoryId", "OptionId", "Revision" });
            DetachedSpatialSaveLoadCoordinator coordinator = Coordinator(fixture, invalidBlank);
            bool evaluated = false;

            DetachedSpatialSaveLoadResult result = coordinator.Load("invalid-path", path =>
            { evaluated = true; return Supported(new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem()); });

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo("gd66.profile.invalid"));
            Assert.That(evaluated, Is.False);
        }

        [Test]
        public void Load_MissingMigrationProfilePreservesExactSelectionReason()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            SpatialLayoutCompatibilityProfilesData data = fixture.Compatibility.Value;
            data.MigrationProfiles = Array.Empty<SpatialMigrationCompatibilityProfile>();
            var compatibility = new SpatialLayoutCompatibilitySnapshot(data);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string activePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-profile.json"));
            fileSystem.Seed(activePath, fixture.Original);
            DetachedSpatialSaveLoadCoordinator coordinator = Coordinator(fixture,
                Gd66DetachedSpatialMigrationTransactionTests.BlankFloorForCoordinator, compatibility);

            DetachedSpatialSaveLoadResult result = coordinator.Load(activePath, Supported(fileSystem));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo("gd66.profile.missing"));
            Assert.That(result.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
        }

        [Test]
        public void Load_RepresentativeTransactionFailureReturnsNoRuntimeAuthority()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string activePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-write-failure.json"));
            fileSystem.Seed(activePath, fixture.Original);
            fileSystem.EnableFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write, 1);

            DetachedSpatialSaveLoadResult result = Coordinator(fixture).Load(activePath,
                Supported(fileSystem));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(result.RuntimeProjection, Is.Null);
            Assert.That(result.Session, Is.Null);
        }

        [TestCase("\"mvpRoomSlotAssignments\":{\"Rooms\":[{\"FloorIndex\":0,\"RoomIndex\":0," +
            "\"RoomOptionId\":\"placement.option.room.basic\",\"MonsterOptionIds\":[]," +
            "\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}],\"NextRevision\":3}")]
        [TestCase("\"mvpDungeonFloorLayout\":{\"Nodes\":[{\"FloorIndex\":0,\"NodeIndex\":0," +
            "\"SlotId\":\"slot.0\",\"CategoryId\":\"placement.category.room\"," +
            "\"OptionId\":\"placement.option.room.basic\",\"Revision\":1}],\"NextRevision\":4}")]
        [TestCase("\"mvpDungeonPlacements\":{\"Entries\":[{" +
            "\"CategoryId\":\"placement.category.room\"," +
            "\"OptionId\":\"placement.option.room.basic\",\"Revision\":1}],\"NextRevision\":5}")]
        public void Load_EachLegacyAuthorityWinnerProducesCanonicalRoom(string primaryMember)
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":4," +
                "\"primary\":{" + primaryMember + "}}");

            DetachedSpatialSaveLoadResult result = LoadOriginal(4, original, "authority");

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.Validation.State.Floors.Length, Is.EqualTo(1));
            Assert.That(result.Validation.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(1));
        }

        [Test]
        public void Load_CompactAndPrettyEquivalentLegacyProduceEquivalentCanonicalProjection()
        {
            const string member = "\"mvpDungeonPlacements\":{\"Entries\":[{" +
                "\"CategoryId\":\"placement.category.room\"," +
                "\"OptionId\":\"placement.option.room.basic\",\"Revision\":1}],\"NextRevision\":5}";
            byte[] compact = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":4," +
                "\"primary\":{" + member + "}}");
            byte[] pretty = Encoding.UTF8.GetBytes("{\n  \"schema\": \"save_root\",\n" +
                "  \"schemaVersion\": 4,\n  \"primary\": {\n    " + member + "\n  }\n}");

            DetachedSpatialSaveLoadResult compactResult = LoadOriginal(4, compact, "compact");
            DetachedSpatialSaveLoadResult prettyResult = LoadOriginal(4, pretty, "pretty");

            Assert.That(compactResult.IsSuccess, Is.True, compactResult.Reason);
            Assert.That(prettyResult.IsSuccess, Is.True, prettyResult.Reason);
            Assert.That(SemanticSpatialJson(prettyResult.Validation.State.Floors),
                Is.EqualTo(SemanticSpatialJson(compactResult.Validation.State.Floors)));
            Assert.That(SemanticSpatialJson(prettyResult.RuntimeProjection.spatialFloors),
                Is.EqualTo(SemanticSpatialJson(compactResult.RuntimeProjection.spatialFloors)));
            Assert.That(prettyResult.Validation.State.Authority.MigrationTransactionId,
                Is.Not.EqualTo(compactResult.Validation.State.Authority.MigrationTransactionId));
            Assert.That(prettyResult.Validation.State.Authority.MigrationDescriptorFingerprint,
                Is.Not.EqualTo(compactResult.Validation.State.Authority.MigrationDescriptorFingerprint));
            DetachedSpatialSaveLoadResult compactAgain = LoadOriginal(4, compact, "compact-again");
            Assert.That(compactAgain.GetValidatedBytes(), Is.EqualTo(compactResult.GetValidatedBytes()));
        }

        private static string SemanticSpatialJson(SavedSpatialFloor[] floors) => JsonUtility.ToJson(
            new DetachedCanonicalSpatialSaveState
            {
                Authority = new CanonicalSpatialAuthorityMarker
                {
                    CanonicalLayoutContractVersion = 1,
                    CreationKind = CanonicalSpatialCreationKind.Migrated,
                    MigrationTransactionId = string.Empty,
                    MigrationDescriptorFingerprint = string.Empty
                },
                Floors = floors
            });

        [Test]
        public void Load_ActualPrettyJsonUtilitySaveRootMigrates()
        {
            var root = new SaveRoot { schema = "save_root", schemaVersion = 6,
                primary = new SaveData { saveVersion = 6 } };
            byte[] pretty = Encoding.UTF8.GetBytes(JsonUtility.ToJson(root, true));

            DetachedSpatialSaveLoadResult result = LoadOriginal(6, pretty, "json-utility-pretty");

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.RuntimeProjection, Is.Not.Null);
        }

        private static DetachedSpatialSaveLoadResult LoadOriginal(int schema, byte[] original, string identity)
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(schema, false, original);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string activePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-" + identity + ".json"));
            fileSystem.Seed(activePath, original);
            return Coordinator(fixture).Load(activePath, Supported(fileSystem));
        }

        private static SaveService ConfiguredService(
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture,
            ISpatialMigrationFileSystem fileSystem, string filename)
        {
            string directory = Path.GetFullPath(Path.Combine(Path.GetTempPath(),
                "gd66-save-service-tests"));
            var service = new SaveService(new SimpleLogger(false),
                new SaveConfig { fileName = filename, useAtomicWrites = true }, directory);
            service.ConfigureCanonical(new SaveSpatialMigrationLimitsProfile(
                    Gd66DetachedSpatialMigrationTransactionTests.RawLimitsForCoordinator,
                    fixture.Limits, fixture.WholeLimits), fixture.Production, fixture.Compatibility,
                LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes), fixture.LegacyBytes);
            service.SetPreflightEvaluatorForTests(path => Supported(fileSystem));
            return service;
        }

        private static DetachedSpatialSaveLoadCoordinator Coordinator(
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture,
            RawLegacyBlankFloorContract blankFloor = null,
            SpatialLayoutCompatibilitySnapshot compatibility = null)
        {
            var profile = new SaveSpatialMigrationLimitsProfile(
                Gd66DetachedSpatialMigrationTransactionTests.RawLimitsForCoordinator,
                fixture.Limits, fixture.WholeLimits);
            return new DetachedSpatialSaveLoadCoordinator(profile, compatibility ?? fixture.Compatibility,
                fixture.Production, fixture.LegacyBytes, new Dictionary<string, byte[]>(),
                new RawSaveEnvelopeVersionContract(1, 6),
                blankFloor ?? Gd66DetachedSpatialMigrationTransactionTests.BlankFloorForCoordinator);
        }

        private static SpatialMigrationActivationPreflight Supported(
            ISpatialMigrationFileSystem fileSystem) => new SpatialMigrationActivationPreflight(true,
                SpatialMigrationCapabilityReason.Ready, SpatialMigrationPlatform.WindowsEditor, fileSystem);
    }
}
#endif
