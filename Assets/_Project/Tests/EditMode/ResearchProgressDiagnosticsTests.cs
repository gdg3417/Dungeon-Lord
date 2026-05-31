using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchProgressDiagnosticsTests
    {
        private GameObject _rootObject;
        private GameObject _overlayObject;
        private GameObject _textObject;
        private GameRoot _root;
        private BootstrapOverlay _overlay;

        [SetUp]
        public void SetUp()
        {
            _rootObject = new GameObject("ResearchProgressDiagnosticsRootTest");
            _overlayObject = new GameObject("ResearchProgressDiagnosticsOverlayTest");
            _textObject = new GameObject("ResearchProgressDiagnosticsTextTest");
            _root = _rootObject.AddComponent<GameRoot>();
            SetBackingField("<Content>k__BackingField", BuildContent(includeProgressFormat: true));
            SetBackingField("<Save>k__BackingField", BuildSave());
            _root.RefreshOfflineSummaryLines();

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
        public void SystemsDiagnostics_UsesLocalizedResearchProgressPreviewFormatWithoutRawKey()
        {
            CycleToSystemsDiagnostics();
            string text = RefreshText();

            Assert.That(text, Does.Contain("Research Progress Preview — resolved=True error=0 pending=True slot=research.slot.primary project=research.project.saved elapsedSeconds=0 delta=0 wouldComplete=False ruleSource=research.progress.rule.test"));
            Assert.That(text, Does.Not.Contain("ui.dev.research_progress_format"));
        }

        [Test]
        public void SystemsDiagnostics_NullResearchPending_ShowsSafeNoPendingProgressSummary()
        {
            _root.Save.researchPending = null;
            _root.Save.researchProgress = null;
            _root.RefreshOfflineSummaryLines();
            CycleToSystemsDiagnostics();
            string text = RefreshText();

            Assert.That(text, Does.Contain("pending False  "));
            Assert.That(text, Does.Contain("Research Progress Preview — resolved=False error=1 pending=False slot= project="));
            Assert.That(text, Does.Contain("Research Progress State — resolved=False error=1 pending=False hasState=False slot= project="));
        }

        [Test]
        public void SystemsDiagnostics_PendingWithoutProgressState_ShowsSafeMissingStateSummary()
        {
            CycleToSystemsDiagnostics();

            Assert.That(RefreshText(), Does.Contain("Research Progress State — resolved=False error=2 pending=True hasState=False slot= project="));
        }

        [Test]
        public void SystemsDiagnostics_MatchingZeroProgressState_ShowsResolvedLocalizedStateWithoutRawKey()
        {
            _root.Save.researchProgress = new ResearchProgressState
            {
                SlotId = "research.slot.primary",
                ProjectId = "research.project.saved",
                RuleSourceIdUsed = "research.progress.rule.saved"
            };
            _root.RefreshOfflineSummaryLines();
            CycleToSystemsDiagnostics();
            string text = RefreshText();

            Assert.That(text, Does.Contain("Research Progress State — resolved=True error=0 pending=True hasState=True slot=research.slot.primary project=research.project.saved progress=0 completionPending=False matchesPending=True ruleSource=research.progress.rule.saved"));
            Assert.That(text, Does.Not.Contain("ui.dev.research_progress_state_format"));
        }

        [Test]
        public void SystemsDiagnostics_StaleProgressState_ShowsSafeMismatch()
        {
            _root.Save.researchProgress = new ResearchProgressState
            {
                SlotId = "research.slot.primary",
                ProjectId = "research.project.stale",
                RuleSourceIdUsed = "research.progress.rule.saved"
            };
            _root.RefreshOfflineSummaryLines();
            CycleToSystemsDiagnostics();

            Assert.That(RefreshText(), Does.Contain("Research Progress State — resolved=False error=5 pending=True hasState=True slot=research.slot.primary project=research.project.stale progress=0 completionPending=False matchesPending=False"));
        }

        [Test]
        public void SystemsDiagnostics_EmptyResearchPendingMarker_NormalizesToSafeNoPendingProgressSummaryWithoutMutation()
        {
            var emptyMarker = new ResearchPendingState();
            _root.Save.researchPending = emptyMarker;
            string before = JsonUtility.ToJson(_root.Save);

            _root.RefreshOfflineSummaryLines();
            CycleToSystemsDiagnostics();
            string text = RefreshText();

            Assert.That(text, Does.Contain("pending False  "));
            Assert.That(text, Does.Contain("validation True 0 research.pending.rule.test"));
            Assert.That(text, Does.Contain("Research Progress Preview — resolved=False error=1 pending=False slot= project="));
            Assert.That(text, Does.Not.Contain("Research Progress Preview — resolved=False error=5"));
            Assert.That(_root.Save.researchPending, Is.SameAs(emptyMarker));
            Assert.That(JsonUtility.ToJson(_root.Save), Is.EqualTo(before));
        }

        [Test]
        public void SystemsDiagnostics_ActiveSessionTicksRenderNonZeroPreviewWithoutCompletion()
        {
            SetBackingField("_activeSessionTickCount", 12L);
            _root.RefreshOfflineSummaryLines();
            CycleToSystemsDiagnostics();
            string text = RefreshText();

            Assert.That(text, Does.Contain("elapsedSeconds=120 delta=1.2 wouldComplete=False"));
            Assert.That(_root.Save.researchPending.ProjectId, Is.EqualTo("research.project.saved"));
        }

        [Test]
        public void MissingLocalization_UsesStableResearchProgressKeyFallback()
        {
            SetBackingField("<Content>k__BackingField", BuildContent(includeProgressFormat: false));
            _root.RefreshOfflineSummaryLines();
            CycleToSystemsDiagnostics();

            Assert.That(RefreshText(), Does.Contain("ui.dev.research_progress_format"));
            Assert.That(RefreshText(), Does.Contain("ui.dev.research_progress_state_format"));
        }

        [Test]
        public void SetAndClearResearchPending_RefreshesPreviewAndDoesNotLeaveStaleDiagnostics()
        {
            Assert.That(_root.SetResearchPendingScaffold(), Is.True);
            Assert.That(_root.ResearchProgressLine, Does.Contain("pending=True slot=research.slot.primary project=research.project.scaffold"));
            Assert.That(_root.ResearchProgressLine, Does.Contain("wouldComplete=False"));
            Assert.That(_root.Save.researchProgress, Is.Not.Null);
            Assert.That(_root.ResearchProgressStateLine, Does.Contain("resolved=True error=0 pending=True hasState=True slot=research.slot.primary project=research.project.scaffold progress=0 completionPending=False matchesPending=True"));

            Assert.That(_root.ClearResearchPendingScaffold(), Is.True);
            Assert.That(_root.ResearchProgressLine, Does.Contain("resolved=False error=1 pending=False slot= project="));
            Assert.That(_root.ResearchProgressLine, Does.Not.Contain("research.project.scaffold"));
            Assert.That(_root.Save.researchProgress, Is.Null);
            Assert.That(_root.ResearchProgressStateLine, Does.Contain("resolved=False error=1 pending=False hasState=False slot= project="));
            Assert.That(_root.ResearchProgressStateLine, Does.Not.Contain("research.project.scaffold"));
        }

        [Test]
        public void DiagnosticsRefresh_DoesNotMutateSaveOrCompleteResearch()
        {
            SaveData save = _root.Save;
            string before = JsonUtility.ToJson(save);

            _root.RefreshOfflineSummaryLines();
            CycleToSystemsDiagnostics();
            RefreshText();

            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
            Assert.That(save.researchPending.ProjectId, Is.EqualTo("research.project.saved"));
            Assert.That(save.structureRuntime.Heat, Is.EqualTo(17d));
            Assert.That(save.structureRuntime.ManaReserve, Is.EqualTo(23d));
            Assert.That(save.totalTicks, Is.EqualTo(19));
            Assert.That(save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
        }

        [Test]
        public void RuntimeCSharp_DoesNotHardcodeVisibleResearchProgressDiagnosticEnglish()
        {
            string gameRoot = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/Core/GameRoot.cs"));
            string overlay = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/UI/BootstrapOverlay.cs"));

            Assert.That(gameRoot, Does.Not.Contain("Research Progress Preview —"));
            Assert.That(overlay, Does.Not.Contain("Research Progress Preview —"));
            Assert.That(gameRoot, Does.Not.Contain("Research Progress State —"));
            Assert.That(overlay, Does.Not.Contain("Research Progress State —"));
        }

        private static SaveData BuildSave()
        {
            return new SaveData
            {
                totalTicks = 19,
                researchPending = new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = "research.project.saved" },
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 23d },
                runHistory = new RunHistoryState(),
                lastOfflineSummary = new OfflineSummary
                {
                    RuleResolved = true,
                    OfflineSecondsObserved = 60,
                    WouldProcessOfflineProgress = false,
                    RuleSourceIdUsed = "offline.summary.rule.test"
                }
            };
        }

        private static ContentService BuildContent(bool includeProgressFormat)
        {
            var content = new ContentService();
            typeof(ContentService).GetField("<Bootstrap>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(content, new ContentBootstrap
                {
                    tickSeconds = 10,
                    researchPendingScaffold = new ResearchPendingScaffoldConfig
                    {
                        enabled = true,
                        slotId = "research.slot.primary",
                        projectId = "research.project.scaffold",
                        ruleSourceId = "research.pending.rule.test"
                    },
                    researchProgressScaffold = new ResearchProgressScaffoldConfig
                    {
                        enabled = true,
                        ruleSourceId = "research.progress.rule.test",
                        progressPerActiveSecond = 0.01d,
                        maxActiveSessionElapsedSeconds = 600
                    }
                });
            var map = (Dictionary<string, string>)typeof(ContentService).GetField("_stringMap", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(content);
            map["ui.dev.hint.toggle_panel"] = "toggle-panel";
            map["ui.dev.hint.toggle_run_diagnostics"] = "toggle-run";
            map["ui.dev.hint.cycle_diagnostics_page"] = "cycle-page";
            map["ui.dev.diagnostics.header_format"] = "Diagnostics: {0} Page {1}/{2}";
            map["ui.dev.diagnostics.page.systems_diagnostics"] = "Systems Diagnostics";
            map["ui.dev.structure_status"] = "structure {0} {1} {2} {3}";
            map["ui.dev.offline_summary_format"] = "offline {0} {1} {2} {3} {4} {5}";
            map["ui.dev.research_pending_format"] = "pending {0} {1} {2}";
            map["ui.dev.research_pending_validation_format"] = "validation {0} {1} {2}";
            if (includeProgressFormat)
            {
                map["ui.dev.research_progress_format"] = "Research Progress Preview — resolved={0} error={1} pending={2} slot={3} project={4} elapsedSeconds={5} delta={6:0.###} wouldComplete={7} ruleSource={8}";
                map["ui.dev.research_progress_state_format"] = "Research Progress State — resolved={0} error={1} pending={2} hasState={3} slot={4} project={5} progress={6:0.###} completionPending={7} matchesPending={8} ruleSource={9}";
            }
            return content;
        }

        private void CycleToSystemsDiagnostics()
        {
            _overlay.CycleFullDiagnosticsPage();
            _overlay.CycleFullDiagnosticsPage();
            _overlay.CycleFullDiagnosticsPage();
        }

        private string RefreshText()
        {
            _overlay.RefreshOverlayText();
            return _overlay.overlayText.text;
        }

        private void SetBackingField(string name, object value)
        {
            typeof(GameRoot).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_root, value);
        }
    }
}
