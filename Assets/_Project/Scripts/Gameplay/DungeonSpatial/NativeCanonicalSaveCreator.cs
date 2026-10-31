using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class NativeCanonicalSaveResult
    {
        internal NativeCanonicalSaveResult(string reason, DetachedCanonicalSaveSession session,
            DetachedCompleteSaveValidationResult validation, SaveData runtime)
        { Reason = reason; Session = session; Validation = validation; RuntimeProjection = runtime; }
        public string Reason { get; }
        public DetachedCanonicalSaveSession Session { get; }
        public DetachedCompleteSaveValidationResult Validation { get; }
        public SaveData RuntimeProjection { get; }
        public bool IsSuccess => Session != null && RuntimeProjection != null;
    }

    /// <summary>Creates a fresh complete canonical payload with configured starting mana.</summary>
    public static class NativeCanonicalSaveCreator
    {
        public static NativeCanonicalSaveResult Create(string activePath,
            ISpatialMigrationFileSystem fileSystem, SaveData recognizedState,
            SpatialLayoutCompatibilitySnapshot compatibility, ProductionSpatialContentSnapshot production,
            byte[] legacyConfiguration, SaveSpatialMigrationLimitsProfile limits,
            ContentAcquisitionEconomySnapshot acquisition,
            RoomContentSpatialOccupancySnapshot roomContentOccupancy = null,
            MonsterRecoveryRules monsterRecovery = null) => CreateCore(activePath, fileSystem, recognizedState,
                compatibility, production, legacyConfiguration, limits, acquisition, roomContentOccupancy,
                monsterRecovery, null, null, null);

        internal static NativeCanonicalSaveResult CreatePrecise(string activePath,
            ISpatialMigrationFileSystem fileSystem, SaveData recognizedState,
            ProductionSpatialContentSnapshot production, SaveSpatialMigrationLimitsProfile limits,
            ContentAcquisitionEconomySnapshot acquisition, RecoveryRuntimeInputs inputs, UnixTimePoint creation,
            Action<DetachedCanonicalSaveSession, TimeProvenance> candidatePrepared = null) =>
            inputs == null ? Failure(RecoveryEpisode.ContextMismatch) : CreateCore(activePath, fileSystem, recognizedState,
                inputs.Compatibility, production, LegacyGameplayConfigurationContract.SerializeCanonical(inputs.Configuration),
                limits, acquisition, inputs.Occupancy, inputs.Monsters, inputs, creation, candidatePrepared);

        private static NativeCanonicalSaveResult CreateCore(string activePath,
            ISpatialMigrationFileSystem fileSystem, SaveData recognizedState,
            SpatialLayoutCompatibilitySnapshot compatibility, ProductionSpatialContentSnapshot production,
            byte[] legacyConfiguration, SaveSpatialMigrationLimitsProfile limits,
            ContentAcquisitionEconomySnapshot acquisition, RoomContentSpatialOccupancySnapshot roomContentOccupancy,
            MonsterRecoveryRules monsterRecovery, RecoveryRuntimeInputs recoveryInputs, UnixTimePoint creation,
            Action<DetachedCanonicalSaveSession, TimeProvenance> candidatePrepared)
        {
            if (fileSystem == null || recognizedState == null || compatibility == null || production == null ||
                legacyConfiguration == null || limits == null || fileSystem.Exists(activePath))
                return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
            if (roomContentOccupancy == null)
                return Failure(RoomContentSpatialOccupancyAuthority.InvalidReason);
            if (acquisition == null) return Failure(ContentAcquisitionEconomySnapshot.InvalidReason);
            bool precise = CanonicalSaveSchemaVersions.CurrentWritableTarget == CanonicalSaveSchemaVersions.AtomicRecoveryIntroduction;
            if (precise && (recoveryInputs == null || creation == null || creation.Seconds <= 0) ||
                !precise && recoveryInputs != null) return Failure(RecoveryEpisode.Invalid);
            try
            {
                // Detached initialization only: loading or migrating existing saves never enters here.
                bool pendingAbsent = recognizedState.researchPending == null;
                bool progressAbsent = recognizedState.researchProgress == null;
                bool summaryAbsent = recognizedState.lastOfflineSummary == null;
                recognizedState = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(recognizedState));
                if (precise) {
                    recognizedState.lastSavedUtcUnix = creation.Seconds;
                    if (pendingAbsent) recognizedState.researchPending = null;
                    if (progressAbsent) recognizedState.researchProgress = null;
                    if (summaryAbsent) recognizedState.lastOfflineSummary = null;
                }
                if (recognizedState.structureRuntime == null) recognizedState.structureRuntime = new StructureRuntimeState();
                recognizedState.structureRuntime.ManaReserve = acquisition.StartingMana;
                CompatibilitySelectionResult<CanonicalLayoutContractSelection> selected =
                    compatibility.SelectContract(CanonicalSaveSchemaVersions.CurrentWritableTarget);
                if (!selected.Success) return Failure(selected.Code);
                var state = new DetachedCanonicalSpatialSaveState
                {
                    Authority = new CanonicalSpatialAuthorityMarker
                    {
                        CanonicalLayoutContractVersion = selected.Value.CanonicalLayoutContractVersion,
                        CreationKind = CanonicalSpatialCreationKind.NativeCanonical,
                        MigrationTransactionId = null,
                        MigrationDescriptorFingerprint = null
                    },
                    Floors = Array.Empty<SavedSpatialFloor>(),
                    LifecycleAndOwnership = NativeStructuralIdentity.CreateInitialLifecycle(
                        Array.Empty<SavedSpatialFloor>())
                };
                SpatialContractResult<CanonicalSpatialSaveSerializer.SerializedMembers> spatial =
                    CanonicalSpatialSaveSerializer.SerializeMembers(state, limits.Canonical);
                if (!spatial.IsValid) return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
                byte[] sourceBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(recognizedState));
                var issues = new SpatialIssueCollector(limits.Canonical.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(sourceBytes, limits.Canonical.Serialized, issues,
                    out ContractJsonNode source) || source.Kind != ContractJsonKind.Object)
                    return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
                var writer = new ContractJsonWriter(limits.Canonical.Serialized);
                writer.Node(); writer.Token("{\"schema\":\"save_root\",\"schemaVersion\":");
                writer.Token(CanonicalSaveSchemaVersions.CurrentWritableTarget.ToString(CultureInfo.InvariantCulture));
                writer.Token(",\"primary\":{");
                bool first = true;
                foreach (string name in RawSavePayloadClassifier.RecognizedSaveDataMemberNames)
                {
                    if (!DetachedCanonicalSaveSession.IsLiveRecognizedMember(name)) continue;
                    ContractJsonNode value = null;
                    foreach (var field in source.Fields)
                        if (field.Key == name) { value = field.Value; break; }
                    if (value == null) continue;
                    if (!first) writer.Token(","); first = false;
                    writer.String(name); writer.Token(":");
                    // Preserve the caller's actual absence in a new schema-17 save.
                    // This neither changes frozen native bytes nor repairs a saved shell.
                    if (precise && (name == "researchPending" && pendingAbsent ||
                        name == "researchProgress" && progressAbsent || name == "lastOfflineSummary" && summaryAbsent))
                    { writer.Node(); writer.Token("null"); }
                    else DetachedCompleteSaveContract.WriteCanonicalNode(writer, value);
                }
                if (!first) writer.Token(",");
                ContractJsonNode authorityNode = Parse(spatial.Value.Authority, limits);
                ContractJsonNode floorsNode = Parse(spatial.Value.Floors, limits);
                ContractJsonNode lifecycleNode = Parse(spatial.Value.LifecycleAndOwnership, limits);
                if (authorityNode == null || floorsNode == null || lifecycleNode == null)
                    return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
                writer.String("canonicalSpatialAuthority"); writer.Token(":");
                DetachedCompleteSaveContract.WriteCanonicalNode(writer, authorityNode);
                writer.Token(","); writer.String("spatialFloors"); writer.Token(":");
                DetachedCompleteSaveContract.WriteCanonicalNode(writer, floorsNode);
                writer.Token(","); writer.String("structuralLifecycleAndOwnership"); writer.Token(":");
                DetachedCompleteSaveContract.WriteCanonicalNode(writer, lifecycleNode);
                writer.Token(",\"structuralInvestment\":[]");
                writer.Token(","); writer.String(PhaseFiveSaveContracts.CorridorOwnerName); writer.Token(":");
                PhaseFiveSaveContracts.Write(writer, PhaseFiveSaveContracts.EmptyCorridor());
                writer.Token(","); writer.String(PhaseFiveSaveContracts.KnowledgeOwnerName); writer.Token(":");
                PhaseFiveSaveContracts.Write(writer, PhaseFiveSaveContracts.EmptyKnowledge());
                writer.Token(","); writer.String(PhaseSixFloorKnowledge.OwnerName); writer.Token(":");
                PhaseSixFloorKnowledge.Write(writer, PhaseSixFloorKnowledge.Empty());
                writer.Token(","); writer.String(LivingDungeonContracts.OwnerName); writer.Token(":");
                LivingDungeonContracts.Write(writer, LivingDungeonState.Empty());
                writer.Token(","); writer.String(MonsterContinuity.Owner); writer.Token(":");
                MonsterContinuity.Write(writer, MonsterContinuityState.Empty());
                writer.Token(","); writer.String(TimeProvenance.Owner); writer.Token(":");
                if (precise) new TimeProvenance(creation, recoveryInputs.Identity.Digest, null).Write(writer);
                else TimeProvenance.WriteLegacy(writer);
                writer.Token("}}");
                byte[] bytes = writer.Finish();
                var context = new DetachedCurrentTargetValidationContext(compatibility, production,
                    legacyConfiguration, limits.Canonical, roomContentOccupancy, monsterRecovery: monsterRecovery);
                if (precise) context.BindRecoveryInputs(recoveryInputs);
                DetachedCompleteSaveValidationResult validation =
                    DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, context);
                DetachedCanonicalSaveSessionResult opened = validation.IsValid && validation.CurrentTargetValidated
                    ? DetachedCanonicalSaveSession.Open(bytes, context, limits) : null;
                if (opened == null || !opened.IsSuccess)
                    return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
                if (!DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.CanonicalMvpRouteProjection.
                    TryPublishValidated(validation, production, out SaveData runtime,
                        out string publishReason))
                    return Failure(publishReason ??
                        DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
                string directory = System.IO.Path.GetDirectoryName(activePath);
                if (!fileSystem.IsPathContainedWithoutRedirection(directory, activePath))
                    return Failure(SpatialMigrationCapabilityReason.PathRedirected);
                string hash = SpatialContractSha256.Compute(bytes);
                string staging = activePath + ".canonical-write-0000000000000000-" +
                    hash.Substring(0, 16) + ".candidate";
                if (!fileSystem.IsPathContainedWithoutRedirection(directory, staging))
                    return Failure(SpatialMigrationCapabilityReason.PathRedirected);
                candidatePrepared?.Invoke(opened.Session, validation.TimeProvenance);
                try
                {
                    fileSystem.WriteAllBytesDurable(staging, bytes);
                    byte[] staged = fileSystem.ReadAllBytes(staging);
                    if (staged == null || SpatialContractSha256.Compute(staged) != hash)
                        throw new InvalidOperationException();
                    fileSystem.MoveSameDirectoryAtomic(staging, activePath);
                    fileSystem.FlushDirectory(directory);
                    byte[] readback = fileSystem.ReadAllBytes(activePath);
                    if (readback == null || SpatialContractSha256.Compute(readback) != hash)
                        throw new InvalidOperationException();
                    if (precise) {
                        var durableValidation = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(readback, context);
                        var durableSession = DetachedCanonicalSaveSession.Open(readback, context, limits);
                        if (!durableValidation.CurrentTargetValidated || !durableSession.IsSuccess ||
                            !DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.CanonicalMvpRouteProjection.TryPublishValidated(
                                durableValidation, production, out var durableRuntime, out _)) throw new InvalidOperationException();
                        return new NativeCanonicalSaveResult(null, durableSession.Session, durableValidation, durableRuntime);
                    }
                }
                catch
                {
                    // A failed successor flush/readback does not establish Q.
                    // Retain its exact candidate/evidence for independent proof.
                    // The frozen legacy creator's behavior remains below.
                    if (precise) return Failure(DetachedCanonicalWriteAuthority.RecoveryRequiredReason);
                    try { if (fileSystem.Exists(staging)) fileSystem.DeleteFile(staging); } catch { }
                    try
                    {
                        if (fileSystem.Exists(activePath) && SpatialContractSha256.Compute(
                            fileSystem.ReadAllBytes(activePath)) == hash)
                            return new NativeCanonicalSaveResult(null, opened.Session, validation, runtime);
                    }
                    catch { }
                    return Failure(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason);
                }
                return new NativeCanonicalSaveResult(null, opened.Session, validation, runtime);
            }
            catch { return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason); }
        }

        private static NativeCanonicalSaveResult Failure(string reason) =>
            new NativeCanonicalSaveResult(reason, null, null, null);

        private static ContractJsonNode Parse(byte[] bytes, SaveSpatialMigrationLimitsProfile limits)
        {
            var issues = new SpatialIssueCollector(limits.Canonical.Serialized.MaximumDiagnostics);
            return ContractJson.TryParse(bytes, limits.Canonical.Serialized, issues, out ContractJsonNode node)
                ? node : null;
        }
    }
}
