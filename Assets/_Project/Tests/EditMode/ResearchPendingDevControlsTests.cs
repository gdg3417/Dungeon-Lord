using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchPendingDevControlsTests
    {
        private GameObject _rootObject;
        private GameRoot _root;

        [SetUp]
        public void SetUp()
        {
            _rootObject = new GameObject("ResearchPendingDevControlsRootTest");
            _root = _rootObject.AddComponent<GameRoot>();
            SetBackingField("<Content>k__BackingField", BuildContent(includeFormats: true));
            SetBackingField("<Save>k__BackingField", BuildSave());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rootObject);
        }

        [Test]
        public void SetResearchPendingScaffold_WritesOnlyResearchStateAndRefreshesDiagnostics()
        {
            SaveData save = _root.Save;
            string beforeWithoutResearch = SerializeWithoutResearchState(save);

            bool didSet = _root.SetResearchPendingScaffold();

            Assert.That(didSet, Is.True);
            Assert.That(save.researchPending, Is.Not.Null);
            Assert.That(save.researchPending.SlotId, Is.EqualTo("research.slot.primary"));
            Assert.That(save.researchPending.ProjectId, Is.EqualTo("research.project.scaffold"));
            Assert.That(save.researchProgress, Is.Not.Null);
            Assert.That(save.researchProgress.SlotId, Is.EqualTo("research.slot.primary"));
            Assert.That(save.researchProgress.ProjectId, Is.EqualTo("research.project.scaffold"));
            Assert.That(save.researchProgress.ProgressUnits, Is.Zero);
            Assert.That(save.researchProgress.CompletionPending, Is.False);
            Assert.That(save.researchProgress.RuleSourceIdUsed, Is.EqualTo("research.progress.rule.test"));
            Assert.That(SerializeWithoutResearchState(save), Is.EqualTo(beforeWithoutResearch));
            Assert.That(_root.ResearchPendingLine, Does.Contain("pending=True slot=research.slot.primary project=research.project.scaffold"));
            Assert.That(_root.ResearchPendingValidationLine, Does.Contain("resolved=True error=0 ruleSource=research.pending.rule.test"));
        }

        [Test]
        public void ClearResearchPendingScaffold_ClearsOnlyResearchStateAndRefreshesDiagnostics()
        {
            SaveData save = _root.Save;
            save.researchPending = new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = "research.project.saved" };
            save.researchProgress = new ResearchProgressState { SlotId = "research.slot.primary", ProjectId = "research.project.saved" };
            string beforeWithoutResearch = SerializeWithoutResearchState(save);

            bool didClear = _root.ClearResearchPendingScaffold();

            Assert.That(didClear, Is.True);
            Assert.That(save.researchPending, Is.Null);
            Assert.That(save.researchProgress, Is.Null);
            Assert.That(SerializeWithoutResearchState(save), Is.EqualTo(beforeWithoutResearch));
            Assert.That(_root.ResearchPendingLine, Does.Contain("pending=False slot= project="));
            Assert.That(_root.ResearchPendingValidationLine, Does.Contain("resolved=True error=0"));
        }

        [Test]
        public void SetResearchPendingScaffold_InvalidConfig_DoesNotMutateSaveAndUsesSafeLocalizationFallback()
        {
            SetBackingField("<Content>k__BackingField", BuildContent(includeFormats: false, enabled: false));
            string before = JsonUtility.ToJson(_root.Save);

            bool didSet = _root.SetResearchPendingScaffold();

            Assert.That(didSet, Is.False);
            Assert.That(JsonUtility.ToJson(_root.Save), Is.EqualTo(before));
            Assert.That(_root.ResearchPendingLine, Is.EqualTo("ui.dev.research_pending_format"));
            Assert.That(_root.ResearchPendingValidationLine, Is.EqualTo("ui.dev.research_pending_validation_format"));
        }

        [Test]
        public void SaveModel_RemainsSingleSlotScaffoldWithoutProgressOrCompletionFields()
        {
            string[] fieldNames = System.Array.ConvertAll(
                typeof(ResearchPendingState).GetFields(BindingFlags.Instance | BindingFlags.Public),
                field => field.Name);

            Assert.That(fieldNames, Is.EqualTo(new[] { "SlotId", "ProjectId" }));
        }

        private static SaveData BuildSave()
        {
            return new SaveData
            {
                totalTicks = 19,
                lastSavedUtcUnix = 100,
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 23d, LastProcessedTick = 11 },
                runHistory = new RunHistoryState(),
                lastOfflineSummary = new OfflineSummary
                {
                    RuleResolved = true,
                    OfflineSecondsObserved = 60,
                    RuleSourceIdUsed = "offline.summary.rule.test"
                }
            };
        }

        private static string SerializeWithoutResearchState(SaveData save)
        {
            ResearchPendingState researchPending = save.researchPending;
            ResearchProgressState researchProgress = save.researchProgress;
            save.researchPending = null;
            save.researchProgress = null;
            string json = JsonUtility.ToJson(save);
            save.researchPending = researchPending;
            save.researchProgress = researchProgress;
            return json;
        }

        private static ContentService BuildContent(bool includeFormats, bool enabled = true)
        {
            var content = new ContentService();
            typeof(ContentService).GetField("<Bootstrap>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(content, new ContentBootstrap
                {
                    researchPendingScaffold = new ResearchPendingScaffoldConfig
                    {
                        enabled = enabled,
                        slotId = "research.slot.primary",
                        projectId = "research.project.scaffold",
                        ruleSourceId = "research.pending.rule.test"
                    },
                    researchProgressScaffold = new ResearchProgressScaffoldConfig
                    {
                        enabled = true,
                        ruleSourceId = "research.progress.rule.test"
                    }
                });
            var map = (Dictionary<string, string>)typeof(ContentService).GetField("_stringMap", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(content);
            if (includeFormats)
            {
                map["ui.dev.research_pending_format"] = "Research Pending — pending={0} slot={1} project={2}";
                map["ui.dev.research_pending_validation_format"] = "Research Pending Validation — resolved={0} error={1} ruleSource={2}";
            }
            return content;
        }

        private void SetBackingField(string name, object value)
        {
            typeof(GameRoot).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_root, value);
        }
    }
}
