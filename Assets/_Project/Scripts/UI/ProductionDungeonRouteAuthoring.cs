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
        private Rect RouteInteractionBounds => independentRoomPlacement || IsAuthoringRoute ? world.LegalBounds : world.Bounds;
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
            viewport.Configure(RouteInteractionBounds, viewport.ScreenRect, false);
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
                var roomId = world.SelectRoom(cell, false);
                if (roomId == null || !draft.RouteAuthoringRoomIds.Contains(roomId)) return false;
                OpenRouteAuthoring(roomId);
                return true;
            }

            var handle = RouteHandleAt(point);
            if (handle.Select != null && !drawIncomingPassage.HasValue)
            {
                handle.Select();
                return true;
            }
            if (TryEditPassage(cell)) return true;

            var floor = DraftSelectedFloor();
            var hitRoom = world.SelectRoom(cell, false);
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
            var proposedFloor = routePreview?.Candidate?.Floors.Single(f => f.FloorInstanceId == selectedFloor);
            if (proposedFloor != null && RequiredFloorTraversal.TryResolve(proposedFloor.Layout, out var proposedOrder))
                order = proposedOrder;
            else if (edge != null)
                order = currentOrder.SelectMany(n => n.NodeId == edge.SourceNodeId ? new[] { n, placed } : new[] { n });
            else if (RequiredFloorTraversal.TryResolve(floor.Layout, out var draftOrder))
                order = draftOrder;
            var eligible = baseline.Layout.Edges.Where(e => routeEdges.Contains(e.EdgeId)).ToArray();
            int index = 0;
            foreach (var node in order)
            {
                index++;
                if (!TryRouteNodeBounds(floor, node, out var bounds)) continue;
                bool highlighted = node.NodeId == routeStart || node.NodeId == placed.NodeId ||
                    eligible.Any(e => routeMiddleSelected ? e.SourceNodeId == routeStart && e.DestinationNodeId == node.NodeId :
                        e.SourceNodeId == node.NodeId);
                string number = index.ToString(Culture);
                world.AddRouteMarker(bounds.center, highlighted ? "[" + number + "]" : number, highlighted, .85f);
            }
            if (edge == null)
            {
                if (!order.Any(n => n.NodeId == placed.NodeId) && TryRouteNodeBounds(floor, placed, out var bounds))
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
            bool isPlacedRoom = DraftSelectedFloor().Layout.Nodes.Any(n => n.NodeId == nodeId && n.RoomInstanceId == routeRoom);
            foreach (var opening in RouteOpenings(nodeId))
            {
                var position = OpeningCenter(opening);
                bool active = opening.Id == selected;
                if (active) selectedPosition = position;
                // The incoming and outgoing sockets on the new room share the same authored points.
                // Separate their handles toward the room interior and exterior.
                if (isPlacedRoom)
                    position += (position - CellCenter(opening.World)).normalized * (incoming ? -.24f : .24f);
                world.AddRouteMarker(position, active ? (incoming ? "1" : "2") : (incoming ? "i" : "o"), active, .36f);
                if (drawIncomingPassage.HasValue) continue;
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

        private Label routeInstruction, routeState, routeMetrics, routeDetail;
        private Button acceptRoute, cancelRoute, chooseRouteRoomsAgain, moveRouteRoom, revertRouteSegment,
            finishRouteSegment, drawIncoming, drawOutgoing, finishRouteTiles, clearRouteTiles, undoRouteTile;

        private void BindRouteDock(VisualElement panel)
        {
            routeInstruction = new Label { name = "routeInstruction" };
            routeState = new Label { name = "routeState" };
            routeMetrics = new Label { name = "routeMetrics" };
            routeDetail = new Label { name = "routeDetail" };
            panel.Add(routeState);
            panel.Add(routeInstruction);
            panel.Add(routeMetrics);
            panel.Add(routeDetail);
            var main = new VisualElement();
            main.AddToClassList("route-action-row");
            panel.Add(main);
            acceptRoute = RouteAction(main, "acceptRoute", "accept", AcceptRoute);
            cancelRoute = RouteAction(main, "cancelRoute", "map_cancel", () =>
            {
                ClearRouteAuthoring();
                world.ClearPreview();
                feedback = null;
                Present();
            });
            chooseRouteRoomsAgain = RouteAction(main, "chooseRouteRoomsAgain", "map_choose_again", () =>
            {
                routeRequest = null;
                drawIncomingPassage = null;
                ClearSegmentSelection();
                world.ClearPreview();
                feedback = null;
                UpdateRouteProposal();
            });
            moveRouteRoom = RouteAction(main, "moveRouteRoom", "reposition_room", () => RepositionDraftRoom(routeRoom));
            var adjustments = new VisualElement();
            adjustments.AddToClassList("route-action-row");
            panel.Add(adjustments);
            revertRouteSegment = RouteAction(adjustments, "revertRouteSegment", "revert_segment", () =>
            {
                (drawIncomingPassage.Value ? routeRequest.Incoming : routeRequest.Outgoing).Passage = segmentOrigin;
                FinishRouteAdjustment();
            });
            finishRouteSegment = RouteAction(adjustments, "finishRouteSegment", "map_finish_segment", FinishRouteAdjustment);
            drawIncoming = RouteAction(adjustments, "drawIncomingPassage", "map_draw_in", () => EditPassage(true, false));
            drawOutgoing = RouteAction(adjustments, "drawOutgoingPassage", "map_draw_out", () => EditPassage(false, false));
            finishRouteTiles = RouteAction(adjustments, "finishRouteTiles", "map_finish_tiles", FinishRouteAdjustment);
            clearRouteTiles = RouteAction(adjustments, "clearPassage", "clear_passage", () => EditPassage(drawIncomingPassage.Value, true));
            undoRouteTile = RouteAction(adjustments, "undoRouteTile", "undo_tile", () =>
            {
                var link = drawIncomingPassage.Value ? routeRequest.Incoming : routeRequest.Outgoing;
                link.Passage = link.Passage.Take(Math.Max(0, link.Passage.Length - 1)).ToArray();
                UpdateRouteProposal();
            });
        }

        private Button RouteAction(VisualElement row, string name, string key, Action action)
        {
            var button = ButtonFor(row, Text("ui.dungeon.route." + key), action);
            button.name = name;
            return button;
        }

        private void FinishRouteAdjustment()
        {
            ClearSegmentSelection();
            drawIncomingPassage = null;
            UpdateRouteProposal();
        }

        private void PresentRouteAuthoring(VisualElement panel)
        {
            panel.style.display = IsAuthoringRoute ? DisplayStyle.Flex : DisplayStyle.None;
            if (!IsAuthoringRoute) return;
            if (acceptRoute == null) BindRouteDock(panel);
            bool valid = routePreview?.IsValid == true;
            bool proposal = routeRequest != null;
            bool sliding = segmentOrigin != null;
            bool drawing = drawIncomingPassage.HasValue && !sliding;
            string instruction = sliding
                ? segmentOrigin[selectedSegment.Start].Y == segmentOrigin[selectedSegment.End].Y
                    ? "ui.dungeon.route.slide_row" : "ui.dungeon.route.slide_column"
                : drawing ? "ui.dungeon.route.map_draw_hint"
                : proposal ? "ui.dungeon.route.map_edit"
                : routeMiddleSelected ? "ui.dungeon.route.map_next"
                : routeStart != null ? "ui.dungeon.route.map_new" : "ui.dungeon.route.map_start";
            routeInstruction.text = Text(instruction);
            string problem = draft.Durability != DraftDurability.Acknowledged ? draft.Reason
                : proposal && !valid ? routePreview?.Reason
                : routeEdges.Length == 0 || feedback == StructuralRouteInsertionService.RetirementUnavailable
                    ? StructuralRouteInsertionService.RetirementUnavailable : null;
            routeState.EnableInClassList("route-problem", problem != null);
            routeState.text = Text(problem == null ? valid ? "ui.dungeon.route.ready" : "ui.dungeon.route.waiting"
                : ConstructionReason(problem));
            routeDetail.text = Text(valid ? "ui.dungeon.route.preview_only" : "ui.dungeon.route.order_on_map");
            routeDetail.style.visibility = detailsCollapsed ? Visibility.Hidden : Visibility.Visible;
            acceptRoute.style.visibility = proposal ? Visibility.Visible : Visibility.Hidden;
            acceptRoute.SetEnabled(valid && draft.Durability == DraftDurability.Acknowledged);
            ShowRouteAction(chooseRouteRoomsAgain, proposal);
            ShowRouteAction(moveRouteRoom, draft.DisconnectedRoomIds.Contains(routeRoom));
            ShowRouteAction(revertRouteSegment, sliding);
            finishRouteSegment.style.display = DisplayStyle.Flex;
            finishRouteSegment.style.visibility = sliding && !segmentExceedsLimit ? Visibility.Visible : Visibility.Hidden;
            ShowRouteAction(drawIncoming, proposal && !valid && !sliding && !drawing);
            ShowRouteAction(drawOutgoing, proposal && !valid && !sliding && !drawing);
            ShowRouteAction(finishRouteTiles, drawing);
            ShowRouteAction(clearRouteTiles, drawing);
            ShowRouteAction(undoRouteTile, drawing);
            // Reserve the edit row in every state. A new blocker or prompt must not move the map.
            if (drawing || proposal && !valid && !sliding) finishRouteSegment.style.display = DisplayStyle.None;
            PresentRouteBalance();
        }

        private static void ShowRouteAction(Button action, bool visible) =>
            action.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        private void PresentRouteBalance()
        {
            if (routeMetrics == null) return;
            var quote = routeQuote;
            string value = string.Empty;
            if (routePreview?.IsValid == true)
            {
                value = Format("ui.dungeon.route.floor_space", routePreview.UsedFloorSpace, routePreview.FloorCapacity);
                if (quote != null)
                {
                    root.SaveService.RefreshDraftPreviewBalance(quote, root.Save.structureRuntime.ManaReserve);
                    value += "   " + Format("ui.dungeon.route.compact_cost",
                        StructuralEconomyPresenter.FormatTransactionAmount(quote.Cost),
                        PlayerManaPresentationFormatter.FormatDiscreteAmount(
                            Math.Max(0d, quote.ResultingMana - quote.RequiredReserveAfter), Culture));
                    if (root.Save.structureRuntime.PlacementLocked) value += "\n" + Text("ui.banner.place_blocked_heat_crisis");
                    else if (!quote.IsAffordable && quote.Reason != null) value += "\n" + Text(quote.Reason);
                }
            }
            if (routeMetrics.text != value) routeMetrics.text = value;
        }

        private string ConstructionReason(string reason)
        {
            if (!IsAuthoringRoute && !independentRoomPlacement) return reason;
            if (reason == StructuralEditService.ConnectionUnavailableReason && IsAuthoringRoute)
                return "ui.dungeon.route.opening_mismatch";
            if (reason == StructuralEditService.CorridorLengthReason && IsAuthoringRoute)
                return "ui.dungeon.route.passage_length";
            if (reason == StructuralEditService.RequiredRouteReason) return "ui.dungeon.route.waiting";
            if (reason == StructuralEditService.OutOfBoundsReason) return "ui.dungeon.route.outside";
            if (reason == StructuralEditService.RoomOverlapReason) return "ui.dungeon.route.room_overlap";
            if (reason == StructuralEditService.FixedOverlapReason) return "ui.dungeon.route.fixed_overlap";
            if (reason == StructuralEditService.CorridorOverlapReason)
                return IsAuthoringRoute ? "ui.dungeon.route.passage_overlap" : "ui.dungeon.route.room_corridor_overlap";
            return reason;
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
