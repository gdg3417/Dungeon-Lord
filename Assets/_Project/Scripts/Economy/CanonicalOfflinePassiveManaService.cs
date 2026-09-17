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
        public double Capacity { get; internal set; }
        public bool CapacityLimited { get; internal set; }
        public bool PersistenceRequired { get; internal set; }
        public bool Persisted { get; internal set; }
        public PassiveManaRateSummary CanonicalRate { get; internal set; }

        public bool CalculationAccepted => Reason == OfflinePassiveManaReason.Applied ||
            Reason == OfflinePassiveManaReason.AlreadyAtCapacity ||
            Reason == OfflinePassiveManaReason.NoManaGenerated;

        internal OfflinePassiveManaResult WithPersistence(
            OfflinePassiveManaReason reason, bool persisted)
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

            if (observedCurrentUtcUnix <= 0)
                return Reject(result, OfflinePassiveManaReason.CurrentTimestampInvalid);
            if (sourceTimestamp <= 0)
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
            result.CanonicalRate = rate;
            if (rate == null || !rate.RuleResolved || !FiniteNonnegative(rate.ManaPerHour))
                return result;

            result.ApplicableOnlineManaPerHour = rate.ManaPerHour;
            result.EffectiveOfflineManaPerHour = rate.ManaPerHour * effectiveOfflineEfficiency;
            result.CalculatedPreCapacityAward = result.EffectiveOfflineManaPerHour *
                result.ObservedElapsedSeconds / SecondsPerHour;
            if (!FiniteNonnegative(result.EffectiveOfflineManaPerHour) ||
                !FiniteNonnegative(result.CalculatedPreCapacityAward))
                return result;

            result.WalletAfter = Math.Min(result.Capacity,
                wallet + result.CalculatedPreCapacityAward);
            result.ActualAwardedMana = result.WalletAfter - wallet;
            result.CapacityLimited = result.CalculatedPreCapacityAward >
                result.ActualAwardedMana;
            result.PersistenceRequired = true;
            result.Reason = result.ActualAwardedMana > 0d
                ? OfflinePassiveManaReason.Applied
                : wallet >= result.Capacity
                    ? OfflinePassiveManaReason.AlreadyAtCapacity
                    : OfflinePassiveManaReason.NoManaGenerated;
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
}
