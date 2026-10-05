using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum DraftDurability { Acknowledged, Pending, Failed, Unknown }

    [Serializable]
    public sealed class DungeonDraftCommand
    {
        public long Sequence;
        public string FloorInstanceId;
        public string RoomInstanceId;
        public string AssignmentId;
        public TileCoordinate Target;
        internal DungeonDraftCommand Copy() => new DungeonDraftCommand { Sequence = Sequence,
            FloorInstanceId = FloorInstanceId, RoomInstanceId = RoomInstanceId,
            AssignmentId = AssignmentId, Target = Target };
    }

    [Serializable]
    internal sealed class DungeonDraftJournal
    {
        public int FormatVersion;
        public string DraftId;
        public string BaselineFingerprint;
        public long PrefixSequence;
        public DungeonDraftCommand[] Commands;
    }

    /// <summary>Injected production validation. No tuning or writable runtime projection.</summary>
    public sealed class DungeonDraftContext
    {
        internal readonly ProductionSpatialContentSnapshot Production;
        internal readonly RunSimulationConfig Configuration;
        internal readonly RoomContentSpatialOccupancySnapshot Occupancy;
        internal readonly SaveSpatialMigrationLimitsProfile Limits;
        public DungeonDraftContext(ProductionSpatialContentSnapshot production,
            RunSimulationConfig configuration, RoomContentSpatialOccupancySnapshot occupancy,
            SaveSpatialMigrationLimitsProfile limits)
        { Production = production; Configuration = configuration; Occupancy = occupancy; Limits = limits; }

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
            if (command == null || !Validate(state)) return false;
            var copy = Copy(state);
            var floor = copy.Floors.SingleOrDefault(value => value.FloorInstanceId == command.FloorInstanceId);
            var assignment = floor?.RoomContents.Assignments.SingleOrDefault(value =>
                value.AssignmentId == command.AssignmentId && value.RoomInstanceId == command.RoomInstanceId);
            if (assignment == null || assignment.RoomLocalPosition.Equals(command.Target)) return false;
            assignment.RoomLocalPosition = command.Target;
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
        public const int CurrentFormatVersion = 2;
        public const string InvalidReason = "ui.dungeon.draft.invalid";
        public const string StaleReason = "ui.dungeon.draft.stale";
        public const string PendingReason = "ui.dungeon.draft.pending";
        public const string UnknownReason = "ui.dungeon.draft.unknown";
        public const string RecoveryFailedReason = "ui.dungeon.draft.recovery_failed";
        public const string FailedReason = "ui.dungeon.draft.failed";
        public const string DeleteFailedReason = "ui.dungeon.draft.delete_failed";
        public const string LimitReason = "ui.dungeon.draft.limit";
        private readonly DungeonDraftContext context;
        private readonly IDungeonDraftStore store;
        private readonly DetachedCanonicalSpatialSaveState baseline;
        private DetachedCanonicalSpatialSaveState presented;
        private readonly List<DungeonDraftCommand> commands = new List<DungeonDraftCommand>();
        private byte[] durableBytes;
        private string draftId = Guid.NewGuid().ToString("N");
        public string BaselineFingerprint { get; }
        public long AcknowledgedSequence { get; private set; }
        public int CommandCount => commands.Count;
        public DraftDurability Durability { get; private set; }
        public string Reason { get; private set; }
        public bool IsClosed { get; private set; }
        public bool HasChanges => context.Fingerprint(presented) != BaselineFingerprint;
        public bool CanSave => !IsClosed && HasChanges && Durability == DraftDurability.Acknowledged &&
            AcknowledgedSequence == commands.Count && context.Validate(presented);
        public DetachedCanonicalSpatialSaveState ReadModel => context.Copy(presented);

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
                if (context == null || store == null || !context.Validate(state) || bytes == null ||
                    bytes.Length > context.Limits.Raw.MaximumInputBytes) return null;
                var issues = new SpatialIssueCollector(context.Limits.Canonical.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(bytes, context.Limits.Canonical.Serialized, issues, out _)) return null;
                string text = new UTF8Encoding(false, true).GetString(bytes);
                var journal = JsonUtility.FromJson<DungeonDraftJournal>(text);
                if (journal == null || journal.FormatVersion != CurrentFormatVersion ||
                    !Guid.TryParseExact(journal.DraftId, "N", out _) ||
                    journal.Commands == null || journal.Commands.Length > context.Limits.Raw.MaximumArrayElements ||
                    journal.PrefixSequence != journal.Commands.Length ||
                    !bytes.SequenceEqual(Encoding.UTF8.GetBytes(JsonUtility.ToJson(journal)))) return null;
                if (journal.BaselineFingerprint != context.Fingerprint(state)) { reason = StaleReason; return null; }
                if (!ExactCompleteSaveAtomicPersistence.Same(bytes, store.Read())) { reason = RecoveryFailedReason; return null; }
                var draft = new TransactionalDungeonDraft(state, context, store) { draftId = journal.DraftId };
                foreach (DungeonDraftCommand command in journal.Commands)
                {
                    if (command == null || command.Sequence != draft.commands.Count + 1L ||
                        !context.Move(draft.presented, command, out var next)) return null;
                    draft.commands.Add(command.Copy()); draft.presented = next;
                }
                draft.AcknowledgedSequence = journal.PrefixSequence;
                draft.durableBytes = (byte[])bytes.Clone(); reason = null;
                return draft;
            }
            catch { reason = RecoveryFailedReason; return null; }
        }

        public bool Move(string floorId, string roomId, string assignmentId, TileCoordinate target)
        {
            if (IsClosed || Durability == DraftDurability.Unknown) return false;
            if (commands.Count >= context.Limits.Raw.MaximumArrayElements) { Reason = LimitReason; return false; }
            var command = new DungeonDraftCommand { Sequence = commands.Count + 1L,
                FloorInstanceId = floorId, RoomInstanceId = roomId, AssignmentId = assignmentId, Target = target };
            if (!context.Move(presented, command, out var candidate)) { Reason = InvalidReason; return false; }
            commands.Add(command); presented = candidate;
            if (Durability != DraftDurability.Failed) Durability = DraftDurability.Pending;
            Reason = Durability == DraftDurability.Failed ? FailedReason : PendingReason;
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
            Reason = Durability == DraftDurability.Pending ? PendingReason : null;
            return true;
        }

        public bool Discard()
        {
            if (!store.Delete(durableBytes)) { FailPersistence(); Reason = DeleteFailedReason; return false; }
            IsClosed = true; return true;
        }

        internal bool PrepareCommit(DetachedCanonicalSpatialSaveState current,
            out DetachedCanonicalSpatialSaveState final, out string reason)
        {
            final = null;
            reason = Durability == DraftDurability.Unknown ? UnknownReason : Durability == DraftDurability.Failed ? FailedReason : PendingReason;
            if (!CanSave) return false;
            if (context.Fingerprint(current) != BaselineFingerprint) { reason = StaleReason; return false; }
            var recovered = Recover(durableBytes, current, context, store, out reason);
            if (recovered == null || !recovered.CanSave) return false;
            final = recovered.ReadModel; reason = null; return true;
        }

        public bool FloorChanged(string floorId)
        {
            var before = baseline.Floors.SingleOrDefault(f => f.FloorInstanceId == floorId);
            var after = presented.Floors.SingleOrDefault(f => f.FloorInstanceId == floorId);
            return JsonUtility.ToJson(before) != JsonUtility.ToJson(after);
        }

        internal void Committed()
        {
            IsClosed = true;
            if (!store.Delete(durableBytes)) Reason = DeleteFailedReason;
        }

        private bool FailPersistence() { Durability = store.OutcomeUnknown ? DraftDurability.Unknown : DraftDurability.Failed; Reason = store.OutcomeUnknown ? UnknownReason : FailedReason; return false; }
        private byte[] Serialize(long acknowledged) => Encoding.UTF8.GetBytes(JsonUtility.ToJson(
            new DungeonDraftJournal { FormatVersion = CurrentFormatVersion,
                DraftId = draftId, BaselineFingerprint = BaselineFingerprint, PrefixSequence = acknowledged,
                Commands = commands.Take((int)acknowledged).Select(c => c.Copy()).ToArray() }));
    }
}
