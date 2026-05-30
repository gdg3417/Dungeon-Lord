namespace DungeonBuilder.M0
{
    public static class ResearchPendingResolver
    {
        public static ResearchPendingValidationResult Resolve(
            ResearchPendingState state,
            ResearchPendingScaffoldConfig config)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.ProjectId))
            {
                return Resolved(pending: false, slotId: string.Empty, projectId: string.Empty, config);
            }

            ResearchPendingValidationResult configError = ValidateConfig(config);
            if (configError != null)
            {
                return configError;
            }

            return Resolved(pending: true, config.slotId, state.ProjectId, config);
        }

        public static ResearchPendingValidationResult ResolveScaffold(ResearchPendingScaffoldConfig config)
        {
            ResearchPendingValidationResult configError = ValidateConfig(config);
            if (configError != null)
            {
                return configError;
            }

            return Resolved(pending: true, config.slotId, config.projectId, config);
        }

        private static ResearchPendingValidationResult ValidateConfig(ResearchPendingScaffoldConfig config)
        {
            if (config == null || !config.enabled)
            {
                return Error(ResearchPendingValidationErrorCode.ScaffoldConfigMissingOrDisabled);
            }

            if (string.IsNullOrWhiteSpace(config.slotId))
            {
                return Error(ResearchPendingValidationErrorCode.ScaffoldSlotIdMissing);
            }

            if (string.IsNullOrWhiteSpace(config.projectId))
            {
                return Error(ResearchPendingValidationErrorCode.ScaffoldProjectIdMissing);
            }

            return null;
        }

        private static ResearchPendingValidationResult Resolved(
            bool pending,
            string slotId,
            string projectId,
            ResearchPendingScaffoldConfig config)
        {
            return new ResearchPendingValidationResult
            {
                RuleResolved = true,
                DeterministicErrorCode = (int)ResearchPendingValidationErrorCode.None,
                Pending = pending,
                SlotId = slotId,
                ProjectId = projectId,
                RuleSourceIdUsed = config != null ? config.ruleSourceId : string.Empty
            };
        }

        private static ResearchPendingValidationResult Error(ResearchPendingValidationErrorCode errorCode)
        {
            return new ResearchPendingValidationResult
            {
                DeterministicErrorCode = (int)errorCode,
                SlotId = string.Empty,
                ProjectId = string.Empty,
                RuleSourceIdUsed = string.Empty
            };
        }
    }
}
