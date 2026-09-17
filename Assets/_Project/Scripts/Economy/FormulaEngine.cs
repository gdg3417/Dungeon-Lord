using System;
using System.Collections.Generic;

namespace DungeonBuilder.M0.Economy
{
    public interface IFormulaEngine
    {
        FormulaResult Evaluate(FormulaInput input);
    }

    public sealed class FormulaEngine : IFormulaEngine
    {
        public FormulaResult Evaluate(FormulaInput input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            double running = input.BaseValue;
            IReadOnlyList<FormulaModifier> modifiers = input.Modifiers;
            double afterBase = running;
            double afterHeat = running;
            double afterResearch = running;
            double afterEventOrSeason = running;
            double afterClampAndSoftCap = running;

            for (int bucketIndex = 0; bucketIndex <= (int)ModifierBucket.Rounding; bucketIndex++)
            {
                ModifierBucket bucket = (ModifierBucket)bucketIndex;
                ApplyBucket(ref running, modifiers, bucket);
                switch (bucket)
                {
                    case ModifierBucket.Base:
                        afterBase = running;
                        break;
                    case ModifierBucket.Heat:
                        afterHeat = running;
                        break;
                    case ModifierBucket.Research:
                        afterResearch = running;
                        break;
                    case ModifierBucket.EventOrSeason:
                        afterEventOrSeason = running;
                        break;
                    case ModifierBucket.ClampAndSoftCap:
                        afterClampAndSoftCap = running;
                        break;
                }
            }

            return new FormulaResult(running, afterBase, afterHeat, afterResearch,
                afterEventOrSeason, afterClampAndSoftCap);
        }

        private static void ApplyBucket(ref double running, IReadOnlyList<FormulaModifier> modifiers, ModifierBucket bucket)
        {
            double additiveFlat = 0d;
            double additivePercent = 0d;
            double multiplicativePercent = 1d;

            for (int i = 0; i < modifiers.Count; i++)
            {
                FormulaModifier m = modifiers[i];
                if (m.bucket != bucket)
                {
                    continue;
                }

                switch (m.type)
                {
                    case ModifierType.AdditiveFlat:
                        additiveFlat += m.value;
                        break;
                    case ModifierType.AdditivePercent:
                        additivePercent += m.value;
                        break;
                    case ModifierType.MultiplicativePercent:
                        multiplicativePercent *= (1d + m.value);
                        break;
                    case ModifierType.Clamp:
                        running = Math.Max(m.value, Math.Min(m.secondaryValue, running));
                        break;
                    case ModifierType.SoftCap:
                        running = ApplySoftCap(running, m.value, m.secondaryValue);
                        break;
                }
            }

            running += additiveFlat;
            running *= (1d + additivePercent);
            running *= multiplicativePercent;

            if (bucket == ModifierBucket.Rounding)
            {
                running = Math.Round(running, MidpointRounding.AwayFromZero);
            }
        }

        private static double ApplySoftCap(double value, double capStart, double slope)
        {
            if (slope <= 0d || value <= capStart)
            {
                return value;
            }

            double overflow = value - capStart;
            return capStart + (overflow / (1d + (overflow / slope)));
        }
    }
}
