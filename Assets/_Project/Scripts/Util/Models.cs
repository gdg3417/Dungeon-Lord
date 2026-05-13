using System;
using UnityEngine;

namespace DungeonBuilder.M0
{
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
    }

    [Serializable]
    public class StringTableRef
    {
        public string path;
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
