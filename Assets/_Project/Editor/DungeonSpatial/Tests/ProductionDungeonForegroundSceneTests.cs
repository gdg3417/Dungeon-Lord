#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests
{
    public abstract partial class PhaseSevenA4ProductionSceneTests
    {
        private Action<double> InstallForegroundBoundaryTestClock()
        {
            var root = GameRoot.Instance; double sample = 100;
            var time = new TimeService(root.Logger, 10, 300, new SystemTimeSource(), () => sample);
            time.AttachSave(root.Save);
            time.BeginForeground(1000, root.Save.livingDungeon.Scheduler.AcceptedThrough);
            typeof(GameRoot).GetProperty(nameof(GameRoot.TimeService)).SetValue(root, time);
            return delta => sample += delta;
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
