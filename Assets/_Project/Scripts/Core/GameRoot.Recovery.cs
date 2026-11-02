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
#if UNITY_EDITOR
        private bool editorRecoveryStartEntered, editorRecoveryClockInjected;
        private IPreciseTimeSource editorRecoveryUtc;
        private Func<double> editorRecoveryMonotonic;
        private string editorRecoverySaveFile;
        private byte[] editorRecoveryOneFloor, editorRecoveryTwoFloor;

        private static bool IsIsolatedSceneClockContext(string product, bool batch, string file)
        {
            const string prefix = "phase7a4-scene-";
            return batch && product?.StartsWith("Dungeon Lord B3B2 Smoke H1F4 ", StringComparison.Ordinal) == true &&
                file != null && file.Length == prefix.Length + 32 + 5 && file.StartsWith(prefix, StringComparison.Ordinal) &&
                file.EndsWith(".json", StringComparison.Ordinal) &&
                Guid.TryParseExact(file.Substring(prefix.Length, 32), "N", out _);
        }
        // Test providers, never a replacement owner. The normal bootstrap below
        // still constructs, binds and loads through exactly one TimeService.
        internal void InjectSceneRecoveryClockForTests(IPreciseTimeSource utc, Func<double> monotonic,
            byte[] oneFloorPolicy = null, byte[] twoFloorPolicy = null)
        {
            if (editorRecoveryStartEntered || editorRecoveryClockInjected || Instance != this || !isActiveAndEnabled ||
                SaveService != null || TimeService != null || recoveryRuntimeInputs != null || utc == null || monotonic == null)
                throw new ArgumentException(RecoveryEpisode.Invalid);
            string file;
            try { file = buildConfigJson == null ? null : JsonUtility.FromJson<BuildConfig>(buildConfigJson.text)?.save?.fileName; }
            catch { throw new ArgumentException(RecoveryEpisode.Invalid); }
            if (!IsIsolatedSceneClockContext(Application.productName, Application.isBatchMode, file))
                throw new ArgumentException(RecoveryEpisode.Invalid);
            if ((oneFloorPolicy == null) != (twoFloorPolicy == null)) throw new ArgumentException(RecoveryEpisode.Invalid);
            if (oneFloorPolicy != null) {
                if (LivingDungeonForegroundConfig.Parse(oneFloorPolicy).TwoFloor || !LivingDungeonForegroundConfig.Parse(twoFloorPolicy).TwoFloor)
                    throw new ArgumentException(RecoveryEpisode.ContextMismatch);
            }
            editorRecoveryUtc = utc; editorRecoveryMonotonic = monotonic;
            editorRecoveryOneFloor = oneFloorPolicy == null ? null : (byte[])oneFloorPolicy.Clone();
            editorRecoveryTwoFloor = twoFloorPolicy == null ? null : (byte[])twoFloorPolicy.Clone();
            editorRecoverySaveFile = file; editorRecoveryClockInjected = true;
        }
#endif

        private bool TryConfigureRecoveryRuntime(SpatialContentValidationWorkloadLimits structuralLimits)
        {
            try {
                if (recoveryRuntimeInputs != null || TimeService != null) throw new InvalidOperationException(RecoveryEpisode.Invalid);
                byte[] Resource(string name) {
#if UNITY_EDITOR
                    // Explicit synthetic policy bytes are authored before ownership,
                    // cloned once, consumed by this bundle and hashed by context-v3.
                    if (editorRecoveryClockInjected && editorRecoveryOneFloor != null) {
                        if (name == "living_dungeon_foreground") return (byte[])editorRecoveryOneFloor.Clone();
                        if (name == "living_dungeon_foreground_v2") return (byte[])editorRecoveryTwoFloor.Clone();
                    }
#endif
                    return Resources.Load<TextAsset>(name)?.bytes ?? throw new InvalidOperationException(RecoveryEpisode.ContextMismatch);
                }
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
#if UNITY_EDITOR
                if (editorRecoveryClockInjected) {
                    if (!IsIsolatedSceneClockContext(Application.productName, Application.isBatchMode, editorRecoverySaveFile) ||
                        System.IO.Path.GetFileName(SaveService.SavePath) != editorRecoverySaveFile)
                        throw new ArgumentException(RecoveryEpisode.Invalid);
                    // CreateNew's initial seconds and native S/Q's precise cut
                    // consume the same injected UTC source before any save IO.
                    SaveService.SetTimeSourceForTests(editorRecoveryUtc);
                    TimeService = new TimeService(Logger, inputs.Bootstrap.tickSeconds, inputs.Bootstrap.timeRules.detectClockSkewSeconds,
                        editorRecoveryUtc, editorRecoveryMonotonic);
                } else
#endif
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                var qualificationClock = LivingDungeonForegroundDevelopmentSmoke.ReopenClock(
                    Environment.GetCommandLineArgs(), Application.productName, SaveService.SavePath);
                qualificationClockControl = qualificationClock?.Target as PreboundForegroundQualificationClock;
                TimeService = qualificationClock == null ? new TimeService(Logger, inputs.Bootstrap.tickSeconds, inputs.Bootstrap.timeRules.detectClockSkewSeconds) :
                    new TimeService(Logger, inputs.Bootstrap.tickSeconds, inputs.Bootstrap.timeRules.detectClockSkewSeconds, new SystemTimeSource(), qualificationClock);
#else
                TimeService = new TimeService(Logger, inputs.Bootstrap.tickSeconds, inputs.Bootstrap.timeRules.detectClockSkewSeconds);
#endif
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                qualificationObservationOwner = TimeService;
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
