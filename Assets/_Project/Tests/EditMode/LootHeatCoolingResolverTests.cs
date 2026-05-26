using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class LootHeatCoolingResolverTests
    {
        private static RunSimulationConfig Config() => new RunSimulationConfig
        {
            LootHeatCoolingRuleSourceId = "run.loot_heat_cooling.rule.v1",
            LootHeatCoolingPerTradeableWorldValue = 0.5d,
            MaxLootHeatCoolingPerRun = 10d
        };

        [Test]
        public void Resolve_MissingExtractionSummary_ReturnsFailureCode()
        {
            var result = LootHeatCoolingResolver.Resolve(Config(), null, 20d, 7);
            Assert.That(result.RuleResolved, Is.False);
            Assert.That(result.DeterministicErrorCode, Is.EqualTo((int)RunLootHeatCoolingSummaryErrorCode.ExtractionSummaryMissingOrFailed));
        }

        [Test]
        public void Resolve_PositiveExtractedTradeableValue_AppliesCoolingWithCap()
        {
            var extraction = new RunLootExtractionSummary { RuleResolved = true, DeterministicErrorCode = 0, TotalExtractedTradeableWorldValue = 30 };
            var result = LootHeatCoolingResolver.Resolve(Config(), extraction, 20d, 7);
            Assert.That(result.RuleResolved, Is.True);
            Assert.That(result.AppliedHeatDelta, Is.EqualTo(-10d));
            Assert.That(result.UnclampedHeatDelta, Is.EqualTo(-15d));
        }
    }
}
