using System;

namespace DungeonBuilder.M0
{
    public static class ResearchUnlockSummaryPresenter
    {
        public const string UnavailableKey = "ui.research_unlock.unavailable";
        public const string NoneKey = "ui.research_unlock.none";

        public static ResearchUnlockSummary Resolve(CompletedResearchState completedState, ResearchUnlockBridgeConfig config)
        {
            bool hasCompleted = HasCompletedResearch(completedState);
            if (!hasCompleted)
            {
                return Error(ResearchUnlockSummaryErrorCode.NoCompletedResearch, hasCompleted, config?.ruleSourceId, NoneKey);
            }

            if (!IsValidConfig(config))
            {
                return Error(ResearchUnlockSummaryErrorCode.MissingOrInvalidConfig, hasCompleted, config?.ruleSourceId, UnavailableKey);
            }

            string[] projectIds = completedState.ProjectIds ?? Array.Empty<string>();
            for (int i = projectIds.Length - 1; i >= 0; i--)
            {
                string projectId = projectIds[i];
                if (string.IsNullOrWhiteSpace(projectId))
                {
                    continue;
                }

                ResearchUnlockDefinitionConfig definition = FindDefinition(config.unlocks, projectId);
                if (definition == null)
                {
                    continue;
                }

                return new ResearchUnlockSummary
                {
                    RuleResolved = true,
                    DeterministicErrorCode = (int)ResearchUnlockSummaryErrorCode.None,
                    HasCompletedResearch = true,
                    MatchedProjectId = projectId,
                    UnlockId = definition.unlockId ?? string.Empty,
                    SummaryLocalizationKey = definition.summaryKey ?? string.Empty,
                    RuleSourceIdUsed = config.ruleSourceId ?? string.Empty,
                    WouldMutateState = false,
                    WouldGrantRewards = false,
                    WouldUnlockContent = false
                };
            }

            return Error(ResearchUnlockSummaryErrorCode.NoMatchingUnlock, hasCompleted, config.ruleSourceId, UnavailableKey);
        }

        private static ResearchUnlockDefinitionConfig FindDefinition(ResearchUnlockDefinitionConfig[] definitions, string projectId)
        {
            for (int i = 0; i < definitions.Length; i++)
            {
                ResearchUnlockDefinitionConfig definition = definitions[i];
                if (definition == null ||
                    string.IsNullOrWhiteSpace(definition.researchProjectId) ||
                    string.IsNullOrWhiteSpace(definition.unlockId) ||
                    string.IsNullOrWhiteSpace(definition.summaryKey))
                {
                    continue;
                }

                if (string.Equals(definition.researchProjectId, projectId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        private static bool HasCompletedResearch(CompletedResearchState completedState)
        {
            if (completedState?.ProjectIds == null)
            {
                return false;
            }

            for (int i = 0; i < completedState.ProjectIds.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(completedState.ProjectIds[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidConfig(ResearchUnlockBridgeConfig config)
        {
            return config != null &&
                   config.enabled &&
                   !string.IsNullOrWhiteSpace(config.ruleSourceId) &&
                   config.unlocks != null &&
                   config.unlocks.Length > 0;
        }

        private static ResearchUnlockSummary Error(
            ResearchUnlockSummaryErrorCode code,
            bool hasCompletedResearch,
            string ruleSourceId,
            string safeKey)
        {
            return new ResearchUnlockSummary
            {
                RuleResolved = false,
                DeterministicErrorCode = (int)code,
                HasCompletedResearch = hasCompletedResearch,
                SummaryLocalizationKey = safeKey ?? UnavailableKey,
                RuleSourceIdUsed = ruleSourceId ?? string.Empty,
                WouldMutateState = false,
                WouldGrantRewards = false,
                WouldUnlockContent = false
            };
        }
    }
}
