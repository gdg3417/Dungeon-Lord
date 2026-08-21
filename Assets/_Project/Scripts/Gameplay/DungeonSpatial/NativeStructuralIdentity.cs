using System;
using System.Globalization;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    /// <summary>Phase 3A structural-only room identity allocation. Deletion is not supported.</summary>
    public static class NativeStructuralIdentity
    {
        public const string InvalidIdentityReason = "structural.edit.invalid_identity";
        private const string RoomSegment = ".room.player.";
        private const int MaximumOrdinal = 9999;

        public static bool TryAllocateRoomId(DetachedCanonicalSpatialSaveState state, string targetFloorId,
            out string roomInstanceId,
            out string reason)
        {
            roomInstanceId = null;
            reason = InvalidIdentityReason;
            if (state?.Floors == null || string.IsNullOrWhiteSpace(targetFloorId))
                return false;

            SavedSpatialFloor target = null;
            string prefix = targetFloorId + RoomSegment;
            int maximum = -1;
            var identities = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (SavedSpatialFloor floor in state.Floors)
            {
                if (floor == null || floor.Layout == null || !Add(identities, floor.FloorInstanceId)) return false;
                if (floor.FloorInstanceId == targetFloorId)
                { if (target != null) return false; target = floor; }
                foreach (RoomSpatialInstance room in floor.Layout.Rooms ?? Array.Empty<RoomSpatialInstance>())
                {
                    if (room == null || !Add(identities, room.RoomInstanceId)) return false;
                    if (floor != target || !room.RoomInstanceId.StartsWith(prefix, StringComparison.Ordinal)) continue;
                    string suffix = room.RoomInstanceId.Substring(prefix.Length);
                    if (suffix.Length != 4 || !int.TryParse(suffix, NumberStyles.None,
                            CultureInfo.InvariantCulture, out int ordinal) || ordinal > MaximumOrdinal) return false;
                    if (ordinal > maximum) maximum = ordinal;
                }
                foreach (FloorRouteNode node in floor.Layout.Nodes ?? Array.Empty<FloorRouteNode>())
                    if (node == null || !Add(identities, node.NodeId)) return false;
                foreach (FloorRouteEdge edge in floor.Layout.Edges ?? Array.Empty<FloorRouteEdge>())
                    if (edge == null || !Add(identities, edge.EdgeId)) return false;
                foreach (SavedFixedSpatialStructure fixedStructure in floor.FixedStructures ??
                    Array.Empty<SavedFixedSpatialStructure>())
                    if (fixedStructure == null || !Add(identities, fixedStructure.FixedStructureInstanceId)) return false;
                foreach (RoomContentAssignment assignment in floor.RoomContents?.Assignments ??
                    Array.Empty<RoomContentAssignment>())
                    if (assignment == null || !Add(identities, assignment.AssignmentId)) return false;
            }

            if (target == null || maximum >= MaximumOrdinal) return false;
            roomInstanceId = prefix + (maximum + 1).ToString("D4", CultureInfo.InvariantCulture);
            if (identities.Contains(roomInstanceId)) { roomInstanceId = null; return false; }
            reason = null;
            return true;
        }

        private static bool Add(System.Collections.Generic.HashSet<string> identities, string value) =>
            !string.IsNullOrWhiteSpace(value) && identities.Add(value);
    }
}
