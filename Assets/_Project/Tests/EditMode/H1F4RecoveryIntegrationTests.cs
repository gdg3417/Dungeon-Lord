#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
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
        static SaveService CreateService(Fixture f, RecoveryRuntimeInputs inputs, TimeService time,
            ISpatialMigrationFileSystem fileSystem = null, ITimeSource source = null)
        {
            var service = new SaveService(new SimpleLogger(false, (_, __) => { }),
                new SaveConfig { fileName = Path.GetFileName(f.ActivePath), useAtomicWrites = true }, Path.GetDirectoryName(f.ActivePath));
            service.ConfigureCanonical(f.Profile, f.Production, inputs.Compatibility, inputs.Configuration,
                LegacyGameplayConfigurationContract.SerializeCanonical(inputs.Configuration), f.PositionProfiles, inputs.Occupancy);
            service.ConfigureStructuralEconomy(inputs.Economy); service.ConfigureContentAcquisitionEconomy(f.Acquisition);
            service.SetPreflightEvaluatorForTests(path => new SpatialMigrationActivationPreflight(true,
                SpatialMigrationCapabilityReason.Ready, SpatialMigrationPlatform.WindowsEditor, fileSystem ?? f.FileSystem, path));
            if (source != null) service.SetTimeSourceForTests(source);
            service.ConfigureRecovery(inputs, time); return service;
        }
        static void Expected(Harness h, double wallet) => Assert.That(h.Current.structureRuntime.ManaReserve, Is.EqualTo(wallet));

        sealed class ObservedFileSystem : ISpatialMigrationFileSystem
        {
            readonly ISpatialMigrationFileSystem inner;
            internal Action AfterFlush;
            internal bool InterruptReplacement;
            int replacements;
            internal ObservedFileSystem(ISpatialMigrationFileSystem value) { inner = value; }
            public bool Exists(string path) => inner.Exists(path);
            public byte[] ReadAllBytes(string path) => inner.ReadAllBytes(path);
            public void WriteAllBytesDurable(string path, byte[] bytes) => inner.WriteAllBytesDurable(path, bytes);
            public void ReplaceSameDirectoryAtomic(string staging, string active) {
                if (InterruptReplacement && replacements++ > 0) throw new IOException("Test rollback interruption");
                inner.ReplaceSameDirectoryAtomic(staging, active);
                if (InterruptReplacement) throw new IOException("Test replacement acknowledgement interruption");
            }
            public void MoveSameDirectoryAtomic(string source, string destination) => inner.MoveSameDirectoryAtomic(source, destination);
            public void DeleteFile(string path) => inner.DeleteFile(path);
            public void FlushDirectory(string path) { inner.FlushDirectory(path); AfterFlush?.Invoke(); }
            public IReadOnlyList<string> EnumerateFiles(string directory, string pattern, int maximum) => inner.EnumerateFiles(directory, pattern, maximum);
            public bool IsPathContainedWithoutRedirection(string directory, string path) => inner.IsPathContainedWithoutRedirection(directory, path);
        }
        static (Clock Clock, TimeService Time, SaveService Service) NativeService(Harness h, ObservedFileSystem fs)
        {
            h.F.FileSystem.RemoveSeededEvidence(h.Service.SavePath);
            var clock = new Clock();
            var time = new TimeService(new SimpleLogger(false), 10, 300, clock, () => clock.Seconds - 100d);
            time.BeginRecoveryForeground(h.Inputs.TwoFloor.UnitsPerSecond);
            return (clock, time, CreateService(h.F, h.Inputs, time, fs, clock));
        }

        [Test] public void ActualNativeCreationUsesDurableSAndHandsOnlySuccessfulMonotonicSuffixToForeground()
        {
            var h = new Harness(); var fs = new ObservedFileSystem(h.F.FileSystem); var native = NativeService(h, fs);
            fs.AfterFlush = () => native.Clock.Seconds = 105;
            var created = native.Service.LoadOrCreate("native-test", out var reason);
            Assert.That(created, Is.Not.Null, reason); Assert.That(native.Service.LastLoadCreatedNative, Is.True);
            Assert.That(created.timeProvenance.Episode, Is.Null); Assert.That(created.timeProvenance.AccountedBoundary.Seconds, Is.EqualTo(100));
            Assert.That(native.Time.ForegroundThrough, Is.EqualTo(5000));
            Assert.That(native.Time.PendingRecoveryPublication, Is.Null); Assert.That(native.Time.RetainedRecoveryEntry, Is.Null);
        }
        [Test] public void ActualNativePauseBeforeSaveAttachmentProducesNoForegroundCredit()
        {
            var h = new Harness(); var fs = new ObservedFileSystem(h.F.FileSystem); var native = NativeService(h, fs);
            native.Time.OnPause(); fs.AfterFlush = () => native.Clock.Seconds = 105;
            Assert.That(native.Service.LoadOrCreate("native-paused", out _), Is.Not.Null);
            Assert.That(native.Time.ForegroundThrough, Is.Zero); Assert.That(native.Time.HasDeferredRecovery, Is.True);
            Assert.That(native.Time.RecoveryDeferredEligible100nsForClockTest, Is.EqualTo(50000000));
        }
        [Test] public void NativeFailedFlushRemainsUncertainUntilFreshProofAndDoesNotBecomeNewOfflineAbsence()
        {
            var h = new Harness(); var fs = new ObservedFileSystem(h.F.FileSystem); var native = NativeService(h, fs);
            h.F.FileSystem.EnableFailure(Faults.OperationType.Flush, 1);
            Assert.That(native.Service.LoadOrCreate("native-flush", out _), Is.Null);
            Assert.That(native.Time.ForegroundThrough, Is.Zero); Assert.That(native.Time.PendingRecoveryPublication, Is.Not.Null);
            Assert.That(h.F.FileSystem.Exists(native.Service.SavePath), Is.True);
            h.F.FileSystem.DisableFailure(); native.Clock.Seconds = 110;
            var resolved = native.Service.LoadOrCreate("native-flush", out var reason);
            Assert.That(resolved, Is.Not.Null, reason); Assert.That(native.Service.LastLoadCreatedNative, Is.True);
            Assert.That(resolved.timeProvenance.Episode, Is.Null); Assert.That(resolved.timeProvenance.AccountedBoundary.Seconds, Is.EqualTo(100));
            Assert.That(native.Time.ForegroundThrough, Is.Zero); Assert.That(native.Time.RecoveryClockActive, Is.False);
        }
        [Test] public void FailedNativeWriteCannotStartASecondCandidateInTheSameProcess()
        {
            var h = new Harness(); var native = NativeService(h, new ObservedFileSystem(h.F.FileSystem));
            h.F.FileSystem.EnableFailure(Faults.OperationType.Write, 1);
            Assert.That(native.Service.LoadOrCreate("native-before-write", out _), Is.Null);
            var retained = native.Time.PendingRecoveryPublication;
            h.F.FileSystem.DisableFailure(); native.Clock.Seconds = 110;
            Assert.That(native.Service.LoadOrCreate("native-before-write", out _), Is.Null);
            Assert.That(native.Time.PendingRecoveryPublication, Is.SameAs(retained));
            Assert.That(h.F.FileSystem.Exists(native.Service.SavePath), Is.False); Assert.That(native.Time.ForegroundThrough, Is.Zero);
        }
        [TestCase(false)] [TestCase(true)]
        public void WriterRejectsConfigurationOrLootOutsideItsValidatedConsumedBundle(bool wrongLoot)
        {
            var h = new Harness(); var bytes = h.Service.CanonicalSession.GetCurrentBytes();
            var bound = h.F.Context.RecoveryInputs;
            var config = bound.Configuration;
            if (!wrongLoot) {
                string changed = Encoding.UTF8.GetString(bound.Identity.GetInput(0)).Replace("\"MaxHealth\": 8,", "\"MaxHealth\": 10,");
                Assert.That(BootstrapConfigValidationService.TryParseRunSimulationConfig(changed, out config), Is.True);
                Assert.That(config.CombatC0, Is.Not.Null);
            }
            var loot = wrongLoot ? UnityEngine.JsonUtility.FromJson<LootConfig>(UnityEngine.JsonUtility.ToJson(bound.Loot)) : bound.Loot;
            var writer = new DetachedCanonicalWriteAuthority(h.F.Production, h.F.Compatibility, config, h.F.Context, h.F.Profile,
                economy: bound.Economy, runLoot: loot);
            var result = writer.SaveRecognizedState(h.Service.SavePath, h.F.FileSystem, h.Service.CanonicalSession, h.Current);
            Assert.That(result.IsSuccess, Is.False); CollectionAssert.AreEqual(bytes, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
        }
        [Test] public void UnsupportedRuntimeProfileCannotBeBoundUnderProductionContextIdentity()
        {
            var h = new Harness(); var unsupported = new LivingDungeonProfile("unsupported-test-executable", 1, 1, 100, 100, 100, 100, 100);
            var context = new DetachedCurrentTargetValidationContext(h.F.Compatibility, h.F.Production,
                LegacyGameplayConfigurationContract.SerializeCanonical(h.Inputs.Configuration), h.F.Profile.Canonical, h.Inputs.Occupancy,
                livingDungeonProfiles: new[] { unsupported }, monsterRecovery: h.Inputs.Monsters);
            Assert.Throws<ArgumentException>(() => context.BindRecoveryInputs(h.Inputs));
        }
        [Test] public void SharedFloorContentWriterUsesTheValidatedLootOwner()
        {
            var h = new Harness();
            h.F.Accept(h.F.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.BasicRoomOptionId)));
            PhaseSixA4Tests.AddContent(h.F, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            Assert.That(h.F.State.Floors.SelectMany(f => f.RoomContents.Assignments)
                .Any(a => a.OptionId == MvpDungeonPlacementIds.GoblinOptionId), Is.True);
            Assert.That(h.F.Runtime.timeProvenance.RecoveryContextSha256, Is.EqualTo(h.Inputs.Identity.Digest));
        }
        [Test] public void SharedResearchServiceRetainsBoundContextAndTheMetadataTimeBoundary()
        {
            var h = new Harness();
            var service = DungeonBuilder.Tests.EditMode.ResearchDurableAcknowledgementTests.OpenService(h.F, out var current);
            var prior = current.timeProvenance.AccountedBoundary;
            Assert.That(service.Save(current, SaveReason.StateChange), Is.True);
            var reopened = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(service.CanonicalSession.GetCurrentBytes(), h.F.Context);
            Assert.That(reopened.IsValid, Is.True, reopened.Reason);
            Assert.That(reopened.TimeProvenance.AccountedBoundary, Is.EqualTo(prior));
            Assert.That(reopened.TimeProvenance.RecoveryContextSha256, Is.EqualTo(h.Inputs.Identity.Digest));
        }
        [Test] public void LifecycleSaveDuringEntryPublicationCannotReenterTheCanonicalWriter()
        {
            var h = new Harness(); var fs = new ObservedFileSystem(h.F.FileSystem);
            var service = CreateService(h.F, h.Inputs, h.Time, fs);
            var current = service.LoadOrCreate("h1f4-owned-reentrant-test", out var banner);
            Assert.That(current, Is.Not.Null, banner);
            using var scope = new RootScope(h);
            scope.Rebind(service, h.Time, current);
            service.BeginCanonicalRecovery(current);
            int before = h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Write);
            int callbacks = 0;
            fs.AfterFlush = () => { if (callbacks++ == 0) scope.Focus(false, 101); };
            scope.Pulse(100);
            Assert.That(callbacks, Is.GreaterThan(0));
            Assert.That(scope.Root.Save.timeProvenance.Episode.Phase, Is.EqualTo(RecoveryEpisode.OriginalPending));
            Assert.That(scope.Root.Save.timeProvenance.AccountedBoundary, Is.EqualTo(new UnixTimePoint(100, 0)));
            Assert.That(scope.Root.Save.livingDungeon.IsDormant, Is.True);
            Assert.That(h.Time.RecoveryClockActive, Is.True);
            Assert.That(h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Write), Is.EqualTo(before + 1));
        }
        [TestCase(false)] [TestCase(true)]
        public void CommittedSettlementWithInterruptedAcknowledgementResolvesWithoutAnotherGrant(bool residual)
        {
            var h = new Harness(); var fs = new ObservedFileSystem(h.F.FileSystem);
            var time = h.Time; var service = CreateService(h.F, h.Inputs, time, fs);
            var current = service.LoadOrCreate("uncertain-settlement", out _);
            service.CanonicalRuntimePublished += value => current = value;
            service.BeginCanonicalRecovery(current);
            Assert.That(service.AdvanceCanonicalRecovery(current, h.Mana, (_, __) => Assert.Fail()).IsSuccess, Is.True);
            if (residual) { h.Wall.Seconds = 102; Assert.That(service.AdvanceCanonicalRecovery(current, h.Mana, (_, __) => Assert.Fail()).IsSuccess, Is.True); }
            h.Wall.Seconds = residual ? 104 : 102; fs.InterruptReplacement = true;
            var failed = service.AdvanceCanonicalRecovery(current, h.Mana, (_, __) => Assert.Fail());
            Assert.That(failed.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
            fs.InterruptReplacement = false; h.Wall.Seconds = 110;
            var proof = service.ResolveUncertainRecoveryPublication(); Assert.That(proof.IsSuccess, Is.True, proof.Reason);
            Assert.That(current.timeProvenance.Episode?.Phase, Is.EqualTo(residual ? null : RecoveryEpisode.ResidualPending));
            var once = h.F.FileSystem.ReadAllBytes(service.SavePath);
            Assert.That(service.ResolveUncertainRecoveryPublication().IsSuccess, Is.False);
            CollectionAssert.AreEqual(once, h.F.FileSystem.ReadAllBytes(service.SavePath));
            Assert.That(time.ForegroundThrough, Is.Zero);
        }
        [TestCase(false)] [TestCase(true)]
        public void ReopenedPendingPhaseRetainsOriginalEntryAndOnlyAddsUnknownSuffix(bool originalComplete)
        {
            var h = new Harness(); h.Start(); h.Accept(100); if (originalComplete) h.Accept(102);
            var r = h.Current.timeProvenance.Episode.Entry;
            long persisted = h.Current.lastSavedUtcUnix;
            var clock = new Clock { Seconds = 110 }; var time = new TimeService(new SimpleLogger(false), 10, 300, clock, () => 0);
            time.BeginRecoveryForeground(h.Inputs.TwoFloor.UnitsPerSecond); var reopened = CreateService(h.F, h.Inputs, time);
            var current = reopened.LoadOrCreate("reopen-pending", out var reason); Assert.That(current, Is.Not.Null, reason);
            time.BindLoadedForegroundHorizon(current.livingDungeon.Scheduler?.AcceptedThrough ?? -1);
            reopened.BeginCanonicalRecovery(current);
            Assert.That(time.RetainedRecoveryEntry, Is.Null); Assert.That(current.timeProvenance.Episode.Entry, Is.EqualTo(r));
            Assert.That(time.RecoveryEligible100nsForClockTest, Is.EqualTo((110 - persisted) * 10000000));
            var result = reopened.AdvanceCanonicalRecovery(current, h.Mana, (_, __) => Assert.Fail()); Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.RuntimeProjection.timeProvenance.Episode?.Entry, Is.EqualTo(originalComplete ? null : r));
        }
        [Test] public void DigestAloneCannotGrantCurrentWriterAuthorityWithoutValidatedInputs()
        {
            var h = new Harness(); var context = new DetachedCurrentTargetValidationContext(h.F.Compatibility, h.F.Production,
                LegacyGameplayConfigurationContract.SerializeCanonical(h.Inputs.Configuration), h.F.Profile.Canonical, h.Inputs.Occupancy,
                monsterRecovery: h.Inputs.Monsters);
            context.BindRecoveryContext(h.Inputs.Identity);
            Assert.That(DetachedCanonicalSaveSession.Open(h.Service.CanonicalSession.GetCurrentBytes(), context, h.F.Profile).IsSuccess, Is.False);
        }
        [TestCase(false)] [TestCase(true)]
        public void ForegroundRejectsForeignPolicyOrTickCadenceBeforeAcceptedWork(bool wrongTick)
        {
            var h = new Harness(); var bound = h.F.Context.RecoveryInputs;
            var policy = wrongTick ? bound.TwoFloor : LivingDungeonForegroundConfig.Parse(bound.Identity.GetInput(7));
            var before = h.F.FileSystem.ReadAllBytes(h.Service.SavePath);
            var result = h.F.Authority.AdvanceForeground(h.Service.SavePath, h.F.FileSystem, h.Service.CanonicalSession,
                h.Current, policy, 100, bound.Bootstrap.tickSeconds + (wrongTick ? 1 : 0), (_, __) => Assert.Fail(), h.Current.lastSavedUtcUnix);
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.Reason, Is.EqualTo(RecoveryEpisode.ContextMismatch));
            CollectionAssert.AreEqual(before, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
        }
        [TestCase(false)] [TestCase(true)]
        public void RecoveryRejectsForeignPassiveRateOrCapacityBeforeGrant(bool wrongCapacity)
        {
            var h = new Harness(); h.Start(); h.Accept(100); var bound = h.F.Context.RecoveryInputs;
            var passive = bound.Passive; var economy = bound.Economy;
            if (wrongCapacity) {
                var bytes = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bound.Identity.GetInput(2)).Replace("\"ManaCapacity\": 1000", "\"ManaCapacity\": 2000"));
                Assert.That(StructuralEconomySnapshot.TryParse(bytes, h.F.Production.Catalog, h.F.Profile.Canonical, out economy), Is.True);
            } else {
                var bytes = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bound.Identity.GetInput(1)).Replace("\"ManaPerCoreLevelPerMinute\": 2", "\"ManaPerCoreLevelPerMinute\": 3"));
                passive = PassiveOnlineManaConfigurationSnapshot.Load(bytes, h.F.Profile.Canonical).Value;
            }
            var online = new CanonicalPassiveManaService(passive, economy, new FormulaEngine(), h.F.Profile.Canonical.Spatial, bound.Bootstrap.tickSeconds);
            var foreign = new CanonicalOfflinePassiveManaService(passive, online);
            var before = h.F.FileSystem.ReadAllBytes(h.Service.SavePath);
            var transition = CanonicalTimeTransition.Recover(before, h.Current.timeProvenance, new UnixTimePoint(102, 0), 20000000,
                h.Current.livingDungeon, settleOriginal: true);
            Assert.That(h.F.Authority.PublishRecovery(h.Service.SavePath, h.F.FileSystem, h.Service.CanonicalSession, transition, foreign).IsSuccess, Is.False);
            CollectionAssert.AreEqual(before, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
        }
        sealed class RootScope : IDisposable
        {
            const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
            readonly UnityEngine.GameObject host = new UnityEngine.GameObject("H1F4-owned-root-integration");
            readonly Harness harness;
            SaveService attached;
            internal readonly GameRoot Root;
            internal RootScope(Harness h, bool bootPending = false)
            {
                harness = h; Root = host.AddComponent<GameRoot>();
                var content = new ContentService(); content.BindRecoveryBootstrap(h.Inputs);
                typeof(ContentService).GetProperty("ProductionSpatialContent").SetValue(content, h.F.Production);
                Property("Logger", new SimpleLogger(false, (_, __) => { })); Property("Content", content);
                Property("Save", h.Current); Property("TimeService", h.Time);
                Field("recoveryRuntimeInputs", h.Inputs); Field("bootRecoveryPending", bootPending);
                Field("_configuredSimulationTickSeconds", h.Inputs.Bootstrap.tickSeconds);
                Field("_offlineHeatConfiguration", h.Inputs.Configuration); Field("_offlinePassiveManaService", h.Mana);
                Field("_passiveManaConfiguration", h.Inputs.Passive); Field("_structuralEconomy", h.Inputs.Economy);
                Field("_passiveManaService", new CanonicalPassiveManaService(h.Inputs.Passive, h.Inputs.Economy, new FormulaEngine(),
                    h.F.Profile.Canonical.Spatial, h.Inputs.Bootstrap.tickSeconds));
                Rebind(h.Service, h.Time, h.Current);
            }
            internal Action<SaveData, long> PassiveTick => (Action<SaveData, long>)Delegate.CreateDelegate(typeof(Action<SaveData, long>), Root,
                typeof(GameRoot).GetMethod("ApplyForegroundPassiveTick", Private));
            internal void Rebind(SaveService service, TimeService time, SaveData current)
            {
                if (attached != null) { attached.BeforeForegroundAction = null; attached.ForegroundCurrent = null; }
                Root.AttachSaveServiceForTests(service); attached = service;
                Property("Save", current); Property("TimeService", time);
                service.BeforeForegroundAction = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), Root,
                    typeof(GameRoot).GetMethod("FlushForeground", Private));
                service.ForegroundCurrent = () => Root.Save;
            }
            void Property(string name, object value) => typeof(GameRoot).GetProperty(name).SetValue(Root, value);
            void Field(string name, object value) => typeof(GameRoot).GetField(name, Private).SetValue(Root, value);
            internal void Pulse(long seconds) {
                harness.Wall.Seconds = seconds;
                typeof(GameRoot).GetMethod("UpdateForeground", Private).Invoke(Root, null);
                Assert.That(typeof(GameRoot).GetField("foregroundFailure", Private).GetValue(Root), Is.Null);
            }
            internal void Focus(bool value, long seconds) {
                harness.Wall.Seconds = seconds;
                typeof(GameRoot).GetMethod("OnApplicationFocus", Private).Invoke(Root, new object[] { value });
            }
            public void Dispose() {
                attached.BeforeForegroundAction = null; attached.ForegroundCurrent = null;
                Root.AttachSaveServiceForTests(null); Property("TimeService", null);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
        [Test] public void RootCoordinatorKeepsActionsBehindEntryOriginalAndResidualClosure()
        {
            var h = new Harness(); h.Start(); using var scope = new RootScope(h);
            Assert.That(h.Service.Save(h.Current, SaveReason.ManualDev), Is.False);
            scope.Pulse(100); Assert.That(scope.Root.Save.timeProvenance.Episode.Phase, Is.EqualTo(RecoveryEpisode.OriginalPending));
            Assert.That(h.Service.Save(h.Current, SaveReason.ManualDev), Is.False);
            scope.Pulse(102); Assert.That(scope.Root.Save.timeProvenance.Episode.Phase, Is.EqualTo(RecoveryEpisode.ResidualPending));
            Assert.That(h.Time.ForegroundThrough, Is.Zero);
            scope.Pulse(105); Assert.That(scope.Root.Save.timeProvenance.Episode, Is.Null);
            Assert.That(h.Service.Save(h.Current, SaveReason.ManualDev), Is.True);
            Assert.That(scope.Root.Save.timeProvenance.AccountedBoundary.Seconds, Is.EqualTo(105));
        }
        [Test] public void RootRetainsOneBackgroundObligationUntilObservedReturnAndBlocksActions()
        {
            var h = new Harness(); h.Start(); h.Accept(100); h.Accept(102); h.Accept(105);
            using var scope = new RootScope(h); scope.Focus(false, 106);
            Assert.That(h.Time.HasDeferredRecovery, Is.True); Assert.That(h.Time.RecoveryClockActive, Is.False);
            var before = h.F.FileSystem.ReadAllBytes(h.Service.SavePath);
            scope.Pulse(110); scope.Pulse(115); scope.Pulse(120);
            CollectionAssert.AreEqual(before, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
            Assert.That(h.Service.Save(h.Current, SaveReason.ManualDev), Is.False);
            scope.Focus(true, 120);
            Assert.That(h.Time.RecoveryClockActive, Is.True);
            scope.Pulse(120); scope.Pulse(121); scope.Pulse(122);
            Assert.That(scope.Root.Save.timeProvenance.Episode, Is.Null); Assert.That(h.Time.HasDeferredRecovery, Is.False);
            Assert.That(h.Time.ForegroundThrough, Is.EqualTo(1000), "Only the full foreground prefix before focus loss advances adventurers.");
        }
        [Test] public void RootDoesNotCreateRecoveryEpisodesEveryFrameWhileBootRemainsBackground()
        {
            var h = new Harness(); h.Start(); h.Time.SetForegroundFocus(false);
            using var scope = new RootScope(h, bootPending: true);
            scope.Pulse(100); scope.Pulse(102); scope.Pulse(105);
            Assert.That(h.Time.HasDeferredRecovery, Is.True); Assert.That(h.Time.RecoveryClockActive, Is.False);
            var before = h.F.FileSystem.ReadAllBytes(h.Service.SavePath);
            scope.Pulse(110); scope.Pulse(120);
            CollectionAssert.AreEqual(before, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
            Assert.That(scope.Root.GameplayServicesInitializedForTests, Is.False);
            Assert.That(h.Time.ForegroundThrough, Is.Zero);
        }
        [Test] public void AcceptedTicksAndResearchDrainAcrossTwoCrashesWithoutReplayingClosedPrefix()
        {
            var h = new Harness(); h.Start(); h.Accept(100); h.Accept(102); h.Accept(105);
            // A declared synthetic prior confirmed record supplies this fixture's
            // Research prerequisite. It is never historical producer evidence.
            h.Current.runHistory.NextRunSequence = 2;
            h.Current.runHistory.AppendOutcome(new RunOutcomeRecord { RunId = "run-1" }, 5);
            Assert.That(h.Service.Save(h.Current, SaveReason.StateChange), Is.True);
            using var scope = new RootScope(h);
            var started = scope.Root.StartConfiguredPlayerResearch(); Assert.That(started.Succeeded, Is.True, started.FeedbackLocalizationKey);
            var ticks = new List<long>(); var passive = scope.PassiveTick;
            void Tick(SaveData candidate, long tick) { ticks.Add(tick); passive(candidate, tick); }
            h.Wall.Seconds = 2105;
            var accepted = h.Service.AdvanceForeground(h.Current, h.Time.CaptureForegroundObservation(h.Service.CanonicalSession),
                h.Inputs.Bootstrap.tickSeconds, Tick);
            Assert.That(accepted.IsSuccess, Is.True, accepted.Reason);
            long acceptedHorizon = h.Current.livingDungeon.Scheduler.AcceptedThrough;
            long firstClosed = h.Current.livingDungeon.Scheduler.ClosedThrough;
            long firstTicks = h.Current.totalTicks;
            Assert.That(acceptedHorizon, Is.EqualTo(2000000)); Assert.That(firstClosed, Is.LessThan(acceptedHorizon));
            Assert.That(h.Current.researchProgress.CompletionPending, Is.True);

            SaveService Open(Clock clock, out TimeService time, out SaveData current) {
                time = new TimeService(new SimpleLogger(false), 10, 300, clock, () => 0);
                time.BeginRecoveryForeground(h.Inputs.TwoFloor.UnitsPerSecond);
                var service = CreateService(h.F, h.Inputs, time);
                current = service.LoadOrCreate("accepted-reopen", out var reason); Assert.That(current, Is.Not.Null, reason);
                Assert.That(time.RetainedRecoveryEntry, Is.Null, "Authority must be established before a new entry observation.");
                time.BindLoadedForegroundHorizon(current.livingDungeon.Scheduler.AcceptedThrough);
                scope.Rebind(service, time, current); service.BeginCanonicalRecovery(current); return service;
            }
            void Step(SaveService service, Clock clock, long at) {
                clock.Seconds = at;
                var result = service.AdvanceCanonicalRecovery(scope.Root.Save, h.Mana, Tick);
                Assert.That(result.IsSuccess, Is.True, result.Reason);
                Assert.That(scope.Root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(acceptedHorizon));
            }
            var one = new Clock { Seconds = 2200 };
            var first = Open(one, out var firstTime, out var firstSave);
            Assert.That(firstSave.totalTicks, Is.EqualTo(firstTicks)); Assert.That(firstSave.livingDungeon.Scheduler.ClosedThrough, Is.EqualTo(firstClosed));
            Step(first, one, 2200); Assert.That(scope.Root.Save.timeProvenance.Episode.Entry.Seconds, Is.EqualTo(2200));
            Assert.That(scope.Root.Save.totalTicks, Is.EqualTo(firstTicks), "Entry publication precedes any drain.");
            Step(first, one, 2201);
            long secondClosed = scope.Root.Save.livingDungeon.Scheduler.ClosedThrough;
            Assert.That(secondClosed, Is.GreaterThan(firstClosed).And.LessThan(acceptedHorizon));
            long secondTicks = scope.Root.Save.totalTicks;

            var two = new Clock { Seconds = 2300 };
            var second = Open(two, out var secondTime, out var secondSave);
            Assert.That(secondTime.RetainedRecoveryEntry, Is.Null);
            Assert.That(secondSave.timeProvenance.Episode.Entry.Seconds, Is.EqualTo(2200));
            Assert.That(secondSave.totalTicks, Is.EqualTo(secondTicks)); Assert.That(secondSave.livingDungeon.Scheduler.ClosedThrough, Is.EqualTo(secondClosed));
            int pumps = 0;
            while (!scope.Root.Save.livingDungeon.IsClosed && pumps++ < 10) Step(second, two, 2300 + pumps);
            Assert.That(scope.Root.Save.livingDungeon.IsClosed, Is.True);
            Assert.That(scope.Root.Save.totalTicks, Is.EqualTo(200));
            CollectionAssert.AreEqual(Enumerable.Range(1, 200).Select(n => (long)n).ToArray(), ticks);
            Assert.That(scope.Root.Save.timeProvenance.Episode.Phase, Is.EqualTo(RecoveryEpisode.OriginalPending));
            Assert.That(scope.Root.Save.timeProvenance.Episode.OriginalEligible100ns, Is.EqualTo(950000000));
            Assert.That(scope.Root.ClaimConfiguredPlayerResearch().Succeeded, Is.False, "Recovery still owns the action barrier.");
            double originalWallet = h.Mana.ResolveRecoveryDuration(scope.Root.Save, h.Inputs.Configuration, 950000000, false).WalletAfter;
            Step(second, two, 2310);
            Assert.That(scope.Root.Save.structureRuntime.ManaReserve, Is.EqualTo(originalWallet));
            Assert.That(scope.Root.Save.timeProvenance.Episode.ResidualEligible100ns, Is.EqualTo(1100000000));
            double residualWallet = h.Mana.ResolveRecoveryDuration(scope.Root.Save, h.Inputs.Configuration, 1110000000, false).WalletAfter;
            Step(second, two, 2311);
            Assert.That(scope.Root.Save.structureRuntime.ManaReserve, Is.EqualTo(residualWallet));
            Assert.That(scope.Root.Save.timeProvenance.Episode, Is.Null);
            Assert.That(secondTime.ForegroundThrough, Is.EqualTo(acceptedHorizon), "UTC recovery never creates new adventurer time.");
            Assert.That(scope.Root.ClaimConfiguredPlayerResearch().Succeeded, Is.True);
            string project = h.Inputs.Configuration.MvpFirstSessionObjective.AnalysisResearchProjectId;
            Assert.That(scope.Root.Save.completedResearch.ProjectIds.Count(p => p == project), Is.EqualTo(1));
            var once = h.F.FileSystem.ReadAllBytes(second.SavePath);
            Assert.That(scope.Root.ClaimConfiguredPlayerResearch().Succeeded, Is.False);
            CollectionAssert.AreEqual(once, h.F.FileSystem.ReadAllBytes(second.SavePath));
        }

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
