using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class DetachedSpatialMigrationPreparationInputs
    {
        public DetachedSpatialMigrationPreparationInputs(byte[] exactOriginalBytes,
            RawSavePayloadClassification classification, SpatialMigrationInputDescriptor descriptorInputs,
            SpatialMigrationCompatibilityProfile profile, CompatibilityLayoutGeometryRecord geometry,
            ProductionSpatialContentSnapshot productionContent, RunSimulationConfig legacyGameplayConfiguration,
            CanonicalSpatialSerializationLimits spatialLimits, DetachedWholeSaveLimits wholeSaveLimits)
        {
            ExactOriginalBytes = exactOriginalBytes == null ? null : (byte[])exactOriginalBytes.Clone();
            Classification = classification; DescriptorInputs = descriptorInputs; Profile = profile;
            Geometry = geometry; ProductionContent = productionContent;
            LegacyGameplayConfiguration = legacyGameplayConfiguration;
            SpatialLimits = spatialLimits; WholeSaveLimits = wholeSaveLimits;
        }
        public byte[] ExactOriginalBytes { get; }
        public RawSavePayloadClassification Classification { get; }
        public SpatialMigrationInputDescriptor DescriptorInputs { get; }
        public SpatialMigrationCompatibilityProfile Profile { get; }
        public CompatibilityLayoutGeometryRecord Geometry { get; }
        public ProductionSpatialContentSnapshot ProductionContent { get; }
        public RunSimulationConfig LegacyGameplayConfiguration { get; }
        public CanonicalSpatialSerializationLimits SpatialLimits { get; }
        public DetachedWholeSaveLimits WholeSaveLimits { get; }
    }

    public sealed class DetachedPreparedSpatialMigrationAttempt
    {
        private readonly byte[] original;
        internal DetachedPreparedSpatialMigrationAttempt(byte[] source, SpatialMigrationInputDescriptor descriptor,
            string fingerprint, string identity, string transactionId, DetachedWholeSaveCandidate candidate,
            CanonicalSpatialAuthorityMarker marker, IEnumerable<string> diagnostics)
        {
            original = (byte[])source.Clone(); Descriptor = descriptor; DescriptorFingerprint = fingerprint;
            TransactionIdentity = identity; TransactionId = transactionId; Candidate = candidate; Marker = marker;
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
            if (inputs == null || inputs.ExactOriginalBytes == null || inputs.Classification == null ||
                !inputs.Classification.IsSuccess || inputs.DescriptorInputs == null || inputs.Profile == null ||
                inputs.Geometry == null || inputs.ProductionContent == null ||
                inputs.LegacyGameplayConfiguration == null || !inputs.SpatialLimits.IsValid ||
                !inputs.WholeSaveLimits.IsValid) return Failure(InvalidInputReason);
            try
            {
                string originalHash = SpatialContractSha256.Compute(inputs.ExactOriginalBytes);
                SpatialMigrationInputDescriptor descriptor = Descriptor(originalHash, inputs);
                SpatialContractResult<byte[]> descriptorBytes = SpatialMigrationDescriptorContracts.Serialize(
                    descriptor, inputs.SpatialLimits.Serialized);
                if (!descriptorBytes.IsValid || !PinsMatch(inputs, descriptor)) return Failure(InvalidInputReason);
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
                return new DetachedSpatialMigrationPreparationResult(
                    new DetachedPreparedSpatialMigrationAttempt(inputs.ExactOriginalBytes, descriptor, fingerprint,
                        identity, transactionId, candidate.Candidate, marker, projection.Diagnostics), null,
                    projection.Diagnostics);
            }
            catch (ArgumentException) { return Failure(InvalidInputReason); }
            catch (InvalidOperationException) { return Failure(ProfileInvalidReason); }
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
            SpatialMigrationInputDescriptor descriptor)
        {
            byte[] manifest = ProductionSpatialGeneratedSetParser.SerializeCanonical(inputs.ProductionContent.Manifest);
            byte[] catalog = ProductionSpatialGeneratedSetParser.SerializeCanonical(inputs.ProductionContent.Catalog);
            byte[] gameplay = Encoding.UTF8.GetBytes(JsonUtility.ToJson(inputs.LegacyGameplayConfiguration));
            return descriptor.SelectedTargetSchemaVersion == DetachedWholeSaveCandidateSerializer.TargetSchemaVersion &&
                descriptor.AuthorityMarkerContractVersion == SpatialMigrationContractIdentity.AuthorityMarkerContractVersion &&
                descriptor.MigrationContractVersion == SpatialMigrationContractIdentity.MigrationContractVersion &&
                string.Equals(descriptor.ProductionManifestSha256, SpatialContractSha256.Compute(manifest), StringComparison.Ordinal) &&
                string.Equals(descriptor.ProductionCatalogSha256, SpatialContractSha256.Compute(catalog), StringComparison.Ordinal) &&
                string.Equals(descriptor.LegacyGameplayConfigurationSha256, SpatialContractSha256.Compute(gameplay), StringComparison.Ordinal) &&
                inputs.Profile.TargetSchemaVersion == descriptor.SelectedTargetSchemaVersion &&
                inputs.Profile.GeometryId == inputs.Geometry.GeometryId &&
                inputs.Profile.GeometryVersion == inputs.Geometry.GeometryVersion &&
                inputs.Profile.GeometryCanonicalHash == inputs.Geometry.CanonicalHash;
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

        internal static FrozenLegacyRouteProjectionResult Project(DetachedSpatialMigrationPreparationInputs inputs)
        {
            var diagnostics = new List<string>();
            RawSavePayloadClassification source = inputs.Classification;
            RouteRoom[] rooms;
            if (source.RoomSlotAssignmentsPresence == RawLegacyRoutePresence.Present)
            {
                if (!TryAssignments(source, out rooms, out string reason)) return Failure(reason);
                CompareLower(source, rooms, diagnostics, false);
            }
            else if (source.FloorLayoutPresence == RawLegacyRoutePresence.Present)
            {
                if (!TryFloor(source, out rooms, out string reason)) return Failure(reason);
                if (!MergeEffectivePlacements(source, rooms, diagnostics, out reason)) return Failure(reason);
            }
            else if (source.DungeonPlacementsPresence == RawLegacyRoutePresence.Present)
            {
                if (!TryPlacements(source, out rooms, out string reason)) return Failure(reason);
            }
            else return new FrozenLegacyRouteProjectionResult(Array.Empty<SavedSpatialFloor>(), null, diagnostics);
            if (rooms.Length == 0) return new FrozenLegacyRouteProjectionResult(Array.Empty<SavedSpatialFloor>(), null, diagnostics);
            return BuildFloor(inputs, rooms, diagnostics);
        }

        private static bool TryAssignments(RawSavePayloadClassification source, out RouteRoom[] rooms,
            out string reason)
        {
            rooms = null; reason = null;
            MvpRoomSlotAssignmentCollection data = Parse<MvpRoomSlotAssignmentCollection>(source, "mvpRoomSlotAssignments");
            if (data?.Rooms == null) { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return false; }
            var keys = new HashSet<string>(StringComparer.Ordinal); var result = new List<RouteRoom>();
            foreach (MvpRoomSlotAssignmentState room in data.Rooms)
            {
                if (room == null || room.FloorIndex != 0 || room.RoomIndex < 0 ||
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

        private static bool TryFloor(RawSavePayloadClassification source, out RouteRoom[] rooms, out string reason)
        {
            rooms = null; reason = null;
            MvpDungeonFloorLayoutState data = Parse<MvpDungeonFloorLayoutState>(source, "mvpDungeonFloorLayout");
            if (data?.Nodes == null) { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return false; }
            var selected = new Dictionary<string, MvpDungeonNodeState>(StringComparer.Ordinal);
            foreach (IGrouping<string, MvpDungeonNodeState> group in data.Nodes.Where(node => node != null &&
                node.FloorIndex == 0 && !string.IsNullOrEmpty(node.CategoryId)).GroupBy(node => node.CategoryId, StringComparer.Ordinal))
            {
                int greatest = group.Max(node => node.Revision); MvpDungeonNodeState[] tied = group.Where(node => node.Revision == greatest).ToArray();
                if (tied.Length != 1) { reason = DetachedSpatialMigrationPreparer.DuplicateFloorRevisionReason; return false; }
                selected[group.Key] = tied[0];
            }
            rooms = FromCategories(selected.ToDictionary(pair => pair.Key,
                pair => pair.Value.OptionId, StringComparer.Ordinal));
            return ValidateOptions(rooms, out reason);
        }

        private static bool TryPlacements(RawSavePayloadClassification source, out RouteRoom[] rooms, out string reason)
        {
            rooms = null; reason = null;
            MvpDungeonPlacementState data = Parse<MvpDungeonPlacementState>(source, "mvpDungeonPlacements");
            if (data?.Entries == null) { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return false; }
            var selected = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (IGrouping<string, MvpDungeonPlacementEntry> group in data.Entries.Where(value => value != null)
                .GroupBy(value => value.CategoryId, StringComparer.Ordinal))
            {
                int greatest = group.Max(value => value.Revision); MvpDungeonPlacementEntry[] tied = group.Where(value => value.Revision == greatest).ToArray();
                if (tied.Length != 1) { reason = DetachedSpatialMigrationPreparer.DuplicatePlacementRevisionReason; return false; }
                selected[group.Key] = tied[0].OptionId;
            }
            rooms = FromCategories(selected); return ValidateOptions(rooms, out reason);
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

        private static bool MergeEffectivePlacements(RawSavePayloadClassification source, RouteRoom[] rooms,
            List<string> diagnostics, out string reason)
        {
            reason = null;
            if (source.DungeonPlacementsPresence != RawLegacyRoutePresence.Present || rooms.Length == 0) return true;
            if (!TryPlacements(source, out RouteRoom[] lower, out reason)) return false;
            if (lower.Length == 0) return true;
            RouteRoom winner = rooms[0], contribution = lower[0];
            if (!Merge(ref winner.Monsters, contribution.Monsters) || !Merge(ref winner.Traps, contribution.Traps) ||
                !Merge(ref winner.Loot, contribution.Loot))
            { reason = DetachedSpatialMigrationPreparer.OutcomeMismatchReason; return false; }
            rooms[0] = winner; diagnostics.Add(EffectiveContentDiagnostic); return true;
        }

        private static bool Merge(ref string[] winner, string[] lower)
        {
            if (lower.Length == 0) return true;
            if (winner.Length == 0) { winner = lower; return true; }
            return winner.SequenceEqual(lower, StringComparer.Ordinal);
        }

        private static void CompareLower(RawSavePayloadClassification source, RouteRoom[] rooms,
            List<string> diagnostics, bool effective)
        {
            if (source.FloorLayoutPresence == RawLegacyRoutePresence.Absent &&
                source.DungeonPlacementsPresence == RawLegacyRoutePresence.Absent) return;
            diagnostics.Add(effective ? EffectiveContentDiagnostic : IneffectiveDiagnostic);
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
        private static T Parse<T>(RawSavePayloadClassification source, string name) where T : class
        {
            RawSaveMemberEvidence evidence = source.Members.FirstOrDefault(value => value.Name == name);
            return evidence == null || evidence.State != RawSaveMemberState.NonNull ? null :
                JsonUtility.FromJson<T>(Encoding.UTF8.GetString(evidence.GetRawValueBytes()));
        }
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
    }
}
