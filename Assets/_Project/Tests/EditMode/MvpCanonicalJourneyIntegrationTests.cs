using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpCanonicalJourneyIntegrationTests
    {
        private GameObject _rootObject;
        private GameRoot _root;
        private RunSimulationConfig _config;

        [SetUp]
        public void SetUp()
        {
            string runConfigJson = File.ReadAllText("Assets/_Project/Data/Bootstrap/run_simulation_config.json");
            string lootConfigJson = File.ReadAllText("Assets/_Project/Data/Bootstrap/loot_config.json");
            Assert.That(
                GameRoot.TryCreateRunSimulationService(runConfigJson, lootConfigJson, out RunSimulationService runService),
                Is.True);
            _config = runService.Config;
            _rootObject = new GameObject("MvpCanonicalJourneyIntegrationRoot");
            _root = _rootObject.AddComponent<GameRoot>();
            SetRoot("<DevPanelEnabled>k__BackingField", true);
            SetRoot("<Content>k__BackingField", BuildContent());
            SetRoot("_runSimulationService", runService);
            SetRoot("<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "mvp_canonical_journey_test.json", useAtomicWrites = false }));
            SetRoot("<Save>k__BackingField", new SaveData());
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_rootObject);

        [Test]
        public void CleanSaveMvpJourney_RemainsRepeatableAcrossBuildRunResearchGreedRiskAdjustmentSaveLoadAndReset()
        {
            Assert.That(_root.ResetCleanMvpValidationSession(), Is.True);
            AssertCleanStart();
            AssertPrimary(MvpPrimaryNextActionPresenter.SourceFirstContract, MvpPrimaryNextActionPresenter.RuleFirstContractIncomplete);

            PlaceStarterDungeon();
            MvpPlayerLoopSummary built = _root.ResolveMvpPlayerLoopSummary();
            Assert.That(built.DungeonPlacements.Select(p => p.OptionId), Is.EquivalentTo(new[]
            {
                MvpDungeonPlacementIds.BasicRoomOptionId,
                MvpDungeonPlacementIds.SkeletonOptionId,
                MvpDungeonPlacementIds.SpikeTrapOptionId,
                MvpDungeonPlacementIds.BasicLootNodeOptionId
            }));
            Assert.That(built.PlacementEffects.RuleResolved, Is.True);
            Assert.That(built.PlacementEffects.ContributingOptionIds, Does.Contain(MvpDungeonPlacementIds.SkeletonOptionId));
            Assert.That(MvpFirstSessionObjectivePresenter.Resolve(_root.Save, _config).PathComplete, Is.True);
            int revisionBeforeNoop = _root.Save.mvpDungeonPlacements.NextRevision;
            Assert.That(_root.TryMvpPlaceOrModifySelectedPlacementEnforcingRoomTarget(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, out _, out _, out _, out _), Is.True);
            Assert.That(_root.Save.mvpDungeonPlacements.NextRevision, Is.EqualTo(revisionBeforeNoop));

            Assert.That(_root.SimulateRunOnce(RunPostureResolver.BalancedId), Is.True);
            MvpPlayerLoopSummary afterFirstRun = _root.ResolveMvpPlayerLoopSummary();
            RunOutcomeRecord firstOutcome = _root.Save.runHistory.LatestOutcome;
            string firstRunDiagnostics = FirstRunDiagnostics(firstOutcome);
            Assert.That(firstOutcome, Is.Not.Null, firstRunDiagnostics);
            Assert.That(afterFirstRun.RuleResolved, Is.True, firstRunDiagnostics);
            Assert.That(afterFirstRun.HasRunOutcome, Is.True, firstRunDiagnostics);
            Assert.That(firstOutcome.LootSummary, Is.Not.Null, firstRunDiagnostics);
            Assert.That(firstOutcome.LootSummary.ResolverSuccess, Is.True, firstRunDiagnostics);
            Assert.That(firstOutcome.LootExtractionSummary, Is.Not.Null, firstRunDiagnostics);
            Assert.That(firstOutcome.LootExtractionSummary.RuleResolved, Is.True, firstRunDiagnostics);
            Assert.That(afterFirstRun.LootGeneratedWorldValue, Is.GreaterThanOrEqualTo(0), firstRunDiagnostics);
            Assert.That(afterFirstRun.LootExtractedWorldValue, Is.GreaterThanOrEqualTo(0), firstRunDiagnostics);
            Assert.That(afterFirstRun.LatestRunPartySize, Is.GreaterThan(0));
            Assert.That(afterFirstRun.AdventurerPartyClassIds, Is.Not.Empty);
            Assert.That(afterFirstRun.NextOptimizationSuggestionKey, Is.Not.Empty);
            Assert.That(firstOutcome.ReasonKey, Is.Not.Empty);
            Assert.That(_root.Save.runHistory.RecentOutcomes, Does.Contain(firstOutcome));
            AssertPrimary(MvpPrimaryNextActionPresenter.SourceFirstContract, MvpPrimaryNextActionPresenter.RuleFirstContractIncomplete);

            CompleteResearchThroughRootBoundary();
            RunUntilFirstContractRequirementsComplete();
            MvpFirstSessionObjectiveSummary completedContract = MvpFirstSessionObjectivePresenter.Resolve(_root.Save, _config);
            Assert.That(completedContract.CurrentRequirementsComplete, Is.True);
            Assert.That(completedContract.IsComplete, Is.True);
            Assert.That(completedContract.CompletionRecorded, Is.True);
            Assert.That(_root.Save.completedObjectives.ObjectiveIds, Does.Contain(_config.MvpFirstSessionObjective.ObjectiveId));
            int completedObjectiveCount = _root.Save.completedObjectives.ObjectiveIds.Length;
            string completedObjectivesBeforeIdempotencyCheck = JsonUtility.ToJson(_root.Save.completedObjectives);
            Assert.That(
                MvpFirstSessionObjectiveCompletionApplier.ApplyIfComplete(_root.Save, _config),
                Is.False,
                "Completion should already have been recorded by the qualifying run.");
            Assert.That(_root.Save.completedObjectives.ObjectiveIds.Length, Is.EqualTo(completedObjectiveCount));
            Assert.That(JsonUtility.ToJson(_root.Save.completedObjectives), Is.EqualTo(completedObjectivesBeforeIdempotencyCheck));
            AssertPrimaryNot(MvpPrimaryNextActionPresenter.SourceFirstContract);
            MvpPostContractGreedTrialSummary activeGreed = MvpPostContractGreedTrialPresenter.Resolve(_root.Save, _config, completedContract);
            Assert.That(activeGreed.IsActive, Is.True);

            PlaceGreedTrialDungeon();
            MvpPostContractGreedTrialSummary completeGreed = MvpPostContractGreedTrialPresenter.Resolve(_root.Save, _config);
            Assert.That(completeGreed.GreedSetupTestedComplete, Is.True);
            Assert.That(completeGreed.HeatStabilizedComplete, Is.True);
            Assert.That(completeGreed.RiskResponseComplete, Is.True);
            Assert.That(completeGreed.IsComplete, Is.True);
            AssertPrimaryNot(MvpPrimaryNextActionPresenter.SourceFirstContract);
            AssertPrimaryNot("recent_spoils_ledger");

            Place(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId);
            RunUntilNotice();
            Assert.That(MvpFirstSessionObjectivePresenter.Resolve(_root.Save, _config).IsComplete, Is.True);
            Assert.That(_root.Save.completedObjectives.ObjectiveIds, Does.Contain(_config.MvpFirstSessionObjective.ObjectiveId));
            MvpPostContractGreedTrialSummary noticeGreed = MvpPostContractGreedTrialPresenter.Resolve(_root.Save, _config);
            Assert.That(noticeGreed.CurrentHeatTierId, Is.EqualTo(CurrentHeatTierResolver.NoticeTierId));
            Assert.That(noticeGreed.IsActive, Is.True);
            Assert.That(noticeGreed.NextActionKey, Is.EqualTo(MvpPostContractGreedTrialPresenter.NextActionStabilizeHeatKey));
            AssertPrimaryNot(MvpPrimaryNextActionPresenter.SourceFirstContract);

            MvpPlayerLoopSummary beforeAdjustment = _root.ResolveMvpPlayerLoopSummary();
            Assert.That(
                beforeAdjustment.AnalysisAdviceKey,
                Is.AnyOf(
                    BasicRunAnalysisRecommendationPresenter.ReduceDangerKey,
                    BasicRunAnalysisRecommendationPresenter.ReduceHeatKey),
                "The Notice-risk phase should recommend reducing either casualty danger or heat pressure.");
            Assert.That(_root.TryMvpPlaceOrModifySelectedPlacementEnforcingRoomTarget(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.ChillingSigilOptionId, out _, out _, out _, out _), Is.True);
            MvpPlayerLoopSummary afterAdjustment = _root.ResolveMvpPlayerLoopSummary();
            BasicRunAnalysisAppliedAdjustmentResult applied = BasicRunAnalysisAppliedAdjustmentPresenter.Resolve(afterAdjustment);
            Assert.That(applied, Is.Not.Null);
            Assert.That(applied.Applied, Is.True);
            Assert.That(applied.NextActionKey, Is.EqualTo(BasicRunAnalysisAppliedAdjustmentPresenter.RunAgainToTestChangeKey));
            if (string.Equals(beforeAdjustment.AnalysisAdviceKey, BasicRunAnalysisRecommendationPresenter.ReduceDangerKey, StringComparison.Ordinal))
            {
                Assert.That(applied.AdjustmentKey, Is.EqualTo(BasicRunAnalysisAppliedAdjustmentPresenter.DangerLowerKey));
            }
            else
            {
                Assert.That(applied.AdjustmentKey, Is.EqualTo(BasicRunAnalysisAppliedAdjustmentPresenter.HeatPressureLowerKey));
            }

            MvpPrimaryNextActionSummary appliedAction = ResolvePrimary();
            Assert.That(appliedAction.PrimaryActionKey, Is.EqualTo(BasicRunAnalysisAppliedAdjustmentPresenter.RunAgainToTestChangeKey));
            string previousRunId = _root.Save.runHistory.LatestOutcome.RunId;
            Assert.That(_root.SimulateRunOnce(RunPostureResolver.CautiousId), Is.True);
            Assert.That(_root.Save.runHistory.LatestOutcome.RunId, Is.Not.EqualTo(previousRunId));
            MvpPlayerLoopSummary afterRerun = _root.ResolveMvpPlayerLoopSummary();
            Assert.That(BasicRunAnalysisAppliedAdjustmentPresenter.Resolve(afterRerun), Is.Null);
            Assert.That(afterRerun.HasRunOutcome, Is.True);
            Assert.That(afterRerun.LatestRunId, Is.EqualTo(_root.Save.runHistory.LatestOutcome.RunId));

            string savedJson = JsonUtility.ToJson(_root.Save);
            SaveData loaded = JsonUtility.FromJson<SaveData>(savedJson);
            SaveMigration.MigrateToLatest(new SaveRoot { primary = loaded });
            SetRoot("<Save>k__BackingField", loaded);
            Assert.That(_root.Save.mvpRoomSlotAssignments.Rooms[0].LootNodeOptionIds, Does.Contain(MvpDungeonPlacementIds.GlitteringHoardOptionId));
            Assert.That(_root.Save.runHistory.LatestOutcome, Is.Not.Null);
            Assert.That(_root.Save.completedObjectives.ObjectiveIds, Does.Contain(_config.MvpFirstSessionObjective.ObjectiveId));
            Assert.That(MvpFirstSessionObjectivePresenter.Resolve(_root.Save, _config).IsComplete, Is.True);
            Assert.That(MvpPostContractGreedTrialPresenter.Resolve(_root.Save, _config).IsActive, Is.True);
            Assert.That(_root.Save.completedResearch.ProjectIds, Does.Contain(_config.MvpFirstSessionObjective.AnalysisResearchProjectId));
            Assert.That(ResolvePrimary().PrimaryActionSource, Is.Not.EqualTo(MvpPrimaryNextActionPresenter.SourceFirstContract));

            Assert.That(_root.ResetCleanMvpValidationSession(), Is.True);
            AssertCleanStart();
            Assert.That(_root.Save.completedResearch.ProjectIds, Is.Null.Or.Empty);
            PlaceStarterDungeon();
            Assert.That(_root.SimulateRunOnce(RunPostureResolver.BalancedId), Is.True);
            Assert.That(_root.Save.runHistory.LatestOutcome.RunId, Does.Contain("run_0001"));
        }

        private void PlaceStarterDungeon()
        {
            Place(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId);
            Place(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
            Place(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId);
            Place(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId);
        }

        private void PlaceGreedTrialDungeon()
        {
            Place(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.ChillingSigilOptionId);
            Place(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.GlitteringHoardOptionId);
        }

        private void Place(string category, string option)
        {
            Assert.That(_root.TryMvpPlaceOrModifySelectedPlacementEnforcingRoomTarget(category, option, out _, out _, out string banner, out string failure), Is.True, category + ":" + option + " " + banner + " " + failure);
        }

        private void CompleteResearchThroughRootBoundary()
        {
            _root.Save.researchPending = new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = _config.MvpFirstSessionObjective.AnalysisResearchProjectId };
            _root.Save.researchProgress = new ResearchProgressState { SlotId = "research.slot.primary", ProjectId = _config.MvpFirstSessionObjective.AnalysisResearchProjectId, ProgressUnits = 1d, CompletionPending = true };
            Assert.That(_root.ClaimResearchCompletionScaffold(), Is.True);
        }

        private string FirstRunDiagnostics(RunOutcomeRecord outcome)
        {
            int historyCount = _root.Save?.runHistory?.RecentOutcomes != null ? _root.Save.runHistory.RecentOutcomes.Length : 0;
            return string.Format(
                "runId={0} lootTable={1} lootError={2} extractionError={3} runHistoryCount={4}",
                outcome?.RunId ?? "<null>",
                outcome?.LootSummary?.LootTableId ?? "<null>",
                outcome?.LootSummary != null ? outcome.LootSummary.ResolverErrorCode : -1,
                outcome?.LootExtractionSummary != null ? outcome.LootExtractionSummary.DeterministicErrorCode : -1,
                historyCount);
        }

        private void RunUntilFirstContractRequirementsComplete()
        {
            const int maxRuns = 20;
            for (int i = 0; i < maxRuns; i++)
            {
                MvpFirstSessionObjectiveSummary summary = MvpFirstSessionObjectivePresenter.Resolve(_root.Save, _config);
                if (summary.CurrentRequirementsComplete)
                {
                    return;
                }

                if (!summary.HeatTargetComplete)
                {
                    Place(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.ChillingSigilOptionId);
                    Assert.That(_root.SimulateRunOnce(RunPostureResolver.CautiousId), Is.True);
                }
                else
                {
                    Assert.That(_root.SimulateRunOnce(RunPostureResolver.BalancedId), Is.True);
                }
            }

            MvpFirstSessionObjectiveSummary final = MvpFirstSessionObjectivePresenter.Resolve(_root.Save, _config);
            Assert.Fail(
                "First Dungeon Contract requirements were not complete after {0} bounded real runs. runCount={1} recoveredLoot={2}/{3} heatTier={4} pathComplete={5} analysisComplete={6} analysisUnlocked={7}",
                maxRuns,
                final.RunCount,
                final.RecoveredLootValue,
                final.RequiredRecoveredLootValue,
                final.CurrentHeatTierId,
                final.PathComplete,
                final.AnalysisComplete,
                final.AnalysisUnlocked);
        }

        private void RunUntilNotice()
        {
            for (int i = 0; i < 20 && !string.Equals(CurrentTier(), CurrentHeatTierResolver.NoticeTierId, StringComparison.Ordinal); i++)
            {
                Assert.That(_root.SimulateRunOnce(RunPostureResolver.GreedyId), Is.True);
            }
        }

        private string CurrentTier() => CurrentHeatTierResolver.Resolve(_config, _root.Save.structureRuntime.Heat).TierId;
        private MvpPrimaryNextActionSummary ResolvePrimary() => MvpPrimaryNextActionPresenter.Resolve(_root.ResolveMvpPlayerLoopSummary(), _root.ResolveGuidedMvpActionPath(), MvpFirstSessionObjectivePresenter.Resolve(_root.Save, _config), MvpPostContractGreedTrialPresenter.Resolve(_root.Save, _config));
        private void AssertPrimary(string source, string rule) { var p = ResolvePrimary(); Assert.That(p.PrimaryActionSource, Is.EqualTo(source)); Assert.That(p.ResolvedRule, Is.EqualTo(rule)); }
        private void AssertPrimaryNot(string source) => Assert.That(ResolvePrimary().PrimaryActionSource, Is.Not.EqualTo(source));

        private void AssertCleanStart()
        {
            Assert.That(MvpFirstSessionObjectivePresenter.Resolve(_root.Save, _config).IsComplete, Is.False);
            Assert.That(MvpPostContractGreedTrialPresenter.Resolve(_root.Save, _config).IsActive, Is.False);
            Assert.That(_root.Save.runHistory.LatestOutcome, Is.Null);
            Assert.That(_root.Save.runHistory.RecentOutcomes, Is.Null.Or.Empty);
            Assert.That(_root.Save.completedObjectives.ObjectiveIds, Is.Null.Or.Empty);
            Assert.That(_root.Save.mvpDungeonPlacements.Entries, Is.Empty);
        }

        private void SetRoot(string field, object value) => typeof(GameRoot).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_root, value);

        private ContentService BuildContent()
        {
            var content = new ContentService();
            typeof(ContentService).GetField("<Bootstrap>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(content, new ContentBootstrap
            {
                featureFlags = new FeatureFlags { enableDevPanel = true },
                researchCompletionEligibilityScaffold = new ResearchCompletionEligibilityScaffoldConfig { enabled = true, projectId = _config.MvpFirstSessionObjective.AnalysisResearchProjectId, requiredProgressUnits = 1d, ruleSourceId = "research.eligibility.canonical" },
                researchCompletionClaimScaffold = new ResearchCompletionClaimScaffoldConfig { enabled = true, ruleSourceId = "research.claim.canonical" },
                researchVerificationScaffold = new ResearchVerificationScaffoldConfig { enabled = true, verificationMode = ResearchVerificationBoundaryResolver.LocalDevPlaceholderVerificationMode, ruleSourceId = "research.verification.canonical" },
                researchUnlockBridge = new ResearchUnlockBridgeConfig { enabled = true, ruleSourceId = "research.unlock.canonical", unlocks = new[] { new ResearchUnlockDefinitionConfig { researchProjectId = _config.MvpFirstSessionObjective.AnalysisResearchProjectId, unlockId = MvpPlayerLoopSummaryPresenter.BasicRunAnalysisUnlockId, summaryKey = "ui.research.unlock.basic_run_analysis.summary" } } }
            });
            return content;
        }
    }
}
