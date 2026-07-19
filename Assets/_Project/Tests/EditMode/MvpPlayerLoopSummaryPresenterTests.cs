#if UNITY_EDITOR
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
        [Test]
        public void Resolve_SharedLocalAuthorityOverridesPlayerStatusWithoutClaimingProductionVerification()
        {
            PlayerResearchAuthoritySummary authority = new PlayerResearchAuthoritySummary
            {
                RuleResolved = true,
                State = PlayerResearchAuthorityState.ReadyForLocalMvpClaim,
                FeedbackLocalizationKey = PlayerResearchActionHandler.ReadyToClaimKey,
                CanClaimLocalMvp = true,
                UsesLocalMvpAuthority = true,
                CanClaimProduction = false
            };
            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(
                FullSave(), HeatConfig(), EligibilityConfig(), UnavailableVerificationConfig(), UnlockConfig(), authority);
            Assert.That(summary.ResearchStatusKey, Is.EqualTo(PlayerResearchActionHandler.ReadyToClaimKey));
            Assert.That(summary.CanClaimLocalMvp, Is.True);
            Assert.That(summary.CanClaimProduction, Is.False);
            Assert.That(summary.PlayerResearchAuthority, Is.SameAs(authority));
        }

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
        public void Resolve_CompletedResearch_ComposesUnlockSummary()
        {
            SaveData save = FullSave();
            save.researchPending = null;
            save.researchProgress = null;
            save.completedResearch = new CompletedResearchState { ProjectIds = new[] { ProjectId } };

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig(), UnlockConfig());

            Assert.That(summary.HasResearchUnlock, Is.True);
            Assert.That(summary.ResearchUnlockId, Is.EqualTo("research.unlock.basic_run_analysis"));
            Assert.That(summary.ResearchUnlockSummaryKey, Is.EqualTo("ui.research_unlock.basic_run_analysis.summary"));
            Assert.That(summary.ResearchUnlockDeterministicErrorCode, Is.EqualTo((int)ResearchUnlockSummaryErrorCode.None));
        }

        [Test]
        public void Resolve_CompletedResearchWithResolvedUnlockAndUnavailableStatus_PresentsResearchCompleted()
        {
            SaveData save = FullSave();
            save.researchPending = null;
            save.researchProgress = null;
            save.completedResearch = new CompletedResearchState { ProjectIds = new[] { ProjectId } };

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), null, VerificationConfig(), UnlockConfig());

            Assert.That(summary.HasResearchStatus, Is.True);
            Assert.That(summary.ResearchStatusKey, Is.EqualTo(MvpPlayerLoopSummaryPresenter.ResearchCompletedKey));
            Assert.That(summary.HasResearchUnlock, Is.True);
            Assert.That(summary.ResearchUnlockSummaryKey, Is.EqualTo("ui.research_unlock.basic_run_analysis.summary"));
        }

        [Test]
        public void Resolve_UnknownCompletedResearch_UsesSafeUnlockFallbackWithoutRawId()
        {
            SaveData save = FullSave();
            save.researchPending = null;
            save.researchProgress = null;
            save.completedResearch = new CompletedResearchState { ProjectIds = new[] { "research.project.unknown" } };

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig(), UnlockConfig());
            string serialized = JsonUtility.ToJson(summary);

            Assert.That(summary.HasResearchUnlock, Is.False);
            Assert.That(summary.ResearchUnlockSummaryKey, Is.EqualTo(ResearchUnlockSummaryPresenter.UnavailableKey));
            Assert.That(summary.ResearchUnlockId, Is.Empty);
            Assert.That(serialized, Does.Not.Contain("research.project.unknown"));
        }



        [Test]
        public void Resolve_BasicRunAnalysisUnlockedWithDominantDangerPlacementEffects_ReturnsReduceDangerAdvice()
        {
            SaveData save = SaveWithBasicRunAnalysisUnlocked();
            save.runHistory.LatestOutcome = RunWithLoot(generatedWorldValue: 10, extractedWorldValue: 8, extractedTradeableWorldValue: 3);
            save.runHistory.LatestOutcome.CompositionOutcomeSummary = new RunCompositionOutcomeSummary
            {
                RuleResolved = true,
                PlacementEffects = new MvpPlacementEffectsSummary
                {
                    RuleResolved = true,
                    Danger = 7,
                    PathCapacity = 2,
                    HeatPressure = 1
                }
            };

            AssertAnalysisAdviceFromResolve(save, MvpPlayerLoopSummaryPresenter.SuggestBasicAnalysisReduceDangerKey);
        }

        [Test]
        public void Resolve_BasicRunAnalysisUnlockedWithHeatIncrease_ReturnsReduceHeatAdviceWithoutMutationOrRewards()
        {
            SaveData save = SaveWithBasicRunAnalysisUnlocked();
            save.runHistory.LatestOutcome = RunWithHeatApplication(2d, 7d, CurrentHeatTierResolver.PeaceTierId, CurrentHeatTierResolver.NoticeTierId);

            AssertAnalysisAdviceFromResolve(save, MvpPlayerLoopSummaryPresenter.SuggestBasicAnalysisReduceHeatKey);
        }

        [Test]
        public void Resolve_BasicRunAnalysisUnlockedWithPartialLootExtraction_ReturnsImproveExtractionAdvice()
        {
            SaveData save = SaveWithBasicRunAnalysisUnlocked();
            save.runHistory.LatestOutcome = RunWithLootAndHeat(generatedWorldValue: 12, extractedWorldValue: 5, extractedTradeableWorldValue: 4, heatBefore: 6d, heatAfter: 6d);

            AssertAnalysisAdviceFromResolve(save, MvpPlayerLoopSummaryPresenter.SuggestBasicAnalysisImproveExtractionKey);
        }

        [Test]
        public void Resolve_BasicRunAnalysisUnlockedWithFullLootRecoveryAndStableHeat_ReturnsTestGreedierAdvice()
        {
            SaveData save = SaveWithBasicRunAnalysisUnlocked();
            save.runHistory.LatestOutcome = RunWithLootAndHeat(generatedWorldValue: 9, extractedWorldValue: 9, extractedTradeableWorldValue: 6, heatBefore: 6d, heatAfter: 6d);

            AssertAnalysisAdviceFromResolve(save, MvpPlayerLoopSummaryPresenter.SuggestBasicAnalysisTestGreedierKey);
        }

        [Test]
        public void Resolve_BasicRunAnalysisUnlockedWithNoRun_ReturnsRunForAnalysisAdvice()
        {
            SaveData save = SaveWithBasicRunAnalysisUnlocked();
            save.runHistory = new RunHistoryState();

            AssertAnalysisAdviceFromResolve(save, MvpPlayerLoopSummaryPresenter.SuggestBasicAnalysisNoRunKey);
        }

        [Test]
        public void Resolve_WithoutBasicRunAnalysisUnlock_LeavesAnalysisAdviceEmpty()
        {
            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(FullSave(), HeatConfig(), EligibilityConfig(), VerificationConfig(), UnlockConfig());

            Assert.That(summary.AnalysisUnlocked, Is.False);
            Assert.That(summary.AnalysisAdviceKey, Is.Empty.Or.Null);
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


        [Test]
        public void Resolve_UnresolvedCompositionOutcome_DoesNotShowStoredOrCurrentEffectsAsLatestRunImpact()
        {
            SaveData save = FullSave();
            save.mvpDungeonPlacements = PlacementState(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId);
            save.runHistory.LatestOutcome = RunWithLoot(generatedWorldValue: 10, extractedWorldValue: 8, extractedTradeableWorldValue: 3);
            save.runHistory.LatestOutcome.CompositionOutcomeSummary = new RunCompositionOutcomeSummary
            {
                RuleResolved = false,
                PlacementEffects = new MvpPlacementEffectsSummary
                {
                    RuleResolved = true,
                    Danger = 9,
                    EffectLocalizationKeys = new[] { "effect.stored" }
                }
            };

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, PlacementEffectsConfig(), EligibilityConfig(), VerificationConfig());
            string feedback = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                new MvpPlayerLoopSummary { RuleResolved = true },
                summary,
                didRun: true,
                FeedbackLocalize);

            Assert.That(MvpPlacementEffectsPresenter.HasAnyEffect(summary.PlacementEffects), Is.True);
            Assert.That(summary.PlacementEffects.PathCapacity, Is.EqualTo(2));
            Assert.That(summary.LatestRunPlacementEffects, Is.Not.Null);
            Assert.That(summary.LatestRunPlacementEffects.RuleResolved, Is.True);
            Assert.That(MvpPlacementEffectsPresenter.HasAnyEffect(summary.LatestRunPlacementEffects), Is.False);
            Assert.That(feedback, Does.Not.Contain("stored danger"));
            Assert.That(feedback, Does.Not.Contain("current room"));
            Assert.That(feedback, Does.Not.Contain("Placement impact"));
        }

        [Test]
        public void Resolve_LegacyRunWithoutCompositionOutcome_FallsBackToCurrentPlacementEffectsForLatestRunImpact()
        {
            SaveData save = FullSave();
            save.mvpDungeonPlacements = PlacementState(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId);
            save.runHistory.LatestOutcome = RunWithLoot(generatedWorldValue: 10, extractedWorldValue: 8, extractedTradeableWorldValue: 3);
            save.runHistory.LatestOutcome.CompositionOutcomeSummary = null;

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, PlacementEffectsConfig(), EligibilityConfig(), VerificationConfig());
            string feedback = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                new MvpPlayerLoopSummary { RuleResolved = true },
                summary,
                didRun: true,
                FeedbackLocalize);

            Assert.That(summary.LatestRunPlacementEffects, Is.SameAs(summary.PlacementEffects));
            Assert.That(MvpPlacementEffectsPresenter.HasAnyEffect(summary.LatestRunPlacementEffects), Is.True);
            Assert.That(feedback, Does.Contain("Placement impact"));
            Assert.That(feedback, Does.Contain("current room"));
        }


        private static void AssertAnalysisAdviceFromResolve(SaveData save, string expectedAdviceKey)
        {
            string before = JsonUtility.ToJson(save);

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig(), UnlockConfig());

            Assert.That(summary.AnalysisUnlocked, Is.True);
            Assert.That(summary.AnalysisAdviceKey, Is.EqualTo(expectedAdviceKey));
            AssertSafetyFlags(summary);
            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
        }

        private static void AssertSuggestion(SaveData save, string expectedKey)
        {
            string first = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig()).NextOptimizationSuggestionKey;
            string second = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig()).NextOptimizationSuggestionKey;

            Assert.That(first, Is.EqualTo(expectedKey));
            Assert.That(second, Is.EqualTo(first));
        }


        private static MvpDungeonPlacementState PlacementState(string categoryId, string optionId)
        {
            return new MvpDungeonPlacementState
            {
                Entries = new System.Collections.Generic.List<MvpDungeonPlacementEntry>
                {
                    new MvpDungeonPlacementEntry(categoryId, optionId, 1)
                },
                NextRevision = 2
            };
        }

        private static RunSimulationConfig PlacementEffectsConfig()
        {
            RunSimulationConfig config = HeatConfig();
            config.MvpPlacementEffectsRuleSourceId = "mvp.placement_effects.rule.test";
            config.MvpPlacementEffects = new[]
            {
                new MvpPlacementEffectConfig
                {
                    CategoryId = MvpDungeonPlacementIds.RoomCategoryId,
                    OptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                    PathCapacity = 2,
                    ExplanationKey = "effect.current"
                }
            };
            return config;
        }

        private static string FeedbackLocalize(string key, string fallback)
        {
            switch (key)
            {
                case MvpRunResultFeedbackPresenter.SuccessStableHeatKey:
                    return "stable";
                case MvpRunResultFeedbackPresenter.SuccessHeatIncreasedKey:
                    return "increased";
                case MvpRunResultFeedbackPresenter.OutcomeCueHeatIncreasedKey:
                    return "heat cue";
                case MvpRunResultFeedbackPresenter.OutcomeCueControlledLootKey:
                    return "loot cue";
                case MvpRunResultFeedbackPresenter.OutcomeCueFormatKey:
                    return "{0} {1}";
                case MvpRunResultFeedbackPresenter.FormatKey:
                    return "{0} loot {2}/{3}/{4} heat {5}->{6}.";
                case MvpRunResultFeedbackPresenter.PlacementEffectsImpactFormatKey:
                    return "{0} Placement impact: {1}.";
                case MvpPlacementEffectsPresenter.DetailSeparatorKey:
                    return ", ";
                case MvpPlacementEffectsPresenter.PathCapacityFormatKey:
                    return "path +{0}";
                case MvpPlacementEffectsPresenter.DangerFormatKey:
                    return "danger +{0}";
                case MvpPlacementEffectsPresenter.ExplanationFormatKey:
                    return "{0} ({1})";
                case "effect.current":
                    return "current room";
                case "effect.stored":
                    return "stored danger";
                default:
                    return fallback;
            }
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


        private static SaveData SaveWithBasicRunAnalysisUnlocked()
        {
            SaveData save = FullSave();
            save.researchPending = null;
            save.researchProgress = null;
            save.completedResearch = new CompletedResearchState { ProjectIds = new[] { ProjectId } };
            return save;
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


        private static RunOutcomeRecord RunWithLootAndHeat(int generatedWorldValue, int extractedWorldValue, int extractedTradeableWorldValue, double heatBefore, double heatAfter)
        {
            RunOutcomeRecord run = RunWithLoot(generatedWorldValue, extractedWorldValue, extractedTradeableWorldValue);
            run.HeatAtStart = heatBefore;
            run.RunHeatApplicationSummary.HeatBefore = heatBefore;
            run.RunHeatApplicationSummary.AppliedDelta = heatAfter - heatBefore;
            run.RunHeatApplicationSummary.HeatAfter = heatAfter;
            run.RunHeatApplicationSummary.TierBefore = CurrentHeatTierResolver.NoticeTierId;
            run.RunHeatApplicationSummary.TierAfter = CurrentHeatTierResolver.NoticeTierId;
            run.RunHeatApplicationSummary.TierChanged = false;
            return run;
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

        private static ResearchUnlockBridgeConfig UnlockConfig()
        {
            return new ResearchUnlockBridgeConfig
            {
                enabled = true,
                ruleSourceId = "test.research.unlock_bridge",
                unlocks = new[]
                {
                    new ResearchUnlockDefinitionConfig
                    {
                        researchProjectId = ProjectId,
                        unlockId = "research.unlock.basic_run_analysis",
                        summaryKey = "ui.research_unlock.basic_run_analysis.summary"
                    }
                }
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
#endif
