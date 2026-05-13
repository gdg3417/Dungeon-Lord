using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public class ContentService
    {
        public ContentBootstrap Bootstrap { get; private set; }
        public BuildConfig BuildConfig { get; private set; }
        public SchemaVersions Schemas { get; private set; }
        public ContentManifest Manifest { get; private set; }
        public DevCommands DevCommands { get; private set; }
        public StringTable Strings { get; private set; }

        private readonly Dictionary<string, string> _stringMap = new Dictionary<string, string>();

        public void LoadAll(
            TextAsset contentBootstrapJson,
            TextAsset buildConfigJson,
            TextAsset schemaVersionsJson,
            TextAsset contentManifestJson,
            TextAsset devCommandsJson,
            TextAsset stringTableJson,
            SimpleLogger logger,
            out string warningBanner
        )
        {
            warningBanner = string.Empty;

            Bootstrap = SafeParse<ContentBootstrap>(contentBootstrapJson, "content_bootstrap", logger, ref warningBanner);
            BuildConfig = SafeParse<BuildConfig>(buildConfigJson, "build_config", logger, ref warningBanner);
            Schemas = SafeParse<SchemaVersions>(schemaVersionsJson, "schema_versions", logger, ref warningBanner);
            Manifest = SafeParse<ContentManifest>(contentManifestJson, "content_manifest", logger, ref warningBanner);
            DevCommands = SafeParse<DevCommands>(devCommandsJson, "dev_commands", logger, ref warningBanner);
            Strings = SafeParse<StringTable>(stringTableJson, "string_table_en", logger, ref warningBanner);

            ValidateManifest(logger, ref warningBanner);
            BuildStringMap(logger);
        }

        private void ValidateManifest(SimpleLogger logger, ref string warningBanner)
        {
            if (Manifest == null || Bootstrap == null || Schemas == null)
            {
                return;
            }

            if (!string.Equals(Manifest.contentVersion, Bootstrap.contentVersion, StringComparison.Ordinal))
            {
                AppendBanner(ref warningBanner, "Content version mismatch between manifest and bootstrap.");
                logger.Warn("Content manifest mismatch: contentVersion differs from content_bootstrap.");
            }

            if (Manifest.requiredSchemas == null)
            {
                return;
            }

            for (int i = 0; i < Manifest.requiredSchemas.Length; i++)
            {
                ManifestSchemaEntry entry = Manifest.requiredSchemas[i];
                if (entry == null || string.IsNullOrEmpty(entry.schemaId))
                {
                    continue;
                }

                bool ok = TryGetRegisteredSchemaVersion(entry.schemaId, out int registeredVersion);
                if (!ok || registeredVersion != entry.schemaVersion)
                {
                    AppendBanner(ref warningBanner, $"Schema gate failed: {entry.schemaId} v{entry.schemaVersion}");
                    logger.Warn($"Schema gate failed for {entry.schemaId}. Expected {entry.schemaVersion}, got {(ok ? registeredVersion : -1)}.");
                }
            }
        }

        private bool TryGetRegisteredSchemaVersion(string schemaId, out int version)
        {
            version = -1;
            if (Schemas == null || Schemas.content == null)
            {
                return false;
            }

            switch (schemaId)
            {
                case "content_bootstrap":
                    version = Schemas.content.content_bootstrap;
                    return true;
                case "string_table":
                    version = Schemas.content.string_table;
                    return true;
                case "mana_modifiers":
                    version = Schemas.content.mana_modifiers;
                    return true;
                case "heat_modifiers":
                    version = Schemas.content.heat_modifiers;
                    return true;
                case "research_modifiers":
                    version = Schemas.content.research_modifiers;
                    return true;
                default:
                    return false;
            }
        }

        private T SafeParse<T>(TextAsset asset, string name, SimpleLogger logger, ref string warningBanner) where T : class
        {
            if (asset == null)
            {
                AppendBanner(ref warningBanner, $"Missing JSON asset: {name}");
                logger.Error($"Missing JSON asset: {name}");
                return null;
            }

            try
            {
                T obj = JsonUtility.FromJson<T>(asset.text);
                if (obj == null)
                {
                    AppendBanner(ref warningBanner, $"Failed to parse JSON: {name}");
                    logger.Error($"Failed to parse JSON: {name}");
                }
                return obj;
            }
            catch (Exception ex)
            {
                AppendBanner(ref warningBanner, $"Invalid JSON: {name}");
                logger.Error($"Invalid JSON for {name}. Exception: {ex.Message}");
                return null;
            }
        }

        private void BuildStringMap(SimpleLogger logger)
        {
            _stringMap.Clear();

            if (Strings == null || Strings.entries == null)
            {
                logger.Warn("String table missing or empty.");
                return;
            }

            for (int i = 0; i < Strings.entries.Length; i++)
            {
                StringEntry e = Strings.entries[i];
                if (e == null || string.IsNullOrEmpty(e.key))
                {
                    continue;
                }

                if (_stringMap.ContainsKey(e.key))
                {
                    logger.Warn($"Duplicate string key: {e.key}");
                    continue;
                }

                _stringMap.Add(e.key, e.text ?? string.Empty);
            }
        }

        public string GetString(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key))
            {
                return fallback;
            }

            if (_stringMap.TryGetValue(key, out string value))
            {
                return value;
            }

            return fallback;
        }

        private void AppendBanner(ref string banner, string message)
        {
            if (string.IsNullOrEmpty(banner))
            {
                banner = message;
            }
            else
            {
                banner = banner + "\n" + message;
            }
        }
    }
}
