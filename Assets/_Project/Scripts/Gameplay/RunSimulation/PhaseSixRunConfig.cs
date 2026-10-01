using System;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    [Serializable]
    public sealed class PhaseSixRunConfig
    {
        public string TransitionRuleSourceId;
        public string ObjectiveRuleSourceId;
        public double ConditionHealthWeight = double.NaN;
        public double SurvivorFractionWeight = double.NaN;
        public double PerceivedDangerWeight = double.NaN;
        public double UncertaintyWeight = double.NaN;
        public double SurvivabilityThreatPenalty = double.NaN;
        public BranchProfileMinimum[] ProfileMinimums;
        public double RewardWeight = double.NaN;
        public double SurvivabilityMarginWeight = double.NaN;
        public double ObjectiveWeight = double.NaN;
        public double DangerAppealWeight = double.NaN;
        public double UncertaintyAppealWeight = double.NaN;
        public double CarriedLootPreservationWeight = double.NaN;
        public double ExitThreshold = double.NaN;
        public double DescendThreshold = double.NaN;
        public double RewardReference = double.NaN;
        public double DangerReference = double.NaN;
        public double CarriedLootReference = double.NaN;
        public double UnknownInformationUncertainty = double.NaN;
        public double InitialObservationConfidence = double.NaN;
        public double ReconfirmationConfidenceIncrease = double.NaN;
        public double ConfidenceClamp = double.NaN;
        public DepthObjectiveWeight[] Objectives;
        public int MaximumActiveFloors;
        public int MaximumTransitions;
        public double TotalAppealWeight => RewardWeight + SurvivabilityMarginWeight + ObjectiveWeight +
            DangerAppealWeight + UncertaintyAppealWeight + CarriedLootPreservationWeight;
    }

    [Serializable]
    public sealed class DepthObjectiveWeight
    {
        public string Mode;
        public double Weight;
        public double PullStrength = double.NaN;
        public int TargetFloorIndex = -1;
    }

    public static class PhaseSixRunConfigValidation
    {
        public const string Invalid = "run.phase6.invalid_configuration";
        public const string TransitionRuleSource = "run.floor_transition_decision.phase6.v1";
        public const string ObjectiveRuleSource = "run.depth_objective.phase6.v1";
        public const int MaximumSupportedActiveFloors = 5;
        public const int MaximumSupportedTransitions = 4;
        internal static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
        internal static bool Unit(double x) => Finite(x) && x >= 0 && x <= 1;
        public static bool IsValid(PhaseSixRunConfig c)
        {
            if (c == null || c.TransitionRuleSourceId != TransitionRuleSource ||
                c.ObjectiveRuleSourceId != ObjectiveRuleSource ||
                c.MaximumActiveFloors < 1 || c.MaximumActiveFloors > MaximumSupportedActiveFloors ||
                c.MaximumTransitions < 0 || c.MaximumTransitions > MaximumSupportedTransitions ||
                c.MaximumTransitions < c.MaximumActiveFloors - 1)
                return false;
            if (!new[] { c.ConditionHealthWeight, c.SurvivorFractionWeight, c.PerceivedDangerWeight,
                c.UncertaintyWeight, c.SurvivabilityThreatPenalty, c.UnknownInformationUncertainty,
                c.InitialObservationConfidence, c.ReconfirmationConfidenceIncrease,
                c.ConfidenceClamp }.All(Unit) ||
                c.InitialObservationConfidence <= 0 || c.ConfidenceClamp <= 0 ||
                c.InitialObservationConfidence > c.ConfidenceClamp ||
                Math.Abs(c.ConditionHealthWeight + c.SurvivorFractionWeight - 1) > double.Epsilon ||
                Math.Abs(c.PerceivedDangerWeight + c.UncertaintyWeight - 1) > double.Epsilon) return false;
            if (!new[] { c.RewardWeight, c.SurvivabilityMarginWeight, c.ObjectiveWeight, c.DangerAppealWeight,
                c.UncertaintyAppealWeight, c.CarriedLootPreservationWeight }.All(x => Finite(x) && x >= 0) ||
                !Finite(c.TotalAppealWeight) || c.TotalAppealWeight <= 0 ||
                !Finite(c.ExitThreshold) || !Finite(c.DescendThreshold) || c.ExitThreshold < -1 ||
                c.DescendThreshold > 1 || c.ExitThreshold >= c.DescendThreshold ||
                !new[] { c.RewardReference, c.DangerReference, c.CarriedLootReference }.All(x => Finite(x) && x > 0)) return false;
            string[] profiles = { "cautious", "greedy", "curious", "goal_oriented", "gambler" };
            if (c.ProfileMinimums == null || c.ProfileMinimums.Length != profiles.Length ||
                c.ProfileMinimums.Any(x => x == null || !profiles.Select(id => "adventurer.behavior_profile." + id).Contains(x.ProfileId) ||
                    !Unit(x.MinimumSurvivability) || x.MinimumSurvivability >= 1) ||
                c.ProfileMinimums.Select(x => x.ProfileId).Distinct(StringComparer.Ordinal).Count() != profiles.Length) return false;
            string[] modes = { "shallow", "target_depth", "deepest_reasonable" };
            return c.Objectives != null && c.Objectives.Length == modes.Length &&
                c.Objectives.All(x => x != null && modes.Contains(x.Mode) && Finite(x.Weight) && x.Weight >= 0 && Unit(x.PullStrength)) &&
                c.Objectives.Select(x => x.Mode).Distinct(StringComparer.Ordinal).Count() == modes.Length &&
                c.Objectives.Single(x => x.Mode == "shallow").PullStrength == 0 &&
                c.Objectives.Single(x => x.Mode == "shallow").TargetFloorIndex == -1 &&
                c.Objectives.Single(x => x.Mode == "deepest_reasonable").TargetFloorIndex == -1 &&
                c.Objectives.Single(x => x.Mode == "target_depth").TargetFloorIndex > 0 &&
                c.Objectives.Single(x => x.Mode == "target_depth").TargetFloorIndex < c.MaximumActiveFloors &&
                Finite(c.Objectives.Sum(x => x.Weight)) && c.Objectives.Sum(x => x.Weight) > 0;
        }
    }
}
