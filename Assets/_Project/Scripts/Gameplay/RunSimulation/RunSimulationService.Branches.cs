using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public sealed partial class RunSimulationService
    {
        internal void CalculatePhaseFiveBRun(SaveData candidate, DetachedCompleteSaveValidationResult owned,
            ProductionSpatialContentSnapshot production, string postureId, CanonicalSpatialSerializationLimits limits)
        {
            if (!PhaseFiveBConfigValidation.IsValid(_config.PhaseFiveB)) throw new ArgumentException(BranchDecisionResolver.InvalidConfiguration);
            var snapshot = ActiveFloorRunSnapshot.Create(owned, production, _config, limits, _lootConfig, _lootTableId, candidate, postureId);
            int sequence;
            if (owned.LivingDungeon?.IsDormant == false)
            {
                if (!LivingDungeonScheduling.AllocatorValid(candidate.runHistory, owned.LivingDungeon, true))
                    throw new InvalidOperationException(LivingDungeonScheduling.Invalid);
                sequence = candidate.runHistory.NextRunSequence;
            }
            else sequence = Math.Max(1, candidate.runHistory.NextRunSequence);
            var execution = StartManualSnapshot(sequence, snapshot);
            var outcome = execution.DrainToCompletion(retainPresentationSteps: true);
            BranchTraversal traversal = execution.Traversal;
            if (outcome.RunHeatApplicationSummary?.RuleResolved == true)
                candidate.structureRuntime.Heat = outcome.RunHeatApplicationSummary.HeatAfter;
            var workload = traversal.Workload;
            var capturedConfig = snapshot.Configuration;
            int seed = outcome.LootExtractionSummary?.DeterministicSeed ?? sequence;
            var cooling = LootHeatCoolingResolver.Resolve(capturedConfig, outcome.LootExtractionSummary, candidate.structureRuntime.Heat, seed);
            outcome.LootHeatCoolingSummary = cooling;
            if (cooling.RuleResolved && cooling.AppliedHeatDelta != 0d)
            {
                var heat = new DungeonBuilder.M0.Economy.HeatSystem().ApplyEvent(new DungeonBuilder.M0.Economy.HeatEventInput(
                    candidate.totalTicks, candidate.structureRuntime.Heat, cooling.AppliedHeatDelta));
                candidate.structureRuntime.Heat = heat.NewHeat;
                cooling.HeatAfterCooling = heat.NewHeat; cooling.AppliedHeatDelta = heat.NewHeat - cooling.HeatBeforeCooling;
            }
            candidate.sharedBranchKnowledge = BranchKnowledgeLearning.Propose(snapshot.BranchKnowledge, traversal.Evidence,
                outcome.Party.ActiveCount > 0, outcome.RunId, capturedConfig.PhaseFiveB.BranchDecision, workload);
            candidate.sharedFloorKnowledge = FloorKnowledgeLearning.Propose(snapshot, outcome);
            candidate.runHistory.AppendOutcome(outcome, capturedConfig.MaxRunHistoryEntries);
            candidate.runHistory.NextRunSequence = checked(sequence + 1);
            MvpFirstSessionObjectiveCompletionApplier.ApplyIfComplete(candidate, capturedConfig, production);
        }

        internal sealed class BranchTraversal
        {
            internal PhaseFiveBFork[] Forks;
            internal BranchRunWorkload Workload;
            internal readonly List<BranchOutcomeEvidence> Evidence = new List<BranchOutcomeEvidence>();
            internal readonly List<string> Items = new List<string>();
            internal int Value, Reserve, Tradeable, Rolls, LootError;
            internal bool HasEncounter, LootSuccess = true;
            internal double CasualtyHeat, CasualtyPenalty, MaximumPressure;
            internal readonly MvpPlacementEffectsSummary Reached = EmptyPlacementEffects();
            internal readonly MvpPlacementEffectsSummary Reward = EmptyPlacementEffects();
        }

        private IEnumerable<RunStep> VisitFork(RunExecution execution, BranchTraversal state, MvpOrderedRouteRoom room, RunParty party, bool stopped,
            List<RunEncounterEvent> events, RunPostureConfig posture, double mana)
        {
            var fork = state.Forks.SingleOrDefault(f => f.FloorIndex == room.FloorIndex && f.RoomIndex == room.RoomIndex);
            if (fork == null) yield break;
            var e = new BranchOutcomeEvidence { Fork = fork, Reason = "branch.outcome.stopped" };

            if (stopped || party.IsWiped)
            { e.PrecedenceReason = "branch.decision.retreat_precedence"; state.Evidence.Add(e); yield break; }
            if (execution.Snapshot != null)
                foreach (var step in execution.Snapshot.Floors.Single(f => f.FloorIndex == room.FloorIndex)
                    .TraverseBranchApproach(fork.OptionalBranchId, false)) yield return step;
            yield return RunStep.Before(RunStepKind.BranchDecision);
            state.Evidence.Add(e);
            state.Workload.Decision(fork.FloorInstanceId);
            e.Decision = BranchDecisionResolver.Resolve(_config.PhaseFiveB.BranchDecision, new BranchDecisionInput {
                Party = party, FloorInstanceId = fork.FloorInstanceId, OptionalBranchId = fork.OptionalBranchId,
                Applicable = fork.Applicable, Knowledge = fork.Knowledge, RemainingRequiredDanger = fork.RemainingRequiredDanger });
            if (e.Decision.Reason == BranchDecisionResolver.InvalidConfiguration) throw new ArgumentException(e.Decision.Reason);
            yield return new RunStep(RunStepKind.BranchDecision, room.FloorIndex, room.RoomInstanceId, fork.OptionalBranchId,
                position: execution.Snapshot == null ? default : execution.Snapshot.Floors.Single(f => f.FloorIndex == room.FloorIndex)
                    .BranchSocket(fork.OptionalBranchId), space: execution.Snapshot == null ? RunPositionSpace.None : RunPositionSpace.RoomLocal,
                continueChosen: e.Decision.Enter);
            if (!e.Decision.Enter)
            {
                e.Reason = "branch.outcome.skipped";
                if (execution.Snapshot != null)
                    foreach (var step in execution.Snapshot.Floors.Single(f => f.FloorIndex == room.FloorIndex)
                        .TraverseBranchApproach(fork.OptionalBranchId, true)) yield return step;
                yield break;
            }
            e.Traversed = true; state.Workload.BeginBranch();
            var reached = new List<string>(); var encounters = new List<RunEncounterEvent>();
            var observed = EmptyPlacementEffects();
            var allEffects = MvpPlacementEffectsResolver.ResolvePlacements(fork.Assignments.Select((a, i) =>
                new MvpDungeonPlacementEntry(a.CategoryId, a.OptionId, i)).ToArray(), _config);
            double pressure = ResolveCasualtyPressure(BuildCompositionOutcomeSummary(allEffects, mana), posture);
            state.MaximumPressure = Math.Max(state.MaximumPressure, pressure);
            int pathCursor = -1;
            foreach (var a in fork.Assignments)
            {
                if (party.IsWiped) break;
                int target = fork.Tiles == null ? -1 : Array.IndexOf(fork.Tiles, a.Tile);
                if (fork.Tiles != null && target < 0) throw new ArgumentException(PhaseFiveBRouteProjection.InvalidRoute);
                while (pathCursor < target)
                {
                    foreach (var boundary in RunStep.Pure(new RunStep(RunStepKind.Movement, room.FloorIndex, room.RoomInstanceId,
                        fork.OptionalBranchId, fork.Tiles[++pathCursor], 1, space: RunPositionSpace.Floor))) yield return boundary;
                }
                yield return RunStep.Before(a.CategoryId == MvpDungeonPlacementIds.TrapCategoryId ? RunStepKind.TrapTrigger : RunStepKind.LootInteraction);
                state.Workload.Assignment();
                if (a.CategoryId != MvpDungeonPlacementIds.TrapCategoryId && a.CategoryId != MvpDungeonPlacementIds.LootNodeCategoryId)
                    throw new ArgumentException(PhaseFiveBRouteProjection.InvalidRoute);
                state.HasEncounter = true; reached.Add(a.AssignmentId);
                var effects = MvpPlacementEffectsResolver.ResolvePlacements(new[] { new MvpDungeonPlacementEntry(a.CategoryId, a.OptionId, 0) }, _config);
                AddPlacementEffects(observed, effects); AddPlacementEffects(state.Reached, effects);
                if (a.CategoryId == MvpDungeonPlacementIds.TrapCategoryId)
                {
                    int before = party.ActiveCount;
                    var encounter = RunEncounterResolver.Resolve(party, _config.PhaseFiveB, new RunRoomAssignment {
                        AssignmentId = a.AssignmentId, CategoryId = a.CategoryId, OptionId = a.OptionId, Sequence = a.Sequence },
                        fork.FloorIndex, fork.RoomIndex, pressure);
                    events.Add(encounter); encounters.Add(encounter);
                    var casualty = party.DeriveSurvival(true); casualty.DeathCount = before - party.ActiveCount;
                    ApplyCasualtyEvidence(casualty, pressure);
                    state.CasualtyHeat += casualty.CasualtyHeatDelta; state.CasualtyPenalty += casualty.CasualtyLootExtractionPenalty;
                    yield return new RunStep(RunStepKind.TrapTrigger, room.FloorIndex, room.RoomInstanceId,
                        a.AssignmentId, a.Tile, encounter: encounter, space: RunPositionSpace.Floor);
                }
                else
                {
                    // Independent branch segment identity, repository-style ordinal integer fold.
                    // It does not use the branch-choice roll or change required-room loot seeds.
                    int seed = BranchLootSeed(party.RunId, fork.FloorInstanceId, fork.OptionalBranchId, a.AssignmentId);
                    var loot = ApplyCompositionToLootSummary(ApplyPostureToLootSummary(BuildLootSummary(seed), posture), BuildCompositionOutcomeSummary(effects, mana));
                    if (loot?.ResolverSuccess != true)
                        throw new InvalidOperationException("branch.run.invalid_state");
                    else
                    {
                        state.Items.AddRange(loot.GeneratedItemIds ?? Array.Empty<string>()); state.Value += loot.TotalGeneratedWorldValue;
                        state.Reserve += loot.TotalGeneratedReserveCost; state.Tradeable += loot.TotalGeneratedTradeableWorldValue;
                        state.Rolls += loot.RollCount; e.GeneratedLootValue += loot.TotalGeneratedWorldValue;
                    }
                    AddPlacementEffects(state.Reward, effects);
                    execution.CarriedLootValue += loot.TotalGeneratedWorldValue;
                    yield return new RunStep(RunStepKind.LootInteraction, room.FloorIndex, room.RoomInstanceId, a.AssignmentId, a.Tile,
                        space: RunPositionSpace.Floor);
                }
            }
            if (!party.IsWiped && fork.Tiles != null)
            {
                while (pathCursor < fork.Tiles.Length - 1)
                    foreach (var boundary in RunStep.Pure(new RunStep(RunStepKind.Movement, room.FloorIndex, room.RoomInstanceId,
                        fork.OptionalBranchId, fork.Tiles[++pathCursor], 1, space: RunPositionSpace.Floor))) yield return boundary;
                for (int index = fork.Tiles.Length - 2; index >= 0; index--)
                    foreach (var boundary in RunStep.Pure(new RunStep(RunStepKind.Movement, room.FloorIndex, room.RoomInstanceId,
                        fork.OptionalBranchId, fork.Tiles[index], 1, space: RunPositionSpace.Floor))) yield return boundary;
                // Final corridor-to-room socket edge, then reverse the legal room approach.
                foreach (var boundary in RunStep.Pure(new RunStep(RunStepKind.Movement, room.FloorIndex, room.RoomInstanceId,
                    fork.OptionalBranchId, execution.Snapshot == null ? default : execution.Snapshot.Floors
                        .Single(f => f.FloorIndex == room.FloorIndex).BranchSocket(fork.OptionalBranchId), 1, space: RunPositionSpace.RoomLocal))) yield return boundary;
                if (execution.Snapshot != null)
                    foreach (var step in execution.Snapshot.Floors.Single(f => f.FloorIndex == room.FloorIndex)
                        .TraverseBranchApproach(fork.OptionalBranchId, true)) yield return step;
            }
            if (!party.IsWiped) yield return RunStep.Before(RunStepKind.BranchReturn);
            e.ReachedAssignments = reached.ToArray(); e.Encounters = encounters.ToArray();
            e.Reason = party.IsWiped ? "branch.outcome.wiped" : "branch.outcome.completed";
            e.Returned = !party.IsWiped;
            e.ActualIncentive = BranchDecisionResolver.Clamp(observed.LootBonus / _config.PhaseFiveB.BranchDecision.IncentiveReferenceLootBonus);
            e.ActualDanger = BranchDecisionResolver.Clamp(observed.Danger / _config.PhaseFiveB.BranchDecision.DangerReferenceValue);
            if (!party.IsWiped) yield return new RunStep(RunStepKind.BranchReturn, room.FloorIndex, room.RoomInstanceId, fork.OptionalBranchId);
            // Automatic return is a control-flow transition only; no content, roll or workload repeats.
        }

        public static int BranchLootSeed(string run, string floor, string branch, string assignment)
        {
            unchecked
            {
                int hash = 17;
                foreach (string field in new[] { "run.loot.branch_segment.v1", run, floor, branch, assignment })
                    hash = hash * 31 + BranchLootStableStringHash(field);
                return hash;
            }
        }

        private static int BranchLootStableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            unchecked
            {
                int hash = 23;
                foreach (char character in value) hash = hash * 31 + character;
                return hash;
            }
        }
    }
}
