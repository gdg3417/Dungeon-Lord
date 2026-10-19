#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests
{
    public abstract partial class PhaseSevenA4ProductionSceneTests
    {
        private Action<double> InstallForegroundBoundaryTestClock(ITimeSource utc = null)
        {
            var root = GameRoot.Instance; double sample = 100;
            var time = new TimeService(root.Logger, 10, 300, utc ?? new SystemTimeSource(), () => sample);
            time.AttachSave(root.Save);
            time.BeginForeground(1000, root.Save.livingDungeon.Scheduler.AcceptedThrough);
            typeof(GameRoot).GetProperty(nameof(GameRoot.TimeService)).SetValue(root, time);
            return delta => sample += delta;
        }

        private sealed class ForegroundBoundaryUtc : ITimeSource
        {
            public long Now;
            public long UtcNowUnixSeconds() => Now;
        }

        [UnityTest]
        public IEnumerator LivingDungeonB3B2AutomaticSceneLoopSettlesAndAdmitsAgainstCommittedEdit()
        {
            var root = GameRoot.Instance;
            Assert.That(root.TrySetQaManaFromDevPanel(true), Is.True);
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0];
            var room = floor.Layout.Rooms[0];
            var placed = root.SaveService.ExecuteCanonicalMutation(root.Save,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId,
                    room.RoomInstanceId, floor.FloorInstanceId, new TileCoordinate(1, 1)));
            Assert.That(placed.IsSuccess, Is.True, placed.Reason);
            var advance = InstallForegroundBoundaryTestClock();
            var update = (Action)Delegate.CreateDelegate(typeof(Action), root,
                typeof(GameRoot).GetMethod("UpdateForeground", BindingFlags.Instance | BindingFlags.NonPublic));
            var profile = root.SaveService.ForegroundConfiguration.CreateProfile(root.SaveSpatialMigrationLimits.Canonical.ForSchema(13).Serialized);
            string oldGeometry = null; RunOutcomeRecord expected = null; bool edited = false;
            int maximumBytes = 0; long replay = 0;
            for (int frame = 0; frame < 1600; frame++) {
                root.TimeService.SetForegroundFocus(true); advance(.25); update(); yield return null;
                Assert.That(typeof(GameRoot).GetField("foregroundFailure", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root), Is.Null);
                if (!edited && root.Save.livingDungeon.Parties.Count == 1 && controller.World.ForegroundPartyVisible) {
                    var party = root.Save.livingDungeon.Parties.Single();
                    var bytes = root.SaveService.CanonicalSession.GetCurrentBytes();
                    var execution = LivingDungeonContracts.Restore(party, profile);
                    try {
                        oldGeometry = execution.Snapshot.Floors[0].KnowledgeFingerprint;
                        while (!execution.IsTerminal) {
                            Assert.That(execution.NextOrdinal, Is.LessThan(profile.MaximumPartyReplay));
                            execution.Advance(1, execution.NextOrdinal);
                            maximumBytes = Math.Max(maximumBytes, execution.ExportCheckpoint(profile.Checkpoints).GetBytes().Length);
                        }
                        replay = execution.NextOrdinal; expected = execution.TakeOutcome();
                    } finally { execution.Abandon(); }
                    CollectionAssert.AreEqual(bytes, root.SaveService.CanonicalSession.GetCurrentBytes(), "Private reconstruction cannot publish");
                    Assert.That(CombatC0Diagnostics.Events(expected).Count(e => e.Kind == CombatC0EventKind.Attack), Is.GreaterThan(0));
                    controller.EnterEdit(); yield return null;
                    Assert.That(controller.World.ForegroundPartyVisible, Is.False);
                    var monster = root.Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments.Single(a => a.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId);
                    var checkpoint = (byte[])party.Checkpoint.Clone();
                    Assert.That(controller.Draft.Move(floor.FloorInstanceId, monster.RoomInstanceId, monster.AssignmentId, new TileCoordinate(3, 2)), Is.True);
                    while (controller.Draft.Durability == DraftDurability.Pending) { Assert.That(controller.Draft.FlushNext(), Is.True); yield return null; }
                    Assert.That(controller.Commit(), Is.True);
                    CollectionAssert.AreEqual(checkpoint, root.Save.livingDungeon.Parties.Single().Checkpoint);
                    edited = true;
                }
                if (edited && root.Save.runHistory.LatestOutcome?.RunId == "run-1" && root.Save.livingDungeon.Parties.Any(p => p.Sequence == 2)) {
                    Assert.That(root.Save.runHistory.RecentOutcomes.Count(o => o.RunId == "run-1"), Is.EqualTo(1));
                    Assert.That(EditMode.LivingDungeonPhaseABaselineTests.Structured(CombatC0Diagnostics.Events(root.Save.runHistory.LatestOutcome)),
                        Is.EqualTo(EditMode.LivingDungeonPhaseABaselineTests.Structured(CombatC0Diagnostics.Events(expected))));
                    Assert.That(CombatC0Diagnostics.ChargedWork(root.Save.runHistory.LatestOutcome), Is.EqualTo(CombatC0Diagnostics.ChargedWork(expected)));
                    var second = LivingDungeonContracts.Restore(root.Save.livingDungeon.Parties.Single(), profile);
                    try { Assert.That(second.Snapshot.Floors[0].KnowledgeFingerprint, Is.Not.EqualTo(oldGeometry)); }
                    finally { second.Abandon(); }
                    TestContext.WriteLine("Loaded production config: checkpointBytes=" + maximumBytes + " replay=" + replay +
                        " combatWork=" + CombatC0Diagnostics.ChargedWork(expected) + " receipts=" + CombatC0Diagnostics.Events(expected).Count);
                    yield break;
                }
            }
            Assert.Fail("Automatic scene loop did not settle and capture the edited second run within its bounded test horizon");
        }

        [UnityTest]
        public IEnumerator LivingDungeonB3B2ResumeCompletesSeveralBoundedPassesBeforeRunningAgain()
        {
            var root = GameRoot.Instance;
            var bounded = LivingDungeonForegroundConfig.Parse(System.Text.Encoding.UTF8.GetBytes(
                File.ReadAllText("Assets/_Project/Resources/living_dungeon_foreground.json")
                    .Replace("\"MaximumBoundariesPerPump\": 64", "\"MaximumBoundariesPerPump\": 1")));
            typeof(SaveService).GetProperty(nameof(SaveService.ForegroundConfiguration)).SetValue(root.SaveService, bounded);
            var utc = new ForegroundBoundaryUtc { Now = root.Save.lastSavedUtcUnix };
            root.SaveService.SetTimeSourceForTests(utc); root.SetOfflineTimeSourceForTests(utc);
            var advance = InstallForegroundBoundaryTestClock(utc);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(GameRoot).GetField("foregroundApplicationFocused", flags).SetValue(root, true);
            typeof(GameRoot).GetField("foregroundApplicationPaused", flags).SetValue(root, false);
            var update = (Action)Delegate.CreateDelegate(typeof(Action), root, typeof(GameRoot).GetMethod("UpdateForeground", flags));
            long before = root.Save.livingDungeon.Scheduler.AcceptedThrough, ticks = root.Save.totalTicks;
            advance(35); root.ApplyPauseState(true); root.ApplyPauseState(false);
            Assert.That(root.TimeService.IsPaused, Is.True);
            Assert.That(typeof(GameRoot).GetField("foregroundResumePending", flags).GetValue(root), Is.True);
            for (int pump = 0; pump < 8 && root.TimeService.IsPaused; pump++) update();
            Assert.That(typeof(GameRoot).GetField("foregroundFailure", flags).GetValue(root), Is.Null);
            Assert.That(root.TimeService.IsPaused, Is.False);
            Assert.That(root.Save.livingDungeon.IsClosed, Is.True);
            Assert.That(root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(before + 35000));
            Assert.That(root.Save.totalTicks - ticks, Is.EqualTo((before + 35000) / 10000 - before / 10000));
            long closed = root.Save.livingDungeon.Scheduler.ClosedThrough;
            ticks = root.Save.totalTicks; double mana = root.Save.structureRuntime.ManaReserve;
            for (int cycle = 0; cycle < 3; cycle++) { root.ApplyPauseState(true); root.ApplyPauseState(false); update(); }
            Assert.That(root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(closed));
            Assert.That(root.Save.totalTicks, Is.EqualTo(ticks));
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(mana));
            yield return null;
        }

        [UnityTest]
        public IEnumerator LivingDungeonB3B2FramesBeforeCachedBoundaryDoNotReconstructOrPublish()
        {
            var root = GameRoot.Instance;
            var advance = InstallForegroundBoundaryTestClock();
            var update = (Action)Delegate.CreateDelegate(typeof(Action), root,
                typeof(GameRoot).GetMethod("UpdateForeground", BindingFlags.Instance | BindingFlags.NonPublic));
            update();
            for (int i = 0; i < 16; i++) update();
            var session = root.SaveService.CanonicalSession; var view = root.ForegroundActivity;
            long accepted = root.Save.livingDungeon.Scheduler.AcceptedThrough;
            long allocated = GC.GetAllocatedBytesForCurrentThread();
            var timer = System.Diagnostics.Stopwatch.StartNew();
            for (int frame = 0; frame < 120; frame++) { advance(.0001); update(); }
            timer.Stop(); allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Assert.That(root.SaveService.CanonicalSession, Is.SameAs(session));
            Assert.That(root.ForegroundActivity, Is.SameAs(view));
            Assert.That(root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(accepted));
            Assert.That(allocated, Is.LessThanOrEqualTo(1024), "Cached pump gate must not reconstruct complete saves per frame");
            TestContext.WriteLine("120 cached pump gates: " + timer.Elapsed.TotalMilliseconds + " ms; " + allocated + " bytes on test thread");
            yield return null;
        }

        [UnityTest]
        public IEnumerator LivingDungeonB3B2LiveFloorActionsValidateAfterTheirElapsedTimeBarrier()
        {
            OpenFloorManagement(); Assert.That(controller.ConstructFloor(), Is.True);
            var root = GameRoot.Instance;
            controller.SelectFloor(root.Save.validatedCanonicalSpatialState.Floors[1].FloorInstanceId);
            controller.EnterEdit(); Assert.That(controller.SelectConstructionRoom("spatial.room.basic"), Is.True);
            controller.TapWorld(new TileCoordinate(1, 2)); Assert.That(controller.ConfirmConstructionPlacement(), Is.True);
            yield return null; yield return null; Assert.That(controller.Commit(), Is.True);
            var advance = InstallForegroundBoundaryTestClock();
            advance(1);
            Assert.That(controller.ChangeFloorLifecycle(FloorLifecycleAction.Activate), Is.True,
                "A fresh direct click must revalidate after its own partial-time flush");
            advance(1);
            Assert.That(controller.ChangeFloorLifecycle(FloorLifecycleAction.Deactivate), Is.True);
        }

        [UnityTest]
        public IEnumerator LivingDungeonB3B2SuspendedDrainRecognizesItsDurablyPersistedPauseBoundary()
        {
            var root = GameRoot.Instance;
            var bounded = LivingDungeonForegroundConfig.Parse(System.Text.Encoding.UTF8.GetBytes(
                File.ReadAllText("Assets/_Project/Resources/living_dungeon_foreground.json")
                    .Replace("\"MaximumBoundariesPerPump\": 64", "\"MaximumBoundariesPerPump\": 1")));
            typeof(SaveService).GetProperty(nameof(SaveService.ForegroundConfiguration)).SetValue(root.SaveService, bounded);
            var advance = InstallForegroundBoundaryTestClock();
            long before = root.Save.livingDungeon.Scheduler.AcceptedThrough;
            advance(35); root.ApplyPauseState(true);
            Assert.That(root.TimeService.IsPaused, Is.True);
            Assert.That(root.Save.livingDungeon.IsClosed, Is.False, "One bounded pause flush cannot close three ticks");
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var update = typeof(GameRoot).GetMethod("UpdateForeground", flags);
            for (int pump = 0; pump < 8 && !root.Save.livingDungeon.IsClosed; pump++) update.Invoke(root, null);
            Assert.That(typeof(GameRoot).GetField("foregroundFailure", flags).GetValue(root), Is.Null);
            Assert.That(root.Save.livingDungeon.IsClosed, Is.True);
            Assert.That(root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(before + 35000));
            Assert.That(typeof(GameRoot).GetField("_resumeOfflineBoundaryPersisted", flags).GetValue(root), Is.True,
                "The completed suspended transaction persisted the original pause boundary; resume must retain its existing offline policy");
            yield return null;
        }
    }
}
#endif
