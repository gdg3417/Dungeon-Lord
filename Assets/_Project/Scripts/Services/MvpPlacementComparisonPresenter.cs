using System;
using System.Collections.Generic;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0
{
    public static class MvpPlacementComparisonPresenter
    {
        public const string ComparedWithFormatKey = "ui.mvp_placement_comparison.compared_with_format";
        public const string BasicRoomToNarrowHallSummaryKey = "ui.mvp_placement_comparison.room.basic_to_narrow_hall";
        public const string NarrowHallToBasicRoomSummaryKey = "ui.mvp_placement_comparison.room.narrow_hall_to_basic";
        public const string SkeletonToGoblinSummaryKey = "ui.mvp_placement_comparison.monster.skeleton_to_goblin";
        public const string GoblinToSkeletonSummaryKey = "ui.mvp_placement_comparison.monster.goblin_to_skeleton";
        public const string SpikeTrapToSnareTrapSummaryKey = "ui.mvp_placement_comparison.trap.spike_to_snare";
        public const string SnareTrapToSpikeTrapSummaryKey = "ui.mvp_placement_comparison.trap.snare_to_spike";
        public const string BasicLootNodeToHiddenCacheSummaryKey = "ui.mvp_placement_comparison.loot_node.basic_to_hidden_cache";
        public const string HiddenCacheToBasicLootNodeSummaryKey = "ui.mvp_placement_comparison.loot_node.hidden_cache_to_basic";

        public static MvpPlacementComparisonPreview Resolve(
            MvpDungeonFloorLayoutState layout,
            MvpDungeonPlacementState placements,
            RunSimulationConfig config,
            string selectedCategoryId,
            string selectedOptionId)
        {
            var preview = new MvpPlacementComparisonPreview
            {
                HasComparison = false,
                SelectedOptionId = selectedOptionId ?? string.Empty
            };

            if (!MvpDungeonPlacementIds.IsAllowedCategory(selectedCategoryId) ||
                !MvpDungeonPlacementIds.IsAllowedOption(selectedOptionId) ||
                !MvpDungeonPlacementIds.TryGetCategoryForOption(selectedOptionId, out string selectedOptionCategoryId) ||
                !string.Equals(selectedCategoryId, selectedOptionCategoryId, StringComparison.Ordinal) ||
                config == null || config.MvpPlacementEffects == null || config.MvpPlacementEffects.Length == 0)
            {
                return preview;
            }

            string baselineOptionId = ResolveBaselineOptionId(layout, placements, selectedCategoryId, selectedOptionId);
            if (string.IsNullOrWhiteSpace(baselineOptionId) || string.Equals(baselineOptionId, selectedOptionId, StringComparison.Ordinal))
            {
                return preview;
            }

            if (!TryGetComparisonSummaryKey(baselineOptionId, selectedOptionId, out string summaryKey))
            {
                return preview;
            }

            Dictionary<string, MvpPlacementEffectConfig> effectsByOptionId = BuildEffectLookup(config.MvpPlacementEffects);
            if (!effectsByOptionId.TryGetValue(baselineOptionId, out MvpPlacementEffectConfig baselineEffect) ||
                !effectsByOptionId.TryGetValue(selectedOptionId, out MvpPlacementEffectConfig selectedEffect))
            {
                return preview;
            }

            preview.HasComparison = true;
            preview.BaselineOptionId = baselineOptionId;
            preview.ComparisonSummaryKey = summaryKey;
            preview.DeltaPathCapacity = selectedEffect.PathCapacity - baselineEffect.PathCapacity;
            preview.DeltaDanger = selectedEffect.Danger - baselineEffect.Danger;
            preview.DeltaManaPressure = selectedEffect.ManaPressure - baselineEffect.ManaPressure;
            preview.DeltaHeatPressure = selectedEffect.HeatPressure - baselineEffect.HeatPressure;
            preview.DeltaLoot = selectedEffect.LootBonus - baselineEffect.LootBonus;
            preview.DeltaAttraction = selectedEffect.Attraction - baselineEffect.Attraction;
            return preview;
        }

        public static string BuildComparisonText(MvpPlacementComparisonPreview preview, Func<string, string, string> localize)
        {
            if (!preview.HasComparison || string.IsNullOrWhiteSpace(preview.BaselineOptionId) || string.IsNullOrWhiteSpace(preview.ComparisonSummaryKey))
            {
                return string.Empty;
            }

            string format = Localize(localize, ComparedWithFormatKey);
            string baselineName = MvpDungeonPlacementPresenter.ResolveOptionName(preview.BaselineOptionId, localize);
            string summary = Localize(localize, preview.ComparisonSummaryKey);
            return string.Format(format, baselineName, summary);
        }

        private static string ResolveBaselineOptionId(MvpDungeonFloorLayoutState layout, MvpDungeonPlacementState placements, string selectedCategoryId, string selectedOptionId)
        {
            MvpDungeonPlacementEntry[] orderedPlacements = MvpDungeonLayoutResolver.ResolveOrderedPlacements(layout, placements);
            for (int i = 0; i < orderedPlacements.Length; i++)
            {
                MvpDungeonPlacementEntry entry = orderedPlacements[i];
                if (entry != null && string.Equals(entry.CategoryId, selectedCategoryId, StringComparison.Ordinal) && !string.Equals(entry.OptionId, selectedOptionId, StringComparison.Ordinal))
                {
                    return entry.OptionId;
                }
            }

            return MvpDungeonPlacementIds.GetStarterOptionForCategory(selectedCategoryId);
        }

        private static bool TryGetComparisonSummaryKey(string baselineOptionId, string selectedOptionId, out string key)
        {
            key = string.Empty;
            if (string.Equals(baselineOptionId, MvpDungeonPlacementIds.BasicRoomOptionId, StringComparison.Ordinal) && string.Equals(selectedOptionId, MvpDungeonPlacementIds.NarrowHallOptionId, StringComparison.Ordinal)) key = BasicRoomToNarrowHallSummaryKey;
            else if (string.Equals(baselineOptionId, MvpDungeonPlacementIds.NarrowHallOptionId, StringComparison.Ordinal) && string.Equals(selectedOptionId, MvpDungeonPlacementIds.BasicRoomOptionId, StringComparison.Ordinal)) key = NarrowHallToBasicRoomSummaryKey;
            else if (string.Equals(baselineOptionId, MvpDungeonPlacementIds.SkeletonOptionId, StringComparison.Ordinal) && string.Equals(selectedOptionId, MvpDungeonPlacementIds.GoblinOptionId, StringComparison.Ordinal)) key = SkeletonToGoblinSummaryKey;
            else if (string.Equals(baselineOptionId, MvpDungeonPlacementIds.GoblinOptionId, StringComparison.Ordinal) && string.Equals(selectedOptionId, MvpDungeonPlacementIds.SkeletonOptionId, StringComparison.Ordinal)) key = GoblinToSkeletonSummaryKey;
            else if (string.Equals(baselineOptionId, MvpDungeonPlacementIds.SpikeTrapOptionId, StringComparison.Ordinal) && string.Equals(selectedOptionId, MvpDungeonPlacementIds.SnareTrapOptionId, StringComparison.Ordinal)) key = SpikeTrapToSnareTrapSummaryKey;
            else if (string.Equals(baselineOptionId, MvpDungeonPlacementIds.SnareTrapOptionId, StringComparison.Ordinal) && string.Equals(selectedOptionId, MvpDungeonPlacementIds.SpikeTrapOptionId, StringComparison.Ordinal)) key = SnareTrapToSpikeTrapSummaryKey;
            else if (string.Equals(baselineOptionId, MvpDungeonPlacementIds.BasicLootNodeOptionId, StringComparison.Ordinal) && string.Equals(selectedOptionId, MvpDungeonPlacementIds.HiddenCacheOptionId, StringComparison.Ordinal)) key = BasicLootNodeToHiddenCacheSummaryKey;
            else if (string.Equals(baselineOptionId, MvpDungeonPlacementIds.HiddenCacheOptionId, StringComparison.Ordinal) && string.Equals(selectedOptionId, MvpDungeonPlacementIds.BasicLootNodeOptionId, StringComparison.Ordinal)) key = HiddenCacheToBasicLootNodeSummaryKey;
            return !string.IsNullOrWhiteSpace(key);
        }

        private static Dictionary<string, MvpPlacementEffectConfig> BuildEffectLookup(MvpPlacementEffectConfig[] effects)
        {
            var lookup = new Dictionary<string, MvpPlacementEffectConfig>(StringComparer.Ordinal);
            for (int i = 0; i < effects.Length; i++)
            {
                MvpPlacementEffectConfig effect = effects[i];
                if (effect != null && !string.IsNullOrWhiteSpace(effect.OptionId) && !lookup.ContainsKey(effect.OptionId))
                {
                    lookup.Add(effect.OptionId, effect);
                }
            }

            return lookup;
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            return localize == null ? key : localize(key, key);
        }
    }

    public struct MvpPlacementComparisonPreview
    {
        public bool HasComparison;
        public string BaselineOptionId;
        public string SelectedOptionId;
        public string ComparisonSummaryKey;
        public int DeltaPathCapacity;
        public int DeltaDanger;
        public int DeltaManaPressure;
        public int DeltaHeatPressure;
        public int DeltaLoot;
        public int DeltaAttraction;
    }
}
