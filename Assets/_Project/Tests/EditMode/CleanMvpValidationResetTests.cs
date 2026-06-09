using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class CleanMvpValidationResetTests
    {
        private GameObject _rootObject;
        private GameRoot _root;

        [SetUp]
        public void SetUp()
        {
            _rootObject = new GameObject("CleanMvpValidationResetRootTest");
            _root = _rootObject.AddComponent<GameRoot>();
            SetBackingField("<Content>k__BackingField", BuildContent(enableDevPanel: true));
            SetBackingField("<DevPanelEnabled>k__BackingField", true);
            SetBackingField("<Save>k__BackingField", BuildDirtySave());
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_rootObject);
        }

        [Test]
        public void ResetCleanMvpValidationSession_WhenDevPanelDisabled_DoesNotMutateSave()
        {
            SetBackingField("<DevPanelEnabled>k__BackingField", false);
            string before = JsonUtility.ToJson(_root.Save);

            bool didReset = _root.ResetCleanMvpValidationSession();

            Assert.That(didReset, Is.False);
            Assert.That(JsonUtility.ToJson(_root.Save), Is.EqualTo(before));
        }

        [Test]
        public void ResetCleanMvpValidationSession_ClearsMvpRuntimeHistoryAndPlacementToCleanBaseline()
        {
            bool didReset = _root.ResetCleanMvpValidationSession();

            Assert.That(didReset, Is.True);
            AssertCleanMvpBaseline(_root.Save);
            Assert.That(_root.SelectedFloorIndex, Is.Zero);
            Assert.That(_root.SelectedSlotIndex, Is.Zero);
            Assert.That(_root.CurrentHeat, Is.Zero);
            Assert.That(_root.ManaLine, Is.EqualTo("Mana: 0.00"));
            Assert.That(_root.HeatLine, Is.EqualTo("Heat: 0.00"));
            Assert.That(_root.RunLine, Is.EqualTo("ui.run.none"));
            Assert.That(_root.Save.completedResearch.ProjectIds, Is.EqualTo(new[] { "research.project.preexisting" }));
        }

        [Test]
        public void ApplyCleanMvpValidationBaseline_IsDeterministicForEquivalentDirtySaves()
        {
            SaveData first = BuildDirtySave();
            SaveData second = BuildDirtySave();

            GameRoot.ApplyCleanMvpValidationBaseline(first);
            GameRoot.ApplyCleanMvpValidationBaseline(second);

            Assert.That(JsonUtility.ToJson(first), Is.EqualTo(JsonUtility.ToJson(second)));
            AssertCleanMvpBaseline(first);
            AssertCleanMvpBaseline(second);
        }

        [Test]
        public void ResetCleanMvpValidationSession_DoesNotClaimResearchGrantRewardsUnlocksOrOfflineProgression()
        {
            SaveData save = _root.Save;
            string completedBefore = JsonUtility.ToJson(save.completedResearch);

            bool didReset = _root.ResetCleanMvpValidationSession();
            MvpPlayerLoopSummary summary = _root.ResolveMvpPlayerLoopSummary();
            GuidedMvpActionPathSummary guided = _root.ResolveGuidedMvpActionPath(summary);

            Assert.That(didReset, Is.True);
            Assert.That(save.researchPending, Is.Null);
            Assert.That(save.researchProgress, Is.Null);
            Assert.That(JsonUtility.ToJson(save.completedResearch), Is.EqualTo(completedBefore));
            Assert.That(summary.CanClaimProduction, Is.False);
            Assert.That(summary.WouldGrantRewards, Is.False);
            Assert.That(summary.WouldUnlockContent, Is.False);
            Assert.That(summary.WouldCallServer, Is.False);
            Assert.That(summary.WouldProcessOfflineProgress, Is.False);
            Assert.That(guided.WouldGrantRewards, Is.False);
            Assert.That(guided.WouldUnlockContent, Is.False);
            Assert.That(guided.WouldProcessOfflineResearch, Is.False);
            Assert.That(guided.WouldProcessOfflineHeat, Is.False);
            Assert.That(save.lastOfflineSummary, Is.Not.Null);
            Assert.That(save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
        }

        private void SetBackingField(string fieldName, object value)
        {
            typeof(GameRoot).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_root, value);
        }

        private static void AssertCleanMvpBaseline(SaveData save)
        {
            Assert.That(save.totalTicks, Is.Zero);
            Assert.That(save.lastPausedUtcUnix, Is.Zero);
            Assert.That(save.lastResumedUtcUnix, Is.Zero);
            Assert.That(save.dungeonLayout, Is.Not.Null);
            Assert.That(save.dungeonLayout.FloorCount, Is.EqualTo(SaveMigration.DefaultFloorCount));
            Assert.That(save.dungeonLayout.SlotsPerFloor, Is.EqualTo(SaveMigration.DefaultSlotsPerFloor));
            Assert.That(save.dungeonLayout.Slots.Count, Is.EqualTo(SaveMigration.DefaultFloorCount * SaveMigration.DefaultSlotsPerFloor));
            Assert.That(save.dungeonLayout.Slots.All(slot => !slot.IsOccupied), Is.True);
            Assert.That(save.structureRuntime, Is.Not.Null);
            Assert.That(save.structureRuntime.ManaReserve, Is.Zero);
            Assert.That(save.structureRuntime.Heat, Is.Zero);
            Assert.That(save.structureRuntime.IsHeatCrisisActive, Is.False);
            Assert.That(save.structureRuntime.LastProcessedTick, Is.Zero);
            Assert.That(save.structureRuntime.RiskLabPaused, Is.False);
            Assert.That(save.structureRuntime.PlacementLocked, Is.False);
            Assert.That(save.runHistory, Is.Not.Null);
            Assert.That(save.runHistory.NextRunSequence, Is.EqualTo(1));
            Assert.That(save.runHistory.LatestOutcome, Is.Null);
            Assert.That(save.runHistory.RecentOutcomes, Is.Empty);
            Assert.That(save.researchPending, Is.Null);
            Assert.That(save.researchProgress, Is.Null);
        }

        private static SaveData BuildDirtySave()
        {
            DungeonLayoutState layout = DungeonLayoutState.CreateEmpty(1, 2);
            new PlacementService().PlaceStructure(layout, 0, 0, StructureSimulationPass.ManaGeneratorBasicId);
            new PlacementService().PlaceStructure(layout, 0, 1, StructureSimulationPass.RiskLabBasicId);

            return new SaveData
            {
                totalTicks = 44,
                lastPausedUtcUnix = 111,
                lastResumedUtcUnix = 222,
                dungeonLayout = layout,
                structureRuntime = new StructureRuntimeState
                {
                    ManaReserve = 99d,
                    Heat = 37d,
                    IsHeatCrisisActive = true,
                    LastProcessedTick = 43,
                    HeatCrisisEnterStreak = 2,
                    HeatCrisisRecoveryStreak = 1,
                    RiskLabPaused = true,
                    PlacementLocked = true
                },
                runHistory = new RunHistoryState
                {
                    NextRunSequence = 8,
                    LatestOutcome = new RunOutcomeRecord { RunId = "run.dirty.latest", Success = true },
                    RecentOutcomes = new[]
                    {
                        new RunOutcomeRecord { RunId = "run.dirty.older" },
                        new RunOutcomeRecord { RunId = "run.dirty.latest", Success = true }
                    }
                },
                researchPending = new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = "research.project.dirty" },
                researchProgress = new ResearchProgressState
                {
                    SlotId = "research.slot.primary",
                    ProjectId = "research.project.dirty",
                    ProgressUnits = 999d,
                    CompletionPending = true
                },
                completedResearch = new CompletedResearchState { ProjectIds = new[] { "research.project.preexisting" } },
                lastOfflineSummary = new OfflineSummary
                {
                    RuleResolved = true,
                    OfflineSecondsObserved = 999,
                    WouldProcessOfflineProgress = true,
                    RuleSourceIdUsed = "offline.summary.dirty"
                }
            };
        }

        private static ContentService BuildContent(bool enableDevPanel)
        {
            var content = new ContentService();
            typeof(ContentService).GetField("<Bootstrap>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(content, new ContentBootstrap
                {
                    featureFlags = new FeatureFlags { enableDevPanel = enableDevPanel },
                    researchCompletionEligibilityScaffold = new ResearchCompletionEligibilityScaffoldConfig
                    {
                        enabled = true,
                        projectId = "research.project.dirty",
                        requiredProgressUnits = 1d,
                        ruleSourceId = "research.eligibility.test"
                    },
                    researchVerificationScaffold = new ResearchVerificationScaffoldConfig
                    {
                        enabled = true,
                        verificationMode = ResearchVerificationBoundaryResolver.LocalDevPlaceholderVerificationMode,
                        ruleSourceId = "research.verification.test"
                    },
                    timeRules = new TimeRules
                    {
                        allowOfflineProgression = false,
                        maxOfflineSeconds = 60,
                        offlineSummaryRuleSourceId = "offline.summary.test"
                    }
                });
            return content;
        }
    }

    public class CleanMvpValidationResetOverlayTests
    {
        private GameObject _rootObject;
        private GameObject _overlayObject;
        private GameObject _textObject;
        private GameRoot _root;
        private BootstrapOverlay _overlay;

        [SetUp]
        public void SetUp()
        {
            _rootObject = new GameObject("CleanMvpValidationResetOverlayRootTest");
            _overlayObject = new GameObject("CleanMvpValidationResetOverlayTest");
            _textObject = new GameObject("CleanMvpValidationResetOverlayTextTest");
            _root = _rootObject.AddComponent<GameRoot>();
            _overlay = _overlayObject.AddComponent<BootstrapOverlay>();
            _overlay.overlayText = _textObject.AddComponent<TextMeshProUGUI>();
            SetRootBackingField("<Content>k__BackingField", BuildOverlayContent(enableDevPanel: true));
            SetRootBackingField("<DevPanelEnabled>k__BackingField", true);
            SetRootBackingField("<Save>k__BackingField", BuildOverlayDirtySave());
            _overlay.Bind(_root);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_textObject);
            UnityEngine.Object.DestroyImmediate(_overlayObject);
            UnityEngine.Object.DestroyImmediate(_rootObject);
        }

        [Test]
        public void ResetControl_IsDevOnlyAndNotNormalPlayerFacingMvpAction()
        {
            SetRootBackingField("<DevPanelEnabled>k__BackingField", false);
            string playerFacingText = RefreshText();

            Assert.That(_overlay.ResetCleanMvpValidationSessionFromDevPanel(), Is.False);
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
            Assert.That(playerFacingText, Does.Not.Contain("Clean MVP Validation Reset"));
            Assert.That(playerFacingText, Does.Not.Contain("clean_mvp_validation_reset"));
        }

        [Test]
        public void ResetFromDevPanel_ClearsCachedFeedbackResetsSelectionAndRefreshesPlayerFacingSummary()
        {
            Assert.That(_overlay.SelectMvpStructure(StructureSimulationPass.RiskLabBasicId), Is.True);
            Assert.That(_overlay.SelectMvpRunPosture(RunPostureResolver.GreedyId), Is.True);
            SetOverlayPrivateField("_mvpStructurePlacementFeedback", "stale placement feedback");
            SetOverlayPrivateField("_mvpRunResultFeedback", "stale run feedback");
            string dirtyText = RefreshText();
            Assert.That(dirtyText, Does.Contain("Latest run: Succeeded"));
            Assert.That(dirtyText, Does.Contain("Mana reserve: 45"));
            Assert.That(_overlay.SelectedMvpStructureId, Is.EqualTo(StructureSimulationPass.RiskLabBasicId));
            Assert.That(_overlay.SelectedMvpRunPostureId, Is.EqualTo(RunPostureResolver.GreedyId));
            Assert.That(_overlay.GetSelectedMvpStructureDisplayName(), Is.EqualTo("Risk Lab"));

            bool didReset = _overlay.ResetCleanMvpValidationSessionFromDevPanel();
            string refreshed = RefreshText();

            Assert.That(didReset, Is.True);
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.Empty);
            Assert.That(_overlay.MvpRunResultFeedback, Is.Empty);
            Assert.That(_overlay.SelectedMvpStructureId, Is.EqualTo(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(_overlay.SelectedMvpRunPostureId, Is.EqualTo(RunPostureResolver.BalancedId));
            Assert.That(_overlay.GetSelectedMvpRunPostureDisplayName(), Is.EqualTo("Balanced"));
            Assert.That(_overlay.GetSelectedMvpStructureDisplayName(), Is.EqualTo("Mana Generator"));
            Assert.That(_overlay.GetSelectedMvpRunPlanPreviewText(), Is.EqualTo("Plan: Mana Generator + Balanced run.\nExpected tradeoff: standard loot and heat pressure."));
            Assert.That(refreshed, Does.Contain("Placement: No structure placed"));
            Assert.That(refreshed, Does.Contain("Latest run: No run yet"));
            Assert.That(refreshed, Does.Contain("Mana reserve: 0"));
            Assert.That(refreshed, Does.Contain("Heat: 0 -> 0"));
            Assert.That(refreshed, Does.Contain("First-session status: place one structure to start the loop."));
            Assert.That(refreshed, Does.Contain("Plan: Mana Generator + Balanced run."));
            Assert.That(refreshed, Does.Contain("Expected tradeoff: standard loot and heat pressure."));
            Assert.That(refreshed, Does.Not.Contain("stale placement feedback"));
            Assert.That(refreshed, Does.Not.Contain("stale run feedback"));
            Assert.That(refreshed, Does.Not.Contain("Clean MVP Validation Reset"));
        }

        private string RefreshText()
        {
            _overlay.RefreshOverlayText();
            return _overlay.overlayText.text;
        }

        private void SetRootBackingField(string fieldName, object value)
        {
            typeof(GameRoot).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_root, value);
        }

        private void SetOverlayPrivateField(string fieldName, object value)
        {
            typeof(BootstrapOverlay).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_overlay, value);
        }

        private static SaveData BuildOverlayDirtySave()
        {
            DungeonLayoutState layout = DungeonLayoutState.CreateEmpty(1, 1);
            new PlacementService().PlaceStructure(layout, 0, 0, StructureSimulationPass.RiskLabBasicId);
            return new SaveData
            {
                dungeonLayout = layout,
                structureRuntime = new StructureRuntimeState { ManaReserve = 45d, Heat = 12d },
                runHistory = new RunHistoryState
                {
                    NextRunSequence = 3,
                    LatestOutcome = new RunOutcomeRecord { RunId = "run.dirty.latest", Success = true, HeatAtStart = 12d },
                    RecentOutcomes = new[] { new RunOutcomeRecord { RunId = "run.dirty.latest", Success = true, HeatAtStart = 12d } }
                }
            };
        }

        private static ContentService BuildOverlayContent(bool enableDevPanel)
        {
            var content = new ContentService();
            typeof(ContentService).GetField("<Bootstrap>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(content, new ContentBootstrap
                {
                    featureFlags = new FeatureFlags { enableDevPanel = enableDevPanel }
                });
            var map = (Dictionary<string, string>)typeof(ContentService).GetField("_stringMap", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(content);
            map["ui.mvp_loop.panel.title"] = "MVP Loop Summary";
            map["ui.mvp_loop.panel.placement_format"] = "Placement: {0}";
            map["ui.mvp_loop.panel.latest_run_format"] = "Latest run: {0}";
            map["ui.mvp_loop.panel.mana_format"] = "Mana reserve: {0:0.##}";
            map["ui.mvp_loop.panel.loot_format"] = "Loot: generated {0}, extracted {1}, tradeable {2}";
            map["ui.mvp_loop.panel.heat_format"] = "Heat: {0:0.##} -> {1:0.##} ({2})";
            map["ui.mvp_loop.panel.research_format"] = "Research: {0}";
            map["ui.mvp_loop.panel.suggestion_format"] = "Next: {0}";
            map["ui.mvp_loop.value.no_placement"] = "No structure placed";
            map["ui.mvp_loop.value.no_run"] = "No run yet";
            map["ui.mvp_loop.value.no_research"] = "No research";
            map["ui.mvp_loop.value.unknown"] = "Unknown";
            map["ui.mvp_loop.run_status.succeeded"] = "Succeeded";
            map["ui.mvp_loop.run_status.failed"] = "Failed";
            map["mvp_loop.suggestion.run_dungeon"] = "Run the dungeon to observe the first outcome.";
            map["ui.guided_mvp.panel.title"] = "Guided MVP Action";
            map["ui.guided_mvp.panel.step_format"] = "Step: {0}";
            map["ui.guided_mvp.panel.status_format"] = "Status: {0}";
            map["ui.guided_mvp.panel.next_action_format"] = "Next action: {0}";
            map["ui.guided_mvp.panel.complete_format"] = "Complete: {0}";
            map["ui.guided_mvp.value.complete_yes"] = "Yes";
            map["ui.guided_mvp.value.complete_no"] = "No";
            map["guided_mvp.step.place_or_modify_structure"] = "Place or modify structure";
            map["guided_mvp.step.run_or_observe"] = "Run or observe";
            map["guided_mvp.step.repeat_or_improve"] = "Repeat or improve";
            map["guided_mvp.status.place_or_modify_structure"] = "Place one structure to start.";
            map["guided_mvp.status.run_or_observe"] = "Run the dungeon next.";
            map["guided_mvp.status.repeat_or_improve"] = "Summary visible.";
            map["guided_mvp.action.place_structure"] = "Place one structure, or modify the selected slot.";
            map["guided_mvp.action.run_dungeon"] = "Run the dungeon and watch the MVP Loop Summary update.";
            map["guided_mvp.action.repeat_or_improve"] = "Run again or adjust one placement based on the summary.";
            map["ui.first_session.status.place_structure"] = "First-session status: place one structure to start the loop.";
            map["ui.first_session.status.run_dungeon"] = "First-session status: structure placed; run the dungeon next.";
            map["ui.first_session.status.observe_summary"] = "First-session status: observe mana, loot, heat, research, and next action.";
            map["ui.first_session.status.complete"] = "First-session loop complete: placement, run, mana, loot, heat, and research are visible.";
            map["ui.mvp_action.panel.title"] = "Minimal MVP Actions";
            map["ui.mvp_action.button.place_or_modify"] = "Place or modify selected";
            map["ui.mvp_action.button.run_or_observe"] = "Run or observe dungeon";
            map["ui.mvp_action.button.show_diagnostics"] = "Show diagnostics";
            map["ui.mvp_action.button.hide_diagnostics"] = "Hide diagnostics";
            map["ui.mvp_action.selection.label"] = "Selected structure: {0}";
            map["ui.mvp_action.posture.label"] = "Run posture: {0}";
            map["run.posture.cautious.name"] = "Cautious";
            map["run.posture.balanced.name"] = "Balanced";
            map["run.posture.greedy.name"] = "Greedy";
            map["ui.mvp_action.selection.mana_generator"] = "Mana Generator";
            map["ui.mvp_action.selection.heat_scrubber"] = "Heat Scrubber";
            map["ui.mvp_action.selection.risk_lab"] = "Risk Lab";
            map["ui.mvp_structure_preview.mana_generator"] = "Role: improves mana reserve.";
            map["ui.mvp_structure_preview.heat_scrubber"] = "Role: lowers heat pressure.";
            map["ui.mvp_structure_preview.risk_lab"] = "Role: clarifies research risk.";
            map["ui.mvp_run_plan_preview.plan_format"] = "Plan: {0} + {1} run.";
            map["ui.mvp_run_plan_preview.tradeoff_format"] = "Expected tradeoff: {0}";
            map["ui.mvp_run_plan_preview.combined_format"] = "{0}\n{1}";
            map["ui.mvp_run_plan_preview.tradeoff.cautious"] = "lower loot, safer heat pressure.";
            map["ui.mvp_run_plan_preview.tradeoff.balanced"] = "standard loot and heat pressure.";
            map["ui.mvp_run_plan_preview.tradeoff.greedy"] = "higher loot, higher heat pressure.";
            map["ui.mvp_run_plan_preview.tradeoff.unknown"] = "run tradeoff unavailable.";
            map["ui.mvp_view.player_mode.status"] = "Player view: diagnostics hidden.";
            map["ui.banner.clean_mvp_validation_reset"] = "Clean MVP validation session reset.";
            map["ui.dev.button.clean_mvp_validation_reset"] = "Clean MVP Validation Reset";
            map["structure.mana_generator.basic.display_name"] = "Mana Generator";
            map["structure.heat_scrubber.basic.display_name"] = "Heat Scrubber";
            map["structure.risk_lab.basic.display_name"] = "Risk Lab";
            map["ui.mvp_label.structure.unknown"] = "Unknown structure";
            map["ui.research.status.blocked_or_invalid"] = "Research unavailable";
            return content;
        }
    }
}
