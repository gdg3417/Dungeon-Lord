using System;
using System.Globalization;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    /// <summary>Lossless frozen-schema upgrade: each existing floor becomes explicitly Active.</summary>
    internal static class SchemaTenToElevenUpgrade
    {
        internal static bool TryPrepare(byte[] source, CanonicalSpatialSerializationLimits limits,
            out byte[] candidate)
        {
            candidate = null;
            if (!DetachedCompleteSaveContract.ParseValidateFrozenSchemaTenAndRoundTrip(source, limits).IsValid)
                return false;
            try
            {
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(source, limits.Serialized, issues, out ContractJsonNode root))
                    return false;
                var writer = new ContractJsonWriter(limits.Serialized);
                writer.Node(); writer.Token("{");
                for (int index = 0; index < root.Fields.Count; index++)
                {
                    if (index != 0) writer.Token(",");
                    var field = root.Fields[index];
                    writer.String(field.Key); writer.Token(":");
                    if (field.Key == "schemaVersion") writer.Token("11");
                    else if (field.Key == "primary") WritePrimary(writer, field.Value);
                    else DetachedCompleteSaveContract.WriteCanonicalNode(writer, field.Value);
                }
                writer.Token("}");
                byte[] prepared = writer.Finish();
                if (!DetachedCompleteSaveContract.ParseValidateFrozenSchemaElevenAndRoundTrip(prepared, limits).IsValid)
                    return false;
                candidate = prepared;
                return true;
            }
            catch { return false; }
        }

        private static void WritePrimary(ContractJsonWriter writer, ContractJsonNode primary)
        {
            writer.Node(); writer.Token("{");
            for (int index = 0; index < primary.Fields.Count; index++)
            {
                if (index != 0) writer.Token(",");
                var field = primary.Fields[index];
                writer.String(field.Key); writer.Token(":");
                if (field.Key != "spatialFloors")
                    DetachedCompleteSaveContract.WriteCanonicalNode(writer, field.Value);
                else
                {
                    writer.Node(); writer.Token("[");
                    for (int floorIndex = 0; floorIndex < field.Value.Items.Count; floorIndex++)
                    {
                        if (floorIndex != 0) writer.Token(",");
                        writer.Record(); writer.Node(); writer.Token("{");
                        ContractJsonNode floor = field.Value.Items[floorIndex];
                        for (int memberIndex = 0; memberIndex < floor.Fields.Count; memberIndex++)
                        {
                            if (memberIndex != 0) writer.Token(",");
                            var member = floor.Fields[memberIndex];
                            writer.String(member.Key); writer.Token(":");
                            DetachedCompleteSaveContract.WriteCanonicalNode(writer, member.Value);
                            if (member.Key == "FloorIndex")
                            {
                                writer.Token(","); writer.String(nameof(SavedSpatialFloor.ActivationState));
                                writer.Token(":"); writer.Node();
                                writer.Token(((int)FloorActivationState.Active).ToString(CultureInfo.InvariantCulture));
                            }
                        }
                        writer.Token("}");
                    }
                    writer.Token("]");
                }
            }
            writer.Token("}");
        }
    }
}
