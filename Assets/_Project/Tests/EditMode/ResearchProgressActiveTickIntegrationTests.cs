#if UNITY_EDITOR
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchProgressActiveTickIntegrationTests
    {
        private GameObject _rootObject;
        private GameRoot _root;

        [SetUp]
        public void SetUp()
        {
            _rootObject = new GameObject("ResearchProgressActiveTickIntegrationRootTest");
            _root = _rootObject.AddComponent<GameRoot>();
            SetBackingField("<Content>k__BackingField", BuildContent());
            SetBackingField("<Save>k__BackingField", BuildSave());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rootObject);
        }

        [Test]
        public void SetResearchPendingScaffold_InitializesZeroProgressState()
        {
            _root.Save.researchPending = null;
            _root.Save.researchProgress = null;

            Assert.That(_root.SetResearchPendingScaffold(), Is.True);
            Assert.That(_root.Save.researchProgress, Is.Not.Null);
            Assert.That(_root.Save.researchProgress.ProgressUnits, Is.Zero);
            Assert.That(_root.Save.researchProgress.CompletionPending, Is.False);
        }

        [Test]
        public void HandleSimulationTick_OneActiveTickAppliesExactlyConfiguredPerTickDelta()
        {
            InvokeSimulationTick(1);

            Assert.That(_root.Save.researchProgress.ProgressUnits, Is.EqualTo(1d));
        }

        [Test]
        public void HandleSimulationTick_BeforeCompletionPendingThresholdMultipleActiveTicksApplySumOfPerTickDeltasOnly()
        {
            _root.Content.Bootstrap.researchCompletionEligibilityScaffold.requiredProgressUnits = 4d;

            InvokeSimulationTick(1);
            InvokeSimulationTick(2);
            InvokeSimulationTick(3);

            Assert.That(_root.Save.researchProgress.ProgressUnits, Is.EqualTo(3d));
            Assert.That(_root.Save.researchProgress.CompletionPending, Is.False);
        }

        [Test]
        public void HandleSimulationTick_DoesNotDoubleCountCumulativePreviewDelta()
        {
            SetPrivateField("_activeSessionTickCount", 12L);

            InvokeSimulationTick(13);

            Assert.That(_root.Save.researchProgress.ProgressUnits, Is.EqualTo(1d));
        }

        [Test]
        public void ClearResearchPendingScaffold_StopsFutureActiveProgressApplication()
        {
            InvokeSimulationTick(1);
            Assert.That(_root.ClearResearchPendingScaffold(), Is.True);

            InvokeSimulationTick(2);

            Assert.That(_root.Save.researchPending, Is.Null);
            Assert.That(_root.Save.researchProgress, Is.Null);
        }

        [Test]
        public void ApplyResearchProgressForActiveTick_MissingProgressStateDoesNotCreateProgress()
        {
            _root.Save.researchProgress = null;

            InvokeResearchProgressApply();

            Assert.That(_root.Save.researchProgress, Is.Null);
        }

        [Test]
        public void ApplyResearchProgressForActiveTick_MismatchedProgressStateDoesNotAccumulate()
        {
            _root.Save.researchProgress.ProjectId = "research.project.stale";

            InvokeResearchProgressApply();

            Assert.That(_root.Save.researchProgress.ProgressUnits, Is.Zero);
        }

        [Test]
        public void ApplyResearchProgressForActiveTick_DisabledConfigDoesNotAccumulate()
        {
            _root.Content.Bootstrap.researchProgressScaffold.enabled = false;

            InvokeResearchProgressApply();

            Assert.That(_root.Save.researchProgress.ProgressUnits, Is.Zero);
        }

        [Test]
        public void ApplyResearchProgressForActiveTick_LargeProgressDoesNotCompleteResearch()
        {
            _root.Save.researchProgress.ProgressUnits = 1000000d;

            InvokeResearchProgressApply();

            Assert.That(_root.Save.researchProgress.ProgressUnits, Is.EqualTo(1000001d));
            Assert.That(_root.Save.researchProgress.CompletionPending, Is.False);
        }

        [Test]
        public void ApplyResearchProgressForActiveTick_MutatesOnlySavedProgressUnits()
        {
            SaveData save = _root.Save;
            string pendingBefore = JsonUtility.ToJson(save.researchPending);
            string structureBefore = JsonUtility.ToJson(save.structureRuntime);
            string runHistoryBefore = JsonUtility.ToJson(save.runHistory);
            string offlineBefore = JsonUtility.ToJson(save.lastOfflineSummary);
            long totalTicksBefore = save.totalTicks;

            InvokeResearchProgressApply();

            Assert.That(save.researchProgress.ProgressUnits, Is.EqualTo(1d));
            Assert.That(save.researchProgress.CompletionPending, Is.False);
            Assert.That(JsonUtility.ToJson(save.researchPending), Is.EqualTo(pendingBefore));
            Assert.That(JsonUtility.ToJson(save.structureRuntime), Is.EqualTo(structureBefore));
            Assert.That(JsonUtility.ToJson(save.runHistory), Is.EqualTo(runHistoryBefore));
            Assert.That(JsonUtility.ToJson(save.lastOfflineSummary), Is.EqualTo(offlineBefore));
            Assert.That(save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
            Assert.That(save.totalTicks, Is.EqualTo(totalTicksBefore));
        }

        [Test]
        public void RefreshOfflineSummaryLines_AfterActiveTickShowsSavedAccumulatedProgressState()
        {
            InvokeSimulationTick(1);

            Assert.That(_root.ResearchProgressLine, Does.Contain("elapsedSeconds=10 delta=1 wouldComplete=False"));
            Assert.That(_root.ResearchProgressStateLine, Does.Contain("progress=1 completionPending=False"));
        }

        [Test]
        public void ApplyResearchCompletionPendingForActiveTick_EligibleStateMutatesOnlyCompletionPending()
        {
            SaveData save = _root.Save;
            save.researchProgress.ProgressUnits = 2d;
            string pendingBefore = JsonUtility.ToJson(save.researchPending);
            string structureBefore = JsonUtility.ToJson(save.structureRuntime);
            string runHistoryBefore = JsonUtility.ToJson(save.runHistory);
            string offlineBefore = JsonUtility.ToJson(save.lastOfflineSummary);
            long totalTicksBefore = save.totalTicks;

            InvokeResearchCompletionPendingApply();

            Assert.That(save.researchProgress.SlotId, Is.EqualTo("research.slot.primary"));
            Assert.That(save.researchProgress.ProjectId, Is.EqualTo("research.project.scaffold"));
            Assert.That(save.researchProgress.ProgressUnits, Is.EqualTo(2d));
            Assert.That(save.researchProgress.CompletionPending, Is.True);
            Assert.That(JsonUtility.ToJson(save.researchPending), Is.EqualTo(pendingBefore));
            Assert.That(JsonUtility.ToJson(save.structureRuntime), Is.EqualTo(structureBefore));
            Assert.That(JsonUtility.ToJson(save.runHistory), Is.EqualTo(runHistoryBefore));
            Assert.That(JsonUtility.ToJson(save.lastOfflineSummary), Is.EqualTo(offlineBefore));
            Assert.That(save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
            Assert.That(save.totalTicks, Is.EqualTo(totalTicksBefore));
        }

        [Test]
        public void HandleSimulationTick_AtCompletionPendingThresholdMarksPendingWithoutCompletingOrClearingResearch()
        {
            SaveData save = _root.Save;
            string pendingBefore = JsonUtility.ToJson(save.researchPending);
            string runHistoryBefore = JsonUtility.ToJson(save.runHistory);
            string offlineBefore = JsonUtility.ToJson(save.lastOfflineSummary);

            InvokeSimulationTick(1);
            Assert.That(save.researchProgress.CompletionPending, Is.False);

            InvokeSimulationTick(2);

            Assert.That(save.researchProgress.ProgressUnits, Is.EqualTo(2d));
            Assert.That(save.researchProgress.CompletionPending, Is.True);
            Assert.That(JsonUtility.ToJson(save.researchPending), Is.EqualTo(pendingBefore));
            Assert.That(JsonUtility.ToJson(save.runHistory), Is.EqualTo(runHistoryBefore));
            Assert.That(JsonUtility.ToJson(save.lastOfflineSummary), Is.EqualTo(offlineBefore));
            Assert.That(_root.ResearchCompletionPendingApplyLine, Does.Contain("alreadyCompletionPending=True wouldSetCompletionPending=False wouldComplete=False"));
        }

        [Test]
        public void HandleSimulationTick_AlreadyCompletionPendingDoesNotCompleteResearchOrGrantMutation()
        {
            InvokeSimulationTick(1);
            InvokeSimulationTick(2);
            string pendingBefore = JsonUtility.ToJson(_root.Save.researchPending);
            string runHistoryBefore = JsonUtility.ToJson(_root.Save.runHistory);

            InvokeSimulationTick(3);

            Assert.That(_root.Save.researchProgress.ProgressUnits, Is.EqualTo(2d));
            Assert.That(_root.Save.researchProgress.CompletionPending, Is.True);
            Assert.That(JsonUtility.ToJson(_root.Save.researchPending), Is.EqualTo(pendingBefore));
            Assert.That(JsonUtility.ToJson(_root.Save.runHistory), Is.EqualTo(runHistoryBefore));
        }

        [Test]
        public void ClearResearchPendingScaffold_ReturnsDiagnosticsToNoPendingWithoutStaleProject()
        {
            InvokeSimulationTick(1);

            Assert.That(_root.ClearResearchPendingScaffold(), Is.True);

            Assert.That(_root.ResearchProgressStateLine, Does.Contain("pending=False hasState=False slot= project="));
            Assert.That(_root.ResearchProgressStateLine, Does.Not.Contain("research.project.scaffold"));
            Assert.That(_root.ResearchCompletionPendingApplyLine, Does.Contain("pending=False hasState=False slot= project="));
            Assert.That(_root.ResearchCompletionPendingApplyLine, Does.Not.Contain("research.project.scaffold"));
        }

        private void InvokeSimulationTick(long tickIndex)
        {
            typeof(GameRoot).GetMethod("HandleSimulationTick", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(_root, new object[] { tickIndex });
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

        private void SetPrivateField(string name, object value)
        {
            typeof(GameRoot).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_root, value);
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
                        ruleSourceId = "research.completion.rule.test",
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
            map["ui.dev.offline_summary_format"] = "Offline Summary — resolved={0} error={1} seconds={2} clamped={3} wouldProcess={4} ruleSource={5}";
            map["ui.dev.research_pending_format"] = "Research Pending — pending={0} slot={1} project={2}";
            map["ui.dev.research_pending_validation_format"] = "Research Pending Validation — resolved={0} error={1} ruleSource={2}";
            map["ui.dev.research_progress_format"] = "Research Progress Preview — resolved={0} error={1} pending={2} slot={3} project={4} elapsedSeconds={5} delta={6} wouldComplete={7} ruleSource={8}";
            map["ui.dev.research_progress_state_format"] = "Research Progress State — resolved={0} error={1} pending={2} hasState={3} slot={4} project={5} progress={6} completionPending={7} matchesPending={8} ruleSource={9}";
            map["ui.dev.research_completion_eligibility_format"] = "Research Completion Eligibility — resolved={0} error={1} pending={2} hasState={3} slot={4} project={5} progress={6} required={7} remaining={8} eligible={9} wouldSetCompletionPending={10} wouldComplete={11} ruleSource={12}";
            map["ui.dev.research_completion_pending_apply_format"] = "Research Completion Pending Apply — resolved={0} error={1} pending={2} hasState={3} slot={4} project={5} progress={6} required={7} eligible={8} alreadyCompletionPending={9} wouldSetCompletionPending={10} wouldComplete={11} ruleSource={12}";
            return content;
        }

        private static SaveData BuildSave()
        {
            return new SaveData
            {
                totalTicks = 19,
                lastSavedUtcUnix = 100,
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 23d, LastProcessedTick = 11 },
                runHistory = new RunHistoryState(),
                researchPending = new ResearchPendingState
                {
                    SlotId = "research.slot.primary",
                    ProjectId = "research.project.scaffold"
                },
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
#endif
