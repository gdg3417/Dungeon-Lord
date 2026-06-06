using System;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.M0
{
    public static class MvpPlayerFacingLabelResolver
    {
        public const string UnknownStructureKey = "ui.mvp_label.structure.unknown";
        public const string ResearchUnavailableKey = "ui.research.status.blocked_or_invalid";

        public static string ResolveStructureDisplayName(string structureId, Func<string, string, string> localize)
        {
            return TryGetStructureDisplayNameKey(structureId, out string key)
                ? Localize(localize, key)
                : Localize(localize, UnknownStructureKey);
        }

        public static bool TryGetStructureDisplayNameKey(string structureId, out string key)
        {
            switch (structureId)
            {
                case StructureSimulationPass.ManaGeneratorBasicId:
                    key = "structure.mana_generator.basic.display_name";
                    return true;
                case StructureSimulationPass.HeatScrubberBasicId:
                    key = "structure.heat_scrubber.basic.display_name";
                    return true;
                case StructureSimulationPass.RiskLabBasicId:
                    key = "structure.risk_lab.basic.display_name";
                    return true;
                default:
                    key = string.Empty;
                    return false;
            }
        }

        public static string ResolveResearchStatusLabel(string statusLocalizationKey, Func<string, string, string> localize)
        {
            return string.IsNullOrWhiteSpace(statusLocalizationKey)
                ? Localize(localize, ResearchUnavailableKey)
                : Localize(localize, statusLocalizationKey);
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
