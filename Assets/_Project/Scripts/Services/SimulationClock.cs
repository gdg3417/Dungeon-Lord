using System;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public enum ClockAuthority
    {
        OfflineLocal = 0,
        OnlineServer = 1
    }

    public interface ITimeSource
    {
        long UtcNowUnixSeconds();
    }

    public sealed class SystemTimeSource : ITimeSource
    {
        public long UtcNowUnixSeconds()
        {
            return TimeUtil.UtcNowUnixSeconds();
        }
    }

    public sealed class SimulationClock
    {
        private readonly int _tickSeconds;
        private readonly int _clockSkewThresholdSeconds;
        private readonly ITimeSource _timeSource;

        private float _accum;
        private long _lastPausedUtc;

        public ClockAuthority Authority { get; private set; } = ClockAuthority.OfflineLocal;

        public SimulationClock(int tickSeconds, int clockSkewThresholdSeconds, ITimeSource timeSource)
        {
            _tickSeconds = Mathf.Max(1, tickSeconds);
            _clockSkewThresholdSeconds = Mathf.Max(1, clockSkewThresholdSeconds);
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
        }

        public void SetAuthority(ClockAuthority authority)
        {
            Authority = authority;
        }

        public bool TryConsumeTick(float deltaTime)
        {
            _accum += deltaTime;
            if (_accum < _tickSeconds)
            {
                return false;
            }

            _accum -= _tickSeconds;
            return true;
        }

        public long MarkPaused()
        {
            _lastPausedUtc = _timeSource.UtcNowUnixSeconds();
            return _lastPausedUtc;
        }

        public ClockResumeResult ResumeAndDetectSkew()
        {
            long now = _timeSource.UtcNowUnixSeconds();
            if (_lastPausedUtc <= 0)
            {
                return new ClockResumeResult(now, 0, false);
            }

            long delta = now - _lastPausedUtc;
            bool skew = Math.Abs(delta) >= _clockSkewThresholdSeconds;
            return new ClockResumeResult(now, delta, skew);
        }
    }

    public readonly struct ClockResumeResult
    {
        public long ResumedAtUtc { get; }
        public long PauseDeltaSeconds { get; }
        public bool SkewDetected { get; }

        public ClockResumeResult(long resumedAtUtc, long pauseDeltaSeconds, bool skewDetected)
        {
            ResumedAtUtc = resumedAtUtc;
            PauseDeltaSeconds = pauseDeltaSeconds;
            SkewDetected = skewDetected;
        }
    }
}
