using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    // Transient: neither this roster nor individual HP is a save owner.
    public sealed class RunPartyMember
    {
        public int MemberOrdinal { get; }
        public string ClassId { get; }
        public string BehaviorProfileId { get; }
        public int Level { get; }
        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }
        public bool IsActive => CurrentHealth > 0;
        public double TrapExpertise { get; }
        public int FormationPriority { get; }

        internal RunPartyMember(int ordinal, RunClassProfile c, RunBehaviorProfile behavior, int level)
        {
            MemberOrdinal = ordinal; ClassId = c.ClassId; BehaviorProfileId = behavior.Id;
            Level = level; MaxHealth = c.LevelOneMaxHealth; CurrentHealth = MaxHealth;
            TrapExpertise = c.TrapExpertise; FormationPriority = c.FormationPriority;
        }

        internal void ApplyDamage(int damage)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            CurrentHealth = damage >= CurrentHealth ? 0 : CurrentHealth - damage;
        }
    }

    public sealed class RunParty
    {
        public string RunId { get; }
        public string RuleSourceId { get; }
        public int DeterministicSeed { get; }
        public IReadOnlyList<RunPartyMember> Members { get; }
        public string IntelligenceId { get; }
        public double IntelligenceInterpretationFactor { get; }
        public double RewardAppetite { get; }
        public double RiskTolerance { get; }
        public double UncertaintyTolerance { get; }
        public double RequiredRouteCommitment { get; }
        public int ActiveCount => Members.Count(m => m.IsActive);
        public bool IsWiped => ActiveCount == 0;
        public double ActiveTrapExpertise => Members.Where(m => m.IsActive).Select(m => m.TrapExpertise).DefaultIfEmpty().Max();
        public RunPartyMember[] Formation => Members.Where(m => m.IsActive)
            .OrderBy(m => m.FormationPriority).ThenBy(m => m.MemberOrdinal).ToArray();

        internal RunParty(string runId, string rule, int seed, RunPartyMember[] members,
            RunBehaviorProfile[] behaviors, RunIntelligenceProfile intelligence)
        {
            RunId = runId; RuleSourceId = rule; DeterministicSeed = seed;
            Members = Array.AsReadOnly(members);
            IntelligenceId = intelligence.Id; IntelligenceInterpretationFactor = intelligence.InterpretationFactor;
            // Original membership, immutable equal-weight personality; casualties never recompute it.
            RewardAppetite = behaviors.Average(b => b.RewardAppetite);
            RiskTolerance = behaviors.Average(b => b.RiskTolerance);
            UncertaintyTolerance = behaviors.Average(b => b.UncertaintyTolerance);
            RequiredRouteCommitment = behaviors.Average(b => b.RequiredRouteCommitment);
        }

        public RunSurvivalSummary DeriveSurvival(bool success) => new RunSurvivalSummary {
            PartySize = Members.Count, SurvivorCount = ActiveCount, DeathCount = Members.Count - ActiveCount,
            SurvivorRatio = (double)ActiveCount / Members.Count, RuleResolved = true,
            DeterministicSeed = DeterministicSeed, RuleSourceId = RuleSourceId, SuccessAtResolution = success && !IsWiped
        };
    }

    public static class RunPartyGenerator
    {
        public static RunParty Create(PhaseFiveBConfig config, string runId)
        {
            if (!PhaseFiveBConfigValidation.IsValid(config) || string.IsNullOrWhiteSpace(runId))
                throw new ArgumentException(PhaseFiveBConfigValidation.InvalidConfiguration);
            // Identity tuple: configured rule source, RunId, purpose, zero-based MemberOrdinal.
            // SHA-256 UTF-8 length-prefixed fields; first four digest bytes are an unsigned big-endian word.
            // Sorting stable IDs before selection makes configured collection insertion order irrelevant.
            RunClassProfile[] classes = config.Classes.OrderBy(x => x.ClassId, StringComparer.Ordinal).ToArray();
            RunBehaviorProfile[] profiles = config.Behaviors.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            RunIntelligenceProfile[] bands = config.IntelligenceBands.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            uint seed = IdentityWord(config.PartyRuleSourceId, runId, "size", 0);
            int count = config.MinPartySize + (int)(seed % (uint)(config.MaxPartySize - config.MinPartySize + 1));
            var members = new RunPartyMember[count];
            var behaviors = new RunBehaviorProfile[count];
            for (int i = 0; i < count; i++)
            {
                RunClassProfile c = classes[IdentityWord(config.PartyRuleSourceId, runId, "class", i) % (uint)classes.Length];
                behaviors[i] = profiles[Select(profiles.Select(x => x.Weight).ToArray(),
                    IdentityWord(config.BehaviorRuleSourceId, runId, "behavior", i))];
                members[i] = new RunPartyMember(i, c, behaviors[i], config.InitialLevel);
            }
            RunIntelligenceProfile intelligence = bands[Select(bands.Select(x => x.Weight).ToArray(),
                IdentityWord(config.IntelligenceRuleSourceId, runId, "intelligence", 0))];
            return new RunParty(runId, config.PartyRuleSourceId, unchecked((int)seed), members, behaviors, intelligence);
        }

        private static int Select(double[] weights, uint word)
        {
            double total = weights.Sum();
            double roll = word / 4294967296d;
            double cumulative = 0d;
            int lastPositive = -1;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0d) continue;
                lastPositive = i;
                cumulative += weights[i] / total;
                if (roll < cumulative) return i;
            }
            return lastPositive; // Floating-point summation tail only; never a zero-weight entry.
        }

        private static uint IdentityWord(string rule, string run, string purpose, int ordinal)
        {
            string[] values = { rule, run, purpose, ordinal.ToString(CultureInfo.InvariantCulture) };
            var bytes = new List<byte>();
            foreach (string value in values)
            {
                byte[] field = System.Text.Encoding.UTF8.GetBytes(value);
                uint length = (uint)field.Length;
                bytes.Add((byte)(length >> 24)); bytes.Add((byte)(length >> 16));
                bytes.Add((byte)(length >> 8)); bytes.Add((byte)length); bytes.AddRange(field);
            }
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes.ToArray());
                return ((uint)hash[0] << 24) | ((uint)hash[1] << 16) | ((uint)hash[2] << 8) | hash[3];
            }
        }
    }
}
