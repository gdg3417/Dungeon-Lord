using System;
using System.Globalization;
using DungeonBuilder.M0.Economy;

namespace DungeonBuilder.M0
{
    public static class ContentAcquisitionEconomyPresenter
    {
        public static string Present(ContentAcquisitionEconomySnapshot economy, string categoryId, string optionId,
            double currentMana, Func<string, string> text, StructuralEconomyPreview preview = null)
        {
            if (economy == null || !economy.TryPrice(categoryId, optionId, out double price) ||
                !StructuralEconomySnapshot.Nonnegative(currentMana))
                return text(ContentAcquisitionEconomySnapshot.InvalidReason);
            return string.Format(CultureInfo.InvariantCulture, text("ui.content_acquisition.cost"), price, currentMana) +
                (preview == null ? string.Empty : "\n" +
                    string.Format(CultureInfo.InvariantCulture, text("combat.c2.reserve"), preview.RequiredReserveAfter) +
                    "\n" + text(preview.IsAffordable ? "ui.content_acquisition.affordable" : preview.Reason));
        }
    }
}
