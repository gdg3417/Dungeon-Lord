using System;

namespace DungeonBuilder.M0.Economy
{
    public enum OfflinePassiveManaReason
    {
        Applied = 0,
        ZeroElapsedInterval = 1,
        AlreadyAtCapacity = 2,
        CurrentTimestampInvalid = 3,
        SavedTimestampInvalid = 4,
        BackwardClockMovement = 5,
        CanonicalStateOrConfigurationInvalid = 6,
        StaleSession = 7,
        PersistenceFailure = 8,
        NoManaGenerated = 9
    }

    /// <summary>
    /// Deterministic evidence for one offline grant attempt. This is derived evidence, not a
    /// writable gameplay authority or proof that a local clock is trustworthy.
    /// </summary>
    public sealed class OfflinePassiveManaResult
    {
        public OfflinePassiveManaReason Reason { get; internal set; }
        public long SourceSavedUtcUnix { get; internal set; }
        public long ObservedCurrentUtcUnix { get; internal set; }
        public long ObservedElapsedSeconds { get; internal set; }
        public double ApplicableOnlineManaPerHour { get; internal set; }
        public double ConfiguredBaseOfflineEfficiency { get; internal set; }
        public double EffectiveOfflineEfficiency { get; internal set; }
        public double EffectiveOfflineManaPerHour { get; internal set; }
        public double CalculatedPreCapacityAward { get; internal set; }
        public double ActualAwardedMana { get; internal set; }
        public double WalletBefore { get; internal set; }
        public double WalletAfter { get; internal set; }
        public double RecoveryManaSpent { get; internal set; }
        public double PublishedWallet => WalletAfter - RecoveryManaSpent;
        public double Capacity { get; internal set; }
        public bool CapacityLimited { get; internal set; }
        public bool PersistenceRequired { get; internal set; }
        public bool Persisted { get; internal set; }
        public PassiveManaRateSummary CanonicalRate { get; internal set; }

        public bool CalculationAccepted => Reason == OfflinePassiveManaReason.Applied ||
            Reason == OfflinePassiveManaReason.AlreadyAtCapacity ||
            Reason == OfflinePassiveManaReason.NoManaGenerated;

        internal OfflinePassiveManaResult WithPersistence(
            OfflinePassiveManaReason reason, bool persisted, double recoveryManaSpent = 0)
        {
            return new OfflinePassiveManaResult
            {
                Reason = reason,
                SourceSavedUtcUnix = SourceSavedUtcUnix,
                ObservedCurrentUtcUnix = ObservedCurrentUtcUnix,
                ObservedElapsedSeconds = ObservedElapsedSeconds,
                ApplicableOnlineManaPerHour = ApplicableOnlineManaPerHour,
                ConfiguredBaseOfflineEfficiency = ConfiguredBaseOfflineEfficiency,
                EffectiveOfflineEfficiency = EffectiveOfflineEfficiency,
                EffectiveOfflineManaPerHour = EffectiveOfflineManaPerHour,
                CalculatedPreCapacityAward = CalculatedPreCapacityAward,
                ActualAwardedMana = ActualAwardedMana,
                WalletBefore = WalletBefore,
                WalletAfter = WalletAfter,
                RecoveryManaSpent = recoveryManaSpent,
                Capacity = Capacity,
                CapacityLimited = CapacityLimited,
                PersistenceRequired = PersistenceRequired,
                Persisted = persisted,
                CanonicalRate = CanonicalRate
            };
        }
    }

    /// <summary>
    /// Resolves a single offline award from the canonical online mana/hour result. The caller
    /// supplies one captured current timestamp and an effective efficiency so a future approved
    /// research authority can modify the configured base without changing lifecycle/persistence.
    /// </summary>
    public sealed class CanonicalOfflinePassiveManaService
    {
        private const double SecondsPerHour = 3600d;
        private readonly PassiveOnlineManaConfigurationSnapshot configuration;
        private readonly CanonicalPassiveManaService canonicalOnline;

        public CanonicalOfflinePassiveManaService(
            PassiveOnlineManaConfigurationSnapshot configuration,
            CanonicalPassiveManaService canonicalOnline)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.canonicalOnline = canonicalOnline ?? throw new ArgumentNullException(nameof(canonicalOnline));
        }

        public OfflinePassiveManaResult Resolve(SaveData save,
            RunSimulationConfig heatConfiguration, long observedCurrentUtcUnix) =>
            Resolve(save, heatConfiguration, observedCurrentUtcUnix,
                configuration.BaseOfflineEfficiency);

        public OfflinePassiveManaResult Resolve(SaveData save,
            RunSimulationConfig heatConfiguration, long observedCurrentUtcUnix,
            double effectiveOfflineEfficiency)
        {
            long sourceTimestamp = save?.lastSavedUtcUnix ?? 0;
            double wallet = save?.structureRuntime?.ManaReserve ?? double.NaN;
            var result = new OfflinePassiveManaResult
            {
                Reason = OfflinePassiveManaReason.CanonicalStateOrConfigurationInvalid,
                SourceSavedUtcUnix = sourceTimestamp,
                ObservedCurrentUtcUnix = observedCurrentUtcUnix,
                ConfiguredBaseOfflineEfficiency = configuration.BaseOfflineEfficiency,
                EffectiveOfflineEfficiency = effectiveOfflineEfficiency,
                WalletBefore = wallet,
                WalletAfter = wallet,
                Capacity = canonicalOnline.ManaCapacity
            };

            if (observedCurrentUtcUnix <= 0 || observedCurrentUtcUnix > UnixTimePoint.MaximumSeconds)
                return Reject(result, OfflinePassiveManaReason.CurrentTimestampInvalid);
            if (sourceTimestamp <= 0 || sourceTimestamp > UnixTimePoint.MaximumSeconds)
                return Reject(result, OfflinePassiveManaReason.SavedTimestampInvalid);
            if (observedCurrentUtcUnix < sourceTimestamp)
                return Reject(result, OfflinePassiveManaReason.BackwardClockMovement);
            result.ObservedElapsedSeconds = observedCurrentUtcUnix - sourceTimestamp;
            if (result.ObservedElapsedSeconds == 0)
                return Reject(result, OfflinePassiveManaReason.ZeroElapsedInterval);
            if (!FiniteNonnegative(wallet) || wallet > result.Capacity ||
                !FiniteNonnegative(effectiveOfflineEfficiency))
                return result;

            PassiveManaRateSummary rate = canonicalOnline.ResolveRate(save, heatConfiguration);
            return Calculate(result, rate, result.ObservedElapsedSeconds);
        }

        // Calculation only. This is deliberately not an overload of Resolve and
        // does not return the result type accepted by canonical publication.
        // Explicit endpoints do not establish interval eligibility or authority.
        internal PreciseOfflinePassiveManaCalculation CalculatePrecise(SaveData intervalStart,
            RunSimulationConfig heatConfiguration, UnixTimePoint source, UnixTimePoint endpoint,
            double? effectiveOfflineEfficiency = null)
        {
            double wallet = intervalStart?.structureRuntime?.ManaReserve ?? double.NaN;
            var result = new OfflinePassiveManaResult {
                Reason = OfflinePassiveManaReason.CanonicalStateOrConfigurationInvalid,
                WalletBefore = wallet, WalletAfter = wallet, Capacity = canonicalOnline.ManaCapacity,
                ConfiguredBaseOfflineEfficiency = configuration.BaseOfflineEfficiency,
                EffectiveOfflineEfficiency = effectiveOfflineEfficiency ?? configuration.BaseOfflineEfficiency };
            long elapsed100ns = 0;
            if (endpoint == null || endpoint.Seconds <= 0)
                result.Reason = OfflinePassiveManaReason.CurrentTimestampInvalid;
            else if (source == null || source.Seconds <= 0)
                result.Reason = OfflinePassiveManaReason.SavedTimestampInvalid;
            else if (endpoint.CompareTo(source) < 0)
                result.Reason = OfflinePassiveManaReason.BackwardClockMovement;
            else {
                // UnixTimePoint validates representability. Subtract checked
                // integers before entering the existing double formula domain.
                try { elapsed100ns = endpoint.Elapsed100nsSince(source); }
                catch (OverflowException) {
                    return new PreciseOfflinePassiveManaCalculation(result, 0);
                }
                if (elapsed100ns == 0) result.Reason = OfflinePassiveManaReason.ZeroElapsedInterval;
                else if (FiniteNonnegative(wallet) && wallet <= result.Capacity &&
                    FiniteNonnegative(result.EffectiveOfflineEfficiency))
                    Calculate(result, canonicalOnline.ResolveRate(intervalStart, heatConfiguration),
                        elapsed100ns / UnixTimePoint.UnitsPerSecond +
                        (double)(elapsed100ns % UnixTimePoint.UnitsPerSecond) / UnixTimePoint.UnitsPerSecond);
            }
            return new PreciseOfflinePassiveManaCalculation(result, elapsed100ns);
        }

        // One formula shared by live legacy grants and read-only calculations.
        // Legacy callers still pass whole seconds with the original operation order.
        private static OfflinePassiveManaResult Calculate(OfflinePassiveManaResult result,
            PassiveManaRateSummary rate, double elapsedSeconds)
        {
            double wallet = result.WalletBefore;
            double effectiveOfflineEfficiency = result.EffectiveOfflineEfficiency;
            result.CanonicalRate = rate;
            if (rate == null || !rate.RuleResolved || !FiniteNonnegative(rate.ManaPerHour))
                return result;

            result.ApplicableOnlineManaPerHour = rate.ManaPerHour;
            result.EffectiveOfflineManaPerHour = rate.ManaPerHour * effectiveOfflineEfficiency;
            result.CalculatedPreCapacityAward = result.EffectiveOfflineManaPerHour *
                elapsedSeconds / SecondsPerHour;
            if (!FiniteNonnegative(result.EffectiveOfflineManaPerHour) ||
                !FiniteNonnegative(result.CalculatedPreCapacityAward))
                return result;

            double candidateWallet = wallet + result.CalculatedPreCapacityAward;
            result.CapacityLimited = candidateWallet > result.Capacity;
            result.WalletAfter = Math.Min(result.Capacity, candidateWallet);
            result.ActualAwardedMana = result.WalletAfter - wallet;
            result.PersistenceRequired = true;
            result.Reason = result.ActualAwardedMana > 0d
                ? OfflinePassiveManaReason.Applied
                : wallet >= result.Capacity
                    ? OfflinePassiveManaReason.AlreadyAtCapacity
                    : OfflinePassiveManaReason.NoManaGenerated;
            return result;
        }

        // Publication callers must prove the episode phase/duration independently.
        // A zero-duration obligation still closes durably and performs the one
        // applicable recovery pass. Grouped durations have no fabricated UTC end.
        internal OfflinePassiveManaResult ResolveRecoveryDuration(SaveData save,
            RunSimulationConfig heatConfiguration, long eligible100ns, bool legacyWholeSeconds)
        {
            double wallet = save?.structureRuntime?.ManaReserve ?? double.NaN;
            var result = new OfflinePassiveManaResult {
                Reason = OfflinePassiveManaReason.CanonicalStateOrConfigurationInvalid,
                SourceSavedUtcUnix = save?.lastSavedUtcUnix ?? 0,
                ObservedElapsedSeconds = eligible100ns / UnixTimePoint.UnitsPerSecond,
                WalletBefore = wallet, WalletAfter = wallet, Capacity = canonicalOnline.ManaCapacity,
                ConfiguredBaseOfflineEfficiency = configuration.BaseOfflineEfficiency,
                EffectiveOfflineEfficiency = configuration.BaseOfflineEfficiency };
            if (eligible100ns < 0 || eligible100ns >
                new UnixTimePoint(UnixTimePoint.MaximumSeconds, 9999999).Elapsed100nsSince(new UnixTimePoint(1, 0)) ||
                legacyWholeSeconds && eligible100ns % UnixTimePoint.UnitsPerSecond != 0 ||
                !FiniteNonnegative(wallet) || wallet > result.Capacity) return result;
            double elapsed = eligible100ns / UnixTimePoint.UnitsPerSecond;
            if (!legacyWholeSeconds) elapsed += (double)(eligible100ns % UnixTimePoint.UnitsPerSecond) / UnixTimePoint.UnitsPerSecond;
            return Calculate(result, canonicalOnline.ResolveRate(save, heatConfiguration), elapsed);
        }

        public HistoricalOfflineManaInterval CaptureHistoricalInterval(SaveData intervalStart,
            RunSimulationConfig heatConfiguration, long acceptedEndUtcUnix)
        {
            var accepted = Resolve(intervalStart, heatConfiguration, acceptedEndUtcUnix);
            if (!accepted.CalculationAccepted) throw new ArgumentException("mana.history.invalid_interval");
            return new HistoricalOfflineManaInterval(accepted);
        }

        public OfflinePassiveManaResult EvaluateHistorical(HistoricalOfflineManaInterval interval, long admissionUtcUnix)
        {
            if (interval == null || admissionUtcUnix < interval.StartUtcUnix || admissionUtcUnix > interval.EndUtcUnix)
                throw new ArgumentException("mana.history.invalid_timestamp");
            var result = new OfflinePassiveManaResult {
                Reason = OfflinePassiveManaReason.CanonicalStateOrConfigurationInvalid,
                SourceSavedUtcUnix = interval.StartUtcUnix, ObservedCurrentUtcUnix = admissionUtcUnix,
                ObservedElapsedSeconds = admissionUtcUnix - interval.StartUtcUnix,
                ConfiguredBaseOfflineEfficiency = interval.BaseEfficiency,
                EffectiveOfflineEfficiency = interval.EffectiveEfficiency,
                WalletBefore = interval.Wallet, WalletAfter = interval.Wallet, Capacity = interval.Capacity };
            Calculate(result, interval.InspectRate(), result.ObservedElapsedSeconds);
            result.PersistenceRequired = false;
            return result;
        }

        private static OfflinePassiveManaResult Reject(OfflinePassiveManaResult result,
            OfflinePassiveManaReason reason)
        {
            result.Reason = reason;
            return result;
        }

        private static bool FiniteNonnegative(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
    }

    // Immutable calculation evidence, never spendable or accepted by
    // SaveService.CommitOfflinePassiveMana. No precise publication API exists.
    internal sealed class PreciseOfflinePassiveManaCalculation
    {
        internal OfflinePassiveManaReason Reason { get; }
        internal long Elapsed100ns { get; }
        internal double ApplicableOnlineManaPerHour { get; }
        internal double ConfiguredBaseOfflineEfficiency { get; }
        internal double EffectiveOfflineEfficiency { get; }
        internal double EffectiveOfflineManaPerHour { get; }
        internal double CalculatedPreCapacityAward { get; }
        internal double ActualAwardedMana { get; }
        internal double WalletBefore { get; }
        internal double WalletAfter { get; }
        internal double Capacity { get; }
        internal bool CapacityLimited { get; }

        internal PreciseOfflinePassiveManaCalculation(OfflinePassiveManaResult result, long elapsed100ns)
        {
            Reason = result.Reason; Elapsed100ns = elapsed100ns;
            ApplicableOnlineManaPerHour = result.ApplicableOnlineManaPerHour;
            ConfiguredBaseOfflineEfficiency = result.ConfiguredBaseOfflineEfficiency;
            EffectiveOfflineEfficiency = result.EffectiveOfflineEfficiency;
            EffectiveOfflineManaPerHour = result.EffectiveOfflineManaPerHour;
            CalculatedPreCapacityAward = result.CalculatedPreCapacityAward;
            ActualAwardedMana = result.ActualAwardedMana;
            WalletBefore = result.WalletBefore; WalletAfter = result.WalletAfter;
            Capacity = result.Capacity; CapacityLimited = result.CapacityLimited;
        }
    }

    // Retained interval-start calculation inputs only; neither a wallet nor a save owner.
    public sealed class HistoricalOfflineManaInterval
    {
        private readonly string rate;
        public long StartUtcUnix { get; }
        public long EndUtcUnix { get; }
        public double Wallet { get; }
        public double Capacity { get; }
        public double BaseEfficiency { get; }
        public double EffectiveEfficiency { get; }
        internal HistoricalOfflineManaInterval(OfflinePassiveManaResult accepted)
        {
            StartUtcUnix = accepted.SourceSavedUtcUnix; EndUtcUnix = accepted.ObservedCurrentUtcUnix;
            Wallet = accepted.WalletBefore; Capacity = accepted.Capacity;
            BaseEfficiency = accepted.ConfiguredBaseOfflineEfficiency; EffectiveEfficiency = accepted.EffectiveOfflineEfficiency;
            rate = UnityEngine.JsonUtility.ToJson(accepted.CanonicalRate);
        }
        public PassiveManaRateSummary InspectRate() => UnityEngine.JsonUtility.FromJson<PassiveManaRateSummary>(rate);
    }
}
