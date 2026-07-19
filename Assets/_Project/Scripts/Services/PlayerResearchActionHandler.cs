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
        public const string AlreadyActiveKey = "ui.player_research.already_active";
        public const string NotReadyKey = "ui.player_research.blocked.not_ready";
        public const string StartSucceededKey = "ui.player_research.start_succeeded";
        public const string ClaimSucceededKey = "ui.player_research.claim_succeeded";

        private readonly SaveData _save;
        private readonly ResearchPendingScaffoldConfig _pendingConfig;
        private readonly ResearchProgressScaffoldConfig _progressConfig;
        private readonly ResearchCompletionEligibilityScaffoldConfig _eligibilityConfig;
        private readonly ResearchCompletionClaimScaffoldConfig _claimConfig;
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
            _configuredProjectId = configuredProjectId ?? string.Empty;
            _restrictedActionGate = restrictedActionGate;
            _hasValidRun = hasValidRun;
            _isOnline = isOnline;
            _verificationPending = verificationPending;
            _saveTransition = saveTransition;
        }

        public PlayerResearchActionResult ResolveState()
        {
            double progress = _save?.researchProgress?.ProgressUnits ?? 0d;
            double required = _eligibilityConfig?.requiredProgressUnits ?? 0d;
            if (!HasValidConfiguration()) return Result(false, false, PlayerResearchState.Blocked, BlockedInvalidKey, progress, required);
            if (HasCompletedConfiguredProject()) return Result(true, false, PlayerResearchState.Completed, CompletedKey, required, required);
            if (_save.researchPending == null)
            {
                return HasValidRun()
                    ? Result(true, false, PlayerResearchState.Available, AvailableKey, progress, required)
                    : Result(true, false, PlayerResearchState.Blocked, BlockedPrerequisiteKey, progress, required);
            }
            if (!IsConfiguredActiveProject()) return Result(true, false, PlayerResearchState.Blocked, BlockedOccupiedKey, progress, required);

            ResearchCompletionClaimReadinessSummary readiness = ResearchCompletionClaimReadinessResolver.Resolve(
                _save.researchPending, _save.researchProgress, _eligibilityConfig);
            if (!readiness.RuleResolved) return Result(false, false, PlayerResearchState.Blocked, BlockedInvalidKey, progress, required);
            return readiness.ReadyForClaim
                ? Result(true, false, PlayerResearchState.ReadyToClaim, ReadyToClaimKey, readiness.ProgressUnits, readiness.RequiredProgressUnits)
                : Result(true, false, PlayerResearchState.InProgress, InProgressFormatKey, readiness.ProgressUnits, readiness.RequiredProgressUnits);
        }

        public PlayerResearchActionResult Start()
        {
            PlayerResearchActionResult state = ResolveState();
            if (state.State == PlayerResearchState.InProgress || state.State == PlayerResearchState.ReadyToClaim)
                return Result(false, false, state.State, AlreadyActiveKey, state.ProgressUnits, state.RequiredProgressUnits);
            if (state.State != PlayerResearchState.Available) return state;

            GateEvaluationResult gate = EvaluateGate(RestrictedActionType.ResearchStart);
            if (!gate.Allowed) return Result(false, false, PlayerResearchState.Blocked, gate.MessageKey, state.ProgressUnits, state.RequiredProgressUnits);
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
            return Result(true, true, PlayerResearchState.InProgress, StartSucceededKey, 0d, state.RequiredProgressUnits);
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
                return Result(true, true, PlayerResearchState.ReadyToClaim, ReadyToClaimKey, apply.ProgressUnits, apply.RequiredProgressUnits);
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
            _save.researchProgress.ProgressUnits = progress.NextProgressUnits;
            return ApplyCompletionPendingIfEligible();
        }

        public PlayerResearchActionResult Claim()
        {
            PlayerResearchActionResult state = ApplyCompletionPendingIfEligible();
            if (state.State == PlayerResearchState.Completed)
                return Result(false, false, PlayerResearchState.Completed, CompletedKey, state.ProgressUnits, state.RequiredProgressUnits);
            if (state.State != PlayerResearchState.ReadyToClaim)
                return Result(false, state.StateChanged, state.State, NotReadyKey, state.ProgressUnits, state.RequiredProgressUnits);
            GateEvaluationResult gate = EvaluateGate(RestrictedActionType.ResearchComplete);
            if (!gate.Allowed) return Result(false, false, PlayerResearchState.Blocked, gate.MessageKey, state.ProgressUnits, state.RequiredProgressUnits);

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
            return Result(true, true, PlayerResearchState.Completed, ClaimSucceededKey, state.RequiredProgressUnits, state.RequiredProgressUnits);
        }

        private bool HasValidConfiguration()
        {
            return _save != null && _pendingConfig != null && _progressConfig != null && _eligibilityConfig != null && _claimConfig != null &&
                   _pendingConfig.enabled && _progressConfig.enabled && _eligibilityConfig.enabled && _claimConfig.enabled &&
                   !string.IsNullOrWhiteSpace(_configuredProjectId) &&
                   string.Equals(_pendingConfig.projectId, _configuredProjectId, StringComparison.Ordinal) &&
                   string.Equals(_eligibilityConfig.projectId, _configuredProjectId, StringComparison.Ordinal) &&
                   !string.IsNullOrWhiteSpace(_pendingConfig.slotId) && !string.IsNullOrWhiteSpace(_progressConfig.ruleSourceId);
        }

        private bool HasValidRun() => _hasValidRun != null && _hasValidRun();
        private bool IsConfiguredActiveProject() => _save.researchPending != null && string.Equals(_save.researchPending.ProjectId, _configuredProjectId, StringComparison.Ordinal);
        private bool HasCompletedConfiguredProject()
        {
            string[] ids = _save?.completedResearch?.ProjectIds;
            if (ids == null) return false;
            for (int i = 0; i < ids.Length; i++) if (string.Equals(ids[i], _configuredProjectId, StringComparison.Ordinal)) return true;
            return false;
        }

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
    }
}
