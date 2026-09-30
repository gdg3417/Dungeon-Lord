using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public sealed partial class RunSimulationService
    {
        public RunOutcomeRecord SimulateSnapshot(int sequence, ActiveFloorRunSnapshot snapshot) =>
            SimulateSnapshotCore(sequence, snapshot, out _);

        private RunOutcomeRecord SimulateSnapshotCore(int sequence, ActiveFloorRunSnapshot snapshot, out BranchTraversal traversal)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var config = snapshot.Configuration;
            var engine = new RunSimulationService(config, snapshot.LootConfiguration, snapshot.LootTableId);
            var first = snapshot.Floors[0].MaterializePlan();
            traversal = new BranchTraversal { Forks = first.Forks,
                Workload = new BranchRunWorkload(config.PhaseFiveB.BranchDecision) };
            var objective = TransientDepthObjective.Select(config.PhaseSix, RunId(sequence));
            var result = engine.SimulateRoute(snapshot.InitialRuntime, snapshot.TickStarted, sequence, snapshot.PostureId,
                first.RequiredRooms.Select(r => r.Room).ToArray(),
                snapshot.Floors.Count > 1 || first.Forks.Length > 0 ? traversal : null,
                snapshot.Floors.Count > 1 ? snapshot : null, objective);
            if (result.LootSummary?.ResolverSuccess != true)
                throw new InvalidOperationException("branch.run.invalid_state");
            result.DepthObjective = objective;
            result.BranchOutcomes = traversal.Evidence.ToArray();
            return result;
        }
    }
}
