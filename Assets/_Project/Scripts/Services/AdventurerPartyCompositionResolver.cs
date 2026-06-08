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

        public static AdventurerPartyCompositionSummary Resolve(
            RunSimulationConfig config,
            string runId,
            long tickStarted,
            string structureContextId)
        {
            int seed = ComputeSeed(runId, tickStarted, structureContextId);
            if (config == null ||
                string.IsNullOrWhiteSpace(config.AdventurerPartyCompositionRuleSourceId) ||
                config.AdventurerPartyCompositionMinSize < 1 ||
                config.AdventurerPartyCompositionMaxSize < config.AdventurerPartyCompositionMinSize ||
                config.AdventurerPartyCompositionMaxAllowedSize < 1 ||
                config.AdventurerPartyCompositionMaxSize > config.AdventurerPartyCompositionMaxAllowedSize)
            {
                return CreateError(config, seed, AdventurerPartyCompositionSummaryErrorCode.MissingOrInvalidConfig);
            }

            string[] allowedClasses = FilterMvpClassIds(config.AdventurerPartyCompositionClassIds);
            if (allowedClasses.Length == 0)
            {
                return CreateError(config, seed, AdventurerPartyCompositionSummaryErrorCode.NoAllowedMvpClasses);
            }

            int sizeRange = config.AdventurerPartyCompositionMaxSize - config.AdventurerPartyCompositionMinSize + 1;
            int partySize = config.AdventurerPartyCompositionMinSize + PositiveModulo(seed, sizeRange);
            partySize = Math.Min(partySize, allowedClasses.Length);

            int startIndex = PositiveModulo(seed, allowedClasses.Length);
            var classIds = new List<string>(partySize);
            for (int i = 0; i < partySize; i++)
            {
                classIds.Add(allowedClasses[(startIndex + i) % allowedClasses.Length]);
            }

            return new AdventurerPartyCompositionSummary
            {
                RuleSourceId = config.AdventurerPartyCompositionRuleSourceId,
                DeterministicSeed = seed,
                RuleResolved = true,
                DeterministicErrorCode = (int)AdventurerPartyCompositionSummaryErrorCode.None,
                ClassIds = classIds.ToArray()
            };
        }

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

        private static AdventurerPartyCompositionSummary CreateError(
            RunSimulationConfig config,
            int seed,
            AdventurerPartyCompositionSummaryErrorCode code)
        {
            return new AdventurerPartyCompositionSummary
            {
                RuleSourceId = config != null ? config.AdventurerPartyCompositionRuleSourceId : string.Empty,
                DeterministicSeed = seed,
                RuleResolved = false,
                DeterministicErrorCode = (int)code,
                ClassIds = Array.Empty<string>()
            };
        }

        private static string[] FilterMvpClassIds(string[] configuredClassIds)
        {
            if (configuredClassIds == null || configuredClassIds.Length == 0)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>(configuredClassIds.Length);
            for (int i = 0; i < configuredClassIds.Length; i++)
            {
                string classId = configuredClassIds[i];
                if (!IsMvpClassId(classId) || result.Contains(classId))
                {
                    continue;
                }

                result.Add(classId);
            }

            return result.ToArray();
        }

        private static int ComputeSeed(string runId, long tickStarted, string structureContextId)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + StableStringHash(runId);
                hash = (hash * 31) + (int)(tickStarted & 0xFFFFFFFFL);
                hash = (hash * 31) + (int)((tickStarted >> 32) & 0xFFFFFFFFL);
                hash = (hash * 31) + StableStringHash(structureContextId);
                return hash;
            }
        }

        private static int StableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 23;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash * 31) + value[i];
                }

                return hash;
            }
        }

        private static int PositiveModulo(int value, int modulo)
        {
            if (modulo <= 0)
            {
                return 0;
            }

            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }
    }
}
