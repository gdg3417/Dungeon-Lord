using System;
using System.Linq;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    [Serializable]
    internal sealed class ProductionResearchNode
    {
        public string research_id;
        public string category;
        public string unlock_type;
        public string unlock_target_id;
        public string effect_profile_id;
    }

    [Serializable]
    internal sealed class ProductionResearchNodeCollection
    {
        public ProductionResearchNode[] Nodes = Array.Empty<ProductionResearchNode>();
    }

    [Serializable]
    internal sealed class ProductionArchitectureEffect
    {
        public string eff_id;
        public string effect_type;
        public double value;
        public string unit;
    }

    [Serializable]
    internal sealed class ProductionArchitectureEffectCollection
    {
        public ProductionArchitectureEffect[] ArchitectureEffects =
            Array.Empty<ProductionArchitectureEffect>();
    }

    public sealed class BasicBranchingResearchSnapshot
    {
        internal BasicBranchingResearchSnapshot(int contribution)
        { AllowanceContribution = contribution; }
        public int AllowanceContribution { get; }
    }

    public sealed class BasicBranchingAllowanceResult
    {
        internal BasicBranchingAllowanceResult(bool resolved, int allowance, string reason)
        { IsResolved = resolved; EffectiveAllowance = allowance; Reason = reason; }
        public bool IsResolved { get; }
        public int EffectiveAllowance { get; }
        public string Reason { get; }
    }

    public static class BasicBranchingResearchAuthority
    {
        public const string ResearchId = "ac_300";
        public const string ConfigurationInvalidReason = "branch.research.configuration_invalid";
        public const string ResearchRequiredReason = "branch.research.required";
        private const string Category = "architecture";
        private const string UnlockType = "effect";
        private const string UnlockTarget = "branching";
        private const string EffectType = "max_optional_branch_rooms_per_floor_set";
        private const string Unit = "int";

        public static bool TryParse(TextAsset nodes, TextAsset effects,
            out BasicBranchingResearchSnapshot snapshot) => TryParse(nodes?.text, effects?.text,
                out snapshot);

        public static bool TryParse(string nodesJson, string effectsJson,
            out BasicBranchingResearchSnapshot snapshot)
        {
            snapshot = null;
            try
            {
                if (string.IsNullOrWhiteSpace(nodesJson) || string.IsNullOrWhiteSpace(effectsJson))
                    return false;
                ProductionResearchNodeCollection nodes = JsonUtility.FromJson<ProductionResearchNodeCollection>(
                    "{\"Nodes\":" + nodesJson + "}");
                ProductionArchitectureEffectCollection effects =
                    JsonUtility.FromJson<ProductionArchitectureEffectCollection>(effectsJson);
                ProductionResearchNode[] matches = (nodes?.Nodes ?? Array.Empty<ProductionResearchNode>())
                    .Where(value => value != null && value.research_id == ResearchId).ToArray();
                if (matches.Length != 1) return false;
                ProductionResearchNode node = matches[0];
                if (node.category != Category || node.unlock_type != UnlockType ||
                    node.unlock_target_id != UnlockTarget || string.IsNullOrWhiteSpace(node.effect_profile_id))
                    return false;
                ProductionArchitectureEffect[] effectMatches = (effects?.ArchitectureEffects ??
                    Array.Empty<ProductionArchitectureEffect>()).Where(value => value != null &&
                    value.eff_id == node.effect_profile_id).ToArray();
                if (effectMatches.Length != 1) return false;
                ProductionArchitectureEffect effect = effectMatches[0];
                if (effect.effect_type != EffectType || effect.unit != Unit ||
                    double.IsNaN(effect.value) || double.IsInfinity(effect.value) || effect.value < 0d ||
                    effect.value > int.MaxValue || Math.Floor(effect.value) != effect.value) return false;
                snapshot = new BasicBranchingResearchSnapshot((int)effect.value);
                return true;
            }
            catch { return false; }
        }

        public static BasicBranchingAllowanceResult Resolve(CompletedResearchState completed,
            BasicBranchingResearchSnapshot research, FloorSpatialConfiguration floor)
        {
            if (research == null || floor == null || floor.OptionalBranchAllowance < 0)
                return new BasicBranchingAllowanceResult(false, 0, ConfigurationInvalidReason);
            string[] completedIds = completed?.ProjectIds ?? Array.Empty<string>();
            bool unlocked = completedIds.Any(value => string.Equals(value, ResearchId,
                StringComparison.Ordinal));
            if (!unlocked)
                return new BasicBranchingAllowanceResult(true, 0, ResearchRequiredReason);
            return new BasicBranchingAllowanceResult(true,
                Math.Min(research.AllowanceContribution, floor.OptionalBranchAllowance), null);
        }
    }
}
