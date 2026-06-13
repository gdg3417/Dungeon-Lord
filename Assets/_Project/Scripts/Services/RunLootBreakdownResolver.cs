using System;
using System.Collections.Generic;

namespace DungeonBuilder.M0
{
    public static class RunLootBreakdownResolver
    {
        public static RunLootDropRecord[] Resolve(LootConfig lootConfig, RunLootExtractionSummary extractionSummary)
        {
            if (lootConfig == null || extractionSummary == null || !extractionSummary.RuleResolved || extractionSummary.ExtractedItemIds == null || extractionSummary.ExtractedItemIds.Length == 0)
            {
                return Array.Empty<RunLootDropRecord>();
            }

            var itemsById = new Dictionary<string, LootItemRecord>(StringComparer.Ordinal);
            LootItemRecord[] items = lootConfig.items ?? Array.Empty<LootItemRecord>();
            for (int i = 0; i < items.Length; i++)
            {
                LootItemRecord item = items[i];
                if (item != null && !string.IsNullOrEmpty(item.id) && !itemsById.ContainsKey(item.id))
                {
                    itemsById.Add(item.id, item);
                }
            }

            var recordsById = new Dictionary<string, RunLootDropRecord>(StringComparer.Ordinal);
            var orderedIds = new List<string>();
            for (int i = 0; i < extractionSummary.ExtractedItemIds.Length; i++)
            {
                string lootId = extractionSummary.ExtractedItemIds[i];
                if (string.IsNullOrEmpty(lootId) || !itemsById.TryGetValue(lootId, out LootItemRecord item) || string.IsNullOrEmpty(item.nameKey))
                {
                    continue;
                }

                if (!recordsById.TryGetValue(lootId, out RunLootDropRecord record))
                {
                    record = new RunLootDropRecord
                    {
                        LootId = lootId,
                        NameKey = item.nameKey,
                        Quantity = 0,
                        TotalWorldValue = 0,
                        TotalTradeableWorldValue = 0
                    };
                    recordsById.Add(lootId, record);
                    orderedIds.Add(lootId);
                }

                record.Quantity++;
                record.TotalWorldValue = SafeAddCapped(record.TotalWorldValue, Math.Max(0, item.worldValue), Math.Max(0, extractionSummary.TotalExtractedWorldValue));
                if (item.isTradeable)
                {
                    record.TotalTradeableWorldValue = SafeAddCapped(record.TotalTradeableWorldValue, Math.Max(0, item.worldValue), Math.Max(0, extractionSummary.TotalExtractedTradeableWorldValue));
                }
            }

            var results = new List<RunLootDropRecord>(orderedIds.Count);
            int worldRemaining = Math.Max(0, extractionSummary.TotalExtractedWorldValue);
            int tradeableRemaining = Math.Max(0, extractionSummary.TotalExtractedTradeableWorldValue);
            for (int i = 0; i < orderedIds.Count; i++)
            {
                RunLootDropRecord source = recordsById[orderedIds[i]];
                int world = Math.Min(source.TotalWorldValue, worldRemaining);
                int tradeable = Math.Min(source.TotalTradeableWorldValue, tradeableRemaining);
                worldRemaining -= world;
                tradeableRemaining -= tradeable;
                results.Add(new RunLootDropRecord
                {
                    LootId = source.LootId,
                    NameKey = source.NameKey,
                    Quantity = source.Quantity,
                    TotalWorldValue = world,
                    TotalTradeableWorldValue = tradeable
                });
            }

            return results.ToArray();
        }

        private static int SafeAddCapped(int current, int add, int cap)
        {
            if (current >= cap || add <= 0)
            {
                return current;
            }

            return add > cap - current ? cap : current + add;
        }
    }
}
