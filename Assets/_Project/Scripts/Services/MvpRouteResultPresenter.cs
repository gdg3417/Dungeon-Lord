using System;
using System.Linq;

namespace DungeonBuilder.M0
{
    public static class MvpRouteResultPresenter
    {
        public const string RouteFormatKey = "ui.mvp_loop.route.result_format";
        public const string DepthFormatKey = "ui.mvp_loop.route.depth_format";
        public const string RoomNumberFormatKey = "ui.mvp_loop.route.room_number_format";
        public const string FloorReachFormatKey = "ui.mvp_loop.route.floor_reach_format";
        public const string TransitionContextFormatKey = "ui.mvp_loop.route.transition_context_format";
        public const string KnowledgeBothKey = "ui.mvp_loop.route.knowledge_both";
        public const string KnowledgeRewardKey = "ui.mvp_loop.route.knowledge_reward";
        public const string KnowledgeDangerKey = "ui.mvp_loop.route.knowledge_danger";
        public const string KnowledgeUnknownKey = "ui.mvp_loop.route.knowledge_unknown";

        public static string BuildCompactText(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || string.IsNullOrWhiteSpace(summary.FinalRouteOutcomeKey)) return string.Empty;
            bool hasFloorEvidence = summary.FloorTransitions != null && summary.FloorTransitions.Length > 0;
            int reachedFloor = (summary.RoomResolutions ?? Array.Empty<RunRoomResolutionSummary>())
                .Where(room => room != null && room.Reached).Select(room => room.FloorIndex + 1).DefaultIfEmpty(0).Max();
            if (summary.ConfiguredRoomCount <= 1 && !hasFloorEvidence && reachedFloor == 0) return string.Empty;
            string result = string.Format(Localize(localize, RouteFormatKey), Localize(localize, summary.FinalRouteOutcomeKey));
            string room = string.Format(Localize(localize, RoomNumberFormatKey), summary.HighestRoomReached + 1);
            string report = result + "\n" + string.Format(Localize(localize, DepthFormatKey), room);
            if (reachedFloor > 0)
                report += "\n" + string.Format(Localize(localize, FloorReachFormatKey), reachedFloor);
            if (hasFloorEvidence)
            {
                foreach (var transition in summary.FloorTransitions)
                {
                    if (transition == null) continue;
                    if (transition.NextFloorInstanceId == null)
                    {
                        report += "\n" + Localize(localize, transition.Reason);
                        continue;
                    }
                    string knowledge = Localize(localize, transition.RewardKnown && transition.DangerKnown ? KnowledgeBothKey :
                        transition.RewardKnown ? KnowledgeRewardKey : transition.DangerKnown ? KnowledgeDangerKey : KnowledgeUnknownKey);
                    report += "\n" + string.Format(Localize(localize, TransitionContextFormatKey),
                        Localize(localize, transition.Reason), knowledge);
                }
            }
            return report;
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            return localize != null ? localize(key, key) : key;
        }
    }
}
