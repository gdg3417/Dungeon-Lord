using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    internal static class SchemaEightToNineUpgrade
    {
        internal static bool TryPrepare(byte[] source, CanonicalSpatialSerializationLimits limits, out byte[] candidate)
        {
            candidate = null;
            var old = DetachedCompleteSaveContract.ParseValidateFrozenSchemaEightAndRoundTrip(source, limits);
            if (!old.IsValid) return false;
            try
            {
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(source, limits.Serialized, issues, out ContractJsonNode root)) return false;
                var primary = root.Fields[2].Value;
                if (primary.Fields.Any(f => string.Equals(f.Key, "structuralInvestment", System.StringComparison.OrdinalIgnoreCase)))
                    return false;
                var writer = new ContractJsonWriter(limits.Serialized);
                writer.Node(); writer.Token("{\"schema\":\"save_root\",\"schemaVersion\":9,\"primary\":{");
                bool first = true;
                foreach (var field in primary.Fields)
                {
                    if (!first) writer.Token(","); first = false;
                    writer.String(field.Key); writer.Token(":"); DetachedCompleteSaveContract.WriteCanonicalNode(writer, field.Value);
                }
                writer.Token(",\"structuralInvestment\":"); StructuralInvestment.Write(writer, StructuralInvestment.Zero(old.State));
                writer.Token("}");
                foreach (var field in root.Fields.Skip(3))
                { writer.Token(","); writer.String(field.Key); writer.Token(":"); DetachedCompleteSaveContract.WriteCanonicalNode(writer, field.Value); }
                writer.Token("}"); candidate = writer.Finish();
                return DetachedCompleteSaveContract.ParseValidateAndRoundTrip(candidate, limits).IsValid;
            }
            catch { candidate = null; return false; }
        }
    }
}
