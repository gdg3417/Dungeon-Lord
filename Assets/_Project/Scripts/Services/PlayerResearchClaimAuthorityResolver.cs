using System;

namespace DungeonBuilder.M0
{
    public enum PlayerResearchAuthorityState
    {
        Blocked = 0,
        Available = 1,
        InProgress = 2,
        ClaimBlocked = 3,
        ReadyForLocalMvpClaim = 4,
        Completed = 5
    }

    [Serializable]
    public sealed class PlayerResearchAuthoritySummary
    {
        public bool RuleResolved;
        public PlayerResearchAuthorityState State;
        public string FeedbackLocalizationKey = string.Empty;
        public double ProgressUnits;
        public double RequiredProgressUnits;
        public bool CanStart;
        public bool CanClaimLocalMvp;
        public bool CanClaimProduction;
        public bool StateMutationPermitted;
        public bool UsesLocalMvpAuthority;
        public bool ProductionVerificationAvailable;
        public bool WouldCallServer;
        public bool WouldGrantRewards;
        public bool WouldChargeCosts;
        public bool WouldProcessOfflineProgress;
    }

    public static class PlayerResearchClaimAuthorityResolver
    {
        public const string LocalMvpAuthorityMode = "localMvp";

        public static PlayerResearchAuthoritySummary Resolve(
            string configuredProjectId,
            bool hasValidRun,
            ResearchPendingState pending,
            ResearchProgressState progress,
            CompletedResearchState completed,
            ResearchPendingScaffoldConfig pendingConfig,
            ResearchProgressScaffoldConfig progressConfig,
            ResearchCompletionEligibilityScaffoldConfig eligibilityConfig,
            ResearchCompletionClaimScaffoldConfig claimConfig,
            ResearchVerificationScaffoldConfig verificationConfig,
            GateEvaluationResult startGate,
            GateEvaluationResult claimGate)
        {
            double current = progress?.ProgressUnits ?? 0d;
            double required = eligibilityConfig?.requiredProgressUnits ?? 0d;
            if (!string.IsNullOrWhiteSpace(configuredProjectId) && Contains(completed?.ProjectIds, configuredProjectId))
                return Create(PlayerResearchAuthorityState.Completed, PlayerResearchActionHandler.CompletedKey, required, required, resolved: true);
            if (!ValidConfig(configuredProjectId, pendingConfig, progressConfig, eligibilityConfig, claimConfig, verificationConfig))
                return Create(PlayerResearchAuthorityState.Blocked, PlayerResearchActionHandler.BlockedInvalidKey, current, required);

            bool hasPending = pending != null;
            bool hasProgress = progress != null;
            if (hasPending != hasProgress)
                return Create(PlayerResearchAuthorityState.Blocked, PlayerResearchActionHandler.BlockedInvalidStateKey, current, required);

            if (!hasPending)
            {
                if (!hasValidRun)
                    return Create(PlayerResearchAuthorityState.Blocked, PlayerResearchActionHandler.BlockedPrerequisiteKey, current, required, resolved: true);
                if (!startGate.Allowed)
                    return Create(PlayerResearchAuthorityState.Blocked, startGate.MessageKey, current, required, resolved: true);
                return Create(PlayerResearchAuthorityState.Available, PlayerResearchActionHandler.AvailableKey, current, required, resolved: true, canStart: true, mutation: true);
            }

            if (!string.Equals(pending.ProjectId, configuredProjectId, StringComparison.Ordinal))
                return Create(PlayerResearchAuthorityState.Blocked, PlayerResearchActionHandler.BlockedOccupiedKey, current, required, resolved: true);
            if (string.IsNullOrWhiteSpace(pending.SlotId) || string.IsNullOrWhiteSpace(progress.SlotId) ||
                !string.Equals(pending.SlotId, progress.SlotId, StringComparison.Ordinal) ||
                !string.Equals(pending.ProjectId, progress.ProjectId, StringComparison.Ordinal) ||
                double.IsNaN(current) || double.IsInfinity(current) || current < 0d)
                return Create(PlayerResearchAuthorityState.Blocked, PlayerResearchActionHandler.BlockedInvalidStateKey, current, required);

            ResearchCompletionClaimReadinessSummary readiness = ResearchCompletionClaimReadinessResolver.Resolve(pending, progress, eligibilityConfig);
            if (!readiness.RuleResolved)
                return Create(PlayerResearchAuthorityState.Blocked, PlayerResearchActionHandler.BlockedInvalidStateKey, current, required);
            if (!readiness.ReadyForClaim)
                return Create(PlayerResearchAuthorityState.InProgress, PlayerResearchActionHandler.InProgressFormatKey, current, required, resolved: true, mutation: true);

            bool productionAvailable = string.Equals(verificationConfig.verificationMode, ResearchVerificationBoundaryResolver.LocalDevPlaceholderVerificationMode, StringComparison.Ordinal);
            bool localAllowed = string.Equals(claimConfig.claimAuthorityMode, LocalMvpAuthorityMode, StringComparison.Ordinal);
            if (!localAllowed)
                return Create(PlayerResearchAuthorityState.ClaimBlocked, PlayerResearchActionHandler.BlockedClaimAuthorityKey, current, required, resolved: true, productionAvailable: productionAvailable);
            if (!claimGate.Allowed)
                return Create(PlayerResearchAuthorityState.ClaimBlocked, claimGate.MessageKey, current, required, resolved: true, local: true, productionAvailable: productionAvailable);
            return Create(PlayerResearchAuthorityState.ReadyForLocalMvpClaim, PlayerResearchActionHandler.ReadyToClaimKey, current, required,
                resolved: true, canClaim: true, mutation: true, local: true, productionAvailable: productionAvailable);
        }

        private static bool ValidConfig(string projectId, ResearchPendingScaffoldConfig pending, ResearchProgressScaffoldConfig progress,
            ResearchCompletionEligibilityScaffoldConfig eligibility, ResearchCompletionClaimScaffoldConfig claim, ResearchVerificationScaffoldConfig verification)
        {
            return !string.IsNullOrWhiteSpace(projectId) && pending != null && pending.enabled && progress != null && progress.enabled &&
                   eligibility != null && eligibility.enabled && claim != null && claim.enabled && verification != null && verification.enabled &&
                   !string.IsNullOrWhiteSpace(pending.slotId) && !string.IsNullOrWhiteSpace(pending.ruleSourceId) && !string.IsNullOrWhiteSpace(progress.ruleSourceId) &&
                   progress.maxActiveSessionElapsedSeconds >= 0 && progress.progressPerActiveSecond >= 0d &&
                   !double.IsNaN(progress.progressPerActiveSecond) && !double.IsInfinity(progress.progressPerActiveSecond) &&
                   !string.IsNullOrWhiteSpace(eligibility.ruleSourceId) && eligibility.requiredProgressUnits > 0d &&
                   !double.IsNaN(eligibility.requiredProgressUnits) && !double.IsInfinity(eligibility.requiredProgressUnits) &&
                   !string.IsNullOrWhiteSpace(claim.ruleSourceId) && !string.IsNullOrWhiteSpace(verification.ruleSourceId) &&
                   (string.Equals(verification.verificationMode, ResearchVerificationBoundaryResolver.UnavailableVerificationMode, StringComparison.Ordinal) ||
                    string.Equals(verification.verificationMode, ResearchVerificationBoundaryResolver.LocalDevPlaceholderVerificationMode, StringComparison.Ordinal)) &&
                   string.Equals(pending.projectId, projectId, StringComparison.Ordinal) && string.Equals(eligibility.projectId, projectId, StringComparison.Ordinal);
        }

        private static bool Contains(string[] values, string value)
        {
            if (values == null) return false;
            for (int i = 0; i < values.Length; i++) if (string.Equals(values[i], value, StringComparison.Ordinal)) return true;
            return false;
        }

        private static PlayerResearchAuthoritySummary Create(PlayerResearchAuthorityState state, string key, double current, double required,
            bool resolved = false, bool canStart = false, bool canClaim = false, bool mutation = false, bool local = false, bool productionAvailable = false)
        {
            return new PlayerResearchAuthoritySummary
            {
                RuleResolved = resolved,
                State = state,
                FeedbackLocalizationKey = key ?? string.Empty,
                ProgressUnits = current,
                RequiredProgressUnits = required,
                CanStart = canStart,
                CanClaimLocalMvp = canClaim,
                CanClaimProduction = false,
                StateMutationPermitted = mutation,
                UsesLocalMvpAuthority = local,
                ProductionVerificationAvailable = productionAvailable,
                WouldCallServer = false,
                WouldGrantRewards = false,
                WouldChargeCosts = false,
                WouldProcessOfflineProgress = false
            };
        }
    }
}
