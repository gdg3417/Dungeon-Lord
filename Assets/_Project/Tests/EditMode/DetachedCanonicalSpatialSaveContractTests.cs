#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests
{
    public sealed class DetachedCanonicalSpatialSaveContractTests
    {
        private static CanonicalSpatialSaveWorkloadLimits Limits(int records = 200, int tiles = 200) =>
            new CanonicalSpatialSaveWorkloadLimits(records, tiles);

        [Test]
        public void EmptyNativeState_NormalizesNullFloorsOnlyOnDetachedCopyAndRoundTrips()
        {
            var source = new DetachedCanonicalSpatialSaveState
            {
                Authority = NativeMarker(), Floors = null
            };
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(source);
            Assert.That(source.Floors, Is.Null);
            Assert.That(canonical.Floors, Is.Empty);
            Assert.That(Validate(canonical, true).IsValid, Is.True);
            AssertStableRoundTrip(canonical);
        }

        [Test]
        public void TwoFloors_CanonicalizeByIndexThenOrdinalId()
        {
            SavedSpatialFloor laterId = Floor("floor.b", 1, "room.b");
            SavedSpatialFloor earlierId = Floor("floor.a", 1, "room.a");
            SavedSpatialFloor firstIndex = Floor("floor.z", 0, "room.z");
            var source = State(laterId, firstIndex, earlierId);
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(source);
            CollectionAssert.AreEqual(new[] { "floor.z", "floor.a", "floor.b" }, canonical.Floors.Select(x => x.FloorInstanceId));
        }

        [Test]
        public void EveryNestedCollection_UsesRequiredCanonicalOrdering_SourceAndDestinationPrecedeEdgeId()
        {
            SavedSpatialFloor floor = Floor("floor.a", 0, "room.b", "room.a");
            Array.Reverse(floor.Layout.Rooms); Array.Reverse(floor.Layout.Nodes); Array.Reverse(floor.Layout.Edges);
            Array.Reverse(floor.FixedStructures); Array.Reverse(floor.RoomContents.Assignments);
            Array.Reverse(floor.RoomContents.RoomSemantics);
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(State(floor));
            floor = canonical.Floors[0];
            CollectionAssert.AreEqual(new[] { "room.a", "room.b" }, floor.Layout.Rooms.Select(x => x.RoomInstanceId));
            CollectionAssert.AreEqual(new[] { FloorId(floor) + ".node.entrance", "room.a.node", "room.b.node", FloorId(floor) + ".node.completion" }, floor.Layout.Nodes.Select(x => x.NodeId));
            CollectionAssert.AreEqual(new[] { FloorId(floor) + ".edge.00", FloorId(floor) + ".edge.02", FloorId(floor) + ".edge.01" }, floor.Layout.Edges.Select(x => x.EdgeId));
            CollectionAssert.AreEqual(new[] { FloorId(floor) + ".node.entrance", "room.a.node", "room.b.node" },
                floor.Layout.Edges.Select(x => x.SourceNodeId));
            CollectionAssert.AreEqual(new[] { FloorId(floor) + ".fixed.completion", FloorId(floor) + ".fixed.entrance" }, floor.FixedStructures.Select(x => x.FixedStructureInstanceId));
            CollectionAssert.AreEqual(new[] { CanonicalSpatialSaveContracts.MonsterCategoryId, CanonicalSpatialSaveContracts.TrapCategoryId, CanonicalSpatialSaveContracts.LootNodeCategoryId }, floor.RoomContents.Assignments.Where(x => x.RoomInstanceId == "room.a").Select(x => x.CategoryId));
            CollectionAssert.AreEqual(new[] { "room.a", "room.b" }, floor.RoomContents.RoomSemantics.Select(x => x.RoomInstanceId));
        }

        [Test]
        public void NonCanonicalOrderingIsDetectedAndCanonicalCopyPasses()
        {
            DetachedCanonicalSpatialSaveState source = State(Floor("floor.b", 1, "room.b"), Floor("floor.a", 0, "room.a"));
            Array.Reverse(source.Floors[0].FixedStructures);
            Assert.That(Validate(source, true).Issues, Does.Contain(CanonicalSpatialSaveValidationIssue.NonCanonicalOrdering));
            Assert.That(Validate(Canonicalize(source), true).Issues.Contains(
                CanonicalSpatialSaveValidationIssue.NonCanonicalOrdering), Is.False);
        }

        [TestCase("room")]
        [TestCase("node")]
        [TestCase("edge")]
        [TestCase("fixed")]
        [TestCase("assignment")]
        public void CandidateWideSameTypeCollisionsAcrossFloors_AreRejected(string kind)
        {
            SavedSpatialFloor first = Floor("floor.a", 0, "room.a");
            SavedSpatialFloor second = Floor("floor.b", 1, "room.b");
            if (kind == "room") second.Layout.Rooms[0].RoomInstanceId = first.Layout.Rooms[0].RoomInstanceId;
            if (kind == "node") second.Layout.Nodes[0].NodeId = first.Layout.Nodes[0].NodeId;
            if (kind == "edge") second.Layout.Edges[0].EdgeId = first.Layout.Edges[0].EdgeId;
            if (kind == "fixed") second.FixedStructures[0].FixedStructureInstanceId = first.FixedStructures[0].FixedStructureInstanceId;
            if (kind == "assignment") second.RoomContents.Assignments[0].AssignmentId = first.RoomContents.Assignments[0].AssignmentId;
            AssertIssue(State(first, second), CanonicalSpatialSaveValidationIssue.CandidateInstanceIdCollision);
        }

        [TestCase("floor-room")]
        [TestCase("room-node")]
        [TestCase("node-edge")]
        [TestCase("edge-fixed")]
        [TestCase("fixed-assignment")]
        public void CandidateWideCrossTypeIdentityCollisions_AreRejected(string kind)
        {
            SavedSpatialFloor floor = Floor("floor.a", 0, "room.a");
            if (kind == "floor-room") floor.Layout.Rooms[0].RoomInstanceId = floor.FloorInstanceId;
            if (kind == "room-node") floor.Layout.Nodes[0].NodeId = floor.Layout.Rooms[0].RoomInstanceId;
            if (kind == "node-edge") floor.Layout.Edges[0].EdgeId = floor.Layout.Nodes[0].NodeId;
            if (kind == "edge-fixed") floor.FixedStructures[0].FixedStructureInstanceId = floor.Layout.Edges[0].EdgeId;
            if (kind == "fixed-assignment") floor.RoomContents.Assignments[0].AssignmentId = floor.FixedStructures[0].FixedStructureInstanceId;
            AssertIssue(State(floor), CanonicalSpatialSaveValidationIssue.CandidateInstanceIdCollision);
        }

        [Test]
        public void DuplicateFloorIdAndIndex_HaveSeparateIssues()
        {
            SavedSpatialFloor first = Floor("floor.a", 0, "room.a");
            SavedSpatialFloor duplicate = Floor("floor.a", 0, "room.b");
            CanonicalSpatialSaveValidationResult result = Validate(State(first, duplicate));
            Assert.That(result.Issues, Does.Contain(CanonicalSpatialSaveValidationIssue.DuplicateFloorIndex));
            Assert.That(result.Issues, Does.Contain(CanonicalSpatialSaveValidationIssue.CandidateInstanceIdCollision));
        }

        [TestCase("floors", CanonicalSpatialSaveValidationIssue.NullFloorRecord)]
        [TestCase("rooms", CanonicalSpatialSaveValidationIssue.NullRoomRecord)]
        [TestCase("nodes", CanonicalSpatialSaveValidationIssue.NullNodeRecord)]
        [TestCase("edges", CanonicalSpatialSaveValidationIssue.NullEdgeRecord)]
        [TestCase("fixed", CanonicalSpatialSaveValidationIssue.NullFixedStructureRecord)]
        [TestCase("assignments", CanonicalSpatialSaveValidationIssue.NullAssignmentRecord)]
        [TestCase("semantics", CanonicalSpatialSaveValidationIssue.NullRoomSemanticsRecord)]
        public void NullSerializedRecords_AreRetainedAndRejected(string collection,
            CanonicalSpatialSaveValidationIssue expected)
        {
            DetachedCanonicalSpatialSaveState source = State(Floor("floor.a", 0, "room.a"));
            SavedSpatialFloor floor = source.Floors[0];
            if (collection == "floors") source.Floors = new[] { floor, null };
            if (collection == "rooms") floor.Layout.Rooms = new[] { floor.Layout.Rooms[0], null };
            if (collection == "nodes") floor.Layout.Nodes = new[] { floor.Layout.Nodes[0], null };
            if (collection == "edges") floor.Layout.Edges = new[] { floor.Layout.Edges[0], null };
            if (collection == "fixed") floor.FixedStructures = new[] { floor.FixedStructures[0], null };
            if (collection == "assignments") floor.RoomContents.Assignments = new[] { floor.RoomContents.Assignments[0], null };
            if (collection == "semantics") floor.RoomContents.RoomSemantics = new[] { floor.RoomContents.RoomSemantics[0], null };
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(source);
            AssertIssue(canonical, expected);
            Assert.That(ContainsNull(canonical, collection), Is.True);
        }

        [Test]
        public void NullCollections_NormalizeOnlyOnDetachedCopy()
        {
            DetachedCanonicalSpatialSaveState source = State(Floor("floor.a", 0));
            SavedSpatialFloor floor = source.Floors[0];
            floor.Layout.Rooms = null; floor.Layout.Nodes = null; floor.Layout.Edges = null;
            floor.FixedStructures = null; floor.RoomContents.Assignments = null; floor.RoomContents.RoomSemantics = null;
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(source);
            Assert.That(floor.Layout.Rooms, Is.Null); Assert.That(floor.FixedStructures, Is.Null);
            Assert.That(floor.RoomContents.Assignments, Is.Null);
            Assert.That(canonical.Floors[0].Layout.Rooms, Is.Empty); Assert.That(canonical.Floors[0].Layout.Nodes, Is.Empty);
            Assert.That(canonical.Floors[0].Layout.Edges, Is.Empty); Assert.That(canonical.Floors[0].FixedStructures, Is.Empty);
            Assert.That(canonical.Floors[0].RoomContents.Assignments, Is.Empty); Assert.That(canonical.Floors[0].RoomContents.RoomSemantics, Is.Empty);
        }

        [TestCase("source", CanonicalSpatialSaveValidationIssue.UnknownEdgeSource)]
        [TestCase("destination", CanonicalSpatialSaveValidationIssue.UnknownEdgeDestination)]
        public void DanglingEdgeEndpoints_AreRejected(string endpoint, CanonicalSpatialSaveValidationIssue issue)
        {
            DetachedCanonicalSpatialSaveState state = State(Floor("floor.a", 0, "room.a"));
            if (endpoint == "source") state.Floors[0].Layout.Edges[0].SourceNodeId = "node.unknown";
            else state.Floors[0].Layout.Edges[0].DestinationNodeId = "node.unknown";
            AssertIssue(state, issue);
        }

        [Test]
        public void RoomNodeReferencingUnknownRoom_IsRejected()
        {
            DetachedCanonicalSpatialSaveState state = State(Floor("floor.a", 0, "room.a"));
            state.Floors[0].Layout.Nodes.Single(x => x.Kind == FloorRouteNodeKind.Room).RoomInstanceId = "room.unknown";
            AssertIssue(state, CanonicalSpatialSaveValidationIssue.UnknownRoomReference);
        }

        [TestCase("floor", CanonicalSpatialSaveValidationIssue.NegativeFloorIndex)]
        [TestCase("room-orientation", CanonicalSpatialSaveValidationIssue.InvalidRoomOrientation)]
        [TestCase("node-kind", CanonicalSpatialSaveValidationIssue.InvalidNodeKind)]
        [TestCase("edge-kind", CanonicalSpatialSaveValidationIssue.InvalidEdgeConnectionKind)]
        [TestCase("classification", CanonicalSpatialSaveValidationIssue.InvalidEdgeClassification)]
        [TestCase("fixed-orientation", CanonicalSpatialSaveValidationIssue.InvalidFixedStructureOrientation)]
        [TestCase("fixed-kind", CanonicalSpatialSaveValidationIssue.InvalidFixedStructureKind)]
        [TestCase("creation", CanonicalSpatialSaveValidationIssue.InvalidCreationKind)]
        [TestCase("origin", CanonicalSpatialSaveValidationIssue.InvalidRoomOriginKind)]
        public void InvalidScalarEvidence_IsPreservedAndRejected(string target, CanonicalSpatialSaveValidationIssue issue)
        {
            DetachedCanonicalSpatialSaveState source = State(Floor("floor.a", 0, "room.a"));
            SavedSpatialFloor floor = source.Floors[0];
            if (target == "floor") floor.FloorIndex = -1;
            if (target == "room-orientation") floor.Layout.Rooms[0].Orientation = (CardinalOrientation)99;
            if (target == "node-kind") floor.Layout.Nodes[0].Kind = (FloorRouteNodeKind)99;
            if (target == "edge-kind") floor.Layout.Edges[0].ConnectionKind = (FloorRouteConnectionKind)99;
            if (target == "classification") floor.Layout.Edges[0].Classification = (RouteClassification)99;
            if (target == "fixed-orientation") floor.FixedStructures[0].Orientation = (CardinalOrientation)99;
            if (target == "fixed-kind") floor.FixedStructures[0].Kind = (FixedSpatialStructureKind)99;
            if (target == "creation") source.Authority.CreationKind = (CanonicalSpatialCreationKind)99;
            if (target == "origin") floor.RoomContents.RoomSemantics[0].LegacyRoomOriginKind = (LegacyRoomOriginKind)99;
            DetachedCanonicalSpatialSaveState copy = Canonicalize(source);
            AssertIssue(copy, issue);
            Assert.That(JsonUtility.ToJson(copy), Does.Contain("99"));
        }

        [TestCase("layout")]
        [TestCase("room")]
        [TestCase("node")]
        [TestCase("edge")]
        [TestCase("fixed")]
        [TestCase("assignment")]
        public void EveryOwnedFloorReferenceMustMatch(string target)
        {
            DetachedCanonicalSpatialSaveState state = State(Floor("floor.a", 0, "room.a"));
            SavedSpatialFloor floor = state.Floors[0]; const string wrong = "floor.wrong";
            if (target == "layout") floor.Layout.FloorId = wrong;
            if (target == "room") floor.Layout.Rooms[0].FloorId = wrong;
            if (target == "node") floor.Layout.Nodes[0].FloorId = wrong;
            if (target == "edge") floor.Layout.Edges[0].FloorId = wrong;
            if (target == "fixed") floor.FixedStructures[0].FloorInstanceId = wrong;
            if (target == "assignment") floor.RoomContents.Assignments[0].RoomInstanceId = "room.wrong";
            AssertIssue(state, CanonicalSpatialSaveValidationIssue.FloorReferenceMismatch);
        }

        [TestCase("doorway-corridor")]
        [TestCase("doorway-footprint")]
        [TestCase("required-branch")]
        [TestCase("optional-no-branch")]
        public void EdgeStructuralShape_IsValidated(string target)
        {
            DetachedCanonicalSpatialSaveState state = State(Floor("floor.a", 0, "room.a"));
            FloorRouteEdge edge = state.Floors[0].Layout.Edges[0];
            if (target == "doorway-corridor") edge.CorridorDefinitionId = "corridor.a";
            if (target == "doorway-footprint") edge.Footprint = new ResolvedTileFootprint { OccupiedTiles = Array.Empty<TileCoordinate>() };
            if (target == "required-branch") edge.OptionalBranchId = "branch.a";
            if (target == "optional-no-branch") { edge.Classification = RouteClassification.Optional; edge.OptionalBranchId = null; }
            AssertIssue(state, target.StartsWith("doorway") ? CanonicalSpatialSaveValidationIssue.InvalidDirectDoorwayShape : CanonicalSpatialSaveValidationIssue.InvalidEdgeBranchShape);
        }

        [Test]
        public void AssignmentAndSemanticsRulesRemainStrict()
        {
            DetachedCanonicalSpatialSaveState state = State(Floor("floor.a", 0, "room.a"));
            FloorRoomContentState contents = state.Floors[0].RoomContents;
            contents.Assignments[0].CategoryId = "placement.category.room";
            contents.Assignments[0].Sequence = -1;
            contents.NextSequence = -1;
            contents.RoomSemantics = Array.Empty<CanonicalRoomSemantics>();
            CanonicalSpatialSaveValidationResult result = Validate(state);
            Assert.That(result.Issues, Does.Contain(CanonicalSpatialSaveValidationIssue.InvalidContentCategory));
            Assert.That(result.Issues, Does.Contain(CanonicalSpatialSaveValidationIssue.NegativeSequence));
            Assert.That(result.Issues, Does.Contain(CanonicalSpatialSaveValidationIssue.InvalidNextSequence));
            Assert.That(result.Issues, Does.Contain(CanonicalSpatialSaveValidationIssue.MissingRoomSemantics));
        }

        [Test]
        public void DuplicateRoomCategorySequence_IsRejectedAndSequenceGapsAreNeverRenumbered()
        {
            DetachedCanonicalSpatialSaveState state = State(Floor("floor.a", 0, "room.a"));
            RoomContentAssignment original = state.Floors[0].RoomContents.Assignments[0];
            state.Floors[0].RoomContents.Assignments = state.Floors[0].RoomContents.Assignments.Concat(new[]
            {
                new RoomContentAssignment { AssignmentId = "room.a.content.other", RoomInstanceId = original.RoomInstanceId,
                    CategoryId = original.CategoryId, OptionId = "option.other", Sequence = original.Sequence }
            }).ToArray();
            AssertIssue(state, CanonicalSpatialSaveValidationIssue.DuplicateRoomCategorySequence);

            state.Floors[0].RoomContents.Assignments = state.Floors[0].RoomContents.Assignments.Take(3).ToArray();
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(state);
            CollectionAssert.AreEqual(new long[] { 1, 3, 7 }, canonical.Floors[0].RoomContents.Assignments.Select(x => x.Sequence));
            Assert.That(canonical.Floors[0].RoomContents.NextSequence, Is.EqualTo(8));
        }

        [TestCase(LegacyRoomOriginKind.MigratedExplicitLegacyRoom)]
        [TestCase(LegacyRoomOriginKind.ImplicitCompatibilityContainer)]
        [TestCase(LegacyRoomOriginKind.CanonicalPlayerPlaced)]
        public void ApprovedRoomOrigins_RoundTrip(LegacyRoomOriginKind origin)
        {
            DetachedCanonicalSpatialSaveState state = State(Floor("floor.a", 0, "room.a"));
            state.Floors[0].RoomContents.RoomSemantics[0].LegacyRoomOriginKind = origin;
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(state);
            AssertStableRoundTrip(canonical);
            Assert.That(JsonUtility.FromJson<DetachedCanonicalSpatialSaveState>(JsonUtility.ToJson(canonical))
                .Floors[0].RoomContents.RoomSemantics[0].LegacyRoomOriginKind, Is.EqualTo(origin));
        }

        [Test]
        public void MigratedMarkerAuditIdentity_RoundTripsWithoutBecomingAnInstanceIdentity()
        {
            DetachedCanonicalSpatialSaveState state = State(Floor("floor.a", 0, "room.a"));
            state.Authority.CreationKind = CanonicalSpatialCreationKind.Migrated;
            state.Authority.MigrationTransactionId = state.Floors[0].FloorInstanceId;
            state.Authority.MigrationDescriptorFingerprint = state.Floors[0].Layout.Rooms[0].RoomInstanceId;
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(state);
            Assert.That(Validate(canonical, true).IsValid, Is.True);
            AssertStableRoundTrip(canonical);
        }

        [TestCase("transaction")]
        [TestCase("fingerprint")]
        public void MalformedMigratedAuditIdentifiers_AreRejected(string field)
        {
            DetachedCanonicalSpatialSaveState state = State(Floor("floor.a", 0, "room.a"));
            state.Authority.CreationKind = CanonicalSpatialCreationKind.Migrated;
            state.Authority.MigrationTransactionId = field == "transaction" ? "Bad Transaction" : "transaction.01";
            state.Authority.MigrationDescriptorFingerprint = field == "fingerprint" ? "Bad Fingerprint" : "fingerprint.01";
            AssertIssue(state, CanonicalSpatialSaveValidationIssue.MalformedPersistentId);
        }

        [Test]
        public void ValidAbsenceNullAndEmpty_CanonicalizeToIdenticalUnityJson()
        {
            DetachedCanonicalSpatialSaveState nullState = State(Floor("floor.a", 0, "room.a"));
            DetachedCanonicalSpatialSaveState emptyState = State(Floor("floor.a", 0, "room.a"));
            FloorRouteNode nullEntrance = nullState.Floors[0].Layout.Nodes.Single(x => x.Kind == FloorRouteNodeKind.Entrance);
            FloorRouteNode emptyEntrance = emptyState.Floors[0].Layout.Nodes.Single(x => x.Kind == FloorRouteNodeKind.Entrance);
            FloorRouteEdge nullEdge = nullState.Floors[0].Layout.Edges[0];
            FloorRouteEdge emptyEdge = emptyState.Floors[0].Layout.Edges[0];
            nullEntrance.RoomInstanceId = null; emptyEntrance.RoomInstanceId = string.Empty;
            nullEdge.CorridorDefinitionId = null; emptyEdge.CorridorDefinitionId = string.Empty;
            nullEdge.OptionalBranchId = null; emptyEdge.OptionalBranchId = string.Empty;

            string nullJson = JsonUtility.ToJson(Canonicalize(nullState));
            string emptyJson = JsonUtility.ToJson(Canonicalize(emptyState));
            Assert.That(nullJson, Is.EqualTo(emptyJson));
        }

        [Test]
        public void MalformedNonemptyOptionalValues_ArePreservedAndRejected()
        {
            DetachedCanonicalSpatialSaveState state = State(Floor("floor.a", 0, "room.a"));
            FloorRouteEdge edge = state.Floors[0].Layout.Edges[0];
            edge.CorridorDefinitionId = " "; edge.OptionalBranchId = "Bad Branch";
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(state);
            FloorRouteEdge copied = canonical.Floors[0].Layout.Edges.Single(x => x.EdgeId == edge.EdgeId);
            Assert.That(copied.CorridorDefinitionId, Is.EqualTo(" "));
            Assert.That(copied.OptionalBranchId, Is.EqualTo("Bad Branch"));
            Assert.That(Validate(canonical).Issues, Does.Contain(CanonicalSpatialSaveValidationIssue.MalformedPersistentId));
            Assert.That(Validate(canonical).Issues, Does.Contain(CanonicalSpatialSaveValidationIssue.InvalidDirectDoorwayShape));
            Assert.That(Validate(canonical).Issues, Does.Contain(CanonicalSpatialSaveValidationIssue.InvalidEdgeBranchShape));
        }

        [TestCase(FloorRouteNodeKind.Entrance)]
        [TestCase(FloorRouteNodeKind.Exit)]
        [TestCase(FloorRouteNodeKind.Descent)]
        [TestCase(FloorRouteNodeKind.Completion)]
        public void NonRoomNodesRejectNonemptyRoomReferences(FloorRouteNodeKind kind)
        {
            DetachedCanonicalSpatialSaveState state = State(Floor("floor.a", 0, "room.a"));
            FloorRouteNode node = state.Floors[0].Layout.Nodes.First();
            node.Kind = kind; node.RoomInstanceId = "room.a";
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(state);
            Assert.That(canonical.Floors[0].Layout.Nodes.Single(x => x.NodeId == node.NodeId).RoomInstanceId, Is.EqualTo("room.a"));
            AssertIssue(canonical, CanonicalSpatialSaveValidationIssue.NonRoomNodeHasRoomReference);
        }

        [TestCase("null-footprint")]
        [TestCase("null-tiles")]
        [TestCase("empty-tiles")]
        public void PhysicalCorridorRequiresNonemptyFootprint(string shape)
        {
            DetachedCanonicalSpatialSaveState state = State(Floor("floor.a", 0, "room.a"));
            FloorRouteEdge edge = state.Floors[0].Layout.Edges[0];
            edge.ConnectionKind = FloorRouteConnectionKind.PhysicalCorridor;
            edge.CorridorDefinitionId = "corridor.a";
            if (shape == "null-footprint") edge.Footprint = null;
            if (shape == "null-tiles") edge.Footprint = new ResolvedTileFootprint { OccupiedTiles = null };
            if (shape == "empty-tiles") edge.Footprint = new ResolvedTileFootprint { OccupiedTiles = Array.Empty<TileCoordinate>() };
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(state);
            if (shape == "null-tiles")
            {
                Assert.That(edge.Footprint.OccupiedTiles, Is.Null);
                Assert.That(canonical.Floors[0].Layout.Edges[0].Footprint.OccupiedTiles, Is.Empty);
            }
            AssertIssue(canonical, CanonicalSpatialSaveValidationIssue.InvalidPhysicalCorridorShape);
        }

        [Test]
        public void PhysicalCorridorWithDefinitionAndTiles_IsStructurallyValid()
        {
            DetachedCanonicalSpatialSaveState state = State(Floor("floor.a", 0, "room.a"));
            FloorRouteEdge edge = state.Floors[0].Layout.Edges[0];
            edge.ConnectionKind = FloorRouteConnectionKind.PhysicalCorridor;
            edge.CorridorDefinitionId = "corridor.a";
            edge.Footprint = new ResolvedTileFootprint { OccupiedTiles = new[] { new TileCoordinate(2, 0), new TileCoordinate(1, 0) } };
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(state);
            Assert.That(Validate(canonical).IsValid, Is.True);
            CollectionAssert.AreEqual(new[] { new TileCoordinate(1, 0), new TileCoordinate(2, 0) }, canonical.Floors[0].Layout.Edges[0].Footprint.OccupiedTiles);
        }

        [TestCase("layout-version", CanonicalSpatialSaveValidationIssue.InvalidLayoutContractVersion)]
        [TestCase("native-audit", CanonicalSpatialSaveValidationIssue.NativeMarkerHasMigrationIdentity)]
        [TestCase("malformed-id", CanonicalSpatialSaveValidationIssue.MalformedPersistentId)]
        [TestCase("missing-layout", CanonicalSpatialSaveValidationIssue.MissingLayout)]
        [TestCase("missing-contents", CanonicalSpatialSaveValidationIssue.MissingRoomContents)]
        public void OtherDetachedShapeFailuresRemainRejected(string target, CanonicalSpatialSaveValidationIssue issue)
        {
            DetachedCanonicalSpatialSaveState state = State(Floor("floor.a", 0, "room.a"));
            if (target == "layout-version") state.Authority.CanonicalLayoutContractVersion = 0;
            if (target == "native-audit") state.Authority.MigrationTransactionId = "transaction.01";
            if (target == "malformed-id") state.Floors[0].FloorDefinitionId = "Spatial Floor";
            if (target == "missing-layout") state.Floors[0].Layout = null;
            if (target == "missing-contents") state.Floors[0].RoomContents = null;
            AssertIssue(state, issue);
        }

        [TestCase("room")]
        [TestCase("node")]
        [TestCase("edge")]
        [TestCase("fixed")]
        [TestCase("assignment")]
        [TestCase("semantics")]
        public void ActualTwoFloorCrossReferences_AreRejected(string target)
        {
            SavedSpatialFloor first = Floor("floor.a", 0, "room.a");
            SavedSpatialFloor second = Floor("floor.b", 1, "room.b");
            if (target == "room") second.Layout.Rooms[0].FloorId = first.FloorInstanceId;
            if (target == "node") second.Layout.Nodes[0].FloorId = first.FloorInstanceId;
            if (target == "edge") second.Layout.Edges[0].FloorId = first.FloorInstanceId;
            if (target == "fixed") second.FixedStructures[0].FloorInstanceId = first.FloorInstanceId;
            if (target == "assignment") second.RoomContents.Assignments[0].RoomInstanceId = first.Layout.Rooms[0].RoomInstanceId;
            if (target == "semantics") second.RoomContents.RoomSemantics[0].RoomInstanceId = first.Layout.Rooms[0].RoomInstanceId;
            CanonicalSpatialSaveValidationResult result = Validate(State(first, second));
            Assert.That(result.Issues.Any(value => value == CanonicalSpatialSaveValidationIssue.FloorReferenceMismatch ||
                value == CanonicalSpatialSaveValidationIssue.UnknownRoomSemantics), Is.True);
        }

        [Test]
        public void WorkloadClassification_DistinguishesInvalidInputLimitsAndBothExhaustionKinds()
        {
            DetachedCanonicalSpatialSaveState state = State(Floor("floor.a", 0, "room.a"));
            int records = CountRecords(state);
            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(state, Limits(records), out _), Is.True);
            AssertIssue(state, CanonicalSpatialSaveValidationIssue.RecordLimitExceeded, Limits(records - 1));
            AssertIssue(state, CanonicalSpatialSaveValidationIssue.InvalidWorkloadLimits, default);
            AssertIssue(null, CanonicalSpatialSaveValidationIssue.InvalidSource, Limits());

            FloorRouteEdge edge = state.Floors[0].Layout.Edges[0];
            edge.ConnectionKind = FloorRouteConnectionKind.PhysicalCorridor;
            edge.CorridorDefinitionId = "corridor.a";
            edge.Footprint = new ResolvedTileFootprint { OccupiedTiles = new[] { new TileCoordinate(2, 0), new TileCoordinate(1, 0) } };
            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(state, Limits(records, 2), out DetachedCanonicalSpatialSaveState exact), Is.True);
            CollectionAssert.AreEqual(new[] { new TileCoordinate(1, 0), new TileCoordinate(2, 0) }, exact.Floors[0].Layout.Edges[0].Footprint.OccupiedTiles);
            AssertIssue(state, CanonicalSpatialSaveValidationIssue.MaterializedTileLimitExceeded, Limits(records, 1));
        }

        [Test]
        public void Canonicalization_IsIdempotentDetachedAndDoesNotMutateInvalidSourceEvidence()
        {
            DetachedCanonicalSpatialSaveState source = State(Floor("floor.a", 0, "room.b", "room.a"));
            RoomSpatialInstance[] sourceRooms = source.Floors[0].Layout.Rooms;
            FloorRouteEdge sourceEdge = source.Floors[0].Layout.Edges[0];
            sourceEdge.OptionalBranchId = "invalid.branch";
            string[] sourceOrder = sourceRooms.Select(x => x.RoomInstanceId).ToArray();
            DetachedCanonicalSpatialSaveState first = Canonicalize(source);
            DetachedCanonicalSpatialSaveState second = Canonicalize(first);
            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
            Assert.That(source.Floors[0].Layout.Rooms, Is.SameAs(sourceRooms));
            CollectionAssert.AreEqual(sourceOrder, sourceRooms.Select(x => x.RoomInstanceId));
            Assert.That(sourceEdge.OptionalBranchId, Is.EqualTo("invalid.branch"));
            Assert.That(first.Floors[0].Layout.Edges.Single(x => x.EdgeId == sourceEdge.EdgeId).OptionalBranchId, Is.EqualTo("invalid.branch"));
        }

        [Test]
        public void DirectDoorwayNullFootprints_SurviveUnityJsonRoundTrip()
        {
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(State(Floor("floor.a", 0, "room.a")));
            Assert.That(canonical.Floors[0].Layout.Edges.All(x => x.Footprint == null), Is.True);
            AssertStableRoundTrip(canonical);
            DetachedCanonicalSpatialSaveState restored = JsonUtility.FromJson<DetachedCanonicalSpatialSaveState>(JsonUtility.ToJson(canonical));
            Assert.That(restored.Floors[0].Layout.Edges.All(x => x.Footprint == null), Is.True);
        }

        [Test]
        public void SchemaSixAndOrdinarySaveJsonRemainWithoutCanonicalMembers()
        {
            Assert.That(SaveMigration.LatestSchemaVersion, Is.EqualTo(6));
            string json = JsonUtility.ToJson(new SaveRoot { schemaVersion = 6, primary = new SaveData() });
            Assert.That(json, Does.Not.Contain("spatialFloors"));
            Assert.That(json, Does.Not.Contain("canonicalSpatial"));
            Assert.That(json, Does.Not.Contain("CanonicalSpatialAuthority"));
        }

        private static CanonicalSpatialAuthorityMarker NativeMarker() => new CanonicalSpatialAuthorityMarker
        { CanonicalLayoutContractVersion = 1, CreationKind = CanonicalSpatialCreationKind.NativeCanonical };
        private static DetachedCanonicalSpatialSaveState State(params SavedSpatialFloor[] floors) =>
            new DetachedCanonicalSpatialSaveState { Authority = NativeMarker(), Floors = floors };

        private static SavedSpatialFloor Floor(string floorId, int index, params string[] roomIds)
        {
            RoomSpatialInstance[] rooms = roomIds.Select((roomId, roomIndex) => new RoomSpatialInstance
            { RoomInstanceId = roomId, RoomDefinitionId = "spatial.room.basic", FloorId = floorId,
                Anchor = new TileCoordinate(0, 2 + roomIndex * 4), Orientation = CardinalOrientation.Zero }).ToArray();
            FloorRouteNode entrance = new FloorRouteNode { NodeId = floorId + ".node.entrance", FloorId = floorId, Kind = FloorRouteNodeKind.Entrance };
            FloorRouteNode completion = new FloorRouteNode { NodeId = floorId + ".node.completion", FloorId = floorId, Kind = FloorRouteNodeKind.Completion };
            FloorRouteNode[] roomNodes = roomIds.Select(roomId => new FloorRouteNode
            { NodeId = roomId + ".node", FloorId = floorId, Kind = FloorRouteNodeKind.Room, RoomInstanceId = roomId }).ToArray();
            FloorRouteNode[] nodes = new[] { entrance }.Concat(roomNodes).Concat(new[] { completion }).ToArray();
            FloorRouteEdge[] edges = Enumerable.Range(0, nodes.Length - 1).Select(edgeIndex => new FloorRouteEdge
            { EdgeId = floorId + ".edge.0" + edgeIndex, FloorId = floorId, SourceNodeId = nodes[edgeIndex].NodeId,
                DestinationNodeId = nodes[edgeIndex + 1].NodeId, Classification = RouteClassification.Required,
                ConnectionKind = FloorRouteConnectionKind.DirectDoorway }).ToArray();
            var assignments = new List<RoomContentAssignment>();
            foreach (string roomId in roomIds)
            {
                assignments.Add(Assignment(roomId, "loot", CanonicalSpatialSaveContracts.LootNodeCategoryId, 7));
                assignments.Add(Assignment(roomId, "trap", CanonicalSpatialSaveContracts.TrapCategoryId, 3));
                assignments.Add(Assignment(roomId, "monster", CanonicalSpatialSaveContracts.MonsterCategoryId, 1));
            }
            return new SavedSpatialFloor
            {
                FloorInstanceId = floorId, FloorDefinitionId = "spatial.floor.01", FloorIndex = index,
                Layout = new FloorSpatialLayout { FloorId = floorId, Rooms = rooms, Nodes = nodes, Edges = edges },
                FixedStructures = new[]
                {
                    new SavedFixedSpatialStructure { FixedStructureInstanceId = floorId + ".fixed.entrance", FixedStructureDefinitionId = "spatial.fixed.entrance_hall", FloorInstanceId = floorId, Kind = FixedSpatialStructureKind.Entrance },
                    new SavedFixedSpatialStructure { FixedStructureInstanceId = floorId + ".fixed.completion", FixedStructureDefinitionId = "spatial.fixed.completion_terminal", FloorInstanceId = floorId, Kind = FixedSpatialStructureKind.CompletionTerminal }
                },
                RoomContents = new FloorRoomContentState
                {
                    Assignments = assignments.ToArray(), NextSequence = roomIds.Length == 0 ? 0 : 8,
                    RoomSemantics = roomIds.Select(roomId => new CanonicalRoomSemantics
                    { RoomInstanceId = roomId, LegacyRoomOriginKind = LegacyRoomOriginKind.MigratedExplicitLegacyRoom }).ToArray()
                }
            };
        }

        private static RoomContentAssignment Assignment(string roomId, string suffix, string category, long sequence) =>
            new RoomContentAssignment { AssignmentId = roomId + ".content." + suffix, RoomInstanceId = roomId,
                CategoryId = category, OptionId = "option." + suffix, Sequence = sequence };
        private static string FloorId(SavedSpatialFloor floor) => floor.FloorInstanceId;
        private static int CountRecords(DetachedCanonicalSpatialSaveState state) => state.Floors.Length + state.Floors.Sum(floor =>
            floor.Layout.Rooms.Length + floor.Layout.Nodes.Length + floor.Layout.Edges.Length + floor.FixedStructures.Length +
            floor.RoomContents.Assignments.Length + floor.RoomContents.RoomSemantics.Length);
        private static CanonicalSpatialSaveValidationResult Validate(DetachedCanonicalSpatialSaveState state, bool canonical = false) =>
            CanonicalSpatialSaveContracts.Validate(state, Limits(), canonical);
        private static void AssertIssue(DetachedCanonicalSpatialSaveState state, CanonicalSpatialSaveValidationIssue issue) =>
            Assert.That(Validate(state).Issues, Does.Contain(issue));
        private static void AssertIssue(DetachedCanonicalSpatialSaveState state, CanonicalSpatialSaveValidationIssue issue,
            CanonicalSpatialSaveWorkloadLimits limits) => Assert.That(CanonicalSpatialSaveContracts.Validate(state, limits).Issues, Does.Contain(issue));
        private static DetachedCanonicalSpatialSaveState Canonicalize(DetachedCanonicalSpatialSaveState state)
        { Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(state, Limits(), out DetachedCanonicalSpatialSaveState result), Is.True); return result; }
        private static void AssertStableRoundTrip(DetachedCanonicalSpatialSaveState canonical)
        {
            string json = JsonUtility.ToJson(canonical);
            DetachedCanonicalSpatialSaveState restored = JsonUtility.FromJson<DetachedCanonicalSpatialSaveState>(json);
            Assert.That(JsonUtility.ToJson(Canonicalize(restored)), Is.EqualTo(json));
        }
        private static bool ContainsNull(DetachedCanonicalSpatialSaveState state, string collection)
        {
            if (collection == "floors") return state.Floors.Any(x => x == null);
            SavedSpatialFloor floor = state.Floors.First(x => x != null);
            if (collection == "rooms") return floor.Layout.Rooms.Any(x => x == null);
            if (collection == "nodes") return floor.Layout.Nodes.Any(x => x == null);
            if (collection == "edges") return floor.Layout.Edges.Any(x => x == null);
            if (collection == "fixed") return floor.FixedStructures.Any(x => x == null);
            if (collection == "assignments") return floor.RoomContents.Assignments.Any(x => x == null);
            return floor.RoomContents.RoomSemantics.Any(x => x == null);
        }
    }
}
#endif
