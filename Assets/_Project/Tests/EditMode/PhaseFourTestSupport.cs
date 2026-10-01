#if UNITY_EDITOR
using System.IO;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    internal static class PhaseFourTestSupport
    {
        internal static bool Upgrade(byte[] source, CanonicalSpatialSerializationLimits limits, out byte[] current)
        {
            current = null;
            return SchemaSevenToEightUpgrade.TryPrepare(source, limits, out byte[] eight) &&
                SchemaEightToNineUpgrade.TryPrepare(eight, limits, out byte[] nine) &&
                SchemaNineToTenUpgrade.TryPrepare(nine, limits, out byte[] ten) &&
                SchemaTenToElevenUpgrade.TryPrepare(ten, limits, out byte[] eleven) &&
                SchemaElevenToTwelveUpgrade.TryPrepare(eleven, limits, out current);
        }

        // Test-only projection for fixtures that intentionally exercise frozen contracts.
        internal static string FrozenTen(byte[] current)
        {
            string value = System.Text.Encoding.UTF8.GetString(current);
            int owner = value.LastIndexOf(",\"sharedFloorKnowledge\":", System.StringComparison.Ordinal);
            if (owner >= 0)
            {
                int start = value.IndexOf('{', owner), depth = 0, end = -1;
                bool quoted = false, escaped = false;
                for (int index = start; index < value.Length; index++)
                {
                    char character = value[index];
                    if (quoted)
                    {
                        if (escaped) escaped = false;
                        else if (character == '\\') escaped = true;
                        else if (character == '"') quoted = false;
                    }
                    else if (character == '"') quoted = true;
                    else if (character == '{') depth++;
                    else if (character == '}' && --depth == 0) { end = index + 1; break; }
                }
                if (start < owner || end < start) throw new System.ArgumentException("Invalid schema 12 test fixture");
                value = value.Remove(owner, end - owner);
            }
            return value.Replace("\"schemaVersion\":12", "\"schemaVersion\":10")
                .Replace("\"schemaVersion\":11", "\"schemaVersion\":10")
                .Replace(",\"ActivationState\":1", "")
                .Replace(",\"ActivationState\":2", "");
        }
        internal static StructuralEconomySnapshot Economy(ProductionSpatialContentSnapshot production,
            CanonicalSpatialSerializationLimits limits)
        {
            Assert.That(StructuralEconomySnapshot.TryParse(File.ReadAllBytes(
                "Assets/_Project/Resources/structural_economy.json"), production.Catalog, limits, out var config), Is.True);
            return config;
        }
        internal static ContentAcquisitionEconomySnapshot Acquisition(StructuralEconomySnapshot economy,
            CanonicalSpatialSerializationLimits limits, double? testStartingMana = null)
        {
            byte[] bytes = File.ReadAllBytes("Assets/_Project/Resources/content_acquisition_economy.json");
            if (testStartingMana.HasValue)
            {
                // Explicit fake starting balance for existing QA-wallet fixtures; production is not overridden.
                var fake = UnityEngine.JsonUtility.FromJson<ContentAcquisitionEconomyConfiguration>(System.Text.Encoding.UTF8.GetString(bytes));
                fake.StartingMana = testStartingMana.Value;
                Assert.That(ContentAcquisitionEconomySnapshot.TryCreate(fake, economy, limits, out var injected), Is.True);
                return injected;
            }
            Assert.That(ContentAcquisitionEconomySnapshot.TryParse(bytes, economy, limits, out var config), Is.True);
            return config;
        }

        internal static PassiveOnlineManaConfigurationSnapshot PassiveMana(
            CanonicalSpatialSerializationLimits limits)
        {
            PassiveOnlineManaConfigurationLoadResult loaded =
                PassiveOnlineManaConfigurationSnapshot.Load(File.ReadAllBytes(
                    "Assets/_Project/Resources/passive_online_mana.json"), limits);
            Assert.That(loaded.IsSuccess, Is.True, loaded.Error.ToString());
            return loaded.Value;
        }
    }
}
#endif
