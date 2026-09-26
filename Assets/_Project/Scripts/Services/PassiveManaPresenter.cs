using System;
using System.Globalization;
using System.Text;
using DungeonBuilder.M0.Economy;

namespace DungeonBuilder.M0
{
    public static class PassiveManaPresenter
    {
        private static readonly CultureInfo EnglishFormatProvider =
            CultureInfo.GetCultureInfo("en-US");

        public const string UnavailableKey = "ui.passive_mana.unavailable";
        public const string BalanceAndRateFormatKey = "ui.passive_mana.balance_rate_format";
        public const string ContributionsFormatKey = "ui.passive_mana.contributions_format";
        public const string HeatFormatKey = "ui.passive_mana.heat_format";
        public const string StorageFullKey = "ui.passive_mana.storage_full";
        public const string UnknownHeatKey = "ui.passive_mana.heat_unknown";

        public static string Build(PassiveManaRateSummary summary, double currentMana,
            double capacity, IFormatProvider formatProvider, Func<string, string> localize)
        {
            if (localize == null) throw new ArgumentNullException(nameof(localize));
            if (summary == null || !summary.RuleResolved)
                return Localized(localize, UnavailableKey);

            formatProvider = formatProvider ?? EnglishFormatProvider;

            string heatLabel = Localized(localize, summary.HeatTierId);
            if (string.IsNullOrWhiteSpace(heatLabel) ||
                string.Equals(heatLabel, summary.HeatTierId, StringComparison.Ordinal))
                heatLabel = Localized(localize, UnknownHeatKey);

            var builder = new StringBuilder();
            builder.AppendFormat(formatProvider,
                Localized(localize, BalanceAndRateFormatKey),
                PlayerManaPresentationFormatter.FormatLiveBalance(currentMana,
                    summary.ManaPerHour, formatProvider),
                PlayerManaPresentationFormatter.FormatDiscreteAmount(capacity, formatProvider),
                PlayerManaPresentationFormatter.FormatDiscreteAmount(summary.ManaPerHour,
                    formatProvider));
            builder.Append('\n');
            builder.AppendFormat(formatProvider,
                Localized(localize, ContributionsFormatKey),
                PlayerManaPresentationFormatter.FormatDiscreteAmount(
                    summary.CoreContributionManaPerHour, formatProvider), summary.ActiveFloorCount,
                PlayerManaPresentationFormatter.FormatDiscreteAmount(
                    summary.ActiveFloorContributionManaPerHour, formatProvider));
            builder.Append('\n');
            builder.AppendFormat(formatProvider,
                Localized(localize, HeatFormatKey), heatLabel,
                summary.HeatEfficiencyMultiplier);
            if (currentMana >= capacity)
            {
                builder.Append('\n');
                builder.Append(Localized(localize, StorageFullKey));
            }
            return builder.ToString();
        }

        public static IFormatProvider ResolveFormatProvider(string language)
        {
            if (!string.IsNullOrWhiteSpace(language))
            {
                try
                {
                    return CultureInfo.CreateSpecificCulture(language.Trim());
                }
                catch (CultureNotFoundException)
                {
                    // The loaded table cannot supply a valid locale; use the same English-safe
                    // fallback policy as the current localization bootstrap.
                }
            }

            return EnglishFormatProvider;
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

    /// <summary>
    /// Formats player-facing mana without changing its authoritative numeric value.
    /// </summary>
    public static class PlayerManaPresentationFormatter
    {
        public const double WholeBalancePassiveManaPerHourThreshold = 3600d;

        public static string FormatDiscreteAmount(double value, IFormatProvider formatProvider) =>
            value.ToString("0.#", formatProvider);

        public static string FormatLiveBalance(double value, double passiveManaPerHour,
            IFormatProvider formatProvider) =>
            value.ToString(passiveManaPerHour >= WholeBalancePassiveManaPerHourThreshold
                ? "0" : "0.#", formatProvider);
    }
}
