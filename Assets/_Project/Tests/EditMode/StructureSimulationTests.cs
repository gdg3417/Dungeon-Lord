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
