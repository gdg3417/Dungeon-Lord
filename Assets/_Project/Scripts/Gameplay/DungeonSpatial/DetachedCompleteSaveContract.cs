using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class DetachedCompleteSaveValidationResult
    {
        internal DetachedCompleteSaveValidationResult(byte[] bytes, DetachedCanonicalSpatialSaveState spatial,
            string reason)
        { Bytes = bytes == null ? null : (byte[])bytes.Clone(); Spatial = spatial; Reason = reason; }
        public bool IsValid => Bytes != null;
        public byte[] GetBytes() => Bytes == null ? null : (byte[])Bytes.Clone();
        public DetachedCanonicalSpatialSaveState Spatial { get; }
        public string Reason { get; }
        private byte[] Bytes { get; }
    }

    public static class DetachedCompleteSaveContract
    {
        public static DetachedCompleteSaveValidationResult ParseValidateAndRoundTrip(byte[] bytes,
            CanonicalSpatialSerializationLimits limits, ProductionSpatialContentSnapshot production = null)
        {
            if (bytes == null || !limits.IsValid) return Failure();
            try
            {
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(bytes, limits.Serialized, issues, out ContractJsonNode root) ||
                    root.Kind != ContractJsonKind.Object || root.Fields.Count < 3 ||
                    !Field(root, 0, "schema", ContractJsonKind.String) || root.Fields[0].Value.Text != "save_root" ||
                    !Field(root, 1, "schemaVersion", ContractJsonKind.Number) || root.Fields[1].Value.Text != "7" ||
                    !Field(root, 2, "primary", ContractJsonKind.Object)) return Failure();
                if (CaseAmbiguous(root, new[] { "schema", "schemaVersion", "primary" })) return Failure();
                ContractJsonNode primary = root.Fields[2].Value;
                if (primary.Fields.Count < 2 ||
                    primary.Fields[primary.Fields.Count - 2].Key != "canonicalSpatialAuthority" ||
                    primary.Fields[primary.Fields.Count - 1].Key != "spatialFloors" ||
                    CaseAmbiguous(primary, new[] { "canonicalSpatialAuthority", "spatialFloors" })) return Failure();

                var spatialWriter = new ContractJsonWriter(limits.Serialized);
                spatialWriter.Node(); spatialWriter.Token("{"); spatialWriter.String("Authority"); spatialWriter.Token(":");
                WriteNode(spatialWriter, primary.Fields[primary.Fields.Count - 2].Value);
                spatialWriter.Token(","); spatialWriter.String("Floors"); spatialWriter.Token(":");
                WriteNode(spatialWriter, primary.Fields[primary.Fields.Count - 1].Value); spatialWriter.Token("}");
                SpatialContractResult<DetachedCanonicalSpatialSaveState> parsedSpatial =
                    CanonicalSpatialSaveSerializer.Parse(spatialWriter.Finish(), limits);
                if (!parsedSpatial.IsValid || !DefinitionsValid(parsedSpatial.Value, production)) return Failure();

                var completeWriter = new ContractJsonWriter(limits.Serialized);
                WriteNode(completeWriter, root); byte[] again = completeWriter.Finish();
                if (!Same(bytes, again)) return Failure();
                return new DetachedCompleteSaveValidationResult(bytes, parsedSpatial.Value, null);
            }
            catch { return Failure(); }
        }

        private static bool DefinitionsValid(DetachedCanonicalSpatialSaveState state,
            ProductionSpatialContentSnapshot production)
        {
            if (production == null) return true;
            SpatialContentCatalog catalog = production.Catalog;
            var floors = new HashSet<string>((catalog.Floors ?? Array.Empty<FloorSpatialConfiguration>())
                .Where(value => value != null).Select(value => value.FloorDefinitionId), StringComparer.Ordinal);
            var rooms = new HashSet<string>((catalog.Rooms ?? Array.Empty<RoomSpatialDefinition>())
                .Where(value => value != null).Select(value => value.RoomDefinitionId), StringComparer.Ordinal);
            var fixedDefinitions = new HashSet<string>((catalog.FixedStructures ?? Array.Empty<FixedSpatialStructureDefinition>())
                .Where(value => value != null).Select(value => value.StructureDefinitionId), StringComparer.Ordinal);
            foreach (SavedSpatialFloor floor in state.Floors ?? Array.Empty<SavedSpatialFloor>())
            {
                if (!floors.Contains(floor.FloorDefinitionId)) return false;
                if ((floor.Layout.Rooms ?? Array.Empty<RoomSpatialInstance>()).Any(value => !rooms.Contains(value.RoomDefinitionId))) return false;
                if ((floor.FixedStructures ?? Array.Empty<SavedFixedSpatialStructure>()).Any(value =>
                    !fixedDefinitions.Contains(value.FixedStructureDefinitionId))) return false;
            }
            return true;
        }

        private static bool Field(ContractJsonNode node, int index, string name, ContractJsonKind kind) =>
            node.Fields[index].Key == name && node.Fields[index].Value.Kind == kind;
        private static bool CaseAmbiguous(ContractJsonNode node, IEnumerable<string> reserved)
        {
            foreach (KeyValuePair<string, ContractJsonNode> field in node.Fields)
                foreach (string name in reserved)
                    if (field.Key != name && string.Equals(field.Key, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
        private static void WriteNode(ContractJsonWriter writer, ContractJsonNode node)
        {
            writer.Node();
            if (node.Kind == ContractJsonKind.Null) { writer.Token("null"); return; }
            if (node.Kind == ContractJsonKind.String) { writer.String(node.Text); return; }
            if (node.Kind == ContractJsonKind.Number || node.Kind == ContractJsonKind.Boolean)
            { writer.Token(node.Text); return; }
            if (node.Kind == ContractJsonKind.Array)
            {
                writer.Token("["); for (int index = 0; index < node.Items.Count; index++)
                { if (index != 0) writer.Token(","); writer.Record(); WriteNode(writer, node.Items[index]); }
                writer.Token("]"); return;
            }
            writer.Token("{"); for (int index = 0; index < node.Fields.Count; index++)
            { if (index != 0) writer.Token(","); writer.String(node.Fields[index].Key); writer.Token(":"); WriteNode(writer, node.Fields[index].Value); }
            writer.Token("}");
        }
        private static bool Same(byte[] left, byte[] right) => left != null && right != null && left.SequenceEqual(right);
        private static DetachedCompleteSaveValidationResult Failure() =>
            new DetachedCompleteSaveValidationResult(null, null, "gd66.transaction.candidate_invalid");
    }
}
