#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class DetachedSpatialSaveLoadCoordinatorTests
    {
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
            Assert.That(JsonUtility.ToJson(prettyResult.Validation.State),
                Is.EqualTo(JsonUtility.ToJson(compactResult.Validation.State)));
        }

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
