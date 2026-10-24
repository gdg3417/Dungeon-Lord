using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0
{
    // Detached, read-only projection of the retained window. Never falls back to LatestOutcome:
    // it may overlap the window or belong to a different historical save contract.
    public sealed class ProductionAnalysisReport
    {
        public int Runs { get; internal set; }
        public int SurvivalRuns { get; internal set; }
        public long Survivors { get; internal set; }
        public long Casualties { get; internal set; }
        public int ExtractionRuns { get; internal set; }
        public long ExtractedValue { get; internal set; }
        public int RouteRuns { get; internal set; }
        public IReadOnlyList<KeyValuePair<string, int>> Routes { get; internal set; }
    }

    public static class ProductionAnalysisPresenter
    {
        public static ProductionAnalysisReport Capture(RunHistoryState history)
        {
            var result = new ProductionAnalysisReport();
            var routes = new SortedDictionary<string, int>(StringComparer.Ordinal);
            // RecentOutcomes is bounded by the existing history owner, in append order.
            foreach (var outcome in history?.RecentOutcomes ?? Array.Empty<RunOutcomeRecord>())
            {
                if (outcome == null) continue;
                result.Runs++;
                var survival = outcome.SurvivalSummary;
                if (survival?.RuleResolved == true && survival.DeterministicErrorCode == 0 &&
                    survival.PartySize > 0 && survival.SurvivorCount >= 0 && survival.DeathCount >= 0 &&
                    (long)survival.SurvivorCount + survival.DeathCount == survival.PartySize)
                {
                    result.SurvivalRuns++; result.Survivors += survival.SurvivorCount;
                    result.Casualties += survival.DeathCount;
                }
                var extraction = outcome.LootExtractionSummary;
                if (extraction?.RuleResolved == true && extraction.DeterministicErrorCode == 0 && extraction.TotalExtractedWorldValue >= 0)
                { result.ExtractionRuns++; result.ExtractedValue += extraction.TotalExtractedWorldValue; }
                if (!string.IsNullOrWhiteSpace(outcome.FinalRouteOutcomeKey))
                {
                    result.RouteRuns++;
                    routes.TryGetValue(outcome.FinalRouteOutcomeKey, out int count);
                    routes[outcome.FinalRouteOutcomeKey] = count + 1;
                }
            }
            result.Routes = Array.AsReadOnly(routes.ToArray());
            return result;
        }

        public static string Compact(ProductionAnalysisReport report, Func<string, string> text, IFormatProvider culture) =>
            report.Runs == 0 ? text("ui.analysis.empty_short") : report.SurvivalRuns == 0 ?
                ProductionDungeonPresenter.Format(text, culture, "ui.analysis.compact_runs", report.Runs) :
                ProductionDungeonPresenter.Format(text, culture, "ui.analysis.compact", report.Runs, report.Survivors, report.Casualties, report.SurvivalRuns);

        public static string Details(ProductionAnalysisReport report, Func<string, string> text, IFormatProvider culture)
        {
            string F(string key, params object[] args) => ProductionDungeonPresenter.Format(text, culture, key, args);
            if (report.Runs == 0) return text("ui.analysis.empty");
            var lines = new List<string> { F("ui.analysis.window", report.Runs), text("ui.analysis.window_limit") };
            lines.Add(report.SurvivalRuns == 0 ? text("ui.analysis.survival_unavailable") :
                F("ui.analysis.survival", report.Survivors, report.Casualties, report.SurvivalRuns, report.Runs));
            lines.Add(report.ExtractionRuns == 0 ? text("ui.analysis.extraction_unavailable") :
                F("ui.analysis.extraction", report.ExtractedValue, report.ExtractionRuns, report.Runs));
            lines.Add(F("ui.analysis.routes", report.RouteRuns, report.Runs));
            foreach (var route in report.Routes) lines.Add(F("ui.analysis.route", text(route.Key), route.Value));
            return string.Join("\n\n", lines);
        }
    }
}
