using System.IO;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpFirstSessionObjectivePresenterTests
    {

        [Test]
        public void BootstrapConfig_FirstSessionLootTarget_MatchesTunedValue()
        {
            string path = Path.Combine(Application.dataPath, "_Project/Data/Bootstrap/run_simulation_config.json");
            RunSimulationConfig config = JsonUtility.FromJson<RunSimulationConfig>(File.ReadAllText(path));

            Assert.That(config.MvpFirstSessionObjective, Is.Not.Null);
            Assert.That(config.MvpFirstSessionObjective.RequiredRecoveredLootValue, Is.GreaterThan(0));
            Assert.That(config.MvpFirstSessionObjective.RequiredRecoveredLootValue, Is.EqualTo(8));
        }

        [Test]
        public void Resolve_MissingObjectiveConfig_IsUnavailableAndIncomplete()
        {
            MvpFirstSessionObjectiveSummary summary = MvpFirstSessionObjectivePresenter.Resolve(new SaveData(), new RunSimulationConfig());

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.IsComplete, Is.False);

            string text = MvpFirstSessionObjectivePresenter.BuildPanelText(summary, Localize);
            Assert.That(text, Does.Contain("Loot recovered: 0 / unavailable"));
            Assert.That(text, Does.Contain("Contract status: Unavailable until objective config is fixed"));
            Assert.That(text, Does.Not.Contain("0 / 0"));
        }

        [Test]
        public void Resolve_ZeroOrInvalidLootTarget_IsUnavailableAndIncomplete()
        {
            RunSimulationConfig zeroTarget = Config();
            zeroTarget.MvpFirstSessionObjective.RequiredRecoveredLootValue = 0;
            RunSimulationConfig negativeTarget = Config();
            negativeTarget.MvpFirstSessionObjective.RequiredRecoveredLootValue = -5;

            MvpFirstSessionObjectiveSummary zero = MvpFirstSessionObjectivePresenter.Resolve(CompleteSaveWithLoot(99), zeroTarget);
            MvpFirstSessionObjectiveSummary negative = MvpFirstSessionObjectivePresenter.Resolve(CompleteSaveWithLoot(99), negativeTarget);

            Assert.That(zero.RuleResolved, Is.False);
            Assert.That(zero.IsComplete, Is.False);
            Assert.That(negative.RuleResolved, Is.False);
            Assert.That(negative.IsComplete, Is.False);
            Assert.That(MvpFirstSessionObjectivePresenter.BuildCompactStatusLine(zero, Localize), Is.EqualTo("First Dungeon Contract: Unavailable until objective config is fixed."));
        }

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
        public void Resolve_CleanSaveAtNoticeBeforeCompletion_RemainsIncomplete()
        {
            SaveData save = new SaveData();
            save.structureRuntime.Heat = 10d;

            MvpFirstSessionObjectiveSummary summary = MvpFirstSessionObjectivePresenter.Resolve(save, Config());

            Assert.That(summary.CurrentHeatTierId, Is.EqualTo(CurrentHeatTierResolver.NoticeTierId));
            Assert.That(summary.CompletionRecorded, Is.False);
            Assert.That(summary.IsComplete, Is.False);
        }

        [Test]
        public void ApplyIfComplete_RecordsStableObjectiveId_WhenRequirementsAreMet()
        {
            SaveData save = CompleteSaveWithLoot(10);

            bool applied = MvpFirstSessionObjectiveCompletionApplier.ApplyIfComplete(save, Config());

            Assert.That(applied, Is.True);
            Assert.That(save.completedObjectives, Is.Not.Null);
            Assert.That(save.completedObjectives.ObjectiveIds, Does.Contain("objective.first_dungeon_contract"));
            Assert.That(save.completedObjectives.LastCompletedObjectiveId, Is.EqualTo("objective.first_dungeon_contract"));
        }

        [Test]
        public void Resolve_RecordedCompletion_DoesNotRegressWhenHeatLaterRises()
        {
            SaveData save = CompleteSaveWithLoot(10);
            Assert.That(MvpFirstSessionObjectiveCompletionApplier.ApplyIfComplete(save, Config()), Is.True);
            save.structureRuntime.Heat = 10d;

            MvpFirstSessionObjectiveSummary summary = MvpFirstSessionObjectivePresenter.Resolve(save, Config());
            string text = MvpFirstSessionObjectivePresenter.BuildPanelText(summary, Localize);

            Assert.That(summary.CompletionRecorded, Is.True);
            Assert.That(summary.CurrentHeatTierId, Is.EqualTo(CurrentHeatTierResolver.NoticeTierId));
            Assert.That(summary.HeatTargetComplete, Is.False);
            Assert.That(summary.IsComplete, Is.True);
            Assert.That(text, Does.Contain("Heat target: Peace (current: Notice)"));
            Assert.That(text, Does.Contain("Completion: First contract complete."));
            Assert.That(text, Does.Contain("Next objective: Test a greedier reward setup while keeping heat under control."));
        }

        [Test]
        public void Resolve_RecordedCompletion_SurvivesSaveLoad()
        {
            SaveData save = CompleteSaveWithLoot(10);
            Assert.That(MvpFirstSessionObjectiveCompletionApplier.ApplyIfComplete(save, Config()), Is.True);

            SaveData loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));
            MvpFirstSessionObjectiveSummary summary = MvpFirstSessionObjectivePresenter.Resolve(loaded, Config());

            Assert.That(summary.CompletionRecorded, Is.True);
            Assert.That(summary.IsComplete, Is.True);
        }

        [Test]
        public void GreedTrialAndPrimaryAction_DoNotRegressToFirstContractAfterRecordedCompletionHeatRise()
        {
            SaveData save = CompleteSaveWithLoot(10);
            save.mvpDungeonFloorLayout.Nodes[3].CategoryId = MvpDungeonPlacementIds.LootNodeCategoryId;
            save.mvpDungeonFloorLayout.Nodes[3].OptionId = MvpDungeonPlacementIds.GlitteringHoardOptionId;
            Assert.That(MvpFirstSessionObjectiveCompletionApplier.ApplyIfComplete(save, Config()), Is.True);
            save.structureRuntime.Heat = 10d;

            MvpFirstSessionObjectiveSummary first = MvpFirstSessionObjectivePresenter.Resolve(save, Config());
            MvpPostContractGreedTrialSummary greed = MvpPostContractGreedTrialPresenter.Resolve(save, Config(), first);
            MvpPrimaryNextActionSummary action = MvpPrimaryNextActionPresenter.Resolve(new MvpPlayerLoopSummary { RuleResolved = true }, null, first, greed);

            Assert.That(first.IsComplete, Is.True);
            Assert.That(greed.IsActive, Is.True);
            Assert.That(action.ResolvedRule, Is.Not.EqualTo(MvpPrimaryNextActionPresenter.RuleFirstContractIncomplete));
        }

        [Test]
        public void BuildPanelText_CleanSaveShowsConfiguredTarget()
        {
            MvpFirstSessionObjectiveSummary summary = MvpFirstSessionObjectivePresenter.Resolve(new SaveData(), Config());

            string text = MvpFirstSessionObjectivePresenter.BuildPanelText(summary, Localize);

            Assert.That(text, Does.Contain("Loot recovered: 0 / 10"));
            Assert.That(text, Does.Not.Contain("0 / 0"));
        }

        [Test]
        public void Resolve_LootBelowTarget_RemainsIncomplete()
        {
            MvpFirstSessionObjectiveSummary summary = MvpFirstSessionObjectivePresenter.Resolve(CompleteSaveWithLoot(9), Config());

            Assert.That(summary.RecoveredLootValue, Is.EqualTo(9));
            Assert.That(summary.LootRecoveredComplete, Is.False);
            Assert.That(summary.IsComplete, Is.False);
        }

        [Test]
        public void Resolve_CompletionRequiresConfiguredLootTargetMet()
        {
            MvpFirstSessionObjectiveSummary below = MvpFirstSessionObjectivePresenter.Resolve(CompleteSaveWithLoot(9), Config());
            MvpFirstSessionObjectiveSummary met = MvpFirstSessionObjectivePresenter.Resolve(CompleteSaveWithLoot(10), Config());

            Assert.That(below.IsComplete, Is.False);
            Assert.That(met.LootRecoveredComplete, Is.True);
            Assert.That(met.IsComplete, Is.True);
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
            Assert.That(text, Does.Contain("Analysis: Adventurer Activity Analysis unlocked"));
            Assert.That(text, Does.Not.Contain("research.unlock"));
            Assert.That(text, Does.Not.Contain("ui.mvp_first_contract"));
        }



        [Test]
        public void BuildPanelText_IncompleteContract_DoesNotShowCompletionPayoffOrNextObjective()
        {
            MvpFirstSessionObjectiveSummary summary = MvpFirstSessionObjectivePresenter.Resolve(new SaveData(), Config());

            string text = MvpFirstSessionObjectivePresenter.BuildPanelText(summary, Localize);

            Assert.That(text, Does.Contain("Contract status: In progress"));
            Assert.That(text, Does.Not.Contain("Completion:"));
            Assert.That(text, Does.Not.Contain("Next objective:"));
            Assert.That(text, Does.Not.Contain("First contract complete."));
            Assert.That(text, Does.Not.Contain("greedier reward setup"));
        }

        [Test]
        public void BuildPanelText_CompleteContract_ShowsCompletionPayoffAndNextObjective()
        {
            MvpFirstSessionObjectiveSummary summary = MvpFirstSessionObjectivePresenter.Resolve(CompleteSaveWithLoot(10), Config());

            string text = MvpFirstSessionObjectivePresenter.BuildPanelText(summary, Localize);

            Assert.That(text, Does.Contain("Contract status: Complete. Try a riskier setup or improve loot recovery."));
            Assert.That(text, Does.Contain("Completion: First contract complete. Your dungeon can attract adventurers, recover loot, control heat, and use analysis."));
            Assert.That(text, Does.Contain("Next objective: Test a greedier reward setup while keeping heat under control."));
            Assert.That(text, Does.Not.Contain("ui.mvp_first_contract"));
        }

        [Test]
        public void BuildPanelText_NullSummary_RemainsSafeWithoutCompletionPayoffOrRawKeys()
        {
            string text = MvpFirstSessionObjectivePresenter.BuildPanelText(null, Localize);

            Assert.That(text, Does.Contain("Loot recovered: 0 / unavailable"));
            Assert.That(text, Does.Contain("Contract status: Unavailable until objective config is fixed"));
            Assert.That(text, Does.Not.Contain("Completion:"));
            Assert.That(text, Does.Not.Contain("Next objective:"));
            Assert.That(text, Does.Not.Contain("ui.mvp_first_contract"));
        }

        [Test]
        public void BootstrapStringTable_ResolvesCompletionPayoffKeys()
        {
            string path = Path.Combine(Application.dataPath, "_Project/Data/Bootstrap/string_table_en.json");
            string json = File.ReadAllText(path);

            Assert.That(json, Does.Contain(MvpFirstSessionObjectivePresenter.CompletionFormatKey));
            Assert.That(json, Does.Contain(MvpFirstSessionObjectivePresenter.CompletionFirstContractCompleteKey));
            Assert.That(json, Does.Contain(MvpFirstSessionObjectivePresenter.NextObjectiveFormatKey));
            Assert.That(json, Does.Contain(MvpFirstSessionObjectivePresenter.NextObjectiveGreedierRewardSetupKey));
            Assert.That(json, Does.Contain("Completion: {0}"));
            Assert.That(json, Does.Contain("First contract complete. Your dungeon can attract adventurers, recover loot, control heat, and use analysis."));
            Assert.That(json, Does.Contain("Next objective: {0}"));
            Assert.That(json, Does.Contain("Test a greedier reward setup while keeping heat under control."));
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

        private static SaveData CompleteSaveWithLoot(int loot)
        {
            SaveData save = SaveWithPlacements(MvpDungeonPlacementIds.OrderedCategoryIds.Length);
            save.runHistory = new RunHistoryState { RecentOutcomes = new[] { RunWithLoot(loot) } };
            save.completedResearch = new CompletedResearchState { ProjectIds = new[] { "research.project.basic_analysis" } };
            return save;
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
                case MvpFirstSessionObjectivePresenter.RunObservedFormatKey: return "Visit observed: {0}";
                case MvpFirstSessionObjectivePresenter.LootRecoveredFormatKey: return "Loot recovered: {0} / {1}";
                case MvpFirstSessionObjectivePresenter.HeatTargetFormatKey: return "Heat target: {0} (current: {1})";
                case MvpFirstSessionObjectivePresenter.AnalysisFormatKey: return "Analysis: {0}";
                case MvpFirstSessionObjectivePresenter.StatusFormatKey: return "Contract status: {0}";
                case MvpFirstSessionObjectivePresenter.CompleteKey: return "complete";
                case MvpFirstSessionObjectivePresenter.IncompleteKey: return "incomplete";
                case MvpFirstSessionObjectivePresenter.AnalysisUnlockedKey: return "Adventurer Activity Analysis unlocked";
                case MvpFirstSessionObjectivePresenter.AnalysisLockedKey: return "unlock Adventurer Activity Analysis";
                case MvpFirstSessionObjectivePresenter.StatusInProgressKey: return "In progress";
                case MvpFirstSessionObjectivePresenter.StatusCompleteKey: return "Complete. Try a riskier setup or improve loot recovery.";
                case MvpFirstSessionObjectivePresenter.StatusUnavailableKey: return "Unavailable until objective config is fixed";
                case MvpFirstSessionObjectivePresenter.ValueUnavailableKey: return "unavailable";
                case MvpFirstSessionObjectivePresenter.CompactInProgressFormatKey: return "{0}: {1}. Loot {2} / {3}, {4}.";
                case MvpFirstSessionObjectivePresenter.CompactUnavailableFormatKey: return "{0}: {1}.";
                case MvpFirstSessionObjectivePresenter.CompactCompleteFormatKey: return "{0}: {1}";
                case MvpFirstSessionObjectivePresenter.CompactPathCompleteKey: return "path complete";
                case MvpFirstSessionObjectivePresenter.CompactPathIncompleteKey: return "path incomplete";
                case MvpFirstSessionObjectivePresenter.CompletionFormatKey: return "Completion: {0}";
                case MvpFirstSessionObjectivePresenter.CompletionFirstContractCompleteKey: return "First contract complete. Your dungeon can attract adventurers, recover loot, control heat, and use analysis.";
                case MvpFirstSessionObjectivePresenter.NextObjectiveFormatKey: return "Next objective: {0}";
                case MvpFirstSessionObjectivePresenter.NextObjectiveGreedierRewardSetupKey: return "Test a greedier reward setup while keeping heat under control.";
                case CurrentHeatTierResolver.PeaceTierId: return "Peace";
                case CurrentHeatTierResolver.NoticeTierId: return "Notice";
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
