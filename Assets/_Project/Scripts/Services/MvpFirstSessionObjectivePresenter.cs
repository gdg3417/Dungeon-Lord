using System;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0
{
    public static class MvpFirstSessionObjectivePresenter
    {
        public const string TitleKey = "ui.mvp_first_contract.title";
        public const string PathBuiltFormatKey = "ui.mvp_first_contract.path_built_format";
        public const string RunObservedFormatKey = "ui.mvp_first_contract.run_observed_format";
        public const string LootRecoveredFormatKey = "ui.mvp_first_contract.loot_recovered_format";
        public const string HeatTargetFormatKey = "ui.mvp_first_contract.heat_target_format";
        public const string AnalysisFormatKey = "ui.mvp_first_contract.analysis_format";
        public const string StatusFormatKey = "ui.mvp_first_contract.status_format";
        public const string CompleteKey = "ui.mvp_first_contract.value.complete";
        public const string IncompleteKey = "ui.mvp_first_contract.value.incomplete";
        public const string AnalysisUnlockedKey = "ui.mvp_first_contract.value.analysis_unlocked";
        public const string AnalysisLockedKey = "ui.mvp_first_contract.value.analysis_locked";
        public const string StatusInProgressKey = "ui.mvp_first_contract.status.in_progress";
        public const string StatusCompleteKey = "ui.mvp_first_contract.status.complete";
        public const string CompactInProgressFormatKey = "ui.mvp_first_contract.compact.in_progress_format";
        public const string CompactCompleteFormatKey = "ui.mvp_first_contract.compact.complete_format";
        public const string CompactPathCompleteKey = "ui.mvp_first_contract.compact.path_complete";
        public const string CompactPathIncompleteKey = "ui.mvp_first_contract.compact.path_incomplete";

        public static MvpFirstSessionObjectiveSummary Resolve(SaveData save, RunSimulationConfig config)
        {
            MvpFirstSessionObjectiveConfig objective = config?.MvpFirstSessionObjective;
            var summary = new MvpFirstSessionObjectiveSummary
            {
                ObjectiveId = objective?.ObjectiveId ?? string.Empty,
                RequiredRunCount = objective != null ? Math.Max(0, objective.RequiredRunCount) : 0,
                RequiredRecoveredLootValue = objective != null ? Math.Max(0, objective.RequiredRecoveredLootValue) : 0,
                RequiredCompletePath = objective != null && objective.RequiredCompletePath,
                AllowedMaxHeatTierId = objective?.AllowedMaxHeatTierId ?? string.Empty,
                RequireResearchAnalysisUnlocked = objective != null && objective.RequireResearchAnalysisUnlocked,
                WouldMutateState = false,
                WouldGrantRewards = false,
                WouldUnlockContent = false,
                WouldCallServer = false
            };

            if (save == null || objective == null || string.IsNullOrWhiteSpace(objective.ObjectiveId))
            {
                return summary;
            }

            summary.RuleResolved = true;
            MvpDungeonPlacementEntry[] placements = MvpDungeonLayoutResolver.ResolveOrderedPlacements(save.mvpDungeonFloorLayout, save.mvpDungeonPlacements);
            summary.CurrentPathPlacementCount = placements.Length;
            summary.RequiredPathPlacementCount = MvpDungeonPlacementIds.OrderedCategoryIds.Length;
            summary.PathComplete = !summary.RequiredCompletePath || summary.CurrentPathPlacementCount >= summary.RequiredPathPlacementCount;

            RunOutcomeRecord[] outcomes = ResolveOutcomes(save.runHistory);
            summary.RunCount = outcomes.Length;
            summary.RunObservedComplete = summary.RunCount >= summary.RequiredRunCount;
            summary.RecoveredLootValue = outcomes.Sum(outcome => outcome?.LootExtractionSummary?.TotalExtractedWorldValue ?? 0);
            summary.LootRecoveredComplete = summary.RecoveredLootValue >= summary.RequiredRecoveredLootValue;

            CurrentHeatTierSummary heat = CurrentHeatTierResolver.Resolve(config, save.structureRuntime?.Heat ?? 0d);
            summary.CurrentHeatTierId = heat.RuleResolved ? heat.TierId : string.Empty;
            summary.HeatTargetComplete = IsHeatAllowed(summary.CurrentHeatTierId, summary.AllowedMaxHeatTierId);
            summary.AnalysisUnlocked = HasAnalysisUnlock(save.completedResearch, config);
            summary.AnalysisComplete = !summary.RequireResearchAnalysisUnlocked || summary.AnalysisUnlocked;
            summary.IsComplete = summary.PathComplete && summary.RunObservedComplete && summary.LootRecoveredComplete && summary.HeatTargetComplete && summary.AnalysisComplete;
            return summary;
        }

        public static string BuildPanelText(MvpFirstSessionObjectiveSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved)
            {
                summary = new MvpFirstSessionObjectiveSummary();
            }

            var builder = new StringBuilder();
            AppendLine(builder, Localize(localize, TitleKey));
            AppendLine(builder, string.Format(Localize(localize, PathBuiltFormatKey), Localize(localize, summary.PathComplete ? CompleteKey : IncompleteKey)));
            AppendLine(builder, string.Format(Localize(localize, RunObservedFormatKey), Localize(localize, summary.RunObservedComplete ? CompleteKey : IncompleteKey)));
            AppendLine(builder, string.Format(Localize(localize, LootRecoveredFormatKey), summary.RecoveredLootValue, summary.RequiredRecoveredLootValue));
            AppendLine(builder, string.Format(Localize(localize, HeatTargetFormatKey), ResolveHeatLabel(summary.AllowedMaxHeatTierId, localize), ResolveHeatLabel(summary.CurrentHeatTierId, localize)));
            AppendLine(builder, string.Format(Localize(localize, AnalysisFormatKey), Localize(localize, summary.AnalysisComplete ? AnalysisUnlockedKey : AnalysisLockedKey)));
            AppendLine(builder, string.Format(Localize(localize, StatusFormatKey), Localize(localize, summary.IsComplete ? StatusCompleteKey : StatusInProgressKey)));
            return builder.ToString();
        }

        public static string BuildCompactStatusLine(MvpFirstSessionObjectiveSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved)
            {
                summary = new MvpFirstSessionObjectiveSummary();
            }

            string title = Localize(localize, TitleKey);
            if (summary.IsComplete)
            {
                return string.Format(
                    Localize(localize, CompactCompleteFormatKey),
                    title,
                    Localize(localize, StatusCompleteKey));
            }

            string pathStatus = Localize(localize, summary.PathComplete ? CompactPathCompleteKey : CompactPathIncompleteKey);
            return string.Format(
                Localize(localize, CompactInProgressFormatKey),
                title,
                Localize(localize, StatusInProgressKey),
                summary.RecoveredLootValue,
                summary.RequiredRecoveredLootValue,
                pathStatus);
        }

        private static RunOutcomeRecord[] ResolveOutcomes(RunHistoryState history)
        {
            if (history == null) return Array.Empty<RunOutcomeRecord>();
            if (history.RecentOutcomes != null && history.RecentOutcomes.Length > 0) return history.RecentOutcomes.Where(outcome => outcome != null).ToArray();
            return history.LatestOutcome != null ? new[] { history.LatestOutcome } : Array.Empty<RunOutcomeRecord>();
        }

        private static bool HasAnalysisUnlock(CompletedResearchState completed, RunSimulationConfig config)
        {
            string requiredProject = config?.MvpFirstSessionObjective?.AnalysisResearchProjectId;
            if (completed?.ProjectIds == null) return false;
            return completed.ProjectIds.Any(projectId => !string.IsNullOrWhiteSpace(projectId) && string.Equals(projectId, requiredProject, StringComparison.Ordinal));
        }

        private static bool IsHeatAllowed(string currentTierId, string maxTierId)
        {
            if (string.IsNullOrWhiteSpace(maxTierId)) return true;
            return HeatRank(currentTierId) >= 0 && HeatRank(currentTierId) <= HeatRank(maxTierId);
        }

        private static int HeatRank(string tierId)
        {
            if (string.Equals(tierId, CurrentHeatTierResolver.PeaceTierId, StringComparison.Ordinal)) return 0;
            if (string.Equals(tierId, CurrentHeatTierResolver.NoticeTierId, StringComparison.Ordinal)) return 1;
            if (string.Equals(tierId, CurrentHeatTierResolver.ConcernTierId, StringComparison.Ordinal)) return 2;
            return -1;
        }

        private static string ResolveHeatLabel(string tierId, Func<string, string, string> localize)
        {
            return string.IsNullOrWhiteSpace(tierId) ? Localize(localize, MvpLoopSummaryPanelPresenter.ValueUnknownKey) : Localize(localize, tierId);
        }

        private static string Localize(Func<string, string, string> localize, string key) => localize == null ? key : localize(key, key);
        private static void AppendLine(StringBuilder builder, string line) { if (builder.Length > 0) builder.Append('\n'); builder.Append(line ?? string.Empty); }
    }

    [Serializable]
    public sealed class MvpFirstSessionObjectiveConfig
    {
        public string ObjectiveId;
        public bool RequiredCompletePath;
        public int RequiredRunCount;
        public int RequiredRecoveredLootValue;
        public string AllowedMaxHeatTierId;
        public bool RequireResearchAnalysisUnlocked;
        public string AnalysisUnlockId;
        public string AnalysisResearchProjectId;
    }

    [Serializable]
    public sealed class MvpFirstSessionObjectiveSummary
    {
        public bool RuleResolved;
        public string ObjectiveId;
        public bool RequiredCompletePath;
        public int RequiredPathPlacementCount;
        public int CurrentPathPlacementCount;
        public bool PathComplete;
        public int RequiredRunCount;
        public int RunCount;
        public bool RunObservedComplete;
        public int RequiredRecoveredLootValue;
        public int RecoveredLootValue;
        public bool LootRecoveredComplete;
        public string AllowedMaxHeatTierId;
        public string CurrentHeatTierId;
        public bool HeatTargetComplete;
        public bool RequireResearchAnalysisUnlocked;
        public bool AnalysisUnlocked;
        public bool AnalysisComplete;
        public bool IsComplete;
        public bool WouldMutateState;
        public bool WouldGrantRewards;
        public bool WouldUnlockContent;
        public bool WouldCallServer;
    }
}
