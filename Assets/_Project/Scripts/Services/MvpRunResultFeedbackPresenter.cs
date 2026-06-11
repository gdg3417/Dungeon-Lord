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
        public const string OutcomeCueFailedKey = "ui.mvp_run_feedback.outcome_cue.failed";
        public const string OutcomeCueHeatIncreasedKey = "ui.mvp_run_feedback.outcome_cue.heat_increased";
        public const string OutcomeCueControlledLootKey = "ui.mvp_run_feedback.outcome_cue.controlled_loot";
        public const string OutcomeCueFormatKey = "ui.mvp_run_feedback.outcome_cue.format";
        public const string FormatKey = "ui.mvp_run_feedback.format";
        public const string FormatWithPartyKey = "ui.mvp_run_feedback.format_with_party";
        public const string PostureFormatKey = "ui.mvp_run_feedback.posture_format";
        public const string PlacementEffectsImpactFormatKey = "ui.mvp_run_feedback.placement_effects_impact_format";
        public const string PartyPreviewFormatKey = "ui.mvp_adventurer_party.preview_format";

        public static string BuildFeedbackText(
            MvpPlayerLoopSummary beforeRunSummary,
            MvpPlayerLoopSummary afterRunSummary,
            bool didRun,
            Func<string, string, string> localize,
            string postureNameKey = null)
        {
            if (!didRun || afterRunSummary == null || !afterRunSummary.RuleResolved || !afterRunSummary.HasRunOutcome)
            {
                return Localize(localize, UnavailableKey);
            }

            string interpretation = Localize(localize, ResolveInterpretationKey(afterRunSummary));
            string outcomeCue = BuildOutcomeCue(beforeRunSummary, afterRunSummary, localize);
            string partyPreview = BuildPartyPreview(afterRunSummary, localize);
            string placementEffects = BuildPlacementEffectsImpact(afterRunSummary, localize);
            string format = string.IsNullOrEmpty(partyPreview)
                ? Localize(localize, FormatKey)
                : Localize(localize, FormatWithPartyKey);
            if (string.IsNullOrEmpty(partyPreview))
            {
                return ApplyPosturePrefix(
                    ApplyPlacementEffectsImpact(
                        AppendOutcomeCue(
                            string.Format(
                                format,
                                interpretation,
                                afterRunSummary.ManaReserve,
                                afterRunSummary.LootGeneratedWorldValue,
                                afterRunSummary.LootExtractedWorldValue,
                                afterRunSummary.LootExtractedTradeableWorldValue,
                                afterRunSummary.HeatBefore,
                                afterRunSummary.HeatAfter),
                            outcomeCue,
                            localize),
                        placementEffects,
                        localize),
                    postureNameKey,
                    localize);
            }

            return ApplyPosturePrefix(
                ApplyPlacementEffectsImpact(
                    AppendOutcomeCue(
                        string.Format(
                            format,
                            interpretation,
                            afterRunSummary.ManaReserve,
                            afterRunSummary.LootGeneratedWorldValue,
                            afterRunSummary.LootExtractedWorldValue,
                            afterRunSummary.LootExtractedTradeableWorldValue,
                            afterRunSummary.HeatBefore,
                            afterRunSummary.HeatAfter,
                            partyPreview),
                        outcomeCue,
                        localize),
                    placementEffects,
                    localize),
                postureNameKey,
                localize);
        }

        public static string BuildPartyPreview(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.AdventurerPartyPreviewResolved || summary.AdventurerPartyClassIds == null || summary.AdventurerPartyClassIds.Length == 0)
            {
                return string.Empty;
            }

            string[] labels = new string[summary.AdventurerPartyClassIds.Length];
            for (int i = 0; i < summary.AdventurerPartyClassIds.Length; i++)
            {
                labels[i] = AdventurerPartyCompositionResolver.ResolveClassLabel(summary.AdventurerPartyClassIds[i], localize);
            }

            return string.Format(Localize(localize, PartyPreviewFormatKey), string.Join(", ", labels));
        }

        private static string BuildPlacementEffectsImpact(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            return MvpPlacementEffectsPresenter.HasAnyEffect(summary?.PlacementEffects)
                ? MvpPlacementEffectsPresenter.BuildEffectsText(summary.PlacementEffects, localize)
                : string.Empty;
        }

        private static string BuildOutcomeCue(MvpPlayerLoopSummary beforeRunSummary, MvpPlayerLoopSummary afterRunSummary, Func<string, string, string> localize)
        {
            if (afterRunSummary == null || !afterRunSummary.RuleResolved || !afterRunSummary.HasRunOutcome)
            {
                return string.Empty;
            }

            if (!afterRunSummary.RunSucceeded)
            {
                return Localize(localize, OutcomeCueFailedKey);
            }

            if (afterRunSummary.HeatAfter > afterRunSummary.HeatBefore)
            {
                return Localize(localize, OutcomeCueHeatIncreasedKey);
            }

            if (afterRunSummary.HeatAfter <= afterRunSummary.HeatBefore && afterRunSummary.LootExtractedWorldValue > 0)
            {
                return Localize(localize, OutcomeCueControlledLootKey);
            }

            return string.Empty;
        }

        private static string AppendOutcomeCue(string feedbackText, string outcomeCue, Func<string, string, string> localize)
        {
            if (string.IsNullOrWhiteSpace(outcomeCue))
            {
                return feedbackText;
            }

            return string.Format(Localize(localize, OutcomeCueFormatKey), feedbackText, outcomeCue);
        }

        private static string ApplyPlacementEffectsImpact(string feedbackText, string placementEffects, Func<string, string, string> localize)
        {
            if (string.IsNullOrWhiteSpace(placementEffects))
            {
                return feedbackText;
            }

            return string.Format(Localize(localize, PlacementEffectsImpactFormatKey), feedbackText, placementEffects);
        }

        private static string ApplyPosturePrefix(string feedbackText, string postureNameKey, Func<string, string, string> localize)
        {
            if (string.IsNullOrWhiteSpace(postureNameKey))
            {
                return feedbackText;
            }

            string postureName = Localize(localize, postureNameKey);
            string format = Localize(localize, PostureFormatKey);
            return string.Format(format, postureName, feedbackText);
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
