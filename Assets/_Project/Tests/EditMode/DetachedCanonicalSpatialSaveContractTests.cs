#if UNITY_EDITOR
using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests
{
    public sealed class DetachedCanonicalSpatialSaveContractTests
    {
        private static CanonicalSpatialSaveWorkloadLimits Limits(int records = 100, int tiles = 100) =>
            new CanonicalSpatialSaveWorkloadLimits(records, tiles);

        [Test]
        public void EmptyNativeState_CanonicalizesAndRoundTrips()
        {
            var source = new DetachedCanonicalSpatialSaveState
            {
                Authority = new CanonicalSpatialAuthorityMarker
                {
                    CanonicalLayoutContractVersion = 1,
                    CreationKind = CanonicalSpatialCreationKind.NativeCanonical
                },
                Floors = null
            };

            DetachedCanonicalSpatialSaveState canonical = Canonicalize(source);
            Assert.That(canonical.Floors, Is.Empty);
            Assert.That(source.Floors, Is.Null);
            Assert.That(CanonicalSpatialSaveContracts.Validate(canonical, Limits(), true).IsValid, Is.True);
            AssertStableUnityRoundTrip(canonical);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void PopulatedRShapes_RoundTripFixedStructuresDoorwaysContentsAndSemantics(int roomCount)
        {
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(Populated(roomCount));
            SavedSpatialFloor floor = canonical.Floors.Single();
            CollectionAssert.AreEqual(new[] { "compat.floor.00.fixed.completion", "compat.floor.00.fixed.entrance" },
                floor.FixedStructures.Select(x => x.FixedStructureInstanceId));
            Assert.That(floor.FixedStructures.Select(x => x.Kind), Does.Contain(FixedSpatialStructureKind.Entrance));
            Assert.That(floor.FixedStructures.Select(x => x.Kind), Does.Contain(FixedSpatialStructureKind.CompletionTerminal));
            Assert.That(floor.Layout.Edges.All(x => x.Footprint == null), Is.True);
            Assert.That(CanonicalSpatialSaveContracts.Validate(canonical, Limits(), true).IsValid, Is.True);
            AssertStableUnityRoundTrip(canonical);
        }

        [TestCase(LegacyRoomOriginKind.MigratedExplicitLegacyRoom)]
        [TestCase(LegacyRoomOriginKind.ImplicitCompatibilityContainer)]
        [TestCase(LegacyRoomOriginKind.CanonicalPlayerPlaced)]
        public void EveryApprovedRoomOrigin_RoundTrips(LegacyRoomOriginKind kind)
        {
            DetachedCanonicalSpatialSaveState source = Populated(1);
            source.Floors[0].RoomContents.RoomSemantics[0].LegacyRoomOriginKind = kind;
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(source);
            Assert.That(JsonUtility.FromJson<DetachedCanonicalSpatialSaveState>(JsonUtility.ToJson(canonical))
                .Floors[0].RoomContents.RoomSemantics[0].LegacyRoomOriginKind, Is.EqualTo(kind));
        }

        [Test]
        public void Assignments_CanonicalizeByRoomCategorySequenceAndIds_WithoutRenumberingGaps()
        {
            DetachedCanonicalSpatialSaveState source = Populated(2);
            FloorRoomContentState contents = source.Floors[0].RoomContents;
            contents.Assignments = new[]
            {
                Assignment("assignment.loot.7", "room.01", CanonicalSpatialSaveContracts.LootNodeCategoryId, "option.loot.a", 7),
                Assignment("assignment.trap.3", "room.00", CanonicalSpatialSaveContracts.TrapCategoryId, "option.trap.a", 3),
                Assignment("assignment.monster.8", "room.00", CanonicalSpatialSaveContracts.MonsterCategoryId, "option.monster.b", 8),
                Assignment("assignment.monster.2", "room.00", CanonicalSpatialSaveContracts.MonsterCategoryId, "option.monster.a", 2)
            };
            contents.NextSequence = 9;

            DetachedCanonicalSpatialSaveState canonical = Canonicalize(source);
            CollectionAssert.AreEqual(new long[] { 2, 8, 3, 7 }, canonical.Floors[0].RoomContents.Assignments.Select(x => x.Sequence));
            CollectionAssert.AreEqual(new[] { CanonicalSpatialSaveContracts.MonsterCategoryId, CanonicalSpatialSaveContracts.MonsterCategoryId,
                CanonicalSpatialSaveContracts.TrapCategoryId, CanonicalSpatialSaveContracts.LootNodeCategoryId },
                canonical.Floors[0].RoomContents.Assignments.Select(x => x.CategoryId));
            Assert.That(canonical.Floors[0].RoomContents.NextSequence, Is.EqualTo(9));
            AssertStableUnityRoundTrip(canonical);
        }

        [Test]
        public void PermutationsAndRepeatedCanonicalization_ProduceIdenticalDetachedJsonWithoutSourceMutation()
        {
            DetachedCanonicalSpatialSaveState source = Populated(2);
            SavedSpatialFloor originalFloor = source.Floors[0];
            RoomSpatialInstance originalRoom = originalFloor.Layout.Rooms[0];
            FloorRouteNode[] nodeArray = originalFloor.Layout.Nodes;
            Array.Reverse(originalFloor.Layout.Rooms);
            Array.Reverse(originalFloor.Layout.Nodes);
            Array.Reverse(originalFloor.Layout.Edges);
            Array.Reverse(originalFloor.FixedStructures);
            Array.Reverse(originalFloor.RoomContents.Assignments);
            Array.Reverse(originalFloor.RoomContents.RoomSemantics);
            string sourceJson = JsonUtility.ToJson(source);

            DetachedCanonicalSpatialSaveState first = Canonicalize(source);
            DetachedCanonicalSpatialSaveState second = Canonicalize(first);
            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
            Assert.That(JsonUtility.ToJson(source), Is.EqualTo(sourceJson));
            Assert.That(first.Floors[0], Is.Not.SameAs(originalFloor));
            Assert.That(first.Floors[0].Layout.Rooms.All(x => !ReferenceEquals(x, originalRoom)), Is.True);
            Assert.That(first.Floors[0].Layout.Nodes, Is.Not.SameAs(nodeArray));
        }

        [Test]
        public void NullCollections_AreNormalizedOnlyOnDetachedResult()
        {
            DetachedCanonicalSpatialSaveState source = Populated(0);
            source.Floors[0].Layout.Rooms = null;
            source.Floors[0].Layout.Nodes = null;
            source.Floors[0].Layout.Edges = null;
            source.Floors[0].FixedStructures = null;
            source.Floors[0].RoomContents.Assignments = null;
            source.Floors[0].RoomContents.RoomSemantics = null;
            DetachedCanonicalSpatialSaveState copy = Canonicalize(source);
            Assert.That(source.Floors[0].Layout.Rooms, Is.Null);
            Assert.That(source.Floors[0].FixedStructures, Is.Null);
            Assert.That(source.Floors[0].RoomContents.Assignments, Is.Null);
            Assert.That(copy.Floors[0].Layout.Rooms, Is.Empty);
            Assert.That(copy.Floors[0].FixedStructures, Is.Empty);
            Assert.That(copy.Floors[0].RoomContents.Assignments, Is.Empty);
        }

        [TestCase("floor-index")]
        [TestCase("floor-id")]
        [TestCase("fixed-id")]
        public void DuplicateBindings_AreRejected(string kind)
        {
            DetachedCanonicalSpatialSaveState source = Populated(1);
            if (kind == "fixed-id") source.Floors[0].FixedStructures = new[] { source.Floors[0].FixedStructures[0], source.Floors[0].FixedStructures[0] };
            else
            {
                SavedSpatialFloor other = Populated(1).Floors[0];
                other.FloorInstanceId = kind == "floor-id" ? source.Floors[0].FloorInstanceId : "compat.floor.01";
                other.Layout.FloorId = other.FloorInstanceId;
                other.FloorIndex = kind == "floor-index" ? source.Floors[0].FloorIndex : 1;
                source.Floors = new[] { source.Floors[0], other };
            }
            CanonicalSpatialSaveValidationIssue expected = kind == "fixed-id" ? CanonicalSpatialSaveValidationIssue.DuplicateFixedStructure : CanonicalSpatialSaveValidationIssue.DuplicateFloorBinding;
            AssertIssue(source, expected);
        }

        [TestCase("room")]
        [TestCase("node")]
        [TestCase("edge")]
        [TestCase("fixed")]
        [TestCase("assignment")]
        public void CrossFloorReferences_AreRejected(string kind)
        {
            DetachedCanonicalSpatialSaveState source = Populated(1);
            SavedSpatialFloor floor = source.Floors[0];
            if (kind == "room") floor.Layout.Rooms[0].FloorId = "compat.floor.01";
            if (kind == "node") floor.Layout.Nodes[0].FloorId = "compat.floor.01";
            if (kind == "edge") floor.Layout.Edges[0].FloorId = "compat.floor.01";
            if (kind == "fixed") floor.FixedStructures[0].FloorInstanceId = "compat.floor.01";
            if (kind == "assignment") floor.RoomContents.Assignments[0].RoomInstanceId = "room.unknown";
            AssertIssue(source, CanonicalSpatialSaveValidationIssue.FloorReferenceMismatch);
        }

        [TestCase("malformed-id", CanonicalSpatialSaveValidationIssue.MalformedPersistentId)]
        [TestCase("layout-owner", CanonicalSpatialSaveValidationIssue.FloorReferenceMismatch)]
        [TestCase("fixed-kind", CanonicalSpatialSaveValidationIssue.InvalidFixedStructureKind)]
        public void InvalidFloorShapeEvidence_IsRejectedWithoutNormalization(string kind,
            CanonicalSpatialSaveValidationIssue expected)
        {
            DetachedCanonicalSpatialSaveState source = Populated(1);
            if (kind == "malformed-id") source.Floors[0].FloorDefinitionId = "Spatial Floor";
            if (kind == "layout-owner") source.Floors[0].Layout.FloorId = "compat.floor.01";
            if (kind == "fixed-kind") source.Floors[0].FixedStructures[0].Kind = 0;
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(source);
            Assert.That(canonical.Floors[0].FloorDefinitionId, Is.EqualTo(source.Floors[0].FloorDefinitionId));
            AssertIssue(canonical, expected);
        }

        [TestCase("assignment-id", CanonicalSpatialSaveValidationIssue.DuplicateAssignment)]
        [TestCase("room-category-sequence", CanonicalSpatialSaveValidationIssue.DuplicateRoomCategorySequence)]
        [TestCase("category", CanonicalSpatialSaveValidationIssue.InvalidContentCategory)]
        [TestCase("negative", CanonicalSpatialSaveValidationIssue.NegativeSequence)]
        [TestCase("next", CanonicalSpatialSaveValidationIssue.InvalidNextSequence)]
        public void InvalidAssignmentState_IsRejected(string kind, CanonicalSpatialSaveValidationIssue expected)
        {
            DetachedCanonicalSpatialSaveState source = Populated(1);
            RoomContentAssignment first = source.Floors[0].RoomContents.Assignments[0];
            if (kind == "assignment-id") source.Floors[0].RoomContents.Assignments = new[] { first, Assignment(first.AssignmentId, first.RoomInstanceId, CanonicalSpatialSaveContracts.TrapCategoryId, "option.trap.a", 2) };
            if (kind == "room-category-sequence") source.Floors[0].RoomContents.Assignments = new[] { first, Assignment("assignment.other", first.RoomInstanceId, first.CategoryId, "option.monster.b", first.Sequence) };
            if (kind == "category") first.CategoryId = "placement.category.room";
            if (kind == "negative") first.Sequence = -1;
            if (kind == "next") source.Floors[0].RoomContents.NextSequence = first.Sequence;
            AssertIssue(source, expected);
        }

        [TestCase("missing", CanonicalSpatialSaveValidationIssue.MissingRoomSemantics)]
        [TestCase("duplicate", CanonicalSpatialSaveValidationIssue.DuplicateRoomSemantics)]
        [TestCase("unknown", CanonicalSpatialSaveValidationIssue.UnknownRoomSemantics)]
        [TestCase("kind", CanonicalSpatialSaveValidationIssue.InvalidRoomOriginKind)]
        public void InvalidRoomSemantics_IsRejected(string kind, CanonicalSpatialSaveValidationIssue expected)
        {
            DetachedCanonicalSpatialSaveState source = Populated(1);
            CanonicalRoomSemantics semantics = source.Floors[0].RoomContents.RoomSemantics[0];
            if (kind == "missing") source.Floors[0].RoomContents.RoomSemantics = Array.Empty<CanonicalRoomSemantics>();
            if (kind == "duplicate") source.Floors[0].RoomContents.RoomSemantics = new[] { semantics, semantics };
            if (kind == "unknown") semantics.RoomInstanceId = "room.unknown";
            if (kind == "kind") semantics.LegacyRoomOriginKind = 0;
            AssertIssue(source, expected);
        }

        [TestCase("version", CanonicalSpatialSaveValidationIssue.InvalidLayoutContractVersion)]
        [TestCase("kind", CanonicalSpatialSaveValidationIssue.InvalidCreationKind)]
        [TestCase("native-audit", CanonicalSpatialSaveValidationIssue.NativeMarkerHasMigrationIdentity)]
        public void InvalidMarker_IsRejected(string kind, CanonicalSpatialSaveValidationIssue expected)
        {
            DetachedCanonicalSpatialSaveState source = Populated(0);
            if (kind == "version") source.Authority.CanonicalLayoutContractVersion = 0;
            if (kind == "kind") source.Authority.CreationKind = 0;
            if (kind == "native-audit") source.Authority.MigrationTransactionId = "transaction.01";
            AssertIssue(source, expected);
        }

        [Test]
        public void MigratedMarker_RetainsImmutableAuditIdentity()
        {
            DetachedCanonicalSpatialSaveState source = Populated(0);
            source.Authority.CreationKind = CanonicalSpatialCreationKind.Migrated;
            source.Authority.MigrationTransactionId = "transaction.01";
            source.Authority.MigrationDescriptorFingerprint = "fingerprint.01";
            DetachedCanonicalSpatialSaveState canonical = Canonicalize(source);
            Assert.That(CanonicalSpatialSaveContracts.Validate(canonical, Limits(), true).IsValid, Is.True);
            AssertStableUnityRoundTrip(canonical);
        }

        [Test]
        public void WorkloadLimits_FailClosedAtInvalidAndOneOver_AndAllowExactLimit()
        {
            DetachedCanonicalSpatialSaveState source = Populated(1);
            const int exactRecords = 11;
            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(source, Limits(exactRecords), out _), Is.True);
            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(source, Limits(exactRecords - 1), out _), Is.False);
            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(source, default, out _), Is.False);
            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(source, new CanonicalSpatialSaveWorkloadLimits(0, 100), out _), Is.False);
        }

        [Test]
        public void NonCanonicalOrdering_IsDetectedAndCanonicalOrderingPasses()
        {
            DetachedCanonicalSpatialSaveState source = Populated(2);
            Array.Reverse(source.Floors[0].FixedStructures);
            AssertIssue(source, CanonicalSpatialSaveValidationIssue.NonCanonicalOrdering, true);
            Assert.That(CanonicalSpatialSaveContracts.Validate(Canonicalize(source), Limits(), true).IsValid, Is.True);
        }

        [Test]
        public void SchemaSixAndOrdinarySaveJson_RemainWithoutCanonicalMembers()
        {
            Assert.That(SaveMigration.LatestSchemaVersion, Is.EqualTo(6));
            string json = JsonUtility.ToJson(new SaveRoot { schemaVersion = 6, primary = new SaveData() });
            Assert.That(json, Does.Not.Contain("spatialFloors"));
            Assert.That(json, Does.Not.Contain("CanonicalSpatialAuthority"));
            Assert.That(json, Does.Not.Contain("canonicalSpatial"));
        }

        private static void AssertStableUnityRoundTrip(DetachedCanonicalSpatialSaveState canonical)
        {
            string json = JsonUtility.ToJson(canonical);
            DetachedCanonicalSpatialSaveState restored = JsonUtility.FromJson<DetachedCanonicalSpatialSaveState>(json);
            DetachedCanonicalSpatialSaveState recanonicalized = Canonicalize(restored);
            Assert.That(JsonUtility.ToJson(recanonicalized), Is.EqualTo(json));
        }

        private static void AssertIssue(DetachedCanonicalSpatialSaveState source, CanonicalSpatialSaveValidationIssue expected,
            bool requireCanonical = false) => Assert.That(CanonicalSpatialSaveContracts.Validate(source, Limits(), requireCanonical).Issues, Does.Contain(expected));

        private static DetachedCanonicalSpatialSaveState Canonicalize(DetachedCanonicalSpatialSaveState source)
        {
            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(source, Limits(), out DetachedCanonicalSpatialSaveState canonical), Is.True);
            return canonical;
        }

        private static RoomContentAssignment Assignment(string id, string roomId, string category, string option, long sequence) =>
            new RoomContentAssignment { AssignmentId = id, RoomInstanceId = roomId, CategoryId = category, OptionId = option, Sequence = sequence };

        private static DetachedCanonicalSpatialSaveState Populated(int roomCount)
        {
            const string floorId = "compat.floor.00";
            var rooms = Enumerable.Range(0, roomCount).Select(index => new RoomSpatialInstance
            {
                RoomInstanceId = "room.0" + index, RoomDefinitionId = "spatial.room.basic", FloorId = floorId,
                Anchor = new TileCoordinate(0, 2 + index * 4), Orientation = CardinalOrientation.Zero
            }).ToArray();
            var nodes = Enumerable.Range(0, roomCount).Select(index => new FloorRouteNode
            {
                NodeId = "node.room.0" + index, FloorId = floorId, Kind = FloorRouteNodeKind.Room,
                RoomInstanceId = "room.0" + index
            }).Concat(new[]
            {
                new FloorRouteNode { NodeId = "node.entrance", FloorId = floorId, Kind = FloorRouteNodeKind.Entrance },
                new FloorRouteNode { NodeId = "node.completion", FloorId = floorId, Kind = FloorRouteNodeKind.Completion }
            }).ToArray();
            string[] route = new[] { "node.entrance" }.Concat(Enumerable.Range(0, roomCount).Select(index => "node.room.0" + index)).Concat(new[] { "node.completion" }).ToArray();
            var edges = Enumerable.Range(0, route.Length - 1).Select(index => new FloorRouteEdge
            {
                EdgeId = "edge.direct.0" + index, FloorId = floorId, SourceNodeId = route[index], DestinationNodeId = route[index + 1],
                ConnectionKind = FloorRouteConnectionKind.DirectDoorway, Classification = RouteClassification.Required,
                CorridorDefinitionId = string.Empty, OptionalBranchId = string.Empty, Footprint = null
            }).ToArray();
            return new DetachedCanonicalSpatialSaveState
            {
                Authority = new CanonicalSpatialAuthorityMarker { CanonicalLayoutContractVersion = 1, CreationKind = CanonicalSpatialCreationKind.NativeCanonical },
                Floors = new[]
                {
                    new SavedSpatialFloor
                    {
                        FloorInstanceId = floorId, FloorDefinitionId = "spatial.floor.01", FloorIndex = 0,
                        Layout = new FloorSpatialLayout { FloorId = floorId, Rooms = rooms, Nodes = nodes, Edges = edges },
                        FixedStructures = new[]
                        {
                            new SavedFixedSpatialStructure { FixedStructureInstanceId = floorId + ".fixed.entrance", FixedStructureDefinitionId = "spatial.fixed.entrance_hall", FloorInstanceId = floorId, Anchor = new TileCoordinate(0, 0), Orientation = CardinalOrientation.Zero, Kind = FixedSpatialStructureKind.Entrance },
                            new SavedFixedSpatialStructure { FixedStructureInstanceId = floorId + ".fixed.completion", FixedStructureDefinitionId = "spatial.fixed.completion_terminal", FloorInstanceId = floorId, Anchor = new TileCoordinate(1, roomCount == 2 ? 10 : 6), Orientation = CardinalOrientation.Zero, Kind = FixedSpatialStructureKind.CompletionTerminal }
                        },
                        RoomContents = new FloorRoomContentState
                        {
                            Assignments = roomCount == 0 ? Array.Empty<RoomContentAssignment>() : new[] { Assignment("assignment.monster.01", "room.00", CanonicalSpatialSaveContracts.MonsterCategoryId, "option.monster.a", 1) },
                            RoomSemantics = rooms.Select(room => new CanonicalRoomSemantics { RoomInstanceId = room.RoomInstanceId, LegacyRoomOriginKind = LegacyRoomOriginKind.MigratedExplicitLegacyRoom }).ToArray(),
                            NextSequence = roomCount == 0 ? 0 : 2
                        }
                    }
                }
            };
        }
    }
}
#endif
