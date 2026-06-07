using System;

namespace DungeonBuilder.M0
{
    public static class MvpStructurePlacementFeedbackPresenter
    {
        public const string EmptySlotKey = "ui.mvp_structure_feedback.empty_slot";
        public const string ChangedFormatKey = "ui.mvp_structure_feedback.changed_format";

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
