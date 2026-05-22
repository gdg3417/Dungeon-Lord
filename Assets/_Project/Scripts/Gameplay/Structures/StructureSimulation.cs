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

        public const double HeatCrisisEnterThreshold = 100d;
        public const double HeatCrisisRecoverThreshold = 65d;

        private readonly IHeatSystem _heatSystem;

        public StructureSimulationPass(IHeatSystem heatSystem)
        {
            _heatSystem = heatSystem ?? throw new ArgumentNullException(nameof(heatSystem));
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
                if (string.IsNullOrEmpty(structureId))
                {
                    continue;
                }

                if (string.Equals(structureId, ManaGeneratorBasicId, StringComparison.Ordinal))
                {
                    manaDelta += 10d;
                    heatDelta += 5d;
                }
                else if (string.Equals(structureId, HeatScrubberBasicId, StringComparison.Ordinal))
                {
                    heatDelta -= 8d;
                }
                else if (string.Equals(structureId, RiskLabBasicId, StringComparison.Ordinal))
                {
                    manaDelta += 18d;
                    heatDelta += 15d;
                }
            }

            HeatResult heatResult = _heatSystem.ApplyEvent(new HeatEventInput(tickIndex, runtime.Heat, heatDelta));

            runtime.ManaReserve = Math.Max(0d, runtime.ManaReserve + manaDelta);
            runtime.Heat = heatResult.NewHeat;
            runtime.LastProcessedTick = tickIndex;

            if (!runtime.IsHeatCrisisActive && runtime.Heat >= HeatCrisisEnterThreshold)
            {
                runtime.IsHeatCrisisActive = true;
            }
            else if (runtime.IsHeatCrisisActive && runtime.Heat <= HeatCrisisRecoverThreshold)
            {
                runtime.IsHeatCrisisActive = false;
            }

            return new StructureTickResult(runtime.ManaReserve, runtime.Heat, runtime.IsHeatCrisisActive);
        }
    }
}
