using System;
using System.Globalization;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class NativeRoomConstructionIdentity
    {
        internal NativeRoomConstructionIdentity(string room, string node, string incoming, string terminal)
        { RoomInstanceId = room; RoomNodeId = node; IncomingRequiredEdgeId = incoming;
          TerminalRequiredEdgeId = terminal; }
        public string RoomInstanceId { get; }
        public string RoomNodeId { get; }
        public string IncomingRequiredEdgeId { get; }
        public string TerminalRequiredEdgeId { get; }
    }

    /// <summary>Schema-8 monotonic structural identity allocation. Deletion is not supported here.</summary>
    public static class NativeStructuralIdentity
    {
        public const string InvalidIdentityReason = "structural.edit.invalid_identity";
        private const string RoomSegment = ".room.player.";
        private const int MaximumOrdinal = 9999;
        private const long MaximumEdgeOrdinal = 99999999L;

        public static bool TryAllocateRoomId(DetachedCanonicalSpatialSaveState state, string targetFloorId,
            out string roomInstanceId,
            out string reason)
        {
            bool success = TryAllocateConstructionIdentity(state, targetFloorId,
                out NativeRoomConstructionIdentity identity, out reason);
            roomInstanceId = identity?.RoomInstanceId;
            return success;
        }

        public static bool TryAllocateConstructionIdentity(DetachedCanonicalSpatialSaveState state,
            string targetFloorId, out NativeRoomConstructionIdentity identity, out string reason)
        {
            identity = null;
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

            FloorStructuralIdentityLifecycle[] lifecycleMatches = (state.LifecycleAndOwnership?.Floors ??
                Array.Empty<FloorStructuralIdentityLifecycle>()).Where(value => value != null &&
                value.FloorInstanceId == targetFloorId).ToArray();
            FloorStructuralIdentityLifecycle lifecycle = lifecycleMatches.Length == 1 ? lifecycleMatches[0] : null;
            if (target == null || lifecycle == null || lifecycle.NextNativeRoomOrdinal < 0 ||
                lifecycle.NextNativeRoomOrdinal < maximum + 1 || lifecycle.NextNativeRoomOrdinal > MaximumOrdinal)
                return false;
            string candidateRoomId = prefix + lifecycle.NextNativeRoomOrdinal.ToString("D4", CultureInfo.InvariantCulture);
            var proposed = new[] { candidateRoomId, candidateRoomId + ".node",
                candidateRoomId + ".edge.incoming", candidateRoomId + ".edge.terminal" };
            if (proposed.Any(value => !Persistent(value)) ||
                proposed.Distinct(StringComparer.Ordinal).Count() != proposed.Length ||
                proposed.Any(identities.Contains)) return false;
            identity = new NativeRoomConstructionIdentity(proposed[0], proposed[1], proposed[2], proposed[3]);
            reason = null;
            return true;
        }

        internal static int DeriveNextNativeRoomOrdinal(SavedSpatialFloor floor)
        {
            if (floor?.Layout?.Rooms == null || string.IsNullOrEmpty(floor.FloorInstanceId)) return 0;
            string prefix = floor.FloorInstanceId + RoomSegment;
            int maximum = -1;
            foreach (RoomSpatialInstance room in floor.Layout.Rooms)
            {
                string id = room?.RoomInstanceId;
                if (id == null || !id.StartsWith(prefix, StringComparison.Ordinal)) continue;
                string suffix = id.Substring(prefix.Length);
                if (suffix.Length == 4 && int.TryParse(suffix, NumberStyles.None,
                        CultureInfo.InvariantCulture, out int ordinal) && ordinal <= MaximumOrdinal)
                    maximum = Math.Max(maximum, ordinal);
            }
            return maximum + 1;
        }

        internal static StructuralLifecycleAndOwnershipState CreateInitialLifecycle(
            SavedSpatialFloor[] floors) => new StructuralLifecycleAndOwnershipState
        {
            Floors = (floors ?? Array.Empty<SavedSpatialFloor>()).Where(value => value != null)
                .Select(value => new FloorStructuralIdentityLifecycle
                {
                    FloorInstanceId = value.FloorInstanceId,
                    NextNativeRoomOrdinal = DeriveNextNativeRoomOrdinal(value),
                    NextNativeEdgeOrdinal = 0L
                }).OrderBy(value => value.FloorInstanceId, StringComparer.Ordinal).ToArray(),
            ReturnedContents = Array.Empty<ReturnedStructuralContent>()
        };

        /// <summary>
        /// Allocates independently minted edges (including future terminal reconnections). Existing
        /// room-derived construction edges do not consume this authority.
        /// </summary>
        public static bool TryAllocateFreshEdgeIdentity(DetachedCanonicalSpatialSaveState state,
            string targetFloorId, out string edgeId, out long nextOrdinal, out string reason)
        {
            edgeId = null; nextOrdinal = -1L; reason = InvalidIdentityReason;
            SavedSpatialFloor[] floorMatches = (state?.Floors ?? Array.Empty<SavedSpatialFloor>())
                .Where(value => value != null && value.FloorInstanceId == targetFloorId).ToArray();
            FloorStructuralIdentityLifecycle[] lifecycleMatches = (state?.LifecycleAndOwnership?.Floors ??
                Array.Empty<FloorStructuralIdentityLifecycle>()).Where(value => value != null &&
                value.FloorInstanceId == targetFloorId).ToArray();
            SavedSpatialFloor floor = floorMatches.Length == 1 ? floorMatches[0] : null;
            FloorStructuralIdentityLifecycle lifecycle = lifecycleMatches.Length == 1 ? lifecycleMatches[0] : null;
            if (floor == null || lifecycle == null || lifecycle.NextNativeEdgeOrdinal < 0L ||
                lifecycle.NextNativeEdgeOrdinal > MaximumEdgeOrdinal) return false;
            string candidate = targetFloorId + ".edge.native." + lifecycle.NextNativeEdgeOrdinal
                .ToString("D8", CultureInfo.InvariantCulture);
            var identities = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (SavedSpatialFloor value in state.Floors)
            {
                if (value == null || value.Layout == null || !Add(identities, value.FloorInstanceId)) return false;
                foreach (RoomSpatialInstance room in value.Layout.Rooms ?? Array.Empty<RoomSpatialInstance>())
                    if (room == null || !Add(identities, room.RoomInstanceId)) return false;
                foreach (FloorRouteNode node in value.Layout.Nodes ?? Array.Empty<FloorRouteNode>())
                    if (node == null || !Add(identities, node.NodeId)) return false;
                foreach (FloorRouteEdge edge in value.Layout.Edges ?? Array.Empty<FloorRouteEdge>())
                    if (edge == null || !Add(identities, edge.EdgeId)) return false;
                foreach (SavedFixedSpatialStructure fixedValue in value.FixedStructures ?? Array.Empty<SavedFixedSpatialStructure>())
                    if (fixedValue == null || !Add(identities, fixedValue.FixedStructureInstanceId)) return false;
                foreach (RoomContentAssignment assignment in value.RoomContents?.Assignments ?? Array.Empty<RoomContentAssignment>())
                    if (assignment == null || !Add(identities, assignment.AssignmentId)) return false;
            }
            foreach (ReturnedStructuralContent returned in state.LifecycleAndOwnership.ReturnedContents ??
                Array.Empty<ReturnedStructuralContent>())
                if (returned == null || !Add(identities, returned.AssignmentId)) return false;
            if (!Persistent(candidate) || identities.Contains(candidate)) return false;
            edgeId = candidate; nextOrdinal = lifecycle.NextNativeEdgeOrdinal + 1L; reason = null;
            return true;
        }

        private static bool Add(System.Collections.Generic.HashSet<string> identities, string value) =>
            !string.IsNullOrWhiteSpace(value) && identities.Add(value);

        private static bool Persistent(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            bool separator = true;
            foreach (char character in value)
            {
                bool alphaNumeric = character >= 'a' && character <= 'z' ||
                    character >= '0' && character <= '9';
                if (alphaNumeric) { separator = false; continue; }
                if ((character != '.' && character != '_' && character != '-') || separator) return false;
                separator = true;
            }
            return !separator;
        }
    }
}
