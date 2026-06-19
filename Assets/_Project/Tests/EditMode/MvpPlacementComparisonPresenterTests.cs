using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpPlacementComparisonPresenterTests
    {
        [TestCase(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId, MvpDungeonPlacementIds.NarrowHallOptionId, MvpPlacementComparisonPresenter.BasicRoomToNarrowHallSummaryKey, -1, 0, 0, 0, 0, 0)]
        [TestCase(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, MvpDungeonPlacementIds.GoblinOptionId, MvpPlacementComparisonPresenter.SkeletonToGoblinSummaryKey, 0, -1, -1, 0, 1, 1)]
        [TestCase(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId, MvpDungeonPlacementIds.SnareTrapOptionId, MvpPlacementComparisonPresenter.SpikeTrapToSnareTrapSummaryKey, 0, -1, 0, -1, 0, 0)]
        [TestCase(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId, MvpDungeonPlacementIds.HiddenCacheOptionId, MvpPlacementComparisonPresenter.BasicLootNodeToHiddenCacheSummaryKey, 0, 0, 0, 0, -1, -1)]
        [TestCase(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId, MvpDungeonPlacementIds.GlitteringHoardOptionId, MvpPlacementComparisonPresenter.BasicLootNodeToGlitteringHoardSummaryKey, 0, 0, 0, 1, 2, 2)]
        [TestCase(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.GlitteringHoardOptionId, MvpDungeonPlacementIds.BasicLootNodeOptionId, MvpPlacementComparisonPresenter.GlitteringHoardToBasicLootNodeSummaryKey, 0, 0, 0, -1, -2, -2)]
        public void Resolve_AlternativeAgainstPlacedStarter_ReturnsOrderedConfigDeltas(string categoryId, string baselineOptionId, string selectedOptionId, string expectedKey, int path, int danger, int mana, int heat, int loot, int attraction)
        {
            var placements = new MvpDungeonPlacementState();
            placements.Entries.Add(new MvpDungeonPlacementEntry(categoryId, baselineOptionId, 1));

            MvpPlacementComparisonPreview preview = MvpPlacementComparisonPresenter.Resolve(null, placements, Config(), categoryId, selectedOptionId);

            Assert.That(preview.HasComparison, Is.True);
            Assert.That(preview.BaselineOptionId, Is.EqualTo(baselineOptionId));
            Assert.That(preview.SelectedOptionId, Is.EqualTo(selectedOptionId));
            Assert.That(preview.ComparisonSummaryKey, Is.EqualTo(expectedKey));
            Assert.That(preview.DeltaPathCapacity, Is.EqualTo(path));
            Assert.That(preview.DeltaDanger, Is.EqualTo(danger));
            Assert.That(preview.DeltaManaPressure, Is.EqualTo(mana));
            Assert.That(preview.DeltaHeatPressure, Is.EqualTo(heat));
            Assert.That(preview.DeltaLoot, Is.EqualTo(loot));
            Assert.That(preview.DeltaAttraction, Is.EqualTo(attraction));
        }

        [Test]
        public void BuildComparisonText_LocalizesBaselineAndSummaryWithoutRawIds()
        {
            var preview = new MvpPlacementComparisonPreview
            {
                HasComparison = true,
                BaselineOptionId = MvpDungeonPlacementIds.SkeletonOptionId,
                SelectedOptionId = MvpDungeonPlacementIds.GoblinOptionId,
                ComparisonSummaryKey = MvpPlacementComparisonPresenter.SkeletonToGoblinSummaryKey
            };

            string text = MvpPlacementComparisonPresenter.BuildComparisonText(preview, Localized);

            Assert.That(text, Is.EqualTo("Compared with Skeleton: less danger and mana pressure, adds a small loot signal."));
            Assert.That(text, Does.Not.Contain("placement.option"));
            Assert.That(text, Does.Not.Contain("ui.mvp"));
        }

        [Test]
        public void Resolve_EmptyPlacement_UsesStarterBaselineForAlternative()
        {
            MvpPlacementComparisonPreview preview = MvpPlacementComparisonPresenter.Resolve(null, new MvpDungeonPlacementState(), Config(), MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SnareTrapOptionId);

            Assert.That(preview.HasComparison, Is.True);
            Assert.That(preview.BaselineOptionId, Is.EqualTo(MvpDungeonPlacementIds.SpikeTrapOptionId));
        }

        [Test]
        public void Resolve_MissingConfigUnknownOptionOrSameStarter_FallsBackSafely()
        {
            Assert.That(MvpPlacementComparisonPresenter.Resolve(null, null, null, MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId).HasComparison, Is.False);
            Assert.That(MvpPlacementComparisonPresenter.Resolve(null, null, Config(), MvpDungeonPlacementIds.RoomCategoryId, "unknown").HasComparison, Is.False);
            Assert.That(MvpPlacementComparisonPresenter.Resolve(null, new MvpDungeonPlacementState(), Config(), MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId).HasComparison, Is.False);
        }



        [Test]
        public void Resolve_RoomSlotComparison_UsesSelectedRoomBasicRoomBaseline()
        {
            SaveData save = SaveWithRoomSlots(0,
                Room(MvpDungeonPlacementIds.BasicRoomOptionId, string.Empty, string.Empty, string.Empty));

            MvpPlacementComparisonPreview preview = MvpPlacementComparisonPresenter.Resolve(save, Config(), 0, MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId);

            Assert.That(preview.HasComparison, Is.True);
            Assert.That(preview.BaselineOptionId, Is.EqualTo(MvpDungeonPlacementIds.BasicRoomOptionId));
            Assert.That(preview.ComparisonSummaryKey, Is.EqualTo(MvpPlacementComparisonPresenter.BasicRoomToNarrowHallSummaryKey));
            Assert.That(preview.DeltaPathCapacity, Is.EqualTo(-1));
        }

        [Test]
        public void Resolve_RoomSlotComparison_UsesSelectedRoomNarrowHallBaseline()
        {
            SaveData save = SaveWithRoomSlots(0,
                Room(MvpDungeonPlacementIds.NarrowHallOptionId, string.Empty, string.Empty, string.Empty));

            MvpPlacementComparisonPreview preview = MvpPlacementComparisonPresenter.Resolve(save, Config(), 0, MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId);

            Assert.That(preview.HasComparison, Is.True);
            Assert.That(preview.BaselineOptionId, Is.EqualTo(MvpDungeonPlacementIds.NarrowHallOptionId));
            Assert.That(preview.ComparisonSummaryKey, Is.EqualTo(MvpPlacementComparisonPresenter.NarrowHallToBasicRoomSummaryKey));
            Assert.That(preview.DeltaPathCapacity, Is.EqualTo(1));
        }

        [Test]
        public void Resolve_RoomSlotComparison_SameRoomToSameRoomReturnsNoComparison()
        {
            SaveData save = SaveWithRoomSlots(0,
                Room(MvpDungeonPlacementIds.BasicRoomOptionId, string.Empty, string.Empty, string.Empty));

            MvpPlacementComparisonPreview preview = MvpPlacementComparisonPresenter.Resolve(save, Config(), 0, MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId);

            Assert.That(preview.HasComparison, Is.False);
        }

        [Test]
        public void Resolve_RoomSlotComparison_UsesSelectedRoomOneMonsterBaseline()
        {
            SaveData save = SaveWithRoomSlots(0,
                Room(MvpDungeonPlacementIds.BasicRoomOptionId, MvpDungeonPlacementIds.SkeletonOptionId, string.Empty, string.Empty),
                Room(MvpDungeonPlacementIds.BasicRoomOptionId, MvpDungeonPlacementIds.GoblinOptionId, string.Empty, string.Empty));

            MvpPlacementComparisonPreview preview = MvpPlacementComparisonPresenter.Resolve(save, Config(), 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);

            Assert.That(preview.HasComparison, Is.True);
            Assert.That(preview.BaselineOptionId, Is.EqualTo(MvpDungeonPlacementIds.SkeletonOptionId));
            Assert.That(preview.ComparisonSummaryKey, Is.EqualTo(MvpPlacementComparisonPresenter.SkeletonToGoblinSummaryKey));
            Assert.That(preview.DeltaDanger, Is.EqualTo(-1));
            Assert.That(preview.DeltaManaPressure, Is.EqualTo(-1));
            Assert.That(preview.DeltaLoot, Is.EqualTo(1));
        }

        [Test]
        public void Resolve_RoomSlotComparison_UsesSelectedRoomOneTrapBaseline()
        {
            SaveData save = SaveWithRoomSlots(0,
                Room(MvpDungeonPlacementIds.BasicRoomOptionId, string.Empty, MvpDungeonPlacementIds.SnareTrapOptionId, string.Empty));

            MvpPlacementComparisonPreview preview = MvpPlacementComparisonPresenter.Resolve(save, Config(), 0, MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId);

            Assert.That(preview.HasComparison, Is.True);
            Assert.That(preview.BaselineOptionId, Is.EqualTo(MvpDungeonPlacementIds.SnareTrapOptionId));
            Assert.That(preview.ComparisonSummaryKey, Is.EqualTo(MvpPlacementComparisonPresenter.SnareTrapToSpikeTrapSummaryKey));
            Assert.That(preview.DeltaDanger, Is.EqualTo(1));
            Assert.That(preview.DeltaHeatPressure, Is.EqualTo(1));
        }

        [Test]
        public void Resolve_RoomSlotComparison_UsesSelectedRoomTwoLootBaseline()
        {
            SaveData save = SaveWithRoomSlots(1,
                Room(MvpDungeonPlacementIds.BasicRoomOptionId, MvpDungeonPlacementIds.SkeletonOptionId, string.Empty, string.Empty),
                Room(MvpDungeonPlacementIds.BasicRoomOptionId, string.Empty, string.Empty, MvpDungeonPlacementIds.BasicLootNodeOptionId));

            MvpPlacementComparisonPreview preview = MvpPlacementComparisonPresenter.Resolve(save, Config(), 1, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);

            Assert.That(preview.HasComparison, Is.True);
            Assert.That(preview.BaselineOptionId, Is.EqualTo(MvpDungeonPlacementIds.BasicLootNodeOptionId));
            Assert.That(preview.ComparisonSummaryKey, Is.EqualTo(MvpPlacementComparisonPresenter.BasicLootNodeToHiddenCacheSummaryKey));
            Assert.That(preview.DeltaLoot, Is.EqualTo(-1));
            Assert.That(preview.DeltaAttraction, Is.EqualTo(-1));
        }


        [Test]
        public void Resolve_RoomSlotComparison_EmptySelectedRoomTwoMonsterSlotReturnsNoComparison()
        {
            SaveData save = SaveWithRoomSlots(1,
                Room(MvpDungeonPlacementIds.BasicRoomOptionId, MvpDungeonPlacementIds.SkeletonOptionId, string.Empty, string.Empty),
                Room(MvpDungeonPlacementIds.BasicRoomOptionId, string.Empty, string.Empty, string.Empty));
            save.mvpDungeonPlacements = new MvpDungeonPlacementState();
            save.mvpDungeonPlacements.Entries.Add(new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 1));

            MvpPlacementComparisonPreview preview = MvpPlacementComparisonPresenter.Resolve(save, Config(), 1, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);

            Assert.That(preview.HasComparison, Is.False);
            Assert.That(preview.BaselineOptionId, Is.Null.Or.Empty);
        }

        [Test]
        public void Resolve_RoomSlotComparison_EmptySelectedRoomTwoLootSlotReturnsNoComparison()
        {
            SaveData save = SaveWithRoomSlots(1,
                Room(MvpDungeonPlacementIds.BasicRoomOptionId, string.Empty, string.Empty, MvpDungeonPlacementIds.BasicLootNodeOptionId),
                Room(MvpDungeonPlacementIds.BasicRoomOptionId, string.Empty, string.Empty, string.Empty));
            save.mvpDungeonPlacements = new MvpDungeonPlacementState();
            save.mvpDungeonPlacements.Entries.Add(new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId, 1));

            MvpPlacementComparisonPreview preview = MvpPlacementComparisonPresenter.Resolve(save, Config(), 1, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);

            Assert.That(preview.HasComparison, Is.False);
            Assert.That(preview.BaselineOptionId, Is.Null.Or.Empty);
        }

        [Test]
        public void Resolve_RoomSlotComparison_FallsBackToLegacyGlobalWhenNoRoomSlotAssignmentExists()
        {
            SaveData save = new SaveData { mvpDungeonPlacements = new MvpDungeonPlacementState() };
            save.mvpDungeonPlacements.Entries.Add(new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 1));

            MvpPlacementComparisonPreview preview = MvpPlacementComparisonPresenter.Resolve(save, Config(), 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);

            Assert.That(preview.HasComparison, Is.True);
            Assert.That(preview.BaselineOptionId, Is.EqualTo(MvpDungeonPlacementIds.SkeletonOptionId));
        }

        [Test]
        public void Resolve_RoomSlotComparison_SameToSameReturnsNoComparison()
        {
            SaveData save = SaveWithRoomSlots(0,
                Room(MvpDungeonPlacementIds.BasicRoomOptionId, MvpDungeonPlacementIds.SkeletonOptionId, string.Empty, string.Empty));

            MvpPlacementComparisonPreview preview = MvpPlacementComparisonPresenter.Resolve(save, Config(), 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);

            Assert.That(preview.HasComparison, Is.False);
        }

        [Test]
        public void Resolve_RoomSlotComparison_InvalidCategoryForSelectedRoomReturnsNoComparison()
        {
            SaveData save = SaveWithRoomSlots(0,
                Room(MvpDungeonPlacementIds.NarrowHallOptionId, string.Empty, string.Empty, string.Empty));
            save.mvpDungeonPlacements = new MvpDungeonPlacementState();
            save.mvpDungeonPlacements.Entries.Add(new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 1));

            MvpPlacementComparisonPreview preview = MvpPlacementComparisonPresenter.Resolve(save, Config(), 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);

            Assert.That(preview.HasComparison, Is.False);
        }

        private static RunSimulationConfig Config()
        {
            return new RunSimulationConfig
            {
                MvpRoomSlotCapacities = new[]
                {
                    new MvpRoomSlotCapacityConfig { RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId, MonsterCapacity = 1, TrapCapacity = 1, LootCapacity = 1 },
                    new MvpRoomSlotCapacityConfig { RoomOptionId = MvpDungeonPlacementIds.NarrowHallOptionId, MonsterCapacity = 0, TrapCapacity = 1, LootCapacity = 0 }
                },
                MvpPlacementEffects = new[]
                {
                    Effect(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId, 2, 0, 0, 0, 0, 0),
                    Effect(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId, 1, 0, 0, 0, 0, 0),
                    Effect(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 0, 3, 2, 0, 0, 0),
                    Effect(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId, 0, 2, 1, 0, 1, 1),
                    Effect(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId, 0, 2, 0, 1, 0, 0),
                    Effect(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SnareTrapOptionId, 0, 1, 0, 0, 0, 0),
                    Effect(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId, 0, 0, 0, 0, 4, 2),
                    Effect(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId, 0, 0, 0, 0, 3, 1),
                    Effect(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.GlitteringHoardOptionId, 0, 0, 0, 1, 6, 4)
                }
            };
        }


        private static SaveData SaveWithRoomSlots(int selectedRoomIndex, params MvpRoomSlotAssignmentState[] rooms)
        {
            var save = new SaveData
            {
                mvpSelectedRoomSlotIndex = selectedRoomIndex,
                mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection()
            };

            for (int i = 0; i < rooms.Length; i++)
            {
                rooms[i].FloorIndex = 0;
                rooms[i].RoomIndex = i;
                save.mvpRoomSlotAssignments.Rooms.Add(rooms[i]);
            }

            return save;
        }

        private static MvpRoomSlotAssignmentState Room(string roomOptionId, string monsterOptionId, string trapOptionId, string lootNodeOptionId)
        {
            return new MvpRoomSlotAssignmentState
            {
                RoomOptionId = roomOptionId,
                MonsterOptionIds = string.IsNullOrWhiteSpace(monsterOptionId) ? System.Array.Empty<string>() : new[] { monsterOptionId },
                TrapOptionIds = string.IsNullOrWhiteSpace(trapOptionId) ? System.Array.Empty<string>() : new[] { trapOptionId },
                LootNodeOptionIds = string.IsNullOrWhiteSpace(lootNodeOptionId) ? System.Array.Empty<string>() : new[] { lootNodeOptionId }
            };
        }

        private static MvpPlacementEffectConfig Effect(string categoryId, string optionId, int path, int danger, int mana, int heat, int loot, int attraction)
        {
            return new MvpPlacementEffectConfig { CategoryId = categoryId, OptionId = optionId, PathCapacity = path, Danger = danger, ManaPressure = mana, HeatPressure = heat, LootBonus = loot, Attraction = attraction };
        }

        private static string Localized(string key, string fallback)
        {
            switch (key)
            {
                case MvpPlacementComparisonPresenter.ComparedWithFormatKey: return "Compared with {0}: {1}";
                case MvpDungeonPlacementPresenter.SkeletonOptionKey: return "Skeleton";
                case MvpPlacementComparisonPresenter.SkeletonToGoblinSummaryKey: return "less danger and mana pressure, adds a small loot signal.";
                default: return fallback;
            }
        }
    }
}
