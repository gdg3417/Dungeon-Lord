#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using System.Text;
using System;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Economy;
using NUnit.Framework;
using UnityEngine;
using FrozenMigration = DungeonBuilder.M0.Tests.EditMode.Gd66DetachedSpatialMigrationTransactionTests;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class DetachedSpatialSaveLoadCoordinatorTests
    {
        [TestCase(1, "livingDungeon", false)] [TestCase(2, "livingDungeon", false)]
        [TestCase(3, "livingDungeon", false)] [TestCase(4, "livingDungeon", false)]
        [TestCase(5, "livingDungeon", false)] [TestCase(6, "livingDungeon", false)]
        [TestCase(6, "LivingDungeon", false)] [TestCase(1, "livingDungeon", true)] [TestCase(1, "LivingDungeon", true)]
        public void NewOwnerCollisionInLegacySourceRejectsBeforeAnyMigrationWrite(int schema, string ownerName, bool unwrapped)
        {
            var fixture = FrozenMigration.PrepareEmptyFixture(schema, unwrapped);
            string json = Encoding.UTF8.GetString(fixture.Original);
            byte[] original = Encoding.UTF8.GetBytes(unwrapped ? "{\"" + ownerName + "\":null," + json.Substring(1) :
                json.Replace("\"primary\":{}", "\"primary\":{\"lastSavedUtcUnix\":1,\"" + ownerName + "\":null}"));
            var historical = FrozenMigration.PrepareEmptyFixture(schema, unwrapped, original);
            Assert.That(historical.Classification.IsSuccess, Is.True, historical.Classification.FailureReason);

            // Frozen schema 7 retains unknown extensions. Schema 8 owns this
            // member and must reject both exact and case-alias collisions.
            Assert.That(historical.Result.IsSuccess, Is.True, historical.Result.Reason);
            byte[] seven = historical.Result.Attempt.Candidate.GetBytes();
            Assert.That(SchemaSevenToEightUpgrade.TryPrepare(seven, historical.Limits, out _), Is.False);
            var current = new FrozenMigration.DeterministicFileSystem();
            string active = FrozenMigration.ActivePath("legacy-owner-collision");
            current.Seed(active, original);
            AssertObsolete(historical, current, active, original);
        }

        [Test]
        public void NarrowHallRepairChangesOnlyRecognizedSpatialEvidenceAndPreservesUnknownBytes()
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"lastSavedUtcUnix\":1,\"contentVersion\":\"repair\",\"mvpDungeonPlacements\":{" +
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
                "\"primary\":{\"lastSavedUtcUnix\":1,\"mvpDungeonPlacements\":{\"Entries\":[" +
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
                "\"primary\":{\"lastSavedUtcUnix\":1,\"mvpSelectedRoomSlotIndex\":1,\"unknown\":{\"n\":1.00}," +
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
                TimedFixture(6);
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

        [TestCase(false)][TestCase(true)]
        public void SchemaEightLoadUpgradesAtomicallyAndRecoversFailedWrite(bool failPersistence)
        {
            var fixture = TimedFixture(6);
            byte[] seven = fixture.Result.Attempt.Candidate.GetBytes();
            Assert.That(SchemaSevenToEightUpgrade.TryPrepare(seven, fixture.Limits, out byte[] eight), Is.True);
            Assert.That(SchemaEightToNineUpgrade.TryPrepare(eight, fixture.Limits, out byte[] nine), Is.True);
            var fs = new FrozenMigration.DeterministicFileSystem();
            string path = FrozenMigration.ActivePath("frozen-eight-nine");
            fs.Seed(path, eight);
            bool ReadNine(byte[] bytes) =>
                DetachedCompleteSaveContract.ParseValidateFrozenSchemaNineAndRoundTrip(bytes, fixture.Limits).IsValid;
            if (failPersistence)
            {
                fs.EnableTargetedFailure(FrozenMigration.OperationType.Replace, _ => true, 1, false);
                string failed = ExactCompleteSaveAtomicPersistence.Persist(path, fs, eight, nine, 64, ReadNine);
                Assert.That(failed, Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
                fs.DisableFailure();
                CollectionAssert.AreEqual(eight, fs.ReadAllBytes(path));
            }
            Assert.That(ExactCompleteSaveAtomicPersistence.Persist(path, fs, eight, nine, 64, ReadNine), Is.Null);
            byte[] persisted = fs.ReadAllBytes(path);
            var frozen = DetachedCompleteSaveContract.ParseValidateFrozenSchemaNineAndRoundTrip(persisted, fixture.Limits);
            Assert.That(frozen.IsValid, Is.True, frozen.Reason);
            Assert.That(frozen.CurrentTargetValidated, Is.False);
            CollectionAssert.AreEqual(nine, frozen.GetBytes());
            AssertObsolete(fixture, fs, path, persisted);
        }

        [Test]
        public void SaveServiceRefusesSchemaSixWithoutPublishingCanonicalSession()
        {
            var fixture = TimedFixture(6);
            var fs = new FrozenMigration.DeterministicFileSystem();
            var service = ConfiguredService(fixture, fs, "service-legacy.json");
            fs.Seed(service.SavePath, fixture.Original);

            AssertSaveServiceObsolete(service, fs, fixture.Original);

            var historical = MigrateFrozenSeven(fixture, "service-legacy-historical");
            Assert.That(historical.IsValid, Is.True, historical.Reason);
        }

        [Test]
        public void SaveServiceAdoptsQualifiedPreflightPathBeforeRefusingObsoleteSave()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                TimedFixture(6);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string canonicalDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(),
                "gd66-qualified-adoption"));
            char alternate = Path.DirectorySeparatorChar == '\\' ? '/' : '\\';
            string suppliedDirectory = canonicalDirectory.Replace(Path.DirectorySeparatorChar, alternate);
            var service = new SaveService(new SimpleLogger(false),
                new SaveConfig { fileName = "save.json", useAtomicWrites = true }, suppliedDirectory);
            service.ConfigureCanonical(PhaseFourTestSupport.ProductionProfile(), fixture.Production, fixture.Compatibility,
                LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes), fixture.LegacyBytes,
                PhaseFourTestSupport.PositionProfiles(fixture.Limits),
                PhaseFourTestSupport.Occupancy(fixture.Limits));
            BindRecovery(service, fixture);
            string qualified = Path.Combine(canonicalDirectory, "save.json");
            fileSystem.Seed(qualified, fixture.Original);
            service.SetPreflightEvaluatorForTests(path => new SpatialMigrationActivationPreflight(true,
                SpatialMigrationCapabilityReason.Ready, SpatialMigrationPlatform.WindowsEditor,
                fileSystem, qualified));

            SaveData loaded = service.LoadOrCreate("gd66-live", out string banner);

            Assert.That(loaded, Is.Null);
            Assert.That(banner, Is.EqualTo(RecoveryEpisode.PlayerLocalizationKey(RecoveryEpisode.ObsoleteDevelopmentSave)));
            Assert.That(service.SavePath, Is.EqualTo(qualified));
            Assert.That(service.CanonicalSession, Is.Null);
            CollectionAssert.AreEqual(fixture.Original, fileSystem.ReadAllBytes(qualified));
            Assert.That(fileSystem.Operations.Any(IsMutation), Is.False);
        }

        [Test]
        public void SaveServiceRecoveryRelevantEvidenceBlocksNativeButLookalikeDoesNot()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                TimedFixture(6);
            var blockedFileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            var errors = new List<string>();
            SaveService blocked = ConfiguredService(fixture, blockedFileSystem,
                "service-evidence.json", errors);
            string evidence = Path.Combine(Path.GetDirectoryName(blocked.SavePath),
                "service-evidence.gd66-invalid-id.journal.json");
            byte[] malformed = Encoding.UTF8.GetBytes("malformed");
            blockedFileSystem.Seed(evidence, malformed);

            Assert.That(blocked.LoadOrCreate("gd66-live", out _), Is.Null);
            CollectionAssert.AreEqual(new[] { "[ERROR] GD66 load failed: " +
                DetachedSpatialMigrationTransaction.RecoveryFailedReason }, errors);
            Assert.That(blocked.CanonicalSession, Is.Null);
            Assert.That(blockedFileSystem.Exists(blocked.SavePath), Is.False);
            CollectionAssert.AreEqual(malformed, blockedFileSystem.ReadAllBytes(evidence));
            Assert.That(blockedFileSystem.Operations.Any(IsMutation), Is.False);

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
                TimedFixture(6);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            SaveService service = ConfiguredService(fixture, fileSystem, "service-delete.json");
            int preflightEvaluations = 0;
            service.SetPreflightEvaluatorForTests(path =>
            {
                preflightEvaluations++;
                return Supported(fileSystem, path);
            });
            Assert.That(service.LoadOrCreate("gd66-live", out string createBanner), Is.Not.Null,
                createBanner);
            Assert.That(preflightEvaluations, Is.EqualTo(1));
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

            Assert.That(preflightEvaluations, Is.EqualTo(1),
                "Delete must reuse the filesystem qualified and cached by LoadOrCreate.");
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
                TimedFixture(6);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            SaveService service = ConfiguredService(fixture, fileSystem, "service-delete-preflight.json");
            bool evaluated = false;
            service.SetPreflightEvaluatorForTests(path =>
            {
                evaluated = true;
                return Supported(fileSystem, path);
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
                TimedFixture(6);
            string directory = Path.Combine(Path.GetTempPath(), "gd66-delete-unsupported-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var errors = new List<string>();
                var service = new SaveService(CapturingLogger(errors),
                    new SaveConfig { fileName = "active.json" }, directory);
                service.ConfigureCanonical(new SaveSpatialMigrationLimitsProfile(
                        Gd66DetachedSpatialMigrationTransactionTests.RawLimitsForCoordinator,
                        fixture.Limits, fixture.WholeLimits), fixture.Production, fixture.Compatibility,
                    LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes), fixture.LegacyBytes,
                    PhaseFourTestSupport.PositionProfiles(fixture.Limits),
                    PhaseFourTestSupport.Occupancy(fixture.Limits));
                service.SetPreflightEvaluatorForTests(path => new SpatialMigrationActivationPreflight(false,
                    SpatialMigrationCapabilityReason.PlatformUnsupported,
                    SpatialMigrationPlatform.Unsupported, null));
                byte[] active = Encoding.UTF8.GetBytes("trusted-active");
                string evidence = Path.Combine(directory, "active.gd66-invalid-id.original.bak");
                File.WriteAllBytes(service.SavePath, active);
                File.WriteAllBytes(evidence, new byte[] { 7 });

                service.DeleteSave(out string banner);
                CollectionAssert.AreEqual(new[] { "[ERROR] Delete save failed. Exception: " +
                    "GD66 delete preflight failed: " +
                    SpatialMigrationCapabilityReason.PlatformUnsupported }, errors);

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
        public void HistoricalNarrowHallRepairRerunsGd66AndPreservesUnknownEvidence()
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"lastSavedUtcUnix\":1,\"contentVersion\":\"repair\",\"unknownPrimary\":{\"n\":1.00}," +
                "\"mvpDungeonPlacements\":{\"Entries\":[{\"CategoryId\":\"placement.category.room\"," +
                "\"OptionId\":\"placement.option.room.narrow_hall\",\"Revision\":1}],\"NextRevision\":2}}," +
                "\"unknownRoot\":[true,null]}");
            original = AuthorAbsentResearch(original);
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, original);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            var service = ConfiguredService(fixture, fileSystem, "service-repair-r1.json");
            fileSystem.Seed(service.SavePath, original);
            AssertSaveServiceObsolete(service, fileSystem, original);
            Assert.That(fixture.Result.Reason, Is.EqualTo(DetachedSpatialMigrationPreparer.NarrowHallReason));
            var repaired = PrepareHistoricalNarrowHallRepair(original, 0);
            var migrated = MigrateFrozenSeven(repaired, "repair-r1-historical");
            string persisted = Encoding.UTF8.GetString(migrated.GetBytes());
            Assert.That(persisted, Does.Contain("\"unknownPrimary\":{\"n\":1.00}"));
            Assert.That(persisted, Does.Contain("\"unknownRoot\":[true,null]"));
        }

        [Test]
        public void HistoricalTwoNarrowR2RepairsOneTargetAtATime()
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"lastSavedUtcUnix\":1,\"mvpSelectedRoomSlotIndex\":1,\"mvpRoomSlotAssignments\":{\"Rooms\":[" +
                Assignment(0, "narrow_hall") + "," + Assignment(1, "narrow_hall") +
                "],\"NextRevision\":3}}}");
            original = AuthorAbsentResearch(original);
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, original);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            var service = ConfiguredService(fixture, fileSystem, "service-repair-r2.json");
            fileSystem.Seed(service.SavePath, original);
            AssertSaveServiceObsolete(service, fileSystem, original);
            Assert.That(fixture.Result.Reason, Is.EqualTo(DetachedSpatialMigrationPreparer.NarrowHallReason));

            var first = PrepareHistoricalNarrowHallRepair(original, 0);
            Assert.That(first.Result.Reason, Is.EqualTo(DetachedSpatialMigrationPreparer.NarrowHallReason));
            Assert.That(Encoding.UTF8.GetString(first.Original),
                Does.Contain(Assignment(0, "basic")).And.Contain(Assignment(1, "narrow_hall")));

            var second = PrepareHistoricalNarrowHallRepair(first.Original, 1);
            var migrated = MigrateFrozenSeven(second, "repair-r2-historical");
            Assert.That(migrated.State.Floors[0].Layout.Rooms, Has.Length.EqualTo(2));
        }

        [Test]
        public void ChangedTrustedOriginalInvalidatesHistoricalRepairAndCurrentStillRefuses()
        {
            byte[] narrow = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"lastSavedUtcUnix\":1,\"mvpDungeonPlacements\":{\"Entries\":[{" +
                "\"CategoryId\":\"placement.category.room\",\"OptionId\":\"placement.option.room.narrow_hall\"," +
                "\"Revision\":1}],\"NextRevision\":2}}}");
            narrow = AuthorAbsentResearch(narrow);
            byte[] changed = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(narrow).Replace(
                "placement.option.room.narrow_hall", "placement.option.room.basic"));
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, narrow);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            var service = ConfiguredService(fixture, fileSystem, "service-repair-auth.json");
            fileSystem.Seed(service.SavePath, narrow);
            AssertSaveServiceObsolete(service, fileSystem, narrow);
            var historical = PrepareHistoricalNarrowHallRepair(narrow, 0);
            fileSystem.Seed(service.SavePath, changed);
            Assert.That(service.RepairNarrowHallToBasicAndRetry("gd66-live", out _), Is.Null);
            AssertSaveServiceObsolete(service, fileSystem, changed);

            // Use a distinct changed original to invalidate the historical proof.
            var third = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(changed).Replace(
                "\"lastSavedUtcUnix\":1", "\"lastSavedUtcUnix\":2"));
            fileSystem.Seed(service.SavePath, third);
            string result = ExactCompleteSaveAtomicPersistence.Persist(service.SavePath, fileSystem,
                narrow, historical.Original, fixture.Limits.Serialized.MaximumCollectionRecords);
            Assert.That(result, Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
            CollectionAssert.AreEqual(third, fileSystem.ReadAllBytes(service.SavePath));
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
        public void WrappedLegacySchemasMigrateThroughFrozenAuthorityAndCurrentRefuses(int schema)
        {
            var fixture = TimedFixture(schema);
            var fs = new FrozenMigration.DeterministicFileSystem();
            string active = FrozenMigration.ActivePath("wrapped-legacy-" + schema);
            fs.Seed(active, fixture.Original);
            AssertObsolete(fixture, fs, active, fixture.Original);
            MigrateFrozenSeven(fixture, "wrapped-legacy-" + schema);
        }

        [Test]
        public void UnwrappedLegacyMigratesThroughFrozenAuthorityAndCurrentRefuses()
        {
            var fixture = TimedFixture(1, true);
            var fs = new FrozenMigration.DeterministicFileSystem();
            string active = FrozenMigration.ActivePath("unwrapped-legacy");
            fs.Seed(active, fixture.Original);
            AssertObsolete(fixture, fs, active, fixture.Original);
            MigrateFrozenSeven(fixture, "unwrapped-legacy");
        }

        [Test]
        public void Load_CurrentTargetDoesNotRunHistoricalMigration()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                TimedFixture(6);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string activePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-current.json"));
            Assert.That(PhaseFourTestSupport.Upgrade(fixture.Result.Attempt.Candidate.GetBytes(),
                fixture.Limits, out byte[] currentBytes), Is.True);
            fileSystem.Seed(activePath, currentBytes);

            DetachedSpatialSaveLoadResult result = Coordinator(fixture).Load(activePath,
                Supported(fileSystem, activePath));

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.Disposition, Is.EqualTo(DetachedSpatialSaveLoadDisposition.CurrentTarget));
            Assert.That(result.Transaction, Is.Null);
            Assert.That(result.Recovery, Is.Null);
            Assert.That(result.RuntimeProjection, Is.Not.Null);
            Assert.That(result.Session, Is.Not.Null);
            CollectionAssert.AreEqual(currentBytes, result.Session.GetCurrentBytes());
            CollectionAssert.AreEqual(currentBytes, fileSystem.ReadAllBytes(activePath));
            Assert.That(fileSystem.Operations.Any(IsMutation), Is.False);
        }

        [Test]
        public void FrozenVerifiedCandidateRecoversOnlyThroughHistoricalAuthority()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                TimedFixture(6);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string activePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-recover-candidate.json"));
            var names = FrozenMigration.MaterializeJournal(fileSystem, fixture, activePath,
                SpatialMigrationJournalStage.CandidateVerified, includeBackup: true,
                includeStaging: true, activeCandidate: false);
            string directory = Path.GetDirectoryName(activePath);
            string[] paths = { names.Journal, names.OriginalBackup, names.CandidateStaging };
            byte[][] evidence = paths.Select(name => fileSystem.ReadAllBytes(Path.Combine(directory, name))).ToArray();

            AssertObsolete(fixture, fileSystem, activePath, fixture.Original);
            for (int i = 0; i < paths.Length; i++)
                CollectionAssert.AreEqual(evidence[i], fileSystem.ReadAllBytes(Path.Combine(directory, paths[i])));

            var historical = new DetachedSpatialMigrationTransaction(fileSystem, FrozenMigration.Recovery(fixture));
            var recovered = historical.Recover(activePath);
            Assert.That(recovered.IsSuccess, Is.True, recovered.Reason);
            Assert.That(recovered.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
            Assert.That(recovered.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            CollectionAssert.AreEqual(fixture.Result.Attempt.Candidate.GetBytes(), fileSystem.ReadAllBytes(activePath));
            Assert.That(historical.Recover(activePath).Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.AlreadyCommittedReason));
        }

        [Test]
        public void LegacyUnknownEvidenceRemainsInFrozenMigrationBytes()
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"lastSavedUtcUnix\":1,\"unknownPrimary\":[1,{\"n\":1.00}]}," +
                "\"unknownRoot\":{\"nested\":[true,null]}}");
            original = AuthorAbsentResearch(original);
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, original);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string activePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-unknown.json"));
            fileSystem.Seed(activePath, original);

            AssertObsolete(fixture, fileSystem, activePath, original);
            var historical = MigrateFrozenSeven(fixture, "unknown-evidence");
            string sessionJson = Encoding.UTF8.GetString(historical.GetBytes());
            Assert.That(sessionJson, Does.Contain("\"unknownPrimary\":[1,{\"n\":1.00}]"));
            Assert.That(sessionJson, Does.Contain("\"unknownRoot\":{\"nested\":[true,null]}"));
        }

        [Test]
        public void Load_UnsupportedPreflight_FailsBeforeReadingOrPublishing()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                TimedFixture(6);
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
        public void Load_QualifiedPreflightCannotAuthorizeDifferentOrNoncanonicalActivePath()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                TimedFixture(6);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string qualified = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-bound.json"));
            SpatialMigrationActivationPreflight preflight = Supported(fileSystem, qualified);
            string directory = Path.GetDirectoryName(qualified);
            string[] mismatched =
            {
                Path.Combine(directory, "gd66-sibling.json"),
                Path.Combine(directory, "other", "gd66-bound.json"),
                directory + Path.DirectorySeparatorChar + "." + Path.DirectorySeparatorChar +
                    "gd66-bound.json",
                directory + Path.DirectorySeparatorChar + "child" + Path.DirectorySeparatorChar + ".." +
                    Path.DirectorySeparatorChar + "gd66-bound.json"
            };

            foreach (string activePath in mismatched)
            {
                DetachedSpatialSaveLoadResult result = Coordinator(fixture).Load(activePath, preflight);
                Assert.That(result.IsSuccess, Is.False, activePath);
                Assert.That(result.Reason,
                    Is.EqualTo(DetachedSpatialMigrationTransaction.PathInvalidReason), activePath);
            }

            var redirected = new SpatialMigrationActivationPreflight(false,
                SpatialMigrationCapabilityReason.PathRedirected,
                SpatialMigrationPlatform.WindowsEditor, null);
            DetachedSpatialSaveLoadResult redirectedResult =
                Coordinator(fixture).Load(qualified, redirected);
            Assert.That(redirectedResult.IsSuccess, Is.False);
            Assert.That(redirectedResult.Reason,
                Is.EqualTo(SpatialMigrationCapabilityReason.PathRedirected));
        }

        [Test]
        public void Load_MalformedRawPayload_FailsWithoutRuntimeProjection()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                TimedFixture(6);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string activePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-malformed.json"));
            fileSystem.Seed(activePath, System.Text.Encoding.UTF8.GetBytes("{not-json"));

            DetachedSpatialSaveLoadResult result = Coordinator(fixture).Load(activePath,
                Supported(fileSystem, activePath));

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
            var fixture = FrozenMigration.PrepareEmptyFixture(6, false, original);
            Assert.That(fixture.Result.IsSuccess, Is.False);
            Assert.That(fixture.Result.Reason, Is.EqualTo(DetachedSpatialMigrationPreparer.OutcomeMismatchReason));
            var fs = new FrozenMigration.DeterministicFileSystem();
            string active = FrozenMigration.ActivePath("semantic-mismatch");
            fs.Seed(active, original);
            Assert.That(new DetachedSpatialMigrationTransaction(fs, FrozenMigration.Recovery(fixture))
                .Recover(active).TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            AssertObsolete(fixture, fs, active, original);
        }

        [Test]
        public void Load_NarrowHallBlocksMigrationButRetainsByteExactTrustedOriginal()
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"lastSavedUtcUnix\":1,\"mvpDungeonPlacements\":{\"Entries\":[{" +
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

            Assert.That(new DetachedSpatialMigrationTransaction(fileSystem, FrozenMigration.Recovery(fixture))
                .Recover(activePath).TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            AssertObsolete(fixture, fileSystem, activePath, original);
        }

        [Test]
        public void Load_InvalidBlankFloorFailsBeforePreflightEvaluation()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                TimedFixture(6);
            var invalidBlank = new RawLegacyBlankFloorContract(1,
                Array.Empty<RawLegacyBlankFloorNodeContract>(), true, true,
                new[] { "Nodes", "NextRevision" },
                new[] { "FloorIndex", "NodeIndex", "SlotId", "CategoryId", "OptionId", "Revision" });
            DetachedSpatialSaveLoadCoordinator coordinator = Coordinator(fixture, invalidBlank);
            bool evaluated = false;

            DetachedSpatialSaveLoadResult result = coordinator.Load("invalid-path", path =>
            { evaluated = true; return Supported(new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem(), path); });

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo("gd66.profile.invalid"));
            Assert.That(evaluated, Is.False);
        }

        [Test]
        public void Load_MissingMigrationProfilePreservesExactSelectionReason()
        {
            var fixture = TimedFixture(6);
            var data = fixture.Compatibility.Value;
            data.MigrationProfiles = Array.Empty<SpatialMigrationCompatibilityProfile>();
            var compatibility = new SpatialLayoutCompatibilitySnapshot(data);
            int frozenSchema = CanonicalSaveSchemaVersions.FrozenLegacyCanonicalMigrationTarget;
            var layout = compatibility.SelectContract(frozenSchema);
            Assert.That(layout.Success, Is.True, layout.Code);
            var selected = compatibility.SelectMigration(6, frozenSchema, layout.Value.CanonicalLayoutContractVersion);
            Assert.That(selected.Success, Is.False);
            Assert.That(selected.Code, Is.EqualTo("gd66.profile.missing"));

            var fs = new FrozenMigration.DeterministicFileSystem();
            string active = FrozenMigration.ActivePath("missing-migration-profile");
            fs.Seed(active, fixture.Original);
            AssertObsolete(fixture, fs, active, fixture.Original);
        }

        [Test]
        public void Load_RepresentativeTransactionFailureReturnsNoRuntimeAuthority()
        {
            var fixture = TimedFixture(6);
            var current = new FrozenMigration.DeterministicFileSystem();
            string path = FrozenMigration.ActivePath("migration-write-failure");
            current.Seed(path, fixture.Original);
            AssertObsolete(fixture, current, path, fixture.Original);

            var fs = new FrozenMigration.DeterministicFileSystem();
            fs.Seed(path, fixture.Original);
            fs.EnableFailure(FrozenMigration.OperationType.Write, 1);
            var failed = new DetachedSpatialMigrationTransaction(fs, FrozenMigration.Recovery(fixture))
                .Execute(path, fixture.Result.Attempt);
            Assert.That(failed.IsSuccess, Is.False);
            Assert.That(failed.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            fs.DisableFailure();
            CollectionAssert.AreEqual(fixture.Original, fs.ReadAllBytes(path));
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

            var result = MigrateFrozenSeven(FrozenMigration.PrepareEmptyFixture(4, false, original), "authority");
            Assert.That(result.State.Floors, Has.Length.EqualTo(1));
            Assert.That(result.State.Floors[0].Layout.Rooms, Has.Length.EqualTo(1));
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

            var compactResult = MigrateFrozenSeven(FrozenMigration.PrepareEmptyFixture(4, false, compact), "compact");
            var prettyResult = MigrateFrozenSeven(FrozenMigration.PrepareEmptyFixture(4, false, pretty), "pretty");
            Assert.That(SemanticSpatialJson(prettyResult.State.Floors),
                Is.EqualTo(SemanticSpatialJson(compactResult.State.Floors)));
            Assert.That(prettyResult.State.Authority.MigrationTransactionId,
                Is.Not.EqualTo(compactResult.State.Authority.MigrationTransactionId));
            Assert.That(prettyResult.State.Authority.MigrationDescriptorFingerprint,
                Is.Not.EqualTo(compactResult.State.Authority.MigrationDescriptorFingerprint));
            var compactAgain = MigrateFrozenSeven(FrozenMigration.PrepareEmptyFixture(4, false, compact), "compact-again");
            CollectionAssert.AreEqual(compactResult.GetBytes(), compactAgain.GetBytes());
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
                Floors = floors,
                LifecycleAndOwnership = NativeStructuralIdentity.CreateInitialLifecycle(floors)
            });

        [Test]
        public void Load_ActualPrettyJsonUtilitySaveRootMigrates()
        {
            var root = new SaveRoot { schema = "save_root", schemaVersion = 6,
                primary = new SaveData { saveVersion = 6, lastSavedUtcUnix = 1 } };
            byte[] pretty = Encoding.UTF8.GetBytes(JsonUtility.ToJson(root, true));
            var historical = FrozenMigration.PrepareEmptyFixture(6, false, pretty);
            var migrated = MigrateFrozenSeven(historical, "json-utility-pretty");
            Assert.That(migrated.State.Authority.CreationKind, Is.EqualTo(CanonicalSpatialCreationKind.Migrated));
            var fs = new FrozenMigration.DeterministicFileSystem();
            string active = FrozenMigration.ActivePath("json-utility-pretty");
            fs.Seed(active, pretty);
            AssertObsolete(historical, fs, active, pretty);
        }

        private static DetachedCompleteSaveValidationResult MigrateFrozenSeven(
            FrozenMigration.PreparedFixture fixture, string identity)
        {
            Assert.That(fixture.Result.IsSuccess, Is.True, fixture.Result.Reason);
            var fs = new FrozenMigration.DeterministicFileSystem();
            string path = FrozenMigration.ActivePath("frozen-gd66-" + identity);
            fs.Seed(path, fixture.Original);
            var transaction = new DetachedSpatialMigrationTransaction(fs, FrozenMigration.Recovery(fixture));
            var executed = transaction.Execute(path, fixture.Result.Attempt);
            Assert.That(executed.IsSuccess, Is.True, executed.Reason);
            Assert.That(executed.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
            byte[] published = fs.ReadAllBytes(path);
            CollectionAssert.AreEqual(fixture.Result.Attempt.Candidate.GetBytes(), published);
            var validated = DetachedCompleteSaveContract.ParseValidateFrozenSchemaSevenAndRoundTrip(
                published, fixture.Limits);
            Assert.That(validated.IsValid, Is.True, validated.Reason);
            Assert.That(validated.CurrentTargetValidated, Is.False);
            CollectionAssert.AreEqual(published, validated.GetBytes());
            return validated;
        }

        private static FrozenMigration.PreparedFixture PrepareHistoricalNarrowHallRepair(byte[] original, int target)
        {
            var repaired = LegacyNarrowHallRepair.Prepare(original, ClassifyRepair(original),
                FrozenMigration.RawLimitsForCoordinator, new RawSaveEnvelopeVersionContract(1, 6),
                FrozenMigration.BlankFloorForCoordinator, target);
            Assert.That(repaired.IsSuccess, Is.True, repaired.Reason);
            return FrozenMigration.PrepareEmptyFixture(6, false, repaired.GetBytes());
        }

        private static void AssertObsolete(FrozenMigration.PreparedFixture fixture,
            FrozenMigration.DeterministicFileSystem fs, string path, byte[] original)
        {
            var result = Coordinator(fixture).Load(path, Supported(fs, path));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo(RecoveryEpisode.ObsoleteDevelopmentSave));
            Assert.That(result.Session, Is.Null);
            Assert.That(result.RuntimeProjection, Is.Null);
            Assert.That(result.Transaction, Is.Null);
            Assert.That(result.Recovery, Is.Null);
            CollectionAssert.AreEqual(original, fs.ReadAllBytes(path));
            Assert.That(fs.Operations.Any(IsMutation), Is.False);
        }

        private static void AssertSaveServiceObsolete(SaveService service,
            FrozenMigration.DeterministicFileSystem fs, byte[] original)
        {
            Assert.That(service.LoadOrCreate("gd66-live", out string banner), Is.Null);
            Assert.That(banner, Is.EqualTo(RecoveryEpisode.PlayerLocalizationKey(RecoveryEpisode.ObsoleteDevelopmentSave)));
            Assert.That(service.CanonicalSession, Is.Null);
            Assert.That(service.NarrowHallRepairAvailable, Is.False);
            CollectionAssert.AreEqual(original, fs.ReadAllBytes(service.SavePath));
            Assert.That(fs.Operations.Any(IsMutation), Is.False);
        }

        private static bool IsMutation(FrozenMigration.FileOperation op) =>
            op.Type == FrozenMigration.OperationType.Write || op.Type == FrozenMigration.OperationType.Replace ||
            op.Type == FrozenMigration.OperationType.Move || op.Type == FrozenMigration.OperationType.Delete;

        private static SaveService ConfiguredService(
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture,
            ISpatialMigrationFileSystem fileSystem, string filename, ICollection<string> errors = null)
        {
            string directory = Path.GetFullPath(Path.Combine(Path.GetTempPath(),
                "gd66-save-service-tests"));
            var service = new SaveService(errors == null ? new SimpleLogger(false) : CapturingLogger(errors),
                new SaveConfig { fileName = filename, useAtomicWrites = true }, directory);
            service.ConfigureCanonical(PhaseFourTestSupport.ProductionProfile(), fixture.Production, fixture.Compatibility,
                LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes), fixture.LegacyBytes,
                PhaseFourTestSupport.PositionProfiles(fixture.Limits),
                PhaseFourTestSupport.Occupancy(fixture.Limits));
            var economy = PhaseFourTestSupport.Economy(fixture.Production, fixture.Limits);
            service.ConfigureStructuralEconomy(economy);
            service.ConfigureContentAcquisitionEconomy(
                PhaseFourTestSupport.Acquisition(economy, fixture.Limits));
            BindRecovery(service, fixture);
            service.SetPreflightEvaluatorForTests(path => Supported(fileSystem, path));
            return service;
        }

        [TestCase("success")]
        [TestCase("write")]
        [TestCase("replace")]
        [TestCase("readback")]
        public void FrozenSchemaTenUpgradeReopensExactlyAndRollsBackFailures(string failure)
        {
            var fixture = TimedFixture(6);
            var f = DetachedCanonicalWriteAuthorityTests.Fixture.Create(null);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)));
            // Synthetic schema 10 representation from the established fixture helper.
            byte[] ten = Encoding.UTF8.GetBytes(PhaseFourTestSupport.FrozenTen(f.Session.GetCurrentBytes()));
            Assert.That(SchemaTenToElevenUpgrade.TryPrepare(ten, f.Profile.Canonical, out byte[] eleven), Is.True);
            var fs = new FrozenMigration.DeterministicFileSystem();
            fs.Seed(f.ActivePath, ten);
            AssertObsolete(fixture, fs, f.ActivePath, ten);
            bool ReadEleven(byte[] bytes) =>
                DetachedCompleteSaveContract.ParseValidateFrozenSchemaElevenAndRoundTrip(bytes, f.Profile.Canonical).IsValid;
            string Persist(FrozenMigration.DeterministicFileSystem target) =>
                ExactCompleteSaveAtomicPersistence.Persist(f.ActivePath, target, ten, eleven, 64, ReadEleven);
            if (failure == "readback")
            {
                var baseline = new FrozenMigration.DeterministicFileSystem();
                baseline.Seed(f.ActivePath, ten);
                Assert.That(Persist(baseline), Is.Null);
                var operations = baseline.Operations.ToArray();
                int replacement = Array.FindIndex(operations, op => op.Type == FrozenMigration.OperationType.Replace);
                Assert.That(replacement, Is.GreaterThanOrEqualTo(0));
                int readNumber = operations.Take(replacement).Count(op =>
                    op.Type == FrozenMigration.OperationType.Read && op.Paths[0] == f.ActivePath) + 1;
                fs.EnableTargetedFailure(FrozenMigration.OperationType.Read,
                    paths => paths[0] == f.ActivePath, readNumber, false);
            }
            else if (failure != "success")
                fs.EnableTargetedFailure(failure == "write" ? FrozenMigration.OperationType.Write :
                    FrozenMigration.OperationType.Replace, _ => true, 1, false);
            if (failure != "success")
            {
                Assert.That(Persist(fs), Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
                fs.DisableFailure();
                CollectionAssert.AreEqual(ten, fs.ReadAllBytes(f.ActivePath));
            }
            Assert.That(Persist(fs), Is.Null);
            byte[] persisted = fs.ReadAllBytes(f.ActivePath);
            var reopened = DetachedCompleteSaveContract.ParseValidateFrozenSchemaElevenAndRoundTrip(
                persisted, f.Profile.Canonical);
            Assert.That(reopened.IsValid, Is.True, reopened.Reason);
            Assert.That(reopened.CurrentTargetValidated, Is.False);
            Assert.That(reopened.State.Floors.Single().ActivationState, Is.EqualTo(FloorActivationState.Active));
            CollectionAssert.AreEqual(eleven, reopened.GetBytes());
            Assert.That(Persist(fs), Is.Null);
            CollectionAssert.AreEqual(eleven, fs.ReadAllBytes(f.ActivePath));
        }

        private static Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture TimedFixture(int schema, bool unwrapped = false) =>
            Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(schema, unwrapped,
                AuthorAbsentResearch(Encoding.UTF8.GetBytes(unwrapped ? "{\"saveVersion\":1,\"lastSavedUtcUnix\":1}" :
                    "{\"schema\":\"save_root\",\"schemaVersion\":"  + schema + ",\"primary\":{\"lastSavedUtcUnix\":1}}"), unwrapped));

        private static DetachedSpatialSaveLoadCoordinator Coordinator(
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture,
            RawLegacyBlankFloorContract blankFloor = null,
            SpatialLayoutCompatibilitySnapshot compatibility = null)
        {
            var profile = PhaseFourTestSupport.ProductionProfile();
            var coordinator = new DetachedSpatialSaveLoadCoordinator(profile, compatibility ?? fixture.Compatibility,
                fixture.Production, fixture.LegacyBytes, new Dictionary<string, byte[]>(),
                new RawSaveEnvelopeVersionContract(1, 6),
                blankFloor ?? Gd66DetachedSpatialMigrationTransactionTests.BlankFloorForCoordinator,
                PhaseFourTestSupport.PositionProfiles(fixture.Limits),
                PhaseFourTestSupport.Occupancy(fixture.Limits));
            var inputs = RecoveryInputs(fixture);
            var time = RecoveryClock(inputs);
            // Deliberately unbound malformed compatibility is a negative-only
            // early-selection fixture. It must fail before any publication.
            if (compatibility == null) coordinator.ConfigureRecovery(inputs, time);
            return coordinator;
        }

        // Newly authored dormant compatibility fixtures consume the complete
        // validated inputs before load. This is not historical A>C producer proof.
        private static RecoveryRuntimeInputs RecoveryInputs(Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture f) =>
            PhaseFourTestSupport.CurrentContext(f.Compatibility, f.Production, f.LegacyBytes,
                PhaseFourTestSupport.ProductionProfile().Canonical).RecoveryInputs;
        private sealed class LoadClock : IPreciseTimeSource
        {
            public UnixTimePoint UtcNow() => new UnixTimePoint(1, 0);
            public long UtcNowUnixSeconds() => 1;
        }
        private static TimeService RecoveryClock(RecoveryRuntimeInputs inputs)
        {
            var time = new TimeService(new SimpleLogger(false), inputs.Bootstrap.tickSeconds,
                inputs.Bootstrap.timeRules.detectClockSkewSeconds, new LoadClock(), () => 0);
            time.BeginRecoveryForeground(inputs.TwoFloor.UnitsPerSecond);
            return time;
        }
        private static void BindRecovery(SaveService service, Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture f)
        {
            var inputs = RecoveryInputs(f);
            service.SetTimeSourceForTests(new LoadClock());
            service.ConfigureRecovery(inputs, RecoveryClock(inputs));
        }

        private static byte[] AuthorAbsentResearch(byte[] source, bool unwrapped = false)
        {
            // Only fresh in-memory synthetic sources call this before preparing
            // their migration identity. Never repair a loaded historical save.
            string json = Encoding.UTF8.GetString(source);
            if (json.Contains("\"researchPending\"") || json.Contains("\"researchProgress\"")) return source;
            const string absence = "\"researchPending\":null,\"researchProgress\":null,";
            if (unwrapped) return Encoding.UTF8.GetBytes("{" + absence + json.Substring(1));
            var pattern = new System.Text.RegularExpressions.Regex("(\"primary\"\\s*:\\s*\\{)");
            Assert.That(pattern.Matches(json).Count, Is.EqualTo(1));
            return Encoding.UTF8.GetBytes(pattern.Replace(json, "$1" + absence, 1));
        }

        private static SpatialMigrationActivationPreflight Supported(
            ISpatialMigrationFileSystem fileSystem, string activePath) =>
            new SpatialMigrationActivationPreflight(true, SpatialMigrationCapabilityReason.Ready,
                SpatialMigrationPlatform.WindowsEditor, fileSystem, Path.GetFullPath(activePath));
    }
}
#endif
