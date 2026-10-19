using System;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public partial class GameRoot
    {
        private bool foregroundApplicationPaused, foregroundApplicationFocused = true;
        private bool foregroundDirty = true, foregroundPumping, foregroundResumePending, foregroundRetryPending;
        private string foregroundFailure;
        public LivingDungeonForegroundInspection ForegroundActivity { get; private set; }

        private void OnDisable()
        {
            if (TimeService?.HasForegroundClock != true) return;
            TimeService.SetForegroundFocus(false);
            FlushForeground(); // close eligible time before the service is suspended
        }
        private void OnEnable()
        {
            if (TimeService?.HasForegroundClock == true) {
                TimeService.SetForegroundFocus(foregroundApplicationFocused && !foregroundApplicationPaused);
                ApplyPauseState(foregroundApplicationPaused || !foregroundApplicationFocused);
            }
        }

        private void InitializeForeground()
        {
            if (SaveService?.ForegroundConfiguration == null || TimeService == null || !CanonicalMvpRouteProjection.IsCanonical(Save)) return;
            if (Save.livingDungeon?.Scheduler != null && Save.livingDungeon.Scheduler.ProfileId != LivingDungeonForegroundConfig.ProfileId) return;
            foregroundApplicationFocused = Application.isFocused;
            TimeService.SetForegroundFocus(foregroundApplicationFocused && isActiveAndEnabled);
            TimeService.BeginForeground(SaveService.ForegroundConfiguration.UnitsPerSecond, Save.livingDungeon?.Scheduler?.AcceptedThrough ?? -1);
            SaveService.BeforeForegroundAction = FlushForeground;
            SaveService.ForegroundCurrent = () => Save;
            SaveService.CanonicalRuntimePublished += ForegroundPublished;
            ForegroundActivity = SaveService.InspectForeground(Save, _configuredSimulationTickSeconds);
            if (foregroundApplicationPaused || !foregroundApplicationFocused) ApplyPauseState(true);
        }

        private void ForegroundPublished(SaveData projection)
        {
            if (SaveService?.ForegroundWriting != true) foregroundDirty = true;
        }

        private void OnApplicationFocus(bool focused)
        {
            foregroundApplicationFocused = focused;
            TimeService?.SetForegroundFocus(focused && isActiveAndEnabled);
            if (isActiveAndEnabled && TimeService?.HasForegroundClock == true) ApplyPauseState(foregroundApplicationPaused || !focused);
        }

        private void UpdateForeground()
        {
            if (_explicitSaveDeleteQuiesced || foregroundFailure != null || foregroundRetryPending || foregroundPumping || Save == null) return;
            try {
                long now = TimeService.SampleForeground();
                if (foregroundDirty || ForegroundActivity == null || now >= ForegroundActivity.NextBoundary || Save.livingDungeon?.IsClosed == false)
                    PumpForeground(now);
                if (foregroundResumePending && foregroundFailure == null && Save.livingDungeon.IsClosed &&
                    !foregroundApplicationPaused && foregroundApplicationFocused) ApplyPauseState(false);
            } catch { ForegroundFault("living.foreground.clock_invalid"); }
        }

        private void ForegroundFault(string reason)
        {
            foregroundFailure = reason;
            ForegroundActivity = new LivingDungeonForegroundInspection { StateKey = "ui.living.blocked", NextBoundary = long.MaxValue };
            Logger?.Warn(reason);
        }

        private bool PumpForeground(long through)
        {
            foregroundPumping = true;
            try {
                long beforeTicks = Save.totalTicks;
                var result = SaveService.AdvanceForeground(Save, through, _configuredSimulationTickSeconds, ApplyForegroundPassiveTick,
                    TimeService.IsPaused ? Save.lastPausedUtcUnix : (long?)null);
                if (!result.IsSuccess) {
                    if (result.Reason == DetachedCanonicalWriteAuthority.AtomicSaveFailedReason) {
                        // The writer proved the original bytes remain authoritative.
                        // Keep that session/prefix; retry only at an explicit action
                        // or lifecycle barrier, never in a per-frame storage loop.
                        foregroundRetryPending = true;
                        ForegroundActivity = new LivingDungeonForegroundInspection { StateKey = "ui.living.retryable", NextBoundary = long.MaxValue };
                        Logger?.Warn(result.Reason);
                    } else ForegroundFault(result.Reason);
                    return false;
                }
                foregroundRetryPending = false;
                foregroundDirty = false;
                // A bounded pause flush can finish in later suspended pumps.
                // This successful complete-save transaction includes lastPaused
                // and the capped saved UTC, so the existing offline guard may
                // recognize it even when the initial AppPause save was pending.
                if (TimeService.IsPaused && Save.livingDungeon.IsClosed)
                    _resumeOfflineBoundaryPersisted = true;
                ForegroundActivity = SaveService.InspectForeground(Save, _configuredSimulationTickSeconds);
                if (result.ForegroundAdmissionReason != null && ForegroundActivity.RunId == null)
                    ForegroundActivity.StateKey = "ui.living.admission_unavailable";
                if (Save.totalTicks > beforeTicks) {
                    _activeSessionTickCount = checked(_activeSessionTickCount + Save.totalTicks - beforeTicks);
                    HandleTickTelemetry(Save.totalTicks);
                }
                CurrentHeat = Save.structureRuntime.Heat;
                RefreshOfflineSummaryLines(); RefreshStructureRuntimeLines(); RefreshRunLine();
                return true;
            } finally { foregroundPumping = false; }
        }

        // Every edit/manual/save action closes earlier accepted work first. Capacity
        // never blocks an edit; an unresolved persistence/condition failure does.
        private bool FlushForeground()
        {
            if (TimeService?.HasForegroundClock != true || foregroundPumping) return true;
            if (foregroundFailure != null || _explicitSaveDeleteQuiesced) return false;
            try {
                long target = TimeService.SampleForeground();
                int pumps = SaveService.ForegroundConfiguration.MaximumBoundaries;
                while (pumps-- > 0 && (foregroundRetryPending || Save.livingDungeon.IsDormant ||
                    !Save.livingDungeon.IsClosed || target > Save.livingDungeon.Scheduler.AcceptedThrough))
                    if (!PumpForeground(target)) return false;
                return Save.livingDungeon.IsClosed && target <= Save.livingDungeon.Scheduler.AcceptedThrough;
            } catch { ForegroundFault("living.foreground.clock_invalid"); return false; }
        }

        private void ApplyForegroundPassiveTick(SaveData candidate, long tick)
        {
            var research = new PlayerResearchActionHandler(candidate, GetResearchPendingScaffoldConfig(), GetResearchProgressScaffoldConfig(),
                GetResearchCompletionEligibilityScaffoldConfig(), GetResearchCompletionClaimScaffoldConfig(), GetResearchVerificationScaffoldConfig(),
                RunSimulationConfig?.MvpFirstSessionObjective?.AnalysisResearchProjectId, _restrictedActionGate,
                () => candidate.runHistory?.LatestOutcome != null || (candidate.runHistory?.RecentOutcomes?.Length ?? 0) > 0,
                () => IsOnline, () => VerificationPending, null);
            research.ApplyActiveTick(_configuredSimulationTickSeconds);
            _passiveManaService?.ApplyTick(candidate, RunSimulationConfig, tick);
        }
    }
}
