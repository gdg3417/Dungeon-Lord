using System;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.M0
{
    public static class MvpStructureImpactPreviewPresenter
    {
        public const string ManaGeneratorPreviewKey = "ui.mvp_structure_preview.mana_generator";
        public const string HeatScrubberPreviewKey = "ui.mvp_structure_preview.heat_scrubber";
        public const string RiskLabPreviewKey = "ui.mvp_structure_preview.risk_lab";
        public const string UnknownPreviewKey = "ui.mvp_structure_preview.unknown";

        public static string ResolvePreviewKey(string structureId)
        {
            switch (structureId)
            {
                case StructureSimulationPass.ManaGeneratorBasicId:
                    return ManaGeneratorPreviewKey;
                case StructureSimulationPass.HeatScrubberBasicId:
                    return HeatScrubberPreviewKey;
                case StructureSimulationPass.RiskLabBasicId:
                    return RiskLabPreviewKey;
                default:
                    return UnknownPreviewKey;
            }
        }

        public static string BuildPreviewText(string structureId, Func<string, string, string> localize)
        {
            return Localize(localize, ResolvePreviewKey(structureId));
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
