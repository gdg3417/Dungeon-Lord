using System.Collections.Generic;
using System.Reflection;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpDungeonPlacementScaffoldTests
    {
        private GameObject _rootObject;
        private GameRoot _root;

        [SetUp]
        public void SetUp()
        {
            _rootObject = new GameObject("MvpDungeonPlacementScaffoldRootTest");
            _root = _rootObject.AddComponent<GameRoot>();
            SetBackingField("<Save>k__BackingField", new SaveData());
            SetBackingField("<Content>k__BackingField", BuildContent());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rootObject);
        }

        [Test]
        public void PlacementModel_InitializesWithExactlyFourCategoriesAndEightValidOptions()
        {
            Assert.That(MvpDungeonPlacementIds.OrderedCategoryIds, Is.EqualTo(new[]
            {
                MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.TrapCategoryId,
                MvpDungeonPlacementIds.LootNodeCategoryId
            }));
            Assert.That(MvpDungeonPlacementIds.OrderedStarterOptionIds, Is.EqualTo(new[]
            {
                MvpDungeonPlacementIds.BasicRoomOptionId,
                MvpDungeonPlacementIds.SkeletonOptionId,
                MvpDungeonPlacementIds.SpikeTrapOptionId,
                MvpDungeonPlacementIds.BasicLootNodeOptionId
            }));
            Assert.That(MvpDungeonPlacementIds.OrderedOptionIds, Is.EqualTo(new[]
            {
                MvpDungeonPlacementIds.BasicRoomOptionId,
                MvpDungeonPlacementIds.NarrowHallOptionId,
                MvpDungeonPlacementIds.SkeletonOptionId,
                MvpDungeonPlacementIds.GoblinOptionId,
                MvpDungeonPlacementIds.SpikeTrapOptionId,
                MvpDungeonPlacementIds.SnareTrapOptionId,
                MvpDungeonPlacementIds.BasicLootNodeOptionId,
                MvpDungeonPlacementIds.HiddenCacheOptionId
            }));
        }

        [TestCase(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)]
        [TestCase(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId)]
        [TestCase(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId)]
        [TestCase(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId)]
        [TestCase(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId)]
        [TestCase(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SnareTrapOptionId)]
        [TestCase(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId)]
        [TestCase(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId)]
        public void PlayerCanPlaceStarterOptionForEachCategory(string categoryId, string optionId)
        {
            bool placed = _root.TryMvpPlaceOrModifySelectedPlacement(
                categoryId,
                optionId,
                out MvpDungeonPlacementEntry prior,
                out MvpDungeonPlacementEntry entry,
                out string bannerKey);

            Assert.That(placed, Is.True);
            Assert.That(prior, Is.Null);
            Assert.That(bannerKey, Is.EqualTo("ui.banner.place_success"));
            Assert.That(entry.CategoryId, Is.EqualTo(categoryId));
            Assert.That(entry.OptionId, Is.EqualTo(optionId));
            Assert.That(_root.Save.mvpDungeonPlacements.Entries.Count, Is.EqualTo(1));
            MvpDungeonPlacementEntry[] nodePlacements = MvpDungeonLayoutResolver.ResolveOrderedNodePlacements(_root.Save.mvpDungeonFloorLayout);
            Assert.That(_root.Save.mvpDungeonFloorLayout.Nodes.Count, Is.EqualTo(MvpDungeonPlacementIds.OrderedCategoryIds.Length));
            Assert.That(nodePlacements, Has.Length.EqualTo(1));
            Assert.That(nodePlacements[0].OptionId, Is.EqualTo(optionId));
        }



        [Test]
        public void SelectedRoomEnforcedPlacement_BasicRoomAcceptsMonster()
        {
            SetRunSimulationConfig(RoomSlotConfig());
            _root.TryMvpPlaceOrModifySelectedPlacement(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId, out _, out _, out _);

            bool placed = _root.TryMvpPlaceOrModifySelectedPlacementEnforcingRoomTarget(
                MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId,
                out _,
                out MvpDungeonPlacementEntry entry,
                out string bannerKey,
                out string failureFeedback);

            Assert.That(placed, Is.True);
            Assert.That(entry.OptionId, Is.EqualTo(MvpDungeonPlacementIds.SkeletonOptionId));
            Assert.That(bannerKey, Is.EqualTo("ui.banner.place_success"));
            Assert.That(failureFeedback, Is.Empty);
            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(_root.Save, RoomSlotConfig());
            Assert.That(layout.Rooms[0].AssignedMonsterOptionIds, Is.EqualTo(new[] { MvpDungeonPlacementIds.SkeletonOptionId }));
            Assert.That(_root.Save.mvpRoomSlotAssignments.Rooms, Has.Count.EqualTo(1));
        }

        [Test]
        public void SelectedRoomEnforcedPlacement_RoomOnePersistsMonsterInRoomOne()
        {
            SetRunSimulationConfig(RoomSlotConfig());
            _root.Save.mvpRoomSlotAssignments.Rooms.Add(new MvpRoomSlotAssignmentState
            {
                FloorIndex = 0,
                RoomIndex = 0,
                RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId
            });
            _root.Save.mvpRoomSlotAssignments.Rooms.Add(new MvpRoomSlotAssignmentState
            {
                FloorIndex = 0,
                RoomIndex = 1,
                RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId
            });
            _root.Save.mvpSelectedRoomSlotIndex = 1;

            bool placed = _root.TryMvpPlaceOrModifySelectedPlacementEnforcingRoomTarget(
                MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.GoblinOptionId,
                out _,
                out _,
                out string bannerKey,
                out string failureFeedback);

            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(_root.Save, RoomSlotConfig());
            Assert.That(placed, Is.True);
            Assert.That(bannerKey, Is.EqualTo("ui.banner.place_success"));
            Assert.That(failureFeedback, Is.Empty);
            Assert.That(layout.Rooms[0].AssignedMonsterOptionIds, Is.Empty);
            Assert.That(layout.Rooms[1].AssignedMonsterOptionIds, Is.EqualTo(new[] { MvpDungeonPlacementIds.GoblinOptionId }));
        }

        [Test]
        public void SelectedRoomEnforcedPlacement_NarrowHallRejectsLootWithLocalizedFeedback()
        {
            SetRunSimulationConfig(RoomSlotConfig());
            _root.TryMvpPlaceOrModifySelectedPlacement(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId, out _, out _, out _);

            bool placed = _root.TryMvpPlaceOrModifySelectedPlacementEnforcingRoomTarget(
                MvpDungeonPlacementIds.LootNodeCategoryId,
                MvpDungeonPlacementIds.HiddenCacheOptionId,
                out _,
                out _,
                out string bannerKey,
                out string failureFeedback);

            Assert.That(placed, Is.False);
            Assert.That(bannerKey, Is.EqualTo("ui.banner.place_failed"));
            Assert.That(failureFeedback, Is.EqualTo("No valid Loot node slot in Room 1: Narrow Hall."));
            Assert.That(failureFeedback, Does.Not.Contain("placement."));
            Assert.That(failureFeedback, Does.Not.Contain("ui."));
        }

        [Test]
        public void FailedLootPlacement_DoesNotRemainActiveInCompositionEffectsOrLayout()
        {
            RunSimulationConfig config = RoomSlotPlacementEffectsConfig();
            SetRunSimulationConfig(config);
            _root.TryMvpPlaceOrModifySelectedPlacement(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId, out _, out _, out _);

            bool placed = _root.TryMvpPlaceOrModifySelectedPlacementEnforcingRoomTarget(
                MvpDungeonPlacementIds.LootNodeCategoryId,
                MvpDungeonPlacementIds.BasicLootNodeOptionId,
                out _,
                out _,
                out _,
                out _);

            string layoutText = MvpDungeonLayoutPresenter.BuildLayoutText(_root.Save, config, Localized);
            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(_root.Save, config);
            MvpPlacementEffectsSummary effects = MvpPlacementEffectsResolver.ResolveForSave(_root.Save, config);

            Assert.That(placed, Is.False);
            Assert.That(layoutText, Does.Not.Contain("Dungeon layout:"));
            Assert.That(layoutText, Does.Contain("Room 1: Narrow Hall"));
            Assert.That(layoutText, Does.Contain("Loot 0/0: Unavailable"));
            Assert.That(layoutText, Does.Not.Contain("Basic Loot Node"));
            Assert.That(summary.DungeonPlacements, Has.None.Matches<MvpDungeonPlacementEntry>(entry => entry.CategoryId == MvpDungeonPlacementIds.LootNodeCategoryId));
            Assert.That(effects.LootBonus, Is.EqualTo(0));
            Assert.That(effects.Attraction, Is.EqualTo(0));
            Assert.That(effects.ContributingOptionIds, Does.Not.Contain(MvpDungeonPlacementIds.BasicLootNodeOptionId));
            AssertNoRawPlacementIds(layoutText);
        }

        [Test]
        public void RoomSlotLayout_UsesPersistedAssignmentsWhenPresent()
        {
            var save = new SaveData
            {
                mvpDungeonPlacements = AllStarterPlacementState(),
                mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection
                {
                    Rooms = new List<MvpRoomSlotAssignmentState>
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
                }
            };

            string text = MvpDungeonLayoutPresenter.BuildRoomSlotLayoutText(MvpRoomSlotLayoutResolver.ResolveDefaultFloor(save, RoomSlotConfig()), Localized);

            Assert.That(text, Does.Contain("Goblin"));
            Assert.That(text, Does.Contain("Snare Trap"));
            Assert.That(text, Does.Contain("Hidden Cache"));
            Assert.That(text, Does.Not.Contain("Skeleton"));
            AssertNoRawPlacementIds(text);
        }

        [Test]
        public void RoomSlotLayout_FallsBackToLegacyPlacementsWhenPersistedAssignmentsAbsent()
        {
            var save = new SaveData { mvpDungeonPlacements = AllStarterPlacementState(), mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection() };

            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(save, RoomSlotConfig());

            Assert.That(layout.Rooms[0].AssignedMonsterOptionIds, Is.EqualTo(new[] { MvpDungeonPlacementIds.SkeletonOptionId }));
            Assert.That(layout.Rooms[0].AssignedTrapOptionIds, Is.EqualTo(new[] { MvpDungeonPlacementIds.SpikeTrapOptionId }));
            Assert.That(layout.Rooms[0].AssignedLootNodeOptionIds, Is.EqualTo(new[] { MvpDungeonPlacementIds.BasicLootNodeOptionId }));
        }

        [Test]
        public void LegacySave_BasicRoomGoblinSpikeTrapHiddenCache_ResolvesSameRoomSlotLayout()
        {
            var save = new SaveData
            {
                mvpDungeonPlacements = new MvpDungeonPlacementState
                {
                    Entries = new List<MvpDungeonPlacementEntry>
                    {
                        new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId, 1),
                        new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId, 2),
                        new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId, 3),
                        new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId, 4)
                    },
                    NextRevision = 5
                },
                mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection()
            };

            string text = MvpDungeonLayoutPresenter.BuildRoomSlotLayoutText(MvpRoomSlotLayoutResolver.ResolveDefaultFloor(save, RoomSlotConfig()), Localized);

            Assert.That(text, Does.Contain("Goblin"));
            Assert.That(text, Does.Contain("Spike Trap"));
            Assert.That(text, Does.Contain("Hidden Cache"));
            AssertNoRawPlacementIds(text);
        }

        [Test]
        public void LegacyNarrowHallLoot_UsesFallbackBasicRoomAndKeepsCompositionEffectsConsistent()
        {
            RunSimulationConfig config = RoomSlotPlacementEffectsConfig();
            var save = new SaveData
            {
                mvpDungeonPlacements = new MvpDungeonPlacementState
                {
                    Entries = new List<MvpDungeonPlacementEntry>
                    {
                        new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId, 1),
                        new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId, 2)
                    },
                    NextRevision = 3
                },
                mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection()
            };

            string layoutText = MvpDungeonLayoutPresenter.BuildLayoutText(save, config, Localized);
            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, config);
            MvpPlacementEffectsSummary effects = MvpPlacementEffectsResolver.ResolveForSave(save, config);

            Assert.That(layoutText, Does.Not.Contain("Dungeon layout:"));
            Assert.That(layoutText, Does.Contain("Room 1: Narrow Hall"));
            Assert.That(layoutText, Does.Contain("Room 2: Basic Room"));
            Assert.That(layoutText, Does.Contain("Loot 1/1: Basic Loot Node"));
            Assert.That(summary.DungeonPlacements, Has.Some.Matches<MvpDungeonPlacementEntry>(entry => entry.OptionId == MvpDungeonPlacementIds.BasicLootNodeOptionId));
            Assert.That(effects.LootBonus, Is.EqualTo(5));
            Assert.That(effects.Attraction, Is.EqualTo(3));
            AssertNoRawPlacementIds(layoutText);
        }

        [Test]
        public void PersistedRoomSlotAssignments_OverrideLegacyForCompositionAndEffects()
        {
            RunSimulationConfig config = RoomSlotPlacementEffectsConfig();
            var save = new SaveData
            {
                mvpDungeonPlacements = new MvpDungeonPlacementState
                {
                    Entries = new List<MvpDungeonPlacementEntry>
                    {
                        new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId, 1),
                        new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId, 2)
                    },
                    NextRevision = 3
                },
                mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection
                {
                    Rooms = new List<MvpRoomSlotAssignmentState>
                    {
                        new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = 0, RoomOptionId = MvpDungeonPlacementIds.NarrowHallOptionId }
                    }
                }
            };

            string layoutText = MvpDungeonLayoutPresenter.BuildLayoutText(save, config, Localized);
            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, config);
            MvpPlacementEffectsSummary effects = MvpPlacementEffectsResolver.ResolveForSave(save, config);

            Assert.That(layoutText, Does.Not.Contain("Dungeon layout:"));
            Assert.That(layoutText, Does.Contain("Loot 0/0: Unavailable"));
            Assert.That(layoutText, Does.Not.Contain("Basic Loot Node"));
            Assert.That(summary.DungeonPlacements, Has.None.Matches<MvpDungeonPlacementEntry>(entry => entry.CategoryId == MvpDungeonPlacementIds.LootNodeCategoryId));
            Assert.That(effects.LootBonus, Is.EqualTo(0));
            Assert.That(effects.Attraction, Is.EqualTo(0));
            Assert.That(effects.ContributingOptionIds, Does.Not.Contain(MvpDungeonPlacementIds.BasicLootNodeOptionId));
            AssertNoRawPlacementIds(layoutText);
        }

        [Test]
        public void PersistedLootCapableFallbackRoom_ShowsLootInOwnSlot()
        {
            var save = new SaveData
            {
                mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection
                {
                    Rooms = new List<MvpRoomSlotAssignmentState>
                    {
                        new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = 0, RoomOptionId = MvpDungeonPlacementIds.NarrowHallOptionId },
                        new MvpRoomSlotAssignmentState
                        {
                            FloorIndex = 0,
                            RoomIndex = 1,
                            RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                            LootNodeOptionIds = new[] { MvpDungeonPlacementIds.HiddenCacheOptionId }
                        }
                    }
                }
            };

            string text = MvpDungeonLayoutPresenter.BuildRoomSlotLayoutText(MvpRoomSlotLayoutResolver.ResolveDefaultFloor(save, RoomSlotConfig()), Localized);

            Assert.That(text, Does.Contain("Room 2: Basic Room"));
            Assert.That(text, Does.Contain("Hidden Cache"));
            AssertNoRawPlacementIds(text);
        }

        [Test]
        public void PersistedRoomSlotLayout_HidesExtraRoomTwoMonsterWhenActiveCompositionUsesRoomOneMonster()
        {
            RunSimulationConfig config = RoomSlotPlacementEffectsConfig();
            var save = new SaveData
            {
                mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection
                {
                    Rooms = new List<MvpRoomSlotAssignmentState>
                    {
                        new MvpRoomSlotAssignmentState
                        {
                            FloorIndex = 0,
                            RoomIndex = 0,
                            RoomOptionId = MvpDungeonPlacementIds.NarrowHallOptionId,
                            MonsterOptionIds = new[] { MvpDungeonPlacementIds.GoblinOptionId },
                            TrapOptionIds = new[] { MvpDungeonPlacementIds.SnareTrapOptionId }
                        },
                        new MvpRoomSlotAssignmentState
                        {
                            FloorIndex = 0,
                            RoomIndex = 1,
                            RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                            MonsterOptionIds = new[] { MvpDungeonPlacementIds.SkeletonOptionId },
                            LootNodeOptionIds = new[] { MvpDungeonPlacementIds.BasicLootNodeOptionId }
                        }
                    }
                },
                mvpSelectedRoomSlotIndex = 1
            };

            string layoutText = MvpDungeonLayoutPresenter.BuildLayoutText(save, config, MvpDungeonPlacementIds.LootNodeCategoryId, Localized);
            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(save, config);
            MvpPlacementEffectsSummary effects = MvpPlacementEffectsResolver.ResolveForSave(save, config);

            Assert.That(layoutText, Does.Not.Contain("Dungeon layout:"));
            Assert.That(layoutText, Does.Contain("Room 1: Narrow Hall"));
            Assert.That(layoutText, Does.Contain("Room 2: Basic Room — Monsters 0/1: Empty; Traps 0/1: Empty; Loot 1/1: Basic Loot Node"));
            Assert.That(layoutText, Does.Not.Contain("Skeleton"));
            Assert.That(summary.DungeonPlacements, Has.Some.Matches<MvpDungeonPlacementEntry>(entry => entry.OptionId == MvpDungeonPlacementIds.GoblinOptionId));
            Assert.That(summary.DungeonPlacements, Has.None.Matches<MvpDungeonPlacementEntry>(entry => entry.OptionId == MvpDungeonPlacementIds.SkeletonOptionId));
            Assert.That(effects.ContributingOptionIds, Does.Not.Contain(MvpDungeonPlacementIds.SkeletonOptionId));
            AssertNoRawPlacementIds(layoutText);
        }

        [Test]
        public void SelectedRoomTarget_ClampsWhenPersistedRoomCountShrinks()
        {
            var save = new SaveData
            {
                mvpSelectedRoomSlotIndex = 9,
                mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection
                {
                    Rooms = new List<MvpRoomSlotAssignmentState>
                    {
                        new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = 0, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId }
                    }
                }
            };

            int index = MvpRoomSlotTargetResolver.ResolveClampedSelectedRoomIndex(save, MvpRoomSlotLayoutResolver.ResolveDefaultFloor(save, RoomSlotConfig()));

            Assert.That(index, Is.EqualTo(0));
        }

        [Test]
        public void PlacingSameCategory_ModifiesExistingEntryDeterministically()
        {
            _root.TryMvpPlaceOrModifySelectedPlacement(
                MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.BasicRoomOptionId,
                out _,
                out MvpDungeonPlacementEntry first,
                out _);
            int firstRevision = first.Revision;

            bool modified = _root.TryMvpPlaceOrModifySelectedPlacement(
                MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.BasicRoomOptionId,
                out MvpDungeonPlacementEntry prior,
                out MvpDungeonPlacementEntry second,
                out _);

            Assert.That(modified, Is.True);
            Assert.That(prior, Is.Not.Null);
            Assert.That(prior.Revision, Is.EqualTo(firstRevision));
            Assert.That(second.Revision, Is.GreaterThan(prior.Revision));
            Assert.That(_root.Save.mvpDungeonPlacements.Entries.Count, Is.EqualTo(1));
            MvpDungeonPlacementEntry[] nodePlacements = MvpDungeonLayoutResolver.ResolveOrderedNodePlacements(_root.Save.mvpDungeonFloorLayout);
            Assert.That(_root.Save.mvpDungeonFloorLayout.Nodes.Count, Is.EqualTo(MvpDungeonPlacementIds.OrderedCategoryIds.Length));
            Assert.That(nodePlacements, Has.Length.EqualTo(1));
            Assert.That(nodePlacements[0].Revision, Is.EqualTo(second.Revision));
        }


        [Test]
        public void RunSummaryResolvesAfterAllStarterPlacementsExist()
        {
            _root.TryMvpPlaceOrModifySelectedPlacement(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId, out _, out _, out _);
            _root.TryMvpPlaceOrModifySelectedPlacement(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, out _, out _, out _);
            _root.TryMvpPlaceOrModifySelectedPlacement(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId, out _, out _, out _);
            _root.TryMvpPlaceOrModifySelectedPlacement(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId, out _, out _, out _);

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(_root.Save);

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.HasPlacementContext, Is.True);
            Assert.That(summary.DungeonPlacements.Length, Is.EqualTo(4));
            Assert.That(summary.DungeonPlacements[0].CategoryId, Is.EqualTo(MvpDungeonPlacementIds.RoomCategoryId));
            Assert.That(summary.DungeonPlacements[3].CategoryId, Is.EqualTo(MvpDungeonPlacementIds.LootNodeCategoryId));
        }

        [Test]
        public void SummaryAndFeedback_UseLocalizedCategoryAndOptionNamesWithoutRawIds()
        {
            _root.TryMvpPlaceOrModifySelectedPlacement(
                MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.BasicRoomOptionId,
                out _,
                out MvpDungeonPlacementEntry room,
                out _);
            _root.TryMvpPlaceOrModifySelectedPlacement(
                MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId,
                out _,
                out _,
                out _);

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(_root.Save);
            string panel = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, Localized);
            string feedback = MvpStructurePlacementFeedbackPresenter.BuildPlacementFeedbackText(null, room, Localized);

            Assert.That(panel, Does.Contain("Dungeon composition: Room: Basic Room; Monster: Skeleton"));
            Assert.That(feedback, Does.Contain("Changed placement: Empty slot -> Room: Basic Room."));
            AssertNoRawPlacementIds(panel);
            AssertNoRawPlacementIds(feedback);
        }


        [Test]
        public void NodeLayoutSummary_UsesLocalizationKeysWithoutRawIds()
        {
            _root.Save.mvpDungeonPlacements = new MvpDungeonPlacementState();
            _root.Save.mvpDungeonFloorLayout = new MvpDungeonFloorLayoutState
            {
                Nodes = new List<MvpDungeonNodeState>
                {
                    new MvpDungeonNodeState(0, 0, "mvp.floor.00.node.00", MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId, 1),
                    new MvpDungeonNodeState(0, 1, "mvp.floor.00.node.01", MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 2)
                },
                NextRevision = 3
            };

            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(_root.Save);
            string panel = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, Localized);

            Assert.That(summary.DungeonPlacements, Has.Length.EqualTo(2));
            Assert.That(panel, Does.Contain("Dungeon composition: Room: Basic Room; Monster: Skeleton"));
            AssertNoRawPlacementIds(panel);
        }


        [Test]
        public void EditingOneCategoryOnMigratedLegacySave_KeepsOtherLegacyCategoriesVisible()
        {
            _root.Save.mvpDungeonPlacements = AllStarterPlacementState();
            _root.Save.mvpDungeonFloorLayout = MvpDungeonFloorLayoutState.CreateEmptyStarterFloor();
            _root.Save.structureRuntime = new StructureRuntimeState();

            bool modified = _root.TryMvpPlaceOrModifySelectedPlacement(
                MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.BasicRoomOptionId,
                out _,
                out _,
                out _);

            MvpDungeonPlacementEntry[] resolved = MvpDungeonLayoutResolver.ResolveOrderedPlacements(_root.Save.mvpDungeonFloorLayout, _root.Save.mvpDungeonPlacements);
            MvpPlacementEffectsSummary effects = MvpPlacementEffectsResolver.Resolve(_root.Save.mvpDungeonFloorLayout, _root.Save.mvpDungeonPlacements, PlacementEffectsConfig());
            MvpPlayerLoopSummary summary = MvpPlayerLoopSummaryPresenter.Resolve(_root.Save, PlacementEffectsConfig());

            Assert.That(modified, Is.True);
            Assert.That(resolved, Has.Length.EqualTo(4));
            Assert.That(effects.ContributingOptionIds, Is.EqualTo(MvpDungeonPlacementIds.OrderedStarterOptionIds));
            Assert.That(summary.DungeonPlacements, Has.Length.EqualTo(4));
            Assert.That(summary.DungeonPlacements[0].CategoryId, Is.EqualTo(MvpDungeonPlacementIds.RoomCategoryId));
            Assert.That(summary.DungeonPlacements[3].CategoryId, Is.EqualTo(MvpDungeonPlacementIds.LootNodeCategoryId));
        }

        [Test]
        public void AddSecondBasicRoomSlot_OnOneRoomSave_CreatesAndSelectsRoomTwo()
        {
            SetRunSimulationConfig(RoomSlotConfig());
            _root.TryMvpPlaceOrModifySelectedPlacement(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId, out _, out _, out _);

            bool added = _root.TryAddSecondMvpBasicRoomSlot();

            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(_root.Save, RoomSlotConfig());
            Assert.That(added, Is.True);
            Assert.That(layout.Rooms, Has.Length.EqualTo(2));
            Assert.That(layout.Rooms[0].RoomOptionId, Is.EqualTo(MvpDungeonPlacementIds.NarrowHallOptionId));
            Assert.That(layout.Rooms[1].RoomOptionId, Is.EqualTo(MvpDungeonPlacementIds.BasicRoomOptionId));
            Assert.That(layout.Rooms[1].AssignedMonsterOptionIds, Is.Empty);
            Assert.That(layout.Rooms[1].AssignedTrapOptionIds, Is.Empty);
            Assert.That(layout.Rooms[1].AssignedLootNodeOptionIds, Is.Empty);
            Assert.That(_root.Save.mvpSelectedRoomSlotIndex, Is.EqualTo(1));
        }

        [Test]
        public void AddSecondBasicRoomSlot_PersistsRoomSlotAssignmentState()
        {
            SetRunSimulationConfig(RoomSlotConfig());

            bool added = _root.TryAddSecondMvpBasicRoomSlot();

            Assert.That(added, Is.True);
            Assert.That(_root.Save.mvpRoomSlotAssignments.Rooms, Has.Count.EqualTo(2));
            Assert.That(_root.Save.mvpRoomSlotAssignments.Rooms[1].FloorIndex, Is.EqualTo(0));
            Assert.That(_root.Save.mvpRoomSlotAssignments.Rooms[1].RoomIndex, Is.EqualTo(1));
            Assert.That(_root.Save.mvpRoomSlotAssignments.Rooms[1].RoomOptionId, Is.EqualTo(MvpDungeonPlacementIds.BasicRoomOptionId));
            Assert.That(_root.Save.mvpRoomSlotAssignments.NextRevision, Is.GreaterThan(1));
        }

        [Test]
        public void AddSecondBasicRoomSlot_WhenRoomTwoAlreadyExists_FailsGracefully()
        {
            SetRunSimulationConfig(RoomSlotConfig());
            Assert.That(_root.TryAddSecondMvpBasicRoomSlot(), Is.True);

            bool addedAgain = _root.TryAddSecondMvpBasicRoomSlot();

            Assert.That(addedAgain, Is.False);
            Assert.That(_root.Save.mvpRoomSlotAssignments.Rooms, Has.Count.EqualTo(2));
        }

        [Test]
        public void CycleRoomTarget_ReachesRoomTwoAfterExpansion()
        {
            SetRunSimulationConfig(RoomSlotConfig());
            _root.TryAddSecondMvpBasicRoomSlot();
            _root.Save.mvpSelectedRoomSlotIndex = 0;

            _root.CycleSelectedMvpRoomSlotTarget();

            Assert.That(_root.Save.mvpSelectedRoomSlotIndex, Is.EqualTo(1));
        }

        [Test]
        public void AddedRoomTwo_ShowsBasicRoomCapacityAndLootFit()
        {
            SetRunSimulationConfig(RoomSlotConfig());
            _root.TryMvpPlaceOrModifySelectedPlacement(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId, out _, out _, out _);
            _root.TryAddSecondMvpBasicRoomSlot();

            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(_root.Save, RoomSlotConfig());
            string capacity = MvpRoomSlotTargetPresenter.BuildSelectedCapacityText(layout, 1, Localized);
            string fit = MvpRoomSlotTargetPresenter.BuildSelectedPlacementFitText(layout, 1, MvpDungeonPlacementIds.LootNodeCategoryId, Localized);
            string roomOneFit = MvpRoomSlotTargetPresenter.BuildSelectedPlacementFitText(layout, 0, MvpDungeonPlacementIds.LootNodeCategoryId, Localized);

            Assert.That(capacity, Does.Contain("Loot 0/1"));
            Assert.That(fit, Is.EqualTo("Selected placement fit: Loot node fits Room 2."));
            Assert.That(roomOneFit, Is.EqualTo("Selected placement fit: Loot node cannot fit Room 1 because this room has no loot slot."));
        }

        [Test]
        public void PlacingLootIntoAddedRoomTwo_AssignsOnlyLootToRoomTwo()
        {
            SetRunSimulationConfig(RoomSlotConfig());
            _root.TryAddSecondMvpBasicRoomSlot();

            bool placed = _root.TryMvpPlaceOrModifySelectedPlacementEnforcingRoomTarget(
                MvpDungeonPlacementIds.LootNodeCategoryId,
                MvpDungeonPlacementIds.BasicLootNodeOptionId,
                out _,
                out _,
                out _,
                out _);

            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(_root.Save, RoomSlotConfig());
            string capacity = MvpRoomSlotTargetPresenter.BuildSelectedCapacityText(layout, 1, Localized);

            Assert.That(placed, Is.True);
            Assert.That(layout.Rooms[1].AssignedMonsterOptionIds, Is.Empty);
            Assert.That(layout.Rooms[1].AssignedTrapOptionIds, Is.Empty);
            Assert.That(layout.Rooms[1].AssignedLootNodeOptionIds, Is.EqualTo(new[] { MvpDungeonPlacementIds.BasicLootNodeOptionId }));
            Assert.That(capacity, Is.EqualTo("Selected room capacity: Monsters 0/1; Traps 0/1; Loot 1/1"));
        }

        [Test]
        public void F6SmokeLayout_IncludesRoomTwoAfterExpansionWithoutRawIds()
        {
            SetRunSimulationConfig(RoomSlotConfig());
            _root.TryAddSecondMvpBasicRoomSlot();

            string layoutText = MvpDungeonLayoutPresenter.BuildLayoutText(_root.Save, RoomSlotConfig(), MvpDungeonPlacementIds.LootNodeCategoryId, Localized);

            Assert.That(layoutText, Does.Contain("Selected room target: Room 2: Basic Room"));
            Assert.That(layoutText, Does.Contain("Selected placement fit: Loot node fits Room 2."));
            Assert.That(layoutText, Does.Contain("Room 2: Basic Room"));
            AssertNoRawPlacementIds(layoutText);
        }

        private void SetBackingField(string fieldName, object value)
        {
            typeof(GameRoot).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_root, value);
        }

        private static void AssertNoRawPlacementIds(string text)
        {
            foreach (string categoryId in MvpDungeonPlacementIds.OrderedCategoryIds)
            {
                Assert.That(text, Does.Not.Contain(categoryId));
            }

            foreach (string optionId in MvpDungeonPlacementIds.OrderedOptionIds)
            {
                Assert.That(text, Does.Not.Contain(optionId));
            }
        }


        private static MvpDungeonPlacementState AllStarterPlacementState()
        {
            return new MvpDungeonPlacementState
            {
                Entries = new List<MvpDungeonPlacementEntry>
                {
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId, 1),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 2),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId, 3),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId, 4)
                },
                NextRevision = 5
            };
        }



        private static ContentService BuildContent()
        {
            var content = new ContentService();
            var map = (Dictionary<string, string>)typeof(ContentService)
                .GetField("_stringMap", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(content);
            if (map != null)
            {
                map[MvpRoomSlotTargetPresenter.SelectedTargetFormatKey] = "Selected room target: Room {0}: {1}";
                map[MvpRoomSlotTargetPresenter.NoValidSlotFormatKey] = "No valid {0} slot in Room {1}: {2}.";
                map["ui.mvp_room_slots.cycle_target_button"] = "Cycle room target";
                map[MvpDungeonPlacementPresenter.RoomCategoryKey] = "Room";
                map[MvpDungeonPlacementPresenter.MonsterCategoryKey] = "Monster";
                map[MvpDungeonPlacementPresenter.TrapCategoryKey] = "Trap";
                map[MvpDungeonPlacementPresenter.LootNodeCategoryKey] = "Loot node";
                map[MvpDungeonPlacementPresenter.BasicRoomOptionKey] = "Basic Room";
                map[MvpDungeonPlacementPresenter.NarrowHallOptionKey] = "Narrow Hall";
                map[MvpDungeonPlacementPresenter.SkeletonOptionKey] = "Skeleton";
                map[MvpDungeonPlacementPresenter.GoblinOptionKey] = "Goblin";
                map[MvpDungeonPlacementPresenter.SpikeTrapOptionKey] = "Spike Trap";
                map[MvpDungeonPlacementPresenter.SnareTrapOptionKey] = "Snare Trap";
                map[MvpDungeonPlacementPresenter.HiddenCacheOptionKey] = "Hidden Cache";
                map[MvpDungeonPlacementPresenter.BasicLootNodeOptionKey] = "Basic Loot Node";
            }

            return content;
        }

        private void SetRunSimulationConfig(RunSimulationConfig config)
        {
            SetBackingField("_runSimulationService", new RunSimulationService(config));
        }

        private static RunSimulationConfig RoomSlotConfig()
        {
            return new RunSimulationConfig
            {
                MvpRoomSlotCapacities = new[]
                {
                    new MvpRoomSlotCapacityConfig { RoomOptionId = MvpDungeonPlacementIds.NarrowHallOptionId, MonsterCapacity = 1, TrapCapacity = 1, LootCapacity = 0 },
                    new MvpRoomSlotCapacityConfig { RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId, MonsterCapacity = 1, TrapCapacity = 1, LootCapacity = 1 }
                }
            };
        }

        private static RunSimulationConfig PlacementEffectsConfig()
        {
            return new RunSimulationConfig
            {
                MvpPlacementEffectsRuleSourceId = "mvp.placement_effects.rule.test",
                MvpPlacementEffects = new[]
                {
                    new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.RoomCategoryId, OptionId = MvpDungeonPlacementIds.BasicRoomOptionId, PathCapacity = 2, ExplanationKey = "effect.room" },
                    new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.MonsterCategoryId, OptionId = MvpDungeonPlacementIds.SkeletonOptionId, Danger = 3, ManaPressure = 2, ExplanationKey = "effect.monster" },
                    new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.TrapCategoryId, OptionId = MvpDungeonPlacementIds.SpikeTrapOptionId, Danger = 2, HeatPressure = 1, ExplanationKey = "effect.trap" },
                    new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.LootNodeCategoryId, OptionId = MvpDungeonPlacementIds.BasicLootNodeOptionId, LootBonus = 4, Attraction = 2, ExplanationKey = "effect.loot" }
                }
            };
        }

        private static RunSimulationConfig RoomSlotPlacementEffectsConfig()
        {
            RunSimulationConfig config = RoomSlotConfig();
            config.MvpPlacementEffectsRuleSourceId = "mvp.placement_effects.rule.test";
            config.MvpPlacementEffects = new[]
            {
                new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.RoomCategoryId, OptionId = MvpDungeonPlacementIds.NarrowHallOptionId, PathCapacity = 1, ExplanationKey = "effect.room.narrow_hall" },
                new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.LootNodeCategoryId, OptionId = MvpDungeonPlacementIds.BasicLootNodeOptionId, LootBonus = 5, Attraction = 3, ExplanationKey = "effect.loot.basic" }
            };

            return config;
        }

        private static string Localized(string key, string fallback)
        {
            var map = new Dictionary<string, string>
            {
                [MvpLoopSummaryPanelPresenter.TitleKey] = "MVP Loop Summary",
                [MvpLoopSummaryPanelPresenter.AdventurerIntentSectionKey] = "Expected next adventurer intent",
                [MvpLoopSummaryPanelPresenter.AdventurerPressureSectionKey] = "Adventurer pressure",
                [AdventurerRunIntentPresenter.SummaryFormatKey] = "Expected next adventurer intent: {0} likely. Reason: {1}",
                [AdventurerArrivalPressurePresenter.SummaryFormatKey] = "Adventurer pressure: {0}. Reason: {1}.",
                [AdventurerArrivalPressurePresenter.BodyFormatKey] = "{0}. Reason: {1}.",
                [AdventurerArrivalPressurePresenter.DetailFormatKey] = "Adventurer pressure detail: score {0:0.##}; band {1}; rule source {2}; error {3}; loot {4}; attraction {5}; danger {6}; heat pressure {7}; recent deaths {8}; recovered loot {9}; path complete {10}; latest visit {11}.",
                ["ui.adventurer_pressure.band.not_yet"] = "not yet",
                ["ui.adventurer_pressure.band.low"] = "low",
                ["ui.adventurer_pressure.band.cautious_interest"] = "cautious interest",
                ["ui.adventurer_pressure.band.building_slowly"] = "building slowly",
                ["ui.adventurer_pressure.band.likely_soon"] = "likely soon",
                ["ui.adventurer_pressure.outcome.none"] = "none yet",
                ["ui.adventurer_pressure.outcome.success"] = "success",
                ["ui.adventurer_pressure.outcome.failure"] = "failure",
                [AdventurerArrivalPressureResolver.ReasonNotYetKey] = "current dungeon signals are still forming",
                [AdventurerArrivalPressureResolver.ReasonHighLootLowHeatKey] = "high loot signal and low heat",
                [AdventurerArrivalPressureResolver.ReasonModestLootLowAttractionKey] = "modest loot and low attraction",
                [AdventurerArrivalPressureResolver.ReasonDeathsHeatKey] = "recent deaths and rising heat",
                [AdventurerArrivalPressureResolver.ReasonIncompletePathWeakLootKey] = "incomplete path or weak loot signal",
                [AdventurerRunIntentPresenter.BodyFormatKey] = "{0} likely. Reason: {1}",
                [AdventurerRunIntentPresenter.DebugPostureFormatKey] = "Expected next adventurer intent: {0} likely. Debug selected posture: {1}.",
                [AdventurerRunIntentResolver.ReasonFallbackKey] = "current dungeon signals are still forming",
                [AdventurerRunIntentResolver.ReasonLootHighHeatLowKey] = "loot signal is high and heat is low",
                [AdventurerRunIntentResolver.ReasonDeathsHeatKey] = "recent deaths and rising heat",
                [AdventurerRunIntentResolver.ReasonModerateKey] = "risk and reward are both moderate",
                [AdventurerRunIntentResolver.ReasonDangerKey] = "danger pressure is high",
                ["run.posture.cautious.name"] = "Cautious",
                ["run.posture.balanced.name"] = "Balanced",
                ["run.posture.greedy.name"] = "Greedy",
                [MvpLoopSummaryPanelPresenter.CompositionFormatKey] = "Dungeon composition: {0}",
                [MvpLoopSummaryPanelPresenter.PlacementEffectsFormatKey] = "Placement effects: {0}",
                [MvpLoopSummaryPanelPresenter.LatestRunFormatKey] = "Latest adventurer visit: {0}",
                [MvpLoopSummaryPanelPresenter.ManaFormatKey] = "Mana reserve: {0:0.##}",
                [MvpLoopSummaryPanelPresenter.LootFormatKey] = "Loot: {1}/{0} recovered; {2} tradeable.",
                [MvpLoopSummaryPanelPresenter.HeatFormatKey] = "Heat: {0:0.##} -> {1:0.##} ({2}). {3}",
                [MvpLoopSummaryPanelPresenter.ResearchFormatKey] = "{0}",
                [MvpLoopSummaryPanelPresenter.SuggestionFormatKey] = "{0}",
                [MvpLoopSummaryPanelPresenter.ValueNoRunKey] = "No adventurer visit yet",
                [MvpLoopSummaryPanelPresenter.ValueUnknownKey] = "Unknown",
                [MvpLoopSummaryPanelPresenter.ValueNoResearchKey] = "No research",

                [MvpLoopSummaryPanelPresenter.CurrentDungeonSectionKey] = "Current Dungeon",
                [MvpLoopSummaryPanelPresenter.LatestRunSectionKey] = "Latest Adventurer Visit",
                [MvpLoopSummaryPanelPresenter.WhyItHappenedSectionKey] = "Why It Happened",
                [MvpLoopSummaryPanelPresenter.RewardsAndRiskSectionKey] = "Rewards and Risk",
                [MvpLoopSummaryPanelPresenter.ResearchSectionKey] = "Research",
                [MvpLoopSummaryPanelPresenter.SuggestedNextActionSectionKey] = "Suggested Next Action",
                [MvpLoopSummaryPanelPresenter.SectionLineFormatKey] = "{0}: {1}",
                [MvpLoopSummaryPanelPresenter.InlineSeparatorKey] = " | ",
                [MvpLoopSummaryPanelPresenter.RunOutcomeLineFormatKey] = "{0}. Party: {1}",
                [MvpLoopSummaryPanelPresenter.WhyNoRunKey] = "No adventurer visit yet. Build or review the dungeon, then observe adventurer activity to learn what happens.",
                [MvpLoopSummaryPanelPresenter.WhyRunFormatKey] = "Main reason: {0}.",
                [MvpLoopSummaryPanelPresenter.WhyMixedKey] = "the current placement mix shaped the result",
                [MvpLoopSummaryPanelPresenter.WhyPathCapacityKey] = "path capacity shaped the adventurer visit",
                [MvpLoopSummaryPanelPresenter.WhyDangerKey] = "danger pressure drove the result",
                [MvpLoopSummaryPanelPresenter.WhyManaPressureKey] = "mana pressure constrained the adventurer visit",
                [MvpLoopSummaryPanelPresenter.WhyHeatPressureKey] = "heat pressure raised the risk",
                [MvpLoopSummaryPanelPresenter.WhyLootBonusKey] = "loot placement improved the reward",
                [MvpLoopSummaryPanelPresenter.WhyAttractionKey] = "attraction changed adventurer interest",
                [MvpLoopSummaryPanelPresenter.RiskNoRunKey] = "Risk will be shown after an adventurer visit.",
                [MvpLoopSummaryPanelPresenter.RiskStableKey] = "Risk stayed steady.",
                [MvpLoopSummaryPanelPresenter.RiskIncreasedKey] = "Risk increased.",
                [MvpLoopSummaryPanelPresenter.RiskReducedKey] = "Risk went down.",
                [MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey] = "Observe adventurer activity.",
                [MvpStructurePlacementFeedbackPresenter.EmptySlotKey] = "Empty slot",
                [MvpStructurePlacementFeedbackPresenter.EmptyPlacementValueKey] = "Empty",
                [MvpStructurePlacementFeedbackPresenter.RoomTargetedPlacementChangedFormatKey] = "Changed Room {0} {1}: {2} -> {3}.",
                [MvpStructurePlacementFeedbackPresenter.RoomTargetedPlacementAlreadySetFormatKey] = "Room {0} {1} already set to {2}.",
                [MvpStructurePlacementFeedbackPresenter.PlacementChangedFormatKey] = "Changed placement: {0} -> {1}: {2}. {3}",
                [MvpRoomSlotTargetPresenter.SelectedTargetFormatKey] = "Selected room target: Room {0}: {1}",
                [MvpRoomSlotTargetPresenter.NoValidSlotFormatKey] = "No valid {0} slot in Room {1}: {2}.",
                [MvpRoomSlotTargetPresenter.SelectedCapacityFormatKey] = "Selected room capacity: {0}",
                [MvpRoomSlotTargetPresenter.SelectedCapacityCategoryFormatKey] = "{0} {1}/{2}",
                [MvpRoomSlotTargetPresenter.SelectedCapacityUnavailableCategoryFormatKey] = "{0} unavailable {1}/{2}",
                [MvpRoomSlotTargetPresenter.SelectedCapacitySeparatorKey] = "; ",
                [MvpRoomSlotTargetPresenter.SelectedPlacementFitFormatKey] = "Selected placement fit: {0}",
                [MvpRoomSlotTargetPresenter.SelectedPlacementFitsFormatKey] = "{0} fits Room {1}.",
                [MvpRoomSlotTargetPresenter.SelectedPlacementCannotFitNoSlotFormatKey] = "{0} cannot fit Room {1} because this room has no {2}.",
                [MvpRoomSlotTargetPresenter.CapacityMonstersLabelKey] = "Monsters",
                [MvpRoomSlotTargetPresenter.CapacityTrapsLabelKey] = "Traps",
                [MvpRoomSlotTargetPresenter.CapacityLootLabelKey] = "Loot",
                [MvpRoomSlotTargetPresenter.MonsterSlotReasonLabelKey] = "monster slot",
                [MvpRoomSlotTargetPresenter.TrapSlotReasonLabelKey] = "trap slot",
                [MvpRoomSlotTargetPresenter.LootSlotReasonLabelKey] = "loot slot",
                ["ui.mvp_room_slots.cycle_target_button"] = "Cycle room target",
                [MvpDungeonLayoutPresenter.LayoutFormatKey] = "Dungeon layout: {0}",
                [MvpDungeonLayoutPresenter.FloorFormatKey] = "Floor {0}: {1}",
                [MvpDungeonLayoutPresenter.AssignedNodeFormatKey] = "{0}: {1}",
                [MvpDungeonLayoutPresenter.EmptyNodeFormatKey] = "{0}: {1}",
                [MvpDungeonLayoutPresenter.NodeSeparatorKey] = " -> ",
                [MvpDungeonLayoutPresenter.EmptyAvailableKey] = "Empty / available",
                [MvpDungeonLayoutPresenter.RoomSlotLayoutFormatKey] = "Room slot layout: {0}",
                [MvpDungeonLayoutPresenter.RoomSlotFloorFormatKey] = "Floor {0}: {1}",
                [MvpDungeonLayoutPresenter.RoomSlotRoomFormatKey] = "Room {0}: {1} — {2}; {3}; {4}",
                [MvpDungeonLayoutPresenter.MonstersFormatKey] = "Monsters {1}/{2}: {0}",
                [MvpDungeonLayoutPresenter.TrapsFormatKey] = "Traps {1}/{2}: {0}",
                [MvpDungeonLayoutPresenter.LootFormatKey] = "Loot {1}/{2}: {0}",
                [MvpDungeonLayoutPresenter.EmptyAssignmentKey] = "Empty",
                [MvpDungeonLayoutPresenter.UnavailableAssignmentKey] = "Unavailable",
                [MvpDungeonLayoutPresenter.AssignmentSeparatorKey] = ", ",
                [MvpDungeonLayoutPresenter.RoomSlotSeparatorKey] = " | ",
                [MvpDungeonPlacementPresenter.RoomCategoryKey] = "Room",
                [MvpDungeonPlacementPresenter.MonsterCategoryKey] = "Monster",
                [MvpDungeonPlacementPresenter.TrapCategoryKey] = "Trap",
                [MvpDungeonPlacementPresenter.LootNodeCategoryKey] = "Loot node",
                [MvpDungeonPlacementPresenter.BasicRoomOptionKey] = "Basic Room",
                [MvpDungeonPlacementPresenter.NarrowHallOptionKey] = "Narrow Hall",
                [MvpDungeonPlacementPresenter.SkeletonOptionKey] = "Skeleton",
                [MvpDungeonPlacementPresenter.GoblinOptionKey] = "Goblin",
                [MvpDungeonPlacementPresenter.SpikeTrapOptionKey] = "Spike Trap",
                [MvpDungeonPlacementPresenter.SnareTrapOptionKey] = "Snare Trap",
                [MvpDungeonPlacementPresenter.BasicLootNodeOptionKey] = "Basic Loot Node",
                [MvpDungeonPlacementPresenter.HiddenCacheOptionKey] = "Hidden Cache",
                [MvpDungeonPlacementPresenter.EntryFormatKey] = "{0}: {1}",
                [MvpDungeonPlacementPresenter.SeparatorKey] = "; ",
                [MvpDungeonPlacementPresenter.BasicRoomPreviewKey] = "Role: adds room space and path context.",
                [MvpPlacementEffectsPresenter.EmptyKey] = "No placement effects",
                [MvpPlacementEffectsPresenter.CombinedFormatKey] = "{0}",
                [MvpPlacementEffectsPresenter.DetailSeparatorKey] = "; ",
                [MvpPlacementEffectsPresenter.PathCapacityFormatKey] = "path capacity {0}",
                [MvpPlacementEffectsPresenter.DangerFormatKey] = "danger {0}",
                [MvpPlacementEffectsPresenter.ManaPressureFormatKey] = "mana pressure {0}",
                [MvpPlacementEffectsPresenter.HeatPressureFormatKey] = "heat pressure {0}",
                [MvpPlacementEffectsPresenter.LootBonusFormatKey] = "loot bonus {0}",
                [MvpPlacementEffectsPresenter.AttractionFormatKey] = "attraction {0}",
                [MvpPlacementEffectsPresenter.ExplanationFormatKey] = "{0} ({1})",
                ["effect.room"] = "room effect",
                ["effect.monster"] = "monster effect",
                ["effect.trap"] = "trap effect",
                ["effect.loot"] = "loot effect"
            };

            return map.TryGetValue(key, out string value) ? value : fallback;
        }
    }
}
