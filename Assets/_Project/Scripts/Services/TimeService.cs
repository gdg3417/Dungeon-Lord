using System;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public class TimeService
    {
        private readonly SimpleLogger _logger;
        private readonly int _tickSeconds;
        private readonly int _detectClockSkewSeconds;

        private float _accum;
        private SaveData _save;

        public event Action<long> OnTick;

        public TimeService(SimpleLogger logger, int tickSeconds, int detectClockSkewSeconds)
        {
            _logger = logger;
            _tickSeconds = Mathf.Max(1, tickSeconds);
            _detectClockSkewSeconds = Mathf.Max(1, detectClockSkewSeconds);
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

            _accum += deltaTime;
            if (_accum < _tickSeconds)
            {
                return;
            }

            _accum -= _tickSeconds;
            _save.totalTicks += 1;

            OnTick?.Invoke(_save.totalTicks);
        }

        public void OnPause()
        {
            if (_save == null)
            {
                return;
            }

            _save.lastPausedUtcUnix = TimeUtil.UtcNowUnixSeconds();
        }

        public string OnResume()
        {
            if (_save == null)
            {
                return string.Empty;
            }

            long now = TimeUtil.UtcNowUnixSeconds();
            _save.lastResumedUtcUnix = now;

            if (_save.lastPausedUtcUnix <= 0)
            {
                return string.Empty;
            }

            long delta = now - _save.lastPausedUtcUnix;
            if (Mathf.Abs((float)delta) >= _detectClockSkewSeconds)
            {
                _logger.Warn($"Time delta looks large: {delta} seconds.");
                return "Time change detected. Some offline results may be limited.";
            }

            return string.Empty;
        }
    }
}
