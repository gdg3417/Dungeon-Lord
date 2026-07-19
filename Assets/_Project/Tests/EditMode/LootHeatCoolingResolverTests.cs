#if UNITY_EDITOR
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class LootHeatCoolingResolverTests
    {
        private static RunSimulationConfig ValidConfig(double perValue = 0.5d, double maxPerRun = 10d) => new RunSimulationConfig
        {
            LootHeatCoolingRuleSourceId = "run.loot_heat_cooling.rule.v1",
            LootHeatCoolingPerTradeableWorldValue = perValue,
            MaxLootHeatCoolingPerRun = maxPerRun
        };

        private static RunLootExtractionSummary ResolvedExtraction(int tradeableValue) => new RunLootExtractionSummary
        {
            RuleResolved = true,
            DeterministicErrorCode = (int)RunLootExtractionSummaryErrorCode.None,
            DeterministicSeed = 101,
            TotalExtractedTradeableWorldValue = tradeableValue
        };

        [Test] public void MissingExtractionSummary_ReturnsExtractionFailure() =>
            Assert.That(LootHeatCoolingResolver.Resolve(ValidConfig(), null, 20d, 7).DeterministicErrorCode, Is.EqualTo((int)RunLootHeatCoolingSummaryErrorCode.ExtractionSummaryMissingOrFailed));

        [Test]
        public void FailedExtractionSummary_ReturnsExtractionFailure()
        {
            var extraction = new RunLootExtractionSummary { RuleResolved = false, DeterministicErrorCode = (int)RunLootExtractionSummaryErrorCode.UnknownRoundingPolicy };
            var result = LootHeatCoolingResolver.Resolve(ValidConfig(), extraction, 20d, 7);
            Assert.That(result.RuleResolved, Is.False);
            Assert.That(result.DeterministicErrorCode, Is.EqualTo((int)RunLootHeatCoolingSummaryErrorCode.ExtractionSummaryMissingOrFailed));
        }

        [Test]
        public void ZeroTradeableValue_ResolvesWithZeroDelta()
        {
            var result = LootHeatCoolingResolver.Resolve(ValidConfig(), ResolvedExtraction(0), 20d, 7);
            Assert.That(result.RuleResolved, Is.True);
            Assert.That(result.AppliedHeatDelta, Is.EqualTo(0d));
        }

        [Test]
        public void PositiveTradeableValue_DeterministicCooling()
        {
            var result = LootHeatCoolingResolver.Resolve(ValidConfig(), ResolvedExtraction(8), 20d, 7);
            Assert.That(result.UnclampedHeatDelta, Is.EqualTo(-4d));
            Assert.That(result.AppliedHeatDelta, Is.EqualTo(-4d));
            Assert.That(result.HeatAfterCooling, Is.EqualTo(16d));
        }

        [Test]
        public void CapApplied_WhenMaxPerRunReached()
        {
            var result = LootHeatCoolingResolver.Resolve(ValidConfig(perValue: 2d, maxPerRun: 5d), ResolvedExtraction(8), 20d, 7);
            Assert.That(result.UnclampedHeatDelta, Is.EqualTo(-16d));
            Assert.That(result.AppliedHeatDelta, Is.EqualTo(-5d));
        }

        [Test]
        public void ZeroCap_ResolvesWithZeroDelta()
        {
            var result = LootHeatCoolingResolver.Resolve(ValidConfig(perValue: 1d, maxPerRun: 0d), ResolvedExtraction(8), 20d, 7);
            Assert.That(result.RuleResolved, Is.True);
            Assert.That(result.AppliedHeatDelta, Is.EqualTo(0d));
        }

        [Test]
        public void InvalidConfig_ReturnsInvalidCoolingConfig()
        {
            var cfg = ValidConfig();
            cfg.LootHeatCoolingRuleSourceId = "";
            Assert.That(LootHeatCoolingResolver.Resolve(cfg, ResolvedExtraction(8), 20d, 7).DeterministicErrorCode, Is.EqualTo((int)RunLootHeatCoolingSummaryErrorCode.InvalidCoolingConfig));
        }

        [Test]
        public void NaNInfinityConfig_ReturnsInvalidCoolingConfig()
        {
            Assert.That(LootHeatCoolingResolver.Resolve(ValidConfig(double.NaN, 1d), ResolvedExtraction(8), 20d, 7).DeterministicErrorCode, Is.EqualTo((int)RunLootHeatCoolingSummaryErrorCode.InvalidCoolingConfig));
            Assert.That(LootHeatCoolingResolver.Resolve(ValidConfig(1d, double.PositiveInfinity), ResolvedExtraction(8), 20d, 7).DeterministicErrorCode, Is.EqualTo((int)RunLootHeatCoolingSummaryErrorCode.InvalidCoolingConfig));
        }

        [Test]
        public void UnsafeNumericResult_ReturnsAggregateOverflow()
        {
            var result = LootHeatCoolingResolver.Resolve(ValidConfig(perValue: double.MaxValue, maxPerRun: double.MaxValue), ResolvedExtraction(int.MaxValue), 20d, 7);
            Assert.That(result.DeterministicErrorCode, Is.EqualTo((int)RunLootHeatCoolingSummaryErrorCode.AggregateOverflow));
        }

        [Test]
        public void Resolver_DoesNotMutateInput()
        {
            var extraction = ResolvedExtraction(8);
            LootHeatCoolingResolver.Resolve(ValidConfig(), extraction, 20d, 7);
            Assert.That(extraction.TotalExtractedTradeableWorldValue, Is.EqualTo(8));
            Assert.That(extraction.RuleResolved, Is.True);
            Assert.That(extraction.DeterministicErrorCode, Is.EqualTo((int)RunLootExtractionSummaryErrorCode.None));
        }
    }
}
#endif
