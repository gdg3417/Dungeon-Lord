using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public sealed class RunEncounterEvent
    {
        public string RuleSourceId { get; internal set; }
        public int FloorIndex { get; internal set; }
        public int RoomIndex { get; internal set; }
        public string AssignmentId { get; internal set; }
        public string OptionId { get; internal set; }
        public int MemberOrdinal { get; internal set; }
        public double Severity { get; internal set; }
        public double TrapExpertise { get; internal set; }
        public int UnmitigatedDamage { get; internal set; }
        public int Damage { get; internal set; }
        public int HealthBefore { get; internal set; }
        public int HealthAfter { get; internal set; }
    }

    public static class RunTargetingResolver
    {
        public static RunPartyMember Resolve(RunParty party, string policyId)
        {
            if (policyId != PhaseFiveBConfigValidation.LeadActiveId)
                throw new ArgumentException(PhaseFiveBConfigValidation.InvalidConfiguration);
            return party.Formation.FirstOrDefault();
        }
    }

    public static class RunEncounterResolver
    {
        // Round to nearest integer, exact halves away from zero (all damage is nonnegative).
        // Interpolation rounds once; trap mitigation rounds the resulting absolute HP again.
        public static int Interpolate(int minimum, int maximum, double severity)
        {
            if (minimum < 0 || maximum < minimum || double.IsNaN(severity) || double.IsInfinity(severity))
                throw new ArgumentOutOfRangeException(nameof(severity));
            double bounded = Math.Max(0d, Math.Min(1d, severity));
            return (int)Math.Round(minimum + (maximum - (double)minimum) * bounded, MidpointRounding.AwayFromZero);
        }

        public static RunEncounterEvent Resolve(RunParty party, PhaseFiveBConfig config,
            RunRoomAssignment assignment, int floorIndex, int roomIndex, double severity)
        {
            RunDamageProfile profile = config.DamageProfiles.Single(x =>
                x.OptionId == assignment.OptionId && x.CategoryId == assignment.CategoryId);
            RunPartyMember target = RunTargetingResolver.Resolve(party, profile.TargetingPolicyId);
            if (target == null) return null;
            int raw = Interpolate(profile.MinimumDamage, profile.MaximumDamage, severity);
            bool trap = assignment.CategoryId == MvpDungeonPlacementIds.TrapCategoryId;
            double expertise = trap ? party.ActiveTrapExpertise : 0d;
            double multiplier = trap ? Math.Max(0d, Math.Min(1d, 1d - config.TrapMitigationCoefficient * expertise)) : 1d;
            int damage = (int)Math.Round(raw * multiplier, MidpointRounding.AwayFromZero);
            int before = target.CurrentHealth;
            target.ApplyDamage(damage);
            return new RunEncounterEvent { RuleSourceId = config.EncounterRuleSourceId, FloorIndex = floorIndex, RoomIndex = roomIndex,
                AssignmentId = assignment.AssignmentId, OptionId = assignment.OptionId,
                MemberOrdinal = target.MemberOrdinal, Severity = Math.Max(0d, Math.Min(1d, severity)),
                TrapExpertise = expertise, UnmitigatedDamage = raw, Damage = damage,
                HealthBefore = before, HealthAfter = target.CurrentHealth };
        }
    }
}
