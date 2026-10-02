using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    [Serializable]
    public sealed class RoomContentMigrationCategoryCapacity
    {
        public string CategoryId;
        public int MaximumAssignments;
    }

    [Serializable]
    public sealed class RoomContentMigrationSlot
    {
        public string SlotId;
        public int Order;
        public TileCoordinate Anchor;
        public string[] AllowedCategoryIds = Array.Empty<string>();
        public TileCoordinate[] OccupiedTileOffsets = Array.Empty<TileCoordinate>();
    }

    [Serializable]
    public sealed class RoomContentPositionMigrationProfile
    {
        public string ProfileId;
        public int ProfileVersion;
        public string CanonicalHash;
        public int SourceSaveSchemaVersion;
        public string RoomDefinitionId;
        public CardinalOrientation Orientation;
        public RectangularFootprintDefinition FrozenFootprint;
        public TileCoordinate[] FrozenReservedTileOffsets = Array.Empty<TileCoordinate>();
        public RoomContentMigrationCategoryCapacity[] CategoryCapacities =
            Array.Empty<RoomContentMigrationCategoryCapacity>();
        public RoomContentMigrationSlot[] OrderedSlots = Array.Empty<RoomContentMigrationSlot>();
    }

    [Serializable]
    public sealed class RoomContentPositionMigrationProfilesData
    {
        public string Schema;
        public int SchemaVersion;
        public string ProfileSetId;
        public int ProfileSetVersion;
        public int SourceSaveSchemaVersion;
        public string CanonicalHash;
        public RoomContentPositionMigrationProfile[] Profiles =
            Array.Empty<RoomContentPositionMigrationProfile>();
    }

    public enum RoomContentPositionMigrationProfileDiagnostic
    {
        None = 0,
        MissingInput = 1,
        EmptyInput = 2,
        InvalidEncoding = 3,
        InvalidJson = 4,
        InvalidSchema = 5,
        UnsupportedSourceSchema = 6,
        NoncanonicalInput = 7,
        WorkloadExceeded = 8,
        InvalidStableId = 9,
        InvalidVersion = 10,
        InvalidHash = 11,
        DuplicateProfileId = 12,
        DuplicateRoomOrientation = 13,
        UnknownRoom = 14,
        UnsupportedOrientation = 15,
        MissingProductionCoverage = 16,
        FrozenContextMismatch = 17,
        InvalidCategoryCapacity = 18,
        InsufficientSlots = 19,
        DuplicateSlotId = 20,
        DuplicateSlotOrder = 21,
        InvalidCategoryCompatibility = 22,
        InvalidOccupancyFootprint = 23,
        OutOfFootprint = 24,
        ReservedTileOverlap = 25,
        SlotOverlap = 26,
        UnauthorizedProductionOccupancy = 27
    }

    public sealed class RoomContentPositionMigrationProfilesResult
    {
        internal RoomContentPositionMigrationProfilesResult(
            RoomContentPositionMigrationProfilesSnapshot value,
            IEnumerable<RoomContentPositionMigrationProfileDiagnostic> diagnostics)
        {
            Value = value;
            Diagnostics = (diagnostics ?? Enumerable.Empty<RoomContentPositionMigrationProfileDiagnostic>())
                .Distinct().OrderBy(value => (int)value).ToArray();
        }

        public bool Success => Value != null && Diagnostics.Length == 0;
        public RoomContentPositionMigrationProfilesSnapshot Value { get; }
        public RoomContentPositionMigrationProfileDiagnostic[] Diagnostics { get; }
    }

    public sealed class ValidatedRoomContentPositionMigrationProfile
    {
        private readonly RoomContentPositionMigrationProfile value;

        internal ValidatedRoomContentPositionMigrationProfile(RoomContentPositionMigrationProfile value)
        {
            this.value = Clone(value);
        }

        public string ProfileId => value.ProfileId;
        public int ProfileVersion => value.ProfileVersion;
        public string CanonicalHash => value.CanonicalHash;
        public int SourceSaveSchemaVersion => value.SourceSaveSchemaVersion;
        public string RoomDefinitionId => value.RoomDefinitionId;
        public CardinalOrientation Orientation => value.Orientation;
        public int SlotCount => value.OrderedSlots?.Length ?? 0;

        internal RoomContentPositionMigrationProfile CloneValue() => Clone(value);

        private static T Clone<T>(T source) where T : class => source == null ? null :
            JsonUtility.FromJson<T>(JsonUtility.ToJson(source));
    }

    public sealed class RoomContentPositionMigrationProfilesSnapshot
    {
        private readonly byte[] canonicalBytes;

        internal RoomContentPositionMigrationProfilesSnapshot(RoomContentPositionMigrationProfilesData value)
        {
            canonicalBytes = RoomContentPositionMigrationProfiles.SerializeCanonical(value);
        }

        public byte[] CanonicalBytes => (byte[])canonicalBytes.Clone();
        public RoomContentPositionMigrationProfilesData Value =>
            JsonUtility.FromJson<RoomContentPositionMigrationProfilesData>(
                Encoding.UTF8.GetString(canonicalBytes));

        public bool TryGetProfile(string roomDefinitionId, CardinalOrientation orientation,
            out ValidatedRoomContentPositionMigrationProfile profile)
        {
            RoomContentPositionMigrationProfile[] matches = (Value.Profiles ??
                Array.Empty<RoomContentPositionMigrationProfile>()).Where(value => value != null &&
                string.Equals(value.RoomDefinitionId, roomDefinitionId, StringComparison.Ordinal) &&
                value.Orientation == orientation).Take(2).ToArray();
            profile = matches.Length == 1
                ? new ValidatedRoomContentPositionMigrationProfile(matches[0]) : null;
            return profile != null;
        }
    }

    public enum RoomContentPositionMigrationPlanFailure
    {
        None = 0,
        MissingProfile = 1,
        InvalidRoomInstanceId = 2,
        InvalidAssignment = 3,
        UnsupportedCategory = 4,
        CapacityExceeded = 5,
        InsufficientCompatibleSlots = 6
    }

    public sealed class RoomContentPositionMigrationPlanEntry
    {
        private readonly TileCoordinate[] occupiedTiles;

        internal RoomContentPositionMigrationPlanEntry(RoomContentAssignment assignment,
            RoomContentMigrationSlot slot, IEnumerable<TileCoordinate> occupiedTiles)
        {
            AssignmentId = assignment.AssignmentId;
            RoomInstanceId = assignment.RoomInstanceId;
            CategoryId = assignment.CategoryId;
            OptionId = assignment.OptionId;
            Sequence = assignment.Sequence;
            SlotId = slot.SlotId;
            RoomLocalPosition = slot.Anchor;
            this.occupiedTiles = occupiedTiles.OrderBy(value => value).ToArray();
        }

        public string AssignmentId { get; }
        public string RoomInstanceId { get; }
        public string CategoryId { get; }
        public string OptionId { get; }
        public long Sequence { get; }
        public string SlotId { get; }
        public TileCoordinate RoomLocalPosition { get; }
        public IReadOnlyList<TileCoordinate> OccupiedTiles => Array.AsReadOnly(
            (TileCoordinate[])occupiedTiles.Clone());
    }

    public sealed class RoomContentPositionMigrationPlanResult
    {
        internal RoomContentPositionMigrationPlanResult(RoomContentPositionMigrationPlanFailure failure,
            IEnumerable<RoomContentPositionMigrationPlanEntry> entries = null)
        {
            Failure = failure;
            Entries = Array.AsReadOnly((entries ??
                Enumerable.Empty<RoomContentPositionMigrationPlanEntry>()).ToArray());
        }

        public bool Success => Failure == RoomContentPositionMigrationPlanFailure.None;
        public RoomContentPositionMigrationPlanFailure Failure { get; }
        public IReadOnlyList<RoomContentPositionMigrationPlanEntry> Entries { get; }
    }

    /// <summary>
    /// Pure inactive schema-12 placement planning. It never mutates assignments or canonical save state.
    /// </summary>
    public static class RoomContentPositionMigrationPlanner
    {
        public static RoomContentPositionMigrationPlanResult Plan(
            ValidatedRoomContentPositionMigrationProfile validatedProfile,
            string roomInstanceId, IEnumerable<RoomContentAssignment> sourceAssignments)
        {
            if (validatedProfile == null)
                return Failure(RoomContentPositionMigrationPlanFailure.MissingProfile);
            if (string.IsNullOrEmpty(roomInstanceId))
                return Failure(RoomContentPositionMigrationPlanFailure.InvalidRoomInstanceId);
            if (sourceAssignments == null)
                return Failure(RoomContentPositionMigrationPlanFailure.InvalidAssignment);

            RoomContentAssignment[] source = sourceAssignments.ToArray();
            if (source.Any(value => value == null || value.RoomInstanceId != roomInstanceId ||
                string.IsNullOrEmpty(value.AssignmentId) || string.IsNullOrEmpty(value.OptionId) ||
                value.Sequence < 0))
                return Failure(RoomContentPositionMigrationPlanFailure.InvalidAssignment);
            if (source.GroupBy(value => value.AssignmentId, StringComparer.Ordinal)
                .Any(group => group.Count() != 1) || source.GroupBy(value =>
                    value.RoomInstanceId + "\0" + value.CategoryId + "\0" +
                    value.Sequence.ToString(CultureInfo.InvariantCulture), StringComparer.Ordinal)
                .Any(group => group.Count() != 1))
                return Failure(RoomContentPositionMigrationPlanFailure.InvalidAssignment);
            if (source.Any(value => !RoomContentPositionMigrationProfiles.IsOrdinaryCategory(
                value.CategoryId)))
                return Failure(RoomContentPositionMigrationPlanFailure.UnsupportedCategory);

            RoomContentPositionMigrationProfile profile = validatedProfile.CloneValue();
            foreach (RoomContentMigrationCategoryCapacity capacity in profile.CategoryCapacities)
                if (source.Count(value => value.CategoryId == capacity.CategoryId) >
                    capacity.MaximumAssignments)
                    return Failure(RoomContentPositionMigrationPlanFailure.CapacityExceeded);

            RoomContentAssignment[] ordered =
                CanonicalSpatialSaveContracts.CanonicalOrderAssignments(source);
            RoomContentMigrationSlot[] slots = profile.OrderedSlots ??
                Array.Empty<RoomContentMigrationSlot>();
            var used = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<RoomContentPositionMigrationPlanEntry>(ordered.Length);
            foreach (RoomContentAssignment assignment in ordered)
            {
                RoomContentMigrationSlot slot = slots.FirstOrDefault(value => value != null &&
                    !used.Contains(value.SlotId) && (value.AllowedCategoryIds ??
                        Array.Empty<string>()).Contains(assignment.CategoryId));
                if (slot == null)
                    return Failure(RoomContentPositionMigrationPlanFailure.InsufficientCompatibleSlots);
                used.Add(slot.SlotId);
                TileCoordinate[] occupied = (slot.OccupiedTileOffsets ??
                    Array.Empty<TileCoordinate>()).Select(offset => new TileCoordinate(
                        slot.Anchor.X + offset.X, slot.Anchor.Y + offset.Y)).ToArray();
                result.Add(new RoomContentPositionMigrationPlanEntry(assignment, slot, occupied));
            }
            return new RoomContentPositionMigrationPlanResult(
                RoomContentPositionMigrationPlanFailure.None, result);
        }

        private static RoomContentPositionMigrationPlanResult Failure(
            RoomContentPositionMigrationPlanFailure failure) =>
            new RoomContentPositionMigrationPlanResult(failure);
    }

    public static class RoomContentPositionMigrationProfiles
    {
        public const string ProductionPath =
            "Assets/_Project/Data/Production/Save/room_content_position_migration_profiles.json";
        public const string SchemaId = "room_content_position_migration_profiles";
        public const int ContractVersion = 1;
        public const int FrozenSourceSaveSchemaVersion = 12;
        public const string ProductionProfileSetId =
            "compat.room_content_position.schema_12";
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        public static RoomContentPositionMigrationProfilesResult ParseAndValidate(
            TextAsset asset, ProductionSpatialContentSnapshot spatial,
            SpatialContentValidationWorkloadLimits structuralLimits,
            SpatialSerializedInputLimits serializedLimits,
            bool enforceProductionReleasePolicy = false) => ParseAndValidate(
                asset == null ? null : asset.bytes, spatial, structuralLimits, serializedLimits,
                enforceProductionReleasePolicy);

        public static RoomContentPositionMigrationProfilesResult ParseAndValidate(
            byte[] bytes, ProductionSpatialContentSnapshot spatial,
            SpatialContentValidationWorkloadLimits structuralLimits,
            SpatialSerializedInputLimits serializedLimits,
            bool enforceProductionReleasePolicy = false)
        {
            var diagnostics = new SortedSet<RoomContentPositionMigrationProfileDiagnostic>();
            if (bytes == null)
                return Finish(null, diagnostics, RoomContentPositionMigrationProfileDiagnostic.MissingInput);
            if (bytes.Length == 0)
                return Finish(null, diagnostics, RoomContentPositionMigrationProfileDiagnostic.EmptyInput);
            if (!structuralLimits.IsValid || !serializedLimits.IsValid ||
                bytes.Length > serializedLimits.MaximumInputBytes)
                return Finish(null, diagnostics, RoomContentPositionMigrationProfileDiagnostic.WorkloadExceeded);
            if (bytes.Length < 2 || bytes[bytes.Length - 1] != (byte)'\n' ||
                bytes[bytes.Length - 2] == (byte)'\n' || bytes.Contains((byte)'\r') ||
                bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf)
                return Finish(null, diagnostics, RoomContentPositionMigrationProfileDiagnostic.InvalidEncoding);
            try { Utf8.GetCharCount(bytes, 0, bytes.Length - 1); }
            catch { return Finish(null, diagnostics,
                RoomContentPositionMigrationProfileDiagnostic.InvalidEncoding); }

            byte[] body = new byte[bytes.Length - 1];
            Buffer.BlockCopy(bytes, 0, body, 0, body.Length);
            var contractIssues = new SpatialIssueCollector(serializedLimits.MaximumDiagnostics);
            if (!ContractJson.TryParse(body, serializedLimits, contractIssues,
                    out ContractJsonNode contractRoot, true) ||
                contractRoot.Kind != ContractJsonKind.Object)
                return Finish(null, diagnostics, contractIssues.ToArray().Any(value =>
                        value == SpatialContractIssue.WorkloadExceeded ||
                        value == SpatialContractIssue.InputByteLimitExceeded)
                    ? RoomContentPositionMigrationProfileDiagnostic.WorkloadExceeded
                    : RoomContentPositionMigrationProfileDiagnostic.InvalidJson);

            var strictIssues = new DiagnosticCollector(structuralLimits.MaximumIssues);
            var budget = new StrictJsonWorkloadBudget(structuralLimits);
            if (!StrictJson.TryParse(bytes, bytes.Length - 1,
                    typeof(RoomContentPositionMigrationProfilesData), strictIssues, budget,
                    out JsonNode root, out ProductionSpatialGeneratedSetDiagnostic parseDiagnostic) ||
                root.Kind != JsonKind.Object)
                return Finish(null, diagnostics,
                    parseDiagnostic == ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded ||
                    parseDiagnostic == ProductionSpatialGeneratedSetDiagnostic.DiagnosticLimitExceeded
                        ? RoomContentPositionMigrationProfileDiagnostic.WorkloadExceeded
                        : RoomContentPositionMigrationProfileDiagnostic.InvalidJson);
            StrictJson.Validate(typeof(RoomContentPositionMigrationProfilesData), root, strictIssues);
            if (strictIssues.HasAny)
                return Finish(null, diagnostics, strictIssues.Diagnostics.Any(value =>
                    value == ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded ||
                    value == ProductionSpatialGeneratedSetDiagnostic.DiagnosticLimitExceeded)
                        ? RoomContentPositionMigrationProfileDiagnostic.WorkloadExceeded
                        : RoomContentPositionMigrationProfileDiagnostic.InvalidJson);

            RoomContentPositionMigrationProfilesData parsed;
            try
            {
                parsed = JsonUtility.FromJson<RoomContentPositionMigrationProfilesData>(
                    StrictJson.ToCompactJson(root));
            }
            catch
            {
                return Finish(null, diagnostics,
                    RoomContentPositionMigrationProfileDiagnostic.InvalidJson);
            }

            if (parsed == null || parsed.Schema != SchemaId ||
                parsed.SchemaVersion != ContractVersion ||
                parsed.ProfileSetId != ProductionProfileSetId)
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.InvalidSchema);
            if (parsed == null || parsed.SourceSaveSchemaVersion != FrozenSourceSaveSchemaVersion)
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.UnsupportedSourceSchema);
            if (parsed == null || parsed.ProfileSetVersion != ContractVersion)
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.InvalidVersion);

            RoomContentPositionMigrationProfilesData canonical = Canonicalize(parsed);
            if (!bytes.SequenceEqual(SerializeCanonical(canonical)))
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.NoncanonicalInput);
            Validate(canonical, spatial?.Catalog, structuralLimits, diagnostics,
                enforceProductionReleasePolicy);
            if (diagnostics.Count != 0)
                return new RoomContentPositionMigrationProfilesResult(null, diagnostics);
            return new RoomContentPositionMigrationProfilesResult(
                new RoomContentPositionMigrationProfilesSnapshot(canonical), diagnostics);
        }

        public static RoomContentPositionMigrationProfilesData Canonicalize(
            RoomContentPositionMigrationProfilesData source)
        {
            RoomContentPositionMigrationProfilesData copy = Clone(source) ??
                new RoomContentPositionMigrationProfilesData();
            copy.Profiles = (copy.Profiles ?? Array.Empty<RoomContentPositionMigrationProfile>())
                .OrderBy(value => value?.RoomDefinitionId, StringComparer.Ordinal)
                .ThenBy(value => value == null ? 0 : (int)value.Orientation)
                .ThenBy(value => value?.ProfileId, StringComparer.Ordinal).ToArray();
            foreach (RoomContentPositionMigrationProfile profile in copy.Profiles.Where(
                value => value != null))
            {
                profile.FrozenReservedTileOffsets = (profile.FrozenReservedTileOffsets ??
                    Array.Empty<TileCoordinate>()).OrderBy(value => value).ToArray();
                profile.CategoryCapacities = (profile.CategoryCapacities ??
                    Array.Empty<RoomContentMigrationCategoryCapacity>())
                    .OrderBy(value => CanonicalSpatialSaveContracts.ContentCategoryRank(
                        value?.CategoryId)).ThenBy(value => value?.CategoryId,
                        StringComparer.Ordinal).ToArray();
                profile.OrderedSlots = (profile.OrderedSlots ??
                    Array.Empty<RoomContentMigrationSlot>()).OrderBy(value => value?.Order ?? 0)
                    .ThenBy(value => value?.SlotId, StringComparer.Ordinal).ToArray();
                foreach (RoomContentMigrationSlot slot in profile.OrderedSlots.Where(
                    value => value != null))
                {
                    slot.AllowedCategoryIds = (slot.AllowedCategoryIds ?? Array.Empty<string>())
                        .OrderBy(CanonicalSpatialSaveContracts.ContentCategoryRank)
                        .ThenBy(value => value, StringComparer.Ordinal).ToArray();
                    slot.OccupiedTileOffsets = (slot.OccupiedTileOffsets ??
                        Array.Empty<TileCoordinate>()).OrderBy(value => value).ToArray();
                }
            }
            return copy;
        }

        public static byte[] SerializeCanonical(RoomContentPositionMigrationProfilesData value) =>
            Utf8.GetBytes(JsonUtility.ToJson(value, true) + "\n");

        public static RoomContentPositionMigrationProfilesData WithComputedIntegrity(
            RoomContentPositionMigrationProfilesData value)
        {
            RoomContentPositionMigrationProfilesData canonical = Canonicalize(value);
            foreach (RoomContentPositionMigrationProfile profile in canonical.Profiles.Where(
                item => item != null))
                profile.CanonicalHash = ComputeProfileHash(profile);
            canonical = Canonicalize(canonical);
            canonical.CanonicalHash = ComputeSetHash(canonical);
            return Canonicalize(canonical);
        }

        public static string ComputeProfileHash(RoomContentPositionMigrationProfile value)
        {
            RoomContentPositionMigrationProfile copy = Clone(value);
            if (copy == null) return string.Empty;
            copy.CanonicalHash = string.Empty;
            var wrapper = new RoomContentPositionMigrationProfilesData { Profiles = new[] { copy } };
            RoomContentPositionMigrationProfile canonical = Canonicalize(wrapper).Profiles[0];
            return Sha256(JsonUtility.ToJson(canonical, false));
        }

        public static string ComputeSetHash(RoomContentPositionMigrationProfilesData value)
        {
            RoomContentPositionMigrationProfilesData copy = Canonicalize(value);
            if (copy == null) return string.Empty;
            copy.CanonicalHash = string.Empty;
            return Sha256(JsonUtility.ToJson(copy, false));
        }

        internal static bool IsOrdinaryCategory(string categoryId) =>
            CanonicalSpatialSaveContracts.ContentCategoryRank(categoryId) < 3;

        private static void Validate(RoomContentPositionMigrationProfilesData data,
            SpatialContentCatalog catalog, SpatialContentValidationWorkloadLimits limits,
            ISet<RoomContentPositionMigrationProfileDiagnostic> diagnostics,
            bool enforceProductionReleasePolicy)
        {
            if (data == null || catalog?.Rooms == null)
            {
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.UnknownRoom);
                return;
            }
            if (!ValidHash(data.CanonicalHash) || data.CanonicalHash != ComputeSetHash(data))
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.InvalidHash);
            RoomContentPositionMigrationProfile[] profiles = data.Profiles ??
                Array.Empty<RoomContentPositionMigrationProfile>();
            if (profiles.Any(value => value == null))
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.InvalidJson);
            if (profiles.Where(value => value != null).GroupBy(value => value.ProfileId,
                    StringComparer.Ordinal).Any(group => group.Count() != 1))
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.DuplicateProfileId);
            if (profiles.Where(value => value != null).GroupBy(value =>
                    (value.RoomDefinitionId ?? string.Empty) + "\0" + (int)value.Orientation,
                    StringComparer.Ordinal).Any(group => group.Count() != 1))
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.DuplicateRoomOrientation);

            foreach (RoomContentPositionMigrationProfile profile in profiles.Where(value => value != null))
                ValidateProfile(profile, catalog, limits, diagnostics, enforceProductionReleasePolicy);

            if (enforceProductionReleasePolicy)
            {
                string[] required = catalog.Rooms.Where(value => value != null)
                    .SelectMany(room => (room.AllowedOrientations ?? Array.Empty<CardinalOrientation>())
                        .Select(orientation => room.RoomDefinitionId + "\0" + (int)orientation))
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();
                string[] actual = profiles.Where(value => value != null)
                    .Select(value => value.RoomDefinitionId + "\0" + (int)value.Orientation)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();
                if (!actual.SequenceEqual(required, StringComparer.Ordinal))
                    diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.MissingProductionCoverage);
            }
        }

        private static void ValidateProfile(RoomContentPositionMigrationProfile profile,
            SpatialContentCatalog catalog, SpatialContentValidationWorkloadLimits limits,
            ISet<RoomContentPositionMigrationProfileDiagnostic> diagnostics,
            bool enforceProductionReleasePolicy)
        {
            if (!Stable(profile.ProfileId) || !Stable(profile.RoomDefinitionId))
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.InvalidStableId);
            if (profile.ProfileVersion != ContractVersion ||
                profile.SourceSaveSchemaVersion != FrozenSourceSaveSchemaVersion)
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.InvalidVersion);
            if (!ValidHash(profile.CanonicalHash) ||
                profile.CanonicalHash != ComputeProfileHash(profile))
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.InvalidHash);

            RoomSpatialDefinition[] matches = catalog.Rooms.Where(value => value != null &&
                value.RoomDefinitionId == profile.RoomDefinitionId).Take(2).ToArray();
            if (matches.Length != 1)
            {
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.UnknownRoom);
                return;
            }
            RoomSpatialDefinition room = matches[0];
            if (!Enum.IsDefined(typeof(CardinalOrientation), profile.Orientation) ||
                room.AllowedOrientations == null || !room.AllowedOrientations.Contains(profile.Orientation))
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.UnsupportedOrientation);
            int width = profile.Orientation == CardinalOrientation.Ninety ||
                profile.Orientation == CardinalOrientation.TwoSeventy
                    ? room.GrossFootprint?.Height ?? 0 : room.GrossFootprint?.Width ?? 0;
            int height = profile.Orientation == CardinalOrientation.Ninety ||
                profile.Orientation == CardinalOrientation.TwoSeventy
                    ? room.GrossFootprint?.Width ?? 0 : room.GrossFootprint?.Height ?? 0;
            if (profile.FrozenFootprint == null || profile.FrozenFootprint.Width != width ||
                profile.FrozenFootprint.Height != height || (long)width * height >
                limits.MaximumMaterializedTiles)
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.FrozenContextMismatch);

            TileCoordinate[] expectedReserved = TransformReserved(room, profile.Orientation);
            TileCoordinate[] frozenReserved = profile.FrozenReservedTileOffsets ??
                Array.Empty<TileCoordinate>();
            if (!frozenReserved.SequenceEqual(expectedReserved) ||
                frozenReserved.Distinct().Count() != frozenReserved.Length)
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.FrozenContextMismatch);

            RoomContentMigrationCategoryCapacity[] capacities = profile.CategoryCapacities ??
                Array.Empty<RoomContentMigrationCategoryCapacity>();
            var expectedCapacities = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { CanonicalSpatialSaveContracts.MonsterCategoryId, room.MonsterCapacity },
                { CanonicalSpatialSaveContracts.TrapCategoryId, room.TrapCapacity },
                { CanonicalSpatialSaveContracts.LootNodeCategoryId, room.LootCapacity }
            };
            if (capacities.Any(value => value == null || !IsOrdinaryCategory(value.CategoryId) ||
                    value.MaximumAssignments < 0) || capacities.GroupBy(value => value?.CategoryId,
                    StringComparer.Ordinal).Any(group => group.Count() != 1) ||
                capacities.Length != expectedCapacities.Count || capacities.Any(value =>
                    !expectedCapacities.TryGetValue(value.CategoryId, out int expected) ||
                    expected != value.MaximumAssignments))
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.InvalidCategoryCapacity);

            RoomContentMigrationSlot[] slots = profile.OrderedSlots ??
                Array.Empty<RoomContentMigrationSlot>();
            if (slots.Length != expectedCapacities.Values.Sum())
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.InsufficientSlots);
            if (slots.Any(value => value == null || !Stable(value.SlotId)))
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.InvalidStableId);
            if (slots.Where(value => value != null).GroupBy(value => value.SlotId,
                    StringComparer.Ordinal).Any(group => group.Count() != 1))
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.DuplicateSlotId);
            if (slots.Where(value => value != null).Select(value => value.Order).Distinct().Count() !=
                slots.Count(value => value != null) || !slots.Where(value => value != null)
                    .Select(value => value.Order).SequenceEqual(Enumerable.Range(0, slots.Length)))
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.DuplicateSlotOrder);

            var occupied = new HashSet<TileCoordinate>();
            var reserved = new HashSet<TileCoordinate>(frozenReserved);
            foreach (RoomContentMigrationSlot slot in slots.Where(value => value != null))
            {
                string[] allowed = slot.AllowedCategoryIds ?? Array.Empty<string>();
                if (allowed.Length == 0 || allowed.Any(value => !IsOrdinaryCategory(value)) ||
                    allowed.Distinct(StringComparer.Ordinal).Count() != allowed.Length)
                    diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.InvalidCategoryCompatibility);
                TileCoordinate[] offsets = slot.OccupiedTileOffsets ?? Array.Empty<TileCoordinate>();
                if (offsets.Length == 0 || offsets.Distinct().Count() != offsets.Length ||
                    offsets.LongLength > limits.MaximumMaterializedTiles)
                    diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.InvalidOccupancyFootprint);
                if (enforceProductionReleasePolicy && (offsets.Length != 1 ||
                    !offsets[0].Equals(new TileCoordinate(0, 0))))
                    diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.UnauthorizedProductionOccupancy);
                foreach (TileCoordinate offset in offsets)
                {
                    long x = (long)slot.Anchor.X + offset.X;
                    long y = (long)slot.Anchor.Y + offset.Y;
                    if (x < 0 || y < 0 || x >= width || y >= height)
                    {
                        diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.OutOfFootprint);
                        continue;
                    }
                    var tile = new TileCoordinate((int)x, (int)y);
                    if (reserved.Contains(tile))
                        diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.ReservedTileOverlap);
                    if (!occupied.Add(tile))
                        diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.SlotOverlap);
                }
            }
            if (!CanMapMaximumEnvelope(capacities, slots))
                diagnostics.Add(RoomContentPositionMigrationProfileDiagnostic.InsufficientSlots);
        }

        private static bool CanMapMaximumEnvelope(
            IEnumerable<RoomContentMigrationCategoryCapacity> capacities,
            IEnumerable<RoomContentMigrationSlot> slots)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (RoomContentMigrationCategoryCapacity capacity in (capacities ??
                Enumerable.Empty<RoomContentMigrationCategoryCapacity>()).Where(value => value != null)
                .OrderBy(value => CanonicalSpatialSaveContracts.ContentCategoryRank(value.CategoryId)))
            {
                for (int index = 0; index < capacity.MaximumAssignments; index++)
                {
                    RoomContentMigrationSlot slot = (slots ?? Enumerable.Empty<RoomContentMigrationSlot>())
                        .FirstOrDefault(value => value != null && !used.Contains(value.SlotId) &&
                            (value.AllowedCategoryIds ?? Array.Empty<string>()).Contains(capacity.CategoryId));
                    if (slot == null) return false;
                    used.Add(slot.SlotId);
                }
            }
            return true;
        }

        private static TileCoordinate[] TransformReserved(RoomSpatialDefinition room,
            CardinalOrientation orientation)
        {
            if (room?.GrossFootprint == null) return Array.Empty<TileCoordinate>();
            return (room.ReservedTileOffsets ?? Array.Empty<TileCoordinate>()).Select(offset =>
            {
                switch (orientation)
                {
                    case CardinalOrientation.Ninety:
                        return new TileCoordinate(offset.Y, room.GrossFootprint.Width - 1 - offset.X);
                    case CardinalOrientation.OneEighty:
                        return new TileCoordinate(room.GrossFootprint.Width - 1 - offset.X,
                            room.GrossFootprint.Height - 1 - offset.Y);
                    case CardinalOrientation.TwoSeventy:
                        return new TileCoordinate(room.GrossFootprint.Height - 1 - offset.Y, offset.X);
                    default: return offset;
                }
            }).OrderBy(value => value).ToArray();
        }

        private static RoomContentPositionMigrationProfilesResult Finish(
            RoomContentPositionMigrationProfilesSnapshot value,
            ISet<RoomContentPositionMigrationProfileDiagnostic> diagnostics,
            RoomContentPositionMigrationProfileDiagnostic diagnostic)
        {
            diagnostics.Add(diagnostic);
            return new RoomContentPositionMigrationProfilesResult(value, diagnostics);
        }

        private static bool Stable(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            bool separator = true;
            foreach (char character in value)
            {
                bool atom = character >= 'a' && character <= 'z' ||
                    character >= '0' && character <= '9';
                if (atom) { separator = false; continue; }
                if ((character == '.' || character == '_' || character == '-') && !separator)
                { separator = true; continue; }
                return false;
            }
            return !separator;
        }

        private static bool ValidHash(string value) => value != null && value.Length == 64 &&
            value.All(character => character >= '0' && character <= '9' ||
                character >= 'a' && character <= 'f');

        private static string Sha256(string value)
        {
            using (SHA256 hash = SHA256.Create())
                return string.Concat(hash.ComputeHash(Utf8.GetBytes(value))
                    .Select(item => item.ToString("x2")));
        }

        private static T Clone<T>(T value) where T : class => value == null ? null :
            JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
    }
}
