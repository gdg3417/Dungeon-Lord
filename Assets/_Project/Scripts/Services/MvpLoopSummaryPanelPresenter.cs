using System;
using System.Text;

namespace DungeonBuilder.M0
{
    public static class MvpLoopSummaryPanelPresenter
    {
        public const string TitleKey = "ui.mvp_loop.panel.title";
        public const string PlacementFormatKey = "ui.mvp_loop.panel.composition_format";
        public const string CompositionFormatKey = PlacementFormatKey;
        public const string LatestRunFormatKey = "ui.mvp_loop.panel.latest_run_format";
        public const string PlacementEffectsFormatKey = "ui.mvp_loop.panel.placement_effects_format";
        public const string ManaFormatKey = "ui.mvp_loop.panel.mana_format";
        public const string LootFormatKey = "ui.mvp_loop.panel.loot_format";
        public const string LootNamedFormatKey = "ui.mvp_loop.panel.loot_named_format";
        public const string LootEntryFormatKey = "ui.mvp_loop.panel.loot_entry_format";
        public const string HeatFormatKey = "ui.mvp_loop.panel.heat_format";
        public const string ResearchFormatKey = "ui.mvp_loop.panel.research_format";
        public const string ResearchUnlockFormatKey = "ui.mvp_loop.panel.research_unlock_format";
        public const string AdventurerPartyFormatKey = "ui.mvp_loop.panel.adventurer_party_format";
        public const string AdventurerIntentSectionKey = "ui.mvp_loop.section.adventurer_intent";
        public const string AdventurerPressureSectionKey = "ui.mvp_loop.section.adventurer_pressure";
        public const string SuggestionFormatKey = "ui.mvp_loop.panel.suggestion_format";
        public const string CurrentDungeonSectionKey = "ui.mvp_loop.section.current_dungeon";
        public const string LatestRunSectionKey = "ui.mvp_loop.section.latest_run";
        public const string WhyItHappenedSectionKey = "ui.mvp_loop.section.why_it_happened";
        public const string RewardsAndRiskSectionKey = "ui.mvp_loop.section.rewards_and_risk";
        public const string ResearchSectionKey = "ui.mvp_loop.section.research";
        public const string SuggestedNextActionSectionKey = "ui.mvp_loop.section.suggested_next_action";
        public const string SectionLineFormatKey = "ui.mvp_loop.section.line_format";
        public const string InlineSeparatorKey = "ui.mvp_loop.inline_separator";
        public const string RunOutcomeLineFormatKey = "ui.mvp_loop.panel.run_outcome_line_format";
        public const string CasualtyFormatKey = "ui.mvp_loop.panel.casualty_format";
        public const string WhyNoRunKey = "ui.mvp_loop.why.no_run";
        public const string WhyRunFormatKey = "ui.mvp_loop.why.run_format";
        public const string WhyPathCapacityKey = "ui.mvp_loop.why.path_capacity";
        public const string WhyDangerKey = "ui.mvp_loop.why.danger";
        public const string WhyManaPressureKey = "ui.mvp_loop.why.mana_pressure";
        public const string WhyHeatPressureKey = "ui.mvp_loop.why.heat_pressure";
        public const string WhyLootBonusKey = "ui.mvp_loop.why.loot_bonus";
        public const string WhyAttractionKey = "ui.mvp_loop.why.attraction";
        public const string WhyMixedKey = "ui.mvp_loop.why.mixed";
        public const string AnalysisRunFormatKey = "ui.mvp_loop.analysis.run_format";
        public const string AnalysisNoRunKey = "ui.mvp_loop.analysis.no_run";
        public const string AnalysisDangerKey = "ui.mvp_loop.analysis.danger";
        public const string AnalysisHeatIncreasedKey = "ui.mvp_loop.analysis.heat_increased";
        public const string AnalysisPartialLootKey = "ui.mvp_loop.analysis.partial_loot";
        public const string AnalysisStrongLootStableHeatKey = "ui.mvp_loop.analysis.strong_loot_stable_heat";
        public const string AnalysisMixedKey = "ui.mvp_loop.analysis.mixed";
        public const string RiskNoRunKey = "ui.mvp_loop.risk.no_run";
        public const string RiskStableKey = "ui.mvp_loop.risk.stable";
        public const string RiskIncreasedKey = "ui.mvp_loop.risk.increased";
        public const string RiskReducedKey = "ui.mvp_loop.risk.reduced";
        public const string ValueNoPlacementKey = "ui.mvp_loop.value.no_placement";
        public const string ValueNoRunKey = "ui.mvp_loop.value.no_run";
        public const string ValueUnknownKey = "ui.mvp_loop.value.unknown";
        public const string ValueNoResearchKey = "ui.mvp_loop.value.no_research";
        public const string RunSucceededKey = "ui.mvp_loop.run_status.succeeded";
        public const string RunFailedKey = "ui.mvp_loop.run_status.failed";

        public static string BuildPanelText(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            var builder = new StringBuilder();
            AppendLine(builder, Localize(localize, TitleKey));
            AppendSectionLine(builder, localize, CurrentDungeonSectionKey, BuildCurrentDungeonLine(summary, localize));
            AppendSectionLine(builder, localize, AdventurerIntentSectionKey, AdventurerRunIntentPresenter.BuildBodyLine(summary?.AdventurerRunIntent, localize));
            AppendSectionLine(builder, localize, AdventurerPressureSectionKey, AdventurerArrivalPressurePresenter.BuildBodyLine(summary?.AdventurerArrivalPressure, localize));
            AppendSectionLine(builder, localize, LatestRunSectionKey, ResolveRun(summary, localize));
            AppendSectionLine(builder, localize, WhyItHappenedSectionKey, ResolveWhyItHappened(summary, localize));
            AppendSectionLine(builder, localize, RewardsAndRiskSectionKey, BuildRewardsAndRisk(summary, localize));
            AppendSectionLine(builder, localize, ResearchSectionKey, BuildResearchLine(summary, localize));
            AppendSectionLine(builder, localize, SuggestedNextActionSectionKey, string.Format(Localize(localize, SuggestionFormatKey), ResolveKeyOrFallback(ResolveSuggestionKey(summary), localize, MvpPlayerLoopSummaryPresenter.SuggestRunDungeonKey)));
            return builder.ToString();
        }

        private static void AppendSectionLine(StringBuilder builder, Func<string, string, string> localize, string headerKey, string body)
        {
            AppendLine(builder, string.Format(Localize(localize, SectionLineFormatKey), Localize(localize, headerKey), body));
        }

        private static string BuildCurrentDungeonLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            return JoinInline(
                localize,
                string.Format(Localize(localize, CompositionFormatKey), ResolveComposition(summary, localize)),
                string.Format(Localize(localize, PlacementEffectsFormatKey), MvpPlacementEffectsPresenter.BuildEffectsText(summary?.PlacementEffects, localize)));
        }

        private static string ResolveComposition(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved) return Localize(localize, ValueNoPlacementKey);
            if (summary.DungeonPlacements != null && summary.DungeonPlacements.Length > 0) return MvpDungeonPlacementPresenter.BuildCompositionText(summary.DungeonPlacements, localize);
            if (summary.HasPlacementContext && !string.IsNullOrWhiteSpace(summary.SelectedStructureId)) return MvpPlayerFacingLabelResolver.ResolveStructureDisplayName(summary.SelectedStructureId, localize);
            return Localize(localize, ValueNoPlacementKey);
        }

        private static string ResolveRun(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved || !summary.HasRunOutcome) return Localize(localize, ValueNoRunKey);
            string outcome = Localize(localize, summary.RunSucceeded ? RunSucceededKey : RunFailedKey);
            string partyList = BuildPartyList(summary, localize);
            string casualtyLine = BuildCasualtyLine(summary, localize);
            string outcomeLine = string.IsNullOrEmpty(partyList) ? outcome : string.Format(Localize(localize, RunOutcomeLineFormatKey), outcome, partyList);
            return JoinInline(localize, outcomeLine, casualtyLine);
        }

        private static string BuildCasualtyLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.HasRunOutcome || summary.LatestRunPartySize <= 0)
            {
                return string.Empty;
            }

            return string.Format(Localize(localize, CasualtyFormatKey), summary.LatestRunSurvivorCount, summary.LatestRunPartySize, summary.LatestRunDeathCount);
        }

        private static string BuildPartyList(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.AdventurerPartyPreviewResolved || summary.AdventurerPartyClassIds == null || summary.AdventurerPartyClassIds.Length == 0)
            {
                return string.Empty;
            }

            string[] labels = new string[summary.AdventurerPartyClassIds.Length];
            for (int i = 0; i < summary.AdventurerPartyClassIds.Length; i++)
            {
                labels[i] = AdventurerPartyCompositionResolver.ResolveClassLabel(summary.AdventurerPartyClassIds[i], localize);
            }

            return string.Join(", ", labels);
        }

        private static string ResolveWhyItHappened(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved || !summary.HasRunOutcome) return Localize(localize, summary != null && summary.AnalysisUnlocked ? AnalysisNoRunKey : WhyNoRunKey);
            if (summary.AnalysisUnlocked) return string.Format(Localize(localize, AnalysisRunFormatKey), Localize(localize, ResolveDominantCauseKey(summary.LatestRunPlacementEffects)), Localize(localize, ResolveAnalysisCauseKey(summary)));
            return string.Format(Localize(localize, WhyRunFormatKey), Localize(localize, ResolveDominantCauseKey(summary.LatestRunPlacementEffects)));
        }


        private static string ResolveAnalysisCauseKey(MvpPlayerLoopSummary summary)
        {
            if (summary == null || !summary.RuleResolved || !summary.HasRunOutcome) return AnalysisNoRunKey;
            if (string.Equals(ResolveDominantCauseKey(summary.LatestRunPlacementEffects), WhyDangerKey, StringComparison.Ordinal)) return AnalysisDangerKey;
            if (summary.HeatAfter > summary.HeatBefore) return AnalysisHeatIncreasedKey;
            if (summary.LootGeneratedWorldValue > 0 && summary.LootExtractedWorldValue > 0 && summary.LootExtractedWorldValue < summary.LootGeneratedWorldValue) return AnalysisPartialLootKey;
            if (summary.LootGeneratedWorldValue > 0 && summary.LootExtractedWorldValue >= summary.LootGeneratedWorldValue && summary.HeatAfter <= summary.HeatBefore) return AnalysisStrongLootStableHeatKey;
            return AnalysisMixedKey;
        }

        private static string ResolveSuggestionKey(MvpPlayerLoopSummary summary)
        {
            if (summary == null || !summary.AnalysisUnlocked || string.IsNullOrWhiteSpace(summary.AnalysisAdviceKey))
            {
                return summary?.NextOptimizationSuggestionKey;
            }

            return summary.AnalysisAdviceKey;
        }

        private static string ResolveDominantCauseKey(MvpPlacementEffectsSummary effects)
        {
            if (effects == null || !effects.RuleResolved || !MvpPlacementEffectsPresenter.HasAnyEffect(effects)) return WhyMixedKey;

            int best = 0;
            string key = WhyMixedKey;
            SelectDominant(Math.Abs(effects.PathCapacity), WhyPathCapacityKey, ref best, ref key);
            SelectDominant(Math.Abs(effects.Danger), WhyDangerKey, ref best, ref key);
            SelectDominant(Math.Abs(effects.ManaPressure), WhyManaPressureKey, ref best, ref key);
            SelectDominant(Math.Abs(effects.HeatPressure), WhyHeatPressureKey, ref best, ref key);
            SelectDominant(Math.Abs(effects.LootBonus), WhyLootBonusKey, ref best, ref key);
            SelectDominant(Math.Abs(effects.Attraction), WhyAttractionKey, ref best, ref key);
            return key;
        }

        private static void SelectDominant(int value, string candidateKey, ref int bestValue, ref string selectedKey)
        {
            if (value > bestValue)
            {
                bestValue = value;
                selectedKey = candidateKey;
            }
        }

        public static string BuildNamedLootText(RunLootDropRecord[] lootBreakdown, Func<string, string, string> localize)
        {
            if (lootBreakdown == null || lootBreakdown.Length == 0) return string.Empty;
            var parts = new System.Collections.Generic.List<string>(lootBreakdown.Length);
            for (int i = 0; i < lootBreakdown.Length; i++)
            {
                RunLootDropRecord entry = lootBreakdown[i];
                if (entry == null || entry.Quantity <= 0 || string.IsNullOrWhiteSpace(entry.NameKey)) continue;
                string name = Localize(localize, entry.NameKey);
                if (string.Equals(name, entry.NameKey, StringComparison.Ordinal)) continue;
                parts.Add(string.Format(Localize(localize, LootEntryFormatKey), entry.Quantity, name));
            }
            return parts.Count == 0 ? string.Empty : string.Join(", ", parts);
        }

        private static string BuildLootLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            int generated = summary != null && summary.RuleResolved ? summary.LootGeneratedWorldValue : 0;
            int extracted = summary != null && summary.RuleResolved ? summary.LootExtractedWorldValue : 0;
            int tradeable = summary != null && summary.RuleResolved ? summary.LootExtractedTradeableWorldValue : 0;
            string namedLoot = summary != null && summary.RuleResolved ? BuildNamedLootText(summary.LootBreakdown, localize) : string.Empty;
            return !string.IsNullOrWhiteSpace(namedLoot)
                ? string.Format(Localize(localize, LootNamedFormatKey), generated, extracted, tradeable, namedLoot)
                : string.Format(Localize(localize, LootFormatKey), generated, extracted, tradeable);
        }

        private static string BuildRewardsAndRisk(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            return JoinInline(
                localize,
                BuildLootLine(summary, localize),
                ResolveHeatLine(summary, localize),
                string.Format(Localize(localize, ManaFormatKey), summary != null && summary.RuleResolved ? summary.ManaReserve : 0d));
        }

        private static string ResolveHeatLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            double before = summary != null && summary.RuleResolved ? summary.HeatBefore : 0d;
            double after = summary != null && summary.RuleResolved ? summary.HeatAfter : 0d;
            string tier = ResolveKeyOrFallback(summary?.HeatTierId, localize, ValueUnknownKey);
            string riskKey = summary == null || !summary.RuleResolved || !summary.HasRunOutcome ? RiskNoRunKey : after > before ? RiskIncreasedKey : after < before ? RiskReducedKey : RiskStableKey;
            return string.Format(Localize(localize, HeatFormatKey), before, after, tier, Localize(localize, riskKey));
        }

        private static string BuildResearchLine(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            string status = string.Format(Localize(localize, ResearchFormatKey), ResolveResearch(summary, localize));
            if (summary == null || !summary.RuleResolved || !summary.HasResearchUnlock)
            {
                return status;
            }

            return JoinInline(
                localize,
                status,
                string.Format(Localize(localize, ResearchUnlockFormatKey), ResolveResearchUnlock(summary, localize)));
        }

        private static string ResolveResearch(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved || !summary.HasResearchStatus) return Localize(localize, ValueNoResearchKey);
            return MvpPlayerFacingLabelResolver.ResolveResearchStatusLabel(summary.ResearchStatusKey, localize);
        }

        private static string ResolveResearchUnlock(MvpPlayerLoopSummary summary, Func<string, string, string> localize)
        {
            if (summary == null || !summary.RuleResolved) return Localize(localize, ResearchUnlockSummaryPresenter.NoneKey);
            string key = summary.HasResearchUnlock ? summary.ResearchUnlockSummaryKey : ResearchUnlockSummaryPresenter.NoneKey;
            return ResolveKeyOrFallback(key, localize, ResearchUnlockSummaryPresenter.NoneKey);
        }

        private static string JoinInline(Func<string, string, string> localize, params string[] parts)
        {
            if (parts == null || parts.Length == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            string separator = Localize(localize, InlineSeparatorKey);
            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(parts[i]))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(separator);
                }

                builder.Append(parts[i]);
            }

            return builder.ToString();
        }

        private static string ResolveKeyOrFallback(string key, Func<string, string, string> localize, string fallbackKey)
        {
            return string.IsNullOrWhiteSpace(key) ? Localize(localize, fallbackKey) : Localize(localize, key);
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            return localize == null ? key : localize(key, key);
        }

        private static void AppendLine(StringBuilder builder, string line)
        {
            if (builder.Length > 0) builder.Append('\n');
            builder.Append(line ?? string.Empty);
        }
    }
}
