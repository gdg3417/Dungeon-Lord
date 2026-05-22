using System;

namespace DungeonBuilder.DungeonLayout
{
    [Serializable]
    public struct DungeonSlot : IEquatable<DungeonSlot>
    {
        public int FloorIndex;
        public int SlotIndex;
        public string StructureId;

        public DungeonSlot(int floorIndex, int slotIndex, string structureId)
        {
            FloorIndex = floorIndex;
            SlotIndex = slotIndex;
            StructureId = structureId ?? string.Empty;
        }

        public string StableKey => $"{FloorIndex:D2}:{SlotIndex:D3}";

        public bool Equals(DungeonSlot other)
        {
            return FloorIndex == other.FloorIndex
                && SlotIndex == other.SlotIndex
                && string.Equals(StructureId, other.StructureId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is DungeonSlot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + FloorIndex;
                hash = (hash * 31) + SlotIndex;
                hash = (hash * 31) + (StructureId?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
