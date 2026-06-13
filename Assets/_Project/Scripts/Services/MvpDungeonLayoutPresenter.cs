using System;
using System.Collections.Generic;
using System.Text;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0
{
    public static class MvpDungeonLayoutPresenter
    {
        public const string LayoutFormatKey = "ui.mvp_dungeon_layout.panel.layout_format";
        public const string FloorFormatKey = "ui.mvp_dungeon_layout.panel.floor_format";
        public const string AssignedNodeFormatKey = "ui.mvp_dungeon_layout.panel.assigned_node_format";
        public const string EmptyNodeFormatKey = "ui.mvp_dungeon_layout.panel.empty_node_format";
        public const string NodeSeparatorKey = "ui.mvp_dungeon_layout.panel.node_separator";
        public const string EmptyAvailableKey = "ui.mvp_dungeon_layout.value.empty_available";

        public static string BuildLayoutText(SaveData save, Func<string, string, string> localize)
        {
            MvpDungeonFloorLayoutState layout = save?.mvpDungeonFloorLayout;
            MvpDungeonPlacementState legacyPlacements = save?.mvpDungeonPlacements;
            MvpDungeonPlacementEntry[] resolvedPlacements = MvpDungeonLayoutResolver.ResolveOrderedPlacements(layout, legacyPlacements);
            var placementsByCategory = new Dictionary<string, MvpDungeonPlacementEntry>(StringComparer.Ordinal);
            foreach (MvpDungeonPlacementEntry placement in resolvedPlacements)
            {
                if (placement != null && !placementsByCategory.ContainsKey(placement.CategoryId))
                {
                    placementsByCategory.Add(placement.CategoryId, placement);
                }
            }

            var nodes = new StringBuilder();
            string separator = Localize(localize, NodeSeparatorKey);
            for (int nodeIndex = 0; nodeIndex < MvpDungeonPlacementIds.OrderedCategoryIds.Length; nodeIndex++)
            {
                string categoryId = MvpDungeonPlacementIds.OrderedCategoryIds[nodeIndex];
                if (nodes.Length > 0)
                {
                    nodes.Append(separator);
                }

                string categoryName = MvpDungeonPlacementPresenter.ResolveCategoryName(categoryId, localize);
                if (placementsByCategory.TryGetValue(categoryId, out MvpDungeonPlacementEntry placement))
                {
                    nodes.Append(string.Format(
                        Localize(localize, AssignedNodeFormatKey),
                        categoryName,
                        MvpDungeonPlacementPresenter.ResolveOptionName(placement.OptionId, localize)));
                }
                else
                {
                    nodes.Append(string.Format(
                        Localize(localize, EmptyNodeFormatKey),
                        categoryName,
                        Localize(localize, EmptyAvailableKey)));
                }
            }

            string floorText = string.Format(Localize(localize, FloorFormatKey), 0, nodes.ToString());
            return string.Format(Localize(localize, LayoutFormatKey), floorText);
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            return localize == null ? key : localize(key, key);
        }
    }
}
