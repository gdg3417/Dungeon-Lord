#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;

namespace DungeonBuilder.M0.Tests
{
    public abstract partial class PhaseSevenA4ProductionSceneTests
    {
        [UnityTest]
        public IEnumerator VisualRunReplayProductionLifecycleAndResponsiveControls()
        {
            var root = GameRoot.Instance;
            Assert.That(controller.StartReplay(), Is.False);
            root.Save.structureRuntime.ManaReserve = 1000; // Explicit fixture funding before content purchases.
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0]; var room = floor.Layout.Rooms[0];
            foreach (var value in new[] { ("placement.category.trap", "placement.option.trap.spike", 1),
                ("placement.category.loot_node", "placement.option.loot_node.hidden_cache", 2) })
            {
                var placed = root.SaveService.ExecuteCanonicalMutation(root.Save, DetachedCanonicalMutationRequest.Place(
                    value.Item1, value.Item2, room.RoomInstanceId, floor.FloorInstanceId, new TileCoordinate(value.Item3,1)));
                Assert.That(placed.IsSuccess, Is.True, placed.Reason);
            }
            root.Save.completedResearch = new CompletedResearchState { ProjectIds = new[] { "ac_100" } };
            root.Save.structureRuntime.ManaReserve = 1000;
            Assert.That(root.SaveService.CommitFloorConstruction(root.Save, root.SaveService.PreviewFloorConstruction(root.Save)).IsSuccess, Is.True);
            string otherFloor = root.Save.validatedCanonicalSpatialState.Floors.Single(f => f.FloorIndex == 1).FloorInstanceId;
            Assert.That(root.SimulateRunOnce(), Is.True);
            var bytes = File.ReadAllBytes(root.SaveService.SavePath); var live = JsonUtility.ToJson(root.Save);
            foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,720), new Vector2Int(720,1280) })
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x, size.y);
                Assert.That(controller.StartReplay(), Is.True); controller.ToggleReplayPause();
                int views = controller.World.GetComponentsInChildren<SpriteRenderer>(true).Length;
                for (int i = 0; i < 15; i++) yield return null;
                var ui = controller.GetComponent<UIDocument>().rootVisualElement;
                foreach (var name in new[] { "pauseReplay", "closeReplay", "edit" }) UatSafeAction(ui.Q<Button>(name));
                Assert.That(ui.Q<Label>("replayEvent").text, Does.Contain(root.Save.runHistory.LatestOutcome.RunId));
                int index = controller.Replay.Index; var position = controller.Replay.DisplayPosition;
                controller.SelectFloor(otherFloor); yield return null;
                Assert.That(controller.World.ReplayPartyVisible, Is.False);
                Assert.That(controller.Replay.Index, Is.EqualTo(index));
                controller.SelectFloor(floor.FloorInstanceId); yield return null;
                Assert.That(controller.World.ReplayPartyVisible, Is.True);
                controller.FitFloor(); yield return null;
                Assert.That(controller.Replay.Index, Is.EqualTo(index)); Assert.That(controller.Replay.DisplayPosition, Is.EqualTo(position));
                var captured = new System.Collections.Generic.HashSet<RunStepKind>();
                while (!controller.Replay.Complete)
                {
                    var current = controller.Replay.Current;
                    if ((current.Step.Kind == RunStepKind.Movement || current.HasMarker) && captured.Add(current.Step.Kind))
                    {
                        Assert.That(controller.World.ReplayPartyVisible, Is.EqualTo(current.Survivors > 0));
                        Assert.That(controller.World.ReplayMarkerVisible, Is.EqualTo(current.HasMarker));
                        yield return CompositionCapture("visual-replay-" + current.Step.Kind, size);
                    }
                    controller.ToggleReplayPause();
                    controller.Replay.Advance(current.Step.Kind == RunStepKind.Movement ? controller.presentationPolicy.ReplayMovementSeconds : controller.presentationPolicy.ReplayInteractionSeconds);
                    controller.ToggleReplayPause(); yield return null;
                }
                foreach (var kind in new[] { RunStepKind.MonsterEngagement, RunStepKind.TrapTrigger, RunStepKind.LootInteraction })
                    Assert.That(captured, Does.Contain(kind));
                Assert.That(controller.StartReplay(), Is.True); controller.ToggleReplayPause();
                controller.EnterEdit(); Assert.That(controller.Replay.Paused, Is.True); Assert.That(controller.World.ReplayPartyVisible, Is.False);
                Assert.That(controller.Discard(), Is.True); Assert.That(controller.Replay.Paused, Is.True);
                controller.ToggleReplayPause(); controller.Replay.Advance(float.MaxValue); yield return null;
                Assert.That(controller.Replay.Complete, Is.True);
                // Public controls refresh completion when a normal frame advances. Pause/resume is disabled there.
                controller.ToggleReplayPause(); yield return CompositionCapture("visual-replay-complete", size);
                controller.CloseReplay(); Assert.That(controller.World.ReplayPartyVisible, Is.False);
                Assert.That(controller.World.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(views), "Replay reuses its two views");
                CollectionAssert.AreEqual(bytes, File.ReadAllBytes(root.SaveService.SavePath));
                Assert.That(JsonUtility.ToJson(root.Save), Is.EqualTo(live));
            }
        }
    }
}
#endif
