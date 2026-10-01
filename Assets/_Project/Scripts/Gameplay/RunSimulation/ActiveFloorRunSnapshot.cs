using System;
using System.Collections.Generic;
using System.Linq;
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
        public string FloorInstanceId { get; }
        public int FloorIndex { get; }
        public string KnowledgeFingerprint { get; }
        internal RunnableFloorSnapshot(SavedSpatialFloor floor, PhaseFiveBRoutePlan value, string fingerprint)
        {
            FloorInstanceId = floor.FloorInstanceId; FloorIndex = floor.FloorIndex;
            KnowledgeFingerprint = fingerprint;
            plan = JsonUtility.ToJson(value); spatial = JsonUtility.ToJson(floor);
        }
        public PhaseFiveBRoutePlan MaterializePlan() => JsonUtility.FromJson<PhaseFiveBRoutePlan>(plan);
        public SavedSpatialFloor InspectSpatialInputs() => JsonUtility.FromJson<SavedSpatialFloor>(spatial);
    }

    public sealed class ActiveFloorRunSnapshot
    {
        private readonly string configuration;
        private readonly string loot;
        private readonly string knowledge;
        private readonly string floorKnowledge;
        private readonly string initialRuntime;
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
                floors.Add(new RunnableFloorSnapshot(floor, plan, fingerprint));
            }
            if (floors.Sum(f => f.MaterializePlan().Forks.Length) > config.PhaseFiveB.BranchDecision.MaximumDecisionsPerRun)
                throw new ArgumentException(BranchRunWorkload.WorkloadExceeded);
            return new ActiveFloorRunSnapshot(floors.ToArray(), config, lootConfig, lootTableId, owned.BranchKnowledge, owned.FloorKnowledge, executionInputs, postureId);
        }
    }
}
