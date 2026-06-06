using System;
using System.Text;

namespace DungeonBuilder.M0
{
    public static class MvpLoopSummaryPanelPresenter
    {
        public const string TitleKey = "ui.mvp_loop.panel.title";
        public const string PlacementFormatKey = "ui.mvp_loop.panel.placement_format";
        public const string LatestRunFormatKey = "ui.mvp_loop.panel.latest_run_format";
        public const string ManaFormatKey = "ui.mvp_loop.panel.mana_format";
        public const string LootFormatKey = "ui.mvp_loop.panel.loot_format";
        public const string HeatFormatKey = "ui.mvp_loop.panel.heat_format";
        public const string ResearchFormatKey = "ui.mvp_loop.panel.research_format";
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
                Localize(localize, PlacementFormatKey),
                ResolvePlacement(summary, localize)));
            AppendLine(builder, string.Format(
                Localize(localize, LatestRunFormatKey),
                ResolveRun(summary, localize)));
            AppendLine(builder, string.Format(
                Localize(localize, ManaFormatKey),
                summary != null && summary.RuleResolved ? summary.ManaReserve : 0d));
            AppendLine(builder, string.Format(
                Localize(localize, LootFormatKey),
                summary != null && summary.RuleResolved ? summary.LootGeneratedWorldValue : 0,
                summary != null && summary.RuleResolved ? summary.LootExtractedWorldValue : 0,
                summary != null && summary.RuleResolved ? summary.LootExtractedTradeableWorldValue : 0));
            AppendLine(builder, string.Format(
                Localize(localize, HeatFormatKey),
                summary != null && summary.RuleResolved ? summary.HeatBefore : 0d,
                summary != null && summary.RuleResolved ? summary.HeatAfter : 0d,
                ResolveKeyOrFallback(summary?.HeatTierId, localize, ValueUnknownKey)));
            AppendLine(builder, string.Format(
                Localize(localize, ResearchFormatKey),
                ResolveResearch(summary, localize)));
            AppendLine(builder, string.Format(
                Localize(localize, SuggestionFormatKey),
                ResolveKeyOrFallback(summary?.NextOptimizationSuggestionKey, localize, MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey)));
            return builder.ToString();
        }

        private static string ResolvePlacement(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved || !summary.HasPlacementContext || string.IsNullOrWhiteSpace(summary.SelectedStructureId))
            {
                return Localize(localize, ValueNoPlacementKey);
            }

            return summary.SelectedStructureId;
        }

        private static string ResolveRun(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved || !summary.HasRunOutcome)
            {
                return Localize(localize, ValueNoRunKey);
            }

            string status = Localize(localize, summary.RunSucceeded ? RunSucceededKey : RunFailedKey);
            if (string.IsNullOrWhiteSpace(summary.LatestRunId))
            {
                return status;
            }

            return status + " (" + summary.LatestRunId + ")";
        }

        private static string ResolveResearch(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved || !summary.HasResearchStatus)
            {
                return Localize(localize, ValueNoResearchKey);
            }

            return ResolveKeyOrFallback(summary.ResearchStatusKey, localize, ValueNoResearchKey);
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
