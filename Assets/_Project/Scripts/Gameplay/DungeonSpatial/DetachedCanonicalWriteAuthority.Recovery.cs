using System;
using System.Linq;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed partial class DetachedCanonicalWriteAuthority
    {
        // Classification/entry and settlement share the existing atomic publication
        // seam; no independently writable recovery file or runtime wallet exists.
        internal DetachedCanonicalWriteResult PublishRecovery(string path, ISpatialMigrationFileSystem fs,
            DetachedCanonicalSaveSession session, CanonicalTimeTransition transition,
            CanonicalOfflinePassiveManaService mana = null,
            Action<DetachedCanonicalSaveSession, TimeProvenance> candidatePrepared = null)
        {
            if (fs == null || session == null || transition == null || context?.RecoveryContext == null)
                return Failure(RecoveryEpisode.Invalid);
            var owned = ValidateSession(session);
            if (owned?.CurrentTargetValidated != true ||
                !transition.Matches(session.GetCurrentBytes(), owned.TimeProvenance)) return Failure(RecoveryEpisode.Invalid);
            try {
                if (!session.GetCurrentBytes().SequenceEqual(fs.ReadAllBytes(path))) return Failure(OfflineStaleSessionReason);
                if (!CanonicalMvpRouteProjection.TryPublishValidated(owned, production, out var current, out _))
                    return Failure(RecoveryEpisode.Invalid);
                double wallet = current.structureRuntime.ManaReserve;
                double recoverySpent = 0;
                OfflinePassiveManaResult grant = null;
                MonsterContinuityState healed = null;
                if (transition.Settles) {
                    if (!owned.LivingDungeon.IsClosed || owned.TimeProvenance.Episode == null || mana == null || !mana.MatchesRecoveryInputs(context.RecoveryInputs))
                        return Failure(RecoveryEpisode.Invalid);
                    var episode = owned.TimeProvenance.Episode;
                    bool original = transition.Successor.Episode != null;
                    long duration = transition.SettlementEligible100ns;
                    grant = mana.ResolveRecoveryDuration(current, configuration, duration,
                        original && episode.OriginalMode == RecoveryEpisode.Legacy);
                    if (!grant.CalculationAccepted) return Failure(RecoveryEpisode.Invalid);
                    wallet = grant.WalletAfter;
                    // Healing belongs to this same checked settlement, including
                    // original -> residualPending. An ordinary metadata rewrite
                    // of that intermediate open episode has no such capability.
                    healed = owned.MonsterContinuity.Copy();
                    double balance = MonsterContinuity.Recover(healed, owned.State, context.MonsterRecovery,
                        wallet, OccupiedFloors(owned));
                    recoverySpent = wallet - balance;
                    wallet = balance;
                }
                var snapshot = DetachedRecognizedSaveStateSnapshot.CaptureWithMana(current, wallet, limits);
                var prepared = session.PrepareRecoveryReplacement(snapshot, owned, transition, healed);
                RecoveryGrantReceipt receipt = null;
                var result = PrepareAndPersist(path, fs, session, prepared, false, candidatePrepared: candidatePrepared,
                    preparedRecoveryManaSpent: recoverySpent, reportingPrepared: (candidate, time, spent, finalWallet) => {
                    // This is the final validated candidate, before IO. It is NOT
                    // a committed receipt until exact durable proof succeeds.
                    if (grant != null)
                            receipt = new RecoveryGrantReceipt(session.GetCurrentBytes(), candidate,
                                time.RecoveryContextSha256, owned.TimeProvenance.Episode,
                                transition.SettlementEligible100ns, grant, spent, finalWallet);
                });
                result.RecoveryReceipt = receipt;
                return result;
            } catch { return Failure(RecoveryEpisode.Invalid); }
        }
    }
}
