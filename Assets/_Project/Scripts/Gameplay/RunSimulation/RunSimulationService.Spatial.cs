using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public sealed partial class RunSimulationService
    {
        private MvpPlacementEffectsSummary ResolveSpatialRoom(MvpOrderedRouteRoom room, RunParty party,
            List<RunEncounterEvent> encounters, List<RunSpatialEvent> evidence, RunPostureConfig posture,
            double mana, out double maximumPressure)
        {
            var plan = IntraroomTraversal.Plan(room);
            maximumPressure = 0d;
            var reached = new List<MvpDungeonPlacementEntry>();
            if (room.IncludeRoomPlacement) reached.Add(new MvpDungeonPlacementEntry(
                MvpDungeonPlacementIds.RoomCategoryId, room.RoomOptionId, 0));
            RecordSpatial(room, evidence, RunSpatialEventKind.RoomEntered, 0, plan.Path[0]);
            int lastStep = 0, interaction = 0;
            for (int step = 0; step < plan.Path.Length && !party.IsWiped; step++)
            {
                lastStep = step;
                while (interaction < plan.Interactions.Length && plan.Interactions[interaction].Step == step && !party.IsWiped)
                {
                    var scheduled = plan.Interactions[interaction++];
                    var a = scheduled.Assignment;
                    var e = RecordSpatial(room, evidence, scheduled.Kind, step, plan.Path[step], a);
                    e.ConfiguredStart = a.RoomLocalPosition; e.Path = scheduled.Movement;
                    if (!scheduled.Reached) continue;
                    reached.Add(new MvpDungeonPlacementEntry(a.CategoryId, a.OptionId, reached.Count));
                    if (a.CategoryId == MvpDungeonPlacementIds.LootNodeCategoryId) continue;
                    var effects = MvpPlacementEffectsResolver.ResolvePlacements(reached.ToArray(), _config);
                    double pressure = ResolveCasualtyPressure(BuildCompositionOutcomeSummary(effects, mana), posture);
                    maximumPressure = Math.Max(maximumPressure, pressure);
                    e.EncounterOrdinal = encounters.Count;
                    encounters.Add(RunEncounterResolver.Resolve(party, _config.PhaseFiveB, a,
                        room.FloorIndex, room.RoomIndex, pressure));
                }
            }
            var traversal = RecordSpatial(room, evidence, RunSpatialEventKind.AdventurerTraversal,
                lastStep, plan.Path[lastStep]);
            traversal.Path = plan.Path.Take(lastStep + 1).ToArray();
            if (!party.IsWiped) RecordSpatial(room, evidence, RunSpatialEventKind.RoomEgressReached,
                lastStep, plan.Path[lastStep]);
            return MvpPlacementEffectsResolver.ResolvePlacements(reached.ToArray(), _config);
        }

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
