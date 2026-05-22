using System;
using DungeonBuilder.M0.Gameplay.DungeonLayout;

namespace DungeonBuilder.M0.Save
{
    [Serializable]
    public sealed class SaveRoot
    {
        public int SchemaVersion = 1;
        public long Mana;
        public int HeatTier;

        public DungeonLayoutState Layout;
    }
}
