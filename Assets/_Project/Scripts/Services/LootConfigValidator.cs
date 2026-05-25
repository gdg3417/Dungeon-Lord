using System;
using System.Collections.Generic;

namespace DungeonBuilder.M0
{
    public static class LootConfigValidator
    {
        public static IReadOnlyList<string> Validate(LootConfig config)
        {
            var errors = new List<string>();
            if (config == null)
            {
                errors.Add("loot.config.null");
                return errors;
            }

            var itemById = new Dictionary<string, LootItemRecord>(StringComparer.Ordinal);
            LootItemRecord[] items = config.items ?? Array.Empty<LootItemRecord>();
            for (int i = 0; i < items.Length; i++)
            {
                LootItemRecord item = items[i];
                string path = $"loot.items[{i}]";
                if (item == null)
                {
                    errors.Add(path + ".null");
                    continue;
                }

                if (string.IsNullOrEmpty(item.id))
                {
                    errors.Add(path + ".id.missing");
                }
                else if (!itemById.TryAdd(item.id, item))
                {
                    errors.Add(path + $".id.duplicate:{item.id}");
                }

                if (!IsValidTier(item.tierId))
                {
                    errors.Add(path + $".tier.invalid:{item.tierId}");
                }

                if (!IsValidRarity(item.rarityId))
                {
                    errors.Add(path + $".rarity.invalid:{item.rarityId}");
                }

                if (string.IsNullOrEmpty(item.categoryId))
                {
                    errors.Add(path + ".categoryId.missing");
                }
                else if (!IsValidCategory(item.categoryId))
                {
                    errors.Add(path + $".categoryId.invalid:{item.categoryId}");
                }

                if (string.IsNullOrEmpty(item.nameKey))
                {
                    errors.Add(path + ".nameKey.missing");
                }

                if (string.IsNullOrEmpty(item.descriptionKey))
                {
                    errors.Add(path + ".descriptionKey.missing");
                }

                if (item.worldValue < 0)
                {
                    errors.Add(path + ".worldValue.negative");
                }

                if (item.reserveCost < 0)
                {
                    errors.Add(path + ".reserveCost.negative");
                }
            }

            var tableById = new HashSet<string>(StringComparer.Ordinal);
            LootTableRecord[] tables = config.tables ?? Array.Empty<LootTableRecord>();
            for (int i = 0; i < tables.Length; i++)
            {
                LootTableRecord table = tables[i];
                string path = $"loot.tables[{i}]";
                if (table == null)
                {
                    errors.Add(path + ".null");
                    continue;
                }

                if (string.IsNullOrEmpty(table.id))
                {
                    errors.Add(path + ".id.missing");
                }
                else if (!tableById.Add(table.id))
                {
                    errors.Add(path + $".id.duplicate:{table.id}");
                }

                if (table.minRollCount <= 0)
                {
                    errors.Add(path + ".minRollCount.invalid");
                }

                if (table.maxRollCount <= 0)
                {
                    errors.Add(path + ".maxRollCount.invalid");
                }

                if (table.maxRollCount < table.minRollCount)
                {
                    errors.Add(path + ".rollCount.range.invalid");
                }

                LootTablePoolEntry[] pool = table.pool ?? Array.Empty<LootTablePoolEntry>();
                if (!table.allowEmptyPool && pool.Length == 0)
                {
                    errors.Add(path + ".pool.empty");
                }

                for (int j = 0; j < pool.Length; j++)
                {
                    LootTablePoolEntry entry = pool[j];
                    string entryPath = path + $".pool[{j}]";
                    if (entry == null)
                    {
                        errors.Add(entryPath + ".null");
                        continue;
                    }

                    if (string.IsNullOrEmpty(entry.itemId) || !itemById.ContainsKey(entry.itemId))
                    {
                        errors.Add(entryPath + $".itemRef.missing:{entry.itemId}");
                    }

                    if (entry.weight <= 0d || double.IsNaN(entry.weight) || double.IsInfinity(entry.weight))
                    {
                        errors.Add(entryPath + ".weight.invalid");
                    }
                }
            }

            return errors;
        }

        private static bool IsValidTier(string tierId)
        {
            return !string.IsNullOrEmpty(tierId) &&
                   (tierId == "loot_tier.wood" ||
                    tierId == "loot_tier.bronze" ||
                    tierId == "loot_tier.iron" ||
                    tierId == "loot_tier.steel");
        }

        private static bool IsValidRarity(string rarityId)
        {
            return !string.IsNullOrEmpty(rarityId) &&
                   (rarityId == "loot_rarity.common" ||
                    rarityId == "loot_rarity.uncommon" ||
                    rarityId == "loot_rarity.rare" ||
                    rarityId == "loot_rarity.epic");
        }

        private static bool IsValidCategory(string categoryId)
        {
            return categoryId == "loot_category.material" ||
                   categoryId == "loot_category.artifact" ||
                   categoryId == "loot_category.consumable" ||
                   categoryId == "loot_category.reagent";
        }
    }
}
