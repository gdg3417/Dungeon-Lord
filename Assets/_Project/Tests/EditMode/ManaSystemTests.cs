using System.Collections.Generic;
using DungeonBuilder.M0.Economy;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class ManaSystemTests
    {
        [Test]
        public void Tick_AppliesFormulaAndReturnsDeterministicResult()
        {
            IManaSystem mana = new ManaSystem(new FormulaEngine());
            var modifiers = new List<FormulaModifier>
            {
                new FormulaModifier("heat_bonus", ModifierBucket.Heat, ModifierType.AdditivePercent, 0.1),
                new FormulaModifier("research_bonus", ModifierBucket.Research, ModifierType.AdditiveFlat, 2)
            };

            ManaTickResult result = mana.Tick(new ManaTickInput(
                tickIndex: 42,
                currentMana: 100,
                manaCapacity: 500,
                baseManaPerTick: 20,
                modifiers: modifiers
            ));

            Assert.AreEqual(42, result.TickIndex);
            Assert.AreEqual(100, result.PreviousMana);
            Assert.AreEqual(24, result.GeneratedMana);
            Assert.AreEqual(124, result.NewMana);
            Assert.IsFalse(result.ClampedToCapacity);
        }

        [Test]
        public void Tick_ClampsToCapacity()
        {
            IManaSystem mana = new ManaSystem(new FormulaEngine());

            ManaTickResult result = mana.Tick(new ManaTickInput(
                tickIndex: 5,
                currentMana: 95,
                manaCapacity: 100,
                baseManaPerTick: 20,
                modifiers: null
            ));

            Assert.AreEqual(20, result.GeneratedMana);
            Assert.AreEqual(100, result.NewMana);
            Assert.IsTrue(result.ClampedToCapacity);
        }
    }
}
