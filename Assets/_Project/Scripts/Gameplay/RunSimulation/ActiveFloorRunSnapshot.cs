using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    // Private serialized values are an in-memory defensive-copy mechanism, never save owners.
    public sealed class RunnableFloorSnapshot
    {
        private readonly string plan;
        private readonly string spatial;
        private readonly string connections;
        internal long ConnectionWorkBound { get; }
        public string FloorInstanceId { get; }
        public int FloorIndex { get; }
        public string KnowledgeFingerprint { get; }
        internal RunnableFloorSnapshot(SavedSpatialFloor floor, PhaseFiveBRoutePlan value, string fingerprint,
            RunConnectionSnapshot movement)
        {
            FloorInstanceId = floor.FloorInstanceId; FloorIndex = floor.FloorIndex;
            KnowledgeFingerprint = fingerprint;
            plan = JsonUtility.ToJson(value); spatial = JsonUtility.ToJson(floor);
            connections = JsonUtility.ToJson(movement);
            ConnectionWorkBound = movement.Required.Sum(p => (long)p.Path.Length) +
                movement.BranchApproach.Sum(p => 2L * p.Path.Length);
        }
        public PhaseFiveBRoutePlan MaterializePlan() => JsonUtility.FromJson<PhaseFiveBRoutePlan>(plan);
        public SavedSpatialFloor InspectSpatialInputs() => JsonUtility.FromJson<SavedSpatialFloor>(spatial);
        internal void WriteCheckpoint(BinaryWriter w)
        {
            RunCheckpointCodec.Text(w, FloorInstanceId); w.Write(FloorIndex); RunCheckpointCodec.Text(w, KnowledgeFingerprint);
            RunCheckpointCodec.Text(w, plan); RunCheckpointCodec.Text(w, spatial); RunCheckpointCodec.Text(w, connections);
            w.Write(ConnectionWorkBound);
        }
        private RunnableFloorSnapshot(BinaryReader r)
        {
            FloorInstanceId = RunCheckpointCodec.Text(r); FloorIndex = r.ReadInt32(); KnowledgeFingerprint = RunCheckpointCodec.Text(r);
            plan = RunCheckpointCodec.Text(r); spatial = RunCheckpointCodec.Text(r); connections = RunCheckpointCodec.Text(r);
            ConnectionWorkBound = r.ReadInt64();
        }
        internal static RunnableFloorSnapshot ReadCheckpoint(BinaryReader r) => new RunnableFloorSnapshot(r);
        internal void ValidateCombatCapture(long maximumWork)
        {
            var floor = InspectSpatialInputs(); var route = MaterializePlan();
            var paths = JsonUtility.FromJson<RunConnectionSnapshot>(connections);
            if (floor.FloorInstanceId != FloorInstanceId || floor.FloorIndex != FloorIndex ||
                floor.ActivationState != FloorActivationState.Active || route.RequiredRooms == null || route.Forks == null ||
                paths?.Required == null || paths.BranchApproach == null || paths.Required.Length != route.RequiredRooms.Length + 1 ||
                paths.BranchApproach.Length != route.Forks.Length || ConnectionWorkBound < 0)
                throw new InvalidDataException("run.checkpoint.invalid_inputs");
            long work = 0;
            foreach (var path in paths.Required.Concat(paths.BranchApproach)) {
                if (path == null || string.IsNullOrEmpty(path.Id) || path.Path == null || path.Path.Length == 0 || path.Path.Length > maximumWork)
                    throw new InvalidDataException("run.checkpoint.invalid_inputs");
                for (int i = 1; i < path.Path.Length; i++)
                    if (Math.Abs((long)path.Path[i].X - path.Path[i - 1].X) + Math.Abs((long)path.Path[i].Y - path.Path[i - 1].Y) != 1)
                        throw new InvalidDataException("run.checkpoint.invalid_inputs");
                work = checked(work + path.Path.Length);
            }
            long expected = checked(paths.Required.Sum(p => (long)p.Path.Length) + 2 * paths.BranchApproach.Sum(p => (long)p.Path.Length));
            if (work > maximumWork || ConnectionWorkBound != expected ||
                paths.Required.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != paths.Required.Length ||
                !paths.BranchApproach.Select(p => p.Id).OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(
                    route.Forks.Select(f => f.OptionalBranchId).OrderBy(id => id, StringComparer.Ordinal)))
                throw new InvalidDataException("run.checkpoint.invalid_inputs");
            foreach (var required in route.RequiredRooms) {
                var room = required.Room;
                if (room == null || room.FloorIndex != FloorIndex || room.Spatial?.FloorInstanceId != FloorInstanceId ||
                    room.Spatial.RoomInstanceId != room.RoomInstanceId ||
                    !floor.Layout.Rooms.Any(r => r.RoomInstanceId == room.RoomInstanceId && r.RoomDefinitionId == room.Spatial.RoomDefinitionId))
                    throw new InvalidDataException("run.checkpoint.invalid_inputs");
            }
        }
        internal IEnumerable<RunStep> TraverseConnection(int index)
        {
            var connection = JsonUtility.FromJson<RunConnectionSnapshot>(connections).Required[index];
            for (int i = 1; i < connection.Path.Length; i++)
                foreach (var boundary in RunStep.Pure(new RunStep(RunStepKind.Movement, FloorIndex, connection.RoomId,
                    connection.Id, connection.Path[i], 1, space: RunPositionSpace.Floor))) yield return boundary;
        }
        internal IEnumerable<RunStep> TraverseBranchApproach(string branch, bool returning)
        {
            var path = JsonUtility.FromJson<RunConnectionSnapshot>(connections).BranchApproach.Single(p => p.Id == branch);
            var cells = returning ? path.Path.Reverse().ToArray() : path.Path;
            for (int i = 1; i < cells.Length; i++)
                foreach (var boundary in RunStep.Pure(new RunStep(RunStepKind.Movement, FloorIndex, path.RoomId, branch, cells[i], 1, space: RunPositionSpace.RoomLocal))) yield return boundary;
        }
        internal TileCoordinate BranchSocket(string branch) =>
            JsonUtility.FromJson<RunConnectionSnapshot>(connections).BranchApproach.Single(p => p.Id == branch).Path.Last();
    }

    public sealed class ActiveFloorRunSnapshot
    {
        private readonly string configuration;
        private readonly string loot;
        private readonly string knowledge;
        private readonly string floorKnowledge;
        private readonly string initialRuntime;
        internal CombatC0QualificationConfig CombatRules { get; private set; }
        internal CombatC0RoomBounds[] CombatBounds { get; private set; }
        private MonsterContinuityState monsterStarts;
        private string[] monsterOptions;
        internal MonsterContinuityState MonsterStarts => monsterStarts?.Copy();
        internal string[] MonsterStartOptions => monsterOptions == null ? null : (string[])monsterOptions.Clone();
        internal int MonsterStartLevel { get; private set; }
        internal int MonsterReadinessNumerator { get; private set; }
        internal int MonsterReadinessDenominator { get; private set; }
        public long TickStarted { get; }
        public string PostureId { get; }
        public DungeonBuilder.M0.Gameplay.Structures.StructureRuntimeState InitialRuntime =>
            JsonUtility.FromJson<DungeonBuilder.M0.Gameplay.Structures.StructureRuntimeState>(initialRuntime);
        public SharedBranchKnowledgeAuthority BranchKnowledge => JsonUtility.FromJson<SharedBranchKnowledgeAuthority>(knowledge);
        public SharedFloorKnowledgeAuthority FloorKnowledge => JsonUtility.FromJson<SharedFloorKnowledgeAuthority>(floorKnowledge);
        public LootConfig LootConfiguration => loot == null ? null : JsonUtility.FromJson<LootConfig>(loot);
        public string LootTableId { get; }
        public IReadOnlyList<RunnableFloorSnapshot> Floors { get; }
        public RunSimulationConfig Configuration => JsonUtility.FromJson<RunSimulationConfig>(configuration);
        internal long ConnectionWorkBound => Floors.Sum(f => f.ConnectionWorkBound);
        internal void WriteCheckpoint(BinaryWriter w)
        {
            foreach (var text in new[] { configuration, loot, knowledge, floorKnowledge, initialRuntime, PostureId, LootTableId })
                RunCheckpointCodec.Text(w, text);
            w.Write(TickStarted); w.Write(Floors.Count); foreach (var floor in Floors) floor.WriteCheckpoint(w);
        }
        private ActiveFloorRunSnapshot(BinaryReader r)
        {
            configuration = RunCheckpointCodec.Text(r); loot = RunCheckpointCodec.Text(r); knowledge = RunCheckpointCodec.Text(r);
            floorKnowledge = RunCheckpointCodec.Text(r); initialRuntime = RunCheckpointCodec.Text(r);
            PostureId = RunCheckpointCodec.Text(r); LootTableId = RunCheckpointCodec.Text(r); TickStarted = r.ReadInt64();
            int count = r.ReadInt32();
            if (count <= 0 || count > r.BaseStream.Length - r.BaseStream.Position) throw new InvalidDataException("run.checkpoint.encoding");
            var floors = new RunnableFloorSnapshot[count];
            for (int i = 0; i < count; i++) floors[i] = RunnableFloorSnapshot.ReadCheckpoint(r);
            Floors = Array.AsReadOnly(floors);
        }
        internal static ActiveFloorRunSnapshot ReadCheckpoint(BinaryReader r) => new ActiveFloorRunSnapshot(r);
        // Admission preparation owns its template privately. Share only immutable
        // leaves; no writable snapshot array/monster record crosses executions.
        internal ActiveFloorRunSnapshot CopyForExecution() => new ActiveFloorRunSnapshot(this);
        private ActiveFloorRunSnapshot(ActiveFloorRunSnapshot source)
        {
            configuration = source.configuration; loot = source.loot; knowledge = source.knowledge;
            floorKnowledge = source.floorKnowledge; initialRuntime = source.initialRuntime;
            TickStarted = source.TickStarted; PostureId = source.PostureId; LootTableId = source.LootTableId;
            Floors = source.Floors; CombatRules = source.CombatRules;
            CombatBounds = (CombatC0RoomBounds[])source.CombatBounds.Clone();
            monsterStarts = source.monsterStarts?.Copy();
            monsterOptions = source.monsterOptions == null ? null : (string[])source.monsterOptions.Clone();
            MonsterStartLevel = source.MonsterStartLevel;
            MonsterReadinessNumerator = source.MonsterReadinessNumerator;
            MonsterReadinessDenominator = source.MonsterReadinessDenominator;
        }
        // Used only by the separate format-2 reader on newly owned snapshot bytes.
        internal void RestoreCombatInputs(CombatC0QualificationConfig rules, CombatC0RoomBounds[] bounds, MonsterContinuityState starts,
            int level, int numerator, int denominator, string[] options)
        {
            CombatRules = rules; CombatBounds = (CombatC0RoomBounds[])bounds.Clone(); monsterStarts = starts?.Copy();
            MonsterStartLevel = level; MonsterReadinessNumerator = numerator; MonsterReadinessDenominator = denominator;
            monsterOptions = options == null ? null : (string[])options.Clone();
        }
        private ActiveFloorRunSnapshot(RunnableFloorSnapshot[] floors, RunSimulationConfig config, LootConfig lootConfig, string lootTableId, SharedBranchKnowledgeAuthority branchKnowledge, SharedFloorKnowledgeAuthority sharedFloorKnowledge, SaveData executionInputs, string postureId)
        { Floors = Array.AsReadOnly(floors); configuration = JsonUtility.ToJson(config);
          knowledge = JsonUtility.ToJson(branchKnowledge);
          floorKnowledge = JsonUtility.ToJson(sharedFloorKnowledge);
          initialRuntime = JsonUtility.ToJson(executionInputs.structureRuntime); TickStarted = executionInputs.totalTicks; PostureId = postureId;
          loot = lootConfig == null ? null : JsonUtility.ToJson(lootConfig); LootTableId = lootTableId ?? config.LootTableId; }

        public static ActiveFloorRunSnapshot Create(DetachedCompleteSaveValidationResult owned,
            ProductionSpatialContentSnapshot production, RunSimulationConfig config, CanonicalSpatialSerializationLimits limits, LootConfig lootConfig = null, string lootTableId = null, SaveData executionInputs = null, string postureId = RunPostureResolver.BalancedId)
        {
            if (owned?.IsValid != true || !owned.CurrentTargetValidated || production == null ||
                !PhaseSixRunConfigValidation.IsValid(config?.PhaseSix) || !PhaseFiveBConfigValidation.IsValid(config.PhaseFiveB))
                throw new ArgumentException(PhaseSixRunConfigValidation.Invalid);
            executionInputs = executionInputs ?? JsonUtility.FromJson<SaveRoot>(System.Text.Encoding.UTF8.GetString(owned.GetBytes())).primary;
            if (executionInputs?.structureRuntime == null) throw new ArgumentException(PhaseFiveBRouteProjection.InvalidRoute);
            if (lootConfig == null || (!LootRollResolver.Resolve(lootConfig, lootTableId ?? config.LootTableId, 0).success ||
                !lootConfig.tables.Any(t => t.id == (lootTableId ?? config.LootTableId))))
                throw new ArgumentException(PhaseSixRunConfigValidation.Invalid);
            var state = owned.State;
            var facade = new SaveData { validatedCanonicalSpatialState = state,
                canonicalSpatialAuthority = state.Authority, spatialFloors = state.Floors };
            if (!CanonicalActiveFloorResolver.TryResolve(facade, limits.Spatial, out int count) || count == 0 ||
                count > config.PhaseSix.MaximumActiveFloors || count - 1 > config.PhaseSix.MaximumTransitions ||
                !DetachedCanonicalProductionSemanticValidation.Validate(state, production, config, limits.Spatial).IsValid)
                throw new ArgumentException(PhaseFiveBRouteProjection.InvalidRoute);
            string[] independentRules = { config.PhaseSix.ObjectiveRuleSourceId, config.PhaseSix.TransitionRuleSourceId,
                config.PhaseFiveB.BranchDecision.RuleSourceId, config.PhaseFiveB.PartyRuleSourceId,
                config.PhaseFiveB.BehaviorRuleSourceId, config.PhaseFiveB.IntelligenceRuleSourceId };
            if (independentRules.Distinct(StringComparer.Ordinal).Count() != independentRules.Length)
                throw new ArgumentException(PhaseSixRunConfigValidation.Invalid);
            var floors = new List<RunnableFloorSnapshot>();
            foreach (var floor in state.Floors.Where(f => f.ActivationState == FloorActivationState.Active)
                .OrderBy(f => f.FloorIndex).ThenBy(f => f.FloorInstanceId, StringComparer.Ordinal))
            {
                if (floor.FloorIndex != floors.Count) throw new ArgumentException(PhaseFiveBRouteProjection.InvalidRoute);
                var plan = PhaseFiveBRouteProjection.ResolveFloor(owned, floor, production, config);
                foreach (var room in plan.RequiredRooms)
                {
                    room.Room.Spatial = IntraroomSnapshot.Capture(floor, room.NodeId, production.Catalog,
                        owned.RoomContentOccupancy, limits.Spatial);
                    IntraroomTraversal.Plan(room.Room);
                }
                if (floor.FloorIndex > 0 && plan.RequiredRooms.Length == 0)
                    throw new ArgumentException(PhaseFiveBRouteProjection.InvalidRoute);
                foreach (var assignment in plan.RequiredRooms.SelectMany(r => r.Room.OrderedAssignments())
                    .Concat(plan.Forks.SelectMany(f => f.Assignments).Select(a => new RunRoomAssignment {
                        AssignmentId = a.AssignmentId, CategoryId = a.CategoryId, OptionId = a.OptionId, Sequence = a.Sequence })))
                    if (assignment.CategoryId != MvpDungeonPlacementIds.LootNodeCategoryId &&
                        !config.PhaseFiveB.DamageProfiles.Any(p => p.OptionId == assignment.OptionId && p.CategoryId == assignment.CategoryId))
                        throw new ArgumentException(PhaseSixRunConfigValidation.Invalid);
                if (plan.Forks.Length > config.PhaseFiveB.BranchDecision.MaximumDecisionsPerFloor ||
                    plan.Forks.Any(f => f.Assignments.Length > config.PhaseFiveB.BranchDecision.MaximumAssignmentsPerBranch))
                    throw new ArgumentException(BranchRunWorkload.WorkloadExceeded);
                if (!FloorKnowledgeApplicability.TryCompute(state, owned.CorridorContent, limits,
                    floor.FloorInstanceId, out string fingerprint))
                    throw new ArgumentException(PhaseFiveBRouteProjection.InvalidRoute);
                floors.Add(new RunnableFloorSnapshot(floor, plan, fingerprint,
                    RunConnectionSnapshot.Capture(floor, plan, production.Catalog, limits.Spatial.MaximumMaterializedTiles)));
            }
            if (floors.Sum(f => f.MaterializePlan().Forks.Length) > config.PhaseFiveB.BranchDecision.MaximumDecisionsPerRun)
                throw new ArgumentException(BranchRunWorkload.WorkloadExceeded);
            var snapshot = new ActiveFloorRunSnapshot(floors.ToArray(), config, lootConfig, lootTableId, owned.BranchKnowledge, owned.FloorKnowledge, executionInputs, postureId);
            snapshot.CombatRules = config.CombatC0?.Capture();
            snapshot.monsterStarts = owned.MonsterContinuity?.Copy();
            if (snapshot.CombatRules != null)
            {
                var recovery = MonsterRecoveryRules.Capture(config);
                snapshot.MonsterStartLevel = recovery.InitialLevel;
                snapshot.MonsterReadinessNumerator = recovery.ReadinessNumerator;
                snapshot.MonsterReadinessDenominator = recovery.ReadinessDenominator;
                var options = MonsterContinuity.Owned(owned.State).ToDictionary(m => m.Id, m => m.Option, StringComparer.Ordinal);
                snapshot.monsterOptions = snapshot.monsterStarts?.Records.Select(m => options[m.AssignmentId]).ToArray();
                var catalog = production.Catalog;
                snapshot.CombatBounds = floors.SelectMany(f => f.MaterializePlan().RequiredRooms.Select(r => {
                    var spatial = r.Room.Spatial;
                    var shape = catalog.Rooms.Single(d => d.RoomDefinitionId == spatial.RoomDefinitionId).GrossFootprint;
                    return new CombatC0RoomBounds(spatial.FloorInstanceId, spatial.RoomInstanceId, shape.Width, shape.Height);
                })).ToArray();
            }
            return snapshot;
        }
    }
}
