using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public sealed partial class RunSimulationService
    {
        private sealed class SpatialRoomResolution
        { internal MvpPlacementEffectsSummary Effects; internal double MaximumPressure; }

        private IEnumerable<RunStep> ResolveSpatialRoom(MvpOrderedRouteRoom room, RunParty party,
            List<RunEncounterEvent> encounters, List<RunSpatialEvent> evidence, RunPostureConfig posture,
            double mana, SpatialRoomResolution result, CombatC0ExecutionState combat = null)
        {
            if (combat != null)
            {
                foreach (var step in ResolveCombatRoom(room, party, encounters, evidence, posture, mana, result, combat)) yield return step;
                yield break;
            }
            var plan = IntraroomTraversal.Plan(room);
            result.MaximumPressure = 0d;
            var reached = new List<MvpDungeonPlacementEntry>();
            if (room.IncludeRoomPlacement) reached.Add(new MvpDungeonPlacementEntry(
                MvpDungeonPlacementIds.RoomCategoryId, room.RoomOptionId, 0));
            yield return RunStep.Before(RunStepKind.RoomEntry);
            RecordSpatial(room, evidence, RunSpatialEventKind.RoomEntered, 0, plan.Path[0]);
            yield return new RunStep(RunStepKind.RoomEntry, room.FloorIndex, room.RoomInstanceId, position: plan.Path[0], space: RunPositionSpace.RoomLocal);
            int lastStep = 0, interaction = 0;
            for (int step = 0; step < plan.Path.Length && !party.IsWiped; step++)
            {
                lastStep = step;
                if (step > 0) foreach (var boundary in RunStep.Pure(new RunStep(RunStepKind.Movement, room.FloorIndex,
                    room.RoomInstanceId, position: plan.Path[step], distance: 1, space: RunPositionSpace.RoomLocal))) yield return boundary;
                while (interaction < plan.Interactions.Length && plan.Interactions[interaction].Step == step && !party.IsWiped)
                {
                    var scheduled = plan.Interactions[interaction++];
                    var a = scheduled.Assignment;
                    yield return RunStep.Before(!scheduled.Reached ? RunStepKind.UnreachableInteraction : InteractionStep(room, a, null).Kind,
                        scheduled.Reached && a.CategoryId != MvpDungeonPlacementIds.LootNodeCategoryId ? Math.Max(0, (scheduled.Movement?.Length ?? 1) - 1) : 0);
                    var e = RecordSpatial(room, evidence, scheduled.Kind, step, plan.Path[step], a);
                    e.ConfiguredStart = a.RoomLocalPosition; e.Path = scheduled.Movement;
                    if (!scheduled.Reached)
                    {
                        yield return new RunStep(RunStepKind.UnreachableInteraction, room.FloorIndex,
                            room.RoomInstanceId, a.AssignmentId, plan.Path[step], space: RunPositionSpace.RoomLocal);
                        continue;
                    }
                    reached.Add(new MvpDungeonPlacementEntry(a.CategoryId, a.OptionId, reached.Count));
                    if (a.CategoryId == MvpDungeonPlacementIds.LootNodeCategoryId)
                    {
                        yield return InteractionStep(room, a, null, plan.Path[step]);
                        continue;
                    }
                    var effects = MvpPlacementEffectsResolver.ResolvePlacements(reached.ToArray(), _config);
                    double pressure = ResolveCasualtyPressure(BuildCompositionOutcomeSummary(effects, mana), posture);
                    result.MaximumPressure = Math.Max(result.MaximumPressure, pressure);
                    e.EncounterOrdinal = encounters.Count;
                    var encounter = RunEncounterResolver.Resolve(party, _config.PhaseFiveB, a,
                        room.FloorIndex, room.RoomIndex, pressure);
                    encounters.Add(encounter);
                    yield return InteractionStep(room, a, encounter, plan.Path[step],
                        Math.Max(0, (scheduled.Movement?.Length ?? 1) - 1));
                }
            }
            var traversal = RecordSpatial(room, evidence, RunSpatialEventKind.AdventurerTraversal,
                lastStep, plan.Path[lastStep]);
            traversal.Path = plan.Path.Take(lastStep + 1).ToArray();
            if (!party.IsWiped)
            {
                yield return RunStep.Before(RunStepKind.RoomEgress);
                RecordSpatial(room, evidence, RunSpatialEventKind.RoomEgressReached, lastStep, plan.Path[lastStep]);
                yield return new RunStep(RunStepKind.RoomEgress, room.FloorIndex, room.RoomInstanceId, position: plan.Path[lastStep], space: RunPositionSpace.RoomLocal);
            }
            result.Effects = MvpPlacementEffectsResolver.ResolvePlacements(reached.ToArray(), _config);
        }

        private static RunStep InteractionStep(MvpOrderedRouteRoom room, RunRoomAssignment a,
            RunEncounterEvent encounter, DungeonSpatial.TileCoordinate position = default, int distance = 0) =>
            new RunStep(a.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId ? RunStepKind.MonsterEngagement :
                a.CategoryId == MvpDungeonPlacementIds.TrapCategoryId ? RunStepKind.TrapTrigger : RunStepKind.LootInteraction,
                room.FloorIndex, room.RoomInstanceId, a.AssignmentId, position, distance, encounter,
                space: room.Spatial == null ? RunPositionSpace.None : RunPositionSpace.RoomLocal);

        private static RunSpatialEvent RecordSpatial(MvpOrderedRouteRoom room, List<RunSpatialEvent> evidence,
            RunSpatialEventKind kind, int step, DungeonSpatial.TileCoordinate position, RunRoomAssignment a = null)
        {
            var e = new RunSpatialEvent { FloorInstanceId = room.Spatial.FloorInstanceId, FloorIndex = room.FloorIndex,
                RoomInstanceId = room.RoomInstanceId, Ordinal = evidence.Count, Kind = kind,
                PathStep = step, Position = position, AssignmentId = a?.AssignmentId,
                CategoryId = a?.CategoryId, OptionId = a?.OptionId };
            evidence.Add(e); return e;
        }
    }
}
