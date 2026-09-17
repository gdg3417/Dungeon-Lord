using System;

namespace DungeonBuilder.M0
{
    /// <summary>Deterministic active-tick scheduler; both intervals are injected configuration.</summary>
    public sealed class ActivePlaySaveScheduler
    {
        private readonly int tickSeconds;
        private readonly int saveIntervalSeconds;
        private long accumulatedSeconds;

        public ActivePlaySaveScheduler(int tickSeconds, int saveIntervalSeconds)
        {
            if (tickSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(tickSeconds));
            if (saveIntervalSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(saveIntervalSeconds));
            this.tickSeconds = tickSeconds;
            this.saveIntervalSeconds = saveIntervalSeconds;
        }

        public bool AdvanceTick()
        {
            accumulatedSeconds = accumulatedSeconds > long.MaxValue - tickSeconds
                ? saveIntervalSeconds
                : accumulatedSeconds + tickSeconds;
            if (accumulatedSeconds < saveIntervalSeconds)
                return false;
            accumulatedSeconds %= saveIntervalSeconds;
            return true;
        }
    }
}
