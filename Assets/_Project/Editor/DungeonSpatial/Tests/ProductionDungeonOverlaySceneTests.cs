#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MouseButton = UnityEngine.InputSystem.LowLevel.MouseButton;

namespace DungeonBuilder.M0.Tests
{
    public abstract partial class PhaseSevenA4ProductionSceneTests
    {
        [UnityTest]
        public IEnumerator FloorHudStatesAndInputCapture()
        {
            var root=GameRoot.Instance;
            root.Save.completedResearch=new CompletedResearchState {ProjectIds=new[] {"ac_100"}};
            var quote=root.SaveService.PreviewFloorConstruction(root.Save);
            root.Save.structureRuntime.ManaReserve+=quote.Profile.ConstructionMana;
            quote=root.SaveService.PreviewFloorConstruction(root.Save);
            Assert.That(root.SaveService.CommitFloorConstruction(root.Save,quote).IsSuccess,Is.True);
            var state=root.Save.validatedCanonicalSpatialState;
            var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            simulatedTouch=InputSystem.AddDevice<Touchscreen>();
            var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                foreach(var resolution in new[] {new Vector2Int(720,1280),new Vector2Int(1920,1080)})
                {
                    DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(resolution.x,resolution.y);
                    for(int i=0;i<18;i++) yield return null;
                    foreach(var text in new[] {DungeonTextSize.Small,DungeonTextSize.Default,DungeonTextSize.Large})
                    {
                        controller.SetTextSize(text);
                        controller.SelectFloor(state.Floors[0].FloorInstanceId); controller.FitFloor();
                        controller.ApplySafeArea(new Rect(20,35,resolution.x-40,resolution.y-70),resolution);
                        for(int i=0;i<10;i++) yield return null;
                        var floors=ui.Q("floors").Children().Cast<Button>().ToArray();
                        Assert.That(floors[0].worldBound.yMax,Is.LessThanOrEqualTo(floors[1].worldBound.yMin));
                        Assert.That(floors[0].text,Does.Contain(root.Content.GetString("ui.dungeon.floor.active",null)));
                        Assert.That(floors[1].text,Does.Contain(root.Content.GetString("ui.dungeon.floor.inactive",null)));
                        Assert.That(floors[0].ClassListContains("selected-floor"),Is.True);
                        foreach(var button in floors.Append(ui.Q<Button>("collapseFloors"))) UatSafeAction(button);
                        foreach(var button in floors.Append(ui.Q<Button>("collapseFloors"))) AssertFloorHudSurface(button);
                        Assert.That(floors[0].resolvedStyle.borderLeftWidth,Is.GreaterThan(floors[1].resolvedStyle.borderLeftWidth));
                        Assert.That(floors[0].resolvedStyle.unityFontStyleAndWeight,Is.EqualTo(FontStyle.Bold));
                        Assert.That(controller.Viewport.ScreenRect.width,Is.EqualTo(resolution.x-40).Within(1));
                        var rail=ui.Q("floorRail").worldBound;
                        var empty=new Vector2(rail.xMin+1,rail.yMax-1);
                        Assert.That(ui.panel.Pick(empty),Is.EqualTo(ui.Q("viewport")));
                        yield return CompositionCapture("floorhud-normal-"+text,resolution);
                        var room=state.Floors[0].Layout.Rooms[0];
                        controller.TapWorld(new TileCoordinate(room.Anchor.X+2,room.Anchor.Y+2)); controller.FocusRoom();
                        yield return CompositionCapture("floorhud-focused-"+text,resolution);
                        var center=controller.Viewport.Center; var size=controller.Viewport.Size;
                        yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("collapseFloors")));
                        for(int i=0;i<8;i++) yield return null;
                        Assert.That(controller.FloorRailCollapsed,Is.True);
                        Assert.That(controller.Viewport.Center,Is.EqualTo(center)); Assert.That(controller.Viewport.Size,Is.EqualTo(size));
                        UatSafeAction(ui.Q<Button>("collapseFloors"));
                        yield return CompositionCapture("floorhud-collapsed-"+text,resolution);
                        yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("collapseFloors")));
                        for(int i=0;i<8;i++) yield return null;
                        var point=CompositionButtonScreen(floors[1]);
                        InputSystem.QueueStateEvent(mouse,new MouseState {position=point}.WithButton(MouseButton.Left));
                        yield return null; yield return null;
                        InputSystem.QueueStateEvent(mouse,new MouseState {position=point});
                        for(int i=0;i<8;i++) yield return null;
                        Assert.That(controller.SelectedFloorInstanceId,Is.EqualTo(state.Floors[1].FloorInstanceId));
                        Assert.That(floors[1].ClassListContains("selected-floor"),Is.True);
                        yield return CompositionCapture("floorhud-inactive-"+text,resolution);
                        yield return CompositionTap(CompositionButtonScreen(floors[0]));
                        Assert.That(controller.SelectedFloorInstanceId,Is.EqualTo(state.Floors[0].FloorInstanceId));
                        AssertCanonicalUnchanged(before,mana);
                        controller.EnterEdit(); Assert.That(controller.SelectConstructionRoom("spatial.room.basic"),Is.True);
                        controller.TapWorld(new TileCoordinate(-1,-1));
                        Assert.That(controller.ConfirmConstructionPlacement(),Is.True);
                        for(int i=0;i<8;i++) yield return null;
                        Assert.That(floors[0].ClassListContains("floor-blocked"),Is.True);
                        Assert.That(floors[0].text,Does.Contain(root.Content.GetString("ui.dungeon.floor_changed",null)));
                        Assert.That(floors[0].text,Does.Contain(root.Content.GetString("ui.dungeon.floor.blocked",null)));
                        Assert.That(floors[0].resolvedStyle.borderBottomWidth,Is.GreaterThan(0),"Blocked text keeps an additional non-color edge cue");
                        foreach(var button in floors) UatSafeAction(button);
                        yield return CompositionCapture("floorhud-blocked-"+text,resolution);
                        int commands=controller.Draft.CommandCount;
                        yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("collapseFloors")));
                        yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("collapseFloors")));
                        Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands));
                        AssertCanonicalUnchanged(before,mana);
                        Assert.That(controller.Discard(),Is.True);
                    }
                }
            }
            finally { InputSystem.RemoveDevice(mouse); }
        }

        private static void AssertFloorHudSurface(Button button)
        {
            Assert.That(button.resolvedStyle.backgroundImage,Is.EqualTo(default(Background)),"No opaque action frame covers the translucent HUD");
            Assert.That(button.resolvedStyle.backgroundColor.a,Is.InRange(.35f,.85f));
            Assert.That(button.resolvedStyle.opacity,Is.EqualTo(1),"Text and the full control remain opaque");
            Assert.That(button.resolvedStyle.color.a,Is.EqualTo(1));
            Assert.That(button.resolvedStyle.borderRightWidth,Is.Zero,"HUD uses restrained edge accents, not a framed box");
            TestContext.WriteLine("Floor HUD "+button.text.Replace('\n',' ')+": background alpha="+button.resolvedStyle.backgroundColor.a+
                " opacity="+button.resolvedStyle.opacity+" hit="+button.worldBound);
        }

        [UnityTest]
        public IEnumerator OverlayFullWidthSafeAreasAndTransparentInput()
        {
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            var before=File.ReadAllBytes(GameRoot.Instance.SaveService.SavePath); double mana=GameRoot.Instance.Save.structureRuntime.ManaReserve;
            simulatedTouch=InputSystem.AddDevice<Touchscreen>();
            var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                foreach(var resolution in new[] {new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1920,1080),new Vector2Int(1536,2048)})
                {
                    DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(resolution.x,resolution.y);
                    for(int i=0;i<18;i++) yield return null;
                    foreach(var text in new[] {DungeonTextSize.Small,DungeonTextSize.Default,DungeonTextSize.Large})
                    {
                        controller.SetTextSize(text);
                        controller.ApplySafeArea(new Rect(20,35,resolution.x-40,resolution.y-70),resolution);
                        for(int i=0;i<8;i++) yield return null;
                        float scale=controller.GetComponent<UIDocument>().panelSettings.scale;
                        var map=ui.Q("mapArea").worldBound; var viewport=ui.Q("viewport").worldBound;
                        Assert.That(viewport.xMin,Is.EqualTo(map.xMin).Within(.1));
                        Assert.That(viewport.width,Is.EqualTo(map.width).Within(.1));
                        Assert.That(controller.Viewport.ScreenRect.width,Is.EqualTo((resolution.x-40)).Within(1));
                        var camera=controller.GetComponentsInChildren<Camera>().Single();
                        Assert.That(Vector2.Distance(camera.pixelRect.position,controller.Viewport.ScreenRect.position),Is.LessThan(.01));
                        Assert.That(Vector2.Distance(camera.pixelRect.size,controller.Viewport.ScreenRect.size),Is.LessThan(.01));
                        var rail=ui.Q("floorRail").worldBound;
                        Assert.That(rail.xMin,Is.GreaterThanOrEqualTo(map.xMin));
                        Assert.That(ui.Q<Button>("collapseFloors").worldBound.xMax,Is.LessThanOrEqualTo(controller.SafeRoot.worldBound.xMax));
                        var emptyPanel=new Vector2(rail.xMin+1,rail.yMax-1);
                        var emptyScreen=new Vector2(emptyPanel.x*scale,Screen.height-emptyPanel.y*scale);
                        Assert.That(ui.panel.Pick(emptyPanel),Is.EqualTo(ui.Q("viewport")),"Transparent rail padding picks the world");
                        Assert.That(controller.IsChrome(emptyScreen),Is.False);
                        Assert.That(controller.IsChrome(CompositionButtonScreen(ui.Q<Button>("collapseFloors"))),Is.True);
                        controller.FitFloor(); float fit=controller.Viewport.Size;
                        InputSystem.QueueStateEvent(mouse,new MouseState {position=emptyScreen,scroll=new Vector2(0,1)});
                        yield return null; yield return null;
                        Assert.That(controller.Viewport.Size,Is.LessThan(fit),"Wheel passes through empty overlay space");
                        var oldCenter=controller.Viewport.Center;
                        InputSystem.QueueStateEvent(mouse,new MouseState {position=emptyScreen}.WithButton(MouseButton.Left)); yield return null;
                        InputSystem.QueueStateEvent(mouse,new MouseState {position=emptyScreen+new Vector2(35,35)}.WithButton(MouseButton.Left)); yield return null;
                        InputSystem.QueueStateEvent(mouse,new MouseState {position=emptyScreen+new Vector2(35,35)}); yield return null;
                        Assert.That(controller.Viewport.Center,Is.Not.EqualTo(oldCenter),"Pan begins through noninteractive overlay padding");
                        var railExpanded=rail.width; var rect=controller.Viewport.ScreenRect;
                        yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("collapseFloors")));
                        for(int i=0;i<8;i++) yield return null;
                        Assert.That(controller.FloorRailCollapsed,Is.True);
                        Assert.That(Vector2.Distance(controller.Viewport.ScreenRect.position,rect.position),Is.LessThan(.01));
                        Assert.That(Vector2.Distance(controller.Viewport.ScreenRect.size,rect.size),Is.LessThan(.01));
                        Assert.That(railExpanded-ui.Q("floorRail").worldBound.width,Is.GreaterThanOrEqualTo(text==DungeonTextSize.Large ? 67 : 55));
                        Assert.That(ui.Q<Button>("collapseFloors").worldBound.height*scale,Is.GreaterThanOrEqualTo(47.99));
                        TestContext.WriteLine("Overlay "+resolution+" "+text+": full map="+rect+" expanded/collapsed rail="+railExpanded+"/"+ui.Q("floorRail").worldBound.width);
                        yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("collapseFloors")));
                        for(int i=0;i<8;i++) yield return null;
                        Assert.That(controller.FloorRailCollapsed,Is.False);
                        controller.FitFloor();
                        AssertCanonicalUnchanged(before,mana);
                    }
                }
                controller.EnterEdit(); controller.OpenRooms(); yield return null; yield return null;
                int commands=controller.Draft.CommandCount;
                yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("collapseFloors")));
                yield return CompositionTap(CompositionButtonScreen(ui.Q<Button>("collapseFloors")));
                Assert.That(controller.Draft.CommandCount,Is.EqualTo(commands));
                AssertCanonicalUnchanged(before,mana);
            }
            finally { InputSystem.RemoveDevice(mouse); }
        }

        [UnityTest]
        public IEnumerator OverlayCompactFootprintCardsAndRuntimeCaptures()
        {
            var root=GameRoot.Instance; root.Save.structureRuntime.ManaReserve=1000;
            var ui=controller.GetComponent<UIDocument>().rootVisualElement;
            var before=File.ReadAllBytes(root.SaveService.SavePath); double mana=root.Save.structureRuntime.ManaReserve;
            var firstRoom=root.Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms[0];
            foreach(var resolution in new[] {new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1920,1080),new Vector2Int(1536,2048)})
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(resolution.x,resolution.y);
                for(int i=0;i<18;i++) yield return null;
                controller.SetTextSize(DungeonTextSize.Default); controller.FitFloor();
                yield return CompositionCapture("overlay-normal-expanded",resolution);
                controller.ToggleFloorRail(); yield return CompositionCapture("overlay-normal-collapsed",resolution); controller.ToggleFloorRail();
                controller.TapWorld(new TileCoordinate(firstRoom.Anchor.X+2,firstRoom.Anchor.Y+2));
                yield return CompositionCapture("overlay-selected",resolution);
                Click(ui.Q<Button>("closeSheet")); controller.EnterEdit(); controller.OpenRooms();
                foreach(var text in new[] {DungeonTextSize.Small,DungeonTextSize.Default,DungeonTextSize.Large})
                {
                    controller.SetTextSize(text); for(int i=0;i<10;i++) yield return null;
                    var cards=ui.Q("roomChoices").Children().OfType<Button>().ToArray();
                    Assert.That(cards.Length,Is.EqualTo(root.ProductionSpatialContent.Catalog.Rooms.Length));
                    foreach(var card in cards)
                    {
                        var definition=root.ProductionSpatialContent.Catalog.Rooms.Single(r=>r.RoomDefinitionId==(string)card.userData);
                        var preview=card.Q<DungeonRoomFootprintPreview>();
                        definition.TryResolveGrossTiles(new TileCoordinate(0,0),definition.AllowedOrientations.First(),
                            new SpatialValidationWorkloadLimits(root.SaveSpatialMigrationLimits.Canonical.Spatial.MaximumMaterializedTiles),out var footprint);
                        CollectionAssert.AreEquivalent(footprint.OccupiedTiles,preview.Cells);
                        var drawn=preview.RenderedFootprintRect;
                        float ratio=(float)(preview.Cells.Max(c=>c.X)-preview.Cells.Min(c=>c.X)+1)/(preview.Cells.Max(c=>c.Y)-preview.Cells.Min(c=>c.Y)+1);
                        Assert.That(drawn.width/drawn.height,Is.EqualTo(ratio).Within(.001));
                        Assert.That(card.Q<Label>("roomCardCost").text,Does.Contain(PlayerManaPresentationFormatter.FormatDiscreteAmount(
                            root.SaveService.TryRoomConstructionBaseCost(definition.RoomDefinitionId,out var quote) ? quote : -1,System.Globalization.CultureInfo.CurrentCulture)));
                        Assert.That(card.worldBound.width,Is.LessThan(text==DungeonTextSize.Large ? (resolution.x>resolution.y ? 640 : 420) : (resolution.x>resolution.y ? 500 : 320)));
                        Assert.That(card.worldBound.height,Is.LessThan(text==DungeonTextSize.Large ? 150 : 125),"Actual authored labels use compact cards, not smaller hit regions");
                        TestContext.WriteLine("Compact card "+resolution+" "+text+" "+card.userData+": "+card.worldBound+" footprint="+drawn+" cells="+preview.Cells.Count);
                    }
                    Assert.That(cards.Select(c=>c.Q<DungeonRoomFootprintPreview>().RenderedFootprintRect.size).Distinct().Count(),Is.EqualTo(cards.Length));
                    foreach(var card in cards) Assert.That(card.worldBound.height*controller.GetComponent<UIDocument>().panelSettings.scale,Is.GreaterThanOrEqualTo(48));
                    var scroll=ui.Q<ScrollView>("roomChoicesScroll"); scroll.ScrollTo(cards.Last()); for(int i=0;i<8;i++) yield return null;
                    Assert.That(scroll.contentViewport.worldBound.Overlaps(cards.Last().worldBound),Is.True,"Last option remains reachable");
                    scroll.scrollOffset=Vector2.zero;
                    yield return CompositionCapture("overlay-cards-"+text,resolution);
                }
                controller.SetTextSize(DungeonTextSize.Default);
                Assert.That(controller.SelectConstructionRoom("spatial.room.rectangle"),Is.True);
                Assert.That(controller.World.MoveGuidanceAnchors,Is.Not.Empty);
                controller.TapWorld(controller.World.MoveGuidanceAnchors.First());
                Assert.That(controller.ConstructionPreview.IsValid,Is.True);
                yield return CompositionCapture("overlay-placement",resolution);
                controller.TapWorld(new TileCoordinate(-1,-1)); yield return CompositionCapture("overlay-invalid",resolution);
                Assert.That(controller.Discard(),Is.True); AssertCanonicalUnchanged(before,mana);
            }
        }
    }
}
#endif
