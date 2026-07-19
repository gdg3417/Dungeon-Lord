#if UNITY_EDITOR
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchCompletionPendingApplyResolverTests
    {
        [Test]
        public void Resolve_NullPendingReturnsSafeNoOp()
        {
            ResearchCompletionPendingApplySummary summary = ResearchCompletionPendingApplyResolver.Resolve(null, Progress(), Config());

            AssertError(summary, ResearchCompletionPendingApplySummaryErrorCode.NoPendingResearch);
            Assert.That(summary.Pending, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Resolve_MissingProgressReturnsSafeNoOp(bool emptyDefault)
        {
            ResearchProgressState progress = emptyDefault ? new ResearchProgressState() : null;

            ResearchCompletionPendingApplySummary summary = ResearchCompletionPendingApplyResolver.Resolve(Pending(), progress, Config());

            AssertError(summary, ResearchCompletionPendingApplySummaryErrorCode.MissingProgressState);
            Assert.That(summary.HasProgressState, Is.False);
        }

        [TestCase(1d, false, false)]
        [TestCase(2d, true, true)]
        [TestCase(3d, true, true)]
        public void Resolve_ValidProgressReturnsDeterministicEligibility(double units, bool eligible, bool wouldSet)
        {
            ResearchCompletionPendingApplySummary summary = ResearchCompletionPendingApplyResolver.Resolve(Pending(), Progress(units), Config());

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.DeterministicErrorCode, Is.Zero);
            Assert.That(summary.EligibleForCompletion, Is.EqualTo(eligible));
            Assert.That(summary.WouldSetCompletionPending, Is.EqualTo(wouldSet));
            Assert.That(summary.WouldCompleteResearch, Is.False);
        }

        [Test]
        public void Resolve_AlreadyPendingReturnsResolvedNoOp()
        {
            ResearchProgressState progress = Progress(2d);
            progress.CompletionPending = true;

            ResearchCompletionPendingApplySummary summary = ResearchCompletionPendingApplyResolver.Resolve(Pending(), progress, Config());

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.EligibleForCompletion, Is.True);
            Assert.That(summary.AlreadyCompletionPending, Is.True);
            Assert.That(summary.WouldSetCompletionPending, Is.False);
            Assert.That(summary.WouldCompleteResearch, Is.False);
        }

        [Test]
        public void Resolve_RepeatedInputsReturnIdenticalSummaries()
        {
            ResearchPendingState pending = Pending();
            ResearchProgressState progress = Progress(2d);
            ResearchCompletionEligibilityScaffoldConfig config = Config();

            string first = JsonUtility.ToJson(ResearchCompletionPendingApplyResolver.Resolve(pending, progress, config));
            string second = JsonUtility.ToJson(ResearchCompletionPendingApplyResolver.Resolve(pending, progress, config));

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Resolve_InvalidPendingReturnsDeterministicError()
        {
            ResearchPendingState pending = Pending();
            pending.ProjectId = string.Empty;

            AssertError(ResearchCompletionPendingApplyResolver.Resolve(pending, Progress(), Config()), ResearchCompletionPendingApplySummaryErrorCode.InvalidPendingState);
        }

        [Test]
        public void Resolve_MissingConfigReturnsDeterministicError()
        {
            AssertError(ResearchCompletionPendingApplyResolver.Resolve(Pending(), Progress(), null), ResearchCompletionPendingApplySummaryErrorCode.MissingConfig);
        }

        [Test]
        public void Resolve_DisabledConfigReturnsDeterministicError()
        {
            ResearchCompletionEligibilityScaffoldConfig config = Config();
            config.enabled = false;

            AssertError(ResearchCompletionPendingApplyResolver.Resolve(Pending(), Progress(), config), ResearchCompletionPendingApplySummaryErrorCode.DisabledConfig);
        }

        [TestCase(null, "research.project.scaffold")]
        [TestCase("", "research.project.scaffold")]
        [TestCase("research.completion.rule.test", null)]
        public void Resolve_InvalidConfigReturnsDeterministicError(string ruleSourceId, string projectId)
        {
            ResearchCompletionEligibilityScaffoldConfig config = Config();
            config.ruleSourceId = ruleSourceId;
            config.projectId = projectId;

            AssertError(ResearchCompletionPendingApplyResolver.Resolve(Pending(), Progress(), config), ResearchCompletionPendingApplySummaryErrorCode.InvalidConfig);
        }

        [TestCase(0d)]
        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Resolve_InvalidRequiredProgressReturnsDeterministicError(double required)
        {
            ResearchCompletionEligibilityScaffoldConfig config = Config();
            config.requiredProgressUnits = required;

            AssertError(ResearchCompletionPendingApplyResolver.Resolve(Pending(), Progress(), config), ResearchCompletionPendingApplySummaryErrorCode.InvalidRequiredProgressUnits);
        }

        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Resolve_InvalidProgressReturnsDeterministicError(double units)
        {
            AssertError(ResearchCompletionPendingApplyResolver.Resolve(Pending(), Progress(units), Config()), ResearchCompletionPendingApplySummaryErrorCode.InvalidProgressUnits);
        }

        [Test]
        public void Resolve_MismatchedSlotReturnsSafeNoOp()
        {
            ResearchProgressState progress = Progress();
            progress.SlotId = "research.slot.stale";

            AssertError(ResearchCompletionPendingApplyResolver.Resolve(Pending(), progress, Config()), ResearchCompletionPendingApplySummaryErrorCode.ProgressStateSlotMismatch);
        }

        [Test]
        public void Resolve_MismatchedProjectReturnsSafeNoOp()
        {
            ResearchProgressState progress = Progress();
            progress.ProjectId = "research.project.stale";

            AssertError(ResearchCompletionPendingApplyResolver.Resolve(Pending(), progress, Config()), ResearchCompletionPendingApplySummaryErrorCode.ProgressStateProjectMismatch);
        }

        [Test]
        public void Resolve_ConfigProjectMismatchReturnsSafeNoOp()
        {
            ResearchCompletionEligibilityScaffoldConfig config = Config();
            config.projectId = "research.project.stale";

            AssertError(ResearchCompletionPendingApplyResolver.Resolve(Pending(), Progress(), config), ResearchCompletionPendingApplySummaryErrorCode.ConfigProjectMismatch);
        }

        [Test]
        public void Resolve_DoesNotMutateSaveOrExistingRuntimeState()
        {
            var save = new SaveData
            {
                totalTicks = 19,
                researchPending = Pending(),
                researchProgress = Progress(2d),
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 23d, LastProcessedTick = 11 },
                runHistory = new RunHistoryState(),
                lastOfflineSummary = new OfflineSummary { WouldProcessOfflineProgress = false, OfflineSecondsObserved = 60 }
            };
            string before = JsonUtility.ToJson(save);

            ResearchCompletionPendingApplySummary summary = ResearchCompletionPendingApplyResolver.Resolve(save.researchPending, save.researchProgress, Config());

            Assert.That(summary.WouldSetCompletionPending, Is.True);
            Assert.That(summary.WouldCompleteResearch, Is.False);
            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
        }

        private static void AssertError(ResearchCompletionPendingApplySummary summary, ResearchCompletionPendingApplySummaryErrorCode expected)
        {
            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)expected));
            Assert.That(summary.WouldSetCompletionPending, Is.False);
            Assert.That(summary.WouldCompleteResearch, Is.False);
        }

        private static ResearchPendingState Pending()
        {
            return new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = "research.project.scaffold" };
        }

        private static ResearchProgressState Progress(double units = 1d)
        {
            return new ResearchProgressState
            {
                SlotId = "research.slot.primary",
                ProjectId = "research.project.scaffold",
                ProgressUnits = units,
                RuleSourceIdUsed = "research.progress.rule.test"
            };
        }

        private static ResearchCompletionEligibilityScaffoldConfig Config()
        {
            return new ResearchCompletionEligibilityScaffoldConfig
            {
                enabled = true,
                ruleSourceId = "research.completion.rule.test",
                projectId = "research.project.scaffold",
                requiredProgressUnits = 2d
            };
        }
    }
}
#endif
