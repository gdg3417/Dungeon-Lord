using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public sealed partial class RunSimulationService
    {
        private IEnumerable<RunStep> ResolveCombatRoom(MvpOrderedRouteRoom room, RunParty party,
            List<RunEncounterEvent> traps, List<RunSpatialEvent> evidence, RunPostureConfig posture,
            double mana, SpatialRoomResolution result, CombatC0ExecutionState combat)
        {
            var kernel = combat.Kernel;
            var plan = IntraroomTraversal.Plan(room, false, kernel.ChargeGeometry);
            var geometry = combat.Geometry(room);
            var monsters = room.OrderedAssignments().Where(a => a.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId && kernel.MonsterEligible(a.AssignmentId)).ToArray();
            var positions = monsters.ToDictionary(a => a.AssignmentId, a => a.RoomLocalPosition, StringComparer.Ordinal);
            if (positions.Values.Distinct().Count() != positions.Count) throw new ArgumentException(CombatC0Kernel.InvalidState);
            var resolved = new HashSet<string>(StringComparer.Ordinal);
            var reached = new List<MvpDungeonPlacementEntry>();
            if (room.IncludeRoomPlacement) reached.Add(new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, room.RoomOptionId, 0));
            yield return RunStep.Before(RunStepKind.RoomEntry);
            RecordSpatial(room, evidence, RunSpatialEventKind.RoomEntered, 0, plan.Path[0]);
            yield return new RunStep(RunStepKind.RoomEntry, room.FloorIndex, room.RoomInstanceId, position: plan.Path[0], space: RunPositionSpace.RoomLocal);
            int last = 0, interaction = 0;
            CombatC0Group lastGroup = null;
            for (int step = 0; step < plan.Path.Length && !party.IsWiped; step++)
            {
                last = step;
                if (step > 0) foreach (var boundary in RunStep.Pure(new RunStep(RunStepKind.Movement,
                    room.FloorIndex, room.RoomInstanceId, position: plan.Path[step], distance: 1, space: RunPositionSpace.RoomLocal))) yield return boundary;
                // A completed group frees its defeated monsters' engagement positions.
                // Recollect before movement/other interactions, never recreate their HP.
                while (!party.IsWiped)
                {
                    kernel.ChargeGeometry(1);
                    lastGroup = geometry.Collect(plan.Path[step], monsters, positions, resolved);
                    if (lastGroup.Participating.Length == 0) break;
                    yield return RunStep.Before(RunStepKind.MonsterEngagement);
                    int ordinal = kernel.CompletedCount;
                    var fight = kernel.Resolve(room.Spatial.FloorInstanceId, room.RoomInstanceId, ordinal,
                        lastGroup.Assignments, lastGroup.Links);
                    positions = lastGroup.Positions;
                    foreach (var id in lastGroup.Participating)
                    {
                        if (!resolved.Add(id)) throw new InvalidOperationException(CombatC0Kernel.InvalidState);
                        var a = monsters.Single(m => m.AssignmentId == id);
                        reached.Add(new MvpDungeonPlacementEntry(a.CategoryId, a.OptionId, reached.Count));
                        var e = RecordSpatial(room, evidence, RunSpatialEventKind.MonsterEngaged, step, plan.Path[step], a);
                        e.ConfiguredStart = a.RoomLocalPosition; e.EncounterOrdinal = ordinal;
                        e.Path = lastGroup.Movement.TryGetValue(id, out var path) ? path : new[] { positions[id] };
                    }
                    // Existing casualty severity/economy consumes reached placements once.
                    result.MaximumPressure = Math.Max(result.MaximumPressure, ResolveCasualtyPressure(
                        BuildCompositionOutcomeSummary(MvpPlacementEffectsResolver.ResolvePlacements(reached.ToArray(), _config), mana), posture));
                    yield return new RunStep(RunStepKind.MonsterEngagement, room.FloorIndex, room.RoomInstanceId,
                        lastGroup.Participating[0], plan.Path[step], space: RunPositionSpace.RoomLocal, combatEvents: fight.Events);
                }
                while (interaction < plan.Interactions.Length && plan.Interactions[interaction].Step == step && !party.IsWiped)
                {
                    var scheduled = plan.Interactions[interaction++]; var a = scheduled.Assignment;
                    yield return RunStep.Before(scheduled.Reached ? InteractionStep(room, a, null).Kind : RunStepKind.UnreachableInteraction);
                    var e = RecordSpatial(room, evidence, scheduled.Kind, step, plan.Path[step], a);
                    e.ConfiguredStart = a.RoomLocalPosition; e.Path = scheduled.Movement;
                    if (!scheduled.Reached)
                    {
                        yield return new RunStep(RunStepKind.UnreachableInteraction, room.FloorIndex, room.RoomInstanceId,
                            a.AssignmentId, plan.Path[step], space: RunPositionSpace.RoomLocal); continue;
                    }
                    reached.Add(new MvpDungeonPlacementEntry(a.CategoryId, a.OptionId, reached.Count));
                    if (a.CategoryId == MvpDungeonPlacementIds.LootNodeCategoryId)
                    { yield return InteractionStep(room, a, null, plan.Path[step]); continue; }
                    double pressure = ResolveCasualtyPressure(BuildCompositionOutcomeSummary(
                        MvpPlacementEffectsResolver.ResolvePlacements(reached.ToArray(), _config), mana), posture);
                    result.MaximumPressure = Math.Max(result.MaximumPressure, pressure);
                    e.EncounterOrdinal = traps.Count;
                    var trap = RunEncounterResolver.Resolve(party, _config.PhaseFiveB, a, room.FloorIndex, room.RoomIndex, pressure);
                    traps.Add(trap); yield return InteractionStep(room, a, trap, plan.Path[step]);
                }
            }
            if (!party.IsWiped)
            {
                foreach (var a in monsters.Where(a => !resolved.Contains(a.AssignmentId)))
                {
                    yield return RunStep.Before(RunStepKind.UnreachableInteraction);
                    var link = lastGroup.Links.First(l => l.ActorId == CombatC0Kernel.MonsterId(party, a.AssignmentId));
                    var bypass = kernel.RecordBypass(room.Spatial.FloorInstanceId, room.RoomInstanceId, a.AssignmentId, link);
                    resolved.Add(a.AssignmentId);
                    RecordSpatial(room, evidence, RunSpatialEventKind.MonsterUnreachable, last, plan.Path[last], a);
                    yield return new RunStep(RunStepKind.UnreachableInteraction, room.FloorIndex, room.RoomInstanceId,
                        a.AssignmentId, plan.Path[last], space: RunPositionSpace.RoomLocal, combatEvents: new[] { bypass });
                }
            }
            var traversal = RecordSpatial(room, evidence, RunSpatialEventKind.AdventurerTraversal, last, plan.Path[last]);
            traversal.Path = plan.Path.Take(last + 1).ToArray();
            if (!party.IsWiped)
            {
                yield return RunStep.Before(RunStepKind.RoomEgress);
                RecordSpatial(room, evidence, RunSpatialEventKind.RoomEgressReached, last, plan.Path[last]);
                yield return new RunStep(RunStepKind.RoomEgress, room.FloorIndex, room.RoomInstanceId, position: plan.Path[last], space: RunPositionSpace.RoomLocal);
            }
            result.Effects = MvpPlacementEffectsResolver.ResolvePlacements(reached.ToArray(), _config);
        }
    }
}
