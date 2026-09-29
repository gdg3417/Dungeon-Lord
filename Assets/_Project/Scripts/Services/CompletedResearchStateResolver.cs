using System;
using System.Collections.Generic;

namespace DungeonBuilder.M0
{
    public static class CompletedResearchStateResolver
    {
        public static bool HasCompletedProject(CompletedResearchState completedState, string projectId)
        {
            string requiredProjectId = SafeId(projectId);
            if (string.IsNullOrEmpty(requiredProjectId) || completedState?.ProjectIds == null)
            {
                return false;
            }

            for (int i = 0; i < completedState.ProjectIds.Length; i++)
            {
                if (string.Equals(SafeId(completedState.ProjectIds[i]), requiredProjectId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static CompletedResearchStateSummary Resolve(
            CompletedResearchState completedState,
            ResearchPendingState pendingState = null,
            ResearchProgressState progressState = null)
        {
            var completedProjectIds = new HashSet<string>(StringComparer.Ordinal);
            if (completedState?.ProjectIds != null)
            {
                for (int i = 0; i < completedState.ProjectIds.Length; i++)
                {
                    string projectId = SafeId(completedState.ProjectIds[i]);
                    if (!string.IsNullOrEmpty(projectId))
                    {
                        completedProjectIds.Add(projectId);
                    }
                }
            }

            string lastCompletedProjectId = SafeId(completedState?.LastCompletedProjectId);
            string lastCompletionRuleSourceId = SafeId(completedState?.LastCompletionRuleSourceId);
            string currentPendingProjectId = SafeId(pendingState?.ProjectId);
            string currentProgressProjectId = SafeId(progressState?.ProjectId);
            bool currentProjectAlreadyCompleted =
                (!string.IsNullOrEmpty(currentPendingProjectId) && completedProjectIds.Contains(currentPendingProjectId)) ||
                (!string.IsNullOrEmpty(currentProgressProjectId) && completedProjectIds.Contains(currentProgressProjectId));

            return new CompletedResearchStateSummary
            {
                RuleResolved = true,
                HasCompletedState = completedProjectIds.Count > 0 ||
                                    !string.IsNullOrEmpty(lastCompletedProjectId) ||
                                    !string.IsNullOrEmpty(lastCompletionRuleSourceId),
                CompletedCount = completedProjectIds.Count,
                LastCompletedProjectId = lastCompletedProjectId,
                LastCompletionRuleSourceId = lastCompletionRuleSourceId,
                CurrentPendingProjectId = currentPendingProjectId,
                CurrentProgressProjectId = currentProgressProjectId,
                CurrentProjectAlreadyCompleted = currentProjectAlreadyCompleted,
                RuleSourceIdUsed = lastCompletionRuleSourceId
            };
        }

        private static string SafeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }
    }
}
