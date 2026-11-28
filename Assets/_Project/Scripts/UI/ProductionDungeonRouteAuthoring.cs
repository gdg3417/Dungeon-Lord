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
        private enum RouteTargetKind { Doorway, Replacement, Section, Bend, Tile }

        private sealed class RouteTarget
        {
            internal RouteTargetKind Kind;
            internal bool Incoming;
            internal bool Source;
            internal string Opening;
            internal int Start, End;
            internal Vector2 Position;
            internal bool IsPassage => Kind == RouteTargetKind.Section || Kind == RouteTargetKind.Bend || Kind == RouteTargetKind.Tile;
        }

        private sealed class DoorwayIntent
        {
            internal bool Incoming;
            internal string SourcePointId;
            internal string DestinationPointId;
            internal string Reason;
        }

        private const int RouteUndoDepth = 8;
        private readonly List<RouteTarget> routeHandles = new List<RouteTarget>();
        private readonly List<StructuralRouteInsertionRequest> routeUndo = new List<StructuralRouteInsertionRequest>();
        private readonly List<DoorwayIntent> doorwayIntents = new List<DoorwayIntent>();
        private RouteTarget routeSelection, routeDrag;
        private DoorwayIntent attemptedDoorways;
        private StructuralRouteInsertionRequest manualRoute;
        private string manualReason;
        private Button beginManualRoute, finishManualRoute;
        private DoorwayIntent PendingDoorways => doorwayIntents.Count == 0 ? null : doorwayIntents[doorwayIntents.Count - 1];
        private Vector2 routeDragStart, routeDragEnd, routeDragCameraCenter;
        private Rect routeDragViewport;
        private float routeDragCameraSize;
        private bool routeDragging;
        private TileCoordinate? attemptedCell;
        private Vector2? attemptedMarker;
        private StructuralRouteInsertionRequest attemptedRoute;
        private string attemptReason;
        private string[] routeEdges = Array.Empty<string>();
        private StructuralRouteInsertionRequest routeRequest;
        private StructuralRouteInsertionPreview routePreview;
        private StructuralEconomyPreview routeQuote;
        private bool draggingIndependentPlacement;
        private TileCoordinate dragAnchorOffset;
        private Vector2 placementDragPoint;

        private bool CanAuthorRoutes => CanonicalSaveSchemaVersions.HasAuthoredRoutes(CanonicalSaveSchemaVersions.CurrentWritableTarget);
        private bool IsAuthoringRoute => IsEditing && (routeRoom != null || renovating);
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
            if (draft.IsRebuildingFloor(selectedFloor) ||
                root.SaveService.EligibleRouteInsertions(root.Save, draft, selectedFloor).Length == 0)
                OpenRenovation();
            else OpenRouteAuthoring(roomId);
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
            renovating = false;
            ClearRenovationSelection();
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
            ClearRouteAdjustment();
            routeUndo.Clear();
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
            ClearRouteAdjustment();
            routeUndo.Clear();
            SuggestRoute();
        }

        private void SuggestRoute()
        {
            var suggested = root.SaveService.SuggestRouteInsertion(root.Save, draft, selectedFloor, routeRequest.ReplacedEdgeId, routeRoom);
            if (suggested == null)
            {
                feedback = "ui.dungeon.route.no_suggestion_move";
                UpdateRouteProposal();
                return;
            }
            feedback = null;
            routeRequest = suggested;
            ClearRouteAdjustment();
            routeUndo.Clear();
            UpdateRouteProposal();
        }

        private RouteOpening[] RouteOpenings(string nodeId) =>
            AuthoredRouteGeometry.Openings(DraftSelectedFloor(), nodeId, root.ProductionSpatialContent.Catalog);

        private AuthoredRouteGeometry RouteLink(bool incoming) => incoming ? routeRequest.Incoming : routeRequest.Outgoing;

        private StructuralRouteInsertionRequest CopyRoute() => new StructuralRouteInsertionRequest
        {
            FloorInstanceId = routeRequest.FloorInstanceId,
            RoomInstanceId = routeRequest.RoomInstanceId,
            ReplacedEdgeId = routeRequest.ReplacedEdgeId,
            Incoming = routeRequest.Incoming.Copy(),
            Outgoing = routeRequest.Outgoing.Copy()
        };

        private void ClearRouteAdjustment()
        {
            routeSelection = null;
            doorwayIntents.Clear();
            manualRoute = null;
            manualReason = null;
            ClearRouteAttempt();
        }

        private void ClearRouteAttempt()
        {
            attemptedDoorways = null;
            attemptedCell = null;
            attemptedMarker = null;
            attemptedRoute = null;
            attemptReason = null;
        }

        private void CancelRouteAdjustment()
        {
            ClearRouteAdjustment();
            PresentAdjustment();
        }

        private void UndoRouteAdjustment()
        {
            if (manualRoute != null)
            {
                var passage = manualRoute.Incoming.Passage;
                if (passage.Length != 0) manualRoute.Incoming.Passage = passage.Take(passage.Length - 1).ToArray();
                RefreshManualRoute();
                return;
            }
            if (PendingDoorways != null)
            {
                doorwayIntents.RemoveAt(doorwayIntents.Count - 1);
                routeSelection = null;
                ClearRouteAttempt();
                PresentAdjustment();
                return;
            }
            if (routeUndo.Count == 0 || routeSelection != null) return;
            routeRequest = routeUndo[routeUndo.Count - 1];
            routeUndo.RemoveAt(routeUndo.Count - 1);
            UpdateRouteProposal();
        }

        private bool CanSelectRouteTarget(RouteTarget target) => manualRoute == null && target != null &&
            (PendingDoorways == null || (target.Kind == RouteTargetKind.Doorway &&
                target.Incoming == PendingDoorways.Incoming));

        private void SelectRouteTarget(RouteTarget target)
        {
            if (!CanSelectRouteTarget(target)) return;
            ClearRouteAttempt();
            routeSelection = target;
            PresentAdjustment();
        }

        private void AdjustRoute(RouteTarget target, TileCoordinate cell, RouteTarget replacement = null)
        {
            ClearRouteAttempt();
            attemptedCell = cell;
            attemptedMarker = replacement?.Position ?? CellCenter(cell);
            var next = CopyRoute();
            var link = target.Incoming ? next.Incoming : next.Outgoing;
            if (replacement != null)
            {
                var pending = PendingDoorways;
                if (pending != null)
                {
                    link.SourcePointId = pending.SourcePointId;
                    link.DestinationPointId = pending.DestinationPointId;
                }
                if (target.Source) link.SourcePointId = replacement.Opening;
                else link.DestinationPointId = replacement.Opening;
                attemptedDoorways = new DoorwayIntent
                {
                    Incoming = target.Incoming, SourcePointId = link.SourcePointId,
                    DestinationPointId = link.DestinationPointId
                };
            }
            if (target.Kind == RouteTargetKind.Section)
            {
                var definition = root.ProductionSpatialContent.Catalog.Corridors.Single(c => c.CorridorDefinitionId ==
                    OptionalBranchStructuralEditService.CorridorDefinitionId);
                if (!AuthoredRouteGeometry.TrySlideSegment(link.Passage, target.Start, target.End, cell,
                    definition.MaximumLength, out var moved))
                    attemptReason = StructuralEditService.CorridorLengthReason;
                else link.Passage = moved;
            }
            else
            {
                TileCoordinate? replaced = replacement == null ? link.Passage[target.Start] : (TileCoordinate?)null;
                TileCoordinate? waypoint = replacement == null ? cell : (TileCoordinate?)null;
                bool bend = target.Kind == RouteTargetKind.Bend;
                string reason;
                link = renovating ? draft.ReconnectRenovation(renovationIntent, link, waypoint, replaced, bend, out reason) :
                    draft.ReconnectRouteLink(next, target.Incoming, waypoint, replaced, bend, out reason);
                if (link == null) attemptReason = reason;
            }
            if (attemptReason == null)
            {
                if (target.Incoming) next.Incoming = link;
                else next.Outgoing = link;
                attemptedRoute = next;
                attemptReason = renovating ? draft.PreviewRenovation(RenovationIntentFor(next.Incoming)).Reason :
                    draft.PreviewRouteGeometry(next).Reason;
            }
        }

        private void CompleteRouteAdjustment()
        {
            if (attemptReason == null && attemptedRoute != null)
            {
                StructuralEconomyPreview quote;
                var preview = renovating ? PreviewRenovationRoute(attemptedRoute, out quote) :
                    root.SaveService.PreviewRouteInsertion(root.Save, draft, attemptedRoute, out quote);
                if (preview.IsValid)
                {
                    var before = RouteLink(routeSelection.Incoming);
                    var after = routeSelection.Incoming ? attemptedRoute.Incoming : attemptedRoute.Outgoing;
                    if (before.SourcePointId != after.SourcePointId || before.DestinationPointId != after.DestinationPointId ||
                        !before.Passage.SequenceEqual(after.Passage))
                    {
                        if (routeUndo.Count == RouteUndoDepth) routeUndo.RemoveAt(0);
                        routeUndo.Add(routeRequest);
                    }
                    routeRequest = attemptedRoute;
                    routePreview = preview;
                    routeQuote = quote;
                    ClearRouteAdjustment();
                }
                else attemptReason = preview.Reason;
            }
            if (attemptedDoorways != null && attemptReason != null)
            {
                attemptedDoorways.Reason = attemptReason;
                if (doorwayIntents.Count == RouteUndoDepth) doorwayIntents.RemoveAt(0);
                doorwayIntents.Add(attemptedDoorways);
                ClearRouteAttempt();
            }
            PresentAdjustment();
        }

        private void StartManualRoute()
        {
            manualRoute = CopyRoute();
            var pending = PendingDoorways;
            if (pending != null)
            {
                manualRoute.Incoming.SourcePointId = pending.SourcePointId;
                manualRoute.Incoming.DestinationPointId = pending.DestinationPointId;
            }
            manualRoute.Incoming.Passage = Array.Empty<TileCoordinate>();
            routeSelection = null;
            ClearRouteAttempt();
            RefreshManualRoute();
        }

        private void MarkManualRouteTile(TileCoordinate cell)
        {
            var passage = manualRoute.Incoming.Passage;
            var corridor = root.ProductionSpatialContent.Catalog.Corridors.Single(value =>
                value.CorridorDefinitionId == OptionalBranchStructuralEditService.CorridorDefinitionId);
            if (passage.Length >= corridor.MaximumLength)
            {
                manualReason = StructuralEditService.CorridorLengthReason;
                PresentAdjustment();
                return;
            }
            var proposed = passage.Append(cell).ToArray();
            if (!AuthoredRouteGeometry.IsContinuous(proposed))
            {
                manualReason = StructuralEditService.ConnectionUnavailableReason;
                PresentAdjustment();
                return;
            }
            manualRoute.Incoming.Passage = proposed;
            RefreshManualRoute();
        }

        private void RefreshManualRoute()
        {
            manualReason = draft.PreviewRenovation(RenovationIntentFor(manualRoute.Incoming)).Reason;
            PresentAdjustment();
        }

        private void FinishManualRoute()
        {
            var preview = PreviewRenovationRoute(manualRoute, out var quote);
            if (!preview.IsValid)
            {
                manualReason = preview.Reason;
                PresentAdjustment();
                return;
            }
            if (routeUndo.Count == RouteUndoDepth) routeUndo.RemoveAt(0);
            routeUndo.Add(routeRequest);
            routeRequest = manualRoute;
            routePreview = preview;
            routeQuote = quote;
            ClearRouteAdjustment();
            PresentAdjustment();
        }

        private void PresentAdjustment()
        {
            PresentRouteMap();
            PresentRouteAuthoring(document.rootVisualElement.Q("routeAuthoring"));
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

            if (manualRoute != null)
            {
                MarkManualRouteTile(cell);
                return true;
            }
            if (renovating && routeRequest == null) return TryRenovationMap(point, cell);
            if (routeRequest != null)
            {
                var handle = RouteHandleAt(point);
                if (handle?.Kind == RouteTargetKind.Replacement && routeSelection?.Kind == RouteTargetKind.Doorway)
                {
                    AdjustRoute(routeSelection, cell, handle);
                    CompleteRouteAdjustment();
                }
                else if (CanSelectRouteTarget(handle))
                {
                    if (PendingDoorways == null && routeSelection != null && handle.Kind == routeSelection.Kind &&
                        handle.Incoming == routeSelection.Incoming && handle.Source == routeSelection.Source &&
                        handle.Start == routeSelection.Start && handle.End == routeSelection.End)
                        CancelRouteAdjustment();
                    else SelectRouteTarget(handle);
                }
                else if (routeSelection?.IsPassage == true)
                {
                    AdjustRoute(routeSelection, cell);
                    CompleteRouteAdjustment();
                }
                return true;
            }

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

        private RouteTarget RouteHandleAt(Vector2 point) =>
            routeHandles.Where(handle => (handle.Position - point).sqrMagnitude <=
                (handle.IsPassage ? .185f : .12f))
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
            if (location != null) return world.TryRouteLocationBounds(location, out bounds);
            bounds = default;
            return false;
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
            if (renovating) { PresentRenovationMap(); return; }
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
            world.PresentRoutePreview(routeRequest.Incoming.Passage.Concat(routeRequest.Outgoing.Passage),
                routePreview?.IsValid == true, null);
            if (attemptedRoute != null)
                world.OverlayRouteAttempt((routeSelection.Incoming ? attemptedRoute.Incoming : attemptedRoute.Outgoing).Passage,
                    attemptReason == null);
            PresentRouteLink(true, edge.SourceNodeId, placed.NodeId);
            PresentRouteLink(false, placed.NodeId, edge.DestinationNodeId);
        }

        private void PresentRouteLink(bool incoming, string source, string destination)
        {
            var link = RouteLink(incoming);
            var from = AddDoorwayHandles(source, link.SourcePointId, incoming, true);
            var to = AddDoorwayHandles(destination, link.DestinationPointId, incoming, false);
            bool selected = routeSelection?.Incoming == incoming;
            DrawConnectedRoute(from, to, link.Passage, routePreview?.IsValid == true, selected);
            if (link.Passage.Length > 0)
                for (int i = 0; i < link.Passage.Length; i++)
                {
                    bool bend = from.Id != null && to.Id != null &&
                        AuthoredRouteGeometry.TryFacing(i == 0 ? from.World : link.Passage[i - 1], link.Passage[i], out var preceding) &&
                        AuthoredRouteGeometry.TryFacing(link.Passage[i], i + 1 == link.Passage.Length ? to.World : link.Passage[i + 1], out var following) &&
                        (int)preceding % 2 != (int)following % 2;
                    var position = CellCenter(link.Passage[i]);
                    var kind = bend ? RouteTargetKind.Bend : RouteTargetKind.Tile;
                    routeHandles.Add(new RouteTarget { Kind = kind, Position = position, Incoming = incoming, Start = i, End = i });
                    if (bend) world.AddRouteMarker(position, "◆", selected && routeSelection.Start == i, .36f);
                }
            foreach (var segment in AuthoredRouteGeometry.Segments(link.Passage))
            {
                // Place the section handle on the first tile join so tile centers are always targetable.
                var position = (CellCenter(link.Passage[segment.Start]) + CellCenter(link.Passage[segment.Start + 1])) * .5f;
                world.AddRouteMarker(position, "●", selected && routeSelection.Kind == RouteTargetKind.Section &&
                    routeSelection.Start == segment.Start, .28f);
                routeHandles.Add(new RouteTarget { Kind = RouteTargetKind.Section, Position = position, Incoming = incoming,
                    Start = segment.Start, End = segment.End });
            }
            if (!selected) return;
            world.AddRouteMarker(routeSelection.Position, "□", true, .56f);
            if (attemptedMarker.HasValue)
                world.AddRouteMarker(attemptedMarker.Value, attemptReason == null ? "+" : "×", false, .55f);
            if (attemptedRoute == null) return;
            var attempted = incoming ? attemptedRoute.Incoming : attemptedRoute.Outgoing;
            DrawConnectedRoute(RouteOpenings(source).FirstOrDefault(p => p.Id == attempted.SourcePointId),
                RouteOpenings(destination).FirstOrDefault(p => p.Id == attempted.DestinationPointId),
                attempted.Passage, attemptReason == null, true);
        }

        private void DrawConnectedRoute(RouteOpening from, RouteOpening to, TileCoordinate[] path, bool valid, bool selected)
        {
            if (from.Id == null || to.Id == null) return;
            if (path.Length == 0)
            {
                if (from.OpensToward(to.World) && to.OpensToward(from.World))
                    world.AddRouteLine(CellCenter(from.World), CellCenter(to.World), valid, selected);
                return;
            }
            if (!from.OpensToward(path[0]) || !to.OpensToward(path[path.Length - 1]) ||
                !AuthoredRouteGeometry.IsContinuous(path)) return;
            var points = new[] { OpeningCenter(from) }.Concat(path.Select(CellCenter)).Append(OpeningCenter(to)).ToArray();
            for (int i = 1; i < points.Length; i++)
                world.AddRouteLine(points[i - 1], points[i], valid, selected);
        }

        private RouteOpening AddDoorwayHandles(string nodeId, string selected, bool incoming, bool source)
        {
            RouteOpening active = default;
            bool replacing = routeSelection?.Kind == RouteTargetKind.Doorway && routeSelection.Incoming == incoming &&
                routeSelection.Source == source;
            var pending = PendingDoorways;
            string intended = pending?.Incoming == incoming
                ? source ? pending.SourcePointId : pending.DestinationPointId : null;
            var openings = RouteOpenings(nodeId);
            string neighborPointId = renovating ? null : incoming && !source ? routeRequest.Outgoing.SourcePointId :
                !incoming && source ? routeRequest.Incoming.DestinationPointId : null;
            var neighbor = openings.FirstOrDefault(point => point.Id == neighborPointId);
            foreach (var opening in openings)
            {
                bool isActive = opening.Id == selected;
                if (isActive) active = opening;
                bool isPending = !isActive && opening.Id == intended;
                if (!isActive && !isPending &&
                    (!replacing || (neighbor.Id != null && opening.Local.Equals(neighbor.Local)))) continue;
                var boundary = OpeningCenter(opening);
                // Each handle sits just inside its own room. Two doors on opposite faces
                // of the same grid line remain separately selectable.
                var position = Vector2.Lerp(boundary, CellCenter(opening.World), .48f);
                world.AddRouteMarker(position, isPending ? "×" : isActive ? "◇" : "+", replacing, .50f);
                if (!isActive && !replacing) continue;
                bool restore = replacing && isActive && intended != null && intended != selected;
                routeHandles.Add(new RouteTarget { Kind = isActive && !restore ? RouteTargetKind.Doorway : RouteTargetKind.Replacement,
                    Position = position, Opening = opening.Id, Incoming = incoming, Source = source });
            }
            return active;
        }

        private void UpdateRouteProposal()
        {
            RefreshRouteProposal();
            PresentRouteMap();
            Present();
        }

        private void RefreshRouteProposal()
        {
            routePreview = null;
            routeQuote = null;
            if (routeRequest == null) return;
            routePreview = renovating ? PreviewRenovationRoute(routeRequest, out routeQuote) :
                root.SaveService.PreviewRouteInsertion(root.Save, draft, routeRequest, out routeQuote);
        }

        private void AcceptRoute()
        {
            if (renovating)
            {
                if (manualRoute == null && routeSelection == null && PendingDoorways == null && routePreview?.IsValid == true)
                    ApplyRenovation(RenovationIntentFor(routeRequest.Incoming));
                return;
            }
            if (routeSelection != null || PendingDoorways != null || routePreview?.IsValid != true ||
                !root.SaveService.AcceptRouteInsertion(root.Save, draft, routeRequest))
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
        private Button acceptRoute, chooseRouteRoomsAgain, moveRouteRoom, cancelRouteAdjustment, undoRouteAdjustment;

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
            var actions = new VisualElement();
            actions.AddToClassList("route-actions");
            panel.Add(actions);
            var main = new VisualElement();
            main.AddToClassList("route-action-row");
            actions.Add(main);
            acceptRoute = RouteAction(main, "acceptRoute", "accept", AcceptRoute);
            RouteAction(main, "cancelRoute", "map_cancel", () =>
            {
                ClearRouteAuthoring();
                world.ClearPreview();
                feedback = null;
                Present();
            });
            chooseRouteRoomsAgain = RouteAction(main, "chooseRouteRoomsAgain", "map_choose_again", () =>
            {
                if (renovating) ClearRenovationSelection();
                routeRequest = null;
                routeStart = null;
                routeMiddleSelected = false;
                ClearRouteAdjustment();
                routeUndo.Clear();
                world.ClearPreview();
                feedback = null;
                UpdateRouteProposal();
            });
            moveRouteRoom = RouteAction(main, "moveRouteRoom", "reposition_room", () => RepositionDraftRoom(routeRoom));
            var adjustments = new VisualElement();
            adjustments.AddToClassList("route-action-row");
            actions.Add(adjustments);
            cancelRouteAdjustment = RouteAction(adjustments, "cancelRouteAdjustment", "cancel_adjustment", CancelRouteAdjustment);
            undoRouteAdjustment = RouteAction(adjustments, "undoRouteAdjustment", "undo_adjustment", UndoRouteAdjustment);
            beginManualRoute = RouteAction(adjustments, "beginManualRoute", "manual_draw", StartManualRoute);
            finishManualRoute = RouteAction(adjustments, "finishManualRoute", "manual_use", FinishManualRoute);
            BindRenovationDock(panel);
        }

        private Button RouteAction(VisualElement row, string name, string key, Action action)
        {
            var button = ButtonFor(row, Text("ui.dungeon.route." + key), action);
            button.name = name;
            return button;
        }

        private void PresentRouteAuthoring(VisualElement panel)
        {
            safe.EnableInClassList("routing", IsAuthoringRoute);
            panel.style.display = IsAuthoringRoute ? DisplayStyle.Flex : DisplayStyle.None;
            if (!IsAuthoringRoute) return;
            if (acceptRoute == null) BindRouteDock(panel);
            renovationActions.style.display = renovating ? DisplayStyle.Flex : DisplayStyle.None;
            if (renovating && routeRequest == null)
            {
                PresentRenovationDock();
                return;
            }
            if (renovating) PresentRenovationActions();
            bool valid = routePreview?.IsValid == true;
            bool proposal = routeRequest != null;
            ShowRouteAction(beginManualRoute, renovating && proposal && manualRoute == null && (!valid || PendingDoorways != null));
            ShowRouteAction(finishManualRoute, manualRoute != null);
            finishManualRoute.SetEnabled(manualRoute != null && manualReason == null);
            string instruction = manualRoute != null ? "ui.dungeon.route.manual_hint"
                : PendingDoorways != null && routeSelection == null
                ? "ui.dungeon.route.finish_doorways"
                : routeSelection?.Kind == RouteTargetKind.Doorway
                ? PendingDoorways != null ? "ui.dungeon.route.finish_doorways"
                : routeHandles.Any(handle => handle.Kind == RouteTargetKind.Replacement)
                    ? "ui.dungeon.route.pick_door" : "ui.dungeon.route.no_alternate_door"
                : routeSelection?.Kind == RouteTargetKind.Section ? RouteLink(routeSelection.Incoming).Passage[routeSelection.Start].Y ==
                    RouteLink(routeSelection.Incoming).Passage[routeSelection.End].Y
                    ? "ui.dungeon.route.slide_row" : "ui.dungeon.route.slide_column"
                : routeSelection?.Kind == RouteTargetKind.Bend ? "ui.dungeon.route.move_bend"
                : routeSelection?.Kind == RouteTargetKind.Tile ? "ui.dungeon.route.move_tile"
                : proposal ? "ui.dungeon.route.map_edit"
                : routeMiddleSelected ? "ui.dungeon.route.map_next"
                : routeStart != null ? "ui.dungeon.route.map_new" : "ui.dungeon.route.map_start";
            routeInstruction.text = Text(instruction);
            string problem = draft.Durability != DraftDurability.Acknowledged ? draft.Reason
                : manualRoute != null ? manualReason
                : (attemptReason ?? PendingDoorways?.Reason) != null ? attemptReason ?? PendingDoorways.Reason
                : proposal && !valid ? feedback ?? routePreview?.Reason
                : !renovating && (routeEdges.Length == 0 || feedback == StructuralRouteInsertionService.RetirementUnavailable)
                    ? StructuralRouteInsertionService.RetirementUnavailable : null;
            routeState.EnableInClassList("route-problem", problem != null);
            routeState.text = Text(problem == null ? valid ? routeSelection != null ? "ui.dungeon.route.adjusting" :
                "ui.dungeon.route.proposal_ready" : "ui.dungeon.route.waiting"
                : ConstructionReason(problem));
            bool saveBlockedByEditor = draft.CanSave;
            routeDetail.text = Text(saveBlockedByEditor ? "ui.dungeon.route.save_after_edit" :
                (routeSelection != null || PendingDoorways != null) && valid ? "ui.dungeon.route.previous_kept" :
                valid ? "ui.dungeon.route.preview_only" : "ui.dungeon.route.order_on_map");
            routeDetail.style.visibility = detailsCollapsed && !saveBlockedByEditor ? Visibility.Hidden : Visibility.Visible;
            acceptRoute.style.visibility = proposal ? Visibility.Visible : Visibility.Hidden;
            acceptRoute.SetEnabled(valid && manualRoute == null && routeSelection == null && PendingDoorways == null &&
                draft.Durability == DraftDurability.Acknowledged);
            ShowRouteAction(chooseRouteRoomsAgain, proposal);
            ShowRouteAction(moveRouteRoom, !renovating && draft.DisconnectedRoomIds.Contains(routeRoom));
            cancelRouteAdjustment.style.visibility = manualRoute != null || routeSelection != null || PendingDoorways != null
                ? Visibility.Visible : Visibility.Hidden;
            undoRouteAdjustment.style.visibility = manualRoute != null || PendingDoorways != null || (routeUndo.Count > 0 && routeSelection == null)
                ? Visibility.Visible : Visibility.Hidden;
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
                        StructuralEconomyPresenter.FormatTransactionAmount(quote.Cost - quote.CreditedRefund),
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
                return routeSelection != null || PendingDoorways != null ? "ui.dungeon.route.no_connection" : "ui.dungeon.route.opening_mismatch";
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
