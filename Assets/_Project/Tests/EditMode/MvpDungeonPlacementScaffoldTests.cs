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
                map[MvpDungeonPlacementPresenter.LootNodeCategoryKey] = "Loot node";
                map[MvpDungeonPlacementPresenter.NarrowHallOptionKey] = "Narrow Hall";
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

        private static string Localized(string key, string fallback)
        {
            var map = new Dictionary<string, string>
            {
                [MvpLoopSummaryPanelPresenter.TitleKey] = "MVP Loop Summary",
                [MvpLoopSummaryPanelPresenter.AdventurerIntentSectionKey] = "Expected Next Adventurer Intent",
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
                [MvpStructurePlacementFeedbackPresenter.PlacementChangedFormatKey] = "Changed placement: {0} -> {1}: {2}. {3}",
                [MvpRoomSlotTargetPresenter.SelectedTargetFormatKey] = "Selected room target: Room {0}: {1}",
                [MvpRoomSlotTargetPresenter.NoValidSlotFormatKey] = "No valid {0} slot in Room {1}: {2}.",
                ["ui.mvp_room_slots.cycle_target_button"] = "Cycle room target",
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
