using System;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    internal static class SchemaThirteenToFourteenUpgrade
    {
        internal const string InvalidReason = "schema14.living_dungeon_migration_failed";
        internal static bool TryPrepare(byte[] source, CanonicalSpatialSerializationLimits limits, out byte[] candidate)
        {
            candidate = null;
            if (!DetachedCompleteSaveContract.ParseValidateFrozenSchemaThirteenAndRoundTrip(source, limits).IsValid) return false;
            try
            {
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(source, limits.Serialized, issues, out var root)) return false;
                var primary = root.Fields[2].Value;
                if (primary.Fields.Any(f => string.Equals(f.Key, LivingDungeonContracts.OwnerName, StringComparison.OrdinalIgnoreCase))) return false;
                var w = new ContractJsonWriter(limits.Serialized); w.Node(); w.Token("{");
                for (int i = 0; i < root.Fields.Count; i++)
                {
                    if (i != 0) w.Token(","); var field = root.Fields[i]; w.String(field.Key); w.Token(":");
                    if (field.Key == "schemaVersion") w.Token("14");
                    else if (field.Key == "primary")
                    {
                        w.Node(); w.Token("{");
                        for (int j = 0; j < primary.Fields.Count; j++)
                        {
                            if (j != 0) w.Token(","); w.String(primary.Fields[j].Key); w.Token(":");
                            DetachedCompleteSaveContract.WriteCanonicalNode(w, primary.Fields[j].Value);
                        }
                        w.Token(","); w.String(LivingDungeonContracts.OwnerName); w.Token(":");
                        LivingDungeonContracts.Write(w, LivingDungeonState.Empty()); w.Token("}");
                    }
                    else DetachedCompleteSaveContract.WriteCanonicalNode(w, field.Value);
                }
                w.Token("}"); var bytes = w.Finish();
                if (!DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, limits).IsValid) return false;
                candidate = bytes; return true;
            }
            catch { return false; }
        }
    }
}
