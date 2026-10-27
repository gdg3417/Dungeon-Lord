using System;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // A detached, exact-predecessor capability consumed by the existing complete-save
    // writer. It neither publishes a save nor owns a clock, wallet or second cursor.
    internal sealed class CanonicalTimeTransition
    {
        private readonly string predecessor;
        private readonly TimeProvenance expected;
        internal TimeProvenance Successor { get; }
        internal bool Settles { get; }
        internal long SettlementEligible100ns { get; }
        private CanonicalTimeTransition(byte[] source, TimeProvenance prior, TimeProvenance successor, bool settles, long settlementEligible100ns = 0)
        { predecessor = SpatialContractSha256.Compute(source); expected = prior; Successor = successor; Settles = settles; SettlementEligible100ns = settlementEligible100ns; }

        internal bool Matches(byte[] source, TimeProvenance prior) =>
            prior?.Mode == TimeProvenance.PreciseMode && expected.SameAs(prior) && SpatialContractSha256.Compute(source) == predecessor &&
            prior.RecoveryContextSha256 == Successor.RecoveryContextSha256 &&
            Successor.AccountedBoundary.CompareTo(prior.AccountedBoundary) >= 0;

        internal static CanonicalTimeTransition Foreground(byte[] source, TimeProvenance prior,
            ForegroundObservation cut, LivingDungeonState owner)
        {
            if (prior?.Mode != TimeProvenance.PreciseMode || prior.Episode != null || cut == null ||
                cut.PredecessorSha256 != SpatialContractSha256.Compute(source) ||
                cut.Context != ForegroundObservationContext.EligibleForeground ||
                cut.AcceptedThrough != (owner?.Scheduler?.AcceptedThrough ?? -1) ||
                cut.ClosedThrough != (owner?.Scheduler?.ClosedThrough ?? -1) ||
                cut.WallEndpoint.CompareTo(prior.AccountedBoundary) < 0)
                throw new ArgumentException(RecoveryEpisode.Invalid);
            return new CanonicalTimeTransition(source, prior, new TimeProvenance(cut.WallEndpoint,
                prior.RecoveryContextSha256, null), false);
        }

        internal static CanonicalTimeTransition Enter(byte[] source, TimeProvenance prior,
            RecoveryEntryObservation entry, long eligible100ns)
        {
            if (prior?.Mode != TimeProvenance.PreciseMode || prior.Episode != null || entry == null ||
                eligible100ns < 0 || eligible100ns > entry.WallEndpoint.Elapsed100nsSince(prior.AccountedBoundary))
                throw new ArgumentException(RecoveryEpisode.Invalid);
            var episode = new RecoveryEpisode(SpatialContractSha256.Compute(source), prior.AccountedBoundary,
                entry.WallEndpoint, entry.Context == RecoveryEntryContext.KnownBackground ? RecoveryEpisode.KnownBackground :
                RecoveryEpisode.UnknownPostCrash, RecoveryEpisode.Precise, RecoveryEpisode.OriginalPending, eligible100ns, 0);
            return new CanonicalTimeTransition(source, prior, new TimeProvenance(entry.WallEndpoint,
                prior.RecoveryContextSha256, episode), false);
        }

        // The coordinator supplies a full classified observation, never R+eligible
        // as an invented wall endpoint. Success/rollback changes only disposition.
        internal static CanonicalTimeTransition Recover(byte[] source, TimeProvenance prior,
            UnixTimePoint through, long eligible100ns, LivingDungeonState owner,
            bool settleOriginal = false, bool closeResidual = false)
        {
            if (prior?.Mode != TimeProvenance.PreciseMode || prior.Episode == null || through == null ||
                through.CompareTo(prior.AccountedBoundary) < 0 || eligible100ns < 0 ||
                eligible100ns > through.Elapsed100nsSince(prior.AccountedBoundary) ||
                settleOriginal && closeResidual || (settleOriginal || closeResidual) && owner?.IsClosed != true ||
                settleOriginal && prior.Episode.Phase != RecoveryEpisode.OriginalPending ||
                closeResidual && prior.Episode.Phase != RecoveryEpisode.ResidualPending)
                throw new ArgumentException(RecoveryEpisode.Invalid);
            var next = prior.Episode.Advance(eligible100ns, settleOriginal);
            return new CanonicalTimeTransition(source, prior, new TimeProvenance(through, prior.RecoveryContextSha256,
                closeResidual ? null : next), settleOriginal || closeResidual,
                closeResidual ? next.ResidualEligible100ns : settleOriginal ? next.OriginalEligible100ns : 0);
        }
    }
}
