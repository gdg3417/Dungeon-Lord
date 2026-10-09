using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEngine;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0
{
    public sealed partial class ProductionDungeonController
    {
        private bool railCollapsed;
        private string selectedEdgeId;
        public bool DetailsCollapsed => detailsCollapsed;
        public bool FloorRailCollapsed => railCollapsed;
        public string SelectedFloorInstanceId => selectedFloor;
        public RoomContentAssignment SelectedContent => selected;
        public DungeonViewport Viewport => viewport;
        public void FitFloor()
        {
            if (!initialized || !HasCanonicalRuntime()) return;
            autoFit=true; viewport.Reset(); ApplyCamera();
        }
        public void FocusRoom()
        {
            if (!initialized || !HasCanonicalRuntime()) return;
            string id=selectedRoomId ?? selected?.RoomInstanceId;
            if(id==null || !world.TryRoomBounds(id,out var bounds)) return;
            autoFit=false; viewport.Focus(bounds); ApplyCamera();
        }
        public void ToggleFloorRail()
        {
            if(!initialized || !HasCanonicalRuntime()) return;
            railCollapsed=!railCollapsed; safe.EnableInClassList("rail-collapsed",railCollapsed);
            var toggle=document.rootVisualElement.Q<Button>("collapseFloors");
            toggle.text=Text(railCollapsed ? "ui.dungeon.floors.compact" : "ui.dungeon.floors.collapse");
            toggle.tooltip=Text(railCollapsed ? "ui.dungeon.floors.expand" : "ui.dungeon.floors.collapse");
        }
        private string Lifecycle(SavedSpatialFloor floor) => Text(floor.ActivationState==FloorActivationState.Active ?
            "ui.dungeon.floor.active" : "ui.dungeon.floor.inactive");
        private void PresentFloorRail()
        {
            var list=document.rootVisualElement.Q("floors");
            // Floor controls retain identity through presentation refreshes; rebuild only when topology changes.
            var ordered=renderState.Floors.OrderBy(f=>f.FloorIndex).ThenBy(f=>f.FloorInstanceId,StringComparer.Ordinal).ToArray();
            if(list.childCount!=ordered.Length || ordered.Where((f,i)=>list[i].userData as string !=f.FloorInstanceId).Any())
            {
                list.Clear();
                foreach(var floor in ordered)
                {
                    string id=floor.FloorInstanceId;
                    var button=new Button(()=>SelectFloor(id)) { userData=id };
                    button.AddToClassList("floor-button"); list.Add(button);
                }
            }
            for(int i=0;i<ordered.Length;i++)
            {
                var floor=ordered[i]; var button=(Button)list[i];
                bool blocked=draft?.InvalidMovements.Any(v=>v.FloorInstanceId==floor.FloorInstanceId)==true ||
                    draft?.InvalidConstructions.Any(v=>v.Request.FloorInstanceId==floor.FloorInstanceId)==true;
                button.text=Format("ui.dungeon.floor",floor.FloorIndex+1)+"\n"+Lifecycle(floor)+
                    (draft?.FloorChanged(floor.FloorInstanceId)==true ? Text("ui.dungeon.floor_changed") : string.Empty)+
                    (blocked ? Text("ui.dungeon.floor.blocked") : string.Empty);
                button.EnableInClassList("selected-floor",floor.FloorInstanceId==selectedFloor);
                button.EnableInClassList("floor-blocked",blocked);
            }
        }
        private void PresentComposition()
        {
            var ui=document.rootVisualElement;
            bool inspecting=selected!=null || selectedRoomId!=null || selectedEdgeId!=null;
            safe.EnableInClassList("editing",IsEditing);
            safe.EnableInClassList("normal-mode",!IsEditing);
            safe.EnableInClassList("details-collapsed",detailsCollapsed);
            safe.EnableInClassList("blocked-state",IsEditing && (!draft.IsStructurallyValid || draft.Durability!=DraftDurability.Acknowledged));
            ui.Q<Label>("sheetTitle").text=Text(IsConstructing ? "ui.dungeon.sheet.placement" : inspecting ? "ui.dungeon.sheet.inspection" :
                IsEditing ? "ui.dungeon.sheet.edit" : "ui.dungeon.floor_summary");
            ui.Q("floorSummary").style.display=!IsEditing && !inspecting ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Q("inspectionActions").style.display=inspecting && !moveMode ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Q<Button>("move").style.display=IsEditing && selectedEdgeId==null ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Q<Button>("focusRoom").SetEnabled(world.TryRoomBounds(selectedRoomId ?? selected?.RoomInstanceId,out _));
            foreach(string id in new[] {"monstersCategory","trapsCategory","lootCategory"})
            {
                ui.Q<Button>(id).SetEnabled(false);
                ui.Q<Button>(id).tooltip=Text("ui.dungeon.category.deferred");
            }
            if(!IsEditing) status.style.display=feedback==null ? DisplayStyle.None : DisplayStyle.Flex;
            PresentFloorRail();
            var floor=renderState.Floors.SingleOrDefault(f=>f.FloorInstanceId==selectedFloor);
            if(floor==null)
            {
                ui.Q<Label>("floorName").text=Text("ui.dungeon.empty.title");
                ui.Q<Label>("floorInfo").text=Text("ui.dungeon.construction.starter_required"); return;
            }
            var catalog=draftContext.Production.Catalog;
            var definition=catalog.Floors.Single(f=>f.FloorDefinitionId==floor.FloorDefinitionId);
            var result=FloorLayoutValidator.Validate(floor.Layout,definition,catalog.Rooms,catalog.Corridors,
                new SpatialValidationWorkloadLimits(draftContext.Limits.Canonical.Spatial.MaximumMaterializedTiles),
                floor.FixedStructures,catalog.FixedStructures,FloorLayoutValidationMode.ActivationValid);
            ui.Q<Label>("floorName").text=Format("ui.dungeon.floor",floor.FloorIndex+1);
            ui.Q<Label>("floorInfo").text=floor.Layout.Rooms.Length==0 ? Lifecycle(floor)+"\n"+Text("ui.dungeon.construction.starter_required") :
                Format(Screen.width > Screen.height ? "ui.dungeon.floor.info_landscape" : "ui.dungeon.floor.info",Lifecycle(floor),floor.Layout.Rooms.Length,Text(result.IsValid ? "ui.dungeon.layout.ready" : "ui.dungeon.layout.blocked"),
                    result.Capacity.UsedFloorSpaceCapacity,result.Capacity.RemainingFloorSpaceCapacity);
            if(!inspecting) return;
            if(selectedEdgeId!=null)
            {
                var edge=floor.Layout.Edges.Single(e=>e.EdgeId==selectedEdgeId);
                ui.Q<Label>("inspectionInfo").text=Format("ui.dungeon.corridor.info",edge.Footprint.OccupiedTiles.Length,
                    Text(edge.Classification==RouteClassification.Required ? "ui.dungeon.route.required" : "ui.dungeon.route.optional"));
                return;
            }
            var room=floor.Layout.Rooms.Single(r=>r.RoomInstanceId==(selectedRoomId ?? selected.RoomInstanceId));
            var roomDefinition=catalog.Rooms.Single(r=>r.RoomDefinitionId==room.RoomDefinitionId);
            var contents=floor.RoomContents.Assignments.Where(a=>a.RoomInstanceId==room.RoomInstanceId).ToArray();
            ui.Q<Label>("inspectionInfo").text=selected==null ? Format("ui.dungeon.room.info",
                contents.Count(a=>a.CategoryId==CanonicalSpatialSaveContracts.MonsterCategoryId),roomDefinition.MonsterCapacity,
                contents.Count(a=>a.CategoryId==CanonicalSpatialSaveContracts.TrapCategoryId),roomDefinition.TrapCapacity,
                contents.Count(a=>a.CategoryId==CanonicalSpatialSaveContracts.LootNodeCategoryId),roomDefinition.LootCapacity) :
                Format("ui.dungeon.content.info",RoomName(roomDefinition),selected.RoomLocalPosition.X,selected.RoomLocalPosition.Y);
        }
    }
}
