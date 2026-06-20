using System;
using System.Text;

namespace DungeonBuilder.M0
{
    public static class MvpPlacementEffectsPresenter
    {
        public const string EmptyKey = "ui.mvp_placement_effects.empty";
        public const string CombinedFormatKey = "ui.mvp_placement_effects.combined_format";
        public const string DetailSeparatorKey = "ui.mvp_placement_effects.detail_separator";
        public const string PathCapacityFormatKey = "ui.mvp_placement_effects.path_capacity_format";
        public const string DangerFormatKey = "ui.mvp_placement_effects.danger_format";
        public const string ManaPressureFormatKey = "ui.mvp_placement_effects.mana_pressure_format";
        public const string HeatPressureFormatKey = "ui.mvp_placement_effects.heat_pressure_format";
        public const string HeatReliefFormatKey = "ui.mvp_placement_effects.heat_relief_format";
        public const string LootBonusFormatKey = "ui.mvp_placement_effects.loot_bonus_format";
        public const string AttractionFormatKey = "ui.mvp_placement_effects.attraction_format";
        public const string ExplanationFormatKey = "ui.mvp_placement_effects.explanation_format";

        public static string BuildEffectsText(MvpPlacementEffectsSummary effects, Func<string, string, string> localize)
        {
            if (effects == null || !effects.RuleResolved || !HasAnyEffect(effects))
            {
                return Localize(localize, EmptyKey);
            }

            string separator = Localize(localize, DetailSeparatorKey);
            var builder = new StringBuilder();
            AppendEffect(builder, separator, localize, PathCapacityFormatKey, effects.PathCapacity);
            AppendEffect(builder, separator, localize, DangerFormatKey, effects.Danger);
            AppendEffect(builder, separator, localize, ManaPressureFormatKey, effects.ManaPressure);
            AppendHeatEffect(builder, separator, localize, effects.HeatPressure);
            AppendEffect(builder, separator, localize, LootBonusFormatKey, effects.LootBonus);
            AppendEffect(builder, separator, localize, AttractionFormatKey, effects.Attraction);

            string explanations = BuildExplanationText(effects, localize, separator);
            string details = builder.ToString();
            if (string.IsNullOrWhiteSpace(explanations))
            {
                return details;
            }

            return string.Format(Localize(localize, ExplanationFormatKey), details, explanations);
        }

        public static bool HasAnyEffect(MvpPlacementEffectsSummary effects)
        {
            return effects != null &&
                   (effects.PathCapacity != 0 ||
                    effects.Danger != 0 ||
                    effects.ManaPressure != 0 ||
                    effects.HeatPressure != 0 ||
                    effects.LootBonus != 0 ||
                    effects.Attraction != 0);
        }

        private static string BuildExplanationText(MvpPlacementEffectsSummary effects, Func<string, string, string> localize, string separator)
        {
            if (effects?.EffectLocalizationKeys == null || effects.EffectLocalizationKeys.Length == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < effects.EffectLocalizationKeys.Length; i++)
            {
                string key = effects.EffectLocalizationKeys[i];
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(separator);
                }

                builder.Append(Localize(localize, key));
            }

            return builder.ToString();
        }

        private static void AppendHeatEffect(StringBuilder builder, string separator, Func<string, string, string> localize, int value)
        {
            if (value == 0)
            {
                return;
            }

            string formatKey = value < 0 ? HeatReliefFormatKey : HeatPressureFormatKey;
            int displayValue = value < 0 ? Math.Abs(value) : value;
            AppendEffect(builder, separator, localize, formatKey, displayValue);
        }

        private static void AppendEffect(StringBuilder builder, string separator, Func<string, string, string> localize, string formatKey, int value)
        {
            if (value == 0)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(separator);
            }

            builder.Append(string.Format(Localize(localize, formatKey), value));
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            return localize == null ? key : localize(key, key);
        }
    }
}
