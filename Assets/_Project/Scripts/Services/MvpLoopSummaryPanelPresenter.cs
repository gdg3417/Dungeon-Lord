using System;
using System.Text;

namespace DungeonBuilder.M0
{
    public static class MvpLoopSummaryPanelPresenter
    {
        public const string TitleKey = "ui.mvp_loop.panel.title";
        public const string PlacementFormatKey = "ui.mvp_loop.panel.composition_format";
        public const string CompositionFormatKey = PlacementFormatKey;
        public const string LatestRunFormatKey = "ui.mvp_loop.panel.latest_run_format";
        public const string PlacementEffectsFormatKey = "ui.mvp_loop.panel.placement_effects_format";
        public const string ManaFormatKey = "ui.mvp_loop.panel.mana_format";
        public const string LootFormatKey = "ui.mvp_loop.panel.loot_format";
        public const string LootNamedFormatKey = "ui.mvp_loop.panel.loot_named_format";
        public const string LootEntryFormatKey = "ui.mvp_loop.panel.loot_entry_format";
        public const string HeatFormatKey = "ui.mvp_loop.panel.heat_format";
        public const string ResearchFormatKey = "ui.mvp_loop.panel.research_format";
        public const string ResearchUnlockFormatKey = "ui.mvp_loop.panel.research_unlock_format";
        public const string AdventurerPartyFormatKey = "ui.mvp_loop.panel.adventurer_party_format";
        public const string SuggestionFormatKey = "ui.mvp_loop.panel.suggestion_format";
        public const string ValueNoPlacementKey = "ui.mvp_loop.value.no_placement";
        public const string ValueNoRunKey = "ui.mvp_loop.value.no_run";
        public const string ValueUnknownKey = "ui.mvp_loop.value.unknown";
        public const string ValueNoResearchKey = "ui.mvp_loop.value.no_research";
        public const string RunSucceededKey = "ui.mvp_loop.run_status.succeeded";
        public const string RunFailedKey = "ui.mvp_loop.run_status.failed";

        public static string BuildPanelText(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            var builder = new StringBuilder();
            AppendLine(builder, Localize(localize, TitleKey));
            AppendLine(builder, string.Format(
                Localize(localize, CompositionFormatKey),
                ResolveComposition(summary, localize)));
            AppendLine(builder, string.Format(
                Localize(localize, LatestRunFormatKey),
                ResolveRun(summary, localize)));
            AppendLine(builder, string.Format(
                Localize(localize, PlacementEffectsFormatKey),
                MvpPlacementEffectsPresenter.BuildEffectsText(summary?.PlacementEffects, localize)));
            AppendLine(builder, string.Format(
                Localize(localize, ManaFormatKey),
                summary != null && summary.RuleResolved ? summary.ManaReserve : 0d));
            AppendLine(builder, BuildLootLine(summary, localize));
            AppendLine(builder, string.Format(
                Localize(localize, HeatFormatKey),
                summary != null && summary.RuleResolved ? summary.HeatBefore : 0d,
                summary != null && summary.RuleResolved ? summary.HeatAfter : 0d,
                ResolveKeyOrFallback(summary?.HeatTierId, localize, ValueUnknownKey)));
            AppendLine(builder, string.Format(
                Localize(localize, ResearchFormatKey),
                ResolveResearch(summary, localize)));
            if (summary != null && summary.RuleResolved && summary.HasResearchUnlock)
            {
                AppendLine(builder, string.Format(
                    Localize(localize, ResearchUnlockFormatKey),
                    ResolveResearchUnlock(summary, localize)));
            }
            if (summary != null && summary.RuleResolved && summary.HasRunOutcome)
            {
                string partyPreview = MvpRunResultFeedbackPresenter.BuildPartyPreview(summary, localize);
                if (!string.IsNullOrEmpty(partyPreview))
                {
                    AppendLine(builder, string.Format(
                        Localize(localize, AdventurerPartyFormatKey),
                        partyPreview));
                }
            }
            AppendLine(builder, string.Format(
                Localize(localize, SuggestionFormatKey),
                ResolveKeyOrFallback(summary?.NextOptimizationSuggestionKey, localize, MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey)));
            return builder.ToString();
        }

        private static string ResolveComposition(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved)
            {
                return Localize(localize, ValueNoPlacementKey);
            }

            if (summary.DungeonPlacements != null && summary.DungeonPlacements.Length > 0)
            {
                return MvpDungeonPlacementPresenter.BuildCompositionText(summary.DungeonPlacements, localize);
            }

            if (summary.HasPlacementContext && !string.IsNullOrWhiteSpace(summary.SelectedStructureId))
            {
                return MvpPlayerFacingLabelResolver.ResolveStructureDisplayName(summary.SelectedStructureId, localize);
            }

            return Localize(localize, ValueNoPlacementKey);
        }

        private static string ResolveRun(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved || !summary.HasRunOutcome)
            {
                return Localize(localize, ValueNoRunKey);
            }

            return Localize(localize, summary.RunSucceeded ? RunSucceededKey : RunFailedKey);
        }

        public static string BuildNamedLootText(RunLootDropRecord[] lootBreakdown, Func<string, string, string> localize)
        {
            if (lootBreakdown == null || lootBreakdown.Length == 0)
            {
                return string.Empty;
            }

            var parts = new System.Collections.Generic.List<string>(lootBreakdown.Length);
            for (int i = 0; i < lootBreakdown.Length; i++)
            {
                RunLootDropRecord entry = lootBreakdown[i];
                if (entry == null || entry.Quantity <= 0 || string.IsNullOrWhiteSpace(entry.NameKey))
                {
                    continue;
                }

                string name = Localize(localize, entry.NameKey);
                if (string.Equals(name, entry.NameKey, StringComparison.Ordinal))
                {
                    continue;
                }

                parts.Add(string.Format(Localize(localize, LootEntryFormatKey), entry.Quantity, name));
            }

            return parts.Count == 0 ? string.Empty : string.Join(", ", parts);
        }

        private static string BuildLootLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            int generated = summary != null && summary.RuleResolved ? summary.LootGeneratedWorldValue : 0;
            int extracted = summary != null && summary.RuleResolved ? summary.LootExtractedWorldValue : 0;
            int tradeable = summary != null && summary.RuleResolved ? summary.LootExtractedTradeableWorldValue : 0;
            string namedLoot = summary != null && summary.RuleResolved ? BuildNamedLootText(summary.LootBreakdown, localize) : string.Empty;
            if (!string.IsNullOrWhiteSpace(namedLoot))
            {
                return string.Format(Localize(localize, LootNamedFormatKey), generated, extracted, tradeable, namedLoot);
            }

            return string.Format(Localize(localize, LootFormatKey), generated, extracted, tradeable);
        }

        private static string ResolveResearch(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved || !summary.HasResearchStatus)
            {
                return Localize(localize, ValueNoResearchKey);
            }

            return MvpPlayerFacingLabelResolver.ResolveResearchStatusLabel(summary.ResearchStatusKey, localize);
        }

        private static string ResolveResearchUnlock(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved)
            {
                return Localize(localize, ResearchUnlockSummaryPresenter.NoneKey);
            }

            string key = summary.HasResearchUnlock
                ? summary.ResearchUnlockSummaryKey
                : ResearchUnlockSummaryPresenter.NoneKey;
            return ResolveKeyOrFallback(key, localize, ResearchUnlockSummaryPresenter.NoneKey);
        }

        private static string ResolveKeyOrFallback(string key, Func<string, string, string> localize, string fallbackKey)
        {
            return string.IsNullOrWhiteSpace(key)
                ? Localize(localize, fallbackKey)
                : Localize(localize, key);
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            if (localize == null)
            {
                return key;
            }

            return localize(key, key);
        }

        private static void AppendLine(StringBuilder builder, string line)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(line ?? string.Empty);
        }
    }
}
