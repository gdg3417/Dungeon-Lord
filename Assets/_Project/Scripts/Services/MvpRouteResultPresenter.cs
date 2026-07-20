using System;

namespace DungeonBuilder.M0
{
    public static class MvpRouteResultPresenter
    {
        public const string RouteFormatKey = "ui.mvp_loop.route.result_format";
        public const string DepthFormatKey = "ui.mvp_loop.route.depth_format";
        public const string RoomNumberFormatKey = "ui.mvp_loop.route.room_number_format";

        public static string BuildCompactText(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || summary.ConfiguredRoomCount <= 1 || string.IsNullOrWhiteSpace(summary.FinalRouteOutcomeKey)) return string.Empty;
            string result = string.Format(Localize(localize, RouteFormatKey), Localize(localize, summary.FinalRouteOutcomeKey));
            string room = string.Format(Localize(localize, RoomNumberFormatKey), summary.HighestRoomReached + 1);
            return result + "\n" + string.Format(Localize(localize, DepthFormatKey), room);
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            return localize != null ? localize(key, key) : key;
        }
    }
}
