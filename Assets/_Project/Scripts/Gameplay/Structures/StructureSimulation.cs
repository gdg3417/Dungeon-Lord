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

        public StructureSimulationPass(IHeatSystem heatSystem, StructureSimulationConfig config)
        {
            _heatSystem = heatSystem ?? throw new ArgumentNullException(nameof(heatSystem));

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _heatCrisisEnterThreshold = config.HeatCrisisEnterThreshold;
            _heatCrisisRecoveryThreshold = config.HeatCrisisRecoveryThreshold;
            _tuningByStructureId = BuildTuningMap(config.Structures);
        }

        public StructureTickResult SimulateTick(DungeonLayoutState layout, StructureRuntimeState runtime, long tickIndex)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));

            double manaDelta = 0d;
            double heatDelta = 0d;

            IReadOnlyList<DungeonSlot> orderedSlots = new PlacementService().GetPlacementOrder(layout);
            for (int i = 0; i < orderedSlots.Count; i++)
            {
                string structureId = orderedSlots[i].StructureId;
                if (string.IsNullOrEmpty(structureId) || !_tuningByStructureId.TryGetValue(structureId, out StructureTuningEntry tuning))
                {
                    continue;
                }

                manaDelta += tuning.ManaDeltaPerTick;
                heatDelta += tuning.HeatDeltaPerTick;
            }

            HeatResult heatResult = _heatSystem.ApplyEvent(new HeatEventInput(tickIndex, runtime.Heat, heatDelta));

            runtime.ManaReserve = Math.Max(0d, runtime.ManaReserve + manaDelta);
            runtime.Heat = heatResult.NewHeat;
            runtime.LastProcessedTick = tickIndex;

            if (!runtime.IsHeatCrisisActive && runtime.Heat >= _heatCrisisEnterThreshold)
            {
                runtime.IsHeatCrisisActive = true;
            }
            else if (runtime.IsHeatCrisisActive && runtime.Heat <= _heatCrisisRecoveryThreshold)
            {
                runtime.IsHeatCrisisActive = false;
            }

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
