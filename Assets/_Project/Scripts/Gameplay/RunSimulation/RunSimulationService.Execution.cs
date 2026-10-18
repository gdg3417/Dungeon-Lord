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
        public RunCheckpointRestoreResult RestoreCheckpoint(byte[] bytes, string expectedCheckpointIdentity, string expectedStartIdentity,
            long expectedOrdinal, RunCheckpointLimits limits) =>
            CombatCheckpointCodec.RestoreSupported(bytes, expectedCheckpointIdentity, expectedStartIdentity, expectedOrdinal, limits);
        public RunExecution StartSnapshot(int sequence, ActiveFloorRunSnapshot snapshot,
            RunLogicalTiming timing = null, long? eventLimit = null)
            => StartCapturedSnapshot(sequence, snapshot, timing, eventLimit, false);

        public RunExecution StartManualSnapshot(int sequence, ActiveFloorRunSnapshot snapshot)
            => StartCapturedSnapshot(sequence, snapshot, null, null, true);
        internal RunExecution StartHistoricalCombatC0Snapshot(int sequence, ActiveFloorRunSnapshot snapshot)
            => StartCapturedSnapshot(sequence, snapshot, null, null, true, false);

        // Explicit technical entry point. Existing manual/B2 callers retain their executables.
        public RunExecution StartTimedCombatSnapshot(int sequence, ActiveFloorRunSnapshot snapshot,
            RunLogicalTiming timing, bool persistentMonsters = true, long? eventLimit = null)
        {
            if (timing == null) throw new ArgumentNullException(nameof(timing));
            CombatStartCapture.Validate(snapshot, persistentMonsters);
            var execution = StartCapturedSnapshot(sequence, snapshot, timing, eventLimit, true, persistentMonsters);
            execution.StartInputs = CombatStartCapture.Capture(sequence, snapshot, timing, execution.MaximumEvents, persistentMonsters);
            execution.PrefixDigest = RunCheckpointCodec.Hash(execution.StartInputs);
            execution.CommittedCombatWork = execution.Combat.Kernel.WorkCount;
            return execution;
        }

        private RunExecution StartCapturedSnapshot(int sequence, ActiveFloorRunSnapshot snapshot,
            RunLogicalTiming timing, long? eventLimit, bool manual, bool persistent = true)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var config = snapshot.Configuration;
            var engine = new RunSimulationService(config, snapshot.LootConfiguration, snapshot.LootTableId);
            var first = snapshot.Floors[0].MaterializePlan();
            var traversal = new BranchTraversal { Forks = first.Forks,
                Workload = new BranchRunWorkload(config.PhaseFiveB.BranchDecision) };
            var objective = TransientDepthObjective.Select(config.PhaseSix, RunId(sequence));
            return engine.CreateExecution(snapshot.InitialRuntime, snapshot.TickStarted, sequence, snapshot.PostureId,
                first.RequiredRooms.Select(r => r.Room).ToArray(), traversal, snapshot, objective, timing, eventLimit,
                manual ? snapshot.CombatRules : null, manual ? snapshot.CombatBounds : null, manual && persistent ? snapshot.MonsterStarts : null);
        }

        public RunExecution StartRoute(StructureRuntimeState runtime, long tickStarted, int sequence,
            string postureId, MvpOrderedRouteRoom[] route, RunLogicalTiming timing = null, long? eventLimit = null) =>
            CreateExecution(runtime, tickStarted, sequence, postureId, route, null, null, null, timing, eventLimit);

        private RunExecution CreateExecution(StructureRuntimeState runtime, long tickStarted, int sequence,
            string postureId, MvpOrderedRouteRoom[] route, BranchTraversal traversal, ActiveFloorRunSnapshot snapshot,
            TransientDepthObjective objective, RunLogicalTiming timing, long? eventLimit,
            CombatC0QualificationConfig combatRules = null, CombatC0RoomBounds[] combatBounds = null,
            DungeonSpatial.MonsterContinuityState monsterStarts = null)
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
            long combatPlanningWork = 0;
            void ChargePlanning(long units)
            {
                combatPlanningWork = checked(combatPlanningWork + units);
                if (combatPlanningWork > combatRules.MaximumWork) throw new InvalidOperationException(CombatC0Kernel.Workload);
            }
            var plans = snapshot == null ? new[] { new PhaseFiveBRoutePlan { RequiredRooms = rooms.Select(r =>
                new PhaseFiveBRequiredRoom { Room = r }).ToArray(), Forks = traversal?.Forks ?? Array.Empty<PhaseFiveBFork>() } } :
                snapshot.Floors.Select(f => f.MaterializePlan()).ToArray();
            foreach (var plan in plans)
            {
                bound = checked(bound + 2); // floor decision/transfer
                foreach (var r in plan.RequiredRooms.Select(r => r.Room))
                {
                    var path = r.Spatial == null ? null : IntraroomTraversal.Plan(r, combatRules == null,
                        combatRules == null ? (Action<long>)null : ChargePlanning);
                    bound = checked(bound + 4 + (path?.Path.LongLength ?? 0) +
                        (path?.Interactions.LongLength ?? r.OrderedAssignments().LongLength) +
                        (combatRules == null ? 0 : r.OrderedAssignments().LongCount(a => a.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId)));
                }
                foreach (var fork in plan.Forks)
                    bound = checked(bound + 2 + fork.Assignments.LongLength + 2 * (fork.Tiles?.LongLength ?? 0));
            }
            // Geometry-only bounds are conservative cardinalities, not gameplay tuning.
            if (snapshot != null) bound = checked(bound + snapshot.ConnectionWorkBound);
            if (eventLimit.HasValue && eventLimit.Value > bound) throw new ArgumentException("run.execution.invalid_limit");
            var state = new RunExecution(RunPartyGenerator.Create(capturedConfig.PhaseFiveB, RunId(sequence)), timing,
                eventLimit ?? bound) { Traversal = traversal, Snapshot = snapshot };
            if (combatRules != null)
            {
                state.Combat = new CombatC0ExecutionState(state.Party, combatRules,
                    plans.SelectMany(p => p.RequiredRooms.Select(r => r.Room)).ToArray(), combatBounds, monsterStarts);
                state.Combat.Kernel.ChargeGeometry(combatPlanningWork);
            }
            else
            {
                state.StartInputs = RunStartCapture.Capture(capturedConfig, engine._lootConfig, engine._lootTableId,
                    captured, tickStarted, sequence, postureId, rooms, snapshot, timing, eventLimit ?? bound);
                state.PrefixDigest = RunCheckpointCodec.Hash(state.StartInputs);
            }
            state.Initialize(engine.ExecuteAndFinish(state, captured, tickStarted, sequence, postureId, rooms,
                traversal, snapshot, objective));
            return state;
        }
        [Serializable] private sealed class RouteCapture { public MvpOrderedRouteRoom[] Rooms; }
        internal RunExecution StartCombatC0Route(StructureRuntimeState runtime, long tick, int sequence,
            string posture, MvpOrderedRouteRoom[] route, CombatC0RoomBounds[] bounds) =>
            CreateExecution(runtime, tick, sequence, posture, route, null, null, null, null, null,
                _config.CombatC0?.Capture() ?? throw new ArgumentException(CombatC0Kernel.InvalidState), bounds);

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
