using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class StructureSimulationTests
    {
        [Test]
        public void StructureTickOutcomes_Are_Reproducible_For_FixedTickCount()
        {
            var layout = BuildLayout();
            StructureSimulationConfig config = BuildTestConfig();
            var pass = new StructureSimulationPass(new HeatSystem(), config);

            var runA = RunTicks(layout, pass, 20);
            var runB = RunTicks(layout, pass, 20);

            Assert.That(runA.ManaReserve, Is.EqualTo(runB.ManaReserve));
            Assert.That(runA.Heat, Is.EqualTo(runB.Heat));
            Assert.That(runA.IsHeatCrisisActive, Is.EqualTo(runB.IsHeatCrisisActive));
        }

        [Test]
        public void HeatCrisis_Enters_At_Threshold()
        {
            var layout = DungeonLayoutState.CreateEmpty(1, 1);
            new PlacementService().PlaceStructure(layout, 0, 0, StructureSimulationPass.RiskLabBasicId);

            StructureSimulationConfig config = BuildTestConfig();
            var runtime = new StructureRuntimeState { Heat = config.HeatCrisisEnterThreshold - 5d };
            var pass = new StructureSimulationPass(new HeatSystem(), config);

            StructureTickResult result = pass.SimulateTick(layout, runtime, 1);
            double expectedHeat = (config.HeatCrisisEnterThreshold - 5d) + Find(config, StructureSimulationPass.RiskLabBasicId).HeatDeltaPerTick;

            Assert.That(result.Heat, Is.EqualTo(expectedHeat));
            Assert.That(result.IsHeatCrisisActive, Is.True);
        }

        [Test]
        public void HeatCrisis_Recovers_Below_RecoveryThreshold()
        {
            var layout = DungeonLayoutState.CreateEmpty(1, 1);
            new PlacementService().PlaceStructure(layout, 0, 0, StructureSimulationPass.HeatScrubberBasicId);

            StructureSimulationConfig config = BuildTestConfig();
            var runtime = new StructureRuntimeState { Heat = config.HeatCrisisRecoveryThreshold + 1d, IsHeatCrisisActive = true };
            var pass = new StructureSimulationPass(new HeatSystem(), config);

            StructureTickResult result = pass.SimulateTick(layout, runtime, 1);
            double expectedHeat = (config.HeatCrisisRecoveryThreshold + 1d) + Find(config, StructureSimulationPass.HeatScrubberBasicId).HeatDeltaPerTick;

            Assert.That(result.Heat, Is.EqualTo(expectedHeat));
            Assert.That(result.IsHeatCrisisActive, Is.False);
        }

        [Test]
        public void SaveLoad_Preserves_Structure_And_Crisis_State()
        {
            var layout = BuildLayout();
            var save = new SaveData
            {
                dungeonLayout = layout,
                structureRuntime = new StructureRuntimeState
                {
                    ManaReserve = 150d,
                    Heat = 102d,
                    IsHeatCrisisActive = true,
                    LastProcessedTick = 42
                }
            };

            string json = JsonUtility.ToJson(save);
            SaveData loaded = JsonUtility.FromJson<SaveData>(json);

            Assert.That(loaded.dungeonLayout.Slots[0].StructureId, Is.EqualTo(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(loaded.structureRuntime.IsHeatCrisisActive, Is.True);
            Assert.That(loaded.structureRuntime.Heat, Is.EqualTo(102d));
            Assert.That(loaded.structureRuntime.ManaReserve, Is.EqualTo(150d));
            Assert.That(loaded.structureRuntime.LastProcessedTick, Is.EqualTo(42));
        }


        [Test]
        public void SaveMigration_Backfills_MissingStructureRuntime_FromLegacyRoot()
        {
            const string legacyRootJson = "{\"schemaVersion\":1,\"primary\":{\"saveVersion\":1,\"contentVersion\":\"1.0.0\",\"dungeonLayout\":{\"FloorCount\":1,\"SlotsPerFloor\":1,\"Slots\":[{\"Floor\":0,\"Index\":0,\"StructureId\":\"structure.mana_generator.basic\",\"IsUnlocked\":true,\"IsOccupied\":true}]}}}";

            SaveRoot root = JsonUtility.FromJson<SaveRoot>(legacyRootJson);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.primary, Is.Not.Null);
            Assert.That(root.primary.structureRuntime, Is.Null);

            SaveRoot migrated = SaveMigration.MigrateToLatest(root);

            Assert.That(migrated.schemaVersion, Is.EqualTo(SaveMigration.LatestSchemaVersion));
            Assert.That(migrated.primary.structureRuntime, Is.Not.Null);
        }

        [Test]
        public void SaveService_LoadOrCreate_Backfills_MissingStructureRuntime()
        {
            string fileName = $"save_test_{System.Guid.NewGuid():N}.json";
            var service = new SaveService(new SimpleLogger(false), new SaveConfig { fileName = fileName });

            try
            {
                const string legacyRootJson = "{\"schemaVersion\":1,\"primary\":{\"saveVersion\":1,\"contentVersion\":\"1.0.0\",\"totalTicks\":7,\"dungeonLayout\":{\"FloorCount\":1,\"SlotsPerFloor\":1,\"Slots\":[{\"Floor\":0,\"Index\":0,\"StructureId\":\"structure.heat_scrubber.basic\",\"IsUnlocked\":true,\"IsOccupied\":true}]}}}";
                System.IO.File.WriteAllText(service.SavePath, legacyRootJson);

                SaveData loaded = service.LoadOrCreate("1.0.0", out string banner);

                Assert.That(banner, Is.Empty);
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded.dungeonLayout, Is.Not.Null);
                Assert.That(loaded.structureRuntime, Is.Not.Null);
                Assert.That(loaded.totalTicks, Is.EqualTo(7));
                Assert.That(loaded.dungeonLayout.Slots[0].StructureId, Is.EqualTo(StructureSimulationPass.HeatScrubberBasicId));
            }
            finally
            {
                if (System.IO.File.Exists(service.SavePath))
                {
                    System.IO.File.Delete(service.SavePath);
                }
            }
        }

        [Test]
        public void EndToEnd_Placement_Then_Tick_Then_SaveLoad_Preserves_Runtime()
        {
            var layout = DungeonLayoutState.CreateEmpty(1, 3);
            var placement = new PlacementService();
            placement.PlaceStructure(layout, 0, 0, StructureSimulationPass.ManaGeneratorBasicId);
            placement.PlaceStructure(layout, 0, 1, StructureSimulationPass.HeatScrubberBasicId);
            placement.PlaceStructure(layout, 0, 2, StructureSimulationPass.RiskLabBasicId);

            var runtime = new StructureRuntimeState();
            var pass = new StructureSimulationPass(new HeatSystem(), BuildTestConfig());
            pass.SimulateTick(layout, runtime, 1);

            var save = new SaveData
            {
                totalTicks = 1,
                dungeonLayout = layout,
                structureRuntime = runtime
            };

            string json = JsonUtility.ToJson(save);
            SaveData loaded = JsonUtility.FromJson<SaveData>(json);

            Assert.That(loaded.totalTicks, Is.EqualTo(1));
            Assert.That(loaded.structureRuntime.ManaReserve, Is.EqualTo(runtime.ManaReserve));
            Assert.That(loaded.structureRuntime.Heat, Is.EqualTo(runtime.Heat));
            Assert.That(loaded.dungeonLayout.Slots[2].StructureId, Is.EqualTo(StructureSimulationPass.RiskLabBasicId));
        }

        private static StructureSimulationConfig BuildTestConfig()
        {
            return new StructureSimulationConfig
            {
                HeatCrisisEnterThreshold = 100d,
                HeatCrisisRecoveryThreshold = 65d,
                Structures = new[]
                {
                    new StructureTuningEntry { StructureId = StructureSimulationPass.ManaGeneratorBasicId, ManaDeltaPerTick = 10d, HeatDeltaPerTick = 5d },
                    new StructureTuningEntry { StructureId = StructureSimulationPass.HeatScrubberBasicId, ManaDeltaPerTick = 0d, HeatDeltaPerTick = -8d },
                    new StructureTuningEntry { StructureId = StructureSimulationPass.RiskLabBasicId, ManaDeltaPerTick = 18d, HeatDeltaPerTick = 15d }
                }
            };
        }

        private static StructureTuningEntry Find(StructureSimulationConfig config, string structureId)
        {
            for (int i = 0; i < config.Structures.Length; i++)
            {
                if (config.Structures[i].StructureId == structureId)
                {
                    return config.Structures[i];
                }
            }

            Assert.Fail($"Missing structure tuning entry for {structureId}");
            return null;
        }

        private static DungeonLayoutState BuildLayout()
        {
            var layout = DungeonLayoutState.CreateEmpty(1, 3);
            var placement = new PlacementService();
            placement.PlaceStructure(layout, 0, 0, StructureSimulationPass.ManaGeneratorBasicId);
            placement.PlaceStructure(layout, 0, 1, StructureSimulationPass.HeatScrubberBasicId);
            placement.PlaceStructure(layout, 0, 2, StructureSimulationPass.RiskLabBasicId);
            return layout;
        }

        private static StructureRuntimeState RunTicks(DungeonLayoutState layout, StructureSimulationPass pass, int tickCount)
        {
            var runtime = new StructureRuntimeState();
            for (int i = 0; i < tickCount; i++)
            {
                pass.SimulateTick(layout, runtime, i + 1);
            }

            return runtime;
        }
    }
}
