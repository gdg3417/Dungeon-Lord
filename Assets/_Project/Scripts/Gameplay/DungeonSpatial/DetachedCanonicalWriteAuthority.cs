using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class DetachedCanonicalWriteResult
    {
        private readonly byte[] persistedBytes;
        internal DetachedCanonicalWriteResult(bool success, string reason, bool noOp, bool roomEffect,
            byte[] bytes, DetachedCanonicalSaveSession session,
            DetachedCompleteSaveValidationResult validation, SaveData runtime)
        {
            IsSuccess = success; Reason = reason; IsNoOp = noOp;
            ApplyExplicitRoomEffect = roomEffect;
            persistedBytes = bytes == null ? null : (byte[])bytes.Clone();
            Session = session; Validation = validation; RuntimeProjection = runtime;
        }
        public bool IsSuccess { get; }
        public string Reason { get; }
        public bool IsNoOp { get; }
        public bool ApplyExplicitRoomEffect { get; }
        public DetachedCanonicalSaveSession Session { get; }
        public DetachedCompleteSaveValidationResult Validation { get; }
        public SaveData RuntimeProjection { get; }
        public byte[] GetPersistedBytes() => persistedBytes == null ? null : (byte[])persistedBytes.Clone();
    }

    /// <summary>
    /// Inactive complete-save writer. It prepares detached state, atomically persists exact session
    /// bytes, verifies durable readback, and only then creates a new runtime projection.
    /// </summary>
    public sealed class DetachedCanonicalWriteAuthority
    {
        public const string AtomicSaveFailedReason = "gd66.write.atomic_save_failed";
        public const string RecoveryRequiredReason = "gd66.transaction.recovery_failed";
        private readonly ProductionSpatialContentSnapshot production;
        private readonly SpatialLayoutCompatibilitySnapshot compatibility;
        private readonly RunSimulationConfig configuration;
        private readonly DetachedCurrentTargetValidationContext context;
        private readonly SaveSpatialMigrationLimitsProfile limits;

        public DetachedCanonicalWriteAuthority(ProductionSpatialContentSnapshot production,
            SpatialLayoutCompatibilitySnapshot compatibility, RunSimulationConfig configuration,
            DetachedCurrentTargetValidationContext context, SaveSpatialMigrationLimitsProfile limits)
        {
            this.production = production; this.compatibility = compatibility;
            this.configuration = configuration; this.context = context; this.limits = limits;
        }

        public DetachedCanonicalWriteResult Execute(string activePath,
            ISpatialMigrationFileSystem fileSystem, DetachedCanonicalSaveSession session,
            DetachedCanonicalSpatialSaveState currentState, SaveData currentRuntime,
            DetachedCanonicalMutationRequest request)
        {
            if (currentState == null) return Failure(DetachedCanonicalSpatialMutation.ValidationFailedReason);
            DetachedCompleteSaveValidationResult owned = ValidateSession(session);
            if (owned == null || !owned.IsValid || !CanonicalEqual(currentState, owned.State))
                return Failure(DetachedCanonicalSpatialMutation.ValidationFailedReason);
            return Execute(activePath, fileSystem, session, currentRuntime, request);
        }

        public DetachedCanonicalWriteResult Execute(string activePath,
            ISpatialMigrationFileSystem fileSystem, DetachedCanonicalSaveSession session,
            SaveData currentRuntime, DetachedCanonicalMutationRequest request)
        {
            if (fileSystem == null || session == null || currentRuntime == null ||
                context == null || limits == null || production == null || compatibility == null ||
                configuration == null) return Failure(DetachedCanonicalSpatialMutation.ValidationFailedReason);
            DetachedCompleteSaveValidationResult owned = ValidateSession(session);
            if (owned == null || !owned.IsValid || !owned.CurrentTargetValidated)
                return Failure(DetachedCanonicalSpatialMutation.ValidationFailedReason);
            DetachedCanonicalMutationResult mutation = DetachedCanonicalSpatialMutation.Prepare(owned.State,
                request, production, compatibility, configuration, limits.Canonical);
            if (mutation.IsNoOp) return new DetachedCanonicalWriteResult(false, mutation.Reason, true,
                false, null, null, null, null);
            if (!mutation.IsSuccess) return Failure(mutation.Reason);
            DetachedRecognizedSaveStateSnapshotResult snapshot =
                DetachedRecognizedSaveStateSnapshot.Capture(currentRuntime, limits);
            if (!snapshot.IsSuccess) return Failure(snapshot.Reason);
            DetachedCanonicalSaveSessionResult prepared =
                session.PrepareLiveReplacement(snapshot, mutation.State);
            if (!prepared.IsSuccess || prepared.Update == null) return Failure(prepared.Reason ??
                DetachedCanonicalSpatialMutation.ValidationFailedReason);
            byte[] candidate = prepared.Update.GetBytes();
            DetachedCompleteSaveValidationResult validated =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(candidate, context);
            if (!validated.IsValid || !validated.CurrentTargetValidated)
                return Failure(DetachedCanonicalSpatialMutation.ValidationFailedReason);
            DetachedCanonicalSaveSessionResult reopened =
                DetachedCanonicalSaveSession.Open(candidate, context, limits);
            if (!reopened.IsSuccess || !CanonicalMvpRouteProjection.TryPublishValidated(validated,
                production, out SaveData runtime, out string reason))
                return Failure(reason ?? DetachedCanonicalSpatialMutation.ValidationFailedReason);
            string persistenceReason = Persist(activePath, fileSystem, session.GetCurrentBytes(), candidate,
                limits.Whole.MaximumUnknownMembers, limits.Raw.MaximumInputBytes);
            if (persistenceReason != null) return Failure(persistenceReason);
            return new DetachedCanonicalWriteResult(true, null, false,
                mutation.ApplyExplicitRoomEffect, candidate, reopened.Session, validated, runtime);
        }

        public DetachedCanonicalWriteResult SaveRecognizedState(string activePath,
            ISpatialMigrationFileSystem fileSystem, DetachedCanonicalSaveSession session,
            SaveData currentRuntime)
        {
            if (fileSystem == null || session == null || currentRuntime == null || context == null ||
                limits == null || production == null || compatibility == null || configuration == null)
                return Failure(DetachedCanonicalSpatialMutation.ValidationFailedReason);
            DetachedCompleteSaveValidationResult owned = ValidateSession(session);
            if (owned == null || !owned.IsValid || !owned.CurrentTargetValidated || currentRuntime == null)
                return Failure(DetachedCanonicalSpatialMutation.ValidationFailedReason);
            DetachedRecognizedSaveStateSnapshotResult snapshot =
                DetachedRecognizedSaveStateSnapshot.Capture(currentRuntime, limits);
            if (!snapshot.IsSuccess) return Failure(snapshot.Reason);
            DetachedCanonicalSaveSessionResult prepared =
                session.PrepareLiveReplacement(snapshot, owned.State);
            return PrepareAndPersist(activePath, fileSystem, session, prepared, false);
        }

        private DetachedCanonicalWriteResult PrepareAndPersist(string activePath,
            ISpatialMigrationFileSystem fileSystem, DetachedCanonicalSaveSession session,
            DetachedCanonicalSaveSessionResult prepared, bool roomEffect)
        {
            if (prepared == null || !prepared.IsSuccess || prepared.Update == null)
                return Failure(prepared?.Reason ?? DetachedCanonicalSpatialMutation.ValidationFailedReason);
            byte[] candidate = prepared.Update.GetBytes();
            DetachedCompleteSaveValidationResult validated =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(candidate, context);
            DetachedCanonicalSaveSessionResult reopened = validated.IsValid
                ? DetachedCanonicalSaveSession.Open(candidate, context, limits) : null;
            if (!validated.IsValid || !validated.CurrentTargetValidated || reopened == null ||
                !reopened.IsSuccess || !CanonicalMvpRouteProjection.TryPublishValidated(validated,
                    production, out SaveData runtime, out string reason))
                return Failure(reason ?? DetachedCanonicalSpatialMutation.ValidationFailedReason);
            string persistenceReason = Persist(activePath, fileSystem, session.GetCurrentBytes(), candidate,
                limits.Whole.MaximumUnknownMembers, limits.Raw.MaximumInputBytes);
            return persistenceReason == null
                ? new DetachedCanonicalWriteResult(true, null, false, roomEffect, candidate,
                    reopened.Session, validated, runtime)
                : Failure(persistenceReason);
        }

        private DetachedCompleteSaveValidationResult ValidateSession(DetachedCanonicalSaveSession session) =>
            session == null ? null : DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                session.GetCurrentBytes(), context);

        private bool CanonicalEqual(DetachedCanonicalSpatialSaveState left,
            DetachedCanonicalSpatialSaveState right)
        {
            SpatialContractResult<byte[]> a = CanonicalSpatialSaveSerializer.Serialize(left, limits.Canonical);
            SpatialContractResult<byte[]> b = CanonicalSpatialSaveSerializer.Serialize(right, limits.Canonical);
            return a.IsValid && b.IsValid && Same(a.Value, b.Value);
        }

        private static string Persist(string activePath, ISpatialMigrationFileSystem fileSystem,
            byte[] original, byte[] candidate, int maximumEvidence, int maximumEvidenceBytes)
        {
            if (string.IsNullOrEmpty(activePath) || original == null || candidate == null)
                return AtomicSaveFailedReason;
            string directory;
            try
            {
                string normalized = Path.GetFullPath(activePath);
                directory = Path.GetDirectoryName(normalized);
                if (normalized != activePath || !fileSystem.Exists(activePath)) return AtomicSaveFailedReason;
                byte[] activeBefore = fileSystem.ReadAllBytes(activePath);
                if (!Same(activeBefore, original) && !Same(activeBefore, candidate))
                    return RecoveryRequiredReason;
                string evidenceReason;
                try { evidenceReason = SettleAllEvidence(fileSystem, activePath, directory,
                    activeBefore, maximumEvidence, maximumEvidenceBytes); }
                catch { return RecoveryRequiredReason; }
                if (evidenceReason != null) return evidenceReason;
                string token = SpatialContractSha256.Compute(original).Substring(0, 16) + "-" +
                    SpatialContractSha256.Compute(candidate).Substring(0, 16);
                string rollback = Path.Combine(directory, Path.GetFileName(activePath) +
                    ".canonical-write-" + token + ".rollback");
                string staging = Path.Combine(directory, Path.GetFileName(activePath) +
                    ".canonical-write-" + token + ".candidate");
                if (!fileSystem.IsPathContainedWithoutRedirection(directory, rollback) ||
                    !fileSystem.IsPathContainedWithoutRedirection(directory, staging)) return AtomicSaveFailedReason;
                if (Same(activeBefore, candidate)) return null;
                if (!Same(activeBefore, original)) return RecoveryRequiredReason;
                fileSystem.WriteAllBytesDurable(rollback, original);
                if (!Same(fileSystem.ReadAllBytes(rollback), original)) return RecoveryRequiredReason;
                fileSystem.WriteAllBytesDurable(staging, candidate);
                if (!Same(fileSystem.ReadAllBytes(staging), candidate)) return RecoveryRequiredReason;
                try
                {
                    fileSystem.ReplaceSameDirectoryAtomic(staging, activePath);
                    fileSystem.FlushDirectory(directory);
                    if (!Same(fileSystem.ReadAllBytes(activePath), candidate))
                        throw new IOException();
                }
                catch
                {
                    return Restore(fileSystem, rollback, activePath, directory, original)
                        ? AtomicSaveFailedReason : RecoveryRequiredReason;
                }
                try
                {
                    fileSystem.DeleteFile(rollback);
                    fileSystem.FlushDirectory(directory);
                    return null;
                }
                catch { return RecoveryRequiredReason; }
            }
            catch
            {
                try
                {
                    byte[] active = fileSystem.ReadAllBytes(activePath);
                    return Same(active, original) ? AtomicSaveFailedReason : RecoveryRequiredReason;
                }
                catch { return RecoveryRequiredReason; }
            }
        }

        private static string SettleAllEvidence(ISpatialMigrationFileSystem fileSystem,
            string activePath, string directory, byte[] validatedActive, int maximumEvidence,
            int maximumEvidenceBytes)
        {
            if (maximumEvidence <= 0 || maximumEvidenceBytes <= 0) return RecoveryRequiredReason;
            string activeName = Path.GetFileName(activePath);
            string prefix = activeName + ".canonical-write-";
            IReadOnlyList<string> discovered = fileSystem.EnumerateFiles(directory, prefix + "*",
                maximumEvidence);
            string[] evidence = discovered.Where(path => IsEvidenceName(Path.GetFileName(path), prefix))
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
            foreach (string path in evidence)
            {
                if (!fileSystem.IsPathContainedWithoutRedirection(directory, path))
                    return RecoveryRequiredReason;
                byte[] evidenceBytes = fileSystem.ReadAllBytes(path);
                if (evidenceBytes == null || evidenceBytes.Length > maximumEvidenceBytes)
                    return RecoveryRequiredReason;
                // The caller has already contextually validated and byte-matched the active complete
                // save. Thus prior ordinary-write sidecars are obsolete, including partial writes.
                // Never overwrite or interpret their payload; retire only their strictly-owned names.
                fileSystem.DeleteFile(path);
            }
            if (evidence.Length != 0) fileSystem.FlushDirectory(directory);
            if (!Same(fileSystem.ReadAllBytes(activePath), validatedActive)) return RecoveryRequiredReason;
            return null;
        }

        private static bool IsEvidenceName(string name, string prefix)
        {
            if (name == null || !name.StartsWith(prefix, StringComparison.Ordinal)) return false;
            string remainder = name.Substring(prefix.Length);
            string suffix = remainder.EndsWith(".rollback", StringComparison.Ordinal) ? ".rollback" :
                remainder.EndsWith(".candidate", StringComparison.Ordinal) ? ".candidate" : null;
            if (suffix == null) return false;
            string token = remainder.Substring(0, remainder.Length - suffix.Length);
            if (token.Length != 33 || token[16] != '-') return false;
            for (int index = 0; index < token.Length; index++)
                if (index != 16 && !((token[index] >= '0' && token[index] <= '9') ||
                    (token[index] >= 'a' && token[index] <= 'f'))) return false;
            return true;
        }

        private static bool Restore(ISpatialMigrationFileSystem fileSystem, string rollback,
            string activePath, string directory, byte[] original)
        {
            try
            {
                if (fileSystem.Exists(rollback))
                {
                    fileSystem.ReplaceSameDirectoryAtomic(rollback, activePath);
                    fileSystem.FlushDirectory(directory);
                    return Same(fileSystem.ReadAllBytes(activePath), original);
                }
            }
            catch { }
            return false;
        }

        private static bool Same(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int index = 0; index < left.Length; index++) if (left[index] != right[index]) return false;
            return true;
        }

        private static DetachedCanonicalWriteResult Failure(string reason) =>
            new DetachedCanonicalWriteResult(false, reason, false, false, null, null, null, null);
    }
}
