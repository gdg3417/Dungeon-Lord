using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    // Deliberately internal and injection-only. No production loader/default/activation flag.
    // These proposed semantics are executable test prerequisites, pending owner review.
    internal sealed class CombatC0AttackProfile
    {
        internal string Id { get; }
        internal string AttackId { get; }
        internal int Damage { get; }
        internal int Initiative { get; }
        internal int MonsterHealth { get; }
        internal IReadOnlyList<string> TargetClassOrder { get; }
        internal CombatC0AttackProfile(string id, string attackId, int damage, int initiative,
            int monsterHealth, string[] targetClassOrder)
        {
            Id = id; AttackId = attackId; Damage = damage; Initiative = initiative;
            MonsterHealth = monsterHealth;
            TargetClassOrder = targetClassOrder == null ? null : Array.AsReadOnly((string[])targetClassOrder.Clone());
        }
    }

    internal sealed class CombatC0QualificationConfig
    {
        internal const string Rules = "run.executable.combat-c0.qualification.v1";
        internal const string Invalid = "combat.c0.invalid_configuration";
        internal const string Targeting = "combat.c0.qualification.class_priority.v1";
        internal const string InitiativeOrder = "combat.c0.qualification.initiative_descending_party_first.v1";
        internal string RulesId => Rules;
        internal int MaximumRounds { get; }
        internal int MaximumParticipants { get; }
        internal int MaximumEncounters { get; }
        internal int MaximumEvents { get; }
        internal long MaximumWork { get; }
        internal IReadOnlyList<CombatC0AttackProfile> Classes { get; }
        internal IReadOnlyList<CombatC0AttackProfile> Monsters { get; }

        internal CombatC0QualificationConfig(string rulesId, string targetingId, string initiativeId,
            int maximumRounds, int maximumParticipants, int maximumEncounters, int maximumEvents,
            long maximumWork, CombatC0AttackProfile[] classes, CombatC0AttackProfile[] monsters)
        {
            if (rulesId != Rules || targetingId != Targeting || initiativeId != InitiativeOrder ||
                maximumRounds <= 0 || maximumParticipants <= 0 || maximumEncounters <= 0 ||
                maximumEvents <= 0 || maximumWork <= 0 || classes == null || monsters == null)
                throw new ArgumentException(Invalid);
            string[] expectedClasses = { "adventurer.class.warrior", "adventurer.class.rogue",
                "adventurer.class.mage", "adventurer.class.cleric", "adventurer.class.ranger" };
            string[] expectedMonsters = { MvpDungeonPlacementIds.GoblinOptionId, MvpDungeonPlacementIds.SkeletonOptionId };
            Validate(classes, expectedClasses, false, expectedClasses);
            Validate(monsters, expectedMonsters, true, expectedClasses);
            MaximumRounds = maximumRounds; MaximumParticipants = maximumParticipants;
            MaximumEncounters = maximumEncounters; MaximumEvents = maximumEvents; MaximumWork = maximumWork;
            // Profiles and nested priorities are immutable; own collection order too.
            Classes = Array.AsReadOnly(classes.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray());
            Monsters = Array.AsReadOnly(monsters.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray());
        }
        private static void Validate(CombatC0AttackProfile[] profiles, string[] expected,
            bool monster, string[] classes)
        {
            if (profiles.Length != expected.Length || profiles.Any(p => p == null) ||
                !profiles.Select(p => p.Id).OrderBy(x => x, StringComparer.Ordinal)
                    .SequenceEqual(expected.OrderBy(x => x, StringComparer.Ordinal))) throw new ArgumentException(Invalid);
            foreach (var p in profiles)
                if (string.IsNullOrWhiteSpace(p.AttackId) || p.Damage < 0 || p.Initiative < 0 ||
                    (monster ? p.MonsterHealth <= 0 : p.MonsterHealth != 0) || p.TargetClassOrder == null ||
                    (monster ? !p.TargetClassOrder.OrderBy(x => x, StringComparer.Ordinal)
                        .SequenceEqual(classes.OrderBy(x => x, StringComparer.Ordinal)) : p.TargetClassOrder.Count != 0))
                    throw new ArgumentException(Invalid);
        }
    }
}
