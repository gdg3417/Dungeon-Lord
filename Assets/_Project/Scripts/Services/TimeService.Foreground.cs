using System;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0
{
    public partial class TimeService
    {
        // Transient sampling only. Scheduler horizons are the sole durable elapsed owner.
        private int foregroundUnitsPerSecond;
        private long foregroundSample, foregroundThrough;
        private bool foregroundFocused = true;
        private UnixTimePoint foregroundWall;
        private bool recoveryAwaitingForegroundHorizon;
        internal bool HasForegroundClock => foregroundUnitsPerSecond > 0;
        internal long ForegroundThrough => foregroundThrough;
        internal bool ForegroundPresentationActive => HasForegroundClock && foregroundFocused && !_isPaused && _save != null &&
            !recoveryClockActive && (recoveryDeferredForegroundWall == null || DeferredForegroundCanExtend);
        internal void BeginForeground(int unitsPerSecond, long acceptedThrough)
        {
            if (HasForegroundClock || unitsPerSecond <= 0 || acceptedThrough < -1) throw new InvalidOperationException("living.foreground.clock_invalid");
            foregroundUnitsPerSecond = unitsPerSecond;
            foregroundSample = ReadForegroundSample(); foregroundThrough = Math.Max(0, acceptedThrough);
        }
        internal void BeginRecoveryForeground(int unitsPerSecond)
        {
            BeginForeground(unitsPerSecond, -1); recoveryAwaitingForegroundHorizon = true;
        }
        internal void BindLoadedForegroundHorizon(long acceptedThrough)
        {
            if (!recoveryAwaitingForegroundHorizon || acceptedThrough < -1) throw new InvalidOperationException(RecoveryEpisode.Invalid);
            foregroundThrough = checked(foregroundThrough + Math.Max(0, acceptedThrough));
            recoveryAwaitingForegroundHorizon = false;
        }
        private long ReadForegroundSample()
        {
            double units = Math.Floor(_clock.MonotonicSeconds * foregroundUnitsPerSecond);
            if (double.IsNaN(units) || double.IsInfinity(units) || units < 0 || units >= long.MaxValue)
                throw new InvalidOperationException("living.foreground.clock_invalid");
            return checked((long)units);
        }
        internal long SampleForeground()
        {
            if (!HasForegroundClock) throw new InvalidOperationException("living.foreground.clock_invalid");
            long sample = ReadForegroundSample();
            if (sample < foregroundSample) throw new InvalidOperationException("living.foreground.clock_invalid");
            if (ForegroundPresentationActive) foregroundThrough = checked(foregroundThrough + checked(sample - foregroundSample));
            foregroundSample = sample; return foregroundThrough;
        }
        internal ForegroundObservation CaptureForegroundObservation(DetachedCanonicalSaveSession predecessor)
            => ForegroundObservation.Capture(this, predecessor);
        internal (long Sample, long Through, long Accepted, long Closed, UnixTimePoint Wall, ForegroundObservationContext Context) ReadForegroundObservation()
        {
            if (!HasForegroundClock || _save == null)
                throw new InvalidOperationException("living.foreground.observation_invalid");
            if (HasDeferredRecovery && !DeferredForegroundCanExtend) {
                var deferred = ObserveDeferredRecovery();
                if (_save.timeProvenance == null || recoveryDeferredForegroundWall.CompareTo(_save.timeProvenance.AccountedBoundary) < 0)
                    throw new InvalidOperationException(RecoveryEpisode.Invalid);
                return (deferred.Units, recoveryDeferredForegroundThrough, _save.livingDungeon?.Scheduler?.AcceptedThrough ?? -1,
                    _save.livingDungeon?.Scheduler?.ClosedThrough ?? -1, recoveryDeferredForegroundWall, ForegroundObservationContext.EligibleForeground);
            }
            long sample = ReadForegroundSample();
            var wall = _clock.CaptureUtc();
            // Do not mutate the logical sample when either clock is invalid.
            if (sample < foregroundSample || wall == null || wall.Seconds <= 0 ||
                wall.Seconds < _save.lastSavedUtcUnix || foregroundWall != null && wall.CompareTo(foregroundWall) < 0)
                throw new InvalidOperationException("living.foreground.clock_invalid");
            if (_save.timeProvenance?.Mode == TimeProvenance.PreciseMode && wall.CompareTo(_save.timeProvenance.AccountedBoundary) < 0)
                throw new InvalidOperationException("living.foreground.clock_invalid");
            bool eligible = ForegroundPresentationActive;
            long through = eligible ? checked(foregroundThrough + checked(sample - foregroundSample)) : foregroundThrough;
            return (sample, through, _save.livingDungeon?.Scheduler?.AcceptedThrough ?? -1, _save.livingDungeon?.Scheduler?.ClosedThrough ?? -1,
                wall, eligible ? ForegroundObservationContext.EligibleForeground : ForegroundObservationContext.Suspended);
        }
        internal void CommitForegroundObservation(long sample, long through, UnixTimePoint wall)
        { foregroundSample = sample; foregroundThrough = through; foregroundWall = wall; }
        internal void SetForegroundFocus(bool focused)
        {
            RecoveryLifecycleObservation(focused, _isPaused);
            if (HasForegroundClock) SampleForeground();
            foregroundFocused = focused;
        }
    }
}
