using System;
using System.Globalization;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0
{
    public static class StructuralEconomyPresenter
    {
        public static string Present(StructuralEconomyPreview preview, Func<string, string> text)
        {
            if (preview == null || (preview.Reason != null && preview.Reason != StructuralEconomyService.InsufficientReason))
                return text(StructuralEconomyService.InvalidReason);
            string amount = preview.Spatial?.Operation == StructuralEditOperation.Deletion
                ? string.Format(CultureInfo.InvariantCulture, text("ui.structural.economy.refund"),
                    preview.RefundBasis, preview.Refund, preview.CreditedRefund)
                : string.Format(CultureInfo.InvariantCulture, text("ui.structural.economy.cost"), preview.BaseCost, preview.Cost);
            if (preview.Spatial?.Operation != StructuralEditOperation.Deletion && preview.RefundBasis > 0)
                amount += "\n" + string.Format(CultureInfo.InvariantCulture, text("ui.structural.economy.refund"),
                    preview.RefundBasis, preview.Refund, preview.CreditedRefund);
            return amount + "\n" + string.Format(CultureInfo.InvariantCulture,
                text("ui.structural.economy.balance"), preview.CurrentMana, preview.ResultingMana) + "\n" +
                text(preview.IsAffordable ? "ui.structural.economy.affordable" : StructuralEconomyService.InsufficientReason);
        }
    }
}
