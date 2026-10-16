using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    internal static class SchemaFourteenToFifteenUpgrade
    {
        internal static bool TryPrepare(byte[] source, CanonicalSpatialSerializationLimits limits,
            MonsterRecoveryRules rules, out byte[] candidate, IReadOnlyList<LivingDungeonProfile> profiles = null)
        {
            candidate = null;
            var frozen = DetachedCompleteSaveContract.ParseValidateFrozenSchemaFourteenAndRoundTrip(source, limits, profiles);
            if (!frozen.IsValid) return false;
            try
            {
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(source, limits.ForSchema(14).Serialized, issues, out var root)) return false;
                var primary = root.Fields[2].Value;
                if (primary.Fields.Any(f => string.Equals(f.Key, MonsterContinuity.Owner, StringComparison.OrdinalIgnoreCase))) return false;
                var monsters = MonsterContinuity.Initialize(frozen.State, rules);
                if (!MonsterContinuity.Valid(monsters, frozen.State, rules, limits.Spatial.MaximumRecords)) return false;
                var w = new ContractJsonWriter(limits.Serialized); w.Node(); w.Token("{");
                for (int i = 0; i < root.Fields.Count; i++)
                {
                    if (i != 0) w.Token(","); var field = root.Fields[i]; w.String(field.Key); w.Token(":");
                    if (field.Key == "schemaVersion") w.Token("15");
                    else if (field.Key == "primary")
                    {
                        w.Node(); w.Token("{");
                        for (int j = 0; j < primary.Fields.Count; j++)
                        {
                            if (j != 0) w.Token(","); w.String(primary.Fields[j].Key); w.Token(":");
                            DetachedCompleteSaveContract.WriteCanonicalNode(w, primary.Fields[j].Value);
                        }
                        w.Token(","); w.String(MonsterContinuity.Owner); w.Token(":"); MonsterContinuity.Write(w, monsters); w.Token("}");
                    }
                    else DetachedCompleteSaveContract.WriteCanonicalNode(w, field.Value);
                }
                w.Token("}"); var bytes = w.Finish();
                // Current semantic/context validation is repeated by the coordinator before any write.
                if (!DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, limits).IsValid) return false;
                candidate = bytes; return true;
            }
            catch { return false; }
        }
    }
}
