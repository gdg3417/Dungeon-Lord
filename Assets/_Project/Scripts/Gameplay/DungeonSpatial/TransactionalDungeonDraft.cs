using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum DraftDurability { Acknowledged, Pending, Failed, Unknown }
    public enum DungeonDraftCommandKind { ContentReposition = 1, RoomMovement = 2 }

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
        internal DungeonDraftContentReposition ContentReposition => ContentRepositions?.Length == 1 ? ContentRepositions[0] : null;
        internal StructuralMovementRequest RoomMovement => RoomMovements?.Length == 1 ? RoomMovements[0] : null;
        internal DungeonDraftCommand Copy() => JsonUtility.FromJson<DungeonDraftCommand>(JsonUtility.ToJson(this));
        internal bool HasExactPayload => Kind == DungeonDraftCommandKind.ContentReposition
            ? ContentReposition != null && RoomMovements?.Length == 0
            : Kind == DungeonDraftCommandKind.RoomMovement && RoomMovement != null && ContentRepositions?.Length == 0;
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
        public DungeonDraftContext(ProductionSpatialContentSnapshot production,
            RunSimulationConfig configuration, RoomContentSpatialOccupancySnapshot occupancy,
            SaveSpatialMigrationLimitsProfile limits, SpatialLayoutCompatibilitySnapshot compatibility = null)
        { Production = production; Configuration = configuration; Occupancy = occupancy; Limits = limits; Compatibility = compatibility; }

        internal bool Validate(DetachedCanonicalSpatialSaveState state) => Limits != null &&
            CanonicalSpatialSaveContracts.Validate(state, Limits.Canonical.Spatial).IsValid &&
            DetachedCanonicalProductionSemanticValidation.Validate(state, Production, Configuration,
                Limits.Canonical.Spatial, Occupancy, true).IsValid;

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
    public sealed class TransactionalDungeonDraft
    {
        public const int CurrentFormatVersion = 3;
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
        public bool HasChanges => invalid.Count != 0 || context.Fingerprint(presented) != BaselineFingerprint;
        public bool IsStructurallyValid => invalid.Count == 0;
        public bool HasStructuralIntent => commands.Any(c => c.Kind == DungeonDraftCommandKind.RoomMovement);
        public DungeonDraftInvalidMovement[] InvalidMovements => invalid.Select(value => value.Copy()).ToArray();
        public string StructuralReason => invalid.FirstOrDefault()?.Reason;
        public bool CanSave => !IsClosed && HasChanges && Durability == DraftDurability.Acknowledged &&
            AcknowledgedSequence == commands.Count && IsStructurallyValid && context.Validate(presented);
        public DetachedCanonicalSpatialSaveState ReadModel => context.Copy(presented);
        internal bool MatchesRuleContext(DungeonDraftContext current) => formatVersion == 2 || ruleIdentity == current?.RuleIdentity();

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
                if (journal.FormatVersion == CurrentFormatVersion && journal.RuleIdentity != context.RuleIdentity())
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
            formatVersion = CurrentFormatVersion; ruleIdentity = context.RuleIdentity();
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

        internal string[] NormalizedMovementTargets() => StructuralRenovationService.NormalizeMovementTargets(
            baseline, presented, commands.Where(c => c.Kind == DungeonDraftCommandKind.RoomMovement)
                .Select(c => c.RoomMovement.RoomInstanceId));

        public bool FloorChanged(string floorId)
        {
            var before = baseline.Floors.SingleOrDefault(f => f.FloorInstanceId == floorId);
            var after = presented.Floors.SingleOrDefault(f => f.FloorInstanceId == floorId);
            return invalid.Any(value => value.FloorInstanceId == floorId) || JsonUtility.ToJson(before) != JsonUtility.ToJson(after);
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
