using System;

namespace DungeonBuilder.M0
{
    public enum PlayerResearchState
    {
        Blocked = 0,
        Available = 1,
        InProgress = 2,
        ReadyToClaim = 3,
        Completed = 4
    }

    public sealed class PlayerResearchActionResult
    {
        public bool Succeeded;
        public bool StateChanged;
        public PlayerResearchState State;
        public string FeedbackLocalizationKey = string.Empty;
        public double ProgressUnits;
        public double RequiredProgressUnits;
        public bool WouldCallServer;
        public bool WouldGrantRewards;
        public bool WouldChargeCosts;
        public bool WouldProcessOfflineProgress;
        public bool CanClaimLocalMvp;
        public bool CanClaimProduction;
        public bool UsesLocalMvpAuthority;
        public PlayerResearchAuthoritySummary Authority;
    }

    /// <summary>
    /// Owns the player-operated bridge into the existing single-slot research lifecycle.
    /// Resolver-owned validation and formulas remain authoritative; this class only applies
    /// resolver-approved transitions and persists them through the injected callback.
    /// </summary>
    public sealed class PlayerResearchActionHandler
    {
        public const string AvailableKey = "ui.player_research.available";
        public const string InProgressFormatKey = "ui.player_research.in_progress_format";
        public const string ReadyToClaimKey = "ui.player_research.ready_to_claim";
        public const string CompletedKey = "ui.player_research.completed";
        public const string BlockedPrerequisiteKey = "ui.player_research.blocked.prerequisite";
        public const string BlockedOccupiedKey = "ui.player_research.blocked.slot_occupied";
        public const string BlockedInvalidKey = "ui.player_research.blocked.unavailable";
        public const string BlockedInvalidStateKey = "ui.player_research.blocked.invalid_state";
        public const string BlockedClaimAuthorityKey = "ui.player_research.blocked.claim_authority";
        public const string AlreadyActiveKey = "ui.player_research.already_active";
        public const string NotReadyKey = "ui.player_research.blocked.not_ready";
        public const string StartSucceededKey = "ui.player_research.start_succeeded";
        public const string ClaimSucceededKey = "ui.player_research.claim_succeeded";

        private readonly SaveData _save;
        private readonly ResearchPendingScaffoldConfig _pendingConfig;
        private readonly ResearchProgressScaffoldConfig _progressConfig;
        private readonly ResearchCompletionEligibilityScaffoldConfig _eligibilityConfig;
        private readonly ResearchCompletionClaimScaffoldConfig _claimConfig;
        private readonly ResearchVerificationScaffoldConfig _verificationConfig;
        private readonly string _configuredProjectId;
        private readonly IRestrictedActionGate _restrictedActionGate;
        private readonly Func<bool> _hasValidRun;
        private readonly Func<bool> _isOnline;
        private readonly Func<bool> _verificationPending;
        private readonly Action _saveTransition;

        public PlayerResearchActionHandler(
            SaveData save,
            ResearchPendingScaffoldConfig pendingConfig,
            ResearchProgressScaffoldConfig progressConfig,
            ResearchCompletionEligibilityScaffoldConfig eligibilityConfig,
            ResearchCompletionClaimScaffoldConfig claimConfig,
            ResearchVerificationScaffoldConfig verificationConfig,
            string configuredProjectId,
            IRestrictedActionGate restrictedActionGate,
            Func<bool> hasValidRun,
            Func<bool> isOnline,
            Func<bool> verificationPending,
            Action saveTransition)
        {
            _save = save;
            _pendingConfig = pendingConfig;
            _progressConfig = progressConfig;
            _eligibilityConfig = eligibilityConfig;
            _claimConfig = claimConfig;
            _verificationConfig = verificationConfig;
            _configuredProjectId = configuredProjectId ?? string.Empty;
            _restrictedActionGate = restrictedActionGate;
            _hasValidRun = hasValidRun;
            _isOnline = isOnline;
            _verificationPending = verificationPending;
            _saveTransition = saveTransition;
        }

        public PlayerResearchActionResult ResolveState()
        {
            PlayerResearchAuthoritySummary authority = ResolveAuthority();
            return FromAuthority(authority, authority.RuleResolved, false);
        }

        public PlayerResearchActionResult Start()
        {
            PlayerResearchActionResult state = ResolveState();
            if (state.State == PlayerResearchState.InProgress || state.State == PlayerResearchState.ReadyToClaim)
                return Result(false, false, state.State, AlreadyActiveKey, state.ProgressUnits, state.RequiredProgressUnits);
            if (state.State != PlayerResearchState.Available || state.Authority == null || !state.Authority.CanStart)
            {
                state.Succeeded = false;
                return state;
            }
            ResearchPendingValidationResult pending = ResearchPendingResolver.ResolveScaffold(_pendingConfig);
            if (!pending.RuleResolved || !string.Equals(pending.ProjectId, _configuredProjectId, StringComparison.Ordinal))
                return Result(false, false, PlayerResearchState.Blocked, BlockedInvalidKey, state.ProgressUnits, state.RequiredProgressUnits);

            _save.researchPending = new ResearchPendingState { SlotId = pending.SlotId, ProjectId = pending.ProjectId };
            _save.researchProgress = new ResearchProgressState
            {
                SlotId = pending.SlotId,
                ProjectId = pending.ProjectId,
                RuleSourceIdUsed = _progressConfig.ruleSourceId
            };
            _saveTransition?.Invoke();
            PlayerResearchActionResult started = FromAuthority(ResolveAuthority(), true, true);
            started.FeedbackLocalizationKey = StartSucceededKey;
            return started;
        }

        public PlayerResearchActionResult ApplyCompletionPendingIfEligible()
        {
            PlayerResearchActionResult state = ResolveState();
            if (state.State != PlayerResearchState.InProgress && state.State != PlayerResearchState.ReadyToClaim) return state;
            ResearchCompletionPendingApplySummary apply = ResearchCompletionPendingApplyResolver.Resolve(
                _save.researchPending, _save.researchProgress, _eligibilityConfig);
            if (!apply.RuleResolved) return Result(false, false, PlayerResearchState.Blocked, BlockedInvalidKey, state.ProgressUnits, state.RequiredProgressUnits);
            if (apply.WouldSetCompletionPending)
            {
                _save.researchProgress.CompletionPending = true;
                _saveTransition?.Invoke();
                return FromAuthority(ResolveAuthority(), true, true);
            }
            return ResolveState();
        }

        public PlayerResearchActionResult ApplyActiveTick(long elapsedSeconds)
        {
            PlayerResearchActionResult state = ResolveState();
            if (state.State != PlayerResearchState.InProgress) return state;
            ResearchProgressApplySummary progress = ResearchProgressApplyResolver.Resolve(
                _save.researchPending, _save.researchProgress, _progressConfig, elapsedSeconds);
            if (!progress.RuleResolved)
                return Result(false, false, PlayerResearchState.Blocked, BlockedInvalidKey, state.ProgressUnits, state.RequiredProgressUnits);
            bool progressChanged = progress.NextProgressUnits != progress.PreviousProgressUnits;
            if (!progressChanged) return FromAuthority(ResolveAuthority(), true, false);
            _save.researchProgress.ProgressUnits = progress.NextProgressUnits;
            ResearchCompletionPendingApplySummary pending = ResearchCompletionPendingApplyResolver.Resolve(
                _save.researchPending, _save.researchProgress, _eligibilityConfig);
            if (pending.RuleResolved && pending.WouldSetCompletionPending)
                _save.researchProgress.CompletionPending = true;
            _saveTransition?.Invoke();
            return FromAuthority(ResolveAuthority(), true, true);
        }

        public PlayerResearchActionResult Claim()
        {
            PlayerResearchActionResult state = ResolveState();
            if (state.State == PlayerResearchState.Completed)
                return Result(false, false, PlayerResearchState.Completed, CompletedKey, state.ProgressUnits, state.RequiredProgressUnits);
            if (state.State != PlayerResearchState.ReadyToClaim || state.Authority == null || !state.Authority.CanClaimLocalMvp)
            {
                state.Succeeded = false;
                return state;
            }

            ResearchCompletionClaimApplySummary claim = ResearchCompletionClaimApplyResolver.Resolve(
                _save.researchPending, _save.researchProgress, _save.completedResearch, _eligibilityConfig, _claimConfig);
            if (!claim.WouldRecordCompletedResearch)
                return Result(false, false, PlayerResearchState.Blocked, NotReadyKey, state.ProgressUnits, state.RequiredProgressUnits);

            CompletedResearchState completed = _save.completedResearch ?? new CompletedResearchState();
            string[] source = completed.ProjectIds ?? Array.Empty<string>();
            var appended = new string[source.Length + 1];
            Array.Copy(source, appended, source.Length);
            appended[source.Length] = claim.ProjectId;
            completed.ProjectIds = appended;
            completed.LastCompletedProjectId = claim.ProjectId;
            completed.LastCompletionRuleSourceId = claim.RuleSourceIdUsed;
            _save.completedResearch = completed;
            _save.researchPending = null;
            _save.researchProgress = null;
            _saveTransition?.Invoke();
            PlayerResearchActionResult claimed = FromAuthority(ResolveAuthority(), true, true);
            claimed.FeedbackLocalizationKey = ClaimSucceededKey;
            return claimed;
        }

        public PlayerResearchAuthoritySummary ResolveAuthority()
        {
            GateEvaluationResult startGate = EvaluateGate(RestrictedActionType.ResearchStart);
            GateEvaluationResult claimGate = EvaluateGate(RestrictedActionType.ResearchComplete);
            return PlayerResearchClaimAuthorityResolver.Resolve(
                _configuredProjectId, HasValidRun(), _save?.researchPending, _save?.researchProgress, _save?.completedResearch,
                _pendingConfig, _progressConfig, _eligibilityConfig, _claimConfig, _verificationConfig, startGate, claimGate);
        }

        private bool HasValidRun() => _hasValidRun != null && _hasValidRun();
        private GateEvaluationResult EvaluateGate(RestrictedActionType action) => _restrictedActionGate == null
            ? new GateEvaluationResult(false, BlockedInvalidKey)
            : _restrictedActionGate.Evaluate(new GateEvaluationInput(action, _isOnline != null && _isOnline(), _verificationPending != null && _verificationPending()));

        private static PlayerResearchActionResult Result(bool succeeded, bool changed, PlayerResearchState state, string key, double progress, double required)
        {
            return new PlayerResearchActionResult
            {
                Succeeded = succeeded,
                StateChanged = changed,
                State = state,
                FeedbackLocalizationKey = key ?? string.Empty,
                ProgressUnits = progress,
                RequiredProgressUnits = required,
                WouldCallServer = false,
                WouldGrantRewards = false,
                WouldChargeCosts = false,
                WouldProcessOfflineProgress = false
            };
        }

        private static PlayerResearchActionResult FromAuthority(PlayerResearchAuthoritySummary authority, bool succeeded, bool changed)
        {
            authority = authority ?? new PlayerResearchAuthoritySummary { State = PlayerResearchAuthorityState.Blocked, FeedbackLocalizationKey = BlockedInvalidKey };
            PlayerResearchState state = authority.State == PlayerResearchAuthorityState.Available ? PlayerResearchState.Available :
                authority.State == PlayerResearchAuthorityState.InProgress ? PlayerResearchState.InProgress :
                authority.State == PlayerResearchAuthorityState.ReadyForLocalMvpClaim ? PlayerResearchState.ReadyToClaim :
                authority.State == PlayerResearchAuthorityState.Completed ? PlayerResearchState.Completed : PlayerResearchState.Blocked;
            PlayerResearchActionResult result = Result(succeeded, changed, state, authority.FeedbackLocalizationKey, authority.ProgressUnits, authority.RequiredProgressUnits);
            result.Authority = authority;
            result.CanClaimLocalMvp = authority.CanClaimLocalMvp;
            result.CanClaimProduction = authority.CanClaimProduction;
            result.UsesLocalMvpAuthority = authority.UsesLocalMvpAuthority;
            return result;
        }
    }
}
