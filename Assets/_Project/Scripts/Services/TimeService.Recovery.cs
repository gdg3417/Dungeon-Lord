using System;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0
{
    public partial class TimeService
    {
        // One bounded process-local summary. No monotonic reading or interval
        // history is serialized; the canonical owner's W remains authoritative.
        private bool recoveryClockActive, recoveryFailureLatched;
        private UnixTimePoint recoveryWall, recoveryCoveredThrough;
        private double recoveryMonotonic;
        private long recoveryEligible100ns;
        private RecoveryEntryObservation retainedRecoveryEntry;
        private RecoveryPublicationObservation recoveryPublication;
        private string recoveryExpectedPredecessorSha256;
        private string recoveryBoundContextSha256;
        private long recoveryConditionalUnits, recoveryConditionalBackground100ns;
        private bool recoveryConditionalInterrupted, recoveryConditionalBackgroundObserved;
        private UnixTimePoint recoveryConditionalPrefixEnd;

        internal bool RecoveryClockActive => recoveryClockActive;
        internal bool RecoveryFailureLatched => recoveryFailureLatched;
        internal RecoveryEntryObservation RetainedRecoveryEntry => retainedRecoveryEntry;
        internal long RecoveryOriginalEligible100ns { get; private set; }
        private bool ObservedForeground => foregroundFocused && !_isPaused;
        internal void BindRecoveryContext(string context)
        {
            if (!SpatialContractSha256.IsCanonical(context) || recoveryBoundContextSha256 != null && recoveryBoundContextSha256 != context)
                throw new InvalidOperationException(RecoveryEpisode.ContextMismatch);
            recoveryBoundContextSha256 = context;
        }

        internal sealed class RecoveryPublicationObservation
        {
            internal readonly UnixTimePoint Through;
            internal readonly long Eligible100ns;
            internal readonly string PredecessorSha256;
            internal readonly bool Closure, StartedStalled;
            private readonly string expectedContext;
            internal string CandidateSha256 { get; private set; }
            internal TimeProvenance CandidateTime { get; private set; }
            internal RecoveryPublicationObservation(UnixTimePoint through, long eligible, byte[] predecessor,
                bool closure, bool stalled, string context)
            { Through = through; Eligible100ns = eligible; PredecessorSha256 = SpatialContractSha256.Compute(predecessor);
              Closure = closure; StartedStalled = stalled; expectedContext = context; }
            internal void Bind(byte[] candidate, TimeProvenance time)
            {
                if (CandidateSha256 != null || candidate == null || time?.Mode != TimeProvenance.PreciseMode ||
                    expectedContext != null && expectedContext != time.RecoveryContextSha256 ||
                    time.AccountedBoundary.CompareTo(Through) != 0 || Closure && time.Episode != null)
                    throw new InvalidOperationException(RecoveryEpisode.Invalid);
                CandidateSha256 = SpatialContractSha256.Compute(candidate); CandidateTime = time;
            }
        }

        // Reopen of a durable episode never captures a replacement R. The unknown
        // suffix after its W joins E; its original fields remain in canonical state.
        internal RecoveryEntryObservation BeginRecoveryClock(TimeProvenance proven)
        {
            if (proven == null || recoveryClockActive || recoveryDeferredForegroundWall != null)
                throw new InvalidOperationException(RecoveryEpisode.Invalid);
            var sample = ReadRecoverySample(proven.AccountedBoundary);
            if (proven.RecoveryContextSha256 != null) BindRecoveryContext(proven.RecoveryContextSha256);
            recoveryClockActive = true; recoveryFailureLatched = false;
            recoveryWall = sample.Wall; recoveryMonotonic = sample.Monotonic;
            recoveryCoveredThrough = proven.AccountedBoundary;
            recoveryExpectedPredecessorSha256 = null;
            recoveryEligible100ns = proven.Episode == null ? 0 : sample.Wall.Elapsed100nsSince(proven.AccountedBoundary);
            RecoveryOriginalEligible100ns = proven.Episode?.OriginalEligible100ns ?? sample.Wall.Elapsed100nsSince(proven.AccountedBoundary);
            retainedRecoveryEntry = proven.Episode == null
                ? RecoveryEntryObservation.FromCaptured(sample.Wall, proven, RecoveryEntryContext.UnknownPostCrash) : null;
            return retainedRecoveryEntry;
        }

        private (UnixTimePoint Wall, double Monotonic, long Units) ReadRecoverySample(UnixTimePoint minimum)
        {
            double monotonic = _clock.MonotonicSeconds;
            var wall = _clock.CaptureUtc();
            double units = HasForegroundClock ? Math.Floor(monotonic * foregroundUnitsPerSecond) : 0;
            if (!_clock.HasPreciseUtc || double.IsNaN(monotonic) || double.IsInfinity(monotonic) || monotonic < 0 ||
                (recoveryClockActive || recoveryDeferredForegroundWall != null) && monotonic < recoveryMonotonic ||
                wall == null || wall.Seconds <= 0 || minimum != null && wall.CompareTo(minimum) < 0 ||
                double.IsNaN(units) || double.IsInfinity(units) || units < 0 || units >= long.MaxValue)
                throw new InvalidOperationException("living.foreground.clock_invalid");
            return (wall, monotonic, checked((long)units));
        }

        private (UnixTimePoint Wall, double Monotonic, long Units) ObserveRecoveryClock()
        {
            if (!recoveryClockActive) throw new InvalidOperationException(RecoveryEpisode.Invalid);
            var sample = ReadRecoverySample(recoveryWall);
            long elapsed = sample.Wall.Elapsed100nsSince(recoveryWall);
            long eligible = !recoveryFailureLatched || !ObservedForeground ? elapsed : 0;
            long total = checked(recoveryEligible100ns + eligible);
            long conditional = recoveryConditionalUnits, background = recoveryConditionalBackground100ns;
            if (recoveryPublication?.Closure == true) {
                if (!ObservedForeground) background = checked(background + elapsed);
                if (!recoveryPublication.StartedStalled && !recoveryConditionalInterrupted && ObservedForeground) {
                    long before = checked((long)Math.Floor(recoveryMonotonic * foregroundUnitsPerSecond));
                    conditional = checked(conditional + checked(sample.Units - before));
                }
            }
            // Validation and all checked arithmetic precede mutation.
            recoveryEligible100ns = total; recoveryConditionalUnits = conditional;
            recoveryConditionalBackground100ns = background;
            recoveryWall = sample.Wall; recoveryMonotonic = sample.Monotonic;
            return sample;
        }

        internal RecoveryPublicationObservation CaptureRecoveryPublication(byte[] predecessor,
            TimeProvenance proven, bool closeResidual = false)
        {
            if (!recoveryClockActive || recoveryPublication != null || predecessor == null || proven == null ||
                proven.AccountedBoundary.CompareTo(recoveryCoveredThrough) != 0 ||
                recoveryExpectedPredecessorSha256 != null &&
                    SpatialContractSha256.Compute(predecessor) != recoveryExpectedPredecessorSha256 ||
                closeResidual && (!HasForegroundClock || proven.Episode?.Phase != RecoveryEpisode.ResidualPending))
                throw new InvalidOperationException(RecoveryEpisode.Invalid);
            var sample = ObserveRecoveryClock();
            recoveryExpectedPredecessorSha256 = SpatialContractSha256.Compute(predecessor);
            recoveryPublication = new RecoveryPublicationObservation(sample.Wall, recoveryEligible100ns, predecessor,
                closeResidual, recoveryFailureLatched, proven.RecoveryContextSha256);
            recoveryConditionalUnits = 0; recoveryConditionalBackground100ns = 0;
            recoveryConditionalBackgroundObserved = !ObservedForeground;
            recoveryConditionalInterrupted = !ObservedForeground || recoveryFailureLatched;
            recoveryConditionalPrefixEnd = recoveryConditionalInterrupted ? sample.Wall : null;
            return recoveryPublication;
        }

        private void RecoveryLifecycleObservation(bool nextFocused, bool nextPaused)
        {
            if (!recoveryClockActive) {
                if (HasForegroundClock && _save?.timeProvenance?.Mode == TimeProvenance.PreciseMode &&
                    ObservedForeground && (!nextFocused || nextPaused) &&
                    (recoveryDeferredForegroundWall == null || !recoveryDeferredHasBackground && !recoveryDeferredStalled)) {
                    var minimum = _save.timeProvenance.AccountedBoundary;
                    if (foregroundWall != null && foregroundWall.CompareTo(minimum) > 0) minimum = foregroundWall;
                    var pause = ReadRecoverySample(minimum);
                    if (pause.Units < foregroundSample) throw new InvalidOperationException("living.foreground.clock_invalid");
                    long through = checked(foregroundThrough + checked(pause.Units - foregroundSample));
                    foregroundThrough = through; foregroundSample = pause.Units; foregroundWall = pause.Wall;
                    recoveryDeferredForegroundWall = pause.Wall; recoveryDeferredForegroundThrough = through;
                    recoveryDeferredBackgroundWall = pause.Wall; recoveryDeferredBackground100ns = 0;
                    recoveryDeferredHasBackground = true; recoveryDeferredPreparing = false;
                    recoveryMonotonic = pause.Monotonic;
                }
                if (recoveryDeferredForegroundWall != null) {
                    ObserveDeferredRecovery();
                    if (!nextFocused || nextPaused) recoveryDeferredHasBackground = true;
                }
                return;
            }
            var sample = ObserveRecoveryClock();
            if (recoveryPublication?.Closure == true && (!nextFocused || nextPaused)) {
                recoveryConditionalBackgroundObserved = true;
                if (!recoveryConditionalInterrupted) {
                    recoveryConditionalInterrupted = true;
                    recoveryConditionalPrefixEnd = sample.Wall;
                }
            }
        }

        internal void RecoveryPublicationFailed(RecoveryPublicationObservation observation)
        {
            if (observation == null || !ReferenceEquals(observation, recoveryPublication)) throw new InvalidOperationException(RecoveryEpisode.Invalid);
            ObserveRecoveryClock();
            recoveryFailureLatched = true;
            recoveryPublication = null; // Attempted S grants no authority or foreground credit.
            recoveryConditionalUnits = 0; recoveryConditionalBackground100ns = 0;
        }

        internal void RecoveryPublicationUncertain(RecoveryPublicationObservation observation)
        {
            if (observation == null || !ReferenceEquals(observation, recoveryPublication)) throw new InvalidOperationException(RecoveryEpisode.Invalid);
            var sample = ObserveRecoveryClock();
            recoveryFailureLatched = true;
            if (observation.Closure && !recoveryConditionalInterrupted) {
                recoveryConditionalInterrupted = true; recoveryConditionalPrefixEnd = sample.Wall;
            }
            // Retain the one conditional suffix until independent authority proof.
        }

        internal void RecoveryPublicationSucceeded(RecoveryPublicationObservation observation, DetachedCanonicalWriteResult proof)
        {
            if (proof?.IsSuccess != true || proof.Session == null || proof.Validation?.CurrentTargetValidated != true ||
                proof.Validation.TimeProvenance == null ||
                SpatialContractSha256.Compute(proof.Session.GetCurrentBytes()) != observation?.CandidateSha256 ||
                !proof.Validation.TimeProvenance.SameAs(observation.CandidateTime))
                throw new InvalidOperationException(RecoveryEpisode.Invalid);
            CompleteRecoveryPublication(observation);
        }

        private void CompleteRecoveryPublication(RecoveryPublicationObservation observation)
        {
            if (observation == null || !ReferenceEquals(observation, recoveryPublication) || observation.CandidateSha256 == null)
                throw new InvalidOperationException(RecoveryEpisode.Invalid);
            var sample = ObserveRecoveryClock();
            if (observation.Eligible100ns > recoveryEligible100ns) throw new InvalidOperationException(RecoveryEpisode.Invalid);
            if (observation.Closure) {
                long next = checked(foregroundThrough + recoveryConditionalUnits);
                // The foreground owner receives only proven eligible monotonic
                // units, once. Q itself never updates the canonical owner's W.
                foregroundThrough = next; foregroundSample = sample.Units;
                if (recoveryConditionalBackgroundObserved) {
                    recoveryDeferredForegroundWall = recoveryConditionalPrefixEnd ?? observation.Through;
                    recoveryDeferredForegroundThrough = next;
                    recoveryDeferredBackground100ns = recoveryConditionalBackground100ns;
                    recoveryDeferredBackgroundWall = sample.Wall;
                    recoveryDeferredHasBackground = true; recoveryDeferredPreparing = false; recoveryDeferredStalled = false;
                }
                recoveryClockActive = false; recoveryEligible100ns = 0;
            } else {
                recoveryEligible100ns -= observation.Eligible100ns;
                recoveryCoveredThrough = observation.Through;
                recoveryExpectedPredecessorSha256 = observation.CandidateSha256;
            }
            recoveryFailureLatched = false; recoveryPublication = null;
            retainedRecoveryEntry = null;
        }

        // At most one complete foreground prefix and one grouped background sum
        // wait behind closure. Later lifecycle integration drains that prefix first.
        private UnixTimePoint recoveryDeferredForegroundWall, recoveryDeferredBackgroundWall;
        private long recoveryDeferredForegroundThrough, recoveryDeferredBackground100ns;
        private bool recoveryDeferredHasBackground, recoveryDeferredPreparing, recoveryDeferredStalled;
        internal bool HasDeferredRecovery => recoveryDeferredForegroundWall != null;
        private bool DeferredForegroundCanExtend => HasDeferredRecovery && !recoveryDeferredHasBackground && !recoveryDeferredStalled;

        // F is observed prospectively. All foreground through F is retained as
        // one full prefix; subsequent known foreground storage stalls earn no
        // offline credit. A lifecycle callback can still add observed background.
        internal void OrdinaryPublicationFailed()
        {
            if (recoveryClockActive || _save?.timeProvenance?.Mode != TimeProvenance.PreciseMode) return;
            if (!HasForegroundClock) throw new InvalidOperationException(RecoveryEpisode.Invalid);
            if (!HasDeferredRecovery || DeferredForegroundCanExtend) {
                var minimum = _save.timeProvenance.AccountedBoundary;
                if (foregroundWall != null && foregroundWall.CompareTo(minimum) > 0) minimum = foregroundWall;
                var sample = ReadRecoverySample(minimum);
                if (sample.Units < foregroundSample) throw new InvalidOperationException("living.foreground.clock_invalid");
                long through = ObservedForeground ? checked(foregroundThrough + checked(sample.Units - foregroundSample)) : foregroundThrough;
                foregroundThrough = through; foregroundSample = sample.Units; foregroundWall = sample.Wall;
                recoveryDeferredForegroundWall = sample.Wall; recoveryDeferredForegroundThrough = through;
                recoveryDeferredBackgroundWall = sample.Wall; recoveryDeferredBackground100ns = 0;
                recoveryDeferredHasBackground = !ObservedForeground; recoveryMonotonic = sample.Monotonic;
            } else ObserveDeferredRecovery();
            recoveryDeferredStalled = true;
        }

        internal void OrdinaryPublicationSucceeded(DetachedCanonicalWriteResult proof)
        {
            if (proof?.IsSuccess != true || proof.Session == null || proof.Validation?.CurrentTargetValidated != true ||
                proof.Validation.TimeProvenance?.Mode != TimeProvenance.PreciseMode ||
                !ExactCompleteSaveAtomicPersistence.Same(proof.GetPersistedBytes(), proof.Session.GetCurrentBytes()))
                throw new InvalidOperationException(RecoveryEpisode.Invalid);
            CompleteOrdinaryPublication(proof.Validation.TimeProvenance, proof.Validation.LivingDungeon);
        }

        private void CompleteOrdinaryPublication(TimeProvenance proven, LivingDungeonState owner)
        {
            if (recoveryClockActive || !HasDeferredRecovery) return;
            if (proven?.Mode != TimeProvenance.PreciseMode || proven.Episode != null ||
                recoveryBoundContextSha256 != null && proven.RecoveryContextSha256 != recoveryBoundContextSha256)
                throw new InvalidOperationException(RecoveryEpisode.Invalid);
            var sample = ObserveDeferredRecovery();
            if (recoveryDeferredStalled) foregroundSample = sample.Units; // Release at Q; never move W to Q.
            recoveryDeferredStalled = false;
            if (recoveryDeferredHasBackground) { recoveryDeferredPreparing = true; return; }
            if (owner?.IsClosed == true && owner.Scheduler.AcceptedThrough >= recoveryDeferredForegroundThrough &&
                proven.AccountedBoundary.CompareTo(recoveryDeferredForegroundWall) >= 0) {
                recoveryDeferredForegroundWall = null; recoveryDeferredBackgroundWall = null;
                recoveryDeferredPreparing = false; recoveryDeferredBackground100ns = 0;
            }
        }

        private (UnixTimePoint Wall, double Monotonic, long Units) ObserveDeferredRecovery()
        {
            var sample = ReadRecoverySample(recoveryDeferredBackgroundWall);
            bool eligible = !ObservedForeground || recoveryDeferredPreparing && !recoveryDeferredStalled;
            long delta = eligible ? sample.Wall.Elapsed100nsSince(recoveryDeferredBackgroundWall) : 0;
            long total = checked(recoveryDeferredBackground100ns + delta);
            recoveryDeferredBackground100ns = total; recoveryDeferredBackgroundWall = sample.Wall;
            recoveryMonotonic = sample.Monotonic; return sample;
        }

        internal void BeginDeferredRecoveryPreparation()
        {
            if (!HasDeferredRecovery || recoveryClockActive) throw new InvalidOperationException(RecoveryEpisode.Invalid);
            ObserveDeferredRecovery();
            if (recoveryDeferredHasBackground) recoveryDeferredPreparing = true;
        }

        internal RecoveryEntryObservation BeginObservedRecovery(SaveData proven)
        {
            if (!ReferenceEquals(proven, _save) || proven?.timeProvenance?.Mode != TimeProvenance.PreciseMode ||
                proven.timeProvenance.Episode != null || recoveryClockActive || !HasDeferredRecovery || !recoveryDeferredHasBackground ||
                proven.timeProvenance.AccountedBoundary.CompareTo(recoveryDeferredForegroundWall) != 0 ||
                proven.livingDungeon?.IsClosed != true || (proven.livingDungeon.Scheduler?.AcceptedThrough ?? -1) < recoveryDeferredForegroundThrough)
                throw new InvalidOperationException(RecoveryEpisode.Invalid);
            var sample = ObserveDeferredRecovery();
            var entry = RecoveryEntryObservation.FromCaptured(sample.Wall, proven.timeProvenance, RecoveryEntryContext.KnownBackground);
            recoveryClockActive = true; recoveryFailureLatched = recoveryDeferredStalled;
            recoveryCoveredThrough = proven.timeProvenance.AccountedBoundary; recoveryExpectedPredecessorSha256 = null;
            recoveryWall = sample.Wall; recoveryMonotonic = sample.Monotonic; recoveryEligible100ns = 0;
            retainedRecoveryEntry = entry; RecoveryOriginalEligible100ns = recoveryDeferredBackground100ns;
            recoveryDeferredForegroundWall = null; recoveryDeferredBackgroundWall = null;
            recoveryDeferredBackground100ns = 0; recoveryDeferredPreparing = false; recoveryDeferredHasBackground = false;
            return entry;
        }

#if UNITY_EDITOR
        // Clock-only deterministic tests supply explicit durability disposition;
        // they never claim a file write or grant authority from a digest alone.
        internal void CompleteRecoveryPublicationForClockTest(RecoveryPublicationObservation observation) => CompleteRecoveryPublication(observation);
        internal long RecoveryEligible100nsForClockTest => recoveryEligible100ns;
        internal UnixTimePoint RecoveryDeferredForegroundWallForClockTest => recoveryDeferredForegroundWall;
        internal long RecoveryDeferredBackground100nsForClockTest => recoveryDeferredBackground100ns;
        internal void CompleteOrdinaryPublicationForClockTest(TimeProvenance proven, LivingDungeonState owner) => CompleteOrdinaryPublication(proven, owner);
#endif
    }
}
