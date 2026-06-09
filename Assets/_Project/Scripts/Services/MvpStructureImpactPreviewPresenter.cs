using System;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.M0
{
    public static class MvpStructureImpactPreviewPresenter
    {
        public const string ManaGeneratorPreviewKey = "ui.mvp_structure_preview.mana_generator";
        public const string HeatScrubberPreviewKey = "ui.mvp_structure_preview.heat_scrubber";
        public const string RiskLabPreviewKey = "ui.mvp_structure_preview.risk_lab";
        public const string UnknownPreviewKey = "ui.mvp_structure_preview.unknown";
        public const string RunPlanFormatKey = "ui.mvp_run_plan_preview.plan_format";
        public const string RunTradeoffFormatKey = "ui.mvp_run_plan_preview.tradeoff_format";
        public const string RunPlanCombinedFormatKey = "ui.mvp_run_plan_preview.combined_format";
        public const string CautiousRunTradeoffKey = "ui.mvp_run_plan_preview.tradeoff.cautious";
        public const string BalancedRunTradeoffKey = "ui.mvp_run_plan_preview.tradeoff.balanced";
        public const string GreedyRunTradeoffKey = "ui.mvp_run_plan_preview.tradeoff.greedy";
        public const string UnknownRunTradeoffKey = "ui.mvp_run_plan_preview.tradeoff.unknown";

        public static string ResolvePreviewKey(string structureId)
        {
            switch (structureId)
            {
                case StructureSimulationPass.ManaGeneratorBasicId:
                    return ManaGeneratorPreviewKey;
                case StructureSimulationPass.HeatScrubberBasicId:
                    return HeatScrubberPreviewKey;
                case StructureSimulationPass.RiskLabBasicId:
                    return RiskLabPreviewKey;
                default:
                    return UnknownPreviewKey;
            }
        }

        public static string BuildPreviewText(string structureId, Func<string, string, string> localize)
        {
            return Localize(localize, ResolvePreviewKey(structureId));
        }

        public static string BuildRunPlanPreviewText(string structureId, string postureNameKey, Func<string, string, string> localize)
        {
            string structureName = MvpPlayerFacingLabelResolver.ResolveStructureDisplayName(structureId, localize);
            string postureName = Localize(localize, string.IsNullOrWhiteSpace(postureNameKey) ? MinimalMvpActionPanelPresenter.BalancedPostureKey : postureNameKey);
            string plan = string.Format(Localize(localize, RunPlanFormatKey), structureName, postureName);
            string tradeoff = string.Format(Localize(localize, RunTradeoffFormatKey), Localize(localize, ResolveRunTradeoffKey(postureNameKey)));
            return string.Format(Localize(localize, RunPlanCombinedFormatKey), plan, tradeoff);
        }

        private static string ResolveRunTradeoffKey(string postureNameKey)
        {
            switch (postureNameKey)
            {
                case MinimalMvpActionPanelPresenter.CautiousPostureKey:
                    return CautiousRunTradeoffKey;
                case MinimalMvpActionPanelPresenter.GreedyPostureKey:
                    return GreedyRunTradeoffKey;
                case MinimalMvpActionPanelPresenter.BalancedPostureKey:
                case null:
                    return BalancedRunTradeoffKey;
                default:
                    return string.IsNullOrWhiteSpace(postureNameKey) ? BalancedRunTradeoffKey : UnknownRunTradeoffKey;
            }
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
