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
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            TestContext.WriteLine("Capture "+state+" "+size+": sheet="+ui.Q("bottomChrome").worldBound+" viewport="+controller.Viewport.ScreenRect+" zoom="+controller.Viewport.Size);
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("composition-"+state+"-"+size.x+"x"+size.y+".png");
            yield return null; yield return null;
        }
        [UnityTest]
        public IEnumerator UatContentHeightCardsDisplayAndSafeActions()
        {
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var root = GameRoot.Instance; root.Save.structureRuntime.ManaReserve = 1000;
            var room = root.Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms[0];
            foreach (var size in new[] {new Vector2Int(720,1280), new Vector2Int(1080,1920), new Vector2Int(1920,1080)})
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x,size.y);
                for (int i=0;i<20;i++) yield return null;
                foreach (var text in new[] {DungeonTextSize.Small,DungeonTextSize.Default,DungeonTextSize.Large})
                {
                    controller.SetTextSize(text); for(int i=0;i<10;i++) yield return null;
                    Assert.That(ui.Q("displayPopover").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                    Click(ui.Q<Button>("displaySettings")); for(int i=0;i<3;i++) yield return null;
                    Assert.That(ui.Q("displayPopover").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                    foreach (string name in new[] {"small","default","large"})
                        Assert.That(ui.Q<Button>(name).worldBound.height*controller.GetComponent<UIDocument>().panelSettings.scale,Is.GreaterThanOrEqualTo(47.99));
                    Click(ui.Q<Button>("closeDisplay"));
                    UatMeasureLayout("normal",size,text,ui);
                    float normalHeight=ui.Q("bottomChrome").worldBound.height;
                    Assert.That(normalHeight,Is.LessThan(controller.SafeRoot.worldBound.height*.32f),"Normal sheet must recover the former fixed 32% allocation");
                    controller.TapWorld(new TileCoordinate(room.Anchor.X+2,room.Anchor.Y+2));
                    for(int i=0;i<8;i++) yield return null;
                    UatMeasureLayout("inspection",size,text,ui);
                    Click(ui.Q<Button>("collapseDetails")); for(int i=0;i<8;i++) yield return null;
                    float collapsed=controller.Viewport.ScreenRect.height;
                    UatMeasureLayout("collapsed",size,text,ui);
                    if(text==DungeonTextSize.Large) yield return CompositionCapture("uat-collapsed-large",size);
                    Click(ui.Q<Button>("collapseDetails")); for(int i=0;i<8;i++) yield return null;
                    Assert.That(controller.Viewport.ScreenRect.height,Is.LessThan(collapsed));
                    Click(ui.Q<Button>("closeSheet")); controller.EnterEdit(); controller.OpenRooms();
                    for(int i=0;i<8;i++) yield return null;
                    UatMeasureLayout("edit",size,text,ui);
                    foreach(var card in ui.Q("roomChoices").Children().OfType<Button>())
                    {
                        var name=card.Q<Label>("roomCardName");
                        name.text="A considerably longer localized authored chamber name for layout qualification";
                    }
                    for(int i=0;i<8;i++) yield return null;
                    foreach(var card in ui.Q("roomChoices").Children().OfType<Button>())
                    {
                        var labels=card.Query<Label>().ToList();
                        Assert.That(labels[1].worldBound.yMin,Is.GreaterThanOrEqualTo(labels[0].worldBound.yMax-.1f),"Name remains above size/cost");
                        Assert.That(labels[2].worldBound.yMin,Is.GreaterThanOrEqualTo(labels[0].worldBound.yMax-.1f));
                        Assert.That(labels[2].worldBound.xMin,Is.GreaterThanOrEqualTo(labels[1].worldBound.xMax-.1f),"Dedicated size and cost regions must not overlap");
                        foreach(var label in labels)
                        {
                            Assert.That(label.worldBound.xMax,Is.LessThanOrEqualTo(card.worldBound.xMax+1));
                            Assert.That(label.worldBound.yMax,Is.LessThanOrEqualTo(card.worldBound.yMax+1));
                        }
                        var artwork=card.Q("roomCardPreview").worldBound;
                        Assert.That(artwork.Overlaps(labels[0].worldBound),Is.False,"Dedicated artwork must not overlap the name");
                        TestContext.WriteLine("Card "+size+" "+text+" "+card.userData+": "+card.worldBound+" name="+labels[0].worldBound+" cost="+labels[2].worldBound);
                    }
                    if(text==DungeonTextSize.Large) yield return CompositionCapture("uat-long-cards",size);
                    Assert.That(controller.SelectConstructionRoom("spatial.room.basic"),Is.True);
                    Assert.That(controller.World.MoveGuidanceAnchors,Is.Not.Empty);
                    controller.TapWorld(controller.World.MoveGuidanceAnchors.First());
                    for(int i=0;i<8;i++) yield return null;
                    UatMeasureLayout("placement",size,text,ui);
                    Assert.That(controller.ConstructionPreview.IsValid,Is.True);
                    foreach(string name in new[] {"confirmPlacement","cancelPlacement","save","discard"}) UatSafeAction(ui.Q<Button>(name));
                    controller.TapWorld(new TileCoordinate(-1,-1)); for(int i=0;i<8;i++) yield return null;
                    UatMeasureLayout("invalid",size,text,ui);
                    foreach(string name in new[] {"confirmPlacement","cancelPlacement","save","discard"}) UatSafeAction(ui.Q<Button>(name));
                    Assert.That(controller.Discard(),Is.True);
                    for(int i=0;i<8;i++) yield return null;
                }
            }
        }
        private void UatSafeAction(Button button)
        {
            var safe=controller.SafeRoot.worldBound; var rect=button.worldBound;
            Assert.That(rect.xMin,Is.GreaterThanOrEqualTo(safe.xMin-1),button.name);
            Assert.That(rect.xMax,Is.LessThanOrEqualTo(safe.xMax+1),button.name);
            Assert.That(rect.yMax,Is.LessThanOrEqualTo(safe.yMax+1),button.name);
            Assert.That(rect.height*controller.GetComponent<UIDocument>().panelSettings.scale,Is.GreaterThanOrEqualTo(47.99),button.name);
        }
        private void UatMeasureLayout(string state,Vector2Int size,DungeonTextSize text,VisualElement ui)
        {
            var sheet=ui.Q("bottomChrome").worldBound;
            Assert.That(sheet.yMax,Is.EqualTo(controller.SafeRoot.worldBound.yMax).Within(1),"Bottom anchored in every orientation/state");
            var actions=ui.Q("sessionActions").worldBound;
            Assert.That(sheet.yMax-actions.yMax,Is.LessThanOrEqualTo(9),"No empty tail beneath essential controls");
            TestContext.WriteLine("Layout "+size+" "+text+" "+state+": sheet="+sheet+" viewport="+controller.Viewport.ScreenRect);
        }
        [UnityTest]
        public IEnumerator UatGenuineWheelDisplayAndZoomBounds()
        {
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(1920,1080);
            for(int i=0;i<20;i++) yield return null;
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            var policy=controller.presentationPolicy;
            Assert.That(policy.WheelZoomRatio(1,false),Is.EqualTo(1.25f).Within(.0001));
            Assert.That(policy.WheelZoomRatio(120,true),Is.EqualTo(1.25f).Within(.0001));
            Assert.That(policy.WheelZoomRatio(.1f,false),Is.EqualTo(Mathf.Pow(1.25f,.1f)).Within(.0001));
            TestContext.WriteLine("Wheel unit 1: old ratio="+Mathf.Exp(1/policy.AndroidBaselineDpi)+" new="+policy.WheelZoomRatio(1,false)+"; .1="+policy.WheelZoomRatio(.1f,false)+"; native Windows120="+policy.WheelZoomRatio(120,true));
            var mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
            var settings=UnityEngine.InputSystem.InputSystem.settings;
            var oldBehavior=settings.scrollDeltaBehavior;
            settings.scrollDeltaBehavior=UnityEngine.InputSystem.InputSettings.ScrollDeltaBehavior.UniformAcrossAllPlatforms;
            simulatedTouch=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
            yield return null; yield return null;
            controller.EnterEdit(); for(int i=0;i<8;i++) yield return null;
            int commands=controller.Draft.CommandCount;
            try
            {
                yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("displaySettings")));
                Assert.That(ui.Q("displayPopover").resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
                yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("large")));
                Assert.That(ui.Q<Label>("mode").resolvedStyle.fontSize,Is.EqualTo(policy.LargeText));
                yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("closeDisplay")));
                for(int i=0;i<8;i++) yield return null;
                foreach(float delta in new[] {.1f,1f,-.1f,-1f,100f,-100f})
                {
                    controller.FitFloor();
                    if(delta<0) controller.Viewport.Zoom(2,controller.Viewport.ScreenRect.center);
                    float before=controller.Viewport.Size; var point=controller.Viewport.ScreenRect.center;
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState {position=point,scroll=new Vector2(0,delta)});
                    yield return null;
                    float minimum=Mathf.Max(policy.MinimumViewSize,controller.Viewport.FitSize/policy.MaximumZoomFactor);
                    Assert.That(controller.Viewport.Size,Is.EqualTo(Mathf.Clamp(before/policy.WheelZoomRatio(delta,false),minimum,controller.Viewport.FitSize)).Within(.001));
                    TestContext.WriteLine("Input wheel "+delta+": size "+before+" -> "+controller.Viewport.Size+" min="+minimum+" fit="+controller.Viewport.FitSize);
                    yield return null;
                }
                controller.FitFloor();
                var chrome=CompositionButtonScreen(ui.Q<Button>("reset")); float fit=controller.Viewport.Size;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState {position=chrome,scroll=new Vector2(0,1)});
                yield return null; yield return null;
                Assert.That(controller.Viewport.Size,Is.EqualTo(fit));
                var touchPoint=controller.Viewport.ScreenRect.center;
                Touch(1,touchPoint,UnityEngine.InputSystem.TouchPhase.Began); yield return null; yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState {position=touchPoint,scroll=new Vector2(0,1)});
                yield return null;
                Assert.That(controller.Viewport.Size,Is.EqualTo(fit),"Active touch excludes simultaneous mouse wheel input");
                Touch(1,touchPoint,UnityEngine.InputSystem.TouchPhase.Canceled); yield return null; yield return null;
                var vp=new DungeonViewport(policy); vp.Configure(new Rect(0,0,100,100),new Rect(0,0,1000,1000),true);
                vp.Zoom(2,vp.ScreenRect.center); var anchor=new Vector2(600,550); var world=vp.ScreenToWorld(anchor);
                vp.Zoom(policy.WheelZoomRatio(1,false),anchor);
                Assert.That(Vector2.Distance(world,vp.ScreenToWorld(anchor)),Is.LessThan(.001),"Mouse-centered anchor preserved away from camera clamps");
                Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands));
                controller.FitFloor(); Assert.That(controller.Viewport.Size,Is.EqualTo(controller.Viewport.FitSize));
            }
            finally { settings.scrollDeltaBehavior=oldBehavior; UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse); }
        }
        [UnityTest]
        public IEnumerator UatCorridorVisualCheckpointAndDisplayPopover()
        {
            var root=GameRoot.Instance; root.Save.structureRuntime.ManaReserve=1000;
            controller.EnterEdit(); Assert.That(controller.SelectConstructionRoom("spatial.room.rectangle"),Is.True);
            foreach(var anchor in controller.World.MoveGuidanceAnchors)
            { controller.TapWorld(anchor); if(controller.ConstructionPreview.IsValid && controller.ConstructionPreview.ConnectionKind==FloorRouteConnectionKind.PhysicalCorridor) break; }
            Assert.That(controller.ConstructionPreview.ConnectionKind,Is.EqualTo(FloorRouteConnectionKind.PhysicalCorridor));
            Assert.That(controller.ConfirmConstructionPlacement(),Is.True); yield return null; yield return null;
            Assert.That(controller.Commit(),Is.True);
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            var room=root.Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms[0];
            foreach(var size in new[] {new Vector2Int(720,1280),new Vector2Int(1920,1080)})
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x,size.y);
                controller.SetTextSize(DungeonTextSize.Default); controller.FitFloor();
                yield return CompositionCapture("uat-corridor-normal",size);
                controller.TapWorld(new TileCoordinate(room.Anchor.X+2,room.Anchor.Y+2));
                controller.FocusRoom(); yield return CompositionCapture("uat-corridor-focused",size);
                controller.SetTextSize(DungeonTextSize.Large);
                Click(ui.Q<Button>("displaySettings")); yield return CompositionCapture("uat-display-large",size);
                Click(ui.Q<Button>("closeDisplay")); Click(ui.Q<Button>("closeSheet")); controller.FitFloor();
            }
        }
        [UnityTest]
        public IEnumerator UatRoomBoundariesRespectSavedConnectionsAndRotation()
        {
            var root=GameRoot.Instance; root.Save.structureRuntime.ManaReserve=1000;
            var before=File.ReadAllBytes(root.SaveService.SavePath);
            var catalog=root.ProductionSpatialContent.Catalog;
            var limits=new SpatialValidationWorkloadLimits(root.SaveSpatialMigrationLimits.Canonical.Spatial.MaximumMaterializedTiles);
            foreach(var kind in new[] {FloorRouteConnectionKind.DirectDoorway,FloorRouteConnectionKind.PhysicalCorridor})
            foreach(var savedOrientation in new[] {CardinalOrientation.Zero,CardinalOrientation.Ninety})
            {
                controller.EnterEdit(); Assert.That(controller.SelectConstructionRoom("spatial.room.rectangle"),Is.True);
                Assert.That(controller.SelectConstructionOrientation(savedOrientation),Is.True);
                var authored=catalog.Rooms.Single(r=>r.RoomDefinitionId=="spatial.room.rectangle");
                Assert.That(controller.SelectTerminalConnection(authored.ConnectionPoints.First(p=>StructuralEditService.Rotate(p.Facing,savedOrientation)==CardinalOrientation.Zero).ConnectionPointId),Is.True);
                foreach(var anchor in controller.World.MoveGuidanceAnchors)
                { controller.TapWorld(anchor); if(controller.ConstructionPreview.IsValid && controller.ConstructionPreview.ConnectionKind==kind) break; }
                Assert.That(controller.ConstructionPreview.ConnectionKind,Is.EqualTo(kind));
                Assert.That(controller.ConfirmConstructionPlacement(),Is.True); yield return null; yield return null;
                var floor=controller.Draft.ReadModel.Floors[0];
                var map=controller.World.transform.Find("StonePerimeter").GetComponent<UnityEngine.Tilemaps.Tilemap>();
                var edge=floor.Layout.Edges.Single(e=>floor.Layout.Nodes.Single(n=>n.NodeId==e.SourceNodeId).Kind==FloorRouteNodeKind.Room &&
                    floor.Layout.Nodes.Single(n=>n.NodeId==e.DestinationNodeId).Kind==FloorRouteNodeKind.Room);
                var connected=floor.Layout.Nodes.Where(n=>n.NodeId==edge.SourceNodeId || n.NodeId==edge.DestinationNodeId).Select(n=>n.RoomInstanceId).ToArray();
                var thresholds=controller.World.ConnectionVisuals.Where(v=>v.EdgeId==edge.EdgeId).ToArray();
                Assert.That(thresholds.Length,Is.EqualTo(kind==FloorRouteConnectionKind.DirectDoorway ? 1 : 2));
                foreach(var threshold in thresholds)
                {
                    Assert.That(threshold.Kind,Is.EqualTo(kind));
                    Assert.That(Mathf.Abs(threshold.Center.x-threshold.Socket.X-.5f)+Mathf.Abs(threshold.Center.y-threshold.Socket.Y-.5f),Is.EqualTo(.5f));
                    Assert.That(floor.Layout.Rooms.Any(r=>catalog.Rooms.Single(d=>d.RoomDefinitionId==r.RoomDefinitionId).ConnectionPoints.Any(p=> {
                        var definition=catalog.Rooms.Single(d=>d.RoomDefinitionId==r.RoomDefinitionId);
                        var local=StructuralEditService.TransformConnectionPointOffset(p.Offset,r.Orientation,definition.GrossFootprint);
                        return threshold.Socket.Equals(new TileCoordinate(r.Anchor.X+local.X,r.Anchor.Y+local.Y)) && threshold.Facing==StructuralEditService.Rotate(p.Facing,r.Orientation);
                    })),Is.True,"Threshold resolves an actual transformed authored room socket");
                }
                Assert.That(controller.World.ConnectionVisuals.Any(v=>floor.Layout.Edges.Any(e=>e.EdgeId==v.EdgeId &&
                    floor.Layout.Nodes.Any(n=>n.NodeId==e.SourceNodeId && n.Kind==FloorRouteNodeKind.Entrance))),Is.True,"Verified entrance edge has an open threshold");
                var cells=connected.SelectMany(id=> {
                    var room=floor.Layout.Rooms.Single(r=>r.RoomInstanceId==id);
                    catalog.Rooms.Single(d=>d.RoomDefinitionId==room.RoomDefinitionId).TryResolveGrossTiles(room.Anchor,room.Orientation,limits,out var f);
                    return f.OccupiedTiles;
                }).ToArray();
                var sprites=cells.Select(c=>map.GetSprite(new Vector3Int(c.X,c.Y,0))).ToArray();
                controller.World.SelectRoomFootprint(connected[0]);
                var selectedMap=controller.World.transform.Find("RoomSelection").GetComponent<UnityEngine.Tilemaps.Tilemap>();
                var selectedInstance=floor.Layout.Rooms.Single(r=>r.RoomInstanceId==connected[0]);
                catalog.Rooms.Single(d=>d.RoomDefinitionId==selectedInstance.RoomDefinitionId).TryResolveGrossTiles(selectedInstance.Anchor,selectedInstance.Orientation,limits,out var selectedFootprint);
                foreach(var cell in selectedFootprint.OccupiedTiles)
                {
                    var tile=new Vector3Int(cell.X,cell.Y,0); var lip=map.GetSprite(tile);
                    int mask=Array.IndexOf(controller.presentationPolicy.Visuals.Boundaries,lip);
                    Assert.That(selectedMap.GetSprite(tile),Is.EqualTo(lip==null ? null : controller.presentationPolicy.Visuals.SelectedEdges[mask]),"Selected outlines preserve the same doorway openings as stone lips");
                }
                floor.Layout.Edges=floor.Layout.Edges.Where(e=>e.EdgeId!=edge.EdgeId).ToArray();
                controller.World.Render(floor,root.ProductionSpatialContent,root.SaveService.DungeonDraftContext.Occupancy,
                    root.SaveSpatialMigrationLimits.Canonical.Spatial.MaximumMaterializedTiles,1000,false);
                int changed=cells.Where((c,i)=>sprites[i]!=map.GetSprite(new Vector3Int(c.X,c.Y,0))).Count();
                Assert.That(controller.World.ConnectionVisuals.Any(v=>v.EdgeId==edge.EdgeId),Is.False,"Removed saved connection removes its threshold");
                var invalidFloor=JsonUtility.FromJson<SavedSpatialFloor>(JsonUtility.ToJson(controller.Draft.ReadModel.Floors[0]));
                invalidFloor.Layout.Edges.Single(e=>e.EdgeId==edge.EdgeId).DestinationNodeId="test.invalid.endpoint";
                controller.World.Render(invalidFloor,root.ProductionSpatialContent,root.SaveService.DungeonDraftContext.Occupancy,
                    root.SaveSpatialMigrationLimits.Canonical.Spatial.MaximumMaterializedTiles,1000,false);
                Assert.That(controller.World.ConnectionVisuals.Any(v=>v.EdgeId==edge.EdgeId),Is.False,"Invalid saved endpoint cannot project an opening");
                Assert.That(changed,Is.EqualTo(2),"Only the two saved endpoint sockets open room boundaries");
                TestContext.WriteLine("Boundary "+kind+": exactly two authorized endpoint openings; removed edge closes them");
                controller.World.Render(controller.Draft.ReadModel.Floors[0],root.ProductionSpatialContent,root.SaveService.DungeonDraftContext.Occupancy,
                    root.SaveSpatialMigrationLimits.Canonical.Spatial.MaximumMaterializedTiles,1000,false);
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(1080,1920);
                for(int i=0;i<12;i++) yield return null;
                controller.FitFloor(); yield return CompositionCapture("overlay-boundary-"+kind+"-"+savedOrientation,new Vector2Int(1080,1920));
                controller.TapWorld(new TileCoordinate(selectedInstance.Anchor.X+1,selectedInstance.Anchor.Y+1)); controller.FocusRoom();
                yield return CompositionCapture("overlay-boundary-focused-"+kind+"-"+savedOrientation,new Vector2Int(1080,1920));
                Assert.That(controller.Discard(),Is.True);
            }
            // Detached presentation fixtures deliberately omit graph edges: physical contact alone
            // must never expose a doorway. No invalid fixture is published to the canonical save.
            foreach(var orientation in new[] {CardinalOrientation.Zero,CardinalOrientation.Ninety,CardinalOrientation.OneEighty,CardinalOrientation.TwoSeventy})
            {
                var floor=JsonUtility.FromJson<SavedSpatialFloor>(JsonUtility.ToJson(root.Save.validatedCanonicalSpatialState.Floors[0]));
                var original=floor.Layout.Rooms[0]; original.RoomDefinitionId="spatial.room.rectangle"; original.Anchor=new TileCoordinate(1,1); original.Orientation=orientation;
                var definition=catalog.Rooms.Single(d=>d.RoomDefinitionId==original.RoomDefinitionId);
                definition.TryResolveGrossTiles(original.Anchor,orientation,limits,out var footprint);
                var other=JsonUtility.FromJson<RoomSpatialInstance>(JsonUtility.ToJson(original)); other.RoomInstanceId="test.detached.touching";
                other.Anchor=new TileCoordinate(footprint.OccupiedTiles.Max(c=>c.X)+1,original.Anchor.Y);
                floor.Layout.Rooms=new[] {original,other}; floor.Layout.Edges=Array.Empty<FloorRouteEdge>();
                floor.Layout.Nodes=Array.Empty<FloorRouteNode>(); floor.FixedStructures=Array.Empty<SavedFixedSpatialStructure>();
                floor.RoomContents.Assignments=Array.Empty<RoomContentAssignment>();
                controller.World.Render(floor,root.ProductionSpatialContent,root.SaveService.DungeonDraftContext.Occupancy,root.SaveSpatialMigrationLimits.Canonical.Spatial.MaximumMaterializedTiles,1000,false);
                var map=controller.World.transform.Find("StonePerimeter").GetComponent<UnityEngine.Tilemaps.Tilemap>();
                var seam=new TileCoordinate(footprint.OccupiedTiles.Max(c=>c.X),original.Anchor.Y+1);
                Assert.That(map.GetSprite(new Vector3Int(seam.X,seam.Y,0)),Is.EqualTo(controller.presentationPolicy.Visuals.Boundaries[2]),"Touching rooms retain a continuous east lip without a graph connection");
                Assert.That(controller.World.ConnectionVisuals,Is.Empty,"Physical contact cannot create a threshold");
                if(orientation==CardinalOrientation.Zero)
                {
                    controller.Viewport.Configure(controller.World.Bounds,controller.Viewport.ScreenRect,true);
                    typeof(ProductionDungeonController).GetMethod("ApplyCamera",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,null);
                    yield return CompositionCapture("overlay-touching-no-edge",new Vector2Int(1080,1920));
                }
                controller.World.SelectRoomFootprint(original.RoomInstanceId);
                Assert.That(controller.World.transform.Find("RoomSelection").GetComponent<UnityEngine.Tilemaps.Tilemap>().GetTile(new Vector3Int(seam.X,seam.Y,0)),Is.Not.Null);
                other.Anchor=new TileCoordinate(other.Anchor.X,other.Anchor.Y+1);
                controller.World.Render(floor,root.ProductionSpatialContent,root.SaveService.DungeonDraftContext.Occupancy,root.SaveSpatialMigrationLimits.Canonical.Spatial.MaximumMaterializedTiles,1000,false);
                Assert.That(map.GetSprite(new Vector3Int(seam.X,seam.Y,0)),Is.EqualTo(controller.presentationPolicy.Visuals.Boundaries[2]),"Partial-edge adjacency also preserves the room seam");
                Assert.That(controller.World.ConnectionVisuals,Is.Empty);
                other.Anchor=new TileCoordinate(other.Anchor.X+2,other.Anchor.Y);
                controller.World.Render(floor,root.ProductionSpatialContent,root.SaveService.DungeonDraftContext.Occupancy,root.SaveSpatialMigrationLimits.Canonical.Spatial.MaximumMaterializedTiles,1000,false);
                Assert.That(map.GetSprite(new Vector3Int(seam.X,seam.Y,0)),Is.EqualTo(controller.presentationPolicy.Visuals.Boundaries[2]),"Separated rooms retain their own perimeter");
                Assert.That(controller.World.transform.Find("Corridors").GetComponent<UnityEngine.Tilemaps.Tilemap>().GetTile(new Vector3Int(seam.X+1,seam.Y,0)),Is.Null,"No decorative corridor invented in the gap");
            }
            CollectionAssert.AreEqual(before,File.ReadAllBytes(root.SaveService.SavePath));
            yield return null;
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
            Assert.That(ui.Q<Label>("floorInfo").text,Does.Contain("Rooms: 1"));
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
            float railExpandedWidth=controller.Viewport.ScreenRect.width;
            yield return CompositionCapture("review-rail-expanded",new Vector2Int(720,1280));
            controller.ToggleFloorRail(); for(int i=0;i<8;i++) yield return null;
            Assert.That(controller.FloorRailCollapsed,Is.True); Assert.That(ui.Q("floorList").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
            Assert.That(controller.Viewport.ScreenRect.width,Is.EqualTo(railExpandedWidth).Within(1),"Overlay collapse does not resize the full-width world");
            Assert.That(ui.Q("floorRail").worldBound.width,Is.LessThan(100));
            TestContext.WriteLine("Floor rail viewport width: expanded="+railExpandedWidth+", collapsed="+controller.Viewport.ScreenRect.width);
            yield return CompositionCapture("review-rail-collapsed",new Vector2Int(720,1280));
            controller.ToggleFloorRail(); yield return null;
            Assert.That(ui.Q("floorList").resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
            AssertCanonicalUnchanged(before,mana);
        }
        [UnityTest]
        public IEnumerator CompositionFloorLayoutSummaryDoesNotMisreportRequiredRoute()
        {
            var root=GameRoot.Instance;
            var floor=root.Save.validatedCanonicalSpatialState.Floors[0];
            var contextField=typeof(ProductionDungeonController).GetField("draftContext",BindingFlags.Instance|BindingFlags.NonPublic);
            var context=(DungeonDraftContext)contextField.GetValue(controller);
            var catalog=context.Production.Catalog;
            var definition=catalog.Floors.Single(f=>f.FloorDefinitionId==floor.FloorDefinitionId);
            var limits=new SpatialValidationWorkloadLimits(context.Limits.Canonical.Spatial.MaximumMaterializedTiles);
            var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            var valid=FloorLayoutValidator.Validate(floor.Layout,definition,catalog.Rooms,catalog.Corridors,limits,
                floor.FixedStructures,catalog.FixedStructures,FloorLayoutValidationMode.ActivationValid);
            Assert.That(valid.IsValid,Is.True,"Seeded required route and layout are valid");
            int capacity=definition.FinalFloorSpaceCapacity;
            try
            {
                // Fixture-only config blocker: route nodes/edges/geometry are unchanged.
                definition.FinalFloorSpaceCapacity=0;
                var blocked=FloorLayoutValidator.Validate(floor.Layout,definition,catalog.Rooms,catalog.Corridors,limits,
                    floor.FixedStructures,catalog.FixedStructures,FloorLayoutValidationMode.ActivationValid);
                Assert.That(blocked.Issues.Select(i=>i.Reason),Is.Not.Empty.And.All.EqualTo(FloorLayoutValidationReason.CapacityExceeded));
                // Snapshots clone their catalogs on read. Inject a fixture-only snapshot
                // into the presentation context; never alter production/canonical authority.
                var fixtureProduction=new ProductionSpatialContentSnapshot(context.Production.Manifest,catalog,context.Production.Languages);
                contextField.SetValue(controller,new DungeonDraftContext(fixtureProduction,context.Configuration,context.Occupancy,context.Limits,context.Compatibility));
                typeof(ProductionDungeonController).GetMethod("PresentComposition",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,null);
                var label=controller.GetComponent<UIDocument>().rootVisualElement.Q<Label>("floorInfo").text;
                Assert.That(label,Does.Contain("Floor layout: Blocked").And.Not.Contain("Required route:"));
                AssertCanonicalUnchanged(before,mana);
            }
            finally { definition.FinalFloorSpaceCapacity=capacity; contextField.SetValue(controller,context); }
            typeof(ProductionDungeonController).GetMethod("PresentComposition",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,null);
            Assert.That(controller.GetComponent<UIDocument>().rootVisualElement.Q<Label>("floorInfo").text,Does.Contain("Floor layout: Valid"));
            yield return null;
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
                    float expandedWidth=controller.Viewport.ScreenRect.width;
                    float expandedRail=ui.Q("floorRail").worldBound.width;
                    Assert.That(expandedWidth,Is.EqualTo(controller.SafeRoot.worldBound.width*controller.GetComponent<UIDocument>().panelSettings.scale).Within(1));
                    controller.ToggleFloorRail(); for(int i=0;i<8;i++) yield return null;
                    float recovery=controller.Viewport.ScreenRect.width-expandedWidth;
                    Assert.That(recovery,Is.EqualTo(0).Within(1),"Floor overlay never reserves a viewport column");
                    Assert.That(expandedRail-ui.Q("floorRail").worldBound.width,Is.GreaterThanOrEqualTo(text==DungeonTextSize.Large ? 67 : 55),"Collapse recovers unobscured overlay space");
                    var toggleRect=ui.Q<Button>("collapseFloors").worldBound;
                    var toggleButton=ui.Q<Button>("collapseFloors");
                    Assert.That(toggleButton.MeasureTextSize(toggleButton.text,0,TextElement.MeasureMode.Undefined,0,TextElement.MeasureMode.Undefined).x,
                        Is.LessThanOrEqualTo(toggleButton.contentRect.width+1),"Compact localized expand label fits without splitting a word");
                    Assert.That(Math.Round(toggleRect.width*controller.GetComponent<UIDocument>().panelSettings.scale,4),Is.GreaterThanOrEqualTo(48));
                    Assert.That(toggleRect.xMin,Is.GreaterThanOrEqualTo(controller.SafeRoot.worldBound.xMin-1));
                    TestContext.WriteLine("Rail "+resolution+" "+text+": viewport "+expandedWidth+" -> "+controller.Viewport.ScreenRect.width+", hit width="+toggleRect.width);
                    if(text==DungeonTextSize.Large) yield return CompositionCapture("review-rail-collapsed-large",resolution);
                    controller.ToggleFloorRail(); for(int i=0;i<8;i++) yield return null;
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
            float expandedRailViewport=controller.Viewport.ScreenRect.width;
            var railCamera=controller.Viewport.Center;
            yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("collapseFloors")));
            for(int i=0;i<8;i++) yield return null;
            Assert.That(controller.FloorRailCollapsed,Is.True);
            Assert.That(controller.Viewport.ScreenRect.width,Is.EqualTo(expandedRailViewport).Within(1));
            Assert.That(ui.Q("floorRail").worldBound.width,Is.LessThan(100));
            yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("collapseFloors")));
            for(int i=0;i<8;i++) yield return null;
            Assert.That(controller.FloorRailCollapsed,Is.False);
            Assert.That(controller.Viewport.Center,Is.EqualTo(railCamera));
            AssertCanonicalUnchanged(before,mana);
            yield return CompositionTap(CompositionButtonScreen(floors[1]));
            Assert.That(controller.SelectedFloorInstanceId,Is.EqualTo(state.Floors[1].FloorInstanceId));
            Assert.That(controller.World.FloorInstanceId,Is.EqualTo(state.Floors[1].FloorInstanceId));
            yield return CompositionTap(CompositionButtonScreen(floors[0]));
            controller.EnterEdit(); for(int i=0;i<12;i++) yield return null;
            var mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
            try
            {
                var toggle=ui.Q<Button>("collapseFloors");
                foreach(bool collapsed in new[] {true,false})
                {
                    var point=CompositionButtonScreen(toggle);
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState {position=point}.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                    yield return null; yield return null;
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState {position=point});
                    for(int i=0;i<8;i++) yield return null;
                    Assert.That(controller.FloorRailCollapsed,Is.EqualTo(collapsed));
                    Assert.That(controller.Draft.CommandCount,Is.Zero);
                }
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

