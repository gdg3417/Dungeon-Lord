using System;

namespace DungeonBuilder.M0
{
    public static class AdventurerTrafficPressurePresenter
    {
        public const string SummaryFormatKey = "ui.adventurer_traffic.summary_format";
        public const string DetailFormatKey = "ui.adventurer_traffic.detail_format";

        public static string BuildSummaryLine(AdventurerTrafficPressureSummary summary, Func<string, string, string> localize)
        {
            string bandKey = ResolveBandNameKey(summary != null && summary.RuleResolved ? summary.TrafficBandId : AdventurerTrafficPressureResolver.BandNoneId);
            string reasonKey = summary != null && summary.RuleResolved && !string.IsNullOrWhiteSpace(summary.PrimaryReasonKey) ? summary.PrimaryReasonKey : AdventurerTrafficPressureResolver.ReasonFallbackKey;
            int count = summary != null && summary.RuleResolved ? summary.EstimatedConcurrentPartyCount : 0;
            return string.Format(Localize(localize, SummaryFormatKey), Localize(localize, bandKey), count, Localize(localize, reasonKey));
        }

        public static string BuildDetailLine(AdventurerTrafficPressureSummary summary, Func<string, string, string> localize)
        {
            AdventurerTrafficPressureSummary safe = summary ?? new AdventurerTrafficPressureSummary();
            return string.Format(Localize(localize, DetailFormatKey), safe.TrafficScore, Localize(localize, ResolveBandNameKey(safe.TrafficBandId)), safe.EstimatedConcurrentPartyCount, Localize(localize, ResolvePartyBandNameKey(safe.EstimatedConcurrentPartyBandId)), AdventurerArrivalPressurePresenter.ResolveBandNameForTraffic(safe.PressureBandIdUsed, localize), RunIntentName(safe.IntentIdUsed, localize), safe.RuleSourceId ?? string.Empty, safe.DeterministicErrorCode, safe.LootSignal, safe.AttractionSignal, safe.DangerSignal, safe.HeatPressureSignal, safe.RecentDeathCount, safe.RecentRecoveredLoot, safe.PathComplete);
        }

        private static string RunIntentName(string intentId, Func<string, string, string> localize)
        {
            if (string.Equals(intentId, RunPostureResolver.CautiousId, StringComparison.Ordinal)) return Localize(localize, "run.posture.cautious.name");
            if (string.Equals(intentId, RunPostureResolver.GreedyId, StringComparison.Ordinal)) return Localize(localize, "run.posture.greedy.name");
            return Localize(localize, "run.posture.balanced.name");
        }

        private static string ResolveBandNameKey(string bandId)
        {
            if (string.Equals(bandId, AdventurerTrafficPressureResolver.BandLowId, StringComparison.Ordinal)) return "ui.adventurer_traffic.band.low";
            if (string.Equals(bandId, AdventurerTrafficPressureResolver.BandBuildingId, StringComparison.Ordinal)) return "ui.adventurer_traffic.band.building";
            if (string.Equals(bandId, AdventurerTrafficPressureResolver.BandSteadyId, StringComparison.Ordinal)) return "ui.adventurer_traffic.band.steady";
            if (string.Equals(bandId, AdventurerTrafficPressureResolver.BandHeavyId, StringComparison.Ordinal)) return "ui.adventurer_traffic.band.heavy";
            if (string.Equals(bandId, AdventurerTrafficPressureResolver.BandDangerousChurnId, StringComparison.Ordinal)) return "ui.adventurer_traffic.band.dangerous_churn";
            return "ui.adventurer_traffic.band.none";
        }

        private static string ResolvePartyBandNameKey(string bandId)
        {
            if (string.Equals(bandId, AdventurerTrafficPressureResolver.PartyBandLowId, StringComparison.Ordinal)) return "ui.adventurer_traffic.party_band.low";
            if (string.Equals(bandId, AdventurerTrafficPressureResolver.PartyBandMediumId, StringComparison.Ordinal)) return "ui.adventurer_traffic.party_band.medium";
            if (string.Equals(bandId, AdventurerTrafficPressureResolver.PartyBandHighId, StringComparison.Ordinal)) return "ui.adventurer_traffic.party_band.high";
            return "ui.adventurer_traffic.party_band.none";
        }

        private static string Localize(Func<string, string, string> localize, string key) => localize == null ? key : localize(key, key);
    }
}
