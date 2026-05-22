using System;
using DungeonBuilder.DungeonLayout;

namespace DungeonBuilder.Save
{
    [Serializable]
    public sealed class SaveRoot
    {
        // Sprint 1 fields remain unchanged and optional for compatibility.
        public int SchemaVersion = 1;
        public long Mana;
        public int HeatTier;

        // Sprint 2A PR-A extension.
        public DungeonLayoutState Layout;
    }
}
