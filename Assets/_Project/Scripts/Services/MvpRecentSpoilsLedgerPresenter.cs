using System;
using System.Text;

namespace DungeonBuilder.M0
{
    public static class MvpRecentSpoilsLedgerPresenter
    {
        public const string TitleKey = "ui.mvp_spoils_ledger.title";
        public const string LatestHaulFormatKey = "ui.mvp_spoils_ledger.latest_haul_format";
        public const string RecoveredValueFormatKey = "ui.mvp_spoils_ledger.recovered_value_format";
        public const string RecentBestFormatKey = "ui.mvp_spoils_ledger.recent_best_format";
        public const string TrendFormatKey = "ui.mvp_spoils_ledger.trend_format";
        public const string NoneYetKey = "ui.mvp_spoils_ledger.value.none_yet";
        public const string TrendRunDungeonKey = "ui.mvp_spoils_ledger.trend.run_dungeon";
        public const string TrendLatestBestKey = "ui.mvp_spoils_ledger.trend.latest_best";
        public const string TrendGreedHeatWarningKey = "ui.mvp_spoils_ledger.trend.greed_heat_warning";
        public const string TrendGreedStabilizedKey = "ui.mvp_spoils_ledger.trend.greed_stabilized";
        public const string TrendSteadyKey = "ui.mvp_spoils_ledger.trend.steady";

        public static MvpRecentSpoilsLedgerSummary Resolve(SaveData save, MvpPostContractGreedTrialSummary greedTrial)
        {
            var summary = new MvpRecentSpoilsLedgerSummary
            {
                WouldMutateState = false,
                WouldGrantRewards = false,
                WouldUnlockContent = false,
                WouldCallServer = false
            };

            RunHistoryState history = save?.runHistory;
            RunOutcomeRecord latest = history?.LatestOutcome;
            if (latest == null && history?.RecentOutcomes != null && history.RecentOutcomes.Length > 0)
            {
                latest = history.RecentOutcomes[history.RecentOutcomes.Length - 1];
            }

            summary.HasRunHistory = latest != null;
            summary.LatestTradeableValue = ResolveTradeableValue(latest);
            summary.LatestLootBreakdown = CloneLootBreakdown(latest?.LootBreakdown);
            summary.LatestNamedLootTextAvailable = HasLocalizableLoot(summary.LatestLootBreakdown);
            summary.RecentBestTradeableValue = ResolveRecentBestTradeableValue(history, summary.LatestTradeableValue);
            summary.HasLootData = summary.LatestTradeableValue > 0 || summary.LatestNamedLootTextAvailable || summary.RecentBestTradeableValue > 0;
            summary.TrendKey = ResolveTrendKey(summary, greedTrial);
            summary.RuleResolved = true;
            return summary;
        }

        public static string BuildPanelText(MvpRecentSpoilsLedgerSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            AppendLine(builder, Localize(localize, TitleKey));
            string namedLoot = summary.LatestNamedLootTextAvailable
                ? MvpLoopSummaryPanelPresenter.BuildNamedLootText(summary.LatestLootBreakdown, localize)
                : string.Empty;
            if (!string.IsNullOrWhiteSpace(namedLoot) || summary.LatestTradeableValue <= 0)
            {
                string latestHaul = !string.IsNullOrWhiteSpace(namedLoot) ? namedLoot : Localize(localize, NoneYetKey);
                AppendLine(builder, string.Format(Localize(localize, LatestHaulFormatKey), latestHaul));
            }
            if (summary.LatestTradeableValue > 0)
            {
                AppendLine(builder, string.Format(Localize(localize, RecoveredValueFormatKey), summary.LatestTradeableValue));
            }
            if (summary.HasRunHistory || summary.RecentBestTradeableValue > 0)
            {
                AppendLine(builder, string.Format(Localize(localize, RecentBestFormatKey), summary.RecentBestTradeableValue));
            }
            AppendLine(builder, string.Format(Localize(localize, TrendFormatKey), Localize(localize, string.IsNullOrWhiteSpace(summary.TrendKey) ? TrendRunDungeonKey : summary.TrendKey)));
            return builder.ToString();
        }

        private static string ResolveTrendKey(MvpRecentSpoilsLedgerSummary summary, MvpPostContractGreedTrialSummary greedTrial)
        {
            if (summary == null || !summary.HasLootData) return TrendRunDungeonKey;
            if (greedTrial != null && greedTrial.IsComplete) return TrendGreedStabilizedKey;
            if (greedTrial != null && greedTrial.GreedSetupTestedComplete &&
                (string.Equals(greedTrial.CurrentHeatTierId, CurrentHeatTierResolver.NoticeTierId, StringComparison.Ordinal) ||
                 string.Equals(greedTrial.CurrentHeatTierId, CurrentHeatTierResolver.ConcernTierId, StringComparison.Ordinal))) return TrendGreedHeatWarningKey;
            if (summary.LatestTradeableValue > 0 && summary.LatestTradeableValue >= summary.RecentBestTradeableValue) return TrendLatestBestKey;
            return TrendSteadyKey;
        }

        private static int ResolveRecentBestTradeableValue(RunHistoryState history, int latestValue)
        {
            int best = Math.Max(0, latestValue);
            RunOutcomeRecord[] outcomes = history?.RecentOutcomes ?? Array.Empty<RunOutcomeRecord>();
            for (int i = 0; i < outcomes.Length; i++) best = Math.Max(best, ResolveTradeableValue(outcomes[i]));
            return best;
        }

        private static int ResolveTradeableValue(RunOutcomeRecord outcome)
        {
            if (outcome?.LootExtractionSummary != null && outcome.LootExtractionSummary.RuleResolved)
            {
                return Math.Max(0, outcome.LootExtractionSummary.TotalExtractedTradeableWorldValue);
            }
            if (outcome?.LootBreakdown != null)
            {
                int total = 0;
                for (int i = 0; i < outcome.LootBreakdown.Length; i++) total += Math.Max(0, outcome.LootBreakdown[i]?.TotalTradeableWorldValue ?? 0);
                return total;
            }
            return 0;
        }

        private static bool HasLocalizableLoot(RunLootDropRecord[] breakdown)
        {
            if (breakdown == null) return false;
            for (int i = 0; i < breakdown.Length; i++)
            {
                RunLootDropRecord entry = breakdown[i];
                if (entry != null && entry.Quantity > 0 && !string.IsNullOrWhiteSpace(entry.NameKey)) return true;
            }
            return false;
        }

        private static RunLootDropRecord[] CloneLootBreakdown(RunLootDropRecord[] source)
        {
            if (source == null || source.Length == 0) return Array.Empty<RunLootDropRecord>();
            var clone = new RunLootDropRecord[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                RunLootDropRecord entry = source[i];
                clone[i] = entry == null ? null : new RunLootDropRecord { LootId = entry.LootId, NameKey = entry.NameKey, Quantity = entry.Quantity, TotalWorldValue = entry.TotalWorldValue, TotalTradeableWorldValue = entry.TotalTradeableWorldValue };
            }
            return clone;
        }

        private static string Localize(Func<string, string, string> localize, string key) => localize == null ? key : localize(key, key);
        private static void AppendLine(StringBuilder builder, string line) { if (builder.Length > 0) builder.Append('\n'); builder.Append(line ?? string.Empty); }
    }

    [Serializable]
    public sealed class MvpRecentSpoilsLedgerSummary
    {
        public bool RuleResolved;
        public bool HasRunHistory;
        public bool HasLootData;
        public int LatestTradeableValue;
        public int RecentBestTradeableValue;
        public bool LatestNamedLootTextAvailable;
        public RunLootDropRecord[] LatestLootBreakdown = Array.Empty<RunLootDropRecord>();
        public string TrendKey;
        public bool WouldMutateState;
        public bool WouldGrantRewards;
        public bool WouldUnlockContent;
        public bool WouldCallServer;
    }
}
