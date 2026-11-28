using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum DraftDurability { Acknowledged, Pending, Failed, Unknown }
    public enum DungeonDraftCommandKind { ContentReposition = 1, RoomMovement = 2, RoomConstruction = 3, RoomContent = 4, IndependentRoomPlacement = 5, RouteInsertion = 6, RouteRenovation = 7 }

    [Serializable]
    public sealed class DungeonDraftConstruction
    {
        public string IntentId;
        public bool Cancel;
        public StructuralConstructionRequest[] Placements = Array.Empty<StructuralConstructionRequest>();
        internal StructuralConstructionRequest Placement => Placements?.Length == 1 ? Placements[0] : null;
        internal bool HasExactPayload => Guid.TryParseExact(IntentId, "N", out _) &&
            (Cancel ? Placements?.Length == 0 : Placement != null &&
                !string.IsNullOrWhiteSpace(Placement.FloorInstanceId) &&
                !string.IsNullOrWhiteSpace(Placement.RoomDefinitionId) &&
                !string.IsNullOrWhiteSpace(Placement.TerminalConnectionPointId));
    }

    [Serializable]
    public sealed class DungeonDraftRouteInsertion
    {
        public StructuralRouteInsertionRequest Request;
        public string ExpectedIncomingEdgeId;
        public string ExpectedOutgoingEdgeId;
    }

    [Serializable]
    public sealed class DungeonDraftIndependentRoomPlacement
    {
        public IndependentRoomPlacementRequest Request;
        public string ExpectedRoomInstanceId;
    }

    [Serializable]
    public sealed class DungeonDraftContentReposition
    {
        public string FloorInstanceId;
        public string RoomInstanceId;
        public string AssignmentId;
        public TileCoordinate RoomLocalPosition;
    }

    [Serializable]
    public sealed class DungeonDraftCommand
    {
        public long Sequence;
        public DungeonDraftCommandKind Kind;
        public DungeonDraftContentReposition[] ContentRepositions = Array.Empty<DungeonDraftContentReposition>();
        public StructuralMovementRequest[] RoomMovements = Array.Empty<StructuralMovementRequest>();
        public DungeonDraftConstruction[] RoomConstructions = Array.Empty<DungeonDraftConstruction>();
        public DungeonDraftRoomContent[] RoomContentChanges = Array.Empty<DungeonDraftRoomContent>();
        public DungeonDraftIndependentRoomPlacement[] IndependentRoomPlacements = Array.Empty<DungeonDraftIndependentRoomPlacement>();
        public DungeonDraftRouteInsertion[] RouteInsertions = Array.Empty<DungeonDraftRouteInsertion>();
        public StructuralRouteRenovation[] RouteRenovations = Array.Empty<StructuralRouteRenovation>();
        internal StructuralRouteRenovation RouteRenovation => RouteRenovations?.Length == 1 ? RouteRenovations[0] : null;
        internal DungeonDraftRouteInsertion RouteInsertion => RouteInsertions?.Length == 1 ? RouteInsertions[0] : null;
        internal DungeonDraftIndependentRoomPlacement IndependentRoomPlacement =>
            IndependentRoomPlacements?.Length == 1 ? IndependentRoomPlacements[0] : null;
        internal DungeonDraftContentReposition ContentReposition => ContentRepositions?.Length == 1 ? ContentRepositions[0] : null;
        internal StructuralMovementRequest RoomMovement => RoomMovements?.Length == 1 ? RoomMovements[0] : null;
        internal DungeonDraftConstruction RoomConstruction => RoomConstructions?.Length == 1 ? RoomConstructions[0] : null;
        internal DungeonDraftCommand Copy() => JsonUtility.FromJson<DungeonDraftCommand>(JsonUtility.ToJson(this));
        internal bool HasExactPayload
        {
            get
            {
                if (ContentRepositions == null || RoomMovements == null || RoomConstructions == null ||
                    RoomContentChanges == null || IndependentRoomPlacements == null || RouteInsertions == null ||
                    RouteRenovations == null ||
                    ContentRepositions.Length + RoomMovements.Length + RoomConstructions.Length +
                    RoomContentChanges.Length + IndependentRoomPlacements.Length + RouteInsertions.Length +
                    RouteRenovations.Length != 1) return false;
                switch (Kind)
                {
                    case DungeonDraftCommandKind.ContentReposition: return ContentReposition != null;
                    case DungeonDraftCommandKind.RoomMovement: return RoomMovement != null;
                    case DungeonDraftCommandKind.RoomConstruction: return RoomConstruction?.HasExactPayload == true;
                    case DungeonDraftCommandKind.RoomContent: return RoomContentChanges.SingleOrDefault()?.HasExactPayload == true;
                    case DungeonDraftCommandKind.IndependentRoomPlacement:
                        return IndependentRoomPlacement?.Request != null &&
                            !string.IsNullOrWhiteSpace(IndependentRoomPlacement.ExpectedRoomInstanceId);
                    case DungeonDraftCommandKind.RouteInsertion:
                        return RouteInsertion?.Request?.Incoming?.Passage != null &&
                            RouteInsertion.Request.Outgoing?.Passage != null &&
                            !string.IsNullOrWhiteSpace(RouteInsertion.ExpectedIncomingEdgeId) &&
                            !string.IsNullOrWhiteSpace(RouteInsertion.ExpectedOutgoingEdgeId);
                    case DungeonDraftCommandKind.RouteRenovation: return RouteRenovation?.HasExactPayload == true;
                    default: return false;
                }
            }
        }
    }

    [Serializable]
    internal sealed class DungeonDraftJournal
    {
        public int FormatVersion;
        public string DraftId;
        public string BaselineFingerprint;
        public string RuleIdentity;
        public long PrefixSequence;
        public DungeonDraftCommand[] Commands;
    }

    /// <summary>Injected production validation. No tuning or writable runtime projection.</summary>
    public sealed partial class DungeonDraftContext
    {
        internal readonly ProductionSpatialContentSnapshot Production;
        internal readonly RunSimulationConfig Configuration;
        internal readonly RoomContentSpatialOccupancySnapshot Occupancy;
        internal readonly SaveSpatialMigrationLimitsProfile Limits;
        internal readonly SpatialLayoutCompatibilitySnapshot Compatibility;
        internal readonly StructuralContentRemovalPolicySnapshot RemovalPolicy;
        public DungeonDraftContext(ProductionSpatialContentSnapshot production,
            RunSimulationConfig configuration, RoomContentSpatialOccupancySnapshot occupancy,
            SaveSpatialMigrationLimitsProfile limits, SpatialLayoutCompatibilitySnapshot compatibility = null,
            StructuralContentRemovalPolicySnapshot removalPolicy = null)
        { Production = production; Configuration = configuration; Occupancy = occupancy; Limits = limits; Compatibility = compatibility; RemovalPolicy = removalPolicy; }

        internal bool Validate(DetachedCanonicalSpatialSaveState state, string[] disconnectedRooms = null,
            IReadOnlyCollection<string> rebuildingFloors = null) => Limits != null &&
            CanonicalSpatialSaveContracts.Validate(state, Limits.Canonical.Spatial).IsValid &&
            DetachedCanonicalProductionSemanticValidation.Validate(state, Production, Configuration,
                Limits.Canonical.Spatial, Occupancy, true, disconnectedRooms, rebuildingFloors).IsValid;

        public string Fingerprint(DetachedCanonicalSpatialSaveState state) => Limits != null &&
            StructuralEditService.TryFingerprint(state, Limits.Canonical, out string fingerprint)
                ? fingerprint : null;

        internal DetachedCanonicalSpatialSaveState Copy(DetachedCanonicalSpatialSaveState state) =>
            CanonicalSpatialSaveContracts.TryCanonicalize(state, Limits.Canonical.Spatial,
                out DetachedCanonicalSpatialSaveState copy) ? copy : null;

        internal bool Move(DetachedCanonicalSpatialSaveState state, DungeonDraftCommand command,
            out DetachedCanonicalSpatialSaveState candidate)
        {
            candidate = null;
            if (command?.Kind != DungeonDraftCommandKind.ContentReposition || !command.HasExactPayload || !Validate(state)) return false;
            var intent = command.ContentReposition;
            var copy = Copy(state);
            var floor = copy.Floors.SingleOrDefault(value => value.FloorInstanceId == intent.FloorInstanceId);
            var assignment = floor?.RoomContents.Assignments.SingleOrDefault(value =>
                value.AssignmentId == intent.AssignmentId && value.RoomInstanceId == intent.RoomInstanceId);
            if (assignment == null || assignment.RoomLocalPosition.Equals(intent.RoomLocalPosition)) return false;
            assignment.RoomLocalPosition = intent.RoomLocalPosition;
            if (!Validate(copy)) return false;
            candidate = copy;
            return true;
        }
    }

    public interface IDungeonDraftStore
    {
        bool OutcomeUnknown { get; }
        byte[] Read();
        bool Write(byte[] expected, byte[] next);
        bool Delete(byte[] expected);
    }

    /// <summary>Whole-dungeon, non-authoritative command journal. Payloads describe a prefix; only the store can establish committed recovery evidence.</summary>
    public sealed partial class TransactionalDungeonDraft
    {
        internal const int RoomContentFormatVersion = 5;
        internal const int InsertionFormatVersion = 6;
        public const int CurrentFormatVersion = 7;
        public const string InvalidReason = "ui.dungeon.draft.invalid";
        public const string StaleReason = "ui.dungeon.draft.stale";
        public const string PendingReason = "ui.dungeon.draft.pending";
        public const string UnknownReason = "ui.dungeon.draft.unknown";
        public const string RecoveryFailedReason = "ui.dungeon.draft.recovery_failed";
        public const string FailedReason = "ui.dungeon.draft.failed";
        public const string DeleteFailedReason = "ui.dungeon.draft.delete_failed";
        public const string LimitReason = "ui.dungeon.draft.limit";
        public const string IncompatibleReason = "ui.dungeon.draft.context_stale";
        private readonly DungeonDraftContext context;
        private readonly IDungeonDraftStore store;
        private readonly DetachedCanonicalSpatialSaveState baseline;
        private DetachedCanonicalSpatialSaveState presented;
        private readonly List<DungeonDraftCommand> commands = new List<DungeonDraftCommand>();
        private readonly List<DungeonDraftInvalidMovement> invalid = new List<DungeonDraftInvalidMovement>();
        private readonly List<DungeonDraftInvalidConstruction> invalidConstruction = new List<DungeonDraftInvalidConstruction>();
        private readonly HashSet<string> completedConstruction = new HashSet<string>(StringComparer.Ordinal);
        private int formatVersion = 2;
        private string ruleIdentity;
        private byte[] durableBytes;
        private string draftId = Guid.NewGuid().ToString("N");
        public string BaselineFingerprint { get; }
        public long AcknowledgedSequence { get; private set; }
        public int CommandCount => commands.Count;
        public DraftDurability Durability { get; private set; }
        public string Reason { get; private set; }
        public bool IsClosed { get; private set; }
        public bool HasChanges => !IsStructurallyValid || context.Fingerprint(presented) != BaselineFingerprint;
        public bool IsStructurallyValid => invalid.Count == 0 && invalidConstruction.Count == 0 &&
            !HasDisconnectedRoom && !HasIncompleteReconstruction;
        public bool HasStructuralIntent => commands.Any(c => c.Kind == DungeonDraftCommandKind.RoomMovement ||
            c.Kind == DungeonDraftCommandKind.RoomConstruction || c.Kind == DungeonDraftCommandKind.IndependentRoomPlacement ||
            c.Kind == DungeonDraftCommandKind.RouteRenovation);
        public string[] DisconnectedRoomIds => commands.Where(c => c.Kind == DungeonDraftCommandKind.IndependentRoomPlacement)
            .Select(command => command.IndependentRoomPlacement)
            .Where(placement =>
            {
                var floor = presented.Floors.Single(f => f.FloorInstanceId == placement.Request.FloorInstanceId);
                var node = floor.Layout.Nodes.Single(n => n.RoomInstanceId == placement.ExpectedRoomInstanceId);
                return !floor.Layout.Edges.Any(e => e.SourceNodeId == node.NodeId || e.DestinationNodeId == node.NodeId);
            }).Select(placement => placement.ExpectedRoomInstanceId).Distinct(StringComparer.Ordinal).ToArray();
        private bool HasDisconnectedRoom => DisconnectedRoomIds.Length != 0;
        public bool HasContentIntent => commands.Any(c => c.Kind == DungeonDraftCommandKind.RoomContent);
        public DungeonDraftInvalidMovement[] InvalidMovements => invalid.Select(value => value.Copy()).ToArray();
        public DungeonDraftInvalidConstruction[] InvalidConstructions => invalidConstruction.Select(value => value.Copy()).ToArray();
        public string StructuralReason => invalid.FirstOrDefault()?.Reason ?? invalidConstruction.FirstOrDefault()?.Reason ??
            (HasDisconnectedRoom || HasIncompleteReconstruction ? StructuralEditService.RequiredRouteReason : null);
        public bool CanSave => !IsClosed && HasChanges && Durability == DraftDurability.Acknowledged &&
            AcknowledgedSequence == commands.Count && IsStructurallyValid && context.Validate(presented);
        public DetachedCanonicalSpatialSaveState ReadModel => context.Copy(presented);
        internal bool MatchesRuleContext(DungeonDraftContext current) => formatVersion == 2 || ruleIdentity == current?.RuleIdentity(formatVersion);

        private TransactionalDungeonDraft(DetachedCanonicalSpatialSaveState state,
            DungeonDraftContext context, IDungeonDraftStore store)
        { this.context = context; this.store = store; baseline = context.Copy(state);
          presented = context.Copy(state); BaselineFingerprint = context.Fingerprint(state); }

        public static TransactionalDungeonDraft Create(DetachedCanonicalSpatialSaveState state,
            DungeonDraftContext context, IDungeonDraftStore store)
        {
            if (context == null || store == null || !context.Validate(state)) return null;
            var draft = new TransactionalDungeonDraft(state, context, store);
            byte[] empty = draft.Serialize(0);
            if (!store.Write(null, empty)) { draft.FailPersistence(); }
            else draft.durableBytes = empty;
            return draft;
        }

        public static TransactionalDungeonDraft Recover(byte[] bytes, DetachedCanonicalSpatialSaveState state,
            DungeonDraftContext context, IDungeonDraftStore store, out string reason)
        {
            reason = InvalidReason;
            try
            {
                if (context?.Limits == null || store == null || bytes == null ||
                    bytes.Length > context.Limits.Raw.MaximumInputBytes) return null;
                var issues = new SpatialIssueCollector(context.Limits.Canonical.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(bytes, context.Limits.Canonical.Serialized, issues, out _)) return null;
                var journal = DungeonDraftFormat.Parse(bytes);
                if (journal == null ||
                    !Guid.TryParseExact(journal.DraftId, "N", out _) ||
                    journal.Commands == null || journal.Commands.Length > context.Limits.Raw.MaximumArrayElements ||
                    journal.PrefixSequence != journal.Commands.Length ||
                    !bytes.SequenceEqual(DungeonDraftFormat.Encode(journal))) return null;
                if (journal.FormatVersion >= 3 && journal.RuleIdentity != context.RuleIdentity(journal.FormatVersion))
                { reason = IncompatibleReason; return null; }
                if (journal.BaselineFingerprint != context.Fingerprint(state)) { reason = StaleReason; return null; }
                if (!context.Validate(state)) return null;
                if (!ExactCompleteSaveAtomicPersistence.Same(bytes, store.Read())) { reason = RecoveryFailedReason; return null; }
                var draft = new TransactionalDungeonDraft(state, context, store) { draftId = journal.DraftId,
                    formatVersion = journal.FormatVersion, ruleIdentity = journal.RuleIdentity };
                foreach (DungeonDraftCommand command in journal.Commands)
                {
                    if (command == null || command.Sequence != draft.commands.Count + 1L ||
                        !draft.Apply(command)) return null;
                    draft.commands.Add(command.Copy());
                }
                draft.AcknowledgedSequence = journal.PrefixSequence;
                draft.durableBytes = (byte[])bytes.Clone(); draft.Reason = draft.StructuralReason; reason = null;
                return draft;
            }
            catch { reason = RecoveryFailedReason; return null; }
        }

        public bool Move(string floorId, string roomId, string assignmentId, TileCoordinate target)
        {
            if (IsClosed || Durability == DraftDurability.Unknown) return false;
            if (commands.Count >= context.Limits.Raw.MaximumArrayElements) { Reason = LimitReason; return false; }
            var command = new DungeonDraftCommand { Sequence = commands.Count + 1L,
                Kind = DungeonDraftCommandKind.ContentReposition,
                ContentRepositions = new[] { new DungeonDraftContentReposition { FloorInstanceId = floorId,
                    RoomInstanceId = roomId, AssignmentId = assignmentId, RoomLocalPosition = target } } };
            if (!context.Move(presented, command, out var candidate)) { Reason = InvalidReason; return false; }
            commands.Add(command); presented = candidate;
            if (Durability != DraftDurability.Failed) Durability = DraftDurability.Pending;
            Reason = Durability == DraftDurability.Failed ? FailedReason : PendingReason;
            return true;
        }

        public bool MoveRoom(string floorId, string roomId, TileCoordinate requestedAnchor)
        {
            if (IsClosed || Durability == DraftDurability.Unknown) return false;
            if (commands.Count >= context.Limits.Raw.MaximumArrayElements) { Reason = LimitReason; return false; }
            var command = new DungeonDraftCommand { Sequence = commands.Count + 1L,
                Kind = DungeonDraftCommandKind.RoomMovement,
                RoomMovements = new[] { new StructuralMovementRequest { FloorInstanceId = floorId,
                    RoomInstanceId = roomId, Anchor = requestedAnchor } } };
            if (!Apply(command)) { Reason = InvalidReason; return false; }
            // A recovered v2 prefix is retained byte-for-byte on disk. Its first structural
            // command explicitly upgrades the vocabulary; predecessor records stay immutable.
            formatVersion = Math.Max(formatVersion, 3); ruleIdentity = context.RuleIdentity(formatVersion);
            commands.Add(command);
            if (Durability != DraftDurability.Failed) Durability = DraftDurability.Pending;
            Reason = Durability == DraftDurability.Failed ? FailedReason : PendingReason;
            return true;
        }

        private bool Apply(DungeonDraftCommand command)
        {
            if (command == null || !command.HasExactPayload) return false;
            if (command.Kind == DungeonDraftCommandKind.ContentReposition)
            {
                if (!context.Move(presented, command, out var candidate)) return false;
                presented = candidate; return true;
            }
            if (command.Kind == DungeonDraftCommandKind.IndependentRoomPlacement)
            {
                var placement = command.IndependentRoomPlacement;
                var placementPreview = PreviewIndependentPlacement(placement.Request);
                if (!placementPreview.IsValid || placementPreview.TargetRoomInstanceId != placement.ExpectedRoomInstanceId) return false;
                presented = placementPreview.DetachedCandidate;
                return true;
            }
            if (command.Kind == DungeonDraftCommandKind.RouteRenovation)
                return ApplyRenovation(command.RouteRenovation);
            if (command.Kind == DungeonDraftCommandKind.RouteInsertion)
            {
                var insertion = command.RouteInsertion;
                var routePreview = PreviewRouteGeometry(insertion.Request);
                if (!routePreview.IsValid || routePreview.IncomingEdgeId != insertion.ExpectedIncomingEdgeId ||
                    routePreview.OutgoingEdgeId != insertion.ExpectedOutgoingEdgeId) return false;
                presented = routePreview.Candidate;
                return true;
            }
            if (command.Kind == DungeonDraftCommandKind.RoomConstruction) return ApplyConstruction(command.RoomConstruction);
            if (command.Kind == DungeonDraftCommandKind.RoomContent)
            {
                var content = context.PreviewContent(presented, command.RoomContentChanges[0]);
                if (!content.IsValid) return false;
                presented = content.Candidate; return true;
            }
            var intent = command.RoomMovement;
            var floor = presented.Floors.SingleOrDefault(f => f.FloorInstanceId == intent.FloorInstanceId);
            var room = floor?.Layout.Rooms.SingleOrDefault(r => r.RoomInstanceId == intent.RoomInstanceId);
            if (room == null || context.Compatibility == null) return false;
            var preview = StructuralRenovationService.PreviewMovement(presented, intent, context.Production,
                context.Compatibility, context.Configuration, context.Limits.Canonical);
            invalid.RemoveAll(value => value.FloorInstanceId == intent.FloorInstanceId && value.RoomInstanceId == intent.RoomInstanceId);
            if (preview.IsValid && context.Validate(preview.DetachedCandidate)) presented = preview.DetachedCandidate;
            else invalid.Add(new DungeonDraftInvalidMovement { FloorInstanceId = intent.FloorInstanceId,
                RoomInstanceId = intent.RoomInstanceId, RequestedAnchor = intent.Anchor,
                RoomDefinitionId = room.RoomDefinitionId, Orientation = room.Orientation,
                Reason = preview.ReasonCodes.FirstOrDefault() ?? StructuralEditService.LayoutInvalidReason });
            invalid.Sort((a, b) => { int floorOrder = string.CompareOrdinal(a.FloorInstanceId, b.FloorInstanceId);
                return floorOrder != 0 ? floorOrder : string.CompareOrdinal(a.RoomInstanceId, b.RoomInstanceId); });
            return true;
        }

        public StructuralEditPreview PreviewIndependentPlacement(IndependentRoomPlacementRequest request)
        {
            if (!string.IsNullOrEmpty(request?.ExistingDraftRoomId) &&
                !DisconnectedRoomIds.Contains(request.ExistingDraftRoomId))
                return StructuralEditService.InvalidPreview(StructuralEditService.TargetRoomNotBuildableReason, null);
            return StructuralEditService.PreviewIndependentPlacement(presented, request, context,
                RebuildingFloors.Contains(request?.FloorInstanceId));
        }

        public bool PlaceIndependentRoom(IndependentRoomPlacementRequest request)
        {
            if (IsClosed || Durability == DraftDurability.Unknown) return false;
            if (commands.Count >= context.Limits.Raw.MaximumArrayElements) { Reason = LimitReason; return false; }
            var preview = PreviewIndependentPlacement(request);
            if (!preview.IsValid) { Reason = preview.ReasonCodes.First(); return false; }
            var command = new DungeonDraftCommand
            {
                Sequence = commands.Count + 1L,
                Kind = DungeonDraftCommandKind.IndependentRoomPlacement,
                IndependentRoomPlacements = new[] { new DungeonDraftIndependentRoomPlacement
                    { Request = request, ExpectedRoomInstanceId = preview.TargetRoomInstanceId } }
            }.Copy();
            presented = preview.DetachedCandidate;
            commands.Add(command);
            formatVersion = Math.Max(formatVersion, InsertionFormatVersion);
            ruleIdentity = context.RuleIdentity(formatVersion);
            if (Durability != DraftDurability.Failed) Durability = DraftDurability.Pending;
            Reason = Durability == DraftDurability.Failed ? FailedReason : PendingReason;
            return true;
        }

        internal StructuralRouteInsertionPreview PreviewRouteGeometry(StructuralRouteInsertionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.RoomInstanceId) ||
                !commands.Any(command => command.IndependentRoomPlacement?.ExpectedRoomInstanceId ==
                request.RoomInstanceId && command.IndependentRoomPlacement.Request.FloorInstanceId == request.FloorInstanceId))
                return StructuralRouteInsertionService.Fail(StructuralEditService.TargetRoomNotFoundReason);
            var previous = CurrentInsertions.LastOrDefault(edit => edit.Request.RoomInstanceId == request.RoomInstanceId);
            return StructuralRouteInsertionService.Preview(context, baseline, presented, request, previous, DisconnectedRoomIds);
        }

        internal bool AcceptRouteInsertion(StructuralRouteInsertionRequest request, DetachedCompleteSaveValidationResult owner)
        {
            if (IsClosed || Durability != DraftDurability.Acknowledged) return false;
            if (commands.Count >= context.Limits.Raw.MaximumArrayElements) { Reason = LimitReason; return false; }
            if (request == null || !StructuralRouteInsertionService.RetirementEligible(owner, request.FloorInstanceId,
                    request.ReplacedEdgeId))
            { Reason = StructuralRouteInsertionService.RetirementUnavailable; return false; }
            var preview = PreviewRouteGeometry(request);
            if (!preview.IsValid) { Reason = preview.Reason; return false; }
            var command = new DungeonDraftCommand
            {
                Sequence = commands.Count + 1L, Kind = DungeonDraftCommandKind.RouteInsertion,
                RouteInsertions = new[] { new DungeonDraftRouteInsertion
                {
                    Request = request, ExpectedIncomingEdgeId = preview.IncomingEdgeId,
                    ExpectedOutgoingEdgeId = preview.OutgoingEdgeId
                } }
            }.Copy();
            presented = preview.Candidate;
            commands.Add(command);
            formatVersion = Math.Max(formatVersion, InsertionFormatVersion);
            ruleIdentity = context.RuleIdentity(formatVersion);
            Durability = DraftDurability.Pending;
            Reason = PendingReason;
            return true;
        }

        internal StructuralRouteInsertionRequest SuggestRouteInsertion(string floorId, string edgeId, string roomId,
            Func<DetachedCanonicalSpatialSaveState, double?> grossCost)
        {
            var floor = baseline.Floors.SingleOrDefault(f => f.FloorInstanceId == floorId);
            var edge = floor?.Layout.Edges.SingleOrDefault(e => e.EdgeId == edgeId);
            return edge == null ? null : StructuralRouteInsertionService.Suggest(context, presented, edge,
                roomId, request =>
                {
                    var preview = PreviewRouteGeometry(request);
                    return (preview.IsValid, preview.IsValid ? grossCost(preview.Candidate) : null);
                });
        }

        internal AuthoredRouteGeometry ReconnectRouteLink(StructuralRouteInsertionRequest request, bool incoming,
            TileCoordinate? waypoint, TileCoordinate? replacedTile, bool requireTurn, out string reason)
        {
            reason = StructuralRouteInsertionService.RetirementUnavailable;
            var original = baseline.Floors.SingleOrDefault(f => f.FloorInstanceId == request.FloorInstanceId)
                ?.Layout.Edges.SingleOrDefault(e => e.EdgeId == request.ReplacedEdgeId);
            var floor = presented.Floors.SingleOrDefault(f => f.FloorInstanceId == request.FloorInstanceId);
            var placed = floor?.Layout.Nodes.SingleOrDefault(n => n.RoomInstanceId == request.RoomInstanceId);
            if (original == null || placed == null) return null;
            var previous = CurrentInsertions.LastOrDefault(edit => edit.Request.RoomInstanceId == request.RoomInstanceId);
            return StructuralRouteInsertionService.Reconnect(context, floor,
                incoming ? original.SourceNodeId : placed.NodeId, incoming ? placed.NodeId : original.DestinationNodeId,
                incoming ? request.Incoming : request.Outgoing, incoming ? request.Outgoing : request.Incoming,
                previous?.ExpectedIncomingEdgeId, previous?.ExpectedOutgoingEdgeId, waypoint, replacedTile, requireTurn, out reason);
        }

        internal string[] RouteAuthoringRoomIds => commands.Where(c => c.Kind == DungeonDraftCommandKind.IndependentRoomPlacement)
            .Select(c => c.IndependentRoomPlacement.ExpectedRoomInstanceId).Distinct(StringComparer.Ordinal).ToArray();

        internal StructuralRouteInsertionRequest AcceptedRouteInsertion(string roomId)
        {
            var insertion = CurrentInsertions.SingleOrDefault(edit => edit.Request.RoomInstanceId == roomId);
            return insertion == null ? null : JsonUtility.FromJson<StructuralRouteInsertionRequest>(
                JsonUtility.ToJson(insertion.Request));
        }

        private IEnumerable<DungeonDraftRouteInsertion> CurrentInsertions => commands
            .Where(command => command.Kind == DungeonDraftCommandKind.RouteInsertion).Select(command => command.RouteInsertion)
            .GroupBy(edit => edit.Request.RoomInstanceId, StringComparer.Ordinal).Select(group => group.Last());

        internal bool VerifyInsertionRetirements(DetachedCompleteSaveValidationResult owner) =>
            CurrentInsertions.All(edit => StructuralRouteInsertionService.RetirementEligible(owner,
                edit.Request.FloorInstanceId, edit.Request.ReplacedEdgeId));

        public bool ConstructRoom(string intentId, StructuralConstructionRequest request) => AppendConstruction(
            new DungeonDraftConstruction { IntentId = intentId, Placements = new[] { request } });

        public bool CancelConstruction(string intentId) => AppendConstruction(
            new DungeonDraftConstruction { IntentId = intentId, Cancel = true });

        private bool AppendConstruction(DungeonDraftConstruction intent)
        {
            if (IsClosed || Durability == DraftDurability.Unknown) return false;
            if (commands.Count >= context.Limits.Raw.MaximumArrayElements) { Reason = LimitReason; return false; }
            var command = new DungeonDraftCommand { Sequence = commands.Count + 1L,
                Kind = DungeonDraftCommandKind.RoomConstruction, RoomConstructions = new[] { intent } };
            // Own the request before applying it; caller mutation cannot rewrite history.
            command = command.Copy();
            if (!command.HasExactPayload || !Apply(command)) { Reason = InvalidReason; return false; }
            formatVersion = Math.Max(formatVersion, 4); ruleIdentity = context.RuleIdentity(formatVersion);
            commands.Add(command);
            if (Durability != DraftDurability.Failed) Durability = DraftDurability.Pending;
            Reason = Durability == DraftDurability.Failed ? FailedReason : PendingReason;
            return true;
        }

        private bool ApplyConstruction(DungeonDraftConstruction intent)
        {
            if (intent?.HasExactPayload != true || completedConstruction.Contains(intent.IntentId)) return false;
            var previous = invalidConstruction.SingleOrDefault(value => value.IntentId == intent.IntentId);
            if (intent.Cancel)
            {
                if (previous == null) return false;
                invalidConstruction.Remove(previous); completedConstruction.Add(intent.IntentId); return true;
            }
            var request = intent.Placement;
            var floor = presented.Floors.SingleOrDefault(value => value.FloorInstanceId == request.FloorInstanceId);
            var definition = context.Production.Catalog.Rooms.SingleOrDefault(value => value.RoomDefinitionId == request.RoomDefinitionId);
            if (floor == null || definition == null || context.Compatibility == null ||
                previous != null && previous.Request.FloorInstanceId != request.FloorInstanceId) return false;
            var preview = StructuralEditService.Preview(presented, request, context.Production, context.Compatibility,
                context.Configuration, context.Limits.Canonical);
            if (previous != null) invalidConstruction.Remove(previous);
            if (preview.IsValid && context.Validate(preview.DetachedCandidate))
            { presented = preview.DetachedCandidate; completedConstruction.Add(intent.IntentId); }
            else invalidConstruction.Add(new DungeonDraftInvalidConstruction { IntentId = intent.IntentId,
                Request = JsonUtility.FromJson<StructuralConstructionRequest>(JsonUtility.ToJson(request)),
                Reason = preview.ReasonCodes.FirstOrDefault() ?? StructuralEditService.LayoutInvalidReason });
            invalidConstruction.Sort((a, b) => { int floorOrder = string.CompareOrdinal(a.Request.FloorInstanceId, b.Request.FloorInstanceId);
                return floorOrder != 0 ? floorOrder : string.CompareOrdinal(a.IntentId, b.IntentId); });
            return true;
        }

        public bool FlushNext()
        {
            if (IsClosed || Durability == DraftDurability.Unknown) return false;
            if (durableBytes == null)
            {
                byte[] empty = Serialize(0);
                if (!store.Write(null, empty)) return FailPersistence();
                durableBytes = empty;
            }
            if (AcknowledgedSequence < commands.Count)
            {
                byte[] next = Serialize(AcknowledgedSequence + 1);
                if (!store.Write(durableBytes, next)) return FailPersistence();
                durableBytes = next; AcknowledgedSequence++;
            }
            Durability = AcknowledgedSequence == commands.Count ? DraftDurability.Acknowledged : DraftDurability.Pending;
            Reason = Durability == DraftDurability.Pending ? PendingReason : StructuralReason;
            return true;
        }

        public bool Discard()
        {
            if (!store.Delete(durableBytes)) { FailPersistence(); Reason = DeleteFailedReason; return false; }
            IsClosed = true; return true;
        }

        internal bool PrepareCommit(DetachedCanonicalSpatialSaveState current, DungeonDraftContext currentContext,
            out TransactionalDungeonDraft recovered, out string reason)
        {
            recovered = null;
            reason = Durability == DraftDurability.Unknown ? UnknownReason : Durability == DraftDurability.Failed ? FailedReason : PendingReason;
            if (!CanSave) { if (!IsStructurallyValid) reason = StructuralReason; return false; }
            if (context.Fingerprint(current) != BaselineFingerprint) { reason = StaleReason; return false; }
            recovered = Recover(durableBytes, current, currentContext, store, out reason);
            if (recovered == null || !recovered.CanSave) return false;
            reason = null; return true;
        }

        internal string[] NormalizedMovementTargets(DetachedCanonicalSpatialSaveState proposed = null) => StructuralRenovationService.NormalizeMovementTargets(
            baseline, proposed ?? presented, commands.Where(c => c.Kind == DungeonDraftCommandKind.RoomMovement)
                .Select(c => c.RoomMovement.RoomInstanceId));

        public bool FloorChanged(string floorId)
        {
            var before = baseline.Floors.SingleOrDefault(f => f.FloorInstanceId == floorId);
            var after = presented.Floors.SingleOrDefault(f => f.FloorInstanceId == floorId);
            return invalid.Any(value => value.FloorInstanceId == floorId) ||
                invalidConstruction.Any(value => value.Request.FloorInstanceId == floorId) || JsonUtility.ToJson(before) != JsonUtility.ToJson(after);
        }

        internal void Committed()
        {
            IsClosed = true;
            if (!store.Delete(durableBytes)) Reason = DeleteFailedReason;
        }

        private bool FailPersistence() { Durability = store.OutcomeUnknown ? DraftDurability.Unknown : DraftDurability.Failed; Reason = store.OutcomeUnknown ? UnknownReason : FailedReason; return false; }
        private byte[] Serialize(long acknowledged) => DungeonDraftFormat.Encode(
            new DungeonDraftJournal { FormatVersion = formatVersion, RuleIdentity = ruleIdentity,
                DraftId = draftId, BaselineFingerprint = BaselineFingerprint, PrefixSequence = acknowledged,
                Commands = commands.Take((int)acknowledged).Select(c => c.Copy()).ToArray() });
    }
}
