using System;
using NUnit.Framework;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class SchemaEightLifecycleOwnershipTests
    {
        [Test]
        public void NativeIdentityUsesPersistedHighWaterInsteadOfExistingMaximum()
        {
            SavedSpatialFloor floor = Floor("floor.alpha", "floor.alpha.room.player.0002");
            var state = new DetachedCanonicalSpatialSaveState
            {
                Floors = new[] { floor },
                LifecycleAndOwnership = new StructuralLifecycleAndOwnershipState
                {
                    Floors = new[] { new FloorStructuralIdentityLifecycle
                    {
                        FloorInstanceId = "floor.alpha", NextNativeRoomOrdinal = 9,
                        NextNativeEdgeOrdinal = 12
                    }}
                }
            };

            Assert.That(NativeStructuralIdentity.TryAllocateConstructionIdentity(state,
                "floor.alpha", out NativeRoomConstructionIdentity value, out string reason), Is.True, reason);
            Assert.That(value.RoomInstanceId, Is.EqualTo("floor.alpha.room.player.0009"));
        }

        [Test]
        public void ExactNativePatternIgnoresLegacyAndMalformedLookalikesWhenDerivingMigrationState()
        {
            SavedSpatialFloor floor = Floor("floor.alpha", "compat.room.9000",
                "floor.alpha.room.player.9", "floor.alpha.room.player.0004");
            var source = new DetachedCanonicalSpatialSaveState { Floors = new[] { floor } };
            var limits = new CanonicalSpatialSaveWorkloadLimits(128, 128);

            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(source, limits,
                out DetachedCanonicalSpatialSaveState canonical), Is.True);
            Assert.That(canonical.LifecycleAndOwnership.Floors[0].NextNativeRoomOrdinal,
                Is.EqualTo(5));
            Assert.That(canonical.LifecycleAndOwnership.ReturnedContents, Is.Empty);
        }

        [Test]
        public void ReturnedIdentityCannotAlsoRemainAssigned()
        {
            SavedSpatialFloor floor = Floor("floor.alpha", "room.alpha");
            floor.RoomContents.Assignments = new[] { new RoomContentAssignment
            {
                AssignmentId = "content.alpha", RoomInstanceId = "room.alpha",
                CategoryId = MvpDungeonPlacementIds.MonsterCategoryId,
                OptionId = MvpDungeonPlacementIds.SkeletonOptionId, Sequence = 0
            }};
            var state = new DetachedCanonicalSpatialSaveState
            {
                Floors = new[] { floor },
                LifecycleAndOwnership = new StructuralLifecycleAndOwnershipState
                {
                    Floors = new[] { new FloorStructuralIdentityLifecycle { FloorInstanceId = "floor.alpha" }},
                    ReturnedContents = new[] { new ReturnedStructuralContent
                    {
                        AssignmentId = "content.alpha", CategoryId = MvpDungeonPlacementIds.MonsterCategoryId,
                        OptionId = MvpDungeonPlacementIds.SkeletonOptionId, Sequence = 0,
                        RemovalDisposition = StructuralContentRemovalDisposition.ReturnToPlayerCustody
                    }}
                }
            };

            CanonicalSpatialSaveValidationResult result = CanonicalSpatialSaveContracts.Validate(state,
                new CanonicalSpatialSaveWorkloadLimits(128, 128));
            Assert.That(result.Issues, Does.Contain(
                CanonicalSpatialSaveValidationIssue.AssignedAndReturnedIdentity));
        }

        private static SavedSpatialFloor Floor(string floorId, params string[] roomIds)
        {
            var rooms = new RoomSpatialInstance[roomIds.Length];
            var nodes = new FloorRouteNode[roomIds.Length];
            var semantics = new CanonicalRoomSemantics[roomIds.Length];
            for (int i = 0; i < roomIds.Length; i++)
            {
                rooms[i] = new RoomSpatialInstance { RoomInstanceId = roomIds[i], RoomDefinitionId = "room.basic",
                    FloorId = floorId, Orientation = CardinalOrientation.North };
                nodes[i] = new FloorRouteNode { NodeId = roomIds[i] + ".node", FloorId = floorId,
                    Kind = FloorRouteNodeKind.Room, RoomInstanceId = roomIds[i] };
                semantics[i] = new CanonicalRoomSemantics { RoomInstanceId = roomIds[i],
                    LegacyRoomOriginKind = LegacyRoomOriginKind.CanonicalPlayerPlaced };
            }
            return new SavedSpatialFloor
            {
                FloorInstanceId = floorId, FloorDefinitionId = "floor.definition", FloorIndex = 0,
                Layout = new FloorSpatialLayout { FloorId = floorId, Rooms = rooms, Nodes = nodes,
                    Edges = Array.Empty<FloorRouteEdge>() },
                FixedStructures = Array.Empty<SavedFixedSpatialStructure>(),
                RoomContents = new FloorRoomContentState { Assignments = Array.Empty<RoomContentAssignment>(),
                    RoomSemantics = semantics, NextSequence = 0 }
            };
        }
    }
}
