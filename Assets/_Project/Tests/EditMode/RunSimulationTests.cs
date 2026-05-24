using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class RunSimulationTests
    {
        private static RunSimulationConfig BuildConfig()
        {
            return new RunSimulationConfig
            {
                BaseSuccessChance = 0.6d,
                HeatPenaltyPerPoint = 0.004d,
                ManaReserveBonusPerPoint = 0.01d,
                CrisisFailurePenalty = 0.3d,
                SuccessThreshold = 0.5d,
                BaseScoreOnSuccess = 100,
                ScorePerManaPoint = 2
            };
        }

        [Test]
        public void SimulateOnce_IsDeterministic_ForSameInput()
        {
            var service = new RunSimulationService(BuildConfig());
            var runtime = new StructureRuntimeState { Heat = 10d, ManaReserve = 20d, IsHeatCrisisActive = false };

            RunOutcomeRecord first = service.SimulateOnce(runtime, 50, 1);
            RunOutcomeRecord second = service.SimulateOnce(runtime, 50, 1);

            Assert.That(second.RunId, Is.EqualTo(first.RunId));
            Assert.That(second.Success, Is.EqualTo(first.Success));
            Assert.That(second.Score, Is.EqualTo(first.Score));
            Assert.That(second.ReasonKey, Is.EqualTo(first.ReasonKey));
        }

        [Test]
        public void SimulateOnce_HeatPenalty_CausesFailure_WhenInputIsHighHeat()
        {
            var service = new RunSimulationService(BuildConfig());
            var runtime = new StructureRuntimeState { Heat = 80d, ManaReserve = 0d, IsHeatCrisisActive = false };

            RunOutcomeRecord outcome = service.SimulateOnce(runtime, 12, 3);

            Assert.That(outcome.Success, Is.False);
            Assert.That(outcome.ReasonKey, Is.EqualTo("run.reason.failed_threshold"));
        }

        [Test]
        public void SimulateOnce_CrisisPenalty_ChangesReasonDeterministically()
        {
            var service = new RunSimulationService(BuildConfig());
            var runtime = new StructureRuntimeState { Heat = 20d, ManaReserve = 0d, IsHeatCrisisActive = true };

            RunOutcomeRecord outcome = service.SimulateOnce(runtime, 12, 4);

            Assert.That(outcome.Success, Is.False);
            Assert.That(outcome.ReasonKey, Is.EqualTo("run.reason.crisis_failure"));
            Assert.That(outcome.CrisisActiveAtStart, Is.True);
        }

        [Test]
        public void SaveData_RunOutcome_PreservesJsonRoundTrip()
        {
            var save = new SaveData
            {
                runHistory = new RunHistoryState
                {
                    NextRunSequence = 9,
                    LatestOutcome = new RunOutcomeRecord
                    {
                        RunId = "run-8",
                        TickStarted = 21,
                        Success = true,
                        Score = 140,
                        ReasonKey = "run.reason.success",
                        HeatAtStart = 10d,
                        ManaAtStart = 20d,
                        CrisisActiveAtStart = false
                    }
                }
            };

            string json = JsonUtility.ToJson(save);
            SaveData loaded = JsonUtility.FromJson<SaveData>(json);

            Assert.That(loaded.runHistory.NextRunSequence, Is.EqualTo(9));
            Assert.That(loaded.runHistory.LatestOutcome.RunId, Is.EqualTo("run-8"));
            Assert.That(loaded.runHistory.LatestOutcome.Score, Is.EqualTo(140));
            Assert.That(loaded.runHistory.LatestOutcome.ReasonKey, Is.EqualTo("run.reason.success"));
        }
        [Test]
        public void TryCreateRunSimulationService_ReturnsFalse_For_Malformed_Config()
        {
            bool ok = GameRoot.TryCreateRunSimulationService("{bad json", out RunSimulationService service);

            Assert.That(ok, Is.False);
            Assert.That(service, Is.Null);
        }

        [Test]
        public void IsValidRunSimulationConfig_Rejects_Invalid_Config()
        {
            var config = BuildConfig();
            config.SuccessThreshold = 2d;

            bool isValid = GameRoot.IsValidRunSimulationConfig(config);

            Assert.That(isValid, Is.False);
        }

    }
}
