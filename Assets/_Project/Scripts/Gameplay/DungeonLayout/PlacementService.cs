using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonLayout
{
    public sealed class PlacementService
    {
        public IReadOnlyList<DungeonSlot> GetPlacementOrder(DungeonLayoutState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return state.OrderedSlots().ToList();
        }

        public DungeonLayoutState PlaceStructure(
            DungeonLayoutState state,
            int floorIndex,
            int slotIndex,
            string structureId,
            bool allowReplace = false)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (string.IsNullOrWhiteSpace(structureId))
            {
                throw new ArgumentException("Structure ID is required.", nameof(structureId));
            }

            var targetIdx = state.Slots.FindIndex(s => s.FloorIndex == floorIndex && s.SlotIndex == slotIndex);
            if (targetIdx < 0)
            {
                throw new InvalidOperationException($"Unknown slot floor={floorIndex}, slot={slotIndex}");
            }

            var existing = state.Slots[targetIdx];
            if (existing.IsOccupied && !allowReplace)
            {
                throw new InvalidOperationException($"Slot already occupied floor={floorIndex}, slot={slotIndex}");
            }

            state.Slots[targetIdx] = new DungeonSlot(existing.FloorIndex, existing.SlotIndex, structureId);
            return state;
        }
    }
}
