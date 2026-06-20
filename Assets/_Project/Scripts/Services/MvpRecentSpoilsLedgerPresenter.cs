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
        public const string AppraisalFormatKey = "ui.mvp_spoils_ledger.appraisal_format";
        public const string AppraisalItemTradeGoodKey = "ui.mvp_spoils_ledger.appraisal.item_trade_good";
        public const string AppraisalMultiTradeGoodsKey = "ui.mvp_spoils_ledger.appraisal.multi_trade_goods";
        public const string AppraisalValueOnlyKey = "ui.mvp_spoils_ledger.appraisal.value_only";
        public const string AppraisalGreedStabilizedKey = "ui.mvp_spoils_ledger.appraisal.greed_stabilized";

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
            ResolveAppraisal(summary, greedTrial);
            summary.RuleResolved = true;
            return summary;
        }

        public static string BuildPanelText(MvpRecentSpoilsLedgerSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved)
            {
                return string.Empty;
            }

            if (!summary.HasLootData && summary.LatestTradeableValue <= 0 && summary.RecentBestTradeableValue <= 0)
            {
                return string.Empty;
            }

            string title = LocalizeRequired(localize, TitleKey);
            string latestHaulFormat = LocalizeRequired(localize, LatestHaulFormatKey);
            string recoveredValueFormat = LocalizeRequired(localize, RecoveredValueFormatKey);
            string recentBestFormat = LocalizeRequired(localize, RecentBestFormatKey);
            string trendFormat = LocalizeRequired(localize, TrendFormatKey);
            string appraisalFormat = LocalizeRequired(localize, AppraisalFormatKey);
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(latestHaulFormat) ||
                string.IsNullOrEmpty(recoveredValueFormat) || string.IsNullOrEmpty(recentBestFormat) ||
                string.IsNullOrEmpty(trendFormat) || string.IsNullOrEmpty(appraisalFormat))
            {
                return string.Empty;
            }

            string namedLoot = summary.LatestNamedLootTextAvailable
                ? MvpLoopSummaryPanelPresenter.BuildNamedLootText(summary.LatestLootBreakdown, localize)
                : string.Empty;
            if (summary.LatestTradeableValue <= 0 && summary.RecentBestTradeableValue <= 0 && string.IsNullOrWhiteSpace(namedLoot))
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            AppendLine(builder, title);
            if (!string.IsNullOrWhiteSpace(namedLoot) || summary.LatestTradeableValue <= 0)
            {
                string latestHaul = !string.IsNullOrWhiteSpace(namedLoot) ? namedLoot : LocalizeOptional(localize, NoneYetKey, "none yet");
                AppendLine(builder, string.Format(latestHaulFormat, latestHaul));
            }
            if (summary.LatestTradeableValue > 0)
            {
                AppendLine(builder, string.Format(recoveredValueFormat, summary.LatestTradeableValue));
            }
            if (summary.HasRunHistory || summary.RecentBestTradeableValue > 0)
            {
                AppendLine(builder, string.Format(recentBestFormat, summary.RecentBestTradeableValue));
            }
            string trend = LocalizeOptional(localize, string.IsNullOrWhiteSpace(summary.TrendKey) ? TrendRunDungeonKey : summary.TrendKey, string.Empty);
            if (string.IsNullOrWhiteSpace(trend)) return string.Empty;
            AppendLine(builder, string.Format(trendFormat, trend));

            string appraisal = BuildAppraisalText(summary, localize);
            if (!string.IsNullOrWhiteSpace(appraisal))
            {
                AppendLine(builder, string.Format(appraisalFormat, appraisal));
            }
            return builder.ToString();
        }

        private static void ResolveAppraisal(MvpRecentSpoilsLedgerSummary summary, MvpPostContractGreedTrialSummary greedTrial)
        {
            if (summary == null || !summary.HasLootData) return;
            if (greedTrial != null && greedTrial.IsComplete)
            {
                summary.HasAppraisal = true;
                summary.AppraisalKey = AppraisalGreedStabilizedKey;
                return;
            }

            int namedStackCount = CountLocalizableLootStacks(summary.LatestLootBreakdown);
            if (namedStackCount > 1)
            {
                summary.HasAppraisal = true;
                summary.AppraisalKey = AppraisalMultiTradeGoodsKey;
                return;
            }

            if (namedStackCount == 1)
            {
                summary.HasAppraisal = true;
                summary.AppraisalKey = AppraisalItemTradeGoodKey;
                summary.AppraisalArgumentNameKey = ResolveFirstLootNameKey(summary.LatestLootBreakdown);
                return;
            }

            if (summary.LatestTradeableValue > 0 || summary.RecentBestTradeableValue > 0)
            {
                summary.HasAppraisal = true;
                summary.AppraisalKey = AppraisalValueOnlyKey;
            }
        }

        private static string BuildAppraisalText(MvpRecentSpoilsLedgerSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.HasAppraisal || string.IsNullOrWhiteSpace(summary.AppraisalKey)) return string.Empty;
            string appraisal = LocalizeOptional(localize, summary.AppraisalKey, string.Empty);
            if (string.IsNullOrWhiteSpace(appraisal)) return string.Empty;
            if (!string.Equals(summary.AppraisalKey, AppraisalItemTradeGoodKey, StringComparison.Ordinal)) return appraisal;

            string itemName = LocalizeOptional(localize, summary.AppraisalArgumentNameKey, string.Empty);
            if (string.IsNullOrWhiteSpace(itemName)) return LocalizeOptional(localize, AppraisalValueOnlyKey, string.Empty);
            return string.Format(appraisal, itemName);
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

        private static int CountLocalizableLootStacks(RunLootDropRecord[] breakdown)
        {
            if (breakdown == null) return 0;
            int count = 0;
            for (int i = 0; i < breakdown.Length; i++)
            {
                RunLootDropRecord entry = breakdown[i];
                if (entry != null && entry.Quantity > 0 && !string.IsNullOrWhiteSpace(entry.NameKey)) count++;
            }
            return count;
        }

        private static string ResolveFirstLootNameKey(RunLootDropRecord[] breakdown)
        {
            if (breakdown == null) return string.Empty;
            for (int i = 0; i < breakdown.Length; i++)
            {
                RunLootDropRecord entry = breakdown[i];
                if (entry != null && entry.Quantity > 0 && !string.IsNullOrWhiteSpace(entry.NameKey)) return entry.NameKey;
            }
            return string.Empty;
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

        private static string LocalizeRequired(Func<string, string, string> localize, string key)
        {
            string value = localize == null ? string.Empty : localize(key, key);
            return string.Equals(value, key, StringComparison.Ordinal) ? string.Empty : value;
        }

        private static string LocalizeOptional(Func<string, string, string> localize, string key, string fallback)
        {
            string value = localize == null ? string.Empty : localize(key, key);
            return string.Equals(value, key, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

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
        public bool HasAppraisal;
        public string AppraisalKey;
        public string AppraisalArgumentNameKey;
        public bool WouldMutateState;
        public bool WouldGrantRewards;
        public bool WouldUnlockContent;
        public bool WouldCallServer;
    }
}
