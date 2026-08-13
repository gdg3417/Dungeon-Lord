using System;
using System.Collections.Generic;
using System.IO;
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
            byte[] narrow = Encoding.UTF8.GetBytes("\"placement.option.room.narrow_hall\"");
            byte[] basic = Encoding.UTF8.GetBytes("\"placement.option.room.basic\"");
            var ranges = classification.Members.Where(member => SpatialMembers.Contains(member.Name,
                StringComparer.Ordinal) && member.State != RawSaveMemberState.Absent)
                .OrderByDescending(member => member.ByteOffset).ToArray();
            var output = new List<byte>(original);
            int replacements = 0;
            foreach (RawSaveMemberEvidence range in ranges)
            {
                byte[] value = range.GetRawValueBytes();
                var repaired = new List<byte>(value.Length);
                for (int index = 0; index < value.Length;)
                {
                    bool match = index <= value.Length - narrow.Length;
                    for (int scan = 0; match && scan < narrow.Length; scan++)
                        match = value[index + scan] == narrow[scan];
                    if (match)
                    { repaired.AddRange(basic); index += narrow.Length; replacements++; }
                    else repaired.Add(value[index++]);
                }
                output.RemoveRange(range.ByteOffset, range.ByteLength);
                output.InsertRange(range.ByteOffset, repaired);
            }
            if (replacements == 0) return Failure(DetachedSpatialMigrationPreparer.NarrowHallReason);
            byte[] candidate = output.ToArray();
            RawSavePayloadClassification verified = RawSavePayloadClassifier.Classify(candidate,
                limits, versions, blankFloor);
            return verified.IsSuccess
                ? new LegacyNarrowHallRepairResult(candidate, null)
                : Failure(verified.FailureReason);
        }

        private static LegacyNarrowHallRepairResult Failure(string reason) =>
            new LegacyNarrowHallRepairResult(null, reason);
    }
}
