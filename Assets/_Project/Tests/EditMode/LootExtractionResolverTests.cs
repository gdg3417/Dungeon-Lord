using DungeonBuilder.M0;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class LootExtractionResolverTests
    {
        private static LootConfig BuildLootConfig()
        {
            return new LootConfig
            {
                items = new[]
                {
                    new LootItemRecord { id = "loot.item.scrap.iron", worldValue = 3, reserveCost = 1, isTradeable = true },
                    new LootItemRecord { id = "loot.item.relic.bronze", worldValue = 8, reserveCost = 2, isTradeable = false }
                }
            };
        }

        private static RunLootSummary SuccessLootSummary(params string[] generatedIds)
        {
            return new RunLootSummary
            {
                ResolverSuccess = true,
                GeneratedItemIds = generatedIds
            };
        }

        private static RunSurvivalSummary SuccessSurvival(double ratio, int partySize = 4, int survivors = 2)
        {
            return new RunSurvivalSummary
            {
                RuleResolved = true,
                DeterministicErrorCode = (int)RunSurvivalSummaryErrorCode.None,
                SurvivorRatio = ratio,
                PartySize = partySize,
                SurvivorCount = survivors
            };
        }

        [Test]
        public void Resolve_UnknownRoundingPolicy_ReturnsFailure()
        {
            RunLootExtractionSummary summary = LootExtractionResolver.Resolve(
                BuildLootConfig(),
                SuccessLootSummary("loot.item.scrap.iron", "loot.item.relic.bronze"),
                SuccessSurvival(0.5d),
                123,
                "loot_extraction.round_unknown",
                "run.loot_extraction.rule.v1");

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)RunLootExtractionSummaryErrorCode.UnknownRoundingPolicy));
        }

        [Test]
        public void Resolve_PartialRatio_SelectsDeterministicSubset()
        {
            RunLootExtractionSummary summary = LootExtractionResolver.Resolve(
                BuildLootConfig(),
                SuccessLootSummary("loot.item.scrap.iron", "loot.item.relic.bronze"),
                SuccessSurvival(0.5d),
                123,
                "loot_extraction.round_floor",
                "run.loot_extraction.rule.v1");

            Assert.That(summary.ExtractedItemIds, Is.EqualTo(new[] { "loot.item.scrap.iron" }));
            Assert.That(summary.LostItemIds, Is.EqualTo(new[] { "loot.item.relic.bronze" }));
        }

        [Test]
        public void Resolve_ItemValueLookupFailed_WhenExtractedItemMissingFromLootConfig()
        {
            RunLootExtractionSummary summary = LootExtractionResolver.Resolve(
                BuildLootConfig(),
                SuccessLootSummary("loot.item.unknown"),
                SuccessSurvival(1d, 1, 1),
                55,
                "loot_extraction.round_floor",
                "run.loot_extraction.rule.v1");

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)RunLootExtractionSummaryErrorCode.ItemValueLookupFailed));
        }

        [Test]
        public void Resolve_AggregateOverflow_ReturnsFailure()
        {
            LootConfig config = new LootConfig
            {
                items = new[] { new LootItemRecord { id = "loot.item.overflow", worldValue = int.MaxValue, reserveCost = int.MaxValue, isTradeable = true } }
            };
            RunLootExtractionSummary summary = LootExtractionResolver.Resolve(
                config,
                SuccessLootSummary("loot.item.overflow", "loot.item.overflow"),
                SuccessSurvival(1d, 2, 2),
                33,
                "loot_extraction.round_floor",
                "run.loot_extraction.rule.v1");

            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)RunLootExtractionSummaryErrorCode.AggregateOverflow));
        }

        [Test]
        public void Resolve_MissingOrFailedPrerequisites_ReturnDeterministicFailure()
        {
            RunLootExtractionSummary missingLoot = LootExtractionResolver.Resolve(BuildLootConfig(), null, SuccessSurvival(1d), 1, "loot_extraction.round_floor", "rule");
            RunLootExtractionSummary failedLoot = LootExtractionResolver.Resolve(BuildLootConfig(), new RunLootSummary { ResolverSuccess = false }, SuccessSurvival(1d), 1, "loot_extraction.round_floor", "rule");
            RunLootExtractionSummary missingSurvival = LootExtractionResolver.Resolve(BuildLootConfig(), SuccessLootSummary("loot.item.scrap.iron"), null, 1, "loot_extraction.round_floor", "rule");
            RunLootExtractionSummary failedSurvival = LootExtractionResolver.Resolve(BuildLootConfig(), SuccessLootSummary("loot.item.scrap.iron"), new RunSurvivalSummary { RuleResolved = false, DeterministicErrorCode = (int)RunSurvivalSummaryErrorCode.InvalidSurvivorRatio }, 1, "loot_extraction.round_floor", "rule");

            Assert.That(missingLoot.DeterministicErrorCode, Is.EqualTo((int)RunLootExtractionSummaryErrorCode.LootSummaryMissingOrFailed));
            Assert.That(failedLoot.DeterministicErrorCode, Is.EqualTo((int)RunLootExtractionSummaryErrorCode.LootSummaryMissingOrFailed));
            Assert.That(missingSurvival.DeterministicErrorCode, Is.EqualTo((int)RunLootExtractionSummaryErrorCode.SurvivalSummaryMissingOrFailed));
            Assert.That(failedSurvival.DeterministicErrorCode, Is.EqualTo((int)RunLootExtractionSummaryErrorCode.SurvivalSummaryMissingOrFailed));
        }
    }
}
