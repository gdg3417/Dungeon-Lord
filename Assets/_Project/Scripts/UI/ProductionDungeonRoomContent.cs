using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0
{
    public sealed partial class ProductionDungeonController
    {
        private string contentCategory;
        private string reassigning;
        private DungeonDraftRoomContent contentIntent;
        private DungeonDraftContentPreview contentPreview;
        public DungeonDraftContentPreview ContentPreview => contentPreview;
        public bool IsPlacingContent => IsEditing && contentIntent != null;
        public string ContentCategory => contentCategory;
        private string ContextRoomId => selectedRoomId ?? selected?.RoomInstanceId;

        private void InitializeRoomContent()
        {
            Button("monstersCategory", () => OpenContentCategory(MvpDungeonPlacementIds.MonsterCategoryId));
            Button("trapsCategory", () => OpenContentCategory(MvpDungeonPlacementIds.TrapCategoryId));
            Button("lootCategory", () => OpenContentCategory(MvpDungeonPlacementIds.LootNodeCategoryId));
            Button("confirmContent", () => ConfirmContentPlacement());
            Button("cancelContent", CancelContentPlacement);
            Button("unassignContent", () => UnassignSelectedContent());
            Button("reassignContent", BeginContentReassignment);
        }
        private void ClearContentSelection()
        { contentCategory = null; reassigning = null; contentIntent = null; contentPreview = null; }

        public bool OpenContentCategory(string category)
        {
            if (!HasCanonicalRuntime() || !IsEditing || ContextRoomId == null ||
                category != MvpDungeonPlacementIds.MonsterCategoryId && category != MvpDungeonPlacementIds.TrapCategoryId && category != MvpDungeonPlacementIds.LootNodeCategoryId)
                return false;
            var room = renderState.Floors.SingleOrDefault(f => f.FloorInstanceId == selectedFloor)?.Layout.Rooms.SingleOrDefault(r => r.RoomInstanceId == ContextRoomId);
            if (room == null) return false;
            string id = room.RoomInstanceId;
            ClearConstructionSelection(); ClearContentSelection(); selected = null; selectedRoomId = id; selectedEdgeId = null; moveMode = false;
            contentCategory = category; detailsCollapsed = false; OpenSheet(); world.SelectRoomFootprint(id);
            feedback = "ui.dungeon.content.choose"; Present(); return true;
        }
        public bool SelectNewContent(string option)
        {
            if (!IsEditing || contentCategory == null || ContextRoomId == null || !root.SaveService.TryContentAcquisitionCost(contentCategory, option, out _)) return false;
            contentIntent = draftContext.ContentIntent(DungeonDraftContentOperation.Acquire, selectedFloor, ContextRoomId,
                categoryId: contentCategory, optionId: option);
            contentPreview = null; feedback = "ui.dungeon.content.position_hint"; Present(); return true;
        }
        public bool SelectReturnedContent(string id)
        {
            if (!IsEditing || contentCategory == null || ContextRoomId == null ||
                !draft.ReadModel.LifecycleAndOwnership.ReturnedContents.Any(a => a.AssignmentId == id && a.CategoryId == contentCategory)) return false;
            contentIntent = draftContext.ContentIntent(DungeonDraftContentOperation.Redeploy, selectedFloor, ContextRoomId, id);
            contentPreview = null; feedback = "ui.dungeon.content.position_hint"; Present(); return true;
        }
        private void BeginContentReposition()
        {
            string room = selected.RoomInstanceId, category = selected.CategoryId, id = selected.AssignmentId;
            OpenContentCategory(category);
            contentIntent = draftContext.ContentIntent(DungeonDraftContentOperation.Reposition, selectedFloor, room, id);
            feedback = "ui.dungeon.content.position_hint"; Present();
        }
        public void BeginContentReassignment()
        {
            if (!IsEditing || selected == null) return;
            string id = selected.AssignmentId, category = selected.CategoryId;
            OpenContentCategory(category); reassigning = id; feedback = "ui.dungeon.content.destination_hint"; Present();
        }
        public bool SelectContentDestination(string floorId, string roomId)
        {
            if (!IsEditing || reassigning == null) return false;
            string id = reassigning, category = contentCategory;
            var state = draft.ReadModel;
            if (!state.Floors.Any(f => f.FloorInstanceId == floorId && f.Layout.Rooms.Any(r => r.RoomInstanceId == roomId))) return false;
            if (selectedFloor != floorId) SelectFloor(floorId);
            selectedRoomId = roomId; selected = null; selectedEdgeId = null; contentCategory = category; reassigning = null;
            contentIntent = draftContext.ContentIntent(DungeonDraftContentOperation.Reassign, floorId, roomId, id);
            contentPreview = null; detailsCollapsed = false; OpenSheet(); world.SelectRoomFootprint(roomId);
            feedback = "ui.dungeon.content.position_hint"; Present(); return true;
        }
        private void PreviewRoomContent(TileCoordinate cell)
        {
            if (!world.TryRoomLocal(contentIntent.RoomInstanceId, cell, out var local))
            {
                contentPreview = new DungeonDraftContentPreview { Reason = DetachedCanonicalSpatialMutation.PositionInvalidReason };
                world.Preview(cell, false);
            }
            else
            {
                contentIntent.RoomLocalPosition = local;
                contentPreview = draft.PreviewContent(contentIntent);
                string category = contentIntent.CategoryId ?? contentPreview.Assignment?.CategoryId;
                string option = contentIntent.OptionId ?? contentPreview.Assignment?.OptionId;
                if (category == null)
                {
                    var owned = draft.ReadModel.Floors.SelectMany(f => f.RoomContents.Assignments).FirstOrDefault(a => a.AssignmentId == contentIntent.AssignmentId);
                    var returned = draft.ReadModel.LifecycleAndOwnership.ReturnedContents.FirstOrDefault(a => a.AssignmentId == contentIntent.AssignmentId);
                    category = owned?.CategoryId ?? returned?.CategoryId; option = owned?.OptionId ?? returned?.OptionId;
                }
                world.PresentContentPreview(contentIntent.RoomInstanceId, category, option, local, contentPreview.IsValid);
            }
            feedback = contentPreview.Reason ?? "ui.dungeon.content.preview"; Present();
        }
        public bool ConfirmContentPlacement()
        {
            if (!HasCanonicalRuntime() || !IsPlacingContent || contentPreview?.IsValid != true) return false;
            if (!draft.ChangeContent(contentIntent)) { feedback = draft.Reason; Present(); return false; }
            string id = contentPreview.Assignment.AssignmentId;
            contentIntent = null; contentPreview = null; reassigning = null;
            RebuildFloor(false); selected = renderState.Floors.SelectMany(f => f.RoomContents.Assignments).Single(a => a.AssignmentId == id);
            selectedRoomId = null; contentCategory = null; OpenSheet(); world.SelectContent(selected);
            feedback = "ui.dungeon.content.confirmed"; Present(); return true;
        }
        public void CancelContentPlacement()
        {
            contentIntent = null; contentPreview = null; reassigning = null; world?.ClearPreview();
            if (ContextRoomId != null) world.SelectRoomFootprint(ContextRoomId);
            feedback = "ui.dungeon.content.choose"; Present();
        }
        public bool UnassignSelectedContent()
        {
            if (!HasCanonicalRuntime() || !IsEditing || selected == null) return false;
            string room = selected.RoomInstanceId, category = selected.CategoryId;
            var intent = draftContext.ContentIntent(DungeonDraftContentOperation.Unassign, selectedFloor, assignmentId: selected.AssignmentId);
            if (!draft.ChangeContent(intent)) { feedback = draft.Reason; Present(); return false; }
            RebuildFloor(false); selected = null; selectedRoomId = room; OpenContentCategory(category);
            feedback = "ui.dungeon.content.returned"; Present(); return true;
        }
        private string ContentName(string option) => MvpDungeonPlacementPresenter.ResolveOptionName(option, (key, fallback) => root.Content.GetString(key, fallback));
        private void PresentRoomContent()
        {
            var ui = document.rootVisualElement;
            bool managing = IsEditing && contentCategory != null && ContextRoomId != null;
            ui.Q<Button>("roomsCategory").EnableInClassList("selected-floor", contentCategory == null);
            ui.Q<Button>("monstersCategory").EnableInClassList("selected-floor", contentCategory == MvpDungeonPlacementIds.MonsterCategoryId);
            ui.Q<Button>("trapsCategory").EnableInClassList("selected-floor", contentCategory == MvpDungeonPlacementIds.TrapCategoryId);
            ui.Q<Button>("lootCategory").EnableInClassList("selected-floor", contentCategory == MvpDungeonPlacementIds.LootNodeCategoryId);
            ui.Q("roomContentPanel").style.display = managing ? DisplayStyle.Flex : DisplayStyle.None;
            if (managing || selected != null && draft?.HasContentIntent == true) ui.Q("economics").style.display = DisplayStyle.None;
            ui.Q("contentPlacementActions").style.display = IsPlacingContent || reassigning != null ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Q<Button>("confirmContent").SetEnabled(contentPreview?.IsValid == true && draft?.Durability != DraftDurability.Unknown);
            ui.Q<Button>("unassignContent").style.display = IsEditing && selected != null ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Q<Button>("reassignContent").style.display = IsEditing && selected != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (!managing) return;
            var floor = renderState.Floors.Single(f => f.FloorInstanceId == selectedFloor);
            var room = floor.Layout.Rooms.Single(r => r.RoomInstanceId == ContextRoomId);
            var definition = draftContext.Production.Catalog.Rooms.Single(r => r.RoomDefinitionId == room.RoomDefinitionId);
            CanonicalRoomCapacityResolver.TryResolve(draftContext.Production, room.RoomDefinitionId, out var capacity, out _);
            int maximum = contentCategory == MvpDungeonPlacementIds.MonsterCategoryId ? capacity.MonsterCapacity : contentCategory == MvpDungeonPlacementIds.TrapCategoryId ? capacity.TrapCapacity : capacity.LootCapacity;
            var assignments = floor.RoomContents.Assignments.Where(a => a.RoomInstanceId == room.RoomInstanceId && a.CategoryId == contentCategory).ToArray();
            ui.Q<Label>("contentCapacity").text = Format("ui.dungeon.content.capacity", RoomName(definition), assignments.Length, maximum);
            var choices = ui.Q("contentChoices"); choices.Clear();
            void Choice(string name, string badge, string identity, string category, string option, Action action)
            {
                var button = new Button(action) { userData = identity }; button.AddToClassList("content-card");
                var icon = new VisualElement { pickingMode = PickingMode.Ignore }; icon.AddToClassList("content-card-icon");
                icon.style.backgroundImage = new StyleBackground(presentationPolicy.Visuals.Resolve(category, option)); button.Add(icon);
                button.Add(new Label(name) { pickingMode = PickingMode.Ignore }); button.Add(new Label(badge) { pickingMode = PickingMode.Ignore }); choices.Add(button);
            }
            if (reassigning != null)
            {
                var source = renderState.Floors.SelectMany(f => f.RoomContents.Assignments).Single(a => a.AssignmentId == reassigning);
                foreach (var destinationFloor in renderState.Floors.OrderBy(f => f.FloorIndex).ThenBy(f => f.FloorInstanceId, StringComparer.Ordinal))
                    foreach (var destination in destinationFloor.Layout.Rooms.Where(r => r.RoomInstanceId != source.RoomInstanceId).OrderBy(r => r.RoomInstanceId, StringComparer.Ordinal))
                    {
                        string floorId = destinationFloor.FloorInstanceId, roomId = destination.RoomInstanceId;
                        var name = RoomName(draftContext.Production.Catalog.Rooms.Single(r => r.RoomDefinitionId == destination.RoomDefinitionId));
                        choices.Add(new Button(() => SelectContentDestination(floorId, roomId)) { text = Format("ui.dungeon.content.destination", destinationFloor.FloorIndex + 1, name), userData = roomId });
                    }
            }
            else if (contentIntent == null)
            {
                foreach (var assignment in assignments.OrderBy(a => a.Sequence).ThenBy(a => a.AssignmentId, StringComparer.Ordinal))
                {
                    string id = assignment.AssignmentId;
                    Choice(ContentName(assignment.OptionId), Format("ui.dungeon.content.assigned", assignment.RoomLocalPosition.X, assignment.RoomLocalPosition.Y), id,
                        assignment.CategoryId, assignment.OptionId, () => { ClearContentSelection(); selectedRoomId = null; selected = assignment; OpenSheet(); world.SelectContent(selected); Present(); });
                }
                foreach (var returned in renderState.LifecycleAndOwnership.ReturnedContents.Where(a => a.CategoryId == contentCategory).OrderBy(a => a.Sequence).ThenBy(a => a.AssignmentId, StringComparer.Ordinal))
                {
                    string id = returned.AssignmentId;
                    Choice(ContentName(returned.OptionId), Text("ui.dungeon.content.owned"), id, returned.CategoryId, returned.OptionId, () => SelectReturnedContent(id));
                }
                foreach (var effect in (draftContext.Configuration.MvpPlacementEffects ?? Array.Empty<MvpPlacementEffectConfig>()).Where(e => e.CategoryId == contentCategory).OrderBy(e => e.OptionId, StringComparer.Ordinal))
                {
                    string option = effect.OptionId;
                    if (!root.SaveService.TryContentAcquisitionCost(contentCategory, option, out double cost) || !RoomContentSpatialOccupancyAuthority.TryResolve(draftContext.Occupancy, contentCategory, option, out _)) continue;
                    Choice(ContentName(option), Format("ui.dungeon.content.new", PlayerManaPresentationFormatter.FormatDiscreteAmount(cost, Culture)), option,
                        contentCategory, option, () => SelectNewContent(option));
                }
            }
            var info = ui.Q<Label>("contentPlacementInfo");
            var gate = root.DungeonDraftPurchaseEligibility();
            info.text = contentIntent == null ? Text("ui.dungeon.content.choose") : Text(contentPreview?.Reason ?? (contentPreview == null ? "ui.dungeon.content.position_hint" : "ui.dungeon.content.preview"));
            if (contentIntent?.Operation == DungeonDraftContentOperation.Acquire)
            {
                info.text += "\n" + root.SaveService.PresentContentAcquisition(contentIntent.CategoryId, contentIntent.OptionId, root.Save, Text);
                if (!gate.Allowed) info.text += "\n" + Text(gate.MessageKey);
            }
            if (contentPreview?.IsValid == true) info.text += "\n" + Format("ui.dungeon.content.coordinates", contentIntent.RoomLocalPosition.X, contentIntent.RoomLocalPosition.Y);
        }
    }
}
