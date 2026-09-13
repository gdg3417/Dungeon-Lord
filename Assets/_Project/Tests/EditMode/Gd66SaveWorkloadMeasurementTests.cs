#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LifecycleFixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    // Measurement-only evidence. These values are never production configuration authority.
    public class Gd66SaveWorkloadMeasurementTests
    {
        private const int High = 2000000;

        // The retained-custody horizon is derived from the already approved raw-array bound,
        // not a proposed unlimited lifetime or a new gameplay policy. Preparation is test-only.
        [Test]
        public void PhaseThreeLifecycle_EmitExactCompleteSaveMeasurements()
        {
            SaveSpatialMigrationLimitsProfile production = ProductionSaveLimits();
            var measurement = new SaveSpatialMigrationLimitsProfile(MeasurementPreparationRawLimits(),
                MeasurementPreparationSerializationLimits(), MeasurementPreparationWholeLimits());
            LifecycleFixture fixture = CreateLifecycle(measurement);
            var rows = new List<string>();
            int reusablePerCycle = ReusableOptions(fixture, "spatial.room.basic").Length;
            int cycles = production.Raw.MaximumArrayElements / reusablePerCycle;
            Assert.That(production.Raw.MaximumArrayElements % reusablePerCycle, Is.Zero);
            for (int cycle = 1; cycle <= cycles; cycle++)
            {
                string room = Construct(fixture, 0, 7, "east");
                FillContents(fixture, room, false);
                if (cycle == 1 || cycle == cycles)
                {
                    PopulateCurrentRuns(fixture);
                    rows.Add(MeasureCurrent("cycle-" + cycle + "-constructed", fixture));
                }
                DeleteTail(fixture, room);
                if (cycle == 1 || cycle == cycles)
                    rows.Add(MeasureCurrent("cycle-" + cycle + "-returned", fixture));
            }
            Assert.That(fixture.State.LifecycleAndOwnership.ReturnedContents.Length,
                Is.EqualTo(production.Raw.MaximumArrayElements));
            // Three Basic Rooms, one corridor, 59/60 tiles. This is a real production-valid
            // Phase 3 route, beyond GD66's historical R2 migration-only sizing model.
            string second = Construct(fixture, 5, 2, "north");
            string third = Construct(fixture, 5, 6, "east");
            FillContents(fixture, second, true); FillContents(fixture, third, true);
            SavedSpatialFloor full = fixture.State.Floors[0];
            FloorLayoutValidationResult geometry = FloorLayoutValidator.Validate(full.Layout,
                fixture.Production.Catalog.Floors.Single(), fixture.Production.Catalog.Rooms,
                fixture.Production.Catalog.Corridors, new SpatialValidationWorkloadLimits(
                    fixture.Profile.Canonical.Spatial.MaximumMaterializedTiles), full.FixedStructures,
                fixture.Production.Catalog.FixedStructures);
            Assert.That(geometry.IsValid, Is.True);
            Assert.That(geometry.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(59));
            Assert.That(full.RoomContents.Assignments.Length, Is.EqualTo(18));
            Assert.That(CountCanonicalRecords(fixture.State), Is.EqualTo(production.Raw.MaximumArrayElements + 37));
            PopulateCurrentRuns(fixture);
            ContentBootstrap bootstrap = ResearchBootstrap();
            fixture.Runtime.researchPending = Pending(bootstrap);
            fixture.Runtime.researchProgress = Progress(bootstrap,
                bootstrap.researchCompletionEligibilityScaffold.requiredProgressUnits / 2d, false);
            Assert.That(ResearchProgressStateResolver.Resolve(fixture.Runtime.researchPending,
                fixture.Runtime.researchProgress).RuleResolved, Is.True);
            PersistRecognized(fixture);
            rows.Add(MeasureCurrent("custody-array-envelope-r3-active-research", fixture));
            AssertProductionEnvelope(fixture, production);
            fixture.Runtime.researchPending = null; fixture.Runtime.researchProgress = null;
            fixture.Runtime.completedResearch = new CompletedResearchState {
                ProjectIds = new[] { bootstrap.researchPendingScaffold.projectId },
                LastCompletedProjectId = bootstrap.researchPendingScaffold.projectId,
                LastCompletionRuleSourceId = bootstrap.researchCompletionClaimScaffold.ruleSourceId };
            fixture.Runtime.completedObjectives = CompletedObjective();
            Assert.That(CompletedResearchStateResolver.Resolve(fixture.Runtime.completedResearch).RuleResolved, Is.True);
            PersistRecognized(fixture);
            rows.Add(MeasureCurrent("custody-array-envelope-r3-completed-research", fixture));
            AssertProductionEnvelope(fixture, production);
            foreach (string row in rows)
            { Debug.Log("PHASE3_LIMIT_MEASUREMENT " + row); TestContext.Progress.WriteLine("PHASE3_LIMIT_MEASUREMENT " + row); }
            Assert.That(rows.Count, Is.EqualTo(6));
        }

        [Test]
        public void Historical64RecordLifecycleBoundaryPreservesCustodyAndRejectsNextRecordAtomically()
        {
            SaveSpatialMigrationLimitsProfile production = ProductionSaveLimits();
            // Retain the exact reported pre-closeout exhaustion case as historical regression.
            var historical = new SaveSpatialMigrationLimitsProfile(production.Raw,
                new CanonicalSpatialSerializationLimits(production.Canonical.Serialized,
                    new CanonicalSpatialSaveWorkloadLimits(64, production.Canonical.Spatial.MaximumMaterializedTiles)), production.Whole);
            LifecycleFixture fixture = CreateLifecycle(historical);
            var issued = new HashSet<string>(StringComparer.Ordinal);
            int returnedPerCycle = ReusableOptions(fixture, "spatial.room.basic").Length;
            int cycles = 0;
            // This reproduces the actual bound by performing writes, never by manufacturing
            // returned records, collapsing identities, or bypassing the complete-save authority.
            while (CountCanonicalRecords(fixture.State) + 4 + returnedPerCycle <=
                fixture.Profile.Canonical.Spatial.MaximumRecords)
            {
                string room = Construct(fixture, 0, 7, "east");
                Assert.That(issued.Add(room), Is.True);
                FillContents(fixture, room, false);
                DeleteTail(fixture, room); cycles++;
            }
            Assert.That(cycles, Is.GreaterThan(1));
            string finalRoom = Construct(fixture, 0, 7, "east");
            Assert.That(issued.Add(finalRoom), Is.True);
            var options = ReusableOptions(fixture, "spatial.room.basic");
            int index = 0;
            while (CountCanonicalRecords(fixture.State) < fixture.Profile.Canonical.Spatial.MaximumRecords)
                PlaceOption(fixture, finalRoom, options[index++]);
            Assert.That(index, Is.LessThan(options.Length));
            PopulateCurrentRuns(fixture);
            fixture.Reopen();
            byte[] before = fixture.Session.GetCurrentBytes(); SaveData runtime = fixture.Runtime;
            var session = fixture.Session;
            ReturnedStructuralContent[] custody = fixture.State.LifecycleAndOwnership.ReturnedContents;
            Assert.That(custody.Length, Is.EqualTo(cycles * returnedPerCycle));
            Assert.That(custody.Select(value => value.AssignmentId).Distinct().Count(), Is.EqualTo(custody.Length));
            Assert.That(fixture.State.Floors[0].RoomContents.Assignments.Any(value =>
                custody.Any(item => item.AssignmentId == value.AssignmentId)), Is.False);
            DetachedCanonicalWriteResult refused = fixture.Execute(DetachedCanonicalMutationRequest.Place(
                options[index].Item1, options[index].Item2, finalRoom));
            Assert.That(refused.IsSuccess, Is.False);
            Assert.That(refused.Reason, Is.EqualTo(DetachedCanonicalSpatialMutation.ValidationFailedReason));
            Assert.That(refused.RuntimeProjection, Is.Null); Assert.That(refused.Session, Is.Null);
            Assert.That(fixture.Runtime, Is.SameAs(runtime)); Assert.That(fixture.Session, Is.SameAs(session));
            CollectionAssert.AreEqual(before, fixture.FileSystem.ReadAllBytes(fixture.ActivePath));
            CollectionAssert.AreEqual(before, fixture.Session.GetCurrentBytes());
            fixture.Reopen();
            Assert.That(fixture.State.LifecycleAndOwnership.Floors.Single().NextNativeRoomOrdinal, Is.EqualTo(cycles + 1));
            Assert.That(fixture.State.LifecycleAndOwnership.Floors.Single().NextNativeEdgeOrdinal, Is.EqualTo(cycles));
            Debug.Log("PHASE3_LIMIT_MEASUREMENT " + MeasureCurrent("historical-64-record-boundary", fixture));
            ReturnedStructuralContent selected = fixture.State.LifecycleAndOwnership.ReturnedContents
                .First(item => item.CategoryId == options[index].Item1 && item.OptionId == options[index].Item2);
            var redeployed = fixture.Execute(DetachedCanonicalMutationRequest.Redeploy(selected.AssignmentId, finalRoom));
            Assert.That(redeployed.IsSuccess, Is.True, redeployed.Reason);
            fixture.Accept(redeployed); fixture.Reopen();
            Assert.That(CountCanonicalRecords(fixture.State), Is.EqualTo(64));
            Assert.That(fixture.State.LifecycleAndOwnership.ReturnedContents.Length, Is.EqualTo(custody.Length - 1));
            Assert.That(fixture.State.Floors[0].RoomContents.Assignments.Single(a => a.AssignmentId == selected.AssignmentId).OptionId,
                Is.EqualTo(selected.OptionId));
            var tooSmall = new CanonicalSpatialSerializationLimits(fixture.Profile.Canonical.Serialized,
                new CanonicalSpatialSaveWorkloadLimits(63, fixture.Profile.Canonical.Spatial.MaximumMaterializedTiles));
            byte[] unchanged = fixture.Session.GetCurrentBytes();
            var refusedRedeployment = DetachedCanonicalSpatialMutation.Prepare(fixture.State,
                DetachedCanonicalMutationRequest.Redeploy(fixture.State.LifecycleAndOwnership.ReturnedContents[0].AssignmentId, finalRoom),
                fixture.Production, fixture.Compatibility, fixture.Configuration, tooSmall);
            Assert.That(refusedRedeployment.IsSuccess, Is.False);
            CollectionAssert.AreEqual(unchanged, fixture.FileSystem.ReadAllBytes(fixture.ActivePath));
            string row = MeasureCurrent("historical-64-record-redeployed", fixture);
            Debug.Log("REDEPLOYMENT_LIMIT_MEASUREMENT " + row);
            TestContext.Progress.WriteLine("REDEPLOYMENT_LIMIT_MEASUREMENT " + row);
        }

        [Test]
        public void ProductionLifecycle_CustodyArrayBoundaryRejectsNextReturnBeforePersistence()
        {
            LifecycleFixture fixture = CreateLifecycle(ProductionSaveLimits());
            int reusable = ReusableOptions(fixture, "spatial.room.basic").Length;
            int cycles = fixture.Profile.Raw.MaximumArrayElements / reusable;
            var issued = new HashSet<string>(StringComparer.Ordinal);
            for (int cycle = 0; cycle < cycles; cycle++)
            {
                string room = Construct(fixture, 0, 7, "east");
                Assert.That(issued.Add(room), Is.True);
                FillContents(fixture, room, false); DeleteTail(fixture, room);
            }
            Assert.That(fixture.State.LifecycleAndOwnership.ReturnedContents.Length,
                Is.EqualTo(fixture.Profile.Raw.MaximumArrayElements));
            string next = Construct(fixture, 0, 7, "east");
            Assert.That(issued.Add(next), Is.True);
            PlaceOption(fixture, next, ReusableOptions(fixture, "spatial.room.basic")[0]);
            RoomContentAssignment retained = fixture.State.Floors[0].RoomContents.Assignments.Single(value => value.RoomInstanceId == next);
            byte[] before = fixture.Session.GetCurrentBytes(); SaveData runtime = fixture.Runtime;
            var session = fixture.Session;
            var preview = StructuralDeletionService.Preview(fixture.State,
                new StructuralDeletionRequest { TargetRoomInstanceId = next }, fixture.RemovalPolicy,
                fixture.Production, fixture.Configuration, fixture.Profile.Canonical);
            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            Assert.That(preview.DetachedCandidate.LifecycleAndOwnership.ReturnedContents.Length,
                Is.EqualTo(fixture.Profile.Raw.MaximumArrayElements + 1));
            // Measure the refused complete candidate without writing it, then prove that the
            // production boot scanner and writer agree on the same 129-entry payload.
            var measurementProfile = new SaveSpatialMigrationLimitsProfile(MeasurementPreparationRawLimits(),
                fixture.Profile.Canonical, fixture.Profile.Whole);
            var measuredSession = DetachedCanonicalSaveSession.Open(before, fixture.Context, measurementProfile);
            Assert.That(measuredSession.IsSuccess, Is.True, measuredSession.Reason);
            var measuredCandidate = measuredSession.Session.PrepareSpatialOnlyReplacement(preview.DetachedCandidate,
                StructuralInvestment.Zero(preview.DetachedCandidate)); // Detached test-only workload measurement.
            Assert.That(measuredCandidate.IsSuccess, Is.True, measuredCandidate.Reason);
            var bootRefusal = RawSavePayloadClassifier.Classify(measuredCandidate.Update.GetBytes(), fixture.Profile.Raw,
                new RawSaveEnvelopeVersionContract(1, SaveMigration.LatestSchemaVersion), BlankFloor());
            Assert.That(bootRefusal.IsSuccess, Is.False);
            Assert.That(bootRefusal.FailureReason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            var result = fixture.Execute(DetachedCanonicalMutationRequest.Delete(preview));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            Assert.That(result.RuntimeProjection, Is.Null); Assert.That(result.Session, Is.Null);
            Assert.That(fixture.Runtime, Is.SameAs(runtime)); Assert.That(fixture.Session, Is.SameAs(session));
            CollectionAssert.AreEqual(before, fixture.FileSystem.ReadAllBytes(fixture.ActivePath));
            CollectionAssert.AreEqual(before, fixture.Session.GetCurrentBytes());
            fixture.Reopen();
            Assert.That(fixture.State.Floors[0].RoomContents.Assignments.Single(value => value.RoomInstanceId == next).AssignmentId,
                Is.EqualTo(retained.AssignmentId));
            Assert.That(fixture.State.LifecycleAndOwnership.ReturnedContents.Any(value => value.AssignmentId == retained.AssignmentId), Is.False);
            Assert.That(fixture.State.LifecycleAndOwnership.Floors.Single().NextNativeRoomOrdinal, Is.EqualTo(cycles + 1));
            Assert.That(fixture.State.LifecycleAndOwnership.Floors.Single().NextNativeEdgeOrdinal, Is.EqualTo(cycles));
            AssertProductionEnvelope(fixture, fixture.Profile);
        }

        [Test]
        public void ProductionCustodyArrayBoundaryRedeploysWithoutInflatingRecords()
        {
            LifecycleFixture fixture = CreateLifecycle(ProductionSaveLimits());
            int reusable = ReusableOptions(fixture, "spatial.room.basic").Length;
            int cycles = fixture.Profile.Raw.MaximumArrayElements / reusable;
            for (int cycle = 0; cycle < cycles; cycle++)
            {
                string room = Construct(fixture, 0, 7, "east");
                FillContents(fixture, room, false); DeleteTail(fixture, room);
            }
            Assert.That(fixture.State.LifecycleAndOwnership.ReturnedContents.Length,
                Is.EqualTo(fixture.Profile.Raw.MaximumArrayElements));
            string target = Construct(fixture, 0, 7, "east");
            int count = CountCanonicalRecords(fixture.State);
            var priorAssignments = fixture.State.Floors[0].RoomContents.Assignments
                .ToDictionary(a => a.AssignmentId, a => JsonUtility.ToJson(a), StringComparer.Ordinal);
            var selected = fixture.State.LifecycleAndOwnership.ReturnedContents[0];
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.Redeploy(selected.AssignmentId, target)));
            fixture.Reopen();
            Assert.That(CountCanonicalRecords(fixture.State), Is.EqualTo(count));
            Assert.That(fixture.State.LifecycleAndOwnership.ReturnedContents.Length,
                Is.EqualTo(fixture.Profile.Raw.MaximumArrayElements - 1));
            Assert.That(fixture.State.Floors[0].RoomContents.Assignments.Single(a =>
                a.AssignmentId == selected.AssignmentId).RoomInstanceId, Is.EqualTo(target));
            Assert.That(fixture.State.Floors[0].RoomContents.Assignments.Length, Is.EqualTo(priorAssignments.Count + 1));
            foreach (var prior in priorAssignments)
                Assert.That(JsonUtility.ToJson(fixture.State.Floors[0].RoomContents.Assignments.Single(a =>
                    a.AssignmentId == prior.Key)), Is.EqualTo(prior.Value));
            AssertProductionEnvelope(fixture, fixture.Profile);
            string row = MeasureCurrent("production-custody-array-boundary-redeployed", fixture);
            Debug.Log("REDEPLOYMENT_LIMIT_MEASUREMENT " + row);
            TestContext.Progress.WriteLine("REDEPLOYMENT_LIMIT_MEASUREMENT " + row);
        }

        private static void AssertProductionEnvelope(LifecycleFixture fixture, SaveSpatialMigrationLimitsProfile profile)
        {
            byte[] bytes = fixture.Session.GetCurrentBytes();
            Assert.That(RawSavePayloadClassifier.Classify(bytes, profile.Raw,
                new RawSaveEnvelopeVersionContract(1, SaveMigration.LatestSchemaVersion), BlankFloor()).IsSuccess, Is.True);
            var context = new DetachedCurrentTargetValidationContext(fixture.Compatibility, fixture.Production,
                LegacyGameplayConfigurationContract.SerializeCanonical(fixture.Configuration), profile.Canonical);
            var opened = DetachedCanonicalSaveSession.Open(bytes, context, profile);
            Assert.That(opened.IsSuccess, Is.True, opened.Reason);
            var repeated = opened.Session.PrepareSpatialOnlyReplacement(fixture.State);
            Assert.That(repeated.IsSuccess, Is.True, repeated.Reason);
            CollectionAssert.AreEqual(bytes, repeated.Update.GetBytes());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void PhaseThreeCompleteCandidate_EnforcesEachRawReadBudget(int dimension)
        {
            LifecycleFixture fixture = CreateLifecycle(ProductionSaveLimits());
            byte[] before = fixture.Session.GetCurrentBytes();
            int required = dimension == 0 ? before.Length : MinimumRaw(before, dimension - 1, 9);
            foreach (int bound in new[] { required, required - 1 })
            {
                var raw = new RawSavePayloadClassificationLimits(dimension == 0 ? bound : High,
                    dimension == 1 ? bound : 256, dimension == 2 ? bound : High,
                    dimension == 3 ? bound : High, dimension == 4 ? bound : High, dimension == 5 ? bound : High);
                var profile = new SaveSpatialMigrationLimitsProfile(raw, fixture.Profile.Canonical, fixture.Profile.Whole);
                var opened = DetachedCanonicalSaveSession.Open(before, fixture.Context, profile);
                Assert.That(opened.IsSuccess, Is.True, opened.Reason);
                var result = opened.Session.PrepareSpatialOnlyReplacement(fixture.State);
                Assert.That(result.IsSuccess, Is.EqualTo(bound == required));
                if (bound == required) CollectionAssert.AreEqual(before, result.Update.GetBytes());
                else
                {
                    Assert.That(result.Reason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
                    Assert.That(result.Update, Is.Null);
                }
                CollectionAssert.AreEqual(before, opened.Session.GetCurrentBytes());
                CollectionAssert.AreEqual(before, fixture.FileSystem.ReadAllBytes(fixture.ActivePath));
            }
        }

        private static SaveSpatialMigrationLimitsProfile ProductionSaveLimits()
        {
            var loaded = SaveSpatialMigrationLimitsLoader.Load(System.IO.File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath));
            Assert.That(loaded.IsSuccess, Is.True, loaded.Reason); return loaded.Profile;
        }

        private static LifecycleFixture CreateLifecycle(SaveSpatialMigrationLimitsProfile profile)
        {
            LifecycleFixture fixture = LifecycleFixture.Create("\"phase3UnknownPrimary\":{\"note\":\"preserve\"}",
                "\"phase3UnknownRoot\":[1,true]", profile);
            // Isolate the retained-custody workload from funding. Paid transactions have their own economy fixtures.
            var economy = JsonUtility.FromJson<DungeonBuilder.M0.Economy.StructuralEconomyConfiguration>(
                System.IO.File.ReadAllText("Assets/_Project/Resources/structural_economy.json"));
            foreach (var price in economy.Rooms.Concat(economy.Corridors)) price.Mana = 0;
            Assert.That(DungeonBuilder.M0.Economy.StructuralEconomySnapshot.TryCreate(economy,
                fixture.Production.Catalog, out fixture.Economy), Is.True);
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)));
            fixture.Runtime = RepresentativeSave(); fixture.Runtime.saveVersion = SaveMigration.LatestSchemaVersion;
            PersistRecognized(fixture);
            FillContents(fixture, fixture.State.Floors[0].Layout.Rooms.Single().RoomInstanceId, true);
            return fixture;
        }

        private static Tuple<string, string>[] ReusableOptions(LifecycleFixture fixture, string definitionId)
        {
            RoomSpatialDefinition definition = fixture.Production.Catalog.Rooms.Single(value => value.RoomDefinitionId == definitionId);
            return new[] { Tuple.Create(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId),
                Tuple.Create(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId) }
                .Take(definition.MonsterCapacity).Concat(new[] {
                    Tuple.Create(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId),
                    Tuple.Create(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SnareTrapOptionId) }
                .Take(definition.TrapCapacity)).ToArray();
        }

        private static void FillContents(LifecycleFixture fixture, string room, bool includeLoot)
        {
            string definition = fixture.State.Floors[0].Layout.Rooms.Single(value => value.RoomInstanceId == room).RoomDefinitionId;
            foreach (var option in ReusableOptions(fixture, definition)) PlaceOption(fixture, room, option);
            if (!includeLoot) return; // Shipped loot removal is unresolved; never delete a loot-bearing room.
            RoomSpatialDefinition content = fixture.Production.Catalog.Rooms.Single(value => value.RoomDefinitionId == definition);
            foreach (string option in new[] { MvpDungeonPlacementIds.BasicLootNodeOptionId,
                MvpDungeonPlacementIds.HiddenCacheOptionId }.Take(content.LootCapacity))
                PlaceOption(fixture, room, Tuple.Create(MvpDungeonPlacementIds.LootNodeCategoryId, option));
        }

        private static void PlaceOption(LifecycleFixture fixture, string room, Tuple<string, string> option) =>
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.Place(option.Item1, option.Item2, room)));

        private static string Construct(LifecycleFixture fixture, int x, int y, string terminalPoint)
        {
            var preview = StructuralEditService.Preview(fixture.State, new StructuralConstructionRequest {
                RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(x, y),
                Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = terminalPoint },
                fixture.Production, fixture.Compatibility, fixture.Configuration, fixture.Profile.Canonical);
            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.Construct(preview)));
            return preview.Consequences.Single(value => value.Kind == StructuralChangeKind.RoomAdded).StableId;
        }

        private static void DeleteTail(LifecycleFixture fixture, string room)
        {
            RoomContentAssignment[] assigned = fixture.State.Floors[0].RoomContents.Assignments
                .Where(value => value.RoomInstanceId == room).ToArray();
            var preview = StructuralDeletionService.Preview(fixture.State,
                new StructuralDeletionRequest { TargetRoomInstanceId = room }, fixture.RemovalPolicy,
                fixture.Production, fixture.Configuration, fixture.Profile.Canonical);
            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.Delete(preview)));
            fixture.Reopen();
            foreach (RoomContentAssignment assignment in assigned)
            {
                ReturnedStructuralContent returned = fixture.State.LifecycleAndOwnership.ReturnedContents.Single(value =>
                    value.AssignmentId == assignment.AssignmentId);
                Assert.That(returned.Sequence, Is.EqualTo(assignment.Sequence));
                Assert.That(returned.CategoryId, Is.EqualTo(assignment.CategoryId));
                Assert.That(returned.OptionId, Is.EqualTo(assignment.OptionId));
                Assert.That(returned.RemovalDisposition, Is.EqualTo(StructuralContentRemovalDisposition.ReturnToPlayerCustody));
            }
        }

        private static void PopulateCurrentRuns(LifecycleFixture fixture)
        {
            MvpOrderedRouteRoom[] route = CanonicalMvpRouteProjection.InspectWithProductionContent(
                fixture.Runtime, fixture.Production).Rooms;
            PopulateTenRuns(fixture.Runtime, new RunSimulationService(fixture.Configuration, ProductionLootConfig()),
                route, fixture.Configuration.MaxRunHistoryEntries);
            PersistRecognized(fixture);
        }

        private static void PersistRecognized(LifecycleFixture fixture)
        {
            fixture.Accept(fixture.Authority.SaveRecognizedState(fixture.ActivePath, fixture.FileSystem,
                fixture.Session, fixture.Runtime));
            fixture.Reopen();
        }

        private static string MeasureCurrent(string name, LifecycleFixture fixture)
        {
            byte[] bytes = fixture.Session.GetCurrentBytes();
            var classification = RawSavePayloadClassifier.Classify(bytes, MeasurementPreparationRawLimits(),
                new RawSaveEnvelopeVersionContract(1, SaveMigration.LatestSchemaVersion), BlankFloor());
            Assert.That(classification.IsSuccess, Is.True, classification.FailureReason);
            // Current canonical owners are not unknown preservation data, even though the
            // frozen legacy raw classifier describes them as unknown primary members.
            string[] owners = { "canonicalSpatialAuthority", "spatialFloors", "structuralLifecycleAndOwnership", "structuralInvestment" };
            var unknown = classification.UnknownRootMembers.Concat(classification.UnknownPrimaryMembers.Where(value =>
                !owners.Contains(value.Name))).ToArray();
            int copied = classification.Members.Where(value => value.State != RawSaveMemberState.Absent).Sum(value => value.ByteLength);
            Assert.That(MinimumWhole(fixture, 0), Is.EqualTo(bytes.Length));
            Assert.That(MinimumWhole(fixture, 1), Is.EqualTo(copied));
            Assert.That(MinimumWhole(fixture, 2), Is.EqualTo(unknown.Length));
            Assert.That(MinimumWhole(fixture, 3), Is.EqualTo(unknown.Sum(value => value.ByteLength)));
            int records = CountCanonicalRecords(fixture.State);
            Assert.That(CanonicalSpatialSaveSerializer.Serialize(fixture.State,
                new CanonicalSpatialSerializationLimits(fixture.Profile.Canonical.Serialized,
                    new CanonicalSpatialSaveWorkloadLimits(records, fixture.Profile.Canonical.Spatial.MaximumMaterializedTiles))).IsValid, Is.True);
            Assert.That(CanonicalSpatialSaveSerializer.Serialize(fixture.State,
                new CanonicalSpatialSerializationLimits(fixture.Profile.Canonical.Serialized,
                    new CanonicalSpatialSaveWorkloadLimits(records - 1, fixture.Profile.Canonical.Spatial.MaximumMaterializedTiles))).IsValid, Is.False);
            Assert.That(Encoding.UTF8.GetString(bytes), Does.Contain("\"phase3UnknownPrimary\":{\"note\":\"preserve\"}"));
            Assert.That(Encoding.UTF8.GetString(bytes), Does.Contain("\"phase3UnknownRoot\":[1,true]"));
            return name + ":rawBytes=" + bytes.Length + ",rawDepth=" + MinimumRaw(bytes, 0, 9) +
                ",rawMembers=" + MinimumRaw(bytes, 1, 9) + ",rawElements=" + MinimumRaw(bytes, 2, 9) +
                ",rawStringBytes=" + MinimumRaw(bytes, 3, 9) + ",rawScanWork=" + MinimumRaw(bytes, 4, 9) +
                ",candidateBytes=" + bytes.Length + ",strictInputBytes=" + bytes.Length +
                ",strictNodes=" + MinimumStrict(bytes, fixture.State, 0, true) +
                ",strictRecords=" + MinimumStrict(bytes, fixture.State, 1, true) +
                ",strictStringChars=" + MinimumStrict(bytes, fixture.State, 2, true) +
                ",diagnostics=0,canonicalRecords=" + records + ",canonicalTiles=" + CountCanonicalTiles(fixture.State) +
                ",copiedBytes=" + copied + ",unknownCount=" + unknown.Length + ",unknownBytes=" + unknown.Sum(value => value.ByteLength);
        }

        private static int MinimumWhole(LifecycleFixture fixture, int dimension) => Minimum(limit =>
        {
            if (limit < 1) return false;
            var profile = new SaveSpatialMigrationLimitsProfile(fixture.Profile.Raw, fixture.Profile.Canonical,
                new DetachedWholeSaveLimits(dimension == 0 ? limit : High, dimension == 1 ? limit : High,
                    dimension == 2 ? limit : High, dimension == 3 ? limit : High));
            var opened = DetachedCanonicalSaveSession.Open(fixture.Session.GetCurrentBytes(), fixture.Context, profile);
            return opened.IsSuccess && opened.Session.PrepareSpatialOnlyReplacement(fixture.State).IsSuccess;
        });

        [Test]
        public void RepositoryOwnedMigrationFixtures_EmitExactWorkloadMeasurements()
        {
            var rows = new List<string>();
            for (int schema = 1; schema <= 6; schema++)
            {
                Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                    Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(schema);
                Assert.That(fixture.Result.IsSuccess, Is.True, fixture.Result.Reason);
                rows.Add(Measure("wrapped-empty-v" + schema, fixture.Original,
                    fixture.Classification, fixture.Result.Attempt.Candidate.GetBytes(),
                    ParseState(fixture.Result.Attempt.Candidate.GetBytes(), fixture.Limits)));
            }

            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture unwrapped =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(1, true);
            rows.Add(Measure("unwrapped-v1", unwrapped.Original, unwrapped.Classification,
                unwrapped.Result.Attempt.Candidate.GetBytes(),
                ParseState(unwrapped.Result.Attempt.Candidate.GetBytes(), unwrapped.Limits)));

            const string r1 = "\"mvpRoomSlotAssignments\":{\"Rooms\":[" +
                "{\"FloorIndex\":0,\"RoomIndex\":0,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[\"placement.option.monster.skeleton\"]," +
                "\"TrapOptionIds\":[\"placement.option.trap.spike\"]," +
                "\"LootNodeOptionIds\":[\"placement.option.loot_node.basic\"]}],\"NextRevision\":4}";
            AddSemantic(rows, "populated-r1", r1);

            const string r2 = "\"mvpRoomSlotAssignments\":{\"Rooms\":[" +
                "{\"FloorIndex\":0,\"RoomIndex\":0,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[\"placement.option.monster.skeleton\",\"placement.option.monster.goblin\"]," +
                "\"TrapOptionIds\":[\"placement.option.trap.spike\",\"placement.option.trap.snare\"]," +
                "\"LootNodeOptionIds\":[\"placement.option.loot_node.basic\",\"placement.option.loot_node.hidden_cache\"]}," +
                "{\"FloorIndex\":0,\"RoomIndex\":1,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[\"placement.option.monster.skeleton\",\"placement.option.monster.goblin\"]," +
                "\"TrapOptionIds\":[\"placement.option.trap.spike\",\"placement.option.trap.snare\"]," +
                "\"LootNodeOptionIds\":[\"placement.option.loot_node.basic\",\"placement.option.loot_node.hidden_cache\"]}],\"NextRevision\":13}";
            AddSemantic(rows, "maximum-content-r2", r2);

            const string implicitContainer = "\"mvpDungeonPlacements\":{\"Entries\":[" +
                "{\"CategoryId\":\"placement.category.monster\",\"OptionId\":\"placement.option.monster.skeleton\",\"Revision\":1}," +
                "{\"CategoryId\":\"placement.category.trap\",\"OptionId\":\"placement.option.trap.spike\",\"Revision\":2}," +
                "{\"CategoryId\":\"placement.category.loot_node\",\"OptionId\":\"placement.option.loot_node.basic\",\"Revision\":3}],\"NextRevision\":4}";
            AddSemantic(rows, "implicit-content-container", implicitContainer);

            SaveData runHistorySave = RepresentativeLegacySaveWithTenPersistedRuns();
            AddSerializedSave(rows, "legacy-runtime-ten-run-history", runHistorySave);
            SaveData canonicalTargetRunHistory = CanonicalTargetSaveWithTenPersistedRuns();
            AddSerializedSave(rows, "canonical-target-ten-run-history", canonicalTargetRunHistory);

            ContentBootstrap bootstrap = ResearchBootstrap();
            SaveData pendingResearch = RepresentativeSave();
            pendingResearch.researchPending = Pending(bootstrap);
            Assert.That(ResearchPendingResolver.Resolve(pendingResearch.researchPending,
                bootstrap.researchPendingScaffold).RuleResolved, Is.True);
            AddSerializedSave(rows, "research-pending", pendingResearch);

            SaveData activeResearch = RepresentativeSave();
            activeResearch.researchPending = Pending(bootstrap);
            activeResearch.researchProgress = Progress(bootstrap,
                bootstrap.researchCompletionEligibilityScaffold.requiredProgressUnits / 2d, false);
            Assert.That(ResearchProgressStateResolver.Resolve(activeResearch.researchPending,
                activeResearch.researchProgress).RuleResolved, Is.True);
            ResearchCompletionEligibilitySummary activeEligibility =
                ResearchCompletionEligibilityResolver.Resolve(activeResearch.researchPending,
                    activeResearch.researchProgress, bootstrap.researchCompletionEligibilityScaffold);
            Assert.That(activeEligibility.RuleResolved, Is.True);
            Assert.That(activeEligibility.EligibleForCompletion, Is.False);
            AddSerializedSave(rows, "research-active-progress", activeResearch);

            SaveData completionPending = RepresentativeSave();
            completionPending.researchPending = Pending(bootstrap);
            completionPending.researchProgress = Progress(bootstrap,
                bootstrap.researchCompletionEligibilityScaffold.requiredProgressUnits, true);
            ResearchCompletionClaimReadinessSummary readiness =
                ResearchCompletionClaimReadinessResolver.Resolve(completionPending.researchPending,
                    completionPending.researchProgress, bootstrap.researchCompletionEligibilityScaffold);
            Assert.That(readiness.RuleResolved, Is.True);
            Assert.That(readiness.ReadyForClaim, Is.True);
            AddSerializedSave(rows, "research-completion-pending", completionPending);

            SaveData completed = RepresentativeSave();
            completed.completedResearch = new CompletedResearchState
            {
                ProjectIds = new[] { bootstrap.researchPendingScaffold.projectId },
                LastCompletedProjectId = bootstrap.researchPendingScaffold.projectId,
                LastCompletionRuleSourceId = bootstrap.researchCompletionClaimScaffold.ruleSourceId
            };
            completed.completedObjectives = CompletedObjective();
            Assert.That(CompletedResearchStateResolver.Resolve(completed.completedResearch)
                .RuleResolved, Is.True);
            AddSerializedSave(rows, "research-and-objective-completed", completed);

            SaveData activeHighWater = CanonicalTargetSaveWithTenPersistedRuns();
            activeHighWater.researchPending = Pending(bootstrap);
            activeHighWater.researchProgress = Progress(bootstrap,
                bootstrap.researchCompletionEligibilityScaffold.requiredProgressUnits / 2d, false);
            Assert.That(ResearchProgressStateResolver.Resolve(activeHighWater.researchPending,
                activeHighWater.researchProgress).RuleResolved, Is.True);
            AddSerializedSave(rows, "canonical-target-full-save-high-water-active-research", activeHighWater);

            SaveData completedHighWater = CanonicalTargetSaveWithTenPersistedRuns();
            completedHighWater.completedResearch = completed.completedResearch;
            completedHighWater.completedObjectives = CompletedObjective();
            Assert.That(CompletedResearchStateResolver.Resolve(completedHighWater.completedResearch)
                .RuleResolved, Is.True);
            AddSerializedSave(rows, "canonical-target-full-save-high-water-completed-research-objective",
                completedHighWater);

            const string unknownJson = "{\"rootBefore\":[1,{\"x\":true}],\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"saveVersion\":6,\"unknownPrimary\":{\"note\":\"preserve\"}},\"rootAfter\":false}";
            byte[] unknownBytes = Encoding.UTF8.GetBytes(unknownJson);
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture unknown =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, unknownBytes);
            Assert.That(unknown.Result.IsSuccess, Is.True, unknown.Result.Reason);
            rows.Add(Measure("unknown-root-primary", unknown.Original, unknown.Classification,
                unknown.Result.Attempt.Candidate.GetBytes(),
                ParseState(unknown.Result.Attempt.Candidate.GetBytes(), unknown.Limits)));

            AddNativeCanonical(rows, unknown);

            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture sidecarFixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            DetachedPreparedSpatialMigrationAttempt attempt = sidecarFixture.Result.Attempt;
            SpatialContractResult<byte[]> descriptorBytes = SpatialMigrationDescriptorContracts.Serialize(
                attempt.Descriptor, sidecarFixture.Limits.Serialized);
            Assert.That(descriptorBytes.IsValid, Is.True);
            rows.Add(MeasureArtifact("descriptor", descriptorBytes.Value));
            string identity = attempt.TransactionIdentity;
            SpatialMigrationSidecarNames names = SpatialMigrationSidecarPaths.Derive(
                "save.json", attempt.TransactionId).Value;
            foreach (SpatialMigrationJournalStage stage in Enum.GetValues(typeof(SpatialMigrationJournalStage)))
            {
                bool hasBackup = stage != SpatialMigrationJournalStage.DescriptorPinned;
                bool hasCandidate = stage == SpatialMigrationJournalStage.CandidateVerified ||
                    stage == SpatialMigrationJournalStage.Replaced ||
                    stage == SpatialMigrationJournalStage.DurableVerified ||
                    stage == SpatialMigrationJournalStage.Finalized;
                var journal = new SpatialMigrationJournal(SpatialMigrationContractIdentity.JournalSchemaVersion,
                    attempt.Descriptor, attempt.DescriptorFingerprint, identity, attempt.TransactionId,
                    names.Journal, names.OriginalBackup, names.CandidateStaging,
                    stage == SpatialMigrationJournalStage.Finalized ? names.FinalizedReceipt : null,
                    attempt.Descriptor.OriginalPayloadSha256,
                    hasBackup ? attempt.Descriptor.OriginalPayloadSha256 : null,
                    hasCandidate ? attempt.CandidateSha256 : null, stage);
                SpatialContractResult<byte[]> journalBytes = SpatialMigrationJournalContracts.Serialize(
                    journal, sidecarFixture.Limits.Serialized);
                Assert.That(journalBytes.IsValid, Is.True, stage.ToString());
                rows.Add(MeasureArtifact("journal-" + stage, journalBytes.Value));
            }
            byte[] receipt = DetachedFinalizationReceiptContract.Serialize(
                new DetachedFinalizationReceipt(attempt.TransactionId, attempt.DescriptorFingerprint,
                    attempt.CandidateSha256), sidecarFixture.Limits.Serialized);
            rows.Add(MeasureArtifact("finalization-receipt", receipt));
            byte[] restoration = DetachedRestorationIntentContract.Serialize(
                new DetachedRestorationIntent(attempt.TransactionId, attempt.DescriptorFingerprint,
                    attempt.Descriptor.OriginalPayloadSha256, attempt.Descriptor.OriginalPayloadSha256,
                    names.Journal, (int)SpatialMigrationJournalStage.DurableVerified),
                sidecarFixture.Limits.Serialized);
            rows.Add(MeasureArtifact("restoration-intent", restoration));

            foreach (string row in rows)
            {
                string line = "GD66_LIMIT_MEASUREMENT " + row;
                Debug.Log(line);
                TestContext.Progress.WriteLine(line);
            }
            Assert.That(rows.Count, Is.EqualTo(30));
        }

        [Test]
        public void LegacyPersistenceBytes_MatchPrettyPrintedSaveRootRepresentation()
        {
            SaveData save = RepresentativeSave();
            byte[] measured = SerializeLegacyPersistence(save);
            string expected = JsonUtility.ToJson(new SaveRoot
            {
                schemaVersion = SaveMigration.LegacyCompatibilitySchemaVersion,
                primary = save
            }, true);

            Assert.That(Encoding.UTF8.GetString(measured), Is.EqualTo(expected));
            Assert.That(expected, Does.Contain("\n"));
            Assert.That(expected, Is.Not.EqualTo(JsonUtility.ToJson(new SaveRoot
            {
                schemaVersion = SaveMigration.LegacyCompatibilitySchemaVersion,
                primary = save
            })));
        }

        [Test]
        public void RawMinimumSearch_StartsAtValidOneAndFindsKnownFixtureDimensions()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(
                "{\"schema\":\"save_root\",\"schemaVersion\":6,\"primary\":{}}");
            Assert.That(MinimumRaw(bytes, 0), Is.EqualTo(2));
            Assert.That(MinimumRaw(bytes, 1), Is.EqualTo(3));
            Assert.That(MinimumRaw(bytes, 2), Is.EqualTo(1));
            Assert.That(MinimumRaw(bytes, 3), Is.EqualTo(13));
            Assert.That(MinimumRaw(bytes, 4), Is.GreaterThan(0));
        }

        [Test]
        public void ProposedCrossFieldProfile_HasReachableWholeCandidateBoundary()
        {
            const int serializedInput = 262144;
            const int candidate = 262144;
            Assert.That(candidate, Is.LessThanOrEqualTo(serializedInput),
                "Complete candidate parsing makes a larger candidate success boundary unreachable.");

            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            byte[] bytes = fixture.Result.Attempt.Candidate.GetBytes();
            Assert.That(bytes.Length, Is.LessThanOrEqualTo(candidate));
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaSevenAndRoundTrip(bytes,
                new CanonicalSpatialSerializationLimits(
                    new SpatialSerializedInputLimits(serializedInput, 8192, 2048, 131072, 64),
                    new CanonicalSpatialSaveWorkloadLimits(64, 64))).IsValid, Is.True);
        }

        private static SaveData RepresentativeSave()
        {
            DungeonLayoutState expectedLayout = PopulatedDungeonLayout();
            MvpRoomSlotAssignmentCollection expectedAssignments = LegacyRuntimeR2Assignments();
            var root = new SaveRoot
            {
                schemaVersion = SaveMigration.LegacyCompatibilitySchemaVersion,
                primary = new SaveData
                {
                    saveVersion = 6, contentVersion = "gd66-measurement",
                    createdUtcUnix = 1700000000L, lastPausedUtcUnix = 1700086400L,
                    lastResumedUtcUnix = 1700086460L, lastSavedUtcUnix = 1700172800L,
                    totalTicks = 987654L, lastKnownAppState = "Paused",
                    dungeonLayout = expectedLayout,
                    structureRuntime = new StructureRuntimeState { ManaReserve = 250d, Heat = 37d },
                    mvpRoomSlotAssignments = expectedAssignments,
                    lastOfflineSummary = new OfflineSummary
                    {
                        RuleResolved = true, OfflineSecondsObserved = 60,
                        RuleSourceIdUsed = "offline.rule.measurement"
                    }
                }
            };

            root = SaveMigration.MigrateToLatest(root);
            SaveData save = root.primary;
            Assert.That(root.schemaVersion, Is.EqualTo(SaveMigration.LegacyCompatibilitySchemaVersion));
            Assert.That(save.dungeonLayout, Is.SameAs(expectedLayout));
            Assert.That(save.dungeonLayout.Slots.Count, Is.EqualTo(30));
            Assert.That(save.mvpRoomSlotAssignments, Is.SameAs(expectedAssignments));
            Assert.That(save.mvpRoomSlotAssignments.Rooms.Count, Is.EqualTo(2));
            Assert.That(save.mvpDungeonPlacements, Is.Not.Null);
            Assert.That(save.mvpDungeonPlacements.Entries, Is.Not.Null);
            Assert.That(save.mvpDungeonFloorLayout, Is.Not.Null);
            Assert.That(save.mvpDungeonFloorLayout.Nodes, Is.Not.Null);
            Assert.That(save.structureRuntime, Is.Not.Null);
            Assert.That(save.runHistory, Is.Not.Null);
            Assert.That(save.runHistory.RecentOutcomes, Is.Not.Null);
            Assert.That(save.completedObjectives, Is.Not.Null);
            Assert.That(save.completedObjectives.ObjectiveIds, Is.Not.Null);
            return save;
        }

        private static SaveData RepresentativeLegacySaveWithTenPersistedRuns()
        {
            SaveData save = RepresentativeSave();
            RunSimulationConfig config = ProductionRunConfig();
            LootConfig loot = ProductionLootConfig();
            var simulation = new RunSimulationService(config, loot);
            MvpOrderedRouteRoom[] route = MvpOrderedRoomRouteResolver.Resolve(save, config);
            Assert.That(route.Length, Is.EqualTo(2));
            foreach (MvpOrderedRouteRoom room in route)
            {
                Assert.That(room.Capacity.MonsterCapacity, Is.EqualTo(1));
                Assert.That(room.Capacity.TrapCapacity, Is.EqualTo(1));
                Assert.That(room.Capacity.LootCapacity, Is.EqualTo(1));
                Assert.That(room.AssignedMonsterOptionIds.Length, Is.EqualTo(1));
                Assert.That(room.AssignedTrapOptionIds.Length, Is.EqualTo(1));
                Assert.That(room.AssignedLootNodeOptionIds.Length, Is.EqualTo(1));
            }
            PopulateTenRuns(save, simulation, route, config.MaxRunHistoryEntries);
            return save;
        }

        private static SaveData CanonicalTargetSaveWithTenPersistedRuns()
        {
            SaveData save = RepresentativeSave();
            save.mvpRoomSlotAssignments = CanonicalTargetR2Assignments();
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture spatialFixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            string basicDefinitionId = spatialFixture.Compatibility.Value.GeometryRecords.Single()
                .BasicRoomDefinitionId;
            RoomSpatialDefinition definition = spatialFixture.Production.Catalog.Rooms.Single(
                room => room.RoomDefinitionId == basicDefinitionId);
            Assert.That(definition.MonsterCapacity, Is.EqualTo(2));
            Assert.That(definition.TrapCapacity, Is.EqualTo(2));
            Assert.That(definition.LootCapacity, Is.EqualTo(2));

            RunSimulationConfig config = ProductionRunConfig();
            LootConfig loot = ProductionLootConfig();
            var simulation = new RunSimulationService(config, loot);
            MvpOrderedRouteRoom[] route = Enumerable.Range(0, 2).Select(index =>
                new MvpOrderedRouteRoom
                {
                    FloorIndex = 0, RoomIndex = index,
                    RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                    IncludeRoomPlacement = true, HasActiveContent = true,
                    Capacity = new MvpRoomSlotCapacity
                    {
                        RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                        MonsterCapacity = definition.MonsterCapacity,
                        TrapCapacity = definition.TrapCapacity,
                        LootCapacity = definition.LootCapacity
                    },
                    AssignedMonsterOptionIds = new[] { MvpDungeonPlacementIds.SkeletonOptionId,
                        MvpDungeonPlacementIds.GoblinOptionId },
                    AssignedTrapOptionIds = new[] { MvpDungeonPlacementIds.SpikeTrapOptionId,
                        MvpDungeonPlacementIds.SnareTrapOptionId },
                    AssignedLootNodeOptionIds = new[] { MvpDungeonPlacementIds.BasicLootNodeOptionId,
                        MvpDungeonPlacementIds.HiddenCacheOptionId }
                }).ToArray();
            PopulateTenRuns(save, simulation, route, config.MaxRunHistoryEntries);
            return save;
        }

        private static MvpRoomSlotAssignmentCollection LegacyRuntimeR2Assignments() =>
            Assignments(new[] { MvpDungeonPlacementIds.SkeletonOptionId },
                new[] { MvpDungeonPlacementIds.SpikeTrapOptionId },
                new[] { MvpDungeonPlacementIds.BasicLootNodeOptionId }, 7);

        private static MvpRoomSlotAssignmentCollection CanonicalTargetR2Assignments() =>
            Assignments(new[] { MvpDungeonPlacementIds.SkeletonOptionId,
                    MvpDungeonPlacementIds.GoblinOptionId },
                new[] { MvpDungeonPlacementIds.SpikeTrapOptionId,
                    MvpDungeonPlacementIds.SnareTrapOptionId },
                new[] { MvpDungeonPlacementIds.BasicLootNodeOptionId,
                    MvpDungeonPlacementIds.HiddenCacheOptionId }, 13);

        private static MvpRoomSlotAssignmentCollection Assignments(string[] monsters,
            string[] traps, string[] loot, int nextRevision) => new MvpRoomSlotAssignmentCollection
        {
            NextRevision = nextRevision,
            Rooms = Enumerable.Range(0, 2).Select(index => new MvpRoomSlotAssignmentState
            {
                FloorIndex = 0, RoomIndex = index,
                RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                MonsterOptionIds = (string[])monsters.Clone(),
                TrapOptionIds = (string[])traps.Clone(),
                LootNodeOptionIds = (string[])loot.Clone()
            }).ToList()
        };

        private static DungeonLayoutState PopulatedDungeonLayout()
        {
            DungeonLayoutState layout = DungeonLayoutState.CreateEmpty(
                SaveMigration.DefaultFloorCount, SaveMigration.DefaultSlotsPerFloor);
            string[] structures = { StructureSimulationPass.ManaGeneratorBasicId,
                StructureSimulationPass.HeatScrubberBasicId, StructureSimulationPass.RiskLabBasicId };
            for (int index = 0; index < layout.Slots.Count; index++)
            {
                DungeonSlot slot = layout.Slots[index];
                layout.Slots[index] = new DungeonSlot(slot.FloorIndex, slot.SlotIndex,
                    structures[index % structures.Length]);
            }
            Assert.That(layout.Slots.Count, Is.EqualTo(30));
            Assert.That(layout.Slots.All(slot => slot.IsOccupied), Is.True);
            return layout;
        }

        private static RunSimulationConfig ProductionRunConfig() => LoadAsset<RunSimulationConfig>(
            "Assets/_Project/Data/Bootstrap/run_simulation_config.json");

        private static LootConfig ProductionLootConfig() => LoadAsset<LootConfig>(
            "Assets/_Project/Data/Bootstrap/loot_config.json");

        private static T LoadAsset<T>(string path) where T : class
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            Assert.That(asset, Is.Not.Null, path);
            return JsonUtility.FromJson<T>(asset.text);
        }

        private static void PopulateTenRuns(SaveData save, RunSimulationService simulation,
            MvpOrderedRouteRoom[] route, int maximumHistory)
        {
            Assert.That(maximumHistory, Is.EqualTo(10));
            save.runHistory = new RunHistoryState();
            for (int sequence = 1; sequence <= maximumHistory; sequence++)
            {
                RunOutcomeRecord outcome = simulation.SimulateRoute(save.structureRuntime,
                    1700000000L + sequence * 60L, sequence, RunPostureResolver.BalancedId, route);
                save.runHistory.AppendOutcome(outcome, maximumHistory);
                save.runHistory.NextRunSequence = sequence + 1;
            }
            Assert.That(save.runHistory.RecentOutcomes.Length, Is.EqualTo(maximumHistory));
        }

        private static ContentBootstrap ResearchBootstrap()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_Project/Data/Bootstrap/content_bootstrap.json");
            Assert.That(asset, Is.Not.Null);
            ContentBootstrap bootstrap = JsonUtility.FromJson<ContentBootstrap>(asset.text);
            Assert.That(bootstrap.researchPendingScaffold, Is.Not.Null);
            Assert.That(bootstrap.researchProgressScaffold, Is.Not.Null);
            Assert.That(bootstrap.researchCompletionEligibilityScaffold, Is.Not.Null);
            return bootstrap;
        }

        private static ResearchPendingState Pending(ContentBootstrap bootstrap) =>
            new ResearchPendingState
            {
                SlotId = bootstrap.researchPendingScaffold.slotId,
                ProjectId = bootstrap.researchPendingScaffold.projectId
            };

        private static ResearchProgressState Progress(ContentBootstrap bootstrap,
            double units, bool completionPending) => new ResearchProgressState
        {
            SlotId = bootstrap.researchPendingScaffold.slotId,
            ProjectId = bootstrap.researchPendingScaffold.projectId,
            ProgressUnits = units,
            CompletionPending = completionPending,
            RuleSourceIdUsed = bootstrap.researchProgressScaffold.ruleSourceId
        };

        private static CompletedObjectiveState CompletedObjective() => new CompletedObjectiveState
        {
            ObjectiveIds = new[] { "objective.first_dungeon_contract" },
            LastCompletedObjectiveId = "objective.first_dungeon_contract",
            LastCompletionRuleSourceId = CompletedObjectiveStateResolver.FirstSessionObjectiveCompletionRuleSourceId
        };

        // Permissive classification ceiling for measurement preparation only. Actual requirements
        // are still discovered independently through MinimumRaw and never use these values as evidence.
        private static RawSavePayloadClassificationLimits MeasurementPreparationRawLimits() =>
            new RawSavePayloadClassificationLimits(1000000, 128, 1000, 1000, 100000, 5000000);

        // Permissive, bounded preparation ceiling for measurement only. These values are not
        // measurements or proposed production authority; each workload is measured independently.
        private static DetachedWholeSaveLimits MeasurementPreparationWholeLimits() =>
            new DetachedWholeSaveLimits(2000000, 1000000, 10000, 1000000);

        private static CanonicalSpatialSerializationLimits MeasurementPreparationSerializationLimits() =>
            new CanonicalSpatialSerializationLimits(
                new SpatialSerializedInputLimits(2000000, 200000, 20000, 200000, 100),
                new CanonicalSpatialSaveWorkloadLimits(20000, 20000));

        private static byte[] SerializeLegacyPersistence(SaveData save)
        {
            var root = new SaveRoot
            {
                schemaVersion = SaveMigration.LegacyCompatibilitySchemaVersion,
                primary = save
            };
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(root, true));
        }

        private static void AddSerializedSave(List<string> rows, string name, SaveData save)
        {
            byte[] raw = SerializeLegacyPersistence(save);
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, raw,
                    MeasurementPreparationRawLimits(), MeasurementPreparationWholeLimits(),
                    MeasurementPreparationSerializationLimits());
            Assert.That(fixture.Result.IsSuccess, Is.True, fixture.Result.Reason);
            rows.Add(Measure(name, fixture.Original, fixture.Classification,
                fixture.Result.Attempt.Candidate.GetBytes(),
                ParseState(fixture.Result.Attempt.Candidate.GetBytes(), fixture.Limits)));
        }

        private static void AddNativeCanonical(List<string> rows,
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture)
        {
            int contractVersion = fixture.Compatibility.SelectContract(7).Value.CanonicalLayoutContractVersion;
            CompatibilitySelectionResult<CanonicalStarterLayoutProfile> starter =
                fixture.Compatibility.SelectStarter(7, contractVersion);
            Assert.That(starter.Success, Is.True, starter.Code);
            Assert.That(starter.Value.CanonicalLayoutContractVersion, Is.EqualTo(contractVersion));
            var state = new DetachedCanonicalSpatialSaveState
            {
                Authority = new CanonicalSpatialAuthorityMarker
                {
                    CanonicalLayoutContractVersion = contractVersion,
                    CreationKind = CanonicalSpatialCreationKind.NativeCanonical
                },
                Floors = Array.Empty<SavedSpatialFloor>()
            };
            SpatialContractResult<CanonicalSpatialSaveSerializer.SerializedMembers> members =
                CanonicalSpatialSaveSerializer.SerializeFrozenSchemaSevenMembers(state, fixture.Limits);
            Assert.That(members.IsValid, Is.True);
            byte[] candidate = Encoding.UTF8.GetBytes(
                "{\"schema\":\"save_root\",\"schemaVersion\":7,\"primary\":{\"canonicalSpatialAuthority\":" +
                Encoding.UTF8.GetString(members.Value.Authority) + ",\"spatialFloors\":" +
                Encoding.UTF8.GetString(members.Value.Floors) + "}}");
            DetachedCompleteSaveValidationResult validated =
                DetachedCompleteSaveContract.ParseValidateFrozenSchemaSevenAndRoundTrip(candidate, fixture.Limits);
            Assert.That(validated.IsValid, Is.True, validated.Reason);
            rows.Add("native-canonical-empty:raw=not-applicable,candidateBytes=" + candidate.Length +
                ",strictNodes=" + MinimumStrict(candidate, validated.State, 0) +
                ",strictRecords=" + MinimumStrict(candidate, validated.State, 1) +
                ",strictStringChars=" + MinimumStrict(candidate, validated.State, 2) +
                ",canonicalRecords=" + CountCanonicalRecords(validated.State) +
                ",canonicalTiles=" + CountCanonicalTiles(validated.State) +
                ",copiedBytes=0,unknownCount=0,unknownBytes=0");
        }

        private static void AddSemantic(List<string> rows, string name, string members)
        {
            Gd66DetachedSpatialMigrationTransactionTests.SemanticFixtureExecution fixture =
                Gd66DetachedSpatialMigrationTransactionTests.RunPopulatedSemanticFixture(name, 6, members);
            rows.Add(Measure(name, fixture.Attempt.GetOriginalBytes(), fixture.Classification,
                fixture.Attempt.Candidate.GetBytes(), fixture.State));
        }

        private static DetachedCanonicalSpatialSaveState ParseState(byte[] candidate,
            CanonicalSpatialSerializationLimits limits)
        {
            DetachedCompleteSaveValidationResult result =
                DetachedCompleteSaveContract.ParseValidateFrozenSchemaSevenAndRoundTrip(candidate, limits);
            Assert.That(result.IsValid, Is.True, result.Reason);
            return result.State;
        }

        private static string MeasureArtifact(string name, byte[] bytes) => name +
            ":strictBytes=" + bytes.Length + ",strictNodes=" + MinimumContract(bytes, 0) +
            ",strictRecords=" + MinimumContract(bytes, 1) +
            ",strictStringChars=" + MinimumContract(bytes, 2);

        private static int MinimumContract(byte[] bytes, int dimension) => Minimum(limit =>
        {
            var limits = new SpatialSerializedInputLimits(bytes.Length,
                dimension == 0 ? limit : High, dimension == 1 ? limit : High,
                dimension == 2 ? limit : High, 64);
            var issues = new SpatialIssueCollector(64);
            return ContractJson.TryParse(bytes, limits, issues, out _);
        });

        private static string Measure(string name, byte[] raw, RawSavePayloadClassification classification,
            byte[] candidate, DetachedCanonicalSpatialSaveState state)
        {
            int copied = classification.Members.Where(value => value.State != RawSaveMemberState.Absent)
                .Sum(value => value.ByteLength);
            int unknownCount = classification.UnknownRootMembers.Count + classification.UnknownPrimaryMembers.Count;
            int unknownBytes = classification.UnknownRootMembers.Sum(value => value.ByteLength) +
                classification.UnknownPrimaryMembers.Sum(value => value.ByteLength);
            int records = CountCanonicalRecords(state);
            int tiles = CountCanonicalTiles(state);
            string rawMetrics = raw == null ? "raw=not-retained" :
                "rawBytes=" + raw.Length + ",rawDepth=" + MinimumRaw(raw, 0) +
                ",rawMembers=" + MinimumRaw(raw, 1) + ",rawElements=" + MinimumRaw(raw, 2) +
                ",rawStringBytes=" + MinimumRaw(raw, 3) + ",rawScanWork=" + MinimumRaw(raw, 4);
            return name + ":" + rawMetrics + ",candidateBytes=" + candidate.Length +
                ",strictNodes=" + MinimumStrict(candidate, state, 0) +
                ",strictRecords=" + MinimumStrict(candidate, state, 1) +
                ",strictStringChars=" + MinimumStrict(candidate, state, 2) +
                ",canonicalRecords=" + records + ",canonicalTiles=" + tiles +
                ",copiedBytes=" + copied + ",unknownCount=" + unknownCount +
                ",unknownBytes=" + unknownBytes;
        }

        private static int MinimumRaw(byte[] bytes, int dimension, int maximumSchema = 6) => Minimum(limit =>
        {
            if (limit < 1) return false;
            int depth = dimension == 0 ? limit : 256;
            int members = dimension == 1 ? limit : High;
            int elements = dimension == 2 ? limit : High;
            int strings = dimension == 3 ? limit : High;
            int work = dimension == 4 ? limit : High;
            return RawSavePayloadClassifier.Classify(bytes,
                new RawSavePayloadClassificationLimits(bytes.Length, depth, members, elements, strings, work),
                new RawSaveEnvelopeVersionContract(1, maximumSchema), BlankFloor()).IsSuccess;
        });

        private static int MinimumStrict(byte[] bytes, DetachedCanonicalSpatialSaveState state, int dimension,
            bool current = false) => Minimum(limit =>
        {
            var limits = new CanonicalSpatialSerializationLimits(new SpatialSerializedInputLimits(bytes.Length,
                    dimension == 0 ? limit : High, dimension == 1 ? limit : High,
                    dimension == 2 ? limit : High, 64),
                    new CanonicalSpatialSaveWorkloadLimits(Math.Max(1, CountCanonicalRecords(state)),
                        Math.Max(1, CountCanonicalTiles(state))));
            return (current ? DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, limits) :
                DetachedCompleteSaveContract.ParseValidateFrozenSchemaSevenAndRoundTrip(bytes, limits)).IsValid;
        });

        private static int Minimum(Func<int, bool> accepts)
        {
            if (accepts(0)) return 0;
            int low = 0, high = 1;
            while (high < High && !accepts(high)) high *= 2;
            Assert.That(accepts(high), Is.True, "Measurement ceiling was insufficient.");
            while (low + 1 < high)
            {
                int middle = low + (high - low) / 2;
                if (accepts(middle)) high = middle; else low = middle;
            }
            return high;
        }

        private static int CountCanonicalRecords(DetachedCanonicalSpatialSaveState state) =>
            (state?.LifecycleAndOwnership?.Floors?.Length ?? 0) +
            (state?.LifecycleAndOwnership?.ReturnedContents?.Length ?? 0) +
            (state?.Floors ?? Array.Empty<SavedSpatialFloor>()).Sum(floor => floor == null ? 1 : 1 +
                (floor.Layout?.Rooms?.Length ?? 0) + (floor.Layout?.Nodes?.Length ?? 0) +
                (floor.Layout?.Edges?.Length ?? 0) + (floor.FixedStructures?.Length ?? 0) +
                (floor.RoomContents?.Assignments?.Length ?? 0) +
                (floor.RoomContents?.RoomSemantics?.Length ?? 0));

        private static int CountCanonicalTiles(DetachedCanonicalSpatialSaveState state) =>
            (state?.Floors ?? Array.Empty<SavedSpatialFloor>()).Where(floor => floor?.Layout?.Edges != null)
                .SelectMany(floor => floor.Layout.Edges).Sum(edge => edge?.Footprint?.OccupiedTiles?.Length ?? 0);

        private static RawLegacyBlankFloorContract BlankFloor() => new RawLegacyBlankFloorContract(1,
            Enumerable.Range(0, 4).Select(index => new RawLegacyBlankFloorNodeContract(
                0, index, "slot." + index, "", "", 0)), true, true,
            new[] { "Nodes", "NextRevision" },
            new[] { "FloorIndex", "NodeIndex", "SlotId", "CategoryId", "OptionId", "Revision" });
    }
}
#endif
