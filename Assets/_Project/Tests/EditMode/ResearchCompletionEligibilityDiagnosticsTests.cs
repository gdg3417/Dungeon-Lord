using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchCompletionEligibilityDiagnosticsTests
    {
        private GameObject _rootObject;
        private GameObject _overlayObject;
        private GameObject _textObject;
        private GameRoot _root;
        private BootstrapOverlay _overlay;

        [SetUp]
        public void SetUp()
        {
            _rootObject = new GameObject("ResearchCompletionEligibilityDiagnosticsRootTest");
            _overlayObject = new GameObject("ResearchCompletionEligibilityDiagnosticsOverlayTest");
            _textObject = new GameObject("ResearchCompletionEligibilityDiagnosticsTextTest");
            _root = _rootObject.AddComponent<GameRoot>();
            SetBackingField("<Content>k__BackingField", BuildContent());
            SetBackingField("<Save>k__BackingField", BuildSave());
            _root.RefreshOfflineSummaryLines();

            _overlay = _overlayObject.AddComponent<BootstrapOverlay>();
            _overlay.overlayText = _textObject.AddComponent<TextMeshProUGUI>();
            _overlay.Bind(_root);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_textObject);
            Object.DestroyImmediate(_overlayObject);
            Object.DestroyImmediate(_rootObject);
        }

        [Test]
        public void ResearchDiagnostics_ShowsExistingProgressLinesAndLocalizedEligibilityWithoutRawKeys()
        {
            string researchText = ResearchDiagnosticsText();
            string statusText = ResearchStatusDiagnosticsText();
            string verificationText = ResearchVerificationDiagnosticsText();

            Assert.That(researchText, Does.Contain("Research Progress Preview —"));
            Assert.That(researchText, Does.Contain("Research Progress State —"));
            Assert.That(researchText, Does.Contain("Research Completion Eligibility — resolved=True error=0 pending=True hasState=True slot=research.slot.primary project=research.project.scaffold progress=0 required=2 remaining=2 eligible=False wouldSetCompletionPending=False wouldComplete=False ruleSource=research.completion_eligibility.rule.test"));
            Assert.That(researchText, Does.Not.Contain("ui.dev.research_completion_eligibility_format"));
            Assert.That(researchText, Does.Contain("Research Completion Pending Apply — resolved=True error=0 pending=True hasState=True slot=research.slot.primary project=research.project.scaffold progress=0 required=2 eligible=False alreadyCompletionPending=False wouldSetCompletionPending=False wouldComplete=False ruleSource=research.completion_eligibility.rule.test"));
            Assert.That(researchText, Does.Not.Contain("ui.dev.research_completion_pending_apply_format"));
            Assert.That(researchText, Does.Contain("Research Completion Claim Readiness — resolved=True error=0 pending=True hasState=True slot=research.slot.primary project=research.project.scaffold progress=0 required=2 completionPending=False eligible=False readyForClaim=False wouldComplete=False wouldGrantRewards=False wouldUnlockContent=False wouldClearPending=False ruleSource=research.completion_eligibility.rule.test"));
            Assert.That(researchText, Does.Not.Contain("ui.dev.research_completion_claim_readiness_format"));
            Assert.That(researchText, Does.Contain("Completed Research State — resolved=True error=0 hasState=False completedCount=0 lastCompletedProject= currentPendingProject=research.project.scaffold currentProgressProject=research.project.scaffold currentProjectAlreadyCompleted=False wouldBlockClaimAsDuplicate=False wouldGrantRewards=False wouldUnlockContent=False ruleSource="));
            Assert.That(researchText, Does.Not.Contain("ui.dev.completed_research_state_format"));
            Assert.That(researchText, Does.Contain("Research Completion Claim Apply — resolved=True error=0 pending=True hasProgressState=True hasCompletedState=False slot=research.slot.primary project=research.project.scaffold progress=0 required=2 completionPending=False eligible=False readyForClaim=False alreadyCompleted=False wouldRecordCompletedResearch=False wouldClearPending=False wouldClearProgress=False wouldGrantRewards=False wouldUnlockContent=False wouldChargeCosts=False wouldProcessOfflineProgress=False ruleSource=research.completion_claim.rule.test"));
            Assert.That(researchText, Does.Not.Contain("ui.dev.research_completion_claim_apply_format"));
            Assert.That(researchText, Does.Not.Contain("Research Status Presentation —"));
            Assert.That(researchText, Does.Not.Contain("Research Status Safety —"));
            Assert.That(researchText, Does.Not.Contain("Research Verification Boundary —"));
            Assert.That(researchText, Does.Not.Contain("Research Verification Safety —"));

            Assert.That(statusText, Does.Contain("Research Status Presentation — state=ActiveInProgress pending=True hasProgressState=True hasCompletedState=False slot=research.slot.primary project=research.project.scaffold progress=0 required=2 completionPending=False eligible=False verificationRequired=False readyToClaim=False completed=False blockedOrInvalid=False statusKey=ui.research.status.active_in_progress ruleSource=research.completion_eligibility.rule.test"));
            Assert.That(statusText, Does.Contain("Research Status Safety — canClaimProduction=False wouldGrantRewards=False wouldUnlockContent=False wouldChargeCosts=False wouldProcessOfflineProgress=False"));
            Assert.That(statusText, Does.Not.Contain("ui.dev.research_status_presentation_format"));
            Assert.That(statusText, Does.Not.Contain("ui.dev.research_status_safety_format"));
            Assert.That(statusText, Does.Not.Contain("Research Verification Boundary —"));
            Assert.That(statusText, Does.Not.Contain("Research Verification Safety —"));

            Assert.That(verificationText, Does.Contain("Research Verification Boundary — resolved=False error=15 pending=True hasProgressState=True hasCompletedState=False slot=research.slot.primary project=research.project.scaffold progress=0 required=2 completionPending=False eligible=False alreadyCompleted=False verificationRequired=False verificationAvailable=False verificationSatisfied=False canClaimProduction=False verificationMode=unavailable ruleSource=research.verification.rule.test"));
            Assert.That(verificationText, Does.Contain("Research Verification Safety — wouldCallServer=False wouldGrantRewards=False wouldUnlockContent=False wouldChargeCosts=False wouldProcessOfflineProgress=False"));
            Assert.That(verificationText, Does.Not.Contain("ui.dev.research_verification_boundary_format"));
            Assert.That(verificationText, Does.Not.Contain("ui.dev.research_verification_safety_format"));
        }

        [Test]
        public void SystemsDiagnostics_DoesNotContainResearchDiagnosticBlock()
        {
            ShowDiagnostics();
            while (_overlay.FullDiagnosticsPageNumber != 4)
            {
                _overlay.CycleFullDiagnosticsPage();
            }
            _overlay.RefreshOverlayText();
            string text = _overlay.overlayText.text;

            Assert.That(text, Does.Contain("Offline Summary —"));
            Assert.That(text, Does.Not.Contain("Research Pending —"));
            Assert.That(text, Does.Not.Contain("Research Progress Preview —"));
            Assert.That(text, Does.Not.Contain("Research Progress State —"));
            Assert.That(text, Does.Not.Contain("Research Completion Eligibility —"));
            Assert.That(text, Does.Not.Contain("Research Completion Pending Apply —"));
            Assert.That(text, Does.Not.Contain("Research Completion Claim Readiness —"));
            Assert.That(text, Does.Not.Contain("Completed Research State —"));
            Assert.That(text, Does.Not.Contain("Research Completion Claim Apply —"));
            Assert.That(text, Does.Not.Contain("Research Status Presentation —"));
            Assert.That(text, Does.Not.Contain("Research Status Safety —"));
            Assert.That(text, Does.Not.Contain("Research Verification Boundary —"));
            Assert.That(text, Does.Not.Contain("Research Verification Safety —"));
        }

        [Test]
        public void RefreshOfflineSummaryLines_NoPendingShowsSafeOutputWithoutStaleProject()
        {
            _root.Save.researchPending = null;
            _root.Save.researchProgress = null;

            _root.RefreshOfflineSummaryLines();

            Assert.That(_root.ResearchCompletionEligibilityLine, Does.Contain("resolved=False error=1 pending=False hasState=False slot= project= progress=0 required=0 remaining=0 eligible=False wouldSetCompletionPending=False wouldComplete=False ruleSource="));
            Assert.That(_root.ResearchCompletionEligibilityLine, Does.Not.Contain("research.project.scaffold"));
            Assert.That(_root.ResearchCompletionPendingApplyLine, Does.Contain("resolved=False error=1 pending=False hasState=False slot= project= progress=0 required=0 eligible=False alreadyCompletionPending=False wouldSetCompletionPending=False wouldComplete=False ruleSource="));
            Assert.That(_root.ResearchCompletionPendingApplyLine, Does.Not.Contain("research.project.scaffold"));
            Assert.That(_root.ResearchCompletionClaimReadinessLine, Does.Contain("resolved=False error=1 pending=False hasState=False slot= project= progress=0 required=0 completionPending=False eligible=False readyForClaim=False wouldComplete=False wouldGrantRewards=False wouldUnlockContent=False wouldClearPending=False ruleSource="));
            Assert.That(_root.ResearchCompletionClaimReadinessLine, Does.Not.Contain("research.project.scaffold"));
            Assert.That(_root.CompletedResearchStateLine, Does.Contain("resolved=True error=0 hasState=False completedCount=0 lastCompletedProject= currentPendingProject= currentProgressProject= currentProjectAlreadyCompleted=False wouldBlockClaimAsDuplicate=False wouldGrantRewards=False wouldUnlockContent=False ruleSource="));
            Assert.That(_root.CompletedResearchStateLine, Does.Not.Contain("research.project.scaffold"));
            Assert.That(_root.ResearchCompletionClaimApplyLine, Does.Contain("resolved=False error=1 pending=False hasProgressState=False hasCompletedState=False slot= project="));
            Assert.That(_root.ResearchCompletionClaimApplyLine, Does.Not.Contain("research.project.scaffold"));
            Assert.That(_root.ResearchStatusPresentationLine, Does.Contain("state=NoResearch pending=False hasProgressState=False hasCompletedState=False slot= project="));
            Assert.That(_root.ResearchStatusSafetyLine, Does.Contain("canClaimProduction=False"));
            Assert.That(_root.ResearchVerificationBoundaryLine, Does.Contain("resolved=False error=1 pending=False hasProgressState=False hasCompletedState=False slot= project="));
            Assert.That(_root.ResearchVerificationSafetyLine, Does.Contain("wouldCallServer=False wouldGrantRewards=False wouldUnlockContent=False wouldChargeCosts=False wouldProcessOfflineProgress=False"));
            Assert.That(_root.ResearchStatusPresentationLine, Does.Not.Contain("project=research.project.scaffold"));
            Assert.That(_root.ResearchVerificationBoundaryLine, Does.Not.Contain("project=research.project.scaffold"));
        }

        [Test]
        public void ActiveTickProgressApply_EnoughProgressMakesEligibilityTrueWithoutCompletionMutationOrAdjacentRewards()
        {
            SaveData save = _root.Save;
            double heatBefore = save.structureRuntime.Heat;
            double manaBefore = save.structureRuntime.ManaReserve;
            string historyBefore = JsonUtility.ToJson(save.runHistory);
            string offlineBefore = JsonUtility.ToJson(save.lastOfflineSummary);

            InvokeResearchProgressApply();
            _root.RefreshOfflineSummaryLines();
            Assert.That(_root.ResearchCompletionEligibilityLine, Does.Contain("progress=1 required=2 remaining=1 eligible=False"));
            Assert.That(_root.ResearchCompletionClaimReadinessLine, Does.Contain("progress=1 required=2 completionPending=False eligible=False readyForClaim=False"));
            InvokeResearchProgressApply();
            _root.RefreshOfflineSummaryLines();

            Assert.That(_root.ResearchCompletionEligibilityLine, Does.Contain("progress=2 required=2 remaining=0 eligible=True wouldSetCompletionPending=False wouldComplete=False"));
            Assert.That(_root.ResearchCompletionClaimReadinessLine, Does.Contain("progress=2 required=2 completionPending=False eligible=True readyForClaim=False wouldComplete=False wouldGrantRewards=False wouldUnlockContent=False wouldClearPending=False"));
            Assert.That(_root.ResearchStatusPresentationLine, Does.Contain("state=ActiveCompletionPending pending=True"));
            Assert.That(_root.ResearchStatusSafetyLine, Does.Contain("canClaimProduction=False"));
            Assert.That(_root.ResearchVerificationBoundaryLine, Does.Contain("verificationSatisfied=False canClaimProduction=False"));
            Assert.That(_root.ResearchVerificationSafetyLine, Does.Contain("wouldCallServer=False"));
            Assert.That(save.researchProgress.CompletionPending, Is.False);
            Assert.That(save.completedResearch, Is.Null);
            Assert.That(save.researchPending, Is.Not.Null);
            Assert.That(save.researchPending.ProjectId, Is.EqualTo("research.project.scaffold"));
            Assert.That(save.structureRuntime.Heat, Is.EqualTo(heatBefore));
            Assert.That(save.structureRuntime.ManaReserve, Is.EqualTo(manaBefore));
            Assert.That(JsonUtility.ToJson(save.runHistory), Is.EqualTo(historyBefore));
            Assert.That(JsonUtility.ToJson(save.lastOfflineSummary), Is.EqualTo(offlineBefore));
            Assert.That(save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
        }

        [Test]
        public void CompletionPendingApply_AfterThresholdShowsResolvedEligibilityAndAlreadyPendingWithoutCompletionOrAdjacentRewards()
        {
            SaveData save = _root.Save;
            double heatBefore = save.structureRuntime.Heat;
            double manaBefore = save.structureRuntime.ManaReserve;
            string pendingBefore = JsonUtility.ToJson(save.researchPending);
            string historyBefore = JsonUtility.ToJson(save.runHistory);
            string offlineBefore = JsonUtility.ToJson(save.lastOfflineSummary);
            long totalTicksBefore = save.totalTicks;

            InvokeResearchProgressApply();
            InvokeResearchProgressApply();
            InvokeResearchCompletionPendingApply();
            _root.RefreshOfflineSummaryLines();

            Assert.That(save.researchProgress.ProgressUnits, Is.EqualTo(2d));
            Assert.That(save.researchProgress.CompletionPending, Is.True);
            Assert.That(save.completedResearch, Is.Null);
            Assert.That(_root.ResearchCompletionEligibilityLine, Does.Contain("resolved=True error=0 pending=True hasState=True"));
            Assert.That(_root.ResearchCompletionEligibilityLine, Does.Contain("progress=2 required=2 remaining=0 eligible=True wouldSetCompletionPending=False wouldComplete=False"));
            Assert.That(_root.ResearchCompletionPendingApplyLine, Does.Contain("eligible=True alreadyCompletionPending=True wouldSetCompletionPending=False wouldComplete=False"));
            Assert.That(_root.ResearchCompletionClaimReadinessLine, Does.Contain("completionPending=True eligible=True readyForClaim=True wouldComplete=False wouldGrantRewards=False wouldUnlockContent=False wouldClearPending=False"));
            Assert.That(_root.ResearchCompletionClaimApplyLine, Does.Contain("resolved=True error=0 pending=True hasProgressState=True hasCompletedState=False slot=research.slot.primary project=research.project.scaffold progress=2 required=2 completionPending=True eligible=True readyForClaim=True alreadyCompleted=False wouldRecordCompletedResearch=True wouldClearPending=True wouldClearProgress=True wouldGrantRewards=False wouldUnlockContent=False wouldChargeCosts=False wouldProcessOfflineProgress=False ruleSource=research.completion_claim.rule.test"));
            Assert.That(_root.ResearchStatusPresentationLine, Does.Contain("state=VerificationRequired pending=True"));
            Assert.That(_root.ResearchStatusPresentationLine, Does.Contain("verificationRequired=True readyToClaim=False"));
            Assert.That(_root.ResearchStatusSafetyLine, Does.Contain("canClaimProduction=False"));
            Assert.That(_root.ResearchVerificationBoundaryLine, Does.Contain("completionPending=True eligible=True alreadyCompleted=False verificationRequired=True verificationAvailable=False verificationSatisfied=False canClaimProduction=False verificationMode=unavailable ruleSource=research.verification.rule.test"));
            Assert.That(_root.ResearchVerificationSafetyLine, Does.Contain("wouldCallServer=False wouldGrantRewards=False wouldUnlockContent=False wouldChargeCosts=False wouldProcessOfflineProgress=False"));
            Assert.That(JsonUtility.ToJson(save.researchPending), Is.EqualTo(pendingBefore));
            Assert.That(save.structureRuntime.Heat, Is.EqualTo(heatBefore));
            Assert.That(save.structureRuntime.ManaReserve, Is.EqualTo(manaBefore));
            Assert.That(JsonUtility.ToJson(save.runHistory), Is.EqualTo(historyBefore));
            Assert.That(JsonUtility.ToJson(save.lastOfflineSummary), Is.EqualTo(offlineBefore));
            Assert.That(save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
            Assert.That(save.totalTicks, Is.EqualTo(totalTicksBefore));
        }

        [Test]
        public void ClaimResearchCompletionScaffold_ReadyStateRefreshesDiagnosticsWithoutStaleActiveProject()
        {
            InvokeResearchProgressApply();
            InvokeResearchProgressApply();
            InvokeResearchCompletionPendingApply();
            _root.RefreshOfflineSummaryLines();

            Assert.That(_root.ClaimResearchCompletionScaffold(), Is.True);

            Assert.That(_root.ResearchPendingLine, Does.Contain("pending=False slot= project="));
            Assert.That(_root.ResearchProgressStateLine, Does.Contain("pending=False hasState=False slot= project="));
            Assert.That(_root.CompletedResearchStateLine, Does.Contain("completedCount=1 lastCompletedProject=research.project.scaffold currentPendingProject= currentProgressProject="));
            Assert.That(_root.ResearchCompletionClaimApplyLine, Does.Contain("pending=False hasProgressState=False hasCompletedState=True slot= project="));
            Assert.That(_root.ResearchCompletionClaimApplyLine, Does.Not.Contain("project=research.project.scaffold"));
            Assert.That(_root.ResearchStatusPresentationLine, Does.Contain("state=Completed pending=False hasProgressState=False hasCompletedState=True slot= project=research.project.scaffold"));
            Assert.That(_root.ResearchStatusSafetyLine, Does.Contain("canClaimProduction=False"));
            Assert.That(_root.ResearchVerificationBoundaryLine, Does.Contain("resolved=False error=1 pending=False hasProgressState=False hasCompletedState=True slot= project="));
            Assert.That(_root.ResearchVerificationBoundaryLine, Does.Not.Contain("project=research.project.scaffold"));
            Assert.That(_root.ResearchVerificationSafetyLine, Does.Contain("wouldCallServer=False"));
            Assert.That(_root.Save.lastOfflineSummary.WouldProcessOfflineProgress, Is.False);
        }

        [Test]
        public void ClearResearchPendingScaffold_ReturnsEligibilityToNoPendingWithoutStaleProject()
        {
            InvokeResearchProgressApply();
            InvokeResearchProgressApply();
            _root.RefreshOfflineSummaryLines();

            Assert.That(_root.ClearResearchPendingScaffold(), Is.True);

            Assert.That(_root.ResearchCompletionEligibilityLine, Does.Contain("resolved=False error=1 pending=False hasState=False slot= project="));
            Assert.That(_root.ResearchCompletionEligibilityLine, Does.Not.Contain("research.project.scaffold"));
            Assert.That(_root.ResearchCompletionClaimReadinessLine, Does.Contain("resolved=False error=1 pending=False hasState=False slot= project="));
            Assert.That(_root.ResearchCompletionClaimReadinessLine, Does.Not.Contain("research.project.scaffold"));
            Assert.That(_root.CompletedResearchStateLine, Does.Contain("currentPendingProject= currentProgressProject="));
            Assert.That(_root.CompletedResearchStateLine, Does.Not.Contain("research.project.scaffold"));
            Assert.That(_root.ResearchCompletionClaimApplyLine, Does.Contain("pending=False hasProgressState=False"));
            Assert.That(_root.ResearchCompletionClaimApplyLine, Does.Not.Contain("research.project.scaffold"));
            string statusPresentation = _root.ResearchStatusPresentationLine;
            Assert.That(statusPresentation, Does.Contain("state=NoResearch"));
            Assert.That(statusPresentation, Does.Contain("pending=False"));
            Assert.That(statusPresentation, Does.Contain("hasProgressState=False"));
            Assert.That(statusPresentation, Does.Contain("hasCompletedState=False"));
            Assert.That(statusPresentation, Does.Contain("slot="));
            Assert.That(statusPresentation, Does.Contain("project="));
            Assert.That(statusPresentation, Does.Contain(" slot= project="));
            Assert.That(_root.ResearchStatusSafetyLine, Does.Contain("canClaimProduction=False"));
            Assert.That(_root.ResearchStatusSafetyLine, Does.Contain("wouldGrantRewards=False"));
            Assert.That(_root.ResearchStatusSafetyLine, Does.Contain("wouldUnlockContent=False"));
            Assert.That(_root.ResearchStatusSafetyLine, Does.Contain("wouldChargeCosts=False"));
            Assert.That(_root.ResearchStatusSafetyLine, Does.Contain("wouldProcessOfflineProgress=False"));
            Assert.That(_root.ResearchVerificationBoundaryLine, Does.Contain("pending=False hasProgressState=False"));
            Assert.That(_root.ResearchVerificationSafetyLine, Does.Contain("wouldProcessOfflineProgress=False"));
            Assert.That(statusPresentation, Does.Not.Contain("research.project.scaffold"));
            Assert.That(_root.ResearchVerificationBoundaryLine, Does.Not.Contain("research.project.scaffold"));
            Assert.That(_root.Save.researchPending, Is.Null);
            Assert.That(_root.Save.researchProgress, Is.Null);
        }

        [Test]
        public void RefreshOfflineSummaryLines_PopulatedCompletedResearchShowsCountLastProjectAndReadOnlyDuplicatePreview()
        {
            _root.Save.completedResearch = new CompletedResearchState
            {
                ProjectIds = new[] { "research.project.scaffold", "research.project.other", "research.project.scaffold" },
                LastCompletedProjectId = "research.project.other",
                LastCompletionRuleSourceId = "research.completed.rule.test"
            };

            _root.RefreshOfflineSummaryLines();

            Assert.That(_root.CompletedResearchStateLine, Does.Contain("hasState=True completedCount=2 lastCompletedProject=research.project.other currentPendingProject=research.project.scaffold currentProgressProject=research.project.scaffold currentProjectAlreadyCompleted=True wouldBlockClaimAsDuplicate=False wouldGrantRewards=False wouldUnlockContent=False ruleSource=research.completed.rule.test"));
            Assert.That(_root.ResearchStatusPresentationLine, Does.Contain("state=BlockedOrInvalid"));
            Assert.That(_root.ResearchVerificationBoundaryLine, Does.Contain("alreadyCompleted=True"));
            Assert.That(_root.ResearchVerificationBoundaryLine, Does.Contain("canClaimProduction=False"));
            Assert.That(_root.ResearchStatusPresentationLine, Does.Not.Contain("state=ActiveInProgress"));
            Assert.That(_root.ResearchStatusSafetyLine, Does.Contain("canClaimProduction=False"));
        }

        [Test]
        public void RefreshOfflineSummaryLines_MissingLocalizationUsesSafeFallbackKey()
        {
            StringMap().Remove("ui.dev.research_completion_eligibility_format");
            StringMap().Remove("ui.dev.research_completion_pending_apply_format");
            StringMap().Remove("ui.dev.research_completion_claim_readiness_format");
            StringMap().Remove("ui.dev.completed_research_state_format");
            StringMap().Remove("ui.dev.research_completion_claim_apply_format");
            StringMap().Remove("ui.dev.research_status_presentation_format");
            StringMap().Remove("ui.dev.research_status_safety_format");
            StringMap().Remove("ui.dev.research_verification_boundary_format");
            StringMap().Remove("ui.dev.research_verification_safety_format");

            _root.RefreshOfflineSummaryLines();

            Assert.That(_root.ResearchCompletionEligibilityLine, Is.EqualTo("ui.dev.research_completion_eligibility_format"));
            Assert.That(_root.ResearchCompletionPendingApplyLine, Is.EqualTo("ui.dev.research_completion_pending_apply_format"));
            Assert.That(_root.ResearchCompletionClaimReadinessLine, Is.EqualTo("ui.dev.research_completion_claim_readiness_format"));
            Assert.That(_root.CompletedResearchStateLine, Is.EqualTo("ui.dev.completed_research_state_format"));
            Assert.That(_root.ResearchCompletionClaimApplyLine, Is.EqualTo("ui.dev.research_completion_claim_apply_format"));
            Assert.That(_root.ResearchStatusPresentationLine, Is.EqualTo("ui.dev.research_status_presentation_format"));
            Assert.That(_root.ResearchStatusSafetyLine, Is.EqualTo("ui.dev.research_status_safety_format"));
            Assert.That(_root.ResearchVerificationBoundaryLine, Is.EqualTo("ui.dev.research_verification_boundary_format"));
            Assert.That(_root.ResearchVerificationSafetyLine, Is.EqualTo("ui.dev.research_verification_safety_format"));
        }

        [Test]
        public void RefreshOfflineSummaryLines_DoesNotMutateSave()
        {
            string before = JsonUtility.ToJson(_root.Save);

            _root.RefreshOfflineSummaryLines();

            Assert.That(JsonUtility.ToJson(_root.Save), Is.EqualTo(before));
            Assert.That(_root.Save.researchProgress.CompletionPending, Is.False);
        }

        [Test]
        public void RuntimeCSharp_DoesNotHardcodeVisibleEligibilityDiagnosticEnglish()
        {
            string gameRoot = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/Core/GameRoot.cs"));
            string overlay = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/UI/BootstrapOverlay.cs"));

            Assert.That(gameRoot, Does.Not.Contain("Research Completion Eligibility —"));
            Assert.That(overlay, Does.Not.Contain("Research Completion Eligibility —"));
            Assert.That(gameRoot, Does.Not.Contain("Research Completion Pending Apply —"));
            Assert.That(overlay, Does.Not.Contain("Research Completion Pending Apply —"));
            Assert.That(gameRoot, Does.Not.Contain("Research Completion Claim Readiness —"));
            Assert.That(overlay, Does.Not.Contain("Research Completion Claim Readiness —"));
            Assert.That(gameRoot, Does.Not.Contain("Completed Research State —"));
            Assert.That(overlay, Does.Not.Contain("Completed Research State —"));
            Assert.That(gameRoot, Does.Not.Contain("Research Completion Claim Apply —"));
            Assert.That(overlay, Does.Not.Contain("Research Completion Claim Apply —"));
            Assert.That(gameRoot, Does.Not.Contain("Research Status Presentation —"));
            Assert.That(overlay, Does.Not.Contain("Research Status Presentation —"));
            Assert.That(gameRoot, Does.Not.Contain("Research Status Safety —"));
            Assert.That(overlay, Does.Not.Contain("Research Status Safety —"));
            Assert.That(gameRoot, Does.Not.Contain("Research Verification Boundary —"));
            Assert.That(overlay, Does.Not.Contain("Research Verification Boundary —"));
            Assert.That(gameRoot, Does.Not.Contain("Research Verification Safety —"));
            Assert.That(overlay, Does.Not.Contain("Research Verification Safety —"));
        }

        private string ResearchDiagnosticsText()
        {
            CycleToResearchDiagnostics();
            while (_overlay.FullDiagnosticsScrollOffset > 0)
            {
                _overlay.ScrollFullDiagnosticsLines(-1);
            }
            var windows = new List<string>();
            while (true)
            {
                _overlay.RefreshOverlayText();
                windows.Add(_overlay.overlayText.text);
                int previousOffset = _overlay.FullDiagnosticsScrollOffset;
                _overlay.ScrollFullDiagnosticsLines(1);
                if (_overlay.FullDiagnosticsScrollOffset == previousOffset)
                {
                    return string.Join("\n", windows);
                }
            }
        }

        private string ResearchStatusDiagnosticsText()
        {
            return DiagnosticsPageText(6) + "\n" + DiagnosticsPageText(7);
        }

        private string ResearchVerificationDiagnosticsText()
        {
            return DiagnosticsPageText(8) + "\n" + DiagnosticsPageText(9);
        }

        private string DiagnosticsPageText(int pageNumber)
        {
            ShowDiagnostics();
            while (_overlay.FullDiagnosticsPageNumber != pageNumber)
            {
                _overlay.CycleFullDiagnosticsPage();
            }
            _overlay.RefreshOverlayText();
            return _overlay.overlayText.text;
        }

        private void CycleToResearchDiagnostics()
        {
            ShowDiagnostics();
            while (_overlay.FullDiagnosticsPageNumber != 5)
            {
                _overlay.CycleFullDiagnosticsPage();
            }
        }

        private void ShowDiagnostics()
        {
            if (!_overlay.DiagnosticsVisible)
            {
                _overlay.ToggleDiagnosticsVisibility();
            }
            _overlay.RefreshOverlayText();
        }

        private void InvokeResearchProgressApply()
        {
            typeof(GameRoot).GetMethod("ApplyResearchProgressForActiveTick", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(_root, null);
        }

        private void InvokeResearchCompletionPendingApply()
        {
            typeof(GameRoot).GetMethod("ApplyResearchCompletionPendingForActiveTick", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(_root, null);
        }

        private void SetBackingField(string name, object value)
        {
            typeof(GameRoot).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_root, value);
        }

        private Dictionary<string, string> StringMap()
        {
            return (Dictionary<string, string>)typeof(ContentService).GetField("_stringMap", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_root.Content);
        }

        private static ContentService BuildContent()
        {
            var content = new ContentService();
            typeof(ContentService).GetField("<Bootstrap>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(content, new ContentBootstrap
                {
                    tickSeconds = 10,
                    researchPendingScaffold = new ResearchPendingScaffoldConfig
                    {
                        enabled = true,
                        slotId = "research.slot.primary",
                        projectId = "research.project.scaffold",
                        ruleSourceId = "research.pending.rule.test"
                    },
                    researchProgressScaffold = new ResearchProgressScaffoldConfig
                    {
                        enabled = true,
                        ruleSourceId = "research.progress.rule.test",
                        progressPerActiveSecond = 0.1d,
                        maxActiveSessionElapsedSeconds = 600
                    },
                    researchCompletionEligibilityScaffold = new ResearchCompletionEligibilityScaffoldConfig
                    {
                        enabled = true,
                        ruleSourceId = "research.completion_eligibility.rule.test",
                        projectId = "research.project.scaffold",
                        requiredProgressUnits = 2d
                    },
                    researchCompletionClaimScaffold = new ResearchCompletionClaimScaffoldConfig
                    {
                        enabled = true,
                        ruleSourceId = "research.completion_claim.rule.test"
                    },
                    researchVerificationScaffold = new ResearchVerificationScaffoldConfig
                    {
                        enabled = true,
                        ruleSourceId = "research.verification.rule.test",
                        verificationMode = "unavailable"
                    },
                    timeRules = new TimeRules
                    {
                        maxOfflineSeconds = 600,
                        offlineSummaryRuleSourceId = "offline.summary.rule.test"
                    }
                });
            var map = (Dictionary<string, string>)typeof(ContentService).GetField("_stringMap", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(content);
            map["ui.dev.hint.toggle_panel"] = "toggle-panel";
            map["ui.dev.hint.toggle_run_diagnostics"] = "toggle-run";
            map["ui.dev.hint.cycle_diagnostics_page"] = "cycle-page";
            map["ui.dev.hint.scroll_diagnostics"] = "scroll-diagnostics";
            map["ui.dev.diagnostics.header_format"] = "Diagnostics: {0} Page {1}/{2}";
            map["ui.dev.diagnostics.page.systems_diagnostics"] = "Systems Diagnostics";
            map["ui.dev.diagnostics.page.research_diagnostics"] = "Research Diagnostics";
            map["ui.dev.diagnostics.page.research_status_presentation_diagnostics"] = "Research Status Presentation Diagnostics";
            map["ui.dev.diagnostics.page.research_status_safety_diagnostics"] = "Research Status Safety Diagnostics";
            map["ui.dev.diagnostics.page.research_verification_boundary_diagnostics"] = "Research Verification Boundary Diagnostics";
            map["ui.dev.diagnostics.page.research_verification_safety_diagnostics"] = "Research Verification Safety Diagnostics";
            map["ui.dev.structure_status"] = "structure {0} {1} {2} {3}";
            map["ui.dev.offline_summary_format"] = "Offline Summary — resolved={0} error={1} observedSeconds={2} clamped={3} wouldProcess={4} ruleSource={5}";
            map["ui.dev.research_pending_format"] = "Research Pending — pending={0} slot={1} project={2}";
            map["ui.dev.research_pending_validation_format"] = "Research Pending Validation — resolved={0} error={1} ruleSource={2}";
            map["ui.dev.research_progress_format"] = "Research Progress Preview — resolved={0} error={1} pending={2} slot={3} project={4} elapsedSeconds={5} delta={6:0.###} wouldComplete={7} ruleSource={8}";
            map["ui.dev.research_progress_state_format"] = "Research Progress State — resolved={0} error={1} pending={2} hasState={3} slot={4} project={5} progress={6:0.###} completionPending={7} matchesPending={8} ruleSource={9}";
            map["ui.dev.research_completion_eligibility_format"] = "Research Completion Eligibility — resolved={0} error={1} pending={2} hasState={3} slot={4} project={5} progress={6:0.###} required={7:0.###} remaining={8:0.###} eligible={9} wouldSetCompletionPending={10} wouldComplete={11} ruleSource={12}";
            map["ui.dev.research_completion_pending_apply_format"] = "Research Completion Pending Apply — resolved={0} error={1} pending={2} hasState={3} slot={4} project={5} progress={6:0.###} required={7:0.###} eligible={8} alreadyCompletionPending={9} wouldSetCompletionPending={10} wouldComplete={11} ruleSource={12}";
            map["ui.dev.research_completion_claim_readiness_format"] = "Research Completion Claim Readiness — resolved={0} error={1} pending={2} hasState={3} slot={4} project={5} progress={6:0.###} required={7:0.###} completionPending={8} eligible={9} readyForClaim={10} wouldComplete={11} wouldGrantRewards={12} wouldUnlockContent={13} wouldClearPending={14} ruleSource={15}";
            map["ui.dev.completed_research_state_format"] = "Completed Research State — resolved={0} error={1} hasState={2} completedCount={3} lastCompletedProject={4} currentPendingProject={5} currentProgressProject={6} currentProjectAlreadyCompleted={7} wouldBlockClaimAsDuplicate={8} wouldGrantRewards={9} wouldUnlockContent={10} ruleSource={11}";
            map["ui.dev.research_completion_claim_apply_format"] = "Research Completion Claim Apply — resolved={0} error={1} pending={2} hasProgressState={3} hasCompletedState={4} slot={5} project={6} progress={7:0.###} required={8:0.###} completionPending={9} eligible={10} readyForClaim={11} alreadyCompleted={12} wouldRecordCompletedResearch={13} wouldClearPending={14} wouldClearProgress={15} wouldGrantRewards={16} wouldUnlockContent={17} wouldChargeCosts={18} wouldProcessOfflineProgress={19} ruleSource={20}";
            map["ui.dev.research_status_presentation_format"] = "Research Status Presentation — state={0} pending={1} hasProgressState={2} hasCompletedState={3} slot={4} project={5} progress={6:0.###} required={7:0.###} completionPending={8} eligible={9} verificationRequired={10} readyToClaim={11} completed={12} blockedOrInvalid={13} statusKey={14} ruleSource={15}";
            map["ui.dev.research_status_safety_format"] = "Research Status Safety — canClaimProduction={0} wouldGrantRewards={1} wouldUnlockContent={2} wouldChargeCosts={3} wouldProcessOfflineProgress={4}";
            map["ui.dev.research_verification_boundary_format"] = "Research Verification Boundary — resolved={0} error={1} pending={2} hasProgressState={3} hasCompletedState={4} slot={5} project={6} progress={7:0.###} required={8:0.###} completionPending={9} eligible={10} alreadyCompleted={11} verificationRequired={12} verificationAvailable={13} verificationSatisfied={14} canClaimProduction={15} verificationMode={16} ruleSource={17}";
            map["ui.dev.research_verification_safety_format"] = "Research Verification Safety — wouldCallServer={0} wouldGrantRewards={1} wouldUnlockContent={2} wouldChargeCosts={3} wouldProcessOfflineProgress={4}";
            return content;
        }

        private static SaveData BuildSave()
        {
            return new SaveData
            {
                totalTicks = 19,
                structureRuntime = new StructureRuntimeState { Heat = 17d, ManaReserve = 23d, LastProcessedTick = 11 },
                runHistory = new RunHistoryState(),
                researchPending = new ResearchPendingState { SlotId = "research.slot.primary", ProjectId = "research.project.scaffold" },
                researchProgress = new ResearchProgressState
                {
                    SlotId = "research.slot.primary",
                    ProjectId = "research.project.scaffold",
                    RuleSourceIdUsed = "research.progress.rule.test"
                },
                lastOfflineSummary = new OfflineSummary
                {
                    RuleResolved = true,
                    OfflineSecondsObserved = 60,
                    WouldProcessOfflineProgress = false,
                    RuleSourceIdUsed = "offline.summary.rule.test"
                }
            };
        }
    }
}
