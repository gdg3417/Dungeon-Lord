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
        public void SameCategoryContentsAppendDeterministicallyAndDuplicateIsByteExactNoOp()
        {
            Fixture fixture = Create();
            DetachedCanonicalMutationResult room = fixture.Prepare(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));
            string roomId = room.State.Floors[0].Layout.Rooms[0].RoomInstanceId;
            DetachedCanonicalMutationResult skeleton = DetachedCanonicalSpatialMutation.Prepare(room.State,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId, roomId), fixture.Production,
                fixture.Compatibility, fixture.Configuration, fixture.Profile.Canonical);
            RoomContentAssignment firstBefore = skeleton.State.Floors[0].RoomContents.Assignments.Single();
            string firstId = firstBefore.AssignmentId;
            long firstSequence = firstBefore.Sequence;
            DetachedCanonicalMutationResult goblin = DetachedCanonicalSpatialMutation.Prepare(skeleton.State,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.GoblinOptionId, roomId), fixture.Production,
                fixture.Compatibility, fixture.Configuration, fixture.Profile.Canonical);

            Assert.That(skeleton.IsSuccess, Is.True, skeleton.Reason);
            Assert.That(goblin.IsSuccess, Is.True, goblin.Reason);
            RoomContentAssignment[] monsters = goblin.State.Floors[0].RoomContents.Assignments
                .Where(value => value.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId).ToArray();
            Assert.That(monsters, Has.Length.EqualTo(2));
            CollectionAssert.AreEqual(new[]
            {
                MvpDungeonPlacementIds.SkeletonOptionId,
                MvpDungeonPlacementIds.GoblinOptionId
            }, monsters.Select(value => value.OptionId).ToArray());
            CollectionAssert.AreEqual(new long[] { 0, 1 }, monsters.Select(value => value.Sequence).ToArray());
            Assert.That(monsters.Select(value => value.AssignmentId).Distinct().Count(), Is.EqualTo(2));
            Assert.That(monsters[0].AssignmentId, Does.EndWith(".content.monster.0000"));
            Assert.That(monsters[1].AssignmentId, Does.EndWith(".content.monster.0001"));
            Assert.That(monsters[0].AssignmentId, Is.EqualTo(firstId));
            Assert.That(monsters[0].Sequence, Is.EqualTo(firstSequence));
            Assert.That(goblin.State.Floors[0].RoomContents.NextSequence, Is.EqualTo(2));
            Assert.That(CanonicalRoomCapacityResolver.TryResolve(fixture.Production,
                goblin.State.Floors[0].Layout.Rooms[0].RoomDefinitionId,
                out MvpRoomSlotCapacity capacity, out string capacityReason), Is.True, capacityReason);
            Assert.That(monsters.Length, Is.EqualTo(capacity.MonsterCapacity));
            byte[] beforeDuplicate = CanonicalSpatialSaveSerializer.Serialize(
                goblin.State, fixture.Profile.Canonical).Value;

            DetachedCanonicalMutationResult duplicate = DetachedCanonicalSpatialMutation.Prepare(goblin.State,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId, roomId), fixture.Production,
                fixture.Compatibility, fixture.Configuration, fixture.Profile.Canonical);

            Assert.That(duplicate.IsNoOp, Is.True);
            Assert.That(duplicate.Reason, Is.EqualTo(DetachedCanonicalSpatialMutation.NoOpReason));
            Assert.That(CanonicalSpatialSaveSerializer.Serialize(goblin.State,
                fixture.Profile.Canonical).Value, Is.EqualTo(beforeDuplicate));
        }

        [Test]
        public void ThirdUniqueTrapIsRejectedAtProductionCapacityWithoutChangingBytes()
        {
            Fixture fixture = Create();
            DetachedCanonicalMutationResult room = fixture.Prepare(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));
            string roomId = room.State.Floors[0].Layout.Rooms[0].RoomInstanceId;
            DetachedCanonicalMutationResult first = DetachedCanonicalSpatialMutation.Prepare(room.State,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.TrapCategoryId,
                    MvpDungeonPlacementIds.SpikeTrapOptionId, roomId), fixture.Production,
                fixture.Compatibility, fixture.Configuration, fixture.Profile.Canonical);
            DetachedCanonicalMutationResult second = DetachedCanonicalSpatialMutation.Prepare(first.State,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.TrapCategoryId,
                    MvpDungeonPlacementIds.SnareTrapOptionId, roomId), fixture.Production,
                fixture.Compatibility, fixture.Configuration, fixture.Profile.Canonical);
            byte[] beforeThird = CanonicalSpatialSaveSerializer.Serialize(
                second.State, fixture.Profile.Canonical).Value;

            DetachedCanonicalMutationResult third = DetachedCanonicalSpatialMutation.Prepare(second.State,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.TrapCategoryId,
                    MvpDungeonPlacementIds.ChillingSigilOptionId, roomId), fixture.Production,
                fixture.Compatibility, fixture.Configuration, fixture.Profile.Canonical);

            Assert.That(first.IsSuccess, Is.True, first.Reason);
            Assert.That(second.IsSuccess, Is.True, second.Reason);
            Assert.That(third.IsSuccess, Is.False);
            Assert.That(third.Reason, Is.EqualTo(DetachedSpatialMigrationPreparer.CapacityReason));
            RoomContentAssignment[] traps = second.State.Floors[0].RoomContents.Assignments
                .Where(value => value.CategoryId == MvpDungeonPlacementIds.TrapCategoryId).ToArray();
            Assert.That(traps, Has.Length.EqualTo(2));
            CollectionAssert.AreEqual(new[]
            {
                MvpDungeonPlacementIds.SpikeTrapOptionId,
                MvpDungeonPlacementIds.SnareTrapOptionId
            }, traps.Select(value => value.OptionId).ToArray());
            Assert.That(CanonicalSpatialSaveSerializer.Serialize(second.State,
                fixture.Profile.Canonical).Value, Is.EqualTo(beforeThird));
        }

        [Test]
        public void WriteAuthorityPersistsTwoMonstersAndRejectsDuplicateWithoutTouchingDisk()
        {
            Fixture fixture = Create();
            DetachedCanonicalWriteResult room = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));
            fixture.Accept(room);
            string roomId = fixture.State.Floors[0].Layout.Rooms[0].RoomInstanceId;
            DetachedCanonicalWriteResult skeleton = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId, roomId));
            fixture.Accept(skeleton);
            RoomContentAssignment persistedFirst = fixture.State.Floors[0].RoomContents.Assignments.Single();
            string firstId = persistedFirst.AssignmentId;
            long firstSequence = persistedFirst.Sequence;

            DetachedCanonicalWriteResult goblin = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.GoblinOptionId, roomId));
            fixture.Accept(goblin);

            MvpOrderedRouteRoom projected = Route(fixture.State, fixture).Single();
            Assert.That(projected.Capacity.MonsterCapacity, Is.EqualTo(2));
            CollectionAssert.AreEqual(new[]
            {
                MvpDungeonPlacementIds.SkeletonOptionId,
                MvpDungeonPlacementIds.GoblinOptionId
            }, projected.AssignedMonsterOptionIds);
            RoomContentAssignment[] persisted = fixture.State.Floors[0].RoomContents.Assignments;
            Assert.That(persisted, Has.Length.EqualTo(2));
            Assert.That(persisted[0].AssignmentId, Is.EqualTo(firstId));
            Assert.That(persisted[0].Sequence, Is.EqualTo(firstSequence));
            CollectionAssert.AreEqual(new long[] { 0, 1 }, persisted.Select(value => value.Sequence).ToArray());
            Assert.That(persisted.Select(value => value.AssignmentId).Distinct().Count(), Is.EqualTo(2));
            byte[] committed = fixture.FileSystem.ReadAllBytes(fixture.ActivePath);
            DetachedCanonicalSaveSession committedSession = fixture.Session;

            DetachedCanonicalWriteResult duplicate = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId, roomId));

            Assert.That(duplicate.IsNoOp, Is.True);
            Assert.That(duplicate.Reason, Is.EqualTo(DetachedCanonicalSpatialMutation.NoOpReason));
            Assert.That(duplicate.Session, Is.Null);
            Assert.That(fixture.Session, Is.SameAs(committedSession));
            Assert.That(fixture.FileSystem.ReadAllBytes(fixture.ActivePath), Is.EqualTo(committed));
        }

        [Test]
        public void FirstWritesRejectBogusRoomTargetWithoutMutation()
        {
            Fixture fixture = Create();
            byte[] before = CanonicalSpatialSaveSerializer.Serialize(fixture.State,
                fixture.Profile.Canonical).Value;

            DetachedCanonicalMutationResult room = fixture.Prepare(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId, "missing.room"));
            DetachedCanonicalMutationResult content = fixture.Prepare(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId, "missing.room"));

            Assert.That(room.Reason,
                Is.EqualTo(DetachedCanonicalSpatialMutation.ValidationFailedReason));
            Assert.That(content.Reason,
                Is.EqualTo(DetachedCanonicalSpatialMutation.ValidationFailedReason));
            Assert.That(CanonicalSpatialSaveSerializer.Serialize(fixture.State,
                fixture.Profile.Canonical).Value, Is.EqualTo(before));
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

        [TestCase(0)]
        [TestCase(1)]
        public void LiveCanonicalTargetUsesSelectedRoomAuthorityNotEconomicSlot(int selectedRoom)
        {
            const string members = "\"mvpRoomSlotAssignments\":{\"Rooms\":[" +
                "{\"FloorIndex\":0,\"RoomIndex\":0,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[],\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}," +
                "{\"FloorIndex\":0,\"RoomIndex\":1,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[],\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}],\"NextRevision\":3}";
            Gd66DetachedSpatialMigrationTransactionTests.SemanticFixtureExecution run =
                Gd66DetachedSpatialMigrationTransactionTests.RunPopulatedSemanticFixture(
                    "writer-live-target-" + selectedRoom, 6, members);
            var save = new SaveData { mvpSelectedRoomSlotIndex = selectedRoom,
                canonicalSpatialAuthority = run.State.Authority, spatialFloors = run.State.Floors,
                validatedCanonicalSpatialState = run.State };
            CanonicalMvpRouteProjectionResult projection =
                CanonicalMvpRouteProjection.InspectWithProductionContent(save, run.Production);

            string target = GameRoot.ResolveCanonicalMutationTargetRoomId(save,
                LegacyGameplayConfigurationContract.Parse(run.LegacyBytes), run.Production,
                projection.Rooms);

            Assert.That(target, Is.EqualTo(run.State.Floors[0].Layout.Rooms[selectedRoom].RoomInstanceId));
        }

        [Test]
        public void R2ReplacementCapacityIgnoresOtherRoomAssignments()
        {
            const string members = "\"mvpRoomSlotAssignments\":{\"Rooms\":[" +
                "{\"FloorIndex\":0,\"RoomIndex\":0,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[],\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}," +
                "{\"FloorIndex\":0,\"RoomIndex\":1,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[\"placement.option.monster.skeleton\"]," +
                "\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}],\"NextRevision\":3}";
            Gd66DetachedSpatialMigrationTransactionTests.SemanticFixtureExecution run =
                Gd66DetachedSpatialMigrationTransactionTests.RunPopulatedSemanticFixture(
                    "writer-r2-capacity", 6, members);
            SavedSpatialFloor floor = run.State.Floors[0];
            string target = floor.Layout.Rooms[0].RoomInstanceId;
            string other = floor.Layout.Rooms[1].RoomInstanceId;
            RoomContentAssignment template = floor.RoomContents.Assignments.Single();
            floor.RoomContents.Assignments = new[] { template,
                Assignment(other, 1), Assignment(other, 2) };
            floor.RoomContents.NextSequence = 3;
            floor.Layout.Rooms[0].RoomDefinitionId = "spatial.room.large_chamber";
            RunSimulationConfig config = LegacyGameplayConfigurationContract.Parse(run.LegacyBytes);

            DetachedCanonicalMutationResult result = DetachedCanonicalSpatialMutation.Prepare(run.State,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId, target), run.Production,
                run.Compatibility, config, run.Limits);

            Assert.That(result.Reason,
                Is.Not.EqualTo(DetachedCanonicalSpatialMutation.CapacityReductionReason));
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
        public void NativeCreationPersistsContextuallyValidatedEmptySchema7WithoutLegacySpatialMembers()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture source =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false);
            var profile = new SaveSpatialMigrationLimitsProfile(
                Gd66DetachedSpatialMigrationTransactionTests.RawLimitsForCoordinator,
                source.Limits, source.WholeLimits);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string path = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-native-" +
                Guid.NewGuid().ToString("N") + ".json"));
            var recognized = new SaveData { contentVersion = "test", createdUtcUnix = 1,
                lastSavedUtcUnix = 1 };

            NativeCanonicalSaveResult result = NativeCanonicalSaveCreator.Create(path, fileSystem,
                recognized, source.Compatibility, source.Production, source.LegacyBytes, profile);

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.Validation.State.Authority.CreationKind,
                Is.EqualTo(CanonicalSpatialCreationKind.NativeCanonical));
            Assert.That(result.Validation.State.Authority.MigrationTransactionId, Is.Null.Or.Empty);
            Assert.That(result.Validation.State.Authority.MigrationDescriptorFingerprint, Is.Null.Or.Empty);
            Assert.That(result.Validation.State.Floors, Is.Empty);
            string json = Encoding.UTF8.GetString(fileSystem.ReadAllBytes(path));
            Assert.That(json, Does.Contain("\"schemaVersion\":7"));
            Assert.That(json, Does.Not.Contain("\"mvpDungeonPlacements\""));
            Assert.That(json, Does.Not.Contain("\"mvpDungeonFloorLayout\""));
            Assert.That(json, Does.Not.Contain("\"mvpRoomSlotAssignments\""));
            Assert.That(result.Session.GetCurrentBytes(), Is.EqualTo(fileSystem.ReadAllBytes(path)));
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
        public void RecognizedStateSavePersistsExplicitNullableClearsAndReopensThemAsNull()
        {
            Fixture fixture = Create();
            fixture.Runtime.researchPending = new ResearchPendingState
                { SlotId = "slot.old", ProjectId = "research.old" };
            fixture.Runtime.researchProgress = new ResearchProgressState
            {
                SlotId = "slot.old", ProjectId = "research.old", ProgressUnits = 1d,
                RuleSourceIdUsed = "rule.old"
            };
            fixture.Runtime.lastOfflineSummary = new OfflineSummary
                { RuleResolved = true, RuleSourceIdUsed = "rule.old" };
            DetachedCanonicalWriteResult populated = fixture.Authority.SaveRecognizedState(
                fixture.ActivePath, fixture.FileSystem, fixture.Session, fixture.Runtime);
            fixture.Accept(populated);
            Assert.That(fixture.Runtime.researchPending.SlotId, Is.EqualTo("slot.old"));
            Assert.That(fixture.Runtime.researchPending.ProjectId, Is.EqualTo("research.old"));
            Assert.That(fixture.Runtime.researchProgress.SlotId, Is.EqualTo("slot.old"));
            Assert.That(fixture.Runtime.researchProgress.ProjectId, Is.EqualTo("research.old"));
            Assert.That(fixture.Runtime.researchProgress.ProgressUnits, Is.EqualTo(1d));
            Assert.That(fixture.Runtime.researchProgress.RuleSourceIdUsed, Is.EqualTo("rule.old"));
            Assert.That(fixture.Runtime.lastOfflineSummary.RuleResolved, Is.True);
            Assert.That(fixture.Runtime.lastOfflineSummary.RuleSourceIdUsed, Is.EqualTo("rule.old"));
            fixture.Runtime.researchPending = null;
            fixture.Runtime.researchProgress = null;
            fixture.Runtime.lastOfflineSummary = null;

            DetachedCanonicalWriteResult cleared = fixture.Authority.SaveRecognizedState(
                fixture.ActivePath, fixture.FileSystem, fixture.Session, fixture.Runtime);

            Assert.That(cleared.IsSuccess, Is.True, cleared.Reason);
            Assert.That(cleared.GetPersistedBytes(), Is.EqualTo(
                fixture.FileSystem.ReadAllBytes(fixture.ActivePath)));
            string json = Encoding.UTF8.GetString(cleared.GetPersistedBytes());
            Assert.That(json, Does.Contain("\"researchPending\":null"));
            Assert.That(json, Does.Contain("\"researchProgress\":null"));
            Assert.That(json, Does.Contain("\"lastOfflineSummary\":null"));
            Assert.That(json, Does.Not.Contain("research.old"));
            Assert.That(json, Does.Not.Contain("rule.old"));
            Assert.That(cleared.RuntimeProjection.researchPending, Is.Null);
            Assert.That(cleared.RuntimeProjection.researchProgress, Is.Null);
            Assert.That(cleared.RuntimeProjection.lastOfflineSummary, Is.Null);
            DetachedCanonicalSaveSessionResult reopened = DetachedCanonicalSaveSession.Open(
                cleared.GetPersistedBytes(), fixture.Context, fixture.Profile);
            Assert.That(reopened.IsSuccess, Is.True, reopened.Reason);
            DetachedCompleteSaveValidationResult revalidated =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                    reopened.Session.GetCurrentBytes(), fixture.Context);
            Assert.That(CanonicalMvpRouteProjection.TryPublishValidated(revalidated,
                fixture.Production, out SaveData republished, out string publishReason),
                Is.True, publishReason);
            Assert.That(republished.researchPending, Is.Null);
            Assert.That(republished.researchProgress, Is.Null);
            Assert.That(republished.lastOfflineSummary, Is.Null);
        }

        [Test]
        public void RecognizedOnlySaveMissingProductionFailsBeforeFilesystemMutation()
        {
            Fixture fixture = Create();
            var authority = new DetachedCanonicalWriteAuthority(null, fixture.Compatibility,
                fixture.Configuration, fixture.Context, fixture.Profile);

            DetachedCanonicalWriteResult result = authority.SaveRecognizedState(fixture.ActivePath,
                fixture.FileSystem, fixture.Session, fixture.Runtime);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(fixture.FileSystem.Operations, Is.Empty);
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

        [TestCase("rollback")]
        [TestCase("candidate")]
        public void PartialPriorEvidenceWithValidatedActiveIsRetiredAndRetrySucceeds(string kind)
        {
            Fixture fixture = Create();
            string evidence = EvidencePath(fixture, 'a', 'b', kind);
            fixture.FileSystem.Seed(evidence, new byte[] { 1, 2, 3 });

            DetachedCanonicalWriteResult result = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(fixture.FileSystem.Exists(evidence), Is.False);
            Assert.That(fixture.FileSystem.Paths.Count(path =>
                path.Contains(".canonical-write-")), Is.EqualTo(0));
        }

        [Test]
        public void ObsoleteOwnedEvidenceIsDeletedWithoutReadingItsPayload()
        {
            Fixture fixture = Create();
            string evidence = EvidencePath(fixture, 'a', 'b', "rollback");
            fixture.FileSystem.Seed(evidence, new byte[] { 1, 2, 3 });
            fixture.FileSystem.EnableTargetedFailure(
                Gd66DetachedSpatialMigrationTransactionTests.OperationType.Read,
                paths => paths.Length == 1 && paths[0] == evidence, 1, false);

            DetachedCanonicalWriteResult result = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(fixture.FileSystem.Operations.Any(operation =>
                operation.Type == Gd66DetachedSpatialMigrationTransactionTests.OperationType.Read &&
                operation.Paths.Length == 1 && operation.Paths[0] == evidence), Is.False);
            Assert.That(fixture.FileSystem.Exists(evidence), Is.False);
        }

        [Test]
        public void RollbackDeleteFailureAfterCandidateProofReturnsCandidateSuccessAndNextSaveSettlesEvidence()
        {
            Fixture fixture = Create();
            fixture.FileSystem.EnableTargetedFailure(
                Gd66DetachedSpatialMigrationTransactionTests.OperationType.Delete,
                paths => paths.Length == 1 && paths[0].EndsWith(".rollback", StringComparison.Ordinal),
                1, false);

            DetachedCanonicalWriteResult first = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));

            AssertCandidateSuccess(fixture, first);
            Assert.That(first.RuntimeProjection.validatedCanonicalSpatialState,
                Is.Not.SameAs(fixture.Runtime.validatedCanonicalSpatialState));
            Assert.That(fixture.FileSystem.Paths.Count(path => path.EndsWith(
                ".rollback", StringComparison.Ordinal)), Is.EqualTo(1));

            fixture.FileSystem.DisableFailure();
            fixture.Accept(first);
            fixture.Runtime.lastSavedUtcUnix++;
            DetachedCanonicalWriteResult second = fixture.Authority.SaveRecognizedState(
                fixture.ActivePath, fixture.FileSystem, fixture.Session, fixture.Runtime);

            AssertCandidateSuccess(fixture, second);
            Assert.That(fixture.FileSystem.Paths.Count(path =>
                path.Contains(".canonical-write-")), Is.EqualTo(0));
        }

        [Test]
        public void CleanupFlushFailureAfterCandidateProofReturnsCandidateSuccess()
        {
            Fixture fixture = Create();
            fixture.FileSystem.EnableFailure(
                Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush, 2);

            DetachedCanonicalWriteResult result = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));

            AssertCandidateSuccess(fixture, result);
            Assert.That(fixture.FileSystem.Paths.Count(path =>
                path.Contains(".canonical-write-")), Is.EqualTo(0));
        }

        [Test]
        public void PartialDurableRollbackWriteIsSettledOnRetry()
        {
            Fixture fixture = Create();
            fixture.FileSystem.EnablePartialWriteFailure(3);
            DetachedCanonicalWriteResult interrupted = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));

            DetachedCanonicalWriteResult retry = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));

            Assert.That(interrupted.IsSuccess, Is.False);
            Assert.That(retry.IsSuccess, Is.True, retry.Reason);
            Assert.That(fixture.FileSystem.Paths.Count(path =>
                path.Contains(".canonical-write-")), Is.EqualTo(0));
        }

        [Test]
        public void PriorTransitionEvidenceIsSettledBeforeDifferentNextTransition()
        {
            Fixture fixture = Create();
            string oldRollback = EvidencePath(fixture, '1', '2', "rollback");
            fixture.FileSystem.Seed(oldRollback, fixture.FileSystem.ReadAllBytes(fixture.ActivePath));

            DetachedCanonicalWriteResult result = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId));

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(fixture.FileSystem.Exists(oldRollback), Is.False);
            Assert.That(fixture.FileSystem.Paths.Count(path =>
                path.Contains(".canonical-write-")), Is.EqualTo(0));
        }

        [Test]
        public void RepeatedAtoBtoAtoBTransitionsDoNotCollideOrLeakEvidence()
        {
            Fixture fixture = Create();
            DetachedCanonicalWriteResult first = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));
            fixture.Accept(first);
            string roomId = fixture.State.Floors[0].Layout.Rooms[0].RoomInstanceId;
            DetachedCanonicalWriteResult second = fixture.Execute(
                DetachedCanonicalMutationRequest.RemoveRoom(roomId));
            fixture.Accept(second);
            DetachedCanonicalWriteResult third = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));

            Assert.That(first.IsSuccess, Is.True, first.Reason);
            Assert.That(second.IsSuccess, Is.True, second.Reason);
            Assert.That(third.IsSuccess, Is.True, third.Reason);
            Assert.That(fixture.FileSystem.Paths.Count(path =>
                path.Contains(".canonical-write-")), Is.EqualTo(0));
        }

        [Test]
        public void EvidenceEnumerationIsBoundedAndFailsRecoveryRequired()
        {
            Fixture fixture = Create();
            int maximum = fixture.Profile.Canonical.Serialized.MaximumCollectionRecords;
            for (int index = 0; index <= maximum; index++)
            {
                string first = index.ToString("x16", CultureInfo.InvariantCulture);
                string name = Path.GetFileName(fixture.ActivePath) + ".canonical-write-" + first +
                    "-0000000000000000.rollback";
                fixture.FileSystem.Seed(Path.Combine(Path.GetDirectoryName(fixture.ActivePath), name),
                    new byte[] { 1 });
            }

            DetachedCanonicalWriteResult result = fixture.Execute(
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));

            Assert.That(result.Reason,
                Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
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

        private static string EvidencePath(Fixture fixture, char first, char second, string kind) =>
            Path.Combine(Path.GetDirectoryName(fixture.ActivePath), Path.GetFileName(fixture.ActivePath) +
                ".canonical-write-" + new string(first, 16) + "-" + new string(second, 16) + "." + kind);

        private static void AssertCandidateSuccess(Fixture fixture,
            DetachedCanonicalWriteResult result)
        {
            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.Session, Is.Not.Null);
            Assert.That(result.RuntimeProjection, Is.Not.Null);
            Assert.That(result.RuntimeProjection.validatedCanonicalSpatialState,
                Is.SameAs(result.Validation.State));
            Assert.That(result.GetPersistedBytes(),
                Is.EqualTo(fixture.FileSystem.ReadAllBytes(fixture.ActivePath)));
            Assert.That(result.Session.GetCurrentBytes(), Is.EqualTo(result.GetPersistedBytes()));
        }

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

            internal void Accept(DetachedCanonicalWriteResult result)
            {
                Assert.That(result.IsSuccess, Is.True, result.Reason);
                Session = result.Session; State = result.Validation.State;
                Runtime = result.RuntimeProjection;
            }

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
