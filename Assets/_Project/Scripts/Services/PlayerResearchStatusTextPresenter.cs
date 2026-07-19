using System;

namespace DungeonBuilder.M0
{
    public static class PlayerResearchStatusTextPresenter
    {
        public static string Present(
            string statusLocalizationKey,
            PlayerResearchAuthoritySummary authority,
            Func<string, string, string> localize)
        {
            string key = string.IsNullOrWhiteSpace(statusLocalizationKey)
                ? PlayerResearchActionHandler.BlockedInvalidKey
                : statusLocalizationKey;
            string localized = localize == null ? key : localize(key, key);
            if (authority == null || authority.State != PlayerResearchAuthorityState.InProgress)
            {
                return localized;
            }

            return string.Format(localized, authority.ProgressUnits, authority.RequiredProgressUnits);
        }
    }
}
