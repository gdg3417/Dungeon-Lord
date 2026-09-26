using System;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    internal static class SchemaNineToTenUpgrade
    {
        internal static bool TryPrepare(byte[] source, CanonicalSpatialSerializationLimits limits,
            out byte[] candidate)
        {
            candidate = null;
            DetachedCompleteSaveValidationResult old =
                DetachedCompleteSaveContract.ParseValidateFrozenSchemaNineAndRoundTrip(source, limits);
            if (!old.IsValid) return false;
            try
            {
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(source, limits.Serialized, issues, out ContractJsonNode root))
                    return false;
                ContractJsonNode primary = root.Fields[2].Value;
                if (primary.Fields.Any(field => string.Equals(field.Key,
                        PhaseFiveSaveContracts.CorridorOwnerName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(field.Key, PhaseFiveSaveContracts.KnowledgeOwnerName,
                        StringComparison.OrdinalIgnoreCase))) return false;
                var writer = new ContractJsonWriter(limits.Serialized);
                writer.Node(); writer.Token("{\"schema\":\"save_root\",\"schemaVersion\":10,\"primary\":{");
                bool first = true;
                foreach (var field in primary.Fields)
                {
                    if (!first) writer.Token(","); first = false;
                    writer.String(field.Key); writer.Token(":");
                    DetachedCompleteSaveContract.WriteCanonicalNode(writer, field.Value);
                }
                writer.Token(","); writer.String(PhaseFiveSaveContracts.CorridorOwnerName);
                writer.Token(":"); PhaseFiveSaveContracts.Write(writer,
                    PhaseFiveSaveContracts.EmptyCorridor());
                writer.Token(","); writer.String(PhaseFiveSaveContracts.KnowledgeOwnerName);
                writer.Token(":"); PhaseFiveSaveContracts.Write(writer,
                    PhaseFiveSaveContracts.EmptyKnowledge());
                writer.Token("}");
                foreach (var field in root.Fields.Skip(3))
                {
                    writer.Token(","); writer.String(field.Key); writer.Token(":");
                    DetachedCompleteSaveContract.WriteCanonicalNode(writer, field.Value);
                }
                writer.Token("}"); candidate = writer.Finish();
                return DetachedCompleteSaveContract.ParseValidateAndRoundTrip(candidate, limits).IsValid;
            }
            catch { candidate = null; return false; }
        }
    }
}
