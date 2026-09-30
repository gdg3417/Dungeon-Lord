using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.MvpDungeonPlacements
{
    public enum CanonicalMvpRuntimeAuthorityState
    {
        Legacy = 0,
        ValidatedCanonical = 1,
        ContradictoryCanonical = 2
    }

    public sealed class CanonicalMvpRouteProjectionResult
    {
        internal CanonicalMvpRouteProjectionResult(CanonicalMvpRuntimeAuthorityState authorityState,
            MvpOrderedRouteRoom[] rooms, string reason)
        { AuthorityState = authorityState; Rooms = rooms; Reason = reason; }
        public CanonicalMvpRuntimeAuthorityState AuthorityState { get; }
        public MvpOrderedRouteRoom[] Rooms { get; }
        public string Reason { get; }
    }

    /// <summary>
    /// The single MVP compatibility projection from validated current schema-8 graph/content authority.
    /// Serialized marker/floor fields alone never publish runtime authority.
    /// </summary>
    public static class CanonicalMvpRouteProjection
    {
        public const string ContradictoryAuthorityReason = "gd66.authority.contradictory_state";

        public static bool IsCanonical(SaveData save) =>
            save?.validatedCanonicalSpatialState != null;

        public static bool HasCanonicalLookingState(SaveData save) => save != null &&
            (save.canonicalSpatialAuthority != null || save.spatialFloors != null);

        internal static bool TryPublishValidated(DetachedCompleteSaveValidationResult validation,
            out SaveData save, out string reason)
            => TryPublishValidated(validation, null, out save, out reason);

        internal static bool TryPublishValidated(DetachedCompleteSaveValidationResult validation,
            ProductionSpatialContentSnapshot production, out SaveData save, out string reason)
        {
            save = null;
            reason = ContradictoryAuthorityReason;
            if (validation == null || !validation.IsValid ||
                !validation.CurrentTargetValidated || validation.State == null)
                return false;
            CanonicalMvpRouteProjectionResult projected = Project(validation.State, null, production);
            if (projected.AuthorityState != CanonicalMvpRuntimeAuthorityState.ValidatedCanonical)
                return false;
            try
            {
                SaveRoot root = JsonUtility.FromJson<SaveRoot>(
                    System.Text.Encoding.UTF8.GetString(validation.GetBytes()));
                if (root?.primary == null || root.schemaVersion !=
                    CanonicalSaveSchemaVersions.CurrentWritableTarget) return false;
                save = root.primary;
                if (validation.ResearchPendingExplicitNull) save.researchPending = null;
                if (validation.ResearchProgressExplicitNull) save.researchProgress = null;
                if (validation.LastOfflineSummaryExplicitNull) save.lastOfflineSummary = null;
                // Runtime consumes the exact strict-parser state, not a second permissive parse
                // of the two canonical members.
                save.canonicalSpatialAuthority = validation.State.Authority;
                save.spatialFloors = validation.State.Floors;
                save.validatedCanonicalSpatialState = validation.State;
                save.corridorContent = validation.CorridorContent;
                save.sharedBranchKnowledge = validation.BranchKnowledge;
                reason = null;
                return true;
            }
            catch
            {
                save = null;
                return false;
            }
        }

        public static CanonicalMvpRouteProjectionResult Inspect(SaveData save,
            RunSimulationConfig config)
        {
            if (save == null || (!HasCanonicalLookingState(save) &&
                save.validatedCanonicalSpatialState == null))
                return new CanonicalMvpRouteProjectionResult(
                    CanonicalMvpRuntimeAuthorityState.Legacy, null, null);
            if (save.validatedCanonicalSpatialState == null)
                return Contradictory();
            if (!ReferenceEquals(save.canonicalSpatialAuthority,
                    save.validatedCanonicalSpatialState.Authority) ||
                !ReferenceEquals(save.spatialFloors,
                    save.validatedCanonicalSpatialState.Floors))
                return Contradictory();
            return Project(save.validatedCanonicalSpatialState, config, null);
        }

        public static CanonicalMvpRouteProjectionResult InspectWithProductionContent(SaveData save,
            ProductionSpatialContentSnapshot production)
        {
            if (save == null || (!HasCanonicalLookingState(save) &&
                save.validatedCanonicalSpatialState == null))
                return new CanonicalMvpRouteProjectionResult(
                    CanonicalMvpRuntimeAuthorityState.Legacy, null, null);
            if (save.validatedCanonicalSpatialState == null ||
                !ReferenceEquals(save.canonicalSpatialAuthority,
                    save.validatedCanonicalSpatialState.Authority) ||
                !ReferenceEquals(save.spatialFloors,
                    save.validatedCanonicalSpatialState.Floors)) return Contradictory();
            return Project(save.validatedCanonicalSpatialState, null, production);
        }

        public static MvpOrderedRouteRoom[] Resolve(SaveData save, RunSimulationConfig config)
            => Resolve(save, config, null);

        public static MvpOrderedRouteRoom[] Resolve(SaveData save, RunSimulationConfig config,
            ProductionSpatialContentSnapshot production)
        {
            CanonicalMvpRouteProjectionResult result = production == null
                ? Inspect(save, config) : InspectWithProductionContent(save, production);
            return result.AuthorityState == CanonicalMvpRuntimeAuthorityState.Legacy ? null : result.Rooms;
        }

        public static MvpDungeonPlacementEntry[] ResolveActivePlacements(SaveData save,
            RunSimulationConfig config)
            => ResolveActivePlacements(save, config, null);

        public static MvpDungeonPlacementEntry[] ResolveActivePlacements(SaveData save,
            RunSimulationConfig config, ProductionSpatialContentSnapshot production)
        {
            MvpOrderedRouteRoom[] route = Resolve(save, config, production);
            if (route == null) return null;
            var result = new List<MvpDungeonPlacementEntry>();
            foreach (MvpOrderedRouteRoom room in route)
                result.AddRange(room.ToOrderedPlacements());
            return result.ToArray();
        }

        private static CanonicalMvpRouteProjectionResult Project(
            DetachedCanonicalSpatialSaveState state, RunSimulationConfig config,
            ProductionSpatialContentSnapshot production)
        {
            if (!MarkerIsValid(state?.Authority) || state.Floors == null) return Contradictory();
            if (state.Floors.Length == 0) return Valid(Array.Empty<MvpOrderedRouteRoom>());
            if (state.Floors.Any(f => f == null || f.FloorIndex < 0 || string.IsNullOrWhiteSpace(f.FloorInstanceId) ||
                    (f.ActivationState != FloorActivationState.Active && f.ActivationState != FloorActivationState.Inactive)) ||
                state.Floors.Select(f => f.FloorIndex).Distinct().Count() != state.Floors.Length ||
                state.Floors.Select(f => f.FloorInstanceId).Distinct(StringComparer.Ordinal).Count() != state.Floors.Length)
                return Contradictory();
            var active = state.Floors.Where(f => f != null && f.ActivationState == FloorActivationState.Active)
                .OrderBy(f => f.FloorIndex).ThenBy(f => f.FloorInstanceId, StringComparer.Ordinal).ToArray();
            if (active.Length == 0) return Contradictory();
            CanonicalMvpRouteProjectionResult first = null;
            for (int i = 0; i < active.Length; i++)
            {
                if (active[i].FloorIndex != i) return Contradictory();
                var result = CanonicalRunnableFloorProjection.Resolve(active[i], config, production);
                if (result.AuthorityState != CanonicalMvpRuntimeAuthorityState.ValidatedCanonical) return Contradictory();
                if (i == 0) first = result;
            }
            // Compatibility consumers inspect Floor 1 only. Real runs consume the full snapshot.
            return first;
        }

        private static MvpRoomSlotCapacity ResolveCapacity(RoomSpatialInstance room,
            RunSimulationConfig legacyConfig, ProductionSpatialContentSnapshot production)
        {
            if (production != null)
            {
                if (!CanonicalRoomCapacityResolver.TryResolve(production, room.RoomDefinitionId,
                    out MvpRoomSlotCapacity capacity, out string ignored))
                    throw new InvalidOperationException();
                capacity.RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId;
                return capacity;
            }
            // Inactive compatibility overload only. Final live cutover injects production content.
            return MvpRoomSlotLayoutResolver.ResolveCapacity(
                MvpDungeonPlacementIds.BasicRoomOptionId, legacyConfig);
        }

        private static bool RoomDefinitionIsAllowed(SavedSpatialFloor floor,
            RoomSpatialInstance room, ProductionSpatialContentSnapshot production)
        {
            if (production == null)
                return string.Equals(room.RoomDefinitionId, "spatial.room.basic", StringComparison.Ordinal);
            RoomSpatialDefinition[] rooms = (production.Catalog.Rooms ?? Array.Empty<RoomSpatialDefinition>())
                .Where(value => value != null && value.RoomDefinitionId == room.RoomDefinitionId).ToArray();
            FloorSpatialConfiguration[] floors = (production.Catalog.Floors ?? Array.Empty<FloorSpatialConfiguration>())
                .Where(value => value != null && value.FloorDefinitionId == floor.FloorDefinitionId &&
                    value.FloorIndex == floor.FloorIndex).ToArray();
            return rooms.Length == 1 && floors.Length == 1 &&
                (floors[0].AllowedRoomDefinitionIds ?? Array.Empty<string>()).Contains(
                    room.RoomDefinitionId);
        }

        private static CanonicalMvpRouteProjectionResult Valid(MvpOrderedRouteRoom[] rooms) =>
            new CanonicalMvpRouteProjectionResult(
                CanonicalMvpRuntimeAuthorityState.ValidatedCanonical, rooms, null);
        private static CanonicalMvpRouteProjectionResult Contradictory() =>
            new CanonicalMvpRouteProjectionResult(
                CanonicalMvpRuntimeAuthorityState.ContradictoryCanonical,
                Array.Empty<MvpOrderedRouteRoom>(), ContradictoryAuthorityReason);
        private static string[] Options(IEnumerable<RoomContentAssignment> values, string category) =>
            values.Where(value => string.Equals(value.CategoryId, category, StringComparison.Ordinal))
                .Select(value => value.OptionId).ToArray();
        private static int CategoryRank(string category) =>
            category == CanonicalSpatialSaveContracts.MonsterCategoryId ? 0 :
            category == CanonicalSpatialSaveContracts.TrapCategoryId ? 1 :
            category == CanonicalSpatialSaveContracts.LootNodeCategoryId ? 2 : int.MaxValue;
        private static bool MarkerIsValid(CanonicalSpatialAuthorityMarker marker)
        {
            if (marker == null || marker.CanonicalLayoutContractVersion <= 0 ||
                !Enum.IsDefined(typeof(CanonicalSpatialCreationKind), marker.CreationKind)) return false;
            if (marker.CreationKind == CanonicalSpatialCreationKind.NativeCanonical)
                return string.IsNullOrEmpty(marker.MigrationTransactionId) &&
                    string.IsNullOrEmpty(marker.MigrationDescriptorFingerprint);
            return SpatialMigrationTransactionIdentity.IsCanonicalTransactionId(
                    marker.MigrationTransactionId) &&
                SpatialContractSha256.IsCanonical(marker.MigrationDescriptorFingerprint);
        }
    }
}
