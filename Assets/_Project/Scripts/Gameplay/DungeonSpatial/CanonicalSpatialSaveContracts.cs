using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum CanonicalSpatialCreationKind
    {
        NativeCanonical = 1,
        Migrated = 2
    }

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

    // Detached holder only. It is intentionally not a member of SaveData or SaveRoot.
    [Serializable]
    public sealed class DetachedCanonicalSpatialSaveState
    {
        public CanonicalSpatialAuthorityMarker Authority;
        public SavedSpatialFloor[] Floors = Array.Empty<SavedSpatialFloor>();
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
        WorkloadExceeded = 1,
        MissingAuthority = 2,
        InvalidLayoutContractVersion = 3,
        InvalidCreationKind = 4,
        NativeMarkerHasMigrationIdentity = 5,
        MalformedPersistentId = 6,
        DuplicateFloorBinding = 7,
        MissingLayout = 8,
        FloorReferenceMismatch = 9,
        DuplicateFixedStructure = 10,
        InvalidFixedStructureKind = 11,
        MissingRoomContents = 12,
        DuplicateAssignment = 13,
        DuplicateRoomCategorySequence = 14,
        InvalidContentCategory = 15,
        NegativeSequence = 16,
        InvalidNextSequence = 17,
        MissingRoomSemantics = 18,
        DuplicateRoomSemantics = 19,
        UnknownRoomSemantics = 20,
        InvalidRoomOriginKind = 21,
        NonCanonicalOrdering = 22
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

        public static bool TryCanonicalize(DetachedCanonicalSpatialSaveState source,
            CanonicalSpatialSaveWorkloadLimits limits, out DetachedCanonicalSpatialSaveState canonical)
        {
            canonical = null;
            if (source == null || !limits.IsValid) return false;
            SavedSpatialFloor[] floors = source.Floors ?? Array.Empty<SavedSpatialFloor>();
            long records = floors.LongLength;
            long tiles = 0L;
            foreach (SavedSpatialFloor floor in floors)
            {
                if (floor == null) continue;
                FloorSpatialLayout layout = floor.Layout;
                records += (layout?.Rooms?.LongLength ?? 0L) + (layout?.Nodes?.LongLength ?? 0L) +
                    (layout?.Edges?.LongLength ?? 0L) + (floor.FixedStructures?.LongLength ?? 0L) +
                    (floor.RoomContents?.Assignments?.LongLength ?? 0L) +
                    (floor.RoomContents?.RoomSemantics?.LongLength ?? 0L);
                foreach (FloorRouteEdge edge in layout?.Edges ?? Array.Empty<FloorRouteEdge>())
                    tiles += edge?.Footprint?.OccupiedTiles?.LongLength ?? 0L;
                if (records > limits.MaximumRecords || tiles > limits.MaximumMaterializedTiles) return false;
            }

            var tileLimits = new SpatialValidationWorkloadLimits(limits.MaximumMaterializedTiles);
            var copiedFloors = new SavedSpatialFloor[floors.Length];
            for (int index = 0; index < floors.Length; index++)
                if (!TryCopyFloor(floors[index], tileLimits, out copiedFloors[index])) return false;

            canonical = new DetachedCanonicalSpatialSaveState
            {
                Authority = CopyMarker(source.Authority),
                Floors = copiedFloors.OrderBy(floor => floor?.FloorIndex ?? 0)
                    .ThenBy(floor => floor?.FloorInstanceId, StringComparer.Ordinal).ToArray()
            };
            return true;
        }

        public static CanonicalSpatialSaveValidationResult Validate(DetachedCanonicalSpatialSaveState state,
            CanonicalSpatialSaveWorkloadLimits limits, bool requireCanonicalOrdering = false)
        {
            var issues = new List<CanonicalSpatialSaveValidationIssue>();
            if (!TryCanonicalize(state, limits, out DetachedCanonicalSpatialSaveState canonical))
            {
                issues.Add(CanonicalSpatialSaveValidationIssue.WorkloadExceeded);
                return new CanonicalSpatialSaveValidationResult(issues);
            }

            ValidateMarker(state.Authority, issues);
            SavedSpatialFloor[] floors = state.Floors ?? Array.Empty<SavedSpatialFloor>();
            if (floors.Where(x => x != null).GroupBy(x => x.FloorIndex).Any(group => group.Count() > 1) ||
                floors.Where(x => x != null).GroupBy(x => x.FloorInstanceId, StringComparer.Ordinal).Any(group => group.Count() > 1))
                issues.Add(CanonicalSpatialSaveValidationIssue.DuplicateFloorBinding);
            foreach (SavedSpatialFloor floor in floors) ValidateFloor(floor, issues);
            if (requireCanonicalOrdering && !SemanticEquals(state, canonical))
                issues.Add(CanonicalSpatialSaveValidationIssue.NonCanonicalOrdering);
            return new CanonicalSpatialSaveValidationResult(issues);
        }

        private static bool TryCopyFloor(SavedSpatialFloor floor, SpatialValidationWorkloadLimits limits,
            out SavedSpatialFloor copy)
        {
            copy = null;
            if (floor == null) return true;
            if (floor.Layout != null && !floor.Layout.TryCanonicalize(limits, out FloorSpatialLayout layout)) return false;
            else if (floor.Layout == null) layout = null;
            copy = new SavedSpatialFloor
            {
                FloorInstanceId = floor.FloorInstanceId,
                FloorDefinitionId = floor.FloorDefinitionId,
                FloorIndex = floor.FloorIndex,
                Layout = layout,
                FixedStructures = (floor.FixedStructures ?? Array.Empty<SavedFixedSpatialStructure>())
                    .Select(CopyFixed).OrderBy(x => x?.FixedStructureInstanceId, StringComparer.Ordinal).ToArray(),
                RoomContents = CopyContents(floor.RoomContents)
            };
            return true;
        }

        private static CanonicalSpatialAuthorityMarker CopyMarker(CanonicalSpatialAuthorityMarker marker) => marker == null ? null :
            new CanonicalSpatialAuthorityMarker { CanonicalLayoutContractVersion = marker.CanonicalLayoutContractVersion,
                CreationKind = marker.CreationKind, MigrationTransactionId = marker.MigrationTransactionId,
                MigrationDescriptorFingerprint = marker.MigrationDescriptorFingerprint };

        private static SavedFixedSpatialStructure CopyFixed(SavedFixedSpatialStructure value) => value == null ? null :
            new SavedFixedSpatialStructure { FixedStructureInstanceId = value.FixedStructureInstanceId,
                FixedStructureDefinitionId = value.FixedStructureDefinitionId, FloorInstanceId = value.FloorInstanceId,
                Anchor = value.Anchor, Orientation = value.Orientation, Kind = value.Kind };

        private static FloorRoomContentState CopyContents(FloorRoomContentState contents)
        {
            if (contents == null) return null;
            return new FloorRoomContentState
            {
                NextSequence = contents.NextSequence,
                Assignments = (contents.Assignments ?? Array.Empty<RoomContentAssignment>()).Select(CopyAssignment)
                    .OrderBy(x => x?.RoomInstanceId, StringComparer.Ordinal).ThenBy(x => CategoryRank(x?.CategoryId))
                    .ThenBy(x => x?.Sequence ?? 0L).ThenBy(x => x?.AssignmentId, StringComparer.Ordinal)
                    .ThenBy(x => x?.OptionId, StringComparer.Ordinal).ToArray(),
                RoomSemantics = (contents.RoomSemantics ?? Array.Empty<CanonicalRoomSemantics>()).Select(CopySemantics)
                    .OrderBy(x => x?.RoomInstanceId, StringComparer.Ordinal).ToArray()
            };
        }

        private static RoomContentAssignment CopyAssignment(RoomContentAssignment value) => value == null ? null :
            new RoomContentAssignment { AssignmentId = value.AssignmentId, RoomInstanceId = value.RoomInstanceId,
                CategoryId = value.CategoryId, OptionId = value.OptionId, Sequence = value.Sequence };
        private static CanonicalRoomSemantics CopySemantics(CanonicalRoomSemantics value) => value == null ? null :
            new CanonicalRoomSemantics { RoomInstanceId = value.RoomInstanceId, LegacyRoomOriginKind = value.LegacyRoomOriginKind };

        private static void ValidateMarker(CanonicalSpatialAuthorityMarker marker,
            ICollection<CanonicalSpatialSaveValidationIssue> issues)
        {
            if (marker == null) { issues.Add(CanonicalSpatialSaveValidationIssue.MissingAuthority); return; }
            if (marker.CanonicalLayoutContractVersion <= 0) issues.Add(CanonicalSpatialSaveValidationIssue.InvalidLayoutContractVersion);
            if (marker.CreationKind != CanonicalSpatialCreationKind.NativeCanonical && marker.CreationKind != CanonicalSpatialCreationKind.Migrated)
                issues.Add(CanonicalSpatialSaveValidationIssue.InvalidCreationKind);
            if (marker.CreationKind == CanonicalSpatialCreationKind.NativeCanonical &&
                (!string.IsNullOrEmpty(marker.MigrationTransactionId) || !string.IsNullOrEmpty(marker.MigrationDescriptorFingerprint)))
                issues.Add(CanonicalSpatialSaveValidationIssue.NativeMarkerHasMigrationIdentity);
            CheckOptionalId(marker.MigrationTransactionId, issues);
            CheckOptionalId(marker.MigrationDescriptorFingerprint, issues);
        }

        private static void ValidateFloor(SavedSpatialFloor floor, ICollection<CanonicalSpatialSaveValidationIssue> issues)
        {
            if (floor == null) { issues.Add(CanonicalSpatialSaveValidationIssue.MalformedPersistentId); return; }
            CheckId(floor.FloorInstanceId, issues); CheckId(floor.FloorDefinitionId, issues);
            if (floor.Layout == null) { issues.Add(CanonicalSpatialSaveValidationIssue.MissingLayout); return; }
            if (!string.Equals(floor.Layout.FloorId, floor.FloorInstanceId, StringComparison.Ordinal))
                issues.Add(CanonicalSpatialSaveValidationIssue.FloorReferenceMismatch);
            var rooms = floor.Layout.Rooms ?? Array.Empty<RoomSpatialInstance>();
            var roomIds = new HashSet<string>(rooms.Where(x => x != null).Select(x => x.RoomInstanceId), StringComparer.Ordinal);
            foreach (RoomSpatialInstance room in rooms) { if (room == null) continue; CheckId(room.RoomInstanceId, issues); CheckId(room.RoomDefinitionId, issues); if (room.FloorId != floor.FloorInstanceId) issues.Add(CanonicalSpatialSaveValidationIssue.FloorReferenceMismatch); }
            foreach (FloorRouteNode node in floor.Layout.Nodes ?? Array.Empty<FloorRouteNode>()) { if (node == null) continue; CheckId(node.NodeId, issues); if (node.FloorId != floor.FloorInstanceId) issues.Add(CanonicalSpatialSaveValidationIssue.FloorReferenceMismatch); if (node.Kind == FloorRouteNodeKind.Room && !roomIds.Contains(node.RoomInstanceId)) issues.Add(CanonicalSpatialSaveValidationIssue.FloorReferenceMismatch); }
            foreach (FloorRouteEdge edge in floor.Layout.Edges ?? Array.Empty<FloorRouteEdge>()) { if (edge == null) continue; CheckId(edge.EdgeId, issues); if (edge.FloorId != floor.FloorInstanceId) issues.Add(CanonicalSpatialSaveValidationIssue.FloorReferenceMismatch); }
            SavedFixedSpatialStructure[] fixedStructures = floor.FixedStructures ?? Array.Empty<SavedFixedSpatialStructure>();
            if (fixedStructures.Where(x => x != null).GroupBy(x => x.FixedStructureInstanceId, StringComparer.Ordinal).Any(x => x.Count() > 1)) issues.Add(CanonicalSpatialSaveValidationIssue.DuplicateFixedStructure);
            foreach (SavedFixedSpatialStructure value in fixedStructures) { if (value == null) continue; CheckId(value.FixedStructureInstanceId, issues); CheckId(value.FixedStructureDefinitionId, issues); if (value.FloorInstanceId != floor.FloorInstanceId) issues.Add(CanonicalSpatialSaveValidationIssue.FloorReferenceMismatch); if (value.Kind != FixedSpatialStructureKind.Entrance && value.Kind != FixedSpatialStructureKind.CompletionTerminal) issues.Add(CanonicalSpatialSaveValidationIssue.InvalidFixedStructureKind); }
            ValidateContents(floor.RoomContents, roomIds, issues);
        }

        private static void ValidateContents(FloorRoomContentState contents, HashSet<string> roomIds,
            ICollection<CanonicalSpatialSaveValidationIssue> issues)
        {
            if (contents == null) { issues.Add(CanonicalSpatialSaveValidationIssue.MissingRoomContents); return; }
            RoomContentAssignment[] assignments = contents.Assignments ?? Array.Empty<RoomContentAssignment>();
            if (assignments.Where(x => x != null).GroupBy(x => x.AssignmentId, StringComparer.Ordinal).Any(x => x.Count() > 1)) issues.Add(CanonicalSpatialSaveValidationIssue.DuplicateAssignment);
            if (assignments.Where(x => x != null).GroupBy(x => Tuple.Create(x.RoomInstanceId, x.CategoryId, x.Sequence)).Any(x => x.Count() > 1)) issues.Add(CanonicalSpatialSaveValidationIssue.DuplicateRoomCategorySequence);
            long maximum = -1;
            foreach (RoomContentAssignment value in assignments) { if (value == null) continue; CheckId(value.AssignmentId, issues); CheckId(value.RoomInstanceId, issues); CheckId(value.OptionId, issues); if (!roomIds.Contains(value.RoomInstanceId)) issues.Add(CanonicalSpatialSaveValidationIssue.FloorReferenceMismatch); if (CategoryRank(value.CategoryId) > 2) issues.Add(CanonicalSpatialSaveValidationIssue.InvalidContentCategory); if (value.Sequence < 0) issues.Add(CanonicalSpatialSaveValidationIssue.NegativeSequence); maximum = Math.Max(maximum, value.Sequence); }
            if (contents.NextSequence < 0 || (assignments.Any(x => x != null) && contents.NextSequence <= maximum)) issues.Add(CanonicalSpatialSaveValidationIssue.InvalidNextSequence);
            CanonicalRoomSemantics[] semantics = contents.RoomSemantics ?? Array.Empty<CanonicalRoomSemantics>();
            if (semantics.Where(x => x != null).GroupBy(x => x.RoomInstanceId, StringComparer.Ordinal).Any(x => x.Count() > 1)) issues.Add(CanonicalSpatialSaveValidationIssue.DuplicateRoomSemantics);
            var semanticIds = new HashSet<string>(semantics.Where(x => x != null).Select(x => x.RoomInstanceId), StringComparer.Ordinal);
            if (roomIds.Any(id => !semanticIds.Contains(id))) issues.Add(CanonicalSpatialSaveValidationIssue.MissingRoomSemantics);
            foreach (CanonicalRoomSemantics value in semantics) { if (value == null) continue; CheckId(value.RoomInstanceId, issues); if (!roomIds.Contains(value.RoomInstanceId)) issues.Add(CanonicalSpatialSaveValidationIssue.UnknownRoomSemantics); if (value.LegacyRoomOriginKind != LegacyRoomOriginKind.MigratedExplicitLegacyRoom && value.LegacyRoomOriginKind != LegacyRoomOriginKind.ImplicitCompatibilityContainer && value.LegacyRoomOriginKind != LegacyRoomOriginKind.CanonicalPlayerPlaced) issues.Add(CanonicalSpatialSaveValidationIssue.InvalidRoomOriginKind); }
        }

        private static int CategoryRank(string category) => category == MonsterCategoryId ? 0 : category == TrapCategoryId ? 1 : category == LootNodeCategoryId ? 2 : 3;
        private static void CheckOptionalId(string value, ICollection<CanonicalSpatialSaveValidationIssue> issues) { if (!string.IsNullOrEmpty(value)) CheckId(value, issues); }
        private static void CheckId(string value, ICollection<CanonicalSpatialSaveValidationIssue> issues) { if (!IsPersistentId(value)) issues.Add(CanonicalSpatialSaveValidationIssue.MalformedPersistentId); }
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

        private static bool SemanticEquals(DetachedCanonicalSpatialSaveState left, DetachedCanonicalSpatialSaveState right)
        {
            return string.Equals(UnityEngine.JsonUtility.ToJson(left), UnityEngine.JsonUtility.ToJson(right), StringComparison.Ordinal);
        }
    }
}
