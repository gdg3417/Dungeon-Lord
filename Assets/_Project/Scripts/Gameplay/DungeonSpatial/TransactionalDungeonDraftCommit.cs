using System;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed partial class DetachedCanonicalWriteAuthority
    {
        public DetachedCanonicalWriteResult CommitDungeonDraft(string activePath,
            ISpatialMigrationFileSystem fileSystem, DetachedCanonicalSaveSession session,
            SaveData currentRuntime, TransactionalDungeonDraft draft)
        {
            if (draft == null || currentRuntime?.structureRuntime == null || fileSystem == null)
                return Failure(TransactionalDungeonDraft.InvalidReason);
            var owned = ValidateSession(session);
            if (owned?.CurrentTargetValidated != true || !owned.IsValid ||
                !CanonicalEqual(currentRuntime.validatedCanonicalSpatialState, owned.State))
                return Failure(TransactionalDungeonDraft.StaleReason);
            // A changed session is never treated as an idempotent retry of an editor command.
            try
            {
                if (!session.GetCurrentBytes().SequenceEqual(fileSystem.ReadAllBytes(activePath)))
                    return Failure(TransactionalDungeonDraft.StaleReason);
            }
            catch { return Failure(AtomicSaveFailedReason); }
            if (!draft.PrepareCommit(owned.State, out var final, out string reason)) return Failure(reason);
            // Reconcile only the supported position command set. No structural pricing or acquisition.
            // Existing content identities, custody and structural investment are preserved by replay.
            // Capture current wallet/lifecycle/history, never their older values at draft creation.
            if (!DetachedCanonicalProductionSemanticValidation.Validate(final, production, configuration,
                    limits.Canonical.Spatial, context.RoomContentOccupancy, true).IsValid)
                return Failure(TransactionalDungeonDraft.InvalidReason);
            var snapshot = DetachedRecognizedSaveStateSnapshot.Capture(currentRuntime, limits);
            if (!snapshot.IsSuccess) return Failure(snapshot.Reason);
            var prepared = session.PrepareLiveReplacement(snapshot, final, owned.Investment,
                owned.CorridorContent, owned.BranchKnowledge);
            var result = PrepareAndPersist(activePath, fileSystem, session, prepared, false);
            if (result.IsSuccess) draft.Committed();
            return result;
        }
    }
}
