using System;
using System.Globalization;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0
{
    public static class StructuralEconomyPresenter
    {
        public const double WholeBalancePassiveManaPerHourThreshold = 3600d;

        public static string FormatTransactionAmount(double value) =>
            value.ToString("0.#", CultureInfo.InvariantCulture);

        public static string FormatBalance(double value, double passiveManaPerHour,
            bool hasFractionalTransaction)
        {
            bool useWholeMana = !hasFractionalTransaction &&
                passiveManaPerHour >= WholeBalancePassiveManaPerHourThreshold;
            return value.ToString(useWholeMana ? "0" : "0.#", CultureInfo.InvariantCulture);
        }

        public static string Present(StructuralEconomyPreview preview, Func<string, string> text,
            double passiveManaPerHour = double.NaN)
        {
            if (preview == null || (preview.Reason != null && preview.Reason != StructuralEconomyService.InsufficientReason))
                return text(StructuralEconomyService.InvalidReason);
            bool hasFractionalTransaction = HasMeaningfulFraction(preview.BaseCost) ||
                HasMeaningfulFraction(preview.Cost) || HasMeaningfulFraction(preview.RefundBasis) ||
                HasMeaningfulFraction(preview.Refund) || HasMeaningfulFraction(preview.CreditedRefund);
            bool removal = preview.Operation == StructuralEditOperation.Deletion ||
                preview.Operation == StructuralEditOperation.OptionalBranchRemoval;
            string amount = removal
                ? string.Format(CultureInfo.InvariantCulture, text("ui.structural.economy.refund"),
                    FormatTransactionAmount(preview.RefundBasis), FormatTransactionAmount(preview.Refund),
                    FormatTransactionAmount(preview.CreditedRefund))
                : string.Format(CultureInfo.InvariantCulture, text("ui.structural.economy.cost"),
                    FormatTransactionAmount(preview.BaseCost), FormatTransactionAmount(preview.Cost));
            if (!removal && preview.RefundBasis > 0)
                amount += "\n" + string.Format(CultureInfo.InvariantCulture, text("ui.structural.economy.refund"),
                    FormatTransactionAmount(preview.RefundBasis), FormatTransactionAmount(preview.Refund),
                    FormatTransactionAmount(preview.CreditedRefund));
            return amount + "\n" + string.Format(CultureInfo.InvariantCulture,
                text("ui.structural.economy.balance"),
                FormatBalance(preview.CurrentMana, passiveManaPerHour, hasFractionalTransaction),
                FormatBalance(preview.ResultingMana, passiveManaPerHour, hasFractionalTransaction)) + "\n" +
                text(preview.IsAffordable ? "ui.structural.economy.affordable" : StructuralEconomyService.InsufficientReason);
        }

        private static bool HasMeaningfulFraction(double value) =>
            Math.Abs(value - Math.Round(value)) > 0.000000001d;
    }
}
