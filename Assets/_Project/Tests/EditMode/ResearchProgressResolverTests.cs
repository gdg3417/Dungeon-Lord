#if UNITY_EDITOR
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchProgressResolverTests
    {
        [Test]
        public void Resolve_IdenticalInputs_ReturnsDeterministicPreviewSummary()
        {
            ResearchPendingState pending = ValidPending();
            ResearchProgressScaffoldConfig config = ValidConfig();

            string first = JsonUtility.ToJson(ResearchProgressResolver.Resolve(pending, config, 120));
            string second = JsonUtility.ToJson(ResearchProgressResolver.Resolve(pending, config, 120));

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Resolve_NoPendingResearch_ReturnsSafeNoProgressSummary()
        {
            ResearchProgressSummary summary = ResearchProgressResolver.Resolve(null, ValidConfig(), 120);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressSummaryErrorCode.NoPendingResearch));
            Assert.That(summary.Pending, Is.False);
            Assert.That(summary.ProgressDeltaPreview, Is.Zero);
            Assert.That(summary.WouldCompleteResearch, Is.False);
        }

        [Test]
        public void Resolve_PendingResearch_ReturnsClampedResolvedPreviewOnly()
        {
            ResearchProgressSummary summary = ResearchProgressResolver.Resolve(ValidPending(), ValidConfig(), 1200);

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressSummaryErrorCode.None));
            Assert.That(summary.Pending, Is.True);
            Assert.That(summary.SlotId, Is.EqualTo("research.slot.primary"));
            Assert.That(summary.ProjectId, Is.EqualTo("research.project.test"));
            Assert.That(summary.ElapsedSecondsUsed, Is.EqualTo(600));
            Assert.That(summary.ProgressDeltaPreview, Is.EqualTo(3d));
            Assert.That(summary.WouldCompleteResearch, Is.False);
            Assert.That(summary.RuleSourceIdUsed, Is.EqualTo("research.progress.rule.test"));
        }

        [Test]
        public void Resolve_MissingConfig_ReturnsDeterministicError()
        {
            AssertError(null, 10, ResearchProgressSummaryErrorCode.MissingConfig);
        }

        [Test]
        public void Resolve_DisabledConfig_ReturnsDeterministicError()
        {
            ResearchProgressScaffoldConfig config = ValidConfig();
            config.enabled = false;

            AssertError(config, 10, ResearchProgressSummaryErrorCode.DisabledConfig);
        }

        [Test]
        public void Resolve_NegativeElapsedTime_ReturnsDeterministicError()
        {
            AssertError(ValidConfig(), -1, ResearchProgressSummaryErrorCode.InvalidElapsedTime);
        }

        [Test]
        public void Resolve_ZeroElapsedTime_IsSafeResolvedNoProgressPreview()
        {
            ResearchProgressSummary summary = ResearchProgressResolver.Resolve(ValidPending(), ValidConfig(), 0);

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.ElapsedSecondsUsed, Is.Zero);
            Assert.That(summary.ProgressDeltaPreview, Is.Zero);
            Assert.That(summary.WouldCompleteResearch, Is.False);
        }

        [TestCase("")]
        [TestCase(" ")]
        public void Resolve_InvalidPendingSlot_ReturnsSafeError(string slotId)
        {
            ResearchPendingState pending = ValidPending();
            pending.SlotId = slotId;

            ResearchProgressSummary summary = ResearchProgressResolver.Resolve(pending, ValidConfig(), 10);

            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressSummaryErrorCode.InvalidPendingState));
            Assert.That(summary.ProgressDeltaPreview, Is.Zero);
            Assert.That(summary.WouldCompleteResearch, Is.False);
        }

        [TestCase("")]
        [TestCase(" ")]
        public void Resolve_InvalidPendingProject_ReturnsSafeError(string projectId)
        {
            ResearchPendingState pending = ValidPending();
            pending.ProjectId = projectId;

            ResearchProgressSummary summary = ResearchProgressResolver.Resolve(pending, ValidConfig(), 10);

            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressSummaryErrorCode.InvalidPendingState));
            Assert.That(summary.ProgressDeltaPreview, Is.Zero);
            Assert.That(summary.WouldCompleteResearch, Is.False);
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Resolve_NonFiniteProgressCoefficient_ReturnsSafeError(double coefficient)
        {
            ResearchProgressScaffoldConfig config = ValidConfig();
            config.progressPerActiveSecond = coefficient;

            AssertError(config, 10, ResearchProgressSummaryErrorCode.InvalidConfig);
        }

        [Test]
        public void Resolve_DoesNotMutateSaveResearchHeatManaLootRunHistoryStructuresTicksOrOfflineSummary()
        {
            var save = new SaveData
            {
                totalTicks = 77,
                researchPending = ValidPending(),
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 23d, LastProcessedTick = 71 },
                runHistory = new RunHistoryState { NextRunSequence = 4 },
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
            StructureRuntimeState structuresBefore = save.structureRuntime;
            RunHistoryState historyBefore = save.runHistory;
            OfflineSummary offlineBefore = save.lastOfflineSummary;

            ResearchProgressSummary summary = ResearchProgressResolver.Resolve(save.researchPending, ValidConfig(), 120);

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.WouldCompleteResearch, Is.False);
            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
            Assert.That(save.researchPending, Is.SameAs(pendingBefore));
            Assert.That(save.structureRuntime, Is.SameAs(structuresBefore));
            Assert.That(save.runHistory, Is.SameAs(historyBefore));
            Assert.That(save.lastOfflineSummary, Is.SameAs(offlineBefore));
            Assert.That(save.totalTicks, Is.EqualTo(77));
            Assert.That(save.structureRuntime.Heat, Is.EqualTo(17d));
            Assert.That(save.structureRuntime.ManaReserve, Is.EqualTo(23d));
            Assert.That(save.runHistory.RecentOutcomes, Is.Empty);
            Assert.That(save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
        }

        private static void AssertError(ResearchProgressScaffoldConfig config, long elapsedSeconds, ResearchProgressSummaryErrorCode errorCode)
        {
            ResearchProgressSummary summary = ResearchProgressResolver.Resolve(ValidPending(), config, elapsedSeconds);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)errorCode));
            Assert.That(summary.ProgressDeltaPreview, Is.Zero);
            Assert.That(summary.WouldCompleteResearch, Is.False);
        }

        private static ResearchPendingState ValidPending()
        {
            return new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = "research.project.test" };
        }

        private static ResearchProgressScaffoldConfig ValidConfig()
        {
            return new ResearchProgressScaffoldConfig
            {
                enabled = true,
                ruleSourceId = "research.progress.rule.test",
                progressPerActiveSecond = 0.005d,
                maxActiveSessionElapsedSeconds = 600
            };
        }
    }
}
#endif
