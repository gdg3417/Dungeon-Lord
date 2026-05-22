using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonLayout
{
    [Serializable]
    public sealed class DungeonLayoutState
    {
        public int FloorCount;
        public int SlotsPerFloor;
        public List<DungeonSlot> Slots = new List<DungeonSlot>();

        public static DungeonLayoutState CreateEmpty(int floorCount, int slotsPerFloor)
        {
            var state = new DungeonLayoutState
            {
                FloorCount = floorCount,
                SlotsPerFloor = slotsPerFloor
            };

            for (var floor = 0; floor < floorCount; floor++)
            {
                for (var slot = 0; slot < slotsPerFloor; slot++)
                {
                    state.Slots.Add(new DungeonSlot(floor, slot, string.Empty));
                }
            }

            return state;
        }

        public IEnumerable<DungeonSlot> OrderedSlots()
        {
            return Slots.OrderBy(x => x.FloorIndex).ThenBy(x => x.SlotIndex);
        }
    }
}
