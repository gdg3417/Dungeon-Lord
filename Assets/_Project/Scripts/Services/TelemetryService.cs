using System;
using System.Collections.Generic;

namespace DungeonBuilder.M0
{
    public readonly struct TelemetryEvent
    {
        public string EventName { get; }
        public long UtcUnixSeconds { get; }
        public string PayloadJson { get; }

        public TelemetryEvent(string eventName, long utcUnixSeconds, string payloadJson)
        {
            EventName = eventName;
            UtcUnixSeconds = utcUnixSeconds;
            PayloadJson = payloadJson;
        }
    }

    public interface ITelemetryService
    {
        void Track(string eventName, string payloadJson);
        IReadOnlyList<TelemetryEvent> GetBufferedEvents();
        void ClearBufferedEvents();
    }

    public sealed class TelemetryService : ITelemetryService
    {
        private readonly List<TelemetryEvent> _buffer = new List<TelemetryEvent>();
        private readonly SimpleLogger _logger;

        public TelemetryService(SimpleLogger logger)
        {
            _logger = logger;
        }

        public void Track(string eventName, string payloadJson)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            var e = new TelemetryEvent(eventName, TimeUtil.UtcNowUnixSeconds(), payloadJson ?? "{}");
            _buffer.Add(e);
            _logger?.Info($"Telemetry buffered: {eventName}");
        }

        public IReadOnlyList<TelemetryEvent> GetBufferedEvents()
        {
            return _buffer;
        }

        public void ClearBufferedEvents()
        {
            _buffer.Clear();
        }
    }
}
