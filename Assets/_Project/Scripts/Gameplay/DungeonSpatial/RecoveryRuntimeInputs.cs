using System;
using System.Linq;
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

        internal RecoveryRuntimeInputs(byte[][] bytes, ProductionSpatialContentSnapshot production,
            SpatialContentValidationWorkloadLimits structuralLimits, CanonicalSpatialSerializationLimits limits)
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
    }
}
