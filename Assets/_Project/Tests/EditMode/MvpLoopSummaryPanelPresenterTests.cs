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
            Assert.That(text, Does.Contain("LOC[ui.mvp_loop.section.current_dungeon]"));
            Assert.That(text, Does.Contain("LOC[ui.mvp_loop.section.suggested_next_action]"));
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

            Assert.That(text, Does.Contain("LOC[structure.mana_generator.basic.display_name]"));
            Assert.That(text, Does.Not.Contain("LOC[ui.mvp_loop.panel.placement_format]:structure.mana_generator.basic"));
            Assert.That(text, Does.Contain("LOC[ui.mvp_loop.run_status.failed]"));
            Assert.That(text, Does.Not.Contain("run.missing_optional"));
            Assert.That(text, Does.Contain("LOC[ui.mvp_loop.panel.loot_format]:0:0:0"));
            Assert.That(text, Does.Contain("LOC[ui.mvp_loop.risk.no_run]").Or.Contain("LOC[ui.mvp_loop.risk.stable]"));
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
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.LatestRunSectionKey));
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.CurrentDungeonSectionKey));
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.WhyItHappenedSectionKey));
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.RewardsAndRiskSectionKey));
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.ResearchSectionKey));
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.SuggestedNextActionSectionKey));
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.LootFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.HeatFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.ResearchFormatKey));
            Assert.That(requestedKeys, Does.Not.Contain(MvpLoopSummaryPanelPresenter.ResearchUnlockFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.SuggestionFormatKey));
            Assert.That(requestedKeys, Does.Contain("structure.heat_scrubber.basic.display_name"));
            Assert.That(requestedKeys, Does.Contain(CurrentHeatTierResolver.NoticeTierId));
            Assert.That(requestedKeys, Does.Contain("ui.research.status.active_in_progress"));
            Assert.That(requestedKeys, Does.Contain(MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey));
        }

        [Test]
        public void BuildPanelText_NoResearchUnlock_DoesNotAppendUnlockFallbackLine()
        {
            MvpPlayerLoopSummary summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasResearchUnlock = false,
                ResearchUnlockSummaryKey = ResearchUnlockSummaryPresenter.NoneKey,
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey
            };

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, Localize);

            Assert.That(text, Does.Not.Contain("LOC[ui.mvp_loop.panel.research_unlock_format]"));
            Assert.That(text, Does.Not.Contain("LOC[ui.research_unlock.none]"));
        }

        [Test]
        public void BuildPanelText_WithResearchUnlock_DisplaysLocalizedUnlockText()
        {
            MvpPlayerLoopSummary summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasResearchUnlock = true,
                ResearchUnlockId = "research.unlock.basic_run_analysis",
                ResearchUnlockSummaryKey = "ui.research_unlock.basic_run_analysis.summary",
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey
            };

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, Localize);

            Assert.That(text, Does.Contain("LOC[ui.mvp_loop.panel.research_unlock_format]:LOC[ui.research_unlock.basic_run_analysis.summary]"));
            Assert.That(text, Does.Not.Contain("research.unlock.basic_run_analysis"));
            Assert.That(text, Does.Not.Contain("ui.research_unlock.basic_run_analysis.summary:"));
        }

        [Test]
        public void BuildPanelText_CompletedResearchWithUnlock_ShowsCompletedStatusAndUnlockWithoutRawIdsOrKeys()
        {
            MvpPlayerLoopSummary summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasResearchStatus = true,
                ResearchProjectId = "research.project.mvp_loop",
                ResearchStatusKey = MvpPlayerLoopSummaryPresenter.ResearchCompletedKey,
                HasResearchUnlock = true,
                ResearchUnlockId = "research.unlock.basic_run_analysis",
                ResearchUnlockSummaryKey = "ui.research_unlock.basic_run_analysis.summary",
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey
            };

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, SmokeLocalize);

            Assert.That(text, Does.Contain("Research completed"));
            Assert.That(text, Does.Contain("Unlocked: Basic run analysis unlocked"));
            Assert.That(text, Does.Not.Contain("research.project."));
            Assert.That(text, Does.Not.Contain("research.unlock."));
            Assert.That(text, Does.Not.Contain("ui.research_unlock."));
        }


        [Test]
        public void BuildPanelText_WithLootBreakdown_UsesLocalizedNamedLootWithoutRawIdsOrKeys()
        {
            MvpPlayerLoopSummary summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasRunOutcome = true,
                RunSucceeded = true,
                LootGeneratedWorldValue = 9,
                LootExtractedWorldValue = 5,
                LootExtractedTradeableWorldValue = 5,
                LootBreakdown = new[]
                {
                    new RunLootDropRecord { LootId = "loot.item.raw", NameKey = "loot.item.test.name", Quantity = 2, TotalWorldValue = 5, TotalTradeableWorldValue = 5 }
                },
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey
            };

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, Localize);

            Assert.That(text, Does.Contain("LOC[ui.mvp_loop.panel.loot_named_format]:9:5:5:LOC[ui.mvp_loop.panel.loot_entry_format]:2:LOC[loot.item.test.name]"));
            Assert.That(text, Does.Not.Contain("loot.item.raw"));
            Assert.That(text, Does.Not.Contain("loot.item.test.name:"));
        }

        [Test]
        public void BuildPanelText_UnknownResearchStatus_UsesSafeLocalizedFallbackInsteadOfRawProjectId()
        {
            MvpPlayerLoopSummary summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasResearchStatus = true,
                ResearchProjectId = "research.project.unmapped",
                ResearchStatusKey = string.Empty,
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey
            };

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, Localize);

            Assert.That(text, Does.Contain("LOC[ui.research.status.blocked_or_invalid]"));
            Assert.That(text, Does.Not.Contain("research.project.unmapped"));
        }


        [TestCase(2d, 5d, MvpLoopSummaryPanelPresenter.RiskIncreasedKey)]
        [TestCase(5d, 2d, MvpLoopSummaryPanelPresenter.RiskReducedKey)]
        [TestCase(3d, 3d, MvpLoopSummaryPanelPresenter.RiskStableKey)]
        public void BuildPanelText_RunHeatDelta_ExplainsRiskDirection(double before, double after, string riskKey)
        {
            MvpPlayerLoopSummary summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasRunOutcome = true,
                HeatBefore = before,
                HeatAfter = after,
                HeatTierId = CurrentHeatTierResolver.NoticeTierId,
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey
            };

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, Localize);

            Assert.That(text, Does.Contain("LOC[" + riskKey + "]"));
        }

        [Test]
        public void BuildPanelText_UsesFirstSessionSectionOrder()
        {
            MvpPlayerLoopSummary summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasRunOutcome = true,
                RunSucceeded = true,
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey
            };

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, Localize);

            Assert.That(text.IndexOf("LOC[" + MvpLoopSummaryPanelPresenter.CurrentDungeonSectionKey + "]", System.StringComparison.Ordinal), Is.LessThan(text.IndexOf("LOC[" + MvpLoopSummaryPanelPresenter.AdventurerIntentSectionKey + "]", System.StringComparison.Ordinal)));
            Assert.That(text.IndexOf("LOC[" + MvpLoopSummaryPanelPresenter.AdventurerIntentSectionKey + "]", System.StringComparison.Ordinal), Is.LessThan(text.IndexOf("LOC[" + MvpLoopSummaryPanelPresenter.LatestRunSectionKey + "]", System.StringComparison.Ordinal)));
            Assert.That(text.IndexOf("LOC[" + MvpLoopSummaryPanelPresenter.LatestRunSectionKey + "]", System.StringComparison.Ordinal), Is.LessThan(text.IndexOf("LOC[" + MvpLoopSummaryPanelPresenter.WhyItHappenedSectionKey + "]", System.StringComparison.Ordinal)));
            Assert.That(text.IndexOf("LOC[" + MvpLoopSummaryPanelPresenter.WhyItHappenedSectionKey + "]", System.StringComparison.Ordinal), Is.LessThan(text.IndexOf("LOC[" + MvpLoopSummaryPanelPresenter.RewardsAndRiskSectionKey + "]", System.StringComparison.Ordinal)));
            Assert.That(text.IndexOf("LOC[" + MvpLoopSummaryPanelPresenter.RewardsAndRiskSectionKey + "]", System.StringComparison.Ordinal), Is.LessThan(text.IndexOf("LOC[" + MvpLoopSummaryPanelPresenter.ResearchSectionKey + "]", System.StringComparison.Ordinal)));
            Assert.That(text.IndexOf("LOC[" + MvpLoopSummaryPanelPresenter.ResearchSectionKey + "]", System.StringComparison.Ordinal), Is.LessThan(text.IndexOf("LOC[" + MvpLoopSummaryPanelPresenter.SuggestedNextActionSectionKey + "]", System.StringComparison.Ordinal)));
        }


        [Test]
        public void BuildPanelText_LockedBasicRunAnalysis_PreservesConciseWhyAndSuggestion()
        {
            MvpPlayerLoopSummary summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasRunOutcome = true,
                RunSucceeded = true,
                AnalysisUnlocked = false,
                LatestRunPlacementEffects = new MvpPlacementEffectsSummary { RuleResolved = true, Danger = 6 },
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey,
                AnalysisAdviceKey = MvpPlayerLoopSummaryPresenter.SuggestBasicAnalysisReduceDangerKey
            };

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, Localize);

            Assert.That(text, Does.Contain("LOC[" + MvpLoopSummaryPanelPresenter.WhyRunFormatKey + "]:LOC[" + MvpLoopSummaryPanelPresenter.WhyDangerKey + "]"));
            Assert.That(text, Does.Not.Contain("LOC[" + MvpLoopSummaryPanelPresenter.AnalysisRunFormatKey + "]"));
            Assert.That(text, Does.Contain("LOC[" + MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey + "]"));
            Assert.That(text, Does.Not.Contain("LOC[" + MvpPlayerLoopSummaryPresenter.SuggestBasicAnalysisReduceDangerKey + "]"));
        }

        [Test]
        public void BuildPanelText_UnlockedBasicRunAnalysis_HighDangerAddsSpecificAnalysisAndAdvice()
        {
            MvpPlayerLoopSummary summary = AnalysisSummary();
            summary.LatestRunPlacementEffects = new MvpPlacementEffectsSummary { RuleResolved = true, Danger = 8, HeatPressure = 2 };
            summary.AnalysisAdviceKey = MvpPlayerLoopSummaryPresenter.SuggestBasicAnalysisReduceDangerKey;

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, Localize);

            Assert.That(text, Does.Contain("LOC[" + MvpLoopSummaryPanelPresenter.AnalysisRunFormatKey + "]:LOC[" + MvpLoopSummaryPanelPresenter.WhyDangerKey + "]:LOC[" + MvpLoopSummaryPanelPresenter.AnalysisDangerKey + "]"));
            Assert.That(text, Does.Contain("LOC[" + MvpPlayerLoopSummaryPresenter.SuggestBasicAnalysisReduceDangerKey + "]"));
        }

        [Test]
        public void BuildPanelText_UnlockedBasicRunAnalysis_HeatIncreaseAddsSpecificAnalysisAndAdvice()
        {
            MvpPlayerLoopSummary summary = AnalysisSummary();
            summary.HeatBefore = 2d;
            summary.HeatAfter = 5d;
            summary.AnalysisAdviceKey = MvpPlayerLoopSummaryPresenter.SuggestBasicAnalysisReduceHeatKey;

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, Localize);

            Assert.That(text, Does.Contain("LOC[" + MvpLoopSummaryPanelPresenter.AnalysisHeatIncreasedKey + "]"));
            Assert.That(text, Does.Contain("LOC[" + MvpPlayerLoopSummaryPresenter.SuggestBasicAnalysisReduceHeatKey + "]"));
        }

        [Test]
        public void BuildPanelText_UnlockedBasicRunAnalysis_PartialLootAddsSpecificAnalysisAndAdvice()
        {
            MvpPlayerLoopSummary summary = AnalysisSummary();
            summary.LootGeneratedWorldValue = 10;
            summary.LootExtractedWorldValue = 4;
            summary.AnalysisAdviceKey = MvpPlayerLoopSummaryPresenter.SuggestBasicAnalysisImproveExtractionKey;

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, Localize);

            Assert.That(text, Does.Contain("LOC[" + MvpLoopSummaryPanelPresenter.AnalysisPartialLootKey + "]"));
            Assert.That(text, Does.Contain("LOC[" + MvpPlayerLoopSummaryPresenter.SuggestBasicAnalysisImproveExtractionKey + "]"));
        }

        [Test]
        public void BuildPanelText_UnlockedBasicRunAnalysis_NoRunYetUsesAnalysisPromptWithoutRawIdsOrKeys()
        {
            MvpPlayerLoopSummary summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasRunOutcome = false,
                AnalysisUnlocked = true,
                HasResearchUnlock = true,
                ResearchUnlockId = MvpPlayerLoopSummaryPresenter.BasicRunAnalysisUnlockId,
                ResearchUnlockSummaryKey = "ui.research_unlock.basic_run_analysis.summary",
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey,
                AnalysisAdviceKey = MvpPlayerLoopSummaryPresenter.SuggestBasicAnalysisNoRunKey
            };

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, SmokeLocalize);

            Assert.That(text, Does.Contain("Basic Run Analysis is ready"));
            Assert.That(text, Does.Contain("Basic Run Analysis can read"));
            Assert.That(text, Does.Not.Contain("research.unlock."));
            Assert.That(text, Does.Not.Contain("ui.research_unlock."));
            Assert.That(text, Does.Not.Contain("mvp_loop.suggestion"));
        }

        private static MvpPlayerLoopSummary AnalysisSummary()
        {
            return new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasRunOutcome = true,
                RunSucceeded = true,
                AnalysisUnlocked = true,
                HeatBefore = 4d,
                HeatAfter = 4d,
                LatestRunPlacementEffects = new MvpPlacementEffectsSummary { RuleResolved = true },
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey
            };
        }

        [Test]
        public void BuildPanelText_WithPartyPreview_UsesPlainPartyListWithoutNestedAdventurersLabel()
        {
            MvpPlayerLoopSummary summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasRunOutcome = true,
                RunSucceeded = true,
                AdventurerPartyPreviewResolved = true,
                AdventurerPartyClassIds = new[] { AdventurerPartyCompositionResolver.WarriorClassId, AdventurerPartyCompositionResolver.RogueClassId, AdventurerPartyCompositionResolver.RangerClassId },
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey
            };

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, SmokeLocalize);

            Assert.That(text, Does.Contain("Party: Warrior, Rogue, Ranger"));
            Assert.That(text, Does.Not.Contain("Party: Adventurers:"));
        }

        [Test]
        public void BuildPanelText_WithCasualties_ShowsLocalizedSurvivorAndDeathCounts()
        {
            MvpPlayerLoopSummary summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasRunOutcome = true,
                RunSucceeded = true,
                LatestRunPartySize = 5,
                LatestRunSurvivorCount = 3,
                LatestRunDeathCount = 2,
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestImproveSurvivabilityOrLayoutKey
            };

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, SmokeLocalize);

            Assert.That(text, Does.Contain("Survivors: 3/5; deaths: 2"));
            Assert.That(text, Does.Not.Contain(MvpLoopSummaryPanelPresenter.CasualtyFormatKey));
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
                case MvpLoopSummaryPanelPresenter.ResearchFormatKey:
                case MvpLoopSummaryPanelPresenter.ResearchUnlockFormatKey:
                case MvpLoopSummaryPanelPresenter.SuggestionFormatKey:
                    return "LOC[" + key + "]:{0}";
                case MvpLoopSummaryPanelPresenter.ManaFormatKey:
                    return "LOC[" + key + "]:{0:0.##}";
                case MvpLoopSummaryPanelPresenter.LootFormatKey:
                    return "LOC[" + key + "]:{0}:{1}:{2}";
                case MvpLoopSummaryPanelPresenter.LootNamedFormatKey:
                    return "LOC[" + key + "]:{0}:{1}:{2}:{3}";
                case MvpLoopSummaryPanelPresenter.LootEntryFormatKey:
                    return "LOC[" + key + "]:{0}:{1}";
                case MvpLoopSummaryPanelPresenter.HeatFormatKey:
                    return "LOC[" + key + "]:{0:0.##}:{1:0.##}:{2}:{3}";
                case MvpLoopSummaryPanelPresenter.SectionLineFormatKey:
                case MvpLoopSummaryPanelPresenter.RunOutcomeLineFormatKey:
                    return "LOC[" + key + "]:{0}:{1}";
                case MvpLoopSummaryPanelPresenter.CasualtyFormatKey:
                    return "LOC[" + key + "]:{0}:{1}:{2}";
                case MvpLoopSummaryPanelPresenter.WhyRunFormatKey:
                    return "LOC[" + key + "]:{0}";
                case MvpLoopSummaryPanelPresenter.AnalysisRunFormatKey:
                    return "LOC[" + key + "]:{0}:{1}";
                case MvpLoopSummaryPanelPresenter.InlineSeparatorKey:
                    return "|";
                default:
                    return "LOC[" + key + "]";
            }
        }

        private static string SmokeLocalize(string key, string fallback)
        {
            switch (key)
            {
                case MvpLoopSummaryPanelPresenter.TitleKey: return "MVP Loop Summary";
                case MvpLoopSummaryPanelPresenter.PlacementFormatKey: return "Dungeon composition: {0}";
                case MvpLoopSummaryPanelPresenter.LatestRunSectionKey: return "Latest Run";
                case MvpLoopSummaryPanelPresenter.AdventurerIntentSectionKey: return "Adventurer intent";
                case MvpLoopSummaryPanelPresenter.AdventurerPressureSectionKey: return "Adventurer pressure";
                case MvpLoopSummaryPanelPresenter.LatestRunFormatKey: return "Latest run: {0}";
                case MvpLoopSummaryPanelPresenter.PlacementEffectsFormatKey: return "Effects: {0}";
                case MvpLoopSummaryPanelPresenter.ManaFormatKey: return "Mana reserve: {0:0.##}";
                case MvpLoopSummaryPanelPresenter.LootFormatKey: return "Loot: generated {0}, extracted {1}, tradeable {2}";
                case MvpLoopSummaryPanelPresenter.CasualtyFormatKey: return "Survivors: {0}/{1}; deaths: {2}";
                case MvpLoopSummaryPanelPresenter.HeatFormatKey: return "Heat: {0:0.##} -> {1:0.##} ({2}). {3}";
                case MvpLoopSummaryPanelPresenter.ResearchFormatKey: return "{0}";
                case MvpLoopSummaryPanelPresenter.ResearchUnlockFormatKey: return "Unlocked: {0}";
                case MvpLoopSummaryPanelPresenter.SuggestionFormatKey: return "{0}";
                case MvpLoopSummaryPanelPresenter.ValueNoPlacementKey: return "No dungeon placements yet";
                case MvpLoopSummaryPanelPresenter.ValueNoRunKey: return "No run yet";
                case MvpLoopSummaryPanelPresenter.ValueUnknownKey: return "Unknown";
                case CurrentHeatTierResolver.PeaceTierId: return "Peace";
                case CurrentHeatTierResolver.NoticeTierId: return "Notice";
                case MvpLoopSummaryPanelPresenter.ValueNoResearchKey: return "No research";
                case MvpLoopSummaryPanelPresenter.CurrentDungeonSectionKey: return "Current Dungeon";
                case MvpLoopSummaryPanelPresenter.WhyItHappenedSectionKey: return "Why It Happened";
                case MvpLoopSummaryPanelPresenter.RewardsAndRiskSectionKey: return "Rewards and Risk";
                case MvpLoopSummaryPanelPresenter.ResearchSectionKey: return "Research";
                case MvpLoopSummaryPanelPresenter.SuggestedNextActionSectionKey: return "Suggested Next Action";
                case MvpLoopSummaryPanelPresenter.SectionLineFormatKey: return "{0}: {1}";
                case MvpLoopSummaryPanelPresenter.InlineSeparatorKey: return " | ";
                case MvpLoopSummaryPanelPresenter.RiskNoRunKey: return "Risk will be shown after a run.";
                case MvpLoopSummaryPanelPresenter.RiskStableKey: return "Risk stayed steady.";
                case MvpLoopSummaryPanelPresenter.RiskIncreasedKey: return "Risk increased.";
                case MvpLoopSummaryPanelPresenter.RiskReducedKey: return "Risk went down.";
                case MvpLoopSummaryPanelPresenter.WhyNoRunKey: return "No run yet. Build or review the dungeon, then run it to learn what happens.";
                case MvpLoopSummaryPanelPresenter.WhyRunFormatKey: return "Main reason: {0}.";
                case MvpLoopSummaryPanelPresenter.WhyMixedKey: return "the current placement mix shaped the result";
                case MvpLoopSummaryPanelPresenter.WhyDangerKey: return "danger pressure drove the result";
                case MvpLoopSummaryPanelPresenter.WhyAttractionKey: return "attraction changed adventurer interest";
                case MvpLoopSummaryPanelPresenter.WhyLootBonusKey: return "loot placement improved the reward";
                case MvpLoopSummaryPanelPresenter.WhyHeatPressureKey: return "heat pressure raised the risk";
                case MvpLoopSummaryPanelPresenter.WhyManaPressureKey: return "mana pressure constrained the run";
                case MvpLoopSummaryPanelPresenter.WhyPathCapacityKey: return "path capacity shaped the run";
                case MvpLoopSummaryPanelPresenter.RunOutcomeLineFormatKey: return "{0}. Party: {1}";
                case AdventurerRunIntentPresenter.SummaryFormatKey: return "Adventurer intent: {0} likely. Reason: {1}";
                case AdventurerArrivalPressurePresenter.SummaryFormatKey: return "Adventurer pressure: {0}. Reason: {1}.";
                case AdventurerArrivalPressurePresenter.BodyFormatKey: return "{0}. Reason: {1}.";
                case AdventurerArrivalPressurePresenter.DetailFormatKey: return "Adventurer pressure detail: score {0:0.##}; band {1}; rule source {2}; error {3}; loot {4}; attraction {5}; danger {6}; heat pressure {7}; recent deaths {8}; recovered loot {9}; path complete {10}; latest outcome {11}.";
                case "ui.adventurer_pressure.band.not_yet": return "not yet";
                case "ui.adventurer_pressure.band.low": return "low";
                case "ui.adventurer_pressure.band.cautious_interest": return "cautious interest";
                case "ui.adventurer_pressure.band.building_slowly": return "building slowly";
                case "ui.adventurer_pressure.band.likely_soon": return "likely soon";
                case "ui.adventurer_pressure.outcome.none": return "none yet";
                case "ui.adventurer_pressure.outcome.success": return "success";
                case "ui.adventurer_pressure.outcome.failure": return "failure";
                case AdventurerArrivalPressureResolver.ReasonNotYetKey: return "current dungeon signals are still forming";
                case AdventurerArrivalPressureResolver.ReasonHighLootLowHeatKey: return "high loot signal and low heat";
                case AdventurerArrivalPressureResolver.ReasonModestLootLowAttractionKey: return "modest loot and low attraction";
                case AdventurerArrivalPressureResolver.ReasonDeathsHeatKey: return "recent deaths and rising heat";
                case AdventurerArrivalPressureResolver.ReasonIncompletePathWeakLootKey: return "incomplete path or weak loot signal";
                case AdventurerRunIntentPresenter.BodyFormatKey: return "{0} likely. Reason: {1}";
                case AdventurerRunIntentPresenter.DebugPostureFormatKey: return "Adventurer intent: {0} likely. Debug selected posture: {1}.";
                case AdventurerRunIntentResolver.ReasonFallbackKey: return "current dungeon signals are still forming";
                case AdventurerRunIntentResolver.ReasonLootHighHeatLowKey: return "loot signal is high and heat is low";
                case AdventurerRunIntentResolver.ReasonDeathsHeatKey: return "recent deaths and rising heat";
                case AdventurerRunIntentResolver.ReasonModerateKey: return "risk and reward are both moderate";
                case AdventurerRunIntentResolver.ReasonDangerKey: return "danger pressure is high";
                case "run.posture.cautious.name": return "Cautious";
                case "run.posture.balanced.name": return "Balanced";
                case "run.posture.greedy.name": return "Greedy";
                case "adventurer.class.warrior.display_name": return "Warrior";
                case "adventurer.class.rogue.display_name": return "Rogue";
                case "adventurer.class.ranger.display_name": return "Ranger";
                case "ui.mvp_adventurer_party.class.unknown": return "Unknown adventurer";
                case MvpPlayerLoopSummaryPresenter.ResearchCompletedKey: return "Research completed";
                case "ui.research_unlock.basic_run_analysis.summary": return "Basic run analysis unlocked";
                case MvpLoopSummaryPanelPresenter.AnalysisNoRunKey: return "Basic Run Analysis is ready. Run the dungeon to unlock analysis from the latest outcome.";
                case MvpPlayerLoopSummaryPresenter.SuggestBasicAnalysisNoRunKey: return "Next: run the dungeon so Basic Run Analysis can read the latest outcome.";
                case MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey: return "Run the dungeon to observe the first outcome.";
                default: return fallback;
            }
        }
    }
}
