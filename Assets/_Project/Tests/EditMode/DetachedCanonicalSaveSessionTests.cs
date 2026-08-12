#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

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

        private static Fixture CreateFixture()
        {
            SaveSpatialMigrationLimitsProfile profile = SaveSpatialMigrationLimitsLoader.Load(
                File.ReadAllText(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
            const string assignments = "\"mvpRoomSlotAssignments\":{\"Rooms\":[{" +
                "\"FloorIndex\":0,\"RoomIndex\":0,\"RoomOptionId\":\"placement.option.room.basic\"," +
                "\"MonsterOptionIds\":[],\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}],\"NextRevision\":2}";
            string json = "{\"rootUnknown\":{\"lexical\":1.00,\"items\":[true,null,{\"s\":\"a \\\" b \\\\ c\"}]}," +
                "\"schema\":\"save_root\",\"schemaVersion\":6,\"primary\":{" +
                "\"dungeonLayout\":{\"Slots\":[]},\"unknownPrimary\":[1,{\"n\":1.00}]," + assignments + "}}";
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
            Assert.That(json, Does.Not.Contain("mvpDungeonPlacements"));
            Assert.That(json, Does.Not.Contain("mvpDungeonFloorLayout"));
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
