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
    }
}
