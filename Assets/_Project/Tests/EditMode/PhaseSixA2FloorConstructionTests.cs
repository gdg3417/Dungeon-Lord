#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.Structures;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;
using Operation = DungeonBuilder.M0.Tests.EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class PhaseSixA2FloorConstructionTests
    {
        private const string ResearchPath = "Assets/_Project/Data/Production/Research/Dungeon_Builder_Research_Export_Bundle/architecture/";
        private const string ProfilePath = "Assets/_Project/Resources/floor_construction_profiles.json";
        private const string FloorId = "canonical.floor.01";
        private static Fixture Start(bool permitted = true)
        {
            var f = Fixture.Create(null);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.BasicRoomOptionId)));
            f.Runtime.completedResearch = new CompletedResearchState { ProjectIds = permitted ? new[] { "ac_100" } : Array.Empty<string>() };
            return f;
        }
        private static FloorConstructionProfileSnapshot Profiles(Fixture f)
        {
            Assert.That(FloorConstructionProfileSnapshot.TryParse(File.ReadAllBytes(ProfilePath), f.Production,
                f.Profile.Canonical, out var snapshot), Is.True);
            return snapshot;
        }
        private static FloorConstructionResearchSnapshot Research(Fixture f)
        {
            Assert.That(FloorConstructionResearchAuthority.TryParse(File.ReadAllText(ResearchPath + "research_nodes.json"),
                File.ReadAllText(ResearchPath + "tables.json"), f.Profile.Canonical, out var snapshot), Is.True);
            return snapshot;
        }
        private static FloorConstructionPreview Preview(Fixture f) => FloorConstructionService.Preview(f.State, f.Runtime,
            f.Runtime.completedResearch, Profiles(f), Research(f), f.Production, f.Configuration, f.Profile.Canonical);
        private static DetachedCanonicalWriteResult Construct(Fixture f, FloorConstructionPreview preview = null) =>
            new DetachedCanonicalWriteAuthority(f.Production, f.Compatibility, f.Configuration, f.Context, f.Profile,
                f.RemovalPolicy, f.Economy, acquisition: f.Acquisition, branchingResearch: f.BranchingResearch,
                floorConstructionProfiles: Profiles(f), floorConstructionResearch: Research(f))
                .ConstructFloor(f.ActivePath, f.FileSystem, f.Session, f.Runtime, preview ?? Preview(f));
        private static Fixture Constructed()
        { var f = Start(); f.Accept(Construct(f)); return f; }
        private static SavedSpatialFloor Two(Fixture f) => f.State.Floors.Single(value => value.FloorInstanceId == FloorId);
        private static StructuralEditPreview Room(Fixture f, int x = 1, int y = 2, string exit = "east") =>
            StructuralEditService.Preview(f.State, new StructuralConstructionRequest { FloorInstanceId = FloorId,
                RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(x, y), Orientation = CardinalOrientation.Zero,
                TerminalConnectionPointId = exit }, f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
        private static void Build(Fixture f, int x = 1, int y = 2, string exit = "east")
        { var p = Room(f, x, y, exit); Assert.That(p.IsValid, Is.True, string.Join(",", p.ReasonCodes));
          f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(p))); }
        private static void ShellInvestment(Fixture f)
        {
            var ledger = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context).Investment;
            Assert.That(ledger.Single(value => value.StructureId == StructuralInvestment.ShellId(FloorId)).ConstructionMana, Is.EqualTo(450));
            Assert.That(ledger.Single(value => value.StructureId == StructuralInvestment.ShellId(FloorId)).RenovationMana, Is.Zero);
            Assert.That(ledger.Count(value => value.StructureId.EndsWith(".shell")), Is.EqualTo(1));
        }
        [Test]
        public void ProductionFloorAndProfileOwnApprovedTuningAndEndpoints()
        {
            var f = Start(); var floor = f.Production.Catalog.Floors.Single(value => value.FloorDefinitionId == "spatial.floor.02");
            Assert.That(floor.FloorIndex, Is.EqualTo(1)); Assert.That(floor.Bounds.Width, Is.EqualTo(14));
            Assert.That(floor.Bounds.Height, Is.EqualTo(14)); Assert.That(floor.FinalFloorSpaceCapacity, Is.EqualTo(80));
            Assert.That(floor.OptionalBranchAllowance, Is.EqualTo(1));
            var first = f.Production.Catalog.Floors.Single(value => value.FloorIndex == 0);
            CollectionAssert.AreEqual(first.AllowedRoomDefinitionIds, floor.AllowedRoomDefinitionIds);
            CollectionAssert.AreEqual(first.AllowedCorridorDefinitionIds, floor.AllowedCorridorDefinitionIds);
            Assert.That(Profiles(f).TryResolve(1, out var p), Is.True); Assert.That(p.ConstructionMana, Is.EqualTo(450));
            Assert.That(p.EntranceAnchor, Is.EqualTo(new TileCoordinate(1, 0)));
            Assert.That(p.CompletionAnchor, Is.EqualTo(new TileCoordinate(11, 12)));
            Assert.That(p.EntranceOrientation, Is.EqualTo(CardinalOrientation.Zero));
            Assert.That(p.CompletionOrientation, Is.EqualTo(CardinalOrientation.Zero));
        }
        [TestCase("missing")][TestCase("malformed")][TestCase("duplicate")][TestCase("anchor_missing")]
        [TestCase("cost")][TestCase("orientation")][TestCase("bounds")][TestCase("overlap")][TestCase("floor")]
        [TestCase("permission")][TestCase("duplicate_field")][TestCase("fractional_orientation")]
        public void InvalidProfilesFailClosed(string scenario)
        {
            var f = Start(); string json = File.ReadAllText(ProfilePath);
            switch (scenario)
            {
                case "missing": json = "{}"; break;
                case "malformed": json = "{broken"; break;
                case "duplicate": var document = JsonUtility.FromJson<FloorConstructionProfileDocument>(json);
                    document.profiles = document.profiles.Concat(document.profiles).ToArray(); json = JsonUtility.ToJson(document); break;
                case "anchor_missing": json = json.Replace("\"entranceAnchor\": { \"X\": 1, \"Y\": 0 },", ""); break;
                case "cost": json = json.Replace("\"constructionMana\": 450", "\"constructionMana\": -1"); break;
                case "orientation": json = json.Replace("\"entranceOrientation\": 0", "\"entranceOrientation\": 9"); break;
                case "fractional_orientation": json = json.Replace("\"entranceOrientation\": 0", "\"entranceOrientation\": 0.5"); break;
                case "bounds": json = json.Replace("\"X\": 11", "\"X\": 14"); break;
                case "overlap": json = json.Replace("\"X\": 11, \"Y\": 12", "\"X\": 1, \"Y\": 0"); break;
                case "floor": json = json.Replace("spatial.floor.02", "spatial.floor.01"); break;
                case "permission": json = json.Replace("ac_100", "ac_300"); break;
                case "duplicate_field": json = json.Replace("\"profileVersion\": 1", "\"profileVersion\": 1, \"profileVersion\": 1"); break;
            }
            Assert.That(FloorConstructionProfileSnapshot.TryParse(Encoding.UTF8.GetBytes(json), f.Production,
                f.Profile.Canonical, out _), Is.False, scenario);
        }
        [TestCase(false)][TestCase(true)]
        public void ResearchCompletionGrantsOnlyPermission(bool completed)
        {
            var f = Start(completed); byte[] before = f.Session.GetCurrentBytes();
            var p = Preview(f); Assert.That(p.IsCommittable, Is.EqualTo(completed), p.Reason);
            Assert.That(f.State.Floors, Has.Length.EqualTo(1)); CollectionAssert.AreEqual(before, f.Session.GetCurrentBytes());
        }
        [TestCase("none", false)]
        [TestCase("other", false)]
        [TestCase("single", true)]
        [TestCase("mixed", true)]
        [TestCase("duplicate", true)]
        public void ResearchPermissionUsesDuplicateSafeCompletedStateSemantics(string scenario, bool expected)
        {
            var f = Start(false);
            string[] ids = scenario == "none" ? Array.Empty<string>() :
                scenario == "other" ? new[] { "ac_200" } :
                scenario == "single" ? new[] { "ac_100" } :
                scenario == "mixed" ? new[] { "ac_200", "ac_100", "ac_300" } :
                new[] { "ac_100", "ac_100" };
            f.Runtime.completedResearch = new CompletedResearchState
            {
                ProjectIds = ids,
                LastCompletedProjectId = ids.LastOrDefault(),
                LastCompletionRuleSourceId = "research.completed.rule.test"
            };
            string before = JsonUtility.ToJson(f.Runtime.completedResearch);

            FloorConstructionPreview preview = Preview(f);

            Assert.That(preview.IsCommittable, Is.EqualTo(expected), preview.Reason);
            Assert.That(f.State.Floors, Has.Length.EqualTo(1));
            Assert.That(JsonUtility.ToJson(f.Runtime.completedResearch), Is.EqualTo(before));
        }
        [Test]
        public void DuplicateResearchCompletionCannotConstructAutomaticallyOrConstructTwice()
        {
            var f = Start(false);
            f.Runtime.completedResearch = new CompletedResearchState
            {
                ProjectIds = new[] { "ac_100", "ac_200", "ac_100" },
                LastCompletedProjectId = "ac_100",
                LastCompletionRuleSourceId = "research.completed.rule.test"
            };
            string before = JsonUtility.ToJson(f.Runtime.completedResearch);
            Assert.That(f.State.Floors, Has.Length.EqualTo(1));

            FloorConstructionPreview first = Preview(f);
            Assert.That(first.IsCommittable, Is.True, first.Reason);
            Assert.That(f.State.Floors, Has.Length.EqualTo(1));
            Assert.That(JsonUtility.ToJson(f.Runtime.completedResearch), Is.EqualTo(before));
            f.Accept(Construct(f, first));

            Assert.That(f.State.Floors.Count(value => value.FloorIndex == 1), Is.EqualTo(1));
            FloorConstructionPreview repeat = Preview(f);
            Assert.That(repeat.IsCommittable, Is.False);
            byte[] durable = f.Session.GetCurrentBytes();
            Assert.That(Construct(f, repeat).IsSuccess, Is.False);
            CollectionAssert.AreEqual(durable, f.Session.GetCurrentBytes());
            Assert.That(JsonUtility.ToJson(f.Runtime.completedResearch), Is.EqualTo(before));
        }
        [TestCase("node")][TestCase("effect")][TestCase("duplicate_node")][TestCase("duplicate_effect")]
        [TestCase("quoted_value")][TestCase("duplicate_property")]
        public void InvalidResearchConfigurationFailsClosed(string scenario)
        {
            string nodes = File.ReadAllText(ResearchPath + "research_nodes.json");
            string effects = File.ReadAllText(ResearchPath + "tables.json");
            if (scenario == "node") nodes = nodes.Replace("max_floors", "wrong_target");
            if (scenario == "effect") effects = effects.Replace("max_floors_set", "wrong_effect");
            if (scenario == "duplicate_node") nodes = nodes.Replace("\"ac_200\"", "\"ac_100\"");
            if (scenario == "duplicate_effect") effects = effects.Replace("eff_ac_fundamentals", "eff_ac_floor_2_permit");
            if (scenario == "quoted_value") effects = effects.Replace("\"value\": 2,", "\"value\": \"2\",");
            if (scenario == "duplicate_property") effects = effects.Replace("\"value\": 2,", "\"value\": 2, \"value\": 2,");
            Assert.That(FloorConstructionResearchAuthority.TryParse(nodes, effects, Start().Profile.Canonical, out _), Is.False);
        }
        [Test]
        public void ShellTransactionIsDeterministicEmptyInactiveAndReopensWithHistoricalInvestment()
        {
            var f = Start(); string first = JsonUtility.ToJson(f.State.Floors.Single());
            var p = Preview(f); var again = Preview(f);
            CollectionAssert.AreEqual(CanonicalSpatialSaveSerializer.Serialize(p.Candidate, f.Profile.Canonical).Value,
                CanonicalSpatialSaveSerializer.Serialize(again.Candidate, f.Profile.Canonical).Value);
            double balance = f.Runtime.structureRuntime.ManaReserve;
            f.Accept(Construct(f, p)); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(balance - 450));
            Assert.That(JsonUtility.ToJson(f.State.Floors.Single(value => value.FloorIndex == 0)), Is.EqualTo(first));
            var second = Two(f); Assert.That(second.ActivationState, Is.EqualTo(FloorActivationState.Inactive));
            Assert.That(second.FixedStructures, Has.Length.EqualTo(2)); Assert.That(second.Layout.Rooms, Is.Empty);
            Assert.That(second.Layout.Edges, Is.Empty); Assert.That(second.RoomContents.Assignments, Is.Empty);
            ShellInvestment(f); byte[] durable = f.Session.GetCurrentBytes(); f.Reopen(); ShellInvestment(f);
            CollectionAssert.AreEqual(durable, f.Session.GetCurrentBytes()); Assert.That(Two(f).FloorInstanceId, Is.EqualTo(FloorId));
        }
        [TestCase("insufficient")][TestCase("duplicate")][TestCase("stale")]
        public void RefusedConstructionPublishesNoChanges(string scenario)
        {
            var f = Start(); var p = Preview(f);
            if (scenario == "insufficient") f.Runtime.structureRuntime.ManaReserve = 449;
            if (scenario == "duplicate") f.Accept(Construct(f));
            if (scenario == "stale") f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId)));
            byte[] before = f.Session.GetCurrentBytes(); string runtime = JsonUtility.ToJson(f.Runtime);
            var result = Construct(f, scenario == "duplicate" ? Preview(f) : p);
            Assert.That(result.IsSuccess, Is.False); CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(runtime));
        }
        [TestCase(Operation.Write, 1)][TestCase(Operation.Write, 2)][TestCase(Operation.Replace, 1)]
        [TestCase(Operation.Read, 3)][TestCase(Operation.Read, 6)][TestCase(Operation.Flush, 1)]
        public void PersistenceFailuresPublishNothing(Operation operation, int occurrence)
        {
            var f = Start(); var p = Preview(f); byte[] before = f.Session.GetCurrentBytes();
            string runtime = JsonUtility.ToJson(f.Runtime); f.FileSystem.EnableFailure(operation, occurrence);
            var result = Construct(f, p); Assert.That(result.IsSuccess, Is.False);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            CollectionAssert.AreEqual(before, f.Session.GetCurrentBytes()); Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(runtime));
        }
        [TestCase("disk")][TestCase("validation")]
        public void StaleDurableSessionOrUnavailableValidationPublishesNothing(string scenario)
        {
            var f = Start(); var preview = Preview(f); byte[] session = f.Session.GetCurrentBytes();
            if (scenario == "disk") f.FileSystem.Seed(f.ActivePath, session.Concat(new byte[] { 32 }).ToArray());
            byte[] durable = f.FileSystem.ReadAllBytes(f.ActivePath); string runtime = JsonUtility.ToJson(f.Runtime);
            var writer = new DetachedCanonicalWriteAuthority(f.Production, f.Compatibility, f.Configuration,
                scenario == "validation" ? null : f.Context, f.Profile, f.RemovalPolicy, f.Economy,
                acquisition: f.Acquisition, floorConstructionProfiles: Profiles(f), floorConstructionResearch: Research(f));
            var result = writer.ConstructFloor(f.ActivePath, f.FileSystem, f.Session, f.Runtime, preview);
            Assert.That(result.IsSuccess, Is.False); CollectionAssert.AreEqual(durable, f.FileSystem.ReadAllBytes(f.ActivePath));
            CollectionAssert.AreEqual(session, f.Session.GetCurrentBytes()); Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(runtime));
        }
        [TestCase("version")][TestCase("cost")][TestCase("anchor")]
        public void ChangedConstructionConfigurationRejectsStalePreview(string scenario)
        {
            var f = Start(); var preview = Preview(f); byte[] before = f.Session.GetCurrentBytes();
            string runtime = JsonUtility.ToJson(f.Runtime);
            var document = JsonUtility.FromJson<FloorConstructionProfileDocument>(File.ReadAllText(ProfilePath));
            if (scenario == "version") document.profiles[0].profileVersion++;
            if (scenario == "cost") document.profiles[0].constructionMana++;
            if (scenario == "anchor") document.profiles[0].completionAnchor = new TileCoordinate(10, 12);
            Assert.That(FloorConstructionProfileSnapshot.TryParse(Encoding.UTF8.GetBytes(JsonUtility.ToJson(document)),
                f.Production, f.Profile.Canonical, out var profiles), Is.True);
            var writer = new DetachedCanonicalWriteAuthority(f.Production, f.Compatibility, f.Configuration, f.Context,
                f.Profile, f.RemovalPolicy, f.Economy, acquisition: f.Acquisition,
                floorConstructionProfiles: profiles, floorConstructionResearch: Research(f));
            var result = writer.ConstructFloor(f.ActivePath, f.FileSystem, f.Session, f.Runtime, preview);
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.Reason, Is.EqualTo(FloorConstructionService.StalePreviewReason));
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(runtime));
        }
        [TestCase("active")][TestCase("missing_entrance")][TestCase("missing_terminal")][TestCase("duplicate")]
        [TestCase("historical")]
        [TestCase("bounds")][TestCase("overlap")][TestCase("dangling")][TestCase("cross_floor")]
        public void InactiveValidationRetainsStructuralIntegrity(string scenario)
        {
            var f = Constructed(); var floor = Two(f);
            Assert.That(DetachedCanonicalProductionSemanticValidation.Validate(f.State, f.Production, f.Configuration,
                f.Profile.Canonical.Spatial).IsValid, Is.True);
            switch (scenario)
            {
                case "active": floor.ActivationState = FloorActivationState.Active; break;
                case "historical": floor.ActivationState = 0; break;
                case "missing_entrance": floor.FixedStructures = floor.FixedStructures.Where(value => value.Kind != FixedSpatialStructureKind.Entrance).ToArray(); break;
                case "missing_terminal": floor.FixedStructures = floor.FixedStructures.Where(value => value.Kind != FixedSpatialStructureKind.CompletionTerminal).ToArray(); break;
                case "duplicate": floor.FixedStructures = floor.FixedStructures.Concat(new[] { floor.FixedStructures[0] }).ToArray(); break;
                case "bounds": floor.FixedStructures[0].Anchor = new TileCoordinate(99, 99); break;
                case "overlap": floor.FixedStructures[1].Anchor = floor.FixedStructures[0].Anchor; break;
                case "dangling": floor.Layout.Edges = new[] { new FloorRouteEdge { EdgeId = FloorId + ".edge.bad",
                    FloorId = FloorId, SourceNodeId = "missing", DestinationNodeId = "missing", Classification = RouteClassification.Required } }; break;
                case "cross_floor": floor.Layout.Nodes[0].FloorId = f.State.Floors.Single(value => value.FloorIndex == 0).FloorInstanceId; break;
            }
            Assert.That(DetachedCanonicalProductionSemanticValidation.Validate(f.State, f.Production, f.Configuration,
                f.Profile.Canonical.Spatial).IsValid, Is.False, scenario);
        }
        [Test]
        public void InactiveFloorSupportsFirstAndAdditionalRoomsReplacementMovementAndLastDeletion()
        {
            var f = Constructed(); string firstFloor = JsonUtility.ToJson(f.State.Floors.Single(value => value.FloorIndex == 0));
            Build(f); string firstId = Two(f).Layout.Rooms.Single().RoomInstanceId;
            var move = StructuralRenovationService.PreviewMovement(f.State, new StructuralMovementRequest {
                FloorInstanceId = FloorId, RoomInstanceId = firstId, Anchor = new TileCoordinate(1, 3) },
                f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            Assert.That(move.IsValid, Is.True, string.Join(",", move.ReasonCodes)); f.Accept(f.Execute(DetachedCanonicalMutationRequest.Move(move)));
            var replace = StructuralRenovationService.PreviewReplacement(f.State, new StructuralReplacementRequest {
                FloorInstanceId = FloorId, RoomInstanceId = firstId, RoomDefinitionId = "spatial.room.rectangle" },
                f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            Assert.That(replace.IsValid, Is.True, string.Join(",", replace.ReasonCodes)); f.Accept(f.Execute(DetachedCanonicalMutationRequest.Replace(replace)));
            Build(f, 1, 8);
            foreach (string roomId in Two(f).Layout.Rooms.OrderByDescending(value => value.RoomInstanceId).Select(value => value.RoomInstanceId).ToArray())
            {
                var delete = StructuralDeletionService.Preview(f.State, new StructuralDeletionRequest { FloorInstanceId = FloorId,
                    TargetRoomInstanceId = roomId }, f.RemovalPolicy, f.Production, f.Configuration, f.Profile.Canonical);
                Assert.That(delete.IsValid, Is.True, string.Join(",", delete.ReasonCodes)); f.Accept(f.Execute(DetachedCanonicalMutationRequest.Delete(delete)));
                Assert.That(delete.ResultingUsedFloorSpace + delete.ResultingRemainingFloorSpace, Is.EqualTo(80));
            }
            Assert.That(Two(f).Layout.Rooms, Is.Empty); Assert.That(Two(f).Layout.Edges, Is.Empty);
            Assert.That(JsonUtility.ToJson(f.State.Floors.Single(value => value.FloorIndex == 0)), Is.EqualTo(firstFloor));
            ShellInvestment(f); f.Reopen(); ShellInvestment(f); Build(f);
            Assert.That(Two(f).Layout.Rooms.Single().RoomInstanceId, Does.EndWith(".0002"));
        }
        [TestCase(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId)]
        [TestCase(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId)]
        [TestCase(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId)]
        public void ExplicitFloorContentPlacementUnassignmentAndCrossFloorRedeployment(string category, string option)
        {
            var f = Constructed(); Build(f); string roomId = Two(f).Layout.Rooms.Single().RoomInstanceId;
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(category, option, roomId, FloorId)));
            string assignment = Two(f).RoomContents.Assignments.Single().AssignmentId;
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(assignment, FloorId)));
            string floorOne = f.State.Floors.Single(value => value.FloorIndex == 0).FloorInstanceId;
            string roomOne = f.State.Floors.Single(value => value.FloorIndex == 0).Layout.Rooms.Single().RoomInstanceId;
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Redeploy(assignment, roomOne, floorOne)));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(assignment, floorOne)));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Redeploy(assignment, roomId, FloorId)));
            ShellInvestment(f); f.Reopen(); Assert.That(Two(f).RoomContents.Assignments.Single().AssignmentId, Is.EqualTo(assignment));
        }
        [TestCase(false)][TestCase(true)]
        public void WrongOrAmbiguousFloorRoomTargetsFailClosed(bool omitted)
        {
            var f = Constructed(); Build(f); string room = f.State.Floors.Single(value => value.FloorIndex == 0).Layout.Rooms.Single().RoomInstanceId;
            byte[] before = f.Session.GetCurrentBytes();
            var result = f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId,
                room, omitted ? null : FloorId)); Assert.That(result.IsSuccess, Is.False);
            CollectionAssert.AreEqual(before, f.Session.GetCurrentBytes());
        }
        [Test]
        public void OnlyFloorOneProjectsToRunsAndManaCountsOneAfterEditingAndReopen()
        {
            var f = Start(); string route = JsonUtility.ToJson(new Route { Rooms = CanonicalMvpRouteProjection.InspectWithProductionContent(f.Runtime, f.Production).Rooms });
            f.Accept(Construct(f)); Build(f); f.Reopen();
            Assert.That(JsonUtility.ToJson(new Route { Rooms = CanonicalMvpRouteProjection.InspectWithProductionContent(f.Runtime, f.Production).Rooms }), Is.EqualTo(route));
            Assert.That(CanonicalActiveFloorResolver.TryResolve(f.Runtime, f.Profile.Canonical.Spatial, out int active), Is.True);
            Assert.That(active, Is.EqualTo(1));
            var passive = PhaseFourTestSupport.PassiveMana(f.Profile.Canonical);
            var online = new CanonicalPassiveManaService(passive, f.Economy, new FormulaEngine(), f.Profile.Canonical.Spatial, 10);
            var rate = online.ResolveRate(f.Runtime, f.Configuration);
            f.Runtime.lastSavedUtcUnix = 1000;
            var offline = new CanonicalOfflinePassiveManaService(passive, online).Resolve(f.Runtime, f.Configuration, f.Runtime.lastSavedUtcUnix + 3600);
            Assert.That(offline.ApplicableOnlineManaPerHour, Is.EqualTo(rate.ManaPerHour));
            Two(f).ActivationState = FloorActivationState.Active;
            Assert.That(CanonicalMvpRouteProjection.InspectWithProductionContent(f.Runtime, f.Production).AuthorityState,
                Is.EqualTo(CanonicalMvpRuntimeAuthorityState.ContradictoryCanonical));
        }
        [Serializable] private sealed class Route { public MvpOrderedRouteRoom[] Rooms; }

        [Test]
        public void EquivalentFloorOneInputsProduceIdenticalDurableRunHistoryWithInactiveFloorTwo()
        {
            var baseline = Start(); var deeper = Start();
            foreach (var f in new[] { baseline, deeper })
                f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId)));
            deeper.Accept(Construct(deeper)); Build(deeper);
            foreach (var content in new[] { Tuple.Create(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId),
                Tuple.Create(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId) })
                deeper.Accept(deeper.Execute(DetachedCanonicalMutationRequest.Place(content.Item1, content.Item2,
                    Two(deeper).Layout.Rooms.Single().RoomInstanceId, FloorId)));
            // Shell spending is real; normalize the existing wallet through its established QA writer
            // so equivalent run inputs isolate the otherwise irrelevant inactive layout/content.
            foreach (var f in new[] { baseline, deeper })
            {
                f.Accept(f.Authority.SaveQaMana(f.ActivePath, f.FileSystem, f.Session, f.Runtime, true));
                f.Accept(PhaseFiveBBranchIntegrationTests.Run(f));
                f.Reopen();
            }
            Assert.That(JsonUtility.ToJson(deeper.Runtime.runHistory), Is.EqualTo(JsonUtility.ToJson(baseline.Runtime.runHistory)));
            Assert.That(Two(deeper).ActivationState, Is.EqualTo(FloorActivationState.Inactive)); ShellInvestment(deeper);
        }

        [Test]
        public void InactiveOptionalBranchAndCorridorCustodyNeverLeakIntoFloorOneRun()
        {
            var f = Constructed(); Build(f, exit: "north");
            f.Runtime.completedResearch = new CompletedResearchState { ProjectIds = new[] { "ac_100", "ac_300" } };
            string route = JsonUtility.ToJson(new Route { Rooms = CanonicalMvpRouteProjection.InspectWithProductionContent(f.Runtime, f.Production).Rooms });
            FloorRouteNode origin = Two(f).Layout.Nodes.Single(value => value.Kind == FloorRouteNodeKind.Room);
            var branch = OptionalBranchStructuralEditService.PreviewConstruction(f.State, new OptionalBranchConstructionRequest {
                FloorInstanceId = FloorId, OriginNodeId = origin.NodeId, OriginConnectionPointId = "east", CorridorLength = 2 },
                f.Runtime.completedResearch, f.BranchingResearch, f.Production, f.Configuration, f.Profile.Canonical);
            Assert.That(branch.IsSpatiallyValid, Is.True, string.Join(",", branch.ReasonCodes));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.ConstructBranch(branch)));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.PlaceCorridor(MvpDungeonPlacementIds.TrapCategoryId,
                MvpDungeonPlacementIds.SpikeTrapOptionId, FloorId, branch.OptionalBranchId, branch.OccupiedTiles[0])));
            string assigned = f.Runtime.corridorContent.Assignments.Single().AssignmentId;
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(assigned, FloorId)));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.RedeployCorridor(assigned, FloorId, branch.OptionalBranchId, branch.OccupiedTiles[0])));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.PlaceCorridor(MvpDungeonPlacementIds.LootNodeCategoryId,
                MvpDungeonPlacementIds.BasicLootNodeOptionId, FloorId, branch.OptionalBranchId, branch.OccupiedTiles[1])));
            var owned = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context);
            var plan = PhaseFiveBRouteProjection.Resolve(owned, f.Runtime, f.Production, f.Configuration);
            Assert.That(plan.Forks, Is.Empty); Assert.That(plan.RequiredRooms.All(value => value.Room.FloorIndex == 0), Is.True);
            Assert.That(JsonUtility.ToJson(new Route { Rooms = CanonicalMvpRouteProjection.InspectWithProductionContent(f.Runtime, f.Production).Rooms }), Is.EqualTo(route));
            f.Reopen(); ShellInvestment(f);
            foreach (string assignmentId in f.Runtime.corridorContent.Assignments.Select(value => value.AssignmentId).ToArray())
                f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(assignmentId, FloorId)));
            var removal = OptionalBranchStructuralEditService.PreviewRemoval(f.State, f.Runtime.corridorContent, new OptionalBranchRemovalRequest {
                FloorInstanceId = FloorId, OptionalBranchId = branch.OptionalBranchId },
                f.Production, f.Configuration, f.Profile.Canonical);
            Assert.That(removal.IsSpatiallyValid, Is.True, string.Join(",", removal.ReasonCodes));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.RemoveBranch(removal))); ShellInvestment(f);
        }
        [Test]
        public void PerFloorOrdinalsStayIndependentAndMalformedLifecycleOrOrderingFails()
        {
            var f = Constructed(); Build(f);
            SavedSpatialFloor first = f.State.Floors.Single(value => value.FloorIndex == 0);
            var p = StructuralEditService.Preview(f.State, new StructuralConstructionRequest {
                FloorInstanceId = first.FloorInstanceId, RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(0, 6),
                Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "east" },
                f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            Assert.That(p.IsValid, Is.True, string.Join(",", p.ReasonCodes)); f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(p)));
            Assert.That(f.State.LifecycleAndOwnership.Floors.Select(value => value.NextNativeRoomOrdinal), Is.EqualTo(new[] { 1, 1 }));
            string[] roomIds = f.State.Floors.SelectMany(value => value.Layout.Rooms).Where(value => value.RoomInstanceId.Contains(".room.player.")).Select(value => value.RoomInstanceId).ToArray();
            Assert.That(roomIds.Distinct().Count(), Is.EqualTo(2)); ShellInvestment(f);
            Array.Reverse(f.State.Floors);
            Assert.That(CanonicalSpatialSaveContracts.Validate(f.State, f.Profile.Canonical.Spatial, true).IsValid, Is.False);
            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(f.State, f.Profile.Canonical.Spatial, out var canonical), Is.True);
            canonical.LifecycleAndOwnership.Floors = canonical.LifecycleAndOwnership.Floors.Take(1).ToArray();
            Assert.That(CanonicalSpatialSaveContracts.Validate(canonical, f.Profile.Canonical.Spatial, true).IsValid, Is.False);
        }
        [Test]
        public void CanonicalAndInvestmentWorkloadBoundariesIncludeTheAdditionalFloor()
        {
            var f = Constructed();
            int records = Enumerable.Range(1, f.Profile.Canonical.Spatial.MaximumRecords).First(limit =>
                CanonicalSpatialSaveContracts.Validate(f.State, new CanonicalSpatialSaveWorkloadLimits(limit,
                    f.Profile.Canonical.Spatial.MaximumMaterializedTiles), true).IsValid);
            Assert.That(CanonicalSpatialSaveContracts.Validate(f.State, new CanonicalSpatialSaveWorkloadLimits(records - 1,
                f.Profile.Canonical.Spatial.MaximumMaterializedTiles), true).IsValid, Is.False);
            var ledger = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context).Investment;
            Assert.That(StructuralInvestment.Valid(ledger, f.State, ledger.Length), Is.True);
            Assert.That(StructuralInvestment.Valid(ledger, f.State, ledger.Length - 1), Is.False);
        }
        [Test]
        public void BootstrapConstructionAndStableFloorSelectionUseLocalizedControls()
        {
            var f = Start(); var go = new GameObject("PhaseSixA2BootstrapQualification");
            go.SetActive(false);
            try
            {
                GameRoot root = StructuralConstructionGameRootTests.PurchaseRoot(go, f);
                root.SaveService.ConfigureFloorConstruction(Profiles(f), Research(f));
                Assert.That(root.PreviewFloorConstruction().IsCommittable, Is.True);
                Assert.That(root.CommitFloorConstruction().IsSuccess, Is.True);
                Assert.That(root.SelectedCanonicalFloor.FloorIndex, Is.Zero);
                root.CycleSelectedCanonicalFloor(); Assert.That(root.SelectedCanonicalFloorInstanceId, Is.EqualTo(FloorId));
                Assert.That(root.SelectedCanonicalFloor.ActivationState, Is.EqualTo(FloorActivationState.Inactive));
                var overlay = go.AddComponent<BootstrapOverlay>(); overlay.Bind(root);
                Assert.That(overlay.StructuralConstructionControlsAvailable, Is.True);
                foreach (string key in new[] { "ui.floor.locked", "ui.floor.unconstructed", "ui.floor.inactive", "ui.floor.active",
                    "ui.floor.construct", "ui.floor.selection", "ui.floor.price" })
                    Assert.That(root.Content.GetString(key, key), Is.Not.EqualTo(key));
                Assert.That(typeof(BootstrapOverlay).GetMethods().Any(value => value.Name.Contains("ActivateFloor") ||
                    value.Name.Contains("DeactivateFloor")), Is.False);
                root.CycleSelectedCanonicalFloor(); Assert.That(root.SelectedCanonicalFloor.FloorIndex, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
#endif
