#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class PhaseFiveADurableBranchTests
    {
        [Test]
        public void SchemaNineUpgradesOnceToTenWithEmptyOwnersAndFrozenBytesOtherwisePreserved()
        {
            var fixture = DetachedCanonicalWriteAuthorityTests.Fixture.Create(null);
            string ten = Encoding.UTF8.GetString(fixture.Session.GetCurrentBytes());
            int owners = ten.IndexOf(",\"corridorContent\":", StringComparison.Ordinal);
            string nineText = ten.Remove(owners, ten.Length - 2 - owners)
                .Replace("\"schemaVersion\":10", "\"schemaVersion\":9");
            byte[] nine = Encoding.UTF8.GetBytes(nineText);
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaNineAndRoundTrip(
                nine, fixture.Profile.Canonical).IsValid, Is.True);
            Assert.That(SchemaNineToTenUpgrade.TryPrepare(nine, fixture.Profile.Canonical,
                out byte[] upgraded), Is.True);
            string current = Encoding.UTF8.GetString(upgraded);
            Assert.That(current, Does.Contain("\"schemaVersion\":10"));
            Assert.That(current, Does.Contain("\"corridorContent\":{\"Assignments\":[]}"));
            Assert.That(current, Does.Contain("\"sharedBranchKnowledge\":{\"Records\":[]}"));
            Assert.That(current.Replace("\"schemaVersion\":10", "\"schemaVersion\":9")
                .Replace(",\"corridorContent\":{\"Assignments\":[]},\"sharedBranchKnowledge\":{\"Records\":[]}", ""),
                Is.EqualTo(nineText));
            Assert.That(SchemaNineToTenUpgrade.TryPrepare(upgraded, fixture.Profile.Canonical, out _), Is.False);
        }

        [Test]
        public void FrozenSchemasRejectDeadEndWhileSchemaTenAcceptsItAndMigrationCannotSmuggleIt()
        {
            var fixture = BranchFixture(2, out OptionalBranchEditPreview preview);
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.ConstructBranch(preview)));
            byte[] ten = fixture.Session.GetCurrentBytes();
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(ten,
                fixture.Context).IsValid, Is.True);
            Assert.That(Encoding.UTF8.GetString(ten), Does.Contain("\"Kind\":6"));

            byte[] nine = FrozenSchemaWithDeadEnd(ten, 9);
            byte[] eight = FrozenSchemaWithDeadEnd(ten, 8);
            byte[] seven = FrozenSchemaWithDeadEnd(ten, 7);
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaNineAndRoundTrip(
                nine, fixture.Profile.Canonical).IsValid, Is.False);
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaEightAndRoundTrip(
                eight, fixture.Profile.Canonical).IsValid, Is.False);
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaSevenAndRoundTrip(
                seven, fixture.Profile.Canonical).IsValid, Is.False);
            Assert.That(SchemaNineToTenUpgrade.TryPrepare(nine, fixture.Profile.Canonical, out _), Is.False);
            Assert.That(SchemaEightToNineUpgrade.TryPrepare(eight, fixture.Profile.Canonical, out _), Is.False);
            Assert.That(SchemaSevenToEightUpgrade.TryPrepare(seven, fixture.Profile.Canonical, out _), Is.False);
        }

        [Test]
        public void ResearchGateUsesAc300EffectAndFloorBound()
        {
            var fixture = DetachedCanonicalWriteAuthorityTests.Fixture.Create(null);
            FloorSpatialConfiguration floor = fixture.Production.Catalog.Floors.Single();
            BasicBranchingAllowanceResult locked = BasicBranchingResearchAuthority.Resolve(
                new CompletedResearchState(), fixture.BranchingResearch, floor);
            Assert.That(locked.IsResolved, Is.True);
            Assert.That(locked.EffectiveAllowance, Is.Zero);
            Assert.That(locked.Reason, Is.EqualTo(BasicBranchingResearchAuthority.ResearchRequiredReason));
            var completed = new CompletedResearchState { ProjectIds = new[] { BasicBranchingResearchAuthority.ResearchId } };
            BasicBranchingAllowanceResult allowed = BasicBranchingResearchAuthority.Resolve(
                completed, fixture.BranchingResearch, floor);
            Assert.That(allowed.IsResolved, Is.True);
            Assert.That(allowed.EffectiveAllowance, Is.EqualTo(Math.Min(
                fixture.BranchingResearch.AllowanceContribution, floor.OptionalBranchAllowance)));
            Assert.That(BasicBranchingResearchAuthority.Resolve(completed, null, floor).IsResolved, Is.False);
        }

        [Test]
        public void BranchConstructionEconomyPreviewCoversAffordabilityAndDoesNotMutateState()
        {
            var fixture = BranchFixture(2, out OptionalBranchEditPreview spatial);
            string before = JsonUtility.ToJson(fixture.State);
            double wallet = fixture.Runtime.structureRuntime.ManaReserve;
            StructuralInvestmentRecord[] investment = Current(fixture).Investment;
            Assert.That(fixture.Economy.TryCorridor(OptionalBranchStructuralEditService.CorridorDefinitionId,
                out double perTile), Is.True);
            double expected = perTile * spatial.OccupiedTiles.Length;

            StructuralEconomyPreview affordable = StructuralEconomyService.Preview(spatial,
                fixture.State, investment, expected + 1d, fixture.Economy);
            Assert.That(affordable.BaseCost, Is.EqualTo(expected));
            Assert.That(affordable.Cost, Is.EqualTo(expected));
            Assert.That(affordable.CurrentMana, Is.EqualTo(expected + 1d));
            Assert.That(affordable.ResultingMana, Is.EqualTo(1d));
            Assert.That(affordable.IsAffordable, Is.True);

            StructuralEconomyPreview exact = StructuralEconomyService.Preview(spatial,
                fixture.State, investment, expected, fixture.Economy);
            Assert.That(exact.IsAffordable, Is.True);
            Assert.That(exact.ResultingMana, Is.Zero);

            StructuralEconomyPreview insufficient = StructuralEconomyService.Preview(spatial,
                fixture.State, investment, expected - 1d, fixture.Economy);
            spatial.Economy = insufficient;
            Assert.That(insufficient.IsAffordable, Is.False);
            Assert.That(insufficient.Reason, Is.EqualTo(StructuralEconomyService.InsufficientReason));
            Assert.That(spatial.IsSpatiallyValid, Is.True);
            Assert.That(spatial.IsCommittable, Is.False);
            Assert.That(JsonUtility.ToJson(fixture.State), Is.EqualTo(before));
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(wallet));

            OptionalBranchEditPreview failed = OptionalBranchStructuralEditService.PreviewConstruction(
                fixture.State, new OptionalBranchConstructionRequest { FloorInstanceId = "missing" },
                fixture.Runtime.completedResearch, fixture.BranchingResearch, fixture.Production,
                fixture.Configuration, fixture.Profile.Canonical);
            StructuralEconomyPreview failedEconomy = StructuralEconomyService.Preview(failed,
                fixture.State, investment, wallet, fixture.Economy);
            Assert.That(failedEconomy.IsAffordable, Is.False);
            Assert.That(JsonUtility.ToJson(fixture.State), Is.EqualTo(before));
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(wallet));
        }

        [Test]
        public void BranchRemovalEconomyPreviewUsesHistoricalInvestmentFlooringAndCapacity()
        {
            var fixture = BranchFixture(2, out OptionalBranchEditPreview construction);
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.ConstructBranch(construction)));
            FloorRouteEdge edge = fixture.State.Floors[0].Layout.Edges.Single(value =>
                value.Classification == RouteClassification.Optional);
            OptionalBranchEditPreview removal = OptionalBranchStructuralEditService.PreviewRemoval(
                fixture.State, fixture.Runtime.corridorContent, new OptionalBranchRemovalRequest
                { FloorInstanceId = edge.FloorId, OptionalBranchId = edge.OptionalBranchId },
                fixture.Production, fixture.Configuration, fixture.Profile.Canonical);
            StructuralInvestmentRecord[] investment = Current(fixture).Investment
                .Select(value => value.Copy()).ToArray();
            investment.Single(value => value.StructureId == edge.EdgeId).ConstructionMana = 11d;

            StructuralEconomyPreview ordinary = StructuralEconomyService.Preview(removal,
                fixture.State, investment, 100d, fixture.Economy);
            Assert.That(ordinary.Operation, Is.EqualTo(StructuralEditOperation.OptionalBranchRemoval));
            Assert.That(ordinary.RefundBasis, Is.EqualTo(11d));
            Assert.That(ordinary.Refund, Is.EqualTo(Math.Floor(11d * fixture.Economy.RefundPercentage)));
            Assert.That(ordinary.CreditedRefund, Is.EqualTo(ordinary.Refund));
            Assert.That(ordinary.ResultingMana, Is.EqualTo(100d + ordinary.Refund));

            StructuralEconomyPreview capped = StructuralEconomyService.Preview(removal,
                fixture.State, investment, fixture.Economy.ManaCapacity - 4d, fixture.Economy);
            Assert.That(capped.RefundBasis, Is.EqualTo(11d));
            Assert.That(capped.Refund, Is.EqualTo(ordinary.Refund));
            Assert.That(capped.CreditedRefund, Is.EqualTo(4d));
            Assert.That(capped.ResultingMana, Is.EqualTo(fixture.Economy.ManaCapacity));
        }

        [Test]
        public void BranchConstructionContentCustodyDeletionAndReopenAreAtomic()
        {
            var fixture = BranchFixture(2, out OptionalBranchEditPreview preview);
            double before = fixture.Runtime.structureRuntime.ManaReserve;
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.ConstructBranch(preview)));
            fixture.Reopen();
            FloorRouteEdge edge = fixture.State.Floors[0].Layout.Edges.Single(value =>
                value.Classification == RouteClassification.Optional);
            Assert.That(edge.OptionalBranchId, Is.EqualTo(edge.EdgeId + ".branch"));
            Assert.That(edge.DestinationNodeId, Is.EqualTo(edge.EdgeId + ".dead-end"));
            Assert.That(fixture.State.Floors[0].Layout.Nodes.Single(value =>
                value.NodeId == edge.DestinationNodeId).Kind, Is.EqualTo(FloorRouteNodeKind.DeadEnd));
            Assert.That(fixture.Economy.TryCorridor(edge.CorridorDefinitionId,
                out double corridorPrice), Is.True);
            Assert.That(before - fixture.Runtime.structureRuntime.ManaReserve,
                Is.EqualTo(2d * corridorPrice));

            TileCoordinate[] tiles = edge.Footprint.OccupiedTiles;
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.PlaceCorridor(
                MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId,
                edge.FloorId, edge.OptionalBranchId, tiles[0])));
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.PlaceCorridor(
                MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId,
                edge.FloorId, edge.OptionalBranchId, tiles[1])));
            fixture.Reopen();
            Assert.That(fixture.Runtime.corridorContent.Assignments, Has.Length.EqualTo(2));
            OptionalBranchEditPreview blocked = OptionalBranchStructuralEditService.PreviewRemoval(
                fixture.State, fixture.Runtime.corridorContent, new OptionalBranchRemovalRequest
                { FloorInstanceId = edge.FloorId, OptionalBranchId = edge.OptionalBranchId },
                fixture.Production, fixture.Configuration, fixture.Profile.Canonical);
            Assert.That(blocked.IsValid, Is.False);
            Assert.That(blocked.ReasonCodes, Does.Contain(OptionalBranchStructuralEditService.AssignedContentReason));

            foreach (string id in fixture.Runtime.corridorContent.Assignments.Select(value => value.AssignmentId).ToArray())
                fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.Unassign(id)));
            fixture.Reopen();
            Assert.That(fixture.Runtime.corridorContent.Assignments, Is.Empty);
            Assert.That(fixture.State.LifecycleAndOwnership.ReturnedContents, Has.Length.EqualTo(2));
            ReturnedStructuralContent returned = fixture.State.LifecycleAndOwnership.ReturnedContents[0];
            double beforeRedeploy = fixture.Runtime.structureRuntime.ManaReserve;
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.RedeployCorridor(
                returned.AssignmentId, edge.FloorId, edge.OptionalBranchId, tiles[0])));
            CorridorContentAssignment redeployed = fixture.Runtime.corridorContent.Assignments.Single();
            Assert.That(redeployed.AssignmentId, Is.EqualTo(returned.AssignmentId));
            Assert.That(redeployed.CategoryId, Is.EqualTo(returned.CategoryId));
            Assert.That(redeployed.OptionId, Is.EqualTo(returned.OptionId));
            Assert.That(redeployed.Sequence, Is.EqualTo(returned.Sequence));
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(beforeRedeploy));
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.Unassign(redeployed.AssignmentId)));
            OptionalBranchEditPreview removal = OptionalBranchStructuralEditService.PreviewRemoval(
                fixture.State, fixture.Runtime.corridorContent, new OptionalBranchRemovalRequest
                { FloorInstanceId = edge.FloorId, OptionalBranchId = edge.OptionalBranchId },
                fixture.Production, fixture.Configuration, fixture.Profile.Canonical);
            Assert.That(removal.IsValid, Is.True, string.Join(",", removal.ReasonCodes));
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.RemoveBranch(removal)));
            fixture.Reopen();
            Assert.That(fixture.State.Floors[0].Layout.Edges.Any(value =>
                value.OptionalBranchId == edge.OptionalBranchId), Is.False);
            Assert.That(fixture.State.LifecycleAndOwnership.ReturnedContents.Select(value => value.AssignmentId),
                Is.EquivalentTo(fixture.Runtime.validatedCanonicalSpatialState.LifecycleAndOwnership.ReturnedContents
                    .Select(value => value.AssignmentId)));
        }

        [Test]
        public void OneTileBranchCannotShareTrapAndLootTile()
        {
            var fixture = BranchFixture(1, out OptionalBranchEditPreview preview);
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.ConstructBranch(preview)));
            FloorRouteEdge edge = fixture.State.Floors[0].Layout.Edges.Single(value =>
                value.Classification == RouteClassification.Optional);
            TileCoordinate tile = edge.Footprint.OccupiedTiles.Single();
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.PlaceCorridor(
                MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId,
                edge.FloorId, edge.OptionalBranchId, tile)));
            DetachedCanonicalWriteResult rejected = fixture.Execute(
                DetachedCanonicalMutationRequest.PlaceCorridor(MvpDungeonPlacementIds.LootNodeCategoryId,
                    MvpDungeonPlacementIds.HiddenCacheOptionId, edge.FloorId, edge.OptionalBranchId, tile));
            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(rejected.Reason, Is.EqualTo(CorridorContentMutationService.OccupiedReason));
            Assert.That(fixture.Runtime.corridorContent.Assignments, Has.Length.EqualTo(1));
        }

        [Test]
        public void KnowledgeApplicabilityUsesDeterministicTopologyFingerprint()
        {
            var fixture = BranchFixture(2, out OptionalBranchEditPreview preview);
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.ConstructBranch(preview)));
            FloorRouteEdge edge = fixture.State.Floors[0].Layout.Edges.Single(value =>
                value.Classification == RouteClassification.Optional);
            Assert.That(BranchTopologyFingerprint.TryCompute(fixture.State, edge.FloorId,
                edge.OptionalBranchId, out string fingerprint), Is.True);
            var record = new BranchKnowledgeRecord { FloorInstanceId = edge.FloorId,
                OptionalBranchId = edge.OptionalBranchId, EdgeId = edge.EdgeId,
                TopologyFingerprint = fingerprint, TopologyKnown = true,
                IncentiveKnown = false, DangerKnown = false, ConfidenceKnown = false };
            Assert.That(BranchTopologyFingerprint.IsApplicable(record, fixture.State), Is.True);
            DetachedCanonicalSpatialSaveState changed = preview.DetachedCandidate;
            changed.Floors[0].Layout.Edges.Single(value => value.EdgeId == edge.EdgeId)
                .Footprint = new ResolvedTileFootprint(new[] { edge.Footprint.OccupiedTiles[0] });
            Assert.That(BranchTopologyFingerprint.IsApplicable(record, changed), Is.False);
        }

        [Test]
        public void BranchRemovalDeletesMatchingKnowledgeInTheSameAtomicMutation()
        {
            var fixture = BranchFixture(2, out OptionalBranchEditPreview construction);
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.ConstructBranch(construction)));
            FloorRouteEdge edge = fixture.State.Floors[0].Layout.Edges.Single(value =>
                value.Classification == RouteClassification.Optional);
            Assert.That(BranchTopologyFingerprint.TryCompute(fixture.State, edge.FloorId,
                edge.OptionalBranchId, out string fingerprint), Is.True);
            var knowledge = new SharedBranchKnowledgeAuthority { Records = new[]
            {
                new BranchKnowledgeRecord { FloorInstanceId = edge.FloorId,
                    OptionalBranchId = edge.OptionalBranchId, EdgeId = edge.EdgeId,
                    TopologyFingerprint = fingerprint, TopologyKnown = true }
            }};
            DetachedCompleteSaveValidationResult current = Current(fixture);
            DetachedRecognizedSaveStateSnapshotResult snapshot =
                DetachedRecognizedSaveStateSnapshot.Capture(fixture.Runtime, fixture.Profile);
            DetachedCanonicalSaveSessionResult prepared = fixture.Session.PrepareLiveReplacement(
                snapshot, fixture.State, current.Investment, current.CorridorContent, knowledge);
            Assert.That(prepared.IsSuccess, Is.True, prepared.Reason);
            byte[] withKnowledge = prepared.Update.GetBytes();
            fixture.FileSystem.Seed(fixture.ActivePath, withKnowledge);
            fixture.Session = DetachedCanonicalSaveSession.Open(withKnowledge,
                fixture.Context, fixture.Profile).Session;
            fixture.Reopen();
            Assert.That(Current(fixture).BranchKnowledge.Records, Has.Length.EqualTo(1));

            OptionalBranchEditPreview removal = OptionalBranchStructuralEditService.PreviewRemoval(
                fixture.State, fixture.Runtime.corridorContent, new OptionalBranchRemovalRequest
                { FloorInstanceId = edge.FloorId, OptionalBranchId = edge.OptionalBranchId },
                fixture.Production, fixture.Configuration, fixture.Profile.Canonical);
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.RemoveBranch(removal)));
            Assert.That(fixture.State.Floors[0].Layout.Edges.Any(value =>
                value.OptionalBranchId == edge.OptionalBranchId), Is.False);
            Assert.That(Current(fixture).BranchKnowledge.Records, Is.Empty);
            fixture.Reopen();
            Assert.That(Current(fixture).BranchKnowledge.Records, Is.Empty);
        }

        [Test]
        public void OptionalBranchDoesNotChangeRequiredRouteOrRunOutcome()
        {
            var fixture = BranchFixture(2, out OptionalBranchEditPreview preview);
            CanonicalMvpRouteProjectionResult beforeProjection =
                CanonicalMvpRouteProjection.InspectWithProductionContent(fixture.Runtime,
                    fixture.Production);
            Assert.That(beforeProjection.AuthorityState,
                Is.EqualTo(CanonicalMvpRuntimeAuthorityState.ValidatedCanonical));
            string beforeRoute = JsonUtility.ToJson(new RouteHolder { Rooms = beforeProjection.Rooms });
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.ConstructBranch(preview)));
            FloorRouteEdge edge = fixture.State.Floors[0].Layout.Edges.Single(value =>
                value.Classification == RouteClassification.Optional);
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.PlaceCorridor(
                MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId,
                edge.FloorId, edge.OptionalBranchId, edge.Footprint.OccupiedTiles[0])));
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.PlaceCorridor(
                MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId,
                edge.FloorId, edge.OptionalBranchId, edge.Footprint.OccupiedTiles[1])));
            CanonicalMvpRouteProjectionResult afterProjection =
                CanonicalMvpRouteProjection.InspectWithProductionContent(fixture.Runtime,
                    fixture.Production);
            Assert.That(JsonUtility.ToJson(new RouteHolder { Rooms = afterProjection.Rooms }),
                Is.EqualTo(beforeRoute));
            Assert.That(afterProjection.Rooms.Any(value => value.RoomInstanceId == edge.DestinationNodeId),
                Is.False);

            var service = new RunSimulationService(fixture.Configuration);
            var runtime = new StructureRuntimeState { Heat = 3d, ManaReserve = 20d };
            string first = JsonUtility.ToJson(service.SimulateRoute(runtime, 123L, 7,
                RunPostureResolver.BalancedId, beforeProjection.Rooms));
            string second = JsonUtility.ToJson(service.SimulateRoute(new StructureRuntimeState
                { Heat = 3d, ManaReserve = 20d }, 123L, 7,
                RunPostureResolver.BalancedId, afterProjection.Rooms));
            Assert.That(second, Is.EqualTo(first));
        }

        [Serializable]
        private sealed class RouteHolder
        {
            public MvpOrderedRouteRoom[] Rooms;
        }

        private static DetachedCanonicalWriteAuthorityTests.Fixture BranchFixture(int length,
            out OptionalBranchEditPreview preview)
        {
            var fixture = DetachedCanonicalWriteAuthorityTests.Fixture.Create(null);
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)));
            fixture.Runtime.completedResearch = new CompletedResearchState
            { ProjectIds = new[] { BasicBranchingResearchAuthority.ResearchId } };
            SavedSpatialFloor floor = fixture.State.Floors.Single();
            FloorRouteNode room = floor.Layout.Nodes.Single(value => value.Kind == FloorRouteNodeKind.Room);
            preview = OptionalBranchStructuralEditService.PreviewConstruction(fixture.State,
                new OptionalBranchConstructionRequest { FloorInstanceId = floor.FloorInstanceId,
                    OriginNodeId = room.NodeId, OriginConnectionPointId = "east", CorridorLength = length },
                fixture.Runtime.completedResearch, fixture.BranchingResearch, fixture.Production,
                fixture.Configuration, fixture.Profile.Canonical);
            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            return fixture;
        }

        private static DetachedCompleteSaveValidationResult Current(
            DetachedCanonicalWriteAuthorityTests.Fixture fixture) =>
            DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                fixture.Session.GetCurrentBytes(), fixture.Context);

        private static byte[] FrozenSchemaWithDeadEnd(byte[] schemaTen, int schemaVersion)
        {
            string text = Encoding.UTF8.GetString(schemaTen);
            text = RemovePrimaryTail(text, PhaseFiveSaveContracts.CorridorOwnerName);
            if (schemaVersion < 9) text = RemovePrimaryTail(text, "structuralInvestment");
            if (schemaVersion < 8) text = RemovePrimaryTail(text, "structuralLifecycleAndOwnership");
            return Encoding.UTF8.GetBytes(text.Replace("\"schemaVersion\":10",
                "\"schemaVersion\":" + schemaVersion));
        }

        private static string RemovePrimaryTail(string text, string member)
        {
            int start = text.IndexOf(",\"" + member + "\":", StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), member);
            return text.Remove(start, text.Length - 2 - start);
        }
    }
}
#endif
