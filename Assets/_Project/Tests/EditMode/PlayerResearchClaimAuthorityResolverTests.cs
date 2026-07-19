#if UNITY_EDITOR
using DungeonBuilder.M0;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class PlayerResearchClaimAuthorityResolverTests
    {
        private const string Project = "test.project";

        [Test]
        public void ValidLifecycle_UsesExplicitLocalAuthorityWithoutProductionClaim()
        {
            PlayerResearchAuthoritySummary blocked = Resolve(false, null, null);
            Assert.That(blocked.State, Is.EqualTo(PlayerResearchAuthorityState.Blocked));
            PlayerResearchAuthoritySummary available = Resolve(true, null, null);
            Assert.That(available.State, Is.EqualTo(PlayerResearchAuthorityState.Available));
            Assert.That(available.CanStart, Is.True);

            ResearchPendingState pending = Pending();
            ResearchProgressState progress = Progress(0.5d, false);
            Assert.That(Resolve(true, pending, progress).State, Is.EqualTo(PlayerResearchAuthorityState.InProgress));
            progress.ProgressUnits = 1d;
            Assert.That(Resolve(true, pending, progress).State, Is.EqualTo(PlayerResearchAuthorityState.InProgress), "Eligibility alone is not claim readiness until completion-pending is applied.");
            progress.CompletionPending = true;
            PlayerResearchAuthoritySummary ready = Resolve(true, pending, progress);
            Assert.That(ready.State, Is.EqualTo(PlayerResearchAuthorityState.ReadyForLocalMvpClaim));
            Assert.That(ready.CanClaimLocalMvp, Is.True);
            Assert.That(ready.UsesLocalMvpAuthority, Is.True);
            Assert.That(ready.CanClaimProduction, Is.False);
            Assert.That(ready.ProductionVerificationAvailable, Is.False);
            Assert.That(ready.WouldCallServer, Is.False);

            PlayerResearchAuthoritySummary completed = Resolve(true, null, null, new CompletedResearchState { ProjectIds = new[] { Project, Project } });
            Assert.That(completed.State, Is.EqualTo(PlayerResearchAuthorityState.Completed));
            Assert.That(completed.CanClaimLocalMvp, Is.False);
        }

        [TestCase(false, false, "gate.error.offline_required")]
        [TestCase(true, true, "gate.error.verification_pending")]
        public void RestrictedClaim_IsExplicitlyBlocked(bool online, bool verificationPending, string expectedKey)
        {
            GateEvaluationResult claimGate = new RestrictedActionGateService().Evaluate(new GateEvaluationInput(RestrictedActionType.ResearchComplete, online, verificationPending));
            PlayerResearchAuthoritySummary result = Resolve(true, Pending(), Progress(1d, true), claimGate: claimGate);
            Assert.That(result.State, Is.EqualTo(PlayerResearchAuthorityState.ClaimBlocked));
            Assert.That(result.FeedbackLocalizationKey, Is.EqualTo(expectedKey));
            Assert.That(result.CanClaimLocalMvp, Is.False);
        }

        [Test]
        public void DisabledLocalPolicyAndInvalidConfigurations_BlockSafely()
        {
            ResearchCompletionClaimScaffoldConfig claim = Claim();
            claim.claimAuthorityMode = "disabled";
            Assert.That(Resolve(true, Pending(), Progress(1d, true), claim: claim).State, Is.EqualTo(PlayerResearchAuthorityState.ClaimBlocked));
            claim.enabled = false;
            Assert.That(Resolve(true, Pending(), Progress(1d, true), claim: claim).State, Is.EqualTo(PlayerResearchAuthorityState.Blocked));
            Assert.That(Resolve(true, Pending(), Progress(1d, true), configuredProject: "").State, Is.EqualTo(PlayerResearchAuthorityState.Blocked));
            ResearchCompletionEligibilityScaffoldConfig eligibility = Eligibility();
            eligibility.requiredProgressUnits = 0d;
            Assert.That(Resolve(true, Pending(), Progress(1d, true), eligibility: eligibility).State, Is.EqualTo(PlayerResearchAuthorityState.Blocked));
        }

        [TestCase("orphan")]
        [TestCase("missing_progress")]
        [TestCase("slot")]
        [TestCase("project")]
        [TestCase("occupied")]
        public void InvalidSavedState_IsNeverActionable(string kind)
        {
            ResearchPendingState pending = Pending();
            ResearchProgressState progress = Progress(0.5d, false);
            if (kind == "orphan") pending = null;
            if (kind == "missing_progress") progress = null;
            if (kind == "slot") progress.SlotId = "other";
            if (kind == "project") progress.ProjectId = "other";
            if (kind == "occupied") pending.ProjectId = progress.ProjectId = "other";
            PlayerResearchAuthoritySummary result = Resolve(true, pending, progress);
            Assert.That(result.State, Is.EqualTo(PlayerResearchAuthorityState.Blocked));
            Assert.That(result.CanStart, Is.False);
            Assert.That(result.CanClaimLocalMvp, Is.False);
        }

        private static PlayerResearchAuthoritySummary Resolve(bool hasRun, ResearchPendingState pending, ResearchProgressState progress,
            CompletedResearchState completed = null, ResearchCompletionClaimScaffoldConfig claim = null,
            ResearchCompletionEligibilityScaffoldConfig eligibility = null, GateEvaluationResult? claimGate = null, string configuredProject = Project)
        {
            return PlayerResearchClaimAuthorityResolver.Resolve(configuredProject, hasRun, pending, progress, completed ?? new CompletedResearchState(),
                PendingConfig(), ProgressConfig(), eligibility ?? Eligibility(), claim ?? Claim(), Verification(),
                new GateEvaluationResult(true, "gate.ok.allowed"), claimGate ?? new GateEvaluationResult(true, "gate.ok.allowed"));
        }

        private static ResearchPendingState Pending() => new ResearchPendingState { SlotId = "slot", ProjectId = Project };
        private static ResearchProgressState Progress(double value, bool completionPending) => new ResearchProgressState { SlotId = "slot", ProjectId = Project, ProgressUnits = value, CompletionPending = completionPending, RuleSourceIdUsed = "test.progress" };
        private static ResearchPendingScaffoldConfig PendingConfig() => new ResearchPendingScaffoldConfig { enabled = true, slotId = "slot", projectId = Project, ruleSourceId = "test.pending" };
        private static ResearchProgressScaffoldConfig ProgressConfig() => new ResearchProgressScaffoldConfig { enabled = true, progressPerActiveSecond = 0.1d, maxActiveSessionElapsedSeconds = 100, ruleSourceId = "test.progress" };
        private static ResearchCompletionEligibilityScaffoldConfig Eligibility() => new ResearchCompletionEligibilityScaffoldConfig { enabled = true, projectId = Project, requiredProgressUnits = 1d, ruleSourceId = "test.eligibility" };
        private static ResearchCompletionClaimScaffoldConfig Claim() => new ResearchCompletionClaimScaffoldConfig { enabled = true, ruleSourceId = "test.claim", claimAuthorityMode = PlayerResearchClaimAuthorityResolver.LocalMvpAuthorityMode };
        private static ResearchVerificationScaffoldConfig Verification() => new ResearchVerificationScaffoldConfig { enabled = true, ruleSourceId = "test.verification", verificationMode = ResearchVerificationBoundaryResolver.UnavailableVerificationMode };
    }
}
#endif
