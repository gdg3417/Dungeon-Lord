#if UNITY_EDITOR
using NUnit.Framework;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class NativeStructuralIdentityTests
    {
        [Test]
        public void ConstructionAllocation_ReturnsCompleteDeterministicBundle()
        {
            SavedSpatialFloor floor = Floor("compat.floor.00.legacy-room.00");
            Assert.That(NativeStructuralIdentity.TryAllocateConstructionIdentity(State(floor),
                floor.FloorInstanceId, out NativeRoomConstructionIdentity identity,
                out string reason), Is.True, reason);
            Assert.That(identity.RoomInstanceId, Is.EqualTo("compat.floor.00.room.player.0000"));
            Assert.That(identity.RoomNodeId, Is.EqualTo("compat.floor.00.room.player.0000.node"));
            Assert.That(identity.IncomingRequiredEdgeId,
                Is.EqualTo("compat.floor.00.room.player.0000.edge.incoming"));
            Assert.That(identity.TerminalRequiredEdgeId,
                Is.EqualTo("compat.floor.00.room.player.0000.edge.terminal"));
        }

        [TestCase("node", "node")]
        [TestCase("node", "edge")]
        [TestCase("node", "fixed")]
        [TestCase("node", "assignment")]
        [TestCase("node", "room")]
        [TestCase("incoming", "node")]
        [TestCase("incoming", "edge")]
        [TestCase("incoming", "assignment")]
        [TestCase("terminal", "edge")]
        [TestCase("terminal", "fixed")]
        [TestCase("terminal", "assignment")]
        public void ConstructionAllocation_DerivedBundleCollisionFailsClosed(string member, string owner)
        {
            SavedSpatialFloor floor = Floor("compat.floor.00.legacy-room.00");
            string root = "compat.floor.00.room.player.0000";
            string occupied = member == "node" ? root + ".node" : member == "incoming"
                ? root + ".edge.incoming" : root + ".edge.terminal";
            Occupy(floor, owner, occupied);
            Assert.That(NativeStructuralIdentity.TryAllocateConstructionIdentity(State(floor),
                floor.FloorInstanceId, out NativeRoomConstructionIdentity identity,
                out string reason), Is.False);
            Assert.That(identity, Is.Null);
            Assert.That(reason, Is.EqualTo(NativeStructuralIdentity.InvalidIdentityReason));
        }

        [Test]
        public void ConstructionAllocation_DerivedNodeCollisionOnAnotherFloorFailsClosed()
        {
            SavedSpatialFloor target = Floor("compat.floor.00.legacy-room.00");
            SavedSpatialFloor other = Floor("other.floor.legacy-room.00");
            other.FloorInstanceId = "compat.floor.00.room.player.0000.node";
            other.Layout.FloorId = other.FloorInstanceId;
            var state = State(target, other);
            Assert.That(NativeStructuralIdentity.TryAllocateConstructionIdentity(state,
                target.FloorInstanceId, out NativeRoomConstructionIdentity identity,
                out string reason), Is.False);
            Assert.That(identity, Is.Null);
            Assert.That(reason, Is.EqualTo(NativeStructuralIdentity.InvalidIdentityReason));
        }

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
            var state = State(target, other);
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

        private static void Occupy(SavedSpatialFloor floor, string owner, string identity)
        {
            if (owner == "node") floor.Layout.Nodes = new[] { new FloorRouteNode { NodeId = identity } };
            if (owner == "edge") floor.Layout.Edges = new[] { new FloorRouteEdge { EdgeId = identity } };
            if (owner == "fixed") floor.FixedStructures = new[]
                { new SavedFixedSpatialStructure { FixedStructureInstanceId = identity } };
            if (owner == "assignment") floor.RoomContents.Assignments = new[]
                { new RoomContentAssignment { AssignmentId = identity } };
            if (owner == "room") floor.Layout.Rooms = new[] { floor.Layout.Rooms[0],
                new RoomSpatialInstance { RoomInstanceId = identity } };
        }

        private static DetachedCanonicalSpatialSaveState State(params SavedSpatialFloor[] floors)
        {
            var state = new DetachedCanonicalSpatialSaveState { Floors = floors,
                LifecycleAndOwnership = NativeStructuralIdentity.CreateInitialLifecycle(floors) };
            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(state,
                new CanonicalSpatialSaveWorkloadLimits(256, 256), out state), Is.True);
            return state;
        }
    }
}
#endif
