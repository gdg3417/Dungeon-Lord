using System.Collections.Generic;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpPlayableScreenPresenterTests
    {
        [Test]
        public void BuildScreenText_OrganizesPlayerFacingLoopIntoPlayableSections()
        {
            var summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasPlacementContext = true,
                DungeonPlacements = new[]
                {
                    new MvpDungeonPlacementEntry
                    {
                        CategoryId = MvpDungeonPlacementIds.RoomCategoryId,
                        OptionId = MvpDungeonPlacementIds.BasicRoomOptionId
                    }
                },
                PlacementEffects = new MvpPlacementEffectsSummary { RuleResolved = true },
                HasRunOutcome = false,
                ManaReserve = 12d,
                HeatBefore = 3d,
                HeatAfter = 3d,
                HasResearchStatus = true,
                ResearchStatusKey = "ui.research.status.active_in_progress",
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey
            };
            var guided = new GuidedMvpActionPathSummary { RuleResolved = true, IsComplete = false };

            string text = MvpPlayableScreenPresenter.BuildScreenText(
                summary,
                guided,
                "Dungeon layout: Floor 0: Room: Basic Room -> Monster: Empty / available -> Trap: Empty / available -> Loot node: Empty / available\nSelected room target: Room 1: Basic Room\nSelected room capacity: Monsters 0/1; Traps 0/1; Loot 0/1\nSelected placement fit: Loot node fits Room 1.\nRoom slot layout: Floor 0: Room 1: Basic Room (Monsters: empty 0/1; Traps: empty 0/1; Loot: empty 0/1)",
                "Room",
                "Basic Room",
                "Role: adds room space and path context.",
                "Compared with Narrow Hall: lower path capacity, better as a connector.",
                "Balanced",
                "Plan: Mana Generator + Balanced adventurer challenge.\nExpected tradeoff: standard loot and heat pressure.",
                string.Empty,
                string.Empty,
                "Status banner.",
                new MvpFirstSessionObjectiveSummary { RuleResolved = true, RequiredRecoveredLootValue = 10, AllowedMaxHeatTierId = CurrentHeatTierResolver.PeaceTierId, CurrentHeatTierId = CurrentHeatTierResolver.PeaceTierId },
                null,
                null,
                Localize);

            Assert.That(text, Does.Contain("Dungeon Command (MVP Loop Summary)"));
            Assert.That(text, Does.Contain("== Top Status =="));
            Assert.That(text, Does.Contain("== Action Controls =="));
            Assert.That(text, Does.Contain("== Latest Result =="));
            Assert.That(text, Does.Contain("Next: Complete the First Dungeon Contract. (First Dungeon Contract)"));
            Assert.That(text, Does.Contain("First Dungeon Contract: In progress. Loot 0 / 10, path incomplete."));
            Assert.That(text, Does.Not.Contain("Path built:"));
            Assert.That(text, Does.Not.Contain("Visit observed:"));
            Assert.That(text, Does.Contain("Status banner."));
            Assert.That(text, Does.Contain("Research: Research in progress"));
            Assert.That(text, Does.Contain("Dungeon composition: Room: Basic Room"));
            Assert.That(text, Does.Contain("Selected room capacity: Monsters 0/1; Traps 0/1; Loot 0/1"));
            Assert.That(text, Does.Contain("Room slot layout: Floor 0: Room 1: Basic Room"));
            Assert.That(text, Does.Not.Contain("Dungeon layout:"));
            Assert.That(text, Does.Contain("Placement: Room / Basic Room"));
            Assert.That(text, Does.Not.Contain("Selected category: Room"));
            Assert.That(text, Does.Not.Contain("Selected option: Basic Room"));
            Assert.That(text, Does.Contain("Run posture: Balanced"));
            Assert.That(text, Does.Not.Contain("Selected posture: Balanced"));
            Assert.That(text, Does.Contain("No adventurer visit yet. Use Run / observe dungeon after the path is ready."));
            Assert.That(text, Does.Not.Contain("Observe adventurer activity to see the first outcome."));
            Assert.That(text, Does.Not.Contain("placement.option"));
            int nextIndex = text.IndexOf("Next:", System.StringComparison.Ordinal);
            int controlsIndex = text.IndexOf("== Action Controls ==", System.StringComparison.Ordinal);
            int resultIndex = text.IndexOf("== Latest Result ==", System.StringComparison.Ordinal);

            Assert.That(CountOccurrences(text, "Next:"), Is.EqualTo(1));
            Assert.That(text.Split('\n')[1], Does.StartWith("Next:"));
            Assert.That(nextIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(nextIndex, Is.LessThan(controlsIndex));
            Assert.That(controlsIndex, Is.LessThan(resultIndex));
            Assert.That(text, Does.Contain("Action button: Run / observe dungeon"));
            Assert.That(text, Does.Contain("Action button: Place / modify selected placement"));
            Assert.That(text, Does.Not.Contain("ui.mvp_"));
        }

        [Test]
        public void BuildScreenText_AfterAppliedAnalysisChange_ShowsRunAgainInstruction()
        {
            var summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                AnalysisUnlocked = true,
                HasRunOutcome = true,
                RunSucceeded = false,
                LatestRunDeathCount = 1,
                HeatTierId = CurrentHeatTierResolver.NoticeTierId,
                AnalysisAdviceKey = BasicRunAnalysisRecommendationPresenter.ReduceDangerKey,
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey,
                PlacementEffects = new MvpPlacementEffectsSummary { RuleResolved = true, Danger = 3 },
                LatestRunPlacementEffects = new MvpPlacementEffectsSummary { RuleResolved = true, Danger = 4 }
            };

            string text = MvpPlayableScreenPresenter.BuildScreenText(
                summary,
                new GuidedMvpActionPathSummary { RuleResolved = true },
                string.Empty,
                "Monster",
                "Goblin",
                string.Empty,
                string.Empty,
                "Balanced",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                new MvpFirstSessionObjectiveSummary { RuleResolved = true, IsComplete = true },
                new MvpPostContractGreedTrialSummary { RuleResolved = true, IsActive = true, IsComplete = true },
                null,
                Localize);

            Assert.That(text, Does.Contain("Next: Run again to test the placement change."));
            Assert.That(text, Does.Contain("Applied adjustment: Danger is lower than the latest visit. Run again to test the change."));
            Assert.That(text, Does.Not.Contain("Next: adjust one placement before the next adventurer visit."));
        }


        [Test]
        public void BuildScreenText_RelegatesRecentSpoilsLedgerPanelFromDefault()
        {
            string text = MvpPlayableScreenPresenter.BuildScreenText(
                new MvpPlayerLoopSummary { RuleResolved = true, HasResearchStatus = true, ResearchStatusKey = MvpPlayerLoopSummaryPresenter.ResearchUnavailableKey, NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey },
                new GuidedMvpActionPathSummary { RuleResolved = true },
                string.Empty,
                "Loot",
                "Basic Loot",
                string.Empty,
                string.Empty,
                "Balanced",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                new MvpFirstSessionObjectiveSummary { RuleResolved = true },
                null,
                new MvpRecentSpoilsLedgerSummary
                {
                    RuleResolved = true,
                    HasRunHistory = true,
                    HasLootData = true,
                    LatestTradeableValue = 5,
                    RecentBestTradeableValue = 5,
                    LatestNamedLootTextAvailable = true,
                    LatestLootBreakdown = new[] { new RunLootDropRecord { NameKey = "loot.item.salvage.trap.name", Quantity = 1, TotalTradeableWorldValue = 5 } },
                    TrendKey = MvpRecentSpoilsLedgerPresenter.TrendLatestBestKey,
                    HasAppraisal = true,
                    AppraisalKey = MvpRecentSpoilsLedgerPresenter.AppraisalItemTradeGoodKey,
                    AppraisalArgumentNameKey = "loot.item.salvage.trap.name"
                },
                Localize);

            Assert.That(text, Does.Not.Contain("Recent Spoils Ledger"));
            Assert.That(text, Does.Not.Contain("Latest haul: 1x Trap salvage"));
            Assert.That(text, Does.Contain("Details: press F5"));
            Assert.That(text, Does.Not.Contain("ui.mvp_spoils_ledger"));
        }


        [Test]
        public void BuildScreenText_SpoilsLedgerDoesNotOverridePrimaryAction()
        {
            string text = MvpPlayableScreenPresenter.BuildScreenText(
                new MvpPlayerLoopSummary { RuleResolved = true, AnalysisUnlocked = true, HasRunOutcome = true, AnalysisAdviceKey = BasicRunAnalysisRecommendationPresenter.ReduceDangerKey, PlacementEffects = new MvpPlacementEffectsSummary { RuleResolved = true }, LatestRunPlacementEffects = new MvpPlacementEffectsSummary { RuleResolved = true } },
                new GuidedMvpActionPathSummary { RuleResolved = true, NextActionKey = GuidedMvpActionPathPanelPresenter.FallbackActionKey },
                string.Empty,
                "Loot",
                "Basic Loot",
                string.Empty,
                string.Empty,
                "Balanced",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                new MvpFirstSessionObjectiveSummary { RuleResolved = true, IsComplete = false, RequiredRecoveredLootValue = 10 },
                new MvpPostContractGreedTrialSummary { RuleResolved = true, IsActive = true, IsComplete = false, NextActionKey = MvpPostContractGreedTrialPresenter.NextActionTestGreedierSetupKey },
                new MvpRecentSpoilsLedgerSummary { RuleResolved = true, HasRunHistory = true, HasLootData = true, LatestTradeableValue = 5, RecentBestTradeableValue = 5, TrendKey = MvpRecentSpoilsLedgerPresenter.TrendLatestBestKey },
                Localize);

            Assert.That(text, Does.Not.Contain("Recent Spoils Ledger"));
            Assert.That(text, Does.Contain("Next: Complete the First Dungeon Contract. (First Dungeon Contract)"));
            Assert.That(text, Does.Not.Contain("Next: Latest run produced the strongest recent haul."));
        }


        [Test]
        public void BuildScreenText_SeparatesCurrentHeatFromLatestRunHeatMovement()
        {
            var summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasRunOutcome = true,
                RunSucceeded = true,
                HeatBefore = 8.9d,
                HeatAfter = 11.45d,
                CurrentHeat = 4d,
                CurrentHeatTierId = CurrentHeatTierResolver.PeaceTierId,
                LatestRunHeatTierId = CurrentHeatTierResolver.NoticeTierId,
                HeatTierId = CurrentHeatTierResolver.PeaceTierId,
                PlacementEffects = new MvpPlacementEffectsSummary { RuleResolved = true },
                LatestRunPlacementEffects = new MvpPlacementEffectsSummary { RuleResolved = true }
            };

            string text = MvpPlayableScreenPresenter.BuildScreenText(
                summary,
                new GuidedMvpActionPathSummary { RuleResolved = true },
                string.Empty,
                "Loot",
                "Basic Loot",
                string.Empty,
                string.Empty,
                "Balanced",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                new MvpFirstSessionObjectiveSummary { RuleResolved = true, IsComplete = true },
                new MvpPostContractGreedTrialSummary { RuleResolved = true, IsActive = true, IsComplete = true },
                null,
                Localize);

            Assert.That(text, Does.Contain("Current heat: 4 (Peace)."));
            Assert.That(text, Does.Contain("Heat: 8.9 -> 11.45 (Notice). Risk increased."));
            Assert.That(text, Does.Not.Contain("Heat: 8.9 -> 11.45 (Peace)"));
            Assert.That(text, Does.Not.Contain("Current heat: 4 (Notice)"));
            Assert.That(text, Does.Not.Contain("heat_tier."));
            Assert.That(text, Does.Not.Contain("ui.mvp_"));
        }

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }
            return count;
        }

        private static string Localize(string key, string fallback)
        {
            return Strings.TryGetValue(key, out string value) ? value : fallback;
        }

        private static readonly Dictionary<string, string> Strings = new Dictionary<string, string>
        {

            [MvpPrimaryNextActionPresenter.CompactLineFormatKey] = "Next: {0} ({1})",
            [MvpPrimaryNextActionPresenter.SourceFirstContractKey] = "First Dungeon Contract",
            [MvpPrimaryNextActionPresenter.SourceGreedTrialKey] = "Post-Contract Greed Trial",
            [MvpPrimaryNextActionPresenter.SourceAppliedAdjustmentKey] = "Applied activity-analysis change",
            [MvpPrimaryNextActionPresenter.SourceAnalysisKey] = "Adventurer Activity Analysis",
            [MvpPrimaryNextActionPresenter.SourceGuidedPathKey] = "Guided MVP path",
            [MvpPrimaryNextActionPresenter.SourceSummaryKey] = "Dungeon loop summary",
            [MvpPrimaryNextActionPresenter.FirstContractIncompleteActionKey] = "Complete the First Dungeon Contract.",
            [MvpRecentSpoilsLedgerPresenter.TitleKey] = "Recent Spoils Ledger",
            [MvpRecentSpoilsLedgerPresenter.LatestHaulFormatKey] = "Latest haul: {0}",
            [MvpRecentSpoilsLedgerPresenter.RecoveredValueFormatKey] = "Recovered value: {0} tradeable",
            [MvpRecentSpoilsLedgerPresenter.RecentBestFormatKey] = "Recent best haul: {0} tradeable",
            [MvpRecentSpoilsLedgerPresenter.TrendFormatKey] = "Spoils trend: {0}",
            [MvpRecentSpoilsLedgerPresenter.AppraisalFormatKey] = "Appraisal: {0}",
            [MvpRecentSpoilsLedgerPresenter.AppraisalItemTradeGoodKey] = "{0} is a useful dungeon trade good. Keep greed pressure stable to recover better hauls.",
            [MvpRecentSpoilsLedgerPresenter.AppraisalValueOnlyKey] = "Recovered trade goods are ready for future merchant systems.",
            [MvpRecentSpoilsLedgerPresenter.TrendLatestBestKey] = "Latest run produced the strongest recent haul.",
            [MvpPostContractGreedTrialPresenter.TitleKey] = "Post-Contract Greed Trial",
            [MvpPostContractGreedTrialPresenter.GreedSetupFormatKey] = "Greedier reward setup: {0}",
            [MvpPostContractGreedTrialPresenter.HeatStabilizedFormatKey] = "Heat stabilized: {0}",
            [MvpPostContractGreedTrialPresenter.RiskResponseFormatKey] = "Counterplay response: {0}",
            [MvpPostContractGreedTrialPresenter.StatusFormatKey] = "Trial status: {0}",
            [MvpPostContractGreedTrialPresenter.StatusInProgressKey] = "In progress",
            [MvpPostContractGreedTrialPresenter.ValueCompleteKey] = "complete",
            [MvpPostContractGreedTrialPresenter.ValueIncompleteKey] = "incomplete",
            [MvpPostContractGreedTrialPresenter.NextActionTestGreedierSetupKey] = "Try a greedier reward setup.",
            [MvpLoopSummaryPanelPresenter.LootEntryFormatKey] = "{0}x {1}",
            ["loot.item.salvage.trap.name"] = "Trap salvage",
            [MvpPlayableScreenPresenter.ActionControlsKey] = "Action Controls",
            [MvpPlayableScreenPresenter.LatestResultKey] = "Latest Result",
            [MvpPlayableScreenPresenter.DetailsHintKey] = "Details: press F5 to cycle focused sections, F6 to copy full smoke evidence, or show diagnostics from the action panel.",
            [MvpPlayableScreenPresenter.RoomTargetControlFormatKey] = "Room target: {0}; {1}",
            [MvpPlayableScreenPresenter.PlacementControlFormatKey] = "Placement: {0} / {1}",
            [MvpPlayableScreenPresenter.PlaceButtonControlKey] = "Action button: Place / modify selected placement",
            [MvpPlayableScreenPresenter.RunPostureControlFormatKey] = "Run posture: {0}",
            [MvpPlayableScreenPresenter.RunButtonControlKey] = "Action button: Run / observe dungeon",
            [MvpPlayableScreenPresenter.LatestResultFormatKey] = "{0}; {1}; {2}; {3}; {4}",
            [MvpPlayableScreenPresenter.LatestResultNoRunKey] = "No adventurer visit yet. Use Run / observe dungeon after the path is ready.",
            [MvpPlayableScreenPresenter.CurrentHeatFormatKey] = "Current heat: {0:0.##} ({1}).",
            [MvpPlayableScreenPresenter.TitleKey] = "Dungeon Command (MVP Loop Summary)",
            [MvpPlayableScreenPresenter.TopStatusKey] = "Top Status",
            [MvpPlayableScreenPresenter.CurrentDungeonKey] = "Current Dungeon",
            [MvpPlayableScreenPresenter.BuildChoiceKey] = "Build Choice",
            [MvpPlayableScreenPresenter.RunSetupKey] = "Activity Setup",
            [MvpPlayableScreenPresenter.LatestRunKey] = "Latest Adventurer Visit",
            [MvpPlayableScreenPresenter.AnalysisNextActionKey] = "Analysis and Next Action",
            [MvpPlayableScreenPresenter.FirstContractKey] = "First Dungeon Contract",
            [MvpFirstSessionObjectivePresenter.TitleKey] = "First Dungeon Contract",
            [MvpFirstSessionObjectivePresenter.PathBuiltFormatKey] = "Path built: {0}",
            [MvpFirstSessionObjectivePresenter.RunObservedFormatKey] = "Visit observed: {0}",
            [MvpFirstSessionObjectivePresenter.LootRecoveredFormatKey] = "Loot recovered: {0} / {1}",
            [MvpFirstSessionObjectivePresenter.HeatTargetFormatKey] = "Heat target: {0} (current: {1})",
            [MvpFirstSessionObjectivePresenter.AnalysisFormatKey] = "Analysis: {0}",
            [MvpFirstSessionObjectivePresenter.StatusFormatKey] = "Contract status: {0}",
            [MvpFirstSessionObjectivePresenter.CompleteKey] = "complete",
            [MvpFirstSessionObjectivePresenter.IncompleteKey] = "incomplete",
            [MvpFirstSessionObjectivePresenter.AnalysisUnlockedKey] = "Adventurer Activity Analysis unlocked",
            [MvpFirstSessionObjectivePresenter.AnalysisLockedKey] = "unlock Adventurer Activity Analysis",
            [MvpFirstSessionObjectivePresenter.StatusInProgressKey] = "In progress",
            [MvpFirstSessionObjectivePresenter.StatusCompleteKey] = "Complete. Try a riskier setup or improve loot recovery.",
            [MvpFirstSessionObjectivePresenter.CompactInProgressFormatKey] = "{0}: {1}. Loot {2} / {3}, {4}.",
            [MvpFirstSessionObjectivePresenter.CompactCompleteFormatKey] = "{0}: {1}",
            [MvpFirstSessionObjectivePresenter.CompactPathCompleteKey] = "path complete",
            [MvpFirstSessionObjectivePresenter.CompactPathIncompleteKey] = "path incomplete",
            [CurrentHeatTierResolver.PeaceTierId] = "Peace",
            [CurrentHeatTierResolver.NoticeTierId] = "Notice",
            [MvpLoopSummaryPanelPresenter.RunSucceededKey] = "Succeeded",
            [MvpLoopSummaryPanelPresenter.RunFailedKey] = "Failed",
            [MvpLoopSummaryPanelPresenter.RiskIncreasedKey] = "Risk increased.",
            [MvpLoopSummaryPanelPresenter.RiskStableKey] = "Risk stayed steady.",
            [MvpLoopSummaryPanelPresenter.RiskReducedKey] = "Risk went down.",
            [MvpPostContractGreedTrialPresenter.StatusCompleteKey] = "Complete. Greed pressure tested and stabilized.",
            [MvpPlayableScreenPresenter.SectionHeaderFormatKey] = "== {0} ==",
            [MvpPlayableScreenPresenter.SelectedCategoryFormatKey] = "Selected category: {0}",
            [MvpPlayableScreenPresenter.SelectedOptionFormatKey] = "Selected option: {0}",
            [MvpPlayableScreenPresenter.SelectedPlacementFormatKey] = "Selected placement: {0} / {1}",
            [MvpPlayableScreenPresenter.RunPostureFormatKey] = "Debug selected posture: {0}",
            [MvpPlayableScreenPresenter.PlacePromptKey] = "Next build step: choose an option, then place or modify it.",
            [MvpPlayableScreenPresenter.RunPromptKey] = "Next activity step: observe the dungeon when ready.",
            [MvpPlayableScreenPresenter.NoRunFeedbackKey] = "No adventurer visit observed yet this session.",
            [MvpPlayableScreenPresenter.NoAnalysisKey] = "Why it happened: observe adventurer activity to see the first result.",
            [MvpPlayableScreenPresenter.PartyUnavailableKey] = "Party: no adventurers observed yet.",
            [MvpPlayableScreenPresenter.PartyFormatKey] = "Party: {0}",
            [MvpPlayableScreenPresenter.ResearchFormatKey] = "Research: {0}",
            [MvpPlayableScreenPresenter.PathCompleteFormatKey] = "Path complete: {0}",
            [MvpPlayableScreenPresenter.PlayerViewStatusKey] = "Player view: diagnostics hidden.",
            [MvpDungeonLayoutPresenter.RoomSlotLayoutFormatKey] = "Room slot layout: {0}",
            [MvpRoomSlotTargetPresenter.SelectedTargetFormatKey] = "Selected room target: Room {0}: {1}",
            [MvpRoomSlotTargetPresenter.SelectedCapacityFormatKey] = "Selected room capacity: {0}",
            [MvpRoomSlotTargetPresenter.SelectedPlacementFitFormatKey] = "Selected placement fit: {0}",
            [MvpLoopSummaryPanelPresenter.CompositionFormatKey] = "Dungeon composition: {0}",
            [MvpLoopSummaryPanelPresenter.ManaFormatKey] = "Mana reserve: {0:0.##}",
            [MvpLoopSummaryPanelPresenter.InlineSeparatorKey] = " | ",
            [MvpLoopSummaryPanelPresenter.HeatFormatKey] = "Heat: {0:0.##} -> {1:0.##} ({2}). {3}",
            [MvpLoopSummaryPanelPresenter.ResearchFormatKey] = "{0}",
            [MvpLoopSummaryPanelPresenter.PlacementEffectsFormatKey] = "Effects: {0}",
            [MvpLoopSummaryPanelPresenter.LootFormatKey] = "Loot: {1}/{0} recovered; {2} tradeable.",
            [MvpLoopSummaryPanelPresenter.SuggestionFormatKey] = "{0}",
            [BasicRunAnalysisAppliedAdjustmentPresenter.AppliedAdjustmentFormatKey] = "Applied adjustment: {0}",
            [AdventurerRunIntentPresenter.SummaryFormatKey] = "Expected next adventurer intent: {0} likely. Reason: {1}",
                [AdventurerArrivalPressurePresenter.SummaryFormatKey] = "Adventurer pressure: {0}. Reason: {1}.",
                [AdventurerArrivalPressurePresenter.BodyFormatKey] = "{0}. Reason: {1}.",
                [AdventurerArrivalPressurePresenter.DetailFormatKey] = "Adventurer pressure detail: score {0:0.##}; band {1}; rule source {2}; error {3}; loot {4}; attraction {5}; danger {6}; heat pressure {7}; recent deaths {8}; recovered loot {9}; path complete {10}; latest visit {11}.",
                ["ui.adventurer_pressure.band.not_yet"] = "not yet",
                ["ui.adventurer_pressure.band.low"] = "low",
                ["ui.adventurer_pressure.band.cautious_interest"] = "cautious interest",
                ["ui.adventurer_pressure.band.building_slowly"] = "building slowly",
                ["ui.adventurer_pressure.band.likely_soon"] = "likely soon",
                ["ui.adventurer_pressure.outcome.none"] = "none yet",
                ["ui.adventurer_pressure.outcome.success"] = "success",
                ["ui.adventurer_pressure.outcome.failure"] = "failure",
                [AdventurerArrivalPressureResolver.ReasonNotYetKey] = "current dungeon signals are still forming",
                [AdventurerArrivalPressureResolver.ReasonHighLootLowHeatKey] = "high loot signal and low heat",
                [AdventurerArrivalPressureResolver.ReasonModestLootLowAttractionKey] = "modest loot and low attraction",
                [AdventurerArrivalPressureResolver.ReasonDeathsHeatKey] = "recent deaths and rising heat",
                [AdventurerArrivalPressureResolver.ReasonIncompletePathWeakLootKey] = "incomplete path or weak loot signal",
                [AdventurerRunIntentPresenter.BodyFormatKey] = "{0} likely. Reason: {1}",
                [AdventurerRunIntentPresenter.DebugPostureFormatKey] = "Expected next adventurer intent: {0} likely. Debug selected posture: {1}.",
            [AdventurerRunIntentResolver.ReasonFallbackKey] = "current dungeon signals are still forming",
                [AdventurerRunIntentResolver.ReasonLootHighHeatLowKey] = "loot attraction is high and route heat pressure is low",
                [AdventurerRunIntentResolver.ReasonDeathsHeatKey] = "recent deaths and rising heat",
                [AdventurerRunIntentResolver.ReasonModerateKey] = "risk and reward are both moderate",
                [AdventurerRunIntentResolver.ReasonDangerKey] = "danger pressure is high",
            ["run.posture.cautious.name"] = "Cautious",
                ["run.posture.balanced.name"] = "Balanced",
                ["run.posture.greedy.name"] = "Greedy",
            [MvpLoopSummaryPanelPresenter.ValueNoRunKey] = "No adventurer visit yet. Use Run / observe dungeon after the path is ready.",
            [MvpLoopSummaryPanelPresenter.ValueUnknownKey] = "Unknown",
            [MvpLoopSummaryPanelPresenter.RiskNoRunKey] = "Risk will be shown after an adventurer visit.",
            [MvpLoopSummaryPanelPresenter.ValueNoPlacementKey] = "No dungeon placements yet",
            [MvpPlacementEffectsPresenter.EmptyKey] = "none yet",
            [MvpPlacementEffectsPresenter.CombinedFormatKey] = "{0}",
            [MvpDungeonPlacementPresenter.EntryFormatKey] = "{0}: {1}",
            [MvpDungeonPlacementPresenter.SeparatorKey] = "; ",
            ["placement.category.room.display_name"] = "Room",
            ["placement.option.room.basic.display_name"] = "Basic Room",
            ["ui.research.status.active_in_progress"] = "Research in progress",
            [MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey] = "Observe adventurer activity to see the first outcome.",
            [MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey] = "Next: adjust one placement before the next adventurer visit.",
            [BasicRunAnalysisRecommendationPresenter.ReduceDangerKey] = "Reduce danger or use a safer posture before pushing for more loot.",
            [BasicRunAnalysisAppliedAdjustmentPresenter.RunAgainToTestChangeKey] = "Run again to test the placement change.",
            [BasicRunAnalysisAppliedAdjustmentPresenter.DangerLowerKey] = "Danger is lower than the latest visit. Run again to test the change.",
            [GuidedMvpActionPathPanelPresenter.CompleteNoKey] = "No"
        };
    }
}
