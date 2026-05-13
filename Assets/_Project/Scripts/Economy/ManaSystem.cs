using System;
using System.Collections.Generic;

namespace DungeonBuilder.M0.Economy
{
    public readonly struct ManaTickInput
    {
        public long TickIndex { get; }
        public double CurrentMana { get; }
        public double ManaCapacity { get; }
        public double BaseManaPerTick { get; }
        public IReadOnlyList<FormulaModifier> Modifiers { get; }

        public ManaTickInput(long tickIndex, double currentMana, double manaCapacity, double baseManaPerTick, IReadOnlyList<FormulaModifier> modifiers)
        {
            TickIndex = tickIndex;
            CurrentMana = currentMana;
            ManaCapacity = manaCapacity;
            BaseManaPerTick = baseManaPerTick;
            Modifiers = modifiers ?? Array.Empty<FormulaModifier>();
        }
    }

    public readonly struct ManaTickResult
    {
        public long TickIndex { get; }
        public double PreviousMana { get; }
        public double GeneratedMana { get; }
        public double NewMana { get; }
        public bool ClampedToCapacity { get; }

        public ManaTickResult(long tickIndex, double previousMana, double generatedMana, double newMana, bool clampedToCapacity)
        {
            TickIndex = tickIndex;
            PreviousMana = previousMana;
            GeneratedMana = generatedMana;
            NewMana = newMana;
            ClampedToCapacity = clampedToCapacity;
        }
    }

    public interface IManaSystem
    {
        ManaTickResult Tick(ManaTickInput input);
    }

    public sealed class ManaSystem : IManaSystem
    {
        private readonly IFormulaEngine _formulaEngine;

        public ManaSystem(IFormulaEngine formulaEngine)
        {
            _formulaEngine = formulaEngine ?? throw new ArgumentNullException(nameof(formulaEngine));
        }

        public ManaTickResult Tick(ManaTickInput input)
        {
            double previousMana = Math.Max(0d, input.CurrentMana);
            double capacity = Math.Max(0d, input.ManaCapacity);

            var formulaInput = new FormulaInput(input.BaseManaPerTick, input.Modifiers);
            FormulaResult formulaResult = _formulaEngine.Evaluate(formulaInput);

            double generatedMana = Math.Max(0d, formulaResult.Value);
            double rawNewMana = previousMana + generatedMana;
            double newMana = Math.Min(capacity, rawNewMana);

            return new ManaTickResult(
                input.TickIndex,
                previousMana,
                generatedMana,
                newMana,
                rawNewMana > capacity
            );
        }
    }
}
