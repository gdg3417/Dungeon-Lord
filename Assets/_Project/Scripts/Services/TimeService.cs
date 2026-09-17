using System;

namespace DungeonBuilder.M0
{
    public class TimeService
    {
        public const string ClockAnomalyMessageKey = "ui.time.clock_anomaly_detected";
        private readonly SimpleLogger _logger;
        private readonly SimulationClock _clock;
        private SaveData _save;
        private bool _isPaused;
#if UNITY_EDITOR
        internal SaveData AttachedSaveForTests => _save;
        internal bool IsPausedForTests => _isPaused;
#endif

        public event Action<long> OnTick;
        public bool IsPaused => _isPaused;

        public TimeService(SimpleLogger logger, int tickSeconds, int detectClockSkewSeconds)
            : this(logger, tickSeconds, detectClockSkewSeconds, new SystemTimeSource())
        {
        }

        public TimeService(SimpleLogger logger, int tickSeconds, int detectClockSkewSeconds, ITimeSource timeSource)
        {
            _logger = logger;
            _clock = new SimulationClock(tickSeconds, detectClockSkewSeconds, timeSource);
        }

        public void AttachSave(SaveData save)
        {
            _save = save;
        }

        public void Update(float deltaTime)
        {
            if (_save == null || _isPaused)
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
            if (_save == null || _isPaused)
            {
                return;
            }

            _isPaused = true;
            _save.lastPausedUtcUnix = _clock.MarkPaused();
        }

        public string OnResume()
        {
            if (_save == null || !_isPaused)
            {
                return string.Empty;
            }

            ClockResumeResult result = _clock.ResumeAndDetectSkew();
            _isPaused = false;
            _save.lastResumedUtcUnix = result.ResumedAtUtc;
            if (result.SkewDetected)
            {
                _logger.Warn($"Time delta looks large: {result.PauseDeltaSeconds} seconds.");
                return ClockAnomalyMessageKey;
            }

            return string.Empty;
        }
    }
}
