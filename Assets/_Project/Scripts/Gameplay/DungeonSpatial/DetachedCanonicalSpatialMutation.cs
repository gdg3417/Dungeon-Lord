using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum DetachedCanonicalMutationKind
    {
        PlaceOrReplace = 1,
        RemoveRoom = 2,
        StructuralConstruction = 3
    }

    public sealed class DetachedCanonicalMutationRequest
    {
        public DetachedCanonicalMutationKind Kind { get; private set; }
        public string CategoryId { get; private set; }
        public string OptionId { get; private set; }
        public string RoomInstanceId { get; private set; }
        internal StructuralConstructionRequest StructuralIntent { get; private set; }
        internal string StructuralBaselineFingerprint { get; private set; }

        public static DetachedCanonicalMutationRequest Place(string categoryId, string optionId,
            string roomInstanceId = null) => new DetachedCanonicalMutationRequest
            { Kind = DetachedCanonicalMutationKind.PlaceOrReplace, CategoryId = categoryId,
              OptionId = optionId, RoomInstanceId = roomInstanceId };

        public static DetachedCanonicalMutationRequest RemoveRoom(string roomInstanceId) =>
            new DetachedCanonicalMutationRequest
            { Kind = DetachedCanonicalMutationKind.RemoveRoom, RoomInstanceId = roomInstanceId };

        public static DetachedCanonicalMutationRequest Construct(StructuralEditPreview preview) =>
            new DetachedCanonicalMutationRequest
            {
                Kind = DetachedCanonicalMutationKind.StructuralConstruction,
                StructuralIntent = preview != null && preview.IsValid ? preview.Intent : null,
                StructuralBaselineFingerprint = preview != null && preview.IsValid ? preview.BaselineFingerprint : null
            };
    }

    public sealed class DetachedCanonicalMutationResult
    {
        internal DetachedCanonicalMutationResult(DetachedCanonicalSpatialSaveState state, string reason,
            bool roomEffect)
        { State = state; Reason = reason; ApplyExplicitRoomEffect = roomEffect; }
        public DetachedCanonicalSpatialSaveState State { get; }
        public string Reason { get; }
        public bool IsSuccess => State != null;
        public bool IsNoOp => Reason == DetachedCanonicalSpatialMutation.NoOpReason;
        public bool ApplyExplicitRoomEffect { get; }
    }

    public static class CanonicalRoomCapacityResolver
    {
        public const string InvalidProductionRoomReason = "gd66.write.first_write_validation_failed";

        public static bool TryResolve(ProductionSpatialContentSnapshot production,
            string roomDefinitionId, out MvpRoomSlotCapacity capacity, out string reason)
        {
            capacity = null; reason = InvalidProductionRoomReason;
            if (production == null || string.IsNullOrEmpty(roomDefinitionId)) return false;
            RoomSpatialDefinition[] matches = (production.Catalog.Rooms ?? Array.Empty<RoomSpatialDefinition>())
                .Where(value => value != null && string.Equals(value.RoomDefinitionId,
                    roomDefinitionId, StringComparison.Ordinal)).ToArray();
            if (matches.Length != 1 || matches[0].MonsterCapacity < 0 ||
                matches[0].TrapCapacity < 0 || matches[0].LootCapacity < 0) return false;
            RoomSpatialDefinition room = matches[0];
            capacity = new MvpRoomSlotCapacity
            {
                RoomOptionId = room.RoomDefinitionId,
                MonsterCapacity = room.MonsterCapacity,
                TrapCapacity = room.TrapCapacity,
                LootCapacity = room.LootCapacity
            };
            reason = null;
            return true;
        }
    }

    /// <summary>Pure, inactive mutation preparation over a detached validated canonical state.</summary>
    public static class DetachedCanonicalSpatialMutation
    {
        public const string UnsupportedRoomReason = "gd66.write.unsupported_room_selection";
        public const string RemovalHasContentsReason = "gd66.write.room_removal_has_contents";
        public const string CapacityReductionReason = "gd66.write.capacity_reduction_invalid";
        public const string NoOpReason = "gd66.diagnostic.canonical_write_noop";
        public const string ValidationFailedReason = "gd66.write.first_write_validation_failed";

        public static DetachedCanonicalMutationResult Prepare(DetachedCanonicalSpatialSaveState current,
            DetachedCanonicalMutationRequest request, ProductionSpatialContentSnapshot production,
            SpatialLayoutCompatibilitySnapshot compatibility, RunSimulationConfig configuration,
            CanonicalSpatialSerializationLimits limits)
        {
            if (current?.Authority == null || current.Floors == null || request == null ||
                production == null || compatibility == null || configuration == null || !limits.IsValid)
                return Failure(ValidationFailedReason);
            if (!TryClone(current, limits, out DetachedCanonicalSpatialSaveState proposed))
                return Failure(ValidationFailedReason);
            bool roomEffect = false;
            string reason;
            if (request.Kind == DetachedCanonicalMutationKind.StructuralConstruction)
            {
                if (!StructuralEditService.TryFingerprint(current, limits, out string currentFingerprint) ||
                    request.StructuralIntent == null || !string.Equals(currentFingerprint,
                        request.StructuralBaselineFingerprint, StringComparison.Ordinal))
                    return Failure(StructuralEditService.StalePreviewReason);
                StructuralEditPreview refreshed = StructuralEditService.Preview(current,
                    request.StructuralIntent, production, compatibility, configuration, limits);
                if (!refreshed.IsValid) return Failure(refreshed.ReasonCodes.FirstOrDefault() ?? ValidationFailedReason);
                proposed = refreshed.DetachedCandidate;
                reason = null;
            }
            else if (request.Kind == DetachedCanonicalMutationKind.RemoveRoom)
                reason = Remove(proposed, request.RoomInstanceId);
            else if (string.Equals(request.CategoryId, MvpDungeonPlacementIds.RoomCategoryId,
                StringComparison.Ordinal))
                reason = PlaceRoom(proposed, request.OptionId, request.RoomInstanceId,
                    production, compatibility, ref roomEffect);
            else
                reason = PlaceContent(proposed, request.CategoryId, request.OptionId,
                    request.RoomInstanceId, production, compatibility);
            if (reason != null) return reason == NoOpReason
                ? new DetachedCanonicalMutationResult(null, reason, false) : Failure(reason);
            if (!CanonicalSpatialSaveContracts.TryCanonicalize(proposed, limits.Spatial,
                    out DetachedCanonicalSpatialSaveState canonical) ||
                !CanonicalSpatialSaveContracts.Validate(canonical, limits.Spatial, true).IsValid ||
                !DetachedCanonicalProductionSemanticValidation.Validate(canonical, production,
                    configuration, limits.Spatial).IsValid) return Failure(ValidationFailedReason);
            return new DetachedCanonicalMutationResult(canonical, null, roomEffect);
        }

        private static string PlaceRoom(DetachedCanonicalSpatialSaveState state, string optionId,
            string requestedRoomId,
            ProductionSpatialContentSnapshot production, SpatialLayoutCompatibilitySnapshot compatibility,
            ref bool roomEffect)
        {
            if (optionId == MvpDungeonPlacementIds.NarrowHallOptionId)
                return UnsupportedRoomReason;
            if (optionId != MvpDungeonPlacementIds.BasicRoomOptionId)
                return DetachedSpatialMigrationPreparer.InvalidOptionReason;
            if (state.Floors.Length == 0)
            {
                if (!string.IsNullOrEmpty(requestedRoomId)) return ValidationFailedReason;
                if (!TryCreateStarter(state, production, compatibility,
                    LegacyRoomOriginKind.CanonicalPlayerPlaced, out SavedSpatialFloor floor,
                    out string starterReason))
                    return starterReason;
                state.Floors = new[] { floor }; roomEffect = true; return null;
            }
            if (!TryTargetRoom(state, requestedRoomId, out SavedSpatialFloor existingFloor,
                out RoomSpatialInstance room, out CanonicalRoomSemantics semantics))
                return ValidationFailedReason;
            string basicDefinition = ResolveBasicDefinition(state, production, compatibility,
                out string definitionReason);
            if (basicDefinition == null) return definitionReason;
            if (room.RoomDefinitionId != basicDefinition)
            {
                if (!CanonicalRoomCapacityResolver.TryResolve(production, basicDefinition,
                    out MvpRoomSlotCapacity replacementCapacity, out string capacityReason))
                    return capacityReason;
                RoomContentAssignment[] retained = existingFloor.RoomContents.Assignments ??
                    Array.Empty<RoomContentAssignment>();
                if (retained.Count(value => value?.RoomInstanceId == room.RoomInstanceId &&
                        value.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId) >
                        replacementCapacity.MonsterCapacity ||
                    retained.Count(value => value?.RoomInstanceId == room.RoomInstanceId &&
                        value.CategoryId == MvpDungeonPlacementIds.TrapCategoryId) >
                        replacementCapacity.TrapCapacity ||
                    retained.Count(value => value?.RoomInstanceId == room.RoomInstanceId &&
                        value.CategoryId == MvpDungeonPlacementIds.LootNodeCategoryId) >
                        replacementCapacity.LootCapacity) return CapacityReductionReason;
                room.RoomDefinitionId = basicDefinition;
            }
            if (semantics.LegacyRoomOriginKind == LegacyRoomOriginKind.CanonicalPlayerPlaced)
                return NoOpReason;
            if (semantics.LegacyRoomOriginKind != LegacyRoomOriginKind.ImplicitCompatibilityContainer)
                return NoOpReason;
            semantics.LegacyRoomOriginKind = LegacyRoomOriginKind.CanonicalPlayerPlaced;
            roomEffect = true;
            return null;
        }

        private static string PlaceContent(DetachedCanonicalSpatialSaveState state, string categoryId,
            string optionId, string requestedRoomId, ProductionSpatialContentSnapshot production,
            SpatialLayoutCompatibilitySnapshot compatibility)
        {
            if (!MvpDungeonPlacementIds.TryGetCategoryForOption(optionId, out string actualCategory) ||
                actualCategory != categoryId || (categoryId != MvpDungeonPlacementIds.MonsterCategoryId &&
                categoryId != MvpDungeonPlacementIds.TrapCategoryId &&
                categoryId != MvpDungeonPlacementIds.LootNodeCategoryId))
                return DetachedSpatialMigrationPreparer.InvalidOptionReason;
            if (state.Floors.Length == 0)
            {
                if (!string.IsNullOrEmpty(requestedRoomId)) return ValidationFailedReason;
                if (!TryCreateStarter(state, production, compatibility,
                    LegacyRoomOriginKind.ImplicitCompatibilityContainer, out SavedSpatialFloor starter,
                    out string reason)) return reason;
                state.Floors = new[] { starter };
            }
            if (!TryTargetRoom(state, requestedRoomId, out SavedSpatialFloor floor,
                out RoomSpatialInstance room, out CanonicalRoomSemantics ignored))
                return ValidationFailedReason;
            RoomContentAssignment[] assignments = floor.RoomContents.Assignments ??
                Array.Empty<RoomContentAssignment>();
            RoomContentAssignment[] matching = assignments.Where(value => value != null &&
                value.RoomInstanceId == room.RoomInstanceId && value.CategoryId == categoryId).ToArray();
            if (matching.Any(value => value.OptionId == optionId)) return NoOpReason;
            if (!CanonicalRoomCapacityResolver.TryResolve(production, room.RoomDefinitionId,
                out MvpRoomSlotCapacity capacity, out string capacityReason)) return capacityReason;
            int maximum = categoryId == MvpDungeonPlacementIds.MonsterCategoryId ? capacity.MonsterCapacity :
                categoryId == MvpDungeonPlacementIds.TrapCategoryId ? capacity.TrapCapacity : capacity.LootCapacity;
            if (matching.Length >= maximum) return DetachedSpatialMigrationPreparer.CapacityReason;
            long sequence = floor.RoomContents.NextSequence;
            string shortCategory = categoryId == MvpDungeonPlacementIds.MonsterCategoryId ? "monster" :
                categoryId == MvpDungeonPlacementIds.TrapCategoryId ? "trap" : "loot";
            var added = new RoomContentAssignment
            {
                AssignmentId = room.RoomInstanceId + ".content." + shortCategory + "." +
                    sequence.ToString("D4", CultureInfo.InvariantCulture),
                RoomInstanceId = room.RoomInstanceId, CategoryId = categoryId,
                OptionId = optionId, Sequence = sequence
            };
            floor.RoomContents.Assignments = assignments.Concat(new[] { added }).ToArray();
            floor.RoomContents.NextSequence = sequence + 1;
            return null;
        }

        private static string Remove(DetachedCanonicalSpatialSaveState state, string requestedRoomId)
        {
            if (!TryTargetRoom(state, requestedRoomId, out SavedSpatialFloor floor,
                out RoomSpatialInstance room, out CanonicalRoomSemantics ignored)) return ValidationFailedReason;
            if ((floor.RoomContents.Assignments ?? Array.Empty<RoomContentAssignment>()).Any(value =>
                value != null && value.RoomInstanceId == room.RoomInstanceId)) return RemovalHasContentsReason;
            // Only the approved R1 -> canonical-empty transition is representable here. R2 removal
            // needs a future explicit topology rule; never infer an array-position mapping.
            if ((floor.Layout.Rooms ?? Array.Empty<RoomSpatialInstance>()).Length != 1)
                return ValidationFailedReason;
            state.Floors = Array.Empty<SavedSpatialFloor>(); return null;
        }

        private static bool TryCreateStarter(DetachedCanonicalSpatialSaveState state,
            ProductionSpatialContentSnapshot production, SpatialLayoutCompatibilitySnapshot compatibility,
            LegacyRoomOriginKind origin, out SavedSpatialFloor saved, out string reason)
        {
            saved = null; reason = ValidationFailedReason;
            CompatibilitySelectionResult<CanonicalStarterLayoutProfile> selected =
                compatibility.SelectStarter(DetachedWholeSaveCandidateSerializer.TargetSchemaVersion,
                    state.Authority.CanonicalLayoutContractVersion);
            if (!selected.Success) { reason = selected.Code; return false; }
            CanonicalStarterLayoutProfile profile = selected.Value;
            if (profile.CanonicalLayoutContractVersion != state.Authority.CanonicalLayoutContractVersion)
            { reason = "gd66.starter_profile.marker_mismatch"; return false; }
            CompatibilityLayoutGeometryRecord geometry = (compatibility.Value.GeometryRecords ??
                Array.Empty<CompatibilityLayoutGeometryRecord>()).SingleOrDefault(value => value != null &&
                value.GeometryId == profile.GeometryId && value.GeometryVersion == profile.GeometryVersion &&
                value.CanonicalHash == profile.GeometryCanonicalHash);
            CompatibilityLayoutVariant variant = (geometry?.Layouts ?? Array.Empty<CompatibilityLayoutVariant>())
                .SingleOrDefault(value => value != null && value.Placements.Count(placement => placement != null &&
                    placement.Role == CompatibilityRouteRole.BasicRoom0) == 1 &&
                    value.Placements.All(placement => placement == null ||
                        placement.Role != CompatibilityRouteRole.BasicRoom1));
            SpatialContentCatalog catalog = production.Catalog;
            FloorSpatialConfiguration floorDefinition = (catalog.Floors ?? Array.Empty<FloorSpatialConfiguration>())
                .SingleOrDefault(value => value != null && value.FloorDefinitionId == geometry?.FloorDefinitionId &&
                    value.FloorIndex == geometry.FloorIndex);
            RoomSpatialDefinition roomDefinition = (catalog.Rooms ?? Array.Empty<RoomSpatialDefinition>())
                .SingleOrDefault(value => value != null && value.RoomDefinitionId == geometry?.BasicRoomDefinitionId);
            FixedSpatialStructureDefinition entrance = (catalog.FixedStructures ?? Array.Empty<FixedSpatialStructureDefinition>())
                .SingleOrDefault(value => value != null && value.StructureDefinitionId == geometry?.EntranceStructureDefinitionId);
            FixedSpatialStructureDefinition completion = (catalog.FixedStructures ?? Array.Empty<FixedSpatialStructureDefinition>())
                .SingleOrDefault(value => value != null && value.StructureDefinitionId == geometry?.CompletionStructureDefinitionId);
            if (geometry == null || variant == null || floorDefinition == null || roomDefinition == null ||
                entrance == null || completion == null) { reason = "gd66.starter_profile.invalid"; return false; }
            const string floorId = "compat.floor.00";
            CompatibilityLayoutPlacement roomPlacement = variant.Placements.Single(value =>
                value.Role == CompatibilityRouteRole.BasicRoom0);
            string roomId = floorId + ".legacy-room.00";
            var room = new RoomSpatialInstance { RoomInstanceId = roomId,
                RoomDefinitionId = roomDefinition.RoomDefinitionId, FloorId = floorId,
                Anchor = roomPlacement.Anchor, Orientation = roomPlacement.Orientation };
            var fixedValues = variant.Placements.Where(value => value.Role == CompatibilityRouteRole.Entrance ||
                value.Role == CompatibilityRouteRole.Completion).Select(value => Fixed(floorId, value,
                    value.Role == CompatibilityRouteRole.Entrance ? entrance : completion)).ToArray();
            var nodes = variant.Placements.Select(value => Node(floorId, value, roomId)).ToArray();
            var edges = variant.Connections.Select(value => Edge(floorId, value)).ToArray();
            saved = new SavedSpatialFloor
            {
                FloorInstanceId = floorId, FloorDefinitionId = floorDefinition.FloorDefinitionId,
                FloorIndex = geometry.FloorIndex,
                Layout = new FloorSpatialLayout { FloorId = floorId, Rooms = new[] { room },
                    Nodes = nodes, Edges = edges }, FixedStructures = fixedValues,
                RoomContents = new FloorRoomContentState { Assignments = Array.Empty<RoomContentAssignment>(),
                    RoomSemantics = new[] { new CanonicalRoomSemantics { RoomInstanceId = roomId,
                        LegacyRoomOriginKind = origin } }, NextSequence = 0 }
            };
            reason = null; return true;
        }

        private static SavedFixedSpatialStructure Fixed(string floorId,
            CompatibilityLayoutPlacement placement, FixedSpatialStructureDefinition definition)
        {
            bool entrance = placement.Role == CompatibilityRouteRole.Entrance;
            return new SavedFixedSpatialStructure { FixedStructureInstanceId = floorId + ".fixed." +
                (entrance ? "entrance" : "completion"), FixedStructureDefinitionId = definition.StructureDefinitionId,
                FloorInstanceId = floorId, Anchor = placement.Anchor, Orientation = placement.Orientation,
                Kind = entrance ? FixedSpatialStructureKind.Entrance : FixedSpatialStructureKind.CompletionTerminal };
        }

        private static FloorRouteNode Node(string floorId, CompatibilityLayoutPlacement placement, string roomId)
        {
            string role = Role(placement.Role);
            return new FloorRouteNode { NodeId = floorId + ".node." + role, FloorId = floorId,
                Kind = placement.Role == CompatibilityRouteRole.Entrance ? FloorRouteNodeKind.Entrance :
                    placement.Role == CompatibilityRouteRole.Completion ? FloorRouteNodeKind.Completion :
                    FloorRouteNodeKind.Room,
                RoomInstanceId = placement.Role == CompatibilityRouteRole.BasicRoom0 ? roomId : null };
        }

        private static FloorRouteEdge Edge(string floorId, CompatibilityLayoutConnection connection) =>
            new FloorRouteEdge { EdgeId = floorId + ".edge.direct." + Role(connection.SourceRole) + "." +
                Role(connection.DestinationRole), CorridorDefinitionId = string.Empty, FloorId = floorId,
                SourceNodeId = floorId + ".node." + Role(connection.SourceRole),
                DestinationNodeId = floorId + ".node." + Role(connection.DestinationRole), Footprint = null,
                Classification = RouteClassification.Required, OptionalBranchId = string.Empty,
                ConnectionKind = FloorRouteConnectionKind.DirectDoorway };

        private static string Role(CompatibilityRouteRole role) => role == CompatibilityRouteRole.Entrance
            ? "entrance" : role == CompatibilityRouteRole.Completion ? "completion" :
            role == CompatibilityRouteRole.BasicRoom0 ? "legacy-room.00" : "legacy-room.01";

        private static bool TryTargetRoom(DetachedCanonicalSpatialSaveState state, string requestedRoomId,
            out SavedSpatialFloor floor, out RoomSpatialInstance room, out CanonicalRoomSemantics semantics)
        {
            floor = state.Floors?.Length == 1 ? state.Floors[0] : null;
            RoomSpatialInstance[] rooms = floor?.Layout?.Rooms ?? Array.Empty<RoomSpatialInstance>();
            room = string.IsNullOrEmpty(requestedRoomId)
                ? rooms.Length == 1 ? rooms[0] : null
                : rooms.SingleOrDefault(value => value != null &&
                    value.RoomInstanceId == requestedRoomId);
            string roomInstanceId = room?.RoomInstanceId;
            semantics = floor?.RoomContents?.RoomSemantics?.SingleOrDefault(value => value != null &&
                value.RoomInstanceId == roomInstanceId);
            return room != null && semantics != null;
        }

        private static string ResolveBasicDefinition(DetachedCanonicalSpatialSaveState state,
            ProductionSpatialContentSnapshot production, SpatialLayoutCompatibilitySnapshot compatibility,
            out string reason)
        {
            reason = ValidationFailedReason;
            CompatibilitySelectionResult<CanonicalStarterLayoutProfile> selected = compatibility.SelectStarter(
                DetachedWholeSaveCandidateSerializer.TargetSchemaVersion,
                state.Authority.CanonicalLayoutContractVersion);
            if (!selected.Success) { reason = selected.Code; return null; }
            CompatibilityLayoutGeometryRecord geometry = (compatibility.Value.GeometryRecords ??
                Array.Empty<CompatibilityLayoutGeometryRecord>()).SingleOrDefault(value => value != null &&
                value.GeometryId == selected.Value.GeometryId && value.GeometryVersion == selected.Value.GeometryVersion &&
                value.CanonicalHash == selected.Value.GeometryCanonicalHash);
            if (geometry == null || !CanonicalRoomCapacityResolver.TryResolve(production,
                geometry.BasicRoomDefinitionId, out MvpRoomSlotCapacity ignored, out reason)) return null;
            reason = null; return geometry.BasicRoomDefinitionId;
        }

        private static bool TryClone(DetachedCanonicalSpatialSaveState state,
            CanonicalSpatialSerializationLimits limits, out DetachedCanonicalSpatialSaveState clone)
        {
            clone = null;
            SpatialContractResult<byte[]> serialized = CanonicalSpatialSaveSerializer.Serialize(state, limits);
            if (!serialized.IsValid) return false;
            SpatialContractResult<DetachedCanonicalSpatialSaveState> parsed =
                CanonicalSpatialSaveSerializer.Parse(serialized.Value, limits);
            clone = parsed.Value; return parsed.IsValid;
        }

        private static DetachedCanonicalMutationResult Failure(string reason) =>
            new DetachedCanonicalMutationResult(null, reason, false);
    }
}
