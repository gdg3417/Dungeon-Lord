#if UNITY_EDITOR
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchProgressStateResolverTests
    {
        [Test]
        public void Resolve_NullPendingAndNullProgressState_ReturnsSafeNoPendingSummary()
        {
            ResearchProgressStateSummary summary = ResearchProgressStateResolver.Resolve(null, null);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressStateSummaryErrorCode.NoPendingResearch));
            Assert.That(summary.Pending, Is.False);
            Assert.That(summary.HasProgressState, Is.False);
            Assert.That(summary.StateMatchesPending, Is.False);
            Assert.That(summary.ProjectId, Is.Empty);
        }

        [Test]
        public void Resolve_PendingWithNullProgressState_ReturnsSafeMissingStateSummary()
        {
            ResearchProgressStateSummary summary = ResearchProgressStateResolver.Resolve(ValidPending(), null);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressStateSummaryErrorCode.MissingProgressState));
            Assert.That(summary.Pending, Is.True);
            Assert.That(summary.HasProgressState, Is.False);
        }

        [Test]
        public void Resolve_PendingWithEmptyDefaultProgressState_ReturnsSafeMissingStateSummaryWithoutMutation()
        {
            ResearchPendingState pending = ValidPending();
            var progress = new ResearchProgressState();
            string before = JsonUtility.ToJson(progress);

            ResearchProgressStateSummary summary = ResearchProgressStateResolver.Resolve(pending, progress);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressStateSummaryErrorCode.MissingProgressState));
            Assert.That(summary.Pending, Is.True);
            Assert.That(summary.HasProgressState, Is.False);
            Assert.That(summary.SlotId, Is.Empty);
            Assert.That(summary.ProjectId, Is.Empty);
            Assert.That(JsonUtility.ToJson(progress), Is.EqualTo(before));
        }

        [Test]
        public void Resolve_PendingWithMatchingZeroProgressState_ReturnsResolvedSummary()
        {
            ResearchProgressStateSummary summary = ResearchProgressStateResolver.Resolve(ValidPending(), ValidProgress());

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressStateSummaryErrorCode.None));
            Assert.That(summary.Pending, Is.True);
            Assert.That(summary.HasProgressState, Is.True);
            Assert.That(summary.SlotId, Is.EqualTo("research.slot.primary"));
            Assert.That(summary.ProjectId, Is.EqualTo("research.project.test"));
            Assert.That(summary.ProgressUnits, Is.Zero);
            Assert.That(summary.CompletionPending, Is.False);
            Assert.That(summary.StateMatchesPending, Is.True);
            Assert.That(summary.RuleSourceIdUsed, Is.EqualTo("research.progress.rule.test"));
        }

        [Test]
        public void Resolve_MismatchedSlot_ReturnsDeterministicStaleSummary()
        {
            ResearchProgressState state = ValidProgress();
            state.SlotId = "research.slot.stale";

            ResearchProgressStateSummary summary = ResearchProgressStateResolver.Resolve(ValidPending(), state);

            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressStateSummaryErrorCode.ProgressStateSlotMismatch));
            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.StateMatchesPending, Is.False);
        }

        [Test]
        public void Resolve_MismatchedProject_ReturnsDeterministicStaleSummary()
        {
            ResearchProgressState state = ValidProgress();
            state.ProjectId = "research.project.stale";

            ResearchProgressStateSummary summary = ResearchProgressStateResolver.Resolve(ValidPending(), state);

            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressStateSummaryErrorCode.ProgressStateProjectMismatch));
            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.StateMatchesPending, Is.False);
        }

        [Test]
        public void Resolve_NoPendingWithExistingProgressState_DoesNotReportActiveOrStaleProgress()
        {
            ResearchProgressStateSummary summary = ResearchProgressStateResolver.Resolve(null, ValidProgress());

            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressStateSummaryErrorCode.NoPendingResearch));
            Assert.That(summary.Pending, Is.False);
            Assert.That(summary.HasProgressState, Is.False);
            Assert.That(summary.ProjectId, Is.Empty);
            Assert.That(summary.ProgressUnits, Is.Zero);
        }

        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Resolve_InvalidProgressUnits_ReturnSafeSummary(double progressUnits)
        {
            ResearchProgressState state = ValidProgress();
            state.ProgressUnits = progressUnits;

            ResearchProgressStateSummary summary = ResearchProgressStateResolver.Resolve(ValidPending(), state);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressStateSummaryErrorCode.InvalidProgressUnits));
            Assert.That(summary.StateMatchesPending, Is.True);
        }

        [Test]
        public void Resolve_CompletionPendingTrue_IsReportedWithoutActivatingCompletion()
        {
            ResearchProgressState state = ValidProgress();
            state.CompletionPending = true;

            ResearchProgressStateSummary summary = ResearchProgressStateResolver.Resolve(ValidPending(), state);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressStateSummaryErrorCode.CompletionPendingNotActive));
            Assert.That(summary.CompletionPending, Is.True);
            Assert.That(summary.StateMatchesPending, Is.True);
        }

        [Test]
        public void Resolve_RepeatedIdenticalInputs_ReturnsIdenticalSummary()
        {
            ResearchPendingState pending = ValidPending();
            ResearchProgressState progress = ValidProgress();

            string first = JsonUtility.ToJson(ResearchProgressStateResolver.Resolve(pending, progress));
            string second = JsonUtility.ToJson(ResearchProgressStateResolver.Resolve(pending, progress));

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Resolve_DoesNotMutateSaveResearchHeatManaLootRunHistoryStructuresTicksOrOfflineSummary()
        {
            var loot = new RunLootSummary { LootTableId = "loot.table.test", GeneratedItemIds = new[] { "loot.item.test" } };
            var outcome = new RunOutcomeRecord { RunId = "run.test", LootSummary = loot };
            var save = new SaveData
            {
                totalTicks = 19,
                researchPending = ValidPending(),
                researchProgress = ValidProgress(),
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 23d },
                runHistory = new RunHistoryState { LatestOutcome = outcome, RecentOutcomes = new[] { outcome } },
                lastOfflineSummary = new OfflineSummary
                {
                    RuleResolved = true,
                    OfflineSecondsObserved = 60,
                    WouldProcessOfflineProgress = false,
                    RuleSourceIdUsed = "offline.summary.rule.test"
                }
            };
            string before = JsonUtility.ToJson(save);
            ResearchPendingState pendingBefore = save.researchPending;
            ResearchProgressState progressBefore = save.researchProgress;
            StructureRuntimeState structuresBefore = save.structureRuntime;
            RunHistoryState historyBefore = save.runHistory;
            OfflineSummary offlineBefore = save.lastOfflineSummary;

            ResearchProgressStateResolver.Resolve(save.researchPending, save.researchProgress);

            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
            Assert.That(save.researchPending, Is.SameAs(pendingBefore));
            Assert.That(save.researchProgress, Is.SameAs(progressBefore));
            Assert.That(save.structureRuntime, Is.SameAs(structuresBefore));
            Assert.That(save.runHistory, Is.SameAs(historyBefore));
            Assert.That(save.runHistory.LatestOutcome, Is.SameAs(outcome));
            Assert.That(save.runHistory.LatestOutcome.LootSummary, Is.SameAs(loot));
            Assert.That(save.runHistory.LatestOutcome.LootSummary.GeneratedItemIds, Is.EqualTo(new[] { "loot.item.test" }));
            Assert.That(save.lastOfflineSummary, Is.SameAs(offlineBefore));
            Assert.That(save.structureRuntime.Heat, Is.EqualTo(17d));
            Assert.That(save.structureRuntime.ManaReserve, Is.EqualTo(23d));
            Assert.That(save.totalTicks, Is.EqualTo(19));
            Assert.That(save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
        }

        [Test]
        public void LegacySaveWithoutResearchProgress_LoadsSafelyAndPreservesResearchPending()
        {
            const string legacyJson = "{\"researchPending\":{\"SlotId\":\"research.slot.primary\",\"ProjectId\":\"research.project.test\"}}";

            SaveData save = JsonUtility.FromJson<SaveData>(legacyJson);
            ResearchProgressStateSummary summary = ResearchProgressStateResolver.Resolve(save.researchPending, save.researchProgress);

            Assert.That(save.researchPending, Is.Not.Null);
            Assert.That(save.researchPending.ProjectId, Is.EqualTo("research.project.test"));
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressStateSummaryErrorCode.MissingProgressState));
            Assert.That(summary.Pending, Is.True);
            Assert.That(summary.HasProgressState, Is.False);
            Assert.That(summary.SlotId, Is.Empty);
            Assert.That(summary.ProjectId, Is.Empty);
        }

        [Test]
        public void SaveWithResearchProgress_RoundTripsAdditively()
        {
            var save = new SaveData { researchPending = ValidPending(), researchProgress = ValidProgress() };

            SaveData loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));

            Assert.That(loaded.researchProgress, Is.Not.Null);
            Assert.That(loaded.researchProgress.SlotId, Is.EqualTo("research.slot.primary"));
            Assert.That(loaded.researchProgress.ProjectId, Is.EqualTo("research.project.test"));
            Assert.That(loaded.researchProgress.ProgressUnits, Is.Zero);
            Assert.That(loaded.researchProgress.CompletionPending, Is.False);
        }

        private static ResearchPendingState ValidPending()
        {
            return new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = "research.project.test" };
        }

        private static ResearchProgressState ValidProgress()
        {
            return new ResearchProgressState
            {
                SlotId = "research.slot.primary",
                ProjectId = "research.project.test",
                RuleSourceIdUsed = "research.progress.rule.test"
            };
        }
    }
}
#endif
