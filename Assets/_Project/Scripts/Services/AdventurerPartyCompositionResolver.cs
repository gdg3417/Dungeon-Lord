using System;
using System.Collections.Generic;

namespace DungeonBuilder.M0
{
    public static class AdventurerPartyCompositionResolver
    {
        public const string WarriorClassId = "adventurer.class.warrior";
        public const string RogueClassId = "adventurer.class.rogue";
        public const string MageClassId = "adventurer.class.mage";
        public const string ClericClassId = "adventurer.class.cleric";
        public const string RangerClassId = "adventurer.class.ranger";

        public static AdventurerPartyCompositionSummary Resolve(Gameplay.RunSimulation.RunParty party)
        {
            return new AdventurerPartyCompositionSummary {
                RuleResolved = party != null,
                DeterministicErrorCode = party == null ? (int)AdventurerPartyCompositionSummaryErrorCode.MissingOrInvalidConfig : 0,
                RuleSourceId = party?.RuleSourceId,
                DeterministicSeed = party?.DeterministicSeed ?? 0,
                ClassIds = party == null ? Array.Empty<string>() : System.Linq.Enumerable.ToArray(
                    System.Linq.Enumerable.Select(party.Members, member => member.ClassId))
            };
        }

        // Legacy API retained for source compatibility; it no longer generates any party.
        public static AdventurerPartyCompositionSummary Resolve(RunSimulationConfig config,
            string runId, long tickStarted, string structureContextId) => Resolve((Gameplay.RunSimulation.RunParty)null);
        public static string ResolveClassLabel(string classId, Func<string, string, string> localize)
        {
            string key = GetClassLabelKey(classId);
            if (localize == null)
            {
                return key == "ui.mvp_adventurer_party.class.unknown" ? key : "ui.mvp_adventurer_party.class.unknown";
            }

            return localize(key, "ui.mvp_adventurer_party.class.unknown");
        }

        public static string GetClassLabelKey(string classId)
        {
            switch (classId)
            {
                case WarriorClassId:
                    return "adventurer.class.warrior.display_name";
                case RogueClassId:
                    return "adventurer.class.rogue.display_name";
                case MageClassId:
                    return "adventurer.class.mage.display_name";
                case ClericClassId:
                    return "adventurer.class.cleric.display_name";
                case RangerClassId:
                    return "adventurer.class.ranger.display_name";
                default:
                    return "ui.mvp_adventurer_party.class.unknown";
            }
        }

        public static bool IsMvpClassId(string classId)
        {
            return classId == WarriorClassId ||
                   classId == RogueClassId ||
                   classId == MageClassId ||
                   classId == ClericClassId ||
                   classId == RangerClassId;
        }

    }
}
