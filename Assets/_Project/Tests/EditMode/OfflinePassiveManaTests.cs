#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    [Category("PhaseFourOfflinePassiveMana")]
    public class OfflinePassiveManaTests
    {
        private sealed class FakeTimeSource : IPreciseTimeSource
        {
            public long Now;
            public double Monotonic;
            public UnixTimePoint UtcNow() => new UnixTimePoint(Now, 0);
            public long UtcNowUnixSeconds() => Now;
        }

        [TestCase(CurrentHeatTierResolver.PeaceTierId, 27d)]
        [TestCase(CurrentHeatTierResolver.NoticeTierId, 25.65d)]
        [TestCase(CurrentHeatTierResolver.ConcernTierId, 22.95d)]
        public void OneHourUsesCanonicalHeatRateAtConfiguredFifteenPercent(
            string tierId, double expectedAward)
        {
            Fixture fixture = OneFloor();
            fixture.Runtime.lastSavedUtcUnix = 1000;
            fixture.Runtime.structureRuntime.ManaReserve = 0d;
            fixture.Runtime.structureRuntime.Heat = HeatFor(fixture.Configuration, tierId);
            CanonicalPassiveManaService online = Online(fixture);

            OfflinePassiveManaResult result = Offline(fixture, online).Resolve(
                fixture.Runtime, fixture.Configuration, 4600);

            Assert.That(result.Reason, Is.EqualTo(OfflinePassiveManaReason.Applied));
            Assert.That(result.ConfiguredBaseOfflineEfficiency, Is.EqualTo(0.15d));
            Assert.That(result.EffectiveOfflineEfficiency, Is.EqualTo(0.15d));
            Assert.That(result.ApplicableOnlineManaPerHour,
                Is.EqualTo(online.ResolveRate(fixture.Runtime, fixture.Configuration).ManaPerHour));
            Assert.That(result.ActualAwardedMana, Is.EqualTo(expectedAward).Within(0.000000001d));
        }

        [Test]
        public void ExactSecondsAndFractionalAwardArePreserved()
        {
            Fixture fixture = OneFloor();
            fixture.Runtime.lastSavedUtcUnix = 1000;
            fixture.Runtime.structureRuntime.ManaReserve = 12.5d;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;

            OfflinePassiveManaResult result = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, 1001);

            Assert.That(result.ObservedElapsedSeconds, Is.EqualTo(1));
            Assert.That(result.CalculatedPreCapacityAward, Is.EqualTo(0.0075d).Within(1e-12));
            Assert.That(result.WalletAfter, Is.EqualTo(12.5075d).Within(1e-12));
            Assert.That(result.ActualAwardedMana, Is.EqualTo(0.0075d).Within(1e-12));
            Assert.That(result.CapacityLimited, Is.False);
        }

        [Test]
        public void UatFractionalAwardBelowCapacityDoesNotReportCapacityLimit()
        {
            Fixture fixture = Fixture.Create(null);
            fixture.Runtime.lastSavedUtcUnix = 1000;
            fixture.Runtime.structureRuntime.ManaReserve = 0.423d;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;

            OfflinePassiveManaResult result = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, 1063);

            Assert.That(result.CanonicalRate.ActiveFloorCount, Is.Zero);
            Assert.That(result.EffectiveOfflineManaPerHour, Is.EqualTo(18d).Within(1e-12));
            Assert.That(result.ObservedElapsedSeconds, Is.EqualTo(63));
            Assert.That(result.CalculatedPreCapacityAward, Is.EqualTo(0.315d).Within(1e-12));
            Assert.That(result.ActualAwardedMana, Is.EqualTo(0.315d).Within(1e-12));
            Assert.That(result.WalletAfter, Is.EqualTo(0.738d).Within(1e-12));
            Assert.That(result.CapacityLimited, Is.False);

            var strings = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [OfflinePassiveManaPresenter.AppliedFormatKey] =
                    "Away {0:N0}; rate {1}; earned {2}; mana {3}/{4}",
                [OfflinePassiveManaPresenter.CapacityLimitedKey] = "Capacity limited"
            };
            string text = OfflinePassiveManaPresenter.Build(result.WithPersistence(
                    OfflinePassiveManaReason.Applied, true), CultureInfo.InvariantCulture,
                key => strings.TryGetValue(key, out string value) ? value : key);

            Assert.That(text, Does.Contain("Away 63; rate 18; earned 0.3; mana 0.7/1000"));
            Assert.That(text, Does.Not.Contain("Capacity limited"));
        }

        [Test]
        public void LongForwardIntervalBeyondDiagnosticDayUsesEverySecondWithoutCap()
        {
            Fixture fixture = OneFloor();
            fixture.Runtime.lastSavedUtcUnix = 1000;
            fixture.Runtime.structureRuntime.ManaReserve = 0d;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;
            const long elapsed = 90000;

            OfflinePassiveManaResult result = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, 1000 + elapsed);

            Assert.That(result.ObservedElapsedSeconds, Is.EqualTo(elapsed));
            Assert.That(result.CalculatedPreCapacityAward, Is.EqualTo(675d).Within(1e-9));
            Assert.That(result.ActualAwardedMana, Is.EqualTo(675d).Within(1e-9));
            Assert.That(result.CapacityLimited, Is.False);
        }

        [TestCase(990d, 1000d, 10d, true, OfflinePassiveManaReason.Applied)]
        [TestCase(999.75d, 1000d, 0.25d, true, OfflinePassiveManaReason.Applied)]
        [TestCase(1000d, 1000d, 0d, true, OfflinePassiveManaReason.AlreadyAtCapacity)]
        public void CapacityClampAndFullWalletProduceStructuredResult(double before,
            double after, double award, bool limited, OfflinePassiveManaReason reason)
        {
            Fixture fixture = OneFloor();
            fixture.Runtime.lastSavedUtcUnix = 1000;
            fixture.Runtime.structureRuntime.ManaReserve = before;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;

            OfflinePassiveManaResult result = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, 4600);

            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.WalletAfter, Is.EqualTo(after));
            Assert.That(result.ActualAwardedMana, Is.EqualTo(award));
            Assert.That(result.CapacityLimited, Is.EqualTo(limited));
            Assert.That(result.PersistenceRequired, Is.True);
        }

        [TestCase(1000, 1000, OfflinePassiveManaReason.ZeroElapsedInterval)]
        [TestCase(0, 1000, OfflinePassiveManaReason.SavedTimestampInvalid)]
        [TestCase(1000, 0, OfflinePassiveManaReason.CurrentTimestampInvalid)]
        [TestCase(1000, 999, OfflinePassiveManaReason.BackwardClockMovement)]
        public void TimestampFailuresAwardZero(long saved, long current,
            OfflinePassiveManaReason reason)
        {
            Fixture fixture = OneFloor();
            fixture.Runtime.lastSavedUtcUnix = saved;
            fixture.Runtime.structureRuntime.ManaReserve = 7.25d;

            OfflinePassiveManaResult result = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, current);

            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.ActualAwardedMana, Is.Zero);
            Assert.That(result.WalletAfter, Is.EqualTo(7.25d));
            Assert.That(result.PersistenceRequired, Is.False);
        }

        [TestCase(-1d)]
        [TestCase(1000.01d)]
        [TestCase(double.NaN)]
        public void InvalidWalletFailsWithoutMutation(double wallet)
        {
            Fixture fixture = OneFloor();
            fixture.Runtime.lastSavedUtcUnix = 1000;
            fixture.Runtime.structureRuntime.ManaReserve = wallet;

            OfflinePassiveManaResult result = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, 4600);

            Assert.That(result.Reason,
                Is.EqualTo(OfflinePassiveManaReason.CanonicalStateOrConfigurationInvalid));
            Assert.That(result.PersistenceRequired, Is.False);
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(wallet));
        }

        [Test]
        public void EffectiveEfficiencyIsAnExplicitFutureModifierSeam()
        {
            Fixture fixture = OneFloor();
            fixture.Runtime.lastSavedUtcUnix = 1000;
            fixture.Runtime.structureRuntime.ManaReserve = 0d;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;

            OfflinePassiveManaResult baseResult = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, 4600);
            OfflinePassiveManaResult modified = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, 4600, 0.3d);

            Assert.That(modified.ConfiguredBaseOfflineEfficiency, Is.EqualTo(0.15d));
            Assert.That(modified.EffectiveOfflineEfficiency, Is.EqualTo(0.3d));
            Assert.That(modified.ActualAwardedMana,
                Is.EqualTo(baseResult.ActualAwardedMana * 2d).Within(1e-9));
        }

        [Test]
        public void CalculationDoesNotMutateHeatTicksResearchGeometryOrOtherState()
        {
            Fixture fixture = OneFloor();
            fixture.Runtime.lastSavedUtcUnix = 1000;
            fixture.Runtime.structureRuntime.ManaReserve = 5d;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatNoticeMinimum;
            string runtimeBefore = JsonUtility.ToJson(fixture.Runtime);
            byte[] spatialBefore = CanonicalSpatialSaveSerializer.Serialize(
                fixture.State, fixture.Profile.Canonical).Value;

            OfflinePassiveManaResult result = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, 4600);

            Assert.That(result.Reason, Is.EqualTo(OfflinePassiveManaReason.Applied));
            Assert.That(JsonUtility.ToJson(fixture.Runtime), Is.EqualTo(runtimeBefore));
            CollectionAssert.AreEqual(spatialBefore, CanonicalSpatialSaveSerializer.Serialize(
                fixture.Runtime.validatedCanonicalSpatialState, fixture.Profile.Canonical).Value);
        }

        [Test]
        public void DetachedCommitPersistsWalletAndConsumedTimestampBeforePublication()
        {
            Fixture fixture = OneFloor(1000, 10d);
            OfflinePassiveManaResult calculated = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, 4600);
            SaveData liveBefore = fixture.Runtime;

            var service = Service(fixture, new FakeTimeSource { Now = 4600 });
            service.BeginCanonicalRecovery(fixture.Runtime);
            Assert.That(Step(fixture, service).IsSuccess, Is.True); // durable originalPending
            var original = Step(fixture, service);
            Assert.That(original.IsSuccess, Is.True, original.Reason);
            Assert.That(original.Validation.TimeProvenance.Episode.Phase, Is.EqualTo(RecoveryEpisode.ResidualPending));
            DetachedCanonicalWriteResult write = Step(fixture, service); // grouped residual and closure

            Assert.That(write.IsSuccess, Is.True, write.Reason);
            Assert.That(liveBefore.structureRuntime.ManaReserve, Is.EqualTo(10d));
            Assert.That(liveBefore.lastSavedUtcUnix, Is.EqualTo(1000));
            Assert.That(write.RuntimeProjection.structureRuntime.ManaReserve,
                Is.EqualTo(calculated.WalletAfter));
            Assert.That(write.RuntimeProjection.lastSavedUtcUnix, Is.EqualTo(4600));
            string persisted = Encoding.UTF8.GetString(write.GetPersistedBytes());
            Assert.That(persisted, Does.Contain("\"schemaVersion\":" + CanonicalSaveSchemaVersions.CurrentWritableTarget));
            Assert.That(persisted, Does.Not.Contain("OfflinePassiveMana"));
            Assert.That(persisted, Does.Not.Contain("BaseOfflineEfficiency"));
        }

        [Test]
        public void PersistenceFailureLeavesWalletTimestampDiskAndSessionUnchangedThenRetrySucceeds()
        {
            Fixture fixture = OneFloor(1000, 10d);
            OfflinePassiveManaResult calculated = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, 4600);
            var service = Service(fixture, new FakeTimeSource { Now = 4600 });
            service.BeginCanonicalRecovery(fixture.Runtime);
            Assert.That(Step(fixture, service).IsSuccess, Is.True);
            byte[] before = fixture.FileSystem.ReadAllBytes(fixture.ActivePath);
            var sessionBefore = service.CanonicalSession;
            var episodeBefore = fixture.Runtime.timeProvenance.Episode;
            fixture.FileSystem.EnableFailure(
                Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write, 2);

            DetachedCanonicalWriteResult failed = Step(fixture, service);

            Assert.That(failed.IsSuccess, Is.False);
            Assert.That(failed.RuntimeProjection, Is.Null);
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(10d));
            // Entry already accounts through R, but originalPending still owes D0.
            Assert.That(fixture.Runtime.lastSavedUtcUnix, Is.EqualTo(4600));
            Assert.That(fixture.Runtime.timeProvenance.Episode, Is.SameAs(episodeBefore));
            Assert.That(episodeBefore.Phase, Is.EqualTo(RecoveryEpisode.OriginalPending));
            Assert.That(episodeBefore.OriginalEligible100ns, Is.EqualTo(3600L * UnixTimePoint.UnitsPerSecond));
            CollectionAssert.AreEqual(before, fixture.FileSystem.ReadAllBytes(fixture.ActivePath));
            Assert.That(service.CanonicalSession, Is.SameAs(sessionBefore));
            CollectionAssert.AreEqual(before, service.CanonicalSession.GetCurrentBytes());

            fixture.FileSystem.DisableFailure();
            DetachedCanonicalWriteResult retry = Step(fixture, service);
            Assert.That(retry.IsSuccess, Is.True, retry.Reason);
            Assert.That(retry.RuntimeProjection.structureRuntime.ManaReserve,
                Is.EqualTo(calculated.WalletAfter));
            Assert.That(Step(fixture, service).IsSuccess, Is.True);
            fixture.Reopen();
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(calculated.WalletAfter));
            Assert.That(fixture.Runtime.timeProvenance.Episode, Is.Null);
        }

        [Test]
        public void StaleDiskSessionReturnsStableReasonWithoutWrite()
        {
            Fixture fixture = OneFloor(1000, 0d);
            var service = Service(fixture, new FakeTimeSource { Now = 4600 });
            service.BeginCanonicalRecovery(fixture.Runtime);
            Assert.That(Step(fixture, service).IsSuccess, Is.True);
            byte[] source = service.CanonicalSession.GetCurrentBytes();
            int writes = fixture.FileSystem.Operations.Count(o => o.Type == Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write);
            byte[] stale = Encoding.UTF8.GetBytes("{\"changed\":true}");
            fixture.FileSystem.Seed(fixture.ActivePath, stale);

            DetachedCanonicalWriteResult result = Step(fixture, service);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason,
                Is.EqualTo(DetachedCanonicalWriteAuthority.OfflineStaleSessionReason));
            CollectionAssert.AreEqual(stale, fixture.FileSystem.ReadAllBytes(fixture.ActivePath));
            CollectionAssert.AreEqual(source, service.CanonicalSession.GetCurrentBytes());
            Assert.That(fixture.FileSystem.Operations.Count(o => o.Type == Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write), Is.EqualTo(writes));
        }

        [Test]
        public void ColdStartCommitBootSaveAndFreshReopenCannotReplayInterval()
        {
            Fixture fixture = OneFloor(1000, 12.5d);
            var time = new FakeTimeSource { Now = 1001 };
            SaveService service = Service(fixture, time);
            GameObject go = new GameObject("Offline cold-start lifecycle");
            try
            {
                GameRoot root = Root(go, fixture, service, time);
                double heat = root.Save.structureRuntime.Heat;
                long ticks = root.Save.totalTicks;
                string research = JsonUtility.ToJson(root.Save.completedResearch);
                string runs = JsonUtility.ToJson(root.Save.runHistory);
                byte[] spatial = CanonicalSpatialSaveSerializer.Serialize(
                    fixture.State, fixture.Profile.Canonical).Value;

                service.BeginCanonicalRecovery(root.Save);
                DrainRecovery(root);
                Assert.That(root.Save.structureRuntime.ManaReserve,
                    Is.EqualTo(12.5075d).Within(1e-12));
                Assert.That(root.Save.lastSavedUtcUnix, Is.EqualTo(1001));
                Assert.That(root.LatestOfflinePassiveManaResultForTests, Is.Not.Null,
                    "Successful canonical recovery must retain the existing offline-mana presentation evidence");
                Assert.That(root.LatestOfflinePassiveManaResultForTests.Persisted, Is.True);
                Assert.That(service.Save(root.Save, SaveReason.Boot), Is.True);
                UnityEngine.Object.DestroyImmediate(go);

                fixture.Reopen();
                SaveData reopened = fixture.Runtime;
                var reopenedService = Service(fixture, time);
                GameObject reopenedGo = new GameObject("Offline cold-start reopened lifecycle");
                try
                {
                    GameRoot reopenedRoot = Root(reopenedGo, fixture, reopenedService, time);
                    reopenedService.BeginCanonicalRecovery(reopenedRoot.Save);
                    DrainRecovery(reopenedRoot);
                    Assert.That(reopenedRoot.LatestOfflinePassiveManaResultForTests.Reason,
                        Is.EqualTo(OfflinePassiveManaReason.ZeroElapsedInterval));
                    Assert.That(reopenedRoot.Save.structureRuntime.ManaReserve,
                        Is.EqualTo(12.5075d).Within(1e-12));
                    Assert.That(reopenedRoot.Save.structureRuntime.Heat, Is.EqualTo(heat));
                    Assert.That(reopenedRoot.Save.totalTicks, Is.EqualTo(ticks));
                    Assert.That(JsonUtility.ToJson(reopenedRoot.Save.completedResearch), Is.EqualTo(research));
                    Assert.That(JsonUtility.ToJson(reopenedRoot.Save.runHistory), Is.EqualTo(runs));
                    CollectionAssert.AreEqual(spatial, CanonicalSpatialSaveSerializer.Serialize(
                        reopenedRoot.Save.validatedCanonicalSpatialState,
                        fixture.Profile.Canonical).Value);
                    Assert.That(Encoding.UTF8.GetString(
                        reopenedService.CanonicalSession.GetCurrentBytes()),
                        Does.Contain("\"schemaVersion\":" + CanonicalSaveSchemaVersions.CurrentWritableTarget));
                    // Even a zero-duration reopen closes a real recovery episode;
                    // its durable publications replace the runtime projection.
                    Assert.That(reopenedRoot.Save.timeProvenance.Episode, Is.Null);
                    CollectionAssert.AreEqual(reopenedService.CanonicalSession.GetCurrentBytes(),
                        fixture.FileSystem.ReadAllBytes(fixture.ActivePath));
                }
                finally { UnityEngine.Object.DestroyImmediate(reopenedGo); }
            }
            finally { if (go != null) UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void BackwardClockBootSavePreservesDurableBoundaryAcrossReopen()
        {
            const long durableBoundary = 2000;
            Fixture fixture = OneFloor(durableBoundary, 12.5d);
            var time = new FakeTimeSource { Now = durableBoundary };
            SaveService firstService = Service(fixture, time);
            Assert.That(firstService.Save(fixture.Runtime, SaveReason.Boot), Is.True);

            time.Now = 1000;
            byte[] before = firstService.CanonicalSession.GetCurrentBytes();
            // H1-F4 D6: an invalid/backward observation fails closed, including
            // ordinary boot metadata; it is not permission to advance W or grant.
            Assert.That(firstService.Save(fixture.Runtime, SaveReason.Boot), Is.False);
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(12.5d));
            Assert.That(fixture.Runtime.lastSavedUtcUnix, Is.EqualTo(durableBoundary));
            CollectionAssert.AreEqual(before, fixture.FileSystem.ReadAllBytes(fixture.ActivePath));
            CollectionAssert.AreEqual(before, firstService.CanonicalSession.GetCurrentBytes());

            fixture.Reopen();
            time.Now = durableBoundary;
            SaveService restoredService = Service(fixture, time);
            restoredService.BeginCanonicalRecovery(fixture.Runtime);
            for (int i = 0; i < 3; i++) Assert.That(Step(fixture, restoredService).IsSuccess, Is.True);
            Assert.That(fixture.Runtime.timeProvenance.Episode, Is.Null);
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(12.5d));
            Assert.That(restoredService.Save(fixture.Runtime, SaveReason.Boot), Is.True);

            fixture.Reopen();
            time.Now = durableBoundary + 1;
            SaveService forwardService = Service(fixture, time);
            forwardService.BeginCanonicalRecovery(fixture.Runtime);
            Assert.That(Step(fixture, forwardService).IsSuccess, Is.True);
            Assert.That(fixture.Runtime.timeProvenance.Episode.OriginalEligible100ns, Is.EqualTo(UnixTimePoint.UnitsPerSecond));
            Assert.That(Step(fixture, forwardService).IsSuccess, Is.True);
            Assert.That(Step(fixture, forwardService).IsSuccess, Is.True);
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(12.5075d).Within(1e-12));
            fixture.Reopen();
            Assert.That(fixture.Runtime.timeProvenance.Episode, Is.Null);
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(12.5075d).Within(1e-12));
        }

        [TestCase(SaveReason.Boot)]
        [TestCase(SaveReason.Periodic)]
        [TestCase(SaveReason.ManualDev)]
        [TestCase(SaveReason.AppPause)]
        [TestCase(SaveReason.AppQuit)]
        public void EveryCanonicalSaveReasonPreservesFutureDurableBoundary(
            SaveReason reason)
        {
            const long durableBoundary = 2000;
            Fixture fixture = OneFloor(durableBoundary);
            var time = new FakeTimeSource { Now = durableBoundary };
            SaveService service = Service(fixture, time);
            Assert.That(service.Save(fixture.Runtime, SaveReason.Boot), Is.True);

            time.Now = 1000;
            byte[] before = service.CanonicalSession.GetCurrentBytes();
            Assert.That(service.Save(fixture.Runtime, reason), Is.False,
                "Every writer must reject a backward observation under H1-F4 D6");
            CollectionAssert.AreEqual(before, service.CanonicalSession.GetCurrentBytes());
            CollectionAssert.AreEqual(before, fixture.FileSystem.ReadAllBytes(fixture.ActivePath));
            fixture.Reopen();

            Assert.That(fixture.Runtime.lastSavedUtcUnix, Is.EqualTo(durableBoundary));
            Assert.That(Encoding.UTF8.GetString(fixture.Session.GetCurrentBytes()),
                Does.Contain("\"schemaVersion\":" + CanonicalSaveSchemaVersions.CurrentWritableTarget));
        }

        [Test]
        public void PauseResumeAppliesOnceAndRepeatedResumeDoesNotReplay()
        {
            Fixture fixture = OneFloor();
            fixture.Runtime.structureRuntime.ManaReserve = 0d;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;
            var time = new FakeTimeSource { Now = TimeUtil.UtcNowUnixSeconds() };
            SaveService service = Service(fixture, time);
            GameObject go = new GameObject("Offline pause-resume lifecycle");
            try
            {
                GameRoot root = Root(go, fixture, service, time);
                double heat = root.Save.structureRuntime.Heat;
                long ticks = root.Save.totalTicks;

                root.ApplyPauseState(true);
                long boundary = root.Save.lastSavedUtcUnix;
                const long elapsed = 90000;
                time.Now = boundary + elapsed;
                root.ApplyPauseState(false);
                double once = root.Save.structureRuntime.ManaReserve;

                Assert.That(once, Is.EqualTo(675d).Within(1e-9));
                Assert.That(root.LatestOfflinePassiveManaResultForTests.ObservedElapsedSeconds,
                    Is.EqualTo(elapsed));
                Assert.That(root.Save.structureRuntime.Heat, Is.EqualTo(heat));
                Assert.That(root.Save.totalTicks, Is.EqualTo(ticks));
                Assert.That(root.TimeService.IsPaused, Is.False);
                Assert.That(root.BannerMessage, Does.Contain("clock change"));
                Assert.That(root.BannerMessage, Does.Contain("without a duration limit"));
                Assert.That(root.BannerMessage, Does.Not.Contain("may be limited"));
                Assert.That(root.BannerMessage, Does.Not.Contain(TimeService.ClockAnomalyMessageKey));
                Assert.That(root.OfflinePassiveManaLine, Does.Contain("90,000"));
                root.ApplyPauseState(false);
                Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(once));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void ResumePersistenceFailureKeepsSimulationPausedAndRetryDoesNotDuplicate()
        {
            Fixture fixture = OneFloor();
            fixture.Runtime.structureRuntime.ManaReserve = 0d;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;
            var time = new FakeTimeSource { Now = TimeUtil.UtcNowUnixSeconds() };
            SaveService service = Service(fixture, time);
            GameObject go = new GameObject("Offline resume failure");
            try
            {
                GameRoot root = Root(go, fixture, service, time);
                root.ApplyPauseState(true);
                long boundary = root.Save.lastSavedUtcUnix;
                byte[] before = service.CanonicalSession.GetCurrentBytes();
                time.Now = boundary + 3600;
                fixture.FileSystem.EnableFailure(
                    Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write, 2);

                root.ApplyPauseState(false);

                Assert.That(root.TimeService.IsPaused, Is.True);
                Assert.That(root.Save.structureRuntime.ManaReserve, Is.Zero);
                Assert.That(root.Save.lastSavedUtcUnix, Is.EqualTo(boundary));
                CollectionAssert.AreEqual(before, fixture.FileSystem.ReadAllBytes(fixture.ActivePath));
                Assert.That(root.LatestOfflinePassiveManaResultForTests.Reason,
                    Is.EqualTo(OfflinePassiveManaReason.PersistenceFailure));

                fixture.FileSystem.DisableFailure();
                root.ApplyPauseState(false);
                Assert.That(root.TimeService.IsPaused, Is.False);
                Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(27d).Within(1e-9));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void LocalizedPresentationUsesLocaleAndNeverLeaksKeysOrReasonCodes()
        {
            Fixture fixture = OneFloor();
            fixture.Runtime.lastSavedUtcUnix = 1000;
            fixture.Runtime.structureRuntime.ManaReserve = 12.5d;
            OfflinePassiveManaResult result = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, 1001).WithPersistence(
                    OfflinePassiveManaReason.Applied, true);
            var strings = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [OfflinePassiveManaPresenter.AppliedFormatKey] =
                    "Sekunden {0:N0}; Rate {1}; Gewinn {2}; Mana {3}/{4}",
                [OfflinePassiveManaPresenter.CapacityLimitedKey] = "Begrenzt",
                [OfflinePassiveManaPresenter.AppliedReasonKey] = "Angewendet"
            };

            string text = OfflinePassiveManaPresenter.Build(result,
                CultureInfo.GetCultureInfo("de-DE"),
                key => strings.TryGetValue(key, out string value) ? value : key);

            Assert.That(text, Does.Contain("12,5"));
            Assert.That(text, Does.Not.Contain("12,5075"));
            Assert.That(result.WalletAfter, Is.EqualTo(12.5075d).Within(1e-12));
            Assert.That(text, Does.Not.Contain("ui.offline_mana"));
            Assert.That(text, Does.Not.Contain(nameof(OfflinePassiveManaReason.Applied)));
        }

        [Test]
        public void PresentationDistinguishesCapacityLimitedAndNoAwardResults()
        {
            Fixture fixture = OneFloor();
            fixture.Runtime.lastSavedUtcUnix = 1000;
            fixture.Runtime.structureRuntime.ManaReserve = 999.75d;
            var strings = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [OfflinePassiveManaPresenter.AppliedFormatKey] =
                    "Away {0:N0}; rate {1}; earned {2}; mana {3}/{4}",
                [OfflinePassiveManaPresenter.CapacityLimitedKey] = "Capacity limited",
                [OfflinePassiveManaPresenter.NoAwardFormatKey] = "No award: {0}",
                [OfflinePassiveManaPresenter.ZeroElapsedReasonKey] = "zero elapsed"
            };
            Func<string, string> localize = key =>
                strings.TryGetValue(key, out string value) ? value : key;

            OfflinePassiveManaResult limited = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, 4600).WithPersistence(
                    OfflinePassiveManaReason.Applied, true);
            OfflinePassiveManaResult zero = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, 1000);

            Assert.That(OfflinePassiveManaPresenter.Build(limited,
                CultureInfo.InvariantCulture, localize), Does.Contain("Capacity limited"));
            Assert.That(OfflinePassiveManaPresenter.Build(zero,
                CultureInfo.InvariantCulture, localize), Is.EqualTo("No award: zero elapsed"));
        }

        [Test]
        public void ProductionLocalizationContainsAllOfflineSummaryKeys()
        {
            StringTable table = JsonUtility.FromJson<StringTable>(File.ReadAllText(
                "Assets/_Project/Data/Bootstrap/string_table_en.json"));
            HashSet<string> keys = table.entries.Select(entry => entry.key)
                .ToHashSet(StringComparer.Ordinal);
            string[] required =
            {
                OfflinePassiveManaPresenter.AppliedFormatKey,
                OfflinePassiveManaPresenter.CapacityLimitedKey,
                OfflinePassiveManaPresenter.NoAwardFormatKey,
                OfflinePassiveManaPresenter.AppliedReasonKey,
                OfflinePassiveManaPresenter.ZeroElapsedReasonKey,
                OfflinePassiveManaPresenter.AlreadyAtCapacityReasonKey,
                OfflinePassiveManaPresenter.InvalidTimeReasonKey,
                OfflinePassiveManaPresenter.UnavailableReasonKey,
                OfflinePassiveManaPresenter.PersistenceFailureReasonKey,
                OfflinePassiveManaPresenter.NoGenerationReasonKey,
                TimeService.ClockAnomalyMessageKey
            };
            CollectionAssert.IsSubsetOf(required, keys);
        }

        private static Fixture OneFloor(long durableSeconds = 1, double? wallet = null)
        {
            Fixture fixture = Fixture.Create("\"lastSavedUtcUnix\":" + durableSeconds.ToString(CultureInfo.InvariantCulture),
                workloadProfile: PhaseFourTestSupport.ProductionProfile());
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.BasicRoomOptionId)));
            if (wallet.HasValue) {
                fixture.Runtime.structureRuntime.ManaReserve = wallet.Value;
                fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;
                fixture.Accept(fixture.Authority.SaveRecognizedState(fixture.ActivePath, fixture.FileSystem,
                    fixture.Session, fixture.Runtime));
            }
            return fixture;
        }

        private static PassiveOnlineManaConfigurationSnapshot Configuration(Fixture fixture) =>
            PhaseFourTestSupport.PassiveMana(fixture.Profile.Canonical);

        private static CanonicalPassiveManaService Online(Fixture fixture) =>
            new CanonicalPassiveManaService(Configuration(fixture), fixture.Economy,
                new FormulaEngine(), fixture.Profile.Canonical.Spatial, 10);

        private static CanonicalOfflinePassiveManaService Offline(Fixture fixture,
            CanonicalPassiveManaService online = null) =>
            new CanonicalOfflinePassiveManaService(Configuration(fixture), online ?? Online(fixture));

        private static double HeatFor(RunSimulationConfig config, string tierId) =>
            tierId == CurrentHeatTierResolver.PeaceTierId ? config.HeatPeaceMinimum :
            tierId == CurrentHeatTierResolver.NoticeTierId ? config.HeatNoticeMinimum :
            config.HeatConcernMinimum;

        private static SaveService Service(Fixture fixture, FakeTimeSource timeSource)
        {
            var service = DungeonBuilder.Tests.EditMode.ResearchDurableAcknowledgementTests.OpenService(
                fixture, out var loaded, timeSource, () => timeSource.Monotonic, out _);
            fixture.Runtime = loaded;
            service.CanonicalRuntimePublished += value => fixture.Runtime = value;
            service.ConfigureStructuralRemovalPolicy(fixture.RemovalPolicy);
            return service;
        }

        private static DetachedCanonicalWriteResult Step(Fixture fixture, SaveService service)
        {
            var inputs = fixture.Context.RecoveryInputs;
            var online = new CanonicalPassiveManaService(inputs.Passive, inputs.Economy, new FormulaEngine(),
                fixture.Profile.Canonical.Spatial, inputs.Bootstrap.tickSeconds);
            return service.AdvanceCanonicalRecovery(fixture.Runtime,
                new CanonicalOfflinePassiveManaService(inputs.Passive, online),
                (_, __) => Assert.Fail("Absence cannot create an adventurer tick"));
        }

        private static GameRoot Root(GameObject go, Fixture fixture, SaveService service,
            FakeTimeSource time)
        {
            GameRoot root = go.AddComponent<GameRoot>();
            typeof(GameRoot).GetProperty("Logger").SetValue(root, new SimpleLogger(false, (_, __) => { }));
            typeof(GameRoot).GetProperty("Save").SetValue(root, fixture.Runtime);
            var content = ProductionLocalizedContent();
            var inputs = fixture.Context.RecoveryInputs;
            content.BindRecoveryBootstrap(inputs);
            typeof(ContentService).GetProperty("ProductionSpatialContent").SetValue(content, fixture.Production);
            SetField(root, "<Content>k__BackingField", content);
            var owner = (TimeService)typeof(SaveService).GetField("recoveryTime",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service);
            Assert.That(owner, Is.Not.Null);
            Assert.That(root.TimeService, Is.Null, "Install the already-bound owner once; never replace it");
            typeof(GameRoot).GetProperty("TimeService").SetValue(root, owner);
            SetField(root, "recoveryRuntimeInputs", inputs);
            root.AttachSaveServiceForTests(service);
            Assert.That(root.ConfigureCanonicalPassiveManaForTests(inputs.Passive,
                fixture.Economy, fixture.Profile.Canonical.Spatial, inputs.Bootstrap.tickSeconds,
                inputs.Bootstrap.timeRules.activeSaveIntervalSeconds), Is.True);
            root.SetOfflineHeatConfigurationForTests(fixture.Configuration);
            root.SetOfflineTimeSourceForTests(time);
            service.BeforeForegroundAction = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), root,
                typeof(GameRoot).GetMethod("FlushForeground", BindingFlags.Instance | BindingFlags.NonPublic));
            service.ForegroundCurrent = () => root.Save;
            return root;
        }

        private static void DrainRecovery(GameRoot root)
        {
            var pulse = typeof(GameRoot).GetMethod("UpdateForeground", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < 8 && root.TimeService.RecoveryClockActive; i++) pulse.Invoke(root, null);
            Assert.That(root.TimeService.RecoveryClockActive, Is.False, "All required recovery phases must close durably");
            Assert.That(root.Save.timeProvenance.Episode, Is.Null);
        }

        private static ContentService ProductionLocalizedContent()
        {
            StringTable table = JsonUtility.FromJson<StringTable>(File.ReadAllText(
                "Assets/_Project/Data/Bootstrap/string_table_en.json"));
            var content = new ContentService();
            SetField(content, "<Strings>k__BackingField", table);
            FieldInfo field = typeof(ContentService).GetField("_stringMap",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var map = (Dictionary<string, string>)field.GetValue(content);
            foreach (StringEntry entry in table.entries)
                map[entry.key] = entry.text;
            return content;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
#endif
