using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Economy
{
    public enum PassiveOnlineManaConfigurationError
    {
        None = 0,
        Missing = 1,
        Malformed = 2,
        UnsupportedVersion = 3
    }

    public readonly struct PassiveOnlineManaConfigurationLoadResult
    {
        internal PassiveOnlineManaConfigurationLoadResult(
            PassiveOnlineManaConfigurationSnapshot value,
            PassiveOnlineManaConfigurationError error)
        {
            Value = value;
            Error = error;
        }

        public PassiveOnlineManaConfigurationSnapshot Value { get; }
        public PassiveOnlineManaConfigurationError Error { get; }
        public bool IsSuccess => Value != null && Error == PassiveOnlineManaConfigurationError.None;
    }

    /// <summary>
    /// Immutable Phase 4 passive-mana tuning authority. The MVP Core Level is deliberately
    /// named as a temporary baseline input rather than a durable progression field.
    /// </summary>
    public sealed class PassiveOnlineManaConfigurationSnapshot
    {
        public const string ProductionResourcePath = "passive_online_mana";
        public const string ConfigurationUnavailableKey = "mana.passive_online.configuration_invalid";
        private const string ExpectedSchema = "passive_online_mana";
        private const int ExpectedSchemaVersion = 1;

        private static readonly string[] RootFields =
        {
            "Schema", "SchemaVersion", "RuleSourceId", "MvpBaselineCoreLevel",
            "ManaPerCoreLevelPerMinute", "ManaPerActiveFloorPerMinute",
            "HeatEfficiencies", "SoftCap"
        };

        private static readonly string[] RequiredHeatTierIds =
        {
            CurrentHeatTierResolver.ConcernTierId,
            CurrentHeatTierResolver.NoticeTierId,
            CurrentHeatTierResolver.PeaceTierId
        };

        private readonly Dictionary<string, double> heatEfficiencies;

        private PassiveOnlineManaConfigurationSnapshot(
            string ruleSourceId,
            int mvpBaselineCoreLevel,
            double manaPerCoreLevelPerMinute,
            double manaPerActiveFloorPerMinute,
            Dictionary<string, double> efficiencies,
            bool softCapEnabled,
            double? softCapStartManaPerHour,
            double? softCapSlopeManaPerHour)
        {
            RuleSourceId = ruleSourceId;
            MvpBaselineCoreLevel = mvpBaselineCoreLevel;
            ManaPerCoreLevelPerMinute = manaPerCoreLevelPerMinute;
            ManaPerActiveFloorPerMinute = manaPerActiveFloorPerMinute;
            heatEfficiencies = efficiencies;
            SoftCapEnabled = softCapEnabled;
            SoftCapStartManaPerHour = softCapStartManaPerHour;
            SoftCapSlopeManaPerHour = softCapSlopeManaPerHour;
        }

        public string RuleSourceId { get; }
        public int MvpBaselineCoreLevel { get; }
        public double ManaPerCoreLevelPerMinute { get; }
        public double ManaPerActiveFloorPerMinute { get; }
        public bool SoftCapEnabled { get; }
        public double? SoftCapStartManaPerHour { get; }
        public double? SoftCapSlopeManaPerHour { get; }

        public bool TryGetHeatEfficiency(string heatTierId, out double multiplier) =>
            heatEfficiencies.TryGetValue(heatTierId ?? string.Empty, out multiplier);

        public static PassiveOnlineManaConfigurationLoadResult Load(
            byte[] bytes,
            CanonicalSpatialSerializationLimits limits)
        {
            if (bytes == null || bytes.Length == 0)
                return Failure(PassiveOnlineManaConfigurationError.Missing);
            if (!limits.IsValid)
                return Failure(PassiveOnlineManaConfigurationError.Malformed);

            try
            {
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(bytes, limits.Serialized, issues, out ContractJsonNode root,
                        allowWhitespace: true) ||
                    !ContractJson.ValidateShape(root, RootFields, issues))
                    return Failure(PassiveOnlineManaConfigurationError.Malformed);

                if (!ContractJson.String(root.Fields[0].Value, out string schema) ||
                    !string.Equals(schema, ExpectedSchema, StringComparison.Ordinal) ||
                    !ContractJson.Int(root.Fields[1].Value, out int version))
                    return Failure(PassiveOnlineManaConfigurationError.Malformed);
                if (version != ExpectedSchemaVersion)
                    return Failure(PassiveOnlineManaConfigurationError.UnsupportedVersion);
                if (!ContractJson.String(root.Fields[2].Value, out string ruleSourceId) ||
                    string.IsNullOrWhiteSpace(ruleSourceId) ||
                    !ContractJson.Int(root.Fields[3].Value, out int coreLevel) || coreLevel <= 0 ||
                    !TryFiniteNonnegative(root.Fields[4].Value, out double perCore) ||
                    !TryFiniteNonnegative(root.Fields[5].Value, out double perFloor))
                    return Failure(PassiveOnlineManaConfigurationError.Malformed);

                if (!TryHeatEfficiencies(root.Fields[6].Value, out Dictionary<string, double> efficiencies) ||
                    !TrySoftCap(root.Fields[7].Value, out bool enabled, out double? start, out double? slope))
                    return Failure(PassiveOnlineManaConfigurationError.Malformed);

                return new PassiveOnlineManaConfigurationLoadResult(
                    new PassiveOnlineManaConfigurationSnapshot(ruleSourceId, coreLevel, perCore, perFloor,
                        efficiencies, enabled, start, slope),
                    PassiveOnlineManaConfigurationError.None);
            }
            catch
            {
                return Failure(PassiveOnlineManaConfigurationError.Malformed);
            }
        }

        private static bool TryHeatEfficiencies(ContractJsonNode node,
            out Dictionary<string, double> efficiencies)
        {
            efficiencies = null;
            if (node?.Kind != ContractJsonKind.Array || node.Items.Count != RequiredHeatTierIds.Length)
                return false;

            var values = new Dictionary<string, double>(StringComparer.Ordinal);
            string previousId = null;
            for (int index = 0; index < node.Items.Count; index++)
            {
                ContractJsonNode entry = node.Items[index];
                if (!HasExactFields(entry, "HeatTierId", "Multiplier") ||
                    !ContractJson.String(entry.Fields[0].Value, out string tierId) ||
                    string.IsNullOrWhiteSpace(tierId) ||
                    (previousId != null && StringComparer.Ordinal.Compare(previousId, tierId) >= 0) ||
                    !TryFiniteNonnegative(entry.Fields[1].Value, out double multiplier) ||
                    !values.TryAdd(tierId, multiplier))
                    return false;
                previousId = tierId;
            }

            if (!values.Keys.SequenceEqual(RequiredHeatTierIds, StringComparer.Ordinal))
                return false;
            efficiencies = values;
            return true;
        }

        private static bool TrySoftCap(ContractJsonNode node, out bool enabled,
            out double? start, out double? slope)
        {
            enabled = false;
            start = null;
            slope = null;
            if (node?.Kind != ContractJsonKind.Object || node.Fields.Count == 0 ||
                !string.Equals(node.Fields[0].Key, "Enabled", StringComparison.Ordinal) ||
                node.Fields[0].Value.Kind != ContractJsonKind.Boolean ||
                !bool.TryParse(node.Fields[0].Value.Text, out enabled))
                return false;

            if (!enabled)
                return HasExactFields(node, "Enabled");

            if (!HasExactFields(node, "Enabled", "StartManaPerHour", "SlopeManaPerHour") ||
                !TryFiniteNonnegative(node.Fields[1].Value, out double parsedStart) ||
                !TryFinitePositive(node.Fields[2].Value, out double parsedSlope))
                return false;
            start = parsedStart;
            slope = parsedSlope;
            return true;
        }

        private static bool HasExactFields(ContractJsonNode node, params string[] names)
        {
            if (node?.Kind != ContractJsonKind.Object || node.Fields.Count != names.Length)
                return false;
            for (int index = 0; index < names.Length; index++)
                if (!string.Equals(node.Fields[index].Key, names[index], StringComparison.Ordinal))
                    return false;
            return true;
        }

        private static bool TryFiniteNonnegative(ContractJsonNode node, out double value) =>
            TryFinite(node, out value) && value >= 0d;

        private static bool TryFinitePositive(ContractJsonNode node, out double value) =>
            TryFinite(node, out value) && value > 0d;

        private static bool TryFinite(ContractJsonNode node, out double value)
        {
            value = 0d;
            return node?.Kind == ContractJsonKind.Number &&
                double.TryParse(node.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static PassiveOnlineManaConfigurationLoadResult Failure(
            PassiveOnlineManaConfigurationError error) =>
            new PassiveOnlineManaConfigurationLoadResult(null, error);
    }
}
