using DungeonBuilder.M0;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchCompletionClaimApplyResolverTests
    {
        private static readonly ResearchPendingState Pending = new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = "research.project.test" };
        private static readonly ResearchCompletionEligibilityScaffoldConfig Eligibility = new ResearchCompletionEligibilityScaffoldConfig { enabled = true, ruleSourceId = "eligibility.rule.test", projectId = "research.project.test", requiredProgressUnits = 10d };
        private static readonly ResearchCompletionClaimScaffoldConfig Claim = new ResearchCompletionClaimScaffoldConfig { enabled = true, ruleSourceId = "claim.rule.test" };

        [Test]
        public void Resolve_NullPendingOrMissingProgress_ReturnsSafeNoOp()
        {
            AssertNoMutation(ResearchCompletionClaimApplyResolver.Resolve(null, null, null, Eligibility, Claim), ResearchCompletionClaimApplySummaryErrorCode.NoPendingResearch);
            AssertNoMutation(ResearchCompletionClaimApplyResolver.Resolve(Pending, null, null, Eligibility, Claim), ResearchCompletionClaimApplySummaryErrorCode.MissingProgressState);
            AssertNoMutation(ResearchCompletionClaimApplyResolver.Resolve(Pending, new ResearchProgressState(), null, Eligibility, Claim), ResearchCompletionClaimApplySummaryErrorCode.MissingProgressState);
        }

        [TestCase(9d, false)]
        [TestCase(10d, false)]
        [TestCase(11d, false)]
        public void Resolve_NotReady_ReturnsResolvedNoOp(double progress, bool completionPending)
        {
            ResearchCompletionClaimApplySummary summary = ResearchCompletionClaimApplyResolver.Resolve(Pending, Progress(progress, completionPending), null, Eligibility, Claim);
            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchCompletionClaimApplySummaryErrorCode.None));
            Assert.That(summary.WouldRecordCompletedResearch, Is.False);
            Assert.That(summary.WouldClearPending, Is.False);
            Assert.That(summary.WouldClearProgress, Is.False);
            AssertSafetyFlags(summary);
        }

        [TestCase(10d)]
        [TestCase(11d)]
        public void Resolve_Ready_ReturnsWouldApplyWithoutSideEffects(double progress)
        {
            var completed = new CompletedResearchState { ProjectIds = new[] { "research.project.existing" } };
            ResearchProgressState progressState = Progress(progress, true);
            string completedBefore = JsonUtility.ToJson(completed);
            string progressBefore = JsonUtility.ToJson(progressState);

            ResearchCompletionClaimApplySummary summary = ResearchCompletionClaimApplyResolver.Resolve(Pending, progressState, completed, Eligibility, Claim);

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.WouldRecordCompletedResearch, Is.True);
            Assert.That(summary.WouldClearPending, Is.True);
            Assert.That(summary.WouldClearProgress, Is.True);
            Assert.That(summary.ReadyForClaim, Is.True);
            Assert.That(summary.RuleSourceIdUsed, Is.EqualTo("claim.rule.test"));
            AssertSafetyFlags(summary);
            Assert.That(JsonUtility.ToJson(completed), Is.EqualTo(completedBefore));
            Assert.That(JsonUtility.ToJson(progressState), Is.EqualTo(progressBefore));
        }

        [Test]
        public void Resolve_InvalidConfigsMismatchesDuplicateAndInvalidNumbers_ReturnDeterministicNoOps()
        {
            AssertNoMutation(ResearchCompletionClaimApplyResolver.Resolve(Pending, Progress(10d, true), null, null, Claim), ResearchCompletionClaimApplySummaryErrorCode.MissingEligibilityConfig);
            AssertNoMutation(ResearchCompletionClaimApplyResolver.Resolve(Pending, Progress(10d, true), null, new ResearchCompletionEligibilityScaffoldConfig(), Claim), ResearchCompletionClaimApplySummaryErrorCode.DisabledEligibilityConfig);
            AssertNoMutation(ResearchCompletionClaimApplyResolver.Resolve(Pending, Progress(10d, true), null, new ResearchCompletionEligibilityScaffoldConfig { enabled = true }, Claim), ResearchCompletionClaimApplySummaryErrorCode.InvalidEligibilityConfig);
            AssertNoMutation(ResearchCompletionClaimApplyResolver.Resolve(Pending, Progress(10d, true), null, Eligibility, null), ResearchCompletionClaimApplySummaryErrorCode.MissingClaimConfig);
            AssertNoMutation(ResearchCompletionClaimApplyResolver.Resolve(Pending, Progress(10d, true), null, Eligibility, new ResearchCompletionClaimScaffoldConfig()), ResearchCompletionClaimApplySummaryErrorCode.DisabledClaimConfig);
            AssertNoMutation(ResearchCompletionClaimApplyResolver.Resolve(Pending, Progress(10d, true), null, Eligibility, new ResearchCompletionClaimScaffoldConfig { enabled = true }), ResearchCompletionClaimApplySummaryErrorCode.InvalidClaimConfig);
            AssertNoMutation(ResearchCompletionClaimApplyResolver.Resolve(Pending, Progress(double.NaN, true), null, Eligibility, Claim), ResearchCompletionClaimApplySummaryErrorCode.InvalidProgressUnits);
            AssertNoMutation(ResearchCompletionClaimApplyResolver.Resolve(Pending, Progress(10d, true), null, new ResearchCompletionEligibilityScaffoldConfig { enabled = true, ruleSourceId = "eligibility.rule.test", projectId = "research.project.test" }, Claim), ResearchCompletionClaimApplySummaryErrorCode.InvalidRequiredProgressUnits);
            AssertNoMutation(ResearchCompletionClaimApplyResolver.Resolve(Pending, new ResearchProgressState { SlotId = "other", ProjectId = Pending.ProjectId }, null, Eligibility, Claim), ResearchCompletionClaimApplySummaryErrorCode.ProgressStateSlotMismatch);
            AssertNoMutation(ResearchCompletionClaimApplyResolver.Resolve(Pending, new ResearchProgressState { SlotId = Pending.SlotId, ProjectId = "other" }, null, Eligibility, Claim), ResearchCompletionClaimApplySummaryErrorCode.ProgressStateProjectMismatch);
            ResearchCompletionClaimApplySummary duplicate = ResearchCompletionClaimApplyResolver.Resolve(Pending, Progress(10d, true), new CompletedResearchState { ProjectIds = new[] { Pending.ProjectId } }, Eligibility, Claim);
            Assert.That(duplicate.RuleResolved, Is.True);
            Assert.That(duplicate.AlreadyCompleted, Is.True);
            Assert.That(duplicate.WouldRecordCompletedResearch, Is.False);
        }

        [Test]
        public void Resolve_RepeatedInputs_ReturnIdenticalSummaryAndNeverMutateSave()
        {
            var save = new SaveData { totalTicks = 9, researchPending = Pending, researchProgress = Progress(10d, true), completedResearch = new CompletedResearchState(), structureRuntime = new DungeonBuilder.M0.Gameplay.Structures.StructureRuntimeState { Heat = 4d, ManaReserve = 8d } };
            string before = JsonUtility.ToJson(save);
            string first = JsonUtility.ToJson(ResearchCompletionClaimApplyResolver.Resolve(save.researchPending, save.researchProgress, save.completedResearch, Eligibility, Claim));
            string second = JsonUtility.ToJson(ResearchCompletionClaimApplyResolver.Resolve(save.researchPending, save.researchProgress, save.completedResearch, Eligibility, Claim));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
        }

        private static ResearchProgressState Progress(double units, bool pending) => new ResearchProgressState { SlotId = Pending.SlotId, ProjectId = Pending.ProjectId, ProgressUnits = units, CompletionPending = pending };
        private static void AssertNoMutation(ResearchCompletionClaimApplySummary summary, ResearchCompletionClaimApplySummaryErrorCode code)
        {
            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)code));
            Assert.That(summary.WouldRecordCompletedResearch, Is.False);
            Assert.That(summary.WouldClearPending, Is.False);
            Assert.That(summary.WouldClearProgress, Is.False);
            AssertSafetyFlags(summary);
        }
        private static void AssertSafetyFlags(ResearchCompletionClaimApplySummary summary)
        {
            Assert.That(summary.WouldGrantRewards, Is.False);
            Assert.That(summary.WouldUnlockContent, Is.False);
            Assert.That(summary.WouldChargeCosts, Is.False);
            Assert.That(summary.WouldProcessOfflineProgress, Is.False);
        }
    }
}
