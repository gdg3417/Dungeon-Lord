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
            Assert.That(controller.SelectContentDestination(floor.FloorInstanceId,destination.RoomInstanceId),Is.True);
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
            controller.FocusRoom(); yield return null; yield return null;
            var camera=controller.GetComponentInChildren<Camera>(); var definition=root.ProductionSpatialContent.Catalog.Rooms.Single(r=>r.RoomDefinitionId==room.RoomDefinitionId);
            Assert.That(definition.TryResolveGrossTiles(room.Anchor,room.Orientation,new SpatialValidationWorkloadLimits(root.SaveSpatialMigrationLimits.Canonical.Spatial.MaximumMaterializedTiles),out var footprint),Is.True);
            var candidates=footprint.OccupiedTiles.Where(c=>controller.World.Select(c)==null).OrderBy(c=>c).Select(c=>new { Cell=c,Screen=(Vector2)camera.WorldToScreenPoint(new Vector3(c.X+.5f,c.Y+.5f,0)) })
                .Where(c=>camera.pixelRect.Contains(c.Screen) && !controller.IsChrome(c.Screen,1)).ToArray();
            Assert.That(candidates.Length,Is.GreaterThan(1));
            var mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
            try
            {
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState {position=candidates[0].Screen}.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                yield return null; yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState {position=candidates[0].Screen}); yield return null; yield return null;
                Assert.That(controller.ContentPreview?.IsValid,Is.True); Assert.That(controller.Draft.CommandCount,Is.Zero);
            }
            finally { UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse); }
            simulatedTouch=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
            Touch(1,candidates[1].Screen,UnityEngine.InputSystem.TouchPhase.Began); yield return null; yield return null;
            Touch(1,candidates[1].Screen,UnityEngine.InputSystem.TouchPhase.Ended); yield return null; yield return null;
            Assert.That(controller.ContentPreview?.IsValid,Is.True); Assert.That(controller.Draft.CommandCount,Is.Zero); AssertCanonicalUnchanged(before,mana);
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
