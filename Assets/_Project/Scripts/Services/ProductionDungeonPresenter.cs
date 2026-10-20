using System;
using System.Globalization;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;

namespace DungeonBuilder.M0
{
    public sealed class DungeonHudPresentation
    {
        public string TotalMana { get; internal set; }
        public string UsableMana { get; internal set; }
        public string ManaPerHour { get; internal set; }
        public string Heat { get; internal set; }
        public string Reserve { get; internal set; }
    }

    public static class ProductionDungeonPresenter
    {
        public static string ForegroundParty(LivingDungeonForegroundInspection view, Func<string,string> text, IFormatProvider culture) =>
            Format(text, culture, "ui.living.party_state", view.Members.Count(m => m.Health > 0), view.Members.Count, view.CarriedLootValue);

        public static string ForegroundInteraction(LivingDungeonForegroundInspection view, Func<string,string> text,
            Func<string,string> contentName, IFormatProvider culture)
        {
            if (!view.RecentInteraction.HasValue) return string.Empty;
            var step = view.RecentInteraction.Value;
            string name = view.InteractionOption == null ? text(step.Kind == RunStepKind.TrapTrigger ? "ui.living.trap" : "ui.living.encounter") : contentName(view.InteractionOption);
            if (step.Kind == RunStepKind.MonsterEngagement) {
                var attacks = view.InteractionCombat.Where(e => e.Kind == CombatC0EventKind.Attack).ToArray();
                if (attacks.Length == 0) {
                    var reason = view.InteractionCombat.LastOrDefault(e => e.Kind == CombatC0EventKind.Bypass || e.Kind == CombatC0EventKind.EncounterComplete);
                    return reason == null ? text("ui.living.action.MonsterEngagement") :
                        Format(text, culture, "ui.living.encounter_skipped", name, text("combat.c0.reason." + reason.Reason));
                }
                var hits = attacks.Where(e => e.TargetId?.StartsWith(view.RunId + "/member/", StringComparison.Ordinal) == true).ToArray();
                return Format(text, culture, "ui.living.encounter_result", name, attacks.Length,
                    hits.Sum(e => (long)e.HealthBefore - e.HealthAfter), hits.Where(e => e.HealthBefore > 0 && e.HealthAfter == 0).Select(e => e.TargetId).Distinct().Count());
            }
            if (step.Kind == RunStepKind.TrapTrigger && step.Encounter != null)
                return Format(text, culture, "ui.living.trap_result", name, step.Encounter.HealthBefore - step.Encounter.HealthAfter,
                    step.Encounter.HealthAfter == 0 ? text("ui.living.casualty") : string.Empty);
            return step.Kind == RunStepKind.LootInteraction ? Format(text, culture, "ui.living.loot_result", step.CarriedLootValueAfter) :
                text("ui.living.action." + step.Kind);
        }

        public static string ForegroundOutcome(RunOutcomeRecord outcome, Func<string,string> text, IFormatProvider culture)
        {
            string result = Format(text, culture, "ui.living.latest", outcome.RunId, outcome.SurvivalSummary.SurvivorCount, outcome.SurvivalSummary.DeathCount);
            if (!string.IsNullOrEmpty(outcome.FinalRouteOutcomeKey)) result += " " + text(outcome.FinalRouteOutcomeKey);
            if (outcome.LootExtractionSummary?.RuleResolved == true)
                result += "\n" + Format(text, culture, "ui.living.extracted", outcome.LootExtractionSummary.TotalExtractedWorldValue);
            return result;
        }
        public static string ReplayPartyIdentity(Func<string,string> text,IFormatProvider culture,string className,string capturedNameOrTitle=null) =>
            string.IsNullOrWhiteSpace(capturedNameOrTitle) ? Format(text,culture,"ui.dungeon.replay.party_identity",className) :
                Format(text,culture,"ui.dungeon.replay.party_named_identity",capturedNameOrTitle,className);
        public static string ReplayPartyRow(Func<string,string> text,IFormatProvider culture,string className,
            int currentHealth,int maximumHealth,string capturedNameOrTitle=null) =>
            string.IsNullOrWhiteSpace(capturedNameOrTitle) ? Format(text,culture,"ui.dungeon.replay.party_row",className,currentHealth,maximumHealth) :
                Format(text,culture,"ui.dungeon.replay.party_named_row",capturedNameOrTitle,className,currentHealth,maximumHealth);
        public static DungeonHudPresentation Hud(SaveData save, RunSimulationConfig configuration,
            double manaPerHour, Func<string, string> text, IFormatProvider culture, MonsterRecoveryRules recoveryRules = null)
        {
            double wallet = save?.structureRuntime?.ManaReserve ?? double.NaN;
            string amount = double.IsFinite(wallet) ? PlayerManaPresentationFormatter.FormatLiveBalance(wallet,
                manaPerHour, culture) : text("ui.dungeon.unavailable");
            long reserve = save?.validatedCanonicalSpatialState == null ? 0 : MonsterContinuity.RequiredReserve(save.validatedCanonicalSpatialState, recoveryRules ?? MonsterRecoveryRules.Capture(configuration));
            string usable = double.IsFinite(wallet) ? PlayerManaPresentationFormatter.FormatLiveBalance(Math.Max(0, wallet - reserve), manaPerHour, culture) : text("ui.dungeon.unavailable");
            var tier = CurrentHeatTierResolver.Resolve(configuration, save?.structureRuntime?.Heat ?? double.NaN);
            return new DungeonHudPresentation {
                TotalMana = Format(text, culture, "ui.dungeon.hud.total", amount),
                UsableMana = Format(text, culture, "ui.dungeon.hud.usable", usable),
                Reserve = Format(text, culture, "combat.c2.reserve", reserve) + (wallet < reserve ? "\n" + text(MonsterRecoveryRules.Insufficient) : string.Empty),
                ManaPerHour = Format(text, culture, "ui.dungeon.hud.rate", double.IsFinite(manaPerHour)
                    ? PlayerManaPresentationFormatter.FormatDiscreteAmount(manaPerHour, culture) : text("ui.dungeon.unavailable")),
                Heat = Format(text, culture, "ui.dungeon.hud.heat", text(tier?.TierId ?? "ui.passive_mana.heat_unknown")) };
        }
        public static string MonsterState(MonsterContinuityRecord monster, string option, MonsterRecoveryRules rules,
            Func<string,string> text, IFormatProvider culture) => Format(text, culture, "combat.c2.monster", monster.Level, monster.Health,
                rules.MaximumHealth(option), rules.Readiness(option), text(monster.RevivalRequired ? "combat.c2.revival_pending" : "combat.c2.living"));
        public static string Format(Func<string, string> text, IFormatProvider culture, string key, params object[] values) =>
            string.Format(culture ?? CultureInfo.CurrentCulture, text(key), values);
        public static string DraftStatus(TransactionalDungeonDraft draft) => draft?.Reason ??
            (draft == null ? "ui.dungeon.normal" : draft.HasChanges ? "ui.dungeon.draft.acknowledged" : "ui.dungeon.draft.clean");
    }
}
