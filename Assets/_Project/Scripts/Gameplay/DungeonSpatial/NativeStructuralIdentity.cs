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

        public static bool TryAllocateRoomId(SavedSpatialFloor floor, out string roomInstanceId,
            out string reason)
        {
            roomInstanceId = null;
            reason = InvalidIdentityReason;
            if (floor == null || string.IsNullOrWhiteSpace(floor.FloorInstanceId) || floor.Layout == null)
                return false;

            string prefix = floor.FloorInstanceId + RoomSegment;
            int maximum = -1;
            var identities = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (RoomSpatialInstance room in floor.Layout.Rooms ?? Array.Empty<RoomSpatialInstance>())
            {
                if (room == null || string.IsNullOrWhiteSpace(room.RoomInstanceId) ||
                    !identities.Add(room.RoomInstanceId)) return false;
                if (!room.RoomInstanceId.StartsWith(prefix, StringComparison.Ordinal)) continue;
                string suffix = room.RoomInstanceId.Substring(prefix.Length);
                if (suffix.Length != 4 || !int.TryParse(suffix, NumberStyles.None,
                        CultureInfo.InvariantCulture, out int ordinal) || ordinal < 0 || ordinal > MaximumOrdinal)
                    return false;
                if (ordinal > maximum) maximum = ordinal;
            }

            if (maximum >= MaximumOrdinal) return false;
            roomInstanceId = prefix + (maximum + 1).ToString("D4", CultureInfo.InvariantCulture);
            if (identities.Contains(roomInstanceId)) { roomInstanceId = null; return false; }
            reason = null;
            return true;
        }
    }
}
