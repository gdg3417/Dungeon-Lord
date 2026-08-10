#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66MigrationLocalizationTests
    {
        [Test]
        public void EveryRegistryOwnedPlayerReasonResolvesInBootstrapEnglish()
        {
            string path = Path.Combine(Application.dataPath,
                "_Project/Data/Bootstrap/string_table_en.json");
            StringTable table = JsonUtility.FromJson<StringTable>(File.ReadAllText(path));
            string[] keys = (table?.entries ?? Array.Empty<StringEntry>())
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.text))
                .Select(entry => entry.key).ToArray();

            Assert.That(keys, Is.Unique);
            Assert.That(keys, Is.SupersetOf(Gd66MigrationReasonRegistry.RequiredPlayerLocalizationKeys));
            Assert.That(Gd66MigrationReasonRegistry.RequiredPlayerLocalizationKeys, Has.Count.EqualTo(87));
        }

        [Test]
        public void LocalizationKeyPreservesCompleteReasonIdentity()
        {
            const string reason = "gd66.content.migration_blocked_narrow_hall";
            Assert.That(Gd66MigrationReasonRegistry.PlayerLocalizationKey(reason), Is.EqualTo(
                "save.migration.spatial.gd66.content.migration_blocked_narrow_hall"));
            Assert.That(Gd66MigrationReasonRegistry.PlayerLocalizationKey(
                "gd66.diagnostic.canonical_write_noop"), Is.Empty);
        }

        [Test]
        public void PlayerMessagesDoNotExposeInternalMigrationNotation()
        {
            string path = Path.Combine(Application.dataPath,
                "_Project/Data/Bootstrap/string_table_en.json");
            StringEntry[] entries = JsonUtility.FromJson<StringTable>(File.ReadAllText(path)).entries
                .Where(entry => entry != null && entry.key != null && entry.key.StartsWith(
                    Gd66MigrationReasonRegistry.PlayerLocalizationPrefix,
                    StringComparison.Ordinal)).ToArray();

            foreach (StringEntry entry in entries)
            {
                Assert.That(entry.text, Does.Not.Contain("JSON"), entry.key);
                Assert.That(entry.text, Does.Not.Contain("FileRename"), entry.key);
                Assert.That(entry.text, Does.Not.Contain(" O "), entry.key);
                Assert.That(entry.text, Does.Not.Contain(" B "), entry.key);
                Assert.That(entry.text, Does.Not.Contain(" C "), entry.key);
            }
        }
    }
}
#endif
