using System.Collections.Generic;
using System.Reflection;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
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
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rootObject);
        }

        [Test]
        public void PlacementModel_InitializesWithExactlyFourCategoriesAndStarterOptions()
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
        }

        [TestCase(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)]
        [TestCase(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId)]
        [TestCase(MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId)]
        [TestCase(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId)]
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

            foreach (string optionId in MvpDungeonPlacementIds.OrderedStarterOptionIds)
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

        private static string Localized(string key, string fallback)
        {
            var map = new Dictionary<string, string>
            {
                [MvpLoopSummaryPanelPresenter.TitleKey] = "MVP Loop Summary",
                [MvpLoopSummaryPanelPresenter.CompositionFormatKey] = "Dungeon composition: {0}",
                [MvpLoopSummaryPanelPresenter.LatestRunFormatKey] = "Latest run: {0}",
                [MvpLoopSummaryPanelPresenter.ManaFormatKey] = "Mana reserve: {0:0.##}",
                [MvpLoopSummaryPanelPresenter.LootFormatKey] = "Loot: generated {0}, extracted {1}, tradeable {2}",
                [MvpLoopSummaryPanelPresenter.HeatFormatKey] = "Heat: {0:0.##} -> {1:0.##} ({2})",
                [MvpLoopSummaryPanelPresenter.ResearchFormatKey] = "Research: {0}",
                [MvpLoopSummaryPanelPresenter.SuggestionFormatKey] = "Next: {0}",
                [MvpLoopSummaryPanelPresenter.ValueNoRunKey] = "No run yet",
                [MvpLoopSummaryPanelPresenter.ValueUnknownKey] = "Unknown",
                [MvpLoopSummaryPanelPresenter.ValueNoResearchKey] = "No research",
                [MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey] = "Run the dungeon.",
                [MvpStructurePlacementFeedbackPresenter.EmptySlotKey] = "Empty slot",
                [MvpStructurePlacementFeedbackPresenter.PlacementChangedFormatKey] = "Changed placement: {0} -> {1}: {2}. {3}",
                [MvpDungeonPlacementPresenter.RoomCategoryKey] = "Room",
                [MvpDungeonPlacementPresenter.MonsterCategoryKey] = "Monster",
                [MvpDungeonPlacementPresenter.TrapCategoryKey] = "Trap",
                [MvpDungeonPlacementPresenter.LootNodeCategoryKey] = "Loot node",
                [MvpDungeonPlacementPresenter.BasicRoomOptionKey] = "Basic Room",
                [MvpDungeonPlacementPresenter.SkeletonOptionKey] = "Skeleton",
                [MvpDungeonPlacementPresenter.SpikeTrapOptionKey] = "Spike Trap",
                [MvpDungeonPlacementPresenter.BasicLootNodeOptionKey] = "Basic Loot Node",
                [MvpDungeonPlacementPresenter.EntryFormatKey] = "{0}: {1}",
                [MvpDungeonPlacementPresenter.SeparatorKey] = "; ",
                [MvpDungeonPlacementPresenter.BasicRoomPreviewKey] = "Role: adds room space and path context."
            };

            return map.TryGetValue(key, out string value) ? value : fallback;
        }
    }
}
