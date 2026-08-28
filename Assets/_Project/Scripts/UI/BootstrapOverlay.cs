using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using DungeonBuilder.M0.Gameplay.Structures;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0
{
    public class BootstrapOverlay : MonoBehaviour
    {
        [Header("UI")]
        public TMP_Text overlayText;

        private const int DiagnosticsPageCount = 9;
        private const int RuntimeSummaryPage = 0;
        private const int RunDiagnosticsPage = 1;
        private const int HeatDiagnosticsPage = 2;
        private const int SystemsDiagnosticsPage = 3;
        private const int ResearchDiagnosticsPage = 4;
        private const int ResearchStatusPresentationDiagnosticsPage = 5;
        private const int ResearchStatusSafetyDiagnosticsPage = 6;
        private const int ResearchVerificationBoundaryDiagnosticsPage = 7;
        private const int ResearchVerificationSafetyDiagnosticsPage = 8;
        private const int VisibleDiagnosticsBodyLineCount = 4;
        private const int VisiblePlayerFacingLineCount = 28;
        private const int PlayerFacingSectionCount = 4;
        private const int PlayerFacingSectionFull = 0;
        private const int PlayerFacingSectionLoopSummary = 1;
        private const int PlayerFacingSectionPlanAndAction = 2;
        private const int PlayerFacingSectionLatestRunFeedback = 3;
        private const float MinimalMvpActionPanelMinimumWidth = 300f;
        private const float MinimalMvpActionPanelMaximumWidth = 500f;
        private const float MinimalMvpActionPanelViewportWidthRatio = 0.34f;
        private const float MinimalMvpActionPanelMargin = 10f;
        private const float MinimalMvpActionPanelLabelHeight = 17f;
        private const float MinimalMvpActionPanelButtonHeight = 19f;
        private const float MinimalMvpActionPanelScrollBarWidth = 16f;
        private const float OverlayTextSafeLeftMargin = 24f;
        private const float OverlayTextSafeTopMargin = 14f;
        private const float OverlayTextSafeBottomMargin = 10f;
        private const float OverlayTextRightCollapsedActionPanelReserve = 96f;
        private const string DefaultMvpStructureId = StructureSimulationPass.ManaGeneratorBasicId;
        private const string DefaultMvpPlacementCategoryId = MvpDungeonPlacementIds.RoomCategoryId;
        private const string DefaultMvpPlacementOptionId = MvpDungeonPlacementIds.BasicRoomOptionId;
        private const string AddBasicRoomSlotButtonKey = "ui.mvp_room_slots.add_basic_room_slot_button";
        private const string AddBasicRoomSlotSuccessKey = "ui.mvp_room_slots.add_basic_room_slot_success";
        private const string AddBasicRoomSlotAlreadyExistsKey = "ui.mvp_room_slots.add_basic_room_slot_already_exists";
        private const string NarrowHallRepairActionKey = "save.migration.spatial.gd66.repair.narrow_hall_to_basic";

        private GameRoot _root;
        private bool _devPanelVisible;
        private bool _runDiagnosticsOnlyVisible;
        private bool _diagnosticsVisible;
        private int _fullDiagnosticsPage;
        private readonly int[] _fullDiagnosticsPageScrollOffsets = new int[DiagnosticsPageCount];
        private Vector2 _devPanelScrollPosition;
        private Vector2 _minimalMvpActionPanelScrollPosition;
        private string _selectedMvpStructureId = DefaultMvpStructureId;
        private string _selectedMvpPlacementCategoryId = DefaultMvpPlacementCategoryId;
        private string _selectedMvpPlacementOptionId = DefaultMvpPlacementOptionId;
        private string _selectedMvpRunPostureId = RunPostureResolver.BalancedId;
        private string _mvpStructurePlacementFeedback = string.Empty;
        private bool _roomSlotPlacementFailureIsLatestAction;
        private string _mvpRunResultFeedback = string.Empty;
        private AdventurerRunIntentSummary _lastRunIntentSummary;
        private string _lastRunPostureUsedId = string.Empty;
        private string _lastRunDebugPostureId = string.Empty;
        private bool _lastRunIntentFallbackUsed;
        private int _playerFacingScrollOffset;
        private bool _compactSmokeViewEnabled;
        private int _playerFacingSectionIndex;
        private bool _minimalMvpActionPanelCollapsed;
        private string _smokeViewportStatusMessage = string.Empty;
        private string _selectedStructuralRoomDefinitionId;
        private TileCoordinate _selectedStructuralAnchor;
        private TileCoordinate _selectedRenovationAnchor;
        private CardinalOrientation _selectedStructuralOrientation;
        private string _selectedStructuralTerminalConnectionPointId;
        private string _structuralFeedback = string.Empty;
        private string _selectedRenovationRoomInstanceId;

        public int FullDiagnosticsPageNumber => _fullDiagnosticsPage + 1;
        public int FullDiagnosticsScrollOffset => _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage];
        public int PlayerFacingScrollOffset => _playerFacingScrollOffset;
        public bool CompactSmokeViewEnabled => _compactSmokeViewEnabled;
        public int PlayerFacingSectionNumber => _playerFacingSectionIndex + 1;
        public bool MinimalMvpActionPanelCollapsed => _minimalMvpActionPanelCollapsed;
        public bool DevPanelVisible => _devPanelVisible;
        public bool DiagnosticsVisible => DiagnosticsAllowed && (_diagnosticsVisible || _runDiagnosticsOnlyVisible);
        private bool DiagnosticsAllowed => _root != null && _root.DevPanelEnabled;
        public bool PlayerFacingPanelsVisible => !_runDiagnosticsOnlyVisible;
        public bool NormalGameplayActionsAvailable => _root?.Save != null;
        public bool NarrowHallRepairOnlyVisible => _root != null && _root.Save == null &&
            _root.SaveService != null && _root.SaveService.NarrowHallRepairAvailable;
        public bool MinimalMvpActionGuiVisible => _root != null && PlayerFacingPanelsVisible &&
            !_minimalMvpActionPanelCollapsed && (NormalGameplayActionsAvailable || NarrowHallRepairOnlyVisible);
        public Vector2 MinimalMvpActionPanelScrollPosition => _minimalMvpActionPanelScrollPosition;
        public string SelectedMvpStructureId => _selectedMvpStructureId;
        public string SelectedMvpPlacementCategoryId => _selectedMvpPlacementCategoryId;
        public string SelectedMvpPlacementOptionId => _selectedMvpPlacementOptionId;
        public string SelectedMvpRunPostureId => _selectedMvpRunPostureId;
        public string MvpStructurePlacementFeedback => _mvpStructurePlacementFeedback;
        public string MvpRunResultFeedback => _mvpRunResultFeedback;
        public string SelectedStructuralRoomDefinitionId => _selectedStructuralRoomDefinitionId;
        public TileCoordinate SelectedStructuralAnchor => _selectedStructuralAnchor;
        public TileCoordinate SelectedRenovationAnchor => _selectedRenovationAnchor;
        public CardinalOrientation SelectedStructuralOrientation => _selectedStructuralOrientation;
        public string SelectedStructuralTerminalConnectionPointId => _selectedStructuralTerminalConnectionPointId;
        public string StructuralFeedback => _structuralFeedback;
        public bool StructuralConstructionControlsAvailable => ResolveCanonicalStructuralRooms().Length != 0;
        public bool StructuralRenovationControlsAvailable => ResolveRenovationRoomIds().Length != 0;
        public string SelectedRenovationRoomInstanceId => _selectedRenovationRoomInstanceId;

        public PlayerResearchPanelPresentation ResolvePlayerResearchPanelPresentation()
        {
            return PlayerResearchPanelPresenter.Present(
                _root?.ResolvePlayerResearchState(),
                (key, fallback) => GetLocalizedString(key, fallback));
        }

        public PlayerResearchActionResult StartPlayerResearch()
        {
            PlayerResearchActionResult result = _root?.StartConfiguredPlayerResearch();
            PresentPlayerResearchFeedback(result);
            return result;
        }

        public PlayerResearchActionResult ClaimPlayerResearch()
        {
            PlayerResearchActionResult result = _root?.ClaimConfiguredPlayerResearch();
            PresentPlayerResearchFeedback(result);
            return result;
        }

        private void PresentPlayerResearchFeedback(PlayerResearchActionResult result)
        {
            if (_root == null || result == null) return;
            _root.SetBanner(GetLocalizedString(result.FeedbackLocalizationKey, result.FeedbackLocalizationKey));
            RefreshOverlayText();
        }

        public void Bind(GameRoot root)
        {
            _root = root;
            RefreshStructuralConstructionAuthority();
        }

        public void RefreshStructuralConstructionAuthority()
        {
            ReconcileStructuralSelection(string.IsNullOrEmpty(_selectedStructuralRoomDefinitionId));
            ReconcileRenovationSelection();
        }

        public void SynchronizeStructuralConstructionPublication()
        {
            _structuralFeedback = string.Empty;
            RefreshStructuralConstructionAuthority();
        }

        public string[] SelectableStructuralRoomDefinitionIds => ResolveCanonicalStructuralRooms()
            .Select(value => value.RoomDefinitionId).ToArray();

        public CardinalOrientation[] SelectableStructuralOrientations =>
            (ResolveSelectedStructuralRoom()?.AllowedOrientations ?? Array.Empty<CardinalOrientation>())
                .Distinct().OrderBy(value => value).ToArray();

        public string[] SelectableStructuralConnectionPointIds => OrderedStructuralConnectionPoints()
            .Select(value => value.ConnectionPointId).ToArray();

        public string SelectedStructuralRoomDisplayName => ResolveStructuralRoomDisplayName(
            ResolveSelectedStructuralRoom());
        public string StructuralAnchorDisplay => string.Format(CultureInfo.InvariantCulture,
            GetLocalizedString("ui.structural.anchor.format"), _selectedStructuralAnchor.X,
            _selectedStructuralAnchor.Y);
        public string RenovationAnchorDisplay => string.Format(CultureInfo.InvariantCulture,
            GetLocalizedString("ui.structural.anchor.format"), _selectedRenovationAnchor.X,
            _selectedRenovationAnchor.Y);
        public string StructuralConnectionPointDisplay => BuildStructuralConnectionPointDisplay();

        public bool CycleStructuralRoom()
        {
            RoomSpatialDefinition[] rooms = ResolveCanonicalStructuralRooms();
            if (rooms.Length == 0) return false;
            int index = Array.FindIndex(rooms, value => value.RoomDefinitionId == _selectedStructuralRoomDefinitionId);
            _selectedStructuralRoomDefinitionId = rooms[(index + 1 + rooms.Length) % rooms.Length].RoomDefinitionId;
            ReconcileStructuralSelection(false); InvalidateStructuralPreview(); return true;
        }

        public bool CycleStructuralOrientation()
        {
            RoomSpatialDefinition room = ResolveSelectedStructuralRoom();
            CardinalOrientation[] values = (room?.AllowedOrientations ?? Array.Empty<CardinalOrientation>())
                .Distinct().OrderBy(value => value).ToArray();
            if (values.Length == 0) return false;
            int index = Array.IndexOf(values, _selectedStructuralOrientation);
            _selectedStructuralOrientation = values[(index + 1 + values.Length) % values.Length];
            InvalidateStructuralPreview(); return true;
        }

        public bool CycleStructuralConnectionPoint()
        {
            SpatialConnectionPointDefinition[] values = OrderedStructuralConnectionPoints();
            if (values.Length == 0) return false;
            int index = Array.FindIndex(values, value => value.ConnectionPointId ==
                _selectedStructuralTerminalConnectionPointId);
            _selectedStructuralTerminalConnectionPointId = values[(index + 1 + values.Length) % values.Length]
                .ConnectionPointId;
            InvalidateStructuralPreview(); return true;
        }

        public void AdjustStructuralAnchor(int deltaX, int deltaY)
        {
            _selectedStructuralAnchor = new TileCoordinate(
                _selectedStructuralAnchor.X + deltaX, _selectedStructuralAnchor.Y + deltaY);
            _root?.InvalidateStructuralConstructionPreview(); _structuralFeedback = string.Empty;
        }

        public void AdjustRenovationAnchor(int deltaX, int deltaY)
        {
            _selectedRenovationAnchor = new TileCoordinate(
                _selectedRenovationAnchor.X + deltaX, _selectedRenovationAnchor.Y + deltaY);
            _root?.InvalidateStructuralRenovationPreview(); _structuralFeedback = string.Empty;
        }

        public bool CycleRenovationTarget()
        {
            string[] ids = ResolveRenovationRoomIds();
            if (ids.Length == 0) return false;
            int index = Array.IndexOf(ids, _selectedRenovationRoomInstanceId);
            _selectedRenovationRoomInstanceId = ids[(index + 1 + ids.Length) % ids.Length];
            RoomSpatialInstance room = ResolveRenovationRoom();
            if (room != null) _selectedRenovationAnchor = room.Anchor;
            _root?.InvalidateStructuralRenovationPreview(); _structuralFeedback = string.Empty; return true;
        }

        public StructuralEditPreview PreviewStructuralMovement()
        {
            StructuralEditPreview preview = _root?.PreviewStructuralMovement(new StructuralMovementRequest
            { RoomInstanceId = _selectedRenovationRoomInstanceId, Anchor = _selectedRenovationAnchor });
            _structuralFeedback = BuildRenovationPreviewPresentation(preview); return preview;
        }

        public StructuralEditPreview PreviewStructuralReplacement()
        {
            StructuralEditPreview preview = _root?.PreviewStructuralReplacement(new StructuralReplacementRequest
            { RoomInstanceId = _selectedRenovationRoomInstanceId,
              RoomDefinitionId = _selectedStructuralRoomDefinitionId });
            _structuralFeedback = BuildRenovationPreviewPresentation(preview); return preview;
        }

        public DetachedCanonicalWriteResult CommitStructuralRenovation()
        {
            if (_root?.StructuralRenovationPreview == null || !_root.StructuralRenovationPreview.IsValid)
            { _structuralFeedback = LocalizeStructuralReason(_root?.StructuralConstructionReasonKey); return null; }
            DetachedCanonicalWriteResult result = _root.CommitStructuralRenovation();
            _structuralFeedback = result.IsSuccess ? GetLocalizedString("ui.structural.renovation.commit.success") :
                LocalizeStructuralReason(_root.StructuralConstructionReasonKey);
            RefreshOverlayText(); return result;
        }

        public string BuildRenovationPreviewPresentation(StructuralEditPreview preview)
        {
            if (preview == null || !preview.IsValid) return LocalizeStructuralReason(
                preview?.ReasonCodes?.FirstOrDefault());
            Dictionary<string, int> roomNumbers = ResolveRequiredRouteRoomNumbers();
            int targetNumber = roomNumbers.TryGetValue(preview.TargetRoomInstanceId, out int number) ? number : 0;
            var lines = new List<string>();
            if (preview.Operation == StructuralEditOperation.Movement)
                lines.Add(string.Format(CultureInfo.InvariantCulture,
                    GetLocalizedString("ui.structural.renovation.move.detail.format"), targetNumber,
                    preview.PreviousAnchor.X, preview.PreviousAnchor.Y, preview.Anchor.X, preview.Anchor.Y));
            else
                lines.Add(string.Format(CultureInfo.InvariantCulture,
                    GetLocalizedString("ui.structural.renovation.replace.detail.format"), targetNumber,
                    ResolveStructuralRoomDisplayName(ResolveRoomDefinition(preview.PreviousRoomDefinitionId)),
                    ResolveStructuralRoomDisplayName(ResolveRoomDefinition(preview.RoomDefinitionId))));
            int[] downstream = (preview.Consequences ?? Array.Empty<StructuralChange>())
                .Where(value => value.Kind == StructuralChangeKind.RoomMoved &&
                    value.StableId != preview.TargetRoomInstanceId && roomNumbers.ContainsKey(value.StableId))
                .Select(value => roomNumbers[value.StableId]).OrderBy(value => value).ToArray();
            if (downstream.Length != 0) lines.Add(string.Format(CultureInfo.InvariantCulture,
                GetLocalizedString("ui.structural.renovation.downstream.format"),
                string.Join(", ", downstream.Select(value => value.ToString(CultureInfo.InvariantCulture)))));
            if (preview.Consequences.Any(value => value.Kind == StructuralChangeKind.FixedStructureMoved))
                lines.Add(GetLocalizedString("ui.structural.renovation.terminal_moved"));
            FloorRouteNode targetNode = preview.DetachedCandidate?.Floors?.SingleOrDefault()?.Layout?.Nodes?
                .SingleOrDefault(value => value?.RoomInstanceId == preview.TargetRoomInstanceId);
            FloorRouteEdge[] candidateEdges = preview.DetachedCandidate?.Floors?.SingleOrDefault()?.Layout?.Edges ??
                Array.Empty<FloorRouteEdge>();
            StructuralChange[] connections = (preview.Consequences ?? Array.Empty<StructuralChange>())
                .Where(value => value.Kind == StructuralChangeKind.EdgeReconnected)
                .OrderBy(value => value.StableId, StringComparer.Ordinal).ToArray();
            for (int index = 0; index < connections.Length; index++)
            {
                StructuralChange change = connections[index];
                FloorRouteEdge edge = candidateEdges.SingleOrDefault(value => value?.EdgeId == change.StableId);
                string relationKey = edge?.DestinationNodeId == targetNode?.NodeId
                    ? "ui.structural.renovation.connection.incoming"
                    : edge?.SourceNodeId == targetNode?.NodeId
                        ? "ui.structural.renovation.connection.outgoing"
                        : "ui.structural.renovation.connection.affected";
                lines.Add(string.Format(CultureInfo.InvariantCulture,
                    GetLocalizedString("ui.structural.renovation.connection.format"),
                    GetLocalizedString(relationKey), ConnectionKindDisplay(change.PreviousConnectionKind),
                    ConnectionKindDisplay(change.ProposedConnectionKind)));
                TileCoordinate[] corridorTiles = change.ProposedConnectionKind ==
                    FloorRouteConnectionKind.PhysicalCorridor ? change.ProposedFootprint : change.PreviousFootprint;
                if (corridorTiles.Length != 0) lines.Add(string.Format(CultureInfo.InvariantCulture,
                    GetLocalizedString("ui.structural.renovation.corridor.format"),
                    ResolveCorridorDisplayName(edge?.CorridorDefinitionId), corridorTiles.Length,
                    string.Join(" ", corridorTiles.OrderBy(value => value).Select(value =>
                        string.Format(CultureInfo.InvariantCulture, "({0},{1})", value.X, value.Y)))));
            }
            lines.Add(string.Format(CultureInfo.InvariantCulture,
                GetLocalizedString("ui.structural.renovation.contents.format"),
                preview.PreservedAssignmentIds.Length));
            lines.Add(string.Format(CultureInfo.InvariantCulture,
                GetLocalizedString("ui.structural.renovation.floor_space.format"),
                preview.PreviousUsedFloorSpace, preview.ResultingUsedFloorSpace,
                preview.ResultingRemainingFloorSpace));
            return string.Join("\n", lines);
        }

        private string ConnectionKindDisplay(FloorRouteConnectionKind kind) => GetLocalizedString(
            kind == FloorRouteConnectionKind.PhysicalCorridor
                ? "ui.structural.connection.corridor" : "ui.structural.connection.direct");

        private RoomSpatialDefinition ResolveRoomDefinition(string definitionId) =>
            (_root?.ProductionSpatialContent?.Catalog?.Rooms ?? Array.Empty<RoomSpatialDefinition>())
                .SingleOrDefault(value => value?.RoomDefinitionId == definitionId);

        private string ResolveCorridorDisplayName(string definitionId)
        {
            CorridorSpatialDefinition[] corridors = (_root?.ProductionSpatialContent?.Catalog?.Corridors ??
                Array.Empty<CorridorSpatialDefinition>()).Where(value => value != null).ToArray();
            CorridorSpatialDefinition corridor = string.IsNullOrEmpty(definitionId) && corridors.Length == 1
                ? corridors[0] : corridors.SingleOrDefault(value => value.CorridorDefinitionId == definitionId);
            string key = corridor?.LocalizationKey ?? string.Empty;
            if (string.IsNullOrEmpty(key)) return key;
            StringTable table = (_root?.ProductionSpatialContent?.Languages ?? Array.Empty<StringTable>())
                .SingleOrDefault(value => value != null && value.language == _root?.Content?.Strings?.language);
            StringEntry[] entries = (table?.entries ?? Array.Empty<StringEntry>()).Where(value =>
                value?.key == key).ToArray();
            return entries.Length == 1 ? entries[0].text : key;
        }

        private Dictionary<string, int> ResolveRequiredRouteRoomNumbers()
        {
            string[] ids = ResolveRenovationRoomIds();
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < ids.Length; index++) result.Add(ids[index], index + 1);
            return result;
        }

        public StructuralEditPreview PreviewStructuralConstruction()
        {
            StructuralEditPreview preview = _root?.PreviewStructuralConstruction(new StructuralConstructionRequest
            {
                RoomDefinitionId = _selectedStructuralRoomDefinitionId,
                Anchor = _selectedStructuralAnchor,
                Orientation = _selectedStructuralOrientation,
                TerminalConnectionPointId = _selectedStructuralTerminalConnectionPointId
            });
            _structuralFeedback = BuildStructuralPreviewPresentation(preview);
            return preview;
        }

        public DetachedCanonicalWriteResult CommitStructuralConstruction()
        {
            if (_root?.StructuralConstructionPreview == null || !_root.StructuralConstructionPreview.IsValid)
            { _structuralFeedback = LocalizeStructuralReason(_root?.StructuralConstructionReasonKey); return null; }
            DetachedCanonicalWriteResult result = _root.CommitStructuralConstruction();
            _structuralFeedback = result.IsSuccess
                ? GetLocalizedString("ui.structural.commit.success")
                : LocalizeStructuralReason(_root.StructuralConstructionReasonKey);
            RefreshOverlayText(); return result;
        }

        public string BuildStructuralPreviewPresentation(StructuralEditPreview preview)
        {
            RoomSpatialDefinition room = ResolveSelectedStructuralRoom();
            string roomName = ResolveStructuralRoomDisplayName(room);
            string request = string.Format(CultureInfo.InvariantCulture,
                GetLocalizedString("ui.structural.request.format"), roomName,
                _selectedStructuralAnchor.X, _selectedStructuralAnchor.Y,
                StructuralOrientationDisplay(), BuildStructuralConnectionPointDisplay());
            if (preview == null || !preview.IsValid)
                return string.Format(CultureInfo.InvariantCulture,
                    GetLocalizedString("ui.structural.preview.invalid.format"), request,
                    LocalizeStructuralReason(preview?.ReasonCodes?.FirstOrDefault()));
            string connection = GetLocalizedString(preview.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor
                ? "ui.structural.connection.corridor" : "ui.structural.connection.direct");
            string summary = string.Format(CultureInfo.InvariantCulture,
                GetLocalizedString("ui.structural.preview.valid.format"), request,
                preview.OccupiedTiles.Length, connection, preview.ResultingUsedFloorSpace,
                preview.ResultingRemainingFloorSpace);
            StructuralChange[] consequences = preview.Consequences ?? Array.Empty<StructuralChange>();
            if (consequences.Any(value => value.Kind == StructuralChangeKind.RoomAdded))
                summary += "\n" + GetLocalizedString("ui.structural.consequence.room_added");
            if (consequences.Any(value => value.Kind == StructuralChangeKind.FixedStructureMoved))
                summary += "\n" + GetLocalizedString("ui.structural.consequence.terminal_moved");
            if (consequences.Any(value => value.Kind == StructuralChangeKind.EdgeAdded ||
                    value.Kind == StructuralChangeKind.EdgeRemoved))
                summary += "\n" + GetLocalizedString("ui.structural.consequence.route_changed");
            if (preview.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor)
                summary += "\n" + string.Format(CultureInfo.InvariantCulture,
                    GetLocalizedString("ui.structural.corridor_tiles.format"),
                    preview.IncomingConnectionTiles.Length,
                    string.Join(" ", preview.IncomingConnectionTiles.OrderBy(value => value)
                        .Select(value => string.Format(CultureInfo.InvariantCulture,
                            "({0},{1})", value.X, value.Y))));
            return summary;
        }

        private string LocalizeStructuralReason(string reason) => GetLocalizedString(
            string.IsNullOrEmpty(reason) ? StructuralEditService.InvalidContextReason : reason,
            GetLocalizedString(StructuralEditService.InvalidContextReason));

        private void InvalidateStructuralPreview()
        { _root?.InvalidateStructuralConstructionPreview(); _root?.InvalidateStructuralRenovationPreview();
          _structuralFeedback = string.Empty; }

        private void ReconcileRenovationSelection()
        {
            string[] ids = ResolveRenovationRoomIds();
            if (ids.Length == 0) { _selectedRenovationRoomInstanceId = null; return; }
            if (!ids.Contains(_selectedRenovationRoomInstanceId)) _selectedRenovationRoomInstanceId = ids[0];
            RoomSpatialInstance room = ResolveRenovationRoom();
            if (room != null) _selectedRenovationAnchor = room.Anchor;
        }

        private RoomSpatialInstance ResolveRenovationRoom() =>
            _root?.Save?.validatedCanonicalSpatialState?.Floors?.SingleOrDefault()?.Layout?.Rooms?
                .SingleOrDefault(value => value?.RoomInstanceId == _selectedRenovationRoomInstanceId);

        private string[] ResolveRenovationRoomIds()
        {
            SavedSpatialFloor floor = _root?.Save?.validatedCanonicalSpatialState?.Floors?.SingleOrDefault();
            if (floor == null) return Array.Empty<string>();
            var semantics = new HashSet<string>((floor.RoomContents.RoomSemantics ?? Array.Empty<CanonicalRoomSemantics>())
                .Where(value => value != null && value.LegacyRoomOriginKind !=
                    LegacyRoomOriginKind.ImplicitCompatibilityContainer)
                .Select(value => value.RoomInstanceId), StringComparer.Ordinal);
            FloorRouteNode entrance = (floor.Layout.Nodes ?? Array.Empty<FloorRouteNode>())
                .SingleOrDefault(value => value?.Kind == FloorRouteNodeKind.Entrance);
            var result = new List<string>(); var visited = new HashSet<string>(StringComparer.Ordinal);
            FloorRouteNode current = entrance;
            while (current != null && visited.Add(current.NodeId))
            {
                FloorRouteEdge[] outgoing = (floor.Layout.Edges ?? Array.Empty<FloorRouteEdge>()).Where(value =>
                    value?.Classification == RouteClassification.Required && value.SourceNodeId == current.NodeId).ToArray();
                if (outgoing.Length != 1) break;
                current = floor.Layout.Nodes.SingleOrDefault(value => value?.NodeId == outgoing[0].DestinationNodeId);
                if (current?.Kind == FloorRouteNodeKind.Room && semantics.Contains(current.RoomInstanceId))
                    result.Add(current.RoomInstanceId);
                if (current?.Kind == FloorRouteNodeKind.Completion) break;
            }
            return result.ToArray();
        }

        private void ReconcileStructuralSelection(bool reset)
        {
            RoomSpatialDefinition[] rooms = ResolveCanonicalStructuralRooms();
            if (rooms.Length == 0) return;
            if (reset || !rooms.Any(value => value.RoomDefinitionId == _selectedStructuralRoomDefinitionId))
                _selectedStructuralRoomDefinitionId = rooms[0].RoomDefinitionId;
            RoomSpatialDefinition room = ResolveSelectedStructuralRoom();
            CardinalOrientation[] orientations = (room.AllowedOrientations ?? Array.Empty<CardinalOrientation>())
                .Distinct().OrderBy(value => value).ToArray();
            _selectedStructuralOrientation = orientations.FirstOrDefault();
            _selectedStructuralTerminalConnectionPointId = OrderedStructuralConnectionPoints()
                .Select(value => value.ConnectionPointId).FirstOrDefault();
        }

        private RoomSpatialDefinition[] ResolveCanonicalStructuralRooms()
        {
            if (_root?.Save?.validatedCanonicalSpatialState?.Floors == null ||
                _root.ProductionSpatialContent?.Catalog == null ||
                CanonicalMvpRouteProjection.InspectWithProductionContent(_root.Save,
                    _root.ProductionSpatialContent).AuthorityState !=
                    CanonicalMvpRuntimeAuthorityState.ValidatedCanonical) return Array.Empty<RoomSpatialDefinition>();
            SpatialContentCatalog catalog = _root.ProductionSpatialContent.Catalog;
            SavedSpatialFloor[] activeFloors = _root.Save.validatedCanonicalSpatialState.Floors;
            if (activeFloors.Length != 1 || activeFloors[0] == null) return Array.Empty<RoomSpatialDefinition>();
            SavedSpatialFloor active = activeFloors[0];
            FloorSpatialConfiguration[] floors = (catalog.Floors ?? Array.Empty<FloorSpatialConfiguration>())
                .Where(value => value != null && value.FloorDefinitionId == active.FloorDefinitionId &&
                    value.FloorIndex == active.FloorIndex).ToArray();
            if (floors.Length != 1) return Array.Empty<RoomSpatialDefinition>();
            string[] allowed = (floors[0].AllowedRoomDefinitionIds ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrEmpty(value)).Distinct(StringComparer.Ordinal).ToArray();
            return (catalog.Rooms ?? Array.Empty<RoomSpatialDefinition>())
                .Where(value => value != null && allowed.Contains(value.RoomDefinitionId) &&
                    catalog.Rooms.Count(candidate => candidate != null &&
                        candidate.RoomDefinitionId == value.RoomDefinitionId) == 1)
                .OrderBy(value => value.RoomDefinitionId, StringComparer.Ordinal).ToArray();
        }

        private RoomSpatialDefinition ResolveSelectedStructuralRoom() =>
            ResolveCanonicalStructuralRooms().SingleOrDefault(value =>
                value.RoomDefinitionId == _selectedStructuralRoomDefinitionId);

        private SpatialConnectionPointDefinition[] OrderedStructuralConnectionPoints() =>
            (ResolveSelectedStructuralRoom()?.ConnectionPoints ?? Array.Empty<SpatialConnectionPointDefinition>())
                .Where(value => value != null).OrderBy(value => value.ConnectionPointId,
                    StringComparer.Ordinal).ToArray();

        private string StructuralOrientationDisplay() => GetLocalizedString(
            "ui.structural.orientation." + ((int)_selectedStructuralOrientation).ToString(
                CultureInfo.InvariantCulture));

        private string BuildStructuralConnectionPointDisplay()
        {
            SpatialConnectionPointDefinition[] points = OrderedStructuralConnectionPoints();
            int index = Array.FindIndex(points, value => value.ConnectionPointId ==
                _selectedStructuralTerminalConnectionPointId);
            if (index < 0) return string.Empty;
            CardinalOrientation worldFacing = StructuralEditService.Rotate(
                points[index].Facing, _selectedStructuralOrientation);
            return string.Format(CultureInfo.InvariantCulture,
                GetLocalizedString("ui.structural.exit.ordinal_direction.format"), index + 1,
                GetLocalizedString("ui.structural.direction." + ((int)worldFacing).ToString(
                    CultureInfo.InvariantCulture)));
        }

        private string ResolveStructuralRoomDisplayName(RoomSpatialDefinition room)
        {
            string key = room?.LocalizationKey ?? string.Empty;
            if (string.IsNullOrEmpty(key)) return key;
            string language = _root?.Content?.Strings?.language;
            StringTable[] matches = (_root?.ProductionSpatialContent?.Languages ??
                Array.Empty<StringTable>()).Where(value => value != null &&
                    string.Equals(value.language, language, StringComparison.Ordinal)).ToArray();
            if (matches.Length != 1) return key;
            StringEntry[] entries = (matches[0].entries ?? Array.Empty<StringEntry>())
                .Where(value => value != null && string.Equals(value.key, key,
                    StringComparison.Ordinal)).ToArray();
            return entries.Length == 1 && !string.IsNullOrEmpty(entries[0].text)
                ? entries[0].text : key;
        }

        public void CycleFullDiagnosticsPage()
        {
            _fullDiagnosticsPage = (_fullDiagnosticsPage + 1) % DiagnosticsPageCount;
            _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] = 0;
        }

        public void ToggleDevPanel()
        {
            if (!DiagnosticsAllowed)
            {
                _devPanelVisible = false;
                return;
            }

            _devPanelVisible = !_devPanelVisible;
        }

        public void ToggleRunDiagnosticsFocus()
        {
            if (!DiagnosticsAllowed)
            {
                _runDiagnosticsOnlyVisible = false;
                return;
            }

            _runDiagnosticsOnlyVisible = !_runDiagnosticsOnlyVisible;
            _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] = 0;
        }

        public void ToggleDiagnosticsVisibility()
        {
            if (!DiagnosticsAllowed)
            {
                _diagnosticsVisible = false;
                return;
            }

            _diagnosticsVisible = !_diagnosticsVisible;
            if (_diagnosticsVisible)
            {
                _fullDiagnosticsPage = RuntimeSummaryPage;
            }
            _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] = 0;
        }

        public bool SelectMvpStructure(string structureId)
        {
            if (!IsAllowedMvpStructure(structureId))
            {
                return false;
            }

            _selectedMvpStructureId = structureId;
            return true;
        }

        public bool SelectMvpPlacementCategory(string categoryId)
        {
            if (!MvpDungeonPlacementIds.IsAllowedCategory(categoryId))
            {
                return false;
            }

            _selectedMvpPlacementCategoryId = categoryId;
            _selectedMvpPlacementOptionId = MvpDungeonPlacementIds.GetStarterOptionForCategory(categoryId);
            return !string.IsNullOrWhiteSpace(_selectedMvpPlacementOptionId);
        }

        public bool SelectMvpPlacementOption(string optionId)
        {
            if (!MvpDungeonPlacementIds.IsAllowedOption(optionId) ||
                !MvpDungeonPlacementIds.TryGetCategoryForOption(optionId, out string categoryId) ||
                !string.Equals(categoryId, _selectedMvpPlacementCategoryId, System.StringComparison.Ordinal))
            {
                return false;
            }

            _selectedMvpPlacementOptionId = optionId;
            return true;
        }

        public string GetSelectedMvpStructureNameKey()
        {
            return GetMvpSelectionNameKey(_selectedMvpStructureId);
        }

        public string GetSelectedMvpStructureDisplayName()
        {
            return MvpPlayerFacingLabelResolver.ResolveStructureDisplayName(_selectedMvpStructureId, (key, fallback) => GetLocalizedString(key, fallback));
        }

        public string GetSelectedMvpStructurePreviewText()
        {
            return MvpStructureImpactPreviewPresenter.BuildPreviewText(_selectedMvpStructureId, (key, fallback) => GetLocalizedString(key, fallback));
        }

        public string GetSelectedMvpPlacementPreviewText()
        {
            return MvpDungeonPlacementPresenter.BuildPreviewText(_selectedMvpPlacementOptionId, (key, fallback) => GetLocalizedString(key, fallback));
        }

        public string GetSelectedMvpRunPlanPreviewText()
        {
            return MvpStructureImpactPreviewPresenter.BuildRunPlanPreviewText(_selectedMvpStructureId, GetSelectedMvpRunPostureNameKey(), (key, fallback) => GetLocalizedString(key, fallback));
        }

        public string GetSelectedMvpRoomCapacityText()
        {
            if (_root == null)
            {
                return string.Empty;
            }

            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(
                _root.Save, _root.RunSimulationConfig, _root.ProductionSpatialContent);
            int selectedRoomIndex = MvpRoomSlotTargetResolver.ResolveClampedSelectedRoomIndex(_root.Save, layout);
            return MvpRoomSlotTargetPresenter.BuildSelectedCapacityText(layout, selectedRoomIndex, (key, fallback) => GetLocalizedString(key, fallback));
        }

        public string GetSelectedMvpPlacementFitText()
        {
            if (_root == null)
            {
                return string.Empty;
            }

            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(
                _root.Save, _root.RunSimulationConfig, _root.ProductionSpatialContent);
            int selectedRoomIndex = MvpRoomSlotTargetResolver.ResolveClampedSelectedRoomIndex(_root.Save, layout);
            return MvpRoomSlotTargetPresenter.BuildSelectedPlacementFitText(layout, selectedRoomIndex, _selectedMvpPlacementCategoryId, (key, fallback) => GetLocalizedString(key, fallback));
        }

        public bool SelectMvpRunPosture(string postureId)
        {
            if (!BootstrapMvpActionHandler.IsAllowedMvpRunPosture(postureId))
            {
                return false;
            }

            _selectedMvpRunPostureId = postureId;
            return true;
        }

        public string GetSelectedMvpRunPostureNameKey()
        {
            return GetMvpRunPostureNameKey(_selectedMvpRunPostureId);
        }

        public string GetSelectedMvpRunPostureDisplayName()
        {
            return GetLocalizedString(GetSelectedMvpRunPostureNameKey(), GetSelectedMvpRunPostureNameKey());
        }

        public void PlaceSelectedMvpStructure()
        {
            if (!NormalGameplayActionsAvailable) return;
            BootstrapMvpActionHandler.PlacementResult result = CreateMvpActionHandler().PlaceOrModifySelectedMvpPlacement(
                _selectedMvpPlacementCategoryId,
                _selectedMvpPlacementOptionId);
            _mvpStructurePlacementFeedback = result.PlacementFeedback;
            _roomSlotPlacementFailureIsLatestAction = !result.Succeeded && IsRoomSlotPlacementFailureFeedback();
            RefreshOverlayText();
        }

        public void AddMvpBasicRoomSlot()
        {
            if (!NormalGameplayActionsAvailable) return;
            bool added = _root != null && _root.TryAddSecondMvpBasicRoomSlot();
            _mvpStructurePlacementFeedback = GetLocalizedString(added ? AddBasicRoomSlotSuccessKey : AddBasicRoomSlotAlreadyExistsKey);
            _roomSlotPlacementFailureIsLatestAction = false;
            _root?.SetBanner(_mvpStructurePlacementFeedback);
            RefreshOverlayText();
        }

        public void CycleSelectedMvpRoomSlotTarget()
        {
            if (!NormalGameplayActionsAvailable) return;
            _root?.CycleSelectedMvpRoomSlotTarget();
            ClearRoomSlotPlacementFailureFeedback();
            RefreshOverlayText();
        }

        public void RunOrObserveDungeon()
        {
            if (!NormalGameplayActionsAvailable) return;
            BootstrapMvpActionHandler.RunResult result = CreateMvpActionHandler().RunOrObserveDungeon(_selectedMvpRunPostureId);
            ApplyRunResultFeedback(result);
            RefreshOverlayText();
        }

        public bool SimulateRunOnceFromDevPanel()
        {
            if (!DiagnosticsAllowed || _root == null)
            {
                return false;
            }

            bool didRun = _root.SimulateRunOnce();
            _mvpRunResultFeedback = string.Empty;
            _lastRunIntentSummary = null;
            _lastRunPostureUsedId = string.Empty;
            _lastRunDebugPostureId = string.Empty;
            _lastRunIntentFallbackUsed = false;
            string bannerKey = didRun ? "ui.banner.run_simulated" : "ui.banner.run_sim_failed";
            _root.SetBanner(_root.Content != null ? _root.Content.GetString(bannerKey, bannerKey) : bannerKey);
            RefreshOverlayText();
            return didRun;
        }

        private void ApplyRunResultFeedback(BootstrapMvpActionHandler.RunResult result)
        {
            _mvpRunResultFeedback = result.RunFeedback;
            _lastRunIntentSummary = result.IntentSummary;
            _lastRunPostureUsedId = result.PostureUsedId;
            _lastRunDebugPostureId = result.DebugPostureId;
            _lastRunIntentFallbackUsed = result.IntentFallbackUsed;
        }

        public bool ResetCleanMvpValidationSessionFromDevPanel()
        {
            if (!DiagnosticsAllowed || _root == null || !_root.ResetCleanMvpValidationSession())
            {
                return false;
            }

            _selectedMvpStructureId = DefaultMvpStructureId;
            _selectedMvpPlacementCategoryId = DefaultMvpPlacementCategoryId;
            _selectedMvpPlacementOptionId = DefaultMvpPlacementOptionId;
            _selectedMvpRunPostureId = RunPostureResolver.BalancedId;
            _mvpStructurePlacementFeedback = string.Empty;
            _roomSlotPlacementFailureIsLatestAction = false;
            _mvpRunResultFeedback = string.Empty;
            _lastRunIntentSummary = null;
            _lastRunPostureUsedId = string.Empty;
            _lastRunDebugPostureId = string.Empty;
            _lastRunIntentFallbackUsed = false;
            _playerFacingScrollOffset = 0;
            _compactSmokeViewEnabled = false;
            _playerFacingSectionIndex = PlayerFacingSectionFull;
            _minimalMvpActionPanelCollapsed = false;
            _smokeViewportStatusMessage = string.Empty;
            _root.SetBanner(GetLocalizedString("ui.banner.clean_mvp_validation_reset", "ui.banner.clean_mvp_validation_reset"));
            RefreshOverlayText();
            return true;
        }

        public void ScrollFullDiagnosticsLines(int lineDelta)
        {
            if (_runDiagnosticsOnlyVisible || !_diagnosticsVisible || lineDelta == 0)
            {
                return;
            }

            string[] bodyLines = BuildCurrentFullDiagnosticsBody().ToString().Split('\n');
            int maxOffset = Mathf.Max(0, bodyLines.Length - VisibleDiagnosticsBodyLineCount);
            _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] = Mathf.Clamp(
                _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] + lineDelta,
                0,
                maxOffset);
        }

        public void ScrollPlayerFacingTextLines(int lineDelta)
        {
            if (_runDiagnosticsOnlyVisible || _diagnosticsVisible || lineDelta == 0)
            {
                return;
            }

            ClampPlayerFacingScrollOffset(_playerFacingScrollOffset + lineDelta);
        }

        public void JumpPlayerFacingTextToTop()
        {
            if (_runDiagnosticsOnlyVisible || _diagnosticsVisible)
            {
                return;
            }

            _playerFacingScrollOffset = 0;
        }

        public void JumpPlayerFacingTextToBottom()
        {
            if (_runDiagnosticsOnlyVisible || _diagnosticsVisible)
            {
                return;
            }

            ClampPlayerFacingScrollOffset(int.MaxValue);
        }

        public void ToggleCompactSmokeView()
        {
            if (!DiagnosticsAllowed)
            {
                _compactSmokeViewEnabled = false;
                return;
            }

            _compactSmokeViewEnabled = !_compactSmokeViewEnabled;
            _playerFacingScrollOffset = 0;
        }

        public void CyclePlayerFacingSmokeSection()
        {
            if (!DiagnosticsAllowed)
            {
                _playerFacingSectionIndex = PlayerFacingSectionFull;
                return;
            }

            _playerFacingSectionIndex = (_playerFacingSectionIndex + 1) % PlayerFacingSectionCount;
            _playerFacingScrollOffset = 0;
        }

        public void ToggleMinimalMvpActionPanelCollapsed()
        {
            _minimalMvpActionPanelCollapsed = !_minimalMvpActionPanelCollapsed;
            _playerFacingScrollOffset = 0;
        }

        public string CopyFullSmokeTextToClipboard()
        {
            if (!DiagnosticsAllowed)
            {
                return string.Empty;
            }

            string smokeText = BuildFullPlayerFacingSmokeText();
            GUIUtility.systemCopyBuffer = smokeText;
            _smokeViewportStatusMessage = GetLocalizedString("ui.mvp_smoke.copy.confirmation");
            RefreshOverlayText();
            return smokeText;
        }

        public void RefreshOverlayText()
        {
            if (_root == null || overlayText == null)
            {
                return;
            }

            ApplyOverlayTextSafeArea();
            overlayText.text = BuildOverlayText();
        }

        public void ApplyOverlayTextSafeArea()
        {
            if (overlayText == null || overlayText.rectTransform == null)
            {
                return;
            }

            RectTransform rectTransform = overlayText.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0f, 1f);
            Rect safeArea = CalculateOverlayTextSafeArea(Screen.width, Screen.height,
                _minimalMvpActionPanelCollapsed);
            rectTransform.offsetMin = new Vector2(safeArea.xMin, OverlayTextSafeBottomMargin);
            float rightReserve = Mathf.Max(0f, Screen.width - safeArea.xMax);
            rectTransform.offsetMax = new Vector2(-rightReserve, -OverlayTextSafeTopMargin);
            overlayText.alignment = TextAlignmentOptions.TopLeft;
        }

        private void Update()
        {
            if (_root == null)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                ToggleDevPanel();
            }
            if (DiagnosticsAllowed && Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            {
                ToggleRunDiagnosticsFocus();
            }
            if (DiagnosticsAllowed && Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                CycleFullDiagnosticsPage();
            }
            if (DiagnosticsAllowed && Keyboard.current != null && Keyboard.current.f4Key.wasPressedThisFrame)
            {
                ToggleCompactSmokeView();
            }
            if (DiagnosticsAllowed && Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
            {
                CyclePlayerFacingSmokeSection();
            }
            if (DiagnosticsAllowed && Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame)
            {
                CopyFullSmokeTextToClipboard();
            }
            if (Keyboard.current != null && Keyboard.current.f7Key.wasPressedThisFrame)
            {
                ToggleMinimalMvpActionPanelCollapsed();
            }
            if (Keyboard.current != null && Keyboard.current.pageUpKey.wasPressedThisFrame)
            {
                ScrollPlayerFacingTextLines(-VisiblePlayerFacingLineCount);
                ScrollFullDiagnosticsLines(-VisibleDiagnosticsBodyLineCount);
            }
            if (Keyboard.current != null && Keyboard.current.pageDownKey.wasPressedThisFrame)
            {
                ScrollPlayerFacingTextLines(VisiblePlayerFacingLineCount);
                ScrollFullDiagnosticsLines(VisibleDiagnosticsBodyLineCount);
            }
            if (Keyboard.current != null && Keyboard.current.homeKey.wasPressedThisFrame)
            {
                JumpPlayerFacingTextToTop();
            }
            if (Keyboard.current != null && Keyboard.current.endKey.wasPressedThisFrame)
            {
                JumpPlayerFacingTextToBottom();
            }
            if (Mouse.current != null)
            {
                float scrollY = Mouse.current.scroll.ReadValue().y;
                if (scrollY > 0f)
                {
                    ScrollFullDiagnosticsLines(-1);
                }
                else if (scrollY < 0f)
                {
                    ScrollFullDiagnosticsLines(1);
                }
            }

            RefreshOverlayText();
        }

        private string BuildOverlayText()
        {
            var builder = new StringBuilder();
            if (!_runDiagnosticsOnlyVisible)
            {
                if (_diagnosticsVisible)
                {
                    AppendLine(builder, BootstrapSmokeTextComposer.BuildMvpLoopSummaryPanelText(
                        BuildSmokeTextContext(),
                        (key, fallback) => GetLocalizedString(key, fallback)));
                }
                else
                {
                    AppendScrolledPlayerFacingSmokeText(builder, BuildCurrentPlayerFacingSmokeText());
                }
                AppendLine(builder, string.Empty);
            }
            if (_runDiagnosticsOnlyVisible)
            {
                AppendHeader(builder);
                AppendRunDiagnostics(builder, includeBreakdownAndFeedback: false, includeHeatDiagnostics: true);
                return builder.ToString();
            }

            if (!_diagnosticsVisible)
            {
                return builder.ToString();
            }

            AppendHeader(builder);
            AppendScrolledFullDiagnosticsBody(builder, BuildCurrentFullDiagnosticsBody());
            return builder.ToString();
        }

        private BootstrapSmokeTextComposer.Context BuildSmokeTextContext()
        {
            ClearStaleRoomSlotPlacementFailureFeedback();
            MvpPlayerLoopSummary summary = _root.ResolveMvpPlayerLoopSummary();
            GuidedMvpActionPathSummary guidedPath = _root.ResolveGuidedMvpActionPath(summary);
            MvpFirstSessionObjectiveSummary firstSessionObjective = MvpFirstSessionObjectivePresenter.Resolve(
                _root.Save, _root.RunSimulationConfig, _root.ProductionSpatialContent);
            MvpPostContractGreedTrialSummary greedTrial = MvpPostContractGreedTrialPresenter.Resolve(_root.Save, _root.RunSimulationConfig, firstSessionObjective);
            return new BootstrapSmokeTextComposer.Context(
                summary,
                guidedPath,
                firstSessionObjective,
                greedTrial,
                MvpRecentSpoilsLedgerPresenter.Resolve(_root.Save, greedTrial),
                MvpDungeonLayoutPresenter.BuildLayoutText(_root.Save, _root.RunSimulationConfig,
                    _root.ProductionSpatialContent, _selectedMvpPlacementCategoryId,
                    (key, fallback) => GetLocalizedString(key, fallback)),
                MvpDungeonPlacementPresenter.ResolveCategoryName(_selectedMvpPlacementCategoryId, (key, fallback) => GetLocalizedString(key, fallback)),
                _selectedMvpPlacementCategoryId,
                MvpDungeonPlacementPresenter.ResolveOptionName(_selectedMvpPlacementOptionId, (key, fallback) => GetLocalizedString(key, fallback)),
                GetSelectedMvpPlacementPreviewText(),
                BuildSelectedMvpPlacementComparisonText(),
                GetSelectedMvpRunPostureDisplayName(),
                GetSelectedMvpRunPlanPreviewText(),
                _mvpStructurePlacementFeedback,
                _mvpRunResultFeedback,
                _root.BannerMessage,
                _lastRunIntentSummary,
                _lastRunPostureUsedId,
                _lastRunDebugPostureId,
                _lastRunIntentFallbackUsed,
                _smokeViewportStatusMessage,
                playerFacingSectionIndex: _playerFacingSectionIndex,
                playerFacingSectionCount: PlayerFacingSectionCount);
        }

        public string BuildFullPlayerFacingSmokeText()
        {
            if (!NormalGameplayActionsAvailable) return BuildBlockedBootPlayerText();
            return BootstrapSmokeTextComposer.BuildFullPlayerFacingSmokeText(BuildSmokeTextContext(), (key, fallback) => GetLocalizedString(key, fallback));
        }

        public string BuildCurrentPlayerFacingSmokeText()
        {
            if (!NormalGameplayActionsAvailable) return BuildBlockedBootPlayerText();
            if (_compactSmokeViewEnabled)
            {
                return BuildCompactSmokeText();
            }

            switch (_playerFacingSectionIndex)
            {
                case PlayerFacingSectionLoopSummary:
                    return BuildLoopSummarySectionText();
                case PlayerFacingSectionPlanAndAction:
                    return BuildPlanAndActionSectionText();
                case PlayerFacingSectionLatestRunFeedback:
                    return BuildLatestRunFeedbackSectionText();
                case PlayerFacingSectionFull:
                default:
                    return BuildPlayableMvpScreenText();
            }
        }

        public string BuildPlayableMvpScreenText()
        {
            if (!NormalGameplayActionsAvailable) return BuildBlockedBootPlayerText();
            return BootstrapSmokeTextComposer.BuildPlayableMvpScreenText(BuildSmokeTextContext(), (key, fallback) => GetLocalizedString(key, fallback));
        }

        public string BuildCompactSmokeText()
        {
            if (!NormalGameplayActionsAvailable) return BuildBlockedBootPlayerText();
            return BootstrapSmokeTextComposer.BuildCompactSmokeText(BuildSmokeTextContext(), (key, fallback) => GetLocalizedString(key, fallback));
        }

        private string BuildBlockedBootPlayerText()
        {
            if (_root == null) return string.Empty;
            if (!string.IsNullOrEmpty(_root.BannerMessage)) return _root.BannerMessage;
            string reason = DetachedSpatialMigrationTransaction.NoTrustedPayloadReason;
            string key = Gd66MigrationReasonRegistry.PlayerLocalizationKey(reason);
            return GetLocalizedString(key, key);
        }

        private string BuildLoopSummarySectionText()
        {
            return BootstrapSmokeTextComposer.BuildLoopSummarySectionText(BuildSmokeTextContext(), (key, fallback) => GetLocalizedString(key, fallback));
        }

        private string BuildPlanAndActionSectionText()
        {
            return BootstrapSmokeTextComposer.BuildPlanAndActionSectionText(BuildSmokeTextContext(), (key, fallback) => GetLocalizedString(key, fallback));
        }

        private string BuildLatestRunFeedbackSectionText()
        {
            return BootstrapSmokeTextComposer.BuildLatestRunFeedbackSectionText(BuildSmokeTextContext(), (key, fallback) => GetLocalizedString(key, fallback));
        }

        private void AppendScrolledPlayerFacingSmokeText(StringBuilder builder, string text)
        {
            string[] bodyLines = (text ?? string.Empty).Split('\n');
            int maxOffset = Mathf.Max(0, bodyLines.Length - VisiblePlayerFacingLineCount);
            _playerFacingScrollOffset = Mathf.Clamp(_playerFacingScrollOffset, 0, maxOffset);
            int end = Mathf.Min(bodyLines.Length, _playerFacingScrollOffset + VisiblePlayerFacingLineCount);
            for (int i = _playerFacingScrollOffset; i < end; i++)
            {
                AppendLine(builder, bodyLines[i]);
            }
        }

        private void ClampPlayerFacingScrollOffset(int requestedOffset)
        {
            string[] bodyLines = BuildCurrentPlayerFacingSmokeText().Split('\n');
            int maxOffset = Mathf.Max(0, bodyLines.Length - VisiblePlayerFacingLineCount);
            _playerFacingScrollOffset = Mathf.Clamp(requestedOffset, 0, maxOffset);
        }

        private void AppendHeader(StringBuilder builder)
        {
            if (_runDiagnosticsOnlyVisible)
            {
                AppendLine(builder, GetLocalizedString("ui.dev.diagnostics.focus.run_diagnostics"));
            }
            else
            {
                string pageName = GetLocalizedString(GetPageNameKey(_fullDiagnosticsPage));
                AppendLine(builder, string.Format(
                    GetLocalizedString("ui.dev.diagnostics.header_format"),
                    pageName,
                    _fullDiagnosticsPage + 1,
                    DiagnosticsPageCount));
            }
            AppendLine(builder, GetLocalizedString("ui.dev.hint.toggle_panel"));
            AppendLine(builder, GetLocalizedString("ui.dev.hint.toggle_run_diagnostics"));
            AppendLine(builder, GetLocalizedString("ui.dev.hint.cycle_diagnostics_page"));
            AppendLine(builder, GetLocalizedString("ui.dev.hint.scroll_diagnostics"));
        }

        private StringBuilder BuildCurrentFullDiagnosticsBody()
        {
            var builder = new StringBuilder();
            switch (_fullDiagnosticsPage)
            {
                case RuntimeSummaryPage:
                    AppendRuntimeSummary(builder);
                    break;
                case RunDiagnosticsPage:
                    AppendRunDiagnostics(builder, includeBreakdownAndFeedback: true, includeHeatDiagnostics: false);
                    break;
                case HeatDiagnosticsPage:
                    AppendHeatDiagnostics(builder);
                    break;
                case SystemsDiagnosticsPage:
                    AppendSystemsDiagnostics(builder);
                    break;
                case ResearchDiagnosticsPage:
                    AppendResearchDiagnostics(builder);
                    break;
                case ResearchStatusPresentationDiagnosticsPage:
                    AppendResearchStatusPresentationDiagnostics(builder);
                    break;
                case ResearchStatusSafetyDiagnosticsPage:
                    AppendResearchStatusSafetyDiagnostics(builder);
                    break;
                case ResearchVerificationBoundaryDiagnosticsPage:
                    AppendResearchVerificationBoundaryDiagnostics(builder);
                    break;
                case ResearchVerificationSafetyDiagnosticsPage:
                    AppendResearchVerificationSafetyDiagnostics(builder);
                    break;
            }
            return builder;
        }

        private void AppendScrolledFullDiagnosticsBody(StringBuilder builder, StringBuilder bodyBuilder)
        {
            string[] bodyLines = bodyBuilder.ToString().Split('\n');
            int maxOffset = Mathf.Max(0, bodyLines.Length - VisibleDiagnosticsBodyLineCount);
            int offset = Mathf.Clamp(_fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage], 0, maxOffset);
            _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] = offset;
            int end = Mathf.Min(bodyLines.Length, offset + VisibleDiagnosticsBodyLineCount);
            for (int i = offset; i < end; i++)
            {
                AppendLine(builder, bodyLines[i]);
            }
        }

        private void AppendRuntimeSummary(StringBuilder builder)
        {
            AppendLine(builder, _root.BuildLine);
            AppendLine(builder, _root.StateLine);
            AppendLine(builder, _root.PendingStateLine);
            AppendLine(builder, _root.GateStatusLine);
            AppendLine(builder, _root.KpiLine);
            AppendLine(builder, _root.TickLine);
            AppendLine(builder, _root.ManaLine);
            AppendLine(builder, _root.SaveLine);
            AppendLine(builder, _root.PauseLine);

            if (!string.IsNullOrEmpty(_root.BannerMessage))
            {
                AppendLine(builder, GetLocalizedString("ui.dev.banner.heading") + ":");
                AppendLine(builder, _root.BannerMessage);
            }
        }

        private void AppendRunDiagnostics(StringBuilder builder, bool includeBreakdownAndFeedback, bool includeHeatDiagnostics)
        {
            AppendLine(builder, _root.RunLine);
            AppendLine(builder, _root.RunHistoryLine);
            if (includeBreakdownAndFeedback)
            {
                AppendLine(builder, _root.RunBreakdownLine);
                AppendLine(builder, _root.RunFeedbackLine);
            }
            AppendLine(builder, _root.RunLootLine);
            AppendLine(builder, _root.RunSurvivalLine);
            AppendLine(builder, _root.RunExtractionLine);
            if (includeHeatDiagnostics)
            {
                AppendLine(builder, _root.RunHeatCoolingLine);
                AppendLine(builder, _root.RunHeatDeltaLine);
                AppendLine(builder, _root.RunHeatApplicationLine);
            }
            AppendLine(builder, _root.RunAdventurerAttractionLine);
            AppendLine(builder, _root.RunAdventurerInterestForecastLine);
            AppendLine(builder, _root.RunAdventurerDemandBudgetLine);
        }

        private void AppendHeatDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.HeatLine);
            AppendLine(builder, _root.CurrentHeatTierLine);
            AppendLine(builder, _root.RunHeatCoolingLine);
            AppendLine(builder, _root.RunHeatDeltaLine);
            AppendLine(builder, _root.RunHeatApplicationLine);
        }

        private void AppendSystemsDiagnostics(StringBuilder builder)
        {
            if (_root.Content == null)
            {
                return;
            }

            AppendLine(builder, string.Format(
                GetLocalizedString("ui.dev.structure_status"),
                _root.SelectedFloorIndex,
                _root.SelectedSlotIndex,
                _root.GetSelectedSlotStructureId(),
                _root.Save != null && _root.Save.structureRuntime != null && _root.Save.structureRuntime.IsHeatCrisisActive));
            AppendLine(builder, _root.OfflineSummaryLine);
        }

        private void AppendResearchDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.ResearchPendingLine);
            AppendLine(builder, _root.ResearchPendingValidationLine);
            AppendLine(builder, _root.ResearchProgressLine);
            AppendLine(builder, _root.ResearchProgressStateLine);
            AppendLine(builder, _root.ResearchCompletionEligibilityLine);
            AppendLine(builder, _root.ResearchCompletionPendingApplyLine);
            AppendLine(builder, _root.ResearchCompletionClaimReadinessLine);
            AppendLine(builder, _root.CompletedResearchStateLine);
            AppendLine(builder, _root.ResearchCompletionClaimApplyLine);
        }

        private void AppendResearchStatusPresentationDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.ResearchStatusPresentationLine);
            AppendLine(builder, _root.PlayerResearchAuthorityLine);
        }

        private void AppendResearchStatusSafetyDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.ResearchStatusSafetyLine);
        }

        private void AppendResearchVerificationBoundaryDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.ResearchVerificationBoundaryLine);
        }

        private void AppendResearchVerificationSafetyDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.ResearchVerificationSafetyLine);
        }

        private string GetLocalizedString(string key)
        {
            return GetLocalizedString(key, key);
        }

        private string GetLocalizedString(string key, string fallback)
        {
            return _root.Content != null ? _root.Content.GetString(key, fallback) : fallback;
        }

        private static string GetPageNameKey(int page)
        {
            switch (page)
            {
                case RuntimeSummaryPage:
                    return "ui.dev.diagnostics.page.runtime_summary";
                case RunDiagnosticsPage:
                    return "ui.dev.diagnostics.page.run_diagnostics";
                case HeatDiagnosticsPage:
                    return "ui.dev.diagnostics.page.heat_diagnostics";
                case SystemsDiagnosticsPage:
                    return "ui.dev.diagnostics.page.systems_diagnostics";
                case ResearchDiagnosticsPage:
                    return "ui.dev.diagnostics.page.research_diagnostics";
                case ResearchStatusPresentationDiagnosticsPage:
                    return "ui.dev.diagnostics.page.research_status_presentation_diagnostics";
                case ResearchStatusSafetyDiagnosticsPage:
                    return "ui.dev.diagnostics.page.research_status_safety_diagnostics";
                case ResearchVerificationBoundaryDiagnosticsPage:
                    return "ui.dev.diagnostics.page.research_verification_boundary_diagnostics";
                case ResearchVerificationSafetyDiagnosticsPage:
                    return "ui.dev.diagnostics.page.research_verification_safety_diagnostics";
                default:
                    return "ui.dev.diagnostics.page.runtime_summary";
            }
        }

        private static void AppendLine(StringBuilder builder, string line)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }
            builder.Append(line ?? string.Empty);
        }

        private void OnGUI()
        {
            DrawMinimalMvpActionPanel();

            if (_root != null && _root.Save == null && _root.SaveService != null &&
                _root.SaveService.NarrowHallRepairAvailable) return;

            if (_root == null || !_root.DevPanelEnabled || !_devPanelVisible)
            {
                return;
            }

            float panelHeight = Mathf.Max(240f, Screen.height - 140f);
            GUILayout.BeginArea(new Rect(10, 120, 360, panelHeight), GUI.skin.box);
            GUILayout.Label(_root.Content.GetString("ui.dev.panel.title", "ui.dev.panel.title"));
            _devPanelScrollPosition = GUILayout.BeginScrollView(_devPanelScrollPosition);

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.save_now", "ui.dev.button.save_now")))
            {
                if (_root.Save != null)
                {
                    _root.SaveService.Save(_root.Save, SaveReason.ManualDev);
                    _root.SetBanner(_root.Content.GetString("ui.banner.saved_dev", "ui.banner.saved_dev"));
                }
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.delete_save", "ui.dev.button.delete_save")))
            {
                _root.TryDeleteSaveFromDevPanel(out _);
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.clear_banner", "ui.dev.button.clear_banner")))
            {
                _root.SetBanner(string.Empty);
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.clean_mvp_validation_reset", "ui.dev.button.clean_mvp_validation_reset")))
            {
                ResetCleanMvpValidationSessionFromDevPanel();
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.toggle_online", "ui.dev.button.toggle_online")))
            {
                _root.SetOnline(!_root.IsOnline);
                if (!_root.IsOnline)
                {
                    string msg = _root.Content != null
                        ? _root.Content.GetString("ui.banner.offline", "Offline mode.")
                        : "Offline mode.";
                    _root.SetBanner(msg);
                }
                else
                {
                    _root.SetBanner(_root.Content.GetString("ui.banner.online_restored", "ui.banner.online_restored"));
                }
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.toggle_verification", "ui.dev.button.toggle_verification")))
            {
                _root.SetVerificationPending(!_root.VerificationPending);
                if (_root.VerificationPending)
                {
                    string msg = _root.Content != null
                        ? _root.Content.GetString("gate.error.verification_pending", "Verification pending.")
                        : "Verification pending.";
                    _root.SetBanner(msg);
                }
                else
                {
                    _root.SetBanner(_root.Content.GetString("ui.banner.verification_cleared", "ui.banner.verification_cleared"));
                }
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.toggle_pause", "ui.dev.button.toggle_pause")))
            {
                bool pause = _root.PauseLine != "Pause: Paused";
                _root.ApplyPauseState(pause);
                _root.SetBanner(pause
                    ? _root.Content.GetString("ui.banner.paused_dev_panel", "ui.banner.paused_dev_panel")
                    : _root.Content.GetString("ui.banner.resumed_dev_panel", "ui.banner.resumed_dev_panel"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.set_research_pending", "ui.dev.button.set_research_pending")))
            {
                bool didSet = _root.SetResearchPendingScaffold();
                _root.SetBanner(_root.Content.GetString(
                    didSet ? "ui.banner.research_pending_set" : "ui.banner.research_pending_set_failed",
                    didSet ? "ui.banner.research_pending_set" : "ui.banner.research_pending_set_failed"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.clear_research_pending", "ui.dev.button.clear_research_pending")))
            {
                _root.ClearResearchPendingScaffold();
                _root.SetBanner(_root.Content.GetString("ui.banner.research_pending_cleared", "ui.banner.research_pending_cleared"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.claim_research_completion", "ui.dev.button.claim_research_completion")))
            {
                bool didClaim = _root.ClaimResearchCompletionScaffold();
                _root.SetBanner(_root.Content.GetString(
                    didClaim ? "ui.banner.research_completion_claimed" : "ui.banner.research_completion_claim_failed",
                    didClaim ? "ui.banner.research_completion_claimed" : "ui.banner.research_completion_claim_failed"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.sim_mana", "ui.dev.button.sim_mana")))
            {
                _root.TrackManaGenerated(10);
                _root.SetBanner(_root.Content.GetString("ui.banner.simulated_mana_kpi", "ui.banner.simulated_mana_kpi"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.sim_heat", "ui.dev.button.sim_heat")))
            {
                _root.ApplyHeatDelta(5d);
                _root.SetBanner(_root.Content.GetString("ui.banner.applied_heat_event", "ui.banner.applied_heat_event"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.select_slot", "ui.dev.button.select_slot")))
            {
                _root.SelectNextSlot();
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.place_mana_generator", "ui.dev.button.place_mana_generator")))
            {
                ShowPlacementBanner(StructureSimulationPass.ManaGeneratorBasicId);
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.place_heat_scrubber", "ui.dev.button.place_heat_scrubber")))
            {
                ShowPlacementBanner(StructureSimulationPass.HeatScrubberBasicId);
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.place_risk_lab", "ui.dev.button.place_risk_lab")))
            {
                ShowPlacementBanner(StructureSimulationPass.RiskLabBasicId);
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.sim_structure_tick", "ui.dev.button.sim_structure_tick")))
            {
                bool didRun = _root.SimulateStructureTick();
                _root.SetBanner(didRun
                    ? _root.Content.GetString("ui.banner.simulated_tick", "ui.banner.simulated_tick")
                    : _root.Content.GetString("ui.banner.structure_tick_failed", "ui.banner.structure_tick_failed"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.sim_run_once", "ui.dev.button.sim_run_once")))
            {
                SimulateRunOnceFromDevPanel();
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.run_previous", "ui.dev.button.run_previous")))
            {
                _root.SelectPreviousRunOutcome();
                _root.RefreshRunLine();
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.run_next", "ui.dev.button.run_next")))
            {
                _root.SelectNextRunOutcome();
                _root.RefreshRunLine();
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.run_latest", "ui.dev.button.run_latest")))
            {
                _root.SelectLatestRunOutcome();
                _root.RefreshRunLine();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }


        private void DrawCollapsedMinimalMvpActionPanel()
        {
            GUIStyle compactBox = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(6, 6, 4, 4)
            };
            GUIStyle compactButton = new GUIStyle(GUI.skin.button)
            {
                margin = new RectOffset(0, 0, 1, 1),
                padding = new RectOffset(4, 4, 1, 1)
            };
            GUILayout.BeginArea(GetCollapsedMinimalMvpActionPanelRect(), compactBox);
            if (GUILayout.Button(GetLocalizedString("ui.mvp_action.button.expand_panel"), compactButton, GUILayout.Height(MinimalMvpActionPanelButtonHeight)))
            {
                ToggleMinimalMvpActionPanelCollapsed();
                RefreshOverlayText();
            }
            GUILayout.EndArea();
        }

        public Rect GetCollapsedMinimalMvpActionPanelRect()
        {
            float width = OverlayTextRightCollapsedActionPanelReserve - (MinimalMvpActionPanelMargin * 2f);
            float x = Mathf.Max(MinimalMvpActionPanelMargin, Screen.width - width - MinimalMvpActionPanelMargin);
            return new Rect(x, MinimalMvpActionPanelMargin, width, MinimalMvpActionPanelButtonHeight + (MinimalMvpActionPanelMargin * 2f));
        }

        public Rect GetMinimalMvpActionPanelRect()
        {
            return CalculateMinimalMvpActionPanelRect(Screen.width, Screen.height);
        }

        public static Rect CalculateMinimalMvpActionPanelRect(float viewportWidth, float viewportHeight)
        {
            float availableWidth = Mathf.Max(1f, viewportWidth - (MinimalMvpActionPanelMargin * 2f));
            float desiredWidth = Mathf.Clamp(viewportWidth * MinimalMvpActionPanelViewportWidthRatio,
                MinimalMvpActionPanelMinimumWidth, MinimalMvpActionPanelMaximumWidth);
            float width = Mathf.Min(availableWidth, desiredWidth);
            float height = Mathf.Max(1f, viewportHeight - (MinimalMvpActionPanelMargin * 2f));
            float x = Mathf.Max(MinimalMvpActionPanelMargin,
                viewportWidth - width - MinimalMvpActionPanelMargin);
            return new Rect(x, MinimalMvpActionPanelMargin, width, height);
        }

        public static Rect CalculateOverlayTextSafeArea(float viewportWidth, float viewportHeight, bool actionPanelCollapsed)
        {
            float rightEdge = actionPanelCollapsed
                ? viewportWidth - OverlayTextRightCollapsedActionPanelReserve
                : CalculateMinimalMvpActionPanelRect(viewportWidth, viewportHeight).xMin -
                    MinimalMvpActionPanelMargin;
            float leftEdge = Mathf.Min(OverlayTextSafeLeftMargin, Mathf.Max(0f, rightEdge - 1f));
            return new Rect(
                leftEdge,
                OverlayTextSafeTopMargin,
                Mathf.Max(1f, rightEdge - leftEdge),
                Mathf.Max(1f, viewportHeight - OverlayTextSafeTopMargin - OverlayTextSafeBottomMargin));
        }

        private void DrawMinimalMvpActionPanel()
        {
            if (_root == null || !PlayerFacingPanelsVisible)
            {
                return;
            }

            if (NarrowHallRepairOnlyVisible)
            {
                GUILayout.BeginArea(GetMinimalMvpActionPanelRect(), GUI.skin.box);
                GUILayout.Label(GetLocalizedString(
                    Gd66MigrationReasonRegistry.PlayerLocalizationKey(
                        DetachedSpatialMigrationPreparer.NarrowHallReason),
                    Gd66MigrationReasonRegistry.PlayerLocalizationKey(
                        DetachedSpatialMigrationPreparer.NarrowHallReason)), GUI.skin.label);
                if (_root.SaveService.NarrowHallRepairTargets.Count > 1)
                {
                    foreach (int roomIndex in _root.SaveService.NarrowHallRepairTargets)
                    {
                        string key = roomIndex == 0
                            ? "save.migration.spatial.gd66.repair.target_room_1"
                            : "save.migration.spatial.gd66.repair.target_room_2";
                        bool selected = roomIndex == _root.SaveService.NarrowHallRepairTargetRoomIndex;
                        GUI.enabled = !selected;
                        if (GUILayout.Button(GetLocalizedString(key, key), GUI.skin.button))
                            _root.SaveService.SelectNarrowHallRepairTarget(roomIndex);
                        GUI.enabled = true;
                    }
                }
                if (GUILayout.Button(GetLocalizedString(NarrowHallRepairActionKey,
                    NarrowHallRepairActionKey), GUI.skin.button))
                    _root.TryRepairMigrationBlockedNarrowHall();
                GUILayout.EndArea();
                return;
            }

            if (!NormalGameplayActionsAvailable) return;

            if (_minimalMvpActionPanelCollapsed)
            {
                DrawCollapsedMinimalMvpActionPanel();
                return;
            }

            string placementComparisonText = BuildSelectedMvpPlacementComparisonText();
            MinimalMvpActionPanelLabels labels = MinimalMvpActionPanelPresenter.BuildPlacementLabels(
                (key, fallback) => GetLocalizedString(key, fallback),
                _selectedMvpPlacementCategoryId,
                _selectedMvpPlacementOptionId,
                _selectedMvpStructureId,
                GetSelectedMvpRunPostureNameKey(),
                placementComparisonText);
            GUIStyle compactBox = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(6, 6, 4, 4)
            };
            GUIStyle compactLabel = new GUIStyle(GUI.skin.label)
            {
                clipping = TextClipping.Clip,
                wordWrap = false,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(2, 2, 0, 0)
            };
            GUIStyle wrappedLabel = new GUIStyle(compactLabel)
            {
                clipping = TextClipping.Overflow,
                wordWrap = true
            };
            GUIStyle compactButton = new GUIStyle(GUI.skin.button)
            {
                margin = new RectOffset(0, 0, 1, 1),
                padding = new RectOffset(4, 4, 1, 1)
            };
            GUIStyle groupHeaderLabel = new GUIStyle(compactLabel)
            {
                fontStyle = FontStyle.Bold
            };
            GUILayoutOption labelHeight = GUILayout.Height(MinimalMvpActionPanelLabelHeight);
            GUILayoutOption buttonHeight = GUILayout.Height(MinimalMvpActionPanelButtonHeight);

            Rect panelRect = GetMinimalMvpActionPanelRect();
            GUILayout.BeginArea(panelRect, compactBox);
            _minimalMvpActionPanelScrollPosition = GUILayout.BeginScrollView(
                _minimalMvpActionPanelScrollPosition,
                false,
                true,
                GUILayout.Width(Mathf.Max(1f, panelRect.width - MinimalMvpActionPanelScrollBarWidth)));
            GUILayout.Label(labels.Title, compactLabel, labelHeight);
            GUILayout.Label(labels.CategoryLabel, compactLabel, labelHeight);
            GUILayout.Label(labels.SelectedStructureLabel, compactLabel, labelHeight);
            GUILayout.Label(labels.PostureLabel, compactLabel, labelHeight);
            GUILayout.Label(labels.PreviewText, wrappedLabel);
            if (!string.IsNullOrWhiteSpace(labels.ComparisonText))
            {
                GUILayout.Label(labels.ComparisonText, wrappedLabel);
            }
            GUILayout.Label(labels.RunPlanPreviewText, wrappedLabel);
            GUILayout.Label(MvpRoomSlotTargetPresenter.BuildSelectedTargetText(_root.Save, _root.RunSimulationConfig, (key, fallback) => GetLocalizedString(key, fallback)), wrappedLabel);
            GUILayout.Label(GetSelectedMvpRoomCapacityText(), wrappedLabel);
            string selectedPlacementFitText = GetSelectedMvpPlacementFitText();
            if (!string.IsNullOrWhiteSpace(selectedPlacementFitText))
            {
                GUILayout.Label(selectedPlacementFitText, wrappedLabel);
            }
            if (GUILayout.Button(GetLocalizedString("ui.mvp_room_slots.cycle_target_button"), compactButton, buttonHeight))
            {
                CycleSelectedMvpRoomSlotTarget();
            }
            if (!CanonicalMvpRouteProjection.IsCanonical(_root.Save) &&
                GUILayout.Button(GetLocalizedString(AddBasicRoomSlotButtonKey), compactButton, buttonHeight))
            {
                AddMvpBasicRoomSlot();
            }
            if (StructuralConstructionControlsAvailable)
                DrawStructuralConstructionControls(compactLabel, compactButton, groupHeaderLabel,
                    labelHeight, buttonHeight);
            if (StructuralRenovationControlsAvailable)
                DrawStructuralRenovationControls(compactLabel, compactButton, groupHeaderLabel,
                    labelHeight, buttonHeight);
            if (!string.IsNullOrEmpty(_structuralFeedback))
                GUILayout.Label(_structuralFeedback, wrappedLabel);
            if (GUILayout.Button(labels.PlacementButton, compactButton, buttonHeight))
            {
                PlaceSelectedMvpStructure();
            }

            if (GUILayout.Button(labels.RunButton, compactButton, buttonHeight))
            {
                RunOrObserveDungeon();
            }
            PlayerResearchPanelPresentation research = ResolvePlayerResearchPanelPresentation();
            GUILayout.Label(research.StatusText, wrappedLabel);
            if (research.ShowAction && GUILayout.Button(research.ActionText, compactButton, buttonHeight))
            {
                if (research.ActionClaimsResearch) ClaimPlayerResearch();
                else StartPlayerResearch();
            }
            GUILayout.Label(labels.RoomsGroupHeader, groupHeaderLabel, labelHeight);
            if (GUILayout.Button(labels.BasicRoomSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.RoomCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.BasicRoomOptionId);
            }
            if (!CanonicalMvpRouteProjection.IsCanonical(_root.Save) &&
                GUILayout.Button(labels.NarrowHallSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.RoomCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.NarrowHallOptionId);
            }
            GUILayout.Label(labels.MonstersGroupHeader, groupHeaderLabel, labelHeight);
            if (GUILayout.Button(labels.SkeletonSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.MonsterCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.SkeletonOptionId);
            }
            if (GUILayout.Button(labels.GoblinSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.MonsterCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.GoblinOptionId);
            }
            GUILayout.Label(labels.TrapsGroupHeader, groupHeaderLabel, labelHeight);
            if (GUILayout.Button(labels.SpikeTrapSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.TrapCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.SpikeTrapOptionId);
            }
            if (GUILayout.Button(labels.SnareTrapSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.TrapCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.SnareTrapOptionId);
            }
            if (GUILayout.Button(labels.ChillingSigilSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.TrapCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.ChillingSigilOptionId);
            }
            GUILayout.Label(labels.LootGroupHeader, groupHeaderLabel, labelHeight);
            if (GUILayout.Button(labels.BasicLootNodeSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.LootNodeCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.BasicLootNodeOptionId);
            }
            if (GUILayout.Button(labels.HiddenCacheSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.LootNodeCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.HiddenCacheOptionId);
            }
            if (GUILayout.Button(labels.GlitteringHoardSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.LootNodeCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.GlitteringHoardOptionId);
            }
            if (GUILayout.Button(labels.CautiousPosture, compactButton, buttonHeight))
            {
                SelectMvpRunPosture(RunPostureResolver.CautiousId);
            }
            if (GUILayout.Button(labels.BalancedPosture, compactButton, buttonHeight))
            {
                SelectMvpRunPosture(RunPostureResolver.BalancedId);
            }
            if (GUILayout.Button(labels.GreedyPosture, compactButton, buttonHeight))
            {
                SelectMvpRunPosture(RunPostureResolver.GreedyId);
            }
            if (DiagnosticsAllowed)
            {
                GUILayout.Label(GetLocalizedString(_diagnosticsVisible
                    ? "ui.mvp_view.diagnostics_mode.status"
                    : "ui.mvp_view.player_mode.status"), compactLabel, labelHeight);
                string diagnosticsButton = _diagnosticsVisible ? labels.HideDiagnosticsButton : labels.ShowDiagnosticsButton;
                if (GUILayout.Button(diagnosticsButton, compactButton, buttonHeight))
                {
                    ToggleDiagnosticsVisibility();
                    RefreshOverlayText();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawStructuralConstructionControls(GUIStyle label, GUIStyle button,
            GUIStyle heading, GUILayoutOption labelHeight, GUILayoutOption buttonHeight)
        {
            RoomSpatialDefinition room = ResolveSelectedStructuralRoom();
            GUILayout.Label(GetLocalizedString("ui.structural.heading"), heading, labelHeight);
            GUILayout.Label(string.Format(CultureInfo.InvariantCulture,
                GetLocalizedString("ui.structural.room.format"),
                ResolveStructuralRoomDisplayName(room)), label, labelHeight);
            if (GUILayout.Button(GetLocalizedString("ui.structural.room.next"), button, buttonHeight))
                CycleStructuralRoom();
            GUILayout.Label(string.Format(CultureInfo.InvariantCulture,
                GetLocalizedString("ui.structural.orientation.format"),
                StructuralOrientationDisplay()), label, labelHeight);
            if (GUILayout.Button(GetLocalizedString("ui.structural.orientation.next"), button, buttonHeight))
                CycleStructuralOrientation();
            GUILayout.Label(StructuralAnchorDisplay, label, labelHeight);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(GetLocalizedString("ui.structural.anchor.x.decrease"), button, buttonHeight))
                AdjustStructuralAnchor(-1, 0);
            if (GUILayout.Button(GetLocalizedString("ui.structural.anchor.x.increase"), button, buttonHeight))
                AdjustStructuralAnchor(1, 0);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(GetLocalizedString("ui.structural.anchor.y.decrease"), button, buttonHeight))
                AdjustStructuralAnchor(0, -1);
            if (GUILayout.Button(GetLocalizedString("ui.structural.anchor.y.increase"), button, buttonHeight))
                AdjustStructuralAnchor(0, 1);
            GUILayout.EndHorizontal();
            GUILayout.Label(string.Format(CultureInfo.InvariantCulture,
                GetLocalizedString("ui.structural.exit.format"),
                BuildStructuralConnectionPointDisplay()), label, labelHeight);
            if (GUILayout.Button(GetLocalizedString("ui.structural.exit.next"), button, buttonHeight))
                CycleStructuralConnectionPoint();
            if (GUILayout.Button(GetLocalizedString("ui.structural.preview.action"), button, buttonHeight))
                PreviewStructuralConstruction();
            bool priorEnabled = GUI.enabled;
            GUI.enabled = priorEnabled && _root.StructuralConstructionPreview != null &&
                _root.StructuralConstructionPreview.IsValid;
            if (GUILayout.Button(GetLocalizedString("ui.structural.commit.action"), button, buttonHeight))
                CommitStructuralConstruction();
            GUI.enabled = priorEnabled;
        }

        private void DrawStructuralRenovationControls(GUIStyle label, GUIStyle button,
            GUIStyle heading, GUILayoutOption labelHeight, GUILayoutOption buttonHeight)
        {
            GUILayout.Label(GetLocalizedString("ui.structural.renovation.heading"), heading, labelHeight);
            GUILayout.Label(string.Format(CultureInfo.InvariantCulture,
                GetLocalizedString("ui.structural.renovation.target.format"),
                Array.IndexOf(ResolveRenovationRoomIds(), _selectedRenovationRoomInstanceId) + 1), label, labelHeight);
            if (GUILayout.Button(GetLocalizedString("ui.structural.renovation.target.next"), button, buttonHeight))
                CycleRenovationTarget();
            GUILayout.Label(RenovationAnchorDisplay, label, labelHeight);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(GetLocalizedString("ui.structural.anchor.x.decrease"), button, buttonHeight))
                AdjustRenovationAnchor(-1, 0);
            if (GUILayout.Button(GetLocalizedString("ui.structural.anchor.x.increase"), button, buttonHeight))
                AdjustRenovationAnchor(1, 0);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(GetLocalizedString("ui.structural.anchor.y.decrease"), button, buttonHeight))
                AdjustRenovationAnchor(0, -1);
            if (GUILayout.Button(GetLocalizedString("ui.structural.anchor.y.increase"), button, buttonHeight))
                AdjustRenovationAnchor(0, 1);
            GUILayout.EndHorizontal();
            if (GUILayout.Button(GetLocalizedString("ui.structural.renovation.move.preview.action"), button, buttonHeight))
                PreviewStructuralMovement();
            GUILayout.Label(string.Format(CultureInfo.InvariantCulture,
                GetLocalizedString("ui.structural.renovation.replacement.format"),
                ResolveStructuralRoomDisplayName(ResolveSelectedStructuralRoom())), label, labelHeight);
            if (GUILayout.Button(GetLocalizedString("ui.structural.room.next"), button, buttonHeight))
                CycleStructuralRoom();
            if (GUILayout.Button(GetLocalizedString("ui.structural.renovation.replace.preview.action"), button, buttonHeight))
                PreviewStructuralReplacement();
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && _root.StructuralRenovationPreview?.IsValid == true;
            if (GUILayout.Button(GetLocalizedString("ui.structural.renovation.commit.action"), button, buttonHeight))
                CommitStructuralRenovation();
            GUI.enabled = enabled;
        }

        private string BuildSelectedMvpPlacementComparisonText()
        {
            if (_root == null)
            {
                return string.Empty;
            }

            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(
                _root.Save, _root.RunSimulationConfig, _root.ProductionSpatialContent);
            int selectedRoomIndex = MvpRoomSlotTargetResolver.ResolveClampedSelectedRoomIndex(_root.Save, layout);
            MvpPlacementComparisonPreview preview = MvpPlacementComparisonPresenter.Resolve(
                _root.Save,
                _root.RunSimulationConfig,
                _root.ProductionSpatialContent,
                selectedRoomIndex,
                _selectedMvpPlacementCategoryId,
                _selectedMvpPlacementOptionId);
            return MvpPlacementComparisonPresenter.BuildComparisonText(preview, (key, fallback) => GetLocalizedString(key, fallback));
        }

        private void ClearStaleRoomSlotPlacementFailureFeedback()
        {
            if (!_roomSlotPlacementFailureIsLatestAction && IsRoomSlotPlacementFailureFeedback() && IsSelectedPlacementAssignedInValidRoomSlot())
            {
                _mvpStructurePlacementFeedback = string.Empty;
                _roomSlotPlacementFailureIsLatestAction = false;
            }
        }

        private void ClearRoomSlotPlacementFailureFeedback()
        {
            if (IsRoomSlotPlacementFailureFeedback())
            {
                _mvpStructurePlacementFeedback = string.Empty;
            }

            _roomSlotPlacementFailureIsLatestAction = false;
        }

        private bool IsRoomSlotPlacementFailureFeedback()
        {
            if (string.IsNullOrWhiteSpace(_mvpStructurePlacementFeedback))
            {
                return false;
            }

            string format = GetLocalizedString(MvpRoomSlotTargetPresenter.NoValidSlotFormatKey, MvpRoomSlotTargetPresenter.NoValidSlotFormatKey);
            string prefix = format.Split('{')[0];
            return !string.IsNullOrWhiteSpace(prefix) && _mvpStructurePlacementFeedback.StartsWith(prefix, System.StringComparison.Ordinal);
        }

        private bool IsSelectedPlacementAssignedInValidRoomSlot()
        {
            if (_root?.Save == null ||
                string.IsNullOrWhiteSpace(_selectedMvpPlacementCategoryId) ||
                string.IsNullOrWhiteSpace(_selectedMvpPlacementOptionId) ||
                string.Equals(_selectedMvpPlacementCategoryId, MvpDungeonPlacementIds.RoomCategoryId, System.StringComparison.Ordinal))
            {
                return false;
            }

            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(
                _root.Save, _root.RunSimulationConfig, _root.ProductionSpatialContent);
            if (layout?.Rooms == null)
            {
                return false;
            }

            for (int i = 0; i < layout.Rooms.Length; i++)
            {
                MvpDungeonRoomInstance room = layout.Rooms[i];
                if (room == null || !MvpRoomSlotTargetResolver.CanAccept(room, _selectedMvpPlacementCategoryId))
                {
                    continue;
                }

                if (ContainsAssignedOption(room, _selectedMvpPlacementCategoryId, _selectedMvpPlacementOptionId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsAssignedOption(MvpDungeonRoomInstance room, string categoryId, string optionId)
        {
            string[] assigned = null;
            switch (categoryId)
            {
                case MvpDungeonPlacementIds.MonsterCategoryId:
                    assigned = room.AssignedMonsterOptionIds;
                    break;
                case MvpDungeonPlacementIds.TrapCategoryId:
                    assigned = room.AssignedTrapOptionIds;
                    break;
                case MvpDungeonPlacementIds.LootNodeCategoryId:
                    assigned = room.AssignedLootNodeOptionIds;
                    break;
            }

            if (assigned == null)
            {
                return false;
            }

            for (int i = 0; i < assigned.Length; i++)
            {
                if (string.Equals(assigned[i], optionId, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private BootstrapMvpActionHandler CreateMvpActionHandler()
        {
            return new BootstrapMvpActionHandler(new BootstrapMvpActionHandler.Context(
                (key, fallback) => GetLocalizedString(key, fallback),
                (categoryId, optionId) =>
                {
                    bool ok = _root.TryMvpPlaceOrModifySelectedPlacementEnforcingRoomTarget(
                        categoryId,
                        optionId,
                        out MvpDungeonPlacementEntry priorEntry,
                        out MvpDungeonPlacementEntry newEntry,
                        out string bannerKey,
                        out string failureFeedback,
                        out string targetFeedback);
                    return new BootstrapMvpActionHandler.PlacementAttempt(ok, priorEntry, newEntry, bannerKey, targetFeedback, failureFeedback);
                },
                () => _root.ResolveMvpPlayerLoopSummary(),
                postureId => _root.SimulateMvpActiveLoopOnce(out _, postureId),
                message => _root.SetBanner(message),
                () => _root.LastRunRejectionReasonKey));
        }

        private void ShowPlacementBanner(string structureId)
        {
            bool ok = _root.TryPlaceSelectedStructure(structureId, out string bannerKey);
            string message = _root.Content.GetString(bannerKey, bannerKey);
            _root.SetBanner(ok ? string.Format(message, structureId) : message);
        }

        private static bool IsAllowedMvpStructure(string structureId)
        {
            return structureId == StructureSimulationPass.ManaGeneratorBasicId ||
                   structureId == StructureSimulationPass.HeatScrubberBasicId ||
                   structureId == StructureSimulationPass.RiskLabBasicId;
        }

        private static string GetMvpRunPostureNameKey(string postureId)
        {
            switch (postureId)
            {
                case RunPostureResolver.CautiousId:
                    return MinimalMvpActionPanelPresenter.CautiousPostureKey;
                case RunPostureResolver.GreedyId:
                    return MinimalMvpActionPanelPresenter.GreedyPostureKey;
                case RunPostureResolver.BalancedId:
                default:
                    return MinimalMvpActionPanelPresenter.BalancedPostureKey;
            }
        }

        private static string GetMvpSelectionNameKey(string structureId)
        {
            switch (structureId)
            {
                case StructureSimulationPass.HeatScrubberBasicId:
                    return MinimalMvpActionPanelPresenter.HeatScrubberSelectionKey;
                case StructureSimulationPass.RiskLabBasicId:
                    return MinimalMvpActionPanelPresenter.RiskLabSelectionKey;
                case StructureSimulationPass.ManaGeneratorBasicId:
                default:
                    return MinimalMvpActionPanelPresenter.ManaGeneratorSelectionKey;
            }
        }
    }
}
