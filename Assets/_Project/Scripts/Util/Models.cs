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
        public FeatureFlags featureFlags;
        public Tables tables;
    }

    [Serializable]
    public class TimeRules
    {
        public bool allowOfflineProgression;
        public int maxOfflineSeconds;
        public int detectClockSkewSeconds;
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
