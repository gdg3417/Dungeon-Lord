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
    /// Complete-save writer. It prepares detached state, atomically persists exact session
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
            string persistenceReason = ExactCompleteSaveAtomicPersistence.Persist(activePath, fileSystem, session.GetCurrentBytes(), candidate,
                limits.Canonical.Serialized.MaximumCollectionRecords);
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
            string persistenceReason = ExactCompleteSaveAtomicPersistence.Persist(activePath, fileSystem, session.GetCurrentBytes(), candidate,
                limits.Canonical.Serialized.MaximumCollectionRecords);
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
            return a.IsValid && b.IsValid &&
                ExactCompleteSaveAtomicPersistence.Same(a.Value, b.Value);
        }

        private static DetachedCanonicalWriteResult Failure(string reason) =>
            new DetachedCanonicalWriteResult(false, reason, false, false, null, null, null, null);
    }
}
