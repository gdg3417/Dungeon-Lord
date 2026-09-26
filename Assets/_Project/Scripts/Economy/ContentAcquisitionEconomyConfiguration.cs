using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Economy
{
    [Serializable]
    public sealed class ContentAcquisitionPrice
    {
        public string OptionId;
        public string CategoryId;
        public double Mana;
    }

    [Serializable]
    public sealed class ContentAcquisitionEconomyConfiguration
    {
        public double StartingMana;
        public ContentAcquisitionPrice[] Prices;
    }

    /// <summary>Immutable acquisition data; the structural economy remains the sole capacity authority.</summary>
    public sealed class ContentAcquisitionEconomySnapshot
    {
        public const string InvalidReason = "content.acquisition.configuration_invalid";
        public const string InsufficientReason = "content.acquisition.insufficient_mana";
        private readonly Dictionary<string, double> prices;
        private readonly string[] orderedOptionIds;
        private ContentAcquisitionEconomySnapshot(ContentAcquisitionEconomyConfiguration config)
        {
            StartingMana = config.StartingMana;
            orderedOptionIds = config.Prices.Select(p => p.OptionId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            prices = config.Prices.ToDictionary(p => p.OptionId, p => p.Mana, StringComparer.Ordinal);
        }
        public double StartingMana { get; }
        public IReadOnlyList<string> OrderedOptionIds => Array.AsReadOnly(orderedOptionIds);
        public bool TryPrice(string categoryId, string optionId, out double mana)
        {
            mana = double.NaN;
            return MvpDungeonPlacementIds.TryGetCategoryForOption(optionId, out string actual) &&
                string.Equals(actual, categoryId, StringComparison.Ordinal) &&
                prices.TryGetValue(optionId, out mana);
        }
        public static bool IsAcquisition(DetachedCanonicalMutationRequest request) =>
            (request?.Kind == DetachedCanonicalMutationKind.PlaceOrReplace &&
             request.CategoryId != MvpDungeonPlacementIds.RoomCategoryId) ||
            request?.Kind == DetachedCanonicalMutationKind.CorridorContentAcquisition;

        public static bool TryCreate(ContentAcquisitionEconomyConfiguration config, StructuralEconomySnapshot economy,
            CanonicalSpatialSerializationLimits limits, out ContentAcquisitionEconomySnapshot result)
        {
            result = null;
            if (!limits.IsValid || economy == null || config?.Prices == null ||
                config.Prices.Length > limits.Serialized.MaximumCollectionRecords ||
                !StructuralEconomySnapshot.Nonnegative(config.StartingMana) || config.StartingMana > economy.ManaCapacity)
                return false;
            string[] required = MvpDungeonPlacementIds.OrderedOptionIds.Where(id =>
                MvpDungeonPlacementIds.TryGetCategoryForOption(id, out string category) &&
                category != MvpDungeonPlacementIds.RoomCategoryId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            if (config.Prices.Any(p => p == null || string.IsNullOrWhiteSpace(p.OptionId) ||
                !StructuralEconomySnapshot.Nonnegative(p.Mana) ||
                !MvpDungeonPlacementIds.TryGetCategoryForOption(p.OptionId, out string category) ||
                !string.Equals(category, p.CategoryId, StringComparison.Ordinal)) ||
                !config.Prices.Select(p => p.OptionId).OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(required))
                return false;
            result = new ContentAcquisitionEconomySnapshot(config);
            return true;
        }
        public static bool TryParse(byte[] bytes, StructuralEconomySnapshot economy,
            CanonicalSpatialSerializationLimits limits, out ContentAcquisitionEconomySnapshot result)
        {
            result = null;
            if (!limits.IsValid) return false;
            try
            {
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(bytes, limits.Serialized, issues, out ContractJsonNode root, allowWhitespace: true) ||
                    !Members(root, "StartingMana", "Prices") || root.Fields[0].Value.Kind != ContractJsonKind.Number ||
                    root.Fields[1].Value.Kind != ContractJsonKind.Array || root.Fields[1].Value.Items.Any(p =>
                        !Members(p, "OptionId", "CategoryId", "Mana") || p.Fields[0].Value.Kind != ContractJsonKind.String ||
                        p.Fields[1].Value.Kind != ContractJsonKind.String || p.Fields[2].Value.Kind != ContractJsonKind.Number))
                    return false;
                if (!double.TryParse(root.Fields[0].Value.Text, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out double startingMana)) return false;
                var records = new List<ContentAcquisitionPrice>();
                foreach (ContractJsonNode price in root.Fields[1].Value.Items)
                {
                    if (!double.TryParse(price.Fields[2].Value.Text, NumberStyles.Float, CultureInfo.InvariantCulture,
                        out double mana)) return false;
                    records.Add(new ContentAcquisitionPrice { OptionId = price.Fields[0].Value.Text,
                        CategoryId = price.Fields[1].Value.Text, Mana = mana });
                }
                return TryCreate(new ContentAcquisitionEconomyConfiguration { StartingMana = startingMana,
                    Prices = records.ToArray() }, economy, limits, out result);
            }
            catch { return false; }
        }
        private static bool Members(ContractJsonNode node, params string[] names) =>
            node?.Kind == ContractJsonKind.Object && node.Fields.Select(f => f.Key).SequenceEqual(names);
    }
}
