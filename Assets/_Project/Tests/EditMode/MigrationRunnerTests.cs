using DungeonBuilder.M0;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class MigrationRunnerTests
    {
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
    }
}
