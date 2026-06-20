using DungeonBuilder.M0;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpRecentSpoilsLedgerPresenterTests
    {
        [Test]
        public void Resolve_NoRunHistoryProducesNoPanel()
        {
            MvpRecentSpoilsLedgerSummary summary = MvpRecentSpoilsLedgerPresenter.Resolve(new SaveData(), null);
            string text = MvpRecentSpoilsLedgerPresenter.BuildPanelText(summary, Localize);
            Assert.That(text, Is.Empty);
        }

        [Test]
        public void BuildPanelText_MissingLedgerLocalizationReturnsNoRawKeys()
        {
            MvpRecentSpoilsLedgerSummary summary = MvpRecentSpoilsLedgerPresenter.Resolve(SaveWithRuns(Run(5, Loot("loot.item.salvage.trap.name", 1, 5))), null);
            string text = MvpRecentSpoilsLedgerPresenter.BuildPanelText(summary, (key, fallback) => fallback);
            Assert.That(text, Is.Empty);
        }

        [Test]
        public void Resolve_LatestRunWithItemizedLootShowsLocalizedCountAndName()
        {
            MvpRecentSpoilsLedgerSummary summary = MvpRecentSpoilsLedgerPresenter.Resolve(SaveWithRuns(Run(5, Loot("loot.item.salvage.trap.name", 1, 5))), null);
            string text = MvpRecentSpoilsLedgerPresenter.BuildPanelText(summary, Localize);
            Assert.That(text, Does.Contain("Latest haul: 1x Trap salvage"));
            Assert.That(text, Does.Contain("Recovered value: 5 tradeable"));
        }

        [Test]
        public void Resolve_ValueWithoutItemizedNamesShowsValueOnly()
        {
            MvpRecentSpoilsLedgerSummary summary = MvpRecentSpoilsLedgerPresenter.Resolve(SaveWithRuns(Run(7)), null);
            string text = MvpRecentSpoilsLedgerPresenter.BuildPanelText(summary, Localize);
            Assert.That(text, Does.Not.Contain("Latest haul:"));
            Assert.That(text, Does.Contain("Recovered value: 7 tradeable"));
            Assert.That(text, Does.Not.Contain("loot.item"));
        }

        [Test]
        public void Resolve_RecentBestUsesRecentRunHistory()
        {
            SaveData save = SaveWithRuns(Run(2), Run(9), Run(5));
            MvpRecentSpoilsLedgerSummary summary = MvpRecentSpoilsLedgerPresenter.Resolve(save, null);
            Assert.That(summary.RecentBestTradeableValue, Is.EqualTo(9));
            Assert.That(MvpRecentSpoilsLedgerPresenter.BuildPanelText(summary, Localize), Does.Contain("Recent best haul: 9 tradeable"));
        }

        [Test]
        public void Resolve_LatestBestTriggersLatestBestTrend()
        {
            MvpRecentSpoilsLedgerSummary summary = MvpRecentSpoilsLedgerPresenter.Resolve(SaveWithRuns(Run(2), Run(5)), null);
            Assert.That(summary.TrendKey, Is.EqualTo(MvpRecentSpoilsLedgerPresenter.TrendLatestBestKey));
        }

        [Test]
        public void Resolve_GreedSetupWithHeatWarningTriggersGreedHeatWarningTrend()
        {
            var greed = new MvpPostContractGreedTrialSummary { GreedSetupTestedComplete = true, CurrentHeatTierId = CurrentHeatTierResolver.NoticeTierId };
            MvpRecentSpoilsLedgerSummary summary = MvpRecentSpoilsLedgerPresenter.Resolve(SaveWithRuns(Run(5)), greed);
            Assert.That(summary.TrendKey, Is.EqualTo(MvpRecentSpoilsLedgerPresenter.TrendGreedHeatWarningKey));
        }

        [Test]
        public void Resolve_GreedTrialCompleteTriggersGreedStabilizedTrend()
        {
            var greed = new MvpPostContractGreedTrialSummary { IsComplete = true };
            MvpRecentSpoilsLedgerSummary summary = MvpRecentSpoilsLedgerPresenter.Resolve(SaveWithRuns(Run(5)), greed);
            Assert.That(summary.TrendKey, Is.EqualTo(MvpRecentSpoilsLedgerPresenter.TrendGreedStabilizedKey));
        }

        [Test]
        public void Resolve_IsReadOnlyAndDoesNotMutateSave()
        {
            SaveData save = SaveWithRuns(Run(5, Loot("loot.item.salvage.trap.name", 1, 5)));
            string before = JsonUtility.ToJson(save);
            MvpRecentSpoilsLedgerSummary summary = MvpRecentSpoilsLedgerPresenter.Resolve(save, null);
            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
            Assert.That(summary.WouldMutateState, Is.False);
            Assert.That(summary.WouldGrantRewards, Is.False);
            Assert.That(summary.WouldUnlockContent, Is.False);
            Assert.That(summary.WouldCallServer, Is.False);
        }

        private static SaveData SaveWithRuns(params RunOutcomeRecord[] runs)
        {
            return new SaveData { runHistory = new RunHistoryState { LatestOutcome = runs[runs.Length - 1], RecentOutcomes = runs } };
        }

        private static RunOutcomeRecord Run(int tradeableValue, params RunLootDropRecord[] loot)
        {
            return new RunOutcomeRecord
            {
                LootExtractionSummary = new RunLootExtractionSummary { RuleResolved = true, TotalExtractedTradeableWorldValue = tradeableValue },
                LootBreakdown = loot
            };
        }

        private static RunLootDropRecord Loot(string nameKey, int quantity, int tradeableValue)
        {
            return new RunLootDropRecord { NameKey = nameKey, Quantity = quantity, TotalTradeableWorldValue = tradeableValue };
        }

        private static string Localize(string key, string fallback)
        {
            if (key == "ui.mvp_spoils_ledger.title") return "Recent Spoils Ledger";
            if (key == "ui.mvp_spoils_ledger.latest_haul_format") return "Latest haul: {0}";
            if (key == "ui.mvp_spoils_ledger.recovered_value_format") return "Recovered value: {0} tradeable";
            if (key == "ui.mvp_spoils_ledger.recent_best_format") return "Recent best haul: {0} tradeable";
            if (key == "ui.mvp_spoils_ledger.trend_format") return "Spoils trend: {0}";
            if (key == "ui.mvp_spoils_ledger.value.none_yet") return "none yet";
            if (key == "ui.mvp_spoils_ledger.trend.run_dungeon") return "Run the dungeon to recover trade goods.";
            if (key == "ui.mvp_spoils_ledger.trend.latest_best") return "Latest run produced the strongest recent haul.";
            if (key == "ui.mvp_spoils_ledger.trend.greed_heat_warning") return "Greed pressure is producing loot, but heat control still matters.";
            if (key == "ui.mvp_spoils_ledger.trend.greed_stabilized") return "Greed pressure is stabilized; continue improving reward pressure.";
            if (key == "ui.mvp_spoils_ledger.trend.steady") return "Recent hauls are steady.";
            if (key == "ui.mvp_loop.panel.loot_entry_format") return "{0}x {1}";
            if (key == "loot.item.salvage.trap.name") return "Trap salvage";
            return fallback;
        }
    }
}
