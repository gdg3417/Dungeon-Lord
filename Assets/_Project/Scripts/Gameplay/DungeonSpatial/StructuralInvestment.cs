using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DungeonBuilder.M0.Economy;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class StructuralInvestmentRecord
    {
        public string StructureId;
        public double ConstructionMana;
        public double RenovationMana;
        internal StructuralInvestmentRecord Copy() => (StructuralInvestmentRecord)MemberwiseClone();
    }

    /// <summary>
    /// Exactly one record per live room/edge plus one non-refundable shell record for each
    /// constructed deeper floor. Historical Floor 1 saves deliberately have no shell record.
    /// </summary>
    public static class StructuralInvestment
    {
        public static string ShellId(string floorInstanceId) => floorInstanceId + ".shell";
        public static string[] Ids(DetachedCanonicalSpatialSaveState state) => state.Floors.SelectMany(f =>
            f.Layout.Rooms.Select(r => r.RoomInstanceId).Concat(f.Layout.Edges.Select(e => e.EdgeId))
                .Concat(OwnsShell(f) ? new[] { ShellId(f.FloorInstanceId) } : Array.Empty<string>()))
            .OrderBy(id => id, StringComparer.Ordinal).ToArray();
        private static bool OwnsShell(SavedSpatialFloor floor) => floor.FloorIndex > 0 &&
            floor.ActivationState != 0 && string.Equals(floor.FloorInstanceId,
                FloorConstructionService.FloorIdentity(floor.FloorIndex), StringComparison.Ordinal);
        public static StructuralInvestmentRecord[] Zero(DetachedCanonicalSpatialSaveState state) =>
            Ids(state).Select(id => new StructuralInvestmentRecord { StructureId = id }).ToArray();
        public static bool Valid(StructuralInvestmentRecord[] records, DetachedCanonicalSpatialSaveState state,
            int maximumRecords) => records != null && records.Length <= maximumRecords &&
            records.All(r => r != null && StructuralEconomySnapshot.Nonnegative(r.ConstructionMana) &&
                StructuralEconomySnapshot.Nonnegative(r.RenovationMana) &&
                StructuralEconomySnapshot.Nonnegative(r.ConstructionMana + r.RenovationMana)) &&
            records.Select(r => r.StructureId).SequenceEqual(Ids(state)) &&
            records.Select(r => r.StructureId).Distinct(StringComparer.Ordinal).Count() == records.Length;

        internal static void Write(ContractJsonWriter writer, StructuralInvestmentRecord[] records)
        {
            writer.Node(); writer.Token("[");
            for (int i = 0; i < records.Length; i++)
            {
                if (i != 0) writer.Token(",");
                writer.Node(); writer.Token("{\"StructureId\":"); writer.String(records[i].StructureId);
                writer.Token(",\"ConstructionMana\":"); writer.Token(records[i].ConstructionMana.ToString("R", CultureInfo.InvariantCulture));
                writer.Token(",\"RenovationMana\":"); writer.Token(records[i].RenovationMana.ToString("R", CultureInfo.InvariantCulture));
                writer.Token("}");
            }
            writer.Token("]");
        }

        internal static bool TryRead(ContractJsonNode node, DetachedCanonicalSpatialSaveState state,
            int maximumRecords, out StructuralInvestmentRecord[] records)
        {
            records = null;
            if (node?.Kind != ContractJsonKind.Array || node.Items.Count > maximumRecords) return false;
            var result = new List<StructuralInvestmentRecord>();
            foreach (ContractJsonNode item in node.Items)
            {
                if (item.Kind != ContractJsonKind.Object || !item.Fields.Select(f => f.Key).SequenceEqual(
                    new[] { "StructureId", "ConstructionMana", "RenovationMana" }) ||
                    item.Fields[0].Value.Kind != ContractJsonKind.String ||
                    item.Fields[1].Value.Kind != ContractJsonKind.Number || item.Fields[2].Value.Kind != ContractJsonKind.Number ||
                    !double.TryParse(item.Fields[1].Value.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double construction) ||
                    !double.TryParse(item.Fields[2].Value.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double renovation)) return false;
                result.Add(new StructuralInvestmentRecord { StructureId = item.Fields[0].Value.Text,
                    ConstructionMana = construction, RenovationMana = renovation });
            }
            records = result.ToArray(); return Valid(records, state, maximumRecords);
        }
    }
}
