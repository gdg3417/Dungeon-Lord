using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpPlayerLoopSummaryPresenterTests
    {
        private const string SlotId = "research.slot.primary";
        private const string ProjectId = "research.project.mvp_loop";
        private const string StructureId = "structure.mana_generator.basic";

        [Test]
        public void Resolve_DoesNotMutateSaveOrNestedRuntimeState()
        {
            SaveData save = FullSave();
            string before = JsonUtility.ToJson(save);
            string runtimeBefore = JsonUtility.ToJson(save.structureRuntime);
            string historyBefore = JsonUtility.ToJson(save.runHistory);
            string pendingBefore = JsonUtility.ToJson(save.researchPending);
            string progressBefore = JsonUtility.ToJson(save.researchProgress);
            string completedBefore = JsonUtility.ToJson(save.completedResearch);

            MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig());

            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
            Assert.That(JsonUtility.ToJson(save.structureRuntime), Is.EqualTo(runtimeBefore));
            Assert.That(JsonUtility.ToJson(save.runHistory), Is.EqualTo(historyBefore));
            Assert.That(JsonUtility.ToJson(save.researchPending), Is.EqualTo(pendingBefore));
            Assert.That(JsonUtility.ToJson(save.researchProgress), Is.EqualTo(progressBefore));
            Assert.That(JsonUtility.ToJson(save.completedResearch), Is.EqualTo(completedBefore));
        }

        [Test]
        public void Resolve_ReturnsDeterministicOutputForIdenticalInput()
        {
            SaveData save = FullSave();

            string first = JsonUtility.ToJson(MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig()));
            string second = JsonUtility.ToJson(MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig()));

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Resolve_NoRunHistory_ReturnsSafeRunSuggestion()
        {
            SaveData save = FullSave();
            save.runHistory = new RunHistoryState();

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig());

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.HasRunOutcome, Is.False);
            Assert.That(summary.LatestRunId, Is.Empty);
            Assert.That(summary.NextOptimizationSuggestionKey, Is.EqualTo(MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey));
            AssertSafetyFlags(summary);
        }

        [Test]
        public void Resolve_LatestRunMissingOptionalSummaries_ReturnsSafeDefaults()
        {
            SaveData save = FullSave();
            save.runHistory = new RunHistoryState
            {
                LatestOutcome = new RunOutcomeRecord
                {
                    RunId = "run.missing_optional",
                    Success = false,
                    HeatAtStart = 7d,
                    ManaAtStart = 3d
                }
            };

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig());

            Assert.That(summary.HasRunOutcome, Is.True);
            Assert.That(summary.LatestRunId, Is.EqualTo("run.missing_optional"));
            Assert.That(summary.LootGeneratedWorldValue, Is.EqualTo(0));
            Assert.That(summary.LootExtractedWorldValue, Is.EqualTo(0));
            Assert.That(summary.LootExtractedTradeableWorldValue, Is.EqualTo(0));
            Assert.That(summary.HeatBefore, Is.EqualTo(7d));
            Assert.That(summary.HeatAfter, Is.EqualTo(save.structureRuntime.Heat));
        }

        [Test]
        public void Resolve_UsesStoredLatestRunPlacementEffectsForRunFeedback_WhenCurrentPlacementsChange()
        {
            SaveData save = FullSave();
            save.mvpDungeonPlacements = new MvpDungeonPlacementState();
            save.runHistory.LatestOutcome = RunWithStoredPlacementEffects();

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, PlacementConfig(), EligibilityConfig(), VerificationConfig());
            string feedback = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                new MvpPlayerLoopSummary { RuleResolved = true },
                summary,
                didRun: true,
                LocalizedFeedback);
            string panel = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, LocalizedFeedback);

            Assert.That(MvpPlacementEffectsPresenter.HasAnyEffect(summary.PlacementEffects), Is.False);
            Assert.That(summary.LatestRunPlacementEffects, Is.Not.Null);
            Assert.That(summary.LatestRunPlacementEffects.RuleResolved, Is.True);
            Assert.That(summary.LatestRunPlacementEffects.PathCapacity, Is.EqualTo(2));
            Assert.That(summary.LatestRunPlacementEffects.Danger, Is.EqualTo(5));
            Assert.That(feedback, Does.Contain("Placement impact: path +2, danger +5 (Stored run composition)."));
            Assert.That(panel, Does.Contain("Effects: none"));
            Assert.That(panel, Does.Not.Contain("Stored run composition"));
        }

        [Test]
        public void Resolve_LegacyLatestRunWithoutStoredCompositionFallsBackToCurrentPlacementEffects()
        {
            SaveData save = FullSave();
            save.mvpDungeonPlacements = new MvpDungeonPlacementState
            {
                Entries = new System.Collections.Generic.List<MvpDungeonPlacementEntry>
                {
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId, 1)
                }
            };
            save.runHistory.LatestOutcome = RunWithLoot(generatedWorldValue: 5, extractedWorldValue: 5, extractedTradeableWorldValue: 3);

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, PlacementConfig(), EligibilityConfig(), VerificationConfig());
            string feedback = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                new MvpPlayerLoopSummary { RuleResolved = true },
                summary,
                didRun: true,
                LocalizedFeedback);

            Assert.That(summary.LatestRunPlacementEffects, Is.SameAs(summary.PlacementEffects));
            Assert.That(summary.LatestRunPlacementEffects.PathCapacity, Is.EqualTo(2));
            Assert.That(feedback, Does.Contain("Placement impact: path +2 (Current room composition)."));
        }

        [Test]
        public void Resolve_LatestRunWithLootSummaries_ComposesExtractionValues()
        {
            SaveData save = FullSave();
            save.runHistory.LatestOutcome = RunWithLoot(generatedWorldValue: 15, extractedWorldValue: 9, extractedTradeableWorldValue: 4);

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig());

            Assert.That(summary.LootGeneratedWorldValue, Is.EqualTo(15));
            Assert.That(summary.LootExtractedWorldValue, Is.EqualTo(9));
            Assert.That(summary.LootExtractedTradeableWorldValue, Is.EqualTo(4));
        }

        [Test]
        public void Resolve_LatestRunWithHeatApplication_ComposesHeatFieldsAndTier()
        {
            SaveData save = FullSave();
            save.structureRuntime.Heat = 12d;
            save.runHistory.LatestOutcome = RunWithHeatApplication(5d, 12d, CurrentHeatTierResolver.NoticeTierId, CurrentHeatTierResolver.ConcernTierId);

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig());

            Assert.That(summary.HeatBefore, Is.EqualTo(5d));
            Assert.That(summary.HeatAfter, Is.EqualTo(12d));
            Assert.That(summary.HeatTierId, Is.EqualTo(CurrentHeatTierResolver.ConcernTierId));
        }

        [Test]
        public void Resolve_ResearchStatusAndVerificationBoundary_ComposesResearchFields()
        {
            SaveData save = FullSave();
            save.researchProgress.ProgressUnits = 3d;
            save.researchProgress.CompletionPending = true;

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig());

            Assert.That(summary.HasResearchStatus, Is.True);
            Assert.That(summary.ResearchProjectId, Is.EqualTo(ProjectId));
            Assert.That(summary.ResearchStatusKey, Is.EqualTo(MvpPlayerLoopSummaryPresenter.ResearchVerificationRequiredKey));
            Assert.That(summary.ResearchVerificationRuleResolved, Is.True);
            Assert.That(summary.ResearchVerificationDeterministicErrorCode, Is.EqualTo((int)ResearchVerificationBoundarySummaryErrorCode.None));
            Assert.That(summary.VerificationRequired, Is.True);
            Assert.That(summary.VerificationAvailable, Is.True);
            Assert.That(summary.CanClaimProduction, Is.False);
        }


        [Test]
        public void Resolve_CompletionPendingWithUnavailableVerification_ReportsResearchUnavailableAndDoesNotSuggestVerify()
        {
            SaveData save = FullSave();
            save.researchProgress.ProgressUnits = 3d;
            save.researchProgress.CompletionPending = true;

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), UnavailableVerificationConfig());

            Assert.That(summary.HasResearchStatus, Is.True);
            Assert.That(summary.ResearchStatusKey, Is.EqualTo(MvpPlayerLoopSummaryPresenter.ResearchUnavailableKey));
            Assert.That(summary.ResearchVerificationRuleResolved, Is.False);
            Assert.That(summary.ResearchVerificationDeterministicErrorCode, Is.EqualTo((int)ResearchVerificationBoundarySummaryErrorCode.UnavailableVerificationMode));
            Assert.That(summary.VerificationRequired, Is.False);
            Assert.That(summary.VerificationAvailable, Is.False);
            Assert.That(summary.NextOptimizationSuggestionKey, Is.EqualTo(MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey));
            AssertSafetyFlags(summary);
        }

        [Test]
        public void Resolve_SafetyFlagsRemainFalse()
        {
            MvpPlayerLoopSummary[] summaries =
            {
                MvpPlayerLoopSummaryPresenter.Resolve(null, HeatConfig(), EligibilityConfig(), VerificationConfig()),
                MvpPlayerLoopSummaryPresenter.Resolve(new SaveData(), HeatConfig(), EligibilityConfig(), VerificationConfig()),
                MvpPlayerLoopSummaryPresenter.Resolve(FullSave(), HeatConfig(), EligibilityConfig(), VerificationConfig())
            };

            foreach (MvpPlayerLoopSummary summary in summaries)
            {
                AssertSafetyFlags(summary);
            }
        }

        [Test]
        public void Resolve_SuggestionKeySelectionIsDeterministic()
        {
            SaveData noRun = FullSave();
            noRun.runHistory = new RunHistoryState();
            AssertSuggestion(noRun, MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey);

            SaveData verification = FullSave();
            verification.researchProgress.CompletionPending = true;
            AssertSuggestion(verification, MvpPlayerLoopSummaryPresenter.SuggestVerifyResearchStatusKey);

            SaveData concern = FullSave();
            concern.researchProgress.CompletionPending = false;
            concern.structureRuntime.Heat = 12d;
            concern.runHistory.LatestOutcome = RunWithHeatApplication(7d, 12d, CurrentHeatTierResolver.NoticeTierId, CurrentHeatTierResolver.ConcernTierId);
            AssertSuggestion(concern, MvpPlayerLoopSummaryPresenter.SuggestReduceHeatPressureKey);

            SaveData lootLost = FullSave();
            lootLost.researchProgress.CompletionPending = false;
            lootLost.runHistory.LatestOutcome = RunWithLoot(generatedWorldValue: 5, extractedWorldValue: 0, extractedTradeableWorldValue: 0);
            AssertSuggestion(lootLost, MvpPlayerLoopSummaryPresenter.SuggestImproveSurvivabilityOrLayoutKey);

            SaveData repeat = FullSave();
            repeat.researchProgress.CompletionPending = false;
            repeat.runHistory.LatestOutcome = RunWithLoot(generatedWorldValue: 5, extractedWorldValue: 3, extractedTradeableWorldValue: 2);
            AssertSuggestion(repeat, MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey);
        }

        private static void AssertSuggestion(SaveData save, string expectedKey)
        {
            string first = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig()).NextOptimizationSuggestionKey;
            string second = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig()).NextOptimizationSuggestionKey;

            Assert.That(first, Is.EqualTo(expectedKey));
            Assert.That(second, Is.EqualTo(first));
        }

        private static void AssertSafetyFlags(MvpPlayerLoopSummary summary)
        {
            Assert.That(summary.WouldMutateState, Is.False);
            Assert.That(summary.WouldGrantRewards, Is.False);
            Assert.That(summary.WouldUnlockContent, Is.False);
            Assert.That(summary.WouldCallServer, Is.False);
            Assert.That(summary.WouldProcessOfflineProgress, Is.False);
        }

        private static SaveData FullSave()
        {
            DungeonLayoutState layout = DungeonLayoutState.CreateEmpty(1, 2);
            layout.Slots[1] = new DungeonSlot(0, 1, StructureId);

            return new SaveData
            {
                totalTicks = 41,
                dungeonLayout = layout,
                structureRuntime = new StructureRuntimeState
                {
                    ManaReserve = 23d,
                    Heat = 6d,
                    LastProcessedTick = 11,
                    IsHeatCrisisActive = false
                },
                runHistory = new RunHistoryState
                {
                    NextRunSequence = 3,
                    LatestOutcome = RunWithLoot(generatedWorldValue: 10, extractedWorldValue: 8, extractedTradeableWorldValue: 3),
                    RecentOutcomes = new[] { new RunOutcomeRecord { RunId = "run.old" } }
                },
                researchPending = Pending(),
                researchProgress = Progress(1d, false),
                completedResearch = new CompletedResearchState { ProjectIds = new[] { "research.project.completed" } }
            };
        }

        private static RunOutcomeRecord RunWithStoredPlacementEffects()
        {
            RunOutcomeRecord outcome = RunWithLoot(generatedWorldValue: 12, extractedWorldValue: 10, extractedTradeableWorldValue: 6);
            outcome.CompositionOutcomeSummary = new RunCompositionOutcomeSummary
            {
                RuleResolved = true,
                RuleSourceId = "test.run.composition",
                PlacementEffects = new MvpPlacementEffectsSummary
                {
                    RuleResolved = true,
                    RuleSourceId = "test.placement.effects",
                    PathCapacity = 2,
                    Danger = 5,
                    EffectLocalizationKeys = new[] { "effect.stored" }
                }
            };
            return outcome;
        }

        private static RunOutcomeRecord RunWithLoot(int generatedWorldValue, int extractedWorldValue, int extractedTradeableWorldValue)
        {
            return new RunOutcomeRecord
            {
                RunId = "run.loot",
                Success = true,
                HeatAtStart = 4d,
                ManaAtStart = 10d,
                LootSummary = new RunLootSummary
                {
                    ResolverSuccess = true,
                    TotalGeneratedWorldValue = generatedWorldValue,
                    TotalGeneratedTradeableWorldValue = generatedWorldValue
                },
                LootExtractionSummary = new RunLootExtractionSummary
                {
                    RuleResolved = true,
                    TotalExtractedWorldValue = extractedWorldValue,
                    TotalExtractedTradeableWorldValue = extractedTradeableWorldValue
                },
                RunHeatApplicationSummary = new RunHeatApplicationSummary
                {
                    RuleResolved = true,
                    HeatBefore = 4d,
                    AppliedDelta = 2d,
                    HeatAfter = 6d,
                    TierBefore = CurrentHeatTierResolver.PeaceTierId,
                    TierAfter = CurrentHeatTierResolver.NoticeTierId
                }
            };
        }

        private static RunOutcomeRecord RunWithHeatApplication(double heatBefore, double heatAfter, string tierBefore, string tierAfter)
        {
            return new RunOutcomeRecord
            {
                RunId = "run.heat",
                Success = true,
                HeatAtStart = heatBefore,
                RunHeatApplicationSummary = new RunHeatApplicationSummary
                {
                    RuleResolved = true,
                    HeatBefore = heatBefore,
                    AppliedDelta = heatAfter - heatBefore,
                    HeatAfter = heatAfter,
                    TierBefore = tierBefore,
                    TierAfter = tierAfter,
                    TierChanged = tierBefore != tierAfter
                }
            };
        }

        private static ResearchPendingState Pending()
        {
            return new ResearchPendingState { SlotId = SlotId, ProjectId = ProjectId };
        }

        private static ResearchProgressState Progress(double progressUnits, bool completionPending)
        {
            return new ResearchProgressState
            {
                SlotId = SlotId,
                ProjectId = ProjectId,
                ProgressUnits = progressUnits,
                CompletionPending = completionPending,
                RuleSourceIdUsed = "test.research.progress"
            };
        }

        private static ResearchCompletionEligibilityScaffoldConfig EligibilityConfig()
        {
            return new ResearchCompletionEligibilityScaffoldConfig
            {
                enabled = true,
                projectId = ProjectId,
                requiredProgressUnits = 2d,
                ruleSourceId = "test.research.eligibility"
            };
        }

        private static ResearchVerificationScaffoldConfig VerificationConfig()
        {
            return new ResearchVerificationScaffoldConfig
            {
                enabled = true,
                ruleSourceId = "test.research.verification",
                verificationMode = ResearchVerificationBoundaryResolver.LocalDevPlaceholderVerificationMode
            };
        }


        private static ResearchVerificationScaffoldConfig UnavailableVerificationConfig()
        {
            return new ResearchVerificationScaffoldConfig
            {
                enabled = true,
                ruleSourceId = "test.research.verification",
                verificationMode = ResearchVerificationBoundaryResolver.UnavailableVerificationMode
            };
        }

        private static RunSimulationConfig PlacementConfig()
        {
            RunSimulationConfig config = HeatConfig();
            config.MvpPlacementEffectsRuleSourceId = "test.placement.effects";
            config.MvpPlacementEffects = new[]
            {
                new MvpPlacementEffectConfig
                {
                    CategoryId = MvpDungeonPlacementIds.RoomCategoryId,
                    OptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                    PathCapacity = 2,
                    ExplanationKey = "effect.current_room"
                }
            };
            return config;
        }

        private static string LocalizedFeedback(string key, string fallback)
        {
            var map = new System.Collections.Generic.Dictionary<string, string>
            {
                [MvpLoopSummaryPanelPresenter.TitleKey] = "Loop",
                [MvpLoopSummaryPanelPresenter.CompositionFormatKey] = "Composition: {0}",
                [MvpLoopSummaryPanelPresenter.LatestRunFormatKey] = "Run: {0}",
                [MvpLoopSummaryPanelPresenter.PlacementEffectsFormatKey] = "Effects: {0}",
                [MvpLoopSummaryPanelPresenter.ManaFormatKey] = "Mana: {0}",
                [MvpLoopSummaryPanelPresenter.LootFormatKey] = "Loot: {0}/{1}/{2}",
                [MvpLoopSummaryPanelPresenter.HeatFormatKey] = "Heat: {0}->{1} {2}",
                [MvpLoopSummaryPanelPresenter.ResearchFormatKey] = "Research: {0}",
                [MvpLoopSummaryPanelPresenter.SuggestionFormatKey] = "Suggestion: {0}",
                [MvpLoopSummaryPanelPresenter.ValueNoPlacementKey] = "No placements",
                [MvpLoopSummaryPanelPresenter.ValueNoRunKey] = "No run",
                [MvpLoopSummaryPanelPresenter.ValueUnknownKey] = "Unknown",
                [MvpLoopSummaryPanelPresenter.ValueNoResearchKey] = "No research",
                [MvpLoopSummaryPanelPresenter.RunSucceededKey] = "Succeeded",
                [MvpLoopSummaryPanelPresenter.RunFailedKey] = "Failed",
                [MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey] = "Repeat",
                [MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey] = "Run dungeon",
                [MvpPlacementEffectsPresenter.EmptyKey] = "none",
                [MvpPlacementEffectsPresenter.DetailSeparatorKey] = ", ",
                [MvpPlacementEffectsPresenter.PathCapacityFormatKey] = "path +{0}",
                [MvpPlacementEffectsPresenter.DangerFormatKey] = "danger +{0}",
                [MvpPlacementEffectsPresenter.ExplanationFormatKey] = "{0} ({1})",
                [MvpDungeonPlacementPresenter.RoomCategoryKey] = "Room",
                [MvpDungeonPlacementPresenter.BasicRoomOptionKey] = "Basic Room",
                [MvpDungeonPlacementPresenter.EntryFormatKey] = "{0}: {1}",
                [MvpDungeonPlacementPresenter.SeparatorKey] = "; ",
                [MvpRunResultFeedbackPresenter.SuccessStableHeatKey] = "Run result: stable.",
                [MvpRunResultFeedbackPresenter.SuccessHeatIncreasedKey] = "Run result: heated.",
                [MvpRunResultFeedbackPresenter.FailedKey] = "Run result: failed.",
                [MvpRunResultFeedbackPresenter.OutcomeCueControlledLootKey] = "Outcome: loot controlled.",
                [MvpRunResultFeedbackPresenter.OutcomeCueHeatIncreasedKey] = "Outcome: heat increased.",
                [MvpRunResultFeedbackPresenter.OutcomeCueFailedKey] = "Outcome: failed.",
                [MvpRunResultFeedbackPresenter.OutcomeCueFormatKey] = "{0} {1}",
                [MvpRunResultFeedbackPresenter.FormatKey] = "{0} Mana {1}. Loot {2}/{3}/{4}. Heat {5}->{6}.",
                [MvpRunResultFeedbackPresenter.PlacementEffectsImpactFormatKey] = "{0} Placement impact: {1}.",
                ["effect.stored"] = "Stored run composition",
                ["effect.current_room"] = "Current room composition",
                [CurrentHeatTierResolver.NoticeTierId] = "Notice"
            };

            return map.TryGetValue(key, out string value) ? value : fallback;
        }

        private static RunSimulationConfig HeatConfig()
        {
            return new RunSimulationConfig
            {
                HeatPeaceMinimum = 0d,
                HeatPeaceMaximum = 4d,
                HeatNoticeMinimum = 5d,
                HeatNoticeMaximum = 9d,
                HeatConcernMinimum = 10d,
                HeatConcernMaximum = 20d,
                RunHeatApplicationRuleSourceId = "test.heat.tiers"
            };
        }
    }
}
