using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    // Owned by one detached execution. Durable combat replays this state; it never serializes the kernel.
    internal sealed class CombatC0ExecutionState
    {
        internal readonly CombatC0Kernel Kernel;
        internal readonly List<CombatC0Event> Events = new List<CombatC0Event>();
        private readonly Dictionary<(string, string), CombatC0RoomBounds> bounds;
        internal CombatC0ExecutionState(RunParty party, CombatC0QualificationConfig rules,
            MvpOrderedRouteRoom[] rooms, CombatC0RoomBounds[] geometry, MonsterContinuityState continuity = null)
        {
            if (geometry == null || rooms.Any(r => r.Spatial == null)) throw new ArgumentException(CombatC0Kernel.InvalidState);
            bounds = geometry.ToDictionary(b => (b.FloorId, b.RoomId));
            if (bounds.Count != rooms.Length || rooms.Any(r => !bounds.ContainsKey((r.Spatial.FloorInstanceId, r.RoomInstanceId))))
                throw new ArgumentException(CombatC0Kernel.InvalidState);
            Kernel = new CombatC0Kernel(party, rules, rooms.SelectMany(r => r.OrderedAssignments()
                .Where(a => a.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId)
                .Select(a => new CombatC0MonsterPlacement(a.AssignmentId, a.OptionId, r.Spatial.FloorInstanceId, r.RoomInstanceId, a.Sequence))).ToArray(), continuity);
        }
        internal CombatC0Geometry Geometry(MvpOrderedRouteRoom room) => new CombatC0Geometry(room.Spatial,
            bounds[(room.Spatial.FloorInstanceId, room.RoomInstanceId)], Kernel);
        internal void Record(IReadOnlyList<CombatC0Event> events, long boundary)
        { foreach (var e in events) Events.Add(e.AtBoundary(boundary)); }
        internal void Finish(RunOutcomeRecord outcome)
        {
            outcome.CombatEvents = Events.ToArray(); outcome.CombatMonsters = Kernel.InspectMonsters();
            outcome.CombatExecutable = Kernel.RulesId; outcome.CombatWork = Kernel.WorkCount;
        }
    }

    // Inspection returns immutable records/copies; it cannot advance or recalculate combat.
    public static class CombatC0Diagnostics
    {
        public static bool IsCombatC0(RunOutcomeRecord outcome) => outcome?.CombatExecutable == CombatC0Config.Executable || outcome?.CombatExecutable == MonsterRecoveryRules.Executable;
        public static bool IsCombatC2(RunOutcomeRecord outcome) => outcome?.CombatExecutable == MonsterRecoveryRules.Executable;
        public static IReadOnlyList<CombatC0Event> Events(RunOutcomeRecord outcome) =>
            Array.AsReadOnly((CombatC0Event[])(outcome?.CombatEvents ?? Array.Empty<CombatC0Event>()).Clone());
        public static IReadOnlyList<CombatC0MonsterState> Monsters(RunOutcomeRecord outcome) =>
            Array.AsReadOnly((CombatC0MonsterState[])(outcome?.CombatMonsters ?? Array.Empty<CombatC0MonsterState>()).Clone());
        public static long ChargedWork(RunOutcomeRecord outcome) => outcome?.CombatWork ?? 0;
    }
}
