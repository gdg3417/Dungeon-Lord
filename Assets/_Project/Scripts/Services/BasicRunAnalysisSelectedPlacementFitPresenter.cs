using System;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0
{
    public static class BasicRunAnalysisSelectedPlacementFitPresenter
    {
        public const string SelectedFitFormatKey = "ui.mvp_loop.analysis.selected_fit_format";
        public const string MatchKey = "mvp_loop.analysis.fit.match";
        public const string MismatchKey = "mvp_loop.analysis.fit.mismatch";
        public const string BroadKey = "mvp_loop.analysis.fit.broad";
        public const string NotReadyKey = "mvp_loop.analysis.fit.not_ready";
        public const string RoomCategoryKey = "mvp_loop.analysis.category.room";
        public const string MonsterCategoryKey = "mvp_loop.analysis.category.monster";
        public const string TrapCategoryKey = "mvp_loop.analysis.category.trap";
        public const string LootNodeCategoryKey = "mvp_loop.analysis.category.loot_node";
        public const string MonsterOrTrapTargetLabelKey = "mvp_loop.analysis.target_label.monster_or_trap";
        public const string TrapOrLootTargetLabelKey = "mvp_loop.analysis.target_label.trap_or_loot";
        public const string RoomOrMonsterTargetLabelKey = "mvp_loop.analysis.target_label.room_or_monster";
        public const string LootNodeTargetLabelKey = "mvp_loop.analysis.target_label.loot_node";
        public const string AnyOneTargetLabelKey = "mvp_loop.analysis.target_label.any_one";

        public static string ResolveFitKey(MvpPlayerLoopSummary summary, string targetKey, string selectedCategoryId)
        {
            if (summary == null || !summary.RuleResolved || !summary.AnalysisUnlocked)
            {
                return string.Empty;
            }

            if (!summary.HasRunOutcome || string.IsNullOrWhiteSpace(targetKey))
            {
                return NotReadyKey;
            }

            if (string.Equals(targetKey, BasicRunAnalysisPlacementTargetPresenter.FallbackTargetKey, StringComparison.Ordinal))
            {
                return BroadKey;
            }

            if (!MvpDungeonPlacementIds.IsAllowedCategory(selectedCategoryId))
            {
                return string.Empty;
            }

            return TargetContainsCategory(targetKey, selectedCategoryId) ? MatchKey : MismatchKey;
        }

        public static string BuildFitLine(MvpPlayerLoopSummary summary, string targetKey, string selectedCategoryId, Func<string, string, string> localize)
        {
            string fitKey = ResolveFitKey(summary, targetKey, selectedCategoryId);
            if (string.IsNullOrEmpty(fitKey))
            {
                return string.Empty;
            }

            string fitText = string.Equals(fitKey, MismatchKey, StringComparison.Ordinal)
                ? string.Format(Localize(localize, fitKey), Localize(localize, ResolveCategoryLabelKey(selectedCategoryId)), Localize(localize, ResolveTargetLabelKey(targetKey)))
                : Localize(localize, fitKey);
            return string.Format(Localize(localize, SelectedFitFormatKey), fitText);
        }

        public static string BuildFitLine(MvpPlayerLoopSummary summary, string selectedCategoryId, Func<string, string, string> localize)
        {
            return BuildFitLine(summary, BasicRunAnalysisPlacementTargetPresenter.ResolveTargetKey(summary), selectedCategoryId, localize);
        }

        private static bool TargetContainsCategory(string targetKey, string selectedCategoryId)
        {
            switch (targetKey)
            {
                case BasicRunAnalysisPlacementTargetPresenter.ReduceDangerTargetKey:
                    return string.Equals(selectedCategoryId, MvpDungeonPlacementIds.MonsterCategoryId, StringComparison.Ordinal) ||
                           string.Equals(selectedCategoryId, MvpDungeonPlacementIds.TrapCategoryId, StringComparison.Ordinal);
                case BasicRunAnalysisPlacementTargetPresenter.ReduceHeatTargetKey:
                    return string.Equals(selectedCategoryId, MvpDungeonPlacementIds.TrapCategoryId, StringComparison.Ordinal) ||
                           string.Equals(selectedCategoryId, MvpDungeonPlacementIds.LootNodeCategoryId, StringComparison.Ordinal);
                case BasicRunAnalysisPlacementTargetPresenter.ImproveExtractionTargetKey:
                    return string.Equals(selectedCategoryId, MvpDungeonPlacementIds.RoomCategoryId, StringComparison.Ordinal) ||
                           string.Equals(selectedCategoryId, MvpDungeonPlacementIds.MonsterCategoryId, StringComparison.Ordinal);
                case BasicRunAnalysisPlacementTargetPresenter.TestGreedierTargetKey:
                    return string.Equals(selectedCategoryId, MvpDungeonPlacementIds.LootNodeCategoryId, StringComparison.Ordinal);
                default:
                    return false;
            }
        }

        private static string ResolveCategoryLabelKey(string selectedCategoryId)
        {
            switch (selectedCategoryId)
            {
                case MvpDungeonPlacementIds.RoomCategoryId: return RoomCategoryKey;
                case MvpDungeonPlacementIds.MonsterCategoryId: return MonsterCategoryKey;
                case MvpDungeonPlacementIds.TrapCategoryId: return TrapCategoryKey;
                case MvpDungeonPlacementIds.LootNodeCategoryId: return LootNodeCategoryKey;
                default: return RoomCategoryKey;
            }
        }

        private static string ResolveTargetLabelKey(string targetKey)
        {
            switch (targetKey)
            {
                case BasicRunAnalysisPlacementTargetPresenter.ReduceDangerTargetKey: return MonsterOrTrapTargetLabelKey;
                case BasicRunAnalysisPlacementTargetPresenter.ReduceHeatTargetKey: return TrapOrLootTargetLabelKey;
                case BasicRunAnalysisPlacementTargetPresenter.ImproveExtractionTargetKey: return RoomOrMonsterTargetLabelKey;
                case BasicRunAnalysisPlacementTargetPresenter.TestGreedierTargetKey: return LootNodeTargetLabelKey;
                default: return AnyOneTargetLabelKey;
            }
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            return localize != null ? localize(key, key) : key;
        }
    }
}
