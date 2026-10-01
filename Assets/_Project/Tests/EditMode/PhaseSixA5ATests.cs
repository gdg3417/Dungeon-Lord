#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class PhaseSixA5ATests
    {
        private static Fixture Eligible() => PhaseSixA4Tests.Eligible();
        private static DetachedCanonicalWriteAuthority Writer(Fixture f) => PhaseSixA4Tests.Writer(f);
        private static string FloorTwo(Fixture f) => PhaseSixA4Tests.FloorTwo(f);
        private static ActiveFloorRunSnapshot Snapshot(Fixture f) => PhaseSixA4Tests.Snapshot(f);
        private static string FrozenEleven(byte[] bytes) => Encoding.UTF8.GetString(bytes)
            .Replace("\"schemaVersion\":12", "\"schemaVersion\":11")
            .Replace(",\"sharedFloorKnowledge\":{\"Records\":[]}", "");

        private static FloorKnowledgeRecord Record(Fixture f, int floorIndex)
        {
            string id = f.State.Floors.Single(x => x.FloorIndex == floorIndex).FloorInstanceId;
            Assert.That(FloorKnowledgeApplicability.TryCompute(f.State, f.Runtime.corridorContent,
                f.Profile.Canonical, id, out string fingerprint), Is.True);
            return new FloorKnowledgeRecord { FloorInstanceId = id, ApplicabilityFingerprint = fingerprint,
                RewardKnown = true, PerceivedRewardScore = 2, DangerKnown = true,
                PerceivedDangerScore = 3, ConfidenceKnown = true, Confidence = .75,
                HasLastConfirmedRun = true, LastConfirmedRunId = "run-test-1" };
        }

        private static byte[] WithKnowledge(Fixture f, params FloorKnowledgeRecord[] records)
        {
            var recognized = DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile);
            var owned = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context);
            var update = f.Session.PrepareLiveReplacement(recognized, f.State, owned.Investment,
                owned.CorridorContent, owned.BranchKnowledge,
                new SharedFloorKnowledgeAuthority { Records = records });
            Assert.That(update.IsSuccess, Is.True, update.Reason);
            return update.Update.GetBytes();
        }

        [Test]
        public void ElevenToTwelveAddsOnlyEmptyOwnerAndPreservesSourceBytes()
        {
            var f = Eligible();
            string source = FrozenEleven(f.Session.GetCurrentBytes());
            source = source.Substring(0, source.Length - 1) + ",\"futureExtension\":{\"v\":1}}";
            byte[] eleven = Encoding.UTF8.GetBytes(source);
            byte[] original = (byte[])eleven.Clone();
            var frozen = DetachedCompleteSaveContract.ParseValidateFrozenSchemaElevenAndRoundTrip(eleven, f.Profile.Canonical);
            Assert.That(frozen.IsValid, Is.True);
            Assert.That(SchemaElevenToTwelveUpgrade.TryPrepare(eleven, f.Profile.Canonical, out byte[] first), Is.True);
            Assert.That(SchemaElevenToTwelveUpgrade.TryPrepare(eleven, f.Profile.Canonical, out byte[] second), Is.True);
            CollectionAssert.AreEqual(first, second); CollectionAssert.AreEqual(eleven, original);
            Assert.That(Encoding.UTF8.GetString(first), Is.EqualTo(Encoding.UTF8.GetString(eleven)
                .Replace("\"schemaVersion\":11", "\"schemaVersion\":12")
                .Replace("\"sharedBranchKnowledge\":{\"Records\":[]}",
                    "\"sharedBranchKnowledge\":{\"Records\":[]},\"sharedFloorKnowledge\":{\"Records\":[]}")));
            var current = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(first, f.Context);
            Assert.That(current.CurrentTargetValidated, Is.True);
            Assert.That(current.FloorKnowledge.Records, Is.Empty);
            Assert.That(JsonUtility.ToJson(current.State), Is.EqualTo(JsonUtility.ToJson(frozen.State)));
            Assert.That(JsonUtility.ToJson(current.BranchKnowledge), Is.EqualTo(JsonUtility.ToJson(frozen.BranchKnowledge)));
            Assert.That(SchemaElevenToTwelveUpgrade.TryPrepare(first, f.Profile.Canonical, out _), Is.False);
        }

        [Test]
        public void RecordsRoundTripInStableFloorOrderAndBranchKnowledgeStaysSeparate()
        {
            var f = Eligible();
            var second = Record(f, 1); var first = Record(f, 0);
            byte[] bytes = WithKnowledge(f, second, first);
            var parsed = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, f.Context);
            Assert.That(parsed.CurrentTargetValidated, Is.True);
            CollectionAssert.AreEqual(new[] { first.FloorInstanceId, second.FloorInstanceId }.OrderBy(x => x, StringComparer.Ordinal),
                parsed.FloorKnowledge.Records.Select(r => r.FloorInstanceId).ToArray());
            Assert.That(parsed.BranchKnowledge.Records, Is.Empty);
            Assert.That(parsed.FloorKnowledge.Records[0].PerceivedRewardScore, Is.EqualTo(2));
            Assert.That(DetachedCanonicalSaveSession.Open(bytes, f.Context, f.Profile).IsSuccess, Is.True);
            Assert.That(PhaseSixFloorKnowledge.Validate(new SharedFloorKnowledgeAuthority(), f.State, 5), Is.True);
        }

        [TestCase("duplicate")]
        [TestCase("identity")]
        [TestCase("fingerprint")]
        [TestCase("nonfinite")]
        [TestCase("confidence")]
        [TestCase("unknown_value")]
        [TestCase("last_run")]
        public void MalformedFloorKnowledgeRejectsBeforePublication(string defect)
        {
            var f = Eligible(); var record = Record(f, 0);
            FloorKnowledgeRecord[] records = { record };
            switch (defect)
            {
                case "duplicate": records = new[] { record, PhaseSixFloorKnowledge.Copy(record) }; break;
                case "identity": record.FloorInstanceId = "bad id"; break;
                case "fingerprint": record.ApplicabilityFingerprint = "bad"; break;
                case "nonfinite": record.PerceivedDangerScore = double.PositiveInfinity; break;
                case "confidence": record.Confidence = 1.01; break;
                case "unknown_value": record.RewardKnown = false; break;
                case "last_run": record.HasLastConfirmedRun = false; break;
            }
            var recognized = DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile);
            var owned = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context);
            byte[] before = f.FileSystem.ReadAllBytes(f.ActivePath);
            var update = f.Session.PrepareLiveReplacement(recognized, f.State, owned.Investment,
                owned.CorridorContent, owned.BranchKnowledge,
                new SharedFloorKnowledgeAuthority { Records = records });
            Assert.That(update.IsSuccess, Is.False);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [Test]
        public void RequiredContentInvalidatesButLifecycleAloneDoesNot()
        {
            var f = Eligible(); var original = Record(f, 1);
            Assert.That(FloorKnowledgeApplicability.IsApplicable(original, f.State,
                f.Runtime.corridorContent, f.Profile.Canonical), Is.True);
            f.Accept(Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                FloorLifecycleAction.Activate, FloorTwo(f)));
            Assert.That(FloorKnowledgeApplicability.IsApplicable(original, f.State,
                f.Runtime.corridorContent, f.Profile.Canonical), Is.True);
            f.Accept(Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                FloorLifecycleAction.Deactivate, FloorTwo(f)));
            Assert.That(FloorKnowledgeApplicability.IsApplicable(original, f.State,
                f.Runtime.corridorContent, f.Profile.Canonical), Is.True);
            PhaseSixA4Tests.AddContent(f, 1, MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId);
            Assert.That(FloorKnowledgeApplicability.IsApplicable(original, f.State,
                f.Runtime.corridorContent, f.Profile.Canonical), Is.False);
            Assert.That(Record(f, 0).ApplicabilityFingerprint,
                Is.Not.EqualTo(Record(f, 1).ApplicabilityFingerprint));
        }

        [Test]
        public void SurvivorLearningReconfirmsClampsAndOnlyPublishesAfterCompleteRun()
        {
            var f = Eligible();
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.LootNodeCategoryId,
                MvpDungeonPlacementIds.HiddenCacheOptionId);
            PhaseSixA4Tests.ForceDescent(f.Configuration, false);
            var writer = Writer(f);
            var initial = Snapshot(f);
            Assert.That(initial.FloorKnowledge.Records, Is.Empty);
            byte[] before = f.FileSystem.ReadAllBytes(f.ActivePath);
            var simulation = new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot());
            var observed = simulation.SimulateSnapshot(1, initial);
            Assert.That(observed.Party.ActiveCount, Is.GreaterThan(0));
            Assert.That(observed.Success, Is.True, observed.FinalRouteOutcomeKey);
            Assert.That(observed.FloorTransitions, Is.Empty);
            Assert.That(observed.RoomResolutions.Any(room => room.FloorIndex == 0 && room.Reached), Is.True);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            f.Accept(writer.CommitPhaseFiveBRun(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                simulation, RunPostureResolver.BalancedId, Math.Max(1, f.Runtime.lastSavedUtcUnix)));
            Assert.That(f.Runtime.sharedFloorKnowledge.Records.Single().Confidence,
                Is.EqualTo(f.Configuration.PhaseSix.InitialObservationConfidence));
            f.Reopen();
            Assert.That(f.Runtime.sharedFloorKnowledge.Records.Single().Confidence,
                Is.EqualTo(f.Configuration.PhaseSix.InitialObservationConfidence));
            Assert.That(initial.FloorKnowledge.Records, Is.Empty);
            for (int index = 0; index < 3; index++)
                f.Accept(writer.CommitPhaseFiveBRun(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                    simulation, RunPostureResolver.BalancedId, Math.Max(1, f.Runtime.lastSavedUtcUnix)));
            Assert.That(f.Runtime.sharedFloorKnowledge.Records.Single().Confidence,
                Is.EqualTo(f.Configuration.PhaseSix.ConfidenceClamp));
        }

        [Test]
        public void ExitBeforeFloorTwoDoesNotLearnItsHiddenSnapshot()
        {
            var f = Eligible(); PhaseSixA4Tests.ForceDescent(f.Configuration, false);
            f.Accept(Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                FloorLifecycleAction.Activate, FloorTwo(f)));
            var simulation = new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot());
            f.Accept(Writer(f).CommitPhaseFiveBRun(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                simulation, RunPostureResolver.BalancedId, Math.Max(1, f.Runtime.lastSavedUtcUnix)));
            Assert.That(f.Runtime.sharedFloorKnowledge.Records.Select(r => r.FloorInstanceId),
                Is.EquivalentTo(new[] { f.State.Floors[0].FloorInstanceId }));
        }

        [Test]
        public void ChangedObservedContentStartsNewConfidenceAndSnapshotCopiesKnowledge()
        {
            var f = Eligible(); PhaseSixA4Tests.ForceDescent(f.Configuration, false);
            var simulation = new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot());
            f.Accept(Writer(f).CommitPhaseFiveBRun(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                simulation, RunPostureResolver.BalancedId, Math.Max(1, f.Runtime.lastSavedUtcUnix)));
            var snapshot = Snapshot(f);
            FloorKnowledgeRecord captured = snapshot.FloorKnowledge.Records.Single();
            string oldFingerprint = captured.ApplicabilityFingerprint;
            f.Runtime.sharedFloorKnowledge.Records[0].Confidence = 0;
            Assert.That(snapshot.FloorKnowledge.Records.Single().Confidence,
                Is.EqualTo(f.Configuration.PhaseSix.InitialObservationConfidence));
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.LootNodeCategoryId,
                MvpDungeonPlacementIds.HiddenCacheOptionId);
            Assert.That(FloorKnowledgeApplicability.IsApplicable(captured, f.State,
                f.Runtime.corridorContent, f.Profile.Canonical), Is.False);
            Assert.That(snapshot.Floors[0].KnowledgeFingerprint, Is.EqualTo(oldFingerprint));
            var first = simulation.SimulateSnapshot(2, snapshot);
            var second = simulation.SimulateSnapshot(2, snapshot);
            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
            f.Accept(Writer(f).CommitPhaseFiveBRun(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                simulation, RunPostureResolver.BalancedId, Math.Max(1, f.Runtime.lastSavedUtcUnix)));
            Assert.That(f.Runtime.sharedFloorKnowledge.Records.Single().Confidence,
                Is.EqualTo(f.Configuration.PhaseSix.InitialObservationConfidence));
            Assert.That(f.Runtime.sharedFloorKnowledge.Records.Single().ApplicabilityFingerprint,
                Is.Not.EqualTo(oldFingerprint));
        }

        [Test]
        public void FinalWipeDoesNotCreatePreciseFloorKnowledge()
        {
            var f = Eligible();
            for (int index = 0; index < 2; index++)
                PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.TrapCategoryId,
                MvpDungeonPlacementIds.SpikeTrapOptionId);
            f.Configuration.PhaseFiveB.MinPartySize = f.Configuration.PhaseFiveB.MaxPartySize = 3;
            foreach (var profile in f.Configuration.PhaseFiveB.DamageProfiles)
                if (profile.OptionId == MvpDungeonPlacementIds.SkeletonOptionId ||
                    profile.OptionId == MvpDungeonPlacementIds.SpikeTrapOptionId)
                    profile.MinimumDamage = profile.MaximumDamage = 100;
            var simulation = new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot());
            Assert.That(simulation.SimulateSnapshot(1, Snapshot(f)).Party.IsWiped, Is.True);
            f.Accept(Writer(f).CommitPhaseFiveBRun(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                simulation, RunPostureResolver.BalancedId, Math.Max(1, f.Runtime.lastSavedUtcUnix)));
            Assert.That(f.Runtime.sharedFloorKnowledge.Records, Is.Empty);
        }

        [Test]
        public void CanonicalOrderingMakesFingerprintIndependentOfInputArrayOrder()
        {
            var f = Eligible(); var record = Record(f, 0);
            f.State.Floors[0].Layout.Nodes = f.State.Floors[0].Layout.Nodes.Reverse().ToArray();
            Assert.That(FloorKnowledgeApplicability.TryCompute(f.State, f.Runtime.corridorContent,
                f.Profile.Canonical, record.FloorInstanceId, out string reordered), Is.True);
            Assert.That(reordered, Is.EqualTo(record.ApplicabilityFingerprint));
        }

        [Test]
        public void MaterialRoomTopologyChangeInvalidatesFloorKnowledge()
        {
            var f = Eligible(); var record = Record(f, 0);
            var room = f.State.Floors[0].Layout.Rooms[0];
            room.Anchor = new TileCoordinate(room.Anchor.X + 1, room.Anchor.Y);
            Assert.That(FloorKnowledgeApplicability.IsApplicable(record, f.State,
                f.Runtime.corridorContent, f.Profile.Canonical), Is.False);
        }

        [Test]
        public void OptionalCorridorContentChangeInvalidatesOnlyItsFloor()
        {
            var f = Eligible(); var first = Record(f, 0); var second = Record(f, 1);
            var corridor = new CorridorContentAuthority { Assignments = new[] {
                new CorridorContentAssignment { AssignmentId = "floor-knowledge-test-assignment",
                    FloorInstanceId = second.FloorInstanceId, OptionalBranchId = "floor-knowledge-test-branch",
                    EdgeId = "floor-knowledge-test-edge", CategoryId = MvpDungeonPlacementIds.LootNodeCategoryId,
                    OptionId = MvpDungeonPlacementIds.HiddenCacheOptionId, Sequence = 0,
                    Tile = new TileCoordinate(1, 1) } } };
            Assert.That(FloorKnowledgeApplicability.IsApplicable(first, f.State,
                corridor, f.Profile.Canonical), Is.True);
            Assert.That(FloorKnowledgeApplicability.IsApplicable(second, f.State,
                corridor, f.Profile.Canonical), Is.False);
        }

        [Test]
        public void StaleSessionAndFailedReplaceLeaveKnowledgeAndSettlementUnchanged()
        {
            var f = Eligible(); var writer = Writer(f);
            var stale = f.Session;
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.LootNodeCategoryId,
                MvpDungeonPlacementIds.HiddenCacheOptionId);
            byte[] before = f.FileSystem.ReadAllBytes(f.ActivePath);
            SaveData live = f.Runtime;
            var simulation = new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot());
            var rejected = writer.CommitPhaseFiveBRun(f.ActivePath, f.FileSystem, stale, f.Runtime,
                simulation, RunPostureResolver.BalancedId, Math.Max(1, f.Runtime.lastSavedUtcUnix));
            Assert.That(rejected.IsSuccess, Is.False);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            f.FileSystem.EnableFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 1);
            rejected = writer.CommitPhaseFiveBRun(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                simulation, RunPostureResolver.BalancedId, Math.Max(1, f.Runtime.lastSavedUtcUnix));
            Assert.That(rejected.IsSuccess, Is.False);
            f.FileSystem.DisableFailure();
            Assert.That(f.Runtime, Is.SameAs(live));
            Assert.That(f.Runtime.sharedFloorKnowledge.Records, Is.Empty);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [Test]
        public void ConcurrentBytesAndFailedReadbackCannotPublishKnowledge()
        {
            var f = Eligible(); var writer = Writer(f);
            byte[] before = f.FileSystem.ReadAllBytes(f.ActivePath);
            var simulation = new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot());
            f.FileSystem.EnableTargetedFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Read,
                paths => paths.Length == 1 && paths[0] == f.ActivePath, 4, false);
            var result = writer.CommitPhaseFiveBRun(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                simulation, RunPostureResolver.BalancedId, Math.Max(1, f.Runtime.lastSavedUtcUnix));
            Assert.That(result.IsSuccess, Is.False);
            f.FileSystem.DisableFailure();
            Assert.That(f.Runtime.sharedFloorKnowledge.Records, Is.Empty);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            byte[] concurrent = (byte[])before.Clone(); concurrent[concurrent.Length - 1] = 32;
            f.FileSystem.Seed(f.ActivePath, concurrent);
            result = writer.CommitPhaseFiveBRun(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                simulation, RunPostureResolver.BalancedId, Math.Max(1, f.Runtime.lastSavedUtcUnix));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(f.Runtime.sharedFloorKnowledge.Records, Is.Empty);
            CollectionAssert.AreEqual(concurrent, f.FileSystem.ReadAllBytes(f.ActivePath));
        }
    }
}
#endif
