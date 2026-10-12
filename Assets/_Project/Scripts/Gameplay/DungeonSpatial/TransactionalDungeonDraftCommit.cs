using System;
using System.Linq;
using DungeonBuilder.M0.Economy;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed partial class DetachedCanonicalWriteAuthority
    {
        public DetachedCanonicalWriteResult CommitDungeonDraft(string activePath,
            ISpatialMigrationFileSystem fileSystem, DetachedCanonicalSaveSession session,
            SaveData currentRuntime, TransactionalDungeonDraft draft, GateEvaluationInput? purchaseInput = null)
        {
            if (draft == null || currentRuntime?.structureRuntime == null || fileSystem == null)
                return Failure(TransactionalDungeonDraft.InvalidReason);
            var draftContext = new DungeonDraftContext(production, configuration, context.RoomContentOccupancy, limits, compatibility, removalPolicy);
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
            var priced = StructuralEconomyService.PreviewFinalContentDraft(owned.State, final, owned.Investment,
                recovered.NormalizedMovementTargets(), currentRuntime.structureRuntime.ManaReserve, economy, acquisition, economyModifiers);
            if (currentRuntime.structureRuntime.PlacementLocked) return Failure("ui.banner.place_blocked_heat_crisis");
            if (priced.ContentAcquisitions > 0)
            {
                if (!purchaseInput.HasValue || purchaseInput.Value.ActionType != RestrictedActionType.Purchase)
                    return Failure("gate.error.offline_required");
                var gate = new RestrictedActionGateService().Evaluate(purchaseInput.Value);
                if (!gate.Allowed) return Failure(gate.MessageKey);
            }
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
