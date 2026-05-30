using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using TMPro;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class RunSimulationTests
    {
        private static void SetSave(GameRoot root, SaveData save)
        {
            typeof(GameRoot).GetField("<Save>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(root, save);
        }

        private static void SetPrivateField<T>(GameRoot root, string fieldName, T value)
        {
            typeof(GameRoot).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(root, value);
        }

        private static void SetContent(GameRoot root, ContentService content)
        {
            typeof(GameRoot).GetField("<Content>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(root, content);
        }

        private static ContentService BuildRunDisplayContent()
        {
            var content = new ContentService();
            Dictionary<string, string> map = (Dictionary<string, string>)typeof(ContentService)
                .GetField("_stringMap", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(content);

            map["ui.run.latest_format"] = "Run: {0} success={1} score={2} reason={3}";
            map["ui.run.history_position_format"] = "Run history: {0}/{1}";
            map["ui.run.breakdown_format"] = "Chance: {0:0.00} / threshold {1:0.00}";
            map["ui.run.feedback_format"] = "Feedback: {0}";
            map["ui.run.loot_summary_format"] = "Loot: table={0} success={1} error={2} rolls={3} items={4} wv={5} rc={6} twv={7}";
            map["ui.run.heat_delta_summary_format"] = "Run Heat Delta: resolved={0} error={1} death={2:0.###} elite={3:0.###} multi={4:0.###} survivorCooling={5:0.###} lootCooling={6:0.###} final={7:0.###} ruleSource={8}";
            map["ui.run.heat_application_summary_format"] = "Heat Application: resolved={0} error={1} before={2:0.###} delta={3:0.###} after={4:0.###} tierBefore={5} tierAfter={6} tierChanged={7} ruleSource={8}";
            map["ui.heat.current_tier_summary_format"] = "Current Heat Tier: resolved={0} error={1} heat={2:0.###} tier={3} min={4:0.###} max={5:0.###} atMin={6} atMax={7} ruleSource={8}";
            map["ui.run.adventurer_attraction_summary_format"] = "Attraction: resolved={0} error={1} extractedWv={2} perWv={3:0.###} signal={4:0.###}";
            map["ui.run.adventurer_interest_forecast_summary_format"] = "Forecast: resolved={0} error={1} signal={2:0.###} score={3:0.###} band={4}";
            map["ui.run.adventurer_demand_budget_summary_format"] = "Demand Budget: resolved={0} error={1} forecastScore={2:0.###} forecastBand={3} score={4:0.###} band={5}";
            map["run.reason.success"] = "Success";
            map["run.reason.failed_threshold"] = "Failed due to low projected chance.";
            map["run.feedback.success"] = "Successful approach";
            return content;
        }

        private static RunSimulationConfig BuildConfig()
        {
            return new RunSimulationConfig
            {
                BaseSuccessChance = 0.6d,
                HeatPenaltyPerPoint = 0.004d,
                ManaReserveBonusPerPoint = 0.01d,
                CrisisFailurePenalty = 0.3d,
                SuccessThreshold = 0.5d,
                BaseScoreOnSuccess = 100,
                ScorePerManaPoint = 2,
                MaxRunHistoryEntries = 10,
                HighHeatFeedbackThreshold = 75d,
                LowManaFeedbackThreshold = 5d,
                StrongManaReserveFeedbackThreshold = 50d,
                LootTableId = "loot.table.run.basic",
                MinPartySize = 3,
                MaxPartySize = 5,
                MaxAllowedPartySize = 100,
                SuccessSurvivorRatio = 1d,
                FailureSurvivorRatio = 0d,
                LootExtractionRoundingPolicyId = "loot_extraction.round_floor",
                LootExtractionRuleSourceId = "run.loot_extraction.rule.v1",
                LootHeatCoolingRuleSourceId = "run.loot_heat_cooling.rule.v1",
                LootHeatCoolingPerTradeableWorldValue = 0.1d,
                MaxLootHeatCoolingPerRun = 25d,
                AdventurerAttractionRuleSourceId = "run.adventurer_attraction.rule.v1",
                AdventurerAttractionPerExtractedWorldValue = 1d,
                AdventurerInterestForecastRuleSourceId = "run.adventurer_interest_forecast.rule.v1",
                AdventurerInterestLowThreshold = 5d,
                AdventurerInterestMediumThreshold = 10d,
                AdventurerInterestHighThreshold = 20d,
                AdventurerInterestScorePerAttractionSignal = 1d,
                AdventurerDemandBudgetRuleSourceId = "run.adventurer_demand_budget.rule.v1",
                AdventurerDemandBudgetScorePerForecastScore = 1d,
                AdventurerDemandBudgetLowThreshold = 5d,
                AdventurerDemandBudgetMediumThreshold = 10d,
                AdventurerDemandBudgetHighThreshold = 20d,
                RunHeatNormalDeathDelta = 1d,
                RunHeatEliteDeathDelta = 3d,
                RunHeatMultipleDeathBonusDelta = 1d,
                RunHeatSurvivorCoolingPerSurvivor = 0.5d,
                RunHeatLootCoolingPerExtractedValue = 0.1d,
                RunHeatDeltaMinimum = -10d,
                RunHeatDeltaMaximum = 10d,
                RunHeatDeltaRuleSourceId = "run.heat_delta.rule.v1",
                HeatPeaceMinimum = 0d,
                HeatPeaceMaximum = 9d,
                HeatNoticeMinimum = 10d,
                HeatNoticeMaximum = 24d,
                HeatConcernMinimum = 25d,
                HeatConcernMaximum = 49d,
                RunHeatApplicationRuleSourceId = "run.heat_application.rule.v1"
            };
        }


        private static LootConfig BuildLootConfig()
        {
            return new LootConfig
            {
                items = new[]
                {
                    new LootItemRecord { id = "loot.item.scrap.iron", tierId = "loot_tier.iron", rarityId = "loot_rarity.common", categoryId = "loot_category.material", worldValue = 3, reserveCost = 1, nameKey = "loot.item.scrap.iron.name", descriptionKey = "loot.item.scrap.iron.desc", isTradeable = true },
                    new LootItemRecord { id = "loot.item.relic.bronze", tierId = "loot_tier.bronze", rarityId = "loot_rarity.uncommon", categoryId = "loot_category.artifact", worldValue = 8, reserveCost = 2, nameKey = "loot.item.relic.bronze.name", descriptionKey = "loot.item.relic.bronze.desc", isTradeable = false }
                },
                tables = new[]
                {
                    new LootTableRecord
                    {
                        id = "loot.table.run.basic",
                        minRollCount = 2,
                        maxRollCount = 2,
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
        [Test]
        public void SimulateOnce_IsDeterministic_ForSameInput()
        {
            var service = new RunSimulationService(BuildConfig(), BuildLootConfig());
            var firstRuntime = new StructureRuntimeState { Heat = 10d, ManaReserve = 20d, IsHeatCrisisActive = false };
            var secondRuntime = new StructureRuntimeState { Heat = 10d, ManaReserve = 20d, IsHeatCrisisActive = false };

            RunOutcomeRecord first = service.SimulateOnce(firstRuntime, 50, 1);
            RunOutcomeRecord second = service.SimulateOnce(secondRuntime, 50, 1);

            Assert.That(second.RunId, Is.EqualTo(first.RunId));
            Assert.That(second.Success, Is.EqualTo(first.Success));
            Assert.That(second.Score, Is.EqualTo(first.Score));
            Assert.That(second.ReasonKey, Is.EqualTo(first.ReasonKey));
            Assert.That(second.FinalChance, Is.EqualTo(first.FinalChance));
            Assert.That(second.SuccessThresholdUsed, Is.EqualTo(first.SuccessThresholdUsed));
            Assert.That(second.FeedbackTagKeys, Is.EqualTo(first.FeedbackTagKeys));
            Assert.That(second.LootSummary.ResolverSeed, Is.EqualTo(first.LootSummary.ResolverSeed));
            Assert.That(second.LootSummary.GeneratedItemIds, Is.EqualTo(first.LootSummary.GeneratedItemIds));
            Assert.That(second.LootSummary.TotalGeneratedWorldValue, Is.EqualTo(first.LootSummary.TotalGeneratedWorldValue));
            Assert.That(second.SurvivalSummary.PartySize, Is.EqualTo(first.SurvivalSummary.PartySize));
            Assert.That(second.SurvivalSummary.SurvivorCount, Is.EqualTo(first.SurvivalSummary.SurvivorCount));
            Assert.That(second.SurvivalSummary.SurvivorRatio, Is.EqualTo(first.SurvivalSummary.SurvivorRatio));
            Assert.That(second.LootExtractionSummary.ExtractedItemIds, Is.EqualTo(first.LootExtractionSummary.ExtractedItemIds));
            Assert.That(second.LootExtractionSummary.LostItemIds, Is.EqualTo(first.LootExtractionSummary.LostItemIds));
            Assert.That(second.AdventurerDemandBudgetSummary.DemandBudgetScore, Is.EqualTo(first.AdventurerDemandBudgetSummary.DemandBudgetScore));
            Assert.That(secondRuntime.Heat, Is.EqualTo(firstRuntime.Heat));
            Assert.That(second.RunHeatApplicationSummary.RuleResolved, Is.EqualTo(first.RunHeatApplicationSummary.RuleResolved));
            Assert.That(second.RunHeatApplicationSummary.HeatBefore, Is.EqualTo(first.RunHeatApplicationSummary.HeatBefore));
            Assert.That(second.RunHeatApplicationSummary.AppliedDelta, Is.EqualTo(first.RunHeatApplicationSummary.AppliedDelta));
            Assert.That(second.RunHeatApplicationSummary.HeatAfter, Is.EqualTo(first.RunHeatApplicationSummary.HeatAfter));
            Assert.That(second.RunHeatApplicationSummary.TierBefore, Is.EqualTo(first.RunHeatApplicationSummary.TierBefore));
            Assert.That(second.RunHeatApplicationSummary.TierAfter, Is.EqualTo(first.RunHeatApplicationSummary.TierAfter));
            Assert.That(second.RunHeatApplicationSummary.TierChanged, Is.EqualTo(first.RunHeatApplicationSummary.TierChanged));
            Assert.That(first.HasBreakdown, Is.True);
            Assert.That(second.HasBreakdown, Is.True);
        }

        [Test]
        public void SimulateOnce_ExtractionSummary_FullSurvivors_ExtractsAll()
        {
            var service = new RunSimulationService(BuildConfig(), BuildLootConfig());
            RunOutcomeRecord outcome = service.SimulateOnce(new StructureRuntimeState { Heat = 0d, ManaReserve = 50d, IsHeatCrisisActive = false }, 10, 2);
            Assert.That(outcome.LootExtractionSummary.RuleResolved, Is.True);
            Assert.That(outcome.LootExtractionSummary.DeterministicErrorCode, Is.EqualTo((int)RunLootExtractionSummaryErrorCode.None));
            Assert.That(outcome.LootExtractionSummary.ExtractedItemIds, Is.EqualTo(outcome.LootSummary.GeneratedItemIds));
            Assert.That(outcome.LootExtractionSummary.LostItemIds, Is.Empty);
        }

        [Test]
        public void SimulateOnce_AttachesResolvedAdventurerAttractionSummary_FromExtractedWorldValue()
        {
            RunSimulationConfig config = BuildConfig();
            config.AdventurerAttractionPerExtractedWorldValue = 2d;
            var service = new RunSimulationService(config, BuildLootConfig());

            RunOutcomeRecord outcome = service.SimulateOnce(new StructureRuntimeState { Heat = 0d, ManaReserve = 50d, IsHeatCrisisActive = false }, 10, 2);

            Assert.That(outcome.AdventurerAttractionSummary, Is.Not.Null);
            Assert.That(outcome.AdventurerAttractionSummary.RuleResolved, Is.True);
            Assert.That(outcome.AdventurerAttractionSummary.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerAttractionSummaryErrorCode.None));
            Assert.That(outcome.AdventurerAttractionSummary.ExtractedWorldValueUsed, Is.EqualTo(outcome.LootExtractionSummary.TotalExtractedWorldValue));
            Assert.That(outcome.AdventurerAttractionSummary.AttractionSignalValue, Is.EqualTo(outcome.LootExtractionSummary.TotalExtractedWorldValue * config.AdventurerAttractionPerExtractedWorldValue));
        }

        [Test]
        public void SimulateOnce_AttachesResolvedAdventurerInterestForecastSummary_FromAttractionSummary()
        {
            RunSimulationConfig config = BuildConfig();
            config.AdventurerInterestScorePerAttractionSignal = 2d;
            config.AdventurerInterestLowThreshold = 5d;
            config.AdventurerInterestMediumThreshold = 10d;
            config.AdventurerInterestHighThreshold = 20d;
            var service = new RunSimulationService(config, BuildLootConfig());

            RunOutcomeRecord outcome = service.SimulateOnce(new StructureRuntimeState { Heat = 0d, ManaReserve = 50d, IsHeatCrisisActive = false }, 10, 2);

            Assert.That(outcome.AdventurerInterestForecastSummary, Is.Not.Null);
            Assert.That(outcome.AdventurerInterestForecastSummary.RuleResolved, Is.True);
            Assert.That(outcome.AdventurerInterestForecastSummary.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerInterestForecastSummaryErrorCode.None));
            Assert.That(outcome.AdventurerInterestForecastSummary.AttractionSignalUsed, Is.EqualTo(outcome.AdventurerAttractionSummary.AttractionSignalValue));
            Assert.That(outcome.AdventurerInterestForecastSummary.ForecastInterestScore, Is.EqualTo(outcome.AdventurerInterestForecastSummary.AttractionSignalUsed * config.AdventurerInterestScorePerAttractionSignal));
            double score = outcome.AdventurerInterestForecastSummary.ForecastInterestScore;
            string expectedBandId = score >= config.AdventurerInterestHighThreshold
                ? "adventurer_interest.high"
                : (score >= config.AdventurerInterestMediumThreshold
                    ? "adventurer_interest.medium"
                    : (score >= config.AdventurerInterestLowThreshold
                        ? "adventurer_interest.low"
                        : "adventurer_interest.none"));
            Assert.That(outcome.AdventurerInterestForecastSummary.ForecastBandId, Is.EqualTo(expectedBandId));
        }

        [Test]
        public void SimulateOnce_ExtractionSummary_ZeroSurvivors_ExtractsNone()
        {
            var service = new RunSimulationService(BuildConfig(), BuildLootConfig());
            RunOutcomeRecord outcome = service.SimulateOnce(new StructureRuntimeState { Heat = 100d, ManaReserve = 0d, IsHeatCrisisActive = true }, 10, 3);
            Assert.That(outcome.LootExtractionSummary.RuleResolved, Is.True);
            Assert.That(outcome.LootExtractionSummary.ExtractedItemIds, Is.Empty);
            Assert.That(outcome.LootExtractionSummary.LostItemIds, Is.EqualTo(outcome.LootSummary.GeneratedItemIds));
        }

        [Test]
        public void SimulateOnce_ExtractionSummary_UnknownRoundingPolicy_ReturnsDeterministicFailure()
        {
            RunSimulationConfig config = BuildConfig();
            config.LootExtractionRoundingPolicyId = "loot_extraction.round_unknown";
            var service = new RunSimulationService(config, BuildLootConfig());
            RunOutcomeRecord outcome = service.SimulateOnce(new StructureRuntimeState { Heat = 10d, ManaReserve = 20d, IsHeatCrisisActive = false }, 20, 6);
            Assert.That(outcome.LootExtractionSummary.RuleResolved, Is.False);
            Assert.That(outcome.LootExtractionSummary.DeterministicErrorCode, Is.EqualTo((int)RunLootExtractionSummaryErrorCode.UnknownRoundingPolicy));
            Assert.That(outcome.LootExtractionSummary.ExtractedItemIds, Is.Empty);
        }

        [Test]
        public void SimulateOnce_ExtractionSummary_MissingLootSummary_ReturnsDeterministicFailure()
        {
            RunSimulationConfig config = BuildConfig();
            config.LootTableId = string.Empty;
            var service = new RunSimulationService(config, BuildLootConfig());
            RunOutcomeRecord outcome = service.SimulateOnce(new StructureRuntimeState { Heat = 10d, ManaReserve = 20d, IsHeatCrisisActive = false }, 20, 6);
            Assert.That(outcome.LootExtractionSummary.RuleResolved, Is.False);
            Assert.That(outcome.LootExtractionSummary.DeterministicErrorCode, Is.EqualTo((int)RunLootExtractionSummaryErrorCode.LootSummaryMissingOrFailed));
        }

        [Test]
        public void SimulateOnce_ExtractionSummary_FailedSurvivalSummary_ReturnsDeterministicFailure()
        {
            RunSimulationConfig config = BuildConfig();
            config.SuccessSurvivorRatio = 1.5d;
            var service = new RunSimulationService(config, BuildLootConfig());
            RunOutcomeRecord outcome = service.SimulateOnce(new StructureRuntimeState { Heat = 0d, ManaReserve = 50d, IsHeatCrisisActive = false }, 20, 6);
            Assert.That(outcome.LootExtractionSummary.RuleResolved, Is.False);
            Assert.That(outcome.LootExtractionSummary.DeterministicErrorCode, Is.EqualTo((int)RunLootExtractionSummaryErrorCode.SurvivalSummaryMissingOrFailed));
        }

        [Test]
        public void SimulateOnce_ExtractionSummary_PartialRatio_ExtractsDeterministicSubset()
        {
            RunSimulationConfig config = BuildConfig();
            config.MinPartySize = 4;
            config.MaxPartySize = 4;
            config.SuccessSurvivorRatio = 0.5d;
            var service = new RunSimulationService(config, BuildLootConfig());

            RunOutcomeRecord outcome = service.SimulateOnce(new StructureRuntimeState { Heat = 0d, ManaReserve = 50d, IsHeatCrisisActive = false }, 10, 2);

            Assert.That(outcome.LootSummary.GeneratedItemIds.Length, Is.EqualTo(2));
            Assert.That(outcome.LootExtractionSummary.ExtractedItemIds, Is.EqualTo(new[] { "loot.item.scrap.iron" }));
            Assert.That(outcome.LootExtractionSummary.LostItemIds, Is.EqualTo(new[] { "loot.item.relic.bronze" }));
        }

        [Test]
        public void SimulateOnce_ExtractionSummary_AggregateOverflow_ReturnsDeterministicFailure()
        {
            LootConfig overflowConfig = new LootConfig
            {
                items = new[]
                {
                    new LootItemRecord { id = "loot.item.overflow", tierId = "loot_tier.iron", rarityId = "loot_rarity.common", categoryId = "loot_category.material", worldValue = int.MaxValue, reserveCost = int.MaxValue, nameKey = "loot.item.overflow.name", descriptionKey = "loot.item.overflow.desc", isTradeable = true }
                },
                tables = new[]
                {
                    new LootTableRecord
                    {
                        id = "loot.table.run.basic",
                        minRollCount = 2,
                        maxRollCount = 2,
                        allowEmptyPool = false,
                        pool = new[] { new LootTablePoolEntry { itemId = "loot.item.overflow", weight = 1d } }
                    }
                }
            };

            var service = new RunSimulationService(BuildConfig(), overflowConfig);
            RunOutcomeRecord outcome = service.SimulateOnce(new StructureRuntimeState { Heat = 0d, ManaReserve = 50d, IsHeatCrisisActive = false }, 10, 2);
            Assert.That(outcome.LootExtractionSummary.RuleResolved, Is.False);
            Assert.That(outcome.LootExtractionSummary.DeterministicErrorCode, Is.EqualTo((int)RunLootExtractionSummaryErrorCode.LootSummaryMissingOrFailed));
        }

        [Test]
        public void SimulateOnce_SurvivalSummary_RespectsBoundsAndRatios()
        {
            var service = new RunSimulationService(BuildConfig());
            RunOutcomeRecord successOutcome = service.SimulateOnce(new StructureRuntimeState { Heat = 0d, ManaReserve = 50d, IsHeatCrisisActive = false }, 10, 2);
            RunOutcomeRecord failureOutcome = service.SimulateOnce(new StructureRuntimeState { Heat = 100d, ManaReserve = 0d, IsHeatCrisisActive = true }, 10, 3);

            Assert.That(successOutcome.SurvivalSummary.PartySize, Is.InRange(3, 5));
            Assert.That(successOutcome.SurvivalSummary.SurvivorCount, Is.EqualTo(successOutcome.SurvivalSummary.PartySize));
            Assert.That(failureOutcome.SurvivalSummary.SurvivorCount, Is.EqualTo(0));
            Assert.That(successOutcome.SurvivalSummary.SurvivorRatio, Is.EqualTo((double)successOutcome.SurvivalSummary.SurvivorCount / successOutcome.SurvivalSummary.PartySize));
            Assert.That(failureOutcome.SurvivalSummary.SurvivorRatio, Is.EqualTo((double)failureOutcome.SurvivalSummary.SurvivorCount / failureOutcome.SurvivalSummary.PartySize));
            Assert.That(successOutcome.SurvivalSummary.DeathCount, Is.EqualTo(successOutcome.SurvivalSummary.PartySize - successOutcome.SurvivalSummary.SurvivorCount));
            Assert.That(failureOutcome.SurvivalSummary.DeathCount, Is.EqualTo(failureOutcome.SurvivalSummary.PartySize - failureOutcome.SurvivalSummary.SurvivorCount));
        }

        [Test]
        public void SimulateOnce_SurvivalSummary_InvalidConfiguredRatio_ReturnsDeterministicFailure()
        {
            RunSimulationConfig invalidSuccessRatio = BuildConfig();
            invalidSuccessRatio.SuccessSurvivorRatio = 2d;
            var successService = new RunSimulationService(invalidSuccessRatio);

            RunOutcomeRecord successOutcome = successService.SimulateOnce(new StructureRuntimeState { Heat = 0d, ManaReserve = 50d, IsHeatCrisisActive = false }, 12, 8);

            Assert.That(successOutcome.SurvivalSummary, Is.Not.Null);
            Assert.That(successOutcome.SurvivalSummary.RuleResolved, Is.False);
            Assert.That(successOutcome.SurvivalSummary.DeterministicErrorCode, Is.EqualTo((int)RunSurvivalSummaryErrorCode.InvalidSurvivorRatio));

            RunSimulationConfig invalidFailureRatio = BuildConfig();
            invalidFailureRatio.FailureSurvivorRatio = -0.1d;
            var failureService = new RunSimulationService(invalidFailureRatio);

            RunOutcomeRecord failureOutcome = failureService.SimulateOnce(new StructureRuntimeState { Heat = 100d, ManaReserve = 0d, IsHeatCrisisActive = true }, 13, 9);

            Assert.That(failureOutcome.SurvivalSummary, Is.Not.Null);
            Assert.That(failureOutcome.SurvivalSummary.RuleResolved, Is.False);
            Assert.That(failureOutcome.SurvivalSummary.DeterministicErrorCode, Is.EqualTo((int)RunSurvivalSummaryErrorCode.InvalidSurvivorRatio));
        }

        [Test]
        public void SimulateOnce_GeneratesDeterministicFeedbackTags_InStableOrder()
        {
            var service = new RunSimulationService(BuildConfig());
            var runtime = new StructureRuntimeState { Heat = 75d, ManaReserve = 50d, IsHeatCrisisActive = true };

            RunOutcomeRecord outcome = service.SimulateOnce(runtime, 44, 11);

            Assert.That(outcome.FeedbackTagKeys, Is.EqualTo(new[]
            {
                "run.feedback.success",
                "run.feedback.high_heat",
                "run.feedback.heat_crisis",
                "run.feedback.strong_mana_reserve"
            }));
        }

        [Test]
        public void SimulateOnce_FailureOutcome_GetsFailureTag()
        {
            var service = new RunSimulationService(BuildConfig());
            var runtime = new StructureRuntimeState { Heat = 80d, ManaReserve = 0d, IsHeatCrisisActive = false };

            RunOutcomeRecord outcome = service.SimulateOnce(runtime, 12, 3);

            Assert.That(outcome.FeedbackTagKeys[0], Is.EqualTo("run.feedback.failure"));
        }

        [Test]
        public void SimulateOnce_LowManaTagAppears_AtOrBelowThreshold()
        {
            var service = new RunSimulationService(BuildConfig());
            var runtime = new StructureRuntimeState { Heat = 0d, ManaReserve = 5d, IsHeatCrisisActive = false };

            RunOutcomeRecord outcome = service.SimulateOnce(runtime, 20, 6);

            Assert.That(outcome.FeedbackTagKeys, Contains.Item("run.feedback.low_mana"));
        }

        [Test]
        public void SimulateOnce_LowManaAndStrongManaTags_AppearInSeparateRuntimeStates()
        {
            var service = new RunSimulationService(BuildConfig());
            var lowManaRuntime = new StructureRuntimeState { Heat = 0d, ManaReserve = 5d, IsHeatCrisisActive = false };
            var strongManaRuntime = new StructureRuntimeState { Heat = 0d, ManaReserve = 50d, IsHeatCrisisActive = false };

            RunOutcomeRecord lowManaOutcome = service.SimulateOnce(lowManaRuntime, 20, 6);
            RunOutcomeRecord strongManaOutcome = service.SimulateOnce(strongManaRuntime, 21, 7);

            Assert.That(lowManaOutcome.FeedbackTagKeys, Contains.Item("run.feedback.low_mana"));
            Assert.That(lowManaOutcome.FeedbackTagKeys, Does.Not.Contain("run.feedback.strong_mana_reserve"));
            Assert.That(strongManaOutcome.FeedbackTagKeys, Contains.Item("run.feedback.strong_mana_reserve"));
            Assert.That(strongManaOutcome.FeedbackTagKeys, Does.Not.Contain("run.feedback.low_mana"));
        }

        [Test]
        public void SimulateOnce_HeatPenalty_CausesFailure_WhenInputIsHighHeat()
        {
            var service = new RunSimulationService(BuildConfig());
            var runtime = new StructureRuntimeState { Heat = 80d, ManaReserve = 0d, IsHeatCrisisActive = false };

            RunOutcomeRecord outcome = service.SimulateOnce(runtime, 12, 3);

            Assert.That(outcome.Success, Is.False);
            Assert.That(outcome.ReasonKey, Is.EqualTo("run.reason.failed_threshold"));
        }

        [Test]
        public void SimulateOnce_CrisisPenalty_ChangesReasonDeterministically()
        {
            var service = new RunSimulationService(BuildConfig());
            var runtime = new StructureRuntimeState { Heat = 20d, ManaReserve = 0d, IsHeatCrisisActive = true };

            RunOutcomeRecord outcome = service.SimulateOnce(runtime, 12, 4);

            Assert.That(outcome.Success, Is.False);
            Assert.That(outcome.ReasonKey, Is.EqualTo("run.reason.crisis_failure"));
            Assert.That(outcome.CrisisActiveAtStart, Is.True);
        }


        [Test]
        public void SimulateOnce_ComputesBreakdown_AndClampsFinalChance()
        {
            var service = new RunSimulationService(BuildConfig());
            var runtime = new StructureRuntimeState { Heat = 100d, ManaReserve = 200d, IsHeatCrisisActive = false };

            RunOutcomeRecord outcome = service.SimulateOnce(runtime, 99, 7);

            Assert.That(outcome.BaseChance, Is.EqualTo(0.6d));
            Assert.That(outcome.HeatPenaltyApplied, Is.EqualTo(0.4d));
            Assert.That(outcome.ManaBonusApplied, Is.EqualTo(2.0d));
            Assert.That(outcome.CrisisPenaltyApplied, Is.EqualTo(0d));
            Assert.That(outcome.FinalChance, Is.EqualTo(1d));
            Assert.That(outcome.SuccessThresholdUsed, Is.EqualTo(0.5d));
            Assert.That(outcome.HasBreakdown, Is.True);
        }

        [Test]
        public void SimulateOnce_CrisisPenaltyApplied_OnlyWhenCrisisActive()
        {
            var service = new RunSimulationService(BuildConfig());
            var nonCrisisRuntime = new StructureRuntimeState { Heat = 10d, ManaReserve = 1d, IsHeatCrisisActive = false };
            var crisisRuntime = new StructureRuntimeState { Heat = 10d, ManaReserve = 1d, IsHeatCrisisActive = true };

            RunOutcomeRecord nonCrisisOutcome = service.SimulateOnce(nonCrisisRuntime, 10, 1);
            RunOutcomeRecord crisisOutcome = service.SimulateOnce(crisisRuntime, 10, 2);

            Assert.That(nonCrisisOutcome.CrisisPenaltyApplied, Is.EqualTo(0d));
            Assert.That(crisisOutcome.CrisisPenaltyApplied, Is.EqualTo(BuildConfig().CrisisFailurePenalty));
        }


        [Test]
        public void SimulateOnce_LootSummary_UsesStableResolverSeedFormula()
        {
            var service = new RunSimulationService(BuildConfig(), BuildLootConfig());
            var runtime = new StructureRuntimeState { Heat = 1d, ManaReserve = 1d, IsHeatCrisisActive = false };

            RunOutcomeRecord outcome = service.SimulateOnce(runtime, 1234567890123L, 7);

            Assert.That(outcome.LootSummary, Is.Not.Null);
            Assert.That(outcome.LootSummary.ResolverSeed, Is.EqualTo(-848467382));
        }

        [Test]
        public void SaveData_RunOutcome_PreservesJsonRoundTrip()
        {
            var save = new SaveData
            {
                runHistory = new RunHistoryState
                {
                    NextRunSequence = 9,
                    LatestOutcome = new RunOutcomeRecord
                    {
                        RunId = "run-8",
                        TickStarted = 21,
                        Success = true,
                        Score = 140,
                        ReasonKey = "run.reason.success",
                        HeatAtStart = 10d,
                        ManaAtStart = 20d,
                        CrisisActiveAtStart = false,
                        HasBreakdown = true,
                        BaseChance = 0.6d,
                        HeatPenaltyApplied = 0.04d,
                        ManaBonusApplied = 0.2d,
                        CrisisPenaltyApplied = 0d,
                        FinalChance = 0.76d,
                        SuccessThresholdUsed = 0.5d,
                        FeedbackTagKeys = new[] { "run.feedback.success", "run.feedback.strong_mana_reserve" },
                        LootSummary = new RunLootSummary
                        {
                            LootTableId = "loot.table.run.basic",
                            ResolverSeed = 12345,
                            ResolverSuccess = true,
                            ResolverErrorCode = 0,
                            RollCount = 2,
                            GeneratedItemIds = new[] { "loot.item.scrap.iron", "loot.item.relic.bronze" },
                            TotalGeneratedWorldValue = 11,
                            TotalGeneratedReserveCost = 3,
                            TotalGeneratedTradeableWorldValue = 3
                        },
                        SurvivalSummary = new RunSurvivalSummary
                        {
                            PartySize = 4,
                            SurvivorCount = 4,
                            DeathCount = 0,
                            SurvivorRatio = 1d,
                            DeterministicSeed = 12345,
                            RuleResolved = true,
                            DeterministicErrorCode = 0,
                            RuleSourceId = "run.survival.rule.v1",
                            SuccessAtResolution = true
                        },
                        LootExtractionSummary = new RunLootExtractionSummary
                        {
                            RuleSourceId = "run.loot_extraction.rule.v1",
                            DeterministicSeed = 12345,
                            RuleResolved = true,
                            DeterministicErrorCode = (int)RunLootExtractionSummaryErrorCode.None,
                            SurvivorRatioUsed = 1d,
                            GeneratedItemCount = 2,
                            ExtractedItemIds = new[] { "loot.item.scrap.iron", "loot.item.relic.bronze" },
                            LostItemIds = new string[0],
                            TotalExtractedWorldValue = 11,
                            TotalExtractedReserveCost = 3,
                            TotalExtractedTradeableWorldValue = 3
                        },
                        LootHeatCoolingSummary = new RunLootHeatCoolingSummary
                        {
                            RuleSourceId = "run.loot_heat_cooling.rule.v1",
                            DeterministicSeed = 12345,
                            RuleResolved = true,
                            DeterministicErrorCode = (int)RunLootHeatCoolingSummaryErrorCode.None,
                            ExtractedTradeableWorldValueUsed = 3d,
                            CoolingPerTradeableWorldValueUsed = 0.1d,
                            UnclampedHeatDelta = -0.3d,
                            AppliedHeatDelta = -0.3d,
                            HeatBeforeCooling = 10d,
                            HeatAfterCooling = 9.7d
                        }
                    },
                    RecentOutcomes = new[]
                    {
                        new RunOutcomeRecord
                        {
                            RunId = "run-7",
                            TickStarted = 20,
                            Success = false,
                            Score = 0,
                            ReasonKey = "run.reason.failed_threshold",
                            LootSummary = new RunLootSummary
                            {
                                LootTableId = "loot.table.run.basic",
                                ResolverSeed = 777,
                                ResolverSuccess = false,
                                ResolverErrorCode = (int)LootRollResolverErrorCode.ItemNotFound,
                                RollCount = 0,
                                GeneratedItemIds = new string[0],
                                TotalGeneratedWorldValue = 0,
                                TotalGeneratedReserveCost = 0,
                                TotalGeneratedTradeableWorldValue = 0
                            },
                            LootExtractionSummary = new RunLootExtractionSummary
                            {
                                RuleSourceId = "run.loot_extraction.rule.v1",
                                DeterministicSeed = 777,
                                RuleResolved = false,
                                DeterministicErrorCode = (int)RunLootExtractionSummaryErrorCode.UnknownRoundingPolicy,
                                SurvivorRatioUsed = 0.5d,
                                GeneratedItemCount = 2,
                                ExtractedItemIds = new[] { "loot.item.scrap.iron" },
                                LostItemIds = new[] { "loot.item.relic.bronze" },
                                TotalExtractedWorldValue = 3,
                                TotalExtractedReserveCost = 1,
                                TotalExtractedTradeableWorldValue = 3
                            },
                            LootHeatCoolingSummary = new RunLootHeatCoolingSummary
                            {
                                RuleSourceId = "run.loot_heat_cooling.rule.v1",
                                DeterministicSeed = 777,
                                RuleResolved = false,
                                DeterministicErrorCode = (int)RunLootHeatCoolingSummaryErrorCode.ExtractionSummaryMissingOrFailed,
                                ExtractedTradeableWorldValueUsed = 0d,
                                CoolingPerTradeableWorldValueUsed = 0.1d,
                                UnclampedHeatDelta = 0d,
                                AppliedHeatDelta = 0d,
                                HeatBeforeCooling = 20d,
                                HeatAfterCooling = 20d
                            }
                        },
                        new RunOutcomeRecord
                        {
                            RunId = "run-8",
                            TickStarted = 21,
                            Success = true,
                            Score = 140,
                            ReasonKey = "run.reason.success"
                        }
                    }
                }
            };

            string json = JsonUtility.ToJson(save);
            SaveData loaded = JsonUtility.FromJson<SaveData>(json);

            Assert.That(loaded.runHistory.NextRunSequence, Is.EqualTo(9));
            Assert.That(loaded.runHistory.LatestOutcome.RunId, Is.EqualTo("run-8"));
            Assert.That(loaded.runHistory.LatestOutcome.Score, Is.EqualTo(140));
            Assert.That(loaded.runHistory.LatestOutcome.ReasonKey, Is.EqualTo("run.reason.success"));
            Assert.That(loaded.runHistory.RecentOutcomes.Length, Is.EqualTo(2));
            Assert.That(loaded.runHistory.RecentOutcomes[0].RunId, Is.EqualTo("run-7"));
            Assert.That(loaded.runHistory.RecentOutcomes[1].RunId, Is.EqualTo("run-8"));
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootSummary, Is.Not.Null);
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootSummary.ResolverErrorCode, Is.EqualTo((int)LootRollResolverErrorCode.ItemNotFound));
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootExtractionSummary, Is.Not.Null);
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootExtractionSummary.RuleResolved, Is.False);
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootExtractionSummary.DeterministicErrorCode, Is.EqualTo((int)RunLootExtractionSummaryErrorCode.UnknownRoundingPolicy));
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootExtractionSummary.ExtractedItemIds, Is.EqualTo(new[] { "loot.item.scrap.iron" }));
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootExtractionSummary.LostItemIds, Is.EqualTo(new[] { "loot.item.relic.bronze" }));
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootExtractionSummary.TotalExtractedWorldValue, Is.EqualTo(3));
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootExtractionSummary.TotalExtractedReserveCost, Is.EqualTo(1));
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootExtractionSummary.TotalExtractedTradeableWorldValue, Is.EqualTo(3));
            Assert.That(loaded.runHistory.LatestOutcome.HasBreakdown, Is.True);
            Assert.That(loaded.runHistory.LatestOutcome.BaseChance, Is.EqualTo(0.6d));
            Assert.That(loaded.runHistory.LatestOutcome.HeatPenaltyApplied, Is.EqualTo(0.04d));
            Assert.That(loaded.runHistory.LatestOutcome.ManaBonusApplied, Is.EqualTo(0.2d));
            Assert.That(loaded.runHistory.LatestOutcome.CrisisPenaltyApplied, Is.EqualTo(0d));
            Assert.That(loaded.runHistory.LatestOutcome.FinalChance, Is.EqualTo(0.76d));
            Assert.That(loaded.runHistory.LatestOutcome.SuccessThresholdUsed, Is.EqualTo(0.5d));
            Assert.That(loaded.runHistory.LatestOutcome.FeedbackTagKeys, Is.EqualTo(new[] { "run.feedback.success", "run.feedback.strong_mana_reserve" }));
            Assert.That(loaded.runHistory.LatestOutcome.LootSummary.LootTableId, Is.EqualTo("loot.table.run.basic"));
            Assert.That(loaded.runHistory.LatestOutcome.LootSummary.GeneratedItemIds.Length, Is.EqualTo(2));
            Assert.That(loaded.runHistory.LatestOutcome.SurvivalSummary, Is.Not.Null);
            Assert.That(loaded.runHistory.LatestOutcome.SurvivalSummary.PartySize, Is.EqualTo(4));
            Assert.That(loaded.runHistory.LatestOutcome.SurvivalSummary.DeterministicSeed, Is.EqualTo(12345));
            Assert.That(loaded.runHistory.LatestOutcome.LootExtractionSummary, Is.Not.Null);
            Assert.That(loaded.runHistory.LatestOutcome.LootExtractionSummary.RuleResolved, Is.True);
            Assert.That(loaded.runHistory.LatestOutcome.LootExtractionSummary.DeterministicErrorCode, Is.EqualTo((int)RunLootExtractionSummaryErrorCode.None));
            Assert.That(loaded.runHistory.LatestOutcome.LootExtractionSummary.SurvivorRatioUsed, Is.EqualTo(1d));
            Assert.That(loaded.runHistory.LatestOutcome.LootExtractionSummary.GeneratedItemCount, Is.EqualTo(2));
            Assert.That(loaded.runHistory.LatestOutcome.LootExtractionSummary.ExtractedItemIds, Is.EqualTo(new[] { "loot.item.scrap.iron", "loot.item.relic.bronze" }));
            Assert.That(loaded.runHistory.LatestOutcome.LootExtractionSummary.LostItemIds, Is.Empty);
            Assert.That(loaded.runHistory.LatestOutcome.LootExtractionSummary.TotalExtractedWorldValue, Is.EqualTo(11));
            Assert.That(loaded.runHistory.LatestOutcome.LootExtractionSummary.TotalExtractedReserveCost, Is.EqualTo(3));
            Assert.That(loaded.runHistory.LatestOutcome.LootExtractionSummary.TotalExtractedTradeableWorldValue, Is.EqualTo(3));
            Assert.That(loaded.runHistory.LatestOutcome.LootHeatCoolingSummary.RuleResolved, Is.True);
            Assert.That(loaded.runHistory.LatestOutcome.LootHeatCoolingSummary.DeterministicErrorCode, Is.EqualTo((int)RunLootHeatCoolingSummaryErrorCode.None));
            Assert.That(loaded.runHistory.LatestOutcome.LootHeatCoolingSummary.ExtractedTradeableWorldValueUsed, Is.EqualTo(3d));
            Assert.That(loaded.runHistory.LatestOutcome.LootHeatCoolingSummary.CoolingPerTradeableWorldValueUsed, Is.EqualTo(0.1d));
            Assert.That(loaded.runHistory.LatestOutcome.LootHeatCoolingSummary.UnclampedHeatDelta, Is.EqualTo(-0.3d));
            Assert.That(loaded.runHistory.LatestOutcome.LootHeatCoolingSummary.AppliedHeatDelta, Is.EqualTo(-0.3d));
            Assert.That(loaded.runHistory.LatestOutcome.LootHeatCoolingSummary.HeatBeforeCooling, Is.EqualTo(10d));
            Assert.That(loaded.runHistory.LatestOutcome.LootHeatCoolingSummary.HeatAfterCooling, Is.EqualTo(9.7d));
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootHeatCoolingSummary.RuleResolved, Is.False);
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootHeatCoolingSummary.DeterministicErrorCode, Is.EqualTo((int)RunLootHeatCoolingSummaryErrorCode.ExtractionSummaryMissingOrFailed));
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootHeatCoolingSummary.ExtractedTradeableWorldValueUsed, Is.EqualTo(0d));
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootHeatCoolingSummary.CoolingPerTradeableWorldValueUsed, Is.EqualTo(0.1d));
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootHeatCoolingSummary.UnclampedHeatDelta, Is.EqualTo(0d));
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootHeatCoolingSummary.AppliedHeatDelta, Is.EqualTo(0d));
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootHeatCoolingSummary.HeatBeforeCooling, Is.EqualTo(20d));
            Assert.That(loaded.runHistory.RecentOutcomes[0].LootHeatCoolingSummary.HeatAfterCooling, Is.EqualTo(20d));
        }

        [Test]
        public void RefreshRunLine_NullFeedbackTags_IsLegacySafe()
        {
            var go = new GameObject("GameRootLegacyFeedbackTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                var save = new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        LatestOutcome = new RunOutcomeRecord
                        {
                            RunId = "run-legacy",
                            Success = true,
                            Score = 100,
                            ReasonKey = "run.reason.success",
                            FeedbackTagKeys = null
                        }
                    }
                };

                SetSave(root, save);

                root.RefreshRunLine();
                Assert.That(root.RunFeedbackLine, Is.EqualTo(string.Empty));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RefreshRunLine_HidesBreakdown_ForLegacyOutcomeWithoutBreakdownFlag()
        {
            var go = new GameObject("GameRootTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                var save = new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        LatestOutcome = new RunOutcomeRecord
                        {
                            RunId = "run-legacy",
                            Success = false,
                            Score = 0,
                            ReasonKey = "run.reason.failed_threshold",
                            HasBreakdown = false
                        },
                        RecentOutcomes = new[] { new RunOutcomeRecord { RunId = "run-legacy" } }
                    }
                };

                SetSave(root, save);

                root.RefreshRunLine();

                Assert.That(root.RunBreakdownLine, Is.EqualTo(string.Empty));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }


        [Test]
        public void SimulateOnce_AttachesLootSummary_WhenLootConfigValid()
        {
            var service = new RunSimulationService(BuildConfig(), BuildLootConfig());
            var runtime = new StructureRuntimeState { Heat = 10d, ManaReserve = 20d, IsHeatCrisisActive = false };

            RunOutcomeRecord outcome = service.SimulateOnce(runtime, 50, 1);

            Assert.That(outcome.LootSummary, Is.Not.Null);
            Assert.That(outcome.LootSummary.LootTableId, Is.EqualTo("loot.table.run.basic"));
            Assert.That(outcome.LootSummary.ResolverSuccess, Is.True);
            Assert.That(outcome.LootSummary.GeneratedItemIds.Length, Is.EqualTo(outcome.LootSummary.RollCount));
        }

        [Test]
        public void SimulateOnce_StoresResolverFailure_WithoutCrashing()
        {
            LootConfig invalid = BuildLootConfig();
            invalid.tables[0].pool[0].itemId = "loot.item.missing";
            var service = new RunSimulationService(BuildConfig(), invalid);
            var runtime = new StructureRuntimeState { Heat = 10d, ManaReserve = 20d, IsHeatCrisisActive = false };

            RunOutcomeRecord outcome = service.SimulateOnce(runtime, 50, 1);

            Assert.That(outcome.LootSummary, Is.Not.Null);
            Assert.That(outcome.LootSummary.ResolverSuccess, Is.False);
            Assert.That(outcome.LootSummary.ResolverErrorCode, Is.EqualTo((int)LootRollResolverErrorCode.ItemNotFound));
        }


        [Test]
        public void RefreshRunLine_LootSummaryLine_IncludesResolverErrorCode()
        {
            var go = new GameObject("GameRootLootLineErrorTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-err",
                                Success = false,
                                Score = 0,
                                ReasonKey = "run.reason.failed_threshold",
                                FeedbackTagKeys = new string[0],
                                LootSummary = new RunLootSummary
                                {
                                    LootTableId = "loot.table.run.basic",
                                    ResolverSeed = 1,
                                    ResolverSuccess = false,
                                    ResolverErrorCode = (int)LootRollResolverErrorCode.ItemNotFound,
                                    RollCount = 0,
                                    GeneratedItemIds = new string[0]
                                }
                            }
                        }
                    }
                });
                SetContent(root, BuildRunDisplayContent());

                root.RefreshRunLine();
                StringAssert.Contains("error=5", root.RunLootLine);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TryCreateRunSimulationService_WithLootConfig_ProducesSuccessfulLootSummary()
        {
            string runJson = JsonUtility.ToJson(BuildConfig());
            string lootJson = JsonUtility.ToJson(BuildLootConfig());

            bool ok = GameRoot.TryCreateRunSimulationService(runJson, lootJson, out RunSimulationService service);

            Assert.That(ok, Is.True);
            Assert.That(service, Is.Not.Null);

            RunOutcomeRecord outcome = service.SimulateOnce(new StructureRuntimeState { Heat = 10d, ManaReserve = 20d, IsHeatCrisisActive = false }, 10, 1);
            Assert.That(outcome.LootSummary, Is.Not.Null);
            Assert.That(outcome.LootSummary.ResolverSuccess, Is.True);
        }

        [Test]
        public void RefreshRunLine_LegacyNullLootSummary_IsSafeAndEmpty()
        {
            var go = new GameObject("GameRootLegacyLootTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-legacy",
                                Success = true,
                                Score = 1,
                                ReasonKey = "run.reason.success",
                                LootSummary = null
                            }
                        }
                    }
                });

                root.RefreshRunLine();
                Assert.That(root.RunLootLine, Is.EqualTo(string.Empty));
                Assert.That(root.RunSurvivalLine, Is.EqualTo(string.Empty));
                Assert.That(root.RunExtractionLine, Is.EqualTo(string.Empty));
                Assert.That(root.RunAdventurerAttractionLine, Is.EqualTo(string.Empty));
                Assert.That(root.RunAdventurerInterestForecastLine, Is.EqualTo(string.Empty));
                Assert.That(root.RunAdventurerDemandBudgetLine, Is.EqualTo(string.Empty));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RefreshRunLine_SurvivalSummary_WithNullContent_UsesKeyFallbackSafely()
        {
            var go = new GameObject("GameRootSurvivalKeyFallbackTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-survival",
                                Success = true,
                                Score = 1,
                                ReasonKey = "run.reason.success",
                                SurvivalSummary = new RunSurvivalSummary
                                {
                                    PartySize = 4,
                                    SurvivorCount = 4,
                                    DeathCount = 0,
                                    SurvivorRatio = 1d,
                                    DeterministicSeed = 10,
                                    DeterministicErrorCode = (int)RunSurvivalSummaryErrorCode.None,
                                    RuleResolved = true,
                                    RuleSourceId = "run.survival.rule.v1",
                                    SuccessAtResolution = true
                                }
                            }
                        }
                    }
                });

                root.RefreshRunLine();
                Assert.That(root.RunSurvivalLine, Is.EqualTo("ui.run.survival_summary_format"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RefreshRunLine_RunDisplay_WithNullContent_UsesKeyFallbackSafely()
        {
            var go = new GameObject("GameRootRunDisplayKeyFallbackTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-1",
                                Success = true,
                                Score = 12,
                                ReasonKey = "run.reason.success",
                                HasBreakdown = true,
                                FinalChance = 0.7d,
                                SuccessThresholdUsed = 0.5d,
                                FeedbackTagKeys = new[] { "run.feedback.success" },
                                LootSummary = new RunLootSummary
                                {
                                    LootTableId = "loot.table.run.basic",
                                    ResolverSuccess = true,
                                    ResolverErrorCode = (int)LootRollResolverErrorCode.None
                                }
                            }
                        }
                    }
                });

                root.RefreshRunLine();
                Assert.That(root.RunLine, Is.EqualTo("ui.run.latest_format"));
                Assert.That(root.RunHistoryLine, Is.EqualTo("ui.run.history_position_format"));
                Assert.That(root.RunBreakdownLine, Is.EqualTo("ui.run.breakdown_format"));
                Assert.That(root.RunFeedbackLine, Is.EqualTo("ui.run.feedback_format"));
                Assert.That(root.RunLootLine, Is.EqualTo("ui.run.loot_summary_format"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RefreshRunLine_ExtractionSummary_WithNullContent_UsesKeyFallbackSafely()
        {
            var go = new GameObject("GameRootExtractionKeyFallbackTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-extraction",
                                Success = true,
                                Score = 1,
                                ReasonKey = "run.reason.success",
                                FeedbackTagKeys = new string[0],
                                LootExtractionSummary = new RunLootExtractionSummary
                                {
                                    RuleSourceId = "run.loot_extraction.rule.v1",
                                    RuleResolved = true,
                                    DeterministicErrorCode = (int)RunLootExtractionSummaryErrorCode.None,
                                    SurvivorRatioUsed = 1d,
                                    GeneratedItemCount = 1,
                                    ExtractedItemIds = new[] { "loot.item.scrap.iron" },
                                    LostItemIds = new string[0]
                                }
                            }
                        }
                    }
                });

                root.RefreshRunLine();
                Assert.That(root.RunExtractionLine, Is.EqualTo("ui.run.extraction_summary_format"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
        [Test]
        public void TryCreateRunSimulationService_ReturnsFalse_For_Malformed_Config()
        {
            bool ok = GameRoot.TryCreateRunSimulationService("{bad json", string.Empty, out RunSimulationService service);

            Assert.That(ok, Is.False);
            Assert.That(service, Is.Null);
        }

        [Test]
        public void IsValidRunSimulationConfig_Rejects_Invalid_Config()
        {
            var config = BuildConfig();
            config.HighHeatFeedbackThreshold = -1d;

            bool isValid = GameRoot.IsValidRunSimulationConfig(config);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void IsValidRunSimulationConfig_Rejects_Overlapping_ManaThresholds()
        {
            var config = BuildConfig();
            config.LowManaFeedbackThreshold = 50d;
            config.StrongManaReserveFeedbackThreshold = 50d;

            bool isValid = GameRoot.IsValidRunSimulationConfig(config);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void IsValidRunSimulationConfig_Rejects_MaxPartySize_AboveCap()
        {
            var config = BuildConfig();
            config.MaxPartySize = config.MaxAllowedPartySize + 1;

            bool isValid = GameRoot.IsValidRunSimulationConfig(config);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void IsValidRunSimulationConfig_Accepts_MaxPartySize_EqualToLimit()
        {
            var config = BuildConfig();
            config.MaxPartySize = config.MaxAllowedPartySize;

            bool isValid = GameRoot.IsValidRunSimulationConfig(config);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void IsValidRunSimulationConfig_Rejects_EmptyLootExtractionRuleSourceId()
        {
            RunSimulationConfig config = BuildConfig();
            config.LootExtractionRuleSourceId = string.Empty;
            bool isValid = GameRoot.IsValidRunSimulationConfig(config);
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void IsValidRunSimulationConfig_Rejects_EmptyLootExtractionRoundingPolicyId()
        {
            RunSimulationConfig config = BuildConfig();
            config.LootExtractionRoundingPolicyId = string.Empty;
            bool isValid = GameRoot.IsValidRunSimulationConfig(config);
            Assert.That(isValid, Is.False);
        }


        [Test]
        public void IsValidRunSimulationConfig_Rejects_BlankRunHeatApplicationRuleSourceId()
        {
            RunSimulationConfig config = BuildConfig();
            config.RunHeatApplicationRuleSourceId = " ";

            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);
        }

        [Test]
        public void IsValidRunSimulationConfig_Rejects_InvalidLootHeatCoolingConfigNumbers()
        {
            RunSimulationConfig config = BuildConfig();
            config.LootHeatCoolingPerTradeableWorldValue = double.NaN;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.MaxLootHeatCoolingPerRun = double.PositiveInfinity;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.LootHeatCoolingRuleSourceId = string.Empty;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.MaxLootHeatCoolingPerRun = 0d;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.True);
        }

        [Test]
        public void IsValidRunSimulationConfig_Accepts_ValidAdventurerAttractionConfig()
        {
            RunSimulationConfig config = BuildConfig();
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.True);
        }

        [Test]
        public void IsValidRunSimulationConfig_Rejects_InvalidAdventurerAttractionConfig()
        {
            RunSimulationConfig config = BuildConfig();
            config.AdventurerAttractionRuleSourceId = string.Empty;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.AdventurerAttractionRuleSourceId = " ";
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.AdventurerAttractionPerExtractedWorldValue = -0.01d;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.AdventurerAttractionPerExtractedWorldValue = double.NaN;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.AdventurerAttractionPerExtractedWorldValue = double.PositiveInfinity;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);
        }

        [Test]
        public void IsValidRunSimulationConfig_Rejects_InvalidAdventurerInterestForecastConfig()
        {
            RunSimulationConfig config = BuildConfig();
            config.AdventurerInterestForecastRuleSourceId = string.Empty;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.AdventurerInterestLowThreshold = -0.01d;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.AdventurerInterestMediumThreshold = 4d;
            config.AdventurerInterestLowThreshold = 5d;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.AdventurerInterestHighThreshold = 9d;
            config.AdventurerInterestMediumThreshold = 10d;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.AdventurerInterestScorePerAttractionSignal = double.NaN;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);
        }

        [Test]
        public void RefreshRunLine_HeatCoolingSummary_WithNullContent_UsesKeyFallbackSafely()
        {
            var go = new GameObject("GameRootHeatCoolingNullContentTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-heat",
                                ReasonKey = "run.reason.success",
                                LootHeatCoolingSummary = new RunLootHeatCoolingSummary { RuleResolved = true, DeterministicErrorCode = (int)RunLootHeatCoolingSummaryErrorCode.None }
                            }
                        }
                    }
                });

                root.RefreshRunLine();
                Assert.That(root.RunHeatCoolingLine, Is.EqualTo("ui.run.heat_cooling_summary_format"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_AdventurerAttractionSummary_WithNullContent_UsesKeyFallbackSafely()
        {
            var go = new GameObject("GameRootAttractionNullContentTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-attraction",
                                ReasonKey = "run.reason.success",
                                FeedbackTagKeys = new string[0],
                                AdventurerAttractionSummary = new RunAdventurerAttractionSummary { RuleResolved = true }
                            }
                        }
                    }
                });

                root.RefreshRunLine();
                Assert.That(root.RunAdventurerAttractionLine, Is.EqualTo("ui.run.adventurer_attraction_summary_format"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_AdventurerAttractionSummary_MissingLocalizationKey_UsesKeyFallbackSafely()
        {
            var go = new GameObject("GameRootAttractionMissingKeyTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-attraction",
                                ReasonKey = "run.reason.success",
                                FeedbackTagKeys = new string[0],
                                AdventurerAttractionSummary = new RunAdventurerAttractionSummary { RuleResolved = true }
                            }
                        }
                    }
                });
                SetContent(root, new ContentService());

                root.RefreshRunLine();
                Assert.That(root.RunAdventurerAttractionLine, Is.EqualTo("ui.run.adventurer_attraction_summary_format"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_AdventurerAttractionSummary_ValidOutcome_IsDisplayed()
        {
            var go = new GameObject("GameRootAttractionDisplayTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-attraction",
                                ReasonKey = "run.reason.success",
                                FeedbackTagKeys = new string[0],
                                AdventurerAttractionSummary = new RunAdventurerAttractionSummary
                                {
                                    RuleResolved = true,
                                    DeterministicErrorCode = (int)RunAdventurerAttractionSummaryErrorCode.None,
                                    ExtractedWorldValueUsed = 11,
                                    AttractionPerExtractedWorldValueUsed = 2d,
                                    AttractionSignalValue = 22d
                                }
                            }
                        }
                    }
                });
                SetContent(root, BuildRunDisplayContent());

                root.RefreshRunLine();
                StringAssert.Contains("resolved=True", root.RunAdventurerAttractionLine);
                StringAssert.Contains("signal=22", root.RunAdventurerAttractionLine);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_EmptyFeedback_ClearsStaleHeatCoolingLine()
        {
            var go = new GameObject("GameRootHeatCoolingStaleTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-a",
                                ReasonKey = "run.reason.success",
                                FeedbackTagKeys = new[] { "run.feedback.success" },
                                LootHeatCoolingSummary = new RunLootHeatCoolingSummary { RuleResolved = true }
                            },
                            new RunOutcomeRecord
                            {
                                RunId = "run-b",
                                ReasonKey = "run.reason.success",
                                FeedbackTagKeys = new string[0],
                                LootHeatCoolingSummary = null
                            }
                        }
                    }
                });

                root.RefreshRunLine();
                Assert.That(root.RunHeatCoolingLine, Is.EqualTo(string.Empty));
                root.SelectPreviousRunOutcome();
                root.RefreshRunLine();
                Assert.That(root.RunHeatCoolingLine, Is.EqualTo("ui.run.heat_cooling_summary_format"));
                root.SelectNextRunOutcome();
                root.RefreshRunLine();
                Assert.That(root.RunHeatCoolingLine, Is.EqualTo(string.Empty));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_AdventurerInterestForecastSummary_WithNullContent_UsesKeyFallbackSafely()
        {
            var go = new GameObject("GameRootForecastNullContentTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-forecast",
                                ReasonKey = "run.reason.success",
                                FeedbackTagKeys = new string[0],
                                AdventurerInterestForecastSummary = new RunAdventurerInterestForecastSummary { RuleResolved = true }
                            }
                        }
                    }
                });

                root.RefreshRunLine();
                Assert.That(root.RunAdventurerInterestForecastLine, Is.EqualTo("ui.run.adventurer_interest_forecast_summary_format"));
            }
            finally { Object.DestroyImmediate(go); }
        }


        [Test]
        public void RefreshRunLine_AdventurerInterestForecastSummary_MissingLocalizationKey_UsesKeyFallbackSafely()
        {
            var go = new GameObject("GameRootForecastMissingKeyTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-forecast",
                                ReasonKey = "run.reason.success",
                                FeedbackTagKeys = new string[0],
                                AdventurerInterestForecastSummary = new RunAdventurerInterestForecastSummary { RuleResolved = true }
                            }
                        }
                    }
                });
                SetContent(root, new ContentService());

                root.RefreshRunLine();
                Assert.That(root.RunAdventurerInterestForecastLine, Is.EqualTo("ui.run.adventurer_interest_forecast_summary_format"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_AdventurerInterestForecastSummary_ValidOutcome_IsDisplayed()
        {
            var go = new GameObject("GameRootForecastDisplayTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-forecast",
                                ReasonKey = "run.reason.success",
                                FeedbackTagKeys = new string[0],
                                AdventurerInterestForecastSummary = new RunAdventurerInterestForecastSummary
                                {
                                    RuleResolved = true,
                                    DeterministicErrorCode = (int)RunAdventurerInterestForecastSummaryErrorCode.None,
                                    AttractionSignalUsed = 12d,
                                    ForecastInterestScore = 24d,
                                    ForecastBandId = "adventurer_interest.high"
                                }
                            }
                        }
                    }
                });
                SetContent(root, BuildRunDisplayContent());

                root.RefreshRunLine();
                StringAssert.Contains("resolved=True", root.RunAdventurerInterestForecastLine);
                StringAssert.Contains("score=24", root.RunAdventurerInterestForecastLine);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_EmptyFeedback_ClearsStaleAdventurerInterestForecastLine()
        {
            var go = new GameObject("GameRootForecastStaleTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-a",
                                ReasonKey = "run.reason.success",
                                FeedbackTagKeys = new[] { "run.feedback.success" },
                                AdventurerInterestForecastSummary = new RunAdventurerInterestForecastSummary { RuleResolved = true }
                            },
                            new RunOutcomeRecord
                            {
                                RunId = "run-b",
                                ReasonKey = "run.reason.success",
                                FeedbackTagKeys = new string[0],
                                AdventurerInterestForecastSummary = null
                            }
                        }
                    }
                });

                root.RefreshRunLine();
                Assert.That(root.RunAdventurerInterestForecastLine, Is.EqualTo(string.Empty));
                root.SelectPreviousRunOutcome();
                root.RefreshRunLine();
                Assert.That(root.RunAdventurerInterestForecastLine, Is.EqualTo("ui.run.adventurer_interest_forecast_summary_format"));
                root.SelectNextRunOutcome();
                root.RefreshRunLine();
                Assert.That(root.RunAdventurerInterestForecastLine, Is.EqualTo(string.Empty));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_EmptyFeedback_ClearsStaleAdventurerAttractionLine()
        {
            var go = new GameObject("GameRootAttractionStaleTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-a",
                                ReasonKey = "run.reason.success",
                                FeedbackTagKeys = new[] { "run.feedback.success" },
                                AdventurerAttractionSummary = new RunAdventurerAttractionSummary { RuleResolved = true }
                            },
                            new RunOutcomeRecord
                            {
                                RunId = "run-b",
                                ReasonKey = "run.reason.success",
                                FeedbackTagKeys = new string[0],
                                AdventurerAttractionSummary = null
                            }
                        }
                    }
                });

                root.RefreshRunLine();
                Assert.That(root.RunAdventurerAttractionLine, Is.EqualTo(string.Empty));
                root.SelectPreviousRunOutcome();
                root.RefreshRunLine();
                Assert.That(root.RunAdventurerAttractionLine, Is.EqualTo("ui.run.adventurer_attraction_summary_format"));
                root.SelectNextRunOutcome();
                root.RefreshRunLine();
                Assert.That(root.RunAdventurerAttractionLine, Is.EqualTo(string.Empty));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void SimulateRunOnce_AttachesCoolingSummary_AndAppliesHeat_WhenResolvedAndNonZero()
        {
            var go = new GameObject("GameRootSimulateRunOnceCoolingApplied");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetContent(root, BuildRunDisplayContent());
                SetPrivateField(root, "_runSimulationService", new RunSimulationService(BuildConfig(), BuildLootConfig()));
                SetPrivateField(root, "<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "run_sim_test_applied.json", useAtomicWrites = false }));
                SetPrivateField(root, "<CurrentHeat>k__BackingField", 20d);

                SetSave(root, new SaveData
                {
                    totalTicks = 10,
                    structureRuntime = new StructureRuntimeState { Heat = 20d, ManaReserve = 50d, IsHeatCrisisActive = false },
                    runHistory = new RunHistoryState { NextRunSequence = 1 }
                });

                bool ok = root.SimulateRunOnce();
                Assert.That(ok, Is.True);
                RunOutcomeRecord latest = root.Save.runHistory.LatestOutcome;
                Assert.That(latest, Is.Not.Null);
                Assert.That(latest.LootHeatCoolingSummary, Is.Not.Null);
                Assert.That(latest.LootHeatCoolingSummary.RuleResolved, Is.True);
                Assert.That(latest.LootHeatCoolingSummary.AppliedHeatDelta, Is.LessThan(0d));
                Assert.That(latest.RunHeatApplicationSummary, Is.Not.Null);
                Assert.That(latest.RunHeatApplicationSummary.RuleResolved, Is.True);
                Assert.That(latest.RunHeatApplicationSummary.HeatBefore, Is.EqualTo(20d));
                Assert.That(latest.LootHeatCoolingSummary.HeatBeforeCooling, Is.EqualTo(latest.RunHeatApplicationSummary.HeatAfter).Within(1e-9));
                Assert.That(latest.LootHeatCoolingSummary.HeatAfterCooling, Is.EqualTo(latest.LootHeatCoolingSummary.HeatBeforeCooling + latest.LootHeatCoolingSummary.AppliedHeatDelta).Within(1e-9));
                Assert.That(root.CurrentHeat, Is.EqualTo(latest.LootHeatCoolingSummary.HeatAfterCooling).Within(1e-9));
                Assert.That(root.Save.structureRuntime.Heat, Is.EqualTo(root.CurrentHeat).Within(1e-9));
                Assert.That(root.CurrentHeatTierLine, Does.Contain($"heat={root.CurrentHeat:0.###}"));
                Assert.That(root.CurrentHeatTierLine, Does.Contain("tier=heat_tier.notice"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void SimulateRunOnce_DoesNotChangeHeat_WhenExtractionMissingOrFailed()
        {
            var go = new GameObject("GameRootSimulateRunOnceCoolingNotApplied");
            try
            {
                RunSimulationConfig config = BuildConfig();
                config.LootTableId = string.Empty;

                var root = go.AddComponent<GameRoot>();
                SetPrivateField(root, "_runSimulationService", new RunSimulationService(config, BuildLootConfig()));
                SetPrivateField(root, "<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "run_sim_test_not_applied.json", useAtomicWrites = false }));
                SetPrivateField(root, "<CurrentHeat>k__BackingField", 20d);
                SetSave(root, new SaveData
                {
                    totalTicks = 10,
                    structureRuntime = new StructureRuntimeState { Heat = 20d, ManaReserve = 50d, IsHeatCrisisActive = false },
                    runHistory = new RunHistoryState { NextRunSequence = 1 }
                });

                bool ok = root.SimulateRunOnce();
                Assert.That(ok, Is.True);
                RunOutcomeRecord latest = root.Save.runHistory.LatestOutcome;
                Assert.That(latest.LootHeatCoolingSummary, Is.Not.Null);
                Assert.That(latest.LootHeatCoolingSummary.RuleResolved, Is.False);
                Assert.That(latest.LootHeatCoolingSummary.DeterministicErrorCode, Is.EqualTo((int)RunLootHeatCoolingSummaryErrorCode.ExtractionSummaryMissingOrFailed));
                Assert.That(root.CurrentHeat, Is.EqualTo(20d));
                Assert.That(root.Save.structureRuntime.Heat, Is.EqualTo(20d));
                Assert.That(latest.LootHeatCoolingSummary.HeatAfterCooling, Is.EqualTo(20d));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void SimulateRunOnce_CoolingClampToZero_RecordsActualAppliedDelta()
        {
            var go = new GameObject("GameRootSimulateRunOnceCoolingClamp");
            try
            {
                RunSimulationConfig config = BuildConfig();
                config.LootHeatCoolingPerTradeableWorldValue = 10d;
                config.MaxLootHeatCoolingPerRun = 100d;

                var root = go.AddComponent<GameRoot>();
                SetPrivateField(root, "_runSimulationService", new RunSimulationService(config, BuildLootConfig()));
                SetPrivateField(root, "<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "run_sim_test_clamp.json", useAtomicWrites = false }));
                SetPrivateField(root, "<CurrentHeat>k__BackingField", 20d);
                SetSave(root, new SaveData
                {
                    totalTicks = 10,
                    structureRuntime = new StructureRuntimeState { Heat = 20d, ManaReserve = 50d, IsHeatCrisisActive = false },
                    runHistory = new RunHistoryState { NextRunSequence = 1 }
                });

                bool ok = root.SimulateRunOnce();
                Assert.That(ok, Is.True);
                RunOutcomeRecord latest = root.Save.runHistory.LatestOutcome;
                Assert.That(latest.RunHeatApplicationSummary.RuleResolved, Is.True);
                Assert.That(latest.RunHeatApplicationSummary.HeatBefore, Is.EqualTo(20d));
                Assert.That(latest.LootHeatCoolingSummary.HeatBeforeCooling, Is.EqualTo(latest.RunHeatApplicationSummary.HeatAfter).Within(1e-9));
                Assert.That(latest.LootHeatCoolingSummary.HeatBeforeCooling, Is.GreaterThan(0d));
                Assert.That(root.CurrentHeat, Is.EqualTo(0d));
                Assert.That(root.Save.structureRuntime.Heat, Is.EqualTo(0d));
                Assert.That(latest.LootHeatCoolingSummary.HeatAfterCooling, Is.EqualTo(0d));
                Assert.That(latest.LootHeatCoolingSummary.AppliedHeatDelta, Is.EqualTo(-latest.LootHeatCoolingSummary.HeatBeforeCooling).Within(1e-9));
                Assert.That(latest.LootHeatCoolingSummary.UnclampedHeatDelta, Is.LessThan(latest.LootHeatCoolingSummary.AppliedHeatDelta));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test]
        public void SimulateOnce_SurvivalSummary_MaxPartyAboveAllowed_ReturnsInvalidPartySizeRange()
        {
            RunSimulationConfig invalid = BuildConfig();
            invalid.MaxAllowedPartySize = 4;
            invalid.MaxPartySize = 5;
            var service = new RunSimulationService(invalid);

            RunOutcomeRecord outcome = service.SimulateOnce(new StructureRuntimeState { Heat = 0d, ManaReserve = 50d, IsHeatCrisisActive = false }, 14, 10);

            Assert.That(outcome.SurvivalSummary, Is.Not.Null);
            Assert.That(outcome.SurvivalSummary.RuleResolved, Is.False);
            Assert.That(outcome.SurvivalSummary.DeterministicErrorCode, Is.EqualTo((int)RunSurvivalSummaryErrorCode.InvalidPartySizeRange));
        }

        [Test]
        public void AppendOutcome_IncrementsHistory_AndKeepsOldestToNewestOrder()
        {
            var history = new RunHistoryState();
            history.AppendOutcome(new RunOutcomeRecord { RunId = "run-1" }, 10);
            history.AppendOutcome(new RunOutcomeRecord { RunId = "run-2" }, 10);

            Assert.That(history.RecentOutcomes.Length, Is.EqualTo(2));
            Assert.That(history.RecentOutcomes[0].RunId, Is.EqualTo("run-1"));
            Assert.That(history.RecentOutcomes[1].RunId, Is.EqualTo("run-2"));
            Assert.That(history.LatestOutcome.RunId, Is.EqualTo("run-2"));
        }

        [Test]
        public void AppendOutcome_TrimsHistory_Deterministically()
        {
            var history = new RunHistoryState();
            history.AppendOutcome(new RunOutcomeRecord { RunId = "run-1" }, 2);
            history.AppendOutcome(new RunOutcomeRecord { RunId = "run-2" }, 2);
            history.AppendOutcome(new RunOutcomeRecord { RunId = "run-3" }, 2);

            Assert.That(history.RecentOutcomes.Length, Is.EqualTo(2));
            Assert.That(history.RecentOutcomes[0].RunId, Is.EqualTo("run-2"));
            Assert.That(history.RecentOutcomes[1].RunId, Is.EqualTo("run-3"));
            Assert.That(history.LatestOutcome.RunId, Is.EqualTo("run-3"));
        }

        [Test]
        public void SaveMigration_SeedsRecentOutcomes_FromLegacyLatestOutcome()
        {
            var root = new SaveRoot
            {
                schemaVersion = 2,
                primary = new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        NextRunSequence = 5,
                        LatestOutcome = new RunOutcomeRecord { RunId = "run-4", ReasonKey = "run.reason.success" }
                    }
                }
            };

            SaveRoot migrated = SaveMigration.MigrateToLatest(root);
            Assert.That(migrated.primary.runHistory.RecentOutcomes.Length, Is.EqualTo(1));
            Assert.That(migrated.primary.runHistory.RecentOutcomes[0].RunId, Is.EqualTo("run-4"));
        }

        [Test]
        public void RefreshRunLine_DefaultsSelection_ToLatestOutcome()
        {
            var go = new GameObject("GameRootRunSelectionDefaultLatest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord { RunId = "run-1", Success = false, Score = 0, ReasonKey = "run.reason.failed_threshold" },
                            new RunOutcomeRecord { RunId = "run-2", Success = true, Score = 120, ReasonKey = "run.reason.success", HasBreakdown = true, FinalChance = 0.8d, SuccessThresholdUsed = 0.5d, FeedbackTagKeys = new[] { "run.feedback.success" } }
                        }
                    }
                });
                SetContent(root, BuildRunDisplayContent());

                root.RefreshRunLine();
                Assert.That(root.RunLine, Does.Contain("run-2"));
                Assert.That(root.RunHistoryLine, Does.Contain("2/2"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RunOutcomeSelection_PreviousNextAndBounds_AreDeterministicAndSafe()
        {
            var go = new GameObject("GameRootRunSelectionBounds");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord { RunId = "run-1", ReasonKey = "run.reason.success" },
                            new RunOutcomeRecord { RunId = "run-2", ReasonKey = "run.reason.success" },
                            new RunOutcomeRecord { RunId = "run-3", ReasonKey = "run.reason.success" }
                        }
                    }
                });
                SetContent(root, BuildRunDisplayContent());

                root.RefreshRunLine();
                Assert.That(root.RunLine, Does.Contain("run-3"));
                Assert.That(root.SelectPreviousRunOutcome(), Is.True);
                root.RefreshRunLine();
                Assert.That(root.RunLine, Does.Contain("run-2"));
                Assert.That(root.SelectPreviousRunOutcome(), Is.True);
                root.RefreshRunLine();
                Assert.That(root.RunLine, Does.Contain("run-1"));
                Assert.That(root.SelectPreviousRunOutcome(), Is.False);
                Assert.That(root.SelectNextRunOutcome(), Is.True);
                Assert.That(root.SelectNextRunOutcome(), Is.True);
                Assert.That(root.SelectNextRunOutcome(), Is.False);
                root.RefreshRunLine();
                Assert.That(root.RunLine, Does.Contain("run-3"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void SelectLatestRunOutcome_SelectsNewestFromOlderSelection()
        {
            var go = new GameObject("GameRootRunSelectionLatest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord { RunId = "run-1", ReasonKey = "run.reason.success" },
                            new RunOutcomeRecord { RunId = "run-2", ReasonKey = "run.reason.success" }
                        }
                    }
                });
                SetContent(root, BuildRunDisplayContent());

                root.RefreshRunLine();
                root.SelectPreviousRunOutcome();
                root.RefreshRunLine();
                Assert.That(root.RunLine, Does.Contain("run-1"));
                Assert.That(root.SelectLatestRunOutcome(), Is.True);
                root.RefreshRunLine();
                Assert.That(root.RunLine, Does.Contain("run-2"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RunOutcomeSelection_EmptyHistory_IsSafe()
        {
            var go = new GameObject("GameRootRunSelectionEmpty");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData { runHistory = new RunHistoryState() });

                root.RefreshRunLine();
                Assert.That(root.SelectPreviousRunOutcome(), Is.False);
                Assert.That(root.SelectNextRunOutcome(), Is.False);
                Assert.That(root.SelectLatestRunOutcome(), Is.False);
                Assert.That(root.RunLine, Is.EqualTo("ui.run.none"));
            }
            finally { Object.DestroyImmediate(go); }
        }


        [Test]
        public void IsValidRunSimulationConfig_Rejects_InvalidAdventurerDemandBudgetConfig()
        {
            RunSimulationConfig config = BuildConfig();
            config.AdventurerDemandBudgetRuleSourceId = " ";
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.AdventurerDemandBudgetLowThreshold = -1d;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.AdventurerDemandBudgetMediumThreshold = double.NaN;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.AdventurerDemandBudgetHighThreshold = double.PositiveInfinity;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.AdventurerDemandBudgetScorePerForecastScore = -0.1d;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.AdventurerDemandBudgetLowThreshold = 11d;
            config.AdventurerDemandBudgetMediumThreshold = 10d;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);

            config = BuildConfig();
            config.AdventurerDemandBudgetMediumThreshold = 10d;
            config.AdventurerDemandBudgetHighThreshold = 9d;
            Assert.That(GameRoot.IsValidRunSimulationConfig(config), Is.False);
        }

        [Test]
        public void SimulateOnce_Attaches_ResolvedAdventurerDemandBudgetSummary()
        {
            RunSimulationConfig config = BuildConfig();
            var service = new RunSimulationService(config, BuildLootConfig());
            RunOutcomeRecord outcome = service.SimulateOnce(new StructureRuntimeState { Heat = 0d, ManaReserve = 50d, IsHeatCrisisActive = false }, 10, 2);

            Assert.That(outcome.AdventurerDemandBudgetSummary, Is.Not.Null);
            Assert.That(outcome.AdventurerDemandBudgetSummary.RuleResolved, Is.True);
            Assert.That(outcome.AdventurerDemandBudgetSummary.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerDemandBudgetSummaryErrorCode.None));
            Assert.That(outcome.AdventurerDemandBudgetSummary.ForecastInterestScoreUsed, Is.EqualTo(outcome.AdventurerInterestForecastSummary.ForecastInterestScore));
            Assert.That(outcome.AdventurerDemandBudgetSummary.ForecastBandIdUsed, Is.EqualTo(outcome.AdventurerInterestForecastSummary.ForecastBandId));

            double expectedDemandScore = outcome.AdventurerDemandBudgetSummary.ForecastInterestScoreUsed * config.AdventurerDemandBudgetScorePerForecastScore;
            Assert.That(outcome.AdventurerDemandBudgetSummary.DemandBudgetScore, Is.EqualTo(expectedDemandScore));

            string expectedBand = "adventurer_demand.none";
            if (expectedDemandScore >= config.AdventurerDemandBudgetHighThreshold)
            {
                expectedBand = "adventurer_demand.high";
            }
            else if (expectedDemandScore >= config.AdventurerDemandBudgetMediumThreshold)
            {
                expectedBand = "adventurer_demand.medium";
            }
            else if (expectedDemandScore >= config.AdventurerDemandBudgetLowThreshold)
            {
                expectedBand = "adventurer_demand.low";
            }

            Assert.That(outcome.AdventurerDemandBudgetSummary.DemandBudgetBandId, Is.EqualTo(expectedBand));
        }

        [Test]
        public void SimulateOnce_AttachesResolvedRunHeatDeltaSummary_WithoutApplyingEliteInference()
        {
            RunSimulationConfig config = BuildConfig();
            config.MinPartySize = 3;
            config.MaxPartySize = 3;
            config.FailureSurvivorRatio = 0d;
            config.AdventurerDemandBudgetLowThreshold = 0d;
            config.AdventurerDemandBudgetMediumThreshold = 0d;
            config.AdventurerDemandBudgetHighThreshold = 0d;
            var service = new RunSimulationService(config, BuildLootConfig());

            RunOutcomeRecord outcome = service.SimulateOnce(
                new StructureRuntimeState { Heat = 100d, ManaReserve = 0d, IsHeatCrisisActive = true },
                10,
                3);

            Assert.That(outcome.AdventurerDemandBudgetSummary.DemandBudgetBandId, Is.EqualTo("adventurer_demand.high"));
            Assert.That(outcome.RunHeatDeltaSummary, Is.Not.Null);
            Assert.That(outcome.RunHeatDeltaSummary.RuleResolved, Is.True);
            Assert.That(outcome.RunHeatDeltaSummary.RuleSourceIdUsed, Is.EqualTo(config.RunHeatDeltaRuleSourceId));
            Assert.That(outcome.RunHeatDeltaSummary.DeterministicSeed, Is.EqualTo(outcome.LootSummary.ResolverSeed));
            Assert.That(outcome.RunHeatDeltaSummary.DeathHeatDelta, Is.EqualTo(3d));
            Assert.That(outcome.RunHeatDeltaSummary.EliteDeathHeatDelta, Is.EqualTo(0d));
            Assert.That(outcome.RunHeatDeltaSummary.SurvivorCoolingDelta, Is.EqualTo(0d));
            Assert.That(outcome.RunHeatDeltaSummary.LootCoolingDelta, Is.EqualTo(0d));
            Assert.That(outcome.RunHeatDeltaSummary.FinalHeatDelta, Is.EqualTo(4d));
        }

        [Test]
        public void SimulateOnce_AttachesHeatApplicationSummary_AndMutatesRuntimeExactlyOnce()
        {
            RunSimulationConfig config = BuildConfig();
            config.MinPartySize = 3;
            config.MaxPartySize = 3;
            config.FailureSurvivorRatio = 0d;
            var service = new RunSimulationService(config, BuildLootConfig());
            var runtime = new StructureRuntimeState { Heat = 20d, ManaReserve = 0d, IsHeatCrisisActive = true };

            RunOutcomeRecord outcome = service.SimulateOnce(runtime, 10, 3);
            double heatAfterSimulation = runtime.Heat;

            Assert.That(outcome.RunHeatApplicationSummary, Is.Not.Null);
            Assert.That(outcome.RunHeatApplicationSummary.RuleResolved, Is.True);
            Assert.That(outcome.RunHeatApplicationSummary.HeatBefore, Is.EqualTo(20d));
            Assert.That(runtime.Heat, Is.EqualTo(outcome.RunHeatApplicationSummary.HeatAfter));
            Assert.That(runtime.Heat, Is.EqualTo(20d + outcome.RunHeatApplicationSummary.AppliedDelta));

            var rootObject = new GameObject("GameRootHeatApplicationRefreshDoesNotMutateTest");
            try
            {
                var root = rootObject.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    structureRuntime = runtime,
                    runHistory = new RunHistoryState { RecentOutcomes = new[] { outcome } }
                });
                SetContent(root, BuildRunDisplayContent());

                root.RefreshRunLine();
                root.RefreshRunLine();

                Assert.That(runtime.Heat, Is.EqualTo(heatAfterSimulation));
                Assert.That(root.RunHeatApplicationLine, Is.EqualTo("Heat Application: resolved=True error=0 before=20 delta=4 after=24 tierBefore=heat_tier.notice tierAfter=heat_tier.notice tierChanged=False ruleSource=run.heat_application.rule.v1"));
            }
            finally { Object.DestroyImmediate(rootObject); }
        }

        [Test]
        public void RefreshRunLine_HeatApplicationSummary_LegacyOrUnresolvedOutcome_ClearsStaleLineSafely()
        {
            var go = new GameObject("GameRootHeatApplicationLegacyTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetContent(root, BuildRunDisplayContent());
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord { RunHeatApplicationSummary = new RunHeatApplicationSummary { RuleResolved = true, DeterministicErrorCode = (int)RunHeatApplicationSummaryErrorCode.None } },
                            new RunOutcomeRecord { RunHeatApplicationSummary = null },
                            new RunOutcomeRecord { RunHeatApplicationSummary = new RunHeatApplicationSummary { RuleResolved = false } }
                        }
                    }
                });

                root.RefreshRunLine();
                Assert.That(root.RunHeatApplicationLine, Is.EqualTo(string.Empty));
                root.SelectPreviousRunOutcome();
                root.RefreshRunLine();
                Assert.That(root.RunHeatApplicationLine, Is.EqualTo(string.Empty));
                root.SelectPreviousRunOutcome();
                root.RefreshRunLine();
                Assert.That(root.RunHeatApplicationLine, Is.EqualTo("Heat Application: resolved=True error=0 before=0 delta=0 after=0 tierBefore= tierAfter= tierChanged=False ruleSource="));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void DiagnosticsRenderingAndHistoryBrowsing_DoNotMutateHeatOrReapplyOldOutcome()
        {
            var rootObject = new GameObject("GameRootHeatApplicationReadOnlyDiagnosticsTest");
            var overlayObject = new GameObject("BootstrapOverlayHeatApplicationReadOnlyTest");
            var textObject = new GameObject("BootstrapOverlayHeatApplicationReadOnlyTextTest");
            try
            {
                var runtime = new StructureRuntimeState { Heat = 17d };
                var root = rootObject.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    structureRuntime = runtime,
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord { RunId = "old-run", RunHeatApplicationSummary = new RunHeatApplicationSummary { RuleResolved = true, DeterministicErrorCode = (int)RunHeatApplicationSummaryErrorCode.None, HeatBefore = 8d, AppliedDelta = 3d, HeatAfter = 11d } },
                            new RunOutcomeRecord { RunId = "latest-run", RunHeatApplicationSummary = new RunHeatApplicationSummary { RuleResolved = true, DeterministicErrorCode = (int)RunHeatApplicationSummaryErrorCode.None, HeatBefore = 11d, AppliedDelta = 6d, HeatAfter = 17d } }
                        }
                    }
                });
                SetContent(root, BuildRunDisplayContent());

                root.RefreshRunLine();
                Assert.That(runtime.Heat, Is.EqualTo(17d));

                var overlay = overlayObject.AddComponent<BootstrapOverlay>();
                overlay.overlayText = textObject.AddComponent<TextMeshProUGUI>();
                overlay.Bind(root);
                typeof(BootstrapOverlay).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(overlay, null);
                Assert.That(runtime.Heat, Is.EqualTo(17d));

                root.SelectPreviousRunOutcome();
                root.RefreshRunLine();
                Assert.That(runtime.Heat, Is.EqualTo(17d));
                Assert.That(root.RunHeatApplicationLine, Does.Contain("before=8 delta=3 after=11"));
            }
            finally
            {
                Object.DestroyImmediate(textObject);
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void LegacySavedOutcome_DisplayAfterLoad_DoesNotMutateHeat()
        {
            var go = new GameObject("GameRootLegacyHeatApplicationDisplayTest");
            try
            {
                SaveData legacy = JsonUtility.FromJson<SaveData>("{\"structureRuntime\":{\"Heat\":17},\"runHistory\":{\"RecentOutcomes\":[{\"RunId\":\"legacy-run\"}]}}");
                var root = go.AddComponent<GameRoot>();
                SetSave(root, legacy);
                SetContent(root, BuildRunDisplayContent());

                root.RefreshRunLine();

                Assert.That(root.Save.structureRuntime.Heat, Is.EqualTo(17d));
                Assert.That(root.RunHeatApplicationLine, Is.EqualTo(string.Empty));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_AdventurerDemandBudgetSummary_WithNullContent_UsesKeyFallbackSafely()
        {
            var go = new GameObject("GameRootDemandBudgetNullContentTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[] { new RunOutcomeRecord { AdventurerDemandBudgetSummary = new RunAdventurerDemandBudgetSummary { RuleResolved = true } } }
                    }
                });
                SetContent(root, null);
                root.RefreshRunLine();
                Assert.That(root.RunAdventurerDemandBudgetLine, Is.EqualTo("ui.run.adventurer_demand_budget_summary_format"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_AdventurerDemandBudgetSummary_MissingLocalizationKey_UsesKeyFallbackSafely()
        {
            var go = new GameObject("GameRootDemandBudgetMissingKeyTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[] { new RunOutcomeRecord { AdventurerDemandBudgetSummary = new RunAdventurerDemandBudgetSummary { RuleResolved = true } } }
                    }
                });
                SetContent(root, new ContentService());
                root.RefreshRunLine();
                Assert.That(root.RunAdventurerDemandBudgetLine, Is.EqualTo("ui.run.adventurer_demand_budget_summary_format"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_AdventurerDemandBudgetSummary_ValidOutcome_IsDisplayed()
        {
            var go = new GameObject("GameRootDemandBudgetDisplayTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-demand",
                                ReasonKey = "run.reason.success",
                                FeedbackTagKeys = new string[0],
                                AdventurerDemandBudgetSummary = new RunAdventurerDemandBudgetSummary
                                {
                                    RuleResolved = true,
                                    DeterministicErrorCode = 0,
                                    ForecastInterestScoreUsed = 11d,
                                    ForecastBandIdUsed = "adventurer_interest.medium",
                                    DemandBudgetScore = 11d,
                                    DemandBudgetBandId = "adventurer_demand.medium"
                                }
                            }
                        }
                    }
                });
                SetContent(root, BuildRunDisplayContent());

                root.RefreshRunLine();

                StringAssert.Contains("Demand Budget:", root.RunAdventurerDemandBudgetLine);
                StringAssert.Contains("resolved=True", root.RunAdventurerDemandBudgetLine);
                StringAssert.Contains("error=0", root.RunAdventurerDemandBudgetLine);
                StringAssert.Contains("forecastScore=11", root.RunAdventurerDemandBudgetLine);
                StringAssert.Contains("forecastBand=adventurer_interest.medium", root.RunAdventurerDemandBudgetLine);
                StringAssert.Contains("score=11", root.RunAdventurerDemandBudgetLine);
                StringAssert.Contains("band=adventurer_demand.medium", root.RunAdventurerDemandBudgetLine);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_SwitchingOutcomes_UpdatesAndClearsAdventurerDemandBudgetLine()
        {
            var go = new GameObject("GameRootDemandBudgetStaleTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord
                            {
                                RunId = "run-a",
                                ReasonKey = "run.reason.success",
                                FeedbackTagKeys = new[] { "run.feedback.success" },
                                AdventurerDemandBudgetSummary = new RunAdventurerDemandBudgetSummary
                                {
                                    RuleResolved = true,
                                    DeterministicErrorCode = 0,
                                    ForecastInterestScoreUsed = 11d,
                                    ForecastBandIdUsed = "adventurer_interest.low",
                                    DemandBudgetScore = 11d,
                                    DemandBudgetBandId = "adventurer_demand.low"
                                }
                            },
                            new RunOutcomeRecord { RunId = "run-b", ReasonKey = "run.reason.success", FeedbackTagKeys = new[] { "run.feedback.success" }, AdventurerDemandBudgetSummary = null },
                            new RunOutcomeRecord
                            {
                                RunId = "run-c",
                                ReasonKey = "run.reason.success",
                                FeedbackTagKeys = new[] { "run.feedback.success" },
                                AdventurerDemandBudgetSummary = new RunAdventurerDemandBudgetSummary
                                {
                                    RuleResolved = true,
                                    DeterministicErrorCode = 0,
                                    ForecastInterestScoreUsed = 11d,
                                    ForecastBandIdUsed = "adventurer_interest.low",
                                    DemandBudgetScore = 11d,
                                    DemandBudgetBandId = "adventurer_demand.low"
                                }
                            }
                        }
                    }
                });
                SetContent(root, BuildRunDisplayContent());
                root.RefreshRunLine();
                Assert.That(root.RunAdventurerDemandBudgetLine, Is.EqualTo("Demand Budget: resolved=True error=0 forecastScore=11 forecastBand=adventurer_interest.low score=11 band=adventurer_demand.low"));
                root.SelectPreviousRunOutcome();
                root.RefreshRunLine();
                Assert.That(root.RunAdventurerDemandBudgetLine, Is.EqualTo(string.Empty));
                root.SelectPreviousRunOutcome();
                root.RefreshRunLine();
                Assert.That(root.RunAdventurerDemandBudgetLine, Is.EqualTo("Demand Budget: resolved=True error=0 forecastScore=11 forecastBand=adventurer_interest.low score=11 band=adventurer_demand.low"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_RunHeatDeltaSummary_FormatsResolvedSummary()
        {
            var go = new GameObject("GameRootHeatDeltaFormatTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, BuildRunHeatDeltaSave(new RunHeatDeltaSummary
                {
                    RuleResolved = true,
                    DeterministicErrorCode = 0,
                    DeathHeatDelta = 2d,
                    EliteDeathHeatDelta = 3d,
                    MultipleDeathBonusDelta = 1d,
                    SurvivorCoolingDelta = -1.5d,
                    LootCoolingDelta = -0.75d,
                    FinalHeatDelta = 3.75d,
                    RuleSourceIdUsed = "run.heat_delta.rule.test"
                }));
                SetContent(root, BuildRunDisplayContent());

                root.RefreshRunLine();

                Assert.That(root.RunHeatDeltaLine, Is.EqualTo("Run Heat Delta: resolved=True error=0 death=2 elite=3 multi=1 survivorCooling=-1.5 lootCooling=-0.75 final=3.75 ruleSource=run.heat_delta.rule.test"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_RunHeatDeltaSummary_WithNullContent_UsesKeyFallbackSafely()
        {
            var go = new GameObject("GameRootHeatDeltaNullContentTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, BuildRunHeatDeltaSave(new RunHeatDeltaSummary { RuleResolved = true }));
                SetContent(root, null);

                root.RefreshRunLine();

                Assert.That(root.RunHeatDeltaLine, Is.EqualTo("ui.run.heat_delta_summary_format"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_RunHeatDeltaSummary_MissingLocalizationKey_UsesKeyFallbackSafely()
        {
            var go = new GameObject("GameRootHeatDeltaMissingLocalizationTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, BuildRunHeatDeltaSave(new RunHeatDeltaSummary { RuleResolved = true }));
                SetContent(root, new ContentService());

                root.RefreshRunLine();

                Assert.That(root.RunHeatDeltaLine, Is.EqualTo("ui.run.heat_delta_summary_format"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_RunHeatDeltaSummary_LegacyOrUnresolvedOutcome_ClearsLineSafely()
        {
            var go = new GameObject("GameRootHeatDeltaLegacyTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, BuildRunHeatDeltaSave(null));
                root.RefreshRunLine();
                Assert.That(root.RunHeatDeltaLine, Is.EqualTo(string.Empty));

                SetSave(root, BuildRunHeatDeltaSave(new RunHeatDeltaSummary { RuleResolved = false }));
                root.RefreshRunLine();
                Assert.That(root.RunHeatDeltaLine, Is.EqualTo(string.Empty));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_SwitchingFromRunHeatDeltaSummary_ToLegacyOrNullOutcome_ClearsStaleLine()
        {
            var go = new GameObject("GameRootHeatDeltaStaleTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetSave(root, new SaveData
                {
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            BuildRunHeatDeltaOutcome(new RunHeatDeltaSummary { RuleResolved = true }),
                            BuildRunHeatDeltaOutcome(null)
                        }
                    }
                });
                SetContent(root, BuildRunDisplayContent());

                root.RefreshRunLine();
                Assert.That(root.RunHeatDeltaLine, Is.EqualTo(string.Empty));
                root.SelectPreviousRunOutcome();
                root.RefreshRunLine();
                Assert.That(root.RunHeatDeltaLine, Is.EqualTo("Run Heat Delta: resolved=True error=0 death=0 elite=0 multi=0 survivorCooling=0 lootCooling=0 final=0 ruleSource="));
                root.SelectNextRunOutcome();
                root.RefreshRunLine();
                Assert.That(root.RunHeatDeltaLine, Is.EqualTo(string.Empty));

                SetSave(root, new SaveData { runHistory = new RunHistoryState() });
                root.RefreshRunLine();
                Assert.That(root.RunHeatDeltaLine, Is.EqualTo(string.Empty));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void BootstrapOverlay_FullDiagnostics_IncludesRunHeatDeltaAfterCoolingBeforeAttraction()
        {
            AssertBootstrapOverlayIncludesHeatDelta(runDiagnosticsOnly: false);
        }

        [Test]
        public void BootstrapOverlay_RunDiagnosticsOnly_IncludesRunHeatDeltaAfterCoolingBeforeAttraction()
        {
            AssertBootstrapOverlayIncludesHeatDelta(runDiagnosticsOnly: true);
        }

        private static SaveData BuildRunHeatDeltaSave(RunHeatDeltaSummary summary)
        {
            return new SaveData
            {
                runHistory = new RunHistoryState
                {
                    RecentOutcomes = new[] { BuildRunHeatDeltaOutcome(summary) }
                }
            };
        }

        private static RunOutcomeRecord BuildRunHeatDeltaOutcome(RunHeatDeltaSummary summary)
        {
            return new RunOutcomeRecord
            {
                RunId = "run-heat-delta",
                ReasonKey = "run.reason.success",
                FeedbackTagKeys = new string[0],
                RunHeatDeltaSummary = summary
            };
        }

        private static void AssertBootstrapOverlayIncludesHeatDelta(bool runDiagnosticsOnly)
        {
            var rootObject = new GameObject("GameRootHeatDeltaOverlayTest");
            var overlayObject = new GameObject("BootstrapOverlayHeatDeltaTest");
            var textObject = new GameObject("BootstrapOverlayHeatDeltaTextTest");
            try
            {
                var root = rootObject.AddComponent<GameRoot>();
                typeof(GameRoot).GetField("<RunHeatCoolingLine>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(root, "cooling-line");
                typeof(GameRoot).GetField("<RunHeatDeltaLine>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(root, "heat-delta-line");
                typeof(GameRoot).GetField("<RunHeatApplicationLine>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(root, "heat-application-line");
                typeof(GameRoot).GetField("<RunAdventurerAttractionLine>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(root, "attraction-line");

                var overlay = overlayObject.AddComponent<BootstrapOverlay>();
                overlay.overlayText = textObject.AddComponent<TextMeshProUGUI>();
                overlay.Bind(root);
                typeof(BootstrapOverlay).GetField("_runDiagnosticsOnlyVisible", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(overlay, runDiagnosticsOnly);

                typeof(BootstrapOverlay).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(overlay, null);

                string text = overlay.overlayText.text;
                int coolingIndex = text.IndexOf("cooling-line", System.StringComparison.Ordinal);
                int heatDeltaIndex = text.IndexOf("heat-delta-line", System.StringComparison.Ordinal);
                int heatApplicationIndex = text.IndexOf("heat-application-line", System.StringComparison.Ordinal);
                int attractionIndex = text.IndexOf("attraction-line", System.StringComparison.Ordinal);
                Assert.That(coolingIndex, Is.GreaterThanOrEqualTo(0));
                Assert.That(heatDeltaIndex, Is.GreaterThan(coolingIndex));
                Assert.That(heatApplicationIndex, Is.GreaterThan(heatDeltaIndex));
                Assert.That(attractionIndex, Is.GreaterThan(heatApplicationIndex));
            }
            finally
            {
                Object.DestroyImmediate(textObject);
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(rootObject);
            }
        }

    }
}
