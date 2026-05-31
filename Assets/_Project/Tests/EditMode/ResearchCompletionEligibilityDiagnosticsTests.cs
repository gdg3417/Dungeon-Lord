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
    public class ResearchCompletionEligibilityDiagnosticsTests
    {
        private GameObject _rootObject;
        private GameObject _overlayObject;
        private GameObject _textObject;
        private GameRoot _root;
        private BootstrapOverlay _overlay;

        [SetUp]
        public void SetUp()
        {
            _rootObject = new GameObject("ResearchCompletionEligibilityDiagnosticsRootTest");
            _overlayObject = new GameObject("ResearchCompletionEligibilityDiagnosticsOverlayTest");
            _textObject = new GameObject("ResearchCompletionEligibilityDiagnosticsTextTest");
            _root = _rootObject.AddComponent<GameRoot>();
            SetBackingField("<Content>k__BackingField", BuildContent());
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
        public void ResearchDiagnostics_ShowsExistingProgressLinesAndLocalizedEligibilityWithoutRawKeys()
        {
            string text = ResearchDiagnosticsText();

            Assert.That(text, Does.Contain("Research Progress Preview —"));
            Assert.That(text, Does.Contain("Research Progress State —"));
            Assert.That(text, Does.Contain("Research Completion Eligibility — resolved=True error=0 pending=True hasState=True slot=research.slot.primary project=research.project.scaffold progress=0 required=2 remaining=2 eligible=False wouldSetCompletionPending=False wouldComplete=False ruleSource=research.completion_eligibility.rule.test"));
            Assert.That(text, Does.Not.Contain("ui.dev.research_completion_eligibility_format"));
            Assert.That(text, Does.Contain("Research Completion Pending Apply — resolved=True error=0 pending=True hasState=True slot=research.slot.primary project=research.project.scaffold progress=0 required=2 eligible=False alreadyCompletionPending=False wouldSetCompletionPending=False wouldComplete=False ruleSource=research.completion_eligibility.rule.test"));
            Assert.That(text, Does.Not.Contain("ui.dev.research_completion_pending_apply_format"));
            Assert.That(text, Does.Contain("Research Completion Claim Readiness — resolved=True error=0 pending=True hasState=True slot=research.slot.primary project=research.project.scaffold progress=0 required=2 completionPending=False eligible=False readyForClaim=False wouldComplete=False wouldGrantRewards=False wouldUnlockContent=False wouldClearPending=False ruleSource=research.completion_eligibility.rule.test"));
            Assert.That(text, Does.Not.Contain("ui.dev.research_completion_claim_readiness_format"));
        }

        [Test]
        public void SystemsDiagnostics_DoesNotContainResearchDiagnosticBlock()
        {
            _overlay.CycleFullDiagnosticsPage();
            _overlay.CycleFullDiagnosticsPage();
            _overlay.CycleFullDiagnosticsPage();
            _overlay.RefreshOverlayText();
            string text = _overlay.overlayText.text;

            Assert.That(text, Does.Contain("Offline Summary —"));
            Assert.That(text, Does.Not.Contain("Research Pending —"));
            Assert.That(text, Does.Not.Contain("Research Progress Preview —"));
            Assert.That(text, Does.Not.Contain("Research Progress State —"));
            Assert.That(text, Does.Not.Contain("Research Completion Eligibility —"));
            Assert.That(text, Does.Not.Contain("Research Completion Pending Apply —"));
            Assert.That(text, Does.Not.Contain("Research Completion Claim Readiness —"));
        }

        [Test]
        public void RefreshOfflineSummaryLines_NoPendingShowsSafeOutputWithoutStaleProject()
        {
            _root.Save.researchPending = null;
            _root.Save.researchProgress = null;

            _root.RefreshOfflineSummaryLines();

            Assert.That(_root.ResearchCompletionEligibilityLine, Does.Contain("resolved=False error=1 pending=False hasState=False slot= project= progress=0 required=0 remaining=0 eligible=False wouldSetCompletionPending=False wouldComplete=False ruleSource="));
            Assert.That(_root.ResearchCompletionEligibilityLine, Does.Not.Contain("research.project.scaffold"));
            Assert.That(_root.ResearchCompletionPendingApplyLine, Does.Contain("resolved=False error=1 pending=False hasState=False slot= project= progress=0 required=0 eligible=False alreadyCompletionPending=False wouldSetCompletionPending=False wouldComplete=False ruleSource="));
            Assert.That(_root.ResearchCompletionPendingApplyLine, Does.Not.Contain("research.project.scaffold"));
            Assert.That(_root.ResearchCompletionClaimReadinessLine, Does.Contain("resolved=False error=1 pending=False hasState=False slot= project= progress=0 required=0 completionPending=False eligible=False readyForClaim=False wouldComplete=False wouldGrantRewards=False wouldUnlockContent=False wouldClearPending=False ruleSource="));
            Assert.That(_root.ResearchCompletionClaimReadinessLine, Does.Not.Contain("research.project.scaffold"));
        }

        [Test]
        public void ActiveTickProgressApply_EnoughProgressMakesEligibilityTrueWithoutCompletionMutationOrAdjacentRewards()
        {
            SaveData save = _root.Save;
            double heatBefore = save.structureRuntime.Heat;
            double manaBefore = save.structureRuntime.ManaReserve;
            string historyBefore = JsonUtility.ToJson(save.runHistory);
            string offlineBefore = JsonUtility.ToJson(save.lastOfflineSummary);

            InvokeResearchProgressApply();
            _root.RefreshOfflineSummaryLines();
            Assert.That(_root.ResearchCompletionEligibilityLine, Does.Contain("progress=1 required=2 remaining=1 eligible=False"));
            Assert.That(_root.ResearchCompletionClaimReadinessLine, Does.Contain("progress=1 required=2 completionPending=False eligible=False readyForClaim=False"));
            InvokeResearchProgressApply();
            _root.RefreshOfflineSummaryLines();

            Assert.That(_root.ResearchCompletionEligibilityLine, Does.Contain("progress=2 required=2 remaining=0 eligible=True wouldSetCompletionPending=False wouldComplete=False"));
            Assert.That(_root.ResearchCompletionClaimReadinessLine, Does.Contain("progress=2 required=2 completionPending=False eligible=True readyForClaim=False wouldComplete=False wouldGrantRewards=False wouldUnlockContent=False wouldClearPending=False"));
            Assert.That(save.researchProgress.CompletionPending, Is.False);
            Assert.That(save.researchPending, Is.Not.Null);
            Assert.That(save.researchPending.ProjectId, Is.EqualTo("research.project.scaffold"));
            Assert.That(save.structureRuntime.Heat, Is.EqualTo(heatBefore));
            Assert.That(save.structureRuntime.ManaReserve, Is.EqualTo(manaBefore));
            Assert.That(JsonUtility.ToJson(save.runHistory), Is.EqualTo(historyBefore));
            Assert.That(JsonUtility.ToJson(save.lastOfflineSummary), Is.EqualTo(offlineBefore));
            Assert.That(save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
        }

        [Test]
        public void CompletionPendingApply_AfterThresholdShowsResolvedEligibilityAndAlreadyPendingWithoutCompletionOrAdjacentRewards()
        {
            SaveData save = _root.Save;
            double heatBefore = save.structureRuntime.Heat;
            double manaBefore = save.structureRuntime.ManaReserve;
            string pendingBefore = JsonUtility.ToJson(save.researchPending);
            string historyBefore = JsonUtility.ToJson(save.runHistory);
            string offlineBefore = JsonUtility.ToJson(save.lastOfflineSummary);
            long totalTicksBefore = save.totalTicks;

            InvokeResearchProgressApply();
            InvokeResearchProgressApply();
            InvokeResearchCompletionPendingApply();
            _root.RefreshOfflineSummaryLines();

            Assert.That(save.researchProgress.ProgressUnits, Is.EqualTo(2d));
            Assert.That(save.researchProgress.CompletionPending, Is.True);
            Assert.That(_root.ResearchCompletionEligibilityLine, Does.Contain("resolved=True error=0 pending=True hasState=True"));
            Assert.That(_root.ResearchCompletionEligibilityLine, Does.Contain("progress=2 required=2 remaining=0 eligible=True wouldSetCompletionPending=False wouldComplete=False"));
            Assert.That(_root.ResearchCompletionPendingApplyLine, Does.Contain("eligible=True alreadyCompletionPending=True wouldSetCompletionPending=False wouldComplete=False"));
            Assert.That(_root.ResearchCompletionClaimReadinessLine, Does.Contain("completionPending=True eligible=True readyForClaim=True wouldComplete=False wouldGrantRewards=False wouldUnlockContent=False wouldClearPending=False"));
            Assert.That(JsonUtility.ToJson(save.researchPending), Is.EqualTo(pendingBefore));
            Assert.That(save.structureRuntime.Heat, Is.EqualTo(heatBefore));
            Assert.That(save.structureRuntime.ManaReserve, Is.EqualTo(manaBefore));
            Assert.That(JsonUtility.ToJson(save.runHistory), Is.EqualTo(historyBefore));
            Assert.That(JsonUtility.ToJson(save.lastOfflineSummary), Is.EqualTo(offlineBefore));
            Assert.That(save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
            Assert.That(save.totalTicks, Is.EqualTo(totalTicksBefore));
        }

        [Test]
        public void ClearResearchPendingScaffold_ReturnsEligibilityToNoPendingWithoutStaleProject()
        {
            InvokeResearchProgressApply();
            InvokeResearchProgressApply();
            _root.RefreshOfflineSummaryLines();

            Assert.That(_root.ClearResearchPendingScaffold(), Is.True);

            Assert.That(_root.ResearchCompletionEligibilityLine, Does.Contain("resolved=False error=1 pending=False hasState=False slot= project="));
            Assert.That(_root.ResearchCompletionEligibilityLine, Does.Not.Contain("research.project.scaffold"));
            Assert.That(_root.ResearchCompletionClaimReadinessLine, Does.Contain("resolved=False error=1 pending=False hasState=False slot= project="));
            Assert.That(_root.ResearchCompletionClaimReadinessLine, Does.Not.Contain("research.project.scaffold"));
            Assert.That(_root.Save.researchPending, Is.Null);
            Assert.That(_root.Save.researchProgress, Is.Null);
        }

        [Test]
        public void RefreshOfflineSummaryLines_MissingLocalizationUsesSafeFallbackKey()
        {
            StringMap().Remove("ui.dev.research_completion_eligibility_format");
            StringMap().Remove("ui.dev.research_completion_pending_apply_format");
            StringMap().Remove("ui.dev.research_completion_claim_readiness_format");

            _root.RefreshOfflineSummaryLines();

            Assert.That(_root.ResearchCompletionEligibilityLine, Is.EqualTo("ui.dev.research_completion_eligibility_format"));
            Assert.That(_root.ResearchCompletionPendingApplyLine, Is.EqualTo("ui.dev.research_completion_pending_apply_format"));
            Assert.That(_root.ResearchCompletionClaimReadinessLine, Is.EqualTo("ui.dev.research_completion_claim_readiness_format"));
        }

        [Test]
        public void RefreshOfflineSummaryLines_DoesNotMutateSave()
        {
            string before = JsonUtility.ToJson(_root.Save);

            _root.RefreshOfflineSummaryLines();

            Assert.That(JsonUtility.ToJson(_root.Save), Is.EqualTo(before));
            Assert.That(_root.Save.researchProgress.CompletionPending, Is.False);
        }

        [Test]
        public void RuntimeCSharp_DoesNotHardcodeVisibleEligibilityDiagnosticEnglish()
        {
            string gameRoot = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/Core/GameRoot.cs"));
            string overlay = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/UI/BootstrapOverlay.cs"));

            Assert.That(gameRoot, Does.Not.Contain("Research Completion Eligibility —"));
            Assert.That(overlay, Does.Not.Contain("Research Completion Eligibility —"));
            Assert.That(gameRoot, Does.Not.Contain("Research Completion Pending Apply —"));
            Assert.That(overlay, Does.Not.Contain("Research Completion Pending Apply —"));
            Assert.That(gameRoot, Does.Not.Contain("Research Completion Claim Readiness —"));
            Assert.That(overlay, Does.Not.Contain("Research Completion Claim Readiness —"));
        }

        private string ResearchDiagnosticsText()
        {
            _overlay.CycleFullDiagnosticsPage();
            _overlay.CycleFullDiagnosticsPage();
            _overlay.CycleFullDiagnosticsPage();
            _overlay.CycleFullDiagnosticsPage();
            _overlay.RefreshOverlayText();
            return _overlay.overlayText.text;
        }

        private void InvokeResearchProgressApply()
        {
            typeof(GameRoot).GetMethod("ApplyResearchProgressForActiveTick", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(_root, null);
        }

        private void InvokeResearchCompletionPendingApply()
        {
            typeof(GameRoot).GetMethod("ApplyResearchCompletionPendingForActiveTick", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(_root, null);
        }

        private void SetBackingField(string name, object value)
        {
            typeof(GameRoot).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_root, value);
        }

        private Dictionary<string, string> StringMap()
        {
            return (Dictionary<string, string>)typeof(ContentService).GetField("_stringMap", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_root.Content);
        }

        private static ContentService BuildContent()
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
                        progressPerActiveSecond = 0.1d,
                        maxActiveSessionElapsedSeconds = 600
                    },
                    researchCompletionEligibilityScaffold = new ResearchCompletionEligibilityScaffoldConfig
                    {
                        enabled = true,
                        ruleSourceId = "research.completion_eligibility.rule.test",
                        projectId = "research.project.scaffold",
                        requiredProgressUnits = 2d
                    },
                    timeRules = new TimeRules
                    {
                        maxOfflineSeconds = 600,
                        offlineSummaryRuleSourceId = "offline.summary.rule.test"
                    }
                });
            var map = (Dictionary<string, string>)typeof(ContentService).GetField("_stringMap", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(content);
            map["ui.dev.hint.toggle_panel"] = "toggle-panel";
            map["ui.dev.hint.toggle_run_diagnostics"] = "toggle-run";
            map["ui.dev.hint.cycle_diagnostics_page"] = "cycle-page";
            map["ui.dev.diagnostics.header_format"] = "Diagnostics: {0} Page {1}/{2}";
            map["ui.dev.diagnostics.page.systems_diagnostics"] = "Systems Diagnostics";
            map["ui.dev.diagnostics.page.research_diagnostics"] = "Research Diagnostics";
            map["ui.dev.structure_status"] = "structure {0} {1} {2} {3}";
            map["ui.dev.offline_summary_format"] = "Offline Summary — resolved={0} error={1} observedSeconds={2} clamped={3} wouldProcess={4} ruleSource={5}";
            map["ui.dev.research_pending_format"] = "Research Pending — pending={0} slot={1} project={2}";
            map["ui.dev.research_pending_validation_format"] = "Research Pending Validation — resolved={0} error={1} ruleSource={2}";
            map["ui.dev.research_progress_format"] = "Research Progress Preview — resolved={0} error={1} pending={2} slot={3} project={4} elapsedSeconds={5} delta={6:0.###} wouldComplete={7} ruleSource={8}";
            map["ui.dev.research_progress_state_format"] = "Research Progress State — resolved={0} error={1} pending={2} hasState={3} slot={4} project={5} progress={6:0.###} completionPending={7} matchesPending={8} ruleSource={9}";
            map["ui.dev.research_completion_eligibility_format"] = "Research Completion Eligibility — resolved={0} error={1} pending={2} hasState={3} slot={4} project={5} progress={6:0.###} required={7:0.###} remaining={8:0.###} eligible={9} wouldSetCompletionPending={10} wouldComplete={11} ruleSource={12}";
            map["ui.dev.research_completion_pending_apply_format"] = "Research Completion Pending Apply — resolved={0} error={1} pending={2} hasState={3} slot={4} project={5} progress={6:0.###} required={7:0.###} eligible={8} alreadyCompletionPending={9} wouldSetCompletionPending={10} wouldComplete={11} ruleSource={12}";
            map["ui.dev.research_completion_claim_readiness_format"] = "Research Completion Claim Readiness — resolved={0} error={1} pending={2} hasState={3} slot={4} project={5} progress={6:0.###} required={7:0.###} completionPending={8} eligible={9} readyForClaim={10} wouldComplete={11} wouldGrantRewards={12} wouldUnlockContent={13} wouldClearPending={14} ruleSource={15}";
            return content;
        }

        private static SaveData BuildSave()
        {
            return new SaveData
            {
                totalTicks = 19,
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 23d, LastProcessedTick = 11 },
                runHistory = new RunHistoryState(),
                researchPending = new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = "research.project.scaffold" },
                researchProgress = new ResearchProgressState
                {
                    SlotId = "research.slot.primary",
                    ProjectId = "research.project.scaffold",
                    RuleSourceIdUsed = "research.progress.rule.test"
                },
                lastOfflineSummary = new OfflineSummary
                {
                    RuleResolved = true,
                    OfflineSecondsObserved = 60,
                    WouldProcessOfflineProgress = false,
                    RuleSourceIdUsed = "offline.summary.rule.test"
                }
            };
        }
    }
}
