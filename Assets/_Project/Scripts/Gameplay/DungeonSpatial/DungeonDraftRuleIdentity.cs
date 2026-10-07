using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed partial class DungeonDraftContext
    {
        [Serializable]
        private sealed class ConfiguredOptions { public string[] Options; }

        // Hash framed canonical inputs, never localization, clocks or object identities.
        // Production semantic validation reads only MvpPlacementEffects.OptionId from RunSimulationConfig.
        internal string RuleIdentity()
        {
            if (Production == null || Configuration == null || Occupancy == null || Limits == null) return null;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(TransactionalDungeonDraft.CurrentFormatVersion);
                writer.Write(SpatialContractSha256.Compute(ProductionSpatialGeneratedSetParser.SerializeCanonical(Production.Manifest)));
                writer.Write(SpatialContractSha256.Compute(ProductionSpatialGeneratedSetParser.SerializeCanonical(Production.Catalog)));
                writer.Write(Compatibility == null ? string.Empty : SpatialContractSha256.Compute(Compatibility.CanonicalBytes));
                writer.Write(SpatialContractSha256.Compute(DungeonDraftFormat.Bytes(new ConfiguredOptions {
                    Options = (Configuration.MvpPlacementEffects ?? Array.Empty<MvpPlacementEffectConfig>())
                        .Where(value => value != null).Select(value => value.OptionId).Distinct(StringComparer.Ordinal)
                        .OrderBy(value => value, StringComparer.Ordinal).ToArray() })));
                writer.Write(SpatialContractSha256.Compute(ProductionSpatialGeneratedSetParser.SerializeCanonical(Occupancy.Value)));
                writer.Write(Occupancy.MaximumValidationMaterializedTiles);
                var raw = Limits.Raw; var json = Limits.Canonical.Serialized; var spatial = Limits.Canonical.Spatial;
                writer.Write(raw.MaximumInputBytes); writer.Write(raw.MaximumNestingDepth);
                writer.Write(raw.MaximumObjectMembers); writer.Write(raw.MaximumArrayElements);
                writer.Write(raw.MaximumStringBytes); writer.Write(raw.MaximumScanWork);
                writer.Write(json.MaximumInputBytes); writer.Write(json.MaximumParsedNodes);
                writer.Write(json.MaximumCollectionRecords); writer.Write(json.MaximumStringCharacters);
                writer.Write(json.MaximumDiagnostics); writer.Write(spatial.MaximumRecords);
                writer.Write(spatial.MaximumMaterializedTiles);
                writer.Write(Limits.Whole.MaximumCandidateBytes); writer.Write(Limits.Whole.MaximumCopiedValueBytes);
                writer.Write(Limits.Whole.MaximumUnknownMembers); writer.Write(Limits.Whole.MaximumUnknownMemberBytes);
                writer.Flush(); return SpatialContractSha256.Compute(stream.ToArray());
            }
        }
    }
}
