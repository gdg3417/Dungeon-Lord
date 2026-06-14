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
                "Dungeon layout: Floor 0: Room: Basic Room -> Monster: Empty / available -> Trap: Empty / available -> Loot node: Empty / available",
                "Room",
                "Basic Room",
                "Role: adds room space and path context.",
                "Compared with Narrow Hall: lower path capacity, better as a connector.",
                "Balanced",
                "Plan: Mana Generator + Balanced run.\nExpected tradeoff: standard loot and heat pressure.",
                string.Empty,
                string.Empty,
                "Status banner.",
                new MvpFirstSessionObjectiveSummary { RuleResolved = true, RequiredRecoveredLootValue = 10, AllowedMaxHeatTierId = CurrentHeatTierResolver.PeaceTierId, CurrentHeatTierId = CurrentHeatTierResolver.PeaceTierId },
                Localize);

            Assert.That(text, Does.Contain("Dungeon Command (MVP Loop Summary)"));
            Assert.That(text, Does.Contain("== Top Status =="));
            Assert.That(text, Does.Contain("== Current Dungeon =="));
            Assert.That(text, Does.Contain("== Build Choice =="));
            Assert.That(text, Does.Contain("== Run Setup =="));
            Assert.That(text, Does.Contain("== Latest Run =="));
            Assert.That(text, Does.Contain("== Analysis and Next Action =="));
            Assert.That(text, Does.Contain("First Dungeon Contract: In progress. Loot 0 / 10, path incomplete."));
            Assert.That(text, Does.Not.Contain("Path built:"));
            Assert.That(text, Does.Not.Contain("Run observed:"));
            Assert.That(text, Does.Contain("Player view: diagnostics hidden."));
            Assert.That(text, Does.Contain("Status banner."));
            Assert.That(text, Does.Contain("Mana reserve: 12"));
            Assert.That(text, Does.Contain("Research: Research in progress"));
            Assert.That(text, Does.Contain("Dungeon composition: Room: Basic Room"));
            Assert.That(text, Does.Contain("Selected placement: Room / Basic Room"));
            Assert.That(text, Does.Not.Contain("Selected category: Room"));
            Assert.That(text, Does.Not.Contain("Selected option: Basic Room"));
            Assert.That(text, Does.Contain("Selected posture: Balanced"));
            Assert.That(text, Does.Contain("Plan: Mana Generator + Balanced run."));
            Assert.That(text, Does.Contain("Expected tradeoff: standard loot and heat pressure."));
            Assert.That(text, Does.Contain("Next build step: choose an option, then place or modify it."));
            Assert.That(text, Does.Contain("No run yet"));
            Assert.That(text, Does.Contain("Run the dungeon to observe the first outcome."));
            Assert.That(text, Does.Contain("Path complete: No"));
            Assert.That(text.IndexOf("== Latest Run ==", System.StringComparison.Ordinal), Is.LessThan(text.IndexOf("== Build Choice ==", System.StringComparison.Ordinal)));
            Assert.That(text.IndexOf("== Analysis and Next Action ==", System.StringComparison.Ordinal), Is.LessThan(text.IndexOf("== Build Choice ==", System.StringComparison.Ordinal)));
            Assert.That(text, Does.Not.Contain("placement.option"));
            Assert.That(text, Does.Not.Contain("ui.mvp_"));
        }

        private static string Localize(string key, string fallback)
        {
            return Strings.TryGetValue(key, out string value) ? value : fallback;
        }

        private static readonly Dictionary<string, string> Strings = new Dictionary<string, string>
        {
            [MvpPlayableScreenPresenter.TitleKey] = "Dungeon Command (MVP Loop Summary)",
            [MvpPlayableScreenPresenter.TopStatusKey] = "Top Status",
            [MvpPlayableScreenPresenter.CurrentDungeonKey] = "Current Dungeon",
            [MvpPlayableScreenPresenter.BuildChoiceKey] = "Build Choice",
            [MvpPlayableScreenPresenter.RunSetupKey] = "Run Setup",
            [MvpPlayableScreenPresenter.LatestRunKey] = "Latest Run",
            [MvpPlayableScreenPresenter.AnalysisNextActionKey] = "Analysis and Next Action",
            [MvpPlayableScreenPresenter.FirstContractKey] = "First Dungeon Contract",
            [MvpFirstSessionObjectivePresenter.TitleKey] = "First Dungeon Contract",
            [MvpFirstSessionObjectivePresenter.PathBuiltFormatKey] = "Path built: {0}",
            [MvpFirstSessionObjectivePresenter.RunObservedFormatKey] = "Run observed: {0}",
            [MvpFirstSessionObjectivePresenter.LootRecoveredFormatKey] = "Loot recovered: {0} / {1}",
            [MvpFirstSessionObjectivePresenter.HeatTargetFormatKey] = "Heat target: {0} (current: {1})",
            [MvpFirstSessionObjectivePresenter.AnalysisFormatKey] = "Analysis: {0}",
            [MvpFirstSessionObjectivePresenter.StatusFormatKey] = "Contract status: {0}",
            [MvpFirstSessionObjectivePresenter.CompleteKey] = "complete",
            [MvpFirstSessionObjectivePresenter.IncompleteKey] = "incomplete",
            [MvpFirstSessionObjectivePresenter.AnalysisUnlockedKey] = "Basic Run Analysis unlocked",
            [MvpFirstSessionObjectivePresenter.AnalysisLockedKey] = "unlock Basic Run Analysis",
            [MvpFirstSessionObjectivePresenter.StatusInProgressKey] = "In progress",
            [MvpFirstSessionObjectivePresenter.StatusCompleteKey] = "Complete. Try a riskier setup or improve loot recovery.",
            [MvpFirstSessionObjectivePresenter.CompactInProgressFormatKey] = "{0}: {1}. Loot {2} / {3}, {4}.",
            [MvpFirstSessionObjectivePresenter.CompactCompleteFormatKey] = "{0}: {1}",
            [MvpFirstSessionObjectivePresenter.CompactPathCompleteKey] = "path complete",
            [MvpFirstSessionObjectivePresenter.CompactPathIncompleteKey] = "path incomplete",
            [CurrentHeatTierResolver.PeaceTierId] = "Peace",
            [MvpPlayableScreenPresenter.SectionHeaderFormatKey] = "== {0} ==",
            [MvpPlayableScreenPresenter.SelectedCategoryFormatKey] = "Selected category: {0}",
            [MvpPlayableScreenPresenter.SelectedOptionFormatKey] = "Selected option: {0}",
            [MvpPlayableScreenPresenter.SelectedPlacementFormatKey] = "Selected placement: {0} / {1}",
            [MvpPlayableScreenPresenter.RunPostureFormatKey] = "Selected posture: {0}",
            [MvpPlayableScreenPresenter.PlacePromptKey] = "Next build step: choose an option, then place or modify it.",
            [MvpPlayableScreenPresenter.RunPromptKey] = "Next run step: run or observe the dungeon when ready.",
            [MvpPlayableScreenPresenter.NoRunFeedbackKey] = "No run observed yet this session.",
            [MvpPlayableScreenPresenter.NoAnalysisKey] = "Why it happened: run the dungeon to see the first result.",
            [MvpPlayableScreenPresenter.PartyUnavailableKey] = "Party: no adventurers observed yet.",
            [MvpPlayableScreenPresenter.PartyFormatKey] = "Party: {0}",
            [MvpPlayableScreenPresenter.ResearchFormatKey] = "Research: {0}",
            [MvpPlayableScreenPresenter.PathCompleteFormatKey] = "Path complete: {0}",
            [MvpPlayableScreenPresenter.PlayerViewStatusKey] = "Player view: diagnostics hidden.",
            [MvpLoopSummaryPanelPresenter.CompositionFormatKey] = "Dungeon composition: {0}",
            [MvpLoopSummaryPanelPresenter.ManaFormatKey] = "Mana reserve: {0:0.##}",
            [MvpLoopSummaryPanelPresenter.HeatFormatKey] = "Heat: {0:0.##} -> {1:0.##} ({2}). {3}",
            [MvpLoopSummaryPanelPresenter.ResearchFormatKey] = "{0}",
            [MvpLoopSummaryPanelPresenter.PlacementEffectsFormatKey] = "Effects: {0}",
            [MvpLoopSummaryPanelPresenter.LootFormatKey] = "Loot: {1}/{0} recovered; {2} tradeable.",
            [MvpLoopSummaryPanelPresenter.SuggestionFormatKey] = "{0}",
            [MvpLoopSummaryPanelPresenter.ValueNoRunKey] = "No run yet",
            [MvpLoopSummaryPanelPresenter.ValueUnknownKey] = "Unknown",
            [MvpLoopSummaryPanelPresenter.RiskNoRunKey] = "Risk will be shown after a run.",
            [MvpLoopSummaryPanelPresenter.ValueNoPlacementKey] = "No dungeon placements yet",
            [MvpPlacementEffectsPresenter.EmptyKey] = "none yet",
            [MvpPlacementEffectsPresenter.CombinedFormatKey] = "{0}",
            [MvpDungeonPlacementPresenter.EntryFormatKey] = "{0}: {1}",
            [MvpDungeonPlacementPresenter.SeparatorKey] = "; ",
            ["placement.category.room.display_name"] = "Room",
            ["placement.option.room.basic.display_name"] = "Basic Room",
            ["ui.research.status.active_in_progress"] = "Research in progress",
            [MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey] = "Run the dungeon to observe the first outcome.",
            [GuidedMvpActionPathPanelPresenter.CompleteNoKey] = "No"
        };
    }
}
