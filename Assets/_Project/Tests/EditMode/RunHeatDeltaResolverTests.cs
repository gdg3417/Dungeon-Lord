using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests
{
    public class RunHeatDeltaResolverTests
    {
        private static RunSimulationConfig ValidConfig() => new RunSimulationConfig
        {
            RunHeatNormalDeathDelta = 2d,
            RunHeatEliteDeathDelta = 3d,
            RunHeatMultipleDeathBonusDelta = 1d,
            RunHeatSurvivorCoolingPerSurvivor = 0.5d,
            RunHeatLootCoolingPerExtractedValue = 0.1d,
            RunHeatDeltaMinimum = -10d,
            RunHeatDeltaMaximum = 10d,
            RunHeatDeltaRuleSourceId = "run.heat_delta.rule.v1"
        };

        [Test] public void Deterministic_ForSameInputs() { var a=RunHeatDeltaResolver.Resolve(ValidConfig(),new RunSurvivalSummary{RuleResolved=true,DeathCount=2,SurvivorCount=1},new RunLootExtractionSummary{RuleResolved=true,TotalExtractedTradeableWorldValue=10},new RunAdventurerDemandBudgetSummary{RuleResolved=true,DemandBudgetBandId="adventurer_demand.high"},7); var b=RunHeatDeltaResolver.Resolve(ValidConfig(),new RunSurvivalSummary{RuleResolved=true,DeathCount=2,SurvivorCount=1},new RunLootExtractionSummary{RuleResolved=true,TotalExtractedTradeableWorldValue=10},new RunAdventurerDemandBudgetSummary{RuleResolved=true,DemandBudgetBandId="adventurer_demand.high"},7); Assert.That(a.FinalHeatDelta, Is.EqualTo(b.FinalHeatDelta)); }
        [Test] public void AppliesDeathEliteBonusSurvivorLootAndClamp() { var s=RunHeatDeltaResolver.Resolve(ValidConfig(),new RunSurvivalSummary{RuleResolved=true,DeathCount=2,SurvivorCount=1},new RunLootExtractionSummary{RuleResolved=true,TotalExtractedTradeableWorldValue=10},new RunAdventurerDemandBudgetSummary{RuleResolved=true,DemandBudgetBandId="adventurer_demand.high"},1); Assert.That(s.DeathHeatDelta, Is.EqualTo(4d)); Assert.That(s.EliteDeathHeatDelta, Is.EqualTo(2d)); Assert.That(s.MultipleDeathBonusDelta, Is.EqualTo(1d)); Assert.That(s.SurvivorCoolingDelta, Is.EqualTo(-0.5d)); Assert.That(s.LootCoolingDelta, Is.EqualTo(-1d)); Assert.That(s.FinalHeatDelta, Is.EqualTo(5.5d)); }
        [Test] public void FullWipe_DisablesLootCooling() { var s=RunHeatDeltaResolver.Resolve(ValidConfig(),new RunSurvivalSummary{RuleResolved=true,DeathCount=3,SurvivorCount=0},new RunLootExtractionSummary{RuleResolved=true,TotalExtractedTradeableWorldValue=999},null,1); Assert.That(s.LootCoolingDelta, Is.EqualTo(0d)); }
        [Test] public void AllowsNegativeWithinClamp() { var s=RunHeatDeltaResolver.Resolve(ValidConfig(),new RunSurvivalSummary{RuleResolved=true,DeathCount=0,SurvivorCount=4},new RunLootExtractionSummary{RuleResolved=true,TotalExtractedTradeableWorldValue=90},null,1); Assert.That(s.FinalHeatDelta, Is.LessThan(0d)); Assert.That(s.FinalHeatDelta, Is.EqualTo(-10d)); }
        [Test] public void InvalidConfig_FailsStableCode() { var s=RunHeatDeltaResolver.Resolve(new RunSimulationConfig { RunHeatDeltaRuleSourceId="" },new RunSurvivalSummary{RuleResolved=true},new RunLootExtractionSummary{RuleResolved=true},null,1); Assert.That(s.RuleResolved, Is.False); Assert.That(s.DeterministicErrorCode, Is.EqualTo((int)RunHeatDeltaSummaryErrorCode.InvalidHeatDeltaConfig)); }
        [Test] public void Overflow_ReturnsAggregateOverflow() { var c=ValidConfig(); c.RunHeatNormalDeathDelta=double.MaxValue; var s=RunHeatDeltaResolver.Resolve(c,new RunSurvivalSummary{RuleResolved=true,DeathCount=2,SurvivorCount=0},new RunLootExtractionSummary{RuleResolved=true},null,1); Assert.That(s.DeterministicErrorCode, Is.EqualTo((int)RunHeatDeltaSummaryErrorCode.AggregateOverflow)); }

        [Test]
        public void RunOutcomeRecord_HeatDeltaSummary_RoundTrips_And_LegacyMissingField_Safe()
        {
            var save = new SaveData{ runHistory = new RunHistoryState{ LatestOutcome = new RunOutcomeRecord{ RunId="r1", RunHeatDeltaSummary = new RunHeatDeltaSummary{ RuleResolved=true, FinalHeatDelta=1d, RuleSourceIdUsed="run.heat_delta.rule.v1"}}}};
            SaveData loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));
            Assert.That(loaded.runHistory.LatestOutcome.RunHeatDeltaSummary, Is.Not.Null);
            Assert.That(loaded.runHistory.LatestOutcome.RunHeatDeltaSummary.FinalHeatDelta, Is.EqualTo(1d));

            SaveData legacy = JsonUtility.FromJson<SaveData>("{\"runHistory\":{\"LatestOutcome\":{\"RunId\":\"legacy\"}}}");
            Assert.That(legacy.runHistory.LatestOutcome.RunHeatDeltaSummary, Is.Null);
        }
    }
}
