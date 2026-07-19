#if UNITY_EDITOR
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class CompletedResearchStateResolverTests
    {
        [Test]
        public void Resolve_NullAndEmptyStates_ReturnSafeEmptySummaries()
        {
            AssertSafeEmpty(CompletedResearchStateResolver.Resolve(null));
            AssertSafeEmpty(CompletedResearchStateResolver.Resolve(new CompletedResearchState()));
            AssertSafeEmpty(CompletedResearchStateResolver.Resolve(new CompletedResearchState { ProjectIds = null }));
            AssertSafeEmpty(CompletedResearchStateResolver.Resolve(new CompletedResearchState { ProjectIds = new string[0] }));
        }

        [Test]
        public void Resolve_IgnoresWhitespaceAndCountsUniqueProjectIdsDeterministically()
        {
            var state = new CompletedResearchState
            {
                ProjectIds = new[] { "research.project.alpha", null, " ", "research.project.beta", "research.project.alpha" },
                LastCompletedProjectId = "research.project.beta",
                LastCompletionRuleSourceId = "research.completed.rule.test"
            };

            CompletedResearchStateSummary first = CompletedResearchStateResolver.Resolve(state);
            CompletedResearchStateSummary second = CompletedResearchStateResolver.Resolve(state);

            Assert.That(first.RuleResolved, Is.True);
            Assert.That(first.DeterministicErrorCode, Is.Zero);
            Assert.That(first.HasCompletedState, Is.True);
            Assert.That(first.CompletedCount, Is.EqualTo(2));
            Assert.That(first.LastCompletedProjectId, Is.EqualTo("research.project.beta"));
            Assert.That(first.LastCompletionRuleSourceId, Is.EqualTo("research.completed.rule.test"));
            Assert.That(first.RuleSourceIdUsed, Is.EqualTo("research.completed.rule.test"));
            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
        }

        [Test]
        public void Resolve_CurrentPendingOrProgressProjectAlreadyCompleted_ReportsReadOnlyPreview()
        {
            var state = new CompletedResearchState { ProjectIds = new[] { "research.project.done" } };

            CompletedResearchStateSummary pending = CompletedResearchStateResolver.Resolve(
                state,
                new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = "research.project.done" });
            CompletedResearchStateSummary progress = CompletedResearchStateResolver.Resolve(
                state,
                null,
                new ResearchProgressState { SlotId = "research.slot.primary", ProjectId = "research.project.done" });

            Assert.That(pending.CurrentPendingProjectId, Is.EqualTo("research.project.done"));
            Assert.That(pending.CurrentProjectAlreadyCompleted, Is.True);
            Assert.That(progress.CurrentProgressProjectId, Is.EqualTo("research.project.done"));
            Assert.That(progress.CurrentProjectAlreadyCompleted, Is.True);
            Assert.That(pending.WouldBlockClaimAsDuplicate, Is.False);
            Assert.That(pending.WouldGrantRewards, Is.False);
            Assert.That(pending.WouldUnlockContent, Is.False);
        }

        [Test]
        public void Resolve_DoesNotMutateSaveOrAdjacentRuntimeState()
        {
            var completed = new CompletedResearchState
            {
                ProjectIds = new[] { "research.project.done", "research.project.done" },
                LastCompletedProjectId = "research.project.done",
                LastCompletionRuleSourceId = "research.completed.rule.test"
            };
            var pending = new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = "research.project.done" };
            var progress = new ResearchProgressState { SlotId = "research.slot.primary", ProjectId = "research.project.done", ProgressUnits = 3d, CompletionPending = true };
            var save = new SaveData
            {
                totalTicks = 41,
                completedResearch = completed,
                researchPending = pending,
                researchProgress = progress,
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 23d, LastProcessedTick = 11 },
                runHistory = new RunHistoryState { NextRunSequence = 2, RecentOutcomes = new[] { new RunOutcomeRecord { RunId = "run.test" } } },
                lastOfflineSummary = new OfflineSummary { RuleResolved = true, WouldProcessOfflineProgress = false, RuleSourceIdUsed = "offline.summary.rule.test" }
            };
            string before = JsonUtility.ToJson(save);

            CompletedResearchStateResolver.Resolve(save.completedResearch, save.researchPending, save.researchProgress);

            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
            Assert.That(save.completedResearch, Is.SameAs(completed));
            Assert.That(save.researchPending, Is.SameAs(pending));
            Assert.That(save.researchProgress, Is.SameAs(progress));
            Assert.That(save.structureRuntime.Heat, Is.EqualTo(17d));
            Assert.That(save.structureRuntime.ManaReserve, Is.EqualTo(23d));
            Assert.That(save.structureRuntime.LastProcessedTick, Is.EqualTo(11));
            Assert.That(save.runHistory.RecentOutcomes[0].RunId, Is.EqualTo("run.test"));
            Assert.That(save.totalTicks, Is.EqualTo(41));
            Assert.That(save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
        }

        [Test]
        public void JsonUtility_LegacySaveWithoutCompletedResearch_LoadsSafelyAndAdditiveStateRoundTrips()
        {
            const string legacyJson = "{\"researchPending\":{\"SlotId\":\"research.slot.primary\",\"ProjectId\":\"research.project.pending\"},\"researchProgress\":{\"SlotId\":\"research.slot.primary\",\"ProjectId\":\"research.project.pending\",\"ProgressUnits\":2,\"CompletionPending\":true}}";
            SaveData legacy = JsonUtility.FromJson<SaveData>(legacyJson);

            Assert.That(legacy, Is.Not.Null);
            Assert.That(legacy.researchPending.ProjectId, Is.EqualTo("research.project.pending"));
            Assert.That(legacy.researchProgress.ProgressUnits, Is.EqualTo(2d));
            Assert.That(legacy.researchProgress.CompletionPending, Is.True);
            AssertSafeEmpty(CompletedResearchStateResolver.Resolve(legacy.completedResearch));

            legacy.completedResearch = new CompletedResearchState
            {
                ProjectIds = new[] { "research.project.done" },
                LastCompletedProjectId = "research.project.done",
                LastCompletionRuleSourceId = "research.completed.rule.test"
            };
            SaveData roundTrip = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(legacy));

            Assert.That(roundTrip.completedResearch.ProjectIds, Is.EqualTo(new[] { "research.project.done" }));
            Assert.That(roundTrip.completedResearch.LastCompletedProjectId, Is.EqualTo("research.project.done"));
            Assert.That(roundTrip.completedResearch.LastCompletionRuleSourceId, Is.EqualTo("research.completed.rule.test"));
        }

        private static void AssertSafeEmpty(CompletedResearchStateSummary summary)
        {
            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.DeterministicErrorCode, Is.Zero);
            Assert.That(summary.HasCompletedState, Is.False);
            Assert.That(summary.CompletedCount, Is.Zero);
            Assert.That(summary.LastCompletedProjectId, Is.Empty);
            Assert.That(summary.LastCompletionRuleSourceId, Is.Empty);
            Assert.That(summary.CurrentProjectAlreadyCompleted, Is.False);
            Assert.That(summary.WouldBlockClaimAsDuplicate, Is.False);
            Assert.That(summary.WouldGrantRewards, Is.False);
            Assert.That(summary.WouldUnlockContent, Is.False);
            Assert.That(summary.RuleSourceIdUsed, Is.Empty);
            Assert.That(summary.CurrentPendingProjectId, Is.Empty);
            Assert.That(summary.CurrentProgressProjectId, Is.Empty);
        }
    }
}
#endif
