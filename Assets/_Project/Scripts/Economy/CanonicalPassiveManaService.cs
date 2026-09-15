using System;
using System.Collections.Generic;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Economy
{
    public enum PassiveManaResolutionError
    {
        None = 0,
        ConfigurationUnavailable = 1,
        CanonicalSpatialStateInvalid = 2,
        HeatStateInvalid = 3,
        WalletInvalid = 4,
        CalculationInvalid = 5
    }

    public sealed class PassiveManaRateSummary
    {
        public bool RuleResolved;
        public PassiveManaResolutionError Error;
        public string RuleSourceId = string.Empty;
        public int MvpBaselineCoreLevel;
        public int ActiveFloorCount;
        public string HeatTierId = string.Empty;
        public double CoreContributionManaPerHour;
        public double ActiveFloorContributionManaPerHour;
        public double BaseManaPerHour;
        public double HeatEfficiencyMultiplier;
        public double AfterHeatManaPerHour;
        public double AfterResearchManaPerHour;
        public double AfterEventOrSeasonManaPerHour;
        public double AfterClampAndSoftCapManaPerHour;
        public double ManaPerHour;
        public bool SoftCapEnabled;
    }

    public readonly struct PassiveManaTickResult
    {
        internal PassiveManaTickResult(PassiveManaRateSummary rate, long tickIndex,
            double previousMana, double potentialMana, double awardedMana, double newMana,
            bool clampedToCapacity, bool storageFull)
        {
            Rate = rate;
            TickIndex = tickIndex;
            PreviousMana = previousMana;
            PotentialMana = potentialMana;
            AwardedMana = awardedMana;
            NewMana = newMana;
            ClampedToCapacity = clampedToCapacity;
            StorageFull = storageFull;
        }

        public PassiveManaRateSummary Rate { get; }
        public long TickIndex { get; }
        public double PreviousMana { get; }
        public double PotentialMana { get; }
        public double AwardedMana { get; }
        public double NewMana { get; }
        public bool ClampedToCapacity { get; }
        public bool StorageFull { get; }
        public bool Applied => Rate != null && Rate.RuleResolved;
    }

    public static class CanonicalActiveFloorResolver
    {
        public static bool TryResolve(SaveData save, CanonicalSpatialSaveWorkloadLimits limits,
            out int activeFloorCount)
        {
            activeFloorCount = 0;
            DetachedCanonicalSpatialSaveState state = save?.validatedCanonicalSpatialState;
            if (state == null || !ReferenceEquals(save.canonicalSpatialAuthority, state.Authority) ||
                !ReferenceEquals(save.spatialFloors, state.Floors))
                return false;
            CanonicalSpatialSaveValidationResult validation =
                CanonicalSpatialSaveContracts.Validate(state, limits, requireCanonicalOrdering: true);
            if (!validation.IsValid || state.Floors == null)
                return false;
            activeFloorCount = state.Floors.Length;
            return true;
        }
    }

    /// <summary>
    /// Resolves the canonical online passive rate in mana/hour, then applies one fractional
    /// active-tick award to the existing schema-9 ManaReserve wallet.
    /// </summary>
    public sealed class CanonicalPassiveManaService
    {
        private const double MinutesPerHour = 60d;
        private const double SecondsPerHour = 3600d;
        private const string HeatModifierId = "mana.passive_online.heat";
        private const string SoftCapModifierId = "mana.passive_online.soft_cap";

        private readonly PassiveOnlineManaConfigurationSnapshot configuration;
        private readonly StructuralEconomySnapshot structuralEconomy;
        private readonly IFormulaEngine formulaEngine;
        private readonly CanonicalSpatialSaveWorkloadLimits spatialLimits;
        private readonly int tickSeconds;

        public CanonicalPassiveManaService(PassiveOnlineManaConfigurationSnapshot configuration,
            StructuralEconomySnapshot structuralEconomy, IFormulaEngine formulaEngine,
            CanonicalSpatialSaveWorkloadLimits spatialLimits, int tickSeconds)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.structuralEconomy = structuralEconomy ?? throw new ArgumentNullException(nameof(structuralEconomy));
            this.formulaEngine = formulaEngine ?? throw new ArgumentNullException(nameof(formulaEngine));
            if (!spatialLimits.IsValid) throw new ArgumentException("Canonical spatial limits are invalid.", nameof(spatialLimits));
            if (tickSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(tickSeconds));
            this.spatialLimits = spatialLimits;
            this.tickSeconds = tickSeconds;
        }

        public double ManaCapacity => structuralEconomy.ManaCapacity;
        public int TickSeconds => tickSeconds;

        public PassiveManaRateSummary ResolveRate(SaveData save, RunSimulationConfig heatConfiguration)
        {
            var result = new PassiveManaRateSummary
            {
                Error = PassiveManaResolutionError.ConfigurationUnavailable,
                RuleSourceId = configuration.RuleSourceId,
                MvpBaselineCoreLevel = configuration.MvpBaselineCoreLevel,
                SoftCapEnabled = configuration.SoftCapEnabled
            };

            if (!CanonicalActiveFloorResolver.TryResolve(save, spatialLimits, out int activeFloors))
            {
                result.Error = PassiveManaResolutionError.CanonicalSpatialStateInvalid;
                return result;
            }
            result.ActiveFloorCount = activeFloors;

            CurrentHeatTierSummary heat = CurrentHeatTierResolver.Resolve(
                heatConfiguration, save?.structureRuntime?.Heat ?? double.NaN);
            if (!heat.RuleResolved ||
                !configuration.TryGetHeatEfficiency(heat.TierId, out double heatMultiplier))
            {
                result.Error = PassiveManaResolutionError.HeatStateInvalid;
                return result;
            }

            result.HeatTierId = heat.TierId;
            result.HeatEfficiencyMultiplier = heatMultiplier;
            result.CoreContributionManaPerHour = configuration.MvpBaselineCoreLevel *
                configuration.ManaPerCoreLevelPerMinute * MinutesPerHour;
            result.ActiveFloorContributionManaPerHour = activeFloors *
                configuration.ManaPerActiveFloorPerMinute * MinutesPerHour;
            result.BaseManaPerHour = result.CoreContributionManaPerHour +
                result.ActiveFloorContributionManaPerHour;
            if (!FiniteNonnegative(result.CoreContributionManaPerHour) ||
                !FiniteNonnegative(result.ActiveFloorContributionManaPerHour) ||
                !FiniteNonnegative(result.BaseManaPerHour))
            {
                result.Error = PassiveManaResolutionError.CalculationInvalid;
                return result;
            }

            var modifiers = new List<FormulaModifier>
            {
                new FormulaModifier(HeatModifierId, ModifierBucket.Heat,
                    ModifierType.MultiplicativePercent, heatMultiplier - 1d)
            };
            if (configuration.SoftCapEnabled)
            {
                if (!configuration.SoftCapStartManaPerHour.HasValue ||
                    !configuration.SoftCapSlopeManaPerHour.HasValue)
                    return result;
                modifiers.Add(new FormulaModifier(SoftCapModifierId,
                    ModifierBucket.ClampAndSoftCap, ModifierType.SoftCap,
                    configuration.SoftCapStartManaPerHour.Value,
                    configuration.SoftCapSlopeManaPerHour.Value));
            }

            FormulaResult formula = formulaEngine.Evaluate(new FormulaInput(result.BaseManaPerHour, modifiers));
            result.AfterHeatManaPerHour = formula.AfterHeat;
            result.AfterResearchManaPerHour = formula.AfterResearch;
            result.AfterEventOrSeasonManaPerHour = formula.AfterEventOrSeason;
            result.AfterClampAndSoftCapManaPerHour = formula.AfterClampAndSoftCap;
            result.ManaPerHour = formula.Value;
            if (!FiniteNonnegative(result.ManaPerHour))
            {
                result.Error = PassiveManaResolutionError.CalculationInvalid;
                return result;
            }

            result.RuleResolved = true;
            result.Error = PassiveManaResolutionError.None;
            return result;
        }

        public PassiveManaTickResult ApplyTick(SaveData save, RunSimulationConfig heatConfiguration,
            long tickIndex)
        {
            PassiveManaRateSummary rate = ResolveRate(save, heatConfiguration);
            double previousMana = save?.structureRuntime?.ManaReserve ?? double.NaN;
            if (!rate.RuleResolved || !FiniteNonnegative(previousMana) ||
                previousMana > structuralEconomy.ManaCapacity)
            {
                if (rate.RuleResolved)
                {
                    rate.RuleResolved = false;
                    rate.Error = PassiveManaResolutionError.WalletInvalid;
                }
                return new PassiveManaTickResult(rate, tickIndex, previousMana, 0d, 0d,
                    previousMana, false, false);
            }

            double potentialMana = (rate.ManaPerHour / SecondsPerHour) * tickSeconds;
            double rawMana = previousMana + potentialMana;
            if (!FiniteNonnegative(potentialMana) || double.IsNaN(rawMana))
            {
                rate.RuleResolved = false;
                rate.Error = PassiveManaResolutionError.CalculationInvalid;
                return new PassiveManaTickResult(rate, tickIndex, previousMana, 0d, 0d,
                    previousMana, false, false);
            }

            double newMana = Math.Min(structuralEconomy.ManaCapacity, rawMana);
            double awardedMana = newMana - previousMana;
            save.structureRuntime.ManaReserve = newMana;
            return new PassiveManaTickResult(rate, tickIndex, previousMana, potentialMana,
                awardedMana, newMana, rawMana > structuralEconomy.ManaCapacity,
                newMana >= structuralEconomy.ManaCapacity);
        }

        private static bool FiniteNonnegative(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
    }
}
