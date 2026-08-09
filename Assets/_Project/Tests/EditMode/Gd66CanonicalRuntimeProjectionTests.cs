using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66CanonicalRuntimeProjectionTests
    {
        [Test]
        public void ContextValidatedProductionFixturePublishesCanonicalGraphAuthority()
        {
            SaveData save = PublishedProductionFixture();

            CanonicalMvpRouteProjectionResult inspected =
                CanonicalMvpRouteProjection.Inspect(save, null);
            MvpOrderedRouteRoom[] route = MvpOrderedRoomRouteResolver.Resolve(save, null);

            Assert.That(inspected.AuthorityState,
                Is.EqualTo(CanonicalMvpRuntimeAuthorityState.ValidatedCanonical));
            Assert.That(route, Has.Length.EqualTo(2));
            Assert.That(route[0].AssignedMonsterOptionIds,
                Is.EqualTo(new[] { MvpDungeonPlacementIds.SkeletonOptionId }));
            Assert.That(route[1].AssignedTrapOptionIds,
                Is.EqualTo(new[] { MvpDungeonPlacementIds.SpikeTrapOptionId }));
        }

        [Test]
        public void TamperedLegacyEvidenceCannotChangePublishedCanonicalProjection()
        {
            SaveData save = PublishedProductionFixture();
            string before = Signature(MvpOrderedRoomRouteResolver.Resolve(save, null));
            save.mvpDungeonPlacements = new MvpDungeonPlacementState
            {
                Entries = { new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.NarrowHallOptionId, 999) }
            };
            save.mvpDungeonFloorLayout = null;
            save.mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection
            {
                Rooms = { new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = 0,
                    RoomOptionId = MvpDungeonPlacementIds.NarrowHallOptionId } }
            };

            Assert.That(Signature(MvpOrderedRoomRouteResolver.Resolve(save, null)), Is.EqualTo(before));
        }

        [Test]
        public void ReplacingPublishedMarkerFailsClosedInsteadOfRetainingAuthority()
        {
            SaveData save = PublishedProductionFixture();
            save.canonicalSpatialAuthority = new CanonicalSpatialAuthorityMarker
            {
                CanonicalLayoutContractVersion = 1,
                CreationKind = CanonicalSpatialCreationKind.NativeCanonical
            };

            CanonicalMvpRouteProjectionResult result =
                CanonicalMvpRouteProjection.Inspect(save, null);

            Assert.That(result.AuthorityState,
                Is.EqualTo(CanonicalMvpRuntimeAuthorityState.ContradictoryCanonical));
            Assert.That(result.Reason,
                Is.EqualTo(CanonicalMvpRouteProjection.ContradictoryAuthorityReason));
        }

        [TestCase(MalformedKind.MarkerOnly)]
        [TestCase(MalformedKind.DuplicateFloor)]
        [TestCase(MalformedKind.DuplicateEntrance)]
        [TestCase(MalformedKind.DuplicateNodeId)]
        [TestCase(MalformedKind.DuplicateRoomId)]
        [TestCase(MalformedKind.DuplicateSemantics)]
        [TestCase(MalformedKind.DuplicateEdgeId)]
        [TestCase(MalformedKind.MissingCompletion)]
        [TestCase(MalformedKind.Cycle)]
        [TestCase(MalformedKind.RouteGap)]
        public void UnpublishedCanonicalLookingStateFailsClosedWithoutThrowing(MalformedKind kind)
        {
            SaveData save = MalformedCanonicalLookingSave(kind);

            Assert.DoesNotThrow(() => CanonicalMvpRouteProjection.Inspect(save, null));
            CanonicalMvpRouteProjectionResult result =
                CanonicalMvpRouteProjection.Inspect(save, null);
            Assert.That(result.AuthorityState,
                Is.EqualTo(CanonicalMvpRuntimeAuthorityState.ContradictoryCanonical));
            Assert.That(result.Reason,
                Is.EqualTo(CanonicalMvpRouteProjection.ContradictoryAuthorityReason));
            Assert.That(MvpOrderedRoomRouteResolver.Resolve(save, null), Is.Empty);
            Assert.That(CanonicalMvpRouteProjection.IsCanonical(save), Is.False);
        }

        [TestCase(MalformedKind.DuplicateFloor)]
        [TestCase(MalformedKind.DuplicateEntrance)]
        [TestCase(MalformedKind.DuplicateNodeId)]
        [TestCase(MalformedKind.DuplicateRoomId)]
        [TestCase(MalformedKind.DuplicateSemantics)]
        [TestCase(MalformedKind.DuplicateEdgeId)]
        [TestCase(MalformedKind.MissingCompletion)]
        [TestCase(MalformedKind.Cycle)]
        [TestCase(MalformedKind.RouteGap)]
        public void MutationAfterValidatedPublicationFailsClosedWithoutThrowing(MalformedKind kind)
        {
            SaveData save = PublishedProductionFixture();
            MutatePublished(save, kind);

            CanonicalMvpRouteProjectionResult result = null;
            Assert.DoesNotThrow(() => result =
                CanonicalMvpRouteProjection.Inspect(save, null));
            Assert.That(result.AuthorityState,
                Is.EqualTo(CanonicalMvpRuntimeAuthorityState.ContradictoryCanonical));
            Assert.That(result.Reason,
                Is.EqualTo(CanonicalMvpRouteProjection.ContradictoryAuthorityReason));
        }

        [Test]
        public void NonContextualValidationCannotPublishRuntimeAuthority()
        {
            var nonContextual = new DetachedCompleteSaveValidationResult(
                Encoding.UTF8.GetBytes("{}"), null, 1,
                new DetachedCanonicalSpatialSaveState
                {
                    Authority = new CanonicalSpatialAuthorityMarker
                    { CanonicalLayoutContractVersion = 1,
                        CreationKind = CanonicalSpatialCreationKind.NativeCanonical },
                    Floors = Array.Empty<SavedSpatialFloor>()
                });

            Assert.That(CanonicalMvpRouteProjection.TryPublishValidated(nonContextual,
                out SaveData save, out string reason), Is.False);
            Assert.That(reason, Is.EqualTo(
                CanonicalMvpRouteProjection.ContradictoryAuthorityReason));
            Assert.That(save, Is.Null);
            Assert.That(CanonicalMvpRouteProjection.IsCanonical(save), Is.False);
        }

        [Test]
        public void CanonicalLookingStateDoesNotReviveOrMutateLegacyModels()
        {
            SaveData save = MalformedCanonicalLookingSave(MalformedKind.MarkerOnly);
            save.mvpDungeonPlacements = null;
            save.mvpDungeonFloorLayout = null;
            save.mvpRoomSlotAssignments = null;

            SaveMigration.MigrateToLatest(new SaveRoot { schemaVersion = 6, primary = save });
            string before = JsonUtility.ToJson(save);
            Assert.That(MvpRoomSlotLayoutResolver.TryAssignToPersistedRoom(save, null, 0,
                MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.GoblinOptionId), Is.False);
            Assert.That(MvpRoomSlotLayoutResolver.TryAddSecondBasicRoomSlot(save, null), Is.False);
            MvpRoomSlotLayoutResolver.SetPersistedRoomOptionIfPresent(save, null, 0,
                MvpDungeonPlacementIds.BasicRoomOptionId);

            Assert.That(save.mvpDungeonPlacements, Is.Null);
            Assert.That(save.mvpDungeonFloorLayout, Is.Null);
            Assert.That(save.mvpRoomSlotAssignments, Is.Null);
            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
            Assert.That(save.dungeonLayout, Is.Not.Null);
            Assert.That(save.structureRuntime, Is.Not.Null);
        }

        [Test]
        public void LegacySaveServiceRefusesCanonicalLookingWrite()
        {
            string directory = Path.Combine(Path.GetTempPath(), "gd66-legacy-save-guard-" +
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var service = new SaveService(new SimpleLogger(false), new SaveConfig
                { fileName = "save.json", useAtomicWrites = true }, directory);
                LogAssert.Expect(LogType.Error, new Regex(Regex.Escape(
                    "[ERROR] Legacy save write rejected for canonical-looking authority.")));

                service.Save(MalformedCanonicalLookingSave(MalformedKind.MarkerOnly),
                    SaveReason.ManualDev);

                Assert.That(File.Exists(service.SavePath), Is.False);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private static SaveData PublishedProductionFixture()
        {
            const string members = "\"mvpRoomSlotAssignments\":{\"Rooms\":[" +
                "{\"FloorIndex\":0,\"RoomIndex\":0,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[\"placement.option.monster.skeleton\"],\"TrapOptionIds\":[]," +
                "\"LootNodeOptionIds\":[]}," +
                "{\"FloorIndex\":0,\"RoomIndex\":1,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[],\"TrapOptionIds\":[\"placement.option.trap.spike\"]," +
                "\"LootNodeOptionIds\":[]}],\"NextRevision\":3}";
            Gd66DetachedSpatialMigrationTransactionTests.SemanticFixtureExecution fixture =
                Gd66DetachedSpatialMigrationTransactionTests.RunPopulatedSemanticFixture(
                    "runtime-projection", 6, members);
            DetachedCompleteSaveValidationResult validated =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                    fixture.Attempt.Candidate.GetBytes(), fixture.CurrentContext);
            Assert.That(validated.IsValid, Is.True);
            Assert.That(CanonicalMvpRouteProjection.TryPublishValidated(validated,
                out SaveData save, out string reason), Is.True, reason);
            return save;
        }

        private static SaveData MalformedCanonicalLookingSave(MalformedKind kind)
        {
            var save = new SaveData
            {
                canonicalSpatialAuthority = new CanonicalSpatialAuthorityMarker
                {
                    CanonicalLayoutContractVersion = 1,
                    CreationKind = CanonicalSpatialCreationKind.NativeCanonical
                }
            };
            if (kind == MalformedKind.MarkerOnly) return save;
            SavedSpatialFloor floor = MinimalFloor();
            save.spatialFloors = kind == MalformedKind.DuplicateFloor
                ? new[] { floor, MinimalFloor() } : new[] { floor };
            switch (kind)
            {
                case MalformedKind.DuplicateEntrance:
                    floor.Layout.Nodes = floor.Layout.Nodes.Concat(new[]
                    { Node("node.other-entrance", FloorRouteNodeKind.Entrance) }).ToArray(); break;
                case MalformedKind.DuplicateNodeId:
                    floor.Layout.Nodes = floor.Layout.Nodes.Concat(new[]
                    { Node("node.entrance", FloorRouteNodeKind.Room, "room.00") }).ToArray(); break;
                case MalformedKind.DuplicateRoomId:
                    floor.Layout.Rooms = floor.Layout.Rooms.Concat(new[] { Room("room.00") }).ToArray(); break;
                case MalformedKind.DuplicateSemantics:
                    floor.RoomContents.RoomSemantics = floor.RoomContents.RoomSemantics.Concat(new[]
                    { Semantics("room.00") }).ToArray(); break;
                case MalformedKind.DuplicateEdgeId:
                    floor.Layout.Edges = floor.Layout.Edges.Concat(new[]
                    { Edge("edge.0", "node.room", "node.completion") }).ToArray(); break;
                case MalformedKind.MissingCompletion:
                    floor.Layout.Nodes = floor.Layout.Nodes.Where(node =>
                        node.Kind != FloorRouteNodeKind.Completion).ToArray(); break;
                case MalformedKind.Cycle:
                    floor.Layout.Edges = new[] { Edge("edge.0", "node.entrance", "node.room"),
                        Edge("edge.1", "node.room", "node.entrance") }; break;
                case MalformedKind.RouteGap:
                    floor.Layout.Edges = new[] { Edge("edge.0", "node.entrance", "node.completion") }; break;
            }
            return save;
        }

        private static void MutatePublished(SaveData save, MalformedKind kind)
        {
            SavedSpatialFloor floor = save.spatialFloors[0];
            switch (kind)
            {
                case MalformedKind.DuplicateFloor:
                    save.spatialFloors = save.spatialFloors.Concat(new[] { floor }).ToArray(); break;
                case MalformedKind.DuplicateEntrance:
                    floor.Layout.Nodes = floor.Layout.Nodes.Concat(new[]
                    { Node("node.other-entrance", FloorRouteNodeKind.Entrance) }).ToArray(); break;
                case MalformedKind.DuplicateNodeId:
                    floor.Layout.Nodes = floor.Layout.Nodes.Concat(new[]
                    { Node(floor.Layout.Nodes[0].NodeId, FloorRouteNodeKind.Room,
                        floor.Layout.Rooms[0].RoomInstanceId) }).ToArray(); break;
                case MalformedKind.DuplicateRoomId:
                    floor.Layout.Rooms = floor.Layout.Rooms.Concat(new[]
                    { floor.Layout.Rooms[0] }).ToArray(); break;
                case MalformedKind.DuplicateSemantics:
                    floor.RoomContents.RoomSemantics = floor.RoomContents.RoomSemantics.Concat(new[]
                    { floor.RoomContents.RoomSemantics[0] }).ToArray(); break;
                case MalformedKind.DuplicateEdgeId:
                    FloorRouteEdge source = floor.Layout.Edges[0];
                    floor.Layout.Edges = floor.Layout.Edges.Concat(new[]
                    { Edge(source.EdgeId, source.SourceNodeId, source.DestinationNodeId) }).ToArray(); break;
                case MalformedKind.MissingCompletion:
                    floor.Layout.Nodes = floor.Layout.Nodes.Where(node =>
                        node.Kind != FloorRouteNodeKind.Completion).ToArray(); break;
                case MalformedKind.Cycle:
                    FloorRouteNode entrance = floor.Layout.Nodes.First(node =>
                        node.Kind == FloorRouteNodeKind.Entrance);
                    FloorRouteNode room = floor.Layout.Nodes.First(node =>
                        node.Kind == FloorRouteNodeKind.Room);
                    floor.Layout.Edges = new[] { Edge("edge.test.0", entrance.NodeId, room.NodeId),
                        Edge("edge.test.1", room.NodeId, entrance.NodeId) }; break;
                case MalformedKind.RouteGap:
                    floor.Layout.Edges = floor.Layout.Edges.Skip(1).ToArray(); break;
            }
        }

        private static SavedSpatialFloor MinimalFloor() => new SavedSpatialFloor
        {
            FloorInstanceId = "floor.00", FloorDefinitionId = "spatial.floor.01", FloorIndex = 0,
            Layout = new FloorSpatialLayout
            {
                FloorId = "floor.00", Rooms = new[] { Room("room.00") },
                Nodes = new[] { Node("node.entrance", FloorRouteNodeKind.Entrance),
                    Node("node.room", FloorRouteNodeKind.Room, "room.00"),
                    Node("node.completion", FloorRouteNodeKind.Completion) },
                Edges = new[] { Edge("edge.0", "node.entrance", "node.room"),
                    Edge("edge.1", "node.room", "node.completion") }
            },
            RoomContents = new FloorRoomContentState
            { Assignments = Array.Empty<RoomContentAssignment>(),
                RoomSemantics = new[] { Semantics("room.00") }, NextSequence = 0 }
        };
        private static RoomSpatialInstance Room(string id) => new RoomSpatialInstance
        { RoomInstanceId = id, RoomDefinitionId = "spatial.room.basic", FloorId = "floor.00" };
        private static FloorRouteNode Node(string id, FloorRouteNodeKind kind, string room = null) =>
            new FloorRouteNode { NodeId = id, FloorId = "floor.00", Kind = kind,
                RoomInstanceId = room };
        private static FloorRouteEdge Edge(string id, string source, string destination) =>
            new FloorRouteEdge { EdgeId = id, FloorId = "floor.00", SourceNodeId = source,
                DestinationNodeId = destination, Classification = RouteClassification.Required,
                ConnectionKind = FloorRouteConnectionKind.DirectDoorway };
        private static CanonicalRoomSemantics Semantics(string room) => new CanonicalRoomSemantics
        { RoomInstanceId = room, LegacyRoomOriginKind = LegacyRoomOriginKind.CanonicalPlayerPlaced };
        private static string Signature(MvpOrderedRouteRoom[] route) => string.Join("|",
            route.Select(room => room.RoomOptionId + ":" +
                string.Join(",", room.AssignedMonsterOptionIds) + ":" +
                string.Join(",", room.AssignedTrapOptionIds) + ":" +
                string.Join(",", room.AssignedLootNodeOptionIds)));

        public enum MalformedKind
        {
            MarkerOnly, DuplicateFloor, DuplicateEntrance, DuplicateNodeId, DuplicateRoomId,
            DuplicateSemantics, DuplicateEdgeId, MissingCompletion, Cycle, RouteGap
        }
    }
}
