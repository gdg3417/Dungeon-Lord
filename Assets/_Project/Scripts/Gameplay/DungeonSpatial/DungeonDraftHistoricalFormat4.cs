using System;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // Exact frozen format-4 / record-3 layouts. New payload members never enter historical bytes.
    [Serializable]
    internal sealed class DungeonDraftV4Command
    {
        public long Sequence;
        public DungeonDraftCommandKind Kind;
        public DungeonDraftContentReposition[] ContentRepositions;
        public StructuralMovementRequest[] RoomMovements;
        public DungeonDraftConstruction[] RoomConstructions;
        internal DungeonDraftCommand Expand() => new DungeonDraftCommand { Sequence = Sequence, Kind = Kind,
            ContentRepositions = ContentRepositions, RoomMovements = RoomMovements, RoomConstructions = RoomConstructions };
        internal static DungeonDraftV4Command From(DungeonDraftCommand value)
        {
            if (value == null) return null;
            if (value.RoomContentChanges?.Length > 0) throw new ArgumentException();
            return new DungeonDraftV4Command { Sequence = value.Sequence, Kind = value.Kind,
                ContentRepositions = value.ContentRepositions, RoomMovements = value.RoomMovements, RoomConstructions = value.RoomConstructions };
        }
    }
    [Serializable]
    internal sealed class DungeonDraftV4Journal
    {
        public int FormatVersion;
        public string DraftId;
        public string BaselineFingerprint;
        public string RuleIdentity;
        public long PrefixSequence;
        public DungeonDraftV4Command[] Commands;
        internal DungeonDraftJournal Expand() => new DungeonDraftJournal { FormatVersion = FormatVersion, DraftId = DraftId,
            BaselineFingerprint = BaselineFingerprint, RuleIdentity = RuleIdentity, PrefixSequence = PrefixSequence,
            Commands = Commands?.Select(c => c?.Expand()).ToArray() };
        internal static DungeonDraftV4Journal From(DungeonDraftJournal value) => new DungeonDraftV4Journal { FormatVersion = 4,
            DraftId = value.DraftId, BaselineFingerprint = value.BaselineFingerprint, RuleIdentity = value.RuleIdentity,
            PrefixSequence = value.PrefixSequence, Commands = value.Commands.Select(DungeonDraftV4Command.From).ToArray() };
    }
    [Serializable]
    internal sealed class DungeonDraftV3Candidate
    {
        public int Version;
        public string DraftId;
        public string Baseline;
        public long Sequence;
        public string PredecessorCommit;
        public int JournalFormatVersion;
        public string RuleIdentity;
        public DungeonDraftV4Command[] Commands;
        internal DungeonDraftCandidate Expand() => new DungeonDraftCandidate { Version = Version, DraftId = DraftId,
            Baseline = Baseline, Sequence = Sequence, PredecessorCommit = PredecessorCommit,
            JournalFormatVersion = JournalFormatVersion, RuleIdentity = RuleIdentity, Commands = Commands?.Select(c => c?.Expand()).ToArray() };
        internal static DungeonDraftV3Candidate From(DungeonDraftCandidate value) => new DungeonDraftV3Candidate { Version = 3,
            DraftId = value.DraftId, Baseline = value.Baseline, Sequence = value.Sequence, PredecessorCommit = value.PredecessorCommit,
            JournalFormatVersion = 4, RuleIdentity = value.RuleIdentity, Commands = value.Commands.Select(DungeonDraftV4Command.From).ToArray() };
    }
}
