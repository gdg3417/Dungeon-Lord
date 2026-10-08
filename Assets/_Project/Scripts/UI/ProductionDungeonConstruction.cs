using System;
using System.Linq;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0
{
    public sealed partial class ProductionDungeonController
    {
        private StructuralConstructionRequest constructionRequest;
        private StructuralEditPreview constructionPreview;
        private StructuralEconomyPreview constructionEconomy;
        private string constructionIntentId;
        private bool detailsCollapsed;
        public bool IsConstructing => constructionRequest != null && IsEditing;
        public StructuralEditPreview ConstructionPreview => constructionPreview;
        private bool NeedsStarterSetup => renderState?.Floors.Length == 0 || renderState?.Floors.Any(f =>
            f.FloorInstanceId == selectedFloor && f.ActivationState == FloorActivationState.Active && f.Layout.Rooms.Length == 0) == true;

        private void InitializeConstruction()
        {
            Button("roomsCategory", OpenRooms);
            Button("confirmPlacement", () => ConfirmConstructionPlacement());
            Button("cancelPlacement", CancelConstructionSelection);
            Button("collapseDetails", () => { detailsCollapsed = !detailsCollapsed; Present(); });
        }

        public void OpenRooms()
        {
            if (!HasCanonicalRuntime() || !IsEditing) return;
            CloseSheet(); detailsCollapsed = false; feedback = NeedsStarterSetup ? "ui.dungeon.construction.starter_required" : "ui.dungeon.construction.choose_room"; Present();
        }

        private RoomSpatialDefinition[] ConstructionRooms()
        {
            var floor = renderState?.Floors.SingleOrDefault(f => f.FloorInstanceId == selectedFloor);
            var definition = root.ProductionSpatialContent.Catalog.Floors.SingleOrDefault(f => f.FloorDefinitionId == floor?.FloorDefinitionId);
            return root.ProductionSpatialContent.Catalog.Rooms.Where(r => definition?.AllowedRoomDefinitionIds.Contains(r.RoomDefinitionId) == true)
                .OrderBy(r => r.RoomDefinitionId, StringComparer.Ordinal).ToArray();
        }

        private string RoomName(RoomSpatialDefinition definition) => SpatialName(definition.LocalizationKey);

        private string SpatialName(string localizationKey)
        {
            var tables = root.ProductionSpatialContent.Languages;
            var language = tables.FirstOrDefault(t => t.language == root.Content.Strings?.language) ?? tables.Single(t => t.language == "en");
            return language.entries.FirstOrDefault(e => e.key == localizationKey)?.text ?? Text("ui.dungeon.unavailable");
        }

        public bool SelectConstructionRoom(string definitionId)
        {
            if (!HasCanonicalRuntime() || !IsEditing || draft.Durability == DraftDurability.Unknown) return false;
            var definition = ConstructionRooms().SingleOrDefault(r => r.RoomDefinitionId == definitionId);
            if (definition == null) return false;
            CloseSheet(); detailsCollapsed = false;
            constructionIntentId = Guid.NewGuid().ToString("N");
            constructionRequest = new StructuralConstructionRequest { FloorInstanceId = selectedFloor, RoomDefinitionId = definitionId,
                Orientation = definition.AllowedOrientations.First(), TerminalConnectionPointId = definition.ConnectionPoints.First().ConnectionPointId };
            RefreshConstructionGuidance(); Present(); return true;
        }

        public bool SelectConstructionOrientation(CardinalOrientation orientation)
        {
            if (!IsConstructing || !ConstructionRooms().Single(r => r.RoomDefinitionId == constructionRequest.RoomDefinitionId).AllowedOrientations.Contains(orientation)) return false;
            constructionRequest.Orientation = orientation; constructionPreview = null;
            RefreshConstructionGuidance(); Present(); return true;
        }

        public bool SelectTerminalConnection(string pointId)
        {
            if (!IsConstructing || !ConstructionRooms().Single(r => r.RoomDefinitionId == constructionRequest.RoomDefinitionId).ConnectionPoints.Any(p => p.ConnectionPointId == pointId)) return false;
            constructionRequest.TerminalConnectionPointId = pointId; constructionPreview = null;
            RefreshConstructionGuidance(); Present(); return true;
        }

        private void RefreshConstructionGuidance()
        {
            constructionEconomy = null;
            world.ClearPreview();
            var guidance = StructuralEditService.GetConstructionGuidance(draft.ReadModel, constructionRequest, draftContext, floorPresentationLimits);
            world.PresentMoveGuidance(guidance.ValidAnchors);
            feedback = guidance.Reason ?? (guidance.ValidAnchors.Length == 0 ? NeedsStarterSetup ? "ui.dungeon.construction.starter_required" : "ui.dungeon.construction.none" : "ui.dungeon.construction.hint");
        }

        private void PreviewConstruction(TileCoordinate cell)
        {
            constructionRequest.Anchor = cell;
            constructionPreview = StructuralEditService.Preview(draft.ReadModel, constructionRequest, draftContext.Production,
                draftContext.Compatibility, draftContext.Configuration, draftContext.Limits.Canonical);
            if (constructionPreview.IsValid && !draftContext.Validate(constructionPreview.DetachedCandidate))
                constructionPreview = StructuralEditService.InvalidPreview(StructuralEditService.LayoutInvalidReason, constructionRequest);
            constructionEconomy = constructionPreview.IsValid ? root.SaveService.PreviewDungeonDraft(root.Save, draft, constructionPreview.DetachedCandidate) : null;
            world.PresentConstructionPreview(constructionPreview);
            feedback = constructionPreview.ReasonCodes.FirstOrDefault() ?? "ui.dungeon.construction.preview";
            detailsCollapsed = false; Present();
        }

        public bool ConfirmConstructionPlacement()
        {
            if (!HasCanonicalRuntime() || !IsConstructing || constructionPreview == null || draft.Durability == DraftDurability.Unknown) return false;
            if (!draft.ConstructRoom(constructionIntentId, constructionRequest)) { feedback = draft.Reason; Present(); return false; }
            string intentId = constructionIntentId;
            bool invalid = draft.InvalidConstructions.Any(value => value.IntentId == intentId);
            ClearConstructionSelection(); RebuildFloor(false);
            feedback = invalid ? draft.StructuralReason : "ui.dungeon.construction.confirmed";
            Present(); return true;
        }

        private void CancelConstructionSelection()
        {
            if (!HasCanonicalRuntime()) return;
            if (IsEditing && draft.InvalidConstructions.Any(value => value.IntentId == constructionIntentId))
                if (!draft.CancelConstruction(constructionIntentId)) { feedback = draft.Reason; Present(); return; }
            ClearConstructionSelection(); RebuildFloor(false); feedback = "ui.dungeon.construction.choose_room"; Present();
        }

        private void ClearConstructionSelection()
        {
            constructionRequest = null; constructionPreview = null; constructionEconomy = null; constructionIntentId = null;
            world?.ClearPreview(); world?.ClearMoveGuidance();
        }

        public bool CorrectConstruction(string intentId)
        {
            if (!HasCanonicalRuntime() || !IsEditing) return false;
            var intent = draft.InvalidConstructions.SingleOrDefault(value => value.IntentId == intentId);
            if (intent == null) return false;
            SelectFloor(intent.Request.FloorInstanceId); CloseSheet(); detailsCollapsed = false;
            constructionIntentId = intent.IntentId; constructionRequest = intent.Request;
            RefreshConstructionGuidance(); PreviewConstruction(constructionRequest.Anchor); return true;
        }

        private bool TrySelectInvalidConstruction(TileCoordinate cell)
        {
            foreach (var value in draft.InvalidConstructions.Where(v => v.Request.FloorInstanceId == selectedFloor))
            {
                var definition = root.ProductionSpatialContent.Catalog.Rooms.Single(r => r.RoomDefinitionId == value.Request.RoomDefinitionId);
                if (definition.TryResolveGrossTiles(value.Request.Anchor, value.Request.Orientation,
                    new SpatialValidationWorkloadLimits(draftContext.Limits.Canonical.Spatial.MaximumMaterializedTiles), out var footprint) && footprint.OccupiedTiles.Contains(cell))
                    return CorrectConstruction(value.IntentId);
            }
            return false;
        }

        private string OrientationName(CardinalOrientation orientation) => Text(orientation == CardinalOrientation.Zero ? "ui.structural.orientation.0" :
            orientation == CardinalOrientation.Ninety ? "ui.structural.orientation.1" : orientation == CardinalOrientation.OneEighty ? "ui.structural.orientation.2" : "ui.structural.orientation.3");

        private string DirectionName(CardinalOrientation facing) => Text(facing == CardinalOrientation.Zero ? "ui.structural.direction.0" :
            facing == CardinalOrientation.Ninety ? "ui.structural.direction.1" : facing == CardinalOrientation.OneEighty ? "ui.structural.direction.2" : "ui.structural.direction.3");

        private void PresentConstructionInformation()
        {
            var information = document.rootVisualElement.Q<Label>("constructionInfo");
            information.text = Text(NeedsStarterSetup ? "ui.dungeon.construction.starter_required" : "ui.dungeon.construction.choose_room");
            if (!IsConstructing) return;
            var definition = ConstructionRooms().Single(r => r.RoomDefinitionId == constructionRequest.RoomDefinitionId);
            information.text = Format("ui.dungeon.construction.properties", RoomName(definition), definition.GrossFootprint.Width,
                definition.GrossFootprint.Height, definition.MonsterCapacity, definition.TrapCapacity, definition.LootCapacity);
            if (constructionPreview?.IsValid != true) return;
            var price = constructionEconomy;
            if (price != null) root.SaveService.RefreshDraftPreviewBalance(price, root.Save.structureRuntime.ManaReserve);
            var floor = constructionPreview.DetachedCandidate.Floors.Single(f => f.FloorInstanceId == selectedFloor);
            string roomId = constructionPreview.Consequences.Single(c => c.Kind == StructuralChangeKind.RoomAdded).StableId;
            var node = floor.Layout.Nodes.Single(n => n.RoomInstanceId == roomId);
            var incoming = floor.Layout.Edges.Single(e => e.DestinationNodeId == node.NodeId);
            var source = floor.Layout.Nodes.Single(n => n.NodeId == incoming.SourceNodeId);
            var catalog = root.ProductionSpatialContent.Catalog;
            string sourceName = source.Kind == FloorRouteNodeKind.Room
                ? RoomName(catalog.Rooms.Single(r => r.RoomDefinitionId == floor.Layout.Rooms.Single(r => r.RoomInstanceId == source.RoomInstanceId).RoomDefinitionId))
                : SpatialName(catalog.FixedStructures.Single(s => s.Kind == FixedSpatialStructureKind.Entrance).LocalizationKey);
            information.text += "\n" + Format("ui.dungeon.construction.source", sourceName);
            information.text += "\n" + Format("ui.dungeon.construction.consequences", constructionPreview.IncomingConnectionTiles.Length,
                Text(constructionPreview.ConnectionKind == FloorRouteConnectionKind.DirectDoorway ? "ui.structural.connection.direct" : "ui.structural.connection.corridor"));
            information.text += "\n" + (price != null ? Text("ui.dungeon.construction.cost_scope") + "\n" +
                StructuralEconomyPresenter.Present(price, Text, root.PassiveManaPerHourForPresentation) : Text("ui.dungeon.construction.cost_blocked"));
        }

        private void PresentConstruction()
        {
            var ui = document.rootVisualElement;
            ui.Q("constructionCategories").style.display = IsEditing ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Q<Button>("collapseDetails").text = Text(detailsCollapsed ? "ui.dungeon.details.expand" : "ui.dungeon.details.collapse");
            ui.Q("contextDetails").style.display = detailsCollapsed ? DisplayStyle.None : DisplayStyle.Flex;
            var panel = ui.Q("constructionSheet"); panel.style.display = IsEditing && selected == null && selectedRoomId == null && !moveMode ? DisplayStyle.Flex : DisplayStyle.None;
            var choices = ui.Q("roomChoices"); choices.Clear();
            ui.Q("roomChoicesScroll").style.display = constructionRequest == null ? DisplayStyle.Flex : DisplayStyle.None;
            if (IsEditing && constructionRequest == null)
                foreach (var room in ConstructionRooms())
                {
                    string id = room.RoomDefinitionId;
                    var button = new Button(() => SelectConstructionRoom(id)) { text = Format("ui.dungeon.construction.card", RoomName(room),
                        room.GrossFootprint.Width, room.GrossFootprint.Height,
                        root.SaveService.TryRoomConstructionBaseCost(id, out double cost) ? PlayerManaPresentationFormatter.FormatDiscreteAmount(cost, Culture) : Text("ui.dungeon.unavailable")) };
                    button.AddToClassList("room-card"); choices.Add(button);
                }
            var options = ui.Q("constructionOptions"); options.Clear();
            ui.Q<Button>("confirmPlacement").style.display = constructionPreview == null ? DisplayStyle.None : DisplayStyle.Flex;
            ui.Q<Button>("confirmPlacement").SetEnabled(IsConstructing && draft.Durability != DraftDurability.Unknown);
            ui.Q<Button>("confirmPlacement").text = Text(constructionPreview?.IsValid == false ? "ui.dungeon.construction.keep_invalid" : "ui.dungeon.construction.confirm");
            ui.Q<Button>("cancelPlacement").style.display = IsConstructing ? DisplayStyle.Flex : DisplayStyle.None;
            PresentConstructionInformation();
            if (IsConstructing)
            {
                var definition = ConstructionRooms().Single(r => r.RoomDefinitionId == constructionRequest.RoomDefinitionId);
                var orientations = new VisualElement(); orientations.AddToClassList("row"); options.Add(orientations);
                foreach (var orientation in definition.AllowedOrientations)
                {
                    var button = new Button(() => SelectConstructionOrientation(orientation)) { text = OrientationName(orientation) };
                    if (orientation == constructionRequest.Orientation) button.AddToClassList("selected-floor"); orientations.Add(button);
                }
                foreach (var point in definition.ConnectionPoints)
                {
                    string id = point.ConnectionPointId;
                    var button = new Button(() => SelectTerminalConnection(id)) { text = Format("ui.dungeon.construction.connection", DirectionName(StructuralEditService.Rotate(point.Facing, constructionRequest.Orientation))) };
                    if (id == constructionRequest.TerminalConnectionPointId) button.AddToClassList("selected-floor"); options.Add(button);
                }
            }
            var blockers = ui.Q("constructionBlockers"); blockers.Clear();
            if (IsEditing)
                foreach (var invalid in draft.InvalidConstructions)
                {
                    string id = invalid.IntentId;
                    var button = new Button(() => CorrectConstruction(id)) { text = Format("ui.dungeon.construction.correct", Text(invalid.Reason)) };
                    blockers.Add(button);
                }
            SetTextSize(textSize);
        }
    }
}
