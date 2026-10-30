using System;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Economy;

namespace DungeonBuilder.M0
{
    public partial class SaveService
    {
        private RecoveryRuntimeInputs recoveryInputs;
        private TimeService recoveryTime;
        private IHistoricalRecoveryEvidence historicalRecoveryEvidence;
        private DetachedCanonicalSaveSession pendingPredecessor, pendingCandidate;
        private TimeService.RecoveryPublicationObservation pendingRecoveryObservation;
        private bool recoveryPublicationUncertain;
        private RecoveryGrantReceipt pendingRecoveryReceipt;
        internal RecoveryEpisodeReport RecoveryReport { get; private set; }
        private static bool UsesAtomicRecovery => CanonicalSaveSchemaVersions.CurrentWritableTarget == CanonicalSaveSchemaVersions.AtomicRecoveryIntroduction;
        internal bool LastLoadCreatedNative { get; private set; }

        private void RetainPublication(DetachedCanonicalSaveSession predecessor, DetachedCanonicalSaveSession candidate, TimeProvenance time)
        {
            if (recoveryPublicationUncertain || pendingCandidate != null) throw new InvalidOperationException(RecoveryEpisode.Invalid);
            pendingRecoveryObservation?.Bind(candidate.GetCurrentBytes(), time);
            pendingPredecessor = predecessor; pendingCandidate = candidate;
        }

        private void ObservePublication(DetachedCanonicalWriteResult result)
        {
            if (pendingRecoveryObservation != null) return; // The recovery coordinator owns this disposition.
            try {
                if (result.IsSuccess) {
                    if (RecoveryReport != null && !RecoveryReport.Closed && pendingPredecessor != null)
                        RecoveryReport.Observe(pendingPredecessor.GetCurrentBytes(), result, null);
                    if (result.Validation.TimeProvenance?.Mode == TimeProvenance.PreciseMode)
                        recoveryTime.OrdinaryPublicationSucceeded(result);
                    pendingPredecessor = null; pendingCandidate = null;
                } else {
                    recoveryTime.OrdinaryPublicationFailed();
                    if (result.Reason == DetachedCanonicalWriteAuthority.AtomicSaveFailedReason) {
                        pendingPredecessor = null; pendingCandidate = null;
                    } else { recoveryPublicationUncertain = true; foregroundRecoveryRequired = true; }
                }
            } catch {
                recoveryPublicationUncertain = true; foregroundRecoveryRequired = true;
                throw;
            }
        }

        internal void BeginCanonicalRecovery(SaveData proven, bool observedBackground = false)
        {
            if (recoveryInputs == null || !recoveryInputs.ConsumedValuesStillBound(_validationContext?.RoomContentOccupancy) ||
                recoveryTime == null || proven?.timeProvenance?.Mode != TimeProvenance.PreciseMode ||
                _canonicalSession == null || recoveryPublicationUncertain) {
                RecoveryReport = null;
                throw new InvalidOperationException(RecoveryEpisode.Invalid);
            }
            recoveryTime.AttachSave(proven);
            if (!recoveryTime.RecoveryClockActive) {
                if (observedBackground) recoveryTime.BeginObservedRecovery(proven);
                else recoveryTime.BeginRecoveryClock(proven.timeProvenance);
                RecoveryReport = new RecoveryEpisodeReport(_canonicalSession.GetCurrentBytes(), proven.timeProvenance.Episode);
            }
        }

        // One bounded transaction per pump. The existing foreground executor
        // drains old A; it cannot accept more time while the episode is open.
        internal DetachedCanonicalWriteResult AdvanceCanonicalRecovery(SaveData proven,
            CanonicalOfflinePassiveManaService mana, Action<SaveData, long> passiveTick)
        {
            if (recoveryInputs == null || recoveryTime?.RecoveryClockActive != true || _canonicalSession == null ||
                recoveryPublicationUncertain || pendingCandidate != null || ForegroundWriting)
                return RecoveryFailure(RecoveryEpisode.Invalid);
            var owned = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(_canonicalSession.GetCurrentBytes(), _validationContext);
            if (owned.CurrentTargetValidated != true || !CanonicalMvpRouteProjection.TryPublishValidated(owned, _production, out var current, out _) ||
                proven?.timeProvenance?.SameAs(owned.TimeProvenance) != true)
                return RecoveryFailure(RecoveryEpisode.Invalid);
            var episode = owned.TimeProvenance.Episode;
            bool residual = episode?.Phase == RecoveryEpisode.ResidualPending;
            ForegroundWriting = true;
            try {
                pendingRecoveryObservation = recoveryTime.CaptureRecoveryPublication(_canonicalSession.GetCurrentBytes(), owned.TimeProvenance, residual);
                var cut = pendingRecoveryObservation;
                CanonicalTimeTransition transition = episode == null
                    ? CanonicalTimeTransition.Enter(_canonicalSession.GetCurrentBytes(), owned.TimeProvenance,
                        recoveryTime.RetainedRecoveryEntry, recoveryTime.RecoveryOriginalEligible100ns, cut.Through, cut.Eligible100ns)
                    : CanonicalTimeTransition.Recover(_canonicalSession.GetCurrentBytes(), owned.TimeProvenance,
                        cut.Through, cut.Eligible100ns, owned.LivingDungeon,
                        settleOriginal: owned.LivingDungeon.IsClosed && !residual, closeResidual: residual);
                var writer = CreateWriteAuthority();
                var result = episode != null && !owned.LivingDungeon.IsClosed
                    ? writer.AdvanceForeground(SavePath, _canonicalFileSystem, _canonicalSession, current, ForegroundConfiguration,
                        owned.LivingDungeon.Scheduler.AcceptedThrough, recoveryInputs.Bootstrap.tickSeconds, passiveTick, cut.Through.Seconds, transition)
                    : writer.PublishRecovery(SavePath, _canonicalFileSystem, _canonicalSession, transition, mana);
                if (result.IsSuccess) {
                    recoveryTime.RecoveryPublicationSucceeded(cut, result);
                    AdoptRecoveryPublication(result, result.RecoveryReceipt);
                } else if (result.Reason == DetachedCanonicalWriteAuthority.RecoveryRequiredReason && pendingCandidate != null) {
                    pendingRecoveryReceipt = result.RecoveryReceipt;
                    recoveryTime.RecoveryPublicationUncertain(cut); recoveryPublicationUncertain = true; foregroundRecoveryRequired = true;
                } else {
                    recoveryTime.RecoveryPublicationFailed(cut);
                    pendingRecoveryObservation = null; pendingPredecessor = null; pendingCandidate = null;
                    pendingRecoveryReceipt = null;
                }
                return result;
            } catch {
                // Candidate/clock evidence remains held. An exception after IO is
                // never treated as rollback or permission to repeat a grant.
                recoveryPublicationUncertain = true; foregroundRecoveryRequired = true;
                return RecoveryFailure(RecoveryEpisode.Invalid);
            } finally { ForegroundWriting = false; }
        }

        private void AdoptRecoveryPublication(DetachedCanonicalWriteResult proof, RecoveryGrantReceipt receipt = null, bool rollback = false)
        {
            if (!rollback && pendingRecoveryObservation != null && pendingPredecessor != null)
                RecoveryReport?.Observe(pendingPredecessor.GetCurrentBytes(), proof, receipt);
            _canonicalSession = proof.Session; _undo = null;
            pendingPredecessor = null; pendingCandidate = null; pendingRecoveryObservation = null;
            recoveryPublicationUncertain = false; foregroundRecoveryRequired = false;
            pendingRecoveryReceipt = null;
            recoveryTime.AttachSave(proof.RuntimeProjection);
            CanonicalRuntimePublished?.Invoke(proof.RuntimeProjection);
        }

        // Explicit retry only. The exact retained predecessor/candidate pair is
        // resolved by fresh active bytes and the existing persistence authority.
        internal DetachedCanonicalWriteResult ResolveUncertainRecoveryPublication()
        {
            if (!recoveryPublicationUncertain || pendingCandidate == null)
                return RecoveryFailure(DetachedCanonicalWriteAuthority.RecoveryRequiredReason);
            try {
                var active = _canonicalFileSystem.ReadAllBytes(SavePath);
                bool committed = ExactCompleteSaveAtomicPersistence.Same(active, pendingCandidate.GetCurrentBytes());
                if (!committed && (pendingPredecessor == null || !ExactCompleteSaveAtomicPersistence.Same(active, pendingPredecessor.GetCurrentBytes())))
                    return RecoveryFailure(DetachedCanonicalWriteAuthority.RecoveryRequiredReason);
                _canonicalFileSystem.FlushDirectory(System.IO.Path.GetDirectoryName(SavePath));
                DetachedCompleteSaveValidationResult validation = null;
                DetachedCanonicalSaveSessionResult opened = null; SaveData runtime = null;
                string reason = ExactCompleteSaveAtomicPersistence.Persist(SavePath, _canonicalFileSystem, active, active,
                    _limits.Canonical.Serialized.MaximumCollectionRecords, durable => {
                        validation = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(durable, _validationContext);
                        opened = DetachedCanonicalSaveSession.Open(durable, _validationContext, _limits);
                        return validation.CurrentTargetValidated && opened.IsSuccess &&
                            CanonicalMvpRouteProjection.TryPublishValidated(validation, _production, out runtime, out _);
                    });
                if (reason != null) return RecoveryFailure(reason);
                var proof = new DetachedCanonicalWriteResult(true, null, false, false, active, opened.Session, validation, runtime);
                if (pendingRecoveryObservation != null) {
                    if (committed) recoveryTime.RecoveryPublicationSucceeded(pendingRecoveryObservation, proof);
                    else recoveryTime.RecoveryPublicationFailed(pendingRecoveryObservation);
                } else recoveryTime.OrdinaryPublicationSucceeded(proof);
                // Reconstructed proof has no historical spending information.
                // Only the exact retained candidate receipt may supply it.
                AdoptRecoveryPublication(proof, committed ? pendingRecoveryReceipt : null, rollback: !committed); return proof;
            } catch { return RecoveryFailure(DetachedCanonicalWriteAuthority.RecoveryRequiredReason); }
        }

        private static DetachedCanonicalWriteResult RecoveryFailure(string reason) =>
            new DetachedCanonicalWriteResult(false, reason, false, false, null, null, null, null);

        internal void ConfigureRecovery(RecoveryRuntimeInputs inputs, TimeService observationOwner,
            IHistoricalRecoveryEvidence historicalEvidence = null)
        {
            if (!_canonicalConfigured || _canonicalSession != null || recoveryInputs != null || inputs == null || observationOwner == null ||
                _limits == null || _production == null || _positionMigrationProfiles == null)
                throw new ArgumentException(RecoveryEpisode.Invalid);
            var context = new DetachedCurrentTargetValidationContext(inputs.Compatibility, _production,
                LegacyGameplayConfigurationContract.SerializeCanonical(inputs.Configuration), _limits.Canonical, inputs.Occupancy,
                livingDungeonProfiles: new[] { inputs.TwoFloor.CreateProfile(_limits.Canonical.ForSchema(13).Serialized),
                    inputs.OneFloor.CreateProfile(_limits.Canonical.ForSchema(13).Serialized) }, monsterRecovery: inputs.Monsters);
            context.BindRecoveryInputs(inputs, historicalEvidence);
            observationOwner.BindRecoveryContext(inputs.Identity.Digest);
            recoveryInputs = inputs; recoveryTime = observationOwner; historicalRecoveryEvidence = historicalEvidence;
            _validationContext = context; _compatibility = inputs.Compatibility; _roomContentOccupancy = inputs.Occupancy;
            _legacyGameplayConfiguration = inputs.Configuration;
            _legacyConfiguration = LegacyGameplayConfigurationContract.SerializeCanonical(inputs.Configuration);
            _runLoot = inputs.Loot; _economy = inputs.Economy;
            ForegroundConfiguration = inputs.TwoFloor;
        }
    }
}
