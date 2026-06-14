using NUnit.Framework;
using UnityEngine;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.Tests.EditMode
{
    public class AdventurerRunIntentResolverTests
    {
        [Test]
        public void Resolve_HighLootAndAttractionSelectsGreedy()
        {
            AdventurerRunIntentSummary summary = AdventurerRunIntentResolver.Resolve(Config(), Effects(0, 8, 5, 0, 2), 0d, Heat("heat_tier.peace"), null);

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.IntentId, Is.EqualTo(RunPostureResolver.GreedyId));
        }

        [Test]
        public void Resolve_HighDangerAndRecentDeathsSelectsCautious()
        {
            var latest = new RunOutcomeRecord { SurvivalSummary = new RunSurvivalSummary { DeathCount = 3 } };
            AdventurerRunIntentSummary summary = AdventurerRunIntentResolver.Resolve(Config(), Effects(7, 0, 0, 3, 0), 30d, Heat("heat_tier.concern"), latest);

            Assert.That(summary.IntentId, Is.EqualTo(RunPostureResolver.CautiousId));
            Assert.That(summary.PrimaryReasonKey, Is.EqualTo(AdventurerRunIntentResolver.ReasonDeathsHeatKey));
        }

        [Test]
        public void Resolve_ModerateSignalsSelectBalanced()
        {
            AdventurerRunIntentSummary summary = AdventurerRunIntentResolver.Resolve(Config(), Effects(1, 1, 1, 1, 1), 0d, Heat("heat_tier.peace"), null);

            Assert.That(summary.IntentId, Is.EqualTo(RunPostureResolver.BalancedId));
        }

        [Test]
        public void Resolve_SameInputsAreDeterministic()
        {
            var config = Config();
            var effects = Effects(1, 1, 1, 1, 1);
            string first = JsonUtility.ToJson(AdventurerRunIntentResolver.Resolve(config, effects, 0d, Heat("heat_tier.peace"), null));
            string second = JsonUtility.ToJson(AdventurerRunIntentResolver.Resolve(config, effects, 0d, Heat("heat_tier.peace"), null));

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Resolve_DoesNotMutateSaveState()
        {
            var save = new SaveData { structureRuntime = new StructureRuntimeState { Heat = 4d, ManaReserve = 12d } };
            string before = JsonUtility.ToJson(save);

            AdventurerRunIntentResolver.Resolve(Config(), Effects(1, 1, 1, 1, 1), save.structureRuntime.Heat, Heat("heat_tier.peace"), null);

            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
        }

        [Test]
        public void Presenter_BuildsLocalizedSummaryWithoutRawKeys()
        {
            var summary = AdventurerRunIntentResolver.Resolve(Config(), Effects(0, 8, 5, 0, 2), 0d, Heat("heat_tier.peace"), null);
            string line = AdventurerRunIntentPresenter.BuildSummaryLine(summary, Localized);

            Assert.That(line, Is.EqualTo("Adventurer intent: Greedy likely. Reason: loot signal is high and heat is low"));
            Assert.That(line, Does.Not.Contain("run.posture.greedy"));
            Assert.That(line, Does.Not.Contain("ui.adventurer_intent"));
        }

        private static MvpPlacementEffectsSummary Effects(int danger, int loot, int attraction, int heatPressure, int pathCapacity) => new MvpPlacementEffectsSummary { RuleResolved = true, Danger = danger, LootBonus = loot, Attraction = attraction, HeatPressure = heatPressure, PathCapacity = pathCapacity };
        private static CurrentHeatTierSummary Heat(string tier) => new CurrentHeatTierSummary { RuleResolved = true, TierId = tier };
        private static RunSimulationConfig Config() => new RunSimulationConfig
        {
            AdventurerIntentRuleSourceId = "run.adventurer_intent.rule.test",
            IntentGreedyScorePerLoot = 2d,
            IntentGreedyScorePerAttraction = 1.5d,
            IntentGreedyPenaltyPerHeatTierRank = 3d,
            IntentGreedyPenaltyPerRecentDeath = 4d,
            IntentGreedyPenaltyPerDanger = 0.75d,
            IntentCautiousScorePerDanger = 1.5d,
            IntentCautiousScorePerHeatPressure = 2d,
            IntentCautiousScorePerHeatTierRank = 3d,
            IntentCautiousScorePerRecentDeath = 4d,
            IntentCautiousReductionPerPathCapacity = 0.75d,
            IntentBalancedBaseScore = 7d,
            IntentBalancedPenaltyPerExtremeScoreDelta = 0.2d,
            IntentModerateRiskTarget = 4d,
            IntentModerateRewardTarget = 4d,
            IntentBalancedPenaltyPerModerateDistance = 0.6d,
            IntentMinimumScore = 0d,
            IntentMaximumScore = 20d
        };

        private static string Localized(string key, string fallback)
        {
            switch (key)
            {
                case "run.posture.cautious.name": return "Cautious";
                case "run.posture.balanced.name": return "Balanced";
                case "run.posture.greedy.name": return "Greedy";
                case AdventurerRunIntentPresenter.SummaryFormatKey: return "Adventurer intent: {0} likely. Reason: {1}";
                case AdventurerRunIntentPresenter.DebugPostureFormatKey: return "Adventurer intent: {0} likely. Selected debug posture: {1}.";
                case AdventurerRunIntentResolver.ReasonLootHighHeatLowKey: return "loot signal is high and heat is low";
                case AdventurerRunIntentResolver.ReasonDeathsHeatKey: return "recent deaths and rising heat";
                case AdventurerRunIntentResolver.ReasonModerateKey: return "risk and reward are both moderate";
                case AdventurerRunIntentResolver.ReasonDangerKey: return "danger pressure is high";
                case AdventurerRunIntentResolver.ReasonFallbackKey: return "current dungeon signals are still forming";
                default: return fallback;
            }
        }
    }
}
