using System;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public class SimpleLogger
    {
        private readonly bool _includeTimestamps;

        public SimpleLogger(bool includeTimestamps)
        {
            _includeTimestamps = includeTimestamps;
        }

        public void Info(string message)
        {
            Debug.Log(Format("INFO", message));
        }

        public void Warn(string message)
        {
            Debug.LogWarning(Format("WARN", message));
        }

        public void Error(string message)
        {
            Debug.LogError(Format("ERROR", message));
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

