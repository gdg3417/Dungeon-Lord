using System;
using System.Globalization;
using DungeonBuilder.M0.Economy;

namespace DungeonBuilder.M0
{
    public static class OfflinePassiveManaPresenter
    {
        public const string AppliedFormatKey = "ui.offline_mana.applied_format";
        public const string CapacityLimitedKey = "ui.offline_mana.capacity_limited";
        public const string NoAwardFormatKey = "ui.offline_mana.no_award_format";
        public const string AppliedReasonKey = "ui.offline_mana.reason.applied";
        public const string ZeroElapsedReasonKey = "ui.offline_mana.reason.zero_elapsed";
        public const string AlreadyAtCapacityReasonKey = "ui.offline_mana.reason.at_capacity";
        public const string InvalidTimeReasonKey = "ui.offline_mana.reason.invalid_time";
        public const string UnavailableReasonKey = "ui.offline_mana.reason.unavailable";
        public const string PersistenceFailureReasonKey = "ui.offline_mana.reason.persistence_failure";
        public const string NoGenerationReasonKey = "ui.offline_mana.reason.no_generation";

        public static string Build(OfflinePassiveManaResult result,
            IFormatProvider formatProvider, Func<string, string> localize)
        {
            if (result == null || localize == null) return string.Empty;
            formatProvider = formatProvider ?? CultureInfo.GetCultureInfo("en-US");
            string reason = Localized(localize, ReasonKey(result.Reason));

            if (result.Persisted && (result.Reason == OfflinePassiveManaReason.Applied ||
                result.Reason == OfflinePassiveManaReason.AlreadyAtCapacity))
            {
                string format = Localized(localize, AppliedFormatKey);
                if (string.IsNullOrEmpty(format)) return string.Empty;
                string text = string.Format(formatProvider, format,
                    result.ObservedElapsedSeconds,
                    PlayerManaPresentationFormatter.FormatDiscreteAmount(
                        result.EffectiveOfflineManaPerHour, formatProvider),
                    PlayerManaPresentationFormatter.FormatDiscreteAmount(
                        result.ActualAwardedMana, formatProvider),
                    PlayerManaPresentationFormatter.FormatDiscreteAmount(
                        result.WalletAfter, formatProvider),
                    PlayerManaPresentationFormatter.FormatDiscreteAmount(
                        result.Capacity, formatProvider));
                if (result.CapacityLimited)
                {
                    string limited = Localized(localize, CapacityLimitedKey);
                    if (!string.IsNullOrEmpty(limited)) text += "\n" + limited;
                }
                return text;
            }

            string noAward = Localized(localize, NoAwardFormatKey);
            return string.IsNullOrEmpty(noAward) || string.IsNullOrEmpty(reason)
                ? string.Empty
                : string.Format(formatProvider, noAward, reason);
        }

        private static string ReasonKey(OfflinePassiveManaReason reason)
        {
            switch (reason)
            {
                case OfflinePassiveManaReason.Applied: return AppliedReasonKey;
                case OfflinePassiveManaReason.ZeroElapsedInterval: return ZeroElapsedReasonKey;
                case OfflinePassiveManaReason.AlreadyAtCapacity: return AlreadyAtCapacityReasonKey;
                case OfflinePassiveManaReason.CurrentTimestampInvalid:
                case OfflinePassiveManaReason.SavedTimestampInvalid:
                case OfflinePassiveManaReason.BackwardClockMovement:
                    return InvalidTimeReasonKey;
                case OfflinePassiveManaReason.PersistenceFailure:
                case OfflinePassiveManaReason.StaleSession:
                    return PersistenceFailureReasonKey;
                case OfflinePassiveManaReason.NoManaGenerated:
                    return NoGenerationReasonKey;
                default: return UnavailableReasonKey;
            }
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
