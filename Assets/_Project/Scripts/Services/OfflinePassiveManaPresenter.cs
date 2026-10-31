using System;
using System.Globalization;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0
{
    // Presentation evidence only. Neither this receipt nor its accumulator can
    // publish, retry, spend, classify time, or confer canonical authority.
    internal sealed class RecoveryGrantReceipt
    {
        internal readonly string Predecessor, Candidate, Context, Phase;
        internal readonly RecoveryEpisode Episode;
        internal readonly long Eligible100ns;
        internal readonly double Rate, Calculated, Awarded, Spent, Wallet, Capacity;
        internal readonly bool CapacityLimited;

        internal RecoveryGrantReceipt(byte[] predecessor, byte[] candidate, string context,
            RecoveryEpisode episode, long eligible, OfflinePassiveManaResult grant, double spent, double wallet)
        {
            Predecessor = SpatialContractSha256.Compute(predecessor);
            Candidate = SpatialContractSha256.Compute(candidate); Context = context;
            Episode = episode; Phase = episode.Phase; Eligible100ns = eligible;
            Rate = grant.EffectiveOfflineManaPerHour; Calculated = grant.CalculatedPreCapacityAward;
            Awarded = grant.ActualAwardedMana; CapacityLimited = grant.CapacityLimited;
            Spent = spent; Wallet = wallet; Capacity = grant.Capacity;
        }

        internal bool Matches(DetachedCanonicalWriteResult proof) => proof?.IsSuccess == true &&
            proof.Session != null && Candidate == SpatialContractSha256.Compute(proof.GetPersistedBytes()) &&
            Candidate == SpatialContractSha256.Compute(proof.Session.GetCurrentBytes()) &&
            proof.Validation?.TimeProvenance?.RecoveryContextSha256 == Context &&
            proof.RuntimeProjection?.structureRuntime?.ManaReserve == Wallet &&
            (Phase == RecoveryEpisode.OriginalPending
                ? RecoveryEpisodeReport.SameEpisode(Episode, proof.Validation.TimeProvenance.Episode)
                    && proof.Validation.TimeProvenance.Episode.Phase == RecoveryEpisode.ResidualPending
                : Phase == RecoveryEpisode.ResidualPending && proof.Validation.TimeProvenance.Episode == null);
    }

    // A single bounded, process-local aggregate: two receipts at most, no history.
    // Missing evidence is distinct from a proven zero. Completed evidence survives
    // ordinary wallet refreshes, but a new recovery starts a fresh aggregate.
    internal sealed class RecoveryEpisodeReport
    {
        private string expected;
        private RecoveryEpisode episode;
        private RecoveryGrantReceipt original, residual;
        internal bool Closed { get; private set; }
        internal bool Complete => Closed && original != null && residual != null;
        internal decimal Eligible100ns => (decimal)original.Eligible100ns + residual.Eligible100ns;
        internal double Awarded => original.Awarded + residual.Awarded;
        internal double Calculated => original.Calculated + residual.Calculated;
        internal double Spent => original.Spent + residual.Spent;
        internal bool CapacityLimited => original.CapacityLimited || residual.CapacityLimited;
        internal double OriginalRate => original.Rate;
        internal double ResidualRate => residual.Rate;
        internal double Capacity => residual.Capacity;
        internal double ClosingWallet => residual.Wallet;

        internal RecoveryEpisodeReport(byte[] source, RecoveryEpisode existing)
        { expected = SpatialContractSha256.Compute(source); episode = existing; }

        internal static bool SameEpisode(RecoveryEpisode a, RecoveryEpisode b) => a != null && b != null &&
            a.EntryPredecessorSha256 == b.EntryPredecessorSha256 && a.Source.CompareTo(b.Source) == 0 &&
            a.Entry.CompareTo(b.Entry) == 0 && a.EntryContext == b.EntryContext &&
            a.OriginalMode == b.OriginalMode && a.OriginalEligible100ns == b.OriginalEligible100ns;

        internal void Observe(byte[] predecessor, DetachedCanonicalWriteResult proof, RecoveryGrantReceipt receipt)
        {
            if (proof?.IsSuccess != true) return;
            string candidate = SpatialContractSha256.Compute(proof.GetPersistedBytes());
            if (candidate == expected) return; // Duplicate acknowledgement.
            var next = proof.Validation.TimeProvenance.Episode;
            bool continuous = expected == SpatialContractSha256.Compute(predecessor) &&
                (episode == null || next == null || SameEpisode(episode, next));
            if (!continuous) { original = null; residual = null; episode = null; }
            if (episode == null) episode = next;
            if (continuous && receipt != null && receipt.Predecessor == expected &&
                SameEpisode(episode, receipt.Episode) && receipt.Matches(proof)) {
                if (receipt.Phase == RecoveryEpisode.OriginalPending && original == null && residual == null)
                    original = receipt;
                else if (receipt.Phase == RecoveryEpisode.ResidualPending && residual == null)
                    residual = receipt;
            }
            expected = candidate;
            Closed = next == null;
        }
    }

    public static class OfflinePassiveManaPresenter
    {
        internal static string BuildRecovery(RecoveryEpisodeReport report, double wallet, double capacity,
            IFormatProvider provider, Func<string, string> localize)
        {
            if (report?.Closed != true || localize == null) return string.Empty;
            provider = provider ?? CultureInfo.GetCultureInfo("en-US");
            Func<double, string> amount = n => PlayerManaPresentationFormatter.FormatDiscreteAmount(n, provider);
            string neutral = Localized(localize, "ui.recovery.completed_format");
            if (!report.Complete) return string.IsNullOrEmpty(neutral) ? string.Empty :
                string.Format(provider, neutral, amount(wallet), amount(capacity));
            string format = Localized(localize, "ui.recovery.summary_format");
            string rateFormat = Localized(localize, report.OriginalRate == report.ResidualRate
                ? "ui.recovery.rate_format" : "ui.recovery.rates_format");
            if (string.IsNullOrEmpty(format) || string.IsNullOrEmpty(rateFormat))
                return string.IsNullOrEmpty(neutral) ? string.Empty : string.Format(provider, neutral, amount(wallet), amount(capacity));
            // Decimal division preserves every 100 ns digit, including fractional
            // parts spanning the two settlements; no per-receipt second rounding.
            string duration = ((decimal)report.Eligible100ns / 10000000m).ToString("#,0.#######", provider);
            string rate = string.Format(provider, rateFormat, amount(report.OriginalRate), amount(report.ResidualRate));
            string text = string.Format(provider, format, duration, rate, amount(report.Awarded), amount(wallet), amount(capacity));
            if (report.CapacityLimited) {
                string limited = Localized(localize, CapacityLimitedKey);
                if (!string.IsNullOrEmpty(limited)) text += "\n" + limited;
            }
            if (report.Spent > 0) {
                string spent = Localized(localize, "combat.c2.recovery_spent");
                if (!string.IsNullOrEmpty(spent)) text += "\n" + string.Format(provider, spent, amount(report.Spent));
            }
            return text;
        }
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
                        result.PublishedWallet, formatProvider),
                    PlayerManaPresentationFormatter.FormatDiscreteAmount(
                        result.Capacity, formatProvider));
                if (result.CapacityLimited)
                {
                    string limited = Localized(localize, CapacityLimitedKey);
                    if (!string.IsNullOrEmpty(limited)) text += "\n" + limited;
                }
                if (result.RecoveryManaSpent > 0) text += "\n" + string.Format(formatProvider,
                    Localized(localize, "combat.c2.recovery_spent"), PlayerManaPresentationFormatter.FormatDiscreteAmount(result.RecoveryManaSpent, formatProvider));
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
