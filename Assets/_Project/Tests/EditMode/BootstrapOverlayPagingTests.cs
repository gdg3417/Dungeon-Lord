using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class BootstrapOverlayPagingTests
    {
        private GameObject _rootObject;
        private GameObject _overlayObject;
        private GameObject _textObject;
        private GameRoot _root;
        private BootstrapOverlay _overlay;

        [SetUp]
        public void SetUp()
        {
            _rootObject = new GameObject("BootstrapOverlayPagingRootTest");
            _overlayObject = new GameObject("BootstrapOverlayPagingOverlayTest");
            _textObject = new GameObject("BootstrapOverlayPagingTextTest");

            _root = _rootObject.AddComponent<GameRoot>();
            SetContent(BuildContent(includeDiagnosticsLocalization: true));
            SetLine("<BuildLine>k__BackingField", "build-line");
            SetLine("<PendingStateLine>k__BackingField", "pending-line");
            SetLine("<GateStatusLine>k__BackingField", "gate-line");
            SetLine("<KpiLine>k__BackingField", "kpi-line");
            SetLine("<HeatLine>k__BackingField", "heat-line");
            SetLine("<CurrentHeatTierLine>k__BackingField", "current-heat-tier-line");
            SetLine("<TickLine>k__BackingField", "tick-line");
            SetLine("<ManaLine>k__BackingField", "mana-line");
            SetLine("<SaveLine>k__BackingField", "save-line");
            SetLine("<PauseLine>k__BackingField", "pause-line");
            SetLine("<RunLine>k__BackingField", "run-line");
            SetLine("<RunHistoryLine>k__BackingField", "run-history-line");
            SetLine("<RunBreakdownLine>k__BackingField", "run-breakdown-line");
            SetLine("<RunFeedbackLine>k__BackingField", "run-feedback-line");
            SetLine("<RunLootLine>k__BackingField", "run-loot-line");
            SetLine("<RunSurvivalLine>k__BackingField", "run-survival-line");
            SetLine("<RunExtractionLine>k__BackingField", "run-extraction-line");
            SetLine("<RunHeatCoolingLine>k__BackingField", "run-heat-cooling-line");
            SetLine("<RunHeatDeltaLine>k__BackingField", "run-heat-delta-line");
            SetLine("<RunHeatApplicationLine>k__BackingField", "run-heat-application-line");
            SetLine("<RunAdventurerAttractionLine>k__BackingField", "run-attraction-line");
            SetLine("<RunAdventurerInterestForecastLine>k__BackingField", "run-forecast-line");
            SetLine("<RunAdventurerDemandBudgetLine>k__BackingField", "run-demand-budget-line");
            SetLine("<ResearchPendingLine>k__BackingField", "research-pending-line");
            SetLine("<ResearchPendingValidationLine>k__BackingField", "research-pending-validation-line");
            SetLine("<ResearchProgressLine>k__BackingField", "research-progress-line");
            SetLine("<ResearchProgressStateLine>k__BackingField", "research-progress-state-line");
            SetLine("<ResearchCompletionEligibilityLine>k__BackingField", "research-completion-eligibility-line");
            SetLine("<ResearchCompletionPendingApplyLine>k__BackingField", "research-completion-pending-apply-line");
            SetLine("<ResearchCompletionClaimReadinessLine>k__BackingField", "research-completion-claim-readiness-line");
            SetLine("<CompletedResearchStateLine>k__BackingField", "completed-research-state-line");
            SetLine("<ResearchCompletionClaimApplyLine>k__BackingField", "research-completion-claim-apply-line");
            SetLine("<ResearchStatusPresentationLine>k__BackingField", "research-status-presentation-line");
            SetLine("<ResearchStatusSafetyLine>k__BackingField", "research-status-safety-line");
            SetLine("<ResearchVerificationBoundaryLine>k__BackingField", "research-verification-boundary-line");
            SetLine("<ResearchVerificationSafetyLine>k__BackingField", "research-verification-safety-line");

            _root.SetBanner("banner-line");
            DungeonLayoutState layout = DungeonLayoutState.CreateEmpty(1, 1);
            new PlacementService().PlaceStructure(layout, 0, 0, StructureSimulationPass.ManaGeneratorBasicId);
            SetBackingField("_runSimulationService", BuildRunSimulationServiceForActionTest());
            SetSave(new SaveData
            {
                dungeonLayout = layout,
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 9d, IsHeatCrisisActive = true },
                runHistory = new RunHistoryState
                {
                    NextRunSequence = 3,
                    RecentOutcomes = new[] { new RunOutcomeRecord { RunId = "run-history-entry" } }
                },
                researchPending = new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = "research.project.panel" },
                researchProgress = new ResearchProgressState { SlotId = "research.slot.primary", ProjectId = "research.project.panel", ProgressUnits = 1d },
                completedResearch = new CompletedResearchState { ProjectIds = new[] { "research.project.done" } },
                lastOfflineSummary = new OfflineSummary { RuleResolved = true, OfflineSecondsObserved = 12, WouldProcessOfflineProgress = false }
            });

            _overlay = _overlayObject.AddComponent<BootstrapOverlay>();
            _overlay.overlayText = _textObject.AddComponent<TextMeshProUGUI>();
            _overlay.Bind(_root);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_textObject);
            Object.DestroyImmediate(_overlayObject);
            Object.DestroyImmediate(_rootObject);
        }

        [Test]
        public void FullDiagnostics_WhenShown_InitialPageIsRuntimeSummaryWithLocalizedHeader()
        {
            ShowDiagnosticsFromPlayerFacingDefault();
            string text = RefreshText();

            Assert.That(_overlay.FullDiagnosticsPageNumber, Is.EqualTo(1));
            Assert.That(text, Does.StartWith("MVP Loop Summary"));
            Assert.That(text, Does.Contain("Guided MVP Action"));
            Assert.That(text, Does.Contain("Dungeon composition: Mana Generator"));
            Assert.That(text, Does.Not.Contain("Dungeon composition: structure.mana_generator.basic"));
            Assert.That(text, Does.Contain("Next action: Adjust one placement before the next adventurer visit."));
            Assert.That(text, Does.Contain("First-session loop complete: placement, adventurer activity, mana, loot, heat, and research are visible."));
            Assert.That(text, Does.Not.Contain("First-session status: dungeon placement ready; observe adventurer activity next."));
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
            Assert.That(text, Does.Not.Contain("Minimal MVP Actions: [Place or modify selected placement] [Observe Dungeon]"));
            Assert.That(text, Does.Contain("Diagnostics: Runtime Summary Page 1/9\nF1 toggles Dev Panel\nF2 toggles Run Diagnostics focus\nF3 cycles Diagnostics Page"));
            Assert.That(text, Does.Contain("build-line"));
            Assert.That(text, Does.Contain("Mouse wheel or PageUp PageDown scroll diagnostics"));
            Assert.That(text, Does.Not.Contain("banner-line"));
            Assert.That(text, Does.Not.Contain("run-line"));
            Assert.That(text, Does.Not.Contain("run-heat-delta-line"));
        }

        [Test]
        public void DiagnosticsVisibility_DefaultMode_HidesDiagnosticsAndShowsPlayerFacingPanelsStatusAndBanner()
        {
            string text = RefreshText();

            Assert.That(_overlay.DiagnosticsVisible, Is.False);
            Assert.That(_overlay.PlayerFacingPanelsVisible, Is.True);
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
            Assert.That(text, Does.Contain("Dungeon Command (MVP Loop Summary)"));
            Assert.That(text, Does.Contain("== Top Status =="));
            Assert.That(text, Does.Contain("== Analysis and Next Action =="));
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(text, Does.Contain("Player view: diagnostics hidden."));
            Assert.That(text, Does.Contain("banner-line"));
            Assert.That(text, Does.Not.Contain("Diagnostics: Runtime Summary Page 1/9"));
            Assert.That(text, Does.Not.Contain("build-line"));
            Assert.That(text, Does.Not.Contain("F1 toggles Dev Panel"));
            Assert.That(text, Does.Not.Contain("F2 toggles Run Diagnostics focus"));
            Assert.That(text, Does.Not.Contain("F3 cycles Diagnostics Page"));
            Assert.That(text, Does.Not.Contain("Mouse wheel or PageUp PageDown scroll diagnostics"));
        }

        [Test]
        public void PlayerFacingDefault_ShowsPlayableMvpScreenSectionsAndHidesDiagnostics()
        {
            string text = RefreshText();

            Assert.That(text, Does.Contain("Dungeon Command (MVP Loop Summary)"));
            Assert.That(text, Does.Contain("== Top Status =="));
            Assert.That(text, Does.Contain("== Current Dungeon =="));
            Assert.That(text, Does.Contain("Room slot layout:"));
            Assert.That(text, Does.Contain("== Build Choice =="));
            Assert.That(text, Does.Contain("== Activity Setup =="));
            Assert.That(text, Does.Contain("== Latest Adventurer Visit =="));
            Assert.That(text, Does.Not.Contain("== Latest " + "Run =="));
            Assert.That(text, Does.Contain("== Analysis and Next Action =="));
            Assert.That(text, Does.Contain("Selected placement: Room / Basic Room"));
            Assert.That(text, Does.Not.Contain("Selected category: Room"));
            Assert.That(text, Does.Not.Contain("Selected option: Basic Room"));
            Assert.That(text, Does.Contain("Comparison: choose the other option in this category to compare tradeoffs."));
            Assert.That(text, Does.Contain("Expected next adventurer intent:"));
            Assert.That(text, Does.Contain("Debug selected posture: Balanced."));
            Assert.That(text, Does.Contain("Next build step: choose an option, then place or modify it."));
            Assert.That(text, Does.Contain("Adjust placement before the next adventurer visit."));
            Assert.That(text, Does.Not.Contain("Run " + "dungeon"));
            Assert.That(text, Does.Contain("Path complete:"));
            Assert.That(text, Does.Contain("Player view: diagnostics hidden."));
            Assert.That(text, Does.Not.Contain("Diagnostics: Runtime Summary Page 1/9"));
            AssertNoPlayerFacingRawIds(text);
        }

        [Test]
        public void ToggleDiagnosticsVisibility_ShowsDiagnosticsPageOneHeaderBodyAndHints()
        {
            _overlay.CycleFullDiagnosticsPage();
            Assert.That(_overlay.FullDiagnosticsPageNumber, Is.EqualTo(2));

            _overlay.ToggleDiagnosticsVisibility();
            string text = RefreshText();

            Assert.That(_overlay.DiagnosticsVisible, Is.True);
            Assert.That(_overlay.FullDiagnosticsPageNumber, Is.EqualTo(1));
            Assert.That(text, Does.Contain("Diagnostics: Runtime Summary Page 1/9"));
            Assert.That(text, Does.Contain("build-line"));
            Assert.That(text, Does.Contain("F1 toggles Dev Panel"));
            Assert.That(text, Does.Contain("F2 toggles Run Diagnostics focus"));
            Assert.That(text, Does.Contain("F3 cycles Diagnostics Page"));
        }

        [Test]
        public void ToggleDiagnosticsVisibility_WhenShown_HidesDiagnosticsAgain()
        {
            ShowDiagnosticsFromPlayerFacingDefault();

            _overlay.ToggleDiagnosticsVisibility();
            string text = RefreshText();

            Assert.That(_overlay.DiagnosticsVisible, Is.False);
            Assert.That(_overlay.PlayerFacingPanelsVisible, Is.True);
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
            Assert.That(text, Does.Contain("Dungeon Command (MVP Loop Summary)"));
            Assert.That(text, Does.Contain("== Latest Adventurer Visit =="));
            Assert.That(text, Does.Contain("Player view: diagnostics hidden."));
            Assert.That(text, Does.Not.Contain("Diagnostics: Runtime Summary Page 1/9"));
            Assert.That(text, Does.Not.Contain("build-line"));
            Assert.That(text, Does.Not.Contain("F1 toggles Dev Panel"));
            Assert.That(text, Does.Not.Contain("F2 toggles Run Diagnostics focus"));
            Assert.That(text, Does.Not.Contain("F3 cycles Diagnostics Page"));
        }

        [Test]
        public void RunDiagnosticsFocus_FromPlayerFacingMode_RestoresPriorPlayerFacingModeSafely()
        {
            string playerFacingText = RefreshText();
            Assert.That(playerFacingText, Does.Not.Contain("Diagnostics: Runtime Summary Page 1/9"));

            _overlay.ToggleRunDiagnosticsFocus();
            string focusedText = RefreshText();

            Assert.That(_overlay.DiagnosticsVisible, Is.True);
            Assert.That(_overlay.PlayerFacingPanelsVisible, Is.False);
            Assert.That(focusedText, Does.StartWith("Diagnostics: Run Diagnostics Focus"));
            Assert.That(focusedText, Does.Contain("run-line"));
            Assert.That(focusedText, Does.Not.Contain("MVP Loop Summary"));
            Assert.That(focusedText, Does.Not.Contain("First-session loop complete:"));
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.False);

            _overlay.ToggleRunDiagnosticsFocus();
            string restoredText = RefreshText();

            Assert.That(_overlay.DiagnosticsVisible, Is.False);
            Assert.That(_overlay.PlayerFacingPanelsVisible, Is.True);
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
            Assert.That(restoredText, Does.Contain("Dungeon Command (MVP Loop Summary)"));
            Assert.That(restoredText, Does.Contain("== Analysis and Next Action =="));
            Assert.That(restoredText, Does.Contain("Player view: diagnostics hidden."));
            Assert.That(restoredText, Does.Not.Contain("Diagnostics: Runtime Summary Page 1/9"));
        }

        [Test]
        public void PlayerFacingMode_MinimalMvpActionLabelsIncludePlacementRunAndDiagnosticsToggleKeys()
        {
            MinimalMvpActionPanelLabels labels = MinimalMvpActionPanelPresenter.BuildPlacementLabels(
                (key, fallback) => _root.Content.GetString(key, fallback),
                _overlay.SelectedMvpPlacementCategoryId,
                _overlay.SelectedMvpPlacementOptionId,
                _overlay.SelectedMvpStructureId,
                _overlay.GetSelectedMvpRunPostureNameKey());

            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
            Assert.That(labels.PlacementButton, Is.EqualTo("Place or modify selected placement"));
            Assert.That(labels.RunButton, Is.EqualTo("Observe Dungeon"));
            Assert.That(labels.CategoryLabel, Is.EqualTo("Selected category: Room"));
            Assert.That(labels.RoomsGroupHeader, Is.EqualTo("Rooms:"));
            Assert.That(labels.MonstersGroupHeader, Is.EqualTo("Monsters:"));
            Assert.That(labels.TrapsGroupHeader, Is.EqualTo("Traps:"));
            Assert.That(labels.LootGroupHeader, Is.EqualTo("Loot:"));
            Assert.That(labels.SelectedStructureLabel, Is.EqualTo("Selected placement: Basic Room"));
            Assert.That(labels.PreviewText, Is.EqualTo("Role: adds room space and path context."));
            Assert.That(labels.RunPlanPreviewText, Is.EqualTo("Plan: Mana Generator + Balanced adventurer challenge.\nExpected tradeoff: standard loot and heat pressure."));
            Assert.That(labels.ShowDiagnosticsButton, Is.EqualTo("Show diagnostics"));
            Assert.That(labels.HideDiagnosticsButton, Is.EqualTo("Hide diagnostics"));
            Assert.That(_overlay.DiagnosticsVisible, Is.False);
        }

        [Test]
        public void PlayerFacingMode_MinimalMvpActionPanelUsesScrollViewAndGroupedOptionLabels()
        {
            MinimalMvpActionPanelLabels labels = MinimalMvpActionPanelPresenter.BuildPlacementLabels(
                (key, fallback) => _root.Content.GetString(key, fallback),
                _overlay.SelectedMvpPlacementCategoryId,
                _overlay.SelectedMvpPlacementOptionId,
                _overlay.SelectedMvpStructureId,
                _overlay.GetSelectedMvpRunPostureNameKey());
            Rect rect = _overlay.GetMinimalMvpActionPanelRect();

            Assert.That(rect.width, Is.EqualTo(260f));
            Assert.That(rect.height, Is.EqualTo(420f));
            Assert.That(labels.CategoryLabel, Is.EqualTo("Selected category: Room"));
            Assert.That(labels.SelectedStructureLabel, Is.EqualTo("Selected placement: Basic Room"));
            Assert.That(labels.PostureLabel, Is.EqualTo("Debug posture: Balanced"));
            Assert.That(labels.PreviewText, Is.EqualTo("Role: adds room space and path context."));
            Assert.That(labels.RunPlanPreviewText, Is.EqualTo("Plan: Mana Generator + Balanced adventurer challenge.\nExpected tradeoff: standard loot and heat pressure."));
            Assert.That(labels.RoomsGroupHeader, Is.EqualTo("Rooms:"));
            Assert.That(labels.MonstersGroupHeader, Is.EqualTo("Monsters:"));
            Assert.That(labels.TrapsGroupHeader, Is.EqualTo("Traps:"));
            Assert.That(labels.LootGroupHeader, Is.EqualTo("Loot:"));
            Assert.That(labels.BasicRoomSelection, Is.EqualTo("Basic Room"));
            Assert.That(labels.NarrowHallSelection, Is.EqualTo("Narrow Hall"));
            Assert.That(labels.SkeletonSelection, Is.EqualTo("Skeleton"));
            Assert.That(labels.GoblinSelection, Is.EqualTo("Goblin"));
            Assert.That(labels.SpikeTrapSelection, Is.EqualTo("Spike Trap"));
            Assert.That(labels.SnareTrapSelection, Is.EqualTo("Snare Trap"));
            Assert.That(labels.BasicLootNodeSelection, Is.EqualTo("Basic Loot Node"));
            Assert.That(labels.HiddenCacheSelection, Is.EqualTo("Hidden Cache"));
            Assert.That(labels.CautiousPosture, Is.EqualTo("Cautious"));
            Assert.That(labels.BalancedPosture, Is.EqualTo("Balanced"));
            Assert.That(labels.GreedyPosture, Is.EqualTo("Greedy"));
            Assert.That(labels.PlacementButton, Is.EqualTo("Place or modify selected placement"));
            Assert.That(labels.RunButton, Is.EqualTo("Observe Dungeon"));
            Assert.That(labels.ShowDiagnosticsButton, Is.EqualTo("Show diagnostics"));
            Assert.That(labels.HideDiagnosticsButton, Is.EqualTo("Hide diagnostics"));

            string overlaySource = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/UI/BootstrapOverlay.cs"));
            Assert.That(overlaySource, Does.Contain("GUILayout.BeginScrollView"));
            Assert.That(overlaySource, Does.Not.Contain("GUILayout.Button(labels.RoomCategory"));
            Assert.That(overlaySource, Does.Not.Contain("GUILayout.Button(labels.MonsterCategory"));
            Assert.That(overlaySource, Does.Not.Contain("GUILayout.Button(labels.TrapCategory"));
            Assert.That(overlaySource, Does.Not.Contain("GUILayout.Button(labels.LootNodeCategory"));
        }

        [Test]
        public void PlayerFacingMode_MinimalMvpActionPanelSelectedRoomFeedbackUsesLocalizedHelpers()
        {
            Assert.That(_overlay.GetSelectedMvpRoomCapacityText(), Is.EqualTo("Selected room capacity: Monsters 0/1; Traps 0/1; Loot 0/1"));
            Assert.That(_overlay.GetSelectedMvpPlacementFitText(), Is.EqualTo(string.Empty));

            string overlaySource = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/UI/BootstrapOverlay.cs"));
            int panelStart = overlaySource.IndexOf("private void DrawMinimalMvpActionPanel", System.StringComparison.Ordinal);
            int selectedTarget = overlaySource.IndexOf("BuildSelectedTargetText", panelStart, System.StringComparison.Ordinal);
            int selectedCapacity = overlaySource.IndexOf("GetSelectedMvpRoomCapacityText()", selectedTarget, System.StringComparison.Ordinal);
            int selectedFit = overlaySource.IndexOf("GetSelectedMvpPlacementFitText()", selectedCapacity, System.StringComparison.Ordinal);
            int cycleButton = overlaySource.IndexOf("ui.mvp_room_slots.cycle_target_button", selectedFit, System.StringComparison.Ordinal);

            Assert.That(selectedTarget, Is.GreaterThan(panelStart));
            Assert.That(selectedCapacity, Is.GreaterThan(selectedTarget));
            Assert.That(selectedFit, Is.GreaterThan(selectedCapacity));
            Assert.That(cycleButton, Is.GreaterThan(selectedFit));
        }

        [Test]
        public void PlayerFacingMode_MinimalMvpActionPanelSelectedPlacementFitShowsNarrowHallLootBlock()
        {
            SetSave(new SaveData
            {
                dungeonLayout = DungeonLayoutState.CreateEmpty(1, 1),
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 9d },
                runHistory = new RunHistoryState(),
                mvpDungeonPlacements = new MvpDungeonPlacementState
                {
                    Entries = new List<MvpDungeonPlacementEntry>
                    {
                        new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId, 1)
                    },
                    NextRevision = 2
                },
                mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection
                {
                    Rooms = new List<MvpRoomSlotAssignmentState>
                    {
                        new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = 0, RoomOptionId = MvpDungeonPlacementIds.NarrowHallOptionId }
                    }
                }
            });
            SetOverlayBackingField("_selectedMvpPlacementCategoryId", MvpDungeonPlacementIds.LootNodeCategoryId);
            SetOverlayBackingField("_selectedMvpPlacementOptionId", MvpDungeonPlacementIds.BasicLootNodeOptionId);

            Assert.That(_overlay.GetSelectedMvpRoomCapacityText(), Is.EqualTo("Selected room capacity: Monsters 0/1; Traps 0/1; Loot unavailable 0/0"));
            Assert.That(_overlay.GetSelectedMvpPlacementFitText(), Is.EqualTo("Selected placement fit: Loot node cannot fit Room 1 because this room has no loot slot."));
            Assert.That(_overlay.GetSelectedMvpPlacementFitText(), Does.Not.Contain("ui.mvp_room_slots"));
            Assert.That(_overlay.GetSelectedMvpPlacementFitText(), Does.Not.Contain("placement.category"));
        }

        [Test]
        public void AddMvpBasicRoomSlot_ShowsLocalizedSuccessThenBlockedFeedback()
        {
            _overlay.AddMvpBasicRoomSlot();

            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Added Room 2: Basic Room."));
            Assert.That(_root.Save.mvpRoomSlotAssignments.Rooms, Has.Count.EqualTo(2));
            Assert.That(_root.Save.mvpSelectedRoomSlotIndex, Is.EqualTo(1));

            _overlay.AddMvpBasicRoomSlot();

            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Room 2 already exists."));
            Assert.That(_root.Save.mvpRoomSlotAssignments.Rooms, Has.Count.EqualTo(2));
            Assert.That(_overlay.MvpStructurePlacementFeedback, Does.Not.Contain("ui.mvp_room_slots"));
        }

        [Test]
        public void OverlayTextSafeArea_KeepsPlayerFacingTextInsideLeftEdgeAndReservedFromActionPanel()
        {
            RefreshText();

            RectTransform rectTransform = _overlay.overlayText.rectTransform;

            Assert.That(rectTransform.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rectTransform.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rectTransform.pivot, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rectTransform.offsetMin.x, Is.EqualTo(24f));
            Assert.That(rectTransform.offsetMin.y, Is.EqualTo(10f));
            Assert.That(rectTransform.offsetMax.x, Is.EqualTo(-304f));
            Assert.That(rectTransform.offsetMax.y, Is.EqualTo(-14f));
            Assert.That(_overlay.overlayText.alignment, Is.EqualTo(TextAlignmentOptions.TopLeft));
        }

        [Test]
        public void PlayerFacingStructureSelection_DefaultsToManaGeneratorAndExposesLocalizedChoices()
        {
            MinimalMvpActionPanelLabels labels = MinimalMvpActionPanelPresenter.BuildLabels(
                (key, fallback) => _root.Content.GetString(key, fallback),
                _overlay.GetSelectedMvpStructureNameKey(),
                _overlay.SelectedMvpStructureId);

            Assert.That(_overlay.SelectedMvpStructureId, Is.EqualTo(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(_overlay.GetSelectedMvpStructureDisplayName(), Is.EqualTo("Mana Generator"));
            Assert.That(labels.SelectedStructureLabel, Is.EqualTo("Selected placement: Mana Generator"));
            Assert.That(labels.PreviewText, Is.EqualTo("Plan: Mana Generator + Balanced adventurer challenge.\nExpected tradeoff: standard loot and heat pressure."));
            Assert.That(labels.ManaGeneratorSelection, Is.EqualTo("Mana Generator"));
            Assert.That(labels.HeatScrubberSelection, Is.EqualTo("Heat Scrubber"));
            Assert.That(labels.RiskLabSelection, Is.EqualTo("Risk Lab"));
        }

        [TestCase(StructureSimulationPass.ManaGeneratorBasicId, "Mana Generator", MinimalMvpActionPanelPresenter.ManaGeneratorSelectionKey, "Plan: Mana Generator + Balanced adventurer challenge.\nExpected tradeoff: standard loot and heat pressure.")]
        [TestCase(StructureSimulationPass.HeatScrubberBasicId, "Heat Scrubber", MinimalMvpActionPanelPresenter.HeatScrubberSelectionKey, "Plan: Heat Scrubber + Balanced adventurer challenge.\nExpected tradeoff: standard loot and heat pressure.")]
        [TestCase(StructureSimulationPass.RiskLabBasicId, "Risk Lab", MinimalMvpActionPanelPresenter.RiskLabSelectionKey, "Plan: Risk Lab + Balanced adventurer challenge.\nExpected tradeoff: standard loot and heat pressure.")]
        public void PlayerFacingStructureSelection_AllowsExistingMvpSafeStructureOptions(string structureId, string displayName, string selectionKey, string previewText)
        {
            bool selected = _overlay.SelectMvpStructure(structureId);
            MinimalMvpActionPanelLabels labels = MinimalMvpActionPanelPresenter.BuildLabels(
                (key, fallback) => _root.Content.GetString(key, fallback),
                _overlay.GetSelectedMvpStructureNameKey(),
                _overlay.SelectedMvpStructureId);

            Assert.That(selected, Is.True);
            Assert.That(_overlay.SelectedMvpStructureId, Is.EqualTo(structureId));
            Assert.That(_overlay.GetSelectedMvpStructureNameKey(), Is.EqualTo(selectionKey));
            Assert.That(_overlay.GetSelectedMvpStructureDisplayName(), Is.EqualTo(displayName));
            Assert.That(labels.SelectedStructureLabel, Is.EqualTo($"Selected placement: {displayName}"));
            Assert.That(labels.PreviewText, Is.EqualTo(previewText));
            Assert.That(labels.PreviewText, Does.Not.Contain(structureId));
        }

        [Test]
        public void CleanMvpValidationResetPreview_DefaultsToManaGeneratorBalancedPlan()
        {
            SetBackingField("<DevPanelEnabled>k__BackingField", true);
            _overlay.SelectMvpStructure(StructureSimulationPass.RiskLabBasicId);
            _overlay.SelectMvpRunPosture(RunPostureResolver.GreedyId);
            typeof(BootstrapOverlay).GetField("_mvpStructurePlacementFeedback", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_overlay, "stale placement feedback");
            typeof(BootstrapOverlay).GetField("_mvpRunResultFeedback", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_overlay, "stale run feedback");

            bool didReset = _overlay.ResetCleanMvpValidationSessionFromDevPanel();
            string text = RefreshText();

            Assert.That(didReset, Is.True);
            Assert.That(_overlay.SelectedMvpStructureId, Is.EqualTo(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(_overlay.SelectedMvpRunPostureId, Is.EqualTo(RunPostureResolver.BalancedId));
            Assert.That(_overlay.GetSelectedMvpRunPlanPreviewText(), Is.EqualTo("Plan: Mana Generator + Balanced adventurer challenge.\nExpected tradeoff: standard loot and heat pressure."));
            Assert.That(text, Does.Contain("Plan: Mana Generator + Balanced adventurer challenge."));
            Assert.That(text, Does.Contain("Expected tradeoff: standard loot and heat pressure."));
            Assert.That(text, Does.Not.Contain("stale placement feedback"));
            Assert.That(text, Does.Not.Contain("stale run feedback"));
        }

        [TestCase(RunPostureResolver.CautiousId, "Plan: Mana Generator + Cautious adventurer challenge.\nExpected tradeoff: lower loot, safer heat pressure.")]
        [TestCase(RunPostureResolver.GreedyId, "Plan: Mana Generator + Greedy adventurer challenge.\nExpected tradeoff: higher loot, higher heat pressure.")]
        public void SelectingRunPosture_UpdatesReadOnlyRunPlanPreview(string postureId, string expectedPreview)
        {
            bool selected = _overlay.SelectMvpRunPosture(postureId);
            string text = RefreshText();

            Assert.That(selected, Is.True);
            Assert.That(_overlay.GetSelectedMvpRunPlanPreviewText(), Is.EqualTo(expectedPreview));
            Assert.That(text, Does.Contain(expectedPreview));
        }

        [Test]
        public void RunPlanPreview_DoesNotExposeRawStructureOrPostureIds()
        {
            _overlay.SelectMvpRunPosture(RunPostureResolver.GreedyId);

            string preview = _overlay.GetSelectedMvpRunPlanPreviewText();
            string text = RefreshText();

            Assert.That(preview, Does.Not.Contain(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(preview, Does.Not.Contain(RunPostureResolver.GreedyId));
            Assert.That(preview, Does.Not.Contain("structure."));
            Assert.That(preview, Does.Not.Contain("run.posture"));
            Assert.That(text, Does.Contain(preview));
        }

        [Test]
        public void PlayerFacingStructureSelection_RejectsUnknownStructureWithoutChangingSelection()
        {
            _overlay.SelectMvpStructure(StructureSimulationPass.RiskLabBasicId);

            bool selected = _overlay.SelectMvpStructure("structure.debug.not_player_facing");

            Assert.That(selected, Is.False);
            Assert.That(_overlay.SelectedMvpStructureId, Is.EqualTo(StructureSimulationPass.RiskLabBasicId));
        }

        [Test]
        public void DefaultPlayerFacingMode_PlayerPlacementAndRunActionsRemainAvailable()
        {
            Assert.That(_overlay.DiagnosticsVisible, Is.False);
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);

            _overlay.PlaceSelectedMvpStructure();
            string placementText = RefreshText();

            Assert.That(placementText, Does.Contain("Changed placement: Empty slot -> Room: Basic Room. Role: adds room space and path context."));
            Assert.That(placementText, Does.Contain("Dungeon composition: Room: Basic Room"));
            Assert.That(placementText, Does.Contain("Dungeon Command (MVP Loop Summary)"));
            Assert.That(placementText, Does.Not.Contain("Diagnostics: Runtime Summary Page 1/9"));

            SetBackingField("_runSimulationService", BuildRunSimulationServiceForActionTest());
            SetBackingField("<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "bootstrap_overlay_action_test.json", useAtomicWrites = false }));

            _overlay.RunOrObserveDungeon();
            string runText = RefreshText();

            string runFeedback = _overlay.MvpRunResultFeedback;
            bool hasLocalizedRunResult = runText.Contains("Succeeded") || runText.Contains("Failed");

            Assert.That(_root.BannerMessage, Is.EqualTo("Run simulated."));
            Assert.That(runFeedback, Is.Not.Empty);
            Assert.That(hasLocalizedRunResult, Is.True, "Fixture may validly produce success or failure; both must remain localized player-facing results.");
            Assert.That(runText, Does.Contain("== Latest Adventurer Visit =="));
            Assert.That(runText, Does.Contain("== Analysis and Next Action =="));
            Assert.That(runText, Does.Contain("Loot: 0/0 recovered; 0 tradeable."));
            Assert.That(runText, Does.Contain("Heat: 17 -> 17"));
            Assert.That(runFeedback, Does.Contain("Mana 9."));
            Assert.That(runFeedback, Does.Contain("Loot 0/0/0."));
            Assert.That(runFeedback, Does.Contain("Heat 17->17."));
            Assert.That(runFeedback, Does.Not.Contain("ui.mvp_run_feedback.outcome_cue"));
            Assert.That(runFeedback, Does.Not.Contain("run-1"));
            Assert.That(runFeedback, Does.Not.Contain(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(runFeedback, Does.Not.Contain("run.heat_delta.rule.test"));
            string copiedSmoke = _overlay.CopyFullSmokeTextToClipboard();
            Assert.That(copiedSmoke, Does.Contain(runFeedback));
            Assert.That(copiedSmoke, Does.Contain("First-session"));
            Assert.That(copiedSmoke, Does.Contain("First Dungeon Contract"));
            Assert.That(copiedSmoke, Does.Contain("Path built:"));
            Assert.That(copiedSmoke, Does.Contain("Loot recovered:"));
            Assert.That(copiedSmoke, Does.Contain("Contract status:"));
            AssertNoRawPlayerFacingSmokeIds(copiedSmoke);
            Assert.That(runText, Does.Contain("Latest Adventurer Visit"));
            Assert.That(runText, Does.Not.Contain("Diagnostics: Runtime Summary Page 1/9"));
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
        }

        [Test]
        public void RunOrObserveDungeon_UsesResolvedIntentInsteadOfSelectedDebugPosture()
        {
            SetBackingField("_runSimulationService", BuildRunSimulationServiceForActionTest());
            SetBackingField("<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "bootstrap_overlay_intent_posture_test.json", useAtomicWrites = false }));
            _root.Save.structureRuntime.Heat = 0d;
            _overlay.SelectMvpPlacementCategory(MvpDungeonPlacementIds.LootNodeCategoryId);
            _overlay.SelectMvpPlacementOption(MvpDungeonPlacementIds.BasicLootNodeOptionId);
            _overlay.PlaceSelectedMvpStructure();
            _overlay.SelectMvpRunPosture(RunPostureResolver.CautiousId);
            MvpPlayerLoopSummary beforeRunSummary = _root.ResolveMvpPlayerLoopSummary();
            string expectedResolvedPostureId = beforeRunSummary.AdventurerRunIntent.PostureId;
            string expectedResolvedPostureNameKey = AdventurerRunIntentPresenter.ResolvePostureNameKey(expectedResolvedPostureId);
            string expectedResolvedPostureName = _root.Content.GetString(expectedResolvedPostureNameKey, expectedResolvedPostureNameKey);

            Assert.That(beforeRunSummary.AdventurerRunIntent.RuleResolved, Is.True);
            Assert.That(expectedResolvedPostureId, Is.Not.EqualTo(_overlay.SelectedMvpRunPostureId));

            _overlay.RunOrObserveDungeon();

            Assert.That(_root.Save.runHistory.LatestOutcome.RunPostureId, Is.EqualTo(expectedResolvedPostureId));
            Assert.That(_root.Save.runHistory.LatestOutcome.RunPostureId, Is.Not.EqualTo(_overlay.SelectedMvpRunPostureId));
            Assert.That(_overlay.SelectedMvpRunPostureId, Is.EqualTo(RunPostureResolver.CautiousId));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Contain($"Latest visit intent: {expectedResolvedPostureName}. Challenge posture used: {expectedResolvedPostureName}. Debug selected posture: Cautious."));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain("Expected next adventurer intent"));
            Assert.That(_overlay.MvpRunResultFeedback.Split(new[] { "Challenge posture used:" }, System.StringSplitOptions.None).Length - 1, Is.EqualTo(1));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain("debug fallback"));
            string copiedSmoke = _overlay.CopyFullSmokeTextToClipboard();
            Assert.That(copiedSmoke, Does.Contain($"Latest visit intent: {expectedResolvedPostureName}; challenge posture used: {expectedResolvedPostureName}; debug selected posture: Cautious; rule source: run.adventurer_intent.rule.test; error code: 0; fallback used: False."));
        }

        [Test]
        public void RunOrObserveDungeon_FallsBackToSelectedDebugPostureWhenIntentResolutionFails()
        {
            RunSimulationService service = BuildRunSimulationServiceForActionTest();
            service.Config.AdventurerIntentRuleSourceId = string.Empty;
            SetBackingField("_runSimulationService", service);
            SetBackingField("<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "bootstrap_overlay_intent_fallback_test.json", useAtomicWrites = false }));
            _overlay.SelectMvpRunPosture(RunPostureResolver.GreedyId);

            _overlay.RunOrObserveDungeon();

            Assert.That(_root.Save.runHistory.LatestOutcome.RunPostureId, Is.EqualTo(RunPostureResolver.GreedyId));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Contain("Latest visit intent unavailable. Challenge posture used debug fallback: Greedy. Debug selected posture: Greedy."));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain("Expected next adventurer intent"));
            Assert.That(_overlay.MvpRunResultFeedback.Split(new[] { "Challenge posture used" }, System.StringSplitOptions.None).Length - 1, Is.EqualTo(1));
            string copiedSmoke = _overlay.CopyFullSmokeTextToClipboard();
            Assert.That(copiedSmoke, Does.Contain("Latest visit intent: Unavailable; challenge posture used: Greedy; debug selected posture: Greedy; rule source: ; error code: 1; fallback used: True."));
        }

        [Test]
        public void FirstSessionMvpUxFlow_HardensSelectionPlacementRunDiagnosticsAndRawIdBoundaries()
        {
            SetSave(new SaveData
            {
                dungeonLayout = DungeonLayoutState.CreateEmpty(1, 1),
                structureRuntime = new StructureRuntimeState { Heat = 4d, ManaReserve = 6d },
                runHistory = new RunHistoryState()
            });
            SetBackingField("_runSimulationService", BuildRunSimulationServiceForActionTest());
            SetBackingField("<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "bootstrap_overlay_vs15_flow_test.json", useAtomicWrites = false }));

            string defaultText = RefreshText();

            Assert.That(_overlay.DiagnosticsVisible, Is.False);
            Assert.That(_overlay.PlayerFacingPanelsVisible, Is.True);
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
            Assert.That(_overlay.SelectedMvpStructureId, Is.EqualTo(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(defaultText, Does.Contain("Dungeon Command (MVP Loop Summary)"));
            Assert.That(defaultText, Does.Contain("== Build Choice =="));
            Assert.That(defaultText, Does.Contain("Path complete:"));
            Assert.That(defaultText, Does.Contain("First Dungeon Contract: In progress. Loot 0 / 10, path incomplete."));
            Assert.That(defaultText, Does.Not.Contain("Path built:"));
            Assert.That(defaultText, Does.Not.Contain("Visit observed:"));
            Assert.That(defaultText, Does.Contain("Player view: diagnostics hidden."));
            Assert.That(defaultText, Does.Not.Contain("Diagnostics: Runtime Summary Page 1/9"));

            Assert.That(_overlay.SelectMvpPlacementCategory(MvpDungeonPlacementIds.RoomCategoryId), Is.True);
            Assert.That(_overlay.GetSelectedMvpPlacementPreviewText(), Is.EqualTo("Role: adds room space and path context."));
            _overlay.PlaceSelectedMvpStructure();
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed placement: Empty slot -> Room: Basic Room. Role: adds room space and path context."));

            Assert.That(_overlay.SelectMvpPlacementCategory(MvpDungeonPlacementIds.MonsterCategoryId), Is.True);
            Assert.That(_overlay.GetSelectedMvpPlacementPreviewText(), Is.EqualTo("Role: adds danger and mana pressure."));
            _overlay.PlaceSelectedMvpStructure();
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed placement: Empty slot -> Monster: Skeleton. Role: adds danger and mana pressure."));

            Assert.That(_overlay.SelectMvpPlacementCategory(MvpDungeonPlacementIds.TrapCategoryId), Is.True);
            Assert.That(_overlay.GetSelectedMvpPlacementPreviewText(), Is.EqualTo("Role: adds danger, heat, and path pressure."));
            _overlay.PlaceSelectedMvpStructure();
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed placement: Empty slot -> Trap: Spike Trap. Role: adds danger, heat, and path pressure."));

            _overlay.RunOrObserveDungeon();
            string playerFacingText = RefreshText();

            Assert.That(_overlay.MvpRunResultFeedback, Is.Not.Empty);
            Assert.That(playerFacingText, Does.Contain("Changed placement: Empty slot -> Trap: Spike Trap. Role: adds danger, heat, and path pressure."));
            Assert.That(playerFacingText, Does.Contain("== Latest Adventurer Visit =="));
            Assert.That(playerFacingText, Does.Contain("== Analysis and Next Action =="));
            Assert.That(_overlay.CopyFullSmokeTextToClipboard(), Does.Contain(_overlay.MvpRunResultFeedback));
            AssertNoPlayerFacingRawIds(playerFacingText);
            AssertNoPlayerFacingRawIds(_overlay.MvpStructurePlacementFeedback);
            AssertNoPlayerFacingRawIds(_overlay.MvpRunResultFeedback);

            _overlay.ToggleRunDiagnosticsFocus();
            string focusedText = RefreshText();

            Assert.That(_overlay.PlayerFacingPanelsVisible, Is.False);
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.False);
            Assert.That(focusedText, Does.StartWith("Diagnostics: Run Diagnostics Focus"));
            Assert.That(focusedText, Does.Not.Contain("MVP Loop Summary"));
            Assert.That(focusedText, Does.Not.Contain("Minimal MVP Actions"));
            Assert.That(focusedText, Does.Not.Contain("Changed placement: Empty slot -> Trap: Spike Trap"));
            Assert.That(focusedText, Does.Not.Contain(_overlay.MvpRunResultFeedback));

            _overlay.ToggleRunDiagnosticsFocus();
            string restoredText = RefreshText();

            Assert.That(_overlay.DiagnosticsVisible, Is.False);
            Assert.That(_overlay.PlayerFacingPanelsVisible, Is.True);
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
            Assert.That(restoredText, Does.Contain("MVP Loop Summary"));
            Assert.That(restoredText, Does.Contain("Player view: diagnostics hidden."));
            AssertNoPlayerFacingRawIds(restoredText);

            _root.TryMvpPlaceOrModifySelectedStructure(StructureSimulationPass.RiskLabBasicId, out _);
            _overlay.ToggleDiagnosticsVisibility();
            AssertPage(1, "Runtime Summary");

            CycleAndAssertPage(2, "Run Diagnostics");
            CycleAndAssertPage(3, "Heat Diagnostics");
            CycleAndAssertPage(4, "Systems Diagnostics");
            string systemsDiagnostics = RefreshText();
            Assert.That(systemsDiagnostics, Does.Contain(StructureSimulationPass.RiskLabBasicId));
            CycleAndAssertPage(5, "Research Diagnostics");
            CycleAndAssertPage(6, "Research Status Presentation Diagnostics");
            CycleAndAssertPage(7, "Research Status Safety Diagnostics");
            CycleAndAssertPage(8, "Research Verification Boundary Diagnostics");
            CycleAndAssertPage(9, "Research Verification Safety Diagnostics");
            CycleAndAssertPage(1, "Runtime Summary");

            Assert.That(_overlay.DevPanelVisible, Is.False);
            _overlay.ToggleDevPanel();
            Assert.That(_overlay.DevPanelVisible, Is.True);
            _overlay.ToggleDevPanel();
            Assert.That(_overlay.DevPanelVisible, Is.False);
        }


        [Test]
        public void MvpLoopPanel_CompletionPendingResearch_UsesGameRootScaffoldConfigPath()
        {
            const string projectId = "research.project.panel_config";
            SetContent(BuildContentWithResearchScaffold(projectId));
            SetSave(new SaveData
            {
                structureRuntime = new StructureRuntimeState { Heat = 3d, ManaReserve = 14d },
                runHistory = new RunHistoryState(),
                researchPending = new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = projectId },
                researchProgress = new ResearchProgressState
                {
                    SlotId = "research.slot.primary",
                    ProjectId = projectId,
                    ProgressUnits = 2d,
                    CompletionPending = true,
                    RuleSourceIdUsed = "research.progress.rule.test"
                }
            });

            MvpPlayerLoopSummary summary = _root.ResolveMvpPlayerLoopSummary();
            string text = RefreshText();

            Assert.That(summary.ResearchStatusKey, Is.EqualTo("ui.research.status.verification_required"));
            Assert.That(summary.ResearchVerificationRuleResolved, Is.True);
            Assert.That(text, Does.Contain("Research: Verification required"));
            Assert.That(text, Does.Not.Contain("Research: Research unavailable"));
        }


        [Test]
        public void MinimalMvpActionPlacementBanner_UsesLocalizedPlacementNameInsteadOfRawId()
        {
            DungeonLayoutState layout = DungeonLayoutState.CreateEmpty(1, 1);
            SetSave(new SaveData
            {
                dungeonLayout = layout,
                structureRuntime = new StructureRuntimeState()
            });

            _overlay.PlaceSelectedMvpStructure();

            Assert.That(_overlay.MvpStructurePlacementFeedback, Does.Contain("Changed placement: Empty slot -> Room: Basic Room"));
            Assert.That(_root.BannerMessage, Does.Not.Contain(MvpDungeonPlacementIds.BasicRoomOptionId));
        }


        [TestCase(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId, "Room", "Basic Room")]
        [TestCase(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, "Monster", "Skeleton")]
        [TestCase(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId, "Trap", "Spike Trap")]
        [TestCase(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId, "Loot node", "Basic Loot Node")]
        public void MinimalMvpActionPlacement_UsesSelectedPlacementBannerAndLocalizedSummary(string categoryId, string optionId, string categoryName, string displayName)
        {
            DungeonLayoutState layout = DungeonLayoutState.CreateEmpty(1, 1);
            SetSave(new SaveData
            {
                dungeonLayout = layout,
                structureRuntime = new StructureRuntimeState()
            });

            Assert.That(_overlay.SelectMvpPlacementCategory(categoryId), Is.True);
            Assert.That(_overlay.SelectMvpPlacementOption(optionId), Is.True);
            _overlay.PlaceSelectedMvpStructure();
            string text = RefreshText();

            Assert.That(_root.GetSelectedSlotStructureId(), Is.Empty);
            Assert.That(_overlay.MvpStructurePlacementFeedback, Does.Contain($"Changed placement: Empty slot -> {categoryName}: {displayName}"));
            if (string.Equals(categoryId, MvpDungeonPlacementIds.RoomCategoryId, System.StringComparison.Ordinal))
            {
                Assert.That(text, Does.Contain($"Dungeon composition: {categoryName}: {displayName}"));
            }
            else
            {
                Assert.That(text, Does.Contain($"Dungeon composition: Room: Basic Room; {categoryName}: {displayName}"));
            }

            Assert.That(text, Does.Contain($"Changed placement: Empty slot -> {categoryName}: {displayName}"));
            Assert.That(text, Does.Not.Contain(categoryId));
            Assert.That(text, Does.Not.Contain(optionId));
            Assert.That(_root.BannerMessage, Does.Not.Contain(optionId));
        }

        [Test]
        public void PlacementFeedback_EmptySlotToManaGenerator_AppearsAfterPlaceOrModifySelected()
        {
            DungeonLayoutState layout = DungeonLayoutState.CreateEmpty(1, 1);
            SetSave(new SaveData
            {
                dungeonLayout = layout,
                structureRuntime = new StructureRuntimeState()
            });

            _overlay.PlaceSelectedMvpStructure();
            string text = RefreshText();

            Assert.That(_root.GetSelectedSlotStructureId(), Is.Empty);
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed placement: Empty slot -> Room: Basic Room. Role: adds room space and path context."));
            Assert.That(text, Does.Contain("Changed placement: Empty slot -> Room: Basic Room. Role: adds room space and path context."));
            Assert.That(text, Does.Not.Contain(MvpDungeonPlacementIds.BasicRoomOptionId));
        }

        [Test]
        public void PlacementFeedback_UpdatesAfterEachSelectedStructurePlacement()
        {
            DungeonLayoutState layout = DungeonLayoutState.CreateEmpty(1, 1);
            SetSave(new SaveData
            {
                dungeonLayout = layout,
                structureRuntime = new StructureRuntimeState()
            });

            _overlay.PlaceSelectedMvpStructure();
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed placement: Empty slot -> Room: Basic Room. Role: adds room space and path context."));

            _overlay.PlaceSelectedMvpStructure();
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed placement: Room: Basic Room -> Room: Basic Room. Role: adds room space and path context."));

            _overlay.SelectMvpPlacementCategory(MvpDungeonPlacementIds.MonsterCategoryId);
            _overlay.PlaceSelectedMvpStructure();
            string text = RefreshText();

            Assert.That(_root.GetSelectedSlotStructureId(), Is.Empty);
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed placement: Empty slot -> Monster: Skeleton. Role: adds danger and mana pressure."));
            Assert.That(text, Does.Contain("Changed placement: Empty slot -> Monster: Skeleton. Role: adds danger and mana pressure."));
            Assert.That(text, Does.Not.Contain(MvpDungeonPlacementIds.SkeletonOptionId));
        }

        [Test]
        public void PlacementFeedback_UnknownPriorPlacementUsesSafeFallback()
        {
            DungeonLayoutState layout = DungeonLayoutState.CreateEmpty(1, 1);
            new PlacementService().PlaceStructure(layout, 0, 0, "structure.debug.not_player_facing");
            SetSave(new SaveData
            {
                dungeonLayout = layout,
                structureRuntime = new StructureRuntimeState()
            });

            _overlay.SelectMvpPlacementCategory(MvpDungeonPlacementIds.MonsterCategoryId);
            _overlay.PlaceSelectedMvpStructure();
            string text = RefreshText();

            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed placement: Empty slot -> Monster: Skeleton. Role: adds danger and mana pressure."));
            Assert.That(text, Does.Not.Contain("structure.debug.not_player_facing"));
        }

        [Test]
        public void RunOrObserveDungeon_DoesNotOverwritePlacementFeedback()
        {
            DungeonLayoutState layout = DungeonLayoutState.CreateEmpty(1, 1);
            SetSave(new SaveData
            {
                dungeonLayout = layout,
                structureRuntime = new StructureRuntimeState(),
                runHistory = new RunHistoryState()
            });
            SetBackingField("_runSimulationService", BuildRunSimulationServiceForActionTest());
            SetBackingField("<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "bootstrap_overlay_feedback_run_test.json", useAtomicWrites = false }));
            _overlay.PlaceSelectedMvpStructure();

            _overlay.RunOrObserveDungeon();
            string text = RefreshText();

            Assert.That(_root.BannerMessage, Is.EqualTo("Run simulated."));
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed placement: Empty slot -> Room: Basic Room. Role: adds room space and path context."));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Contain("Adventurer visit result: succeeded."));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Contain("Adventurers: "));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain(AdventurerPartyCompositionResolver.WarriorClassId));
            Assert.That(text, Does.Contain("Party: "));
            Assert.That(text, Does.Contain("Changed placement: Empty slot -> Room: Basic Room. Role: adds room space and path context."));
            Assert.That(text, Does.Contain("Succeeded"));
            Assert.That(_overlay.CopyFullSmokeTextToClipboard(), Does.Contain(_overlay.MvpRunResultFeedback));
        }

        [Test]
        public void RunOrObserveDungeon_RunFeedbackIncludesLocalizedOutcomeCue()
        {
            SetSave(new SaveData
            {
                dungeonLayout = DungeonLayoutState.CreateEmpty(1, 1),
                structureRuntime = new StructureRuntimeState { Heat = 49d, ManaReserve = 6d },
                runHistory = new RunHistoryState()
            });
            SetBackingField("_runSimulationService", BuildRunSimulationServiceForActionTest());
            SetBackingField("<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "bootstrap_overlay_run_feedback_outcome_cue_test.json", useAtomicWrites = false }));

            _overlay.RunOrObserveDungeon();
            string text = RefreshText();

            Assert.That(_root.BannerMessage, Is.EqualTo("Run simulated."));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Contain("Outcome cue: the adventurer visit failed, so reduce pressure before the next challenge."));
            Assert.That(text, Does.Contain("== Latest Adventurer Visit =="));
            Assert.That(text, Does.Contain("Failed"));
            Assert.That(_overlay.CopyFullSmokeTextToClipboard(), Does.Contain(_overlay.MvpRunResultFeedback));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain("ui.mvp_run_feedback.outcome_cue"));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain("run.posture"));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain("run-1"));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain("run.heat_delta.rule.test"));
        }

        [Test]
        public void RunFeedback_UpdatesAfterEachRunAndDoesNotExposeRawIds()
        {
            SetSave(new SaveData
            {
                dungeonLayout = DungeonLayoutState.CreateEmpty(1, 1),
                structureRuntime = new StructureRuntimeState { Heat = 4d, ManaReserve = 6d },
                runHistory = new RunHistoryState()
            });
            SetBackingField("_runSimulationService", BuildRunSimulationServiceForActionTest());
            SetBackingField("<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "bootstrap_overlay_run_feedback_updates_test.json", useAtomicWrites = false }));

            _overlay.RunOrObserveDungeon();
            string firstFeedback = _overlay.MvpRunResultFeedback;
            _root.Save.structureRuntime.Heat = 8d;

            _overlay.RunOrObserveDungeon();
            string text = RefreshText();

            Assert.That(firstFeedback, Is.Not.Empty);
            Assert.That(_overlay.MvpRunResultFeedback, Is.Not.EqualTo(firstFeedback));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Contain("Heat 8->8."));
            Assert.That(text, Does.Contain("Heat: 8 -> 8"));
            Assert.That(_overlay.CopyFullSmokeTextToClipboard(), Does.Contain(_overlay.MvpRunResultFeedback));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain("ui.mvp_run_feedback.posture_format"));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain("run.posture"));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain("run-2"));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain("run.heat_delta.rule.test"));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain(AdventurerPartyCompositionResolver.RogueClassId));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain("adventurer.class."));
        }

        [Test]
        public void RunDiagnosticsFocus_HidesRunFeedbackAndRestoresSafely()
        {
            SetSave(new SaveData
            {
                dungeonLayout = DungeonLayoutState.CreateEmpty(1, 1),
                structureRuntime = new StructureRuntimeState { Heat = 4d, ManaReserve = 6d },
                runHistory = new RunHistoryState()
            });
            SetBackingField("_runSimulationService", BuildRunSimulationServiceForActionTest());
            SetBackingField("<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "bootstrap_overlay_run_feedback_focus_test.json", useAtomicWrites = false }));
            _overlay.RunOrObserveDungeon();
            string feedback = _overlay.MvpRunResultFeedback;

            _overlay.ToggleRunDiagnosticsFocus();
            string focusedText = RefreshText();

            Assert.That(focusedText, Does.Not.Contain(feedback));

            _overlay.ToggleRunDiagnosticsFocus();
            string restoredText = RefreshText();

            Assert.That(restoredText, Does.Contain("== Latest Adventurer Visit =="));
            Assert.That(restoredText, Does.Contain("== Analysis and Next Action =="));
            Assert.That(_overlay.CopyFullSmokeTextToClipboard(), Does.Contain(feedback));
        }

        [Test]
        public void RunDiagnosticsFocus_HidesPlacementFeedbackAndRestoresSafely()
        {
            DungeonLayoutState layout = DungeonLayoutState.CreateEmpty(1, 1);
            SetSave(new SaveData
            {
                dungeonLayout = layout,
                structureRuntime = new StructureRuntimeState()
            });
            _overlay.PlaceSelectedMvpStructure();

            _overlay.ToggleRunDiagnosticsFocus();
            string focusedText = RefreshText();

            Assert.That(focusedText, Does.Not.Contain("Changed placement: Empty slot -> Room: Basic Room"));

            _overlay.ToggleRunDiagnosticsFocus();
            string restoredText = RefreshText();

            Assert.That(restoredText, Does.Contain("Changed placement: Empty slot -> Room: Basic Room. Role: adds room space and path context."));
        }

        [Test]
        public void DiagnosticsStillPreserveRawStructureIdWhereUseful()
        {
            _overlay.SelectMvpStructure(StructureSimulationPass.RiskLabBasicId);
            _root.TryMvpPlaceOrModifySelectedStructure(StructureSimulationPass.RiskLabBasicId, out _);
            ShowDiagnosticsFromPlayerFacingDefault();

            while (_overlay.FullDiagnosticsPageNumber != 4)
            {
                _overlay.CycleFullDiagnosticsPage();
            }

            string text = RefreshText();
            Assert.That(text, Does.Contain(StructureSimulationPass.RiskLabBasicId));
        }

        [Test]
        public void RunDiagnosticsFocus_RestoresPlayerFacingDefaultWithSelectedStructurePreserved()
        {
            _overlay.SelectMvpStructure(StructureSimulationPass.HeatScrubberBasicId);

            _overlay.ToggleRunDiagnosticsFocus();
            string focusedText = RefreshText();

            Assert.That(_overlay.PlayerFacingPanelsVisible, Is.False);
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.False);
            Assert.That(_overlay.SelectedMvpStructureId, Is.EqualTo(StructureSimulationPass.HeatScrubberBasicId));
            Assert.That(focusedText, Does.Not.Contain("MVP Loop Summary"));
            Assert.That(focusedText, Does.Not.Contain("Minimal MVP Actions"));
            Assert.That(focusedText, Does.Not.Contain("Selected placement"));
            Assert.That(focusedText, Does.Not.Contain("Heat Scrubber"));
            Assert.That(focusedText, Does.Not.Contain("Role:"));

            _overlay.ToggleRunDiagnosticsFocus();

            Assert.That(_overlay.PlayerFacingPanelsVisible, Is.True);
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
            Assert.That(_overlay.SelectedMvpStructureId, Is.EqualTo(StructureSimulationPass.HeatScrubberBasicId));
            Assert.That(_overlay.GetSelectedMvpStructureDisplayName(), Is.EqualTo("Heat Scrubber"));
            Assert.That(_overlay.GetSelectedMvpStructurePreviewText(), Is.EqualTo("Role: lowers heat pressure."));
        }

        [Test]
        public void FullDiagnostics_F3PageCycle_WrapsFromFinalDiagnosticsBackToRuntimeSummary()
        {
            ShowDiagnosticsFromPlayerFacingDefault();
            AssertPage(1, "Runtime Summary");
            CycleAndAssertPage(2, "Run Diagnostics");
            CycleAndAssertPage(3, "Heat Diagnostics");
            CycleAndAssertPage(4, "Systems Diagnostics");
            CycleAndAssertPage(5, "Research Diagnostics");
            CycleAndAssertPage(6, "Research Status Presentation Diagnostics");
            CycleAndAssertPage(7, "Research Status Safety Diagnostics");
            CycleAndAssertPage(8, "Research Verification Boundary Diagnostics");
            CycleAndAssertPage(9, "Research Verification Safety Diagnostics");
            CycleAndAssertPage(1, "Runtime Summary");
        }

        [Test]
        public void HiddenDiagnostics_F3OnlyChangesHiddenPageStateAndDoesNotChangePlayerFacingText()
        {
            string before = RefreshText();

            _overlay.CycleFullDiagnosticsPage();
            string after = RefreshText();

            Assert.That(_overlay.DiagnosticsVisible, Is.False);
            Assert.That(_overlay.FullDiagnosticsPageNumber, Is.EqualTo(2));
            Assert.That(after, Is.EqualTo(before));
            Assert.That(after, Does.Contain("Player view: diagnostics hidden."));
            Assert.That(after, Does.Not.Contain("Diagnostics: Run Diagnostics Page 2/9"));
        }

        [Test]
        public void FullDiagnostics_PagesIncludeTheirLinesAndExcludeOtherPageLines()
        {
            ShowDiagnosticsFromPlayerFacingDefault();
            AssertPageLines("build-line", "run-line");

            _overlay.CycleFullDiagnosticsPage();
            AssertPageLines("run-breakdown-line", "heat-line");
            Assert.That(_overlay.overlayText.text, Does.Contain("run-feedback-line"));
            Assert.That(_overlay.overlayText.text, Does.Not.Contain("run-demand-budget-line"));
            _overlay.ScrollFullDiagnosticsLines(100);
            Assert.That(RefreshText(), Does.Contain("run-demand-budget-line"));

            _overlay.CycleFullDiagnosticsPage();
            AssertPageLines("current-heat-tier-line", "build-line");
            AssertLinesAppearInOrder(_overlay.overlayText.text, "run-heat-cooling-line", "run-heat-delta-line");
            _overlay.ScrollFullDiagnosticsLines(100);
            Assert.That(RefreshText(), Does.Contain("run-heat-application-line"));

            _overlay.CycleFullDiagnosticsPage();
            AssertPageLines("Structure Sim", "run-line");
            Assert.That(_overlay.overlayText.text, Does.Contain(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(_overlay.overlayText.text, Does.Not.Contain("research-pending-line"));

            _overlay.CycleFullDiagnosticsPage();
            AssertPageLines("research-pending-line", "Structure Sim");
            string researchText = CollectAllResearchDiagnosticsText();
            AssertLinesAppearInOrder(
                researchText,
                "research-pending-line",
                "research-pending-validation-line",
                "research-progress-line",
                "research-progress-state-line",
                "research-completion-eligibility-line",
                "research-completion-pending-apply-line",
                "research-completion-claim-readiness-line",
                "completed-research-state-line",
                "research-completion-claim-apply-line");

            Assert.That(researchText, Does.Not.Contain("research-status-presentation-line"));
            _overlay.CycleFullDiagnosticsPage();
            AssertPageLines("research-status-presentation-line", "research-pending-line");
            Assert.That(_overlay.overlayText.text, Does.Not.Contain("research-status-safety-line"));
            Assert.That(_overlay.overlayText.text, Does.Not.Contain("research-verification-boundary-line"));
            Assert.That(_overlay.overlayText.text, Does.Not.Contain("research-verification-safety-line"));
            Assert.That(_overlay.FullDiagnosticsScrollOffset, Is.Zero);
            _overlay.ScrollFullDiagnosticsLines(100);
            Assert.That(_overlay.FullDiagnosticsScrollOffset, Is.Zero);

            _overlay.CycleFullDiagnosticsPage();
            AssertPageLines("research-status-safety-line", "research-status-presentation-line");
            Assert.That(_overlay.overlayText.text, Does.Not.Contain("research-verification-boundary-line"));
            Assert.That(_overlay.overlayText.text, Does.Not.Contain("research-verification-safety-line"));
            Assert.That(_overlay.FullDiagnosticsScrollOffset, Is.Zero);
            _overlay.ScrollFullDiagnosticsLines(100);
            Assert.That(_overlay.FullDiagnosticsScrollOffset, Is.Zero);

            _overlay.CycleFullDiagnosticsPage();
            AssertPageLines("research-verification-boundary-line", "research-status-presentation-line");
            Assert.That(_overlay.overlayText.text, Does.Not.Contain("research-verification-safety-line"));
            Assert.That(_overlay.overlayText.text, Does.Not.Contain("research-status-safety-line"));
            Assert.That(_overlay.FullDiagnosticsScrollOffset, Is.Zero);
            _overlay.ScrollFullDiagnosticsLines(100);
            Assert.That(_overlay.FullDiagnosticsScrollOffset, Is.Zero);

            _overlay.CycleFullDiagnosticsPage();
            AssertPageLines("research-verification-safety-line", "research-verification-boundary-line");
            Assert.That(_overlay.overlayText.text, Does.Not.Contain("research-status-safety-line"));
            Assert.That(_overlay.FullDiagnosticsScrollOffset, Is.Zero);
            _overlay.ScrollFullDiagnosticsLines(100);
            Assert.That(_overlay.FullDiagnosticsScrollOffset, Is.Zero);
        }

        [Test]
        public void RunDiagnosticsFocus_IncludesExpectedLinesAndDoesNotDependOnFullPageSelection()
        {
            _overlay.ToggleRunDiagnosticsFocus();
            string focusedFromRuntimePage = RefreshText();

            Assert.That(focusedFromRuntimePage, Does.StartWith("Diagnostics: Run Diagnostics Focus"));
            Assert.That(_overlay.PlayerFacingPanelsVisible, Is.False);
            Assert.That(focusedFromRuntimePage, Does.Not.Contain("MVP Loop Summary"));
            Assert.That(focusedFromRuntimePage, Does.Not.Contain("Guided MVP Action"));
            Assert.That(focusedFromRuntimePage, Does.Not.Contain("First-session loop complete:"));
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.False);
            Assert.That(focusedFromRuntimePage, Does.Not.Contain("Minimal MVP Actions"));
            Assert.That(focusedFromRuntimePage, Does.Not.Contain("Place or modify selected placement"));
            Assert.That(focusedFromRuntimePage, Does.Not.Contain("Observe Dungeon"));
            Assert.That(focusedFromRuntimePage, Does.Contain("run-line"));
            Assert.That(focusedFromRuntimePage, Does.Contain("run-history-line"));
            Assert.That(focusedFromRuntimePage, Does.Contain("run-loot-line"));
            Assert.That(focusedFromRuntimePage, Does.Contain("run-survival-line"));
            Assert.That(focusedFromRuntimePage, Does.Contain("run-extraction-line"));
            AssertLinesAppearInOrder(
                focusedFromRuntimePage,
                "run-heat-cooling-line",
                "run-heat-delta-line",
                "run-heat-application-line",
                "run-attraction-line",
                "run-forecast-line",
                "run-demand-budget-line");
            Assert.That(focusedFromRuntimePage, Does.Not.Contain("build-line"));

            _overlay.CycleFullDiagnosticsPage();
            _overlay.CycleFullDiagnosticsPage();
            Assert.That(RefreshText(), Is.EqualTo(focusedFromRuntimePage));

            _overlay.ToggleRunDiagnosticsFocus();
            Assert.That(_overlay.PlayerFacingPanelsVisible, Is.True);
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
        }

        [Test]
        public void ResearchDiagnostics_ScrollOffsetClampsAndRevealsBottomLines()
        {
            ShowDiagnosticsFromPlayerFacingDefault();
            CycleToResearchDiagnostics();

            Assert.That(_overlay.FullDiagnosticsScrollOffset, Is.Zero);
            Assert.That(RefreshText(), Does.Contain("research-pending-line"));
            Assert.That(_overlay.overlayText.text, Does.Not.Contain("research-completion-claim-readiness-line"));

            _overlay.ScrollFullDiagnosticsLines(100);

            Assert.That(_overlay.FullDiagnosticsScrollOffset, Is.EqualTo(5));
            Assert.That(RefreshText(), Does.Contain("research-completion-claim-readiness-line"));
            Assert.That(_overlay.overlayText.text, Does.Contain("completed-research-state-line"));

            _overlay.ScrollFullDiagnosticsLines(-100);

            Assert.That(_overlay.FullDiagnosticsScrollOffset, Is.Zero);
            Assert.That(RefreshText(), Does.Contain("research-pending-line"));
        }


        [Test]
        public void PageDownWhileDiagnosticsVisibleScrollsDiagnosticsOnlyAndKeepsPlayerFacingAtTop()
        {
            MakePlayerFacingSmokeTextScrollable("bootstrap_overlay_diagnostics_page_down_smoke_scroll_test.json");
            Assert.That(_overlay.PlayerFacingScrollOffset, Is.Zero);
            ShowDiagnosticsFromPlayerFacingDefault();
            CycleToResearchDiagnostics();
            int diagnosticsPage = _overlay.FullDiagnosticsPageNumber;

            _overlay.ScrollPlayerFacingTextLines(VisiblePlayerFacingScrollPageSizeForTest());
            _overlay.ScrollFullDiagnosticsLines(VisibleScrollPageSizeForTest());

            Assert.That(_overlay.FullDiagnosticsPageNumber, Is.EqualTo(diagnosticsPage));
            Assert.That(_overlay.FullDiagnosticsScrollOffset, Is.EqualTo(VisibleScrollPageSizeForTest()));
            Assert.That(_overlay.PlayerFacingScrollOffset, Is.Zero);

            _overlay.ToggleDiagnosticsVisibility();
            string playerFacingText = RefreshText();
            Assert.That(playerFacingText, Does.StartWith("Dungeon Command (MVP Loop Summary)"));
            Assert.That(playerFacingText, Does.Contain("== Analysis and Next Action =="));
        }

        [Test]
        public void PageDownWhileRunDiagnosticsFocusVisibleDoesNotScrollHiddenPlayerFacingText()
        {
            MakePlayerFacingSmokeTextScrollable("bootstrap_overlay_run_focus_page_down_smoke_scroll_test.json");
            Assert.That(_overlay.PlayerFacingScrollOffset, Is.Zero);

            _overlay.ToggleRunDiagnosticsFocus();
            _overlay.ScrollPlayerFacingTextLines(VisiblePlayerFacingScrollPageSizeForTest());

            Assert.That(_overlay.PlayerFacingScrollOffset, Is.Zero);
        }

        [Test]
        public void PageAndFocusChanges_ResetDiagnosticsScrollOffset()
        {
            ShowDiagnosticsFromPlayerFacingDefault();
            CycleToResearchDiagnostics();
            _overlay.ScrollFullDiagnosticsLines(100);
            Assert.That(_overlay.FullDiagnosticsScrollOffset, Is.EqualTo(5));

            _overlay.CycleFullDiagnosticsPage();
            Assert.That(_overlay.FullDiagnosticsScrollOffset, Is.Zero);

            CycleToResearchDiagnostics();
            _overlay.ScrollFullDiagnosticsLines(100);
            Assert.That(_overlay.FullDiagnosticsScrollOffset, Is.EqualTo(5));

            _overlay.ToggleRunDiagnosticsFocus();
            Assert.That(_overlay.FullDiagnosticsScrollOffset, Is.Zero);
        }

        [Test]
        public void ScrollFullDiagnosticsLines_DoesNotMutateSaveHeatRunHistoryOrStructureRuntime()
        {
            SaveData save = _root.Save;
            string before = JsonUtility.ToJson(save);

            ShowDiagnosticsFromPlayerFacingDefault();
            CycleToResearchDiagnostics();
            _overlay.ScrollFullDiagnosticsLines(100);
            _overlay.RefreshOverlayText();
            _overlay.ScrollFullDiagnosticsLines(-100);
            _overlay.RefreshOverlayText();

            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
            Assert.That(save.structureRuntime.Heat, Is.EqualTo(17d));
            Assert.That(save.structureRuntime.ManaReserve, Is.EqualTo(9d));
            Assert.That(save.runHistory.RecentOutcomes[0].RunId, Is.EqualTo("run-history-entry"));
        }

        [Test]
        public void CycleFullDiagnosticsPage_DoesNotMutateSaveHeatRunHistoryOrStructureRuntime()
        {
            SaveData save = _root.Save;
            StructureRuntimeState runtime = save.structureRuntime;
            RunHistoryState history = save.runHistory;
            RunOutcomeRecord[] outcomes = history.RecentOutcomes;

            for (int i = 0; i < 9; i++)
            {
                _overlay.CycleFullDiagnosticsPage();
                _overlay.RefreshOverlayText();
            }

            Assert.That(_root.Save, Is.SameAs(save));
            Assert.That(save.structureRuntime, Is.SameAs(runtime));
            Assert.That(runtime.Heat, Is.EqualTo(17d));
            Assert.That(runtime.ManaReserve, Is.EqualTo(9d));
            Assert.That(runtime.IsHeatCrisisActive, Is.True);
            Assert.That(save.runHistory, Is.SameAs(history));
            Assert.That(history.NextRunSequence, Is.EqualTo(3));
            Assert.That(history.RecentOutcomes, Is.SameAs(outcomes));
            Assert.That(history.RecentOutcomes[0].RunId, Is.EqualTo("run-history-entry"));
            Assert.That(save.researchPending.ProjectId, Is.EqualTo("research.project.panel"));
            Assert.That(save.researchProgress.ProgressUnits, Is.EqualTo(1d));
            Assert.That(save.completedResearch.ProjectIds[0], Is.EqualTo("research.project.done"));
            Assert.That(save.lastOfflineSummary.OfflineSecondsObserved, Is.EqualTo(12));
            Assert.That(save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
        }


        [Test]
        public void ToggleDevPanel_PreservesF1DevPanelVisibilityToggleState()
        {
            Assert.That(_overlay.DevPanelVisible, Is.False);

            _overlay.ToggleDevPanel();
            Assert.That(_overlay.DevPanelVisible, Is.True);

            _overlay.ToggleDevPanel();
            Assert.That(_overlay.DevPanelVisible, Is.False);
        }

        [Test]
        public void ViewOnlyRefreshFocusPagingAndScroll_DoNotMutateSaveState()
        {
            SaveData save = _root.Save;
            string before = JsonUtility.ToJson(save);

            _overlay.SelectMvpStructure(StructureSimulationPass.HeatScrubberBasicId);
            Assert.That(_overlay.GetSelectedMvpStructurePreviewText(), Is.EqualTo("Role: lowers heat pressure."));
            _overlay.RefreshOverlayText();
            _overlay.ToggleDiagnosticsVisibility();
            _overlay.RefreshOverlayText();
            _overlay.CycleFullDiagnosticsPage();
            _overlay.ScrollFullDiagnosticsLines(1);
            _overlay.ToggleRunDiagnosticsFocus();
            _overlay.RefreshOverlayText();
            _overlay.ToggleRunDiagnosticsFocus();
            _overlay.ToggleDiagnosticsVisibility();
            _overlay.CycleFullDiagnosticsPage();
            _overlay.ScrollFullDiagnosticsLines(VisibleScrollPageSizeForTest());
            _overlay.ScrollFullDiagnosticsLines(-VisibleScrollPageSizeForTest());
            _overlay.RefreshOverlayText();

            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
        }

        [Test]
        public void CompactDefaultPlayerFacingPageUpPageDownLeavesScrollAtTopWhenNoOverflow()
        {
            string firstPage = RefreshText();
            int diagnosticsPage = _overlay.FullDiagnosticsPageNumber;

            _overlay.ScrollPlayerFacingTextLines(VisiblePlayerFacingScrollPageSizeForTest());
            string scrolled = RefreshText();

            Assert.That(_overlay.PlayerFacingScrollOffset, Is.Zero);
            Assert.That(_overlay.FullDiagnosticsPageNumber, Is.EqualTo(diagnosticsPage));
            Assert.That(scrolled, Is.EqualTo(firstPage));

            _overlay.ScrollPlayerFacingTextLines(-VisiblePlayerFacingScrollPageSizeForTest());
            Assert.That(_overlay.PlayerFacingScrollOffset, Is.Zero);
        }

        [Test]
        public void CompactDefaultPlayerFacingHomeEndKeepsScrollAtTopWhenNoOverflow()
        {
            string firstPage = RefreshText();

            _overlay.JumpPlayerFacingTextToBottom();
            Assert.That(_overlay.PlayerFacingScrollOffset, Is.Zero);
            Assert.That(RefreshText(), Is.EqualTo(firstPage));

            _overlay.JumpPlayerFacingTextToTop();
            Assert.That(_overlay.PlayerFacingScrollOffset, Is.Zero);
        }

        [Test]
        public void OverflowPlayerFacingSection_PageDownEndAndHomeScrollWithinSection()
        {
            MakePlayerFacingSmokeTextScrollable("bootstrap_overlay_overflow_scroll_test.json");
            _overlay.CyclePlayerFacingSmokeSection();
            _overlay.CyclePlayerFacingSmokeSection();
            _overlay.CyclePlayerFacingSmokeSection();
            string firstPage = RefreshText();

            _overlay.ScrollPlayerFacingTextLines(VisiblePlayerFacingScrollPageSizeForTest());
            string scrolled = RefreshText();

            Assert.That(_overlay.PlayerFacingScrollOffset, Is.GreaterThan(0));
            Assert.That(scrolled, Is.Not.EqualTo(firstPage));

            _overlay.JumpPlayerFacingTextToBottom();
            Assert.That(_overlay.PlayerFacingScrollOffset, Is.GreaterThan(0));

            _overlay.JumpPlayerFacingTextToTop();
            Assert.That(_overlay.PlayerFacingScrollOffset, Is.Zero);
        }

        [Test]
        public void F4CompactSmokeViewToggleShowsCompactSmokeFacts()
        {
            Assert.That(_overlay.CompactSmokeViewEnabled, Is.False);

            _overlay.ToggleCompactSmokeView();
            string text = RefreshText();

            Assert.That(_overlay.CompactSmokeViewEnabled, Is.True);
            Assert.That(text, Does.Contain("Smoke section: Compact Smoke View"));
            Assert.That(text, Does.Contain("Dungeon composition: Mana Generator"));
            Assert.That(text, Does.Contain("Latest Adventurer Visit"));
            Assert.That(text, Does.Contain("Mana reserve:"));
            Assert.That(text, Does.Contain("Loot:"));
            Assert.That(text, Does.Contain("Heat:"));
            Assert.That(text, Does.Contain("Research:"));
            Assert.That(text, Does.Contain("Adventurers:"));
            Assert.That(text, Does.Contain("Role: adds room space and path context."));
            Assert.That(text, Does.Contain("Plan: Mana Generator + Balanced adventurer challenge."));
            Assert.That(text, Does.Contain("Path complete:"));

            _overlay.ToggleCompactSmokeView();
            Assert.That(_overlay.CompactSmokeViewEnabled, Is.False);
        }

        [Test]
        public void BuildCompactSmokeText_PreservesCompactSmokeComposition()
        {
            string compact = _overlay.BuildCompactSmokeText();

            Assert.That(compact, Does.Contain("Smoke section: Compact Smoke View"));
            Assert.That(compact, Does.Contain("Dungeon composition: Mana Generator"));
            Assert.That(compact, Does.Contain("Mana reserve:"));
            Assert.That(compact, Does.Contain("Loot:"));
            Assert.That(compact, Does.Contain("Heat:"));
            Assert.That(compact, Does.Contain("Research:"));
            Assert.That(compact, Does.Contain("Adventurers:"));
            Assert.That(compact, Does.Contain("Role: adds room space and path context."));
            Assert.That(compact, Does.Contain("Plan: Mana Generator + Balanced adventurer challenge."));
            Assert.That(compact, Does.Contain("Path complete:"));
            AssertNoUnintendedTrailingNewline(compact);
            AssertNoDuplicateBlankLineInflation(compact);
            AssertNoRawPlayerFacingSmokeIds(compact);
        }

        [Test]
        public void CompactSmokeViewIncludesLatestRunFeedbackAndOutcomeCueWhenPresent()
        {
            SetSave(new SaveData
            {
                dungeonLayout = DungeonLayoutState.CreateEmpty(1, 1),
                structureRuntime = new StructureRuntimeState { Heat = 49d, ManaReserve = 6d },
                runHistory = new RunHistoryState()
            });
            SetBackingField("_runSimulationService", BuildRunSimulationServiceForActionTest());
            SetBackingField("<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "bootstrap_overlay_compact_feedback_test.json", useAtomicWrites = false }));
            _overlay.RunOrObserveDungeon();

            _overlay.ToggleCompactSmokeView();
            string text = RefreshText();

            Assert.That(text, Does.Contain(_overlay.MvpRunResultFeedback));
            Assert.That(text, Does.Contain("Outcome cue: the adventurer visit failed, so reduce pressure before the next challenge."));
        }

        [Test]
        public void F5CyclesPlayerFacingSectionsWithoutChangingDiagnosticsPages()
        {
            int diagnosticsPage = _overlay.FullDiagnosticsPageNumber;
            Assert.That(_overlay.PlayerFacingSectionNumber, Is.EqualTo(1));

            _overlay.CyclePlayerFacingSmokeSection();
            string loop = RefreshText();
            Assert.That(_overlay.PlayerFacingSectionNumber, Is.EqualTo(2));
            Assert.That(loop, Does.Contain("Smoke section: Loop summary"));
            Assert.That(_overlay.FullDiagnosticsPageNumber, Is.EqualTo(diagnosticsPage));

            _overlay.CyclePlayerFacingSmokeSection();
            string plan = RefreshText();
            Assert.That(_overlay.PlayerFacingSectionNumber, Is.EqualTo(3));
            Assert.That(plan, Does.Contain("Smoke section: Plan and action"));
            Assert.That(plan, Does.Contain("Role: adds room space and path context."));
            Assert.That(plan, Does.Contain("Plan: Mana Generator + Balanced adventurer challenge."));
            Assert.That(plan, Does.Contain("Expected tradeoff: standard loot and heat pressure."));
            Assert.That(_overlay.FullDiagnosticsPageNumber, Is.EqualTo(diagnosticsPage));

            _overlay.CyclePlayerFacingSmokeSection();
            string feedback = RefreshText();
            Assert.That(_overlay.PlayerFacingSectionNumber, Is.EqualTo(4));
            Assert.That(feedback, Does.Contain("Smoke section: Latest adventurer visit feedback"));
            Assert.That(_overlay.FullDiagnosticsPageNumber, Is.EqualTo(diagnosticsPage));

            _overlay.CyclePlayerFacingSmokeSection();
            Assert.That(_overlay.PlayerFacingSectionNumber, Is.EqualTo(1));
            Assert.That(_overlay.FullDiagnosticsPageNumber, Is.EqualTo(diagnosticsPage));
        }

        [Test]
        public void F6BuildsAndCopiesFullSmokeTextIncludingOutcomeCueWhenPresent()
        {
            SetSave(new SaveData
            {
                dungeonLayout = DungeonLayoutState.CreateEmpty(1, 1),
                structureRuntime = new StructureRuntimeState { Heat = 49d, ManaReserve = 6d },
                runHistory = new RunHistoryState()
            });
            SetBackingField("_runSimulationService", BuildRunSimulationServiceForActionTest());
            SetBackingField("<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "bootstrap_overlay_copy_feedback_test.json", useAtomicWrites = false }));
            _overlay.PlaceSelectedMvpStructure();
            _overlay.RunOrObserveDungeon();
            _overlay.CyclePlayerFacingSmokeSection();

            string copied = _overlay.CopyFullSmokeTextToClipboard();
            string visible = RefreshText();

            Assert.That(copied, Does.Contain("MVP Loop Summary"));
            Assert.That(copied, Does.Contain("Expected next adventurer intent:"));
            Assert.That(copied, Does.Contain("Intent scores:"));
            Assert.That(copied, Does.Contain("Dungeon composition: Room: Basic Room"));
            Assert.That(copied, Does.Not.Contain("Dungeon layout:"));
            Assert.That(copied, Does.Contain("Selected room capacity: Monsters 0/1; Traps 0/1; Loot 0/1"));
            Assert.That(copied, Does.Contain("Room slot layout:"));
            Assert.That(copied, Does.Contain("Effects: none yet"));
            Assert.That(copied, Does.Contain("First-session"));
            Assert.That(copied, Does.Contain("First Dungeon Contract"));
            Assert.That(copied, Does.Contain("Path built:"));
            Assert.That(copied, Does.Contain("Visit observed:"));
            Assert.That(copied, Does.Contain("Loot recovered:"));
            Assert.That(copied, Does.Contain("Heat target:"));
            Assert.That(copied, Does.Contain("Analysis:"));
            Assert.That(copied, Does.Contain("Contract status:"));
            Assert.That(copied, Does.Contain(_overlay.MvpRunResultFeedback));
            Assert.That(copied, Does.Contain("Outcome cue: the adventurer visit failed, so reduce pressure before the next challenge."));
            AssertNoRawPlayerFacingSmokeIds(copied);
            Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo(copied));
            Assert.That(visible, Does.Contain("Smoke text copied."));
        }

        [Test]
        public void CopyFullSmokeTextToClipboard_PreservesFullSmokeComposition()
        {
            string built = _overlay.BuildFullPlayerFacingSmokeText();
            string copied = _overlay.CopyFullSmokeTextToClipboard();

            Assert.That(copied, Is.EqualTo(built));
            Assert.That(copied, Does.Contain("MVP Loop Summary"));
            Assert.That(copied, Does.Contain("Guided MVP Action"));
            Assert.That(copied, Does.Contain("Role: adds room space and path context."));
            Assert.That(copied, Does.Contain("Plan: Mana Generator + Balanced adventurer challenge."));
            Assert.That(copied, Does.Contain("Expected tradeoff: standard loot and heat pressure."));
            Assert.That(copied, Does.Contain("Expected next adventurer intent:"));
            Assert.That(copied, Does.Contain("Intent scores:"));
            Assert.That(copied, Does.Contain("Dungeon composition: Mana Generator"));
            Assert.That(copied, Does.Contain("Player view: diagnostics hidden."));
            Assert.That(copied, Does.Contain("banner-line"));
            Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo(copied));
            AssertNoUnintendedTrailingNewline(copied);
            AssertNoDuplicateBlankLineInflation(copied);
            AssertNoRawPlayerFacingSmokeIds(copied);
        }

        [Test]
        public void SectionedPlayerFacingSmokeText_DoesNotInflateBlankLines()
        {
            AssertNoDuplicateBlankLineInflation(_overlay.BuildCurrentPlayerFacingSmokeText());

            _overlay.CyclePlayerFacingSmokeSection();
            AssertNoDuplicateBlankLineInflation(_overlay.BuildCurrentPlayerFacingSmokeText());

            _overlay.CyclePlayerFacingSmokeSection();
            AssertNoDuplicateBlankLineInflation(_overlay.BuildCurrentPlayerFacingSmokeText());

            _overlay.CyclePlayerFacingSmokeSection();
            AssertNoDuplicateBlankLineInflation(_overlay.BuildCurrentPlayerFacingSmokeText());
        }

        [Test]
        public void F7CollapsesAndExpandsMinimalMvpActionsPanel()
        {
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
            Assert.That(_overlay.MinimalMvpActionPanelCollapsed, Is.False);

            _overlay.ToggleMinimalMvpActionPanelCollapsed();
            Assert.That(_overlay.MinimalMvpActionPanelCollapsed, Is.True);
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.False);

            _overlay.ToggleMinimalMvpActionPanelCollapsed();
            Assert.That(_overlay.MinimalMvpActionPanelCollapsed, Is.False);
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
        }

        [Test]
        public void CollapsedActionPanelDoesNotHideOrAlterPlayerFacingSmokeText()
        {
            string before = RefreshText();

            _overlay.ToggleMinimalMvpActionPanelCollapsed();
            string after = RefreshText();

            Assert.That(after, Is.EqualTo(before));
            Assert.That(after, Does.Contain("MVP Loop Summary"));
            Assert.That(after, Does.Contain("Player view: diagnostics hidden."));
        }

        [Test]
        public void VisibleAndCopiedPlayerFacingSmokeTextDoesNotExposeRawIdsOrLocalizationKeys()
        {
            SetSave(new SaveData
            {
                dungeonLayout = DungeonLayoutState.CreateEmpty(1, 1),
                structureRuntime = new StructureRuntimeState { Heat = 49d, ManaReserve = 6d },
                runHistory = new RunHistoryState()
            });
            SetBackingField("_runSimulationService", BuildRunSimulationServiceForActionTest());
            SetBackingField("<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "bootstrap_overlay_no_raw_ids_test.json", useAtomicWrites = false }));
            _overlay.PlaceSelectedMvpStructure();
            _overlay.RunOrObserveDungeon();

            string visible = RefreshText();
            string copied = _overlay.CopyFullSmokeTextToClipboard();

            Assert.That(visible, Does.Contain("== Build Choice =="));
            Assert.That(copied, Does.Not.Contain("Dungeon layout:"));
            Assert.That(copied, Does.Contain("Effects: none yet"));
            AssertNoRawPlayerFacingSmokeIds(visible);
            AssertNoRawPlayerFacingSmokeIds(copied);
        }

        [Test]
        public void F6FullSmoke_PrimaryDungeonViewUsesRoomSlotsForRoomTwoLoot()
        {
            SetSave(new SaveData
            {
                dungeonLayout = DungeonLayoutState.CreateEmpty(1, 1),
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 9d },
                runHistory = new RunHistoryState(),
                mvpDungeonPlacements = new MvpDungeonPlacementState
                {
                    Entries = new List<MvpDungeonPlacementEntry>
                    {
                        new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId, 1),
                        new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId, 2)
                    },
                    NextRevision = 3
                },
                mvpSelectedRoomSlotIndex = 1,
                mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection
                {
                    Rooms = new List<MvpRoomSlotAssignmentState>
                    {
                        new MvpRoomSlotAssignmentState
                        {
                            FloorIndex = 0,
                            RoomIndex = 0,
                            RoomOptionId = MvpDungeonPlacementIds.NarrowHallOptionId,
                            MonsterOptionIds = new[] { MvpDungeonPlacementIds.GoblinOptionId },
                            TrapOptionIds = new[] { MvpDungeonPlacementIds.SnareTrapOptionId }
                        },
                        new MvpRoomSlotAssignmentState
                        {
                            FloorIndex = 0,
                            RoomIndex = 1,
                            RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                            LootNodeOptionIds = new[] { MvpDungeonPlacementIds.BasicLootNodeOptionId }
                        }
                    }
                }
            });
            SetBackingField("_runSimulationService", BuildRunSimulationServiceForActionTest());
            SetBackingField("<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "bootstrap_overlay_f6_room_slot_consistency_test.json", useAtomicWrites = false }));
            SetOverlayBackingField("_selectedMvpPlacementCategoryId", MvpDungeonPlacementIds.LootNodeCategoryId);
            _overlay.RunOrObserveDungeon();

            string copied = _overlay.CopyFullSmokeTextToClipboard();

            Assert.That(copied, Does.Contain("Dungeon composition: Room: Narrow Hall; Monster: Goblin; Trap: Snare Trap; Loot node: Basic Loot Node"));
            Assert.That(copied, Does.Not.Contain("Dungeon layout:"));
            Assert.That(copied, Does.Contain("Selected room target: Room 2: Basic Room"));
            Assert.That(copied, Does.Contain("Selected room capacity: Monsters 0/1; Traps 0/1; Loot 1/1"));
            Assert.That(copied, Does.Contain("Selected placement fit: Loot node fits Room 2."));
            Assert.That(copied, Does.Contain("Room slot layout: Floor 0: Room 1: Narrow Hall (Monsters: Goblin 1/1; Traps: Snare Trap 1/1; Loot: unavailable 0/0) | Room 2: Basic Room (Monsters: empty 0/1; Traps: empty 0/1; Loot: Basic Loot Node 1/1)"));
            Assert.That(copied, Does.Not.Contain("Room: Narrow Hall -> Monster: Goblin -> Trap: Snare Trap -> Loot node: Basic Loot Node"));
            Assert.That(copied, Does.Contain("Expected next adventurer intent"));
            Assert.That(copied, Does.Contain("Latest visit intent"));
            Assert.That(copied, Does.Contain("Challenge posture used"));
            Assert.That(copied, Does.Contain("Debug selected posture"));
            Assert.That(copied, Does.Contain("traffic pressure intent input"));
            AssertNoRawPlayerFacingSmokeIds(copied);
        }

        private static void AssertNoRawPlayerFacingSmokeIds(string text)
        {
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.HeatScrubberBasicId));
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.RiskLabBasicId));
            Assert.That(text, Does.Not.Contain("run.posture"));
            Assert.That(text, Does.Not.Contain("placement.category"));
            Assert.That(text, Does.Not.Contain("placement.option"));
            Assert.That(text, Does.Not.Contain("adventurer.class."));
            Assert.That(text, Does.Not.Contain("heat_tier."));
            Assert.That(text, Does.Not.Contain("mvp_loop.suggestion"));
            Assert.That(text, Does.Not.Contain("structure."));
            Assert.That(text, Does.Not.Contain("run-"));
            Assert.That(text, Does.Not.Contain("run.heat_delta.rule.test"));
            Assert.That(text, Does.Not.Contain("ui.mvp_"));
        }

        private static void AssertNoUnintendedTrailingNewline(string text)
        {
            Assert.That(text, Does.Not.EndWith("\n"));
        }

        private static void AssertNoDuplicateBlankLineInflation(string text)
        {
            Assert.That(text, Does.Not.Contain("\n\n\n"));
        }


        private void MakePlayerFacingSmokeTextScrollable(string saveFileName)
        {
            _overlay.PlaceSelectedMvpStructure();
            SetBackingField("_runSimulationService", BuildRunSimulationServiceForActionTest());
            SetBackingField("<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = saveFileName, useAtomicWrites = false }));
            _overlay.RunOrObserveDungeon();
            var feedback = new System.Text.StringBuilder(_overlay.MvpRunResultFeedback);
            for (int i = 0; i < VisiblePlayerFacingScrollPageSizeForTest() + 4; i++)
            {
                feedback.Append('\n').Append("Extra smoke line ").Append(i + 1).Append('.');
            }
            SetOverlayBackingField("_mvpRunResultFeedback", feedback.ToString());
        }

        private static int VisiblePlayerFacingScrollPageSizeForTest() => 28;


        private static MvpRoomSlotCapacityConfig[] BuildRoomSlotCapacities()
        {
            return new[]
            {
                new MvpRoomSlotCapacityConfig
                {
                    RoomOptionId = MvpDungeonPlacementIds.NarrowHallOptionId,
                    MonsterCapacity = 1,
                    TrapCapacity = 1,
                    LootCapacity = 0
                },
                new MvpRoomSlotCapacityConfig
                {
                    RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                    MonsterCapacity = 1,
                    TrapCapacity = 1,
                    LootCapacity = 1
                }
            };
        }

        private static int VisibleScrollPageSizeForTest() => 4;

        private static RunSimulationService BuildRunSimulationServiceForActionTest()
        {
            return new RunSimulationService(new RunSimulationConfig
            {
                BaseSuccessChance = 0.6d,
                HeatPenaltyPerPoint = 0.004d,
                ManaReserveBonusPerPoint = 0.01d,
                CrisisFailurePenalty = 0.3d,
                SuccessThreshold = 0.5d,
                RunPostures = new[]
                {
                    new RunPostureConfig { Id = RunPostureResolver.CautiousId, DisplayNameKey = "run.posture.cautious.name", GeneratedLootWorldValueMultiplier = 0.8d, ExtractedLootWorldValueMultiplier = 0.8d, HeatDeltaOffset = -1d },
                    new RunPostureConfig { Id = RunPostureResolver.BalancedId, DisplayNameKey = "run.posture.balanced.name", GeneratedLootWorldValueMultiplier = 1d, ExtractedLootWorldValueMultiplier = 1d, HeatDeltaOffset = 0d },
                    new RunPostureConfig { Id = RunPostureResolver.GreedyId, DisplayNameKey = "run.posture.greedy.name", GeneratedLootWorldValueMultiplier = 1.25d, ExtractedLootWorldValueMultiplier = 1.25d, HeatDeltaOffset = 1d }
                },
                BaseScoreOnSuccess = 100,
                ScorePerManaPoint = 2,
                MaxRunHistoryEntries = 10,
                HighHeatFeedbackThreshold = 75d,
                LowManaFeedbackThreshold = 5d,
                StrongManaReserveFeedbackThreshold = 50d,
                MinPartySize = 3,
                MaxPartySize = 5,
                MaxAllowedPartySize = 100,
                SuccessSurvivorRatio = 1d,
                FailureSurvivorRatio = 0d,
                LootExtractionRoundingPolicyId = "loot_extraction.round_floor",
                LootExtractionRuleSourceId = "run.loot_extraction.rule.test",
                LootHeatCoolingRuleSourceId = "run.loot_heat_cooling.rule.test",
                AdventurerAttractionRuleSourceId = "run.adventurer_attraction.rule.test",
                AdventurerInterestForecastRuleSourceId = "run.adventurer_interest_forecast.rule.test",
                AdventurerDemandBudgetRuleSourceId = "run.adventurer_demand_budget.rule.test",
                RunHeatDeltaRuleSourceId = "run.heat_delta.rule.test",
                HeatPeaceMinimum = 0d,
                HeatPeaceMaximum = 9d,
                HeatNoticeMinimum = 10d,
                HeatNoticeMaximum = 24d,
                HeatConcernMinimum = 25d,
                HeatConcernMaximum = 49d,
                RunHeatApplicationRuleSourceId = "run.heat_application.rule.test",
                AdventurerPartyCompositionRuleSourceId = "run.adventurer_party_composition.rule.test",
                AdventurerIntentRuleSourceId = "run.adventurer_intent.rule.test",
                IntentGreedyScorePerLoot = 2d,
                IntentGreedyScorePerAttraction = 1.5d,
                IntentGreedyPenaltyPerHeatTierRank = 3d,
                IntentGreedyPenaltyPerRecentDeath = 4d,
                IntentGreedyPenaltyPerDanger = 0.75d,
                IntentCautiousScorePerDanger = 1.5d,
                IntentCautiousScorePerHeatPressure = 2d,
                IntentCautiousScorePerHeatTierRank = 3d,
                IntentCautiousScorePerRecentDeath = 4d,
                IntentCautiousReductionPerPathCapacity = 0.75d,
                IntentBalancedBaseScore = 7d,
                IntentBalancedPenaltyPerExtremeScoreDelta = 0.2d,
                IntentModerateRiskTarget = 4d,
                IntentModerateRewardTarget = 4d,
                IntentBalancedPenaltyPerModerateDistance = 0.6d,
                IntentMinimumScore = 0d,
                IntentMaximumScore = 20d,
                AdventurerPartyCompositionMinSize = 1,
                AdventurerPartyCompositionMaxSize = 3,
                AdventurerPartyCompositionMaxAllowedSize = 5,
                AdventurerPartyCompositionClassIds = new[]
                {
                    AdventurerPartyCompositionResolver.WarriorClassId,
                    AdventurerPartyCompositionResolver.RogueClassId,
                    AdventurerPartyCompositionResolver.MageClassId,
                    AdventurerPartyCompositionResolver.ClericClassId,
                    AdventurerPartyCompositionResolver.RangerClassId
                },
                MvpRoomSlotCapacities = BuildRoomSlotCapacities(),
                MvpFirstSessionObjective = new MvpFirstSessionObjectiveConfig
                {
                    ObjectiveId = "objective.first_dungeon_contract",
                    RequiredCompletePath = true,
                    RequiredRunCount = 1,
                    RequiredRecoveredLootValue = 10,
                    AllowedMaxHeatTierId = CurrentHeatTierResolver.PeaceTierId,
                    RequireResearchAnalysisUnlocked = true,
                    AnalysisResearchProjectId = "research.project.m7_a2_scaffold"
                }
            });
        }

        [Test]
        public void Header_MissingLocalization_UsesLocalizationKeyFallbacksSafely()
        {
            SetContent(BuildContent(includeDiagnosticsLocalization: false));
            ShowDiagnosticsFromPlayerFacingDefault();

            string text = RefreshText();

            Assert.That(text, Does.Contain("ui.dev.diagnostics.header_format"));
            Assert.That(text, Does.Contain("ui.dev.hint.cycle_diagnostics_page"));
            Assert.That(text, Does.Contain("ui.dev.hint.scroll_diagnostics"));

            _overlay.ToggleRunDiagnosticsFocus();
            Assert.That(RefreshText(), Does.Contain("ui.dev.diagnostics.focus.run_diagnostics"));
        }

        private void ShowDiagnosticsFromPlayerFacingDefault()
        {
            if (!_overlay.DiagnosticsVisible)
            {
                _overlay.ToggleDiagnosticsVisibility();
            }
        }

        private void AssertPage(int number, string name)
        {
            Assert.That(_overlay.FullDiagnosticsPageNumber, Is.EqualTo(number));
            Assert.That(RefreshText(), Does.Contain($"Diagnostics: {name} Page {number}/9"));
        }

        private void CycleAndAssertPage(int number, string name)
        {
            _overlay.CycleFullDiagnosticsPage();
            AssertPage(number, name);
        }

        private void AssertPageLines(string included, string excluded)
        {
            string text = RefreshText();
            Assert.That(text, Does.Contain(included));
            Assert.That(text, Does.Not.Contain(excluded));
        }

        private static void AssertLinesAppearInOrder(string text, params string[] lines)
        {
            int previousIndex = -1;
            foreach (string line in lines)
            {
                int index = text.IndexOf(line, System.StringComparison.Ordinal);
                Assert.That(index, Is.GreaterThan(previousIndex), $"Expected '{line}' after the preceding diagnostic line.");
                previousIndex = index;
            }
        }

        private static void AssertNoPlayerFacingRawIds(string text)
        {
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.HeatScrubberBasicId));
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.RiskLabBasicId));
            Assert.That(text, Does.Not.Contain("placement.category"));
            Assert.That(text, Does.Not.Contain("placement.option"));
            Assert.That(text, Does.Not.Contain("run-"));
            Assert.That(text, Does.Not.Contain("run.loot_extraction.rule.test"));
            Assert.That(text, Does.Not.Contain("run.loot_heat_cooling.rule.test"));
            Assert.That(text, Does.Not.Contain("run.adventurer_attraction.rule.test"));
            Assert.That(text, Does.Not.Contain("run.adventurer_interest_forecast.rule.test"));
            Assert.That(text, Does.Not.Contain("run.adventurer_demand_budget.rule.test"));
            Assert.That(text, Does.Not.Contain("run.heat_delta.rule.test"));
            Assert.That(text, Does.Not.Contain("run.heat_application.rule.test"));
        }

        private void CycleToResearchDiagnostics()
        {
            while (_overlay.FullDiagnosticsPageNumber != 5)
            {
                _overlay.CycleFullDiagnosticsPage();
            }
        }

        private string CollectAllResearchDiagnosticsText()
        {
            CycleToResearchDiagnostics();
            while (_overlay.FullDiagnosticsScrollOffset > 0)
            {
                _overlay.ScrollFullDiagnosticsLines(-1);
            }
            var windows = new List<string>();
            while (true)
            {
                windows.Add(RefreshText());
                int previousOffset = _overlay.FullDiagnosticsScrollOffset;
                _overlay.ScrollFullDiagnosticsLines(1);
                if (_overlay.FullDiagnosticsScrollOffset == previousOffset)
                {
                    return string.Join("\n", windows);
                }
            }
        }

        private static ContentService BuildContentWithResearchScaffold(string projectId)
        {
            ContentService content = BuildContent(includeDiagnosticsLocalization: true);
            typeof(ContentService).GetField("<Bootstrap>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(content, new ContentBootstrap
                {
                    researchCompletionEligibilityScaffold = new ResearchCompletionEligibilityScaffoldConfig
                    {
                        enabled = true,
                        ruleSourceId = "research.completion_eligibility.rule.test",
                        projectId = projectId,
                        requiredProgressUnits = 2d
                    },
                    researchVerificationScaffold = new ResearchVerificationScaffoldConfig
                    {
                        enabled = true,
                        ruleSourceId = "research.verification.rule.test",
                        verificationMode = ResearchVerificationBoundaryResolver.LocalDevPlaceholderVerificationMode
                    }
                });
            return content;
        }

        private string RefreshText()
        {
            _overlay.RefreshOverlayText();
            return _overlay.overlayText.text;
        }

        private void SetLine(string fieldName, string value) => SetBackingField(fieldName, value);
        private void SetSave(SaveData save) => SetBackingField("<Save>k__BackingField", save);
        private void SetContent(ContentService content) => SetBackingField("<Content>k__BackingField", content);
        private void SetBackingField(string fieldName, object value) => typeof(GameRoot).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_root, value);
        private void SetOverlayBackingField(string fieldName, object value) => typeof(BootstrapOverlay).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_overlay, value);

        private static ContentService BuildContent(bool includeDiagnosticsLocalization)
        {
            var content = new ContentService();
            var map = (Dictionary<string, string>)typeof(ContentService).GetField("_stringMap", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(content);
            map["ui.dev.structure_status"] = "Structure Sim — Slot F{0} S{1}, Structure: {2}, Heat Crisis: {3}";
            map["ui.banner.place_success"] = "Placed: {0}";
            map["ui.banner.run_simulated"] = "Run simulated.";
            map["ui.banner.run_sim_failed"] = "Run simulation failed.";
            map["structure.mana_generator.basic.display_name"] = "Mana Generator";
            map["structure.heat_scrubber.basic.display_name"] = "Heat Scrubber";
            map["structure.risk_lab.basic.display_name"] = "Risk Lab";
            map["ui.mvp_label.structure.unknown"] = "Unknown structure";
            map["ui.mvp_loop.panel.title"] = "MVP Loop Summary";
            map["ui.mvp_loop.section.adventurer_intent"] = "Expected next adventurer intent";
            map["ui.adventurer_intent.summary_format"] = "Expected next adventurer intent: {0} likely. Reason: {1}";
            map["ui.adventurer_intent.score_summary_format"] = "Intent scores: Cautious {0:0.#}, Balanced {1:0.#}, Greedy {2:0.#}";
            map["ui.adventurer_intent.debug_posture_format"] = "Expected next adventurer intent: {0} likely. Debug selected posture: {1}.";
            map["ui.adventurer_intent.run_posture_used_format"] = "Latest visit intent: {0}. Challenge posture used: {1}. Debug selected posture: {2}.";
            map["ui.adventurer_intent.fallback_run_posture_used_format"] = "Latest visit intent unavailable. Challenge posture used debug fallback: {0}. Debug selected posture: {1}.";
            map["ui.adventurer_intent.smoke_evidence_format"] = "Latest visit intent: {0}; challenge posture used: {1}; debug selected posture: {2}; rule source: {3}; error code: {4}; fallback used: {5}.";
            map["ui.adventurer_intent.unavailable"] = "Unavailable";
            map["ui.adventurer_intent.reason.loot_high_heat_low"] = "loot signal is high and heat is low";
            map["ui.adventurer_intent.reason.deaths_heat"] = "recent deaths and rising heat";
            map["ui.adventurer_intent.reason.moderate"] = "risk and reward are both moderate";
            map["ui.adventurer_intent.reason.danger"] = "danger pressure is high";
            map["ui.adventurer_intent.reason.fallback"] = "current dungeon signals are still forming";
            map["ui.adventurer_intent.body_format"] = "{0} likely. Reason: {1}";
            map["ui.mvp_loop.section.adventurer_pressure"] = "Adventurer pressure";
            map["ui.adventurer_pressure.summary_format"] = "Adventurer pressure: {0}. Reason: {1}.";
            map["ui.adventurer_pressure.body_format"] = "{0}. Reason: {1}.";
            map["ui.adventurer_pressure.detail_format"] = "Adventurer pressure detail: score {0:0.##}; band {1}; rule source {2}; error {3}; loot {4}; attraction {5}; danger {6}; heat pressure {7}; recent deaths {8}; recovered loot {9}; path complete {10}; latest visit {11}.";
            map["ui.adventurer_pressure.band.not_yet"] = "not yet";
            map["ui.adventurer_pressure.band.low"] = "low";
            map["ui.adventurer_pressure.band.cautious_interest"] = "cautious interest";
            map["ui.adventurer_pressure.band.building_slowly"] = "building slowly";
            map["ui.adventurer_pressure.band.likely_soon"] = "likely soon";
            map["ui.adventurer_pressure.reason.high_loot_low_heat"] = "high loot signal and low heat";
            map["ui.adventurer_pressure.reason.modest_loot_low_attraction"] = "modest loot and low attraction";
            map["ui.adventurer_pressure.reason.deaths_heat"] = "recent deaths and rising heat";
            map["ui.adventurer_pressure.reason.incomplete_path_weak_loot"] = "incomplete path or weak loot signal";
            map["ui.adventurer_pressure.reason.not_yet"] = "current dungeon signals are still forming";
            map["ui.adventurer_pressure.outcome.none"] = "none yet";
            map["ui.adventurer_pressure.outcome.success"] = "success";
            map["ui.adventurer_pressure.outcome.failure"] = "failure";
            map["ui.adventurer_traffic.summary_format"] = "Adventurer traffic: {0}. Estimated active delves: {1}. Reason: {2}.";
            map["ui.adventurer_traffic.detail_format"] = "Adventurer traffic detail: score {0:0.##}; band {1}; estimated active delves {2}; estimated delve band {3}; arrival pressure {4}; traffic pressure intent input {5}; rule source {6}; error {7}; loot {8}; attraction {9}; danger {10}; heat pressure {11}; recent deaths {12}; recovered loot {13}; path complete {14}.";
            map["ui.adventurer_traffic.band.none"] = "none";
            map["ui.adventurer_traffic.band.low"] = "low";
            map["ui.adventurer_traffic.band.building"] = "building";
            map["ui.adventurer_traffic.band.steady"] = "steady";
            map["ui.adventurer_traffic.band.heavy"] = "heavy";
            map["ui.adventurer_traffic.band.dangerous_churn"] = "dangerous churn";
            map["ui.adventurer_traffic.party_band.none"] = "none";
            map["ui.adventurer_traffic.party_band.low"] = "low";
            map["ui.adventurer_traffic.party_band.medium"] = "medium";
            map["ui.adventurer_traffic.party_band.high"] = "high";
            map["ui.adventurer_traffic.reason.incomplete_path_weak_loot"] = "weak loot signal and incomplete path";
            map["ui.adventurer_traffic.reason.high_loot_low_heat"] = "high loot signal and low heat are attracting attention";
            map["ui.adventurer_traffic.reason.balanced"] = "loot and danger are balanced enough to keep parties coming";
            map["ui.adventurer_traffic.reason.deaths_caution"] = "parties are interested, but recent deaths are raising caution";
            map["ui.adventurer_traffic.reason.strong_loot_attraction"] = "strong loot and high attraction are pulling in multiple delves";
            map["ui.adventurer_traffic.reason.fallback"] = "current dungeon signals are still forming";
            map["ui.mvp_screen.title"] = "Dungeon Command (MVP Loop Summary)";
            map["ui.mvp_screen.section.top_status"] = "Top Status";
            map["ui.mvp_screen.section.current_dungeon"] = "Current Dungeon";
            map["ui.mvp_screen.section.build_choice"] = "Build Choice";
            map["ui.mvp_screen.section.run_setup"] = "Activity Setup";
            map["ui.mvp_screen.section.latest_run"] = "Latest Adventurer Visit";
            map["ui.mvp_screen.section.analysis_next_action"] = "Analysis and Next Action";
            map["ui.mvp_screen.section.first_contract"] = "First Dungeon Contract";
            map["ui.mvp_first_contract.title"] = "First Dungeon Contract";
            map["ui.mvp_first_contract.path_built_format"] = "Path built: {0}";
            map["ui.mvp_first_contract.run_observed_format"] = "Visit observed: {0}";
            map["ui.mvp_first_contract.loot_recovered_format"] = "Loot recovered: {0} / {1}";
            map["ui.mvp_first_contract.heat_target_format"] = "Heat target: {0} (current: {1})";
            map["ui.mvp_first_contract.analysis_format"] = "Analysis: {0}";
            map["ui.mvp_first_contract.status_format"] = "Contract status: {0}";
            map["ui.mvp_first_contract.value.complete"] = "complete";
            map["ui.mvp_first_contract.value.incomplete"] = "incomplete";
            map["ui.mvp_first_contract.value.analysis_unlocked"] = "Adventurer Activity Analysis unlocked";
            map["ui.mvp_first_contract.value.analysis_locked"] = "unlock Adventurer Activity Analysis";
            map["ui.mvp_first_contract.status.in_progress"] = "In progress";
            map["ui.mvp_first_contract.status.complete"] = "Complete. Try a riskier setup or improve loot recovery.";
            map["ui.mvp_first_contract.status.unavailable"] = "Unavailable until objective config is fixed";
            map["ui.mvp_first_contract.value.unavailable"] = "unavailable";
            map["ui.mvp_first_contract.compact.unavailable_format"] = "{0}: {1}.";
            map["ui.mvp_first_contract.compact.in_progress_format"] = "{0}: {1}. Loot {2} / {3}, {4}.";
            map["ui.mvp_first_contract.compact.complete_format"] = "{0}: {1}";
            map["ui.mvp_first_contract.compact.path_complete"] = "path complete";
            map["ui.mvp_first_contract.compact.path_incomplete"] = "path incomplete";
            map["ui.mvp_screen.section.header_format"] = "== {0} ==";
            map["ui.mvp_screen.selected_category_format"] = "Selected category: {0}";
            map["ui.mvp_screen.selected_option_format"] = "Selected option: {0}";
            map["ui.mvp_screen.selected_placement_format"] = "Selected placement: {0} / {1}";
            map["ui.mvp_screen.run_posture_format"] = "Debug selected posture: {0}";
            map["ui.mvp_screen.prompt.place_or_modify"] = "Next build step: choose an option, then place or modify it.";
            map["ui.mvp_screen.prompt.run_or_observe"] = "Next activity step: observe the dungeon when ready.";
            map["ui.mvp_screen.feedback.no_placement"] = "No build change yet this session.";
            map["ui.mvp_screen.feedback.no_run"] = "No adventurer visit observed yet this session.";
            map["ui.mvp_screen.comparison.none"] = "Comparison: choose the other option in this category to compare tradeoffs.";
            map["ui.mvp_screen.analysis.no_run"] = "Why it happened: observe adventurer activity to see the first result.";
            map["ui.mvp_screen.party.unavailable"] = "Party: no adventurers observed yet.";
            map["ui.mvp_screen.party.format"] = "Party: {0}";
            map["ui.mvp_screen.research_format"] = "Research: {0}";
            map["ui.mvp_screen.path_complete_format"] = "Path complete: {0}";
            map["ui.mvp_screen.analysis.format"] = "Why it happened: {0}";
            map["ui.mvp_loop.panel.placement_format"] = "Placement: {0}";
            map["ui.mvp_loop.panel.composition_format"] = "Dungeon composition: {0}";
            map["ui.mvp_loop.panel.latest_run_format"] = "Latest adventurer visit: {0}";
            map["ui.mvp_loop.panel.mana_format"] = "Mana reserve: {0:0.##}";
            map["ui.mvp_loop.panel.loot_format"] = "Loot: {1}/{0} recovered; {2} tradeable.";
            map["ui.mvp_loop.panel.heat_format"] = "Heat: {0:0.##} -> {1:0.##} ({2}). {3}";
            map["ui.mvp_loop.panel.research_format"] = "{0}";
            map["ui.mvp_loop.panel.research_unlock_format"] = "Unlocked: {0}";
            map["ui.mvp_loop.panel.adventurer_party_format"] = "{0}";
            map["ui.mvp_loop.panel.suggestion_format"] = "{0}";
            map["ui.mvp_loop.value.no_placement"] = "No dungeon placements yet";
            map["ui.mvp_loop.value.no_run"] = "No adventurer visit yet";
            map["ui.mvp_loop.value.unknown"] = "Unknown";
            map["ui.mvp_loop.value.no_research"] = "No research";
            map["ui.mvp_loop.run_status.succeeded"] = "Succeeded";
            map["ui.mvp_loop.run_status.failed"] = "Failed";
            map["ui.mvp_loop.section.current_dungeon"] = "Current Dungeon";
            map["ui.mvp_loop.section.latest_run"] = "Latest Adventurer Visit";
            map["ui.mvp_loop.section.why_it_happened"] = "Why It Happened";
            map["ui.mvp_loop.section.rewards_and_risk"] = "Rewards and Risk";
            map["ui.mvp_loop.section.research"] = "Research";
            map["ui.mvp_loop.section.suggested_next_action"] = "Suggested Next Action";
            map["ui.mvp_loop.section.line_format"] = "{0}: {1}";
            map["ui.mvp_loop.inline_separator"] = " | ";
            map["ui.mvp_loop.panel.run_outcome_line_format"] = "{0}. Party: {1}";
            map["ui.mvp_loop.panel.casualty_format"] = "Survivors: {0}/{1}; deaths: {2}";
            map["ui.mvp_loop.why.no_run"] = "No adventurer visit yet. Build or review the dungeon, then observe adventurer activity to learn what happens.";
            map["ui.mvp_loop.why.run_format"] = "Main reason: {0}.";
            map["ui.mvp_loop.why.path_capacity"] = "path capacity shaped the adventurer visit";
            map["ui.mvp_loop.why.danger"] = "danger pressure drove the result";
            map["ui.mvp_loop.why.mana_pressure"] = "mana pressure constrained the adventurer visit";
            map["ui.mvp_loop.why.heat_pressure"] = "heat pressure raised the risk";
            map["ui.mvp_loop.why.loot_bonus"] = "loot placement improved the reward";
            map["ui.mvp_loop.why.attraction"] = "attraction changed adventurer interest";
            map["ui.mvp_loop.why.mixed"] = "the current placement mix shaped the result";
            map["ui.mvp_loop.analysis.run_format"] = "Main reason: {0}. Analysis: {1}";
            map["ui.mvp_loop.analysis.no_run"] = "Adventurer Activity Analysis is ready. Observe adventurer activity to unlock analysis from the latest visit.";
            map["ui.mvp_loop.analysis.danger"] = "Danger was the largest pressure on this adventurer visit.";
            map["ui.mvp_loop.analysis.heat_increased"] = "Heat rose during this adventurer visit, increasing future risk.";
            map["ui.mvp_loop.analysis.partial_loot"] = "Loot was generated but not fully recovered.";
            map["ui.mvp_loop.analysis.strong_loot_stable_heat"] = "Loot recovery worked while heat stayed controlled.";
            map["ui.mvp_loop.analysis.mixed"] = "The resolved adventurer visit data does not point to a single extra pressure.";
            map["ui.mvp_loop.risk.no_run"] = "Risk will be shown after an adventurer visit.";
            map["ui.mvp_loop.risk.stable"] = "Risk stayed steady.";
            map["ui.mvp_loop.risk.increased"] = "Risk increased.";
            map["ui.mvp_loop.risk.reduced"] = "Risk went down.";
            map["ui.research.status.verification_required"] = "Verification required";
            map["ui.research.status.completed"] = "Research completed";
            map["ui.research.status.blocked_or_invalid"] = "Research unavailable";
            map["ui.research_unlock.none"] = "No research unlock yet";
            map["ui.research_unlock.unavailable"] = "Research unlock unavailable";
            map["ui.research_unlock.basic_run_analysis.summary"] = "Adventurer activity analysis unlocked";
            map["mvp_loop.suggestion.run_dungeon"] = "Observe adventurer activity to see the first outcome.";
            map["mvp_loop.suggestion.reduce_heat_pressure"] = "Reduce heat pressure before pushing further.";
            map["ui.mvp_placement_comparison.compared_with_format"] = "Compared with {0}: {1}";
            map["ui.mvp_placement_comparison.room.basic_to_narrow_hall"] = "lower path capacity, better as a connector.";
            map["ui.mvp_placement_comparison.room.narrow_hall_to_basic"] = "higher path capacity, better as a main room.";
            map["ui.mvp_placement_comparison.monster.skeleton_to_goblin"] = "less danger and mana pressure, adds a small loot signal.";
            map["ui.mvp_placement_comparison.monster.goblin_to_skeleton"] = "more danger and mana pressure, removes the loot signal.";
            map["ui.mvp_placement_comparison.trap.spike_to_snare"] = "less danger and no heat pressure.";
            map["ui.mvp_placement_comparison.trap.snare_to_spike"] = "more danger and adds heat pressure.";
            map["ui.mvp_placement_comparison.loot_node.basic_to_hidden_cache"] = "subtler loot and lower attraction.";
            map["ui.mvp_placement_comparison.loot_node.hidden_cache_to_basic"] = "stronger loot and higher attraction.";
            map["mvp_loop.suggestion.improve_survivability_or_layout"] = "Improve survivability or layout before the next adventurer visit.";
            map["mvp_loop.suggestion.verify_research_status"] = "Verify research status before claiming progress.";
            map["mvp_loop.suggestion.repeat_or_improve_placement"] = "Adjust placement before the next adventurer visit.";
            map["mvp_loop.suggestion.analysis.run_for_analysis"] = "Observe adventurer activity so Adventurer Activity Analysis can read the latest visit.";
            map["mvp_loop.suggestion.analysis.reduce_danger"] = "Reduce danger or use a safer posture before pushing for more loot.";
            map["mvp_loop.suggestion.analysis.reduce_heat"] = "Lower heat pressure or use a cautious posture before the next adventurer visit.";
            map["mvp_loop.suggestion.analysis.improve_extraction"] = "Improve survivability or reduce danger so generated loot is recovered.";
            map["mvp_loop.suggestion.analysis.test_greedier"] = "Repeat this setup or test slightly greedier pressure while heat is controlled.";
            AddGd10PlacementEffectsLocalization(map);
            map["mvp_loop.suggestion.repeat_or_improve_placement"] = "Adjust placement before the next adventurer visit.";
            map["mvp_loop.suggestion.analysis.run_for_analysis"] = "Observe adventurer activity so Adventurer Activity Analysis can read the latest visit.";
            map["mvp_loop.suggestion.analysis.reduce_danger"] = "Reduce danger or use a safer posture before pushing for more loot.";
            map["mvp_loop.suggestion.analysis.reduce_heat"] = "Lower heat pressure or use a cautious posture before the next adventurer visit.";
            map["mvp_loop.suggestion.analysis.improve_extraction"] = "Improve survivability or reduce danger so generated loot is recovered.";
            map["mvp_loop.suggestion.analysis.test_greedier"] = "Repeat this setup or test slightly greedier pressure while heat is controlled.";
            map["ui.guided_mvp.panel.title"] = "Guided MVP Action";
            map["ui.mvp_action.panel.title"] = "Minimal MVP Actions";
            map["ui.mvp_action.button.place_or_modify"] = "Place or modify selected placement";
            map["ui.mvp_action.button.run_or_observe"] = "Observe Dungeon";
            map["ui.mvp_action.button.show_diagnostics"] = "Show diagnostics";
            map["ui.mvp_action.button.hide_diagnostics"] = "Hide diagnostics";
            map["ui.mvp_action.selection.label"] = "Selected placement: {0}";
            map["ui.mvp_action.category.label"] = "Selected category: {0}";
            map["ui.mvp_action.group.rooms"] = "Rooms:";
            map["ui.mvp_action.group.monsters"] = "Monsters:";
            map["ui.mvp_action.group.traps"] = "Traps:";
            map["ui.mvp_action.group.loot"] = "Loot:";
            map["ui.mvp_action.posture.label"] = "Debug posture: {0}";
            map["run.posture.cautious.name"] = "Cautious";
            map["run.posture.balanced.name"] = "Balanced";
            map["run.posture.greedy.name"] = "Greedy";
            map["ui.mvp_action.selection.mana_generator"] = "Mana Generator";
            map["ui.mvp_action.selection.heat_scrubber"] = "Heat Scrubber";
            map["ui.mvp_action.selection.risk_lab"] = "Risk Lab";
            map["ui.mvp_structure_preview.mana_generator"] = "Role: improves mana reserve.";
            map["ui.mvp_structure_preview.heat_scrubber"] = "Role: lowers heat pressure.";
            map["ui.mvp_structure_preview.risk_lab"] = "Role: clarifies research risk.";
            map["ui.mvp_structure_preview.unknown"] = "Role unavailable.";
            map["placement.category.room.display_name"] = "Room";
            map["placement.category.monster.display_name"] = "Monster";
            map["placement.category.trap.display_name"] = "Trap";
            map["placement.category.loot_node.display_name"] = "Loot node";
            map["placement.option.room.basic.display_name"] = "Basic Room";
            map["placement.option.room.narrow_hall.display_name"] = "Narrow Hall";
            map["placement.option.monster.skeleton.display_name"] = "Skeleton";
            map["placement.option.monster.goblin.display_name"] = "Goblin";
            map["placement.option.trap.spike.display_name"] = "Spike Trap";
            map["placement.option.trap.snare.display_name"] = "Snare Trap";
            map["placement.option.loot_node.basic.display_name"] = "Basic Loot Node";
            map["placement.option.loot_node.hidden_cache.display_name"] = "Hidden Cache";
            map["ui.mvp_label.placement_category.unknown"] = "Unknown category";
            map["ui.mvp_label.placement_option.unknown"] = "Unknown placement";
            map["ui.mvp_composition.empty"] = "No dungeon placements yet";
            map["ui.mvp_composition.entry_format"] = "{0}: {1}";
            map["ui.mvp_composition.separator"] = "; ";
            map["ui.mvp_dungeon_layout.panel.layout_format"] = "Dungeon layout: {0}";
            map["ui.mvp_dungeon_layout.panel.floor_format"] = "Floor {0}: {1}";
            map["ui.mvp_dungeon_layout.panel.assigned_node_format"] = "{0}: {1}";
            map["ui.mvp_dungeon_layout.panel.empty_node_format"] = "{0}: {1}";
            map["ui.mvp_dungeon_layout.panel.node_separator"] = " -> ";
            map["ui.mvp_dungeon_layout.value.empty_available"] = "Empty / available";
            map["ui.mvp_room_slots.panel.layout_format"] = "Room slot layout: {0}";
            map["ui.mvp_room_slots.selected_target_format"] = "Selected room target: Room {0}: {1}";
            map["ui.mvp_room_slots.selected_capacity_format"] = "Selected room capacity: {0}";
            map["ui.mvp_room_slots.selected_capacity_category_format"] = "{0} {1}/{2}";
            map["ui.mvp_room_slots.selected_capacity_unavailable_category_format"] = "{0} unavailable {1}/{2}";
            map["ui.mvp_room_slots.selected_capacity_separator"] = "; ";
            map["ui.mvp_room_slots.selected_placement_fit_format"] = "Selected placement fit: {0}";
            map["ui.mvp_room_slots.selected_placement_fits_format"] = "{0} fits Room {1}.";
            map["ui.mvp_room_slots.selected_placement_cannot_fit_no_slot_format"] = "{0} cannot fit Room {1} because this room has no {2}.";
            map["ui.mvp_room_slots.capacity.monsters"] = "Monsters";
            map["ui.mvp_room_slots.capacity.traps"] = "Traps";
            map["ui.mvp_room_slots.capacity.loot"] = "Loot";
            map["ui.mvp_room_slots.reason.monster_slot"] = "monster slot";
            map["ui.mvp_room_slots.reason.trap_slot"] = "trap slot";
            map["ui.mvp_room_slots.reason.loot_slot"] = "loot slot";
            map["ui.mvp_room_slots.no_valid_slot_format"] = "No valid {0} slot in Room {1}: {2}.";
            map["ui.mvp_room_slots.cycle_target_button"] = "Cycle room target";
            map["ui.mvp_room_slots.add_basic_room_slot_button"] = "Add Basic Room slot";
            map["ui.mvp_room_slots.add_basic_room_slot_success"] = "Added Room 2: Basic Room.";
            map["ui.mvp_room_slots.add_basic_room_slot_already_exists"] = "Room 2 already exists.";
            map["ui.mvp_room_slots.panel.floor_format"] = "Floor {0}: {1}";
            map["ui.mvp_room_slots.panel.room_format"] = "Room {0}: {1} ({2}; {3}; {4})";
            map["ui.mvp_room_slots.panel.monsters_format"] = "Monsters: {0} {1}/{2}";
            map["ui.mvp_room_slots.panel.traps_format"] = "Traps: {0} {1}/{2}";
            map["ui.mvp_room_slots.panel.loot_format"] = "Loot: {0} {1}/{2}";
            map["ui.mvp_room_slots.panel.empty"] = "empty";
            map["ui.mvp_room_slots.panel.unavailable"] = "unavailable";
            map["ui.mvp_room_slots.panel.assignment_separator"] = ", ";
            map["ui.mvp_room_slots.panel.room_separator"] = " | ";
            map["ui.mvp_placement_preview.room.basic"] = "Role: adds room space and path context.";
            map["ui.mvp_placement_preview.room.narrow_hall"] = "Role: connects rooms with a lower-capacity hallway.";
            map["ui.mvp_placement_preview.monster.skeleton"] = "Role: adds danger and mana pressure.";
            map["ui.mvp_placement_preview.monster.goblin"] = "Role: adds lighter danger, lower mana pressure, and a small loot signal.";
            map["ui.mvp_placement_preview.trap.spike"] = "Role: adds danger, heat, and path pressure.";
            map["ui.mvp_placement_preview.trap.snare"] = "Role: controls adventurers with lower danger and less heat pressure.";
            map["ui.mvp_placement_preview.loot_node.basic"] = "Role: adds loot and adventurer attraction context.";
            map["ui.mvp_placement_preview.loot_node.hidden_cache"] = "Role: adds subtler loot with lower adventurer attraction.";
            map["ui.mvp_placement_preview.unknown"] = "Role unavailable.";
            map["ui.mvp_placement_feedback.changed_format"] = "Changed placement: {0} -> {1}: {2}. {3}";
            map["ui.mvp_run_plan_preview.plan_format"] = "Plan: {0} + {1} adventurer challenge.";
            map["ui.mvp_run_plan_preview.tradeoff_format"] = "Expected tradeoff: {0}";
            map["ui.mvp_run_plan_preview.combined_format"] = "{0}\n{1}";
            map["ui.mvp_run_plan_preview.tradeoff.cautious"] = "lower loot, safer heat pressure.";
            map["ui.mvp_run_plan_preview.tradeoff.balanced"] = "standard loot and heat pressure.";
            map["ui.mvp_run_plan_preview.tradeoff.greedy"] = "higher loot, higher heat pressure.";
            map["ui.mvp_run_plan_preview.tradeoff.unknown"] = "activity tradeoff unavailable.";
            map["ui.mvp_structure_feedback.empty_slot"] = "Empty slot";
            map["ui.mvp_structure_feedback.changed_format"] = "Changed: {0} -> {1}. {2}";
            map["ui.mvp_run_feedback.success_stable_heat"] = "Adventurer visit result: succeeded. Loot extracted, heat stable.";
            map["ui.mvp_run_feedback.success_heat_reduced"] = "Adventurer visit result: succeeded. Loot extracted, heat reduced.";
            map["ui.mvp_run_feedback.success_heat_increased"] = "Adventurer visit result: succeeded. Loot extracted, heat increased.";
            map["ui.mvp_run_feedback.failed"] = "Adventurer visit result: failed. Review placement before the next challenge.";
            map["ui.mvp_run_feedback.unavailable"] = "Adventurer visit result unavailable.";
            map["ui.mvp_run_feedback.outcome_cue.failed"] = "Outcome cue: the adventurer visit failed, so reduce pressure before the next challenge.";
            map["ui.mvp_run_feedback.outcome_cue.heat_increased"] = "Outcome cue: heat pressure rose; consider a safer posture or heat control.";
            map["ui.mvp_run_feedback.outcome_cue.controlled_loot"] = "Outcome cue: loot landed while heat stayed controlled.";
            map["ui.mvp_run_feedback.outcome_cue.format"] = "{0} {1}";
            map["ui.mvp_run_feedback.format"] = "{0} Mana {1:0.##}. Loot {2}/{3}/{4}. Heat {5:0.##}->{6:0.##}.";
            map["ui.mvp_run_feedback.format_with_party"] = "{0} Mana {1:0.##}. Loot {2}/{3}/{4}. Heat {5:0.##}->{6:0.##}. {7}";
            map["ui.mvp_run_feedback.posture_format"] = "Challenge posture used: {0}. {1}";
            map["ui.mvp_adventurer_party.preview_format"] = "Adventurers: {0}";
            map["ui.mvp_adventurer_party.class.unknown"] = "Unknown adventurer";
            map["adventurer.class.warrior.display_name"] = "Warrior";
            map["adventurer.class.rogue.display_name"] = "Rogue";
            map["adventurer.class.mage.display_name"] = "Mage";
            map["adventurer.class.cleric.display_name"] = "Cleric";
            map["adventurer.class.ranger.display_name"] = "Ranger";
            map["ui.mvp_action.panel.compact_format"] = "{0}: {1} {2} [{3}] [{4}]";
            map["ui.mvp_view.player_mode.status"] = "Player view: diagnostics hidden.";
            map["ui.mvp_view.diagnostics_mode.status"] = "Diagnostics visible.";
            map["ui.mvp_smoke.section.status_format"] = "Smoke section: {0} ({1}/{2})";
            map["ui.mvp_smoke.section.full"] = "Full player-facing text";
            map["ui.mvp_smoke.section.loop_summary"] = "Loop summary";
            map["ui.mvp_smoke.section.plan_and_action"] = "Plan and action";
            map["ui.mvp_smoke.section.latest_run_feedback"] = "Latest adventurer visit feedback";
            map["ui.mvp_smoke.section.compact"] = "Compact Smoke View";
            map["ui.mvp_smoke.adventurers_unavailable"] = "Adventurers: unavailable";
            map["ui.mvp_smoke.copy.confirmation"] = "Smoke text copied.";
            map["ui.mvp_action.button.collapse_panel"] = "Collapse actions (F7)";
            map["ui.mvp_action.button.expand_panel"] = "Expand actions (F7)";
            map["ui.first_session.status.not_started"] = "First-session status: waiting for MVP loop summary.";
            map["ui.first_session.status.place_structure"] = "First-session status: place one room, monster, trap, or loot node to start the loop.";
            map["ui.first_session.status.run_dungeon"] = "First-session status: dungeon placement ready; observe adventurer activity next.";
            map["ui.first_session.status.observe_summary"] = "First-session status: observe mana, loot, heat, research, and next action.";
            map["ui.first_session.status.complete"] = "First-session loop complete: placement, adventurer activity, mana, loot, heat, and research are visible.";
            map["ui.guided_mvp.panel.step_format"] = "Step: {0}";
            map["ui.guided_mvp.panel.status_format"] = "Status: {0}";
            map["ui.guided_mvp.panel.next_action_format"] = "Next action: {0}";
            map["ui.guided_mvp.panel.complete_format"] = "Path complete: {0}";
            map["ui.guided_mvp.value.complete_yes"] = "Yes";
            map["ui.guided_mvp.value.complete_no"] = "No";
            map["guided_mvp.step.place_or_modify_structure"] = "Place or modify one dungeon placement";
            map["guided_mvp.step.run_or_observe"] = "Observe adventurer activity";
            map["guided_mvp.step.reduce_heat_pressure"] = "Reduce heat pressure";
            map["guided_mvp.step.improve_survivability_or_layout"] = "Improve survivability or layout";
            map["guided_mvp.step.verify_research_status"] = "Verify research status";
            map["guided_mvp.step.repeat_or_improve"] = "Repeat the loop or improve placement";
            map["guided_mvp.status.missing_save"] = "Save state is not available yet.";
            map["guided_mvp.status.place_or_modify_structure"] = "No dungeon placement is visible in the current summary.";
            map["guided_mvp.status.run_or_observe"] = "A dungeon placement is ready; no adventurer activity has been observed yet.";
            map["guided_mvp.status.heat_pressure"] = "The latest summary shows heat pressure.";
            map["guided_mvp.status.poor_loot_extraction"] = "The latest adventurer visit generated loot but extracted none.";
            map["guided_mvp.status.research_completion_pending"] = "Research completion is pending verification.";
            map["guided_mvp.status.repeat_or_improve"] = "Placement, adventurer activity, mana, loot, heat, and research are visible in the summary.";
            map["guided_mvp.action.place_structure"] = "Place one room, monster, trap, or loot node.";
            map["guided_mvp.action.run_dungeon"] = "Observe adventurer activity and watch the MVP Loop Summary update.";
            map["guided_mvp.action.reduce_heat_pressure"] = "Improve placement toward lower heat pressure before pushing further.";
            map["guided_mvp.action.improve_survivability_or_layout"] = "Improve survivability or layout, then adjust before the next adventurer visit.";
            map["guided_mvp.action.verify_research_status"] = "Check the research status line before claiming progress.";
            map["guided_mvp.action.repeat_or_improve"] = "Adjust one placement before the next adventurer visit.";
            if (includeDiagnosticsLocalization)
            {
                map["ui.dev.hint.toggle_panel"] = "F1 toggles Dev Panel";
                map["ui.dev.hint.toggle_run_diagnostics"] = "F2 toggles Run Diagnostics focus";
                map["ui.dev.hint.cycle_diagnostics_page"] = "F3 cycles Diagnostics Page";
                map["ui.dev.hint.scroll_diagnostics"] = "Mouse wheel or PageUp PageDown scroll diagnostics";
                map["ui.dev.diagnostics.header_format"] = "Diagnostics: {0} Page {1}/{2}";
                map["ui.dev.diagnostics.focus.run_diagnostics"] = "Diagnostics: Run Diagnostics Focus";
                map["ui.dev.diagnostics.page.runtime_summary"] = "Runtime Summary";
                map["ui.dev.diagnostics.page.run_diagnostics"] = "Run Diagnostics";
                map["ui.dev.diagnostics.page.heat_diagnostics"] = "Heat Diagnostics";
                map["ui.dev.diagnostics.page.systems_diagnostics"] = "Systems Diagnostics";
                map["ui.dev.diagnostics.page.research_diagnostics"] = "Research Diagnostics";
                map["ui.dev.diagnostics.page.research_status_presentation_diagnostics"] = "Research Status Presentation Diagnostics";
                map["ui.dev.diagnostics.page.research_status_safety_diagnostics"] = "Research Status Safety Diagnostics";
                map["ui.dev.diagnostics.page.research_verification_boundary_diagnostics"] = "Research Verification Boundary Diagnostics";
                map["ui.dev.diagnostics.page.research_verification_safety_diagnostics"] = "Research Verification Safety Diagnostics";
            }
            return content;
        }

        private static void AddGd10PlacementEffectsLocalization(Dictionary<string, string> map)
        {
            map["ui.mvp_loop.panel.placement_effects_format"] = "Effects: {0}";
            map["ui.mvp_placement_effects.empty"] = "none yet";
            map["ui.mvp_placement_effects.combined_format"] = "{0}";
            map["ui.mvp_placement_effects.detail_separator"] = ", ";
            map["ui.mvp_placement_effects.path_capacity_format"] = "path capacity +{0}";
            map["ui.mvp_placement_effects.danger_format"] = "danger +{0}";
            map["ui.mvp_placement_effects.mana_pressure_format"] = "mana pressure +{0}";
            map["ui.mvp_placement_effects.heat_pressure_format"] = "heat pressure +{0}";
            map["ui.mvp_placement_effects.loot_bonus_format"] = "loot +{0}";
            map["ui.mvp_placement_effects.attraction_format"] = "attraction +{0}";
            map["ui.mvp_placement_effects.explanation_format"] = "{0} ({1})";
            map["ui.mvp_placement_effects.explanation.room.basic"] = "Basic Room opens the adventurer path and capacity context";
            map["ui.mvp_placement_effects.explanation.room.narrow_hall"] = "Narrow Hall connects the route with lower path capacity than a Basic Room";
            map["ui.mvp_placement_effects.explanation.monster.skeleton"] = "Skeleton adds danger and mana upkeep pressure";
            map["ui.mvp_placement_effects.explanation.monster.goblin"] = "Goblin adds lighter danger, lower mana upkeep, and a small loot signal";
            map["ui.mvp_placement_effects.explanation.trap.spike"] = "Spike Trap adds danger and heat pressure";
            map["ui.mvp_placement_effects.explanation.trap.snare"] = "Snare Trap controls the path with lower danger and less heat pressure";
            map["ui.mvp_placement_effects.explanation.loot_node.basic"] = "Basic Loot Node increases loot and adventurer attraction context";
            map["ui.mvp_placement_effects.explanation.loot_node.hidden_cache"] = "Hidden Cache adds safer loot with a subtler attraction signal";
            map["ui.mvp_run_feedback.placement_effects_impact_format"] = "{0} Placement effects: {1}.";
            map["heat_tier.peace"] = "Peace";
            map["heat_tier.notice"] = "Notice";
            map["heat_tier.concern"] = "Concern";
            map["mvp_loop.suggestion.reduce_heat_pressure"] = "Reduce heat pressure before pushing further.";
            map["ui.mvp_placement_comparison.compared_with_format"] = "Compared with {0}: {1}";
            map["ui.mvp_placement_comparison.room.basic_to_narrow_hall"] = "lower path capacity, better as a connector.";
            map["ui.mvp_placement_comparison.room.narrow_hall_to_basic"] = "higher path capacity, better as a main room.";
            map["ui.mvp_placement_comparison.monster.skeleton_to_goblin"] = "less danger and mana pressure, adds a small loot signal.";
            map["ui.mvp_placement_comparison.monster.goblin_to_skeleton"] = "more danger and mana pressure, removes the loot signal.";
            map["ui.mvp_placement_comparison.trap.spike_to_snare"] = "less danger and no heat pressure.";
            map["ui.mvp_placement_comparison.trap.snare_to_spike"] = "more danger and adds heat pressure.";
            map["ui.mvp_placement_comparison.loot_node.basic_to_hidden_cache"] = "subtler loot and lower attraction.";
            map["ui.mvp_placement_comparison.loot_node.hidden_cache_to_basic"] = "stronger loot and higher attraction.";
        }
        [Test]
        public void MinimalMvpActionPanel_SourceRendersComparisonLineInsideScrollablePanel()
        {
            string overlaySource = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/UI/BootstrapOverlay.cs"));

            int panelStart = overlaySource.IndexOf("private void DrawMinimalMvpActionPanel", System.StringComparison.Ordinal);
            int scrollStart = overlaySource.IndexOf("GUILayout.BeginScrollView", panelStart, System.StringComparison.Ordinal);
            int comparisonLine = overlaySource.IndexOf("labels.ComparisonText", panelStart, System.StringComparison.Ordinal);
            int scrollEnd = overlaySource.IndexOf("GUILayout.EndScrollView", panelStart, System.StringComparison.Ordinal);

            Assert.That(scrollStart, Is.GreaterThan(panelStart));
            Assert.That(comparisonLine, Is.GreaterThan(scrollStart));
            Assert.That(comparisonLine, Is.LessThan(scrollEnd));
        }

    }
}
