#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0.Tests
{
    public abstract partial class PhaseSevenA4ProductionSceneTests
    {
        [UnityTest]
        public IEnumerator CombatC1MultiDefenderPlaybackControlsAndResponsiveEvidence()
        {
            var root=GameRoot.Instance; root.Save.structureRuntime.ManaReserve=1000;
            var floor=root.Save.validatedCanonicalSpatialState.Floors[0]; var room=floor.Layout.Rooms[0];
            Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save,DetachedCanonicalMutationRequest.Place(
                "placement.category.monster","placement.option.monster.skeleton",room.RoomInstanceId,floor.FloorInstanceId,new TileCoordinate(0,3))).IsSuccess,Is.True);
            Assert.That(root.SimulateRunOnce(),Is.True); var outcome=root.Save.runHistory.LatestOutcome;
            var selected=root.Save.runHistory.RecentOutcomes.Last();
            Assert.That(CombatC0Diagnostics.Events(selected),Is.EqualTo(CombatC0Diagnostics.Events(outcome)));
            var bytes=File.ReadAllBytes(root.SaveService.SavePath); var live=JsonUtility.ToJson(root.Save);
            int pooled=-1;
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(720,1280)})
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x,size.y);
                Assert.That(controller.StartReplay(),Is.True); controller.ToggleReplayPause();
                var replay=controller.Replay; var ui=controller.GetComponent<UIDocument>().rootVisualElement;
                while(replay.Current.Combat?.Kind!=CombatC0EventKind.Attack) controller.NextReplay();
                yield return null;
                Assert.That(controller.World.ReplayDefenderCount,Is.EqualTo(2)); Assert.That(controller.World.ReplayAttackVisible,Is.True);
                Assert.That(controller.World.GetComponentsInChildren<SpriteRenderer>(true).Count(v=>v.name=="RecordedCorridorContent"),Is.Zero,"Reuse both canonical defenders");
                Assert.That(ui.Q<Label>("replayEvent").text,Does.Contain("Round").And.Contain("damage"));
                Assert.That(ui.Q<Label>("replayActors").text,Does.Contain("M1").And.Contain("M2").And.Contain("HP"));
                int index=replay.Index; float elapsed=replay.Elapsed;
                yield return null; yield return null;
                Assert.That(replay.Index,Is.EqualTo(index)); Assert.That(replay.Elapsed,Is.EqualTo(elapsed));
                foreach(var text in new[]{DungeonTextSize.Small,DungeonTextSize.Default,DungeonTextSize.Large})
                {
                    controller.SetTextSize(text); for(int i=0;i<5;i++) yield return null;
                    foreach(var name in new[]{"pauseReplay","closeReplay","nextReplay","edit"}) UatSafeAction(ui.Q<Button>(name));
                    yield return CompositionCapture("combat-c1-attack-"+text,size);
                }
                // The ordinary viewport controls remain presentation-only during a paused attack.
                controller.Viewport.Pan(new Vector2(20,10)); controller.Viewport.Zoom(1.2f,controller.Viewport.ScreenRect.center); controller.FitFloor();
                Assert.That(replay.Index,Is.EqualTo(index));
                controller.ToggleReplayPause(); yield return null; Assert.That(replay.Elapsed,Is.GreaterThan(elapsed)); controller.ToggleReplayPause();
                while(!replay.Complete) controller.NextReplay(); yield return null;
                Assert.That(ui.Q<Label>("replayActors").text,Does.Contain("defeated"));
                int count=controller.World.GetComponentsInChildren<Renderer>(true).Length;
                if(pooled<0) pooled=count; else Assert.That(count,Is.EqualTo(pooled));
                controller.CloseReplay(); Assert.That(controller.World.ReplayAttackVisible,Is.False); Assert.That(controller.World.ReplayPartyVisible,Is.False);
                CollectionAssert.AreEqual(bytes,File.ReadAllBytes(root.SaveService.SavePath)); Assert.That(JsonUtility.ToJson(root.Save),Is.EqualTo(live));
            }
        }

        [UnityTest]
        public IEnumerator CombatC1EditSuspensionLayoutInvalidationAndPinnedRun()
        {
            var root=GameRoot.Instance; root.Save.structureRuntime.ManaReserve=1000;
            Assert.That(root.SimulateRunOnce(),Is.True); Assert.That(controller.StartReplay(),Is.True);
            var replay=controller.Replay;
            controller.NextReplay(); controller.EnterEdit(); Assert.That(replay.Paused,Is.True); Assert.That(controller.World.ReplayPartyVisible,Is.False);
            Assert.That(controller.Discard(),Is.True); Assert.That(controller.Replay,Is.SameAs(replay)); Assert.That(replay.Paused,Is.True);
            var source=replay.Combat.Events.ToArray(); Assert.That(root.SimulateRunOnce(),Is.True);
            Assert.That(controller.Replay,Is.SameAs(replay)); CollectionAssert.AreEqual(source,replay.Combat.Events);
            var floor=root.Save.validatedCanonicalSpatialState.Floors[0]; var room=floor.Layout.Rooms[0];
            Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save,DetachedCanonicalMutationRequest.Place(
                "placement.category.trap","placement.option.trap.spike",room.RoomInstanceId,floor.FloorInstanceId,new TileCoordinate(2,1))).IsSuccess,Is.True);
            Assert.That(controller.Replay,Is.Null); Assert.That(replay.Closed,Is.True); Assert.That(controller.StartReplay(),Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CombatC1FloorSelectionDoesNotAnimateOffscreenOrInterpolateTransfers()
        {
            var root=GameRoot.Instance; root.Save.structureRuntime.ManaReserve=1000;
            root.Save.completedResearch=new CompletedResearchState { ProjectIds=new[]{"ac_100"} };
            Assert.That(root.SaveService.CommitFloorConstruction(root.Save,root.SaveService.PreviewFloorConstruction(root.Save)).IsSuccess,Is.True);
            string other=root.Save.validatedCanonicalSpatialState.Floors.Single(f=>f.FloorIndex==1).FloorInstanceId;
            Assert.That(root.SimulateRunOnce(),Is.True); Assert.That(controller.StartReplay(),Is.True); controller.ToggleReplayPause();
            var replay=controller.Replay; int cursor=replay.Index;
            controller.SelectFloor(other); yield return null;
            Assert.That(replay.Index,Is.EqualTo(cursor)); Assert.That(controller.World.ReplayPartyVisible,Is.False); Assert.That(controller.World.ReplayDefenderCount,Is.Zero);
            controller.SelectFloor(root.Save.validatedCanonicalSpatialState.Floors[0].FloorInstanceId); yield return null;
            Assert.That(controller.World.ReplayDefenderCount,Is.GreaterThan(0)); controller.CloseReplay();
        }
    }
}
#endif
