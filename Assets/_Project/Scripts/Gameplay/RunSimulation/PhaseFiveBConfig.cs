using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    [Serializable]
    public sealed class PhaseFiveBConfig
    {
        public int Version;
        public string PartyRuleSourceId;
        public string BehaviorRuleSourceId;
        public string IntelligenceRuleSourceId;
        public string EncounterRuleSourceId;
        public int MinPartySize;
        public int MaxPartySize;
        public int InitialLevel;
        public string CapabilityId;
        public double TrapMitigationCoefficient;
        public RunClassProfile[] Classes;
        public RunBehaviorProfile[] Behaviors;
        public RunIntelligenceProfile[] IntelligenceBands;
        public RunDamageProfile[] DamageProfiles;
    }

    [Serializable]
    public sealed class RunClassProfile
    {
        public string ClassId;
        public int LevelOneMaxHealth;
        public double TrapExpertise;
        public int FormationPriority;
    }

    [Serializable]
    public sealed class RunBehaviorProfile
    {
        public string Id;
        public double Weight;
        public double RewardAppetite;
        public double RiskTolerance;
        public double UncertaintyTolerance;
        public double RequiredRouteCommitment;
    }

    [Serializable]
    public sealed class RunIntelligenceProfile
    {
        public string Id;
        public double InterpretationFactor;
        public double Weight;
    }

    [Serializable]
    public sealed class RunDamageProfile
    {
        public string OptionId;
        public string CategoryId;
        public string TargetingPolicyId;
        public int MinimumDamage;
        public int MaximumDamage;
    }

    public static class PhaseFiveBConfigValidation
    {
        public const string TrapExpertiseId = "adventurer.capability.trap_expertise";
        public const string LeadActiveId = "run.targeting.lead_active";
        public const string InvalidConfiguration = "run.phase5b.invalid_configuration";

        public static bool IsValid(PhaseFiveBConfig c)
        {
            if (c == null || c.Version != 1 || c.MinPartySize < 3 || c.MaxPartySize > 5 ||
                c.MaxPartySize < c.MinPartySize || c.InitialLevel != 1 ||
                c.CapabilityId != TrapExpertiseId || !Unit(c.TrapMitigationCoefficient)) return false;
            string[] rules = { c.PartyRuleSourceId, c.BehaviorRuleSourceId,
                c.IntelligenceRuleSourceId, c.EncounterRuleSourceId };
            if (rules.Any(string.IsNullOrWhiteSpace) || rules.Distinct(StringComparer.Ordinal).Count() != rules.Length) return false;
            if (c.Classes == null || c.Classes.Length != 5 || c.Classes.Any(x => x == null ||
                !AdventurerPartyCompositionResolver.IsMvpClassId(x.ClassId) || x.LevelOneMaxHealth <= 0 ||
                !Unit(x.TrapExpertise) || x.FormationPriority < 1 || x.FormationPriority > c.Classes.Length) ||
                c.Classes.Select(x => x.ClassId).Distinct(StringComparer.Ordinal).Count() != c.Classes.Length ||
                c.Classes.Select(x => x.FormationPriority).Distinct().Count() != c.Classes.Length) return false;
            string[] behaviors = { "cautious", "greedy", "curious", "goal_oriented", "gambler" };
            if (c.Behaviors == null || c.Behaviors.Length != behaviors.Length || c.Behaviors.Any(x => x == null ||
                !behaviors.Any(id => x.Id == "adventurer.behavior_profile." + id) || !Weight(x.Weight) ||
                !Unit(x.RewardAppetite) || !Unit(x.RiskTolerance) || !Unit(x.UncertaintyTolerance) ||
                !Unit(x.RequiredRouteCommitment)) ||
                c.Behaviors.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != c.Behaviors.Length ||
                !PositiveTotal(c.Behaviors.Select(x => x.Weight).ToArray())) return false;
            string[] bands = { "poor", "standard", "good" };
            if (c.IntelligenceBands == null || c.IntelligenceBands.Length != bands.Length ||
                c.IntelligenceBands.Any(x => x == null || !bands.Any(id => x.Id == "adventurer.intelligence." + id) ||
                    !Unit(x.InterpretationFactor) || !Weight(x.Weight)) ||
                c.IntelligenceBands.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != bands.Length ||
                !PositiveTotal(c.IntelligenceBands.Select(x => x.Weight).ToArray())) return false;
            string[] monsters = { MvpDungeonPlacementIds.GoblinOptionId, MvpDungeonPlacementIds.SkeletonOptionId };
            string[] traps = { MvpDungeonPlacementIds.SnareTrapOptionId, MvpDungeonPlacementIds.SpikeTrapOptionId,
                MvpDungeonPlacementIds.ChillingSigilOptionId };
            return c.DamageProfiles != null && c.DamageProfiles.Length == monsters.Length + traps.Length &&
                c.DamageProfiles.All(x => x != null && x.MinimumDamage >= 0 && x.MaximumDamage >= x.MinimumDamage &&
                    x.TargetingPolicyId == LeadActiveId &&
                    (x.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId && monsters.Contains(x.OptionId) ||
                     x.CategoryId == MvpDungeonPlacementIds.TrapCategoryId && traps.Contains(x.OptionId))) &&
                c.DamageProfiles.Select(x => x.OptionId).Distinct(StringComparer.Ordinal).Count() == c.DamageProfiles.Length;
        }

        private static bool PositiveTotal(double[] values) => values.Sum() > 0d && Finite(values.Sum());
        private static bool Weight(double value) => Finite(value) && value >= 0d;
        private static bool Unit(double value) => Weight(value) && value <= 1d;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
