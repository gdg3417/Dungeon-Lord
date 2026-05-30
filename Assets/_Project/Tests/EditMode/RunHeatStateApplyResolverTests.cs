using DungeonBuilder.M0;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class RunHeatStateApplyResolverTests
    {
        [TestCase(2d, 3d, 5d, RunHeatStateApplyResolver.PeaceTierId, RunHeatStateApplyResolver.PeaceTierId, false)]
        [TestCase(8d, 3d, 11d, RunHeatStateApplyResolver.PeaceTierId, RunHeatStateApplyResolver.NoticeTierId, true)]
        [TestCase(23d, 3d, 26d, RunHeatStateApplyResolver.NoticeTierId, RunHeatStateApplyResolver.ConcernTierId, true)]
        [TestCase(48d, 3d, 49d, RunHeatStateApplyResolver.ConcernTierId, RunHeatStateApplyResolver.ConcernTierId, false)]
        [TestCase(20d, -3d, 17d, RunHeatStateApplyResolver.NoticeTierId, RunHeatStateApplyResolver.NoticeTierId, false)]
        [TestCase(11d, -3d, 8d, RunHeatStateApplyResolver.NoticeTierId, RunHeatStateApplyResolver.PeaceTierId, true)]
        [TestCase(1d, -3d, 0d, RunHeatStateApplyResolver.PeaceTierId, RunHeatStateApplyResolver.PeaceTierId, false)]
        public void Resolve_AppliesDelta_ClampsAndReportsTiers(double heatBefore, double delta, double expectedAfter, string expectedTierBefore, string expectedTierAfter, bool expectedChanged)
        {
            RunHeatApplicationSummary result = RunHeatStateApplyResolver.Resolve(ValidConfig(), heatBefore, ResolvedDelta(delta));

            Assert.That(result.RuleResolved, Is.True);
            Assert.That(result.DeterministicErrorCode, Is.EqualTo((int)RunHeatApplicationSummaryErrorCode.None));
            Assert.That(result.HeatBefore, Is.EqualTo(heatBefore));
            Assert.That(result.AppliedDelta, Is.EqualTo(expectedAfter - heatBefore));
            Assert.That(result.HeatAfter, Is.EqualTo(expectedAfter));
            Assert.That(result.TierBefore, Is.EqualTo(expectedTierBefore));
            Assert.That(result.TierAfter, Is.EqualTo(expectedTierAfter));
            Assert.That(result.TierChanged, Is.EqualTo(expectedChanged));
            Assert.That(result.RuleSourceIdUsed, Is.EqualTo("run.heat_application.rule.test"));
        }

        [Test]
        public void Resolve_MissingSummary_ReturnsStableUnresolvedError()
        {
            AssertUnresolved(RunHeatStateApplyResolver.Resolve(ValidConfig(), 5d, null), RunHeatApplicationSummaryErrorCode.HeatDeltaSummaryMissing, 5d);
        }

        [Test]
        public void Resolve_UnresolvedSummary_ReturnsStableUnresolvedError()
        {
            AssertUnresolved(RunHeatStateApplyResolver.Resolve(ValidConfig(), 5d, new RunHeatDeltaSummary()), RunHeatApplicationSummaryErrorCode.HeatDeltaSummaryUnresolved, 5d);
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Resolve_NonFiniteDelta_ReturnsStableInvalidSummaryError(double delta)
        {
            AssertUnresolved(RunHeatStateApplyResolver.Resolve(ValidConfig(), 5d, ResolvedDelta(delta)), RunHeatApplicationSummaryErrorCode.InvalidHeatDeltaSummary, 5d);
        }

        [Test]
        public void Resolve_InvalidConfig_ReturnsStableInvalidConfigError()
        {
            RunSimulationConfig config = ValidConfig();
            config.HeatNoticeMinimum = config.HeatPeaceMaximum;

            AssertUnresolved(RunHeatStateApplyResolver.Resolve(config, 5d, ResolvedDelta(1d)), RunHeatApplicationSummaryErrorCode.InvalidHeatApplicationConfig, 5d);
        }

        [Test]
        public void Resolve_BlankRuleSourceId_ReturnsStableInvalidConfigError()
        {
            RunSimulationConfig config = ValidConfig();
            config.RunHeatApplicationRuleSourceId = " ";

            AssertUnresolved(RunHeatStateApplyResolver.Resolve(config, 5d, ResolvedDelta(1d)), RunHeatApplicationSummaryErrorCode.InvalidHeatApplicationConfig, 5d);
        }

        [Test]
        public void DefaultSummary_IsUnresolvedAndSafe()
        {
            AssertLegacyDefaultIsSafe(new RunHeatApplicationSummary());
        }

        [Test]
        public void RunOutcomeRecord_ApplicationSummary_RoundTrips_AndLegacyMissingFieldIsSafe()
        {
            string json = JsonUtility.ToJson(new RunOutcomeRecord
            {
                RunHeatApplicationSummary = RunHeatStateApplyResolver.Resolve(ValidConfig(), 8d, ResolvedDelta(3d))
            });
            RunOutcomeRecord loaded = JsonUtility.FromJson<RunOutcomeRecord>(json);
            RunOutcomeRecord legacy = JsonUtility.FromJson<RunOutcomeRecord>("{\"RunId\":\"legacy\"}");

            Assert.That(loaded.RunHeatApplicationSummary, Is.Not.Null);
            Assert.That(loaded.RunHeatApplicationSummary.HeatAfter, Is.EqualTo(11d));
            if (legacy.RunHeatApplicationSummary != null)
            {
                AssertLegacyDefaultIsSafe(legacy.RunHeatApplicationSummary);
            }
        }

        private static void AssertLegacyDefaultIsSafe(RunHeatApplicationSummary summary)
        {
            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)RunHeatApplicationSummaryErrorCode.LegacyDefaultUnresolved));
            Assert.That(double.IsNaN(summary.HeatBefore) || double.IsInfinity(summary.HeatBefore), Is.False);
            Assert.That(double.IsNaN(summary.AppliedDelta) || double.IsInfinity(summary.AppliedDelta), Is.False);
            Assert.That(double.IsNaN(summary.HeatAfter) || double.IsInfinity(summary.HeatAfter), Is.False);
            Assert.That(summary.AppliedDelta, Is.EqualTo(0d));
        }

        private static void AssertUnresolved(RunHeatApplicationSummary result, RunHeatApplicationSummaryErrorCode expectedError, double expectedHeat)
        {
            Assert.That(result.RuleResolved, Is.False);
            Assert.That(result.DeterministicErrorCode, Is.EqualTo((int)expectedError));
            Assert.That(result.HeatBefore, Is.EqualTo(expectedHeat));
            Assert.That(result.HeatAfter, Is.EqualTo(expectedHeat));
            Assert.That(result.AppliedDelta, Is.EqualTo(0d));
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

        private static RunHeatDeltaSummary ResolvedDelta(double delta)
        {
            return new RunHeatDeltaSummary { RuleResolved = true, FinalHeatDelta = delta };
        }
    }
}
