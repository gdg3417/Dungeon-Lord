#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchProgressActiveTickIntegrationTests
    {
        private GameObject _rootObject;
        private GameRoot _root;
        private string _projectId;

        [SetUp]
        public void SetUp()
        {
            Assert.That(GameRoot.TryCreateRunSimulationService(
                File.ReadAllText("Assets/_Project/Data/Bootstrap/run_simulation_config.json"),
                File.ReadAllText("Assets/_Project/Data/Bootstrap/loot_config.json"), out RunSimulationService runService), Is.True);
            _projectId = runService.Config.MvpFirstSessionObjective.AnalysisResearchProjectId;
            _rootObject = new GameObject("ResearchProgressActiveTickIntegrationRootTest");
            _root = _rootObject.AddComponent<GameRoot>();
            SetRequiredField("<Content>k__BackingField", BuildContent(_projectId));
            SetRequiredField("<Save>k__BackingField", BuildSave(_projectId));
            SetRequiredField("_runSimulationService", runService);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_rootObject);

        [Test]
        public void ProductionActiveTick_OneAndMultipleTicksApplyOnlyPerTickDelta()
        {
            PlayerResearchActionResult first = _root.ApplyConfiguredPlayerResearchActiveTick(10);
            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.StateChanged, Is.True);
            Assert.That(first.ProgressUnits, Is.EqualTo(1d));
            Assert.That(_root.Save.researchProgress.ProgressUnits, Is.EqualTo(1d));

            _root.ApplyConfiguredPlayerResearchActiveTick(10);
            _root.ApplyConfiguredPlayerResearchActiveTick(10);
            Assert.That(_root.Save.researchProgress.ProgressUnits, Is.EqualTo(3d));
        }

        [Test]
        public void ProductionActiveTick_DoesNotDoubleCountCumulativeSessionTime()
        {
            SetRequiredField("_activeSessionTickCount", 12L);
            _root.ApplyConfiguredPlayerResearchActiveTick(10);
            Assert.That(_root.Save.researchProgress.ProgressUnits, Is.EqualTo(1d));
        }

        [Test]
        public void ProductionActiveTick_MissingMismatchedAndDisabledStateDoNotAccumulate()
        {
            _root.Save.researchProgress = null;
            Assert.That(_root.ApplyConfiguredPlayerResearchActiveTick(10).StateChanged, Is.False);
            Assert.That(_root.Save.researchProgress, Is.Null);
        }

        [Test]
        public void ProductionActiveTick_MismatchedProgressDoesNotAccumulateOrRepair()
        {
            _root.Save.researchProgress.ProjectId = "test.stale";
            string before = JsonUtility.ToJson(_root.Save.researchProgress);
            PlayerResearchActionResult result = _root.ApplyConfiguredPlayerResearchActiveTick(10);
            Assert.That(result.StateChanged, Is.False);
            Assert.That(JsonUtility.ToJson(_root.Save.researchProgress), Is.EqualTo(before));
        }

        [Test]
        public void ProductionActiveTick_DisabledProgressConfigDoesNotAccumulate()
        {
            _root.Content.Bootstrap.researchProgressScaffold.enabled = false;
            PlayerResearchActionResult result = _root.ApplyConfiguredPlayerResearchActiveTick(10);
            Assert.That(result.StateChanged, Is.False);
            Assert.That(_root.Save.researchProgress.ProgressUnits, Is.Zero);
        }

        [Test]
        public void ProductionActiveTick_AppliesCompletionPendingExactlyOnceWithoutCompletingOrClearing()
        {
            _root.Content.Bootstrap.researchCompletionEligibilityScaffold.requiredProgressUnits = 2d;
            _root.ApplyConfiguredPlayerResearchActiveTick(10);
            PlayerResearchActionResult ready = _root.ApplyConfiguredPlayerResearchActiveTick(10);
            Assert.That(ready.State, Is.EqualTo(PlayerResearchState.ReadyToClaim));
            Assert.That(ready.StateChanged, Is.True);
            Assert.That(_root.Save.researchProgress.CompletionPending, Is.True);

            string pendingBefore = JsonUtility.ToJson(_root.Save.researchPending);
            string progressBefore = JsonUtility.ToJson(_root.Save.researchProgress);
            PlayerResearchActionResult repeated = _root.ApplyConfiguredPlayerResearchActiveTick(10);
            Assert.That(repeated.StateChanged, Is.False);
            Assert.That(JsonUtility.ToJson(_root.Save.researchPending), Is.EqualTo(pendingBefore));
            Assert.That(JsonUtility.ToJson(_root.Save.researchProgress), Is.EqualTo(progressBefore));
            Assert.That(_root.Save.completedResearch.ProjectIds, Is.Null.Or.Empty);
        }

        [Test]
        public void ProductionActiveTick_AfterClearDoesNotCreateFutureProgress()
        {
            _root.Save.researchPending = null;
            _root.Save.researchProgress = null;
            PlayerResearchActionResult result = _root.ApplyConfiguredPlayerResearchActiveTick(10);
            Assert.That(result.StateChanged, Is.False);
            Assert.That(_root.Save.researchPending, Is.Null);
            Assert.That(_root.Save.researchProgress, Is.Null);
        }

        [Test]
        public void ProductionActiveTick_MutatesOnlyResearchState()
        {
            string pendingBefore = JsonUtility.ToJson(_root.Save.researchPending);
            string structureBefore = JsonUtility.ToJson(_root.Save.structureRuntime);
            string historyBefore = JsonUtility.ToJson(_root.Save.runHistory);
            string offlineBefore = JsonUtility.ToJson(_root.Save.lastOfflineSummary);
            long ticksBefore = _root.Save.totalTicks;

            _root.ApplyConfiguredPlayerResearchActiveTick(10);

            Assert.That(JsonUtility.ToJson(_root.Save.researchPending), Is.EqualTo(pendingBefore));
            Assert.That(JsonUtility.ToJson(_root.Save.structureRuntime), Is.EqualTo(structureBefore));
            Assert.That(JsonUtility.ToJson(_root.Save.runHistory), Is.EqualTo(historyBefore));
            Assert.That(JsonUtility.ToJson(_root.Save.lastOfflineSummary), Is.EqualTo(offlineBefore));
            Assert.That(_root.Save.totalTicks, Is.EqualTo(ticksBefore));
        }

        [Test]
        public void ProductionEntryPoint_IsPublicAndCannotSilentlyDisappear()
        {
            MethodInfo method = typeof(GameRoot).GetMethod(nameof(GameRoot.ApplyConfiguredPlayerResearchActiveTick), BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null);
        }

        private void SetRequiredField(string name, object value)
        {
            FieldInfo field = typeof(GameRoot).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Required production fixture field is missing: " + name);
            field.SetValue(_root, value);
        }

        private static ContentService BuildContent(string projectId)
        {
            var content = new ContentService();
            FieldInfo field = typeof(ContentService).GetField("<Bootstrap>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(content, new ContentBootstrap
            {
                tickSeconds = 10,
                researchPendingScaffold = new ResearchPendingScaffoldConfig { enabled = true, slotId = "research.slot.primary", projectId = projectId, ruleSourceId = "test.pending" },
                researchProgressScaffold = new ResearchProgressScaffoldConfig { enabled = true, ruleSourceId = "test.progress", progressPerActiveSecond = 0.1d, maxActiveSessionElapsedSeconds = 600 },
                researchCompletionEligibilityScaffold = new ResearchCompletionEligibilityScaffoldConfig { enabled = true, ruleSourceId = "test.eligibility", projectId = projectId, requiredProgressUnits = 10d },
                researchCompletionClaimScaffold = new ResearchCompletionClaimScaffoldConfig { enabled = true, ruleSourceId = "test.claim", claimAuthorityMode = PlayerResearchClaimAuthorityResolver.LocalMvpAuthorityMode },
                researchVerificationScaffold = new ResearchVerificationScaffoldConfig { enabled = true, ruleSourceId = "test.verification", verificationMode = ResearchVerificationBoundaryResolver.UnavailableVerificationMode }
            });
            return content;
        }

        private static SaveData BuildSave(string projectId) => new SaveData
        {
            totalTicks = 19,
            structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 23d, LastProcessedTick = 11 },
            runHistory = new RunHistoryState(),
            completedResearch = new CompletedResearchState(),
            researchPending = new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = projectId },
            researchProgress = new ResearchProgressState { SlotId = "research.slot.primary", ProjectId = projectId, RuleSourceIdUsed = "test.progress" },
            lastOfflineSummary = new OfflineSummary { RuleResolved = true, OfflineSecondsObserved = 60, WouldProcessOfflineProgress = false, RuleSourceIdUsed = "test.offline" }
        };
    }
}
#endif
