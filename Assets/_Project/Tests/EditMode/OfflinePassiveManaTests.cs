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
        private sealed class FakeTimeSource : ITimeSource
        {
            public long Now;
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
            Fixture fixture = OneFloor();
            fixture.Runtime.lastSavedUtcUnix = 1000;
            fixture.Runtime.structureRuntime.ManaReserve = 10d;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;
            OfflinePassiveManaResult calculated = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, 4600);
            SaveData liveBefore = fixture.Runtime;

            DetachedCanonicalWriteResult write = fixture.Authority.SaveOfflinePassiveMana(
                fixture.ActivePath, fixture.FileSystem, fixture.Session, fixture.Runtime,
                calculated.WalletAfter, calculated.SourceSavedUtcUnix,
                calculated.ObservedCurrentUtcUnix);

            Assert.That(write.IsSuccess, Is.True, write.Reason);
            Assert.That(liveBefore.structureRuntime.ManaReserve, Is.EqualTo(10d));
            Assert.That(liveBefore.lastSavedUtcUnix, Is.EqualTo(1000));
            Assert.That(write.RuntimeProjection.structureRuntime.ManaReserve,
                Is.EqualTo(calculated.WalletAfter));
            Assert.That(write.RuntimeProjection.lastSavedUtcUnix, Is.EqualTo(4600));
            string persisted = Encoding.UTF8.GetString(write.GetPersistedBytes());
            Assert.That(persisted, Does.Contain("\"schemaVersion\":9"));
            Assert.That(persisted, Does.Not.Contain("OfflinePassiveMana"));
            Assert.That(persisted, Does.Not.Contain("BaseOfflineEfficiency"));
        }

        [Test]
        public void PersistenceFailureLeavesWalletTimestampDiskAndSessionUnchangedThenRetrySucceeds()
        {
            Fixture fixture = OneFloor();
            fixture.Runtime.lastSavedUtcUnix = 1000;
            fixture.Runtime.structureRuntime.ManaReserve = 10d;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;
            OfflinePassiveManaResult calculated = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, 4600);
            byte[] before = fixture.FileSystem.ReadAllBytes(fixture.ActivePath);
            fixture.FileSystem.EnableFailure(
                Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write, 2);

            DetachedCanonicalWriteResult failed = fixture.Authority.SaveOfflinePassiveMana(
                fixture.ActivePath, fixture.FileSystem, fixture.Session, fixture.Runtime,
                calculated.WalletAfter, calculated.SourceSavedUtcUnix,
                calculated.ObservedCurrentUtcUnix);

            Assert.That(failed.IsSuccess, Is.False);
            Assert.That(failed.RuntimeProjection, Is.Null);
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(10d));
            Assert.That(fixture.Runtime.lastSavedUtcUnix, Is.EqualTo(1000));
            CollectionAssert.AreEqual(before, fixture.FileSystem.ReadAllBytes(fixture.ActivePath));
            CollectionAssert.AreEqual(before, fixture.Session.GetCurrentBytes());

            fixture.FileSystem.DisableFailure();
            DetachedCanonicalWriteResult retry = fixture.Authority.SaveOfflinePassiveMana(
                fixture.ActivePath, fixture.FileSystem, fixture.Session, fixture.Runtime,
                calculated.WalletAfter, calculated.SourceSavedUtcUnix,
                calculated.ObservedCurrentUtcUnix);
            Assert.That(retry.IsSuccess, Is.True, retry.Reason);
            Assert.That(retry.RuntimeProjection.structureRuntime.ManaReserve,
                Is.EqualTo(calculated.WalletAfter));
        }

        [Test]
        public void StaleDiskSessionReturnsStableReasonWithoutWrite()
        {
            Fixture fixture = OneFloor();
            fixture.Runtime.lastSavedUtcUnix = 1000;
            fixture.Runtime.structureRuntime.ManaReserve = 0d;
            OfflinePassiveManaResult calculated = Offline(fixture).Resolve(
                fixture.Runtime, fixture.Configuration, 4600);
            byte[] stale = Encoding.UTF8.GetBytes("{\"changed\":true}");
            fixture.FileSystem.Seed(fixture.ActivePath, stale);

            DetachedCanonicalWriteResult result = fixture.Authority.SaveOfflinePassiveMana(
                fixture.ActivePath, fixture.FileSystem, fixture.Session, fixture.Runtime,
                calculated.WalletAfter, calculated.SourceSavedUtcUnix,
                calculated.ObservedCurrentUtcUnix);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason,
                Is.EqualTo(DetachedCanonicalWriteAuthority.OfflineStaleSessionReason));
            CollectionAssert.AreEqual(stale, fixture.FileSystem.ReadAllBytes(fixture.ActivePath));
        }

        [Test]
        public void ColdStartCommitBootSaveAndFreshReopenCannotReplayInterval()
        {
            Fixture fixture = OneFloor();
            fixture.Runtime.lastSavedUtcUnix = 1000;
            fixture.Runtime.structureRuntime.ManaReserve = 12.5d;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;
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

                Assert.That(root.TryApplyOfflinePassiveManaForTests(), Is.True);
                Assert.That(root.Save.structureRuntime.ManaReserve,
                    Is.EqualTo(12.5075d).Within(1e-12));
                Assert.That(root.Save.lastSavedUtcUnix, Is.EqualTo(1001));
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
                    Assert.That(reopenedRoot.TryApplyOfflinePassiveManaForTests(), Is.True);
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
                        Does.Contain("\"schemaVersion\":9"));
                    Assert.That(reopened, Is.SameAs(reopenedRoot.Save));
                }
                finally { UnityEngine.Object.DestroyImmediate(reopenedGo); }
            }
            finally { if (go != null) UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void BackwardClockBootSavePreservesDurableBoundaryAcrossReopen()
        {
            const long durableBoundary = 2000;
            Fixture fixture = OneFloor();
            fixture.Runtime.lastSavedUtcUnix = durableBoundary;
            fixture.Runtime.structureRuntime.ManaReserve = 12.5d;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;
            var time = new FakeTimeSource { Now = durableBoundary };
            SaveService firstService = Service(fixture, time);
            Assert.That(firstService.Save(fixture.Runtime, SaveReason.Boot), Is.True);

            time.Now = 1000;
            GameObject firstGo = new GameObject("Offline backward-clock boot");
            GameRoot firstRoot = Root(firstGo, fixture, firstService, time);
            Assert.That(firstRoot.TryApplyOfflinePassiveManaForTests(), Is.True);
            Assert.That(firstRoot.LatestOfflinePassiveManaResultForTests.Reason,
                Is.EqualTo(OfflinePassiveManaReason.BackwardClockMovement));
            Assert.That(firstRoot.Save.structureRuntime.ManaReserve, Is.EqualTo(12.5d));
            Assert.That(firstService.Save(firstRoot.Save, SaveReason.Boot), Is.True);
            Assert.That(firstRoot.Save.lastSavedUtcUnix, Is.EqualTo(durableBoundary));
            UnityEngine.Object.DestroyImmediate(firstGo);

            fixture.Reopen();
            time.Now = durableBoundary;
            SaveService restoredService = Service(fixture, time);
            GameObject restoredGo = new GameObject("Offline restored-clock reopen");
            GameRoot restoredRoot = Root(restoredGo, fixture, restoredService, time);
            Assert.That(restoredRoot.TryApplyOfflinePassiveManaForTests(), Is.True);
            Assert.That(restoredRoot.LatestOfflinePassiveManaResultForTests.Reason,
                Is.EqualTo(OfflinePassiveManaReason.ZeroElapsedInterval));
            Assert.That(restoredRoot.Save.structureRuntime.ManaReserve, Is.EqualTo(12.5d));
            Assert.That(restoredService.Save(restoredRoot.Save, SaveReason.Boot), Is.True);
            UnityEngine.Object.DestroyImmediate(restoredGo);

            fixture.Reopen();
            time.Now = durableBoundary + 1;
            SaveService forwardService = Service(fixture, time);
            GameObject forwardGo = new GameObject("Offline genuine-forward reopen");
            try
            {
                GameRoot forwardRoot = Root(forwardGo, fixture, forwardService, time);
                Assert.That(forwardRoot.TryApplyOfflinePassiveManaForTests(), Is.True);
                Assert.That(forwardRoot.LatestOfflinePassiveManaResultForTests.Reason,
                    Is.EqualTo(OfflinePassiveManaReason.Applied));
                Assert.That(forwardRoot.LatestOfflinePassiveManaResultForTests.ObservedElapsedSeconds,
                    Is.EqualTo(1));
                Assert.That(forwardRoot.Save.structureRuntime.ManaReserve,
                    Is.EqualTo(12.5075d).Within(1e-12));
            }
            finally { UnityEngine.Object.DestroyImmediate(forwardGo); }
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
            Fixture fixture = OneFloor();
            fixture.Runtime.lastSavedUtcUnix = durableBoundary;
            var time = new FakeTimeSource { Now = durableBoundary };
            SaveService service = Service(fixture, time);
            Assert.That(service.Save(fixture.Runtime, SaveReason.Boot), Is.True);

            time.Now = 1000;
            Assert.That(service.Save(fixture.Runtime, reason), Is.True);
            fixture.Reopen();

            Assert.That(fixture.Runtime.lastSavedUtcUnix, Is.EqualTo(durableBoundary));
            Assert.That(Encoding.UTF8.GetString(fixture.Session.GetCurrentBytes()),
                Does.Contain("\"schemaVersion\":9"));
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
                GameRoot root = Root(go, fixture, service, time, attachClock: true);
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
                GameRoot root = Root(go, fixture, service, time, attachClock: true);
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
                    "Sekunden {0:N0}; Rate {1:0.###}; Gewinn {2:0.####}; Mana {3:0.####}/{4:0.###}",
                [OfflinePassiveManaPresenter.CapacityLimitedKey] = "Begrenzt",
                [OfflinePassiveManaPresenter.AppliedReasonKey] = "Angewendet"
            };

            string text = OfflinePassiveManaPresenter.Build(result,
                CultureInfo.GetCultureInfo("de-DE"),
                key => strings.TryGetValue(key, out string value) ? value : key);

            Assert.That(text, Does.Contain("12,5075"));
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
                    "Away {0:N0}; rate {1:0.###}; earned {2:0.###}; mana {3:0.###}/{4:0.###}",
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

        private static Fixture OneFloor()
        {
            Fixture fixture = Fixture.Create(null);
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.BasicRoomOptionId)));
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

        private static SaveService Service(Fixture fixture, ITimeSource timeSource = null)
        {
            var service = new SaveService(new SimpleLogger(false, (level, message) => { }), null,
                Path.GetDirectoryName(fixture.ActivePath));
            service.ConfigureCanonical(fixture.Profile, fixture.Production,
                fixture.Compatibility, fixture.Configuration,
                LegacyGameplayConfigurationContract.SerializeCanonical(fixture.Configuration));
            service.ConfigureStructuralEconomy(fixture.Economy);
            service.ConfigureStructuralRemovalPolicy(fixture.RemovalPolicy);
            service.ConfigureContentAcquisitionEconomy(fixture.Acquisition);
            typeof(SaveService).GetProperty("SavePath").SetValue(service, fixture.ActivePath);
            SetField(service, "_canonicalSession", fixture.Session);
            SetField(service, "_canonicalFileSystem", fixture.FileSystem);
            SetField(service, "_validationContext", fixture.Context);
            if (timeSource != null) service.SetTimeSourceForTests(timeSource);
            return service;
        }

        private static GameRoot Root(GameObject go, Fixture fixture, SaveService service,
            FakeTimeSource time, bool attachClock = false)
        {
            GameRoot root = go.AddComponent<GameRoot>();
            typeof(GameRoot).GetProperty("Save").SetValue(root, fixture.Runtime);
            SetField(root, "<Content>k__BackingField", ProductionLocalizedContent());
            root.AttachSaveServiceForTests(service);
            Assert.That(root.ConfigureCanonicalPassiveManaForTests(Configuration(fixture),
                fixture.Economy, fixture.Profile.Canonical.Spatial, 10, 30), Is.True);
            root.SetOfflineHeatConfigurationForTests(fixture.Configuration);
            root.SetOfflineTimeSourceForTests(time);
            if (attachClock)
            {
                var clock = new TimeService(new SimpleLogger(false), 10, 300, time);
                clock.AttachSave(root.Save);
                typeof(GameRoot).GetProperty("TimeService").SetValue(root, clock);
            }
            return root;
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
