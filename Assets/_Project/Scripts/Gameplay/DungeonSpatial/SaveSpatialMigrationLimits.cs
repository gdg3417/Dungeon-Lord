using System;
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

        public static SaveSpatialMigrationLimitsLoadResult Load(TextAsset asset) =>
            asset == null ? Failure(MissingReason) : Load(asset.text);

        public static SaveSpatialMigrationLimitsLoadResult Load(string json)
        {
            if (string.IsNullOrEmpty(json)) return Failure(MissingReason);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
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
            return new SaveSpatialMigrationLimitsLoadResult(new SaveSpatialMigrationLimitsProfile(raw,
                new CanonicalSpatialSerializationLimits(serialized, spatial), whole), null);
        }

        private static SaveSpatialMigrationLimitsLoadResult Failure(string reason) =>
            new SaveSpatialMigrationLimitsLoadResult(null, reason);
    }
}
