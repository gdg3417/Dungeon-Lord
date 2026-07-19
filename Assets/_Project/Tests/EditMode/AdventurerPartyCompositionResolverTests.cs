#if UNITY_EDITOR
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class AdventurerPartyCompositionResolverTests
    {
        [Test]
        public void Resolve_SameInput_ReturnsDeterministicComposition()
        {
            RunSimulationConfig config = ValidConfig();

            AdventurerPartyCompositionSummary first = AdventurerPartyCompositionResolver.Resolve(config, "run-1", 42L, StructureSimulationPass.ManaGeneratorBasicId);
            AdventurerPartyCompositionSummary second = AdventurerPartyCompositionResolver.Resolve(config, "run-1", 42L, StructureSimulationPass.ManaGeneratorBasicId);

            Assert.That(second.RuleResolved, Is.EqualTo(first.RuleResolved));
            Assert.That(second.DeterministicSeed, Is.EqualTo(first.DeterministicSeed));
            Assert.That(second.ClassIds, Is.EqualTo(first.ClassIds));
            Assert.That(first.ClassIds.Length, Is.InRange(config.AdventurerPartyCompositionMinSize, config.AdventurerPartyCompositionMaxSize));
        }

        [Test]
        public void Resolve_NullOrEmptyConfig_ReturnsSafeEmptyFallback()
        {
            AdventurerPartyCompositionSummary nullConfig = AdventurerPartyCompositionResolver.Resolve(null, "run-1", 1L, StructureSimulationPass.ManaGeneratorBasicId);
            AdventurerPartyCompositionSummary emptyClasses = AdventurerPartyCompositionResolver.Resolve(new RunSimulationConfig
            {
                AdventurerPartyCompositionRuleSourceId = "run.adventurer_party_composition.rule.test",
                AdventurerPartyCompositionMinSize = 1,
                AdventurerPartyCompositionMaxSize = 3,
                AdventurerPartyCompositionMaxAllowedSize = 5,
                AdventurerPartyCompositionClassIds = new string[0]
            }, "run-1", 1L, StructureSimulationPass.ManaGeneratorBasicId);

            Assert.That(nullConfig.RuleResolved, Is.False);
            Assert.That(nullConfig.ClassIds, Is.Empty);
            Assert.That(nullConfig.DeterministicErrorCode, Is.EqualTo((int)AdventurerPartyCompositionSummaryErrorCode.MissingOrInvalidConfig));
            Assert.That(emptyClasses.RuleResolved, Is.False);
            Assert.That(emptyClasses.ClassIds, Is.Empty);
            Assert.That(emptyClasses.DeterministicErrorCode, Is.EqualTo((int)AdventurerPartyCompositionSummaryErrorCode.NoAllowedMvpClasses));
        }

        [Test]
        public void ClassLabels_AllFiveMvpClassesResolveThroughLocalizationKeys()
        {
            var requestedKeys = new List<string>();
            string[] labels = new[]
            {
                AdventurerPartyCompositionResolver.ResolveClassLabel(AdventurerPartyCompositionResolver.WarriorClassId, Capture(requestedKeys)),
                AdventurerPartyCompositionResolver.ResolveClassLabel(AdventurerPartyCompositionResolver.RogueClassId, Capture(requestedKeys)),
                AdventurerPartyCompositionResolver.ResolveClassLabel(AdventurerPartyCompositionResolver.MageClassId, Capture(requestedKeys)),
                AdventurerPartyCompositionResolver.ResolveClassLabel(AdventurerPartyCompositionResolver.ClericClassId, Capture(requestedKeys)),
                AdventurerPartyCompositionResolver.ResolveClassLabel(AdventurerPartyCompositionResolver.RangerClassId, Capture(requestedKeys))
            };

            Assert.That(labels, Is.EqualTo(new[] { "Warrior", "Rogue", "Mage", "Cleric", "Ranger" }));
            Assert.That(requestedKeys, Does.Contain("adventurer.class.warrior.display_name"));
            Assert.That(requestedKeys, Does.Contain("adventurer.class.rogue.display_name"));
            Assert.That(requestedKeys, Does.Contain("adventurer.class.mage.display_name"));
            Assert.That(requestedKeys, Does.Contain("adventurer.class.cleric.display_name"));
            Assert.That(requestedKeys, Does.Contain("adventurer.class.ranger.display_name"));
        }

        [Test]
        public void BuildPartyPreview_UsesLocalizedLabelsAndDoesNotExposeRawClassIds()
        {
            var summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasRunOutcome = true,
                AdventurerPartyPreviewResolved = true,
                AdventurerPartyClassIds = new[]
                {
                    AdventurerPartyCompositionResolver.WarriorClassId,
                    AdventurerPartyCompositionResolver.RogueClassId,
                    AdventurerPartyCompositionResolver.MageClassId,
                    AdventurerPartyCompositionResolver.ClericClassId,
                    AdventurerPartyCompositionResolver.RangerClassId
                }
            };

            string text = MvpRunResultFeedbackPresenter.BuildPartyPreview(summary, Localized);

            Assert.That(text, Is.EqualTo("Adventurers: Warrior, Rogue, Mage, Cleric, Ranger"));
            Assert.That(text, Does.Not.Contain("adventurer.class."));
        }


        [Test]
        public void BuildPartyPreview_MissingClassLocalizationFallsBackWithoutRawClassIds()
        {
            var summary = new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasRunOutcome = true,
                AdventurerPartyPreviewResolved = true,
                AdventurerPartyClassIds = new[] { AdventurerPartyCompositionResolver.WarriorClassId }
            };

            string text = MvpRunResultFeedbackPresenter.BuildPartyPreview(summary, (key, fallback) => key == MvpRunResultFeedbackPresenter.PartyPreviewFormatKey ? "Adventurers: {0}" : fallback);

            Assert.That(text, Does.Not.Contain(AdventurerPartyCompositionResolver.WarriorClassId));
            Assert.That(text, Does.Contain("ui.mvp_adventurer_party.class.unknown"));
        }

        [Test]
        public void ResolveAndPresent_DoesNotMutateRuntimeEconomyLootResearchOrRunHistory()
        {
            var save = new SaveData
            {
                structureRuntime = new StructureRuntimeState { ManaReserve = 12d, Heat = 7d },
                runHistory = new RunHistoryState
                {
                    NextRunSequence = 4,
                    LatestOutcome = new RunOutcomeRecord { RunId = "run-3", TickStarted = 9L, Success = true }
                },
                researchProgress = new ResearchProgressState { ProjectId = "research.project.safe", ProgressUnits = 2d }
            };
            string before = JsonUtility.ToJson(save);

            AdventurerPartyCompositionSummary party = AdventurerPartyCompositionResolver.Resolve(ValidConfig(), "run-3", 9L, StructureSimulationPass.RiskLabBasicId);
            string preview = MvpRunResultFeedbackPresenter.BuildPartyPreview(new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasRunOutcome = true,
                AdventurerPartyPreviewResolved = party.RuleResolved,
                AdventurerPartyClassIds = party.ClassIds
            }, Localized);

            Assert.That(preview, Does.StartWith("Adventurers: "));
            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
            Assert.That(save.structureRuntime.ManaReserve, Is.EqualTo(12d));
            Assert.That(save.structureRuntime.Heat, Is.EqualTo(7d));
            Assert.That(save.runHistory.NextRunSequence, Is.EqualTo(4));
            Assert.That(save.runHistory.LatestOutcome.RunId, Is.EqualTo("run-3"));
            Assert.That(save.researchProgress.ProgressUnits, Is.EqualTo(2d));
        }

        private static RunSimulationConfig ValidConfig()
        {
            return new RunSimulationConfig
            {
                AdventurerPartyCompositionRuleSourceId = "run.adventurer_party_composition.rule.test",
                AdventurerPartyCompositionMinSize = 1,
                AdventurerPartyCompositionMaxSize = 3,
                AdventurerPartyCompositionMaxAllowedSize = 5,
                AdventurerPartyCompositionClassIds = new[]
                {
                    AdventurerPartyCompositionResolver.WarriorClassId,
                    AdventurerPartyCompositionResolver.RogueClassId,
                    AdventurerPartyCompositionResolver.MageClassId,
                    AdventurerPartyCompositionResolver.ClericClassId,
                    AdventurerPartyCompositionResolver.RangerClassId
                }
            };
        }

        private static System.Func<string, string, string> Capture(List<string> requestedKeys)
        {
            return (key, fallback) =>
            {
                requestedKeys.Add(key);
                return Localized(key, fallback);
            };
        }

        private static string Localized(string key, string fallback)
        {
            switch (key)
            {
                case "adventurer.class.warrior.display_name": return "Warrior";
                case "adventurer.class.rogue.display_name": return "Rogue";
                case "adventurer.class.mage.display_name": return "Mage";
                case "adventurer.class.cleric.display_name": return "Cleric";
                case "adventurer.class.ranger.display_name": return "Ranger";
                case MvpRunResultFeedbackPresenter.PartyPreviewFormatKey: return "Adventurers: {0}";
                case "ui.mvp_adventurer_party.class.unknown": return "Unknown adventurer";
                default: return fallback;
            }
        }
    }
}
#endif
