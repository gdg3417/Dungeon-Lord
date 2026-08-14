using System;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public class SimpleLogger
    {
        private readonly bool _includeTimestamps;
        private readonly Action<string, string> _sink;

        public SimpleLogger(bool includeTimestamps)
            : this(includeTimestamps, null)
        {
        }

        internal SimpleLogger(bool includeTimestamps, Action<string, string> sink)
        {
            _includeTimestamps = includeTimestamps;
            _sink = sink;
        }

        public void Info(string message)
        {
            Write("INFO", message, Debug.Log);
        }

        public void Warn(string message)
        {
            Write("WARN", message, Debug.LogWarning);
        }

        public void Error(string message)
        {
            Write("ERROR", message, Debug.LogError);
        }

        private void Write(string level, string message, Action<object> unityOutput)
        {
            string formatted = Format(level, message);
            if (_sink != null) _sink(level, formatted);
            else unityOutput(formatted);
        }

        private string Format(string level, string message)
        {
            if (!_includeTimestamps)
            {
                return $"[{level}] {message}";
            }

            string ts = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
            return $"[{ts}] [{level}] {message}";
        }
    }
}
