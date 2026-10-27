using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // Only the strict complete-save contract writes this ninth owner. Spatial/custody
    // identity and option remain outside it; maximum HP is always configuration-derived.
    public sealed class MonsterContinuityState
    {
        internal MonsterContinuityRecord[] Records;
        internal MonsterContinuityState(MonsterContinuityRecord[] records) { Records = records; }
        public IReadOnlyList<MonsterContinuityRecord> Inspect() => Array.AsReadOnly((MonsterContinuityRecord[])Records.Clone());
        internal MonsterContinuityState Copy() => new MonsterContinuityState(Records.Select(r => r.Copy()).ToArray());
        internal static MonsterContinuityState Empty() => new MonsterContinuityState(Array.Empty<MonsterContinuityRecord>());
    }

    public sealed class MonsterContinuityRecord
    {
        public string AssignmentId { get; }
        public int Level { get; }
        public int Health { get; internal set; }
        public bool RevivalRequired { get; internal set; }
        internal MonsterContinuityRecord(string id, int level, int health, bool revival)
        { AssignmentId = id; Level = level; Health = health; RevivalRequired = revival; }
        internal MonsterContinuityRecord Copy() => new MonsterContinuityRecord(AssignmentId, Level, Health, RevivalRequired);
    }

    // Snapshot combines the existing validated C0 health authority with C2's small
    // configuration-owned recovery policy. It never changes B1 serialized config bytes.
    public sealed class MonsterRecoveryRules
    {
        internal const string Invalid = "combat.c2.invalid_state";
        internal const string Insufficient = "combat.c2.reserve_shortage";
        internal const string Executable = "run.executable.combat-c2.v1";
        internal const string Rule = "run.combat.c2.persistent.v1";
        private readonly Dictionary<string, int> health;
        public int InitialLevel { get; }
        public int ReadinessNumerator { get; }
        public int ReadinessDenominator { get; }
        public int ManaPerHealth { get; }
        [Serializable] private sealed class Policy
        { public int FormatVersion, InitialLevel, ReadinessNumerator, ReadinessDenominator, ManaPerHealth; }
        private MonsterRecoveryRules(CombatC0QualificationConfig combat, Policy policy)
        {
            if (combat == null || policy == null || policy.FormatVersion != 1 || policy.InitialLevel <= 0 ||
                policy.ReadinessNumerator <= 0 || policy.ReadinessDenominator < policy.ReadinessNumerator || policy.ManaPerHealth <= 0)
                throw new ArgumentException(Invalid);
            health = combat.Monsters.ToDictionary(m => m.Id, m => m.MonsterHealth, StringComparer.Ordinal);
            InitialLevel = policy.InitialLevel; ReadinessNumerator = policy.ReadinessNumerator;
            ReadinessDenominator = policy.ReadinessDenominator; ManaPerHealth = policy.ManaPerHealth;
        }
        public static MonsterRecoveryRules Capture(RunSimulationConfig config)
        {
            if (config?.CombatC0?.Capture() == null) return EditorFallback() ?? throw new ArgumentException(Invalid);
            var asset = Resources.Load<TextAsset>("monster_recovery");
            if (asset == null) throw new ArgumentException(Invalid);
            return CaptureBound(config, asset.bytes);
        }
        internal static MonsterRecoveryRules CaptureBound(RunSimulationConfig config, byte[] exactPolicy)
        {
            if (exactPolicy == null || config?.CombatC0?.Capture() == null) throw new ArgumentException(Invalid);
            var limits = new SpatialSerializedInputLimits(4096, 32, 16, 2048, 8);
            var issues = new SpatialIssueCollector(8);
            if (!ContractJson.TryParse(exactPolicy, limits, issues, out var node, true) ||
                !ContractJson.ValidateShape(node, new[] { "FormatVersion", "InitialLevel", "ReadinessNumerator", "ReadinessDenominator", "ManaPerHealth" }, issues) ||
                node.Fields.Any(f => !ContractJson.Int(f.Value, out _))) throw new ArgumentException(Invalid);
            return new MonsterRecoveryRules(config.CombatC0.Capture(), JsonUtility.FromJson<Policy>(System.Text.Encoding.UTF8.GetString(exactPolicy)));
        }
        internal bool SameRules(MonsterRecoveryRules other) => other != null && InitialLevel == other.InitialLevel &&
            ReadinessNumerator == other.ReadinessNumerator && ReadinessDenominator == other.ReadinessDenominator &&
            ManaPerHealth == other.ManaPerHealth && health.Count == other.health.Count &&
            health.All(pair => other.health.TryGetValue(pair.Key, out int maximum) && maximum == pair.Value);
        internal static MonsterRecoveryRules EditorFallback()
        {
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Bootstrap/run_simulation_config.json");
            if (asset != null && BootstrapConfigValidationService.TryParseRunSimulationConfig(asset.text, out var config)) return Capture(config);
#endif
            return null;
        }
        public int MaximumHealth(string option) => health.TryGetValue(option, out int maximum) ? maximum : throw new ArgumentException(Invalid);
        public int Readiness(string option) => checked((int)(((long)MaximumHealth(option) * ReadinessNumerator + ReadinessDenominator - 1) / ReadinessDenominator));
    }

    internal static class MonsterContinuity
    {
        internal const string Owner = "monsterContinuity";
        internal static IEnumerable<(string Id, string Option)> Owned(DetachedCanonicalSpatialSaveState spatial) =>
            spatial.Floors.SelectMany(f => f.RoomContents.Assignments).Where(a => a.CategoryId == CanonicalSpatialSaveContracts.MonsterCategoryId)
                .Select(a => (a.AssignmentId, a.OptionId)).Concat(spatial.LifecycleAndOwnership.ReturnedContents
                    .Where(a => a.CategoryId == CanonicalSpatialSaveContracts.MonsterCategoryId).Select(a => (a.AssignmentId, a.OptionId)));

        internal static bool Valid(MonsterContinuityState value, DetachedCanonicalSpatialSaveState spatial, MonsterRecoveryRules rules, int maximumRecords)
        {
            try
            {
                var owned = Owned(spatial).ToDictionary(a => a.Id, a => a.Option, StringComparer.Ordinal);
                if (value?.Records == null || value.Records.Length > maximumRecords || value.Records.Length != owned.Count ||
                    value.Records.Any(r => r == null) || !value.Records.Select(r => r.AssignmentId).SequenceEqual(owned.Keys.OrderBy(id => id, StringComparer.Ordinal))) return false;
                return value.Records.All(r => rules != null && owned.TryGetValue(r.AssignmentId, out var option) && r.Level == rules.InitialLevel &&
                    r.Health >= 0 && r.Health <= rules.MaximumHealth(option) && (r.Health != 0 || r.RevivalRequired) &&
                    (!r.RevivalRequired || r.Health < rules.Readiness(option)));
            }
            catch { return false; }
        }
        internal static bool IdentitiesValid(MonsterContinuityState value, DetachedCanonicalSpatialSaveState spatial)
        {
            try
            {
                var ids = Owned(spatial).Select(a => a.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();
                return value != null && ids.Distinct(StringComparer.Ordinal).Count() == ids.Length &&
                    value.Records.Select(r => r.AssignmentId).SequenceEqual(ids) &&
                    value.Records.All(r => r.Level > 0 && r.Health >= 0 && (r.Health != 0 || r.RevivalRequired));
            }
            catch { return false; }
        }
        internal static MonsterContinuityState Initialize(DetachedCanonicalSpatialSaveState spatial, MonsterRecoveryRules rules) =>
            new MonsterContinuityState(Owned(spatial).OrderBy(a => a.Id, StringComparer.Ordinal).Select(a =>
                new MonsterContinuityRecord(a.Id, rules.InitialLevel, rules.MaximumHealth(a.Option), false)).ToArray());

        // Reconcile only at an authorized spatial mutation: retain individual state, add
        // healthy new identities, retire only identities no longer owned by spatial/custody.
        internal static MonsterContinuityState Reconcile(MonsterContinuityState prior, DetachedCanonicalSpatialSaveState before,
            DetachedCanonicalSpatialSaveState after, MonsterRecoveryRules rules)
        {
            if (!Valid(prior, before, rules, int.MaxValue)) throw new ArgumentException(MonsterRecoveryRules.Invalid);
            var old = Owned(before).ToDictionary(a => a.Id, a => a.Option, StringComparer.Ordinal);
            var records = prior.Records.ToDictionary(r => r.AssignmentId, StringComparer.Ordinal);
            return new MonsterContinuityState(Owned(after).OrderBy(a => a.Id, StringComparer.Ordinal).Select(a => {
                if (old.TryGetValue(a.Id, out var option))
                { if (option != a.Option) throw new ArgumentException(MonsterRecoveryRules.Invalid); return records[a.Id].Copy(); }
                return new MonsterContinuityRecord(a.Id, rules.InitialLevel, rules.MaximumHealth(a.Option), false);
            }).ToArray());
        }
        internal static void Write(ContractJsonWriter w, MonsterContinuityState value)
        {
            w.Node(); w.Token("{\"FormatVersion\":1,\"Records\":["); w.Node(); bool first = true;
            foreach (var r in value.Records)
            {
                if (!first) w.Token(","); first = false; w.Node(); w.Token("{\"AssignmentId\":"); w.String(r.AssignmentId);
                w.Token(",\"Level\":"); w.Token(r.Level.ToString(CultureInfo.InvariantCulture));
                w.Token(",\"Health\":"); w.Token(r.Health.ToString(CultureInfo.InvariantCulture));
                w.Token(",\"RevivalRequired\":"); w.Token(r.RevivalRequired ? "true" : "false"); w.Token("}");
            }
            w.Token("]}");
        }
        internal static bool TryRead(ContractJsonNode node, int maximumRecords, out MonsterContinuityState value)
        {
            value = null; var issues = new SpatialIssueCollector(8);
            if (!ContractJson.ValidateShape(node, new[] { "FormatVersion", "Records" }, issues) ||
                !ContractJson.Int(node.Fields[0].Value, out int format) || format != 1 || node.Fields[1].Value.Kind != ContractJsonKind.Array ||
                node.Fields[1].Value.Items.Count > maximumRecords) return false;
            var records = new List<MonsterContinuityRecord>();
            foreach (var n in node.Fields[1].Value.Items)
            {
                if (!ContractJson.ValidateShape(n, new[] { "AssignmentId", "Level", "Health", "RevivalRequired" }, issues) ||
                    !ContractJson.String(n.Fields[0].Value, out var id) || string.IsNullOrWhiteSpace(id) ||
                    !ContractJson.Int(n.Fields[1].Value, out int level) || level <= 0 ||
                    !ContractJson.Int(n.Fields[2].Value, out int hp) || hp < 0 || n.Fields[3].Value.Kind != ContractJsonKind.Boolean) return false;
                records.Add(new MonsterContinuityRecord(id, level, hp, n.Fields[3].Value.Text == "true"));
            }
            value = new MonsterContinuityState(records.ToArray()); return true;
        }
        internal static long RequiredReserve(DetachedCanonicalSpatialSaveState spatial, MonsterRecoveryRules rules) =>
            spatial.Floors.Where(f => f.ActivationState == FloorActivationState.Active).SelectMany(f => f.RoomContents.Assignments)
                .Where(a => a.CategoryId == CanonicalSpatialSaveContracts.MonsterCategoryId)
                .Aggregate(0L, (total, a) => checked(total + (long)rules.Readiness(a.OptionId) * rules.ManaPerHealth));

        // Called on a detached complete-save candidate, never in encounter resolution.
        // Occupancy is supplied by the caller; no scheduler or inferred occupancy is created.
        internal static double Recover(MonsterContinuityState value, DetachedCanonicalSpatialSaveState spatial, MonsterRecoveryRules rules,
            double wallet, ISet<string> occupiedFloors)
        {
            if (!Economy.StructuralEconomySnapshot.Nonnegative(wallet) || occupiedFloors == null || !Valid(value, spatial, rules, int.MaxValue))
                throw new ArgumentException(MonsterRecoveryRules.Invalid);
            var records = value.Records.ToDictionary(r => r.AssignmentId, StringComparer.Ordinal);
            var ordered = spatial.Floors.Where(f => f.ActivationState == FloorActivationState.Active && !occupiedFloors.Contains(f.FloorInstanceId))
                .OrderBy(f => f.FloorIndex).ThenBy(f => f.FloorInstanceId, StringComparer.Ordinal)
                .SelectMany(f => f.RoomContents.Assignments.Where(a => a.CategoryId == CanonicalSpatialSaveContracts.MonsterCategoryId)
                    .OrderBy(a => a.Sequence).ThenBy(a => a.AssignmentId, StringComparer.Ordinal)).ToArray();
            foreach (bool revival in new[] { false, true })
                foreach (var a in ordered)
                {
                    var r = records[a.AssignmentId]; int threshold = rules.Readiness(a.OptionId);
                    if (r.RevivalRequired != revival || r.Health >= threshold) continue;
                    int restored = (int)Math.Min(threshold - r.Health, Math.Floor(wallet / rules.ManaPerHealth));
                    if (restored <= 0) return wallet;
                    wallet -= checked((long)restored * rules.ManaPerHealth); r.Health += restored;
                    if (r.Health >= threshold) r.RevivalRequired = false;
                }
            return wallet;
        }
    }
}
