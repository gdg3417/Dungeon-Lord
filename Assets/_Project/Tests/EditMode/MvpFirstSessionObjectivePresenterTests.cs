using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpFirstSessionObjectivePresenterTests
    {
        [Test]
        public void Resolve_CleanSave_ShowsObjectiveFirstStepsWithoutMutation()
        {
            MvpFirstSessionObjectiveSummary summary = MvpFirstSessionObjectivePresenter.Resolve(new SaveData(), Config());

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.PathComplete, Is.False);
            Assert.That(summary.RunObservedComplete, Is.False);
            Assert.That(summary.RecoveredLootValue, Is.EqualTo(0));
            Assert.That(summary.HeatTargetComplete, Is.True);
            Assert.That(summary.AnalysisComplete, Is.False);
            Assert.That(summary.IsComplete, Is.False);
            AssertReadOnly(summary);
        }

        [Test]
        public void Resolve_PartialAndCompletePath_UpdatePathProgress()
        {
            MvpFirstSessionObjectiveSummary partial = MvpFirstSessionObjectivePresenter.Resolve(SaveWithPlacements(1), Config());
            MvpFirstSessionObjectiveSummary complete = MvpFirstSessionObjectivePresenter.Resolve(SaveWithPlacements(MvpDungeonPlacementIds.OrderedCategoryIds.Length), Config());

            Assert.That(partial.CurrentPathPlacementCount, Is.EqualTo(1));
            Assert.That(partial.PathComplete, Is.False);
            Assert.That(complete.PathComplete, Is.True);
        }

        [Test]
        public void Resolve_RunObservedAndLootProgress_SumsRecoveredLootAcrossRuns()
        {
            SaveData save = SaveWithPlacements(MvpDungeonPlacementIds.OrderedCategoryIds.Length);
            save.runHistory = new RunHistoryState
            {
                RecentOutcomes = new[] { RunWithLoot(7), RunWithLoot(3) }
            };

            MvpFirstSessionObjectiveSummary summary = MvpFirstSessionObjectivePresenter.Resolve(save, Config());

            Assert.That(summary.RunCount, Is.EqualTo(2));
            Assert.That(summary.RunObservedComplete, Is.True);
            Assert.That(summary.RecoveredLootValue, Is.EqualTo(10));
            Assert.That(summary.LootRecoveredComplete, Is.True);
        }

        [Test]
        public void Resolve_HeatTargetReflectsCurrentTier()
        {
            SaveData save = new SaveData();
            save.structureRuntime.Heat = 10d;

            MvpFirstSessionObjectiveSummary summary = MvpFirstSessionObjectivePresenter.Resolve(save, Config());

            Assert.That(summary.CurrentHeatTierId, Is.EqualTo(CurrentHeatTierResolver.NoticeTierId));
            Assert.That(summary.HeatTargetComplete, Is.False);
        }

        [Test]
        public void Resolve_ResearchAnalysisUnlockedAndCompleteObjective_AreDeterministic()
        {
            SaveData save = SaveWithPlacements(MvpDungeonPlacementIds.OrderedCategoryIds.Length);
            save.runHistory = new RunHistoryState { RecentOutcomes = new[] { RunWithLoot(10) } };
            save.completedResearch = new CompletedResearchState { ProjectIds = new[] { "research.project.basic_analysis" } };

            string before = UnityEngine.JsonUtility.ToJson(save);
            MvpFirstSessionObjectiveSummary first = MvpFirstSessionObjectivePresenter.Resolve(save, Config());
            MvpFirstSessionObjectiveSummary second = MvpFirstSessionObjectivePresenter.Resolve(save, Config());
            string after = UnityEngine.JsonUtility.ToJson(save);

            Assert.That(first.AnalysisUnlocked, Is.True);
            Assert.That(first.IsComplete, Is.True);
            Assert.That(UnityEngine.JsonUtility.ToJson(first), Is.EqualTo(UnityEngine.JsonUtility.ToJson(second)));
            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void BuildPanelText_UsesLocalizedObjectiveOutputWithoutRawIds()
        {
            var summary = new MvpFirstSessionObjectiveSummary
            {
                RuleResolved = true,
                PathComplete = true,
                RunObservedComplete = true,
                RecoveredLootValue = 7,
                RequiredRecoveredLootValue = 10,
                AllowedMaxHeatTierId = CurrentHeatTierResolver.PeaceTierId,
                CurrentHeatTierId = CurrentHeatTierResolver.PeaceTierId,
                AnalysisComplete = true,
                IsComplete = false
            };

            string text = MvpFirstSessionObjectivePresenter.BuildPanelText(summary, Localize);

            Assert.That(text, Does.Contain("First Dungeon Contract"));
            Assert.That(text, Does.Contain("Path built: complete"));
            Assert.That(text, Does.Contain("Loot recovered: 7 / 10"));
            Assert.That(text, Does.Contain("Analysis: Basic Run Analysis unlocked"));
            Assert.That(text, Does.Not.Contain("research.unlock"));
            Assert.That(text, Does.Not.Contain("ui.mvp_first_contract"));
        }


        [Test]
        public void BuildCompactStatusLine_UsesSingleLocalizedVisibleLine()
        {
            var summary = new MvpFirstSessionObjectiveSummary
            {
                RuleResolved = true,
                PathComplete = false,
                RecoveredLootValue = 0,
                RequiredRecoveredLootValue = 10,
                IsComplete = false
            };

            string text = MvpFirstSessionObjectivePresenter.BuildCompactStatusLine(summary, Localize);

            Assert.That(text, Is.EqualTo("First Dungeon Contract: In progress. Loot 0 / 10, path incomplete."));
            Assert.That(text.Split('\n').Length, Is.EqualTo(1));
            Assert.That(text, Does.Not.Contain("Path built:"));
            Assert.That(text, Does.Not.Contain("ui.mvp_first_contract"));
        }

        private static SaveData SaveWithPlacements(int count)
        {
            var save = new SaveData();
            for (int i = 0; i < count; i++)
            {
                string category = MvpDungeonPlacementIds.OrderedCategoryIds[i];
                save.mvpDungeonFloorLayout.Nodes[i].CategoryId = category;
                save.mvpDungeonFloorLayout.Nodes[i].OptionId = OptionForCategory(category);
                save.mvpDungeonFloorLayout.Nodes[i].Revision = i + 1;
            }
            return save;
        }

        private static string OptionForCategory(string category)
        {
            if (category == MvpDungeonPlacementIds.RoomCategoryId) return MvpDungeonPlacementIds.BasicRoomOptionId;
            if (category == MvpDungeonPlacementIds.MonsterCategoryId) return MvpDungeonPlacementIds.SkeletonOptionId;
            if (category == MvpDungeonPlacementIds.TrapCategoryId) return MvpDungeonPlacementIds.SpikeTrapOptionId;
            return MvpDungeonPlacementIds.BasicLootNodeOptionId;
        }

        private static RunOutcomeRecord RunWithLoot(int loot) => new RunOutcomeRecord { LootExtractionSummary = new RunLootExtractionSummary { TotalExtractedWorldValue = loot } };

        private static RunSimulationConfig Config() => new RunSimulationConfig
        {
            HeatPeaceMinimum = 0d,
            HeatPeaceMaximum = 9d,
            HeatNoticeMinimum = 10d,
            HeatNoticeMaximum = 24d,
            HeatConcernMinimum = 25d,
            HeatConcernMaximum = 49d,
            RunHeatApplicationRuleSourceId = "test.heat",
            MvpFirstSessionObjective = new MvpFirstSessionObjectiveConfig
            {
                ObjectiveId = "objective.first_dungeon_contract",
                RequiredCompletePath = true,
                RequiredRunCount = 1,
                RequiredRecoveredLootValue = 10,
                AllowedMaxHeatTierId = CurrentHeatTierResolver.PeaceTierId,
                RequireResearchAnalysisUnlocked = true,
                AnalysisResearchProjectId = "research.project.basic_analysis"
            }
        };

        private static string Localize(string key, string fallback)
        {
            switch (key)
            {
                case MvpFirstSessionObjectivePresenter.TitleKey: return "First Dungeon Contract";
                case MvpFirstSessionObjectivePresenter.PathBuiltFormatKey: return "Path built: {0}";
                case MvpFirstSessionObjectivePresenter.RunObservedFormatKey: return "Run observed: {0}";
                case MvpFirstSessionObjectivePresenter.LootRecoveredFormatKey: return "Loot recovered: {0} / {1}";
                case MvpFirstSessionObjectivePresenter.HeatTargetFormatKey: return "Heat target: {0} (current: {1})";
                case MvpFirstSessionObjectivePresenter.AnalysisFormatKey: return "Analysis: {0}";
                case MvpFirstSessionObjectivePresenter.StatusFormatKey: return "Contract status: {0}";
                case MvpFirstSessionObjectivePresenter.CompleteKey: return "complete";
                case MvpFirstSessionObjectivePresenter.IncompleteKey: return "incomplete";
                case MvpFirstSessionObjectivePresenter.AnalysisUnlockedKey: return "Basic Run Analysis unlocked";
                case MvpFirstSessionObjectivePresenter.AnalysisLockedKey: return "unlock Basic Run Analysis";
                case MvpFirstSessionObjectivePresenter.StatusInProgressKey: return "In progress";
                case MvpFirstSessionObjectivePresenter.StatusCompleteKey: return "Complete. Try a riskier setup or improve loot recovery.";
                case MvpFirstSessionObjectivePresenter.CompactInProgressFormatKey: return "{0}: {1}. Loot {2} / {3}, {4}.";
                case MvpFirstSessionObjectivePresenter.CompactCompleteFormatKey: return "{0}: {1}";
                case MvpFirstSessionObjectivePresenter.CompactPathCompleteKey: return "path complete";
                case MvpFirstSessionObjectivePresenter.CompactPathIncompleteKey: return "path incomplete";
                case CurrentHeatTierResolver.PeaceTierId: return "Peace";
                default: return fallback;
            }
        }

        private static void AssertReadOnly(MvpFirstSessionObjectiveSummary summary)
        {
            Assert.That(summary.WouldMutateState, Is.False);
            Assert.That(summary.WouldGrantRewards, Is.False);
            Assert.That(summary.WouldUnlockContent, Is.False);
            Assert.That(summary.WouldCallServer, Is.False);
        }
    }
}
