using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEngine;

namespace DungeonBuilder.M0.Economy
{
    [Serializable]
    public sealed class StructuralPrice
    {
        public string DefinitionId;
        public double Mana;
    }

    [Serializable]
    public sealed class StructuralEconomyConfiguration
    {
        public StructuralPrice[] Rooms;
        public StructuralPrice[] Corridors;
        public double MovementFactor;
        public double ReplacementFactor;
        public double RefundPercentage;
        public double ManaCapacity;
        public double UndoSeconds;
    }

    /// <summary>Immutable price/cap authority, separate from geometry and frozen migration inputs.</summary>
    public sealed class StructuralEconomySnapshot
    {
        private readonly Dictionary<string, double> rooms;
        private readonly Dictionary<string, double> corridors;
        private StructuralEconomySnapshot(StructuralEconomyConfiguration config)
        {
            rooms = config.Rooms.OrderBy(p => p.DefinitionId, StringComparer.Ordinal)
                .ToDictionary(p => p.DefinitionId, p => p.Mana, StringComparer.Ordinal);
            corridors = config.Corridors.OrderBy(p => p.DefinitionId, StringComparer.Ordinal)
                .ToDictionary(p => p.DefinitionId, p => p.Mana, StringComparer.Ordinal);
            MovementFactor = config.MovementFactor; ReplacementFactor = config.ReplacementFactor;
            RefundPercentage = config.RefundPercentage; ManaCapacity = config.ManaCapacity;
            UndoSeconds = config.UndoSeconds;
        }
        public double MovementFactor { get; }
        public double ReplacementFactor { get; }
        public double RefundPercentage { get; }
        public double ManaCapacity { get; }
        public double UndoSeconds { get; }
        public bool TryRoom(string id, out double price) => rooms.TryGetValue(id ?? string.Empty, out price);
        public bool TryCorridor(string id, out double price) => corridors.TryGetValue(id ?? string.Empty, out price);
        public static bool Nonnegative(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;

        public static bool TryCreate(StructuralEconomyConfiguration config, SpatialContentCatalog catalog,
            out StructuralEconomySnapshot result)
        {
            result = null;
            if (config == null || catalog?.Rooms == null || catalog.Corridors == null ||
                !PricesValid(config.Rooms, catalog.Rooms.Select(r => r.RoomDefinitionId)) ||
                !PricesValid(config.Corridors, catalog.Corridors.Select(c => c.CorridorDefinitionId)) ||
                !Nonnegative(config.MovementFactor) || config.MovementFactor > 1 ||
                !Nonnegative(config.ReplacementFactor) || config.ReplacementFactor > 1 ||
                !Nonnegative(config.RefundPercentage) || config.RefundPercentage > 1 ||
                !Nonnegative(config.ManaCapacity) || config.ManaCapacity == 0 ||
                !Nonnegative(config.UndoSeconds) || config.UndoSeconds == 0) return false;
            result = new StructuralEconomySnapshot(config); return true;
        }

        public static bool TryParse(byte[] bytes, SpatialContentCatalog catalog,
            CanonicalSpatialSerializationLimits limits, out StructuralEconomySnapshot result)
        {
            result = null;
            try
            {
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(bytes, limits.Serialized, issues, out ContractJsonNode root, allowWhitespace: true) ||
                    !Members(root, "Rooms", "Corridors", "MovementFactor", "ReplacementFactor",
                        "RefundPercentage", "ManaCapacity", "UndoSeconds")) return false;
                foreach (ContractJsonNode list in root.Fields.Take(2).Select(f => f.Value))
                    if (list.Kind != ContractJsonKind.Array || list.Items.Any(p => !Members(p, "DefinitionId", "Mana") ||
                        p.Fields[0].Value.Kind != ContractJsonKind.String || p.Fields[1].Value.Kind != ContractJsonKind.Number))
                        return false;
                if (root.Fields.Skip(2).Any(f => f.Value.Kind != ContractJsonKind.Number)) return false;
                return TryCreate(JsonUtility.FromJson<StructuralEconomyConfiguration>(
                    new UTF8Encoding(false, true).GetString(bytes)), catalog, out result);
            }
            catch { return false; }
        }
        private static bool Members(ContractJsonNode node, params string[] names) =>
            node?.Kind == ContractJsonKind.Object && node.Fields.Select(f => f.Key).SequenceEqual(names);
        private static bool PricesValid(StructuralPrice[] prices, IEnumerable<string> required)
        {
            if (prices == null || prices.Any(p => p == null || string.IsNullOrWhiteSpace(p.DefinitionId) ||
                !Nonnegative(p.Mana)) || prices.GroupBy(p => p.DefinitionId, StringComparer.Ordinal).Any(g => g.Count() != 1))
                return false;
            return prices.Select(p => p.DefinitionId).OrderBy(id => id, StringComparer.Ordinal)
                .SequenceEqual(required.OrderBy(id => id, StringComparer.Ordinal));
        }
        public double AddWithinCapacity(double balance, double addition) =>
            balance + Math.Min(addition, Math.Max(0, ManaCapacity - balance));
    }
}
