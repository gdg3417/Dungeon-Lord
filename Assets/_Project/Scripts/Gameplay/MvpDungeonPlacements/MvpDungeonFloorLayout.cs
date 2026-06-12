using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.MvpDungeonPlacements
{
    [Serializable]
    public sealed class MvpDungeonNodeState
    {
        public int FloorIndex;
        public int NodeIndex;
        public string SlotId;
        public string CategoryId;
        public string OptionId;
        public int Revision;

        public MvpDungeonNodeState()
        {
        }

        public MvpDungeonNodeState(int floorIndex, int nodeIndex, string slotId, string categoryId, string optionId, int revision)
        {
            FloorIndex = floorIndex;
            NodeIndex = nodeIndex;
            SlotId = slotId ?? string.Empty;
            CategoryId = categoryId ?? string.Empty;
            OptionId = optionId ?? string.Empty;
            Revision = revision;
        }
    }

    [Serializable]
    public sealed class MvpDungeonFloorLayoutState
    {
        public List<MvpDungeonNodeState> Nodes = new List<MvpDungeonNodeState>();
        public int NextRevision = 1;

        public static MvpDungeonFloorLayoutState CreateEmptyStarterFloor()
        {
            var state = new MvpDungeonFloorLayoutState();
            for (int nodeIndex = 0; nodeIndex < MvpDungeonPlacementIds.OrderedCategoryIds.Length; nodeIndex++)
            {
                state.Nodes.Add(new MvpDungeonNodeState(0, nodeIndex, MvpDungeonLayoutResolver.ResolveSlotId(0, nodeIndex), string.Empty, string.Empty, 0));
            }

            return state;
        }
    }

    public static class MvpDungeonLayoutResolver
    {
        public static MvpDungeonPlacementEntry[] ResolveOrderedPlacements(
            MvpDungeonFloorLayoutState layout,
            MvpDungeonPlacementState legacyPlacements)
        {
            MvpDungeonPlacementEntry[] nodePlacements = ResolveOrderedNodePlacements(layout);
            return nodePlacements.Length > 0
                ? nodePlacements
                : ResolveOrderedLegacyPlacements(legacyPlacements);
        }

        public static MvpDungeonPlacementEntry[] ResolveOrderedNodePlacements(MvpDungeonFloorLayoutState layout)
        {
            if (layout == null || layout.Nodes == null || layout.Nodes.Count == 0)
            {
                return Array.Empty<MvpDungeonPlacementEntry>();
            }

            return layout.Nodes
                .Where(IsValidAssignedNode)
                .GroupBy(NodeKey, StringComparer.Ordinal)
                .Select(group => group
                    .OrderByDescending(node => node.Revision)
                    .ThenBy(node => CategoryOrder(node.CategoryId))
                    .ThenBy(node => node.CategoryId, StringComparer.Ordinal)
                    .ThenBy(node => node.OptionId, StringComparer.Ordinal)
                    .First())
                .OrderBy(node => node.FloorIndex)
                .ThenBy(node => node.NodeIndex)
                .ThenBy(node => node.Revision)
                .Select(node => new MvpDungeonPlacementEntry(node.CategoryId, node.OptionId, node.Revision))
                .ToArray();
        }

        public static MvpDungeonPlacementEntry[] ResolveOrderedLegacyPlacements(MvpDungeonPlacementState placements)
        {
            if (placements == null || placements.Entries == null || placements.Entries.Count == 0)
            {
                return Array.Empty<MvpDungeonPlacementEntry>();
            }

            return placements.Entries
                .Where(IsValidLegacyPlacement)
                .OrderBy(entry => CategoryOrder(entry.CategoryId))
                .ThenBy(entry => entry.Revision)
                .ToArray();
        }

        public static int ResolveNodeIndexForCategory(string categoryId)
        {
            int index = CategoryOrder(categoryId);
            return index < 0 ? 0 : index;
        }

        public static string ResolveSlotId(int floorIndex, int nodeIndex)
        {
            return $"mvp.floor.{floorIndex:D2}.node.{nodeIndex:D2}";
        }

        private static bool IsValidAssignedNode(MvpDungeonNodeState node)
        {
            return node != null &&
                   MvpDungeonPlacementIds.IsAllowedCategory(node.CategoryId) &&
                   MvpDungeonPlacementIds.IsAllowedOption(node.OptionId) &&
                   MvpDungeonPlacementIds.TryGetCategoryForOption(node.OptionId, out string optionCategoryId) &&
                   string.Equals(optionCategoryId, node.CategoryId, StringComparison.Ordinal);
        }

        private static bool IsValidLegacyPlacement(MvpDungeonPlacementEntry entry)
        {
            return entry != null &&
                   MvpDungeonPlacementIds.IsAllowedCategory(entry.CategoryId) &&
                   MvpDungeonPlacementIds.IsAllowedOption(entry.OptionId) &&
                   MvpDungeonPlacementIds.TryGetCategoryForOption(entry.OptionId, out string optionCategoryId) &&
                   string.Equals(optionCategoryId, entry.CategoryId, StringComparison.Ordinal);
        }

        private static string NodeKey(MvpDungeonNodeState node)
        {
            return $"{node.FloorIndex:D4}:{node.NodeIndex:D4}";
        }

        private static int CategoryOrder(string categoryId)
        {
            return Array.IndexOf(MvpDungeonPlacementIds.OrderedCategoryIds, categoryId);
        }
    }
}
