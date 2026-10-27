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
            CanonicalOfflinePassiveManaService mana = null)
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
                if (transition.Settles) {
                    if (!owned.LivingDungeon.IsClosed || owned.TimeProvenance.Episode == null || mana == null)
                        return Failure(RecoveryEpisode.Invalid);
                    var episode = owned.TimeProvenance.Episode;
                    bool original = transition.Successor.Episode != null;
                    long duration = transition.SettlementEligible100ns;
                    var grant = mana.ResolveRecoveryDuration(current, configuration, duration,
                        original && episode.OriginalMode == RecoveryEpisode.Legacy);
                    if (!grant.CalculationAccepted) return Failure(RecoveryEpisode.Invalid);
                    wallet = grant.WalletAfter;
                }
                var snapshot = DetachedRecognizedSaveStateSnapshot.CaptureWithMana(current, wallet, limits);
                var prepared = session.PrepareRecoveryReplacement(snapshot, owned, transition);
                return PrepareAndPersist(path, fs, session, prepared, false, recover: transition.Settles);
            } catch { return Failure(RecoveryEpisode.Invalid); }
        }
    }
}
