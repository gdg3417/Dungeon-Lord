using System;
using System.Collections.Generic;

namespace DungeonBuilder.M0.Economy
{
    public enum ModifierBucket
    {
        Base = 0,
        Heat = 1,
        Research = 2,
        EventOrSeason = 3,
        ClampAndSoftCap = 4,
        Rounding = 5
    }

    public enum ModifierType
    {
        AdditiveFlat = 0,
        AdditivePercent = 1,
        MultiplicativePercent = 2,
        Clamp = 3,
        SoftCap = 4
    }

    [Serializable]
    public struct FormulaModifier
    {
        public string id;
        public ModifierBucket bucket;
        public ModifierType type;
        public double value;
        public double secondaryValue;

        public FormulaModifier(string id, ModifierBucket bucket, ModifierType type, double value, double secondaryValue = 0d)
        {
            this.id = id;
            this.bucket = bucket;
            this.type = type;
            this.value = value;
            this.secondaryValue = secondaryValue;
        }
    }

    public sealed class FormulaInput
    {
        public double BaseValue { get; }
        public IReadOnlyList<FormulaModifier> Modifiers { get; }

        public FormulaInput(double baseValue, IReadOnlyList<FormulaModifier> modifiers)
        {
            BaseValue = baseValue;
            Modifiers = modifiers ?? Array.Empty<FormulaModifier>();
        }
    }

    public readonly struct FormulaResult
    {
        public double Value { get; }

        public FormulaResult(double value)
        {
            Value = value;
        }
    }
}
