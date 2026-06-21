using NUnit.Framework;
using UnityEngine;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.Tests.EditMode
{
    public class AdventurerTrafficPressureResolverTests
    {
        [Test]
        public void Resolve_SameInputsAreDeterministic()
        {
            var arrival = Arrival(5, 2, 0, 1);
            var intent = Intent();
            string first = JsonUtility.ToJson(AdventurerTrafficPressureResolver.Resolve(Config(), arrival, intent));
            string second = JsonUtility.ToJson(AdventurerTrafficPressureResolver.Resolve(Config(), arrival, intent));
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Resolve_DoesNotMutateSaveStateOrCreatePartyRecords()
        {
            var save = new SaveData { structureRuntime = new StructureRuntimeState { Heat = 4d, ManaReserve = 12d } };
            string before = JsonUtility.ToJson(save);
            AdventurerTrafficPressureResolver.Resolve(Config(), Arrival(5, 2, 0, 1), Intent());
            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(before));
        }

        [Test]
        public void Resolve_MissingOrInvalidConfigReturnsDeterministicError()
        {
            var summary = AdventurerTrafficPressureResolver.Resolve(new RunSimulationConfig(), Arrival(5, 2, 0, 1), Intent());
            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)AdventurerTrafficPressureSummaryErrorCode.MissingOrInvalidConfig));
        }

        [Test]
        public void Resolve_LowPressureProducesLowOrNoneTraffic()
        {
            var summary = AdventurerTrafficPressureResolver.Resolve(Config(), Arrival(0, 0, 0, 0, false, 0d), Intent());
            Assert.That(summary.TrafficBandId == AdventurerTrafficPressureResolver.BandLowId || summary.TrafficBandId == AdventurerTrafficPressureResolver.BandNoneId, Is.True);
        }

        [Test]
        public void Resolve_HighLootAndLowHeatProducesBuildingOrHeavyTraffic()
        {
            var summary = AdventurerTrafficPressureResolver.Resolve(Config(), Arrival(6, 4, 0, 1, true, 14d), Intent());
            Assert.That(summary.TrafficBandId == AdventurerTrafficPressureResolver.BandBuildingId || summary.TrafficBandId == AdventurerTrafficPressureResolver.BandSteadyId || summary.TrafficBandId == AdventurerTrafficPressureResolver.BandHeavyId, Is.True);
        }

        [Test]
        public void Resolve_RecentDeathsPlusActiveInterestProducesDangerousChurn()
        {
            var summary = AdventurerTrafficPressureResolver.Resolve(Config(), Arrival(6, 4, 0, 1, true, 14d, 1), Intent());
            Assert.That(summary.TrafficBandId, Is.EqualTo(AdventurerTrafficPressureResolver.BandDangerousChurnId));
            Assert.That(summary.PrimaryReasonKey, Is.EqualTo(AdventurerTrafficPressureResolver.ReasonDeathsCautionKey));
        }

        [Test]
        public void Resolve_EstimatedPartyCountIsDeterministicAndClamped()
        {
            var summary = AdventurerTrafficPressureResolver.Resolve(Config(), Arrival(50, 50, 0, 1, true, 100d), Intent());
            Assert.That(summary.EstimatedConcurrentPartyCount, Is.EqualTo(Config().TrafficMaximumEstimatedConcurrentParties));
            Assert.That(summary.EstimatedConcurrentPartyBandId, Is.EqualTo(AdventurerTrafficPressureResolver.PartyBandHighId));
        }

        [Test]
        public void Presenter_LocalizesBandsAndReasons()
        {
            var summary = AdventurerTrafficPressureResolver.Resolve(Config(), Arrival(6, 4, 0, 1, true, 14d), Intent());
            string line = AdventurerTrafficPressurePresenter.BuildSummaryLine(summary, Localized);
            Assert.That(line, Does.Contain("Adventurer traffic:"));
            Assert.That(line, Does.Not.Contain("ui.adventurer_traffic"));
        }

        [Test]
        public void PlayableScreenIncludesCompactTrafficLine()
        {
            var summary = new MvpPlayerLoopSummary { RuleResolved = true, AdventurerTrafficPressure = AdventurerTrafficPressureResolver.Resolve(Config(), Arrival(6, 4, 0, 1, true, 14d), Intent()), AdventurerArrivalPressure = new AdventurerArrivalPressureSummary { RuleResolved = true }, AdventurerRunIntent = Intent(), ResearchStatusKey = MvpPlayerLoopSummaryPresenter.ResearchUnavailableKey, NextOptimizationSuggestionKey = MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey };
            string text = MvpPlayableScreenPresenter.BuildScreenText(summary, new GuidedMvpActionPathSummary { IsComplete = true }, string.Empty, "Room", "Basic", string.Empty, string.Empty, "Balanced", "Plan", string.Empty, string.Empty, string.Empty, null, null, null, Localized);
            Assert.That(text, Does.Contain("Adventurer traffic:"));
            Assert.That(text, Does.Contain("== Top Status =="));
            Assert.That(text, Does.Contain("== Action Controls =="));
        }

        [Test]
        public void Presenter_DetailIncludesFullTrafficSmokeEvidence()
        {
            string detail = AdventurerTrafficPressurePresenter.BuildDetailLine(AdventurerTrafficPressureResolver.Resolve(Config(), Arrival(6, 4, 0, 1, true, 14d), Intent()), Localized);
            Assert.That(detail, Does.Contain("score"));
            Assert.That(detail, Does.Contain("estimated active delves"));
            Assert.That(detail, Does.Contain("traffic pressure intent input Greedy"));
            Assert.That(detail, Does.Contain("rule source run.adventurer_traffic_pressure.rule.test"));
            Assert.That(detail, Does.Not.Contain("ui.adventurer_traffic"));
        }

        [Test]
        public void ConfigValidationRejectsMissingRuleSourceInvalidThresholdsNanInfinityOrInvalidMaxPartyCount()
        {
            var missing = Config(); missing.AdventurerTrafficPressureRuleSourceId = string.Empty;
            Assert.That(AdventurerTrafficPressureResolver.Resolve(missing, Arrival(5, 2, 0, 1), Intent()).DeterministicErrorCode, Is.EqualTo((int)AdventurerTrafficPressureSummaryErrorCode.MissingOrInvalidConfig));
            var invalid = Config(); invalid.TrafficLowThreshold = 99d;
            Assert.That(AdventurerTrafficPressureResolver.Resolve(invalid, Arrival(5, 2, 0, 1), Intent()).DeterministicErrorCode, Is.EqualTo((int)AdventurerTrafficPressureSummaryErrorCode.MissingOrInvalidConfig));
            var nan = Config(); nan.TrafficScoreWeightLootSignal = double.NaN;
            Assert.That(AdventurerTrafficPressureResolver.Resolve(nan, Arrival(5, 2, 0, 1), Intent()).DeterministicErrorCode, Is.EqualTo((int)AdventurerTrafficPressureSummaryErrorCode.MissingOrInvalidConfig));
            var inf = Config(); inf.TrafficScoreWeightLootSignal = double.PositiveInfinity;
            Assert.That(AdventurerTrafficPressureResolver.Resolve(inf, Arrival(5, 2, 0, 1), Intent()).DeterministicErrorCode, Is.EqualTo((int)AdventurerTrafficPressureSummaryErrorCode.MissingOrInvalidConfig));
            var badMax = Config(); badMax.TrafficMaximumEstimatedConcurrentParties = 0;
            Assert.That(AdventurerTrafficPressureResolver.Resolve(badMax, Arrival(5, 2, 0, 1), Intent()).DeterministicErrorCode, Is.EqualTo((int)AdventurerTrafficPressureSummaryErrorCode.MissingOrInvalidConfig));
        }

        private static AdventurerArrivalPressureSummary Arrival(int loot, int attraction, int heatPressure, int path, bool complete = true, double score = 10d, int deaths = 0) => new AdventurerArrivalPressureSummary { RuleResolved = true, Score = score, PressureBandId = AdventurerArrivalPressureResolver.BandBuildingId, PathComplete = complete && path > 0, LootSignal = loot, AttractionSignal = attraction, HeatPressureSignal = heatPressure, RecentDeathCount = deaths };
        private static AdventurerRunIntentSummary Intent() => new AdventurerRunIntentSummary { RuleResolved = true, IntentId = RunPostureResolver.GreedyId };
        private static RunSimulationConfig Config() => new RunSimulationConfig { AdventurerTrafficPressureRuleSourceId = "run.adventurer_traffic_pressure.rule.test", TrafficScoreWeightArrivalPressure = 1d, TrafficScoreWeightLootSignal = 0.75d, TrafficScoreWeightAttractionSignal = 1d, TrafficScoreWeightDangerSignal = 0.25d, TrafficScoreWeightHeatPressureSignal = 0.25d, TrafficScoreWeightRecentRecoveredLoot = 0.02d, TrafficPathCompleteBonus = 1d, TrafficIncompletePathPenalty = 3d, TrafficRecentDeathCautionModifier = 1d, TrafficHeatCautionModifier = 0.5d, TrafficNoneThreshold = 0d, TrafficLowThreshold = 2d, TrafficBuildingThreshold = 8d, TrafficSteadyThreshold = 14d, TrafficHeavyThreshold = 20d, TrafficDangerousChurnRecentDeathThreshold = 1, TrafficDangerousChurnMinimumInterestScore = 8d, TrafficEstimatedPartyCountMultiplier = 1d, TrafficEstimatedPartyCountScoreDivisor = 8d, TrafficEstimatedPartyCountLowThreshold = 1, TrafficEstimatedPartyCountMediumThreshold = 3, TrafficMinimumEstimatedConcurrentParties = 0, TrafficMaximumEstimatedConcurrentParties = 4 };
        private static string Localized(string key, string fallback)
        {
            if (key == AdventurerTrafficPressurePresenter.SummaryFormatKey) return "Adventurer traffic: {0}. Estimated active delves: {1}. Reason: {2}.";
            if (key == AdventurerTrafficPressurePresenter.DetailFormatKey) return "Adventurer traffic detail: score {0:0.##}; band {1}; estimated active delves {2}; estimated delve band {3}; arrival pressure {4}; traffic pressure intent input {5}; rule source {6}; error {7}; loot {8}; attraction {9}; danger {10}; heat pressure {11}; recent deaths {12}; recovered loot {13}; path complete {14}.";
            if (key.StartsWith("ui.adventurer_traffic.band.")) return key.Substring(key.LastIndexOf('.') + 1).Replace('_', ' ');
            if (key.StartsWith("ui.adventurer_traffic.party_band.")) return key.Substring(key.LastIndexOf('.') + 1);
            if (key.StartsWith("ui.adventurer_traffic.reason.")) return "localized reason";
            if (key.StartsWith("ui.adventurer_pressure.band.")) return "arrival band";
            if (key == AdventurerArrivalPressurePresenter.SummaryFormatKey) return "Adventurer pressure: {0}. Reason: {1}.";
            if (key == AdventurerArrivalPressureResolver.ReasonNotYetKey) return "pressure reason";
            if (key == MvpLoopSummaryPanelPresenter.HeatFormatKey) return "Heat: {0:0.##} -> {1:0.##} ({2}); risk {3}";
            if (key == MvpLoopSummaryPanelPresenter.InlineSeparatorKey) return " | ";
            if (key == MvpPlayableScreenPresenter.TitleKey) return "Dungeon Command (MVP Loop Summary)";
            if (key == MvpPlayableScreenPresenter.TopStatusKey) return "Top Status";
            if (key == MvpPlayableScreenPresenter.CurrentDungeonKey) return "Current Dungeon";
            if (key == MvpPlayableScreenPresenter.BuildChoiceKey) return "Build Choice";
            if (key == MvpPlayableScreenPresenter.RunSetupKey) return "Activity Setup";
            if (key == MvpPlayableScreenPresenter.LatestRunKey) return "Latest Adventurer Visit";
            if (key == MvpPlayableScreenPresenter.AnalysisNextActionKey) return "Analysis and Next Action";
            if (key == MvpPlayableScreenPresenter.SectionHeaderFormatKey) return "== {0} ==";
            if (key == MvpPlayableScreenPresenter.PlayerViewStatusKey) return "Player view: diagnostics hidden.";
            if (key == MvpPlayableScreenPresenter.PathCompleteFormatKey) return "Path complete: {0}";
            if (key == MvpPlayableScreenPresenter.SelectedPlacementFormatKey) return "Selected placement: {0} / {1}";
            if (key == MvpPlayableScreenPresenter.NoComparisonKey) return "No comparison.";
            if (key == MvpPlayableScreenPresenter.PlacePromptKey) return "Next build step: choose an option, then place or modify it.";
            if (key == MvpPlayableScreenPresenter.ActionControlsKey) return "Action Controls";
            if (key == MvpPlayableScreenPresenter.LatestResultKey) return "Latest Result";
            if (key == MvpPlayableScreenPresenter.DetailsHintKey) return "Details: press F5 to cycle focused sections, F6 to copy full smoke evidence, or show diagnostics from the action panel.";
            if (key == MvpPlayableScreenPresenter.RoomTargetControlFormatKey) return "Room target: {0}; {1}";
            if (key == MvpPlayableScreenPresenter.PlacementControlFormatKey) return "Placement: {0} / {1}";
            if (key == MvpPlayableScreenPresenter.PlaceButtonControlKey) return "Action button: Place / modify selected placement";
            if (key == MvpPlayableScreenPresenter.RunPostureControlFormatKey) return "Run posture: {0}";
            if (key == MvpPlayableScreenPresenter.RunButtonControlKey) return "Action button: Run / observe dungeon";
            if (key == MvpPlayableScreenPresenter.LatestResultFormatKey) return "{0}; {1}; {2}; {3}; {4}";
            if (key == MvpPlayableScreenPresenter.LatestResultNoRunKey) return "No adventurer visit yet. Use Run / observe dungeon after the path is ready.";
            if (key == MvpPlayableScreenPresenter.PartyFormatKey) return "Party: {0}";
            if (key == MvpPlayableScreenPresenter.ResearchFormatKey) return "Research: {0}";
            if (key == MvpPlayableScreenPresenter.AnalysisFormatKey) return "Why it happened: {0}";
            if (key == MvpPlayableScreenPresenter.PartyUnavailableKey) return "Party preview unavailable.";
            if (key == MvpPlayableScreenPresenter.NoAnalysisKey) return "Run once to unlock analysis.";
            if (key == MvpLoopSummaryPanelPresenter.ValueNoRunKey) return "No adventurer visit yet.";
            if (key == MvpLoopSummaryPanelPresenter.ValueNoPlacementKey) return "no placement";
            if (key == MvpLoopSummaryPanelPresenter.ValueNoResearchKey) return "no research";
            if (key == MvpLoopSummaryPanelPresenter.ValueUnknownKey) return "unknown";
            if (key == MvpLoopSummaryPanelPresenter.RiskNoRunKey) return "no run yet";
            if (key == MvpLoopSummaryPanelPresenter.LootFormatKey) return "Loot: generated {0}, recovered {1}, tradeable {2}";
            if (key == MvpLoopSummaryPanelPresenter.SuggestionFormatKey) return "Suggested next action: {0}";
            if (key == MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey) return "observe dungeon";
            if (key == MvpPlayerLoopSummaryPresenter.ResearchUnavailableKey) return "no active research";
            if (key == AdventurerRunIntentPresenter.DebugPostureFormatKey) return "Expected next adventurer intent: {0} likely. Debug selected posture: {1}.";
            if (key == AdventurerRunIntentResolver.ReasonFallbackKey) return "intent reason";
            if (key == "run.posture.greedy.name") return "Greedy";
            if (key == "run.posture.balanced.name") return "Balanced";
            if (key == GuidedMvpActionPathPanelPresenter.CompleteYesKey) return "yes";
            return fallback;
        }
    }
}
