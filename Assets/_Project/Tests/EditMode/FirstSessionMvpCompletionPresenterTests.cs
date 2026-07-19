#if UNITY_EDITOR
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using System.Collections.Generic;

namespace DungeonBuilder.Tests.EditMode
{
    public class FirstSessionMvpCompletionPresenterTests
    {
        [Test]
        public void Resolve_NoSummaryOrUnresolvedSummary_UsesNotStartedStatusWithoutMutationFlags()
        {
            FirstSessionMvpCompletionSummary missing = FirstSessionMvpCompletionPresenter.Resolve(null, CompleteGuidedPath());
            FirstSessionMvpCompletionSummary unresolved = FirstSessionMvpCompletionPresenter.Resolve(new MvpPlayerLoopSummary { RuleResolved = false }, CompleteGuidedPath());

            Assert.That(missing.StatusKey, Is.EqualTo(FirstSessionMvpCompletionPresenter.NotStartedKey));
            Assert.That(missing.IsComplete, Is.False);
            Assert.That(unresolved.StatusKey, Is.EqualTo(FirstSessionMvpCompletionPresenter.NotStartedKey));
            AssertReadOnly(missing);
            AssertReadOnly(unresolved);
        }

        [Test]
        public void Resolve_NoPlacement_UsesPlaceStructureStatus()
        {
            FirstSessionMvpCompletionSummary result = FirstSessionMvpCompletionPresenter.Resolve(
                new MvpPlayerLoopSummary
                {
                    RuleResolved = true,
                    HasPlacementContext = false,
                    HasRunOutcome = false,
                    HasResearchStatus = false
                },
                GuidedPath(GuidedMvpActionPathPresenter.StepPlaceOrModifyStructureId, GuidedMvpActionPathPresenter.ActionPlaceStructureKey));

            Assert.That(result.StatusKey, Is.EqualTo(FirstSessionMvpCompletionPresenter.PlaceStructureKey));
            Assert.That(result.IsComplete, Is.False);
            AssertReadOnly(result);
        }

        [Test]
        public void Resolve_PlacementExistsButNoRun_UsesRunDungeonStatus()
        {
            FirstSessionMvpCompletionSummary result = FirstSessionMvpCompletionPresenter.Resolve(
                new MvpPlayerLoopSummary
                {
                    RuleResolved = true,
                    HasPlacementContext = true,
                    SelectedStructureId = StructureSimulationPass.ManaGeneratorBasicId,
                    HasRunOutcome = false,
                    HasResearchStatus = true
                },
                GuidedPath(GuidedMvpActionPathPresenter.StepRunOrObserveId, GuidedMvpActionPathPresenter.ActionRunDungeonKey));

            Assert.That(result.StatusKey, Is.EqualTo(FirstSessionMvpCompletionPresenter.RunDungeonKey));
            Assert.That(result.IsComplete, Is.False);
            AssertReadOnly(result);
        }

        [Test]
        public void Resolve_PlacementAndRunExistButResearchUnavailable_UsesObserveSummaryStatus()
        {
            FirstSessionMvpCompletionSummary result = FirstSessionMvpCompletionPresenter.Resolve(
                new MvpPlayerLoopSummary
                {
                    RuleResolved = true,
                    HasPlacementContext = true,
                    SelectedStructureId = StructureSimulationPass.ManaGeneratorBasicId,
                    HasRunOutcome = true,
                    HasResearchStatus = false,
                    ManaReserve = 10d,
                    LootGeneratedWorldValue = 3,
                    HeatAfter = 4d
                },
                CompleteGuidedPath());

            Assert.That(result.StatusKey, Is.EqualTo(FirstSessionMvpCompletionPresenter.ObserveSummaryKey));
            Assert.That(result.IsComplete, Is.False);
            AssertReadOnly(result);
        }

        [Test]
        public void Resolve_CompletedFirstSessionLoop_UsesCompleteStatus()
        {
            FirstSessionMvpCompletionSummary result = FirstSessionMvpCompletionPresenter.Resolve(CompleteLoopSummary(), CompleteGuidedPath());

            Assert.That(result.StatusKey, Is.EqualTo(FirstSessionMvpCompletionPresenter.CompleteKey));
            Assert.That(result.IsComplete, Is.True);
            AssertReadOnly(result);
        }

        [Test]
        public void Resolve_CoherentNextActionAfterFirstRun_CanCompleteWhenSummarySignalsAreVisible()
        {
            FirstSessionMvpCompletionSummary result = FirstSessionMvpCompletionPresenter.Resolve(
                CompleteLoopSummary(),
                GuidedPath(GuidedMvpActionPathPresenter.StepImproveSurvivabilityOrLayoutId, GuidedMvpActionPathPresenter.ActionImproveSurvivabilityOrLayoutKey));

            Assert.That(result.StatusKey, Is.EqualTo(FirstSessionMvpCompletionPresenter.CompleteKey));
            Assert.That(result.IsComplete, Is.True);
        }

        [Test]
        public void Resolve_NoGuidedPath_UsesObserveSummaryUntilNextActionIsCoherent()
        {
            FirstSessionMvpCompletionSummary result = FirstSessionMvpCompletionPresenter.Resolve(CompleteLoopSummary(), null);

            Assert.That(result.StatusKey, Is.EqualTo(FirstSessionMvpCompletionPresenter.ObserveSummaryKey));
            Assert.That(result.IsComplete, Is.False);
        }

        [Test]
        public void BuildStatusLine_UsesLocalizationKeyAndDoesNotShowRawStructureId()
        {
            var requestedKeys = new List<string>();
            MvpPlayerLoopSummary loopSummary = CompleteLoopSummary();

            string text = FirstSessionMvpCompletionPresenter.BuildStatusLine(
                loopSummary,
                CompleteGuidedPath(),
                (key, fallback) =>
                {
                    requestedKeys.Add(key);
                    return "LOC[" + key + "]";
                });

            Assert.That(text, Is.EqualTo("LOC[ui.first_session.status.complete]"));
            Assert.That(requestedKeys, Does.Contain(FirstSessionMvpCompletionPresenter.CompleteKey));
            Assert.That(text, Does.Not.Contain(loopSummary.SelectedStructureId));
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.ManaGeneratorBasicId));
        }

        private static MvpPlayerLoopSummary CompleteLoopSummary()
        {
            return new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasPlacementContext = true,
                SelectedStructureId = StructureSimulationPass.ManaGeneratorBasicId,
                HasRunOutcome = true,
                LatestRunId = "run.first_session",
                RunSucceeded = true,
                ManaReserve = 12d,
                LootGeneratedWorldValue = 8,
                LootExtractedWorldValue = 5,
                LootExtractedTradeableWorldValue = 3,
                HeatBefore = 1d,
                HeatAfter = 2d,
                HeatTierId = CurrentHeatTierResolver.PeaceTierId,
                HasResearchStatus = true,
                ResearchStatusKey = "ui.research.status.active_in_progress"
            };
        }

        private static GuidedMvpActionPathSummary CompleteGuidedPath()
        {
            return GuidedPath(GuidedMvpActionPathPresenter.StepRepeatOrImproveId, GuidedMvpActionPathPresenter.ActionRepeatOrImproveKey, isComplete: true);
        }

        private static GuidedMvpActionPathSummary GuidedPath(string stepId, string nextActionKey, bool isComplete = false)
        {
            return new GuidedMvpActionPathSummary
            {
                RuleResolved = true,
                CurrentStepId = stepId,
                CurrentStepStatusKey = GuidedMvpActionPathPresenter.StatusRepeatOrImproveKey,
                NextActionKey = nextActionKey,
                IsComplete = isComplete
            };
        }

        private static void AssertReadOnly(FirstSessionMvpCompletionSummary summary)
        {
            Assert.That(summary.WouldMutateState, Is.False);
            Assert.That(summary.WouldGrantRewards, Is.False);
            Assert.That(summary.WouldUnlockContent, Is.False);
            Assert.That(summary.WouldChargeCosts, Is.False);
            Assert.That(summary.WouldCallServer, Is.False);
            Assert.That(summary.WouldProcessOfflineResearch, Is.False);
            Assert.That(summary.WouldProcessOfflineHeat, Is.False);
            Assert.That(summary.WouldStartRaid, Is.False);
        }
    }
}
#endif
