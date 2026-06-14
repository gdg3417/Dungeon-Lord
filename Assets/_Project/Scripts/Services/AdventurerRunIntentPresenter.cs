using System;

namespace DungeonBuilder.M0
{
    public static class AdventurerRunIntentPresenter
    {
        public const string SummaryFormatKey = "ui.adventurer_intent.summary_format";
        public const string BodyFormatKey = "ui.adventurer_intent.body_format";
        public const string ScoreSummaryFormatKey = "ui.adventurer_intent.score_summary_format";
        public const string DebugPostureFormatKey = "ui.adventurer_intent.debug_posture_format";

        public static string BuildSummaryLine(AdventurerRunIntentSummary summary, Func<string, string, string> localize)
        {
            string postureNameKey = ResolvePostureNameKey(summary?.IntentId);
            string reasonKey = summary != null && summary.RuleResolved && !string.IsNullOrWhiteSpace(summary.PrimaryReasonKey) ? summary.PrimaryReasonKey : AdventurerRunIntentResolver.ReasonFallbackKey;
            return string.Format(Localize(localize, SummaryFormatKey), Localize(localize, postureNameKey), Localize(localize, reasonKey));
        }

        public static string BuildBodyLine(AdventurerRunIntentSummary summary, Func<string, string, string> localize)
        {
            string postureNameKey = ResolvePostureNameKey(summary?.IntentId);
            string reasonKey = summary != null && summary.RuleResolved && !string.IsNullOrWhiteSpace(summary.PrimaryReasonKey) ? summary.PrimaryReasonKey : AdventurerRunIntentResolver.ReasonFallbackKey;
            return string.Format(Localize(localize, BodyFormatKey), Localize(localize, postureNameKey), Localize(localize, reasonKey));
        }

        public static string BuildScoreSummaryLine(AdventurerRunIntentSummary summary, Func<string, string, string> localize)
        {
            double cautious = summary != null && summary.RuleResolved ? summary.CautiousScore : 0d;
            double balanced = summary != null && summary.RuleResolved ? summary.BalancedScore : 0d;
            double greedy = summary != null && summary.RuleResolved ? summary.GreedyScore : 0d;
            return string.Format(Localize(localize, ScoreSummaryFormatKey), cautious, balanced, greedy);
        }

        public static string BuildDebugPostureLine(AdventurerRunIntentSummary summary, string selectedRunPostureName, Func<string, string, string> localize)
        {
            string postureNameKey = ResolvePostureNameKey(summary?.IntentId);
            string debugPosture = string.IsNullOrWhiteSpace(selectedRunPostureName) ? Localize(localize, "run.posture.balanced.name") : selectedRunPostureName;
            return string.Format(Localize(localize, DebugPostureFormatKey), Localize(localize, postureNameKey), debugPosture);
        }

        private static string ResolvePostureNameKey(string postureId)
        {
            if (string.Equals(postureId, RunPostureResolver.CautiousId, StringComparison.Ordinal)) return "run.posture.cautious.name";
            if (string.Equals(postureId, RunPostureResolver.GreedyId, StringComparison.Ordinal)) return "run.posture.greedy.name";
            return "run.posture.balanced.name";
        }

        private static string Localize(Func<string, string, string> localize, string key) => localize == null ? key : localize(key, key);
    }
}
