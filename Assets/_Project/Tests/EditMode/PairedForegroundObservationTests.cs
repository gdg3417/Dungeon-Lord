#if UNITY_EDITOR
using System;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class PairedForegroundObservationTests
    {
        internal sealed class PreciseClock : IPreciseTimeSource
        {
            internal UnixTimePoint Now = new UnixTimePoint(1000, 1);
            public UnixTimePoint UtcNow() => Now;
            public long UtcNowUnixSeconds() => Now.Seconds;
        }
        static TimeService Service(Fixture f, PreciseClock wall, Func<double> monotonic)
        {
            var time = new TimeService(new SimpleLogger(false), 10, 300, wall, monotonic);
            time.AttachSave(f.Runtime); time.BeginForeground(1000, -1); return time;
        }
        [Test] public void SameSecondForegroundAndBackgroundRetainCapturedFractionsAndEligibility()
        {
            var f = Fixture.Create(null); var wall = new PreciseClock(); double monotonic = 0;
            var time = Service(f, wall, () => monotonic);
            monotonic = .25; wall.Now = new UnixTimePoint(1000, 2500001);
            var foreground = time.CaptureForegroundObservation(f.Session);
            Assert.That(foreground.CapturedThrough, Is.EqualTo(250));
            Assert.That(foreground.WallEndpoint, Is.EqualTo(wall.Now));
            Assert.That(foreground.Context, Is.EqualTo(ForegroundObservationContext.EligibleForeground));
            time.SetForegroundFocus(false); monotonic = .75; wall.Now = new UnixTimePoint(1000, 7500001);
            var background = time.CaptureForegroundObservation(f.Session);
            Assert.That(background.CapturedThrough, Is.EqualTo(250));
            Assert.That(background.WallEndpoint.Seconds, Is.EqualTo(foreground.WallEndpoint.Seconds));
            // The foreground cut remains F. Observed background belongs to the
            // deferred recovery summary, never to a new foreground endpoint.
            Assert.That(background.WallEndpoint, Is.EqualTo(foreground.WallEndpoint));
            Assert.That(background.Context, Is.EqualTo(ForegroundObservationContext.EligibleForeground));
            Assert.That(time.HasDeferredBackground, Is.True);
            Assert.That(time.RecoveryDeferredEligible100nsForClockTest, Is.EqualTo(5000000));
            Assert.That(time.CaptureForegroundObservation(f.Session).WallEndpoint, Is.EqualTo(foreground.WallEndpoint));
            Assert.That(time.RecoveryDeferredEligible100nsForClockTest, Is.EqualTo(5000000));
        }
        [Test] public void IndependentUtcAndMonotonicMovementNeverGivesUtcExecutionAuthority()
        {
            var f = Fixture.Create(null); var wall = new PreciseClock(); double mono = 0;
            var time = Service(f, wall, () => mono);
            wall.Now = new UnixTimePoint(5000, 1);
            Assert.That(time.CaptureForegroundObservation(f.Session).CapturedThrough, Is.Zero);
            mono = 2;
            var observed = time.CaptureForegroundObservation(f.Session);
            Assert.That(observed.CapturedThrough, Is.EqualTo(2000));
            Assert.That(observed.WallEndpoint, Is.EqualTo(wall.Now));
            Assert.That(observed.AcceptedThrough, Is.EqualTo(-1)); Assert.That(observed.ClosedThrough, Is.EqualTo(-1));
            Assert.That(f.Runtime.livingDungeon.IsDormant, Is.True);
        }
        [Test] public void CapturedWorkIsDistinctFromDurablyAcceptedAndClosedWork()
        {
            var f = Fixture.Create(null); var wall = new PreciseClock(); double mono = 0;
            var time = Service(f, wall, () => mono); mono = 1;
            var cut = time.CaptureForegroundObservation(f.Session);
            Assert.That(cut.CapturedThrough, Is.EqualTo(1000)); Assert.That(cut.AcceptedThrough, Is.EqualTo(-1));
            Assert.That(cut.ClosedThrough, Is.EqualTo(-1)); Assert.That(cut.Matches(f.Session, f.Runtime), Is.True);
            Assert.That(cut.Authorizes(f.Session, f.Runtime, 1000), Is.True);
            Assert.That(cut.Authorizes(f.Session, f.Runtime, 500), Is.False);
            Assert.That(cut.Authorizes(f.Session, f.Runtime, 1001), Is.False);
            Assert.That(cut.WallEndpoint, Is.SameAs(wall.Now));
        }
        [Test] public void DuplicateCaptureIsStableAndInvalidClocksDoNotAdvanceSample()
        {
            var f = Fixture.Create(null); var wall = new PreciseClock(); double mono = 0;
            var time = Service(f, wall, () => mono); mono = 1;
            var first = time.CaptureForegroundObservation(f.Session); var duplicate = time.CaptureForegroundObservation(f.Session);
            Assert.That(duplicate.CapturedThrough, Is.EqualTo(first.CapturedThrough));
            Assert.That(duplicate.WallEndpoint, Is.EqualTo(first.WallEndpoint));
            mono = 2; wall.Now = new UnixTimePoint(1000, 0);
            Assert.Throws<InvalidOperationException>(() => time.CaptureForegroundObservation(f.Session));
            Assert.That(time.ForegroundThrough, Is.EqualTo(1000));
            wall.Now = new UnixTimePoint(1000, 2); mono = double.NaN;
            Assert.Throws<InvalidOperationException>(() => time.CaptureForegroundObservation(f.Session));
            mono = 2;
            Assert.That(time.CaptureForegroundObservation(f.Session).CapturedThrough, Is.EqualTo(2000));
            mono = 1;
            Assert.Throws<InvalidOperationException>(() => time.CaptureForegroundObservation(f.Session));
        }
        [Test] public void StorageElapsedTimeBelongsToTheNextCapturedCut()
        {
            var f = Fixture.Create(null); var wall = new PreciseClock(); double mono = 0;
            var time = Service(f, wall, () => mono); mono = .5;
            var authorized = time.CaptureForegroundObservation(f.Session);
            // A synchronous writer is busy; neither sampling source is frozen.
            mono = 1.75; wall.Now = new UnixTimePoint(1001, 7500000);
            Assert.That(authorized.CapturedThrough, Is.EqualTo(500));
            Assert.That(authorized.WallEndpoint, Is.EqualTo(new UnixTimePoint(1000, 1)));
            var next = time.CaptureForegroundObservation(f.Session);
            Assert.That(next.CapturedThrough, Is.EqualTo(1750));
            Assert.That(next.WallEndpoint, Is.EqualTo(wall.Now));
        }
        [Test] public void PauseResumeReanchorsWithoutBackgroundLogicalCredit()
        {
            var f = LivingDungeonTwoFloorTests.Dungeon(false); var wall = new PreciseClock(); double mono = 0;
            var save = LivingDungeonB3B2Tests.Service(f, wall, out var time, () => mono);
            mono = .25; time.OnPause();
            mono = .75; wall.Now = new UnixTimePoint(1000, 7500000);
            Assert.That(time.CaptureForegroundObservation(f.Session).CapturedThrough, Is.EqualTo(250));
            time.OnResume();
            Assert.That(time.CaptureForegroundObservation(save.CanonicalSession).CapturedThrough, Is.EqualTo(250),
                "Returning alone cannot bypass the observed-background recovery barrier");
            f.Accept(save.AdvanceForeground(f.Runtime, time.CaptureForegroundObservation(save.CanonicalSession),
                f.Context.RecoveryInputs.Bootstrap.tickSeconds, (_, __) => Assert.Fail()));
            save.BeginCanonicalRecovery(f.Runtime, observedBackground: true);
            var inputs = f.Context.RecoveryInputs;
            var mana = new CanonicalOfflinePassiveManaService(inputs.Passive,
                new CanonicalPassiveManaService(inputs.Passive, inputs.Economy, new FormulaEngine(),
                    f.Profile.Canonical.Spatial, inputs.Bootstrap.tickSeconds));
            for (int i = 0; i < 3; i++) f.Accept(save.AdvanceCanonicalRecovery(f.Runtime, mana, (_, __) => Assert.Fail()));
            Assert.That(f.Runtime.timeProvenance.Episode, Is.Null);
            Assert.That(time.RecoveryClockActive || time.HasDeferredRecovery, Is.False);
            mono = 1;
            Assert.That(time.CaptureForegroundObservation(f.Session).CapturedThrough, Is.EqualTo(500));
        }
        [Test] public void PredecessorPublicationInvalidatesOldObservation()
        {
            var f = Fixture.Create(null); var wall = new PreciseClock(); var time = Service(f, wall, () => 0);
            var old = time.CaptureForegroundObservation(f.Session);
            // Metadata publication changes predecessor bytes while retaining W;
            // an independent timestamp assignment is intentionally forbidden.
            var boundary = f.Runtime.timeProvenance.AccountedBoundary;
            f.Runtime.lastKnownAppState = "Paused";
            f.Accept(f.Authority.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
            Assert.That(f.Runtime.timeProvenance.AccountedBoundary, Is.EqualTo(boundary));
            Assert.That(old.Matches(f.Session, f.Runtime), Is.False);
        }
        [Test] public void ActualForegroundWriterConsumesCapturedEndpointAndRetainsLaterStorageTime()
        {
            var f = LivingDungeonTwoFloorTests.Dungeon(false); var wall = new PreciseClock(); double mono = 0;
            var save = LivingDungeonB3B2Tests.Service(f, wall, out var time, () => mono); mono = .25;
            var cut = time.CaptureForegroundObservation(save.CanonicalSession);
            var before = save.CanonicalSession.GetCurrentBytes();
            Assert.That(cut.Authorizes(save.CanonicalSession, f.Runtime, 125), Is.False);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(typeof(ForegroundObservation).GetConstructors(), Is.Empty);
            mono = .75; wall.Now = new UnixTimePoint(2000, 7500000);
            f.Accept(save.AdvanceForeground(f.Runtime, cut, 10, (candidate, tick) => { }));
            Assert.That(f.Runtime.lastSavedUtcUnix, Is.EqualTo(1000));
            Assert.That(f.Runtime.timeProvenance.AccountedBoundary, Is.EqualTo(cut.WallEndpoint));
            Assert.That(f.Runtime.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(250));
            before = f.Session.GetCurrentBytes();
            Assert.That(save.AdvanceForeground(f.Runtime, cut, 10, (candidate, tick) => { }).IsSuccess, Is.False);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            time.AttachSave(f.Runtime);
            var next = time.CaptureForegroundObservation(save.CanonicalSession);
            Assert.That(next.CapturedThrough, Is.EqualTo(750));
            Assert.That(next.WallEndpoint, Is.EqualTo(wall.Now));
        }
    }
}
#endif
