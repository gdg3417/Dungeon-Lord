#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests
{
    public abstract partial class PhaseSevenA4ProductionSceneTests
    {
        private BootstrapOverlay ResearchOverlay()
        {
            var root = GameRoot.Instance;
            var overlay = root.overlay;
            Assert.That(overlay, Is.Not.Null);
            typeof(ProductionDungeonController).GetMethod("SetLegacy", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, new object[] { true });
            overlay.Bind(root);
            return overlay;
        }
        [UnityTest] public IEnumerator R0ResearchPrerequisiteNativeShellBlocksWithoutResearchWrite()
        {
            var root = GameRoot.Instance; var service = root.SaveService;
            InstallForegroundBoundaryTestClock();
            Assert.That(root.ClearResearchPendingScaffold(), Is.True);
            var reopened = service.LoadOrCreate("r0-prerequisite", out string banner);
            Assert.That(reopened, Is.Not.Null, banner);
            typeof(GameRoot).GetMethod("PublishCanonicalRuntime", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(root, new object[] { reopened });
            Assert.That(root.Save.runHistory.NextRunSequence, Is.EqualTo(1));
            Assert.That(root.Save.runHistory.RecentOutcomes, Is.Empty);
            Assert.That(string.IsNullOrWhiteSpace(root.Save.runHistory.LatestOutcome?.RunId), Is.True);
            Assert.That(service.BeforeForegroundAction(), Is.True, "Close separately authorized foreground preparation before measuring research writes");
            var overlay = ResearchOverlay();
            Assert.That(root.ResolvePlayerResearchState().State, Is.EqualTo(PlayerResearchState.Blocked));
            Assert.That(overlay.ResolvePlayerResearchPanelPresentation().ShowAction, Is.False);
            var bytes = service.CanonicalSession.GetCurrentBytes(); var session = service.CanonicalSession;
            var original = root.Save; string state = UnityEngine.JsonUtility.ToJson(root.Save);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var field = typeof(SaveService).GetField("_canonicalFileSystem", flags); var native = field.GetValue(service);
            var fs = new EditMode.Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem(); fs.Seed(service.SavePath, bytes);
            field.SetValue(service, fs);
            int publications = 0; service.CanonicalRuntimePublished += save => publications++;
            try
            {
                var result = overlay.StartPlayerResearch();
                Assert.That(result.Succeeded || result.StateChanged, Is.False);
                Assert.That(result.FeedbackLocalizationKey, Is.EqualTo(PlayerResearchActionHandler.BlockedPrerequisiteKey));
                Assert.That(root.Save.researchPending, Is.Null); Assert.That(root.Save.researchProgress, Is.Null);
                Assert.That(root.ResolveMvpPlayerLoopSummary().HasResearchUnlock, Is.False);
                Assert.That(root.Save.completedResearch?.ProjectIds ?? Array.Empty<string>(), Is.Empty);
                Assert.That(UnityEngine.JsonUtility.ToJson(root.Save), Is.EqualTo(state));
                Assert.That(root.Save, Is.SameAs(original)); Assert.That(service.CanonicalSession, Is.SameAs(session));
                Assert.That(publications, Is.Zero);
                Assert.That(fs.Operations.Any(op => op.Type == EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write ||
                    op.Type == EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace), Is.False);
                CollectionAssert.AreEqual(bytes, fs.ReadAllBytes(service.SavePath));
            }
            finally { field.SetValue(service, native); }
            yield return null;
        }

        [UnityTest] public IEnumerator R0ResearchPrerequisiteRequiresAutomaticTerminalSettlement()
        {
            var root = GameRoot.Instance; var service = root.SaveService;
            var advance = InstallForegroundBoundaryTestClock();
            Assert.That(root.ClearResearchPendingScaffold(), Is.True);
            Assert.That(root.TrySetQaManaFromDevPanel(true), Is.True);
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0];
            Assert.That(service.ExecuteCanonicalMutation(root.Save, DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId,
                floor.Layout.Rooms[0].RoomInstanceId, floor.FloorInstanceId, new TileCoordinate(0, 1))).IsSuccess, Is.True);
            var update = (Action)Delegate.CreateDelegate(typeof(Action), root, typeof(GameRoot).GetMethod("UpdateForeground", BindingFlags.Instance | BindingFlags.NonPublic));
            update();
            Assert.That(root.ResolvePlayerResearchState().State, Is.EqualTo(PlayerResearchState.Blocked));
            long due = root.Save.livingDungeon.Scheduler.NextOpportunityTime.Value;
            advance((due - root.TimeService.SampleForeground()) / 1000d + .001); update();
            Assert.That(root.Save.livingDungeon.Parties.Count, Is.EqualTo(1));
            Assert.That(root.Save.runHistory.RecentOutcomes, Is.Empty);
            Assert.That(root.ResolvePlayerResearchState().State, Is.EqualTo(PlayerResearchState.Blocked));
            var overlay = ResearchOverlay(); Assert.That(overlay.ResolvePlayerResearchPanelPresentation().ShowAction, Is.False);
            Assert.That(service.BeforeForegroundAction(), Is.True);
            var session = service.CanonicalSession;
            var rejected = overlay.StartPlayerResearch();
            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(rejected.FeedbackLocalizationKey, Is.EqualTo(PlayerResearchActionHandler.BlockedPrerequisiteKey));
            Assert.That(service.CanonicalSession, Is.SameAs(session), "No extra research write after foreground barrier");
            var party = root.Save.livingDungeon.Parties.Single();
            var profile = service.ForegroundConfiguration.CreateProfile(root.SaveSpatialMigrationLimits.Canonical.ForSchema(13).Serialized);
            var execution = LivingDungeonContracts.Restore(party, profile); long finish;
            try { execution.DrainToCompletion(); finish = party.AdmittedAt + execution.Inspect().LogicalTime; }
            finally { execution.Abandon(); }
            advance((finish - root.TimeService.SampleForeground()) / 1000d + .001); update();
            Assert.That(service.BeforeForegroundAction(), Is.True);
            Assert.That(root.Save.runHistory.RecentOutcomes.Any(o => o.RunId == "run-1"), Is.True);
            Assert.That(root.ResolvePlayerResearchState().State, Is.EqualTo(PlayerResearchState.Available));
            Assert.That(overlay.ResolvePlayerResearchPanelPresentation().ShowAction, Is.True);
            Assert.That(overlay.StartPlayerResearch().Succeeded, Is.True);
            Assert.That(overlay.StartPlayerResearch().Succeeded, Is.False);
            advance(root.Content.Bootstrap.tickSeconds); update();
            Assert.That(service.BeforeForegroundAction(), Is.True);
            Assert.That(root.Save.researchProgress.ProgressUnits, Is.GreaterThan(0), "Detached foreground callback still progresses research");
            yield return null;
        }

        [UnityTest] public IEnumerator R0ResearchStartProgressClaimAndReopenUseCommittedPublication()
        {
            var root = GameRoot.Instance;
            Assert.That(root.ClearResearchPendingScaffold(), Is.True, "Seed explicit absent research through the existing development control");
            Assert.That(root.SimulateRunOnce(RunPostureResolver.BalancedId), Is.True);
            var advance = InstallForegroundBoundaryTestClock();
            var overlay = ResearchOverlay();
            var before = root.Save;
            var start = overlay.StartPlayerResearch();
            Assert.That(start.Succeeded, Is.True, start.PersistenceReason ?? start.FeedbackLocalizationKey);
            Assert.That(root.Save, Is.Not.SameAs(before)); Assert.That(before.researchPending, Is.Null);
            Assert.That(root.BannerMessage, Is.EqualTo(root.Content.GetString(PlayerResearchActionHandler.StartSucceededKey, null)));
            Assert.That(overlay.StartPlayerResearch().Succeeded, Is.False);
            var reopened = root.SaveService.LoadOrCreate("r0-scene", out string banner);
            Assert.That(reopened, Is.Not.Null, banner);
            typeof(GameRoot).GetMethod("PublishCanonicalRuntime", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(root, new object[] { reopened });
            Assert.That(root.ResolvePlayerResearchState().State, Is.EqualTo(PlayerResearchState.InProgress));
            before = root.Save;
            int published = 0; root.SaveService.CanonicalRuntimePublished += save => published++;
            var bootstrap = root.Content.Bootstrap;
            advance(bootstrap.researchCompletionEligibilityScaffold.requiredProgressUnits / bootstrap.researchProgressScaffold.progressPerActiveSecond + bootstrap.tickSeconds);
            typeof(GameRoot).GetMethod("UpdateForeground", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(root, null);
            Assert.That(root.ResolvePlayerResearchState().State, Is.EqualTo(PlayerResearchState.ReadyToClaim));
            Assert.That(before.researchProgress.ProgressUnits, Is.Zero);
            Assert.That(published, Is.EqualTo(1), "Detached passive ticks are included in one foreground publication");
            var claim = overlay.ClaimPlayerResearch();
            Assert.That(claim.Succeeded, Is.True, claim.PersistenceReason ?? claim.FeedbackLocalizationKey);
            Assert.That(root.BannerMessage, Is.EqualTo(root.Content.GetString(PlayerResearchActionHandler.ClaimSucceededKey, null)));
            Assert.That(root.ResolveMvpPlayerLoopSummary().HasResearchUnlock, Is.True);
            Assert.That(overlay.ClaimPlayerResearch().Succeeded, Is.False);
            reopened = root.SaveService.LoadOrCreate("r0-scene", out banner);
            Assert.That(reopened, Is.Not.Null, banner);
            typeof(GameRoot).GetMethod("PublishCanonicalRuntime", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(root, new object[] { reopened });
            Assert.That(root.ResolvePlayerResearchState().State, Is.EqualTo(PlayerResearchState.Completed));
            Assert.That(root.Save.researchPending, Is.Null); Assert.That(root.Save.researchProgress, Is.Null);
            yield return null;
        }

        [UnityTest] public IEnumerator R0ResearchStartProvenRollback() => ResearchActionFailure(false, false);
        [UnityTest] public IEnumerator R0ResearchClaimProvenRollback() => ResearchActionFailure(true, false);
        [UnityTest] public IEnumerator R0ResearchStartUncertainRecovery() => ResearchActionFailure(false, true);
        [UnityTest] public IEnumerator R0ResearchClaimUncertainRecovery() => ResearchActionFailure(true, true);
        [UnityTest] public IEnumerator R0ResearchForegroundRollbackBlocksAction() => ResearchActionFailure(false, false, true);
        private IEnumerator ResearchActionFailure(bool claim, bool uncertain, bool failForeground = false)
        {
            var root = GameRoot.Instance; var service = root.SaveService;
            Assert.That(root.ClearResearchPendingScaffold(), Is.True);
            Assert.That(root.SimulateRunOnce(RunPostureResolver.BalancedId), Is.True);
            var advance = InstallForegroundBoundaryTestClock();
            if (claim)
            {
                var start = root.StartConfiguredPlayerResearch();
                Assert.That(start.Succeeded, Is.True, UnityEngine.JsonUtility.ToJson(start));
                var bootstrap = root.Content.Bootstrap;
                root.ApplyConfiguredPlayerResearchActiveTick((long)Math.Ceiling(
                    bootstrap.researchCompletionEligibilityScaffold.requiredProgressUnits / bootstrap.researchProgressScaffold.progressPerActiveSecond));
            }
            Assert.That(service.BeforeForegroundAction(), Is.True);
            var original = root.Save; var session = service.CanonicalSession;
            string state = UnityEngine.JsonUtility.ToJson(original); byte[] bytes = session.GetCurrentBytes();
            var fsField = typeof(SaveService).GetField("_canonicalFileSystem", BindingFlags.Instance | BindingFlags.NonPublic);
            var native = fsField.GetValue(service);
            var fs = new EditMode.Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            fs.Seed(service.SavePath, bytes); fsField.SetValue(service, fs);
            if (uncertain) fs.EnableFailureSequence(EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush, 1,
                EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 2);
            else fs.EnableFailure(EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush, 1);
            if (failForeground) advance(root.Content.Bootstrap.tickSeconds);
            int published = 0; service.CanonicalRuntimePublished += save => published++;
            try
            {
                var overlay = ResearchOverlay();
                var result = claim ? overlay.ClaimPlayerResearch() : overlay.StartPlayerResearch();
                Assert.That(result.Succeeded || result.StateChanged, Is.False);
                Assert.That(result.RecoveryRequired, Is.EqualTo(uncertain));
                Assert.That(result.PersistenceReason, Is.EqualTo(uncertain ? DetachedCanonicalWriteAuthority.RecoveryRequiredReason :
                    failForeground ? "living.foreground.boundary_pending" : DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
                if (failForeground) Assert.That(fs.Operations.Count(o => o.Type == EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write), Is.EqualTo(2),
                    "Only the failed foreground protocol writes; research is never staged/persisted");
                Assert.That(root.BannerMessage, Is.EqualTo(root.Content.GetString(result.FeedbackLocalizationKey, null)));
                Assert.That(root.Save, Is.SameAs(original)); Assert.That(UnityEngine.JsonUtility.ToJson(root.Save), Is.EqualTo(state));
                Assert.That(published, Is.Zero);
                fs.DisableFailure(); int operations = fs.Operations.Count();
                if (uncertain)
                {
                    Assert.That(root.StartConfiguredPlayerResearch().RecoveryRequired, Is.True);
                    Assert.That(root.ClaimConfiguredPlayerResearch().RecoveryRequired, Is.True);
                    typeof(GameRoot).GetMethod("UpdateForeground", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(root, null);
                    Assert.That(fs.Operations.Count(), Is.EqualTo(operations));
                    Assert.That(root.ForegroundActivity.StateKey, Is.EqualTo("ui.living.blocked"));
                    Assert.That(root.ResolvePlayerResearchState().RecoveryRequired, Is.True);
                }
                else
                {
                    CollectionAssert.AreEqual(bytes, fs.ReadAllBytes(service.SavePath));
                    Assert.That(claim ? root.ClaimConfiguredPlayerResearch().Succeeded : root.StartConfiguredPlayerResearch().Succeeded, Is.True);
                    Assert.That(claim ? root.ClaimConfiguredPlayerResearch().Succeeded : root.StartConfiguredPlayerResearch().Succeeded, Is.False);
                }
            }
            finally
            {
                fsField.SetValue(service, native);
                typeof(SaveService).GetField("_canonicalSession", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(service, session);
                typeof(GameRoot).GetProperty(nameof(GameRoot.Save)).SetValue(root, original);
                typeof(GameRoot).GetField("_explicitSaveDeleteQuiesced", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(root, true);
            }
            yield return null;
        }

        [UnityTest, Timeout(1800000)] public IEnumerator ForegroundPerformanceSustainedProduction68Runs()
        {
            var root = GameRoot.Instance;
            foreach (var assignment in root.Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments.ToArray())
                Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save,
                    DetachedCanonicalMutationRequest.Unassign(assignment.AssignmentId)).IsSuccess, Is.True);
            var report = new GameRoot.ForegroundProfileReport();
            var operation = root.ProfileForeground(report);
            try {
                while (true) {
                    bool more; object next = null;
                    try { more = operation.MoveNext(); if (more) next = operation.Current; }
                    catch (Exception error) { report.Failure = error.ToString(); break; }
                    if (!more) break;
                    yield return next;
                }
            }
            finally {
                string path = "TestResults/foreground-profile-" + Guid.NewGuid().ToString("N") + ".json";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(stream)) writer.Write(UnityEngine.JsonUtility.ToJson(report, true));
                TestContext.WriteLine(path);
            }
            Assert.That(report.Passed, Is.True, report.Failure);
        }
        private Action<double> InstallForegroundBoundaryTestClock()
        {
            var root = GameRoot.Instance;
            Assert.That(sceneClock, Is.Not.Null);
            Assert.That(root.TimeService, Is.SameAs(typeof(SaveService).GetField("recoveryTime",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root.SaveService)));
            TraceSceneState("clock.verify-prebound-owner");
            return sceneClock.Advance;
        }

        [UnityTest] public IEnumerator LivingDungeonB3B2ProvenRollbackRetriesThroughRealSaveBarrier()
            => ForegroundFailureRetry(false);

        [UnityTest] public IEnumerator LivingDungeonB3B2UncertainRollbackKeepsRealRootRecoveryLatch()
            => ForegroundFailureRetry(true);

        private IEnumerator ForegroundFailureRetry(bool uncertain)
        {
            var root = GameRoot.Instance; var service = root.SaveService;
            Assert.That(root.TrySetQaManaFromDevPanel(true), Is.True);
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0];
            Assert.That(service.ExecuteCanonicalMutation(root.Save, DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId,
                floor.Layout.Rooms[0].RoomInstanceId, floor.FloorInstanceId, new TileCoordinate(1, 1))).IsSuccess, Is.True);
            var advance = InstallForegroundBoundaryTestClock();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var update = (Action)Delegate.CreateDelegate(typeof(Action), root, typeof(GameRoot).GetMethod("UpdateForeground", flags));
            update();
            advance((root.Save.livingDungeon.Scheduler.NextOpportunityTime.Value - root.TimeService.SampleForeground()) / 1000d + .001);
            update(); PumpSceneToClosed(); Assert.That(root.Save.livingDungeon.Parties.Count, Is.EqualTo(1));
            var profile = service.ForegroundConfiguration.CreateProfile(root.SaveSpatialMigrationLimits.Canonical.ForSchema(13).Serialized);
            var execution = LivingDungeonContracts.Restore(root.Save.livingDungeon.Parties.Single(), profile);
            long finish; RunOutcomeRecord expected;
            try {
                expected = execution.DrainToCompletion();
                finish = root.Save.livingDungeon.Parties.Single().AdmittedAt + execution.Inspect().LogicalTime;
            } finally { execution.Abandon(); }
            Assert.That(service.ForegroundConfiguration.MaximumBoundaries, Is.EqualTo(1), "Bounded policy was consumed before ownership");
            advance((finish - root.TimeService.SampleForeground()) / 1000d + .001); update();
            Assert.That(root.Save.livingDungeon.IsClosed, Is.False, "Accept a horizon that needs several bounded passes");
            var original = root.Save; var session = service.CanonicalSession; var before = session.GetCurrentBytes();
            string state = EditMode.LivingDungeonPhaseABaselineTests.Structured(root.Save);
            var fsField = typeof(SaveService).GetField("_canonicalFileSystem", flags);
            var native = fsField.GetValue(service);
            var fs = new EditMode.Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            fs.Seed(service.SavePath, before); fsField.SetValue(service, fs);
            int published = 0; service.CanonicalRuntimePublished += save => published++;
            var flush = EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush;
            if (uncertain) fs.EnableFailureSequence(flush, 1, EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 2);
            else fs.EnableFailure(flush, 1);
            try {
                update(); Assert.That(published, Is.Zero);
                Assert.That(root.Save, Is.SameAs(original)); Assert.That(service.CanonicalSession, Is.SameAs(session));
                Assert.That(EditMode.LivingDungeonPhaseABaselineTests.Structured(root.Save), Is.EqualTo(state));
                if (!uncertain) CollectionAssert.AreEqual(before, fs.ReadAllBytes(service.SavePath));
                int operations = fs.Operations.Count();
                for (int frame = 0; frame < 120; frame++) update();
                Assert.That(fs.Operations.Count(), Is.EqualTo(operations), "Do not busy-retry storage each render frame");
                if (uncertain) {
                    fs.DisableFailure(); Assert.That(service.BeforeForegroundAction(), Is.False);
                    Assert.That(typeof(GameRoot).GetField("foregroundFailure", flags).GetValue(root),
                        Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
                    Assert.That(service.AdvanceForeground(root.Save, finish, 10, (save, tick) => { }).Reason,
                        Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
                    Assert.That(published, Is.Zero);
                } else {
                    fs.EnableTargetedFailure(flush, paths => true, 1, false);
                    Assert.That(service.BeforeForegroundAction(), Is.False);
                    Assert.That(fs.Operations.Count(), Is.GreaterThan(operations), "An explicit barrier must attempt a safe retry");
                    Assert.That(root.ForegroundActivity.StateKey, Is.EqualTo("ui.living.retryable"));
                    Assert.That(published, Is.Zero); CollectionAssert.AreEqual(before, fs.ReadAllBytes(service.SavePath));
                    fs.DisableFailure(); bool closed = false;
                    for (int attempt = 0; attempt < 32 && !closed; attempt++) closed = service.BeforeForegroundAction();
                    Assert.That(closed, Is.True, "Proven rollback must resume without restarting");
                    Assert.That(typeof(GameRoot).GetField("foregroundFailure", flags).GetValue(root), Is.Null);
                    Assert.That(root.Save.livingDungeon.Parties, Is.Empty);
                    Assert.That(root.Save.runHistory.RecentOutcomes.Count(o => o.RunId == "run-1"), Is.EqualTo(1));
                    Assert.That(root.Save.runHistory.NextRunSequence, Is.EqualTo(2));
                    Assert.That(EditMode.LivingDungeonPhaseABaselineTests.Structured(CombatC0Diagnostics.Events(root.Save.runHistory.LatestOutcome)),
                        Is.EqualTo(EditMode.LivingDungeonPhaseABaselineTests.Structured(CombatC0Diagnostics.Events(expected))));
                    long ticks = root.Save.totalTicks; double mana = root.Save.structureRuntime.ManaReserve;
                    var monsters = EditMode.LivingDungeonPhaseABaselineTests.Structured(root.Save.monsterContinuity);
                    Assert.That(service.Save(root.Save, SaveReason.ManualDev), Is.True, "Ordinary saving remains usable after retry");
                    Assert.That(root.Save.totalTicks, Is.EqualTo(ticks)); Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(mana));
                    Assert.That(EditMode.LivingDungeonPhaseABaselineTests.Structured(root.Save.monsterContinuity), Is.EqualTo(monsters));
                    Assert.That(root.Save.runHistory.RecentOutcomes.Count(o => o.RunId == "run-1"), Is.EqualTo(1));
                }
            } finally {
                // Restore the disposable native fixture; do not flush the in-memory
                // failure experiment during object teardown.
                fsField.SetValue(service, native);
                typeof(SaveService).GetField("_canonicalSession", flags).SetValue(service, session);
                typeof(GameRoot).GetProperty(nameof(GameRoot.Save)).SetValue(root, original);
                typeof(GameRoot).GetField("_explicitSaveDeleteQuiesced", flags).SetValue(root, true);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator LivingDungeonB3B2AutomaticSceneLoopSettlesAndAdmitsAgainstCommittedEdit()
        {
            var root = GameRoot.Instance;
            Assert.That(root.TrySetQaManaFromDevPanel(true), Is.True);
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0];
            var room = floor.Layout.Rooms[0];
            var placed = root.SaveService.ExecuteCanonicalMutation(root.Save,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId,
                    room.RoomInstanceId, floor.FloorInstanceId, new TileCoordinate(1, 1)));
            Assert.That(placed.IsSuccess, Is.True, placed.Reason);
            var advance = InstallForegroundBoundaryTestClock();
            var update = (Action)Delegate.CreateDelegate(typeof(Action), root,
                typeof(GameRoot).GetMethod("UpdateForeground", BindingFlags.Instance | BindingFlags.NonPublic));
            var profile = root.SaveService.ForegroundConfiguration.CreateProfile(root.SaveSpatialMigrationLimits.Canonical.ForSchema(13).Serialized);
            string oldGeometry = null; RunOutcomeRecord expected = null; bool edited = false;
            int maximumBytes = 0; long replay = 0;
            for (int frame = 0; frame < 1600; frame++) {
                root.TimeService.SetForegroundFocus(true); advance(.25); update(); yield return null;
                Assert.That(typeof(GameRoot).GetField("foregroundFailure", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root), Is.Null);
                if (!edited && root.Save.livingDungeon.Parties.Count == 1 && controller.World.ForegroundPartyVisible) {
                    var party = root.Save.livingDungeon.Parties.Single();
                    var bytes = root.SaveService.CanonicalSession.GetCurrentBytes();
                    var execution = LivingDungeonContracts.Restore(party, profile);
                    try {
                        oldGeometry = execution.Snapshot.Floors[0].KnowledgeFingerprint;
                        while (!execution.IsTerminal) {
                            Assert.That(execution.NextOrdinal, Is.LessThan(profile.MaximumPartyReplay));
                            execution.Advance(1, execution.NextOrdinal);
                            maximumBytes = Math.Max(maximumBytes, execution.ExportCheckpoint(profile.Checkpoints).GetBytes().Length);
                        }
                        replay = execution.NextOrdinal; expected = execution.TakeOutcome();
                    } finally { execution.Abandon(); }
                    CollectionAssert.AreEqual(bytes, root.SaveService.CanonicalSession.GetCurrentBytes(), "Private reconstruction cannot publish");
                    Assert.That(CombatC0Diagnostics.Events(expected).Count(e => e.Kind == CombatC0EventKind.Attack), Is.GreaterThan(0));
                    controller.EnterEdit(); yield return null;
                    Assert.That(controller.World.ForegroundPartyVisible, Is.False);
                    var monster = root.Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments.Single(a => a.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId);
                    var checkpoint = (byte[])party.Checkpoint.Clone();
                    Assert.That(controller.Draft.Move(floor.FloorInstanceId, monster.RoomInstanceId, monster.AssignmentId, new TileCoordinate(3, 2)), Is.True);
                    while (controller.Draft.Durability == DraftDurability.Pending) { Assert.That(controller.Draft.FlushNext(), Is.True); yield return null; }
                    Assert.That(controller.Commit(), Is.True);
                    CollectionAssert.AreEqual(checkpoint, root.Save.livingDungeon.Parties.Single().Checkpoint);
                    edited = true;
                }
                if (edited && root.Save.runHistory.LatestOutcome?.RunId == "run-1" && root.Save.livingDungeon.Parties.Any(p => p.Sequence == 2)) {
                    Assert.That(root.Save.runHistory.RecentOutcomes.Count(o => o.RunId == "run-1"), Is.EqualTo(1));
                    Assert.That(EditMode.LivingDungeonPhaseABaselineTests.Structured(CombatC0Diagnostics.Events(root.Save.runHistory.LatestOutcome)),
                        Is.EqualTo(EditMode.LivingDungeonPhaseABaselineTests.Structured(CombatC0Diagnostics.Events(expected))));
                    Assert.That(CombatC0Diagnostics.ChargedWork(root.Save.runHistory.LatestOutcome), Is.EqualTo(CombatC0Diagnostics.ChargedWork(expected)));
                    var second = LivingDungeonContracts.Restore(root.Save.livingDungeon.Parties.Single(), profile);
                    try { Assert.That(second.Snapshot.Floors[0].KnowledgeFingerprint, Is.Not.EqualTo(oldGeometry)); }
                    finally { second.Abandon(); }
                    TestContext.WriteLine("Loaded production config: checkpointBytes=" + maximumBytes + " replay=" + replay +
                        " combatWork=" + CombatC0Diagnostics.ChargedWork(expected) + " receipts=" + CombatC0Diagnostics.Events(expected).Count);
                    yield break;
                }
            }
            Assert.Fail("Automatic scene loop did not settle and capture the edited second run within its bounded test horizon");
        }

        [UnityTest]
        public IEnumerator LivingDungeonB3B2ResumeCompletesSeveralBoundedPassesBeforeRunningAgain()
        {
            var root = GameRoot.Instance;
            Assert.That(root.SaveService.ForegroundConfiguration.MaximumBoundaries, Is.EqualTo(1));
            var advance = InstallForegroundBoundaryTestClock();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var update = (Action)Delegate.CreateDelegate(typeof(Action), root, typeof(GameRoot).GetMethod("UpdateForeground", flags));
            long before = root.Save.livingDungeon.Scheduler.AcceptedThrough, ticks = root.Save.totalTicks;
            advance(35); root.ApplyPauseState(true); root.ApplyPauseState(false);
            // Schema 17 observes the return immediately; foreground eligibility
            // remains blocked by accepted work and the durable recovery barrier.
            Assert.That(root.TimeService.ForegroundPresentationActive, Is.False);
            Assert.That(root.TimeService.HasDeferredRecovery || root.TimeService.RecoveryClockActive, Is.True);
            PumpSceneToClosed();
            Assert.That(typeof(GameRoot).GetField("foregroundFailure", flags).GetValue(root), Is.Null);
            Assert.That(root.TimeService.IsPaused, Is.False);
            Assert.That(root.Save.livingDungeon.IsClosed, Is.True);
            Assert.That(root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(before + 35000));
            Assert.That(root.Save.totalTicks - ticks, Is.EqualTo((before + 35000) / 10000 - before / 10000));
            long closed = root.Save.livingDungeon.Scheduler.ClosedThrough;
            ticks = root.Save.totalTicks; double mana = root.Save.structureRuntime.ManaReserve;
            for (int cycle = 0; cycle < 3; cycle++) { root.ApplyPauseState(true); root.ApplyPauseState(false); update(); }
            Assert.That(root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(closed));
            Assert.That(root.Save.totalTicks, Is.EqualTo(ticks));
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(mana));
            yield return null;
        }

        [UnityTest]
        public IEnumerator LivingDungeonB3B2FramesBeforeCachedBoundaryDoNotReconstructOrPublish()
        {
            var root = GameRoot.Instance;
            var advance = InstallForegroundBoundaryTestClock();
            var update = (Action)Delegate.CreateDelegate(typeof(Action), root,
                typeof(GameRoot).GetMethod("UpdateForeground", BindingFlags.Instance | BindingFlags.NonPublic));
            update();
            for (int i = 0; i < 16; i++) update();
            var session = root.SaveService.CanonicalSession; var view = root.ForegroundActivity;
            long accepted = root.Save.livingDungeon.Scheduler.AcceptedThrough;
            long allocated = GC.GetAllocatedBytesForCurrentThread();
            var timer = System.Diagnostics.Stopwatch.StartNew();
            for (int frame = 0; frame < 120; frame++) { advance(.0001); update(); }
            timer.Stop(); allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Assert.That(root.SaveService.CanonicalSession, Is.SameAs(session));
            Assert.That(root.ForegroundActivity, Is.SameAs(view));
            Assert.That(root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(accepted));
            Assert.That(allocated, Is.LessThanOrEqualTo(1024), "Cached pump gate must not reconstruct complete saves per frame");
            TestContext.WriteLine("120 cached pump gates: " + timer.Elapsed.TotalMilliseconds + " ms; " + allocated + " bytes on test thread");
            yield return null;
        }

        [UnityTest]
        public IEnumerator LivingDungeonB3B2LiveFloorActionsValidateAfterTheirElapsedTimeBarrier()
        {
            OpenFloorManagement(); Assert.That(controller.ConstructFloor(), Is.True);
            var root = GameRoot.Instance;
            controller.SelectFloor(root.Save.validatedCanonicalSpatialState.Floors[1].FloorInstanceId);
            controller.EnterEdit(); Assert.That(controller.SelectConstructionRoom("spatial.room.basic"), Is.True);
            controller.TapWorld(new TileCoordinate(1, 2)); Assert.That(controller.ConfirmConstructionPlacement(), Is.True);
            yield return null; yield return null; Assert.That(controller.Commit(), Is.True);
            var advance = InstallForegroundBoundaryTestClock();
            advance(1);
            Assert.That(controller.ChangeFloorLifecycle(FloorLifecycleAction.Activate), Is.True,
                "A fresh direct click must revalidate after its own partial-time flush");
            advance(1);
            Assert.That(controller.ChangeFloorLifecycle(FloorLifecycleAction.Deactivate), Is.True);
        }

        [UnityTest]
        public IEnumerator LivingDungeonB3B2SuspendedDrainRecognizesItsDurablyPersistedPauseBoundary()
        {
            var root = GameRoot.Instance;
            Assert.That(root.SaveService.ForegroundConfiguration.MaximumBoundaries, Is.EqualTo(1));
            var advance = InstallForegroundBoundaryTestClock();
            long before = root.Save.livingDungeon.Scheduler.AcceptedThrough;
            advance(35); root.ApplyPauseState(true);
            Assert.That(root.TimeService.IsPaused, Is.True);
            Assert.That(root.Save.livingDungeon.IsClosed, Is.False, "One bounded pause flush cannot close three ticks");
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var update = typeof(GameRoot).GetMethod("UpdateForeground", flags);
            for (int pump = 0; pump < 8 && !root.Save.livingDungeon.IsClosed; pump++) update.Invoke(root, null);
            Assert.That(typeof(GameRoot).GetField("foregroundFailure", flags).GetValue(root), Is.Null);
            Assert.That(root.Save.livingDungeon.IsClosed, Is.True);
            Assert.That(root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(before + 35000));
            Assert.That(typeof(GameRoot).GetField("_resumeOfflineBoundaryPersisted", flags).GetValue(root), Is.True,
                "The completed suspended transaction persisted the original pause boundary; resume must retain its existing offline policy");
            yield return null;
        }
    }
}
#endif
