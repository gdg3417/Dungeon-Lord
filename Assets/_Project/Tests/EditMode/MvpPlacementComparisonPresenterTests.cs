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

        private static RunSimulationConfig Config()
        {
            return new RunSimulationConfig
            {
                MvpPlacementEffects = new[]
                {
                    Effect(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId, 2, 0, 0, 0, 0, 0),
                    Effect(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.NarrowHallOptionId, 1, 0, 0, 0, 0, 0),
                    Effect(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 0, 3, 2, 0, 0, 0),
                    Effect(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId, 0, 2, 1, 0, 1, 1),
                    Effect(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId, 0, 2, 0, 1, 0, 0),
                    Effect(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SnareTrapOptionId, 0, 1, 0, 0, 0, 0),
                    Effect(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId, 0, 0, 0, 0, 4, 2),
                    Effect(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId, 0, 0, 0, 0, 3, 1)
                }
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
