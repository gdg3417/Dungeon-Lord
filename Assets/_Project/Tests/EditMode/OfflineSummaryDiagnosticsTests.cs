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
    public class OfflineSummaryDiagnosticsTests
    {
        private GameObject _rootObject;
        private GameObject _overlayObject;
        private GameObject _textObject;
        private GameRoot _root;
        private BootstrapOverlay _overlay;

        [SetUp]
        public void SetUp()
        {
            _rootObject = new GameObject("OfflineSummaryDiagnosticsRootTest");
            _overlayObject = new GameObject("OfflineSummaryDiagnosticsOverlayTest");
            _textObject = new GameObject("OfflineSummaryDiagnosticsTextTest");
            _root = _rootObject.AddComponent<GameRoot>();
            SetBackingField("<Content>k__BackingField", BuildContent(includeFormats: true));
            SetBackingField("<Save>k__BackingField", new SaveData
            {
                lastSavedUtcUnix = 100,
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 23d },
                runHistory = new RunHistoryState(),
                researchPending = new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = "research.project.pending" }
            });
            typeof(GameRoot).GetField("_offlineSummaryResolver", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_root, new OfflineSummaryResolver(new FixedTimeSource(160)));
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
        public void SystemsDiagnostics_IncludesLocalizedOfflineSummaryAndResearchPendingLines()
        {
            CycleToSystemsDiagnostics();
            string text = RefreshText();

            Assert.That(text, Does.Contain("Offline Summary — resolved=True error=0 observedSeconds=60 clamped=False wouldProcess=False ruleSource=offline.summary.rule.test"));
            Assert.That(text, Does.Contain("Research Pending — pending=True slot=research.slot.primary project=research.project.pending"));
        }

        [Test]
        public void RefreshAndPageCycling_DoNotApplyOfflineRewardsResearchCompletionOrHeatMutation()
        {
            SaveData save = _root.Save;
            StructureRuntimeState structures = save.structureRuntime;
            ResearchPendingState research = save.researchPending;
            RunHistoryState history = save.runHistory;

            _root.RefreshOfflineSummaryLines();
            CycleToSystemsDiagnostics();
            RefreshText();

            Assert.That(save.structureRuntime, Is.SameAs(structures));
            Assert.That(structures.Heat, Is.EqualTo(17d));
            Assert.That(structures.ManaReserve, Is.EqualTo(23d));
            Assert.That(save.runHistory, Is.SameAs(history));
            Assert.That(history.RecentOutcomes, Is.Empty);
            Assert.That(save.researchPending, Is.SameAs(research));
            Assert.That(research.ProjectId, Is.EqualTo("research.project.pending"));
        }

        [Test]
        public void MissingLocalization_UsesStableKeysAsSafeFallback()
        {
            SetBackingField("<Content>k__BackingField", BuildContent(includeFormats: false));
            _root.RefreshOfflineSummaryLines();
            CycleToSystemsDiagnostics();
            string text = RefreshText();

            Assert.That(text, Does.Contain("ui.dev.offline_summary_format"));
            Assert.That(text, Does.Contain("ui.dev.research_pending_format"));
        }

        [Test]
        public void RuntimeCSharp_DoesNotHardcodeNewVisibleDiagnosticEnglish()
        {
            string gameRoot = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/Core/GameRoot.cs"));
            string overlay = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/UI/BootstrapOverlay.cs"));

            Assert.That(gameRoot, Does.Not.Contain("Offline Summary —"));
            Assert.That(gameRoot, Does.Not.Contain("Research Pending —"));
            Assert.That(overlay, Does.Not.Contain("Offline Summary —"));
            Assert.That(overlay, Does.Not.Contain("Research Pending —"));
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

        private static ContentService BuildContent(bool includeFormats)
        {
            var content = new ContentService();
            typeof(ContentService).GetField("<Bootstrap>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(content, new ContentBootstrap
                {
                    timeRules = new TimeRules { maxOfflineSeconds = 100, offlineSummaryRuleSourceId = "offline.summary.rule.test" }
                });
            var map = (Dictionary<string, string>)typeof(ContentService).GetField("_stringMap", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(content);
            map["ui.dev.hint.toggle_panel"] = "toggle-panel";
            map["ui.dev.hint.toggle_run_diagnostics"] = "toggle-run";
            map["ui.dev.hint.cycle_diagnostics_page"] = "cycle-page";
            map["ui.dev.diagnostics.header_format"] = "Diagnostics: {0} Page {1}/{2}";
            map["ui.dev.diagnostics.page.systems_diagnostics"] = "Systems Diagnostics";
            map["ui.dev.structure_status"] = "structure {0} {1} {2} {3}";
            if (includeFormats)
            {
                map["ui.dev.offline_summary_format"] = "Offline Summary — resolved={0} error={1} observedSeconds={2} clamped={3} wouldProcess={4} ruleSource={5}";
                map["ui.dev.research_pending_format"] = "Research Pending — pending={0} slot={1} project={2}";
            }
            return content;
        }

        private sealed class FixedTimeSource : ITimeSource
        {
            private readonly long _timestamp;

            public FixedTimeSource(long timestamp)
            {
                _timestamp = timestamp;
            }

            public long UtcNowUnixSeconds()
            {
                return _timestamp;
            }
        }
    }
}
