using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.DungeonLayout
{
    public sealed class PlacementService
    {
        public IReadOnlyList<DungeonSlot> GetPlacementOrder(DungeonLayoutState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            // Deterministic ordering: floor ascending, slot ascending.
            return state.OrderedSlots().ToList();
        }

        public DungeonLayoutState PlaceStructure(
            DungeonLayoutState state,
            int floorIndex,
            int slotIndex,
            string structureId)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var targetIdx = state.Slots.FindIndex(s => s.FloorIndex == floorIndex && s.SlotIndex == slotIndex);
            if (targetIdx < 0)
            {
                throw new InvalidOperationException($"Unknown slot floor={floorIndex}, slot={slotIndex}");
            }

            var existing = state.Slots[targetIdx];
            state.Slots[targetIdx] = new DungeonSlot(existing.FloorIndex, existing.SlotIndex, structureId);
            return state;
        }
    }
}
