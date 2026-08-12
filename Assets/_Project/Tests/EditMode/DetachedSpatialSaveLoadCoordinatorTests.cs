#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

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

        private static DetachedSpatialSaveLoadCoordinator Coordinator(
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture)
        {
            var profile = new SaveSpatialMigrationLimitsProfile(
                Gd66DetachedSpatialMigrationTransactionTests.RawLimitsForCoordinator,
                fixture.Limits, fixture.WholeLimits);
            return new DetachedSpatialSaveLoadCoordinator(profile, fixture.Compatibility,
                fixture.Production, fixture.LegacyBytes, new Dictionary<string, byte[]>(),
                new RawSaveEnvelopeVersionContract(1, 6),
                Gd66DetachedSpatialMigrationTransactionTests.BlankFloorForCoordinator);
        }

        private static SpatialMigrationActivationPreflight Supported(
            ISpatialMigrationFileSystem fileSystem) => new SpatialMigrationActivationPreflight(true,
                SpatialMigrationCapabilityReason.Ready, SpatialMigrationPlatform.WindowsEditor, fileSystem);
    }
}
#endif
