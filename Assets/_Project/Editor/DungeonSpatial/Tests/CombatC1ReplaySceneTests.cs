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
            var sprites=controller.World.GetComponentsInChildren<SpriteRenderer>(true).Where(v=>v.name=="Content").ToDictionary(v=>v,v=>v.sprite);
            int pooled=-1,pooledLabels=-1;
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(720,1280)})
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x,size.y);
                Assert.That(controller.StartReplay(),Is.True); controller.ToggleReplayPause();
                var replay=controller.Replay; var ui=controller.GetComponent<UIDocument>().rootVisualElement;
                while(replay.Current.Combat?.Kind!=CombatC0EventKind.Attack) controller.NextReplay();
                yield return null;
                Assert.That(controller.World.ReplayDefenderCount,Is.EqualTo(2)); Assert.That(controller.World.ReplayAttackVisible,Is.True);
                Assert.That(controller.World.GetComponentsInChildren<SpriteRenderer>(true).Count(v=>v.name=="RecordedCorridorContent"),Is.Zero,"Reuse both canonical defenders");
                foreach(var sprite in sprites) { Assert.That(sprite.Key.sprite,Is.SameAs(sprite.Value)); Assert.That(sprite.Key.color,Is.EqualTo(Color.white)); }
                Assert.That(ui.Q<Label>("replayEvent").text,Does.Contain("Round").And.Contain("damage"));
                Assert.That(ui.Q<Label>("replayActors").text,Does.Contain("M1").And.Contain("M2").And.Contain("HP"));
                int index=replay.Index; float elapsed=replay.Elapsed;
                yield return null; yield return null;
                Assert.That(replay.Index,Is.EqualTo(index)); Assert.That(replay.Elapsed,Is.EqualTo(elapsed));
                foreach(var text in new[]{DungeonTextSize.Small,DungeonTextSize.Default,DungeonTextSize.Large})
                {
                    controller.SetTextSize(text); for(int i=0;i<5;i++) yield return null;
                    foreach(var name in new[]{"pauseReplay","closeReplay","nextReplay","edit"}) UatSafeAction(ui.Q<Button>(name));
                    AssertCombatMapReadability(ui);
                    yield return CompositionCapture("combat-c1-readable-attack-"+text,size);
                }
                // The ordinary viewport controls remain presentation-only during a paused attack.
                controller.Viewport.Pan(new Vector2(20,10)); controller.Viewport.Zoom(1.2f,controller.Viewport.ScreenRect.center); controller.FitFloor();
                Assert.That(replay.Index,Is.EqualTo(index));
                controller.ToggleReplayPause(); yield return null; Assert.That(replay.Elapsed,Is.GreaterThan(elapsed)); controller.ToggleReplayPause();
                while(!replay.Complete) controller.NextReplay(); yield return null;
                Assert.That(ui.Q<Label>("replayActors").text,Does.Contain("defeated"));
                Assert.That(ui.Q<Label>("replayDamage").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
                if(size.x==720) yield return CompositionCapture("combat-c1-readable-completed",size);
                int count=controller.World.GetComponentsInChildren<Renderer>(true).Length;
                if(pooled<0) pooled=count; else Assert.That(count,Is.EqualTo(pooled));
                int labels=ui.Query<Label>(className:"replay-actor").ToList().Count;
                if(pooledLabels<0) pooledLabels=labels; else Assert.That(labels,Is.EqualTo(pooledLabels));
                controller.CloseReplay(); Assert.That(controller.World.ReplayAttackVisible,Is.False); Assert.That(controller.World.ReplayPartyVisible,Is.False);
                Assert.That(ui.Query<Label>(className:"replay-actor").ToList().All(l=>l.style.display==DisplayStyle.None),Is.True);
                CollectionAssert.AreEqual(bytes,File.ReadAllBytes(root.SaveService.SavePath)); Assert.That(JsonUtility.ToJson(root.Save),Is.EqualTo(live));
            }
        }

        [UnityTest]
        public IEnumerator CombatC1EditSuspensionLayoutInvalidationAndPinnedRun()
        {
            var root=GameRoot.Instance; root.Save.structureRuntime.ManaReserve=1000;
            Assert.That(root.SimulateRunOnce(),Is.True); Assert.That(controller.StartReplay(),Is.True);
            var replay=controller.Replay;
            while(replay.Current.Combat?.Kind!=CombatC0EventKind.Attack) controller.NextReplay();
            yield return null;
            controller.EnterEdit(); Assert.That(replay.Paused,Is.True); Assert.That(controller.World.ReplayPartyVisible,Is.False);
            Assert.That(controller.GetComponent<UIDocument>().rootVisualElement.Q<Label>("replayDamage").style.display,Is.EqualTo(new StyleEnum<DisplayStyle>(DisplayStyle.None)));
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
            var replay=controller.Replay;
            while(replay.Current.Combat?.Kind!=CombatC0EventKind.Attack) controller.NextReplay();
            for(int i=0;i<3;i++) yield return null;
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            AssertCombatMapReadability(ui); int cursor=replay.Index;
            controller.SelectFloor(other); yield return null;
            Assert.That(replay.Index,Is.EqualTo(cursor)); Assert.That(controller.World.ReplayPartyVisible,Is.False); Assert.That(controller.World.ReplayDefenderCount,Is.Zero);
            Assert.That(ui.Q<Label>("replayDamage").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
            Assert.That(ui.Query<Label>(className:"replay-actor").ToList().All(l=>l.style.display==DisplayStyle.None),Is.True);
            controller.SelectFloor(root.Save.validatedCanonicalSpatialState.Floors[0].FloorInstanceId); yield return null;
            for(int i=0;i<3;i++) yield return null;
            Assert.That(controller.World.ReplayDefenderCount,Is.GreaterThan(0)); AssertCombatMapReadability(ui); controller.CloseReplay();
        }

        private void AssertCombatMapReadability(VisualElement ui)
        {
            var replay=controller.Replay; var e=replay.Current.Combat; var damage=ui.Q<Label>("replayDamage");
            var actor=replay.Combat.Actors[replay.Current.TargetIndex];
            string alias=string.Format(actor.IsMonster?"M{0}":"A{0}",actor.Ordinal+1);
            var root=GameRoot.Instance;
            string key=e.Defeated?"ui.dungeon.replay.combat_fallen":e.Damage==0?"ui.dungeon.replay.combat_zero":"ui.dungeon.replay.combat_hit";
            Assert.That(damage.resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
            Assert.That(damage.text,Is.EqualTo(string.Format(root.Content.GetString(key,key),alias,e.Damage,e.HealthAfter)));
            var camera=controller.GetComponentInChildren<Camera>(); var document=controller.GetComponent<UIDocument>();
            foreach(var a in replay.Combat.Actors.Select((value,index)=>(value,index)))
            {
                if(!controller.World.TryReplayCombatantBounds(replay,a.index,out var bounds)) continue;
                Vector3 min=camera.WorldToScreenPoint(bounds.min),max=camera.WorldToScreenPoint(bounds.max);
                var body=Rect.MinMaxRect(min.x/document.panelSettings.scale,(Screen.height-max.y)/document.panelSettings.scale,
                    max.x/document.panelSettings.scale,(Screen.height-min.y)/document.panelSettings.scale);
                Assert.That(damage.worldBound.Overlaps(body),Is.False,"Damage must not cover combat art");
                foreach(var tag in ui.Query<Label>(className:"replay-actor").ToList().Where(l=>l.resolvedStyle.display==DisplayStyle.Flex))
                { Assert.That(tag.worldBound.Overlaps(body),Is.False,"Alias must not cover combat art"); Assert.That(tag.resolvedStyle.whiteSpace,Is.EqualTo(WhiteSpace.NoWrap)); }
            }
            foreach(var a in replay.Combat.Actors.Where(a=>a.IsMonster&&a.RoomId==replay.Current.Step.RoomId))
                Assert.That(ui.Query<Label>(className:"replay-actor").ToList().Any(l=>Equals(l.userData,a.Id)&&l.resolvedStyle.display==DisplayStyle.Flex),Is.True,"Defenders retain distinct chips");
        }

        [UnityTest]
        public IEnumerator CombatC1TargetFeedbackTracksRecordedHitsAndDefeats()
        {
            var root=GameRoot.Instance; root.Save.structureRuntime.ManaReserve=1000;
            var service=(RunSimulationService)typeof(GameRoot).GetField("_runSimulationService",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(root);
            bool monsterDefeat=false,memberDefeat=false,zero=false;
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            foreach(int scenario in new[]{0,1,2})
            {
                if(scenario>0) foreach(var m in service.Config.CombatC0.Monsters) { m.Damage=scenario==1?100:1; m.Initiative=100; m.MaxHealth=100; }
                if(scenario==2) foreach(var c in service.Config.CombatC0.Classes) c.Damage=0;
                Assert.That(root.SimulateRunOnce(),Is.True); var bytes=File.ReadAllBytes(root.SaveService.SavePath); var live=JsonUtility.ToJson(root.Save);
                Assert.That(controller.StartReplay(),Is.True); controller.ToggleReplayPause();
                while(!controller.Replay.Complete)
                {
                    var e=controller.Replay.Current.Combat;
                    if(e?.Kind==CombatC0EventKind.Attack)
                    {
                        for(int i=0;i<3;i++) yield return null;
                        AssertCombatMapReadability(ui);
                        bool monster=controller.Replay.Combat.Actors[controller.Replay.Current.TargetIndex].IsMonster;
                        monsterDefeat|=e.Defeated&&monster; memberDefeat|=e.Defeated&&!monster; zero|=e.Damage==0;
                        if(e.Defeated) yield return CompositionCapture(monster?"combat-c1-readable-monster-defeat":"combat-c1-readable-member-defeat",new Vector2Int(Screen.width,Screen.height));
                    }
                    controller.NextReplay();
                }
                yield return null;
                Assert.That(ui.Q<Label>("replayDamage").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
                controller.CloseReplay(); CollectionAssert.AreEqual(bytes,File.ReadAllBytes(root.SaveService.SavePath)); Assert.That(JsonUtility.ToJson(root.Save),Is.EqualTo(live));
            }
            Assert.That(monsterDefeat&&memberDefeat&&zero,Is.True);
        }
    }
}
#endif
