using System;
using System.IO;
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
            if (fileSystem == null || session == null || currentState == null || currentRuntime == null ||
                context == null || limits == null || production == null || compatibility == null ||
                configuration == null) return Failure(DetachedCanonicalSpatialMutation.ValidationFailedReason);
            DetachedCanonicalMutationResult mutation = DetachedCanonicalSpatialMutation.Prepare(currentState,
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
            if (!Persist(activePath, fileSystem, session.GetCurrentBytes(), candidate))
                return Failure(AtomicSaveFailedReason);
            DetachedCanonicalSaveSessionResult reopened =
                DetachedCanonicalSaveSession.Open(candidate, context, limits);
            if (!reopened.IsSuccess || !CanonicalMvpRouteProjection.TryPublishValidated(validated,
                production, out SaveData runtime, out string reason))
                return Failure(reason ?? AtomicSaveFailedReason);
            return new DetachedCanonicalWriteResult(true, null, false,
                mutation.ApplyExplicitRoomEffect, candidate, reopened.Session, validated, runtime);
        }

        private static bool Persist(string activePath, ISpatialMigrationFileSystem fileSystem,
            byte[] original, byte[] candidate)
        {
            if (string.IsNullOrEmpty(activePath) || original == null || candidate == null) return false;
            string directory;
            try
            {
                string normalized = Path.GetFullPath(activePath);
                directory = Path.GetDirectoryName(normalized);
                if (normalized != activePath || !fileSystem.Exists(activePath) ||
                    !Same(fileSystem.ReadAllBytes(activePath), original)) return false;
                string token = SpatialContractSha256.Compute(original).Substring(0, 16) + "-" +
                    SpatialContractSha256.Compute(candidate).Substring(0, 16);
                string rollback = Path.Combine(directory, Path.GetFileName(activePath) +
                    ".canonical-write-" + token + ".rollback");
                string staging = Path.Combine(directory, Path.GetFileName(activePath) +
                    ".canonical-write-" + token + ".candidate");
                if (fileSystem.Exists(rollback) || fileSystem.Exists(staging) ||
                    !fileSystem.IsPathContainedWithoutRedirection(directory, rollback) ||
                    !fileSystem.IsPathContainedWithoutRedirection(directory, staging)) return false;
                fileSystem.WriteAllBytesDurable(rollback, original);
                if (!Same(fileSystem.ReadAllBytes(rollback), original)) return false;
                fileSystem.WriteAllBytesDurable(staging, candidate);
                if (!Same(fileSystem.ReadAllBytes(staging), candidate)) return false;
                try
                {
                    fileSystem.ReplaceSameDirectoryAtomic(staging, activePath);
                    fileSystem.FlushDirectory(directory);
                    if (!Same(fileSystem.ReadAllBytes(activePath), candidate))
                        throw new IOException();
                    return true;
                }
                catch
                {
                    Restore(fileSystem, rollback, activePath, directory, original);
                    return false;
                }
            }
            catch { return false; }
        }

        private static void Restore(ISpatialMigrationFileSystem fileSystem, string rollback,
            string activePath, string directory, byte[] original)
        {
            try
            {
                if (fileSystem.Exists(rollback))
                {
                    fileSystem.ReplaceSameDirectoryAtomic(rollback, activePath);
                    fileSystem.FlushDirectory(directory);
                    fileSystem.ReadAllBytes(activePath);
                }
            }
            catch { }
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
