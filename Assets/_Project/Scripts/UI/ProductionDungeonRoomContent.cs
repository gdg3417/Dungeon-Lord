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
        private bool contentBrowserOwned;
        public bool ContentBrowserOwned => contentBrowserOwned;
        public DungeonDraftContentPreview ContentPreview => contentPreview;
        public bool IsPlacingContent => IsEditing && contentIntent != null;
        public string ContentCategory => contentCategory;
        private string ContentPreviewMessageKey => contentPreview?.Reason == DetachedCanonicalSpatialMutation.NoOpReason
            ? contentCategory == MvpDungeonPlacementIds.LootNodeCategoryId ? "ui.dungeon.content.same_loot"
                : contentCategory == MvpDungeonPlacementIds.TrapCategoryId ? "ui.dungeon.content.same_trap" : contentPreview.Reason
            : contentPreview?.Reason == DetachedSpatialMigrationPreparer.CapacityReason ? "ui.dungeon.content.category_full"
            : contentPreview?.Reason == DetachedCanonicalSpatialMutation.PositionInvalidReason ? "ui.dungeon.content.position_blocked"
            : contentPreview?.Reason;
        private string ContextRoomId => selectedRoomId ?? selected?.RoomInstanceId;
        private string ContentHeadingKey => reassigning != null || contentIntent?.Operation == DungeonDraftContentOperation.Reassign ?
            "ui.dungeon.content.reassign" : contentIntent?.Operation == DungeonDraftContentOperation.Reposition ? "ui.dungeon.move" :
            contentCategory == MvpDungeonPlacementIds.MonsterCategoryId ? "ui.dungeon.content.add_monster" :
            contentCategory == MvpDungeonPlacementIds.TrapCategoryId ? "ui.dungeon.content.add_trap" : "ui.dungeon.content.add_loot";

        private void InitializeRoomContent()
        {
            Button("monstersCategory", () => OpenContentCategory(MvpDungeonPlacementIds.MonsterCategoryId));
            Button("trapsCategory", () => OpenContentCategory(MvpDungeonPlacementIds.TrapCategoryId));
            Button("lootCategory", () => OpenContentCategory(MvpDungeonPlacementIds.LootNodeCategoryId));
            Button("confirmContent", () => ConfirmContentPlacement());
            Button("cancelContent", CancelContentPlacement);
            Button("unassignContent", () => UnassignSelectedContent());
            Button("reassignContent", BeginContentReassignment);
            Button("newContent", () => SelectContentBrowser(false));
            Button("ownedContent", () => SelectContentBrowser(true));
        }
        private void ClearContentSelection()
        { contentCategory = null; reassigning = null; contentIntent = null; contentPreview = null; contentBrowserOwned = false; world?.ClearRoomPlacementGrid(); }

        public bool SelectContentBrowser(bool owned)
        {
            if (!IsEditing || contentCategory == null || ContextRoomId == null) return false;
            contentBrowserOwned = owned; contentIntent = null; contentPreview = null; reassigning = null;
            world.ClearPreview(); world.SelectRoomFootprint(ContextRoomId);
            feedback = null; Present(); return true;
        }
        private void PrepareContentPositioning()
        {
            contentPreview = null; world.ClearPreview(); world.SelectRoomFootprint(ContextRoomId);
            world.PresentRoomPlacementGrid(ContextRoomId);
        }

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
            contentBrowserOwned = false; PrepareContentPositioning(); feedback = "ui.dungeon.content.position_hint"; Present(); return true;
        }
        public bool SelectReturnedContent(string id)
        {
            if (!IsEditing || contentCategory == null || ContextRoomId == null ||
                !draft.ReadModel.LifecycleAndOwnership.ReturnedContents.Any(a => a.AssignmentId == id && a.CategoryId == contentCategory)) return false;
            contentIntent = draftContext.ContentIntent(DungeonDraftContentOperation.Redeploy, selectedFloor, ContextRoomId, id);
            contentBrowserOwned = true; PrepareContentPositioning(); feedback = "ui.dungeon.content.position_hint"; Present(); return true;
        }
        private void BeginContentReposition()
        {
            string room = selected.RoomInstanceId, category = selected.CategoryId, id = selected.AssignmentId;
            OpenContentCategory(category);
            contentIntent = draftContext.ContentIntent(DungeonDraftContentOperation.Reposition, selectedFloor, room, id);
            PrepareContentPositioning();
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
            var source = state.Floors.SelectMany(f => f.RoomContents.Assignments).FirstOrDefault(a => a.AssignmentId == id);
            if (source == null || source.RoomInstanceId == roomId ||
                !state.Floors.Any(f => f.FloorInstanceId == floorId && f.Layout.Rooms.Any(r => r.RoomInstanceId == roomId)))
            {
                world.ClearPreview(); world.SelectRoomFootprint(ContextRoomId);
                feedback = "ui.dungeon.content.destination_invalid"; Present(); return false;
            }
            if (selectedFloor != floorId) SelectFloor(floorId);
            selectedRoomId = roomId; selected = null; selectedEdgeId = null; contentCategory = category; reassigning = null;
            contentIntent = draftContext.ContentIntent(DungeonDraftContentOperation.Reassign, floorId, roomId, id);
            detailsCollapsed = false; OpenSheet(); PrepareContentPositioning();
            feedback = "ui.dungeon.content.position_hint"; Present(); return true;
        }
        private void SelectContentDestinationAt(TileCoordinate cell)
        {
            if (SelectContentDestination(selectedFloor, world.SelectContentRoom(cell))) PreviewRoomContent(cell);
        }
        private void PreviewRoomContent(TileCoordinate cell)
        {
            // New/returned choices may retarget freely; existing assignments require explicit Reassign.
            if (contentIntent.Operation == DungeonDraftContentOperation.Acquire || contentIntent.Operation == DungeonDraftContentOperation.Redeploy)
            {
                string target = world.SelectContentRoom(cell);
                if (target != null && target != contentIntent.RoomInstanceId)
                {
                    selectedRoomId = target; contentIntent.RoomInstanceId = target;
                    PrepareContentPositioning(); OpenSheet();
                }
            }
            if (!world.TryRoomLocal(contentIntent.RoomInstanceId, cell, out var local))
            {
                contentPreview = new DungeonDraftContentPreview { Reason = DetachedCanonicalSpatialMutation.PositionInvalidReason };
                world.ClearPreview();
                world.SelectRoomFootprint(contentIntent.RoomInstanceId);
                world.PresentRoomPlacementGrid(contentIntent.RoomInstanceId);
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
            feedback = ContentPreviewMessageKey ?? "ui.dungeon.content.preview"; Present();
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
            contentBrowserOwned = true;
            feedback = "ui.dungeon.content.returned"; Present(); return true;
        }
        private string ContentName(string option) => MvpDungeonPlacementPresenter.ResolveOptionName(option, (key, fallback) => root.Content.GetString(key, fallback));
        private void PresentRoomContent()
        {
            var ui = document.rootVisualElement;
            bool managing = IsEditing && contentCategory != null && ContextRoomId != null;
            string activeCategory = contentCategory ?? selected?.CategoryId;
            ui.Q<Button>("roomsCategory").EnableInClassList("selected-floor", activeCategory == null);
            ui.Q<Button>("monstersCategory").EnableInClassList("selected-floor", activeCategory == MvpDungeonPlacementIds.MonsterCategoryId);
            ui.Q<Button>("trapsCategory").EnableInClassList("selected-floor", activeCategory == MvpDungeonPlacementIds.TrapCategoryId);
            ui.Q<Button>("lootCategory").EnableInClassList("selected-floor", activeCategory == MvpDungeonPlacementIds.LootNodeCategoryId);
            ui.Q("roomContentPanel").style.display = managing ? DisplayStyle.Flex : DisplayStyle.None;
            if (managing) sheet.style.display = DisplayStyle.None;
            safe.EnableInClassList("placing-content", IsPlacingContent || reassigning != null);
            if (managing && draft.Durability == DraftDurability.Acknowledged && draft.StructuralReason == null)
                status.style.display = DisplayStyle.None;
            if (managing) ui.Q<Button>("move").style.display = DisplayStyle.None;
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
            string roomLabel = Format("ui.dungeon.room_selected", RoomName(definition), Array.FindIndex(floor.Layout.Rooms, r => r.RoomInstanceId == room.RoomInstanceId) + 1);
            ui.Q<Label>("contentCapacity").text = Format("ui.dungeon.content.capacity", roomLabel, assignments.Length, maximum);
            var returnedChoices = renderState.LifecycleAndOwnership.ReturnedContents.Where(a => a.CategoryId == contentCategory)
                .OrderBy(a => a.Sequence).ThenBy(a => a.AssignmentId, StringComparer.Ordinal).ToArray();
            ui.Q<Button>("newContent").text = Text("ui.dungeon.content.browser_new");
            ui.Q<Button>("ownedContent").text = Format("ui.dungeon.content.browser_owned", returnedChoices.Length);
            ui.Q<Button>("newContent").EnableInClassList("selected-floor", !contentBrowserOwned);
            ui.Q<Button>("ownedContent").EnableInClassList("selected-floor", contentBrowserOwned);
            ui.Q("contentBrowserSelector").style.display = reassigning == null && contentIntent == null ? DisplayStyle.Flex : DisplayStyle.None;
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
                        choices.Add(new Button(() => SelectContentDestination(floorId, roomId)) { text = Format("ui.dungeon.content.destination", destinationFloor.FloorIndex + 1,
                            Format("ui.dungeon.room_selected", name, Array.IndexOf(destinationFloor.Layout.Rooms, destination) + 1)), userData = roomId });
                    }
            }
            else if (contentIntent == null)
            {
                if (contentBrowserOwned) foreach (var returned in returnedChoices)
                {
                    string id = returned.AssignmentId;
                    Choice(ContentName(returned.OptionId), Text("ui.dungeon.content.owned"), id, returned.CategoryId, returned.OptionId, () => SelectReturnedContent(id));
                }
                if (!contentBrowserOwned) foreach (var effect in (draftContext.Configuration.MvpPlacementEffects ?? Array.Empty<MvpPlacementEffectConfig>()).Where(e => e.CategoryId == contentCategory).OrderBy(e => e.OptionId, StringComparer.Ordinal))
                {
                    string option = effect.OptionId;
                    if (!root.SaveService.TryContentAcquisitionCost(contentCategory, option, out double cost) || !RoomContentSpatialOccupancyAuthority.TryResolve(draftContext.Occupancy, contentCategory, option, out _)) continue;
                    Choice(ContentName(option), Format("ui.dungeon.content.new", PlayerManaPresentationFormatter.FormatDiscreteAmount(cost, Culture)), option,
                        contentCategory, option, () => SelectNewContent(option));
                }
            }
            var info = ui.Q<Label>("contentPlacementInfo");
            var gate = root.DungeonDraftPurchaseEligibility();
            info.text = contentIntent == null ? Text(reassigning != null ? feedback ?? "ui.dungeon.content.destination_hint" :
                contentBrowserOwned ? returnedChoices.Length == 0 ? "ui.dungeon.content.owned_empty" : "ui.dungeon.content.owned_hint" : "ui.dungeon.content.new_hint") :
                Text(ContentPreviewMessageKey ?? (contentPreview == null ? "ui.dungeon.content.position_hint" : "ui.dungeon.content.preview"));
            if (contentIntent != null)
            {
                string option = contentIntent.OptionId ?? contentPreview?.Assignment?.OptionId ??
                    renderState.Floors.SelectMany(f => f.RoomContents.Assignments).FirstOrDefault(a => a.AssignmentId == contentIntent.AssignmentId)?.OptionId ??
                    returnedChoices.FirstOrDefault(a => a.AssignmentId == contentIntent.AssignmentId)?.OptionId;
                info.text += "\n" + Format("ui.dungeon.content.target", ContentName(option), roomLabel);
                if (contentIntent.Operation == DungeonDraftContentOperation.Redeploy) info.text += "\n" + Text("ui.dungeon.content.owned");
            }
            if (contentIntent?.Operation == DungeonDraftContentOperation.Acquire)
            {
                info.text += "\n" + root.SaveService.PresentContentAcquisition(contentIntent.CategoryId, contentIntent.OptionId, root.Save, Text);
                if (!gate.Allowed) info.text += "\n" + Text(gate.MessageKey);
            }
        }
    }
}
