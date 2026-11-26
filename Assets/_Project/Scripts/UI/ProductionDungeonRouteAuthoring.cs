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
        private string routeStart;
        private bool routeMiddleSelected;
        private readonly List<(Vector2 Position, Action Select, bool Segment)> routeHandles =
            new List<(Vector2, Action, bool)>();
        private Action routeDrag;
        private Vector2 routeDragStart;
        private Vector2 routeDragEnd;
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
            autoFit = false;
            interactionFocus = null;
            routeRoom = roomId;
            routeEdges = root.SaveService.EligibleRouteInsertions(root.Save, draft, selectedFloor);
            routeRequest = draft.AcceptedRouteInsertion(roomId);
            RefreshRouteProposal();
            PresentRouteMap();
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
            routeStart = null;
            routeMiddleSelected = false;
            routeDrag = null;
            routeHandles.Clear();
            world?.ClearRouteMarkers();
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
            SuggestRoute();
        }

        private void SuggestRoute()
        {
            var suggested = root.SaveService.SuggestRouteInsertion(root.Save, draft, selectedFloor, routeRequest.ReplacedEdgeId, routeRoom);
            if (suggested == null)
            {
                feedback = "ui.dungeon.route.no_suggestion";
                UpdateRouteProposal();
                return;
            }
            feedback = null;
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

        private void SelectSegment(bool incoming, AuthoredRouteGeometry link, (int Start, int End) segment)
        {
            drawIncomingPassage = incoming;
            segmentOrigin = (TileCoordinate[])link.Passage.Clone();
            selectedSegment = segment;
            segmentTarget = link.Passage[segment.Start];
            segmentExceedsLimit = false;
            UpdateRouteProposal();
        }

        private bool TryRouteMap(Vector2 point)
        {
            var cell = new TileCoordinate(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
            if (!IsAuthoringRoute)
            {
                if (!IsEditing) return false;
                var roomId = world.SelectRoom(cell, true);
                if (roomId == null || !draft.RouteAuthoringRoomIds.Contains(roomId)) return false;
                OpenRouteAuthoring(roomId);
                return true;
            }

            var handle = RouteHandleAt(point);
            if (handle.Select != null && (!drawIncomingPassage.HasValue || segmentOrigin != null))
            {
                handle.Select();
                return true;
            }
            if (TryEditPassage(cell)) return true;

            var floor = DraftSelectedFloor();
            var hitRoom = world.SelectRoom(cell, true);
            var selectedNode = floor.Layout.Nodes.FirstOrDefault(n => n.Kind == FloorRouteNodeKind.Room
                ? n.RoomInstanceId == hitRoom
                : TryRouteNodeBounds(floor, n, out var bounds) && bounds.Contains(point));
            if (selectedNode == null) return true;
            var canonical = CommittedSelectedFloor();
            var eligible = canonical.Layout.Edges.Where(edge => routeEdges.Contains(edge.EdgeId)).ToArray();
            var selectedEdge = eligible.FirstOrDefault(edge => edge.SourceNodeId == routeStart &&
                edge.DestinationNodeId == selectedNode.NodeId);
            feedback = null;
            if (selectedNode.RoomInstanceId == routeRoom)
            {
                if (routeStart != null) routeMiddleSelected = true;
            }
            else if (routeMiddleSelected && selectedEdge != null)
            {
                routeStart = null;
                routeMiddleSelected = false;
                SelectRouteEdge(selectedEdge.EdgeId);
                return true;
            }
            else if (eligible.Any(edge => edge.SourceNodeId == selectedNode.NodeId))
            {
                routeStart = selectedNode.NodeId;
                routeMiddleSelected = false;
                routeRequest = null;
                world.ClearPreview();
            }
            else feedback = StructuralRouteInsertionService.RetirementUnavailable;
            UpdateRouteProposal();
            return true;
        }

        private (Vector2 Position, Action Select, bool Segment) RouteHandleAt(Vector2 point) =>
            routeHandles.Where(handle => (handle.Position - point).sqrMagnitude <= .22f)
                .OrderBy(handle => (handle.Position - point).sqrMagnitude).FirstOrDefault();

        private bool TryRouteNodeBounds(SavedSpatialFloor floor, FloorRouteNode node, out Rect bounds)
        {
            var location = node.RoomInstanceId;
            if (node.Kind == FloorRouteNodeKind.Entrance || node.Kind == FloorRouteNodeKind.Completion)
            {
                var kind = node.Kind == FloorRouteNodeKind.Entrance
                    ? FixedSpatialStructureKind.Entrance : FixedSpatialStructureKind.CompletionTerminal;
                location = floor.FixedStructures.Single(structure => structure.Kind == kind).FixedStructureInstanceId;
            }
            return world.TryRouteLocationBounds(location, out bounds);
        }

        private static Vector2 CellCenter(TileCoordinate cell) => new Vector2(cell.X + .5f, cell.Y + .5f);

        private static Vector2 OpeningCenter(RouteOpening opening)
        {
            float angle = -(int)opening.Facing * Mathf.PI / 2f;
            return CellCenter(opening.World) + new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * .5f;
        }

        private void PresentRouteMap()
        {
            world.ClearRouteMarkers();
            routeHandles.Clear();
            if (!IsAuthoringRoute) return;
            var floor = DraftSelectedFloor();
            var baseline = CommittedSelectedFloor();
            if (!RequiredFloorTraversal.TryResolve(baseline.Layout, out var currentOrder)) return;
            var edge = baseline.Layout.Edges.FirstOrDefault(e => e.EdgeId == routeRequest?.ReplacedEdgeId);
            var placed = floor.Layout.Nodes.Single(n => n.RoomInstanceId == routeRoom);
            IEnumerable<FloorRouteNode> order = currentOrder;
            if (edge != null)
                order = currentOrder.SelectMany(n => n.NodeId == edge.SourceNodeId ? new[] { n, placed } : new[] { n });
            var eligible = baseline.Layout.Edges.Where(e => routeEdges.Contains(e.EdgeId)).ToArray();
            int index = 0;
            foreach (var node in order)
            {
                index++;
                if (!TryRouteNodeBounds(floor, node, out var bounds)) continue;
                bool highlighted = node.NodeId == routeStart || node == placed ||
                    eligible.Any(e => routeMiddleSelected ? e.SourceNodeId == routeStart && e.DestinationNodeId == node.NodeId :
                        e.SourceNodeId == node.NodeId);
                string number = index.ToString(Culture);
                world.AddRouteMarker(bounds.center, highlighted ? "[" + number + "]" : number, highlighted, .85f);
            }
            if (edge == null)
            {
                if (TryRouteNodeBounds(floor, placed, out var bounds))
                    world.AddRouteMarker(bounds.center, routeMiddleSelected ? "[+]" : "+", true, .85f);
                return;
            }

            var retiredOpening = world.ConnectionVisuals.FirstOrDefault(connection => connection.EdgeId == edge.EdgeId);
            if (retiredOpening.EdgeId != null)
                world.AddRouteMarker(retiredOpening.Center, "X", false, .48f);
            PresentRouteLink(true, edge.SourceNodeId, placed.NodeId);
            PresentRouteLink(false, placed.NodeId, edge.DestinationNodeId);
        }

        private void PresentRouteLink(bool incoming, string source, string destination)
        {
            var link = incoming ? routeRequest.Incoming : routeRequest.Outgoing;
            var from = AddOpeningHandles(source, link.SourcePointId, incoming, true);
            var to = AddOpeningHandles(destination, link.DestinationPointId, incoming, false);
            if (!from.HasValue || !to.HasValue) return;
            var points = new[] { from.Value }.Concat(link.Passage.Select(CellCenter)).Append(to.Value).ToArray();
            for (int i = 1; i < points.Length; i++)
                world.AddRouteLine(points[i - 1], points[i], routePreview?.IsValid == true);
            if (segmentOrigin != null || drawIncomingPassage.HasValue) return;
            foreach (var segment in AuthoredRouteGeometry.Segments(link.Passage))
            {
                var position = (CellCenter(link.Passage[segment.Start]) + CellCenter(link.Passage[segment.End])) * .5f;
                world.AddRouteMarker(position, "=", true, .45f);
                routeHandles.Add((position, () => SelectSegment(incoming, link, segment), true));
            }
        }

        private Vector2? AddOpeningHandles(string nodeId, string selected, bool incoming, bool source)
        {
            Vector2? selectedPosition = null;
            var placed = DraftSelectedFloor().Layout.Nodes.Single(n => n.RoomInstanceId == routeRoom);
            foreach (var opening in RouteOpenings(nodeId))
            {
                var position = OpeningCenter(opening);
                bool active = opening.Id == selected;
                if (active) selectedPosition = position;
                // The incoming and outgoing sockets on the new room share the same authored points.
                // Separate their handles toward the room interior and exterior.
                if (nodeId == placed.NodeId)
                    position += (position - CellCenter(opening.World)).normalized * (incoming ? -.24f : .24f);
                world.AddRouteMarker(position, active ? (incoming ? "1" : "2") : (incoming ? "i" : "o"), active, .36f);
                routeHandles.Add((position, () =>
                {
                    var link = incoming ? routeRequest.Incoming : routeRequest.Outgoing;
                    if (source) link.SourcePointId = opening.Id;
                    else link.DestinationPointId = opening.Id;
                    ClearSegmentSelection();
                    drawIncomingPassage = null;
                    UpdateRouteProposal();
                }, false));
            }
            return selectedPosition;
        }

        private void UpdateRouteProposal()
        {
            RefreshRouteProposal();
            PresentRouteMap();
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
            panel.style.display = DisplayStyle.None;
            if (!CanAuthorRoutes || !IsEditing || IsConstructing) return;
            if (!IsAuthoringRoute) return;
            panel.style.display = DisplayStyle.Flex;
            string instruction = routeRequest != null ? "ui.dungeon.route.map_edit" :
                routeMiddleSelected ? "ui.dungeon.route.map_next" :
                routeStart != null ? "ui.dungeon.route.map_new" : "ui.dungeon.route.map_start";
            panel.Add(new Label(Text(instruction)));
            var actions = new VisualElement();
            actions.AddToClassList("row");
            panel.Add(actions);
            ButtonFor(actions, Text("ui.dungeon.route.map_cancel"), () =>
            {
                ClearRouteAuthoring();
                world.ClearPreview();
                feedback = null;
                Present();
            }).name = "cancelRoute";
            if (routeRequest != null)
                ButtonFor(actions, Text("ui.dungeon.route.map_choose_again"), () =>
                {
                    routeRequest = null;
                    drawIncomingPassage = null;
                    ClearSegmentSelection();
                    world.ClearPreview();
                    feedback = null;
                    UpdateRouteProposal();
                }).name = "chooseRouteRoomsAgain";
            if (draft.DisconnectedRoomIds.Contains(routeRoom))
                ButtonFor(actions, Text("ui.dungeon.route.reposition_room"), () => RepositionDraftRoom(routeRoom));
            if (routeEdges.Length == 0) panel.Add(new Label(Text(StructuralRouteInsertionService.RetirementUnavailable)));
            if (routeRequest == null) return;

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
                if (!segmentExceedsLimit)
                    ButtonFor(panel, Text("ui.dungeon.route.map_finish_segment"), () =>
                    {
                        ClearSegmentSelection();
                        drawIncomingPassage = null;
                        UpdateRouteProposal();
                    }).name = "finishRouteSegment";
            }
            if (routePreview?.IsValid != true && segmentOrigin == null && !drawIncomingPassage.HasValue)
            {
                ButtonFor(actions, Format("ui.dungeon.route.draw_incoming", routeRequest.Incoming.Passage.Length),
                    () => EditPassage(true, false));
                ButtonFor(actions, Format("ui.dungeon.route.draw_outgoing", routeRequest.Outgoing.Passage.Length),
                    () => EditPassage(false, false)).name = "drawOutgoingPassage";
            }
            if (drawIncomingPassage.HasValue && segmentOrigin == null)
            {
                ButtonFor(panel, Text("ui.dungeon.route.map_finish_tiles"), () =>
                {
                    drawIncomingPassage = null;
                    UpdateRouteProposal();
                }).name = "finishRouteTiles";
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
