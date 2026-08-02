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
                ValidateAuthority(source == null ? null : source.Authority, issues);
                if (issues.Count != 0) return Result<byte[]>(null, issues);
                DetachedCanonicalSpatialSaveState canonical;
                if (!CanonicalSpatialSaveContracts.TryCanonicalize(source, limits.Spatial, out canonical) ||
                    !CanonicalSpatialSaveContracts.Validate(canonical, limits.Spatial, true).IsValid)
                { issues.Add(SpatialContractIssue.StructuralValidationFailed); return Result<byte[]>(null, issues); }
                if (!DeclaredFieldsMatchSerializableFields())
                { issues.Add(SpatialContractIssue.InvalidField); return Result<byte[]>(null, issues); }

                var writer = new ContractJsonWriter(limits.Serialized);
                Write(writer, canonical, typeof(DetachedCanonicalSpatialSaveState));
                return Result(writer.Finish(), issues);
            }
            catch (ContractJsonBudgetException failure)
            { issues.Add(failure.Issue); return Result<byte[]>(null, issues); }
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
                ValidateNode(root, typeof(DetachedCanonicalSpatialSaveState), null, null, issues);
                if (issues.Count != 0) return Result<DetachedCanonicalSpatialSaveState>(null, issues);

                var value = JsonUtility.FromJson<DetachedCanonicalSpatialSaveState>(Encoding.UTF8.GetString(bytes));
                ValidateAuthority(value == null ? null : value.Authority, issues);
                if (issues.Count == 0 && !CanonicalSpatialSaveContracts.Validate(value, limits.Spatial, true).IsValid)
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

        private static void Write(ContractJsonWriter writer, object value, Type type)
        {
            writer.Node();
            if (value == null) { writer.Token("null"); return; }
            if (type == typeof(string)) { writer.String((string)value); return; }
            if (type.IsEnum)
            { writer.Token(Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)); return; }
            if (type == typeof(int) || type == typeof(long))
            { writer.Token(Convert.ToString(value, CultureInfo.InvariantCulture)); return; }
            if (type.IsArray)
            {
                writer.Token("["); int index = 0;
                foreach (object item in (IEnumerable)value)
                {
                    writer.Record();
                    if (index++ != 0) writer.Token(",");
                    Write(writer, item, type.GetElementType());
                }
                writer.Token("]"); return;
            }

            writer.Token("{"); string[] names = Fields[type];
            for (int index = 0; index < names.Length; index++)
            {
                if (index != 0) writer.Token(",");
                writer.String(names[index]); writer.Token(":");
                FieldInfo field = type.GetField(names[index], BindingFlags.Instance | BindingFlags.Public);
                if (field == null) throw new MissingFieldException();
                Write(writer, field.GetValue(value), field.FieldType);
            }
            writer.Token("}");
        }

        private static void ValidateAuthority(CanonicalSpatialAuthorityMarker marker,
            SpatialIssueCollector issues)
        {
            if (marker == null) return;
            if (marker.CreationKind == CanonicalSpatialCreationKind.NativeCanonical)
            {
                if (!string.IsNullOrEmpty(marker.MigrationTransactionId) ||
                    !string.IsNullOrEmpty(marker.MigrationDescriptorFingerprint))
                    issues.Add(SpatialContractIssue.InvalidIdentity);
                return;
            }
            if (marker.CreationKind != CanonicalSpatialCreationKind.Migrated) return;
            if (!SpatialMigrationTransactionIdentity.IsCanonicalTransactionId(marker.MigrationTransactionId))
                issues.Add(SpatialContractIssue.InvalidIdentity);
            if (!SpatialContractSha256.IsCanonical(marker.MigrationDescriptorFingerprint))
                issues.Add(SpatialContractIssue.InvalidHash);
        }

        private static void ValidateNode(ContractJsonNode node, Type type, Type declaringType,
            string fieldName, SpatialIssueCollector issues)
        {
            if (issues.IsExhausted) return;
            if (type == typeof(string))
            {
                bool nullableAuthorityField = declaringType == typeof(CanonicalSpatialAuthorityMarker) &&
                    (fieldName == nameof(CanonicalSpatialAuthorityMarker.MigrationTransactionId) ||
                     fieldName == nameof(CanonicalSpatialAuthorityMarker.MigrationDescriptorFingerprint));
                if (node.Kind != ContractJsonKind.String &&
                    !(nullableAuthorityField && node.Kind == ContractJsonKind.Null))
                    issues.Add(SpatialContractIssue.WrongFieldType);
                return;
            }
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
                { ValidateNode(item, type.GetElementType(), null, null, issues); if (issues.IsExhausted) break; }
                return;
            }
            if (node.Kind == ContractJsonKind.Null) return;
            string[] names = Fields[type];
            if (!ContractJson.ValidateShape(node, names, issues)) return;
            for (int index = 0; index < names.Length && !issues.IsExhausted; index++)
                ValidateNode(ContractJson.Field(node, index), type.GetField(names[index]).FieldType,
                    type, names[index], issues);
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
