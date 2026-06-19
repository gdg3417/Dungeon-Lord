using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class GuidedMvpActionPathPresenterTests
    {
        private const string SlotId = "research.slot.primary";
        private const string ProjectId = "research.project.guided_mvp";
        private const string StructureId = "structure.mana_generator.basic";

        [Test]
        public void Resolve_NoPlacementState_SuggestsPlacingStructure()
        {
            SaveData save = FullSave();
            save.dungeonLayout = DungeonLayoutState.CreateEmpty(1, 2);
            save.researchProgress.ProgressUnits = 3d;
            save.researchProgress.CompletionPending = true;

            GuidedMvpActionPathSummary summary = Resolve(save);

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.CurrentStepId, Is.EqualTo(GuidedMvpActionPathPresenter.StepPlaceOrModifyStructureId));
            Assert.That(summary.CurrentStepStatusKey, Is.EqualTo(GuidedMvpActionPathPresenter.StatusPlaceOrModifyStructureKey));
            Assert.That(summary.NextActionKey, Is.EqualTo(GuidedMvpActionPathPresenter.ActionPlaceStructureKey));
            Assert.That(summary.IsComplete, Is.False);
            AssertSafetyFlags(summary);
        }

        [Test]
        public void Resolve_PlacementExistsButNoRunState_SuggestsRunningDungeon()
        {
            SaveData save = FullSave();
            save.runHistory = new RunHistoryState();
            save.researchProgress.ProgressUnits = 3d;
            save.researchProgress.CompletionPending = true;

            GuidedMvpActionPathSummary summary = Resolve(save);

            Assert.That(summary.CurrentStepId, Is.EqualTo(GuidedMvpActionPathPresenter.StepRunOrObserveId));
            Assert.That(summary.CurrentStepStatusKey, Is.EqualTo(GuidedMvpActionPathPresenter.StatusRunOrObserveKey));
            Assert.That(summary.NextActionKey, Is.EqualTo(GuidedMvpActionPathPresenter.ActionRunDungeonKey));
            Assert.That(summary.IsComplete, Is.False);
            AssertSafetyFlags(summary);
        }

        [Test]
        public void Resolve_LatestRunExistsWithHeatPressure_SuggestsReducingHeatPressure()
        {
            SaveData save = FullSave();
            save.structureRuntime.Heat = 12d;
            save.runHistory.LatestOutcome = RunWithHeatApplication(7d, 12d, CurrentHeatTierResolver.NoticeTierId, CurrentHeatTierResolver.ConcernTierId);

            GuidedMvpActionPathSummary summary = Resolve(save);

            Assert.That(summary.CurrentStepId, Is.EqualTo(GuidedMvpActionPathPresenter.StepReduceHeatPressureId));
            Assert.That(summary.CurrentStepStatusKey, Is.EqualTo(GuidedMvpActionPathPresenter.StatusHeatPressureKey));
            Assert.That(summary.NextActionKey, Is.EqualTo(GuidedMvpActionPathPresenter.ActionReduceHeatPressureKey));
            Assert.That(summary.IsComplete, Is.False);
            AssertSafetyFlags(summary);
        }

        [Test]
        public void Resolve_LatestRunExistsWithPoorLootExtraction_SuggestsImprovingSurvivabilityOrLayout()
        {
            SaveData save = FullSave();
            save.runHistory.LatestOutcome = RunWithLoot(generatedWorldValue: 6, extractedWorldValue: 0, extractedTradeableWorldValue: 0);

            GuidedMvpActionPathSummary summary = Resolve(save);

            Assert.That(summary.CurrentStepId, Is.EqualTo(GuidedMvpActionPathPresenter.StepImproveSurvivabilityOrLayoutId));
            Assert.That(summary.CurrentStepStatusKey, Is.EqualTo(GuidedMvpActionPathPresenter.StatusPoorLootExtractionKey));
            Assert.That(summary.NextActionKey, Is.EqualTo(GuidedMvpActionPathPresenter.ActionImproveSurvivabilityOrLayoutKey));
            Assert.That(summary.IsComplete, Is.False);
            AssertSafetyFlags(summary);
        }

        [Test]
        public void Resolve_CompletionPendingResearchState_SuggestsVerifyingResearchStatus()
        {
            SaveData save = FullSave();
            save.researchProgress.CompletionPending = true;

            MvpPlayerLoopSummary loopSummary = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig());
            GuidedMvpActionPathSummary summary = GuidedMvpActionPathPresenter.Resolve(save, loopSummary);

            Assert.That(loopSummary.ResearchStatusKey, Is.EqualTo(MvpPlayerLoopSummaryPresenter.ResearchVerificationRequiredKey));
            Assert.That(loopSummary.VerificationRequired, Is.True);
            Assert.That(loopSummary.VerificationAvailable, Is.True);
            Assert.That(summary.CurrentStepId, Is.EqualTo(GuidedMvpActionPathPresenter.StepVerifyResearchStatusId));
            Assert.That(summary.CurrentStepStatusKey, Is.EqualTo(GuidedMvpActionPathPresenter.StatusResearchCompletionPendingKey));
            Assert.That(summary.NextActionKey, Is.EqualTo(GuidedMvpActionPathPresenter.ActionVerifyResearchStatusKey));
            Assert.That(summary.IsComplete, Is.False);
            AssertSafetyFlags(summary);
        }


        [Test]
        public void Resolve_CompletionPendingWithUnavailableResearch_DoesNotSuggestVerifyingResearchStatus()
        {
            SaveData save = FullSave();
            save.researchProgress.ProgressUnits = 3d;
            save.researchProgress.CompletionPending = true;

            MvpPlayerLoopSummary loopSummary = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), UnavailableVerificationConfig());
            GuidedMvpActionPathSummary summary = GuidedMvpActionPathPresenter.Resolve(save, loopSummary);

            Assert.That(loopSummary.ResearchStatusKey, Is.EqualTo(MvpPlayerLoopSummaryPresenter.ResearchUnavailableKey));
            Assert.That(loopSummary.VerificationRequired, Is.False);
            Assert.That(summary.CurrentStepId, Is.EqualTo(GuidedMvpActionPathPresenter.StepRepeatOrImproveId));
            Assert.That(summary.CurrentStepId, Is.Not.EqualTo(GuidedMvpActionPathPresenter.StepVerifyResearchStatusId));
            Assert.That(summary.NextActionKey, Is.EqualTo(GuidedMvpActionPathPresenter.ActionRepeatOrImproveKey));
            Assert.That(summary.NextActionKey, Is.Not.EqualTo(GuidedMvpActionPathPresenter.ActionVerifyResearchStatusKey));
            AssertSafetyFlags(summary);
        }

        [Test]
        public void Resolve_CompleteOrRepeatImproveState_SuggestsRepeatingLoopAndMarksComplete()
        {
            SaveData save = FullSave();

            GuidedMvpActionPathSummary summary = Resolve(save);

            Assert.That(summary.CurrentStepId, Is.EqualTo(GuidedMvpActionPathPresenter.StepRepeatOrImproveId));
            Assert.That(summary.CurrentStepStatusKey, Is.EqualTo(GuidedMvpActionPathPresenter.StatusRepeatOrImproveKey));
            Assert.That(summary.NextActionKey, Is.EqualTo(GuidedMvpActionPathPresenter.ActionRepeatOrImproveKey));
            Assert.That(summary.IsComplete, Is.True);
            AssertSafetyFlags(summary);
        }

        [Test]
        public void Resolve_BasicRunAnalysisUnlocked_SuggestsApplyingAnalysisToPlacement()
        {
            SaveData save = FullSave();
            save.researchPending = null;
            save.researchProgress = null;
            save.completedResearch = new CompletedResearchState { ProjectIds = new[] { ProjectId }, LastCompletedProjectId = ProjectId };

            MvpPlayerLoopSummary loopSummary = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig(), UnlockConfig());
            GuidedMvpActionPathSummary summary = GuidedMvpActionPathPresenter.Resolve(save, loopSummary);

            Assert.That(loopSummary.AnalysisUnlocked, Is.True);
            Assert.That(summary.CurrentStepId, Is.EqualTo(GuidedMvpActionPathPresenter.StepApplyRunAnalysisId));
            Assert.That(summary.CurrentStepStatusKey, Is.EqualTo(GuidedMvpActionPathPresenter.StatusApplyRunAnalysisKey));
            Assert.That(summary.NextActionKey, Is.EqualTo(GuidedMvpActionPathPresenter.ActionApplyRunAnalysisKey));
            Assert.That(summary.IsComplete, Is.True);
            AssertSafetyFlags(summary);
        }

        [Test]
        public void Resolve_BasicRunAnalysisUnlockedWithoutAppliedAdjustment_KeepsCompareAndAdjustGuidance()
        {
            MvpPlayerLoopSummary loopSummary = AnalysisSummary(
                BasicRunAnalysisRecommendationPresenter.ReduceDangerKey,
                currentDanger: 4,
                latestDanger: 4);

            GuidedMvpActionPathSummary summary = GuidedMvpActionPathPresenter.Resolve(new SaveData(), loopSummary);

            Assert.That(summary.CurrentStepId, Is.EqualTo(GuidedMvpActionPathPresenter.StepApplyRunAnalysisId));
            Assert.That(summary.CurrentStepStatusKey, Is.EqualTo(GuidedMvpActionPathPresenter.StatusApplyRunAnalysisKey));
            Assert.That(summary.NextActionKey, Is.EqualTo(GuidedMvpActionPathPresenter.ActionApplyRunAnalysisKey));
            Assert.That(summary.HasAppliedAnalysisAdjustment, Is.False);
            Assert.That(summary.AppliedAnalysisAdjustmentKey, Is.Null);
            Assert.That(summary.IsComplete, Is.True);
            AssertSafetyFlags(summary);
        }

        [Test]
        public void Resolve_BasicRunAnalysisUnlockedWithReducedDangerAdjustment_ShiftsToRunAgainGuidance()
        {
            MvpPlayerLoopSummary loopSummary = AnalysisSummary(
                BasicRunAnalysisRecommendationPresenter.ReduceDangerKey,
                currentDanger: 3,
                latestDanger: 4);

            GuidedMvpActionPathSummary summary = GuidedMvpActionPathPresenter.Resolve(new SaveData(), loopSummary);

            Assert.That(summary.CurrentStepId, Is.EqualTo(GuidedMvpActionPathPresenter.StepTestPlacementChangeId));
            Assert.That(summary.CurrentStepStatusKey, Is.EqualTo(GuidedMvpActionPathPresenter.StatusAppliedAnalysisAdjustmentKey));
            Assert.That(summary.NextActionKey, Is.EqualTo(GuidedMvpActionPathPresenter.ActionRunAgainToTestChangeKey));
            Assert.That(summary.HasAppliedAnalysisAdjustment, Is.True);
            Assert.That(summary.AppliedAnalysisAdjustmentKey, Is.EqualTo(BasicRunAnalysisAppliedAdjustmentPresenter.DangerLowerKey));
            Assert.That(summary.IsComplete, Is.True);
            AssertSafetyFlags(summary);
        }

        [Test]
        public void Resolve_BasicRunAnalysisUnlockedWithGenericAppliedAdjustment_ShiftsToRunAgainGuidance()
        {
            MvpPlayerLoopSummary loopSummary = AnalysisSummary(
                "test.analysis.generic_advice",
                currentDanger: 5,
                latestDanger: 4);

            GuidedMvpActionPathSummary summary = GuidedMvpActionPathPresenter.Resolve(new SaveData(), loopSummary);

            Assert.That(summary.CurrentStepId, Is.EqualTo(GuidedMvpActionPathPresenter.StepTestPlacementChangeId));
            Assert.That(summary.CurrentStepStatusKey, Is.EqualTo(GuidedMvpActionPathPresenter.StatusAppliedAnalysisAdjustmentKey));
            Assert.That(summary.NextActionKey, Is.EqualTo(GuidedMvpActionPathPresenter.ActionRunAgainToTestChangeKey));
            Assert.That(summary.HasAppliedAnalysisAdjustment, Is.True);
            Assert.That(summary.AppliedAnalysisAdjustmentKey, Is.EqualTo(BasicRunAnalysisAppliedAdjustmentPresenter.EffectsChangedKey));
            Assert.That(summary.IsComplete, Is.True);
            AssertSafetyFlags(summary);
        }

        [Test]
        public void Resolve_NoSaveOrMissingOptionalState_ReturnsSafeDeterministicGuidance()
        {
            GuidedMvpActionPathSummary missingSave = GuidedMvpActionPathPresenter.Resolve(null, null);
            Assert.That(missingSave.RuleResolved, Is.False);
            Assert.That(missingSave.DeterministicErrorCode, Is.EqualTo((int)GuidedMvpActionPathErrorCode.MissingSave));
            Assert.That(missingSave.CurrentStepId, Is.EqualTo(GuidedMvpActionPathPresenter.StepPlaceOrModifyStructureId));
            Assert.That(missingSave.NextActionKey, Is.EqualTo(GuidedMvpActionPathPresenter.ActionPlaceStructureKey));
            AssertSafetyFlags(missingSave);

            SaveData missingOptional = new SaveData();
            string first = JsonUtility.ToJson(Resolve(missingOptional));
            string second = JsonUtility.ToJson(Resolve(missingOptional));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(Resolve(missingOptional).CurrentStepId, Is.EqualTo(GuidedMvpActionPathPresenter.StepPlaceOrModifyStructureId));
        }

        [Test]
        public void Resolve_DoesNotMutateSaveHeatManaRunHistoryResearchOrOfflineSummary()
        {
            SaveData save = FullSave();
            save.lastOfflineSummary = new OfflineSummary { OfflineSecondsObserved = 9, OfflineWindowClamped = true };
            string saveBefore = JsonUtility.ToJson(save);
            string runtimeBefore = JsonUtility.ToJson(save.structureRuntime);
            string historyBefore = JsonUtility.ToJson(save.runHistory);
            string pendingBefore = JsonUtility.ToJson(save.researchPending);
            string progressBefore = JsonUtility.ToJson(save.researchProgress);
            string completedBefore = JsonUtility.ToJson(save.completedResearch);
            string offlineBefore = JsonUtility.ToJson(save.lastOfflineSummary);

            GuidedMvpActionPathSummary first = Resolve(save);
            GuidedMvpActionPathSummary second = Resolve(save);

            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(saveBefore));
            Assert.That(JsonUtility.ToJson(save.structureRuntime), Is.EqualTo(runtimeBefore));
            Assert.That(JsonUtility.ToJson(save.runHistory), Is.EqualTo(historyBefore));
            Assert.That(JsonUtility.ToJson(save.researchPending), Is.EqualTo(pendingBefore));
            Assert.That(JsonUtility.ToJson(save.researchProgress), Is.EqualTo(progressBefore));
            Assert.That(JsonUtility.ToJson(save.completedResearch), Is.EqualTo(completedBefore));
            Assert.That(JsonUtility.ToJson(save.lastOfflineSummary), Is.EqualTo(offlineBefore));
            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
            AssertSafetyFlags(first);
        }

        private static GuidedMvpActionPathSummary Resolve(SaveData save)
        {
            MvpPlayerLoopSummary loopSummary = MvpPlayerLoopSummaryPresenter.Resolve(save, HeatConfig(), EligibilityConfig(), VerificationConfig());
            return GuidedMvpActionPathPresenter.Resolve(save, loopSummary);
        }

        private static void AssertSafetyFlags(GuidedMvpActionPathSummary summary)
        {
            Assert.That(summary.WouldMutateState, Is.False);
            Assert.That(summary.WouldGrantRewards, Is.False);
            Assert.That(summary.WouldUnlockContent, Is.False);
            Assert.That(summary.WouldChargeCosts, Is.False);
            Assert.That(summary.WouldCallServer, Is.False);
            Assert.That(summary.WouldProcessOfflineResearch, Is.False);
            Assert.That(summary.WouldProcessOfflineHeat, Is.False);
            Assert.That(summary.WouldStartRaid, Is.False);
        }

        private static MvpPlayerLoopSummary AnalysisSummary(string adviceKey, int currentDanger, int latestDanger)
        {
            return new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasPlacementContext = true,
                HasRunOutcome = true,
                RunSucceeded = true,
                AnalysisUnlocked = true,
                AnalysisAdviceKey = adviceKey,
                NextOptimizationSuggestionKey = GuidedMvpActionPathPresenter.ActionApplyRunAnalysisKey,
                PlacementEffects = new MvpPlacementEffectsSummary
                {
                    RuleResolved = true,
                    Danger = currentDanger,
                    PathCapacity = 1
                },
                LatestRunPlacementEffects = new MvpPlacementEffectsSummary
                {
                    RuleResolved = true,
                    Danger = latestDanger,
                    PathCapacity = 1
                }
            };
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
                researchPending = new ResearchPendingState { SlotId = SlotId, ProjectId = ProjectId },
                researchProgress = new ResearchProgressState
                {
                    SlotId = SlotId,
                    ProjectId = ProjectId,
                    ProgressUnits = 1d,
                    CompletionPending = false,
                    RuleSourceIdUsed = "test.research.progress"
                },
                completedResearch = new CompletedResearchState { ProjectIds = new[] { "research.project.completed" } }
            };
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
        private static ResearchUnlockBridgeConfig UnlockConfig()
        {
            return new ResearchUnlockBridgeConfig
            {
                enabled = true,
                ruleSourceId = "test.research.unlock",
                unlocks = new[]
                {
                    new ResearchUnlockDefinitionConfig
                    {
                        researchProjectId = ProjectId,
                        unlockId = MvpPlayerLoopSummaryPresenter.BasicRunAnalysisUnlockId,
                        summaryKey = "ui.research_unlock.basic_run_analysis.summary"
                    }
                }
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
