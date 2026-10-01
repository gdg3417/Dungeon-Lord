#if UNITY_EDITOR
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class PhaseSixAActivationTests
    {
        [Test]
        public void CurrentConstantsAndNativeEmptyCreationUseTwelve()
        {
            Fixture f = Fixture.Create(null);
            Assert.That(SaveMigration.LatestSchemaVersion, Is.EqualTo(12));
            Assert.That(CanonicalSaveSchemaVersions.CurrentWritableTarget, Is.EqualTo(12));
            var fs = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            NativeCanonicalSaveResult created = NativeCanonicalSaveCreator.Create(f.ActivePath, fs,
                f.Runtime, f.Compatibility, f.Production,
                    LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.Profile, f.Acquisition);
            Assert.That(created.IsSuccess, Is.True, created.Reason);
            Assert.That(created.Validation.State.Floors, Is.Empty);
            Assert.That(Encoding.UTF8.GetString(created.Session.GetCurrentBytes()),
                Does.Contain("\"schemaVersion\":12"));
            string native = Encoding.UTF8.GetString(created.Session.GetCurrentBytes());
            Assert.That(native.Split(new[] { "\"sharedFloorKnowledge\"" },
                StringSplitOptions.None).Length, Is.EqualTo(2));
            Assert.That(created.Validation.FloorKnowledge.Records, Is.Empty);
            var issues = new SpatialIssueCollector(f.Profile.Canonical.Serialized.MaximumDiagnostics);
            Assert.That(ContractJson.TryParse(created.Session.GetCurrentBytes(),
                f.Profile.Canonical.Serialized, issues, out ContractJsonNode root), Is.True);
            ContractJsonNode schemaVersion = root.Fields.Single(field => field.Key == "schemaVersion").Value;
            Assert.That(schemaVersion.Kind, Is.EqualTo(ContractJsonKind.Number));
            Assert.That(schemaVersion.Text, Is.EqualTo(
                CanonicalSaveSchemaVersions.CurrentWritableTarget.ToString(CultureInfo.InvariantCulture)));
            CollectionAssert.AreEqual(created.Session.GetCurrentBytes(), fs.ReadAllBytes(f.ActivePath));
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                fs.ReadAllBytes(f.ActivePath), f.Context).CurrentTargetValidated, Is.True);
        }

        [Test]
        public void ActiveFirstFloorHasExactRoundTripAndDeclaredCanonicalFieldOrder()
        {
            Fixture f = OneFloor();
            Assert.That(f.State.Floors.Single().ActivationState, Is.EqualTo(FloorActivationState.Active));
            f.State.Authority.CreationKind = CanonicalSpatialCreationKind.NativeCanonical;
            f.State.Authority.MigrationTransactionId = null;
            f.State.Authority.MigrationDescriptorFingerprint = null;
            SpatialContractResult<byte[]> bytes = CanonicalSpatialSaveSerializer.Serialize(f.State, f.Profile.Canonical);
            Assert.That(bytes.IsValid, Is.True);
            Assert.That(Encoding.UTF8.GetString(bytes.Value),
                Does.Contain("\"FloorIndex\":0,\"ActivationState\":1,\"Layout\":"));
            var parsed = CanonicalSpatialSaveSerializer.Parse(bytes.Value, f.Profile.Canonical);
            Assert.That(parsed.IsValid, Is.True);
            CollectionAssert.AreEqual(bytes.Value,
                CanonicalSpatialSaveSerializer.Serialize(parsed.Value, f.Profile.Canonical).Value);
            Assert.That(CanonicalSpatialSaveSerializer.DeclaredFieldsMatchSerializableFields(), Is.True);
        }

        [TestCase("missing")]
        [TestCase("0")]
        [TestCase("3")]
        [TestCase("-1")]
        [TestCase("null")]
        [TestCase("\"Active\"")]
        [TestCase("true")]
        [TestCase("1.0")]
        [TestCase("2147483648")]
        [TestCase("duplicate")]
        [TestCase("case")]
        [TestCase("order")]
        public void InvalidSerializedActivationCannotBecomeActive(string mutation)
        {
            Fixture f = OneFloor();
            string original = Encoding.UTF8.GetString(f.Session.GetCurrentBytes());
            string text = mutation == "missing" ? original.Replace(",\"ActivationState\":1", "") :
                mutation == "duplicate" ? original.Replace("\"ActivationState\":1", "\"ActivationState\":1,\"ActivationState\":1") :
                mutation == "case" ? original.Replace("\"ActivationState\":1", "\"activationState\":1") :
                mutation == "order" ? original.Replace("\"FloorIndex\":0,\"ActivationState\":1", "\"ActivationState\":1,\"FloorIndex\":0") :
                original.Replace("\"ActivationState\":1", "\"ActivationState\":" + mutation);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                Encoding.UTF8.GetBytes(text), f.Context).IsValid, Is.False);
            Assert.That(f.State.Floors[0].ActivationState, Is.EqualTo(FloorActivationState.Active));
        }

        [TestCase(0)] [TestCase(3)] [TestCase(-1)]
        public void DefaultAndUnknownRuntimeActivationFailValidationAndMana(int value)
        {
            Fixture f = OneFloor();
            f.State.Floors[0].ActivationState = (FloorActivationState)value;
            Assert.That(CanonicalSpatialSaveContracts.Validate(f.State, f.Profile.Canonical.Spatial).Issues,
                Does.Contain(CanonicalSpatialSaveValidationIssue.InvalidFloorActivationState));
            Assert.That(CanonicalActiveFloorResolver.TryResolve(f.Runtime, f.Profile.Canonical.Spatial,
                out int count), Is.False);
            Assert.That(count, Is.Zero);
        }

        [Test]
        public void InactiveFirstFloorAndInactiveRunProjectionFailWithoutRepair()
        {
            Fixture f = OneFloor();
            f.State.Floors[0].ActivationState = FloorActivationState.Inactive;
            string before = JsonUtility.ToJson(f.State);
            Assert.That(CanonicalSpatialSaveContracts.Validate(f.State, f.Profile.Canonical.Spatial).Issues,
                Does.Contain(CanonicalSpatialSaveValidationIssue.InactiveFirstFloor));
            Assert.That(CanonicalMvpRouteProjection.InspectWithProductionContent(f.Runtime, f.Production).AuthorityState,
                Is.EqualTo(CanonicalMvpRuntimeAuthorityState.ContradictoryCanonical));
            Assert.That(JsonUtility.ToJson(f.State), Is.EqualTo(before));
        }

        [TestCase("gap")]
        [TestCase("missing-first")]
        [TestCase("missing-middle")]
        public void ActiveDeeperFloorsRequireEveryShallowerFloorActive(string shape)
        {
            Fixture f = OneFloor();
            SavedSpatialFloor first = f.State.Floors[0];
            SavedSpatialFloor second = CloneFloor(first, 1, FloorActivationState.Inactive);
            SavedSpatialFloor third = CloneFloor(first, 2, FloorActivationState.Active);
            var state = Join(f, shape == "gap" ? new[] { first, second, third } :
                shape == "missing-first" ? new[] { third } : new[] { first, third });
            string before = JsonUtility.ToJson(state);
            Assert.That(CanonicalSpatialSaveContracts.Validate(state, f.Profile.Canonical.Spatial).Issues,
                Does.Contain(CanonicalSpatialSaveValidationIssue.NonContiguousActiveFloors));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
        }

        [Test]
        public void ActiveAndInactiveFloorsRoundTripAndBothManaPathsUseExactlyOneContribution()
        {
            Fixture f = OneFloor();
            PassiveManaRateSummary baseline = Online(f).ResolveRate(f.Runtime, f.Configuration);
            SavedSpatialFloor second = CloneFloor(f.State.Floors[0], 1, FloorActivationState.Inactive);
            var state = Join(f, f.State.Floors[0], second);
            byte[] bytes = CanonicalSpatialSaveSerializer.Serialize(state, f.Profile.Canonical).Value;
            Assert.That(bytes, Is.Not.Null);
            var restored = CanonicalSpatialSaveSerializer.Parse(bytes, f.Profile.Canonical);
            Assert.That(restored.IsValid, Is.True);
            Assert.That(restored.Value.Floors[1].ActivationState, Is.EqualTo(FloorActivationState.Inactive));
            CollectionAssert.AreEqual(bytes, CanonicalSpatialSaveSerializer.Serialize(restored.Value, f.Profile.Canonical).Value);
            SetRuntimeState(f.Runtime, restored.Value);
            Assert.That(CanonicalActiveFloorResolver.TryResolve(f.Runtime, f.Profile.Canonical.Spatial, out int count), Is.True);
            Assert.That(count, Is.EqualTo(1));
            f.Runtime.structureRuntime.ManaReserve = 0;
            f.Runtime.lastSavedUtcUnix = 1000;
            var online = Online(f);
            var rate = online.ResolveRate(f.Runtime, f.Configuration);
            Assert.That(rate.RuleResolved, Is.True);
            Assert.That(rate.ActiveFloorContributionManaPerHour, Is.EqualTo(baseline.ActiveFloorContributionManaPerHour));
            var tick = online.ApplyTick(f.Runtime, f.Configuration, 1);
            Assert.That(tick.PotentialMana, Is.EqualTo(baseline.ManaPerHour / 3600d * online.TickSeconds));
            var offline = new CanonicalOfflinePassiveManaService(PhaseFourTestSupport.PassiveMana(f.Profile.Canonical), online)
                .Resolve(f.Runtime, f.Configuration, 4600);
            Assert.That(offline.ApplicableOnlineManaPerHour, Is.EqualTo(baseline.ManaPerHour));
            Assert.That(offline.ActualAwardedMana, Is.EqualTo(baseline.ManaPerHour * offline.EffectiveOfflineEfficiency).Within(1e-10));
        }

        [Test]
        public void ActivationValidationDoesNotImposeAnUnrelatedFiveFloorSerializerCeiling()
        {
            Fixture f = OneFloor();
            var state = Join(f, Enumerable.Range(0, 6)
                .Select(index => index == 0 ? f.State.Floors[0] : CloneFloor(f.State.Floors[0], index, FloorActivationState.Active)).ToArray());
            Assert.That(CanonicalSpatialSaveContracts.Validate(state, f.Profile.Canonical.Spatial).IsValid, Is.True);
        }

        [Test]
        public void FrozenTenRetainsExactShapeAndRejectsSmuggledActivation()
        {
            Fixture f = OneFloor();
            byte[] ten = Encoding.UTF8.GetBytes(PhaseFourTestSupport.FrozenTen(f.Session.GetCurrentBytes()));
            var frozen = DetachedCompleteSaveContract.ParseValidateFrozenSchemaTenAndRoundTrip(ten, f.Profile.Canonical);
            Assert.That(frozen.IsValid, Is.True);
            CollectionAssert.AreEqual(ten, frozen.GetBytes());
            Assert.That((int)frozen.State.Floors[0].ActivationState, Is.Zero);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(ten, f.Context).IsValid, Is.False);
            byte[] smuggled = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(f.Session.GetCurrentBytes())
                .Replace("\"schemaVersion\":12", "\"schemaVersion\":10"));
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaTenAndRoundTrip(smuggled, f.Profile.Canonical).IsValid, Is.False);
            Assert.That(SchemaTenToElevenUpgrade.TryPrepare(smuggled, f.Profile.Canonical, out _), Is.False);
        }

        [Test]
        public void TenUpgradeIsLosslessDeterministicAndRejectsElevenSource()
        {
            Fixture f = OneFloor("\"unknownPrimary\":{\"n\":1.00,\"v\":[true,null]}", "\"unknownRoot\":[null,{\"x\":2e0}]");
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId, f.State.Floors[0].Layout.Rooms[0].RoomInstanceId)));
            f.Runtime.completedResearch = new CompletedResearchState { ProjectIds = new[] { BasicBranchingResearchAuthority.ResearchId } };
            var preview = OptionalBranchStructuralEditService.PreviewConstruction(f.State,
                new OptionalBranchConstructionRequest { FloorInstanceId = f.State.Floors[0].FloorInstanceId,
                    OriginNodeId = f.State.Floors[0].Layout.Nodes.Single(n => n.Kind == FloorRouteNodeKind.Room).NodeId,
                    OriginConnectionPointId = "east", CorridorLength = 2 }, f.Runtime.completedResearch,
                f.BranchingResearch, f.Production, f.Configuration, f.Profile.Canonical);
            Assert.That(preview.IsValid, Is.True);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.ConstructBranch(preview)));
            FloorRouteEdge edge = f.State.Floors[0].Layout.Edges.Single(e => e.Classification == RouteClassification.Optional);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.PlaceCorridor(MvpDungeonPlacementIds.TrapCategoryId,
                MvpDungeonPlacementIds.SpikeTrapOptionId, edge.FloorId, edge.OptionalBranchId, edge.Footprint.OccupiedTiles[0])));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.PlaceCorridor(MvpDungeonPlacementIds.LootNodeCategoryId,
                MvpDungeonPlacementIds.HiddenCacheOptionId, edge.FloorId, edge.OptionalBranchId, edge.Footprint.OccupiedTiles[1])));
            f.Accept(PhaseFiveBBranchIntegrationTests.Run(f));
            string assigned = f.State.Floors[0].RoomContents.Assignments.Single().AssignmentId;
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(assigned)));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.TrapCategoryId,
                MvpDungeonPlacementIds.SpikeTrapOptionId, f.State.Floors[0].Layout.Rooms[0].RoomInstanceId)));
            Assert.That(BranchTopologyFingerprint.TryCompute(f.State, f.State.Floors[0].FloorInstanceId,
                edge.OptionalBranchId, out string fingerprint), Is.True);
            var knowledge = new SharedBranchKnowledgeAuthority { Records = new[] { new BranchKnowledgeRecord {
                FloorInstanceId = f.State.Floors[0].FloorInstanceId, OptionalBranchId = edge.OptionalBranchId,
                EdgeId = edge.EdgeId, TopologyFingerprint = fingerprint, TopologyKnown = true,
                ConfidenceKnown = true, Confidence = 0.75, HasLastConfirmedRun = true, LastConfirmedRunId = "run-test-1" } } };
            var captured = DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile);
            var update = f.Session.PrepareLiveReplacement(captured, f.State,
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context).Investment,
                branchKnowledge: knowledge);
            Assert.That(update.IsSuccess, Is.True, update.Reason);
            byte[] ten = Encoding.UTF8.GetBytes(PhaseFourTestSupport.FrozenTen(update.Update.GetBytes()));
            byte[] before = (byte[])ten.Clone();
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaTenAndRoundTrip(ten,
                f.Profile.Canonical).IsValid, Is.True, "Synthetic frozen schema 10 source must remain valid.");
            Assert.That(SchemaTenToElevenUpgrade.TryPrepare(ten, f.Profile.Canonical, out byte[] first), Is.True);
            Assert.That(SchemaTenToElevenUpgrade.TryPrepare(ten, f.Profile.Canonical, out byte[] again), Is.True);
            CollectionAssert.AreEqual(first, again);
            CollectionAssert.AreEqual(before, ten);
            Assert.That(PhaseFourTestSupport.FrozenTen(first), Is.EqualTo(Encoding.UTF8.GetString(ten)));
            var current = DetachedCompleteSaveContract.ParseValidateFrozenSchemaElevenAndRoundTrip(first, f.Profile.Canonical);
            Assert.That(current.IsValid, Is.True);
            Assert.That(current.State.Floors.Length, Is.EqualTo(f.State.Floors.Length));
            Assert.That(current.State.Floors.All(floor => floor.ActivationState == FloorActivationState.Active), Is.True);
            Assert.That(current.BranchKnowledge.Records.Length, Is.EqualTo(1));
            Assert.That(current.CorridorContent.Assignments.Length, Is.EqualTo(2));
            Assert.That(current.State.LifecycleAndOwnership.ReturnedContents.Length, Is.EqualTo(1));
            Assert.That(Encoding.UTF8.GetString(first), Does.Contain("\"runHistory\":"));
            Assert.That(current.Investment.Any(record => record.ConstructionMana > 0), Is.True);
            Assert.That(SchemaTenToElevenUpgrade.TryPrepare(first, f.Profile.Canonical, out byte[] rejected), Is.False);
            Assert.That(rejected, Is.Null);
        }

        [TestCase("write")]
        [TestCase("replace")]
        [TestCase("readback")]
        public void CurrentTargetWriteFailuresPublishNoActivationOrRuntimeState(string failure)
        {
            Fixture f = OneFloor();
            byte[] before = f.Session.GetCurrentBytes();
            string runtime = JsonUtility.ToJson(f.Runtime);
            var activation = f.State.Floors.Select(floor => floor.ActivationState).ToArray();
            var fs = f.FileSystem;
            if (failure == "readback")
            {
                Fixture baseline = OneFloor();
                baseline.Accept(baseline.Execute(DetachedCanonicalMutationRequest.Place(
                    MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId)));
                var operations = baseline.FileSystem.Operations.ToArray();
                int replacement = Array.FindLastIndex(operations, op => op.Type == Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace);
                int readNumber = operations.Take(replacement).Count(op => op.Type == Gd66DetachedSpatialMigrationTransactionTests.OperationType.Read &&
                    op.Paths[0] == baseline.ActivePath) + 1;
                // Counts for a targeted failure start when enabled, after the already completed room write.
                int existingReads = f.FileSystem.Operations.Count(op => op.Type == Gd66DetachedSpatialMigrationTransactionTests.OperationType.Read && op.Paths[0] == f.ActivePath);
                fs.EnableTargetedFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Read,
                    paths => paths[0] == f.ActivePath, readNumber - existingReads, false);
            }
            else fs.EnableTargetedFailure(failure == "write" ? Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write :
                Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, paths => true, 1, false);
            var result = f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId));
            fs.DisableFailure();
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Session, Is.Null); Assert.That(result.RuntimeProjection, Is.Null);
            CollectionAssert.AreEqual(before, fs.ReadAllBytes(f.ActivePath));
            Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(runtime));
            CollectionAssert.AreEqual(activation, f.State.Floors.Select(floor => floor.ActivationState));
        }

        [Test]
        public void StaleSessionPublishesNoActivationOrRuntimeState()
        {
            Fixture f = OneFloor();
            var oldSession = f.Session; var oldState = f.State; var oldRuntime = f.Runtime;
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId)));
            byte[] before = f.Session.GetCurrentBytes();
            string runtime = JsonUtility.ToJson(oldRuntime);
            var result = f.Authority.Execute(f.ActivePath, f.FileSystem, oldSession, oldState, oldRuntime,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Session, Is.Null); Assert.That(result.RuntimeProjection, Is.Null);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(JsonUtility.ToJson(oldRuntime), Is.EqualTo(runtime));
            Assert.That(oldState.Floors[0].ActivationState, Is.EqualTo(FloorActivationState.Active));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(3)]
        public void MigrationAddsActiveToEveryFloorWithoutCreatingOrReordering(int count)
        {
            Fixture f = OneFloor();
            var state = Join(f, Enumerable.Range(0, count).Select(index => index == 0 ? f.State.Floors[0] :
                CloneFloor(f.State.Floors[0], index, FloorActivationState.Active)).ToArray());
            string spatial = Encoding.UTF8.GetString(CanonicalSpatialSaveSerializer.Serialize(state, f.Profile.Canonical).Value)
                .Replace(",\"ActivationState\":1", "");
            var nodes = new SpatialIssueCollector(f.Profile.Canonical.Serialized.MaximumDiagnostics);
            Assert.That(ContractJson.TryParse(Encoding.UTF8.GetBytes(spatial), f.Profile.Canonical.Serialized, nodes, out var root), Is.True);
            var writer = new ContractJsonWriter(f.Profile.Canonical.Serialized);
            writer.Token("{\"schema\":\"save_root\",\"schemaVersion\":10,\"primary\":{\"canonicalSpatialAuthority\":");
            DetachedCompleteSaveContract.WriteCanonicalNode(writer, root.Fields[0].Value);
            writer.Token(",\"spatialFloors\":"); DetachedCompleteSaveContract.WriteCanonicalNode(writer, root.Fields[1].Value);
            writer.Token(",\"structuralLifecycleAndOwnership\":"); DetachedCompleteSaveContract.WriteCanonicalNode(writer, root.Fields[2].Value);
            writer.Token(",\"structuralInvestment\":"); StructuralInvestment.Write(writer, StructuralInvestment.Zero(state));
            writer.Token(",\"corridorContent\":{\"Assignments\":[]},\"sharedBranchKnowledge\":{\"Records\":[]}}}");
            byte[] ten = writer.Finish();
            Assert.That(SchemaTenToElevenUpgrade.TryPrepare(ten, f.Profile.Canonical, out byte[] eleven), Is.True);
            var result = DetachedCompleteSaveContract.ParseValidateFrozenSchemaElevenAndRoundTrip(eleven, f.Profile.Canonical);
            Assert.That(result.IsValid, Is.True);
            CollectionAssert.AreEqual(state.Floors.Select(floor => floor.FloorInstanceId), result.State.Floors.Select(floor => floor.FloorInstanceId));
            Assert.That(result.State.Floors.Length, Is.EqualTo(count));
            Assert.That(result.State.Floors.All(floor => floor.ActivationState == FloorActivationState.Active), Is.True);
            Assert.That(PhaseFourTestSupport.FrozenTen(eleven), Is.EqualTo(Encoding.UTF8.GetString(ten)));
        }

        [Test]
        public void MigratedOneFloorKeepsTheExistingPassiveRate()
        {
            Fixture f = OneFloor();
            double rate = Online(f).ResolveRate(f.Runtime, f.Configuration).ManaPerHour;
            byte[] ten = Encoding.UTF8.GetBytes(PhaseFourTestSupport.FrozenTen(f.Session.GetCurrentBytes()));
            Assert.That(SchemaTenToElevenUpgrade.TryPrepare(ten, f.Profile.Canonical, out byte[] eleven), Is.True);
            var reopened = DetachedCompleteSaveContract.ParseValidateFrozenSchemaElevenAndRoundTrip(eleven, f.Profile.Canonical);
            SetRuntimeState(f.Runtime, reopened.State);
            Assert.That(Online(f).ResolveRate(f.Runtime, f.Configuration).ManaPerHour, Is.EqualTo(rate));
        }

        [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)]
        public void FrozenChainContinuesThroughEachHistoricalStepToTwelve(int schema)
        {
            Fixture f = OneFloor();
            string text = PhaseFourTestSupport.FrozenTen(f.Session.GetCurrentBytes());
            if (schema < 10) text = RemoveTail(text, "corridorContent");
            if (schema < 9) text = RemoveTail(text, "structuralInvestment");
            if (schema < 8) text = RemoveTail(text, "structuralLifecycleAndOwnership");
            byte[] source = Encoding.UTF8.GetBytes(text.Replace("\"schemaVersion\":10", "\"schemaVersion\":" + schema));
            byte[] current = source;
            if (schema == 7) Assert.That(SchemaSevenToEightUpgrade.TryPrepare(current, f.Profile.Canonical, out current), Is.True);
            if (schema <= 8) Assert.That(SchemaEightToNineUpgrade.TryPrepare(current, f.Profile.Canonical, out current), Is.True);
            if (schema <= 9) Assert.That(SchemaNineToTenUpgrade.TryPrepare(current, f.Profile.Canonical, out current), Is.True);
            Assert.That(SchemaTenToElevenUpgrade.TryPrepare(current, f.Profile.Canonical, out current), Is.True);
            Assert.That(SchemaElevenToTwelveUpgrade.TryPrepare(current, f.Profile.Canonical, out current), Is.True);
            var result = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(current, f.Context);
            Assert.That(result.CurrentTargetValidated, Is.True);
            Assert.That(result.State.Floors.Single().ActivationState, Is.EqualTo(FloorActivationState.Active));
            Assert.That(result.State.Floors.Single().FloorInstanceId, Is.EqualTo(f.State.Floors.Single().FloorInstanceId));
            CollectionAssert.AreEqual(source, Encoding.UTF8.GetBytes(text.Replace("\"schemaVersion\":10", "\"schemaVersion\":" + schema)));
        }

        [Test]
        public void FrozenTenWithMissingShallowerFloorIsRejectedByUpgradeWithoutRepair()
        {
            Fixture f = OneFloor();
            byte[] source = Encoding.UTF8.GetBytes(PhaseFourTestSupport.FrozenTen(f.Session.GetCurrentBytes())
                .Replace("\"FloorIndex\":0", "\"FloorIndex\":1"));
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaTenAndRoundTrip(source, f.Profile.Canonical).IsValid, Is.True);
            Assert.That(SchemaTenToElevenUpgrade.TryPrepare(source, f.Profile.Canonical, out byte[] candidate), Is.False);
            Assert.That(candidate, Is.Null);
            Assert.That(Encoding.UTF8.GetString(source), Does.Contain("\"FloorIndex\":1"));
        }

        private static string RemoveTail(string text, string name)
        {
            int start = text.IndexOf(",\"" + name + "\":", StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0));
            return text.Remove(start, text.Length - 2 - start);
        }

        private static Fixture OneFloor(string primary = null, string root = null)
        {
            Fixture f = Fixture.Create(primary, root);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.BasicRoomOptionId)));
            return f;
        }
        private static CanonicalPassiveManaService Online(Fixture f) => new CanonicalPassiveManaService(
            PhaseFourTestSupport.PassiveMana(f.Profile.Canonical), f.Economy, new FormulaEngine(), f.Profile.Canonical.Spatial, 10);
        private static SavedSpatialFloor CloneFloor(SavedSpatialFloor source, int index, FloorActivationState activation)
        {
            string id = "test.activation.floor." + index;
            var floor = JsonUtility.FromJson<SavedSpatialFloor>(JsonUtility.ToJson(source).Replace(source.FloorInstanceId, id));
            floor.FloorDefinitionId = "test.activation.definition." + index;
            floor.FloorIndex = index; floor.ActivationState = activation;
            return floor;
        }
        private static DetachedCanonicalSpatialSaveState Join(Fixture f, params SavedSpatialFloor[] floors) =>
            new DetachedCanonicalSpatialSaveState { Authority = f.State.Authority, Floors = floors,
                LifecycleAndOwnership = NativeStructuralIdentity.CreateInitialLifecycle(floors) };
        private static void SetRuntimeState(SaveData runtime, DetachedCanonicalSpatialSaveState state)
        { runtime.canonicalSpatialAuthority = state.Authority; runtime.spatialFloors = state.Floors;
          runtime.validatedCanonicalSpatialState = state; }
    }
}
#endif
