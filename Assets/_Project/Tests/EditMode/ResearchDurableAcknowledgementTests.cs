#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;
using Faults = DungeonBuilder.M0.Tests.EditMode.Gd66DetachedSpatialMigrationTransactionTests;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchDurableAcknowledgementTests
    {
        internal const string Project = "test.activity.analysis";
        internal const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private Fixture f;
        private SaveService service;
        private SaveData current;
        private int publications;

        [SetUp] public void SetUp()
        {
            f = Fixture.Create("\"unrelatedResearchEvidence\":{\"n\":1.00}", "\"unrelatedRoot\":[true,null]",
                workloadProfile: SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile,
                recoveryInputBytes: AuthoredResearchInputs());
            service = OpenService(f, out current);
            current.researchPending = null; current.researchProgress = null;
            current.completedResearch = new CompletedResearchState(); current.completedObjectives = new CompletedObjectiveState();
            current.runHistory.AppendOutcome(new RunOutcomeRecord { RunId = "run-1",
                LootExtractionSummary = new RunLootExtractionSummary { TotalExtractedWorldValue = 1 } }, 5);
            current.runHistory.NextRunSequence = 2; // This synthetic confirmed run already owns run-1.
            service.CanonicalRuntimePublished += save => { current = save; publications++; };
            Assert.That(service.Save(current, SaveReason.StateChange), Is.True);
            publications = 0;
        }

        internal static SaveService OpenService(Fixture fixture, out SaveData loaded)
        {
            // This is a synthetic current-schema Research fixture, not historical
            // activation evidence. Use its already-bound consumed inputs and a
            // fixed test clock; metadata writes cannot move the fixture's W.
            var inputs = fixture.Context.RecoveryInputs;
            var wall = new ResearchFixtureClock(fixture.Runtime.timeProvenance.AccountedBoundary);
            return OpenService(fixture, out loaded, wall, () => 0, out _);
        }

        internal static SaveService OpenService(Fixture fixture, out SaveData loaded,
            IPreciseTimeSource wall, Func<double> monotonic, out TimeService time)
        {
            var inputs = fixture.Context.RecoveryInputs;
            time = new TimeService(new SimpleLogger(false), inputs.Bootstrap.tickSeconds,
                inputs.Bootstrap.timeRules.detectClockSkewSeconds, wall, monotonic);
            time.BeginRecoveryForeground(inputs.TwoFloor.UnitsPerSecond);
            var result = new SaveService(new SimpleLogger(false, (level, message) => { }),
                new SaveConfig { fileName = Path.GetFileName(fixture.ActivePath), useAtomicWrites = true }, Path.GetDirectoryName(fixture.ActivePath));
            result.ConfigureCanonical(fixture.Profile, fixture.Production, fixture.Compatibility, fixture.Configuration,
                LegacyGameplayConfigurationContract.SerializeCanonical(fixture.Configuration), fixture.PositionProfiles, fixture.Occupancy);
            // Native creation after explicit deletion needs these before LoadOrCreate,
            // just as the real GameRoot bootstrap supplies them before its first load.
            result.ConfigureStructuralEconomy(inputs.Economy);
            result.ConfigureContentAcquisitionEconomy(fixture.Acquisition);
            result.SetPreflightEvaluatorForTests(path => new SpatialMigrationActivationPreflight(true,
                SpatialMigrationCapabilityReason.Ready, SpatialMigrationPlatform.WindowsEditor, fixture.FileSystem, path));
            result.SetTimeSourceForTests(wall);
            result.ConfigureRecovery(inputs, time);
            loaded = result.LoadOrCreate("r0-test", out string banner);
            Assert.That(loaded, Is.Not.Null, banner);
            time.BindLoadedForegroundHorizon(loaded.livingDungeon.Scheduler?.AcceptedThrough ?? -1);
            time.AttachSave(loaded);
            result.CanonicalRuntimePublished += time.AttachSave;
            return result;
        }

        private sealed class ResearchFixtureClock : IPreciseTimeSource
        {
            private readonly UnixTimePoint now;
            internal ResearchFixtureClock(UnixTimePoint value) { now = value; }
            public UnixTimePoint UtcNow() => now;
            public long UtcNowUnixSeconds() => now.Seconds;
        }

        // Historical presentation fixtures use the schema-6 placement sandbox. Run
        // their research segment on a real canonical session and publication path,
        // then return the committed research to that fixture's legacy presentation.
        internal sealed class CanonicalRootScope : IDisposable
        {
            private readonly GameRoot root;
            private readonly SaveData legacy;
            private readonly SaveService previousService;
            private readonly ProductionSpatialContentSnapshot previousProduction;
            private readonly ContentBootstrap previousBootstrap;
            private readonly object previousTick;
            private readonly SaveService canonical;
            internal CanonicalRootScope(GameRoot root, bool starterDungeon = false)
            {
                this.root = root; legacy = root.Save; previousService = root.SaveService;
                previousProduction = root.Content.ProductionSpatialContent;
                previousBootstrap = root.Content.Bootstrap;
                previousTick = typeof(GameRoot).GetField("_configuredSimulationTickSeconds", Private).GetValue(root);
                Assert.That(root.TimeService, Is.Null, "This legacy presentation sandbox must not replace an existing observation owner");
                var run = (RunSimulationService)typeof(GameRoot).GetField("_runSimulationService", Private).GetValue(root);
                var bytes = DungeonBuilder.M0.Tests.EditMode.PhaseFourTestSupport.AuthoredInputs((config, bootstrap) => {
                    config.MvpFirstSessionObjective = JsonUtility.FromJson<MvpFirstSessionObjectiveConfig>(JsonUtility.ToJson(run.Config.MvpFirstSessionObjective));
                    bootstrap.tickSeconds = previousBootstrap.tickSeconds;
                    bootstrap.researchPendingScaffold = JsonUtility.FromJson<ResearchPendingScaffoldConfig>(JsonUtility.ToJson(previousBootstrap.researchPendingScaffold));
                    bootstrap.researchProgressScaffold = JsonUtility.FromJson<ResearchProgressScaffoldConfig>(JsonUtility.ToJson(previousBootstrap.researchProgressScaffold));
                    bootstrap.researchCompletionEligibilityScaffold = JsonUtility.FromJson<ResearchCompletionEligibilityScaffoldConfig>(JsonUtility.ToJson(previousBootstrap.researchCompletionEligibilityScaffold));
                    bootstrap.researchCompletionClaimScaffold = JsonUtility.FromJson<ResearchCompletionClaimScaffoldConfig>(JsonUtility.ToJson(previousBootstrap.researchCompletionClaimScaffold));
                    bootstrap.researchVerificationScaffold = JsonUtility.FromJson<ResearchVerificationScaffoldConfig>(JsonUtility.ToJson(previousBootstrap.researchVerificationScaffold));
                    bootstrap.researchUnlockBridge = JsonUtility.FromJson<ResearchUnlockBridgeConfig>(JsonUtility.ToJson(previousBootstrap.researchUnlockBridge));
                });
                string JsonOrNull(object value) => value == null ? "null" : JsonUtility.ToJson(value);
                // Explicit synthetic initial Research/history, before ownership.
                // Do not clone the presentation sandbox's time/spatial authority
                // or overwrite a bound owner's runtime with that legacy object.
                var fixture = Fixture.Create("\"lastSavedUtcUnix\":" + Math.Max(1, legacy.lastSavedUtcUnix) +
                    ",\"researchPending\":" + JsonOrNull(legacy.researchPending) +
                    ",\"researchProgress\":" + JsonOrNull(legacy.researchProgress) +
                    ",\"completedResearch\":" + JsonOrNull(legacy.completedResearch) +
                    ",\"completedObjectives\":" + JsonOrNull(legacy.completedObjectives) +
                    ",\"runHistory\":" + JsonOrNull(legacy.runHistory),
                    workloadProfile: DungeonBuilder.M0.Tests.EditMode.PhaseFourTestSupport.ProductionProfile(), recoveryInputBytes: bytes);
                if (starterDungeon)
                {
                    foreach (var pair in new[] {
                        (MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId),
                        (MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId),
                        (MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId),
                        (MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId) })
                        fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.Place(pair.Item1, pair.Item2)));
                }
                var clock = new ResearchFixtureClock(fixture.Runtime.timeProvenance.AccountedBoundary);
                canonical = OpenService(fixture, out var save, clock, () => 0, out var owner);
                typeof(GameRoot).GetProperty(nameof(GameRoot.SaveService)).SetValue(root, canonical);
                typeof(GameRoot).GetProperty(nameof(GameRoot.Save)).SetValue(root, save);
                typeof(GameRoot).GetProperty(nameof(GameRoot.TimeService)).SetValue(root, owner);
                typeof(GameRoot).GetField("_configuredSimulationTickSeconds", Private).SetValue(root, fixture.Context.RecoveryInputs.Bootstrap.tickSeconds);
                root.Content.BindRecoveryBootstrap(fixture.Context.RecoveryInputs);
                typeof(ContentService).GetProperty(nameof(ContentService.ProductionSpatialContent)).SetValue(root.Content, fixture.Production);
                canonical.BeforeForegroundAction = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), root,
                    typeof(GameRoot).GetMethod("FlushForeground", Private));
                canonical.ForegroundCurrent = () => root.Save;
                canonical.CanonicalRuntimePublished += (Action<SaveData>)Delegate.CreateDelegate(typeof(Action<SaveData>), root,
                    typeof(GameRoot).GetMethod("PublishCanonicalRuntime", Private));
                root.RefreshOfflineSummaryLines();
            }
            public void Dispose()
            {
                legacy.researchPending = root.Save.researchPending; legacy.researchProgress = root.Save.researchProgress;
                legacy.completedResearch = root.Save.completedResearch; legacy.completedObjectives = root.Save.completedObjectives;
                canonical.BeforeForegroundAction = null; canonical.ForegroundCurrent = null;
                typeof(GameRoot).GetProperty(nameof(GameRoot.TimeService)).SetValue(root, null);
                typeof(GameRoot).GetField("_configuredSimulationTickSeconds", Private).SetValue(root, previousTick);
                typeof(ContentService).GetProperty(nameof(ContentService.Bootstrap)).SetValue(root.Content, previousBootstrap);
                typeof(GameRoot).GetProperty(nameof(GameRoot.Save)).SetValue(root, legacy);
                typeof(GameRoot).GetProperty(nameof(GameRoot.SaveService)).SetValue(root, previousService);
                typeof(ContentService).GetProperty(nameof(ContentService.ProductionSpatialContent)).SetValue(root.Content, previousProduction);
                root.RefreshOfflineSummaryLines();
            }
        }

        private PlayerResearchActionHandler Handler(Action progressSave = null) => CreateHandler(
            () => current, stage => service.CommitPlayerResearchTransition(current, candidate =>
            {
                var result = stage(candidate);
                if (result.Succeeded && result.StateChanged)
                    MvpFirstSessionObjectiveCompletionApplier.ApplyIfComplete(candidate, ObjectiveConfig(), f.Production);
                return result;
            }), progressSave);

        internal static PlayerResearchActionHandler CreateHandler(Func<SaveData> read,
            Func<Func<SaveData, PlayerResearchActionResult>, PlayerResearchActionResult> commit, Action progressSave = null)
            => new PlayerResearchActionHandler(read(),
                new ResearchPendingScaffoldConfig { enabled = true, slotId = "slot", projectId = Project, ruleSourceId = "test.pending" },
                new ResearchProgressScaffoldConfig { enabled = true, progressPerActiveSecond = .1, maxActiveSessionElapsedSeconds = 100, ruleSourceId = "test.progress" },
                new ResearchCompletionEligibilityScaffoldConfig { enabled = true, projectId = Project, requiredProgressUnits = 1, ruleSourceId = "test.eligibility" },
                new ResearchCompletionClaimScaffoldConfig { enabled = true, ruleSourceId = "test.claim", claimAuthorityMode = PlayerResearchClaimAuthorityResolver.LocalMvpAuthorityMode },
                new ResearchVerificationScaffoldConfig { enabled = true, ruleSourceId = "test.verification", verificationMode = ResearchVerificationBoundaryResolver.UnavailableVerificationMode },
                Project, new RestrictedActionGateService(), () => read()?.runHistory?.LatestOutcome != null,
                () => true, () => false, progressSave, commit, read);

        private static RunSimulationConfig ObjectiveConfig() => new RunSimulationConfig
        {
            MvpFirstSessionObjective = new MvpFirstSessionObjectiveConfig { ObjectiveId = "test.objective", RequiredRecoveredLootValue = 1,
                RequireResearchAnalysisUnlocked = true, AnalysisResearchProjectId = Project }
        };

        internal static byte[][] AuthoredResearchInputs()
        {
            // The existing test coefficients/project are authored before the
            // current owner exists. Do not alter production assets or historical
            // serialization, and never hash JsonUtility's incomplete run config.
            return DungeonBuilder.M0.Tests.EditMode.PhaseFourTestSupport.AuthoredInputs((config, bootstrap) => {
            config.MvpFirstSessionObjective = ObjectiveConfig().MvpFirstSessionObjective;
            bootstrap.researchPendingScaffold = new ResearchPendingScaffoldConfig {
                enabled = true, slotId = "slot", projectId = Project, ruleSourceId = "test.pending" };
            bootstrap.researchProgressScaffold = new ResearchProgressScaffoldConfig {
                enabled = true, progressPerActiveSecond = .1, maxActiveSessionElapsedSeconds = 100, ruleSourceId = "test.progress" };
            bootstrap.researchCompletionEligibilityScaffold = new ResearchCompletionEligibilityScaffoldConfig {
                enabled = true, projectId = Project, requiredProgressUnits = 1, ruleSourceId = "test.eligibility" };
            bootstrap.researchCompletionClaimScaffold = new ResearchCompletionClaimScaffoldConfig {
                enabled = true, ruleSourceId = "test.claim", claimAuthorityMode = PlayerResearchClaimAuthorityResolver.LocalMvpAuthorityMode };
            bootstrap.researchVerificationScaffold = new ResearchVerificationScaffoldConfig {
                enabled = true, ruleSourceId = "test.verification", verificationMode = ResearchVerificationBoundaryResolver.UnavailableVerificationMode };
            });
        }

        private int Writes => f.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Write);
        private byte[] Bytes => f.FileSystem.ReadAllBytes(service.SavePath);
        private static string Json(object value) => JsonUtility.ToJson(value);
        private void Reopen()
        {
            service = OpenService(f, out current);
            service.CanonicalRuntimePublished += save => { current = save; publications++; };
        }
        private void Ready()
        {
            Assert.That(Handler().Start().Succeeded, Is.True);
            Assert.That(Handler().ApplyActiveTick(10).State, Is.EqualTo(PlayerResearchState.ReadyToClaim));
            Assert.That(service.Save(current, SaveReason.StateChange), Is.True);
            publications = 0;
        }

        [Test] public void StartProgressClaimReopen_PublishesOnlyDurableStateAndAtomicObjectiveUnlock()
        {
            var handler = Handler(); var prior = current;
            var started = handler.Start();
            Assert.That(started.Succeeded && started.StateChanged, Is.True);
            Assert.That(started.FeedbackLocalizationKey, Is.EqualTo(PlayerResearchActionHandler.StartSucceededKey));
            Assert.That(prior.researchPending, Is.Null, "Staging must not change the prior publication");
            Assert.That(current, Is.Not.SameAs(prior));
            Assert.That(handler.ResolveState().State, Is.EqualTo(PlayerResearchState.InProgress), "Retained handlers follow publication");
            int writes = Writes;
            Assert.That(handler.Start().Succeeded, Is.False); Assert.That(Writes, Is.EqualTo(writes));
            Reopen(); Assert.That(current.researchPending.ProjectId, Is.EqualTo(Project));
            handler = Handler(); handler.ApplyActiveTick(4);
            Assert.That(current.researchProgress.ProgressUnits, Is.EqualTo(.4));
            handler.ApplyActiveTick(6);
            prior = current;
            var claimed = handler.Claim();
            Assert.That(claimed.Succeeded && claimed.StateChanged, Is.True);
            Assert.That(claimed.FeedbackLocalizationKey, Is.EqualTo(PlayerResearchActionHandler.ClaimSucceededKey));
            Assert.That(prior.completedResearch.ProjectIds, Does.Not.Contain(Project));
            Assert.That(current.completedObjectives.ObjectiveIds, Is.EqualTo(new[] { "test.objective" }));
            Assert.That(MvpFirstSessionObjectivePresenter.Resolve(current, ObjectiveConfig(), f.Production).AnalysisUnlocked, Is.True);
            Assert.That(handler.ResolveState().State, Is.EqualTo(PlayerResearchState.Completed));
            writes = Writes;
            Assert.That(handler.Claim().Succeeded, Is.False); Assert.That(handler.Start().Succeeded, Is.False);
            Assert.That(Writes, Is.EqualTo(writes));
            Reopen(); Assert.That(current.researchPending, Is.Null); Assert.That(current.researchProgress, Is.Null);
            Assert.That(current.completedResearch.ProjectIds, Is.EqualTo(new[] { Project }));
            Assert.That(current.completedObjectives.ObjectiveIds, Is.EqualTo(new[] { "test.objective" }));
            string durable = Encoding.UTF8.GetString(Bytes);
            Assert.That(durable, Does.Contain("\"unrelatedResearchEvidence\":{\"n\":1.00}"));
            Assert.That(durable, Does.Contain("\"unrelatedRoot\":[true,null]"));
            Assert.That(CanonicalSaveSchemaVersions.CurrentWritableTarget, Is.EqualTo(CanonicalSaveSchemaVersions.AtomicRecoveryIntroduction));
        }

        [TestCase(false, Faults.OperationType.Write, 1)]
        [TestCase(false, Faults.OperationType.Write, 2)]
        [TestCase(false, Faults.OperationType.Replace, 1)]
        [TestCase(false, Faults.OperationType.Flush, 1)]
        [TestCase(true, Faults.OperationType.Write, 2)]
        [TestCase(true, Faults.OperationType.Replace, 1)]
        [TestCase(true, Faults.OperationType.Flush, 1)]
        public void ProvenRollback_NoLiveMutationNoSuccessAndSafeExplicitRetry(bool claim, Faults.OperationType operation, int occurrence)
        {
            if (claim) Ready();
            var prior = current; var session = service.CanonicalSession;
            string before = Json(current); byte[] durable = Bytes;
            f.FileSystem.EnableTargetedFailure(operation, paths => true, occurrence, false);
            var result = claim ? Handler().Claim() : Handler().Start();
            Assert.That(result.Succeeded || result.StateChanged, Is.False);
            Assert.That(result.PersistenceReason, Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            Assert.That(result.RecoveryRequired, Is.False);
            Assert.That(result.FeedbackLocalizationKey, Is.EqualTo(PlayerResearchActionHandler.PersistenceFailedKey));
            Assert.That(publications, Is.Zero); Assert.That(current, Is.SameAs(prior));
            Assert.That(service.CanonicalSession, Is.SameAs(session)); Assert.That(Json(current), Is.EqualTo(before));
            CollectionAssert.AreEqual(durable, Bytes);
            f.FileSystem.DisableFailure(); Reopen();
            Assert.That(claim ? Handler().Claim().Succeeded : Handler().Start().Succeeded, Is.True);
            Assert.That(claim ? Handler().Claim().Succeeded : Handler().Start().Succeeded, Is.False);
        }

        [TestCase(false)] [TestCase(true)]
        public void PostReplacementReadbackFailure_RestoresCompletePredecessor(bool claim)
        {
            if (claim) Ready();
            byte[] before = Bytes; string live = Json(current);
            // Active reads: initial predecessor, settled evidence check, post-replace readback.
            f.FileSystem.EnableTargetedFailure(Faults.OperationType.Read,
                paths => paths[0] == service.SavePath, 3, false);
            var result = claim ? Handler().Claim() : Handler().Start();
            Assert.That(result.PersistenceReason, Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            Assert.That(result.Succeeded, Is.False); Assert.That(publications, Is.Zero);
            Assert.That(Json(current), Is.EqualTo(live)); CollectionAssert.AreEqual(before, Bytes);
            f.FileSystem.DisableFailure(); Reopen();
            Assert.That(current.completedResearch.ProjectIds, Does.Not.Contain(Project));
        }

        [TestCase(false)] [TestCase(true)]
        public void UncertainRollback_LatchesAuthorityAndRequiresReopenWithoutAutomaticRetry(bool claim)
        {
            if (claim) Ready();
            var original = current; string before = Json(current);
            int replaces = f.FileSystem.Operations.Count(o => o.Type == Faults.OperationType.Replace);
            f.FileSystem.EnableFailureSequence(Faults.OperationType.Flush, 1, Faults.OperationType.Replace, replaces + 2);
            var result = claim ? Handler().Claim() : Handler().Start();
            Assert.That(result.Succeeded || result.StateChanged, Is.False);
            Assert.That(result.RecoveryRequired, Is.True);
            Assert.That(result.FeedbackLocalizationKey, Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
            Assert.That(current, Is.SameAs(original)); Assert.That(Json(current), Is.EqualTo(before));
            Assert.That(publications, Is.Zero);
            f.FileSystem.DisableFailure(); int operations = f.FileSystem.Operations.Count();
            Assert.That(Handler().Start().RecoveryRequired, Is.True); Assert.That(Handler().Claim().RecoveryRequired, Is.True);
            Assert.That(service.Save(current, SaveReason.StateChange), Is.False);
            Assert.That(f.FileSystem.Operations.Count(), Is.EqualTo(operations), "Uncertain authority cannot retry storage");
            Reopen(); // A fresh coordinator validates the surviving active candidate and settles its evidence.
            Assert.That(claim ? current.completedResearch.ProjectIds.Contains(Project) : current.researchPending?.ProjectId == Project, Is.True);
            int writes = Writes;
            Assert.That(claim ? Handler().Claim().Succeeded : Handler().Start().Succeeded, Is.False);
            Assert.That(Writes, Is.EqualTo(writes));
        }

        [Test] public void CandidatePreparationFailureAndRejectedActions_DoNotWriteOrPublish()
        {
            int writes = Writes; string before = Json(current);
            var rejected = Handler().Claim(); Assert.That(rejected.Succeeded, Is.False);
            var failed = service.CommitPlayerResearchTransition(current, candidate => { candidate.completedObjectives.ObjectiveIds = new[] { "test.uncommitted" }; throw new InvalidOperationException("test.stage"); });
            Assert.That(failed.Succeeded, Is.False); Assert.That(current.completedObjectives, Is.Not.Null);
            Assert.That(Json(current), Is.EqualTo(before));
            Assert.That(Writes, Is.EqualTo(writes)); Assert.That(publications, Is.Zero);
        }

        [Test] public void SemanticallyInvalidCandidate_IsRejectedBeforeAnyWrite()
        {
            int writes = Writes; string before = Json(current);
            var result = service.CommitPlayerResearchTransition(current, candidate =>
            {
                candidate.structureRuntime.ManaReserve = double.NaN;
                return new PlayerResearchActionResult { Succeeded = true, StateChanged = true };
            });
            Assert.That(result.Succeeded || result.StateChanged, Is.False);
            Assert.That(Json(current), Is.EqualTo(before)); Assert.That(Writes, Is.EqualTo(writes));
            Assert.That(publications, Is.Zero);
        }

        [Test] public void UnconfiguredLegacyService_RejectsResearchWithoutChangingLegacySaveCompatibility()
        {
            string directory = Path.Combine(Path.GetTempPath(), "r0-legacy-" + Guid.NewGuid().ToString("N"));
            var legacy = new SaveService(new SimpleLogger(false, (level, message) => { }), new SaveConfig { fileName = "legacy.json", useAtomicWrites = true }, directory);
            try
            {
                var save = legacy.LoadOrCreate("r0-legacy", out _);
                bool staged = false;
                var result = legacy.CommitPlayerResearchTransition(save, candidate => { staged = true; return null; });
                Assert.That(result.Succeeded, Is.False); Assert.That(staged, Is.False);
                Assert.That(File.Exists(legacy.SavePath), Is.False);
                Assert.That(legacy.Save(save, SaveReason.StateChange), Is.True);
                Assert.That(legacy.LoadOrCreate("r0-legacy", out _), Is.Not.Null);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test] public void ForegroundPreparationReplacesStateBeforeValidationAndStaging()
        {
            var stale = current; int preparations = 0;
            service.BeforeForegroundAction = () =>
            {
                preparations++;
                // Simulate a successful preceding foreground publication through the actual writer.
                service.BeforeForegroundAction = null;
                current.totalTicks++;
                bool saved = service.Save(current, SaveReason.StateChange);
                return saved;
            };
            service.ForegroundCurrent = () => current;
            Assert.That(Handler().Start().Succeeded, Is.True);
            Assert.That(preparations, Is.EqualTo(1)); Assert.That(current, Is.Not.SameAs(stale));
            Assert.That(current.totalTicks, Is.EqualTo(stale.totalTicks));
            Reopen(); Assert.That(current.researchPending.ProjectId, Is.EqualTo(Project));
        }

        [Test] public void ForegroundFailureBlocksResearchBeforeStaging()
        {
            int writes = Writes; string before = Json(current);
            service.BeforeForegroundAction = () => false;
            Assert.That(Handler().Start().Succeeded, Is.False);
            Assert.That(Json(current), Is.EqualTo(before)); Assert.That(Writes, Is.EqualTo(writes));
            Assert.That(publications, Is.Zero);
        }

        [Test] public void DetachedProgression_RemainsCallbackFreeAndDoesNotWritePerTick()
        {
            Assert.That(Handler().Start().Succeeded, Is.True);
            int writes = Writes;
            var detached = JsonUtility.FromJson<SaveData>(Json(current));
            var research = CreateHandler(() => detached, null);
            Assert.That(research.ApplyActiveTick(4).ProgressUnits, Is.EqualTo(.4));
            Assert.That(research.ApplyActiveTick(6).State, Is.EqualTo(PlayerResearchState.ReadyToClaim));
            Assert.That(Writes, Is.EqualTo(writes)); Assert.That(current.researchProgress.ProgressUnits, Is.Zero);
            Assert.That(research.Claim().Succeeded, Is.False, "A detached tick handler cannot acknowledge player actions");
        }
    }
}
#endif
