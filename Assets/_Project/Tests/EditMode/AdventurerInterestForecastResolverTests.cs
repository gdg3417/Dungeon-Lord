#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class AdventurerInterestForecastResolverTests
    {
        private static RunSimulationConfig ValidConfig(double scorePerSignal = 1d) => new RunSimulationConfig
        {
            AdventurerInterestForecastRuleSourceId = "run.adventurer_interest_forecast.rule.v1",
            AdventurerInterestLowThreshold = 5d,
            AdventurerInterestMediumThreshold = 10d,
            AdventurerInterestHighThreshold = 20d,
            AdventurerInterestScorePerAttractionSignal = scorePerSignal
        };

        private static RunAdventurerAttractionSummary ResolvedAttraction(double signal) => new RunAdventurerAttractionSummary
        {
            RuleResolved = true,
            DeterministicErrorCode = (int)RunAdventurerAttractionSummaryErrorCode.None,
            AttractionSignalValue = signal
        };

        [Test]
        public void Resolve_IsDeterministic_AndUsesAttractionSignalInput()
        {
            RunAdventurerInterestForecastSummary first = AdventurerInterestForecastResolver.Resolve(ValidConfig(2d), ResolvedAttraction(6d), 13);
            RunAdventurerInterestForecastSummary second = AdventurerInterestForecastResolver.Resolve(ValidConfig(2d), ResolvedAttraction(6d), 13);
            Assert.That(first.RuleResolved, Is.True);
            Assert.That(first.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerInterestForecastSummaryErrorCode.None));
            Assert.That(first.ForecastInterestScore, Is.EqualTo(12d));
            Assert.That(first.ForecastBandId, Is.EqualTo("adventurer_interest.medium"));
            Assert.That(second.ForecastInterestScore, Is.EqualTo(first.ForecastInterestScore));
        }

        [Test]
        public void Resolve_Fails_WhenAttractionSummaryMissingOrFailed()
        {
            RunAdventurerInterestForecastSummary missing = AdventurerInterestForecastResolver.Resolve(ValidConfig(), null, 2);
            RunAdventurerInterestForecastSummary failed = AdventurerInterestForecastResolver.Resolve(ValidConfig(), new RunAdventurerAttractionSummary { RuleResolved = false, DeterministicErrorCode = (int)RunAdventurerAttractionSummaryErrorCode.AggregateOverflow }, 2);
            Assert.That(missing.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerInterestForecastSummaryErrorCode.AttractionSummaryMissingOrFailed));
            Assert.That(failed.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerInterestForecastSummaryErrorCode.AttractionSummaryMissingOrFailed));
        }

        [Test]
        public void Resolve_Fails_WhenConfigInvalid()
        {
            Assert.That(AdventurerInterestForecastResolver.Resolve(null, ResolvedAttraction(10d), 4).DeterministicErrorCode, Is.EqualTo((int)RunAdventurerInterestForecastSummaryErrorCode.InvalidForecastConfig));
            Assert.That(AdventurerInterestForecastResolver.Resolve(new RunSimulationConfig { AdventurerInterestForecastRuleSourceId = " " }, ResolvedAttraction(10d), 4).DeterministicErrorCode, Is.EqualTo((int)RunAdventurerInterestForecastSummaryErrorCode.InvalidForecastConfig));
            Assert.That(AdventurerInterestForecastResolver.Resolve(new RunSimulationConfig { AdventurerInterestForecastRuleSourceId = "x", AdventurerInterestLowThreshold = double.NaN, AdventurerInterestMediumThreshold = 1d, AdventurerInterestHighThreshold = 2d, AdventurerInterestScorePerAttractionSignal = 1d }, ResolvedAttraction(10d), 4).DeterministicErrorCode, Is.EqualTo((int)RunAdventurerInterestForecastSummaryErrorCode.InvalidForecastConfig));
            Assert.That(AdventurerInterestForecastResolver.Resolve(new RunSimulationConfig { AdventurerInterestForecastRuleSourceId = "x", AdventurerInterestLowThreshold = 0d, AdventurerInterestMediumThreshold = double.PositiveInfinity, AdventurerInterestHighThreshold = 2d, AdventurerInterestScorePerAttractionSignal = 1d }, ResolvedAttraction(10d), 4).DeterministicErrorCode, Is.EqualTo((int)RunAdventurerInterestForecastSummaryErrorCode.InvalidForecastConfig));
            Assert.That(AdventurerInterestForecastResolver.Resolve(new RunSimulationConfig { AdventurerInterestForecastRuleSourceId = "x", AdventurerInterestLowThreshold = 2d, AdventurerInterestMediumThreshold = 1d, AdventurerInterestHighThreshold = 3d, AdventurerInterestScorePerAttractionSignal = 1d }, ResolvedAttraction(10d), 4).DeterministicErrorCode, Is.EqualTo((int)RunAdventurerInterestForecastSummaryErrorCode.InvalidForecastConfig));
            Assert.That(AdventurerInterestForecastResolver.Resolve(new RunSimulationConfig { AdventurerInterestForecastRuleSourceId = "x", AdventurerInterestLowThreshold = 1d, AdventurerInterestMediumThreshold = 2d, AdventurerInterestHighThreshold = 3d, AdventurerInterestScorePerAttractionSignal = -0.1d }, ResolvedAttraction(10d), 4).DeterministicErrorCode, Is.EqualTo((int)RunAdventurerInterestForecastSummaryErrorCode.InvalidForecastConfig));
        }

        [Test]
        public void Resolve_UsesThresholdBoundaries_Deterministically()
        {
            RunSimulationConfig config = ValidConfig(1d);
            Assert.That(AdventurerInterestForecastResolver.Resolve(config, ResolvedAttraction(0d), 1).ForecastBandId, Is.EqualTo("adventurer_interest.none"));
            Assert.That(AdventurerInterestForecastResolver.Resolve(config, ResolvedAttraction(4.99d), 1).ForecastBandId, Is.EqualTo("adventurer_interest.none"));
            Assert.That(AdventurerInterestForecastResolver.Resolve(config, ResolvedAttraction(5d), 1).ForecastBandId, Is.EqualTo("adventurer_interest.low"));
            Assert.That(AdventurerInterestForecastResolver.Resolve(config, ResolvedAttraction(10d), 1).ForecastBandId, Is.EqualTo("adventurer_interest.medium"));
            Assert.That(AdventurerInterestForecastResolver.Resolve(config, ResolvedAttraction(20d), 1).ForecastBandId, Is.EqualTo("adventurer_interest.high"));
        }

        [Test]
        public void Resolve_FailsOnAggregateOverflow()
        {
            RunAdventurerInterestForecastSummary overflowResult = AdventurerInterestForecastResolver.Resolve(ValidConfig(1d), ResolvedAttraction(double.PositiveInfinity), 6);
            Assert.That(overflowResult.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerInterestForecastSummaryErrorCode.AggregateOverflow));
        }

        [Test]
        public void RunOutcomeRecord_InterestForecastSummary_RoundTrips_WithPopulatedValues()
        {
            var save = new SaveData
            {
                runHistory = new RunHistoryState
                {
                    LatestOutcome = new RunOutcomeRecord { RunId = "run-2", AdventurerInterestForecastSummary = new RunAdventurerInterestForecastSummary { RuleResolved = true, DeterministicErrorCode = 0, ForecastBandId = "adventurer_interest.medium", ForecastInterestScore = 12d } },
                    RecentOutcomes = new[]
                    {
                        new RunOutcomeRecord { RunId = "run-1", AdventurerInterestForecastSummary = new RunAdventurerInterestForecastSummary { RuleResolved = false, DeterministicErrorCode = (int)RunAdventurerInterestForecastSummaryErrorCode.InvalidForecastConfig } },
                        new RunOutcomeRecord { RunId = "run-2", AdventurerInterestForecastSummary = new RunAdventurerInterestForecastSummary { RuleResolved = true, DeterministicErrorCode = 0, ForecastBandId = "adventurer_interest.medium", ForecastInterestScore = 12d } }
                    }
                }
            };

            string json = JsonUtility.ToJson(save);
            SaveData loaded = JsonUtility.FromJson<SaveData>(json);
            Assert.That(loaded.runHistory.LatestOutcome.AdventurerInterestForecastSummary, Is.Not.Null);
            Assert.That(loaded.runHistory.LatestOutcome.AdventurerInterestForecastSummary.ForecastBandId, Is.EqualTo("adventurer_interest.medium"));
            Assert.That(loaded.runHistory.RecentOutcomes[0].AdventurerInterestForecastSummary, Is.Not.Null);
        }

        [Test]
        public void RunOutcomeRecord_InterestForecastSummary_LegacyMissingField_DeserializesSafely()
        {
            const string legacyJson = "{\"runHistory\":{\"LatestOutcome\":{\"RunId\":\"run-legacy-latest\"},\"RecentOutcomes\":[{\"RunId\":\"run-legacy-recent\"}]}}";
            SaveData loaded = JsonUtility.FromJson<SaveData>(legacyJson);
            RunAdventurerInterestForecastSummary latest = loaded.runHistory.LatestOutcome.AdventurerInterestForecastSummary;
            RunAdventurerInterestForecastSummary recent = loaded.runHistory.RecentOutcomes[0].AdventurerInterestForecastSummary;

            if (latest != null)
            {
                Assert.That(latest.RuleResolved, Is.False);
                Assert.That(latest.DeterministicErrorCode, Is.EqualTo(0));
            }
            else
            {
                Assert.That(latest, Is.Null);
            }

            if (recent != null)
            {
                Assert.That(recent.RuleResolved, Is.False);
                Assert.That(recent.DeterministicErrorCode, Is.EqualTo(0));
            }
            else
            {
                Assert.That(recent, Is.Null);
            }
        }
    }
}
#endif
