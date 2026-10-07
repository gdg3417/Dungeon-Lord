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
    internal static class DungeonDraftFormat
    {
        internal static byte[] Encode(DungeonDraftJournal journal) => journal == null ? null :
            journal.FormatVersion == 2 ? Bytes(new DungeonDraftV2Journal { FormatVersion = 2,
                DraftId = journal.DraftId, BaselineFingerprint = journal.BaselineFingerprint,
                PrefixSequence = journal.PrefixSequence, Commands = journal.Commands.Select(DungeonDraftV2Command.From).ToArray() })
                : Bytes(journal);
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
}
