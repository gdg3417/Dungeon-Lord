using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class AdventurerDemandBudgetResolverTests
    {
        private static RunSimulationConfig ValidConfig(double scorePerForecast = 1d) => new RunSimulationConfig
        {
            AdventurerDemandBudgetRuleSourceId = "run.adventurer_demand_budget.rule.v1",
            AdventurerDemandBudgetScorePerForecastScore = scorePerForecast,
            AdventurerDemandBudgetLowThreshold = 5d,
            AdventurerDemandBudgetMediumThreshold = 10d,
            AdventurerDemandBudgetHighThreshold = 20d
        };

        private static RunAdventurerInterestForecastSummary ResolvedForecast(double score, string bandId = "adventurer_interest.medium") => new RunAdventurerInterestForecastSummary
        {
            RuleResolved = true,
            DeterministicErrorCode = (int)RunAdventurerInterestForecastSummaryErrorCode.None,
            ForecastInterestScore = score,
            ForecastBandId = bandId
        };

        [Test]
        public void Resolve_IsDeterministic_AndUsesForecastSummaryInput()
        {
            var first = AdventurerDemandBudgetResolver.Resolve(ValidConfig(2d), ResolvedForecast(6d), 13);
            var second = AdventurerDemandBudgetResolver.Resolve(ValidConfig(2d), ResolvedForecast(6d), 13);
            Assert.That(first.RuleResolved, Is.True);
            Assert.That(first.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerDemandBudgetSummaryErrorCode.None));
            Assert.That(first.ForecastInterestScoreUsed, Is.EqualTo(6d));
            Assert.That(first.DemandBudgetScore, Is.EqualTo(12d));
            Assert.That(first.DemandBudgetBandId, Is.EqualTo("adventurer_demand.medium"));
            Assert.That(second.DemandBudgetScore, Is.EqualTo(first.DemandBudgetScore));
        }

        [Test]
        public void Resolve_Fails_WhenForecastSummaryMissingOrFailed()
        {
            var missing = AdventurerDemandBudgetResolver.Resolve(ValidConfig(), null, 2);
            var failed = AdventurerDemandBudgetResolver.Resolve(ValidConfig(), new RunAdventurerInterestForecastSummary { RuleResolved = false, DeterministicErrorCode = (int)RunAdventurerInterestForecastSummaryErrorCode.AggregateOverflow }, 2);
            Assert.That(missing.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerDemandBudgetSummaryErrorCode.InterestForecastMissingOrFailed));
            Assert.That(failed.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerDemandBudgetSummaryErrorCode.InterestForecastMissingOrFailed));
        }

        [Test]
        public void Resolve_Fails_WhenConfigInvalid()
        {
            Assert.That(AdventurerDemandBudgetResolver.Resolve(null, ResolvedForecast(10d), 4).DeterministicErrorCode, Is.EqualTo((int)RunAdventurerDemandBudgetSummaryErrorCode.InvalidDemandBudgetConfig));
            Assert.That(AdventurerDemandBudgetResolver.Resolve(new RunSimulationConfig { AdventurerDemandBudgetRuleSourceId = " " }, ResolvedForecast(10d), 4).DeterministicErrorCode, Is.EqualTo((int)RunAdventurerDemandBudgetSummaryErrorCode.InvalidDemandBudgetConfig));
            Assert.That(AdventurerDemandBudgetResolver.Resolve(new RunSimulationConfig { AdventurerDemandBudgetRuleSourceId = "x", AdventurerDemandBudgetLowThreshold = double.NaN, AdventurerDemandBudgetMediumThreshold = 1d, AdventurerDemandBudgetHighThreshold = 2d, AdventurerDemandBudgetScorePerForecastScore = 1d }, ResolvedForecast(10d), 4).DeterministicErrorCode, Is.EqualTo((int)RunAdventurerDemandBudgetSummaryErrorCode.InvalidDemandBudgetConfig));
            Assert.That(AdventurerDemandBudgetResolver.Resolve(new RunSimulationConfig { AdventurerDemandBudgetRuleSourceId = "x", AdventurerDemandBudgetLowThreshold = 0d, AdventurerDemandBudgetMediumThreshold = double.PositiveInfinity, AdventurerDemandBudgetHighThreshold = 2d, AdventurerDemandBudgetScorePerForecastScore = 1d }, ResolvedForecast(10d), 4).DeterministicErrorCode, Is.EqualTo((int)RunAdventurerDemandBudgetSummaryErrorCode.InvalidDemandBudgetConfig));
            Assert.That(AdventurerDemandBudgetResolver.Resolve(new RunSimulationConfig { AdventurerDemandBudgetRuleSourceId = "x", AdventurerDemandBudgetLowThreshold = 2d, AdventurerDemandBudgetMediumThreshold = 1d, AdventurerDemandBudgetHighThreshold = 3d, AdventurerDemandBudgetScorePerForecastScore = 1d }, ResolvedForecast(10d), 4).DeterministicErrorCode, Is.EqualTo((int)RunAdventurerDemandBudgetSummaryErrorCode.InvalidDemandBudgetConfig));
            Assert.That(AdventurerDemandBudgetResolver.Resolve(new RunSimulationConfig { AdventurerDemandBudgetRuleSourceId = "x", AdventurerDemandBudgetLowThreshold = 1d, AdventurerDemandBudgetMediumThreshold = 2d, AdventurerDemandBudgetHighThreshold = 3d, AdventurerDemandBudgetScorePerForecastScore = -0.1d }, ResolvedForecast(10d), 4).DeterministicErrorCode, Is.EqualTo((int)RunAdventurerDemandBudgetSummaryErrorCode.InvalidDemandBudgetConfig));
            Assert.That(AdventurerDemandBudgetResolver.Resolve(new RunSimulationConfig { AdventurerDemandBudgetRuleSourceId = "x", AdventurerDemandBudgetLowThreshold = 1d, AdventurerDemandBudgetMediumThreshold = 10d, AdventurerDemandBudgetHighThreshold = 9d, AdventurerDemandBudgetScorePerForecastScore = 1d }, ResolvedForecast(10d), 4).DeterministicErrorCode, Is.EqualTo((int)RunAdventurerDemandBudgetSummaryErrorCode.InvalidDemandBudgetConfig));
        }

        [Test]
        public void Resolve_UsesThresholdBoundaries_Deterministically()
        {
            RunSimulationConfig config = ValidConfig(1d);
            Assert.That(AdventurerDemandBudgetResolver.Resolve(config, ResolvedForecast(0d), 1).DemandBudgetBandId, Is.EqualTo("adventurer_demand.none"));
            Assert.That(AdventurerDemandBudgetResolver.Resolve(config, ResolvedForecast(4.99d), 1).DemandBudgetBandId, Is.EqualTo("adventurer_demand.none"));
            Assert.That(AdventurerDemandBudgetResolver.Resolve(config, ResolvedForecast(5d), 1).DemandBudgetBandId, Is.EqualTo("adventurer_demand.low"));
            Assert.That(AdventurerDemandBudgetResolver.Resolve(config, ResolvedForecast(10d), 1).DemandBudgetBandId, Is.EqualTo("adventurer_demand.medium"));
            Assert.That(AdventurerDemandBudgetResolver.Resolve(config, ResolvedForecast(20d), 1).DemandBudgetBandId, Is.EqualTo("adventurer_demand.high"));
        }

        [Test]
        public void Resolve_FailsOnAggregateOverflow()
        {
            var overflowResult = AdventurerDemandBudgetResolver.Resolve(ValidConfig(1d), ResolvedForecast(double.PositiveInfinity), 6);
            Assert.That(overflowResult.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerDemandBudgetSummaryErrorCode.AggregateOverflow));
        }

        [Test]
        public void RunOutcomeRecord_DemandBudgetSummary_RoundTrips_WithPopulatedValues()
        {
            var save = new SaveData
            {
                runHistory = new RunHistoryState
                {
                    LatestOutcome = new RunOutcomeRecord { RunId = "run-2", AdventurerDemandBudgetSummary = new RunAdventurerDemandBudgetSummary { RuleResolved = true, DeterministicErrorCode = 0, DemandBudgetBandId = "adventurer_demand.medium", DemandBudgetScore = 12d } },
                    RecentOutcomes = new[]
                    {
                        new RunOutcomeRecord { RunId = "run-1", AdventurerDemandBudgetSummary = new RunAdventurerDemandBudgetSummary { RuleResolved = false, DeterministicErrorCode = (int)RunAdventurerDemandBudgetSummaryErrorCode.InvalidDemandBudgetConfig } },
                        new RunOutcomeRecord { RunId = "run-2", AdventurerDemandBudgetSummary = new RunAdventurerDemandBudgetSummary { RuleResolved = true, DeterministicErrorCode = 0, DemandBudgetBandId = "adventurer_demand.medium", DemandBudgetScore = 12d } }
                    }
                }
            };

            string json = JsonUtility.ToJson(save);
            SaveData loaded = JsonUtility.FromJson<SaveData>(json);
            Assert.That(loaded.runHistory.LatestOutcome.AdventurerDemandBudgetSummary, Is.Not.Null);
            Assert.That(loaded.runHistory.LatestOutcome.AdventurerDemandBudgetSummary.DemandBudgetBandId, Is.EqualTo("adventurer_demand.medium"));
            Assert.That(loaded.runHistory.RecentOutcomes[0].AdventurerDemandBudgetSummary, Is.Not.Null);
            Assert.That(loaded.runHistory.RecentOutcomes[0].AdventurerDemandBudgetSummary.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerDemandBudgetSummaryErrorCode.InvalidDemandBudgetConfig));
            Assert.That(loaded.runHistory.RecentOutcomes[1].AdventurerDemandBudgetSummary.DemandBudgetBandId, Is.EqualTo("adventurer_demand.medium"));
            Assert.That(loaded.runHistory.RecentOutcomes[1].AdventurerDemandBudgetSummary.DemandBudgetScore, Is.EqualTo(12d));
        }

        [Test]
        public void RunOutcomeRecord_DemandBudgetSummary_LegacyMissingField_DeserializesSafely()
        {
            const string legacyJson = "{\"runHistory\":{\"LatestOutcome\":{\"RunId\":\"run-legacy-latest\"},\"RecentOutcomes\":[{\"RunId\":\"run-legacy-recent\"}]}}";
            SaveData loaded = JsonUtility.FromJson<SaveData>(legacyJson);
            RunAdventurerDemandBudgetSummary latest = loaded.runHistory.LatestOutcome.AdventurerDemandBudgetSummary;
            RunAdventurerDemandBudgetSummary recent = loaded.runHistory.RecentOutcomes[0].AdventurerDemandBudgetSummary;

            if (latest != null)
            {
                Assert.That(latest.RuleResolved, Is.False);
                Assert.That(latest.DeterministicErrorCode, Is.EqualTo(0));
            }

            if (recent != null)
            {
                Assert.That(recent.RuleResolved, Is.False);
                Assert.That(recent.DeterministicErrorCode, Is.EqualTo(0));
            }
        }
    }
}
