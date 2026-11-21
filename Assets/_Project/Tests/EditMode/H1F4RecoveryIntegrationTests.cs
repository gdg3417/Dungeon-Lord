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
            internal int Fraction;
            public UnixTimePoint UtcNow() => new UnixTimePoint(Seconds, Fraction);
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
            internal Harness(bool paidResearch = false, bool native = false, bool starterRoute = false)
            {
                var profile = SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
                F = Fixture.Create("\"researchPending\":null,\"researchProgress\":null", workloadProfile: profile);
                if (starterRoute) {
                    F.Accept(F.Execute(DetachedCanonicalMutationRequest.Place(
                        MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)));
                    PhaseSixA4Tests.AddContent(F, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
                    PhaseSixA4Tests.AddContent(F, 0, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);
                }
                if (native) F.FileSystem.DeleteFile(F.ActivePath);
                Inputs = paidResearch ? PaidBundle(F) : Bundle(F);
                Time = new TimeService(new SimpleLogger(false), Inputs.Bootstrap.tickSeconds, Inputs.Bootstrap.timeRules.detectClockSkewSeconds,
                    Wall, () => Wall.Seconds - 100d + Wall.Fraction / 10000000d);
                Time.BeginRecoveryForeground(Inputs.TwoFloor.UnitsPerSecond);
                Service = CreateService(F, Inputs, Time);
                Current = Service.LoadOrCreate("h1f4-owned-test", out var banner);
                Assert.That(Current, Is.Not.Null, banner);
                Time.BindLoadedForegroundHorizon(Current.livingDungeon.Scheduler?.AcceptedThrough ?? -1);
                Time.AttachSave(Current);
                Service.CanonicalRuntimePublished += value => { Current = value; Time.AttachSave(value); };
                Mana = CreateRecoveryMana(F, Inputs);
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
        static CanonicalOfflinePassiveManaService CreateRecoveryMana(Fixture fixture, RecoveryRuntimeInputs inputs)
        {
            var online = new CanonicalPassiveManaService(inputs.Passive, inputs.Economy, new FormulaEngine(),
                fixture.Profile.Canonical.Spatial, inputs.Bootstrap.tickSeconds);
            return new CanonicalOfflinePassiveManaService(inputs.Passive, online);
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

        private const string ResearchRulePath = "Assets/_Project/Resources/ResearchTiming/adventurer_analysis_v1.json";
        private const string StarterSelectionPath = "Assets/_Project/Resources/research_starter_selection.json";
        private const string ReleaseKey = "research.timing.adventurer_analysis.v1";

        [Test]
        public void RecoveryBootstrapAllowsMissingStarterSelection()
        {
            const string scenePath = "Assets/_Project/Scenes/Bootstrap.unity";
            const string resourceName = "research_starter_selection";
            var profile = SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
            var fixture = Fixture.Create(null, workloadProfile: profile);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(scenePath);
            string relocated = UnityEditor.AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/ResearchSelectionBootstrapTest.json");
            try
            {
                var root = scene.GetRootGameObjects().Select(go => go.GetComponent<GameRoot>()).Single(go => go != null);
                typeof(GameRoot).GetProperty(nameof(GameRoot.Content)).SetValue(root, new ContentService());
                typeof(GameRoot).GetProperty(nameof(GameRoot.SaveSpatialMigrationLimits)).SetValue(root, profile);
                root.Content.LoadProductionSpatialContent(root.productionSpatialManifest, root.productionSpatialCatalog,
                    root.productionSpatialLanguageTables, root.productionSpatialValidationLimits);
                var service = new SaveService(new SimpleLogger(false),
                    new SaveConfig { fileName = "research-bootstrap-" + Guid.NewGuid().ToString("N") + ".json" }, Path.GetTempPath());
                service.ConfigureCanonical(profile, fixture.Production, fixture.Compatibility, fixture.Configuration,
                    LegacyGameplayConfigurationContract.SerializeCanonical(fixture.Configuration), fixture.PositionProfiles, fixture.Occupancy);
                root.AttachSaveServiceForTests(service);
                var limits = ProductionSpatialContentWorkloadLimitParser.Parse(root.productionSpatialValidationLimits).Limits;
                var selection = UnityEngine.Resources.Load<UnityEngine.TextAsset>(resourceName);
                Assert.That(selection, Is.Not.Null);
                UnityEngine.Resources.UnloadAsset(selection);
                Assert.That(UnityEditor.AssetDatabase.MoveAsset(StarterSelectionPath, relocated), Is.Empty);
                Assert.That(UnityEngine.Resources.Load<UnityEngine.TextAsset>(resourceName), Is.Null);

                var configure = typeof(GameRoot).GetMethod("TryConfigureRecoveryRuntime", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(configure.Invoke(root, new object[] { limits }), Is.True);
                Assert.That(root.TimeService, Is.Not.Null);
                Assert.That(service.ResearchTiming.Select("research.project.m7_a2_scaffold", out _), Is.EqualTo(ResearchTimingRules.Disabled));
                string reference = ReleaseKey + "." + SpatialContractSha256.Compute(File.ReadAllBytes(ResearchRulePath));
                Assert.That(service.ResearchTiming.Resolve(reference, out _), Is.Null);
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrEmpty(UnityEditor.AssetDatabase.AssetPathToGUID(relocated,
                        UnityEditor.AssetPathToGUIDOptions.OnlyExistingAssets)))
                        Assert.That(UnityEditor.AssetDatabase.MoveAsset(relocated, StarterSelectionPath), Is.Empty);
                }
                finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
            }
        }

        [Test]
        public void StarterResourceSelectionBindsOnlyTheExactArchivedProjectAndSlot()
        {
            var fixture = Fixture.Create(null);
            var limits = fixture.Profile.Canonical.Serialized;
            const string project = "research.project.m7_a2_scaffold", slot = "research.slot.primary";
            byte[] archive = File.ReadAllBytes(ResearchRulePath);
            byte[] selection = File.ReadAllBytes(StarterSelectionPath);
            string text = Encoding.UTF8.GetString(selection);
            var variations = new[] {
                (text, project, slot, (string)null),
                (null, project, slot, ResearchTimingRules.Disabled),
                (text.Replace("true", "false"), project, slot, ResearchTimingRules.Disabled),
                (text.Replace("true", "\"true\""), project, slot, ResearchTimingRules.Invalid),
                (text.Replace("true", "true,\"enabled\":true"), project, slot, ResearchTimingRules.Invalid),
                (text.Replace(":1,", ":2,"), project, slot, ResearchTimingRules.Invalid),
                (text.Replace(".v1.", ".v9."), project, slot, ResearchTimingRules.Missing),
                (text.Replace(SpatialContractSha256.Compute(archive), new string('0', 64)), project, slot, ResearchTimingRules.Mismatch),
                (text, "research.project.other", slot, ResearchTimingRules.Mismatch),
                (text, project, "research.slot.other", ResearchTimingRules.Mismatch)
            };
            foreach (var (content, projectBinding, slotBinding, expected) in variations)
            {
                var rules = new ResearchTimingRules(new[] { archive }, Array.Empty<string>(), 1000, limits,
                    content == null ? null : Encoding.UTF8.GetBytes(content), projectBinding, slotBinding);
                Assert.That(rules.Select(projectBinding, out var selected), Is.EqualTo(expected));
                if (selected != null)
                {
                    Assert.That(selected.ManaCost, Is.EqualTo(10));
                    Assert.That(selected.RequiredUnits / rules.UnitsPerSecond, Is.EqualTo(120));
                }
                Assert.That(rules.Resolve(ReleaseKey + "." + SpatialContractSha256.Compute(archive), out _), Is.Null);
            }
        }

        [Test]
        public void PaidStartRejectsPlausibleRunEvidenceAbsentFromTheDurablePredecessor()
        {
            var h = new Harness(paidResearch: true);
            h.Start(); h.Accept(100); h.Accept(100); h.Accept(100);
            h.Current.structureRuntime.ManaReserve = 1000;
            Assert.That(h.Service.Save(h.Current, SaveReason.StateChange), Is.True);
            using var scope = new RootScope(h);
            scope.Pulse(100);
            var current = scope.Root.Save;
            current.runHistory.NextRunSequence = 2;
            current.runHistory.AppendOutcome(new RunOutcomeRecord { RunId = "run-1", Success = false }, 5);
            var before = h.F.FileSystem.ReadAllBytes(h.Service.SavePath);
            var result = scope.Root.StartConfiguredPlayerResearch();
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FeedbackLocalizationKey, Is.EqualTo(PlayerResearchActionHandler.BlockedPrerequisiteKey));
            CollectionAssert.AreEqual(before, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
            Assert.That(scope.Root.Save.researchProgress, Is.Null);
            Assert.That(scope.Root.Save.structureRuntime.ManaReserve, Is.EqualTo(1000));
        }

        private static RecoveryRuntimeInputs PaidBundle(Fixture fixture, bool updatedDefault = false, bool enabled = true)
        {
            byte[] original = File.ReadAllBytes(ResearchRulePath);
            var later = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(original)
                .Replace(".v1", ".v2").Replace(":120", ":240"));
            string selected = updatedDefault ? ReleaseKey.Replace(".v1", ".v2") + "." + SpatialContractSha256.Compute(later)
                : ReleaseKey + "." + SpatialContractSha256.Compute(original);
            return WithResearchArchive(fixture, new[] { original, later }, enabled ? new[] { selected } : Array.Empty<string>());
        }

        private static RecoveryRuntimeInputs WithResearchArchive(Fixture fixture, byte[][] archive, string[] selection) =>
            new RecoveryRuntimeInputs(Inputs(), fixture.Production,
                ProductionSpatialContentWorkloadLimitParser.Parse(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(
                    "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json")).Limits,
                fixture.Profile.Canonical, archive, selection);

        private static DetachedCurrentTargetValidationContext ResearchContext(Fixture fixture, RecoveryRuntimeInputs inputs)
        {
            var limits = fixture.Profile.Canonical;
            var baseBudget = limits.ForSchema(13).Serialized;
            var context = new DetachedCurrentTargetValidationContext(inputs.Compatibility, fixture.Production,
                LegacyGameplayConfigurationContract.SerializeCanonical(inputs.Configuration), limits, inputs.Occupancy,
                livingDungeonProfiles: new[] { inputs.OneFloor.CreateProfile(baseBudget), inputs.TwoFloor.CreateProfile(baseBudget) },
                monsterRecovery: inputs.Monsters);
            context.BindRecoveryInputs(inputs);
            return context;
        }

        [TestCase(0L, 10000L)]
        [TestCase(7000L, 3000L)]
        [TestCase(10000L, 10000L)]
        [TestCase(7125L, 2875L)]
        public void PaidRuleUsesExactClosedTickUnits(long start, long firstCredit)
        {
            byte[] bytes = File.ReadAllBytes(ResearchRulePath);
            string reference = ReleaseKey + "." + SpatialContractSha256.Compute(bytes);
            var rules = new ResearchTimingRules(new[] { bytes }, new[] { reference }, 1000, Fixture.Create(null).Profile.Canonical.Serialized);
            Assert.That(rules.Resolve(reference, out var rule), Is.Null);
            Assert.That(rules.Resolve(ReleaseKey + "." + new string('0', 64), out _), Is.EqualTo(ResearchTimingRules.Mismatch));
            Assert.That(rules.Resolve(reference.Replace(".v1", ".v2"), out _), Is.EqualTo(ResearchTimingRules.Missing));
            Assert.That(rules.Resolve(reference.Replace(".v1", ".v01"), out _), Is.EqualTo(ResearchTimingRules.Invalid));
            Assert.That(rule.EarnedThrough(start, start == 10000 ? 20000 : 10000, 10000), Is.EqualTo(firstCredit));
            Assert.That(rule.EarnedThrough(start, start + 119999, 10000), Is.LessThan(120000));
            long ready = ((start + 120000 + 9999) / 10000) * 10000;
            Assert.That(rule.EarnedThrough(start, ready, 10000), Is.EqualTo(120000));
            Assert.That(rule.EarnedThrough(start, ready + 10000, 10000), Is.EqualTo(120000));
        }

        private static Harness PreparePaidStart(bool native = false, bool starterRoute = false)
        {
            var h = new Harness(paidResearch: true, native: native, starterRoute: starterRoute);
            h.Start(); h.Accept(100); h.Accept(100); h.Accept(100);
            // An explicit synthetic completed defeat supplies the test prerequisite.
            h.Current.runHistory.NextRunSequence = 2;
            h.Current.runHistory.AppendOutcome(new RunOutcomeRecord { RunId = "run-1", Success = false }, 5);
            h.Current.structureRuntime.ManaReserve = 1000;
            Assert.That(h.Service.Save(h.Current, SaveReason.StateChange), Is.True);
            return h;
        }

        [Test]
        public void PaidStartClosesPartialTickAndReopensUnderOriginalRule()
        {
            var h = PreparePaidStart(native: true);
            Assert.That(h.Service.LastLoadCreatedNative, Is.True);
            byte[] productionProfiles = File.ReadAllBytes(SpatialLayoutCompatibilityProfiles.ProductionPath);
            CollectionAssert.AreEqual(productionProfiles, h.Inputs.Identity.GetInput(9));
            CollectionAssert.AreEqual(productionProfiles, h.Inputs.Compatibility.CanonicalBytes);
            var invalidRegistration = h.Inputs.Compatibility.Value;
            invalidRegistration.ContractSelections.Single(item =>
                item.TargetSchemaVersion == CanonicalSaveSchemaVersions.AtomicRecoveryIntroduction)
                .TargetSchemaVersion = CanonicalSaveSchemaVersions.ResearchTimingIntroduction;
            Assert.That(CompatibilityReleasePolicy.IsAuthorized(invalidRegistration), Is.False);
            using var scope = new RootScope(h);
            scope.Pulse(107);
            double before = scope.Root.Save.structureRuntime.ManaReserve;
            var start = scope.Root.StartConfiguredPaidResearch();
            Assert.That(start.Succeeded, Is.True, start.FeedbackLocalizationKey);
            Assert.That(scope.Root.Save.structureRuntime.ManaReserve, Is.EqualTo(before - 10));
            string reference = scope.Root.Save.researchProgress.RuleSourceIdUsed;
            Assert.That(scope.Root.Save.researchProgress.StartClosedThrough, Is.EqualTo(7000));
            var startedBytes = h.F.FileSystem.ReadAllBytes(h.Service.SavePath);
            Assert.That(scope.Root.StartConfiguredPaidResearch().Succeeded, Is.False);
            CollectionAssert.AreEqual(startedBytes, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));

            long DisplaySeconds() => PlayerResearchStatusTextPresenter.DisplayElapsedSeconds(
                scope.Root.Save.researchProgress, 1000, 120, scope.Root.ReadResearchPresentationHorizon());
            Assert.That(DisplaySeconds(), Is.Zero);
            h.Wall.Seconds = 108;
            Assert.That(DisplaySeconds(), Is.EqualTo(1));
            h.Wall.Seconds = 109;
            Assert.That(DisplaySeconds(), Is.EqualTo(2));
            h.Wall.Seconds = 106;
            Assert.That(scope.Root.ReadResearchPresentationHorizon(), Is.Null);
            Assert.That(DisplaySeconds(), Is.Zero);
            CollectionAssert.AreEqual(startedBytes, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));

            scope.Pulse(110);
            Assert.That(scope.Root.Save.researchProgress.ProgressUnits, Is.EqualTo(3000));

            var changed = PaidBundle(h.F, updatedDefault: true);
            Assert.That(changed.Identity.Digest, Is.EqualTo(h.Inputs.Identity.Digest));
            var reopened = CreateService(h.F, changed, h.Time);
            var current = reopened.LoadOrCreate("paid-reopen", out string banner);
            Assert.That(current, Is.Not.Null, banner);
            Assert.That(current.researchProgress.RuleSourceIdUsed, Is.EqualTo(reference));
            Assert.That(current.researchProgress.ProgressUnits, Is.EqualTo(3000));
            scope.Rebind(reopened, h.Time, current);
            scope.Pulse(220);
            Assert.That(scope.Root.Save.researchProgress.ProgressUnits, Is.EqualTo(113000));
            Assert.That(scope.Root.Save.researchProgress.CompletionPending, Is.False);
            scope.Pulse(227);
            Assert.That(DisplaySeconds(), Is.EqualTo(120));
            Assert.That(PlayerResearchStatusTextPresenter.PresentCountdown(120, 120, key => key, null),
                Is.EqualTo(PlayerResearchStatusTextPresenter.AwaitingTickKey));
            Assert.That(scope.Root.ResolveProductionResearchState().CanClaimLocalMvp, Is.False);
            Assert.That(scope.Root.Save.researchProgress.CompletionPending, Is.False);
            scope.Pulse(230);
            Assert.That(scope.Root.Save.researchProgress.ProgressUnits, Is.EqualTo(120000));
            Assert.That(scope.Root.ClaimConfiguredPlayerResearch().Succeeded, Is.True);
            Assert.That(scope.Root.ClaimConfiguredPlayerResearch().Succeeded, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PaidStartPublicationRequiresExactDebitAndPendingProof(bool uncertain)
        {
            var h = PreparePaidStart();
            using var scope = new RootScope(h);
            scope.Pulse(107);
            var fs = new ObservedFileSystem(h.F.FileSystem);
            var service = CreateService(h.F, h.Inputs, h.Time, fs);
            var current = service.LoadOrCreate("paid-start-fault", out var reason);
            Assert.That(current, Is.Not.Null, reason);
            scope.Rebind(service, h.Time, current);
            h.Time.AttachSave(current);
            var before = service.CanonicalSession.GetCurrentBytes();
            double wallet = current.structureRuntime.ManaReserve;
            if (uncertain) fs.InterruptReplacement = true;
            else h.F.FileSystem.EnableTargetedFailure(Faults.OperationType.Read,
                paths => paths[0] == service.SavePath, 3, false);

            var result = scope.Root.StartConfiguredPaidResearch();
            Assert.That(result.Succeeded || result.StateChanged || result.WouldChargeCosts, Is.False);
            Assert.That(result.PersistenceReason, Is.EqualTo(uncertain
                ? DetachedCanonicalWriteAuthority.RecoveryRequiredReason
                : DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            Assert.That(scope.Root.Save.researchPending, Is.Null);
            Assert.That(scope.Root.Save.structureRuntime.ManaReserve, Is.EqualTo(wallet));
            CollectionAssert.AreEqual(before, service.CanonicalSession.GetCurrentBytes());
            fs.InterruptReplacement = false;
            h.F.FileSystem.DisableFailure();

            if (!uncertain) {
                CollectionAssert.AreEqual(before, fs.ReadAllBytes(service.SavePath));
                Assert.That(scope.Root.StartConfiguredPaidResearch().Succeeded, Is.True);
                Assert.That(scope.Root.Save.structureRuntime.ManaReserve, Is.EqualTo(wallet - 10));
                return;
            }

            var surviving = fs.ReadAllBytes(service.SavePath);
            Assert.That(surviving, Is.Not.EqualTo(before));
            int operations = h.F.FileSystem.Operations.Count();
            Assert.That(scope.Root.StartConfiguredPaidResearch().RecoveryRequired, Is.True);
            Assert.That(h.F.FileSystem.Operations.Count(), Is.EqualTo(operations));
            var proof = service.ResolveUncertainRecoveryPublication();
            Assert.That(proof.IsSuccess, Is.True, proof.Reason);
            CollectionAssert.AreEqual(surviving, proof.GetPersistedBytes());
            Assert.That(proof.RuntimeProjection.researchPending.ProjectId, Is.Not.Empty);
            Assert.That(proof.RuntimeProjection.researchProgress.StartClosedThrough, Is.EqualTo(7000));
            Assert.That(proof.RuntimeProjection.structureRuntime.ManaReserve, Is.EqualTo(wallet - 10));
            Assert.That(service.ResolveUncertainRecoveryPublication().IsSuccess, Is.False);
            CollectionAssert.AreEqual(surviving, fs.ReadAllBytes(service.SavePath));
        }

        [Test]
        public void PaidStartRequiresBarrierEnabledSelectionAndProtectedReserve()
        {
            var h = PreparePaidStart(starterRoute: true);
            using var scope = new RootScope(h);
            scope.Pulse(107);
            long reserve = h.Service.ResearchProtectedReserve(scope.Root.Save);
            Assert.That(reserve, Is.Positive);
            scope.Root.Save.structureRuntime.ManaReserve = reserve + 9;
            Assert.That(h.Service.Save(scope.Root.Save, SaveReason.StateChange), Is.True);
            var before = h.Service.CanonicalSession.GetCurrentBytes();
            var result = scope.Root.StartConfiguredPaidResearch();
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FeedbackLocalizationKey, Is.EqualTo("ui.player_research.insufficient_usable_mana"));
            Assert.That(scope.Root.Save.researchPending, Is.Null);
            CollectionAssert.AreEqual(before, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));

            var disabled = CreateService(h.F, PaidBundle(h.F, enabled: false), h.Time);
            var current = disabled.LoadOrCreate("disabled-selection", out var banner);
            Assert.That(current, Is.Not.Null, banner);
            scope.Rebind(disabled, h.Time, current);
            h.Time.AttachSave(current);
            var barrier = disabled.BeforeForegroundAction;
            disabled.BeforeForegroundAction = null;
            Assert.That(scope.Root.StartConfiguredPaidResearch().PersistenceReason, Is.EqualTo("living.foreground.boundary_pending"));
            disabled.BeforeForegroundAction = barrier;
            Assert.That(scope.Root.StartConfiguredPaidResearch().PersistenceReason, Is.EqualTo(ResearchTimingRules.Disabled));
            CollectionAssert.AreEqual(before, h.F.FileSystem.ReadAllBytes(disabled.SavePath));
        }

        [Test]
        public void FailedPaidTickLeavesClosureWalletAndProgressUnpublished()
        {
            var h = PreparePaidStart();
            using var scope = new RootScope(h);
            scope.Pulse(107);
            Assert.That(scope.Root.StartConfiguredPaidResearch().Succeeded, Is.True);
            var before = h.Service.CanonicalSession.GetCurrentBytes();
            var prior = scope.Root.Save;
            h.F.FileSystem.EnableTargetedFailure(Faults.OperationType.Write,
                paths => paths[0].EndsWith(".candidate", StringComparison.Ordinal), 1, false);
            h.Wall.Seconds = 110;
            var failed = h.Service.AdvanceForeground(prior, h.Time.CaptureForegroundObservation(h.Service.CanonicalSession),
                h.Inputs.Bootstrap.tickSeconds, scope.PassiveTick);
            Assert.That(failed.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            Assert.That(h.F.FileSystem.FailedOperation, Is.Not.Null);
            Assert.That(scope.Root.Save, Is.SameAs(prior));
            Assert.That(prior.livingDungeon.Scheduler.ClosedThrough, Is.EqualTo(7000));
            Assert.That(prior.researchProgress.ProgressUnits, Is.Zero);
            CollectionAssert.AreEqual(before, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
            h.F.FileSystem.DisableFailure();
        }

        [Test]
        public void PaidCreditSurvivesOpportunityAndSettlementStagesWithoutChangingExpeditionResults()
        {
            (string History, string Monsters, double Wallet, long Closed) Play(bool paid)
            {
                var h = PreparePaidStart(starterRoute: true);
                using var scope = new RootScope(h);
                scope.Pulse(110);
                if (paid) {
                    Assert.That(scope.Root.StartConfiguredPaidResearch().Succeeded, Is.True);
                    Assert.That(scope.Root.Save.researchProgress.StartClosedThrough, Is.EqualTo(10000));
                    Assert.That(scope.Root.Save.researchProgress.ProgressUnits, Is.Zero);
                } else {
                    // Compare expedition outcomes at the same wallet after debit.
                    scope.Root.Save.structureRuntime.ManaReserve -= 10;
                    Assert.That(h.Service.Save(scope.Root.Save, SaveReason.StateChange), Is.True);
                }

                bool enabled = ForegroundQualificationMetrics.Enabled;
                bool detailed = ForegroundQualificationMetrics.DetailedEnabled;
                ForegroundQualificationMetrics.ResetDetailed();
                try {
                    ForegroundQualificationMetrics.Enabled = ForegroundQualificationMetrics.DetailedEnabled = true;
                    for (int wall = 120; wall <= 230; wall += 10) {
                        scope.Pulse(wall);
                        var save = scope.Root.Save;
                        Assert.That(save.livingDungeon.Scheduler.ClosedThrough, Is.EqualTo((wall - 100) * 1000L));
                        if (paid) {
                            Assert.That(save.researchProgress.ProgressUnits, Is.EqualTo((wall - 110) * 1000L));
                            Assert.That(save.researchProgress.CompletionPending, Is.EqualTo(wall == 230));
                        }
                    }
                    var origins = ForegroundQualificationMetrics.OriginCalls;
                    int stage = (int)ForegroundQualificationMetrics.Boundary.StagePreparation;
                    Assert.That(origins[stage, (int)ForegroundQualificationMetrics.Origin.Opportunity], Is.Positive);
                    Assert.That(origins[stage, (int)ForegroundQualificationMetrics.Origin.Settlement], Is.Positive);
                    var final = scope.Root.Save;
                    var writer = new ContractJsonWriter(h.F.Profile.Canonical.Serialized);
                    MonsterContinuity.Write(writer, final.monsterContinuity);
                    return (UnityEngine.JsonUtility.ToJson(final.runHistory), Encoding.UTF8.GetString(writer.Finish()),
                        final.structureRuntime.ManaReserve, final.livingDungeon.Scheduler.ClosedThrough);
                } finally {
                    ForegroundQualificationMetrics.Enabled = enabled;
                    ForegroundQualificationMetrics.DetailedEnabled = detailed;
                    ForegroundQualificationMetrics.ResetDetailed();
                }
            }

            Assert.That(Play(true), Is.EqualTo(Play(false)));
        }

        [Test]
        public void OriginalAndResidualAbsenceDoNotCreditAnUnfinishedPaidProject()
        {
            var h = PreparePaidStart();
            using var scope = new RootScope(h);
            scope.Pulse(107);
            Assert.That(scope.Root.StartConfiguredPaidResearch().Succeeded, Is.True);
            scope.Pulse(110);
            Assert.That(scope.Root.Save.researchProgress.ProgressUnits, Is.EqualTo(3000));
            string reference = scope.Root.Save.researchProgress.RuleSourceIdUsed;
            h.Wall.Seconds = 410;
            h.Start();
            h.Accept(410);
            Assert.That(h.Current.timeProvenance.Episode.Phase, Is.EqualTo(RecoveryEpisode.OriginalPending));
            h.Accept(414);
            Assert.That(h.Current.timeProvenance.Episode.Phase, Is.EqualTo(RecoveryEpisode.ResidualPending));
            h.Accept(420);
            Assert.That(h.Current.timeProvenance.Episode, Is.Null);
            Assert.That(h.Current.researchProgress.RuleSourceIdUsed, Is.EqualTo(reference));
            Assert.That(h.Current.researchProgress.ProgressUnits, Is.EqualTo(3000));
            Assert.That(h.Current.researchProgress.CompletionPending, Is.False);
            Assert.That(h.Current.livingDungeon.Scheduler.ClosedThrough, Is.EqualTo(10000));
            Assert.That(h.Current.timeProvenance.RecoveryContextSha256, Is.EqualTo(h.Inputs.Identity.Digest));
        }

        [Test]
        public void PaidWireValidationAndWritesRejectContradictionsAndActiveRebinding()
        {
            var h = PreparePaidStart();
            using var scope = new RootScope(h);
            scope.Pulse(107);
            var beforeStart = h.Service.CanonicalSession.GetCurrentBytes();
            string project = h.Inputs.Configuration.MvpFirstSessionObjective.AnalysisResearchProjectId;
            Assert.That(h.Inputs.ResearchTiming.Select(project, out var paidRule), Is.Null);
            scope.Root.Save.researchPending = new ResearchPendingState { SlotId = paidRule.SlotId, ProjectId = project };
            scope.Root.Save.researchProgress = new ResearchProgressState {
                SlotId = paidRule.SlotId, ProjectId = project, RuleSourceIdUsed = paidRule.Reference, StartClosedThrough = 7000
            };
            Assert.That(h.Service.Save(scope.Root.Save, SaveReason.StateChange), Is.False,
                "A direct save cannot start a project without the debit transaction");
            CollectionAssert.AreEqual(beforeStart, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
            scope.Root.Save.researchPending = null;
            scope.Root.Save.researchProgress = null;
            Assert.That(scope.Root.StartConfiguredPaidResearch().Succeeded, Is.True);
            var before = h.Service.CanonicalSession.GetCurrentBytes();
            string json = Encoding.UTF8.GetString(before);
            string reference = scope.Root.Save.researchProgress.RuleSourceIdUsed;
            var context = ResearchContext(h.F, h.Inputs);
            var mutations = new[] {
                (reference, reference.Replace(".v1", ".v9"), ResearchTimingRules.Missing),
                (reference, reference.Replace(".v1", ".v01"), ResearchTimingRules.Invalid),
                (reference, ReleaseKey + "." + new string('0', 64), ResearchTimingRules.Mismatch),
                ("\"StartClosedThrough\":7000", "\"StartClosedThrough\":7001", ResearchTimingRules.StateInvalid),
                ("\"StartClosedThrough\":7000", "\"StartClosedThrough\":7000.0", ResearchTimingRules.StateInvalid),
                (",\"StartClosedThrough\":7000", "", ResearchTimingRules.StateInvalid),
                ("\"ProgressUnits\":0", "\"ProgressUnits\":0.5", ResearchTimingRules.StateInvalid),
                ("\"ProgressUnits\":0", "\"ProgressUnits\":1", ResearchTimingRules.StateInvalid),
                ("\"ProgressUnits\":0", "\"ProgressUnits\":9007199254740992", ResearchTimingRules.StateInvalid),
                ("\"CompletionPending\":false", "\"CompletionPending\":true", ResearchTimingRules.StateInvalid)
            };
            foreach (var (oldValue, newValue, expected) in mutations) {
                string invalid = json.Replace(oldValue, newValue);
                Assert.That(invalid, Is.Not.EqualTo(json));
                Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(Encoding.UTF8.GetBytes(invalid), context).Reason,
                    Is.EqualTo(expected), newValue);
            }
            // A matching accumulator cannot legitimize a negative Start boundary.
            long closed = scope.Root.Save.livingDungeon.Scheduler.ClosedThrough;
            Assert.That(closed, Is.EqualTo(7000));
            Assert.That(paidRule.EarnedThrough(-1, closed, 10000), Is.EqualTo(1));
            var negativeStart = json.Replace("\"StartClosedThrough\":7000", "\"StartClosedThrough\":-1")
                .Replace("\"ProgressUnits\":0", "\"ProgressUnits\":1");
            Assert.That(negativeStart, Does.Contain("\"StartClosedThrough\":-1").And.Contain("\"ProgressUnits\":1"));
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(Encoding.UTF8.GetBytes(negativeStart), context).Reason,
                Is.EqualTo(ResearchTimingRules.StateInvalid));
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(before, ResearchContext(h.F, Bundle(h.F))).Reason,
                Is.EqualTo(ResearchTimingRules.Missing));

            // Each replacement is individually well-formed. Only predecessor
            // comparison can prevent rewriting these immutable paid terms.
            var current = scope.Root.Save;
            var updated = PaidBundle(h.F, updatedDefault: true);
            Assert.That(updated.ResearchTiming.Select(current.researchProgress.ProjectId, out var later), Is.Null);
            current.researchProgress.RuleSourceIdUsed = later.Reference;
            Assert.That(h.Service.Save(current, SaveReason.StateChange), Is.False);
            current.researchProgress.RuleSourceIdUsed = reference;
            current.researchProgress.StartClosedThrough = 6000;
            Assert.That(h.Service.Save(current, SaveReason.StateChange), Is.False);
            current.researchProgress.StartClosedThrough = 7000;
            var pending = current.researchPending;
            var progress = current.researchProgress;
            current.researchPending = null;
            current.researchProgress = null;
            Assert.That(h.Service.Save(current, SaveReason.StateChange), Is.False,
                "A direct save cannot clear the active project and allow another charge");
            current.researchPending = pending;
            current.researchProgress = progress;
            current.completedResearch = new CompletedResearchState { ProjectIds = new[] { current.researchProgress.ProjectId } };
            Assert.That(h.Service.Save(current, SaveReason.StateChange), Is.False);
            CollectionAssert.AreEqual(before, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
        }

        [Test]
        public void CurrentPaidRecordRetainsFullWidthIntegersAndNestedExtensions()
        {
            var h = PreparePaidStart();
            using var scope = new RootScope(h);
            scope.Pulse(107);
            Assert.That(scope.Root.StartConfiguredPaidResearch().Succeeded, Is.True);
            var existing = scope.Root.Save.researchProgress.RuleSourceIdUsed;
            string key = "research.timing." + new string('a', 32) + ".v2147483647";
            long seconds = ResearchTimingRule.MaximumExactUnits / h.Inputs.OneFloor.UnitsPerSecond;
            long earned = seconds * h.Inputs.OneFloor.UnitsPerSecond;
            const long start = 4611686018427387905;
            long closed = (start + earned + 9999) / 10000 * 10000;
            var archive = Encoding.UTF8.GetBytes(File.ReadAllText(ResearchRulePath)
                .Replace(ReleaseKey, key).Replace(":120", ":" + seconds));
            string reference = key + "." + SpatialContractSha256.Compute(archive);
            Assert.That(reference.Length, Is.EqualTo(125));
            string input = Encoding.UTF8.GetString(h.Service.CanonicalSession.GetCurrentBytes())
                .Replace(existing, reference)
                .Replace("\"AcceptedThrough\":7000", "\"AcceptedThrough\":" + closed)
                .Replace("\"ClosedThrough\":7000", "\"ClosedThrough\":" + closed)
                .Replace("\"ProgressUnits\":0", "\"ProgressUnits\":" + earned)
                .Replace("\"CompletionPending\":false", "\"CompletionPending\":true")
                .Replace("\"StartClosedThrough\":7000", "\"extension\":{\"n\":[null,1.00]},\"StartClosedThrough\":" + start);
            var inputs = WithResearchArchive(h.F, new[] { archive }, Array.Empty<string>());
            h.F.FileSystem.Seed(h.Service.SavePath, Encoding.UTF8.GetBytes(input));
            var reopened = CreateService(h.F, inputs, h.Time);
            var current = reopened.LoadOrCreate("paid-full-width", out var banner);
            Assert.That(current, Is.Not.Null, banner);
            Assert.That(current.researchProgress.StartClosedThrough, Is.EqualTo(start));
            Assert.That(current.researchProgress.ProgressUnits, Is.EqualTo((double)earned));
            Assert.That(current.researchProgress.CompletionPending, Is.True);
            Assert.That(reopened.Save(current, SaveReason.StateChange), Is.True);
            var roundTrip = reopened.CanonicalSession.GetCurrentBytes();
            Assert.That(Encoding.UTF8.GetString(roundTrip), Does.Contain(
                "\"extension\":{\"n\":[null,1.00]},\"StartClosedThrough\":" + start));
            Assert.That(Encoding.UTF8.GetString(roundTrip), Does.Contain("\"ProgressUnits\":" + earned));
            Assert.That(RawSavePayloadClassifier.TryValidateWorkload(roundTrip, h.F.Profile.Raw, out var reason), Is.True, reason);
        }

        [TestCase(CanonicalSaveSchemaVersions.AtomicRecoveryIntroduction)]
        [TestCase(CanonicalSaveSchemaVersions.ResearchTimingIntroduction)]
        public void ObsoleteSchemaRefusalPreservesOriginalBytes(int earlierSchema)
        {
            var h = new Harness();
            var obsolete = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(h.Service.CanonicalSession.GetCurrentBytes())
                .Replace("\"schemaVersion\":" + CanonicalSaveSchemaVersions.CurrentWritableTarget + ",",
                    "\"schemaVersion\":" + earlierSchema + ","));
            h.F.FileSystem.Seed(h.Service.SavePath, obsolete);
            var service = CreateService(h.F, h.Inputs, h.Time);
            Assert.That(service.LoadOrCreate("obsolete", out var banner), Is.Null);
            Assert.That(banner, Is.EqualTo("ui.save.obsolete_development_version"));
            CollectionAssert.AreEqual(obsolete, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
        }

        [Test] public void ReportingCombinesOnlyProvenFractionalSettlementsWithoutWritingDisplayState()
        {
            var h = new Harness(); h.Wall.Fraction = 1; h.Start(); h.Accept(100);
            Assert.That(h.Service.RecoveryReport.Closed, Is.False);
            h.Wall.Fraction = 3333334; var original = h.Step(101);
            Assert.That(original.IsSuccess, Is.True, original.Reason);
            Assert.That(original.RecoveryReceipt, Is.Not.Null);
            Assert.That(h.Service.RecoveryReport.Complete, Is.False);
            h.Wall.Fraction = 6666667; var residual = h.Step(102);
            Assert.That(residual.IsSuccess, Is.True, residual.Reason);
            var report = h.Service.RecoveryReport;
            Assert.That(report.Complete, Is.True);
            Assert.That(report.Eligible100ns, Is.EqualTo(original.RecoveryReceipt.Eligible100ns + residual.RecoveryReceipt.Eligible100ns));
            Assert.That(report.Awarded, Is.EqualTo(original.RecoveryReceipt.Awarded + residual.RecoveryReceipt.Awarded));
            Assert.That(report.Spent, Is.EqualTo(original.RecoveryManaSpent + residual.RecoveryManaSpent));
            Assert.That(report.ClosingWallet, Is.EqualTo(h.Current.structureRuntime.ManaReserve));
            byte[] before = h.F.FileSystem.ReadAllBytes(h.Service.SavePath);
            int operations = h.F.FileSystem.Operations.Count();
            string Localize(string key) => key == "ui.recovery.summary_format" ? "Eligible {0}; {1}; award {2}; wallet {3}/{4}" :
                key == "ui.recovery.rate_format" ? "Rate {0}" : key;
            string text = OfflinePassiveManaPresenter.BuildRecovery(report, report.ClosingWallet, report.Capacity,
                System.Globalization.CultureInfo.InvariantCulture, Localize);
            Assert.That(text, Does.Contain("101.6666667"));
            report.Observe(original.GetPersistedBytes(), residual, residual.RecoveryReceipt);
            Assert.That(report.Awarded, Is.EqualTo(original.RecoveryReceipt.Awarded + residual.RecoveryReceipt.Awarded), "Duplicate proof cannot add awards");
            Assert.That(OfflinePassiveManaPresenter.BuildRecovery(report, report.ClosingWallet, report.Capacity, null, key => key), Is.Empty);
            Assert.That(h.F.FileSystem.Operations.Count(), Is.EqualTo(operations));
            CollectionAssert.AreEqual(before, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
        }

        [Test] public void UncertainPredecessorProofDiscardsAttemptedReceiptAndRetryReportsOnce()
        {
            var h = new Harness(); var fs = new ObservedFileSystem(h.F.FileSystem);
            var service = CreateService(h.F, h.Inputs, h.Time, fs);
            var current = service.LoadOrCreate("report-predecessor", out _);
            service.CanonicalRuntimePublished += value => current = value;
            service.BeginCanonicalRecovery(current);
            Assert.That(service.AdvanceCanonicalRecovery(current, h.Mana, (_, __) => Assert.Fail()).IsSuccess, Is.True);
            var predecessor = service.CanonicalSession.GetCurrentBytes();
            fs.InterruptReplacement = true;
            Assert.That(service.AdvanceCanonicalRecovery(current, h.Mana, (_, __) => Assert.Fail()).IsSuccess, Is.False);
            Assert.That(service.RecoveryReport.Complete, Is.False);
            h.F.FileSystem.Seed(service.SavePath, predecessor); fs.InterruptReplacement = false;
            Assert.That(service.ResolveUncertainRecoveryPublication().IsSuccess, Is.True);
            Assert.That(service.RecoveryReport.Complete, Is.False);
            var original = service.AdvanceCanonicalRecovery(current, h.Mana, (_, __) => Assert.Fail());
            var residual = service.AdvanceCanonicalRecovery(current, h.Mana, (_, __) => Assert.Fail());
            Assert.That(original.IsSuccess && residual.IsSuccess, Is.True);
            Assert.That(service.RecoveryReport.Complete, Is.True);
            Assert.That(service.RecoveryReport.Awarded, Is.EqualTo(original.RecoveryReceipt.Awarded + residual.RecoveryReceipt.Awarded));
            Assert.That(service.ResolveUncertainRecoveryPublication().IsSuccess, Is.False);
        }

        [Test] public void LostPendingSpendingEvidenceCannotBecomeAZeroSpendingReceipt()
        {
            var h = new Harness(); var fs = new ObservedFileSystem(h.F.FileSystem);
            var service = CreateService(h.F, h.Inputs, h.Time, fs);
            var current = service.LoadOrCreate("report-missing-receipt", out _);
            service.CanonicalRuntimePublished += value => current = value;
            service.BeginCanonicalRecovery(current);
            Assert.That(service.AdvanceCanonicalRecovery(current, h.Mana, (_, __) => Assert.Fail()).IsSuccess, Is.True);
            Assert.That(service.AdvanceCanonicalRecovery(current, h.Mana, (_, __) => Assert.Fail()).IsSuccess, Is.True);
            fs.InterruptReplacement = true;
            Assert.That(service.AdvanceCanonicalRecovery(current, h.Mana, (_, __) => Assert.Fail()).IsSuccess, Is.False);
            // Explicitly model loss of display-only evidence, not save/time state.
            typeof(SaveService).GetField("pendingRecoveryReceipt", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(service, null);
            fs.InterruptReplacement = false;
            var proof = service.ResolveUncertainRecoveryPublication();
            Assert.That(proof.IsSuccess, Is.True, proof.Reason);
            Assert.That(proof.RecoveryManaSpent, Is.Zero, "Reconstructed proof's default is not historical spending evidence");
            Assert.That(service.RecoveryReport.Closed, Is.True);
            Assert.That(service.RecoveryReport.Complete, Is.False);
        }

        [Test] public void SyntheticPresentationKeepsDistinctRatesCapacityAndSpendingWithoutWalletInference()
        {
            // Pure presentation unit inputs. These synthetic receipts never enter
            // SaveService and do not claim a gameplay settlement occurred.
            var h = new Harness(); h.Start(); h.Accept(100);
            var predecessor = h.Service.CanonicalSession.GetCurrentBytes();
            var firstEpisode = h.Current.timeProvenance.Episode;
            var first = h.Step(102); Assert.That(first.IsSuccess, Is.True);
            var secondEpisode = h.Current.timeProvenance.Episode;
            var second = h.Step(105); Assert.That(second.IsSuccess, Is.True);
            var original = new RecoveryGrantReceipt(predecessor, first.GetPersistedBytes(), h.Inputs.Identity.Digest,
                firstEpisode, 1, new OfflinePassiveManaResult { EffectiveOfflineManaPerHour = 18, CalculatedPreCapacityAward = 20,
                    ActualAwardedMana = 10, CapacityLimited = true, Capacity = 1000 }, 3, first.RuntimeProjection.structureRuntime.ManaReserve);
            var residual = new RecoveryGrantReceipt(first.GetPersistedBytes(), second.GetPersistedBytes(), h.Inputs.Identity.Digest,
                secondEpisode, 2, new OfflinePassiveManaResult { EffectiveOfflineManaPerHour = 27, CalculatedPreCapacityAward = 7,
                    ActualAwardedMana = 7, Capacity = 1000 }, 2, second.RuntimeProjection.structureRuntime.ManaReserve);
            var report = new RecoveryEpisodeReport(predecessor, firstEpisode);
            report.Observe(predecessor, first, original); report.Observe(first.GetPersistedBytes(), second, residual);
            Assert.That(report.Complete, Is.True);
            Assert.That(report.Calculated, Is.EqualTo(27)); Assert.That(report.Awarded, Is.EqualTo(17));
            Assert.That(report.Spent, Is.EqualTo(5)); Assert.That(report.CapacityLimited, Is.True);
            string Localize(string key) => key == "ui.recovery.summary_format" ? "Eligible {0}; {1}; earned {2}; current {3}/{4}" :
                key == "ui.recovery.rates_format" ? "Original {0}; residual {1}" :
                key == OfflinePassiveManaPresenter.CapacityLimitedKey ? "Capacity limited" :
                key == "combat.c2.recovery_spent" ? "Healing cost {0}" : key;
            string text = OfflinePassiveManaPresenter.BuildRecovery(report, 23, 1000, System.Globalization.CultureInfo.InvariantCulture, Localize);
            Assert.That(text, Does.Contain("0.0000003")); Assert.That(text, Does.Contain("Original 18; residual 27"));
            Assert.That(text, Does.Contain("earned 17; current 23/1000"));
            Assert.That(text, Does.Contain("Capacity limited")); Assert.That(text, Does.Contain("Healing cost 5"));
        }

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

            // A retained reference is not proof that its mutable DTO still
            // represents the immutable bytes consumed before owner creation.
            double threshold = bound.Configuration.SuccessThreshold;
            int value = bound.Loot.items[0].worldValue;
            string digest = bound.Identity.Digest;
            try {
                if (wrongLoot) bound.Loot.items[0].worldValue = checked(value + 1);
                else bound.Configuration.SuccessThreshold = threshold == 0d ? .01d : 0d;
                var sameReference = new DetachedCanonicalWriteAuthority(h.F.Production, h.F.Compatibility,
                    bound.Configuration, h.F.Context, h.F.Profile, economy: bound.Economy, runLoot: bound.Loot);
                var changedResult = sameReference.SaveRecognizedState(h.Service.SavePath, h.F.FileSystem,
                    h.Service.CanonicalSession, h.Current);
                TestContext.WriteLine("Same consumed reference: loot=" + wrongLoot + ", digestUnchanged=" +
                    (bound.Identity.Digest == digest) + ", published=" + changedResult.IsSuccess + ", reason=" + changedResult.Reason);
                Assert.That(changedResult.IsSuccess, Is.False, "Mutable consumed DTO must not authorize publication under the old digest");
                CollectionAssert.AreEqual(bytes, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
            }
            finally {
                bound.Configuration.SuccessThreshold = threshold;
                bound.Loot.items[0].worldValue = value;
            }

            for (int phase = 0; phase < 3; phase++) {
                var owned = new Harness();
                if (phase > 0) { owned.Start(); owned.Accept(100); }
                if (phase > 1) owned.Accept(102);
                var serviceContext = (DetachedCurrentTargetValidationContext)typeof(SaveService)
                    .GetField("_validationContext", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owned.Service);
                var inputs = serviceContext.RecoveryInputs;
                void RejectMutation(Action change, Action restore)
                {
                    byte[] before = owned.Service.CanonicalSession.GetCurrentBytes();
                    var entry = owned.Time.RetainedRecoveryEntry;
                    var boundary = owned.Current.timeProvenance.AccountedBoundary;
                    int writes = owned.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Write);
                    // Construct first: late DTO mutation must also invalidate an
                    // existing writer, not only a newly constructed one.
                    var existing = new DetachedCanonicalWriteAuthority(owned.F.Production, owned.F.Compatibility,
                        inputs.Configuration, serviceContext, owned.F.Profile, economy: inputs.Economy, runLoot: inputs.Loot);
                    try {
                        change();
                        Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(before, serviceContext).IsValid, Is.False);
                        Assert.That(existing.SaveRecognizedState(owned.Service.SavePath, owned.F.FileSystem,
                            owned.Service.CanonicalSession, owned.Current).IsSuccess, Is.False);
                        Assert.Throws<InvalidOperationException>(() => owned.Service.BeginCanonicalRecovery(owned.Current));
                        Assert.That(owned.Time.RetainedRecoveryEntry, Is.SameAs(entry));
                        Assert.That(owned.Current.timeProvenance.AccountedBoundary, Is.EqualTo(boundary));
                        Assert.That(owned.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Write), Is.EqualTo(writes));
                        CollectionAssert.AreEqual(before, owned.F.FileSystem.ReadAllBytes(owned.Service.SavePath));
                    }
                    finally { restore(); }
                    Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(before, serviceContext).IsValid, Is.True);
                }
                if (wrongLoot) {
                    int original = inputs.Loot.items[0].worldValue;
                    RejectMutation(() => inputs.Loot.items[0].worldValue++, () => inputs.Loot.items[0].worldValue = original);
                } else {
                    var goblin = inputs.Configuration.CombatC0.Monsters.Single(m => m.Id == MvpDungeonPlacementIds.GoblinOptionId);
                    int health = goblin.MaxHealth, tick = inputs.Bootstrap.tickSeconds;
                    double progress = inputs.Bootstrap.researchProgressScaffold.progressPerActiveSecond;
                    int occupancyVersion = inputs.Occupancy.Value.SchemaVersion;
                    int contextVersion = serviceContext.RoomContentOccupancy.Value.SchemaVersion;
                    RejectMutation(() => goblin.MaxHealth++, () => goblin.MaxHealth = health);
                    RejectMutation(() => inputs.Bootstrap.tickSeconds++, () => inputs.Bootstrap.tickSeconds = tick);
                    RejectMutation(() => inputs.Bootstrap.researchProgressScaffold.progressPerActiveSecond++,
                        () => inputs.Bootstrap.researchProgressScaffold.progressPerActiveSecond = progress);
                    RejectMutation(() => inputs.Occupancy.Value.SchemaVersion++, () => inputs.Occupancy.Value.SchemaVersion = occupancyVersion);
                    RejectMutation(() => serviceContext.RoomContentOccupancy.Value.SchemaVersion++,
                        () => serviceContext.RoomContentOccupancy.Value.SchemaVersion = contextVersion);
                }
            }
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
            bool nestedSaveAllowed = true;
            fs.AfterFlush = () => {
                if (callbacks++ != 0) return;
                Assert.That(service.ForegroundWriting, Is.True);
                scope.Focus(false, 101);
                nestedSaveAllowed = service.Save(scope.Root.Save, SaveReason.StateChange);
            };
            scope.Pulse(100);
            Assert.That(callbacks, Is.GreaterThan(0));
            Assert.That(scope.Root.Save.timeProvenance.Episode.Phase, Is.EqualTo(RecoveryEpisode.OriginalPending));
            Assert.That(scope.Root.Save.timeProvenance.AccountedBoundary, Is.EqualTo(new UnixTimePoint(100, 0)));
            Assert.That(scope.Root.Save.livingDungeon.IsDormant, Is.True);
            Assert.That(h.Time.RecoveryClockActive, Is.True);
            Assert.That(nestedSaveAllowed, Is.False);
            var writes = h.F.FileSystem.Operations.Where(o => o.Type == Faults.OperationType.Write).Skip(before).ToArray();
            Assert.That(writes.Length, Is.EqualTo(2), "The existing protocol writes one rollback and one candidate");
            Assert.That(writes.Count(o => o.Paths[0].EndsWith(".rollback", StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(writes.Count(o => o.Paths[0].EndsWith(".candidate", StringComparison.Ordinal)), Is.EqualTo(1));
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
            Assert.That(service.RecoveryReport.Complete, Is.False, "Provisional receipts are not committed");
            var committed = h.F.FileSystem.ReadAllBytes(service.SavePath);
            var committedState = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(committed, h.F.Context);
            Assert.That(committedState.CurrentTargetValidated, Is.True);
            int writes = h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Write);
            int replaces = h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Replace);
            int deletes = h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Delete);
            h.F.FileSystem.Seed(service.SavePath, Encoding.UTF8.GetBytes("test-only unrecognized authority"));
            Assert.That(service.ResolveUncertainRecoveryPublication().IsSuccess, Is.False);
            Assert.That(service.RequiresCanonicalRecovery, Is.True);
            Assert.That(h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Write), Is.EqualTo(writes));
            Assert.That(h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Replace), Is.EqualTo(replaces));
            Assert.That(h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Delete), Is.EqualTo(deletes));
            h.F.FileSystem.Seed(service.SavePath, committed);
            fs.InterruptReplacement = false; h.Wall.Seconds = 110;
            var proof = service.ResolveUncertainRecoveryPublication(); Assert.That(proof.IsSuccess, Is.True, proof.Reason);
            CollectionAssert.AreEqual(committed, proof.GetPersistedBytes());
            Assert.That(h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Write), Is.EqualTo(writes));
            Assert.That(h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Replace), Is.EqualTo(replaces));
            Assert.That(proof.Validation.LivingDungeon.Scheduler?.AcceptedThrough ?? -1,
                Is.EqualTo(committedState.LivingDungeon.Scheduler?.AcceptedThrough ?? -1));
            Assert.That(proof.Validation.LivingDungeon.Scheduler?.ClosedThrough ?? -1,
                Is.EqualTo(committedState.LivingDungeon.Scheduler?.ClosedThrough ?? -1));
            Assert.That(current.timeProvenance.Episode?.Phase, Is.EqualTo(residual ? null : RecoveryEpisode.ResidualPending));
            var once = h.F.FileSystem.ReadAllBytes(service.SavePath);
            Assert.That(service.ResolveUncertainRecoveryPublication().IsSuccess, Is.False);
            CollectionAssert.AreEqual(once, h.F.FileSystem.ReadAllBytes(service.SavePath));
            Assert.That(time.ForegroundThrough, Is.Zero);
            if (!residual)
                Assert.That(service.AdvanceCanonicalRecovery(current, h.Mana, (_, __) => Assert.Fail()).IsSuccess, Is.True);
            Assert.That(service.RecoveryReport.Complete, Is.True, "Retained receipt survives exact candidate-authority proof");
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
            if (!originalComplete) {
                current = result.RuntimeProjection;
                result = reopened.AdvanceCanonicalRecovery(current, h.Mana, (_, __) => Assert.Fail());
                Assert.That(result.IsSuccess, Is.True, result.Reason);
            }
            Assert.That(reopened.RecoveryReport.Closed, Is.True);
            Assert.That(reopened.RecoveryReport.Complete, Is.EqualTo(!originalComplete));
            if (originalComplete) {
                string neutral = OfflinePassiveManaPresenter.BuildRecovery(reopened.RecoveryReport,
                    result.RuntimeProjection.structureRuntime.ManaReserve, h.Mana.ResolveRecoveryDuration(result.RuntimeProjection, h.Inputs.Configuration, 0, false).Capacity,
                    null, key => key == "ui.recovery.completed_format" ? "Recovery complete. Current mana: {0}/{1}" : key);
                Assert.That(neutral, Does.Contain("Recovery complete. Current mana:"));
                Assert.That(neutral, Does.Not.Contain("No offline mana"));
            }
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
            internal void Disable(long seconds) {
                harness.Wall.Seconds = seconds;
                typeof(GameRoot).GetMethod("OnDisable", Private).Invoke(Root, null);
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
        [Test] public void OrdinaryMetadataRollbackHoldsForegroundWithoutInventingBackground()
        {
            var h = new Harness(); h.Start(); h.Accept(100); h.Accept(102); h.Accept(105);
            using var scope = new RootScope(h); scope.Pulse(105);
            var before = h.F.FileSystem.ReadAllBytes(h.Service.SavePath);
            double wallet = scope.Root.Save.structureRuntime.ManaReserve;
            scope.Root.Save.lastPausedUtcUnix = 104; // Test-only metadata change; no physical pause is claimed.
            h.F.FileSystem.EnableTargetedFailure(Faults.OperationType.Write,
                paths => paths[0].EndsWith(".candidate", StringComparison.Ordinal), 1, false);
            Assert.That(h.Service.Save(scope.Root.Save, SaveReason.StateChange), Is.False);
            Assert.That(h.F.FileSystem.FailedOperation, Is.Not.Null);
            CollectionAssert.AreEqual(before, h.F.FileSystem.ReadAllBytes(h.Service.SavePath));
            h.F.FileSystem.DisableFailure();
            int writes = h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Write);
            scope.Pulse(110);
            Assert.That(h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Write), Is.EqualTo(writes));
            Assert.That(h.Time.RecoveryClockActive, Is.False);
            Assert.That(scope.Root.Save.timeProvenance.Episode, Is.Null);
            Assert.That(h.Service.Save(scope.Root.Save, SaveReason.ManualDev), Is.True);
            Assert.That(scope.Root.Save.timeProvenance.AccountedBoundary, Is.EqualTo(new UnixTimePoint(105, 0)));
            Assert.That(h.Time.HasDeferredRecovery, Is.False);
            Assert.That(scope.Root.Save.structureRuntime.ManaReserve, Is.EqualTo(wallet));
            scope.Pulse(111);
            Assert.That(h.Time.ForegroundThrough, Is.EqualTo(1000));
            Assert.That(h.Service.Save(scope.Root.Save, SaveReason.ManualDev), Is.True,
                "The explicit barrier publishes captured time before checking durable acceptance");
            Assert.That(scope.Root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(1000));
            Assert.That(scope.Root.Save.structureRuntime.ManaReserve, Is.EqualTo(wallet));
            var clock = new Clock { Seconds = 111 };
            var time = new TimeService(new SimpleLogger(false), h.Inputs.Bootstrap.tickSeconds,
                h.Inputs.Bootstrap.timeRules.detectClockSkewSeconds, clock, () => 0);
            time.BeginRecoveryForeground(h.Inputs.TwoFloor.UnitsPerSecond);
            var reopened = CreateService(h.F, h.Inputs, time);
            var current = reopened.LoadOrCreate("h1f4-known-stall-reopen", out var reason);
            Assert.That(current, Is.Not.Null, reason);
            Assert.That(current.timeProvenance.Episode, Is.Null);
            Assert.That(current.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(1000));
            Assert.That(current.structureRuntime.ManaReserve, Is.EqualTo(wallet));
        }
        [TestCase(false)] [TestCase(true)]
        public void DormantOrdinaryRetryOrCommittedCandidateProofClosesOnlyTheZeroPrefix(bool uncertain)
        {
            var h = new Harness(); h.Start(); h.Accept(100); h.Accept(102); h.Accept(105);
            var fs = new ObservedFileSystem(h.F.FileSystem);
            var service = CreateService(h.F, h.Inputs, h.Time, fs);
            var current = service.LoadOrCreate("h1f4-dormant-acknowledgement", out var reason);
            Assert.That(current, Is.Not.Null, reason); h.Time.AttachSave(current);
            service.CanonicalRuntimePublished += value => current = value;
            double wallet = current.structureRuntime.ManaReserve;
            current.lastPausedUtcUnix = 104;
            if (uncertain) fs.InterruptReplacement = true;
            else h.F.FileSystem.EnableTargetedFailure(Faults.OperationType.Write,
                paths => paths[0].EndsWith(".candidate", StringComparison.Ordinal), 1, false);
            Assert.That(service.Save(current, SaveReason.StateChange), Is.False);
            fs.InterruptReplacement = false; h.F.FileSystem.DisableFailure(); h.Wall.Seconds = 110;
            if (uncertain) {
                int writes = h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Write);
                int replaces = h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Replace);
                var proof = service.ResolveUncertainRecoveryPublication();
                Assert.That(proof.IsSuccess, Is.True, proof.Reason);
                Assert.That(h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Write), Is.EqualTo(writes));
                Assert.That(h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Replace), Is.EqualTo(replaces));
            } else Assert.That(service.Save(current, SaveReason.StateChange), Is.True);
            Assert.That(current.livingDungeon.IsDormant, Is.True);
            Assert.That(current.structureRuntime.ManaReserve, Is.EqualTo(wallet));
            Assert.That(current.timeProvenance.AccountedBoundary, Is.EqualTo(new UnixTimePoint(105, 0)));
            Assert.That(h.Time.HasDeferredRecovery, Is.False);
            Assert.That(h.Time.ForegroundThrough, Is.Zero);
            h.Wall.Seconds = 111;
            var cut = h.Time.CaptureForegroundObservation(service.CanonicalSession);
            Assert.That(cut.CapturedThrough, Is.EqualTo(1000));
            Assert.That(cut.AcceptedThrough, Is.EqualTo(-1));
        }
        [TestCase(false)] [TestCase(true)]
        public void LifecycleObservationDuringOrdinaryPublicationCannotStartAnotherWriter(bool disable)
        {
            var h = new Harness(); h.Start(); h.Accept(100); h.Accept(102); h.Accept(105);
            using var scope = new RootScope(h); scope.Pulse(105);
            var fs = new ObservedFileSystem(h.F.FileSystem);
            var service = CreateService(h.F, h.Inputs, h.Time, fs);
            var current = service.LoadOrCreate("h1f4-ordinary-reentrancy", out var reason);
            Assert.That(current, Is.Not.Null, reason); h.Time.AttachSave(current);
            scope.Rebind(service, h.Time, current); current.lastPausedUtcUnix = 104;
            int before = h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Write);
            int callbacks = 0;
            fs.AfterFlush = () => {
                if (callbacks++ == 0) { if (disable) scope.Disable(106); else scope.Focus(false, 106); }
            };
            Assert.That(service.Save(current, SaveReason.StateChange), Is.True);
            Assert.That(callbacks, Is.GreaterThan(0));
            scope.Pulse(106);
            Assert.That(h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Write), Is.EqualTo(before + 2));
            Assert.That(scope.Root.Save.timeProvenance.AccountedBoundary, Is.EqualTo(new UnixTimePoint(105, 0)));
            Assert.That(scope.Root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.Zero);
            Assert.That(h.Time.HasDeferredBackground, Is.True);
            Assert.That(h.Time.ForegroundThrough, Is.EqualTo(1000));
        }
        [Test] public void KnownForegroundStallThenObservedBackgroundGrantsOnlyItsEligibleTimeAndReopens()
        {
            var h = new Harness(); h.Start(); h.Accept(100); h.Accept(102); h.Accept(105);
            using var scope = new RootScope(h); scope.Pulse(105);
            double wallet = scope.Root.Save.structureRuntime.ManaReserve;
            scope.Root.Save.lastPausedUtcUnix = 104;
            h.F.FileSystem.EnableTargetedFailure(Faults.OperationType.Write,
                paths => paths[0].EndsWith(".candidate", StringComparison.Ordinal), 1, false);
            Assert.That(h.Service.Save(scope.Root.Save, SaveReason.StateChange), Is.False);
            h.F.FileSystem.DisableFailure();
            scope.Pulse(110); // Five known stalled foreground seconds: no grant or automatic retry.
            Assert.That(h.Time.HasForegroundPersistenceStall, Is.True);
            scope.Focus(false, 110); // Explicit lifecycle retry proves the held foreground prefix.
            Assert.That(h.Time.RecoveryClockActive, Is.False);
            Assert.That(h.Time.HasDeferredBackground, Is.True);
            int writes = h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Write);
            scope.Pulse(115);
            Assert.That(h.F.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Write), Is.EqualTo(writes));
            Assert.That(scope.Root.Save.structureRuntime.ManaReserve, Is.EqualTo(wallet));
            scope.Focus(true, 120);
            Assert.That(h.Time.RecoveryClockActive, Is.True);
            Assert.That(h.Time.RecoveryOriginalEligible100ns, Is.EqualTo(100000000));
            scope.Pulse(120);
            Assert.That(scope.Root.Save.timeProvenance.Episode.EntryContext, Is.EqualTo(RecoveryEpisode.KnownBackground));
            double original = h.Mana.ResolveRecoveryDuration(scope.Root.Save, h.Inputs.Configuration, 100000000, false).WalletAfter;
            scope.Pulse(121);
            Assert.That(scope.Root.Save.structureRuntime.ManaReserve, Is.EqualTo(original));
            double residual = h.Mana.ResolveRecoveryDuration(scope.Root.Save, h.Inputs.Configuration, 20000000, false).WalletAfter;
            scope.Pulse(122);
            Assert.That(scope.Root.Save.structureRuntime.ManaReserve, Is.EqualTo(residual));
            Assert.That(scope.Root.Save.timeProvenance.Episode, Is.Null);
            Assert.That(scope.Root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.Zero);
            Assert.That(h.Service.RecoveryReport.Complete, Is.True);
            Assert.That(h.Service.RecoveryReport.Eligible100ns, Is.EqualTo(120000000), "The five known stalled seconds are excluded");
            var clock = new Clock { Seconds = 130 };
            var time = new TimeService(new SimpleLogger(false), h.Inputs.Bootstrap.tickSeconds,
                h.Inputs.Bootstrap.timeRules.detectClockSkewSeconds, clock, () => 0);
            time.BeginRecoveryForeground(h.Inputs.TwoFloor.UnitsPerSecond);
            var reopened = CreateService(h.F, h.Inputs, time);
            var current = reopened.LoadOrCreate("h1f4-background-after-stall-reopen", out var reason);
            Assert.That(current, Is.Not.Null, reason);
            Assert.That(current.structureRuntime.ManaReserve, Is.EqualTo(residual));
            time.BindLoadedForegroundHorizon(current.livingDungeon.Scheduler.AcceptedThrough);
            reopened.BeginCanonicalRecovery(current);
            Assert.That(time.RecoveryOriginalEligible100ns, Is.EqualTo(80000000)); // Only the unrecorded W=122 -> R=130 suffix.
            Assert.That(time.ForegroundThrough, Is.Zero);
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
        [TestCase(false)]
        [TestCase(true)]
        public void AcceptedTicksAndResearchDrainAcrossTwoCrashesWithoutReplayingClosedPrefix(bool paid)
        {
            var h = new Harness(paidResearch: paid); h.Start(); h.Accept(100); h.Accept(102); h.Accept(105);
            // A declared synthetic prior confirmed record supplies this fixture's
            // Research prerequisite. It is never historical producer evidence.
            h.Current.runHistory.NextRunSequence = 2;
            h.Current.runHistory.AppendOutcome(new RunOutcomeRecord { RunId = "run-1" }, 5);
            Assert.That(h.Service.Save(h.Current, SaveReason.StateChange), Is.True);
            using var scope = new RootScope(h);
            if (paid) {
                h.Current.structureRuntime.ManaReserve = 1000;
                Assert.That(h.Service.Save(h.Current, SaveReason.StateChange), Is.True);
            }
            var started = paid ? scope.Root.StartConfiguredPaidResearch() : scope.Root.StartUnpaidResearchForTests();
            Assert.That(started.Succeeded, Is.True, started.FeedbackLocalizationKey);
            string startedRule = scope.Root.Save.researchProgress.RuleSourceIdUsed;
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

            SaveService Open(Clock clock, out TimeService time, out SaveData current, out CanonicalOfflinePassiveManaService mana) {
                time = new TimeService(new SimpleLogger(false), 10, 300, clock, () => 0);
                time.BeginRecoveryForeground(h.Inputs.TwoFloor.UnitsPerSecond);
                var replayInputs = paid ? PaidBundle(h.F, updatedDefault: true) : h.Inputs;
                Assert.That(replayInputs.Identity.Digest, Is.EqualTo(h.Inputs.Identity.Digest));
                if (paid) {
                    Assert.That(replayInputs.ResearchTiming.Select(h.Inputs.Configuration.MvpFirstSessionObjective.AnalysisResearchProjectId,
                        out var selected), Is.Null);
                    Assert.That(selected.Reference, Is.Not.EqualTo(startedRule));
                }
                mana = CreateRecoveryMana(h.F, replayInputs);
                var service = CreateService(h.F, replayInputs, time);
                current = service.LoadOrCreate("accepted-reopen", out var reason); Assert.That(current, Is.Not.Null, reason);
                Assert.That(time.RetainedRecoveryEntry, Is.Null, "Authority must be established before a new entry observation.");
                time.BindLoadedForegroundHorizon(current.livingDungeon.Scheduler.AcceptedThrough);
                scope.Rebind(service, time, current); service.BeginCanonicalRecovery(current); return service;
            }
            void Step(SaveService service, CanonicalOfflinePassiveManaService mana, Clock clock, long at) {
                clock.Seconds = at;
                var result = service.AdvanceCanonicalRecovery(scope.Root.Save, mana, Tick);
                Assert.That(result.IsSuccess, Is.True, result.Reason);
                Assert.That(scope.Root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(acceptedHorizon));
            }
            var one = new Clock { Seconds = 2200 };
            var first = Open(one, out _, out var firstSave, out var firstMana);
            Assert.That(firstSave.totalTicks, Is.EqualTo(firstTicks)); Assert.That(firstSave.livingDungeon.Scheduler.ClosedThrough, Is.EqualTo(firstClosed));
            Step(first, firstMana, one, 2200); Assert.That(scope.Root.Save.timeProvenance.Episode.Entry.Seconds, Is.EqualTo(2200));
            Assert.That(scope.Root.Save.totalTicks, Is.EqualTo(firstTicks), "Entry publication precedes any drain.");
            Step(first, firstMana, one, 2201);
            long secondClosed = scope.Root.Save.livingDungeon.Scheduler.ClosedThrough;
            Assert.That(secondClosed, Is.GreaterThan(firstClosed).And.LessThan(acceptedHorizon));
            long secondTicks = scope.Root.Save.totalTicks;

            var two = new Clock { Seconds = 2300 };
            var second = Open(two, out var secondTime, out var secondSave, out var secondMana);
            Assert.That(secondTime.RetainedRecoveryEntry, Is.Null);
            Assert.That(secondSave.timeProvenance.Episode.Entry.Seconds, Is.EqualTo(2200));
            Assert.That(secondSave.totalTicks, Is.EqualTo(secondTicks)); Assert.That(secondSave.livingDungeon.Scheduler.ClosedThrough, Is.EqualTo(secondClosed));
            int pumps = 0;
            while (!scope.Root.Save.livingDungeon.IsClosed && pumps++ < 10) Step(second, secondMana, two, 2300 + pumps);
            Assert.That(scope.Root.Save.livingDungeon.IsClosed, Is.True);
            Assert.That(scope.Root.Save.totalTicks, Is.EqualTo(200));
            CollectionAssert.AreEqual(Enumerable.Range(1, 200).Select(n => (long)n).ToArray(), ticks);
            Assert.That(scope.Root.Save.timeProvenance.Episode.Phase, Is.EqualTo(RecoveryEpisode.OriginalPending));
            Assert.That(scope.Root.Save.timeProvenance.Episode.OriginalEligible100ns, Is.EqualTo(950000000));
            Assert.That(scope.Root.ClaimConfiguredPlayerResearch().Succeeded, Is.False, "Recovery still owns the action barrier.");
            if (paid) {
                two.Seconds = 2310;
                var predecessor = h.F.FileSystem.ReadAllBytes(second.SavePath);
                var rejected = second.AdvanceCanonicalRecovery(scope.Root.Save, h.Mana, Tick);
                Assert.That(rejected.IsSuccess, Is.False);
                Assert.That(rejected.Reason, Is.EqualTo(RecoveryEpisode.Invalid));
                CollectionAssert.AreEqual(predecessor, h.F.FileSystem.ReadAllBytes(second.SavePath));
            }
            double originalWallet = secondMana.ResolveRecoveryDuration(scope.Root.Save, h.Inputs.Configuration, 950000000, false).WalletAfter;
            Step(second, secondMana, two, 2310);
            Assert.That(scope.Root.Save.structureRuntime.ManaReserve, Is.EqualTo(originalWallet));
            Assert.That(scope.Root.Save.timeProvenance.Episode.ResidualEligible100ns, Is.EqualTo(1100000000));
            double residualWallet = secondMana.ResolveRecoveryDuration(scope.Root.Save, h.Inputs.Configuration, 1110000000, false).WalletAfter;
            Step(second, secondMana, two, 2311);
            Assert.That(scope.Root.Save.structureRuntime.ManaReserve, Is.EqualTo(residualWallet));
            Assert.That(scope.Root.Save.timeProvenance.Episode, Is.Null);
            Assert.That(secondTime.ForegroundThrough, Is.EqualTo(acceptedHorizon), "UTC recovery never creates new adventurer time.");
            if (paid) {
                Assert.That(scope.Root.Save.researchProgress.ProgressUnits, Is.EqualTo(120000));
                Assert.That(scope.Root.Save.researchProgress.RuleSourceIdUsed, Is.EqualTo(startedRule));
            }
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
            Assert.That(h.Service.RecoveryReport.Complete, Is.True);
            Assert.That(h.Service.RecoveryReport.Eligible100ns, Is.EqualTo(1030000000));
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
