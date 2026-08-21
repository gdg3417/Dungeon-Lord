#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class NativeStructuralIdentityTests
    {
        [Test]
        public void Allocation_DependsOnlyOnPersistedStructuralRoomIdentities()
        {
            SavedSpatialFloor floor = Floor("compat.floor.00.legacy-room.00");
            Assert.That(NativeStructuralIdentity.TryAllocateRoomId(State(floor), floor.FloorInstanceId, out string initial,
                out string reason), Is.True, reason);
            Assert.That(initial, Is.EqualTo("compat.floor.00.room.player.0000"));

            floor.RoomContents = new FloorRoomContentState
            {
                NextSequence = 9876,
                Assignments = new[]
                {
                    Assignment("monster", "placement.category.monster"),
                    Assignment("trap", "placement.category.trap"),
                    Assignment("loot", "placement.category.loot_node")
                }
            };
            Assert.That(NativeStructuralIdentity.TryAllocateRoomId(State(floor), floor.FloorInstanceId, out string afterContent,
                out reason), Is.True, reason);
            Assert.That(afterContent, Is.EqualTo(initial));
            Assert.That(floor.Layout.Rooms[0].RoomInstanceId,
                Is.EqualTo("compat.floor.00.legacy-room.00"));
        }

        [Test]
        public void Allocation_AdvancesPastExistingNativeOrdinalWithoutUsingArrayPosition()
        {
            SavedSpatialFloor floor = Floor("compat.floor.00.room.player.0007",
                "compat.floor.00.legacy-room.00", "compat.floor.00.room.player.0002");
            Assert.That(NativeStructuralIdentity.TryAllocateRoomId(State(floor), floor.FloorInstanceId, out string allocated,
                out string reason), Is.True, reason);
            Assert.That(allocated, Is.EqualTo("compat.floor.00.room.player.0008"));

            floor.Layout.Rooms = new[] { floor.Layout.Rooms[2], floor.Layout.Rooms[0], floor.Layout.Rooms[1] };
            Assert.That(NativeStructuralIdentity.TryAllocateRoomId(State(floor), floor.FloorInstanceId, out string reordered,
                out reason), Is.True, reason);
            Assert.That(reordered, Is.EqualTo(allocated));
        }

        [TestCase("compat.floor.00.room.player.bad")]
        [TestCase("compat.floor.00.room.player.00000")]
        [TestCase("compat.floor.00.room.player.-001")]
        public void Allocation_MalformedNativeIdentityFailsClosed(string malformed)
        {
            SavedSpatialFloor floor = Floor(malformed);
            Assert.That(NativeStructuralIdentity.TryAllocateRoomId(State(floor), floor.FloorInstanceId, out string allocated,
                out string reason), Is.False);
            Assert.That(allocated, Is.Null);
            Assert.That(reason, Is.EqualTo(NativeStructuralIdentity.InvalidIdentityReason));
        }

        [Test]
        public void Allocation_DuplicateExistingRoomIdentityFailsClosed()
        {
            SavedSpatialFloor floor = Floor("compat.floor.00.legacy-room.00",
                "compat.floor.00.legacy-room.00");
            Assert.That(NativeStructuralIdentity.TryAllocateRoomId(State(floor), floor.FloorInstanceId,
                out string allocated, out string reason), Is.False);
            Assert.That(allocated, Is.Null);
            Assert.That(reason, Is.EqualTo(NativeStructuralIdentity.InvalidIdentityReason));
        }

        [TestCase("node")]
        [TestCase("edge")]
        [TestCase("fixed")]
        [TestCase("assignment")]
        public void Allocation_ProposedIdentityOccupiedByAnotherCanonicalKindFailsClosed(string kind)
        {
            SavedSpatialFloor floor = Floor("compat.floor.00.legacy-room.00");
            string collision = "compat.floor.00.room.player.0000";
            if (kind == "node") floor.Layout.Nodes = new[] { new FloorRouteNode { NodeId = collision } };
            if (kind == "edge") floor.Layout.Edges = new[] { new FloorRouteEdge { EdgeId = collision } };
            if (kind == "fixed") floor.FixedStructures = new[]
                { new SavedFixedSpatialStructure { FixedStructureInstanceId = collision } };
            if (kind == "assignment") floor.RoomContents.Assignments = new[]
                { new RoomContentAssignment { AssignmentId = collision } };
            Assert.That(NativeStructuralIdentity.TryAllocateRoomId(State(floor), floor.FloorInstanceId,
                out string allocated, out string reason), Is.False);
            Assert.That(allocated, Is.Null);
            Assert.That(reason, Is.EqualTo(NativeStructuralIdentity.InvalidIdentityReason));
        }

        [Test]
        public void Allocation_CollisionOnAnotherFloorFailsClosed()
        {
            SavedSpatialFloor target = Floor("compat.floor.00.legacy-room.00");
            SavedSpatialFloor other = Floor("other.floor.legacy-room.00");
            other.FloorInstanceId = "other.floor"; other.Layout.FloorId = "other.floor";
            other.Layout.Nodes = new[] { new FloorRouteNode
                { NodeId = "compat.floor.00.room.player.0000" } };
            var state = new DetachedCanonicalSpatialSaveState { Floors = new[] { target, other } };
            Assert.That(NativeStructuralIdentity.TryAllocateRoomId(state, target.FloorInstanceId,
                out string allocated, out string reason), Is.False);
            Assert.That(allocated, Is.Null);
            Assert.That(reason, Is.EqualTo(NativeStructuralIdentity.InvalidIdentityReason));
        }

        private static SavedSpatialFloor Floor(params string[] roomIds)
        {
            var rooms = new RoomSpatialInstance[roomIds.Length];
            for (int index = 0; index < roomIds.Length; index++)
                rooms[index] = new RoomSpatialInstance { RoomInstanceId = roomIds[index],
                    FloorId = "compat.floor.00", RoomDefinitionId = "spatial.room.basic" };
            return new SavedSpatialFloor { FloorInstanceId = "compat.floor.00",
                Layout = new FloorSpatialLayout { FloorId = "compat.floor.00", Rooms = rooms },
                RoomContents = new FloorRoomContentState() };
        }

        private static RoomContentAssignment Assignment(string id, string category) =>
            new RoomContentAssignment { AssignmentId = id, CategoryId = category };

        private static DetachedCanonicalSpatialSaveState State(SavedSpatialFloor floor) =>
            new DetachedCanonicalSpatialSaveState { Floors = new[] { floor } };
    }
}
#endif
