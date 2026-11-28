using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    [Serializable]
    internal sealed class DungeonDraftV2Command
    {
        public long Sequence;
        public string FloorInstanceId;
        public string RoomInstanceId;
        public string AssignmentId;
        public TileCoordinate Target;
        internal DungeonDraftCommand Expand() => new DungeonDraftCommand { Sequence = Sequence,
            Kind = DungeonDraftCommandKind.ContentReposition, ContentRepositions = new[] { new DungeonDraftContentReposition {
                FloorInstanceId = FloorInstanceId, RoomInstanceId = RoomInstanceId, AssignmentId = AssignmentId,
                RoomLocalPosition = Target } } };
        internal static DungeonDraftV2Command From(DungeonDraftCommand command)
        {
            if (command?.Kind != DungeonDraftCommandKind.ContentReposition || !command.HasExactPayload)
                throw new ArgumentException();
            var value = command.ContentReposition;
            return new DungeonDraftV2Command { Sequence = command.Sequence, FloorInstanceId = value.FloorInstanceId,
                RoomInstanceId = value.RoomInstanceId, AssignmentId = value.AssignmentId, Target = value.RoomLocalPosition };
        }
    }
    [Serializable]
    internal sealed class DungeonDraftV2Journal
    {
        public int FormatVersion;
        public string DraftId;
        public string BaselineFingerprint;
        public long PrefixSequence;
        public DungeonDraftV2Command[] Commands;
    }
    [Serializable]
    internal sealed class DungeonDraftV3Command
    {
        public long Sequence;
        public DungeonDraftCommandKind Kind;
        public DungeonDraftContentReposition[] ContentRepositions;
        public StructuralMovementRequest[] RoomMovements;
        internal DungeonDraftCommand Expand() => new DungeonDraftCommand { Sequence = Sequence, Kind = Kind,
            ContentRepositions = ContentRepositions, RoomMovements = RoomMovements };
        internal static DungeonDraftV3Command From(DungeonDraftCommand command)
        {
            if (command == null || !command.HasExactPayload || (int)command.Kind > (int)DungeonDraftCommandKind.RoomMovement)
                throw new ArgumentException();
            return new DungeonDraftV3Command { Sequence = command.Sequence, Kind = command.Kind,
                ContentRepositions = command.ContentRepositions, RoomMovements = command.RoomMovements };
        }
    }
    [Serializable]
    internal sealed class DungeonDraftV3Journal
    {
        public int FormatVersion;
        public string DraftId;
        public string BaselineFingerprint;
        public string RuleIdentity;
        public long PrefixSequence;
        public DungeonDraftV3Command[] Commands;
    }
    // Frozen vocabulary and field order for format 5 and its record-version 4 deltas.
    [Serializable]
    internal sealed class DungeonDraftV5Command
    {
        public long Sequence;
        public DungeonDraftCommandKind Kind;
        public DungeonDraftContentReposition[] ContentRepositions;
        public StructuralMovementRequest[] RoomMovements;
        public DungeonDraftConstruction[] RoomConstructions;
        public DungeonDraftRoomContent[] RoomContentChanges;

        internal DungeonDraftCommand Expand() => new DungeonDraftCommand
        {
            Sequence = Sequence, Kind = Kind, ContentRepositions = ContentRepositions,
            RoomMovements = RoomMovements, RoomConstructions = RoomConstructions, RoomContentChanges = RoomContentChanges
        };

        internal static DungeonDraftV5Command From(DungeonDraftCommand value)
        {
            if (value == null || (int)value.Kind > (int)DungeonDraftCommandKind.RoomContent ||
                value.IndependentRoomPlacements?.Length != 0 || value.RouteInsertions?.Length != 0) throw new ArgumentException();
            return new DungeonDraftV5Command
            {
                Sequence = value.Sequence, Kind = value.Kind, ContentRepositions = value.ContentRepositions,
                RoomMovements = value.RoomMovements, RoomConstructions = value.RoomConstructions,
                RoomContentChanges = value.RoomContentChanges
            };
        }
    }

    [Serializable]
    internal sealed class DungeonDraftV5Journal
    {
        public int FormatVersion;
        public string DraftId;
        public string BaselineFingerprint;
        public string RuleIdentity;
        public long PrefixSequence;
        public DungeonDraftV5Command[] Commands;
        internal DungeonDraftJournal Expand() => new DungeonDraftJournal
        {
            FormatVersion = FormatVersion, DraftId = DraftId, BaselineFingerprint = BaselineFingerprint,
            RuleIdentity = RuleIdentity, PrefixSequence = PrefixSequence, Commands = Commands?.Select(c => c?.Expand()).ToArray()
        };
        internal static DungeonDraftV5Journal From(DungeonDraftJournal journal) => new DungeonDraftV5Journal
        {
            FormatVersion = journal.FormatVersion, DraftId = journal.DraftId, BaselineFingerprint = journal.BaselineFingerprint,
            RuleIdentity = journal.RuleIdentity, PrefixSequence = journal.PrefixSequence,
            Commands = journal.Commands.Select(DungeonDraftV5Command.From).ToArray()
        };
    }

    [Serializable]
    internal sealed class DungeonDraftV4Candidate
    {
        public int Version;
        public string DraftId;
        public string Baseline;
        public long Sequence;
        public string PredecessorCommit;
        public int JournalFormatVersion;
        public string RuleIdentity;
        public DungeonDraftV5Command[] Commands;
        internal DungeonDraftCandidate Expand() => new DungeonDraftCandidate
        {
            Version = Version, DraftId = DraftId, Baseline = Baseline, Sequence = Sequence,
            PredecessorCommit = PredecessorCommit, JournalFormatVersion = JournalFormatVersion,
            RuleIdentity = RuleIdentity, Commands = Commands?.Select(c => c?.Expand()).ToArray()
        };
        internal static DungeonDraftV4Candidate From(DungeonDraftCandidate candidate) => new DungeonDraftV4Candidate
        {
            Version = candidate.Version, DraftId = candidate.DraftId, Baseline = candidate.Baseline, Sequence = candidate.Sequence,
            PredecessorCommit = candidate.PredecessorCommit, JournalFormatVersion = candidate.JournalFormatVersion,
            RuleIdentity = candidate.RuleIdentity, Commands = candidate.Commands.Select(DungeonDraftV5Command.From).ToArray()
        };
    }

    // Frozen wire fields for the Amendment A command vocabulary.
    [Serializable]
    internal sealed class DungeonDraftV6Command
    {
        public long Sequence;
        public DungeonDraftCommandKind Kind;
        public DungeonDraftContentReposition[] ContentRepositions;
        public StructuralMovementRequest[] RoomMovements;
        public DungeonDraftConstruction[] RoomConstructions;
        public DungeonDraftRoomContent[] RoomContentChanges;
        public DungeonDraftIndependentRoomPlacement[] IndependentRoomPlacements;
        public DungeonDraftRouteInsertion[] RouteInsertions;

        internal DungeonDraftCommand Expand() => new DungeonDraftCommand
        {
            Sequence = Sequence, Kind = Kind, ContentRepositions = ContentRepositions,
            RoomMovements = RoomMovements, RoomConstructions = RoomConstructions,
            RoomContentChanges = RoomContentChanges, IndependentRoomPlacements = IndependentRoomPlacements,
            RouteInsertions = RouteInsertions
        };

        internal static DungeonDraftV6Command From(DungeonDraftCommand value)
        {
            if (value == null || (int)value.Kind > (int)DungeonDraftCommandKind.RouteInsertion ||
                value.RouteRenovations?.Length != 0) throw new ArgumentException();
            return new DungeonDraftV6Command
            {
                Sequence = value.Sequence, Kind = value.Kind, ContentRepositions = value.ContentRepositions,
                RoomMovements = value.RoomMovements, RoomConstructions = value.RoomConstructions,
                RoomContentChanges = value.RoomContentChanges, IndependentRoomPlacements = value.IndependentRoomPlacements,
                RouteInsertions = value.RouteInsertions
            };
        }
    }

    [Serializable]
    internal sealed class DungeonDraftV6Journal
    {
        public int FormatVersion;
        public string DraftId;
        public string BaselineFingerprint;
        public string RuleIdentity;
        public long PrefixSequence;
        public DungeonDraftV6Command[] Commands;

        internal DungeonDraftJournal Expand() => new DungeonDraftJournal
        {
            FormatVersion = FormatVersion, DraftId = DraftId, BaselineFingerprint = BaselineFingerprint,
            RuleIdentity = RuleIdentity, PrefixSequence = PrefixSequence,
            Commands = Commands?.Select(command => command?.Expand()).ToArray()
        };

        internal static DungeonDraftV6Journal From(DungeonDraftJournal journal) => new DungeonDraftV6Journal
        {
            FormatVersion = journal.FormatVersion, DraftId = journal.DraftId, BaselineFingerprint = journal.BaselineFingerprint,
            RuleIdentity = journal.RuleIdentity, PrefixSequence = journal.PrefixSequence,
            Commands = journal.Commands.Select(DungeonDraftV6Command.From).ToArray()
        };
    }

    [Serializable]
    internal sealed class DungeonDraftV5Candidate
    {
        public int Version;
        public string DraftId;
        public string Baseline;
        public long Sequence;
        public string PredecessorCommit;
        public int JournalFormatVersion;
        public string RuleIdentity;
        public DungeonDraftV6Command[] Commands;

        internal DungeonDraftCandidate Expand() => new DungeonDraftCandidate
        {
            Version = Version, DraftId = DraftId, Baseline = Baseline, Sequence = Sequence,
            PredecessorCommit = PredecessorCommit, JournalFormatVersion = JournalFormatVersion,
            RuleIdentity = RuleIdentity, Commands = Commands?.Select(command => command?.Expand()).ToArray()
        };

        internal static DungeonDraftV5Candidate From(DungeonDraftCandidate candidate) => new DungeonDraftV5Candidate
        {
            Version = candidate.Version, DraftId = candidate.DraftId, Baseline = candidate.Baseline, Sequence = candidate.Sequence,
            PredecessorCommit = candidate.PredecessorCommit, JournalFormatVersion = candidate.JournalFormatVersion,
            RuleIdentity = candidate.RuleIdentity, Commands = candidate.Commands.Select(DungeonDraftV6Command.From).ToArray()
        };
    }

    internal static class DungeonDraftFormat
    {
        internal static byte[] Encode(DungeonDraftJournal journal) => journal == null ? null :
            journal.FormatVersion == 2 ? Bytes(new DungeonDraftV2Journal { FormatVersion = 2,
                DraftId = journal.DraftId, BaselineFingerprint = journal.BaselineFingerprint,
                PrefixSequence = journal.PrefixSequence, Commands = journal.Commands.Select(DungeonDraftV2Command.From).ToArray() })
                : journal.FormatVersion == 3 ? Bytes(new DungeonDraftV3Journal { FormatVersion = 3,
                    DraftId = journal.DraftId, BaselineFingerprint = journal.BaselineFingerprint,
                    RuleIdentity = journal.RuleIdentity, PrefixSequence = journal.PrefixSequence,
                    Commands = journal.Commands.Select(DungeonDraftV3Command.From).ToArray() })
                : journal.FormatVersion == 4 ? Bytes(DungeonDraftV4Journal.From(journal))
                : journal.FormatVersion == 5 ? Bytes(DungeonDraftV5Journal.From(journal))
                : journal.FormatVersion == 6 ? Bytes(DungeonDraftV6Journal.From(journal)) : Bytes(journal);
        internal static byte[] Bytes(object value) => Encoding.UTF8.GetBytes(JsonUtility.ToJson(value));
        internal static T Exact<T>(byte[] bytes) where T : class
        {
            var value = JsonUtility.FromJson<T>(new UTF8Encoding(false, true).GetString(bytes));
            if (value == null || !bytes.SequenceEqual(Bytes(value))) throw new ArgumentException();
            return value;
        }
        internal static DungeonDraftJournal Parse(byte[] bytes)
        {
            var version = JsonUtility.FromJson<DungeonDraftJournal>(new UTF8Encoding(false, true).GetString(bytes));
            if (version?.FormatVersion == 2)
            {
                var old = Exact<DungeonDraftV2Journal>(bytes);
                return new DungeonDraftJournal { FormatVersion = 2, DraftId = old.DraftId,
                    BaselineFingerprint = old.BaselineFingerprint, PrefixSequence = old.PrefixSequence,
                    Commands = old.Commands?.Select(value => value?.Expand()).ToArray() };
            }
            if (version?.FormatVersion == 3)
            {
                var old = Exact<DungeonDraftV3Journal>(bytes);
                return new DungeonDraftJournal { FormatVersion = 3, DraftId = old.DraftId,
                    BaselineFingerprint = old.BaselineFingerprint, RuleIdentity = old.RuleIdentity,
                    PrefixSequence = old.PrefixSequence, Commands = old.Commands?.Select(value => value?.Expand()).ToArray() };
            }
            if (version?.FormatVersion == 4) return Exact<DungeonDraftV4Journal>(bytes).Expand();
            if (version?.FormatVersion == 5) return Exact<DungeonDraftV5Journal>(bytes).Expand();
            if (version?.FormatVersion == 6) return Exact<DungeonDraftV6Journal>(bytes).Expand();
            if (version?.FormatVersion != TransactionalDungeonDraft.CurrentFormatVersion) throw new ArgumentException();
            return Exact<DungeonDraftJournal>(bytes);
        }
    }

    // Presentation evidence only. Invalid coordinates are never inserted in a canonical save.
    public sealed class DungeonDraftInvalidMovement
    {
        public string FloorInstanceId { get; internal set; }
        public string RoomInstanceId { get; internal set; }
        public string RoomDefinitionId { get; internal set; }
        public CardinalOrientation Orientation { get; internal set; }
        public TileCoordinate RequestedAnchor { get; internal set; }
        public string Reason { get; internal set; }
        internal DungeonDraftInvalidMovement Copy() => (DungeonDraftInvalidMovement)MemberwiseClone();
    }
    public sealed class DungeonDraftInvalidConstruction
    {
        public string IntentId { get; internal set; }
        public StructuralConstructionRequest Request { get; internal set; }
        public string Reason { get; internal set; }
        internal DungeonDraftInvalidConstruction Copy() => new DungeonDraftInvalidConstruction { IntentId = IntentId,
            Request = JsonUtility.FromJson<StructuralConstructionRequest>(JsonUtility.ToJson(Request)), Reason = Reason };
    }
}
