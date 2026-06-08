using DungeonBuilder.M0;
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

            Assert.That(text, Is.EqualTo("Run result: succeeded. Loot extracted, heat stable. Mana 12. Loot 7/5/3. Heat 4->4."));
        }

        [Test]
        public void SuccessfulRunWithLowerHeat_UsesReducedHeatInterpretation()
        {
            string text = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                Summary(hasRun: false),
                Summary(runSucceeded: true, mana: 8d, generatedLoot: 6, extractedLoot: 4, tradeableLoot: 2, heatBefore: 9d, heatAfter: 6d),
                didRun: true,
                Localized);

            Assert.That(text, Is.EqualTo("Run result: succeeded. Loot extracted, heat reduced. Mana 8. Loot 6/4/2. Heat 9->6."));
        }

        [Test]
        public void SuccessfulRunWithHigherHeat_UsesIncreasedHeatInterpretation()
        {
            string text = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                Summary(hasRun: false),
                Summary(runSucceeded: true, mana: 5d, generatedLoot: 9, extractedLoot: 6, tradeableLoot: 4, heatBefore: 7d, heatAfter: 11d),
                didRun: true,
                Localized);

            Assert.That(text, Is.EqualTo("Run result: succeeded. Loot extracted, heat increased. Mana 5. Loot 9/6/4. Heat 7->11."));
        }

        [Test]
        public void FailedRun_UsesLocalizedFailureInterpretationAndDoesNotExposeRawIds()
        {
            string text = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                Summary(hasRun: true, latestRunId: "run-prior", selectedStructureId: StructureSimulationPass.ManaGeneratorBasicId),
                Summary(runSucceeded: false, latestRunId: "run-debug-raw", selectedStructureId: StructureSimulationPass.HeatScrubberBasicId, mana: 2d, generatedLoot: 1, extractedLoot: 0, tradeableLoot: 0, heatBefore: 12d, heatAfter: 14d),
                didRun: true,
                Localized);

            Assert.That(text, Is.EqualTo("Run result: failed. Review placement and try again. Mana 2. Loot 1/0/0. Heat 12->14."));
            Assert.That(text, Does.Not.Contain("run-debug-raw"));
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.ManaGeneratorBasicId));
            Assert.That(text, Does.Not.Contain(StructureSimulationPass.HeatScrubberBasicId));
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
        public void Feedback_RequestsLocalizationKeysInsteadOfHardcodedEnglish()
        {
            var requestedKeys = new List<string>();

            MvpRunResultFeedbackPresenter.BuildFeedbackText(
                Summary(hasRun: false),
                Summary(runSucceeded: true, heatBefore: 1d, heatAfter: 1d),
                didRun: true,
                (key, fallback) =>
                {
                    requestedKeys.Add(key);
                    return Localized(key, fallback);
                });

            Assert.That(requestedKeys, Does.Contain(MvpRunResultFeedbackPresenter.SuccessStableHeatKey));
            Assert.That(requestedKeys, Does.Contain(MvpRunResultFeedbackPresenter.FormatKey));
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
            double heatAfter = 0d)
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
                HeatAfter = heatAfter
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
                [MvpRunResultFeedbackPresenter.FormatKey] = "{0} Mana {1:0.##}. Loot {2}/{3}/{4}. Heat {5:0.##}->{6:0.##}."
            };

            return map.TryGetValue(key, out string value) ? value : fallback;
        }
    }
}
