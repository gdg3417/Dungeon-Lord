using DungeonBuilder.M0.Economy;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class HeatSystemTests
    {
        [Test]
        public void ApplyEvent_AddsAndSubtractsDeterministically()
        {
            IHeatSystem heat = new HeatSystem();

            HeatResult increase = heat.ApplyEvent(new HeatEventInput(
                tickIndex: 10,
                currentHeat: 5,
                delta: 2.5
            ));

            HeatResult decrease = heat.ApplyEvent(new HeatEventInput(
                tickIndex: 11,
                currentHeat: increase.NewHeat,
                delta: -1.5
            ));

            Assert.AreEqual(10, increase.TickIndex);
            Assert.AreEqual(5d, increase.PreviousHeat);
            Assert.AreEqual(7.5d, increase.NewHeat);
            Assert.IsFalse(increase.ClampedToMinimum);

            Assert.AreEqual(11, decrease.TickIndex);
            Assert.AreEqual(7.5d, decrease.PreviousHeat);
            Assert.AreEqual(6d, decrease.NewHeat);
            Assert.IsFalse(decrease.ClampedToMinimum);
        }

        [Test]
        public void Decay_AppliesElapsedTicksDeterministically()
        {
            IHeatSystem heat = new HeatSystem();

            HeatResult result = heat.Decay(new HeatDecayInput(
                tickIndex: 25,
                currentHeat: 12,
                decayPerTick: 0.75,
                elapsedTicks: 8
            ));

            Assert.AreEqual(25, result.TickIndex);
            Assert.AreEqual(12d, result.PreviousHeat);
            Assert.AreEqual(6d, result.NewHeat);
            Assert.IsFalse(result.ClampedToMinimum);
        }

        [Test]
        public void ApplyEvent_AndDecay_NeverGoBelowZero()
        {
            IHeatSystem heat = new HeatSystem();

            HeatResult eventResult = heat.ApplyEvent(new HeatEventInput(
                tickIndex: 1,
                currentHeat: 2,
                delta: -10
            ));

            HeatResult decayResult = heat.Decay(new HeatDecayInput(
                tickIndex: 2,
                currentHeat: 2,
                decayPerTick: 1.5,
                elapsedTicks: 3
            ));

            Assert.AreEqual(0d, eventResult.NewHeat);
            Assert.IsTrue(eventResult.ClampedToMinimum);

            Assert.AreEqual(0d, decayResult.NewHeat);
            Assert.IsTrue(decayResult.ClampedToMinimum);
        }

        [Test]
        public void SameInputSequences_ProduceSameOutputs()
        {
            IHeatSystem heatA = new HeatSystem();
            IHeatSystem heatB = new HeatSystem();

            HeatResult a1 = heatA.ApplyEvent(new HeatEventInput(1, 3, 2));
            HeatResult a2 = heatA.Decay(new HeatDecayInput(2, a1.NewHeat, 0.5, 4));

            HeatResult b1 = heatB.ApplyEvent(new HeatEventInput(1, 3, 2));
            HeatResult b2 = heatB.Decay(new HeatDecayInput(2, b1.NewHeat, 0.5, 4));

            Assert.AreEqual(a1.NewHeat, b1.NewHeat);
            Assert.AreEqual(a1.ClampedToMinimum, b1.ClampedToMinimum);
            Assert.AreEqual(a2.NewHeat, b2.NewHeat);
            Assert.AreEqual(a2.ClampedToMinimum, b2.ClampedToMinimum);
        }
    }
}
