#if UNITY_EDITOR
using System;
using System.IO;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class PlayerResearchSaveServiceIntegrationTests
    {
        private const string ProjectId = "test.activity.analysis";
        private string _directory;
        private SaveService _service;
        private int _saveCount;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "DungeonLordGD59", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _service = new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "gd59.json", useAtomicWrites = true }, _directory);
            _saveCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void StartPartialReadyAndClaim_RoundTripThroughActualSaveService()
        {
            SaveData save = NewSave();
            PlayerResearchActionHandler handler = Create(save);
            Assert.That(handler.Start().Succeeded, Is.True);
            Assert.That(File.Exists(_service.SavePath), Is.True);
            PlayerResearchActionResult partial = handler.ApplyActiveTick(4);
            Assert.That(partial.StateChanged, Is.True);
            Assert.That(partial.ProgressUnits, Is.EqualTo(0.4d));

            SaveData partialLoaded = _service.LoadOrCreate("gd59", out string partialBanner);
            Assert.That(partialBanner, Is.Empty);
            Assert.That(partialLoaded.researchPending.ProjectId, Is.EqualTo(ProjectId));
            Assert.That(partialLoaded.researchProgress.ProgressUnits, Is.EqualTo(0.4d));
            Assert.That(partialLoaded.researchProgress.CompletionPending, Is.False);

            PlayerResearchActionHandler resumed = Create(partialLoaded);
            PlayerResearchActionResult ready = resumed.ApplyActiveTick(6);
            Assert.That(ready.State, Is.EqualTo(PlayerResearchState.ReadyToClaim));
            SaveData readyLoaded = _service.LoadOrCreate("gd59", out _);
            Assert.That(readyLoaded.researchProgress.CompletionPending, Is.True);
            Assert.That(Create(readyLoaded).ResolveState().CanClaimLocalMvp, Is.True);

            Assert.That(Create(readyLoaded).Claim().Succeeded, Is.True);
            Assert.That(_saveCount, Is.EqualTo(4), "Start, partial progress, ready transition, and claim each own exactly one save.");
            SaveData completedLoaded = _service.LoadOrCreate("gd59", out _);
            Assert.That(completedLoaded.completedResearch.ProjectIds, Is.EqualTo(new[] { ProjectId }));
            Assert.That(completedLoaded.researchPending, Is.Null);
            Assert.That(completedLoaded.researchProgress, Is.Null);
            ResearchUnlockSummary unlock = ResearchUnlockSummaryPresenter.Resolve(completedLoaded.completedResearch, UnlockConfig());
            Assert.That(unlock.RuleResolved, Is.True);
            Assert.That(unlock.UnlockId, Is.EqualTo(MvpPlayerLoopSummaryPresenter.BasicRunAnalysisUnlockId));
            MvpFirstSessionObjectiveSummary contract = MvpFirstSessionObjectivePresenter.Resolve(completedLoaded, RunConfig());
            Assert.That(contract.AnalysisComplete, Is.True);
            Assert.That(contract.CurrentRequirementsComplete, Is.False, "Research persists coherently without fabricating unrelated contract requirements.");
        }

        private PlayerResearchActionHandler Create(SaveData save) => new PlayerResearchActionHandler(
            save, PendingConfig(), ProgressConfig(), EligibilityConfig(), ClaimConfig(), VerificationConfig(), ProjectId,
            new RestrictedActionGateService(), () => true, () => true, () => false,
            () => { _saveCount++; _service.Save(save, SaveReason.StateChange); });

        private static SaveData NewSave() => new SaveData
        {
            completedResearch = new CompletedResearchState(),
            completedObjectives = new CompletedObjectiveState(),
            runHistory = new RunHistoryState(),
            structureRuntime = new StructureRuntimeState()
        };

        private static ResearchPendingScaffoldConfig PendingConfig() => new ResearchPendingScaffoldConfig { enabled = true, slotId = "slot", projectId = ProjectId, ruleSourceId = "test.pending" };
        private static ResearchProgressScaffoldConfig ProgressConfig() => new ResearchProgressScaffoldConfig { enabled = true, progressPerActiveSecond = 0.1d, maxActiveSessionElapsedSeconds = 100, ruleSourceId = "test.progress" };
        private static ResearchCompletionEligibilityScaffoldConfig EligibilityConfig() => new ResearchCompletionEligibilityScaffoldConfig { enabled = true, projectId = ProjectId, requiredProgressUnits = 1d, ruleSourceId = "test.eligibility" };
        private static ResearchCompletionClaimScaffoldConfig ClaimConfig() => new ResearchCompletionClaimScaffoldConfig { enabled = true, ruleSourceId = "test.claim", claimAuthorityMode = PlayerResearchClaimAuthorityResolver.LocalMvpAuthorityMode };
        private static ResearchVerificationScaffoldConfig VerificationConfig() => new ResearchVerificationScaffoldConfig { enabled = true, ruleSourceId = "test.verification", verificationMode = ResearchVerificationBoundaryResolver.UnavailableVerificationMode };
        private static ResearchUnlockBridgeConfig UnlockConfig() => new ResearchUnlockBridgeConfig { enabled = true, ruleSourceId = "test.unlock", unlocks = new[] { new ResearchUnlockDefinitionConfig { researchProjectId = ProjectId, unlockId = MvpPlayerLoopSummaryPresenter.BasicRunAnalysisUnlockId, summaryKey = "test.summary" } } };
        private static RunSimulationConfig RunConfig() => new RunSimulationConfig { MvpFirstSessionObjective = new MvpFirstSessionObjectiveConfig { ObjectiveId = "test.objective", RequiredRecoveredLootValue = 1, RequireResearchAnalysisUnlocked = true, AnalysisResearchProjectId = ProjectId } };
    }
}
#endif
