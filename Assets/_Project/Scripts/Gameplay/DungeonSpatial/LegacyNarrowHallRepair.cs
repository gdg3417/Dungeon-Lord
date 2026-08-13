using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class LegacyNarrowHallRepairResult
    {
        private readonly byte[] bytes;
        internal LegacyNarrowHallRepairResult(byte[] value, string reason)
        { bytes = value == null ? null : (byte[])value.Clone(); Reason = reason; }
        public bool IsSuccess => bytes != null;
        public string Reason { get; }
        public byte[] GetBytes() => bytes == null ? null : (byte[])bytes.Clone();
    }

    /// <summary>Basic-only, lossless repair for a trusted legacy Narrow Hall payload.</summary>
    public static class LegacyNarrowHallRepair
    {
        private static readonly string[] SpatialMembers =
        { "mvpDungeonPlacements", "mvpDungeonFloorLayout", "mvpRoomSlotAssignments" };

        public static LegacyNarrowHallRepairResult Prepare(byte[] original,
            RawSavePayloadClassification classification, RawSavePayloadClassificationLimits limits,
            RawSaveEnvelopeVersionContract versions, RawLegacyBlankFloorContract blankFloor)
        {
            if (original == null || classification == null || !classification.IsSuccess ||
                SpatialContractSha256.Compute(original) != classification.SourcePayloadSha256)
                return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
            var patches = new List<Patch>();
            int selectedRoom = 0;
            RawSaveMemberEvidence selectedEvidence = classification.Members.FirstOrDefault(member =>
                member.Name == "mvpSelectedRoomSlotIndex" && member.State == RawSaveMemberState.NonNull);
            if (selectedEvidence != null && !int.TryParse(Encoding.UTF8.GetString(
                selectedEvidence.GetRawValueBytes()), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out selectedRoom))
                return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
            foreach (RawSaveMemberEvidence member in classification.Members)
            {
                if (member.State == RawSaveMemberState.Absent ||
                    !SpatialMembers.Contains(member.Name, StringComparer.Ordinal)) continue;
                JsonValue root;
                try { root = JsonValue.Parse(member.GetRawValueBytes()); }
                catch { return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason); }
                foreach (JsonValue record in Records(member.Name, root))
                {
                    JsonValue option = null;
                    if (member.Name == "mvpRoomSlotAssignments")
                    {
                        if (!int.TryParse(record.String("RoomIndex"), NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out int roomIndex) ||
                            roomIndex != selectedRoom) continue;
                        record.Fields.TryGetValue("RoomOptionId", out option);
                    }
                    else if (record.String("CategoryId") == "placement.category.room")
                        record.Fields.TryGetValue("OptionId", out option);
                    if (option != null && option.StringValue ==
                        "placement.option.room.narrow_hall")
                        patches.Add(new Patch(member.ByteOffset + option.Start,
                            option.End - option.Start));
                }
            }
            if (patches.Count == 0) return Failure(DetachedSpatialMigrationPreparer.NarrowHallReason);
            byte[] basic = Encoding.UTF8.GetBytes("\"placement.option.room.basic\"");
            var output = new List<byte>(original);
            foreach (Patch patch in patches.OrderByDescending(value => value.Start))
            { output.RemoveRange(patch.Start, patch.Length); output.InsertRange(patch.Start, basic); }
            byte[] candidate = output.ToArray();
            RawSavePayloadClassification verified = RawSavePayloadClassifier.Classify(candidate,
                limits, versions, blankFloor);
            return verified.IsSuccess
                ? new LegacyNarrowHallRepairResult(candidate, null)
                : Failure(verified.FailureReason);
        }

        private static IEnumerable<JsonValue> Records(string member, JsonValue root)
        {
            string collection = member == "mvpDungeonPlacements" ? "Entries" :
                member == "mvpDungeonFloorLayout" ? "Nodes" : "Rooms";
            return root.Fields.TryGetValue(collection, out JsonValue value) && value.Items != null
                ? value.Items.Where(item => item.Fields != null) : Array.Empty<JsonValue>();
        }

        private readonly struct Patch
        {
            internal Patch(int start, int length) { Start = start; Length = length; }
            internal int Start { get; }
            internal int Length { get; }
        }

        private sealed class JsonValue
        {
            internal int Start, End;
            internal string StringValue;
            internal Dictionary<string, JsonValue> Fields;
            internal List<JsonValue> Items;
            internal string String(string name) => Fields != null && Fields.TryGetValue(name,
                out JsonValue value) ? value.StringValue : null;

            internal static JsonValue Parse(byte[] bytes)
            { int index = 0; JsonValue value = Read(bytes, ref index); Skip(bytes, ref index);
              if (index != bytes.Length) throw new FormatException(); return value; }

            private static JsonValue Read(byte[] bytes, ref int index)
            {
                Skip(bytes, ref index); if (index >= bytes.Length) throw new FormatException();
                int start = index;
                if (bytes[index] == (byte)'{')
                {
                    index++; var fields = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
                    Skip(bytes, ref index);
                    while (index < bytes.Length && bytes[index] != (byte)'}')
                    {
                        string name = ReadString(bytes, ref index, out _, out _); Skip(bytes, ref index);
                        if (index >= bytes.Length || bytes[index++] != (byte)':') throw new FormatException();
                        if (fields.ContainsKey(name)) throw new FormatException();
                        fields.Add(name, Read(bytes, ref index));
                        Skip(bytes, ref index); if (bytes[index] == (byte)',') { index++; Skip(bytes, ref index); }
                        else break;
                    }
                    if (index >= bytes.Length || bytes[index++] != (byte)'}') throw new FormatException();
                    return new JsonValue { Start = start, End = index, Fields = fields };
                }
                if (bytes[index] == (byte)'[')
                {
                    index++; var items = new List<JsonValue>(); Skip(bytes, ref index);
                    while (index < bytes.Length && bytes[index] != (byte)']')
                    { items.Add(Read(bytes, ref index)); Skip(bytes, ref index);
                      if (bytes[index] == (byte)',') { index++; Skip(bytes, ref index); } else break; }
                    if (index >= bytes.Length || bytes[index++] != (byte)']') throw new FormatException();
                    return new JsonValue { Start = start, End = index, Items = items };
                }
                if (bytes[index] == (byte)'"')
                { string value = ReadString(bytes, ref index, out int s, out int e);
                  return new JsonValue { Start = s, End = e, StringValue = value }; }
                while (index < bytes.Length && bytes[index] != (byte)',' && bytes[index] != (byte)'}' &&
                    bytes[index] != (byte)']' && !char.IsWhiteSpace((char)bytes[index])) index++;
                if (index == start) throw new FormatException();
                return new JsonValue { Start = start, End = index,
                    StringValue = Encoding.UTF8.GetString(bytes, start, index - start) };
            }

            private static string ReadString(byte[] bytes, ref int index, out int start, out int end)
            {
                Skip(bytes, ref index); start = index;
                if (index >= bytes.Length || bytes[index++] != (byte)'"') throw new FormatException();
                var raw = new List<byte>();
                while (index < bytes.Length)
                {
                    byte current = bytes[index++];
                    if (current == (byte)'"') { end = index; return Encoding.UTF8.GetString(raw.ToArray()); }
                    if (current == (byte)'\\')
                    { if (index >= bytes.Length) throw new FormatException(); raw.Add(current); raw.Add(bytes[index++]); }
                    else raw.Add(current);
                }
                throw new FormatException();
            }

            private static void Skip(byte[] bytes, ref int index)
            { while (index < bytes.Length && char.IsWhiteSpace((char)bytes[index])) index++; }
        }

        private static LegacyNarrowHallRepairResult Failure(string reason) =>
            new LegacyNarrowHallRepairResult(null, reason);
    }
}
