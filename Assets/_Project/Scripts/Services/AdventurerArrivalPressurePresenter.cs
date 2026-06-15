using System;

namespace DungeonBuilder.M0
{
    public static class AdventurerArrivalPressurePresenter
    {
        public const string SummaryFormatKey = "ui.adventurer_pressure.summary_format";
        public const string BodyFormatKey = "ui.adventurer_pressure.body_format";
        public const string DetailFormatKey = "ui.adventurer_pressure.detail_format";

        public static string BuildSummaryLine(AdventurerArrivalPressureSummary summary, Func<string, string, string> localize)
        {
            string bandKey = ResolveBandNameKey(summary != null && summary.RuleResolved ? summary.PressureBandId : AdventurerArrivalPressureResolver.BandNotYetId);
            string reasonKey = summary != null && summary.RuleResolved && !string.IsNullOrWhiteSpace(summary.PrimaryReasonKey) ? summary.PrimaryReasonKey : AdventurerArrivalPressureResolver.ReasonNotYetKey;
            return string.Format(Localize(localize, SummaryFormatKey), Localize(localize, bandKey), Localize(localize, reasonKey));
        }

        public static string BuildBodyLine(AdventurerArrivalPressureSummary summary, Func<string, string, string> localize)
        {
            string bandKey = ResolveBandNameKey(summary != null && summary.RuleResolved ? summary.PressureBandId : AdventurerArrivalPressureResolver.BandNotYetId);
            string reasonKey = summary != null && summary.RuleResolved && !string.IsNullOrWhiteSpace(summary.PrimaryReasonKey) ? summary.PrimaryReasonKey : AdventurerArrivalPressureResolver.ReasonNotYetKey;
            return string.Format(Localize(localize, BodyFormatKey), Localize(localize, bandKey), Localize(localize, reasonKey));
        }

        public static string BuildDetailLine(AdventurerArrivalPressureSummary summary, Func<string, string, string> localize)
        {
            AdventurerArrivalPressureSummary safe = summary ?? new AdventurerArrivalPressureSummary();
            return string.Format(Localize(localize, DetailFormatKey), safe.Score, Localize(localize, ResolveBandNameKey(safe.PressureBandId)), safe.RuleSourceId ?? string.Empty, safe.DeterministicErrorCode, safe.LootSignal, safe.AttractionSignal, safe.DangerSignal, safe.HeatPressureSignal, safe.RecentDeathCount, safe.RecentRecoveredLoot, safe.PathComplete, Localize(localize, ResolveOutcomeKey(safe.LatestRunOutcomeId)));
        }

        private static string ResolveOutcomeKey(string outcomeId)
        {
            if (string.Equals(outcomeId, "run.outcome.success", StringComparison.Ordinal)) return "ui.adventurer_pressure.outcome.success";
            if (string.Equals(outcomeId, "run.outcome.failure", StringComparison.Ordinal)) return "ui.adventurer_pressure.outcome.failure";
            return "ui.adventurer_pressure.outcome.none";
        }

        public static string ResolveBandNameKey(string bandId)
        {
            if (string.Equals(bandId, AdventurerArrivalPressureResolver.BandLowId, StringComparison.Ordinal)) return "ui.adventurer_pressure.band.low";
            if (string.Equals(bandId, AdventurerArrivalPressureResolver.BandCautiousId, StringComparison.Ordinal)) return "ui.adventurer_pressure.band.cautious_interest";
            if (string.Equals(bandId, AdventurerArrivalPressureResolver.BandBuildingId, StringComparison.Ordinal)) return "ui.adventurer_pressure.band.building_slowly";
            if (string.Equals(bandId, AdventurerArrivalPressureResolver.BandLikelySoonId, StringComparison.Ordinal)) return "ui.adventurer_pressure.band.likely_soon";
            return "ui.adventurer_pressure.band.not_yet";
        }

        public static string ResolveBandNameForTraffic(string bandId, Func<string, string, string> localize) => Localize(localize, ResolveBandNameKey(bandId));

        private static string Localize(Func<string, string, string> localize, string key) => localize == null ? key : localize(key, key);
    }
}
