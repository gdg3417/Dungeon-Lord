using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public sealed class MvpSaveLifecycleIntegrityTests
    {
        private string _tempDir;
        private RunSimulationConfig _config;
        private RunSimulationService _runService;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "DungeonLord_GD56_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            Assert.That(GameRoot.TryCreateRunSimulationService(
                    File.ReadAllText("Assets/_Project/Data/Bootstrap/run_simulation_config.json"),
                    File.ReadAllText("Assets/_Project/Data/Bootstrap/loot_config.json"),
                    out _runService),
                Is.True);
            _config = _runService.Config;
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Test]
        public void CompleteMvpSaveReloadAndContinue_PreservesAuthoritativeStateWithoutDuplicatingRunsLootResearchOrObjectives()
        {
            using Harness original = CreateHarness("roundtrip.json");
            Assert.That(original.Root.ResetCleanMvpValidationSession(), Is.True);
            PlaceStarterDungeon(original.Root);
            Assert.That(original.Root.SimulateRunOnce(RunPostureResolver.BalancedId), Is.True);
            CompleteResearchThroughRootBoundary(original.Root);
            RunUntilFirstContractRequirementsComplete(original.Root);
            PlaceGreedTrialDungeon(original.Root);
            RunUntilNotice(original.Root);
            Assert.That(
                CurrentHeatTierResolver.Resolve(_config, original.Root.Save.structureRuntime.Heat).TierId,
                Is.EqualTo(CurrentHeatTierResolver.NoticeTierId),
                Snapshot.Capture(original.Root, _config).Describe());
            Assert.That(original.Root.TryMvpPlaceOrModifySelectedPlacementEnforcingRoomTarget(
                    MvpDungeonPlacementIds.TrapCategoryId,
                    MvpDungeonPlacementIds.ChillingSigilOptionId,
                    out _, out _, out _, out _),
                Is.True);
            Assert.That(BasicRunAnalysisAppliedAdjustmentPresenter.Resolve(original.Root.ResolveMvpPlayerLoopSummary()), Is.Not.Null);
            string latestBeforeAdjustmentRunId = original.Root.Save.runHistory.LatestOutcome.RunId;
            Assert.That(original.Root.SimulateRunOnce(RunPostureResolver.CautiousId), Is.True);
            Assert.That(original.Root.Save.runHistory.LatestOutcome.RunId, Is.Not.EqualTo(latestBeforeAdjustmentRunId));

            Snapshot before = Snapshot.Capture(original.Root, _config);
            Assert.That(before.RecoveredLoot, Is.GreaterThan(0), before.Describe());
            Assert.That(before.TradeableLoot, Is.GreaterThanOrEqualTo(0), before.Describe());
            Assert.That(before.CompletedResearchIds, Does.Contain(_config.MvpFirstSessionObjective.AnalysisResearchProjectId));
            Assert.That(before.CompletedObjectiveIds, Does.Contain(_config.MvpFirstSessionObjective.ObjectiveId));
            AssertUnique(before.CompletedResearchIds, "completed research before reload");
            AssertUnique(before.CompletedObjectiveIds, "completed objectives before reload");
            original.SaveService.Save(original.Root.Save, SaveReason.ManualDev);
            string savePath = original.SaveService.SavePath;
            original.Dispose();

            using Harness reloaded = CreateHarness("roundtrip.json");
            reloaded.LoadFromDisk();
            Snapshot after = Snapshot.Capture(reloaded.Root, _config);
            Assert.That(after.PlacementOptions, Is.EquivalentTo(before.PlacementOptions), after.Describe());
            Assert.That(after.RoomSignature, Is.EqualTo(before.RoomSignature), after.Describe());
            Assert.That(after.Heat, Is.EqualTo(before.Heat), after.Describe());
            Assert.That(after.Mana, Is.EqualTo(before.Mana), after.Describe());
            Assert.That(after.RunCount, Is.EqualTo(before.RunCount), after.Describe());
            Assert.That(after.LatestRunId, Is.EqualTo(before.LatestRunId), after.Describe());
            Assert.That(after.NextRunSequence, Is.EqualTo(before.NextRunSequence), after.Describe());
            Assert.That(after.LatestSuccess, Is.EqualTo(before.LatestSuccess), after.Describe());
            Assert.That(after.CompletedResearchIds, Is.EquivalentTo(before.CompletedResearchIds), after.Describe());
            Assert.That(after.CompletedObjectiveIds, Is.EquivalentTo(before.CompletedObjectiveIds), after.Describe());
            Assert.That(after.RecoveredLoot, Is.EqualTo(before.RecoveredLoot), after.Describe());
            Assert.That(after.TradeableLoot, Is.EqualTo(before.TradeableLoot), after.Describe());
            Assert.That(after.RecentSpoilsLatest, Is.EqualTo(before.RecentSpoilsLatest), after.Describe());
            Assert.That(after.RecentSpoilsBest, Is.EqualTo(before.RecentSpoilsBest), after.Describe());
            Assert.That(after.ContractComplete, Is.True, after.Describe());
            Assert.That(after.GreedTrialActive, Is.EqualTo(before.GreedTrialActive), after.Describe());
            Assert.That(after.HeatTier, Is.EqualTo(before.HeatTier), after.Describe());
            Assert.That(after.PrimaryActionKey, Is.EqualTo(before.PrimaryActionKey), after.Describe());
            Assert.That(after.OfflineSummaryJson, Is.EqualTo(before.OfflineSummaryJson), "savePath=" + savePath);

            int previousRunCount = reloaded.Root.Save.runHistory.RecentOutcomes.Length;
            int previousNextSequence = reloaded.Root.Save.runHistory.NextRunSequence;
            int previousLoot = after.RecoveredLoot;
            string[] researchBeforeContinue = reloaded.Root.Save.completedResearch.ProjectIds.ToArray();
            string[] objectivesBeforeContinue = reloaded.Root.Save.completedObjectives.ObjectiveIds.ToArray();
            Assert.That(reloaded.Root.SimulateRunOnce(RunPostureResolver.CautiousId), Is.True);
            Snapshot continued = Snapshot.Capture(reloaded.Root, _config);
            Assert.That(continued.RunCount, Is.EqualTo(previousRunCount + 1), continued.Describe());
            Assert.That(continued.LatestRunId, Is.EqualTo("run-" + previousNextSequence), continued.Describe());
            Assert.That(continued.NextRunSequence, Is.EqualTo(previousNextSequence + 1), continued.Describe());
            Assert.That(continued.RecoveredLoot - previousLoot, Is.EqualTo(reloaded.Root.Save.runHistory.LatestOutcome.LootExtractionSummary.TotalExtractedWorldValue), continued.Describe());
            Assert.That(continued.CompletedResearchIds, Is.EquivalentTo(researchBeforeContinue), continued.Describe());
            Assert.That(continued.CompletedObjectiveIds, Is.EquivalentTo(objectivesBeforeContinue), continued.Describe());
            AssertUnique(continued.CompletedResearchIds, "completed research after continue");
            AssertUnique(continued.CompletedObjectiveIds, "completed objectives after continue");
        }

        [Test]
        public void PauseResumeSkewQuitAndOfflineSummary_DoNotMutateGameplayOrGrantRewards()
        {
            var clock = new MutableTimeSource(1000);
            using Harness harness = CreateHarness("lifecycle.json", clock);
            Assert.That(harness.Root.ResetCleanMvpValidationSession(), Is.True);
            PlaceStarterDungeon(harness.Root);
            Assert.That(harness.Root.SimulateRunOnce(RunPostureResolver.BalancedId), Is.True);
            Snapshot beforePause = Snapshot.Capture(harness.Root, _config);

            harness.Root.ApplyPauseState(true);
            Assert.That(harness.Root.Save.lastPausedUtcUnix, Is.EqualTo(1000), beforePause.Describe());
            Assert.That(harness.Root.SaveLine, Is.EqualTo("Save: AppPause"));
            AssertGameplayUnchanged(beforePause, Snapshot.Capture(harness.Root, _config), "pause");

            clock.Now = 1030;
            harness.Root.ApplyPauseState(false);
            Assert.That(harness.Root.Save.lastResumedUtcUnix, Is.EqualTo(1030));
            Assert.That(harness.Root.BannerMessage, Is.Empty);
            AssertGameplayUnchanged(beforePause, Snapshot.Capture(harness.Root, _config), "normal resume");

            harness.Root.ApplyPauseState(true);
            clock.Now = 2000;
            harness.Root.ApplyPauseState(false);
            Assert.That(harness.Root.BannerMessage, Does.Contain("Time change detected"));
            AssertGameplayUnchanged(beforePause, Snapshot.Capture(harness.Root, _config), "skew resume");

            harness.Root.ApplyApplicationQuit();
            Assert.That(harness.Root.SaveLine, Is.EqualTo("Save: AppQuit"));
            AssertGameplayUnchanged(beforePause, Snapshot.Capture(harness.Root, _config), "quit");

            string offlineBefore = JsonUtility.ToJson(harness.Root.Save);
            harness.Root.CaptureOfflineSummaryDiagnostics();
            harness.Root.RefreshOfflineSummaryLines();
            Assert.That(harness.Root.Save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
            Assert.That(harness.Root.Save.runHistory.RecentOutcomes.Length, Is.EqualTo(beforePause.RunCount), offlineBefore);
            Assert.That(Snapshot.Capture(harness.Root, _config).RecoveredLoot, Is.EqualTo(beforePause.RecoveredLoot));
        }

        [Test]
        public void SaveService_CorruptMigrationAtomicBackupAndIsolationBehaviorsAreSafe()
        {
            var config = new SaveConfig { fileName = "safety.json", useAtomicWrites = true, keepBackups = 2 };
            var service = new SaveService(new SimpleLogger(false), config, _tempDir);
            File.WriteAllText(Path.Combine(_tempDir, "unrelated.txt"), "keep");
            File.WriteAllText(service.SavePath, "{ not valid json");
            SaveData recovered = service.LoadOrCreate("gd56", out string corruptBanner);
            Assert.That(recovered, Is.Not.Null);
            Assert.That(corruptBanner, Does.Contain("Created a new save"));
            Assert.That(Directory.GetFiles(_tempDir, "safety_corrupt_*.json"), Has.Length.EqualTo(1));
            Assert.That(File.Exists(Path.Combine(_tempDir, "unrelated.txt")), Is.True);
            service.Save(recovered, SaveReason.Boot);
            Assert.That(service.LoadOrCreate("gd56", out string secondBanner), Is.Not.Null);
            Assert.That(secondBanner, Is.Empty);
            Assert.That(Directory.GetFiles(_tempDir, "safety_corrupt_*.json"), Has.Length.EqualTo(1));

            SaveData legacy = BuildLegacySave();
            File.WriteAllText(service.SavePath, JsonUtility.ToJson(new SaveRoot { schemaVersion = 1, primary = legacy }, true));
            SaveData migrated = service.LoadOrCreate("gd56", out string migrationBanner);
            Assert.That(migrationBanner, Is.Empty);
            Assert.That(migrated.mvpDungeonPlacements.Entries.Select(e => e.OptionId), Does.Contain(MvpDungeonPlacementIds.BasicRoomOptionId));
            Assert.That(migrated.runHistory.LatestOutcome.RunId, Is.EqualTo("run-4"));
            Assert.That(migrated.runHistory.NextRunSequence, Is.EqualTo(5));
            Assert.That(migrated.completedResearch.ProjectIds, Is.EquivalentTo(new[] { "research.analysis" }));
            Assert.That(migrated.completedObjectives.ObjectiveIds, Is.EquivalentTo(new[] { "objective.first" }));
            AssertUnique(migrated.completedResearch.ProjectIds, "migrated research");
            AssertUnique(migrated.completedObjectives.ObjectiveIds, "migrated objectives");

            for (int i = 0; i < 4; i++)
            {
                migrated.totalTicks = i;
                service.Save(migrated, SaveReason.ManualDev);
                Assert.That(File.Exists(service.SavePath + ".tmp"), Is.False, "Temporary atomic save remained after iteration " + i);
                Assert.That(service.LoadOrCreate("gd56", out _).totalTicks, Is.EqualTo(i));
            }

            Assert.That(Directory.GetFiles(_tempDir, "safety_backup_*.json").Length, Is.LessThanOrEqualTo(2));
            Assert.That(Directory.GetFiles(_tempDir), Does.Contain(Path.Combine(_tempDir, "unrelated.txt")));
            Assert.That(Directory.Exists(_tempDir), Is.True);
        }

        [Test]
        public void FreshBootUiOnlyStateStartsAtDefaultsAndUsesIsolatedTemporarySave()
        {
            using Harness harness = CreateHarness("ui.json");
            harness.LoadFromDisk();
            var overlayObject = new GameObject("GD56Overlay");
            try
            {
                BootstrapOverlay overlay = overlayObject.AddComponent<BootstrapOverlay>();
                SetOverlay(overlay, "_devPanelVisible", true);
                SetOverlay(overlay, "_diagnosticsVisible", true);
                SetOverlay(overlay, "_compactSmokeViewEnabled", true);
                SetOverlay(overlay, "_minimalMvpActionPanelCollapsed", true);
                UnityEngine.Object.DestroyImmediate(overlayObject);

                overlayObject = new GameObject("GD56OverlayReloaded");
                overlay = overlayObject.AddComponent<BootstrapOverlay>();
                Assert.That(GetOverlay<bool>(overlay, "_devPanelVisible"), Is.False);
                Assert.That(GetOverlay<bool>(overlay, "_diagnosticsVisible"), Is.False);
                Assert.That(GetOverlay<bool>(overlay, "_compactSmokeViewEnabled"), Is.False);
                Assert.That(GetOverlay<bool>(overlay, "_minimalMvpActionPanelCollapsed"), Is.False);
                Assert.That(harness.Root.BannerMessage, Is.Empty);
                Assert.That(harness.SaveService.SavePath, Does.StartWith(_tempDir));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(overlayObject);
            }
        }

        private Harness CreateHarness(string fileName, MutableTimeSource clock = null)
        {
            var rootObject = new GameObject("GD56Root");
            var root = rootObject.AddComponent<GameRoot>();
            var saveService = new SaveService(new SimpleLogger(false), new SaveConfig { fileName = fileName, useAtomicWrites = true, keepBackups = 2 }, _tempDir);
            SetRoot(root, "<DevPanelEnabled>k__BackingField", true);
            SetRoot(root, "<Content>k__BackingField", BuildContent());
            SetRoot(root, "<SaveService>k__BackingField", saveService);
            SetRoot(root, "_runSimulationService", _runService);
            SetRoot(root, "<Save>k__BackingField", new SaveData());
            SetRoot(root, "<TimeService>k__BackingField", new TimeService(new SimpleLogger(false), 10, 300, clock ?? new MutableTimeSource(1000)));
            root.TimeService.AttachSave(root.Save);
            SetRoot(root, "_offlineSummaryResolver", new OfflineSummaryResolver(clock ?? new MutableTimeSource(1000)));
            return new Harness(rootObject, root, saveService, _config);
        }

        private ContentService BuildContent()
        {
            var content = new ContentService();
            SetContent(content, "<Bootstrap>k__BackingField", new ContentBootstrap
            {
                contentVersion = "gd56-test",
                tickSeconds = 10,
                timeRules = new TimeRules { detectClockSkewSeconds = 300, maxOfflineSeconds = 3600, offlineSummaryRuleSourceId = "offline.summary.gd56" },
                featureFlags = new FeatureFlags { enableDevPanel = true },
                researchCompletionEligibilityScaffold = new ResearchCompletionEligibilityScaffoldConfig { enabled = true, projectId = _config.MvpFirstSessionObjective.AnalysisResearchProjectId, requiredProgressUnits = 1d, ruleSourceId = "research.eligibility.gd56" },
                researchCompletionClaimScaffold = new ResearchCompletionClaimScaffoldConfig { enabled = true, ruleSourceId = "research.claim.gd56" },
                researchVerificationScaffold = new ResearchVerificationScaffoldConfig { enabled = true, verificationMode = ResearchVerificationBoundaryResolver.LocalDevPlaceholderVerificationMode, ruleSourceId = "research.verification.gd56" },
                researchUnlockBridge = new ResearchUnlockBridgeConfig { enabled = true, ruleSourceId = "research.unlock.gd56", unlocks = new[] { new ResearchUnlockDefinitionConfig { researchProjectId = _config.MvpFirstSessionObjective.AnalysisResearchProjectId, unlockId = MvpPlayerLoopSummaryPresenter.BasicRunAnalysisUnlockId, summaryKey = "ui.research.unlock.basic_run_analysis.summary" } } }
            });
            return content;
        }

        private static void PlaceStarterDungeon(GameRoot root)
        {
            Place(root, MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId);
            Place(root, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
            Place(root, MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId);
            Place(root, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId);
        }

        private static void PlaceGreedTrialDungeon(GameRoot root)
        {
            Place(root, MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.ChillingSigilOptionId);
            Place(root, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.GlitteringHoardOptionId);
        }

        private static void Place(GameRoot root, string category, string option)
        {
            Assert.That(root.TryMvpPlaceOrModifySelectedPlacementEnforcingRoomTarget(category, option, out _, out _, out string banner, out string failure), Is.True, category + ":" + option + " " + banner + " " + failure);
        }

        private void CompleteResearchThroughRootBoundary(GameRoot root)
        {
            root.Save.researchPending = new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = _config.MvpFirstSessionObjective.AnalysisResearchProjectId };
            root.Save.researchProgress = new ResearchProgressState { SlotId = "research.slot.primary", ProjectId = _config.MvpFirstSessionObjective.AnalysisResearchProjectId, ProgressUnits = 1d, CompletionPending = true };
            Assert.That(root.ClaimResearchCompletionScaffold(), Is.True);
        }

        private void RunUntilFirstContractRequirementsComplete(GameRoot root)
        {
            for (int i = 0; i < 20; i++)
            {
                MvpFirstSessionObjectiveSummary summary = MvpFirstSessionObjectivePresenter.Resolve(root.Save, _config);
                if (summary.CurrentRequirementsComplete)
                {
                    return;
                }

                if (!summary.HeatTargetComplete)
                {
                    Place(root, MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.ChillingSigilOptionId);
                    Assert.That(root.SimulateRunOnce(RunPostureResolver.CautiousId), Is.True);
                }
                else
                {
                    Assert.That(root.SimulateRunOnce(RunPostureResolver.BalancedId), Is.True);
                }
            }

            Assert.Fail("First contract did not complete. " + Snapshot.Capture(root, _config).Describe());
        }

        private void RunUntilNotice(GameRoot root)
        {
            for (int i = 0; i < 20 && !string.Equals(CurrentHeatTierResolver.Resolve(_config, root.Save.structureRuntime.Heat).TierId, CurrentHeatTierResolver.NoticeTierId, StringComparison.Ordinal); i++)
            {
                Assert.That(root.SimulateRunOnce(RunPostureResolver.GreedyId), Is.True);
            }
        }

        private static SaveData BuildLegacySave()
        {
            return new SaveData
            {
                saveVersion = 1,
                createdUtcUnix = 1,
                lastSavedUtcUnix = 2,
                mvpDungeonPlacements = new MvpDungeonPlacementState { Entries = { new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId, 1) }, NextRevision = 2 },
                runHistory = new RunHistoryState { NextRunSequence = 5, LatestOutcome = new RunOutcomeRecord { RunId = "run-4", LootExtractionSummary = new RunLootExtractionSummary { TotalExtractedWorldValue = 3 } }, RecentOutcomes = new[] { new RunOutcomeRecord { RunId = "run-4", LootExtractionSummary = new RunLootExtractionSummary { TotalExtractedWorldValue = 3 } } } },
                completedResearch = new CompletedResearchState { ProjectIds = new[] { "research.analysis" }, LastCompletedProjectId = "research.analysis" },
                completedObjectives = new CompletedObjectiveState { ObjectiveIds = new[] { "objective.first" }, LastCompletedObjectiveId = "objective.first" }
            };
        }

        private static void AssertGameplayUnchanged(Snapshot expected, Snapshot actual, string boundary)
        {
            Assert.That(actual.RunCount, Is.EqualTo(expected.RunCount), boundary + " " + actual.Describe());
            Assert.That(actual.RecoveredLoot, Is.EqualTo(expected.RecoveredLoot), boundary + " " + actual.Describe());
            Assert.That(actual.CompletedResearchIds, Is.EquivalentTo(expected.CompletedResearchIds), boundary + " " + actual.Describe());
            Assert.That(actual.CompletedObjectiveIds, Is.EquivalentTo(expected.CompletedObjectiveIds), boundary + " " + actual.Describe());
            Assert.That(actual.PlacementOptions, Is.EquivalentTo(expected.PlacementOptions), boundary + " " + actual.Describe());
            Assert.That(actual.Heat, Is.EqualTo(expected.Heat), boundary + " " + actual.Describe());
            Assert.That(actual.Mana, Is.EqualTo(expected.Mana), boundary + " " + actual.Describe());
            Assert.That(actual.PrimaryActionKey, Is.EqualTo(expected.PrimaryActionKey), boundary + " " + actual.Describe());
        }

        private static void AssertUnique(string[] ids, string label)
        {
            string[] safe = ids ?? Array.Empty<string>();
            Assert.That(safe.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(safe.Length), label + ": " + string.Join(",", safe));
        }

        private static void SetRoot(GameRoot root, string field, object value) => typeof(GameRoot).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(root, value);
        private static void SetContent(ContentService content, string field, object value) => typeof(ContentService).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(content, value);
        private static void SetOverlay(BootstrapOverlay overlay, string field, object value) => typeof(BootstrapOverlay).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(overlay, value);
        private static T GetOverlay<T>(BootstrapOverlay overlay, string field) => (T)typeof(BootstrapOverlay).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(overlay);

        private sealed class Harness : IDisposable
        {
            private readonly GameObject _rootObject;
            private readonly RunSimulationConfig _config;
            public GameRoot Root { get; }
            public SaveService SaveService { get; }

            public Harness(GameObject rootObject, GameRoot root, SaveService saveService, RunSimulationConfig config)
            {
                _rootObject = rootObject;
                Root = root;
                SaveService = saveService;
                _config = config;
            }

            public void LoadFromDisk()
            {
                SaveData loaded = SaveService.LoadOrCreate("gd56-test", out string banner);
                SaveMigration.MigrateToLatest(new SaveRoot { primary = loaded });
                SetRoot(Root, "<Save>k__BackingField", loaded);
                Root.TimeService.AttachSave(loaded);
                Root.CaptureOfflineSummaryDiagnostics();
                Root.RefreshOfflineSummaryLines();
                Assert.That(
                    string.IsNullOrEmpty(banner) || banner.Contains("Created a new save"),
                    Is.True,
                    "Unexpected load banner: " + banner);
                Assert.That(Root.ResolveMvpPlayerLoopSummary().RuleResolved, Is.True, Snapshot.Capture(Root, _config).Describe());
            }

            public void Dispose()
            {
                if (_rootObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(_rootObject);
                }
            }
        }

        private sealed class MutableTimeSource : ITimeSource
        {
            public long Now;
            public MutableTimeSource(long now) => Now = now;
            public long UtcNowUnixSeconds() => Now;
        }

        private sealed class Snapshot
        {
            public string[] PlacementOptions;
            public string RoomSignature;
            public double Heat;
            public double Mana;
            public int RunCount;
            public string LatestRunId;
            public int NextRunSequence;
            public bool LatestSuccess;
            public string[] CompletedResearchIds;
            public string[] CompletedObjectiveIds;
            public int RecoveredLoot;
            public int TradeableLoot;
            public int RecentSpoilsLatest;
            public int RecentSpoilsBest;
            public bool ContractComplete;
            public bool GreedTrialActive;
            public string HeatTier;
            public string PrimaryActionKey;
            public string OfflineSummaryJson;

            public static Snapshot Capture(GameRoot root, RunSimulationConfig config)
            {
                MvpPlayerLoopSummary loop = root.ResolveMvpPlayerLoopSummary();
                MvpFirstSessionObjectiveSummary contract = MvpFirstSessionObjectivePresenter.Resolve(root.Save, config);
                MvpPostContractGreedTrialSummary greed = MvpPostContractGreedTrialPresenter.Resolve(root.Save, config, contract);
                MvpRecentSpoilsLedgerSummary spoils = MvpRecentSpoilsLedgerPresenter.Resolve(root.Save, greed);
                MvpPrimaryNextActionSummary primary = MvpPrimaryNextActionPresenter.Resolve(loop, root.ResolveGuidedMvpActionPath(loop), contract, greed);
                RunOutcomeRecord[] outcomes = root.Save.runHistory?.RecentOutcomes ?? Array.Empty<RunOutcomeRecord>();
                return new Snapshot
                {
                    PlacementOptions = (root.Save.mvpDungeonPlacements?.Entries != null
                        ? root.Save.mvpDungeonPlacements.Entries
                        : Enumerable.Empty<MvpDungeonPlacementEntry>()).Select(e => e.OptionId).ToArray(),
                    RoomSignature = JsonUtility.ToJson(root.Save.mvpRoomSlotAssignments),
                    Heat = root.Save.structureRuntime != null ? root.Save.structureRuntime.Heat : 0d,
                    Mana = root.Save.structureRuntime != null ? root.Save.structureRuntime.ManaReserve : 0d,
                    RunCount = outcomes.Length,
                    LatestRunId = root.Save.runHistory?.LatestOutcome?.RunId ?? string.Empty,
                    NextRunSequence = root.Save.runHistory != null ? root.Save.runHistory.NextRunSequence : 0,
                    LatestSuccess = root.Save.runHistory?.LatestOutcome?.Success ?? false,
                    CompletedResearchIds = root.Save.completedResearch?.ProjectIds ?? Array.Empty<string>(),
                    CompletedObjectiveIds = root.Save.completedObjectives?.ObjectiveIds ?? Array.Empty<string>(),
                    RecoveredLoot = outcomes.Sum(o => o?.LootExtractionSummary?.TotalExtractedWorldValue ?? 0),
                    TradeableLoot = outcomes.Sum(o => o?.LootExtractionSummary?.TotalExtractedTradeableWorldValue ?? 0),
                    RecentSpoilsLatest = spoils.LatestTradeableValue,
                    RecentSpoilsBest = spoils.RecentBestTradeableValue,
                    ContractComplete = contract.IsComplete,
                    GreedTrialActive = greed.IsActive,
                    HeatTier = greed.CurrentHeatTierId,
                    PrimaryActionKey = primary.PrimaryActionKey,
                    OfflineSummaryJson = JsonUtility.ToJson(root.Save.lastOfflineSummary)
                };
            }

            public string Describe()
            {
                return $"runCount={RunCount} latestRunId={LatestRunId} next={NextRunSequence} loot={RecoveredLoot} tradeable={TradeableLoot} research=[{string.Join(",", CompletedResearchIds ?? Array.Empty<string>())}] objectives=[{string.Join(",", CompletedObjectiveIds ?? Array.Empty<string>())}] heat={Heat} mana={Mana} tier={HeatTier} primary={PrimaryActionKey}";
            }
        }
    }
}
