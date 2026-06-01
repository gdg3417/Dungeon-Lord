using DungeonBuilder.M0;
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

            _root.SetBanner("banner-line");
            SetSave(new SaveData
            {
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 9d, IsHeatCrisisActive = true },
                runHistory = new RunHistoryState
                {
                    NextRunSequence = 3,
                    RecentOutcomes = new[] { new RunOutcomeRecord { RunId = "run-history-entry" } }
                }
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
        public void FullDiagnostics_InitialPage_IsRuntimeSummaryWithLocalizedHeader()
        {
            string text = RefreshText();

            Assert.That(_overlay.FullDiagnosticsPageNumber, Is.EqualTo(1));
            Assert.That(text, Does.StartWith("Diagnostics: Runtime Summary Page 1/5\nF1 toggles Dev Panel\nF2 toggles Run Diagnostics focus\nF3 cycles Diagnostics Page"));
            Assert.That(text, Does.Contain("build-line"));
            Assert.That(text, Does.Contain("Mouse wheel or PageUp PageDown scroll diagnostics"));
            Assert.That(text, Does.Not.Contain("banner-line"));
            Assert.That(text, Does.Not.Contain("run-line"));
            Assert.That(text, Does.Not.Contain("run-heat-delta-line"));
        }

        [Test]
        public void FullDiagnostics_F3PageCycle_WrapsFromResearchBackToRuntimeSummary()
        {
            AssertPage(1, "Runtime Summary");
            CycleAndAssertPage(2, "Run Diagnostics");
            CycleAndAssertPage(3, "Heat Diagnostics");
            CycleAndAssertPage(4, "Systems Diagnostics");
            CycleAndAssertPage(5, "Research Diagnostics");
            CycleAndAssertPage(1, "Runtime Summary");
        }

        [Test]
        public void FullDiagnostics_PagesIncludeTheirLinesAndExcludeOtherPageLines()
        {
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
        }

        [Test]
        public void RunDiagnosticsFocus_IncludesExpectedLinesAndDoesNotDependOnFullPageSelection()
        {
            _overlay.ToggleRunDiagnosticsFocus();
            string focusedFromRuntimePage = RefreshText();

            Assert.That(focusedFromRuntimePage, Does.StartWith("Diagnostics: Run Diagnostics Focus"));
            Assert.That(focusedFromRuntimePage, Does.Contain("run-line"));
            Assert.That(focusedFromRuntimePage, Does.Contain("run-history-line"));
            Assert.That(focusedFromRuntimePage, Does.Contain("run-loot-line"));
            AssertLinesAppearInOrder(focusedFromRuntimePage, "run-heat-cooling-line", "run-heat-delta-line", "run-heat-application-line", "run-attraction-line");
            Assert.That(focusedFromRuntimePage, Does.Not.Contain("build-line"));

            _overlay.CycleFullDiagnosticsPage();
            _overlay.CycleFullDiagnosticsPage();
            Assert.That(RefreshText(), Is.EqualTo(focusedFromRuntimePage));
        }

        [Test]
        public void ResearchDiagnostics_ScrollOffsetClampsAndRevealsBottomLines()
        {
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

            for (int i = 0; i < 5; i++)
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
        }

        [Test]
        public void Header_MissingLocalization_UsesLocalizationKeyFallbacksSafely()
        {
            SetContent(BuildContent(includeDiagnosticsLocalization: false));

            string text = RefreshText();

            Assert.That(text, Does.StartWith("ui.dev.diagnostics.header_format"));
            Assert.That(text, Does.Contain("ui.dev.hint.cycle_diagnostics_page"));
            Assert.That(text, Does.Contain("ui.dev.hint.scroll_diagnostics"));

            _overlay.ToggleRunDiagnosticsFocus();
            Assert.That(RefreshText(), Does.StartWith("ui.dev.diagnostics.focus.run_diagnostics"));
        }

        private void AssertPage(int number, string name)
        {
            Assert.That(_overlay.FullDiagnosticsPageNumber, Is.EqualTo(number));
            Assert.That(RefreshText(), Does.StartWith($"Diagnostics: {name} Page {number}/5"));
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
            }
            return content;
        }
    }
}
