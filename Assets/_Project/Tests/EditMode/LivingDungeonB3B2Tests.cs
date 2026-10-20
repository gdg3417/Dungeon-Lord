#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;
using B = DungeonBuilder.M0.Tests.EditMode.LivingDungeonPhaseABaselineTests;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class LivingDungeonB3B2Tests
    {
        private static LivingDungeonForegroundConfig Policy => LivingDungeonForegroundConfig.Load();
        private static Fixture Dungeon()
        {
            var f = Fixture.Create(null); f.Configuration = CombatC0IntegrationTests.Config();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)));
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);
            return Support(f);
        }
        private static Fixture Support(Fixture f)
        {
            f.Profile = SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
            f.Context = new DetachedCurrentTargetValidationContext(f.Compatibility, f.Production,
                LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.Profile.Canonical, f.Occupancy,
                livingDungeonProfiles: new[] { Policy.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized) },
                monsterRecovery: MonsterRecoveryRules.Capture(f.Configuration));
            f.Reopen(); return f;
        }
        private static RunExecution Start(Fixture f) => new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot())
            .StartTimedCombatSnapshot(1, PhaseSixA4Tests.Snapshot(f), Policy.Timing);
        private static DetachedCanonicalWriteResult Pump(Fixture f, long target, LivingDungeonForegroundConfig policy = null) => PhaseSixA4Tests.Writer(f).AdvanceForeground(
            f.ActivePath, f.FileSystem, f.Session, f.Runtime, policy ?? Policy, target, 10, (save, tick) => { }, 1000);
        private static void Through(Fixture f, long target)
        {
            for (int i = 0; i < 100; i++) {
                f.Accept(Pump(f, target));
                if (f.Runtime.livingDungeon.IsClosed && f.Runtime.livingDungeon.Scheduler.AcceptedThrough == target) return;
            }
            Assert.Fail("bounded test pump did not close");
        }
        private static long Admitted(Fixture f)
        {
            Through(f, 0); long arrival = f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value;
            Through(f, arrival); return arrival;
        }
        private static long Finish(Fixture f)
        {
            var p = f.Runtime.livingDungeon.Parties.Single();
            var e = LivingDungeonContracts.Restore(p, Policy.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized));
            try { e.DrainToCompletion(); return checked(p.AdmittedAt + e.Inspect().LogicalTime); } finally { e.Abandon(); }
        }

        [Test] public void PresentationObserverCannotChangeCheckpointBytesWorkReceiptsOrOutcome()
        {
            var f = Dungeon(); Admitted(f); var party = f.Runtime.livingDungeon.Parties.Single();
            var profile = Policy.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized);
            var plain = LivingDungeonContracts.Restore(party, profile);
            var observed = LivingDungeonContracts.Restore(party, profile, retainPresentation: true);
            byte[] canonical = f.Session.GetCurrentBytes();
            try {
                while (!plain.IsTerminal) {
                    long due = plain.NextDueTime().Value;
                    Assert.That(observed.NextDueTime(), Is.EqualTo(due));
                    Assert.That(observed.NextDueTime(), Is.EqualTo(due));
                    CollectionAssert.AreEqual(plain.ExportCheckpoint(profile.Checkpoints).GetBytes(), observed.ExportCheckpoint(profile.Checkpoints).GetBytes());
                    if (due > plain.Inspect().LogicalTime) {
                        plain.AdvanceThrough(due - 1, 1, plain.NextOrdinal);
                        observed.AdvanceThrough(due - 1, 1, observed.NextOrdinal);
                        Assert.That(observed.PresentationSteps.Count, Is.EqualTo(observed.NextOrdinal), "Lookahead is never a recorded effect");
                    }
                    Assert.That(B.Structured(observed.AdvanceThrough(due, 1, observed.NextOrdinal)),
                        Is.EqualTo(B.Structured(plain.AdvanceThrough(due, 1, plain.NextOrdinal))));
                    CollectionAssert.AreEqual(plain.ExportCheckpoint(profile.Checkpoints).GetBytes(), observed.ExportCheckpoint(profile.Checkpoints).GetBytes());
                }
                var expected = plain.TakeOutcome(); var shown = observed.TakeOutcome();
                Assert.That(B.Structured(shown), Is.EqualTo(B.Structured(expected)));
                Assert.That(B.Structured(CombatC0Diagnostics.Events(shown)), Is.EqualTo(B.Structured(CombatC0Diagnostics.Events(expected))));
                Assert.That(CombatC0Diagnostics.ChargedWork(shown), Is.EqualTo(CombatC0Diagnostics.ChargedWork(expected)));
                Assert.That(shown.PresentationSteps.Length, Is.EqualTo(observed.NextOrdinal));
                Assert.That(shown.PresentationSteps.Last().Kind, Is.EqualTo(RunStepKind.Terminal));
                CollectionAssert.AreEqual(canonical, f.Session.GetCurrentBytes());
            } finally { plain.Abandon(); observed.Abandon(); }
        }

        [Test] public void AutomaticSettlementRetainsSessionReplayWithoutChangingHistoricalSaveContract()
        {
            var f = Dungeon(); Admitted(f); Through(f, Finish(f));
            var outcome = f.Runtime.runHistory.LatestOutcome;
            Assert.That(outcome.PresentationSteps, Is.Not.Empty);
            Assert.That(outcome.PresentationLayoutFingerprint, Is.Not.Null);
            Assert.That(outcome.PresentationCorridorFingerprint, Is.Not.Null);
            Assert.That(CombatC0Diagnostics.Events(outcome), Is.Not.Empty);
            string savedSummary = JsonUtility.ToJson(outcome);
            string bytes = System.Text.Encoding.UTF8.GetString(f.Session.GetCurrentBytes());
            Assert.That(bytes, Does.Not.Contain("PresentationSteps").And.Not.Contain("CombatEvents"));
            f.Reopen();
            Assert.That(JsonUtility.ToJson(f.Runtime.runHistory.LatestOutcome), Is.EqualTo(savedSummary));
            Assert.That(f.Runtime.runHistory.LatestOutcome.PresentationSteps, Is.Null);
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(r => r.RunId == outcome.RunId), Is.EqualTo(1));
        }

        [Test] public void TerminalSettlementPreservesTheCurrentSavedBoundary()
        {
            var f = Dungeon(); Admitted(f); long finish = Finish(f); Through(f, finish - 1);
            // Test-only passive period is derived beyond this run's terminal
            // boundary so a coincident tick cannot mask lost lifecycle metadata.
            int tickSeconds = checked((int)(finish / Policy.UnitsPerSecond + 1));
            var result = PhaseSixA4Tests.Writer(f).AdvanceForeground(f.ActivePath, f.FileSystem, f.Session,
                f.Runtime, Policy, finish, tickSeconds, (save, tick) => { }, 2000);
            f.Accept(result);
            Assert.That(f.Runtime.runHistory.LatestOutcome.RunId, Is.EqualTo("run-1"));
            Assert.That(f.Runtime.lastSavedUtcUnix, Is.EqualTo(2000));
            f.Reopen(); Assert.That(f.Runtime.lastSavedUtcUnix, Is.EqualTo(2000));
        }

        [Test] public void AcceptedButUnclosedHorizonReopensWithoutDroppingTimeOrDuplicatingHistory()
        {
            var budget = LivingDungeonForegroundConfig.Parse(System.Text.Encoding.UTF8.GetBytes(
                File.ReadAllText("Assets/_Project/Resources/living_dungeon_foreground.json").Replace("\"MaximumBoundariesPerPump\": 64", "\"MaximumBoundariesPerPump\": 1")));
            var f = Dungeon(); f.Accept(Pump(f, 20000, budget));
            Assert.That(f.Runtime.livingDungeon.IsClosed, Is.False);
            Assert.That(f.Runtime.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(20000));
            f.Reopen(); Through(f, 20000);
            Assert.That(f.Runtime.totalTicks, Is.EqualTo(2));
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Select(r => r.RunId).Distinct().Count(), Is.EqualTo(f.Runtime.runHistory.RecentOutcomes.Length));
        }

        [TestCase(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush, 1)]
        [TestCase(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 1)]
        [TestCase(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write, 2)]
        public void FailedTerminalPumpPublishesNothingAndProvenRollbackRetriesExactlyOnce(Gd66DetachedSpatialMigrationTransactionTests.OperationType op, int occurrence)
        {
            var f = Dungeon(); Admitted(f); long finish = Finish(f);
            var before = f.Session.GetCurrentBytes(); string state = B.Structured(f.Runtime);
            f.FileSystem.EnableFailure(op, occurrence); var result = Pump(f, finish); f.FileSystem.DisableFailure();
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.RuntimeProjection, Is.Null);
            Assert.That(result.Session, Is.Null); CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(B.Structured(f.Runtime), Is.EqualTo(state)); f.Reopen(); Through(f, finish);
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(r => r.RunId == "run-1"), Is.EqualTo(1));
        }

        [Test] public void UnprovenRollbackRequiresRecoveryAndDoesNotPublishTerminalEffects()
        {
            var f = Dungeon(); Admitted(f); long finish = Finish(f); var old = f.Runtime;
            f.FileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            f.FileSystem.Seed(f.ActivePath, f.Session.GetCurrentBytes());
            f.FileSystem.EnableFailureSequence(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush, 1,
                Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 2);
            var failed = Pump(f, finish); f.FileSystem.DisableFailure();
            Assert.That(failed.IsSuccess, Is.False); Assert.That(failed.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
            Assert.That(failed.RuntimeProjection, Is.Null); Assert.That(f.Runtime, Is.SameAs(old));
            Assert.That(f.Runtime.runHistory.LatestOutcome?.RunId, Is.Not.EqualTo("run-1"));
        }

        [Test] public void CurrentRecoveryBeforeSettlementIsChargedOnceAndDamageIsThenAppliedToRecoveredIdentity()
        {
            var f = Dungeon(); CombatC2Tests.SetState(f, 2, false, 0); Admitted(f);
            var start = f.Runtime.livingDungeon.Parties.Single().Checkpoint;
            // Authorized condition change during occupancy, representing a completed
            // manual action. The foreground producer cannot recover an occupied floor.
            CombatC2Tests.SetState(f, 3, false, 100);
            Through(f, f.Runtime.livingDungeon.Scheduler.ClosedThrough + 1);
            CollectionAssert.AreEqual(start, f.Runtime.livingDungeon.Parties.Single().Checkpoint);
            long finish = Finish(f); Through(f, finish);
            var m = f.Runtime.runHistory.LatestOutcome.CombatMonsters.Single();
            int damage = m.InitialHealth - m.Health;
            // Vacant recovery uses the existing configured living-first resolver.
            int ready = MonsterRecoveryRules.Capture(f.Configuration).Readiness(m.OptionId);
            Assert.That(f.Runtime.monsterContinuity.Records.Single().Health, Is.EqualTo(ready));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(100 - (ready - Math.Max(0, 3 - damage))));
            double wallet = f.Runtime.structureRuntime.ManaReserve; f.Reopen(); Through(f, finish);
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(wallet));
        }

        [Test] public void MalformedProductionConfigurationRejectsOverflowAndUnsupportedProfile()
        {
            string invalidMovement = File.ReadAllText("Assets/_Project/Resources/living_dungeon_foreground.json")
                .Replace("\"UnitsPerTileEdge\": 1000", "\"UnitsPerTileEdge\": 0");
            Assert.Throws<ArgumentException>(() => LivingDungeonForegroundConfig.Parse(System.Text.Encoding.UTF8.GetBytes(invalidMovement)));
            string json = File.ReadAllText("Assets/_Project/Resources/living_dungeon_foreground.json");
            Assert.Throws<ArgumentException>(() => LivingDungeonForegroundConfig.Parse(System.Text.Encoding.UTF8.GetBytes(json.Replace("1000", "2147483648"))));
            Assert.Throws<ArgumentException>(() => LivingDungeonForegroundConfig.Parse(System.Text.Encoding.UTF8.GetBytes(json.Replace(LivingDungeonForegroundConfig.ProfileId, "unknown"))));
        }

        [TestCase("0", false)]
        [TestCase("-1", false)]
        [TestCase("1", true)]
        [TestCase("1000000", true)]
        [TestCase("1.5", false)]
        [TestCase("\"1000000\"", false)]
        [TestCase("null", false)]
        [TestCase("9223372036854775808", false)]
        public void ProductionCombatWorkEnvelopeMustBePositiveInteger(string value, bool valid)
        {
            string json = File.ReadAllText("Assets/_Project/Resources/living_dungeon_foreground.json")
                .Replace("\"MaximumCombatWork\": 1000000", "\"MaximumCombatWork\": " + value);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            if (valid) Assert.DoesNotThrow(() => LivingDungeonForegroundConfig.Parse(bytes));
            else Assert.That(Assert.Throws<ArgumentException>(() => LivingDungeonForegroundConfig.Parse(bytes)).Message,
                Does.StartWith(LivingDungeonForegroundConfig.Invalid));
        }

        private sealed class MutableUtc : ITimeSource { public long Now = 2000; public long UtcNowUnixSeconds() => Now; }
        private static SaveService Service(Fixture f, ITimeSource utc)
        {
            var service = new SaveService(new SimpleLogger(false, (level, message) => { }),
                new SaveConfig { fileName = Path.GetFileName(f.ActivePath), useAtomicWrites = true }, Path.GetDirectoryName(f.ActivePath));
            service.ConfigureCanonical(f.Profile, f.Production, f.Compatibility, f.Configuration,
                LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.PositionProfiles, f.Occupancy);
            service.ConfigureStructuralEconomy(f.Economy); service.ConfigureContentAcquisitionEconomy(f.Acquisition);
            service.ConfigureStructuralRemovalPolicy(f.RemovalPolicy); service.ConfigureRunLoot(PhaseSixA4Tests.Loot());
            service.SetTimeSourceForTests(utc);
            service.SetPreflightEvaluatorForTests(path => new SpatialMigrationActivationPreflight(true,
                SpatialMigrationCapabilityReason.Ready, SpatialMigrationPlatform.WindowsEditor, f.FileSystem, path));
            var loaded = service.LoadOrCreate("foreground-test", out string banner);
            Assert.That(loaded, Is.Not.Null, banner); f.Runtime = loaded; f.State = loaded.validatedCanonicalSpatialState;
            return service;
        }

        [Test] public void FreshNativeSessionBindsProductionProfilesBeforeDormantActivation()
        {
            var f = Dungeon(); f.FileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            var service = Service(f, new Utc());
            Assert.That(f.Runtime.livingDungeon.IsDormant, Is.True);
            f.Accept(service.AdvanceForeground(f.Runtime, 0, 10, (save, tick) => { }));
            Assert.That(f.Runtime.livingDungeon.Scheduler.ProfileId, Is.EqualTo(LivingDungeonForegroundConfig.ProfileId));
            Assert.That(f.Runtime.livingDungeon.IsClosed, Is.True);
            service = Service(f, new Utc());
            Assert.That(f.Runtime.livingDungeon.Scheduler.AcceptedThrough, Is.Zero);
        }

        [Test] public void IndependentlyInjectedHistoricalLimitsDoNotAcquireProductionScheduling()
        {
            var f = Fixture.Create(null);
            Assert.That(f.Profile.Canonical.ForSchema(13).Serialized.MaximumInputBytes, Is.Not.EqualTo(Policy.CreateProfile(default).MaximumBaseSaveBytes));
            var service = Service(f, new Utc());
            Assert.That(service.ForegroundConfiguration, Is.Null);
            Assert.That(f.Runtime.livingDungeon.IsDormant, Is.True);
            Assert.That(service.Save(f.Runtime, SaveReason.ManualDev), Is.True);
        }

        [Test] public void UnsupportedCombatWorkConsumesOpportunityWithoutRunAllocationOrGameplayDamage()
        {
            var f = Dungeon(); f.Configuration.CombatC0.MaximumWork = 1; Support(f);
            Through(f, 0); long due = f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value;
            string monsters = B.Structured(f.Runtime.monsterContinuity); int sequence = f.Runtime.runHistory.NextRunSequence;
            f.Accept(Pump(f, due));
            Assert.That(f.Runtime.livingDungeon.Parties, Is.Empty);
            Assert.That(f.Runtime.livingDungeon.Scheduler.NextOpportunityOrdinal, Is.EqualTo(1));
            Assert.That(f.Runtime.livingDungeon.Scheduler.LastOpportunityTime, Is.EqualTo(due));
            Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(sequence));
            Assert.That(f.Runtime.runHistory.LatestOutcome?.RunId, Is.Null.Or.Empty);
            Assert.That(B.Structured(f.Runtime.monsterContinuity), Is.EqualTo(monsters));
        }

        [Test] public void QualificationCannotConfigureProductionProfileAndTwoValidActivePrefixesCannotBypassCapacity()
        {
            var f = Dungeon(); var before = f.Session.GetCurrentBytes();
            var q = new DetachedCanonicalWriteAuthority.SyntheticForegroundQualification(PhaseSixA4Tests.Writer(f), f.Session);
            Assert.That(q.Configure(f.ActivePath, f.FileSystem, f.Session, LivingDungeonForegroundConfig.ProfileId, Policy.MinimumArrival).IsSuccess, Is.False);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Admitted(f); before = f.Session.GetCurrentBytes();
            var existing = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties.Single(), Policy.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized));
            var second = new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot()).StartTimedCombatSnapshot(2, existing.Snapshot, Policy.Timing);
            var other = new LivingDungeonParty { Sequence = 2, AdmittedAt = f.Runtime.livingDungeon.Parties.Single().AdmittedAt };
            try {
                while (second.NextDueTime() == 0) second.AdvanceThrough(0, 1, second.NextOrdinal);
                var profile = Policy.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized);
                LivingDungeonScheduling.Pin(other, second, profile); f.Runtime.livingDungeon.Parties.Add(other);
                Assert.That(LivingDungeonContracts.Validate(f.Runtime.livingDungeon, new[] { profile }), Is.False);
            } finally { existing.Abandon(); second.Abandon(); f.Runtime.livingDungeon.Parties.Remove(other); }
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [Test] public void ReinspectionAndPausedHorizonDrainRetainOfflineBoundary()
        {
            var f = Dungeon(); var utc = new MutableUtc(); var service = Service(f, utc);
            f.Accept(service.AdvanceForeground(f.Runtime, 0, 10, (save, tick) => { }));
            long boundary = f.Runtime.lastSavedUtcUnix; Assert.That(boundary, Is.EqualTo(2000));
            utc.Now = 9000; f.Accept(service.AdvanceForeground(f.Runtime, 0, 10, (save, tick) => { }));
            Assert.That(f.Runtime.lastSavedUtcUnix, Is.EqualTo(boundary));
            f.Accept(service.AdvanceForeground(f.Runtime, 1, 10, (save, tick) => { }));
            Assert.That(f.Runtime.lastSavedUtcUnix, Is.EqualTo(9000));
        }

        [Test] public void DirtyClosedActionBarrierRetainsSessionAndDisabledPauseDoesNotPublish()
        {
            var f = Dungeon(); var service = Service(f, new Utc());
            f.Accept(service.AdvanceForeground(f.Runtime, 0, 10, (save, tick) => { }));
            double now = 0;
            var time = new TimeService(new SimpleLogger(false), 10, 300, new Utc(), () => now);
            time.AttachSave(f.Runtime); time.BeginForeground(1000, 0);
            var host = new GameObject("foreground-disabled-root-test"); host.SetActive(false);
            var root = host.AddComponent<GameRoot>();
            try {
                typeof(GameRoot).GetProperty(nameof(GameRoot.Save)).SetValue(root, f.Runtime);
                typeof(GameRoot).GetProperty(nameof(GameRoot.SaveService)).SetValue(root, service);
                typeof(GameRoot).GetProperty(nameof(GameRoot.TimeService)).SetValue(root, time);
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var before = f.Session.GetCurrentBytes(); int published = 0;
                service.CanonicalRuntimePublished += save => published++;
                Assert.That(typeof(GameRoot).GetMethod("FlushForeground", flags).Invoke(root, null), Is.True);
                typeof(GameRoot).GetMethod("OnApplicationPause", flags).Invoke(root, new object[] { true });
                Assert.That(time.IsPaused, Is.False); Assert.That(published, Is.Zero);
                CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
                time.OnPause();
                typeof(GameRoot).GetField("foregroundApplicationPaused", flags).SetValue(root, false);
                typeof(GameRoot).GetMethod("OnEnable", flags).Invoke(root, null);
                Assert.That(time.IsPaused, Is.False); Assert.That(time.SampleForeground(), Is.Zero);
            } finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test] public void DeferredPrePauseSampleDoesNotConsumeLaterOfflineInterval()
        {
            var f = Dungeon(); var utc = new MutableUtc(); var service = Service(f, utc);
            var budget = LivingDungeonForegroundConfig.Parse(System.Text.Encoding.UTF8.GetBytes(
                File.ReadAllText("Assets/_Project/Resources/living_dungeon_foreground.json").Replace("\"MaximumBoundariesPerPump\": 64", "\"MaximumBoundariesPerPump\": 1")));
            typeof(SaveService).GetProperty(nameof(SaveService.ForegroundConfiguration)).SetValue(service, budget);
            f.Accept(service.AdvanceForeground(f.Runtime, 20000, 10, (save, tick) => { }));
            Assert.That(f.Runtime.livingDungeon.IsClosed, Is.False);
            Assert.That(f.Runtime.lastSavedUtcUnix, Is.EqualTo(2000));
            utc.Now = 9000;
            f.Accept(service.AdvanceForeground(f.Runtime, 30000, 10, (save, tick) => { }, 3000));
            Assert.That(f.Runtime.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(20000));
            Assert.That(f.Runtime.lastSavedUtcUnix, Is.EqualTo(2000));
            f.Accept(service.AdvanceForeground(f.Runtime, 30000, 10, (save, tick) => { }, 3000));
            Assert.That(f.Runtime.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(30000));
            Assert.That(f.Runtime.lastSavedUtcUnix, Is.EqualTo(3000));
            f.Accept(service.AdvanceForeground(f.Runtime, 30000, 10, (save, tick) => { }, 3000));
            Assert.That(f.Runtime.lastSavedUtcUnix, Is.EqualTo(3000));
        }

        [Test] public void ServiceLatchesRecoveryRequiredAndBlocksAllLaterForegroundPublication()
        {
            var f = Dungeon(); Admitted(f); long finish = Finish(f);
            f.FileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            f.FileSystem.Seed(f.ActivePath, f.Session.GetCurrentBytes());
            var service = Service(f, new Utc()); int published = 0;
            service.CanonicalRuntimePublished += save => published++;
            f.FileSystem.EnableFailureSequence(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush, 1,
                Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 2);
            var failed = service.AdvanceForeground(f.Runtime, finish, 10, (save, tick) => { }); f.FileSystem.DisableFailure();
            Assert.That(failed.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
            Assert.That(service.AdvanceForeground(f.Runtime, finish, 10, (save, tick) => { }).Reason, Is.EqualTo(failed.Reason));
            Assert.That(service.Save(f.Runtime, SaveReason.AppPause), Is.False); Assert.That(published, Is.Zero);
        }

        [Test] public void PublishedPacingIsExplicitApprovedAndSeparateFromQualificationProfile()
        {
            var p = Policy; Assert.That(p.UnitsPerSecond, Is.EqualTo(1000));
            Assert.That(p.Timing.UnitsPerTileEdge, Is.EqualTo(1000)); Assert.That(p.Timing.MonsterInteractionUnits, Is.EqualTo(3000));
            Assert.That(p.Timing.TrapInteractionUnits, Is.EqualTo(1000)); Assert.That(p.Timing.LootInteractionUnits, Is.EqualTo(1000));
            Assert.That(new[] { p.Interval("low"), p.Interval("building"), p.Interval("steady"), p.Interval("heavy"), p.Interval("dangerous_churn") },
                Is.EqualTo(new long[] { 90000, 60000, 45000, 30000, 60000 }));
            Assert.That(p.Interval("none"), Is.Zero);
            Assert.That(p.CreateProfile(default).Id, Is.Not.EqualTo("living.qualification.profile_a.v1"));
        }

        [Test] public void QualifiedProductionLifetimeMeasuresCheckpointReplayAndBoundaryWork()
        {
            var f = Dungeon(); var watch = System.Diagnostics.Stopwatch.StartNew(); Admitted(f);
            var p = f.Runtime.livingDungeon.Parties.Single(); var profile = Policy.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized);
            var e = LivingDungeonContracts.Restore(p, profile); int maximumBytes = 0, sameTimeEvents = 0, maximumSameTimeEvents = 0;
            long previous = -1;
            try {
                while (!e.IsTerminal) {
                    long due = e.NextDueTime().Value;
                    sameTimeEvents = due == previous ? sameTimeEvents + 1 : 1; previous = due;
                    maximumSameTimeEvents = Math.Max(maximumSameTimeEvents, sameTimeEvents);
                    e.AdvanceThrough(due, 1, e.NextOrdinal);
                    maximumBytes = Math.Max(maximumBytes, e.ExportCheckpoint(profile.Checkpoints).GetBytes().Length);
                }
                var outcome = e.TakeOutcome();
                TestContext.WriteLine("B3B2 measured bytes=" + maximumBytes + " replay=" + e.NextOrdinal +
                    " sameTimeEvents=" + maximumSameTimeEvents + " combatWork=" + CombatC0Diagnostics.ChargedWork(outcome) +
                    " logicalDuration=" + e.Inspect().LogicalTime + " admissionAndReplayMs=" + watch.ElapsedMilliseconds);
                Assert.That(maximumBytes, Is.LessThanOrEqualTo(profile.MaximumCheckpointBytes));
                Assert.That(e.NextOrdinal, Is.LessThanOrEqualTo(profile.MaximumPartyReplay));
                Assert.That(maximumSameTimeEvents, Is.LessThanOrEqualTo(Policy.MaximumBoundaryEvents));
            } finally { e.Abandon(); }
        }

        [Test] public void ActualAdmissionRetainsAnUnfinishedTimedC2PrefixThenSettlesAndRetiresExactlyOnce()
        {
            var f = Dungeon(); Through(f, 0);
            long arrival = f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value;
            Assert.That(arrival, Is.InRange(30000, 90000));
            Through(f, arrival - 1); Assert.That(f.Runtime.livingDungeon.Parties, Is.Empty);
            Through(f, arrival); var party = f.Runtime.livingDungeon.Parties.Single();
            Assert.That(party.Sequence, Is.EqualTo(1)); Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(2));
            Assert.That(f.Runtime.runHistory.LatestOutcome?.RunId, Is.Not.EqualTo("run-1"));
            var e = LivingDungeonContracts.Restore(party, Policy.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized));
            Assert.That(e.Combat.Kernel.RulesId, Is.EqualTo(MonsterRecoveryRules.Executable)); Assert.That(e.IsTerminal, Is.False);
            RunOutcomeRecord expected;
            long finish; try { expected = e.DrainToCompletion(); finish = arrival + e.Inspect().LogicalTime; } finally { e.Abandon(); }
            Through(f, finish); Assert.That(f.Runtime.livingDungeon.Parties, Is.Empty);
            Assert.That(f.Runtime.runHistory.LatestOutcome.RunId, Is.EqualTo("run-1"));
            Assert.That(B.Structured(CombatC0Diagnostics.Events(f.Runtime.runHistory.LatestOutcome)), Is.EqualTo(B.Structured(CombatC0Diagnostics.Events(expected))));
            Assert.That(B.Structured(CombatC0Diagnostics.Monsters(f.Runtime.runHistory.LatestOutcome)), Is.EqualTo(B.Structured(CombatC0Diagnostics.Monsters(expected))));
            Assert.That(CombatC0Diagnostics.ChargedWork(f.Runtime.runHistory.LatestOutcome), Is.EqualTo(CombatC0Diagnostics.ChargedWork(expected)));
            var state = JsonUtility.ToJson(f.Runtime); var bytes = f.Session.GetCurrentBytes(); f.Reopen(); Through(f, finish);
            CollectionAssert.AreEqual(bytes, f.Session.GetCurrentBytes()); Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(state));
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(r => r.RunId == "run-1"), Is.EqualTo(1));
        }

        [Test] public void FirstOpportunityAndAllOutcomesAreIndependentOfForegroundPartitionAndReopen()
        {
            var direct = Dungeon(); var split = Dungeon();
            Through(direct, 120000);
            foreach (long t in new long[] { 0, 1, 9999, 10000, 10001, 29999, 30000, 30001, 45000, 60000, 89999, 90000, 120000 }) {
                Through(split, t); split.Reopen();
            }
            // Revision counts authoritative writes/acceptances, which intentionally
            // differ with pump partitioning. Every other durable byte must match.
            string Durable(Fixture f) => System.Text.Encoding.UTF8.GetString(f.Session.GetCurrentBytes()).Replace(
                "\"Revision\":" + f.Runtime.livingDungeon.Revision + ",", "\"Revision\":0,");
            Assert.That(Durable(direct), Is.EqualTo(Durable(split)));
            direct.Reopen(); split.Reopen();
            direct.Runtime.livingDungeon.Revision = split.Runtime.livingDungeon.Revision = 0;
            Assert.That(B.Structured(direct.Runtime), Is.EqualTo(B.Structured(split.Runtime)));
        }

        [TestCase("unchanged")] [TestCase("recovered")] [TestCase("injured")] [TestCase("revival")]
        public void CapturedDefeatAppliesNetDamageToCurrentConditionWithoutHistoricalOverwrite(string action)
        {
            var f = Dungeon(); CombatC2Tests.SetState(f, 2, false, 0);
            var e = Start(f); var outcome = e.DrainToCompletion(); var monster = outcome.CombatMonsters.Single();
            Assert.That(monster.Health, Is.Zero); int damage = monster.InitialHealth - monster.Health;
            if (action == "recovered") CombatC2Tests.SetState(f, 8, false, 0);
            if (action == "injured") CombatC2Tests.SetState(f, 1, false, 0);
            if (action == "revival") CombatC2Tests.SetState(f, 3, true, 0);
            var before = f.Session.GetCurrentBytes(); var current = f.Runtime.monsterContinuity.Records.Single().Copy();
            var applied = LivingDungeonForegroundReconciliation.Apply(e, outcome, f.Runtime.monsterContinuity, f.State, MonsterRecoveryRules.Capture(f.Configuration));
            var r = applied.Records.Single(); Assert.That(r.Health, Is.EqualTo(Math.Max(0, current.Health - damage)));
            Assert.That(r.RevivalRequired, Is.EqualTo(r.Health == 0 || current.RevivalRequired));
            if (action == "recovered") { Assert.That(r.Health, Is.EqualTo(6)); Assert.That(r.RevivalRequired, Is.False); }
            CollectionAssert.AreEqual(before, f.Session.GetCurrentBytes()); Assert.That(f.Runtime.monsterContinuity.Records.Single().Health, Is.EqualTo(current.Health));
        }

        [TestCase(false)] [TestCase(true)]
        public void UnassignedAndRedeployedIdentitiesKeepDamageWithoutDuplicatingOwnership(bool redeploy)
        {
            var f = Dungeon(); var e = Start(f); var outcome = e.DrainToCompletion(); var m = outcome.CombatMonsters.Single();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(m.AssignmentId)));
            if (redeploy) f.Accept(f.Execute(DetachedCanonicalMutationRequest.Redeploy(m.AssignmentId,
                f.State.Floors[0].Layout.Rooms[0].RoomInstanceId, null, new TileCoordinate(3, 2))));
            var applied = LivingDungeonForegroundReconciliation.Apply(e, outcome, f.Runtime.monsterContinuity, f.State, MonsterRecoveryRules.Capture(f.Configuration));
            Assert.That(applied.Records.Single().AssignmentId, Is.EqualTo(m.AssignmentId));
            Assert.That(applied.Records.Single().Health, Is.EqualTo(Math.Max(0, f.Runtime.monsterContinuity.Records.Single().Health - m.InitialHealth + m.Health)));
        }

        [TestCase(false)] [TestCase(true)]
        public void ActualTerminalReconcilesCustodyOrRedeploymentAndRecoversOnlyAssignedVacantIdentity(bool redeploy)
        {
            var f = Dungeon(); Admitted(f); long finish = Finish(f);
            string id = f.Runtime.monsterContinuity.Records.Single().AssignmentId;
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(id)));
            if (redeploy) f.Accept(f.Execute(DetachedCanonicalMutationRequest.Redeploy(id,
                f.State.Floors[0].Layout.Rooms[0].RoomInstanceId, null, new TileCoordinate(3, 2))));
            double wallet = f.Runtime.structureRuntime.ManaReserve; f.Reopen(); Through(f, finish);
            var historical = f.Runtime.runHistory.LatestOutcome.CombatMonsters.Single();
            Assert.That(historical.Health, Is.Zero);
            var actual = f.Runtime.monsterContinuity.Records.Single();
            int readiness = MonsterRecoveryRules.Capture(f.Configuration).Readiness(historical.OptionId);
            Assert.That(actual.AssignmentId, Is.EqualTo(id)); Assert.That(actual.Health, Is.EqualTo(redeploy ? readiness : 0));
            Assert.That(actual.RevivalRequired, Is.EqualTo(!redeploy));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(wallet - (redeploy ? readiness : 0)));
            var bytes = f.Session.GetCurrentBytes(); f.Reopen(); Through(f, finish);
            CollectionAssert.AreEqual(bytes, f.Session.GetCurrentBytes());
        }

        [TestCase(false)] [TestCase(true)]
        public void MoveToAnotherFloorPreservesIdentityAndOrdersCurrentVacantRecoveryBeforeHistoricalDamage(bool activateTarget)
        {
            var f = PhaseSixA4Tests.Eligible(); f.Configuration = CombatC0IntegrationTests.Config();
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);
            Support(f); Admitted(f); long finish = Finish(f);
            string id = f.Runtime.monsterContinuity.Records.Single().AssignmentId;
            string second = PhaseSixA4Tests.FloorTwo(f); var room = f.State.Floors.Single(floor => floor.FloorInstanceId == second).Layout.Rooms[0];
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(id, f.State.Floors[0].FloorInstanceId)));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Redeploy(id, room.RoomInstanceId, second, new TileCoordinate(1, 1))));
            if (activateTarget) f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, second));
            CombatC2Tests.SetState(f, 3, false, 100);
            Through(f, f.Runtime.livingDungeon.Scheduler.ClosedThrough + 1);
            int ready = MonsterRecoveryRules.Capture(f.Configuration).Readiness(MvpDungeonPlacementIds.GoblinOptionId);
            Assert.That(f.Runtime.monsterContinuity.Records.Single().Health, Is.EqualTo(activateTarget ? ready : 3));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(activateTarget ? 100 - ready + 3 : 100));
            f.Reopen(); Through(f, finish);
            Assert.That(f.Runtime.runHistory.LatestOutcome.CombatMonsters.Single().Health, Is.Zero);
            Assert.That(f.State.Floors.Single(floor => floor.FloorInstanceId == second).RoomContents.Assignments.Single().AssignmentId, Is.EqualTo(id));
            Assert.That(f.Runtime.monsterContinuity.Records.Single().Health, Is.EqualTo(activateTarget ? ready : 0));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(activateTarget ? 100 - (ready - 3) - ready : 100));
            var bytes = f.Session.GetCurrentBytes(); f.Reopen(); Through(f, finish);
            CollectionAssert.AreEqual(bytes, f.Session.GetCurrentBytes());
            if (activateTarget) Assert.That(Service(f, new Utc()).InspectForeground(f.Runtime, 10).StateKey, Is.EqualTo("ui.living.unsupported_layout"));
        }

        [Test] public void RetiredCapturedIdentityIsNeverRecreatedAndReplacementIsUntouched()
        {
            var f = Dungeon(); var e = Start(f); var outcome = e.DrainToCompletion(); var m = outcome.CombatMonsters.Single();
            // Controlled valid post-retirement ownership; no new player retirement action.
            var spatial = JsonUtility.FromJson<DetachedCanonicalSpatialSaveState>(JsonUtility.ToJson(f.State));
            spatial.Floors[0].RoomContents.Assignments = spatial.Floors[0].RoomContents.Assignments.Where(a => a.AssignmentId != m.AssignmentId).ToArray();
            var retired = MonsterContinuity.Reconcile(f.Runtime.monsterContinuity, f.State, spatial, MonsterRecoveryRules.Capture(f.Configuration));
            Assert.That(retired.Records, Is.Empty);
            Assert.That(LivingDungeonForegroundReconciliation.Apply(e, outcome, retired, spatial, MonsterRecoveryRules.Capture(f.Configuration)).Records, Is.Empty);
            var prepared = f.Session.PrepareLiveReplacement(DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile), spatial, monsterContinuity: retired);
            Assert.That(prepared.IsSuccess, Is.True, prepared.Reason);
            f.FileSystem.Seed(f.ActivePath, prepared.Update.GetBytes()); f.Reopen();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId,
                f.State.Floors[0].Layout.Rooms[0].RoomInstanceId, null, new TileCoordinate(3, 2))));
            var replacement = f.Runtime.monsterContinuity.Records.Single(); Assert.That(replacement.AssignmentId, Is.Not.EqualTo(m.AssignmentId));
            var applied = LivingDungeonForegroundReconciliation.Apply(e, outcome, f.Runtime.monsterContinuity, f.State, MonsterRecoveryRules.Capture(f.Configuration));
            Assert.That(applied.Records.Single().AssignmentId, Is.EqualTo(replacement.AssignmentId));
            Assert.That(applied.Records.Single().Health, Is.EqualTo(replacement.Health));
        }

        [Test] public void ActualActiveRunAllowsSameFloorDraftCommitAndUsesSnapshotUntilSettlement()
        {
            var f = Dungeon(); Through(f, 0); long arrival = f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value; Through(f, arrival);
            var bytes = f.Runtime.livingDungeon.Parties.Single().Checkpoint;
            var id = f.Runtime.monsterContinuity.Records.Single().AssignmentId;
            var draft = TransactionalDungeonDraft.Create(f.State, PhaseSevenA4TransactionalEditorTests.Context(f), new PhaseSevenA4TransactionalEditorTests.Store());
            Assert.That(draft.Move(f.State.Floors[0].FloorInstanceId, f.State.Floors[0].Layout.Rooms[0].RoomInstanceId, id, new TileCoordinate(3, 2)), Is.True);
            while (draft.Durability == DraftDurability.Pending) Assert.That(draft.FlushNext(), Is.True);
            f.Accept(PhaseSixA4Tests.Writer(f).CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, draft));
            CollectionAssert.AreEqual(bytes, f.Runtime.livingDungeon.Parties.Single().Checkpoint);
            var restored = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties.Single(), Policy.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized));
            long finish; try { restored.DrainToCompletion(); finish = arrival + restored.Inspect().LogicalTime; } finally { restored.Abandon(); }
            Through(f, finish);
            Assert.That(f.State.Floors[0].RoomContents.Assignments.Single(a => a.AssignmentId == id).RoomLocalPosition, Is.EqualTo(new TileCoordinate(3, 2)));
            Assert.That(f.Runtime.runHistory.LatestOutcome.RunId, Is.EqualTo("run-1"));
        }

        [Test] public void ManualC2CanFinishBeforeAnOlderAutomaticReservationWithoutDiscardingEitherResult()
        {
            var f = Dungeon(); Admitted(f); long finish = Finish(f);
            var pinned = f.Runtime.livingDungeon.Parties.Single().Checkpoint;
            double wallet = f.Runtime.structureRuntime.ManaReserve;
            f.Accept(PhaseSixA4Tests.Writer(f).CommitPhaseFiveBRun(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot()), RunPostureResolver.BalancedId, 1000));
            Assert.That(f.Runtime.runHistory.LatestOutcome.RunId, Is.EqualTo("run-2"));
            CollectionAssert.AreEqual(pinned, f.Runtime.livingDungeon.Parties.Single().Checkpoint);
            Assert.That(f.Runtime.monsterContinuity.Records.Single().Health, Is.Zero, "Occupied floor cannot recover between the two results");
            Through(f, finish);
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Select(o => o.RunId), Is.EqualTo(new[] { "run-2", "run-1" }));
            Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(3));
            int ready = MonsterRecoveryRules.Capture(f.Configuration).Readiness(MvpDungeonPlacementIds.GoblinOptionId);
            Assert.That(f.Runtime.monsterContinuity.Records.Single().Health, Is.EqualTo(ready));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(wallet - ready));
            var bytes = f.Session.GetCurrentBytes(); f.Reopen(); Through(f, finish);
            CollectionAssert.AreEqual(bytes, f.Session.GetCurrentBytes());
        }

        [TestCase(false)] [TestCase(true)]
        public void ContradictoryTerminalReceiptsOrMonsterSetRejectWithoutCanonicalPublication(bool receipts)
        {
            var f = Dungeon(); var e = Start(f); var outcome = e.DrainToCompletion(); var bytes = f.Session.GetCurrentBytes();
            if (receipts) outcome.CombatEvents = outcome.CombatEvents.Reverse().ToArray();
            else outcome.CombatMonsters = Array.Empty<CombatC0MonsterState>();
            Assert.That(Assert.Throws<InvalidOperationException>(() => LivingDungeonForegroundReconciliation.Apply(e, outcome,
                f.Runtime.monsterContinuity, f.State, MonsterRecoveryRules.Capture(f.Configuration))).Message,
                Is.EqualTo(LivingDungeonForegroundReconciliation.Conflict));
            CollectionAssert.AreEqual(bytes, f.FileSystem.ReadAllBytes(f.ActivePath)); e.Abandon();
        }

        private sealed class Utc : ITimeSource { public long UtcNowUnixSeconds() => 1000; }
        [Test] public void DevelopmentReopenClockIsOptInAndPreservesRestoredHorizon()
        {
            string product = "Dungeon Lord B3B2 Smoke 20261018";
            string save = "living-dungeon-b3b2-qualification-20261018-native-smoke-fix.json";
            Func<double> Source(string phase, string identity = null, string filename = null) =>
                LivingDungeonForegroundDevelopmentSmoke.ReopenClock(new[] { "-b3b2ForegroundSmoke", phase },
                    identity ?? product, filename ?? save);
            Assert.That(Source("admit"), Is.Null, "Admission must retain its real monotonic source qualification");
            Assert.That(Source("settle", "Dungeon Lord"), Is.Null);
            Assert.That(Source("settle", filename: "save_primary.json"), Is.Null);
            Assert.That(LivingDungeonForegroundDevelopmentSmoke.ReopenClock(Array.Empty<string>(), product, save), Is.Null);
            foreach (string phase in new[] { "settle", "finish" }) {
                var clock = Source(phase); Assert.That(clock, Is.Not.Null);
                var time = new TimeService(new SimpleLogger(false), 10, 300, new Utc(), clock);
                time.AttachSave(new SaveData()); time.BeginForeground(1000, 91629);
                for (int frame = 0; frame < 120; frame++) {
                    time.SetForegroundFocus(frame % 2 == 0);
                    Assert.That(time.SampleForeground(), Is.EqualTo(91629));
                }
            }
        }

        [Test] public void ForegroundClockIsIndependentOfFramePartitionAndRejectsOverflowWithoutElapsedCredit()
        {
            long Sample(bool partitioned) {
                double now = 100;
                var time = new TimeService(new SimpleLogger(false), 10, 300, new Utc(), () => now);
                time.AttachSave(new SaveData()); time.BeginForeground(1000, 0);
                if (partitioned) for (int i = 1; i <= 10000; i++) { now = 100 + i / 10000d; time.SampleForeground(); }
                else now = 101;
                return time.SampleForeground();
            }
            Assert.That(Sample(true), Is.EqualTo(Sample(false))); Assert.That(Sample(true), Is.EqualTo(1000));
            double sample = 100;
            var overflow = new TimeService(new SimpleLogger(false), 10, 300, new Utc(), () => sample);
            overflow.AttachSave(new SaveData()); overflow.BeginForeground(1000, long.MaxValue - 5);
            sample = 101; Assert.Throws<OverflowException>(() => overflow.SampleForeground());
            Assert.That(overflow.ForegroundThrough, Is.EqualTo(long.MaxValue - 5));
            sample = double.NaN; Assert.Throws<InvalidOperationException>(() => overflow.SampleForeground());
            sample = 99; Assert.Throws<InvalidOperationException>(() => overflow.SampleForeground());
        }
        [Test] public void SharedMonotonicClockPreservesWholeUnitsAcrossPartitionFocusPauseAndRestart()
        {
            double now = 100;
            var time = new TimeService(new SimpleLogger(false), 10, 300, new Utc(), () => now);
            time.AttachSave(new SaveData()); time.BeginForeground(1000, 123);
            now = 101; Assert.That(time.SampleForeground(), Is.EqualTo(1123));
            time.SetForegroundFocus(false); now = 1000; time.SetForegroundFocus(true);
            Assert.That(time.SampleForeground(), Is.EqualTo(1123));
            now = 1001; time.OnPause(); now = 10000; time.SetForegroundFocus(false); time.OnResume(); time.SetForegroundFocus(true);
            Assert.That(time.SampleForeground(), Is.EqualTo(2123));
            now = 10001; Assert.That(time.SampleForeground(), Is.EqualTo(3123));
            var reopened = new TimeService(new SimpleLogger(false), 10, 300, new Utc(), () => now);
            reopened.AttachSave(new SaveData()); reopened.BeginForeground(1000, 3123);
            now += .001; Assert.That(reopened.SampleForeground(), Is.EqualTo(3124));
        }
    }
}
#endif
