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
    }

    public static class ProductionDungeonPresenter
    {
        public static DungeonHudPresentation Hud(SaveData save, RunSimulationConfig configuration,
            double manaPerHour, Func<string, string> text, IFormatProvider culture)
        {
            double wallet = save?.structureRuntime?.ManaReserve ?? double.NaN;
            string amount = double.IsFinite(wallet) ? PlayerManaPresentationFormatter.FormatLiveBalance(wallet,
                manaPerHour, culture) : text("ui.dungeon.unavailable");
            // Current production economy has one spendable wallet and no live upkeep reservation owner.
            // Both values consume that authority; introduce a read-only reservation seam when it exists.
            var tier = CurrentHeatTierResolver.Resolve(configuration, save?.structureRuntime?.Heat ?? double.NaN);
            return new DungeonHudPresentation {
                TotalMana = Format(text, culture, "ui.dungeon.hud.total", amount),
                UsableMana = Format(text, culture, "ui.dungeon.hud.usable", amount),
                ManaPerHour = Format(text, culture, "ui.dungeon.hud.rate", double.IsFinite(manaPerHour)
                    ? PlayerManaPresentationFormatter.FormatDiscreteAmount(manaPerHour, culture) : text("ui.dungeon.unavailable")),
                Heat = Format(text, culture, "ui.dungeon.hud.heat", text(tier?.TierId ?? "ui.passive_mana.heat_unknown")) };
        }
        public static string Format(Func<string, string> text, IFormatProvider culture, string key, params object[] values) =>
            string.Format(culture ?? CultureInfo.CurrentCulture, text(key), values);
        public static string DraftStatus(TransactionalDungeonDraft draft) => draft?.Reason ??
            (draft == null ? "ui.dungeon.normal" : draft.HasChanges ? "ui.dungeon.draft.acknowledged" : "ui.dungeon.draft.clean");
    }
}
