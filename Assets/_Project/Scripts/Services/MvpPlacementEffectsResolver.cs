using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0
{
    public static class MvpPlacementEffectsResolver
    {
        public static MvpPlacementEffectsSummary Resolve(MvpDungeonPlacementState placements, RunSimulationConfig config)
        {
            var summary = new MvpPlacementEffectsSummary
            {
                RuleResolved = true,
                RuleSourceId = ResolveRuleSourceId(config),
                ContributingOptionIds = Array.Empty<string>(),
                EffectLocalizationKeys = Array.Empty<string>()
            };

            if (placements == null || placements.Entries == null || placements.Entries.Count == 0 || config == null || config.MvpPlacementEffects == null || config.MvpPlacementEffects.Length == 0)
            {
                return summary;
            }

            Dictionary<string, MvpPlacementEffectConfig> effectsByOption = BuildEffectLookup(config.MvpPlacementEffects);
            var contributingOptionIds = new List<string>();
            var localizationKeys = new List<string>();

            foreach (MvpDungeonPlacementEntry placement in GetOrderedPlacements(placements))
            {
                if (placement == null || string.IsNullOrWhiteSpace(placement.OptionId) || !effectsByOption.TryGetValue(placement.OptionId, out MvpPlacementEffectConfig effect))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(effect.CategoryId) && !string.Equals(effect.CategoryId, placement.CategoryId, StringComparison.Ordinal))
                {
                    continue;
                }

                summary.PathCapacity += effect.PathCapacity;
                summary.Danger += effect.Danger;
                summary.ManaPressure += effect.ManaPressure;
                summary.HeatPressure += effect.HeatPressure;
                summary.LootBonus += effect.LootBonus;
                summary.Attraction += effect.Attraction;
                contributingOptionIds.Add(placement.OptionId);
                if (!string.IsNullOrWhiteSpace(effect.ExplanationKey))
                {
                    localizationKeys.Add(effect.ExplanationKey);
                }
            }

            summary.ContributingOptionIds = contributingOptionIds.ToArray();
            summary.EffectLocalizationKeys = localizationKeys.ToArray();
            return summary;
        }

        private static string ResolveRuleSourceId(RunSimulationConfig config)
        {
            return config != null && !string.IsNullOrWhiteSpace(config.MvpPlacementEffectsRuleSourceId)
                ? config.MvpPlacementEffectsRuleSourceId
                : string.Empty;
        }

        private static Dictionary<string, MvpPlacementEffectConfig> BuildEffectLookup(MvpPlacementEffectConfig[] effects)
        {
            var lookup = new Dictionary<string, MvpPlacementEffectConfig>(StringComparer.Ordinal);
            if (effects == null)
            {
                return lookup;
            }

            for (int i = 0; i < effects.Length; i++)
            {
                MvpPlacementEffectConfig effect = effects[i];
                if (effect == null || string.IsNullOrWhiteSpace(effect.OptionId) || lookup.ContainsKey(effect.OptionId))
                {
                    continue;
                }

                lookup.Add(effect.OptionId, effect);
            }

            return lookup;
        }

        private static IEnumerable<MvpDungeonPlacementEntry> GetOrderedPlacements(MvpDungeonPlacementState placements)
        {
            return placements.Entries
                .Where(entry => entry != null &&
                    MvpDungeonPlacementIds.IsAllowedCategory(entry.CategoryId) &&
                    MvpDungeonPlacementIds.IsAllowedOption(entry.OptionId))
                .OrderBy(entry => Array.IndexOf(MvpDungeonPlacementIds.OrderedCategoryIds, entry.CategoryId))
                .ThenBy(entry => entry.Revision);
        }
    }
}
