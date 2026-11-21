using UnityEngine.UIElements;

namespace DungeonBuilder.M0
{
    public sealed partial class ProductionDungeonController
    {
        public bool IsResearch { get; private set; }
        private bool researchRecoveryRequired;
        private Label researchStatus;
        private ResearchProgressState presentedResearchProgress;
        private long researchDurationSeconds, lastResearchElapsed = -1;

        private void InitializeResearch()
        {
            researchStatus = document.rootVisualElement.Q<Label>("researchStatus");
            researchStatus.schedule.Execute(RefreshResearchCountdown).Every(100);
            Button("openResearch", OpenResearch);
            Button("researchDungeon", ReturnToDungeon);
            Button("researchDisplay", () => document.rootVisualElement.Q("displayPopover").style.display = DisplayStyle.Flex);
            Button("researchStart", () => ApplyResearchAction(claim: false));
            Button("researchClaim", () => ApplyResearchAction(claim: true));
        }

        public void OpenResearch() => OpenDungeonDestination(ShowResearch, "ui.research.leave_edit", "ui.research.leave_invalid");

        private void ShowResearch()
        {
            IsResearch = true;
            document.rootVisualElement.Q<Label>("researchFeedback").text = string.Empty;
            ShowDungeonDestination("researchScreen", "researchDungeon");
            PresentResearch();
        }

        private void ApplyResearchAction(bool claim)
        {
            if (!IsResearch || !HasCanonicalRuntime() || ResearchInteractionBlocked ||
                document.rootVisualElement.Q("displayPopover").style.display == DisplayStyle.Flex) return;
            var result = claim ? root.ClaimConfiguredPlayerResearch() : root.StartProductionResearch();
            PresentResearch();
            document.rootVisualElement.Q<Label>("researchFeedback").text =
                PlayerResearchStatusTextPresenter.Present(result.FeedbackLocalizationKey, result.Authority, (key, _) => Text(key));
        }

        private void PresentResearch()
        {
            if (!IsResearch) return;
            var ui = document.rootVisualElement;
            var state = root.ResolveProductionResearchState();
            var authority = state.Authority;
            researchRecoveryRequired = state.RecoveryRequired;
            ui.Q<Label>("researchProject").text = Text("ui.research.starter.name");
            bool completedRun = authority?.HasCompletedRun ?? RunHistoryState.HasCompletedAdventurerRun(root.Save.runHistory);
            ui.Q<Label>("researchPrerequisite").text = Text(completedRun ?
                "ui.research.prerequisite_met" : "ui.player_research.blocked.prerequisite");
            bool started = state.State == PlayerResearchState.InProgress || state.State == PlayerResearchState.ReadyToClaim ||
                authority?.State == PlayerResearchAuthorityState.ClaimBlocked;
            bool completed = state.State == PlayerResearchState.Completed;
            ui.Q<Label>("researchCost").text = authority?.ManaCost is double cost && !completed
                ? Format(started ? "ui.research.paid_cost" : "ui.research.start_cost", cost)
                : started || completed ? string.Empty : Text(PlayerResearchActionHandler.CostUnavailableKey);
            ui.Q<Label>("researchDuration").text = authority?.DurationSeconds is double duration && !completed
                ? Format("ui.research.duration", duration) : string.Empty;
            ui.Q<Label>("researchAffordability").text = !completed && authority?.UsableMana is double usable
                ? Format(authority.IsAffordable ? "ui.research.affordable" : "ui.research.unaffordable", usable)
                : string.Empty;
            string status = PlayerResearchStatusTextPresenter.Present(state.FeedbackLocalizationKey, authority,
                (key, _) => Text(key), Culture);
            if (authority?.State == PlayerResearchAuthorityState.ClaimBlocked)
                status = Text(PlayerResearchActionHandler.ReadyToClaimKey) + "\n" + status;
            if (completed)
            {
                var unlock = root.ResolveMvpPlayerLoopSummary();
                if (unlock.HasResearchUnlock) status += "\n" + Text(unlock.ResearchUnlockSummaryKey);
            }
            presentedResearchProgress = root.Save.researchProgress;
            researchDurationSeconds = authority?.State == PlayerResearchAuthorityState.InProgress &&
                authority.DurationSeconds is double activeDuration && authority.RemainingSeconds.HasValue
                ? (long)activeDuration : 0;
            lastResearchElapsed = -1;
            researchStatus.text = status == ui.Q<Label>("researchPrerequisite").text ||
                status == ui.Q<Label>("researchCost").text ? Text("ui.research.start_blocked") : status;
            ui.Q<Button>("researchStart").style.display = authority?.CanStart == true ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Q<Button>("researchClaim").style.display = state.CanClaimLocalMvp ? DisplayStyle.Flex : DisplayStyle.None;
            PresentResearchCountdown();
        }

        private void RefreshResearchCountdown()
        {
            if (!IsResearch || !HasCanonicalRuntime()) return;
            if (presentedResearchProgress != root.Save.researchProgress)
                PresentResearch();
            else
                PresentResearchCountdown();
        }

        private void PresentResearchCountdown()
        {
            int unitsPerSecond = root.SaveService.ForegroundConfiguration?.UnitsPerSecond ?? 0;
            if (researchDurationSeconds <= 0 || presentedResearchProgress == null || unitsPerSecond <= 0) return;
            long elapsed = PlayerResearchStatusTextPresenter.DisplayElapsedSeconds(presentedResearchProgress,
                unitsPerSecond, researchDurationSeconds, root.ReadResearchPresentationHorizon());
            if (elapsed == lastResearchElapsed) return;
            lastResearchElapsed = elapsed;
            researchStatus.text = PlayerResearchStatusTextPresenter.PresentCountdown(elapsed,
                researchDurationSeconds, Text, Culture);
        }

        private bool ResearchInteractionBlocked => IsEditing || modal.style.display == DisplayStyle.Flex;
    }
}
