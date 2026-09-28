using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    // All evidence is transient. Unknown facts have no value; zero contributes to the
    // arithmetic while the independent known flags and uncertainty retain that distinction.
    public sealed class BranchDecisionInput
    {
        public RunParty Party;
        public string FloorInstanceId;
        public string OptionalBranchId;
        public bool Applicable;
        public BranchKnowledgeRecord Knowledge;
        public double RemainingRequiredDanger;
    }

    public sealed class BranchDecisionEvidence
    {
        public string RunId, FloorInstanceId, OptionalBranchId;
        public bool Applicable, IncentiveKnown, DangerKnown, Enter;
        public double I, D, U, Q, J;
        public double RewardAppetite, RiskTolerance, UncertaintyTolerance, RequiredRouteCommitment;
        public double AveragePartyHealth, ActiveMemberFraction, ActiveTrapExpertise, PartyMinimumSurvivability;
        public double EffectiveIncentiveConfidence, EffectiveDangerConfidence;
        public double Condition, Threat, MitigatedThreat, ExpectedSurvivability, BranchAppeal;
        public double? EntryLikelihood, DecisionRoll;
        public string Reason;
    }

    public static class BranchDecisionResolver
    {
        public const string InvalidConfiguration = "branch.decision.invalid_configuration";
        public static int StableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            unchecked { int hash = 23; foreach (char character in value) hash = hash * 31 + character; return hash; }
        }

        public static int IdentityHash(string rule, string run, string floor, string branch)
        {
            unchecked
            {
                int hash = 17;
                foreach (string field in new[] { rule, run, floor, branch })
                    hash = hash * 31 + StableStringHash(field);
                return hash;
            }
        }

        public static double Roll(string rule, string run, string floor, string branch) =>
            unchecked((uint)IdentityHash(rule, run, floor, branch)) / 4294967296.0;

        public static bool MarginalEnter(double roll, double likelihood) => roll < likelihood;

        public static BranchDecisionEvidence Resolve(BranchDecisionConfig c, BranchDecisionInput input)
        {
            var e = new BranchDecisionEvidence { Reason = InvalidConfiguration };
            RunParty p = input?.Party;
            if (!PhaseFiveBConfigValidation.ValidDecision(c) || p == null || p.IsWiped ||
                string.IsNullOrWhiteSpace(input.FloorInstanceId) || string.IsNullOrWhiteSpace(input.OptionalBranchId) ||
                !Finite(input.RemainingRequiredDanger) || input.RemainingRequiredDanger < 0d) return e;
            BranchKnowledgeRecord k = input.Knowledge;
            if (input.Applicable && k != null && (!Unit(k.Confidence) || !Unit(k.PerceivedIncentive) || !Unit(k.PerceivedDanger))) return e;
            e.RunId = p.RunId; e.FloorInstanceId = input.FloorInstanceId; e.OptionalBranchId = input.OptionalBranchId;
            e.Applicable = input.Applicable && k != null;
            e.IncentiveKnown = e.Applicable && k.IncentiveKnown;
            e.DangerKnown = e.Applicable && k.DangerKnown;
            e.I = e.IncentiveKnown ? k.PerceivedIncentive : 0d;
            e.D = e.DangerKnown ? k.PerceivedDanger : 0d;
            e.ActiveTrapExpertise = p.ActiveTrapExpertise;
            double confidence = e.Applicable && k.ConfidenceKnown ? k.Confidence : 0d;
            e.EffectiveIncentiveConfidence = e.IncentiveKnown ? Clamp(confidence * p.IntelligenceInterpretationFactor) : 0d;
            e.EffectiveDangerConfidence = e.DangerKnown ? Clamp(confidence * p.IntelligenceInterpretationFactor +
                c.TrapInterpretationConfidenceBonus * e.ActiveTrapExpertise) : 0d;
            e.U = ((1d - e.EffectiveIncentiveConfidence) + (1d - e.EffectiveDangerConfidence)) / 2d;
            e.Q = Clamp(input.RemainingRequiredDanger / c.RequiredRouteDangerReference); e.J = c.InitialInclination;
            e.RewardAppetite = p.RewardAppetite; e.RiskTolerance = p.RiskTolerance;
            e.UncertaintyTolerance = p.UncertaintyTolerance; e.RequiredRouteCommitment = p.RequiredRouteCommitment;
            e.AveragePartyHealth = p.Members.Where(m => m.IsActive).Average(m => (double)m.CurrentHealth / m.MaxHealth);
            e.ActiveMemberFraction = (double)p.ActiveCount / p.Members.Count;
            e.PartyMinimumSurvivability = p.Members.Average(m => c.ProfileMinimums.Single(v => v.ProfileId == m.BehaviorProfileId).MinimumSurvivability);
            e.Condition = c.ConditionHealthWeight * e.AveragePartyHealth + c.ConditionActiveMemberWeight * e.ActiveMemberFraction;
            e.Threat = c.ThreatDangerWeight * e.D + c.ThreatUncertaintyWeight * e.U;
            e.MitigatedThreat = e.Threat * (1d - c.TrapExpertiseThreatMitigation * e.ActiveTrapExpertise);
            e.ExpectedSurvivability = Clamp(e.Condition - c.SurvivabilityThreatPenalty * e.MitigatedThreat);
            if (e.ExpectedSurvivability < e.PartyMinimumSurvivability)
            { e.Reason = "branch.decision.survivability_refusal"; return e; }
            double total = c.RewardWeight + c.DangerWeight + c.UncertaintyWeight + c.ReserveWeight + c.IntentWeight;
            e.BranchAppeal = Math.Max(-1d, Math.Min(1d,
                (c.RewardWeight * e.I * e.RewardAppetite - c.DangerWeight * e.D * (1d - e.RiskTolerance) -
                 c.UncertaintyWeight * e.U * (1d - e.UncertaintyTolerance) - c.ReserveWeight * e.Q * e.RequiredRouteCommitment +
                 c.IntentWeight * e.J) / total));
            if (e.BranchAppeal <= c.SkipThreshold) { e.Reason = "branch.decision.appeal_skip"; return e; }
            if (e.BranchAppeal >= c.EnterThreshold) { e.Enter = true; e.Reason = "branch.decision.appeal_enter"; return e; }
            e.EntryLikelihood = (e.BranchAppeal - c.SkipThreshold) / (c.EnterThreshold - c.SkipThreshold);
            e.DecisionRoll = Roll(c.RuleSourceId, p.RunId, input.FloorInstanceId, input.OptionalBranchId);
            e.Enter = MarginalEnter(e.DecisionRoll.Value, e.EntryLikelihood.Value);
            e.Reason = e.Enter ? "branch.decision.marginal_enter" : "branch.decision.marginal_skip";
            return e;
        }

        internal static double Clamp(double value) => Math.Max(0d, Math.Min(1d, value));
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool Unit(double value) => Finite(value) && value >= 0d && value <= 1d;
    }
}
