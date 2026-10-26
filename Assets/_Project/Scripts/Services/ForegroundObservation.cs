using System;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0
{
    public enum ForegroundObservationContext { EligibleForeground, Suspended }

    // A captured full horizon, not a completed candidate or another elapsed ledger.
    // No API can subdivide/relabel this cut or interpolate another wall endpoint.
    public sealed class ForegroundObservation
    {
        public string PredecessorSha256 { get; }
        public long CapturedThrough { get; }
        public long AcceptedThrough { get; }
        public long ClosedThrough { get; }
        public UnixTimePoint WallEndpoint { get; }
        public ForegroundObservationContext Context { get; }
        private ForegroundObservation(byte[] predecessor, long captured, long accepted, long closed,
            UnixTimePoint wall, ForegroundObservationContext context)
        {
            if (predecessor == null || captured < 0 || accepted < -1 || closed < -1 || closed > accepted ||
                captured < accepted || wall == null || wall.Seconds <= 0 || !Enum.IsDefined(typeof(ForegroundObservationContext), context))
                throw new ArgumentException("living.foreground.observation_invalid");
            PredecessorSha256 = SpatialContractSha256.Compute(predecessor);
            CapturedThrough = captured; AcceptedThrough = accepted; ClosedThrough = closed;
            WallEndpoint = wall; Context = context;
        }
        internal static ForegroundObservation Capture(TimeService time, DetachedCanonicalSaveSession predecessor)
        {
            if (predecessor == null) throw new ArgumentException("living.foreground.observation_invalid");
            var sample = time.ReadForegroundObservation();
            var cut = new ForegroundObservation(predecessor.GetCurrentBytes(), sample.Through, sample.Accepted,
                sample.Closed, sample.Wall, sample.Context);
            time.CommitForegroundObservation(sample.Sample, sample.Through, sample.Wall);
            return cut;
        }
#if UNITY_EDITOR
        internal static ForegroundObservation ForDeterministicServiceTest(byte[] predecessor, long through, long accepted, long closed,
            UnixTimePoint wall, ForegroundObservationContext context) => new ForegroundObservation(predecessor, through, accepted, closed, wall, context);
#endif
        internal bool Matches(DetachedCanonicalSaveSession session, SaveData current) =>
            session != null && SpatialContractSha256.Compute(session.GetCurrentBytes()) == PredecessorSha256 &&
            AcceptedThrough == (current?.livingDungeon?.Scheduler?.AcceptedThrough ?? -1) &&
            ClosedThrough == (current?.livingDungeon?.Scheduler?.ClosedThrough ?? -1);
        internal bool Authorizes(DetachedCanonicalSaveSession session, SaveData current, long requestedThrough) =>
            requestedThrough == CapturedThrough && Matches(session, current);
    }
}
