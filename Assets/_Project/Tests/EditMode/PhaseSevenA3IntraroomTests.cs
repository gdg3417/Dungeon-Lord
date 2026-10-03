#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class PhaseSevenA3IntraroomTests
    {
        private static TileCoordinate T(int x, int y) => new TileCoordinate(x, y);
        private static RunRoomAssignment A(string id, string category, string option, int x, int y, long sequence = 0) =>
            new RunRoomAssignment { AssignmentId = id, CategoryId = category, OptionId = option,
                RoomLocalPosition = T(x, y), Sequence = sequence };
        private static RunRoomAssignment Trap(int x, int y) => A("trap", MvpDungeonPlacementIds.TrapCategoryId,
            MvpDungeonPlacementIds.SpikeTrapOptionId, x, y);
        private static RunRoomAssignment Loot(string id, int x, int y, long sequence = 0) => A(id,
            MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId, x, y, sequence);
        private static RunRoomAssignment Monster(string id, int x, int y, long sequence = 0) => A(id,
            MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, x, y, sequence);

        // Synthetic geometry/config is confined to this fixture. Production snapshot tests follow below.
        private static MvpOrderedRouteRoom Room(params RunRoomAssignment[] assignments)
        {
            var tiles = IntraroomSnapshot.DeriveTraversableTiles(new RoomSpatialDefinition {
                GrossFootprint = new RectangularFootprintDefinition(4, 4) }, 16);
            return new MvpOrderedRouteRoom { FloorIndex = 0, RoomIndex = 0, RoomInstanceId = "room.test",
                IncludeRoomPlacement = true, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                Assignments = assignments, Spatial = new IntraroomSnapshot { FloorInstanceId = "floor.test",
                    RoomInstanceId = "room.test", RoomDefinitionId = "spatial.room.basic", Ingress = T(0, 1),
                    Egress = T(3, 1), TraversableTiles = tiles, MaximumMaterializedTiles = 16,
                    Assignments = assignments.Select(a => new IntraroomAssignment { AssignmentId = a.AssignmentId,
                        OccupiedTiles = new[] { a.RoomLocalPosition } }).ToArray() } };
        }
        private static RunSimulationConfig Config()
        {
            var c = PhaseSixA4Tests.Config(); PhaseSixA4Tests.ForceDescent(c, true); return c;
        }
        private static RunOutcomeRecord Run(MvpOrderedRouteRoom room, RunSimulationConfig config = null) =>
            new RunSimulationService(config ?? Config(), PhaseSixA4Tests.Loot()).SimulateRoute(
                new StructureRuntimeState { ManaReserve = 100 }, 42, 1, RunPostureResolver.BalancedId, new[] { room });

        [TestCase("spatial.room.basic", CardinalOrientation.Zero, 16)]
        [TestCase("spatial.room.rectangle", CardinalOrientation.Zero, 15)]
        [TestCase("spatial.room.rectangle", CardinalOrientation.Ninety, 15)]
        [TestCase("spatial.room.large_chamber", CardinalOrientation.Zero, 30)]
        [TestCase("spatial.room.large_chamber", CardinalOrientation.Ninety, 30)]
        public void ProductionGeometryAndSupportedTransformsUseAuthoredFootprint(string id, CardinalOrientation orientation, int count)
        {
            var f = Fixture.Create(null); var d = f.Production.Catalog.Rooms.Single(r => r.RoomDefinitionId == id);
            var cells = IntraroomSnapshot.DeriveTraversableTiles(d, f.Profile.Canonical.Spatial.MaximumMaterializedTiles);
            Assert.That(cells, Has.Length.EqualTo(count));
            foreach (var cell in cells)
            {
                Assert.That(RoomLocalCoordinateTransform.TryToFloor(cell, d.GrossFootprint, T(7, 9), orientation,
                    out var global), Is.True);
                Assert.That(d.ResolveUsableTiles(T(7, 9), orientation, new SpatialValidationWorkloadLimits(4096)),
                    Does.Contain(global));
            }
            var paths = new DeterministicRoomPaths(cells, 4096);
            Assert.That(paths.TryShortest(cells.First(), new[] { cells.Last() }, out var path), Is.True);
            Assert.That(path.Length, Is.EqualTo(d.GrossFootprint.Width + d.GrossFootprint.Height - 1));
        }

        [Test]
        public void ReservedTilesAndEqualLengthTieBreakAreCanonicalIndependentOfInsertionOrder()
        {
            var d = new RoomSpatialDefinition { GrossFootprint = new RectangularFootprintDefinition(3, 3),
                ReservedTileOffsets = new[] { T(1, 1) } };
            var tiles = IntraroomSnapshot.DeriveTraversableTiles(d, 9);
            var expected = new[] { T(0, 0), T(0, 1), T(0, 2), T(1, 2), T(2, 2) };
            foreach (var input in new[] { tiles, tiles.Reverse().ToArray() })
            {
                Assert.That(new DeterministicRoomPaths(input, 9).TryShortest(T(0, 0), new[] { T(2, 2) }, out var path), Is.True);
                CollectionAssert.AreEqual(expected, path);
                Assert.That(path, Has.No.Member(T(1, 1)));
            }
        }

        [Test]
        public void EquidistantObjectiveTilesChooseCoordinateOrder()
        {
            var paths = new DeterministicRoomPaths(Room().Spatial.TraversableTiles, 16);
            Assert.That(paths.TryShortest(T(1, 1), new[] { T(2, 1), T(0, 1) }, out var path), Is.True);
            Assert.That(path.Last(), Is.EqualTo(T(0, 1)));
        }

        [TestCase(true)] [TestCase(false)]
        public void TrapDamageAndEveryReachedAggregateFollowIntersection(bool crossed)
        {
            var baseline = Run(Room()); var a = Trap(1, crossed ? 1 : 3); var result = Run(Room(a));
            Assert.That(result.EncounterEvents.Length, Is.EqualTo(crossed ? 1 : 0));
            Assert.That(result.SpatialEvents.Count(e => e.Kind == RunSpatialEventKind.TrapTriggered), Is.EqualTo(crossed ? 1 : 0));
            Assert.That(result.ConfiguredRoutePlacementEffects.ContributingOptionIds, Does.Contain(a.OptionId));
            if (crossed)
            {
                Assert.That(result.EncounterEvents.Single().Damage, Is.GreaterThan(0));
                Assert.That(result.ReachedRoutePlacementEffects.ContributingOptionIds, Does.Contain(a.OptionId));
            }
            else
            {
                Assert.That(JsonUtility.ToJson(result.ReachedRoutePlacementEffects), Is.EqualTo(JsonUtility.ToJson(baseline.ReachedRoutePlacementEffects)));
                Assert.That(JsonUtility.ToJson(result.ClearedRewardPlacementEffects), Is.EqualTo(JsonUtility.ToJson(baseline.ClearedRewardPlacementEffects)));
                Assert.That(JsonUtility.ToJson(result.CompositionOutcomeSummary), Is.EqualTo(JsonUtility.ToJson(baseline.CompositionOutcomeSummary)));
                Assert.That(result.FinalChance, Is.EqualTo(baseline.FinalChance));
                Assert.That(JsonUtility.ToJson(result.LootSummary), Is.EqualTo(JsonUtility.ToJson(baseline.LootSummary)));
                Assert.That(JsonUtility.ToJson(result.LootExtractionSummary), Is.EqualTo(JsonUtility.ToJson(baseline.LootExtractionSummary)));
                Assert.That(JsonUtility.ToJson(result.RunHeatDeltaSummary), Is.EqualTo(JsonUtility.ToJson(baseline.RunHeatDeltaSummary)));
                Assert.That(JsonUtility.ToJson(result.AdventurerAttractionSummary), Is.EqualTo(JsonUtility.ToJson(baseline.AdventurerAttractionSummary)));
                Assert.That(JsonUtility.ToJson(result.AdventurerDemandBudgetSummary), Is.EqualTo(JsonUtility.ToJson(baseline.AdventurerDemandBudgetSummary)));
            }
        }

        [Test]
        public void MultiTileTrapTriggersOnceOnAnyOccupiedTileEvenAcrossRepeatedTraversal()
        {
            var room = Room(Trap(1, 0), Loot("a", 3, 3), Loot("b", 0, 0, 1));
            room.Spatial.Assignments[0].OccupiedTiles = new[] { T(1, 0), T(1, 1) };
            var result = Run(room);
            Assert.That(result.EncounterEvents.Count(e => e.AssignmentId == "trap"), Is.EqualTo(1));
            var trigger = result.SpatialEvents.Single(e => e.Kind == RunSpatialEventKind.TrapTriggered);
            Assert.That(room.Spatial.Assignments[0].OccupiedTiles, Does.Contain(trigger.Position));
        }

        [Test]
        public void LootObjectivesUseSequenceThenIdAndRequirePhysicalInteraction()
        {
            var room = Room(Loot("z", 3, 3, 1), Loot("b", 2, 2), Loot("a", 0, 3));
            var result = Run(room);
            var reached = result.SpatialEvents.Where(e => e.Kind == RunSpatialEventKind.LootReached).ToArray();
            CollectionAssert.AreEqual(new[] { "a", "b", "z" }, reached.Select(e => e.AssignmentId));
            var path = result.SpatialEvents.Single(e => e.Kind == RunSpatialEventKind.AdventurerTraversal).Path;
            foreach (var e in reached) Assert.That(path[e.PathStep], Is.EqualTo(e.Position));
            Assert.That(result.ReachedRoutePlacementEffects.ContributingOptionIds.Count(id => id == MvpDungeonPlacementIds.HiddenCacheOptionId), Is.EqualTo(3));
            Assert.That(result.LootSummary.TotalGeneratedWorldValue, Is.GreaterThan(0));
            for (int i = 1; i < path.Length; i++) Assert.That(Math.Abs(path[i].X - path[i-1].X) + Math.Abs(path[i].Y - path[i-1].Y), Is.EqualTo(1));
        }

        [Test]
        public void UnreachableLootAndMonsterDoNotTeleportOrContributeEffects()
        {
            var room = Room(Loot("loot", 3, 3), Monster("monster", 3, 3));
            room.Spatial.TraversableTiles = room.Spatial.TraversableTiles.Where(t => !t.Equals(T(2, 3)) && !t.Equals(T(3, 2))).ToArray();
            var result = Run(room);
            Assert.That(result.SpatialEvents.Count(e => e.Kind == RunSpatialEventKind.LootUnreachable), Is.EqualTo(1));
            Assert.That(result.SpatialEvents.Count(e => e.Kind == RunSpatialEventKind.MonsterUnreachable), Is.EqualTo(1));
            Assert.That(result.EncounterEvents, Is.Empty);
            CollectionAssert.AreEqual(new[] { MvpDungeonPlacementIds.BasicRoomOptionId }, result.ReachedRoutePlacementEffects.ContributingOptionIds);
            Assert.That(result.SpatialEvents.Single(e => e.Kind == RunSpatialEventKind.AdventurerTraversal).Path, Has.No.Member(T(3, 3)));
        }

        [Test]
        public void MonsterPositionChangesEngagementStepAndMovementStartsAtCanonicalAnchor()
        {
            var first = Room(Monster("m", 0, 3)); var second = Room(Monster("m", 3, 3));
            var a = Run(first).SpatialEvents.Single(e => e.Kind == RunSpatialEventKind.MonsterEngaged);
            var b = Run(second).SpatialEvents.Single(e => e.Kind == RunSpatialEventKind.MonsterEngaged);
            Assert.That(a.PathStep, Is.EqualTo(0)); Assert.That(b.PathStep, Is.EqualTo(3));
            Assert.That(a.Path.First(), Is.EqualTo(T(0, 3))); Assert.That(b.Path.First(), Is.EqualTo(T(3, 3)));
            Assert.That(a.Path.Last(), Is.EqualTo(a.Position)); Assert.That(b.Path.Last(), Is.EqualTo(b.Position));
            Assert.That(first.Assignments[0].RoomLocalPosition, Is.EqualTo(T(0, 3)));
        }

        [Test]
        public void EventTiesUseMonsterTrapLootThenSequenceAndOrdinalId()
        {
            var room = Room(Loot("loot", 0, 1), Trap(0, 1), Monster("z", 0, 1, 1),
                Monster("b", 0, 1), Monster("a", 0, 1));
            var planned = IntraroomTraversal.Plan(room);
            CollectionAssert.AreEqual(new[] { "a", "b", "z", "trap", "loot" }, planned.Interactions.Select(e => e.Assignment.AssignmentId));
            var result = Run(room);
            CollectionAssert.AreEqual(Enumerable.Range(0, result.SpatialEvents.Length), result.SpatialEvents.Select(e => e.Ordinal));
        }

        [Test]
        public void WipeStopsPathAndLaterEffectsRewardsAndSettlementRemainCoherent()
        {
            var c = Config(); c.PhaseFiveB.MinPartySize = c.PhaseFiveB.MaxPartySize = 3;
            foreach (var p in c.PhaseFiveB.DamageProfiles) p.MinimumDamage = p.MaximumDamage = int.MaxValue;
            var room = Room(Monster("a", 0, 1), Monster("b", 0, 1, 1), Monster("c", 0, 1, 2),
                Trap(2, 1), Loot("loot", 3, 3));
            var result = Run(room, c);
            Assert.That(result.Party.IsWiped, Is.True); Assert.That(result.Success, Is.False);
            Assert.That(result.ReachedRoutePlacementEffects.ContributingOptionIds, Does.Not.Contain(MvpDungeonPlacementIds.HiddenCacheOptionId).And.Not.Contain(MvpDungeonPlacementIds.SpikeTrapOptionId));
            Assert.That(result.SpatialEvents.Any(e => e.Kind == RunSpatialEventKind.RoomEgressReached), Is.False);
            Assert.That(result.SpatialEvents.Single(e => e.Kind == RunSpatialEventKind.AdventurerTraversal).Path, Has.Length.EqualTo(1));
            Assert.That(result.LootSummary.TotalGeneratedWorldValue, Is.Zero);
            Assert.That(result.LootExtractionSummary.TotalExtractedWorldValue, Is.Zero);
        }

        [Test]
        public void ChillingSigilKeepsConfiguredZeroDamage()
        {
            var a = A("chill", MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.ChillingSigilOptionId, 1, 1);
            Assert.That(Run(Room(a)).EncounterEvents.Single().Damage, Is.Zero);
        }

        [Test]
        public void EquivalentArrangementsAndRepeatedRunIdentitiesHaveIdenticalEvidenceAndOutcomes()
        {
            var a = Room(Monster("m", 2, 3), Trap(1, 1), Loot("l", 3, 3));
            var b = Room(a.Assignments.Reverse().ToArray()); b.Spatial.TraversableTiles = b.Spatial.TraversableTiles.Reverse().ToArray();
            var one = Run(a); var two = Run(b); var repeat = Run(a);
            Assert.That(JsonUtility.ToJson(one), Is.EqualTo(JsonUtility.ToJson(two)).And.EqualTo(JsonUtility.ToJson(repeat)));
            CollectionAssert.AreEqual(SpatialFingerprint(one), SpatialFingerprint(two));
            CollectionAssert.AreEqual(SpatialFingerprint(one), SpatialFingerprint(repeat));
            CollectionAssert.AreEqual(one.EncounterEvents.Select(e => e.Damage), two.EncounterEvents.Select(e => e.Damage));
        }
        private static string[] SpatialFingerprint(RunOutcomeRecord run) => run.SpatialEvents.Select(e =>
            e.Ordinal + ":" + e.Kind + ":" + e.AssignmentId + ":" + e.PathStep + ":" + e.Position.X + "," + e.Position.Y +
            ":" + string.Join(";", (e.Path ?? Array.Empty<TileCoordinate>()).Select(t => t.X + "," + t.Y))).ToArray();

        private static Fixture CanonicalRoom()
        {
            var f = Fixture.Create(null); f.Configuration = Config();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)));
            return f;
        }

        [Test]
        public void CanonicalProjectionSnapshotIsolationAndTransientMovementPreserveExactPositions()
        {
            var f = CanonicalRoom();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId, null, null, T(0, 3))));
            var snapshot = PhaseSixA4Tests.Snapshot(f); var engine = PhaseFiveBBranchIntegrationTests.Service(f.Configuration);
            var room = snapshot.Floors[0].MaterializePlan().RequiredRooms.Single().Room;
            Assert.That(room.Assignments.Single().RoomLocalPosition, Is.EqualTo(T(0, 3)));
            string canonical = JsonUtility.ToJson(f.State);
            var first = engine.SimulateSnapshot(1, snapshot);
            Assert.That(JsonUtility.ToJson(f.State), Is.EqualTo(canonical));
            room.Assignments[0].RoomLocalPosition = T(3, 0); room.Spatial.TraversableTiles = Array.Empty<TileCoordinate>();
            var changed = f.State; changed.Floors[0].RoomContents.Assignments[0].RoomLocalPosition = T(3, 3);
            f = f.Rebase(changed);
            var second = engine.SimulateSnapshot(1, PhaseSixA4Tests.Snapshot(f));
            CollectionAssert.AreEqual(SpatialFingerprint(first), SpatialFingerprint(engine.SimulateSnapshot(1, snapshot)));
            Assert.That(SpatialFingerprint(first), Is.Not.EqualTo(SpatialFingerprint(second)));
            Assert.That(f.State.Floors[0].RoomContents.Assignments.Single().RoomLocalPosition, Is.EqualTo(T(3, 3)));
        }

        [Test]
        public void CanonicalPublicationRetainsSpatialEvidenceAndReopenOmitsItWithoutSchemaChange()
        {
            var f = CanonicalRoom();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.LootNodeCategoryId,
                MvpDungeonPlacementIds.HiddenCacheOptionId, null, null, T(3, 3))));
            f.Accept(PhaseFiveBBranchIntegrationTests.Run(f));
            var run = f.Runtime.runHistory.LatestOutcome; Assert.That(run.SpatialEvents, Is.Not.Empty);
            var evidence = run.SpatialEvents;
            var source = f.Runtime;
            var publication = f.Execute(DetachedCanonicalMutationRequest.Unassign(f.State.Floors[0].RoomContents.Assignments[0].AssignmentId));
            // GameRoot invokes this established retention boundary on canonical publication.
            RunTransientEvidence.Retain(source, publication.RuntimeProjection);
            f.Accept(publication);
            Assert.That(f.Runtime.runHistory.LatestOutcome.SpatialEvents, Is.SameAs(evidence));
            Assert.That(JsonUtility.ToJson(run), Does.Not.Contain("SpatialEvents").And.Not.Contain("TraversableTiles"));
            f.Reopen(); Assert.That(f.Runtime.runHistory.LatestOutcome.SpatialEvents, Is.Null);
            Assert.That(SaveMigration.LatestSchemaVersion, Is.EqualTo(13));
        }

        [Test]
        public void DirectAndPhysicalSavedConnectionsResolveFromSameRenovationGeometry()
        {
            var f = CanonicalRoom(); var floor = f.State.Floors.Single();
            foreach (var edge in floor.Layout.Edges)
                Assert.That(StructuralRenovationService.TryResolveSavedConnection(floor, edge, f.Production.Catalog, out _, out _), Is.True);
            var preview = StructuralEditService.Preview(f.State, new StructuralConstructionRequest {
                RoomDefinitionId = "spatial.room.basic", Anchor = T(5, 2), Orientation = CardinalOrientation.Zero,
                TerminalConnectionPointId = "east" }, f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            floor = preview.DetachedCandidate.Floors.Single();
            var physical = floor.Layout.Edges.Single(e => e.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor);
            Assert.That(StructuralRenovationService.TryResolveSavedConnection(floor, physical, f.Production.Catalog,
                out var source, out var destination), Is.True);
            Assert.That(source, Is.EqualTo(T(3, 1))); Assert.That(destination, Is.EqualTo(T(0, 1)));
            Assert.That(PhaseSixA4Tests.Snapshot(f.Rebase(preview.DetachedCandidate)).Floors[0].MaterializePlan().RequiredRooms, Has.Length.EqualTo(2));
        }

        [TestCase(false)] [TestCase(true)]
        public void InvalidAndAmbiguousRequiredGeometryFailClosed(bool ambiguous)
        {
            var f = CanonicalRoom(); var floor = f.State.Floors.Single(); var catalog = f.Production.Catalog;
            var edge = floor.Layout.Edges.First();
            if (ambiguous)
            {
                var d = catalog.Rooms.Single(r => r.RoomDefinitionId == "spatial.room.basic");
                d.ConnectionPoints = d.ConnectionPoints.Concat(d.ConnectionPoints).ToArray();
            }
            else floor.Layout.Rooms[0].Anchor = T(100, 100);
            Assert.That(StructuralRenovationService.TryResolveSavedConnection(floor, edge, catalog, out _, out _), Is.False);
        }

        [TestCase(15)] [TestCase(0)]
        public void GeometryWorkloadOverflowFailsBeforeOutcomePublication(int limit)
        {
            var d = new RoomSpatialDefinition { GrossFootprint = new RectangularFootprintDefinition(4, 4) };
            Assert.Throws<ArgumentException>(() => IntraroomSnapshot.DeriveTraversableTiles(d, limit));
            Assert.Throws<ArgumentException>(() => new DeterministicRoomPaths(Room().Spatial.TraversableTiles, limit));
        }
        [Test]
        public void DisconnectedEgressAndMalformedAssignmentFailDeterministically()
        {
            var room = Room(Trap(1, 1)); room.Spatial.TraversableTiles = room.Spatial.TraversableTiles.Where(t => t.X != 2).ToArray();
            Assert.Throws<ArgumentException>(() => Run(room));
            room = Room(Trap(1, 1)); room.Spatial.Assignments[0].OccupiedTiles = new[] { T(50, 50) };
            Assert.Throws<ArgumentException>(() => Run(room));
        }

        [TestCase("spatial.room.basic", CardinalOrientation.Zero, 4, 2, "east")]
        [TestCase("spatial.room.rectangle", CardinalOrientation.Zero, 4, 1, "north")]
        [TestCase("spatial.room.rectangle", CardinalOrientation.Ninety, 4, 2, "west")]
        [TestCase("spatial.room.large_chamber", CardinalOrientation.Zero, 4, 1, "north")]
        [TestCase("spatial.room.large_chamber", CardinalOrientation.Ninety, 4, 1, "west")]
        public void ProductionSnapshotsTraverseActualRoomAndResolveOrientedRouteSockets(string id,
            CardinalOrientation orientation, int x, int y, string terminal)
        {
            var f = CanonicalRoom();
            if (id != "spatial.room.basic")
            {
                var preview = StructuralEditService.Preview(f.State, new StructuralConstructionRequest {
                    RoomDefinitionId = id, Anchor = T(x, y), Orientation = orientation,
                    TerminalConnectionPointId = terminal }, f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
                Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
                f = f.Rebase(preview.DetachedCandidate);
            }
            var snapshot = PhaseSixA4Tests.Snapshot(f);
            var plan = snapshot.Floors[0].MaterializePlan();
            var spatial = plan.RequiredRooms.Single(r => r.Room.Spatial.RoomDefinitionId == id).Room.Spatial;
            var result = PhaseFiveBBranchIntegrationTests.Service(f.Configuration).SimulateSnapshot(1, snapshot);
            var path = result.SpatialEvents.Single(e => e.Kind == RunSpatialEventKind.AdventurerTraversal &&
                e.RoomInstanceId == spatial.RoomInstanceId).Path;
            Assert.That(path.First(), Is.EqualTo(spatial.Ingress)); Assert.That(path.Last(), Is.EqualTo(spatial.Egress));
            Assert.That(path.All(t => spatial.TraversableTiles.Contains(t)), Is.True);
            var floor = f.State.Floors.Single();
            foreach (var room in plan.RequiredRooms)
            {
                var incoming = floor.Layout.Edges.Single(e => e.DestinationNodeId == room.NodeId);
                var outgoing = floor.Layout.Edges.Single(e => e.SourceNodeId == room.NodeId && e.Classification == RouteClassification.Required);
                Assert.That(StructuralRenovationService.TryResolveSavedConnection(floor, incoming, f.Production.Catalog, out _, out var ingress), Is.True);
                Assert.That(StructuralRenovationService.TryResolveSavedConnection(floor, outgoing, f.Production.Catalog, out var egress, out _), Is.True);
                Assert.That(room.Room.Spatial.Ingress, Is.EqualTo(ingress)); Assert.That(room.Room.Spatial.Egress, Is.EqualTo(egress));
            }
        }

        [Test]
        public void CanonicalTrapPositionChangesDamageWithoutChangingConfiguredEffects()
        {
            var f = CanonicalRoom();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.TrapCategoryId,
                MvpDungeonPlacementIds.SpikeTrapOptionId, null, null, T(1, 1))));
            var old = PhaseSixA4Tests.Snapshot(f); var service = PhaseFiveBBranchIntegrationTests.Service(f.Configuration);
            var crossed = service.SimulateSnapshot(1, old);
            f.State.Floors[0].RoomContents.Assignments[0].RoomLocalPosition = T(3, 3); f = f.Rebase(f.State);
            var bypassed = service.SimulateSnapshot(1, PhaseSixA4Tests.Snapshot(f));
            Assert.That(crossed.EncounterEvents, Has.Length.EqualTo(1)); Assert.That(bypassed.EncounterEvents, Is.Empty);
            Assert.That(bypassed.LootSummary.TotalGeneratedWorldValue, Is.Zero);
            Assert.That(JsonUtility.ToJson(crossed.ConfiguredRoutePlacementEffects), Is.EqualTo(JsonUtility.ToJson(bypassed.ConfiguredRoutePlacementEffects)));
            Assert.That(crossed.ReachedRoutePlacementEffects.Danger, Is.GreaterThan(bypassed.ReachedRoutePlacementEffects.Danger));
            CollectionAssert.AreEqual(SpatialFingerprint(crossed), SpatialFingerprint(service.SimulateSnapshot(1, old)));
        }

        [Test]
        public void MovingLootChangesTrapReachAndDeterministicDamage()
        {
            var a = Run(Room(Trap(0, 2), Loot("l", 0, 1)));
            var b = Run(Room(Trap(0, 2), Loot("l", 0, 3)));
            Assert.That(a.EncounterEvents, Is.Empty); Assert.That(b.EncounterEvents, Has.Length.EqualTo(1));
            Assert.That(a.Party.Members.Sum(m => m.CurrentHealth), Is.GreaterThan(b.Party.Members.Sum(m => m.CurrentHealth)));
        }

        [Test]
        public void MonsterDistanceTiesChooseEarliestPathStepAcrossRepeatedTiles()
        {
            var room = Room(Monster("m", 1, 2), Loot("first", 3, 3), Loot("second", 0, 1, 1));
            var plan = IntraroomTraversal.Plan(room);
            var engaged = plan.Interactions.Single(e => e.Kind == RunSpatialEventKind.MonsterEngaged);
            Assert.That(engaged.Step, Is.EqualTo(1)); Assert.That(plan.Path[engaged.Step], Is.EqualTo(T(0, 2)));
            CollectionAssert.AreEqual(new[] { T(1, 2), T(0, 2) }, engaged.Movement);
        }

        [Test]
        public void LootUsesNearestOccupiedInteractionTileRatherThanAssumingOneTile()
        {
            var room = Room(Loot("l", 3, 3));
            room.Spatial.Assignments.Single().OccupiedTiles = new[] { T(3, 3), T(0, 2), T(1, 1) };
            var e = Run(room).SpatialEvents.Single(value => value.Kind == RunSpatialEventKind.LootReached);
            Assert.That(e.Position, Is.EqualTo(T(0, 2))); Assert.That(e.PathStep, Is.EqualTo(1));
        }

        [Test]
        public void LootReachStillRequiresRoomClearForRewardAndExtraction()
        {
            var c = Config(); c.BaseSuccessChance = 0; c.SuccessThreshold = 1;
            var result = Run(Room(Loot("l", 0, 1)), c);
            Assert.That(result.SpatialEvents.Any(e => e.Kind == RunSpatialEventKind.LootReached), Is.True);
            Assert.That(result.Success, Is.False); Assert.That(result.LootSummary.TotalGeneratedWorldValue, Is.Zero);
            Assert.That(result.LootExtractionSummary.TotalExtractedWorldValue, Is.Zero);
            Assert.That(result.ClearedRewardPlacementEffects.ContributingOptionIds, Is.Empty);
        }

        [Test]
        public void TwoFloorSpatialRunsReuseRunIdentityPartyHealthTransitionsAndKnowledge()
        {
            var f = PhaseSixA4Tests.Eligible(); PhaseSixA4Tests.ForceDescent(f.Configuration, true);
            foreach (int floor in new[] { 0, 1 }) PhaseSixA4Tests.AddContent(f, floor,
                MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
            f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                FloorLifecycleAction.ActivateAllEligible, null));
            var snapshot = PhaseSixA4Tests.Snapshot(f); var engine = PhaseFiveBBranchIntegrationTests.Service(f.Configuration);
            var first = engine.SimulateSnapshot(1, snapshot); var repeat = engine.SimulateSnapshot(1, snapshot);
            CollectionAssert.AreEqual(SpatialFingerprint(first), SpatialFingerprint(repeat));
            Assert.That(first.SpatialEvents.Select(e => e.FloorInstanceId).Distinct().Count(), Is.EqualTo(2));
            Assert.That(first.Party.RunId, Is.EqualTo(first.RunId));
            var original = RunPartyGenerator.Create(f.Configuration.PhaseFiveB, first.RunId);
            foreach (var member in first.Party.Members)
            {
                Assert.That(member.ClassId, Is.EqualTo(original.Members[member.MemberOrdinal].ClassId));
                Assert.That(member.CurrentHealth, Is.EqualTo(Math.Max(0, member.MaxHealth -
                    first.EncounterEvents.Where(e => e.MemberOrdinal == member.MemberOrdinal).Sum(e => e.Damage))));
            }
            Assert.That(first.FloorTransitions, Has.Length.EqualTo(2));
            var knowledge = FloorKnowledgeLearning.Propose(snapshot, first);
            Assert.That(knowledge.Records, Has.Length.EqualTo(2));
            foreach (var record in knowledge.Records)
            {
                int floor = snapshot.Floors.Single(s => s.FloorInstanceId == record.FloorInstanceId).FloorIndex;
                Assert.That(record.PerceivedDangerScore, Is.EqualTo(first.RoomResolutions.Where(r => r.FloorIndex == floor)
                    .Sum(r => r.LocalPlacementEffects.Danger)));
            }
        }
    }
}
#endif
