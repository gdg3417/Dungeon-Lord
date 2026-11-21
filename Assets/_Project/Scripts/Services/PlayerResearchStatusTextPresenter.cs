using System;

namespace DungeonBuilder.M0
{
    public static class PlayerResearchStatusTextPresenter
    {
        internal const string AwaitingTickKey = "ui.research.awaiting_completion_tick";
        private const string TimedProgressKey = "ui.research.timed_progress";

        internal static long DisplayElapsedSeconds(ResearchProgressState progress, int unitsPerSecond,
            long durationSeconds, long? observedThrough)
        {
            long committed = (long)progress.ProgressUnits;
            long elapsed = observedThrough is long through && progress.StartClosedThrough >= 0 &&
                through >= progress.StartClosedThrough
                ? Math.Max(committed, through - progress.StartClosedThrough) : committed;
            return Math.Min(durationSeconds, elapsed / unitsPerSecond);
        }

        internal static string PresentCountdown(long elapsed, long duration, Func<string, string> localize,
            IFormatProvider culture) => elapsed == duration ? localize(AwaitingTickKey) :
            string.Format(culture, localize(TimedProgressKey), elapsed, duration, duration - elapsed);

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
                return string.Format(culture, localize?.Invoke(TimedProgressKey, TimedProgressKey) ?? TimedProgressKey,
                    Math.Floor(duration - remaining), duration, Math.Ceiling(remaining));
            }
            return string.Format(culture, localized, authority.ProgressUnits, authority.RequiredProgressUnits);
        }
    }
}
