#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    [TestFixture]
    public class PhaseSixA3FloorActivationEligibilityTests
    {
        private const string FloorId = "canonical.floor.01";
        private const string ProfilePath = "Assets/_Project/Resources/floor_construction_profiles.json";
        private const string ResearchPath = "Assets/_Project/Data/Production/Research/Dungeon_Builder_Research_Export_Bundle/architecture/";

        [Test]
        public void ConstructedInactiveFloorTwoWithPermissionAndCompleteRouteIsEligible()
        {
            Fixture fixture = Eligible();
            FloorActivationEligibilityResult result = Resolve(fixture);

            Assert.That(result.IsEligible, Is.True, result.PrimaryBlocker);
            Assert.That(result.ReasonCodes, Is.Empty);
            Assert.That(result.HasTargetFloor, Is.True);
            Assert.That(result.TargetFloorInstanceId, Is.EqualTo(FloorId));
            Assert.That(result.TargetFloorDefinitionId, Is.EqualTo("spatial.floor.02"));
            Assert.That(result.TargetFloorIndex, Is.EqualTo(1));
            Assert.That(Two(fixture).ActivationState, Is.EqualTo(FloorActivationState.Inactive));
        }

        [Test]
        public void MissingResearchIsThePrimaryStableBlocker()
        {
            Fixture fixture = Eligible();
            fixture.Runtime.completedResearch.ProjectIds = Array.Empty<string>();

            FloorActivationEligibilityResult result = Resolve(fixture);

            Assert.That(result.IsEligible, Is.False);
            Assert.That(result.PrimaryBlocker, Is.EqualTo(FloorActivationEligibilityReasons.ResearchRequired));
            CollectionAssert.AreEqual(new[] { FloorActivationEligibilityReasons.ResearchRequired }, result.ReasonCodes);
        }

        [Test]
        public void DuplicateResearchPermissionIsReadOnlyAndGrantsOnce()
        {
            Fixture fixture = Eligible();
            fixture.Runtime.completedResearch = new CompletedResearchState
            {
                ProjectIds = new[] { "ac_200", "ac_100", "ac_100" },
                LastCompletedProjectId = "ac_100",
                LastCompletionRuleSourceId = "test.rule"
            };
            string before = JsonUtility.ToJson(fixture.Runtime.completedResearch);

            Assert.That(Resolve(fixture).IsEligible, Is.True);
            Assert.That(JsonUtility.ToJson(fixture.Runtime.completedResearch), Is.EqualTo(before));
        }

        [Test]
        public void MissingFloorTwoIsNotConstructed()
        {
            Fixture fixture = Start();
            FloorActivationEligibilityResult result = Resolve(fixture);

            Assert.That(result.PrimaryBlocker, Is.EqualTo(FloorActivationEligibilityReasons.TargetNotConstructed));
            Assert.That(result.HasTargetFloor, Is.False);
        }

        [Test]
        public void AlreadyActiveFloorTwoIsNotAnActivationCandidate()
        {
            Fixture fixture = Eligible();
            Two(fixture).ActivationState = FloorActivationState.Active;

            FloorActivationEligibilityResult result = Resolve(fixture);

            Assert.That(result.PrimaryBlocker, Is.EqualTo(FloorActivationEligibilityReasons.TargetAlreadyActive));
            Assert.That(result.ReasonCodes, Does.Contain(FloorActivationEligibilityReasons.TargetAlreadyActive));
        }

        [Test]
        public void InvalidShallowerActivationFailsAtCanonicalPrecedence()
        {
            Fixture fixture = Eligible();
            fixture.State.Floors.Single(value => value.FloorIndex == 0).ActivationState =
                FloorActivationState.Inactive;

            FloorActivationEligibilityResult result = Resolve(fixture);

            Assert.That(result.PrimaryBlocker, Is.EqualTo(FloorActivationEligibilityReasons.CanonicalStateInvalid));
        }

        [Test]
        public void EmptyConstructedShellIsConstructionValidButActivationIneligible()
        {
            Fixture fixture = Constructed();
            Assert.That(DetachedCanonicalProductionSemanticValidation.Validate(fixture.State,
                fixture.Production, fixture.Configuration, fixture.Profile.Canonical.Spatial).IsValid, Is.True);

            FloorActivationEligibilityResult result = Resolve(fixture);

            Assert.That(result.PrimaryBlocker, Is.EqualTo(FloorActivationEligibilityReasons.ActivationLayoutInvalid));
        }

        [Test]
        public void IncompleteRequiredRouteIsActivationIneligible()
        {
            Fixture fixture = Constructed();
            BuildRoom(fixture);
            SavedSpatialFloor floor = Two(fixture);
            string completion = floor.Layout.Nodes.Single(value =>
                value.Kind == FloorRouteNodeKind.Completion).NodeId;
            floor.Layout.Edges = floor.Layout.Edges.Where(value =>
                value.DestinationNodeId != completion).ToArray();

            FloorActivationEligibilityResult result = Resolve(fixture);

            Assert.That(result.PrimaryBlocker, Is.EqualTo(FloorActivationEligibilityReasons.ActivationLayoutInvalid));
        }

        [Test]
        public void DirectEntranceToCompletionWithoutRoomIsActivationIneligible()
        {
            Fixture fixture = Constructed();
            SavedSpatialFloor floor = Two(fixture);
            floor.Layout.Edges = new[] { RequiredEdge(floor.Layout.Nodes.Single(value =>
                value.Kind == FloorRouteNodeKind.Entrance).NodeId, floor.Layout.Nodes.Single(value =>
                value.Kind == FloorRouteNodeKind.Completion).NodeId, "canonical.floor.01.edge.direct") };

            FloorActivationEligibilityResult result = Resolve(fixture);

            Assert.That(result.PrimaryBlocker, Is.EqualTo(FloorActivationEligibilityReasons.RequiredRouteMissingRoom));
        }

        [TestCase("missing_entrance")]
        [TestCase("duplicate_completion")]
        public void InvalidEntranceOrCompletionCardinalityFails(string scenario)
        {
            Fixture fixture = Eligible();
            SavedSpatialFloor floor = Two(fixture);
            if (scenario == "missing_entrance")
                floor.FixedStructures = floor.FixedStructures.Where(value =>
                    value.Kind != FixedSpatialStructureKind.Entrance).ToArray();
            else
                floor.Layout.Nodes = floor.Layout.Nodes.Concat(new[] { new FloorRouteNode
                {
                    NodeId = FloorId + ".node.completion.duplicate", FloorId = FloorId,
                    Kind = FloorRouteNodeKind.Completion
                } }).OrderBy(value => value.NodeId, StringComparer.Ordinal).ToArray();

            Assert.That(Resolve(fixture).IsEligible, Is.False);
        }

        [TestCase("dangling")]
        [TestCase("cross_floor")]
        public void DanglingAndCrossFloorRouteReferencesFailClosed(string scenario)
        {
            Fixture fixture = Eligible();
            SavedSpatialFloor floor = Two(fixture);
            if (scenario == "dangling") floor.Layout.Edges[0].DestinationNodeId = "missing.node";
            else floor.Layout.Nodes.Single(value => value.Kind == FloorRouteNodeKind.Room).FloorId =
                fixture.State.Floors.Single(value => value.FloorIndex == 0).FloorInstanceId;

            Assert.That(Resolve(fixture).PrimaryBlocker,
                Is.EqualTo(FloorActivationEligibilityReasons.CanonicalStateInvalid));
        }

        [TestCase("definition")]
        [TestCase("instance")]
        public void InvalidPersistedTargetIdentityFails(string scenario)
        {
            Fixture fixture = Eligible();
            SavedSpatialFloor floor = Two(fixture);
            if (scenario == "definition") floor.FloorDefinitionId = "spatial.floor.invalid";
            else
            {
                string oldId = floor.FloorInstanceId;
                string replacement = "canonical.floor.wrong";
                floor.FloorInstanceId = replacement;
                floor.Layout.FloorId = replacement;
                foreach (RoomSpatialInstance room in floor.Layout.Rooms) room.FloorId = replacement;
                foreach (FloorRouteNode node in floor.Layout.Nodes) node.FloorId = replacement;
                foreach (FloorRouteEdge edge in floor.Layout.Edges) edge.FloorId = replacement;
                foreach (SavedFixedSpatialStructure fixedStructure in floor.FixedStructures)
                    fixedStructure.FloorInstanceId = replacement;
                fixture.State.LifecycleAndOwnership.Floors.Single(value =>
                    value.FloorInstanceId == oldId).FloorInstanceId = replacement;
            }

            Assert.That(Resolve(fixture).PrimaryBlocker,
                Is.EqualTo(FloorActivationEligibilityReasons.TargetIdentityInvalid));
        }

        [Test]
        public void DuplicateProductionFloorDefinitionFailsConfigurationResolution()
        {
            Fixture fixture = Eligible();
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            catalog.Floors = catalog.Floors.Concat(new[] { catalog.Floors.Single(value =>
                value.FloorDefinitionId == "spatial.floor.02") }).ToArray();
            ProductionSpatialContentSnapshot production = new ProductionSpatialContentSnapshot(
                fixture.Production.Manifest, catalog, fixture.Production.Languages);

            FloorActivationEligibilityResult result = Resolve(fixture, production: production);

            Assert.That(result.PrimaryBlocker,
                Is.EqualTo(FloorActivationEligibilityReasons.TargetConfigurationInvalid));
        }

        [Test]
        public void NoncanonicalFloorOrderFailsClosed()
        {
            Fixture fixture = Eligible();
            Array.Reverse(fixture.State.Floors);

            Assert.That(Resolve(fixture).PrimaryBlocker,
                Is.EqualTo(FloorActivationEligibilityReasons.CanonicalStateInvalid));
        }

        [Test]
        public void CanonicalWorkloadLimitsRemainEnforced()
        {
            Fixture fixture = Eligible();
            CanonicalSpatialSerializationLimits limits = fixture.Profile.Canonical;
            int minimumRecords = Enumerable.Range(1, limits.Spatial.MaximumRecords).First(value =>
                CanonicalSpatialSaveContracts.Validate(fixture.State,
                    new CanonicalSpatialSaveWorkloadLimits(value,
                        limits.Spatial.MaximumMaterializedTiles), true).IsValid);
            var constrained = new CanonicalSpatialSerializationLimits(limits.Serialized,
                new CanonicalSpatialSaveWorkloadLimits(minimumRecords - 1,
                    limits.Spatial.MaximumMaterializedTiles));

            Assert.That(Resolve(fixture, limits: constrained).PrimaryBlocker,
                Is.EqualTo(FloorActivationEligibilityReasons.CanonicalStateInvalid));
        }

        [Test]
        public void ResolutionLeavesSpatialRuntimeResearchAndSaveBytesUnchanged()
        {
            Fixture fixture = Eligible();
            fixture.Runtime.completedResearch.ProjectIds = new[] { "ac_100", "ac_100", "ac_200" };
            string spatial = JsonUtility.ToJson(fixture.State);
            string runtime = JsonUtility.ToJson(fixture.Runtime);
            string research = JsonUtility.ToJson(fixture.Runtime.completedResearch);
            byte[] save = fixture.Session.GetCurrentBytes();

            Assert.That(Resolve(fixture).IsEligible, Is.True);

            Assert.That(JsonUtility.ToJson(fixture.State), Is.EqualTo(spatial));
            Assert.That(JsonUtility.ToJson(fixture.Runtime), Is.EqualTo(runtime));
            Assert.That(JsonUtility.ToJson(fixture.Runtime.completedResearch), Is.EqualTo(research));
            CollectionAssert.AreEqual(save, fixture.Session.GetCurrentBytes());
        }

        [Test]
        public void RepeatedCallsAndEquivalentPermissionOrderReturnEquivalentResults()
        {
            Fixture fixture = Constructed();
            fixture.Runtime.completedResearch.ProjectIds = new[] { "ac_200", "ac_100", "ac_100" };
            FloorActivationEligibilityResult first = Resolve(fixture);
            fixture.Runtime.completedResearch.ProjectIds = new[] { "ac_100", "ac_200", "ac_100" };
            FloorActivationEligibilityResult second = Resolve(fixture);
            FloorActivationEligibilityResult third = Resolve(fixture);

            Assert.That(first.IsEligible, Is.EqualTo(second.IsEligible));
            CollectionAssert.AreEqual(first.ReasonCodes, second.ReasonCodes);
            CollectionAssert.AreEqual(second.ReasonCodes, third.ReasonCodes);
            CollectionAssert.AreEqual(new[]
            {
                FloorActivationEligibilityReasons.ActivationLayoutInvalid
            }, first.ReasonCodes);
        }

        [Test]
        public void FloorOneAndCurrentRunProjectionRemainUnchangedAndRejectSecondActiveFloor()
        {
            Fixture fixture = Eligible();
            SavedSpatialFloor first = fixture.State.Floors.Single(value => value.FloorIndex == 0);
            string before = JsonUtility.ToJson(first);
            Assert.That(Resolve(fixture).IsEligible, Is.True);
            Assert.That(JsonUtility.ToJson(first), Is.EqualTo(before));

            Two(fixture).ActivationState = FloorActivationState.Active;
            Assert.That(CanonicalMvpRouteProjection.InspectWithProductionContent(
                fixture.Runtime, fixture.Production).AuthorityState,
                Is.EqualTo(CanonicalMvpRuntimeAuthorityState.ContradictoryCanonical));
        }

        [Test]
        public void BlockerPrecedenceIsExplicitWhenResearchAndLayoutBothFail()
        {
            Fixture fixture = Constructed();
            fixture.Runtime.completedResearch.ProjectIds = Array.Empty<string>();

            FloorActivationEligibilityResult result = Resolve(fixture);

            CollectionAssert.AreEqual(new[]
            {
                FloorActivationEligibilityReasons.ResearchRequired,
                FloorActivationEligibilityReasons.ActivationLayoutInvalid
            }, result.ReasonCodes);
            Assert.That(result.PrimaryBlocker, Is.EqualTo(FloorActivationEligibilityReasons.ResearchRequired));
        }

        private static Fixture Start()
        {
            Fixture fixture = Fixture.Create(null);
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)));
            fixture.Runtime.completedResearch = new CompletedResearchState
                { ProjectIds = new[] { FloorConstructionResearchAuthority.ResearchId } };
            return fixture;
        }

        private static Fixture Constructed()
        {
            Fixture fixture = Start();
            FloorConstructionPreview preview = FloorConstructionService.Preview(
                fixture.State, fixture.Runtime, fixture.Runtime.completedResearch, Profiles(fixture),
                Research(fixture), fixture.Production, fixture.Configuration, fixture.Profile.Canonical);
            Assert.That(preview.IsCommittable, Is.True, preview.Reason);
            fixture.Accept(new DetachedCanonicalWriteAuthority(fixture.Production, fixture.Compatibility,
                fixture.Configuration, fixture.Context, fixture.Profile, fixture.RemovalPolicy,
                fixture.Economy, acquisition: fixture.Acquisition,
                branchingResearch: fixture.BranchingResearch,
                floorConstructionProfiles: Profiles(fixture),
                floorConstructionResearch: Research(fixture)).ConstructFloor(fixture.ActivePath,
                    fixture.FileSystem, fixture.Session, fixture.Runtime, preview));
            return fixture;
        }

        private static Fixture Eligible()
        {
            Fixture fixture = Constructed();
            BuildRoom(fixture);
            CanonicalSpatialSaveValidationResult validation = CanonicalSpatialSaveContracts.Validate(
                fixture.State, fixture.Profile.Canonical.Spatial, true);
            Assert.That(validation.IsValid, Is.True, string.Join(",", validation.Issues));
            return fixture;
        }

        private static void BuildRoom(Fixture fixture)
        {
            StructuralEditPreview preview = StructuralEditService.Preview(fixture.State,
                new StructuralConstructionRequest { FloorInstanceId = FloorId,
                    RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(1, 2),
                    Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "east" },
                fixture.Production, fixture.Compatibility, fixture.Configuration,
                fixture.Profile.Canonical);
            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.Construct(preview)));
        }

        private static FloorRouteEdge RequiredEdge(string source, string destination, string id) =>
            new FloorRouteEdge { EdgeId = id, FloorId = FloorId, SourceNodeId = source,
                DestinationNodeId = destination, ConnectionKind = FloorRouteConnectionKind.DirectDoorway,
                Classification = RouteClassification.Required };

        private static SavedSpatialFloor Two(Fixture fixture) => fixture.State.Floors.Single(value =>
            value.FloorInstanceId == FloorId);

        private static FloorConstructionProfileSnapshot Profiles(Fixture fixture)
        {
            Assert.That(FloorConstructionProfileSnapshot.TryParse(File.ReadAllBytes(ProfilePath),
                fixture.Production, fixture.Profile.Canonical, out FloorConstructionProfileSnapshot value), Is.True);
            return value;
        }

        private static FloorConstructionResearchSnapshot Research(Fixture fixture)
        {
            Assert.That(FloorConstructionResearchAuthority.TryParse(
                File.ReadAllText(ResearchPath + "research_nodes.json"),
                File.ReadAllText(ResearchPath + "tables.json"), fixture.Profile.Canonical,
                out FloorConstructionResearchSnapshot value), Is.True);
            return value;
        }

        private static FloorActivationEligibilityResult Resolve(Fixture fixture,
            ProductionSpatialContentSnapshot production = null,
            CanonicalSpatialSerializationLimits? limits = null) =>
            FloorActivationEligibilityAuthority.Resolve(fixture.State,
                fixture.Runtime.completedResearch, Profiles(fixture), Research(fixture),
                production ?? fixture.Production, fixture.Configuration,
                limits ?? fixture.Profile.Canonical);
    }
}
#endif
