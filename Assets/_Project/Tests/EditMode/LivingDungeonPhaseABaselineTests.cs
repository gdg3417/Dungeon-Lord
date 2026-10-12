#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class LivingDungeonPhaseABaselineTests
    {
        internal static MvpOrderedRouteRoom Room(int index, bool spatial, params RunRoomAssignment[] assignments)
        {
            var r = new MvpOrderedRouteRoom { FloorIndex = 0, RoomIndex = index,
                RoomInstanceId = "phase.a.room." + index, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                IncludeRoomPlacement = true, HasActiveContent = assignments.Length > 0, Assignments = assignments };
            if (spatial) r.Spatial = new IntraroomSnapshot { FloorInstanceId = "phase.a.floor", RoomInstanceId = r.RoomInstanceId,
                RoomDefinitionId = "spatial.room.basic", Ingress = new TileCoordinate(0, 1), Egress = new TileCoordinate(3, 1),
                TraversableTiles = IntraroomSnapshot.DeriveTraversableTiles(new RoomSpatialDefinition {
                    GrossFootprint = new RectangularFootprintDefinition(4, 4) }, 16), MaximumMaterializedTiles = 16,
                Assignments = assignments.Select(a => new IntraroomAssignment { AssignmentId = a.AssignmentId,
                    OccupiedTiles = new[] { a.RoomLocalPosition } }).ToArray() };
            return r;
        }
        internal static RunRoomAssignment Assignment(string id, string category, string option, int x, int y, long sequence = 0) =>
            new RunRoomAssignment { AssignmentId = id, CategoryId = category, OptionId = option,
                RoomLocalPosition = new TileCoordinate(x, y), Sequence = sequence };
        internal static RunRoomAssignment[] Content() => new[] {
            Assignment("m1", MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 1, 3),
            Assignment("m2", MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 3, 3, 1),
            Assignment("t1", MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId, 1, 1),
            Assignment("t2", MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SnareTrapOptionId, 2, 1, 1),
            Assignment("l1", MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId, 3, 1),
            Assignment("l2", MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId, 3, 3, 1) };
        internal static RunSimulationConfig Configuration(string mode)
        {
            var c = PhaseSixA4Tests.Config(); PhaseSixA4Tests.ForceDescent(c, true);
            if (mode == "retreat") { c.BaseSuccessChance = 0; c.SuccessThreshold = 1; }
            if (mode == "wipe") {
                c.PhaseFiveB.MinPartySize = c.PhaseFiveB.MaxPartySize = 3;
                foreach (var p in c.PhaseFiveB.DamageProfiles) p.MinimumDamage = p.MaximumDamage = int.MaxValue;
            }
            return c;
        }
        // Reflection is test-only: Unity JSON omits public properties and nonserialized transient evidence.
        internal static string Structured(object value)
        {
            var lines = new List<string>(); Flatten("root", value, lines); return string.Join("\n", lines);
        }
        private static void Flatten(string path, object value, List<string> lines)
        {
            if (value == null) { lines.Add(path + "=null"); return; }
            var t = value.GetType();
            if (t.IsPrimitive || t.IsEnum || value is string || value is decimal) {
                lines.Add(path + "=" + (value is double d ? d.ToString("R", CultureInfo.InvariantCulture) :
                    Convert.ToString(value, CultureInfo.InvariantCulture))); return;
            }
            if (value is IEnumerable array) { int i = 0; foreach (var child in array) Flatten(path + "[" + i++ + "]", child, lines);
                lines.Add(path + ".length=" + i); return; }
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(f => f.Name, StringComparer.Ordinal))
                Flatten(path + "." + f.Name, f.GetValue(value), lines);
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name, StringComparer.Ordinal)) Flatten(path + "." + p.Name, p.GetValue(value), lines);
        }
        [Test]
        public void QualifiedPreRefactorStructuredFixturesRemainEquivalent()
        {
            var rows = new List<string>();
            foreach (bool spatial in new[] { false, true })
            foreach (int rooms in new[] { 1, 2 })
            foreach (string mode in new[] { "clear", "retreat", "wipe" })
            foreach (string posture in new[] { RunPostureResolver.BalancedId, RunPostureResolver.CautiousId, RunPostureResolver.GreedyId }) {
                var c = Configuration(mode); var runtime = new StructureRuntimeState { Heat = 9.75, ManaReserve = 100 };
                var route = Enumerable.Range(0, rooms).Select(i => Room(i, spatial, Content())).ToArray();
                var result = new RunSimulationService(c, PhaseSixA4Tests.Loot()).SimulateRoute(runtime, 42, 1, posture, route);
                rows.Add("CASE " + spatial + "/" + rooms + "/" + mode + "/" + posture + "\n" + Structured(result) + "\n" + Structured(runtime));
            }
            foreach (bool branch in new[] { true, false }) {
                var f = branch ? PhaseFiveBBranchIntegrationTests.Fixture() : PhaseSixA4Tests.Eligible();
                if (!branch) {
                    PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
                    PhaseSixA4Tests.AddContent(f, 1, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
                    PhaseSixA4Tests.ForceDescent(f.Configuration, true);
                    f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                        FloorLifecycleAction.Activate, PhaseSixA4Tests.FloorTwo(f)));
                }
                var result = PhaseSixA4Tests.Writer(f).CommitPhaseFiveBRun(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                    new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot()), RunPostureResolver.BalancedId, 1000);
                Assert.That(result.IsSuccess, Is.True, result.Reason); f.Accept(result);
                rows.Add("CASE canonical/" + branch + "\n" + Structured(f.Runtime.runHistory.LatestOutcome) + "\n" +
                    JsonUtility.ToJson(f.Runtime.structureRuntime) + "\n" + JsonUtility.ToJson(f.Runtime.sharedBranchKnowledge) + "\n" +
                    JsonUtility.ToJson(f.Runtime.sharedFloorKnowledge) + "\n" + JsonUtility.ToJson(f.Runtime.runHistory));
            }
            Assert.That(string.Join("\n", rows), Is.EqualTo(File.ReadAllText(
                "Assets/_Project/Tests/EditMode/phase-a-baseline.txt")));
            Assert.That(rows.Count, Is.EqualTo(38));
        }
    }
}
#endif
