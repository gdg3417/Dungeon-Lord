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

        [TestCase(true)] [TestCase(false)]
        public void BlankAndMissingEndpoints_ReportCompleteCanonicalDiagnostics(bool source)
        {
            var blank = ValidLayout(); var missing = ValidLayout();
            if (source) { blank.Edges[0].SourceNodeId = " "; missing.Edges[0].SourceNodeId = "node.missing"; }
            else { blank.Edges[0].DestinationNodeId = " "; missing.Edges[0].DestinationNodeId = "node.missing"; }
            FloorLayoutValidationReason reason = source ? FloorLayoutValidationReason.MissingSourceNode : FloorLayoutValidationReason.MissingDestinationNode;
            string endpointKey = $"{(int)reason}|edge.0| |False|0|0";
            string[] blankKeys = EndpointIssueKeys(blank, reason);
            CollectionAssert.AreEqual(new[] { "1|edge.0||False|0|0", endpointKey }, blankKeys);
            blank.Edges = blank.Edges.Reverse().ToArray();
            CollectionAssert.AreEqual(blankKeys, EndpointIssueKeys(blank, reason));

            string[] missingKeys = EndpointIssueKeys(missing, reason);
            CollectionAssert.AreEqual(new[] { $"{(int)reason}|edge.0|node.missing|False|0|0" }, missingKeys);
            Assert.That(FloorLayoutValidator.Validate(missing, Configuration(), Definitions(), CorridorDefinitions()).Issues.Any(x =>
                x.Reason == FloorLayoutValidationReason.MissingStableId && x.SubjectId == "edge.0"), Is.False);
            missing.Edges = missing.Edges.Reverse().ToArray();
            CollectionAssert.AreEqual(missingKeys, EndpointIssueKeys(missing, reason));
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

        [Test]
        public void MissingAndDuplicateDefinitionIdentities_AreDeterministic()
        {
            var config = Configuration(); config.FloorDefinitionId = " "; AssertReason(ValidLayout(), config, Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.MissingStableId);
            var missingRoomId = Definitions(); missingRoomId[0].RoomDefinitionId = null; AssertReason(ValidLayout(), Configuration(), missingRoomId, CorridorDefinitions(), FloorLayoutValidationReason.MissingStableId);
            var missingCorridorId = CorridorDefinitions(); missingCorridorId[0].CorridorDefinitionId = null; AssertReason(ValidLayout(), Configuration(), Definitions(), missingCorridorId, FloorLayoutValidationReason.MissingStableId);
            var duplicateRooms = Definitions().Concat(Definitions()).ToArray(); AssertReason(ValidLayout(), Configuration(), duplicateRooms, CorridorDefinitions(), FloorLayoutValidationReason.DuplicateRoomDefinitionId);
            var duplicateCorridors = CorridorDefinitions().Concat(CorridorDefinitions()).ToArray(); AssertReason(ValidLayout(), Configuration(), Definitions(), duplicateCorridors, FloorLayoutValidationReason.DuplicateCorridorDefinitionId);
        }

        [Test]
        public void DuplicateRoomDefinitions_AreAmbiguousAndDeterministic()
        {
            RoomSpatialDefinition first = Definitions(cost: 2, connections: 1)[0];
            RoomSpatialDefinition second = Definitions(cost: 9, connections: 7)[0];
            second.GrossFootprint = new RectangularFootprintDefinition(3, 2);
            FloorSpatialLayout layout = ValidLayout();
            FloorLayoutValidationResult forward = FloorLayoutValidator.Validate(layout, Configuration(30), new[] { first, second }, CorridorDefinitions());
            FloorLayoutValidationResult reverse = FloorLayoutValidator.Validate(layout, Configuration(30), new[] { second, first }, CorridorDefinitions());
            CollectionAssert.AreEqual(forward.Issues.Select(Key), reverse.Issues.Select(Key));
            Assert.That(forward.Issues.Any(x => x.Reason == FloorLayoutValidationReason.DuplicateRoomDefinitionId && x.SubjectId == RoomDefinitionId), Is.True);
            Assert.That(forward.Issues.Count(x => x.Reason == FloorLayoutValidationReason.MissingRoomDefinition), Is.EqualTo(layout.Rooms.Length));
            Assert.That(forward.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(3));
            Assert.That(reverse.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(forward.Capacity.UsedFloorSpaceCapacity));
        }

        [Test]
        public void DuplicateCorridorDefinitions_AreAmbiguousAndDeterministic()
        {
            CorridorSpatialDefinition first = CorridorDefinitions(cost: 1)[0];
            CorridorSpatialDefinition second = CorridorDefinitions(cost: 8)[0];
            FloorSpatialLayout layout = ValidLayout();
            FloorLayoutValidationResult forward = FloorLayoutValidator.Validate(layout, Configuration(30), Definitions(), new[] { first, second });
            FloorLayoutValidationResult reverse = FloorLayoutValidator.Validate(layout, Configuration(30), Definitions(), new[] { second, first });
            CollectionAssert.AreEqual(forward.Issues.Select(Key), reverse.Issues.Select(Key));
            Assert.That(forward.Issues.Any(x => x.Reason == FloorLayoutValidationReason.DuplicateCorridorDefinitionId && x.SubjectId == CorridorDefinitionId), Is.True);
            Assert.That(forward.Issues.Count(x => x.Reason == FloorLayoutValidationReason.MissingCorridorDefinition), Is.EqualTo(layout.Edges.Length));
            Assert.That(forward.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(4));
            Assert.That(reverse.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(forward.Capacity.UsedFloorSpaceCapacity));
        }

        [Test]
        public void RoomNodeBijectionAndFloorMembership_AreValidated()
        {
            Assert.That(FloorLayoutValidator.Validate(ValidLayout(), Configuration(), Definitions(), CorridorDefinitions()).IsValid, Is.True);
            var noNode = ValidLayout(); noNode.Nodes = noNode.Nodes.Where(x => x.RoomInstanceId != "room.1").ToArray(); AssertReason(noNode, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.RoomMissingNode);
            var twoNodes = ValidLayout(); twoNodes.Nodes = twoNodes.Nodes.Concat(new[] { Node("node.room.duplicate", FloorRouteNodeKind.Room, "room.0") }).ToArray(); AssertReason(twoNodes, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.MultipleNodesForRoom);
            var missingRoom = ValidLayout(); missingRoom.Nodes[1].RoomInstanceId = "room.missing"; AssertReason(missingRoom, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.RoomNodeMissingRoom);
            var roomFloor = ValidLayout(); roomFloor.Rooms[0].FloorId = "floor.other"; AssertReason(roomFloor, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.RoomFloorMismatch);
            AssertReason(roomFloor, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.RoomNodeFloorMismatch);
            var nodeFloor = ValidLayout(); nodeFloor.Nodes.Single(x => x.NodeId == "terminal").FloorId = "floor.other"; AssertReason(nodeFloor, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.NodeFloorMismatch);
            var edgeFloor = ValidLayout(); edgeFloor.Edges[0].FloorId = "floor.other"; AssertReason(edgeFloor, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.EdgeFloorMismatch);
        }

        [Test]
        public void UndefinedEnumsAndInvalidRoomGeometry_AreRejected()
        {
            var orientation = ValidLayout(); orientation.Rooms[0].Orientation = (CardinalOrientation)99; AssertReason(orientation, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.InvalidOrientation);
            var nodeKind = ValidLayout(); nodeKind.Nodes[0].Kind = (FloorRouteNodeKind)99; AssertReason(nodeKind, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.InvalidNodeKind);
            var classification = ValidLayout(); classification.Edges[0].Classification = (RouteClassification)99; AssertReason(classification, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.InvalidRouteClassification);
            var footprint = Definitions(); footprint[0].GrossFootprint = null; AssertReason(ValidLayout(), Configuration(), footprint, CorridorDefinitions(), FloorLayoutValidationReason.InvalidRoomFootprint);
        }

        [TestCase("null")] [TestCase("empty")] [TestCase("single")] [TestCase("duplicate")] [TestCase("diagonal")] [TestCase("bent")] [TestCase("gapped")]
        public void InvalidCorridorFootprints_AreRejected(string kind)
        {
            var layout = ValidLayout();
            TileCoordinate[] tiles = kind == "empty" ? Array.Empty<TileCoordinate>() : kind == "single" ? new[] { new TileCoordinate(20, 20) } :
                kind == "duplicate" ? new[] { new TileCoordinate(20, 20), new TileCoordinate(20, 20) } : kind == "diagonal" ? new[] { new TileCoordinate(20, 20), new TileCoordinate(21, 21) } :
                kind == "bent" ? new[] { new TileCoordinate(20, 20), new TileCoordinate(21, 20), new TileCoordinate(21, 21) } : new[] { new TileCoordinate(20, 20), new TileCoordinate(22, 20) };
            layout.Edges[0].Footprint = kind == "null" ? null : new ResolvedTileFootprint(tiles);
            AssertReason(layout, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.InvalidCorridorFootprint);
        }

        [Test]
        public void DirectUnsortedRawHorizontalAndVerticalCorridorFootprints_AreAcceptedWithoutInputMutation_AndCanonicalizedOnlyByCanonicalized()
        {
            var horizontalTiles = new[] { new TileCoordinate(21, 20), new TileCoordinate(20, 20) };
            var verticalTiles = new[] { new TileCoordinate(30, 31), new TileCoordinate(30, 30) };
            var horizontalFootprint = new ResolvedTileFootprint { OccupiedTiles = horizontalTiles };
            var verticalFootprint = new ResolvedTileFootprint { OccupiedTiles = verticalTiles };
            var layout = ValidLayout();
            layout.Edges[0].Footprint = horizontalFootprint;
            layout.Edges[1].Footprint = verticalFootprint;

            Assert.That(FloorLayoutValidator.Validate(layout, Configuration(), Definitions(), CorridorDefinitions()).Issues.Any(x => x.Reason == FloorLayoutValidationReason.InvalidCorridorFootprint), Is.False);
            Assert.That(horizontalFootprint.OccupiedTiles, Is.SameAs(horizontalTiles));
            Assert.That(verticalFootprint.OccupiedTiles, Is.SameAs(verticalTiles));
            CollectionAssert.AreEqual(new[] { new TileCoordinate(21, 20), new TileCoordinate(20, 20) }, horizontalTiles);
            CollectionAssert.AreEqual(new[] { new TileCoordinate(30, 31), new TileCoordinate(30, 30) }, verticalTiles);

            FloorSpatialLayout canonicalized = layout.Canonicalized();
            CollectionAssert.AreEqual(new[] { new TileCoordinate(20, 20), new TileCoordinate(21, 20) }, canonicalized.Edges.Single(x => x.EdgeId == "edge.0").Footprint.OccupiedTiles);
            CollectionAssert.AreEqual(new[] { new TileCoordinate(30, 30), new TileCoordinate(30, 31) }, canonicalized.Edges.Single(x => x.EdgeId == "edge.1").Footprint.OccupiedTiles);
        }

        [Test]
        public void ThreeOccupants_EmitAllNormalizedOverlapPairs_InEveryPermutation()
        {
            var layout = ValidLayout(); layout.Rooms[0].Anchor = new TileCoordinate(20, 20); layout.Rooms[1].Anchor = new TileCoordinate(20, 20);
            layout.Edges[0].Footprint = new ResolvedTileFootprint(new[] { new TileCoordinate(20, 20), new TileCoordinate(21, 20) });
            string[] expected = FloorLayoutValidator.Validate(layout, Configuration(), Definitions(), CorridorDefinitions()).Issues.Where(x => x.Reason == FloorLayoutValidationReason.FootprintOverlap).Select(Key).ToArray();
            Assert.That(expected.Length, Is.EqualTo(3));
            Assert.That(expected, Does.Contain("13|room:room.0|room:room.1|True|20|20"));
            Assert.That(expected, Does.Contain("13|room:room.0|corridor:edge.0|True|20|20"));
            Assert.That(expected, Does.Contain("13|room:room.1|corridor:edge.0|True|20|20"));
            layout.Rooms = layout.Rooms.Reverse().ToArray(); layout.Edges = layout.Edges.Reverse().ToArray();
            CollectionAssert.AreEqual(expected, FloorLayoutValidator.Validate(layout, Configuration(), Definitions().Reverse(), CorridorDefinitions().Reverse()).Issues.Where(x => x.Reason == FloorLayoutValidationReason.FootprintOverlap).Select(Key));
        }

        [Test]
        public void RoomAndCorridorWithSameRawId_StillOverlapWithTypedDiagnosticIds()
        {
            var layout = ValidLayout();
            layout.Rooms[0].RoomInstanceId = "shared";
            layout.Nodes.Single(x => x.NodeId == "node.room.0").RoomInstanceId = "shared";
            layout.Edges[0].EdgeId = "shared";
            layout.Edges[0].Footprint = new ResolvedTileFootprint(new[] { layout.Rooms[0].Anchor, new TileCoordinate(layout.Rooms[0].Anchor.X + 1, layout.Rooms[0].Anchor.Y) });
            string[] overlaps = FloorLayoutValidator.Validate(layout, Configuration(), Definitions(), CorridorDefinitions()).Issues
                .Where(x => x.Reason == FloorLayoutValidationReason.FootprintOverlap).Select(Key).ToArray();
            Assert.That(overlaps, Does.Contain("13|room:shared|corridor:shared|True|2|0"));
        }

        [Test]
        public void RoomRoomOverlap_HasExactTypedIssueUnderRoomPermutation()
        {
            var layout = ValidLayout(); layout.Rooms[1].Anchor = layout.Rooms[0].Anchor;
            string[] expected = { "13|room:room.0|room:room.1|True|2|0" };
            CollectionAssert.AreEqual(expected, OverlapIssueKeys(layout));
            layout.Rooms = layout.Rooms.Reverse().ToArray();
            CollectionAssert.AreEqual(expected, OverlapIssueKeys(layout));
        }

        [Test]
        public void RoomCorridorOverlap_HasExactTypedIssueUnderInputPermutation()
        {
            var layout = ValidLayout();
            layout.Edges[0].Footprint = new ResolvedTileFootprint(new[] { new TileCoordinate(2, 0), new TileCoordinate(3, 0) });
            string[] expected = { "13|room:room.0|corridor:edge.0|True|2|0" };
            CollectionAssert.AreEqual(expected, OverlapIssueKeys(layout));
            layout.Rooms = layout.Rooms.Reverse().ToArray(); layout.Edges = layout.Edges.Reverse().ToArray();
            CollectionAssert.AreEqual(expected, OverlapIssueKeys(layout));
        }

        [Test]
        public void CorridorCorridorOverlap_HasExactTypedIssuesUnderEdgePermutation()
        {
            var layout = ValidLayout(); layout.Edges[1].Footprint = layout.Edges[0].Footprint;
            string[] expected =
            {
                "13|corridor:edge.0|corridor:edge.1|True|0|2",
                "13|corridor:edge.0|corridor:edge.1|True|1|2"
            };
            CollectionAssert.AreEqual(expected, OverlapIssueKeys(layout));
            layout.Edges = layout.Edges.Reverse().ToArray();
            CollectionAssert.AreEqual(expected, OverlapIssueKeys(layout));
        }

        [Test]
        public void AdjacentRoomsAndAdjacentRoomCorridor_DoNotOverlap()
        {
            var rooms = ValidLayout(); rooms.Rooms[1].Anchor = new TileCoordinate(rooms.Rooms[0].Anchor.X + 1, rooms.Rooms[0].Anchor.Y);
            Assert.That(OverlapIssueKeys(rooms), Is.Empty);

            var corridor = ValidLayout();
            corridor.Edges[0].Footprint = new ResolvedTileFootprint(new[] { new TileCoordinate(2, 1), new TileCoordinate(3, 1) });
            Assert.That(OverlapIssueKeys(corridor), Is.Empty);
        }

        [TestCase("undefined-kind")] [TestCase("missing-room")] [TestCase("ambiguous-room")] [TestCase("room-floor")]
        [TestCase("node-floor")] [TestCase("duplicate-node")]
        public void InvalidIntermediaryNode_CannotProveRoomReachability(string kind)
        {
            var layout = ValidLayout();
            FloorRouteNode intermediary = layout.Nodes.Single(x => x.NodeId == "node.room.0");
            if (kind == "undefined-kind") intermediary.Kind = (FloorRouteNodeKind)99;
            if (kind == "missing-room") intermediary.RoomInstanceId = "room.missing";
            if (kind == "ambiguous-room") layout.Rooms = layout.Rooms.Concat(new[] { Room("room.0", 12) }).ToArray();
            if (kind == "room-floor") layout.Rooms.Single(x => x.RoomInstanceId == "room.0").FloorId = "floor.other";
            if (kind == "node-floor") intermediary.FloorId = "floor.other";
            if (kind == "duplicate-node") layout.Nodes = layout.Nodes.Concat(new[] { Node("node.room.0", FloorRouteNodeKind.Room, "room.0") }).ToArray();
            FloorLayoutValidationResult result = FloorLayoutValidator.Validate(layout, Configuration(20), Definitions(), CorridorDefinitions());
            Assert.That(result.Issues.Any(x => x.Reason == FloorLayoutValidationReason.MissingDestinationNode && x.SubjectId == "edge.0"), Is.True);
            Assert.That(result.Issues.Any(x => x.Reason == FloorLayoutValidationReason.UnreachableRoom && x.SubjectId == "node.room.1"), Is.True);
        }

        [Test]
        public void UndefinedKindIntermediary_ProducesDeterministicOwnedIssueKeys()
        {
            var layout = ValidLayout(); layout.Nodes.Single(x => x.NodeId == "node.room.0").Kind = (FloorRouteNodeKind)99;
            string[] expected =
            {
                "14|edge.1|node.room.0|False|0|0",
                "15|edge.0|node.room.0|False|0|0",
                "21|node.room.1|room.1|False|0|0",
                "36|node.room.0||False|0|0"
            };
            CollectionAssert.AreEqual(expected, OwnedMalformedNodeIssueKeys(layout));
            layout.Nodes = layout.Nodes.Reverse().ToArray(); layout.Edges = layout.Edges.Reverse().ToArray();
            CollectionAssert.AreEqual(expected, OwnedMalformedNodeIssueKeys(layout));
        }

        [Test]
        public void DistinctNodesForOneRoom_CannotBecomeAuthorityOrBridgeRoute()
        {
            var layout = ValidLayout();
            layout.Nodes = layout.Nodes.Concat(new[] { Node("node.room.alias", FloorRouteNodeKind.Room, "room.0") }).ToArray();
            string[] forward = IssueKeys(layout, Definitions(), CorridorDefinitions());
            Assert.That(forward, Does.Contain("31|room.0||False|0|0"));
            Assert.That(forward, Does.Contain("15|edge.0|node.room.0|False|0|0"));
            Assert.That(forward, Does.Contain("14|edge.1|node.room.0|False|0|0"));
            Assert.That(forward, Does.Contain("21|node.room.1|room.1|False|0|0"));
            layout.Nodes = layout.Nodes.Reverse().ToArray();
            CollectionAssert.AreEqual(forward, IssueKeys(layout, Definitions(), CorridorDefinitions()));
        }

        [Test]
        public void InvalidEntranceAndTerminal_DoNotSatisfyGraphConclusions()
        {
            var entrance = ValidLayout(); entrance.Nodes.Single(x => x.NodeId == "entrance").Kind = (FloorRouteNodeKind)99;
            FloorLayoutValidationResult entranceResult = FloorLayoutValidator.Validate(entrance, Configuration(), Definitions(), CorridorDefinitions());
            Assert.That(entranceResult.Issues.Any(x => x.Reason == FloorLayoutValidationReason.MissingEntrance), Is.True);
            var terminal = ValidLayout(); terminal.Nodes.Single(x => x.NodeId == "terminal").FloorId = "floor.other";
            FloorLayoutValidationResult terminalResult = FloorLayoutValidator.Validate(terminal, Configuration(), Definitions(), CorridorDefinitions());
            Assert.That(terminalResult.Issues.Any(x => x.Reason == FloorLayoutValidationReason.RequiredRouteWithoutTerminal), Is.True);
        }

        [Test]
        public void ExistingButInvalidEndpoints_AreRejectedOnBothSides()
        {
            var source = ValidLayout(); source.Nodes.Single(x => x.NodeId == "node.room.0").Kind = (FloorRouteNodeKind)99;
            AssertReason(source, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.MissingSourceNode);
            var destination = ValidLayout(); destination.Nodes.Single(x => x.NodeId == "node.room.0").Kind = (FloorRouteNodeKind)99;
            AssertReason(destination, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.MissingDestinationNode);
        }

        [Test]
        public void DuplicateInputs_AreDeterministicUnderPermutation()
        {
            var first = ValidLayout();
            first.Rooms = first.Rooms.Concat(new[] { Room("room.0", 12) }).ToArray();
            first.Nodes = first.Nodes.Concat(new[] { Node("node.room.0", FloorRouteNodeKind.Room, "room.0") }).ToArray();
            first.Edges = first.Edges.Concat(new[] { Edge("edge.0", "entrance", "terminal", 14) }).ToArray();
            var second = new FloorSpatialLayout { FloorId = first.FloorId, Rooms = first.Rooms.Reverse().ToArray(), Nodes = first.Nodes.Reverse().ToArray(), Edges = first.Edges.Reverse().ToArray() };
            CollectionAssert.AreEqual(IssueKeys(first, Definitions(), CorridorDefinitions()), IssueKeys(second, Definitions().Reverse().ToArray(), CorridorDefinitions().Reverse().ToArray()));
        }

        [TestCase("missing")] [TestCase("cross-floor")] [TestCase("classification")] [TestCase("footprint")]
        public void StructurallyInvalidEdge_DoesNotProveReachabilityOrConsumeConnectionLimit(string kind)
        {
            var layout = ValidLayout(); var edge = layout.Edges[1];
            if (kind == "missing") edge.SourceNodeId = "missing";
            if (kind == "cross-floor") layout.Nodes.Single(x => x.NodeId == edge.SourceNodeId).FloorId = "floor.other";
            if (kind == "classification") edge.Classification = (RouteClassification)99;
            if (kind == "footprint") edge.Footprint = new ResolvedTileFootprint(new[] { new TileCoordinate(4, 2) });
            var result = FloorLayoutValidator.Validate(layout, Configuration(), Definitions(connections: 1), CorridorDefinitions());
            Assert.That(result.Issues.Any(x => x.Reason == FloorLayoutValidationReason.UnreachableRoom), Is.True);
            Assert.That(result.Issues.Any(x => x.Reason == FloorLayoutValidationReason.ConnectionLimitExceeded && x.SubjectId == "node.room.0"), Is.False);
        }

        [Test]
        public void NegativeCostsCannotReduceUsedCapacity_AndNegativeFloorValuesFail()
        {
            var roomResult = FloorLayoutValidator.Validate(ValidLayout(), Configuration(), Definitions(-2), CorridorDefinitions()); Assert.That(roomResult.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(3));
            var corridorResult = FloorLayoutValidator.Validate(ValidLayout(), Configuration(), Definitions(), CorridorDefinitions(-1)); Assert.That(corridorResult.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(4));
            var branch = Configuration(); branch.OptionalBranchAllowance = -1; AssertReason(ValidLayout(), branch, Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.NegativeConfigurationValue);
            var index = Configuration(); index.FloorIndex = -1; AssertReason(ValidLayout(), index, Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.NegativeConfigurationValue);
            AssertReason(ValidLayout(), Configuration(2), Definitions(-2), CorridorDefinitions(), FloorLayoutValidationReason.CapacityExceeded);
        }

        private static RoomSpatialInstance Room(string id, int x) => new RoomSpatialInstance { RoomInstanceId = id, RoomDefinitionId = RoomDefinitionId, FloorId = FloorId, Anchor = new TileCoordinate(x, 0), Orientation = CardinalOrientation.Zero };
        private static FloorRouteNode Node(string id, FloorRouteNodeKind kind, string room = null) => new FloorRouteNode { NodeId = id, FloorId = FloorId, Kind = kind, RoomInstanceId = room };
        private static CorridorEdge Edge(string id, string source, string destination, int x) => new CorridorEdge { EdgeId = id, CorridorDefinitionId = CorridorDefinitionId, FloorId = FloorId, SourceNodeId = source, DestinationNodeId = destination, Footprint = new ResolvedTileFootprint(new[] { new TileCoordinate(x, 2), new TileCoordinate(x + 1, 2) }), Classification = RouteClassification.Required };
        private static void AssertReason(FloorSpatialLayout layout, FloorSpatialConfiguration config, RoomSpatialDefinition[] rooms, CorridorSpatialDefinition[] corridors, FloorLayoutValidationReason reason) => Assert.That(FloorLayoutValidator.Validate(layout, config, rooms, corridors).Issues.Any(x => x.Reason == reason), Is.True);
        private static string[] IssueKeys(FloorSpatialLayout layout, RoomSpatialDefinition[] rooms, CorridorSpatialDefinition[] corridors) => FloorLayoutValidator.Validate(layout, Configuration(30), rooms, corridors).Issues.Select(Key).ToArray();
        private static string[] EndpointIssueKeys(FloorSpatialLayout layout, FloorLayoutValidationReason endpointReason) => FloorLayoutValidator.Validate(layout, Configuration(), Definitions(), CorridorDefinitions()).Issues
            .Where(x => x.SubjectId == "edge.0" && (x.Reason == FloorLayoutValidationReason.MissingStableId || x.Reason == endpointReason)).Select(Key).ToArray();
        private static string[] OverlapIssueKeys(FloorSpatialLayout layout) => FloorLayoutValidator.Validate(layout, Configuration(30), Definitions(), CorridorDefinitions()).Issues
            .Where(x => x.Reason == FloorLayoutValidationReason.FootprintOverlap).Select(Key).ToArray();
        private static string[] OwnedMalformedNodeIssueKeys(FloorSpatialLayout layout) => FloorLayoutValidator.Validate(layout, Configuration(30), Definitions(), CorridorDefinitions()).Issues
            .Where(x => (x.Reason == FloorLayoutValidationReason.InvalidNodeKind && x.SubjectId == "node.room.0") ||
                (x.Reason == FloorLayoutValidationReason.MissingDestinationNode && x.SubjectId == "edge.0") ||
                (x.Reason == FloorLayoutValidationReason.MissingSourceNode && x.SubjectId == "edge.1") ||
                (x.Reason == FloorLayoutValidationReason.UnreachableRoom && x.SubjectId == "node.room.1")).Select(Key).ToArray();
        private static string Key(FloorLayoutValidationIssue issue) => $"{(int)issue.Reason}|{issue.SubjectId}|{issue.RelatedId}|{issue.HasCoordinate}|{issue.Coordinate.X}|{issue.Coordinate.Y}";
    }
}
#endif
