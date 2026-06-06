using System;

namespace DungeonBuilder.M0
{
    public static class GuidedMvpActionPathPresenter
    {
        public const string StepPlaceOrModifyStructureId = "guided_mvp.step.place_or_modify_structure";
        public const string StepRunOrObserveId = "guided_mvp.step.run_or_observe";
        public const string StepReduceHeatPressureId = "guided_mvp.step.reduce_heat_pressure";
        public const string StepImproveSurvivabilityOrLayoutId = "guided_mvp.step.improve_survivability_or_layout";
        public const string StepVerifyResearchStatusId = "guided_mvp.step.verify_research_status";
        public const string StepRepeatOrImproveId = "guided_mvp.step.repeat_or_improve";

        public const string StatusMissingSaveKey = "guided_mvp.status.missing_save";
        public const string StatusPlaceOrModifyStructureKey = "guided_mvp.status.place_or_modify_structure";
        public const string StatusRunOrObserveKey = "guided_mvp.status.run_or_observe";
        public const string StatusHeatPressureKey = "guided_mvp.status.heat_pressure";
        public const string StatusPoorLootExtractionKey = "guided_mvp.status.poor_loot_extraction";
        public const string StatusResearchCompletionPendingKey = "guided_mvp.status.research_completion_pending";
        public const string StatusRepeatOrImproveKey = "guided_mvp.status.repeat_or_improve";

        public const string ActionPlaceStructureKey = "guided_mvp.action.place_structure";
        public const string ActionRunDungeonKey = "guided_mvp.action.run_dungeon";
        public const string ActionReduceHeatPressureKey = "guided_mvp.action.reduce_heat_pressure";
        public const string ActionImproveSurvivabilityOrLayoutKey = "guided_mvp.action.improve_survivability_or_layout";
        public const string ActionVerifyResearchStatusKey = "guided_mvp.action.verify_research_status";
        public const string ActionRepeatOrImproveKey = "guided_mvp.action.repeat_or_improve";

        public static GuidedMvpActionPathSummary Resolve(SaveData save, MvpPlayerLoopSummary loopSummary)
        {
            if (save == null)
            {
                return Create(
                    ruleResolved: false,
                    errorCode: GuidedMvpActionPathErrorCode.MissingSave,
                    stepId: StepPlaceOrModifyStructureId,
                    statusKey: StatusMissingSaveKey,
                    nextActionKey: ActionPlaceStructureKey,
                    isComplete: false);
            }

            MvpPlayerLoopSummary summary = loopSummary ?? MvpPlayerLoopSummaryPresenter.Resolve(save);
            if (summary == null || !summary.RuleResolved || !summary.HasPlacementContext)
            {
                return Create(
                    ruleResolved: true,
                    errorCode: GuidedMvpActionPathErrorCode.None,
                    stepId: StepPlaceOrModifyStructureId,
                    statusKey: StatusPlaceOrModifyStructureKey,
                    nextActionKey: ActionPlaceStructureKey,
                    isComplete: false);
            }

            if (!summary.HasRunOutcome)
            {
                return Create(
                    ruleResolved: true,
                    errorCode: GuidedMvpActionPathErrorCode.None,
                    stepId: StepRunOrObserveId,
                    statusKey: StatusRunOrObserveKey,
                    nextActionKey: ActionRunDungeonKey,
                    isComplete: false);
            }

            if (summary.HasResearchStatus && summary.VerificationRequired && summary.VerificationAvailable && !summary.CanClaimProduction)
            {
                return Create(
                    ruleResolved: true,
                    errorCode: GuidedMvpActionPathErrorCode.None,
                    stepId: StepVerifyResearchStatusId,
                    statusKey: StatusResearchCompletionPendingKey,
                    nextActionKey: ActionVerifyResearchStatusKey,
                    isComplete: false);
            }

            if (string.Equals(summary.HeatTierId, CurrentHeatTierResolver.ConcernTierId, StringComparison.Ordinal))
            {
                return Create(
                    ruleResolved: true,
                    errorCode: GuidedMvpActionPathErrorCode.None,
                    stepId: StepReduceHeatPressureId,
                    statusKey: StatusHeatPressureKey,
                    nextActionKey: ActionReduceHeatPressureKey,
                    isComplete: false);
            }

            if (summary.LootGeneratedWorldValue > 0 && summary.LootExtractedWorldValue <= 0)
            {
                return Create(
                    ruleResolved: true,
                    errorCode: GuidedMvpActionPathErrorCode.None,
                    stepId: StepImproveSurvivabilityOrLayoutId,
                    statusKey: StatusPoorLootExtractionKey,
                    nextActionKey: ActionImproveSurvivabilityOrLayoutKey,
                    isComplete: false);
            }

            return Create(
                ruleResolved: true,
                errorCode: GuidedMvpActionPathErrorCode.None,
                stepId: StepRepeatOrImproveId,
                statusKey: StatusRepeatOrImproveKey,
                nextActionKey: ActionRepeatOrImproveKey,
                isComplete: true);
        }

        private static GuidedMvpActionPathSummary Create(
            bool ruleResolved,
            GuidedMvpActionPathErrorCode errorCode,
            string stepId,
            string statusKey,
            string nextActionKey,
            bool isComplete)
        {
            return new GuidedMvpActionPathSummary
            {
                RuleResolved = ruleResolved,
                DeterministicErrorCode = (int)errorCode,
                CurrentStepId = stepId,
                CurrentStepStatusKey = statusKey,
                NextActionKey = nextActionKey,
                IsComplete = isComplete,
                WouldMutateState = false,
                WouldGrantRewards = false,
                WouldUnlockContent = false,
                WouldChargeCosts = false,
                WouldCallServer = false,
                WouldProcessOfflineResearch = false,
                WouldProcessOfflineHeat = false,
                WouldStartRaid = false
            };
        }
    }
}
