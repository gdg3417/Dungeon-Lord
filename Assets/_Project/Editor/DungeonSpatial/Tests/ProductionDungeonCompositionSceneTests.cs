#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests
{
    public abstract partial class PhaseSevenA4ProductionSceneTests
    {
        // The same authority-seeded state is used for baseline and new captures.
        [UnityTest]
        public IEnumerator CompositionVisualCheckpoint()
        {
            var root = GameRoot.Instance;
            root.Save.structureRuntime.ManaReserve = 1000;
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0];
            var room = floor.Layout.Rooms[0];
            foreach (var option in new[] { "placement.option.trap.spike", "placement.option.loot_node.glittering_hoard" })
            {
                var result = root.SaveService.ExecuteCanonicalMutation(root.Save,
                    DetachedCanonicalMutationRequest.Place(option.Contains("trap") ? "placement.category.trap" : "placement.category.loot_node",
                        option, room.RoomInstanceId, floor.FloorInstanceId, new TileCoordinate(option.Contains("trap") ? 1 : 2, 1)));
                Assert.That(result.IsSuccess, Is.True, result.Reason);
            }
            controller.EnterEdit();
            Assert.That(controller.SelectConstructionRoom("spatial.room.rectangle"), Is.True);
            Assert.That(controller.World.MoveGuidanceAnchors, Is.Not.Empty);
            controller.TapWorld(controller.World.MoveGuidanceAnchors.First());
            Assert.That(controller.ConfirmConstructionPlacement(), Is.True);
            yield return null; yield return null;
            Assert.That(controller.Commit(), Is.True);
            root.Save.completedResearch = new CompletedResearchState { ProjectIds = new[] { "ac_100" } };
            root.Save.structureRuntime.ManaReserve = 1000;
            var floorQuote = root.SaveService.PreviewFloorConstruction(root.Save);
            Assert.That(root.SaveService.CommitFloorConstruction(root.Save, floorQuote).IsSuccess, Is.True);
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            foreach (var size in new[] { new Vector2Int(720,1280), new Vector2Int(1080,1920), new Vector2Int(1920,1080), new Vector2Int(1536,2048) })
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x,size.y);
                controller.SetTextSize(size.x == 1536 ? DungeonTextSize.Large : DungeonTextSize.Default);
                for (int i=0;i<20;i++) yield return null;
                controller.SelectFloor(floor.FloorInstanceId);
                yield return CompositionCapture("normal", size);
                controller.TapWorld(new TileCoordinate(room.Anchor.X+2,room.Anchor.Y+2));
                yield return CompositionCapture("inspection", size);
                typeof(ProductionDungeonController).GetMethod("FocusRoom")?.Invoke(controller,null);
                yield return CompositionCapture("focused", size);
                Click(ui.Q<Button>("reset")); Click(ui.Q<Button>("closeSheet"));
                controller.EnterEdit(); controller.OpenRooms();
                yield return CompositionCapture("edit-rooms", size);
                Assert.That(controller.SelectConstructionRoom("spatial.room.basic"),Is.True);
                if (controller.World.MoveGuidanceAnchors.Length > 0)
                {
                    controller.TapWorld(controller.World.MoveGuidanceAnchors.First());
                    yield return CompositionCapture("placement", size);
                }
                controller.TapWorld(new TileCoordinate(-1,-1));
                yield return CompositionCapture("invalid", size);
                Assert.That(controller.Discard(),Is.True);
            }
        }
        private IEnumerator CompositionCapture(string state, Vector2Int size)
        {
            for(int i=0;i<12;i++) yield return null;
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("composition-"+state+"-"+size.x+"x"+size.y+".png");
            yield return null; yield return null;
        }
        [UnityTest]
        public IEnumerator CompositionAuthoredCorridorInspectionAndOutlineAreReadOnly()
        {
            var root=GameRoot.Instance; root.Save.structureRuntime.ManaReserve=1000;
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(720,1280);
            controller.EnterEdit(); Assert.That(controller.SelectConstructionRoom("spatial.room.rectangle"),Is.True);
            foreach(var anchor in controller.World.MoveGuidanceAnchors)
            {
                controller.TapWorld(anchor);
                if(controller.ConstructionPreview.ConnectionKind==FloorRouteConnectionKind.PhysicalCorridor) break;
            }
            Assert.That(controller.ConstructionPreview.IsValid,Is.True);
            Assert.That(controller.ConstructionPreview.ConnectionKind,Is.EqualTo(FloorRouteConnectionKind.PhysicalCorridor));
            Assert.That(controller.ConfirmConstructionPlacement(),Is.True); yield return null; yield return null;
            Assert.That(controller.Commit(),Is.True);
            for(int i=0;i<12;i++) yield return null;
            var floor=root.Save.validatedCanonicalSpatialState.Floors[0];
            var edge=floor.Layout.Edges.First(e=>e.ConnectionKind==FloorRouteConnectionKind.PhysicalCorridor);
            var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            var center=controller.Viewport.Center; float size=controller.Viewport.Size;
            controller.TapWorld(edge.Footprint.OccupiedTiles[0]); for(int i=0;i<8;i++) yield return null;
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(ui.Q<Label>("inspectionInfo").text,Does.Contain(edge.Footprint.OccupiedTiles.Length.ToString()));
            Assert.That(ui.Q<Button>("move").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
            Assert.That(ui.Q<Button>("focusRoom").enabledSelf,Is.False);
            Assert.That(controller.Viewport.Center,Is.EqualTo(center)); Assert.That(controller.Viewport.Size,Is.EqualTo(size));
            Assert.That(controller.World.PreviewVisible,Is.False,"Corridor silhouette replaces any prior point selection");
            var map=controller.World.transform.Find("RoomSelection").GetComponent<UnityEngine.Tilemaps.Tilemap>();
            foreach(var cell in edge.Footprint.OccupiedTiles)
                Assert.That(map.GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(cell.X,cell.Y,0)).color,Is.EqualTo(controller.presentationPolicy.SelectionColor));
            AssertCanonicalUnchanged(before,mana);
            yield return CompositionCapture("corridor-inspection",new Vector2Int(720,1280));
        }
        [UnityTest]
        public IEnumerator CompositionNormalInspectionFocusRailAndCollapseAreReadOnly()
        {
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(720,1280);
            for(int i=0;i<20;i++) yield return null;
            var root=GameRoot.Instance; var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            var floor=root.Save.validatedCanonicalSpatialState.Floors[0]; var room=floor.Layout.Rooms[0];
            Assert.That(controller.IsEditing,Is.False); Assert.That(controller.World.GridVisible,Is.False);
            Assert.That(ui.Q("floorSummary").resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
            Assert.That(ui.Q<Label>("floorInfo").text,Does.Contain("1 rooms"));
            var center=controller.Viewport.Center; float size=controller.Viewport.Size;
            controller.TapWorld(new TileCoordinate(room.Anchor.X+2,room.Anchor.Y+2));
            for(int i=0;i<8;i++) yield return null;
            Assert.That(controller.SelectedRoomInstanceId,Is.EqualTo(room.RoomInstanceId));
            Assert.That(controller.IsEditing,Is.False); Assert.That(controller.Viewport.Center,Is.EqualTo(center));
            Assert.That(controller.Viewport.Size,Is.EqualTo(size));
            Assert.That(ui.Q("contextSheet").resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
            Assert.That(ui.Q<Button>("move").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
            controller.FocusRoom(); Assert.That(controller.Viewport.Size,Is.LessThan(size));
            Assert.That(controller.SelectedRoomInstanceId,Is.EqualTo(room.RoomInstanceId));
            controller.FitFloor(); Assert.That(controller.Viewport.Size,Is.EqualTo(controller.Viewport.FitSize));
            Click(ui.Q<Button>("closeSheet")); yield return null;
            float expanded=controller.Viewport.ScreenRect.height;
            Click(ui.Q<Button>("collapseDetails")); for(int i=0;i<8;i++) yield return null;
            Assert.That(controller.DetailsCollapsed,Is.True); Assert.That(controller.Viewport.ScreenRect.height,Is.GreaterThan(expanded));
            Click(ui.Q<Button>("collapseDetails")); for(int i=0;i<8;i++) yield return null;
            Assert.That(controller.Viewport.ScreenRect.height,Is.EqualTo(expanded));
            controller.ToggleFloorRail(); for(int i=0;i<8;i++) yield return null;
            Assert.That(controller.FloorRailCollapsed,Is.True); Assert.That(ui.Q("floorList").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
            controller.ToggleFloorRail(); yield return null;
            Assert.That(ui.Q("floorList").resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
            AssertCanonicalUnchanged(before,mana);
        }
        [UnityTest]
        public IEnumerator CompositionLayoutsAndPresentationWorkRemainBounded()
        {
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            var root=GameRoot.Instance; var floor=root.Save.validatedCanonicalSpatialState.Floors[0]; var room=floor.Layout.Rooms[0];
            controller.EnterEdit(); controller.TapWorld(new TileCoordinate(room.Anchor.X+2,room.Anchor.Y+2));
            int commands=controller.Draft.CommandCount;
            foreach(var resolution in new[] {new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1920,1080)})
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(resolution.x,resolution.y);
                for(int i=0;i<12;i++) yield return null;
                foreach(var text in new[] {DungeonTextSize.Small,DungeonTextSize.Default,DungeonTextSize.Large})
                {
                    controller.SetTextSize(text); for(int i=0;i<8;i++) yield return null;
                    Assert.That(controller.SelectedRoomInstanceId,Is.EqualTo(room.RoomInstanceId));
                    Assert.That(ui.Q("bottomChrome").worldBound.xMin,Is.EqualTo(controller.SafeRoot.worldBound.xMin).Within(1));
                    Assert.That(ui.Q("bottomChrome").worldBound.width,Is.EqualTo(controller.SafeRoot.worldBound.width).Within(1));
                    foreach(string name in new[] {"save","discard","focusRoom","reset","collapseDetails"})
                    {
                        var rect=ui.Q<Button>(name).worldBound; var safeRect=controller.SafeRoot.worldBound;
                        Assert.That(rect.yMax,Is.LessThanOrEqualTo(safeRect.yMax+1),name);
                        Assert.That(rect.xMax,Is.LessThanOrEqualTo(safeRect.xMax+1),name);
                        // Panel-to-pixel multiplication can represent exactly 48 as 47.999996.
                        Assert.That(Math.Round(rect.height*controller.GetComponent<UIDocument>().panelSettings.scale,4),Is.GreaterThanOrEqualTo(48),name);
                    }
                }
            }
            int rebuilds=controller.World.ReconstructionCount; int pool=controller.World.PooledEntityCount;
            for(int i=0;i<12;i++)
            {
                controller.FocusRoom(); controller.FitFloor(); Click(ui.Q<Button>("collapseDetails"));
                root.Save.totalTicks++; yield return null;
            }
            Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands));
            Assert.That(controller.World.ReconstructionCount,Is.EqualTo(rebuilds));
            Assert.That(controller.World.PooledEntityCount,Is.EqualTo(pool));
            TestContext.WriteLine("12 focus/fit/sheet/tick cycles: reconstruction="+rebuilds+", pooled renderers="+pool+", draft commands="+commands);
            Assert.That(controller.Discard(),Is.True);
        }
        public IEnumerator CompositionInputSystemInspectionFocusFitAndChrome()
        {
            Assert.That(Application.isPlaying,Is.True);
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(720,1280);
            for(int i=0;i<20;i++) yield return null;
            var root=GameRoot.Instance; var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            var floor=root.Save.validatedCanonicalSpatialState.Floors[0]; var room=floor.Layout.Rooms[0];
            simulatedTouch=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
            // Let the production UI input module bind the newly connected test device.
            yield return null; yield return null;
            var camera=controller.transform.Find("ProductionDungeonCamera").GetComponent<Camera>();
            Vector2 point=camera.WorldToScreenPoint(new Vector3(room.Anchor.X+2.5f,room.Anchor.Y+2.5f));
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            var hitPoint=RuntimePanelUtils.ScreenToPanel(ui.panel,new Vector2(point.x,Screen.height-point.y));
            var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) {position=point},hits);
            TestContext.WriteLine("Normal touch="+point+" screen="+Screen.width+"x"+Screen.height+" panelScale="+controller.GetComponent<UIDocument>().panelSettings.scale+
                " center="+controller.Viewport.Center+" size="+controller.Viewport.Size+" cameraPosition="+camera.transform.position+" cameraSize="+camera.orthographicSize+" cameraAspect="+camera.aspect+
                " viewport="+controller.Viewport.ScreenRect+" camera="+camera.pixelRect+
                " inverse="+controller.Viewport.ScreenToWorld(point)+" picked="+ui.panel.Pick(hitPoint)?.name+" chrome="+controller.IsChrome(point,1)+
                "; raycasts="+string.Join(",",hits.Select(h=>h.gameObject.name+":"+h.module.GetType().Name)));
            Assert.That(controller.IsChrome(point,1),Is.False,"Authored room center must be in the unobscured world viewport");
            Assert.That(controller.Viewport.Size,Is.EqualTo(controller.Viewport.FitSize),"Normal Mode starts with the current floor overview even if starter publication followed empty initialization");
            yield return CompositionTap(point);
            Assert.That(controller.SelectedRoomInstanceId,Is.EqualTo(room.RoomInstanceId)); Assert.That(controller.IsEditing,Is.False);
            float size=controller.Viewport.Size;
            yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("focusRoom")));
            Assert.That(controller.Viewport.Size,Is.LessThan(size));
            yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("reset")));
            Assert.That(controller.Viewport.Size,Is.EqualTo(controller.Viewport.FitSize));
            yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("closeSheet")));
            Assert.That(controller.SelectedRoomInstanceId,Is.Null);
            yield return CompositionTap(camera.WorldToScreenPoint(new Vector3(room.Anchor.X+.5f,room.Anchor.Y+.5f)));
            Assert.That(controller.SelectedContent,Is.Not.Null);
            Assert.That(ui.Q<Label>("selectedName").text,Does.Contain(root.Content.GetString("placement.option.monster.skeleton.display_name",null)));
            yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("legacy")));
            Assert.That(ui.Q("bottomChrome").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
            Assert.That(ui.Q<Button>("legacy").parent,Is.EqualTo(controller.SafeRoot));
            yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("legacy")));
            Assert.That(ui.Q("bottomChrome").resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
            Assert.That(ui.Q<Button>("legacy").parent,Is.EqualTo(ui.Q("sessionActions")));
            AssertCanonicalUnchanged(before,mana);
        }
        public IEnumerator CompositionInputSystemFloorNavigationMousePanAndZoom()
        {
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(720,1280);
            for(int i=0;i<20;i++) yield return null;
            var root=GameRoot.Instance;
            root.Save.completedResearch=new CompletedResearchState {ProjectIds=new[] {"ac_100"}};
            var quote=root.SaveService.PreviewFloorConstruction(root.Save);
            root.Save.structureRuntime.ManaReserve+=quote.Profile.ConstructionMana;
            quote=root.SaveService.PreviewFloorConstruction(root.Save);
            Assert.That(root.SaveService.CommitFloorConstruction(root.Save,quote).IsSuccess,Is.True);
            var state=root.Save.validatedCanonicalSpatialState;
            var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            for(int i=0;i<12;i++) yield return null;
            var floors=ui.Q("floors").Children().Cast<Button>().ToArray();
            Assert.That(floors.Length,Is.EqualTo(2)); Assert.That(floors[0].worldBound.yMax,Is.LessThanOrEqualTo(floors[1].worldBound.yMin));
            simulatedTouch=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
            yield return null; yield return null;
            yield return CompositionTap(CompositionButtonScreen(floors[1]));
            Assert.That(controller.SelectedFloorInstanceId,Is.EqualTo(state.Floors[1].FloorInstanceId));
            Assert.That(controller.World.FloorInstanceId,Is.EqualTo(state.Floors[1].FloorInstanceId));
            yield return CompositionTap(CompositionButtonScreen(floors[0]));
            controller.EnterEdit(); for(int i=0;i<12;i++) yield return null;
            var mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
            try
            {
                Vector2 center=controller.Viewport.ScreenRect.center;
                float size=controller.Viewport.Size;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState {position=center,scroll=new Vector2(0,120)});
                yield return null; yield return null;
                Assert.That(controller.Viewport.Size,Is.LessThan(size));
                var oldCenter=controller.Viewport.Center;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState {position=center}.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                yield return null; yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState {position=center+new Vector2(50,20)}.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                yield return null; yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState {position=center+new Vector2(50,20)});
                yield return null; yield return null;
                Assert.That(controller.Viewport.Center,Is.Not.EqualTo(oldCenter));
                Assert.That(controller.Draft.CommandCount,Is.Zero); AssertCanonicalUnchanged(before,mana);
            }
            finally {UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);}
            int pool=controller.World.PooledEntityCount, rebuilds=controller.World.ReconstructionCount;
            var room=state.Floors[0].Layout.Rooms[0];
            for(int i=0;i<12;i++)
            {
                controller.SelectFloor(state.Floors[0].FloorInstanceId);
                controller.TapWorld(new TileCoordinate(room.Anchor.X+2,room.Anchor.Y+2));
                controller.FocusRoom(); controller.FitFloor(); controller.OpenRooms();
                controller.SelectFloor(state.Floors[1].FloorInstanceId); controller.OpenRooms();
                Click(ui.Q<Button>("collapseDetails")); root.Save.totalTicks++; yield return null;
            }
            Assert.That(controller.World.PooledEntityCount,Is.EqualTo(pool));
            Assert.That(controller.World.ReconstructionCount,Is.EqualTo(rebuilds+24));
            Assert.That(controller.Draft.CommandCount,Is.Zero); AssertCanonicalUnchanged(before,mana);
            TestContext.WriteLine("24 selected-floor reconstructions with selection/category/sheet/tick changes: pooled renderers="+pool+"; no draft commands or canonical writes");
            Assert.That(controller.Discard(),Is.True);
        }
        private Vector2 CompositionButtonScreen(Button button)
        {
            float scale=controller.GetComponent<UIDocument>().panelSettings.scale; var point=button.worldBound.center;
            return new Vector2(point.x*scale,Screen.height-point.y*scale);
        }
        private IEnumerator CompositionTap(Vector2 point)
        {
            Touch(1,point,UnityEngine.InputSystem.TouchPhase.Began); yield return null; yield return null;
            Touch(1,point,UnityEngine.InputSystem.TouchPhase.Ended); yield return null; yield return null;
        }
        [UnityTest]
        public IEnumerator CompositionSpriteReplacementPreservesPositionsIdentitiesAndSaveBytes()
        {
            var root=GameRoot.Instance; var floor=root.Save.validatedCanonicalSpatialState.Floors[0];
            var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            string identities=JsonUtility.ToJson(floor.RoomContents);
            var policy=controller.presentationPolicy; var original=policy.Visuals;
            var replacement=UnityEngine.Object.Instantiate(original);
            try
            {
                replacement.Contents.Single(v=>v.OptionId=="placement.option.monster.skeleton").Sprite=replacement.MonsterFallback;
                policy.Visuals=replacement;
                var ctx=root.SaveService.DungeonDraftContext;
                controller.World.Render(floor,ctx.Production,ctx.Occupancy,ctx.Limits.Canonical.Spatial.MaximumMaterializedTiles,144,false);
                var assignment=floor.RoomContents.Assignments.Single();
                var room=floor.Layout.Rooms.Single(r=>r.RoomInstanceId==assignment.RoomInstanceId);
                var definition=ctx.Production.Catalog.Rooms.Single(r=>r.RoomDefinitionId==room.RoomDefinitionId);
                Assert.That(RoomLocalCoordinateTransform.TryToFloor(assignment.RoomLocalPosition,definition.GrossFootprint,room.Anchor,room.Orientation,out var tile),Is.True);
                var renderer=controller.World.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.name=="Content");
                Assert.That(renderer.sprite,Is.EqualTo(replacement.MonsterFallback));
                Assert.That((Vector2)renderer.transform.localPosition,Is.EqualTo(new Vector2(tile.X+.5f,tile.Y+.5f)));
                Assert.That(controller.World.Select(tile).AssignmentId,Is.EqualTo(assignment.AssignmentId));
                controller.TapWorld(tile);
                Assert.That((Vector2)controller.World.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.name=="Selection").transform.localPosition,
                    Is.EqualTo(new Vector2(tile.X+.5f,tile.Y+.5f)));
                Assert.That(JsonUtility.ToJson(floor.RoomContents),Is.EqualTo(identities));
                AssertCanonicalUnchanged(before,mana);
            }
            finally { policy.Visuals=original; UnityEngine.Object.Destroy(replacement); }
            yield return null;
        }
    }
}
#endif

