#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0.Tests
{
    public abstract partial class PhaseSevenA4ProductionSceneTests
    {
        private TileCoordinate ContentCell(RoomSpatialInstance room, int x, int y)
        {
            var definition = GameRoot.Instance.ProductionSpatialContent.Catalog.Rooms.Single(r => r.RoomDefinitionId == room.RoomDefinitionId);
            Assert.That(RoomLocalCoordinateTransform.TryToFloor(new TileCoordinate(x,y), definition.GrossFootprint, room.Anchor, room.Orientation, out var cell), Is.True);
            return cell;
        }
        private IEnumerator SettleContentPanel()
        { for (int i=0;i<12;i++) yield return null; }
        private void AssertCompactContentPanel(string state, bool compact = true)
        {
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            var details=ui.Q<ScrollView>("contextDetails"); var actions=ui.Q("contentPlacementActions").worldBound;
            var session=ui.Q("sessionActions").worldBound; var panel=ui.Q("bottomChrome").worldBound;
            var infoLabel=ui.Q<Label>("contentPlacementInfo"); var capacityLabel=ui.Q<Label>("contentCapacity");
            var information=infoLabel.worldBound; var capacity=capacityLabel.worldBound;
            float natural=information.yMax-capacity.yMin+capacityLabel.resolvedStyle.marginTop+infoLabel.resolvedStyle.marginBottom;
            float blank=details.contentViewport.worldBound.height-natural;
            TestContext.WriteLine("COMPACT "+Screen.width+"x"+Screen.height+" "+controller.TextSize+" "+state+
                " panel="+panel.height+" natural="+natural+" details="+details.contentViewport.worldBound.height+
                " blank="+blank+" map="+ui.Q("viewport").worldBound.height+" safe="+controller.SafeRoot.worldBound.height+
                " line="+capacity.height+" top="+ui.Q("topChrome").worldBound.height);
            Assert.That(details.contentViewport.worldBound.height,Is.GreaterThan(0),state);
            Assert.That(details.contentViewport.worldBound.height,Is.GreaterThanOrEqualTo(capacity.height*2+
                capacityLabel.resolvedStyle.marginTop+infoLabel.resolvedStyle.marginBottom),state+" two complete information lines");
            Assert.That(details.contentViewport.worldBound.yMax,Is.LessThanOrEqualTo(actions.yMin+.5f),state);
            Assert.That(actions.yMax,Is.LessThanOrEqualTo(session.yMin+.5f),state);
            Assert.That(ui.Q<Button>("cancelContent").worldBound.xMax,Is.LessThanOrEqualTo(ui.Q<Button>("confirmContent").worldBound.xMin+.5f),state);
            foreach(string id in new[] {"cancelContent","confirmContent","save","discard"}) AssertReachable(ui.Q<Button>(id));
            Assert.That(blank,Is.LessThanOrEqualTo(2),state+" no unused information region");
            if(compact)
            {
                if(details.contentViewport.worldBound.height<natural-.5f)
                    Assert.That(details.verticalScroller.highValue,Is.GreaterThan(0),state+" bounded information remains scrollable");
            }
            Assert.That(ui.Q("viewport").worldBound.height,Is.GreaterThanOrEqualTo(controller.SafeRoot.worldBound.height*.25f),state+" at least one quarter remains usable map");
        }
        private void AssertLootRoomCount(RoomSpatialInstance room, int expected)
        {
            controller.TapWorld(ContentCell(room,3,3)); Assert.That(controller.OpenContentCategory("placement.category.loot_node"),Is.True);
            var state=controller.Draft.ReadModel; var floor=state.Floors.Single(f=>f.Layout.Rooms.Any(r=>r.RoomInstanceId==room.RoomInstanceId));
            Assert.That(floor.RoomContents.Assignments.Count(a=>a.RoomInstanceId==room.RoomInstanceId && a.CategoryId=="placement.category.loot_node"),Is.EqualTo(expected));
            var label=controller.GetComponent<UIDocument>().rootVisualElement.Q<Label>("contentCapacity").text;
            Assert.That(label,Does.Contain(expected+" / 2"));
            TestContext.WriteLine("LOOT COUNT "+room.RoomInstanceId+"="+expected+"/2 UI="+label);
        }
        private IEnumerator PrepareCanonicalLootRooms()
        {
            var root=GameRoot.Instance; Assert.That(root.SaveService.SetQaMana(root.Save,true).IsSuccess,Is.True);
            yield return ConstructReassignmentDestination(); Assert.That(controller.Commit(),Is.True); yield return SettleContentPanel();
        }
        private void PlaceLootFixture(RoomSpatialInstance room, string option, int x)
        {
            var root=GameRoot.Instance; var floor=root.Save.validatedCanonicalSpatialState.Floors.Single(f=>f.Layout.Rooms.Any(r=>r.RoomInstanceId==room.RoomInstanceId));
            Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save,DetachedCanonicalMutationRequest.Place("placement.category.loot_node",option,
                room.RoomInstanceId,floor.FloorInstanceId,new TileCoordinate(x,1))).IsSuccess,Is.True);
        }
        private IEnumerator SequentialLootReassignment(bool saveBetween)
        {
            yield return PrepareCanonicalLootRooms(); var root=GameRoot.Instance;
            var rooms=root.Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms; var a=rooms[0]; var b=rooms[1];
            PlaceLootFixture(a,"placement.option.loot_node.basic",1); PlaceLootFixture(a,"placement.option.loot_node.hidden_cache",2);
            PlaceLootFixture(b,"placement.option.loot_node.glittering_hoard",1); yield return SettleContentPanel();
            var original=root.Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments.Where(x=>x.CategoryId=="placement.category.loot_node").ToArray();
            var hidden=original.Single(x=>x.OptionId=="placement.option.loot_node.hidden_cache"); var glitter=original.Single(x=>x.OptionId=="placement.option.loot_node.glittering_hoard");
            var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            controller.EnterEdit(); AssertLootRoomCount(a,2); AssertLootRoomCount(b,1);
            controller.TapWorld(ContentCell(a,2,1)); controller.BeginContentReassignment(); controller.TapWorld(ContentCell(b,2,1));
            Assert.That(controller.ContentPreview.IsValid,Is.True,controller.ContentPreview.Reason); Assert.That(controller.Draft.CommandCount,Is.Zero);
            Assert.That(controller.ConfirmContentPlacement(),Is.True); yield return SettleContentPanel();
            AssertLootRoomCount(a,1); AssertLootRoomCount(b,2); AssertCanonicalUnchanged(before,mana);
            AssertReassignedIdentity(controller.Draft.ReadModel.Floors[0].RoomContents.Assignments.Single(x=>x.AssignmentId==hidden.AssignmentId),hidden,b.RoomInstanceId,new TileCoordinate(2,1));
            if(saveBetween)
            {
                Assert.That(controller.Commit(),Is.True); yield return SettleContentPanel(); controller.EnterEdit();
                AssertLootRoomCount(a,1); AssertLootRoomCount(b,2); Assert.That(controller.Discard(),Is.True);
                yield return RestartShell(); root=GameRoot.Instance; controller.EnterEdit(); AssertLootRoomCount(a,1); AssertLootRoomCount(b,2);
                before=File.ReadAllBytes(root.SaveService.SavePath); Assert.That(root.Save.structureRuntime.ManaReserve,Is.EqualTo(mana));
            }
            int commands=controller.Draft.CommandCount;
            controller.TapWorld(ContentCell(b,1,1)); controller.BeginContentReassignment(); controller.TapWorld(ContentCell(a,2,1));
            Assert.That(controller.SelectedRoomInstanceId,Is.EqualTo(a.RoomInstanceId)); Assert.That(controller.ContentPreview.IsValid,Is.True,controller.ContentPreview.Reason);
            Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands)); AssertCanonicalUnchanged(before,mana);
            Assert.That(controller.ConfirmContentPlacement(),Is.True); yield return SettleContentPanel();
            AssertLootRoomCount(a,2); AssertLootRoomCount(b,1);
            var quote=root.SaveService.PreviewDungeonDraft(root.Save,controller.Draft); Assert.That(quote.ContentAcquisitions,Is.Zero); Assert.That(quote.ContentAcquisitionCost,Is.Zero);
            AssertReassignedIdentity(controller.Draft.ReadModel.Floors[0].RoomContents.Assignments.Single(x=>x.AssignmentId==glitter.AssignmentId),glitter,a.RoomInstanceId,new TileCoordinate(2,1));
            Assert.That(controller.Draft.ReadModel.LifecycleAndOwnership.ReturnedContents,Is.Empty); AssertCanonicalUnchanged(before,mana);
            if(!saveBetween) yield return RoomContentCapture("loot-reassignment-sequential.png");
            yield return RestartShell(); root=GameRoot.Instance; var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            Click(ui.Q<Button>("confirm")); yield return SettleContentPanel(); AssertLootRoomCount(a,2); AssertLootRoomCount(b,1);
            Assert.That(controller.Commit(),Is.True); yield return SettleContentPanel(); controller.EnterEdit();
            AssertLootRoomCount(a,2); AssertLootRoomCount(b,1); Assert.That(controller.Discard(),Is.True);
            yield return RestartShell(); root=GameRoot.Instance; controller.EnterEdit(); AssertLootRoomCount(a,2); AssertLootRoomCount(b,1);
            var saved=root.Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments;
            AssertReassignedIdentity(saved.Single(x=>x.AssignmentId==hidden.AssignmentId),hidden,b.RoomInstanceId,new TileCoordinate(2,1));
            AssertReassignedIdentity(saved.Single(x=>x.AssignmentId==glitter.AssignmentId),glitter,a.RoomInstanceId,new TileCoordinate(2,1));
            Assert.That(root.Save.structureRuntime.ManaReserve,Is.EqualTo(mana)); Assert.That(controller.Discard(),Is.True);
            TestContext.WriteLine("SEQUENTIAL LOOT saveBetween="+saveBetween+" A2/B1 -> A1/B2 -> A2/B1; identities, wallet, recovery and reopening preserved");
        }
        [UnityTest] public IEnumerator RoomContentSequentialLootDraft() { yield return SequentialLootReassignment(false); }
        [UnityTest] public IEnumerator RoomContentSequentialLootSaveBetween() { yield return SequentialLootReassignment(true); }
        [UnityTest] public IEnumerator RoomContentLootRejectionReasons()
        {
            yield return PrepareCanonicalLootRooms(); var root=GameRoot.Instance; var rooms=root.Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms;
            var a=rooms[0]; var b=rooms[1]; PlaceLootFixture(a,"placement.option.loot_node.glittering_hoard",1);
            PlaceLootFixture(b,"placement.option.loot_node.glittering_hoard",1);
            yield return SettleContentPanel(); var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            controller.EnterEdit(); AssertLootRoomCount(a,1); AssertLootRoomCount(b,1);
            controller.TapWorld(ContentCell(a,1,1)); controller.BeginContentReassignment(); controller.TapWorld(ContentCell(b,2,1));
            Assert.That(controller.ContentPreview.IsValid,Is.True,"Owner-approved matching option at 1/2 is eligible");
            Assert.That(controller.Draft.CommandCount,Is.Zero); Assert.That(controller.ConfirmContentPlacement(),Is.True); yield return SettleContentPanel();
            AssertLootRoomCount(a,0); AssertLootRoomCount(b,2); AssertCanonicalUnchanged(before,mana);
            var info=controller.GetComponent<UIDocument>().rootVisualElement.Q<Label>("contentPlacementInfo").text;
            Assert.That(root.SaveService.PreviewDungeonDraft(root.Save,controller.Draft).ContentAcquisitionCost,Is.Zero); controller.CancelContentPlacement();
            Assert.That(controller.Discard(),Is.True); AssertCanonicalUnchanged(before,mana);
            // Prepare the separate full-room/collision fixture outside Edit Mode.
            PlaceLootFixture(a,"placement.option.loot_node.hidden_cache",2); yield return SettleContentPanel();
            before=File.ReadAllBytes(root.SaveService.SavePath); mana=root.Save.structureRuntime.ManaReserve;
            controller.EnterEdit(); AssertLootRoomCount(a,2); AssertLootRoomCount(b,1);
            controller.TapWorld(ContentCell(b,3,3)); controller.OpenContentCategory("placement.category.loot_node"); controller.SelectNewContent("placement.option.loot_node.basic");
            controller.TapWorld(ContentCell(b,2,1)); Assert.That(controller.ConfirmContentPlacement(),Is.True); yield return SettleContentPanel(); AssertLootRoomCount(b,2);
            int commands=controller.Draft.CommandCount;
            controller.TapWorld(ContentCell(a,2,1)); controller.BeginContentReassignment(); controller.TapWorld(ContentCell(b,3,2));
            Assert.That(controller.ContentPreview.IsValid,Is.False); Assert.That(controller.ContentPreview.Reason,Is.EqualTo(DetachedSpatialMigrationPreparer.CapacityReason));
            info=controller.GetComponent<UIDocument>().rootVisualElement.Q<Label>("contentPlacementInfo").text;
            string capacityInfo=info; TestContext.WriteLine("FULL LOOT authority="+controller.ContentPreview.Reason+" UI="+info); Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands));
            controller.CancelContentPlacement(); controller.TapWorld(ContentCell(b,2,1)); Assert.That(controller.UnassignSelectedContent(),Is.True); yield return SettleContentPanel();
            AssertLootRoomCount(b,1); commands=controller.Draft.CommandCount;
            controller.TapWorld(ContentCell(a,2,1)); controller.BeginContentReassignment(); controller.TapWorld(ContentCell(b,1,1));
            Assert.That(controller.ContentPreview.IsValid,Is.False); Assert.That(controller.ContentPreview.Reason,Is.EqualTo(DetachedCanonicalSpatialMutation.PositionInvalidReason));
            info=controller.GetComponent<UIDocument>().rootVisualElement.Q<Label>("contentPlacementInfo").text;
            string collisionInfo=info; TestContext.WriteLine("COLLISION LOOT authority="+controller.ContentPreview.Reason+" UI="+info); Assert.That(controller.ConfirmContentPlacement(),Is.False);
            Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands)); AssertCanonicalUnchanged(before,mana); controller.CancelContentPlacement();
            Assert.That(controller.Discard(),Is.True); AssertCanonicalUnchanged(before,mana);
            Assert.That(capacityInfo,Does.StartWith(root.Content.GetString("ui.dungeon.content.category_full","ui.dungeon.content.category_full")));
            Assert.That(collisionInfo,Does.StartWith(root.Content.GetString("ui.dungeon.content.position_blocked","ui.dungeon.content.position_blocked")));
        }
        private void AssertInstanceCapacity(RoomSpatialInstance room,string category,int expected)
        {
            controller.TapWorld(ContentCell(room,3,3)); Assert.That(controller.OpenContentCategory(category),Is.True);
            Assert.That(controller.Draft.ReadModel.Floors[0].RoomContents.Assignments.Count(a=>a.RoomInstanceId==room.RoomInstanceId && a.CategoryId==category),Is.EqualTo(expected));
            Assert.That(controller.GetComponent<UIDocument>().rootVisualElement.Q<Label>("contentCapacity").text,Does.Contain(expected+" / 2"));
        }
        private IEnumerator RepeatedContentProduction(string category,string option,int width,int height,string capture)
        {
            yield return PrepareCanonicalLootRooms(); var root=GameRoot.Instance; var rooms=root.Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms;
            var a=rooms[0]; var b=rooms[1]; var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(width,height); yield return SettleContentPanel(); controller.EnterEdit();
            for(int i=1;i<=2;i++)
            {
                AssertInstanceCapacity(a,category,i-1); Assert.That(controller.SelectNewContent(option),Is.True);
                int commands=controller.Draft.CommandCount; controller.TapWorld(ContentCell(a,i,1));
                Assert.That(controller.ContentPreview.IsValid,Is.True,controller.ContentPreview.Reason); Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands));
                AssertCanonicalUnchanged(before,mana); Assert.That(controller.ConfirmContentPlacement(),Is.True); yield return SettleContentPanel(); AssertInstanceCapacity(a,category,i);
            }
            var assigned=controller.Draft.ReadModel.Floors[0].RoomContents.Assignments.Where(x=>x.CategoryId==category).ToArray(); var first=assigned[0]; var sibling=assigned[1];
            Assert.That(first.AssignmentId,Is.Not.EqualTo(sibling.AssignmentId)); Assert.That(first.OptionId,Is.EqualTo(sibling.OptionId));
            var quote=root.SaveService.PreviewDungeonDraft(root.Save,controller.Draft); Assert.That(root.SaveService.TryContentAcquisitionCost(category,option,out double price),Is.True);
            Assert.That(quote.ContentAcquisitions,Is.EqualTo(2)); Assert.That(quote.ContentAcquisitionCost,Is.EqualTo(2*price));
            Assert.That(controller.SelectNewContent(option),Is.True); controller.TapWorld(ContentCell(a,3,2));
            Assert.That(controller.ContentPreview.IsValid,Is.False); Assert.That(controller.GetComponent<UIDocument>().rootVisualElement.Q<Label>("contentPlacementInfo").text,
                Does.StartWith(root.Content.GetString("ui.dungeon.content.category_full",null))); Assert.That(controller.ConfirmContentPlacement(),Is.False);
            yield return RoomContentCapture(capture); controller.CancelContentPlacement();
            controller.TapWorld(ContentCell(a,1,1)); Assert.That(controller.SelectedContent.AssignmentId,Is.EqualTo(first.AssignmentId)); controller.BeginMove();
            controller.TapWorld(ContentCell(a,1,2)); Assert.That(controller.ConfirmContentPlacement(),Is.True); yield return SettleContentPanel();
            Assert.That(controller.Draft.ReadModel.Floors[0].RoomContents.Assignments.Single(x=>x.AssignmentId==sibling.AssignmentId).RoomLocalPosition,Is.EqualTo(new TileCoordinate(2,1)));
            AssertCanonicalUnchanged(before,mana); Assert.That(controller.Commit(),Is.True); yield return SettleContentPanel();
            Assert.That(root.Save.structureRuntime.ManaReserve,Is.EqualTo(mana-2*price)); yield return RestartShell(); root=GameRoot.Instance;
            before=File.ReadAllBytes(root.SaveService.SavePath); mana=root.Save.structureRuntime.ManaReserve;
            controller.EnterEdit(); AssertInstanceCapacity(a,category,2);
            controller.TapWorld(ContentCell(a,2,1)); Assert.That(controller.SelectedContent.AssignmentId,Is.EqualTo(sibling.AssignmentId)); Assert.That(controller.UnassignSelectedContent(),Is.True); yield return SettleContentPanel();
            AssertInstanceCapacity(a,category,1); Assert.That(controller.SelectContentBrowser(true),Is.True); Assert.That(controller.SelectReturnedContent(sibling.AssignmentId),Is.True);
            controller.TapWorld(ContentCell(a,2,2)); Assert.That(controller.ContentPreview.IsValid,Is.True); Assert.That(controller.ConfirmContentPlacement(),Is.True); yield return SettleContentPanel(); AssertInstanceCapacity(a,category,2);
            foreach(var target in new[] {first,sibling})
            {
                var current=controller.Draft.ReadModel.Floors[0].RoomContents.Assignments.Single(x=>x.AssignmentId==target.AssignmentId);
                controller.TapWorld(ContentCell(a,current.RoomLocalPosition.X,current.RoomLocalPosition.Y)); Assert.That(controller.SelectedContent.AssignmentId,Is.EqualTo(target.AssignmentId));
                controller.BeginContentReassignment(); controller.TapWorld(ContentCell(b,target==first?1:2,1)); Assert.That(controller.ContentPreview.IsValid,Is.True,controller.ContentPreview.Reason);
                Assert.That(controller.ConfirmContentPlacement(),Is.True); yield return SettleContentPanel();
            }
            AssertInstanceCapacity(a,category,0); AssertInstanceCapacity(b,category,2); AssertCanonicalUnchanged(before,mana);
            quote=root.SaveService.PreviewDungeonDraft(root.Save,controller.Draft); Assert.That(quote.ContentAcquisitions,Is.Zero); Assert.That(quote.Cost,Is.Zero);
            yield return RestartShell(); root=GameRoot.Instance; Click(controller.GetComponent<UIDocument>().rootVisualElement.Q<Button>("confirm")); yield return SettleContentPanel();
            AssertInstanceCapacity(b,category,2); Assert.That(controller.Commit(),Is.True); yield return SettleContentPanel(); yield return RestartShell(); root=GameRoot.Instance;
            Assert.That(root.Save.structureRuntime.ManaReserve,Is.EqualTo(mana)); before=File.ReadAllBytes(root.SaveService.SavePath);
            controller.EnterEdit(); AssertInstanceCapacity(b,category,2); assigned=root.Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments.Where(x=>x.CategoryId==category).ToArray();
            AssertReassignedIdentity(assigned.Single(x=>x.AssignmentId==first.AssignmentId),first,b.RoomInstanceId,new TileCoordinate(1,1));
            AssertReassignedIdentity(assigned.Single(x=>x.AssignmentId==sibling.AssignmentId),sibling,b.RoomInstanceId,new TileCoordinate(2,1));
            controller.TapWorld(ContentCell(b,1,1)); Assert.That(controller.UnassignSelectedContent(),Is.True); yield return SettleContentPanel(); Assert.That(controller.Discard(),Is.True); AssertCanonicalUnchanged(before,mana);
            TestContext.WriteLine("MULTIPLICITY "+category+" paid twice, map selection, sibling isolation, free redeployment and matching-option map reassignment, recovery/save/reopen/discard");
        }
        [UnityTest] public IEnumerator RoomContentRepeatedTrapInstances() { yield return RepeatedContentProduction("placement.category.trap","placement.option.trap.spike",1920,1080,"content-multiplicity-traps.png"); }
        [UnityTest] public IEnumerator RoomContentRepeatedLootInstances() { yield return RepeatedContentProduction("placement.category.loot_node","placement.option.loot_node.glittering_hoard",720,1280,"content-multiplicity-loot.png"); }
        private static void AssertReassignedIdentity(RoomContentAssignment actual, RoomContentAssignment original, string room, TileCoordinate local)
        {
            Assert.That(actual.AssignmentId,Is.EqualTo(original.AssignmentId));
            Assert.That(actual.CategoryId,Is.EqualTo(original.CategoryId)); Assert.That(actual.OptionId,Is.EqualTo(original.OptionId));
            Assert.That(actual.Sequence,Is.EqualTo(original.Sequence)); Assert.That(actual.RoomInstanceId,Is.EqualTo(room));
            Assert.That(actual.RoomLocalPosition,Is.EqualTo(local));
        }
        private void AssertContentOverlaysCleared()
        {
            Assert.That(controller.World.RoomPlacementGridTileCount,Is.Zero);
            Assert.That(controller.World.ConstructionPreviewTileCount,Is.Zero);
            Assert.That(controller.World.transform.Find("Selection").gameObject.activeSelf,Is.False);
            Assert.That(controller.ContentPreview,Is.Null);
        }
        private IEnumerator ConstructReassignmentDestination()
        {
            controller.EnterEdit(); Assert.That(controller.SelectConstructionRoom("spatial.room.basic"),Is.True);
            controller.TapWorld(controller.World.MoveGuidanceAnchors.First()); Assert.That(controller.ConfirmConstructionPlacement(),Is.True);
            yield return SettleContentPanel();
        }
        [UnityTest]
        public IEnumerator RoomContentMapReassignmentCategoriesInvalidRetryAndSave()
        {
            var root=GameRoot.Instance; Assert.That(root.SaveService.SetQaMana(root.Save,true).IsSuccess,Is.True);
            var floor=root.Save.validatedCanonicalSpatialState.Floors[0]; var source=floor.Layout.Rooms[0];
            foreach(var item in new[] {new {Category="placement.category.trap",Option="placement.option.trap.spike",X=1},
                new {Category="placement.category.loot_node",Option="placement.option.loot_node.basic",X=2}})
                Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save,DetachedCanonicalMutationRequest.Place(item.Category,item.Option,
                    source.RoomInstanceId,floor.FloorInstanceId,new TileCoordinate(item.X,0))).IsSuccess,Is.True);
            yield return SettleContentPanel();
            var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            var originals=root.Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments.OrderBy(a=>a.Sequence).ToArray();
            int custody=root.Save.validatedCanonicalSpatialState.LifecycleAndOwnership.ReturnedContents.Length;
            yield return ConstructReassignmentDestination();
            var destination=controller.Draft.ReadModel.Floors[0].Layout.Rooms.Single(r=>r.RoomInstanceId!=source.RoomInstanceId);
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            foreach(var original in originals)
            {
                controller.TapWorld(ContentCell(source,original.RoomLocalPosition.X,original.RoomLocalPosition.Y));
                Click(ui.Q<Button>("reassignContent")); int commands=controller.Draft.CommandCount;
                Assert.That(ui.Q<Label>("contentPlacementInfo").text,Is.EqualTo(root.Content.GetString("ui.dungeon.content.destination_hint",null)));
                foreach(var invalid in new[] {new TileCoordinate(-100,-100),ContentCell(source,original.RoomLocalPosition.X,original.RoomLocalPosition.Y)})
                {
                    controller.TapWorld(invalid);
                    Assert.That(controller.ContentCategory,Is.EqualTo(original.CategoryId),"Invalid map tap must preserve pending reassignment");
                    Assert.That(controller.IsPlacingContent,Is.False); Assert.That(controller.ConfirmContentPlacement(),Is.False);
                    Assert.That(ui.Q<Label>("contentPlacementInfo").text,Is.EqualTo(root.Content.GetString("ui.dungeon.content.destination_invalid",null)));
                    Assert.That(ui.Q("contentChoices").Query<Button>().ToList().Any(b=>(string)b.userData==destination.RoomInstanceId),Is.True);
                    Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands)); AssertContentOverlaysCleared();
                }
                Assert.That(controller.SelectContentDestination("missing-floor",destination.RoomInstanceId),Is.False);
                var local=new TileCoordinate(original.RoomLocalPosition.X,1); var target=ContentCell(destination,local.X,local.Y);
                // Hit detection must take precedence even when the tap hits an assigned object.
                if(original.CategoryId!="placement.category.monster")
                {
                    controller.TapWorld(ContentCell(destination,0,1));
                    Assert.That(controller.IsPlacingContent,Is.True); Assert.That(controller.ContentPreview.IsValid,Is.False);
                    Assert.That(controller.ConfirmContentPlacement(),Is.False); Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands));
                    controller.CancelContentPlacement(); AssertContentOverlaysCleared();
                    controller.TapWorld(ContentCell(source,original.RoomLocalPosition.X,original.RoomLocalPosition.Y)); controller.BeginContentReassignment();
                }
                controller.TapWorld(target); Assert.That(controller.IsPlacingContent,Is.True,"Room map click must choose a reassignment destination");
                Assert.That(controller.ContentPreview.IsValid,Is.True,controller.ContentPreview.Reason);
                AssertReassignedIdentity(controller.ContentPreview.Assignment,original,destination.RoomInstanceId,local);
                Assert.That(controller.World.RoomPlacementGridTarget,Is.EqualTo(destination.RoomInstanceId));
                Assert.That(controller.Draft.ReadModel.Floors[0].RoomContents.Assignments.Single(a=>a.AssignmentId==original.AssignmentId).RoomInstanceId,Is.EqualTo(source.RoomInstanceId));
                Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands)); AssertCanonicalUnchanged(before,mana);
                controller.TapWorld(new TileCoordinate(-100,-100)); Assert.That(controller.ContentPreview.IsValid,Is.False);
                Assert.That(controller.World.ConstructionPreviewTileCount,Is.Zero); Assert.That(controller.ConfirmContentPlacement(),Is.False);
                controller.TapWorld(target); Assert.That(controller.ConfirmContentPlacement(),Is.True); yield return SettleContentPanel();
                Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands+1)); Assert.That(controller.Draft.Durability,Is.EqualTo(DraftDurability.Acknowledged));
                AssertReassignedIdentity(controller.SelectedContent,original,destination.RoomInstanceId,local);
                Assert.That(controller.Draft.ReadModel.LifecycleAndOwnership.ReturnedContents.Length,Is.EqualTo(custody));
                Assert.That(root.SaveService.PreviewDungeonDraft(root.Save,controller.Draft).ContentAcquisitions,Is.Zero);
                AssertCanonicalUnchanged(before,mana);
                TestContext.WriteLine("MAP REASSIGN "+original.CategoryId+" identity/sequence preserved; one acknowledged command; no reacquisition");
            }
            yield return RestartShell(); root=GameRoot.Instance; ui=controller.GetComponent<UIDocument>().rootVisualElement; Click(ui.Q<Button>("confirm")); yield return SettleContentPanel();
            foreach(var original in originals) AssertReassignedIdentity(controller.Draft.ReadModel.Floors[0].RoomContents.Assignments.Single(a=>a.AssignmentId==original.AssignmentId),
                original,destination.RoomInstanceId,new TileCoordinate(original.RoomLocalPosition.X,1));
            AssertCanonicalUnchanged(before,mana); var quote=root.SaveService.PreviewDungeonDraft(root.Save,controller.Draft);
            Assert.That(quote.ContentAcquisitions,Is.Zero); Assert.That(quote.ContentAcquisitionCost,Is.Zero);
            Click(ui.Q<Button>("save")); Click(ui.Q<Button>("confirm")); yield return SettleContentPanel();
            Assert.That(controller.IsEditing,Is.False); Assert.That(root.Save.structureRuntime.ManaReserve,Is.EqualTo(mana-quote.Cost));
            yield return RestartShell(); root=GameRoot.Instance;
            foreach(var original in originals) AssertReassignedIdentity(root.Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments.Single(a=>a.AssignmentId==original.AssignmentId),
                original,destination.RoomInstanceId,new TileCoordinate(original.RoomLocalPosition.X,1));
            Assert.That(root.Save.validatedCanonicalSpatialState.LifecycleAndOwnership.ReturnedContents.Length,Is.EqualTo(custody));
        }
        [UnityTest]
        public IEnumerator RoomContentMapReassignmentCapacityMoveOwnedAndDiscard()
        {
            var root=GameRoot.Instance; Assert.That(root.SaveService.SetQaMana(root.Save,true).IsSuccess,Is.True);
            var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            var floor=root.Save.validatedCanonicalSpatialState.Floors[0]; var source=floor.Layout.Rooms[0]; var original=floor.RoomContents.Assignments.Single();
            yield return ConstructReassignmentDestination();
            var destination=controller.Draft.ReadModel.Floors[0].Layout.Rooms.Single(r=>r.RoomInstanceId!=source.RoomInstanceId);
            Assert.That(CanonicalRoomCapacityResolver.TryResolve(root.ProductionSpatialContent,destination.RoomDefinitionId,out var capacity,out _),Is.True);
            for(int i=0;i<capacity.MonsterCapacity;i++)
            {
                controller.TapWorld(ContentCell(destination,i,0)); controller.OpenContentCategory(original.CategoryId);
                Assert.That(controller.SelectNewContent("placement.option.monster.goblin"),Is.True);
                controller.TapWorld(ContentCell(destination,i,0)); Assert.That(controller.ConfirmContentPlacement(),Is.True); yield return SettleContentPanel();
            }
            int commands=controller.Draft.CommandCount;
            controller.TapWorld(ContentCell(source,0,0)); controller.BeginContentReassignment(); controller.TapWorld(ContentCell(destination,0,2));
            Assert.That(controller.IsPlacingContent,Is.True); Assert.That(controller.ContentPreview.IsValid,Is.False,"Destination category capacity is authoritative");
            Assert.That(controller.ContentPreview.Reason,Is.Not.Null.And.Not.Empty); Assert.That(controller.ConfirmContentPlacement(),Is.False);
            Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands)); Assert.That(controller.World.RoomPlacementGridTarget,Is.EqualTo(destination.RoomInstanceId));
            controller.CancelContentPlacement(); AssertContentOverlaysCleared();
            controller.TapWorld(ContentCell(source,0,0)); controller.BeginContentReassignment(); controller.CancelContentPlacement(); AssertContentOverlaysCleared();
            controller.TapWorld(ContentCell(source,0,0)); controller.BeginMove(); controller.TapWorld(ContentCell(destination,0,2));
            Assert.That(controller.ContentPreview.IsValid,Is.False,"Move must never become Reassign");
            Assert.That(controller.World.RoomPlacementGridTarget,Is.EqualTo(source.RoomInstanceId));
            controller.TapWorld(ContentCell(source,1,1)); Assert.That(controller.ConfirmContentPlacement(),Is.True); yield return SettleContentPanel();
            AssertReassignedIdentity(controller.SelectedContent,original,source.RoomInstanceId,new TileCoordinate(1,1));
            var cost=root.SaveService.PreviewDungeonDraft(root.Save,controller.Draft).ContentAcquisitionCost;
            Assert.That(controller.UnassignSelectedContent(),Is.True); Assert.That(controller.SelectReturnedContent(original.AssignmentId),Is.True);
            controller.TapWorld(ContentCell(destination,0,2)); Assert.That(controller.ContentPreview.IsValid,Is.False);
            controller.TapWorld(ContentCell(source,1,1)); Assert.That(controller.ConfirmContentPlacement(),Is.True); yield return SettleContentPanel();
            AssertReassignedIdentity(controller.SelectedContent,original,source.RoomInstanceId,new TileCoordinate(1,1));
            Assert.That(root.SaveService.PreviewDungeonDraft(root.Save,controller.Draft).ContentAcquisitionCost,Is.EqualTo(cost));
            AssertCanonicalUnchanged(before,mana); Assert.That(controller.Discard(),Is.True); AssertCanonicalUnchanged(before,mana);
        }
        [UnityTest]
        public IEnumerator RoomContentMapReassignmentProductionMouseTouchRecovery()
        {
            var root=GameRoot.Instance;
            // This preview-preservation fixture controls autonomous ticks while it compares exact save bytes.
            root.TimeService.OnPause();
            Assert.That(root.SaveService.SetQaMana(root.Save,true).IsSuccess,Is.True);
            var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            var floor=root.Save.validatedCanonicalSpatialState.Floors[0]; var source=floor.Layout.Rooms[0]; var original=floor.RoomContents.Assignments.Single();
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(1920,1080); yield return SettleContentPanel();
            yield return ConstructReassignmentDestination();
            var destination=controller.Draft.ReadModel.Floors[0].Layout.Rooms.Single(r=>r.RoomInstanceId!=source.RoomInstanceId);
            controller.TapWorld(ContentCell(source,0,0)); controller.BeginContentReassignment(); yield return SettleContentPanel(); controller.FitFloor();
            var camera=controller.GetComponentInChildren<Camera>(); var target=ContentCell(destination,1,1);
            Vector2 screen=camera.WorldToScreenPoint(new Vector3(target.X+.5f,target.Y+.5f,0));
            Assert.That(camera.pixelRect.Contains(screen),Is.True); Assert.That(controller.IsChrome(screen,1),Is.False);
            int commands=controller.Draft.CommandCount;
            var mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
            try
            {
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState {position=screen}.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                yield return null; yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState {position=screen}); yield return SettleContentPanel();
                Assert.That(controller.IsPlacingContent,Is.True,"Actual production mouse destination tap");
                Assert.That(controller.ContentPreview.IsValid,Is.True); AssertReassignedIdentity(controller.ContentPreview.Assignment,original,destination.RoomInstanceId,new TileCoordinate(1,1));
                Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands)); AssertCanonicalUnchanged(before,mana);
                yield return RoomContentCapture("map-reassignment-destination.png");
            }
            finally { UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse); }
            controller.CancelContentPlacement(); AssertContentOverlaysCleared();
            controller.TapWorld(ContentCell(source,0,0)); controller.BeginContentReassignment(); yield return SettleContentPanel(); controller.FitFloor();
            screen=camera.WorldToScreenPoint(new Vector3(target.X+.5f,target.Y+.5f,0));
            simulatedTouch=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
            Touch(1,screen,UnityEngine.InputSystem.TouchPhase.Began); yield return null; yield return null;
            Touch(1,screen,UnityEngine.InputSystem.TouchPhase.Ended); yield return SettleContentPanel();
            Assert.That(controller.IsPlacingContent,Is.True,"Actual production touch destination tap");
            Assert.That(controller.ContentPreview.IsValid,Is.True); AssertReassignedIdentity(controller.ContentPreview.Assignment,original,destination.RoomInstanceId,new TileCoordinate(1,1));
            Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands)); AssertCanonicalUnchanged(before,mana);
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            screen=CompositionButtonScreen(ui.Q<Button>("confirmContent"));
            Touch(1,screen,UnityEngine.InputSystem.TouchPhase.Began); yield return null; yield return null;
            Touch(1,screen,UnityEngine.InputSystem.TouchPhase.Ended); yield return SettleContentPanel();
            Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands+1)); Assert.That(controller.Draft.Durability,Is.EqualTo(DraftDurability.Acknowledged));
            AssertReassignedIdentity(controller.SelectedContent,original,destination.RoomInstanceId,new TileCoordinate(1,1)); AssertCanonicalUnchanged(before,mana);
            yield return RestartShell(); ui=controller.GetComponent<UIDocument>().rootVisualElement; Click(ui.Q<Button>("confirm")); yield return SettleContentPanel();
            AssertReassignedIdentity(controller.Draft.ReadModel.Floors[0].RoomContents.Assignments.Single(a=>a.AssignmentId==original.AssignmentId),original,destination.RoomInstanceId,new TileCoordinate(1,1));
            Assert.That(controller.Discard(),Is.True); AssertCanonicalUnchanged(before,mana);
            TestContext.WriteLine("MAP REASSIGN production mouse/touch, preview-only destination tap, touch Confirm, restart recovery and Discard passed");
        }
        [UnityTest]
        public IEnumerator RoomContentCompactMoveAllCategoriesAndDraftCosts()
        {
            var root=GameRoot.Instance; Assert.That(root.SaveService.SetQaMana(root.Save,true).IsSuccess,Is.True);
            var floor=root.Save.validatedCanonicalSpatialState.Floors[0]; var room=floor.Layout.Rooms[0];
            foreach(var item in new[] {new {Category="placement.category.trap",Option="placement.option.trap.spike",X=1},
                new {Category="placement.category.loot_node",Option="placement.option.loot_node.basic",X=2}})
                Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save,DetachedCanonicalMutationRequest.Place(item.Category,item.Option,
                    room.RoomInstanceId,floor.FloorInstanceId,new TileCoordinate(item.X,0))).IsSuccess,Is.True);
            for(int i=0;i<12;i++) yield return null;
            var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            var identities=root.Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments.OrderBy(a=>a.RoomLocalPosition.X).ToArray();
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            foreach(bool accumulated in new[] {false,true})
            {
                controller.EnterEdit();
                if(accumulated)
                {
                    controller.TapWorld(ContentCell(room,3,3)); controller.OpenContentCategory("placement.category.monster");
                    controller.SelectNewContent("placement.option.monster.goblin"); controller.TapWorld(ContentCell(room,3,3));
                    Assert.That(controller.ConfirmContentPlacement(),Is.True);
                    Assert.That(root.SaveService.PreviewDungeonDraft(root.Save,controller.Draft).ContentAcquisitionCost,Is.GreaterThan(0));
                }
                int iteration=0;
                foreach(var size in new[] {new Vector2Int(720,1280),new Vector2Int(1920,1080),new Vector2Int(1024,600)})
                foreach(var text in new[] {DungeonTextSize.Small,DungeonTextSize.Default,DungeonTextSize.Large})
                {
                    DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x,size.y); controller.SetTextSize(text);
                    foreach(var identity in identities)
                    {
                        var current=controller.Draft.ReadModel.Floors[0].RoomContents.Assignments.Single(a=>a.AssignmentId==identity.AssignmentId);
                        controller.TapWorld(ContentCell(room,current.RoomLocalPosition.X,current.RoomLocalPosition.Y)); controller.BeginMove();
                        int commands=controller.Draft.CommandCount;
                        yield return SettleContentPanel(); AssertCompactContentPanel("Move instruction "+identity.CategoryId+" costs="+accumulated);
                        if(!accumulated && identity==identities[0] && text==DungeonTextSize.Default && size.x!=1024)
                            yield return RoomContentCapture(size.x==720 ? "placement-compact-portrait-move.png" : "placement-compact-landscape-move.png");
                        var valid=ContentCell(room,identity.RoomLocalPosition.X,1+iteration%2);
                        controller.TapWorld(valid); Assert.That(controller.ContentPreview.IsValid,Is.True,controller.ContentPreview.Reason);
                        yield return SettleContentPanel(); AssertCompactContentPanel("Valid Move "+identity.CategoryId+" costs="+accumulated);
                        if(accumulated && identity==identities[0] && text==DungeonTextSize.Large && size.x==1024)
                        { controller.FocusRoom(); yield return RoomContentCapture("placement-compact-narrow-large.png"); }
                        controller.TapWorld(new TileCoordinate(-100,-100)); Assert.That(controller.ContentPreview.IsValid,Is.False);
                        Assert.That(controller.World.ConstructionPreviewTileCount,Is.Zero);
                        yield return SettleContentPanel(); AssertCompactContentPanel("Invalid Move "+identity.CategoryId+" costs="+accumulated);
                        Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands)); controller.CancelContentPlacement();
                        yield return SettleContentPanel(); Assert.That(controller.IsPlacingContent,Is.False);
                        Assert.That(controller.SafeRoot.ClassListContains("placing-content"),Is.False);
                        Assert.That(controller.World.RoomPlacementGridTileCount,Is.Zero);
                        AssertReachable(ui.Q<Button>("save")); AssertReachable(ui.Q<Button>("discard"));
                        controller.TapWorld(ContentCell(room,current.RoomLocalPosition.X,current.RoomLocalPosition.Y)); controller.BeginMove();
                        controller.TapWorld(valid); Assert.That(controller.ConfirmContentPlacement(),Is.True);
                        yield return SettleContentPanel(); Assert.That(controller.SelectedContent.AssignmentId,Is.EqualTo(identity.AssignmentId));
                        Assert.That(controller.IsPlacingContent,Is.False); Assert.That(controller.SafeRoot.ClassListContains("placing-content"),Is.False);
                        AssertReachable(ui.Q<Button>("save")); AssertReachable(ui.Q<Button>("discard")); AssertCanonicalUnchanged(before,mana);
                    }
                    iteration++;
                }
                int reconstructions=controller.World.ReconstructionCount;
                for(int i=0;i<12;i++) { controller.TapWorld(ContentCell(room,3,2)); controller.FocusRoom(); controller.FitFloor(); }
                Assert.That(controller.World.ReconstructionCount,Is.EqualTo(reconstructions));
                Assert.That(controller.Discard(),Is.True); AssertCanonicalUnchanged(before,mana);
            }
        }
        [UnityTest]
        public IEnumerator RoomContentCompactAcquisitionOwnedReassignAndOverflow()
        {
            var root=GameRoot.Instance; Assert.That(root.SaveService.SetQaMana(root.Save,true).IsSuccess,Is.True);
            var floor=root.Save.validatedCanonicalSpatialState.Floors[0]; var room=floor.Layout.Rooms[0]; var identity=floor.RoomContents.Assignments.Single();
            var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            controller.EnterEdit(); controller.SelectConstructionRoom("spatial.room.basic");
            controller.TapWorld(controller.World.MoveGuidanceAnchors.First()); Assert.That(controller.ConfirmConstructionPlacement(),Is.True);
            var destination=controller.Draft.ReadModel.Floors[0].Layout.Rooms.Single(r=>r.RoomInstanceId!=room.RoomInstanceId);
            yield return SettleContentPanel();
            Assert.That(controller.Commit(),Is.True,"Isolated second-room fixture: "+ui.Q<Label>("status").text);
            before=File.ReadAllBytes(root.SaveService.SavePath); mana=root.Save.structureRuntime.ManaReserve;
            controller.EnterEdit(); Assert.That(controller.Draft.CommandCount,Is.Zero);
            var strings=(System.Collections.Generic.Dictionary<string,string>)typeof(ContentService).GetField("_stringMap",
                System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(root.Content);
            string originalHint=strings["ui.dungeon.content.position_hint"];
            try
            {
                foreach(bool accumulated in new[] {false,true})
                {
                if(accumulated)
                {
                    controller.TapWorld(ContentCell(destination,3,3)); controller.OpenContentCategory("placement.category.monster");
                    controller.SelectNewContent("placement.option.monster.goblin"); controller.TapWorld(ContentCell(destination,3,3));
                    Assert.That(controller.ConfirmContentPlacement(),Is.True);
                    Assert.That(root.SaveService.PreviewDungeonDraft(root.Save,controller.Draft).ContentAcquisitionCost,Is.GreaterThan(0));
                }
                foreach(var size in new[] {new Vector2Int(720,1280),new Vector2Int(1920,1080),new Vector2Int(1024,600)})
                foreach(var text in new[] {DungeonTextSize.Small,DungeonTextSize.Default,DungeonTextSize.Large})
                {
                    DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x,size.y); controller.SetTextSize(text);
                    foreach(var option in new[] {new {Category="placement.category.monster",Option="placement.option.monster.goblin"},
                        new {Category="placement.category.trap",Option="placement.option.trap.spike"},new {Category="placement.category.loot_node",Option="placement.option.loot_node.basic"}})
                    {
                        controller.TapWorld(ContentCell(room,1,2)); controller.OpenContentCategory(option.Category); controller.SelectNewContent(option.Option);
                        yield return SettleContentPanel(); AssertCompactContentPanel("New "+option.Category+" costs="+accumulated,false);
                        controller.TapWorld(ContentCell(room,1,2)); Assert.That(controller.ContentPreview.IsValid,Is.True);
                        yield return SettleContentPanel(); AssertCompactContentPanel("New valid "+option.Category,false); controller.CancelContentPlacement();
                    }
                    controller.TapWorld(ContentCell(room,0,0)); Assert.That(controller.UnassignSelectedContent(),Is.True);
                    var quote=root.SaveService.PreviewDungeonDraft(root.Save,controller.Draft).ContentAcquisitionCost;
                    Assert.That(controller.SelectReturnedContent(identity.AssignmentId),Is.True);
                    yield return SettleContentPanel(); AssertCompactContentPanel("Owned costs="+accumulated);
                    controller.TapWorld(ContentCell(room,0,0)); Assert.That(controller.ContentPreview.IsValid,Is.True);
                    yield return SettleContentPanel(); AssertCompactContentPanel("Owned valid"); Assert.That(controller.ConfirmContentPlacement(),Is.True);
                    Assert.That(root.SaveService.PreviewDungeonDraft(root.Save,controller.Draft).ContentAcquisitionCost,Is.EqualTo(quote));
                    controller.BeginContentReassignment(); Assert.That(controller.SelectContentDestination(floor.FloorInstanceId,destination.RoomInstanceId),Is.True);
                    yield return SettleContentPanel(); AssertCompactContentPanel("Reassign instruction costs="+accumulated);
                    controller.TapWorld(ContentCell(destination,1,1)); Assert.That(controller.ContentPreview.IsValid,Is.True);
                    yield return SettleContentPanel(); AssertCompactContentPanel("Reassign valid"); controller.CancelContentPlacement();
                    controller.TapWorld(ContentCell(room,0,0)); controller.BeginMove(); strings["ui.dungeon.content.position_hint"]=string.Join("\n",
                        Enumerable.Repeat("Test language expansion: select an exact cell in this room; preview changes only the draft until Confirm Placement.",20));
                    controller.SetTextSize(text); controller.CancelContentPlacement(); controller.TapWorld(ContentCell(room,0,0)); controller.BeginMove();
                    yield return SettleContentPanel(); AssertCompactContentPanel("Long localized Move",false);
                    var details=ui.Q<ScrollView>("contextDetails");
                    Assert.That(details.verticalScroller.highValue,Is.GreaterThan(0));
                    Assert.That(details.contentContainer.worldBound.height,Is.GreaterThan(details.contentViewport.worldBound.height));
                    details.scrollOffset=new Vector2(0,details.verticalScroller.highValue); yield return SettleContentPanel();
                    Assert.That(details.scrollOffset.y,Is.GreaterThan(0)); AssertReachable(ui.Q<Button>("confirmContent"));
                    if(size.x==1024 && text==DungeonTextSize.Large) yield return RoomContentCapture("placement-compact-long-scroll.png");
                    strings["ui.dungeon.content.position_hint"]=originalHint; controller.CancelContentPlacement();
                    controller.TapWorld(ContentCell(room,0,0)); controller.BeginMove();
                    yield return SettleContentPanel(); AssertCompactContentPanel("Short after overflow"); controller.CancelContentPlacement();
                    AssertCanonicalUnchanged(before,mana);
                }
                if(!accumulated) { Assert.That(controller.Discard(),Is.True); controller.EnterEdit(); }
                }
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(720,1280); controller.SetTextSize(DungeonTextSize.Default);
                controller.TapWorld(ContentCell(room,0,0)); controller.BeginMove(); controller.TapWorld(ContentCell(room,1,1));
                yield return RoomContentCapture("placement-compact-grid-floor.png"); controller.FocusRoom();
                yield return RoomContentCapture("placement-compact-grid-focus.png");
                controller.CancelContentPlacement(); Assert.That(controller.Discard(),Is.True); AssertCanonicalUnchanged(before,mana);
            }
            finally { strings["ui.dungeon.content.position_hint"]=originalHint; }
        }
        [UnityTest]
        public IEnumerator RoomContentUatBrowserIdentityAndDirectTargetSwitch()
        {
            var root = GameRoot.Instance; Assert.That(root.SaveService.SetQaMana(root.Save,true).IsSuccess, Is.True);
            var before = File.ReadAllBytes(root.SaveService.SavePath); double mana = root.Save.structureRuntime.ManaReserve;
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0]; var source = floor.Layout.Rooms[0];
            var original = floor.RoomContents.Assignments.Single();
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            controller.EnterEdit(); controller.SelectConstructionRoom("spatial.room.basic");
            controller.TapWorld(controller.World.MoveGuidanceAnchors.First()); Assert.That(controller.ConfirmConstructionPlacement(), Is.True);
            yield return null; yield return null;
            var destination = controller.Draft.ReadModel.Floors[0].Layout.Rooms.Single(r => r.RoomInstanceId != source.RoomInstanceId);
            controller.TapWorld(ContentCell(source,0,0));
            Assert.That(ui.Q<Button>("roomsCategory").ClassListContains("selected-floor"), Is.False);
            Assert.That(ui.Q<Button>("monstersCategory").ClassListContains("selected-floor"), Is.True);
            var definition = root.ProductionSpatialContent.Catalog.Rooms.Single(r=>r.RoomDefinitionId==source.RoomDefinitionId);
            var roomName = root.ProductionSpatialContent.Languages.Single(t=>t.language=="en").entries.Single(e=>e.key==definition.LocalizationKey).text;
            Assert.That(ui.Q<Label>("inspectionInfo").text, Does.StartWith(string.Format(root.Content.GetString("ui.dungeon.content.info",null),
                string.Format(root.Content.GetString("ui.dungeon.room_selected",null),roomName,1))));
            Assert.That(ui.Q<Label>("inspectionInfo").text, Does.Not.Contain("Room-local").And.Not.Contain("0, 0"));
            Assert.That(controller.UnassignSelectedContent(), Is.True);
            Assert.That(controller.ContentBrowserOwned, Is.True); Assert.That(controller.ContentPreview, Is.Null);
            CollectionAssert.AreEqual(new[] {original.AssignmentId}, ui.Q("contentChoices").Query<Button>().ToList().Select(b => (string)b.userData));
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(720,1280);
            yield return RoomContentCapture("room-content-uat-owned.png");
            Click(ui.Q<Button>("newContent"));
            Assert.That(controller.ContentBrowserOwned, Is.False); Assert.That(controller.IsPlacingContent, Is.False);
            Assert.That(ui.Q("contentChoices").Query<Button>().ToList().Any(b => (string)b.userData == original.AssignmentId), Is.False);
            Assert.That(ui.Q("contentChoices").Query<Button>().ToList().Count(b => (string)b.userData == original.OptionId), Is.EqualTo(1));
            Assert.That(ui.Q<Button>("move").style.display.value, Is.EqualTo(DisplayStyle.None));
            yield return RoomContentCapture("room-content-uat-new.png");
            var newIds = new System.Collections.Generic.List<string>();
            for (int x=1;x<=2;x++)
            {
                controller.OpenContentCategory(original.CategoryId); Assert.That(controller.SelectNewContent(original.OptionId), Is.True);
                controller.TapWorld(ContentCell(source,x,1)); Assert.That(controller.ContentPreview.IsValid, Is.True);
                Assert.That(controller.ConfirmContentPlacement(), Is.True); newIds.Add(controller.SelectedContent.AssignmentId);
            }
            Assert.That(newIds.Distinct().Count(), Is.EqualTo(2)); Assert.That(newIds, Does.Not.Contain(original.AssignmentId));
            controller.OpenContentCategory(original.CategoryId); Click(ui.Q<Button>("ownedContent"));
            CollectionAssert.AreEqual(new[] {original.AssignmentId}, ui.Q("contentChoices").Query<Button>().ToList().Select(b => (string)b.userData));
            Assert.That(controller.SelectReturnedContent(original.AssignmentId), Is.True);
            int commands = controller.Draft.CommandCount; var quote = root.SaveService.PreviewDungeonDraft(root.Save,controller.Draft);
            controller.TapWorld(ContentCell(source,3,1)); Assert.That(controller.ContentPreview.IsValid, Is.False, "Full target room");
            controller.TapWorld(ContentCell(destination,1,1));
            Assert.That(controller.SelectedRoomInstanceId, Is.EqualTo(destination.RoomInstanceId));
            Assert.That(controller.ContentPreview.IsValid, Is.True, controller.ContentPreview.Reason);
            Assert.That(controller.ContentPreview.Assignment.AssignmentId, Is.EqualTo(original.AssignmentId));
            Assert.That(ui.Q<Label>("contentCapacity").text, Does.Contain("0 / 2"));
            Assert.That(controller.World.RoomPlacementGridTarget, Is.EqualTo(destination.RoomInstanceId));
            Assert.That(controller.Draft.CommandCount, Is.EqualTo(commands)); AssertCanonicalUnchanged(before,mana);
            controller.FocusRoom(); yield return RoomContentCapture("room-content-uat-target-room.png");
            Assert.That(controller.ConfirmContentPlacement(), Is.True);
            Assert.That(controller.SelectedContent.AssignmentId, Is.EqualTo(original.AssignmentId));
            Assert.That(controller.SelectedContent.Sequence, Is.EqualTo(original.Sequence));
            Assert.That(controller.Draft.ReadModel.LifecycleAndOwnership.ReturnedContents, Is.Empty);
            var after = root.SaveService.PreviewDungeonDraft(root.Save,controller.Draft);
            Assert.That(after.ContentAcquisitionCost, Is.EqualTo(quote.ContentAcquisitionCost)); Assert.That(after.ContentAcquisitions, Is.EqualTo(2));
            AssertCanonicalUnchanged(before,mana);
            controller.TapWorld(ContentCell(source,3,2)); controller.OpenContentCategory(original.CategoryId); controller.SelectNewContent("placement.option.monster.goblin");
            controller.TapWorld(ContentCell(destination,2,1)); Assert.That(controller.SelectedRoomInstanceId, Is.EqualTo(destination.RoomInstanceId));
            Assert.That(controller.ContentPreview.Assignment.OptionId, Is.EqualTo("placement.option.monster.goblin"));
            controller.TapWorld(ContentCell(source,3,2)); Assert.That(controller.SelectedRoomInstanceId, Is.EqualTo(source.RoomInstanceId));
            Assert.That(controller.ContentPreview.IsValid, Is.False); Assert.That(controller.Draft.CommandCount, Is.EqualTo(commands+1));
            controller.CancelContentPlacement(); controller.TapWorld(ContentCell(destination,1,1)); controller.BeginMove();
            controller.TapWorld(ContentCell(source,3,2)); Assert.That(controller.ContentPreview.IsValid, Is.False);
            Assert.That(controller.SelectedRoomInstanceId, Is.EqualTo(destination.RoomInstanceId), "Move cannot become implicit reassignment");
            controller.CancelContentPlacement(); controller.TapWorld(ContentCell(destination,1,1));
            yield return RoomContentCapture("room-content-uat-inspector.png");
            Assert.That(controller.Discard(), Is.True); AssertCanonicalUnchanged(before,mana);
        }
        [UnityTest]
        public IEnumerator RoomContentUatGridAndMulticellInvalidTransitions()
        {
            var root = GameRoot.Instance;
            root.Save.completedResearch = new CompletedResearchState { ProjectIds = new[] {"ac_100"} };
            var construction = root.SaveService.PreviewFloorConstruction(root.Save);
            root.Save.structureRuntime.ManaReserve += construction.Profile.ConstructionMana;
            construction = root.SaveService.PreviewFloorConstruction(root.Save);
            Assert.That(root.SaveService.CommitFloorConstruction(root.Save,construction).IsSuccess, Is.True);
            Assert.That(root.SaveService.SetQaMana(root.Save,true).IsSuccess, Is.True);
            for(int i=0;i<12;i++) yield return null;
            var before = File.ReadAllBytes(root.SaveService.SavePath); double mana = root.Save.structureRuntime.ManaReserve;
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0]; var room = floor.Layout.Rooms[0];
            var context = root.SaveService.DungeonDraftContext;
            Assert.That(RoomContentSpatialOccupancyAuthority.TryResolve(context.Occupancy,"placement.category.trap","placement.option.trap.spike",out var rule), Is.True);
            var offsets = rule.OccupiedTileOffsets;
            // Inject a test-only two-cell footprint; production occupancy is untouched.
            rule.OccupiedTileOffsets = new[] {new TileCoordinate(0,0),new TileCoordinate(1,0)};
            try
            {
                controller.EnterEdit(); controller.SelectConstructionRoom("spatial.room.basic");
                controller.TapWorld(controller.World.MoveGuidanceAnchors.First()); Assert.That(controller.ConfirmConstructionPlacement(), Is.True);
                var otherRoom = controller.Draft.ReadModel.Floors[0].Layout.Rooms.Single(r=>r.RoomInstanceId!=room.RoomInstanceId);
                int commands = controller.Draft.CommandCount;
                controller.TapWorld(ContentCell(room,1,1)); controller.OpenContentCategory("placement.category.trap");
                controller.SelectNewContent("placement.option.trap.spike"); controller.TapWorld(ContentCell(room,1,1));
                Assert.That(controller.ContentPreview.IsValid, Is.True); Assert.That(controller.World.ConstructionPreviewTileCount, Is.EqualTo(2));
                var grid = controller.World.transform.Find("RoomPlacementGrid").GetComponent<UnityEngine.Tilemaps.Tilemap>();
                var gridTile=grid.GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(ContentCell(room,1,1).X,ContentCell(room,1,1).Y,0));
                Assert.That(gridTile.sprite.texture.GetPixel(0,16).a,Is.EqualTo(1));
                Assert.That(gridTile.sprite.texture.GetPixel(16,16).a,Is.Zero,"Transparent interior cannot hide content or masonry");
                Assert.That(gridTile.color,Is.EqualTo(controller.presentationPolicy.RoomPlacementGridColor));
                Assert.That(gridTile.color,Is.Not.EqualTo(controller.presentationPolicy.FloorBoundaryColor));
                double Luminance(Color color) { var linear=color.linear; return .2126*linear.r+.7152*linear.g+.0722*linear.b; }
                foreach(var masonry in controller.presentationPolicy.Visuals.RoomStone)
                {
                    var texture=new Texture2D(2,2);
                    try
                    {
                        Assert.That(texture.LoadImage(File.ReadAllBytes(UnityEditor.AssetDatabase.GetAssetPath(masonry.texture))),Is.True);
                        double background=texture.GetPixels().Where(c=>c.a>0).Average(Luminance);
                        double contrast=(Luminance(gridTile.color)+.05)/(background+.05);
                        double previous=(Luminance(controller.presentationPolicy.FloorBoundaryColor)+.05)/(background+.05);
                        TestContext.WriteLine("GRID "+masonry.name+" contrast="+contrast+" previous="+previous);
                        Assert.That(contrast,Is.GreaterThanOrEqualTo(3),"Grid against actual authored masonry average luminance");
                        Assert.That(contrast,Is.GreaterThan(previous*1.5),"Contrast improves without changing floor boundaries");
                    }
                    finally { Object.Destroy(texture); }
                }
                foreach(var semantic in new[] {controller.presentationPolicy.SelectionColor,controller.presentationPolicy.ValidColor,controller.presentationPolicy.InvalidColor})
                    Assert.That(gridTile.color,Is.Not.EqualTo(semantic));
                int order = grid.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder;
                int gridBudget = controller.World.GridTileCount;
                Assert.That(order, Is.GreaterThan(controller.World.transform.Find("Rooms").GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder));
                Assert.That(order, Is.LessThan(controller.World.transform.Find("StonePerimeter").GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder));
                Assert.That(controller.World.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name=="Content" || r.name=="SavedConnectionThreshold").All(r=>r.sortingOrder>order), Is.True);
                controller.TapWorld(ContentCell(otherRoom,1,1));
                Assert.That(controller.ContentPreview.IsValid, Is.True); Assert.That(controller.World.ConstructionPreviewTileCount, Is.EqualTo(2));
                Assert.That(controller.World.RoomPlacementGridTarget, Is.EqualTo(otherRoom.RoomInstanceId));
                var previousCell=ContentCell(room,1,1);
                Assert.That(grid.HasTile(new Vector3Int(previousCell.X,previousCell.Y,0)), Is.False);
                Assert.That(controller.World.transform.Find("ConstructionPreview").GetComponent<UnityEngine.Tilemaps.Tilemap>().HasTile(new Vector3Int(previousCell.X,previousCell.Y,0)), Is.False);
                controller.TapWorld(previousCell); Assert.That(controller.ContentPreview.IsValid, Is.True);
                controller.FocusRoom(); yield return RoomContentCapture("room-content-uat-grid-portrait.png");
                var outsideDefinition=context.Production.Catalog.Rooms.Single(r=>r.RoomDefinitionId==room.RoomDefinitionId);
                var outside=new TileCoordinate(room.Anchor.X+outsideDefinition.GrossFootprint.Width,room.Anchor.Y+1);
                Assert.That(controller.World.SelectContentRoom(outside),Is.Null);
                controller.TapWorld(outside);
                Assert.That(controller.ContentPreview.IsValid, Is.False); Assert.That(controller.World.ConstructionPreviewTileCount, Is.Zero);
                Assert.That(controller.SelectedRoomInstanceId, Is.EqualTo(room.RoomInstanceId));
                Assert.That(controller.Draft.CommandCount, Is.EqualTo(commands)); AssertCanonicalUnchanged(before,mana);
                yield return RoomContentCapture("room-content-uat-invalid-multicell.png");
                controller.TapWorld(ContentCell(room,1,1)); Assert.That(controller.ContentPreview.IsValid, Is.True);
                Assert.That(controller.World.ConstructionPreviewTileCount, Is.EqualTo(2));
                controller.CancelContentPlacement(); Assert.That(controller.World.RoomPlacementGridTileCount, Is.Zero);
                Assert.That(controller.World.ConstructionPreviewTileCount, Is.Zero);
                controller.SelectNewContent("placement.option.trap.spike"); controller.TapWorld(ContentCell(room,1,1));
                controller.SelectFloor(root.Save.validatedCanonicalSpatialState.Floors[1].FloorInstanceId);
                Assert.That(controller.World.RoomPlacementGridTileCount, Is.Zero); Assert.That(controller.World.ConstructionPreviewTileCount, Is.Zero);
                Assert.That(controller.IsPlacingContent, Is.False); Assert.That(controller.World.PreviewVisible, Is.False);
                Assert.That(controller.Discard(), Is.True); AssertCanonicalUnchanged(before,mana);
                foreach (CardinalOrientation orientation in System.Enum.GetValues(typeof(CardinalOrientation)))
                {
                    var projection = JsonUtility.FromJson<SavedSpatialFloor>(JsonUtility.ToJson(floor));
                    projection.Layout.Rooms[0].RoomDefinitionId="spatial.room.rectangle"; projection.Layout.Rooms[0].Orientation=orientation;
                    var world = controller.World;
                    world.Render(projection,context.Production,context.Occupancy,context.Limits.Canonical.Spatial.MaximumMaterializedTiles,gridBudget,true);
                    world.PresentRoomPlacementGrid(room.RoomInstanceId);
                    var definition = context.Production.Catalog.Rooms.Single(r=>r.RoomDefinitionId==projection.Layout.Rooms[0].RoomDefinitionId);
                    Assert.That(world.RoomPlacementGridTileCount, Is.EqualTo(definition.GrossFootprint.Width*definition.GrossFootprint.Height));
                    for(int x=0;x<definition.GrossFootprint.Width;x++) for(int y=0;y<definition.GrossFootprint.Height;y++)
                    {
                        Assert.That(RoomLocalCoordinateTransform.TryToFloor(new TileCoordinate(x,y),definition.GrossFootprint,room.Anchor,orientation,out var cell), Is.True);
                        Assert.That(grid.HasTile(new Vector3Int(cell.X,cell.Y,0)), Is.True);
                        Assert.That((Vector2)grid.GetCellCenterWorld(new Vector3Int(cell.X,cell.Y,0)), Is.EqualTo(new Vector2(cell.X+.5f,cell.Y+.5f)));
                    }
                    world.ClearPreview(); Assert.That(world.RoomPlacementGridTileCount, Is.Zero);
                }
            }
            finally { rule.OccupiedTileOffsets = offsets; }
        }
        [UnityTest]
        public IEnumerator RoomContentUatPlacementGeometryAndLocalizedFeedback()
        {
            var root = GameRoot.Instance; var room = root.Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms[0];
            Assert.That(root.SaveService.SetQaMana(root.Save,true).IsSuccess, Is.True);
            var before = File.ReadAllBytes(root.SaveService.SavePath); double mana = root.Save.structureRuntime.ManaReserve;
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            controller.EnterEdit(); controller.TapWorld(ContentCell(room,1,1));
            foreach(var size in new[] {new Vector2Int(720,1280),new Vector2Int(1920,1080),new Vector2Int(1024,600)})
            foreach(var text in new[] {DungeonTextSize.Small,DungeonTextSize.Default,DungeonTextSize.Large})
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x,size.y); controller.SetTextSize(text);
                controller.OpenContentCategory("placement.category.trap"); controller.SelectNewContent("placement.option.trap.spike");
                controller.TapWorld(ContentCell(room,1,1));
                for(int i=0;i<12;i++) yield return null;
                var viewport = ui.Q<ScrollView>("contextDetails").contentViewport.worldBound;
                var actions = ui.Q("contentPlacementActions").worldBound;
                Assert.That(viewport.height, Is.GreaterThan(0));
                Assert.That(viewport.yMax, Is.LessThanOrEqualTo(actions.yMin+.5f), size+" "+text+" information/action separation");
                Assert.That(actions.yMax, Is.LessThanOrEqualTo(ui.Q("sessionActions").worldBound.yMin+.5f));
                Assert.That(ui.Q<Button>("cancelContent").worldBound.xMax, Is.LessThanOrEqualTo(ui.Q<Button>("confirmContent").worldBound.xMin+.5f));
                AssertReachable(ui.Q<Button>("cancelContent")); AssertReachable(ui.Q<Button>("confirmContent"));
                AssertReachable(ui.Q<Button>("save")); AssertReachable(ui.Q<Button>("discard"));
                Assert.That(ui.Q<Label>("status").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                Assert.That(ui.Q<Label>("sheetTitle").text, Is.EqualTo(root.Content.GetString("ui.dungeon.content.add_trap",null)));
                var info = ui.Q<Label>("contentPlacementInfo").text;
                Assert.That(info.Split(new[] {root.Content.GetString("ui.dungeon.content.preview",null)},System.StringSplitOptions.None).Length, Is.EqualTo(2));
                Assert.That(info, Does.Not.Contain("Room position").And.Not.Contain("1, 1"));
                if(size.x==1920 && text==DungeonTextSize.Large) { controller.FocusRoom(); yield return RoomContentCapture("room-content-uat-landscape-placement.png"); }
                TestContext.WriteLine(size+" "+text+" viewport="+viewport+" actions="+actions+" session="+ui.Q("sessionActions").worldBound);
                controller.CancelContentPlacement();
            }
            Assert.That(controller.Draft.CommandCount, Is.Zero); AssertCanonicalUnchanged(before,mana);
            Assert.That(controller.Discard(), Is.True); controller.TapWorld(ContentCell(room,0,0));
            Assert.That(ui.Q<Label>("inspectionInfo").text, Does.Not.Contain("Room-local").And.Not.Contain("0, 0"));
        }
        [UnityTest]
        public IEnumerator RoomContentProductionPreviewCustodySaveDiscardAndLayouts()
        {
            var root = GameRoot.Instance;
            Assert.That(root.SaveService.SetQaMana(root.Save,true).IsSuccess,Is.True);
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0]; var room = floor.Layout.Rooms[0];
            var definition = root.ProductionSpatialContent.Catalog.Rooms.Single(r=>r.RoomDefinitionId==room.RoomDefinitionId);
            TileCoordinate Cell(int x,int y)
            {
                Assert.That(RoomLocalCoordinateTransform.TryToFloor(new TileCoordinate(x,y),definition.GrossFootprint,room.Anchor,room.Orientation,out var value),Is.True); return value;
            }
            var before = File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            controller.EnterEdit();
            Assert.That(controller.OpenContentCategory("placement.category.monster"),Is.False);
            Assert.That(ui.Q<Button>("monstersCategory").enabledSelf,Is.False);
            controller.TapWorld(Cell(1,1)); Assert.That(controller.SelectedRoomInstanceId,Is.EqualTo(room.RoomInstanceId));
            Assert.That(ui.Q<Button>("monstersCategory").enabledSelf,Is.True);
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(720,1280);
            yield return null; yield return null;
            yield return RoomContentCapture("room-content-normal-room.png");
            foreach (var choice in new[] {
                new { Category="placement.category.monster",Option="placement.option.monster.goblin",X=1 },
                new { Category="placement.category.trap",Option="placement.option.trap.spike",X=2 },
                new { Category="placement.category.loot_node",Option="placement.option.loot_node.basic",X=3 } })
            {
                Assert.That(controller.OpenContentCategory(choice.Category),Is.True);
                Assert.That(ui.Q("contentChoices").Query<Button>().ToList().All(b=>((string)b.userData).StartsWith("placement.option.")),Is.True,"Browser contains definitions, never assigned identities");
                Assert.That(controller.SelectNewContent(choice.Option),Is.True); int count=controller.Draft.CommandCount;
                controller.TapWorld(Cell(choice.X,1)); Assert.That(controller.ContentPreview.IsValid,Is.True,controller.ContentPreview.Reason);
                Assert.That(controller.Draft.CommandCount,Is.EqualTo(count)); AssertCanonicalUnchanged(before,mana);
                if(choice.X==1) yield return RoomContentCapture("room-content-valid-placement.png");
                Assert.That(controller.ConfirmContentPlacement(),Is.True); yield return null; yield return null;
                Assert.That(controller.Draft.CommandCount,Is.EqualTo(count+1)); AssertCanonicalUnchanged(before,mana);
                Assert.That(controller.SelectedContent.OptionId,Is.EqualTo(choice.Option));
            }
            controller.TapWorld(Cell(1,2)); Assert.That(controller.OpenContentCategory("placement.category.trap"),Is.True);
            Assert.That(controller.SelectNewContent("placement.option.trap.snare"),Is.True);
            controller.TapWorld(Cell(2,1)); Assert.That(controller.ContentPreview.IsValid,Is.False);
            Assert.That(controller.ConfirmContentPlacement(),Is.False); Assert.That(ui.Q<Label>("contentPlacementInfo").text,Is.Not.Empty);
            yield return RoomContentCapture("room-content-invalid-placement.png");
            int commands=controller.Draft.CommandCount; controller.CancelContentPlacement(); Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands));
            controller.TapWorld(Cell(1,1)); string id=controller.SelectedContent.AssignmentId;
            yield return RoomContentCapture("room-content-selection.png");
            controller.BeginMove(); controller.TapWorld(Cell(1,2)); Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands));
            Assert.That(controller.ConfirmContentPlacement(),Is.True); yield return null; yield return null;
            Assert.That(controller.SelectedContent.AssignmentId,Is.EqualTo(id));
            Assert.That(controller.UnassignSelectedContent(),Is.True); yield return null; yield return null;
            Assert.That(controller.Draft.ReadModel.LifecycleAndOwnership.ReturnedContents.Any(a=>a.AssignmentId==id),Is.True);
            Assert.That(controller.SelectReturnedContent(id),Is.True); controller.TapWorld(Cell(1,1));
            Assert.That(controller.ConfirmContentPlacement(),Is.True); yield return null; yield return null;
            Assert.That(controller.SelectedContent.AssignmentId,Is.EqualTo(id));
            foreach(var size in new[] { new Vector2Int(720,1280),new Vector2Int(1920,1080) })
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x,size.y);
                foreach(var text in new[] {DungeonTextSize.Small,DungeonTextSize.Default,DungeonTextSize.Large})
                {
                    controller.SetTextSize(text); controller.TapWorld(Cell(2,2)); controller.OpenContentCategory("placement.category.trap");
                    yield return null; yield return null; yield return null;
                    AssertReachable(ui.Q<Button>("save")); AssertReachable(ui.Q<Button>("discard"));
                    AssertReachable(ui.Q<Button>("monstersCategory")); AssertReachable(ui.Q<Button>("lootCategory"));
                    Assert.That(ui.Q<ScrollView>("contextDetails").worldBound.height,Is.GreaterThan(0));
                    controller.SelectNewContent("placement.option.trap.snare"); controller.TapWorld(Cell(3,2)); yield return null; yield return null;
                    AssertReachable(ui.Q<Button>("cancelContent")); AssertReachable(ui.Q<Button>("confirmContent"));
                    controller.CancelContentPlacement();
                }
            }
            controller.SetTextSize(DungeonTextSize.Default);
            root.SetOnline(false); Assert.That(controller.Commit(),Is.False); AssertCanonicalUnchanged(before,mana); root.SetOnline(true);
            Click(ui.Q<Button>("save")); yield return null; yield return null;
            Assert.That(ui.Q<Label>("modalText").text,Does.Contain("New owned items"));
            yield return RoomContentCapture("room-content-save-discard-review.png");
            Click(ui.Q<Button>("confirm")); yield return null; yield return null;
            Assert.That(controller.IsEditing,Is.False); Assert.That(root.Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments.Length,Is.EqualTo(4));
            Assert.That(root.Save.structureRuntime.ManaReserve,Is.EqualTo(mana-55));
            controller.TapWorld(Cell(2,2)); DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(720,1280);
            yield return null; yield return null;
            yield return RoomContentCapture("room-content-normal-room.png");
            before=File.ReadAllBytes(root.SaveService.SavePath); mana=root.Save.structureRuntime.ManaReserve;
            controller.EnterEdit(); controller.TapWorld(Cell(1,1)); controller.UnassignSelectedContent(); yield return null; yield return null;
            Assert.That(controller.Discard(),Is.True); AssertCanonicalUnchanged(before,mana);
            Assert.That(root.Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments.Any(a=>a.AssignmentId==id),Is.True);
        }
        private IEnumerator RoomContentCapture(string name)
        {
            for(int i=0;i<8;i++) yield return null;
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture(name);
            for(int i=0;i<8;i++) yield return null;
        }
        [UnityTest]
        public IEnumerator RoomContentExplicitReassignmentWithConstructionAndInspectionReuse()
        {
            var root=GameRoot.Instance; Assert.That(root.SaveService.SetQaMana(root.Save,true).IsSuccess,Is.True);
            var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            var floor=root.Save.validatedCanonicalSpatialState.Floors[0]; var original=floor.RoomContents.Assignments.Single();
            controller.EnterEdit(); Assert.That(controller.SelectConstructionRoom("spatial.room.basic"),Is.True);
            controller.TapWorld(controller.World.MoveGuidanceAnchors.First()); Assert.That(controller.ConstructionPreview.IsValid,Is.True);
            Assert.That(controller.ConfirmConstructionPlacement(),Is.True); yield return null; yield return null;
            var destination=controller.Draft.ReadModel.Floors[0].Layout.Rooms.Single(r=>r.RoomInstanceId!=original.RoomInstanceId);
            var source=floor.Layout.Rooms.Single(r=>r.RoomInstanceId==original.RoomInstanceId);
            var definition=root.ProductionSpatialContent.Catalog.Rooms.Single(r=>r.RoomDefinitionId==source.RoomDefinitionId);
            Assert.That(RoomLocalCoordinateTransform.TryToFloor(original.RoomLocalPosition,definition.GrossFootprint,source.Anchor,source.Orientation,out var start),Is.True);
            int rebuilds=controller.World.ReconstructionCount;
            for(int i=0;i<12;i++) { controller.TapWorld(start); controller.FocusRoom(); controller.FitFloor(); }
            Assert.That(controller.World.ReconstructionCount,Is.EqualTo(rebuilds));
            Assert.That(controller.SelectedContent.AssignmentId,Is.EqualTo(original.AssignmentId));
            controller.BeginContentReassignment(); var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            var destinationButton=ui.Q("contentChoices").Query<Button>().ToList().Single(b=>(string)b.userData==destination.RoomInstanceId);
            string roomLabel=string.Format(root.Content.GetString("ui.dungeon.room_selected",null),
                root.ProductionSpatialContent.Languages.Single(t=>t.language=="en").entries.Single(e=>e.key==definition.LocalizationKey).text,
                System.Array.FindIndex(controller.Draft.ReadModel.Floors[0].Layout.Rooms,r=>r.RoomInstanceId==destination.RoomInstanceId)+1);
            Assert.That(destinationButton.text,Does.Contain(roomLabel));
            Click(destinationButton); Assert.That(controller.IsPlacingContent,Is.True,"Existing destination button remains a supported alternative");
            definition=root.ProductionSpatialContent.Catalog.Rooms.Single(r=>r.RoomDefinitionId==destination.RoomDefinitionId);
            Assert.That(RoomLocalCoordinateTransform.TryToFloor(new TileCoordinate(1,1),definition.GrossFootprint,destination.Anchor,destination.Orientation,out var target),Is.True);
            controller.TapWorld(target); Assert.That(controller.ContentPreview.IsValid,Is.True,controller.ContentPreview.Reason);
            Assert.That(controller.Draft.CommandCount,Is.EqualTo(1)); Assert.That(controller.ConfirmContentPlacement(),Is.True);
            yield return null; yield return null; Assert.That(controller.Draft.CommandCount,Is.EqualTo(2));
            Assert.That(controller.SelectedContent.Sequence,Is.EqualTo(original.Sequence)); AssertCanonicalUnchanged(before,mana);
            var quote=root.SaveService.PreviewDungeonDraft(root.Save,controller.Draft); Assert.That(quote.ContentAcquisitions,Is.Zero);
            Assert.That(controller.Commit(),Is.True); yield return null;
            var committed=root.Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments.Single();
            Assert.That(committed.AssignmentId,Is.EqualTo(original.AssignmentId)); Assert.That(committed.RoomInstanceId,Is.EqualTo(destination.RoomInstanceId));
            Assert.That(root.Save.structureRuntime.ManaReserve,Is.EqualTo(mana-quote.Cost));
        }
        [UnityTest]
        public IEnumerator RoomContentMouseTouchPreviewAndResumeKeepCanonicalIsolated()
        {
            var root=GameRoot.Instance; var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(1920,1080); yield return null; yield return null;
            var floor=root.Save.validatedCanonicalSpatialState.Floors[0]; var room=floor.Layout.Rooms[0];
            controller.EnterEdit(); controller.TapWorld(new TileCoordinate(room.Anchor.X+1,room.Anchor.Y+1));
            Assert.That(controller.OpenContentCategory("placement.category.trap"),Is.True); Assert.That(controller.SelectNewContent("placement.option.trap.spike"),Is.True);
            controller.FocusRoom(); yield return WaitForViewportGeometry();
            var camera=controller.GetComponentInChildren<Camera>(); var definition=root.ProductionSpatialContent.Catalog.Rooms.Single(r=>r.RoomDefinitionId==room.RoomDefinitionId);
            Assert.That(definition.TryResolveGrossTiles(room.Anchor,room.Orientation,new SpatialValidationWorkloadLimits(root.SaveSpatialMigrationLimits.Canonical.Spatial.MaximumMaterializedTiles),out var footprint),Is.True);
            var candidates=footprint.OccupiedTiles.Where(c=>controller.World.Select(c)==null).OrderBy(c=>c).Select(c=>new { Cell=c,Screen=(Vector2)camera.WorldToScreenPoint(new Vector3(c.X+.5f,c.Y+.5f,0)) })
                .Where(c=>camera.pixelRect.Contains(c.Screen) && !controller.IsChrome(c.Screen,1)).ToArray();
            Assert.That(candidates.Length,Is.GreaterThan(1));
            Rect beforeMouseViewport=controller.Viewport.ScreenRect;
            Vector3 beforeMouseCamera=camera.transform.position; float beforeMouseSize=camera.orthographicSize;
            var mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
            try
            {
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState {position=candidates[0].Screen}.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                yield return null; yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState {position=candidates[0].Screen}); yield return null; yield return null;
                Assert.That(controller.ContentPreview?.IsValid,Is.True); Assert.That(controller.Draft.CommandCount,Is.Zero);
            }
            finally { UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse); }
            yield return WaitForViewportGeometry();
            var touchCell=candidates[1].Cell;
            var touchWorld=new Vector3(touchCell.X+.5f,touchCell.Y+.5f,0);
            Vector2 touchPoint=camera.WorldToScreenPoint(touchWorld);
            Vector2 roundTrip=controller.Viewport.ScreenToWorld(touchPoint);
            TestContext.WriteLine("Content pointer geometry beforeMouse="+beforeMouseViewport+" afterMouse="+controller.Viewport.ScreenRect+
                " cameraBefore="+beforeMouseCamera+" cameraAfter="+camera.transform.position+" sizeBefore="+beforeMouseSize+" sizeAfter="+camera.orthographicSize+
                " cachedTouch="+candidates[1].Screen+" cachedResolvesTo="+controller.Viewport.ScreenToWorld(candidates[1].Screen)+
                " currentTouch="+touchPoint+" intendedCell="+touchCell.X+","+touchCell.Y);
            Assert.That(roundTrip.x,Is.EqualTo(touchWorld.x).Within(.001f),"Current camera/viewport X round-trip");
            Assert.That(roundTrip.y,Is.EqualTo(touchWorld.y).Within(.001f),"Current camera/viewport Y round-trip");
            Assert.That(camera.pixelRect.Contains(touchPoint),Is.True);
            Assert.That(controller.IsChrome(touchPoint,1),Is.False,"Current touch target must reach the world viewport");
            simulatedTouch=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
            yield return null; yield return null;
            Assert.That(simulatedTouch.enabled,Is.True,"Simulated touchscreen must be active before queuing input");
            int observedTouchId=0; bool observedCurrent=false; Vector2 observedPosition=default;
            System.Action observeTouch=()=>
            {
                // EditMode coroutines can read the Editor buffer after yielding.
                // Observe the same player update/state buffer as the controller.
                if(UnityEngine.InputSystem.LowLevel.InputState.currentUpdateType!=UnityEngine.InputSystem.LowLevel.InputUpdateType.Dynamic) return;
                var active=simulatedTouch.touches.FirstOrDefault(t=>t.press.isPressed);
                if(active==null) return;
                observedTouchId=active.touchId.ReadValue(); observedPosition=active.position.ReadValue();
                observedCurrent=UnityEngine.InputSystem.Touchscreen.current==simulatedTouch;
            };
            UnityEngine.InputSystem.InputSystem.onAfterUpdate+=observeTouch;
            try
            {
                Touch(1,touchPoint,UnityEngine.InputSystem.TouchPhase.Began); yield return null; yield return null;
                TestContext.WriteLine("Content touch coroutineBuffer="+UnityEngine.InputSystem.LowLevel.InputState.currentUpdateType+
                    " playerId="+observedTouchId+" playerPosition="+observedPosition+" currentDevice="+observedCurrent);
                Assert.That(observedCurrent,Is.True); Assert.That(observedTouchId,Is.EqualTo(1));
                Assert.That(Vector2.Distance(observedPosition,touchPoint),Is.LessThan(.001f));
                Touch(1,touchPoint,UnityEngine.InputSystem.TouchPhase.Ended); yield return null; yield return null;
            }
            finally { UnityEngine.InputSystem.InputSystem.onAfterUpdate-=observeTouch; }
            Assert.That(controller.ContentPreview?.IsValid,Is.True,controller.ContentPreview?.Reason); Assert.That(controller.Draft.CommandCount,Is.Zero); AssertCanonicalUnchanged(before,mana);
            Assert.That(controller.World.TryRoomLocal(room.RoomInstanceId,touchCell,out var expectedLocal),Is.True);
            Assert.That(controller.ContentPreview.Assignment.RoomLocalPosition,Is.EqualTo(expectedLocal),"Touch must preview the intended current map cell");
            yield return WaitForViewportGeometry();
            var ui=controller.GetComponent<UIDocument>().rootVisualElement; AssertReachable(ui.Q<Button>("confirmContent"));
            var screen=CompositionButtonScreen(ui.Q<Button>("confirmContent"));
            Touch(1,screen,UnityEngine.InputSystem.TouchPhase.Began); yield return null; yield return null;
            Touch(1,screen,UnityEngine.InputSystem.TouchPhase.Ended); yield return null; yield return null;
            Assert.That(controller.Draft.CommandCount,Is.EqualTo(1)); Assert.That(controller.Draft.Durability,Is.EqualTo(DraftDurability.Acknowledged)); AssertCanonicalUnchanged(before,mana);
            var identity=controller.SelectedContent.AssignmentId;
            yield return RestartShell(); ui=controller.GetComponent<UIDocument>().rootVisualElement;
            Click(ui.Q<Button>("confirm")); yield return null; yield return null;
            Assert.That(controller.IsEditing,Is.True); Assert.That(controller.Draft.ReadModel.Floors[0].RoomContents.Assignments.Any(a=>a.AssignmentId==identity),Is.True);
            Assert.That(controller.Discard(),Is.True); AssertCanonicalUnchanged(before,mana);
        }
    }
}
#endif
