using System.Linq;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class LootRollResolverTests
    {
        private const string TableId = "loot.table.run.basic";

        [Test]
        public void Resolve_SameSeedAndTable_ReturnsSameGeneratedItems()
        {
            LootConfig config = BuildConfig();
            var first = LootRollResolver.Resolve(config, TableId, 1001);
            var second = LootRollResolver.Resolve(config, TableId, 1001);

            Assert.That(first.success, Is.True);
            Assert.That(second.success, Is.True);
            Assert.That(first.generatedItemIds, Is.EqualTo(second.generatedItemIds));
            Assert.That(first.rollCount, Is.EqualTo(second.rollCount));
        }

        [Test]
        public void Resolve_DifferentSeed_CanReturnDifferentGeneratedItems()
        {
            LootConfig config = BuildConfig();
            var baseline = LootRollResolver.Resolve(config, TableId, 1001);

            Assert.That(baseline.success, Is.True);

            bool foundDifferent = false;
            for (int seed = 1002; seed <= 1065; seed++)
            {
                var candidate = LootRollResolver.Resolve(config, TableId, seed);
                Assert.That(candidate.success, Is.True);

                bool sameItems = baseline.generatedItemIds.SequenceEqual(candidate.generatedItemIds);
                bool sameRollCount = baseline.rollCount == candidate.rollCount;
                if (!sameItems || !sameRollCount)
                {
                    foundDifferent = true;
                    break;
                }
            }

            Assert.That(foundDifferent, Is.True);
        }

        [Test]
        public void Resolve_Weighting_IsRespected()
        {
            LootConfig config = BuildConfig();
            config.tables[0].minRollCount = 50;
            config.tables[0].maxRollCount = 50;
            config.tables[0].pool = new[]
            {
                new LootTablePoolEntry { itemId = "loot.item.scrap.iron", weight = 10d },
                new LootTablePoolEntry { itemId = "loot.item.relic.bronze", weight = 1d }
            };

            var result = LootRollResolver.Resolve(config, TableId, 7);
            int scrapCount = result.generatedItemIds.Count(id => id == "loot.item.scrap.iron");
            int relicCount = result.generatedItemIds.Count(id => id == "loot.item.relic.bronze");

            Assert.That(result.success, Is.True);
            Assert.That(scrapCount, Is.GreaterThan(relicCount));
        }

        [Test]
        public void Resolve_RollCount_WithinConfiguredRange()
        {
            LootConfig config = BuildConfig();
            config.tables[0].minRollCount = 2;
            config.tables[0].maxRollCount = 4;

            var result = LootRollResolver.Resolve(config, TableId, 99);
            Assert.That(result.rollCount, Is.InRange(2, 4));
        }


        [Test]
        public void Resolve_InvalidRollCount_MinLessThanOrEqualZero_ReturnsFailure()
        {
            LootConfig config = BuildConfig();
            config.tables[0].minRollCount = 0;
            var result = LootRollResolver.Resolve(config, TableId, 42);
            Assert.That(result.success, Is.False);
            Assert.That(result.errorCode, Is.EqualTo(LootRollResolverErrorCode.InvalidRollCountRange));
        }

        [Test]
        public void Resolve_InvalidRollCount_MaxLessThanOrEqualZero_ReturnsFailure()
        {
            LootConfig config = BuildConfig();
            config.tables[0].maxRollCount = 0;
            var result = LootRollResolver.Resolve(config, TableId, 42);
            Assert.That(result.success, Is.False);
            Assert.That(result.errorCode, Is.EqualTo(LootRollResolverErrorCode.InvalidRollCountRange));
        }

        [Test]
        public void Resolve_InvalidRollCount_MaxLessThanMin_ReturnsFailure()
        {
            LootConfig config = BuildConfig();
            config.tables[0].minRollCount = 3;
            config.tables[0].maxRollCount = 2;
            var result = LootRollResolver.Resolve(config, TableId, 42);
            Assert.That(result.success, Is.False);
            Assert.That(result.errorCode, Is.EqualTo(LootRollResolverErrorCode.InvalidRollCountRange));
        }

        [Test]
        public void Resolve_MissingTableId_ReturnsDeterministicFailure()
        {
            LootConfig config = BuildConfig();
            var result = LootRollResolver.Resolve(config, "missing", 42);
            Assert.That(result.success, Is.False);
            Assert.That(result.errorCode, Is.EqualTo(LootRollResolverErrorCode.TableNotFound));
        }

        [Test]
        public void Resolve_EmptyPool_Disallowed_ReturnsDeterministicFailure()
        {
            LootConfig config = BuildConfig();
            config.tables[0].allowEmptyPool = false;
            config.tables[0].pool = new LootTablePoolEntry[0];
            var result = LootRollResolver.Resolve(config, TableId, 42);
            Assert.That(result.success, Is.False);
            Assert.That(result.errorCode, Is.EqualTo(LootRollResolverErrorCode.PoolEmptyNotAllowed));
        }

        [Test]
        public void Resolve_EmptyPool_Allowed_ReturnsSuccessWithZeroItems()
        {
            LootConfig config = BuildConfig();
            config.tables[0].allowEmptyPool = true;
            config.tables[0].pool = new LootTablePoolEntry[0];
            var result = LootRollResolver.Resolve(config, TableId, 42);
            Assert.That(result.success, Is.True);
            Assert.That(result.generatedItemIds, Is.Empty);
            Assert.That(result.rollCount, Is.EqualTo(0));
        }

        [Test]
        public void Resolve_Totals_AreSummedCorrectly()
        {
            LootConfig config = BuildConfig();
            config.tables[0].minRollCount = 2;
            config.tables[0].maxRollCount = 2;
            config.tables[0].pool = new[]
            {
                new LootTablePoolEntry { itemId = "loot.item.scrap.iron", weight = 1d }
            };

            var result = LootRollResolver.Resolve(config, TableId, 123);

            Assert.That(result.success, Is.True);
            Assert.That(result.totalGeneratedWorldValue, Is.EqualTo(6));
            Assert.That(result.totalGeneratedReserveCost, Is.EqualTo(2));
            Assert.That(result.totalGeneratedTradeableWorldValue, Is.EqualTo(6));
        }

        [Test]
        public void Resolve_DoesNotMutateInputData()
        {
            LootConfig config = BuildConfig();
            LootConfig snapshot = BuildConfig();

            _ = LootRollResolver.Resolve(config, TableId, 500);

            Assert.That(config.items[0].id, Is.EqualTo(snapshot.items[0].id));
            Assert.That(config.items[0].worldValue, Is.EqualTo(snapshot.items[0].worldValue));
            Assert.That(config.items[1].id, Is.EqualTo(snapshot.items[1].id));
            Assert.That(config.tables[0].id, Is.EqualTo(snapshot.tables[0].id));
            Assert.That(config.tables[0].minRollCount, Is.EqualTo(snapshot.tables[0].minRollCount));
            Assert.That(config.tables[0].maxRollCount, Is.EqualTo(snapshot.tables[0].maxRollCount));
            Assert.That(config.tables[0].pool[0].itemId, Is.EqualTo(snapshot.tables[0].pool[0].itemId));
            Assert.That(config.tables[0].pool[0].weight, Is.EqualTo(snapshot.tables[0].pool[0].weight));
        }

        private static LootConfig BuildConfig()
        {
            return new LootConfig
            {
                schema = "loot_config",
                schemaVersion = 1,
                items = new[]
                {
                    new LootItemRecord
                    {
                        id = "loot.item.scrap.iron",
                        tierId = "loot_tier.iron",
                        rarityId = "loot_rarity.common",
                        categoryId = "loot_category.material",
                        worldValue = 3,
                        reserveCost = 1,
                        nameKey = "loot.item.scrap.iron.name",
                        descriptionKey = "loot.item.scrap.iron.desc",
                        isTradeable = true
                    },
                    new LootItemRecord
                    {
                        id = "loot.item.relic.bronze",
                        tierId = "loot_tier.bronze",
                        rarityId = "loot_rarity.uncommon",
                        categoryId = "loot_category.artifact",
                        worldValue = 7,
                        reserveCost = 2,
                        nameKey = "loot.item.relic.bronze.name",
                        descriptionKey = "loot.item.relic.bronze.desc",
                        isTradeable = false
                    }
                },
                tables = new[]
                {
                    new LootTableRecord
                    {
                        id = TableId,
                        minRollCount = 3,
                        maxRollCount = 3,
                        allowEmptyPool = false,
                        pool = new[]
                        {
                            new LootTablePoolEntry { itemId = "loot.item.scrap.iron", weight = 1d },
                            new LootTablePoolEntry { itemId = "loot.item.relic.bronze", weight = 1d }
                        }
                    }
                }
            };
        }
    }
}
