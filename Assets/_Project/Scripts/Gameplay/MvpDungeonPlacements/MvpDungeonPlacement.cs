using System;
using System.Collections.Generic;

namespace DungeonBuilder.M0.Gameplay.MvpDungeonPlacements
{
    public static class MvpDungeonPlacementIds
    {
        public const string RoomCategoryId = "placement.category.room";
        public const string MonsterCategoryId = "placement.category.monster";
        public const string TrapCategoryId = "placement.category.trap";
        public const string LootNodeCategoryId = "placement.category.loot_node";

        public const string BasicRoomOptionId = "placement.option.room.basic";
        public const string SkeletonOptionId = "placement.option.monster.skeleton";
        public const string SpikeTrapOptionId = "placement.option.trap.spike";
        public const string BasicLootNodeOptionId = "placement.option.loot_node.basic";

        public static readonly string[] OrderedCategoryIds =
        {
            RoomCategoryId,
            MonsterCategoryId,
            TrapCategoryId,
            LootNodeCategoryId
        };

        public static readonly string[] OrderedStarterOptionIds =
        {
            BasicRoomOptionId,
            SkeletonOptionId,
            SpikeTrapOptionId,
            BasicLootNodeOptionId
        };

        public static bool IsAllowedCategory(string categoryId)
        {
            return string.Equals(categoryId, RoomCategoryId, StringComparison.Ordinal) ||
                   string.Equals(categoryId, MonsterCategoryId, StringComparison.Ordinal) ||
                   string.Equals(categoryId, TrapCategoryId, StringComparison.Ordinal) ||
                   string.Equals(categoryId, LootNodeCategoryId, StringComparison.Ordinal);
        }

        public static bool IsAllowedOption(string optionId)
        {
            return string.Equals(optionId, BasicRoomOptionId, StringComparison.Ordinal) ||
                   string.Equals(optionId, SkeletonOptionId, StringComparison.Ordinal) ||
                   string.Equals(optionId, SpikeTrapOptionId, StringComparison.Ordinal) ||
                   string.Equals(optionId, BasicLootNodeOptionId, StringComparison.Ordinal);
        }

        public static bool TryGetCategoryForOption(string optionId, out string categoryId)
        {
            switch (optionId)
            {
                case BasicRoomOptionId:
                    categoryId = RoomCategoryId;
                    return true;
                case SkeletonOptionId:
                    categoryId = MonsterCategoryId;
                    return true;
                case SpikeTrapOptionId:
                    categoryId = TrapCategoryId;
                    return true;
                case BasicLootNodeOptionId:
                    categoryId = LootNodeCategoryId;
                    return true;
                default:
                    categoryId = string.Empty;
                    return false;
            }
        }

        public static string GetStarterOptionForCategory(string categoryId)
        {
            switch (categoryId)
            {
                case RoomCategoryId:
                    return BasicRoomOptionId;
                case MonsterCategoryId:
                    return SkeletonOptionId;
                case TrapCategoryId:
                    return SpikeTrapOptionId;
                case LootNodeCategoryId:
                    return BasicLootNodeOptionId;
                default:
                    return string.Empty;
            }
        }
    }

    [Serializable]
    public sealed class MvpDungeonPlacementEntry
    {
        public string CategoryId;
        public string OptionId;
        public int Revision;

        public MvpDungeonPlacementEntry()
        {
        }

        public MvpDungeonPlacementEntry(string categoryId, string optionId, int revision)
        {
            CategoryId = categoryId ?? string.Empty;
            OptionId = optionId ?? string.Empty;
            Revision = revision;
        }
    }

    [Serializable]
    public sealed class MvpDungeonPlacementState
    {
        public List<MvpDungeonPlacementEntry> Entries = new List<MvpDungeonPlacementEntry>();
        public int NextRevision = 1;
    }
}
