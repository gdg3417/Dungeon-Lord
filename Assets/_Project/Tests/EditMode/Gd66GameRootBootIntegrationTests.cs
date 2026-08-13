#if UNITY_EDITOR
using System.IO;
using System.Reflection;
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
            GameRoot root = Root(service);

            bool completed = root.CompleteSuccessfulBootForTests(canonical, true);

            Assert.That(completed, Is.True);
            Assert.That(root.Save, Is.Not.Null);
            Assert.That(root.Save.validatedCanonicalSpatialState, Is.Not.Null);
            Assert.That(root.TimeService, Is.Not.Null);
            Assert.That(root.TimeService.AttachedSaveForTests, Is.SameAs(root.Save));
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
            SaveService service = Service(fixture, fileSystem, "root-repair.json");
            fileSystem.Seed(service.SavePath, original);
            Assert.That(service.LoadOrCreate("gd66-live", out _), Is.Null);
            GameRoot root = Root(service);
            Assert.That(root.Save, Is.Null);
            Assert.That(root.TimeService, Is.Null);
            Assert.That(service.NarrowHallRepairAvailable, Is.True);

            bool repaired = root.TryRepairMigrationBlockedNarrowHall();

            Assert.That(repaired, Is.True);
            Assert.That(root.Save, Is.Not.Null);
            Assert.That(root.TimeService.AttachedSaveForTests, Is.SameAs(root.Save));
            Assert.That(root.GameplayServicesInitializedForTests, Is.True);
            Assert.That(root.StateLine, Does.Contain("Home"));
            Assert.That(root.TryRepairMigrationBlockedNarrowHall(), Is.False);
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
            SaveService service = Service(fixture, fileSystem, "root-repair-failed.json");
            fileSystem.Seed(service.SavePath, original);
            Assert.That(service.LoadOrCreate("gd66-live", out _), Is.Null);
            GameRoot root = Root(service);
            fileSystem.EnableFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write, 1);

            bool repaired = root.TryRepairMigrationBlockedNarrowHall();

            Assert.That(repaired, Is.False);
            Assert.That(root.Save, Is.Null);
            Assert.That(root.TimeService, Is.Null);
            Assert.That(root.GameplayServicesInitializedForTests, Is.False);
            Assert.That(root.StateLine, Does.Not.Contain("Home"));
        }

        private GameRoot Root(SaveService service)
        {
            rootObject = new GameObject("GD66 GameRoot integration");
            GameRoot root = rootObject.AddComponent<GameRoot>();
            root.structureSimulationConfigJson = Asset("Assets/_Project/Data/Bootstrap/structure_simulation_config.json");
            root.runSimulationConfigJson = Asset("Assets/_Project/Data/Bootstrap/run_simulation_config.json");
            root.lootConfigJson = Asset("Assets/_Project/Data/Bootstrap/loot_config.json");
            Set(root, "<SaveService>k__BackingField", service);
            return root;
        }

        private static SaveService Service(
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture,
            ISpatialMigrationFileSystem fileSystem, string filename)
        {
            var service = new SaveService(new SimpleLogger(false),
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

        private static Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture Fixture() =>
            Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);

        private static TextAsset Asset(string path) => AssetDatabase.LoadAssetAtPath<TextAsset>(path);

        private static void Set(GameRoot root, string field, object value) => typeof(GameRoot)
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(root, value);
    }
}
#endif
