using System;
using System.Linq;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0
{
    public sealed partial class ProductionDungeonController
    {
        private bool renovating, pickingMove, pickingBranch, pickingContentDestination;
        private string renovationNode, movingStructure, branchPointId, selectedCorridorContent;
        private CardinalOrientation moveOrientation;
        private StructuralRouteRenovation renovationIntent;
        private StructuralRouteRenovation branchDraft, structureMoveDraft;
        private StructuralEconomyPreview renovationQuote;
        private CorridorContentAssignment[] displacedCorridorContents = Array.Empty<CorridorContentAssignment>();
        private string corridorRenovationBlocker;
        private VisualElement renovationActions;
        private Button moveStructureAction, rotateStructureAction, confirmStructureMove, disconnectAction, retireAction, confirmBranchAction;
        private Button returnCorridorContentAction, moveCorridorContentAction, cancelCorridorDispositionAction, nextCorridorContentAction;

        private CorridorContentAssignment ActiveDisplacedContent => displacedCorridorContents
            .FirstOrDefault(item => item.FloorInstanceId == selectedFloor && item.AssignmentId == selectedCorridorContent) ??
            displacedCorridorContents.FirstOrDefault(item => item.FloorInstanceId == selectedFloor);

        private StructuralRouteRenovation ActiveCorridorDisposition => draft?.CorridorDispositions()
            .FirstOrDefault(intent => intent.TargetId == ActiveDisplacedContent?.AssignmentId);

        private void OpenRenovation()
        {
            if (!IsEditing || DraftSelectedFloor()?.ActivationState != FloorActivationState.Active) return;
            CloseSheet();
            renovating = true;
            detailsCollapsed = false;
            autoFit = false;
            interactionFocus = null;
            viewport.Configure(RouteInteractionBounds, viewport.ScreenRect, false);
            feedback = null;
            RefreshCorridorRenovationConsequences();
            PresentRouteMap();
            Present();
        }

        private void ClearRenovationSelection()
        {
            renovationIntent = null;
            renovationNode = null;
            movingStructure = null;
            branchPointId = null;
            branchDraft = null;
            structureMoveDraft = null;
            renovationQuote = null;
            pickingMove = pickingBranch = pickingContentDestination = false;
        }

        private void ResetRenovationSelection()
        {
            ClearRenovationSelection();
            routeStart = null;
            routeRequest = null;
            ClearRouteAdjustment();
            routeUndo.Clear();
            world.ClearPreview();
        }

        private void RefreshCorridorRenovationConsequences()
        {
            displacedCorridorContents = root.SaveService.DisplacedCorridorContents(root.Save, draft, out corridorRenovationBlocker);
        }

        private void ApplyRenovation(StructuralRouteRenovation command)
        {
            if (!root.SaveService.AcceptRenovation(root.Save, draft, command, out var reason))
            {
                feedback = reason;
                Present();
                return;
            }
            ResetRenovationSelection();
            feedback = command.IsCorridorDisposition ? null : command.Kind == StructuralRouteRenovationKind.Reset
                ? "ui.dungeon.route.rebuild_start" : "ui.dungeon.route.connection_accepted";
            RebuildFloor(false);
            Present();
        }

        private bool TryRenovationMap(Vector2 point, TileCoordinate cell)
        {
            var floor = DraftSelectedFloor();
            if (draft.Durability != DraftDurability.Acknowledged || floor == null) return true;
            if (pickingMove && movingStructure != null)
            {
                structureMoveDraft = new StructuralRouteRenovation { Kind = StructuralRouteRenovationKind.MoveStructure,
                    FloorInstanceId = selectedFloor, TargetId = movingStructure, Anchor = cell, Orientation = moveOrientation };
                PreviewStructureMove();
                return true;
            }
            if (pickingContentDestination && ActiveDisplacedContent != null)
            {
                var edge = floor.Layout.Edges.SingleOrDefault(value => value.Classification == RouteClassification.Optional &&
                    value.CorridorTiles.Contains(cell));
                if (edge != null)
                    ApplyRenovation(new StructuralRouteRenovation { Kind = StructuralRouteRenovationKind.ReassignCorridorContent,
                        FloorInstanceId = selectedFloor, TargetId = ActiveDisplacedContent.AssignmentId,
                        DestinationNodeId = edge.OptionalBranchId, Anchor = cell });
                return true;
            }
            if (pickingBranch) return PickBranch(point, cell, floor);

            var hit = world.SelectCorridor(cell) ?? world.ConnectionVisuals
                .Where(visual => ((Vector2)visual.Center - point).sqrMagnitude <= .09f)
                .OrderBy(visual => ((Vector2)visual.Center - point).sqrMagnitude)
                .Select(visual => visual.EdgeId).FirstOrDefault();
            var connected = floor.Layout.Edges.SingleOrDefault(edge => edge.EdgeId == hit);
            if (connected != null && routeStart == null)
            {
                if (connected.Classification == RouteClassification.Optional)
                {
                    pickingBranch = true;
                    renovationNode = connected.SourceNodeId;
                    renovationIntent = new StructuralRouteRenovation { Kind = StructuralRouteRenovationKind.ConnectBranch,
                        FloorInstanceId = selectedFloor, SourceNodeId = connected.SourceNodeId, TargetId = connected.EdgeId };
                    var geometry = connected.AuthoredGeometry;
                    if (geometry == null && OptionalBranchGeometry.TryResolveSourceTile(floor, connected,
                        root.ProductionSpatialContent.Catalog, out var start))
                    {
                        var tiles = connected.CorridorTiles.OrderBy(tile =>
                            Math.Abs((long)tile.X - start.X) + Math.Abs((long)tile.Y - start.Y)).ToArray();
                        if (tiles.Length > 0 && AuthoredRouteGeometry.TryFacing(start, tiles[0], out var facing))
                        {
                            var source = RouteOpenings(connected.SourceNodeId)
                                .SingleOrDefault(opening => opening.World.Equals(start) && opening.Facing == facing);
                            if (source.Id != null) geometry = new AuthoredRouteGeometry
                                { SourcePointId = source.Id, DestinationPointId = string.Empty, Passage = tiles };
                        }
                    }
                    branchPointId = geometry?.SourcePointId;
                    if (geometry != null) PreviewBranch(geometry);
                    ShowRenovationMap();
                }
                else OpenRenovationLink(connected.SourceNodeId, connected.DestinationNodeId, connected);
                return true;
            }

            var node = floor.Layout.Nodes.FirstOrDefault(value => TryRouteNodeBounds(floor, value, out var bounds) && bounds.Contains(point));
            if (node == null) return true;
            feedback = null;
            if (routeStart != null && node.NodeId != routeStart && node.Kind != FloorRouteNodeKind.Entrance)
            {
                OpenRenovationLink(routeStart, node.NodeId, floor.Layout.Edges.SingleOrDefault(edge =>
                    edge.Classification == RouteClassification.Required && edge.SourceNodeId == routeStart &&
                    edge.DestinationNodeId == node.NodeId));
                return true;
            }
            renovationNode = node.NodeId;
            routeStart = node.Kind == FloorRouteNodeKind.Completion ? null : node.NodeId;
            ShowRenovationMap();
            return true;
        }

        private void OpenRenovationLink(string from, string to, FloorRouteEdge existing)
        {
            renovationIntent = new StructuralRouteRenovation { Kind = StructuralRouteRenovationKind.Connect,
                FloorInstanceId = selectedFloor, SourceNodeId = from, DestinationNodeId = to, TargetId = existing?.EdgeId };
            var fromOpenings = RouteOpenings(from);
            var toOpenings = RouteOpenings(to);
            if (fromOpenings.Length == 0 || toOpenings.Length == 0)
            {
                feedback = StructuralEditService.ConnectionUnavailableReason;
                Present();
                return;
            }
            var geometry = existing?.AuthoredGeometry?.Copy();
            if (geometry == null && existing != null &&
                StructuralRenovationService.TryResolveSavedConnection(DraftSelectedFloor(), existing,
                    root.ProductionSpatialContent.Catalog, out var source, out var destination))
            {
                var points = AuthoredRouteGeometry.Openings(DraftSelectedFloor(), from, root.ProductionSpatialContent.Catalog, inferred: true);
                var ends = AuthoredRouteGeometry.Openings(DraftSelectedFloor(), to, root.ProductionSpatialContent.Catalog, inferred: true);
                var start = points.Single(point => point.Local.Equals(source));
                geometry = new AuthoredRouteGeometry { SourcePointId = start.Id,
                    DestinationPointId = ends.Single(point => point.Local.Equals(destination)).Id,
                    Passage = (existing.CorridorTiles ?? Array.Empty<TileCoordinate>()).OrderBy(tile => Math.Abs((long)tile.X - start.World.X) +
                        Math.Abs((long)tile.Y - start.World.Y)).ToArray() };
            }
            geometry ??= root.SaveService.SuggestRenovation(root.Save, draft, renovationIntent);
            routeRequest = new StructuralRouteInsertionRequest { FloorInstanceId = selectedFloor,
                Incoming = geometry ?? new AuthoredRouteGeometry { SourcePointId = fromOpenings[0].Id,
                    DestinationPointId = toOpenings[0].Id }, Outgoing = new AuthoredRouteGeometry() };
            routeStart = null;
            ClearRouteAdjustment();
            routeUndo.Clear();
            UpdateRouteProposal();
        }

        private StructuralRouteRenovation RenovationIntentFor(AuthoredRouteGeometry link) =>
            new StructuralRouteRenovation { Kind = renovationIntent.Kind, FloorInstanceId = selectedFloor,
                TargetId = renovationIntent.TargetId, SourceNodeId = renovationIntent.SourceNodeId,
                DestinationNodeId = renovationIntent.DestinationNodeId, Connections = new[] { link } };

        private StructuralRouteInsertionPreview PreviewRenovationRoute(StructuralRouteInsertionRequest request,
            out StructuralEconomyPreview quote)
        {
            var result = root.SaveService.PreviewRenovation(root.Save, draft, RenovationIntentFor(request.Incoming), out quote);
            return new StructuralRouteInsertionPreview { Reason = result.Reason, Candidate = result.Candidate,
                UsedFloorSpace = result.UsedFloorSpace, FloorCapacity = result.FloorCapacity };
        }

        private void BeginStructureRelocation()
        {
            var floor = DraftSelectedFloor();
            var node = floor.Layout.Nodes.SingleOrDefault(value => value.NodeId == renovationNode);
            if (node == null) return;
            var room = floor.Layout.Rooms.SingleOrDefault(value => value.RoomInstanceId == node.RoomInstanceId);
            var fixedStructure = room == null ? floor.FixedStructures.SingleOrDefault(value =>
                value.Kind == (node.Kind == FloorRouteNodeKind.Entrance ? FixedSpatialStructureKind.Entrance :
                    FixedSpatialStructureKind.CompletionTerminal)) : null;
            movingStructure = room?.RoomInstanceId ?? fixedStructure?.FixedStructureInstanceId;
            if (movingStructure == null) return;
            moveOrientation = room?.Orientation ?? fixedStructure.Orientation;
            pickingMove = true;
            routeStart = null;
            feedback = null;
            ShowRenovationMap();
        }

        private void RotateRenovationStructure()
        {
            var floor = DraftSelectedFloor();
            var catalog = root.ProductionSpatialContent.Catalog;
            var room = floor.Layout.Rooms.SingleOrDefault(value => value.RoomInstanceId == movingStructure);
            var fixedStructure = floor.FixedStructures.SingleOrDefault(value => value.FixedStructureInstanceId == movingStructure);
            var orientations = room == null
                ? catalog.FixedStructures.Single(value => value.StructureDefinitionId == fixedStructure.FixedStructureDefinitionId).AllowedOrientations
                : catalog.Rooms.Single(value => value.RoomDefinitionId == room.RoomDefinitionId).AllowedOrientations;
            moveOrientation = orientations[(Array.IndexOf(orientations, moveOrientation) + 1) % orientations.Length];
            if (structureMoveDraft == null) Present();
            else
            {
                structureMoveDraft.Orientation = moveOrientation;
                PreviewStructureMove();
            }
        }

        private void PreviewStructureMove()
        {
            var floor = DraftSelectedFloor();
            var catalog = root.ProductionSpatialContent.Catalog;
            var room = floor.Layout.Rooms.SingleOrDefault(value => value.RoomInstanceId == movingStructure);
            var structure = floor.FixedStructures.SingleOrDefault(value => value.FixedStructureInstanceId == movingStructure);
            var footprint = room != null
                ? catalog.Rooms.Single(value => value.RoomDefinitionId == room.RoomDefinitionId).GrossFootprint
                : catalog.FixedStructures.Single(value => value.StructureDefinitionId == structure.FixedStructureDefinitionId).GrossFootprint;
            var workload = new SpatialValidationWorkloadLimits(draftContext.Limits.Canonical.Spatial.MaximumMaterializedTiles);
            var preview = root.SaveService.PreviewRenovation(root.Save, draft, structureMoveDraft, out renovationQuote);
            feedback = preview.Reason;
            if (TileFootprintResolver.TryResolveRectangle(footprint, structureMoveDraft.Anchor, structureMoveDraft.Orientation,
                workload, out var tiles))
                world.PresentRoutePreview(tiles.OccupiedTiles, preview.IsValid, null);
            Present();
        }

        private bool PickBranch(Vector2 point, TileCoordinate cell, SavedSpatialFloor floor)
        {
            if (renovationNode == null)
            {
                string room = world.SelectRoom(cell, false);
                renovationNode = floor.Layout.Nodes.SingleOrDefault(value => value.RoomInstanceId == room && room != null)?.NodeId;
                if (renovationNode != null) renovationIntent = new StructuralRouteRenovation { Kind = StructuralRouteRenovationKind.ConnectBranch,
                    FloorInstanceId = selectedFloor, SourceNodeId = renovationNode };
            }
            else
            {
                var openings = RouteOpenings(renovationNode);
                var clicked = openings.OrderBy(opening => (RenovationOpeningHandle(opening) - point).sqrMagnitude).FirstOrDefault();
                if (clicked.Id != null && (RenovationOpeningHandle(clicked) - point).sqrMagnitude <= .12f)
                {
                    branchPointId = clicked.Id;
                    branchDraft = null;
                    renovationQuote = null;
                    feedback = null;
                }
                else if (branchPointId != null)
                {
                    var start = openings.Single(opening => opening.Id == branchPointId);
                    ExtendBranch(start, cell);
                }
            }
            ShowRenovationMap();
            return true;
        }

        private void ExtendBranch(RouteOpening opening, TileCoordinate cell)
        {
            var passage = branchDraft?.Connection.Passage ?? Array.Empty<TileCoordinate>();
            int trim = Array.IndexOf(passage, cell);
            if (trim >= 0)
            {
                PreviewBranch(new AuthoredRouteGeometry { SourcePointId = opening.Id, DestinationPointId = string.Empty,
                    Passage = passage.Take(trim + 1).ToArray() });
                return;
            }
            var from = passage.Length == 0 ? opening.World : passage[passage.Length - 1];
            long dx = (long)cell.X - from.X, dy = (long)cell.Y - from.Y;
            if ((dx == 0) == (dy == 0))
            {
                feedback = StructuralEditService.ConnectionUnavailableReason;
                return;
            }
            long length = Math.Abs(dx) + Math.Abs(dy);
            var corridor = root.ProductionSpatialContent.Catalog.Corridors.Single(value =>
                value.CorridorDefinitionId == OptionalBranchStructuralEditService.CorridorDefinitionId);
            if (passage.Length + length > corridor.MaximumLength)
            {
                feedback = StructuralEditService.CorridorLengthReason;
                return;
            }
            int horizontal = Math.Sign(dx), vertical = Math.Sign(dy);
            var first = new TileCoordinate(from.X + horizontal, from.Y + vertical);
            if (passage.Length == 0 &&
                (!AuthoredRouteGeometry.TryFacing(from, first, out var facing) || facing != opening.Facing))
            {
                feedback = StructuralEditService.ConnectionUnavailableReason;
                return;
            }
            var path = passage.Concat(Enumerable.Range(1, (int)length)
                .Select(step => new TileCoordinate(from.X + step * horizontal, from.Y + step * vertical))).ToArray();
            PreviewBranch(new AuthoredRouteGeometry { SourcePointId = opening.Id, DestinationPointId = string.Empty, Passage = path });
        }

        private void PreviewBranch(AuthoredRouteGeometry geometry)
        {
            var intent = RenovationIntentFor(geometry);
            var preview = root.SaveService.PreviewRenovation(root.Save, draft, intent, out renovationQuote);
            feedback = preview.Reason;
            if (preview.IsValid) branchDraft = intent;
        }

        private static Vector2 RenovationOpeningHandle(RouteOpening opening) =>
            Vector2.Lerp(OpeningCenter(opening), CellCenter(opening.World), .48f);

        private void ShowRenovationMap()
        {
            PresentRouteMap();
            Present();
        }

        private void PresentRenovationMap()
        {
            var floor = DraftSelectedFloor();
            if (floor == null) return;
            var node = floor.Layout.Nodes.SingleOrDefault(value => value.Kind == FloorRouteNodeKind.Entrance);
            var visited = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            int index = 0;
            while (node != null && visited.Add(node.NodeId))
            {
                index++;
                if (TryRouteNodeBounds(floor, node, out var bounds))
                    world.AddRouteMarker(bounds.center, index.ToString(Culture), node.NodeId == renovationNode, .75f);
                var next = floor.Layout.Edges.SingleOrDefault(edge => edge.Classification == RouteClassification.Required &&
                    edge.SourceNodeId == node.NodeId);
                node = next == null ? null : floor.Layout.Nodes.Single(value => value.NodeId == next.DestinationNodeId);
            }
            foreach (var disconnected in floor.Layout.Nodes.Where(value => !visited.Contains(value.NodeId)))
                if (TryRouteNodeBounds(floor, disconnected, out var bounds))
                    world.AddRouteMarker(bounds.center, "+", disconnected.NodeId == renovationNode, .65f);
            if (routeRequest != null)
            {
                world.PresentRoutePreview(routeRequest.Incoming.Passage, routePreview?.IsValid == true, null);
                PresentRouteLink(true, renovationIntent.SourceNodeId, renovationIntent.DestinationNodeId);
                if (manualRoute != null)
                    world.OverlayRouteAttempt(manualRoute.Incoming.Passage, manualReason == null);
            }
            else if (pickingBranch && renovationNode != null)
            {
                foreach (var opening in RouteOpenings(renovationNode))
                    world.AddRouteMarker(RenovationOpeningHandle(opening), opening.Id == branchPointId ? "◇" : "+",
                        opening.Id == branchPointId, .48f);
                if (branchDraft != null) world.PresentRoutePreview(branchDraft.Connection.Passage, true, null);
            }
        }

        private void BindRenovationDock(VisualElement panel)
        {
            renovationActions = new VisualElement();
            renovationActions.AddToClassList("route-actions");
            panel.Add(renovationActions);
            var rows = new[] { new VisualElement(), new VisualElement(), new VisualElement() };
            foreach (var row in rows) { row.AddToClassList("route-action-row"); renovationActions.Add(row); }
            RouteAction(rows[0], "resetHallways", "reset_hallways", () =>
            {
                ShowModal("ui.dungeon.route.reset_confirm", () => ApplyRenovation(new StructuralRouteRenovation
                    { Kind = StructuralRouteRenovationKind.Reset, FloorInstanceId = selectedFloor }), "ui.dungeon.route.reset_hallways");
            });
            moveStructureAction = RouteAction(rows[0], "moveStructure", "move_structure", BeginStructureRelocation);
            rotateStructureAction = RouteAction(rows[0], "rotateStructure", "rotate_structure", RotateRenovationStructure);
            confirmStructureMove = RouteAction(rows[1], "confirmStructureMove", "confirm_move", () => ApplyRenovation(structureMoveDraft));
            RouteAction(rows[0], "startOptionalBranch", "add_branch", () =>
            {
                ResetRenovationSelection(); pickingBranch = true; feedback = null; ShowRenovationMap();
            });
            disconnectAction = RouteAction(rows[1], "disconnectRoute", "disconnect", () => RemoveRenovationLink(false));
            retireAction = RouteAction(rows[1], "retireRoute", "retire", () => RemoveRenovationLink(true));
            confirmBranchAction = RouteAction(rows[1], "acceptOptionalBranch", "accept", () => ApplyRenovation(branchDraft));
            returnCorridorContentAction = RouteAction(rows[2], "returnCorridorContent", "return_displaced", () =>
            {
                var item = ActiveDisplacedContent;
                if (item != null) ApplyRenovation(new StructuralRouteRenovation { Kind = StructuralRouteRenovationKind.ReturnCorridorContent,
                    FloorInstanceId = selectedFloor, TargetId = item.AssignmentId });
            });
            moveCorridorContentAction = RouteAction(rows[2], "moveCorridorContent", "move_displaced", () =>
            {
                ResetRenovationSelection(); pickingContentDestination = true; feedback = null; ShowRenovationMap();
            });
            cancelCorridorDispositionAction = RouteAction(rows[2], "cancelCorridorDisposition", "cancel_disposition", () =>
            {
                var item = ActiveDisplacedContent;
                if (item != null) ApplyRenovation(new StructuralRouteRenovation
                    { Kind = StructuralRouteRenovationKind.CancelCorridorDisposition, FloorInstanceId = selectedFloor, TargetId = item.AssignmentId });
            });
            nextCorridorContentAction = RouteAction(rows[2], "nextCorridorContent", "next_corridor_content", () =>
            {
                var items = displacedCorridorContents.Where(item => item.FloorInstanceId == selectedFloor).ToArray();
                if (items.Length < 2) return;
                int index = Array.IndexOf(items, ActiveDisplacedContent);
                selectedCorridorContent = items[(index + 1) % items.Length].AssignmentId;
                pickingContentDestination = false;
                feedback = null;
                ShowRenovationMap();
            });
        }

        private void RemoveRenovationLink(bool retire)
        {
            var intent = new StructuralRouteRenovation
            {
                Kind = retire ? StructuralRouteRenovationKind.Retire : StructuralRouteRenovationKind.Disconnect,
                FloorInstanceId = selectedFloor, TargetId = renovationIntent?.TargetId
            };
            var preview = root.SaveService.PreviewRenovation(root.Save, draft, intent, out var quote);
            if (!preview.IsValid) { feedback = preview.Reason; Present(); return; }
            ShowModal(retire ? "ui.dungeon.route.retire_confirm" : "ui.dungeon.route.disconnect_confirm",
                () => ApplyRenovation(intent), retire ? "ui.dungeon.route.retire" : "ui.dungeon.route.disconnect");
            if (quote != null)
                document.rootVisualElement.Q<Label>("modalText").text += "\n" +
                    StructuralEconomyPresenter.Present(quote, Text, root.PassiveManaPerHourForPresentation);
        }

        private void PresentRenovationActions()
        {
            var item = ActiveDisplacedContent;
            ShowRouteAction(moveStructureAction, renovationNode != null && renovationIntent == null && !pickingBranch && !pickingMove);
            ShowRouteAction(rotateStructureAction, pickingMove);
            ShowRouteAction(confirmStructureMove, pickingMove && structureMoveDraft != null);
            confirmStructureMove.SetEnabled(structureMoveDraft != null && feedback == null &&
                draft.Durability == DraftDurability.Acknowledged);
            ShowRouteAction(disconnectAction, renovationIntent?.TargetId != null);
            var original = CommittedSelectedFloor()?.Layout.Edges.SingleOrDefault(value => value.EdgeId == renovationIntent?.TargetId);
            ShowRouteAction(retireAction, original?.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor);
            ShowRouteAction(confirmBranchAction, branchDraft != null);
            confirmBranchAction.SetEnabled(branchDraft != null && feedback == null && draft.Durability == DraftDurability.Acknowledged);
            ShowRouteAction(returnCorridorContentAction, root.SaveService.CanReturnDisplacedCorridorContent(item));
            ShowRouteAction(moveCorridorContentAction, item != null);
            ShowRouteAction(cancelCorridorDispositionAction, ActiveCorridorDisposition != null);
            ShowRouteAction(nextCorridorContentAction, displacedCorridorContents.Count(value => value.FloorInstanceId == selectedFloor) > 1);
        }

        private void PresentRenovationDock()
        {
            PresentRenovationActions();
            string instruction = pickingMove ? "ui.dungeon.route.move_destination" :
                pickingContentDestination ? "ui.dungeon.route.content_destination" :
                pickingBranch ? renovationNode == null ? "ui.dungeon.route.branch_room" :
                    branchPointId == null ? "ui.dungeon.route.branch_opening" : "ui.dungeon.route.branch_end" :
                routeStart == null ? "ui.dungeon.route.rebuild_start" : "ui.dungeon.route.rebuild_next";
            routeInstruction.text = Text(instruction);
            string status = feedback ?? (draft.Durability == DraftDurability.Acknowledged ? null : draft.Reason);
            routeState.text = status == null ? string.Empty : Text(ConstructionReason(status));
            routeState.EnableInClassList("route-problem", feedback != null && feedback != "ui.dungeon.route.connection_accepted");
            routeMetrics.text = feedback == null && renovationQuote != null
                ? StructuralEconomyPresenter.Present(renovationQuote, Text, root.PassiveManaPerHourForPresentation)
                : string.Empty;
            var item = ActiveDisplacedContent;
            routeDetail.text = item == null ? corridorRenovationBlocker == null ? string.Empty : Text(corridorRenovationBlocker) :
                Format(ActiveCorridorDisposition?.Kind == StructuralRouteRenovationKind.ReturnCorridorContent
                    ? "ui.dungeon.route.disposition_return" : ActiveCorridorDisposition != null
                        ? "ui.dungeon.route.disposition_move" : "ui.dungeon.route.affected_content",
                    MvpDungeonPlacementPresenter.ResolveOptionName(item.OptionId,
                    (key, fallback) => root.Content.GetString(key, fallback)));
            routeDetail.style.visibility = Visibility.Visible;
            acceptRoute.style.visibility = Visibility.Hidden;
            ShowRouteAction(chooseRouteRoomsAgain, routeStart != null || pickingMove || pickingBranch || pickingContentDestination);
            ShowRouteAction(moveRouteRoom, false);
            cancelRouteAdjustment.style.visibility = Visibility.Hidden;
            undoRouteAdjustment.style.visibility = Visibility.Hidden;
            ShowRouteAction(beginManualRoute, false);
            ShowRouteAction(finishManualRoute, false);
        }
    }
}
