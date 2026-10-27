#if UNITY_EDITOR
using System;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class H1F4RecoveryClockTests : H1F4RecoveryTransitionTests
    {
        sealed class Clock : IPreciseTimeSource
        {
            internal UnixTimePoint Wall;
            internal double Mono;
            public UnixTimePoint UtcNow() => Wall;
            public long UtcNowUnixSeconds() => Wall.Seconds;
            internal void Set(long seconds, double mono, int fraction = 0) { Wall = new UnixTimePoint(seconds, fraction); Mono = mono; }
        }
        sealed class WholeClock : ITimeSource { public long UtcNowUnixSeconds() => 100; }
        static readonly string Context = new string('a', 64), Predecessor = new string('b', 64);
        static readonly byte[] Source = { 1, 2, 3 }, Candidate = { 4, 5, 6 };
        static TimeProvenance Closed(long seconds) => new TimeProvenance(new UnixTimePoint(seconds, 0), Context, null);
        static TimeProvenance Pending(long through, long residual, string phase = RecoveryEpisode.ResidualPending) =>
            new TimeProvenance(new UnixTimePoint(through, 0), Context, new RecoveryEpisode(Predecessor,
                new UnixTimePoint(80, 0), new UnixTimePoint(100, 0), RecoveryEpisode.UnknownPostCrash,
                RecoveryEpisode.Precise, phase, 200000000, residual));
        static TimeService Service(Clock clock, TimeProvenance time)
        {
            var service = new TimeService(new SimpleLogger(false), 10, 300, clock, () => clock.Mono);
            service.AttachSave(new SaveData { lastSavedUtcUnix = time.AccountedBoundary.Seconds, timeProvenance = time });
            service.BeginForeground(1000, -1); return service;
        }
        [Test] public void StableEntrySurvivesLongPublicationAndCoverageAcknowledgement()
        {
            var wall = new Clock(); wall.Set(100, 0); var prior = Closed(80); var time = Service(wall, prior);
            var entry = time.BeginRecoveryClock(prior); wall.Set(105, 5);
            var cut = time.CaptureRecoveryPublication(Source, prior);
            var next = Pending(105, 50000000, RecoveryEpisode.OriginalPending); cut.Bind(Candidate, next);
            wall.Set(108, 8); time.CompleteRecoveryPublicationForClockTest(cut);
            Assert.That(entry.WallEndpoint, Is.EqualTo(new UnixTimePoint(100, 0)));
            Assert.That(next.Episode.Entry, Is.EqualTo(entry.WallEndpoint));
            Assert.That(time.RecoveryEligible100nsForClockTest, Is.EqualTo(30000000));
            wall.Set(110, 10); var later = time.CaptureRecoveryPublication(Candidate, next);
            Assert.That(later.Eligible100ns, Is.EqualTo(50000000));
            Assert.That(time.ForegroundThrough, Is.Zero);
        }
        [Test] public void RepeatedReopenAddsOnlyUnknownSuffixAndNeverReplacesOriginalR()
        {
            var wall = new Clock(); wall.Set(120, 0); var persisted = Pending(106, 60000000, RecoveryEpisode.OriginalPending);
            var first = Service(wall, persisted);
            Assert.That(first.BeginRecoveryClock(persisted), Is.Null);
            Assert.That(first.CaptureRecoveryPublication(Source, persisted).Eligible100ns, Is.EqualTo(140000000));
            var secondPersisted = Pending(123, 230000000, RecoveryEpisode.OriginalPending);
            wall = new Clock(); wall.Set(130, 0); var second = Service(wall, secondPersisted);
            Assert.That(second.BeginRecoveryClock(secondPersisted), Is.Null);
            Assert.That(second.CaptureRecoveryPublication(Candidate, secondPersisted).Eligible100ns, Is.EqualTo(70000000));
            Assert.That(secondPersisted.Episode.Entry, Is.EqualTo(persisted.Episode.Entry));
            Assert.That(second.ForegroundThrough, Is.Zero);
        }
        [Test] public void SuccessfulResidualHandsMonotonicPrefixBackOnceWithoutAdvancingWToQ()
        {
            var wall = new Clock(); wall.Set(110, 0); var prior = Pending(110, 100000000); var time = Service(wall, prior);
            time.BeginRecoveryClock(prior); var cut = time.CaptureRecoveryPublication(Source, prior, true);
            cut.Bind(Candidate, Closed(110)); wall.Set(115, 4.5);
            time.CompleteRecoveryPublicationForClockTest(cut);
            Assert.That(time.ForegroundThrough, Is.EqualTo(4500));
            Assert.That(cut.CandidateTime.AccountedBoundary, Is.EqualTo(new UnixTimePoint(110, 0)));
            Assert.That(time.RecoveryClockActive, Is.False);
            Assert.Throws<InvalidOperationException>(() => time.CompleteRecoveryPublicationForClockTest(cut));
            Assert.That(time.ForegroundThrough, Is.EqualTo(4500));
        }
        [Test] public void ProvenRollbackAndRepeatedRetryExcludeForegroundStallsButRetainBackground()
        {
            var wall = new Clock(); wall.Set(110, 0); var prior = Pending(110, 100000000); var time = Service(wall, prior);
            time.BeginRecoveryClock(prior); var failed = time.CaptureRecoveryPublication(Source, prior, true);
            failed.Bind(Candidate, Closed(110)); wall.Set(112, 2); time.RecoveryPublicationFailed(failed);
            wall.Set(120, 10); time.SetForegroundFocus(false);
            wall.Set(130, 20); time.SetForegroundFocus(true);
            wall.Set(140, 30); var second = time.CaptureRecoveryPublication(Source, prior, true);
            Assert.That(second.Eligible100ns, Is.EqualTo(120000000));
            wall.Set(142, 32); time.RecoveryPublicationFailed(second);
            wall.Set(150, 40); var final = time.CaptureRecoveryPublication(Source, prior, true);
            Assert.That(final.StartedStalled, Is.True); Assert.That(final.Eligible100ns, Is.EqualTo(120000000));
            Assert.That(prior.Episode.ResidualEligible100ns + final.Eligible100ns, Is.EqualTo(220000000));
            final.Bind(Candidate, Closed(150)); wall.Set(153, 43); time.CompleteRecoveryPublicationForClockTest(final);
            Assert.That(time.ForegroundThrough, Is.Zero); Assert.That(time.RecoveryFailureLatched, Is.False);
        }
        [Test] public void DelayedFocusLossUsesItsActualObservationAndInventsNoHiddenTimestamp()
        {
            var wall = new Clock(); wall.Set(200, 0); var prior = Pending(200, 100000000); var time = Service(wall, prior);
            time.BeginRecoveryClock(prior); var cut = time.CaptureRecoveryPublication(Source, prior, true);
            cut.Bind(Candidate, Closed(200)); wall.Set(210, 10); time.SetForegroundFocus(false);
            time.CompleteRecoveryPublicationForClockTest(cut);
            Assert.That(time.ForegroundThrough, Is.EqualTo(10000));
            Assert.That(time.RecoveryDeferredForegroundWallForClockTest, Is.EqualTo(new UnixTimePoint(210, 0)));
            Assert.That(time.RecoveryDeferredBackground100nsForClockTest, Is.Zero);
        }
        [Test] public void ObservedBackgroundSegmentsRemainGroupedBehindOneForegroundPrefix()
        {
            var wall = new Clock(); wall.Set(200, 0); var prior = Pending(200, 100000000); var time = Service(wall, prior);
            time.BeginRecoveryClock(prior); var cut = time.CaptureRecoveryPublication(Source, prior, true);
            cut.Bind(Candidate, Closed(200)); wall.Set(204, 4); time.SetForegroundFocus(false);
            wall.Set(208, 8); time.SetForegroundFocus(true);
            wall.Set(210, 10); time.CompleteRecoveryPublicationForClockTest(cut);
            Assert.That(time.ForegroundThrough, Is.EqualTo(4000));
            Assert.That(time.RecoveryDeferredForegroundWallForClockTest, Is.EqualTo(new UnixTimePoint(204, 0)));
            Assert.That(time.RecoveryDeferredBackground100nsForClockTest, Is.EqualTo(40000000));
            wall.Set(212, 12); time.SetForegroundFocus(false);
            wall.Set(214, 14); time.SetForegroundFocus(true);
            Assert.That(time.RecoveryDeferredBackground100nsForClockTest, Is.EqualTo(60000000));
            Assert.That(time.SampleForeground(), Is.EqualTo(4000));
        }
        [Test] public void InvalidClocksCannotMutateSampleOrManufacturePreciseRecoveryEntry()
        {
            var wall = new Clock(); wall.Set(100, 10); var prior = Closed(80); var time = Service(wall, prior);
            var entry = time.BeginRecoveryClock(prior); wall.Set(99, 11);
            Assert.Throws<InvalidOperationException>(() => time.CaptureRecoveryPublication(Source, prior));
            Assert.That(time.RecoveryEligible100nsForClockTest, Is.Zero);
            wall.Set(101, 11); Assert.That(time.CaptureRecoveryPublication(Source, prior).Eligible100ns, Is.EqualTo(10000000));
            Assert.That(entry.WallEndpoint, Is.EqualTo(new UnixTimePoint(100, 0)));
            var whole = new TimeService(new SimpleLogger(false), 10, 300, new WholeClock(), () => 0);
            Assert.Throws<InvalidOperationException>(() => whole.BeginRecoveryClock(prior));
        }
        [Test] public void UnprovenPublicationCannotReleaseRecoveryOrAdmitConditionalTime()
        {
            var wall = new Clock(); wall.Set(110, 0); var prior = Pending(110, 100000000); var time = Service(wall, prior);
            time.BeginRecoveryClock(prior); var cut = time.CaptureRecoveryPublication(Source, prior, true);
            cut.Bind(Candidate, Closed(110));
            Assert.Throws<InvalidOperationException>(() => cut.Bind(Candidate, Closed(110)));
            Assert.Throws<InvalidOperationException>(() => time.RecoveryPublicationFailed(null));
            wall.Set(115, 5);
            Assert.Throws<InvalidOperationException>(() => time.RecoveryPublicationSucceeded(cut, null));
            Assert.That(time.ForegroundThrough, Is.Zero); Assert.That(time.RecoveryClockActive, Is.True);
            time.RecoveryPublicationUncertain(cut); wall.Set(125, 15);
            time.CompleteRecoveryPublicationForClockTest(cut);
            Assert.That(time.ForegroundThrough, Is.EqualTo(5000));
            Assert.That(cut.CandidateTime.AccountedBoundary, Is.EqualTo(new UnixTimePoint(110, 0)));
        }

        static LivingDungeonState ClosedOwner(long through) => new LivingDungeonState {
            Scheduler = new LivingDungeonScheduler { AcceptedThrough = through, ClosedThrough = through }
        };
        static SaveData Settled(TimeProvenance time, long through) => new SaveData {
            lastSavedUtcUnix = time.AccountedBoundary.Seconds, timeProvenance = time, livingDungeon = ClosedOwner(through)
        };
        [Test] public void RecoveryRetryRetainsOriginalEntryAndRejectsChangedPredecessor()
        {
            var clock = new Clock(); clock.Set(100, 0); var prior = Closed(80); var time = Service(clock, prior);
            var entry = time.BeginRecoveryClock(prior); var cut = time.CaptureRecoveryPublication(Source, prior);
            clock.Set(105, 5); time.RecoveryPublicationFailed(cut);
            Assert.That(time.RetainedRecoveryEntry, Is.SameAs(entry));
            Assert.Throws<InvalidOperationException>(() => time.CaptureRecoveryPublication(Candidate, prior));
            var retry = time.CaptureRecoveryPublication(Source, prior);
            Assert.That(retry.Eligible100ns, Is.EqualTo(50000000));
            Assert.That(time.RetainedRecoveryEntry.WallEndpoint, Is.EqualTo(new UnixTimePoint(100, 0)));
        }
        [Test] public void RecoveryContextCannotRebindDuringRetryOrAfterClosure()
        {
            var clock = new Clock(); clock.Set(110, 0); var prior = Pending(110, 0); var time = Service(clock, prior);
            time.BeginRecoveryClock(prior); time.BindRecoveryContext(Context);
            Assert.Throws<InvalidOperationException>(() => time.BindRecoveryContext(new string('c', 64)));
            var cut = time.CaptureRecoveryPublication(Source, prior, true); cut.Bind(Candidate, Closed(110));
            time.CompleteRecoveryPublicationForClockTest(cut);
            Assert.Throws<InvalidOperationException>(() => time.BindRecoveryContext(new string('c', 64)));
        }
        [Test] public void OrdinaryRollbackRetainsFullForegroundPrefixAndReleasesOnlyAtSuccessfulQ()
        {
            var clock = new Clock(); clock.Set(100, 0); var time = Service(clock, Closed(100));
            clock.Set(112, 12); time.OrdinaryPublicationFailed();
            clock.Set(140, 40); var retained = time.ReadForegroundObservation();
            Assert.That(retained.Through, Is.EqualTo(12000)); Assert.That(retained.Wall.Seconds, Is.EqualTo(112));
            Assert.That(time.RecoveryDeferredBackground100nsForClockTest, Is.Zero);
            clock.Set(143, 43); time.CompleteOrdinaryPublicationForClockTest(Closed(112), ClosedOwner(12000));
            Assert.That(time.HasDeferredRecovery, Is.False);
            clock.Set(145, 45); Assert.That(time.SampleForeground(), Is.EqualTo(14000));
        }
        [Test] public void OrdinaryRetryGroupsBackgroundAndExcludesKnownForegroundStalls()
        {
            var clock = new Clock(); clock.Set(100, 0); var time = Service(clock, Closed(100));
            clock.Set(112, 12); time.OrdinaryPublicationFailed();
            clock.Set(120, 20); time.SetForegroundFocus(false);
            clock.Set(130, 30); time.SetForegroundFocus(true);
            clock.Set(140, 40); time.OrdinaryPublicationFailed();
            clock.Set(143, 43); time.CompleteOrdinaryPublicationForClockTest(Closed(112), ClosedOwner(12000));
            var proven = Settled(Closed(112), 12000); time.AttachSave(proven);
            clock.Set(145, 45); var entry = time.BeginObservedRecovery(proven);
            Assert.That(entry.Context, Is.EqualTo(RecoveryEntryContext.KnownBackground));
            Assert.That(time.RecoveryOriginalEligible100ns, Is.EqualTo(120000000));
            Assert.That(time.ForegroundThrough, Is.EqualTo(12000));
            Assert.That(time.RecoveryEligible100nsForClockTest, Is.Zero);
        }
        [Test] public void OrdinaryPauseUsesObservedFullCutAndGroupsPreparationAfterReturn()
        {
            var clock = new Clock(); clock.Set(100, 0); var time = Service(clock, Closed(100));
            clock.Set(110, 10); time.SetForegroundFocus(false);
            clock.Set(120, 20); time.SetForegroundFocus(true); time.BeginDeferredRecoveryPreparation();
            clock.Set(125, 25); time.CompleteOrdinaryPublicationForClockTest(Closed(110), ClosedOwner(10000));
            var proven = Settled(Closed(110), 10000); time.AttachSave(proven);
            clock.Set(126, 26); var entry = time.BeginObservedRecovery(proven);
            Assert.That(entry.WallEndpoint.Seconds, Is.EqualTo(126));
            Assert.That(time.RecoveryOriginalEligible100ns, Is.EqualTo(160000000));
            Assert.That(time.ForegroundThrough, Is.EqualTo(10000));
        }
        [Test] public void MultipleObservedBackgroundSegmentsDoNotReplacePendingForegroundCut()
        {
            var clock = new Clock(); clock.Set(100, 0); var time = Service(clock, Closed(100));
            clock.Set(110, 10); time.SetForegroundFocus(false);
            clock.Set(120, 20); time.SetForegroundFocus(true);
            clock.Set(122, 22); time.SetForegroundFocus(false);
            clock.Set(126, 26); time.SetForegroundFocus(true);
            var cut = time.ReadForegroundObservation();
            Assert.That(cut.Wall.Seconds, Is.EqualTo(110)); Assert.That(cut.Through, Is.EqualTo(10000));
            Assert.That(time.RecoveryDeferredBackground100nsForClockTest, Is.EqualTo(140000000));
        }
        [Test] public void ObservedRecoveryRequiresExactClosedForegroundPrefixBeforeCapturingR()
        {
            var clock = new Clock(); clock.Set(100, 0); var time = Service(clock, Closed(100));
            clock.Set(110, 10); time.SetForegroundFocus(false);
            clock.Set(120, 20); time.SetForegroundFocus(true);
            var stale = Settled(Closed(100), 10000); time.AttachSave(stale);
            Assert.Throws<InvalidOperationException>(() => time.BeginObservedRecovery(stale));
            var unfinished = Settled(Closed(110), 10000); unfinished.livingDungeon.Scheduler.ClosedThrough = 9000;
            time.AttachSave(unfinished); Assert.Throws<InvalidOperationException>(() => time.BeginObservedRecovery(unfinished));
            Assert.That(time.RecoveryClockActive, Is.False); Assert.That(time.HasDeferredRecovery, Is.True);
        }
        [Test] public void InvalidOrdinaryFailureObservationDoesNotLoseEligibleForeground()
        {
            var clock = new Clock(); clock.Set(100, 10); var time = Service(clock, Closed(100));
            clock.Set(99, 11); Assert.Throws<InvalidOperationException>(() => time.OrdinaryPublicationFailed());
            Assert.That(time.HasDeferredRecovery, Is.False);
            clock.Set(102, 12); time.OrdinaryPublicationFailed();
            Assert.That(time.ReadForegroundObservation().Through, Is.EqualTo(2000));
            Assert.That(time.RecoveryDeferredForegroundWallForClockTest.Seconds, Is.EqualTo(102));
        }
    }
}
#endif
