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
        private Rect ForegroundMarkerPanelRect(VisualElement ui)
        {
            var camera = (Camera)typeof(ProductionDungeonController).GetField("worldCamera", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
            var sprite = (SpriteRenderer)typeof(DungeonFloorWorldView).GetField("foregroundParty", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller.World);
            float scale = controller.GetComponent<UIDocument>().panelSettings.scale;
            var min = camera.WorldToScreenPoint(sprite.bounds.min); var max = camera.WorldToScreenPoint(sprite.bounds.max);
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x) / scale, (Screen.height - Mathf.Max(min.y, max.y)) / scale,
                Mathf.Max(min.x, max.x) / scale, (Screen.height - Mathf.Min(min.y, max.y)) / scale);
        }
        private void AssertForegroundLabelClear(VisualElement ui)
        {
            Assert.That(controller.World.ForegroundPartyVisible, Is.True, "A caption must never suppress the party icon");
            var label = ui.Q<Label>("foregroundPartyLabel");
            Assert.That(ui.Q<Label>("foregroundActivity").text, Does.Contain("Adventurers"));
            if (label.resolvedStyle.display == DisplayStyle.None) return;
            var icon = ForegroundMarkerPanelRect(ui);
            var policy = controller.presentationPolicy;
            float gap = policy.ReplayActorLabelOffset - .5f; // subpixel layout tolerance
            icon = Rect.MinMaxRect(icon.xMin - gap, icon.yMin - gap, icon.xMax + gap, icon.yMax + gap);
            Assert.That(label.worldBound.Overlaps(icon), Is.False, "The actual label box must clear the rendered diamond plus its separation");
            var map = ui.Q("mapArea").worldBound;
            Assert.That(label.worldBound.xMin, Is.GreaterThanOrEqualTo(map.xMin - .5f));
            Assert.That(label.worldBound.yMin, Is.GreaterThanOrEqualTo(map.yMin - .5f));
            Assert.That(label.worldBound.xMax, Is.LessThanOrEqualTo(map.xMax + .5f));
            Assert.That(label.worldBound.yMax, Is.LessThanOrEqualTo(map.yMax + .5f));
            Assert.That(label.worldBound.Overlaps(ui.Q("floorRail").worldBound), Is.False);
        }
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
            ObserveSceneFocus(true); update();
            advance((root.Save.livingDungeon.Scheduler.NextOpportunityTime.Value - root.TimeService.SampleForeground()) / 1000d); update();
            for (int frame = 0; frame < 16 && !controller.World.ForegroundPartyVisible; frame++) { advance(.25); update(); yield return null; }
            Assert.That(controller.World.ForegroundPartyVisible, Is.True);
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(ui.Q<Label>("foregroundActivity").text, Does.Contain("Adventurers"));
            Assert.That(ui.Q<Label>("foregroundPartyLabel").text, Does.Contain("Adventurers"));
            byte[] canonical = root.SaveService.CanonicalSession.GetCurrentBytes();
            foreach (var size in new[] { new Vector2Int(720, 1280), new Vector2Int(1280, 720), new Vector2Int(360, 640) }) {
                EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x, size.y); controller.SetTextSize(DungeonTextSize.Large);
                // Observe resize-related focus changes through the real root;
                // the prebound TimeService remains the sole clock owner.
                for (int frame = 0; frame < 8; frame++) { ObserveSceneFocus(true); yield return null; }
                var marker = ui.Q<Label>("foregroundPartyLabel");
                TestContext.WriteLine("Live layout " + size + " marker=" + controller.World.ForegroundPartyVisible +
                    " label=" + marker.resolvedStyle.display + " bounds=" + marker.worldBound +
                    " map=" + ui.Q("mapArea").worldBound + " rail=" + ui.Q("floorRail").worldBound + " clock=" + root.TimeService.ForegroundPresentationActive);
                yield return RoomContentCapture("living-live-large-" + size.x + "x" + size.y + ".png");
                AssertForegroundLabelClear(ui);
                Assert.That(marker.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "The ordinary layout has room for the caption");
            }
            yield return ForegroundMarkerBoundaryLayouts(ui);
            CollectionAssert.AreEqual(canonical, root.SaveService.CanonicalSession.GetCurrentBytes());
            ObserveSceneFocus(false); yield return null;
            Assert.That(controller.World.ForegroundPartyVisible, Is.False);
            ObserveSceneFocus(true);
            Assert.That(root.TimeService.HasDeferredRecovery || root.TimeService.RecoveryClockActive, Is.True);
            Assert.That(root.ForegroundPresentationActive, Is.False, "Focus return cannot bypass durable recovery closure");
            long acceptedBeforeReturn = root.Save.livingDungeon.Scheduler.AcceptedThrough;
            long ticksBeforeReturn = root.Save.totalTicks; double manaBeforeReturn = root.Save.structureRuntime.ManaReserve;
            PumpSceneToClosed(); yield return null;
            Assert.That(root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(acceptedBeforeReturn));
            Assert.That(root.Save.totalTicks, Is.EqualTo(ticksBeforeReturn));
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(manaBeforeReturn));
            Assert.That(controller.World.ForegroundPartyVisible, Is.True);
            for (int boundary = 0; boundary < 20 && root.ForegroundActivity.RecentInteraction?.Kind != RunStepKind.MonsterEngagement; boundary++) {
                advance(Math.Max(0, root.ForegroundActivity.NextBoundary - root.TimeService.SampleForeground()) / 1000d);
                update(); yield return null;
            }
            Assert.That(root.ForegroundActivity.RecentInteraction?.Kind, Is.EqualTo(RunStepKind.MonsterEngagement));
            Assert.That(ui.Q<Label>("foregroundActivity").text, Does.Contain("attacks").Or.Contain("bypassed"));
            canonical = root.SaveService.CanonicalSession.GetCurrentBytes();
            foreach (var size in new[] { new Vector2Int(720, 1280), new Vector2Int(1280, 720), new Vector2Int(360, 640) }) {
                EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x, size.y);
                for (int frame = 0; frame < 8; frame++) { ObserveSceneFocus(true); yield return null; }
                var activity = ui.Q<Label>("foregroundActivity"); var map = ui.Q("mapArea");
                var activityScroll = ui.Q<ScrollView>("foregroundActivityScroll");
                var visibleActivity = activityScroll != null && activityScroll.resolvedStyle.display != DisplayStyle.None
                    ? activityScroll.contentViewport.worldBound : activity.worldBound;
                Assert.That(visibleActivity.yMax, Is.LessThanOrEqualTo(map.worldBound.yMin + 1), "Feedback must not cover the map");
                AssertForegroundLabelClear(ui);
                Assert.That(ui.worldBound.Contains(ui.Q("sessionActions").worldBound.center), Is.True, "Keep the ordinary actions onscreen");
                yield return RoomContentCapture("living-live-encounter-large-" + size.x + "x" + size.y + ".png");
            }
            CollectionAssert.AreEqual(canonical, root.SaveService.CanonicalSession.GetCurrentBytes());
            var profile = root.SaveService.ForegroundConfiguration.CreateProfile(root.SaveSpatialMigrationLimits.Canonical.ForSchema(13).Serialized);
            var execution = LivingDungeonContracts.Restore(root.Save.livingDungeon.Parties.Single(), profile);
            long finish;
            try { execution.DrainToCompletion(); finish = root.Save.livingDungeon.Parties.Single().AdmittedAt + execution.Inspect().LogicalTime; }
            finally { execution.Abandon(); }
            advance((finish - root.TimeService.SampleForeground()) / 1000d); update(); yield return null;
            Assert.That(root.Save.runHistory.LatestOutcome.RunId, Is.EqualTo("run-1"));
            Assert.That(ui.Q<Label>("foregroundActivity").text, Does.Contain("survived").And.Contain("loot"));
            Assert.That(ui.Q<Label>("replayAvailability").text, Is.EqualTo("Completed run available for replay in this session."));
            Assert.That(controller.StartReplay(), Is.True, "A just-completed compatible automatic run must retain its authoritative session evidence");
            yield return null;
            var liveSprite = (SpriteRenderer)typeof(DungeonFloorWorldView).GetField("foregroundParty", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller.World);
            var replaySprite = (SpriteRenderer)typeof(DungeonFloorWorldView).GetField("replayParty", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller.World);
            Assert.That(replaySprite.sprite, Is.SameAs(liveSprite.sprite), "Live and replay use the same party diamond artwork");
            Assert.That(replaySprite.transform.localScale, Is.EqualTo(liveSprite.transform.localScale));
            canonical = root.SaveService.CanonicalSession.GetCurrentBytes();
            controller.Replay.Advance(10000); yield return null; controller.CloseReplay();
            CollectionAssert.AreEqual(canonical, root.SaveService.CanonicalSession.GetCurrentBytes());
            Assert.That(root.Save.runHistory.RecentOutcomes.Count(o => o.RunId == "run-1"), Is.EqualTo(1));
            controller.SetTextSize(DungeonTextSize.Default);
        }

        private IEnumerator ForegroundMarkerBoundaryLayouts(VisualElement ui)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var camera = (Camera)typeof(ProductionDungeonController).GetField("worldCamera", flags).GetValue(controller);
            var present = (Action)Delegate.CreateDelegate(typeof(Action), controller,
                typeof(ProductionDungeonController).GetMethod("PresentForegroundPartyLabel", flags));
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(720, 1280), new Vector2Int(360, 640) }) {
                EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x, size.y);
                for (int frame = 0; frame < 8; frame++) { ObserveSceneFocus(true); yield return null; }
                controller.enabled = false;
                var position = camera.transform.position; float zoom = camera.orthographicSize;
                try {
                    foreach (float factor in new[] { .5f, 1f, 2f })
                    foreach (var target in new[] { new Vector2(.05f, .05f), new Vector2(.95f, .05f), new Vector2(.05f, .95f), new Vector2(.95f, .95f), new Vector2(.5f, .5f) }) {
                        camera.orthographicSize = zoom * factor;
                        var desired = camera.ViewportToWorldPoint(new Vector3(target.x, target.y, -camera.transform.position.z));
                        var center = controller.World.transform.TransformPoint(controller.World.ForegroundPartyPosition);
                        camera.transform.position += new Vector3(center.x - desired.x, center.y - desired.y, 0);
                        present(); yield return null; present(); yield return null;
                        TestContext.WriteLine("Marker edge " + size + " zoom=" + factor + " at=" + target + " icon=" + ForegroundMarkerPanelRect(ui) + " label=" + ui.Q("foregroundPartyLabel").worldBound + " display=" + ui.Q("foregroundPartyLabel").resolvedStyle.display);
                        AssertForegroundLabelClear(ui);
                        if (factor == 1f && target.x == .95f)
                            yield return RoomContentCapture("living-marker-edge-" + size.x + "x" + size.y + "-" + target.y + ".png");
                    }
                    var cached = typeof(ProductionDungeonController).GetField("foregroundLabelSize", flags);
                    var measured = cached.GetValue(controller);
                    cached.SetValue(controller, new Vector2(ui.Q("mapArea").contentRect.width + 1, ui.Q("mapArea").contentRect.height + 1));
                    present(); yield return null;
                    Assert.That(ui.Q("foregroundPartyLabel").resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "No valid placement hides only the optional label");
                    AssertForegroundLabelClear(ui);
                    cached.SetValue(controller, measured);
                } finally {
                    camera.transform.position = position; camera.orthographicSize = zoom; controller.enabled = true;
                }
                yield return null;
            }
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
            ObserveSceneFocus(true); update();
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
