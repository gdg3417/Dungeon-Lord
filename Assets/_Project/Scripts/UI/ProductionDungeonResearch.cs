using UnityEngine.UIElements;

namespace DungeonBuilder.M0
{
    public sealed partial class ProductionDungeonController
    {
        public bool IsResearch { get; private set; }
        private bool researchRecoveryRequired;

        private void InitializeResearch()
        {
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
            if (!IsResearch || !HasCanonicalRuntime() || IsEditing) return;
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
            ui.Q<Label>("researchCost").text = authority?.ManaCost is double cost ?
                Format("ui.research.start_cost", cost) : Text("ui.player_research.cost_unavailable");
            ui.Q<Label>("researchAffordability").text = authority?.UsableMana is double usable
                ? Format(authority.IsAffordable ? "ui.research.affordable" : "ui.research.unaffordable", usable)
                : string.Empty;
            string status = authority?.State == PlayerResearchAuthorityState.InProgress
                ? Format(PlayerResearchActionHandler.InProgressFormatKey, state.ProgressUnits, state.RequiredProgressUnits)
                : Text(state.FeedbackLocalizationKey);
            if (authority?.State == PlayerResearchAuthorityState.ClaimBlocked)
                status = Text(PlayerResearchActionHandler.ReadyToClaimKey) + "\n" + status;
            ui.Q<Label>("researchStatus").text = status == ui.Q<Label>("researchPrerequisite").text ||
                status == ui.Q<Label>("researchCost").text ? Text("ui.research.start_blocked") : status;
            ui.Q<Button>("researchStart").style.display = authority?.CanStart == true ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Q<Button>("researchClaim").style.display = state.CanClaimLocalMvp ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
