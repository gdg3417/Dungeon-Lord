using System.Linq;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class LootConfigValidatorTests
    {
        [Test]
        public void Validate_ValidConfig_ReturnsNoErrors()
        {
            LootConfig config = BuildValidConfig();
            var errors = LootConfigValidator.Validate(config);
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void Validate_DuplicateLootItemIds_FailsValidation()
        {
            LootConfig config = BuildValidConfig();
            config.items[1].id = config.items[0].id;
            var errors = LootConfigValidator.Validate(config);
            Assert.That(errors, Has.Some.Contains("id.duplicate"));
        }

        [Test]
        public void Validate_MissingLootItemReference_FailsValidation()
        {
            LootConfig config = BuildValidConfig();
            config.tables[0].pool[0].itemId = "loot.item.missing";
            var errors = LootConfigValidator.Validate(config);
            Assert.That(errors, Has.Some.Contains("itemRef.missing"));
        }

        [Test]
        public void Validate_InvalidTierAndRarity_FailsValidation()
        {
            LootConfig config = BuildValidConfig();
            config.items[0].tierId = "loot_tier.invalid";
            config.items[0].rarityId = "loot_rarity.invalid";
            var errors = LootConfigValidator.Validate(config);
            Assert.That(errors, Has.Some.Contains("tier.invalid"));
            Assert.That(errors, Has.Some.Contains("rarity.invalid"));
        }

        [Test]
        public void Validate_NegativeValueFields_FailValidation()
        {
            LootConfig config = BuildValidConfig();
            config.items[0].worldValue = -1;
            config.items[0].reserveCost = -10;
            var errors = LootConfigValidator.Validate(config);
            Assert.That(errors, Has.Some.Contains("worldValue.negative"));
            Assert.That(errors, Has.Some.Contains("reserveCost.negative"));
        }

        [Test]
        public void Validate_InvalidRollCountsAndWeight_FailValidation()
        {
            LootConfig config = BuildValidConfig();
            config.tables[0].minRollCount = 0;
            config.tables[0].maxRollCount = -1;
            config.tables[0].pool[0].weight = 0d;
            var errors = LootConfigValidator.Validate(config);
            Assert.That(errors, Has.Some.Contains("minRollCount.invalid"));
            Assert.That(errors, Has.Some.Contains("maxRollCount.invalid"));
            Assert.That(errors, Has.Some.Contains("weight.invalid"));
        }

        [Test]
        public void Validate_EmptyItemPool_Disallowed_FailsValidation()
        {
            LootConfig config = BuildValidConfig();
            config.tables[0].allowEmptyPool = false;
            config.tables[0].pool = new LootTablePoolEntry[0];
            var errors = LootConfigValidator.Validate(config);
            Assert.That(errors, Has.Some.Contains("pool.empty"));
        }

        [Test]
        public void Validate_EmptyItemPool_Allowed_PassesValidation()
        {
            LootConfig config = BuildValidConfig();
            config.tables[0].allowEmptyPool = true;
            config.tables[0].pool = new LootTablePoolEntry[0];
            var errors = LootConfigValidator.Validate(config);
            Assert.That(errors.Any(e => e.Contains("pool.empty")), Is.False);
        }

        [Test]
        public void Validate_ErrorOrder_IsDeterministic()
        {
            LootConfig config = BuildValidConfig();
            config.items = new[]
            {
                new LootItemRecord { id = "loot.item.a", tierId = "bad", rarityId = "bad", worldValue = -1, reserveCost = -1 }
            };
            config.tables[0].pool = new[]
            {
                new LootTablePoolEntry { itemId = "missing", weight = 0d }
            };

            var errors = LootConfigValidator.Validate(config).ToArray();
            Assert.That(errors[0], Is.EqualTo("loot.items[0].tier.invalid:bad"));
            Assert.That(errors[1], Is.EqualTo("loot.items[0].rarity.invalid:bad"));
            Assert.That(errors[2], Is.EqualTo("loot.items[0].worldValue.negative"));
            Assert.That(errors[3], Is.EqualTo("loot.items[0].reserveCost.negative"));
            Assert.That(errors[4], Is.EqualTo("loot.tables[0].pool[0].itemRef.missing:missing"));
            Assert.That(errors[5], Is.EqualTo("loot.tables[0].pool[0].weight.invalid"));
        }

        private static LootConfig BuildValidConfig()
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
                        worldValue = 3,
                        reserveCost = 1,
                        nameKey = "loot.item.scrap.iron.name",
                        descriptionKey = "loot.item.scrap.iron.desc"
                    },
                    new LootItemRecord
                    {
                        id = "loot.item.relic.bronze",
                        tierId = "loot_tier.bronze",
                        rarityId = "loot_rarity.uncommon",
                        worldValue = 7,
                        reserveCost = 2,
                        nameKey = "loot.item.relic.bronze.name",
                        descriptionKey = "loot.item.relic.bronze.desc"
                    }
                },
                tables = new[]
                {
                    new LootTableRecord
                    {
                        id = "loot.table.run.basic",
                        minRollCount = 1,
                        maxRollCount = 2,
                        allowEmptyPool = false,
                        pool = new[]
                        {
                            new LootTablePoolEntry
                            {
                                itemId = "loot.item.scrap.iron",
                                weight = 1d
                            }
                        }
                    }
                }
            };
        }
    }
}
