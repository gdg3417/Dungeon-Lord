using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum CanonicalSpatialCreationKind { NativeCanonical = 1, Migrated = 2 }
    public enum LegacyRoomOriginKind
    {
        MigratedExplicitLegacyRoom = 1,
        ImplicitCompatibilityContainer = 2,
        CanonicalPlayerPlaced = 3
    }

    [Serializable]
    public sealed class CanonicalSpatialAuthorityMarker
    {
        public int CanonicalLayoutContractVersion;
        public CanonicalSpatialCreationKind CreationKind;
        public string MigrationTransactionId;
        public string MigrationDescriptorFingerprint;
    }

    [Serializable]
    public sealed class SavedFixedSpatialStructure
    {
        public string FixedStructureInstanceId;
        public string FixedStructureDefinitionId;
        public string FloorInstanceId;
        public TileCoordinate Anchor;
        public CardinalOrientation Orientation;
        public FixedSpatialStructureKind Kind;
    }

    [Serializable]
    public sealed class RoomContentAssignment
    {
        public string AssignmentId;
        public string RoomInstanceId;
        public string CategoryId;
        public string OptionId;
        public long Sequence;
    }

    [Serializable]
    public sealed class CanonicalRoomSemantics
    {
        public string RoomInstanceId;
        public LegacyRoomOriginKind LegacyRoomOriginKind;
    }

    [Serializable]
    public sealed class FloorRoomContentState
    {
        public RoomContentAssignment[] Assignments = Array.Empty<RoomContentAssignment>();
        public CanonicalRoomSemantics[] RoomSemantics = Array.Empty<CanonicalRoomSemantics>();
        public long NextSequence;
    }

    public enum StructuralContentRemovalDisposition
    {
        ReturnToPlayerCustody = 1
    }

    [Serializable]
    public sealed class ReturnedStructuralContent
    {
        public string AssignmentId;
        public string CategoryId;
        public string OptionId;
        public long Sequence;
        public StructuralContentRemovalDisposition RemovalDisposition;
    }

    [Serializable]
    public sealed class FloorStructuralIdentityLifecycle
    {
        public string FloorInstanceId;
        public int NextNativeRoomOrdinal;
        public long NextNativeEdgeOrdinal;
    }

    [Serializable]
    public sealed class StructuralLifecycleAndOwnershipState
    {
        public FloorStructuralIdentityLifecycle[] Floors = Array.Empty<FloorStructuralIdentityLifecycle>();
        public ReturnedStructuralContent[] ReturnedContents = Array.Empty<ReturnedStructuralContent>();
    }

    [Serializable]
    public sealed class SavedSpatialFloor
    {
        public string FloorInstanceId;
        public string FloorDefinitionId;
        public int FloorIndex;
        public FloorSpatialLayout Layout;
        public SavedFixedSpatialStructure[] FixedStructures = Array.Empty<SavedFixedSpatialStructure>();
        public FloorRoomContentState RoomContents;
    }

    // Canonicalization holder used by the strict schema-8 serializer.
    [Serializable]
    public sealed class DetachedCanonicalSpatialSaveState
    {
        public CanonicalSpatialAuthorityMarker Authority;
        public SavedSpatialFloor[] Floors = Array.Empty<SavedSpatialFloor>();
        public StructuralLifecycleAndOwnershipState LifecycleAndOwnership =
            new StructuralLifecycleAndOwnershipState();
    }

    public readonly struct CanonicalSpatialSaveWorkloadLimits
    {
        public CanonicalSpatialSaveWorkloadLimits(int maximumRecords, int maximumMaterializedTiles)
        {
            MaximumRecords = maximumRecords;
            MaximumMaterializedTiles = maximumMaterializedTiles;
        }
        public int MaximumRecords { get; }
        public int MaximumMaterializedTiles { get; }
        public bool IsValid => MaximumRecords > 0 && MaximumMaterializedTiles > 0;
    }

    public enum CanonicalSpatialSaveValidationIssue
    {
        InvalidSource = 1,
        InvalidWorkloadLimits = 2,
        RecordLimitExceeded = 3,
        MaterializedTileLimitExceeded = 4,
        MissingAuthority = 5,
        InvalidLayoutContractVersion = 6,
        InvalidCreationKind = 7,
        NativeMarkerHasMigrationIdentity = 8,
        MalformedPersistentId = 9,
        DuplicateFloorIndex = 10,
        CandidateInstanceIdCollision = 11,
        NullFloorRecord = 12,
        MissingLayout = 13,
        NegativeFloorIndex = 14,
        FloorReferenceMismatch = 15,
        NullRoomRecord = 16,
        NullNodeRecord = 17,
        NullEdgeRecord = 18,
        NullFixedStructureRecord = 19,
        NullAssignmentRecord = 20,
        NullRoomSemanticsRecord = 21,
        InvalidRoomOrientation = 22,
        InvalidNodeKind = 23,
        InvalidEdgeConnectionKind = 24,
        InvalidEdgeClassification = 25,
        InvalidFixedStructureOrientation = 26,
        InvalidFixedStructureKind = 27,
        UnknownRoomReference = 28,
        UnknownEdgeSource = 29,
        UnknownEdgeDestination = 30,
        InvalidDirectDoorwayShape = 31,
        InvalidEdgeBranchShape = 32,
        MissingRoomContents = 33,
        DuplicateRoomCategorySequence = 34,
        InvalidContentCategory = 35,
        NegativeSequence = 36,
        InvalidNextSequence = 37,
        MissingRoomSemantics = 38,
        DuplicateRoomSemantics = 39,
        UnknownRoomSemantics = 40,
        InvalidRoomOriginKind = 41,
        NonCanonicalOrdering = 42,
        NonRoomNodeHasRoomReference = 43,
        InvalidPhysicalCorridorShape = 44,
        MissingLifecycleAndOwnership = 45,
        InvalidIdentityLifecycle = 46,
        DuplicateLifecycleFloor = 47,
        DuplicateReturnedIdentity = 48,
        AssignedAndReturnedIdentity = 49,
        InvalidReturnedContent = 50
    }

    public sealed class CanonicalSpatialSaveValidationResult
    {
        internal CanonicalSpatialSaveValidationResult(IEnumerable<CanonicalSpatialSaveValidationIssue> issues)
        {
            Issues = issues.Distinct().OrderBy(issue => (int)issue).ToArray();
        }
        public CanonicalSpatialSaveValidationIssue[] Issues { get; }
        public bool IsValid => Issues.Length == 0;
    }

    public static class CanonicalSpatialSaveContracts
    {
        public const string MonsterCategoryId = "placement.category.monster";
        public const string TrapCategoryId = "placement.category.trap";
        public const string LootNodeCategoryId = "placement.category.loot_node";

        private enum CanonicalizationFailure
        {
            None,
            InvalidSource,
            InvalidLimits,
            RecordLimit,
            TileLimit
        }

        public static bool TryCanonicalize(DetachedCanonicalSpatialSaveState source,
            CanonicalSpatialSaveWorkloadLimits limits, out DetachedCanonicalSpatialSaveState canonical) =>
            TryCanonicalizeCore(source, limits, true, out canonical) == CanonicalizationFailure.None;

        internal static bool TryCanonicalizeFrozenSchemaSeven(DetachedCanonicalSpatialSaveState source,
            CanonicalSpatialSaveWorkloadLimits limits, out DetachedCanonicalSpatialSaveState canonical) =>
            TryCanonicalizeCore(source, limits, false, out canonical) == CanonicalizationFailure.None;

        public static CanonicalSpatialSaveValidationResult Validate(DetachedCanonicalSpatialSaveState state,
            CanonicalSpatialSaveWorkloadLimits limits, bool requireCanonicalOrdering = false)
        {
            var issues = new List<CanonicalSpatialSaveValidationIssue>();
            return ValidateCore(state, limits, requireCanonicalOrdering, true);
        }

        internal static CanonicalSpatialSaveValidationResult ValidateFrozenSchemaSeven(
            DetachedCanonicalSpatialSaveState state, CanonicalSpatialSaveWorkloadLimits limits,
            bool requireCanonicalOrdering = false) =>
            ValidateCore(state, limits, requireCanonicalOrdering, false);

        private static CanonicalSpatialSaveValidationResult ValidateCore(DetachedCanonicalSpatialSaveState state,
            CanonicalSpatialSaveWorkloadLimits limits, bool requireCanonicalOrdering, bool includeLifecycle)
        {
            var issues = new List<CanonicalSpatialSaveValidationIssue>();
            CanonicalizationFailure failure = TryCanonicalizeCore(state, limits, includeLifecycle,
                out DetachedCanonicalSpatialSaveState canonical);
            if (failure != CanonicalizationFailure.None)
            {
                issues.Add(FailureIssue(failure));
                return new CanonicalSpatialSaveValidationResult(issues);
            }

            ValidateMarker(state.Authority, issues);
            SavedSpatialFloor[] floors = state.Floors ?? Array.Empty<SavedSpatialFloor>();
            if (floors.Where(floor => floor != null).GroupBy(floor => floor.FloorIndex).Any(group => group.Count() > 1))
                issues.Add(CanonicalSpatialSaveValidationIssue.DuplicateFloorIndex);

            var instanceIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (SavedSpatialFloor floor in floors)
                ValidateFloor(floor, instanceIds, issues);

            if (includeLifecycle) ValidateLifecycleAndOwnership(state, instanceIds, issues);

            if (requireCanonicalOrdering && !HasCanonicalOrdering(state))
                issues.Add(CanonicalSpatialSaveValidationIssue.NonCanonicalOrdering);
            return new CanonicalSpatialSaveValidationResult(issues);
        }

        private static CanonicalizationFailure TryCanonicalizeCore(DetachedCanonicalSpatialSaveState source,
            CanonicalSpatialSaveWorkloadLimits limits, bool includeLifecycle,
            out DetachedCanonicalSpatialSaveState canonical)
        {
            canonical = null;
            if (source == null) return CanonicalizationFailure.InvalidSource;
            if (!limits.IsValid) return CanonicalizationFailure.InvalidLimits;
            SavedSpatialFloor[] floors = source.Floors ?? Array.Empty<SavedSpatialFloor>();
            long records = 0L, tiles = 0L;
            if (!TryAdd(ref records, floors.LongLength, limits.MaximumRecords)) return CanonicalizationFailure.RecordLimit;
            StructuralLifecycleAndOwnershipState lifecycle = source.LifecycleAndOwnership;
            if (includeLifecycle && lifecycle != null &&
                (!TryAdd(ref records, lifecycle.Floors?.LongLength ?? 0L, limits.MaximumRecords) ||
                 !TryAdd(ref records, lifecycle.ReturnedContents?.LongLength ?? 0L, limits.MaximumRecords)))
                return CanonicalizationFailure.RecordLimit;
            foreach (SavedSpatialFloor floor in floors)
            {
                if (floor == null) continue;
                FloorSpatialLayout layout = floor.Layout;
                if (!TryAdd(ref records, layout?.Rooms?.LongLength ?? 0L, limits.MaximumRecords) ||
                    !TryAdd(ref records, layout?.Nodes?.LongLength ?? 0L, limits.MaximumRecords) ||
                    !TryAdd(ref records, layout?.Edges?.LongLength ?? 0L, limits.MaximumRecords) ||
                    !TryAdd(ref records, floor.FixedStructures?.LongLength ?? 0L, limits.MaximumRecords) ||
                    !TryAdd(ref records, floor.RoomContents?.Assignments?.LongLength ?? 0L, limits.MaximumRecords) ||
                    !TryAdd(ref records, floor.RoomContents?.RoomSemantics?.LongLength ?? 0L, limits.MaximumRecords))
                    return CanonicalizationFailure.RecordLimit;
                foreach (FloorRouteEdge edge in layout?.Edges ?? Array.Empty<FloorRouteEdge>())
                    if (!TryAdd(ref tiles, edge?.Footprint?.OccupiedTiles?.LongLength ?? 0L,
                        limits.MaximumMaterializedTiles)) return CanonicalizationFailure.TileLimit;
            }

            SavedSpatialFloor[] copies = new SavedSpatialFloor[floors.Length];
            for (int index = 0; index < floors.Length; index++) copies[index] = CopyFloor(floors[index]);
            canonical = new DetachedCanonicalSpatialSaveState
            {
                Authority = CopyMarker(source.Authority),
                Floors = copies.OrderBy(floor => floor?.FloorIndex ?? 0)
                    .ThenBy(floor => floor?.FloorInstanceId, StringComparer.Ordinal).ToArray(),
                LifecycleAndOwnership = includeLifecycle ? CopyLifecycle(source.LifecycleAndOwnership) : null
            };
            return CanonicalizationFailure.None;
        }

        private static StructuralLifecycleAndOwnershipState CopyLifecycle(
            StructuralLifecycleAndOwnershipState source)
        {
            if (source == null) return null;
            return new StructuralLifecycleAndOwnershipState
            {
                Floors = (source.Floors ?? Array.Empty<FloorStructuralIdentityLifecycle>())
                    .Select(value => value == null ? null : new FloorStructuralIdentityLifecycle
                    {
                        FloorInstanceId = value.FloorInstanceId,
                        NextNativeRoomOrdinal = value.NextNativeRoomOrdinal,
                        NextNativeEdgeOrdinal = value.NextNativeEdgeOrdinal
                    }).OrderBy(value => value?.FloorInstanceId, StringComparer.Ordinal).ToArray(),
                ReturnedContents = (source.ReturnedContents ?? Array.Empty<ReturnedStructuralContent>())
                    .Select(value => value == null ? null : new ReturnedStructuralContent
                    {
                        AssignmentId = value.AssignmentId, CategoryId = value.CategoryId,
                        OptionId = value.OptionId, Sequence = value.Sequence,
                        RemovalDisposition = value.RemovalDisposition
                    }).OrderBy(value => value?.Sequence ?? 0L)
                    .ThenBy(value => value?.AssignmentId, StringComparer.Ordinal).ToArray()
            };
        }

        private static void ValidateLifecycleAndOwnership(DetachedCanonicalSpatialSaveState state,
            HashSet<string> assignedIdentities, List<CanonicalSpatialSaveValidationIssue> issues)
        {
            StructuralLifecycleAndOwnershipState owner = state.LifecycleAndOwnership;
            if (owner == null) { issues.Add(CanonicalSpatialSaveValidationIssue.MissingLifecycleAndOwnership); return; }
            FloorStructuralIdentityLifecycle[] values = owner.Floors ?? Array.Empty<FloorStructuralIdentityLifecycle>();
            if (values.Any(value => value == null || string.IsNullOrWhiteSpace(value.FloorInstanceId) ||
                    value.NextNativeRoomOrdinal < 0 || value.NextNativeEdgeOrdinal < 0))
                issues.Add(CanonicalSpatialSaveValidationIssue.InvalidIdentityLifecycle);
            if (values.Where(value => value != null).GroupBy(value => value.FloorInstanceId,
                    StringComparer.Ordinal).Any(group => group.Count() != 1))
                issues.Add(CanonicalSpatialSaveValidationIssue.DuplicateLifecycleFloor);
            SavedSpatialFloor[] floors = state.Floors ?? Array.Empty<SavedSpatialFloor>();
            if (values.Length != floors.Length || values.Any(value => value != null && !floors.Any(floor =>
                    floor != null && floor.FloorInstanceId == value.FloorInstanceId)) ||
                floors.Any(floor => floor != null && !values.Any(value => value != null &&
                    value.FloorInstanceId == floor.FloorInstanceId && value.NextNativeRoomOrdinal >=
                    NativeStructuralIdentity.DeriveNextNativeRoomOrdinal(floor))))
                issues.Add(CanonicalSpatialSaveValidationIssue.InvalidIdentityLifecycle);
            ReturnedStructuralContent[] returned = owner.ReturnedContents ?? Array.Empty<ReturnedStructuralContent>();
            if (returned.Where(value => value != null).GroupBy(value => value.AssignmentId,
                    StringComparer.Ordinal).Any(group => group.Count() != 1))
                issues.Add(CanonicalSpatialSaveValidationIssue.DuplicateReturnedIdentity);
            foreach (ReturnedStructuralContent value in returned)
            {
                if (value == null || !IsPersistentId(value.AssignmentId) || value.Sequence < 0 ||
                    value.RemovalDisposition != StructuralContentRemovalDisposition.ReturnToPlayerCustody ||
                    !DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.MvpDungeonPlacementIds.TryGetCategoryForOption(
                        value.OptionId, out string category) || category != value.CategoryId ||
                    (category != MonsterCategoryId && category != TrapCategoryId &&
                     category != LootNodeCategoryId))
                    issues.Add(CanonicalSpatialSaveValidationIssue.InvalidReturnedContent);
                else if (assignedIdentities.Contains(value.AssignmentId))
                    issues.Add(CanonicalSpatialSaveValidationIssue.AssignedAndReturnedIdentity);
            }
        }

        private static bool TryAdd(ref long total, long value, int maximum)
        {
            if (value < 0L || total < 0L || value > maximum || total > maximum - value) return false;
            total += value;
            return true;
        }

        private static SavedSpatialFloor CopyFloor(SavedSpatialFloor floor)
        {
            if (floor == null) return null;
            return new SavedSpatialFloor
            {
                FloorInstanceId = floor.FloorInstanceId,
                FloorDefinitionId = floor.FloorDefinitionId,
                FloorIndex = floor.FloorIndex,
                Layout = CopyLayout(floor.Layout),
                FixedStructures = (floor.FixedStructures ?? Array.Empty<SavedFixedSpatialStructure>()).Select(CopyFixed)
                    .OrderBy(value => value?.FixedStructureInstanceId, StringComparer.Ordinal).ToArray(),
                RoomContents = CopyContents(floor.RoomContents)
            };
        }

        private static FloorSpatialLayout CopyLayout(FloorSpatialLayout layout)
        {
            if (layout == null) return null;
            return new FloorSpatialLayout
            {
                FloorId = layout.FloorId,
                Rooms = (layout.Rooms ?? Array.Empty<RoomSpatialInstance>()).Select(CopyRoom)
                    .OrderBy(room => room?.RoomInstanceId, StringComparer.Ordinal).ToArray(),
                Nodes = (layout.Nodes ?? Array.Empty<FloorRouteNode>()).Select(CopyNode)
                    .OrderBy(node => node == null ? 0 : (int)node.Kind)
                    .ThenBy(node => node?.NodeId, StringComparer.Ordinal).ToArray(),
                Edges = (layout.Edges ?? Array.Empty<FloorRouteEdge>()).Select(CopyEdge)
                    .OrderBy(edge => edge == null ? 0 : (int)edge.Classification)
                    .ThenBy(edge => edge?.SourceNodeId, StringComparer.Ordinal)
                    .ThenBy(edge => edge?.DestinationNodeId, StringComparer.Ordinal)
                    .ThenBy(edge => edge?.EdgeId, StringComparer.Ordinal).ToArray()
            };
        }

        private static RoomSpatialInstance CopyRoom(RoomSpatialInstance room) => room == null ? null : new RoomSpatialInstance
        {
            RoomInstanceId = room.RoomInstanceId, RoomDefinitionId = room.RoomDefinitionId, FloorId = room.FloorId,
            Anchor = room.Anchor, Orientation = room.Orientation
        };

        private static FloorRouteNode CopyNode(FloorRouteNode node) => node == null ? null : new FloorRouteNode
        {
            NodeId = node.NodeId, FloorId = node.FloorId, Kind = node.Kind,
            RoomInstanceId = node.Kind != FloorRouteNodeKind.Room && node.RoomInstanceId == null
                ? string.Empty : node.RoomInstanceId
        };

        private static FloorRouteEdge CopyEdge(FloorRouteEdge edge)
        {
            if (edge == null) return null;
            ResolvedTileFootprint footprint = null;
            if (edge.Footprint != null)
            {
                TileCoordinate[] tiles = edge.Footprint.OccupiedTiles == null
                    ? Array.Empty<TileCoordinate>() : (TileCoordinate[])edge.Footprint.OccupiedTiles.Clone();
                Array.Sort(tiles);
                footprint = new ResolvedTileFootprint { OccupiedTiles = tiles };
            }
            return new FloorRouteEdge
            {
                EdgeId = edge.EdgeId,
                CorridorDefinitionId = edge.ConnectionKind == FloorRouteConnectionKind.DirectDoorway &&
                    string.IsNullOrEmpty(edge.CorridorDefinitionId) ? string.Empty : edge.CorridorDefinitionId,
                FloorId = edge.FloorId,
                SourceNodeId = edge.SourceNodeId, DestinationNodeId = edge.DestinationNodeId, Footprint = footprint,
                Classification = edge.Classification, OptionalBranchId = edge.OptionalBranchId ?? string.Empty,
                ConnectionKind = edge.ConnectionKind
            };
        }

        private static CanonicalSpatialAuthorityMarker CopyMarker(CanonicalSpatialAuthorityMarker marker) => marker == null ? null :
            new CanonicalSpatialAuthorityMarker { CanonicalLayoutContractVersion = marker.CanonicalLayoutContractVersion,
                CreationKind = marker.CreationKind, MigrationTransactionId = marker.MigrationTransactionId,
                MigrationDescriptorFingerprint = marker.MigrationDescriptorFingerprint };
        private static SavedFixedSpatialStructure CopyFixed(SavedFixedSpatialStructure value) => value == null ? null :
            new SavedFixedSpatialStructure { FixedStructureInstanceId = value.FixedStructureInstanceId,
                FixedStructureDefinitionId = value.FixedStructureDefinitionId, FloorInstanceId = value.FloorInstanceId,
                Anchor = value.Anchor, Orientation = value.Orientation, Kind = value.Kind };
        private static RoomContentAssignment CopyAssignment(RoomContentAssignment value) => value == null ? null :
            new RoomContentAssignment { AssignmentId = value.AssignmentId, RoomInstanceId = value.RoomInstanceId,
                CategoryId = value.CategoryId, OptionId = value.OptionId, Sequence = value.Sequence };
        private static CanonicalRoomSemantics CopySemantics(CanonicalRoomSemantics value) => value == null ? null :
            new CanonicalRoomSemantics { RoomInstanceId = value.RoomInstanceId,
                LegacyRoomOriginKind = value.LegacyRoomOriginKind };

        private static FloorRoomContentState CopyContents(FloorRoomContentState contents)
        {
            if (contents == null) return null;
            return new FloorRoomContentState
            {
                NextSequence = contents.NextSequence,
                Assignments = (contents.Assignments ?? Array.Empty<RoomContentAssignment>()).Select(CopyAssignment)
                    .OrderBy(value => value?.RoomInstanceId, StringComparer.Ordinal)
                    .ThenBy(value => CategoryRank(value?.CategoryId)).ThenBy(value => value?.Sequence ?? 0L)
                    .ThenBy(value => value?.AssignmentId, StringComparer.Ordinal)
                    .ThenBy(value => value?.OptionId, StringComparer.Ordinal).ToArray(),
                RoomSemantics = (contents.RoomSemantics ?? Array.Empty<CanonicalRoomSemantics>()).Select(CopySemantics)
                    .OrderBy(value => value?.RoomInstanceId, StringComparer.Ordinal).ToArray()
            };
        }

        private static void ValidateMarker(CanonicalSpatialAuthorityMarker marker,
            ICollection<CanonicalSpatialSaveValidationIssue> issues)
        {
            if (marker == null) { Add(issues, CanonicalSpatialSaveValidationIssue.MissingAuthority); return; }
            if (marker.CanonicalLayoutContractVersion <= 0) Add(issues, CanonicalSpatialSaveValidationIssue.InvalidLayoutContractVersion);
            if (!Enum.IsDefined(typeof(CanonicalSpatialCreationKind), marker.CreationKind)) Add(issues, CanonicalSpatialSaveValidationIssue.InvalidCreationKind);
            if (marker.CreationKind == CanonicalSpatialCreationKind.NativeCanonical &&
                (!string.IsNullOrEmpty(marker.MigrationTransactionId) || !string.IsNullOrEmpty(marker.MigrationDescriptorFingerprint)))
                Add(issues, CanonicalSpatialSaveValidationIssue.NativeMarkerHasMigrationIdentity);
            if (!string.IsNullOrEmpty(marker.MigrationTransactionId)) CheckId(marker.MigrationTransactionId, issues);
            if (!string.IsNullOrEmpty(marker.MigrationDescriptorFingerprint)) CheckId(marker.MigrationDescriptorFingerprint, issues);
        }

        private static void ValidateFloor(SavedSpatialFloor floor, HashSet<string> instanceIds,
            ICollection<CanonicalSpatialSaveValidationIssue> issues)
        {
            if (floor == null) { Add(issues, CanonicalSpatialSaveValidationIssue.NullFloorRecord); return; }
            CheckInstanceId(floor.FloorInstanceId, instanceIds, issues); CheckId(floor.FloorDefinitionId, issues);
            if (floor.FloorIndex < 0) Add(issues, CanonicalSpatialSaveValidationIssue.NegativeFloorIndex);
            if (floor.Layout == null) { Add(issues, CanonicalSpatialSaveValidationIssue.MissingLayout); return; }
            CheckId(floor.Layout.FloorId, issues);
            if (!OrdinalEqual(floor.Layout.FloorId, floor.FloorInstanceId)) Add(issues, CanonicalSpatialSaveValidationIssue.FloorReferenceMismatch);

            RoomSpatialInstance[] rooms = floor.Layout.Rooms ?? Array.Empty<RoomSpatialInstance>();
            FloorRouteNode[] nodes = floor.Layout.Nodes ?? Array.Empty<FloorRouteNode>();
            FloorRouteEdge[] edges = floor.Layout.Edges ?? Array.Empty<FloorRouteEdge>();
            var roomIds = new HashSet<string>(StringComparer.Ordinal);
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (RoomSpatialInstance room in rooms)
            {
                if (room == null) { Add(issues, CanonicalSpatialSaveValidationIssue.NullRoomRecord); continue; }
                CheckInstanceId(room.RoomInstanceId, instanceIds, issues); roomIds.Add(room.RoomInstanceId);
                CheckId(room.RoomDefinitionId, issues); CheckFloorReference(room.FloorId, floor.FloorInstanceId, issues);
                if (!Enum.IsDefined(typeof(CardinalOrientation), room.Orientation)) Add(issues, CanonicalSpatialSaveValidationIssue.InvalidRoomOrientation);
            }
            foreach (FloorRouteNode node in nodes)
            {
                if (node == null) { Add(issues, CanonicalSpatialSaveValidationIssue.NullNodeRecord); continue; }
                CheckInstanceId(node.NodeId, instanceIds, issues); nodeIds.Add(node.NodeId);
                CheckFloorReference(node.FloorId, floor.FloorInstanceId, issues);
                if (!Enum.IsDefined(typeof(FloorRouteNodeKind), node.Kind)) Add(issues, CanonicalSpatialSaveValidationIssue.InvalidNodeKind);
                if (node.Kind == FloorRouteNodeKind.Room)
                {
                    CheckId(node.RoomInstanceId, issues);
                    if (!roomIds.Contains(node.RoomInstanceId)) Add(issues, CanonicalSpatialSaveValidationIssue.UnknownRoomReference);
                }
                else if (Enum.IsDefined(typeof(FloorRouteNodeKind), node.Kind) && !string.IsNullOrEmpty(node.RoomInstanceId))
                    Add(issues, CanonicalSpatialSaveValidationIssue.NonRoomNodeHasRoomReference);
            }
            foreach (FloorRouteEdge edge in edges)
            {
                if (edge == null) { Add(issues, CanonicalSpatialSaveValidationIssue.NullEdgeRecord); continue; }
                CheckInstanceId(edge.EdgeId, instanceIds, issues); CheckFloorReference(edge.FloorId, floor.FloorInstanceId, issues);
                CheckId(edge.SourceNodeId, issues); CheckId(edge.DestinationNodeId, issues);
                if (!nodeIds.Contains(edge.SourceNodeId)) Add(issues, CanonicalSpatialSaveValidationIssue.UnknownEdgeSource);
                if (!nodeIds.Contains(edge.DestinationNodeId)) Add(issues, CanonicalSpatialSaveValidationIssue.UnknownEdgeDestination);
                bool kindValid = Enum.IsDefined(typeof(FloorRouteConnectionKind), edge.ConnectionKind);
                bool classificationValid = Enum.IsDefined(typeof(RouteClassification), edge.Classification);
                if (!kindValid) Add(issues, CanonicalSpatialSaveValidationIssue.InvalidEdgeConnectionKind);
                if (!classificationValid) Add(issues, CanonicalSpatialSaveValidationIssue.InvalidEdgeClassification);
                if (!string.IsNullOrEmpty(edge.CorridorDefinitionId)) CheckId(edge.CorridorDefinitionId, issues);
                if (!string.IsNullOrEmpty(edge.OptionalBranchId)) CheckId(edge.OptionalBranchId, issues);
                if (edge.ConnectionKind == FloorRouteConnectionKind.DirectDoorway &&
                    (!string.IsNullOrEmpty(edge.CorridorDefinitionId) || edge.Footprint != null))
                    Add(issues, CanonicalSpatialSaveValidationIssue.InvalidDirectDoorwayShape);
                if (edge.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor && string.IsNullOrEmpty(edge.CorridorDefinitionId))
                    Add(issues, CanonicalSpatialSaveValidationIssue.MalformedPersistentId);
                if (edge.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor &&
                    (edge.Footprint == null || edge.Footprint.OccupiedTiles == null || edge.Footprint.OccupiedTiles.Length == 0))
                    Add(issues, CanonicalSpatialSaveValidationIssue.InvalidPhysicalCorridorShape);
                if (classificationValid && ((edge.Classification == RouteClassification.Required && !string.IsNullOrEmpty(edge.OptionalBranchId)) ||
                    (edge.Classification == RouteClassification.Optional && !IsPersistentId(edge.OptionalBranchId))))
                    Add(issues, CanonicalSpatialSaveValidationIssue.InvalidEdgeBranchShape);
            }

            foreach (SavedFixedSpatialStructure value in floor.FixedStructures ?? Array.Empty<SavedFixedSpatialStructure>())
            {
                if (value == null) { Add(issues, CanonicalSpatialSaveValidationIssue.NullFixedStructureRecord); continue; }
                CheckInstanceId(value.FixedStructureInstanceId, instanceIds, issues); CheckId(value.FixedStructureDefinitionId, issues);
                CheckFloorReference(value.FloorInstanceId, floor.FloorInstanceId, issues);
                if (!Enum.IsDefined(typeof(CardinalOrientation), value.Orientation)) Add(issues, CanonicalSpatialSaveValidationIssue.InvalidFixedStructureOrientation);
                if (!Enum.IsDefined(typeof(FixedSpatialStructureKind), value.Kind)) Add(issues, CanonicalSpatialSaveValidationIssue.InvalidFixedStructureKind);
            }
            ValidateContents(floor.RoomContents, roomIds, instanceIds, issues);
        }

        private static void ValidateContents(FloorRoomContentState contents, HashSet<string> roomIds,
            HashSet<string> instanceIds, ICollection<CanonicalSpatialSaveValidationIssue> issues)
        {
            if (contents == null) { Add(issues, CanonicalSpatialSaveValidationIssue.MissingRoomContents); return; }
            RoomContentAssignment[] assignments = contents.Assignments ?? Array.Empty<RoomContentAssignment>();
            var roomCategorySequences = new HashSet<Tuple<string, string, long>>();
            long maximum = -1L; bool hasAssignment = false;
            foreach (RoomContentAssignment value in assignments)
            {
                if (value == null) { Add(issues, CanonicalSpatialSaveValidationIssue.NullAssignmentRecord); continue; }
                hasAssignment = true;
                CheckInstanceId(value.AssignmentId, instanceIds, issues); CheckId(value.RoomInstanceId, issues);
                CheckId(value.OptionId, issues);
                if (!roomIds.Contains(value.RoomInstanceId)) Add(issues, CanonicalSpatialSaveValidationIssue.FloorReferenceMismatch);
                if (CategoryRank(value.CategoryId) > 2) Add(issues, CanonicalSpatialSaveValidationIssue.InvalidContentCategory);
                if (value.Sequence < 0) Add(issues, CanonicalSpatialSaveValidationIssue.NegativeSequence);
                maximum = Math.Max(maximum, value.Sequence);
                var tuple = Tuple.Create(value.RoomInstanceId, value.CategoryId, value.Sequence);
                if (!roomCategorySequences.Add(tuple)) Add(issues, CanonicalSpatialSaveValidationIssue.DuplicateRoomCategorySequence);
            }
            if (contents.NextSequence < 0 || (hasAssignment && contents.NextSequence <= maximum))
                Add(issues, CanonicalSpatialSaveValidationIssue.InvalidNextSequence);

            CanonicalRoomSemantics[] semantics = contents.RoomSemantics ?? Array.Empty<CanonicalRoomSemantics>();
            var semanticIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (CanonicalRoomSemantics value in semantics)
            {
                if (value == null) { Add(issues, CanonicalSpatialSaveValidationIssue.NullRoomSemanticsRecord); continue; }
                CheckId(value.RoomInstanceId, issues);
                if (!semanticIds.Add(value.RoomInstanceId)) Add(issues, CanonicalSpatialSaveValidationIssue.DuplicateRoomSemantics);
                if (!roomIds.Contains(value.RoomInstanceId)) Add(issues, CanonicalSpatialSaveValidationIssue.UnknownRoomSemantics);
                if (!Enum.IsDefined(typeof(LegacyRoomOriginKind), value.LegacyRoomOriginKind)) Add(issues, CanonicalSpatialSaveValidationIssue.InvalidRoomOriginKind);
            }
            if (roomIds.Any(roomId => !semanticIds.Contains(roomId))) Add(issues, CanonicalSpatialSaveValidationIssue.MissingRoomSemantics);
        }

        private static void CheckInstanceId(string value, HashSet<string> instanceIds,
            ICollection<CanonicalSpatialSaveValidationIssue> issues)
        {
            CheckId(value, issues);
            if (!string.IsNullOrEmpty(value) && !instanceIds.Add(value)) Add(issues, CanonicalSpatialSaveValidationIssue.CandidateInstanceIdCollision);
        }
        private static void CheckFloorReference(string value, string owner, ICollection<CanonicalSpatialSaveValidationIssue> issues)
        { CheckId(value, issues); if (!OrdinalEqual(value, owner)) Add(issues, CanonicalSpatialSaveValidationIssue.FloorReferenceMismatch); }
        private static void CheckId(string value, ICollection<CanonicalSpatialSaveValidationIssue> issues)
        { if (!IsPersistentId(value)) Add(issues, CanonicalSpatialSaveValidationIssue.MalformedPersistentId); }
        private static bool OrdinalEqual(string left, string right) => string.Equals(left, right, StringComparison.Ordinal);
        private static void Add(ICollection<CanonicalSpatialSaveValidationIssue> issues, CanonicalSpatialSaveValidationIssue issue)
        { issues.Add(issue); }
        private static int CategoryRank(string category) => category == MonsterCategoryId ? 0 : category == TrapCategoryId ? 1 : category == LootNodeCategoryId ? 2 : 3;
        private static bool IsPersistentId(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            bool separator = true;
            foreach (char character in value)
            {
                bool atom = character >= 'a' && character <= 'z' || character >= '0' && character <= '9';
                if (atom) { separator = false; continue; }
                if ((character == '.' || character == '_' || character == '-') && !separator) { separator = true; continue; }
                return false;
            }
            return !separator;
        }
        private static CanonicalSpatialSaveValidationIssue FailureIssue(CanonicalizationFailure failure)
        {
            switch (failure)
            {
                case CanonicalizationFailure.InvalidSource: return CanonicalSpatialSaveValidationIssue.InvalidSource;
                case CanonicalizationFailure.InvalidLimits: return CanonicalSpatialSaveValidationIssue.InvalidWorkloadLimits;
                case CanonicalizationFailure.RecordLimit: return CanonicalSpatialSaveValidationIssue.RecordLimitExceeded;
                default: return CanonicalSpatialSaveValidationIssue.MaterializedTileLimitExceeded;
            }
        }
        private static bool HasCanonicalOrdering(DetachedCanonicalSpatialSaveState state)
        {
            if (state.Floors == null) return false;
            for (int index = 1; index < state.Floors.Length; index++)
            {
                SavedSpatialFloor previous = state.Floors[index - 1], current = state.Floors[index];
                int indexOrder = (previous?.FloorIndex ?? 0).CompareTo(current?.FloorIndex ?? 0);
                if (indexOrder > 0 || indexOrder == 0 && StringComparer.Ordinal.Compare(
                    previous?.FloorInstanceId, current?.FloorInstanceId) > 0) return false;
            }
            foreach (SavedSpatialFloor floor in state.Floors.Where(value => value != null))
            {
                if (floor.Layout == null || floor.Layout.Rooms == null || floor.Layout.Nodes == null ||
                    floor.Layout.Edges == null || floor.FixedStructures == null || floor.RoomContents == null ||
                    floor.RoomContents.Assignments == null || floor.RoomContents.RoomSemantics == null) return false;
                if (!Ordered(floor.Layout.Rooms, (left, right) => StringComparer.Ordinal.Compare(left?.RoomInstanceId, right?.RoomInstanceId)) ||
                    !Ordered(floor.Layout.Nodes, CompareNodes) || !Ordered(floor.Layout.Edges, CompareEdges) ||
                    !Ordered(floor.FixedStructures, (left, right) => StringComparer.Ordinal.Compare(left?.FixedStructureInstanceId, right?.FixedStructureInstanceId)) ||
                    !Ordered(floor.RoomContents.Assignments, CompareAssignments) ||
                    !Ordered(floor.RoomContents.RoomSemantics, (left, right) => StringComparer.Ordinal.Compare(left?.RoomInstanceId, right?.RoomInstanceId))) return false;
                foreach (FloorRouteEdge edge in floor.Layout.Edges.Where(value => value?.Footprint?.OccupiedTiles != null))
                    if (!Ordered(edge.Footprint.OccupiedTiles, (left, right) => left.CompareTo(right))) return false;
            }
            if (state.LifecycleAndOwnership == null) return true;
            FloorStructuralIdentityLifecycle[] lifecycle = state.LifecycleAndOwnership.Floors ??
                Array.Empty<FloorStructuralIdentityLifecycle>();
            ReturnedStructuralContent[] returned = state.LifecycleAndOwnership?.ReturnedContents ??
                Array.Empty<ReturnedStructuralContent>();
            return IsOrdered(lifecycle, value => value?.FloorInstanceId) &&
                IsOrdered(returned, value => value == null ? null :
                    value.Sequence.ToString("D20", System.Globalization.CultureInfo.InvariantCulture) +
                    "\u0000" + value.AssignmentId);
        }

        private static bool IsOrdered<T>(T[] values, Func<T, string> key)
        {
            for (int index = 1; index < values.Length; index++)
                if (string.CompareOrdinal(key(values[index - 1]), key(values[index])) > 0) return false;
            return true;
        }

        private static bool Ordered<T>(T[] values, Comparison<T> comparison)
        {
            for (int index = 1; index < values.Length; index++)
                if (comparison(values[index - 1], values[index]) > 0) return false;
            return true;
        }
        private static int CompareNodes(FloorRouteNode left, FloorRouteNode right)
        {
            int kind = (left == null ? 0 : (int)left.Kind).CompareTo(right == null ? 0 : (int)right.Kind);
            return kind != 0 ? kind : StringComparer.Ordinal.Compare(left?.NodeId, right?.NodeId);
        }
        private static int CompareEdges(FloorRouteEdge left, FloorRouteEdge right)
        {
            int result = (left == null ? 0 : (int)left.Classification).CompareTo(right == null ? 0 : (int)right.Classification);
            if (result == 0) result = StringComparer.Ordinal.Compare(left?.SourceNodeId, right?.SourceNodeId);
            if (result == 0) result = StringComparer.Ordinal.Compare(left?.DestinationNodeId, right?.DestinationNodeId);
            return result != 0 ? result : StringComparer.Ordinal.Compare(left?.EdgeId, right?.EdgeId);
        }
        private static int CompareAssignments(RoomContentAssignment left, RoomContentAssignment right)
        {
            int result = StringComparer.Ordinal.Compare(left?.RoomInstanceId, right?.RoomInstanceId);
            if (result == 0) result = CategoryRank(left?.CategoryId).CompareTo(CategoryRank(right?.CategoryId));
            if (result == 0) result = (left?.Sequence ?? 0L).CompareTo(right?.Sequence ?? 0L);
            if (result == 0) result = StringComparer.Ordinal.Compare(left?.AssignmentId, right?.AssignmentId);
            return result != 0 ? result : StringComparer.Ordinal.Compare(left?.OptionId, right?.OptionId);
        }
    }
}
