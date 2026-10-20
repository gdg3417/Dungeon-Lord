#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0.Tests
{
    public abstract partial class PhaseSevenA4ProductionSceneTests
    {
        // Display-only fake data against the existing rendered test room. It is
        // never installed in the canonical owner or passed to a simulation.
        private LivingDungeonForegroundInspection ForegroundDisplay(params RunStep[] steps)
        {
            var member = new RunPartyMember(0, new RunClassProfile { ClassId = "adventurer.class.warrior", LevelOneMaxHealth = 10 },
                new RunBehaviorProfile { Id = "test.behavior" }, 1);
            return new LivingDungeonForegroundInspection { RunId = "run-1", FloorId = controller.World.FloorInstanceId,
                Ordinal = steps.Length, GeometryCompatible = true, Step = steps.Last(), PositionStep = steps.Last(),
                Steps = Array.AsReadOnly(steps), Members = Array.AsReadOnly(new[] { new RunMemberState(member) }),
                AdmittedAt = 100, MovementUnits = 1000 };
        }
        private RunStep ForegroundDisplayStep(int ordinal, TileCoordinate tile, int distance = 1,
            RunPositionSpace space = RunPositionSpace.RoomLocal)
            => new RunStep(ordinal == 0 ? RunStepKind.RoomEntry : RunStepKind.Movement, 0,
                GameRoot.Instance.Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms[0].RoomInstanceId,
                position: tile, distance: ordinal == 0 ? 0 : distance, run: "run-1", ordinal: ordinal, time: ordinal * 1000, space: space);

        [UnityTest] public IEnumerator LivingDungeonB3B2RecordedAdjacentMovementInterpolatesWithoutCanonicalEffects()
        {
            var root = GameRoot.Instance; var world = controller.World;
            var canonical = root.SaveService.CanonicalSession.GetCurrentBytes(); var save = root.Save;
            var entry = ForegroundDisplayStep(0, new TileCoordinate(0, 0));
            var movement = ForegroundDisplayStep(1, new TileCoordinate(1, 0));
            var before = ForegroundDisplay(entry); var after = ForegroundDisplay(entry, movement);
            world.PresentForeground(before, true, 100); world.PresentForeground(before, false, 100);
            var from = world.ForegroundPartyPosition;
            Assert.That(DungeonRunReplay.TryProject(movement.Position, movement.PositionSpace, movement.RoomId,
                root.Save.validatedCanonicalSpatialState.Floors[0], root.ProductionSpatialContent.Catalog, out var destination), Is.True);
            world.PresentForeground(after, false, 1100);
            Assert.That(world.ForegroundPartyPosition, Is.EqualTo(from));
            world.PresentForeground(after, false, 1600);
            Assert.That(world.ForegroundPartyPosition, Is.EqualTo(Vector2.Lerp(from, destination, .5f)));
            world.PresentForeground(after, false, 2100);
            Assert.That(world.ForegroundPartyPosition, Is.EqualTo(destination));
            for (int frame = 0; frame < 120; frame++) world.PresentForeground(after, false, 2100);
            Assert.That(root.Save, Is.SameAs(save));
            CollectionAssert.AreEqual(canonical, root.SaveService.CanonicalSession.GetCurrentBytes());
            yield break;
        }

        [UnityTest] public IEnumerator LivingDungeonB3B2PresentationSnapsGapsAndInterruptionsAndHidesUnknownGeometry()
        {
            var world = controller.World;
            var entry = ForegroundDisplayStep(0, new TileCoordinate(0, 0));
            var one = ForegroundDisplayStep(1, new TileCoordinate(1, 0));
            var before = ForegroundDisplay(entry); var after = ForegroundDisplay(entry, one);
            world.PresentForeground(before, true, 100); world.PresentForeground(before, false, 100);
            world.PresentForeground(after, false, 1600);
            world.PresentForeground(after, true, 1600); world.PresentForeground(after, false, 1600);
            var target = world.ForegroundPartyPosition;
            world.PresentForeground(after, false, 1700);
            Assert.That(world.ForegroundPartyPosition, Is.EqualTo(target), "Resume reconstructs current position, never missed motion");
            world.PresentForeground(before, true, 100); world.PresentForeground(before, false, 100);
            var gap = ForegroundDisplay(entry, ForegroundDisplayStep(1, new TileCoordinate(3, 0), 3));
            world.PresentForeground(gap, false, 1100); var jumped = world.ForegroundPartyPosition;
            world.PresentForeground(gap, false, 1600); Assert.That(world.ForegroundPartyPosition, Is.EqualTo(jumped));
            world.PresentForeground(before, true, 100); world.PresentForeground(before, false, 100);
            var missed = ForegroundDisplay(entry, one, ForegroundDisplayStep(2, new TileCoordinate(0, 0)), ForegroundDisplayStep(3, new TileCoordinate(1, 0)));
            world.PresentForeground(missed, false, 3100); var current = world.ForegroundPartyPosition;
            world.PresentForeground(missed, false, 3600); Assert.That(world.ForegroundPartyPosition, Is.EqualTo(current), "Do not replay a backlog");
            after.GeometryCompatible = false; world.PresentForeground(after, false, 1700); Assert.That(world.ForegroundPartyVisible, Is.False);
            var unknown = ForegroundDisplay(ForegroundDisplayStep(0, default, space: RunPositionSpace.None));
            world.PresentForeground(unknown, false, 100); Assert.That(world.ForegroundPartyVisible, Is.False);
            var outside = ForegroundDisplay(ForegroundDisplayStep(0, new TileCoordinate(-1000, -1000), space: RunPositionSpace.Floor));
            world.PresentForeground(outside, false, 100); Assert.That(world.ForegroundPartyVisible, Is.False);
            world.PresentForeground(before, false, 100); Assert.That(world.ForegroundPartyVisible, Is.True);
            controller.SelectFloor(controller.SelectedFloorInstanceId);
            world.PresentForeground(after = ForegroundDisplay(entry, one), false, 1600); var reopened = world.ForegroundPartyPosition;
            world.PresentForeground(after, false, 1700); Assert.That(world.ForegroundPartyPosition, Is.EqualTo(reopened), "Reconstruction/floor selection snaps");
            yield break;
        }

        [UnityTest] public IEnumerator LivingDungeonB3B2LiveLabelsSessionReplayAndLargeLayoutsObserveOnlyCommittedState()
        {
            var root = GameRoot.Instance;
            Assert.That(root.TrySetQaManaFromDevPanel(true), Is.True);
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0];
            Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save, DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId,
                floor.Layout.Rooms[0].RoomInstanceId, floor.FloorInstanceId, new TileCoordinate(1, 1))).IsSuccess, Is.True);
            var advance = InstallForegroundBoundaryTestClock();
            var update = (Action)Delegate.CreateDelegate(typeof(Action), root, typeof(GameRoot).GetMethod("UpdateForeground", BindingFlags.NonPublic | BindingFlags.Instance));
            root.TimeService.SetForegroundFocus(true); update();
            advance((root.Save.livingDungeon.Scheduler.NextOpportunityTime.Value - root.TimeService.SampleForeground()) / 1000d); update();
            for (int frame = 0; frame < 16 && !controller.World.ForegroundPartyVisible; frame++) { advance(.25); update(); yield return null; }
            Assert.That(controller.World.ForegroundPartyVisible, Is.True);
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(ui.Q<Label>("foregroundActivity").text, Does.Contain("Adventurers"));
            Assert.That(ui.Q<Label>("foregroundPartyLabel").text, Does.Contain("Adventurers"));
            byte[] canonical = root.SaveService.CanonicalSession.GetCurrentBytes();
            foreach (var size in new[] { new Vector2Int(720, 1280), new Vector2Int(1280, 720), new Vector2Int(360, 640) }) {
                EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x, size.y); controller.SetTextSize(DungeonTextSize.Large);
                for (int frame = 0; frame < 8; frame++) yield return null;
                var marker = ui.Q<Label>("foregroundPartyLabel");
                Assert.That(marker.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(marker.worldBound.width, Is.GreaterThan(0));
                Assert.That(ui.Q("mapArea").worldBound.Contains(marker.worldBound.center), Is.True);
                yield return RoomContentCapture("living-live-large-" + size.x + "x" + size.y + ".png");
            }
            CollectionAssert.AreEqual(canonical, root.SaveService.CanonicalSession.GetCurrentBytes());
            root.TimeService.SetForegroundFocus(false); yield return null;
            Assert.That(controller.World.ForegroundPartyVisible, Is.False);
            root.TimeService.SetForegroundFocus(true); yield return null;
            Assert.That(controller.World.ForegroundPartyVisible, Is.True);
            var profile = root.SaveService.ForegroundConfiguration.CreateProfile(root.SaveSpatialMigrationLimits.Canonical.ForSchema(13).Serialized);
            var execution = LivingDungeonContracts.Restore(root.Save.livingDungeon.Parties.Single(), profile);
            long finish;
            try { execution.DrainToCompletion(); finish = root.Save.livingDungeon.Parties.Single().AdmittedAt + execution.Inspect().LogicalTime; }
            finally { execution.Abandon(); }
            advance((finish - root.TimeService.SampleForeground()) / 1000d); update(); yield return null;
            Assert.That(root.Save.runHistory.LatestOutcome.RunId, Is.EqualTo("run-1"));
            Assert.That(ui.Q<Label>("foregroundActivity").text, Does.Contain("survived").And.Contain("loot"));
            Assert.That(controller.StartReplay(), Is.True, "A just-completed compatible automatic run must retain its authoritative session evidence");
            canonical = root.SaveService.CanonicalSession.GetCurrentBytes();
            controller.Replay.Advance(10000); yield return null; controller.CloseReplay();
            CollectionAssert.AreEqual(canonical, root.SaveService.CanonicalSession.GetCurrentBytes());
            Assert.That(root.Save.runHistory.RecentOutcomes.Count(o => o.RunId == "run-1"), Is.EqualTo(1));
            controller.SetTextSize(DungeonTextSize.Default);
        }

        [UnityTest] public IEnumerator LivingDungeonB3B2ActiveIdleFramesDoNotReconstructSaveOrRebuildUi()
        {
            var root = GameRoot.Instance;
            Assert.That(root.TrySetQaManaFromDevPanel(true), Is.True);
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0];
            Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save, DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId,
                floor.Layout.Rooms[0].RoomInstanceId, floor.FloorInstanceId, new TileCoordinate(1, 1))).IsSuccess, Is.True);
            var advance = InstallForegroundBoundaryTestClock();
            var update = (Action)Delegate.CreateDelegate(typeof(Action), root, typeof(GameRoot).GetMethod("UpdateForeground", BindingFlags.NonPublic | BindingFlags.Instance));
            root.TimeService.SetForegroundFocus(true); update();
            advance((root.Save.livingDungeon.Scheduler.NextOpportunityTime.Value - root.TimeService.SampleForeground()) / 1000d); update();
            yield return null;
            Assert.That(root.Save.livingDungeon.Parties.Count, Is.EqualTo(1));
            var session = root.SaveService.CanonicalSession; var view = root.ForegroundActivity;
            int render = controller.World.ReconstructionCount;
            ForegroundQualificationMetrics.Reset(); ForegroundQualificationMetrics.Enabled = true;
            try {
                for (int frame = 0; frame < 120; frame++) { advance(.0001); update(); yield return null; }
                foreach (var area in new[] { ForegroundQualificationMetrics.Area.Stage, ForegroundQualificationMetrics.Area.Restore,
                    ForegroundQualificationMetrics.Area.Inspect, ForegroundQualificationMetrics.Area.AtomicWrite,
                    ForegroundQualificationMetrics.Area.Publication, ForegroundQualificationMetrics.Area.Present, ForegroundQualificationMetrics.Area.WorldRender })
                    Assert.That(ForegroundQualificationMetrics.Calls[(int)area], Is.Zero, area.ToString());
            } finally { ForegroundQualificationMetrics.Enabled = false; }
            Assert.That(root.SaveService.CanonicalSession, Is.SameAs(session));
            Assert.That(root.ForegroundActivity, Is.SameAs(view));
            Assert.That(controller.World.ReconstructionCount, Is.EqualTo(render));
        }
    }
}
#endif
