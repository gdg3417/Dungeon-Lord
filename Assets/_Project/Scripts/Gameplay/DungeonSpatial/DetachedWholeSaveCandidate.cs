using System;
using System.Collections.Generic;
using System.Text;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public readonly struct DetachedWholeSaveLimits
    {
        public DetachedWholeSaveLimits(int maximumCandidateBytes, int maximumCopiedValueBytes,
            int maximumUnknownMembers, int maximumUnknownMemberBytes)
        { MaximumCandidateBytes = maximumCandidateBytes; MaximumCopiedValueBytes = maximumCopiedValueBytes;
          MaximumUnknownMembers = maximumUnknownMembers; MaximumUnknownMemberBytes = maximumUnknownMemberBytes; }
        public int MaximumCandidateBytes { get; }
        public int MaximumCopiedValueBytes { get; }
        public int MaximumUnknownMembers { get; }
        public int MaximumUnknownMemberBytes { get; }
        public bool IsValid => MaximumCandidateBytes > 0 && MaximumCopiedValueBytes >= 0 &&
            MaximumUnknownMembers >= 0 && MaximumUnknownMemberBytes >= 0;
    }

    public sealed class DetachedWholeSaveCandidate
    {
        private readonly byte[] bytes;
        internal DetachedWholeSaveCandidate(byte[] value, string hash, string transactionId,
            string descriptorFingerprint)
        { bytes = (byte[])value.Clone(); Sha256 = hash; MigrationTransactionId = transactionId;
          MigrationDescriptorFingerprint = descriptorFingerprint; }
        public string Sha256 { get; }
        public string MigrationTransactionId { get; }
        public string MigrationDescriptorFingerprint { get; }
        public byte[] GetBytes() => (byte[])bytes.Clone();
    }

    public sealed class DetachedWholeSaveResult
    {
        internal DetachedWholeSaveResult(DetachedWholeSaveCandidate candidate, string reason)
        { Candidate = candidate; Reason = reason; }
        public DetachedWholeSaveCandidate Candidate { get; }
        public string Reason { get; }
        public bool IsSuccess => Candidate != null;
    }

    // This is deliberately not a SaveData serializer. It copies raw JSON values captured before
    // current constructors/migrations run and adds the two schema-7 spatial owners explicitly.
    public static class DetachedWholeSaveCandidateSerializer
    {
        public const int TargetSchemaVersion = 7;
        public const string UnknownMemberUnpreservableReason = "gd66.payload.unknown_member_unpreservable";
        public const string WorkloadExceededReason = "gd66.payload.workload_exceeded";
        public const string CandidateInvalidReason = "gd66.transaction.candidate_invalid";

        internal static DetachedWholeSaveResult BuildPrepared(RawSavePayloadClassification source,
            DetachedCanonicalSpatialSaveState spatial, CanonicalSpatialSerializationLimits spatialLimits,
            DetachedWholeSaveLimits limits)
        {
            if (!limits.IsValid) throw new ArgumentOutOfRangeException(nameof(limits));
            int sourceVersion = source != null && source.Envelope == RawSaveEnvelopeKind.UnwrappedSaveData
                ? 1 : source?.SchemaVersion ?? 0;
            if (source == null || !source.IsSuccess || sourceVersion < 1 || sourceVersion > 6)
                return Failure(CandidateInvalidReason);
            SpatialContractResult<CanonicalSpatialSaveSerializer.SerializedMembers> serialized =
                CanonicalSpatialSaveSerializer.SerializeMembers(spatial, spatialLimits);
            if (!serialized.IsValid) return Failure(CandidateInvalidReason);
            byte[] authority = serialized.Value.Authority;
            byte[] floors = serialized.Value.Floors;
            var output = new BoundedOutput(limits.MaximumCandidateBytes);
            try
            {
                output.Ascii("{\"schema\":\"save_root\",\"schemaVersion\":7,\"primary\":{");
                int copied = 0; int unknownBytes = 0; int unknownCount = 0; bool first = true;
                for (int i = 0; i < RawSavePayloadClassifier.RecognizedSaveDataMemberNames.Count; i++)
                {
                    string name = RawSavePayloadClassifier.RecognizedSaveDataMemberNames[i];
                    RawSaveMemberEvidence evidence = Find(source.Members, name);
                    if (evidence == null || evidence.State == RawSaveMemberState.Absent) continue;
                    byte[] value = evidence.GetRawValueBytes();
                    copied = CheckedAdd(copied, value.Length, limits.MaximumCopiedValueBytes);
                    Member(output, ref first, name, value);
                }
                IReadOnlyList<RawUnknownMemberEvidence> primaryUnknown = source.UnknownPrimaryMembers;
                for (int i = 0; i < primaryUnknown.Count; i++)
                {
                    RawUnknownMemberEvidence evidence = primaryUnknown[i];
                    if (IsReserved(evidence.Name)) return Failure(UnknownMemberUnpreservableReason);
                    unknownCount = CheckedAdd(unknownCount, 1, limits.MaximumUnknownMembers);
                    byte[] value = evidence.GetRawValueBytes();
                    unknownBytes = CheckedAdd(unknownBytes, value.Length, limits.MaximumUnknownMemberBytes);
                    Member(output, ref first, evidence.Name, value);
                }
                Member(output, ref first, "canonicalSpatialAuthority", authority);
                Member(output, ref first, "spatialFloors", floors);
                output.Ascii("}");
                IReadOnlyList<RawUnknownMemberEvidence> rootUnknown = source.UnknownRootMembers;
                for (int i = 0; i < rootUnknown.Count; i++)
                {
                    RawUnknownMemberEvidence evidence = rootUnknown[i];
                    if (evidence.Name == "schema" || evidence.Name == "schemaVersion" || evidence.Name == "primary")
                        return Failure(UnknownMemberUnpreservableReason);
                    unknownCount = CheckedAdd(unknownCount, 1, limits.MaximumUnknownMembers);
                    byte[] value = evidence.GetRawValueBytes();
                    unknownBytes = CheckedAdd(unknownBytes, value.Length, limits.MaximumUnknownMemberBytes);
                    output.Ascii(","); output.String(evidence.Name); output.Ascii(":"); output.Bytes(value);
                }
                output.Ascii("}");
                byte[] candidate = output.Finish();
                return new DetachedWholeSaveResult(
                    new DetachedWholeSaveCandidate(candidate, SpatialContractSha256.Compute(candidate),
                        spatial.Authority.MigrationTransactionId,
                        spatial.Authority.MigrationDescriptorFingerprint), null);
            }
            catch (BudgetException) { return Failure(WorkloadExceededReason); }
            catch { return Failure(CandidateInvalidReason); }
        }

        private static bool IsReserved(string name) => name == "canonicalSpatialAuthority" || name == "spatialFloors";
        private static RawSaveMemberEvidence Find(IReadOnlyList<RawSaveMemberEvidence> values, string name)
        { for (int i = 0; i < values.Count; i++) if (values[i].Name == name) return values[i]; return null; }
        private static int CheckedAdd(int value, int addition, int maximum)
        { if (addition > maximum - value) throw new BudgetException(); return value + addition; }
        private static DetachedWholeSaveResult Failure(string reason) => new DetachedWholeSaveResult(null, reason);
        private static void Member(BoundedOutput output, ref bool first, string name, byte[] value)
        { if (!first) output.Ascii(","); first = false; output.String(name); output.Ascii(":"); output.Bytes(value); }

        private sealed class BudgetException : Exception { }
        private sealed class BoundedOutput
        {
            private readonly int maximum; private readonly List<byte> value = new List<byte>();
            internal BoundedOutput(int maximumBytes) { maximum = maximumBytes; }
            internal void Bytes(byte[] bytes)
            { if (bytes == null || bytes.Length > maximum - value.Count) throw new BudgetException(); value.AddRange(bytes); }
            internal void Ascii(string text) => Bytes(Encoding.ASCII.GetBytes(text));
            internal void String(string text)
            {
                if (text == null) throw new BudgetException(); Ascii("\"");
                int segmentStart = 0;
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    if (c != '"' && c != '\\' && c >= 0x20) continue;
                    if (i > segmentStart) Bytes(Encoding.UTF8.GetBytes(text.Substring(segmentStart, i - segmentStart)));
                    if (c == '"' || c == '\\') Ascii("\\" + c);
                    else Ascii("\\u" + ((int)c).ToString("x4"));
                    segmentStart = i + 1;
                }
                if (segmentStart < text.Length) Bytes(Encoding.UTF8.GetBytes(text.Substring(segmentStart)));
                Ascii("\"");
            }
            internal byte[] Finish() => value.ToArray();
        }
    }
}
