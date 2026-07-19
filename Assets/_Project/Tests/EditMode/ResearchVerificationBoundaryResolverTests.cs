#if UNITY_EDITOR
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchVerificationBoundaryResolverTests
    {
        private const string SlotId = "research.slot.primary";
        private const string ProjectId = "research.project.verification";

        [Test]
        public void NoPendingResearch_ReturnsSafeNoOp()
        {
            ResearchVerificationBoundarySummary summary = Resolve(null, null);

            Assert.That(summary.Pending, Is.False);
            Assert.That(summary.HasProgressState, Is.False);
            Assert.That(summary.VerificationRequired, Is.False);
            AssertSafeBlocked(summary);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchVerificationBoundarySummaryErrorCode.NoPendingResearch));
        }

        [Test]
        public void PendingWithNoProgress_ReturnsSafeBlockedOutput()
        {
            ResearchVerificationBoundarySummary summary = Resolve(Pending(), null);

            Assert.That(summary.Pending, Is.True);
            Assert.That(summary.HasProgressState, Is.False);
            Assert.That(summary.VerificationRequired, Is.False);
            AssertSafeBlocked(summary);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchVerificationBoundarySummaryErrorCode.MissingProgressState));
        }

        [Test]
        public void PendingBelowRequirement_DoesNotRequireOrSatisfyVerification()
        {
            ResearchVerificationBoundarySummary summary = Resolve(Pending(), Progress(1d));

            Assert.That(summary.EligibleForCompletion, Is.False);
            Assert.That(summary.VerificationRequired, Is.False);
            Assert.That(summary.VerificationSatisfied, Is.False);
            AssertSafeBlocked(summary);
        }

        [TestCase(2d)]
        [TestCase(3d)]
        public void PendingAtOrAboveRequirementWithoutCompletionPending_DoesNotSatisfyVerification(double progressUnits)
        {
            ResearchVerificationBoundarySummary summary = Resolve(Pending(), Progress(progressUnits));

            Assert.That(summary.EligibleForCompletion, Is.True);
            Assert.That(summary.CompletionPending, Is.False);
            Assert.That(summary.VerificationRequired, Is.False);
            Assert.That(summary.VerificationSatisfied, Is.False);
            AssertSafeBlocked(summary);
        }

        [Test]
        public void CompletionPending_RequiresVerificationButNeverSatisfiesOrAllowsProductionClaim()
        {
            ResearchVerificationBoundarySummary summary = Resolve(Pending(), Progress(2d, completionPending: true));

            Assert.That(summary.CompletionPending, Is.True);
            Assert.That(summary.VerificationRequired, Is.True);
            Assert.That(summary.VerificationSatisfied, Is.False);
            Assert.That(summary.CanClaimProduction, Is.False);
            AssertSafeBlocked(summary);
        }

        [Test]
        public void MissingVerificationConfig_ReturnsDeterministicSafeOutput()
        {
            ResearchVerificationBoundarySummary summary = ResearchVerificationBoundaryResolver.Resolve(
                Pending(),
                Progress(2d, true),
                null,
                EligibilityConfig(),
                null);

            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchVerificationBoundarySummaryErrorCode.MissingVerificationConfig));
            Assert.That(summary.VerificationRequired, Is.True);
            AssertSafeBlocked(summary);
        }

        [Test]
        public void DisabledVerificationConfig_ReturnsDeterministicSafeOutput()
        {
            ResearchVerificationScaffoldConfig config = VerificationConfig();
            config.enabled = false;

            ResearchVerificationBoundarySummary summary = Resolve(Pending(), Progress(2d, true), verificationConfig: config);

            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchVerificationBoundarySummaryErrorCode.DisabledVerificationConfig));
            Assert.That(summary.VerificationSatisfied, Is.False);
            AssertSafeBlocked(summary);
        }

        [Test]
        public void DisabledVerificationMode_ReturnsDeterministicSafeOutput()
        {
            ResearchVerificationScaffoldConfig config = VerificationConfig("disabled");

            ResearchVerificationBoundarySummary summary = Resolve(Pending(), Progress(2d, true), verificationConfig: config);

            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchVerificationBoundarySummaryErrorCode.DisabledVerificationConfig));
            AssertSafeBlocked(summary);
        }

        [Test]
        public void UnavailableVerificationMode_ReturnsDeterministicSafeOutput()
        {
            ResearchVerificationBoundarySummary summary = Resolve(Pending(), Progress(2d, true), verificationConfig: VerificationConfig("unavailable"));

            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchVerificationBoundarySummaryErrorCode.UnavailableVerificationMode));
            Assert.That(summary.VerificationModeUsed, Is.EqualTo("unavailable"));
            Assert.That(summary.VerificationAvailable, Is.False);
            AssertSafeBlocked(summary);
        }

        [Test]
        public void LocalDevPlaceholderModeReportsAvailableButStillNeverSatisfiedOrClaimable()
        {
            ResearchVerificationBoundarySummary summary = Resolve(Pending(), Progress(2d, true), verificationConfig: VerificationConfig("localDevPlaceholder"));

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.VerificationAvailable, Is.True);
            Assert.That(summary.VerificationSatisfied, Is.False);
            AssertSafeBlocked(summary);
        }

        [TestCase("research.slot.other", ProjectId, ResearchVerificationBoundarySummaryErrorCode.ProgressStateSlotMismatch)]
        [TestCase(SlotId, "research.project.other", ResearchVerificationBoundarySummaryErrorCode.ProgressStateProjectMismatch)]
        public void MismatchedProgress_ReturnsSafeOutput(string slotId, string projectId, ResearchVerificationBoundarySummaryErrorCode expected)
        {
            ResearchProgressState progress = Progress(2d, true);
            progress.SlotId = slotId;
            progress.ProjectId = projectId;

            ResearchVerificationBoundarySummary summary = Resolve(Pending(), progress);

            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)expected));
            AssertSafeBlocked(summary);
        }

        [Test]
        public void AlreadyCompletedActiveProject_IsDuplicateSafeAndNotClaimable()
        {
            ResearchVerificationBoundarySummary summary = Resolve(
                Pending(),
                Progress(2d, true),
                completed: new CompletedResearchState { ProjectIds = new[] { ProjectId } });

            Assert.That(summary.AlreadyCompleted, Is.True);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchVerificationBoundarySummaryErrorCode.AlreadyCompleted));
            AssertSafeBlocked(summary);
        }

        [Test]
        public void RepeatedIdenticalInputs_ReturnIdenticalSummaries()
        {
            ResearchPendingState pending = Pending();
            ResearchProgressState progress = Progress(2d, true);

            string first = JsonUtility.ToJson(Resolve(pending, progress));
            string second = JsonUtility.ToJson(Resolve(pending, progress));

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Resolver_DoesNotMutateSaveResearchOrAdjacentRuntimeState()
        {
            var save = new SaveData
            {
                totalTicks = 41,
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 23d, LastProcessedTick = 11 },
                runHistory = new RunHistoryState
                {
                    NextRunSequence = 3,
                    RecentOutcomes = new[] { new RunOutcomeRecord { RunId = "run.verification.test" } }
                },
                researchPending = Pending(),
                researchProgress = Progress(2d, true),
                completedResearch = new CompletedResearchState { ProjectIds = new[] { "research.project.done" } },
                lastOfflineSummary = new OfflineSummary { OfflineSecondsObserved = 60, WouldProcessOfflineProgress = false }
            };
            string saveBefore = JsonUtility.ToJson(save);
            string pendingBefore = JsonUtility.ToJson(save.researchPending);
            string progressBefore = JsonUtility.ToJson(save.researchProgress);
            string completedBefore = JsonUtility.ToJson(save.completedResearch);

            Resolve(save.researchPending, save.researchProgress, save.completedResearch);

            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(saveBefore));
            Assert.That(JsonUtility.ToJson(save.researchPending), Is.EqualTo(pendingBefore));
            Assert.That(JsonUtility.ToJson(save.researchProgress), Is.EqualTo(progressBefore));
            Assert.That(JsonUtility.ToJson(save.completedResearch), Is.EqualTo(completedBefore));
            Assert.That(save.structureRuntime.Heat, Is.EqualTo(17d));
            Assert.That(save.structureRuntime.ManaReserve, Is.EqualTo(23d));
            Assert.That(save.runHistory.RecentOutcomes[0].RunId, Is.EqualTo("run.verification.test"));
            Assert.That(save.totalTicks, Is.EqualTo(41));
            Assert.That(save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
        }

        [Test]
        public void SafetyFlags_AreAlwaysFalseForRepresentativeInputs()
        {
            ResearchVerificationBoundarySummary[] summaries =
            {
                Resolve(null, null),
                Resolve(Pending(), null),
                Resolve(Pending(), Progress(1d)),
                Resolve(Pending(), Progress(2d, true)),
                Resolve(Pending(), Progress(2d, true), verificationConfig: VerificationConfig("localDevPlaceholder"))
            };

            foreach (ResearchVerificationBoundarySummary summary in summaries)
            {
                AssertSafeBlocked(summary);
            }
        }

        private static void AssertSafeBlocked(ResearchVerificationBoundarySummary summary)
        {
            Assert.That(summary.VerificationSatisfied, Is.False);
            Assert.That(summary.CanClaimProduction, Is.False);
            Assert.That(summary.WouldCallServer, Is.False);
            Assert.That(summary.WouldGrantRewards, Is.False);
            Assert.That(summary.WouldUnlockContent, Is.False);
            Assert.That(summary.WouldChargeCosts, Is.False);
            Assert.That(summary.WouldProcessOfflineProgress, Is.False);
        }

        private static ResearchVerificationBoundarySummary Resolve(
            ResearchPendingState pending,
            ResearchProgressState progress,
            CompletedResearchState completed = null,
            ResearchCompletionEligibilityScaffoldConfig eligibilityConfig = null,
            ResearchVerificationScaffoldConfig verificationConfig = null)
        {
            return ResearchVerificationBoundaryResolver.Resolve(
                pending,
                progress,
                completed,
                eligibilityConfig ?? EligibilityConfig(),
                verificationConfig ?? VerificationConfig());
        }

        private static ResearchPendingState Pending()
        {
            return new ResearchPendingState { SlotId = SlotId, ProjectId = ProjectId };
        }

        private static ResearchProgressState Progress(double progressUnits, bool completionPending = false)
        {
            return new ResearchProgressState
            {
                SlotId = SlotId,
                ProjectId = ProjectId,
                ProgressUnits = progressUnits,
                CompletionPending = completionPending,
                RuleSourceIdUsed = "research.progress.rule.verification.test"
            };
        }

        private static ResearchCompletionEligibilityScaffoldConfig EligibilityConfig()
        {
            return new ResearchCompletionEligibilityScaffoldConfig
            {
                enabled = true,
                ruleSourceId = "research.eligibility.rule.verification.test",
                projectId = ProjectId,
                requiredProgressUnits = 2d
            };
        }

        private static ResearchVerificationScaffoldConfig VerificationConfig(string mode = "unavailable")
        {
            return new ResearchVerificationScaffoldConfig
            {
                enabled = true,
                ruleSourceId = "research.verification.rule.test",
                verificationMode = mode
            };
        }
    }
}
#endif
