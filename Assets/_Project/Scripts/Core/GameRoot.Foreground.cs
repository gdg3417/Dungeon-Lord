using System;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public partial class GameRoot
    {
        private bool foregroundApplicationPaused, foregroundApplicationFocused = true;
        private bool foregroundDirty = true, foregroundPumping, foregroundResumePending;
        private string foregroundFailure;
        public LivingDungeonForegroundInspection ForegroundActivity { get; private set; }

        private void InitializeForeground()
        {
            if (SaveService?.ForegroundConfiguration == null || TimeService == null || !CanonicalMvpRouteProjection.IsCanonical(Save)) return;
            if (Save.livingDungeon?.Scheduler != null && Save.livingDungeon.Scheduler.ProfileId != LivingDungeonForegroundConfig.ProfileId) return;
            foregroundApplicationFocused = Application.isFocused;
            TimeService.SetForegroundFocus(foregroundApplicationFocused);
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
            TimeService?.SetForegroundFocus(focused);
            if (TimeService?.HasForegroundClock == true) ApplyPauseState(foregroundApplicationPaused || !focused);
        }

        private void UpdateForeground()
        {
            if (_explicitSaveDeleteQuiesced || foregroundFailure != null || foregroundPumping || Save == null) return;
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
                var result = SaveService.AdvanceForeground(Save, through, _configuredSimulationTickSeconds, ApplyForegroundPassiveTick);
                if (!result.IsSuccess) { ForegroundFault(result.Reason); return false; }
                foregroundDirty = false;
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
                while (pumps-- > 0 && (foregroundDirty || Save.livingDungeon.IsDormant ||
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
