#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text;
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
    }
}
#endif
