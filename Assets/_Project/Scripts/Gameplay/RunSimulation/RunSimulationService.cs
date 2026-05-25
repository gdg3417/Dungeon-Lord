using System;
using System.Collections.Generic;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public sealed class RunSimulationService
    {
        private const string LootExtractionRoundFloorPolicyId = "loot_extraction.round_floor";
        private readonly RunSimulationConfig _config;
        private readonly LootConfig _lootConfig;
        private readonly string _lootTableId;
        public RunSimulationConfig Config => _config;

        public RunSimulationService(RunSimulationConfig config, LootConfig lootConfig = null, string lootTableId = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _lootConfig = lootConfig;
            _lootTableId = !string.IsNullOrEmpty(lootTableId) ? lootTableId : _config.LootTableId;
        }

        public RunOutcomeRecord SimulateOnce(StructureRuntimeState runtime, long tickStarted, int runSequence)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));

            double baseChance = _config.BaseSuccessChance;
            double heatPenaltyApplied = runtime.Heat * _config.HeatPenaltyPerPoint;
            double manaBonusApplied = runtime.ManaReserve * _config.ManaReserveBonusPerPoint;
            double crisisPenaltyApplied = runtime.IsHeatCrisisActive ? _config.CrisisFailurePenalty : 0d;

            double unclampedChance = baseChance - heatPenaltyApplied + manaBonusApplied - crisisPenaltyApplied;
            double finalChance = Math.Max(0d, Math.Min(1d, unclampedChance));
            double successThreshold = _config.SuccessThreshold;
            bool success = finalChance >= successThreshold;

            int score = success
                ? _config.BaseScoreOnSuccess + (int)Math.Round(runtime.ManaReserve * _config.ScorePerManaPoint)
                : 0;

            string reasonKey = success
                ? "run.reason.success"
                : (runtime.IsHeatCrisisActive ? "run.reason.crisis_failure" : "run.reason.failed_threshold");
            string[] feedbackTagKeys = BuildFeedbackTagKeys(runtime, success);

            RunLootSummary lootSummary = BuildLootSummary(runSequence, tickStarted);
            RunSurvivalSummary survivalSummary = BuildSurvivalSummary(runSequence, tickStarted, success);
            return new RunOutcomeRecord
            {
                RunId = $"run-{runSequence}",
                TickStarted = tickStarted,
                Success = success,
                Score = score,
                ReasonKey = reasonKey,
                HeatAtStart = runtime.Heat,
                ManaAtStart = runtime.ManaReserve,
                CrisisActiveAtStart = runtime.IsHeatCrisisActive,
                HasBreakdown = true,
                BaseChance = baseChance,
                HeatPenaltyApplied = heatPenaltyApplied,
                ManaBonusApplied = manaBonusApplied,
                CrisisPenaltyApplied = crisisPenaltyApplied,
                FinalChance = finalChance,
                SuccessThresholdUsed = successThreshold,
                FeedbackTagKeys = feedbackTagKeys,
                LootSummary = lootSummary,
                SurvivalSummary = survivalSummary,
                LootExtractionSummary = BuildLootExtractionSummary(runSequence, tickStarted, lootSummary, survivalSummary)
            };
        }

        private RunSurvivalSummary BuildSurvivalSummary(int runSequence, long tickStarted, bool success)
        {
            int seed = ComputeResolverSeed(runSequence, tickStarted);
            int minPartySize = _config.MinPartySize;
            int maxPartySize = _config.MaxPartySize;
            int maxAllowedPartySize = _config.MaxAllowedPartySize;
            if (minPartySize < 1 || maxPartySize < minPartySize || maxAllowedPartySize < 1 || maxPartySize > maxAllowedPartySize)
            {
                return new RunSurvivalSummary
                {
                    RuleResolved = false,
                    DeterministicErrorCode = (int)RunSurvivalSummaryErrorCode.InvalidPartySizeRange,
                    DeterministicSeed = seed,
                    RuleSourceId = "run.survival.rule.v1",
                    SuccessAtResolution = success
                };
            }

            int partySizeRange = maxPartySize - minPartySize + 1;
            int partySizeOffset = Math.Abs(seed % partySizeRange);
            int partySize = minPartySize + partySizeOffset;
            double ratio = success ? _config.SuccessSurvivorRatio : _config.FailureSurvivorRatio;
            if (ratio < 0d || ratio > 1d)
            {
                return new RunSurvivalSummary
                {
                    RuleResolved = false,
                    DeterministicErrorCode = (int)RunSurvivalSummaryErrorCode.InvalidSurvivorRatio,
                    DeterministicSeed = seed,
                    RuleSourceId = "run.survival.rule.v1",
                    SuccessAtResolution = success
                };
            }

            int survivorCount = (int)Math.Round(partySize * ratio);
            survivorCount = Math.Max(0, Math.Min(partySize, survivorCount));

            return new RunSurvivalSummary
            {
                PartySize = partySize,
                SurvivorCount = survivorCount,
                DeathCount = partySize - survivorCount,
                SurvivorRatio = partySize > 0 ? (double)survivorCount / partySize : 0d,
                DeterministicSeed = seed,
                RuleResolved = true,
                DeterministicErrorCode = (int)RunSurvivalSummaryErrorCode.None,
                RuleSourceId = "run.survival.rule.v1",
                SuccessAtResolution = success
            };
        }

        private RunLootSummary BuildLootSummary(int runSequence, long tickStarted)
        {
            if (string.IsNullOrEmpty(_lootTableId))
            {
                return null;
            }

            int resolverSeed = ComputeResolverSeed(runSequence, tickStarted);
            LootRollResolverResult result = LootRollResolver.Resolve(_lootConfig, _lootTableId, resolverSeed);

            return new RunLootSummary
            {
                LootTableId = _lootTableId,
                ResolverSeed = resolverSeed,
                ResolverSuccess = result.success,
                ResolverErrorCode = (int)result.errorCode,
                RollCount = result.rollCount,
                GeneratedItemIds = result.generatedItemIds != null ? new System.Collections.Generic.List<string>(result.generatedItemIds).ToArray() : Array.Empty<string>(),
                TotalGeneratedWorldValue = result.totalGeneratedWorldValue,
                TotalGeneratedReserveCost = result.totalGeneratedReserveCost,
                TotalGeneratedTradeableWorldValue = result.totalGeneratedTradeableWorldValue
            };
        }

        private RunLootExtractionSummary BuildLootExtractionSummary(int runSequence, long tickStarted, RunLootSummary lootSummary, RunSurvivalSummary survivalSummary)
        {
            int seed = ComputeResolverSeed(runSequence, tickStarted);
            string ruleSourceId = _config.LootExtractionRuleSourceId;
            string[] generatedItemIds = lootSummary?.GeneratedItemIds ?? Array.Empty<string>();
            int generatedItemCount = generatedItemIds.Length;

            if (lootSummary == null || !lootSummary.ResolverSuccess)
            {
                return BuildExtractionFailureSummary(ruleSourceId, seed, RunLootExtractionSummaryErrorCode.LootSummaryMissingOrFailed, generatedItemCount, generatedItemIds);
            }

            if (survivalSummary == null || !survivalSummary.RuleResolved || survivalSummary.DeterministicErrorCode != (int)RunSurvivalSummaryErrorCode.None)
            {
                return BuildExtractionFailureSummary(ruleSourceId, seed, RunLootExtractionSummaryErrorCode.SurvivalSummaryMissingOrFailed, generatedItemCount, generatedItemIds);
            }

            double survivorRatio = survivalSummary.SurvivorRatio;
            if (survivorRatio < 0d || survivorRatio > 1d || double.IsNaN(survivorRatio) || double.IsInfinity(survivorRatio))
            {
                return BuildExtractionFailureSummary(ruleSourceId, seed, RunLootExtractionSummaryErrorCode.InvalidSurvivorRatio, generatedItemCount, generatedItemIds);
            }

            int extractedItemCount = ResolveExtractedItemCount(generatedItemCount, survivorRatio);
            if (extractedItemCount < 0)
            {
                return BuildExtractionFailureSummary(ruleSourceId, seed, RunLootExtractionSummaryErrorCode.UnknownRoundingPolicy, generatedItemCount, generatedItemIds);
            }

            if (survivalSummary.SurvivorCount <= 0 || survivalSummary.PartySize <= 0 || extractedItemCount <= 0)
            {
                return new RunLootExtractionSummary
                {
                    RuleSourceId = ruleSourceId,
                    DeterministicSeed = seed,
                    RuleResolved = true,
                    DeterministicErrorCode = (int)RunLootExtractionSummaryErrorCode.None,
                    SurvivorRatioUsed = survivorRatio,
                    GeneratedItemCount = generatedItemCount,
                    ExtractedItemIds = Array.Empty<string>(),
                    LostItemIds = CloneItemIds(generatedItemIds)
                };
            }

            string[] extracted = SelectExtractedItems(seed, generatedItemIds, extractedItemCount);
            string[] lost = ComputeLostItems(generatedItemIds, extracted);
            RunLootExtractionSummaryErrorCode totalsErrorCode = TryCalculateTotals(extracted, out int worldValue, out int reserveCost, out int tradeableWorldValue);
            if (totalsErrorCode != RunLootExtractionSummaryErrorCode.None)
            {
                return BuildExtractionFailureSummary(ruleSourceId, seed, totalsErrorCode, generatedItemCount, generatedItemIds);
            }

            return new RunLootExtractionSummary
            {
                RuleSourceId = ruleSourceId,
                DeterministicSeed = seed,
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

        private RunLootExtractionSummary BuildExtractionFailureSummary(string ruleSourceId, int seed, RunLootExtractionSummaryErrorCode errorCode, int generatedCount, string[] generatedItemIds)
        {
            return new RunLootExtractionSummary
            {
                RuleSourceId = ruleSourceId,
                DeterministicSeed = seed,
                RuleResolved = false,
                DeterministicErrorCode = (int)errorCode,
                GeneratedItemCount = generatedCount,
                ExtractedItemIds = Array.Empty<string>(),
                LostItemIds = CloneItemIds(generatedItemIds)
            };
        }

        private int ResolveExtractedItemCount(int generatedCount, double survivorRatio)
        {
            string policyId = _config.LootExtractionRoundingPolicyId;
            if (string.Equals(policyId, LootExtractionRoundFloorPolicyId, StringComparison.Ordinal))
            {
                return Math.Max(0, Math.Min(generatedCount, (int)Math.Floor(generatedCount * survivorRatio)));
            }

            return -1;
        }

        private static string[] SelectExtractedItems(int seed, string[] generatedItemIds, int extractedItemCount)
        {
            if (extractedItemCount >= generatedItemIds.Length)
            {
                return CloneItemIds(generatedItemIds);
            }

            var ranked = new List<(int index, int score)>(generatedItemIds.Length);
            for (int i = 0; i < generatedItemIds.Length; i++)
            {
                ranked.Add((i, ComputeItemRank(seed, generatedItemIds[i], i)));
            }

            ranked.Sort((left, right) =>
            {
                int scoreOrder = left.score.CompareTo(right.score);
                return scoreOrder != 0 ? scoreOrder : left.index.CompareTo(right.index);
            });

            var selected = new List<(int index, string id)>(extractedItemCount);
            for (int i = 0; i < extractedItemCount; i++)
            {
                int selectedIndex = ranked[i].index;
                selected.Add((selectedIndex, generatedItemIds[selectedIndex]));
            }

            selected.Sort((left, right) => left.index.CompareTo(right.index));
            string[] extracted = new string[selected.Count];
            for (int i = 0; i < selected.Count; i++)
            {
                extracted[i] = selected[i].id;
            }

            return extracted;
        }

        private static int ComputeItemRank(int seed, string itemId, int itemIndex)
        {
            unchecked
            {
                int hash = seed;
                string id = itemId ?? string.Empty;
                for (int i = 0; i < id.Length; i++)
                {
                    hash = (hash * 31) + id[i];
                }

                return (hash * 31) + itemIndex;
            }
        }

        private static string[] ComputeLostItems(string[] generated, string[] extracted)
        {
            var extractedCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < extracted.Length; i++)
            {
                string id = extracted[i] ?? string.Empty;
                extractedCounts[id] = extractedCounts.TryGetValue(id, out int existingCount) ? existingCount + 1 : 1;
            }

            var lost = new List<string>();
            for (int i = 0; i < generated.Length; i++)
            {
                string id = generated[i] ?? string.Empty;
                if (extractedCounts.TryGetValue(id, out int count) && count > 0)
                {
                    extractedCounts[id] = count - 1;
                    continue;
                }

                lost.Add(generated[i]);
            }

            return lost.ToArray();
        }

        private RunLootExtractionSummaryErrorCode TryCalculateTotals(string[] extracted, out int worldValue, out int reserveCost, out int tradeableWorldValue)
        {
            worldValue = 0;
            reserveCost = 0;
            tradeableWorldValue = 0;
            var valuesById = new Dictionary<string, LootItemRecord>(StringComparer.Ordinal);
            LootItemRecord[] items = _lootConfig?.items ?? Array.Empty<LootItemRecord>();
            for (int i = 0; i < items.Length; i++)
            {
                LootItemRecord item = items[i];
                if (item == null || string.IsNullOrEmpty(item.id) || valuesById.ContainsKey(item.id))
                {
                    continue;
                }
                valuesById.Add(item.id, item);
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

        private static string[] CloneItemIds(string[] itemIds)
        {
            return itemIds != null ? new List<string>(itemIds).ToArray() : Array.Empty<string>();
        }

        private static int ComputeResolverSeed(int runSequence, long tickStarted)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + runSequence;
                int tickLow = (int)(tickStarted & 0xFFFFFFFFL);
                int tickHigh = (int)((tickStarted >> 32) & 0xFFFFFFFFL);
                hash = (hash * 31) + tickLow;
                hash = (hash * 31) + tickHigh;
                return hash;
            }
        }

        private string[] BuildFeedbackTagKeys(StructureRuntimeState runtime, bool success)
        {
            System.Collections.Generic.List<string> tags = new System.Collections.Generic.List<string>(5);
            tags.Add(success ? "run.feedback.success" : "run.feedback.failure");

            if (runtime.Heat >= _config.HighHeatFeedbackThreshold)
            {
                tags.Add("run.feedback.high_heat");
            }

            if (runtime.ManaReserve <= _config.LowManaFeedbackThreshold)
            {
                tags.Add("run.feedback.low_mana");
            }

            if (runtime.IsHeatCrisisActive)
            {
                tags.Add("run.feedback.heat_crisis");
            }

            if (runtime.ManaReserve >= _config.StrongManaReserveFeedbackThreshold)
            {
                tags.Add("run.feedback.strong_mana_reserve");
            }

            return tags.ToArray();
        }
    }
}
