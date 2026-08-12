using System;
using System.Collections.Generic;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class DetachedCanonicalSaveSessionUpdate
    {
        private readonly byte[] bytes;
        internal DetachedCanonicalSaveSessionUpdate(byte[] value, DetachedCanonicalSpatialSaveState state)
        { bytes = (byte[])value.Clone(); State = state; }
        public byte[] GetBytes() => (byte[])bytes.Clone();
        internal DetachedCanonicalSpatialSaveState State { get; }
    }

    public sealed class DetachedCanonicalSaveSessionResult
    {
        internal DetachedCanonicalSaveSessionResult(DetachedCanonicalSaveSession session,
            DetachedCanonicalSaveSessionUpdate update, string reason)
        { Session = session; Update = update; Reason = reason; }
        public DetachedCanonicalSaveSession Session { get; }
        public DetachedCanonicalSaveSessionUpdate Update { get; }
        public string Reason { get; }
        public bool IsSuccess => Session != null || Update != null;
    }

    /// <summary>
    /// Inactive lossless owner for one contextually validated schema-7 complete save. It retains
    /// complete bytes and builds detached replacements; it never publishes runtime state or writes files.
    /// </summary>
    public sealed class DetachedCanonicalSaveSession
    {
        private readonly byte[] currentBytes;
        private readonly DetachedCurrentTargetValidationContext context;
        private readonly SaveSpatialMigrationLimitsProfile limits;

        private DetachedCanonicalSaveSession(byte[] bytes, DetachedCurrentTargetValidationContext validation,
            SaveSpatialMigrationLimitsProfile profile)
        { currentBytes = (byte[])bytes.Clone(); context = validation; limits = profile; }

        public byte[] GetCurrentBytes() => (byte[])currentBytes.Clone();

        public static DetachedCanonicalSaveSessionResult Open(byte[] bytes,
            DetachedCurrentTargetValidationContext context, SaveSpatialMigrationLimitsProfile limits)
        {
            if (bytes == null || context == null || limits == null ||
                !limits.Raw.IsValid || !limits.Canonical.IsValid || !limits.Whole.IsValid ||
                !SameLimits(context.Limits, limits.Canonical))
                return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
            DetachedCompleteSaveValidationResult validated =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, context);
            return validated.IsValid && validated.CurrentTargetValidated
                ? new DetachedCanonicalSaveSessionResult(
                    new DetachedCanonicalSaveSession(validated.GetBytes(), context, limits), null, null)
                : Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
        }

        public DetachedCanonicalSaveSessionResult PrepareReplacement(DetachedCanonicalSpatialSaveState replacement)
        {
            SpatialContractResult<CanonicalSpatialSaveSerializer.SerializedMembers> spatial =
                CanonicalSpatialSaveSerializer.SerializeMembers(replacement, limits.Canonical);
            if (!spatial.IsValid)
                return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
            try
            {
                var issues = new SpatialIssueCollector(limits.Canonical.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(currentBytes, limits.Canonical.Serialized, issues,
                    out ContractJsonNode root))
                    return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
                ContractJsonNode primary = root.Fields[2].Value;
                var writer = new ContractJsonWriter(limits.Canonical.Serialized);
                writer.Node(); writer.Token("{");
                WriteField(writer, root.Fields[0].Key, root.Fields[0].Value, true);
                WriteField(writer, root.Fields[1].Key, root.Fields[1].Value, false);
                writer.Token(","); writer.String("primary"); writer.Token(":");
                writer.Node(); writer.Token("{");

                int copiedBytes = 0, unknownBytes = 0, unknownCount = 0;
                bool first = true;
                for (int index = 0; index < primary.Fields.Count - 2; index++)
                {
                    KeyValuePair<string, ContractJsonNode> field = primary.Fields[index];
                    byte[] valueBytes = SerializeNode(field.Value);
                    bool recognized = Contains(RawSavePayloadClassifier.RecognizedSaveDataMemberNames, field.Key);
                    if (recognized)
                        copiedBytes = Add(copiedBytes, valueBytes.Length,
                            limits.Whole.MaximumCopiedValueBytes);
                    else
                    {
                        unknownCount = Add(unknownCount, 1, limits.Whole.MaximumUnknownMembers);
                        unknownBytes = Add(unknownBytes, valueBytes.Length,
                            limits.Whole.MaximumUnknownMemberBytes);
                    }
                    WriteField(writer, field.Key, field.Value, first); first = false;
                }
                WriteRawField(writer, "canonicalSpatialAuthority", spatial.Value.Authority, first);
                WriteRawField(writer, "spatialFloors", spatial.Value.Floors, false);
                writer.Token("}");

                for (int index = 3; index < root.Fields.Count; index++)
                {
                    KeyValuePair<string, ContractJsonNode> field = root.Fields[index];
                    byte[] valueBytes = SerializeNode(field.Value);
                    unknownCount = Add(unknownCount, 1, limits.Whole.MaximumUnknownMembers);
                    unknownBytes = Add(unknownBytes, valueBytes.Length,
                        limits.Whole.MaximumUnknownMemberBytes);
                    WriteField(writer, field.Key, field.Value, false);
                }
                writer.Token("}");
                byte[] candidate = writer.Finish();
                if (candidate.Length > limits.Whole.MaximumCandidateBytes)
                    return Failure(DetachedWholeSaveCandidateSerializer.WorkloadExceededReason);
                DetachedCompleteSaveValidationResult validated =
                    DetachedCompleteSaveContract.ParseValidateAndRoundTrip(candidate, context);
                return validated.IsValid && validated.CurrentTargetValidated
                    ? new DetachedCanonicalSaveSessionResult(null,
                        new DetachedCanonicalSaveSessionUpdate(validated.GetBytes(), validated.State), null)
                    : Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
            }
            catch (WorkloadException)
            { return Failure(DetachedWholeSaveCandidateSerializer.WorkloadExceededReason); }
            catch (ContractJsonBudgetException)
            { return Failure(DetachedWholeSaveCandidateSerializer.WorkloadExceededReason); }
            catch
            { return Failure(DetachedWholeSaveCandidateSerializer.UnknownMemberUnpreservableReason); }
        }

        private byte[] SerializeNode(ContractJsonNode node)
        {
            var writer = new ContractJsonWriter(limits.Canonical.Serialized);
            DetachedCompleteSaveContract.WriteCanonicalNode(writer, node);
            return writer.Finish();
        }

        private static void WriteField(ContractJsonWriter writer, string key,
            ContractJsonNode value, bool first)
        {
            if (!first) writer.Token(",");
            writer.String(key); writer.Token(":");
            DetachedCompleteSaveContract.WriteCanonicalNode(writer, value);
        }

        private void WriteRawField(ContractJsonWriter writer, string key, byte[] value, bool first)
        {
            if (!first) writer.Token(",");
            var issues = new SpatialIssueCollector(limits.Canonical.Serialized.MaximumDiagnostics);
            if (!ContractJson.TryParse(value, limits.Canonical.Serialized, issues, out ContractJsonNode node))
                throw new InvalidOperationException();
            writer.String(key); writer.Token(":");
            DetachedCompleteSaveContract.WriteCanonicalNode(writer, node);
        }

        private static int Add(int current, int addition, int maximum)
        {
            if (addition > maximum - current) throw new WorkloadException();
            return current + addition;
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        { for (int index = 0; index < values.Count; index++) if (values[index] == value) return true; return false; }

        private static bool SameLimits(CanonicalSpatialSerializationLimits left,
            CanonicalSpatialSerializationLimits right) =>
            left.Serialized.MaximumInputBytes == right.Serialized.MaximumInputBytes &&
            left.Serialized.MaximumParsedNodes == right.Serialized.MaximumParsedNodes &&
            left.Serialized.MaximumCollectionRecords == right.Serialized.MaximumCollectionRecords &&
            left.Serialized.MaximumStringCharacters == right.Serialized.MaximumStringCharacters &&
            left.Serialized.MaximumDiagnostics == right.Serialized.MaximumDiagnostics &&
            left.Spatial.MaximumRecords == right.Spatial.MaximumRecords &&
            left.Spatial.MaximumMaterializedTiles == right.Spatial.MaximumMaterializedTiles;

        private static DetachedCanonicalSaveSessionResult Failure(string reason) =>
            new DetachedCanonicalSaveSessionResult(null, null, reason);
        private sealed class WorkloadException : Exception { }
    }
}
