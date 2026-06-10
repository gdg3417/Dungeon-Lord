using System;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0
{
    public static class MvpStructurePlacementFeedbackPresenter
    {
        public const string EmptySlotKey = "ui.mvp_structure_feedback.empty_slot";
        public const string ChangedFormatKey = "ui.mvp_structure_feedback.changed_format";
        public const string PlacementChangedFormatKey = "ui.mvp_placement_feedback.changed_format";

        public static string BuildFeedbackText(
            string priorStructureId,
            string newStructureId,
            string selectedStructureId,
            Func<string, string, string> localize)
        {
            string priorLabel = ResolvePriorLabel(priorStructureId, localize);
            string newLabel = ResolvePlacedLabel(newStructureId, localize);
            string roleText = MvpStructureImpactPreviewPresenter.BuildPreviewText(selectedStructureId, localize);
            string format = Localize(localize, ChangedFormatKey);
            return string.Format(format, priorLabel, newLabel, roleText);
        }

        public static string BuildPlacementFeedbackText(
            MvpDungeonPlacementEntry priorEntry,
            MvpDungeonPlacementEntry newEntry,
            Func<string, string, string> localize)
        {
            string priorLabel = ResolvePlacementEntryLabel(priorEntry, localize);
            string categoryLabel = newEntry != null
                ? MvpDungeonPlacementPresenter.ResolveCategoryName(newEntry.CategoryId, localize)
                : Localize(localize, MvpDungeonPlacementPresenter.UnknownCategoryKey);
            string optionLabel = newEntry != null
                ? MvpDungeonPlacementPresenter.ResolveOptionName(newEntry.OptionId, localize)
                : Localize(localize, MvpDungeonPlacementPresenter.UnknownOptionKey);
            string preview = newEntry != null
                ? MvpDungeonPlacementPresenter.BuildPreviewText(newEntry.OptionId, localize)
                : MvpDungeonPlacementPresenter.BuildPreviewText(string.Empty, localize);
            string format = Localize(localize, PlacementChangedFormatKey);
            return string.Format(format, priorLabel, categoryLabel, optionLabel, preview);
        }

        private static string ResolvePlacementEntryLabel(MvpDungeonPlacementEntry entry, Func<string, string, string> localize)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.OptionId))
            {
                return Localize(localize, EmptySlotKey);
            }

            return string.Format(
                Localize(localize, MvpDungeonPlacementPresenter.EntryFormatKey),
                MvpDungeonPlacementPresenter.ResolveCategoryName(entry.CategoryId, localize),
                MvpDungeonPlacementPresenter.ResolveOptionName(entry.OptionId, localize));
        }

        private static string ResolvePriorLabel(string structureId, Func<string, string, string> localize)
        {
            if (string.IsNullOrWhiteSpace(structureId) || !MvpPlayerFacingLabelResolver.TryGetStructureDisplayNameKey(structureId, out _))
            {
                return Localize(localize, EmptySlotKey);
            }

            return MvpPlayerFacingLabelResolver.ResolveStructureDisplayName(structureId, localize);
        }

        private static string ResolvePlacedLabel(string structureId, Func<string, string, string> localize)
        {
            if (string.IsNullOrWhiteSpace(structureId))
            {
                return Localize(localize, EmptySlotKey);
            }

            return MvpPlayerFacingLabelResolver.ResolveStructureDisplayName(structureId, localize);
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            if (localize == null)
            {
                return key;
            }

            return localize(key, key);
        }
    }
}
