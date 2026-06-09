using System;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public sealed class RunSimulationService
    {
        private readonly RunSimulationConfig _config;
        private readonly LootConfig _lootConfig;
        private readonly string _lootTableId;
        public RunSimulationConfig Config => _config;

        public RunSimulationService(RunSimulationConfig config, LootConfig lootConfig = null, string lootTableId = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _lootConfig = lootConfig;
            _lootTableId = !string.IsNullOrEmpty(lootTableId) ? lootTableId : _config.LootTableId;
        }

        public RunOutcomeRecord SimulateOnce(StructureRuntimeState runtime, long tickStarted, int runSequence)
        {
            return SimulateOnce(runtime, tickStarted, runSequence, RunPostureResolver.BalancedId);
        }

        public RunOutcomeRecord SimulateOnce(StructureRuntimeState runtime, long tickStarted, int runSequence, string postureId)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));

            RunPostureConfig posture = RunPostureResolver.Resolve(_config, postureId);
            double heatAtStart = runtime.Heat;
            double baseChance = _config.BaseSuccessChance;
            double heatPenaltyApplied = heatAtStart * _config.HeatPenaltyPerPoint;
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

            RunLootSummary lootSummary = ApplyPostureToLootSummary(BuildLootSummary(runSequence, tickStarted), posture);
            RunSurvivalSummary survivalSummary = BuildSurvivalSummary(runSequence, tickStarted, success);
            int resolverSeed = ComputeResolverSeed(runSequence, tickStarted);
            RunLootExtractionSummary extractionSummary = ApplyPostureToExtractionSummary(LootExtractionResolver.Resolve(
                _lootConfig,
                lootSummary,
                survivalSummary,
                resolverSeed,
                _config.LootExtractionRoundingPolicyId,
                _config.LootExtractionRuleSourceId), posture);
            RunAdventurerAttractionSummary attractionSummary = AdventurerAttractionResolver.Resolve(
                _config,
                extractionSummary,
                resolverSeed);
            RunAdventurerInterestForecastSummary forecastSummary = AdventurerInterestForecastResolver.Resolve(
                _config,
                attractionSummary,
                resolverSeed);
            RunAdventurerDemandBudgetSummary demandBudgetSummary = AdventurerDemandBudgetResolver.Resolve(
                _config,
                forecastSummary,
                resolverSeed);
            RunHeatDeltaSummary heatDeltaSummary = ApplyPostureToHeatDeltaSummary(RunHeatDeltaResolver.Resolve(
                _config,
                survivalSummary,
                extractionSummary,
                resolverSeed), posture);
            RunHeatApplicationSummary heatApplicationSummary = RunHeatStateApplyResolver.Resolve(
                _config,
                runtime.Heat,
                heatDeltaSummary);
            if (heatApplicationSummary.RuleResolved)
            {
                runtime.Heat = heatApplicationSummary.HeatAfter;
            }

            return new RunOutcomeRecord
            {
                RunId = $"run-{runSequence}",
                TickStarted = tickStarted,
                Success = success,
                Score = score,
                ReasonKey = reasonKey,
                HeatAtStart = heatAtStart,
                ManaAtStart = runtime.ManaReserve,
                CrisisActiveAtStart = runtime.IsHeatCrisisActive,
                HasBreakdown = true,
                BaseChance = baseChance,
                HeatPenaltyApplied = heatPenaltyApplied,
                ManaBonusApplied = manaBonusApplied,
                CrisisPenaltyApplied = crisisPenaltyApplied,
                FinalChance = finalChance,
                SuccessThresholdUsed = successThreshold,
                FeedbackTagKeys = feedbackTagKeys,
                LootSummary = lootSummary,
                SurvivalSummary = survivalSummary,
                LootExtractionSummary = extractionSummary,
                AdventurerAttractionSummary = attractionSummary,
                AdventurerInterestForecastSummary = forecastSummary,
                AdventurerDemandBudgetSummary = demandBudgetSummary,
                RunHeatDeltaSummary = heatDeltaSummary,
                RunHeatApplicationSummary = heatApplicationSummary
            };
        }

        private RunSurvivalSummary BuildSurvivalSummary(int runSequence, long tickStarted, bool success)
        {
            int seed = ComputeResolverSeed(runSequence, tickStarted);
            int minPartySize = _config.MinPartySize;
            int maxPartySize = _config.MaxPartySize;
            int maxAllowedPartySize = _config.MaxAllowedPartySize;
            if (minPartySize < 1 || maxPartySize < minPartySize || maxAllowedPartySize < 1 || maxPartySize > maxAllowedPartySize)
            {
                return new RunSurvivalSummary
                {
                    RuleResolved = false,
                    DeterministicErrorCode = (int)RunSurvivalSummaryErrorCode.InvalidPartySizeRange,
                    DeterministicSeed = seed,
                    RuleSourceId = "run.survival.rule.v1",
                    SuccessAtResolution = success
                };
            }

            int partySizeRange = maxPartySize - minPartySize + 1;
            int partySizeOffset = Math.Abs(seed % partySizeRange);
            int partySize = minPartySize + partySizeOffset;
            double ratio = success ? _config.SuccessSurvivorRatio : _config.FailureSurvivorRatio;
            if (ratio < 0d || ratio > 1d)
            {
                return new RunSurvivalSummary
                {
                    RuleResolved = false,
                    DeterministicErrorCode = (int)RunSurvivalSummaryErrorCode.InvalidSurvivorRatio,
                    DeterministicSeed = seed,
                    RuleSourceId = "run.survival.rule.v1",
                    SuccessAtResolution = success
                };
            }

            int survivorCount = (int)Math.Round(partySize * ratio);
            survivorCount = Math.Max(0, Math.Min(partySize, survivorCount));

            return new RunSurvivalSummary
            {
                PartySize = partySize,
                SurvivorCount = survivorCount,
                DeathCount = partySize - survivorCount,
                SurvivorRatio = partySize > 0 ? (double)survivorCount / partySize : 0d,
                DeterministicSeed = seed,
                RuleResolved = true,
                DeterministicErrorCode = (int)RunSurvivalSummaryErrorCode.None,
                RuleSourceId = "run.survival.rule.v1",
                SuccessAtResolution = success
            };
        }

        private RunLootSummary BuildLootSummary(int runSequence, long tickStarted)
        {
            if (string.IsNullOrEmpty(_lootTableId))
            {
                return null;
            }

            int resolverSeed = ComputeResolverSeed(runSequence, tickStarted);
            LootRollResolverResult result = LootRollResolver.Resolve(_lootConfig, _lootTableId, resolverSeed);

            return new RunLootSummary
            {
                LootTableId = _lootTableId,
                ResolverSeed = resolverSeed,
                ResolverSuccess = result.success,
                ResolverErrorCode = (int)result.errorCode,
                RollCount = result.rollCount,
                GeneratedItemIds = result.generatedItemIds != null ? new System.Collections.Generic.List<string>(result.generatedItemIds).ToArray() : Array.Empty<string>(),
                TotalGeneratedWorldValue = result.totalGeneratedWorldValue,
                TotalGeneratedReserveCost = result.totalGeneratedReserveCost,
                TotalGeneratedTradeableWorldValue = result.totalGeneratedTradeableWorldValue
            };
        }


        private RunLootSummary ApplyPostureToLootSummary(RunLootSummary summary, RunPostureConfig posture)
        {
            if (summary == null || posture == null || !summary.ResolverSuccess)
            {
                return summary;
            }

            summary.TotalGeneratedWorldValue = ScaleToInt(summary.TotalGeneratedWorldValue, posture.GeneratedLootWorldValueMultiplier);
            return summary;
        }

        private RunLootExtractionSummary ApplyPostureToExtractionSummary(RunLootExtractionSummary summary, RunPostureConfig posture)
        {
            if (summary == null || posture == null || !summary.RuleResolved)
            {
                return summary;
            }

            summary.TotalExtractedWorldValue = ScaleToInt(summary.TotalExtractedWorldValue, posture.ExtractedLootWorldValueMultiplier);
            return summary;
        }

        private RunHeatDeltaSummary ApplyPostureToHeatDeltaSummary(RunHeatDeltaSummary summary, RunPostureConfig posture)
        {
            if (summary == null || posture == null || !summary.RuleResolved)
            {
                return summary;
            }

            double adjusted = summary.FinalHeatDelta + posture.HeatDeltaOffset;
            if (double.IsNaN(adjusted) || double.IsInfinity(adjusted))
            {
                return summary;
            }

            summary.FinalHeatDelta = Math.Max(_config.RunHeatDeltaMinimum, Math.Min(_config.RunHeatDeltaMaximum, adjusted));
            return summary;
        }

        private static int ScaleToInt(int value, double multiplier)
        {
            if (value <= 0 || multiplier <= 0d)
            {
                return 0;
            }

            double scaled = value * multiplier;
            if (double.IsNaN(scaled) || double.IsInfinity(scaled))
            {
                return value;
            }

            if (scaled >= int.MaxValue)
            {
                return int.MaxValue;
            }

            return Math.Max(0, (int)Math.Round(scaled));
        }


        private static int ComputeResolverSeed(int runSequence, long tickStarted)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + runSequence;
                int tickLow = (int)(tickStarted & 0xFFFFFFFFL);
                int tickHigh = (int)((tickStarted >> 32) & 0xFFFFFFFFL);
                hash = (hash * 31) + tickLow;
                hash = (hash * 31) + tickHigh;
                return hash;
            }
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
