using System;
using System.Collections.Generic;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonLayout;

namespace DungeonBuilder.M0.Gameplay.Structures
{
    [Serializable]
    public sealed class StructureRuntimeState
    {
        public double ManaReserve;
        public double Heat;
        public bool IsHeatCrisisActive;
        public long LastProcessedTick;
        public int HeatCrisisEnterStreak;
        public int HeatCrisisRecoveryStreak;
        public bool RiskLabPaused;
        public bool PlacementLocked;
    }

    [Serializable]
    public sealed class StructureTuningEntry
    {
        public string StructureId;
        public double ManaDeltaPerTick;
        public double HeatDeltaPerTick;
    }

    [Serializable]
    public sealed class StructureSimulationConfig
    {
        public StructureTuningEntry[] Structures = Array.Empty<StructureTuningEntry>();
        public double HeatCrisisEnterThreshold;
        public double HeatCrisisRecoveryThreshold;
        public int CrisisEnterConsecutiveTicks;
        public int CrisisRecoveryConsecutiveTicks;
        public double CrisisManaDrainPerTick;
        public bool RiskLabPausedDuringCrisis;
        public bool PlacementBlockedDuringCrisis;
    }

    public readonly struct StructureTickResult
    {
        public double ManaReserve { get; }
        public double Heat { get; }
        public bool IsHeatCrisisActive { get; }

        public StructureTickResult(double manaReserve, double heat, bool isHeatCrisisActive)
        {
            ManaReserve = manaReserve;
            Heat = heat;
            IsHeatCrisisActive = isHeatCrisisActive;
        }
    }

    public sealed class StructureSimulationPass
    {
        public const string ManaGeneratorBasicId = "structure.mana_generator.basic";
        public const string HeatScrubberBasicId = "structure.heat_scrubber.basic";
        public const string RiskLabBasicId = "structure.risk_lab.basic";

        private static readonly HashSet<string> AllowedStructureIds = new HashSet<string>(StringComparer.Ordinal)
        {
            ManaGeneratorBasicId,
            HeatScrubberBasicId,
            RiskLabBasicId
        };

        private readonly IHeatSystem _heatSystem;
        private readonly Dictionary<string, StructureTuningEntry> _tuningByStructureId;
        private readonly double _heatCrisisEnterThreshold;
        private readonly double _heatCrisisRecoveryThreshold;
        private readonly int _crisisEnterConsecutiveTicks;
        private readonly int _crisisRecoveryConsecutiveTicks;
        private readonly double _crisisManaDrainPerTick;
        private readonly bool _riskLabPausedDuringCrisis;
        private readonly bool _placementBlockedDuringCrisis;

        public StructureSimulationPass(IHeatSystem heatSystem, StructureSimulationConfig config)
        {
            _heatSystem = heatSystem ?? throw new ArgumentNullException(nameof(heatSystem));

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _heatCrisisEnterThreshold = config.HeatCrisisEnterThreshold;
            _heatCrisisRecoveryThreshold = config.HeatCrisisRecoveryThreshold;
            _crisisEnterConsecutiveTicks = config.CrisisEnterConsecutiveTicks;
            _crisisRecoveryConsecutiveTicks = config.CrisisRecoveryConsecutiveTicks;
            _crisisManaDrainPerTick = config.CrisisManaDrainPerTick;
            _riskLabPausedDuringCrisis = config.RiskLabPausedDuringCrisis;
            _placementBlockedDuringCrisis = config.PlacementBlockedDuringCrisis;
            _tuningByStructureId = BuildTuningMap(config.Structures);
        }

        public StructureTickResult SimulateTick(DungeonLayoutState layout, StructureRuntimeState runtime, long tickIndex)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));

            double manaDelta = 0d;
            double heatDelta = 0d;

            List<DungeonSlot> orderedSlots = new List<DungeonSlot>(layout.OrderedSlots());
            for (int i = 0; i < orderedSlots.Count; i++)
            {
                string structureId = orderedSlots[i].StructureId;
                if (string.IsNullOrEmpty(structureId) || !_tuningByStructureId.TryGetValue(structureId, out StructureTuningEntry tuning))
                {
                    continue;
                }

                if (runtime.IsHeatCrisisActive && _riskLabPausedDuringCrisis && string.Equals(structureId, RiskLabBasicId, StringComparison.Ordinal))
                {
                    continue;
                }

                manaDelta += tuning.ManaDeltaPerTick;
                heatDelta += tuning.HeatDeltaPerTick;
            }

            HeatResult heatResult = _heatSystem.ApplyEvent(new HeatEventInput(tickIndex, runtime.Heat, heatDelta));

            double crisisManaDrain = runtime.IsHeatCrisisActive ? _crisisManaDrainPerTick : 0d;
            runtime.ManaReserve = Math.Max(0d, runtime.ManaReserve + manaDelta - crisisManaDrain);
            runtime.Heat = heatResult.NewHeat;
            runtime.LastProcessedTick = tickIndex;

            if (!runtime.IsHeatCrisisActive)
            {
                runtime.HeatCrisisRecoveryStreak = 0;
                if (runtime.Heat >= _heatCrisisEnterThreshold)
                {
                    runtime.HeatCrisisEnterStreak++;
                }
                else
                {
                    runtime.HeatCrisisEnterStreak = 0;
                }

                if (runtime.HeatCrisisEnterStreak >= _crisisEnterConsecutiveTicks)
                {
                    runtime.IsHeatCrisisActive = true;
                    runtime.HeatCrisisEnterStreak = 0;
                }
            }
            else
            {
                runtime.HeatCrisisEnterStreak = 0;
                if (runtime.Heat <= _heatCrisisRecoveryThreshold)
                {
                    runtime.HeatCrisisRecoveryStreak++;
                }
                else
                {
                    runtime.HeatCrisisRecoveryStreak = 0;
                }

                if (runtime.HeatCrisisRecoveryStreak >= _crisisRecoveryConsecutiveTicks)
                {
                    runtime.IsHeatCrisisActive = false;
                    runtime.HeatCrisisRecoveryStreak = 0;
                }
            }

            runtime.RiskLabPaused = runtime.IsHeatCrisisActive && _riskLabPausedDuringCrisis;
            runtime.PlacementLocked = runtime.IsHeatCrisisActive && _placementBlockedDuringCrisis;

            return new StructureTickResult(runtime.ManaReserve, runtime.Heat, runtime.IsHeatCrisisActive);
        }

        private static Dictionary<string, StructureTuningEntry> BuildTuningMap(StructureTuningEntry[] entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            var map = new Dictionary<string, StructureTuningEntry>(StringComparer.Ordinal);
            for (int i = 0; i < entries.Length; i++)
            {
                StructureTuningEntry entry = entries[i] ?? throw new ArgumentException("Structure tuning entry cannot be null.", nameof(entries));
                if (string.IsNullOrWhiteSpace(entry.StructureId))
                {
                    throw new ArgumentException("Structure tuning entry requires a structure ID.", nameof(entries));
                }

                if (!AllowedStructureIds.Contains(entry.StructureId))
                {
                    throw new ArgumentException($"Structure ID is not allowed in PR-B scope: {entry.StructureId}", nameof(entries));
                }

                map[entry.StructureId] = entry;
            }

            return map;
        }
    }
}
