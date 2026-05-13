using System;

namespace DungeonBuilder.M0
{
    public class TimeService
    {
        private readonly SimpleLogger _logger;
        private readonly SimulationClock _clock;
        private SaveData _save;

        public event Action<long> OnTick;

        public TimeService(SimpleLogger logger, int tickSeconds, int detectClockSkewSeconds)
        {
            _logger = logger;
            _clock = new SimulationClock(tickSeconds, detectClockSkewSeconds, new SystemTimeSource());
        }

        public void AttachSave(SaveData save)
        {
            _save = save;
        }

        public void Update(float deltaTime)
        {
            if (_save == null)
            {
                return;
            }

            if (!_clock.TryConsumeTick(deltaTime))
            {
                return;
            }
            _save.totalTicks += 1;

            OnTick?.Invoke(_save.totalTicks);
        }

        public void OnPause()
        {
            if (_save == null)
            {
                return;
            }

            _save.lastPausedUtcUnix = _clock.MarkPaused();
        }

        public string OnResume()
        {
            if (_save == null)
            {
                return string.Empty;
            }

            ClockResumeResult result = _clock.ResumeAndDetectSkew();
            _save.lastResumedUtcUnix = result.ResumedAtUtc;
            if (result.SkewDetected)
            {
                _logger.Warn($"Time delta looks large: {result.PauseDeltaSeconds} seconds.");
                return "Time change detected. Some offline results may be limited.";
            }

            return string.Empty;
        }
    }
}
