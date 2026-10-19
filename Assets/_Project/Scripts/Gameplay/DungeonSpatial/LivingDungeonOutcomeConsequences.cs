using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // Shared pure consequence composition; eligibility remains in each caller.
    internal static class LivingDungeonOutcomeConsequences
    {
        private const string Invalid = "living.settlement.invalid";
        internal static void Apply(SaveData candidate, DetachedCompleteSaveValidationResult owned,
            RunExecution execution, RunOutcomeRecord outcome, RunSimulationConfig currentConfig,
            ProductionSpatialContentSnapshot production, CanonicalSpatialSerializationLimits limits)
        {
            var snapshot = execution.Snapshot ?? throw new InvalidOperationException(Invalid);
            var config = snapshot.Configuration;
            if (outcome.RunId != execution.Inspect().RunId || outcome.Party == null ||
                execution.Combat != null && !CombatC0Diagnostics.IsCombatC2(outcome)) throw new InvalidOperationException(Invalid);
            var heat = RunHeatStateApplyResolver.Resolve(config, candidate.structureRuntime.Heat, outcome.RunHeatDeltaSummary);
            if (!heat.RuleResolved) throw new InvalidOperationException(Invalid);
            outcome.RunHeatApplicationSummary = heat;
            candidate.structureRuntime.Heat = heat.HeatAfter;
            int seed = outcome.LootExtractionSummary?.DeterministicSeed ?? throw new InvalidOperationException(Invalid);
            var cooling = LootHeatCoolingResolver.Resolve(config, outcome.LootExtractionSummary, heat.HeatAfter, seed);
            if (!cooling.RuleResolved) throw new InvalidOperationException(Invalid);
            // Same separate foreground cooling/clamp stage as manual settlement.
            var cooled = new HeatSystem().ApplyEvent(new HeatEventInput(candidate.totalTicks, heat.HeatAfter, cooling.AppliedHeatDelta));
            cooling.HeatAfterCooling = cooled.NewHeat;
            cooling.AppliedHeatDelta = cooled.NewHeat - cooling.HeatBeforeCooling;
            outcome.LootHeatCoolingSummary = cooling;
            candidate.structureRuntime.Heat = cooled.NewHeat;

            // Floor applicability includes room/corridor content and geometry. A branch
            // topology hash alone cannot authenticate monster/trap/loot observations.
            var eligible = new HashSet<string>(StringComparer.Ordinal);
            if (JsonUtility.ToJson(config) == JsonUtility.ToJson(currentConfig))
                foreach (var floor in snapshot.Floors)
                    if (FloorKnowledgeApplicability.TryCompute(owned.State, owned.CorridorContent, limits,
                        floor.FloorInstanceId, out string fingerprint) && fingerprint == floor.KnowledgeFingerprint)
                        eligible.Add(floor.FloorInstanceId);
            var observations = (outcome.BranchOutcomes ?? Array.Empty<BranchOutcomeEvidence>()).Where(o =>
                o.Fork != null && eligible.Contains(o.Fork.FloorInstanceId) && Unchanged(
                    owned.BranchKnowledge.Records.SingleOrDefault(r => r.FloorInstanceId == o.Fork.FloorInstanceId && r.OptionalBranchId == o.Fork.OptionalBranchId),
                    snapshot.BranchKnowledge.Records.SingleOrDefault(r => r.FloorInstanceId == o.Fork.FloorInstanceId && r.OptionalBranchId == o.Fork.OptionalBranchId))).ToArray();
            candidate.sharedBranchKnowledge = BranchKnowledgeLearning.Propose(owned.BranchKnowledge, observations,
                outcome.Party.ActiveCount > 0, outcome.RunId, config.PhaseFiveB.BranchDecision,
                new BranchRunWorkload(config.PhaseFiveB.BranchDecision));
            var proposedFloors = FloorKnowledgeLearning.Propose(snapshot, outcome);
            var floors = PhaseSixFloorKnowledge.Canonicalize(owned.FloorKnowledge).Records.ToList();
            foreach (var learned in proposedFloors.Records)
            {
                var prior = floors.SingleOrDefault(r => r.FloorInstanceId == learned.FloorInstanceId);
                var start = snapshot.FloorKnowledge.Records.SingleOrDefault(r => r.FloorInstanceId == learned.FloorInstanceId);
                if (!eligible.Contains(learned.FloorInstanceId) || !learned.HasLastConfirmedRun ||
                    learned.LastConfirmedRunId != outcome.RunId || !Unchanged(prior, start)) continue;
                if (prior != null) floors.Remove(prior); floors.Add(learned);
            }
            candidate.sharedFloorKnowledge = PhaseSixFloorKnowledge.Canonicalize(new SharedFloorKnowledgeAuthority { Records = floors.ToArray() });
            candidate.runHistory.AppendOutcome(outcome, currentConfig.MaxRunHistoryEntries);
            // Only the existing first-session objective has an applicable verified rule.
            // Evaluate that one configured objective; no broad completion/research helper.
            var objective = Unchanged(currentConfig.MvpFirstSessionObjective, config.MvpFirstSessionObjective)
                ? MvpFirstSessionObjectivePresenter.Resolve(candidate, currentConfig, production) : null;
            if (objective?.RuleResolved == true && objective.CurrentRequirementsComplete &&
                objective.ObjectiveId == currentConfig.MvpFirstSessionObjective?.ObjectiveId && !objective.CompletionRecorded)
                CompletedObjectiveStateResolver.MarkCompleted(candidate, objective.ObjectiveId,
                    CompletedObjectiveStateResolver.FirstSessionObjectiveCompletionRuleSourceId);
        }
        private static bool Unchanged(object current, object start) => current == null ? start == null :
            start != null && JsonUtility.ToJson(current) == JsonUtility.ToJson(start);
    }

}
