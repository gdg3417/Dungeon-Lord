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
        public DevCommands DevCommands { get; private set; }
        public StringTable Strings { get; private set; }

        private readonly Dictionary<string, string> _stringMap = new Dictionary<string, string>();

        public void LoadAll(
            TextAsset contentBootstrapJson,
            TextAsset buildConfigJson,
            TextAsset schemaVersionsJson,
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
            DevCommands = SafeParse<DevCommands>(devCommandsJson, "dev_commands", logger, ref warningBanner);
            Strings = SafeParse<StringTable>(stringTableJson, "string_table_en", logger, ref warningBanner);

            BuildStringMap(logger);
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
