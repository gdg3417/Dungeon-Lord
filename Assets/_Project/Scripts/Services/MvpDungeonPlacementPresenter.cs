using System;
using System.Text;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0
{
    public static class MvpDungeonPlacementPresenter
    {
        public const string UnknownCategoryKey = "ui.mvp_label.placement_category.unknown";
        public const string UnknownOptionKey = "ui.mvp_label.placement_option.unknown";
        public const string EmptyCompositionKey = "ui.mvp_composition.empty";
        public const string EntryFormatKey = "ui.mvp_composition.entry_format";
        public const string SeparatorKey = "ui.mvp_composition.separator";
        public const string RoomCategoryKey = "placement.category.room.display_name";
        public const string MonsterCategoryKey = "placement.category.monster.display_name";
        public const string TrapCategoryKey = "placement.category.trap.display_name";
        public const string LootNodeCategoryKey = "placement.category.loot_node.display_name";
        public const string BasicRoomOptionKey = "placement.option.room.basic.display_name";
        public const string SkeletonOptionKey = "placement.option.monster.skeleton.display_name";
        public const string SpikeTrapOptionKey = "placement.option.trap.spike.display_name";
        public const string BasicLootNodeOptionKey = "placement.option.loot_node.basic.display_name";
        public const string BasicRoomPreviewKey = "ui.mvp_placement_preview.room.basic";
        public const string SkeletonPreviewKey = "ui.mvp_placement_preview.monster.skeleton";
        public const string SpikeTrapPreviewKey = "ui.mvp_placement_preview.trap.spike";
        public const string BasicLootNodePreviewKey = "ui.mvp_placement_preview.loot_node.basic";
        public const string UnknownPreviewKey = "ui.mvp_placement_preview.unknown";

        public static string ResolveCategoryName(string categoryId, Func<string, string, string> localize)
        {
            return TryGetCategoryNameKey(categoryId, out string key) ? Localize(localize, key) : Localize(localize, UnknownCategoryKey);
        }

        public static string ResolveOptionName(string optionId, Func<string, string, string> localize)
        {
            return TryGetOptionNameKey(optionId, out string key) ? Localize(localize, key) : Localize(localize, UnknownOptionKey);
        }

        public static string BuildPreviewText(string optionId, Func<string, string, string> localize)
        {
            return Localize(localize, ResolvePreviewKey(optionId));
        }

        public static string BuildCompositionText(MvpDungeonPlacementEntry[] entries, Func<string, string, string> localize)
        {
            if (entries == null || entries.Length == 0)
            {
                return Localize(localize, EmptyCompositionKey);
            }

            var builder = new StringBuilder();
            string separator = Localize(localize, SeparatorKey);
            string format = Localize(localize, EntryFormatKey);
            for (int i = 0; i < entries.Length; i++)
            {
                MvpDungeonPlacementEntry entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(separator);
                }

                builder.Append(string.Format(
                    format,
                    ResolveCategoryName(entry.CategoryId, localize),
                    ResolveOptionName(entry.OptionId, localize)));
            }

            return builder.Length > 0 ? builder.ToString() : Localize(localize, EmptyCompositionKey);
        }

        public static bool TryGetCategoryNameKey(string categoryId, out string key)
        {
            switch (categoryId)
            {
                case MvpDungeonPlacementIds.RoomCategoryId:
                    key = RoomCategoryKey;
                    return true;
                case MvpDungeonPlacementIds.MonsterCategoryId:
                    key = MonsterCategoryKey;
                    return true;
                case MvpDungeonPlacementIds.TrapCategoryId:
                    key = TrapCategoryKey;
                    return true;
                case MvpDungeonPlacementIds.LootNodeCategoryId:
                    key = LootNodeCategoryKey;
                    return true;
                default:
                    key = string.Empty;
                    return false;
            }
        }

        public static bool TryGetOptionNameKey(string optionId, out string key)
        {
            switch (optionId)
            {
                case MvpDungeonPlacementIds.BasicRoomOptionId:
                    key = BasicRoomOptionKey;
                    return true;
                case MvpDungeonPlacementIds.SkeletonOptionId:
                    key = SkeletonOptionKey;
                    return true;
                case MvpDungeonPlacementIds.SpikeTrapOptionId:
                    key = SpikeTrapOptionKey;
                    return true;
                case MvpDungeonPlacementIds.BasicLootNodeOptionId:
                    key = BasicLootNodeOptionKey;
                    return true;
                default:
                    key = string.Empty;
                    return false;
            }
        }

        private static string ResolvePreviewKey(string optionId)
        {
            switch (optionId)
            {
                case MvpDungeonPlacementIds.BasicRoomOptionId:
                    return BasicRoomPreviewKey;
                case MvpDungeonPlacementIds.SkeletonOptionId:
                    return SkeletonPreviewKey;
                case MvpDungeonPlacementIds.SpikeTrapOptionId:
                    return SpikeTrapPreviewKey;
                case MvpDungeonPlacementIds.BasicLootNodeOptionId:
                    return BasicLootNodePreviewKey;
                default:
                    return UnknownPreviewKey;
            }
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            return localize == null ? key : localize(key, key);
        }
    }
}
