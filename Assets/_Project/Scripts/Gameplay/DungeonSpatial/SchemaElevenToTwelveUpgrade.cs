using System;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // Frozen schema 11 is read as it was written. Only the new empty tail owner is added.
    internal static class SchemaElevenToTwelveUpgrade
    {
        internal static bool TryPrepare(byte[] source, CanonicalSpatialSerializationLimits limits,
            out byte[] candidate)
        {
            candidate = null;
            if (!DetachedCompleteSaveContract.ParseValidateFrozenSchemaElevenAndRoundTrip(source, limits).IsValid)
                return false;
            try
            {
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(source, limits.Serialized, issues, out ContractJsonNode root))
                    return false;
                ContractJsonNode primary = root.Fields[2].Value;
                foreach (var field in primary.Fields)
                    if (string.Equals(field.Key, PhaseSixFloorKnowledge.OwnerName,
                        StringComparison.OrdinalIgnoreCase)) return false;
                var writer = new ContractJsonWriter(limits.Serialized);
                writer.Node(); writer.Token("{");
                for (int index = 0; index < root.Fields.Count; index++)
                {
                    if (index != 0) writer.Token(",");
                    var field = root.Fields[index];
                    writer.String(field.Key); writer.Token(":");
                    if (field.Key == "schemaVersion") writer.Token("12");
                    else if (field.Key == "primary")
                    {
                        writer.Node(); writer.Token("{");
                        for (int member = 0; member < primary.Fields.Count; member++)
                        {
                            if (member != 0) writer.Token(",");
                            writer.String(primary.Fields[member].Key); writer.Token(":");
                            DetachedCompleteSaveContract.WriteCanonicalNode(writer, primary.Fields[member].Value);
                        }
                        writer.Token(","); writer.String(PhaseSixFloorKnowledge.OwnerName); writer.Token(":");
                        PhaseSixFloorKnowledge.Write(writer, PhaseSixFloorKnowledge.Empty());
                        writer.Token("}");
                    }
                    else DetachedCompleteSaveContract.WriteCanonicalNode(writer, field.Value);
                }
                writer.Token("}");
                byte[] prepared = writer.Finish();
                if (!DetachedCompleteSaveContract.ParseValidateFrozenSchemaTwelveAndRoundTrip(
                        prepared, limits).IsValid)
                    return false;
                candidate = prepared;
                return true;
            }
            catch { return false; }
        }
    }
}
