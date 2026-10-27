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
        public string Mode { get; }
        public string RecoveryContextSha256 { get; }
        internal RecoveryEpisode Episode { get; }
        private TimeProvenance(UnixTimePoint boundary) { AccountedBoundary = boundary; Mode = LegacyMode; }

        internal const string PreciseMode = "preciseRecovery";
        internal TimeProvenance(UnixTimePoint boundary, string context, RecoveryEpisode episode)
        {
            if (boundary == null || boundary.Seconds <= 0 || !SpatialContractSha256.IsCanonical(context) ||
                episode != null && !episode.IsValidThrough(boundary)) throw new ArgumentException(RecoveryEpisode.Invalid);
            AccountedBoundary = boundary; Mode = PreciseMode; RecoveryContextSha256 = context; Episode = episode;
        }

        internal static bool TryRead(ContractJsonNode primary, int schema, out TimeProvenance result)
        {
            if (schema == CanonicalSaveSchemaVersions.FrozenSchemaSixteen) return TryRead(primary, out result);
            result = null;
            if (schema != CanonicalSaveSchemaVersions.AtomicRecoveryIntroduction) return false;
            try {
                var owner = primary.Fields.Single(f => f.Key == Owner).Value;
                var issues = new SpatialIssueCollector(8);
                if (!ContractJson.ValidateShape(owner, new[] { "FormatVersion", "Mode", "AccountedFraction100ns", "RecoveryContextSha256", "Episode" }, issues) ||
                    owner.Fields[0].Value.Kind != ContractJsonKind.Number || owner.Fields[0].Value.Text != "2" ||
                    !ContractJson.String(owner.Fields[1].Value, out string mode) || mode != PreciseMode ||
                    !RecoveryEpisode.TryInteger(owner.Fields[2].Value, out long fraction) || fraction > 9999999 ||
                    !ContractJson.String(owner.Fields[3].Value, out string context) || !SpatialContractSha256.IsCanonical(context) ||
                    !TrySeconds(primary.Fields.Single(f => f.Key == "lastSavedUtcUnix").Value, out long seconds)) return false;
                RecoveryEpisode episode = null;
                if (owner.Fields[4].Value.Kind != ContractJsonKind.Null && !RecoveryEpisode.TryRead(owner.Fields[4].Value, out episode)) return false;
                result = new TimeProvenance(new UnixTimePoint(seconds, (int)fraction), context, episode); return true;
            } catch { return false; }
        }

        internal void Write(ContractJsonWriter writer)
        {
            if (Mode == LegacyMode) { WriteLegacy(writer); return; }
            writer.Node(); writer.Token("{\"FormatVersion\":2,\"Mode\":\"preciseRecovery\",\"AccountedFraction100ns\":");
            writer.Token(AccountedBoundary.Fraction100ns.ToString(CultureInfo.InvariantCulture));
            writer.Token(",\"RecoveryContextSha256\":"); writer.String(RecoveryContextSha256); writer.Token(",\"Episode\":");
            if (Episode == null) { writer.Node(); writer.Token("null"); } else Episode.Write(writer);
            writer.Token("}");
        }

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
