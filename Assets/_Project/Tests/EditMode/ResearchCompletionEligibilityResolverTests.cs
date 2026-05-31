using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchCompletionEligibilityResolverTests
    {
        private const string SlotId = "research.slot.primary";
        private const string ProjectId = "research.project.scaffold";
        private const string RuleSourceId = "research.completion_eligibility.rule.test";

        [Test]
        public void Resolve_NullPendingReturnsSafeNoEligibilitySummary()
        {
            ResearchCompletionEligibilitySummary summary = ResearchCompletionEligibilityResolver.Resolve(null, Progress(), Config());

            AssertSafeError(summary, ResearchCompletionEligibilitySummaryErrorCode.NoPendingResearch);
            Assert.That(summary.Pending, Is.False);
            Assert.That(summary.HasProgressState, Is.False);
            Assert.That(summary.SlotId, Is.Empty);
            Assert.That(summary.ProjectId, Is.Empty);
        }

        [Test]
        public void Resolve_ValidPendingWithNullProgressReturnsSafeMissingStateSummary()
        {
            ResearchCompletionEligibilitySummary summary = ResearchCompletionEligibilityResolver.Resolve(Pending(), null, Config());

            AssertSafeError(summary, ResearchCompletionEligibilitySummaryErrorCode.MissingProgressState);
            Assert.That(summary.Pending, Is.True);
            Assert.That(summary.HasProgressState, Is.False);
        }

        [Test]
        public void Resolve_ValidPendingWithEmptyDefaultProgressReturnsSafeMissingStateSummary()
        {
            ResearchCompletionEligibilitySummary summary = ResearchCompletionEligibilityResolver.Resolve(Pending(), new ResearchProgressState(), Config());

            AssertSafeError(summary, ResearchCompletionEligibilitySummaryErrorCode.MissingProgressState);
            Assert.That(summary.HasProgressState, Is.False);
        }

        [TestCase(9d, false, 1d)]
        [TestCase(10d, true, 0d)]
        [TestCase(11d, true, 0d)]
        public void Resolve_MatchingProgressReturnsResolvedEligibility(double progressUnits, bool expectedEligible, double expectedRemaining)
        {
            ResearchCompletionEligibilitySummary summary = ResearchCompletionEligibilityResolver.Resolve(Pending(), Progress(progressUnits), Config());

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.DeterministicErrorCode, Is.Zero);
            Assert.That(summary.Pending, Is.True);
            Assert.That(summary.HasProgressState, Is.True);
            Assert.That(summary.SlotId, Is.EqualTo(SlotId));
            Assert.That(summary.ProjectId, Is.EqualTo(ProjectId));
            Assert.That(summary.ProgressUnits, Is.EqualTo(progressUnits));
            Assert.That(summary.RequiredProgressUnits, Is.EqualTo(10d));
            Assert.That(summary.RemainingProgressUnits, Is.EqualTo(expectedRemaining));
            Assert.That(summary.EligibleForCompletion, Is.EqualTo(expectedEligible));
            Assert.That(summary.RuleSourceIdUsed, Is.EqualTo(RuleSourceId));
            AssertNeverMutatesCompletion(summary);
        }

        [Test]
        public void Resolve_RepeatedIdenticalInputsReturnIdenticalSummaries()
        {
            ResearchPendingState pending = Pending();
            ResearchProgressState progress = Progress(7d);
            ResearchCompletionEligibilityScaffoldConfig config = Config();

            string first = JsonUtility.ToJson(ResearchCompletionEligibilityResolver.Resolve(pending, progress, config));
            string second = JsonUtility.ToJson(ResearchCompletionEligibilityResolver.Resolve(pending, progress, config));

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Resolve_InvalidPendingReturnsDeterministicError()
        {
            AssertSafeError(ResearchCompletionEligibilityResolver.Resolve(new ResearchPendingState(), Progress(), Config()), ResearchCompletionEligibilitySummaryErrorCode.InvalidPendingState);
        }

        [Test]
        public void Resolve_MissingConfigReturnsDeterministicError()
        {
            AssertSafeError(ResearchCompletionEligibilityResolver.Resolve(Pending(), Progress(), null), ResearchCompletionEligibilitySummaryErrorCode.MissingConfig);
        }

        [Test]
        public void Resolve_DisabledConfigReturnsDeterministicError()
        {
            ResearchCompletionEligibilityScaffoldConfig config = Config();
            config.enabled = false;

            AssertSafeError(ResearchCompletionEligibilityResolver.Resolve(Pending(), Progress(), config), ResearchCompletionEligibilitySummaryErrorCode.DisabledConfig);
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

            AssertSafeError(ResearchCompletionEligibilityResolver.Resolve(Pending(), Progress(), config), ResearchCompletionEligibilitySummaryErrorCode.InvalidConfig);
        }

        [TestCase(0d)]
        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Resolve_InvalidRequiredProgressReturnsDeterministicError(double requiredProgressUnits)
        {
            ResearchCompletionEligibilityScaffoldConfig config = Config();
            config.requiredProgressUnits = requiredProgressUnits;

            AssertSafeError(ResearchCompletionEligibilityResolver.Resolve(Pending(), Progress(), config), ResearchCompletionEligibilitySummaryErrorCode.InvalidRequiredProgressUnits);
        }

        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Resolve_InvalidExistingProgressReturnsDeterministicError(double progressUnits)
        {
            AssertSafeError(ResearchCompletionEligibilityResolver.Resolve(Pending(), Progress(progressUnits), Config()), ResearchCompletionEligibilitySummaryErrorCode.InvalidProgressUnits);
        }

        [Test]
        public void Resolve_CompletionPendingStateAtRequirementReturnsResolvedEligibleNonCompletingSummary()
        {
            ResearchProgressState progress = Progress(10d);
            progress.CompletionPending = true;

            ResearchCompletionEligibilitySummary summary = ResearchCompletionEligibilityResolver.Resolve(Pending(), progress, Config());

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.DeterministicErrorCode, Is.Zero);
            Assert.That(summary.Pending, Is.True);
            Assert.That(summary.HasProgressState, Is.True);
            Assert.That(summary.ProgressUnits, Is.EqualTo(10d));
            Assert.That(summary.RequiredProgressUnits, Is.EqualTo(10d));
            Assert.That(summary.RemainingProgressUnits, Is.Zero);
            Assert.That(summary.EligibleForCompletion, Is.True);
            AssertNeverMutatesCompletion(summary);
        }

        [Test]
        public void Resolve_MismatchedSlotReturnsSafeMismatchOutput()
        {
            ResearchProgressState progress = Progress();
            progress.SlotId = "research.slot.stale";

            ResearchCompletionEligibilitySummary summary = ResearchCompletionEligibilityResolver.Resolve(Pending(), progress, Config());

            AssertSafeError(summary, ResearchCompletionEligibilitySummaryErrorCode.ProgressStateSlotMismatch);
            Assert.That(summary.SlotId, Is.EqualTo("research.slot.stale"));
        }

        [Test]
        public void Resolve_MismatchedProgressProjectReturnsSafeMismatchOutput()
        {
            ResearchProgressState progress = Progress();
            progress.ProjectId = "research.project.stale";

            ResearchCompletionEligibilitySummary summary = ResearchCompletionEligibilityResolver.Resolve(Pending(), progress, Config());

            AssertSafeError(summary, ResearchCompletionEligibilitySummaryErrorCode.ProgressStateProjectMismatch);
            Assert.That(summary.ProjectId, Is.EqualTo("research.project.stale"));
        }

        [Test]
        public void Resolve_MismatchedConfigProjectReturnsSafeMismatchOutput()
        {
            ResearchCompletionEligibilityScaffoldConfig config = Config();
            config.projectId = "research.project.stale";

            AssertSafeError(ResearchCompletionEligibilityResolver.Resolve(Pending(), Progress(), config), ResearchCompletionEligibilitySummaryErrorCode.ConfigProjectMismatch);
        }

        [Test]
        public void Resolve_DoesNotMutateSaveOrAdjacentRuntimeState()
        {
            var save = new SaveData
            {
                totalTicks = 29,
                researchPending = Pending(),
                researchProgress = Progress(12d),
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 23d, LastProcessedTick = 11 },
                runHistory = new RunHistoryState(),
                lastOfflineSummary = new OfflineSummary
                {
                    RuleResolved = true,
                    WouldProcessOfflineProgress = false,
                    RuleSourceIdUsed = "offline.summary.rule.test"
                }
            };
            string before = JsonUtility.ToJson(save);

            ResearchCompletionEligibilitySummary summary = ResearchCompletionEligibilityResolver.Resolve(save.researchPending, save.researchProgress, Config());

            Assert.That(summary.EligibleForCompletion, Is.True);
            AssertNeverMutatesCompletion(summary);
            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
            Assert.That(save.researchProgress.CompletionPending, Is.False);
            Assert.That(save.structureRuntime.Heat, Is.EqualTo(17d));
            Assert.That(save.structureRuntime.ManaReserve, Is.EqualTo(23d));
            Assert.That(save.structureRuntime.LastProcessedTick, Is.EqualTo(11));
            Assert.That(save.totalTicks, Is.EqualTo(29));
            Assert.That(save.runHistory.RecentOutcomes, Is.Empty);
            Assert.That(save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
        }

        private static ResearchPendingState Pending()
        {
            return new ResearchPendingState { SlotId = SlotId, ProjectId = ProjectId };
        }

        private static ResearchProgressState Progress(double progressUnits = 0d)
        {
            return new ResearchProgressState
            {
                SlotId = SlotId,
                ProjectId = ProjectId,
                ProgressUnits = progressUnits,
                RuleSourceIdUsed = "research.progress.rule.test"
            };
        }

        private static ResearchCompletionEligibilityScaffoldConfig Config()
        {
            return new ResearchCompletionEligibilityScaffoldConfig
            {
                enabled = true,
                projectId = ProjectId,
                requiredProgressUnits = 10d,
                ruleSourceId = RuleSourceId
            };
        }

        private static void AssertSafeError(ResearchCompletionEligibilitySummary summary, ResearchCompletionEligibilitySummaryErrorCode errorCode)
        {
            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)errorCode));
            Assert.That(summary.EligibleForCompletion, Is.False);
            Assert.That(summary.RequiredProgressUnits, Is.Zero);
            Assert.That(summary.RemainingProgressUnits, Is.Zero);
            AssertNeverMutatesCompletion(summary);
        }

        private static void AssertNeverMutatesCompletion(ResearchCompletionEligibilitySummary summary)
        {
            Assert.That(summary.WouldSetCompletionPending, Is.False);
            Assert.That(summary.WouldCompleteResearch, Is.False);
        }
    }
}
