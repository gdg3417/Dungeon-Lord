#if UNITY_EDITOR
using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests
{
    public sealed class ProductionAnalysisPresenterTests
    {
        private static RunOutcomeRecord Outcome(string route, int survivors, int deaths, int loot) => new RunOutcomeRecord
        {
            FinalRouteOutcomeKey = route,
            SurvivalSummary = new RunSurvivalSummary { RuleResolved = true, PartySize = survivors + deaths, SurvivorCount = survivors, DeathCount = deaths },
            LootExtractionSummary = new RunLootExtractionSummary { RuleResolved = true, TotalExtractedWorldValue = loot }
        };

        [Test] public void EmptyDoesNotUseLatestOrInventTotals()
        {
            var report = ProductionAnalysisPresenter.Capture(new RunHistoryState { LatestOutcome = Outcome("test.exit", 3, 1, 9) });
            Assert.That(report.Runs, Is.Zero);
            Assert.That(ProductionAnalysisPresenter.Details(report, k => k, CultureInfo.InvariantCulture), Is.EqualTo("ui.analysis.empty"));
            Assert.That(ProductionAnalysisPresenter.Capture(null).Runs, Is.Zero);
        }

        [TestCase(1)] [TestCase(3)] [TestCase(32)]
        public void RetainedWindowUsesCorrectTotalsWithoutMutation(int retention)
        {
            var history = new RunHistoryState();
            for (int i = 0; i < retention + 5; i++) history.AppendOutcome(Outcome(i % 2 == 0 ? "test.z" : "test.a", 3, 1, 7), retention);
            string before = JsonUtility.ToJson(history);
            var report = ProductionAnalysisPresenter.Capture(history);
            Assert.That(report.Runs, Is.EqualTo(retention));
            Assert.That(report.SurvivalRuns, Is.EqualTo(retention));
            Assert.That(report.Survivors, Is.EqualTo(3L * retention)); Assert.That(report.Casualties, Is.EqualTo(retention));
            Assert.That(report.ExtractionRuns, Is.EqualTo(retention)); Assert.That(report.ExtractedValue, Is.EqualTo(7L * retention));
            Assert.That(report.RouteRuns, Is.EqualTo(retention));
            Assert.That(report.Routes.Select(r => r.Key), Is.EqualTo(report.Routes.Select(r => r.Key).OrderBy(k => k, StringComparer.Ordinal)));
            Assert.That(JsonUtility.ToJson(history), Is.EqualTo(before));
            var reopened = ProductionAnalysisPresenter.Capture(JsonUtility.FromJson<RunHistoryState>(before));
            Assert.That(reopened.ExtractedValue, Is.EqualTo(report.ExtractedValue));
            Assert.That(reopened.Routes, Is.EqualTo(report.Routes));
        }

        [Test] public void ConfiguredProductionRetentionBoundUsesExactWindow()
        {
            var config = JsonUtility.FromJson<RunSimulationConfig>(System.IO.File.ReadAllText("Assets/_Project/Data/Bootstrap/run_simulation_config.json"));
            Assert.That(config.MaxRunHistoryEntries, Is.GreaterThan(0));
            RetainedWindowUsesCorrectTotalsWithoutMutation(config.MaxRunHistoryEntries);
        }

        [Test] public void OptionalIncompleteInvalidAndNullRecordsHaveIndependentCoverage()
        {
            var bad = Outcome(null, 2, 2, -1); bad.SurvivalSummary.PartySize = 3;
            var unresolved = Outcome(null, 2, 1, 100); unresolved.SurvivalSummary.RuleResolved = false; unresolved.LootExtractionSummary.RuleResolved = false;
            var history = new RunHistoryState { RecentOutcomes = new[] { null, new RunOutcomeRecord(), Outcome("test.exit", 0, 3, 0), bad, unresolved } };
            var report = ProductionAnalysisPresenter.Capture(history);
            Assert.That(report.Runs, Is.EqualTo(4)); Assert.That(report.SurvivalRuns, Is.EqualTo(1));
            Assert.That(report.Survivors, Is.Zero); Assert.That(report.Casualties, Is.EqualTo(3));
            Assert.That(report.ExtractionRuns, Is.EqualTo(1)); Assert.That(report.ExtractedValue, Is.Zero);
            Assert.That(report.RouteRuns, Is.EqualTo(1));
        }

        [Test] public void UnknownOptionalValuesDoNotBecomeZerosOrHistoricalCauses()
        {
            var report = ProductionAnalysisPresenter.Capture(new RunHistoryState { RecentOutcomes = new[] { new RunOutcomeRecord { ReasonKey = "test.cause" } } });
            string details = ProductionAnalysisPresenter.Details(report, k => k, CultureInfo.InvariantCulture);
            Assert.That(details, Does.Contain("ui.analysis.survival_unavailable").And.Contain("ui.analysis.extraction_unavailable"));
            Assert.That(details, Does.Not.Contain("test.cause"));
            Assert.That(report.Routes, Is.Empty);
        }

        [Test] public void FormattingUsesCultureAndMissingKeysRemainSafe()
        {
            var report = ProductionAnalysisPresenter.Capture(new RunHistoryState { RecentOutcomes = new[] { Outcome("test.missing", 1000, 0, 2000) } });
            string Text(string key) => key == "ui.analysis.survival" ? "{0:N0}" : key;
            Assert.That(ProductionAnalysisPresenter.Details(report, Text, CultureInfo.GetCultureInfo("de-DE")), Does.Contain("1.000"));
            Assert.DoesNotThrow(() => ProductionAnalysisPresenter.Details(report, k => k, CultureInfo.InvariantCulture));
        }
    }
}
#endif
