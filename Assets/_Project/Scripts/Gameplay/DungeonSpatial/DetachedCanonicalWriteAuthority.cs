using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Economy;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class DetachedCanonicalWriteResult
    {
        private readonly byte[] persistedBytes;
        internal DetachedCanonicalWriteResult(bool success, string reason, bool noOp, bool roomEffect,
            byte[] bytes, DetachedCanonicalSaveSession session,
            DetachedCompleteSaveValidationResult validation, SaveData runtime, double recoveryManaSpent = 0)
        {
            IsSuccess = success; Reason = reason; IsNoOp = noOp;
            ApplyExplicitRoomEffect = roomEffect;
            persistedBytes = bytes == null ? null : (byte[])bytes.Clone();
            Session = session; Validation = validation; RuntimeProjection = runtime;
            RecoveryManaSpent = recoveryManaSpent;
        }
        public bool IsSuccess { get; }
        public string Reason { get; }
        public bool IsNoOp { get; }
        public bool ApplyExplicitRoomEffect { get; }
        public DetachedCanonicalSaveSession Session { get; }
        public DetachedCompleteSaveValidationResult Validation { get; }
        public SaveData RuntimeProjection { get; }
        public double RecoveryManaSpent { get; }
        public byte[] GetPersistedBytes() => persistedBytes == null ? null : (byte[])persistedBytes.Clone();
    }

    /// <summary>
    /// Complete-save writer. It prepares detached state, atomically persists exact session
    /// bytes, verifies durable readback, and only then creates a new runtime projection.
    /// </summary>
    public sealed partial class DetachedCanonicalWriteAuthority
    {
        public const string AtomicSaveFailedReason = "gd66.write.atomic_save_failed";
        public const string RecoveryRequiredReason = "gd66.transaction.recovery_failed";
        public const string OfflineStaleSessionReason = "mana.passive_offline.stale_session";
        private readonly ProductionSpatialContentSnapshot production;
        private readonly SpatialLayoutCompatibilitySnapshot compatibility;
        private readonly RunSimulationConfig configuration;
        private readonly LootConfig runLoot;
        private readonly DetachedCurrentTargetValidationContext context;
        private readonly SaveSpatialMigrationLimitsProfile limits;
        private readonly StructuralContentRemovalPolicySnapshot removalPolicy;
        private readonly StructuralEconomySnapshot economy;
        private readonly ContentAcquisitionEconomySnapshot acquisition;
        private readonly BasicBranchingResearchSnapshot branchingResearch;
        private readonly FloorConstructionProfileSnapshot floorConstructionProfiles;
        private readonly FloorConstructionResearchSnapshot floorConstructionResearch;
        private readonly FormulaModifier[] economyModifiers;

        public DetachedCanonicalWriteAuthority(ProductionSpatialContentSnapshot production,
            SpatialLayoutCompatibilitySnapshot compatibility, RunSimulationConfig configuration,
            DetachedCurrentTargetValidationContext context, SaveSpatialMigrationLimitsProfile limits,
            StructuralContentRemovalPolicySnapshot removalPolicy = null, StructuralEconomySnapshot economy = null,
            IReadOnlyList<FormulaModifier> economyModifiers = null, ContentAcquisitionEconomySnapshot acquisition = null,
            BasicBranchingResearchSnapshot branchingResearch = null,
            FloorConstructionProfileSnapshot floorConstructionProfiles = null,
            FloorConstructionResearchSnapshot floorConstructionResearch = null, LootConfig runLoot = null)
        {
            this.production = production; this.compatibility = compatibility; this.runLoot = runLoot;
            this.configuration = configuration; this.context = context; this.limits = limits;
            this.removalPolicy = removalPolicy;
            this.economy = economy;
            this.acquisition = acquisition;
            this.branchingResearch = branchingResearch;
            this.floorConstructionProfiles = floorConstructionProfiles;
            this.floorConstructionResearch = floorConstructionResearch;
            this.economyModifiers = economyModifiers?.ToArray() ?? Array.Empty<FormulaModifier>();
        }

        public DetachedCanonicalWriteResult ConstructFloor(string activePath,
            ISpatialMigrationFileSystem fileSystem, DetachedCanonicalSaveSession session,
            SaveData currentRuntime, FloorConstructionPreview preview)
        {
            if (fileSystem == null || session == null || currentRuntime?.structureRuntime == null ||
                preview?.Profile == null || string.IsNullOrWhiteSpace(preview.BaselineFingerprint) ||
                floorConstructionProfiles == null || floorConstructionResearch == null)
                return Failure(FloorConstructionService.InvalidContextReason);
            DetachedCompleteSaveValidationResult owned = ValidateSession(session);
            if (owned?.IsValid != true || !owned.CurrentTargetValidated ||
                !StructuralEditService.TryFingerprint(owned.State, limits.Canonical, out string fingerprint) ||
                !string.Equals(fingerprint, preview.BaselineFingerprint, StringComparison.Ordinal))
                return Failure(FloorConstructionService.StalePreviewReason);
            try
            {
                if (!session.GetCurrentBytes().SequenceEqual(fileSystem.ReadAllBytes(activePath)))
                    return Failure(FloorConstructionService.StalePreviewReason);
            }
            catch (Exception) { return Failure(AtomicSaveFailedReason); }

            FloorConstructionPreview refreshed = FloorConstructionService.Preview(owned.State,
                currentRuntime, currentRuntime.completedResearch, floorConstructionProfiles,
                floorConstructionResearch, production, configuration, limits.Canonical);
            if (!refreshed.IsCommittable || !refreshed.Profile.Matches(preview.Profile) ||
                refreshed.FloorInstanceId != preview.FloorInstanceId)
                return Failure(refreshed.Reason ?? FloorConstructionService.StalePreviewReason);

            double resultingMana = currentRuntime.structureRuntime.ManaReserve -
                refreshed.Profile.ConstructionMana;
            if (!StructuralEconomySnapshot.Nonnegative(resultingMana))
                return Failure(FloorConstructionService.InsufficientManaReason);
            var prior = owned.Investment.ToDictionary(value => value.StructureId,
                StringComparer.Ordinal);
            StructuralInvestmentRecord[] investment = StructuralInvestment.Zero(refreshed.Candidate)
                .Select(value => prior.TryGetValue(value.StructureId, out StructuralInvestmentRecord retained)
                    ? retained.Copy() : value).ToArray();
            string shellId = StructuralInvestment.ShellId(refreshed.FloorInstanceId);
            StructuralInvestmentRecord[] shellRecords = investment.Where(value =>
                value.StructureId == shellId).ToArray();
            if (shellRecords.Length != 1 || prior.ContainsKey(shellId))
                return Failure(FloorConstructionService.IdentityInvalidReason);
            shellRecords[0].ConstructionMana = refreshed.Profile.ConstructionMana;
            DetachedRecognizedSaveStateSnapshotResult snapshot =
                DetachedRecognizedSaveStateSnapshot.CaptureWithMana(currentRuntime, resultingMana, limits);
            if (!snapshot.IsSuccess) return Failure(snapshot.Reason);
            DetachedCanonicalSaveSessionResult prepared = session.PrepareLiveReplacement(snapshot,
                refreshed.Candidate, investment, owned.CorridorContent, owned.BranchKnowledge);
            return PrepareAndPersist(activePath, fileSystem, session, prepared, false, ordinarySpend: refreshed.Profile.ConstructionMana > 0);
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
            if (request?.Kind == DetachedCanonicalMutationKind.RedeployReturnedContent ||
                request?.Kind == DetachedCanonicalMutationKind.CorridorContentRedeployment ||
                request?.Kind == DetachedCanonicalMutationKind.UnassignContent ||
                request?.Kind == DetachedCanonicalMutationKind.OptionalBranchRemoval ||
                ContentAcquisitionEconomySnapshot.IsAcquisition(request))
            {
                // Ownership moves and purchases require a current session even when an old
                // request would reproduce candidate bytes accepted by the general retry path.
                try
                {
                    if (!session.GetCurrentBytes().SequenceEqual(fileSystem.ReadAllBytes(activePath)))
                        return Failure(DetachedCanonicalSpatialMutation.ValidationFailedReason);
                }
                catch (Exception)
                { return Failure(AtomicSaveFailedReason); }
            }
            DetachedCanonicalMutationResult mutation = DetachedCanonicalSpatialMutation.Prepare(owned.State,
                request, production, compatibility, configuration, limits.Canonical, removalPolicy,
                owned.CorridorContent, owned.BranchKnowledge, currentRuntime.completedResearch,
                branchingResearch, context.RoomContentOccupancy);
            if (mutation.IsNoOp) return new DetachedCanonicalWriteResult(false, mutation.Reason, true,
                false, null, null, null, null);
            if (!mutation.IsSuccess) return Failure(mutation.Reason);
            StructuralInvestmentRecord[] investment = owned.Investment;
            bool ordinarySpend = false;
            DetachedRecognizedSaveStateSnapshotResult snapshot;
            if (StructuralEconomyService.IsStructural(request))
            {
                var priced = StructuralEconomyService.Prepare(owned.State, mutation.State, owned.Investment,
                    currentRuntime.structureRuntime?.ManaReserve ?? double.NaN, economy,
                    StructuralEconomyService.Operation(request), StructuralEconomyService.Target(request), economyModifiers);
                if (!priced.IsAffordable) return Failure(priced.Reason);
                investment = priced.Investment;
                ordinarySpend = priced.Cost > 0;
                snapshot = DetachedRecognizedSaveStateSnapshot.CaptureWithMana(currentRuntime, priced.ResultingMana, limits);
            }
            else if (request.Kind == DetachedCanonicalMutationKind.RedeployReturnedContent ||
                request.Kind == DetachedCanonicalMutationKind.CorridorContentRedeployment ||
                request.Kind == DetachedCanonicalMutationKind.UnassignContent)
            {
                // Moving owned content changes neither the wallet nor the structural ledger.
                snapshot = DetachedRecognizedSaveStateSnapshot.Capture(currentRuntime, limits);
            }
            else
            {
                // Implicit compatibility geometry gains zero structural basis; content spending
                // never becomes structural investment. Migration reconstruction uses other paths.
                var prior = investment.ToDictionary(r => r.StructureId, StringComparer.Ordinal);
                if (prior.Keys.Except(StructuralInvestment.Ids(mutation.State), StringComparer.Ordinal).Any())
                    return Failure(StructuralEconomyService.InvalidReason);
                investment = StructuralInvestment.Zero(mutation.State).Select(r =>
                    prior.TryGetValue(r.StructureId, out var retained) ? retained.Copy() : r).ToArray();
                if (ContentAcquisitionEconomySnapshot.IsAcquisition(request))
                {
                    if (acquisition == null || !acquisition.TryPrice(request.CategoryId, request.OptionId, out double price))
                        return Failure(ContentAcquisitionEconomySnapshot.InvalidReason);
                    double mana = currentRuntime.structureRuntime?.ManaReserve ?? double.NaN;
                    if (!StructuralEconomySnapshot.Nonnegative(mana))
                        return Failure(DetachedCanonicalSpatialMutation.ValidationFailedReason);
                    if (mana < price) return Failure(ContentAcquisitionEconomySnapshot.InsufficientReason);
                    ordinarySpend = price > 0;
                    snapshot = DetachedRecognizedSaveStateSnapshot.CaptureWithMana(currentRuntime, mana - price, limits);
                }
                else snapshot = DetachedRecognizedSaveStateSnapshot.Capture(currentRuntime, limits);
            }
            if (!snapshot.IsSuccess) return Failure(snapshot.Reason);
            DetachedCanonicalSaveSessionResult prepared =
                session.PrepareLiveReplacement(snapshot, mutation.State, investment,
                    mutation.CorridorContent, mutation.BranchKnowledge);
            return PrepareAndPersist(activePath, fileSystem, session, prepared, mutation.ApplyExplicitRoomEffect, ordinarySpend);
        }

        internal DetachedCanonicalWriteResult UndoRenovation(string activePath,
            ISpatialMigrationFileSystem fileSystem, DetachedCanonicalSaveSession session, SaveData currentRuntime,
            DetachedCanonicalSpatialSaveState previous, StructuralInvestmentRecord[] investment,
            string expectedFingerprint, double paid)
        {
            var owned = ValidateSession(session);
            if (owned?.IsValid != true || !StructuralEditService.TryFingerprint(owned.State, limits.Canonical,
                out string actual) || actual != expectedFingerprint || currentRuntime?.structureRuntime == null)
                return Failure(StructuralEconomyService.UndoUnavailableReason);
            var snapshot = DetachedRecognizedSaveStateSnapshot.CaptureWithMana(currentRuntime,
                currentRuntime.structureRuntime.ManaReserve + paid, limits);
            if (!snapshot.IsSuccess) return Failure(snapshot.Reason);
            return PrepareAndPersist(activePath, fileSystem, session,
                session.PrepareLiveReplacement(snapshot, previous, investment), false);
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

        public DetachedCanonicalWriteResult CommitPhaseFiveBRun(string activePath,
            ISpatialMigrationFileSystem fileSystem, DetachedCanonicalSaveSession session,
            SaveData currentRuntime, DungeonBuilder.M0.Gameplay.RunSimulation.RunSimulationService simulation,
            string postureId, long savedUtcUnix)
        {
            const string invalid = "branch.run.invalid_state";
            if (fileSystem == null || currentRuntime?.structureRuntime == null || currentRuntime.runHistory == null ||
                simulation == null || context == null || limits == null || production == null) return Failure(invalid);
            DetachedCompleteSaveValidationResult owned;
            try
            {
                owned = ValidateSession(session);
                if (owned?.CurrentTargetValidated != true || !owned.IsValid ||
                    CanonicalMvpRouteProjection.InspectWithProductionContent(currentRuntime, production).AuthorityState !=
                        CanonicalMvpRuntimeAuthorityState.ValidatedCanonical ||
                    !CanonicalEqual(currentRuntime.validatedCanonicalSpatialState, owned.State)) return Failure(invalid);
            }
            catch { return Failure(invalid); }
            if (owned.LivingDungeon?.IsDormant == false &&
                (!owned.LivingDungeon.IsClosed || !LivingDungeonScheduling.AllocatorValid(currentRuntime.runHistory, owned.LivingDungeon, true)))
                return Failure(LivingDungeonScheduling.Invalid);
            try
            {
                if (!session.GetCurrentBytes().SequenceEqual(fileSystem.ReadAllBytes(activePath)))
                    return Failure("branch.run.stale_session");
            }
            catch { return Failure(AtomicSaveFailedReason); }
            SaveData candidate;
            try
            {
                candidate = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(currentRuntime));
                if (currentRuntime.researchPending == null) candidate.researchPending = null;
                if (currentRuntime.researchProgress == null) candidate.researchProgress = null;
                if (currentRuntime.lastOfflineSummary == null) candidate.lastOfflineSummary = null;
                DungeonBuilder.M0.Gameplay.RunSimulation.RunTransientEvidence.Retain(currentRuntime, candidate);
                candidate.validatedCanonicalSpatialState = owned.State;
                candidate.canonicalSpatialAuthority = owned.State.Authority; candidate.spatialFloors = owned.State.Floors;
                candidate.corridorContent = owned.CorridorContent; candidate.sharedBranchKnowledge = owned.BranchKnowledge;
                candidate.sharedFloorKnowledge = owned.FloorKnowledge;
                candidate.livingDungeon = owned.LivingDungeon;
                candidate.monsterContinuity = owned.MonsterContinuity.Copy();
                candidate.lastSavedUtcUnix = savedUtcUnix;
                simulation.CalculatePhaseFiveBRun(candidate, owned, production, postureId, limits.Canonical);
                var completed = candidate.runHistory.LatestOutcome;
                if (DungeonBuilder.M0.Gameplay.RunSimulation.CombatC0Diagnostics.IsCombatC2(completed))
                {
                    var records = candidate.monsterContinuity.Records.ToDictionary(r => r.AssignmentId, StringComparer.Ordinal);
                    foreach (var monster in completed.CombatMonsters)
                    {
                        var record = records[monster.AssignmentId];
                        if (record.Health != monster.InitialHealth || record.RevivalRequired != monster.InitialRevivalRequired)
                            return Failure(MonsterRecoveryRules.Invalid);
                        record.Health = monster.Health; record.RevivalRequired = monster.RevivalRequired;
                    }
                }
                if (!StructuralEditService.TryFingerprint(owned.State, limits.Canonical, out var presentationFingerprint))
                    return Failure(invalid);
                candidate.runHistory.LatestOutcome.PresentationLayoutFingerprint = presentationFingerprint;
                if (!DungeonBuilder.M0.Gameplay.RunSimulation.RunTransientEvidence.TryCorridorFingerprint(
                    candidate.runHistory.LatestOutcome, owned.CorridorContent, limits.Canonical, out var corridorFingerprint))
                    return Failure(invalid);
                candidate.runHistory.LatestOutcome.PresentationCorridorFingerprint = corridorFingerprint;
            }
            catch (Exception error)
            {
                string reason = error.Message;
                return Failure(reason == DungeonBuilder.M0.Gameplay.RunSimulation.BranchRunWorkload.WorkloadExceeded ||
                    reason == DungeonBuilder.M0.Gameplay.RunSimulation.BranchDecisionResolver.InvalidConfiguration ||
                    reason == DungeonBuilder.M0.Gameplay.RunSimulation.PhaseFiveBRouteProjection.InvalidRoute ? reason : invalid);
            }
            var snapshot = DetachedRecognizedSaveStateSnapshot.Capture(candidate, limits);
            if (!snapshot.IsSuccess) return Failure(snapshot.Reason);
            var result = PrepareAndPersist(activePath, fileSystem, session,
                session.PrepareManualRunReplacement(snapshot, owned.State, owned.Investment,
                    owned.CorridorContent, candidate.sharedBranchKnowledge,
                    candidate.sharedFloorKnowledge, candidate.monsterContinuity), false, recover: true);
            if (result.IsSuccess)
                DungeonBuilder.M0.Gameplay.RunSimulation.RunTransientEvidence.Retain(candidate, result.RuntimeProjection);
            return result;
        }

        internal DetachedCanonicalWriteResult SaveQaMana(string activePath,
            ISpatialMigrationFileSystem fileSystem, DetachedCanonicalSaveSession session,
            SaveData currentRuntime, bool fillToCapacity)
        {
            if (economy == null) return Failure(StructuralEconomyService.InvalidReason);
            if (fileSystem == null || context == null || limits == null || production == null ||
                compatibility == null || configuration == null || currentRuntime?.structureRuntime == null)
                return Failure(DetachedCanonicalSpatialMutation.ValidationFailedReason);
            var owned = ValidateSession(session);
            if (owned?.IsValid != true || !owned.CurrentTargetValidated ||
                !StructuralEconomySnapshot.Nonnegative(currentRuntime.structureRuntime.ManaReserve))
                return Failure(DetachedCanonicalSpatialMutation.ValidationFailedReason);
            double mana = fillToCapacity
                ? Math.Max(currentRuntime.structureRuntime.ManaReserve, economy.ManaCapacity) : 0;
            var snapshot = DetachedRecognizedSaveStateSnapshot.CaptureWithMana(currentRuntime, mana, limits);
            if (!snapshot.IsSuccess) return Failure(snapshot.Reason);
            return PrepareAndPersist(activePath, fileSystem, session,
                session.PrepareLiveReplacement(snapshot, owned.State, owned.Investment), false);
        }

        internal DetachedCanonicalWriteResult SaveOfflinePassiveMana(string activePath,
            ISpatialMigrationFileSystem fileSystem, DetachedCanonicalSaveSession session,
            SaveData currentRuntime, double resultingMana, long sourceSavedUtcUnix,
            long observedCurrentUtcUnix)
        {
            if (economy == null || fileSystem == null || session == null || context == null ||
                limits == null || production == null || compatibility == null ||
                configuration == null || currentRuntime?.structureRuntime == null ||
                currentRuntime.lastSavedUtcUnix != sourceSavedUtcUnix ||
                observedCurrentUtcUnix < sourceSavedUtcUnix ||
                !StructuralEconomySnapshot.Nonnegative(resultingMana) ||
                resultingMana > economy.ManaCapacity)
                return Failure(DetachedCanonicalSpatialMutation.ValidationFailedReason);

            DetachedCompleteSaveValidationResult owned = ValidateSession(session);
            if (owned?.IsValid != true || !owned.CurrentTargetValidated)
                return Failure(DetachedCanonicalSpatialMutation.ValidationFailedReason);
            try
            {
                if (!session.GetCurrentBytes().SequenceEqual(fileSystem.ReadAllBytes(activePath)))
                    return Failure(OfflineStaleSessionReason);
            }
            catch (Exception)
            {
                return Failure(AtomicSaveFailedReason);
            }

            DetachedRecognizedSaveStateSnapshotResult snapshot =
                DetachedRecognizedSaveStateSnapshot.CaptureWithManaAndTimestamp(
                    currentRuntime, resultingMana, observedCurrentUtcUnix, limits);
            if (!snapshot.IsSuccess) return Failure(snapshot.Reason);
            return PrepareAndPersist(activePath, fileSystem, session,
                session.PrepareLiveReplacement(snapshot, owned.State, owned.Investment), false, recover: true);
        }

        private DetachedCanonicalWriteResult PrepareAndPersist(string activePath,
            ISpatialMigrationFileSystem fileSystem, DetachedCanonicalSaveSession session,
            DetachedCanonicalSaveSessionResult prepared, bool roomEffect, bool ordinarySpend = false, bool recover = false)
        {
            if (prepared == null || !prepared.IsSuccess || prepared.Update == null)
                return Failure(prepared?.Reason ?? DetachedCanonicalSpatialMutation.ValidationFailedReason);
            byte[] candidate = prepared.Update.GetBytes();
            double recoveryManaSpent = 0;
            DetachedCompleteSaveValidationResult validated =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(candidate, context);
            var predecessor = ValidateSession(session);
            if (validated.IsValid && predecessor?.IsValid == true)
            {
                try
                {
                    if (!CanonicalMvpRouteProjection.TryPublishValidated(validated, production, out var proposed, out _) ||
                        !CanonicalMvpRouteProjection.TryPublishValidated(predecessor, production, out var prior, out _)) return Failure(MonsterRecoveryRules.Invalid);
                    long required = MonsterContinuity.RequiredReserve(validated.State, context.MonsterRecovery);
                    long priorRequired = MonsterContinuity.RequiredReserve(predecessor.State, context.MonsterRecovery);
                    if ((ordinarySpend || required > priorRequired) && proposed.structureRuntime.ManaReserve < required)
                        return Failure(MonsterRecoveryRules.Insufficient);
                    // No recovery at a partially processed B2 horizon. B2 checkpoints retain
                    // their original B1 inputs and acquire no C2 injury attribution.
                    var individuals = validated.MonsterContinuity.Records.ToDictionary(r => r.AssignmentId, StringComparer.Ordinal);
                    bool fundablePending = proposed.structureRuntime.ManaReserve >= context.MonsterRecovery.ManaPerHealth &&
                        validated.State.Floors.Where(f => f.ActivationState == FloorActivationState.Active).SelectMany(f => f.RoomContents.Assignments)
                        .Any(a => a.CategoryId == CanonicalSpatialSaveContracts.MonsterCategoryId && individuals[a.AssignmentId].Health < context.MonsterRecovery.Readiness(a.OptionId));
                    if (fundablePending && validated.LivingDungeon.IsClosed && (recover || required > priorRequired ||
                        proposed.structureRuntime.ManaReserve > prior.structureRuntime.ManaReserve))
                    {
                        var occupied = OccupiedFloors(validated);
                        var healed = validated.MonsterContinuity.Copy();
                        double balance = MonsterContinuity.Recover(healed, validated.State, context.MonsterRecovery,
                            proposed.structureRuntime.ManaReserve, occupied);
                        if (balance != proposed.structureRuntime.ManaReserve)
                        {
                            recoveryManaSpent = proposed.structureRuntime.ManaReserve - balance;
                            var intermediate = DetachedCanonicalSaveSession.Open(candidate, context, limits);
                            var capture = DetachedRecognizedSaveStateSnapshot.CaptureWithMana(proposed, balance, limits);
                            var recovered = intermediate.Session.PrepareLiveReplacement(capture, validated.State, monsterContinuity: healed);
                            if (!recovered.IsSuccess) return Failure(recovered.Reason);
                            candidate = recovered.Update.GetBytes();
                            validated = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(candidate, context);
                        }
                    }
                }
                catch { return Failure(MonsterRecoveryRules.Invalid); }
            }
            DetachedCanonicalSaveSessionResult reopened = validated.IsValid
                ? DetachedCanonicalSaveSession.Open(candidate, context, limits) : null;
            if (!validated.IsValid || !validated.CurrentTargetValidated || reopened == null ||
                !reopened.IsSuccess)
                return Failure(DetachedCanonicalSpatialMutation.ValidationFailedReason);
            if (!CanonicalMvpRouteProjection.TryPublishValidated(validated,
                    production, out SaveData runtime, out string publishReason))
                return Failure(publishReason ?? DetachedCanonicalSpatialMutation.ValidationFailedReason);
            if (validated.State.Floors.Count(f => f.ActivationState == FloorActivationState.Active) > 1)
            {
                try { DungeonBuilder.M0.Gameplay.RunSimulation.ActiveFloorRunSnapshot.Create(
                    validated, production, configuration, limits.Canonical, runLoot); }
                catch { return Failure("floor.lifecycle.invalid_state"); }
            }
            string persistenceReason = ExactCompleteSaveAtomicPersistence.Persist(activePath, fileSystem, session.GetCurrentBytes(), candidate,
                limits.Canonical.Serialized.MaximumCollectionRecords, durable =>
                {
                    var readValidation = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(durable, context);
                    var readSession = DetachedCanonicalSaveSession.Open(durable, context, limits);
                    if (!readValidation.IsValid || !readValidation.CurrentTargetValidated || !readSession.IsSuccess ||
                        !CanonicalMvpRouteProjection.TryPublishValidated(readValidation, production, out SaveData readRuntime, out _)) return false;
                    validated = readValidation; reopened = readSession; runtime = readRuntime;
                    return true;
                });
            return persistenceReason == null
                ? new DetachedCanonicalWriteResult(true, null, false, roomEffect, candidate,
                    reopened.Session, validated, runtime, recoveryManaSpent)
                : Failure(persistenceReason);
        }

        private DetachedCompleteSaveValidationResult ValidateSession(DetachedCanonicalSaveSession session) =>
            session == null ? null : DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                session.GetCurrentBytes(), context);

        private System.Collections.Generic.ISet<string> OccupiedFloors(DetachedCompleteSaveValidationResult owned)
        {
            var occupied = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            if (owned.LivingDungeon.IsDormant) return occupied;
            var profile = LivingDungeonContracts.Profile(owned.LivingDungeon, context.LivingDungeonProfiles);
            foreach (var party in owned.LivingDungeon.Parties)
            {
                var execution = LivingDungeonContracts.Restore(party, profile);
                if (execution.IsTerminal) continue;
                var last = execution.Inspect().LastStep;
                // Qualification-only route checkpoints can lack canonical floor identity.
                // Vacancy cannot be proven for these inputs, so recovery is ineligible.
                if (execution.Snapshot == null) return owned.State.Floors.Select(f => f.FloorInstanceId).ToHashSet(StringComparer.Ordinal);
                int index = last?.FloorIndex ?? execution.Snapshot.Floors[0].FloorIndex;
                foreach (var floor in execution.Snapshot.Floors.Where(f => f.FloorIndex == index)) occupied.Add(floor.FloorInstanceId);
            }
            return occupied;
        }

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
