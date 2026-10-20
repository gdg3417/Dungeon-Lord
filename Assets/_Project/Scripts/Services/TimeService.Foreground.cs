using System;

namespace DungeonBuilder.M0
{
    public partial class TimeService
    {
        // Transient sampling only. Scheduler horizons are the sole durable elapsed owner.
        private int foregroundUnitsPerSecond;
        private long foregroundSample, foregroundThrough;
        private bool foregroundFocused = true;
        internal bool HasForegroundClock => foregroundUnitsPerSecond > 0;
        internal long ForegroundThrough => foregroundThrough;
        internal bool ForegroundPresentationActive => HasForegroundClock && foregroundFocused && !_isPaused && _save != null;
        internal void BeginForeground(int unitsPerSecond, long acceptedThrough)
        {
            if (HasForegroundClock || unitsPerSecond <= 0 || acceptedThrough < -1) throw new InvalidOperationException("living.foreground.clock_invalid");
            foregroundUnitsPerSecond = unitsPerSecond;
            foregroundSample = ReadForegroundSample(); foregroundThrough = Math.Max(0, acceptedThrough);
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
            if (_save != null && !_isPaused && foregroundFocused) foregroundThrough = checked(foregroundThrough + checked(sample - foregroundSample));
            foregroundSample = sample; return foregroundThrough;
        }
        internal void SetForegroundFocus(bool focused)
        {
            if (HasForegroundClock) SampleForeground();
            foregroundFocused = focused;
        }
    }
}
