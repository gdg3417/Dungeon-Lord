using System;
using System.Collections.Generic;
using System.Linq;
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

        internal static DetachedRecognizedSaveStateSnapshotResult CaptureWithMana(SaveData source,
            double mana, SaveSpatialMigrationLimitsProfile limits)
        {
            var captured = Capture(source, limits);
            if (!captured.IsSuccess || !DungeonBuilder.M0.Economy.StructuralEconomySnapshot.Nonnegative(mana))
                return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
            try
            {
                var values = captured.Snapshot.values;
                var issues = new SpatialIssueCollector(limits.Canonical.Serialized.MaximumDiagnostics);
                if (!values.TryGetValue("structureRuntime", out byte[] bytes) ||
                    !ContractJson.TryParse(bytes, limits.Canonical.Serialized, issues, out ContractJsonNode runtime) ||
                    runtime.Kind != ContractJsonKind.Object) return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
                var writer = new ContractJsonWriter(limits.Canonical.Serialized);
                writer.Node(); writer.Token("{"); bool first = true, found = false;
                foreach (var field in runtime.Fields)
                {
                    if (!first) writer.Token(","); first = false;
                    writer.String(field.Key); writer.Token(":");
                    if (field.Key == "ManaReserve")
                    { writer.Token(mana.ToString("R", System.Globalization.CultureInfo.InvariantCulture)); found = true; }
                    else DetachedCompleteSaveContract.WriteCanonicalNode(writer, field.Value);
                }
                writer.Token("}");
                if (!found) return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
                values["structureRuntime"] = writer.Finish(); return captured;
            }
            catch { return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason); }
        }

        internal static DetachedRecognizedSaveStateSnapshotResult CaptureWithManaAndTimestamp(
            SaveData source, double mana, long lastSavedUtcUnix,
            SaveSpatialMigrationLimitsProfile limits)
        {
            var captured = CaptureWithMana(source, mana, limits);
            if (!captured.IsSuccess || lastSavedUtcUnix <= 0)
                return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
            try
            {
                captured.Snapshot.values["lastSavedUtcUnix"] = Encoding.UTF8.GetBytes(
                    lastSavedUtcUnix.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return captured;
            }
            catch { return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason); }
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
            DetachedCanonicalSaveSessionUpdate update, string reason, DetachedCompleteSaveValidationResult foregroundValidation = null)
        { Session = session; Update = update; Reason = reason; ForegroundValidation = foregroundValidation; }
        public DetachedCanonicalSaveSession Session { get; }
        public DetachedCanonicalSaveSessionUpdate Update { get; }
        public string Reason { get; }
        public bool IsSuccess => Session != null || Update != null;
        // Only a freshly validated foreground stage/readback carries this result.
        // It is consumed synchronously inside the same detached transaction.
        internal DetachedCompleteSaveValidationResult ForegroundValidation { get; }
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
            => OpenCore(bytes, context, limits, false);

        internal static DetachedCanonicalSaveSessionResult OpenForegroundReadback(byte[] bytes,
            DetachedCurrentTargetValidationContext context, SaveSpatialMigrationLimitsProfile limits)
            => OpenCore(bytes, context, limits, true);

        private static DetachedCanonicalSaveSessionResult OpenCore(byte[] bytes,
            DetachedCurrentTargetValidationContext context, SaveSpatialMigrationLimitsProfile limits, bool foreground)
        {
            using var diagnostic = ForegroundQualificationMetrics.Trace(ForegroundQualificationMetrics.Boundary.SessionOpen);
            if (bytes == null || context == null || limits == null ||
                !limits.Raw.IsValid || !limits.Canonical.IsValid || !limits.Whole.IsValid ||
                !SameLimits(context.Limits, limits.Canonical))
                return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
            DetachedCompleteSaveValidationResult validated =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, context);
            return validated.IsValid && validated.CurrentTargetValidated
                ? new DetachedCanonicalSaveSessionResult(
                    new DetachedCanonicalSaveSession(validated.GetBytes(), context, limits), null, null, foreground ? validated : null)
                : Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
        }

        public DetachedCanonicalSaveSessionResult PrepareSpatialOnlyReplacement(
            DetachedCanonicalSpatialSaveState replacement, StructuralInvestmentRecord[] investment = null) =>
            PrepareReplacement(null, replacement, investment, null, null, null);

        public DetachedCanonicalSaveSessionResult PrepareLiveReplacement(
            DetachedRecognizedSaveStateSnapshotResult recognizedState,
            DetachedCanonicalSpatialSaveState replacement, StructuralInvestmentRecord[] investment = null,
            CorridorContentAuthority corridorContent = null,
            SharedBranchKnowledgeAuthority branchKnowledge = null,
            SharedFloorKnowledgeAuthority floorKnowledge = null,
            MonsterContinuityState monsterContinuity = null)
        {
            if (recognizedState == null || !recognizedState.IsSuccess)
                return Failure(recognizedState?.Reason ??
                    DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
            return PrepareReplacement(recognizedState.Snapshot, replacement, investment,
                corridorContent, branchKnowledge, floorKnowledge, monsterContinuity: monsterContinuity);
        }

        private DetachedCanonicalSaveSessionResult PrepareReplacement(
            DetachedRecognizedSaveStateSnapshot recognizedState,
            DetachedCanonicalSpatialSaveState replacement, StructuralInvestmentRecord[] investment,
            CorridorContentAuthority corridorContent,
            SharedBranchKnowledgeAuthority branchKnowledge,
            SharedFloorKnowledgeAuthority floorKnowledge,
            LivingDungeonState livingDungeon = null, bool manualRun = false,
            MonsterContinuityState monsterContinuity = null, DurableSettlementHistory settlement = null, bool foregroundStage = false,
            LivingDungeonForegroundLineage.ReleasePermit releases = null)
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
                if (settlement != null && !settlement.Matches(currentBytes, livingDungeon, limits.Canonical.Serialized))
                    return Failure("living.settlement.history_invalid");
                var retainedOwner = primary.Fields[primary.Fields.Count - 2].Value;
                bool activated = retainedOwner.Fields[2].Value.Kind != ContractJsonKind.Null;
                bool unfinished = activated && retainedOwner.Fields[2].Value.Fields[2].Value.Text !=
                    retainedOwner.Fields[2].Value.Fields[3].Value.Text;
                if (unfinished && livingDungeon == null)
                {
                    int start = primary.Fields.Count - 9;
                    if (!SerializeNode(primary.Fields[start].Value).AsSpan().SequenceEqual(spatial.Value.Authority) ||
                        !SerializeNode(primary.Fields[start + 1].Value).AsSpan().SequenceEqual(spatial.Value.Floors) ||
                        !SerializeNode(primary.Fields[start + 2].Value).AsSpan().SequenceEqual(spatial.Value.LifecycleAndOwnership) ||
                        investment != null || corridorContent != null || branchKnowledge != null || floorKnowledge != null || monsterContinuity != null)
                        return Failure(LivingDungeonScheduling.Boundary);
                }
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
                    if (name == PhaseFiveSaveContracts.CorridorOwnerName ||
                        name == PhaseFiveSaveContracts.KnowledgeOwnerName ||
                        name == PhaseSixFloorKnowledge.OwnerName) continue;
                    byte[] valueBytes;
                    if (IsFrozenLegacySpatialMember(name) || recognizedState == null)
                    {
                        if (!TryFind(primary, name, out ContractJsonNode retained)) continue;
                        valueBytes = SerializeNode(retained);
                    }
                    else if (!recognizedState.TryGet(name, out valueBytes)) continue;
                    if (settlement != null && name != "runHistory")
                    {
                        if (TryFind(primary, name, out var settlementPrior))
                            valueBytes = settlement.MergeRecognized(name, settlementPrior, valueBytes, limits.Canonical.Serialized);
                        else if (name != "completedObjectives" || !settlement.HasObjective(valueBytes)) continue;
                    }
                    if (recognizedState != null && (activated || livingDungeon?.IsDormant == false) && name == "runHistory" && TryFind(primary, name, out var priorHistory))
                    {
                        var historyIssues = new SpatialIssueCollector(limits.Canonical.Serialized.MaximumDiagnostics);
                        if (!ContractJson.TryParse(valueBytes, limits.Canonical.Serialized, historyIssues, out var nextHistory))
                            return Failure(LivingDungeonScheduling.Invalid);
                        if (!LivingDungeonScheduling.HistorySequence(priorHistory, out int oldValue) ||
                            !LivingDungeonScheduling.HistorySequence(nextHistory, out int nextValue) ||
                            nextValue < oldValue) return Failure(LivingDungeonScheduling.Invalid);
                        if (manualRun && (oldValue == int.MaxValue || nextValue != checked(oldValue + 1)))
                            return Failure(LivingDungeonScheduling.Invalid);
                        if (settlement != null)
                        {
                            if (manualRun || nextValue != oldValue || !settlement.MatchesHistory(valueBytes))
                                return Failure("living.settlement.history_invalid");
                        }
                        else if (!manualRun)
                        {
                            var expected = JsonUtility.FromJson<RunHistoryState>(Encoding.UTF8.GetString(SerializeNode(priorHistory)));
                            expected.NextRunSequence = nextValue;
                            if ((livingDungeon == null && nextValue != oldValue) ||
                                JsonUtility.ToJson(expected) != Encoding.UTF8.GetString(valueBytes)) return Failure(LivingDungeonScheduling.Invalid);
                        }
                        // Scheduling changes only the allocator. Preserve the other historical fields, nulls and extensions.
                        valueBytes = settlement != null ? settlement.Merge(priorHistory, nextHistory, limits.Canonical.Serialized)
                            : MergeHistory(priorHistory, nextHistory, manualRun);
                    }
                    if (unfinished && livingDungeon == null && !LifecycleMember(name) &&
                        (!TryFind(primary, name, out var priorValue) || !SerializeNode(priorValue).AsSpan().SequenceEqual(valueBytes)))
                        return Failure(LivingDungeonScheduling.Boundary);
                    copiedBytes = Add(copiedBytes, valueBytes.Length,
                        limits.Whole.MaximumCopiedValueBytes);
                    WriteRawField(writer, name, valueBytes, first); first = false;
                }
                const int canonicalMemberCount = 9;
                int canonicalStart = primary.Fields.Count - canonicalMemberCount;
                for (int index = 0; index < canonicalStart; index++)
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
                WriteRawField(writer, "structuralLifecycleAndOwnership",
                    spatial.Value.LifecycleAndOwnership, false);
                writer.Token(",\"structuralInvestment\":");
                if (investment != null) StructuralInvestment.Write(writer, investment);
                else DetachedCompleteSaveContract.WriteCanonicalNode(writer, primary.Fields[canonicalStart + 3].Value);
                writer.Token(","); writer.String(PhaseFiveSaveContracts.CorridorOwnerName); writer.Token(":");
                if (corridorContent != null) PhaseFiveSaveContracts.Write(writer, corridorContent);
                else DetachedCompleteSaveContract.WriteCanonicalNode(writer, primary.Fields[canonicalStart + 4].Value);
                writer.Token(","); writer.String(PhaseFiveSaveContracts.KnowledgeOwnerName); writer.Token(":");
                if (branchKnowledge != null) PhaseFiveSaveContracts.Write(writer, branchKnowledge);
                else DetachedCompleteSaveContract.WriteCanonicalNode(writer, primary.Fields[canonicalStart + 5].Value);
                writer.Token(","); writer.String(PhaseSixFloorKnowledge.OwnerName); writer.Token(":");
                if (floorKnowledge != null) PhaseSixFloorKnowledge.Write(writer, floorKnowledge);
                else DetachedCompleteSaveContract.WriteCanonicalNode(writer, primary.Fields[canonicalStart + 6].Value);
                writer.Token(","); writer.String(LivingDungeonContracts.OwnerName); writer.Token(":");
                if (livingDungeon != null) LivingDungeonContracts.Write(writer, livingDungeon);
                else DetachedCompleteSaveContract.WriteCanonicalNode(writer, primary.Fields[canonicalStart + 7].Value);
                writer.Token(","); writer.String(MonsterContinuity.Owner); writer.Token(":");
                DetachedCompleteSaveValidationResult prior;
                using (ForegroundQualificationMetrics.Trace(ForegroundQualificationMetrics.Boundary.PriorValidation))
                    prior = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(currentBytes, context);
                if (!prior.IsValid) return Failure(MonsterRecoveryRules.Invalid);
                var continuity = monsterContinuity ?? MonsterContinuity.Reconcile(prior.MonsterContinuity, prior.State, replacement, context.MonsterRecovery);
                MonsterContinuity.Write(writer, continuity);
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
                if (!RawSavePayloadClassifier.TryValidateWorkload(candidate, limits.Raw, out string rawReason))
                    return Failure(rawReason);
                DetachedCompleteSaveValidationResult validated;
                using (ForegroundQualificationMetrics.Trace(ForegroundQualificationMetrics.Boundary.CandidateValidation))
                    validated = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(candidate, context);
                if (validated.IsValid && livingDungeon?.Scheduler?.ProfileId == LivingDungeonForegroundConfig.TwoFloorProfileId) {
                    using var releaseDiagnostic = ForegroundQualificationMetrics.Trace(ForegroundQualificationMetrics.Boundary.ReleaseCheck);
                    // The same immutable currentBytes/context were fully validated above.
                    // Reconcile and Write only read that detached result; keep the exact
                    // predecessor proof local to this preparation, never between saves.
                    var releasePredecessor = prior;
                    var profile = LivingDungeonContracts.Profile(livingDungeon, context.LivingDungeonProfiles);
                    foreach (var party in livingDungeon.Parties) {
                        var nextExecution = LivingDungeonContracts.Restore(party, profile);
                        try {
                            if (nextExecution.TransferGate?.ReleaseOrdinal >= 0) {
                                var old = releasePredecessor.LivingDungeon.Parties.SingleOrDefault(p => p.Sequence == party.Sequence);
                                bool alreadyReleased = false;
                                if (old != null) {
                                    var oldExecution = LivingDungeonContracts.Restore(old, profile);
                                    try {
                                        alreadyReleased = oldExecution.TransferGate?.ReleaseOrdinal >= 0;
                                        if (alreadyReleased && !RunSimulation.TransferCheckpointCodec.ControlBytes(oldExecution.TransferGate)
                                            .SequenceEqual(RunSimulation.TransferCheckpointCodec.ControlBytes(nextExecution.TransferGate)))
                                            return Failure("living.transfer.vacancy_unproven");
                                    }
                                    finally { oldExecution.Abandon(); }
                                }
                                if (!alreadyReleased && releases?.Allows(currentBytes, party) != true)
                                    return Failure("living.transfer.vacancy_unproven");
                            }
                        } finally { nextExecution.Abandon(); }
                    }
                }
                return validated.IsValid && validated.CurrentTargetValidated
                    ? new DetachedCanonicalSaveSessionResult(foregroundStage ? new DetachedCanonicalSaveSession(validated.GetBytes(), context, limits) : null,
                        new DetachedCanonicalSaveSessionUpdate(validated.GetBytes(), validated.State), null, foregroundStage ? validated : null)
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

        private byte[] MergeHistory(ContractJsonNode prior, ContractJsonNode next, bool manualRun)
        {
            bool Changed(string key) => key == "NextRunSequence" || (manualRun && (key == "LatestOutcome" || key == "RecentOutcomes"));
            var writer = new ContractJsonWriter(limits.Canonical.Serialized); writer.Node(); writer.Token("{"); bool first = true;
            foreach (var field in prior.Fields)
            {
                var value = Changed(field.Key) && TryFind(next, field.Key, out var replacement) ? replacement : field.Value;
                WriteField(writer, field.Key, value, first); first = false;
            }
            foreach (var field in next.Fields)
                if (Changed(field.Key) && !TryFind(prior, field.Key, out _))
                { WriteField(writer, field.Key, field.Value, first); first = false; }
            writer.Token("}"); return writer.Finish();
        }

        internal DetachedCanonicalSaveSessionResult PrepareLivingDungeonReplacement(
            DetachedRecognizedSaveStateSnapshotResult recognized, DetachedCanonicalSpatialSaveState spatial,
            LivingDungeonState livingDungeon) => recognized?.IsSuccess == true
                ? PrepareReplacement(recognized.Snapshot, spatial, null, null, null, null, livingDungeon)
                : Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);

        internal DetachedCanonicalSaveSessionResult PrepareManualRunReplacement(
            DetachedRecognizedSaveStateSnapshotResult recognized, DetachedCanonicalSpatialSaveState spatial,
            StructuralInvestmentRecord[] investment, CorridorContentAuthority corridor,
            SharedBranchKnowledgeAuthority branch, SharedFloorKnowledgeAuthority floor,
            MonsterContinuityState monsters = null) => recognized?.IsSuccess == true
                ? PrepareReplacement(recognized.Snapshot, spatial, investment, corridor, branch, floor, null, true, monsters)
                : Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);

#if UNITY_EDITOR
        internal DetachedCanonicalSaveSessionResult PrepareDurableSettlementReplacement(
            DetachedRecognizedSaveStateSnapshotResult recognized, DetachedCompleteSaveValidationResult owned,
            LivingDungeonState livingDungeon, SharedBranchKnowledgeAuthority branch,
            SharedFloorKnowledgeAuthority floor, DurableSettlementHistory settlement) =>
            recognized?.IsSuccess == true && settlement != null
                ? PrepareReplacement(recognized.Snapshot, owned.State, null, null, branch, floor,
                    livingDungeon, settlement: settlement)
                : Failure("living.settlement.history_invalid");
#endif

        internal DetachedCanonicalSaveSessionResult PrepareForegroundReplacement(
            DetachedRecognizedSaveStateSnapshotResult recognized, DetachedCompleteSaveValidationResult owned,
            LivingDungeonState livingDungeon, MonsterContinuityState monsters,
            SharedBranchKnowledgeAuthority branch = null, SharedFloorKnowledgeAuthority floor = null,
            DurableSettlementHistory settlement = null, LivingDungeonForegroundLineage.ReleasePermit releases = null) => recognized?.IsSuccess == true
                ? PrepareReplacement(recognized.Snapshot, owned.State, null, null, branch, floor,
                    livingDungeon, monsterContinuity: monsters, settlement: settlement, foregroundStage: true, releases: releases)
                : Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);

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
            !IsFrozenLegacySpatialMember(name) &&
            name != PhaseFiveSaveContracts.CorridorOwnerName &&
            name != PhaseFiveSaveContracts.KnowledgeOwnerName &&
            name != PhaseSixFloorKnowledge.OwnerName;

        private static bool IsFrozenLegacySpatialMember(string name) =>
            name == "mvpDungeonPlacements" || name == "mvpDungeonFloorLayout" ||
            name == "mvpRoomSlotAssignments";

        private static bool LifecycleMember(string name) => name == "lastSavedUtcUnix" ||
            name == "lastPausedUtcUnix" || name == "lastResumedUtcUnix" || name == "lastKnownAppState";

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
