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
        public static SpatialValidationWorkloadLimits Limits(int maximumTiles = 100) => new SpatialValidationWorkloadLimits(maximumTiles);
        private const string RoomDefinitionId = "room.definition.test";
        private const string CorridorDefinitionId = "corridor.definition.test";
        private const int FinalCapacity = 8;

        public static FloorSpatialConfiguration Configuration(int capacity = FinalCapacity, int branches = 0) => new FloorSpatialConfiguration { FloorDefinitionId = "floor.definition.test", FloorIndex = 0, Bounds = new RectangularFloorBounds(new TileCoordinate(-100, -100), 400, 400), FinalFloorSpaceCapacity = capacity, OptionalBranchAllowance = branches };
        public static RoomSpatialDefinition[] Definitions(int connections = 2) => new[] { new RoomSpatialDefinition { RoomDefinitionId = RoomDefinitionId, GrossFootprint = new RectangularFootprintDefinition(1, 1), MaximumConnectionCount = connections, MonsterCapacity = 1, TrapCapacity = 2, LootCapacity = 3 } };
        public static CorridorSpatialDefinition[] CorridorDefinitions() => new[] { new CorridorSpatialDefinition { CorridorDefinitionId = CorridorDefinitionId } };

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
            var result = FloorLayoutValidator.Validate(ValidLayout(terminal), Configuration(), Definitions(), CorridorDefinitions(), Limits());
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
            var result = FloorLayoutValidator.Validate(ValidLayout(), Configuration(), definitions, CorridorDefinitions(), Limits());
            Assert.That(result.Issues.Any(x => x.Reason == FloorLayoutValidationReason.ReservedTileOutsideFootprint), Is.True);
            Assert.That(result.Issues.Any(x => x.Reason == FloorLayoutValidationReason.DuplicateReservedTile), Is.True);
        }

        [TestCase("room-room")] [TestCase("room-corridor")] [TestCase("corridor-corridor")]
        public void OverlapKinds_AreReported_WhileAdjacencyPasses(string kind)
        {
            FloorSpatialLayout layout = ValidLayout();
            if (kind == "room-room") layout.Rooms[1].Anchor = layout.Rooms[0].Anchor;
            if (kind == "room-corridor") layout.Edges[0].Footprint = new ResolvedTileFootprint(new[] { layout.Rooms[0].Anchor, new TileCoordinate(layout.Rooms[0].Anchor.X + 1, layout.Rooms[0].Anchor.Y) });
            if (kind == "corridor-corridor") layout.Edges[1].Footprint = layout.Edges[0].Footprint;
            AssertReason(layout, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.FootprintOverlap);
            Assert.That(FloorLayoutValidator.Validate(ValidLayout(), Configuration(), Definitions(), CorridorDefinitions(), Limits()).Issues.Any(x => x.Reason == FloorLayoutValidationReason.FootprintOverlap), Is.False);
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
            Assert.That(FloorLayoutValidator.Validate(missing, Configuration(), Definitions(), CorridorDefinitions(), Limits()).Issues.Any(x =>
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
            Assert.That(FloorLayoutValidator.Validate(ValidLayout(), Configuration(), Definitions(connections: 2), CorridorDefinitions(), Limits()).Issues.Any(x => x.Reason == FloorLayoutValidationReason.ConnectionLimitExceeded), Is.False);
            var exceeded = ValidLayout();
            string[] connectionIssues = FloorLayoutValidator.Validate(exceeded, Configuration(), Definitions(connections: 1), CorridorDefinitions(), Limits()).Issues
                .Where(x => x.Reason == FloorLayoutValidationReason.ConnectionLimitExceeded).Select(Key).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "22|node.room.0|room.0|False|0|0",
                "22|node.room.1|room.1|False|0|0"
            }, connectionIssues);
            exceeded.Rooms = exceeded.Rooms.Reverse().ToArray(); exceeded.Nodes = exceeded.Nodes.Reverse().ToArray(); exceeded.Edges = exceeded.Edges.Reverse().ToArray();
            CollectionAssert.AreEqual(connectionIssues, FloorLayoutValidator.Validate(exceeded, Configuration(), Definitions(connections: 1), CorridorDefinitions(), Limits()).Issues
                .Where(x => x.Reason == FloorLayoutValidationReason.ConnectionLimitExceeded).Select(Key).ToArray());
            var deadEnd = ValidLayout(); deadEnd.Edges = deadEnd.Edges.Where(x => x.EdgeId != "edge.2").ToArray(); AssertReason(deadEnd, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.RequiredRouteWithoutTerminal);
            var cycle = ValidLayout(); cycle.Edges[2].DestinationNodeId = "node.room.0"; AssertReason(cycle, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.RequiredRouteWithoutTerminal);
        }

        [Test]
        public void OptionalBranches_CountDistinctIds_AndEnforceClassificationRules()
        {
            var zero = ValidLayout(); Assert.That(FloorLayoutValidator.Validate(zero, Configuration(branches: 0), Definitions(), CorridorDefinitions(), Limits()).IsValid, Is.True);
            var one = ValidLayout(); one.Edges[1].Classification = RouteClassification.Optional; one.Edges[1].OptionalBranchId = "branch.a"; one.Edges[2].Classification = RouteClassification.Optional; one.Edges[2].OptionalBranchId = "branch.a"; Assert.That(FloorLayoutValidator.Validate(one, Configuration(branches: 1), Definitions(), CorridorDefinitions(), Limits()).Issues.Any(x => x.Reason == FloorLayoutValidationReason.OptionalBranchAllowanceExceeded), Is.False);
            var two = ValidLayout(); two.Edges[1].Classification = RouteClassification.Optional; two.Edges[1].OptionalBranchId = "branch.a"; two.Edges[2].Classification = RouteClassification.Optional; two.Edges[2].OptionalBranchId = "branch.b"; AssertReason(two, Configuration(branches: 1), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.OptionalBranchAllowanceExceeded);
            var missing = ValidLayout(); missing.Edges[1].Classification = RouteClassification.Optional; AssertReason(missing, Configuration(branches: 1), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.OptionalEdgeMissingBranchId);
            var required = ValidLayout(); required.Edges[0].OptionalBranchId = "branch.a"; AssertReason(required, Configuration(branches: 1), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.RequiredEdgeHasBranchId);
        }

        [Test]
        public void DuplicateAndMissingDefinitions_AndMultipleFailures_AreReturned()
        {
            var duplicate = ValidLayout(); duplicate.Rooms = duplicate.Rooms.Concat(new[] { Room("room.0", 10) }).ToArray(); duplicate.Nodes = duplicate.Nodes.Concat(new[] { Node("entrance", FloorRouteNodeKind.Entrance) }).ToArray(); duplicate.Edges = duplicate.Edges.Concat(new[] { Edge("edge.0", "missing", "missing", 12) }).ToArray();
            var result = FloorLayoutValidator.Validate(duplicate, null, Array.Empty<RoomSpatialDefinition>(), Array.Empty<CorridorSpatialDefinition>(), Limits());
            Assert.That(result.Issues.Select(x => x.Reason), Does.Contain(FloorLayoutValidationReason.DuplicateRoomId)); Assert.That(result.Issues.Select(x => x.Reason), Does.Contain(FloorLayoutValidationReason.DuplicateNodeId)); Assert.That(result.Issues.Select(x => x.Reason), Does.Contain(FloorLayoutValidationReason.DuplicateEdgeId)); Assert.That(result.Issues.Select(x => x.Reason), Does.Contain(FloorLayoutValidationReason.MissingFloorConfiguration)); Assert.That(result.Issues.Select(x => x.Reason), Does.Contain(FloorLayoutValidationReason.MissingRoomDefinition)); Assert.That(result.Issues.Select(x => x.Reason), Does.Contain(FloorLayoutValidationReason.MissingCorridorDefinition));
        }

        [Test]
        public void ShuffledInputs_ProduceIdenticalIssues_AndValidationDoesNotMutateInput()
        {
            var first = ValidLayout(); first.Edges[0].SourceNodeId = "missing"; var second = new FloorSpatialLayout { FloorId = first.FloorId, Rooms = first.Rooms.Reverse().ToArray(), Nodes = first.Nodes.Reverse().ToArray(), Edges = first.Edges.Reverse().ToArray() };
            FloorSpatialConfiguration configuration = Configuration();
            RoomSpatialDefinition[] roomDefinitions = Definitions();
            CorridorSpatialDefinition[] corridorDefinitions = CorridorDefinitions();
            string[] originalRooms = first.Rooms.Select(x => x.RoomInstanceId).ToArray();
            string[] originalNodes = first.Nodes.Select(x => x.NodeId).ToArray();
            string[] originalEdges = first.Edges.Select(x => x.EdgeId).ToArray();
            TileCoordinate[][] originalFootprints = first.Edges.Select(x => x.Footprint?.OccupiedTiles?.ToArray()).ToArray();
            TileCoordinate boundsMinimum = configuration.Bounds.Minimum; int boundsWidth = configuration.Bounds.Width; int boundsHeight = configuration.Bounds.Height;
            string roomDefinitionId = roomDefinitions[0].RoomDefinitionId; int maximumConnections = roomDefinitions[0].MaximumConnectionCount;
            string corridorDefinitionId = corridorDefinitions[0].CorridorDefinitionId;
            var a = FloorLayoutValidator.Validate(first, configuration, roomDefinitions, corridorDefinitions, Limits());
            var b = FloorLayoutValidator.Validate(second, configuration, roomDefinitions.Reverse(), corridorDefinitions.Reverse(), Limits());
            CollectionAssert.AreEqual(a.Issues.Select(Key), b.Issues.Select(Key)); AssertCapacityEqual(a.Capacity, b.Capacity);
            CollectionAssert.AreEqual(originalRooms, first.Rooms.Select(x => x.RoomInstanceId));
            CollectionAssert.AreEqual(originalNodes, first.Nodes.Select(x => x.NodeId));
            CollectionAssert.AreEqual(originalEdges, first.Edges.Select(x => x.EdgeId));
            for (int index = 0; index < originalFootprints.Length; index++) CollectionAssert.AreEqual(originalFootprints[index], first.Edges[index].Footprint?.OccupiedTiles);
            Assert.That(configuration.Bounds.Minimum, Is.EqualTo(boundsMinimum)); Assert.That(configuration.Bounds.Width, Is.EqualTo(boundsWidth)); Assert.That(configuration.Bounds.Height, Is.EqualTo(boundsHeight));
            Assert.That(roomDefinitions[0].RoomDefinitionId, Is.EqualTo(roomDefinitionId)); Assert.That(roomDefinitions[0].MaximumConnectionCount, Is.EqualTo(maximumConnections));
            Assert.That(corridorDefinitions[0].CorridorDefinitionId, Is.EqualTo(corridorDefinitionId));
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
            RoomSpatialDefinition first = Definitions(connections: 1)[0];
            RoomSpatialDefinition second = Definitions(connections: 7)[0];
            second.GrossFootprint = new RectangularFootprintDefinition(3, 2);
            FloorSpatialLayout layout = ValidLayout();
            FloorLayoutValidationResult forward = FloorLayoutValidator.Validate(layout, Configuration(30), new[] { first, second }, CorridorDefinitions());
            FloorLayoutValidationResult reverse = FloorLayoutValidator.Validate(layout, Configuration(30), new[] { second, first }, CorridorDefinitions());
            CollectionAssert.AreEqual(forward.Issues.Select(Key), reverse.Issues.Select(Key));
            Assert.That(forward.Issues.Any(x => x.Reason == FloorLayoutValidationReason.DuplicateRoomDefinitionId && x.SubjectId == RoomDefinitionId), Is.True);
            Assert.That(forward.Issues.Count(x => x.Reason == FloorLayoutValidationReason.MissingRoomDefinition), Is.EqualTo(layout.Rooms.Length));
            Assert.That(forward.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(6));
            Assert.That(reverse.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(forward.Capacity.UsedFloorSpaceCapacity));
        }

        [Test]
        public void DuplicateCorridorDefinitions_AreAmbiguousAndDeterministic()
        {
            CorridorSpatialDefinition first = CorridorDefinitions()[0];
            CorridorSpatialDefinition second = CorridorDefinitions()[0];
            FloorSpatialLayout layout = ValidLayout();
            FloorLayoutValidationResult forward = FloorLayoutValidator.Validate(layout, Configuration(30), Definitions(), new[] { first, second });
            FloorLayoutValidationResult reverse = FloorLayoutValidator.Validate(layout, Configuration(30), Definitions(), new[] { second, first });
            CollectionAssert.AreEqual(forward.Issues.Select(Key), reverse.Issues.Select(Key));
            Assert.That(forward.Issues.Any(x => x.Reason == FloorLayoutValidationReason.DuplicateCorridorDefinitionId && x.SubjectId == CorridorDefinitionId), Is.True);
            Assert.That(forward.Issues.Count(x => x.Reason == FloorLayoutValidationReason.MissingCorridorDefinition), Is.EqualTo(layout.Edges.Length));
            Assert.That(forward.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(2));
            Assert.That(reverse.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(forward.Capacity.UsedFloorSpaceCapacity));
        }

        [Test]
        public void RoomNodeBijectionAndFloorMembership_AreValidated()
        {
            Assert.That(FloorLayoutValidator.Validate(ValidLayout(), Configuration(), Definitions(), CorridorDefinitions(), Limits()).IsValid, Is.True);
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

        [TestCase("null")] [TestCase("empty")] [TestCase("duplicate")] [TestCase("diagonal")] [TestCase("bent")] [TestCase("gapped")]
        public void InvalidCorridorFootprints_AreRejected(string kind)
        {
            var layout = ValidLayout();
            TileCoordinate[] tiles = kind == "empty" ? Array.Empty<TileCoordinate>() : kind == "duplicate" ? new[] { new TileCoordinate(20, 20), new TileCoordinate(20, 20) } : kind == "diagonal" ? new[] { new TileCoordinate(20, 20), new TileCoordinate(21, 21) } :
                kind == "bent" ? new[] { new TileCoordinate(20, 20), new TileCoordinate(21, 20), new TileCoordinate(21, 21) } : new[] { new TileCoordinate(20, 20), new TileCoordinate(22, 20) };
            layout.Edges[0].Footprint = kind == "null" ? null : new ResolvedTileFootprint(tiles);
            AssertReason(layout, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.InvalidCorridorFootprint);
        }

        [Test]
        public void OneTilePhysicalCorridorContributesOneTileAndParticipatesInGraphAndConnections()
        {
            var layout = ValidLayout(); layout.Edges[1].Footprint = new ResolvedTileFootprint(new[] { new TileCoordinate(4, 2) });
            FloorLayoutValidationResult valid = FloorLayoutValidator.Validate(layout, Configuration(7), Definitions(), CorridorDefinitions(), Limits());
            Assert.That(valid.IsValid, Is.True); Assert.That(valid.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(7));
            Assert.That(valid.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.UnreachableRoom), Is.False);
            Assert.That(FloorLayoutValidator.Validate(layout, Configuration(7), Definitions(connections: 1), CorridorDefinitions(), Limits()).Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.ConnectionLimitExceeded && issue.SubjectId == "node.room.0"), Is.True);
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

            Assert.That(FloorLayoutValidator.Validate(layout, Configuration(), Definitions(), CorridorDefinitions(), Limits()).Issues.Any(x => x.Reason == FloorLayoutValidationReason.InvalidCorridorFootprint), Is.False);
            Assert.That(horizontalFootprint.OccupiedTiles, Is.SameAs(horizontalTiles));
            Assert.That(verticalFootprint.OccupiedTiles, Is.SameAs(verticalTiles));
            CollectionAssert.AreEqual(new[] { new TileCoordinate(21, 20), new TileCoordinate(20, 20) }, horizontalTiles);
            CollectionAssert.AreEqual(new[] { new TileCoordinate(30, 31), new TileCoordinate(30, 30) }, verticalTiles);

            FloorSpatialLayout canonicalized = layout.Canonicalized();
            TileCoordinate[] canonicalHorizontalTiles = canonicalized.Edges.Single(x => x.EdgeId == "edge.0").Footprint.OccupiedTiles;
            TileCoordinate[] canonicalVerticalTiles = canonicalized.Edges.Single(x => x.EdgeId == "edge.1").Footprint.OccupiedTiles;
            Assert.That(horizontalFootprint.OccupiedTiles, Is.SameAs(horizontalTiles));
            Assert.That(verticalFootprint.OccupiedTiles, Is.SameAs(verticalTiles));
            CollectionAssert.AreEqual(new[] { new TileCoordinate(21, 20), new TileCoordinate(20, 20) }, horizontalTiles);
            CollectionAssert.AreEqual(new[] { new TileCoordinate(30, 31), new TileCoordinate(30, 30) }, verticalTiles);
            Assert.That(canonicalHorizontalTiles, Is.Not.SameAs(horizontalTiles));
            Assert.That(canonicalVerticalTiles, Is.Not.SameAs(verticalTiles));
            CollectionAssert.AreEqual(new[] { new TileCoordinate(20, 20), new TileCoordinate(21, 20) }, canonicalHorizontalTiles);
            CollectionAssert.AreEqual(new[] { new TileCoordinate(30, 30), new TileCoordinate(30, 31) }, canonicalVerticalTiles);
        }

        [Test]
        public void ThreeOccupants_EmitAllNormalizedOverlapPairs_InEveryPermutation()
        {
            var layout = ValidLayout(); layout.Rooms[0].Anchor = new TileCoordinate(20, 20); layout.Rooms[1].Anchor = new TileCoordinate(20, 20);
            layout.Edges[0].Footprint = new ResolvedTileFootprint(new[] { new TileCoordinate(20, 20), new TileCoordinate(21, 20) });
            string[] expected = FloorLayoutValidator.Validate(layout, Configuration(), Definitions(), CorridorDefinitions(), Limits()).Issues.Where(x => x.Reason == FloorLayoutValidationReason.FootprintOverlap).Select(Key).ToArray();
            Assert.That(expected.Length, Is.EqualTo(3));
            Assert.That(expected, Does.Contain("13|room:room.0|room:room.1|True|20|20"));
            Assert.That(expected, Does.Contain("13|room:room.0|corridor:edge.0|True|20|20"));
            Assert.That(expected, Does.Contain("13|room:room.1|corridor:edge.0|True|20|20"));
            layout.Rooms = layout.Rooms.Reverse().ToArray(); layout.Edges = layout.Edges.Reverse().ToArray();
            CollectionAssert.AreEqual(expected, FloorLayoutValidator.Validate(layout, Configuration(), Definitions().Reverse(), CorridorDefinitions().Reverse(), Limits()).Issues.Where(x => x.Reason == FloorLayoutValidationReason.FootprintOverlap).Select(Key));
        }

        [Test]
        public void RoomAndCorridorWithSameRawId_StillOverlapWithTypedDiagnosticIds()
        {
            var layout = ValidLayout();
            layout.Rooms[0].RoomInstanceId = "shared";
            layout.Nodes.Single(x => x.NodeId == "node.room.0").RoomInstanceId = "shared";
            layout.Edges[0].EdgeId = "shared";
            layout.Edges[0].Footprint = new ResolvedTileFootprint(new[] { layout.Rooms[0].Anchor, new TileCoordinate(layout.Rooms[0].Anchor.X + 1, layout.Rooms[0].Anchor.Y) });
            string[] overlaps = FloorLayoutValidator.Validate(layout, Configuration(), Definitions(), CorridorDefinitions(), Limits()).Issues
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
            FloorLayoutValidationResult result = FloorLayoutValidator.Validate(layout, Configuration(20), Definitions(), CorridorDefinitions(), Limits());
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
            FloorLayoutValidationResult entranceResult = FloorLayoutValidator.Validate(entrance, Configuration(), Definitions(), CorridorDefinitions(), Limits());
            Assert.That(entranceResult.Issues.Any(x => x.Reason == FloorLayoutValidationReason.MissingEntrance), Is.True);
            var terminal = ValidLayout(); terminal.Nodes.Single(x => x.NodeId == "terminal").FloorId = "floor.other";
            FloorLayoutValidationResult terminalResult = FloorLayoutValidator.Validate(terminal, Configuration(), Definitions(), CorridorDefinitions(), Limits());
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
            var result = FloorLayoutValidator.Validate(layout, Configuration(), Definitions(connections: 1), CorridorDefinitions(), Limits());
            Assert.That(result.Issues.Any(x => x.Reason == FloorLayoutValidationReason.UnreachableRoom), Is.True);
            Assert.That(result.Issues.Any(x => x.Reason == FloorLayoutValidationReason.ConnectionLimitExceeded && x.SubjectId == "node.room.0"), Is.False);
        }

        [Test]
        public void NegativeFloorValuesFailWithoutIndependentStructureCosts()
        {
            var branch = Configuration(); branch.OptionalBranchAllowance = -1; AssertReason(ValidLayout(), branch, Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.NegativeConfigurationValue);
            var index = Configuration(); index.FloorIndex = -1; AssertReason(ValidLayout(), index, Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.NegativeConfigurationValue);
            FloorLayoutValidationResult exceeded = FloorLayoutValidator.Validate(ValidLayout(), Configuration(7), Definitions(), CorridorDefinitions(), Limits());
            Assert.That(exceeded.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.CapacityExceeded), Is.True);
            Assert.That(exceeded.Capacity.RemainingFloorSpaceCapacity, Is.EqualTo(-1));
        }

        [TestCase(-1, 0)] [TestCase(10, 0)] [TestCase(0, -1)] [TestCase(0, 10)]
        public void RoomOutsideEachBoundsEdge_EmitsTypedCoordinate(int x, int y)
        {
            var layout = ValidLayout(); layout.Rooms[0].Anchor = new TileCoordinate(x, y);
            var config = Configuration(); config.Bounds = new RectangularFloorBounds(default, 10, 10);
            var issue = FloorLayoutValidator.Validate(layout, config, Definitions(), CorridorDefinitions(), Limits()).Issues.Single(candidate =>
                candidate.Reason == FloorLayoutValidationReason.StructureTileOutsideFloorBounds && candidate.SubjectId == "room:room.0");
            Assert.That(issue.Coordinate, Is.EqualTo(new TileCoordinate(x, y)));
        }

        [TestCase(-1, 0, -1, 1)] [TestCase(10, 0, 10, 1)] [TestCase(0, -1, 1, -1)] [TestCase(0, 10, 1, 10)]
        public void CorridorOutsideEachBoundsEdge_EmitsOneIssuePerCoordinate(int x1, int y1, int x2, int y2)
        {
            var layout = ValidLayout();
            layout.Edges[0].Footprint = new ResolvedTileFootprint(new[] { new TileCoordinate(x1, y1), new TileCoordinate(x2, y2) });
            var config = Configuration(); config.Bounds = new RectangularFloorBounds(default, 10, 10);
            var issues = FloorLayoutValidator.Validate(layout, config, Definitions(), CorridorDefinitions(), Limits()).Issues.Where(candidate =>
                candidate.Reason == FloorLayoutValidationReason.StructureTileOutsideFloorBounds && candidate.SubjectId == "corridor:edge.0").ToArray();
            Assert.That(issues.Length, Is.EqualTo(2));
        }

        [Test]
        public void RotatedRectangularRoomIsCheckedAgainstResolvedBounds()
        {
            var definitions = Definitions(); definitions[0].GrossFootprint = new RectangularFootprintDefinition(2, 3);
            var layout = ValidLayout(); layout.Rooms[0].Anchor = new TileCoordinate(7, 8); layout.Rooms[0].Orientation = CardinalOrientation.Ninety;
            var config = Configuration(20); config.Bounds = new RectangularFloorBounds(default, 10, 10);
            FloorLayoutValidationResult result = FloorLayoutValidator.Validate(layout, config, definitions, CorridorDefinitions(), Limits());
            Assert.That(result.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.StructureTileOutsideFloorBounds && issue.SubjectId == "room:room.0"), Is.False);
        }

        [Test]
        public void MissingInvalidBoundsAndCapacityAboveArea_AreRejected()
        {
            var missing = Configuration(); missing.Bounds = null;
            AssertReason(ValidLayout(), missing, Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.InvalidFloorBounds);
            var invalid = Configuration(); invalid.Bounds = new RectangularFloorBounds(default, 0, 10);
            AssertReason(ValidLayout(), invalid, Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.InvalidFloorBounds);
            var excessive = Configuration(5); excessive.Bounds = new RectangularFloorBounds(default, 2, 2);
            AssertReason(ValidLayout(), excessive, Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.FinalCapacityExceedsFloorBounds);
        }

        [Test]
        public void CapacityUsesTileUnionIncludingReservedAndOverlapsOnlyOnce()
        {
            var definitions = Definitions(); definitions[0].GrossFootprint = new RectangularFootprintDefinition(2, 1);
            definitions[0].ReservedTileOffsets = new[] { new TileCoordinate(1, 0) };
            var layout = ValidLayout(); layout.Rooms[1].Anchor = layout.Rooms[0].Anchor;
            layout.Edges[0].Footprint = new ResolvedTileFootprint(new[] { layout.Rooms[0].Anchor, new TileCoordinate(3, 0) });
            layout.Edges[1].Footprint = layout.Edges[0].Footprint;
            FloorLayoutValidationResult result = FloorLayoutValidator.Validate(layout, Configuration(20), definitions, CorridorDefinitions(), Limits());
            Assert.That(result.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(4));
            Assert.That(result.Capacity.RemainingFloorSpaceCapacity, Is.EqualTo(16));
        }

        [Test]
        public void DirectDoorwayHasNoPhysicalAuthorityButParticipatesInGraph()
        {
            var layout = ValidLayout();
            layout.Edges[1].ConnectionKind = FloorRouteConnectionKind.DirectDoorway;
            layout.Edges[1].CorridorDefinitionId = null;
            layout.Edges[1].Footprint = null;
            FloorLayoutValidationResult result = FloorLayoutValidator.Validate(layout, Configuration(6), Definitions(), CorridorDefinitions(), Limits());
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(6));
            Assert.That(result.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.UnreachableRoom), Is.False);
        }

        [Test]
        public void DirectDoorwayRejectsDefinitionAndAnyFootprintShell()
        {
            var definition = ValidLayout(); definition.Edges[0].ConnectionKind = FloorRouteConnectionKind.DirectDoorway;
            AssertReason(definition, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.DirectDoorwayHasCorridorDefinition);
            var footprint = ValidLayout(); footprint.Edges[0].ConnectionKind = FloorRouteConnectionKind.DirectDoorway;
            footprint.Edges[0].CorridorDefinitionId = " "; footprint.Edges[0].Footprint = new ResolvedTileFootprint();
            AssertReason(footprint, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.DirectDoorwayHasFootprint);
        }

        [Test]
        public void InvalidConnectionKindAndPhysicalCorridorRequirements_AreRejectedWithoutCapacityAuthority()
        {
            var invalid = ValidLayout(); invalid.Edges[0].ConnectionKind = (FloorRouteConnectionKind)99;
            AssertReason(invalid, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.InvalidConnectionKind);
            var missingDefinition = ValidLayout(); missingDefinition.Edges[0].CorridorDefinitionId = "missing";
            FloorLayoutValidationResult missing = FloorLayoutValidator.Validate(missingDefinition, Configuration(), Definitions(), CorridorDefinitions(), Limits());
            Assert.That(missing.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.MissingCorridorDefinition), Is.True);
            Assert.That(missing.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(6));
            var missingFootprint = ValidLayout(); missingFootprint.Edges[0].Footprint = null;
            FloorLayoutValidationResult footprint = FloorLayoutValidator.Validate(missingFootprint, Configuration(), Definitions(), CorridorDefinitions(), Limits());
            Assert.That(footprint.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.InvalidCorridorFootprint), Is.True);
            Assert.That(footprint.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(6));
        }

        [Test]
        public void MissingCorridorDefinitionStillProducesRawGeometryDiagnosticsWithoutAuthority()
        {
            var layout = ValidLayout();
            layout.Edges[1].CorridorDefinitionId = "corridor.missing";
            layout.Edges[1].Footprint = new ResolvedTileFootprint(new[] { layout.Rooms[0].Anchor, new TileCoordinate(3, 0) });
            FloorLayoutValidationResult result = FloorLayoutValidator.Validate(layout, Configuration(), Definitions(), CorridorDefinitions(), Limits());
            Assert.That(result.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.MissingCorridorDefinition && issue.SubjectId == "edge.1"), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.FootprintOverlap && issue.SubjectId == "room:room.0" && issue.RelatedId == "corridor:edge.1"), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.UnreachableRoom && issue.SubjectId == "node.room.1"), Is.True);
            Assert.That(result.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(6));
        }

        [Test]
        public void AmbiguousCorridorDefinitionStillProducesEveryRawBoundsDiagnosticWithoutAuthority()
        {
            var layout = ValidLayout();
            TileCoordinate[] rawTiles = { new TileCoordinate(-1, 1), new TileCoordinate(-2, 1) };
            layout.Edges[1].Footprint = new ResolvedTileFootprint { OccupiedTiles = rawTiles };
            CorridorSpatialDefinition[] definitions = CorridorDefinitions().Concat(CorridorDefinitions()).ToArray();
            var config = Configuration(); config.Bounds = new RectangularFloorBounds(default, 10, 10);

            FloorLayoutValidationResult forward = FloorLayoutValidator.Validate(layout, config, Definitions(), definitions, Limits());
            layout.Rooms = layout.Rooms.Reverse().ToArray(); layout.Edges = layout.Edges.Reverse().ToArray();
            rawTiles = rawTiles.Reverse().ToArray(); layout.Edges.Single(edge => edge.EdgeId == "edge.1").Footprint.OccupiedTiles = rawTiles;
            FloorLayoutValidationResult reverse = FloorLayoutValidator.Validate(layout, config, Definitions().Reverse(), definitions.Reverse(), Limits());

            Assert.That(forward.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.DuplicateCorridorDefinitionId), Is.True);
            Assert.That(forward.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.MissingCorridorDefinition && issue.SubjectId == "edge.1"), Is.True);
            Assert.That(forward.Issues.Count(issue => issue.Reason == FloorLayoutValidationReason.StructureTileOutsideFloorBounds && issue.SubjectId == "corridor:edge.1"), Is.EqualTo(2));
            Assert.That(forward.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(2));
            CollectionAssert.AreEqual(forward.Issues.Select(Key), reverse.Issues.Select(Key));
            AssertCapacityEqual(forward.Capacity, reverse.Capacity);
            CollectionAssert.AreEqual(new[] { new TileCoordinate(-2, 1), new TileCoordinate(-1, 1) }, rawTiles);
        }

        [Test]
        public void InvalidCorridorFootprintStillProducesRawOverlapAndBoundsDiagnosticsWithoutAuthority()
        {
            var layout = ValidLayout();
            TileCoordinate[] rawTiles = { layout.Rooms[0].Anchor, new TileCoordinate(10, 0) };
            layout.Edges[1].Footprint = new ResolvedTileFootprint { OccupiedTiles = rawTiles };
            var config = Configuration(); config.Bounds = new RectangularFloorBounds(default, 10, 10);
            FloorLayoutValidationResult result = FloorLayoutValidator.Validate(layout, config, Definitions(connections: 1), CorridorDefinitions(), Limits());
            Assert.That(result.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.InvalidCorridorFootprint && issue.SubjectId == "edge.1"), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.FootprintOverlap && issue.RelatedId == "corridor:edge.1"), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.StructureTileOutsideFloorBounds && issue.SubjectId == "corridor:edge.1" && issue.Coordinate.Equals(new TileCoordinate(10, 0))), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.UnreachableRoom && issue.SubjectId == "node.room.1"), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.ConnectionLimitExceeded && issue.SubjectId == "node.room.0"), Is.False);
            Assert.That(result.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(6));
            Assert.That(layout.Edges[1].Footprint.OccupiedTiles, Is.SameAs(rawTiles));
            CollectionAssert.AreEqual(new[] { new TileCoordinate(2, 0), new TileCoordinate(10, 0) }, rawTiles);
        }

        [Test]
        public void DuplicateRawCorridorCoordinatesEmitUniqueBoundsDiagnosticsWithoutAuthorityOrMutation()
        {
            var layout = ValidLayout();
            TileCoordinate[] rawTiles = { new TileCoordinate(-1, 0), new TileCoordinate(-1, 0), new TileCoordinate(-2, 0) };
            layout.Edges[1].Footprint = new ResolvedTileFootprint { OccupiedTiles = rawTiles };
            var config = Configuration(); config.Bounds = new RectangularFloorBounds(default, 10, 10);
            FloorLayoutValidationResult result = FloorLayoutValidator.Validate(layout, config, Definitions(), CorridorDefinitions(), Limits());
            Assert.That(result.Issues.Count(issue => issue.Reason == FloorLayoutValidationReason.InvalidCorridorFootprint && issue.SubjectId == "edge.1"), Is.EqualTo(1));
            FloorLayoutValidationIssue[] bounds = result.Issues.Where(issue => issue.Reason == FloorLayoutValidationReason.StructureTileOutsideFloorBounds && issue.SubjectId == "corridor:edge.1").ToArray();
            Assert.That(bounds.Length, Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { new TileCoordinate(-2, 0), new TileCoordinate(-1, 0) }, bounds.Select(issue => issue.Coordinate));
            Assert.That(result.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(6));
            Assert.That(layout.Edges[1].Footprint.OccupiedTiles, Is.SameAs(rawTiles));
            CollectionAssert.AreEqual(new[] { new TileCoordinate(-1, 0), new TileCoordinate(-1, 0), new TileCoordinate(-2, 0) }, rawTiles);
        }

        [Test]
        public void ExtremeCorridorFootprintProducesStableIssueWithoutOverflowOrMutation()
        {
            var layout = ValidLayout(); TileCoordinate[] rawTiles = { new TileCoordinate(int.MinValue, 0), new TileCoordinate(int.MaxValue, 0) };
            layout.Edges[1].Footprint = new ResolvedTileFootprint { OccupiedTiles = rawTiles };
            AssertReason(layout, Configuration(), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.InvalidCorridorFootprint);
            CollectionAssert.AreEqual(new[] { new TileCoordinate(int.MinValue, 0), new TileCoordinate(int.MaxValue, 0) }, rawTiles);
        }

        [Test]
        public void OversizedRawCorridorFailsBeforeTileProcessingWithoutAuthorityOrMutation()
        {
            var layout = ValidLayout();
            TileCoordinate[] rawTiles = { new TileCoordinate(2, 0), new TileCoordinate(3, 0), new TileCoordinate(4, 0), new TileCoordinate(5, 0) };
            layout.Edges[1].Footprint = new ResolvedTileFootprint { OccupiedTiles = rawTiles };
            FloorLayoutValidationResult result = FloorLayoutValidator.Validate(layout, Configuration(), Definitions(connections: 1), CorridorDefinitions(), Limits(3));
            Assert.That(result.Issues.Count(issue => issue.Reason == FloorLayoutValidationReason.InvalidCorridorFootprint && issue.SubjectId == "edge.1"), Is.EqualTo(1));
            Assert.That(result.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.FootprintOverlap && issue.RelatedId == "corridor:edge.1"), Is.False);
            Assert.That(result.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.UnreachableRoom && issue.SubjectId == "node.room.1"), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.ConnectionLimitExceeded && issue.SubjectId == "node.room.0"), Is.False);
            Assert.That(result.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(6));
            Assert.That(layout.Edges[1].Footprint.OccupiedTiles, Is.SameAs(rawTiles));
            CollectionAssert.AreEqual(new[] { new TileCoordinate(2, 0), new TileCoordinate(3, 0), new TileCoordinate(4, 0), new TileCoordinate(5, 0) }, rawTiles);
        }

        [Test]
        public void NonpositiveLimitsAndOversizedReservedTilesFailClosedWithoutMutation()
        {
            var layout = ValidLayout();
            RoomSpatialDefinition[] definitions = Definitions();
            TileCoordinate[] reserved = { default, default, default, default };
            definitions[0].ReservedTileOffsets = reserved;
            FloorLayoutValidationResult oversized = FloorLayoutValidator.Validate(layout, Configuration(), definitions, CorridorDefinitions(), Limits(3));
            Assert.That(oversized.Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.InvalidRoomFootprint), Is.True);
            Assert.That(definitions[0].ReservedTileOffsets, Is.SameAs(reserved));
            CollectionAssert.AreEqual(new[] { default(TileCoordinate), default(TileCoordinate), default(TileCoordinate), default(TileCoordinate) }, reserved);
            FloorLayoutValidationResult missing = FloorLayoutValidator.Validate(ValidLayout(), Configuration(), Definitions(), CorridorDefinitions(), default);
            Assert.That(missing.IsValid, Is.False);
            Assert.That(missing.Capacity.UsedFloorSpaceCapacity, Is.Zero);
            FloorLayoutValidationResult negative = FloorLayoutValidator.Validate(ValidLayout(), Configuration(), Definitions(), CorridorDefinitions(), Limits(-1));
            Assert.That(negative.IsValid, Is.False);
            Assert.That(negative.Capacity.UsedFloorSpaceCapacity, Is.Zero);
        }

        [Test]
        public void DirectDoorwayBranchRulesAndMixedConnectionLimitsAreEnforced()
        {
            var optional = ValidLayout(); SetDoorway(optional.Edges[0]); optional.Edges[0].Classification = RouteClassification.Optional; optional.Edges[0].OptionalBranchId = "branch.a";
            Assert.That(FloorLayoutValidator.Validate(optional, Configuration(branches: 1), Definitions(), CorridorDefinitions(), Limits()).Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.OptionalEdgeMissingBranchId), Is.False);
            var missingBranch = ValidLayout(); SetDoorway(missingBranch.Edges[0]); missingBranch.Edges[0].Classification = RouteClassification.Optional;
            AssertReason(missingBranch, Configuration(branches: 1), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.OptionalEdgeMissingBranchId);
            var requiredBranch = ValidLayout(); SetDoorway(requiredBranch.Edges[0]); requiredBranch.Edges[0].OptionalBranchId = "branch.a";
            AssertReason(requiredBranch, Configuration(branches: 1), Definitions(), CorridorDefinitions(), FloorLayoutValidationReason.RequiredEdgeHasBranchId);

            var mixed = ValidLayout(); SetDoorway(mixed.Edges[0]);
            Assert.That(FloorLayoutValidator.Validate(mixed, Configuration(), Definitions(connections: 1), CorridorDefinitions(), Limits()).Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.ConnectionLimitExceeded && issue.SubjectId == "node.room.0"), Is.True);
            mixed.Edges[0].Footprint = new ResolvedTileFootprint();
            Assert.That(FloorLayoutValidator.Validate(mixed, Configuration(), Definitions(connections: 1), CorridorDefinitions(), Limits()).Issues.Any(issue => issue.Reason == FloorLayoutValidationReason.ConnectionLimitExceeded && issue.SubjectId == "node.room.0"), Is.False);
        }

        [Test]
        public void ReasonCodeValuesRemainStableAndAppendExactlyFortyThroughFortyFive()
        {
            int[] values = Enum.GetValues(typeof(FloorLayoutValidationReason)).Cast<int>().ToArray();
            CollectionAssert.AreEqual(Enumerable.Range(1, 45), values);
            Assert.That((int)FloorLayoutValidationReason.InvalidFloorBounds, Is.EqualTo(40));
            Assert.That((int)FloorLayoutValidationReason.StructureTileOutsideFloorBounds, Is.EqualTo(41));
            Assert.That((int)FloorLayoutValidationReason.FinalCapacityExceedsFloorBounds, Is.EqualTo(42));
            Assert.That((int)FloorLayoutValidationReason.InvalidConnectionKind, Is.EqualTo(43));
            Assert.That((int)FloorLayoutValidationReason.DirectDoorwayHasCorridorDefinition, Is.EqualTo(44));
            Assert.That((int)FloorLayoutValidationReason.DirectDoorwayHasFootprint, Is.EqualTo(45));
        }

        private static RoomSpatialInstance Room(string id, int x) => new RoomSpatialInstance { RoomInstanceId = id, RoomDefinitionId = RoomDefinitionId, FloorId = FloorId, Anchor = new TileCoordinate(x, 0), Orientation = CardinalOrientation.Zero };
        private static FloorRouteNode Node(string id, FloorRouteNodeKind kind, string room = null) => new FloorRouteNode { NodeId = id, FloorId = FloorId, Kind = kind, RoomInstanceId = room };
        private static FloorRouteEdge Edge(string id, string source, string destination, int x) => new FloorRouteEdge { EdgeId = id, CorridorDefinitionId = CorridorDefinitionId, FloorId = FloorId, SourceNodeId = source, DestinationNodeId = destination, Footprint = new ResolvedTileFootprint(new[] { new TileCoordinate(x, 2), new TileCoordinate(x + 1, 2) }), Classification = RouteClassification.Required, ConnectionKind = FloorRouteConnectionKind.PhysicalCorridor };
        private static void SetDoorway(FloorRouteEdge edge) { edge.ConnectionKind = FloorRouteConnectionKind.DirectDoorway; edge.CorridorDefinitionId = null; edge.Footprint = null; }
        private static void AssertCapacityEqual(FloorCapacitySummary expected, FloorCapacitySummary actual) { Assert.That(actual.FinalFloorSpaceCapacity, Is.EqualTo(expected.FinalFloorSpaceCapacity)); Assert.That(actual.UsedFloorSpaceCapacity, Is.EqualTo(expected.UsedFloorSpaceCapacity)); Assert.That(actual.RemainingFloorSpaceCapacity, Is.EqualTo(expected.RemainingFloorSpaceCapacity)); }
        private static void AssertReason(FloorSpatialLayout layout, FloorSpatialConfiguration config, RoomSpatialDefinition[] rooms, CorridorSpatialDefinition[] corridors, FloorLayoutValidationReason reason) => Assert.That(FloorLayoutValidator.Validate(layout, config, rooms, corridors, Limits()).Issues.Any(x => x.Reason == reason), Is.True);
        private static string[] IssueKeys(FloorSpatialLayout layout, RoomSpatialDefinition[] rooms, CorridorSpatialDefinition[] corridors) => FloorLayoutValidator.Validate(layout, Configuration(30), rooms, corridors, Limits()).Issues.Select(Key).ToArray();
        private static string[] EndpointIssueKeys(FloorSpatialLayout layout, FloorLayoutValidationReason endpointReason) => FloorLayoutValidator.Validate(layout, Configuration(), Definitions(), CorridorDefinitions(), Limits()).Issues
            .Where(x => x.SubjectId == "edge.0" && (x.Reason == FloorLayoutValidationReason.MissingStableId || x.Reason == endpointReason)).Select(Key).ToArray();
        private static string[] OverlapIssueKeys(FloorSpatialLayout layout) => FloorLayoutValidator.Validate(layout, Configuration(30), Definitions(), CorridorDefinitions(), Limits()).Issues
            .Where(x => x.Reason == FloorLayoutValidationReason.FootprintOverlap).Select(Key).ToArray();
        private static string[] OwnedMalformedNodeIssueKeys(FloorSpatialLayout layout) => FloorLayoutValidator.Validate(layout, Configuration(30), Definitions(), CorridorDefinitions(), Limits()).Issues
            .Where(x => (x.Reason == FloorLayoutValidationReason.InvalidNodeKind && x.SubjectId == "node.room.0") ||
                (x.Reason == FloorLayoutValidationReason.MissingDestinationNode && x.SubjectId == "edge.0") ||
                (x.Reason == FloorLayoutValidationReason.MissingSourceNode && x.SubjectId == "edge.1") ||
                (x.Reason == FloorLayoutValidationReason.UnreachableRoom && x.SubjectId == "node.room.1")).Select(Key).ToArray();
        private static string Key(FloorLayoutValidationIssue issue) => $"{(int)issue.Reason}|{issue.SubjectId}|{issue.RelatedId}|{issue.HasCoordinate}|{issue.Coordinate.X}|{issue.Coordinate.Y}";
    }
}
#endif
