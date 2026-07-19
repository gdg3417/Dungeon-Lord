#if UNITY_EDITOR
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests
{
    public class BasicRunAnalysisSelectedPlacementFitPresenterTests
    {
        [Test]
        public void ResolveFitKey_MonsterTrapTargetMatchesMonster()
        {
            Assert.That(Resolve(BasicRunAnalysisPlacementTargetPresenter.ReduceDangerTargetKey, MvpDungeonPlacementIds.MonsterCategoryId), Is.EqualTo(BasicRunAnalysisSelectedPlacementFitPresenter.MatchKey));
        }

        [Test]
        public void ResolveFitKey_MonsterTrapTargetMatchesTrap()
        {
            Assert.That(Resolve(BasicRunAnalysisPlacementTargetPresenter.ReduceDangerTargetKey, MvpDungeonPlacementIds.TrapCategoryId), Is.EqualTo(BasicRunAnalysisSelectedPlacementFitPresenter.MatchKey));
        }

        [Test]
        public void ResolveFitKey_MonsterTrapTargetMismatchesLootNode()
        {
            Assert.That(Resolve(BasicRunAnalysisPlacementTargetPresenter.ReduceDangerTargetKey, MvpDungeonPlacementIds.LootNodeCategoryId), Is.EqualTo(BasicRunAnalysisSelectedPlacementFitPresenter.MismatchKey));
        }

        [Test]
        public void ResolveFitKey_TrapLootTargetMatchesLootNode()
        {
            Assert.That(Resolve(BasicRunAnalysisPlacementTargetPresenter.ReduceHeatTargetKey, MvpDungeonPlacementIds.LootNodeCategoryId), Is.EqualTo(BasicRunAnalysisSelectedPlacementFitPresenter.MatchKey));
        }

        [Test]
        public void ResolveFitKey_RoomMonsterTargetMatchesRoom()
        {
            Assert.That(Resolve(BasicRunAnalysisPlacementTargetPresenter.ImproveExtractionTargetKey, MvpDungeonPlacementIds.RoomCategoryId), Is.EqualTo(BasicRunAnalysisSelectedPlacementFitPresenter.MatchKey));
        }

        [Test]
        public void ResolveFitKey_LootNodeTargetMatchesLootNode()
        {
            Assert.That(Resolve(BasicRunAnalysisPlacementTargetPresenter.TestGreedierTargetKey, MvpDungeonPlacementIds.LootNodeCategoryId), Is.EqualTo(BasicRunAnalysisSelectedPlacementFitPresenter.MatchKey));
        }

        [Test]
        public void ResolveFitKey_FallbackTargetReportsBroadFit()
        {
            Assert.That(Resolve(BasicRunAnalysisPlacementTargetPresenter.FallbackTargetKey, MvpDungeonPlacementIds.RoomCategoryId), Is.EqualTo(BasicRunAnalysisSelectedPlacementFitPresenter.BroadKey));
        }

        [Test]
        public void ResolveFitKey_AnalysisLockedReturnsSafeEmptyFit()
        {
            MvpPlayerLoopSummary summary = Summary();
            summary.AnalysisUnlocked = false;

            Assert.That(BasicRunAnalysisSelectedPlacementFitPresenter.ResolveFitKey(summary, BasicRunAnalysisPlacementTargetPresenter.ReduceDangerTargetKey, MvpDungeonPlacementIds.MonsterCategoryId), Is.EqualTo(string.Empty));
            Assert.That(BasicRunAnalysisSelectedPlacementFitPresenter.BuildFitLine(summary, BasicRunAnalysisPlacementTargetPresenter.ReduceDangerTargetKey, MvpDungeonPlacementIds.MonsterCategoryId, Localize), Is.EqualTo(string.Empty));
        }

        [Test]
        public void ResolveFitKey_NoRunOutcomeReportsNotReady()
        {
            MvpPlayerLoopSummary summary = Summary();
            summary.HasRunOutcome = false;

            Assert.That(BasicRunAnalysisSelectedPlacementFitPresenter.ResolveFitKey(summary, BasicRunAnalysisPlacementTargetPresenter.ReduceDangerTargetKey, MvpDungeonPlacementIds.MonsterCategoryId), Is.EqualTo(BasicRunAnalysisSelectedPlacementFitPresenter.NotReadyKey));
        }

        [Test]
        public void ResolveFitKey_IsDeterministicAndDoesNotMutateSummary()
        {
            MvpPlayerLoopSummary summary = Summary();
            string before = JsonUtility.ToJson(summary);

            string first = BasicRunAnalysisSelectedPlacementFitPresenter.ResolveFitKey(summary, BasicRunAnalysisPlacementTargetPresenter.ReduceDangerTargetKey, MvpDungeonPlacementIds.LootNodeCategoryId);
            string second = BasicRunAnalysisSelectedPlacementFitPresenter.ResolveFitKey(summary, BasicRunAnalysisPlacementTargetPresenter.ReduceDangerTargetKey, MvpDungeonPlacementIds.LootNodeCategoryId);

            Assert.That(first, Is.EqualTo(BasicRunAnalysisSelectedPlacementFitPresenter.MismatchKey));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(JsonUtility.ToJson(summary), Is.EqualTo(before));
        }

        [Test]
        public void BuildFitLine_UsesLocalizedFormattingForMismatch()
        {
            string line = BasicRunAnalysisSelectedPlacementFitPresenter.BuildFitLine(Summary(), BasicRunAnalysisPlacementTargetPresenter.ReduceDangerTargetKey, MvpDungeonPlacementIds.LootNodeCategoryId, Localize);

            Assert.That(line, Is.EqualTo("Selected placement fit: Current selection is LOC[" + BasicRunAnalysisSelectedPlacementFitPresenter.LootNodeCategoryKey + "], but analysis recommends LOC[" + BasicRunAnalysisSelectedPlacementFitPresenter.MonsterOrTrapTargetLabelKey + "] first. Switch category before the next adventurer visit."));
        }

        private static string Resolve(string targetKey, string selectedCategoryId)
        {
            return BasicRunAnalysisSelectedPlacementFitPresenter.ResolveFitKey(Summary(), targetKey, selectedCategoryId);
        }

        private static MvpPlayerLoopSummary Summary()
        {
            return new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                AnalysisUnlocked = true,
                HasRunOutcome = true
            };
        }

        private static string Localize(string key, string fallback)
        {
            switch (key)
            {
                case BasicRunAnalysisSelectedPlacementFitPresenter.SelectedFitFormatKey:
                    return "Selected placement fit: {0}";
                case BasicRunAnalysisSelectedPlacementFitPresenter.MismatchKey:
                    return "Current selection is {0}, but analysis recommends {1} first. Switch category before the next adventurer visit.";
                default:
                    return "LOC[" + key + "]";
            }
        }
    }
}
#endif
