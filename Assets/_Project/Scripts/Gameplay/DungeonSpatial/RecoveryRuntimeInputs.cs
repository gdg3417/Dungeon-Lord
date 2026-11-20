using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using DungeonBuilder.M0.Economy;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // Immutable consumed-byte bundle. All dependent services are constructed from
    // these retained bytes; a later Resources.Load cannot supply identity evidence.
    internal sealed class RecoveryRuntimeInputs
    {
        internal RecoveryContextIdentity Identity { get; }
        internal ResearchTimingRules ResearchTiming { get; }
        internal RunSimulationConfig Configuration { get; }
        internal LootConfig Loot { get; }
        internal ContentBootstrap Bootstrap { get; }
        internal PassiveOnlineManaConfigurationSnapshot Passive { get; }
        internal StructuralEconomySnapshot Economy { get; }
        internal MonsterRecoveryRules Monsters { get; }
        internal LivingDungeonForegroundConfig OneFloor { get; }
        internal LivingDungeonForegroundConfig TwoFloor { get; }
        internal RoomContentSpatialOccupancySnapshot Occupancy { get; }
        internal SpatialLayoutCompatibilitySnapshot Compatibility { get; }
        private readonly string configurationValues, combatValues, bootstrapValues, lootValues, occupancyValues;

        internal RecoveryRuntimeInputs(byte[][] bytes, ProductionSpatialContentSnapshot production,
            SpatialContentValidationWorkloadLimits structuralLimits, CanonicalSpatialSerializationLimits limits,
            IEnumerable<byte[]> researchRules = null, IEnumerable<string> researchSelections = null)
        {
            Identity = new RecoveryContextIdentity(bytes);
            string Text(int index) => new UTF8Encoding(false, true).GetString(Identity.GetInput(index));
            if (!BootstrapConfigValidationService.TryParseRunSimulationConfig(Text(0), out var config) ||
                production == null ||
                !ProductionSpatialGeneratedSetParser.SerializeCanonical(production.Manifest).AsSpan().SequenceEqual(Identity.GetInput(4)) ||
                !ProductionSpatialGeneratedSetParser.SerializeCanonical(production.Catalog).AsSpan().SequenceEqual(Identity.GetInput(5)))
                throw new ArgumentException(RecoveryEpisode.ContextMismatch);
            Configuration = config;
            var passive = PassiveOnlineManaConfigurationSnapshot.Load(Identity.GetInput(1), limits);
            if (!passive.IsSuccess || !StructuralEconomySnapshot.TryParse(Identity.GetInput(2), production.Catalog, limits, out var economy))
                throw new ArgumentException(RecoveryEpisode.ContextMismatch);
            Passive = passive.Value; Economy = economy;
            Monsters = MonsterRecoveryRules.CaptureBound(config, Identity.GetInput(3));
            OneFloor = LivingDungeonForegroundConfig.Parse(Identity.GetInput(6));
            TwoFloor = LivingDungeonForegroundConfig.Parse(Identity.GetInput(7));
            if (OneFloor.TwoFloor || !TwoFloor.TwoFloor || OneFloor.UnitsPerSecond != TwoFloor.UnitsPerSecond ||
                !RoomContentSpatialOccupancyAuthority.TryParse(Identity.GetInput(8), structuralLimits, config, out var occupancy))
                throw new ArgumentException(RecoveryEpisode.ContextMismatch);
            ResearchTiming = new ResearchTimingRules(researchRules ?? Array.Empty<byte[]>(),
                researchSelections ?? Array.Empty<string>(), OneFloor.UnitsPerSecond, limits.Serialized);
            Occupancy = occupancy;
            var compatibility = SpatialLayoutCompatibilityProfiles.ParseAndValidate(Identity.GetInput(9), production, structuralLimits,
                enforceProductionReleasePolicy: true);
            if (!compatibility.Success) throw new ArgumentException(RecoveryEpisode.ContextMismatch);
            Compatibility = compatibility.Value;
            Bootstrap = JsonUtility.FromJson<ContentBootstrap>(Text(10));
            Loot = BootstrapConfigValidationService.TryParseLootConfig(Text(11));
            if (Bootstrap?.schema != "content_bootstrap" || Bootstrap.schemaVersion != 1 || Bootstrap.tickSeconds <= 0 ||
                Bootstrap.timeRules == null || Bootstrap.timeRules.detectClockSkewSeconds <= 0 ||
                Bootstrap.timeRules.activeSaveIntervalSeconds <= 0 || Loot == null ||
                !LootRollResolver.Resolve(Loot, config.LootTableId, 0).success ||
                Loot.tables?.Any(t => t.id == config.LootTableId) != true || !ResearchValid(null))
                throw new ArgumentException(RecoveryEpisode.ContextMismatch);
            // Derived process-local mutation guards, never digest inputs or save
            // authority. Identity continues to bind the exact validated bytes.
            configurationValues = JsonUtility.ToJson(Configuration);
            combatValues = JsonUtility.ToJson(Configuration.CombatC0);
            bootstrapValues = JsonUtility.ToJson(Bootstrap);
            lootValues = JsonUtility.ToJson(Loot);
            occupancyValues = JsonUtility.ToJson(Occupancy.Value);
        }

        // These legacy DTOs expose mutable fields. Reference identity alone must
        // not let a changed value execute under the original immutable digest.
        // CombatC0 is explicitly included because the legacy config serializer
        // intentionally excludes it. No reload, rebinding or repaired value.
        internal bool ConsumedValuesStillBound(RoomContentSpatialOccupancySnapshot contextOccupancy)
        {
            try {
                return string.Equals(configurationValues, JsonUtility.ToJson(Configuration), StringComparison.Ordinal) &&
                    string.Equals(combatValues, JsonUtility.ToJson(Configuration.CombatC0), StringComparison.Ordinal) &&
                    string.Equals(bootstrapValues, JsonUtility.ToJson(Bootstrap), StringComparison.Ordinal) &&
                    string.Equals(lootValues, JsonUtility.ToJson(Loot), StringComparison.Ordinal) &&
                    string.Equals(occupancyValues, JsonUtility.ToJson(Occupancy.Value), StringComparison.Ordinal) &&
                    contextOccupancy != null && string.Equals(occupancyValues, JsonUtility.ToJson(contextOccupancy.Value), StringComparison.Ordinal);
            } catch { return false; }
        }

        // Existing pure Research authorities validate the actual saved state.
        // No tick, completion mutation, claim or reward occurs during validation.
        internal bool ResearchValid(SaveData save)
        {
            var authority = PlayerResearchClaimAuthorityResolver.Resolve(Configuration.MvpFirstSessionObjective?.AnalysisResearchProjectId,
                false, save?.researchPending, save?.researchProgress, save?.completedResearch,
                Bootstrap.researchPendingScaffold, Bootstrap.researchProgressScaffold,
                Bootstrap.researchCompletionEligibilityScaffold, Bootstrap.researchCompletionClaimScaffold,
                Bootstrap.researchVerificationScaffold, new GateEvaluationResult(false, string.Empty), new GateEvaluationResult(false, string.Empty));
            if (!authority.RuleResolved) return false;
            if (save?.researchPending == null) return save?.researchProgress == null;
            return ResearchCompletionEligibilityResolver.Resolve(save.researchPending, save.researchProgress,
                Bootstrap.researchCompletionEligibilityScaffold).RuleResolved;
        }

        internal string ValidateCurrentResearch(SaveData save, ContractJsonNode wire, LivingDungeonState living)
        {
            const string invalid = ResearchTimingRules.StateInvalid;
            var progress = save.researchProgress;
            var pending = save.researchPending;
            if (pending == null || progress == null)
                return pending == null && progress == null && ResearchValid(save) ? null : invalid;
            if (pending.SlotId != Bootstrap.researchPendingScaffold.slotId ||
                pending.ProjectId != Configuration.MvpFirstSessionObjective?.AnalysisResearchProjectId ||
                progress.SlotId != pending.SlotId || progress.ProjectId != pending.ProjectId ||
                save.completedResearch?.ProjectIds?.Contains(pending.ProjectId, StringComparer.Ordinal) == true ||
                wire?.Kind != ContractJsonKind.Object)
                return invalid;

            var cut = wire.Fields.SingleOrDefault(field =>
                string.Equals(field.Key, "StartClosedThrough", StringComparison.OrdinalIgnoreCase));
            if (cut.Value == null && progress.RuleSourceIdUsed == Bootstrap.researchProgressScaffold.ruleSourceId)
                return ResearchValid(save) && (!progress.CompletionPending ||
                    progress.ProgressUnits >= Bootstrap.researchCompletionEligibilityScaffold.requiredProgressUnits)
                    ? null : invalid;

            string reason = ResearchTiming.Resolve(progress.RuleSourceIdUsed, out var rule);
            if (reason != null) return reason;
            if (rule.ProjectId != progress.ProjectId || rule.SlotId != progress.SlotId) return ResearchTimingRules.Mismatch;
            if (cut.Key != "StartClosedThrough" ||
                !RecoveryEpisode.TryInteger(cut.Value, out long start) || start != progress.StartClosedThrough ||
                living?.Scheduler == null || start < 0 || start > living.Scheduler.ClosedThrough)
                return invalid;
            var known = wire.Fields.Where(field => DetachedRecognizedSaveStateSnapshot.ResearchProgressMembers
                .Any(member => string.Equals(field.Key, member, StringComparison.OrdinalIgnoreCase))).ToArray();
            if (!known.Select(field => field.Key).SequenceEqual(DetachedRecognizedSaveStateSnapshot.ResearchProgressMembers) ||
                !RecoveryEpisode.TryInteger(known[2].Value, out long earned) ||
                earned > ResearchTimingRule.MaximumExactUnits || progress.ProgressUnits != earned ||
                !ContractJson.String(known[0].Value, out string slot) || slot != progress.SlotId ||
                !ContractJson.String(known[1].Value, out string project) || project != progress.ProjectId ||
                known[3].Value.Kind != ContractJsonKind.Boolean || (known[3].Value.Text == "true") != progress.CompletionPending ||
                !ContractJson.String(known[4].Value, out string reference) || reference != rule.Reference)
                return invalid;

            long tickWidth = (long)Bootstrap.tickSeconds * OneFloor.UnitsPerSecond;
            return earned == rule.EarnedThrough(start, living.Scheduler.ClosedThrough, tickWidth) &&
                progress.CompletionPending == (earned == rule.RequiredUnits) ? null : invalid;
        }

        internal bool SupportsProfile(LivingDungeonProfile profile)
        {
            if (profile == null || !LivingDungeonForegroundConfig.IsProduction(profile.Id)) return false;
            if (!profile.BaseBudget.IsValid) return false;
            var expected = (profile.Id == OneFloor.Id ? OneFloor : TwoFloor).CreateProfile(profile.BaseBudget);
            return profile.MaximumActive == expected.MaximumActive && profile.MaximumRetained == expected.MaximumRetained &&
                profile.MaximumCheckpointBytes == expected.MaximumCheckpointBytes && profile.MaximumPartyReplay == expected.MaximumPartyReplay &&
                profile.MaximumTotalReplay == expected.MaximumTotalReplay && profile.MaximumBaseSaveBytes == expected.MaximumBaseSaveBytes &&
                profile.MaximumOwnerBytes == expected.MaximumOwnerBytes && profile.MaximumCombatWork == expected.MaximumCombatWork &&
                profile.QualificationPostureId == expected.QualificationPostureId && profile.QualificationTiming != null &&
                profile.QualificationTiming.UnitsPerTileEdge == expected.QualificationTiming.UnitsPerTileEdge &&
                profile.QualificationTiming.MonsterInteractionUnits == expected.QualificationTiming.MonsterInteractionUnits &&
                profile.QualificationTiming.TrapInteractionUnits == expected.QualificationTiming.TrapInteractionUnits &&
                profile.QualificationTiming.LootInteractionUnits == expected.QualificationTiming.LootInteractionUnits &&
                profile.QualificationTiming.FloorTransferUnits == expected.QualificationTiming.FloorTransferUnits;
        }
    }
}
