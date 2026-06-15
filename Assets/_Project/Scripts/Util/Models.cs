using System;
using DungeonBuilder.M0.Gameplay.Structures;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public enum RunSurvivalSummaryErrorCode
    {
        None = 0,
        InvalidPartySizeRange = 1,
        InvalidSurvivorRatio = 2
    }
    public enum RunLootExtractionSummaryErrorCode
    {
        None = 0,
        LootSummaryMissingOrFailed = 1,
        SurvivalSummaryMissingOrFailed = 2,
        InvalidSurvivorRatio = 3,
        UnknownRoundingPolicy = 4,
        ItemValueLookupFailed = 5,
        AggregateOverflow = 6
    }

    public enum RunLootHeatCoolingSummaryErrorCode
    {
        None = 0,
        ExtractionSummaryMissingOrFailed = 1,
        InvalidCoolingConfig = 2,
        AggregateOverflow = 3
    }
    public enum RunAdventurerAttractionSummaryErrorCode
    {
        None = 0,
        ExtractionSummaryMissingOrFailed = 1,
        InvalidAttractionConfig = 2,
        AggregateOverflow = 3
    }
    public enum RunAdventurerInterestForecastSummaryErrorCode
    {
        None = 0,
        AttractionSummaryMissingOrFailed = 1,
        InvalidForecastConfig = 2,
        AggregateOverflow = 3
    }

    public enum RunAdventurerDemandBudgetSummaryErrorCode
    {
        None = 0,
        InterestForecastMissingOrFailed = 1,
        InvalidDemandBudgetConfig = 2,
        AggregateOverflow = 3
    }

    public enum AdventurerPartyCompositionSummaryErrorCode
    {
        None = 0,
        MissingOrInvalidConfig = 1,
        NoAllowedMvpClasses = 2
    }

    public enum AdventurerRunIntentSummaryErrorCode
    {
        None = 0,
        MissingOrInvalidConfig = 1,
        InvalidHeatTier = 2,
        AggregateOverflow = 3
    }

    public enum AdventurerArrivalPressureSummaryErrorCode
    {
        None = 0,
        MissingOrInvalidConfig = 1,
        InvalidHeatTier = 2,
        AggregateOverflow = 3
    }

    public enum AdventurerTrafficPressureSummaryErrorCode
    {
        None = 0,
        MissingOrInvalidConfig = 1,
        MissingArrivalPressure = 2,
        MissingRunIntent = 3,
        AggregateOverflow = 4
    }
    public enum RunHeatDeltaSummaryErrorCode
    {
        None = 0,
        InvalidHeatDeltaConfig = 1,
        SurvivalSummaryMissingOrFailed = 2,
        ExtractionSummaryMissingOrFailed = 3,
        AggregateOverflow = 4
    }
    public enum RunHeatApplicationSummaryErrorCode
    {
        None = 0,
        HeatDeltaSummaryMissing = 1,
        HeatDeltaSummaryUnresolved = 2,
        InvalidHeatDeltaSummary = 3,
        InvalidHeatApplicationConfig = 4,
        InvalidCurrentHeat = 5,
        AggregateOverflow = 6,
        LegacyDefaultUnresolved = 7
    }
    public enum CurrentHeatTierSummaryErrorCode
    {
        None = 0,
        InvalidHeatTierConfig = 1,
        InvalidCurrentHeat = 2,
        CurrentHeatOutOfRange = 3
    }

    public enum MvpPlayerLoopSummaryErrorCode
    {
        None = 0,
        MissingSave = 1
    }
    public enum ResearchUnlockSummaryErrorCode
    {
        None = 0,
        NoCompletedResearch = 1,
        MissingOrInvalidConfig = 2,
        NoMatchingUnlock = 3
    }
    public enum GuidedMvpActionPathErrorCode
    {
        None = 0,
        MissingSave = 1
    }
    public enum OfflineSummaryErrorCode
    {
        None = 0,
        SaveMissing = 1,
        TimeRulesMissingOrInvalid = 2,
        CurrentTimestampInvalid = 3,
        LastKnownTimestampInvalid = 4,
        CurrentTimestampBeforeLastKnownTimestamp = 5
    }
    public enum ResearchPendingValidationErrorCode
    {
        None = 0,
        ScaffoldConfigMissingOrDisabled = 1,
        ScaffoldSlotIdMissing = 2,
        ScaffoldProjectIdMissing = 3
    }
    public enum ResearchProgressSummaryErrorCode
    {
        None = 0,
        NoPendingResearch = 1,
        MissingConfig = 2,
        DisabledConfig = 3,
        InvalidElapsedTime = 4,
        InvalidPendingState = 5,
        InvalidConfig = 6
    }
    public enum ResearchProgressApplySummaryErrorCode
    {
        None = 0,
        NoPendingResearch = 1,
        MissingProgressState = 2,
        InvalidPendingState = 3,
        ProgressStateSlotMismatch = 4,
        ProgressStateProjectMismatch = 5,
        MissingConfig = 6,
        DisabledConfig = 7,
        InvalidConfig = 8,
        InvalidElapsedTime = 9,
        InvalidProgressUnits = 10,
        InvalidProgressDelta = 11,
        CompletionPendingNotActive = 12
    }
    public enum ResearchProgressStateSummaryErrorCode
    {
        None = 0,
        NoPendingResearch = 1,
        MissingProgressState = 2,
        InvalidPendingState = 3,
        ProgressStateSlotMismatch = 4,
        ProgressStateProjectMismatch = 5,
        InvalidProgressUnits = 6,
        CompletionPendingNotActive = 7
    }
    public enum ResearchCompletionEligibilitySummaryErrorCode
    {
        None = 0,
        NoPendingResearch = 1,
        MissingProgressState = 2,
        InvalidPendingState = 3,
        ProgressStateSlotMismatch = 4,
        ProgressStateProjectMismatch = 5,
        MissingConfig = 6,
        DisabledConfig = 7,
        InvalidConfig = 8,
        InvalidRequiredProgressUnits = 9,
        InvalidProgressUnits = 10,
        ConfigProjectMismatch = 11,
        CompletionPendingNotActive = 12
    }
    public enum ResearchCompletionPendingApplySummaryErrorCode
    {
        None = 0,
        NoPendingResearch = 1,
        MissingProgressState = 2,
        InvalidPendingState = 3,
        ProgressStateSlotMismatch = 4,
        ProgressStateProjectMismatch = 5,
        MissingConfig = 6,
        DisabledConfig = 7,
        InvalidConfig = 8,
        InvalidRequiredProgressUnits = 9,
        InvalidProgressUnits = 10,
        ConfigProjectMismatch = 11
    }
    public enum CompletedResearchStateSummaryErrorCode
    {
        None = 0
    }
    public enum ResearchCompletionClaimReadinessSummaryErrorCode
    {
        None = 0,
        NoPendingResearch = 1,
        MissingProgressState = 2,
        InvalidPendingState = 3,
        ProgressStateSlotMismatch = 4,
        ProgressStateProjectMismatch = 5,
        MissingConfig = 6,
        DisabledConfig = 7,
        InvalidConfig = 8,
        InvalidRequiredProgressUnits = 9,
        InvalidProgressUnits = 10,
        ConfigProjectMismatch = 11
    }
    public enum ResearchCompletionClaimApplySummaryErrorCode
    {
        None = 0,
        NoPendingResearch = 1,
        MissingProgressState = 2,
        InvalidPendingState = 3,
        ProgressStateSlotMismatch = 4,
        ProgressStateProjectMismatch = 5,
        MissingEligibilityConfig = 6,
        DisabledEligibilityConfig = 7,
        InvalidEligibilityConfig = 8,
        InvalidRequiredProgressUnits = 9,
        InvalidProgressUnits = 10,
        EligibilityConfigProjectMismatch = 11,
        MissingClaimConfig = 12,
        DisabledClaimConfig = 13,
        InvalidClaimConfig = 14
    }
    public enum ResearchVerificationBoundarySummaryErrorCode
    {
        None = 0,
        NoPendingResearch = 1,
        MissingProgressState = 2,
        InvalidPendingState = 3,
        ProgressStateSlotMismatch = 4,
        ProgressStateProjectMismatch = 5,
        MissingEligibilityConfig = 6,
        DisabledEligibilityConfig = 7,
        InvalidEligibilityConfig = 8,
        InvalidRequiredProgressUnits = 9,
        InvalidProgressUnits = 10,
        EligibilityConfigProjectMismatch = 11,
        MissingVerificationConfig = 12,
        DisabledVerificationConfig = 13,
        InvalidVerificationConfig = 14,
        UnavailableVerificationMode = 15,
        AlreadyCompleted = 16
    }

[Serializable]
    public sealed class RunSimulationConfig
    {
        public double BaseSuccessChance;
        public double HeatPenaltyPerPoint;
        public double ManaReserveBonusPerPoint;
        public double CrisisFailurePenalty;
        public double SuccessThreshold;
        public int BaseScoreOnSuccess;
        public int ScorePerManaPoint;
        public int MaxRunHistoryEntries;
        public double HighHeatFeedbackThreshold;
        public double LowManaFeedbackThreshold;
        public double StrongManaReserveFeedbackThreshold;
        public string LootTableId;
        public int MinPartySize;
        public int MaxPartySize;
        public int MaxAllowedPartySize;
        public double SuccessSurvivorRatio;
        public double FailureSurvivorRatio;
        public string CasualtyPressureRuleSourceId;
        public double CasualtyPressurePerDanger;
        public double CasualtyPressureReductionPerPathCapacity;
        public double CasualtyPressurePerManaPressure;
        public double CautiousCasualtyPressureMultiplier;
        public double BalancedCasualtyPressureMultiplier;
        public double GreedyCasualtyPressureMultiplier;
        public double CasualtyPressureMinimum;
        public double CasualtyPressureMaximum;
        public double CasualtyLootExtractionPenaltyPerCasualty;
        public double CasualtyHeatDeltaPerCasualty;
        public double PartyWipeCasualtyPressureThreshold;
        public string MvpPlacementEffectsRuleSourceId;
        public MvpPlacementEffectConfig[] MvpPlacementEffects = Array.Empty<MvpPlacementEffectConfig>();
        public MvpCompositionOutcomeTuningConfig MvpCompositionOutcomeTuning;
        public string LootExtractionRoundingPolicyId;
        public string LootExtractionRuleSourceId;
        public string LootHeatCoolingRuleSourceId;
        public double LootHeatCoolingPerTradeableWorldValue;
        public double MaxLootHeatCoolingPerRun;
        public string AdventurerAttractionRuleSourceId;
        public double AdventurerAttractionPerExtractedWorldValue;
        public string AdventurerInterestForecastRuleSourceId;
        public double AdventurerInterestLowThreshold;
        public double AdventurerInterestMediumThreshold;
        public double AdventurerInterestHighThreshold;
        public double AdventurerInterestScorePerAttractionSignal;
        public string AdventurerDemandBudgetRuleSourceId;
        public double AdventurerDemandBudgetScorePerForecastScore;
        public double AdventurerDemandBudgetLowThreshold;
        public double AdventurerDemandBudgetMediumThreshold;
        public double AdventurerDemandBudgetHighThreshold;
        public double RunHeatNormalDeathDelta;
        public double RunHeatEliteDeathDelta;
        public double RunHeatMultipleDeathBonusDelta;
        public double RunHeatSurvivorCoolingPerSurvivor;
        public double RunHeatLootCoolingPerExtractedValue;
        public double RunHeatDeltaMinimum;
        public double RunHeatDeltaMaximum;
        public string RunHeatDeltaRuleSourceId;
        public double HeatPeaceMinimum;
        public double HeatPeaceMaximum;
        public double HeatNoticeMinimum;
        public double HeatNoticeMaximum;
        public double HeatConcernMinimum;
        public double HeatConcernMaximum;
        public string RunHeatApplicationRuleSourceId;
        public string AdventurerPartyCompositionRuleSourceId;
        public int AdventurerPartyCompositionMinSize;
        public int AdventurerPartyCompositionMaxSize;
        public int AdventurerPartyCompositionMaxAllowedSize;
        public string[] AdventurerPartyCompositionClassIds = Array.Empty<string>();
        public RunPostureConfig[] RunPostures = Array.Empty<RunPostureConfig>();
        public string AdventurerIntentRuleSourceId;
        public double IntentGreedyScorePerLoot;
        public double IntentGreedyScorePerAttraction;
        public double IntentGreedyPenaltyPerHeatTierRank;
        public double IntentGreedyPenaltyPerRecentDeath;
        public double IntentGreedyPenaltyPerDanger;
        public double IntentCautiousScorePerDanger;
        public double IntentCautiousScorePerHeatPressure;
        public double IntentCautiousScorePerHeatTierRank;
        public double IntentCautiousScorePerRecentDeath;
        public double IntentCautiousReductionPerPathCapacity;
        public double IntentBalancedBaseScore;
        public double IntentBalancedPenaltyPerExtremeScoreDelta;
        public double IntentModerateRiskTarget;
        public double IntentModerateRewardTarget;
        public double IntentBalancedPenaltyPerModerateDistance;
        public double IntentMinimumScore;
        public double IntentMaximumScore;
        public string AdventurerArrivalPressureRuleSourceId;
        public double ArrivalPressureScorePerLoot;
        public double ArrivalPressureScorePerAttraction;
        public double ArrivalPressureScorePerDanger;
        public double ArrivalPressureScorePerHeatPressure;
        public double ArrivalPressureScorePerRecentRecoveredLoot;
        public double ArrivalPressureLatestSuccessBonus;
        public double ArrivalPressureLatestFailurePenalty;
        public double ArrivalPressurePathCompleteBonus;
        public double ArrivalPressureIncompletePathPenalty;
        public double ArrivalPressureHeatNoticePenalty;
        public double ArrivalPressureHeatConcernPenalty;
        public double ArrivalPressureRecentDeathPenalty;
        public double ArrivalPressureNoneThreshold;
        public double ArrivalPressureLowThreshold;
        public double ArrivalPressureCautiousThreshold;
        public double ArrivalPressureBuildingThreshold;
        public double ArrivalPressureLikelySoonThreshold;
        public string AdventurerTrafficPressureRuleSourceId;
        public double TrafficScoreWeightArrivalPressure;
        public double TrafficScoreWeightLootSignal;
        public double TrafficScoreWeightAttractionSignal;
        public double TrafficScoreWeightDangerSignal;
        public double TrafficScoreWeightHeatPressureSignal;
        public double TrafficScoreWeightRecentRecoveredLoot;
        public double TrafficPathCompleteBonus;
        public double TrafficIncompletePathPenalty;
        public double TrafficRecentDeathCautionModifier;
        public double TrafficHeatCautionModifier;
        public double TrafficNoneThreshold;
        public double TrafficLowThreshold;
        public double TrafficBuildingThreshold;
        public double TrafficSteadyThreshold;
        public double TrafficHeavyThreshold;
        public int TrafficDangerousChurnRecentDeathThreshold;
        public double TrafficDangerousChurnMinimumInterestScore;
        public double TrafficEstimatedPartyCountMultiplier;
        public double TrafficEstimatedPartyCountScoreDivisor;
        public int TrafficEstimatedPartyCountLowThreshold;
        public int TrafficEstimatedPartyCountMediumThreshold;
        public int TrafficMinimumEstimatedConcurrentParties;
        public int TrafficMaximumEstimatedConcurrentParties;
        public MvpFirstSessionObjectiveConfig MvpFirstSessionObjective;
    }

    [Serializable]
    public sealed class MvpPlacementEffectConfig
    {
        public string CategoryId;
        public string OptionId;
        public int PathCapacity;
        public int Danger;
        public int ManaPressure;
        public int HeatPressure;
        public int LootBonus;
        public int Attraction;
        public string ExplanationKey;
    }

    [Serializable]
    public sealed class MvpCompositionOutcomeTuningConfig
    {
        public string RuleSourceId;
        public double SuccessChancePerPathCapacity;
        public double SuccessChancePenaltyPerDanger;
        public double ManaReserveCostPerManaPressure;
        public double SurvivorRatioBonusPerPathCapacity;
        public double SurvivorRatioPenaltyPerDanger;
        public double GeneratedLootMultiplierPerLootBonus;
        public double ExtractedLootMultiplierPerLootBonus;
        public double HeatDeltaPerHeatPressure;
        public double AttractionSignalPerAttraction;
    }

    [Serializable]
    public sealed class RunPostureConfig
    {
        public string Id;
        public string DisplayNameKey;
        public double GeneratedLootWorldValueMultiplier = 1d;
        public double ExtractedLootWorldValueMultiplier = 1d;
        public double HeatDeltaOffset = 0d;
    }

    [Serializable]
    public sealed class RunOutcomeRecord
    {
        public string RunId;
        public long TickStarted;
        public bool Success;
        public int Score;
        public string ReasonKey;
        public double HeatAtStart;
        public double ManaAtStart;
        public bool CrisisActiveAtStart;
        public bool HasBreakdown;
        public double BaseChance;
        public double HeatPenaltyApplied;
        public double ManaBonusApplied;
        public double CrisisPenaltyApplied;
        public double FinalChance;
        public double SuccessThresholdUsed;
        public string[] FeedbackTagKeys = Array.Empty<string>();
        public RunLootSummary LootSummary;
        public RunSurvivalSummary SurvivalSummary;
        public RunLootExtractionSummary LootExtractionSummary;
        public RunLootDropRecord[] LootBreakdown = Array.Empty<RunLootDropRecord>();
        public RunLootHeatCoolingSummary LootHeatCoolingSummary;
        public RunAdventurerAttractionSummary AdventurerAttractionSummary;
        public RunAdventurerInterestForecastSummary AdventurerInterestForecastSummary;
        public RunAdventurerDemandBudgetSummary AdventurerDemandBudgetSummary;
        public RunHeatDeltaSummary RunHeatDeltaSummary;
        public RunHeatApplicationSummary RunHeatApplicationSummary;
        public RunCompositionOutcomeSummary CompositionOutcomeSummary;
        public string RunPostureId;
    }

    [Serializable]
    public sealed class RunCompositionOutcomeSummary
    {
        public bool RuleResolved = false;
        public string RuleSourceId;
        public MvpPlacementEffectsSummary PlacementEffects = new MvpPlacementEffectsSummary();
        public double SuccessChanceDelta = 0d;
        public double EffectiveManaReserve = 0d;
        public double ManaReservePressureCost = 0d;
        public double SurvivorRatioDelta = 0d;
        public double GeneratedLootMultiplier = 1d;
        public double ExtractedLootMultiplier = 1d;
        public double HeatDeltaOffset = 0d;
        public double AttractionSignalBonus = 0d;
    }

    [Serializable]
    public sealed class RunSurvivalSummary
    {
        public int PartySize;
        public int SurvivorCount;
        public int DeathCount;
        public double SurvivorRatio;
        public double CasualtyPressure;
        public double CasualtyLootExtractionPenalty;
        public double CasualtyHeatDelta;
        public int DeterministicSeed;
        public bool RuleResolved;
        public int DeterministicErrorCode;
        public string RuleSourceId;
        public bool SuccessAtResolution;
    }

    [Serializable]
    public sealed class RunLootSummary
    {
        public string LootTableId;
        public int ResolverSeed;
        public bool ResolverSuccess;
        public int ResolverErrorCode;
        public int RollCount;
        public string[] GeneratedItemIds = Array.Empty<string>();
        public int TotalGeneratedWorldValue;
        public int TotalGeneratedReserveCost;
        public int TotalGeneratedTradeableWorldValue;
    }

    [Serializable]
    public sealed class RunLootDropRecord
    {
        public string LootId;
        public string NameKey;
        public int Quantity;
        public int TotalWorldValue;
        public int TotalTradeableWorldValue;
    }

    [Serializable]
    public sealed class RunLootExtractionSummary
    {
        public string RuleSourceId;
        public int DeterministicSeed;
        public bool RuleResolved;
        public int DeterministicErrorCode;
        public double SurvivorRatioUsed;
        public int GeneratedItemCount;
        public string[] ExtractedItemIds = Array.Empty<string>();
        public string[] LostItemIds = Array.Empty<string>();
        public int TotalExtractedWorldValue;
        public int TotalExtractedReserveCost;
        public int TotalExtractedTradeableWorldValue;
    }


    [Serializable]
    public sealed class RunLootHeatCoolingSummary
    {
        public string RuleSourceId;
        public int DeterministicSeed;
        public bool RuleResolved;
        public int DeterministicErrorCode;
        public double ExtractedTradeableWorldValueUsed;
        public double CoolingPerTradeableWorldValueUsed;
        public double UnclampedHeatDelta;
        public double AppliedHeatDelta;
        public double HeatBeforeCooling;
        public double HeatAfterCooling;
    }

    [Serializable]
    public sealed class RunAdventurerAttractionSummary
    {
        public string RuleSourceId;
        public int DeterministicSeed;
        public bool RuleResolved;
        public int DeterministicErrorCode;
        public double ExtractedWorldValueUsed;
        public double AttractionPerExtractedWorldValueUsed;
        public double AttractionSignalValue;
    }

    [Serializable]
    public sealed class RunAdventurerInterestForecastSummary
    {
        public string RuleSourceId;
        public int DeterministicSeed;
        public bool RuleResolved;
        public int DeterministicErrorCode;
        public double AttractionSignalUsed;
        public double ForecastInterestScore;
        public string ForecastBandId;
    }

    [Serializable]
    public sealed class RunAdventurerDemandBudgetSummary
    {
        public string RuleSourceId;
        public int DeterministicSeed;
        public bool RuleResolved;
        public int DeterministicErrorCode;
        public string ForecastBandIdUsed;
        public double ForecastInterestScoreUsed;
        public double DemandBudgetScore;
        public string DemandBudgetBandId;
    }
    [Serializable]
    public sealed class AdventurerPartyCompositionSummary
    {
        public string RuleSourceId;
        public int DeterministicSeed;
        public bool RuleResolved;
        public int DeterministicErrorCode;
        public string[] ClassIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class AdventurerRunIntentSummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)AdventurerRunIntentSummaryErrorCode.None;
        public string RuleSourceId;
        public string IntentId;
        public string PostureId;
        public string PrimaryReasonKey;
        public string SecondaryReasonKey;
        public double CautiousScore;
        public double BalancedScore;
        public double GreedyScore;
        public double ConfidenceScore;
        public bool WouldMutateState = false;
    }

    [Serializable]
    public sealed class AdventurerArrivalPressureSummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)AdventurerArrivalPressureSummaryErrorCode.None;
        public string RuleSourceId;
        public string PressureBandId;
        public string PrimaryReasonKey;
        public double Score;
        public bool WouldMutateState = false;
        public bool PathComplete = false;
        public int LootSignal = 0;
        public int AttractionSignal = 0;
        public int DangerSignal = 0;
        public int HeatPressureSignal = 0;
        public int RecentDeathCount = 0;
        public int RecentRecoveredLoot = 0;
        public string LatestRunOutcomeId;
        public int HeatTierRank = 0;
    }

    [Serializable]
    public sealed class AdventurerTrafficPressureSummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)AdventurerTrafficPressureSummaryErrorCode.None;
        public string RuleSourceId;
        public string TrafficBandId;
        public string PrimaryReasonKey;
        public double TrafficScore = 0d;
        public int EstimatedConcurrentPartyCount = 0;
        public string EstimatedConcurrentPartyBandId;
        public string PressureBandIdUsed;
        public string IntentIdUsed;
        public bool PathComplete = false;
        public int LootSignal = 0;
        public int AttractionSignal = 0;
        public int DangerSignal = 0;
        public int HeatPressureSignal = 0;
        public int RecentDeathCount = 0;
        public int RecentRecoveredLoot = 0;
        public bool WouldMutateState = false;
    }

    [Serializable]
    public sealed class RunHeatDeltaSummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)RunHeatDeltaSummaryErrorCode.None;
        public double DeathHeatDelta = 0d;
        public double EliteDeathHeatDelta = 0d;
        public double MultipleDeathBonusDelta = 0d;
        public double SurvivorCoolingDelta = 0d;
        public double LootCoolingDelta = 0d;
        public double FinalHeatDelta = 0d;
        public string RuleSourceIdUsed;
        public int DeterministicSeed = 0;
    }
    [Serializable]
    public sealed class RunHeatApplicationSummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)RunHeatApplicationSummaryErrorCode.LegacyDefaultUnresolved;
        public double HeatBefore = 0d;
        public double AppliedDelta = 0d;
        public double HeatAfter = 0d;
        public string TierBefore;
        public string TierAfter;
        public bool TierChanged = false;
        public string RuleSourceIdUsed;
    }
    [Serializable]
    public sealed class CurrentHeatTierSummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)CurrentHeatTierSummaryErrorCode.None;
        public double CurrentHeat = 0d;
        public string TierId;
        public double TierMinimum = 0d;
        public double TierMaximum = 0d;
        public bool IsAtTierMinimum = false;
        public bool IsAtTierMaximum = false;
        public string RuleSourceIdUsed;
    }
    [Serializable]
    public sealed class MvpPlayerLoopSummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)MvpPlayerLoopSummaryErrorCode.None;
        public bool HasPlacementContext = false;
        public string SelectedStructureId;
        public MvpDungeonPlacementEntry[] DungeonPlacements = Array.Empty<MvpDungeonPlacementEntry>();
        public MvpPlacementEffectsSummary PlacementEffects = new MvpPlacementEffectsSummary();
        public MvpPlacementEffectsSummary LatestRunPlacementEffects = new MvpPlacementEffectsSummary();
        public bool HasRunOutcome = false;
        public string LatestRunId;
        public bool RunSucceeded = false;
        public double ManaReserve = 0d;
        public int LootGeneratedWorldValue = 0;
        public int LootExtractedWorldValue = 0;
        public int LootExtractedTradeableWorldValue = 0;
        public RunLootDropRecord[] LootBreakdown = Array.Empty<RunLootDropRecord>();
        public int LatestRunPartySize = 0;
        public int LatestRunSurvivorCount = 0;
        public int LatestRunDeathCount = 0;
        public double HeatBefore = 0d;
        public double HeatAfter = 0d;
        public string HeatTierId;
        public bool HasResearchStatus = false;
        public string ResearchProjectId;
        public string ResearchStatusKey;
        public bool HasResearchUnlock = false;
        public string ResearchUnlockId;
        public string ResearchUnlockSummaryKey;
        public int ResearchUnlockDeterministicErrorCode = (int)ResearchUnlockSummaryErrorCode.NoCompletedResearch;
        public bool ResearchVerificationRuleResolved = false;
        public int ResearchVerificationDeterministicErrorCode = (int)ResearchVerificationBoundarySummaryErrorCode.None;
        public bool VerificationRequired = false;
        public bool VerificationAvailable = false;
        public bool CanClaimProduction = false;
        public string[] AdventurerPartyClassIds = Array.Empty<string>();
        public bool AdventurerPartyPreviewResolved = false;
        public int AdventurerPartyPreviewDeterministicErrorCode = (int)AdventurerPartyCompositionSummaryErrorCode.None;
        public string AdventurerPartyPreviewRuleSourceId;
        public string NextOptimizationSuggestionKey;
        public bool AnalysisUnlocked = false;
        public string AnalysisAdviceKey;
        public bool WouldMutateState = false;
        public bool WouldGrantRewards = false;
        public bool WouldUnlockContent = false;
        public bool WouldCallServer = false;
        public bool WouldProcessOfflineProgress = false;
        public AdventurerRunIntentSummary AdventurerRunIntent = new AdventurerRunIntentSummary();
        public AdventurerArrivalPressureSummary AdventurerArrivalPressure = new AdventurerArrivalPressureSummary();
        public AdventurerTrafficPressureSummary AdventurerTrafficPressure = new AdventurerTrafficPressureSummary();
    }

    [Serializable]
    public sealed class MvpPlacementEffectsSummary
    {
        public bool RuleResolved = false;
        public string RuleSourceId;
        public int PathCapacity = 0;
        public int Danger = 0;
        public int ManaPressure = 0;
        public int HeatPressure = 0;
        public int LootBonus = 0;
        public int Attraction = 0;
        public string[] ContributingOptionIds = Array.Empty<string>();
        public string[] EffectLocalizationKeys = Array.Empty<string>();
    }

    [Serializable]
    public sealed class GuidedMvpActionPathSummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)GuidedMvpActionPathErrorCode.None;
        public string CurrentStepId;
        public string CurrentStepStatusKey;
        public string NextActionKey;
        public bool IsComplete = false;
        public bool WouldMutateState = false;
        public bool WouldGrantRewards = false;
        public bool WouldUnlockContent = false;
        public bool WouldChargeCosts = false;
        public bool WouldCallServer = false;
        public bool WouldProcessOfflineResearch = false;
        public bool WouldProcessOfflineHeat = false;
        public bool WouldStartRaid = false;
    }

    [Serializable]
    public sealed class ResearchPendingState
    {
        public string SlotId;
        public string ProjectId;
    }

    [Serializable]
    public sealed class ResearchPendingScaffoldConfig
    {
        public bool enabled;
        public string slotId;
        public string projectId;
        public string ruleSourceId;
    }

    [Serializable]
    public sealed class ResearchProgressScaffoldConfig
    {
        public bool enabled;
        public string ruleSourceId;
        public double progressPerActiveSecond;
        public long maxActiveSessionElapsedSeconds;
    }

    [Serializable]
    public sealed class ResearchCompletionEligibilityScaffoldConfig
    {
        public bool enabled;
        public string ruleSourceId;
        public string projectId;
        public double requiredProgressUnits;
    }

    [Serializable]
    public sealed class ResearchCompletionClaimScaffoldConfig
    {
        public bool enabled;
        public string ruleSourceId;
    }

    [Serializable]
    public sealed class ResearchVerificationScaffoldConfig
    {
        public bool enabled;
        public string ruleSourceId;
        public string verificationMode;
    }

    [Serializable]
    public sealed class ResearchUnlockBridgeConfig
    {
        public bool enabled;
        public string ruleSourceId;
        public ResearchUnlockDefinitionConfig[] unlocks = Array.Empty<ResearchUnlockDefinitionConfig>();
    }

    [Serializable]
    public sealed class ResearchUnlockDefinitionConfig
    {
        public string researchProjectId;
        public string unlockId;
        public string summaryKey;
    }

    [Serializable]
    public sealed class ResearchProgressState
    {
        public string SlotId;
        public string ProjectId;
        public double ProgressUnits = 0d;
        public bool CompletionPending = false;
        public string RuleSourceIdUsed;
    }

    [Serializable]
    public sealed class CompletedResearchState
    {
        public string[] ProjectIds;
        public string LastCompletedProjectId;
        public string LastCompletionRuleSourceId;
    }

    [Serializable]
    public sealed class ResearchUnlockSummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)ResearchUnlockSummaryErrorCode.None;
        public bool HasCompletedResearch = false;
        public string MatchedProjectId;
        public string UnlockId;
        public string SummaryLocalizationKey;
        public string RuleSourceIdUsed;
        public bool WouldMutateState = false;
        public bool WouldGrantRewards = false;
        public bool WouldUnlockContent = false;
    }

    [Serializable]
    public sealed class CompletedResearchStateSummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)CompletedResearchStateSummaryErrorCode.None;
        public bool HasCompletedState = false;
        public int CompletedCount = 0;
        public string LastCompletedProjectId;
        public string LastCompletionRuleSourceId;
        public string CurrentPendingProjectId;
        public string CurrentProgressProjectId;
        public bool CurrentProjectAlreadyCompleted = false;
        public bool WouldBlockClaimAsDuplicate = false;
        public bool WouldGrantRewards = false;
        public bool WouldUnlockContent = false;
        public string RuleSourceIdUsed;
    }

    [Serializable]
    public sealed class ResearchProgressApplySummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)ResearchProgressApplySummaryErrorCode.None;
        public bool Pending = false;
        public bool HasProgressState = false;
        public string SlotId;
        public string ProjectId;
        public long ElapsedSecondsUsed = 0;
        public double ProgressDeltaApplied = 0d;
        public double PreviousProgressUnits = 0d;
        public double NextProgressUnits = 0d;
        public bool WouldCompleteResearch = false;
        public string RuleSourceIdUsed;
    }

    [Serializable]
    public sealed class ResearchProgressStateSummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)ResearchProgressStateSummaryErrorCode.None;
        public bool Pending = false;
        public bool HasProgressState = false;
        public string SlotId;
        public string ProjectId;
        public double ProgressUnits = 0d;
        public bool CompletionPending = false;
        public bool StateMatchesPending = false;
        public string RuleSourceIdUsed;
    }

    [Serializable]
    public sealed class ResearchCompletionEligibilitySummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)ResearchCompletionEligibilitySummaryErrorCode.None;
        public bool Pending = false;
        public bool HasProgressState = false;
        public string SlotId;
        public string ProjectId;
        public double ProgressUnits = 0d;
        public double RequiredProgressUnits = 0d;
        public double RemainingProgressUnits = 0d;
        public bool EligibleForCompletion = false;
        public bool WouldSetCompletionPending = false;
        public bool WouldCompleteResearch = false;
        public string RuleSourceIdUsed;
    }

    [Serializable]
    public sealed class ResearchCompletionPendingApplySummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)ResearchCompletionPendingApplySummaryErrorCode.None;
        public bool Pending = false;
        public bool HasProgressState = false;
        public string SlotId;
        public string ProjectId;
        public double ProgressUnits = 0d;
        public double RequiredProgressUnits = 0d;
        public bool EligibleForCompletion = false;
        public bool AlreadyCompletionPending = false;
        public bool WouldSetCompletionPending = false;
        public bool WouldCompleteResearch = false;
        public string RuleSourceIdUsed;
    }

    [Serializable]
    public sealed class ResearchCompletionClaimReadinessSummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)ResearchCompletionClaimReadinessSummaryErrorCode.None;
        public bool Pending = false;
        public bool HasProgressState = false;
        public string SlotId;
        public string ProjectId;
        public double ProgressUnits = 0d;
        public double RequiredProgressUnits = 0d;
        public bool CompletionPending = false;
        public bool EligibleForCompletion = false;
        public bool ReadyForClaim = false;
        public bool WouldCompleteResearch = false;
        public bool WouldGrantRewards = false;
        public bool WouldUnlockContent = false;
        public bool WouldClearPending = false;
        public string RuleSourceIdUsed;
    }

    [Serializable]
    public sealed class ResearchCompletionClaimApplySummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)ResearchCompletionClaimApplySummaryErrorCode.None;
        public bool Pending = false;
        public bool HasProgressState = false;
        public bool HasCompletedState = false;
        public string SlotId;
        public string ProjectId;
        public double ProgressUnits = 0d;
        public double RequiredProgressUnits = 0d;
        public bool CompletionPending = false;
        public bool EligibleForCompletion = false;
        public bool ReadyForClaim = false;
        public bool AlreadyCompleted = false;
        public bool WouldRecordCompletedResearch = false;
        public bool WouldClearPending = false;
        public bool WouldClearProgress = false;
        public bool WouldGrantRewards = false;
        public bool WouldUnlockContent = false;
        public bool WouldChargeCosts = false;
        public bool WouldProcessOfflineProgress = false;
        public string RuleSourceIdUsed;
    }

    [Serializable]
    public sealed class ResearchVerificationBoundarySummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)ResearchVerificationBoundarySummaryErrorCode.None;
        public bool Pending = false;
        public bool HasProgressState = false;
        public bool HasCompletedState = false;
        public string SlotId;
        public string ProjectId;
        public double ProgressUnits = 0d;
        public double RequiredProgressUnits = 0d;
        public bool CompletionPending = false;
        public bool EligibleForCompletion = false;
        public bool AlreadyCompleted = false;
        public bool VerificationRequired = false;
        public bool VerificationAvailable = false;
        public bool VerificationSatisfied = false;
        public bool CanClaimProduction = false;
        public bool WouldCallServer = false;
        public bool WouldGrantRewards = false;
        public bool WouldUnlockContent = false;
        public bool WouldChargeCosts = false;
        public bool WouldProcessOfflineProgress = false;
        public string VerificationModeUsed;
        public string RuleSourceIdUsed;
    }

    public enum ResearchStatusPresentationState
    {
        NoResearch = 0,
        ActiveInProgress = 1,
        ActiveCompletionPending = 2,
        VerificationRequired = 3,
        ReadyToClaim = 4,
        Completed = 5,
        BlockedOrInvalid = 6
    }

    [Serializable]
    public sealed class ResearchStatusPresentation
    {
        public ResearchStatusPresentationState State = ResearchStatusPresentationState.BlockedOrInvalid;
        public bool Pending = false;
        public bool HasProgressState = false;
        public bool HasCompletedState = false;
        public string SlotId;
        public string ProjectId;
        public double ProgressUnits = 0d;
        public double RequiredProgressUnits = 0d;
        public bool CompletionPending = false;
        public bool EligibleForCompletion = false;
        public bool VerificationRequired = false;
        public bool ReadyToClaim = false;
        public bool Completed = false;
        public bool BlockedOrInvalid = true;
        public bool CanClaimProduction = false;
        public bool WouldGrantRewards = false;
        public bool WouldUnlockContent = false;
        public bool WouldChargeCosts = false;
        public bool WouldProcessOfflineProgress = false;
        public string StatusLocalizationKey;
        public string RuleSourceIdUsed;
    }

    [Serializable]
    public sealed class ResearchProgressSummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)ResearchProgressSummaryErrorCode.None;
        public bool Pending = false;
        public string SlotId;
        public string ProjectId;
        public long ElapsedSecondsUsed = 0;
        public double ProgressDeltaPreview = 0d;
        public bool WouldCompleteResearch = false;
        public string RuleSourceIdUsed;
    }

    [Serializable]
    public sealed class ResearchPendingValidationResult
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)ResearchPendingValidationErrorCode.None;
        public bool Pending = false;
        public string SlotId;
        public string ProjectId;
        public string RuleSourceIdUsed;
    }

    [Serializable]
    public sealed class OfflineSummary
    {
        public bool RuleResolved = false;
        public int DeterministicErrorCode = (int)OfflineSummaryErrorCode.None;
        public long OfflineSecondsObserved = 0;
        public bool OfflineWindowClamped = false;
        public bool ResearchPending = false;
        public string ResearchSlotId;
        public string ResearchProjectId;
        public bool WouldProcessOfflineProgress = false;
        public string RuleSourceIdUsed;
    }

    [Serializable]
    public sealed class RunHistoryState
    {
        public int NextRunSequence = 1;
        public RunOutcomeRecord LatestOutcome;
        public RunOutcomeRecord[] RecentOutcomes = Array.Empty<RunOutcomeRecord>();

        public void AppendOutcome(RunOutcomeRecord outcome, int maxEntries)
        {
            if (outcome == null)
            {
                return;
            }

            int boundedMax = Mathf.Max(1, maxEntries);
            RunOutcomeRecord[] source = RecentOutcomes ?? Array.Empty<RunOutcomeRecord>();
            int appendCount = source.Length + 1;
            int keepCount = Mathf.Min(appendCount, boundedMax);
            var next = new RunOutcomeRecord[keepCount];
            int startIndex = Mathf.Max(0, appendCount - keepCount);

            for (int i = 0; i < keepCount; i++)
            {
                int index = startIndex + i;
                next[i] = index < source.Length ? source[index] : outcome;
            }

            RecentOutcomes = next;
            LatestOutcome = outcome;
        }
    }

    [Serializable]
    public class ContentBootstrap
    {
        public string schema;
        public int schemaVersion;

        public string contentVersion;
        public string minAppVersion;

        public int tickSeconds;

        public TimeRules timeRules;
        public ResearchPendingScaffoldConfig researchPendingScaffold;
        public ResearchProgressScaffoldConfig researchProgressScaffold;
        public ResearchCompletionEligibilityScaffoldConfig researchCompletionEligibilityScaffold;
        public ResearchCompletionClaimScaffoldConfig researchCompletionClaimScaffold;
        public ResearchVerificationScaffoldConfig researchVerificationScaffold;
        public ResearchUnlockBridgeConfig researchUnlockBridge;
        public FeatureFlags featureFlags;
        public Tables tables;
    }

    [Serializable]
    public class TimeRules
    {
        public bool allowOfflineProgression;
        public int maxOfflineSeconds;
        public int detectClockSkewSeconds;
        public string offlineSummaryRuleSourceId;
    }

    [Serializable]
    public class FeatureFlags
    {
        public bool enableDevPanel;
        public bool enableTelemetry;
        public bool enableOfflineRestrictions;
    }

    [Serializable]
    public class Tables
    {
        public StringTableRef stringTable;
        public HeatRuntimeRef heatRuntime;
    }

    [Serializable]
    public class StringTableRef
    {
        public string path;
    }



    [Serializable]
    public class HeatRuntimeRef
    {
        public string path;
    }

    [Serializable]
    public class HeatRuntimeConfig
    {
        public string schema;
        public int schemaVersion;
        public double decayPerTick = 0.1d;
        public double minHeat = 0d;
        public bool enableWarnings = true;
    }
    [Serializable]
    public class BuildConfig
    {
        public string schema;
        public int schemaVersion;

        public string environment;
        public LoggingConfig logging;
        public SaveConfig save;
        public UiConfig ui;
    }

    [Serializable]
    public class LoggingConfig
    {
        public string logLevel;
        public bool includeTimestamps;
        public bool writeToFile;
    }

    [Serializable]
    public class SaveConfig
    {
        public string slot;
        public string fileName;
        public bool useAtomicWrites;
        public int keepBackups;
    }

    [Serializable]
    public class UiConfig
    {
        public bool showBuildInfo;
        public bool showState;
        public int bannerTimeoutSeconds;
    }

    [Serializable]
    public class SchemaVersions
    {
        public string schema;
        public int schemaVersion;

        public ContentSchemaMap content;
        public SaveSchemaMap save;
    }

    [Serializable]
    public class ContentSchemaMap
    {
        public int content_bootstrap;
        public int string_table;
        public int mana_modifiers;
        public int heat_modifiers;
        public int research_modifiers;
    }

    [Serializable]
    public class SaveSchemaMap
    {
        public int save_data;
    }

    [Serializable]
    public class ContentManifest
    {
        public string schema;
        public int schemaVersion;

        public string contentVersion;
        public string minAppVersion;
        public ManifestSchemaEntry[] requiredSchemas;
    }

    [Serializable]
    public class ManifestSchemaEntry
    {
        public string schemaId;
        public int schemaVersion;
    }

    [Serializable]
    public class StringTable
    {
        public string schema;
        public int schemaVersion;

        public string language;
        public StringEntry[] entries;
    }

    [Serializable]
    public class StringEntry
    {
        public string key;
        public string text;
    }

    [Serializable]
    public class DevCommands
    {
        public string schema;
        public int schemaVersion;

        public DevCommand[] commands;
    }

    [Serializable]
    public class DevCommand
    {
        public string id;
        public string labelKey;
        public bool enabled;
    }


    [Serializable]
    public sealed class LootConfig
    {
        public string schema;
        public int schemaVersion;
        public LootItemRecord[] items;
        public LootTableRecord[] tables;
    }

    [Serializable]
    public sealed class LootItemRecord
    {
        public string id;
        public string tierId;
        public string rarityId;
        public string categoryId;
        public int worldValue;
        public int reserveCost;
        public string nameKey;
        public string descriptionKey;
        public bool isTradeable;
    }

    [Serializable]
    public sealed class LootTableRecord
    {
        public string id;
        public int minRollCount;
        public int maxRollCount;
        public bool allowEmptyPool;
        public LootTablePoolEntry[] pool;
    }

    [Serializable]
    public sealed class LootTablePoolEntry
    {
        public string itemId;
        public double weight;
    }

    public enum SaveReason
    {
        Boot = 0,
        ManualDev = 1,
        AppPause = 2,
        AppQuit = 3,
        StateChange = 4,
        Periodic = 5
    }

    [Serializable]
    public class SaveData
    {
        public int saveVersion = 1;
        public string contentVersion = "0.0.0";

        public long createdUtcUnix;
        public long lastSavedUtcUnix;

        public long lastPausedUtcUnix;
        public long lastResumedUtcUnix;

        public long totalTicks;
        public string lastKnownAppState = "None";

        public DungeonLayoutState dungeonLayout;
        public MvpDungeonPlacementState mvpDungeonPlacements = new MvpDungeonPlacementState();
        public MvpDungeonFloorLayoutState mvpDungeonFloorLayout = MvpDungeonFloorLayoutState.CreateEmptyStarterFloor();
        public StructureRuntimeState structureRuntime = new StructureRuntimeState();
        public RunHistoryState runHistory = new RunHistoryState();
        public ResearchPendingState researchPending;
        public ResearchProgressState researchProgress;
        public CompletedResearchState completedResearch;
        public OfflineSummary lastOfflineSummary;

        public string[] integrityFlags = Array.Empty<string>();
    }

    public static class TimeUtil
    {
        public static long UtcNowUnixSeconds()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            return now.ToUnixTimeSeconds();
        }
    }
}
