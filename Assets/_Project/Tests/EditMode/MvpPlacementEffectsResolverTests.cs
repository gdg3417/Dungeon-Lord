using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpPlacementEffectsResolverTests
    {
        [Test]
        public void Resolve_NoPlacements_ReturnsSafeZeroDefaults()
        {
            MvpPlacementEffectsSummary summary = MvpPlacementEffectsResolver.Resolve(null, Config());

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.PathCapacity, Is.Zero);
            Assert.That(summary.Danger, Is.Zero);
            Assert.That(summary.ManaPressure, Is.Zero);
            Assert.That(summary.HeatPressure, Is.Zero);
            Assert.That(summary.LootBonus, Is.Zero);
            Assert.That(summary.Attraction, Is.Zero);
            Assert.That(summary.ContributingOptionIds, Is.Empty);
        }

        [Test]
        public void Resolve_BasicRoom_ExposesPathCapacityContribution()
        {
            MvpPlacementEffectsSummary summary = MvpPlacementEffectsResolver.Resolve(State(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId), Config());

            Assert.That(summary.PathCapacity, Is.EqualTo(2));
            Assert.That(summary.Danger, Is.Zero);
            Assert.That(summary.ContributingOptionIds, Is.EqualTo(new[] { MvpDungeonPlacementIds.BasicRoomOptionId }));
        }

        [Test]
        public void Resolve_Skeleton_ExposesDangerAndManaPressureContribution()
        {
            MvpPlacementEffectsSummary summary = MvpPlacementEffectsResolver.Resolve(State(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId), Config());

            Assert.That(summary.Danger, Is.EqualTo(3));
            Assert.That(summary.ManaPressure, Is.EqualTo(2));
        }

        [Test]
        public void Resolve_SpikeTrap_ExposesDangerHeatAndPathPressureContribution()
        {
            MvpPlacementEffectsSummary summary = MvpPlacementEffectsResolver.Resolve(State(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId), Config());

            Assert.That(summary.Danger, Is.EqualTo(2));
            Assert.That(summary.HeatPressure, Is.EqualTo(1));
        }

        [Test]
        public void Resolve_BasicLootNode_ExposesLootAndAttractionContribution()
        {
            MvpPlacementEffectsSummary summary = MvpPlacementEffectsResolver.Resolve(State(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId), Config());

            Assert.That(summary.LootBonus, Is.EqualTo(4));
            Assert.That(summary.Attraction, Is.EqualTo(2));
        }

        [Test]
        public void Resolve_AllStarterPlacements_CombinesContributionsDeterministically()
        {
            MvpDungeonPlacementState state = AllStarterState();
            RunSimulationConfig config = Config();

            MvpPlacementEffectsSummary first = MvpPlacementEffectsResolver.Resolve(state, config);
            MvpPlacementEffectsSummary second = MvpPlacementEffectsResolver.Resolve(state, config);

            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
            Assert.That(first.PathCapacity, Is.EqualTo(2));
            Assert.That(first.Danger, Is.EqualTo(5));
            Assert.That(first.ManaPressure, Is.EqualTo(2));
            Assert.That(first.HeatPressure, Is.EqualTo(1));
            Assert.That(first.LootBonus, Is.EqualTo(4));
            Assert.That(first.Attraction, Is.EqualTo(2));
            Assert.That(first.ContributingOptionIds, Is.EqualTo(new[]
            {
                MvpDungeonPlacementIds.BasicRoomOptionId,
                MvpDungeonPlacementIds.SkeletonOptionId,
                MvpDungeonPlacementIds.SpikeTrapOptionId,
                MvpDungeonPlacementIds.BasicLootNodeOptionId
            }));
        }

        [Test]
        public void Resolve_LegacyStateWithoutConfig_ReturnsSafeZeroDefaults()
        {
            MvpPlacementEffectsSummary summary = MvpPlacementEffectsResolver.Resolve(AllStarterState(), new RunSimulationConfig());

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.PathCapacity, Is.Zero);
            Assert.That(summary.Danger, Is.Zero);
            Assert.That(summary.ContributingOptionIds, Is.Empty);
        }

        private static MvpDungeonPlacementState State(string categoryId, string optionId)
        {
            return new MvpDungeonPlacementState
            {
                Entries = new System.Collections.Generic.List<MvpDungeonPlacementEntry>
                {
                    new MvpDungeonPlacementEntry(categoryId, optionId, 1)
                }
            };
        }

        private static MvpDungeonPlacementState AllStarterState()
        {
            return new MvpDungeonPlacementState
            {
                Entries = new System.Collections.Generic.List<MvpDungeonPlacementEntry>
                {
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId, 4),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId, 3),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 2),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId, 1)
                }
            };
        }

        private static RunSimulationConfig Config()
        {
            return new RunSimulationConfig
            {
                MvpPlacementEffectsRuleSourceId = "test.mvp_placement_effects",
                MvpPlacementEffects = new[]
                {
                    new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.RoomCategoryId, OptionId = MvpDungeonPlacementIds.BasicRoomOptionId, PathCapacity = 2, ExplanationKey = "effect.room" },
                    new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.MonsterCategoryId, OptionId = MvpDungeonPlacementIds.SkeletonOptionId, Danger = 3, ManaPressure = 2, ExplanationKey = "effect.monster" },
                    new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.TrapCategoryId, OptionId = MvpDungeonPlacementIds.SpikeTrapOptionId, Danger = 2, HeatPressure = 1, ExplanationKey = "effect.trap" },
                    new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.LootNodeCategoryId, OptionId = MvpDungeonPlacementIds.BasicLootNodeOptionId, LootBonus = 4, Attraction = 2, ExplanationKey = "effect.loot" }
                }
            };
        }
    }
}
