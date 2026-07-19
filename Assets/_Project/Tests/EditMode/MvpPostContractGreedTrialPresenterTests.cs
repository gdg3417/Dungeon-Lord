#if UNITY_EDITOR
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpPostContractGreedTrialPresenterTests
    {
        [Test]
        public void Resolve_IncompleteFirstContract_IsInactive()
        {
            MvpPostContractGreedTrialSummary summary = MvpPostContractGreedTrialPresenter.Resolve(StarterSave(), Config(), new MvpFirstSessionObjectiveSummary { RuleResolved = true, IsComplete = false });

            Assert.That(summary.IsActive, Is.False);
            Assert.That(MvpPostContractGreedTrialPresenter.BuildPanelText(summary, Localize), Is.Empty);
            AssertReadOnly(summary);
        }

        [Test]
        public void Resolve_CompleteFirstContractWithoutGreed_IsInProgress()
        {
            MvpPostContractGreedTrialSummary summary = MvpPostContractGreedTrialPresenter.Resolve(StarterSave(), Config(), CompleteFirstContract());

            Assert.That(summary.IsActive, Is.True);
            Assert.That(summary.GreedSetupTestedComplete, Is.False);
            Assert.That(summary.HeatStabilizedComplete, Is.True);
            Assert.That(summary.RiskResponseComplete, Is.False);
            Assert.That(summary.IsComplete, Is.False);
            Assert.That(summary.NextActionKey, Is.EqualTo(MvpPostContractGreedTrialPresenter.NextActionTestGreedierSetupKey));
        }

        [Test]
        public void Resolve_GlitteringHoard_CompletesGreedSetup()
        {
            MvpPostContractGreedTrialSummary summary = MvpPostContractGreedTrialPresenter.Resolve(SaveWith(MvpDungeonPlacementIds.SpikeTrapOptionId, MvpDungeonPlacementIds.GlitteringHoardOptionId, 10d), Config(), CompleteFirstContract());

            Assert.That(summary.GreedSetupTestedComplete, Is.True);
            Assert.That(summary.HeatStabilizedComplete, Is.False);
            Assert.That(summary.IsComplete, Is.False);
        }

        [Test]
        public void Resolve_PeaceHeat_CompletesHeatStabilized()
        {
            MvpPostContractGreedTrialSummary summary = MvpPostContractGreedTrialPresenter.Resolve(SaveWith(MvpDungeonPlacementIds.SpikeTrapOptionId, MvpDungeonPlacementIds.BasicLootNodeOptionId, 0d), Config(), CompleteFirstContract());

            Assert.That(summary.HeatStabilizedComplete, Is.True);
        }

        [Test]
        public void Resolve_GlitteringHoardWithChillingSigil_CompletesRiskResponse()
        {
            MvpPostContractGreedTrialSummary summary = MvpPostContractGreedTrialPresenter.Resolve(SaveWith(MvpDungeonPlacementIds.ChillingSigilOptionId, MvpDungeonPlacementIds.GlitteringHoardOptionId, 0d), Config(), CompleteFirstContract());

            Assert.That(summary.GreedSetupTestedComplete, Is.True);
            Assert.That(summary.PlacementEffects.HeatPressure, Is.LessThanOrEqualTo(0));
            Assert.That(summary.RiskResponseComplete, Is.True);
        }

        [Test]
        public void Resolve_CompleteOnlyWhenAllChecksComplete()
        {
            MvpPostContractGreedTrialSummary notice = MvpPostContractGreedTrialPresenter.Resolve(SaveWith(MvpDungeonPlacementIds.ChillingSigilOptionId, MvpDungeonPlacementIds.GlitteringHoardOptionId, 10d), Config(), CompleteFirstContract());
            MvpPostContractGreedTrialSummary peace = MvpPostContractGreedTrialPresenter.Resolve(SaveWith(MvpDungeonPlacementIds.ChillingSigilOptionId, MvpDungeonPlacementIds.GlitteringHoardOptionId, 0d), Config(), CompleteFirstContract());

            Assert.That(notice.IsComplete, Is.False);
            Assert.That(notice.NextActionKey, Is.EqualTo(MvpPostContractGreedTrialPresenter.NextActionStabilizeHeatKey));
            Assert.That(peace.IsComplete, Is.True);
            Assert.That(MvpPostContractGreedTrialPresenter.BuildPanelText(peace, Localize), Does.Contain("Trial status: Complete. Greed pressure tested and stabilized."));
        }

        private static SaveData StarterSave() => SaveWith(MvpDungeonPlacementIds.SpikeTrapOptionId, MvpDungeonPlacementIds.BasicLootNodeOptionId, 0d);

        private static SaveData SaveWith(string trap, string loot, double heat)
        {
            var save = new SaveData();
            save.structureRuntime.Heat = heat;
            save.mvpDungeonFloorLayout.Nodes[0].CategoryId = MvpDungeonPlacementIds.RoomCategoryId;
            save.mvpDungeonFloorLayout.Nodes[0].OptionId = MvpDungeonPlacementIds.BasicRoomOptionId;
            save.mvpDungeonFloorLayout.Nodes[1].CategoryId = MvpDungeonPlacementIds.MonsterCategoryId;
            save.mvpDungeonFloorLayout.Nodes[1].OptionId = MvpDungeonPlacementIds.SkeletonOptionId;
            save.mvpDungeonFloorLayout.Nodes[2].CategoryId = MvpDungeonPlacementIds.TrapCategoryId;
            save.mvpDungeonFloorLayout.Nodes[2].OptionId = trap;
            save.mvpDungeonFloorLayout.Nodes[3].CategoryId = MvpDungeonPlacementIds.LootNodeCategoryId;
            save.mvpDungeonFloorLayout.Nodes[3].OptionId = loot;
            return save;
        }

        private static MvpFirstSessionObjectiveSummary CompleteFirstContract() => new MvpFirstSessionObjectiveSummary { RuleResolved = true, IsComplete = true };

        private static RunSimulationConfig Config() => new RunSimulationConfig
        {
            HeatPeaceMinimum = 0d,
            HeatPeaceMaximum = 9d,
            HeatNoticeMinimum = 10d,
            HeatNoticeMaximum = 24d,
            HeatConcernMinimum = 25d,
            HeatConcernMaximum = 49d,
            RunHeatApplicationRuleSourceId = "test.heat_application.rule",
            MvpPlacementEffects = new[]
            {
                new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.RoomCategoryId, OptionId = MvpDungeonPlacementIds.BasicRoomOptionId, PathCapacity = 2 },
                new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.MonsterCategoryId, OptionId = MvpDungeonPlacementIds.SkeletonOptionId, Danger = 3, ManaPressure = 2 },
                new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.TrapCategoryId, OptionId = MvpDungeonPlacementIds.SpikeTrapOptionId, Danger = 2, HeatPressure = 1 },
                new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.TrapCategoryId, OptionId = MvpDungeonPlacementIds.ChillingSigilOptionId, HeatPressure = -1 },
                new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.LootNodeCategoryId, OptionId = MvpDungeonPlacementIds.BasicLootNodeOptionId, LootBonus = 4, Attraction = 2 },
                new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.LootNodeCategoryId, OptionId = MvpDungeonPlacementIds.GlitteringHoardOptionId, LootBonus = 6, Attraction = 4, HeatPressure = 1 }
            }
        };

        private static string Localize(string key, string fallback)
        {
            switch (key)
            {
                case MvpPostContractGreedTrialPresenter.TitleKey: return "Post-Contract Greed Trial";
                case MvpPostContractGreedTrialPresenter.GreedSetupFormatKey: return "Greed setup tested: {0}";
                case MvpPostContractGreedTrialPresenter.HeatStabilizedFormatKey: return "Heat stabilized: {0}";
                case MvpPostContractGreedTrialPresenter.RiskResponseFormatKey: return "Risk response: {0}";
                case MvpPostContractGreedTrialPresenter.StatusFormatKey: return "Trial status: {0}";
                case MvpPostContractGreedTrialPresenter.StatusInProgressKey: return "In progress";
                case MvpPostContractGreedTrialPresenter.StatusCompleteKey: return "Complete. Greed pressure tested and stabilized.";
                case MvpPostContractGreedTrialPresenter.ValueCompleteKey: return "complete";
                case MvpPostContractGreedTrialPresenter.ValueIncompleteKey: return "incomplete";
                case MvpPostContractGreedTrialPresenter.NextActionFormatKey: return "Next action: {0}";
                case MvpPostContractGreedTrialPresenter.NextActionTestGreedierSetupKey: return "Test a greedier loot setup.";
                case MvpPostContractGreedTrialPresenter.NextActionStabilizeHeatKey: return "Stabilize heat back to Peace while keeping the greed setup.";
                case MvpPostContractGreedTrialPresenter.NextActionAddCounterplayKey: return "Add heat-control counterplay while keeping the greed setup.";
                case MvpPostContractGreedTrialPresenter.NextActionCompleteKey: return "Greed trial complete. Continue improving reward pressure without losing heat control.";
                default: return fallback;
            }
        }

        private static void AssertReadOnly(MvpPostContractGreedTrialSummary summary)
        {
            Assert.That(summary.WouldMutateState, Is.False);
            Assert.That(summary.WouldGrantRewards, Is.False);
            Assert.That(summary.WouldUnlockContent, Is.False);
            Assert.That(summary.WouldCallServer, Is.False);
        }
    }
}
#endif
