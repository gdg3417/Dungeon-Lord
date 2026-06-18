using System.IO;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpPlacementEffectsResolverTests
    {

        [Test]
        public void BootstrapConfig_DefinesEffectsForAllMvpPlacementOptions()
        {
            string path = Path.Combine(Application.dataPath, "_Project/Data/Bootstrap/run_simulation_config.json");
            RunSimulationConfig config = JsonUtility.FromJson<RunSimulationConfig>(File.ReadAllText(path));

            foreach (string optionId in MvpDungeonPlacementIds.OrderedOptionIds)
            {
                Assert.That(System.Array.Exists(config.MvpPlacementEffects, effect => effect != null && effect.OptionId == optionId), Is.True, optionId);
            }
        }

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
        public void Resolve_AllAlternativePlacements_CombinesDistinctContributionsDeterministically()
        {
            MvpDungeonPlacementState state = new MvpDungeonPlacementState
            {
                Entries = new System.Collections.Generic.List<MvpDungeonPlacementEntry>
                {
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId, 1),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId, 2),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SnareTrapOptionId, 3),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId, 4)
                }
            };

            MvpPlacementEffectsSummary first = MvpPlacementEffectsResolver.Resolve(state, Config());
            MvpPlacementEffectsSummary second = MvpPlacementEffectsResolver.Resolve(state, Config());

            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
            Assert.That(first.PathCapacity, Is.EqualTo(1));
            Assert.That(first.Danger, Is.EqualTo(3));
            Assert.That(first.ManaPressure, Is.EqualTo(1));
            Assert.That(first.HeatPressure, Is.Zero);
            Assert.That(first.LootBonus, Is.EqualTo(4));
            Assert.That(first.Attraction, Is.EqualTo(2));
            Assert.That(first.ContributingOptionIds, Is.EqualTo(new[]
            {
                MvpDungeonPlacementIds.NarrowHallOptionId,
                MvpDungeonPlacementIds.GoblinOptionId,
                MvpDungeonPlacementIds.SnareTrapOptionId,
                MvpDungeonPlacementIds.HiddenCacheOptionId
            }));
        }


        [Test]
        public void ResolveForSave_PersistedRoomSlotAssignmentsWinOverLegacyGlobalPlacements()
        {
            SaveData save = SaveWithLegacyStarterPlacements();
            save.mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection
            {
                Rooms = new System.Collections.Generic.List<MvpRoomSlotAssignmentState>
                {
                    new MvpRoomSlotAssignmentState
                    {
                        FloorIndex = 0,
                        RoomIndex = 0,
                        RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                        MonsterOptionIds = new[] { MvpDungeonPlacementIds.GoblinOptionId },
                        TrapOptionIds = new[] { MvpDungeonPlacementIds.SnareTrapOptionId },
                        LootNodeOptionIds = new[] { MvpDungeonPlacementIds.HiddenCacheOptionId }
                    }
                }
            };

            MvpPlacementEffectsSummary first = MvpPlacementEffectsResolver.ResolveForSave(save, Config());
            MvpPlacementEffectsSummary second = MvpPlacementEffectsResolver.ResolveForSave(save, Config());

            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
            Assert.That(first.PathCapacity, Is.EqualTo(2));
            Assert.That(first.Danger, Is.EqualTo(3));
            Assert.That(first.ManaPressure, Is.EqualTo(1));
            Assert.That(first.HeatPressure, Is.Zero);
            Assert.That(first.LootBonus, Is.EqualTo(4));
            Assert.That(first.Attraction, Is.EqualTo(2));
            Assert.That(first.ContributingOptionIds, Is.EqualTo(new[]
            {
                MvpDungeonPlacementIds.BasicRoomOptionId,
                MvpDungeonPlacementIds.GoblinOptionId,
                MvpDungeonPlacementIds.SnareTrapOptionId,
                MvpDungeonPlacementIds.HiddenCacheOptionId
            }));
            Assert.That(first.ContributingOptionIds, Does.Not.Contain(MvpDungeonPlacementIds.SkeletonOptionId));
            Assert.That(first.ContributingOptionIds, Does.Not.Contain(MvpDungeonPlacementIds.SpikeTrapOptionId));
            Assert.That(first.ContributingOptionIds, Does.Not.Contain(MvpDungeonPlacementIds.BasicLootNodeOptionId));
        }

        [Test]
        public void ResolveForSave_RoomTwoLootContributesWhenRoomOneCannotHoldLoot()
        {
            SaveData save = new SaveData
            {
                mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection
                {
                    Rooms = new System.Collections.Generic.List<MvpRoomSlotAssignmentState>
                    {
                        new MvpRoomSlotAssignmentState
                        {
                            FloorIndex = 0,
                            RoomIndex = 0,
                            RoomOptionId = MvpDungeonPlacementIds.NarrowHallOptionId,
                            LootNodeOptionIds = new[] { MvpDungeonPlacementIds.HiddenCacheOptionId }
                        },
                        new MvpRoomSlotAssignmentState
                        {
                            FloorIndex = 0,
                            RoomIndex = 1,
                            RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                            LootNodeOptionIds = new[] { MvpDungeonPlacementIds.BasicLootNodeOptionId }
                        }
                    }
                }
            };

            MvpPlacementEffectsSummary effects = MvpPlacementEffectsResolver.ResolveForSave(save, Config());

            Assert.That(effects.ContributingOptionIds, Is.EqualTo(new[]
            {
                MvpDungeonPlacementIds.NarrowHallOptionId,
                MvpDungeonPlacementIds.BasicLootNodeOptionId
            }));
            Assert.That(effects.LootBonus, Is.EqualTo(4));
            Assert.That(effects.Attraction, Is.EqualTo(2));
        }

        [Test]
        public void ResolveForSave_LegacyGlobalPlacementsRemainFallbackWhenPersistedRoomSlotsAreAbsent()
        {
            SaveData save = SaveWithLegacyStarterPlacements();

            MvpPlacementEffectsSummary effects = MvpPlacementEffectsResolver.ResolveForSave(save, Config());

            Assert.That(effects.PathCapacity, Is.EqualTo(2));
            Assert.That(effects.Danger, Is.EqualTo(5));
            Assert.That(effects.ManaPressure, Is.EqualTo(2));
            Assert.That(effects.HeatPressure, Is.EqualTo(1));
            Assert.That(effects.LootBonus, Is.EqualTo(4));
            Assert.That(effects.Attraction, Is.EqualTo(2));
            Assert.That(effects.ContributingOptionIds, Is.EqualTo(new[]
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

        private static SaveData SaveWithLegacyStarterPlacements()
        {
            return new SaveData { mvpDungeonPlacements = AllStarterState() };
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
                MvpRoomSlotCapacities = new[]
                {
                    new MvpRoomSlotCapacityConfig { RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId, MonsterCapacity = 1, TrapCapacity = 1, LootCapacity = 1 },
                    new MvpRoomSlotCapacityConfig { RoomOptionId = MvpDungeonPlacementIds.NarrowHallOptionId, MonsterCapacity = 1, TrapCapacity = 1, LootCapacity = 0 }
                },
                MvpPlacementEffects = new[]
                {
                    new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.RoomCategoryId, OptionId = MvpDungeonPlacementIds.BasicRoomOptionId, PathCapacity = 2, ExplanationKey = "effect.room" },
                    new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.RoomCategoryId, OptionId = MvpDungeonPlacementIds.NarrowHallOptionId, PathCapacity = 1, ExplanationKey = "effect.hall" },
                    new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.MonsterCategoryId, OptionId = MvpDungeonPlacementIds.SkeletonOptionId, Danger = 3, ManaPressure = 2, ExplanationKey = "effect.monster" },
                    new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.MonsterCategoryId, OptionId = MvpDungeonPlacementIds.GoblinOptionId, Danger = 2, ManaPressure = 1, LootBonus = 1, Attraction = 1, ExplanationKey = "effect.goblin" },
                    new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.TrapCategoryId, OptionId = MvpDungeonPlacementIds.SpikeTrapOptionId, Danger = 2, HeatPressure = 1, ExplanationKey = "effect.trap" },
                    new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.TrapCategoryId, OptionId = MvpDungeonPlacementIds.SnareTrapOptionId, Danger = 1, ExplanationKey = "effect.snare" },
                    new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.LootNodeCategoryId, OptionId = MvpDungeonPlacementIds.BasicLootNodeOptionId, LootBonus = 4, Attraction = 2, ExplanationKey = "effect.loot" },
                    new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.LootNodeCategoryId, OptionId = MvpDungeonPlacementIds.HiddenCacheOptionId, LootBonus = 3, Attraction = 1, ExplanationKey = "effect.cache" }
                }
            };
        }
    }
}
