using System;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public partial class GameRoot
    {
        private static bool UsesAtomicRecoveryRuntime => CanonicalSaveSchemaVersions.CurrentWritableTarget == CanonicalSaveSchemaVersions.AtomicRecoveryIntroduction;
        private RecoveryRuntimeInputs recoveryRuntimeInputs;
        private bool bootRecoveryPending;
        private string bootRecoveryBanner;

        private bool TryConfigureRecoveryRuntime(SpatialContentValidationWorkloadLimits structuralLimits)
        {
            try {
                if (recoveryRuntimeInputs != null || TimeService != null) throw new InvalidOperationException(RecoveryEpisode.Invalid);
                byte[] Resource(string name) => Resources.Load<TextAsset>(name)?.bytes ?? throw new InvalidOperationException(RecoveryEpisode.ContextMismatch);
                // Each immutable asset is captured once. Running dependencies are
                // then taken from this validated bundle, never a later reload.
                var bytes = new[] { runSimulationConfigJson?.bytes, Resource("passive_online_mana"), Resource("structural_economy"),
                    Resource("monster_recovery"), productionSpatialManifest?.bytes, productionSpatialCatalog?.bytes,
                    Resource("living_dungeon_foreground"), Resource("living_dungeon_foreground_v2"), roomContentSpatialOccupancyJson?.bytes,
                    spatialLayoutCompatibilityProfilesJson?.bytes, contentBootstrapJson?.bytes, lootConfigJson?.bytes };
                var inputs = new RecoveryRuntimeInputs(bytes, Content.ProductionSpatialContent, structuralLimits, SaveSpatialMigrationLimits.Canonical);
                Content.BindRecoveryBootstrap(inputs);
                ConfigureCanonicalPassiveMana(inputs.Passive, inputs.Economy, SaveSpatialMigrationLimits.Canonical.Spatial,
                    inputs.Bootstrap.tickSeconds, inputs.Bootstrap.timeRules.activeSaveIntervalSeconds);
                _offlineHeatConfiguration = inputs.Configuration;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                var qualificationClock = LivingDungeonForegroundDevelopmentSmoke.ReopenClock(
                    Environment.GetCommandLineArgs(), Application.productName, SaveService.SavePath);
                TimeService = qualificationClock == null ? new TimeService(Logger, inputs.Bootstrap.tickSeconds, inputs.Bootstrap.timeRules.detectClockSkewSeconds) :
                    new TimeService(Logger, inputs.Bootstrap.tickSeconds, inputs.Bootstrap.timeRules.detectClockSkewSeconds, new SystemTimeSource(), qualificationClock);
#else
                TimeService = new TimeService(Logger, inputs.Bootstrap.tickSeconds, inputs.Bootstrap.timeRules.detectClockSkewSeconds);
#endif
                TimeService.BeginRecoveryForeground(inputs.TwoFloor.UnitsPerSecond);
                TimeService.SetForegroundFocus(foregroundApplicationFocused && isActiveAndEnabled && Application.isFocused);
                if (foregroundApplicationPaused) TimeService.OnPause();
                SaveService.ConfigureRecovery(inputs, TimeService);
                recoveryRuntimeInputs = inputs;
                return true;
            } catch (Exception error) {
                Logger?.Warn("Recovery bootstrap refused: " + error.Message);
                string key = RecoveryEpisode.PlayerLocalizationKey(RecoveryEpisode.ContextMismatch);
                SetBanner(Content.GetString(key, key));
                return false;
            }
        }

        private void PumpCanonicalRecovery()
        {
            if (foregroundPumping || Save == null || TimeService?.RecoveryClockActive != true) return;
            foregroundPumping = true;
            try {
                var result = SaveService.AdvanceCanonicalRecovery(Save, _offlinePassiveManaService, ApplyForegroundPassiveTick);
                if (!result.IsSuccess) {
                    if (result.Reason == DetachedCanonicalWriteAuthority.AtomicSaveFailedReason) {
                        foregroundRetryPending = true;
                        ForegroundActivity = new LivingDungeonForegroundInspection { StateKey = "ui.living.retryable", NextBoundary = long.MaxValue };
                        Logger?.Warn(result.Reason);
                    } else ForegroundFault(result.Reason);
                    return;
                }
                foregroundRetryPending = false; foregroundDirty = true;
                if (!TimeService.RecoveryClockActive && !TimeService.HasDeferredRecovery && bootRecoveryPending) FinishRecoveryBoot();
            } catch { ForegroundFault(RecoveryEpisode.Invalid); }
            finally { foregroundPumping = false; }
        }

        private void FinishRecoveryBoot()
        {
            if (!bootRecoveryPending || TimeService.RecoveryClockActive || TimeService.HasDeferredRecovery) return;
            bootRecoveryPending = false;
            if (CompleteSuccessfulBoot(Save, bootRecoveryBanner)) GoHomeStub();
            else ForegroundFault(RecoveryEpisode.Invalid);
        }
    }
}
