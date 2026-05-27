using NUnit.Framework;

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
            Assert.That(second.ForecastBandId, Is.EqualTo(first.ForecastBandId));
        }

        [Test]
        public void Resolve_Fails_WhenAttractionSummaryMissingOrFailed()
        {
            RunAdventurerInterestForecastSummary missing = AdventurerInterestForecastResolver.Resolve(ValidConfig(), null, 2);
            RunAdventurerInterestForecastSummary failed = AdventurerInterestForecastResolver.Resolve(ValidConfig(), new RunAdventurerAttractionSummary { RuleResolved = false }, 2);

            Assert.That(missing.RuleResolved, Is.False);
            Assert.That(missing.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerInterestForecastSummaryErrorCode.AttractionSummaryMissingOrFailed));
            Assert.That(failed.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerInterestForecastSummaryErrorCode.AttractionSummaryMissingOrFailed));
        }

        [Test]
        public void Resolve_Fails_WhenConfigInvalid()
        {
            RunAdventurerInterestForecastSummary result = AdventurerInterestForecastResolver.Resolve(ValidConfig(), ResolvedAttraction(10d), 4);
            Assert.That(result.RuleResolved, Is.True);

            RunAdventurerInterestForecastSummary invalid = AdventurerInterestForecastResolver.Resolve(new RunSimulationConfig { AdventurerInterestForecastRuleSourceId = "x", AdventurerInterestLowThreshold = 2d, AdventurerInterestMediumThreshold = 1d, AdventurerInterestHighThreshold = 3d, AdventurerInterestScorePerAttractionSignal = 1d }, ResolvedAttraction(10d), 4);
            Assert.That(invalid.RuleResolved, Is.False);
            Assert.That(invalid.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerInterestForecastSummaryErrorCode.InvalidForecastConfig));
        }

        [Test]
        public void Resolve_UsesThresholdBoundaries_Deterministically()
        {
            RunSimulationConfig config = ValidConfig(1d);

            Assert.That(AdventurerInterestForecastResolver.Resolve(config, ResolvedAttraction(5d), 1).ForecastBandId, Is.EqualTo("adventurer_interest.low"));
            Assert.That(AdventurerInterestForecastResolver.Resolve(config, ResolvedAttraction(10d), 1).ForecastBandId, Is.EqualTo("adventurer_interest.medium"));
            Assert.That(AdventurerInterestForecastResolver.Resolve(config, ResolvedAttraction(20d), 1).ForecastBandId, Is.EqualTo("adventurer_interest.high"));
            Assert.That(AdventurerInterestForecastResolver.Resolve(config, ResolvedAttraction(4.99d), 1).ForecastBandId, Is.EqualTo("adventurer_interest.none"));
        }

        [Test]
        public void Resolve_FailsOnNaNOrInfinityAggregate()
        {
            RunAdventurerInterestForecastSummary nanResult = AdventurerInterestForecastResolver.Resolve(ValidConfig(double.PositiveInfinity), ResolvedAttraction(1d), 6);
            RunAdventurerInterestForecastSummary overflowResult = AdventurerInterestForecastResolver.Resolve(ValidConfig(1d), ResolvedAttraction(double.PositiveInfinity), 6);

            Assert.That(nanResult.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerInterestForecastSummaryErrorCode.InvalidForecastConfig));
            Assert.That(overflowResult.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerInterestForecastSummaryErrorCode.AggregateOverflow));
        }
    }
}
