using System;

namespace DungeonBuilder.M0.Economy
{
    public readonly struct HeatEventInput
    {
        public long TickIndex { get; }
        public double CurrentHeat { get; }
        public double Delta { get; }

        public HeatEventInput(long tickIndex, double currentHeat, double delta)
        {
            TickIndex = tickIndex;
            CurrentHeat = currentHeat;
            Delta = delta;
        }
    }

    public readonly struct HeatDecayInput
    {
        public long TickIndex { get; }
        public double CurrentHeat { get; }
        public double DecayPerTick { get; }
        public int ElapsedTicks { get; }

        public HeatDecayInput(long tickIndex, double currentHeat, double decayPerTick, int elapsedTicks)
        {
            TickIndex = tickIndex;
            CurrentHeat = currentHeat;
            DecayPerTick = decayPerTick;
            ElapsedTicks = elapsedTicks;
        }
    }

    public readonly struct HeatResult
    {
        public long TickIndex { get; }
        public double PreviousHeat { get; }
        public double NewHeat { get; }
        public bool ClampedToMinimum { get; }

        public HeatResult(long tickIndex, double previousHeat, double newHeat, bool clampedToMinimum)
        {
            TickIndex = tickIndex;
            PreviousHeat = previousHeat;
            NewHeat = newHeat;
            ClampedToMinimum = clampedToMinimum;
        }
    }

    public interface IHeatSystem
    {
        HeatResult ApplyEvent(HeatEventInput input);
        HeatResult Decay(HeatDecayInput input);
    }

    public sealed class HeatSystem : IHeatSystem
    {
        public HeatResult ApplyEvent(HeatEventInput input)
        {
            double previousHeat = Math.Max(0d, input.CurrentHeat);
            double rawHeat = previousHeat + input.Delta;
            double newHeat = Math.Max(0d, rawHeat);

            return new HeatResult(
                input.TickIndex,
                previousHeat,
                newHeat,
                rawHeat < 0d
            );
        }

        public HeatResult Decay(HeatDecayInput input)
        {
            double previousHeat = Math.Max(0d, input.CurrentHeat);
            int ticks = Math.Max(0, input.ElapsedTicks);
            double decayPerTick = Math.Max(0d, input.DecayPerTick);

            double totalDecay = decayPerTick * ticks;
            double rawHeat = previousHeat - totalDecay;
            double newHeat = Math.Max(0d, rawHeat);

            return new HeatResult(
                input.TickIndex,
                previousHeat,
                newHeat,
                rawHeat < 0d
            );
        }
    }
}
