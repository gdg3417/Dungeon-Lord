using System;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0
{
    public static class MvpPostContractGreedTrialPresenter
    {
        public const string TitleKey = "ui.mvp_greed_trial.title";
        public const string GreedSetupFormatKey = "ui.mvp_greed_trial.greed_setup_format";
        public const string HeatStabilizedFormatKey = "ui.mvp_greed_trial.heat_stabilized_format";
        public const string RiskResponseFormatKey = "ui.mvp_greed_trial.risk_response_format";
        public const string StatusFormatKey = "ui.mvp_greed_trial.status_format";
        public const string StatusInProgressKey = "ui.mvp_greed_trial.status.in_progress";
        public const string StatusCompleteKey = "ui.mvp_greed_trial.status.complete";
        public const string ValueCompleteKey = "ui.mvp_greed_trial.value.complete";
        public const string ValueIncompleteKey = "ui.mvp_greed_trial.value.incomplete";
        public const string NextActionFormatKey = "ui.mvp_greed_trial.next_action_format";
        public const string NextActionTestGreedierSetupKey = "ui.mvp_greed_trial.next_action.test_greedier_setup";
        public const string NextActionStabilizeHeatKey = "ui.mvp_greed_trial.next_action.stabilize_heat";
        public const string NextActionAddCounterplayKey = "ui.mvp_greed_trial.next_action.add_counterplay";
        public const string NextActionCompleteKey = "ui.mvp_greed_trial.next_action.complete";

        public static MvpPostContractGreedTrialSummary Resolve(SaveData save, RunSimulationConfig config)
        {
            return Resolve(save, config, MvpFirstSessionObjectivePresenter.Resolve(save, config));
        }

        public static MvpPostContractGreedTrialSummary Resolve(SaveData save, RunSimulationConfig config, MvpFirstSessionObjectiveSummary firstContract)
        {
            var summary = new MvpPostContractGreedTrialSummary
            {
                WouldMutateState = false,
                WouldGrantRewards = false,
                WouldUnlockContent = false,
                WouldCallServer = false
            };

            if (save == null || config == null || firstContract == null || !firstContract.RuleResolved || !firstContract.IsComplete)
            {
                return summary;
            }

            summary.RuleResolved = true;
            summary.IsActive = true;
            summary.PlacementEffects = MvpPlacementEffectsResolver.ResolveForSave(save, config);
            CurrentHeatTierSummary heat = CurrentHeatTierResolver.Resolve(config, save.structureRuntime?.Heat ?? 0d);
            summary.CurrentHeatTierId = heat.RuleResolved ? heat.TierId : string.Empty;
            summary.GreedSetupTestedComplete = HasGreedierLootSetup(summary.PlacementEffects, config);
            summary.HeatStabilizedComplete = heat.RuleResolved && string.Equals(summary.CurrentHeatTierId, CurrentHeatTierResolver.PeaceTierId, StringComparison.Ordinal);
            summary.RiskResponseComplete = summary.GreedSetupTestedComplete && summary.PlacementEffects != null && summary.PlacementEffects.RuleResolved && summary.PlacementEffects.HeatPressure <= 0;
            summary.IsComplete = summary.GreedSetupTestedComplete && summary.HeatStabilizedComplete && summary.RiskResponseComplete;
            summary.NextActionKey = ResolveNextActionKey(summary);
            return summary;
        }

        public static string BuildPanelText(MvpPostContractGreedTrialSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.IsActive)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            AppendLine(builder, Localize(localize, TitleKey));
            AppendLine(builder, string.Format(Localize(localize, GreedSetupFormatKey), Localize(localize, summary.GreedSetupTestedComplete ? ValueCompleteKey : ValueIncompleteKey)));
            AppendLine(builder, string.Format(Localize(localize, HeatStabilizedFormatKey), Localize(localize, summary.HeatStabilizedComplete ? ValueCompleteKey : ValueIncompleteKey)));
            AppendLine(builder, string.Format(Localize(localize, RiskResponseFormatKey), Localize(localize, summary.RiskResponseComplete ? ValueCompleteKey : ValueIncompleteKey)));
            AppendLine(builder, string.Format(Localize(localize, StatusFormatKey), Localize(localize, summary.IsComplete ? StatusCompleteKey : StatusInProgressKey)));
            AppendLine(builder, string.Format(Localize(localize, NextActionFormatKey), Localize(localize, string.IsNullOrWhiteSpace(summary.NextActionKey) ? ResolveNextActionKey(summary) : summary.NextActionKey)));
            return builder.ToString();
        }

        public static string BuildStatusPanelText(MvpPostContractGreedTrialSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.IsActive)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            AppendLine(builder, Localize(localize, TitleKey));
            AppendLine(builder, string.Format(Localize(localize, GreedSetupFormatKey), Localize(localize, summary.GreedSetupTestedComplete ? ValueCompleteKey : ValueIncompleteKey)));
            AppendLine(builder, string.Format(Localize(localize, HeatStabilizedFormatKey), Localize(localize, summary.HeatStabilizedComplete ? ValueCompleteKey : ValueIncompleteKey)));
            AppendLine(builder, string.Format(Localize(localize, RiskResponseFormatKey), Localize(localize, summary.RiskResponseComplete ? ValueCompleteKey : ValueIncompleteKey)));
            AppendLine(builder, string.Format(Localize(localize, StatusFormatKey), Localize(localize, summary.IsComplete ? StatusCompleteKey : StatusInProgressKey)));
            return builder.ToString();
        }

        private static bool HasGreedierLootSetup(MvpPlacementEffectsSummary effects, RunSimulationConfig config)
        {
            if (effects == null || !effects.RuleResolved)
            {
                return false;
            }

            if (effects.ContributingOptionIds != null && effects.ContributingOptionIds.Any(optionId => string.Equals(optionId, MvpDungeonPlacementIds.GlitteringHoardOptionId, StringComparison.Ordinal)))
            {
                return true;
            }

            MvpPlacementEffectConfig baseline = ResolveBasicLootBaseline(config);
            return baseline != null && (effects.LootBonus > baseline.LootBonus || effects.Attraction > baseline.Attraction);
        }

        private static MvpPlacementEffectConfig ResolveBasicLootBaseline(RunSimulationConfig config)
        {
            if (config?.MvpPlacementEffects == null) return null;
            return config.MvpPlacementEffects.FirstOrDefault(effect => effect != null && string.Equals(effect.OptionId, MvpDungeonPlacementIds.BasicLootNodeOptionId, StringComparison.Ordinal));
        }

        private static string ResolveNextActionKey(MvpPostContractGreedTrialSummary summary)
        {
            if (summary != null && summary.IsComplete) return NextActionCompleteKey;
            if (summary == null || !summary.GreedSetupTestedComplete) return NextActionTestGreedierSetupKey;
            if (!summary.HeatStabilizedComplete) return NextActionStabilizeHeatKey;
            return NextActionAddCounterplayKey;
        }

        private static string Localize(Func<string, string, string> localize, string key) => localize == null ? key : localize(key, key);
        private static void AppendLine(StringBuilder builder, string line) { if (builder.Length > 0) builder.Append('\n'); builder.Append(line ?? string.Empty); }
    }

    [Serializable]
    public sealed class MvpPostContractGreedTrialSummary
    {
        public bool RuleResolved;
        public bool IsActive;
        public bool GreedSetupTestedComplete;
        public bool HeatStabilizedComplete;
        public bool RiskResponseComplete;
        public bool IsComplete;
        public string CurrentHeatTierId;
        public string NextActionKey;
        public MvpPlacementEffectsSummary PlacementEffects = new MvpPlacementEffectsSummary();
        public bool WouldMutateState;
        public bool WouldGrantRewards;
        public bool WouldUnlockContent;
        public bool WouldCallServer;
    }
}
