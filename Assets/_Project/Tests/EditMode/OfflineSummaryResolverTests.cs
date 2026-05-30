using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using System;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class OfflineSummaryResolverTests
    {
        private const string RuleSourceId = "offline.summary.rule.test";

        [Test]
        public void Resolve_IdenticalInputs_ReturnsDeterministicReadOnlySummary()
        {
            var research = new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = "research.project.test" };
            var loot = new RunLootSummary { ResolverSuccess = true };
            var outcome = new RunOutcomeRecord { RunId = "run.test", LootSummary = loot };
            var history = new RunHistoryState { NextRunSequence = 7, LatestOutcome = outcome, RecentOutcomes = new[] { outcome } };
            var structures = new StructureRuntimeState { Heat = 12d, ManaReserve = 34d, LastProcessedTick = 56 };
            var save = new SaveData
            {
                lastSavedUtcUnix = 100,
                totalTicks = 9,
                structureRuntime = structures,
                runHistory = history,
                researchPending = research
            };
            var resolver = new OfflineSummaryResolver(new FixedTimeSource(160));

            OfflineSummary first = resolver.Resolve(save, ValidRules(maxOfflineSeconds: 100));
            OfflineSummary second = resolver.Resolve(save, ValidRules(maxOfflineSeconds: 100));

            Assert.That(first.RuleResolved, Is.True);
            Assert.That(first.DeterministicErrorCode, Is.EqualTo((int)OfflineSummaryErrorCode.None));
            Assert.That(first.OfflineSecondsObserved, Is.EqualTo(60));
            Assert.That(first.OfflineWindowClamped, Is.False);
            Assert.That(first.ResearchPending, Is.True);
            Assert.That(first.ResearchSlotId, Is.EqualTo("research.slot.primary"));
            Assert.That(first.ResearchProjectId, Is.EqualTo("research.project.test"));
            Assert.That(first.WouldProcessOfflineProgress, Is.False);
            Assert.That(first.RuleSourceIdUsed, Is.EqualTo(RuleSourceId));
            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));

            Assert.That(save.structureRuntime, Is.SameAs(structures));
            Assert.That(structures.Heat, Is.EqualTo(12d));
            Assert.That(structures.ManaReserve, Is.EqualTo(34d));
            Assert.That(structures.LastProcessedTick, Is.EqualTo(56));
            Assert.That(save.totalTicks, Is.EqualTo(9));
            Assert.That(save.runHistory, Is.SameAs(history));
            Assert.That(history.NextRunSequence, Is.EqualTo(7));
            Assert.That(history.LatestOutcome, Is.SameAs(outcome));
            Assert.That(history.RecentOutcomes, Has.Length.EqualTo(1));
            Assert.That(outcome.LootSummary, Is.SameAs(loot));
            Assert.That(save.researchPending, Is.SameAs(research));
            Assert.That(research.ProjectId, Is.EqualTo("research.project.test"));
        }

        [Test]
        public void Resolve_ElapsedBeyondConfigLimit_ReportsClampedObservedWindowWithoutProcessingProgress()
        {
            OfflineSummary summary = new OfflineSummaryResolver(new FixedTimeSource(250))
                .Resolve(new SaveData { lastSavedUtcUnix = 100 }, ValidRules(maxOfflineSeconds: 30));

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.OfflineSecondsObserved, Is.EqualTo(30));
            Assert.That(summary.OfflineWindowClamped, Is.True);
            Assert.That(summary.WouldProcessOfflineProgress, Is.False);
        }

        [Test]
        public void Resolve_MissingRules_ReturnsSafeDeterministicErrorWithoutOfflineEffects()
        {
            OfflineSummary summary = new OfflineSummaryResolver(new FixedTimeSource(160))
                .Resolve(new SaveData { lastSavedUtcUnix = 100 }, null);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)OfflineSummaryErrorCode.TimeRulesMissingOrInvalid));
            Assert.That(summary.OfflineSecondsObserved, Is.Zero);
            Assert.That(summary.WouldProcessOfflineProgress, Is.False);
        }

        [TestCase(0, 100, OfflineSummaryErrorCode.CurrentTimestampInvalid)]
        [TestCase(99, 100, OfflineSummaryErrorCode.CurrentTimestampBeforeLastKnownTimestamp)]
        [TestCase(100, 0, OfflineSummaryErrorCode.LastKnownTimestampInvalid)]
        public void Resolve_InvalidElapsedWindow_ReturnsSafeDeterministicError(long currentTimestamp, long lastSavedTimestamp, OfflineSummaryErrorCode expected)
        {
            OfflineSummary summary = new OfflineSummaryResolver(new FixedTimeSource(currentTimestamp))
                .Resolve(new SaveData { lastSavedUtcUnix = lastSavedTimestamp }, ValidRules(maxOfflineSeconds: 100));

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)expected));
            Assert.That(summary.OfflineSecondsObserved, Is.Zero);
            Assert.That(summary.WouldProcessOfflineProgress, Is.False);
        }

        [Test]
        public void Resolve_LegacySaveWithoutResearchPendingState_RemainsSafeAndReportsNoPendingResearch()
        {
            SaveData legacy = JsonUtility.FromJson<SaveData>("{\"lastSavedUtcUnix\":100}");
            string before = JsonUtility.ToJson(legacy);

            OfflineSummary summary = new OfflineSummaryResolver(new FixedTimeSource(160))
                .Resolve(legacy, ValidRules(maxOfflineSeconds: 100));

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.ResearchPending, Is.False);
            Assert.That(summary.ResearchSlotId, Is.Empty);
            Assert.That(summary.ResearchProjectId, Is.Empty);
            Assert.That(summary.WouldProcessOfflineProgress, Is.False);
            Assert.That(JsonUtility.ToJson(legacy), Is.EqualTo(before));
            Assert.That(legacy.totalTicks, Is.Zero);
            Assert.That(legacy.runHistory.RecentOutcomes, Is.Empty);
            Assert.That(legacy.structureRuntime.Heat, Is.Zero);
            Assert.That(legacy.structureRuntime.ManaReserve, Is.Zero);
        }

        [Test]
        public void Resolve_DefaultResearchPendingObject_NormalizesNullIdsWithoutMutation()
        {
            var research = new ResearchPendingState();
            var save = new SaveData { lastSavedUtcUnix = 100, researchPending = research };

            OfflineSummary summary = new OfflineSummaryResolver(new FixedTimeSource(160))
                .Resolve(save, ValidRules(maxOfflineSeconds: 100));

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.ResearchPending, Is.False);
            Assert.That(summary.ResearchSlotId, Is.Empty);
            Assert.That(summary.ResearchProjectId, Is.Empty);
            Assert.That(summary.WouldProcessOfflineProgress, Is.False);
            Assert.That(save.researchPending, Is.SameAs(research));
            Assert.That(research.SlotId, Is.Null);
            Assert.That(research.ProjectId, Is.Null);
        }

        [Test]
        public void Resolve_SingleResearchSlot_ReportsPendingWithoutCompletingOrMutatingResearch()
        {
            var research = new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = "research.project.pending" };
            var save = new SaveData { lastSavedUtcUnix = 100, researchPending = research };

            OfflineSummary summary = new OfflineSummaryResolver(new FixedTimeSource(100000))
                .Resolve(save, ValidRules(maxOfflineSeconds: 10));

            Assert.That(summary.ResearchPending, Is.True);
            Assert.That(summary.ResearchSlotId, Is.EqualTo("research.slot.primary"));
            Assert.That(summary.ResearchProjectId, Is.EqualTo("research.project.pending"));
            Assert.That(summary.WouldProcessOfflineProgress, Is.False);
            Assert.That(save.researchPending, Is.SameAs(research));
            Assert.That(research.ProjectId, Is.EqualTo("research.project.pending"));
        }

        private static TimeRules ValidRules(int maxOfflineSeconds)
        {
            return new TimeRules
            {
                maxOfflineSeconds = maxOfflineSeconds,
                offlineSummaryRuleSourceId = RuleSourceId
            };
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
