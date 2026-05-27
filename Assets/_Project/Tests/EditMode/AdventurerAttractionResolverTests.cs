using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class AdventurerAttractionResolverTests
    {
        private static RunSimulationConfig ValidConfig(double perWorldValue = 1d) => new RunSimulationConfig
        {
            AdventurerAttractionRuleSourceId = "run.adventurer_attraction.rule.v1",
            AdventurerAttractionPerExtractedWorldValue = perWorldValue
        };

        [Test]
        public void Resolve_IsDeterministic_AndUsesExtractedWorldValue()
        {
            var extraction = new RunLootExtractionSummary
            {
                RuleResolved = true,
                DeterministicErrorCode = (int)RunLootExtractionSummaryErrorCode.None,
                TotalExtractedWorldValue = 12,
                TotalExtractedTradeableWorldValue = 3
            };

            RunAdventurerAttractionSummary first = AdventurerAttractionResolver.Resolve(ValidConfig(2d), extraction, 7);
            RunAdventurerAttractionSummary second = AdventurerAttractionResolver.Resolve(ValidConfig(2d), extraction, 7);

            Assert.That(first.RuleResolved, Is.True);
            Assert.That(first.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerAttractionSummaryErrorCode.None));
            Assert.That(first.ExtractedWorldValueUsed, Is.EqualTo(12));
            Assert.That(first.AttractionSignalValue, Is.EqualTo(24d));
            Assert.That(second.AttractionSignalValue, Is.EqualTo(first.AttractionSignalValue));
        }

        [Test]
        public void Resolve_Fails_WhenExtractionSummaryMissingOrFailed()
        {
            var failedExtraction = new RunLootExtractionSummary { RuleResolved = false, DeterministicErrorCode = (int)RunLootExtractionSummaryErrorCode.UnknownRoundingPolicy };
            RunAdventurerAttractionSummary missing = AdventurerAttractionResolver.Resolve(ValidConfig(), null, 7);
            RunAdventurerAttractionSummary failed = AdventurerAttractionResolver.Resolve(ValidConfig(), failedExtraction, 7);

            Assert.That(missing.RuleResolved, Is.False);
            Assert.That(missing.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerAttractionSummaryErrorCode.ExtractionSummaryMissingOrFailed));
            Assert.That(failed.RuleResolved, Is.False);
            Assert.That(failed.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerAttractionSummaryErrorCode.ExtractionSummaryMissingOrFailed));
        }

        [Test]
        public void Resolve_Fails_WhenConfigInvalid()
        {
            RunAdventurerAttractionSummary result = AdventurerAttractionResolver.Resolve(ValidConfig(double.NaN), new RunLootExtractionSummary
            {
                RuleResolved = true,
                DeterministicErrorCode = (int)RunLootExtractionSummaryErrorCode.None,
                TotalExtractedWorldValue = 4
            }, 8);

            Assert.That(result.RuleResolved, Is.False);
            Assert.That(result.DeterministicErrorCode, Is.EqualTo((int)RunAdventurerAttractionSummaryErrorCode.InvalidAttractionConfig));
        }

        [Test]
        public void RunOutcomeRecord_AttractionSummary_RoundTrips_WithPopulatedValues()
        {
            var save = new SaveData
            {
                runHistory = new RunHistoryState
                {
                    LatestOutcome = new RunOutcomeRecord
                    {
                        RunId = "run-2",
                        AdventurerAttractionSummary = new RunAdventurerAttractionSummary { RuleResolved = true, DeterministicErrorCode = 0, AttractionSignalValue = 10d }
                    },
                    RecentOutcomes = new[]
                    {
                        new RunOutcomeRecord { RunId = "run-1", AdventurerAttractionSummary = new RunAdventurerAttractionSummary { RuleResolved = true, DeterministicErrorCode = 0, AttractionSignalValue = 2d } },
                        new RunOutcomeRecord { RunId = "run-2", AdventurerAttractionSummary = new RunAdventurerAttractionSummary { RuleResolved = true, DeterministicErrorCode = 0, AttractionSignalValue = 10d } }
                    }
                }
            };

            string json = JsonUtility.ToJson(save);
            SaveData loaded = JsonUtility.FromJson<SaveData>(json);

            Assert.That(loaded.runHistory.LatestOutcome.AdventurerAttractionSummary, Is.Not.Null);
            Assert.That(loaded.runHistory.LatestOutcome.AdventurerAttractionSummary.RuleResolved, Is.True);
            Assert.That(loaded.runHistory.LatestOutcome.AdventurerAttractionSummary.DeterministicErrorCode, Is.EqualTo(0));
            Assert.That(loaded.runHistory.LatestOutcome.AdventurerAttractionSummary.AttractionSignalValue, Is.EqualTo(10d));
            Assert.That(loaded.runHistory.RecentOutcomes[0].AdventurerAttractionSummary, Is.Not.Null);
            Assert.That(loaded.runHistory.RecentOutcomes[0].AdventurerAttractionSummary.RuleResolved, Is.True);
            Assert.That(loaded.runHistory.RecentOutcomes[0].AdventurerAttractionSummary.AttractionSignalValue, Is.EqualTo(2d));
            Assert.That(loaded.runHistory.RecentOutcomes[1].AdventurerAttractionSummary.AttractionSignalValue, Is.EqualTo(10d));
        }

        [Test]
        public void RunOutcomeRecord_AttractionSummary_LegacyMissingField_DeserializesSafely()
        {
            string legacyJson = "{\"runHistory\":{\"LatestOutcome\":{\"RunId\":\"run-2\"},\"RecentOutcomes\":[{\"RunId\":\"run-1\"}]}}";
            SaveData loaded = JsonUtility.FromJson<SaveData>(legacyJson);

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.runHistory, Is.Not.Null);
            Assert.That(loaded.runHistory.LatestOutcome, Is.Not.Null);
            Assert.That(loaded.runHistory.RecentOutcomes, Is.Not.Null);
            Assert.That(loaded.runHistory.RecentOutcomes.Length, Is.EqualTo(1));

            RunAdventurerAttractionSummary latest = loaded.runHistory.LatestOutcome.AdventurerAttractionSummary;
            RunAdventurerAttractionSummary recent = loaded.runHistory.RecentOutcomes[0].AdventurerAttractionSummary;

            Assert.That(latest, Is.Not.Null);
            Assert.That(latest.RuleResolved, Is.False);
            Assert.That(latest.DeterministicErrorCode, Is.EqualTo(0));
            Assert.That(latest.AttractionSignalValue, Is.EqualTo(0d));

            Assert.That(recent, Is.Not.Null);
            Assert.That(recent.RuleResolved, Is.False);
            Assert.That(recent.DeterministicErrorCode, Is.EqualTo(0));
            Assert.That(recent.AttractionSignalValue, Is.EqualTo(0d));
        }
    }
}
