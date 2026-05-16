using System.Collections.Generic;
using DungeonBuilder.M0.Economy;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class FormulaEngineTests
    {
        [Test]
        public void Evaluate_AppliesBucketsInGlobalOrder()
        {
            IFormulaEngine engine = new FormulaEngine();
            var modifiers = new List<FormulaModifier>
            {
                new FormulaModifier("heat_add", ModifierBucket.Heat, ModifierType.AdditiveFlat, 10),
                new FormulaModifier("research_pct", ModifierBucket.Research, ModifierType.AdditivePercent, 0.5),
                new FormulaModifier("event_mul", ModifierBucket.EventOrSeason, ModifierType.MultiplicativePercent, 0.2),
                new FormulaModifier("cap", ModifierBucket.ClampAndSoftCap, ModifierType.Clamp, 0, 1000),
                new FormulaModifier("round", ModifierBucket.Rounding, ModifierType.AdditiveFlat, 0),
            };

            FormulaResult result = engine.Evaluate(new FormulaInput(100, modifiers));

            Assert.AreEqual(198, result.Value);
        }

        [Test]
        public void Evaluate_AppliesClampBeforeRounding()
        {
            IFormulaEngine engine = new FormulaEngine();
            var modifiers = new List<FormulaModifier>
            {
                new FormulaModifier("heat_pct", ModifierBucket.Heat, ModifierType.AdditivePercent, 0.333),
                new FormulaModifier("clamp", ModifierBucket.ClampAndSoftCap, ModifierType.Clamp, 0, 120),
            };

            FormulaResult result = engine.Evaluate(new FormulaInput(100, modifiers));

            Assert.AreEqual(120, result.Value);
        }

        [Test]
        public void Evaluate_MixedStackWithSoftCapClampAndRounding_UsesContractOrder()
        {
            IFormulaEngine engine = new FormulaEngine();
            var modifiers = new List<FormulaModifier>
            {
                new FormulaModifier("base_plus", ModifierBucket.Base, ModifierType.AdditiveFlat, 2),
                new FormulaModifier("heat_pct", ModifierBucket.Heat, ModifierType.AdditivePercent, 0.25),
                new FormulaModifier("research_plus", ModifierBucket.Research, ModifierType.AdditiveFlat, 3),
                new FormulaModifier("event_mul", ModifierBucket.EventOrSeason, ModifierType.MultiplicativePercent, 0.5),
                new FormulaModifier("softcap", ModifierBucket.ClampAndSoftCap, ModifierType.SoftCap, 20, 5),
                new FormulaModifier("clamp", ModifierBucket.ClampAndSoftCap, ModifierType.Clamp, 0, 22),
                new FormulaModifier("round_anchor", ModifierBucket.Rounding, ModifierType.AdditiveFlat, 0)
            };

            FormulaResult result = engine.Evaluate(new FormulaInput(10, modifiers));

            // Manual contract order:
            // Base: 10 + 2 = 12
            // Heat: 12 * 1.25 = 15
            // Research: 15 + 3 = 18
            // Event: 18 * 1.5 = 27
            // Clamp/SoftCap (same bucket, in listed order):
            //   SoftCap(start=20,slope=5): 20 + (7 / (1 + 7/5)) = 22.91666...
            //   Clamp [0,22] => 22
            // Rounding bucket => 22
            Assert.AreEqual(22d, result.Value);
        }

        [Test]
        public void Evaluate_WithNegativeBaseAndClamp_RespectsLowerBound()
        {
            IFormulaEngine engine = new FormulaEngine();
            var modifiers = new List<FormulaModifier>
            {
                new FormulaModifier("flat_penalty", ModifierBucket.Heat, ModifierType.AdditiveFlat, -10),
                new FormulaModifier("clamp", ModifierBucket.ClampAndSoftCap, ModifierType.Clamp, 0, 9999),
            };

            FormulaResult result = engine.Evaluate(new FormulaInput(-5, modifiers));

            Assert.AreEqual(0d, result.Value);
        }

        [Test]
        public void Evaluate_WithTinyAndHugeValues_RemainsStableAndRounded()
        {
            IFormulaEngine engine = new FormulaEngine();
            var modifiers = new List<FormulaModifier>
            {
                new FormulaModifier("tiny_pct", ModifierBucket.Heat, ModifierType.AdditivePercent, 0.0000001),
                new FormulaModifier("huge_mul", ModifierBucket.EventOrSeason, ModifierType.MultiplicativePercent, 99999),
                new FormulaModifier("upper_bound", ModifierBucket.ClampAndSoftCap, ModifierType.Clamp, 0, 1000000000),
                new FormulaModifier("round_anchor", ModifierBucket.Rounding, ModifierType.AdditiveFlat, 0),
            };

            FormulaResult result = engine.Evaluate(new FormulaInput(0.0001, modifiers));

            Assert.AreEqual(10d, result.Value);
        }
    }
}
