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
            var f = Fixture.Create(null);
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
                context.Fingerprint(f.State), CorridorFingerprint(f), policy, out var replay, out var reason), Is.True, reason);
            return replay;
        }
        internal static string CorridorFingerprint(Fixture f)
        {
            Assert.That(RunTransientEvidence.TryCorridorFingerprint(f.Runtime.runHistory.LatestOutcome,
                f.Runtime.corridorContent, f.Profile.Canonical, out var fingerprint), Is.True);
            return fingerprint;
        }
        private static RunOutcomeRecord LegacyPresentation(Fixture f)
        {
            // Supported legacy executable, transient read-only evidence only.
            // Never a schema-17 write or historical producer/activation proof.
            var before = f.Session.GetCurrentBytes();
            var run = PhaseFiveBBranchIntegrationTests.Service(f).StartSnapshot(1, PhaseSixA4Tests.Snapshot(f)).DrainToCompletion(true);
            Assert.That(CombatC0Diagnostics.IsCombatC0(run), Is.False);
            run.PresentationLayoutFingerprint = new DungeonDraftContext(f.Production, f.Configuration, f.Occupancy, f.Profile).Fingerprint(f.State);
            Assert.That(RunTransientEvidence.TryCorridorFingerprint(run, f.Runtime.corridorContent, f.Profile.Canonical, out var corridor), Is.True);
            run.PresentationCorridorFingerprint = corridor;
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            return run;
        }
        private static void AssertOriginalStepStream(RunOutcomeRecord run, DungeonRunReplay replay)
        {
            // C1 projects multiple recorded combat events for one run ordinal.
            // Preserve every original step in order and verify every repeated
            // frame still references that exact authoritative step.
            Assert.That(replay.Frames.Select(frame => frame.Step.Ordinal), Is.Ordered);
            CollectionAssert.AreEqual(run.PresentationSteps.Select(step => step.Ordinal),
                replay.Frames.Select(frame => frame.Step.Ordinal).Distinct());
            var source = run.PresentationSteps.ToDictionary(step => step.Ordinal);
            foreach (var frame in replay.Frames) Assert.That(frame.Step, Is.EqualTo(source[frame.Step.Ordinal]));
        }
        [TestCase("tile")]
        [TestCase("option")]
        [TestCase("removed")]
        [TestCase("id")]
        [TestCase("sequence")]
        public void CorridorAuthorityChangesFailHistoricalValidationWithoutChangingSpatialFingerprint(string change)
        {
            var f = PhaseFiveBBranchIntegrationTests.Fixture(); f.Accept(PhaseFiveBBranchIntegrationTests.Run(f));
            var policy = ScriptableObject.CreateInstance<DungeonPresentationPolicy>();
            try
            {
                var original = Create(f, policy); var run = f.Runtime.runHistory.LatestOutcome;
                var legacy = LegacyPresentation(f);
                Assert.That(DungeonRunReplay.TryCreate(legacy, f.State, f.Production.Catalog, legacy.PresentationLayoutFingerprint,
                    legacy.PresentationCorridorFingerprint, policy, out _, out var legacyReason), Is.True, legacyReason);
                Assert.That(run.BranchOutcomes.Single().Traversed, Is.True);
                var current = PhaseFiveSaveContracts.Canonicalize(f.Runtime.corridorContent);
                string canonical = run.PresentationLayoutFingerprint;
                var first = current.Assignments[0];
                switch (change)
                {
                    case "tile": first.Tile = new TileCoordinate(first.Tile.X + 1, first.Tile.Y); break;
                    case "option": first.OptionId = "test.replaced"; break;
                    case "removed": current.Assignments = current.Assignments.Skip(1).ToArray(); break;
                    case "id": first.AssignmentId += ".replacement"; break;
                    case "sequence": first.Sequence++; break;
                }
                Assert.That(RunTransientEvidence.TryCorridorFingerprint(run, current, f.Profile.Canonical, out var changed), Is.True);
                Assert.That(DungeonRunReplay.TryCreate(run, f.State, f.Production.Catalog, canonical, changed, policy,
                    out _, out var reason), Is.False);
                Assert.That(reason, Is.EqualTo(DungeonRunReplay.Layout));
                Assert.That(DungeonRunReplay.TryCreate(legacy, f.State, f.Production.Catalog, canonical, changed, policy,
                    out _, out legacyReason), Is.False);
                Assert.That(legacyReason, Is.EqualTo(DungeonRunReplay.Historical));
                Assert.That(new DungeonDraftContext(f.Production, f.Configuration, f.Occupancy, f.Profile).Fingerprint(f.State), Is.EqualTo(canonical));
            }
            finally { UnityEngine.Object.DestroyImmediate(policy); }
        }
        [Test]
        public void CorridorIdentityIsCanonicalAndIgnoresUnrelatedGameplayChanges()
        {
            var f = PhaseFiveBBranchIntegrationTests.Fixture(); f.Accept(PhaseFiveBBranchIntegrationTests.Run(f));
            var run = f.Runtime.runHistory.LatestOutcome;
            var reversed = new CorridorContentAuthority { Assignments = f.Runtime.corridorContent.Assignments.Reverse().ToArray() };
            Assert.That(RunTransientEvidence.TryCorridorFingerprint(run, reversed, f.Profile.Canonical, out var fingerprint), Is.True);
            Assert.That(fingerprint, Is.EqualTo(run.PresentationCorridorFingerprint));
            f.Runtime.structureRuntime.ManaReserve += 17; f.Runtime.structureRuntime.Heat += 2;
            reversed.Assignments = reversed.Assignments.Concat(new[] { new CorridorContentAssignment {
                FloorInstanceId = "test.unvisited.floor", OptionalBranchId = "test.unvisited.branch" } }).ToArray();
            Assert.That(RunTransientEvidence.TryCorridorFingerprint(run, reversed, f.Profile.Canonical, out fingerprint), Is.True);
            Assert.That(fingerprint, Is.EqualTo(run.PresentationCorridorFingerprint));
            var policy = ScriptableObject.CreateInstance<DungeonPresentationPolicy>();
            try { Assert.That(Create(f, policy), Is.Not.Null); }
            finally { UnityEngine.Object.DestroyImmediate(policy); }
            var copy = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(f.Runtime)); RunTransientEvidence.Retain(f.Runtime, copy);
            Assert.That(copy.runHistory.LatestOutcome.PresentationCorridorFingerprint, Is.EqualTo(fingerprint));
            Assert.That(JsonUtility.ToJson(run), Does.Not.Contain("PresentationCorridorFingerprint"));
        }
        internal static RunOutcomeRecord CombatEvidence(Fixture f, bool fatal, bool zero, SavedSpatialFloor source = null)
        {
            var floor = source ?? f.State.Floors[0]; var roomId = floor.Layout.Rooms[0].RoomInstanceId;
            var assignments = new[] {
                new RunRoomAssignment { AssignmentId = "combat.m1", CategoryId = MvpDungeonPlacementIds.MonsterCategoryId,
                    OptionId = MvpDungeonPlacementIds.SkeletonOptionId, RoomLocalPosition = new TileCoordinate(0,3) },
                new RunRoomAssignment { AssignmentId = "combat.m2", CategoryId = MvpDungeonPlacementIds.MonsterCategoryId,
                    OptionId = MvpDungeonPlacementIds.SkeletonOptionId, RoomLocalPosition = new TileCoordinate(3,3), Sequence = 1 },
                new RunRoomAssignment { AssignmentId = "combat.trap", CategoryId = MvpDungeonPlacementIds.TrapCategoryId,
                    OptionId = MvpDungeonPlacementIds.SpikeTrapOptionId, RoomLocalPosition = new TileCoordinate(2,1) },
                new RunRoomAssignment { AssignmentId = "combat.loot", CategoryId = MvpDungeonPlacementIds.LootNodeCategoryId,
                    OptionId = MvpDungeonPlacementIds.HiddenCacheOptionId, RoomLocalPosition = new TileCoordinate(3,1) },
                new RunRoomAssignment { AssignmentId = "combat.offpath", CategoryId = MvpDungeonPlacementIds.TrapCategoryId,
                    OptionId = MvpDungeonPlacementIds.SpikeTrapOptionId, RoomLocalPosition = new TileCoordinate(1,3), Sequence = 1 }
            };
            if (source != null) assignments = source.RoomContents.Assignments.Select(a => new RunRoomAssignment {
                AssignmentId = a.AssignmentId, CategoryId = a.CategoryId, OptionId = a.OptionId,
                Sequence = a.Sequence, RoomLocalPosition = a.RoomLocalPosition }).ToArray();
            foreach (var profile in f.Configuration.PhaseFiveB.DamageProfiles)
                profile.MinimumDamage = profile.MaximumDamage = zero ? (profile.CategoryId == MvpDungeonPlacementIds.TrapCategoryId ? 2 : 0) : fatal ? 10000 : 1;
            if (zero)
            {
                f.Configuration.PhaseFiveB.TrapMitigationCoefficient = 1;
                foreach (var memberClass in f.Configuration.PhaseFiveB.Classes) memberClass.TrapExpertise = 1;
            }
            var room = new MvpOrderedRouteRoom { FloorIndex = 0, RoomIndex = 0, RoomInstanceId = roomId,
                Assignments = assignments, IncludeRoomPlacement = true, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                Spatial = new IntraroomSnapshot { FloorInstanceId = floor.FloorInstanceId, RoomInstanceId = roomId,
                    RoomDefinitionId = floor.Layout.Rooms[0].RoomDefinitionId, Ingress = new TileCoordinate(0,1), Egress = new TileCoordinate(3,1),
                    MaximumMaterializedTiles = 16, TraversableTiles = Enumerable.Range(0,4).SelectMany(x =>
                        Enumerable.Range(0,4).Select(y => new TileCoordinate(x,y))).ToArray(),
                    Assignments = assignments.Select(a => new IntraroomAssignment { AssignmentId = a.AssignmentId,
                        OccupiedTiles = new[] { a.RoomLocalPosition } }).ToArray() } };
            var run = PhaseFiveBBranchIntegrationTests.Service(f.Configuration).StartRoute(f.Runtime.structureRuntime, 0, 17,
                RunPostureResolver.BalancedId, new[] { room }).DrainToCompletion(true);
            run.PresentationLayoutFingerprint = "test.combat"; run.PresentationCorridorFingerprint = "test.corridor";
            return run;
        }
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void SequentialApproachDamageAndCasualtiesUseOnlyRecordedEvidence(bool fatal, bool zero)
        {
            var f = Completed(); var run = CombatEvidence(f, fatal, zero);
            string observed = JsonUtility.ToJson(run);
            if (zero) Assert.That(run.EncounterEvents.Single(e => e.AssignmentId == "combat.trap").UnmitigatedDamage, Is.GreaterThan(0));
            // Transform the recorded room through all orientations below; no canonical write occurs.
            var policy = ScriptableObject.CreateInstance<DungeonPresentationPolicy>();
            try
            {
                foreach (CardinalOrientation orientation in Enum.GetValues(typeof(CardinalOrientation)))
                {
                    f.State.Floors[0].Layout.Rooms[0].Anchor = new TileCoordinate(11,17);
                    f.State.Floors[0].Layout.Rooms[0].Orientation = orientation;
                    Assert.That(DungeonRunReplay.TryCreate(run, f.State, f.Production.Catalog, "test.combat", "test.corridor", policy,
                        out var replay, out var reason), Is.True, reason);
                    var monsters = replay.Frames.Where(v => v.Step.Kind == RunStepKind.MonsterEngagement).ToArray();
                    Assert.That(monsters, Has.Length.EqualTo(2));
                    Assert.That(monsters[0].Step.AssignmentId, Is.Not.EqualTo(monsters[1].Step.AssignmentId));
                    while (!replay.Complete)
                    {
                        var frame = replay.Current;
                        if (frame.Step.Kind == RunStepKind.MonsterEngagement)
                        {
                            Assert.That(frame.MonsterPath.Count, Is.GreaterThan(1));
                            var evidence = run.SpatialEvents.Single(e => e.AssignmentId == frame.Step.AssignmentId);
                            Assert.That(frame.MonsterPath.Count, Is.EqualTo(evidence.Path.Length));
                            for (int p = 0; p < evidence.Path.Length; p++)
                            {
                                Assert.That(DungeonRunReplay.TryProject(evidence.Path[p], RunPositionSpace.RoomLocal, frame.Step.RoomId,
                                    f.State.Floors[0], f.Production.Catalog, out var projected), Is.True);
                                Assert.That(frame.MonsterPath[p], Is.EqualTo(projected));
                            }
                            Assert.That(Vector2.Distance(replay.MonsterPosition, frame.MonsterPath[0]), Is.LessThan(.001f));
                            Assert.That(replay.DamageVisible, Is.False);
                            replay.Advance(policy.ReplayMonsterTileSeconds * .5f);
                            Assert.That(Vector2.Distance(replay.MonsterPosition, Vector2.Lerp(frame.MonsterPath[0], frame.MonsterPath[1], .5f)), Is.LessThan(.001f));
                            replay.Suspend(); var paused = replay.MonsterPosition; replay.Advance(100);
                            Assert.That(replay.MonsterPosition, Is.EqualTo(paused)); replay.TogglePause();
                            replay.Advance(replay.ApproachSeconds - replay.Elapsed);
                            Assert.That(replay.MonsterPosition, Is.EqualTo(frame.MarkerPosition)); Assert.That(replay.DamageVisible, Is.True);
                        }
                        if (frame.Step.Encounter != null)
                        {
                            if (zero) Assert.That(frame.Step.Encounter.Damage, Is.Zero);
                            else Assert.That(frame.Step.Encounter.Damage, Is.GreaterThan(0));
                            Assert.That(replay.DamageApplied, Is.True);
                            replay.Suspend(); float elapsed = replay.Elapsed; replay.Advance(100);
                            Assert.That(replay.DamageVisible, Is.True); Assert.That(replay.Elapsed, Is.EqualTo(elapsed)); replay.TogglePause();
                        }
                        replay.Advance(replay.CurrentDuration - replay.Elapsed + .00001f);
                    }
                    Assert.That(replay.DamageVisible, Is.False);
                    Assert.That(replay.Frames.Any(v => v.Step.AssignmentId == "combat.offpath"), Is.False);
                    Assert.That(replay.Frames.Count(v => v.Step.Encounter?.HealthAfter == 0), Is.EqualTo(fatal ? 3 : 0));
                    replay.Close(); Assert.That(replay.DamageVisible, Is.False);
                    Assert.That(JsonUtility.ToJson(run), Is.EqualTo(observed), "Observer playback never recalculates an outcome");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(policy); }
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
                AssertOriginalStepStream(run, replay);
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
                var legacy = edited ? LegacyPresentation(f) : null;
                if (edited) outcome.PresentationLayoutFingerprint = "historical.test";
                else { f.Reopen(); outcome = f.Runtime.runHistory.LatestOutcome; Assert.That(outcome.PresentationSteps, Is.Null); }
                var context = new DungeonDraftContext(f.Production, f.Configuration, f.Occupancy, f.Profile);
                Assert.That(DungeonRunReplay.TryCreate(outcome, f.State, f.Production.Catalog, context.Fingerprint(f.State), CorridorFingerprint(f), policy,
                    out var replay, out var reason), Is.False); Assert.That(replay, Is.Null);
                Assert.That(reason, Is.EqualTo(edited ? DungeonRunReplay.Layout : DungeonRunReplay.Missing));
                if (edited) {
                    Assert.That(DungeonRunReplay.TryCreate(legacy, f.State, f.Production.Catalog, context.Fingerprint(f.State),
                        legacy.PresentationCorridorFingerprint, policy, out _, out var validReason), Is.True, validReason);
                    legacy.PresentationLayoutFingerprint = "historical.test";
                    Assert.That(DungeonRunReplay.TryCreate(legacy, f.State, f.Production.Catalog, context.Fingerprint(f.State),
                        legacy.PresentationCorridorFingerprint, policy, out _, out var legacyReason), Is.False);
                    Assert.That(legacyReason, Is.EqualTo(DungeonRunReplay.Historical));
                }
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
                CorridorFingerprint(f), policy, out _, out _), Is.False); }
            finally { UnityEngine.Object.DestroyImmediate(policy); }
        }
        [TestCase(false)]
        [TestCase(true)]
        public void TwoFloorConnectionsAndBranchReturnsFollowOriginalOrdinalStream(bool multiFloor)
        {
            {
                var f = multiFloor ? PhaseSixA4Tests.Eligible(PhaseFourTestSupport.AuthoredInputs((config, _) =>
                    PhaseSixA4Tests.ForceDescent(config, true))) : PhaseFiveBBranchIntegrationTests.Fixture();
                if (multiFloor)
                {
                    foreach (int floor in new[] { 0, 1 }) PhaseSixA4Tests.AddContent(f, floor, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
                    var activation = PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.ActivateAllEligible, null);
                    f.Accept(activation);
                }
                // A multi-floor manual commit repeats snapshot qualification at persistence.
                // Use the established full writer with injected loot, not the single-floor
                // branch helper whose authority intentionally omits that dependency.
                f.Accept(multiFloor ? PhaseSixA4Tests.Writer(f).CommitPhaseFiveBRun(f.ActivePath,
                    f.FileSystem, f.Session, f.Runtime, PhaseFiveBBranchIntegrationTests.Service(f),
                    RunPostureResolver.BalancedId, Math.Max(1, f.Runtime.lastSavedUtcUnix)) : PhaseFiveBBranchIntegrationTests.Run(f));
                var policy = ScriptableObject.CreateInstance<DungeonPresentationPolicy>();
                try
                {
                    var replay = Create(f, policy);
                    Assert.That(replay.Frames.Any(f => f.Step.Kind == (multiFloor ? RunStepKind.FloorTransfer : RunStepKind.BranchReturn)), Is.True);
                    Assert.That(replay.Frames.Where(f => f.HasPosition).Select(f => f.FloorId).Distinct().Count(), Is.EqualTo(multiFloor ? 2 : 1));
                    if (!multiFloor) Assert.That(replay.CorridorContents.Count, Is.EqualTo(f.Runtime.corridorContent.Assignments.Length));
                    AssertOriginalStepStream(f.Runtime.runHistory.LatestOutcome, replay);
                    CollectionAssert.AreEqual(Enumerable.Range(0, f.Runtime.runHistory.LatestOutcome.PresentationSteps.Length).Select(i => (long)i),
                        replay.Frames.Select(frame => frame.Step.Ordinal).Distinct());
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
            outcome.PresentationCorridorFingerprint = "test.corridor";
            var policy = ScriptableObject.CreateInstance<DungeonPresentationPolicy>();
            try
            {
                Assert.That(DungeonRunReplay.TryCreate(outcome, f.State, f.Production.Catalog, "test.synthetic", "test.corridor", policy, out var replay, out var reason), Is.True, reason);
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
                    var viewObject = new GameObject("UnreachableReplayFixture");
                    try
                    {
                        var world = viewObject.AddComponent<DungeonFloorWorldView>(); world.Initialize(policy);
                        world.Render(floor, f.Production, f.Occupancy, f.Profile.Canonical.Spatial.MaximumMaterializedTiles, 65536, false);
                        world.PresentReplay(replay, false); replay.Advance(float.MaxValue); world.PresentReplay(replay, false);
                        Assert.That(viewObject.GetComponentsInChildren<SpriteRenderer>().Count(v => v.name == "RecordedCorridorContent" && v.color == policy.InvalidColor), Is.EqualTo(2));
                        Assert.That(world.ReplayCasualtyCount, Is.Zero);
                        replay.Close(); world.PresentReplay(replay, false);
                        Assert.That(viewObject.GetComponentsInChildren<SpriteRenderer>().Any(v => v.name == "RecordedCorridorContent"), Is.False);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(viewObject); }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(policy); }
        }
    }
}
#endif
