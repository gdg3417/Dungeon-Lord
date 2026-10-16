using System;
using System.Globalization;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

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
