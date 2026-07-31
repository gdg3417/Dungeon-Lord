using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum CompatibilityProfileLifecycle
    {
        Active = 1,
        Retired = 2
    }
    public enum CompatibilityRouteRole
    {
        Entrance = 1,
        BasicRoom0 = 2,
        BasicRoom1 = 3,
        Completion = 4
    }

    [Serializable]
    public sealed class CompatibilityLayoutPlacement
    {
        public CompatibilityRouteRole Role;
        public TileCoordinate Anchor;
        public CardinalOrientation Orientation;
    }
    [Serializable]
    public sealed class CompatibilityLayoutConnection
    {
        public CompatibilityRouteRole SourceRole;
        public string SourceConnectionPointId;
        public CompatibilityRouteRole DestinationRole;
        public string DestinationConnectionPointId;
        public string SocketTypeId;
        public FloorRouteConnectionKind ConnectionKind;
        public string CorridorDefinitionId;
    }
    [Serializable]
    public sealed class CompatibilityLayoutVariant
    {
        public string LayoutId;
        public CompatibilityLayoutPlacement[] Placements = Array.Empty<CompatibilityLayoutPlacement>();
        public CompatibilityLayoutConnection[] Connections = Array.Empty<CompatibilityLayoutConnection>();
        public int ExpectedOccupiedTileTotal;
    }
    [Serializable]
    public sealed class CompatibilityLayoutGeometryRecord
    {
        public string GeometryId;
        public int GeometryVersion;
        public string CanonicalHash;
        public string FloorDefinitionId;
        public int FloorIndex;
        public string EntranceStructureDefinitionId;
        public string EntranceConnectionPointId;
        public string CompletionStructureDefinitionId;
        public string CompletionConnectionPointId;
        public string BasicRoomDefinitionId;
        public string BasicRoomSouthConnectionPointId;
        public string BasicRoomNorthConnectionPointId;
        public string SocketTypeId;
        public CompatibilityLayoutVariant[] Layouts = Array.Empty<CompatibilityLayoutVariant>();
    }
    [Serializable]
    public sealed class SpatialMigrationCompatibilityProfile
    {
        public string ProfileId;
        public int ProfileVersion;
        public string CanonicalHash;
        public CompatibilityProfileLifecycle Lifecycle;
        public int MinimumSourceSchemaVersion;
        public int MaximumSourceSchemaVersion;
        public int TargetSchemaVersion;
        public int TargetCanonicalLayoutContractVersion;
        public string GeometryId;
        public int GeometryVersion;
        public string GeometryCanonicalHash;
    }
    [Serializable]
    public sealed class CanonicalStarterLayoutProfile
    {
        public string ProfileId;
        public int ProfileVersion;
        public string CanonicalHash;
        public CompatibilityProfileLifecycle Lifecycle;
        public int TargetSchemaVersion;
        public int CanonicalLayoutContractVersion;
        public string GeometryId;
        public int GeometryVersion;
        public string GeometryCanonicalHash;
    }
    [Serializable]
    public sealed class CanonicalLayoutContractSelection
    {
        public int TargetSchemaVersion;
        public int CanonicalLayoutContractVersion;
        public CompatibilityProfileLifecycle Lifecycle;
    }
    [Serializable]
    public sealed class SpatialLayoutCompatibilityProfilesData
    {
        public string Schema;
        public int SchemaVersion;
        public CompatibilityLayoutGeometryRecord[] GeometryRecords = Array.Empty<CompatibilityLayoutGeometryRecord>();
        public SpatialMigrationCompatibilityProfile[] MigrationProfiles =
            Array.Empty<SpatialMigrationCompatibilityProfile>();
        public CanonicalStarterLayoutProfile[] StarterProfiles = Array.Empty<CanonicalStarterLayoutProfile>();
        public CanonicalLayoutContractSelection[] ContractSelections = Array.Empty<CanonicalLayoutContractSelection>();
    }

    public enum SpatialLayoutCompatibilityDiagnostic
    {
        None = 0,
        MissingInput = 1,
        EmptyInput = 2,
        InvalidEncoding = 3,
        InvalidJson = 4,
        InvalidSchema = 5,
        NoncanonicalInput = 6,
        WorkloadExceeded = 7,
        InvalidStableId = 8,
        InvalidVersion = 9,
        InvalidHash = 10,
        DuplicateGeometry = 11,
        MissingGeometry = 12,
        InvalidLifecycleSelection = 13,
        InvalidProductionReference = 14,
        InvalidGeometry = 15,
        UnauthorizedActiveProductionSelection = 16,
        DuplicateLayout = 17,
        DuplicateProfile = 18,
        IncompleteGeometry = 19
    }

    public enum CompatibilitySelectionStatus
    {
        Success = 0,
        Missing = 1,
        Duplicate = 2,
        Invalid = 3,
        VersionMismatch = 4
    }

    public sealed class CompatibilitySelectionResult<T>
        where T : class
    {
        internal CompatibilitySelectionResult(CompatibilitySelectionStatus status, string code, T value)
        {
            Status = status;
            Code = code;
            Value = value;
        }
        public bool Success => Status == CompatibilitySelectionStatus.Success && Value != null;
        public CompatibilitySelectionStatus Status { get; }
        public string Code { get; }
        public T Value { get; }
    }

    public sealed class SpatialLayoutCompatibilityResult
    {
        internal SpatialLayoutCompatibilityResult(SpatialLayoutCompatibilitySnapshot value,
                                                  IEnumerable<SpatialLayoutCompatibilityDiagnostic> diagnostics)
        {
            Value = value;
            Diagnostics = diagnostics.Distinct().OrderBy(x => (int)x).ToArray();
        }
        public bool Success => Value != null && Diagnostics.Length == 0;
        public SpatialLayoutCompatibilitySnapshot Value { get; }
        public SpatialLayoutCompatibilityDiagnostic[] Diagnostics { get; }
    }

    public sealed class SpatialLayoutCompatibilitySnapshot
    {
        private readonly byte[] bytes;
        internal SpatialLayoutCompatibilitySnapshot(SpatialLayoutCompatibilityProfilesData value)
        {
            bytes = SpatialLayoutCompatibilityProfiles.SerializeCanonical(value);
        }
        public SpatialLayoutCompatibilityProfilesData Value =>
            JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(Encoding.UTF8.GetString(bytes));
        public byte[] CanonicalBytes => (byte[])bytes.Clone();
    }

    public static class SpatialLayoutCompatibilityProfiles
    {
        public const string ProductionPath =
            "Assets/_Project/Data/Production/DungeonSpatial/" + "spatial_layout_compatibility_profiles.json";
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        public static SpatialLayoutCompatibilityResult ParseAndValidate(
            TextAsset asset, ProductionSpatialContentSnapshot spatial, SpatialContentValidationWorkloadLimits limits,
            Action<SpatialLayoutCompatibilityDiagnostic> sink = null, bool requireInactiveProduction = false)
        {
            if (asset == null)
                return Failure(SpatialLayoutCompatibilityDiagnostic.MissingInput, sink);
            return ParseAndValidate(asset.bytes, spatial, limits, sink, requireInactiveProduction);
        }

        public static SpatialLayoutCompatibilityResult ParseAndValidate(
            byte[] bytes, ProductionSpatialContentSnapshot spatial, SpatialContentValidationWorkloadLimits limits,
            Action<SpatialLayoutCompatibilityDiagnostic> sink = null, bool requireInactiveProduction = false)
        {
            var issues = new CompatibilityDiagnosticCollector(limits.MaximumIssues);
            if (bytes == null)
            {
                issues.Add(SpatialLayoutCompatibilityDiagnostic.MissingInput);
                return Finish(null, issues.Diagnostics, sink);
            }
            if (bytes.Length == 0)
            {
                issues.Add(SpatialLayoutCompatibilityDiagnostic.EmptyInput);
                return Finish(null, issues.Diagnostics, sink);
            }
            if (!limits.IsValid)
            {
                issues.Add(SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded);
                return Finish(null, issues.Diagnostics, sink);
            }
            if (bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf ||
                bytes.Contains((byte)'\r') || bytes[bytes.Length - 1] != (byte)'\n' ||
                bytes.Length > 1 && bytes[bytes.Length - 2] == (byte)'\n')
            {
                issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidEncoding);
                return Finish(null, issues.Diagnostics, sink);
            }
            try
            {
                Utf8.GetCharCount(bytes, 0, bytes.Length - 1);
            }
            catch
            {
                issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidEncoding);
                return Finish(null, issues.Diagnostics, sink);
            }
            var strictIssues = new DiagnosticCollector(limits.MaximumIssues);
            var budget = new StrictJsonWorkloadBudget(limits);
            if (!StrictJson.TryParse(bytes, bytes.Length - 1, typeof(SpatialLayoutCompatibilityProfilesData), strictIssues,
                                     budget, out JsonNode root,
                                     out ProductionSpatialGeneratedSetDiagnostic parseDiagnostic) ||
                root.Kind != JsonKind.Object)
            {
                issues.Add(parseDiagnostic == ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded ||
                                   parseDiagnostic == ProductionSpatialGeneratedSetDiagnostic.DiagnosticLimitExceeded
                               ? SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded
                               : SpatialLayoutCompatibilityDiagnostic.InvalidJson);
                return Finish(null, issues.Diagnostics, sink);
            }
            StrictJson.Validate(typeof(SpatialLayoutCompatibilityProfilesData), root, strictIssues);
            if (strictIssues.HasAny)
            {
                issues.Add(
                    strictIssues.Diagnostics.Any(x => x == ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded ||
                                                      x == ProductionSpatialGeneratedSetDiagnostic.DiagnosticLimitExceeded)
                        ? SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded
                        : SpatialLayoutCompatibilityDiagnostic.InvalidJson);
                return Finish(null, issues.Diagnostics, sink);
            }
            SpatialLayoutCompatibilityProfilesData data;
            try
            {
                data = JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(StrictJson.ToCompactJson(root));
            }
            catch
            {
                issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidJson);
                return Finish(null, issues.Diagnostics, sink);
            }
            if (data == null || data.Schema != "spatial_layout_compatibility_profiles" || data.SchemaVersion != 1)
                issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidSchema);
            if (!PrevalidateIdentities(data, issues))
                return Finish(null, issues.Diagnostics, sink);
            SpatialLayoutCompatibilityProfilesData canonical = Canonicalize(data);
            if (!bytes.SequenceEqual(SerializeCanonical(canonical)))
                issues.Add(SpatialLayoutCompatibilityDiagnostic.NoncanonicalInput);
            Validate(canonical, spatial?.Catalog, limits, issues, requireInactiveProduction);
            return !issues.HasAny ? Finish(new SpatialLayoutCompatibilitySnapshot(canonical), issues.Diagnostics, sink)
                                  : Finish(null, issues.Diagnostics, sink);
        }

        public static byte[] SerializeCanonical(SpatialLayoutCompatibilityProfilesData value) =>
            Utf8.GetBytes(JsonUtility.ToJson(value, true) + "\n");
        public static string ComputeGeometryHash(CompatibilityLayoutGeometryRecord geometry)
        {
            CompatibilityLayoutGeometryRecord copy = Clone(geometry);
            copy.CanonicalHash = string.Empty;
            var wrapper = new SpatialLayoutCompatibilityProfilesData
            {
                GeometryRecords = new[] { copy }
            };
            CompatibilityLayoutGeometryRecord canonical = Canonicalize(wrapper).GeometryRecords[0];
            return Sha256(JsonUtility.ToJson(canonical, false));
        }
        public static string ComputeMigrationProfileHash(SpatialMigrationCompatibilityProfile profile)
        {
            SpatialMigrationCompatibilityProfile copy = Clone(profile);
            copy.CanonicalHash = string.Empty;
            return Sha256(JsonUtility.ToJson(copy, false));
        }
        public static string ComputeStarterProfileHash(CanonicalStarterLayoutProfile profile)
        {
            CanonicalStarterLayoutProfile copy = Clone(profile);
            copy.CanonicalHash = string.Empty;
            return Sha256(JsonUtility.ToJson(copy, false));
        }
        public static SpatialLayoutCompatibilityProfilesData Canonicalize(SpatialLayoutCompatibilityProfilesData source)
        {
            var copy = Clone(source) ?? new SpatialLayoutCompatibilityProfilesData();
            copy.GeometryRecords = (copy.GeometryRecords ?? Array.Empty<CompatibilityLayoutGeometryRecord>())
                                       .OrderBy(x => x?.GeometryId, StringComparer.Ordinal)
                                       .ThenBy(x => x?.GeometryVersion ?? 0)
                                       .ToArray();
            foreach (var geometry in copy.GeometryRecords.Where(x => x != null))
            {
                geometry.Layouts = (geometry.Layouts ?? Array.Empty<CompatibilityLayoutVariant>())
                                       .OrderBy(x => x?.LayoutId, StringComparer.Ordinal)
                                       .ToArray();
                foreach (var layout in geometry.Layouts.Where(x => x != null))
                {
                    layout.Placements = (layout.Placements ?? Array.Empty<CompatibilityLayoutPlacement>())
                                            .OrderBy(x => x == null ? 0 : (int)x.Role)
                                            .ToArray();
                    layout.Connections = (layout.Connections ?? Array.Empty<CompatibilityLayoutConnection>())
                                             .OrderBy(ConnectionIdentity, StringComparer.Ordinal)
                                             .ToArray();
                }
            }
            copy.MigrationProfiles = (copy.MigrationProfiles ?? Array.Empty<SpatialMigrationCompatibilityProfile>())
                                         .OrderBy(x => x?.ProfileId, StringComparer.Ordinal)
                                         .ThenBy(x => x?.ProfileVersion ?? 0)
                                         .ThenBy(x => x?.CanonicalHash, StringComparer.Ordinal)
                                         .ToArray();
            copy.StarterProfiles = (copy.StarterProfiles ?? Array.Empty<CanonicalStarterLayoutProfile>())
                                       .OrderBy(x => x?.ProfileId, StringComparer.Ordinal)
                                       .ThenBy(x => x?.ProfileVersion ?? 0)
                                       .ThenBy(x => x?.CanonicalHash, StringComparer.Ordinal)
                                       .ToArray();
            copy.ContractSelections = (copy.ContractSelections ?? Array.Empty<CanonicalLayoutContractSelection>())
                                          .OrderBy(x => x?.TargetSchemaVersion ?? 0)
                                          .ThenBy(x => x?.CanonicalLayoutContractVersion ?? 0)
                                          .ThenBy(x => x == null ? 0 : (int)x.Lifecycle)
                                          .ToArray();
            return copy;
        }

        public static CompatibilitySelectionResult<SpatialMigrationCompatibilityProfile> SelectMigration(
            SpatialLayoutCompatibilityProfilesData data, int rawSchema, int targetSchema, int targetContractVersion)
        {
            SpatialMigrationCompatibilityProfile[] supplied = data?.MigrationProfiles ??
                Array.Empty<SpatialMigrationCompatibilityProfile>();
            if (supplied.Any(value => !ValidMigration(value) ||
                !Enum.IsDefined(typeof(CompatibilityProfileLifecycle), value.Lifecycle) ||
                !ValidHash(value.CanonicalHash) || value.CanonicalHash != ComputeMigrationProfileHash(value)))
                return Selection<SpatialMigrationCompatibilityProfile>(CompatibilitySelectionStatus.Invalid,
                    "gd66.profile.invalid");
            SpatialMigrationCompatibilityProfile[] active =
                supplied
                    .Where(value => value != null && value.Lifecycle == CompatibilityProfileLifecycle.Active &&
                                    rawSchema >= value.MinimumSourceSchemaVersion &&
                                    rawSchema <= value.MaximumSourceSchemaVersion)
                    .ToArray();
            if (active.Length == 0)
                return Selection<SpatialMigrationCompatibilityProfile>(CompatibilitySelectionStatus.Missing,
                                                                       "gd66.profile.missing");
            if (active.Length > 1)
                return Selection<SpatialMigrationCompatibilityProfile>(CompatibilitySelectionStatus.Duplicate,
                                                                       "gd66.profile.duplicate");
            SpatialMigrationCompatibilityProfile match = active[0];
            if (!ValidMigration(match) || !ValidHash(match.CanonicalHash) ||
                match.CanonicalHash != ComputeMigrationProfileHash(match))
                return Selection<SpatialMigrationCompatibilityProfile>(CompatibilitySelectionStatus.Invalid,
                                                                       "gd66.profile.invalid");
            if (match.TargetSchemaVersion != targetSchema ||
                match.TargetCanonicalLayoutContractVersion != targetContractVersion)
                return Selection<SpatialMigrationCompatibilityProfile>(CompatibilitySelectionStatus.VersionMismatch,
                                                                       "gd66.profile.version_mismatch");
            return Selection(CompatibilitySelectionStatus.Success, string.Empty, Clone(match));
        }

        public static CompatibilitySelectionResult<CanonicalStarterLayoutProfile> SelectStarter(
            SpatialLayoutCompatibilityProfilesData data, int targetSchema, int contractVersion)
        {
            CanonicalStarterLayoutProfile[] supplied = data?.StarterProfiles ??
                Array.Empty<CanonicalStarterLayoutProfile>();
            if (supplied.Any(value => !ValidStarter(value) ||
                !Enum.IsDefined(typeof(CompatibilityProfileLifecycle), value.Lifecycle) ||
                !ValidHash(value.CanonicalHash) || value.CanonicalHash != ComputeStarterProfileHash(value)))
                return Selection<CanonicalStarterLayoutProfile>(CompatibilitySelectionStatus.Invalid,
                    "gd66.starter_profile.invalid");
            CanonicalStarterLayoutProfile[] activeForSchema =
                supplied
                    .Where(value => value != null && value.Lifecycle == CompatibilityProfileLifecycle.Active &&
                                    value.TargetSchemaVersion == targetSchema)
                    .ToArray();
            if (activeForSchema.Length == 0)
                return Selection<CanonicalStarterLayoutProfile>(CompatibilitySelectionStatus.Missing,
                                                                  "gd66.starter_profile.missing");
            CanonicalStarterLayoutProfile[] active = activeForSchema.Where(value =>
                value.CanonicalLayoutContractVersion == contractVersion).ToArray();
            if (active.Length == 0)
                return Selection<CanonicalStarterLayoutProfile>(CompatibilitySelectionStatus.VersionMismatch,
                    "gd66.starter_profile.version_mismatch");
            if (active.Length > 1)
                return Selection<CanonicalStarterLayoutProfile>(CompatibilitySelectionStatus.Duplicate,
                                                                "gd66.starter_profile.duplicate");
            if (!ValidStarter(active[0]) || !ValidHash(active[0].CanonicalHash) ||
                active[0].CanonicalHash != ComputeStarterProfileHash(active[0]))
                return Selection<CanonicalStarterLayoutProfile>(CompatibilitySelectionStatus.Invalid,
                                                                "gd66.starter_profile.invalid");
            return Selection(CompatibilitySelectionStatus.Success, string.Empty, Clone(active[0]));
        }

        public static CompatibilitySelectionResult<CanonicalLayoutContractSelection> SelectContract(
            SpatialLayoutCompatibilityProfilesData data, int targetSchema)
        {
            CanonicalLayoutContractSelection[] active =
                (data?.ContractSelections ?? Array.Empty<CanonicalLayoutContractSelection>())
                    .Where(value => value != null && value.Lifecycle == CompatibilityProfileLifecycle.Active &&
                                    value.TargetSchemaVersion == targetSchema)
                    .ToArray();
            if (active.Length == 0)
                return Selection<CanonicalLayoutContractSelection>(CompatibilitySelectionStatus.Missing,
                                                                   "gd66.layout_contract.selection_missing");
            if (active.Length > 1)
                return Selection<CanonicalLayoutContractSelection>(CompatibilitySelectionStatus.Duplicate,
                                                                   "gd66.layout_contract.selection_duplicate");
            return Selection(CompatibilitySelectionStatus.Success, string.Empty, Clone(active[0]));
        }

        public static bool TryRecoverMigration(SpatialLayoutCompatibilityProfilesData data, string profileId,
                                               int profileVersion, string profileHash, string geometryId,
                                               int geometryVersion, string geometryHash,
                                               out SpatialMigrationCompatibilityProfile profile)
        {
            SpatialMigrationCompatibilityProfile[] matches =
                (data?.MigrationProfiles ?? Array.Empty<SpatialMigrationCompatibilityProfile>())
                    .Where(value => value != null && value.ProfileId == profileId &&
                                    value.ProfileVersion == profileVersion && value.CanonicalHash == profileHash &&
                                    value.GeometryId == geometryId && value.GeometryVersion == geometryVersion &&
                                    value.GeometryCanonicalHash == geometryHash)
                    .Take(2)
                    .ToArray();
            profile = matches.Length == 1 ? Clone(matches[0]) : null;
            if (profile != null && (!ValidHash(profile.CanonicalHash) ||
                profile.CanonicalHash != ComputeMigrationProfileHash(profile) ||
                !ReferenceExists(data.GeometryRecords ?? Array.Empty<CompatibilityLayoutGeometryRecord>(),
                    geometryId, geometryVersion, geometryHash)))
                profile = null;
            return profile != null;
        }

        public static bool TryRecoverStarter(SpatialLayoutCompatibilityProfilesData data, string profileId,
                                             int profileVersion, string profileHash, string geometryId,
                                             int geometryVersion, string geometryHash,
                                             out CanonicalStarterLayoutProfile profile)
        {
            CanonicalStarterLayoutProfile[] matches =
                (data?.StarterProfiles ?? Array.Empty<CanonicalStarterLayoutProfile>())
                    .Where(value => value != null && value.ProfileId == profileId &&
                                    value.ProfileVersion == profileVersion && value.CanonicalHash == profileHash &&
                                    value.GeometryId == geometryId && value.GeometryVersion == geometryVersion &&
                                    value.GeometryCanonicalHash == geometryHash)
                    .Take(2)
                    .ToArray();
            profile = matches.Length == 1 ? Clone(matches[0]) : null;
            if (profile != null && (!ValidHash(profile.CanonicalHash) ||
                profile.CanonicalHash != ComputeStarterProfileHash(profile) ||
                !ReferenceExists(data.GeometryRecords ?? Array.Empty<CompatibilityLayoutGeometryRecord>(),
                    geometryId, geometryVersion, geometryHash)))
                profile = null;
            return profile != null;
        }

        private static bool PrevalidateIdentities(SpatialLayoutCompatibilityProfilesData data,
                                                  CompatibilityDiagnosticCollector issues)
        {
            CompatibilityLayoutGeometryRecord[] geometries = data?.GeometryRecords;
            if (geometries == null || geometries.Length == 0)
            {
                issues.Add(SpatialLayoutCompatibilityDiagnostic.MissingGeometry);
                return false;
            }
            if (geometries.Any(value => value == null) ||
                HasDuplicate(geometries, value => value.GeometryId + "\0" + value.GeometryVersion))
                issues.Add(SpatialLayoutCompatibilityDiagnostic.DuplicateGeometry);
            foreach (CompatibilityLayoutGeometryRecord geometry in geometries.Where(value => value != null))
            {
                if (geometry.Layouts == null || geometry.Layouts.Length == 0)
                    issues.Add(SpatialLayoutCompatibilityDiagnostic.IncompleteGeometry);
                else if (geometry.Layouts.Any(value => value == null) ||
                         HasDuplicate(geometry.Layouts, value => value.LayoutId))
                    issues.Add(SpatialLayoutCompatibilityDiagnostic.DuplicateLayout);
                foreach (CompatibilityLayoutVariant layout in (geometry.Layouts ??
                                                               Array.Empty<CompatibilityLayoutVariant>())
                             .Where(value => value != null))
                {
                    if (layout.Placements == null || layout.Placements.Any(value => value == null) ||
                        HasDuplicate(layout.Placements, value => ((int)value.Role).ToString()))
                        issues.Add(SpatialLayoutCompatibilityDiagnostic.IncompleteGeometry);
                    if (layout.Connections == null || layout.Connections.Any(value => value == null) ||
                        HasDuplicate(layout.Connections, ConnectionIdentity))
                        issues.Add(SpatialLayoutCompatibilityDiagnostic.IncompleteGeometry);
                }
            }
            if (HasDuplicate(data.MigrationProfiles ?? Array.Empty<SpatialMigrationCompatibilityProfile>(),
                             value => value == null ? "<null>" : value.ProfileId + "\0" + value.ProfileVersion) ||
                HasDuplicate(data.StarterProfiles ?? Array.Empty<CanonicalStarterLayoutProfile>(),
                             value => value == null ? "<null>" : value.ProfileId + "\0" + value.ProfileVersion))
                issues.Add(SpatialLayoutCompatibilityDiagnostic.DuplicateProfile);
            if (HasDuplicate(data.ContractSelections ?? Array.Empty<CanonicalLayoutContractSelection>(),
                             value => value == null
                                          ? "<null>"
                                          : value.TargetSchemaVersion + "\0" + value.CanonicalLayoutContractVersion + "\0" +
                                                (int)value.Lifecycle))
                issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidLifecycleSelection);
            return !issues.HasAny;
        }

        private static void Validate(SpatialLayoutCompatibilityProfilesData data, SpatialContentCatalog catalog,
                                     SpatialContentValidationWorkloadLimits limits, CompatibilityDiagnosticCollector issues,
                                     bool production)
        {
            var geometries = data.GeometryRecords ?? Array.Empty<CompatibilityLayoutGeometryRecord>();
            if (geometries.Any(x => x == null))
            {
                issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidGeometry);
                return;
            }
            if (geometries.GroupBy(x => x.GeometryId + "\0" + x.GeometryVersion, StringComparer.Ordinal)
                    .Any(g => g.Count() != 1))
                issues.Add(SpatialLayoutCompatibilityDiagnostic.DuplicateGeometry);
            foreach (var g in geometries)
            {
                if (issues.IsExhausted)
                    return;
                if (!Stable(g.GeometryId) || !Stable(g.FloorDefinitionId) || !Stable(g.EntranceStructureDefinitionId) ||
                    !Stable(g.CompletionStructureDefinitionId) || !Stable(g.BasicRoomDefinitionId) ||
                    !Stable(g.SocketTypeId) || !Stable(g.EntranceConnectionPointId) ||
                    !Stable(g.CompletionConnectionPointId) || !Stable(g.BasicRoomSouthConnectionPointId) ||
                    !Stable(g.BasicRoomNorthConnectionPointId))
                    issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidStableId);
                if (g.GeometryVersion <= 0)
                    issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidVersion);
                if (!ValidHash(g.CanonicalHash) || g.CanonicalHash != ComputeGeometryHash(g))
                    issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidHash);
                ValidateGeometry(g, catalog, limits, issues);
            }
            foreach (var p in data.MigrationProfiles ?? Array.Empty<SpatialMigrationCompatibilityProfile>())
            {
                if (issues.IsExhausted)
                    return;
                if (!ValidMigration(p) || !ValidHash(p.CanonicalHash) ||
                    p.CanonicalHash != ComputeMigrationProfileHash(p) ||
                    !ReferenceExists(geometries, p?.GeometryId, p?.GeometryVersion, p?.GeometryCanonicalHash))
                    issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidLifecycleSelection);
            }
            foreach (var p in data.StarterProfiles ?? Array.Empty<CanonicalStarterLayoutProfile>())
            {
                if (issues.IsExhausted)
                    return;
                if (!ValidStarter(p) || !ValidHash(p.CanonicalHash) || p.CanonicalHash != ComputeStarterProfileHash(p) ||
                    !ReferenceExists(geometries, p?.GeometryId, p?.GeometryVersion, p?.GeometryCanonicalHash))
                    issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidLifecycleSelection);
            }
            var activeM = (data.MigrationProfiles ?? Array.Empty<SpatialMigrationCompatibilityProfile>())
                              .Where(x => x?.Lifecycle == CompatibilityProfileLifecycle.Active)
                              .OrderBy(x => x.MinimumSourceSchemaVersion)
                              .ToArray();
            for (int i = 1; i < activeM.Length; i++)
                if (activeM[i].MinimumSourceSchemaVersion <= activeM[i - 1].MaximumSourceSchemaVersion)
                    issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidLifecycleSelection);
            if ((data.StarterProfiles ?? Array.Empty<CanonicalStarterLayoutProfile>())
                    .Where(x => x?.Lifecycle == CompatibilityProfileLifecycle.Active)
                    .GroupBy(x => x.TargetSchemaVersion + "\0" + x.CanonicalLayoutContractVersion)
                    .Any(g => g.Count() != 1) ||
                (data.ContractSelections ?? Array.Empty<CanonicalLayoutContractSelection>())
                    .Any(x => x == null || x.TargetSchemaVersion <= 0 || x.CanonicalLayoutContractVersion <= 0) ||
                (data.ContractSelections ?? Array.Empty<CanonicalLayoutContractSelection>())
                    .Where(x => x?.Lifecycle == CompatibilityProfileLifecycle.Active)
                    .GroupBy(x => x.TargetSchemaVersion)
                    .Any(g => g.Count() != 1))
                issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidLifecycleSelection);
            if (production && ((data.MigrationProfiles ?? Array.Empty<SpatialMigrationCompatibilityProfile>())
                                   .Any(x => x?.Lifecycle == CompatibilityProfileLifecycle.Active) ||
                               (data.StarterProfiles ?? Array.Empty<CanonicalStarterLayoutProfile>())
                                   .Any(x => x?.Lifecycle == CompatibilityProfileLifecycle.Active) ||
                               (data.ContractSelections ?? Array.Empty<CanonicalLayoutContractSelection>())
                                   .Any(x => x?.Lifecycle == CompatibilityProfileLifecycle.Active)))
                issues.Add(SpatialLayoutCompatibilityDiagnostic.UnauthorizedActiveProductionSelection);
        }
        private static void ValidateGeometry(CompatibilityLayoutGeometryRecord g, SpatialContentCatalog c,
                                             SpatialContentValidationWorkloadLimits limits,
                                             CompatibilityDiagnosticCollector issues)
        {
            if (c == null)
            {
                issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidProductionReference);
                return;
            }
            var floor = c.Floors?.SingleOrDefault(x => x?.FloorDefinitionId == g.FloorDefinitionId);
            var room = c.Rooms?.SingleOrDefault(x => x?.RoomDefinitionId == g.BasicRoomDefinitionId);
            var entrance =
                c.FixedStructures?.SingleOrDefault(x => x?.StructureDefinitionId == g.EntranceStructureDefinitionId);
            var completion =
                c.FixedStructures?.SingleOrDefault(x => x?.StructureDefinitionId == g.CompletionStructureDefinitionId);
            var socket = c.SocketTypes?.SingleOrDefault(x => x?.SocketTypeId == g.SocketTypeId);
            if (floor == null || room == null || entrance == null || completion == null || socket == null ||
                floor.FloorIndex != g.FloorIndex ||
                floor.EntranceStructureDefinitionId != g.EntranceStructureDefinitionId ||
                floor.CompletionStructureDefinitionId != g.CompletionStructureDefinitionId ||
                !floor.AllowedRoomDefinitionIds.Contains(g.BasicRoomDefinitionId) ||
                !HasPoint(entrance, g.EntranceConnectionPointId, g.SocketTypeId) ||
                !HasPoint(completion, g.CompletionConnectionPointId, g.SocketTypeId) ||
                !HasPoint(room, g.BasicRoomSouthConnectionPointId, g.SocketTypeId) ||
                !HasPoint(room, g.BasicRoomNorthConnectionPointId, g.SocketTypeId) ||
                socket.CompatibleSocketTypeIds == null || !socket.CompatibleSocketTypeIds.Contains(g.SocketTypeId))
            {
                issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidProductionReference);
                return;
            }
            CompatibilityLayoutVariant[] layouts = g.Layouts ?? Array.Empty<CompatibilityLayoutVariant>();
            if (layouts.Length != 2 || layouts.Count(value => value.LayoutId == "compat.layout.r1") != 1 ||
                layouts.Count(value => value.LayoutId == "compat.layout.r2") != 1)
            {
                issues.Add(SpatialLayoutCompatibilityDiagnostic.IncompleteGeometry);
                return;
            }
            foreach (var layout in g.Layouts ?? Array.Empty<CompatibilityLayoutVariant>())
            {
                if (issues.IsExhausted)
                    return;
                if (layout == null || !Stable(layout.LayoutId) || layout.ExpectedOccupiedTileTotal <= 0)
                {
                    issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidGeometry);
                    continue;
                }
                CompatibilityRouteRole[] expectedRoles =
                    layout.LayoutId == "compat.layout.r1"
                        ? new[] { CompatibilityRouteRole.Entrance, CompatibilityRouteRole.BasicRoom0,
                                  CompatibilityRouteRole.Completion }
                        : new[] { CompatibilityRouteRole.Entrance, CompatibilityRouteRole.BasicRoom0,
                                  CompatibilityRouteRole.BasicRoom1, CompatibilityRouteRole.Completion };
                string[] expectedConnections =
                    layout.LayoutId == "compat.layout.r1"
                        ? new[] { ConnectionIdentity(CompatibilityRouteRole.Entrance, g.EntranceConnectionPointId,
                                                     CompatibilityRouteRole.BasicRoom0, g.BasicRoomSouthConnectionPointId),
                                  ConnectionIdentity(CompatibilityRouteRole.BasicRoom0, g.BasicRoomNorthConnectionPointId,
                                                     CompatibilityRouteRole.Completion, g.CompletionConnectionPointId) }
                        : new[] { ConnectionIdentity(CompatibilityRouteRole.Entrance, g.EntranceConnectionPointId,
                                                     CompatibilityRouteRole.BasicRoom0, g.BasicRoomSouthConnectionPointId),
                                  ConnectionIdentity(CompatibilityRouteRole.BasicRoom0, g.BasicRoomNorthConnectionPointId,
                                                     CompatibilityRouteRole.BasicRoom1, g.BasicRoomSouthConnectionPointId),
                                  ConnectionIdentity(CompatibilityRouteRole.BasicRoom1, g.BasicRoomNorthConnectionPointId,
                                                     CompatibilityRouteRole.Completion, g.CompletionConnectionPointId) };
                if (layout.Placements == null ||
                    !layout.Placements.Select(value => value.Role)
                         .OrderBy(value => (int)value)
                         .SequenceEqual(expectedRoles.OrderBy(value => (int)value)) ||
                    layout.Connections == null ||
                    !layout.Connections.Select(ConnectionIdentity)
                         .OrderBy(value => value, StringComparer.Ordinal)
                         .SequenceEqual(expectedConnections.OrderBy(value => value, StringComparer.Ordinal)))
                {
                    issues.Add(SpatialLayoutCompatibilityDiagnostic.IncompleteGeometry);
                    continue;
                }
                var occupied = new HashSet<TileCoordinate>();
                long total = 0;
                bool bad = false;
                foreach (var p in layout.Placements ?? Array.Empty<CompatibilityLayoutPlacement>())
                {
                    CardinalOrientation[] allowed = p.Role == CompatibilityRouteRole.Entrance ? entrance.AllowedOrientations
                                                    : p.Role == CompatibilityRouteRole.Completion
                                                        ? completion.AllowedOrientations
                                                        : room.AllowedOrientations;
                    if (p == null || !Enum.IsDefined(typeof(CompatibilityRouteRole), p.Role) || allowed == null ||
                        !allowed.Contains(p.Orientation))
                    {
                        bad = true;
                        continue;
                    }
                    RectangularFootprintDefinition footprint =
                        p.Role == CompatibilityRouteRole.Entrance     ? entrance.GrossFootprint
                        : p.Role == CompatibilityRouteRole.Completion ? completion.GrossFootprint
                                                                      : room.GrossFootprint;
                    if (!TileFootprintResolver.TryResolveRectangle(
                            footprint, p.Anchor, p.Orientation,
                            new SpatialValidationWorkloadLimits(limits.MaximumMaterializedTiles),
                            out ResolvedTileFootprint resolved))
                    {
                        bad = true;
                        continue;
                    }
                    TileCoordinate[] ordered = resolved.OccupiedTiles.OrderBy(tile => tile).ToArray();
                    if (!resolved.OccupiedTiles.SequenceEqual(ordered))
                        bad = true;
                    foreach (var tile in ordered)
                    {
                        total++;
                        if (!floor.Bounds.Contains(tile) || !occupied.Add(tile))
                            bad = true;
                    }
                }
                foreach (var edge in layout.Connections ?? Array.Empty<CompatibilityLayoutConnection>())
                {
                    if (edge == null || edge.ConnectionKind != FloorRouteConnectionKind.DirectDoorway ||
                        edge.CorridorDefinitionId != "" || edge.SocketTypeId != g.SocketTypeId ||
                        !Adjacent(g, layout, edge, room, entrance, completion))
                        bad = true;
                }
                if (total != layout.ExpectedOccupiedTileTotal || total > floor.FinalFloorSpaceCapacity ||
                    total > limits.MaximumMaterializedTiles)
                    bad = true;
                if (bad)
                    issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidGeometry);
            }
        }
        private static bool Adjacent(CompatibilityLayoutGeometryRecord g, CompatibilityLayoutVariant l,
                                     CompatibilityLayoutConnection e, RoomSpatialDefinition room,
                                     FixedSpatialStructureDefinition entrance, FixedSpatialStructureDefinition completion)
        {
            var aa = (l.Placements ?? Array.Empty<CompatibilityLayoutPlacement>())
                         .Where(x => x != null && x.Role == e.SourceRole)
                         .Take(2)
                         .ToArray();
            var bb = (l.Placements ?? Array.Empty<CompatibilityLayoutPlacement>())
                         .Where(x => x != null && x.Role == e.DestinationRole)
                         .Take(2)
                         .ToArray();
            if (aa.Length != 1 || bb.Length != 1)
                return false;
            var a = aa[0];
            var b = bb[0];
            var ap = Point(e.SourceRole, e.SourceConnectionPointId, room, entrance, completion);
            var bp = Point(e.DestinationRole, e.DestinationConnectionPointId, room, entrance, completion);
            if (ap == null || bp == null || ap.SocketTypeId != e.SocketTypeId || bp.SocketTypeId != e.SocketTypeId)
                return false;
            RectangularFootprintDefinition af = a.Role == CompatibilityRouteRole.Entrance ? entrance.GrossFootprint :
                a.Role == CompatibilityRouteRole.Completion ? completion.GrossFootprint : room.GrossFootprint;
            RectangularFootprintDefinition bf = b.Role == CompatibilityRouteRole.Entrance ? entrance.GrossFootprint :
                b.Role == CompatibilityRouteRole.Completion ? completion.GrossFootprint : room.GrossFootprint;
            TileCoordinate ac = TransformPoint(ap.Offset, a.Anchor, a.Orientation, af);
            TileCoordinate bc = TransformPoint(bp.Offset, b.Anchor, b.Orientation, bf);
            int aFacing = ((int)ap.Facing + (int)a.Orientation) % 4;
            int bFacing = ((int)bp.Facing + (int)b.Orientation) % 4;
            return Math.Abs(ac.X - bc.X) + Math.Abs(ac.Y - bc.Y) == 1 && (aFacing + 2) % 4 == bFacing;
        }
        private static TileCoordinate TransformPoint(TileCoordinate offset, TileCoordinate anchor,
                                                     CardinalOrientation orientation,
                                                     RectangularFootprintDefinition footprint)
        {
            switch (orientation)
            {
                case CardinalOrientation.Ninety:
                    return new TileCoordinate(anchor.X + footprint.Height - 1 - offset.Y, anchor.Y + offset.X);
                case CardinalOrientation.OneEighty:
                    return new TileCoordinate(anchor.X + footprint.Width - 1 - offset.X,
                        anchor.Y + footprint.Height - 1 - offset.Y);
                case CardinalOrientation.TwoSeventy:
                    return new TileCoordinate(anchor.X + offset.Y, anchor.Y + footprint.Width - 1 - offset.X);
                default:
                    return new TileCoordinate(anchor.X + offset.X, anchor.Y + offset.Y);
            }
        }
        private static SpatialConnectionPointDefinition Point(CompatibilityRouteRole role, string id,
                                                              RoomSpatialDefinition room,
                                                              FixedSpatialStructureDefinition entrance,
                                                              FixedSpatialStructureDefinition completion) =>
            (role == CompatibilityRouteRole.Entrance     ? entrance.ConnectionPoints
             : role == CompatibilityRouteRole.Completion ? completion.ConnectionPoints
                                                         : room.ConnectionPoints)
                .Where(x => x != null && x.ConnectionPointId == id)
                .Take(2)
                .SingleOrDefault();
        private static bool HasPoint(FixedSpatialStructureDefinition owner, string id,
                                     string socket) => owner.ConnectionPoints.Count(x => x?.ConnectionPointId == id &&
                                                                                         x.SocketTypeId == socket) == 1;
        private static bool HasPoint(RoomSpatialDefinition owner, string id,
                                     string socket) => owner.ConnectionPoints.Count(x => x?.ConnectionPointId == id &&
                                                                                         x.SocketTypeId == socket) == 1;
        private static bool ReferenceExists(IEnumerable<CompatibilityLayoutGeometryRecord> gs, string id, int? version,
                                            string hash) => gs.Count(x => x.GeometryId == id &&
                                                                          x.GeometryVersion == version &&
                                                                          x.CanonicalHash == hash) == 1;
        private static bool Stable(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            bool segmentStart = true;
            foreach (char character in value)
            {
                bool alphaNumeric = character >= 'a' && character <= 'z' || character >= '0' && character <= '9';
                if (alphaNumeric)
                {
                    segmentStart = false;
                    continue;
                }
                if ((character != '.' && character != '_' && character != '-') || segmentStart)
                    return false;
                segmentStart = true;
            }
            return !segmentStart;
        }
        private static bool ValidHash(string s) =>
            s != null && s.Length == 64 && s.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f');
        private static T Clone<T>(T value)
            where T : class => value == null ? null : JsonUtility.FromJson<T>(JsonUtility.ToJson(value)); private static string Sha256(string value)
        {
            using (var sha = SHA256.Create()) return string.Concat(
                sha.ComputeHash(Utf8.GetBytes(value)).Select(item => item.ToString("x2")));
        }
        private static bool ValidMigration(SpatialMigrationCompatibilityProfile value) =>
            value != null && Stable(value.ProfileId) && value.ProfileVersion > 0 && value.MinimumSourceSchemaVersion >= 0 &&
            value.MaximumSourceSchemaVersion >= value.MinimumSourceSchemaVersion
            && value.TargetSchemaVersion > 0 && value.TargetCanonicalLayoutContractVersion > 0 && Stable(value.GeometryId);
        private static bool ValidStarter(CanonicalStarterLayoutProfile value) =>
            value != null && Stable(value.ProfileId) && value.ProfileVersion > 0 && value.TargetSchemaVersion > 0 &&
            value.CanonicalLayoutContractVersion > 0 && Stable(value.GeometryId);
        private static string ConnectionIdentity(CompatibilityLayoutConnection value) =>
            value == null ? "<null>"
                          : ConnectionIdentity(value.SourceRole, value.SourceConnectionPointId, value.DestinationRole,
                                               value.DestinationConnectionPointId);
        private static string ConnectionIdentity(CompatibilityRouteRole source, string sourcePoint,
                                                 CompatibilityRouteRole destination,
                                                 string destinationPoint) => string.Join("\0", new[] {
            ((int)source).ToString(), sourcePoint, ((int)destination).ToString(), destinationPoint
        });
        private static bool HasDuplicate<T>(IEnumerable<T> values, Func<T, string> identity) =>
            values.GroupBy(identity, StringComparer.Ordinal).Any(group => group.Count() != 1);
        private static CompatibilitySelectionResult<T> Selection<T>(CompatibilitySelectionStatus status, string code,
                                                                    T value = null)
            where T : class => new CompatibilitySelectionResult<T>(status, code,
      value); private static SpatialLayoutCompatibilityResult Failure(SpatialLayoutCompatibilityDiagnostic issue,
      Action<SpatialLayoutCompatibilityDiagnostic> sink) => Finish(null,
      new[] { issue }, sink);
        private static SpatialLayoutCompatibilityResult Finish(SpatialLayoutCompatibilitySnapshot value,
                                                               IEnumerable<SpatialLayoutCompatibilityDiagnostic> issues,
                                                               Action<SpatialLayoutCompatibilityDiagnostic> sink)
        {
            var result = new SpatialLayoutCompatibilityResult(value, issues);
            foreach (var issue in result.Diagnostics)
                sink?.Invoke(issue);
            return result;
        }
    }

    internal sealed class CompatibilityDiagnosticCollector
    {
        private readonly int maximum;
        private readonly SortedSet<SpatialLayoutCompatibilityDiagnostic> diagnostics =
            new SortedSet<SpatialLayoutCompatibilityDiagnostic>();
        private bool overflowed;

        internal CompatibilityDiagnosticCollector(int maximum)
        {
            this.maximum = maximum;
        }
        internal bool HasAny => diagnostics.Count != 0 || overflowed;
        internal bool IsExhausted => overflowed;
        internal IEnumerable<SpatialLayoutCompatibilityDiagnostic> Diagnostics =>
            overflowed ? new[] { SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded } : diagnostics;
        internal void Add(SpatialLayoutCompatibilityDiagnostic diagnostic)
        {
            if (overflowed || diagnostics.Contains(diagnostic))
                return;
            if (diagnostics.Count >= maximum)
            {
                diagnostics.Clear();
                overflowed = true;
                return;
            }
            diagnostics.Add(diagnostic);
        }
    }
}
