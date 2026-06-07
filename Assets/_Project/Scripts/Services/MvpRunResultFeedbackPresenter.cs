using System;

namespace DungeonBuilder.M0
{
    public static class MvpRunResultFeedbackPresenter
    {
        public const string SuccessStableHeatKey = "ui.mvp_run_feedback.success_stable_heat";
        public const string SuccessHeatReducedKey = "ui.mvp_run_feedback.success_heat_reduced";
        public const string SuccessHeatIncreasedKey = "ui.mvp_run_feedback.success_heat_increased";
        public const string FailedKey = "ui.mvp_run_feedback.failed";
        public const string UnavailableKey = "ui.mvp_run_feedback.unavailable";
        public const string FormatKey = "ui.mvp_run_feedback.format";

        public static string BuildFeedbackText(
            MvpPlayerLoopSummary beforeRunSummary,
            MvpPlayerLoopSummary afterRunSummary,
            bool didRun,
            Func<string, string, string> localize)
        {
            if (!didRun || afterRunSummary == null || !afterRunSummary.RuleResolved || !afterRunSummary.HasRunOutcome)
            {
                return Localize(localize, UnavailableKey);
            }

            string interpretation = Localize(localize, ResolveInterpretationKey(afterRunSummary));
            string format = Localize(localize, FormatKey);
            return string.Format(
                format,
                interpretation,
                afterRunSummary.ManaReserve,
                afterRunSummary.LootGeneratedWorldValue,
                afterRunSummary.LootExtractedWorldValue,
                afterRunSummary.LootExtractedTradeableWorldValue,
                afterRunSummary.HeatBefore,
                afterRunSummary.HeatAfter);
        }

        private static string ResolveInterpretationKey(MvpPlayerLoopSummary summary)
        {
            if (!summary.RunSucceeded)
            {
                return FailedKey;
            }

            if (summary.HeatAfter < summary.HeatBefore)
            {
                return SuccessHeatReducedKey;
            }

            if (summary.HeatAfter > summary.HeatBefore)
            {
                return SuccessHeatIncreasedKey;
            }

            return SuccessStableHeatKey;
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            if (localize == null)
            {
                return key;
            }

            return localize(key, key);
        }
    }
}
