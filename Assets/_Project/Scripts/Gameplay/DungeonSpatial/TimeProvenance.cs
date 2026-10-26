using System;
using System.Globalization;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // No second timestamp: lastSavedUtcUnix is the seconds component of this owner.
    public sealed class TimeProvenance
    {
        public const string Owner = "timeProvenance";
        public const string LegacyMode = "legacyWholeSeconds";
        internal const string LegacyJson = "{\"FormatVersion\":1,\"Mode\":\"legacyWholeSeconds\",\"AccountedFraction100ns\":0}";
        internal static readonly int LegacyMemberByteCount = ContractJson.Bytes(",\"" + Owner + "\":" + LegacyJson).Length;
        public UnixTimePoint AccountedBoundary { get; }
        public string Mode => LegacyMode;
        private TimeProvenance(UnixTimePoint boundary) { AccountedBoundary = boundary; }

        internal static bool TryRead(ContractJsonNode primary, out TimeProvenance result)
        {
            result = null;
            var fields = primary.Fields;
            var owner = fields.SingleOrDefault(f => f.Key == Owner).Value;
            var seconds = fields.SingleOrDefault(f => f.Key == "lastSavedUtcUnix").Value;
            if (owner?.Kind != ContractJsonKind.Object || owner.Fields.Count != 3 ||
                owner.Fields[0].Key != "FormatVersion" || owner.Fields[0].Value.Kind != ContractJsonKind.Number || owner.Fields[0].Value.Text != "1" ||
                owner.Fields[1].Key != "Mode" || owner.Fields[1].Value.Kind != ContractJsonKind.String || owner.Fields[1].Value.Text != LegacyMode ||
                owner.Fields[2].Key != "AccountedFraction100ns" || owner.Fields[2].Value.Kind != ContractJsonKind.Number || owner.Fields[2].Value.Text != "0" ||
                !TrySeconds(seconds, out long value)) return false;
            result = new TimeProvenance(new UnixTimePoint(value, 0)); return true;
        }
        internal static bool TrySeconds(ContractJsonNode node, out long seconds)
        {
            seconds = 0;
            return node?.Kind == ContractJsonKind.Number && long.TryParse(node.Text, NumberStyles.None,
                CultureInfo.InvariantCulture, out seconds) && seconds > 0 && seconds <= UnixTimePoint.MaximumSeconds &&
                node.Text == seconds.ToString(CultureInfo.InvariantCulture);
        }
        internal static void WriteLegacy(ContractJsonWriter writer) { writer.Node(); writer.Token(LegacyJson); }
    }
}
