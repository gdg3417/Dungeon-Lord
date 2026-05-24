using System;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public sealed class RunSimulationService
    {
        private readonly RunSimulationConfig _config;

        public RunSimulationService(RunSimulationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public RunOutcomeRecord SimulateOnce(StructureRuntimeState runtime, long tickStarted, int runSequence)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));

            double chance = _config.BaseSuccessChance;
            chance -= runtime.Heat * _config.HeatPenaltyPerPoint;
            chance += runtime.ManaReserve * _config.ManaReserveBonusPerPoint;
            if (runtime.IsHeatCrisisActive)
            {
                chance -= _config.CrisisFailurePenalty;
            }

            chance = Math.Max(0d, Math.Min(1d, chance));
            bool success = chance >= _config.SuccessThreshold;

            int score = success
                ? _config.BaseScoreOnSuccess + (int)Math.Round(runtime.ManaReserve * _config.ScorePerManaPoint)
                : 0;

            string reasonKey = success
                ? "run.reason.success"
                : (runtime.IsHeatCrisisActive ? "run.reason.crisis_failure" : "run.reason.failed_threshold");

            return new RunOutcomeRecord
            {
                RunId = $"run-{runSequence}",
                TickStarted = tickStarted,
                Success = success,
                Score = score,
                ReasonKey = reasonKey,
                HeatAtStart = runtime.Heat,
                ManaAtStart = runtime.ManaReserve,
                CrisisActiveAtStart = runtime.IsHeatCrisisActive
            };
        }
    }
}
