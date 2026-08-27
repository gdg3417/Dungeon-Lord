using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    /// <summary>Detached, deterministic schema-boundary upgrade. Schema 7 never becomes writable.</summary>
    internal static class SchemaSevenToEightUpgrade
    {
        internal const string InvalidReason = "schema8.upgrade.invalid_schema7";

        internal static bool TryPrepare(byte[] source, CanonicalSpatialSerializationLimits limits,
            out byte[] candidate)
        {
            candidate = null;
            if (!DetachedCompleteSaveContract.ParseValidateFrozenSchemaSevenAndRoundTrip(
                    source, limits).IsValid) return false;
            var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
            if (source == null || !limits.IsValid ||
                !ContractJson.TryParse(source, limits.Serialized, issues, out ContractJsonNode root) ||
                root.Kind != ContractJsonKind.Object || root.Fields.Count < 3 ||
                root.Fields[0].Key != "schema" || root.Fields[0].Value.Text != "save_root" ||
                root.Fields[1].Key != "schemaVersion" || root.Fields[1].Value.Text != "7" ||
                root.Fields[2].Key != "primary" || root.Fields[2].Value.Kind != ContractJsonKind.Object)
                return false;
            ContractJsonNode primary = root.Fields[2].Value;
            ContractJsonNode authority = Find(primary, "canonicalSpatialAuthority");
            ContractJsonNode floors = Find(primary, "spatialFloors");
            if (authority == null || floors?.Kind != ContractJsonKind.Array ||
                Find(primary, "structuralLifecycleAndOwnership") != null) return false;

            var lifecycleWriter = new ContractJsonWriter(limits.Serialized);
            if (!TryWriteInitialLifecycle(lifecycleWriter, floors)) return false;
            ContractJsonNode lifecycle = Parse(lifecycleWriter.Finish(), limits);
            if (lifecycle == null) return false;

            var writer = new ContractJsonWriter(limits.Serialized);
            writer.Node(); writer.Token("{\"schema\":\"save_root\",\"schemaVersion\":8,\"primary\":{");
            bool first = true;
            foreach (KeyValuePair<string, ContractJsonNode> field in primary.Fields)
            {
                if (field.Key == "canonicalSpatialAuthority" || field.Key == "spatialFloors") continue;
                if (!first) writer.Token(","); first = false;
                writer.String(field.Key); writer.Token(":"); DetachedCompleteSaveContract.WriteCanonicalNode(writer, field.Value);
            }
            if (!first) writer.Token(",");
            writer.String("canonicalSpatialAuthority"); writer.Token(":"); DetachedCompleteSaveContract.WriteCanonicalNode(writer, authority);
            writer.Token(",\"spatialFloors\":"); DetachedCompleteSaveContract.WriteCanonicalNode(writer, floors);
            writer.Token(",\"structuralLifecycleAndOwnership\":"); DetachedCompleteSaveContract.WriteCanonicalNode(writer, lifecycle);
            writer.Token("}");
            for (int index = 3; index < root.Fields.Count; index++)
            {
                writer.Token(","); writer.String(root.Fields[index].Key); writer.Token(":");
                DetachedCompleteSaveContract.WriteCanonicalNode(writer, root.Fields[index].Value);
            }
            writer.Token("}"); candidate = writer.Finish();
            return DetachedCompleteSaveContract.ParseValidateAndRoundTrip(candidate, limits).IsValid;
        }

        internal static bool TryWriteInitialLifecycle(ContractJsonWriter lifecycleWriter,
            ContractJsonNode floors)
        {
            if (lifecycleWriter == null || floors?.Kind != ContractJsonKind.Array) return false;
            lifecycleWriter.Node(); lifecycleWriter.Token("{\"Floors\":[");
            var values = new List<Tuple<string, int>>();
            foreach (ContractJsonNode floor in floors.Items)
            {
                string floorId = Text(Find(floor, "FloorInstanceId"));
                ContractJsonNode rooms = Find(Find(floor, "Layout"), "Rooms");
                if (string.IsNullOrWhiteSpace(floorId) || rooms?.Kind != ContractJsonKind.Array) return false;
                int next = 0;
                string prefix = floorId + ".room.player.";
                foreach (ContractJsonNode room in rooms.Items)
                {
                    string id = Text(Find(room, "RoomInstanceId"));
                    if (id == null || !id.StartsWith(prefix, StringComparison.Ordinal)) continue;
                    string suffix = id.Substring(prefix.Length);
                    if (suffix.Length == 4 && int.TryParse(suffix, NumberStyles.None,
                            CultureInfo.InvariantCulture, out int ordinal) && ordinal <= 9999)
                        next = Math.Max(next, ordinal + 1);
                }
                values.Add(Tuple.Create(floorId, next));
            }
            values.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));
            for (int i = 0; i < values.Count; i++)
            {
                if (i != 0) lifecycleWriter.Token(",");
                lifecycleWriter.Token("{\"FloorInstanceId\":"); lifecycleWriter.String(values[i].Item1);
                lifecycleWriter.Token(",\"NextNativeRoomOrdinal\":"); lifecycleWriter.Token(values[i].Item2.ToString(CultureInfo.InvariantCulture));
                lifecycleWriter.Token(",\"NextNativeEdgeOrdinal\":0}");
            }
            lifecycleWriter.Token("],\"ReturnedContents\":[]}");
            return true;
        }

        private static ContractJsonNode Parse(byte[] bytes, CanonicalSpatialSerializationLimits limits)
        {
            var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
            return ContractJson.TryParse(bytes, limits.Serialized, issues, out ContractJsonNode node) ? node : null;
        }
        private static string Text(ContractJsonNode node) => node?.Kind == ContractJsonKind.String ? node.Text : null;
        private static ContractJsonNode Find(ContractJsonNode node, string name)
        {
            if (node?.Kind != ContractJsonKind.Object) return null;
            ContractJsonNode found = null;
            foreach (KeyValuePair<string, ContractJsonNode> field in node.Fields)
                if (field.Key == name) { if (found != null) return null; found = field.Value; }
            return found;
        }
    }
}
