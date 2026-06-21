using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0
{
    public static class CompletedObjectiveStateResolver
    {
        public const string FirstSessionObjectiveCompletionRuleSourceId = "mvp_first_session_objective.completed";

        public static bool HasCompletedObjective(CompletedObjectiveState completedState, string objectiveId)
        {
            if (string.IsNullOrWhiteSpace(objectiveId) || completedState == null)
            {
                return false;
            }

            if (string.Equals(completedState.LastCompletedObjectiveId, objectiveId, StringComparison.Ordinal))
            {
                return true;
            }

            if (completedState.ObjectiveIds == null)
            {
                return false;
            }

            for (int i = 0; i < completedState.ObjectiveIds.Length; i++)
            {
                if (string.Equals(completedState.ObjectiveIds[i], objectiveId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool MarkCompleted(SaveData save, string objectiveId, string ruleSourceId)
        {
            if (save == null || string.IsNullOrWhiteSpace(objectiveId))
            {
                return false;
            }

            if (save.completedObjectives == null)
            {
                save.completedObjectives = new CompletedObjectiveState();
            }

            if (HasCompletedObjective(save.completedObjectives, objectiveId))
            {
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (save.completedObjectives.ObjectiveIds != null)
            {
                for (int i = 0; i < save.completedObjectives.ObjectiveIds.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(save.completedObjectives.ObjectiveIds[i]))
                    {
                        ids.Add(save.completedObjectives.ObjectiveIds[i]);
                    }
                }
            }

            ids.Add(objectiveId);
            save.completedObjectives.ObjectiveIds = ids.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            save.completedObjectives.LastCompletedObjectiveId = objectiveId;
            save.completedObjectives.LastCompletionRuleSourceId = string.IsNullOrWhiteSpace(ruleSourceId) ? FirstSessionObjectiveCompletionRuleSourceId : ruleSourceId;
            return true;
        }
    }
}
