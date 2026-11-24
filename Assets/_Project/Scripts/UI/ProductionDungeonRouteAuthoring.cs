using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEngine;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0
{
    public sealed partial class ProductionDungeonController
    {
        private bool independentRoomPlacement;
        private string relocatingDraftRoom;
        private string routeRoom;
        private string[] routeEdges = Array.Empty<string>();
        private StructuralRouteInsertionRequest routeRequest;
        private StructuralRouteInsertionPreview routePreview;
        private StructuralEconomyPreview routeQuote;
        private bool? drawIncomingPassage;
        private TileCoordinate[] segmentOrigin;
        private (int Start, int End) selectedSegment;
        private TileCoordinate segmentTarget;
        private bool segmentExceedsLimit;
        private bool draggingIndependentPlacement;
        private TileCoordinate dragAnchorOffset;
        private Vector2 placementDragPoint;

        private bool CanAuthorRoutes => CanonicalSaveSchemaVersions.HasAuthoredRoutes(CanonicalSaveSchemaVersions.CurrentWritableTarget);
        private bool IsAuthoringRoute => IsEditing && routeRoom != null;
        private IndependentRoomPlacementRequest IndependentPlacementRequest() => new IndependentRoomPlacementRequest
        {
            FloorInstanceId = constructionRequest.FloorInstanceId,
            ExistingDraftRoomId = relocatingDraftRoom,
            RoomDefinitionId = constructionRequest.RoomDefinitionId,
            Anchor = constructionRequest.Anchor,
            Orientation = constructionRequest.Orientation
        };

        private bool ConfirmIndependentRoomPlacement()
        {
            if (!constructionPreview.IsValid || !draft.PlaceIndependentRoom(IndependentPlacementRequest()))
            {
                feedback = constructionPreview.ReasonCodes.FirstOrDefault() ?? draft.Reason;
                Present();
                return false;
            }
            string roomId = constructionPreview.TargetRoomInstanceId;
            ClearConstructionSelection();
            RebuildFloor(false);
            OpenRouteAuthoring(roomId);
            feedback = "ui.dungeon.route.room_confirmed";
            Present();
            return true;
        }

        private void OpenRouteAuthoring(string roomId)
        {
            if (!CanAuthorRoutes || !IsEditing) return;
            var floor = DraftSelectedFloor();
            if (floor?.Layout.Rooms.Any(room => room.RoomInstanceId == roomId) != true) return;
            CloseSheet();
            detailsCollapsed = false;
            routeRoom = roomId;
            routeEdges = root.SaveService.EligibleRouteInsertions(root.Save, draft, selectedFloor);
            routeRequest = draft.AcceptedRouteInsertion(roomId);
            RefreshRouteProposal();
            Present();
        }

        private void RepositionDraftRoom(string roomId)
        {
            var room = DraftSelectedFloor().Layout.Rooms.Single(r => r.RoomInstanceId == roomId);
            if (!SelectConstructionRoom(room.RoomDefinitionId, true)) return;
            relocatingDraftRoom = roomId;
            constructionRequest.Orientation = room.Orientation;
            PreviewConstruction(room.Anchor);
        }

        private void ClearRouteAuthoring()
        {
            routeRoom = null;
            routeEdges = Array.Empty<string>();
            routeRequest = null;
            routePreview = null;
            routeQuote = null;
            drawIncomingPassage = null;
            ClearSegmentSelection();
        }

        private SavedSpatialFloor DraftSelectedFloor() => draft?.ReadModel.Floors.SingleOrDefault(f => f.FloorInstanceId == selectedFloor);
        private SavedSpatialFloor CommittedSelectedFloor() =>
            root.Save.validatedCanonicalSpatialState.Floors.SingleOrDefault(f => f.FloorInstanceId == selectedFloor);

        private void SelectRouteEdge(string edgeId)
        {
            if (routeRequest?.ReplacedEdgeId == edgeId) return;
            var canonical = CommittedSelectedFloor();
            var edge = canonical.Layout.Edges.Single(e => e.EdgeId == edgeId);
            var newNode = DraftSelectedFloor().Layout.Nodes.Single(n => n.RoomInstanceId == routeRoom);
            var from = RouteOpenings(edge.SourceNodeId);
            var to = RouteOpenings(edge.DestinationNodeId);
            var placed = RouteOpenings(newNode.NodeId);
            if (from.Length == 0 || to.Length == 0 || placed.Length < 2)
            {
                feedback = StructuralEditService.ConnectionUnavailableReason;
                Present();
                return;
            }
            routeRequest = new StructuralRouteInsertionRequest
            {
                FloorInstanceId = selectedFloor, RoomInstanceId = routeRoom, ReplacedEdgeId = edgeId,
                Incoming = new AuthoredRouteGeometry { SourcePointId = from[0].Id, DestinationPointId = placed[0].Id },
                Outgoing = new AuthoredRouteGeometry { SourcePointId = placed[1].Id, DestinationPointId = to[0].Id }
            };
            drawIncomingPassage = null;
            ClearSegmentSelection();
            UpdateRouteProposal();
        }

        private void SuggestRoute()
        {
            var suggested = root.SaveService.SuggestRouteInsertion(root.Save, draft, selectedFloor, routeRequest.ReplacedEdgeId, routeRoom);
            if (suggested == null)
            {
                feedback = "ui.dungeon.route.no_suggestion";
                Present();
                return;
            }
            routeRequest = suggested;
            drawIncomingPassage = null;
            ClearSegmentSelection();
            UpdateRouteProposal();
        }

        private RouteOpening[] RouteOpenings(string nodeId) =>
            AuthoredRouteGeometry.Openings(DraftSelectedFloor(), nodeId, root.ProductionSpatialContent.Catalog);

        private void EditPassage(bool incoming, bool clear)
        {
            ClearSegmentSelection();
            drawIncomingPassage = incoming;
            if (clear) (incoming ? routeRequest.Incoming : routeRequest.Outgoing).Passage = Array.Empty<TileCoordinate>();
            UpdateRouteProposal();
        }

        private bool TryEditPassage(TileCoordinate cell)
        {
            if (!IsAuthoringRoute || !drawIncomingPassage.HasValue || routeRequest == null) return false;
            var link = drawIncomingPassage.Value ? routeRequest.Incoming : routeRequest.Outgoing;
            var definition = root.ProductionSpatialContent.Catalog.Corridors.Single(c => c.CorridorDefinitionId ==
                OptionalBranchStructuralEditService.CorridorDefinitionId);
            if (segmentOrigin != null)
            {
                segmentExceedsLimit = !AuthoredRouteGeometry.TrySlideSegment(segmentOrigin, selectedSegment.Start,
                    selectedSegment.End, cell, definition.MaximumLength, out var moved);
                if (!segmentExceedsLimit)
                {
                    segmentTarget = cell;
                    link.Passage = moved;
                }
                UpdateRouteProposal();
                return true;
            }
            if (link.Passage.Length < definition.MaximumLength) link.Passage = link.Passage.Append(cell).ToArray();
            else feedback = StructuralEditService.CorridorLengthReason;
            UpdateRouteProposal();
            return true;
        }

        private void ClearSegmentSelection()
        {
            segmentOrigin = null;
            segmentExceedsLimit = false;
        }

        private void PresentSegments(VisualElement panel, bool incoming, AuthoredRouteGeometry link)
        {
            if (segmentOrigin != null && drawIncomingPassage == incoming) return;
            int number = 0;
            foreach (var segment in AuthoredRouteGeometry.Segments(link.Passage))
            {
                var from = link.Passage[segment.Start];
                var to = link.Passage[segment.End];
                var label = Format(incoming ? "ui.dungeon.route.incoming_segment" : "ui.dungeon.route.outgoing_segment",
                    ++number, from.X, from.Y, to.X, to.Y);
                ButtonFor(panel, label, () =>
                {
                    drawIncomingPassage = incoming;
                    segmentOrigin = (TileCoordinate[])link.Passage.Clone();
                    selectedSegment = segment;
                    segmentTarget = from;
                    segmentExceedsLimit = false;
                    UpdateRouteProposal();
                }).name = incoming ? "slideIncomingSegment" : "slideOutgoingSegment";
            }
        }

        private void UpdateRouteProposal()
        {
            RefreshRouteProposal();
            Present();
        }

        private void RefreshRouteProposal()
        {
            routePreview = null; routeQuote = null;
            if (routeRequest == null) return;
            routePreview = segmentExceedsLimit
                ? StructuralRouteInsertionService.Fail(StructuralEditService.CorridorLengthReason)
                : root.SaveService.PreviewRouteInsertion(root.Save, draft, routeRequest, out routeQuote);
            var edge = CommittedSelectedFloor().Layout.Edges.Single(e => e.EdgeId == routeRequest.ReplacedEdgeId);
            var node = DraftSelectedFloor().Layout.Nodes.Single(n => n.RoomInstanceId == routeRoom);
            var selectedOpenings = new[] {
                (edge.SourceNodeId, routeRequest.Incoming.SourcePointId), (node.NodeId, routeRequest.Incoming.DestinationPointId),
                (node.NodeId, routeRequest.Outgoing.SourcePointId), (edge.DestinationNodeId, routeRequest.Outgoing.DestinationPointId) };
            var doorTiles = selectedOpenings.SelectMany(value => RouteOpenings(value.Item1).Where(p => p.Id == value.Item2)
                .Select(p => p.World));
            IEnumerable<TileCoordinate> selected = null;
            if (segmentOrigin != null)
            {
                bool horizontal = segmentOrigin[selectedSegment.Start].Y == segmentOrigin[selectedSegment.End].Y;
                selected = segmentOrigin.Skip(selectedSegment.Start).Take(selectedSegment.End - selectedSegment.Start + 1)
                    .Select(cell => horizontal ? new TileCoordinate(cell.X, segmentTarget.Y) :
                        new TileCoordinate(segmentTarget.X, cell.Y));
            }
            world.PresentRoutePreview(doorTiles.Concat(routeRequest.Incoming.Passage).Concat(routeRequest.Outgoing.Passage),
                routePreview.IsValid, selected);
        }

        private void AcceptRoute()
        {
            if (routePreview?.IsValid != true || !root.SaveService.AcceptRouteInsertion(root.Save, draft, routeRequest))
            {
                feedback = draft.Reason;
                Present();
                return;
            }
            ClearRouteAuthoring();
            world.ClearPreview();
            RebuildFloor(false);
            feedback = "ui.dungeon.route.connection_accepted";
            Present();
        }

        private void PresentRouteAuthoring(VisualElement panel)
        {
            panel.Clear();
            panel.style.display = CanAuthorRoutes && IsEditing && !IsConstructing ? DisplayStyle.Flex : DisplayStyle.None;
            if (panel.style.display.value == DisplayStyle.None) return;
            var floor = DraftSelectedFloor();
            if (floor == null) return;
            var browse = new VisualElement();
            browse.AddToClassList("row");
            foreach (var roomId in draft.RouteAuthoringRoomIds.Where(id => floor.Layout.Rooms.Any(r => r.RoomInstanceId == id)))
                ButtonFor(browse, Format("ui.dungeon.route.edit_for_room", RouteRoomName(floor, roomId)),
                    () => OpenRouteAuthoring(roomId));
            if (browse.childCount > 0) panel.Add(browse);
            if (!IsAuthoringRoute) return;

            var canonical = CommittedSelectedFloor();
            panel.Add(new Label(Format("ui.dungeon.route.choose_edge", RouteRoomName(floor, routeRoom))));
            if (draft.DisconnectedRoomIds.Contains(routeRoom))
                ButtonFor(panel, Text("ui.dungeon.route.reposition_room"), () => RepositionDraftRoom(routeRoom));
            foreach (var id in routeEdges)
            {
                var edge = canonical.Layout.Edges.Single(e => e.EdgeId == id);
                var button = ButtonFor(panel, Format("ui.dungeon.route.edge", RouteNodeName(canonical, edge.SourceNodeId),
                    RouteNodeName(canonical, edge.DestinationNodeId)), () => SelectRouteEdge(id));
                button.name = "replaceRouteEdge";
                button.userData = id;
                if (id == routeRequest?.ReplacedEdgeId) button.AddToClassList("selected-floor");
            }
            if (routeEdges.Length == 0) panel.Add(new Label(Text(StructuralRouteInsertionService.RetirementUnavailable)));
            if (routeRequest == null) return;

            var chosen = canonical.Layout.Edges.Single(e => e.EdgeId == routeRequest.ReplacedEdgeId);
            var newNode = floor.Layout.Nodes.Single(n => n.RoomInstanceId == routeRoom);
            ButtonFor(panel, Text("ui.dungeon.route.suggest"), SuggestRoute).name = "suggestRoute";
            AddOpeningChoice(panel, "ui.dungeon.route.predecessor_opening", chosen.SourceNodeId,
                routeRequest.Incoming.SourcePointId, point => routeRequest.Incoming.SourcePointId = point);
            AddOpeningChoice(panel, "ui.dungeon.route.new_room_entry", newNode.NodeId,
                routeRequest.Incoming.DestinationPointId, point => routeRequest.Incoming.DestinationPointId = point);
            AddOpeningChoice(panel, "ui.dungeon.route.new_room_exit", newNode.NodeId,
                routeRequest.Outgoing.SourcePointId, point => routeRequest.Outgoing.SourcePointId = point);
            AddOpeningChoice(panel, "ui.dungeon.route.successor_opening", chosen.DestinationNodeId,
                routeRequest.Outgoing.DestinationPointId, point => routeRequest.Outgoing.DestinationPointId = point);
            PresentSegments(panel, true, routeRequest.Incoming);
            PresentSegments(panel, false, routeRequest.Outgoing);
            if (segmentOrigin != null)
            {
                bool horizontal = segmentOrigin[selectedSegment.Start].Y == segmentOrigin[selectedSegment.End].Y;
                panel.Add(new Label(Text(horizontal ? "ui.dungeon.route.slide_row" : "ui.dungeon.route.slide_column")));
                ButtonFor(panel, Text("ui.dungeon.route.revert_segment"), () =>
                {
                    var link = drawIncomingPassage.Value ? routeRequest.Incoming : routeRequest.Outgoing;
                    link.Passage = segmentOrigin;
                    ClearSegmentSelection();
                    drawIncomingPassage = null;
                    UpdateRouteProposal();
                }).name = "revertRouteSegment";
            }
            ButtonFor(panel, Format("ui.dungeon.route.draw_incoming", routeRequest.Incoming.Passage.Length), () => EditPassage(true, false));
            ButtonFor(panel, Format("ui.dungeon.route.draw_outgoing", routeRequest.Outgoing.Passage.Length), () => EditPassage(false, false))
                .name = "drawOutgoingPassage";
            if (drawIncomingPassage.HasValue && segmentOrigin == null)
            {
                panel.Add(new Label(Text(drawIncomingPassage.Value ? "ui.dungeon.route.drawing_incoming" : "ui.dungeon.route.drawing_outgoing")));
                ButtonFor(panel, Text("ui.dungeon.route.clear_passage"), () => EditPassage(drawIncomingPassage.Value, true)).name = "clearPassage";
                ButtonFor(panel, Text("ui.dungeon.route.undo_tile"), () =>
                {
                    var link = drawIncomingPassage.Value ? routeRequest.Incoming : routeRequest.Outgoing;
                    link.Passage = link.Passage.Take(Math.Max(0, link.Passage.Length - 1)).ToArray();
                    UpdateRouteProposal();
                });
            }
            if (routePreview?.IsValid == true)
            {
                var proposedFloor = routePreview.Candidate.Floors.Single(f => f.FloorInstanceId == selectedFloor);
                if (RequiredFloorTraversal.TryResolve(proposedFloor.Layout, out var order))
                    panel.Add(new Label(Format("ui.dungeon.route.order", string.Join(Text("ui.dungeon.route.separator"),
                        order.Select(node => RouteNodeName(proposedFloor, node.NodeId))))));
                panel.Add(new Label(Format("ui.dungeon.route.floor_space", routePreview.UsedFloorSpace, routePreview.FloorCapacity)));
                if (routeQuote != null)
                {
                    root.SaveService.RefreshDraftPreviewBalance(routeQuote, root.Save.structureRuntime.ManaReserve);
                    panel.Add(new Label(StructuralEconomyPresenter.Present(routeQuote, Text, root.PassiveManaPerHourForPresentation)));
                    if (root.Save.structureRuntime.PlacementLocked) panel.Add(new Label(Text("ui.banner.place_blocked_heat_crisis")));
                    panel.Add(new Label(Format("ui.dungeon.route.resulting_usable",
                        PlayerManaPresentationFormatter.FormatDiscreteAmount(
                            Math.Max(0d, routeQuote.ResultingMana - routeQuote.RequiredReserveAfter), Culture))));
                }
            }
            else if (routePreview?.Reason != null) panel.Add(new Label(Text(routePreview.Reason)));

            var accept = ButtonFor(panel, Text("ui.dungeon.route.accept"), AcceptRoute);
            accept.name = "acceptRoute";
            accept.SetEnabled(routePreview?.IsValid == true && draft.Durability == DraftDurability.Acknowledged);
        }

        private void AddOpeningChoice(VisualElement panel, string label, string nodeId, string pointId, Action<string> select)
        {
            var choices = RouteOpenings(nodeId);
            int index = Array.FindIndex(choices, p => p.Id == pointId);
            if (index < 0) return;
            var point = choices[index];
            var button = ButtonFor(panel, Format(label, DirectionName(point.Facing), point.World.X, point.World.Y), () =>
            {
                select(choices[(index + 1) % choices.Length].Id);
                UpdateRouteProposal();
            });
            button.SetEnabled(choices.Length > 1);
        }

        private string RouteRoomName(SavedSpatialFloor floor, string roomId)
        {
            var room = floor.Layout.Rooms.Single(r => r.RoomInstanceId == roomId);
            var definition = root.ProductionSpatialContent.Catalog.Rooms.Single(r => r.RoomDefinitionId == room.RoomDefinitionId);
            return Format("ui.dungeon.route.room_location", RoomName(definition), room.Anchor.X, room.Anchor.Y);
        }

        private string RouteNodeName(SavedSpatialFloor floor, string nodeId)
        {
            var node = floor.Layout.Nodes.Single(n => n.NodeId == nodeId);
            if (node.Kind == FloorRouteNodeKind.Room) return RouteRoomName(floor, node.RoomInstanceId);
            var kind = node.Kind == FloorRouteNodeKind.Entrance ? FixedSpatialStructureKind.Entrance : FixedSpatialStructureKind.CompletionTerminal;
            var structure = floor.FixedStructures.Single(s => s.Kind == kind);
            return SpatialName(root.ProductionSpatialContent.Catalog.FixedStructures
                .Single(d => d.StructureDefinitionId == structure.FixedStructureDefinitionId).LocalizationKey);
        }

        private static Button ButtonFor(VisualElement container, string label, Action action)
        {
            var button = new Button(action) { text = label, tooltip = label };
            container.Add(button);
            return button;
        }

        private TileCoordinate ScreenCell(Vector2 point)
        {
            var worldPoint = viewport.ScreenToWorld(point);
            return new TileCoordinate(Mathf.FloorToInt(worldPoint.x), Mathf.FloorToInt(worldPoint.y));
        }

        private bool TryBeginIndependentPlacementDrag(Vector2 point, int pointerId)
        {
            if (!independentRoomPlacement || constructionPreview == null || IsChrome(point, pointerId) ||
                !viewport.ScreenRect.Contains(point)) return false;
            var cell = ScreenCell(point);
            if (!constructionPreview.OccupiedTiles.Contains(cell)) return false;
            dragAnchorOffset = new TileCoordinate(constructionRequest.Anchor.X - cell.X, constructionRequest.Anchor.Y - cell.Y);
            placementDragPoint = point;
            draggingIndependentPlacement = true;
            return true;
        }
    }
}
