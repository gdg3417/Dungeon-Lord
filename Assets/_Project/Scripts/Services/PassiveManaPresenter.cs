using System;
using System.Globalization;
using System.Text;
using DungeonBuilder.M0.Economy;

namespace DungeonBuilder.M0
{
    public static class PassiveManaPresenter
    {
        public const string UnavailableKey = "ui.passive_mana.unavailable";
        public const string BalanceAndRateFormatKey = "ui.passive_mana.balance_rate_format";
        public const string ContributionsFormatKey = "ui.passive_mana.contributions_format";
        public const string HeatFormatKey = "ui.passive_mana.heat_format";
        public const string StorageFullKey = "ui.passive_mana.storage_full";
        public const string UnknownHeatKey = "ui.passive_mana.heat_unknown";

        public static string Build(PassiveManaRateSummary summary, double currentMana,
            double capacity, Func<string, string> localize)
        {
            if (localize == null) throw new ArgumentNullException(nameof(localize));
            if (summary == null || !summary.RuleResolved)
                return Localized(localize, UnavailableKey);

            string heatLabel = Localized(localize, summary.HeatTierId);
            if (string.IsNullOrWhiteSpace(heatLabel) ||
                string.Equals(heatLabel, summary.HeatTierId, StringComparison.Ordinal))
                heatLabel = Localized(localize, UnknownHeatKey);

            var builder = new StringBuilder();
            builder.AppendFormat(CultureInfo.InvariantCulture,
                Localized(localize, BalanceAndRateFormatKey), currentMana, capacity,
                summary.ManaPerHour);
            builder.Append('\n');
            builder.AppendFormat(CultureInfo.InvariantCulture,
                Localized(localize, ContributionsFormatKey),
                summary.CoreContributionManaPerHour, summary.ActiveFloorCount,
                summary.ActiveFloorContributionManaPerHour);
            builder.Append('\n');
            builder.AppendFormat(CultureInfo.InvariantCulture,
                Localized(localize, HeatFormatKey), heatLabel,
                summary.HeatEfficiencyMultiplier);
            if (currentMana >= capacity)
            {
                builder.Append('\n');
                builder.Append(Localized(localize, StorageFullKey));
            }
            return builder.ToString();
        }

        private static string Localized(Func<string, string> localize, string key)
        {
            string value = localize(key);
            return string.IsNullOrWhiteSpace(value) ||
                string.Equals(value, key, StringComparison.Ordinal)
                ? string.Empty
                : value;
        }
    }
}
