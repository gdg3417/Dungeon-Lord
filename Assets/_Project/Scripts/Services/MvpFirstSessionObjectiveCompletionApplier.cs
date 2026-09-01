using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0
{
    public static class MvpFirstSessionObjectiveCompletionApplier
    {
        public static bool ApplyIfComplete(SaveData save, RunSimulationConfig config)
            => ApplyIfComplete(save, config, null);

        public static bool ApplyIfComplete(SaveData save, RunSimulationConfig config,
            ProductionSpatialContentSnapshot production)
        {
            MvpFirstSessionObjectiveSummary summary = MvpFirstSessionObjectivePresenter.Resolve(
                save, config, production);
            if (summary == null || !summary.RuleResolved || !summary.IsComplete || string.IsNullOrWhiteSpace(summary.ObjectiveId))
            {
                return false;
            }

            return CompletedObjectiveStateResolver.MarkCompleted(
                save,
                summary.ObjectiveId,
                CompletedObjectiveStateResolver.FirstSessionObjectiveCompletionRuleSourceId);
        }
    }
}
