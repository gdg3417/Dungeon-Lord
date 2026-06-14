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
            return SimulateOnce(runtime, tickStarted, runSequence, RunPostureResolver.BalancedId, null);
        }

        public RunOutcomeRecord SimulateOnce(StructureRuntimeState runtime, long tickStarted, int runSequence, string postureId)
        {
            return SimulateOnce(runtime, tickStarted, runSequence, postureId, null);
        }

        public RunOutcomeRecord SimulateOnce(StructureRuntimeState runtime, long tickStarted, int runSequence, string postureId, MvpPlacementEffectsSummary placementEffects)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));

            RunPostureConfig posture = RunPostureResolver.Resolve(_config, postureId);
            RunCompositionOutcomeSummary compositionOutcome = BuildCompositionOutcomeSummary(placementEffects, runtime.ManaReserve);
            double heatAtStart = runtime.Heat;
            double baseChance = _config.BaseSuccessChance;
            double heatPenaltyApplied = heatAtStart * _config.HeatPenaltyPerPoint;
            double manaBonusApplied = compositionOutcome.EffectiveManaReserve * _config.ManaReserveBonusPerPoint;
            double crisisPenaltyApplied = runtime.IsHeatCrisisActive ? _config.CrisisFailurePenalty : 0d;

            double unclampedChance = baseChance - heatPenaltyApplied + manaBonusApplied - crisisPenaltyApplied + compositionOutcome.SuccessChanceDelta;
            double finalChance = Math.Max(0d, Math.Min(1d, unclampedChance));
            double successThreshold = _config.SuccessThreshold;
            bool success = finalChance >= successThreshold;

            int score = success
                ? _config.BaseScoreOnSuccess + (int)Math.Round(compositionOutcome.EffectiveManaReserve * _config.ScorePerManaPoint)
                : 0;

            string reasonKey = success
                ? "run.reason.success"
                : (runtime.IsHeatCrisisActive ? "run.reason.crisis_failure" : "run.reason.failed_threshold");
            string[] feedbackTagKeys = BuildFeedbackTagKeys(runtime, success, compositionOutcome);

            RunLootSummary lootSummary = ApplyCompositionToLootSummary(
                ApplyPostureToLootSummary(BuildLootSummary(runSequence, tickStarted), posture),
                compositionOutcome);
            RunSurvivalSummary survivalSummary = ApplyCasualtyPressureToSurvivalSummary(
                ApplyCompositionToSurvivalSummary(BuildSurvivalSummary(runSequence, tickStarted, success), compositionOutcome),
                compositionOutcome,
                posture);
            int resolverSeed = ComputeResolverSeed(runSequence, tickStarted);
            RunLootExtractionSummary extractionSummary = ApplyCompositionToExtractionSummary(
                ApplyPostureToExtractionSummary(LootExtractionResolver.Resolve(
                    _lootConfig,
                    lootSummary,
                    survivalSummary,
                    resolverSeed,
                    _config.LootExtractionRoundingPolicyId,
                    _config.LootExtractionRuleSourceId), lootSummary, posture),
                lootSummary,
                compositionOutcome);
            ApplyCasualtyPressureToExtractionSummary(extractionSummary, lootSummary, survivalSummary);
            RunLootDropRecord[] lootBreakdown = RunLootBreakdownResolver.Resolve(_lootConfig, extractionSummary);
            RunAdventurerAttractionSummary attractionSummary = ApplyCompositionToAttractionSummary(AdventurerAttractionResolver.Resolve(
                _config,
                extractionSummary,
                resolverSeed), compositionOutcome);
            RunAdventurerInterestForecastSummary forecastSummary = AdventurerInterestForecastResolver.Resolve(
                _config,
                attractionSummary,
                resolverSeed);
            RunAdventurerDemandBudgetSummary demandBudgetSummary = AdventurerDemandBudgetResolver.Resolve(
                _config,
                forecastSummary,
                resolverSeed);
            RunHeatDeltaSummary heatDeltaSummary = ApplyCompositionToHeatDeltaSummary(
                ApplyPostureToHeatDeltaSummary(RunHeatDeltaResolver.Resolve(
                    _config,
                    survivalSummary,
                    extractionSummary,
                    resolverSeed), posture),
                compositionOutcome);
            ApplyCasualtyPressureToHeatDeltaSummary(heatDeltaSummary, survivalSummary);
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
                LootBreakdown = lootBreakdown,
                AdventurerAttractionSummary = attractionSummary,
                AdventurerInterestForecastSummary = forecastSummary,
                AdventurerDemandBudgetSummary = demandBudgetSummary,
                RunHeatDeltaSummary = heatDeltaSummary,
                RunHeatApplicationSummary = heatApplicationSummary,
                CompositionOutcomeSummary = compositionOutcome
            };
        }

        private RunCompositionOutcomeSummary BuildCompositionOutcomeSummary(MvpPlacementEffectsSummary effects, double manaReserve)
        {
            MvpCompositionOutcomeTuningConfig tuning = _config.MvpCompositionOutcomeTuning;
            var summary = new RunCompositionOutcomeSummary
            {
                RuleResolved = tuning != null,
                RuleSourceId = tuning?.RuleSourceId ?? string.Empty,
                PlacementEffects = ClonePlacementEffects(effects),
                EffectiveManaReserve = manaReserve,
                GeneratedLootMultiplier = 1d,
                ExtractedLootMultiplier = 1d
            };

            if (tuning == null || effects == null || !effects.RuleResolved)
            {
                return summary;
            }

            summary.SuccessChanceDelta =
                (effects.PathCapacity * tuning.SuccessChancePerPathCapacity) -
                (effects.Danger * tuning.SuccessChancePenaltyPerDanger);
            summary.ManaReservePressureCost = Math.Max(0d, effects.ManaPressure * tuning.ManaReserveCostPerManaPressure);
            summary.EffectiveManaReserve = Math.Max(0d, manaReserve - summary.ManaReservePressureCost);
            summary.SurvivorRatioDelta =
                (effects.PathCapacity * tuning.SurvivorRatioBonusPerPathCapacity) -
                (effects.Danger * tuning.SurvivorRatioPenaltyPerDanger);
            summary.GeneratedLootMultiplier = Math.Max(0d, 1d + (effects.LootBonus * tuning.GeneratedLootMultiplierPerLootBonus));
            summary.ExtractedLootMultiplier = Math.Max(0d, 1d + (effects.LootBonus * tuning.ExtractedLootMultiplierPerLootBonus));
            summary.HeatDeltaOffset = effects.HeatPressure * tuning.HeatDeltaPerHeatPressure;
            summary.AttractionSignalBonus = Math.Max(0d, effects.Attraction * tuning.AttractionSignalPerAttraction);
            return summary;
        }

        private static MvpPlacementEffectsSummary ClonePlacementEffects(MvpPlacementEffectsSummary effects)
        {
            if (effects == null)
            {
                return new MvpPlacementEffectsSummary { RuleResolved = true, ContributingOptionIds = Array.Empty<string>(), EffectLocalizationKeys = Array.Empty<string>() };
            }

            return new MvpPlacementEffectsSummary
            {
                RuleResolved = effects.RuleResolved,
                RuleSourceId = effects.RuleSourceId,
                PathCapacity = effects.PathCapacity,
                Danger = effects.Danger,
                ManaPressure = effects.ManaPressure,
                HeatPressure = effects.HeatPressure,
                LootBonus = effects.LootBonus,
                Attraction = effects.Attraction,
                ContributingOptionIds = effects.ContributingOptionIds != null ? (string[])effects.ContributingOptionIds.Clone() : Array.Empty<string>(),
                EffectLocalizationKeys = effects.EffectLocalizationKeys != null ? (string[])effects.EffectLocalizationKeys.Clone() : Array.Empty<string>()
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

        private RunLootSummary ApplyCompositionToLootSummary(RunLootSummary summary, RunCompositionOutcomeSummary composition)
        {
            if (summary == null || composition == null || !summary.ResolverSuccess)
            {
                return summary;
            }

            summary.TotalGeneratedWorldValue = ScaleToInt(summary.TotalGeneratedWorldValue, composition.GeneratedLootMultiplier);
            summary.TotalGeneratedTradeableWorldValue = Math.Min(
                summary.TotalGeneratedWorldValue,
                ScaleToInt(summary.TotalGeneratedTradeableWorldValue, composition.GeneratedLootMultiplier));
            return summary;
        }

        private RunSurvivalSummary ApplyCompositionToSurvivalSummary(RunSurvivalSummary summary, RunCompositionOutcomeSummary composition)
        {
            if (summary == null || composition == null || !summary.RuleResolved || summary.DeterministicErrorCode != (int)RunSurvivalSummaryErrorCode.None)
            {
                return summary;
            }

            double ratio = Math.Max(0d, Math.Min(1d, summary.SurvivorRatio + composition.SurvivorRatioDelta));
            int survivorCount = Math.Max(0, Math.Min(summary.PartySize, (int)Math.Round(summary.PartySize * ratio)));
            summary.SurvivorCount = survivorCount;
            summary.DeathCount = summary.PartySize - survivorCount;
            summary.SurvivorRatio = summary.PartySize > 0 ? (double)survivorCount / summary.PartySize : 0d;
            return summary;
        }

        private RunSurvivalSummary ApplyCasualtyPressureToSurvivalSummary(RunSurvivalSummary summary, RunCompositionOutcomeSummary composition, RunPostureConfig posture)
        {
            if (summary == null || composition == null || !summary.RuleResolved || summary.DeterministicErrorCode != (int)RunSurvivalSummaryErrorCode.None || string.IsNullOrWhiteSpace(_config.CasualtyPressureRuleSourceId))
            {
                return summary;
            }

            MvpPlacementEffectsSummary effects = composition.PlacementEffects;
            double rawPressure =
                ((effects?.Danger ?? 0) * _config.CasualtyPressurePerDanger) -
                ((effects?.PathCapacity ?? 0) * _config.CasualtyPressureReductionPerPathCapacity) +
                ((effects?.ManaPressure ?? 0) * _config.CasualtyPressurePerManaPressure);
            double multiplier = ResolveCasualtyPressureMultiplier(posture);
            double pressure = Math.Max(_config.CasualtyPressureMinimum, Math.Min(_config.CasualtyPressureMaximum, rawPressure * multiplier));
            if (double.IsNaN(pressure) || double.IsInfinity(pressure))
            {
                return summary;
            }

            int originalDeathCount = summary.DeathCount;
            int pressureDeaths = Math.Max(0, Math.Min(summary.PartySize, (int)Math.Round(summary.PartySize * pressure)));
            if (pressure < _config.PartyWipeCasualtyPressureThreshold && pressureDeaths >= summary.PartySize && summary.PartySize > 0)
            {
                pressureDeaths = summary.PartySize - 1;
            }

            int deathCount = Math.Max(summary.DeathCount, pressureDeaths);
            summary.DeathCount = Math.Max(0, Math.Min(summary.PartySize, deathCount));
            summary.SurvivorCount = Math.Max(0, summary.PartySize - summary.DeathCount);
            summary.SurvivorRatio = summary.PartySize > 0 ? (double)summary.SurvivorCount / summary.PartySize : 0d;
            summary.CasualtyPressure = pressure;
            int pressureCasualties = Math.Max(0, summary.DeathCount - originalDeathCount);
            summary.CasualtyLootExtractionPenalty = Math.Max(0d, pressureCasualties * _config.CasualtyLootExtractionPenaltyPerCasualty);
            summary.CasualtyHeatDelta = Math.Max(0d, pressureCasualties * _config.CasualtyHeatDeltaPerCasualty);
            summary.RuleSourceId = _config.CasualtyPressureRuleSourceId;
            return summary;
        }

        private double ResolveCasualtyPressureMultiplier(RunPostureConfig posture)
        {
            string postureId = posture?.Id;
            if (string.Equals(postureId, RunPostureResolver.CautiousId, StringComparison.Ordinal)) return _config.CautiousCasualtyPressureMultiplier;
            if (string.Equals(postureId, RunPostureResolver.GreedyId, StringComparison.Ordinal)) return _config.GreedyCasualtyPressureMultiplier;
            return _config.BalancedCasualtyPressureMultiplier;
        }

        private RunLootExtractionSummary ApplyPostureToExtractionSummary(RunLootExtractionSummary summary, RunLootSummary lootSummary, RunPostureConfig posture)
        {
            if (summary == null || posture == null || !summary.RuleResolved)
            {
                return summary;
            }

            int generatedWorldValue = Math.Max(0, lootSummary?.TotalGeneratedWorldValue ?? 0);
            summary.TotalExtractedWorldValue = Math.Min(
                generatedWorldValue,
                ScaleToInt(summary.TotalExtractedWorldValue, posture.ExtractedLootWorldValueMultiplier));
            summary.TotalExtractedTradeableWorldValue = Math.Min(
                summary.TotalExtractedWorldValue,
                ScaleToInt(summary.TotalExtractedTradeableWorldValue, posture.ExtractedLootWorldValueMultiplier));
            return summary;
        }

        private RunLootExtractionSummary ApplyCompositionToExtractionSummary(RunLootExtractionSummary summary, RunLootSummary lootSummary, RunCompositionOutcomeSummary composition)
        {
            if (summary == null || composition == null || !summary.RuleResolved)
            {
                return summary;
            }

            int generatedWorldValue = Math.Max(0, lootSummary?.TotalGeneratedWorldValue ?? 0);
            summary.TotalExtractedWorldValue = Math.Min(
                generatedWorldValue,
                ScaleToInt(summary.TotalExtractedWorldValue, composition.ExtractedLootMultiplier));
            summary.TotalExtractedTradeableWorldValue = Math.Min(
                summary.TotalExtractedWorldValue,
                ScaleToInt(summary.TotalExtractedTradeableWorldValue, composition.ExtractedLootMultiplier));
            return summary;
        }

        private void ApplyCasualtyPressureToExtractionSummary(RunLootExtractionSummary summary, RunLootSummary lootSummary, RunSurvivalSummary survival)
        {
            if (summary == null || lootSummary == null || survival == null || !summary.RuleResolved || survival.DeathCount <= 0 || survival.CasualtyLootExtractionPenalty <= 0d)
            {
                return;
            }

            double multiplier = Math.Max(0d, 1d - survival.CasualtyLootExtractionPenalty);
            summary.TotalExtractedWorldValue = Math.Min(lootSummary.TotalGeneratedWorldValue, ScaleToInt(summary.TotalExtractedWorldValue, multiplier));
            summary.TotalExtractedTradeableWorldValue = Math.Min(summary.TotalExtractedWorldValue, ScaleToInt(summary.TotalExtractedTradeableWorldValue, multiplier));
        }

        private RunAdventurerAttractionSummary ApplyCompositionToAttractionSummary(RunAdventurerAttractionSummary summary, RunCompositionOutcomeSummary composition)
        {
            if (summary == null || composition == null || !summary.RuleResolved)
            {
                return summary;
            }

            summary.AttractionSignalValue += composition.AttractionSignalBonus;
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

        private RunHeatDeltaSummary ApplyCompositionToHeatDeltaSummary(RunHeatDeltaSummary summary, RunCompositionOutcomeSummary composition)
        {
            if (summary == null || composition == null || !summary.RuleResolved)
            {
                return summary;
            }

            double adjusted = summary.FinalHeatDelta + composition.HeatDeltaOffset;
            if (double.IsNaN(adjusted) || double.IsInfinity(adjusted))
            {
                return summary;
            }

            summary.FinalHeatDelta = Math.Max(_config.RunHeatDeltaMinimum, Math.Min(_config.RunHeatDeltaMaximum, adjusted));
            return summary;
        }

        private void ApplyCasualtyPressureToHeatDeltaSummary(RunHeatDeltaSummary summary, RunSurvivalSummary survival)
        {
            if (summary == null || survival == null || !summary.RuleResolved || survival.CasualtyHeatDelta <= 0d)
            {
                return;
            }

            double adjusted = summary.FinalHeatDelta + survival.CasualtyHeatDelta;
            if (double.IsNaN(adjusted) || double.IsInfinity(adjusted))
            {
                return;
            }

            summary.DeathHeatDelta += survival.CasualtyHeatDelta;
            summary.FinalHeatDelta = Math.Max(_config.RunHeatDeltaMinimum, Math.Min(_config.RunHeatDeltaMaximum, adjusted));
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

        private string[] BuildFeedbackTagKeys(StructureRuntimeState runtime, bool success, RunCompositionOutcomeSummary composition)
        {
            System.Collections.Generic.List<string> tags = new System.Collections.Generic.List<string>(6);
            tags.Add(success ? "run.feedback.success" : "run.feedback.failure");

            if (runtime.Heat >= _config.HighHeatFeedbackThreshold)
            {
                tags.Add("run.feedback.high_heat");
            }

            double effectiveManaReserve = composition != null ? composition.EffectiveManaReserve : runtime.ManaReserve;
            if (effectiveManaReserve <= _config.LowManaFeedbackThreshold)
            {
                tags.Add("run.feedback.low_mana");
            }

            if (runtime.IsHeatCrisisActive)
            {
                tags.Add("run.feedback.heat_crisis");
            }

            if (effectiveManaReserve >= _config.StrongManaReserveFeedbackThreshold)
            {
                tags.Add("run.feedback.strong_mana_reserve");
            }

            return tags.ToArray();
        }
    }
}
