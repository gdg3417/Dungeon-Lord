using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpLoopSummaryPanelPresenterTests
    {
        [Test]
        public void BuildPanelText_NoRunHistory_UsesSafeLocalizedFallbacksAndRunSuggestion()
        {
            MvpPlayerLoopSummary summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasPlacementContext = false,
                HasRunOutcome = false,
                ManaReserve = 7d,
                HeatBefore = 2d,
                HeatAfter = 2d,
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey
            };

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, Localize);

            Assert.That(text, Does.Contain("LOC[ui.mvp_loop.panel.title]"));
            Assert.That(text, Does.Contain("LOC[ui.mvp_loop.value.no_placement]"));
            Assert.That(text, Does.Contain("LOC[ui.mvp_loop.value.no_run]"));
            Assert.That(text, Does.Contain("LOC[mvp_loop.suggestion.run_dungeon]"));
            Assert.That(text, Does.Not.Contain("No run yet"));
            Assert.That(text, Does.Not.Contain("Run the dungeon"));
        }

        [Test]
        public void BuildPanelText_MissingOptionalLootHeatAndResearch_UsesLocalizedSafeDefaults()
        {
            MvpPlayerLoopSummary summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasPlacementContext = true,
                SelectedStructureId = "structure.mana_generator.basic",
                HasRunOutcome = true,
                LatestRunId = "run.missing_optional",
                RunSucceeded = false,
                ManaReserve = 3d,
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestImproveSurvivabilityOrLayoutKey
            };

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, Localize);

            Assert.That(text, Does.Contain("structure.mana_generator.basic"));
            Assert.That(text, Does.Contain("LOC[ui.mvp_loop.run_status.failed] (run.missing_optional)"));
            Assert.That(text, Does.Contain("LOC[ui.mvp_loop.panel.loot_format]:0:0:0"));
            Assert.That(text, Does.Contain("LOC[ui.mvp_loop.value.unknown]"));
            Assert.That(text, Does.Contain("LOC[ui.mvp_loop.value.no_research]"));
            Assert.That(text, Does.Contain("LOC[mvp_loop.suggestion.improve_survivability_or_layout]"));
        }

        [Test]
        public void BuildPanelText_PlayerFacingLabelsAndSuggestionResolveThroughLocalizationKeys()
        {
            var requestedKeys = new List<string>();
            MvpPlayerLoopSummary summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasPlacementContext = true,
                SelectedStructureId = "structure.heat_scrubber.basic",
                HasRunOutcome = true,
                LatestRunId = "run.1",
                RunSucceeded = true,
                ManaReserve = 12d,
                LootGeneratedWorldValue = 9,
                LootExtractedWorldValue = 6,
                LootExtractedTradeableWorldValue = 4,
                HeatBefore = 5d,
                HeatAfter = 8d,
                HeatTierId = CurrentHeatTierResolver.NoticeTierId,
                HasResearchStatus = true,
                ResearchStatusKey = "ui.research.status.active_in_progress",
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey
            };

            MvpLoopSummaryPanelPresenter.BuildPanelText(summary, (key, fallback) =>
            {
                requestedKeys.Add(key);
                return "LOC[" + key + "]";
            });

            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.TitleKey));
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.PlacementFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.LatestRunFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.ManaFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.LootFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.HeatFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.ResearchFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.SuggestionFormatKey));
            Assert.That(requestedKeys, Does.Contain(CurrentHeatTierResolver.NoticeTierId));
            Assert.That(requestedKeys, Does.Contain("ui.research.status.active_in_progress"));
            Assert.That(requestedKeys, Does.Contain(MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey));
        }

        [Test]
        public void BuildPanelText_DoesNotMutateSummaryOrSaveState()
        {
            SaveData save = new SaveData
            {
                structureRuntime = new StructureRuntimeState { ManaReserve = 11d, Heat = 4d },
                runHistory = new RunHistoryState
                {
                    LatestOutcome = new RunOutcomeRecord { RunId = "run.safe", Success = true, HeatAtStart = 1d }
                },
                researchProgress = new ResearchProgressState { ProjectId = "research.project.safe", ProgressUnits = 1d }
            };
            string saveBefore = JsonUtility.ToJson(save);
            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save);
            string summaryBefore = JsonUtility.ToJson(summary);

            MvpLoopSummaryPanelPresenter.BuildPanelText(summary, Localize);

            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(saveBefore));
            Assert.That(JsonUtility.ToJson(summary), Is.EqualTo(summaryBefore));
        }

        private static string Localize(string key, string fallback)
        {
            switch (key)
            {
                case MvpLoopSummaryPanelPresenter.PlacementFormatKey:
                case MvpLoopSummaryPanelPresenter.LatestRunFormatKey:
                case MvpLoopSummaryPanelPresenter.ResearchFormatKey:
                case MvpLoopSummaryPanelPresenter.SuggestionFormatKey:
                    return "LOC[" + key + "]:{0}";
                case MvpLoopSummaryPanelPresenter.ManaFormatKey:
                    return "LOC[" + key + "]:{0:0.##}";
                case MvpLoopSummaryPanelPresenter.LootFormatKey:
                    return "LOC[" + key + "]:{0}:{1}:{2}";
                case MvpLoopSummaryPanelPresenter.HeatFormatKey:
                    return "LOC[" + key + "]:{0:0.##}:{1:0.##}:{2}";
                default:
                    return "LOC[" + key + "]";
            }
        }
    }
}
