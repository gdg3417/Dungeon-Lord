using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
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
                LootTableId = "loot.table.run.basic"
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
            var runtime = new StructureRuntimeState { Heat = 10d, ManaReserve = 20d, IsHeatCrisisActive = false };

            RunOutcomeRecord first = service.SimulateOnce(runtime, 50, 1);
            RunOutcomeRecord second = service.SimulateOnce(runtime, 50, 1);

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
            Assert.That(first.HasBreakdown, Is.True);
            Assert.That(second.HasBreakdown, Is.True);
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

    }
}
