using System;
using System.Text;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class SaveSpatialMigrationLimitsProfile
    {
        internal SaveSpatialMigrationLimitsProfile(RawSavePayloadClassificationLimits raw,
            CanonicalSpatialSerializationLimits canonical, DetachedWholeSaveLimits whole)
        { Raw = raw; Canonical = canonical; Whole = whole; }

        public RawSavePayloadClassificationLimits Raw { get; }
        public CanonicalSpatialSerializationLimits Canonical { get; }
        public DetachedWholeSaveLimits Whole { get; }
    }

    public readonly struct SaveSpatialMigrationLimitsLoadResult
    {
        internal SaveSpatialMigrationLimitsLoadResult(SaveSpatialMigrationLimitsProfile profile, string reason)
        { Profile = profile; Reason = reason; }
        public SaveSpatialMigrationLimitsProfile Profile { get; }
        public string Reason { get; }
        public bool IsSuccess => Profile != null;
    }

    /// <summary>Strict production authority for the composed GD66 save workload limits.</summary>
    public static class SaveSpatialMigrationLimitsLoader
    {
        public const string ProductionPath =
            "Assets/_Project/Data/Production/Save/save_spatial_migration_limits.json";
        public const string MissingReason = "gd66.profile.missing";
        public const string InvalidReason = "gd66.profile.invalid";
        public const string VersionMismatchReason = "gd66.profile.version_mismatch";
        private const string Schema = "save_spatial_migration_limits";
        private const int SchemaVersion = 1;

        private static readonly string[] Fields =
        {
            "Schema", "SchemaVersion", "MaximumRawSaveBytes", "MaximumRawNestingDepth",
            "MaximumRawObjectMembers", "MaximumRawArrayElements", "MaximumRawStringBytes",
            "MaximumRawScanWork", "MaximumSerializedInputBytes", "MaximumSerializedParsedNodes",
            "MaximumSerializedCollectionRecords", "MaximumSerializedStringCharacters",
            "MaximumSerializedDiagnostics", "MaximumCanonicalSpatialRecords",
            "MaximumCanonicalMaterializedTiles", "MaximumWholeSaveCandidateBytes",
            "MaximumCopiedSourceValueBytes", "MaximumUnknownMembers", "MaximumUnknownMemberBytes"
        };

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static SaveSpatialMigrationLimitsLoadResult Load(TextAsset asset) =>
            asset == null ? Failure(MissingReason) : Load(asset.bytes);

        public static SaveSpatialMigrationLimitsLoadResult Load(string json)
        {
            if (string.IsNullOrEmpty(json)) return Failure(MissingReason);
            try { return Load(StrictUtf8.GetBytes(json)); }
            catch (EncoderFallbackException) { return Failure(InvalidReason); }
        }

        public static SaveSpatialMigrationLimitsLoadResult Load(byte[] framedBytes) => LoadCore(framedBytes, true);
        private static SaveSpatialMigrationLimitsProfile historical;
        private static SaveSpatialMigrationLimitsLoadResult LoadCore(byte[] framedBytes, bool includeHistorical)
        {
            if (framedBytes == null || framedBytes.Length == 0) return Failure(MissingReason);
            // Production text assets use exactly one terminal LF. ContractJson receives only the
            // strict JSON body; BOM, CR/CRLF, missing LF, and multiple terminal LFs fail closed.
            if (framedBytes.Length < 2 || framedBytes[framedBytes.Length - 1] != (byte)'\n' ||
                framedBytes[framedBytes.Length - 2] == (byte)'\n' ||
                Array.IndexOf(framedBytes, (byte)'\r') >= 0 ||
                (framedBytes.Length >= 3 && framedBytes[0] == 0xef &&
                    framedBytes[1] == 0xbb && framedBytes[2] == 0xbf))
                return Failure(InvalidReason);
            var bytes = new byte[framedBytes.Length - 1];
            Buffer.BlockCopy(framedBytes, 0, bytes, 0, bytes.Length);
            // Bootstrap parsing limits bound only this small technical configuration document;
            // they are not fallback save limits and are never returned to migration consumers.
            var parseLimits = new SpatialSerializedInputLimits(8192, 128, 32, 2048, 8);
            var issues = new SpatialIssueCollector(parseLimits.MaximumDiagnostics);
            if (!ContractJson.TryParse(bytes, parseLimits, issues, out ContractJsonNode root) ||
                !ContractJson.ValidateShape(root, Fields, issues))
                return Failure(InvalidReason);
            if (!ContractJson.String(root.Fields[0].Value, out string schema) || schema != Schema)
                return Failure(InvalidReason);
            if (!ContractJson.Int(root.Fields[1].Value, out int version))
                return Failure(InvalidReason);
            if (version != SchemaVersion) return Failure(VersionMismatchReason);

            var values = new int[17];
            for (int index = 0; index < values.Length; index++)
                if (!ContractJson.Int(root.Fields[index + 2].Value, out values[index]) || values[index] <= 0)
                    return Failure(InvalidReason);

            var raw = new RawSavePayloadClassificationLimits(values[0], values[1], values[2],
                values[3], values[4], values[5]);
            var serialized = new SpatialSerializedInputLimits(values[6], values[7], values[8],
                values[9], values[10]);
            var spatial = new CanonicalSpatialSaveWorkloadLimits(values[11], values[12]);
            var whole = new DetachedWholeSaveLimits(values[13], values[14], values[15], values[16]);
            if (!raw.IsValid || !serialized.IsValid || !spatial.IsValid || !whole.IsValid ||
                whole.MaximumCandidateBytes > serialized.MaximumInputBytes ||
                whole.MaximumCopiedValueBytes > raw.MaximumInputBytes ||
                whole.MaximumUnknownMemberBytes > raw.MaximumInputBytes)
                return Failure(InvalidReason);
            if (includeHistorical)
            {
                if (historical == null)
                {
                    TextAsset frozen = Resources.Load<TextAsset>("save_spatial_migration_limits_schema13");
                    var loaded = LoadCore(frozen?.bytes, false);
                    if (!loaded.IsSuccess) return Failure(loaded.Reason);
                    var schema14 = LoadCore(Resources.Load<TextAsset>("save_spatial_migration_limits_schema14")?.bytes, false);
                    if (!schema14.IsSuccess) return Failure(schema14.Reason);
                    var prior = loaded.Profile;
                    var raw14 = schema14.Profile.Raw;
                    historical = new SaveSpatialMigrationLimitsProfile(new RawSavePayloadClassificationLimits(raw14.MaximumInputBytes,
                        raw14.MaximumNestingDepth, raw14.MaximumObjectMembers, raw14.MaximumArrayElements, raw14.MaximumStringBytes,
                        raw14.MaximumScanWork, prior), new CanonicalSpatialSerializationLimits(schema14.Profile.Canonical.Serialized,
                        schema14.Profile.Canonical.Spatial, prior), schema14.Profile.Whole);
                    var schema15 = LoadCore(Resources.Load<TextAsset>("save_spatial_migration_limits_schema15")?.bytes, false);
                    if (!schema15.IsSuccess) { historical = null; return Failure(schema15.Reason); }
                    var raw15 = schema15.Profile.Raw;
                    var prior14 = historical;
                    // Preserve the exact schema-15 envelope, including the monster-record exception.
                    historical = new SaveSpatialMigrationLimitsProfile(new RawSavePayloadClassificationLimits(
                        raw15.MaximumInputBytes, raw15.MaximumNestingDepth, raw15.MaximumObjectMembers,
                        raw15.MaximumArrayElements, raw15.MaximumStringBytes, raw15.MaximumScanWork,
                        prior14, CanonicalSaveSchemaVersions.FrozenSchemaFifteen,
                        schema15.Profile.Canonical.Spatial.MaximumRecords),
                        new CanonicalSpatialSerializationLimits(schema15.Profile.Canonical.Serialized,
                            schema15.Profile.Canonical.Spatial, prior14, CanonicalSaveSchemaVersions.FrozenSchemaFifteen),
                        schema15.Profile.Whole);
                }
                raw = new RawSavePayloadClassificationLimits(values[0], values[1], values[2], values[3], values[4], values[5],
                    historical, CanonicalSaveSchemaVersions.FrozenSchemaFifteen, values[11], true);
            }
            return new SaveSpatialMigrationLimitsLoadResult(new SaveSpatialMigrationLimitsProfile(raw,
                new CanonicalSpatialSerializationLimits(serialized, spatial, includeHistorical ? historical : null,
                    CanonicalSaveSchemaVersions.FrozenSchemaFifteen, true), whole), null);
        }

        private static SaveSpatialMigrationLimitsLoadResult Failure(string reason) =>
            new SaveSpatialMigrationLimitsLoadResult(null, reason);
    }
}
