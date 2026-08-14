#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66GameRootBootIntegrationTests
    {
        private GameObject rootObject;

        [TearDown]
        public void TearDown()
        {
            if (rootObject != null) Object.DestroyImmediate(rootObject);
        }

        [Test]
        public void SuccessfulCanonicalBootCompletionInstallsAuthorityAndEntersHome()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture = Fixture();
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            SaveService service = Service(fixture, fileSystem, "root-success.json");
            SaveData canonical = service.LoadOrCreate("gd66-live", out string banner);
            Assert.That(canonical, Is.Not.Null, banner);
            DetachedCanonicalSaveSession beforeSession = service.CanonicalSession;
            GameRoot root = Root(service);

            bool completed = root.CompleteSuccessfulBootForTests(canonical, true);

            Assert.That(completed, Is.True);
            Assert.That(root.Save, Is.Not.Null);
            Assert.That(root.Save.validatedCanonicalSpatialState, Is.Not.Null);
            Assert.That(root.TimeService, Is.Not.Null);
            Assert.That(root.TimeService.AttachedSaveForTests, Is.SameAs(root.Save));
            Assert.That(root.Save, Is.Not.SameAs(canonical));
            Assert.That(service.CanonicalSession, Is.Not.SameAs(beforeSession));
            Assert.That(service.CanonicalSession.GetCurrentBytes(),
                Is.EqualTo(fileSystem.ReadAllBytes(service.SavePath)));
            Assert.That(root.GameplayServicesInitializedForTests, Is.True);
            Assert.That(root.StateLine, Does.Contain("Home"));
        }

        [Test]
        public void FailedBootCompletionLeavesGameplayAndHomeUninitialized()
        {
            GameRoot root = Root(null);

            bool completed = root.CompleteSuccessfulBootForTests(null, true);

            Assert.That(completed, Is.False);
            Assert.That(root.Save, Is.Null);
            Assert.That(root.TimeService, Is.Null);
            Assert.That(root.GameplayServicesInitializedForTests, Is.False);
            Assert.That(root.StateLine, Does.Not.Contain("Home"));
        }

        [Test]
        public void NarrowHallBlockedRootRepairsThroughRealBootCompletionOnce()
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"mvpDungeonPlacements\":{\"Entries\":[{" +
                "\"CategoryId\":\"placement.category.room\",\"OptionId\":\"placement.option.room.narrow_hall\"," +
                "\"Revision\":1}],\"NextRevision\":2}}}");
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, original);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            var errors = new List<string>();
            SaveService service = Service(fixture, fileSystem, "root-repair.json", errors);
            fileSystem.Seed(service.SavePath, original);
            Assert.That(service.LoadOrCreate("gd66-live", out _), Is.Null);
            CollectionAssert.AreEqual(new[] { NarrowHallLoadError }, errors);
            GameRoot root = Root(service);
            Assert.That(root.Save, Is.Null);
            Assert.That(root.TimeService, Is.Null);
            Assert.That(service.NarrowHallRepairAvailable, Is.True);
            DetachedCanonicalSaveSession blockedSession = service.CanonicalSession;
            const string blockedBanner = "Replace Narrow Hall to continue safely.";
            root.SetBanner(blockedBanner);
            Assert.That(root.BannerMessage, Is.EqualTo(blockedBanner));

            bool repaired = root.TryRepairMigrationBlockedNarrowHall();

            Assert.That(repaired, Is.True);
            Assert.That(root.Save, Is.Not.Null);
            Assert.That(root.Save.validatedCanonicalSpatialState, Is.Not.Null);
            Assert.That(root.TimeService.AttachedSaveForTests, Is.SameAs(root.Save));
            Assert.That(service.NarrowHallRepairAvailable, Is.False);
            Assert.That(service.CanonicalSession, Is.Not.Null);
            Assert.That(service.CanonicalSession, Is.Not.SameAs(blockedSession));
            Assert.That(service.CanonicalSession.GetCurrentBytes(),
                Is.EqualTo(fileSystem.ReadAllBytes(service.SavePath)));
            Assert.That(root.GameplayServicesInitializedForTests, Is.True);
            Assert.That(root.StateLine, Does.Contain("Home"));
            Assert.That(root.BannerMessage, Is.Not.EqualTo(blockedBanner));
            Assert.That(root.BannerMessage, Does.Not.Contain("Narrow Hall"));
            TimeService initializedTimeService = root.TimeService;
            SaveData initializedSave = root.Save;
            Assert.That(root.TryRepairMigrationBlockedNarrowHall(), Is.False);
            Assert.That(root.TimeService, Is.SameAs(initializedTimeService));
            Assert.That(root.Save, Is.SameAs(initializedSave));
            Assert.That(root.TimeService.AttachedSaveForTests, Is.SameAs(root.Save));
        }

        [Test]
        public void FailedNarrowHallRepairKeepsRootBlockedWithoutPartialServices()
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"mvpDungeonPlacements\":{\"Entries\":[{" +
                "\"CategoryId\":\"placement.category.room\",\"OptionId\":\"placement.option.room.narrow_hall\"," +
                "\"Revision\":1}],\"NextRevision\":2}}}");
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, original);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            var errors = new List<string>();
            SaveService service = Service(fixture, fileSystem, "root-repair-failed.json", errors);
            fileSystem.Seed(service.SavePath, original);
            Assert.That(service.LoadOrCreate("gd66-live", out _), Is.Null);
            CollectionAssert.AreEqual(new[] { NarrowHallLoadError }, errors);
            GameRoot root = Root(service);
            const string blockedBanner = "Replace Narrow Hall to continue safely.";
            root.SetBanner(blockedBanner);
            fileSystem.EnableFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write, 1);

            bool repaired = root.TryRepairMigrationBlockedNarrowHall();

            Assert.That(repaired, Is.False);
            Assert.That(root.Save, Is.Null);
            Assert.That(root.TimeService, Is.Null);
            Assert.That(root.GameplayServicesInitializedForTests, Is.False);
            Assert.That(root.StateLine, Does.Not.Contain("Home"));
            Assert.That(root.BannerMessage, Is.Not.Empty);
            Assert.That(root.BannerMessage, Is.Not.EqualTo(blockedBanner));
        }

        private GameRoot Root(SaveService service)
        {
            rootObject = new GameObject("GD66 GameRoot integration");
            GameRoot root = rootObject.AddComponent<GameRoot>();
            root.structureSimulationConfigJson = Asset("Assets/_Project/Data/Bootstrap/structure_simulation_config.json");
            root.runSimulationConfigJson = Asset("Assets/_Project/Data/Bootstrap/run_simulation_config.json");
            root.lootConfigJson = Asset("Assets/_Project/Data/Bootstrap/loot_config.json");
            root.AttachSaveServiceForTests(service);
            return root;
        }

        private static SaveService Service(
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture,
            ISpatialMigrationFileSystem fileSystem, string filename, ICollection<string> errors = null)
        {
            var logger = errors == null ? new SimpleLogger(false) :
                new SimpleLogger(false, (level, formatted) =>
                {
                    if (level == "ERROR") errors.Add(formatted);
                });
            var service = new SaveService(logger,
                new SaveConfig { fileName = filename, useAtomicWrites = true },
                Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-root-tests")));
            service.ConfigureCanonical(new SaveSpatialMigrationLimitsProfile(
                    Gd66DetachedSpatialMigrationTransactionTests.RawLimitsForCoordinator,
                    fixture.Limits, fixture.WholeLimits), fixture.Production, fixture.Compatibility,
                LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes), fixture.LegacyBytes);
            service.SetPreflightEvaluatorForTests(path => new SpatialMigrationActivationPreflight(true,
                SpatialMigrationCapabilityReason.Ready, SpatialMigrationPlatform.WindowsEditor, fileSystem));
            return service;
        }

        private const string NarrowHallLoadError =
            "[ERROR] GD66 load failed: gd66.content.migration_blocked_narrow_hall";

        private static Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture Fixture() =>
            Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);

        private static TextAsset Asset(string path) => AssetDatabase.LoadAssetAtPath<TextAsset>(path);

    }
}
#endif
