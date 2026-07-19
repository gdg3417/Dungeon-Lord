using System;

namespace DungeonBuilder.M0
{
    public sealed class PlayerResearchPanelPresentation
    {
        public string StatusText = string.Empty;
        public string ActionText = string.Empty;
        public bool ShowAction;
        public bool ActionClaimsResearch;
    }

    public static class PlayerResearchPanelPresenter
    {
        public const string StartActionKey = "ui.player_research.action.start";
        public const string ClaimActionKey = "ui.player_research.action.claim";

        public static PlayerResearchPanelPresentation Present(PlayerResearchActionResult state, Func<string, string, string> localize)
        {
            state = state ?? new PlayerResearchActionResult { State = PlayerResearchState.Blocked, FeedbackLocalizationKey = PlayerResearchActionHandler.BlockedInvalidKey };
            string status = PlayerResearchStatusTextPresenter.Present(
                state.FeedbackLocalizationKey,
                state.Authority,
                localize);
            return new PlayerResearchPanelPresentation
            {
                StatusText = status,
                ShowAction = state.State == PlayerResearchState.Available || state.State == PlayerResearchState.ReadyToClaim,
                ActionClaimsResearch = state.State == PlayerResearchState.ReadyToClaim,
                ActionText = state.State == PlayerResearchState.ReadyToClaim
                    ? Localize(localize, ClaimActionKey)
                    : state.State == PlayerResearchState.Available ? Localize(localize, StartActionKey) : string.Empty
            };
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            return localize == null ? key ?? string.Empty : localize(key ?? string.Empty, key ?? string.Empty);
        }
    }
}
