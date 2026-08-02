using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum SpatialRawEnvelopeClassification { WrappedSaveRoot = 1, UnwrappedSaveData = 2 }

    [Serializable]
    public sealed class SpatialValidationInputHash
    {
        public SpatialValidationInputHash(string inputId, string sha256)
        { InputId = inputId; Sha256 = sha256; }
        public string InputId { get; }
        public string Sha256 { get; }
    }

    [Serializable]
    public sealed class SpatialMigrationInputDescriptor
    {
        private readonly SpatialValidationInputHash[] validationInputHashes;

        public SpatialMigrationInputDescriptor(string originalPayloadSha256, int rawSourceSchemaVersion,
            SpatialRawEnvelopeClassification rawEnvelopeClassification, int selectedTargetSchemaVersion,
            int authorityMarkerContractVersion, int migrationContractVersion, string migrationProfileId,
            int migrationProfileVersion, string migrationProfileCanonicalHash, string sharedGeometryId,
            int sharedGeometryVersion, string sharedGeometryCanonicalHash, string productionManifestSha256,
            string productionCatalogSha256, IEnumerable<SpatialValidationInputHash> validationInputHashes,
            string legacyGameplayConfigurationSha256, string canonicalSerializerId,
            int canonicalSerializerVersion)
        {
            OriginalPayloadSha256 = originalPayloadSha256;
            RawSourceSchemaVersion = rawSourceSchemaVersion;
            RawEnvelopeClassification = rawEnvelopeClassification;
            SelectedTargetSchemaVersion = selectedTargetSchemaVersion;
            AuthorityMarkerContractVersion = authorityMarkerContractVersion;
            MigrationContractVersion = migrationContractVersion;
            MigrationProfileId = migrationProfileId;
            MigrationProfileVersion = migrationProfileVersion;
            MigrationProfileCanonicalHash = migrationProfileCanonicalHash;
            SharedGeometryId = sharedGeometryId;
            SharedGeometryVersion = sharedGeometryVersion;
            SharedGeometryCanonicalHash = sharedGeometryCanonicalHash;
            ProductionManifestSha256 = productionManifestSha256;
            ProductionCatalogSha256 = productionCatalogSha256;
            this.validationInputHashes = (validationInputHashes ?? Enumerable.Empty<SpatialValidationInputHash>())
                .OrderBy(value => value == null ? null : value.InputId, StringComparer.Ordinal).ToArray();
            LegacyGameplayConfigurationSha256 = legacyGameplayConfigurationSha256;
            CanonicalSerializerId = canonicalSerializerId;
            CanonicalSerializerVersion = canonicalSerializerVersion;
        }

        public string OriginalPayloadSha256 { get; }
        public int RawSourceSchemaVersion { get; }
        public SpatialRawEnvelopeClassification RawEnvelopeClassification { get; }
        public int SelectedTargetSchemaVersion { get; }
        public int AuthorityMarkerContractVersion { get; }
        public int MigrationContractVersion { get; }
        public string MigrationProfileId { get; }
        public int MigrationProfileVersion { get; }
        public string MigrationProfileCanonicalHash { get; }
        public string SharedGeometryId { get; }
        public int SharedGeometryVersion { get; }
        public string SharedGeometryCanonicalHash { get; }
        public string ProductionManifestSha256 { get; }
        public string ProductionCatalogSha256 { get; }
        public SpatialValidationInputHash[] ValidationInputHashes =>
            (SpatialValidationInputHash[])validationInputHashes.Clone();
        public string LegacyGameplayConfigurationSha256 { get; }
        public string CanonicalSerializerId { get; }
        public int CanonicalSerializerVersion { get; }
    }

    public static class SpatialMigrationDescriptorContracts
    {
        private static readonly string[] Names =
        {
            "OriginalPayloadSha256", "RawSourceSchemaVersion", "RawEnvelopeClassification",
            "SelectedTargetSchemaVersion", "AuthorityMarkerContractVersion", "MigrationContractVersion",
            "MigrationProfileId", "MigrationProfileVersion", "MigrationProfileCanonicalHash",
            "SharedGeometryId", "SharedGeometryVersion", "SharedGeometryCanonicalHash",
            "ProductionManifestSha256", "ProductionCatalogSha256", "ValidationInputHashes",
            "LegacyGameplayConfigurationSha256", "CanonicalSerializerId", "CanonicalSerializerVersion"
        };
        private static readonly string[] HashNames = { "InputId", "Sha256" };

        public static SpatialContractResult<byte[]> Serialize(SpatialMigrationInputDescriptor value,
            SpatialSerializedInputLimits limits)
        {
            var issues = new SpatialIssueCollector(limits.MaximumDiagnostics);
            if (!limits.IsValid)
            { issues.Add(SpatialContractIssue.InvalidLimits); return Result<byte[]>(null, issues); }
            try
            {
                Validate(value, issues);
                if (issues.Count != 0) return Result<byte[]>(null, issues);
                byte[] bytes = ContractJson.Bytes(BuildCanonical(value));
                if (bytes.Length > limits.MaximumInputBytes)
                { issues.Add(SpatialContractIssue.InputByteLimitExceeded); return Result<byte[]>(null, issues); }
                return Result(bytes, issues);
            }
            catch
            { issues.Add(SpatialContractIssue.InvalidField); return Result<byte[]>(null, issues); }
        }

        public static SpatialContractResult<SpatialMigrationInputDescriptor> Parse(byte[] bytes,
            SpatialSerializedInputLimits limits)
        {
            var issues = new SpatialIssueCollector(limits.MaximumDiagnostics);
            if (!limits.IsValid)
            { issues.Add(SpatialContractIssue.InvalidLimits); return Result<SpatialMigrationInputDescriptor>(null, issues); }
            try
            {
                ContractJsonNode node;
                if (!ContractJson.TryParse(bytes, limits, issues, out node))
                    return Result<SpatialMigrationInputDescriptor>(null, issues);
                if (!ContractJson.ValidateShape(node, Names, issues))
                    return Result<SpatialMigrationInputDescriptor>(null, issues);

                int[] integers = new int[Names.Length];
                string[] strings = new string[Names.Length];
                foreach (int index in new[] { 1, 2, 3, 4, 5, 7, 10, 17 })
                    if (!ContractJson.Int(ContractJson.Field(node, index), out integers[index]))
                        issues.Add(ContractJson.Field(node, index).Kind == ContractJsonKind.Number
                            ? SpatialContractIssue.IntegerOverflow : SpatialContractIssue.WrongFieldType);
                foreach (int index in new[] { 0, 6, 8, 9, 11, 12, 13, 15, 16 })
                    if (!ContractJson.String(ContractJson.Field(node, index), out strings[index]))
                        issues.Add(SpatialContractIssue.WrongFieldType);

                var hashes = new List<SpatialValidationInputHash>();
                ContractJsonNode array = ContractJson.Field(node, 14);
                if (array.Kind != ContractJsonKind.Array) issues.Add(SpatialContractIssue.WrongFieldType);
                else foreach (ContractJsonNode item in array.Items)
                {
                    if (issues.IsExhausted) break;
                    if (!ContractJson.ValidateShape(item, HashNames, issues)) continue;
                    string inputId, sha256;
                    if (!ContractJson.String(ContractJson.Field(item, 0), out inputId) ||
                        !ContractJson.String(ContractJson.Field(item, 1), out sha256))
                        issues.Add(SpatialContractIssue.WrongFieldType);
                    else hashes.Add(new SpatialValidationInputHash(inputId, sha256));
                }
                if (issues.Count != 0) return Result<SpatialMigrationInputDescriptor>(null, issues);

                var descriptor = new SpatialMigrationInputDescriptor(strings[0], integers[1],
                    (SpatialRawEnvelopeClassification)integers[2], integers[3], integers[4], integers[5],
                    strings[6], integers[7], strings[8], strings[9], integers[10], strings[11], strings[12],
                    strings[13], hashes, strings[15], strings[16], integers[17]);
                Validate(descriptor, issues);
                if (issues.Count == 0)
                {
                    SpatialContractResult<byte[]> again = Serialize(descriptor, limits);
                    if (!again.IsValid || !bytes.SequenceEqual(again.Value)) issues.Add(SpatialContractIssue.NonCanonicalBytes);
                }
                return Result(issues.Count == 0 ? descriptor : null, issues);
            }
            catch
            { issues.Add(SpatialContractIssue.MalformedJson); return Result<SpatialMigrationInputDescriptor>(null, issues); }
        }

        public static string ComputeInputFingerprint(SpatialMigrationInputDescriptor value,
            SpatialSerializedInputLimits limits)
        {
            SpatialContractResult<byte[]> result = Serialize(value, limits);
            return result.IsValid ? SpatialContractSha256.Compute(result.Value) : null;
        }

        private static string BuildCanonical(SpatialMigrationInputDescriptor value)
        {
            var builder = new StringBuilder(); builder.Append('{');
            AddString(builder, Names[0], value.OriginalPayloadSha256, true);
            AddInteger(builder, Names[1], value.RawSourceSchemaVersion);
            AddInteger(builder, Names[2], (int)value.RawEnvelopeClassification);
            AddInteger(builder, Names[3], value.SelectedTargetSchemaVersion);
            AddInteger(builder, Names[4], value.AuthorityMarkerContractVersion);
            AddInteger(builder, Names[5], value.MigrationContractVersion);
            AddString(builder, Names[6], value.MigrationProfileId);
            AddInteger(builder, Names[7], value.MigrationProfileVersion);
            AddString(builder, Names[8], value.MigrationProfileCanonicalHash);
            AddString(builder, Names[9], value.SharedGeometryId);
            AddInteger(builder, Names[10], value.SharedGeometryVersion);
            AddString(builder, Names[11], value.SharedGeometryCanonicalHash);
            AddString(builder, Names[12], value.ProductionManifestSha256);
            AddString(builder, Names[13], value.ProductionCatalogSha256);
            builder.Append(','); ContractJson.AppendString(builder, Names[14]); builder.Append(":");
            builder.Append('['); SpatialValidationInputHash[] hashes = value.ValidationInputHashes;
            for (int index = 0; index < hashes.Length; index++)
            {
                if (index != 0) builder.Append(','); builder.Append('{');
                AddString(builder, HashNames[0], hashes[index].InputId, true);
                AddString(builder, HashNames[1], hashes[index].Sha256); builder.Append('}');
            }
            builder.Append(']');
            AddString(builder, Names[15], value.LegacyGameplayConfigurationSha256);
            AddString(builder, Names[16], value.CanonicalSerializerId);
            AddInteger(builder, Names[17], value.CanonicalSerializerVersion);
            builder.Append('}'); return builder.ToString();
        }

        private static void Validate(SpatialMigrationInputDescriptor descriptor, SpatialIssueCollector issues)
        {
            if (descriptor == null) { issues.Add(SpatialContractIssue.InvalidField); return; }
            foreach (string hash in new[] { descriptor.OriginalPayloadSha256,
                descriptor.MigrationProfileCanonicalHash, descriptor.SharedGeometryCanonicalHash,
                descriptor.ProductionManifestSha256, descriptor.ProductionCatalogSha256,
                descriptor.LegacyGameplayConfigurationSha256 })
            { if (!SpatialContractSha256.IsCanonical(hash)) issues.Add(SpatialContractIssue.InvalidHash); if (issues.IsExhausted) return; }

            if (descriptor.RawSourceSchemaVersion < 1 || descriptor.SelectedTargetSchemaVersion < 1 ||
                descriptor.AuthorityMarkerContractVersion != SpatialMigrationContractIdentity.AuthorityMarkerContractVersion ||
                descriptor.MigrationContractVersion != SpatialMigrationContractIdentity.MigrationContractVersion ||
                descriptor.MigrationProfileVersion < 1 || descriptor.SharedGeometryVersion < 1 ||
                descriptor.CanonicalSerializerVersion != SpatialMigrationContractIdentity.CanonicalSerializerVersion ||
                !Enum.IsDefined(typeof(SpatialRawEnvelopeClassification), descriptor.RawEnvelopeClassification))
                issues.Add(SpatialContractIssue.InvalidIdentity);
            if (!SpatialContractSha256.IsStableId(descriptor.MigrationProfileId) ||
                !SpatialContractSha256.IsStableId(descriptor.SharedGeometryId) ||
                !SpatialContractSha256.IsStableId(descriptor.CanonicalSerializerId) ||
                !string.Equals(descriptor.CanonicalSerializerId, SpatialMigrationContractIdentity.CanonicalSerializerId,
                    StringComparison.Ordinal)) issues.Add(SpatialContractIssue.InvalidStableId);

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (SpatialValidationInputHash hash in descriptor.ValidationInputHashes)
            {
                if (hash == null || !SpatialContractSha256.IsStableId(hash.InputId) ||
                    !SpatialContractSha256.IsCanonical(hash.Sha256) || !ids.Add(hash.InputId))
                    issues.Add(SpatialContractIssue.InvalidField);
                if (issues.IsExhausted) return;
            }
        }

        private static void AddString(StringBuilder builder, string name, string value, bool first = false)
        { if (!first) builder.Append(','); ContractJson.AppendString(builder, name); builder.Append(':'); ContractJson.AppendString(builder, value); }
        private static void AddInteger(StringBuilder builder, string name, int value)
        { builder.Append(','); ContractJson.AppendString(builder, name); builder.Append(':').Append(value.ToString(CultureInfo.InvariantCulture)); }
        private static SpatialContractResult<T> Result<T>(T value, SpatialIssueCollector issues) =>
            new SpatialContractResult<T>(value, issues.ToArray());
    }

    public static class SpatialMigrationTransactionIdentity
    {
        public const string TransactionIdPrefix = "gd66-";

        public static byte[] CanonicalIdentityBytes(string originalPayloadSha256, string inputFingerprintSha256)
        {
            if (!SpatialContractSha256.IsCanonical(originalPayloadSha256) ||
                !SpatialContractSha256.IsCanonical(inputFingerprintSha256)) return null;
            return ContractJson.Bytes("{\"OriginalPayloadSha256\":\"" + originalPayloadSha256 +
                "\",\"InputFingerprintSha256\":\"" + inputFingerprintSha256 + "\"}");
        }

        public static string ComputeIdentity(string originalPayloadSha256, string inputFingerprintSha256)
        { byte[] bytes = CanonicalIdentityBytes(originalPayloadSha256, inputFingerprintSha256); return bytes == null ? null : SpatialContractSha256.Compute(bytes); }
        public static string CreateTransactionId(string identitySha256) =>
            SpatialContractSha256.IsCanonical(identitySha256) ? TransactionIdPrefix + identitySha256 : null;
        public static bool IsCanonicalTransactionId(string value) => value != null && value.Length == 69 &&
            value.StartsWith(TransactionIdPrefix, StringComparison.Ordinal) &&
            SpatialContractSha256.IsCanonical(value.Substring(TransactionIdPrefix.Length));
    }
}
