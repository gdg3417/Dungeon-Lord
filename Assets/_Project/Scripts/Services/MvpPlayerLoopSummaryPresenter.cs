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
        public const string SuggestBasicAnalysisNoRunKey = BasicRunAnalysisRecommendationPresenter.RunForAnalysisKey;
        public const string SuggestBasicAnalysisReduceDangerKey = BasicRunAnalysisRecommendationPresenter.ReduceDangerKey;
        public const string SuggestBasicAnalysisReduceHeatKey = BasicRunAnalysisRecommendationPresenter.ReduceHeatKey;
        public const string SuggestBasicAnalysisImproveExtractionKey = BasicRunAnalysisRecommendationPresenter.ImproveExtractionKey;
        public const string SuggestBasicAnalysisTestGreedierKey = BasicRunAnalysisRecommendationPresenter.TestGreedierKey;
        public const string BasicRunAnalysisUnlockId = "research.unlock.basic_run_analysis";
        public const string ResearchVerificationRequiredKey = "ui.research.status.verification_required";
        public const string ResearchCompletedKey = "ui.research.status.completed";
        public const string ResearchUnavailableKey = MvpPlayerFacingLabelResolver.ResearchUnavailableKey;

        public static MvpPlayerLoopSummary Resolve(
            SaveData save,
            RunSimulationConfig runConfig = null,
            ResearchCompletionEligibilityScaffoldConfig researchEligibilityConfig = null,
            ResearchVerificationScaffoldConfig researchVerificationConfig = null,
            ResearchUnlockBridgeConfig researchUnlockConfig = null,
            PlayerResearchAuthoritySummary playerResearchAuthority = null)
        {
            if (save == null)
            {
                return CreateError(MvpPlayerLoopSummaryErrorCode.MissingSave);
            }

            StructureRuntimeState runtime = save.structureRuntime;
            RunOutcomeRecord latestRun = GetLatestRun(save.runHistory);
            DungeonSlot? selectedSlot = GetSelectedSlot(save.dungeonLayout);
            MvpDungeonPlacementEntry[] dungeonPlacements = MvpRoomSlotLayoutResolver.ResolveActivePlacements(save, runConfig);
            MvpPlacementEffectsSummary placementEffects = MvpPlacementEffectsResolver.ResolveConfiguredRouteForSave(save, runConfig);
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
            ResearchUnlockSummary unlockSummary = ResearchUnlockSummaryPresenter.Resolve(
                save.completedResearch,
                researchUnlockConfig);

            CurrentHeatTierSummary heatTier = CurrentHeatTierResolver.Resolve(runConfig, currentHeat);
            RunHeatApplicationSummary heatApplication = latestRun?.RunHeatApplicationSummary;
            RunLootSummary lootSummary = latestRun?.LootSummary;
            RunLootExtractionSummary extractionSummary = latestRun?.LootExtractionSummary;
            RunSurvivalSummary survivalSummary = latestRun?.SurvivalSummary;

            AdventurerRunIntentSummary runIntent = AdventurerRunIntentResolver.Resolve(runConfig, placementEffects, currentHeat, heatTier, latestRun);
            AdventurerArrivalPressureSummary arrivalPressure = AdventurerArrivalPressureResolver.Resolve(runConfig, placementEffects, heatTier, latestRun);
            AdventurerTrafficPressureSummary trafficPressure = AdventurerTrafficPressureResolver.Resolve(runConfig, arrivalPressure, runIntent);

            bool hasRunOutcome = latestRun != null;
            bool hasResolvedHeatApplication = heatApplication != null && heatApplication.RuleResolved;
            bool hasResearchStatus = HasResearchSignal(researchStatus, verificationBoundary, save.researchPending, save.researchProgress, save.completedResearch);
            bool verificationActionable = IsVerificationActionable(researchStatus, verificationBoundary);
            string researchStatusKey = ResolveResearchStatusKey(researchStatus, verificationActionable);
            bool hasResolvedPlayerResearchAuthority = playerResearchAuthority != null && playerResearchAuthority.RuleResolved;
            if (hasResolvedPlayerResearchAuthority)
            {
                hasResearchStatus = true;
                researchStatusKey = playerResearchAuthority.FeedbackLocalizationKey;
            }
            if (unlockSummary != null &&
                unlockSummary.RuleResolved &&
                !HasActiveResearchSignal(save.researchPending, save.researchProgress) &&
                IsUnavailableOrEmptyResearchStatus(researchStatusKey))
            {
                researchStatusKey = ResearchCompletedKey;
            }
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
                PlacementEffects = placementEffects,
                LatestRunPlacementEffects = ResolveLatestRunPlacementEffects(latestRun, placementEffects),
                HasRunOutcome = hasRunOutcome,
                LatestRunId = latestRun?.RunId ?? string.Empty,
                RunSucceeded = latestRun != null && latestRun.Success,
                FinalRouteOutcomeKey = latestRun?.FinalRouteOutcomeKey ?? string.Empty,
                HighestRoomReached = latestRun?.HighestRoomReached ?? -1,
                ReachedRoomCount = latestRun?.ReachedRoomCount ?? 0,
                ClearedRoomCount = latestRun?.ClearedRoomCount ?? 0,
                RoomResolutions = latestRun?.RoomResolutions ?? Array.Empty<RunRoomResolutionSummary>(),
                ManaReserve = currentMana,
                LootGeneratedWorldValue = generatedWorldValue,
                LootExtractedWorldValue = extractedWorldValue,
                LootExtractedTradeableWorldValue = extractedTradeableWorldValue,
                LootBreakdown = CloneLootBreakdown(latestRun?.LootBreakdown),
                LatestRunPartySize = survivalSummary?.PartySize ?? 0,
                LatestRunSurvivorCount = survivalSummary?.SurvivorCount ?? 0,
                LatestRunDeathCount = survivalSummary?.DeathCount ?? 0,
                HeatBefore = hasResolvedHeatApplication ? heatApplication.HeatBefore : latestRun?.HeatAtStart ?? currentHeat,
                HeatAfter = hasResolvedHeatApplication ? heatApplication.HeatAfter : currentHeat,
                HeatTierId = heatTierId,
                CurrentHeat = currentHeat,
                CurrentHeatTierId = heatTier.RuleResolved ? heatTier.TierId : string.Empty,
                LatestRunHeatTierId = hasResolvedHeatApplication ? heatApplication.TierAfter : heatTierId,
                HasResearchStatus = hasResearchStatus,
                ResearchProjectId = researchProjectId,
                ResearchStatusKey = researchStatusKey,
                HasResearchUnlock = unlockSummary != null && unlockSummary.RuleResolved,
                ResearchUnlockId = unlockSummary?.UnlockId ?? string.Empty,
                ResearchUnlockSummaryKey = unlockSummary?.SummaryLocalizationKey ?? ResearchUnlockSummaryPresenter.NoneKey,
                ResearchUnlockDeterministicErrorCode = unlockSummary?.DeterministicErrorCode ?? (int)ResearchUnlockSummaryErrorCode.NoCompletedResearch,
                ResearchVerificationRuleResolved = verificationBoundary != null && verificationBoundary.RuleResolved,
                ResearchVerificationDeterministicErrorCode = verificationBoundary?.DeterministicErrorCode ?? (int)ResearchVerificationBoundarySummaryErrorCode.NoPendingResearch,
                VerificationRequired = verificationActionable,
                VerificationAvailable = verificationBoundary != null && verificationBoundary.VerificationAvailable,
                CanClaimProduction = (researchStatus != null && researchStatus.CanClaimProduction) || (verificationBoundary != null && verificationBoundary.CanClaimProduction),
                CanClaimLocalMvp = hasResolvedPlayerResearchAuthority && playerResearchAuthority.CanClaimLocalMvp,
                UsesLocalMvpClaimAuthority = hasResolvedPlayerResearchAuthority && playerResearchAuthority.UsesLocalMvpAuthority,
                PlayerResearchAuthority = hasResolvedPlayerResearchAuthority ? playerResearchAuthority : null,
                AdventurerPartyClassIds = partyPreview != null && partyPreview.ClassIds != null ? partyPreview.ClassIds : System.Array.Empty<string>(),
                AdventurerPartyPreviewResolved = partyPreview != null && partyPreview.RuleResolved,
                AdventurerPartyPreviewDeterministicErrorCode = partyPreview != null ? partyPreview.DeterministicErrorCode : (int)AdventurerPartyCompositionSummaryErrorCode.None,
                AdventurerPartyPreviewRuleSourceId = partyPreview != null ? partyPreview.RuleSourceId : string.Empty,
                AdventurerRunIntent = runIntent,
                AdventurerArrivalPressure = arrivalPressure,
                AdventurerTrafficPressure = trafficPressure,
                WouldMutateState = false,
                WouldGrantRewards = false,
                WouldUnlockContent = false,
                WouldCallServer = false,
                WouldProcessOfflineProgress = false
            };

            summary.NextOptimizationSuggestionKey = ChooseSuggestion(summary);
            summary.AnalysisUnlocked = HasBasicRunAnalysisUnlock(summary);
            summary.AnalysisAdviceKey = summary.AnalysisUnlocked ? BasicRunAnalysisRecommendationPresenter.ResolveKey(summary) : string.Empty;
            return summary;
        }

        private static MvpPlacementEffectsSummary ResolveLatestRunPlacementEffects(RunOutcomeRecord latestRun, MvpPlacementEffectsSummary currentPlacementEffects)
        {
            RunCompositionOutcomeSummary composition = latestRun?.CompositionOutcomeSummary;
            MvpPlacementEffectsSummary stored = latestRun?.ConfiguredRoutePlacementEffects ?? composition?.PlacementEffects;
            if (latestRun == null) return currentPlacementEffects;
            if (stored != null && stored.RuleResolved)
            {
                return stored;
            }

            return CreateEmptyResolvedPlacementEffects();
        }

        private static MvpPlacementEffectsSummary CreateEmptyResolvedPlacementEffects()
        {
            return new MvpPlacementEffectsSummary
            {
                RuleResolved = true,
                ContributingOptionIds = Array.Empty<string>(),
                EffectLocalizationKeys = Array.Empty<string>()
            };
        }

        private static RunLootDropRecord[] CloneLootBreakdown(RunLootDropRecord[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<RunLootDropRecord>();
            }

            var clone = new RunLootDropRecord[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                RunLootDropRecord entry = source[i];
                clone[i] = entry == null ? null : new RunLootDropRecord
                {
                    LootId = entry.LootId,
                    NameKey = entry.NameKey,
                    Quantity = entry.Quantity,
                    TotalWorldValue = entry.TotalWorldValue,
                    TotalTradeableWorldValue = entry.TotalTradeableWorldValue
                };
            }

            return clone;
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

        private static bool IsUnavailableOrEmptyResearchStatus(string statusKey)
        {
            return string.IsNullOrWhiteSpace(statusKey) ||
                   string.Equals(statusKey, ResearchUnavailableKey, StringComparison.Ordinal);
        }

        private static bool HasActiveResearchSignal(ResearchPendingState pending, ResearchProgressState progress)
        {
            return (pending != null && !string.IsNullOrWhiteSpace(pending.ProjectId)) ||
                   (progress != null && !string.IsNullOrWhiteSpace(progress.ProjectId));
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

        private static bool HasBasicRunAnalysisUnlock(MvpPlayerLoopSummary summary)
        {
            return summary != null &&
                   summary.HasResearchUnlock &&
                   string.Equals(summary.ResearchUnlockId, BasicRunAnalysisUnlockId, StringComparison.Ordinal);
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
