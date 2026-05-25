using System;
using System.Collections.Generic;

namespace DungeonBuilder.M0
{
    public enum LootRollResolverErrorCode
    {
        None = 0,
        ConfigNull = 1,
        TableIdMissing = 2,
        TableNotFound = 3,
        PoolEmptyNotAllowed = 4,
        ItemNotFound = 5,
        InvalidWeight = 6
    }

    public readonly struct LootRollResolverResult
    {
        public LootRollResolverResult(
            string tableId,
            int seed,
            int rollCount,
            IReadOnlyList<string> generatedItemIds,
            int totalGeneratedWorldValue,
            int totalGeneratedReserveCost,
            int totalGeneratedTradeableWorldValue,
            bool success,
            LootRollResolverErrorCode errorCode)
        {
            this.tableId = tableId;
            this.seed = seed;
            this.rollCount = rollCount;
            this.generatedItemIds = generatedItemIds;
            this.totalGeneratedWorldValue = totalGeneratedWorldValue;
            this.totalGeneratedReserveCost = totalGeneratedReserveCost;
            this.totalGeneratedTradeableWorldValue = totalGeneratedTradeableWorldValue;
            this.success = success;
            this.errorCode = errorCode;
        }

        public readonly string tableId;
        public readonly int seed;
        public readonly int rollCount;
        public readonly IReadOnlyList<string> generatedItemIds;
        public readonly int totalGeneratedWorldValue;
        public readonly int totalGeneratedReserveCost;
        public readonly int totalGeneratedTradeableWorldValue;
        public readonly bool success;
        public readonly LootRollResolverErrorCode errorCode;
    }

    public static class LootRollResolver
    {
        public static LootRollResolverResult Resolve(LootConfig config, string tableId, int seed)
        {
            if (config == null)
            {
                return Failure(tableId, seed, LootRollResolverErrorCode.ConfigNull);
            }

            if (string.IsNullOrEmpty(tableId))
            {
                return Failure(tableId, seed, LootRollResolverErrorCode.TableIdMissing);
            }

            LootTableRecord[] tables = config.tables ?? Array.Empty<LootTableRecord>();
            LootTableRecord table = null;
            for (int i = 0; i < tables.Length; i++)
            {
                LootTableRecord candidate = tables[i];
                if (candidate != null && candidate.id == tableId)
                {
                    table = candidate;
                    break;
                }
            }

            if (table == null)
            {
                return Failure(tableId, seed, LootRollResolverErrorCode.TableNotFound);
            }

            LootTablePoolEntry[] pool = table.pool ?? Array.Empty<LootTablePoolEntry>();
            if (pool.Length == 0)
            {
                if (!table.allowEmptyPool)
                {
                    return Failure(tableId, seed, LootRollResolverErrorCode.PoolEmptyNotAllowed);
                }

                return Success(tableId, seed, 0, Array.Empty<string>(), 0, 0, 0);
            }

            var itemById = new Dictionary<string, LootItemRecord>(StringComparer.Ordinal);
            LootItemRecord[] items = config.items ?? Array.Empty<LootItemRecord>();
            for (int i = 0; i < items.Length; i++)
            {
                LootItemRecord item = items[i];
                if (item == null || string.IsNullOrEmpty(item.id) || itemById.ContainsKey(item.id))
                {
                    continue;
                }

                itemById.Add(item.id, item);
            }

            double totalWeight = 0d;
            for (int i = 0; i < pool.Length; i++)
            {
                LootTablePoolEntry entry = pool[i];
                if (entry == null || entry.weight <= 0d || double.IsNaN(entry.weight) || double.IsInfinity(entry.weight))
                {
                    return Failure(tableId, seed, LootRollResolverErrorCode.InvalidWeight);
                }

                if (string.IsNullOrEmpty(entry.itemId) || !itemById.ContainsKey(entry.itemId))
                {
                    return Failure(tableId, seed, LootRollResolverErrorCode.ItemNotFound);
                }

                totalWeight += entry.weight;
            }

            var random = new Random(CombineSeed(seed, tableId));
            int rollCount = random.Next(table.minRollCount, table.maxRollCount + 1);

            var generatedItemIds = new List<string>(rollCount);
            int totalGeneratedWorldValue = 0;
            int totalGeneratedReserveCost = 0;
            int totalGeneratedTradeableWorldValue = 0;

            for (int i = 0; i < rollCount; i++)
            {
                LootTablePoolEntry selectedEntry = SelectEntry(pool, totalWeight, random.NextDouble());
                LootItemRecord selectedItem = itemById[selectedEntry.itemId];
                generatedItemIds.Add(selectedItem.id);

                totalGeneratedWorldValue += selectedItem.worldValue;
                totalGeneratedReserveCost += selectedItem.reserveCost;
                if (selectedItem.isTradeable)
                {
                    totalGeneratedTradeableWorldValue += selectedItem.worldValue;
                }
            }

            return Success(
                tableId,
                seed,
                rollCount,
                generatedItemIds,
                totalGeneratedWorldValue,
                totalGeneratedReserveCost,
                totalGeneratedTradeableWorldValue);
        }

        private static int CombineSeed(int seed, string tableId)
        {
            unchecked
            {
                int hash = (int)2166136261;
                for (int i = 0; i < tableId.Length; i++)
                {
                    hash ^= tableId[i];
                    hash *= 16777619;
                }

                return seed ^ hash;
            }
        }

        private static LootTablePoolEntry SelectEntry(LootTablePoolEntry[] pool, double totalWeight, double rollUnit)
        {
            double threshold = rollUnit * totalWeight;
            double cumulative = 0d;
            for (int i = 0; i < pool.Length; i++)
            {
                LootTablePoolEntry entry = pool[i];
                cumulative += entry.weight;
                if (threshold <= cumulative)
                {
                    return entry;
                }
            }

            return pool[pool.Length - 1];
        }

        private static LootRollResolverResult Failure(string tableId, int seed, LootRollResolverErrorCode errorCode)
        {
            return new LootRollResolverResult(tableId, seed, 0, Array.Empty<string>(), 0, 0, 0, false, errorCode);
        }

        private static LootRollResolverResult Success(string tableId, int seed, int rollCount, IReadOnlyList<string> generatedItemIds, int worldValue, int reserveCost, int tradeableWorldValue)
        {
            return new LootRollResolverResult(tableId, seed, rollCount, generatedItemIds, worldValue, reserveCost, tradeableWorldValue, true, LootRollResolverErrorCode.None);
        }
    }
}
