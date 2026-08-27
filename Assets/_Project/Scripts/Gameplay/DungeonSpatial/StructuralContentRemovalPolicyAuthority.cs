using System;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public static class StructuralContentRemovalPolicyAuthority
    {
        public const string MissingOrUnresolvedReason = "schema8.ownership.removal_policy_unresolved";

        public static bool TryResolve(RunSimulationConfig configuration, string categoryId,
            string optionId, out StructuralContentRemovalPolicy policy, out string reason)
        {
            policy = StructuralContentRemovalPolicy.Unresolved; reason = MissingOrUnresolvedReason;
            MvpPlacementEffectConfig[] matches = (configuration?.MvpPlacementEffects ??
                Array.Empty<MvpPlacementEffectConfig>()).Where(value => value != null &&
                string.Equals(value.CategoryId, categoryId, StringComparison.Ordinal) &&
                string.Equals(value.OptionId, optionId, StringComparison.Ordinal)).ToArray();
            if (matches.Length != 1 || matches[0].StructuralRemovalPolicy ==
                StructuralContentRemovalPolicy.Unresolved) return false;
            policy = matches[0].StructuralRemovalPolicy; reason = null; return true;
        }
    }
}
