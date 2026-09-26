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
                SchemaNineToTenUpgrade.TryPrepare(nine, limits, out current);
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
