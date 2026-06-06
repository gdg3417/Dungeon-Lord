using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using System.Collections.Generic;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpPlayerFacingLabelResolverTests
    {
        [TestCase(StructureSimulationPass.ManaGeneratorBasicId, "structure.mana_generator.basic.display_name")]
        [TestCase(StructureSimulationPass.HeatScrubberBasicId, "structure.heat_scrubber.basic.display_name")]
        [TestCase(StructureSimulationPass.RiskLabBasicId, "structure.risk_lab.basic.display_name")]
        public void KnownStructureIds_ResolveToLocalizationKeys(string structureId, string expectedKey)
        {
            var requestedKeys = new List<string>();

            string label = MvpPlayerFacingLabelResolver.ResolveStructureDisplayName(structureId, (key, fallback) =>
            {
                requestedKeys.Add(key);
                return "LOC[" + key + "]";
            });

            Assert.That(label, Is.EqualTo("LOC[" + expectedKey + "]"));
            Assert.That(label, Is.Not.EqualTo(structureId));
            Assert.That(requestedKeys, Does.Contain(expectedKey));
            Assert.That(requestedKeys, Does.Not.Contain(structureId));
        }

        [Test]
        public void UnknownStructureId_UsesSafeLocalizedFallbackInsteadOfRawId()
        {
            const string rawId = "structure.unmapped.prototype";

            string label = MvpPlayerFacingLabelResolver.ResolveStructureDisplayName(rawId, (key, fallback) => "LOC[" + key + "]");

            Assert.That(label, Is.EqualTo("LOC[ui.mvp_label.structure.unknown]"));
            Assert.That(label, Does.Not.Contain(rawId));
        }

        [Test]
        public void KnownStructureId_CanExposeDisplayKeyForDiagnosticsMappingTestsWithoutDisplayText()
        {
            bool resolved = MvpPlayerFacingLabelResolver.TryGetStructureDisplayNameKey(
                StructureSimulationPass.ManaGeneratorBasicId,
                out string key);

            Assert.That(resolved, Is.True);
            Assert.That(key, Is.EqualTo("structure.mana_generator.basic.display_name"));
        }

        [Test]
        public void UnknownResearchStatus_UsesResearchUnavailableLocalizationKey()
        {
            string label = MvpPlayerFacingLabelResolver.ResolveResearchStatusLabel(string.Empty, (key, fallback) => "LOC[" + key + "]");

            Assert.That(label, Is.EqualTo("LOC[ui.research.status.blocked_or_invalid]"));
        }
    }
}
