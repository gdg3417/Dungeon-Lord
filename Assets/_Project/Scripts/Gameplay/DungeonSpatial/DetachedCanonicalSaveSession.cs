using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class DetachedRecognizedSaveStateSnapshotResult
    {
        internal DetachedRecognizedSaveStateSnapshotResult(
            DetachedRecognizedSaveStateSnapshot snapshot, string reason)
        { Snapshot = snapshot; Reason = reason; }
        public DetachedRecognizedSaveStateSnapshot Snapshot { get; }
        public string Reason { get; }
        public bool IsSuccess => Snapshot != null;
    }

    public sealed class DetachedRecognizedSaveStateSnapshot
    {
        private readonly Dictionary<string, byte[]> values;
        private DetachedRecognizedSaveStateSnapshot(Dictionary<string, byte[]> source) { values = source; }

        public static DetachedRecognizedSaveStateSnapshotResult Capture(SaveData source,
            SaveSpatialMigrationLimitsProfile limits)
        {
            if (source == null || limits == null)
                return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
            try
            {
                byte[] json = Encoding.UTF8.GetBytes(JsonUtility.ToJson(source));
                var issues = new SpatialIssueCollector(limits.Canonical.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(json, limits.Canonical.Serialized, issues,
                    out ContractJsonNode root) || root.Kind != ContractJsonKind.Object)
                    return Failure(IsWorkloadFailure(issues)
                        ? DetachedWholeSaveCandidateSerializer.WorkloadExceededReason
                        : DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
                var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, ContractJsonNode> field in root.Fields)
                {
                    if (!DetachedCanonicalSaveSession.IsLiveRecognizedMember(field.Key)) continue;
                    var writer = new ContractJsonWriter(limits.Canonical.Serialized);
                    DetachedCompleteSaveContract.WriteCanonicalNode(writer, field.Value);
                    result.Add(field.Key, writer.Finish());
                }
                PreserveNullableState(result, "researchPending", source.researchPending);
                PreserveNullableState(result, "researchProgress", source.researchProgress);
                PreserveNullableState(result, "lastOfflineSummary", source.lastOfflineSummary);
                return new DetachedRecognizedSaveStateSnapshotResult(
                    new DetachedRecognizedSaveStateSnapshot(result), null);
            }
            catch (ContractJsonBudgetException)
            { return Failure(DetachedWholeSaveCandidateSerializer.WorkloadExceededReason); }
            catch
            { return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason); }
        }

        internal bool TryGet(string name, out byte[] value)
        {
            if (!values.TryGetValue(name, out byte[] stored)) { value = null; return false; }
            value = (byte[])stored.Clone(); return true;
        }

        private static DetachedRecognizedSaveStateSnapshotResult Failure(string reason) =>
            new DetachedRecognizedSaveStateSnapshotResult(null, reason);

        private static void PreserveNullableState(Dictionary<string, byte[]> result,
            string memberName, object runtimeValue)
        {
            if (runtimeValue == null) result[memberName] = Encoding.UTF8.GetBytes("null");
        }

        private static bool IsWorkloadFailure(SpatialIssueCollector issues)
        {
            if (issues.IsExhausted) return true;
            SpatialContractIssue[] values = issues.ToArray();
            for (int index = 0; index < values.Length; index++)
                if (values[index] == SpatialContractIssue.InputByteLimitExceeded ||
                    values[index] == SpatialContractIssue.WorkloadExceeded) return true;
            return false;
        }
    }

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
    /// Lossless owner for one contextually validated schema-7 complete save. It retains
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

        public DetachedCanonicalSaveSessionResult PrepareSpatialOnlyReplacement(
            DetachedCanonicalSpatialSaveState replacement) => PrepareReplacement(null, replacement);

        public DetachedCanonicalSaveSessionResult PrepareLiveReplacement(
            DetachedRecognizedSaveStateSnapshotResult recognizedState,
            DetachedCanonicalSpatialSaveState replacement)
        {
            if (recognizedState == null || !recognizedState.IsSuccess)
                return Failure(recognizedState?.Reason ??
                    DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
            return PrepareReplacement(recognizedState.Snapshot, replacement);
        }

        private DetachedCanonicalSaveSessionResult PrepareReplacement(
            DetachedRecognizedSaveStateSnapshot recognizedState,
            DetachedCanonicalSpatialSaveState replacement)
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
                IReadOnlyList<string> recognizedNames = RawSavePayloadClassifier.RecognizedSaveDataMemberNames;
                for (int nameIndex = 0; nameIndex < recognizedNames.Count; nameIndex++)
                {
                    string name = recognizedNames[nameIndex];
                    byte[] valueBytes;
                    if (IsFrozenLegacySpatialMember(name) || recognizedState == null)
                    {
                        if (!TryFind(primary, name, out ContractJsonNode retained)) continue;
                        valueBytes = SerializeNode(retained);
                    }
                    else if (!recognizedState.TryGet(name, out valueBytes)) continue;
                    copiedBytes = Add(copiedBytes, valueBytes.Length,
                        limits.Whole.MaximumCopiedValueBytes);
                    WriteRawField(writer, name, valueBytes, first); first = false;
                }
                for (int index = 0; index < primary.Fields.Count - 2; index++)
                {
                    KeyValuePair<string, ContractJsonNode> field = primary.Fields[index];
                    if (Contains(recognizedNames, field.Key)) continue;
                    byte[] valueBytes = SerializeNode(field.Value);
                    unknownCount = Add(unknownCount, 1, limits.Whole.MaximumUnknownMembers);
                    unknownBytes = Add(unknownBytes, valueBytes.Length,
                        limits.Whole.MaximumUnknownMemberBytes);
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

        internal static bool IsLiveRecognizedMember(string name) =>
            Contains(RawSavePayloadClassifier.RecognizedSaveDataMemberNames, name) &&
            !IsFrozenLegacySpatialMember(name);

        private static bool IsFrozenLegacySpatialMember(string name) =>
            name == "mvpDungeonPlacements" || name == "mvpDungeonFloorLayout" ||
            name == "mvpRoomSlotAssignments";

        private static bool TryFind(ContractJsonNode parent, string name, out ContractJsonNode value)
        {
            for (int index = 0; index < parent.Fields.Count; index++)
                if (parent.Fields[index].Key == name) { value = parent.Fields[index].Value; return true; }
            value = null; return false;
        }

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
