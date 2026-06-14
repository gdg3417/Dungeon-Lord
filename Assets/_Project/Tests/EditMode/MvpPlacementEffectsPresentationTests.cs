using System.Collections.Generic;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpPlacementEffectsPresentationTests
    {
        [Test]
        public void LoopSummaryPanel_ShowsCombinedPlacementEffectsWithoutRawIds()
        {
            MvpPlayerLoopSummary summary = SummaryWithEffects();

            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, Localized);

            Assert.That(text, Does.Contain("Effects: path +2, danger +5, mana +2, heat +1, loot +4, attraction +2"));
            Assert.That(text, Does.Contain("Room path"));
            Assert.That(text, Does.Contain("Skeleton pressure"));
            Assert.That(text, Does.Not.Contain("placement.category"));
            Assert.That(text, Does.Not.Contain("placement.option"));
            Assert.That(text, Does.Not.Contain("ui.mvp"));
        }

        [Test]
        public void RunFeedback_ShowsPlacementEffectsImpactWithoutRawIds()
        {
            string text = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                new MvpPlayerLoopSummary { RuleResolved = true },
                SummaryWithEffects(hasRun: true),
                true,
                Localized);

            Assert.That(text, Does.Contain("Placement impact: path +2, danger +5, mana +2, heat +1, loot +4, attraction +2"));
            Assert.That(text, Does.Not.Contain("placement.category"));
            Assert.That(text, Does.Not.Contain("placement.option"));
            Assert.That(text, Does.Not.Contain("ui.mvp"));
            Assert.That(text, Does.Not.Contain("run-test"));
        }

        [Test]
        public void Presentation_RequestsLocalizationKeysForNewVisiblePlacementEffectsText()
        {
            var requestedKeys = new List<string>();

            MvpLoopSummaryPanelPresenter.BuildPanelText(SummaryWithEffects(), (key, fallback) =>
            {
                requestedKeys.Add(key);
                return Localized(key, fallback);
            });

            Assert.That(requestedKeys, Does.Contain(MvpLoopSummaryPanelPresenter.PlacementEffectsFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpPlacementEffectsPresenter.PathCapacityFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpPlacementEffectsPresenter.DangerFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpPlacementEffectsPresenter.ManaPressureFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpPlacementEffectsPresenter.HeatPressureFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpPlacementEffectsPresenter.LootBonusFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpPlacementEffectsPresenter.AttractionFormatKey));
            Assert.That(requestedKeys, Does.Contain("effect.room"));
            Assert.That(requestedKeys, Does.Contain("effect.monster"));
        }

        private static MvpPlayerLoopSummary SummaryWithEffects(bool hasRun = false)
        {
            return new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasPlacementContext = true,
                DungeonPlacements = new[]
                {
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId, 1),
                    new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 2)
                },
                PlacementEffects = EffectsSummary(),
                LatestRunPlacementEffects = EffectsSummary(),
                HasRunOutcome = hasRun,
                LatestRunId = "run-test",
                RunSucceeded = true,
                ManaReserve = 3d,
                LootGeneratedWorldValue = 4,
                LootExtractedWorldValue = 4,
                LootExtractedTradeableWorldValue = 2,
                HeatBefore = 1d,
                HeatAfter = 1d,
                NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey
            };
        }

        private static MvpPlacementEffectsSummary EffectsSummary()
        {
            return new MvpPlacementEffectsSummary
            {
                RuleResolved = true,
                PathCapacity = 2,
                Danger = 5,
                ManaPressure = 2,
                HeatPressure = 1,
                LootBonus = 4,
                Attraction = 2,
                EffectLocalizationKeys = new[] { "effect.room", "effect.monster" }
            };
        }

        private static string Localized(string key, string fallback)
        {
            var map = new Dictionary<string, string>
            {
                [MvpLoopSummaryPanelPresenter.TitleKey] = "Loop",
                [MvpLoopSummaryPanelPresenter.AdventurerIntentSectionKey] = "Adventurer intent",
                [AdventurerRunIntentPresenter.SummaryFormatKey] = "Adventurer intent: {0} likely. Reason: {1}",
                [AdventurerRunIntentResolver.ReasonFallbackKey] = "current dungeon signals are still forming",
                ["run.posture.balanced.name"] = "Balanced",
                [MvpLoopSummaryPanelPresenter.CompositionFormatKey] = "Composition: {0}",
                [MvpLoopSummaryPanelPresenter.LatestRunSectionKey] = "Latest Run",
                [MvpLoopSummaryPanelPresenter.LatestRunFormatKey] = "Run: {0}",
                [MvpLoopSummaryPanelPresenter.PlacementEffectsFormatKey] = "Effects: {0}",
                [MvpLoopSummaryPanelPresenter.ManaFormatKey] = "Mana: {0:0.##}",
                [MvpLoopSummaryPanelPresenter.LootFormatKey] = "Loot: {0}/{1}/{2}",
                [MvpLoopSummaryPanelPresenter.HeatFormatKey] = "Heat: {0}->{1} {2} {3}",
                [MvpLoopSummaryPanelPresenter.ResearchFormatKey] = "{0}",
                [MvpLoopSummaryPanelPresenter.SuggestionFormatKey] = "Suggestion: {0}",
                [MvpLoopSummaryPanelPresenter.ValueNoRunKey] = "no run",
                [MvpLoopSummaryPanelPresenter.ValueUnknownKey] = "unknown",
                [MvpLoopSummaryPanelPresenter.ValueNoResearchKey] = "no research",
                [MvpLoopSummaryPanelPresenter.RunSucceededKey] = "succeeded",
                [MvpLoopSummaryPanelPresenter.CurrentDungeonSectionKey] = "Current Dungeon",
                [MvpLoopSummaryPanelPresenter.WhyItHappenedSectionKey] = "Why It Happened",
                [MvpLoopSummaryPanelPresenter.RewardsAndRiskSectionKey] = "Rewards and Risk",
                [MvpLoopSummaryPanelPresenter.ResearchSectionKey] = "Research",
                [MvpLoopSummaryPanelPresenter.SuggestedNextActionSectionKey] = "Suggested Next Action",
                [MvpLoopSummaryPanelPresenter.SectionLineFormatKey] = "{0}: {1}",
                [MvpLoopSummaryPanelPresenter.InlineSeparatorKey] = " | ",
                [MvpLoopSummaryPanelPresenter.RunOutcomeLineFormatKey] = "{0}. Party: {1}",
                [MvpLoopSummaryPanelPresenter.WhyNoRunKey] = "no run reason",
                [MvpLoopSummaryPanelPresenter.WhyRunFormatKey] = "Main reason: {0}.",
                [MvpLoopSummaryPanelPresenter.WhyDangerKey] = "danger pressure drove the result",
                [MvpLoopSummaryPanelPresenter.WhyMixedKey] = "the current placement mix shaped the result",
                [MvpLoopSummaryPanelPresenter.RiskNoRunKey] = "risk after run",
                [MvpLoopSummaryPanelPresenter.RiskStableKey] = "risk steady",
                [MvpLoopSummaryPanelPresenter.RiskIncreasedKey] = "risk up",
                [MvpLoopSummaryPanelPresenter.RiskReducedKey] = "risk down",
                [MvpPlayerLoopSummaryPresenter.SuggestRepeatOrImprovePlacementKey] = "repeat",
                [MvpDungeonPlacementPresenter.RoomCategoryKey] = "Room",
                [MvpDungeonPlacementPresenter.MonsterCategoryKey] = "Monster",
                [MvpDungeonPlacementPresenter.BasicRoomOptionKey] = "Basic Room",
                [MvpDungeonPlacementPresenter.NarrowHallOptionKey] = "Narrow Hall",
                [MvpDungeonPlacementPresenter.SkeletonOptionKey] = "Skeleton",
                [MvpDungeonPlacementPresenter.EntryFormatKey] = "{0}: {1}",
                [MvpDungeonPlacementPresenter.SeparatorKey] = "; ",
                [MvpPlacementEffectsPresenter.DetailSeparatorKey] = ", ",
                [MvpPlacementEffectsPresenter.PathCapacityFormatKey] = "path +{0}",
                [MvpPlacementEffectsPresenter.DangerFormatKey] = "danger +{0}",
                [MvpPlacementEffectsPresenter.ManaPressureFormatKey] = "mana +{0}",
                [MvpPlacementEffectsPresenter.HeatPressureFormatKey] = "heat +{0}",
                [MvpPlacementEffectsPresenter.LootBonusFormatKey] = "loot +{0}",
                [MvpPlacementEffectsPresenter.AttractionFormatKey] = "attraction +{0}",
                [MvpPlacementEffectsPresenter.ExplanationFormatKey] = "{0} ({1})",
                ["effect.room"] = "Room path",
                ["effect.monster"] = "Skeleton pressure",
                [MvpRunResultFeedbackPresenter.SuccessStableHeatKey] = "Run result: stable.",
                [MvpRunResultFeedbackPresenter.OutcomeCueControlledLootKey] = "Outcome: loot controlled.",
                [MvpRunResultFeedbackPresenter.OutcomeCueFormatKey] = "{0} {1}",
                [MvpRunResultFeedbackPresenter.FormatKey] = "{0} Mana {1}. Loot {2}/{3}/{4}. Heat {5}->{6}.",
                [MvpRunResultFeedbackPresenter.PlacementEffectsImpactFormatKey] = "{0} Placement impact: {1}."
            };

            return map.TryGetValue(key, out string value) ? value : fallback;
        }
    }
}
