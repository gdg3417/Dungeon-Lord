using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using System.Collections.Generic;
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
            Assert.That(text, Does.Contain("Placement: Mana Generator"));
            Assert.That(text, Does.Not.Contain("Placement: structure.mana_generator.basic"));
            Assert.That(text, Does.Contain("Next action: Run again or adjust one placement based on the summary."));
            Assert.That(text, Does.Contain("First-session loop complete: placement, run, mana, loot, heat, and research are visible."));
            Assert.That(text, Does.Not.Contain("First-session status: structure placed; run the dungeon next."));
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
            Assert.That(text, Does.Not.Contain("Minimal MVP Actions: [Place or modify selected] [Run or observe dungeon]"));
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
            Assert.That(text, Does.Contain("MVP Loop Summary"));
            Assert.That(text, Does.Contain("Guided MVP Action"));
            Assert.That(text, Does.Contain("First-session loop complete: placement, run, mana, loot, heat, and research are visible."));
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
            Assert.That(text, Does.Contain("MVP Loop Summary"));
            Assert.That(text, Does.Contain("Guided MVP Action"));
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
            Assert.That(restoredText, Does.Contain("MVP Loop Summary"));
            Assert.That(restoredText, Does.Contain("First-session loop complete: placement, run, mana, loot, heat, and research are visible."));
            Assert.That(restoredText, Does.Contain("Player view: diagnostics hidden."));
            Assert.That(restoredText, Does.Not.Contain("Diagnostics: Runtime Summary Page 1/9"));
        }

        [Test]
        public void PlayerFacingMode_MinimalMvpActionLabelsIncludePlacementRunAndDiagnosticsToggleKeys()
        {
            MinimalMvpActionPanelLabels labels = MinimalMvpActionPanelPresenter.BuildLabels((key, fallback) => _root.Content.GetString(key, fallback));

            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
            Assert.That(labels.PlacementButton, Is.EqualTo("Place or modify selected"));
            Assert.That(labels.RunButton, Is.EqualTo("Run or observe dungeon"));
            Assert.That(labels.PreviewText, Is.EqualTo("Role: improves mana reserve."));
            Assert.That(labels.ShowDiagnosticsButton, Is.EqualTo("Show diagnostics"));
            Assert.That(labels.HideDiagnosticsButton, Is.EqualTo("Hide diagnostics"));
            Assert.That(_overlay.DiagnosticsVisible, Is.False);
        }

        [Test]
        public void PlayerFacingMode_MinimalMvpActionPanelKeepsAllVerticalSelectorLabelsInCompactRect()
        {
            MinimalMvpActionPanelLabels labels = MinimalMvpActionPanelPresenter.BuildLabels(
                (key, fallback) => _root.Content.GetString(key, fallback),
                _overlay.GetSelectedMvpStructureNameKey(),
                _overlay.SelectedMvpStructureId);
            Rect rect = _overlay.GetMinimalMvpActionPanelRect();

            Assert.That(rect.width, Is.EqualTo(260f));
            Assert.That(rect.height, Is.EqualTo(300f));
            Assert.That(labels.SelectedStructureLabel, Is.EqualTo("Selected structure: Mana Generator"));
            Assert.That(labels.PostureLabel, Is.EqualTo("Run posture: Balanced"));
            Assert.That(labels.PreviewText, Is.EqualTo("Role: improves mana reserve."));
            Assert.That(labels.ManaGeneratorSelection, Is.EqualTo("Mana Generator"));
            Assert.That(labels.HeatScrubberSelection, Is.EqualTo("Heat Scrubber"));
            Assert.That(labels.RiskLabSelection, Is.EqualTo("Risk Lab"));
            Assert.That(labels.CautiousPosture, Is.EqualTo("Cautious"));
            Assert.That(labels.BalancedPosture, Is.EqualTo("Balanced"));
            Assert.That(labels.GreedyPosture, Is.EqualTo("Greedy"));
            Assert.That(labels.PlacementButton, Is.EqualTo("Place or modify selected"));
            Assert.That(labels.RunButton, Is.EqualTo("Run or observe dungeon"));
            Assert.That(labels.ShowDiagnosticsButton, Is.EqualTo("Show diagnostics"));
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
            Assert.That(labels.SelectedStructureLabel, Is.EqualTo("Selected structure: Mana Generator"));
            Assert.That(labels.PreviewText, Is.EqualTo("Role: improves mana reserve."));
            Assert.That(labels.ManaGeneratorSelection, Is.EqualTo("Mana Generator"));
            Assert.That(labels.HeatScrubberSelection, Is.EqualTo("Heat Scrubber"));
            Assert.That(labels.RiskLabSelection, Is.EqualTo("Risk Lab"));
        }

        [TestCase(StructureSimulationPass.ManaGeneratorBasicId, "Mana Generator", MinimalMvpActionPanelPresenter.ManaGeneratorSelectionKey, "Role: improves mana reserve.")]
        [TestCase(StructureSimulationPass.HeatScrubberBasicId, "Heat Scrubber", MinimalMvpActionPanelPresenter.HeatScrubberSelectionKey, "Role: lowers heat pressure.")]
        [TestCase(StructureSimulationPass.RiskLabBasicId, "Risk Lab", MinimalMvpActionPanelPresenter.RiskLabSelectionKey, "Role: clarifies research risk.")]
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
            Assert.That(labels.SelectedStructureLabel, Is.EqualTo($"Selected structure: {displayName}"));
            Assert.That(labels.PreviewText, Is.EqualTo(previewText));
            Assert.That(labels.PreviewText, Does.Not.Contain(structureId));
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

            Assert.That(_root.BannerMessage, Is.EqualTo("Placed structure: Mana Generator"));
            Assert.That(placementText, Does.Contain("Placed structure: Mana Generator"));
            Assert.That(placementText, Does.Contain("MVP Loop Summary"));
            Assert.That(placementText, Does.Not.Contain("Diagnostics: Runtime Summary Page 1/9"));

            SetBackingField("_runSimulationService", BuildRunSimulationServiceForActionTest());
            SetBackingField("<SaveService>k__BackingField", new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "bootstrap_overlay_action_test.json", useAtomicWrites = false }));

            _overlay.RunOrObserveDungeon();
            string runText = RefreshText();

            string runFeedback = _overlay.MvpRunResultFeedback;
            bool hasLocalizedRunResult = runFeedback.StartsWith("Posture: Balanced. Run result: succeeded.", System.StringComparison.Ordinal) ||
                                         runFeedback.StartsWith("Posture: Balanced. Run result: failed.", System.StringComparison.Ordinal);

            Assert.That(_root.BannerMessage, Is.EqualTo("Run simulated."));
            Assert.That(runText, Does.Contain("Run simulated."));
            Assert.That(runFeedback, Is.Not.Empty);
            Assert.That(hasLocalizedRunResult, Is.True, "Fixture may validly produce success or failure feedback; both must remain localized player-facing results.");
            Assert.That(runText, Does.Contain(runFeedback));
            Assert.That(runFeedback, Does.Contain("Mana 9."));
            Assert.That(runFeedback, Does.Contain("Loot 0/0/0."));
            Assert.That(runFeedback, Does.Contain("Heat 17->17."));
            Assert.That(runFeedback, Does.Not.Contain("run-1"));
            Assert.That(runFeedback, Does.Not.Contain(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(runFeedback, Does.Not.Contain("run.heat_delta.rule.test"));
            Assert.That(runText, Does.Contain("Latest run:"));
            Assert.That(runText, Does.Not.Contain("Diagnostics: Runtime Summary Page 1/9"));
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
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
            Assert.That(defaultText, Does.Contain("MVP Loop Summary"));
            Assert.That(defaultText, Does.Contain("Guided MVP Action"));
            Assert.That(defaultText, Does.Contain("First-session status:"));
            Assert.That(defaultText, Does.Contain("Player view: diagnostics hidden."));
            Assert.That(defaultText, Does.Not.Contain("Diagnostics: Runtime Summary Page 1/9"));

            _overlay.SelectMvpStructure(StructureSimulationPass.ManaGeneratorBasicId);
            Assert.That(_overlay.GetSelectedMvpStructureDisplayName(), Is.EqualTo("Mana Generator"));
            Assert.That(_overlay.GetSelectedMvpStructurePreviewText(), Is.EqualTo("Role: improves mana reserve."));
            _overlay.PlaceSelectedMvpStructure();
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed: Empty slot -> Mana Generator. Role: improves mana reserve."));

            _overlay.SelectMvpStructure(StructureSimulationPass.HeatScrubberBasicId);
            Assert.That(_overlay.GetSelectedMvpStructureDisplayName(), Is.EqualTo("Heat Scrubber"));
            Assert.That(_overlay.GetSelectedMvpStructurePreviewText(), Is.EqualTo("Role: lowers heat pressure."));
            _overlay.PlaceSelectedMvpStructure();
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed: Mana Generator -> Heat Scrubber. Role: lowers heat pressure."));

            _overlay.SelectMvpStructure(StructureSimulationPass.RiskLabBasicId);
            Assert.That(_overlay.GetSelectedMvpStructureDisplayName(), Is.EqualTo("Risk Lab"));
            Assert.That(_overlay.GetSelectedMvpStructurePreviewText(), Is.EqualTo("Role: clarifies research risk."));
            _overlay.PlaceSelectedMvpStructure();
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed: Heat Scrubber -> Risk Lab. Role: clarifies research risk."));

            _overlay.RunOrObserveDungeon();
            string playerFacingText = RefreshText();

            Assert.That(_overlay.MvpRunResultFeedback, Is.Not.Empty);
            Assert.That(playerFacingText, Does.Contain("Changed: Heat Scrubber -> Risk Lab. Role: clarifies research risk."));
            Assert.That(playerFacingText, Does.Contain(_overlay.MvpRunResultFeedback));
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
            Assert.That(focusedText, Does.Not.Contain("Changed: Heat Scrubber -> Risk Lab"));
            Assert.That(focusedText, Does.Not.Contain(_overlay.MvpRunResultFeedback));

            _overlay.ToggleRunDiagnosticsFocus();
            string restoredText = RefreshText();

            Assert.That(_overlay.DiagnosticsVisible, Is.False);
            Assert.That(_overlay.PlayerFacingPanelsVisible, Is.True);
            Assert.That(_overlay.MinimalMvpActionGuiVisible, Is.True);
            Assert.That(restoredText, Does.Contain("MVP Loop Summary"));
            Assert.That(restoredText, Does.Contain("Player view: diagnostics hidden."));
            AssertNoPlayerFacingRawIds(restoredText);

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
        public void MinimalMvpActionPlacementBanner_UsesLocalizedStructureNameInsteadOfRawId()
        {
            DungeonLayoutState layout = DungeonLayoutState.CreateEmpty(1, 1);
            SetSave(new SaveData
            {
                dungeonLayout = layout,
                structureRuntime = new StructureRuntimeState()
            });

            _overlay.PlaceSelectedMvpStructure();

            Assert.That(_root.BannerMessage, Is.EqualTo("Placed structure: Mana Generator"));
            Assert.That(_root.BannerMessage, Does.Not.Contain(StructureSimulationPass.ManaGeneratorBasicId));
        }


        [TestCase(StructureSimulationPass.ManaGeneratorBasicId, "Mana Generator")]
        [TestCase(StructureSimulationPass.HeatScrubberBasicId, "Heat Scrubber")]
        [TestCase(StructureSimulationPass.RiskLabBasicId, "Risk Lab")]
        public void MinimalMvpActionPlacement_UsesSelectedStructurePathBannerAndLocalizedSummary(string structureId, string displayName)
        {
            DungeonLayoutState layout = DungeonLayoutState.CreateEmpty(1, 1);
            SetSave(new SaveData
            {
                dungeonLayout = layout,
                structureRuntime = new StructureRuntimeState()
            });

            _overlay.SelectMvpStructure(structureId);
            _overlay.PlaceSelectedMvpStructure();
            string text = RefreshText();

            Assert.That(_root.GetSelectedSlotStructureId(), Is.EqualTo(structureId));
            Assert.That(_root.BannerMessage, Is.EqualTo($"Placed structure: {displayName}"));
            Assert.That(text, Does.Contain($"Placement: {displayName}"));
            Assert.That(text, Does.Contain($"Placed structure: {displayName}"));
            Assert.That(text, Does.Not.Contain(structureId));
            Assert.That(_root.BannerMessage, Does.Not.Contain(structureId));
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

            _overlay.SelectMvpStructure(StructureSimulationPass.ManaGeneratorBasicId);
            _overlay.PlaceSelectedMvpStructure();
            string text = RefreshText();

            Assert.That(_root.GetSelectedSlotStructureId(), Is.EqualTo(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed: Empty slot -> Mana Generator. Role: improves mana reserve."));
            Assert.That(text, Does.Contain("Changed: Empty slot -> Mana Generator. Role: improves mana reserve."));
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.ManaGeneratorBasicId));
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

            _overlay.SelectMvpStructure(StructureSimulationPass.ManaGeneratorBasicId);
            _overlay.PlaceSelectedMvpStructure();
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed: Empty slot -> Mana Generator. Role: improves mana reserve."));

            _overlay.SelectMvpStructure(StructureSimulationPass.HeatScrubberBasicId);
            _overlay.PlaceSelectedMvpStructure();
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed: Mana Generator -> Heat Scrubber. Role: lowers heat pressure."));

            _overlay.SelectMvpStructure(StructureSimulationPass.RiskLabBasicId);
            _overlay.PlaceSelectedMvpStructure();
            string text = RefreshText();

            Assert.That(_root.GetSelectedSlotStructureId(), Is.EqualTo(StructureSimulationPass.RiskLabBasicId));
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed: Heat Scrubber -> Risk Lab. Role: clarifies research risk."));
            Assert.That(text, Does.Contain("Changed: Heat Scrubber -> Risk Lab. Role: clarifies research risk."));
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.RiskLabBasicId));
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

            _overlay.SelectMvpStructure(StructureSimulationPass.HeatScrubberBasicId);
            _overlay.PlaceSelectedMvpStructure();
            string text = RefreshText();

            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed: Empty slot -> Heat Scrubber. Role: lowers heat pressure."));
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
            _overlay.SelectMvpStructure(StructureSimulationPass.ManaGeneratorBasicId);
            _overlay.PlaceSelectedMvpStructure();

            _overlay.RunOrObserveDungeon();
            string text = RefreshText();

            Assert.That(_root.BannerMessage, Is.EqualTo("Run simulated."));
            Assert.That(_overlay.MvpStructurePlacementFeedback, Is.EqualTo("Changed: Empty slot -> Mana Generator. Role: improves mana reserve."));
            Assert.That(_overlay.MvpRunResultFeedback, Does.StartWith("Posture: Balanced. Run result: succeeded."));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Contain("Adventurers: "));
            Assert.That(_overlay.MvpRunResultFeedback, Does.Not.Contain(AdventurerPartyCompositionResolver.WarriorClassId));
            Assert.That(text, Does.Contain("Adventurers: "));
            Assert.That(text, Does.Contain("Changed: Empty slot -> Mana Generator. Role: improves mana reserve."));
            Assert.That(text, Does.Contain(_overlay.MvpRunResultFeedback));
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
            Assert.That(text, Does.Contain(_overlay.MvpRunResultFeedback));
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

            Assert.That(restoredText, Does.Contain(feedback));
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

            Assert.That(focusedText, Does.Not.Contain("Changed: Empty slot -> Mana Generator"));

            _overlay.ToggleRunDiagnosticsFocus();
            string restoredText = RefreshText();

            Assert.That(restoredText, Does.Contain("Changed: Empty slot -> Mana Generator. Role: improves mana reserve."));
        }

        [Test]
        public void DiagnosticsStillPreserveRawStructureIdWhereUseful()
        {
            _overlay.SelectMvpStructure(StructureSimulationPass.RiskLabBasicId);
            _overlay.PlaceSelectedMvpStructure();
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
            Assert.That(focusedText, Does.Not.Contain("Selected structure"));
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
            Assert.That(focusedFromRuntimePage, Does.Not.Contain("Place or modify selected"));
            Assert.That(focusedFromRuntimePage, Does.Not.Contain("Run or observe dungeon"));
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

        private static ContentService BuildContent(bool includeDiagnosticsLocalization)
        {
            var content = new ContentService();
            var map = (Dictionary<string, string>)typeof(ContentService).GetField("_stringMap", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(content);
            map["ui.dev.structure_status"] = "Structure Sim — Slot F{0} S{1}, Structure: {2}, Heat Crisis: {3}";
            map["ui.banner.place_success"] = "Placed structure: {0}";
            map["ui.banner.run_simulated"] = "Run simulated.";
            map["ui.banner.run_sim_failed"] = "Run simulation failed.";
            map["structure.mana_generator.basic.display_name"] = "Mana Generator";
            map["structure.heat_scrubber.basic.display_name"] = "Heat Scrubber";
            map["structure.risk_lab.basic.display_name"] = "Risk Lab";
            map["ui.mvp_label.structure.unknown"] = "Unknown structure";
            map["ui.mvp_loop.panel.title"] = "MVP Loop Summary";
            map["ui.mvp_loop.panel.placement_format"] = "Placement: {0}";
            map["ui.mvp_loop.panel.latest_run_format"] = "Latest run: {0}";
            map["ui.mvp_loop.panel.mana_format"] = "Mana reserve: {0:0.##}";
            map["ui.mvp_loop.panel.loot_format"] = "Loot: generated {0}, extracted {1}, tradeable {2}";
            map["ui.mvp_loop.panel.heat_format"] = "Heat: {0:0.##} -> {1:0.##} ({2})";
            map["ui.mvp_loop.panel.research_format"] = "Research: {0}";
            map["ui.mvp_loop.panel.adventurer_party_format"] = "{0}";
            map["ui.mvp_loop.panel.suggestion_format"] = "Next: {0}";
            map["ui.mvp_loop.value.no_placement"] = "No structure placed";
            map["ui.mvp_loop.value.no_run"] = "No run yet";
            map["ui.mvp_loop.value.unknown"] = "Unknown";
            map["ui.mvp_loop.value.no_research"] = "No research";
            map["ui.mvp_loop.run_status.succeeded"] = "Succeeded";
            map["ui.mvp_loop.run_status.failed"] = "Failed";
            map["ui.research.status.verification_required"] = "Verification required";
            map["ui.research.status.blocked_or_invalid"] = "Research unavailable";
            map["mvp_loop.suggestion.run_dungeon"] = "Run the dungeon to observe the first outcome.";
            map["mvp_loop.suggestion.repeat_or_improve_placement"] = "Run again or improve placement based on the summary.";
            map["ui.guided_mvp.panel.title"] = "Guided MVP Action";
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
            map["ui.mvp_structure_preview.unknown"] = "Role unavailable.";
            map["ui.mvp_structure_feedback.empty_slot"] = "Empty slot";
            map["ui.mvp_structure_feedback.changed_format"] = "Changed: {0} -> {1}. {2}";
            map["ui.mvp_run_feedback.success_stable_heat"] = "Run result: succeeded. Loot extracted, heat stable.";
            map["ui.mvp_run_feedback.success_heat_reduced"] = "Run result: succeeded. Loot extracted, heat reduced.";
            map["ui.mvp_run_feedback.success_heat_increased"] = "Run result: succeeded. Loot extracted, heat increased.";
            map["ui.mvp_run_feedback.failed"] = "Run result: failed. Review placement and try again.";
            map["ui.mvp_run_feedback.unavailable"] = "Run result unavailable.";
            map["ui.mvp_run_feedback.format"] = "{0} Mana {1:0.##}. Loot {2}/{3}/{4}. Heat {5:0.##}->{6:0.##}.";
            map["ui.mvp_run_feedback.format_with_party"] = "{0} Mana {1:0.##}. Loot {2}/{3}/{4}. Heat {5:0.##}->{6:0.##}. {7}";
            map["ui.mvp_run_feedback.posture_format"] = "Posture: {0}. {1}";
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
            map["ui.first_session.status.not_started"] = "First-session status: waiting for MVP loop summary.";
            map["ui.first_session.status.place_structure"] = "First-session status: place one structure to start the loop.";
            map["ui.first_session.status.run_dungeon"] = "First-session status: structure placed; run the dungeon next.";
            map["ui.first_session.status.observe_summary"] = "First-session status: observe mana, loot, heat, research, and next action.";
            map["ui.first_session.status.complete"] = "First-session loop complete: placement, run, mana, loot, heat, and research are visible.";
            map["ui.guided_mvp.panel.step_format"] = "Step: {0}";
            map["ui.guided_mvp.panel.status_format"] = "Status: {0}";
            map["ui.guided_mvp.panel.next_action_format"] = "Next action: {0}";
            map["ui.guided_mvp.panel.complete_format"] = "Path complete: {0}";
            map["ui.guided_mvp.value.complete_yes"] = "Yes";
            map["ui.guided_mvp.value.complete_no"] = "No";
            map["guided_mvp.step.place_or_modify_structure"] = "Place or modify one structure";
            map["guided_mvp.step.run_or_observe"] = "Run or observe adventurers";
            map["guided_mvp.step.reduce_heat_pressure"] = "Reduce heat pressure";
            map["guided_mvp.step.improve_survivability_or_layout"] = "Improve survivability or layout";
            map["guided_mvp.step.verify_research_status"] = "Verify research status";
            map["guided_mvp.step.repeat_or_improve"] = "Repeat the loop or improve placement";
            map["guided_mvp.status.missing_save"] = "Save state is not available yet.";
            map["guided_mvp.status.place_or_modify_structure"] = "No placed structure is visible in the current summary.";
            map["guided_mvp.status.run_or_observe"] = "A structure is placed; no adventurer run has been observed yet.";
            map["guided_mvp.status.heat_pressure"] = "The latest summary shows heat pressure.";
            map["guided_mvp.status.poor_loot_extraction"] = "The latest run generated loot but extracted none.";
            map["guided_mvp.status.research_completion_pending"] = "Research completion is pending verification.";
            map["guided_mvp.status.repeat_or_improve"] = "Placement, run, mana, loot, heat, and research are visible in the summary.";
            map["guided_mvp.action.place_structure"] = "Place one structure, or modify the selected slot.";
            map["guided_mvp.action.run_dungeon"] = "Run the dungeon and watch the MVP Loop Summary update.";
            map["guided_mvp.action.reduce_heat_pressure"] = "Improve placement toward lower heat pressure before pushing further.";
            map["guided_mvp.action.improve_survivability_or_layout"] = "Improve survivability or layout, then run again.";
            map["guided_mvp.action.verify_research_status"] = "Check the research status line before claiming progress.";
            map["guided_mvp.action.repeat_or_improve"] = "Run again or adjust one placement based on the summary.";
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
    }
}
