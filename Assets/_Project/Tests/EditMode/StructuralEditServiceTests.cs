#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class StructuralEditServiceTests
    {
        [Test]
        public void StraightStoneCorridor_MaximumLengthUsesProductionLimit()
        {
            PreviewFixture fixture = CreateR1();
            var request = new StructuralConstructionRequest { RoomDefinitionId = "spatial.room.basic",
                Anchor = new TileCoordinate(8, 2), Orientation = CardinalOrientation.Zero,
                TerminalConnectionPointId = "north" };
            StructuralEditPreview first = StructuralEditService.Preview(fixture.State, request,
                fixture.Production, fixture.Compatibility, fixture.Configuration, fixture.Limits);
            StructuralEditPreview second = StructuralEditService.Preview(fixture.State, request,
                fixture.Production, fixture.Compatibility, fixture.Configuration, fixture.Limits);
            Assert.That(first.IsValid, Is.True, string.Join(",", first.ReasonCodes));
            FloorRouteEdge edge = first.DetachedCandidate.Floors[0].Layout.Edges.Single(value =>
                value.EdgeId.EndsWith(".edge.incoming"));
            Assert.That(edge.ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.PhysicalCorridor));
            Assert.That(edge.Footprint.OccupiedTiles.Length, Is.EqualTo(4));
            CollectionAssert.AreEqual(new[] { new TileCoordinate(4, 3), new TileCoordinate(5, 3),
                new TileCoordinate(6, 3), new TileCoordinate(7, 3) }, edge.Footprint.OccupiedTiles);
            Assert.That(first.ResultingUsedFloorSpace, Is.EqualTo(46));
            Assert.That(first.ResultingRemainingFloorSpace, Is.EqualTo(14));
            SpatialContractResult<byte[]> a = CanonicalSpatialSaveSerializer.Serialize(first.DetachedCandidate, fixture.Limits);
            SpatialContractResult<byte[]> b = CanonicalSpatialSaveSerializer.Serialize(second.DetachedCandidate, fixture.Limits);
            CollectionAssert.AreEqual(a.Value, b.Value);
        }

        [Test]
        public void StraightStoneCorridor_AboveMaximumHasLengthReason()
        {
            PreviewFixture fixture = CreateR1();
            AssertInvalidUnchanged(fixture, new StructuralConstructionRequest
            { RoomDefinitionId = "spatial.room.rectangle", Anchor = new TileCoordinate(9, 1),
              Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "north" },
                fixture.Production, fixture.Limits, StructuralEditService.CorridorLengthReason);
        }

        [Test]
        public void CorridorTileAloneCanExceedTestOnlyCapacity()
        {
            PreviewFixture fixture = CreateR1();
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            catalog.Floors[0].FinalFloorSpaceCapacity = 42;
            ProductionSpatialContentSnapshot production = new ProductionSpatialContentSnapshot(
                fixture.Production.Manifest, catalog, fixture.Production.Languages);
            AssertInvalidUnchanged(fixture, new StructuralConstructionRequest
            { RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(5, 2),
              Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "east" },
                production, fixture.Limits, StructuralEditService.CapacityReason);
        }

        [Test]
        public void CorridorRequiresBothEndpointSocketTypesInDefinition()
        {
            PreviewFixture fixture = CreateR1();
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            RoomSpatialDefinition basic = catalog.Rooms.Single(value => value.RoomDefinitionId == "spatial.room.basic");
            basic.ConnectionPoints.Single(value => value.ConnectionPointId == "west").SocketTypeId = "test.socket.destination";
            catalog.SocketTypes[0].CompatibleSocketTypeIds = catalog.SocketTypes[0].CompatibleSocketTypeIds
                .Concat(new[] { "test.socket.destination" }).ToArray();
            catalog.SocketTypes = catalog.SocketTypes.Concat(new[] { new SpatialSocketTypeDefinition
            { SocketTypeId = "test.socket.destination", CompatibleSocketTypeIds = new[] { "spatial.socket.standard_passage" } } }).ToArray();
            ProductionSpatialContentSnapshot production = new ProductionSpatialContentSnapshot(
                fixture.Production.Manifest, catalog, fixture.Production.Languages);
            AssertInvalidUnchanged(fixture, new StructuralConstructionRequest
            { RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(5, 2),
              Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "east" },
                production, fixture.Limits, StructuralEditService.ConnectionUnavailableReason);
        }

        [TestCase(5, 2, "east", 4, 3)]
        [TestCase(0, 7, "east", 1, 6)]
        public void StraightStoneCorridor_OneTileAppendIsDeterministic(int x, int y,
            string terminalPoint, int corridorX, int corridorY)
        {
            PreviewFixture fixture = CreateR1();
            var request = new StructuralConstructionRequest { RoomDefinitionId = "spatial.room.basic",
                Anchor = new TileCoordinate(x, y), Orientation = CardinalOrientation.Zero,
                TerminalConnectionPointId = terminalPoint };
            StructuralEditPreview first = StructuralEditService.Preview(fixture.State, request,
                fixture.Production, fixture.Compatibility, fixture.Configuration, fixture.Limits);
            StructuralEditPreview second = StructuralEditService.Preview(fixture.State, request,
                fixture.Production, fixture.Compatibility, fixture.Configuration, fixture.Limits);
            Assert.That(first.IsValid, Is.True, string.Join(",", first.ReasonCodes));
            Assert.That(first.ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.PhysicalCorridor));
            FloorRouteEdge incoming = first.DetachedCandidate.Floors[0].Layout.Edges.Single(value =>
                value.EdgeId.EndsWith(".edge.incoming"));
            Assert.That(incoming.ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.PhysicalCorridor));
            Assert.That(incoming.CorridorDefinitionId, Is.EqualTo("spatial.corridor.straight_stone"));
            Assert.That(incoming.Footprint.OccupiedTiles.Length, Is.EqualTo(1));
            Assert.That(incoming.Footprint.OccupiedTiles[0].X, Is.EqualTo(corridorX));
            Assert.That(incoming.Footprint.OccupiedTiles[0].Y, Is.EqualTo(corridorY));
            FloorRouteEdge terminal = first.DetachedCandidate.Floors[0].Layout.Edges.Single(value =>
                value.EdgeId.EndsWith(".edge.terminal"));
            Assert.That(terminal.ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.DirectDoorway));
            Assert.That(terminal.Footprint, Is.Null);
            Assert.That(first.ResultingUsedFloorSpace, Is.EqualTo(43));
            SpatialContractResult<byte[]> a = CanonicalSpatialSaveSerializer.Serialize(first.DetachedCandidate, fixture.Limits);
            SpatialContractResult<byte[]> b = CanonicalSpatialSaveSerializer.Serialize(second.DetachedCandidate, fixture.Limits);
            CollectionAssert.AreEqual(a.Value, b.Value);
        }

        [TestCase("unknown", CardinalOrientation.Zero, 0, 6, "north", StructuralEditService.RoomDefinitionInvalidReason)]
        [TestCase("spatial.room.basic", CardinalOrientation.Ninety, 0, 6, "north", StructuralEditService.OrientationInvalidReason)]
        [TestCase("spatial.room.basic", CardinalOrientation.Zero, -1, 6, "north", StructuralEditService.OutOfBoundsReason)]
        [TestCase("spatial.room.basic", CardinalOrientation.Zero, 0, 2, "north", StructuralEditService.RoomOverlapReason)]
        [TestCase("spatial.room.basic", CardinalOrientation.Zero, 0, 6, "missing", StructuralEditService.ConnectionPointInvalidReason)]
        [TestCase("spatial.room.basic", CardinalOrientation.Zero, 5, 6, "north", StructuralEditService.ConnectionUnavailableReason)]
        [TestCase("spatial.room.rectangle", CardinalOrientation.Zero, 4, 1, "south", StructuralEditService.TerminalPlacementInvalidReason)]
        [TestCase("spatial.room.rectangle", CardinalOrientation.Zero, 4, 1, "west", StructuralEditService.TerminalPlacementInvalidReason)]
        public void InvalidNativePlacement_HasStableReasonAndDoesNotMutateSource(string definitionId,
            CardinalOrientation orientation, int x, int y, string point, string expectedReason)
        {
            PreviewFixture fixture = CreateR1();
            AssertInvalidUnchanged(fixture, new StructuralConstructionRequest { RoomDefinitionId = definitionId,
                Anchor = new TileCoordinate(x, y), Orientation = orientation,
                TerminalConnectionPointId = point }, fixture.Production, fixture.Limits, expectedReason);
        }

        [Test]
        public void RoomNotAllowed_UsesTestOnlyCatalogAndStableReason()
        {
            PreviewFixture fixture = CreateR1();
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            catalog.Floors[0].AllowedRoomDefinitionIds = catalog.Floors[0].AllowedRoomDefinitionIds
                .Where(value => value != "spatial.room.rectangle").ToArray();
            ProductionSpatialContentSnapshot production = new ProductionSpatialContentSnapshot(
                fixture.Production.Manifest, catalog, fixture.Production.Languages);
            AssertInvalidUnchanged(fixture, new StructuralConstructionRequest
            { RoomDefinitionId = "spatial.room.rectangle", Anchor = new TileCoordinate(4, 1),
              Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "north" },
                production, fixture.Limits, StructuralEditService.RoomNotAllowedReason);
        }

        [Test]
        public void CapacityExceeded_IncludesFixedStructuresUsingTestOnlyCapacity()
        {
            PreviewFixture fixture = CreateR1();
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            catalog.Floors[0].FinalFloorSpaceCapacity = 40;
            ProductionSpatialContentSnapshot production = new ProductionSpatialContentSnapshot(
                fixture.Production.Manifest, catalog, fixture.Production.Languages);
            AssertInvalidUnchanged(fixture, new StructuralConstructionRequest
            { RoomDefinitionId = "spatial.room.rectangle", Anchor = new TileCoordinate(4, 1),
              Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "north" },
                production, fixture.Limits, StructuralEditService.CapacityReason);
        }

        [Test]
        public void SocketIncompatible_UsesTestOnlySocketMatrix()
        {
            PreviewFixture fixture = CreateR1();
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            FixedSpatialStructureDefinition terminal = catalog.FixedStructures.Single(value =>
                value.Kind == FixedSpatialStructureKind.CompletionTerminal);
            terminal.ConnectionPoints[0].SocketTypeId = "test.socket.blocked";
            catalog.SocketTypes = catalog.SocketTypes.Concat(new[] { new SpatialSocketTypeDefinition
                { SocketTypeId = "test.socket.blocked", CompatibleSocketTypeIds = new[] { "test.socket.blocked" } } }).ToArray();
            ProductionSpatialContentSnapshot production = new ProductionSpatialContentSnapshot(
                fixture.Production.Manifest, catalog, fixture.Production.Languages);
            AssertInvalidUnchanged(fixture, new StructuralConstructionRequest
            { RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(0, 6),
              Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "north" },
                production, fixture.Limits, StructuralEditService.SocketIncompatibleReason);
        }

        [Test]
        public void WorkloadExceeded_FailsBeforeRoomMaterialization()
        {
            PreviewFixture fixture = CreateR1();
            var limits = new CanonicalSpatialSerializationLimits(fixture.Limits.Serialized,
                new CanonicalSpatialSaveWorkloadLimits(fixture.Limits.Spatial.MaximumRecords, 15));
            AssertInvalidUnchanged(fixture, new StructuralConstructionRequest
            { RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(0, 6),
              Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "north" },
                fixture.Production, limits, StructuralEditService.WorkloadReason);
        }

        [Test]
        public void ExistingPhysicalCorridorOverlap_HasStableReason()
        {
            PreviewFixture fixture = CreateR1();
            SavedSpatialFloor floor = fixture.State.Floors[0];
            FloorRouteNode room = floor.Layout.Nodes.Single(value => value.Kind == FloorRouteNodeKind.Room);
            FloorRouteNode completion = floor.Layout.Nodes.Single(value => value.Kind == FloorRouteNodeKind.Completion);
            floor.Layout.Edges = floor.Layout.Edges.Concat(new[] { new FloorRouteEdge
            {
                EdgeId = "compat.floor.00.edge.test-corridor", FloorId = floor.FloorInstanceId,
                SourceNodeId = room.NodeId, DestinationNodeId = completion.NodeId,
                CorridorDefinitionId = "spatial.corridor.straight_stone",
                ConnectionKind = FloorRouteConnectionKind.PhysicalCorridor,
                Classification = RouteClassification.Optional, OptionalBranchId = "test.branch",
                Footprint = new ResolvedTileFootprint(new[] { new TileCoordinate(0, 6) })
            } }).ToArray();
            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(fixture.State, fixture.Limits.Spatial,
                out DetachedCanonicalSpatialSaveState canonical), Is.True);
            Assert.That(CanonicalSpatialSaveContracts.Validate(canonical, fixture.Limits.Spatial,
                true).IsValid, Is.True);
            Assert.That(DetachedCanonicalProductionSemanticValidation.Validate(canonical,
                fixture.Production, fixture.Configuration, fixture.Limits.Spatial).IsValid, Is.True);
            fixture.State = canonical;
            AssertInvalidUnchanged(fixture, new StructuralConstructionRequest
            { RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(0, 6),
              Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "north" },
                fixture.Production, fixture.Limits, StructuralEditService.CorridorOverlapReason);
        }

        [Test]
        public void DerivedIncomingCorridorCannotOverlapExistingPhysicalCorridor()
        {
            PreviewFixture fixture = CreateR1();
            SavedSpatialFloor floor = fixture.State.Floors[0];
            FloorRouteNode room = floor.Layout.Nodes.Single(value => value.Kind == FloorRouteNodeKind.Room);
            FloorRouteNode completion = floor.Layout.Nodes.Single(value => value.Kind == FloorRouteNodeKind.Completion);
            floor.Layout.Edges = floor.Layout.Edges.Concat(new[] { new FloorRouteEdge
            {
                EdgeId = "compat.floor.00.edge.test-corridor-derived", FloorId = floor.FloorInstanceId,
                SourceNodeId = room.NodeId, DestinationNodeId = completion.NodeId,
                CorridorDefinitionId = "spatial.corridor.straight_stone",
                ConnectionKind = FloorRouteConnectionKind.PhysicalCorridor,
                Classification = RouteClassification.Optional, OptionalBranchId = "test.branch.derived",
                Footprint = new ResolvedTileFootprint(new[] { new TileCoordinate(4, 3) })
            } }).ToArray();
            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(fixture.State, fixture.Limits.Spatial,
                out DetachedCanonicalSpatialSaveState canonical), Is.True);
            Assert.That(CanonicalSpatialSaveContracts.Validate(canonical, fixture.Limits.Spatial, true).IsValid, Is.True);
            Assert.That(DetachedCanonicalProductionSemanticValidation.Validate(canonical,
                fixture.Production, fixture.Configuration, fixture.Limits.Spatial).IsValid, Is.True);
            fixture.State = canonical;
            AssertInvalidUnchanged(fixture, new StructuralConstructionRequest
            { RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(5, 2),
              Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "east" },
                fixture.Production, fixture.Limits, StructuralEditService.CorridorOverlapReason);
        }

        [Test]
        public void Preview_DerivedIdentityCollisionFailsBeforeCandidatePublication()
        {
            var fixture = Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            DetachedCompleteSaveValidationResult parsed = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                fixture.Result.Attempt.Candidate.GetBytes(), new DetachedCurrentTargetValidationContext(
                    fixture.Compatibility, fixture.Production, fixture.LegacyBytes, fixture.Limits));
            RunSimulationConfig configuration = LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes);
            DetachedCanonicalMutationResult r1 = DetachedCanonicalSpatialMutation.Prepare(parsed.State,
                DetachedCanonicalMutationRequest.Place("placement.category.room", "placement.option.room.basic"),
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            DetachedCanonicalMutationResult content = DetachedCanonicalSpatialMutation.Prepare(r1.State,
                DetachedCanonicalMutationRequest.Place("placement.category.monster",
                    "placement.option.monster.skeleton"), fixture.Production, fixture.Compatibility,
                configuration, fixture.Limits);
            content.State.Floors[0].RoomContents.Assignments[0].AssignmentId =
                "compat.floor.00.room.player.0000.node";
            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(content.State, fixture.Limits.Spatial,
                out DetachedCanonicalSpatialSaveState source), Is.True);
            SpatialContractResult<byte[]> before = CanonicalSpatialSaveSerializer.Serialize(source, fixture.Limits);
            Assert.That(before.IsValid, Is.True);
            StructuralEditPreview preview = StructuralEditService.Preview(source,
                new StructuralConstructionRequest { RoomDefinitionId = "spatial.room.basic",
                    Anchor = new TileCoordinate(0, 6), Orientation = CardinalOrientation.Zero,
                    TerminalConnectionPointId = "north" }, fixture.Production, fixture.Compatibility,
                configuration, fixture.Limits);
            Assert.That(preview.IsValid, Is.False);
            Assert.That(preview.ReasonCodes[0], Is.EqualTo(StructuralEditService.InvalidIdentityReason));
            Assert.That(preview.DetachedCandidate, Is.Null);
            SpatialContractResult<byte[]> after = CanonicalSpatialSaveSerializer.Serialize(source, fixture.Limits);
            Assert.That(after.IsValid, Is.True);
            CollectionAssert.AreEqual(before.Value, after.Value);
        }

        [TestCase("spatial.room.rectangle", CardinalOrientation.Ninety, 4, 2, "west", 6, 5, 41, 19)]
        [TestCase("spatial.room.large_chamber", CardinalOrientation.Ninety, 4, 1, "west", 6, 6, 56, 4)]
        [TestCase("spatial.room.basic", CardinalOrientation.Zero, 4, 2, "east", 8, 2, 42, 18)]
        public void RotatedConnectionGeometry_DerivesExpectedTerminal(string definitionId,
            CardinalOrientation orientation, int x, int y, string pointId, int terminalX,
            int terminalY, int used, int remaining)
        {
            var fixture = Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            DetachedCompleteSaveValidationResult parsed = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                fixture.Result.Attempt.Candidate.GetBytes(), new DetachedCurrentTargetValidationContext(
                    fixture.Compatibility, fixture.Production, fixture.LegacyBytes, fixture.Limits));
            RunSimulationConfig configuration = LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes);
            DetachedCanonicalMutationResult r1 = DetachedCanonicalSpatialMutation.Prepare(parsed.State,
                DetachedCanonicalMutationRequest.Place("placement.category.room", "placement.option.room.basic"),
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            var request = new StructuralConstructionRequest { RoomDefinitionId = definitionId,
                Anchor = new TileCoordinate(x, y), Orientation = orientation,
                TerminalConnectionPointId = pointId };
            StructuralEditPreview first = StructuralEditService.Preview(r1.State, request,
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            StructuralEditPreview second = StructuralEditService.Preview(r1.State, request,
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            Assert.That(first.IsValid, Is.True, string.Join(",", first.ReasonCodes));
            SavedFixedSpatialStructure terminal = first.DetachedCandidate.Floors[0].FixedStructures.Single(value =>
                value.Kind == FixedSpatialStructureKind.CompletionTerminal);
            Assert.That(terminal.Anchor.X, Is.EqualTo(terminalX));
            Assert.That(terminal.Anchor.Y, Is.EqualTo(terminalY));
            Assert.That(terminal.Orientation, Is.EqualTo(pointId == "east"
                ? CardinalOrientation.Ninety : CardinalOrientation.Zero));
            Assert.That(first.ResultingUsedFloorSpace, Is.EqualTo(used));
            Assert.That(first.ResultingRemainingFloorSpace, Is.EqualTo(remaining));
            SpatialContractResult<byte[]> firstBytes = CanonicalSpatialSaveSerializer.Serialize(
                first.DetachedCandidate, fixture.Limits);
            SpatialContractResult<byte[]> secondBytes = CanonicalSpatialSaveSerializer.Serialize(
                second.DetachedCandidate, fixture.Limits);
            Assert.That(firstBytes.IsValid, Is.True);
            Assert.That(secondBytes.IsValid, Is.True);
            CollectionAssert.AreEqual(firstBytes.Value, secondBytes.Value);
        }

        [Test]
        public void ConnectionPointTransforms_StayOnRotatedFacingBoundary()
        {
            var fixture = Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            foreach (RoomSpatialDefinition room in catalog.Rooms)
            foreach (CardinalOrientation orientation in new[] { CardinalOrientation.Zero,
                CardinalOrientation.Ninety, CardinalOrientation.OneEighty, CardinalOrientation.TwoSeventy })
            foreach (SpatialConnectionPointDefinition point in room.ConnectionPoints)
            {
                TileCoordinate transformed = StructuralEditService.TransformConnectionPointOffset(
                    point.Offset, orientation, room.GrossFootprint);
                CardinalOrientation facing = StructuralEditService.Rotate(point.Facing, orientation);
                int width = orientation == CardinalOrientation.Ninety || orientation == CardinalOrientation.TwoSeventy
                    ? room.GrossFootprint.Height : room.GrossFootprint.Width;
                int height = orientation == CardinalOrientation.Ninety || orientation == CardinalOrientation.TwoSeventy
                    ? room.GrossFootprint.Width : room.GrossFootprint.Height;
                bool onBoundary = facing == CardinalOrientation.Zero ? transformed.Y == height - 1 :
                    facing == CardinalOrientation.Ninety ? transformed.X == width - 1 :
                    facing == CardinalOrientation.OneEighty ? transformed.Y == 0 : transformed.X == 0;
                Assert.That(onBoundary, Is.True, room.RoomDefinitionId + ":" + point.ConnectionPointId + ":" + orientation);
            }
            foreach (FixedSpatialStructureDefinition structure in catalog.FixedStructures)
            foreach (CardinalOrientation orientation in structure.AllowedOrientations)
            foreach (SpatialConnectionPointDefinition point in structure.ConnectionPoints)
            {
                TileCoordinate transformed = StructuralEditService.TransformConnectionPointOffset(
                    point.Offset, orientation, structure.GrossFootprint);
                CardinalOrientation facing = StructuralEditService.Rotate(point.Facing, orientation);
                int width = orientation == CardinalOrientation.Ninety || orientation == CardinalOrientation.TwoSeventy
                    ? structure.GrossFootprint.Height : structure.GrossFootprint.Width;
                int height = orientation == CardinalOrientation.Ninety || orientation == CardinalOrientation.TwoSeventy
                    ? structure.GrossFootprint.Width : structure.GrossFootprint.Height;
                bool onBoundary = facing == CardinalOrientation.Zero ? transformed.Y == height - 1 :
                    facing == CardinalOrientation.Ninety ? transformed.X == width - 1 :
                    facing == CardinalOrientation.OneEighty ? transformed.Y == 0 : transformed.X == 0;
                Assert.That(onBoundary, Is.True, structure.StructureDefinitionId + ":" + orientation);
            }
        }

        [TestCase("spatial.room.rectangle", CardinalOrientation.Zero, 4, 1, "north", true)]
        [TestCase("spatial.room.rectangle", CardinalOrientation.Ninety, 4, 2, "west", true)]
        [TestCase("spatial.room.large_chamber", CardinalOrientation.Zero, 4, 1, "north", true)]
        [TestCase("spatial.room.large_chamber", CardinalOrientation.Ninety, 4, 1, "west", true)]
        public void NativeRoomAppend_UsesProductionGeometry(string definitionId,
            CardinalOrientation orientation, int x, int y, string terminalPoint, bool expectedValid)
        {
            var fixture = Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            DetachedCompleteSaveValidationResult parsed = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                fixture.Result.Attempt.Candidate.GetBytes(), new DetachedCurrentTargetValidationContext(
                    fixture.Compatibility, fixture.Production, fixture.LegacyBytes, fixture.Limits));
            RunSimulationConfig configuration = LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes);
            DetachedCanonicalMutationResult r1 = DetachedCanonicalSpatialMutation.Prepare(parsed.State,
                DetachedCanonicalMutationRequest.Place("placement.category.room", "placement.option.room.basic"),
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            StructuralEditPreview preview = StructuralEditService.Preview(r1.State,
                new StructuralConstructionRequest { RoomDefinitionId = definitionId,
                    Anchor = new TileCoordinate(x, y), Orientation = orientation,
                    TerminalConnectionPointId = terminalPoint }, fixture.Production,
                fixture.Compatibility, configuration, fixture.Limits);
            Assert.That(preview.IsValid, Is.EqualTo(expectedValid), string.Join(",", preview.ReasonCodes));
            if (!expectedValid) return;
            Assert.That(preview.ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.DirectDoorway));
            Assert.That(preview.Consequences.Any(value => value.Kind == StructuralChangeKind.FixedStructureMoved), Is.True);
            DetachedCanonicalSpatialSaveState candidate = preview.DetachedCandidate;
            Assert.That(candidate.Floors[0].Layout.Edges.Where(value =>
                value.EdgeId.Contains("room.player.0000")).All(value =>
                    value.ConnectionKind == FloorRouteConnectionKind.DirectDoorway &&
                    value.Footprint == null && value.CorridorDefinitionId == string.Empty), Is.True);
            Assert.That(r1.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(1));
        }

        [Test]
        public void ApprovedBasicRoomPreview_IsDeterministicAndProducesCanonicalR2()
        {
            var fixture = Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            DetachedCompleteSaveValidationResult parsed = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                fixture.Result.Attempt.Candidate.GetBytes(), new DetachedCurrentTargetValidationContext(
                    fixture.Compatibility, fixture.Production, fixture.LegacyBytes, fixture.Limits));
            Assert.That(parsed.IsValid, Is.True);
            RunSimulationConfig configuration = LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes);
            DetachedCanonicalMutationResult r1 = DetachedCanonicalSpatialMutation.Prepare(parsed.State,
                DetachedCanonicalMutationRequest.Place("placement.category.room", "placement.option.room.basic"),
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            Assert.That(r1.IsSuccess, Is.True, r1.Reason);
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            FloorLayoutValidationResult r1Layout = FloorLayoutValidator.Validate(r1.State.Floors[0].Layout,
                catalog.Floors.Single(), catalog.Rooms, catalog.Corridors,
                new SpatialValidationWorkloadLimits(fixture.Limits.Spatial.MaximumMaterializedTiles),
                r1.State.Floors[0].FixedStructures, catalog.FixedStructures);
            Assert.That(r1Layout.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(26));
            CompatibilityLayoutGeometryRecord geometry = fixture.Compatibility.Value.GeometryRecords.Single();
            CompatibilityLayoutPlacement placement = geometry.Layouts.Single(layout =>
                layout.Placements.Any(value => value.Role == CompatibilityRouteRole.BasicRoom1))
                .Placements.Single(value => value.Role == CompatibilityRouteRole.BasicRoom1);
            var request = new StructuralConstructionRequest { RoomDefinitionId = geometry.BasicRoomDefinitionId,
                Anchor = placement.Anchor, Orientation = placement.Orientation, TerminalConnectionPointId = "north" };

            StructuralEditPreview first = StructuralEditService.Preview(r1.State, request,
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            StructuralEditPreview second = StructuralEditService.Preview(r1.State, request,
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);

            Assert.That(first.IsValid, Is.True, string.Join(",", first.ReasonCodes));
            Assert.That(first.ProspectiveFloorSpace, Is.EqualTo(first.OccupiedTiles.Length));
            Assert.That(first.ResultingUsedFloorSpace, Is.EqualTo(42));
            Assert.That(first.ResultingRemainingFloorSpace, Is.EqualTo(18));
            Assert.That(first.Consequences.Any(value => value.Kind == StructuralChangeKind.FixedStructureMoved), Is.True);
            CollectionAssert.AreEqual(first.OccupiedTiles, second.OccupiedTiles);
            DetachedCanonicalMutationResult mutation = DetachedCanonicalSpatialMutation.Prepare(r1.State,
                DetachedCanonicalMutationRequest.Construct(first), fixture.Production, fixture.Compatibility,
                configuration, fixture.Limits);
            Assert.That(mutation.IsSuccess, Is.True, mutation.Reason);
            Assert.That(mutation.State.Floors[0].Layout.Rooms.Any(value =>
                value.RoomInstanceId == "compat.floor.00.room.player.0000"), Is.True);
            SpatialContractResult<byte[]> canonicalBytes = CanonicalSpatialSaveSerializer.Serialize(
                mutation.State, fixture.Limits);
            Assert.That(canonicalBytes.IsValid, Is.True);
            SpatialContractResult<DetachedCanonicalSpatialSaveState> reopened =
                CanonicalSpatialSaveSerializer.Parse(canonicalBytes.Value, fixture.Limits);
            Assert.That(reopened.IsValid, Is.True);
            Assert.That(reopened.Value.Floors[0].Layout.Rooms.Any(value =>
                value.RoomInstanceId == "compat.floor.00.room.player.0000"), Is.True);
            Assert.That(mutation.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(2));
            Assert.That(r1.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(1));

            DetachedCanonicalMutationResult intervening = DetachedCanonicalSpatialMutation.Prepare(r1.State,
                DetachedCanonicalMutationRequest.Place("placement.category.monster",
                    "placement.option.monster.skeleton"), fixture.Production, fixture.Compatibility,
                configuration, fixture.Limits);
            Assert.That(intervening.IsSuccess, Is.True, intervening.Reason);
            StructuralEditPreview afterContent = StructuralEditService.Preview(intervening.State, request,
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            Assert.That(afterContent.IsValid, Is.True, string.Join(",", afterContent.ReasonCodes));
            Assert.That(afterContent.Consequences.Single(value => value.Kind == StructuralChangeKind.RoomAdded).StableId,
                Is.EqualTo(first.Consequences.Single(value => value.Kind == StructuralChangeKind.RoomAdded).StableId));
            DetachedCanonicalMutationResult stale = DetachedCanonicalSpatialMutation.Prepare(intervening.State,
                DetachedCanonicalMutationRequest.Construct(first), fixture.Production, fixture.Compatibility,
                configuration, fixture.Limits);
            Assert.That(stale.IsSuccess, Is.False);
            Assert.That(stale.Reason, Is.EqualTo(StructuralEditService.StalePreviewReason));
            Assert.That(intervening.State.Floors[0].RoomContents.Assignments.Length, Is.EqualTo(1));
        }

        [Test]
        public void InvalidPlacement_DoesNotExposeCandidate()
        {
            var fixture = Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            DetachedCompleteSaveValidationResult parsed = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                fixture.Result.Attempt.Candidate.GetBytes(), new DetachedCurrentTargetValidationContext(
                    fixture.Compatibility, fixture.Production, fixture.LegacyBytes, fixture.Limits));
            CompatibilityLayoutGeometryRecord geometry = fixture.Compatibility.Value.GeometryRecords.Single();
            RunSimulationConfig configuration = LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes);
            DetachedCanonicalMutationResult r1 = DetachedCanonicalSpatialMutation.Prepare(parsed.State,
                DetachedCanonicalMutationRequest.Place("placement.category.room", "placement.option.room.basic"),
                fixture.Production, fixture.Compatibility, configuration, fixture.Limits);
            var request = new StructuralConstructionRequest { RoomDefinitionId = geometry.BasicRoomDefinitionId,
                Anchor = new TileCoordinate(-1, -1), Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "north" };
            StructuralEditPreview preview = StructuralEditService.Preview(r1.State, request,
                fixture.Production, fixture.Compatibility,
                configuration, fixture.Limits);
            Assert.That(preview.IsValid, Is.False);
            Assert.That(preview.ReasonCodes[0], Is.EqualTo(StructuralEditService.OutOfBoundsReason));
            Assert.That(r1.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(1));
        }

        [Test]
        public void Movement_PreservesStableIdentitiesAndRejectsStalePreview()
        {
            PreviewFixture fixture = CreateR2();
            SavedSpatialFloor source = fixture.State.Floors[0];
            RoomSpatialInstance target = source.Layout.Rooms.Single(value =>
                value.RoomInstanceId == "compat.floor.00.room.player.0000");
            FloorRouteNode node = source.Layout.Nodes.Single(value => value.RoomInstanceId == target.RoomInstanceId);
            SavedFixedSpatialStructure terminal = source.FixedStructures.Single(value =>
                value.Kind == FixedSpatialStructureKind.CompletionTerminal);
            var request = new StructuralMovementRequest { RoomInstanceId = target.RoomInstanceId,
                Anchor = new TileCoordinate(5, 2) };
            StructuralEditPreview preview = StructuralRenovationService.PreviewMovement(fixture.State, request,
                fixture.Production, fixture.Compatibility, fixture.Configuration, fixture.Limits);
            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            SavedSpatialFloor proposed = preview.DetachedCandidate.Floors[0];
            Assert.That(proposed.Layout.Rooms.Single(value => value.RoomInstanceId == target.RoomInstanceId).Anchor,
                Is.EqualTo(request.Anchor));
            Assert.That(proposed.Layout.Nodes.Single(value => value.RoomInstanceId == target.RoomInstanceId).NodeId,
                Is.EqualTo(node.NodeId));
            Assert.That(proposed.FixedStructures.Single(value =>
                value.Kind == FixedSpatialStructureKind.CompletionTerminal).FixedStructureInstanceId,
                Is.EqualTo(terminal.FixedStructureInstanceId));

            DetachedCanonicalMutationResult intervening = DetachedCanonicalSpatialMutation.Prepare(fixture.State,
                DetachedCanonicalMutationRequest.Place("placement.category.monster",
                    "placement.option.monster.skeleton", target.RoomInstanceId), fixture.Production,
                fixture.Compatibility, fixture.Configuration, fixture.Limits);
            Assert.That(intervening.IsSuccess, Is.True, intervening.Reason);
            DetachedCanonicalMutationResult stale = DetachedCanonicalSpatialMutation.Prepare(intervening.State,
                DetachedCanonicalMutationRequest.Move(preview), fixture.Production, fixture.Compatibility,
                fixture.Configuration, fixture.Limits);
            Assert.That(stale.Reason, Is.EqualTo(StructuralEditService.StalePreviewReason));
        }

        [Test]
        public void Replacement_PreservesRoomNodeAndAssignments()
        {
            PreviewFixture fixture = CreateR2("spatial.room.large_chamber", new TileCoordinate(4, 1));
            RoomSpatialInstance target = fixture.State.Floors[0].Layout.Rooms.Single(value =>
                value.RoomInstanceId == "compat.floor.00.room.player.0000");
            FloorRouteNode node = fixture.State.Floors[0].Layout.Nodes.Single(value =>
                value.RoomInstanceId == target.RoomInstanceId);
            DetachedCanonicalMutationResult content = DetachedCanonicalSpatialMutation.Prepare(fixture.State,
                DetachedCanonicalMutationRequest.Place("placement.category.monster",
                    "placement.option.monster.skeleton", target.RoomInstanceId), fixture.Production,
                fixture.Compatibility, fixture.Configuration, fixture.Limits);
            Assert.That(content.IsSuccess, Is.True, content.Reason);
            RoomContentAssignment assignment = content.State.Floors[0].RoomContents.Assignments.Single();
            StructuralEditPreview preview = StructuralRenovationService.PreviewReplacement(content.State,
                new StructuralReplacementRequest { RoomInstanceId = target.RoomInstanceId,
                    RoomDefinitionId = "spatial.room.rectangle" }, fixture.Production,
                fixture.Compatibility, fixture.Configuration, fixture.Limits);
            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            SavedSpatialFloor proposed = preview.DetachedCandidate.Floors[0];
            RoomSpatialInstance replacement = proposed.Layout.Rooms.Single(value =>
                value.RoomInstanceId == target.RoomInstanceId);
            Assert.That(replacement.RoomDefinitionId, Is.EqualTo("spatial.room.rectangle"));
            Assert.That(replacement.Anchor, Is.EqualTo(target.Anchor));
            Assert.That(proposed.Layout.Nodes.Single(value => value.RoomInstanceId == target.RoomInstanceId).NodeId,
                Is.EqualTo(node.NodeId));
            Assert.That(proposed.RoomContents.Assignments.Single().AssignmentId, Is.EqualTo(assignment.AssignmentId));
            Assert.That(proposed.RoomContents.Assignments.Single().Sequence, Is.EqualTo(assignment.Sequence));
        }

        [Test]
        public void Replacement_SameDefinitionIsRejectedWithoutCandidateOrSourceMutation()
        {
            PreviewFixture fixture = CreateR2("spatial.room.large_chamber", new TileCoordinate(4, 1));
            string roomId = "compat.floor.00.room.player.0000";
            byte[] before = Bytes(fixture.State, fixture.Limits);

            StructuralEditPreview preview = Replace(fixture, roomId, "spatial.room.large_chamber");

            Assert.That(preview.IsValid, Is.False);
            Assert.That(preview.DetachedCandidate, Is.Null);
            Assert.That(preview.ReasonCodes, Is.EqualTo(new[]
                { StructuralEditService.ReplacementSameDefinitionReason }));
            Assert.That(preview.Consequences, Is.Empty);
            CollectionAssert.AreEqual(before, Bytes(fixture.State, fixture.Limits));

            StructuralEditPreview differentDefinition = Replace(fixture, roomId, "spatial.room.rectangle");
            Assert.That(differentDefinition.IsValid, Is.True,
                string.Join(",", differentDefinition.ReasonCodes));
        }

        [Test]
        public void Movement_ContentConsequencesIncludeOnlyMovedGroupAndLeaveUpstreamAssignmentUnchanged()
        {
            PreviewFixture fixture = CreateR2("spatial.room.basic", new TileCoordinate(4, 2));
            SavedSpatialFloor floor = fixture.State.Floors[0];
            string upstreamId = floor.Layout.Rooms.Single(value =>
                value.RoomInstanceId != "compat.floor.00.room.player.0000").RoomInstanceId;
            string movedId = "compat.floor.00.room.player.0000";
            fixture.State = Place(fixture, fixture.State, "placement.category.monster",
                "placement.option.monster.skeleton", upstreamId);
            fixture.State = Place(fixture, fixture.State, "placement.category.trap",
                "placement.option.trap.spike", movedId);
            RoomContentAssignment upstreamBefore = fixture.State.Floors[0].RoomContents.Assignments.Single(value =>
                value.RoomInstanceId == upstreamId);

            StructuralEditPreview preview = Move(fixture, movedId, new TileCoordinate(5, 2));

            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            RoomContentAssignment moved = fixture.State.Floors[0].RoomContents.Assignments.Single(value =>
                value.RoomInstanceId == movedId);
            CollectionAssert.AreEqual(new[] { moved.AssignmentId }, preview.PreservedAssignmentIds);
            Assert.That(preview.Consequences.Count(value => value.Kind == StructuralChangeKind.ContentPreserved),
                Is.EqualTo(1));
            RoomContentAssignment upstreamAfter = preview.DetachedCandidate.Floors[0].RoomContents.Assignments.Single(
                value => value.AssignmentId == upstreamBefore.AssignmentId);
            AssertAssignmentEqual(upstreamBefore, upstreamAfter);
        }

        [Test]
        public void Movement_UpstreamTargetListsTargetAndTranslatedDownstreamContentsInOrdinalOrder()
        {
            PreviewFixture fixture = CreateR2("spatial.room.basic", new TileCoordinate(5, 2));
            SavedSpatialFloor floor = fixture.State.Floors[0];
            string targetId = floor.Layout.Rooms.Single(value =>
                value.RoomInstanceId != "compat.floor.00.room.player.0000").RoomInstanceId;
            string downstreamId = "compat.floor.00.room.player.0000";
            fixture.State = Place(fixture, fixture.State, "placement.category.monster",
                "placement.option.monster.skeleton", targetId);
            fixture.State = Place(fixture, fixture.State, "placement.category.loot_node",
                "placement.option.loot_node.basic", downstreamId);
            SavedSpatialFloor before = fixture.State.Floors[0];
            RoomSpatialInstance targetBefore = before.Layout.Rooms.Single(value => value.RoomInstanceId == targetId);
            RoomSpatialInstance downstreamBefore = before.Layout.Rooms.Single(value => value.RoomInstanceId == downstreamId);
            SavedFixedSpatialStructure terminalBefore = before.FixedStructures.Single(value =>
                value.Kind == FixedSpatialStructureKind.CompletionTerminal);
            FloorRouteEdge internalEdgeBefore = before.Layout.Edges.Single(value =>
                value.SourceNodeId == before.Layout.Nodes.Single(node => node.RoomInstanceId == targetId).NodeId &&
                value.DestinationNodeId == before.Layout.Nodes.Single(node => node.RoomInstanceId == downstreamId).NodeId);

            StructuralEditPreview preview = Move(fixture, targetId,
                new TileCoordinate(targetBefore.Anchor.X, targetBefore.Anchor.Y + 1));

            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            SavedSpatialFloor after = preview.DetachedCandidate.Floors[0];
            var delta = new TileCoordinate(0, 1);
            Assert.That(after.Layout.Rooms.Single(value => value.RoomInstanceId == targetId).Anchor,
                Is.EqualTo(Add(targetBefore.Anchor, delta)));
            Assert.That(after.Layout.Rooms.Single(value => value.RoomInstanceId == downstreamId).Anchor,
                Is.EqualTo(Add(downstreamBefore.Anchor, delta)));
            Assert.That(after.FixedStructures.Single(value =>
                value.Kind == FixedSpatialStructureKind.CompletionTerminal).Anchor,
                Is.EqualTo(Add(terminalBefore.Anchor, delta)));
            FloorRouteEdge internalAfter = after.Layout.Edges.Single(value => value.EdgeId == internalEdgeBefore.EdgeId);
            CollectionAssert.AreEqual(internalEdgeBefore.Footprint.OccupiedTiles.Select(value => Add(value, delta)),
                internalAfter.Footprint.OccupiedTiles);
            CollectionAssert.AreEqual(before.RoomContents.Assignments.Select(value => value.AssignmentId)
                    .OrderBy(value => value, System.StringComparer.Ordinal), preview.PreservedAssignmentIds);
            Assert.That(Delta(targetBefore.Anchor, downstreamBefore.Anchor),
                Is.EqualTo(Delta(after.Layout.Rooms.Single(value => value.RoomInstanceId == targetId).Anchor,
                    after.Layout.Rooms.Single(value => value.RoomInstanceId == downstreamId).Anchor)));
        }

        [Test]
        public void Movement_BoundaryConnectionConvertsDirectAndCorridorWithoutChangingEdgeIdentity()
        {
            PreviewFixture directFixture = CreateR2("spatial.room.basic", new TileCoordinate(4, 2));
            SavedSpatialFloor directFloor = directFixture.State.Floors[0];
            string targetId = "compat.floor.00.room.player.0000";
            FloorRouteEdge boundary = IncomingEdge(directFloor, targetId);
            Assert.That(boundary.ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.DirectDoorway));
            StructuralEditPreview toCorridor = Move(directFixture, targetId, new TileCoordinate(5, 2));
            Assert.That(toCorridor.IsValid, Is.True, string.Join(",", toCorridor.ReasonCodes));
            FloorRouteEdge corridor = toCorridor.DetachedCandidate.Floors[0].Layout.Edges.Single(value =>
                value.EdgeId == boundary.EdgeId);
            Assert.That(corridor.ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.PhysicalCorridor));
            Assert.That(corridor.Footprint.OccupiedTiles.Length, Is.EqualTo(1));

            PreviewFixture corridorFixture = CreateR2("spatial.room.basic", new TileCoordinate(5, 2));
            FloorRouteEdge oldCorridor = IncomingEdge(corridorFixture.State.Floors[0], targetId);
            StructuralEditPreview toDirect = Move(corridorFixture, targetId, new TileCoordinate(4, 2));
            Assert.That(toDirect.IsValid, Is.True, string.Join(",", toDirect.ReasonCodes));
            FloorRouteEdge direct = toDirect.DetachedCandidate.Floors[0].Layout.Edges.Single(value =>
                value.EdgeId == oldCorridor.EdgeId);
            Assert.That(direct.ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.DirectDoorway));
            Assert.That(direct.Footprint, Is.Null);
        }

        [TestCase(5, 2, 1)]
        [TestCase(8, 2, 4)]
        public void Movement_UsesProductionCorridorLengthBoundaries(int x, int y, int length)
        {
            PreviewFixture fixture = CreateR2("spatial.room.basic", new TileCoordinate(4, 2));
            StructuralEditPreview preview = Move(fixture, "compat.floor.00.room.player.0000",
                new TileCoordinate(x, y));
            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            Assert.That(preview.IncomingConnectionTiles.Length, Is.EqualTo(length));
        }

        [Test]
        public void Movement_AboveMaximumCorridorLengthUsesInBoundsTestOwnedFloorGeometry()
        {
            PreviewFixture fixture = CreateR2("spatial.room.basic", new TileCoordinate(4, 2));
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            catalog.Floors[0].Bounds.Width += 4;
            ProductionSpatialContentSnapshot production = Snapshot(fixture, catalog);
            AssertMoveInvalidUnchanged(fixture, new TileCoordinate(9, 2),
                StructuralEditService.CorridorLengthReason, production);
        }

        [Test]
        public void Movement_RepeatedPreviewIsByteAndConsequenceDeterministic()
        {
            PreviewFixture fixture = CreateR2("spatial.room.basic", new TileCoordinate(4, 2));
            StructuralEditPreview first = Move(fixture, "compat.floor.00.room.player.0000",
                new TileCoordinate(5, 2));
            StructuralEditPreview second = Move(fixture, "compat.floor.00.room.player.0000",
                new TileCoordinate(5, 2));
            CollectionAssert.AreEqual(Bytes(first.DetachedCandidate, fixture.Limits),
                Bytes(second.DetachedCandidate, fixture.Limits));
            CollectionAssert.AreEqual(first.Consequences.Select(ConsequenceKey),
                second.Consequences.Select(ConsequenceKey));
        }

        [Test]
        public void Movement_InvalidBoundsOverlapAndCapacityNeverMutateSource()
        {
            PreviewFixture fixture = CreateR2("spatial.room.basic", new TileCoordinate(4, 2));
            AssertMoveInvalidUnchanged(fixture, new TileCoordinate(20, 20), StructuralEditService.OutOfBoundsReason);
            AssertMoveInvalidUnchanged(fixture, fixture.State.Floors[0].Layout.Rooms.Single(value =>
                value.RoomInstanceId != "compat.floor.00.room.player.0000").Anchor,
                StructuralEditService.RoomOverlapReason);

        }

        [Test]
        public void Movement_FloorCapacityFailureUsesIsolatedTestOwnedSnapshot()
        {
            PreviewFixture fixture = CreateR2("spatial.room.basic", new TileCoordinate(4, 2));
            FloorLayoutValidationResult before = Validate(fixture);
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            catalog.Floors[0].FinalFloorSpaceCapacity = before.Capacity.UsedFloorSpaceCapacity;
            AssertMoveInvalidUnchanged(fixture, new TileCoordinate(5, 2),
                StructuralEditService.CapacityReason, Snapshot(fixture, catalog));
        }

        [Test]
        public void Movement_FixedAndPhysicalCorridorOverlapsFailWithoutMutation()
        {
            PreviewFixture fixedOverlap = CreateR2("spatial.room.basic", new TileCoordinate(4, 2));
            fixedOverlap.State.Floors[0].FixedStructures.Single(value =>
                value.Kind == FixedSpatialStructureKind.Entrance).Anchor = new TileCoordinate(5, 0);
            AssertMoveInvalidUnchanged(fixedOverlap, new TileCoordinate(5, 0),
                StructuralEditService.FixedOverlapReason);

            PreviewFixture corridorOverlap = CreateR2("spatial.room.basic", new TileCoordinate(4, 2));
            SavedSpatialFloor floor = corridorOverlap.State.Floors[0];
            FloorRouteNode room = floor.Layout.Nodes.Single(value =>
                value.RoomInstanceId == "compat.floor.00.room.player.0000");
            FloorRouteNode completion = floor.Layout.Nodes.Single(value =>
                value.Kind == FloorRouteNodeKind.Completion);
            floor.Layout.Edges = floor.Layout.Edges.Concat(new[] { new FloorRouteEdge
            {
                EdgeId = "test.optional.corridor", FloorId = floor.FloorInstanceId,
                SourceNodeId = room.NodeId, DestinationNodeId = completion.NodeId,
                CorridorDefinitionId = "spatial.corridor.straight_stone",
                ConnectionKind = FloorRouteConnectionKind.PhysicalCorridor,
                Classification = RouteClassification.Optional, OptionalBranchId = "test.branch",
                Footprint = new ResolvedTileFootprint(new[] { new TileCoordinate(8, 3) })
            } }).ToArray();
            AssertMoveInvalidUnchanged(corridorOverlap, new TileCoordinate(8, 2),
                StructuralEditService.CorridorOverlapReason);
        }

        [Test]
        public void Movement_UnsupportedCorridorSocketIsConnectionUnavailable()
        {
            PreviewFixture fixture = CreateR2("spatial.room.basic", new TileCoordinate(4, 2));
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            catalog.Corridors[0].CompatibleSocketTypeIds = System.Array.Empty<string>();
            AssertMoveInvalidUnchanged(fixture, new TileCoordinate(5, 2),
                StructuralEditService.ConnectionUnavailableReason, Snapshot(fixture, catalog));
        }

        [Test]
        public void Movement_DuplicateConnectionPairIsAmbiguous()
        {
            PreviewFixture fixture = CreateR2("spatial.room.basic", new TileCoordinate(4, 2));
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            RoomSpatialDefinition basic = catalog.Rooms.Single(value =>
                value.RoomDefinitionId == "spatial.room.basic");
            SpatialConnectionPointDefinition west = basic.ConnectionPoints.Single(value => value.ConnectionPointId == "west");
            basic.ConnectionPoints = basic.ConnectionPoints.Concat(new[] { new SpatialConnectionPointDefinition
            { ConnectionPointId = "test.duplicate.west", Offset = west.Offset, Facing = west.Facing,
              SocketTypeId = west.SocketTypeId } }).ToArray();
            AssertMoveInvalidUnchanged(fixture, new TileCoordinate(5, 2),
                StructuralEditService.ConnectionAmbiguousReason, Snapshot(fixture, catalog));
        }

        [Test]
        public void Movement_MultipleApprovedCorridorDefinitionsAreInvalid()
        {
            PreviewFixture fixture = CreateR2("spatial.room.basic", new TileCoordinate(4, 2));
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            CorridorSpatialDefinition original = catalog.Corridors[0];
            catalog.Corridors = catalog.Corridors.Concat(new[]
            { new CorridorSpatialDefinition { CorridorDefinitionId = "test.corridor.second",
              Category = CorridorSpatialCategory.Straight, MinimumLength = original.MinimumLength,
              MaximumLength = original.MaximumLength, Width = original.Width,
              AllowedOrientations = original.AllowedOrientations,
              CompatibleSocketTypeIds = original.CompatibleSocketTypeIds } }).ToArray();
            catalog.Floors[0].AllowedCorridorDefinitionIds =
                catalog.Floors[0].AllowedCorridorDefinitionIds.Concat(
                    new[] { "test.corridor.second" }).ToArray();
            AssertMoveInvalidUnchanged(fixture, new TileCoordinate(5, 2),
                StructuralEditService.CorridorDefinitionReason, Snapshot(fixture, catalog));
        }

        [Test]
        public void Replacement_ContentConsequencesAreTargetScopedAndCandidateRetainsUnrelatedAssignment()
        {
            PreviewFixture fixture = CreateR2("spatial.room.large_chamber", new TileCoordinate(4, 1));
            SavedSpatialFloor floor = fixture.State.Floors[0];
            string targetId = "compat.floor.00.room.player.0000";
            string upstreamId = floor.Layout.Rooms.Single(value => value.RoomInstanceId != targetId).RoomInstanceId;
            fixture.State = Place(fixture, fixture.State, "placement.category.monster",
                "placement.option.monster.skeleton", upstreamId);
            fixture.State = Place(fixture, fixture.State, "placement.category.trap",
                "placement.option.trap.spike", targetId);
            RoomContentAssignment upstream = fixture.State.Floors[0].RoomContents.Assignments.Single(value =>
                value.RoomInstanceId == upstreamId);
            RoomContentAssignment target = fixture.State.Floors[0].RoomContents.Assignments.Single(value =>
                value.RoomInstanceId == targetId);

            StructuralEditPreview preview = Replace(fixture, targetId, "spatial.room.rectangle");

            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            CollectionAssert.AreEqual(new[] { target.AssignmentId }, preview.PreservedAssignmentIds);
            AssertAssignmentEqual(upstream, preview.DetachedCandidate.Floors[0].RoomContents.Assignments.Single(value =>
                value.AssignmentId == upstream.AssignmentId));
        }

        [Test]
        public void Replacement_TranslatedDownstreamRoomContentIsDisclosed()
        {
            PreviewFixture fixture = CreateR2("spatial.room.large_chamber", new TileCoordinate(4, 1));
            SavedSpatialFloor floor = fixture.State.Floors[0];
            string targetId = floor.Layout.Rooms.Single(value =>
                value.RoomInstanceId != "compat.floor.00.room.player.0000").RoomInstanceId;
            string downstreamId = "compat.floor.00.room.player.0000";
            fixture.State = Place(fixture, fixture.State, "placement.category.monster",
                "placement.option.monster.skeleton", targetId);
            fixture.State = Place(fixture, fixture.State, "placement.category.trap",
                "placement.option.trap.spike", downstreamId);
            RoomSpatialInstance downstreamBefore = fixture.State.Floors[0].Layout.Rooms.Single(value =>
                value.RoomInstanceId == downstreamId);
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            RoomSpatialDefinition downstreamDefinition = catalog.Rooms.Single(value =>
                value.RoomDefinitionId == "spatial.room.large_chamber");
            downstreamDefinition.ConnectionPoints = downstreamDefinition.ConnectionPoints.Where(value =>
                value.ConnectionPointId != "east").ToArray();

            StructuralEditPreview preview = Replace(fixture, targetId, "spatial.room.rectangle",
                Snapshot(fixture, catalog));

            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            Assert.That(preview.DetachedCandidate.Floors[0].Layout.Rooms.Single(value =>
                value.RoomInstanceId == downstreamId).Anchor, Is.Not.EqualTo(downstreamBefore.Anchor));
            CollectionAssert.AreEqual(fixture.State.Floors[0].RoomContents.Assignments
                .Select(value => value.AssignmentId).OrderBy(value => value, System.StringComparer.Ordinal),
                preview.PreservedAssignmentIds);
        }

        [Test]
        public void Replacement_PreservesEveryIdentityAndIsDeterministic()
        {
            PreviewFixture fixture = CreateR2("spatial.room.large_chamber", new TileCoordinate(4, 1));
            string targetId = "compat.floor.00.room.player.0000";
            fixture.State = Place(fixture, fixture.State, "placement.category.monster",
                "placement.option.monster.skeleton", targetId);
            SavedSpatialFloor before = fixture.State.Floors[0];
            string[] edgeIds = before.Layout.Edges.Select(value => value.EdgeId).OrderBy(value => value).ToArray();
            RoomContentAssignment assignment = before.RoomContents.Assignments.Single();

            StructuralEditPreview first = Replace(fixture, targetId, "spatial.room.rectangle");
            StructuralEditPreview second = Replace(fixture, targetId, "spatial.room.rectangle");

            Assert.That(first.IsValid, Is.True, string.Join(",", first.ReasonCodes));
            SavedSpatialFloor after = first.DetachedCandidate.Floors[0];
            CollectionAssert.AreEqual(edgeIds, after.Layout.Edges.Select(value => value.EdgeId).OrderBy(value => value));
            AssertAssignmentEqual(assignment, after.RoomContents.Assignments.Single());
            CollectionAssert.AreEqual(Bytes(first.DetachedCandidate, fixture.Limits),
                Bytes(second.DetachedCandidate, fixture.Limits));
            CollectionAssert.AreEqual(first.Consequences.Select(ConsequenceKey),
                second.Consequences.Select(ConsequenceKey));
            Assert.That(first.Consequences.Count(value => value.Kind == StructuralChangeKind.EdgeReconnected),
                Is.EqualTo(2));
        }

        [Test]
        public void Replacement_CapacityFailureDoesNotMutateSource()
        {
            PreviewFixture capacity = CreateR2("spatial.room.large_chamber", new TileCoordinate(4, 1));
            string targetId = "compat.floor.00.room.player.0000";
            capacity.State = Place(capacity, capacity.State, "placement.category.trap",
                "placement.option.trap.spike", targetId);
            capacity.State = Place(capacity, capacity.State, "placement.category.trap",
                "placement.option.trap.snare", targetId);
            AssertReplaceInvalidUnchanged(capacity, targetId, "spatial.room.rectangle",
                StructuralEditService.ContentCapacityReason);

        }

        [Test]
        public void Replacement_OrientationFailureUsesIsolatedTestOwnedSnapshot()
        {
            PreviewFixture fixture = CreateR2("spatial.room.large_chamber", new TileCoordinate(4, 1));
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            catalog.Rooms.Single(value => value.RoomDefinitionId ==
                "spatial.room.rectangle").AllowedOrientations = new[] { CardinalOrientation.Ninety };
            AssertReplaceInvalidUnchanged(fixture, "compat.floor.00.room.player.0000",
                "spatial.room.rectangle", StructuralEditService.OrientationInvalidReason,
                Snapshot(fixture, catalog));
        }

        [Test]
        public void Replacement_MissingConnectionPointFailsWithoutMutation()
        {
            PreviewFixture fixture = CreateR2("spatial.room.large_chamber", new TileCoordinate(4, 1));
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            RoomSpatialDefinition replacement = catalog.Rooms.Single(value =>
                value.RoomDefinitionId == "spatial.room.rectangle");
            replacement.ConnectionPoints = replacement.ConnectionPoints.Where(value =>
                value.ConnectionPointId != "north").ToArray();
            AssertReplaceInvalidUnchanged(fixture, "compat.floor.00.room.player.0000",
                "spatial.room.rectangle", StructuralEditService.ConnectionAmbiguousReason,
                Snapshot(fixture, catalog));
        }

        [Test]
        public void ReplacementPreviewBecomesStaleAfterInterveningTrapPlacement()
        {
            PreviewFixture fixture = CreateR2("spatial.room.large_chamber", new TileCoordinate(4, 1));
            string targetId = "compat.floor.00.room.player.0000";
            StructuralEditPreview preview = Replace(fixture, targetId, "spatial.room.rectangle");
            DetachedCanonicalSpatialSaveState changed = Place(fixture, fixture.State, "placement.category.trap",
                "placement.option.trap.spike", targetId);
            DetachedCanonicalMutationResult stale = DetachedCanonicalSpatialMutation.Prepare(changed,
                DetachedCanonicalMutationRequest.Replace(preview), fixture.Production, fixture.Compatibility,
                fixture.Configuration, fixture.Limits);
            Assert.That(stale.IsSuccess, Is.False);
            Assert.That(stale.Reason, Is.EqualTo(StructuralEditService.StalePreviewReason));
        }

        [Test]
        public void RenovationMutationsRoundTripCanonicalStateWithoutSchemaOrLegacyMutation()
        {
            PreviewFixture movementFixture = CreateR2("spatial.room.basic", new TileCoordinate(4, 2));
            string roomId = "compat.floor.00.room.player.0000";
            movementFixture.State = Place(movementFixture, movementFixture.State, "placement.category.monster",
                "placement.option.monster.skeleton", roomId);
            StructuralEditPreview movement = Move(movementFixture, roomId, new TileCoordinate(5, 2));
            DetachedCanonicalMutationResult moved = DetachedCanonicalSpatialMutation.Prepare(movementFixture.State,
                DetachedCanonicalMutationRequest.Move(movement), movementFixture.Production,
                movementFixture.Compatibility, movementFixture.Configuration, movementFixture.Limits);
            Assert.That(moved.IsSuccess, Is.True, moved.Reason);
            SpatialContractResult<DetachedCanonicalSpatialSaveState> movedReopened =
                CanonicalSpatialSaveSerializer.Parse(Bytes(moved.State, movementFixture.Limits), movementFixture.Limits);
            Assert.That(movedReopened.IsValid, Is.True);
            Assert.That(movedReopened.Value.Floors[0].Layout.Rooms.Single(value =>
                value.RoomInstanceId == roomId).Anchor, Is.EqualTo(new TileCoordinate(5, 2)));
            CollectionAssert.AreEqual(moved.State.Floors[0].Layout.Edges.Select(value => value.EdgeId),
                movedReopened.Value.Floors[0].Layout.Edges.Select(value => value.EdgeId));
            AssertAssignmentEqual(moved.State.Floors[0].RoomContents.Assignments.Single(),
                movedReopened.Value.Floors[0].RoomContents.Assignments.Single());

            PreviewFixture replacementFixture = CreateR2("spatial.room.large_chamber", new TileCoordinate(4, 1));
            replacementFixture.State = Place(replacementFixture, replacementFixture.State,
                "placement.category.monster", "placement.option.monster.skeleton", roomId);
            StructuralEditPreview replacement = Replace(replacementFixture, roomId, "spatial.room.rectangle");
            DetachedCanonicalMutationResult replaced = DetachedCanonicalSpatialMutation.Prepare(
                replacementFixture.State, DetachedCanonicalMutationRequest.Replace(replacement),
                replacementFixture.Production, replacementFixture.Compatibility,
                replacementFixture.Configuration, replacementFixture.Limits);
            Assert.That(replaced.IsSuccess, Is.True, replaced.Reason);
            SpatialContractResult<DetachedCanonicalSpatialSaveState> replacedReopened =
                CanonicalSpatialSaveSerializer.Parse(Bytes(replaced.State, replacementFixture.Limits),
                    replacementFixture.Limits);
            Assert.That(replacedReopened.Value.Floors[0].Layout.Rooms.Single(value =>
                value.RoomInstanceId == roomId).RoomDefinitionId, Is.EqualTo("spatial.room.rectangle"));
            AssertAssignmentEqual(replaced.State.Floors[0].RoomContents.Assignments.Single(),
                replacedReopened.Value.Floors[0].RoomContents.Assignments.Single());
            Assert.That(DetachedWholeSaveCandidateSerializer.TargetSchemaVersion, Is.EqualTo(7));
        }

        private static StructuralEditPreview Move(PreviewFixture fixture, string roomId, TileCoordinate anchor,
            ProductionSpatialContentSnapshot production = null) =>
            StructuralRenovationService.PreviewMovement(fixture.State,
                new StructuralMovementRequest { RoomInstanceId = roomId, Anchor = anchor },
                production ?? fixture.Production, fixture.Compatibility, fixture.Configuration, fixture.Limits);

        private static StructuralEditPreview Replace(PreviewFixture fixture, string roomId, string definitionId,
            ProductionSpatialContentSnapshot production = null) =>
            StructuralRenovationService.PreviewReplacement(fixture.State,
                new StructuralReplacementRequest { RoomInstanceId = roomId, RoomDefinitionId = definitionId },
                production ?? fixture.Production, fixture.Compatibility, fixture.Configuration, fixture.Limits);

        private static DetachedCanonicalSpatialSaveState Place(PreviewFixture fixture,
            DetachedCanonicalSpatialSaveState state, string category, string option, string roomId)
        {
            DetachedCanonicalMutationResult result = DetachedCanonicalSpatialMutation.Prepare(state,
                DetachedCanonicalMutationRequest.Place(category, option, roomId), fixture.Production,
                fixture.Compatibility, fixture.Configuration, fixture.Limits);
            Assert.That(result.IsSuccess, Is.True, result.Reason);
            return result.State;
        }

        private static void AssertMoveInvalidUnchanged(PreviewFixture fixture, TileCoordinate anchor,
            string expectedReason, ProductionSpatialContentSnapshot production = null)
        {
            byte[] before = Bytes(fixture.State, fixture.Limits);
            StructuralEditPreview preview = Move(fixture, "compat.floor.00.room.player.0000", anchor, production);
            Assert.That(preview.IsValid, Is.False);
            Assert.That(preview.ReasonCodes, Is.EqualTo(new[] { expectedReason }));
            CollectionAssert.AreEqual(before, Bytes(fixture.State, fixture.Limits));
        }

        private static void AssertReplaceInvalidUnchanged(PreviewFixture fixture, string roomId,
            string definitionId, string expectedReason, ProductionSpatialContentSnapshot production = null)
        {
            byte[] before = Bytes(fixture.State, fixture.Limits);
            StructuralEditPreview preview = Replace(fixture, roomId, definitionId, production);
            Assert.That(preview.IsValid, Is.False);
            Assert.That(preview.ReasonCodes, Is.EqualTo(new[] { expectedReason }));
            CollectionAssert.AreEqual(before, Bytes(fixture.State, fixture.Limits));
        }

        private static byte[] Bytes(DetachedCanonicalSpatialSaveState state,
            CanonicalSpatialSerializationLimits limits)
        {
            SpatialContractResult<byte[]> result = CanonicalSpatialSaveSerializer.Serialize(state, limits);
            Assert.That(result.IsValid, Is.True);
            return result.Value;
        }

        private static FloorRouteEdge IncomingEdge(SavedSpatialFloor floor, string roomId)
        {
            FloorRouteNode node = floor.Layout.Nodes.Single(value => value.RoomInstanceId == roomId);
            return floor.Layout.Edges.Single(value => value.DestinationNodeId == node.NodeId &&
                value.Classification == RouteClassification.Required);
        }

        private static FloorLayoutValidationResult Validate(PreviewFixture fixture) =>
            FloorLayoutValidator.Validate(fixture.State.Floors[0].Layout,
                fixture.Production.Catalog.Floors.Single(), fixture.Production.Catalog.Rooms,
                fixture.Production.Catalog.Corridors,
                new SpatialValidationWorkloadLimits(fixture.Limits.Spatial.MaximumMaterializedTiles),
                fixture.State.Floors[0].FixedStructures, fixture.Production.Catalog.FixedStructures);

        private static string ConsequenceKey(StructuralChange value) => string.Join("|",
            ((int)value.Kind).ToString(), value.StableId, Coordinate(value.From), Coordinate(value.To),
            ((int)value.PreviousConnectionKind).ToString(), ((int)value.ProposedConnectionKind).ToString(),
            string.Join(",", value.PreviousFootprint.Select(Coordinate)),
            string.Join(",", value.ProposedFootprint.Select(Coordinate)));

        private static string Coordinate(TileCoordinate value) =>
            value.X.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
            value.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);

        private static ProductionSpatialContentSnapshot Snapshot(PreviewFixture fixture,
            SpatialContentCatalog catalog) => new ProductionSpatialContentSnapshot(
                fixture.Production.Manifest, catalog, fixture.Production.Languages);

        private static void AssertAssignmentEqual(RoomContentAssignment expected, RoomContentAssignment actual)
        {
            Assert.That(actual.AssignmentId, Is.EqualTo(expected.AssignmentId));
            Assert.That(actual.RoomInstanceId, Is.EqualTo(expected.RoomInstanceId));
            Assert.That(actual.CategoryId, Is.EqualTo(expected.CategoryId));
            Assert.That(actual.OptionId, Is.EqualTo(expected.OptionId));
            Assert.That(actual.Sequence, Is.EqualTo(expected.Sequence));
        }

        private static TileCoordinate Add(TileCoordinate a, TileCoordinate b) =>
            new TileCoordinate(a.X + b.X, a.Y + b.Y);

        private static TileCoordinate Delta(TileCoordinate from, TileCoordinate to) =>
            new TileCoordinate(to.X - from.X, to.Y - from.Y);

        private sealed class PreviewFixture
        {
            internal DetachedCanonicalSpatialSaveState State;
            internal ProductionSpatialContentSnapshot Production;
            internal SpatialLayoutCompatibilitySnapshot Compatibility;
            internal RunSimulationConfig Configuration;
            internal CanonicalSpatialSerializationLimits Limits;
        }

        private static PreviewFixture CreateR1()
        {
            var source = Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            DetachedCompleteSaveValidationResult parsed = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                source.Result.Attempt.Candidate.GetBytes(), new DetachedCurrentTargetValidationContext(
                    source.Compatibility, source.Production, source.LegacyBytes, source.Limits));
            RunSimulationConfig configuration = LegacyGameplayConfigurationContract.Parse(source.LegacyBytes);
            DetachedCanonicalMutationResult r1 = DetachedCanonicalSpatialMutation.Prepare(parsed.State,
                DetachedCanonicalMutationRequest.Place("placement.category.room", "placement.option.room.basic"),
                source.Production, source.Compatibility, configuration, source.Limits);
            Assert.That(r1.IsSuccess, Is.True, r1.Reason);
            return new PreviewFixture { State = r1.State, Production = source.Production,
                Compatibility = source.Compatibility, Configuration = configuration, Limits = source.Limits };
        }

        private static PreviewFixture CreateR2()
        {
            return CreateR2("spatial.room.basic", new TileCoordinate(0, 6));
        }

        private static PreviewFixture CreateR2(string definitionId, TileCoordinate anchor)
        {
            PreviewFixture fixture = CreateR1();
            StructuralEditPreview construction = StructuralEditService.Preview(fixture.State,
                new StructuralConstructionRequest { RoomDefinitionId = definitionId,
                    Anchor = anchor, Orientation = CardinalOrientation.Zero,
                    TerminalConnectionPointId = "north" }, fixture.Production, fixture.Compatibility,
                fixture.Configuration, fixture.Limits);
            Assert.That(construction.IsValid, Is.True, string.Join(",", construction.ReasonCodes));
            fixture.State = construction.DetachedCandidate;
            return fixture;
        }

        private static void AssertInvalidUnchanged(PreviewFixture fixture,
            StructuralConstructionRequest request, ProductionSpatialContentSnapshot production,
            CanonicalSpatialSerializationLimits limits, string expectedReason)
        {
            SpatialContractResult<byte[]> before = CanonicalSpatialSaveSerializer.Serialize(fixture.State, fixture.Limits);
            Assert.That(before.IsValid, Is.True);
            StructuralEditPreview preview = StructuralEditService.Preview(fixture.State, request,
                production, fixture.Compatibility, fixture.Configuration, limits);
            Assert.That(preview.IsValid, Is.False);
            Assert.That(preview.DetachedCandidate, Is.Null);
            Assert.That(preview.ReasonCodes.Length, Is.EqualTo(1));
            Assert.That(preview.ReasonCodes[0], Is.EqualTo(expectedReason));
            SpatialContractResult<byte[]> after = CanonicalSpatialSaveSerializer.Serialize(fixture.State, fixture.Limits);
            Assert.That(after.IsValid, Is.True);
            CollectionAssert.AreEqual(before.Value, after.Value);
        }
    }
}
#endif
