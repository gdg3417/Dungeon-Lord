#if UNITY_EDITOR
using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class FloorLayoutValidatorTests
    {
        public const string FloorId = "floor.instance.test";
        private const string RoomDefinitionId = "room.definition.test";
        private const string CorridorDefinitionId = "corridor.definition.test";
        private const int FinalCapacity = 7;

        public static FloorSpatialConfiguration Configuration(int capacity = FinalCapacity, int branches = 0) => new FloorSpatialConfiguration { FloorDefinitionId = "floor.definition.test", FloorIndex = 0, FinalFloorSpaceCapacity = capacity, OptionalBranchAllowance = branches };
        public static RoomSpatialDefinition[] Definitions(int cost = 2, int connections = 2) => new[] { new RoomSpatialDefinition { RoomDefinitionId = RoomDefinitionId, GrossFootprint = new RectangularFootprintDefinition(1, 1), FloorSpaceCost = cost, MaximumConnectionCount = connections, MonsterCapacity = 1, TrapCapacity = 2, LootCapacity = 3 } };
        public static CorridorSpatialDefinition[] CorridorDefinitions(int cost = 1) => new[] { new CorridorSpatialDefinition { CorridorDefinitionId = CorridorDefinitionId, FloorSpaceCost = cost } };

        public static FloorSpatialLayout ValidLayout(FloorRouteNodeKind terminal = FloorRouteNodeKind.Completion)
        {
            return new FloorSpatialLayout {
                FloorId = FloorId,
                Rooms = new[] { Room("room.0", 2), Room("room.1", 6) },
                Nodes = new[] { Node("entrance", FloorRouteNodeKind.Entrance), Node("node.room.0", FloorRouteNodeKind.Room, "room.0"), Node("node.room.1", FloorRouteNodeKind.Room, "room.1"), Node("terminal", terminal) },
                Edges = new[] { Edge("edge.0", "entrance", "node.room.0", 0), Edge("edge.1", "node.room.0", "node.room.1", 4), Edge("edge.2", "node.room.1", "terminal", 8) }
            };
        }

        [TestCase(FloorRouteNodeKind.Completion)] [TestCase(FloorRouteNodeKind.Exit)] [TestCase(FloorRouteNodeKind.Descent)]
        public void ValidLayout_ExactCapacity_AndTerminalKindsPass(FloorRouteNodeKind terminal)
        {
            var result = FloorLayoutValidator.Validate(ValidLayout(terminal), Configuration(), Definitions(), CorridorDefinitions());
            Assert.That(result.IsValid, Is.True); Assert.That(result.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(FinalCapacity)); Assert.That(result.Capacity.RemainingFloorSpaceCapacity, Is.Zero);
        }

        [Test]
        public void CapacityExceededByOne_AndNegativeConfiguration_AreReported()
        {
            AssertReason(ValidLayout(), Configuration(FinalCapacity - 1), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.CapacityExceeded);
            AssertReason(ValidLayout(), Configuration(-1), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.NegativeConfigurationValue);
            AssertReason(ValidLayout(), Configuration(), Definitions(-1), CorridorDefinitions(), FloorLayoutValidationReason.NegativeConfigurationValue);
        }

        [Test]
        public void ReservedOutsideAndDuplicate_AreBothReported()
        {
            var definitions = Definitions(); definitions[0].ReservedTileOffsets = new[] { new TileCoordinate(2, 2), new TileCoordinate(2, 2) };
            var result = FloorLayoutValidator.Validate(ValidLayout(), Configuration(), definitions, CorridorDefinitions());
            Assert.That(result.Issues.Any(x => x.Reason == FloorLayoutValidationReason.ReservedTileOutsideFootprint), Is.True);
            Assert.That(result.Issues.Any(x => x.Reason == FloorLayoutValidationReason.DuplicateReservedTile), Is.True);
        }

        [TestCase("room-room")] [TestCase("room-corridor")] [TestCase("corridor-corridor")]
        public void OverlapKinds_AreReported_WhileAdjacencyPasses(string kind)
        {
            FloorSpatialLayout layout = ValidLayout();
            if (kind == "room-room") layout.Rooms[1].Anchor = layout.Rooms[0].Anchor;
            if (kind == "room-corridor") layout.Edges[0].Footprint = new ResolvedTileFootprint(new[] { layout.Rooms[0].Anchor });
            if (kind == "corridor-corridor") layout.Edges[1].Footprint = layout.Edges[0].Footprint;
            AssertReason(layout, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.FootprintOverlap);
            Assert.That(FloorLayoutValidator.Validate(ValidLayout(), Configuration(), Definitions(), CorridorDefinitions()).Issues.Any(x => x.Reason == FloorLayoutValidationReason.FootprintOverlap), Is.False);
        }

        [TestCase(FloorLayoutValidationReason.MissingSourceNode)] [TestCase(FloorLayoutValidationReason.MissingDestinationNode)] [TestCase(FloorLayoutValidationReason.SelfEdge)] [TestCase(FloorLayoutValidationReason.CrossFloorEdge)]
        public void EndpointFailures_AreReported(FloorLayoutValidationReason reason)
        {
            var layout = ValidLayout();
            if (reason == FloorLayoutValidationReason.MissingSourceNode) layout.Edges[0].SourceNodeId = "missing";
            if (reason == FloorLayoutValidationReason.MissingDestinationNode) layout.Edges[0].DestinationNodeId = "missing";
            if (reason == FloorLayoutValidationReason.SelfEdge) layout.Edges[0].DestinationNodeId = layout.Edges[0].SourceNodeId;
            if (reason == FloorLayoutValidationReason.CrossFloorEdge) layout.Nodes[0].FloorId = "other.floor";
            AssertReason(layout, Configuration(), Definitions(), CorridorDefinitions(), reason);
        }

        [Test]
        public void EntranceReachabilityConnectionAndRequiredRouteBoundaries_AreReported()
        {
            var missingEntrance = ValidLayout(); missingEntrance.Nodes = missingEntrance.Nodes.Where(x => x.Kind != FloorRouteNodeKind.Entrance).ToArray(); AssertReason(missingEntrance, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.MissingEntrance);
            var multiple = ValidLayout(); multiple.Nodes = multiple.Nodes.Concat(new[] { Node("entrance.2", FloorRouteNodeKind.Entrance) }).ToArray(); AssertReason(multiple, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.MultipleEntrances);
            var disconnected = ValidLayout(); disconnected.Edges = disconnected.Edges.Where(x => x.EdgeId != "edge.1").ToArray(); AssertReason(disconnected, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.UnreachableRoom);
            Assert.That(FloorLayoutValidator.Validate(ValidLayout(), Configuration(), Definitions(2), CorridorDefinitions()).Issues.Any(x => x.Reason == FloorLayoutValidationReason.ConnectionLimitExceeded), Is.False);
            AssertReason(ValidLayout(), Configuration(), Definitions(1), CorridorDefinitions(), FloorLayoutValidationReason.ConnectionLimitExceeded);
            var deadEnd = ValidLayout(); deadEnd.Edges = deadEnd.Edges.Where(x => x.EdgeId != "edge.2").ToArray(); AssertReason(deadEnd, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.RequiredRouteWithoutTerminal);
            var cycle = ValidLayout(); cycle.Edges[2].DestinationNodeId = "node.room.0"; AssertReason(cycle, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.RequiredRouteWithoutTerminal);
        }

        [Test]
        public void OptionalBranches_CountDistinctIds_AndEnforceClassificationRules()
        {
            var zero = ValidLayout(); Assert.That(FloorLayoutValidator.Validate(zero, Configuration(branches: 0), Definitions(), CorridorDefinitions()).IsValid, Is.True);
            var one = ValidLayout(); one.Edges[1].Classification = RouteClassification.Optional; one.Edges[1].OptionalBranchId = "branch.a"; one.Edges[2].Classification = RouteClassification.Optional; one.Edges[2].OptionalBranchId = "branch.a"; Assert.That(FloorLayoutValidator.Validate(one, Configuration(branches: 1), Definitions(), CorridorDefinitions()).Issues.Any(x => x.Reason == FloorLayoutValidationReason.OptionalBranchAllowanceExceeded), Is.False);
            var two = ValidLayout(); two.Edges[1].Classification = RouteClassification.Optional; two.Edges[1].OptionalBranchId = "branch.a"; two.Edges[2].Classification = RouteClassification.Optional; two.Edges[2].OptionalBranchId = "branch.b"; AssertReason(two, Configuration(branches: 1), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.OptionalBranchAllowanceExceeded);
            var missing = ValidLayout(); missing.Edges[1].Classification = RouteClassification.Optional; AssertReason(missing, Configuration(branches: 1), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.OptionalEdgeMissingBranchId);
            var required = ValidLayout(); required.Edges[0].OptionalBranchId = "branch.a"; AssertReason(required, Configuration(branches: 1), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.RequiredEdgeHasBranchId);
        }

        [Test]
        public void DuplicateAndMissingDefinitions_AndMultipleFailures_AreReturned()
        {
            var duplicate = ValidLayout(); duplicate.Rooms = duplicate.Rooms.Concat(new[] { Room("room.0", 10) }).ToArray(); duplicate.Nodes = duplicate.Nodes.Concat(new[] { Node("entrance", FloorRouteNodeKind.Entrance) }).ToArray(); duplicate.Edges = duplicate.Edges.Concat(new[] { Edge("edge.0", "missing", "missing", 12) }).ToArray();
            var result = FloorLayoutValidator.Validate(duplicate, null, Array.Empty<RoomSpatialDefinition>(), Array.Empty<CorridorSpatialDefinition>());
            Assert.That(result.Issues.Select(x => x.Reason), Does.Contain(FloorLayoutValidationReason.DuplicateRoomId)); Assert.That(result.Issues.Select(x => x.Reason), Does.Contain(FloorLayoutValidationReason.DuplicateNodeId)); Assert.That(result.Issues.Select(x => x.Reason), Does.Contain(FloorLayoutValidationReason.DuplicateEdgeId)); Assert.That(result.Issues.Select(x => x.Reason), Does.Contain(FloorLayoutValidationReason.MissingFloorConfiguration)); Assert.That(result.Issues.Select(x => x.Reason), Does.Contain(FloorLayoutValidationReason.MissingRoomDefinition)); Assert.That(result.Issues.Select(x => x.Reason), Does.Contain(FloorLayoutValidationReason.MissingCorridorDefinition));
        }

        [Test]
        public void ShuffledInputs_ProduceIdenticalIssues_AndValidationDoesNotMutateInput()
        {
            var first = ValidLayout(); first.Edges[0].SourceNodeId = "missing"; var second = new FloorSpatialLayout { FloorId = first.FloorId, Rooms = first.Rooms.Reverse().ToArray(), Nodes = first.Nodes.Reverse().ToArray(), Edges = first.Edges.Reverse().ToArray() };
            string[] originalRooms = first.Rooms.Select(x => x.RoomInstanceId).ToArray();
            var a = FloorLayoutValidator.Validate(first, Configuration(), Definitions(), CorridorDefinitions()); var b = FloorLayoutValidator.Validate(second, Configuration(), Definitions(), CorridorDefinitions());
            CollectionAssert.AreEqual(a.Issues.Select(Key), b.Issues.Select(Key)); CollectionAssert.AreEqual(originalRooms, first.Rooms.Select(x => x.RoomInstanceId));
            CollectionAssert.AreEqual(a.Issues, a.Issues.OrderBy(x => (int)x.Reason).ThenBy(x => x.SubjectId, StringComparer.Ordinal).ThenBy(x => x.RelatedId, StringComparer.Ordinal).ThenBy(x => x.HasCoordinate ? 1 : 0).ThenBy(x => x.Coordinate));
        }

        private static RoomSpatialInstance Room(string id, int x) => new RoomSpatialInstance { RoomInstanceId = id, RoomDefinitionId = RoomDefinitionId, FloorId = FloorId, Anchor = new TileCoordinate(x, 0), Orientation = CardinalOrientation.Zero };
        private static FloorRouteNode Node(string id, FloorRouteNodeKind kind, string room = null) => new FloorRouteNode { NodeId = id, FloorId = FloorId, Kind = kind, RoomInstanceId = room };
        private static CorridorEdge Edge(string id, string source, string destination, int x) => new CorridorEdge { EdgeId = id, CorridorDefinitionId = CorridorDefinitionId, FloorId = FloorId, SourceNodeId = source, DestinationNodeId = destination, Footprint = new ResolvedTileFootprint(new[] { new TileCoordinate(x, 2) }), Classification = RouteClassification.Required };
        private static void AssertReason(FloorSpatialLayout layout, FloorSpatialConfiguration config, RoomSpatialDefinition[] rooms, CorridorSpatialDefinition[] corridors, FloorLayoutValidationReason reason) => Assert.That(FloorLayoutValidator.Validate(layout, config, rooms, corridors).Issues.Any(x => x.Reason == reason), Is.True);
        private static string Key(FloorLayoutValidationIssue issue) => $"{(int)issue.Reason}|{issue.SubjectId}|{issue.RelatedId}|{issue.HasCoordinate}|{issue.Coordinate.X}|{issue.Coordinate.Y}";
    }
}
#endif
