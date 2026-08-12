#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class DetachedCanonicalSaveSessionTests
    {
        [Test]
        public void ReopenAndReplaceTwicePreservesCompleteSaveEvidence()
        {
            Fixture fixture = CreateFixture();
            DetachedCanonicalSaveSessionResult opened = DetachedCanonicalSaveSession.Open(
                fixture.Bytes, fixture.Context, fixture.Profile);
            Assert.That(opened.IsSuccess, Is.True, opened.Reason);
            DetachedCanonicalSaveSessionResult first = opened.Session.PrepareReplacement(
                State(fixture.State.Authority, Array.Empty<SavedSpatialFloor>()));
            Assert.That(first.IsSuccess, Is.True, first.Reason);
            AssertEvidence(first.Update.GetBytes());
            Assert.That(first.Update.State.Floors, Is.Empty);

            DetachedCanonicalSaveSessionResult reopened = DetachedCanonicalSaveSession.Open(
                first.Update.GetBytes(), fixture.Context, fixture.Profile);
            Assert.That(reopened.IsSuccess, Is.True, reopened.Reason);
            DetachedCanonicalSaveSessionResult second = reopened.Session.PrepareReplacement(fixture.State);
            Assert.That(second.IsSuccess, Is.True, second.Reason);
            AssertEvidence(second.Update.GetBytes());
            Assert.That(second.Update.State.Floors, Has.Length.EqualTo(1));
            Assert.That(reopened.Session.PrepareReplacement(fixture.State).Update.GetBytes(),
                Is.EqualTo(second.Update.GetBytes()));
            Assert.That(opened.Session.GetCurrentBytes(), Is.EqualTo(fixture.Bytes));
        }

        [Test]
        public void InvalidAndWorkloadFailureDoNotCreateAnUpdate()
        {
            Fixture fixture = CreateFixture();
            Assert.That(DetachedCanonicalSaveSession.Open(new byte[] { 1 }, fixture.Context,
                fixture.Profile).IsSuccess, Is.False);
            Assert.That(DetachedCanonicalSaveSession.Open(fixture.Bytes, null,
                fixture.Profile).IsSuccess, Is.False);
            var constrained = new SaveSpatialMigrationLimitsProfile(fixture.Profile.Raw,
                fixture.Profile.Canonical, new DetachedWholeSaveLimits(1,
                    fixture.Profile.Whole.MaximumCopiedValueBytes,
                    fixture.Profile.Whole.MaximumUnknownMembers,
                    fixture.Profile.Whole.MaximumUnknownMemberBytes));
            DetachedCanonicalSaveSessionResult opened = DetachedCanonicalSaveSession.Open(
                fixture.Bytes, fixture.Context, constrained);
            Assert.That(opened.IsSuccess, Is.True);
            DetachedCanonicalSaveSessionResult failed = opened.Session.PrepareReplacement(fixture.State);
            Assert.That(failed.IsSuccess, Is.False);
            Assert.That(failed.Reason, Is.EqualTo(DetachedWholeSaveCandidateSerializer.WorkloadExceededReason));
            Assert.That(opened.Session.GetCurrentBytes(), Is.EqualTo(fixture.Bytes));
        }

        [Test]
        public void CurrentRecognizedStateUpdatesWhileLegacySpatialEvidenceStaysFrozen()
        {
            Fixture fixture = CreateFixture();
            DetachedCanonicalSaveSession session = DetachedCanonicalSaveSession.Open(
                fixture.Bytes, fixture.Context, fixture.Profile).Session;
            SaveData firstLive = LiveState(200, 17, 31d, "research.first", "objective.first");
            MutateLegacySpatialProjection(firstLive, "placement.option.room.narrow_hall");
            DetachedRecognizedSaveStateSnapshot firstSnapshot =
                DetachedRecognizedSaveStateSnapshot.Create(firstLive, fixture.Profile);

            DetachedCanonicalSaveSessionResult first = session.PrepareReplacement(firstSnapshot, fixture.State);

            Assert.That(first.IsSuccess, Is.True, first.Reason);
            AssertLiveState(first.Update.GetBytes(), 200, 17, 31d, "research.first", "objective.first");
            AssertFrozenLegacy(first.Update.GetBytes());
            AssertEvidence(first.Update.GetBytes());

            DetachedCanonicalSaveSession reopened = DetachedCanonicalSaveSession.Open(
                first.Update.GetBytes(), fixture.Context, fixture.Profile).Session;
            SaveData secondLive = LiveState(300, 29, 47d, "research.second", "objective.second");
            MutateLegacySpatialProjection(secondLive, "placement.option.room.narrow_hall");
            DetachedRecognizedSaveStateSnapshot secondSnapshot =
                DetachedRecognizedSaveStateSnapshot.Create(secondLive, fixture.Profile);
            DetachedCanonicalSaveSessionResult second = reopened.PrepareReplacement(secondSnapshot,
                State(fixture.State.Authority, Array.Empty<SavedSpatialFloor>()));

            Assert.That(second.IsSuccess, Is.True, second.Reason);
            AssertLiveState(second.Update.GetBytes(), 300, 29, 47d, "research.second", "objective.second");
            AssertFrozenLegacy(second.Update.GetBytes());
            AssertEvidence(second.Update.GetBytes());
            Assert.That(DetachedCanonicalSaveSession.Open(second.Update.GetBytes(), fixture.Context,
                fixture.Profile).IsSuccess, Is.True);
            Assert.That(reopened.PrepareReplacement(secondSnapshot,
                State(fixture.State.Authority, Array.Empty<SavedSpatialFloor>())).Update.GetBytes(),
                Is.EqualTo(second.Update.GetBytes()));
        }

        [Test]
        public void AbsentLegacySpatialEvidenceRemainsAbsentDespiteInitializedRuntimeFields()
        {
            Fixture fixture = CreateFixture(false);
            DetachedCanonicalSaveSession session = DetachedCanonicalSaveSession.Open(
                fixture.Bytes, fixture.Context, fixture.Profile).Session;
            SaveData live = LiveState(400, 41, 53d, "research.absent", "objective.absent");
            DetachedCanonicalSaveSessionResult result = session.PrepareReplacement(
                DetachedRecognizedSaveStateSnapshot.Create(live, fixture.Profile), fixture.State);

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            string json = Encoding.UTF8.GetString(result.Update.GetBytes());
            Assert.That(json, Does.Not.Contain("mvpDungeonPlacements"));
            Assert.That(json, Does.Not.Contain("mvpDungeonFloorLayout"));
            Assert.That(json, Does.Not.Contain("mvpRoomSlotAssignments"));
        }

        private static Fixture CreateFixture(bool includeLegacy = true)
        {
            SaveSpatialMigrationLimitsProfile profile = SaveSpatialMigrationLimitsLoader.Load(
                File.ReadAllText(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
            const string assignments = "\"mvpRoomSlotAssignments\":{\"Rooms\":[{" +
                "\"FloorIndex\":0,\"RoomIndex\":0,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[],\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}],\"NextRevision\":2}";
            const string floor = "\"mvpDungeonFloorLayout\":{\"Nodes\":[{" +
                "\"FloorIndex\":0,\"NodeIndex\":0,\"SlotId\":\"slot.0\",\"CategoryId\":\"placement.category.room\"," +
                "\"OptionId\":\"placement.option.room.basic\",\"Revision\":1}],\"NextRevision\":2}";
            const string placements = "\"mvpDungeonPlacements\":{\"Entries\":[{" +
                "\"CategoryId\":\"placement.category.room\",\"OptionId\":\"placement.option.room.basic\"," +
                "\"Revision\":1}],\"NextRevision\":2}";
            string legacy = includeLegacy ? placements + "," + floor + "," + assignments + "," : string.Empty;
            string json = "{\"rootUnknown\":{\"lexical\":1.00,\"items\":[true,null,{\"s\":\"a \\\" b \\\\ c\"}]}," +
                "\"schema\":\"save_root\",\"schemaVersion\":6,\"primary\":{" +
                "\"dungeonLayout\":{\"Slots\":[]}," + legacy +
                "\"unknownPrimary\":[1,{\"n\":1.00}]}}";
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture prepared =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false,
                    Encoding.UTF8.GetBytes(json), profile.Raw, profile.Whole, profile.Canonical);
            Assert.That(prepared.Result.IsSuccess, Is.True, prepared.Result.Reason);
            byte[] bytes = prepared.Result.Attempt.Candidate.GetBytes();
            var context = new DetachedCurrentTargetValidationContext(prepared.Compatibility,
                prepared.Production, prepared.LegacyBytes, profile.Canonical);
            DetachedCompleteSaveValidationResult validated =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, context);
            Assert.That(validated.IsValid, Is.True, validated.Reason);
            return new Fixture(bytes, context, profile, validated.State);
        }

        private static DetachedCanonicalSpatialSaveState State(CanonicalSpatialAuthorityMarker authority,
            SavedSpatialFloor[] floors) => new DetachedCanonicalSpatialSaveState
        {
            Authority = new CanonicalSpatialAuthorityMarker
            {
                CanonicalLayoutContractVersion = authority.CanonicalLayoutContractVersion,
                CreationKind = authority.CreationKind,
                MigrationTransactionId = authority.MigrationTransactionId,
                MigrationDescriptorFingerprint = authority.MigrationDescriptorFingerprint
            },
            Floors = floors
        };

        private static void AssertEvidence(byte[] bytes)
        {
            string json = Encoding.UTF8.GetString(bytes);
            Assert.That(json, Does.Contain("\"dungeonLayout\":{\"Slots\":[]}"));
            Assert.That(json, Does.Contain("\"unknownPrimary\":[1,{\"n\":1.00}]"));
            Assert.That(json, Does.Contain("\"rootUnknown\":{\"lexical\":1.00,\"items\":[true,null,{\"s\":\"a \\\" b \\\\ c\"}]}"));
        }

        private static SaveData LiveState(long saved, int runSequence, double mana,
            string researchId, string objectiveId) => new SaveData
        {
            saveVersion = 7,
            contentVersion = "current-content",
            createdUtcUnix = 100,
            lastSavedUtcUnix = saved,
            lastPausedUtcUnix = saved - 2,
            lastResumedUtcUnix = saved - 1,
            totalTicks = saved * 10,
            lastKnownAppState = "Paused",
            dungeonLayout = DungeonLayoutState.CreateEmpty(1, 1),
            structureRuntime = new StructureRuntimeState { ManaReserve = mana, Heat = 4d },
            runHistory = new RunHistoryState { NextRunSequence = runSequence },
            completedResearch = new CompletedResearchState
            {
                ProjectIds = new[] { researchId }, LastCompletedProjectId = researchId,
                LastCompletionRuleSourceId = "rule.research"
            },
            completedObjectives = new CompletedObjectiveState
            {
                ObjectiveIds = new[] { objectiveId }, LastCompletedObjectiveId = objectiveId,
                LastCompletionRuleSourceId = "rule.objective"
            },
            integrityFlags = new[] { "current" }
        };

        private static void MutateLegacySpatialProjection(SaveData save, string option)
        {
            save.mvpDungeonPlacements = new MvpDungeonPlacementState
            {
                Entries = new System.Collections.Generic.List<MvpDungeonPlacementEntry>
                { new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, option, 99) },
                NextRevision = 100
            };
            save.mvpDungeonFloorLayout = null;
            save.mvpRoomSlotAssignments = null;
        }

        private static void AssertLiveState(byte[] bytes, long saved, int runSequence, double mana,
            string researchId, string objectiveId)
        {
            SaveData save = JsonUtility.FromJson<SaveRoot>(Encoding.UTF8.GetString(bytes)).primary;
            Assert.That(save.lastSavedUtcUnix, Is.EqualTo(saved));
            Assert.That(save.runHistory.NextRunSequence, Is.EqualTo(runSequence));
            Assert.That(save.structureRuntime.ManaReserve, Is.EqualTo(mana));
            Assert.That(save.completedResearch.LastCompletedProjectId, Is.EqualTo(researchId));
            Assert.That(save.completedObjectives.LastCompletedObjectiveId, Is.EqualTo(objectiveId));
            Assert.That(save.dungeonLayout.FloorCount, Is.EqualTo(1));
            Assert.That(save.dungeonLayout.SlotsPerFloor, Is.EqualTo(1));
        }

        private static void AssertFrozenLegacy(byte[] bytes)
        {
            string json = Encoding.UTF8.GetString(bytes);
            Assert.That(json, Does.Contain("\"mvpDungeonPlacements\":{\"Entries\":[{" +
                "\"CategoryId\":\"placement.category.room\",\"OptionId\":\"placement.option.room.basic\"," +
                "\"Revision\":1}],\"NextRevision\":2}"));
            Assert.That(json, Does.Contain("\"mvpDungeonFloorLayout\":{\"Nodes\":[{"));
            Assert.That(json, Does.Contain("\"mvpRoomSlotAssignments\":{\"Rooms\":[{"));
            Assert.That(json, Does.Not.Contain("placement.option.room.narrow_hall"));
        }

        private sealed class Fixture
        {
            internal Fixture(byte[] bytes, DetachedCurrentTargetValidationContext context,
                SaveSpatialMigrationLimitsProfile profile, DetachedCanonicalSpatialSaveState state)
            { Bytes = bytes; Context = context; Profile = profile; State = state; }
            internal byte[] Bytes { get; }
            internal DetachedCurrentTargetValidationContext Context { get; }
            internal SaveSpatialMigrationLimitsProfile Profile { get; }
            internal DetachedCanonicalSpatialSaveState State { get; }
        }
    }
}
#endif
