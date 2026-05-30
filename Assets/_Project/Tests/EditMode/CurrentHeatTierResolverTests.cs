using DungeonBuilder.M0;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class CurrentHeatTierResolverTests
    {
        [TestCase(0d, CurrentHeatTierResolver.PeaceTierId, true, false)]
        [TestCase(9d, CurrentHeatTierResolver.PeaceTierId, false, true)]
        [TestCase(10d, CurrentHeatTierResolver.NoticeTierId, true, false)]
        [TestCase(24d, CurrentHeatTierResolver.NoticeTierId, false, true)]
        [TestCase(25d, CurrentHeatTierResolver.ConcernTierId, true, false)]
        [TestCase(49d, CurrentHeatTierResolver.ConcernTierId, false, true)]
        public void Resolve_ConfiguredBoundaries_ClassifiesTierAndBoundaryFlags(double heat, string expectedTierId, bool expectedAtMinimum, bool expectedAtMaximum)
        {
            CurrentHeatTierSummary result = CurrentHeatTierResolver.Resolve(ValidConfig(), heat);

            Assert.That(result.RuleResolved, Is.True);
            Assert.That(result.DeterministicErrorCode, Is.EqualTo((int)CurrentHeatTierSummaryErrorCode.None));
            Assert.That(result.CurrentHeat, Is.EqualTo(heat));
            Assert.That(result.TierId, Is.EqualTo(expectedTierId));
            Assert.That(result.IsAtTierMinimum, Is.EqualTo(expectedAtMinimum));
            Assert.That(result.IsAtTierMaximum, Is.EqualTo(expectedAtMaximum));
            Assert.That(result.RuleSourceIdUsed, Is.EqualTo("run.heat_application.rule.test"));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Resolve_NonFiniteHeat_ReturnsStableInvalidHeatError(double heat)
        {
            CurrentHeatTierSummary result = CurrentHeatTierResolver.Resolve(ValidConfig(), heat);

            Assert.That(result.RuleResolved, Is.False);
            Assert.That(result.DeterministicErrorCode, Is.EqualTo((int)CurrentHeatTierSummaryErrorCode.InvalidCurrentHeat));
        }

        [Test]
        public void Resolve_InvalidConfig_ReturnsStableInvalidConfigError()
        {
            RunSimulationConfig config = ValidConfig();
            config.HeatNoticeMinimum = config.HeatPeaceMaximum;

            CurrentHeatTierSummary result = CurrentHeatTierResolver.Resolve(config, 5d);

            Assert.That(result.RuleResolved, Is.False);
            Assert.That(result.DeterministicErrorCode, Is.EqualTo((int)CurrentHeatTierSummaryErrorCode.InvalidHeatTierConfig));
        }

        [TestCase(-1d)]
        [TestCase(50d)]
        public void Resolve_OutOfConfiguredMvpRange_ReturnsStableOutOfRangeError(double heat)
        {
            CurrentHeatTierSummary result = CurrentHeatTierResolver.Resolve(ValidConfig(), heat);

            Assert.That(result.RuleResolved, Is.False);
            Assert.That(result.DeterministicErrorCode, Is.EqualTo((int)CurrentHeatTierSummaryErrorCode.CurrentHeatOutOfRange));
            Assert.That(result.CurrentHeat, Is.EqualTo(heat));
        }

        private static RunSimulationConfig ValidConfig()
        {
            return new RunSimulationConfig
            {
                HeatPeaceMinimum = 0d,
                HeatPeaceMaximum = 9d,
                HeatNoticeMinimum = 10d,
                HeatNoticeMaximum = 24d,
                HeatConcernMinimum = 25d,
                HeatConcernMaximum = 49d,
                RunHeatApplicationRuleSourceId = "run.heat_application.rule.test"
            };
        }
    }
}
