using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public sealed partial class RunSimulationService
    {
        public RunOutcomeRecord SimulateSnapshot(int sequence, ActiveFloorRunSnapshot snapshot) =>
            StartSnapshot(sequence, snapshot).DrainToCompletion();
    }
}
