#if UNITY_EDITOR
using System;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class LivingDungeonPhaseABoundaryTests
    {
        [TestCase(9.999, CurrentHeatTierResolver.PeaceTierId)]
        [TestCase(10, CurrentHeatTierResolver.NoticeTierId)]
        [TestCase(24.999, CurrentHeatTierResolver.NoticeTierId)]
        [TestCase(25, CurrentHeatTierResolver.ConcernTierId)]
        [TestCase(49, CurrentHeatTierResolver.ConcernTierId)]
        public void ContinuousFractionalMembershipUsesNextLowerThreshold(double heat, string tier)
        {
            var c = PhaseSixA4Tests.Config(); var result = RunHeatBoundaryCalculation.ApplyStage(c, heat, 0);
            Assert.That(result.Accepted, Is.True); Assert.That(result.After, Is.EqualTo(heat)); Assert.That(result.TierAfter, Is.EqualTo(tier));
            Assert.That(c.HeatPeaceMinimum, Is.Zero);
        }
        [TestCase(9.999, 5, 10)] [TestCase(24.999, 5, 25)]
        public void RestrictedHeatingStopsAtImmediateRepresentablePredecessor(double heat, double delta, double next)
        {
            var c = PhaseSixA4Tests.Config(); var result = RunHeatBoundaryCalculation.ApplyStage(c, heat, delta, heat);
            Assert.That(result.Accepted, Is.True); Assert.That(result.After, Is.LessThan(next));
            Assert.That(BitConverter.DoubleToInt64Bits(result.After)+1, Is.EqualTo(BitConverter.DoubleToInt64Bits(next)));
            Assert.That(result.TierAfter, Is.EqualTo(result.TierBefore)); Assert.That(result.DiscardedDelta, Is.GreaterThan(0));
        }
        [TestCase(10.001, -5, 10)] [TestCase(25.001, -5, 25)] [TestCase(48.5, 5, 49)]
        public void RestrictedStagesRespectInclusiveLowerAndLastMaximum(double heat, double delta, double expected)
        {
            var result = RunHeatBoundaryCalculation.ApplyStage(PhaseSixA4Tests.Config(), heat, delta, heat);
            Assert.That(result.After, Is.EqualTo(expected)); Assert.That(result.DiscardedDelta, Is.Not.Zero);
        }
        [Test]
        public void StageOrderingDiscardsFirstExcessBeforeSecondCooling()
        {
            var c = PhaseSixA4Tests.Config(); var result = RunHeatBoundaryCalculation.ApplyStages(c, 9.5, 4, -1, 9.5);
            Assert.That(result.Accepted, Is.True); Assert.That(result.First.DiscardedDelta, Is.GreaterThan(3));
            Assert.That(result.Second.Before, Is.EqualTo(result.First.After));
            Assert.That(result.Second.After, Is.EqualTo(result.First.After-1));
            Assert.That(result.Second.After, Is.Not.EqualTo(9.5+4-1));
        }
        [TestCase(9.75, 3, -2)] [TestCase(24.75, 3, -1)] [TestCase(1, -8, -1)] [TestCase(48, 9, -3)]
        public void ForegroundStagesMatchExistingRunHeatAndSecondCooling(double heat, double first, double second)
        {
            var c = PhaseSixA4Tests.Config(); var old = RunHeatStateApplyResolver.Resolve(c, heat,
                new RunHeatDeltaSummary { RuleResolved = true, FinalHeatDelta = first });
            var cooling = new HeatSystem().ApplyEvent(new HeatEventInput(0, old.HeatAfter, second));
            var result = RunHeatBoundaryCalculation.ApplyStages(c, heat, first, second);
            Assert.That(result.First.After, Is.EqualTo(old.HeatAfter)); Assert.That(result.Second.After, Is.EqualTo(cooling.NewHeat));
        }
        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-1)] [TestCase(50)]
        public void InvalidHeatRejectedWithoutMutation(double heat)
        {
            var c = PhaseSixA4Tests.Config(); string before = JsonUtility.ToJson(c);
            Assert.That(RunHeatBoundaryCalculation.ApplyStage(c, heat, 1).Accepted, Is.False);
            Assert.That(JsonUtility.ToJson(c), Is.EqualTo(before));
        }
        [Test]
        public void InvalidConfigurationDeltaCapturedTierAndOverflowHaveStableReasons()
        {
            var c = PhaseSixA4Tests.Config(); c.HeatNoticeMinimum = c.HeatPeaceMaximum;
            Assert.That(RunHeatBoundaryCalculation.ApplyStage(c, 1, 1).Reason, Is.EqualTo(RunHeatBoundaryReason.InvalidConfiguration));
            c = PhaseSixA4Tests.Config();
            Assert.That(RunHeatBoundaryCalculation.ApplyStage(c, 1, double.NaN).Reason, Is.EqualTo(RunHeatBoundaryReason.InvalidDelta));
            Assert.That(RunHeatBoundaryCalculation.ApplyStage(c, 11, 1, 1).Reason, Is.EqualTo(RunHeatBoundaryReason.InvalidCapturedTier));
            c.HeatConcernMaximum = double.MaxValue;
            Assert.That(RunHeatBoundaryCalculation.ApplyStage(c, double.MaxValue, double.MaxValue).Reason, Is.EqualTo(RunHeatBoundaryReason.Overflow));
        }
        [Test]
        public void HistoricalManaUsesCapturedCanonicalRateAtSeveralTimesAndCapacityCrossing()
        {
            var f = DetachedCanonicalWriteAuthorityTests.Fixture.Create(null);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)));
            f.Runtime.lastSavedUtcUnix=1000; f.Runtime.structureRuntime.ManaReserve=990.25;
            var config = PhaseFourTestSupport.PassiveMana(f.Profile.Canonical);
            var online = new CanonicalPassiveManaService(config,f.Economy,new FormulaEngine(),f.Profile.Canonical.Spatial,10);
            var service = new CanonicalOfflinePassiveManaService(config,online); string before=JsonUtility.ToJson(f.Runtime);
            var interval=service.CaptureHistoricalInterval(f.Runtime,f.Configuration,100000);
            foreach (long timestamp in new[] { 1000L,1001L,1100L,4600L,100000L }) {
                var result=service.EvaluateHistorical(interval,timestamp);
                Assert.That(result.CalculationAccepted,Is.True); Assert.That(result.PersistenceRequired,Is.False);
                Assert.That(result.Persisted,Is.False); Assert.That(result.WalletAfter,Is.EqualTo(Math.Min(1000,990.25+27d*(timestamp-1000)/3600)));
            }
            var grant=service.Resolve(f.Runtime,f.Configuration,100000);
            Assert.That(grant.PersistenceRequired,Is.True); Assert.That(grant.ActualAwardedMana,Is.EqualTo(9.75));
            Assert.That(JsonUtility.ToJson(f.Runtime),Is.EqualTo(before));
            f.Runtime.structureRuntime.ManaReserve=0; f.Configuration.HeatPeaceMinimum=-15;
            interval.InspectRate().ManaPerHour=0;
            Assert.That(service.EvaluateHistorical(interval,4600).WalletAfter,Is.EqualTo(1000));
            Assert.Throws<ArgumentException>(()=>service.EvaluateHistorical(interval,999));
            Assert.Throws<ArgumentException>(()=>service.EvaluateHistorical(interval,100001));
            Assert.Throws<ArgumentException>(()=>service.CaptureHistoricalInterval(f.Runtime,f.Configuration,999));
        }
    }
}
#endif
