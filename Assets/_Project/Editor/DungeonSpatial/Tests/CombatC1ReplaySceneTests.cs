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
                for(int i=0;i<5;i++) yield return null;
                AssertRpgParty(ui);
                foreach(var a in replay.Combat.Actors.Where(a=>a.IsMonster)) Assert.That(a.At(replay.Index).Health,Is.EqualTo(a.MaxHealth));
                while(replay.Current.Combat?.Kind!=CombatC0EventKind.Attack) controller.NextReplay();
                yield return null;
                Assert.That(controller.World.ReplayDefenderCount,Is.EqualTo(2)); Assert.That(controller.World.ReplayAttackVisible,Is.True);
                Assert.That(controller.World.GetComponentsInChildren<SpriteRenderer>(true).Count(v=>v.name=="RecordedCorridorContent"),Is.Zero,"Reuse both canonical defenders");
                foreach(var sprite in sprites) { Assert.That(sprite.Key.sprite,Is.SameAs(sprite.Value)); Assert.That(sprite.Key.color,Is.EqualTo(Color.white)); }
                Assert.That(ui.Q<Label>("replayEvent").text,Does.Contain("Round").And.Contain("damage"));
                Assert.That(ui.Q<Label>("replayActors").text,Does.Contain("Skeleton").And.Contain("HP").And.Not.Contain("M1").And.Not.Contain("M2"));
                var monsters=replay.Combat.Actors.Where(a=>a.IsMonster).ToArray();
                Assert.That(monsters.Select(a=>a.Id),Is.Unique);
                Assert.That(monsters.Select(a=>a.At(replay.Index).Health).Distinct().Count(),Is.EqualTo(2),"Duplicate Skeletons keep separate HP");
                int index=replay.Index; float elapsed=replay.Elapsed;
                yield return null; yield return null;
                Assert.That(replay.Index,Is.EqualTo(index)); Assert.That(replay.Elapsed,Is.EqualTo(elapsed));
                foreach(var text in new[]{DungeonTextSize.Small,DungeonTextSize.Default,DungeonTextSize.Large})
                {
                    controller.SetTextSize(text); for(int i=0;i<5;i++) yield return null;
                    foreach(var name in new[]{"pauseReplay","closeReplay","nextReplay","edit"}) UatSafeAction(ui.Q<Button>(name));
                    AssertCombatMapReadability(ui);
                    yield return CompositionCapture("combat-c1-rpg-attack-"+text,size);
                }
                var number=ui.Q<Label>("replayDamage"); var heldText=number.text; var heldPosition=number.worldBound.position; float heldOpacity=number.resolvedStyle.opacity;
                yield return null; yield return null;
                Assert.That(number.text,Is.EqualTo(heldText)); Assert.That(number.worldBound.position,Is.EqualTo(heldPosition)); Assert.That(number.resolvedStyle.opacity,Is.EqualTo(heldOpacity));
                // The ordinary viewport controls remain presentation-only during a paused attack.
                var groupBefore=ui.Q("replayPartyInfo").worldBound.position;
                controller.Viewport.Pan(new Vector2(20,10)); controller.Viewport.Zoom(1.1f,controller.Viewport.ScreenRect.center);
                typeof(ProductionDungeonController).GetMethod("ApplyCamera",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(controller,null);
                for(int i=0;i<3;i++) yield return null;
                AssertRpgParty(ui); Assert.That(ui.Q("replayPartyInfo").worldBound.position,Is.Not.EqualTo(groupBefore));
                controller.FitFloor();
                Assert.That(replay.Index,Is.EqualTo(index));
                controller.ToggleReplayPause(); yield return null; Assert.That(replay.Elapsed,Is.GreaterThan(elapsed)); controller.ToggleReplayPause();
                while(!replay.Complete) controller.NextReplay(); yield return null;
                Assert.That(ui.Q<Label>("replayActors").text,Does.Contain("defeated"));
                for(int i=0;i<3;i++) yield return null;
                AssertRpgParty(ui);
                Assert.That(ui.Q<Label>("replayDamage").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
                if(size.x==720) yield return CompositionCapture("combat-c1-rpg-completed",size);
                int count=controller.World.GetComponentsInChildren<Renderer>(true).Length;
                if(pooled<0) pooled=count; else Assert.That(count,Is.EqualTo(pooled));
                int labels=ui.Query<Label>(className:"replay-actor").ToList().Count;
                if(pooledLabels<0) pooledLabels=labels; else Assert.That(labels,Is.EqualTo(pooledLabels));
                controller.CloseReplay(); Assert.That(controller.World.ReplayAttackVisible,Is.False); Assert.That(controller.World.ReplayPartyVisible,Is.False);
                Assert.That(ui.Query<Label>(className:"replay-actor").ToList().All(l=>l.style.display==DisplayStyle.None),Is.True);
                Assert.That(ui.Q("replayPartyInfo").style.display,Is.EqualTo(new StyleEnum<DisplayStyle>(DisplayStyle.None)));
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
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(720,1280);
            var root=GameRoot.Instance; root.Save.structureRuntime.ManaReserve=1000;
            root.Save.completedResearch=new CompletedResearchState { ProjectIds=new[]{"ac_100"} };
            Assert.That(root.SaveService.CommitFloorConstruction(root.Save,root.SaveService.PreviewFloorConstruction(root.Save)).IsSuccess,Is.True);
            string other=root.Save.validatedCanonicalSpatialState.Floors.Single(f=>f.FloorIndex==1).FloorInstanceId;
            Assert.That(root.SimulateRunOnce(),Is.True); Assert.That(controller.StartReplay(),Is.True); controller.ToggleReplayPause();
            controller.SelectFloor(root.Save.validatedCanonicalSpatialState.Floors[0].FloorInstanceId);
            var replay=controller.Replay;
            while(replay.Current.Combat?.Kind!=CombatC0EventKind.Attack) controller.NextReplay();
            for(int i=0;i<3;i++) yield return null;
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            AssertCombatMapReadability(ui); int cursor=replay.Index;
            controller.SelectFloor(other); yield return null;
            Assert.That(replay.Index,Is.EqualTo(cursor)); Assert.That(controller.World.ReplayPartyVisible,Is.False); Assert.That(controller.World.ReplayDefenderCount,Is.Zero);
            Assert.That(ui.Q<Label>("replayDamage").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
            Assert.That(ui.Q("replayPartyInfo").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
            Assert.That(ui.Query<Label>(className:"replay-actor").ToList().All(l=>l.style.display==DisplayStyle.None),Is.True);
            controller.SelectFloor(root.Save.validatedCanonicalSpatialState.Floors[0].FloorInstanceId); yield return null;
            for(int i=0;i<3;i++) yield return null;
            Assert.That(controller.World.ReplayDefenderCount,Is.GreaterThan(0)); AssertCombatMapReadability(ui); controller.CloseReplay();
        }

        private void AssertCombatMapReadability(VisualElement ui)
        {
            var replay=controller.Replay; var e=replay.Current.Combat; var damage=ui.Q<Label>("replayDamage");
            var actor=replay.Combat.Actors[replay.Current.TargetIndex];
            var root=GameRoot.Instance;
            string key=e.Defeated?"ui.dungeon.replay.damage_defeat":e.Damage==0?"ui.dungeon.replay.damage_zero":"ui.dungeon.replay.damage_number";
            Assert.That(damage.resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
            Assert.That(damage.text,Is.EqualTo(string.Format(root.Content.GetString(key,key),e.Damage)));
            Assert.That(damage.resolvedStyle.backgroundColor.a,Is.Zero,"Damage has no opaque panel");
            if(e.Damage>0) Assert.That(damage.resolvedStyle.color.r,Is.GreaterThan(damage.resolvedStyle.color.g));
            AssertRpgParty(ui);
            var camera=controller.GetComponentInChildren<Camera>(); var document=controller.GetComponent<UIDocument>();
            foreach(var a in replay.Combat.Actors.Select((value,index)=>(value,index)))
            {
                if(!controller.World.TryReplayCombatantBounds(replay,a.index,out var bounds)) continue;
                Vector3 min=camera.WorldToScreenPoint(bounds.min),max=camera.WorldToScreenPoint(bounds.max);
                var body=Rect.MinMaxRect(min.x/document.panelSettings.scale,(Screen.height-max.y)/document.panelSettings.scale,
                    max.x/document.panelSettings.scale,(Screen.height-min.y)/document.panelSettings.scale);
                Assert.That(damage.worldBound.Overlaps(body),Is.False,"Damage must not cover combat art");
                foreach(var tag in ui.Query<Label>(className:"replay-actor").ToList().Where(l=>l.resolvedStyle.display==DisplayStyle.Flex))
                { Assert.That(tag.worldBound.Overlaps(body),Is.False,"Name and HP must not cover combat art"); Assert.That(tag.resolvedStyle.whiteSpace,Is.EqualTo(WhiteSpace.NoWrap)); Assert.That(tag.resolvedStyle.backgroundColor.a,Is.Zero); }
            }
            foreach(var a in replay.Combat.Actors.Where(a=>a.IsMonster&&a.RoomId==replay.Current.Step.RoomId))
            {
                var tag=ui.Query<Label>(className:"replay-actor").ToList().Single(l=>Equals(l.userData,a.Id));
                Assert.That(tag.resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex),"Every defender retains its own name and HP");
                Assert.That(tag.text,Is.EqualTo(string.Format(root.Content.GetString("ui.dungeon.replay.monster_hp",null),
                    root.Content.GetString(a.ContentId+".display_name",null),a.At(replay.Index).Health,a.MaxHealth)));
            }
        }

        private void AssertRpgParty(VisualElement ui)
        {
            var replay=controller.Replay; var group=ui.Q("replayPartyInfo");
            controller.World.TryReplayLocationBounds(replay,out var location);
            Assert.That(group.resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex),"Party roster must be usable: "+replay.Current.Step.Kind+" "+replay.DisplayPosition+" area="+location+" viewport="+controller.Viewport.ScreenRect);
            // A pooled row's parent is hidden when the next captured party is smaller.
            var rows=group.Query<Label>(className:"replay-party-row").ToList().Where(l=>l.parent.resolvedStyle.display==DisplayStyle.Flex).ToArray();
            var members=replay.Combat.Actors.Where(a=>!a.IsMonster).ToArray(); Assert.That(rows.Length,Is.EqualTo(members.Length));
            var root=GameRoot.Instance;
            foreach(var member in members)
            {
                var row=rows.Single(l=>Equals(l.userData,member.Id));
                var name=AdventurerPartyCompositionResolver.ResolveClassLabel(member.ContentId,root.Content.GetString);
                Assert.That(row.tooltip,Is.EqualTo(string.Format(root.Content.GetString("ui.dungeon.replay.party_row",null),name,member.At(replay.Index).Health,member.MaxHealth)));
                var hp=row.parent.Q<Label>(className:"replay-party-health");
                Assert.That(hp.text,Is.EqualTo(string.Format(root.Content.GetString("ui.dungeon.replay.party_health",null),member.At(replay.Index).Health,member.MaxHealth)));
                Assert.That(group.worldBound.Contains(hp.worldBound.min),Is.True);
                Assert.That(row.enableRichText,Is.False); Assert.That(group.worldBound.Contains(row.worldBound.min),Is.True);
            }
            var camera=controller.GetComponentInChildren<Camera>(); float scale=controller.GetComponent<UIDocument>().panelSettings.scale;
            foreach(var region in controller.World.ReplayOccupiedRegions)
            {
                var a=camera.WorldToScreenPoint(new Vector3(region.xMin,region.yMin)); var b=camera.WorldToScreenPoint(new Vector3(region.xMax,region.yMax));
                var area=Rect.MinMaxRect(a.x/scale,(Screen.height-b.y)/scale,b.x/scale,(Screen.height-a.y)/scale);
                Assert.That(group.worldBound.Overlaps(area),Is.False,"Party info must stay outside rooms and corridors");
            }
        }

        [UnityTest]
        public IEnumerator CombatC1TargetFeedbackTracksRecordedHitsAndDefeats()
        {
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(720,1280);
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
                        if(e.Defeated) yield return CompositionCapture(monster?"combat-c1-rpg-monster-defeat":"combat-c1-rpg-member-defeat",new Vector2Int(Screen.width,Screen.height));
                    }
                    controller.NextReplay();
                }
                yield return null;
                Assert.That(ui.Q<Label>("replayDamage").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
                controller.CloseReplay(); CollectionAssert.AreEqual(bytes,File.ReadAllBytes(root.SaveService.SavePath)); Assert.That(JsonUtility.ToJson(root.Save),Is.EqualTo(live));
            }
            Assert.That(monsterDefeat&&memberDefeat&&zero,Is.True);
        }

        [UnityTest]
        public IEnumerator CombatC1PartyFollowsRoomsCorridorsAndFloors()
        {
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(1920,1080);
            var root=GameRoot.Instance; root.Save.structureRuntime.ManaReserve=10000;
            root.Save.completedResearch=new CompletedResearchState { ProjectIds=new[]{BasicBranchingResearchAuthority.ResearchId,"ac_100"} };
            var service=(RunSimulationService)typeof(GameRoot).GetField("_runSimulationService",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(root);
            foreach(var c in service.Config.CombatC0.Classes) c.Damage=100;
            var decision=service.Config.PhaseFiveB.BranchDecision; decision.SkipThreshold=-1; decision.EnterThreshold=-.9;
            foreach(var profile in decision.ProfileMinimums) profile.MinimumSurvivability=0;
            service.Config.BaseSuccessChance=1; service.Config.SuccessThreshold=0; service.Config.HeatPenaltyPerPoint=0; service.Config.ManaReserveBonusPerPoint=0;
            service.Config.PhaseSix.ExitThreshold=-1; service.Config.PhaseSix.DescendThreshold=-.9;
            foreach(var profile in service.Config.PhaseSix.ProfileMinimums) profile.MinimumSurvivability=0;
            var floor=root.Save.validatedCanonicalSpatialState.Floors[0]; var origin=floor.Layout.Nodes.Single(n=>n.Kind==FloorRouteNodeKind.Room);
            var preview=root.SaveService.PreviewOptionalBranchConstruction(new OptionalBranchConstructionRequest {
                FloorInstanceId=floor.FloorInstanceId,OriginNodeId=origin.NodeId,OriginConnectionPointId="east",CorridorLength=2 },root.Save.completedResearch,root.Save);
            Assert.That(preview.IsCommittable,Is.True,string.Join(",",preview.ReasonCodes));
            Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save,DetachedCanonicalMutationRequest.ConstructBranch(preview)).IsSuccess,Is.True);
            floor=root.Save.validatedCanonicalSpatialState.Floors[0]; var edge=floor.Layout.Edges.Single(e=>e.Classification==RouteClassification.Optional);
            var tiles=edge.Footprint.OccupiedTiles.OrderBy(t=>t.X).ToArray();
            Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save,DetachedCanonicalMutationRequest.PlaceCorridor("placement.category.trap","placement.option.trap.spike",floor.FloorInstanceId,edge.OptionalBranchId,tiles[0])).IsSuccess,Is.True);
            Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save,DetachedCanonicalMutationRequest.PlaceCorridor("placement.category.loot_node","placement.option.loot_node.hidden_cache",floor.FloorInstanceId,edge.OptionalBranchId,tiles[1])).IsSuccess,Is.True);
            Assert.That(root.SaveService.CommitFloorConstruction(root.Save,root.SaveService.PreviewFloorConstruction(root.Save)).IsSuccess,Is.True);
            var other=root.Save.validatedCanonicalSpatialState.Floors.Single(f=>f.FloorIndex==1);
            var construction=root.SaveService.PreviewStructuralConstruction(new StructuralConstructionRequest { FloorInstanceId=other.FloorInstanceId,
                RoomDefinitionId="spatial.room.basic",Anchor=new TileCoordinate(1,2),Orientation=CardinalOrientation.Zero,TerminalConnectionPointId="east" });
            Assert.That(construction.IsValid,Is.True,string.Join(",",construction.ReasonCodes));
            Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save,DetachedCanonicalMutationRequest.Construct(construction)).IsSuccess,Is.True);
            other=root.Save.validatedCanonicalSpatialState.Floors.Single(f=>f.FloorIndex==1); var room=other.Layout.Rooms.Single();
            Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save,DetachedCanonicalMutationRequest.Place("placement.category.monster","placement.option.monster.skeleton",room.RoomInstanceId,other.FloorInstanceId,new TileCoordinate(0,0))).IsSuccess,Is.True);
            Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save,DetachedCanonicalMutationRequest.Place("placement.category.loot_node","placement.option.loot_node.hidden_cache",room.RoomInstanceId,other.FloorInstanceId,new TileCoordinate(1,1))).IsSuccess,Is.True);
            Assert.That(root.SaveService.CommitFloorLifecycle(root.Save,FloorLifecycleAction.Activate,other.FloorInstanceId).IsSuccess,Is.True);
            Assert.That(root.SimulateRunOnce(),Is.True);
            Assert.That(root.Save.runHistory.LatestOutcome.PresentationSteps.Any(s=>s.Kind==RunStepKind.FloorTransfer),Is.True);
            var bytes=File.ReadAllBytes(root.SaveService.SavePath); var live=JsonUtility.ToJson(root.Save);
            Assert.That(controller.StartReplay(),Is.True); controller.ToggleReplayPause();
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            var floors=new System.Collections.Generic.HashSet<string>(); var positions=new System.Collections.Generic.HashSet<Vector2>(); bool corridor=false,captured=false;
            while(true)
            {
                var frame=controller.Replay.Current;
                if(frame.HasPosition)
                {
                    if(controller.World.FloorInstanceId!=frame.FloorId) controller.SelectFloor(frame.FloorId);
                    for(int i=0;i<4;i++) yield return null;
                    AssertRpgParty(ui); floors.Add(frame.FloorId); positions.Add(ui.Q("replayPartyInfo").worldBound.position);
                    bool onCorridorTrap=frame.Step.PositionSpace==RunPositionSpace.Floor&&frame.Step.Kind==RunStepKind.TrapTrigger;
                    corridor|=onCorridorTrap;
                    if(onCorridorTrap&&!captured)
                    {
                        Assert.That(frame.Step.Position,Is.EqualTo(tiles[0]),"Capture the actual assigned corridor trap, not initial Floor-space movement inside a room");
                        Assert.That(controller.Replay.DisplayPosition,Is.EqualTo(new Vector2(tiles[0].X+.5f,tiles[0].Y+.5f)));
                        yield return CompositionCapture("combat-c1-rpg-corridor",new Vector2Int(1920,1080)); captured=true;
                    }
                }
                else { yield return null; Assert.That(ui.Q("replayPartyInfo").resolvedStyle.display,Is.EqualTo(DisplayStyle.None)); }
                if(controller.Replay.Complete) break;
                controller.NextReplay();
            }
            Assert.That(floors.Count,Is.EqualTo(2)); Assert.That(corridor,Is.True); Assert.That(positions.Count,Is.GreaterThan(2));
            controller.CloseReplay(); CollectionAssert.AreEqual(bytes,File.ReadAllBytes(root.SaveService.SavePath)); Assert.That(JsonUtility.ToJson(root.Save),Is.EqualTo(live));
        }

        [UnityTest]
        public IEnumerator CombatC1LocalizedNamesKeepHealthVisibleWithoutOverflow()
        {
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(720,1280);
            var root=GameRoot.Instance; root.Save.structureRuntime.ManaReserve=1000;
            var strings=(System.Collections.Generic.Dictionary<string,string>)typeof(ContentService).GetField("_stringMap",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(root.Content);
            const string longName="A very long localized name that must remain safely bounded in the dungeon view";
            strings["placement.option.monster.skeleton.display_name"]=longName;
            foreach(var id in new[]{"warrior","rogue","ranger","mage","cleric"}) strings["adventurer.class."+id+".display_name"]=longName;
            Assert.That(root.SimulateRunOnce(),Is.True); Assert.That(controller.StartReplay(),Is.True); controller.ToggleReplayPause();
            controller.SetTextSize(DungeonTextSize.Large);
            while(controller.Replay.Current.Combat?.Kind!=CombatC0EventKind.Attack) controller.NextReplay();
            for(int i=0;i<6;i++) yield return null;
            var ui=controller.GetComponent<UIDocument>().rootVisualElement; AssertCombatMapReadability(ui);
            foreach(var row in ui.Q("replayPartyInfo").Query<Label>(className:"replay-party-row").ToList())
            {
                Assert.That(row.text,Does.Contain(longName)); var hp=row.parent.Q<Label>(className:"replay-party-health");
                Assert.That(hp.worldBound.xMax,Is.LessThanOrEqualTo(row.parent.worldBound.xMax+.1f));
                Assert.That(row.resolvedStyle.textOverflow,Is.EqualTo(TextOverflow.Ellipsis));
            }
            yield return CompositionCapture("combat-c1-rpg-localized-long",new Vector2Int(720,1280));
            controller.CloseReplay();
        }
    }
}
#endif
