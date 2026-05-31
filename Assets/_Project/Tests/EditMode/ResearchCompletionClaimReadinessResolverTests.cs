using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchCompletionClaimReadinessResolverTests
    {
        private const string SlotId = "research.slot.primary";
        private const string ProjectId = "research.project.scaffold";
        private const string RuleSourceId = "research.completion_eligibility.rule.test";

        [Test]
        public void Resolve_NullPendingReturnsSafeNotReadySummary()
        {
            ResearchCompletionClaimReadinessSummary summary = ResearchCompletionClaimReadinessResolver.Resolve(null, Progress(10d, true), Config());

            AssertSafeError(summary, ResearchCompletionClaimReadinessSummaryErrorCode.NoPendingResearch);
            Assert.That(summary.Pending, Is.False);
            Assert.That(summary.HasProgressState, Is.False);
            Assert.That(summary.SlotId, Is.Empty);
            Assert.That(summary.ProjectId, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Resolve_MissingProgressReturnsSafeNotReadySummary(bool emptyDefault)
        {
            ResearchProgressState progress = emptyDefault ? new ResearchProgressState() : null;
            ResearchCompletionClaimReadinessSummary summary = ResearchCompletionClaimReadinessResolver.Resolve(Pending(), progress, Config());

            AssertSafeError(summary, ResearchCompletionClaimReadinessSummaryErrorCode.MissingProgressState);
            Assert.That(summary.Pending, Is.True);
            Assert.That(summary.HasProgressState, Is.False);
        }

        [TestCase(9d, false, false, false)]
        [TestCase(10d, false, true, false)]
        [TestCase(11d, false, true, false)]
        [TestCase(10d, true, true, true)]
        [TestCase(11d, true, true, true)]
        public void Resolve_MatchingProgressReturnsResolvedReadiness(double progressUnits, bool completionPending, bool expectedEligible, bool expectedReady)
        {
            ResearchCompletionClaimReadinessSummary summary = ResearchCompletionClaimReadinessResolver.Resolve(Pending(), Progress(progressUnits, completionPending), Config());

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.DeterministicErrorCode, Is.Zero);
            Assert.That(summary.Pending, Is.True);
            Assert.That(summary.HasProgressState, Is.True);
            Assert.That(summary.SlotId, Is.EqualTo(SlotId));
            Assert.That(summary.ProjectId, Is.EqualTo(ProjectId));
            Assert.That(summary.ProgressUnits, Is.EqualTo(progressUnits));
            Assert.That(summary.RequiredProgressUnits, Is.EqualTo(10d));
            Assert.That(summary.CompletionPending, Is.EqualTo(completionPending));
            Assert.That(summary.EligibleForCompletion, Is.EqualTo(expectedEligible));
            Assert.That(summary.ReadyForClaim, Is.EqualTo(expectedReady));
            Assert.That(summary.RuleSourceIdUsed, Is.EqualTo(RuleSourceId));
            AssertNeverMutates(summary);
        }

        [Test]
        public void Resolve_RepeatedIdenticalInputsReturnIdenticalSummaries()
        {
            ResearchPendingState pending = Pending();
            ResearchProgressState progress = Progress(10d, true);
            ResearchCompletionEligibilityScaffoldConfig config = Config();

            string first = JsonUtility.ToJson(ResearchCompletionClaimReadinessResolver.Resolve(pending, progress, config));
            string second = JsonUtility.ToJson(ResearchCompletionClaimReadinessResolver.Resolve(pending, progress, config));

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Resolve_InvalidPendingReturnsDeterministicError()
        {
            AssertSafeError(ResearchCompletionClaimReadinessResolver.Resolve(new ResearchPendingState(), Progress(), Config()), ResearchCompletionClaimReadinessSummaryErrorCode.InvalidPendingState);
        }

        [Test]
        public void Resolve_MissingConfigReturnsDeterministicError()
        {
            AssertSafeError(ResearchCompletionClaimReadinessResolver.Resolve(Pending(), Progress(), null), ResearchCompletionClaimReadinessSummaryErrorCode.MissingConfig);
        }

        [Test]
        public void Resolve_DisabledConfigReturnsDeterministicError()
        {
            ResearchCompletionEligibilityScaffoldConfig config = Config();
            config.enabled = false;
            AssertSafeError(ResearchCompletionClaimReadinessResolver.Resolve(Pending(), Progress(), config), ResearchCompletionClaimReadinessSummaryErrorCode.DisabledConfig);
        }

        [TestCase(null, ProjectId)]
        [TestCase("", ProjectId)]
        [TestCase(RuleSourceId, null)]
        [TestCase(RuleSourceId, "")]
        public void Resolve_InvalidConfigReturnsDeterministicError(string ruleSourceId, string projectId)
        {
            ResearchCompletionEligibilityScaffoldConfig config = Config();
            config.ruleSourceId = ruleSourceId;
            config.projectId = projectId;
            AssertSafeError(ResearchCompletionClaimReadinessResolver.Resolve(Pending(), Progress(), config), ResearchCompletionClaimReadinessSummaryErrorCode.InvalidConfig);
        }

        [TestCase(0d)]
        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Resolve_InvalidRequiredProgressReturnsDeterministicError(double requiredProgressUnits)
        {
            ResearchCompletionEligibilityScaffoldConfig config = Config();
            config.requiredProgressUnits = requiredProgressUnits;
            AssertSafeError(ResearchCompletionClaimReadinessResolver.Resolve(Pending(), Progress(), config), ResearchCompletionClaimReadinessSummaryErrorCode.InvalidRequiredProgressUnits);
        }

        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Resolve_InvalidExistingProgressReturnsDeterministicError(double progressUnits)
        {
            AssertSafeError(ResearchCompletionClaimReadinessResolver.Resolve(Pending(), Progress(progressUnits), Config()), ResearchCompletionClaimReadinessSummaryErrorCode.InvalidProgressUnits);
        }

        [Test]
        public void Resolve_MismatchedSlotReturnsSafeMismatchOutput()
        {
            ResearchProgressState progress = Progress();
            progress.SlotId = "research.slot.other";
            AssertSafeError(ResearchCompletionClaimReadinessResolver.Resolve(Pending(), progress, Config()), ResearchCompletionClaimReadinessSummaryErrorCode.ProgressStateSlotMismatch);
        }

        [Test]
        public void Resolve_MismatchedProjectReturnsSafeMismatchOutput()
        {
            ResearchProgressState progress = Progress();
            progress.ProjectId = "research.project.other";
            AssertSafeError(ResearchCompletionClaimReadinessResolver.Resolve(Pending(), progress, Config()), ResearchCompletionClaimReadinessSummaryErrorCode.ProgressStateProjectMismatch);
        }

        [Test]
        public void Resolve_MismatchedConfigProjectReturnsSafeMismatchOutput()
        {
            ResearchCompletionEligibilityScaffoldConfig config = Config();
            config.projectId = "research.project.other";
            AssertSafeError(ResearchCompletionClaimReadinessResolver.Resolve(Pending(), Progress(), config), ResearchCompletionClaimReadinessSummaryErrorCode.ConfigProjectMismatch);
        }

        [Test]
        public void Resolve_DoesNotMutateSaveOrAdjacentRuntimeState()
        {
            var save = new SaveData
            {
                totalTicks = 29,
                structureRuntime = new StructureRuntimeState { Heat = 13d, ManaReserve = 17d, LastProcessedTick = 23 },
                runHistory = new RunHistoryState(),
                researchPending = Pending(),
                researchProgress = Progress(11d, true),
                lastOfflineSummary = new OfflineSummary { RuleResolved = true, OfflineSecondsObserved = 31, WouldProcessOfflineProgress = false, RuleSourceIdUsed = "offline.summary.rule.test" }
            };
            string before = JsonUtility.ToJson(save);

            ResearchCompletionClaimReadinessSummary summary = ResearchCompletionClaimReadinessResolver.Resolve(save.researchPending, save.researchProgress, Config());

            Assert.That(summary.ReadyForClaim, Is.True);
            AssertNeverMutates(summary);
            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
            Assert.That(save.structureRuntime.Heat, Is.EqualTo(13d));
            Assert.That(save.structureRuntime.ManaReserve, Is.EqualTo(17d));
            Assert.That(save.structureRuntime.LastProcessedTick, Is.EqualTo(23));
            Assert.That(save.totalTicks, Is.EqualTo(29));
            Assert.That(save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
        }

        private static ResearchPendingState Pending() => new ResearchPendingState { SlotId = SlotId, ProjectId = ProjectId };

        private static ResearchProgressState Progress(double units = 0d, bool completionPending = false) => new ResearchProgressState
        {
            SlotId = SlotId,
            ProjectId = ProjectId,
            ProgressUnits = units,
            CompletionPending = completionPending,
            RuleSourceIdUsed = "research.progress.rule.test"
        };

        private static ResearchCompletionEligibilityScaffoldConfig Config() => new ResearchCompletionEligibilityScaffoldConfig
        {
            enabled = true,
            ruleSourceId = RuleSourceId,
            projectId = ProjectId,
            requiredProgressUnits = 10d
        };

        private static void AssertSafeError(ResearchCompletionClaimReadinessSummary summary, ResearchCompletionClaimReadinessSummaryErrorCode errorCode)
        {
            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)errorCode));
            Assert.That(summary.ReadyForClaim, Is.False);
            AssertNeverMutates(summary);
        }

        private static void AssertNeverMutates(ResearchCompletionClaimReadinessSummary summary)
        {
            Assert.That(summary.WouldCompleteResearch, Is.False);
            Assert.That(summary.WouldGrantRewards, Is.False);
            Assert.That(summary.WouldUnlockContent, Is.False);
            Assert.That(summary.WouldClearPending, Is.False);
        }
    }
}
