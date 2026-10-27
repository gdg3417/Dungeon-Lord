#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;
using Faults = DungeonBuilder.M0.Tests.EditMode.Gd66DetachedSpatialMigrationTransactionTests;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class H1F4RecoveryIntegrationTests : H1F4RecoveryClockTests
    {
        sealed class Clock : IPreciseTimeSource
        {
            internal long Seconds = 100;
            public UnixTimePoint UtcNow() => new UnixTimePoint(Seconds, 0);
            public long UtcNowUnixSeconds() => Seconds;
        }
        sealed class Harness
        {
            internal readonly Fixture F;
            internal readonly RecoveryRuntimeInputs Inputs;
            internal readonly Clock Wall = new Clock();
            internal readonly TimeService Time;
            internal readonly SaveService Service;
            internal readonly CanonicalOfflinePassiveManaService Mana;
            internal SaveData Current;
            internal Harness()
            {
                var profile = SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
                F = Fixture.Create("\"researchPending\":null,\"researchProgress\":null", workloadProfile: profile);
                Inputs = Bundle(F);
                Time = new TimeService(new SimpleLogger(false), Inputs.Bootstrap.tickSeconds, Inputs.Bootstrap.timeRules.detectClockSkewSeconds,
                    Wall, () => Wall.Seconds - 100d);
                Time.BeginRecoveryForeground(Inputs.TwoFloor.UnitsPerSecond);
                Service = CreateService(F, Inputs, Time);
                Current = Service.LoadOrCreate("h1f4-owned-test", out var banner);
                Assert.That(Current, Is.Not.Null, banner);
                Time.BindLoadedForegroundHorizon(Current.livingDungeon.Scheduler?.AcceptedThrough ?? -1);
                Time.AttachSave(Current);
                Service.CanonicalRuntimePublished += value => { Current = value; Time.AttachSave(value); };
                var online = new CanonicalPassiveManaService(Inputs.Passive, Inputs.Economy, new FormulaEngine(), F.Profile.Canonical.Spatial, Inputs.Bootstrap.tickSeconds);
                Mana = new CanonicalOfflinePassiveManaService(Inputs.Passive, online);
            }
            internal void Start() => Service.BeginCanonicalRecovery(Current);
            internal DetachedCanonicalWriteResult Step(long seconds)
            {
                Wall.Seconds = seconds;
                return Service.AdvanceCanonicalRecovery(Current, Mana, (_, __) => Assert.Fail("Dormant fixture has no accepted ticks"));
            }
            internal void Accept(long seconds)
            { var result = Step(seconds); Assert.That(result.IsSuccess, Is.True, result.Reason); }
        }
        static SaveService CreateService(Fixture f, RecoveryRuntimeInputs inputs, TimeService time)
        {
            var service = new SaveService(new SimpleLogger(false, (_, __) => { }),
                new SaveConfig { fileName = Path.GetFileName(f.ActivePath), useAtomicWrites = true }, Path.GetDirectoryName(f.ActivePath));
            service.ConfigureCanonical(f.Profile, f.Production, inputs.Compatibility, inputs.Configuration,
                LegacyGameplayConfigurationContract.SerializeCanonical(inputs.Configuration), f.PositionProfiles, inputs.Occupancy);
            service.ConfigureStructuralEconomy(inputs.Economy); service.ConfigureContentAcquisitionEconomy(f.Acquisition);
            service.SetPreflightEvaluatorForTests(path => new SpatialMigrationActivationPreflight(true,
                SpatialMigrationCapabilityReason.Ready, SpatialMigrationPlatform.WindowsEditor, f.FileSystem, path));
            service.ConfigureRecovery(inputs, time); return service;
        }
        static void Expected(Harness h, double wallet) => Assert.That(h.Current.structureRuntime.ManaReserve, Is.EqualTo(wallet));

        [Test] public void ActualAtomicEntryOriginalAndResidualCloseOneCanonicalOwner()
        {
            var h = new Harness(); h.Start(); var r = h.Time.RetainedRecoveryEntry.WallEndpoint;
            h.Accept(100); Assert.That(h.Current.timeProvenance.Episode.Phase, Is.EqualTo(RecoveryEpisode.OriginalPending));
            double original = h.Mana.ResolveRecoveryDuration(h.Current, h.Inputs.Configuration, 990000000, false).WalletAfter;
            h.Accept(102); Expected(h, original);
            Assert.That(h.Current.timeProvenance.Episode.Phase, Is.EqualTo(RecoveryEpisode.ResidualPending));
            Assert.That(h.Current.timeProvenance.Episode.Entry, Is.EqualTo(r));
            double residual = h.Mana.ResolveRecoveryDuration(h.Current, h.Inputs.Configuration, 50000000, false).WalletAfter;
            h.Accept(105); Expected(h, residual);
            Assert.That(h.Current.timeProvenance.Episode, Is.Null); Assert.That(h.Time.RecoveryClockActive, Is.False);
            Assert.That(h.Current.timeProvenance.AccountedBoundary.Seconds, Is.EqualTo(105));
            Assert.That(h.Current.timeProvenance.RecoveryContextSha256, Is.EqualTo(h.Inputs.Identity.Digest));
            Assert.That(h.Time.ForegroundThrough, Is.Zero);
        }
        [TestCase(Faults.OperationType.Write, 2)]
        [TestCase(Faults.OperationType.Replace, 1)]
        [TestCase(Faults.OperationType.Flush, 1)]
        public void OriginalRollbackRetainsEntryAndPaysExactlyOnceAfterRetry(Faults.OperationType operation, int occurrence)
        {
            var h = new Harness(); h.Start(); h.Accept(100);
            var before = h.F.FileSystem.ReadAllBytes(h.Service.SavePath);
            double original = h.Mana.ResolveRecoveryDuration(h.Current, h.Inputs.Configuration, 990000000, false).WalletAfter;
            h.F.FileSystem.EnableFailure(operation, occurrence);
            var failed = h.Step(102); Assert.That(failed.IsSuccess, Is.False);
            Assert.That(failed.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            CollectionAssert.AreEqual(before, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
            h.F.FileSystem.DisableFailure(); h.Accept(110); Expected(h, original);
            Assert.That(h.Current.timeProvenance.Episode.Entry.Seconds, Is.EqualTo(100));
            double residual = h.Mana.ResolveRecoveryDuration(h.Current, h.Inputs.Configuration, 40000000, false).WalletAfter;
            h.Accept(112); Expected(h, residual); Assert.That(h.Current.timeProvenance.Episode, Is.Null);
        }
        [TestCase(Faults.OperationType.Write, 2)]
        [TestCase(Faults.OperationType.Replace, 1)]
        [TestCase(Faults.OperationType.Flush, 1)]
        public void ResidualRollbackNeverCommitsFailedGrantOrForegroundStall(Faults.OperationType operation, int occurrence)
        {
            var h = new Harness(); h.Start(); h.Accept(100); h.Accept(102);
            var before = h.F.FileSystem.ReadAllBytes(h.Service.SavePath);
            double residual = h.Mana.ResolveRecoveryDuration(h.Current, h.Inputs.Configuration, 40000000, false).WalletAfter;
            h.F.FileSystem.EnableFailure(operation, occurrence); var failed = h.Step(104);
            Assert.That(failed.IsSuccess, Is.False); Assert.That(failed.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            CollectionAssert.AreEqual(before, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
            h.F.FileSystem.DisableFailure(); h.Accept(110); Expected(h, residual);
            Assert.That(h.Current.timeProvenance.Episode, Is.Null); Assert.That(h.Time.ForegroundThrough, Is.Zero);
        }
        [TestCase(false)] [TestCase(true)]
        public void BothPendingPhasesRejectChangedHealthContextBeforeAnyEffect(bool originalComplete)
        {
            var h = new Harness(); h.Start(); h.Accept(100); if (originalComplete) h.Accept(102);
            var before = h.F.FileSystem.ReadAllBytes(h.Service.SavePath); var bytes = Inputs();
            bytes[0] = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes[0]).Replace("\"MaxHealth\": 8,", "\"MaxHealth\": 10,"));
            var changed = Bundle(h.F, bytes); var clock = new Clock();
            var time = new TimeService(new SimpleLogger(false), 10, 300, clock, () => 0); time.BeginRecoveryForeground(changed.TwoFloor.UnitsPerSecond);
            var other = CreateService(h.F, changed, time);
            Assert.That(other.LoadOrCreate("changed-context", out _), Is.Null);
            Assert.That(time.RetainedRecoveryEntry, Is.Null); Assert.That(time.RecoveryClockActive, Is.False);
            CollectionAssert.AreEqual(before, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
        }
        [Test] public void OrdinaryMetadataCannotBypassEpisodeAndRetainsClosedWAndContext()
        {
            var h = new Harness(); h.Start(); h.Accept(100);
            Assert.That(h.Service.Save(h.Current, SaveReason.Boot), Is.False);
            h.Accept(102); h.Accept(105); h.Wall.Seconds = 120;
            Assert.That(h.Service.Save(h.Current, SaveReason.StateChange), Is.True);
            Assert.That(h.Current.timeProvenance.AccountedBoundary.Seconds, Is.EqualTo(105));
            Assert.That(h.Current.timeProvenance.RecoveryContextSha256, Is.EqualTo(h.Inputs.Identity.Digest));
        }
        [Test] public void FrozenSixteenCannotBeOpenedAsWritableSeventeen()
        {
            var h = new Harness(); var old = (byte[])h.F.FrozenSixteen.Clone();
            Assert.That(DetachedCanonicalSaveSession.Open(old, h.F.Context, h.F.Profile).IsSuccess, Is.False);
            var frozen = DetachedCompleteSaveContract.ParseValidateFrozenSchemaSixteenAndRoundTrip(old, h.F.Context);
            Assert.That(frozen.IsValid, Is.True); Assert.That(frozen.CurrentTargetValidated, Is.False);
            CollectionAssert.AreEqual(old, frozen.GetBytes());
        }
        [Test] public void CorruptSourceCannotCaptureEntryOrBeReplacedByNativeCreation()
        {
            var h = new Harness(); var bad = Encoding.UTF8.GetBytes("{bad"); h.F.FileSystem.Seed(h.Service.SavePath, bad);
            var clock = new Clock(); var time = new TimeService(new SimpleLogger(false), 10, 300, clock, () => 0);
            time.BeginRecoveryForeground(h.Inputs.TwoFloor.UnitsPerSecond); var service = CreateService(h.F, h.Inputs, time);
            Assert.That(service.LoadOrCreate("corrupt-source", out _), Is.Null);
            Assert.That(time.RetainedRecoveryEntry, Is.Null); Assert.That(time.RecoveryClockActive, Is.False);
            CollectionAssert.AreEqual(bad, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
        }
    }
}
#endif
