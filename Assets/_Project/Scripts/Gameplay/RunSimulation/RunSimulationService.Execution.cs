using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.Structures;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public sealed partial class RunSimulationService
    {
        public RunExecution StartSnapshot(int sequence, ActiveFloorRunSnapshot snapshot,
            RunLogicalTiming timing = null, long? eventLimit = null)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var config = snapshot.Configuration;
            var engine = new RunSimulationService(config, snapshot.LootConfiguration, snapshot.LootTableId);
            var first = snapshot.Floors[0].MaterializePlan();
            var traversal = new BranchTraversal { Forks = first.Forks,
                Workload = new BranchRunWorkload(config.PhaseFiveB.BranchDecision) };
            var objective = TransientDepthObjective.Select(config.PhaseSix, RunId(sequence));
            return engine.CreateExecution(snapshot.InitialRuntime, snapshot.TickStarted, sequence, snapshot.PostureId,
                first.RequiredRooms.Select(r => r.Room).ToArray(), traversal, snapshot, objective, timing, eventLimit);
        }

        public RunExecution StartRoute(StructureRuntimeState runtime, long tickStarted, int sequence,
            string postureId, MvpOrderedRouteRoom[] route, RunLogicalTiming timing = null, long? eventLimit = null) =>
            CreateExecution(runtime, tickStarted, sequence, postureId, route, null, null, null, timing, eventLimit);

        private RunExecution CreateExecution(StructureRuntimeState runtime, long tickStarted, int sequence,
            string postureId, MvpOrderedRouteRoom[] route, BranchTraversal traversal, ActiveFloorRunSnapshot snapshot,
            TransientDepthObjective objective, RunLogicalTiming timing, long? eventLimit)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (tickStarted < 0 || sequence <= 0) throw new ArgumentException("run.execution.invalid_identity");
            if (double.IsNaN(runtime.Heat) || double.IsInfinity(runtime.Heat) || runtime.Heat < 0 ||
                double.IsNaN(runtime.ManaReserve) || double.IsInfinity(runtime.ManaReserve) || runtime.ManaReserve < 0)
                throw new ArgumentException("run.execution.invalid_inputs");
            if (route != null && route.Any(r => r == null)) throw new ArgumentException("run.execution.invalid_inputs");
            var capturedConfig = JsonUtility.FromJson<RunSimulationConfig>(JsonUtility.ToJson(_config));
            if (_config.MvpCompositionOutcomeTuning == null) capturedConfig.MvpCompositionOutcomeTuning = null;
            if (_config.PhaseSix == null) capturedConfig.PhaseSix = null;
            var engine = new RunSimulationService(capturedConfig,
                _lootConfig == null ? null : JsonUtility.FromJson<LootConfig>(JsonUtility.ToJson(_lootConfig)), _lootTableId);
            var captured = JsonUtility.FromJson<StructureRuntimeState>(JsonUtility.ToJson(runtime));
            var rooms = JsonUtility.FromJson<RouteCapture>(JsonUtility.ToJson(new RouteCapture {
                Rooms = route ?? Array.Empty<MvpOrderedRouteRoom>() })).Rooms;
            // Unity materializes null nested objects/arrays. Preserve the legacy discriminator:
            // absent Spatial/Assignments means nonspatial/compatibility projection, not empty geometry.
            for (int i = 0; i < rooms.Length; i++)
            {
                if (route[i].Spatial == null) rooms[i].Spatial = null;
                if (route[i].Assignments == null) rooms[i].Assignments = null;
            }
            rooms = rooms.OrderBy(r => r.FloorIndex).ThenBy(r => r.RoomIndex).ToArray();
            engine.ValidateAssignments(rooms);
            RunPostureResolver.Resolve(capturedConfig, postureId);
            if (timing != null && snapshot == null && rooms.Any(r => r.Spatial == null))
                throw new ArgumentException("run.execution.timed_geometry_required");
            long bound = 1; // terminal
            var plans = snapshot == null ? new[] { new PhaseFiveBRoutePlan { RequiredRooms = rooms.Select(r =>
                new PhaseFiveBRequiredRoom { Room = r }).ToArray(), Forks = traversal?.Forks ?? Array.Empty<PhaseFiveBFork>() } } :
                snapshot.Floors.Select(f => f.MaterializePlan()).ToArray();
            foreach (var plan in plans)
            {
                bound = checked(bound + 2); // floor decision/transfer
                foreach (var r in plan.RequiredRooms.Select(r => r.Room))
                {
                    var path = r.Spatial == null ? null : IntraroomTraversal.Plan(r);
                    bound = checked(bound + 4 + (path?.Path.LongLength ?? 0) +
                        (path?.Interactions.LongLength ?? r.OrderedAssignments().LongLength));
                }
                foreach (var fork in plan.Forks)
                    bound = checked(bound + 2 + fork.Assignments.LongLength + 2 * (fork.Tiles?.LongLength ?? 0));
            }
            // Geometry-only bounds are conservative cardinalities, not gameplay tuning.
            if (snapshot != null) bound = checked(bound + snapshot.ConnectionWorkBound);
            if (eventLimit.HasValue && eventLimit.Value > bound) throw new ArgumentException("run.execution.invalid_limit");
            var state = new RunExecution(RunPartyGenerator.Create(capturedConfig.PhaseFiveB, RunId(sequence)), timing,
                eventLimit ?? bound) { Traversal = traversal, Snapshot = snapshot };
            state.Initialize(engine.ExecuteAndFinish(state, captured, tickStarted, sequence, postureId, rooms,
                traversal, snapshot, objective));
            return state;
        }
        [Serializable] private sealed class RouteCapture { public MvpOrderedRouteRoom[] Rooms; }

        private IEnumerable<RunStep> ExecuteAndFinish(RunExecution state, StructureRuntimeState runtime,
            long tickStarted, int sequence, string postureId, MvpOrderedRouteRoom[] route,
            BranchTraversal traversal, ActiveFloorRunSnapshot snapshot, TransientDepthObjective objective)
        {
            foreach (var step in ExecuteRoute(state, runtime, tickStarted, sequence, postureId, route, traversal, snapshot, objective))
                yield return step;
            if (snapshot != null)
            {
                if (state.Outcome.LootSummary?.ResolverSuccess != true) throw new InvalidOperationException("branch.run.invalid_state");
                state.Outcome.DepthObjective = objective;
                state.Outcome.BranchOutcomes = traversal.Evidence.ToArray();
            }
            yield return new RunStep(RunStepKind.Terminal, -1, null);
        }
    }
}
