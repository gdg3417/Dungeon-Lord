using System.Collections.Generic;
using DungeonBuilder.M0.Economy;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class SimulationDeterminismTests
    {
        [Test]
        public void FixedTimeline_ReplaysIdenticallyAcrossRuns()
        {
            var timeline = BuildTimeline(300);

            SimulationSnapshot[] runA = RunTimeline(timeline);
            SimulationSnapshot[] runB = RunTimeline(timeline);

            Assert.AreEqual(runA.Length, runB.Length);
            for (int i = 0; i < runA.Length; i++)
            {
                Assert.AreEqual(runA[i].TickIndex, runB[i].TickIndex, $"Tick mismatch at index {i}");
                Assert.AreEqual(runA[i].GeneratedMana, runB[i].GeneratedMana, 1e-9, $"Generated mana mismatch at tick {runA[i].TickIndex}");
                Assert.AreEqual(runA[i].CurrentMana, runB[i].CurrentMana, 1e-9, $"Current mana mismatch at tick {runA[i].TickIndex}");
                Assert.AreEqual(runA[i].CurrentHeat, runB[i].CurrentHeat, 1e-9, $"Heat mismatch at tick {runA[i].TickIndex}");
            }
        }

        private static SimulationSnapshot[] RunTimeline(IReadOnlyList<TickInput> timeline)
        {
            IManaSystem manaSystem = new ManaSystem(new FormulaEngine());
            IHeatSystem heatSystem = new HeatSystem();

            double mana = 50d;
            double heat = 0d;
            var snapshots = new SimulationSnapshot[timeline.Count];

            for (int i = 0; i < timeline.Count; i++)
            {
                TickInput input = timeline[i];

                HeatResult eventHeat = heatSystem.ApplyEvent(new HeatEventInput(input.TickIndex, heat, input.HeatDelta));
                HeatResult decayedHeat = heatSystem.Decay(new HeatDecayInput(input.TickIndex, eventHeat.NewHeat, input.HeatDecayPerTick, input.ElapsedTicks));

                var modifiers = new List<FormulaModifier>
                {
                    new FormulaModifier("heat_influence", ModifierBucket.Heat, ModifierType.AdditivePercent, decayedHeat.NewHeat * 0.001),
                    new FormulaModifier("research_flat", ModifierBucket.Research, ModifierType.AdditiveFlat, input.ResearchFlatBonus)
                };

                ManaTickResult manaResult = manaSystem.Tick(new ManaTickInput(
                    tickIndex: input.TickIndex,
                    currentMana: mana,
                    manaCapacity: 500d,
                    baseManaPerTick: input.BaseManaPerTick,
                    modifiers: modifiers
                ));

                mana = manaResult.NewMana;
                heat = decayedHeat.NewHeat;

                snapshots[i] = new SimulationSnapshot(
                    input.TickIndex,
                    manaResult.GeneratedMana,
                    mana,
                    heat
                );
            }

            return snapshots;
        }

        private static TickInput[] BuildTimeline(int tickCount)
        {
            var timeline = new TickInput[tickCount];

            for (int i = 0; i < tickCount; i++)
            {
                long tickIndex = i + 1;
                double baseMana = 8d + ((i % 5) * 0.5d);
                double heatDelta = (i % 7 == 0) ? 1.5d : ((i % 11 == 0) ? -0.8d : 0.2d);
                double heatDecay = 0.15d + ((i % 3) * 0.05d);
                double researchBonus = (i % 13 == 0) ? 1d : 0d;

                timeline[i] = new TickInput(
                    tickIndex,
                    baseMana,
                    heatDelta,
                    heatDecay,
                    elapsedTicks: 1,
                    researchFlatBonus: researchBonus
                );
            }

            return timeline;
        }

        private readonly struct TickInput
        {
            public long TickIndex { get; }
            public double BaseManaPerTick { get; }
            public double HeatDelta { get; }
            public double HeatDecayPerTick { get; }
            public int ElapsedTicks { get; }
            public double ResearchFlatBonus { get; }

            public TickInput(long tickIndex, double baseManaPerTick, double heatDelta, double heatDecayPerTick, int elapsedTicks, double researchFlatBonus)
            {
                TickIndex = tickIndex;
                BaseManaPerTick = baseManaPerTick;
                HeatDelta = heatDelta;
                HeatDecayPerTick = heatDecayPerTick;
                ElapsedTicks = elapsedTicks;
                ResearchFlatBonus = researchFlatBonus;
            }
        }

        private readonly struct SimulationSnapshot
        {
            public long TickIndex { get; }
            public double GeneratedMana { get; }
            public double CurrentMana { get; }
            public double CurrentHeat { get; }

            public SimulationSnapshot(long tickIndex, double generatedMana, double currentMana, double currentHeat)
            {
                TickIndex = tickIndex;
                GeneratedMana = generatedMana;
                CurrentMana = currentMana;
                CurrentHeat = currentHeat;
            }
        }
    }
}
