using DungeonBuilder.M0;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class MigrationRunnerTests
    {
        private sealed class V0ToV1Migration : ISaveMigrationStep
        {
            public int FromVersion => 0;
            public int ToVersion => 1;

            public void Apply(SaveRoot root)
            {
                if (root.primary == null)
                {
                    root.primary = new SaveData();
                }

                if (string.IsNullOrEmpty(root.primary.lastKnownAppState))
                {
                    root.primary.lastKnownAppState = "Boot";
                }
            }
        }

        private sealed class V1ToV2Migration : ISaveMigrationStep
        {
            public int FromVersion => 1;
            public int ToVersion => 2;

            public void Apply(SaveRoot root)
            {
                root.primary.lastKnownAppState = "Migrated";
            }
        }

        [Test]
        public void Run_AppliesRegisteredStepsToTargetVersion()
        {
            var runner = new MigrationRunner();
            runner.Register(new V1ToV2Migration());

            var root = new SaveRoot
            {
                schemaVersion = 1,
                primary = new SaveData { lastKnownAppState = "Boot" }
            };

            bool ok = runner.Run(root, 2, out string error);

            Assert.IsTrue(ok);
            Assert.IsEmpty(error);
            Assert.AreEqual(2, root.schemaVersion);
            Assert.AreEqual("Migrated", root.primary.lastKnownAppState);
        }

        [Test]
        public void Run_FailsWhenStepMissing()
        {
            var runner = new MigrationRunner();
            var root = new SaveRoot { schemaVersion = 1, primary = new SaveData() };

            bool ok = runner.Run(root, 2, out string error);

            Assert.IsFalse(ok);
            StringAssert.Contains("Missing migration step", error);
        }

        [Test]
        public void Fixture_OldSchemaRoot_MigratesAndRoundTrips()
        {
            const string fixture = "{\"schema\":\"save_root\",\"schemaVersion\":0,\"primary\":{\"saveVersion\":1,\"contentVersion\":\"0.9.0\",\"totalTicks\":12}}";

            SaveRoot root = JsonUtility.FromJson<SaveRoot>(fixture);
            Assert.NotNull(root);
            Assert.AreEqual(0, root.schemaVersion);

            var runner = new MigrationRunner();
            runner.Register(new V0ToV1Migration());
            runner.Register(new V1ToV2Migration());

            bool ok = runner.Run(root, 2, out string error);

            Assert.IsTrue(ok, error);
            Assert.AreEqual(2, root.schemaVersion);
            Assert.AreEqual("Migrated", root.primary.lastKnownAppState);

            string jsonAfter = JsonUtility.ToJson(root);
            SaveRoot parsedAgain = JsonUtility.FromJson<SaveRoot>(jsonAfter);

            Assert.NotNull(parsedAgain);
            Assert.AreEqual(2, parsedAgain.schemaVersion);
            Assert.AreEqual("Migrated", parsedAgain.primary.lastKnownAppState);
            Assert.AreEqual(12, parsedAgain.primary.totalTicks);
        }

        [Test]
        public void Fixture_CurrentSchemaRoot_RoundTripsWithoutMigration()
        {
            const string fixture = "{\"schema\":\"save_root\",\"schemaVersion\":1,\"primary\":{\"saveVersion\":1,\"contentVersion\":\"1.0.0\",\"lastKnownAppState\":\"Home\",\"totalTicks\":55}}";

            SaveRoot root = JsonUtility.FromJson<SaveRoot>(fixture);
            Assert.NotNull(root);

            var runner = new MigrationRunner();
            runner.Register(new V1ToV2Migration());

            bool ok = runner.Run(root, 1, out string error);

            Assert.IsTrue(ok, error);
            Assert.IsEmpty(error);
            Assert.AreEqual(1, root.schemaVersion);
            Assert.AreEqual("Home", root.primary.lastKnownAppState);

            string jsonAfter = JsonUtility.ToJson(root);
            SaveRoot parsedAgain = JsonUtility.FromJson<SaveRoot>(jsonAfter);
            Assert.NotNull(parsedAgain);
            Assert.AreEqual(1, parsedAgain.schemaVersion);
            Assert.AreEqual("Home", parsedAgain.primary.lastKnownAppState);
            Assert.AreEqual(55, parsedAgain.primary.totalTicks);
        }

        [Test]
        public void Fixture_MalformedRoot_ParseThrows_AsExpectedForFallbackHandling()
        {
            const string malformedFixture = "{not-valid-json";

            Assert.Throws<System.ArgumentException>(() => JsonUtility.FromJson<SaveRoot>(malformedFixture));
        }
    }
}
