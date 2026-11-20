using System;

namespace DungeonBuilder.M0
{
    public static class PlayerResearchStatusTextPresenter
    {
        public static string Present(
            string statusLocalizationKey,
            PlayerResearchAuthoritySummary authority,
            Func<string, string, string> localize,
            IFormatProvider culture = null)
        {
            string key = string.IsNullOrWhiteSpace(statusLocalizationKey)
                ? PlayerResearchActionHandler.BlockedInvalidKey
                : statusLocalizationKey;
            string localized = localize == null ? key : localize(key, key);
            if (key != PlayerResearchActionHandler.InProgressFormatKey || authority?.State != PlayerResearchAuthorityState.InProgress)
            {
                return localized;
            }

            if (authority.DurationSeconds is double duration && authority.RemainingSeconds is double remaining)
            {
                const string timedKey = "ui.research.timed_progress";
                return string.Format(culture, localize?.Invoke(timedKey, timedKey) ?? timedKey,
                    Math.Floor(duration - remaining), duration, Math.Ceiling(remaining));
            }
            return string.Format(culture, localized, authority.ProgressUnits, authority.RequiredProgressUnits);
        }
    }
}
