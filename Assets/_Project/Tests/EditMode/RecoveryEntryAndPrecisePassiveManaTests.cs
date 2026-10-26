#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class RecoveryEntryAndPrecisePassiveManaTests
    {
        private sealed class Clock : IPreciseTimeSource
        {
            internal UnixTimePoint Now;
            internal int PreciseReads;
            public UnixTimePoint UtcNow() { PreciseReads++; return Now; }
            public long UtcNowUnixSeconds() => throw new AssertionException("Split observation requested");
        }

        private static Fixture OneFloor(double wallet = 0)
        {
            var f = Fixture.Create(null);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)));
            f.Runtime.structureRuntime.Heat = f.Configuration.HeatPeaceMinimum;
            f.Runtime.structureRuntime.ManaReserve = wallet;
            f.Runtime.lastSavedUtcUnix = 1000;
            return f;
        }
        private static CanonicalPassiveManaService Online(Fixture f) => new CanonicalPassiveManaService(
            PhaseFourTestSupport.PassiveMana(f.Profile.Canonical), f.Economy,
            new FormulaEngine(), f.Profile.Canonical.Spatial, 10);
        private static CanonicalOfflinePassiveManaService Offline(Fixture f) => new CanonicalOfflinePassiveManaService(
            PhaseFourTestSupport.PassiveMana(f.Profile.Canonical), Online(f));
        private static TimeProvenance Provenance(Fixture f) =>
            DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context).TimeProvenance;

        [TestCase(1000L, 1, 1000L, 2, 1L, 0.00000000075d)]
        [TestCase(1000L, 1000000, 1000L, 6000000, 5000000L, 0.00375d)]
        [TestCase(1000L, 9999999, 1001L, 0, 1L, 0.00000000075d)]
        [TestCase(1000L, 9000000, 1003L, 1000000, 22000000L, 0.0165d)]
        [TestCase(1000L, 0, 91000L, 5000000, 900005000000L, 675.00375d)]
        public void PreciseIntervalsUseCheckedIntegerDifference(long start, int fraction,
            long end, int endFraction, long units, double award)
        {
            var f = OneFloor();
            var result = Offline(f).CalculatePrecise(f.Runtime, f.Configuration,
                new UnixTimePoint(start, fraction), new UnixTimePoint(end, endFraction));
            Assert.That(result.Reason, Is.EqualTo(OfflinePassiveManaReason.Applied));
            Assert.That(result.Elapsed100ns, Is.EqualTo(units));
            Assert.That(result.CalculatedPreCapacityAward, Is.EqualTo(award).Within(1e-12));
            Assert.That(result.WalletAfter, Is.EqualTo(award).Within(1e-12));
        }

        [TestCase(1L)] [TestCase(3599L)] [TestCase(3600L)] [TestCase(90000L)]
        [TestCase(900719927L)] [TestCase(253402299799L)]
        public void WholeSecondResultsAreExactlyLegacyIncludingLongIntervals(long elapsed)
        {
            var f = OneFloor(12.5d); var service = Offline(f);
            var legacy = service.Resolve(f.Runtime, f.Configuration, 1000 + elapsed);
            var precise = service.CalculatePrecise(f.Runtime, f.Configuration,
                new UnixTimePoint(1000, 0), new UnixTimePoint(1000 + elapsed, 0));
            Assert.That(precise.Elapsed100ns, Is.EqualTo(checked(elapsed * UnixTimePoint.UnitsPerSecond)));
            Assert.That(precise.Reason, Is.EqualTo(legacy.Reason));
            Assert.That(precise.CalculatedPreCapacityAward, Is.EqualTo(legacy.CalculatedPreCapacityAward));
            Assert.That(precise.WalletAfter, Is.EqualTo(legacy.WalletAfter));
            Assert.That(precise.ActualAwardedMana, Is.EqualTo(legacy.ActualAwardedMana));
            Assert.That(precise.CapacityLimited, Is.EqualTo(legacy.CapacityLimited));
            Assert.That(precise.ApplicableOnlineManaPerHour, Is.EqualTo(legacy.ApplicableOnlineManaPerHour));
        }

        [TestCase(12.5d, 12.50375d, false, OfflinePassiveManaReason.Applied)]
        [TestCase(999.999d, 1000d, true, OfflinePassiveManaReason.Applied)]
        [TestCase(1000d, 1000d, true, OfflinePassiveManaReason.AlreadyAtCapacity)]
        public void FractionalWalletAndCapacityArePreserved(double wallet, double expected,
            bool limited, OfflinePassiveManaReason reason)
        {
            var f = OneFloor(wallet);
            var result = Offline(f).CalculatePrecise(f.Runtime, f.Configuration,
                new UnixTimePoint(1000, 0), new UnixTimePoint(1000, 5000000));
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.WalletAfter, Is.EqualTo(expected).Within(1e-12));
            Assert.That(result.CapacityLimited, Is.EqualTo(limited));
            Assert.That(result.ActualAwardedMana, Is.EqualTo(expected - wallet).Within(1e-12));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void SameCanonicalRateAndConfiguredEfficiencyWithExplicitModifier(int tier)
        {
            var f = OneFloor();
            f.Runtime.structureRuntime.Heat = tier == 0 ? f.Configuration.HeatPeaceMinimum :
                tier == 1 ? f.Configuration.HeatNoticeMinimum : f.Configuration.HeatConcernMinimum;
            var service = Offline(f); var start = new UnixTimePoint(1000, 5000000);
            var end = new UnixTimePoint(4600, 5000000);
            var basic = service.CalculatePrecise(f.Runtime, f.Configuration, start, end);
            var modified = service.CalculatePrecise(f.Runtime, f.Configuration, start, end, 0.3d);
            Assert.That(basic.ConfiguredBaseOfflineEfficiency, Is.EqualTo(0.15d));
            Assert.That(basic.EffectiveOfflineEfficiency, Is.EqualTo(0.15d));
            Assert.That(basic.ApplicableOnlineManaPerHour, Is.EqualTo(Online(f).ResolveRate(f.Runtime, f.Configuration).ManaPerHour));
            Assert.That(modified.EffectiveOfflineEfficiency, Is.EqualTo(0.3d));
            Assert.That(modified.ConfiguredBaseOfflineEfficiency, Is.EqualTo(basic.ConfiguredBaseOfflineEfficiency));
            Assert.That(modified.EffectiveOfflineManaPerHour, Is.EqualTo(basic.EffectiveOfflineManaPerHour * 2));
            Assert.That(modified.CalculatedPreCapacityAward, Is.EqualTo(basic.CalculatedPreCapacityAward * 2));
        }

        [TestCase(0, 1, OfflinePassiveManaReason.ZeroElapsedInterval)]
        [TestCase(0, 0, OfflinePassiveManaReason.BackwardClockMovement)]
        [TestCase(-1, 0, OfflinePassiveManaReason.CurrentTimestampInvalid)]
        public void ZeroReversedAndUnsupportedEndpointRejectWithoutAward(int secondDelta, int fraction,
            OfflinePassiveManaReason reason)
        {
            var f = OneFloor(7.25d);
            var result = Offline(f).CalculatePrecise(f.Runtime, f.Configuration,
                new UnixTimePoint(1, 1), new UnixTimePoint(1 + secondDelta, fraction));
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.ActualAwardedMana, Is.Zero);
            Assert.That(result.WalletAfter, Is.EqualTo(7.25d));
        }

        [Test]
        public void MissingUnsupportedUnrepresentableAndOverflowedInputsCannotCalculate()
        {
            var f = OneFloor(); var service = Offline(f); var point = new UnixTimePoint(1000, 0);
            Assert.That(service.CalculatePrecise(f.Runtime, f.Configuration, null, point).Reason,
                Is.EqualTo(OfflinePassiveManaReason.SavedTimestampInvalid));
            Assert.That(service.CalculatePrecise(f.Runtime, f.Configuration, point, null).Reason,
                Is.EqualTo(OfflinePassiveManaReason.CurrentTimestampInvalid));
            Assert.That(service.CalculatePrecise(f.Runtime, f.Configuration, new UnixTimePoint(-1, 0), point).Reason,
                Is.EqualTo(OfflinePassiveManaReason.SavedTimestampInvalid));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnixTimePoint(UnixTimePoint.MaximumSeconds + 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnixTimePoint(1000, 10000000));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnixTimePoint(1000, -1));
            Assert.Throws<OverflowException>(() => point.Add100ns(long.MaxValue));
            // All validated positive endpoints fit signed 64-bit interval arithmetic.
            var longest = service.CalculatePrecise(f.Runtime, f.Configuration,
                new UnixTimePoint(1, 0), new UnixTimePoint(UnixTimePoint.MaximumSeconds, 9999999));
            Assert.That(longest.Elapsed100ns, Is.EqualTo(2534023007989999999L));
            Assert.That(longest.Reason, Is.EqualTo(OfflinePassiveManaReason.Applied));
        }

        [TestCase(-1d)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidEfficiencyFailsClosed(double efficiency)
        {
            var f = OneFloor(7.25d);
            var result = Offline(f).CalculatePrecise(f.Runtime, f.Configuration,
                new UnixTimePoint(1000, 0), new UnixTimePoint(1001, 1), efficiency);
            Assert.That(result.Reason, Is.EqualTo(OfflinePassiveManaReason.CanonicalStateOrConfigurationInvalid));
            Assert.That(result.ActualAwardedMana, Is.Zero);
            Assert.That(result.WalletAfter, Is.EqualTo(7.25d));
        }

        [TestCase(-1d)] [TestCase(1000.01d)] [TestCase(double.NaN)]
        public void InvalidWalletFailsClosed(double wallet)
        {
            var f = OneFloor(wallet);
            var result = Offline(f).CalculatePrecise(f.Runtime, f.Configuration,
                new UnixTimePoint(1000, 0), new UnixTimePoint(1001, 1));
            Assert.That(result.Reason, Is.EqualTo(OfflinePassiveManaReason.CanonicalStateOrConfigurationInvalid));
            Assert.That(result.ActualAwardedMana, Is.Zero);
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(wallet));
        }

        [TestCase((int)RecoveryEntryContext.KnownBackground)]
        [TestCase((int)RecoveryEntryContext.UnknownPostCrash)]
        public void RecoveryEntryRetainsOneCompleteSampleThroughProcessing(int classification)
        {
            var context = (RecoveryEntryContext)classification;
            var f = OneFloor(); var provenance = Provenance(f);
            var clock = new Clock { Now = new UnixTimePoint(1000, 1234567) };
            var entry = RecoveryEntryObservation.Capture(clock, provenance, context);
            clock.Now = new UnixTimePoint(9000, 7654321); // simulated lengthy accepted-work drain
            var result = Offline(f).CalculatePrecise(f.Runtime, f.Configuration,
                new UnixTimePoint(1000, 0), entry.WallEndpoint);
            Assert.That(entry.WallEndpoint, Is.EqualTo(new UnixTimePoint(1000, 1234567)));
            Assert.That(result.Elapsed100ns, Is.EqualTo(1234567));
            Assert.That(entry.Context, Is.EqualTo(context));
            Assert.That(clock.PreciseReads, Is.EqualTo(1));
            Assert.That(provenance.AccountedBoundary.Fraction100ns, Is.Zero);
        }

        [TestCase((int)RecoveryEntryContext.KnownForeground)]
        [TestCase((int)RecoveryEntryContext.PersistenceStalled)]
        [TestCase(999)]
        public void KnownForegroundStallsAndUnknownClassificationCannotBecomeAbsence(int classification)
        {
            var context = (RecoveryEntryContext)classification;
            var f = OneFloor(); var clock = new Clock { Now = new UnixTimePoint(1000, 1) };
            Assert.Throws<ArgumentException>(() => RecoveryEntryObservation.Capture(clock, Provenance(f), context));
            Assert.That(clock.PreciseReads, Is.Zero);
        }

        [Test]
        public void InvalidBackwardAndMissingRecoveryObservationsAreRejected()
        {
            var f = OneFloor(); var provenance = Provenance(f);
            var clock = new Clock { Now = provenance.AccountedBoundary.Add100ns(-1) };
            Assert.Throws<ArgumentException>(() => RecoveryEntryObservation.Capture(clock, provenance, RecoveryEntryContext.UnknownPostCrash));
            clock.Now = null;
            Assert.Throws<ArgumentException>(() => RecoveryEntryObservation.Capture(clock, provenance, RecoveryEntryContext.UnknownPostCrash));
            clock.Now = new UnixTimePoint(0, 1);
            Assert.Throws<ArgumentException>(() => RecoveryEntryObservation.Capture(clock, provenance, RecoveryEntryContext.UnknownPostCrash));
            Assert.Throws<ArgumentException>(() => RecoveryEntryObservation.Capture(null, provenance, RecoveryEntryContext.UnknownPostCrash));
            Assert.Throws<ArgumentException>(() => RecoveryEntryObservation.Capture(clock, null, RecoveryEntryContext.UnknownPostCrash));
            clock.Now = provenance.AccountedBoundary;
            Assert.That(RecoveryEntryObservation.Capture(clock, provenance, RecoveryEntryContext.UnknownPostCrash).WallEndpoint,
                Is.EqualTo(provenance.AccountedBoundary));
        }

        [Test]
        public void CalculationAndObservationCannotPublishOrMutateCanonicalState()
        {
            var f = OneFloor(12.5d); string runtime = JsonUtility.ToJson(f.Runtime);
            byte[] session = f.Session.GetCurrentBytes(), disk = f.FileSystem.ReadAllBytes(f.ActivePath);
            var entry = RecoveryEntryObservation.Capture(new Clock { Now = new UnixTimePoint(1000, 5000000) },
                Provenance(f), RecoveryEntryContext.UnknownPostCrash);
            var calculated = Offline(f).CalculatePrecise(f.Runtime, f.Configuration,
                new UnixTimePoint(1000, 0), entry.WallEndpoint);
            Assert.That(calculated.WalletAfter, Is.EqualTo(12.50375d).Within(1e-12));
            Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(runtime));
            CollectionAssert.AreEqual(session, f.Session.GetCurrentBytes());
            CollectionAssert.AreEqual(disk, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(typeof(OfflinePassiveManaResult).IsAssignableFrom(calculated.GetType()), Is.False);
            Assert.That(calculated.GetType().GetProperties(System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).All(p => p.SetMethod == null), Is.True);

            // Only the existing whole-second grant can enter the actual writer.
            var legacy = Offline(f).Resolve(f.Runtime, f.Configuration, entry.WallEndpoint.Seconds);
            Assert.That(legacy.Reason, Is.EqualTo(OfflinePassiveManaReason.ZeroElapsedInterval));
            Assert.That(legacy.PersistenceRequired, Is.False);
            legacy = Offline(f).Resolve(f.Runtime, f.Configuration, 1001);
            var write = f.Authority.SaveOfflinePassiveMana(f.ActivePath, f.FileSystem, f.Session,
                f.Runtime, legacy.WalletAfter, legacy.SourceSavedUtcUnix, legacy.ObservedCurrentUtcUnix);
            Assert.That(write.IsSuccess, Is.True, write.Reason);
            Assert.That(write.RuntimeProjection.structureRuntime.ManaReserve, Is.EqualTo(12.5075d).Within(1e-12));
            var reopened = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(write.GetPersistedBytes(), f.Context);
            Assert.That(reopened.CurrentTargetValidated, Is.True);
            Assert.That(reopened.TimeProvenance.AccountedBoundary, Is.EqualTo(new UnixTimePoint(1001, 0)));
            Assert.That(Encoding.UTF8.GetString(write.GetPersistedBytes()), Does.Not.Contain("RecoveryEntry"));
        }
    }
}
#endif
