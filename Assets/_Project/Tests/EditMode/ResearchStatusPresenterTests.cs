#if UNITY_EDITOR
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchStatusPresenterTests
    {
        private const string SlotId = "research.slot.primary";
        private const string ProjectId = "research.project.presenter";

        [Test]
        public void NoPendingResearch_ReturnsNoResearch()
        {
            Assert.That(Present().State, Is.EqualTo(ResearchStatusPresentationState.NoResearch));
        }

        [Test]
        public void PendingWithoutProgress_ReturnsBlockedOrInvalid()
        {
            Assert.That(Present(Pending()).State, Is.EqualTo(ResearchStatusPresentationState.BlockedOrInvalid));
        }

        [TestCase(1d, false, ResearchStatusPresentationState.ActiveInProgress)]
        [TestCase(2d, false, ResearchStatusPresentationState.ActiveCompletionPending)]
        [TestCase(3d, false, ResearchStatusPresentationState.ActiveCompletionPending)]
        [TestCase(2d, true, ResearchStatusPresentationState.VerificationRequired)]
        public void ValidPending_ReturnsExpectedReadOnlyPresentation(double progressUnits, bool completionPending, ResearchStatusPresentationState expected)
        {
            ResearchStatusPresentation presentation = Present(Pending(), Progress(progressUnits, completionPending));

            Assert.That(presentation.State, Is.EqualTo(expected));
            Assert.That(presentation.CanClaimProduction, Is.False);
            Assert.That(presentation.WouldGrantRewards, Is.False);
            Assert.That(presentation.WouldUnlockContent, Is.False);
            Assert.That(presentation.WouldChargeCosts, Is.False);
            Assert.That(presentation.WouldProcessOfflineProgress, Is.False);
        }

        [Test]
        public void EveryReachableState_KeepsProductionSideEffectsDisabled()
        {
            ResearchCompletionEligibilityScaffoldConfig invalidConfig = Config();
            invalidConfig.enabled = false;
            ResearchStatusPresentation[] presentations =
            {
                Present(),
                Present(Pending(), Progress(1d)),
                Present(Pending(), Progress(2d)),
                Present(Pending(), Progress(2d, true)),
                Present(completed: new CompletedResearchState { ProjectIds = new[] { ProjectId } }),
                Present(Pending(), Progress(), config: invalidConfig)
            };

            foreach (ResearchStatusPresentation presentation in presentations)
            {
                Assert.That(presentation.CanClaimProduction, Is.False);
                Assert.That(presentation.ReadyToClaim, Is.False);
                Assert.That(presentation.WouldGrantRewards, Is.False);
                Assert.That(presentation.WouldUnlockContent, Is.False);
                Assert.That(presentation.WouldChargeCosts, Is.False);
                Assert.That(presentation.WouldProcessOfflineProgress, Is.False);
            }
        }

        [Test]
        public void CompletionPending_RequiresVerificationAndNeverBecomesProductionClaimable()
        {
            ResearchStatusPresentation presentation = Present(Pending(), Progress(2d, true));

            Assert.That(presentation.VerificationRequired, Is.True);
            Assert.That(presentation.ReadyToClaim, Is.False);
            Assert.That(presentation.CanClaimProduction, Is.False);
        }

        [Test]
        public void CompletedConfiguredProjectWithoutPending_ReturnsCompleted()
        {
            ResearchStatusPresentation presentation = Present(completed: new CompletedResearchState
            {
                ProjectIds = new[] { ProjectId },
                LastCompletedProjectId = ProjectId
            });

            Assert.That(presentation.State, Is.EqualTo(ResearchStatusPresentationState.Completed));
            Assert.That(presentation.Completed, Is.True);
            Assert.That(presentation.ProjectId, Is.EqualTo(ProjectId));
        }

        [Test]
        public void InvalidConfig_ReturnsBlockedOrInvalid()
        {
            ResearchCompletionEligibilityScaffoldConfig config = Config();
            config.requiredProgressUnits = 0d;

            Assert.That(Present(Pending(), Progress(), config: config).State, Is.EqualTo(ResearchStatusPresentationState.BlockedOrInvalid));
        }

        [TestCase("research.slot.other", ProjectId)]
        [TestCase(SlotId, "research.project.other")]
        public void MismatchedProgressState_ReturnsBlockedOrInvalid(string progressSlotId, string progressProjectId)
        {
            ResearchProgressState progress = Progress();
            progress.SlotId = progressSlotId;
            progress.ProjectId = progressProjectId;

            Assert.That(Present(Pending(), progress).State, Is.EqualTo(ResearchStatusPresentationState.BlockedOrInvalid));
        }

        [Test]
        public void ActivePendingProjectAlreadyCompleted_ReturnsBlockedWithoutProductionSideEffects()
        {
            ResearchPendingState pending = Pending();
            ResearchProgressState progress = Progress(1d);
            CompletedResearchState completed = new CompletedResearchState { ProjectIds = new[] { ProjectId } };
            string pendingBefore = JsonUtility.ToJson(pending);
            string progressBefore = JsonUtility.ToJson(progress);
            string completedBefore = JsonUtility.ToJson(completed);

            ResearchStatusPresentation first = Present(pending, progress, completed);
            ResearchStatusPresentation second = Present(pending, progress, completed);

            AssertBlockedWithoutProductionSideEffects(first);
            Assert.That(first.State, Is.Not.EqualTo(ResearchStatusPresentationState.ActiveInProgress));
            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
            Assert.That(JsonUtility.ToJson(pending), Is.EqualTo(pendingBefore));
            Assert.That(JsonUtility.ToJson(progress), Is.EqualTo(progressBefore));
            Assert.That(JsonUtility.ToJson(completed), Is.EqualTo(completedBefore));
        }

        [Test]
        public void ActiveProgressProjectAlreadyCompleted_ReturnsBlockedWithoutProductionSideEffects()
        {
            ResearchProgressState progress = Progress(1d);
            progress.ProjectId = "research.project.completed.progress";
            CompletedResearchState completed = new CompletedResearchState
            {
                ProjectIds = new[] { progress.ProjectId }
            };

            ResearchStatusPresentation presentation = Present(Pending(), progress, completed);

            AssertBlockedWithoutProductionSideEffects(presentation);
            Assert.That(presentation.State, Is.Not.EqualTo(ResearchStatusPresentationState.ActiveInProgress));
        }

        [Test]
        public void IdenticalInputs_ReturnIdenticalPresentation()
        {
            ResearchPendingState pending = Pending();
            ResearchProgressState progress = Progress(1d);
            CompletedResearchState completed = new CompletedResearchState { ProjectIds = new[] { "research.project.done" } };

            string first = JsonUtility.ToJson(Present(pending, progress, completed));
            string second = JsonUtility.ToJson(Present(pending, progress, completed));

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Present_DoesNotMutateSaveOrUnrelatedRuntimeState()
        {
            var save = new SaveData
            {
                totalTicks = 41,
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 23d, LastProcessedTick = 11 },
                runHistory = new RunHistoryState
                {
                    NextRunSequence = 3,
                    RecentOutcomes = new[] { new RunOutcomeRecord { RunId = "run.presenter.test" } }
                },
                researchPending = Pending(),
                researchProgress = Progress(2d, true),
                completedResearch = new CompletedResearchState { ProjectIds = new[] { "research.project.done" } },
                lastOfflineSummary = new OfflineSummary { OfflineSecondsObserved = 60, WouldProcessOfflineProgress = false }
            };
            string before = JsonUtility.ToJson(save);

            ResearchStatusPresenter.Present(save.researchPending, save.researchProgress, save.completedResearch, Config());

            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
            Assert.That(save.structureRuntime.Heat, Is.EqualTo(17d));
            Assert.That(save.structureRuntime.ManaReserve, Is.EqualTo(23d));
            Assert.That(save.totalTicks, Is.EqualTo(41));
            Assert.That(save.runHistory.RecentOutcomes[0].RunId, Is.EqualTo("run.presenter.test"));
            Assert.That(save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
        }

        [Test]
        public void StaleProgressWithoutPending_ReturnsBlockedWithoutStaleIds()
        {
            ResearchStatusPresentation presentation = Present(progress: Progress());

            Assert.That(presentation.State, Is.EqualTo(ResearchStatusPresentationState.BlockedOrInvalid));
            Assert.That(presentation.SlotId, Is.Empty);
            Assert.That(presentation.ProjectId, Is.Empty);
        }

        private static void AssertBlockedWithoutProductionSideEffects(ResearchStatusPresentation presentation)
        {
            Assert.That(presentation.State, Is.EqualTo(ResearchStatusPresentationState.BlockedOrInvalid));
            Assert.That(presentation.CanClaimProduction, Is.False);
            Assert.That(presentation.ReadyToClaim, Is.False);
            Assert.That(presentation.WouldGrantRewards, Is.False);
            Assert.That(presentation.WouldUnlockContent, Is.False);
            Assert.That(presentation.WouldChargeCosts, Is.False);
            Assert.That(presentation.WouldProcessOfflineProgress, Is.False);
        }

        private static ResearchStatusPresentation Present(
            ResearchPendingState pending = null,
            ResearchProgressState progress = null,
            CompletedResearchState completed = null,
            ResearchCompletionEligibilityScaffoldConfig config = null)
        {
            return ResearchStatusPresenter.Present(pending, progress, completed, config ?? Config());
        }

        private static ResearchPendingState Pending()
        {
            return new ResearchPendingState { SlotId = SlotId, ProjectId = ProjectId };
        }

        private static ResearchProgressState Progress(double progressUnits = 0d, bool completionPending = false)
        {
            return new ResearchProgressState
            {
                SlotId = SlotId,
                ProjectId = ProjectId,
                ProgressUnits = progressUnits,
                CompletionPending = completionPending,
                RuleSourceIdUsed = "research.progress.rule.presenter.test"
            };
        }

        private static ResearchCompletionEligibilityScaffoldConfig Config()
        {
            return new ResearchCompletionEligibilityScaffoldConfig
            {
                enabled = true,
                ruleSourceId = "research.completion_eligibility.rule.presenter.test",
                projectId = ProjectId,
                requiredProgressUnits = 2d
            };
        }
    }
}
#endif
