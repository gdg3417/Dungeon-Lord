using System;
using System.Text;
using UnityEngine;

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

    /// <summary>Creates the first complete schema-8 payload without a legacy whole-save writer.</summary>
    public static class NativeCanonicalSaveCreator
    {
        public static NativeCanonicalSaveResult Create(string activePath,
            ISpatialMigrationFileSystem fileSystem, SaveData recognizedState,
            SpatialLayoutCompatibilitySnapshot compatibility, ProductionSpatialContentSnapshot production,
            byte[] legacyConfiguration, SaveSpatialMigrationLimitsProfile limits)
        {
            if (fileSystem == null || recognizedState == null || compatibility == null || production == null ||
                legacyConfiguration == null || limits == null || fileSystem.Exists(activePath))
                return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason);
            try
            {
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
                writer.Node(); writer.Token("{\"schema\":\"save_root\",\"schemaVersion\":8,\"primary\":{");
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
                    DetachedCompleteSaveContract.WriteCanonicalNode(writer, value);
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
                DetachedCompleteSaveContract.WriteCanonicalNode(writer, lifecycleNode); writer.Token("}}");
                byte[] bytes = writer.Finish();
                var context = new DetachedCurrentTargetValidationContext(compatibility, production,
                    legacyConfiguration, limits.Canonical);
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
                }
                catch
                {
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
