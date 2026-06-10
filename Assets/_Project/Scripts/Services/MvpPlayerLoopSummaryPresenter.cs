using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.M0
{
    public static class MvpPlayerLoopSummaryPresenter
    {
        public const string SuggestRunDungeonKey = "mvp_loop.suggestion.run_dungeon";
        public const string SuggestReduceHeatPressureKey = "mvp_loop.suggestion.reduce_heat_pressure";
        public const string SuggestImproveSurvivabilityOrLayoutKey = "mvp_loop.suggestion.improve_survivability_or_layout";
        public const string SuggestVerifyResearchStatusKey = "mvp_loop.suggestion.verify_research_status";
        public const string SuggestRepeatOrImprovePlacementKey = "mvp_loop.suggestion.repeat_or_improve_placement";
        public const string ResearchVerificationRequiredKey = "ui.research.status.verification_required";
        public const string ResearchUnavailableKey = MvpPlayerFacingLabelResolver.ResearchUnavailableKey;

        public static MvpPlayerLoopSummary Resolve(
            SaveData save,
            RunSimulationConfig runConfig = null,
            ResearchCompletionEligibilityScaffoldConfig researchEligibilityConfig = null,
            ResearchVerificationScaffoldConfig researchVerificationConfig = null)
        {
            if (save == null)
            {
                return CreateError(MvpPlayerLoopSummaryErrorCode.MissingSave);
            }

            StructureRuntimeState runtime = save.structureRuntime;
            RunOutcomeRecord latestRun = GetLatestRun(save.runHistory);
            DungeonSlot? selectedSlot = GetSelectedSlot(save.dungeonLayout);
            MvpDungeonPlacementEntry[] dungeonPlacements = GetOrderedMvpPlacements(save.mvpDungeonPlacements);
            double currentMana = runtime?.ManaReserve ?? 0d;
            double currentHeat = runtime?.Heat ?? 0d;

            ResearchStatusPresentation researchStatus = ResearchStatusPresenter.Present(
                save.researchPending,
                save.researchProgress,
                save.completedResearch,
                researchEligibilityConfig);
            ResearchVerificationBoundarySummary verificationBoundary = ResearchVerificationBoundaryResolver.Resolve(
                save.researchPending,
                save.researchProgress,
                save.completedResearch,
                researchEligibilityConfig,
                researchVerificationConfig);

            CurrentHeatTierSummary heatTier = CurrentHeatTierResolver.Resolve(runConfig, currentHeat);
            RunHeatApplicationSummary heatApplication = latestRun?.RunHeatApplicationSummary;
            RunLootSummary lootSummary = latestRun?.LootSummary;
            RunLootExtractionSummary extractionSummary = latestRun?.LootExtractionSummary;

            bool hasRunOutcome = latestRun != null;
            bool hasResolvedHeatApplication = heatApplication != null && heatApplication.RuleResolved;
            bool hasResearchStatus = HasResearchSignal(researchStatus, verificationBoundary, save.researchPending, save.researchProgress, save.completedResearch);
            bool verificationActionable = IsVerificationActionable(researchStatus, verificationBoundary);
            string researchStatusKey = ResolveResearchStatusKey(researchStatus, verificationActionable);
            string researchProjectId = FirstNonEmpty(verificationBoundary?.ProjectId, researchStatus?.ProjectId, save.researchProgress?.ProjectId, save.researchPending?.ProjectId);
            string heatTierId = FirstNonEmpty(heatTier.RuleResolved ? heatTier.TierId : null, heatApplication?.TierAfter);
            int generatedWorldValue = lootSummary?.TotalGeneratedWorldValue ?? 0;
            int extractedWorldValue = extractionSummary?.TotalExtractedWorldValue ?? 0;
            int extractedTradeableWorldValue = extractionSummary?.TotalExtractedTradeableWorldValue ?? 0;
            AdventurerPartyCompositionSummary partyPreview = hasRunOutcome
                ? AdventurerPartyCompositionResolver.Resolve(runConfig, latestRun.RunId, latestRun.TickStarted, selectedSlot.HasValue ? selectedSlot.Value.StructureId : string.Empty)
                : null;

            var summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                DeterministicErrorCode = (int)MvpPlayerLoopSummaryErrorCode.None,
                HasPlacementContext = dungeonPlacements.Length > 0 || selectedSlot.HasValue,
                SelectedStructureId = selectedSlot.HasValue ? selectedSlot.Value.StructureId ?? string.Empty : string.Empty,
                DungeonPlacements = dungeonPlacements,
                HasRunOutcome = hasRunOutcome,
                LatestRunId = latestRun?.RunId ?? string.Empty,
                RunSucceeded = latestRun != null && latestRun.Success,
                ManaReserve = currentMana,
                LootGeneratedWorldValue = generatedWorldValue,
                LootExtractedWorldValue = extractedWorldValue,
                LootExtractedTradeableWorldValue = extractedTradeableWorldValue,
                HeatBefore = hasResolvedHeatApplication ? heatApplication.HeatBefore : latestRun?.HeatAtStart ?? currentHeat,
                HeatAfter = hasResolvedHeatApplication ? heatApplication.HeatAfter : currentHeat,
                HeatTierId = heatTierId,
                HasResearchStatus = hasResearchStatus,
                ResearchProjectId = researchProjectId,
                ResearchStatusKey = researchStatusKey,
                ResearchVerificationRuleResolved = verificationBoundary != null && verificationBoundary.RuleResolved,
                ResearchVerificationDeterministicErrorCode = verificationBoundary?.DeterministicErrorCode ?? (int)ResearchVerificationBoundarySummaryErrorCode.NoPendingResearch,
                VerificationRequired = verificationActionable,
                VerificationAvailable = verificationBoundary != null && verificationBoundary.VerificationAvailable,
                CanClaimProduction = (researchStatus != null && researchStatus.CanClaimProduction) || (verificationBoundary != null && verificationBoundary.CanClaimProduction),
                AdventurerPartyClassIds = partyPreview != null && partyPreview.ClassIds != null ? partyPreview.ClassIds : System.Array.Empty<string>(),
                AdventurerPartyPreviewResolved = partyPreview != null && partyPreview.RuleResolved,
                AdventurerPartyPreviewDeterministicErrorCode = partyPreview != null ? partyPreview.DeterministicErrorCode : (int)AdventurerPartyCompositionSummaryErrorCode.None,
                AdventurerPartyPreviewRuleSourceId = partyPreview != null ? partyPreview.RuleSourceId : string.Empty,
                WouldMutateState = false,
                WouldGrantRewards = false,
                WouldUnlockContent = false,
                WouldCallServer = false,
                WouldProcessOfflineProgress = false
            };

            summary.NextOptimizationSuggestionKey = ChooseSuggestion(summary);
            return summary;
        }

        private static MvpPlayerLoopSummary CreateError(MvpPlayerLoopSummaryErrorCode code)
        {
            return new MvpPlayerLoopSummary
            {
                RuleResolved = false,
                DeterministicErrorCode = (int)code,
                NextOptimizationSuggestionKey = SuggestRunDungeonKey,
                WouldMutateState = false,
                WouldGrantRewards = false,
                WouldUnlockContent = false,
                WouldCallServer = false,
                WouldProcessOfflineProgress = false
            };
        }

        private static RunOutcomeRecord GetLatestRun(RunHistoryState runHistory)
        {
            if (runHistory == null)
            {
                return null;
            }

            if (runHistory.LatestOutcome != null)
            {
                return runHistory.LatestOutcome;
            }

            RunOutcomeRecord[] recent = runHistory.RecentOutcomes;
            return recent != null && recent.Length > 0 ? recent[recent.Length - 1] : null;
        }

        private static MvpDungeonPlacementEntry[] GetOrderedMvpPlacements(MvpDungeonPlacementState placements)
        {
            if (placements == null || placements.Entries == null || placements.Entries.Count == 0)
            {
                return Array.Empty<MvpDungeonPlacementEntry>();
            }

            return placements.Entries
                .Where(entry => entry != null &&
                    MvpDungeonPlacementIds.IsAllowedCategory(entry.CategoryId) &&
                    MvpDungeonPlacementIds.IsAllowedOption(entry.OptionId))
                .OrderBy(entry => Array.IndexOf(MvpDungeonPlacementIds.OrderedCategoryIds, entry.CategoryId))
                .ThenBy(entry => entry.Revision)
                .ToArray();
        }

        private static DungeonSlot? GetSelectedSlot(DungeonLayoutState layout)
        {
            if (layout == null || layout.Slots == null)
            {
                return null;
            }

            foreach (DungeonSlot slot in layout.OrderedSlots())
            {
                if (slot.IsOccupied)
                {
                    return slot;
                }
            }

            return null;
        }

        private static bool HasResearchSignal(
            ResearchStatusPresentation status,
            ResearchVerificationBoundarySummary boundary,
            ResearchPendingState pending,
            ResearchProgressState progress,
            CompletedResearchState completed)
        {
            return (status != null && (status.Pending || status.HasProgressState || status.HasCompletedState || !string.IsNullOrWhiteSpace(status.ProjectId))) ||
                   (boundary != null && (boundary.Pending || boundary.HasProgressState || boundary.HasCompletedState || !string.IsNullOrWhiteSpace(boundary.ProjectId))) ||
                   (pending != null && !string.IsNullOrWhiteSpace(pending.ProjectId)) ||
                   (progress != null && !string.IsNullOrWhiteSpace(progress.ProjectId)) ||
                   (completed != null && completed.ProjectIds != null && completed.ProjectIds.Any(projectId => !string.IsNullOrWhiteSpace(projectId)));
        }

        private static string ResolveResearchStatusKey(
            ResearchStatusPresentation status,
            bool verificationActionable)
        {
            if (status == null)
            {
                return string.Empty;
            }

            if (!status.VerificationRequired)
            {
                return status.StatusLocalizationKey ?? string.Empty;
            }

            return verificationActionable
                ? ResearchVerificationRequiredKey
                : ResearchUnavailableKey;
        }

        private static bool IsVerificationActionable(
            ResearchStatusPresentation status,
            ResearchVerificationBoundarySummary boundary)
        {
            return status != null &&
                   status.VerificationRequired &&
                   boundary != null &&
                   boundary.RuleResolved &&
                   boundary.VerificationRequired &&
                   boundary.VerificationAvailable &&
                   !boundary.CanClaimProduction;
        }

        private static string ChooseSuggestion(MvpPlayerLoopSummary summary)
        {
            if (summary == null || !summary.HasRunOutcome)
            {
                return SuggestRunDungeonKey;
            }

            if (summary.HasResearchStatus && summary.VerificationRequired && summary.VerificationAvailable && !summary.CanClaimProduction)
            {
                return SuggestVerifyResearchStatusKey;
            }

            if (string.Equals(summary.HeatTierId, CurrentHeatTierResolver.ConcernTierId, StringComparison.Ordinal))
            {
                return SuggestReduceHeatPressureKey;
            }

            if (summary.LootGeneratedWorldValue > 0 && summary.LootExtractedWorldValue <= 0)
            {
                return SuggestImproveSurvivabilityOrLayoutKey;
            }

            return SuggestRepeatOrImprovePlacementKey;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    return values[i];
                }
            }

            return string.Empty;
        }
    }
}
