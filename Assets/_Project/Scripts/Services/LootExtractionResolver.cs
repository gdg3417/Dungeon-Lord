using System;
using System.Collections.Generic;

namespace DungeonBuilder.M0
{
    public static class LootExtractionResolver
    {
        private const string LootExtractionRoundFloorPolicyId = "loot_extraction.round_floor";

        public static RunLootExtractionSummary Resolve(
            LootConfig lootConfig,
            RunLootSummary lootSummary,
            RunSurvivalSummary survivalSummary,
            int deterministicSeed,
            string roundingPolicyId,
            string ruleSourceId)
        {
            string[] generatedItemIds = lootSummary?.GeneratedItemIds ?? Array.Empty<string>();
            int generatedItemCount = generatedItemIds.Length;

            if (lootSummary == null || !lootSummary.ResolverSuccess)
            {
                return BuildFailure(ruleSourceId, deterministicSeed, RunLootExtractionSummaryErrorCode.LootSummaryMissingOrFailed, generatedItemCount, generatedItemIds);
            }

            if (survivalSummary == null || !survivalSummary.RuleResolved || survivalSummary.DeterministicErrorCode != (int)RunSurvivalSummaryErrorCode.None)
            {
                return BuildFailure(ruleSourceId, deterministicSeed, RunLootExtractionSummaryErrorCode.SurvivalSummaryMissingOrFailed, generatedItemCount, generatedItemIds);
            }

            double survivorRatio = survivalSummary.SurvivorRatio;
            if (survivorRatio < 0d || survivorRatio > 1d || double.IsNaN(survivorRatio) || double.IsInfinity(survivorRatio))
            {
                return BuildFailure(ruleSourceId, deterministicSeed, RunLootExtractionSummaryErrorCode.InvalidSurvivorRatio, generatedItemCount, generatedItemIds);
            }

            int extractedItemCount = ResolveExtractedItemCount(roundingPolicyId, generatedItemCount, survivorRatio);
            if (extractedItemCount < 0)
            {
                return BuildFailure(ruleSourceId, deterministicSeed, RunLootExtractionSummaryErrorCode.UnknownRoundingPolicy, generatedItemCount, generatedItemIds);
            }

            if (survivalSummary.SurvivorCount <= 0 || survivalSummary.PartySize <= 0 || extractedItemCount <= 0)
            {
                return new RunLootExtractionSummary
                {
                    RuleSourceId = ruleSourceId,
                    DeterministicSeed = deterministicSeed,
                    RuleResolved = true,
                    DeterministicErrorCode = (int)RunLootExtractionSummaryErrorCode.None,
                    SurvivorRatioUsed = survivorRatio,
                    GeneratedItemCount = generatedItemCount,
                    ExtractedItemIds = Array.Empty<string>(),
                    LostItemIds = Clone(generatedItemIds)
                };
            }

            string[] extracted = SelectExtractedItems(deterministicSeed, generatedItemIds, extractedItemCount);
            string[] lost = ComputeLostItems(generatedItemIds, extracted);
            RunLootExtractionSummaryErrorCode totalsCode = CalculateTotals(lootConfig, extracted, out int worldValue, out int reserveCost, out int tradeableWorldValue);
            if (totalsCode != RunLootExtractionSummaryErrorCode.None)
            {
                return BuildFailure(ruleSourceId, deterministicSeed, totalsCode, generatedItemCount, generatedItemIds);
            }

            return new RunLootExtractionSummary
            {
                RuleSourceId = ruleSourceId,
                DeterministicSeed = deterministicSeed,
                RuleResolved = true,
                DeterministicErrorCode = (int)RunLootExtractionSummaryErrorCode.None,
                SurvivorRatioUsed = survivorRatio,
                GeneratedItemCount = generatedItemCount,
                ExtractedItemIds = extracted,
                LostItemIds = lost,
                TotalExtractedWorldValue = worldValue,
                TotalExtractedReserveCost = reserveCost,
                TotalExtractedTradeableWorldValue = tradeableWorldValue
            };
        }

        private static RunLootExtractionSummary BuildFailure(string ruleSourceId, int seed, RunLootExtractionSummaryErrorCode errorCode, int generatedCount, string[] generatedItemIds)
        {
            return new RunLootExtractionSummary
            {
                RuleSourceId = ruleSourceId,
                DeterministicSeed = seed,
                RuleResolved = false,
                DeterministicErrorCode = (int)errorCode,
                GeneratedItemCount = generatedCount,
                ExtractedItemIds = Array.Empty<string>(),
                LostItemIds = Clone(generatedItemIds)
            };
        }

        private static int ResolveExtractedItemCount(string roundingPolicyId, int generatedCount, double survivorRatio)
        {
            if (string.Equals(roundingPolicyId, LootExtractionRoundFloorPolicyId, StringComparison.Ordinal))
            {
                return Math.Max(0, Math.Min(generatedCount, (int)Math.Floor(generatedCount * survivorRatio)));
            }

            return -1;
        }

        private static RunLootExtractionSummaryErrorCode CalculateTotals(LootConfig lootConfig, string[] extracted, out int worldValue, out int reserveCost, out int tradeableWorldValue)
        {
            worldValue = 0;
            reserveCost = 0;
            tradeableWorldValue = 0;
            var valuesById = new Dictionary<string, LootItemRecord>(StringComparer.Ordinal);
            LootItemRecord[] items = lootConfig?.items ?? Array.Empty<LootItemRecord>();
            for (int i = 0; i < items.Length; i++)
            {
                LootItemRecord item = items[i];
                if (item == null || string.IsNullOrEmpty(item.id) || valuesById.ContainsKey(item.id))
                {
                    continue;
                }
                valuesById[item.id] = item;
            }

            for (int i = 0; i < extracted.Length; i++)
            {
                string itemId = extracted[i];
                if (string.IsNullOrEmpty(itemId) || !valuesById.TryGetValue(itemId, out LootItemRecord item))
                {
                    return RunLootExtractionSummaryErrorCode.ItemValueLookupFailed;
                }

                try
                {
                    checked
                    {
                        worldValue += item.worldValue;
                        reserveCost += item.reserveCost;
                        if (item.isTradeable)
                        {
                            tradeableWorldValue += item.worldValue;
                        }
                    }
                }
                catch (OverflowException)
                {
                    return RunLootExtractionSummaryErrorCode.AggregateOverflow;
                }
            }

            return RunLootExtractionSummaryErrorCode.None;
        }

        private static string[] SelectExtractedItems(int seed, string[] generatedItemIds, int extractedItemCount)
        {
            if (extractedItemCount >= generatedItemIds.Length)
            {
                return Clone(generatedItemIds);
            }

            var ranked = new List<(int index, int score)>(generatedItemIds.Length);
            for (int i = 0; i < generatedItemIds.Length; i++)
            {
                ranked.Add((i, ComputeItemRank(seed, generatedItemIds[i], i)));
            }
            ranked.Sort((a, b) => a.score != b.score ? a.score.CompareTo(b.score) : a.index.CompareTo(b.index));

            var selected = new List<(int index, string id)>(extractedItemCount);
            for (int i = 0; i < extractedItemCount; i++)
            {
                int idx = ranked[i].index;
                selected.Add((idx, generatedItemIds[idx]));
            }
            selected.Sort((a, b) => a.index.CompareTo(b.index));

            string[] extracted = new string[selected.Count];
            for (int i = 0; i < selected.Count; i++) extracted[i] = selected[i].id;
            return extracted;
        }

        private static string[] ComputeLostItems(string[] generated, string[] extracted)
        {
            var extractedCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < extracted.Length; i++)
            {
                string id = extracted[i] ?? string.Empty;
                extractedCounts[id] = extractedCounts.TryGetValue(id, out int c) ? c + 1 : 1;
            }

            var lost = new List<string>();
            for (int i = 0; i < generated.Length; i++)
            {
                string id = generated[i] ?? string.Empty;
                if (extractedCounts.TryGetValue(id, out int c) && c > 0)
                {
                    extractedCounts[id] = c - 1;
                    continue;
                }
                lost.Add(generated[i]);
            }
            return lost.ToArray();
        }

        private static int ComputeItemRank(int seed, string itemId, int itemIndex)
        {
            unchecked
            {
                int hash = seed;
                string id = itemId ?? string.Empty;
                for (int i = 0; i < id.Length; i++) hash = (hash * 31) + id[i];
                return (hash * 31) + itemIndex;
            }
        }

        private static string[] Clone(string[] ids) => ids != null ? new List<string>(ids).ToArray() : Array.Empty<string>();
    }
}
