using System;
using System.Globalization;
using DungeonBuilder.M0.Economy;

namespace DungeonBuilder.M0
{
    public static class ContentAcquisitionEconomyPresenter
    {
        public static string Present(ContentAcquisitionEconomySnapshot economy, string categoryId, string optionId,
            double currentMana, Func<string, string> text)
        {
            if (economy == null || !economy.TryPrice(categoryId, optionId, out double price) ||
                !StructuralEconomySnapshot.Nonnegative(currentMana))
                return text(ContentAcquisitionEconomySnapshot.InvalidReason);
            return string.Format(CultureInfo.InvariantCulture, text("ui.content_acquisition.cost"), price, currentMana) +
                "\n" + text(currentMana >= price ? "ui.content_acquisition.affordable" : ContentAcquisitionEconomySnapshot.InsufficientReason);
        }
    }
}
