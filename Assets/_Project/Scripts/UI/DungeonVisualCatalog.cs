using System;
using System.Linq;
using UnityEngine;

namespace DungeonBuilder.M0
{
    [Serializable]
    public sealed class DungeonContentVisual
    {
        public string CategoryId, OptionId;
        public Sprite Sprite;
    }

    /// <summary>Cosmetics only. Stable identity is independent of array ordering and gameplay data.</summary>
    [CreateAssetMenu(menuName = "Dungeon Lord/Dungeon Visual Catalog")]
    public sealed class DungeonVisualCatalog : ScriptableObject
    {
        public Sprite[] RoomStone, Boundaries, SelectedEdges;
        public Sprite Corridor, Surrounding, Entrance, Terminal, Selection, Invalid, Grid, Anchor;
        public Sprite MonsterFallback, TrapFallback, LootFallback;
        public DungeonContentVisual[] Contents = Array.Empty<DungeonContentVisual>();
        public Sprite Resolve(string category, string option)
        {
            foreach (var value in Contents ?? Array.Empty<DungeonContentVisual>())
                if (value != null && value.CategoryId == category && value.OptionId == option && value.Sprite != null) return value.Sprite;
            return category == Gameplay.DungeonSpatial.CanonicalSpatialSaveContracts.TrapCategoryId ? TrapFallback :
                category == Gameplay.DungeonSpatial.CanonicalSpatialSaveContracts.LootNodeCategoryId ? LootFallback : MonsterFallback;
        }
        public bool IsComplete => RoomStone?.Length > 0 && RoomStone.All(v=>v!=null) && Boundaries?.Length == 16 && Boundaries.All(v=>v!=null) &&
            SelectedEdges?.Length==16 && SelectedEdges.All(v=>v!=null) && Corridor != null && Surrounding != null &&
            Entrance != null && Terminal != null && Selection != null && Invalid != null && Grid != null && Anchor != null &&
            MonsterFallback != null && TrapFallback != null && LootFallback != null;
    }
}
