namespace DungeonBuilder.M0
{
    public static class MvpFirstSessionObjectiveCompletionApplier
    {
        public static bool ApplyIfComplete(SaveData save, RunSimulationConfig config)
        {
            MvpFirstSessionObjectiveSummary summary = MvpFirstSessionObjectivePresenter.Resolve(save, config);
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
