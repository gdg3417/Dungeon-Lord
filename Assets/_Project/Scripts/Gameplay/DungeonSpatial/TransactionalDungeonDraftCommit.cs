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
            if (!recovered.VerifyInsertionRetirements(owned)) return Failure(StructuralRouteInsertionService.RetirementUnavailable);
            var final = recovered.ReadModel;
            if (!DetachedCanonicalProductionSemanticValidation.Validate(final, production, configuration,
                    limits.Canonical.Spatial, context.RoomContentOccupancy, true).IsValid)
                return Failure(TransactionalDungeonDraft.InvalidReason);
            var corridor = owned.CorridorContent;
            var knowledge = owned.BranchKnowledge;
            var returned = final.LifecycleAndOwnership.ReturnedContents;
            if (recovered.HasRouteRenovation)
            {
                string branchReason = CorridorRenovationConsequences.BranchEntitlement(owned.State, final, production,
                    currentRuntime.completedResearch, branchingResearch);
                if (branchReason != null) return Failure(branchReason);
                var projection = CorridorRenovationConsequences.Project(owned, final, recovered.CorridorDispositions(),
                    production, configuration, removalPolicy, limits.Canonical.Spatial);
                if (projection.Reason != null) return Failure(projection.Reason);
                corridor = projection.Corridor;
                knowledge = projection.Knowledge;
                returned = projection.Returned;
            }
            var priced = StructuralEconomyService.PreviewFinalContentDraft(owned.State, final, owned.Investment,
                recovered.FinalEconomicMovementTargets(final), currentRuntime.structureRuntime.ManaReserve, economy, acquisition,
                economyModifiers, recovered.HasRouteRenovation ? recovered.ExplicitStructuralRetirements : null);
            if (currentRuntime.structureRuntime.PlacementLocked) return Failure("ui.banner.place_blocked_heat_crisis");
            if (priced.ContentAcquisitions > 0)
            {
                if (!purchaseInput.HasValue || purchaseInput.Value.ActionType != RestrictedActionType.Purchase)
                    return Failure("gate.error.offline_required");
                var gate = new RestrictedActionGateService().Evaluate(purchaseInput.Value);
                if (!gate.Allowed) return Failure(gate.MessageKey);
            }
            if (!priced.IsAffordable) return Failure(priced.Reason);
            final.LifecycleAndOwnership.ReturnedContents = returned;
            var snapshot = DetachedRecognizedSaveStateSnapshot.CaptureWithMana(currentRuntime, priced.ResultingMana, limits);
            if (!snapshot.IsSuccess) return Failure(snapshot.Reason);
            var prepared = session.PrepareLiveReplacement(snapshot, final, priced.Investment,
                corridor, knowledge);
            var result = PrepareAndPersist(activePath, fileSystem, session, prepared, false, ordinarySpend: priced.Cost > 0);
            if (result.IsSuccess) draft.Committed();
            return result;
        }
    }
}
