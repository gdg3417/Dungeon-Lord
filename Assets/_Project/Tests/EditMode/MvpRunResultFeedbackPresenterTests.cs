using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using System.Collections.Generic;

namespace DungeonBuilder.Tests.EditMode
{
    public class MvpRunResultFeedbackPresenterTests
    {
        [Test]
        public void SuccessfulRunWithStableHeat_UsesLocalizedInterpretationAndSummaryValues()
        {
            string text = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                Summary(hasRun: false),
                Summary(runSucceeded: true, mana: 12d, generatedLoot: 7, extractedLoot: 5, tradeableLoot: 3, heatBefore: 4d, heatAfter: 4d),
                didRun: true,
                Localized);

            Assert.That(text, Is.EqualTo("Run result: succeeded. Loot extracted, heat stable. Mana 12. Loot 7/5/3. Heat 4->4. Outcome cue: loot landed while heat stayed controlled."));
        }

        [Test]
        public void SuccessfulRunWithLowerHeat_UsesReducedHeatInterpretation()
        {
            string text = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                Summary(hasRun: false),
                Summary(runSucceeded: true, mana: 8d, generatedLoot: 6, extractedLoot: 4, tradeableLoot: 2, heatBefore: 9d, heatAfter: 6d),
                didRun: true,
                Localized);

            Assert.That(text, Is.EqualTo("Run result: succeeded. Loot extracted, heat reduced. Mana 8. Loot 6/4/2. Heat 9->6. Outcome cue: loot landed while heat stayed controlled."));
        }

        [Test]
        public void SuccessfulRunWithHigherHeat_UsesIncreasedHeatInterpretation()
        {
            string text = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                Summary(hasRun: false),
                Summary(runSucceeded: true, mana: 5d, generatedLoot: 9, extractedLoot: 6, tradeableLoot: 4, heatBefore: 7d, heatAfter: 11d),
                didRun: true,
                Localized);

            Assert.That(text, Is.EqualTo("Run result: succeeded. Loot extracted, heat increased. Mana 5. Loot 9/6/4. Heat 7->11. Outcome cue: heat pressure rose; consider a safer posture or heat control."));
        }

        [Test]
        public void FailedRun_UsesLocalizedFailureInterpretationAndDoesNotExposeRawIds()
        {
            string text = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                Summary(hasRun: true, latestRunId: "run-prior", selectedStructureId: StructureSimulationPass.ManaGeneratorBasicId),
                Summary(runSucceeded: false, latestRunId: "run-debug-raw", selectedStructureId: StructureSimulationPass.HeatScrubberBasicId, mana: 2d, generatedLoot: 1, extractedLoot: 0, tradeableLoot: 0, heatBefore: 12d, heatAfter: 14d),
                didRun: true,
                Localized);

            Assert.That(text, Is.EqualTo("Run result: failed. Review placement and try again. Mana 2. Loot 1/0/0. Heat 12->14. Outcome cue: the run failed, so reduce pressure before trying again."));
            Assert.That(text, Does.Not.Contain("run-debug-raw"));
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.HeatScrubberBasicId));
            Assert.That(text, Does.Not.Contain("ui.mvp_run_feedback"));
            Assert.That(text, Does.Not.Contain("run.posture"));
            Assert.That(text, Does.Not.Contain("run.heat_delta.rule.test"));
        }


        [Test]
        public void Feedback_WithResolvedPartyPreview_IncludesLocalizedPartyAndNoRawClassIds()
        {
            string text = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                Summary(hasRun: false),
                Summary(runSucceeded: true, mana: 12d, generatedLoot: 7, extractedLoot: 5, tradeableLoot: 3, heatBefore: 4d, heatAfter: 4d, partyClassIds: new[]
                {
                    AdventurerPartyCompositionResolver.WarriorClassId,
                    AdventurerPartyCompositionResolver.RogueClassId
                }),
                didRun: true,
                Localized);

            Assert.That(text, Is.EqualTo("Run result: succeeded. Loot extracted, heat stable. Mana 12. Loot 7/5/3. Heat 4->4. Adventurers: Warrior, Rogue Outcome cue: loot landed while heat stayed controlled."));
            Assert.That(text, Does.Not.Contain("adventurer.class."));
        }

        [Test]
        public void Feedback_WithPosture_UsesLocalizedPostureNameAndDoesNotExposeRawPostureId()
        {
            string text = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                Summary(hasRun: false),
                Summary(runSucceeded: true, mana: 12d, generatedLoot: 7, extractedLoot: 5, tradeableLoot: 3, heatBefore: 4d, heatAfter: 4d),
                didRun: true,
                Localized,
                MinimalMvpActionPanelPresenter.GreedyPostureKey);

            Assert.That(text, Is.EqualTo("Posture: Greedy. Run result: succeeded. Loot extracted, heat stable. Mana 12. Loot 7/5/3. Heat 4->4. Outcome cue: loot landed while heat stayed controlled."));
            Assert.That(text, Does.Not.Contain(RunPostureResolver.GreedyId));
            Assert.That(text, Does.Not.Contain("run.posture"));
        }

        [Test]
        public void UnavailableRun_UsesLocalizedFallbackWithoutFormattingRawState()
        {
            string text = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                Summary(hasRun: false),
                Summary(hasRun: false, selectedStructureId: "structure.debug.raw"),
                didRun: false,
                Localized);

            Assert.That(text, Is.EqualTo("Run result unavailable."));
            Assert.That(text, Does.Not.Contain("structure.debug.raw"));
        }


        [Test]
        public void Feedback_UsesLatestRunPlacementEffectsInsteadOfCurrentPlacementEffects_WithoutRawIds()
        {
            MvpPlayerLoopSummary summary = Summary(
                runSucceeded: true,
                latestRunId: "run.raw.latest",
                selectedStructureId: StructureSimulationPass.HeatRiskId,
                mana: 12d,
                generatedLoot: 7,
                extractedLoot: 5,
                tradeableLoot: 3,
                heatBefore: 4d,
                heatAfter: 4d,
                partyClassIds: new[] { AdventurerPartyCompositionResolver.WarriorClassId });
            summary.PlacementEffects = CurrentPlacementEffects();
            summary.LatestRunPlacementEffects = LatestRunPlacementEffects();

            string text = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                Summary(hasRun: false),
                summary,
                didRun: true,
                Localized,
                MinimalMvpActionPanelPresenter.GreedyPostureKey);

            Assert.That(text, Does.Contain("Placement impact: danger +3 (latest trap pressure)."));
            Assert.That(text, Does.Not.Contain("current room capacity"));
            AssertNoRawIdsOrKeys(text);
        }

        [Test]
        public void Feedback_LegacyFallbackLatestRunEffects_ShowsCurrentEffectsSafelyWithoutRawIds()
        {
            MvpPlacementEffectsSummary fallbackEffects = CurrentPlacementEffects();
            MvpPlayerLoopSummary summary = Summary(
                runSucceeded: true,
                latestRunId: "run.raw.legacy",
                selectedStructureId: StructureSimulationPass.ManaGeneratorBasicId,
                mana: 12d,
                generatedLoot: 7,
                extractedLoot: 5,
                tradeableLoot: 3,
                heatBefore: 4d,
                heatAfter: 4d,
                partyClassIds: new[] { AdventurerPartyCompositionResolver.RogueClassId });
            summary.PlacementEffects = fallbackEffects;
            summary.LatestRunPlacementEffects = fallbackEffects;

            string text = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                Summary(hasRun: false),
                summary,
                didRun: true,
                Localized,
                MinimalMvpActionPanelPresenter.CautiousPostureKey);

            Assert.That(text, Does.Contain("Placement impact: path +2 (current room capacity)."));
            AssertNoRawIdsOrKeys(text);
        }

        [Test]
        public void Feedback_RequestsLocalizationKeysInsteadOfHardcodedEnglish()
        {
            var requestedKeys = new List<string>();

            MvpRunResultFeedbackPresenter.BuildFeedbackText(
                Summary(hasRun: false),
                Summary(runSucceeded: true, extractedLoot: 1, heatBefore: 1d, heatAfter: 1d),
                didRun: true,
                (key, fallback) =>
                {
                    requestedKeys.Add(key);
                    return Localized(key, fallback);
                });

            Assert.That(requestedKeys, Does.Contain(MvpRunResultFeedbackPresenter.SuccessStableHeatKey));
            Assert.That(requestedKeys, Does.Contain(MvpRunResultFeedbackPresenter.OutcomeCueControlledLootKey));
            Assert.That(requestedKeys, Does.Contain(MvpRunResultFeedbackPresenter.OutcomeCueFormatKey));
            Assert.That(requestedKeys, Does.Contain(MvpRunResultFeedbackPresenter.FormatKey));
        }


        private static MvpPlacementEffectsSummary CurrentPlacementEffects()
        {
            return new MvpPlacementEffectsSummary
            {
                RuleResolved = true,
                PathCapacity = 2,
                ContributingOptionIds = new[] { MvpDungeonPlacementIds.BasicRoomOptionId },
                EffectLocalizationKeys = new[] { "effect.current_room" }
            };
        }

        private static MvpPlacementEffectsSummary LatestRunPlacementEffects()
        {
            return new MvpPlacementEffectsSummary
            {
                RuleResolved = true,
                Danger = 3,
                ContributingOptionIds = new[] { MvpDungeonPlacementIds.SpikeTrapOptionId },
                EffectLocalizationKeys = new[] { "effect.latest_trap" }
            };
        }

        private static void AssertNoRawIdsOrKeys(string text)
        {
            Assert.That(text, Does.Not.Contain("ui.mvp_run_feedback"));
            Assert.That(text, Does.Not.Contain("ui.mvp_placement_effects"));
            Assert.That(text, Does.Not.Contain(MvpDungeonPlacementIds.BasicRoomOptionId));
            Assert.That(text, Does.Not.Contain(MvpDungeonPlacementIds.SpikeTrapOptionId));
            Assert.That(text, Does.Not.Contain(CurrentHeatTierResolver.NoticeTierId));
            Assert.That(text, Does.Not.Contain(RunPostureResolver.CautiousId));
            Assert.That(text, Does.Not.Contain(RunPostureResolver.GreedyId));
            Assert.That(text, Does.Not.Contain(AdventurerPartyCompositionResolver.WarriorClassId));
            Assert.That(text, Does.Not.Contain(AdventurerPartyCompositionResolver.RogueClassId));
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.HeatRiskId));
            Assert.That(text, Does.Not.Contain("run.raw"));
        }

        private static MvpPlayerLoopSummary Summary(
            bool hasRun = true,
            bool runSucceeded = true,
            string latestRunId = "run-test",
            string selectedStructureId = null,
            double mana = 0d,
            int generatedLoot = 0,
            int extractedLoot = 0,
            int tradeableLoot = 0,
            double heatBefore = 0d,
            double heatAfter = 0d,
            string[] partyClassIds = null)
        {
            return new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasRunOutcome = hasRun,
                LatestRunId = latestRunId,
                RunSucceeded = runSucceeded,
                SelectedStructureId = selectedStructureId,
                ManaReserve = mana,
                LootGeneratedWorldValue = generatedLoot,
                LootExtractedWorldValue = extractedLoot,
                LootExtractedTradeableWorldValue = tradeableLoot,
                HeatBefore = heatBefore,
                HeatAfter = heatAfter,
                AdventurerPartyPreviewResolved = partyClassIds != null && partyClassIds.Length > 0,
                AdventurerPartyClassIds = partyClassIds ?? System.Array.Empty<string>()
            };
        }

        private static string Localized(string key, string fallback)
        {
            var map = new Dictionary<string, string>
            {
                [MvpRunResultFeedbackPresenter.SuccessStableHeatKey] = "Run result: succeeded. Loot extracted, heat stable.",
                [MvpRunResultFeedbackPresenter.SuccessHeatReducedKey] = "Run result: succeeded. Loot extracted, heat reduced.",
                [MvpRunResultFeedbackPresenter.SuccessHeatIncreasedKey] = "Run result: succeeded. Loot extracted, heat increased.",
                [MvpRunResultFeedbackPresenter.FailedKey] = "Run result: failed. Review placement and try again.",
                [MvpRunResultFeedbackPresenter.UnavailableKey] = "Run result unavailable.",
                [MvpRunResultFeedbackPresenter.OutcomeCueFailedKey] = "Outcome cue: the run failed, so reduce pressure before trying again.",
                [MvpRunResultFeedbackPresenter.OutcomeCueHeatIncreasedKey] = "Outcome cue: heat pressure rose; consider a safer posture or heat control.",
                [MvpRunResultFeedbackPresenter.OutcomeCueControlledLootKey] = "Outcome cue: loot landed while heat stayed controlled.",
                [MvpRunResultFeedbackPresenter.OutcomeCueFormatKey] = "{0} {1}",
                [MvpRunResultFeedbackPresenter.FormatKey] = "{0} Mana {1:0.##}. Loot {2}/{3}/{4}. Heat {5:0.##}->{6:0.##}.",
                [MvpRunResultFeedbackPresenter.FormatWithPartyKey] = "{0} Mana {1:0.##}. Loot {2}/{3}/{4}. Heat {5:0.##}->{6:0.##}. {7}",
                [MvpRunResultFeedbackPresenter.PostureFormatKey] = "Posture: {0}. {1}",
                [MvpRunResultFeedbackPresenter.PartyPreviewFormatKey] = "Adventurers: {0}",
                [MvpRunResultFeedbackPresenter.PlacementEffectsImpactFormatKey] = "{0} Placement impact: {1}.",
                [MvpPlacementEffectsPresenter.DetailSeparatorKey] = ", ",
                [MvpPlacementEffectsPresenter.PathCapacityFormatKey] = "path +{0}",
                [MvpPlacementEffectsPresenter.DangerFormatKey] = "danger +{0}",
                [MvpPlacementEffectsPresenter.ExplanationFormatKey] = "{0} ({1})",
                [MinimalMvpActionPanelPresenter.CautiousPostureKey] = "Cautious",
                [MinimalMvpActionPanelPresenter.BalancedPostureKey] = "Balanced",
                [MinimalMvpActionPanelPresenter.GreedyPostureKey] = "Greedy",
                ["adventurer.class.warrior.display_name"] = "Warrior",
                ["adventurer.class.rogue.display_name"] = "Rogue",
                ["adventurer.class.mage.display_name"] = "Mage",
                ["adventurer.class.cleric.display_name"] = "Cleric",
                ["adventurer.class.ranger.display_name"] = "Ranger",
                ["effect.current_room"] = "current room capacity",
                ["effect.latest_trap"] = "latest trap pressure"
            };

            return map.TryGetValue(key, out string value) ? value : fallback;
        }
    }
}
