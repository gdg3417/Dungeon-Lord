using System;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    internal static class SchemaFifteenToSixteenUpgrade
    {
        internal static bool TryPrepare(byte[] source, DetachedCurrentTargetValidationContext context, out byte[] candidate)
        {
            candidate = null;
            if (!DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(source, context).IsValid) return false;
            if (!TryPrepareStructure(source, context.Limits, context.LivingDungeonProfiles, out var bytes) ||
                !DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, context).CurrentTargetValidated) return false;
            candidate = bytes; return true;
        }

        // Structural construction grants no writable session/publication authority.
        internal static bool TryPrepareStructure(byte[] source, CanonicalSpatialSerializationLimits limits,
            System.Collections.Generic.IReadOnlyList<LivingDungeonProfile> profiles, out byte[] candidate)
        {
            candidate = null;
            if (!DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(source, limits, profiles).IsValid) return false;
            try
            {
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(source, limits.ForSchema(15).Serialized, issues, out var root)) return false;
                var primary = root.Fields[2].Value;
                if (root.Fields.Concat(primary.Fields).Any(f => string.Equals(f.Key, TimeProvenance.Owner, StringComparison.OrdinalIgnoreCase)) ||
                    !TimeProvenance.TrySeconds(primary.Fields.SingleOrDefault(f => f.Key == "lastSavedUtcUnix").Value, out _)) return false;
                var w = new ContractJsonWriter(limits.Serialized); w.Node(); w.Token("{");
                for (int i = 0; i < root.Fields.Count; i++)
                {
                    if (i != 0) w.Token(","); var f = root.Fields[i]; w.String(f.Key); w.Token(":");
                    if (f.Key == "schemaVersion") w.Token("16");
                    else if (f.Key == "primary")
                    {
                        w.Node(); w.Token("{");
                        for (int j = 0; j < primary.Fields.Count; j++)
                        {
                            if (j != 0) w.Token(","); var p = primary.Fields[j]; w.String(p.Key); w.Token(":");
                            DetachedCompleteSaveContract.WriteCanonicalNode(w, p.Value);
                        }
                        w.Token(","); w.String(TimeProvenance.Owner); w.Token(":"); TimeProvenance.WriteLegacy(w); w.Token("}");
                    }
                    else DetachedCompleteSaveContract.WriteCanonicalNode(w, f.Value);
                }
                w.Token("}"); var bytes = w.Finish();
                candidate = bytes; return true;
            }
            catch { return false; }
        }
    }
}
