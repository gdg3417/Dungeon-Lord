using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests
{
    public class RunHeatDeltaResolverTests
    {
        private static RunSimulationConfig ValidConfig() => new RunSimulationConfig
        {
            RunHeatNormalDeathDelta = 1d,
            RunHeatEliteDeathDelta = 3d,
            RunHeatMultipleDeathBonusDelta = 1d,
            RunHeatSurvivorCoolingPerSurvivor = 0.5d,
            RunHeatLootCoolingPerExtractedValue = 0.1d,
            RunHeatDeltaMinimum = -10d,
            RunHeatDeltaMaximum = 10d,
            RunHeatDeltaRuleSourceId = "run.heat_delta.rule.v1"
        };

        [Test]
        public void Deterministic_ForSameInputs()
        {
            var survival = new RunSurvivalSummary { RuleResolved = true, PartySize = 3, DeathCount = 2, SurvivorCount = 1 };
            var extraction = new RunLootExtractionSummary { RuleResolved = true, TotalExtractedTradeableWorldValue = 10 };

            RunHeatDeltaSummary first = RunHeatDeltaResolver.Resolve(ValidConfig(), survival, extraction, 7);
            RunHeatDeltaSummary second = RunHeatDeltaResolver.Resolve(ValidConfig(), survival, extraction, 7);

            Assert.That(first.DeathHeatDelta, Is.EqualTo(second.DeathHeatDelta));
            Assert.That(first.EliteDeathHeatDelta, Is.EqualTo(second.EliteDeathHeatDelta));
            Assert.That(first.MultipleDeathBonusDelta, Is.EqualTo(second.MultipleDeathBonusDelta));
            Assert.That(first.SurvivorCoolingDelta, Is.EqualTo(second.SurvivorCoolingDelta));
            Assert.That(first.LootCoolingDelta, Is.EqualTo(second.LootCoolingDelta));
            Assert.That(first.FinalHeatDelta, Is.EqualTo(second.FinalHeatDelta));
        }

        [Test]
        public void AppliesNormalDeathMultipleDeathBonusAndLootCooling()
        {
            RunHeatDeltaSummary summary = RunHeatDeltaResolver.Resolve(
                ValidConfig(),
                new RunSurvivalSummary { RuleResolved = true, PartySize = 3, DeathCount = 2, SurvivorCount = 1 },
                new RunLootExtractionSummary { RuleResolved = true, TotalExtractedTradeableWorldValue = 10 },
                1);

            Assert.That(summary.DeathHeatDelta, Is.EqualTo(2d));
            Assert.That(summary.EliteDeathHeatDelta, Is.EqualTo(0d));
            Assert.That(summary.MultipleDeathBonusDelta, Is.EqualTo(1d));
            Assert.That(summary.SurvivorCoolingDelta, Is.EqualTo(0d));
            Assert.That(summary.LootCoolingDelta, Is.EqualTo(-1d));
            Assert.That(summary.FinalHeatDelta, Is.EqualTo(2d));
        }

        [Test]
        public void FullPartySurvival_AppliesSurvivorCooling()
        {
            RunHeatDeltaSummary summary = RunHeatDeltaResolver.Resolve(
                ValidConfig(),
                new RunSurvivalSummary { RuleResolved = true, PartySize = 4, DeathCount = 0, SurvivorCount = 4 },
                new RunLootExtractionSummary { RuleResolved = true, TotalExtractedTradeableWorldValue = 0 },
                1);

            Assert.That(summary.SurvivorCoolingDelta, Is.EqualTo(-2d));
            Assert.That(summary.FinalHeatDelta, Is.EqualTo(-2d));
        }

        [Test]
        public void PartialSurvival_DoesNotApplySurvivorCooling()
        {
            RunHeatDeltaSummary summary = RunHeatDeltaResolver.Resolve(
                ValidConfig(),
                new RunSurvivalSummary { RuleResolved = true, PartySize = 4, DeathCount = 2, SurvivorCount = 2 },
                new RunLootExtractionSummary { RuleResolved = true, TotalExtractedTradeableWorldValue = 0 },
                1);

            Assert.That(summary.SurvivorCoolingDelta, Is.EqualTo(0d));
        }

        [Test]
        public void HighDemandBudgetSignal_DoesNotCreateEliteHeat()
        {
            var highDemandSignal = new RunAdventurerDemandBudgetSummary
            {
                RuleResolved = true,
                DemandBudgetBandId = "adventurer_demand.high"
            };

            RunHeatDeltaSummary summary = RunHeatDeltaResolver.Resolve(
                ValidConfig(),
                new RunSurvivalSummary { RuleResolved = true, PartySize = 3, DeathCount = 3, SurvivorCount = 0 },
                new RunLootExtractionSummary { RuleResolved = true, TotalExtractedTradeableWorldValue = 0 },
                1);

            Assert.That(highDemandSignal.DemandBudgetBandId, Is.EqualTo("adventurer_demand.high"));
            Assert.That(summary.EliteDeathHeatDelta, Is.EqualTo(0d));
            Assert.That(summary.FinalHeatDelta, Is.EqualTo(4d));
        }

        [Test]
        public void FullWipe_DisablesLootCooling()
        {
            RunHeatDeltaSummary summary = RunHeatDeltaResolver.Resolve(
                ValidConfig(),
                new RunSurvivalSummary { RuleResolved = true, PartySize = 3, DeathCount = 3, SurvivorCount = 0 },
                new RunLootExtractionSummary { RuleResolved = true, TotalExtractedTradeableWorldValue = 999 },
                1);

            Assert.That(summary.LootCoolingDelta, Is.EqualTo(0d));
        }

        [Test]
        public void AllowsNegativeWithinClamp()
        {
            RunHeatDeltaSummary summary = RunHeatDeltaResolver.Resolve(
                ValidConfig(),
                new RunSurvivalSummary { RuleResolved = true, PartySize = 1, DeathCount = 0, SurvivorCount = 1 },
                new RunLootExtractionSummary { RuleResolved = true, TotalExtractedTradeableWorldValue = 200 },
                1);

            Assert.That(summary.FinalHeatDelta, Is.LessThan(0d));
            Assert.That(summary.FinalHeatDelta, Is.EqualTo(-10d));
        }

        [Test]
        public void InvalidConfig_FailsStableCode()
        {
            RunHeatDeltaSummary summary = RunHeatDeltaResolver.Resolve(
                new RunSimulationConfig { RunHeatDeltaRuleSourceId = string.Empty },
                new RunSurvivalSummary { RuleResolved = true },
                new RunLootExtractionSummary { RuleResolved = true },
                1);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)RunHeatDeltaSummaryErrorCode.InvalidHeatDeltaConfig));
        }

        [Test]
        public void SurvivalSummaryMissingOrFailed_ReturnsStableCode()
        {
            RunHeatDeltaSummary summary = RunHeatDeltaResolver.Resolve(
                ValidConfig(),
                new RunSurvivalSummary { RuleResolved = false },
                new RunLootExtractionSummary { RuleResolved = true },
                1);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)RunHeatDeltaSummaryErrorCode.SurvivalSummaryMissingOrFailed));
        }

        [Test]
        public void ExtractionSummaryMissingOrFailed_ReturnsStableCode()
        {
            RunHeatDeltaSummary summary = RunHeatDeltaResolver.Resolve(
                ValidConfig(),
                new RunSurvivalSummary { RuleResolved = true },
                new RunLootExtractionSummary { RuleResolved = false },
                1);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)RunHeatDeltaSummaryErrorCode.ExtractionSummaryMissingOrFailed));
        }

        [Test]
        public void Overflow_ReturnsAggregateOverflow_WithFinitePersistedFields()
        {
            RunSimulationConfig config = ValidConfig();
            config.RunHeatNormalDeathDelta = double.MaxValue;
            config.RunHeatEliteDeathDelta = double.MaxValue;

            RunHeatDeltaSummary summary = RunHeatDeltaResolver.Resolve(
                config,
                new RunSurvivalSummary { RuleResolved = true, PartySize = 2, DeathCount = 2, SurvivorCount = 0 },
                new RunLootExtractionSummary { RuleResolved = true },
                1);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)RunHeatDeltaSummaryErrorCode.AggregateOverflow));
            AssertFinite(summary);
        }

        [Test]
        public void RunOutcomeRecord_HeatDeltaSummary_RoundTrips_And_LegacyMissingField_Safe()
        {
            var save = new SaveData
            {
                runHistory = new RunHistoryState
                {
                    LatestOutcome = new RunOutcomeRecord
                    {
                        RunId = "r1",
                        RunHeatDeltaSummary = new RunHeatDeltaSummary { RuleResolved = true, FinalHeatDelta = 1d, RuleSourceIdUsed = "run.heat_delta.rule.v1" }
                    }
                }
            };

            SaveData loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));
            Assert.That(loaded.runHistory.LatestOutcome.RunHeatDeltaSummary, Is.Not.Null);
            Assert.That(loaded.runHistory.LatestOutcome.RunHeatDeltaSummary.FinalHeatDelta, Is.EqualTo(1d));

            SaveData legacy = JsonUtility.FromJson<SaveData>("{\"runHistory\":{\"LatestOutcome\":{\"RunId\":\"legacy\"}}}");
            Assert.That(legacy.runHistory.LatestOutcome.RunId, Is.EqualTo("legacy"));

            RunHeatDeltaSummary legacySummary = legacy.runHistory.LatestOutcome.RunHeatDeltaSummary;
            if (legacySummary != null)
            {
                Assert.That(legacySummary.RuleResolved, Is.False);
                AssertFinite(legacySummary);
            }
        }

        private static void AssertFinite(RunHeatDeltaSummary summary)
        {
            Assert.That(double.IsNaN(summary.DeathHeatDelta) || double.IsInfinity(summary.DeathHeatDelta), Is.False);
            Assert.That(double.IsNaN(summary.EliteDeathHeatDelta) || double.IsInfinity(summary.EliteDeathHeatDelta), Is.False);
            Assert.That(double.IsNaN(summary.MultipleDeathBonusDelta) || double.IsInfinity(summary.MultipleDeathBonusDelta), Is.False);
            Assert.That(double.IsNaN(summary.SurvivorCoolingDelta) || double.IsInfinity(summary.SurvivorCoolingDelta), Is.False);
            Assert.That(double.IsNaN(summary.LootCoolingDelta) || double.IsInfinity(summary.LootCoolingDelta), Is.False);
            Assert.That(double.IsNaN(summary.FinalHeatDelta) || double.IsInfinity(summary.FinalHeatDelta), Is.False);
        }
    }
}
