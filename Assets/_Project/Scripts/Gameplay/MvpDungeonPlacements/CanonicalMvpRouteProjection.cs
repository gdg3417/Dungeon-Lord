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
    /// The single MVP compatibility projection from validated schema-7 graph/content authority.
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
                    DetachedWholeSaveCandidateSerializer.TargetSchemaVersion) return false;
                save = root.primary;
                // Runtime consumes the exact strict-parser state, not a second permissive parse
                // of the two canonical members.
                save.canonicalSpatialAuthority = validation.State.Authority;
                save.spatialFloors = validation.State.Floors;
                save.validatedCanonicalSpatialState = validation.State;
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
        {
            CanonicalMvpRouteProjectionResult result = Inspect(save, config);
            return result.AuthorityState == CanonicalMvpRuntimeAuthorityState.Legacy ? null : result.Rooms;
        }

        public static MvpDungeonPlacementEntry[] ResolveActivePlacements(SaveData save,
            RunSimulationConfig config)
        {
            MvpOrderedRouteRoom[] route = Resolve(save, config);
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
            try
            {
                if (!MarkerIsValid(state?.Authority) || state.Floors == null)
                    return Contradictory();
                if (state.Floors.Length == 0)
                    return Valid(Array.Empty<MvpOrderedRouteRoom>());
                if (state.Floors.Length != 1 || state.Floors[0] == null ||
                    state.Floors[0].FloorIndex != 0)
                    return Contradictory();
                SavedSpatialFloor floor = state.Floors[0];
                if (floor.Layout == null || floor.RoomContents == null)
                    return Contradictory();
                RoomSpatialInstance[] rooms = floor.Layout.Rooms;
                FloorRouteNode[] nodes = floor.Layout.Nodes;
                FloorRouteEdge[] edges = floor.Layout.Edges;
                CanonicalRoomSemantics[] semanticValues = floor.RoomContents.RoomSemantics;
                RoomContentAssignment[] assignments = floor.RoomContents.Assignments;
                if (rooms == null || nodes == null || edges == null || semanticValues == null ||
                    assignments == null) return Contradictory();

                var roomById = new Dictionary<string, RoomSpatialInstance>(StringComparer.Ordinal);
                foreach (RoomSpatialInstance room in rooms)
                    if (room == null || string.IsNullOrWhiteSpace(room.RoomInstanceId) ||
                        roomById.ContainsKey(room.RoomInstanceId)) return Contradictory();
                    else roomById.Add(room.RoomInstanceId, room);
                var nodeById = new Dictionary<string, FloorRouteNode>(StringComparer.Ordinal);
                var roomNodeIds = new HashSet<string>(StringComparer.Ordinal);
                FloorRouteNode entrance = null, completion = null;
                foreach (FloorRouteNode node in nodes)
                {
                    if (node == null || string.IsNullOrWhiteSpace(node.NodeId) ||
                        nodeById.ContainsKey(node.NodeId)) return Contradictory();
                    nodeById.Add(node.NodeId, node);
                    if (node.Kind == FloorRouteNodeKind.Entrance)
                    { if (entrance != null) return Contradictory(); entrance = node; }
                    if (node.Kind == FloorRouteNodeKind.Completion)
                    { if (completion != null) return Contradictory(); completion = node; }
                    if (node.Kind == FloorRouteNodeKind.Room &&
                        (string.IsNullOrWhiteSpace(node.RoomInstanceId) ||
                         !roomNodeIds.Add(node.RoomInstanceId))) return Contradictory();
                    if (node.Kind != FloorRouteNodeKind.Entrance &&
                        node.Kind != FloorRouteNodeKind.Room &&
                        node.Kind != FloorRouteNodeKind.Completion) return Contradictory();
                }
                if (entrance == null || completion == null ||
                    roomNodeIds.Count != roomById.Count ||
                    roomNodeIds.Any(id => !roomById.ContainsKey(id))) return Contradictory();

                var semantics = new Dictionary<string, LegacyRoomOriginKind>(StringComparer.Ordinal);
                foreach (CanonicalRoomSemantics value in semanticValues)
                    if (value == null || string.IsNullOrWhiteSpace(value.RoomInstanceId) ||
                        semantics.ContainsKey(value.RoomInstanceId)) return Contradictory();
                    else semantics.Add(value.RoomInstanceId, value.LegacyRoomOriginKind);
                if (semantics.Count != roomById.Count ||
                    semantics.Keys.Any(id => !roomById.ContainsKey(id))) return Contradictory();

                var outgoing = new Dictionary<string, FloorRouteEdge>(StringComparer.Ordinal);
                var incoming = new Dictionary<string, int>(StringComparer.Ordinal);
                var edgeIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (FloorRouteEdge edge in edges)
                {
                    if (edge == null || string.IsNullOrWhiteSpace(edge.EdgeId) ||
                        !edgeIds.Add(edge.EdgeId) ||
                        edge.Classification != RouteClassification.Required ||
                        string.IsNullOrWhiteSpace(edge.SourceNodeId) ||
                        string.IsNullOrWhiteSpace(edge.DestinationNodeId) ||
                        !nodeById.ContainsKey(edge.SourceNodeId) ||
                        !nodeById.ContainsKey(edge.DestinationNodeId) ||
                        outgoing.ContainsKey(edge.SourceNodeId)) return Contradictory();
                    outgoing.Add(edge.SourceNodeId, edge);
                    incoming.TryGetValue(edge.DestinationNodeId, out int count);
                    incoming[edge.DestinationNodeId] = count + 1;
                }
                if (edges.Length != nodeById.Count - 1 || incoming.ContainsKey(entrance.NodeId) ||
                    outgoing.ContainsKey(completion.NodeId)) return Contradictory();
                foreach (FloorRouteNode node in nodes)
                    if (node != entrance && (!incoming.TryGetValue(node.NodeId, out int count) ||
                        count != 1) || node != completion && !outgoing.ContainsKey(node.NodeId))
                        return Contradictory();

                var result = new List<MvpOrderedRouteRoom>();
                var visitedNodes = new HashSet<string>(StringComparer.Ordinal);
                var visitedRooms = new HashSet<string>(StringComparer.Ordinal);
                FloorRouteNode current = entrance;
                while (current != null)
                {
                    if (!visitedNodes.Add(current.NodeId)) return Contradictory();
                    if (current.Kind == FloorRouteNodeKind.Completion) break;
                    if (current.Kind == FloorRouteNodeKind.Room)
                    {
                        if (!roomById.TryGetValue(current.RoomInstanceId ?? string.Empty,
                            out RoomSpatialInstance room) || !visitedRooms.Add(room.RoomInstanceId) ||
                            !semantics.TryGetValue(room.RoomInstanceId, out LegacyRoomOriginKind origin) ||
                            !string.Equals(room.RoomDefinitionId, "spatial.room.basic",
                                StringComparison.Ordinal)) return Contradictory();
                        RoomContentAssignment[] owned = assignments.Where(value => value != null &&
                            string.Equals(value.RoomInstanceId, room.RoomInstanceId,
                                StringComparison.Ordinal)).OrderBy(value => CategoryRank(value.CategoryId))
                            .ThenBy(value => value.Sequence).ThenBy(value => value.AssignmentId,
                                StringComparer.Ordinal).ToArray();
                        if (owned.Any(value => CategoryRank(value.CategoryId) == int.MaxValue))
                            return Contradictory();
                        result.Add(new MvpOrderedRouteRoom
                        {
                            FloorIndex = floor.FloorIndex, RoomIndex = result.Count,
                            RoomInstanceId = room.RoomInstanceId,
                            RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                            IncludeRoomPlacement = origin !=
                                LegacyRoomOriginKind.ImplicitCompatibilityContainer,
                            AssignedMonsterOptionIds = Options(owned,
                                CanonicalSpatialSaveContracts.MonsterCategoryId),
                            AssignedTrapOptionIds = Options(owned,
                                CanonicalSpatialSaveContracts.TrapCategoryId),
                            AssignedLootNodeOptionIds = Options(owned,
                                CanonicalSpatialSaveContracts.LootNodeCategoryId),
                            Capacity = ResolveCapacity(room, config, production),
                            HasActiveContent = owned.Length != 0
                        });
                    }
                    if (!outgoing.TryGetValue(current.NodeId, out FloorRouteEdge next) ||
                        !nodeById.TryGetValue(next.DestinationNodeId, out current))
                        return Contradictory();
                }
                if (current != completion || outgoing.ContainsKey(completion.NodeId) ||
                    visitedRooms.Count != roomById.Count || assignments.Any(value => value == null ||
                        !roomById.ContainsKey(value.RoomInstanceId ?? string.Empty)))
                    return Contradictory();
                return Valid(result.ToArray());
            }
            catch
            {
                // Projection is a trust boundary. Malformed state is classified, never surfaced.
                return Contradictory();
            }
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
