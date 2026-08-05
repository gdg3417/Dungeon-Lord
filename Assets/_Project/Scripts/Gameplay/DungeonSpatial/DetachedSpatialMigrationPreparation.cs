using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class DetachedRequiredValidationInputSpecification
    {
        private readonly string[] inputIds;
        private DetachedRequiredValidationInputSpecification(IEnumerable<string> values)
        { inputIds = (values ?? Array.Empty<string>()).OrderBy(value => value, StringComparer.Ordinal).ToArray(); }

        // Locked GD66 design §25: contract 1 has no extension validation-input IDs; profile,
        // geometry, production, legacy-config, serializer, marker, and contract pins are named fields.
        public static DetachedRequiredValidationInputSpecification Current { get; } =
            new DetachedRequiredValidationInputSpecification(Array.Empty<string>());
        public string[] InputIds => (string[])inputIds.Clone();
        public string SerializerId => SpatialMigrationContractIdentity.CanonicalSerializerId;
        public int SerializerVersion => SpatialMigrationContractIdentity.CanonicalSerializerVersion;
        public int MarkerContractVersion => SpatialMigrationContractIdentity.AuthorityMarkerContractVersion;
        public int MigrationContractVersion => SpatialMigrationContractIdentity.MigrationContractVersion;
        public int TargetSchemaVersion => DetachedWholeSaveCandidateSerializer.TargetSchemaVersion;

        internal string Validate(IReadOnlyDictionary<string, byte[]> values,
            IEnumerable<SpatialValidationInputHash> descriptorPins)
        {
            SpatialValidationInputHash[] pins = (descriptorPins ?? Array.Empty<SpatialValidationInputHash>()).ToArray();
            if (pins.Any(pin => pin == null) || pins.Select(pin => pin.InputId).Distinct(StringComparer.Ordinal).Count() != pins.Length)
                return "gd66.transaction.pinned_input_hash_mismatch";
            if (pins.Length != inputIds.Length || (values?.Count ?? 0) != inputIds.Length)
                return values == null || pins.Length < inputIds.Length
                    ? "gd66.transaction.pinned_input_missing" : "gd66.transaction.pinned_input_hash_mismatch";
            for (int index = 0; index < inputIds.Length; index++)
            {
                SpatialValidationInputHash pin = pins.SingleOrDefault(value => value.InputId == inputIds[index]);
                if (pin == null || values == null || !values.TryGetValue(inputIds[index], out byte[] bytes) || bytes == null)
                    return "gd66.transaction.pinned_input_missing";
                if (SpatialContractSha256.Compute(bytes) != pin.Sha256)
                    return "gd66.transaction.pinned_input_hash_mismatch";
            }
            return null;
        }
    }

    public static class LegacyGameplayConfigurationContract
    {
        public static byte[] SerializeCanonical(RunSimulationConfig value)
        {
            if (value == null) return null;
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(value, true) + "\n");
        }

        public static RunSimulationConfig Parse(byte[] bytes)
        {
            if (bytes == null) return null;
            string text = new UTF8Encoding(false, true).GetString(bytes);
            RunSimulationConfig value = JsonUtility.FromJson<RunSimulationConfig>(text);
            byte[] again = SerializeCanonical(value);
            if (!bytes.SequenceEqual(again)) throw new FormatException();
            return value;
        }

        public static string ComputeSha256(RunSimulationConfig value) =>
            SpatialContractSha256.Compute(SerializeCanonical(value));
    }

    public sealed class DetachedSpatialMigrationPreparationInputs
    {
        private readonly byte[] exactOriginalBytes;
        private readonly byte[] profileBytes;
        private readonly byte[] geometryBytes;
        private readonly byte[] legacyConfigurationBytes;
        private readonly SpatialLayoutCompatibilitySnapshot compatibilitySnapshot;
        private readonly Dictionary<string, byte[]> validationInputs;

        public DetachedSpatialMigrationPreparationInputs(byte[] exactOriginalBytes,
            RawSavePayloadClassification classification, SpatialMigrationInputDescriptor descriptorInputs,
            SpatialLayoutCompatibilitySnapshot compatibility, ProductionSpatialContentSnapshot productionContent,
            RunSimulationConfig legacyGameplayConfiguration, CanonicalSpatialSerializationLimits spatialLimits,
            DetachedWholeSaveLimits wholeSaveLimits)
            : this(exactOriginalBytes, classification, descriptorInputs, null, null, productionContent,
                legacyGameplayConfiguration, null, spatialLimits, wholeSaveLimits, compatibility)
        { }

        public DetachedSpatialMigrationPreparationInputs(byte[] exactOriginalBytes,
            RawSavePayloadClassification classification, SpatialMigrationInputDescriptor descriptorInputs,
            SpatialLayoutCompatibilitySnapshot compatibility, ProductionSpatialContentSnapshot productionContent,
            RunSimulationConfig legacyGameplayConfiguration, IReadOnlyDictionary<string, byte[]> validationInputs,
            CanonicalSpatialSerializationLimits spatialLimits, DetachedWholeSaveLimits wholeSaveLimits)
            : this(exactOriginalBytes, classification, descriptorInputs, null, null, productionContent,
                legacyGameplayConfiguration, validationInputs, spatialLimits, wholeSaveLimits, compatibility)
        { }

        private DetachedSpatialMigrationPreparationInputs(byte[] exactOriginalBytes,
            RawSavePayloadClassification classification, SpatialMigrationInputDescriptor descriptorInputs,
            SpatialMigrationCompatibilityProfile profile, CompatibilityLayoutGeometryRecord geometry,
            ProductionSpatialContentSnapshot productionContent, RunSimulationConfig legacyGameplayConfiguration,
            IReadOnlyDictionary<string, byte[]> validationInputs,
            CanonicalSpatialSerializationLimits spatialLimits, DetachedWholeSaveLimits wholeSaveLimits,
            SpatialLayoutCompatibilitySnapshot compatibility = null)
        {
            this.exactOriginalBytes = exactOriginalBytes == null ? null : (byte[])exactOriginalBytes.Clone();
            profileBytes = profile == null ? null : Encoding.UTF8.GetBytes(JsonUtility.ToJson(profile));
            geometryBytes = geometry == null ? null : Encoding.UTF8.GetBytes(JsonUtility.ToJson(geometry));
            legacyConfigurationBytes = legacyGameplayConfiguration == null ? null :
                LegacyGameplayConfigurationContract.SerializeCanonical(legacyGameplayConfiguration);
            Classification = classification; DescriptorInputs = descriptorInputs; ProductionContent = productionContent;
            this.validationInputs = validationInputs == null ? null : validationInputs.ToDictionary(
                pair => pair.Key, pair => pair.Value == null ? null : (byte[])pair.Value.Clone(), StringComparer.Ordinal);
            SpatialLimits = spatialLimits; WholeSaveLimits = wholeSaveLimits;
            compatibilitySnapshot = compatibility;
        }
        public byte[] GetExactOriginalBytes() => exactOriginalBytes == null ? null : (byte[])exactOriginalBytes.Clone();
        internal byte[] OwnedOriginalBytes => exactOriginalBytes;
        public RawSavePayloadClassification Classification { get; }
        public SpatialMigrationInputDescriptor DescriptorInputs { get; }
        public SpatialMigrationCompatibilityProfile Profile => profileBytes == null ? null :
            JsonUtility.FromJson<SpatialMigrationCompatibilityProfile>(Encoding.UTF8.GetString(profileBytes));
        public CompatibilityLayoutGeometryRecord Geometry => geometryBytes == null ? null :
            JsonUtility.FromJson<CompatibilityLayoutGeometryRecord>(Encoding.UTF8.GetString(geometryBytes));
        public ProductionSpatialContentSnapshot ProductionContent { get; }
        public RunSimulationConfig LegacyGameplayConfiguration => legacyConfigurationBytes == null ? null :
            LegacyGameplayConfigurationContract.Parse(legacyConfigurationBytes);
        internal byte[] LegacyConfigurationBytes => (byte[])legacyConfigurationBytes.Clone();
        public CanonicalSpatialSerializationLimits SpatialLimits { get; }
        public DetachedWholeSaveLimits WholeSaveLimits { get; }
        internal SpatialLayoutCompatibilitySnapshot CompatibilitySnapshot => compatibilitySnapshot;
        internal IReadOnlyDictionary<string, byte[]> ValidationInputs => validationInputs;

        internal static DetachedSpatialMigrationPreparationInputs FromValidatedResolution(
            DetachedSpatialMigrationPreparationInputs source, SpatialMigrationCompatibilityProfile profile,
            CompatibilityLayoutGeometryRecord geometry) =>
            new DetachedSpatialMigrationPreparationInputs(source.OwnedOriginalBytes, source.Classification,
                source.DescriptorInputs, profile, geometry, source.ProductionContent,
                source.LegacyGameplayConfiguration, source.ValidationInputs, source.SpatialLimits,
                source.WholeSaveLimits, source.CompatibilitySnapshot);
    }

    public sealed class DetachedPreparedSpatialMigrationAttempt
    {
        private readonly byte[] original;
        internal DetachedPreparedSpatialMigrationAttempt(byte[] source, SpatialMigrationInputDescriptor descriptor,
            string fingerprint, string identity, string transactionId, DetachedWholeSaveCandidate candidate,
            CanonicalSpatialAuthorityMarker marker, bool isEmptyMigration, IEnumerable<string> diagnostics)
        {
            original = (byte[])source.Clone(); Descriptor = descriptor; DescriptorFingerprint = fingerprint;
            TransactionIdentity = identity; TransactionId = transactionId; Candidate = candidate; Marker = marker;
            IsEmptyMigration = isEmptyMigration;
            Diagnostics = (diagnostics ?? Array.Empty<string>()).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }
        public byte[] GetOriginalBytes() => (byte[])original.Clone();
        public SpatialMigrationInputDescriptor Descriptor { get; }
        public string DescriptorFingerprint { get; }
        public string TransactionIdentity { get; }
        public string TransactionId { get; }
        public DetachedWholeSaveCandidate Candidate { get; }
        public string CandidateSha256 => Candidate.Sha256;
        public CanonicalSpatialAuthorityMarker Marker { get; }
        public bool IsEmptyMigration { get; }
        public string[] Diagnostics { get; }
    }

    public sealed class DetachedSpatialMigrationPreparationResult
    {
        internal DetachedSpatialMigrationPreparationResult(DetachedPreparedSpatialMigrationAttempt attempt,
            string reason, IEnumerable<string> diagnostics)
        { Attempt = attempt; Reason = reason; Diagnostics = (diagnostics ?? Array.Empty<string>()).ToArray(); }
        public DetachedPreparedSpatialMigrationAttempt Attempt { get; }
        public string Reason { get; }
        public string[] Diagnostics { get; }
        public bool IsSuccess => Attempt != null;
    }

    public static class DetachedSpatialMigrationPreparer
    {
        public const string InvalidInputReason = "gd66.transaction.pinned_input_hash_mismatch";
        public const string DuplicateAssignmentReason = "gd66.content.duplicate_assignment";
        public const string DuplicateRoomSlotReason = "gd66.route.duplicate_room_slot";
        public const string DuplicateFloorRevisionReason = "gd66.route.duplicate_floor_node_revision";
        public const string DuplicatePlacementRevisionReason = "gd66.route.duplicate_placement_revision";
        public const string RouteGapReason = "gd66.route.gap";
        public const string OutcomeMismatchReason = "gd66.content.outcome_mismatch";
        public const string NarrowHallReason = "gd66.content.migration_blocked_narrow_hall";
        public const string InvalidOptionReason = "gd66.content.invalid_option";
        public const string CapacityReason = "gd66.content.room_capacity_exceeded";
        public const string ProfileInvalidReason = "gd66.profile.invalid";

        public static DetachedSpatialMigrationPreparationResult Prepare(
            DetachedSpatialMigrationPreparationInputs inputs)
        {
            if (inputs == null || inputs.OwnedOriginalBytes == null || inputs.Classification == null)
                return Failure(InvalidInputReason);
            string ownedOriginalHash = SpatialContractSha256.Compute(inputs.OwnedOriginalBytes);
            if (!string.Equals(ownedOriginalHash, inputs.Classification.SourcePayloadSha256,
                StringComparison.Ordinal)) return Failure("gd66.transaction.input_fingerprint_mismatch");
            if (!inputs.Classification.IsSuccess || inputs.DescriptorInputs == null || inputs.ProductionContent == null ||
                inputs.LegacyGameplayConfiguration == null || !inputs.SpatialLimits.IsValid ||
                !inputs.WholeSaveLimits.IsValid) return Failure(InvalidInputReason);
            try
            {
                DetachedSpatialMigrationPreparationInputs resolvedInputs = ResolveCompatibility(inputs,
                    out string compatibilityReason);
                if (resolvedInputs == null) return Failure(compatibilityReason);
                inputs = resolvedInputs;
                string originalHash = ownedOriginalHash;
                SpatialMigrationInputDescriptor descriptor = Descriptor(originalHash, inputs);
                SpatialContractResult<byte[]> descriptorBytes = SpatialMigrationDescriptorContracts.Serialize(
                    descriptor, inputs.SpatialLimits.Serialized);
                if (!descriptorBytes.IsValid) return Failure(InvalidInputReason);
                if (!PinsMatch(inputs, descriptor, out string pinReason)) return Failure(pinReason);
                string fingerprint = SpatialContractSha256.Compute(descriptorBytes.Value);
                string identity = SpatialMigrationTransactionIdentity.ComputeIdentity(originalHash, fingerprint);
                string transactionId = SpatialMigrationTransactionIdentity.CreateTransactionId(identity);
                var marker = new CanonicalSpatialAuthorityMarker
                {
                    CanonicalLayoutContractVersion = inputs.Profile.TargetCanonicalLayoutContractVersion,
                    CreationKind = CanonicalSpatialCreationKind.Migrated,
                    MigrationTransactionId = transactionId,
                    MigrationDescriptorFingerprint = fingerprint
                };
                FrozenLegacyRouteProjectionResult projection = FrozenLegacyRouteProjection.Project(inputs);
                if (!projection.IsSuccess) return Failure(projection.Reason, projection.Diagnostics);
                var spatial = new DetachedCanonicalSpatialSaveState { Authority = marker, Floors = projection.Floors };
                DetachedWholeSaveResult candidate = DetachedWholeSaveCandidateSerializer.BuildPrepared(
                    inputs.Classification, spatial, inputs.SpatialLimits, inputs.WholeSaveLimits);
                if (!candidate.IsSuccess) return Failure(candidate.Reason, projection.Diagnostics);
                CompatibilitySelectionResult<CanonicalLayoutContractSelection> selectedContract =
                    inputs.CompatibilitySnapshot.SelectContract(descriptor.SelectedTargetSchemaVersion);
                if (!selectedContract.Success) return Failure(selectedContract.Code, projection.Diagnostics);
                var validationContext = new DetachedUnfinishedAttemptValidationContext(descriptor,
                    transactionId, fingerprint, candidate.Candidate.Sha256, selectedContract.Value, inputs.Profile, inputs.Geometry, inputs.ProductionContent,
                    inputs.LegacyConfigurationBytes, inputs.ValidationInputs, inputs.SpatialLimits);
                if (!DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                    candidate.Candidate.GetBytes(), validationContext).IsValid)
                    return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason, projection.Diagnostics);
                return new DetachedSpatialMigrationPreparationResult(
                    new DetachedPreparedSpatialMigrationAttempt(inputs.OwnedOriginalBytes, descriptor, fingerprint,
                        identity, transactionId, candidate.Candidate, marker, projection.Floors.Length == 0,
                        projection.Diagnostics), null,
                    projection.Diagnostics);
            }
            catch (ArgumentException) { return Failure(InvalidInputReason); }
            catch (InvalidOperationException) { return Failure(ProfileInvalidReason); }
        }

        private static DetachedSpatialMigrationPreparationInputs ResolveCompatibility(
            DetachedSpatialMigrationPreparationInputs inputs, out string reason)
        {
            reason = null;
            if (inputs.Profile != null && inputs.Geometry != null) return inputs;
            if (inputs.CompatibilitySnapshot == null)
            { reason = "gd66.profile.missing"; return null; }
            int rawSchema = inputs.Classification.Envelope == RawSaveEnvelopeKind.UnwrappedSaveData
                ? 1 : inputs.Classification.SchemaVersion ?? 0;
            CompatibilitySelectionResult<CanonicalLayoutContractSelection> contract =
                inputs.CompatibilitySnapshot.SelectContract(
                    inputs.DescriptorInputs.SelectedTargetSchemaVersion);
            if (!contract.Success) { reason = contract.Code; return null; }
            CompatibilitySelectionResult<SpatialMigrationCompatibilityProfile> selection =
                inputs.CompatibilitySnapshot.SelectMigration(rawSchema,
                    inputs.DescriptorInputs.SelectedTargetSchemaVersion,
                    contract.Value.CanonicalLayoutContractVersion);
            if (!selection.Success) { reason = selection.Code; return null; }
            if (selection.Value.Lifecycle != CompatibilityProfileLifecycle.Active ||
                selection.Value.CanonicalHash != SpatialLayoutCompatibilityProfiles.ComputeMigrationProfileHash(selection.Value))
            { reason = "gd66.profile.invalid"; return null; }
            SpatialLayoutCompatibilityProfilesData data = inputs.CompatibilitySnapshot.Value;
            CompatibilityLayoutGeometryRecord[] matches = (data.GeometryRecords ??
                Array.Empty<CompatibilityLayoutGeometryRecord>()).Where(value => value != null &&
                value.GeometryId == selection.Value.GeometryId &&
                value.GeometryVersion == selection.Value.GeometryVersion &&
                value.CanonicalHash == selection.Value.GeometryCanonicalHash).ToArray();
            if (matches.Length != 1) { reason = "gd66.profile.invalid"; return null; }
            if (matches[0].CanonicalHash != SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(matches[0]))
            { reason = "gd66.profile.invalid"; return null; }
            return DetachedSpatialMigrationPreparationInputs.FromValidatedResolution(inputs,
                selection.Value, matches[0]);
        }

        private static SpatialMigrationInputDescriptor Descriptor(string originalHash,
            DetachedSpatialMigrationPreparationInputs inputs)
        {
            SpatialMigrationInputDescriptor source = inputs.DescriptorInputs;
            int schema = inputs.Classification.Envelope == RawSaveEnvelopeKind.UnwrappedSaveData
                ? 1 : inputs.Classification.SchemaVersion ?? 0;
            return new SpatialMigrationInputDescriptor(originalHash, schema,
                inputs.Classification.Envelope == RawSaveEnvelopeKind.WrappedSaveRoot
                    ? SpatialRawEnvelopeClassification.WrappedSaveRoot
                    : SpatialRawEnvelopeClassification.UnwrappedSaveData,
                source.SelectedTargetSchemaVersion, source.AuthorityMarkerContractVersion,
                source.MigrationContractVersion, inputs.Profile.ProfileId, inputs.Profile.ProfileVersion,
                inputs.Profile.CanonicalHash, inputs.Geometry.GeometryId, inputs.Geometry.GeometryVersion,
                inputs.Geometry.CanonicalHash, source.ProductionManifestSha256,
                source.ProductionCatalogSha256, source.ValidationInputHashes,
                source.LegacyGameplayConfigurationSha256, source.CanonicalSerializerId,
                source.CanonicalSerializerVersion);
        }

        private static bool PinsMatch(DetachedSpatialMigrationPreparationInputs inputs,
            SpatialMigrationInputDescriptor descriptor, out string reason)
        {
            reason = null;
            byte[] manifest = ProductionSpatialGeneratedSetParser.SerializeCanonical(inputs.ProductionContent.Manifest);
            byte[] catalog = ProductionSpatialGeneratedSetParser.SerializeCanonical(inputs.ProductionContent.Catalog);
            byte[] gameplay = inputs.LegacyConfigurationBytes;
            bool fixedPins = descriptor.SelectedTargetSchemaVersion == DetachedWholeSaveCandidateSerializer.TargetSchemaVersion &&
                descriptor.AuthorityMarkerContractVersion == SpatialMigrationContractIdentity.AuthorityMarkerContractVersion &&
                descriptor.MigrationContractVersion == SpatialMigrationContractIdentity.MigrationContractVersion &&
                string.Equals(descriptor.ProductionManifestSha256, SpatialContractSha256.Compute(manifest), StringComparison.Ordinal) &&
                string.Equals(descriptor.ProductionCatalogSha256, SpatialContractSha256.Compute(catalog), StringComparison.Ordinal) &&
                string.Equals(descriptor.LegacyGameplayConfigurationSha256, SpatialContractSha256.Compute(gameplay), StringComparison.Ordinal) &&
                inputs.Profile.TargetSchemaVersion == descriptor.SelectedTargetSchemaVersion &&
                inputs.Profile.GeometryId == inputs.Geometry.GeometryId &&
                inputs.Profile.GeometryVersion == inputs.Geometry.GeometryVersion &&
                inputs.Profile.GeometryCanonicalHash == inputs.Geometry.CanonicalHash;
            if (!fixedPins) { reason = "gd66.transaction.pinned_input_hash_mismatch"; return false; }
            string registryReason = DetachedRequiredValidationInputSpecification.Current.Validate(
                inputs.ValidationInputs, descriptor.ValidationInputHashes);
            if (registryReason != null) { reason = registryReason; return false; }
            if ((inputs.ValidationInputs?.Count ?? 0) != descriptor.ValidationInputHashes.Length)
            { reason = inputs.ValidationInputs == null ? "gd66.transaction.pinned_input_missing" :
                "gd66.transaction.pinned_input_hash_mismatch"; return false; }
            foreach (SpatialValidationInputHash pin in descriptor.ValidationInputHashes)
            {
                if (inputs.ValidationInputs == null || !inputs.ValidationInputs.TryGetValue(pin.InputId,
                    out byte[] bytes) || bytes == null)
                { reason = "gd66.transaction.pinned_input_missing"; return false; }
                if (!string.Equals(pin.Sha256, SpatialContractSha256.Compute(bytes), StringComparison.Ordinal))
                { reason = "gd66.transaction.pinned_input_hash_mismatch"; return false; }
            }
            return true;
        }

        private static DetachedSpatialMigrationPreparationResult Failure(string reason,
            IEnumerable<string> diagnostics = null) =>
            new DetachedSpatialMigrationPreparationResult(null, reason, diagnostics);
    }

    internal sealed class FrozenLegacyRouteProjectionResult
    {
        internal FrozenLegacyRouteProjectionResult(SavedSpatialFloor[] floors, string reason,
            IEnumerable<string> diagnostics)
        { Floors = floors; Reason = reason; Diagnostics = (diagnostics ?? Array.Empty<string>()).ToArray(); }
        internal SavedSpatialFloor[] Floors { get; }
        internal string Reason { get; }
        internal string[] Diagnostics { get; }
        internal bool IsSuccess => Floors != null;
    }

    internal static class FrozenLegacyRouteProjection
    {
        private const string AgreementDiagnostic = "gd66.diagnostic.lower_model_agreement";
        private const string IneffectiveDiagnostic = "gd66.diagnostic.lower_ineffective_conflict";
        private const string EffectiveContentDiagnostic = "gd66.diagnostic.lower_effective_content_contributed";
        private const string NoRouteDiagnostic = "gd66.diagnostic.no_legacy_route";
        private const string MissingRoomDiagnostic = "gd66.diagnostic.missing_explicit_room_supported_content";
        private const string ImplicitRoomDiagnostic = "gd66.diagnostic.implicit_basic_container_created";

        internal static FrozenLegacyRouteProjectionResult Project(DetachedSpatialMigrationPreparationInputs inputs)
        {
            var diagnostics = new List<string>();
            RawSavePayloadClassification source = inputs.Classification;
            RouteRoom[] rooms;
            if (source.RoomSlotAssignmentsPresence == RawLegacyRoutePresence.Present)
            {
                if (!TryAssignments(source, inputs.SpatialLimits.Serialized, out rooms, out string reason)) return Failure(reason);
                CompareLower(source, inputs.SpatialLimits.Serialized, rooms, diagnostics);
            }
            else if (source.FloorLayoutPresence == RawLegacyRoutePresence.Present)
            {
                if (!TryFloor(source, inputs.SpatialLimits.Serialized, out rooms, out string reason)) return Failure(reason);
                if (!MergeEffectivePlacements(source, inputs.SpatialLimits.Serialized, ref rooms, diagnostics, out reason)) return Failure(reason);
            }
            else if (source.DungeonPlacementsPresence == RawLegacyRoutePresence.Present)
            {
                if (!TryPlacements(source, inputs.SpatialLimits.Serialized, out rooms, out string reason)) return Failure(reason);
            }
            else
            {
                diagnostics.Add(NoRouteDiagnostic);
                return new FrozenLegacyRouteProjectionResult(Array.Empty<SavedSpatialFloor>(), null, diagnostics);
            }
            if (rooms.Length == 0) return new FrozenLegacyRouteProjectionResult(Array.Empty<SavedSpatialFloor>(), null, diagnostics);
            if (rooms.Any(value => !value.Explicit))
            { diagnostics.Add(MissingRoomDiagnostic); diagnostics.Add(ImplicitRoomDiagnostic); }
            return BuildFloor(inputs, rooms, diagnostics);
        }

        private static bool TryAssignments(RawSavePayloadClassification source, SpatialSerializedInputLimits limits, out RouteRoom[] rooms,
            out string reason)
        {
            rooms = null; reason = null;
            MvpRoomSlotAssignmentCollection data = RawLegacyRouteContracts.ParseAssignments(source, "mvpRoomSlotAssignments", limits, out string parseReason);
            if (data == null) { reason = parseReason; return false; }
            if (data?.Rooms == null) { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return false; }
            var keys = new HashSet<string>(StringComparer.Ordinal); var result = new List<RouteRoom>();
            foreach (MvpRoomSlotAssignmentState room in data.Rooms)
            {
                if (room == null || room.FloorIndex != 0 || room.RoomIndex < 0 || room.RoomIndex > 1)
                { reason = "gd66.route.record_out_of_range"; return false; }
                if (
                    !keys.Add(room.FloorIndex.ToString(CultureInfo.InvariantCulture) + ":" + room.RoomIndex.ToString(CultureInfo.InvariantCulture)))
                { reason = DetachedSpatialMigrationPreparer.DuplicateRoomSlotReason; return false; }
                result.Add(new RouteRoom(room.RoomIndex, room.RoomOptionId, true,
                    room.MonsterOptionIds, room.TrapOptionIds, room.LootNodeOptionIds));
            }
            result.Sort((a, b) => a.Index.CompareTo(b.Index));
            for (int index = 0; index < result.Count; index++) if (result[index].Index != index)
            { reason = DetachedSpatialMigrationPreparer.RouteGapReason; return false; }
            rooms = result.ToArray(); return ValidateOptions(rooms, out reason);
        }

        private static bool TryFloor(RawSavePayloadClassification source, SpatialSerializedInputLimits limits, out RouteRoom[] rooms, out string reason)
        {
            rooms = null; reason = null;
            MvpDungeonFloorLayoutState data = RawLegacyRouteContracts.ParseFloor(source, "mvpDungeonFloorLayout", limits, out string parseReason);
            if (data == null) { reason = parseReason; return false; }
            if (data?.Nodes == null) { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return false; }
            var selected = new Dictionary<string, MvpDungeonNodeState>(StringComparer.Ordinal);
            var slotOwners = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (MvpDungeonNodeState node in data.Nodes)
            {
                if (node == null || node.FloorIndex != 0 || node.NodeIndex < 0 ||
                    node.NodeIndex >= MvpDungeonPlacementIds.OrderedCategoryIds.Length ||
                    string.IsNullOrEmpty(node.SlotId) || node.Revision < 0)
                { reason = "gd66.route.record_out_of_range"; return false; }
                string identity = node.FloorIndex.ToString(CultureInfo.InvariantCulture) + ":" +
                    node.NodeIndex.ToString(CultureInfo.InvariantCulture);
                if (slotOwners.TryGetValue(node.SlotId, out string owner) && owner != identity)
                { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return false; }
                slotOwners[node.SlotId] = identity;
            }
            foreach (IGrouping<string, MvpDungeonNodeState> group in data.Nodes.GroupBy(node =>
                node.FloorIndex.ToString(CultureInfo.InvariantCulture) + ":" +
                node.NodeIndex.ToString(CultureInfo.InvariantCulture), StringComparer.Ordinal))
            {
                int greatest = group.Max(node => node.Revision); MvpDungeonNodeState[] tied = group.Where(node => node.Revision == greatest).ToArray();
                if (tied.Length != 1) { reason = DetachedSpatialMigrationPreparer.DuplicateFloorRevisionReason; return false; }
                MvpDungeonNodeState winner = tied[0];
                if (string.IsNullOrEmpty(winner.CategoryId) && string.IsNullOrEmpty(winner.OptionId)) continue;
                if (!MvpDungeonPlacementIds.IsAllowedCategory(winner.CategoryId) ||
                    !MvpDungeonPlacementIds.TryGetCategoryForOption(winner.OptionId, out string optionCategory) ||
                    optionCategory != winner.CategoryId || selected.ContainsKey(winner.CategoryId))
                { reason = "gd66.content.category_mismatch"; return false; }
                selected[winner.CategoryId] = winner;
            }
            rooms = FromCategories(selected.ToDictionary(pair => pair.Key,
                pair => pair.Value.OptionId, StringComparer.Ordinal));
            return ValidateOptions(rooms, out reason);
        }

        private static bool TryPlacements(RawSavePayloadClassification source, SpatialSerializedInputLimits limits, out RouteRoom[] rooms, out string reason)
        {
            rooms = null; reason = null;
            MvpDungeonPlacementState data = RawLegacyRouteContracts.ParsePlacements(source, "mvpDungeonPlacements", limits, out string parseReason);
            if (data == null) { reason = parseReason; return false; }
            if (data?.Entries == null) { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return false; }
            var selected = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (IGrouping<string, MvpDungeonPlacementEntry> group in data.Entries.Where(value => value != null)
                .GroupBy(value => value.CategoryId, StringComparer.Ordinal))
            {
                if (!MvpDungeonPlacementIds.IsAllowedCategory(group.Key) || group.Any(value => value.Revision < 0))
                { reason = "gd66.route.record_out_of_range"; return false; }
                int greatest = group.Max(value => value.Revision); MvpDungeonPlacementEntry[] tied = group.Where(value => value.Revision == greatest).ToArray();
                if (tied.Length != 1) { reason = DetachedSpatialMigrationPreparer.DuplicatePlacementRevisionReason; return false; }
                if (!MvpDungeonPlacementIds.TryGetCategoryForOption(tied[0].OptionId, out string optionCategory) ||
                    optionCategory != group.Key)
                {
                    reason = "gd66.content.category_mismatch"; return false;
                }
                selected[group.Key] = tied[0].OptionId;
            }
            rooms = FromCategories(selected); return ValidateOptions(rooms, out reason);
        }

        private static bool TryLowerPlacementEvidence(RawSavePayloadClassification source,
            SpatialSerializedInputLimits limits, out LowerPlacementEvidence evidence, out string reason)
        {
            evidence = null; reason = null;
            MvpDungeonPlacementState data = RawLegacyRouteContracts.ParsePlacements(source,
                "mvpDungeonPlacements", limits, out string parseReason);
            if (data == null) { reason = parseReason; return false; }
            if (data?.Entries == null) { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return false; }
            var selected = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (IGrouping<string, MvpDungeonPlacementEntry> group in data.Entries.Where(value => value != null)
                .GroupBy(value => value.CategoryId, StringComparer.Ordinal))
            {
                bool roomCategory = string.Equals(group.Key, MvpDungeonPlacementIds.RoomCategoryId,
                    StringComparison.Ordinal);
                if (!MvpDungeonPlacementIds.IsAllowedCategory(group.Key) || group.Any(value => value.Revision < 0))
                { reason = "gd66.route.record_out_of_range"; return false; }
                int greatest = group.Max(value => value.Revision);
                MvpDungeonPlacementEntry[] tied = group.Where(value => value.Revision == greatest).ToArray();
                if (tied.Length != 1) { reason = DetachedSpatialMigrationPreparer.DuplicatePlacementRevisionReason; return false; }
                if (!MvpDungeonPlacementIds.TryGetCategoryForOption(tied[0].OptionId, out string optionCategory) ||
                    optionCategory != group.Key)
                {
                    reason = roomCategory ? DetachedSpatialMigrationPreparer.OutcomeMismatchReason :
                        "gd66.content.category_mismatch";
                    return false;
                }
                if (roomCategory && tied[0].OptionId != MvpDungeonPlacementIds.BasicRoomOptionId)
                { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return false; }
                selected[group.Key] = tied[0].OptionId;
            }
            evidence = new LowerPlacementEvidence(
                selected.TryGetValue(MvpDungeonPlacementIds.RoomCategoryId, out string room),
                room,
                Value(selected, MvpDungeonPlacementIds.MonsterCategoryId),
                Value(selected, MvpDungeonPlacementIds.TrapCategoryId),
                Value(selected, MvpDungeonPlacementIds.LootNodeCategoryId));
            return true;
        }

        private static RouteRoom[] FromCategories(IDictionary<string, string> values)
        {
            bool explicitRoom = values.TryGetValue(MvpDungeonPlacementIds.RoomCategoryId, out string room);
            var monsters = Value(values, MvpDungeonPlacementIds.MonsterCategoryId);
            var traps = Value(values, MvpDungeonPlacementIds.TrapCategoryId);
            var loot = Value(values, MvpDungeonPlacementIds.LootNodeCategoryId);
            if (!explicitRoom && monsters.Length + traps.Length + loot.Length == 0) return Array.Empty<RouteRoom>();
            return new[] { new RouteRoom(0, explicitRoom ? room : MvpDungeonPlacementIds.BasicRoomOptionId,
                explicitRoom, monsters, traps, loot) };
        }

        private static string[] Value(IDictionary<string, string> values, string category) =>
            values.TryGetValue(category, out string value) ? new[] { value } : Array.Empty<string>();

        private static bool MergeEffectivePlacements(RawSavePayloadClassification source, SpatialSerializedInputLimits limits, ref RouteRoom[] rooms,
            List<string> diagnostics, out string reason)
        {
            reason = null;
            if (source.DungeonPlacementsPresence != RawLegacyRoutePresence.Present) return true;
            if (!TryLowerPlacementEvidence(source, limits, out LowerPlacementEvidence lower, out reason)) return false;
            if (!lower.HasRoom && !lower.HasContent) return true;
            if (lower.HasRoom && !LowerRoomAgreesWithWinner(lower, rooms))
            { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return false; }
            if (rooms.Length == 0)
            {
                rooms = new[] { lower.ToContribution() };
                diagnostics.Add(EffectiveContentDiagnostic);
                return true;
            }
            RouteRoom winner = rooms[0], contribution = lower.ToContribution();
            bool contributed = (winner.Monsters.Length == 0 && contribution.Monsters.Length != 0) ||
                (winner.Traps.Length == 0 && contribution.Traps.Length != 0) ||
                (winner.Loot.Length == 0 && contribution.Loot.Length != 0);
            if (!Merge(ref winner.Monsters, contribution.Monsters) || !Merge(ref winner.Traps, contribution.Traps) ||
                !Merge(ref winner.Loot, contribution.Loot))
            { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return false; }
            rooms[0] = winner;
            diagnostics.Add(contributed ? EffectiveContentDiagnostic : AgreementDiagnostic); return true;
        }

        private static bool LowerRoomAgreesWithWinner(LowerPlacementEvidence lower, RouteRoom[] rooms)
        {
            return lower.HasRoom && rooms != null && rooms.Length == 1 && rooms[0].Explicit &&
                rooms[0].RoomOption == MvpDungeonPlacementIds.BasicRoomOptionId &&
                lower.RoomOption == MvpDungeonPlacementIds.BasicRoomOptionId;
        }

        private static bool Merge(ref string[] winner, string[] lower)
        {
            if (lower.Length == 0) return true;
            if (winner.Length == 0) { winner = lower; return true; }
            return winner.SequenceEqual(lower, StringComparer.Ordinal);
        }

        private static void CompareLower(RawSavePayloadClassification source,
            SpatialSerializedInputLimits limits, RouteRoom[] winner, List<string> diagnostics)
        {
            if (source.FloorLayoutPresence == RawLegacyRoutePresence.Absent &&
                source.DungeonPlacementsPresence == RawLegacyRoutePresence.Absent) return;
            RouteRoom[] lower; string reason;
            bool parsed = source.FloorLayoutPresence == RawLegacyRoutePresence.Present
                ? TryFloor(source, limits, out lower, out reason)
                : TryPlacements(source, limits, out lower, out reason);
            if (parsed && source.FloorLayoutPresence == RawLegacyRoutePresence.Present)
                parsed = MergeEffectivePlacements(source, limits, ref lower, new List<string>(), out reason);
            diagnostics.Add(parsed && Equivalent(winner, lower) ? AgreementDiagnostic : IneffectiveDiagnostic);
        }

        private static bool Equivalent(RouteRoom[] left, RouteRoom[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int index = 0; index < left.Length; index++)
                if (left[index].Index != right[index].Index || left[index].Explicit != right[index].Explicit ||
                    left[index].RoomOption != right[index].RoomOption ||
                    !left[index].Monsters.SequenceEqual(right[index].Monsters, StringComparer.Ordinal) ||
                    !left[index].Traps.SequenceEqual(right[index].Traps, StringComparer.Ordinal) ||
                    !left[index].Loot.SequenceEqual(right[index].Loot, StringComparer.Ordinal)) return false;
            return true;
        }

        private static bool ValidateOptions(RouteRoom[] rooms, out string reason)
        {
            reason = null;
            foreach (RouteRoom room in rooms)
            {
                if (room.RoomOption == MvpDungeonPlacementIds.NarrowHallOptionId)
                { reason = DetachedSpatialMigrationPreparer.NarrowHallReason; return false; }
                if (room.RoomOption != MvpDungeonPlacementIds.BasicRoomOptionId ||
                    !Options(room.Monsters, MvpDungeonPlacementIds.MonsterCategoryId) ||
                    !Options(room.Traps, MvpDungeonPlacementIds.TrapCategoryId) ||
                    !Options(room.Loot, MvpDungeonPlacementIds.LootNodeCategoryId))
                { reason = DetachedSpatialMigrationPreparer.InvalidOptionReason; return false; }
            }
            return true;
        }

        private static bool Options(IEnumerable<string> values, string category) => values != null && values.All(value =>
            MvpDungeonPlacementIds.TryGetCategoryForOption(value, out string actual) && actual == category);

        private static FrozenLegacyRouteProjectionResult BuildFloor(DetachedSpatialMigrationPreparationInputs inputs,
            RouteRoom[] route, List<string> diagnostics)
        {
            SpatialContentCatalog catalog = inputs.ProductionContent.Catalog;
            FloorSpatialConfiguration floor = catalog.Floors.SingleOrDefault(value => value != null &&
                value.FloorDefinitionId == inputs.Geometry.FloorDefinitionId && value.FloorIndex == 0);
            RoomSpatialDefinition roomDefinition = catalog.Rooms.SingleOrDefault(value => value != null &&
                value.RoomDefinitionId == inputs.Geometry.BasicRoomDefinitionId);
            FixedSpatialStructureDefinition entrance = catalog.FixedStructures.SingleOrDefault(value => value != null &&
                value.StructureDefinitionId == inputs.Geometry.EntranceStructureDefinitionId);
            FixedSpatialStructureDefinition completion = catalog.FixedStructures.SingleOrDefault(value => value != null &&
                value.StructureDefinitionId == inputs.Geometry.CompletionStructureDefinitionId);
            CompatibilityLayoutVariant variant = (inputs.Geometry.Layouts ?? Array.Empty<CompatibilityLayoutVariant>()).SingleOrDefault(value => value != null &&
                value.Placements.Count(placement => placement != null &&
                    (placement.Role == CompatibilityRouteRole.BasicRoom0 || placement.Role == CompatibilityRouteRole.BasicRoom1)) == route.Length);
            if (floor == null || roomDefinition == null || entrance == null || completion == null || variant == null)
                return Failure(DetachedSpatialMigrationPreparer.ProfileInvalidReason);
            var configuredOptions = new HashSet<string>((inputs.LegacyGameplayConfiguration.MvpPlacementEffects ??
                Array.Empty<MvpPlacementEffectConfig>()).Where(value => value != null)
                .Select(value => value.OptionId), StringComparer.Ordinal);
            foreach (RouteRoom room in route)
            {
                if (room.Monsters.Concat(room.Traps).Concat(room.Loot).Any(value => !configuredOptions.Contains(value)))
                    return Failure(DetachedSpatialMigrationPreparer.InvalidOptionReason);
                if (room.Monsters.Length > roomDefinition.MonsterCapacity || room.Traps.Length > roomDefinition.TrapCapacity ||
                    room.Loot.Length > roomDefinition.LootCapacity)
                    return Failure(DetachedSpatialMigrationPreparer.CapacityReason);
            }
            const string floorId = "compat.floor.00";
            var rooms = new List<RoomSpatialInstance>(); var nodes = new List<FloorRouteNode>();
            var fixedStructures = new List<SavedFixedSpatialStructure>();
            foreach (CompatibilityLayoutPlacement placement in variant.Placements)
            {
                if (placement.Role == CompatibilityRouteRole.Entrance || placement.Role == CompatibilityRouteRole.Completion)
                {
                    bool isEntrance = placement.Role == CompatibilityRouteRole.Entrance;
                    string role = isEntrance ? "entrance" : "completion";
                    fixedStructures.Add(new SavedFixedSpatialStructure { FixedStructureInstanceId = floorId + ".fixed." + role,
                        FixedStructureDefinitionId = isEntrance ? entrance.StructureDefinitionId : completion.StructureDefinitionId,
                        FloorInstanceId = floorId, Anchor = placement.Anchor, Orientation = placement.Orientation,
                        Kind = isEntrance ? FixedSpatialStructureKind.Entrance : FixedSpatialStructureKind.CompletionTerminal });
                    nodes.Add(new FloorRouteNode { NodeId = floorId + ".node." + role, FloorId = floorId,
                        Kind = isEntrance ? FloorRouteNodeKind.Entrance : FloorRouteNodeKind.Completion, RoomInstanceId = null });
                }
                else
                {
                    int index = placement.Role == CompatibilityRouteRole.BasicRoom0 ? 0 : 1;
                    string roomId = floorId + ".legacy-room." + index.ToString("D2", CultureInfo.InvariantCulture);
                    rooms.Add(new RoomSpatialInstance { RoomInstanceId = roomId, RoomDefinitionId = roomDefinition.RoomDefinitionId,
                        FloorId = floorId, Anchor = placement.Anchor, Orientation = placement.Orientation });
                    nodes.Add(new FloorRouteNode { NodeId = floorId + ".node.legacy-room." + index.ToString("D2", CultureInfo.InvariantCulture),
                        FloorId = floorId, Kind = FloorRouteNodeKind.Room, RoomInstanceId = roomId });
                }
            }
            var edges = variant.Connections.Select(connection => new FloorRouteEdge
            {
                EdgeId = floorId + ".edge.direct." + Role(connection.SourceRole) + "." + Role(connection.DestinationRole),
                CorridorDefinitionId = string.Empty, FloorId = floorId,
                SourceNodeId = NodeId(floorId, connection.SourceRole), DestinationNodeId = NodeId(floorId, connection.DestinationRole),
                Footprint = null, Classification = RouteClassification.Required, OptionalBranchId = string.Empty,
                ConnectionKind = FloorRouteConnectionKind.DirectDoorway
            }).ToArray();
            var assignments = new List<RoomContentAssignment>(); var semantics = new List<CanonicalRoomSemantics>(); long sequence = 0;
            foreach (RouteRoom source in route)
            {
                string roomId = floorId + ".legacy-room." + source.Index.ToString("D2", CultureInfo.InvariantCulture);
                Add(assignments, roomId, "monster", MvpDungeonPlacementIds.MonsterCategoryId, source.Monsters, ref sequence);
                Add(assignments, roomId, "trap", MvpDungeonPlacementIds.TrapCategoryId, source.Traps, ref sequence);
                Add(assignments, roomId, "loot", MvpDungeonPlacementIds.LootNodeCategoryId, source.Loot, ref sequence);
                semantics.Add(new CanonicalRoomSemantics { RoomInstanceId = roomId,
                    LegacyRoomOriginKind = source.Explicit ? LegacyRoomOriginKind.MigratedExplicitLegacyRoom :
                        LegacyRoomOriginKind.ImplicitCompatibilityContainer });
            }
            var saved = new SavedSpatialFloor { FloorInstanceId = floorId, FloorDefinitionId = floor.FloorDefinitionId,
                FloorIndex = 0, Layout = new FloorSpatialLayout { FloorId = floorId, Rooms = rooms.ToArray(),
                    Nodes = nodes.ToArray(), Edges = edges }, FixedStructures = fixedStructures.ToArray(),
                RoomContents = new FloorRoomContentState { Assignments = assignments.ToArray(),
                    RoomSemantics = semantics.ToArray(), NextSequence = sequence } };
            return new FrozenLegacyRouteProjectionResult(new[] { saved }, null, diagnostics);
        }

        private static void Add(List<RoomContentAssignment> target, string roomId, string shortCategory,
            string category, IEnumerable<string> values, ref long sequence)
        {
            foreach (string value in values)
            {
                long current = sequence++;
                target.Add(new RoomContentAssignment { AssignmentId = roomId + ".content." + shortCategory + "." +
                    current.ToString("D4", CultureInfo.InvariantCulture), RoomInstanceId = roomId,
                    CategoryId = category, OptionId = value, Sequence = current });
            }
        }

        private static string Role(CompatibilityRouteRole role) => role == CompatibilityRouteRole.Entrance ? "entrance" :
            role == CompatibilityRouteRole.Completion ? "completion" : role == CompatibilityRouteRole.BasicRoom0 ?
            "legacy-room.00" : "legacy-room.01";
        private static string NodeId(string floorId, CompatibilityRouteRole role) => floorId + ".node." + Role(role);
        private static FrozenLegacyRouteProjectionResult Failure(string reason) =>
            new FrozenLegacyRouteProjectionResult(null, reason, null);

        private struct RouteRoom
        {
            internal RouteRoom(int index, string roomOption, bool explicitRoom, string[] monsters, string[] traps, string[] loot)
            { Index = index; RoomOption = roomOption; Explicit = explicitRoom; Monsters = monsters ?? Array.Empty<string>();
              Traps = traps ?? Array.Empty<string>(); Loot = loot ?? Array.Empty<string>(); }
            internal int Index; internal string RoomOption; internal bool Explicit;
            internal string[] Monsters; internal string[] Traps; internal string[] Loot;
        }

        private sealed class LowerPlacementEvidence
        {
            internal LowerPlacementEvidence(bool hasRoom, string roomOption, string[] monsters,
                string[] traps, string[] loot)
            { HasRoom = hasRoom; RoomOption = roomOption; Monsters = monsters ?? Array.Empty<string>();
              Traps = traps ?? Array.Empty<string>(); Loot = loot ?? Array.Empty<string>(); }
            internal bool HasRoom { get; }
            internal string RoomOption { get; }
            internal string[] Monsters { get; }
            internal string[] Traps { get; }
            internal string[] Loot { get; }
            internal bool HasContent => Monsters.Length + Traps.Length + Loot.Length != 0;
            internal RouteRoom ToContribution() =>
                new RouteRoom(0, MvpDungeonPlacementIds.BasicRoomOptionId, false, Monsters, Traps, Loot);
        }
    }

    internal static class RawLegacyRouteContracts
    {
        private static readonly string[] AssignmentRoot = { "Rooms", "NextRevision" };
        private static readonly string[] AssignmentFields = { "FloorIndex", "RoomIndex", "RoomOptionId", "MonsterOptionIds", "TrapOptionIds", "LootNodeOptionIds" };
        private static readonly string[] FloorRoot = { "Nodes", "NextRevision" };
        private static readonly string[] FloorFields = { "FloorIndex", "NodeIndex", "SlotId", "CategoryId", "OptionId", "Revision" };
        private static readonly string[] PlacementRoot = { "Entries", "NextRevision" };
        private static readonly string[] PlacementFields = { "CategoryId", "OptionId", "Revision" };

        internal static MvpRoomSlotAssignmentCollection ParseAssignments(RawSavePayloadClassification source,
            string member, SpatialSerializedInputLimits limits, out string reason)
        {
            reason = null; ContractJsonNode root = Parse(source, member, limits, out reason);
            if (!Shape(root, AssignmentRoot, limits, out reason) || !Integer(root.Fields[1].Value, true)) return null;
            if (root.Fields[0].Value.Kind != ContractJsonKind.Array) { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return null; }
            var result = new MvpRoomSlotAssignmentCollection { Rooms = new List<MvpRoomSlotAssignmentState>() };
            foreach (ContractJsonNode item in root.Fields[0].Value.Items)
            {
                if (!Shape(item, AssignmentFields, limits, out reason)) return null;
                if (!Int(item.Fields[0].Value, out int floor) || !Int(item.Fields[1].Value, out int room) ||
                    !String(item.Fields[2].Value, out string roomOption) ||
                    !Strings(item.Fields[3].Value, out string[] monsters) ||
                    !Strings(item.Fields[4].Value, out string[] traps) ||
                    !Strings(item.Fields[5].Value, out string[] loot))
                { reason = "gd66.route.record_out_of_range"; return null; }
                result.Rooms.Add(new MvpRoomSlotAssignmentState { FloorIndex = floor, RoomIndex = room,
                    RoomOptionId = roomOption, MonsterOptionIds = monsters, TrapOptionIds = traps,
                    LootNodeOptionIds = loot });
            }
            Int(root.Fields[1].Value, out int next); result.NextRevision = next; return result;
        }

        internal static MvpDungeonFloorLayoutState ParseFloor(RawSavePayloadClassification source,
            string member, SpatialSerializedInputLimits limits, out string reason)
        {
            reason = null; ContractJsonNode root = Parse(source, member, limits, out reason);
            if (!Shape(root, FloorRoot, limits, out reason) || !Integer(root.Fields[1].Value, true)) return null;
            if (root.Fields[0].Value.Kind != ContractJsonKind.Array) { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return null; }
            var result = new MvpDungeonFloorLayoutState { Nodes = new List<MvpDungeonNodeState>() };
            foreach (ContractJsonNode item in root.Fields[0].Value.Items)
            {
                if (!Shape(item, FloorFields, limits, out reason)) return null;
                if (!Int(item.Fields[0].Value, out int floor) || !Int(item.Fields[1].Value, out int node) ||
                    !String(item.Fields[2].Value, out string slot) || !String(item.Fields[3].Value, out string category) ||
                    !String(item.Fields[4].Value, out string option) || !Int(item.Fields[5].Value, out int revision))
                { reason = "gd66.route.record_out_of_range"; return null; }
                result.Nodes.Add(new MvpDungeonNodeState(floor, node, slot, category, option, revision));
            }
            Int(root.Fields[1].Value, out int next); result.NextRevision = next; return result;
        }

        internal static MvpDungeonPlacementState ParsePlacements(RawSavePayloadClassification source,
            string member, SpatialSerializedInputLimits limits, out string reason)
        {
            reason = null; ContractJsonNode root = Parse(source, member, limits, out reason);
            if (!Shape(root, PlacementRoot, limits, out reason) || !Integer(root.Fields[1].Value, true)) return null;
            if (root.Fields[0].Value.Kind != ContractJsonKind.Array) { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return null; }
            var result = new MvpDungeonPlacementState { Entries = new List<MvpDungeonPlacementEntry>() };
            foreach (ContractJsonNode item in root.Fields[0].Value.Items)
            {
                if (!Shape(item, PlacementFields, limits, out reason)) return null;
                if (!String(item.Fields[0].Value, out string category) || !String(item.Fields[1].Value, out string option) ||
                    !Int(item.Fields[2].Value, out int revision))
                { reason = "gd66.route.record_out_of_range"; return null; }
                result.Entries.Add(new MvpDungeonPlacementEntry(category, option, revision));
            }
            Int(root.Fields[1].Value, out int next); result.NextRevision = next; return result;
        }

        private static ContractJsonNode Parse(RawSavePayloadClassification source, string member,
            SpatialSerializedInputLimits limits, out string reason)
        {
            reason = null; RawSaveMemberEvidence evidence = source.Members.FirstOrDefault(value => value.Name == member);
            if (evidence == null || evidence.State != RawSaveMemberState.NonNull)
            { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return null; }
            var issues = new SpatialIssueCollector(limits.MaximumDiagnostics);
            if (!ContractJson.TryParse(evidence.GetRawValueBytes(), limits, issues, out ContractJsonNode root))
            { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return null; }
            return root;
        }

        private static bool Shape(ContractJsonNode node, string[] fields, SpatialSerializedInputLimits limits,
            out string reason)
        {
            reason = null; var issues = new SpatialIssueCollector(limits.MaximumDiagnostics);
            if (node == null || !ContractJson.ValidateShape(node, fields, issues))
            { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return false; }
            return true;
        }
        private static bool Integer(ContractJsonNode node, bool nonnegative) =>
            Int(node, out int value) && (!nonnegative || value >= 0);
        private static bool Int(ContractJsonNode node, out int value) => ContractJson.Int(node, out value);
        private static bool String(ContractJsonNode node, out string value) => ContractJson.String(node, out value);
        private static bool Strings(ContractJsonNode node, out string[] values)
        {
            values = null; if (node.Kind != ContractJsonKind.Array) return false;
            var result = new List<string>();
            foreach (ContractJsonNode item in node.Items)
            { if (!String(item, out string value)) return false; result.Add(value); }
            values = result.ToArray(); return true;
        }
    }

}
