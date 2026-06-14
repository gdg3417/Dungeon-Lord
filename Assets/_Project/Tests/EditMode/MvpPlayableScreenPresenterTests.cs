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
                Localize);

            Assert.That(text, Does.Contain("Dungeon Command (MVP Loop Summary)"));
            Assert.That(text, Does.Contain("== Top Status =="));
            Assert.That(text, Does.Contain("== Current Dungeon =="));
            Assert.That(text, Does.Contain("== Build Choice =="));
            Assert.That(text, Does.Contain("== Run Setup =="));
            Assert.That(text, Does.Contain("== Latest Run =="));
            Assert.That(text, Does.Contain("== Analysis and Next Action =="));
            Assert.That(text, Does.Contain("Mana reserve: 12"));
            Assert.That(text, Does.Contain("Dungeon composition: Room: Basic Room"));
            Assert.That(text, Does.Contain("Selected category: Room"));
            Assert.That(text, Does.Contain("Selected option: Basic Room"));
            Assert.That(text, Does.Contain("Effects: none yet"));
            Assert.That(text, Does.Contain("Selected posture: Balanced"));
            Assert.That(text, Does.Contain("No run yet"));
            Assert.That(text, Does.Contain("Run the dungeon to observe the first outcome."));
            Assert.That(text, Does.Contain("Path complete: No"));
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
            [MvpPlayableScreenPresenter.SectionHeaderFormatKey] = "== {0} ==",
            [MvpPlayableScreenPresenter.SelectedCategoryFormatKey] = "Selected category: {0}",
            [MvpPlayableScreenPresenter.SelectedOptionFormatKey] = "Selected option: {0}",
            [MvpPlayableScreenPresenter.RunPostureFormatKey] = "Selected posture: {0}",
            [MvpPlayableScreenPresenter.PlacePromptKey] = "No build change yet this session.",
            [MvpPlayableScreenPresenter.RunPromptKey] = "Next run step: run or observe the dungeon when ready.",
            [MvpPlayableScreenPresenter.NoRunFeedbackKey] = "No run observed yet this session.",
            [MvpPlayableScreenPresenter.NoAnalysisKey] = "Why it happened: run the dungeon to see the first result.",
            [MvpPlayableScreenPresenter.PartyUnavailableKey] = "Party: no adventurers observed yet.",
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
            [GuidedMvpActionPathPanelPresenter.CompleteFormatKey] = "Path complete: {0}",
            [GuidedMvpActionPathPanelPresenter.CompleteNoKey] = "No"
        };
    }
}
