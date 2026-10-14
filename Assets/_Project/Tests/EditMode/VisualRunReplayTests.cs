#if UNITY_EDITOR
using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class VisualRunReplayTests
    {
        internal static Fixture Completed()
        {
            var f = Fixture.Create(null); f.Configuration = PhaseSixA4Tests.Config();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)));
            foreach (var value in new[] { (MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 0),
                (MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId, 1),
                (MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId, 2) })
                f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(value.Item1, value.Item2, null, null, new TileCoordinate(value.Item3, 1))));
            f.Accept(PhaseFiveBBranchIntegrationTests.Run(f)); return f;
        }
        internal static DungeonRunReplay Create(Fixture f, DungeonPresentationPolicy policy)
        {
            var context = new DungeonDraftContext(f.Production, f.Configuration, f.Occupancy, f.Profile);
            Assert.That(DungeonRunReplay.TryCreate(f.Runtime.runHistory.LatestOutcome, f.State, f.Production.Catalog,
                context.Fingerprint(f.State), policy, out var replay, out var reason), Is.True, reason);
            return replay;
        }
        [Test]
        public void CompletedPublicationProjectsActualStepsAndDoesNotWriteOrResolveAgain()
        {
            var f = Completed(); var policy = ScriptableObject.CreateInstance<DungeonPresentationPolicy>();
            try
            {
                var run = f.Runtime.runHistory.LatestOutcome; var bytes = f.Session.GetCurrentBytes(); var live = JsonUtility.ToJson(f.Runtime);
                var replay = Create(f, policy);
                var timer = System.Diagnostics.Stopwatch.StartNew();
                int displayFrames = 0;
                while (!replay.Complete) { replay.Advance(.016f); var displayed = replay.DisplayPosition; displayFrames++; }
                timer.Stop();
                TestContext.WriteLine("Pure playback: " + replay.FrameCount + " recorded steps / " + displayFrames +
                    " display updates / " + timer.Elapsed.TotalMilliseconds.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) +
                    " ms (Windows Editor; excludes view/UI and projection construction).");
                CollectionAssert.AreEqual(run.PresentationSteps.Select(s => s.Kind), replay.Frames.Select(f => f.Step.Kind));
                Assert.That(run.PresentationSteps.Any(s => s.PositionSpace == RunPositionSpace.Floor), Is.True, "Recorded connections included");
                foreach (var kind in new[] { RunStepKind.RoomEntry, RunStepKind.Movement, RunStepKind.MonsterEngagement,
                    RunStepKind.TrapTrigger, RunStepKind.LootInteraction, RunStepKind.RoomEgress, RunStepKind.Terminal })
                    Assert.That(replay.Frames.Any(f => f.Step.Kind == kind), Is.True, kind.ToString());
                foreach (var frame in replay.Frames.Where(f => f.Category != null)) Assert.That(frame.HasMarker, Is.True);
                var aggregate = run.SpatialEvents.Single(e => e.Kind == RunSpatialEventKind.AdventurerTraversal);
                Assert.That(run.SpatialEvents.Any(e => e.EncounterOrdinal >= 0 && e.Ordinal < aggregate.Ordinal), Is.True);
                for (int repeat = 0; repeat < 3; repeat++)
                { replay = Create(f, policy); replay.Advance(float.MaxValue); Assert.That(replay.Complete, Is.True); replay.Close(); }
                CollectionAssert.AreEqual(bytes, f.Session.GetCurrentBytes());
                CollectionAssert.AreEqual(bytes, f.FileSystem.ReadAllBytes(f.ActivePath));
                Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(live), "Mana, Heat, history, learning, identity and saves unchanged");
                Assert.That(replay.Current.Survivors, Is.EqualTo(run.SurvivalSummary.SurvivorCount));
                Assert.That(JsonUtility.ToJson(run), Does.Not.Contain("PresentationSteps").And.Not.Contain("PresentationLayoutFingerprint"));
            }
            finally { UnityEngine.Object.DestroyImmediate(policy); }
        }
        [TestCase(CardinalOrientation.Zero)]
        [TestCase(CardinalOrientation.Ninety)]
        [TestCase(CardinalOrientation.OneEighty)]
        [TestCase(CardinalOrientation.TwoSeventy)]
        public void ProjectionUsesCanonicalTransformForRepositionedRotatedRoom(CardinalOrientation orientation)
        {
            var f = Completed(); var floor = f.State.Floors[0]; var room = floor.Layout.Rooms[0];
            room.Anchor = new TileCoordinate(11, 17); room.Orientation = orientation;
            var local = new TileCoordinate(1, 2); var shape = f.Production.Catalog.Rooms.Single(d => d.RoomDefinitionId == room.RoomDefinitionId).GrossFootprint;
            Assert.That(RoomLocalCoordinateTransform.TryToFloor(local, shape, room.Anchor, orientation, out var expected), Is.True);
            Assert.That(DungeonRunReplay.TryProject(local, RunPositionSpace.RoomLocal, room.RoomInstanceId, floor, f.Production.Catalog, out var position), Is.True);
            Assert.That(position, Is.EqualTo(new Vector2(expected.X + .5f, expected.Y + .5f)));
            Assert.That(DungeonRunReplay.TryProject(expected, RunPositionSpace.Floor, null, floor, f.Production.Catalog, out position), Is.True);
            Assert.That(position, Is.EqualTo(new Vector2(expected.X + .5f, expected.Y + .5f)));
            Assert.That(DungeonRunReplay.TryProject(local, RunPositionSpace.RoomLocal, "missing", floor, f.Production.Catalog, out _), Is.False);
        }
        [Test]
        public void PlaybackLifecycleAndPartitioningAreDeterministic()
        {
            var f = Completed(); var policy = ScriptableObject.CreateInstance<DungeonPresentationPolicy>();
            try
            {
                var a = Create(f, policy); var b = Create(f, policy);
                a.TogglePause(); Assert.That(a.Advance(100), Is.False); Assert.That(a.Index, Is.Zero);
                a.TogglePause(); a.Advance(.4f); for (int i = 0; i < 4; i++) b.Advance(.1f);
                Assert.That(a.Index, Is.EqualTo(b.Index)); Assert.That(a.DisplayPosition, Is.EqualTo(b.DisplayPosition));
                a.Suspend(); Assert.That(a.Paused, Is.True); a.Close(); a.TogglePause();
                Assert.That(a.Advance(100), Is.False); b.Advance(float.MaxValue); Assert.That(b.Complete, Is.True);
                Assert.That(b.Advance(100), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(policy); }
        }
        [TestCase(false)]
        [TestCase(true)]
        public void MissingRestartAndHistoricalLayoutsFailClosed(bool edited)
        {
            var f = Completed(); var policy = ScriptableObject.CreateInstance<DungeonPresentationPolicy>();
            try
            {
                var bytes = f.Session.GetCurrentBytes(); var outcome = f.Runtime.runHistory.LatestOutcome;
                if (edited) outcome.PresentationLayoutFingerprint = "historical.test";
                else { f.Reopen(); outcome = f.Runtime.runHistory.LatestOutcome; Assert.That(outcome.PresentationSteps, Is.Null); }
                var context = new DungeonDraftContext(f.Production, f.Configuration, f.Occupancy, f.Profile);
                Assert.That(DungeonRunReplay.TryCreate(outcome, f.State, f.Production.Catalog, context.Fingerprint(f.State), policy,
                    out var replay, out var reason), Is.False); Assert.That(replay, Is.Null);
                Assert.That(reason, Is.EqualTo(edited ? DungeonRunReplay.Historical : DungeonRunReplay.Missing));
                CollectionAssert.AreEqual(bytes, f.FileSystem.ReadAllBytes(f.ActivePath));
            }
            finally { UnityEngine.Object.DestroyImmediate(policy); }
        }
        [Test]
        public void EvidenceRetainsThroughOrdinaryPublicationAndLimitsRejectWithoutEffects()
        {
            var f = Completed(); var outcome = f.Runtime.runHistory.LatestOutcome;
            var target = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(f.Runtime)); RunTransientEvidence.Retain(f.Runtime, target);
            Assert.That(target.runHistory.LatestOutcome.PresentationSteps, Is.SameAs(outcome.PresentationSteps));
            Assert.That(target.runHistory.LatestOutcome.PresentationLayoutFingerprint, Is.EqualTo(outcome.PresentationLayoutFingerprint));
            var policy = ScriptableObject.CreateInstance<DungeonPresentationPolicy>(); policy.MaximumReplaySteps = 1;
            try { Assert.That(DungeonRunReplay.TryCreate(outcome, f.State, f.Production.Catalog, outcome.PresentationLayoutFingerprint,
                policy, out _, out _), Is.False); }
            finally { UnityEngine.Object.DestroyImmediate(policy); }
        }
        [TestCase(false)]
        [TestCase(true)]
        public void TwoFloorConnectionsAndBranchReturnsFollowOriginalOrdinalStream(bool multiFloor)
        {
            {
                var f = multiFloor ? PhaseSixA4Tests.Eligible() : PhaseFiveBBranchIntegrationTests.Fixture();
                if (multiFloor)
                {
                    PhaseSixA4Tests.ForceDescent(f.Configuration, true);
                    foreach (int floor in new[] { 0, 1 }) PhaseSixA4Tests.AddContent(f, floor, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
                    var activation = PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.ActivateAllEligible, null);
                    f.Accept(activation);
                }
                // A multi-floor manual commit repeats snapshot qualification at persistence.
                // Use the established full writer with injected loot, not the single-floor
                // branch helper whose authority intentionally omits that dependency.
                f.Accept(multiFloor ? PhaseSixA4Tests.Writer(f).CommitPhaseFiveBRun(f.ActivePath,
                    f.FileSystem, f.Session, f.Runtime, PhaseFiveBBranchIntegrationTests.Service(f.Configuration),
                    RunPostureResolver.BalancedId, Math.Max(1, f.Runtime.lastSavedUtcUnix)) : PhaseFiveBBranchIntegrationTests.Run(f));
                var policy = ScriptableObject.CreateInstance<DungeonPresentationPolicy>();
                try
                {
                    var replay = Create(f, policy);
                    Assert.That(replay.Frames.Any(f => f.Step.Kind == (multiFloor ? RunStepKind.FloorTransfer : RunStepKind.BranchReturn)), Is.True);
                    Assert.That(replay.Frames.Where(f => f.HasPosition).Select(f => f.FloorId).Distinct().Count(), Is.EqualTo(multiFloor ? 2 : 1));
                    CollectionAssert.AreEqual(Enumerable.Range(0, replay.FrameCount).Select(i => (long)i), replay.Frames.Select(f => f.Step.Ordinal));
                }
                finally { UnityEngine.Object.DestroyImmediate(policy); }
            }
        }
        [TestCase(false)]
        [TestCase(true)]
        public void ActualEngineUnreachableInteractionsAndWipeNeverInventReach(bool wipe)
        {
            var f = Completed(); var floor = f.State.Floors[0]; var roomId = floor.Layout.Rooms[0].RoomInstanceId;
            var assignments = new[] {
                new RunRoomAssignment { AssignmentId = "m", CategoryId = MvpDungeonPlacementIds.MonsterCategoryId,
                    OptionId = MvpDungeonPlacementIds.SkeletonOptionId, RoomLocalPosition = new TileCoordinate(wipe ? 0 : 3, wipe ? 1 : 3) },
                new RunRoomAssignment { AssignmentId = "l", CategoryId = MvpDungeonPlacementIds.LootNodeCategoryId,
                    OptionId = MvpDungeonPlacementIds.HiddenCacheOptionId, RoomLocalPosition = new TileCoordinate(3,3) },
                new RunRoomAssignment { AssignmentId = "t", CategoryId = MvpDungeonPlacementIds.TrapCategoryId,
                    OptionId = MvpDungeonPlacementIds.SpikeTrapOptionId, RoomLocalPosition = new TileCoordinate(2,3) }
            };
            if (wipe) assignments = assignments.Concat(new[] { "m2", "m3" }.Select((id, index) => new RunRoomAssignment {
                AssignmentId = id, CategoryId = MvpDungeonPlacementIds.MonsterCategoryId, OptionId = MvpDungeonPlacementIds.SkeletonOptionId,
                RoomLocalPosition = new TileCoordinate(0,1), Sequence = index + 1 })).ToArray();
            var room = new MvpOrderedRouteRoom { FloorIndex = 0, RoomIndex = 0, RoomInstanceId = roomId,
                Assignments = assignments, IncludeRoomPlacement = true, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                Spatial = new IntraroomSnapshot { FloorInstanceId = floor.FloorInstanceId, RoomInstanceId = roomId,
                    RoomDefinitionId = floor.Layout.Rooms[0].RoomDefinitionId, Ingress = new TileCoordinate(0,1),
                    Egress = new TileCoordinate(3,1), MaximumMaterializedTiles = 16,
                    TraversableTiles = new[] {new TileCoordinate(0,1),new TileCoordinate(1,1),new TileCoordinate(2,1),new TileCoordinate(3,1),new TileCoordinate(3,3),new TileCoordinate(2,3)},
                    Assignments = assignments.Select(a => new IntraroomAssignment { AssignmentId = a.AssignmentId,
                        OccupiedTiles = new[] {a.RoomLocalPosition} }).ToArray() } };
            if (wipe)
            {
                f.Configuration.PhaseFiveB.MinPartySize = f.Configuration.PhaseFiveB.MaxPartySize = 3;
                foreach (var profile in f.Configuration.PhaseFiveB.DamageProfiles) profile.MinimumDamage = profile.MaximumDamage = 10000;
            }
            var service = PhaseFiveBBranchIntegrationTests.Service(f.Configuration);
            var outcome = service.StartRoute(f.Runtime.structureRuntime, 0, 17, RunPostureResolver.BalancedId, new[] {room}).DrainToCompletion(true);
            outcome.PresentationLayoutFingerprint = "test.synthetic";
            var policy = ScriptableObject.CreateInstance<DungeonPresentationPolicy>();
            try
            {
                Assert.That(DungeonRunReplay.TryCreate(outcome, f.State, f.Production.Catalog, "test.synthetic", policy, out var replay, out var reason), Is.True, reason);
                if (wipe)
                {
                    Assert.That(replay.Frames.Any(f => f.Step.Kind == RunStepKind.RoomEgress || f.Step.Kind == RunStepKind.LootInteraction), Is.False);
                    Assert.That(replay.Frames.Last().Survivors, Is.Zero);
                    Assert.That(outcome.SpatialEvents.Single(e => e.Kind == RunSpatialEventKind.AdventurerTraversal).Path.Length, Is.EqualTo(1));
                }
                else
                {
                    var unreachable = replay.Frames.Where(f => f.Step.Kind == RunStepKind.UnreachableInteraction).ToArray();
                    Assert.That(unreachable, Has.Length.EqualTo(2)); Assert.That(unreachable.All(f => f.HasMarker), Is.True);
                    Assert.That(replay.Frames.Any(f => f.Step.Kind == RunStepKind.TrapTrigger), Is.False, "Off-path trap never triggers");
                    var repeatRoom = room; repeatRoom.Assignments = assignments.Reverse().ToArray();
                    var repeat = service.StartRoute(f.Runtime.structureRuntime, 0, 17, RunPostureResolver.BalancedId, new[] {repeatRoom}).DrainToCompletion(true);
                    CollectionAssert.AreEqual(outcome.PresentationSteps.Select(s => (s.Kind,s.AssignmentId,s.Position)),
                        repeat.PresentationSteps.Select(s => (s.Kind,s.AssignmentId,s.Position)), "Same-step tie order comes from engine");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(policy); }
        }
    }
}
#endif
