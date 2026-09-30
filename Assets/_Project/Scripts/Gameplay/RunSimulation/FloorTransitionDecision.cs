using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public static class PhaseSixIdentity
    {
        public static double Roll(params string[] fields)
        {
            var bytes = new List<byte>();
            foreach (string field in fields)
            {
                if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException(PhaseSixRunConfigValidation.Invalid);
                byte[] value = Encoding.UTF8.GetBytes(field);
                uint length = (uint)value.Length;
                bytes.Add((byte)(length >> 24)); bytes.Add((byte)(length >> 16));
                bytes.Add((byte)(length >> 8)); bytes.Add((byte)length); bytes.AddRange(value);
            }
            using (var sha = SHA256.Create())
            {
                byte[] digest = sha.ComputeHash(bytes.ToArray());
                uint word = ((uint)digest[0] << 24) | ((uint)digest[1] << 16) | ((uint)digest[2] << 8) | digest[3];
                return word / 4294967296.0;
            }
        }
    }

    public sealed class TransientDepthObjective
    {
        public string Mode { get; }
        public string RuleSourceId { get; }
        public double PullStrength { get; }
        public int TargetFloorIndex { get; }
        internal TransientDepthObjective(DepthObjectiveWeight definition, string rule)
        {
            Mode = definition.Mode;
            RuleSourceId = rule;
            PullStrength = definition.PullStrength;
            TargetFloorIndex = definition.TargetFloorIndex;
        }
        public double Pull(int currentFloorIndex, bool hasNext)
        {
            if (!hasNext || Mode == "shallow") return 0;
            if (Mode == "deepest_reasonable") return PullStrength;
            return Mode == "target_depth" && currentFloorIndex < TargetFloorIndex ? PullStrength : 0;
        }
        public static TransientDepthObjective Select(PhaseSixRunConfig c, string runId)
        {
            if (!PhaseSixRunConfigValidation.IsValid(c)) throw new ArgumentException(PhaseSixRunConfigValidation.Invalid);
            double roll = PhaseSixIdentity.Roll(c.ObjectiveRuleSourceId, runId);
            double total = c.Objectives.Sum(x => x.Weight), cumulative = 0;
            DepthObjectiveWeight last = null;
            foreach (var entry in c.Objectives.OrderBy(x => x.Mode, StringComparer.Ordinal))
            {
                if (entry.Weight <= 0) continue;
                last = entry; cumulative += entry.Weight / total;
                if (roll < cumulative) return new TransientDepthObjective(entry, c.ObjectiveRuleSourceId);
            }
            return new TransientDepthObjective(last, c.ObjectiveRuleSourceId);
        }
    }

    // Explicit perception input; never accepts canonical layout/content. A4 supplies Unknown.
    public sealed class FloorTransitionPerception
    {
        public bool RewardKnown { get; }
        public bool DangerKnown { get; }
        public double RewardScore { get; }
        public double DangerScore { get; }
        public double Uncertainty { get; }
        public FloorTransitionPerception(bool rewardKnown, double reward, bool dangerKnown, double danger, double uncertainty)
        { RewardKnown = rewardKnown; RewardScore = reward; DangerKnown = dangerKnown; DangerScore = danger; Uncertainty = uncertainty; }
        public static FloorTransitionPerception Unknown(PhaseSixRunConfig c) => new FloorTransitionPerception(false, 0, false, 0, c.UnknownInformationUncertainty);
    }

    public sealed class FloorTransitionEvidence
    {
        public string RunId { get; internal set; }
        public string CurrentFloorInstanceId { get; internal set; }
        public string NextFloorInstanceId { get; internal set; }
        public string Reason { get; internal set; }
        public bool Descend { get; internal set; }
        public double ExpectedSurvivability { get; internal set; }
        public double PartyMinimum { get; internal set; }
        public double Appeal { get; internal set; }
        public double? Roll { get; internal set; }
        public double? Likelihood { get; internal set; }
        public int[] MemberHealth { get; internal set; }
        public int[] FormationOrdinals { get; internal set; }
    }

    public static class FloorTransitionDecision
    {
        public static bool MarginalDescend(double roll, double likelihood) => roll < likelihood;
        public static FloorTransitionEvidence Resolve(PhaseSixRunConfig c, RunParty party,
            string currentFloor, string nextFloor, double carriedLoot, double objectivePull, FloorTransitionPerception perception)
        {
            if (!PhaseSixRunConfigValidation.IsValid(c) || party == null || party.IsWiped ||
                string.IsNullOrWhiteSpace(currentFloor) || !PhaseSixRunConfigValidation.Finite(carriedLoot) || carriedLoot < 0 ||
                !PhaseSixRunConfigValidation.Unit(objectivePull) || perception == null ||
                !PhaseSixRunConfigValidation.Unit(perception.Uncertainty) ||
                !PhaseSixRunConfigValidation.Finite(perception.RewardScore) || perception.RewardScore < 0 ||
                !PhaseSixRunConfigValidation.Finite(perception.DangerScore) || perception.DangerScore < 0)
                throw new ArgumentException(PhaseSixRunConfigValidation.Invalid);
            var e = new FloorTransitionEvidence { RunId = party.RunId, CurrentFloorInstanceId = currentFloor,
                NextFloorInstanceId = nextFloor, MemberHealth = party.Members.OrderBy(m => m.MemberOrdinal).Select(m => m.CurrentHealth).ToArray(),
                FormationOrdinals = party.Formation.Select(m => m.MemberOrdinal).ToArray(), Reason = "run.floor.exit_final" };
            if (nextFloor == null) return e;
            double reward = perception.RewardKnown ? Clamp(perception.RewardScore / c.RewardReference) : 0;
            double danger = perception.DangerKnown ? Clamp(perception.DangerScore / c.DangerReference) : 0;
            double uncertainty = perception.Uncertainty;
            double health = party.Members.Where(m => m.IsActive).Average(m => (double)m.CurrentHealth / m.MaxHealth);
            double condition = c.ConditionHealthWeight * health + c.SurvivorFractionWeight * party.ActiveCount / party.Members.Count;
            e.ExpectedSurvivability = Clamp(condition - c.SurvivabilityThreatPenalty * (c.PerceivedDangerWeight * danger + c.UncertaintyWeight * uncertainty));
            e.PartyMinimum = party.Members.Average(m => c.ProfileMinimums.Single(x => x.ProfileId == m.BehaviorProfileId).MinimumSurvivability);
            if (e.ExpectedSurvivability < e.PartyMinimum) { e.Reason = "run.floor.exit_survivability"; return e; }
            double margin = Clamp((e.ExpectedSurvivability - e.PartyMinimum) / (1 - e.PartyMinimum));
            e.Appeal = Math.Max(-1, Math.Min(1, (c.RewardWeight * reward * party.RewardAppetite +
                c.SurvivabilityMarginWeight * margin + c.ObjectiveWeight * objectivePull -
                c.DangerAppealWeight * danger * (1 - party.RiskTolerance) -
                c.UncertaintyAppealWeight * uncertainty * (1 - party.UncertaintyTolerance) -
                c.CarriedLootPreservationWeight * Clamp(carriedLoot / c.CarriedLootReference) * (1 - party.RiskTolerance)) / c.TotalAppealWeight));
            if (e.Appeal <= c.ExitThreshold) { e.Reason = "run.floor.exit_appeal"; return e; }
            if (e.Appeal >= c.DescendThreshold) { e.Descend = true; e.Reason = "run.floor.descend_appeal"; return e; }
            e.Likelihood = (e.Appeal - c.ExitThreshold) / (c.DescendThreshold - c.ExitThreshold);
            e.Roll = PhaseSixIdentity.Roll(c.TransitionRuleSourceId, party.RunId, currentFloor, nextFloor);
            e.Descend = MarginalDescend(e.Roll.Value, e.Likelihood.Value);
            e.Reason = e.Descend ? "run.floor.descend_marginal" : "run.floor.exit_marginal";
            return e;
        }
        private static double Clamp(double value) => Math.Max(0, Math.Min(1, value));
    }
}
