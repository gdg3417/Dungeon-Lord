using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66CanonicalRuntimeProjectionTests
    {
        [Test]
        public void CanonicalGraphAndContentsAreTheOnlyRouteAuthority()
        {
            SaveData save = CanonicalTwoRoomSave();
            MvpOrderedRouteRoom[] expected = MvpOrderedRoomRouteResolver.Resolve(save, null);

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

            MvpOrderedRouteRoom[] actual = MvpOrderedRoomRouteResolver.Resolve(save, null);
            Assert.That(actual.Select(room => room.RoomOptionId),
                Is.EqualTo(expected.Select(room => room.RoomOptionId)));
            Assert.That(actual.SelectMany(room => room.AssignedMonsterOptionIds),
                Is.EqualTo(new[] { MvpDungeonPlacementIds.SkeletonOptionId }));
            Assert.That(actual.SelectMany(room => room.AssignedTrapOptionIds),
                Is.EqualTo(new[] { MvpDungeonPlacementIds.SpikeTrapOptionId }));
        }

        [Test]
        public void CanonicalRouteFollowsGraphRatherThanRoomArrayOrder()
        {
            SaveData save = CanonicalTwoRoomSave();
            Array.Reverse(save.spatialFloors[0].Layout.Rooms);

            MvpOrderedRouteRoom[] route = MvpOrderedRoomRouteResolver.Resolve(save, null);

            Assert.That(route, Has.Length.EqualTo(2));
            Assert.That(route[0].AssignedMonsterOptionIds,
                Is.EqualTo(new[] { MvpDungeonPlacementIds.SkeletonOptionId }));
            Assert.That(route[1].AssignedTrapOptionIds,
                Is.EqualTo(new[] { MvpDungeonPlacementIds.SpikeTrapOptionId }));
        }

        [Test]
        public void RuntimeNormalizationDoesNotReviveLegacyModelsForCanonicalAuthority()
        {
            SaveData save = CanonicalTwoRoomSave();
            save.mvpDungeonPlacements = null;
            save.mvpDungeonFloorLayout = null;
            save.mvpRoomSlotAssignments = null;

            SaveMigration.MigrateToLatest(new SaveRoot { schemaVersion = 7, primary = save });

            Assert.That(save.mvpDungeonPlacements, Is.Null);
            Assert.That(save.mvpDungeonFloorLayout, Is.Null);
            Assert.That(save.mvpRoomSlotAssignments, Is.Null);
            Assert.That(save.dungeonLayout, Is.Not.Null);
            Assert.That(save.structureRuntime, Is.Not.Null);
        }

        [Test]
        public void LegacyRoomWritersFailClosedWithoutMutatingCanonicalOrLegacyEvidence()
        {
            SaveData save = CanonicalTwoRoomSave();
            string before = JsonUtility.ToJson(save);

            Assert.That(MvpRoomSlotLayoutResolver.TryAssignToPersistedRoom(save, null, 0,
                MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId), Is.False);
            Assert.That(MvpRoomSlotLayoutResolver.TryAddSecondBasicRoomSlot(save, null), Is.False);
            MvpRoomSlotLayoutResolver.SetPersistedRoomOptionIfPresent(save, null, 0,
                MvpDungeonPlacementIds.BasicRoomOptionId);

            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
        }

        private static SaveData CanonicalTwoRoomSave()
        {
            const string floor = "compat.floor.00";
            const string room0 = "compat.floor.00.legacy-room.00";
            const string room1 = "compat.floor.00.legacy-room.01";
            return new SaveData
            {
                canonicalSpatialAuthority = new CanonicalSpatialAuthorityMarker
                {
                    CanonicalLayoutContractVersion = 1,
                    CreationKind = CanonicalSpatialCreationKind.NativeCanonical
                },
                spatialFloors = new[]
                {
                    new SavedSpatialFloor
                    {
                        FloorInstanceId = floor,
                        FloorDefinitionId = "spatial.floor.01",
                        FloorIndex = 0,
                        Layout = new FloorSpatialLayout
                        {
                            FloorId = floor,
                            Rooms = new[]
                            {
                                Room(room0), Room(room1)
                            },
                            Nodes = new[]
                            {
                                Node("node.entrance", FloorRouteNodeKind.Entrance, null),
                                Node("node.room0", FloorRouteNodeKind.Room, room0),
                                Node("node.room1", FloorRouteNodeKind.Room, room1),
                                Node("node.completion", FloorRouteNodeKind.Completion, null)
                            },
                            Edges = new[]
                            {
                                Edge("edge.0", "node.entrance", "node.room0"),
                                Edge("edge.1", "node.room0", "node.room1"),
                                Edge("edge.2", "node.room1", "node.completion")
                            }
                        },
                        FixedStructures = Array.Empty<SavedFixedSpatialStructure>(),
                        RoomContents = new FloorRoomContentState
                        {
                            Assignments = new[]
                            {
                                Assignment("assignment.monster", room0,
                                    CanonicalSpatialSaveContracts.MonsterCategoryId,
                                    MvpDungeonPlacementIds.SkeletonOptionId, 1),
                                Assignment("assignment.trap", room1,
                                    CanonicalSpatialSaveContracts.TrapCategoryId,
                                    MvpDungeonPlacementIds.SpikeTrapOptionId, 2)
                            },
                            RoomSemantics = new[]
                            {
                                Semantics(room0), Semantics(room1)
                            },
                            NextSequence = 3
                        }
                    }
                }
            };
        }

        private static RoomSpatialInstance Room(string id) => new RoomSpatialInstance
        {
            RoomInstanceId = id,
            RoomDefinitionId = "spatial.room.basic",
            FloorId = "compat.floor.00"
        };

        private static FloorRouteNode Node(string id, FloorRouteNodeKind kind, string room) =>
            new FloorRouteNode { NodeId = id, FloorId = "compat.floor.00", Kind = kind,
                RoomInstanceId = room };

        private static FloorRouteEdge Edge(string id, string source, string destination) =>
            new FloorRouteEdge { EdgeId = id, FloorId = "compat.floor.00", SourceNodeId = source,
                DestinationNodeId = destination, Classification = RouteClassification.Required,
                ConnectionKind = FloorRouteConnectionKind.DirectDoorway };

        private static RoomContentAssignment Assignment(string id, string room, string category,
            string option, long sequence) => new RoomContentAssignment { AssignmentId = id,
                RoomInstanceId = room, CategoryId = category, OptionId = option, Sequence = sequence };

        private static CanonicalRoomSemantics Semantics(string room) => new CanonicalRoomSemantics
        {
            RoomInstanceId = room,
            LegacyRoomOriginKind = LegacyRoomOriginKind.CanonicalPlayerPlaced
        };
    }
}
