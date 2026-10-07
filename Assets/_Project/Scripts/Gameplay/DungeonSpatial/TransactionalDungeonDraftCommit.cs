using System;
using System.Linq;
using DungeonBuilder.M0.Economy;

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
            var draftContext = new DungeonDraftContext(production, configuration, context.RoomContentOccupancy, limits, compatibility);
            if (!draft.MatchesRuleContext(draftContext)) return Failure(TransactionalDungeonDraft.IncompatibleReason);
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
            if (!draft.PrepareCommit(owned.State, draftContext, out var recovered, out string reason)) return Failure(reason);
            var final = recovered.ReadModel;
            if (!DetachedCanonicalProductionSemanticValidation.Validate(final, production, configuration,
                    limits.Canonical.Spatial, context.RoomContentOccupancy, true).IsValid)
                return Failure(TransactionalDungeonDraft.InvalidReason);
            var priced = StructuralEconomyService.PreviewFinalMovement(owned.State, final, owned.Investment,
                recovered.NormalizedMovementTargets(), currentRuntime.structureRuntime.ManaReserve, economy, economyModifiers);
            if (!priced.IsAffordable) return Failure(priced.Reason);
            var snapshot = DetachedRecognizedSaveStateSnapshot.CaptureWithMana(currentRuntime, priced.ResultingMana, limits);
            if (!snapshot.IsSuccess) return Failure(snapshot.Reason);
            var prepared = session.PrepareLiveReplacement(snapshot, final, priced.Investment,
                owned.CorridorContent, owned.BranchKnowledge);
            var result = PrepareAndPersist(activePath, fileSystem, session, prepared, false);
            if (result.IsSuccess) draft.Committed();
            return result;
        }
    }
}
