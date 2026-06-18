using System;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0
{
    public static class MvpStructurePlacementFeedbackPresenter
    {
        public const string EmptySlotKey = "ui.mvp_structure_feedback.empty_slot";
        public const string ChangedFormatKey = "ui.mvp_structure_feedback.changed_format";
        public const string PlacementChangedFormatKey = "ui.mvp_placement_feedback.changed_format";
        public const string RoomTargetedPlacementChangedFormatKey = "ui.mvp_placement_feedback.room_changed_format";
        public const string RoomTargetedPlacementAlreadySetFormatKey = "ui.mvp_placement_feedback.room_already_set_format";
        public const string EmptyPlacementValueKey = "ui.mvp_placement_feedback.empty_value";

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

        public static string BuildRoomTargetedPlacementFeedbackText(
            int roomIndex,
            string categoryId,
            string priorOptionId,
            string newOptionId,
            Func<string, string, string> localize)
        {
            string categoryLabel = MvpDungeonPlacementPresenter.ResolveCategoryName(categoryId, localize);
            string priorLabel = ResolvePlacementValueLabel(priorOptionId, localize);
            string newLabel = ResolvePlacementValueLabel(newOptionId, localize);
            if (string.Equals(priorOptionId ?? string.Empty, newOptionId ?? string.Empty, StringComparison.Ordinal))
            {
                string alreadySetFormat = Localize(localize, RoomTargetedPlacementAlreadySetFormatKey);
                return string.Format(alreadySetFormat, roomIndex + 1, categoryLabel, newLabel);
            }

            string format = Localize(localize, RoomTargetedPlacementChangedFormatKey);
            return string.Format(format, roomIndex + 1, categoryLabel, priorLabel, newLabel);
        }

        private static string ResolvePlacementValueLabel(string optionId, Func<string, string, string> localize)
        {
            if (string.IsNullOrWhiteSpace(optionId))
            {
                return Localize(localize, EmptyPlacementValueKey);
            }

            return MvpDungeonPlacementPresenter.ResolveOptionName(optionId, localize);
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
