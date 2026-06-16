using NUnit.Framework;
using UnityEngine;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.Tests.EditMode
{
    public class AdventurerArrivalPressureResolverTests
    {
        [Test]
        public void Resolve_SameInputsAreDeterministic()
        {
            var config = Config();
            var effects = Effects(1, 2, 1, 0, 1);
            string first = JsonUtility.ToJson(AdventurerArrivalPressureResolver.Resolve(config, effects, Heat("heat_tier.peace"), null));
            string second = JsonUtility.ToJson(AdventurerArrivalPressureResolver.Resolve(config, effects, Heat("heat_tier.peace"), null));
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Resolve_DoesNotMutateSaveState()
        {
            var save = new SaveData { structureRuntime = new StructureRuntimeState { Heat = 4d, ManaReserve = 12d } };
            string before = JsonUtility.ToJson(save);
            AdventurerArrivalPressureResolver.Resolve(Config(), Effects(1, 2, 1, 0, 1), Heat("heat_tier.peace"), null);
            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
        }

        [Test]
        public void Resolve_InvalidOrMissingConfigReturnsDeterministicError()
        {
            var summary = AdventurerArrivalPressureResolver.Resolve(new RunSimulationConfig(), Effects(0, 8, 4, 0, 1), Heat("heat_tier.peace"), null);
            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)AdventurerArrivalPressureSummaryErrorCode.MissingOrInvalidConfig));
        }

        [Test]
        public void Resolve_IncompletePathProducesLowOrNonePressure()
        {
            var summary = AdventurerArrivalPressureResolver.Resolve(Config(), Effects(0, 2, 0, 0, 0), Heat("heat_tier.peace"), null);
            Assert.That(
                summary.PressureBandId == AdventurerArrivalPressureResolver.BandLowId ||
                summary.PressureBandId == AdventurerArrivalPressureResolver.BandNotYetId,
                Is.True);
        }

        [Test]
        public void Resolve_HighLootIncompletePathCannotBecomeLikelySoon()
        {
            var summary = AdventurerArrivalPressureResolver.Resolve(Config(), Effects(0, 9, 4, 0, 0), Heat("heat_tier.peace"), null);

            Assert.That(summary.PressureBandId, Is.Not.EqualTo(AdventurerArrivalPressureResolver.BandLikelySoonId));
        }

        [Test]
        public void Resolve_HighLootAndLowHeatProducesLikelySoon()
        {
            var summary = AdventurerArrivalPressureResolver.Resolve(Config(), Effects(0, 5, 2, 0, 1), Heat("heat_tier.peace"), null);
            Assert.That(summary.PressureBandId, Is.EqualTo(AdventurerArrivalPressureResolver.BandLikelySoonId));
            Assert.That(summary.PrimaryReasonKey, Is.EqualTo(AdventurerArrivalPressureResolver.ReasonHighLootLowHeatKey));
        }

        [Test]
        public void Resolve_ModestLootAndLowAttractionProducesBuildingSlowlyOrLow()
        {
            var summary = AdventurerArrivalPressureResolver.Resolve(Config(), Effects(0, 2, 0, 0, 1), Heat("heat_tier.peace"), null);
            Assert.That(
                summary.PressureBandId == AdventurerArrivalPressureResolver.BandBuildingId ||
                summary.PressureBandId == AdventurerArrivalPressureResolver.BandLowId,
                Is.True);
            Assert.That(summary.PrimaryReasonKey, Is.EqualTo(AdventurerArrivalPressureResolver.ReasonModestLootLowAttractionKey));
        }

        [Test]
        public void Resolve_RecentDeathsAndRisingHeatProducesCautiousInterest()
        {
            var latest = new RunOutcomeRecord { Success = false, SurvivalSummary = new RunSurvivalSummary { DeathCount = 1 } };
            var summary = AdventurerArrivalPressureResolver.Resolve(Config(), Effects(0, 5, 2, 1, 1), Heat("heat_tier.notice"), latest);
            Assert.That(summary.PressureBandId, Is.EqualTo(AdventurerArrivalPressureResolver.BandCautiousId));
            Assert.That(summary.PrimaryReasonKey, Is.EqualTo(AdventurerArrivalPressureResolver.ReasonDeathsHeatKey));
        }

        [Test]
        public void Presenter_UsesLocalizedTextWithoutRawIdsOrKeys()
        {
            var summary = AdventurerArrivalPressureResolver.Resolve(Config(), Effects(0, 5, 2, 0, 1), Heat("heat_tier.peace"), null);
            string line = AdventurerArrivalPressurePresenter.BuildSummaryLine(summary, Localized);
            Assert.That(line, Is.EqualTo("Adventurer pressure: likely soon. Reason: high loot signal and low heat."));
            Assert.That(line, Does.Not.Contain("adventurer_arrival_pressure"));
            Assert.That(line, Does.Not.Contain("ui.adventurer_pressure"));
        }

        [Test]
        public void Presenter_DetailIncludesFullSmokeEvidenceWithoutRawBandId()
        {
            var summary = AdventurerArrivalPressureResolver.Resolve(Config(), Effects(0, 5, 2, 0, 1), Heat("heat_tier.peace"), null);
            string line = AdventurerArrivalPressurePresenter.BuildDetailLine(summary, Localized);
            Assert.That(line, Does.Contain("score"));
            Assert.That(line, Does.Contain("rule source run.adventurer_arrival_pressure.rule.test"));
            Assert.That(line, Does.Contain("error 0"));
            Assert.That(line, Does.Not.Contain("adventurer_arrival_pressure.band"));
        }

        [Test]
        public void ConfigValidationRejectsMissingRuleSourceInvalidThresholdsNanOrInfinite()
        {
            var missing = Config();
            missing.AdventurerArrivalPressureRuleSourceId = string.Empty;
            Assert.That(AdventurerArrivalPressureResolver.Resolve(missing, Effects(0, 5, 2, 0, 1), Heat("heat_tier.peace"), null).DeterministicErrorCode, Is.EqualTo((int)AdventurerArrivalPressureSummaryErrorCode.MissingOrInvalidConfig));

            var invalidThresholds = Config();
            invalidThresholds.ArrivalPressureLowThreshold = 20d;
            Assert.That(AdventurerArrivalPressureResolver.Resolve(invalidThresholds, Effects(0, 5, 2, 0, 1), Heat("heat_tier.peace"), null).DeterministicErrorCode, Is.EqualTo((int)AdventurerArrivalPressureSummaryErrorCode.MissingOrInvalidConfig));

            var nan = Config();
            nan.ArrivalPressureScorePerLoot = double.NaN;
            Assert.That(AdventurerArrivalPressureResolver.Resolve(nan, Effects(0, 5, 2, 0, 1), Heat("heat_tier.peace"), null).DeterministicErrorCode, Is.EqualTo((int)AdventurerArrivalPressureSummaryErrorCode.MissingOrInvalidConfig));

            var infinite = Config();
            infinite.ArrivalPressureScorePerLoot = double.PositiveInfinity;
            Assert.That(AdventurerArrivalPressureResolver.Resolve(infinite, Effects(0, 5, 2, 0, 1), Heat("heat_tier.peace"), null).DeterministicErrorCode, Is.EqualTo((int)AdventurerArrivalPressureSummaryErrorCode.MissingOrInvalidConfig));
        }


        [Test]
        public void PlayableScreenIncludesCompactPressureLineAndPreservesSections()
        {
            var summary = LoopSummary();
            string text = MvpPlayableScreenPresenter.BuildScreenText(summary, new GuidedMvpActionPathSummary { IsComplete = true }, string.Empty, "Room", "Basic", string.Empty, string.Empty, "Balanced", "Plan", string.Empty, string.Empty, string.Empty, null, Localized);
            Assert.That(text, Does.Contain("Adventurer pressure: likely soon. Reason: high loot signal and low heat."));
            Assert.That(text, Does.Contain("== Top Status =="));
            Assert.That(text, Does.Contain("== Activity Setup =="));
            Assert.That(text, Does.Contain("== Build Choice =="));
        }

        [Test]
        public void LoopSummaryPanelIncludesPressureWithoutDuplicateIntentLabels()
        {
            string text = MvpLoopSummaryPanelPresenter.BuildPanelText(LoopSummary(), Localized);
            Assert.That(text, Does.Contain("Adventurer pressure: likely soon. Reason: high loot signal and low heat."));
            Assert.That(text, Does.Not.Contain("Adventurer intent: Adventurer intent:"));
            Assert.That(text, Does.Not.Contain("Adventurer pressure: Adventurer pressure:"));
        }

        private static MvpPlayerLoopSummary LoopSummary() => new MvpPlayerLoopSummary
        {
            RuleResolved = true,
            PlacementEffects = Effects(0, 5, 2, 0, 1),
            AdventurerArrivalPressure = AdventurerArrivalPressureResolver.Resolve(Config(), Effects(0, 5, 2, 0, 1), Heat("heat_tier.peace"), null),
            AdventurerRunIntent = new AdventurerRunIntentSummary { RuleResolved = true, IntentId = RunPostureResolver.GreedyId, PrimaryReasonKey = AdventurerRunIntentResolver.ReasonLootHighHeatLowKey },
            ResearchStatusKey = MvpPlayerLoopSummaryPresenter.ResearchUnavailableKey,
            NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey
        };

        private static MvpPlacementEffectsSummary Effects(int danger, int loot, int attraction, int heatPressure, int pathCapacity) => new MvpPlacementEffectsSummary { RuleResolved = true, Danger = danger, LootBonus = loot, Attraction = attraction, HeatPressure = heatPressure, PathCapacity = pathCapacity };
        private static CurrentHeatTierSummary Heat(string tier) => new CurrentHeatTierSummary { RuleResolved = true, TierId = tier };
        private static RunSimulationConfig Config() => new RunSimulationConfig
        {
            AdventurerArrivalPressureRuleSourceId = "run.adventurer_arrival_pressure.rule.test",
            ArrivalPressureScorePerLoot = 2d,
            ArrivalPressureScorePerAttraction = 1d,
            ArrivalPressureScorePerDanger = 0.25d,
            ArrivalPressureScorePerHeatPressure = 0.5d,
            ArrivalPressureScorePerRecentRecoveredLoot = 0.05d,
            ArrivalPressureLatestSuccessBonus = 1d,
            ArrivalPressureLatestFailurePenalty = 1d,
            ArrivalPressurePathCompleteBonus = 2d,
            ArrivalPressureIncompletePathPenalty = 4d,
            ArrivalPressureHeatNoticePenalty = 2d,
            ArrivalPressureHeatConcernPenalty = 5d,
            ArrivalPressureRecentDeathPenalty = 3d,
            ArrivalPressureNoneThreshold = 0d,
            ArrivalPressureLowThreshold = 2d,
            ArrivalPressureCautiousThreshold = 5d,
            ArrivalPressureBuildingThreshold = 8d,
            ArrivalPressureLikelySoonThreshold = 12d
        };

        private static string Localized(string key, string fallback)
        {
            switch (key)
            {
                case AdventurerArrivalPressurePresenter.SummaryFormatKey: return "Adventurer pressure: {0}. Reason: {1}.";
                case AdventurerArrivalPressurePresenter.BodyFormatKey: return "{0}. Reason: {1}.";
                case AdventurerRunIntentPresenter.BodyFormatKey: return "{0} likely. Reason: {1}";
                case AdventurerRunIntentPresenter.SummaryFormatKey: return "Adventurer intent: {0} likely. Reason: {1}";
                case AdventurerRunIntentPresenter.DebugPostureFormatKey: return "Adventurer intent: {0} likely. Debug selected posture: {1}.";
                case MvpLoopSummaryPanelPresenter.TitleKey: return "MVP Loop Summary";
                case MvpLoopSummaryPanelPresenter.AdventurerIntentSectionKey: return "Adventurer intent";
                case MvpLoopSummaryPanelPresenter.AdventurerPressureSectionKey: return "Adventurer pressure";
                case MvpLoopSummaryPanelPresenter.SectionLineFormatKey: return "{0}: {1}";
                case MvpLoopSummaryPanelPresenter.CompositionFormatKey: return "Dungeon composition: {0}";
                case MvpLoopSummaryPanelPresenter.PlacementEffectsFormatKey: return "Effects: {0}";
                case MvpLoopSummaryPanelPresenter.ManaFormatKey: return "Mana reserve: {0:0.##}";
                case MvpLoopSummaryPanelPresenter.InlineSeparatorKey: return " | ";
                case MvpLoopSummaryPanelPresenter.HeatFormatKey: return "Heat: {0:0.##} -> {1:0.##} ({2}); risk {3}";
                case MvpLoopSummaryPanelPresenter.ResearchFormatKey: return "Research: {0}";
                case MvpLoopSummaryPanelPresenter.SuggestionFormatKey: return "Suggested next action: {0}";
                case MvpLoopSummaryPanelPresenter.CurrentDungeonSectionKey: return "Current Dungeon";
                case MvpLoopSummaryPanelPresenter.LatestRunSectionKey: return "Latest Adventurer Visit";
                case MvpLoopSummaryPanelPresenter.WhyItHappenedSectionKey: return "Why It Happened";
                case MvpLoopSummaryPanelPresenter.RewardsAndRiskSectionKey: return "Rewards and Risk";
                case MvpLoopSummaryPanelPresenter.ResearchSectionKey: return "Research";
                case MvpLoopSummaryPanelPresenter.SuggestedNextActionSectionKey: return "Suggested Next Action";
                case MvpPlayableScreenPresenter.TitleKey: return "Dungeon Command (MVP Loop Summary)";
                case MvpPlayableScreenPresenter.TopStatusKey: return "Top Status";
                case MvpPlayableScreenPresenter.CurrentDungeonKey: return "Current Dungeon";
                case MvpPlayableScreenPresenter.BuildChoiceKey: return "Build Choice";
                case MvpPlayableScreenPresenter.RunSetupKey: return "Activity Setup";
                case MvpPlayableScreenPresenter.LatestRunKey: return "Latest Adventurer Visit";
                case MvpPlayableScreenPresenter.AnalysisNextActionKey: return "Analysis and Next Action";
                case MvpPlayableScreenPresenter.SectionHeaderFormatKey: return "== {0} ==";
                case MvpPlayableScreenPresenter.PlayerViewStatusKey: return "Player view: diagnostics hidden.";
                case MvpPlayableScreenPresenter.PathCompleteFormatKey: return "Path complete: {0}";
                case MvpPlayableScreenPresenter.SelectedPlacementFormatKey: return "Selected placement: {0} / {1}";
                case MvpPlayableScreenPresenter.NoComparisonKey: return "No comparison.";
                case MvpPlayableScreenPresenter.PlacePromptKey: return "Next build step: choose an option, then place or modify it.";
                case AdventurerArrivalPressurePresenter.DetailFormatKey: return "Adventurer pressure detail: score {0:0.##}; band {1}; rule source {2}; error {3}; loot {4}; attraction {5}; danger {6}; heat pressure {7}; recent deaths {8}; recovered loot {9}; path complete {10}; latest visit {11}.";
                case "ui.adventurer_pressure.band.not_yet": return "not yet";
                case "ui.adventurer_pressure.band.low": return "low";
                case "ui.adventurer_pressure.band.cautious_interest": return "cautious interest";
                case "ui.adventurer_pressure.band.building_slowly": return "building slowly";
                case "ui.adventurer_pressure.band.likely_soon": return "likely soon";
                case "ui.adventurer_pressure.outcome.none": return "none yet";
                case "ui.adventurer_pressure.outcome.success": return "success";
                case "ui.adventurer_pressure.outcome.failure": return "failure";
                case AdventurerArrivalPressureResolver.ReasonHighLootLowHeatKey: return "high loot signal and low heat";
                case AdventurerArrivalPressureResolver.ReasonModestLootLowAttractionKey: return "modest loot and low attraction";
                case AdventurerArrivalPressureResolver.ReasonDeathsHeatKey: return "recent deaths and rising heat";
                case AdventurerArrivalPressureResolver.ReasonIncompletePathWeakLootKey: return "incomplete path or weak loot signal";
                case AdventurerArrivalPressureResolver.ReasonNotYetKey: return "current dungeon signals are still forming";
                case "run.posture.greedy.name": return "Greedy";
                case "run.posture.balanced.name": return "Balanced";
                case GuidedMvpActionPathPanelPresenter.CompleteYesKey: return "yes";
                case MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey: return "observe dungeon";
                case MvpPlayerLoopSummaryPresenter.ResearchUnavailableKey: return "no active research";
                case AdventurerRunIntentResolver.ReasonLootHighHeatLowKey: return "loot signal is high and heat is low";
                default: return fallback;
            }
        }
    }
}
