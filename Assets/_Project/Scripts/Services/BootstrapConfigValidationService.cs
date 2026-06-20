using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public sealed class BootstrapConfigValidationResult
    {
        public BootstrapConfigValidationResult(bool isValid)
        {
            IsValid = isValid;
        }

        public bool IsValid { get; }
    }

    public static class BootstrapConfigValidationService
    {
        internal static bool TryCreateRunSimulationService(string configJson, string lootConfigJson, out RunSimulationService service)
        {
            service = null;

            try
            {
                RunSimulationConfig config = JsonUtility.FromJson<RunSimulationConfig>(configJson);
                if (!IsValidRunSimulationConfig(config))
                {
                    return false;
                }

                LootConfig lootConfig = TryParseLootConfig(lootConfigJson);
                service = new RunSimulationService(config, lootConfig);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static LootConfig TryParseLootConfig(string lootConfigJson)
        {
            if (string.IsNullOrWhiteSpace(lootConfigJson))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<LootConfig>(lootConfigJson);
            }
            catch
            {
                return null;
            }
        }

        public static BootstrapConfigValidationResult ValidateRunSimulationConfig(RunSimulationConfig config)
        {
            return new BootstrapConfigValidationResult(IsValidRunSimulationConfig(config));
        }

        public static BootstrapConfigValidationResult ValidateStructureSimulationConfig(StructureSimulationConfig config)
        {
            return new BootstrapConfigValidationResult(IsValidStructureSimulationConfig(config));
        }

        internal static bool IsValidRunSimulationConfig(RunSimulationConfig config)
        {
            if (config == null)
            {
                return false;
            }

            if (config.BaseSuccessChance < 0d || config.BaseSuccessChance > 1d)
            {
                return false;
            }

            if (config.SuccessThreshold < 0d || config.SuccessThreshold > 1d)
            {
                return false;
            }

            if (config.HeatPenaltyPerPoint < 0d ||
                config.ManaReserveBonusPerPoint < 0d ||
                config.CrisisFailurePenalty < 0d ||
                config.BaseScoreOnSuccess < 0 ||
                config.ScorePerManaPoint < 0)
            {
                return false;
            }

            if (config.MaxRunHistoryEntries < 1 || config.MaxRunHistoryEntries > 100)
            {
                return false;
            }

            if (config.HighHeatFeedbackThreshold < 0d ||
                config.LowManaFeedbackThreshold < 0d ||
                config.StrongManaReserveFeedbackThreshold < 0d)
            {
                return false;
            }

            if (config.LowManaFeedbackThreshold >= config.StrongManaReserveFeedbackThreshold)
            {
                return false;
            }

            if (config.MinPartySize < 1 ||
                config.MaxPartySize < config.MinPartySize ||
                config.MaxAllowedPartySize < 1 ||
                config.MaxPartySize > config.MaxAllowedPartySize)
            {
                return false;
            }

            if (config.SuccessSurvivorRatio < 0d || config.SuccessSurvivorRatio > 1d ||
                config.FailureSurvivorRatio < 0d || config.FailureSurvivorRatio > 1d)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.LootExtractionRuleSourceId) ||
                string.IsNullOrWhiteSpace(config.LootExtractionRoundingPolicyId) ||
                string.IsNullOrWhiteSpace(config.LootHeatCoolingRuleSourceId) ||
                string.IsNullOrWhiteSpace(config.AdventurerAttractionRuleSourceId) ||
                config.LootHeatCoolingPerTradeableWorldValue < 0d ||
                config.MaxLootHeatCoolingPerRun < 0d ||
                config.AdventurerAttractionPerExtractedWorldValue < 0d ||
                string.IsNullOrWhiteSpace(config.AdventurerInterestForecastRuleSourceId) ||
                config.AdventurerInterestLowThreshold < 0d ||
                config.AdventurerInterestMediumThreshold < 0d ||
                config.AdventurerInterestHighThreshold < 0d ||
                config.AdventurerInterestScorePerAttractionSignal < 0d ||
                config.AdventurerInterestLowThreshold > config.AdventurerInterestMediumThreshold ||
                config.AdventurerInterestMediumThreshold > config.AdventurerInterestHighThreshold ||
                double.IsNaN(config.LootHeatCoolingPerTradeableWorldValue) ||
                double.IsInfinity(config.LootHeatCoolingPerTradeableWorldValue) ||
                double.IsNaN(config.MaxLootHeatCoolingPerRun) ||
                double.IsInfinity(config.MaxLootHeatCoolingPerRun) ||
                double.IsNaN(config.AdventurerAttractionPerExtractedWorldValue) ||
                double.IsInfinity(config.AdventurerAttractionPerExtractedWorldValue) ||
                double.IsNaN(config.AdventurerInterestLowThreshold) ||
                double.IsInfinity(config.AdventurerInterestLowThreshold) ||
                double.IsNaN(config.AdventurerInterestMediumThreshold) ||
                double.IsInfinity(config.AdventurerInterestMediumThreshold) ||
                double.IsNaN(config.AdventurerInterestHighThreshold) ||
                double.IsInfinity(config.AdventurerInterestHighThreshold) ||
                double.IsNaN(config.AdventurerInterestScorePerAttractionSignal) ||
                double.IsInfinity(config.AdventurerInterestScorePerAttractionSignal) ||
                string.IsNullOrWhiteSpace(config.AdventurerDemandBudgetRuleSourceId) ||
                config.AdventurerDemandBudgetLowThreshold < 0d ||
                config.AdventurerDemandBudgetMediumThreshold < 0d ||
                config.AdventurerDemandBudgetHighThreshold < 0d ||
                config.AdventurerDemandBudgetScorePerForecastScore < 0d ||
                config.AdventurerDemandBudgetLowThreshold > config.AdventurerDemandBudgetMediumThreshold ||
                config.AdventurerDemandBudgetMediumThreshold > config.AdventurerDemandBudgetHighThreshold ||
                double.IsNaN(config.AdventurerDemandBudgetLowThreshold) ||
                double.IsInfinity(config.AdventurerDemandBudgetLowThreshold) ||
                double.IsNaN(config.AdventurerDemandBudgetMediumThreshold) ||
                double.IsInfinity(config.AdventurerDemandBudgetMediumThreshold) ||
                double.IsNaN(config.AdventurerDemandBudgetHighThreshold) ||
                double.IsInfinity(config.AdventurerDemandBudgetHighThreshold) ||
                double.IsNaN(config.AdventurerDemandBudgetScorePerForecastScore) ||
                double.IsInfinity(config.AdventurerDemandBudgetScorePerForecastScore) ||
                string.IsNullOrWhiteSpace(config.RunHeatDeltaRuleSourceId) ||
                config.RunHeatNormalDeathDelta < 0d ||
                config.RunHeatEliteDeathDelta < config.RunHeatNormalDeathDelta ||
                config.RunHeatMultipleDeathBonusDelta < 0d ||
                config.RunHeatSurvivorCoolingPerSurvivor < 0d ||
                config.RunHeatLootCoolingPerExtractedValue < 0d ||
                config.RunHeatDeltaMinimum > config.RunHeatDeltaMaximum ||
                double.IsNaN(config.RunHeatNormalDeathDelta) ||
                double.IsInfinity(config.RunHeatNormalDeathDelta) ||
                double.IsNaN(config.RunHeatEliteDeathDelta) ||
                double.IsInfinity(config.RunHeatEliteDeathDelta) ||
                double.IsNaN(config.RunHeatMultipleDeathBonusDelta) ||
                double.IsInfinity(config.RunHeatMultipleDeathBonusDelta) ||
                double.IsNaN(config.RunHeatSurvivorCoolingPerSurvivor) ||
                double.IsInfinity(config.RunHeatSurvivorCoolingPerSurvivor) ||
                double.IsNaN(config.RunHeatLootCoolingPerExtractedValue) ||
                double.IsInfinity(config.RunHeatLootCoolingPerExtractedValue) ||
                double.IsNaN(config.RunHeatDeltaMinimum) ||
                double.IsInfinity(config.RunHeatDeltaMinimum) ||
                double.IsNaN(config.RunHeatDeltaMaximum) ||
                double.IsInfinity(config.RunHeatDeltaMaximum) ||
                double.IsNaN(config.HeatPeaceMinimum) ||
                double.IsInfinity(config.HeatPeaceMinimum) ||
                double.IsNaN(config.HeatPeaceMaximum) ||
                double.IsInfinity(config.HeatPeaceMaximum) ||
                double.IsNaN(config.HeatNoticeMinimum) ||
                double.IsInfinity(config.HeatNoticeMinimum) ||
                double.IsNaN(config.HeatNoticeMaximum) ||
                double.IsInfinity(config.HeatNoticeMaximum) ||
                double.IsNaN(config.HeatConcernMinimum) ||
                double.IsInfinity(config.HeatConcernMinimum) ||
                double.IsNaN(config.HeatConcernMaximum) ||
                double.IsInfinity(config.HeatConcernMaximum) ||
                string.IsNullOrWhiteSpace(config.RunHeatApplicationRuleSourceId) ||
                config.HeatPeaceMinimum > config.HeatPeaceMaximum ||
                config.HeatPeaceMaximum >= config.HeatNoticeMinimum ||
                config.HeatNoticeMinimum > config.HeatNoticeMaximum ||
                config.HeatNoticeMaximum >= config.HeatConcernMinimum ||
                config.HeatConcernMinimum > config.HeatConcernMaximum ||
                string.IsNullOrWhiteSpace(config.AdventurerPartyCompositionRuleSourceId) ||
                config.AdventurerPartyCompositionMinSize < 1 ||
                config.AdventurerPartyCompositionMaxSize < config.AdventurerPartyCompositionMinSize ||
                config.AdventurerPartyCompositionMaxAllowedSize < 1 ||
                config.AdventurerPartyCompositionMaxSize > config.AdventurerPartyCompositionMaxAllowedSize ||
                !HasValidCasualtyPressureConfig(config) ||
                !HasValidAdventurerIntentConfig(config) ||
                !HasValidAdventurerArrivalPressureConfig(config) ||
                !HasValidAdventurerTrafficPressureConfig(config) ||
                !HasConfiguredMvpAdventurerClasses(config.AdventurerPartyCompositionClassIds))
            {
                return false;
            }

            if (!HasValidMvpPlacementEffectsConfig(config) || !HasValidMvpCompositionOutcomeTuningConfig(config.MvpCompositionOutcomeTuning))
            {
                return false;
            }

            return true;
        }


        private static bool HasValidAdventurerIntentConfig(RunSimulationConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.AdventurerIntentRuleSourceId))
            {
                return false;
            }

            return IsFinite(config.IntentGreedyScorePerLoot) &&
                   IsFinite(config.IntentGreedyScorePerAttraction) &&
                   IsFinite(config.IntentGreedyPenaltyPerHeatTierRank) &&
                   IsFinite(config.IntentGreedyPenaltyPerRecentDeath) &&
                   IsFinite(config.IntentGreedyPenaltyPerDanger) &&
                   IsFinite(config.IntentCautiousScorePerDanger) &&
                   IsFinite(config.IntentCautiousScorePerHeatPressure) &&
                   IsFinite(config.IntentCautiousScorePerHeatTierRank) &&
                   IsFinite(config.IntentCautiousScorePerRecentDeath) &&
                   IsFinite(config.IntentCautiousReductionPerPathCapacity) &&
                   IsFinite(config.IntentBalancedBaseScore) &&
                   IsFinite(config.IntentBalancedPenaltyPerExtremeScoreDelta) &&
                   IsFinite(config.IntentModerateRiskTarget) &&
                   IsFinite(config.IntentModerateRewardTarget) &&
                   IsFinite(config.IntentBalancedPenaltyPerModerateDistance) &&
                   IsFinite(config.IntentMinimumScore) &&
                   IsFinite(config.IntentMaximumScore) &&
                   config.IntentMinimumScore <= config.IntentMaximumScore;
        }

        private static bool HasValidAdventurerArrivalPressureConfig(RunSimulationConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.AdventurerArrivalPressureRuleSourceId))
            {
                return false;
            }

            return IsFinite(config.ArrivalPressureScorePerLoot) &&
                   IsFinite(config.ArrivalPressureScorePerAttraction) &&
                   IsFinite(config.ArrivalPressureScorePerDanger) &&
                   IsFinite(config.ArrivalPressureScorePerHeatPressure) &&
                   IsFinite(config.ArrivalPressureScorePerRecentRecoveredLoot) &&
                   IsFinite(config.ArrivalPressureLatestSuccessBonus) &&
                   IsFinite(config.ArrivalPressureLatestFailurePenalty) &&
                   IsFinite(config.ArrivalPressurePathCompleteBonus) &&
                   IsFinite(config.ArrivalPressureIncompletePathPenalty) &&
                   IsFinite(config.ArrivalPressureHeatNoticePenalty) &&
                   IsFinite(config.ArrivalPressureHeatConcernPenalty) &&
                   IsFinite(config.ArrivalPressureRecentDeathPenalty) &&
                   IsFinite(config.ArrivalPressureNoneThreshold) &&
                   IsFinite(config.ArrivalPressureLowThreshold) &&
                   IsFinite(config.ArrivalPressureCautiousThreshold) &&
                   IsFinite(config.ArrivalPressureBuildingThreshold) &&
                   IsFinite(config.ArrivalPressureLikelySoonThreshold) &&
                   config.ArrivalPressureNoneThreshold <= config.ArrivalPressureLowThreshold &&
                   config.ArrivalPressureLowThreshold <= config.ArrivalPressureCautiousThreshold &&
                   config.ArrivalPressureCautiousThreshold <= config.ArrivalPressureBuildingThreshold &&
                   config.ArrivalPressureBuildingThreshold <= config.ArrivalPressureLikelySoonThreshold;
        }

        private static bool HasValidAdventurerTrafficPressureConfig(RunSimulationConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.AdventurerTrafficPressureRuleSourceId))
            {
                return false;
            }

            return IsFinite(config.TrafficScoreWeightArrivalPressure) &&
                   IsFinite(config.TrafficScoreWeightLootSignal) &&
                   IsFinite(config.TrafficScoreWeightAttractionSignal) &&
                   IsFinite(config.TrafficScoreWeightDangerSignal) &&
                   IsFinite(config.TrafficScoreWeightHeatPressureSignal) &&
                   IsFinite(config.TrafficScoreWeightRecentRecoveredLoot) &&
                   IsFinite(config.TrafficPathCompleteBonus) &&
                   IsFinite(config.TrafficIncompletePathPenalty) &&
                   IsFinite(config.TrafficRecentDeathCautionModifier) &&
                   IsFinite(config.TrafficHeatCautionModifier) &&
                   IsFinite(config.TrafficNoneThreshold) &&
                   IsFinite(config.TrafficLowThreshold) &&
                   IsFinite(config.TrafficBuildingThreshold) &&
                   IsFinite(config.TrafficSteadyThreshold) &&
                   IsFinite(config.TrafficHeavyThreshold) &&
                   IsFinite(config.TrafficDangerousChurnMinimumInterestScore) &&
                   IsFinite(config.TrafficEstimatedPartyCountMultiplier) &&
                   IsFinite(config.TrafficEstimatedPartyCountScoreDivisor) &&
                   config.TrafficNoneThreshold <= config.TrafficLowThreshold &&
                   config.TrafficLowThreshold <= config.TrafficBuildingThreshold &&
                   config.TrafficBuildingThreshold <= config.TrafficSteadyThreshold &&
                   config.TrafficSteadyThreshold <= config.TrafficHeavyThreshold &&
                   config.TrafficDangerousChurnRecentDeathThreshold >= 1 &&
                   config.TrafficDangerousChurnMinimumInterestScore >= config.TrafficLowThreshold &&
                   config.TrafficEstimatedPartyCountScoreDivisor > 0d &&
                   config.TrafficEstimatedPartyCountMultiplier >= 0d &&
                   config.TrafficEstimatedPartyCountLowThreshold >= 1 &&
                   config.TrafficEstimatedPartyCountMediumThreshold >= config.TrafficEstimatedPartyCountLowThreshold &&
                   config.TrafficMinimumEstimatedConcurrentParties >= 0 &&
                   config.TrafficMaximumEstimatedConcurrentParties >= config.TrafficMinimumEstimatedConcurrentParties &&
                   config.TrafficMaximumEstimatedConcurrentParties > 0;
        }

        private static bool HasValidCasualtyPressureConfig(RunSimulationConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.CasualtyPressureRuleSourceId))
            {
                return false;
            }

            return IsFiniteNonNegative(config.CasualtyPressurePerDanger) &&
                   IsFiniteNonNegative(config.CasualtyPressureReductionPerPathCapacity) &&
                   IsFiniteNonNegative(config.CasualtyPressurePerManaPressure) &&
                   IsFiniteNonNegative(config.CautiousCasualtyPressureMultiplier) &&
                   IsFiniteNonNegative(config.BalancedCasualtyPressureMultiplier) &&
                   IsFiniteNonNegative(config.GreedyCasualtyPressureMultiplier) &&
                   IsFiniteNonNegative(config.CasualtyPressureMinimum) &&
                   IsFiniteNonNegative(config.CasualtyPressureMaximum) &&
                   IsFiniteNonNegative(config.CasualtyLootExtractionPenaltyPerCasualty) &&
                   IsFiniteNonNegative(config.CasualtyHeatDeltaPerCasualty) &&
                   IsFiniteNonNegative(config.PartyWipeCasualtyPressureThreshold) &&
                   config.CasualtyPressureMinimum <= config.CasualtyPressureMaximum &&
                   config.CasualtyPressureMaximum <= 1d &&
                   config.PartyWipeCasualtyPressureThreshold <= 1d;
        }

        private static bool HasValidMvpCompositionOutcomeTuningConfig(MvpCompositionOutcomeTuningConfig tuning)
        {
            if (tuning == null)
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(tuning.RuleSourceId) &&
                   IsFiniteNonNegative(tuning.SuccessChancePerPathCapacity) &&
                   IsFiniteNonNegative(tuning.SuccessChancePenaltyPerDanger) &&
                   IsFiniteNonNegative(tuning.ManaReserveCostPerManaPressure) &&
                   IsFiniteNonNegative(tuning.SurvivorRatioBonusPerPathCapacity) &&
                   IsFiniteNonNegative(tuning.SurvivorRatioPenaltyPerDanger) &&
                   IsFiniteNonNegative(tuning.GeneratedLootMultiplierPerLootBonus) &&
                   IsFiniteNonNegative(tuning.ExtractedLootMultiplierPerLootBonus) &&
                   IsFiniteNonNegative(tuning.HeatDeltaPerHeatPressure) &&
                   IsFiniteNonNegative(tuning.AttractionSignalPerAttraction);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return value >= 0d && IsFinite(value);
        }

        private static bool HasValidMvpPlacementEffectsConfig(RunSimulationConfig config)
        {
            if (config == null || config.MvpPlacementEffects == null || config.MvpPlacementEffects.Length == 0)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(config.MvpPlacementEffectsRuleSourceId))
            {
                return false;
            }

            for (int i = 0; i < config.MvpPlacementEffects.Length; i++)
            {
                MvpPlacementEffectConfig effect = config.MvpPlacementEffects[i];
                if (effect == null ||
                    !MvpDungeonPlacementIds.IsAllowedCategory(effect.CategoryId) ||
                    !MvpDungeonPlacementIds.IsAllowedOption(effect.OptionId) ||
                    !MvpDungeonPlacementIds.TryGetCategoryForOption(effect.OptionId, out string optionCategoryId) ||
                    !string.Equals(optionCategoryId, effect.CategoryId, StringComparison.Ordinal) ||
                    effect.PathCapacity < 0 ||
                    effect.Danger < 0 ||
                    effect.ManaPressure < 0 ||
                    effect.LootBonus < 0 ||
                    effect.Attraction < 0 ||
                    string.IsNullOrWhiteSpace(effect.ExplanationKey))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasConfiguredMvpAdventurerClasses(string[] classIds)
        {
            if (classIds == null || classIds.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < classIds.Length; i++)
            {
                if (AdventurerPartyCompositionResolver.IsMvpClassId(classIds[i]))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool TryCreateStructureSimulationPass(
            IHeatSystem heatSystem,
            string configJson,
            out StructureSimulationPass pass)
        {
            pass = null;

            try
            {
                StructureSimulationConfig config = JsonUtility.FromJson<StructureSimulationConfig>(configJson);
                if (!IsValidStructureSimulationConfig(config))
                {
                    return false;
                }

                pass = new StructureSimulationPass(heatSystem, config);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsValidStructureSimulationConfig(StructureSimulationConfig config)
        {
            if (config == null || config.Structures == null || config.Structures.Length == 0)
            {
                return false;
            }

            if (config.HeatCrisisEnterThreshold <= config.HeatCrisisRecoveryThreshold)
            {
                return false;
            }
            if (config.CrisisEnterConsecutiveTicks < 1 ||
                config.CrisisRecoveryConsecutiveTicks < 1 ||
                config.CrisisManaDrainPerTick < 0d)
            {
                return false;
            }

            var required = new HashSet<string>(StringComparer.Ordinal)
            {
                StructureSimulationPass.ManaGeneratorBasicId,
                StructureSimulationPass.HeatScrubberBasicId,
                StructureSimulationPass.RiskLabBasicId
            };

            for (int i = 0; i < config.Structures.Length; i++)
            {
                StructureTuningEntry entry = config.Structures[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.StructureId))
                {
                    return false;
                }

                required.Remove(entry.StructureId);
            }

            return required.Count == 0;
        }

    }
}
