using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public readonly struct CanonicalSpatialSerializationLimits
    {
        public CanonicalSpatialSerializationLimits(SpatialSerializedInputLimits serialized,
            CanonicalSpatialSaveWorkloadLimits spatial)
        { Serialized = serialized; Spatial = spatial; }

        public SpatialSerializedInputLimits Serialized { get; }
        public CanonicalSpatialSaveWorkloadLimits Spatial { get; }
        public bool IsValid => Serialized.IsValid && Spatial.IsValid;
    }

    public static class CanonicalSpatialSaveSerializer
    {
        private static readonly Dictionary<Type, string[]> Fields = new Dictionary<Type, string[]>
        {
            { typeof(DetachedCanonicalSpatialSaveState), new[] { "Authority", "Floors" } },
            { typeof(CanonicalSpatialAuthorityMarker), new[] { "CanonicalLayoutContractVersion", "CreationKind", "MigrationTransactionId", "MigrationDescriptorFingerprint" } },
            { typeof(SavedSpatialFloor), new[] { "FloorInstanceId", "FloorDefinitionId", "FloorIndex", "Layout", "FixedStructures", "RoomContents" } },
            { typeof(FloorSpatialLayout), new[] { "FloorId", "Rooms", "Nodes", "Edges" } },
            { typeof(RoomSpatialInstance), new[] { "RoomInstanceId", "RoomDefinitionId", "FloorId", "Anchor", "Orientation" } },
            { typeof(TileCoordinate), new[] { "X", "Y" } },
            { typeof(FloorRouteNode), new[] { "NodeId", "FloorId", "Kind", "RoomInstanceId" } },
            { typeof(FloorRouteEdge), new[] { "EdgeId", "CorridorDefinitionId", "FloorId", "SourceNodeId", "DestinationNodeId", "Footprint", "Classification", "OptionalBranchId", "ConnectionKind" } },
            { typeof(ResolvedTileFootprint), new[] { "OccupiedTiles" } },
            { typeof(SavedFixedSpatialStructure), new[] { "FixedStructureInstanceId", "FixedStructureDefinitionId", "FloorInstanceId", "Anchor", "Orientation", "Kind" } },
            { typeof(FloorRoomContentState), new[] { "Assignments", "RoomSemantics", "NextSequence" } },
            { typeof(RoomContentAssignment), new[] { "AssignmentId", "RoomInstanceId", "CategoryId", "OptionId", "Sequence" } },
            { typeof(CanonicalRoomSemantics), new[] { "RoomInstanceId", "LegacyRoomOriginKind" } }
        };

        public static SpatialContractResult<byte[]> Serialize(DetachedCanonicalSpatialSaveState source,
            CanonicalSpatialSerializationLimits limits)
        {
            var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
            if (!limits.IsValid)
            { issues.Add(SpatialContractIssue.InvalidLimits); return Result<byte[]>(null, issues); }

            try
            {
                DetachedCanonicalSpatialSaveState canonical;
                if (!CanonicalSpatialSaveContracts.TryCanonicalize(source, limits.Spatial, out canonical) ||
                    !CanonicalSpatialSaveContracts.Validate(canonical, limits.Spatial, true).IsValid)
                { issues.Add(SpatialContractIssue.StructuralValidationFailed); return Result<byte[]>(null, issues); }
                if (!DeclaredFieldsMatchSerializableFields())
                { issues.Add(SpatialContractIssue.InvalidField); return Result<byte[]>(null, issues); }

                var builder = new StringBuilder();
                Write(builder, canonical, typeof(DetachedCanonicalSpatialSaveState));
                byte[] bytes = ContractJson.Bytes(builder.ToString());
                if (bytes.Length > limits.Serialized.MaximumInputBytes)
                { issues.Add(SpatialContractIssue.InputByteLimitExceeded); return Result<byte[]>(null, issues); }
                return Result(bytes, issues);
            }
            catch
            { issues.Add(SpatialContractIssue.InvalidField); return Result<byte[]>(null, issues); }
        }

        public static SpatialContractResult<DetachedCanonicalSpatialSaveState> Parse(byte[] bytes,
            CanonicalSpatialSerializationLimits limits)
        {
            var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
            if (!limits.IsValid)
            { issues.Add(SpatialContractIssue.InvalidLimits); return Result<DetachedCanonicalSpatialSaveState>(null, issues); }

            try
            {
                ContractJsonNode root;
                if (!ContractJson.TryParse(bytes, limits.Serialized, issues, out root))
                    return Result<DetachedCanonicalSpatialSaveState>(null, issues);
                ValidateNode(root, typeof(DetachedCanonicalSpatialSaveState), issues);
                if (issues.Count != 0) return Result<DetachedCanonicalSpatialSaveState>(null, issues);

                var value = JsonUtility.FromJson<DetachedCanonicalSpatialSaveState>(Encoding.UTF8.GetString(bytes));
                if (!CanonicalSpatialSaveContracts.Validate(value, limits.Spatial, true).IsValid)
                    issues.Add(SpatialContractIssue.StructuralValidationFailed);
                if (issues.Count == 0)
                {
                    SpatialContractResult<byte[]> again = Serialize(value, limits);
                    if (!again.IsValid || !BytesEqual(bytes, again.Value)) issues.Add(SpatialContractIssue.NonCanonicalBytes);
                }
                return Result(issues.Count == 0 ? value : null, issues);
            }
            catch
            { issues.Add(SpatialContractIssue.MalformedJson); return Result<DetachedCanonicalSpatialSaveState>(null, issues); }
        }

        public static bool DeclaredFieldsMatchSerializableFields()
        {
            foreach (KeyValuePair<Type, string[]> pair in Fields)
            {
                string[] actual = pair.Key.GetFields(BindingFlags.Instance | BindingFlags.Public)
                    .Select(field => field.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
                string[] declared = pair.Value.OrderBy(name => name, StringComparer.Ordinal).ToArray();
                if (!actual.SequenceEqual(declared)) return false;
            }
            return true;
        }

        private static void Write(StringBuilder builder, object value, Type type)
        {
            if (value == null) { builder.Append("null"); return; }
            if (type == typeof(string)) { ContractJson.AppendString(builder, (string)value); return; }
            if (type.IsEnum)
            { builder.Append(Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)); return; }
            if (type == typeof(int) || type == typeof(long))
            { builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture)); return; }
            if (type.IsArray)
            {
                builder.Append('['); int index = 0;
                foreach (object item in (IEnumerable)value)
                { if (index++ != 0) builder.Append(','); Write(builder, item, type.GetElementType()); }
                builder.Append(']'); return;
            }

            builder.Append('{'); string[] names = Fields[type];
            for (int index = 0; index < names.Length; index++)
            {
                if (index != 0) builder.Append(',');
                ContractJson.AppendString(builder, names[index]); builder.Append(':');
                FieldInfo field = type.GetField(names[index], BindingFlags.Instance | BindingFlags.Public);
                if (field == null) throw new MissingFieldException();
                Write(builder, field.GetValue(value), field.FieldType);
            }
            builder.Append('}');
        }

        private static void ValidateNode(ContractJsonNode node, Type type, SpatialIssueCollector issues)
        {
            if (issues.IsExhausted) return;
            if (type == typeof(string))
            { if (node.Kind != ContractJsonKind.String) issues.Add(SpatialContractIssue.WrongFieldType); return; }
            if (type == typeof(int) || type == typeof(long) || type.IsEnum)
            {
                if (node.Kind != ContractJsonKind.Number)
                { issues.Add(SpatialContractIssue.WrongFieldType); return; }
                int integer = 0; long longInteger;
                bool parsed = type == typeof(long)
                    ? long.TryParse(node.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out longInteger)
                    : int.TryParse(node.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out integer);
                if (!parsed) { issues.Add(SpatialContractIssue.IntegerOverflow); return; }
                if (type.IsEnum && !Enum.IsDefined(type, integer)) issues.Add(SpatialContractIssue.UndefinedEnum);
                return;
            }
            if (type.IsArray)
            {
                if (node.Kind != ContractJsonKind.Array)
                { issues.Add(SpatialContractIssue.WrongFieldType); return; }
                foreach (ContractJsonNode item in node.Items)
                { ValidateNode(item, type.GetElementType(), issues); if (issues.IsExhausted) break; }
                return;
            }
            if (node.Kind == ContractJsonKind.Null) return;
            string[] names = Fields[type];
            if (!ContractJson.ValidateShape(node, names, issues)) return;
            for (int index = 0; index < names.Length && !issues.IsExhausted; index++)
                ValidateNode(ContractJson.Field(node, index), type.GetField(names[index]).FieldType, issues);
        }

        private static SpatialContractResult<T> Result<T>(T value, SpatialIssueCollector issues) =>
            new SpatialContractResult<T>(value, issues.ToArray());
        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int index = 0; index < left.Length; index++) if (left[index] != right[index]) return false;
            return true;
        }
    }
}
