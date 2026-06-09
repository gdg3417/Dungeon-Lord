using System;

namespace DungeonBuilder.M0
{
    public static class RunPostureResolver
    {
        public const string CautiousId = "run.posture.cautious";
        public const string BalancedId = "run.posture.balanced";
        public const string GreedyId = "run.posture.greedy";
        public const string BalancedDisplayNameKey = "run.posture.balanced.name";

        public static RunPostureConfig Resolve(RunSimulationConfig config, string postureId)
        {
            RunPostureConfig balanced = null;
            RunPostureConfig requested = null;
            RunPostureConfig[] postures = config?.RunPostures ?? Array.Empty<RunPostureConfig>();
            string requestedId = string.IsNullOrWhiteSpace(postureId) ? BalancedId : postureId;

            for (int i = 0; i < postures.Length; i++)
            {
                RunPostureConfig candidate = postures[i];
                if (!IsValid(candidate))
                {
                    continue;
                }

                if (string.Equals(candidate.Id, BalancedId, StringComparison.Ordinal))
                {
                    balanced = candidate;
                }

                if (string.Equals(candidate.Id, requestedId, StringComparison.Ordinal))
                {
                    requested = candidate;
                }
            }

            return requested ?? balanced ?? CreateBuiltInBalancedFallback();
        }

        public static string ResolveDisplayNameKey(RunSimulationConfig config, string postureId)
        {
            return Resolve(config, postureId).DisplayNameKey;
        }

        private static RunPostureConfig CreateBuiltInBalancedFallback()
        {
            return new RunPostureConfig
            {
                Id = BalancedId,
                DisplayNameKey = BalancedDisplayNameKey,
                GeneratedLootWorldValueMultiplier = 1d,
                ExtractedLootWorldValueMultiplier = 1d,
                HeatDeltaOffset = 0d
            };
        }

        private static bool IsValid(RunPostureConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.Id) || string.IsNullOrWhiteSpace(config.DisplayNameKey))
            {
                return false;
            }

            return IsValidMultiplier(config.GeneratedLootWorldValueMultiplier) &&
                   IsValidMultiplier(config.ExtractedLootWorldValueMultiplier) &&
                   IsFinite(config.HeatDeltaOffset);
        }

        private static bool IsValidMultiplier(double value)
        {
            return value >= 0d && IsFinite(value);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
