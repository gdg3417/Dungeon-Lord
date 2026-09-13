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
                SchemaEightToNineUpgrade.TryPrepare(eight, limits, out current);
        }
        internal static StructuralEconomySnapshot Economy(ProductionSpatialContentSnapshot production,
            CanonicalSpatialSerializationLimits limits)
        {
            Assert.That(StructuralEconomySnapshot.TryParse(File.ReadAllBytes(
                "Assets/_Project/Resources/structural_economy.json"), production.Catalog, limits, out var config), Is.True);
            return config;
        }
    }
}
#endif
