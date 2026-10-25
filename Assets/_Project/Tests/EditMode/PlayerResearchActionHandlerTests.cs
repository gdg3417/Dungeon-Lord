#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using DungeonBuilder.M0;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class PlayerResearchActionHandlerTests
    {
        private const string ProjectId = "test.research.activity_analysis";
        private SaveData _save;
        private bool _hasRun;
        private bool _online;
        private bool _verificationPending;
        private int _saveCount;
        private bool _allowLocalClaim;
        private bool _includeGate;

        [Test] public void CompletedHistoryPrerequisiteRejectsNullEmptyAndAdmissionOnlyHistory()
        {
            Assert.That(RunHistoryState.HasCompletedAdventurerRun(null), Is.False);
            foreach (int next in new[] { 1, 2, 3 }) // allocator advance/admitted or retained parties is not completion
                Assert.That(RunHistoryState.HasCompletedAdventurerRun(new RunHistoryState { NextRunSequence = next }), Is.False);
            Assert.That(RunHistoryState.HasCompletedAdventurerRun(new RunHistoryState {
                LatestOutcome = null, RecentOutcomes = null }), Is.False);
        }

        [TestCase(null)] [TestCase("")] [TestCase(" \t\r\n")] [TestCase("arbitrary")]
        [TestCase("run-0")] [TestCase("run-01")] [TestCase("run-+1")] [TestCase("run-2147483648")]
        [TestCase("run-2")] [TestCase("RUN-1")] [TestCase("run-1 ")] [TestCase("run-1\0")]
        public void CompletedHistoryPrerequisiteRejectsShellAndNonCanonicalOrUnallocatedIdentity(string id)
        {
            var shell = new RunOutcomeRecord { RunId = id };
            Assert.That(RunHistoryState.HasCompletedAdventurerRun(new RunHistoryState { NextRunSequence = 2, LatestOutcome = shell }), Is.False);
            Assert.That(RunHistoryState.HasCompletedAdventurerRun(new RunHistoryState { NextRunSequence = 2,
                RecentOutcomes = new[] { null, new RunOutcomeRecord(), shell } }), Is.False);
        }

        [Test] public void CompletedHistoryPrerequisiteRejectsActualUnityRoundtripShell()
        {
            var history = UnityEngine.JsonUtility.FromJson<RunHistoryState>(UnityEngine.JsonUtility.ToJson(new RunHistoryState()));
            Assert.That(history.LatestOutcome, Is.Not.Null, "Reproduce native Unity null-class serialization");
            Assert.That(history.LatestOutcome.RunId, Is.Null.Or.Empty);
            Assert.That(RunHistoryState.HasCompletedAdventurerRun(history), Is.False);
        }

        [TestCase(true)] [TestCase(false)]
        public void CompletedHistoryPrerequisiteAcceptsSettledHistoryAndNativeReopenRegardlessOfVictory(bool success)
        {
            var history = new RunHistoryState { NextRunSequence = 2 };
            history.AppendOutcome(new RunOutcomeRecord { RunId = "run-1", Success = success }, 5);
            Assert.That(history.LatestOutcome, Is.SameAs(history.RecentOutcomes[0]));
            Assert.That(RunHistoryState.HasCompletedAdventurerRun(history), Is.True);
            var reopened = UnityEngine.JsonUtility.FromJson<RunHistoryState>(UnityEngine.JsonUtility.ToJson(history));
            Assert.That(RunHistoryState.HasCompletedAdventurerRun(reopened), Is.True);
            // A retained legacy latest-only outcome remains eligible; full canonical
            // validation, not this existence predicate, rejects contradictory aliases.
            reopened.RecentOutcomes = null;
            Assert.That(RunHistoryState.HasCompletedAdventurerRun(reopened), Is.True);
            reopened.RecentOutcomes = new[] { reopened.LatestOutcome }; reopened.LatestOutcome = null;
            Assert.That(RunHistoryState.HasCompletedAdventurerRun(reopened), Is.True);
        }

        [Test] public void CompletedHistoryPrerequisiteDoesNotAllocate()
        {
            var history = new RunHistoryState { NextRunSequence = 2, RecentOutcomes = new[] { new RunOutcomeRecord { RunId = "run-1" } } };
            for (int i = 0; i < 100; i++) RunHistoryState.HasCompletedAdventurerRun(history);
            long before = GC.GetAllocatedBytesForCurrentThread(); int found = 0;
            for (int i = 0; i < 1000; i++) if (RunHistoryState.HasCompletedAdventurerRun(history)) found++;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(found, Is.EqualTo(1000)); Assert.That(allocated, Is.Zero);
        }

        [SetUp]
        public void SetUp()
        {
            _save = new SaveData { completedResearch = new CompletedResearchState(), runHistory = new RunHistoryState() };
            _hasRun = false;
            _online = true;
            _verificationPending = false;
            _saveCount = 0;
            _allowLocalClaim = true;
            _includeGate = true;
        }

        [Test]
        public void Start_RequiresRun_UsesConfiguredProject_AndIsExplicitlyIdempotent()
        {
            PlayerResearchActionHandler handler = Create();
            Assert.That(handler.ResolveState().State, Is.EqualTo(PlayerResearchState.Blocked));
            Assert.That(handler.Start().Succeeded, Is.False);
            Assert.That(_save.researchPending, Is.Null);

            _hasRun = true;
            Assert.That(handler.ResolveState().State, Is.EqualTo(PlayerResearchState.Available));
            PlayerResearchActionResult started = handler.Start();
            Assert.That(started.Succeeded, Is.True);
            Assert.That(started.StateChanged, Is.True);
            Assert.That(_save.researchPending.ProjectId, Is.EqualTo(ProjectId));
            Assert.That(_save.researchProgress.ProjectId, Is.EqualTo(ProjectId));
            Assert.That(_saveCount, Is.EqualTo(1));

            PlayerResearchActionResult repeated = handler.Start();
            Assert.That(repeated.Succeeded, Is.False);
            Assert.That(repeated.FeedbackLocalizationKey, Is.EqualTo(PlayerResearchActionHandler.AlreadyActiveKey));
            Assert.That(_saveCount, Is.EqualTo(1));
        }

        [Test]
        public void Start_WhenAnotherProjectOccupiesSlot_FailsWithoutMutation()
        {
            _hasRun = true;
            _save.researchPending = new ResearchPendingState { SlotId = "slot", ProjectId = "test.research.other" };
            _save.researchProgress = new ResearchProgressState { SlotId = "slot", ProjectId = "test.research.other" };
            string before = UnityEngine.JsonUtility.ToJson(_save);
            PlayerResearchActionResult result = Create().Start();
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FeedbackLocalizationKey, Is.EqualTo(PlayerResearchActionHandler.BlockedOccupiedKey));
            Assert.That(UnityEngine.JsonUtility.ToJson(_save), Is.EqualTo(before));
        }

        [Test]
        public void ActiveTicks_AdvanceDeterministically_AndApplyPendingOnce()
        {
            _hasRun = true;
            PlayerResearchActionHandler handler = Create();
            handler.Start();
            PlayerResearchActionResult first = handler.ApplyActiveTick(5);
            Assert.That(first.State, Is.EqualTo(PlayerResearchState.InProgress));
            Assert.That(first.StateChanged, Is.True);
            Assert.That(_save.researchProgress.ProgressUnits, Is.EqualTo(0.5d));
            PlayerResearchActionResult second = handler.ApplyActiveTick(5);
            Assert.That(second.State, Is.EqualTo(PlayerResearchState.ReadyToClaim));
            Assert.That(second.StateChanged, Is.True);
            Assert.That(_save.researchProgress.CompletionPending, Is.True);
            int savesAfterPending = _saveCount;
            PlayerResearchActionResult repeated = handler.ApplyCompletionPendingIfEligible();
            Assert.That(repeated.State, Is.EqualTo(PlayerResearchState.ReadyToClaim));
            Assert.That(repeated.StateChanged, Is.False);
            Assert.That(_saveCount, Is.EqualTo(savesAfterPending));
        }

        [Test]
        public void ActiveTick_ZeroNegativeDisabledReadyAndCompletedDoNotReportMutation()
        {
            _hasRun = true;
            PlayerResearchActionHandler handler = Create();
            handler.Start();
            Assert.That(handler.ApplyActiveTick(0).StateChanged, Is.False);
            Assert.That(handler.ApplyActiveTick(-1).StateChanged, Is.False);
            handler.ApplyActiveTick(10);
            Assert.That(handler.ApplyActiveTick(10).StateChanged, Is.False);
            handler.Claim();
            Assert.That(handler.ApplyActiveTick(10).StateChanged, Is.False);
        }

        [Test]
        public void ActiveTick_ClampsElapsedTimeToConfiguredMaximum()
        {
            _hasRun = true;
            PlayerResearchActionHandler handler = Create();
            handler.Start();
            PlayerResearchActionResult result = handler.ApplyActiveTick(1000);
            Assert.That(result.ProgressUnits, Is.EqualTo(10d));
            Assert.That(result.StateChanged, Is.True);
        }

        [Test]
        public void Claim_RequiresProgressOnlineAndNoPendingVerification_ThenRecordsExactlyOnce()
        {
            _hasRun = true;
            PlayerResearchActionHandler handler = Create();
            handler.Start();
            Assert.That(handler.Claim().Succeeded, Is.False);
            Assert.That(_save.completedResearch.ProjectIds, Is.Null.Or.Empty);
            handler.ApplyActiveTick(10);

            _online = false;
            PlayerResearchActionResult offline = handler.Claim();
            Assert.That(offline.Succeeded, Is.False);
            Assert.That(offline.FeedbackLocalizationKey, Is.EqualTo("gate.error.offline_required"));
            _online = true;
            _verificationPending = true;
            PlayerResearchActionResult pending = handler.Claim();
            Assert.That(pending.Succeeded, Is.False);
            Assert.That(pending.FeedbackLocalizationKey, Is.EqualTo("gate.error.verification_pending"));

            _verificationPending = false;
            PlayerResearchActionResult claimed = handler.Claim();
            Assert.That(claimed.Succeeded, Is.True);
            Assert.That(_save.completedResearch.ProjectIds, Is.EqualTo(new[] { ProjectId }));
            Assert.That(_save.researchPending, Is.Null);
            Assert.That(_save.researchProgress, Is.Null);
            PlayerResearchActionResult repeated = handler.Claim();
            Assert.That(repeated.Succeeded, Is.False);
            Assert.That(_save.completedResearch.ProjectIds, Has.Length.EqualTo(1));
            Assert.That(claimed.WouldCallServer, Is.False);
            Assert.That(claimed.WouldGrantRewards, Is.False);
            Assert.That(claimed.WouldChargeCosts, Is.False);
            Assert.That(claimed.WouldProcessOfflineProgress, Is.False);
        }

        [Test]
        public void ResolveState_ClaimPolicyDisabledOrGateMissingBlocksLocalClaimWithoutProductionAuthority()
        {
            _hasRun = true;
            PlayerResearchActionHandler handler = Create();
            handler.Start();
            handler.ApplyActiveTick(10);
            _allowLocalClaim = false;
            PlayerResearchActionResult disabled = Create().ResolveState();
            Assert.That(disabled.State, Is.EqualTo(PlayerResearchState.Blocked));
            Assert.That(disabled.CanClaimLocalMvp, Is.False);
            Assert.That(disabled.CanClaimProduction, Is.False);

            _allowLocalClaim = true;
            _includeGate = false;
            PlayerResearchActionResult missingGate = Create().ResolveState();
            Assert.That(missingGate.State, Is.EqualTo(PlayerResearchState.Blocked));
            Assert.That(missingGate.CanClaimLocalMvp, Is.False);
        }

        [TestCase("orphan_progress")]
        [TestCase("pending_without_progress")]
        [TestCase("slot_mismatch")]
        [TestCase("project_mismatch")]
        [TestCase("other_project")]
        public void MalformedResearchState_IsBlockedAndPreserved(string malformedCase)
        {
            _hasRun = true;
            _save.researchPending = new ResearchPendingState { SlotId = "slot", ProjectId = ProjectId };
            _save.researchProgress = new ResearchProgressState { SlotId = "slot", ProjectId = ProjectId, ProgressUnits = 0.25d, RuleSourceIdUsed = "test.progress" };
            switch (malformedCase)
            {
                case "orphan_progress": _save.researchPending = null; break;
                case "pending_without_progress": _save.researchProgress = null; break;
                case "slot_mismatch": _save.researchProgress.SlotId = "other.slot"; break;
                case "project_mismatch": _save.researchProgress.ProjectId = "other.project"; break;
                case "other_project": _save.researchPending.ProjectId = _save.researchProgress.ProjectId = "other.project"; break;
            }
            string before = UnityEngine.JsonUtility.ToJson(_save);
            PlayerResearchActionHandler handler = Create();
            PlayerResearchActionResult state = handler.ResolveState();
            Assert.That(state.State, Is.EqualTo(PlayerResearchState.Blocked));
            Assert.That(handler.Start().Succeeded, Is.False);
            Assert.That(handler.ApplyActiveTick(10).StateChanged, Is.False);
            Assert.That(UnityEngine.JsonUtility.ToJson(_save), Is.EqualTo(before));
            PlayerResearchPanelPresentation panel = PlayerResearchPanelPresenter.Present(state, (key, fallback) => "Localized blocked state");
            Assert.That(panel.ShowAction, Is.False);
        }

        [Test]
        public void StateAndPanel_AreLegacySafeLocalizedAndContainNoInternalIdsOrKeys()
        {
            PlayerResearchActionHandler handler = Create();
            Assert.DoesNotThrow(() => handler.ResolveState());
            _hasRun = true;
            handler.Start();
            handler.ApplyActiveTick(4);
            var strings = new Dictionary<string, string>
            {
                [PlayerResearchActionHandler.InProgressFormatKey] = "Research in progress: {0} / {1}"
            };
            PlayerResearchPanelPresentation panel = PlayerResearchPanelPresenter.Present(
                handler.ResolveState(), (key, fallback) => strings.TryGetValue(key, out string value) ? value : fallback);
            Assert.That(panel.StatusText, Is.EqualTo("Research in progress: 0.4 / 1"));
            Assert.That(panel.StatusText, Does.Not.Contain(ProjectId));
            Assert.That(panel.StatusText, Does.Not.Contain("ui."));
            Assert.That(panel.ShowAction, Is.False);
        }

        [Test]
        public void JsonRoundTrip_PreservesActiveAndCompletedResearch_WhileCleanResetClearsBoth()
        {
            _hasRun = true;
            PlayerResearchActionHandler handler = Create();
            handler.Start();
            handler.ApplyActiveTick(4);
            SaveData activeLoaded = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(_save));
            Assert.That(activeLoaded.researchProgress.ProgressUnits, Is.EqualTo(0.4d));
            handler.ApplyActiveTick(6);
            handler.Claim();
            SaveData completedLoaded = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(_save));
            Assert.That(completedLoaded.completedResearch.ProjectIds, Does.Contain(ProjectId));
            GameRoot.ApplyCleanMvpValidationBaseline(completedLoaded);
            Assert.That(completedLoaded.researchPending, Is.Null);
            Assert.That(completedLoaded.researchProgress, Is.Null);
            Assert.That(completedLoaded.completedResearch.ProjectIds, Is.Null.Or.Empty);
        }

        private PlayerResearchActionHandler Create()
        {
            return new PlayerResearchActionHandler(
                _save,
                new ResearchPendingScaffoldConfig { enabled = true, slotId = "slot", projectId = ProjectId, ruleSourceId = "test.pending" },
                new ResearchProgressScaffoldConfig { enabled = true, ruleSourceId = "test.progress", progressPerActiveSecond = 0.1d, maxActiveSessionElapsedSeconds = 100 },
                new ResearchCompletionEligibilityScaffoldConfig { enabled = true, projectId = ProjectId, requiredProgressUnits = 1d, ruleSourceId = "test.eligibility" },
                new ResearchCompletionClaimScaffoldConfig { enabled = true, ruleSourceId = "test.claim", claimAuthorityMode = _allowLocalClaim ? PlayerResearchClaimAuthorityResolver.LocalMvpAuthorityMode : "disabled" },
                new ResearchVerificationScaffoldConfig { enabled = true, ruleSourceId = "test.verification", verificationMode = ResearchVerificationBoundaryResolver.UnavailableVerificationMode },
                ProjectId,
                _includeGate ? (IRestrictedActionGate)new RestrictedActionGateService() : null,
                () => _hasRun,
                () => _online,
                () => _verificationPending,
                () => _saveCount++,
                stage =>
                {
                    // Unit-only durable owner fake; production coverage uses SaveService.
                    var candidate = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(_save));
                    if (_save.researchPending == null) candidate.researchPending = null;
                    if (_save.researchProgress == null) candidate.researchProgress = null;
                    var result = stage(candidate);
                    if (result.Succeeded && result.StateChanged) { _save = candidate; _saveCount++; }
                    return result;
                }, () => _save);
        }
    }
}
#endif
