#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class DetachedCanonicalWriteAuthorityTests
    {
        [Test]
        public void EmptyExplicitBasicCreatesDeterministicProductionStarter()
        {
            Fixture fixture = Create();
            DetachedCanonicalMutationRequest request = DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId);

            DetachedCanonicalMutationResult first = fixture.Prepare(request);
            DetachedCanonicalMutationResult second = fixture.Prepare(request);

            Assert.That(first.IsSuccess, Is.True, first.Reason);
            Assert.That(first.ApplyExplicitRoomEffect, Is.True);
            Assert.That(first.State.Floors.Length, Is.EqualTo(1));
            Assert.That(first.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(1));
            Assert.That(first.State.Floors[0].RoomContents.RoomSemantics[0].LegacyRoomOriginKind,
                Is.EqualTo(LegacyRoomOriginKind.CanonicalPlayerPlaced));
            Assert.That(CanonicalSpatialSaveSerializer.Serialize(first.State, fixture.Profile.Canonical).Value,
                Is.EqualTo(CanonicalSpatialSaveSerializer.Serialize(second.State, fixture.Profile.Canonical).Value));
        }

        [TestCase("placement.category.monster", "placement.option.monster.skeleton")]
        [TestCase("placement.category.trap", "placement.option.trap.spike")]
        [TestCase("placement.category.loot_node", "placement.option.loot_node.basic")]
        public void ContentFirstCreatesImplicitContainerWithoutRoomEffect(string category, string option)
        {
            Fixture fixture = Create();

            DetachedCanonicalMutationResult result = fixture.Prepare(
                DetachedCanonicalMutationRequest.Place(category, option));

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            SavedSpatialFloor floor = result.State.Floors[0];
            Assert.That(floor.RoomContents.RoomSemantics[0].LegacyRoomOriginKind,
                Is.EqualTo(LegacyRoomOriginKind.ImplicitCompatibilityContainer));
            Assert.That(floor.RoomContents.Assignments.Single().OptionId, Is.EqualTo(option));
            Assert.That(result.ApplyExplicitRoomEffect, Is.False);
        }

        [Test]
        public void ImplicitContainerPromotesAndRetainsContentExactlyOnce()
        {
            Fixture fixture = Create();
            DetachedCanonicalMutationResult implicitState = fixture.Prepare(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId));

            DetachedCanonicalMutationResult promoted = DetachedCanonicalSpatialMutation.Prepare(
                implicitState.State, DetachedCanonicalMutationRequest.Place(
                    MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId),
                fixture.Production, fixture.Compatibility, fixture.Configuration, fixture.Profile.Canonical);
            DetachedCanonicalMutationResult repeated = DetachedCanonicalSpatialMutation.Prepare(
                promoted.State, DetachedCanonicalMutationRequest.Place(
                    MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId),
                fixture.Production, fixture.Compatibility, fixture.Configuration, fixture.Profile.Canonical);

            Assert.That(promoted.IsSuccess, Is.True, promoted.Reason);
            Assert.That(promoted.ApplyExplicitRoomEffect, Is.True);
            Assert.That(promoted.State.Floors[0].RoomContents.Assignments.Single().OptionId,
                Is.EqualTo(MvpDungeonPlacementIds.SkeletonOptionId));
            Assert.That(repeated.IsNoOp, Is.True);
            Assert.That(repeated.ApplyExplicitRoomEffect, Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void R2ContentMutationTargetsStableRoomIdentity(int targetIndex)
        {
            const string members = "\"mvpRoomSlotAssignments\":{\"Rooms\":[" +
                "{\"FloorIndex\":0,\"RoomIndex\":0,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[],\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}," +
                "{\"FloorIndex\":0,\"RoomIndex\":1,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[],\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}],\"NextRevision\":3}";
            Gd66DetachedSpatialMigrationTransactionTests.SemanticFixtureExecution run =
                Gd66DetachedSpatialMigrationTransactionTests.RunPopulatedSemanticFixture(
                    "writer-r2-" + targetIndex, 6, members);
            string target = run.State.Floors[0].Layout.Rooms[targetIndex].RoomInstanceId;
            byte[] otherBefore = CanonicalSpatialSaveSerializer.Serialize(run.State, run.Limits).Value;
            RunSimulationConfig config = LegacyGameplayConfigurationContract.Parse(run.LegacyBytes);

            DetachedCanonicalMutationResult result = DetachedCanonicalSpatialMutation.Prepare(run.State,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId, target), run.Production,
                run.Compatibility, config, run.Limits);

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.State.Floors[0].RoomContents.Assignments.Single().RoomInstanceId,
                Is.EqualTo(target));
            string other = run.State.Floors[0].Layout.Rooms[1 - targetIndex].RoomInstanceId;
            Assert.That(result.State.Floors[0].RoomContents.Assignments.Any(value =>
                value.RoomInstanceId == other), Is.False);
            Assert.That(CanonicalSpatialSaveSerializer.Serialize(run.State, run.Limits).Value,
                Is.EqualTo(otherBefore));
        }

        [Test]
        public void WrongRoomIdentityFailsWithoutMutation()
        {
            Fixture fixture = Create();
            DetachedCanonicalMutationResult placed = fixture.Prepare(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));
            byte[] before = CanonicalSpatialSaveSerializer.Serialize(placed.State,
                fixture.Profile.Canonical).Value;

            DetachedCanonicalMutationResult result = DetachedCanonicalSpatialMutation.Prepare(placed.State,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId, "missing.room"), fixture.Production,
                fixture.Compatibility, fixture.Configuration, fixture.Profile.Canonical);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(CanonicalSpatialSaveSerializer.Serialize(placed.State,
                fixture.Profile.Canonical).Value, Is.EqualTo(before));
        }

        [Test]
        public void ProductionCapacityIsIndependentOfLegacyCapacityConfig()
        {
            Fixture fixture = Create();
            DetachedCanonicalMutationResult result = fixture.Prepare(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));
            RoomSpatialInstance room = result.State.Floors[0].Layout.Rooms[0];
            fixture.Configuration.MvpRoomSlotCapacities = Array.Empty<MvpRoomSlotCapacityConfig>();

            Assert.That(CanonicalRoomCapacityResolver.TryResolve(fixture.Production,
                room.RoomDefinitionId, out MvpRoomSlotCapacity capacity, out string reason), Is.True, reason);
            Assert.That(capacity.MonsterCapacity, Is.GreaterThan(0));
            Assert.That(capacity.TrapCapacity, Is.GreaterThan(0));
            Assert.That(capacity.LootCapacity, Is.GreaterThan(0));
        }

        [Test]
        public void MissingProductionRoomFailsClosed()
        {
            Fixture fixture = Create();
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            catalog.Rooms = (catalog.Rooms ?? Array.Empty<RoomSpatialDefinition>()).Where(value =>
                value?.RoomDefinitionId != "spatial.room.basic").ToArray();
            var missing = new ProductionSpatialContentSnapshot(fixture.Production.Manifest,
                catalog, fixture.Production.Languages);

            DetachedCanonicalMutationResult result = DetachedCanonicalSpatialMutation.Prepare(fixture.State,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId), missing, fixture.Compatibility,
                fixture.Configuration, fixture.Profile.Canonical);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo("gd66.starter_profile.invalid"));
        }

        [Test]
        public void NarrowHallAndOccupiedRemovalFailWithoutDetachedMutation()
        {
            Fixture fixture = Create();
            DetachedCanonicalMutationResult narrow = fixture.Prepare(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.NarrowHallOptionId));
            DetachedCanonicalMutationResult occupied = fixture.Prepare(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId));
            string roomId = occupied.State.Floors[0].Layout.Rooms[0].RoomInstanceId;
            DetachedCanonicalMutationResult removed = DetachedCanonicalSpatialMutation.Prepare(occupied.State,
                DetachedCanonicalMutationRequest.RemoveRoom(roomId), fixture.Production,
                fixture.Compatibility, fixture.Configuration, fixture.Profile.Canonical);

            Assert.That(narrow.IsSuccess, Is.False);
            Assert.That(narrow.Reason, Is.EqualTo(DetachedCanonicalSpatialMutation.UnsupportedRoomReason));
            Assert.That(removed.IsSuccess, Is.False);
            Assert.That(removed.Reason, Is.EqualTo(DetachedCanonicalSpatialMutation.RemovalHasContentsReason));
        }

        [Test]
        public void CapacityReducingBasicReplacementRejectsRetainedContents()
        {
            Fixture fixture = Create();
            DetachedCanonicalSpatialSaveState state = fixture.Prepare(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId)).State;
            SavedSpatialFloor floor = state.Floors[0];
            RoomSpatialInstance room = floor.Layout.Rooms[0];
            room.RoomDefinitionId = "spatial.room.large_chamber";
            floor.RoomContents.Assignments = new[]
            {
                Assignment(room.RoomInstanceId, 0), Assignment(room.RoomInstanceId, 1),
                Assignment(room.RoomInstanceId, 2)
            };
            floor.RoomContents.NextSequence = 3;

            DetachedCanonicalMutationResult result = DetachedCanonicalSpatialMutation.Prepare(state,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId), fixture.Production,
                fixture.Compatibility, fixture.Configuration, fixture.Profile.Canonical);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo(DetachedCanonicalSpatialMutation.CapacityReductionReason));
        }

        [Test]
        public void SuccessfulWritePersistsExactCompleteBytesThenPublishesRuntime()
        {
            Fixture fixture = CreateWithUnknownEvidence();
            fixture.Runtime.lastSavedUtcUnix = 777;
            fixture.Runtime.structureRuntime.ManaReserve = 123d;
            fixture.Runtime.mvpDungeonPlacements.Entries[0].OptionId =
                MvpDungeonPlacementIds.NarrowHallOptionId;

            DetachedCanonicalWriteResult result = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.ApplyExplicitRoomEffect, Is.True);
            Assert.That(result.GetPersistedBytes(), Is.EqualTo(fixture.FileSystem.ReadAllBytes(fixture.ActivePath)));
            Assert.That(result.Session.GetCurrentBytes(), Is.EqualTo(result.GetPersistedBytes()));
            string json = Encoding.UTF8.GetString(result.GetPersistedBytes());
            Assert.That(json, Does.Contain("\"unknownPrimary\":{\"n\":1.00}"));
            Assert.That(json, Does.Contain("\"unknownRoot\":[true,null]"));
            Assert.That(json, Does.Contain("\"mvpDungeonPlacements\":{\"Entries\":[{" +
                "\"CategoryId\":\"placement.category.room\"," +
                "\"OptionId\":\"placement.option.room.basic\""));
            Assert.That(json, Does.Not.Contain("\"OptionId\":\"placement.option.room.narrow_hall\""));
            Assert.That(result.RuntimeProjection.lastSavedUtcUnix, Is.EqualTo(777));
            Assert.That(result.RuntimeProjection.structureRuntime.ManaReserve, Is.EqualTo(123d));
            Assert.That(result.RuntimeProjection.validatedCanonicalSpatialState, Is.Not.Null);
            CanonicalMvpRouteProjectionResult route =
                CanonicalMvpRouteProjection.InspectWithProductionContent(
                    result.RuntimeProjection, fixture.Production);
            Assert.That(route.AuthorityState,
                Is.EqualTo(CanonicalMvpRuntimeAuthorityState.ValidatedCanonical));
            Assert.That(route.Rooms[0].Capacity.MonsterCapacity, Is.GreaterThan(0));
            Assert.That(fixture.FileSystem.Paths.Any(path =>
                path.Contains(".canonical-write-")), Is.False);
        }

        [Test]
        public void CanonicalRouteDerivesImplicitAndExplicitRoomEffectsExactlyOnce()
        {
            Fixture fixture = Create();
            DetachedCanonicalMutationResult implicitState = fixture.Prepare(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId));
            MvpOrderedRouteRoom implicitRoom = Route(implicitState.State, fixture).Single();
            MvpPlacementEffectsSummary implicitEffects = MvpPlacementEffectsResolver.ResolvePlacements(
                implicitRoom.ToOrderedPlacements(), fixture.Configuration);
            DetachedCanonicalMutationResult explicitState = DetachedCanonicalSpatialMutation.Prepare(
                implicitState.State, DetachedCanonicalMutationRequest.Place(
                    MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId,
                    implicitState.State.Floors[0].Layout.Rooms[0].RoomInstanceId), fixture.Production,
                fixture.Compatibility, fixture.Configuration, fixture.Profile.Canonical);
            MvpOrderedRouteRoom explicitRoom = Route(explicitState.State, fixture).Single();
            MvpPlacementEffectsSummary explicitEffects = MvpPlacementEffectsResolver.ResolvePlacements(
                explicitRoom.ToOrderedPlacements(), fixture.Configuration);

            Assert.That(implicitRoom.IncludeRoomPlacement, Is.False);
            Assert.That(implicitEffects.ContributingOptionIds.Count(value =>
                value == MvpDungeonPlacementIds.BasicRoomOptionId), Is.EqualTo(0));
            Assert.That(explicitRoom.IncludeRoomPlacement, Is.True);
            Assert.That(explicitEffects.ContributingOptionIds.Count(value =>
                value == MvpDungeonPlacementIds.BasicRoomOptionId), Is.EqualTo(1));
            Assert.That(explicitEffects.ContributingOptionIds.Count(value =>
                value == MvpDungeonPlacementIds.SkeletonOptionId), Is.EqualTo(1));
        }

        [Test]
        public void NoOpDoesNotWriteOrPublish()
        {
            Fixture fixture = Create();
            DetachedCanonicalMutationResult placed = fixture.Prepare(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));
            Fixture populated = fixture.Rebase(placed.State);
            byte[] before = populated.FileSystem.ReadAllBytes(populated.ActivePath);

            DetachedCanonicalWriteResult result = populated.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));

            Assert.That(result.IsNoOp, Is.True);
            Assert.That(result.Reason, Is.EqualTo(DetachedCanonicalSpatialMutation.NoOpReason));
            Assert.That(populated.FileSystem.ReadAllBytes(populated.ActivePath), Is.EqualTo(before));
            Assert.That(result.RuntimeProjection, Is.Null);
        }

        [Test]
        public void RecognizedStateOnlySaveKeepsCanonicalAndUnknownEvidence()
        {
            Fixture fixture = CreateWithUnknownEvidence();
            fixture.Runtime.lastSavedUtcUnix = 991;
            byte[] canonicalBefore = CanonicalSpatialSaveSerializer.Serialize(fixture.State,
                fixture.Profile.Canonical).Value;

            DetachedCanonicalWriteResult result = fixture.Authority.SaveRecognizedState(
                fixture.ActivePath, fixture.FileSystem, fixture.Session, fixture.Runtime);

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.RuntimeProjection.lastSavedUtcUnix, Is.EqualTo(991));
            Assert.That(CanonicalSpatialSaveSerializer.Serialize(result.Validation.State,
                fixture.Profile.Canonical).Value, Is.EqualTo(canonicalBefore));
            string json = Encoding.UTF8.GetString(result.GetPersistedBytes());
            Assert.That(json, Does.Contain("\"unknownPrimary\":{\"n\":1.00}"));
            Assert.That(json, Does.Contain("\"unknownRoot\":[true,null]"));
        }

        [Test]
        public void StaleSuppliedStateCannotOverrideSessionAuthority()
        {
            Fixture fixture = Create();
            DetachedCanonicalMutationResult stale = fixture.Prepare(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));
            byte[] before = fixture.FileSystem.ReadAllBytes(fixture.ActivePath);

            DetachedCanonicalWriteResult result = fixture.Authority.Execute(fixture.ActivePath,
                fixture.FileSystem, fixture.Session, stale.State, fixture.Runtime,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(fixture.FileSystem.ReadAllBytes(fixture.ActivePath), Is.EqualTo(before));
        }

        [Test]
        public void ProductionInspectionRejectsReplacedCanonicalReferences()
        {
            Fixture fixture = Create();
            fixture.Runtime.spatialFloors = Array.Empty<SavedSpatialFloor>();

            CanonicalMvpRouteProjectionResult result =
                CanonicalMvpRouteProjection.InspectWithProductionContent(
                    fixture.Runtime, fixture.Production);

            Assert.That(result.AuthorityState,
                Is.EqualTo(CanonicalMvpRuntimeAuthorityState.ContradictoryCanonical));
        }

        [Test]
        public void FailedStagingWriteLeavesDiskAndRuntimeUnpublished()
        {
            Fixture fixture = Create();
            byte[] before = fixture.FileSystem.ReadAllBytes(fixture.ActivePath);
            fixture.FileSystem.EnableFailure(
                Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write, 2);

            DetachedCanonicalWriteResult result = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            Assert.That(fixture.FileSystem.ReadAllBytes(fixture.ActivePath), Is.EqualTo(before));
            Assert.That(result.RuntimeProjection, Is.Null);
            Assert.That(result.Session, Is.Null);
        }

        [TestCase(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 1)]
        [TestCase(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush, 1)]
        public void FailedReplaceOrDurabilityRestoresOldDiskAndPublishesNothing(
            Gd66DetachedSpatialMigrationTransactionTests.OperationType operation, int occurrence)
        {
            Fixture fixture = Create();
            byte[] before = fixture.FileSystem.ReadAllBytes(fixture.ActivePath);
            fixture.FileSystem.EnableFailure(operation, occurrence);

            DetachedCanonicalWriteResult result = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            Assert.That(fixture.FileSystem.ReadAllBytes(fixture.ActivePath), Is.EqualTo(before));
            Assert.That(result.RuntimeProjection, Is.Null);
            Assert.That(result.Session, Is.Null);
        }

        [Test]
        public void RestoreFailureReturnsRecoveryRequiredNotOrdinaryAtomicFailure()
        {
            Fixture fixture = Create();
            fixture.FileSystem.EnableFailureSequence(
                Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush, 1,
                Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 2);

            DetachedCanonicalWriteResult result = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason,
                Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
            Assert.That(result.Reason,
                Is.Not.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            Assert.That(result.RuntimeProjection, Is.Null);
        }

        private static Fixture Create() => Fixture.Create(null);
        private static Fixture CreateWithUnknownEvidence() => Fixture.Create(
            "\"mvpDungeonPlacements\":{\"Entries\":[{\"CategoryId\":\"placement.category.room\"," +
            "\"OptionId\":\"placement.option.room.basic\",\"Revision\":1}],\"NextRevision\":2}," +
            "\"unknownPrimary\":{\"n\":1.00}", "\"unknownRoot\":[true,null]");

        private static RoomContentAssignment Assignment(string roomId, long sequence) =>
            new RoomContentAssignment { AssignmentId = roomId + ".content.monster." +
                sequence.ToString("D4", CultureInfo.InvariantCulture), RoomInstanceId = roomId,
                CategoryId = MvpDungeonPlacementIds.MonsterCategoryId,
                OptionId = MvpDungeonPlacementIds.SkeletonOptionId, Sequence = sequence };

        private static MvpOrderedRouteRoom[] Route(DetachedCanonicalSpatialSaveState state, Fixture fixture)
        {
            var save = new SaveData { canonicalSpatialAuthority = state.Authority,
                spatialFloors = state.Floors, validatedCanonicalSpatialState = state };
            return CanonicalMvpRouteProjection.InspectWithProductionContent(save,
                fixture.Production).Rooms;
        }

        private sealed class Fixture
        {
            internal ProductionSpatialContentSnapshot Production;
            internal SpatialLayoutCompatibilitySnapshot Compatibility;
            internal RunSimulationConfig Configuration;
            internal SaveSpatialMigrationLimitsProfile Profile;
            internal DetachedCurrentTargetValidationContext Context;
            internal DetachedCanonicalSaveSession Session;
            internal DetachedCanonicalSpatialSaveState State;
            internal SaveData Runtime;
            internal Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem FileSystem;
            internal string ActivePath;
            internal DetachedCanonicalWriteAuthority Authority => new DetachedCanonicalWriteAuthority(
                Production, Compatibility, Configuration, Context, Profile);

            internal static Fixture Create(string primaryUnknown, string rootUnknown = null)
            {
                string primary = primaryUnknown == null ? string.Empty : primaryUnknown;
                string root = rootUnknown == null ? string.Empty : "," + rootUnknown;
                byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                    "\"primary\":{" + primary + "}" + root + "}");
                Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture source =
                    Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, original);
                byte[] candidate = source.Result.Attempt.Candidate.GetBytes();
                var context = new DetachedCurrentTargetValidationContext(source.Compatibility,
                    source.Production, source.LegacyBytes, source.Limits);
                var profile = new SaveSpatialMigrationLimitsProfile(
                    Gd66DetachedSpatialMigrationTransactionTests.RawLimitsForCoordinator,
                    source.Limits, source.WholeLimits);
                DetachedCompleteSaveValidationResult validation =
                    DetachedCompleteSaveContract.ParseValidateAndRoundTrip(candidate, context);
                DetachedCanonicalSaveSessionResult opened =
                    DetachedCanonicalSaveSession.Open(candidate, context, profile);
                if (primaryUnknown != null && primaryUnknown.Contains("mvpDungeonPlacements"))
                {
                    var empty = new DetachedCanonicalSpatialSaveState
                    { Authority = validation.State.Authority, Floors = Array.Empty<SavedSpatialFloor>() };
                    DetachedCanonicalSaveSessionResult emptied =
                        opened.Session.PrepareSpatialOnlyReplacement(empty);
                    candidate = emptied.Update.GetBytes();
                    validation = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(candidate, context);
                    opened = DetachedCanonicalSaveSession.Open(candidate, context, profile);
                }
                CanonicalMvpRouteProjection.TryPublishValidated(validation, source.Production,
                    out SaveData runtime, out string reason);
                var fs = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
                string path = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-canonical-write-" +
                    Guid.NewGuid().ToString("N") + ".json"));
                fs.Seed(path, candidate);
                return new Fixture { Production = source.Production, Compatibility = source.Compatibility,
                    Configuration = LegacyGameplayConfigurationContract.Parse(source.LegacyBytes),
                    Profile = profile, Context = context, Session = opened.Session, State = validation.State,
                    Runtime = runtime, FileSystem = fs, ActivePath = path };
            }

            internal DetachedCanonicalMutationResult Prepare(DetachedCanonicalMutationRequest request) =>
                DetachedCanonicalSpatialMutation.Prepare(State, request, Production, Compatibility,
                    Configuration, Profile.Canonical);

            internal DetachedCanonicalWriteResult Execute(DetachedCanonicalMutationRequest request) =>
                Authority.Execute(ActivePath, FileSystem, Session, State, Runtime, request);

            internal Fixture Rebase(DetachedCanonicalSpatialSaveState state)
            {
                DetachedRecognizedSaveStateSnapshotResult snapshot =
                    DetachedRecognizedSaveStateSnapshot.Capture(Runtime, Profile);
                DetachedCanonicalSaveSessionResult update = Session.PrepareLiveReplacement(snapshot, state);
                byte[] bytes = update.Update.GetBytes();
                DetachedCanonicalSaveSessionResult opened = DetachedCanonicalSaveSession.Open(bytes, Context, Profile);
                DetachedCompleteSaveValidationResult validation =
                    DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, Context);
                CanonicalMvpRouteProjection.TryPublishValidated(validation, Production,
                    out SaveData runtime, out string ignored);
                var fs = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
                string path = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-rebase-" +
                    Guid.NewGuid().ToString("N") + ".json"));
                fs.Seed(path, bytes);
                return new Fixture { Production = Production, Compatibility = Compatibility,
                    Configuration = Configuration, Profile = Profile, Context = Context,
                    Session = opened.Session, State = validation.State, Runtime = runtime,
                    FileSystem = fs, ActivePath = path };
            }
        }
    }
}
#endif
