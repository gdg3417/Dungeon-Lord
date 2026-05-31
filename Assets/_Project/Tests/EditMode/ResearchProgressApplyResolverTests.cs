using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchProgressApplyResolverTests
    {
        [Test]
        public void Resolve_NullPending_ReturnsSafeNoApplySummary()
        {
            ResearchProgressApplySummary summary = ResearchProgressApplyResolver.Resolve(null, ValidProgress(), ValidConfig(), 10);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressApplySummaryErrorCode.NoPendingResearch));
            Assert.That(summary.Pending, Is.False);
            Assert.That(summary.ProgressDeltaApplied, Is.Zero);
            Assert.That(summary.WouldCompleteResearch, Is.False);
        }

        [Test]
        public void Resolve_ValidPendingWithNullProgress_ReturnsSafeMissingStateSummary()
        {
            ResearchProgressApplySummary summary = ResearchProgressApplyResolver.Resolve(ValidPending(), null, ValidConfig(), 10);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressApplySummaryErrorCode.MissingProgressState));
            Assert.That(summary.Pending, Is.True);
            Assert.That(summary.HasProgressState, Is.False);
        }

        [Test]
        public void Resolve_ValidPendingWithEmptyDefaultProgress_ReturnsSafeMissingStateSummary()
        {
            ResearchProgressApplySummary summary = ResearchProgressApplyResolver.Resolve(ValidPending(), new ResearchProgressState(), ValidConfig(), 10);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressApplySummaryErrorCode.MissingProgressState));
            Assert.That(summary.HasProgressState, Is.False);
        }

        [Test]
        public void Resolve_InvalidPending_ReturnsSafeError()
        {
            ResearchPendingState pending = ValidPending();
            pending.SlotId = string.Empty;

            ResearchProgressApplySummary summary = ResearchProgressApplyResolver.Resolve(pending, ValidProgress(), ValidConfig(), 10);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressApplySummaryErrorCode.InvalidPendingState));
            Assert.That(summary.ProgressDeltaApplied, Is.Zero);
        }

        [Test]
        public void Resolve_ValidMatchingState_ReturnsNextProgressSummaryWithoutCompletion()
        {
            ResearchProgressState progress = ValidProgress();
            progress.ProgressUnits = 4d;

            ResearchProgressApplySummary summary = ResearchProgressApplyResolver.Resolve(ValidPending(), progress, ValidConfig(), 10);

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressApplySummaryErrorCode.None));
            Assert.That(summary.Pending, Is.True);
            Assert.That(summary.HasProgressState, Is.True);
            Assert.That(summary.SlotId, Is.EqualTo("research.slot.primary"));
            Assert.That(summary.ProjectId, Is.EqualTo("research.project.test"));
            Assert.That(summary.ElapsedSecondsUsed, Is.EqualTo(10));
            Assert.That(summary.ProgressDeltaApplied, Is.EqualTo(2.5d));
            Assert.That(summary.PreviousProgressUnits, Is.EqualTo(4d));
            Assert.That(summary.NextProgressUnits, Is.EqualTo(6.5d));
            Assert.That(summary.WouldCompleteResearch, Is.False);
            Assert.That(summary.RuleSourceIdUsed, Is.EqualTo("research.progress.rule.test"));
        }

        [Test]
        public void Resolve_IdenticalInputs_ReturnsIdenticalSummary()
        {
            string first = JsonUtility.ToJson(ResearchProgressApplyResolver.Resolve(ValidPending(), ValidProgress(), ValidConfig(), 10));
            string second = JsonUtility.ToJson(ResearchProgressApplyResolver.Resolve(ValidPending(), ValidProgress(), ValidConfig(), 10));

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Resolve_MissingConfig_ReturnsDeterministicError()
        {
            AssertError(null, 10, ResearchProgressApplySummaryErrorCode.MissingConfig);
        }

        [Test]
        public void Resolve_DisabledConfig_ReturnsDeterministicError()
        {
            ResearchProgressScaffoldConfig config = ValidConfig();
            config.enabled = false;

            AssertError(config, 10, ResearchProgressApplySummaryErrorCode.DisabledConfig);
        }

        [TestCase("")]
        [TestCase(" ")]
        public void Resolve_MissingRuleSource_ReturnsInvalidConfig(string ruleSourceId)
        {
            ResearchProgressScaffoldConfig config = ValidConfig();
            config.ruleSourceId = ruleSourceId;

            AssertError(config, 10, ResearchProgressApplySummaryErrorCode.InvalidConfig);
        }

        [Test]
        public void Resolve_NegativeConfiguredPreviewCap_ReturnsInvalidConfig()
        {
            ResearchProgressScaffoldConfig config = ValidConfig();
            config.maxActiveSessionElapsedSeconds = -1;

            AssertError(config, 10, ResearchProgressApplySummaryErrorCode.InvalidConfig);
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(-0.25d)]
        public void Resolve_InvalidCoefficient_ReturnsInvalidConfig(double coefficient)
        {
            ResearchProgressScaffoldConfig config = ValidConfig();
            config.progressPerActiveSecond = coefficient;

            AssertError(config, 10, ResearchProgressApplySummaryErrorCode.InvalidConfig);
        }

        [Test]
        public void Resolve_ZeroElapsedSeconds_ResolvesWithZeroDelta()
        {
            ResearchProgressApplySummary summary = ResearchProgressApplyResolver.Resolve(ValidPending(), ValidProgress(), ValidConfig(), 0);

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.ElapsedSecondsUsed, Is.Zero);
            Assert.That(summary.ProgressDeltaApplied, Is.Zero);
            Assert.That(summary.PreviousProgressUnits, Is.EqualTo(summary.NextProgressUnits));
        }

        [Test]
        public void Resolve_NegativeElapsedSeconds_ReturnsSafeNoDeltaError()
        {
            ResearchProgressApplySummary summary = ResearchProgressApplyResolver.Resolve(ValidPending(), ValidProgress(), ValidConfig(), -1);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressApplySummaryErrorCode.InvalidElapsedTime));
            Assert.That(summary.ProgressDeltaApplied, Is.Zero);
            Assert.That(summary.PreviousProgressUnits, Is.EqualTo(summary.NextProgressUnits));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(-1d)]
        public void Resolve_InvalidExistingProgressUnits_ReturnSafeError(double progressUnits)
        {
            ResearchProgressState progress = ValidProgress();
            progress.ProgressUnits = progressUnits;

            ResearchProgressApplySummary summary = ResearchProgressApplyResolver.Resolve(ValidPending(), progress, ValidConfig(), 10);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressApplySummaryErrorCode.InvalidProgressUnits));
            Assert.That(summary.ProgressDeltaApplied, Is.Zero);
            Assert.That(summary.WouldCompleteResearch, Is.False);
        }

        [Test]
        public void Resolve_OverflowingDelta_ReturnsSafeError()
        {
            ResearchProgressScaffoldConfig config = ValidConfig();
            config.progressPerActiveSecond = double.MaxValue;

            AssertError(config, 10, ResearchProgressApplySummaryErrorCode.InvalidProgressDelta);
        }

        [Test]
        public void Resolve_MismatchedSlot_ReturnsSafeMismatch()
        {
            ResearchProgressState progress = ValidProgress();
            progress.SlotId = "research.slot.stale";

            ResearchProgressApplySummary summary = ResearchProgressApplyResolver.Resolve(ValidPending(), progress, ValidConfig(), 10);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressApplySummaryErrorCode.ProgressStateSlotMismatch));
            Assert.That(summary.ProgressDeltaApplied, Is.Zero);
        }

        [Test]
        public void Resolve_MismatchedProject_ReturnsSafeMismatch()
        {
            ResearchProgressState progress = ValidProgress();
            progress.ProjectId = "research.project.stale";

            ResearchProgressApplySummary summary = ResearchProgressApplyResolver.Resolve(ValidPending(), progress, ValidConfig(), 10);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressApplySummaryErrorCode.ProgressStateProjectMismatch));
            Assert.That(summary.ProgressDeltaApplied, Is.Zero);
        }

        [Test]
        public void Resolve_CompletionPending_ReturnsSafeNoApplySummary()
        {
            ResearchProgressState progress = ValidProgress();
            progress.CompletionPending = true;

            ResearchProgressApplySummary summary = ResearchProgressApplyResolver.Resolve(ValidPending(), progress, ValidConfig(), 10);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchProgressApplySummaryErrorCode.CompletionPendingNotActive));
            Assert.That(summary.WouldCompleteResearch, Is.False);
        }

        [Test]
        public void Resolve_DoesNotMutateSaveOrAnyNestedRuntimeState()
        {
            SaveData save = BuildSave();
            string before = JsonUtility.ToJson(save);

            ResearchProgressApplySummary summary = ResearchProgressApplyResolver.Resolve(save.researchPending, save.researchProgress, ValidConfig(), 10);

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
        }

        private static void AssertError(ResearchProgressScaffoldConfig config, long elapsedSeconds, ResearchProgressApplySummaryErrorCode expected)
        {
            ResearchProgressApplySummary summary = ResearchProgressApplyResolver.Resolve(ValidPending(), ValidProgress(), config, elapsedSeconds);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)expected));
            Assert.That(summary.ProgressDeltaApplied, Is.Zero);
            Assert.That(summary.WouldCompleteResearch, Is.False);
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
                ProgressUnits = 1d,
                RuleSourceIdUsed = "research.progress.rule.saved"
            };
        }

        private static ResearchProgressScaffoldConfig ValidConfig()
        {
            return new ResearchProgressScaffoldConfig
            {
                enabled = true,
                ruleSourceId = "research.progress.rule.test",
                progressPerActiveSecond = 0.25d
            };
        }

        private static SaveData BuildSave()
        {
            return new SaveData
            {
                totalTicks = 19,
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 23d, LastProcessedTick = 11 },
                runHistory = new RunHistoryState(),
                researchPending = ValidPending(),
                researchProgress = ValidProgress(),
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
