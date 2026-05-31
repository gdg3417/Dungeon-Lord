using System;
using DungeonBuilder.M0.Gameplay.Structures;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
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
        public RunLootHeatCoolingSummary LootHeatCoolingSummary;
        public RunAdventurerAttractionSummary AdventurerAttractionSummary;
        public RunAdventurerInterestForecastSummary AdventurerInterestForecastSummary;
        public RunAdventurerDemandBudgetSummary AdventurerDemandBudgetSummary;
        public RunHeatDeltaSummary RunHeatDeltaSummary;
        public RunHeatApplicationSummary RunHeatApplicationSummary;
    }

    [Serializable]
    public sealed class RunSurvivalSummary
    {
        public int PartySize;
        public int SurvivorCount;
        public int DeathCount;
        public double SurvivorRatio;
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
    public sealed class ResearchProgressState
    {
        public string SlotId;
        public string ProjectId;
        public double ProgressUnits = 0d;
        public bool CompletionPending = false;
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
        public StructureRuntimeState structureRuntime = new StructureRuntimeState();
        public RunHistoryState runHistory = new RunHistoryState();
        public ResearchPendingState researchPending;
        public ResearchProgressState researchProgress;
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
