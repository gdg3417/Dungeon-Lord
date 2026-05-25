using System;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public sealed class RunSimulationService
    {
        private readonly RunSimulationConfig _config;
        public RunSimulationConfig Config => _config;

        public RunSimulationService(RunSimulationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public RunOutcomeRecord SimulateOnce(StructureRuntimeState runtime, long tickStarted, int runSequence)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));

            double baseChance = _config.BaseSuccessChance;
            double heatPenaltyApplied = runtime.Heat * _config.HeatPenaltyPerPoint;
            double manaBonusApplied = runtime.ManaReserve * _config.ManaReserveBonusPerPoint;
            double crisisPenaltyApplied = runtime.IsHeatCrisisActive ? _config.CrisisFailurePenalty : 0d;

            double unclampedChance = baseChance - heatPenaltyApplied + manaBonusApplied - crisisPenaltyApplied;
            double finalChance = Math.Max(0d, Math.Min(1d, unclampedChance));
            double successThreshold = _config.SuccessThreshold;
            bool success = finalChance >= successThreshold;

            int score = success
                ? _config.BaseScoreOnSuccess + (int)Math.Round(runtime.ManaReserve * _config.ScorePerManaPoint)
                : 0;

            string reasonKey = success
                ? "run.reason.success"
                : (runtime.IsHeatCrisisActive ? "run.reason.crisis_failure" : "run.reason.failed_threshold");
            string[] feedbackTagKeys = BuildFeedbackTagKeys(runtime, success);

            return new RunOutcomeRecord
            {
                RunId = $"run-{runSequence}",
                TickStarted = tickStarted,
                Success = success,
                Score = score,
                ReasonKey = reasonKey,
                HeatAtStart = runtime.Heat,
                ManaAtStart = runtime.ManaReserve,
                CrisisActiveAtStart = runtime.IsHeatCrisisActive,
                HasBreakdown = true,
                BaseChance = baseChance,
                HeatPenaltyApplied = heatPenaltyApplied,
                ManaBonusApplied = manaBonusApplied,
                CrisisPenaltyApplied = crisisPenaltyApplied,
                FinalChance = finalChance,
                SuccessThresholdUsed = successThreshold,
                FeedbackTagKeys = feedbackTagKeys
            };
        }

        private string[] BuildFeedbackTagKeys(StructureRuntimeState runtime, bool success)
        {
            System.Collections.Generic.List<string> tags = new System.Collections.Generic.List<string>(5);
            tags.Add(success ? "run.feedback.success" : "run.feedback.failure");

            if (runtime.Heat >= _config.HighHeatFeedbackThreshold)
            {
                tags.Add("run.feedback.high_heat");
            }

            if (runtime.ManaReserve <= _config.LowManaFeedbackThreshold)
            {
                tags.Add("run.feedback.low_mana");
            }

            if (runtime.IsHeatCrisisActive)
            {
                tags.Add("run.feedback.heat_crisis");
            }

            if (runtime.ManaReserve >= _config.StrongManaReserveFeedbackThreshold)
            {
                tags.Add("run.feedback.strong_mana_reserve");
            }

            return tags.ToArray();
        }
    }
}
