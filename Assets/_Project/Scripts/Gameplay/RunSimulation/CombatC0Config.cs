using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    [Serializable] internal sealed class CombatC0ClassProfileData
    {
        public string Id, AttackId, AttackType;
        public int Damage, Initiative, Range;
    }
    [Serializable] internal sealed class CombatC0MonsterProfileData
    {
        public string Id, AttackId, AttackType;
        public int MaxHealth, Damage, Initiative, Range;
    }

    // Sole authoring source is the CombatC0 section of the existing bootstrap JSON.
    // This DTO is deliberately NONserialized inside RunSimulationConfig, preserving
    // the actual qualified B1 JsonUtility representation and start/checkpoint bytes.
    [Serializable] internal sealed class CombatC0Config
    {
        internal const string Executable = "run.executable.combat-c0.v1";
        internal const string Rule = "run.combat.c0.basic.v1";
        internal const string DamagePolicy = "combat.c0.fixed_integer.v1";
        internal const string InitiativePolicy = "combat.c0.initiative_descending_party_first.v1";
        internal const string TargetPolicy = "combat.c0.targeting.class_priority.v1";
        internal const string PositionPolicy = "combat.c0.party_anchor.distinct_melee.v1";
        internal const string GroupPolicy = "combat.c0.earliest_path_step.room_group.v1";
        internal const string RangePolicy = "combat.c0.manhattan.v1";
        internal const string LosPolicy = "combat.c0.supercover.solid_corner.v1";
        internal const string ZeroPolicy = "combat.c0.zero_damage_round.v1";
        internal const string LimitPolicy = "combat.c0.technical_failure.v1";
        internal const string Melee = "combat.c0.attack.melee";
        internal const string Ranged = "combat.c0.attack.ranged";
        public bool Enabled;
        public int Version;
        public string ExecutableId, RuleSourceId, DamagePolicyId, InitiativePolicyId, TargetingPolicyId;
        public string PositionPolicyId, GroupPolicyId, RangePolicyId, LineOfSightPolicyId, ZeroProgressPolicyId, RoundLimitPolicyId;
        public int MaximumRounds, MaximumParticipants, MaximumEncounters, MaximumEvents;
        public long MaximumWork;
        public string[] MonsterTargetClassOrder;
        public CombatC0ClassProfileData[] Classes;
        public CombatC0MonsterProfileData[] Monsters;

        internal CombatC0QualificationConfig Capture()
        {
            if (!Enabled) return null;
            if (Version != 1 || ExecutableId != Executable || RuleSourceId != Rule || DamagePolicyId != DamagePolicy ||
                InitiativePolicyId != InitiativePolicy || TargetingPolicyId != TargetPolicy || PositionPolicyId != PositionPolicy ||
                GroupPolicyId != GroupPolicy || RangePolicyId != RangePolicy || LineOfSightPolicyId != LosPolicy ||
                ZeroProgressPolicyId != ZeroPolicy || RoundLimitPolicyId != LimitPolicy || Classes == null || Monsters == null ||
                MonsterTargetClassOrder == null || Classes.Any(p => p == null || !AttackValid(p.AttackType, p.Range)) ||
                Monsters.Any(p => p == null || !AttackValid(p.AttackType, p.Range))) Invalid();
            var attackIds = Classes.Select(p => p.AttackId).Concat(Monsters.Select(p => p.AttackId)).ToArray();
            if (attackIds.Distinct(StringComparer.Ordinal).Count() != attackIds.Length) Invalid();
            return new CombatC0QualificationConfig(ExecutableId, TargetingPolicyId, InitiativePolicyId,
                MaximumRounds, MaximumParticipants, MaximumEncounters, MaximumEvents, MaximumWork,
                Classes.Select(p => new CombatC0AttackProfile(p.Id, p.AttackId, p.Damage, p.Initiative, 0,
                    Array.Empty<string>(), p.AttackType, p.Range)).ToArray(),
                Monsters.Select(p => new CombatC0AttackProfile(p.Id, p.AttackId, p.Damage, p.Initiative, p.MaxHealth,
                    MonsterTargetClassOrder, p.AttackType, p.Range)).ToArray());
        }
        private static bool AttackValid(string type, int range) => type == Melee ? range == 1 : type == Ranged && range > 0;
        internal static bool IsValid(CombatC0Config value)
        { try { value?.Capture(); return true; } catch { return false; } }
        private static void Invalid() => throw new ArgumentException(CombatC0QualificationConfig.Invalid);

        [Serializable] private sealed class Envelope { public CombatC0Config CombatC0; }
        internal static bool TryAttach(string json, RunSimulationConfig target)
        {
            try
            {
                // Reuse the existing, frozen resource's parser envelope (no limit changes,
                // copied tuning or second authoring file). This is bootstrap parsing only.
                var limits = SaveSpatialMigrationLimitsLoader.Load(Resources.Load<TextAsset>("save_spatial_migration_limits_schema13")).Profile;
                if (limits == null) return false;
                var issues = new SpatialIssueCollector(limits.Canonical.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(ContractJson.Bytes(json), limits.Canonical.Serialized, issues,
                    out var root, allowWhitespace: true) || root.Kind != ContractJsonKind.Object) return false;
                var fields = root.Fields.Where(f => string.Equals(f.Key, "CombatC0", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (fields.Length == 0) { target.CombatC0 = null; return true; }
                if (fields.Length != 1 || fields[0].Key != "CombatC0") return false;
                var node = fields[0].Value;
                if (node.Kind != ContractJsonKind.Object) return false;
                var enabled = node.Fields.Where(f => string.Equals(f.Key, "Enabled", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (enabled.Length != 1 || enabled[0].Key != "Enabled" || enabled[0].Value.Kind != ContractJsonKind.Boolean) return false;
                if (enabled[0].Value.Text == "true")
                {
                    if (!Shape(node, nameof(Enabled), nameof(Version), nameof(ExecutableId), nameof(RuleSourceId), nameof(DamagePolicyId),
                        nameof(InitiativePolicyId), nameof(TargetingPolicyId), nameof(PositionPolicyId), nameof(GroupPolicyId),
                        nameof(RangePolicyId), nameof(LineOfSightPolicyId), nameof(ZeroProgressPolicyId), nameof(RoundLimitPolicyId),
                        nameof(MaximumRounds), nameof(MaximumParticipants), nameof(MaximumEncounters), nameof(MaximumEvents),
                        nameof(MaximumWork), nameof(MonsterTargetClassOrder), nameof(Classes), nameof(Monsters))) return false;
                    foreach (string id in new[] { nameof(Version), nameof(MaximumRounds), nameof(MaximumParticipants), nameof(MaximumEncounters), nameof(MaximumEvents) })
                        if (!ContractJson.Int(Field(node, id), out _)) return false;
                    if (!ContractJson.Long(Field(node, nameof(MaximumWork)), out _)) return false;
                    foreach (var key in node.Fields.Where(f => f.Key.EndsWith("Id", StringComparison.Ordinal)))
                        if (key.Value.Kind != ContractJsonKind.String) return false;
                    var priorities = Field(node, nameof(MonsterTargetClassOrder));
                    if (priorities.Kind != ContractJsonKind.Array || priorities.Items.Any(p => p.Kind != ContractJsonKind.String)) return false;
                    foreach (string key in new[] { nameof(Classes), nameof(Monsters) })
                    {
                        var array = Field(node, key);
                        if (array.Kind != ContractJsonKind.Array) return false;
                        foreach (var p in array.Items)
                        {
                            var names = key == nameof(Classes) ? new[] { "Id", "AttackId", "AttackType", "Damage", "Initiative", "Range" } :
                                new[] { "Id", "AttackId", "AttackType", "MaxHealth", "Damage", "Initiative", "Range" };
                            if (!Shape(p, names)) return false;
                            foreach (var name in names)
                                if (name == "Id" || name == "AttackId" || name == "AttackType")
                                { if (Field(p, name).Kind != ContractJsonKind.String) return false; }
                                else if (!ContractJson.Int(Field(p, name), out _)) return false;
                        }
                    }
                }
                target.CombatC0 = JsonUtility.FromJson<Envelope>(json).CombatC0;
                return target.CombatC0 != null && IsValid(target.CombatC0);
            }
            catch { return false; }
        }
        private static ContractJsonNode Field(ContractJsonNode node, string name) => node.Fields.Single(f => f.Key == name).Value;
        private static bool Shape(ContractJsonNode node, params string[] fields) => node.Kind == ContractJsonKind.Object &&
            node.Fields.Count == fields.Length && node.Fields.Select(f => f.Key).Distinct(StringComparer.Ordinal).Count() == fields.Length &&
            node.Fields.All(f => fields.Contains(f.Key, StringComparer.Ordinal));
    }
}
