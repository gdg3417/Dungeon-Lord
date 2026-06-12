using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DungeonBuilder.M0
{
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        [Header("JSON TextAssets (drag your .json files here)")]
        public TextAsset contentBootstrapJson;
        public TextAsset buildConfigJson;
        public TextAsset schemaVersionsJson;
        public TextAsset contentManifestJson;
        public TextAsset devCommandsJson;
        public TextAsset stringTableJson;
        public TextAsset heatRuntimeJson;
        public TextAsset structureSimulationConfigJson;
        public TextAsset runSimulationConfigJson;
        public TextAsset lootConfigJson;

        [Header("UI")]
        public BootstrapOverlay overlay;

        public SimpleLogger Logger { get; private set; }
        public ContentService Content { get; private set; }
        public SaveService SaveService { get; private set; }
        public TimeService TimeService { get; private set; }
        public ITelemetryService Telemetry { get; private set; }
        public IKpiService Kpi { get; private set; }
        public SaveData Save { get; private set; }

        public string BannerMessage { get; private set; } = string.Empty;
        public string PendingStateLine { get; private set; } = "Pending: None";
        public string GateStatusLine { get; private set; } = "Gate: unknown";
        public string KpiLine { get; private set; } = "KPI: n/a";
        public string HeatLine { get; private set; } = "Heat: 0.00";
        public string TickLine { get; private set; } = "Tick: 0";
        public string ManaLine { get; private set; } = "Mana: 0.00";
        public string SaveLine { get; private set; } = "Save: n/a";
        public string PauseLine { get; private set; } = "Pause: Running";
        public string RunLine { get; private set; } = "ui.run.none";
        public string RunHistoryLine { get; private set; } = "ui.run.history_position_format";
        public string RunBreakdownLine { get; private set; } = string.Empty;
        public string RunFeedbackLine { get; private set; } = string.Empty;
        public string RunLootLine { get; private set; } = string.Empty;
        public string RunSurvivalLine { get; private set; } = string.Empty;
        public string RunExtractionLine { get; private set; } = string.Empty;
        public string RunHeatCoolingLine { get; private set; } = string.Empty;
        public string RunHeatDeltaLine { get; private set; } = string.Empty;
        public string RunHeatApplicationLine { get; private set; } = string.Empty;
        public string CurrentHeatTierLine { get; private set; } = string.Empty;
        public string RunAdventurerAttractionLine { get; private set; } = string.Empty;
        public string RunAdventurerInterestForecastLine { get; private set; } = string.Empty;
        public string RunAdventurerDemandBudgetLine { get; private set; } = string.Empty;
        public string OfflineSummaryLine { get; private set; } = "ui.dev.offline_summary_format";
        public string ResearchPendingLine { get; private set; } = "ui.dev.research_pending_format";
        public string ResearchPendingValidationLine { get; private set; } = "ui.dev.research_pending_validation_format";
        public string ResearchProgressLine { get; private set; } = "ui.dev.research_progress_format";
        public string ResearchProgressStateLine { get; private set; } = "ui.dev.research_progress_state_format";
        public string ResearchCompletionEligibilityLine { get; private set; } = "ui.dev.research_completion_eligibility_format";
        public string ResearchCompletionPendingApplyLine { get; private set; } = "ui.dev.research_completion_pending_apply_format";
        public string ResearchCompletionClaimReadinessLine { get; private set; } = "ui.dev.research_completion_claim_readiness_format";
        public string CompletedResearchStateLine { get; private set; } = "ui.dev.completed_research_state_format";
        public string ResearchCompletionClaimApplyLine { get; private set; } = "ui.dev.research_completion_claim_apply_format";
        public string ResearchStatusPresentationLine { get; private set; } = "ui.dev.research_status_presentation_format";
        public string ResearchStatusSafetyLine { get; private set; } = "ui.dev.research_status_safety_format";
        public string ResearchVerificationBoundaryLine { get; private set; } = "ui.dev.research_verification_boundary_format";
        public string ResearchVerificationSafetyLine { get; private set; } = "ui.dev.research_verification_safety_format";

        private AppStateMachine _sm;
#if UNITY_EDITOR
        private string _editorFallbackWarningLine = string.Empty;
#endif

        private readonly IRestrictedActionGate _restrictedActionGate = new RestrictedActionGateService();
        private readonly IHeatSystem _heatSystem = new HeatSystem();
        private readonly PlacementService _placementService = new PlacementService();
        private StructureSimulationPass _structureSimulationPass;
        private RunSimulationService _runSimulationService;
        private OfflineSummaryResolver _offlineSummaryResolver;
        private int _selectedFloorIndex;
        private int _selectedSlotIndex;
        private int _selectedRunHistoryIndex = -1;
        private long _activeSessionTickCount;

        public bool DevPanelEnabled { get; private set; }
        public bool IsOnline { get; private set; } = true;
        public bool VerificationPending { get; private set; }
        public double CurrentHeat { get; private set; }
        public double HeatDecayPerTick { get; private set; } = 0.1d;

        public string BuildLine { get; private set; } = "Build: unknown";
        public string StateLine => "State: " + (_sm != null ? _sm.CurrentStateName : "None");
        public int SelectedFloorIndex => _selectedFloorIndex;
        public int SelectedSlotIndex => _selectedSlotIndex;

        public MvpPlayerLoopSummary ResolveMvpPlayerLoopSummary()
        {
            RunSimulationConfig config = _runSimulationService != null ? _runSimulationService.Config : null;
            return MvpPlayerLoopSummaryPresenter.Resolve(
                Save,
                config,
                GetResearchCompletionEligibilityScaffoldConfig(),
                GetResearchVerificationScaffoldConfig());
        }

        public GuidedMvpActionPathSummary ResolveGuidedMvpActionPath(MvpPlayerLoopSummary summary = null)
        {
            return GuidedMvpActionPathPresenter.Resolve(Save, summary ?? ResolveMvpPlayerLoopSummary());
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Logger = new SimpleLogger(includeTimestamps: true);
            Content = new ContentService();
            _sm = new AppStateMachine();

            if (overlay != null)
            {
                overlay.Bind(this);
            }
        }

        private void Start()
        {
            _sm.SetState(new BootState(this));
        }

        private void Update()
        {
            if (TimeService != null)
            {
                TimeService.Update(Time.deltaTime);
            }

            if (_sm != null)
            {
                _sm.Tick(Time.deltaTime);
            }
        }

        private void OnApplicationPause(bool pause)
        {
            ApplyPauseState(pause);
        }

        public void ApplyPauseState(bool pause)
        {
            if (pause)
            {
                PauseLine = "Pause: Paused";
                if (TimeService != null)
                {
                    TimeService.OnPause();
                }

                if (SaveService != null && Save != null)
                {
                    SaveService.Save(Save, SaveReason.AppPause);
                    SaveLine = "Save: AppPause";
                }
            }
            else
            {
                PauseLine = "Pause: Running";
                if (TimeService != null)
                {
                    string banner = TimeService.OnResume();
                    if (!string.IsNullOrEmpty(banner))
                    {
                        SetBanner(banner);
                    }
                }
            }
        }


        private void EnsureContentAssetsAssigned()
        {
#if UNITY_EDITOR
            System.Collections.Generic.List<string> assignedFields = new System.Collections.Generic.List<string>();

            contentBootstrapJson = EnsureEditorFallbackAsset(contentBootstrapJson, "contentBootstrapJson", "Assets/_Project/Data/Bootstrap/content_bootstrap.json", assignedFields);
            buildConfigJson = EnsureEditorFallbackAsset(buildConfigJson, "buildConfigJson", "Assets/_Project/Data/Bootstrap/build_config.json", assignedFields);
            schemaVersionsJson = EnsureEditorFallbackAsset(schemaVersionsJson, "schemaVersionsJson", "Assets/_Project/Data/Bootstrap/schema_versions.json", assignedFields);
            contentManifestJson = EnsureEditorFallbackAsset(contentManifestJson, "contentManifestJson", "Assets/_Project/Data/Bootstrap/content_manifest.json", assignedFields);
            devCommandsJson = EnsureEditorFallbackAsset(devCommandsJson, "devCommandsJson", "Assets/_Project/Data/Bootstrap/dev_commands.json", assignedFields);
            stringTableJson = EnsureEditorFallbackAsset(stringTableJson, "stringTableJson", "Assets/_Project/Data/Bootstrap/string_table_en.json", assignedFields);
            heatRuntimeJson = EnsureEditorFallbackAsset(heatRuntimeJson, "heatRuntimeJson", "Assets/_Project/Data/Bootstrap/heat_runtime.json", assignedFields);
            structureSimulationConfigJson = EnsureEditorFallbackAsset(structureSimulationConfigJson, "structureSimulationConfigJson", "Assets/_Project/Data/Bootstrap/structure_simulation_config.json", assignedFields);
            runSimulationConfigJson = EnsureEditorFallbackAsset(runSimulationConfigJson, "runSimulationConfigJson", "Assets/_Project/Data/Bootstrap/run_simulation_config.json", assignedFields);
            lootConfigJson = EnsureEditorFallbackAsset(lootConfigJson, "lootConfigJson", "Assets/_Project/Data/Bootstrap/loot_config.json", assignedFields);

            if (assignedFields.Count > 0)
            {
                _editorFallbackWarningLine = "WARNING: Editor fallback JSON in use (fields: " + string.Join(", ", assignedFields) + ")";
                Logger?.Warn(_editorFallbackWarningLine);
            }
            else
            {
                _editorFallbackWarningLine = string.Empty;
            }
#endif
        }

#if UNITY_EDITOR
        private TextAsset EnsureEditorFallbackAsset(
            TextAsset current,
            string fieldName,
            string path,
            System.Collections.Generic.List<string> assignedFields)
        {
            if (current != null)
            {
                return current;
            }

            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset == null)
            {
                Logger?.Warn($"Editor fallback JSON asset not found for {fieldName} at path: {path}");
                return null;
            }

            assignedFields.Add(fieldName);
            return asset;
        }
#endif

        public void InitializeServicesAndData()
        {
            EnsureContentAssetsAssigned();

            Content.LoadAll(
                contentBootstrapJson,
                buildConfigJson,
                schemaVersionsJson,
                contentManifestJson,
                devCommandsJson,
                stringTableJson,
                heatRuntimeJson,
                Logger,
                out string contentBanner
            );

            if (!string.IsNullOrEmpty(contentBanner))
            {
                SetBanner(contentBanner);
            }

            string contentVersion = Content.Bootstrap != null ? Content.Bootstrap.contentVersion : "0.0.0";

            string env = Content.BuildConfig != null ? Content.BuildConfig.environment : "unknown";
            BuildLine = $"Build: {Application.version}  Env: {env}";

            DevPanelEnabled = Content.Bootstrap != null &&
                              Content.Bootstrap.featureFlags != null &&
                              Content.Bootstrap.featureFlags.enableDevPanel;

            SaveService = new SaveService(Logger, Content.BuildConfig != null ? Content.BuildConfig.save : null);
            Save = SaveService.LoadOrCreate(contentVersion, out string saveBanner);
            _offlineSummaryResolver = new OfflineSummaryResolver(new SystemTimeSource());
            CaptureOfflineSummaryDiagnostics();
            RefreshOfflineSummaryLines();

            if (!string.IsNullOrEmpty(saveBanner))
            {
                SetBanner(saveBanner);
            }

            int tickSeconds = Content.Bootstrap != null ? Content.Bootstrap.tickSeconds : 10;
            int skewSeconds = (Content.Bootstrap != null && Content.Bootstrap.timeRules != null)
                ? Content.Bootstrap.timeRules.detectClockSkewSeconds
                : 300;

            TimeService = new TimeService(Logger, tickSeconds, skewSeconds);
            TimeService.AttachSave(Save);
            TimeService.OnTick += HandleSimulationTick;
            TimeService.OnTick += HandleTickTelemetry;

            Telemetry = new TelemetryService(Logger);
            Kpi = new KpiService();

            CurrentHeat = 0d;
            HeatDecayPerTick = Content != null && Content.HeatRuntime != null
                ? System.Math.Max(0d, Content.HeatRuntime.decayPerTick)
                : 0.1d;

            Save.lastKnownAppState = "Boot";
            SaveMigration.MigrateToLatest(new SaveRoot { primary = Save });
            CurrentHeat = Save.structureRuntime != null ? Save.structureRuntime.Heat : 0d;
            _selectedFloorIndex = 0;
            _selectedSlotIndex = 0;
            InitializeStructureSimulationPass();
            InitializeRunSimulationService();
            SaveService.Save(Save, SaveReason.Boot);
            SaveLine = "Save: Boot";

            Logger.Info("M0 init complete.");
            RefreshDashboardState();
            RefreshStructureRuntimeLines();
            RefreshRunLine();
        }

        private void InitializeStructureSimulationPass()
        {
            _structureSimulationPass = null;
            string json = structureSimulationConfigJson != null ? structureSimulationConfigJson.text : string.Empty;
            if (!TryCreateStructureSimulationPass(_heatSystem, json, out _structureSimulationPass))
            {
                SetBanner(Content.GetString("ui.banner.structure_sim_config_missing", "ui.banner.structure_sim_config_missing"));
                return;
            }

            if (Save?.structureRuntime != null)
            {
                _structureSimulationPass.NormalizeRuntimeFlags(Save.structureRuntime);
            }
        }

        private void InitializeRunSimulationService()
        {
            _runSimulationService = null;
            string json = runSimulationConfigJson != null ? runSimulationConfigJson.text : string.Empty;
            string lootJson = lootConfigJson != null ? lootConfigJson.text : string.Empty;
            if (!TryCreateRunSimulationService(json, lootJson, out _runSimulationService))
            {
                SetBanner(Content.GetString("ui.banner.run_sim_failed", "ui.banner.run_sim_failed"));
            }
        }

        internal static bool TryCreateRunSimulationService(string configJson, string lootConfigJson, out RunSimulationService service)
        {
            service = null;

            try
            {
                RunSimulationConfig config = JsonUtility.FromJson<RunSimulationConfig>(configJson);
                if (!IsValidRunSimulationConfig(config))
                {
                    return false;
                }

                LootConfig lootConfig = TryParseLootConfig(lootConfigJson);
                service = new RunSimulationService(config, lootConfig);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static LootConfig TryParseLootConfig(string lootConfigJson)
        {
            if (string.IsNullOrWhiteSpace(lootConfigJson))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<LootConfig>(lootConfigJson);
            }
            catch
            {
                return null;
            }
        }

        internal static bool IsValidRunSimulationConfig(RunSimulationConfig config)
        {
            if (config == null)
            {
                return false;
            }

            if (config.BaseSuccessChance < 0d || config.BaseSuccessChance > 1d)
            {
                return false;
            }

            if (config.SuccessThreshold < 0d || config.SuccessThreshold > 1d)
            {
                return false;
            }

            if (config.HeatPenaltyPerPoint < 0d ||
                config.ManaReserveBonusPerPoint < 0d ||
                config.CrisisFailurePenalty < 0d ||
                config.BaseScoreOnSuccess < 0 ||
                config.ScorePerManaPoint < 0)
            {
                return false;
            }

            if (config.MaxRunHistoryEntries < 1 || config.MaxRunHistoryEntries > 100)
            {
                return false;
            }

            if (config.HighHeatFeedbackThreshold < 0d ||
                config.LowManaFeedbackThreshold < 0d ||
                config.StrongManaReserveFeedbackThreshold < 0d)
            {
                return false;
            }

            if (config.LowManaFeedbackThreshold >= config.StrongManaReserveFeedbackThreshold)
            {
                return false;
            }

            if (config.MinPartySize < 1 ||
                config.MaxPartySize < config.MinPartySize ||
                config.MaxAllowedPartySize < 1 ||
                config.MaxPartySize > config.MaxAllowedPartySize)
            {
                return false;
            }

            if (config.SuccessSurvivorRatio < 0d || config.SuccessSurvivorRatio > 1d ||
                config.FailureSurvivorRatio < 0d || config.FailureSurvivorRatio > 1d)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.LootExtractionRuleSourceId) ||
                string.IsNullOrWhiteSpace(config.LootExtractionRoundingPolicyId) ||
                string.IsNullOrWhiteSpace(config.LootHeatCoolingRuleSourceId) ||
                string.IsNullOrWhiteSpace(config.AdventurerAttractionRuleSourceId) ||
                config.LootHeatCoolingPerTradeableWorldValue < 0d ||
                config.MaxLootHeatCoolingPerRun < 0d ||
                config.AdventurerAttractionPerExtractedWorldValue < 0d ||
                string.IsNullOrWhiteSpace(config.AdventurerInterestForecastRuleSourceId) ||
                config.AdventurerInterestLowThreshold < 0d ||
                config.AdventurerInterestMediumThreshold < 0d ||
                config.AdventurerInterestHighThreshold < 0d ||
                config.AdventurerInterestScorePerAttractionSignal < 0d ||
                config.AdventurerInterestLowThreshold > config.AdventurerInterestMediumThreshold ||
                config.AdventurerInterestMediumThreshold > config.AdventurerInterestHighThreshold ||
                double.IsNaN(config.LootHeatCoolingPerTradeableWorldValue) ||
                double.IsInfinity(config.LootHeatCoolingPerTradeableWorldValue) ||
                double.IsNaN(config.MaxLootHeatCoolingPerRun) ||
                double.IsInfinity(config.MaxLootHeatCoolingPerRun) ||
                double.IsNaN(config.AdventurerAttractionPerExtractedWorldValue) ||
                double.IsInfinity(config.AdventurerAttractionPerExtractedWorldValue) ||
                double.IsNaN(config.AdventurerInterestLowThreshold) ||
                double.IsInfinity(config.AdventurerInterestLowThreshold) ||
                double.IsNaN(config.AdventurerInterestMediumThreshold) ||
                double.IsInfinity(config.AdventurerInterestMediumThreshold) ||
                double.IsNaN(config.AdventurerInterestHighThreshold) ||
                double.IsInfinity(config.AdventurerInterestHighThreshold) ||
                double.IsNaN(config.AdventurerInterestScorePerAttractionSignal) ||
                double.IsInfinity(config.AdventurerInterestScorePerAttractionSignal) ||
                string.IsNullOrWhiteSpace(config.AdventurerDemandBudgetRuleSourceId) ||
                config.AdventurerDemandBudgetLowThreshold < 0d ||
                config.AdventurerDemandBudgetMediumThreshold < 0d ||
                config.AdventurerDemandBudgetHighThreshold < 0d ||
                config.AdventurerDemandBudgetScorePerForecastScore < 0d ||
                config.AdventurerDemandBudgetLowThreshold > config.AdventurerDemandBudgetMediumThreshold ||
                config.AdventurerDemandBudgetMediumThreshold > config.AdventurerDemandBudgetHighThreshold ||
                double.IsNaN(config.AdventurerDemandBudgetLowThreshold) ||
                double.IsInfinity(config.AdventurerDemandBudgetLowThreshold) ||
                double.IsNaN(config.AdventurerDemandBudgetMediumThreshold) ||
                double.IsInfinity(config.AdventurerDemandBudgetMediumThreshold) ||
                double.IsNaN(config.AdventurerDemandBudgetHighThreshold) ||
                double.IsInfinity(config.AdventurerDemandBudgetHighThreshold) ||
                double.IsNaN(config.AdventurerDemandBudgetScorePerForecastScore) ||
                double.IsInfinity(config.AdventurerDemandBudgetScorePerForecastScore) ||
                string.IsNullOrWhiteSpace(config.RunHeatDeltaRuleSourceId) ||
                config.RunHeatNormalDeathDelta < 0d ||
                config.RunHeatEliteDeathDelta < config.RunHeatNormalDeathDelta ||
                config.RunHeatMultipleDeathBonusDelta < 0d ||
                config.RunHeatSurvivorCoolingPerSurvivor < 0d ||
                config.RunHeatLootCoolingPerExtractedValue < 0d ||
                config.RunHeatDeltaMinimum > config.RunHeatDeltaMaximum ||
                double.IsNaN(config.RunHeatNormalDeathDelta) ||
                double.IsInfinity(config.RunHeatNormalDeathDelta) ||
                double.IsNaN(config.RunHeatEliteDeathDelta) ||
                double.IsInfinity(config.RunHeatEliteDeathDelta) ||
                double.IsNaN(config.RunHeatMultipleDeathBonusDelta) ||
                double.IsInfinity(config.RunHeatMultipleDeathBonusDelta) ||
                double.IsNaN(config.RunHeatSurvivorCoolingPerSurvivor) ||
                double.IsInfinity(config.RunHeatSurvivorCoolingPerSurvivor) ||
                double.IsNaN(config.RunHeatLootCoolingPerExtractedValue) ||
                double.IsInfinity(config.RunHeatLootCoolingPerExtractedValue) ||
                double.IsNaN(config.RunHeatDeltaMinimum) ||
                double.IsInfinity(config.RunHeatDeltaMinimum) ||
                double.IsNaN(config.RunHeatDeltaMaximum) ||
                double.IsInfinity(config.RunHeatDeltaMaximum) ||
                double.IsNaN(config.HeatPeaceMinimum) ||
                double.IsInfinity(config.HeatPeaceMinimum) ||
                double.IsNaN(config.HeatPeaceMaximum) ||
                double.IsInfinity(config.HeatPeaceMaximum) ||
                double.IsNaN(config.HeatNoticeMinimum) ||
                double.IsInfinity(config.HeatNoticeMinimum) ||
                double.IsNaN(config.HeatNoticeMaximum) ||
                double.IsInfinity(config.HeatNoticeMaximum) ||
                double.IsNaN(config.HeatConcernMinimum) ||
                double.IsInfinity(config.HeatConcernMinimum) ||
                double.IsNaN(config.HeatConcernMaximum) ||
                double.IsInfinity(config.HeatConcernMaximum) ||
                string.IsNullOrWhiteSpace(config.RunHeatApplicationRuleSourceId) ||
                config.HeatPeaceMinimum > config.HeatPeaceMaximum ||
                config.HeatPeaceMaximum >= config.HeatNoticeMinimum ||
                config.HeatNoticeMinimum > config.HeatNoticeMaximum ||
                config.HeatNoticeMaximum >= config.HeatConcernMinimum ||
                config.HeatConcernMinimum > config.HeatConcernMaximum ||
                string.IsNullOrWhiteSpace(config.AdventurerPartyCompositionRuleSourceId) ||
                config.AdventurerPartyCompositionMinSize < 1 ||
                config.AdventurerPartyCompositionMaxSize < config.AdventurerPartyCompositionMinSize ||
                config.AdventurerPartyCompositionMaxAllowedSize < 1 ||
                config.AdventurerPartyCompositionMaxSize > config.AdventurerPartyCompositionMaxAllowedSize ||
                !HasConfiguredMvpAdventurerClasses(config.AdventurerPartyCompositionClassIds))
            {
                return false;
            }

            if (!HasValidMvpPlacementEffectsConfig(config) || !HasValidMvpCompositionOutcomeTuningConfig(config.MvpCompositionOutcomeTuning))
            {
                return false;
            }

            return true;
        }

        private static bool HasValidMvpCompositionOutcomeTuningConfig(MvpCompositionOutcomeTuningConfig tuning)
        {
            if (tuning == null)
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(tuning.RuleSourceId) &&
                   IsFiniteNonNegative(tuning.SuccessChancePerPathCapacity) &&
                   IsFiniteNonNegative(tuning.SuccessChancePenaltyPerDanger) &&
                   IsFiniteNonNegative(tuning.ManaReserveCostPerManaPressure) &&
                   IsFiniteNonNegative(tuning.SurvivorRatioBonusPerPathCapacity) &&
                   IsFiniteNonNegative(tuning.SurvivorRatioPenaltyPerDanger) &&
                   IsFiniteNonNegative(tuning.GeneratedLootMultiplierPerLootBonus) &&
                   IsFiniteNonNegative(tuning.ExtractedLootMultiplierPerLootBonus) &&
                   IsFiniteNonNegative(tuning.HeatDeltaPerHeatPressure) &&
                   IsFiniteNonNegative(tuning.AttractionSignalPerAttraction);
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return value >= 0d && !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool HasValidMvpPlacementEffectsConfig(RunSimulationConfig config)
        {
            if (config == null || config.MvpPlacementEffects == null || config.MvpPlacementEffects.Length == 0)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(config.MvpPlacementEffectsRuleSourceId))
            {
                return false;
            }

            for (int i = 0; i < config.MvpPlacementEffects.Length; i++)
            {
                MvpPlacementEffectConfig effect = config.MvpPlacementEffects[i];
                if (effect == null ||
                    !MvpDungeonPlacementIds.IsAllowedCategory(effect.CategoryId) ||
                    !MvpDungeonPlacementIds.IsAllowedOption(effect.OptionId) ||
                    !MvpDungeonPlacementIds.TryGetCategoryForOption(effect.OptionId, out string optionCategoryId) ||
                    !string.Equals(optionCategoryId, effect.CategoryId, StringComparison.Ordinal) ||
                    effect.PathCapacity < 0 ||
                    effect.Danger < 0 ||
                    effect.ManaPressure < 0 ||
                    effect.HeatPressure < 0 ||
                    effect.LootBonus < 0 ||
                    effect.Attraction < 0 ||
                    string.IsNullOrWhiteSpace(effect.ExplanationKey))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasConfiguredMvpAdventurerClasses(string[] classIds)
        {
            if (classIds == null || classIds.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < classIds.Length; i++)
            {
                if (AdventurerPartyCompositionResolver.IsMvpClassId(classIds[i]))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool TryCreateStructureSimulationPass(
            IHeatSystem heatSystem,
            string configJson,
            out StructureSimulationPass pass)
        {
            pass = null;

            try
            {
                StructureSimulationConfig config = JsonUtility.FromJson<StructureSimulationConfig>(configJson);
                if (!IsValidStructureSimulationConfig(config))
                {
                    return false;
                }

                pass = new StructureSimulationPass(heatSystem, config);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsValidStructureSimulationConfig(StructureSimulationConfig config)
        {
            if (config == null || config.Structures == null || config.Structures.Length == 0)
            {
                return false;
            }

            if (config.HeatCrisisEnterThreshold <= config.HeatCrisisRecoveryThreshold)
            {
                return false;
            }
            if (config.CrisisEnterConsecutiveTicks < 1 ||
                config.CrisisRecoveryConsecutiveTicks < 1 ||
                config.CrisisManaDrainPerTick < 0d)
            {
                return false;
            }

            var required = new HashSet<string>(StringComparer.Ordinal)
            {
                StructureSimulationPass.ManaGeneratorBasicId,
                StructureSimulationPass.HeatScrubberBasicId,
                StructureSimulationPass.RiskLabBasicId
            };

            for (int i = 0; i < config.Structures.Length; i++)
            {
                StructureTuningEntry entry = config.Structures[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.StructureId))
                {
                    return false;
                }

                required.Remove(entry.StructureId);
            }

            return required.Count == 0;
        }

        public void GoHomeStub()
        {
            _sm.SetState(new HomeStubState(this));
        }

        public void SetBanner(string message)
        {
            string baseMessage = message ?? string.Empty;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(_editorFallbackWarningLine))
            {
                if (string.IsNullOrEmpty(baseMessage))
                {
                    BannerMessage = _editorFallbackWarningLine;
                    return;
                }

                if (baseMessage.StartsWith(_editorFallbackWarningLine + "\n", System.StringComparison.Ordinal) ||
                    string.Equals(baseMessage, _editorFallbackWarningLine, System.StringComparison.Ordinal))
                {
                    BannerMessage = baseMessage;
                    return;
                }

                BannerMessage = _editorFallbackWarningLine + "\n" + baseMessage;
                return;
            }
#endif
            BannerMessage = baseMessage;
        }

        public void SetOnline(bool isOnline)
        {
            IsOnline = isOnline;
            RefreshDashboardState();
        }

        public void SetVerificationPending(bool pending)
        {
            VerificationPending = pending;
            Telemetry?.Track("verification_pending_state_changed", $"{{\"pending\":{pending.ToString().ToLowerInvariant()}}}");
            RefreshDashboardState();
        }

        public void RefreshDashboardState()
        {
            PendingStateLine = VerificationPending ? "Pending: Verification" : "Pending: None";

            GateEvaluationResult result = _restrictedActionGate.Evaluate(
                new GateEvaluationInput(RestrictedActionType.ResearchComplete, IsOnline, VerificationPending)
            );

            string gateMessage = Content != null
                ? Content.GetString(result.MessageKey, result.MessageKey)
                : result.MessageKey;

            GateStatusLine = result.Allowed ? $"Gate: OK ({gateMessage})" : $"Gate: Blocked ({gateMessage})";
            Kpi?.RecordGateEvaluation(result.Allowed);

            KpiSnapshot snap = Kpi != null ? Kpi.Snapshot() : new KpiSnapshot(0, 0, 0);
            KpiLine = $"KPI: avgMana/tick={snap.AverageManaPerTick:0.00}, blockedGates={snap.BlockedGateCount}/{snap.TotalGateEvaluations}";
        }

        public void TrackResearchOutcome(string outcome)
        {
            string safeOutcome = string.IsNullOrEmpty(outcome) ? "unknown" : outcome;
            Telemetry?.Track("research_outcome", $"{{\"outcome\":\"{safeOutcome}\"}}");
        }

        public void TrackManaGenerated(double generatedMana)
        {
            Kpi?.RecordManaTick(generatedMana);
            Telemetry?.Track("mana_generated", $"{{\"amount\":{generatedMana:0.###}}}");
            KpiSnapshot snap = Kpi != null ? Kpi.Snapshot() : new KpiSnapshot(0, 0, 0);
            KpiLine = $"KPI: avgMana/tick={snap.AverageManaPerTick:0.00}, blockedGates={snap.BlockedGateCount}/{snap.TotalGateEvaluations}";
        }

        public void ApplyHeatDelta(double delta)
        {
            HeatResult result = _heatSystem.ApplyEvent(new HeatEventInput(
                Save != null ? Save.totalTicks : 0,
                CurrentHeat,
                delta
            ));

            CurrentHeat = result.NewHeat;
            HeatLine = $"Heat: {CurrentHeat:0.00}";
            Telemetry?.Track("heat_event_applied", $"{{\"delta\":{delta:0.###},\"heat\":{CurrentHeat:0.###}}}");
            if (Save != null && Save.structureRuntime != null)
            {
                Save.structureRuntime.Heat = CurrentHeat;
            }

            RefreshCurrentHeatTierLine();
        }

        public void SelectNextSlot()
        {
            if (Save?.dungeonLayout == null || Save.dungeonLayout.SlotsPerFloor <= 0)
            {
                return;
            }

            int slotsPerFloor = Save.dungeonLayout.SlotsPerFloor;
            int floorCount = Save.dungeonLayout.FloorCount;
            _selectedSlotIndex++;
            if (_selectedSlotIndex >= slotsPerFloor)
            {
                _selectedSlotIndex = 0;
                _selectedFloorIndex = (_selectedFloorIndex + 1) % Mathf.Max(1, floorCount);
            }
        }

        public bool TryPlaceSelectedStructure(string structureId, out string bannerKey)
        {
            return TryPlaceSelectedStructure(structureId, allowReplace: false, out bannerKey);
        }

        public bool TryMvpPlaceOrModifySelectedStructure(string structureId, out string bannerKey)
        {
            return TryPlaceSelectedStructure(structureId, allowReplace: true, out bannerKey);
        }

        public bool TryMvpPlaceOrModifySelectedPlacement(string categoryId, string optionId, out MvpDungeonPlacementEntry priorEntry, out MvpDungeonPlacementEntry newEntry, out string bannerKey)
        {
            priorEntry = null;
            newEntry = null;
            bannerKey = "ui.banner.place_success";
            if (Save == null || !MvpDungeonPlacementIds.IsAllowedCategory(categoryId) || !MvpDungeonPlacementIds.IsAllowedOption(optionId))
            {
                bannerKey = "ui.banner.place_failed";
                return false;
            }

            if (!MvpDungeonPlacementIds.TryGetCategoryForOption(optionId, out string optionCategoryId) ||
                !string.Equals(optionCategoryId, categoryId, StringComparison.Ordinal))
            {
                bannerKey = "ui.banner.place_failed";
                return false;
            }

            if (Save.structureRuntime != null && Save.structureRuntime.PlacementLocked)
            {
                bannerKey = "ui.banner.place_blocked_heat_crisis";
                return false;
            }

            if (Save.mvpDungeonPlacements == null)
            {
                Save.mvpDungeonPlacements = new MvpDungeonPlacementState();
            }

            if (Save.mvpDungeonPlacements.Entries == null)
            {
                Save.mvpDungeonPlacements.Entries = new List<MvpDungeonPlacementEntry>();
            }

            int existingIndex = Save.mvpDungeonPlacements.Entries.FindIndex(entry =>
                entry != null && string.Equals(entry.CategoryId, categoryId, StringComparison.Ordinal));
            if (existingIndex >= 0)
            {
                MvpDungeonPlacementEntry existing = Save.mvpDungeonPlacements.Entries[existingIndex];
                priorEntry = new MvpDungeonPlacementEntry(existing.CategoryId, existing.OptionId, existing.Revision);
                existing.OptionId = optionId;
                existing.Revision = Math.Max(1, Save.mvpDungeonPlacements.NextRevision);
                newEntry = existing;
            }
            else
            {
                newEntry = new MvpDungeonPlacementEntry(categoryId, optionId, Math.Max(1, Save.mvpDungeonPlacements.NextRevision));
                Save.mvpDungeonPlacements.Entries.Add(newEntry);
            }

            Save.mvpDungeonPlacements.NextRevision = Math.Max(newEntry.Revision + 1, Save.mvpDungeonPlacements.NextRevision + 1);
            SaveService?.Save(Save, SaveReason.ManualDev);
            return true;
        }

        private bool TryPlaceSelectedStructure(string structureId, bool allowReplace, out string bannerKey)
        {
            bannerKey = "ui.banner.place_success";
            if (Save?.dungeonLayout == null)
            {
                bannerKey = "ui.banner.place_failed";
                return false;
            }
            if (Save.structureRuntime != null && Save.structureRuntime.PlacementLocked)
            {
                bannerKey = "ui.banner.place_blocked_heat_crisis";
                return false;
            }

            try
            {
                _placementService.PlaceStructure(Save.dungeonLayout, _selectedFloorIndex, _selectedSlotIndex, structureId, allowReplace);
                SaveService?.Save(Save, SaveReason.ManualDev);
                RefreshStructureRuntimeLines();
                return true;
            }
            catch
            {
                bannerKey = "ui.banner.place_failed";
                return false;
            }
        }

        public bool SimulateStructureTick()
        {
            if (_structureSimulationPass == null || Save?.dungeonLayout == null || Save.structureRuntime == null)
            {
                return false;
            }

            long tick = Save.totalTicks + 1;
            _structureSimulationPass.SimulateTick(Save.dungeonLayout, Save.structureRuntime, tick);
            Save.totalTicks = tick;
            CurrentHeat = Save.structureRuntime.Heat;
            RefreshStructureRuntimeLines();
            SaveService.Save(Save, SaveReason.ManualDev);
            return true;
        }

        public bool ResetCleanMvpValidationSession()
        {
            if (!DevPanelEnabled || Save == null)
            {
                return false;
            }

            ApplyCleanMvpValidationBaseline(Save);
            _selectedFloorIndex = 0;
            _selectedSlotIndex = 0;
            _selectedRunHistoryIndex = -1;
            _activeSessionTickCount = 0;

            if (_structureSimulationPass != null && Save.structureRuntime != null)
            {
                _structureSimulationPass.NormalizeRuntimeFlags(Save.structureRuntime);
            }

            CurrentHeat = Save.structureRuntime != null ? Save.structureRuntime.Heat : 0d;
            Save.lastSavedUtcUnix = TimeUtil.UtcNowUnixSeconds();
            CaptureOfflineSummaryDiagnostics();
            RefreshDashboardState();
            RefreshOfflineSummaryLines();
            RefreshStructureRuntimeLines();
            RefreshRunLine();
            SaveService?.Save(Save, SaveReason.ManualDev);
            SaveLine = "Save: ManualDev";
            return true;
        }

        internal static void ApplyCleanMvpValidationBaseline(SaveData save)
        {
            if (save == null)
            {
                return;
            }

            save.totalTicks = 0;
            save.lastPausedUtcUnix = 0;
            save.lastResumedUtcUnix = 0;
            save.dungeonLayout = DungeonLayoutState.CreateEmpty(SaveMigration.DefaultFloorCount, SaveMigration.DefaultSlotsPerFloor);
            save.mvpDungeonPlacements = new MvpDungeonPlacementState();
            save.structureRuntime = new StructureRuntimeState();
            save.runHistory = new RunHistoryState();
            save.researchPending = null;
            save.researchProgress = null;
            save.lastOfflineSummary = null;
        }

        public bool SimulateMvpActiveLoopOnce(out bool didApplyStructureTick)
        {
            return SimulateMvpActiveLoopOnce(out didApplyStructureTick, RunPostureResolver.BalancedId);
        }

        public bool SimulateMvpActiveLoopOnce(out bool didApplyStructureTick, string postureId)
        {
            didApplyStructureTick = false;
            if (_runSimulationService == null || Save?.structureRuntime == null || Save.runHistory == null)
            {
                return false;
            }

            if (_structureSimulationPass != null && Save.dungeonLayout != null)
            {
                didApplyStructureTick = SimulateStructureTick();
            }

            return SimulateRunOnce(postureId);
        }

        public bool SimulateRunOnce()
        {
            return SimulateRunOnce(RunPostureResolver.BalancedId);
        }

        public bool SimulateRunOnce(string postureId)
        {
            if (_runSimulationService == null || Save?.structureRuntime == null || Save.runHistory == null)
            {
                return false;
            }

            long tickStarted = Save.totalTicks;
            int sequence = Math.Max(1, Save.runHistory.NextRunSequence);
            MvpPlacementEffectsSummary placementEffects = MvpPlacementEffectsResolver.Resolve(Save.mvpDungeonPlacements, _runSimulationService.Config);
            RunOutcomeRecord outcome = _runSimulationService.SimulateOnce(Save.structureRuntime, tickStarted, sequence, postureId, placementEffects);
            RunSimulationConfig config = _runSimulationService.Config;
            CurrentHeat = Save.structureRuntime.Heat;
            RefreshStructureRuntimeLines();
            int deterministicSeed = outcome.LootExtractionSummary != null
                ? outcome.LootExtractionSummary.DeterministicSeed
                : sequence;
            RunLootHeatCoolingSummary coolingSummary = LootHeatCoolingResolver.Resolve(config, outcome.LootExtractionSummary, CurrentHeat, deterministicSeed);
            outcome.LootHeatCoolingSummary = coolingSummary;
            if (coolingSummary.RuleResolved && coolingSummary.AppliedHeatDelta != 0d)
            {
                ApplyHeatDelta(coolingSummary.AppliedHeatDelta);
                coolingSummary.HeatAfterCooling = CurrentHeat;
                coolingSummary.AppliedHeatDelta = CurrentHeat - coolingSummary.HeatBeforeCooling;
            }

            Save.runHistory.AppendOutcome(outcome, config.MaxRunHistoryEntries);
            Save.runHistory.NextRunSequence = sequence + 1;
            SelectLatestRunOutcome();
            RefreshRunLine();
            SaveService.Save(Save, SaveReason.ManualDev);
            return true;
        }

        public bool SelectPreviousRunOutcome()
        {
            if (!TryGetRunHistoryCount(out int historyCount))
            {
                return false;
            }

            int previous = Math.Max(0, _selectedRunHistoryIndex - 1);
            if (previous == _selectedRunHistoryIndex)
            {
                return false;
            }

            _selectedRunHistoryIndex = previous;
            return true;
        }

        public bool SelectNextRunOutcome()
        {
            if (!TryGetRunHistoryCount(out int historyCount))
            {
                return false;
            }

            int next = Math.Min(historyCount - 1, _selectedRunHistoryIndex + 1);
            if (next == _selectedRunHistoryIndex)
            {
                return false;
            }

            _selectedRunHistoryIndex = next;
            return true;
        }

        public bool SelectLatestRunOutcome()
        {
            if (!TryGetRunHistoryCount(out int historyCount))
            {
                return false;
            }

            int latestIndex = historyCount - 1;
            if (_selectedRunHistoryIndex == latestIndex)
            {
                return false;
            }

            _selectedRunHistoryIndex = latestIndex;
            return true;
        }

        public string GetSelectedSlotStructureId()
        {
            if (Save?.dungeonLayout?.Slots == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < Save.dungeonLayout.Slots.Count; i++)
            {
                DungeonSlot slot = Save.dungeonLayout.Slots[i];
                if (slot.FloorIndex == _selectedFloorIndex && slot.SlotIndex == _selectedSlotIndex)
                {
                    return slot.StructureId ?? string.Empty;
                }
            }

            return string.Empty;
        }

        public void CaptureOfflineSummaryDiagnostics()
        {
            if (Save == null)
            {
                return;
            }

            Save.lastOfflineSummary = ResolveOfflineSummary();
        }

        public bool SetResearchPendingScaffold()
        {
            if (Save == null)
            {
                return false;
            }

            ResearchPendingValidationResult result = ResearchPendingResolver.ResolveScaffold(GetResearchPendingScaffoldConfig());
            if (!result.RuleResolved)
            {
                RefreshOfflineSummaryLines();
                return false;
            }

            Save.researchPending = new ResearchPendingState
            {
                SlotId = result.SlotId,
                ProjectId = result.ProjectId
            };
            ResearchProgressScaffoldConfig progressConfig = GetResearchProgressScaffoldConfig();
            Save.researchProgress = new ResearchProgressState
            {
                SlotId = result.SlotId,
                ProjectId = result.ProjectId,
                RuleSourceIdUsed = progressConfig != null ? progressConfig.ruleSourceId : string.Empty
            };
            SaveService?.Save(Save, SaveReason.ManualDev);
            RefreshOfflineSummaryLines();
            return true;
        }

        public bool ClearResearchPendingScaffold()
        {
            if (Save == null)
            {
                return false;
            }

            Save.researchPending = null;
            Save.researchProgress = null;
            SaveService?.Save(Save, SaveReason.ManualDev);
            RefreshOfflineSummaryLines();
            return true;
        }

        public bool ClaimResearchCompletionScaffold()
        {
            if (Save == null)
            {
                return false;
            }

            ResearchCompletionClaimApplySummary summary = ResearchCompletionClaimApplyResolver.Resolve(
                Save.researchPending,
                Save.researchProgress,
                Save.completedResearch,
                GetResearchCompletionEligibilityScaffoldConfig(),
                GetResearchCompletionClaimScaffoldConfig());
            if (!summary.WouldRecordCompletedResearch)
            {
                RefreshOfflineSummaryLines();
                return false;
            }

            CompletedResearchState completed = Save.completedResearch ?? new CompletedResearchState();
            string[] source = completed.ProjectIds ?? Array.Empty<string>();
            bool alreadyPresent = false;
            for (int i = 0; i < source.Length; i++)
            {
                if (string.Equals(source[i], summary.ProjectId, StringComparison.Ordinal))
                {
                    alreadyPresent = true;
                    break;
                }
            }

            if (!alreadyPresent)
            {
                var appended = new string[source.Length + 1];
                Array.Copy(source, appended, source.Length);
                appended[source.Length] = summary.ProjectId;
                completed.ProjectIds = appended;
            }
            else if (completed.ProjectIds == null)
            {
                completed.ProjectIds = source;
            }

            completed.LastCompletedProjectId = summary.ProjectId;
            completed.LastCompletionRuleSourceId = summary.RuleSourceIdUsed;
            Save.completedResearch = completed;
            Save.researchPending = null;
            Save.researchProgress = null;
            SaveService?.Save(Save, SaveReason.ManualDev);
            RefreshOfflineSummaryLines();
            return true;
        }

        public void RefreshOfflineSummaryLines()
        {
            OfflineSummary summary = Save != null && IsUsablePersistedOfflineSummary(Save.lastOfflineSummary)
                ? Save.lastOfflineSummary
                : ResolveOfflineSummary();
            ResearchPendingValidationResult research = ResearchPendingResolver.Resolve(
                Save != null ? Save.researchPending : null,
                GetResearchPendingScaffoldConfig());

            const string offlineFormatKey = "ui.dev.offline_summary_format";
            string offlineFormat = Content != null ? Content.GetString(offlineFormatKey, offlineFormatKey) : offlineFormatKey;
            OfflineSummaryLine = string.Equals(offlineFormat, offlineFormatKey, StringComparison.Ordinal)
                ? offlineFormatKey
                : string.Format(
                    offlineFormat,
                    summary.RuleResolved,
                    summary.DeterministicErrorCode,
                    summary.OfflineSecondsObserved,
                    summary.OfflineWindowClamped,
                    summary.WouldProcessOfflineProgress,
                    summary.RuleSourceIdUsed ?? string.Empty);

            const string researchFormatKey = "ui.dev.research_pending_format";
            string researchFormat = Content != null ? Content.GetString(researchFormatKey, researchFormatKey) : researchFormatKey;
            ResearchPendingLine = string.Equals(researchFormat, researchFormatKey, StringComparison.Ordinal)
                ? researchFormatKey
                : string.Format(
                    researchFormat,
                    research.Pending,
                    research.SlotId ?? string.Empty,
                    research.ProjectId ?? string.Empty);

            const string validationFormatKey = "ui.dev.research_pending_validation_format";
            string validationFormat = Content != null ? Content.GetString(validationFormatKey, validationFormatKey) : validationFormatKey;
            ResearchPendingValidationLine = string.Equals(validationFormat, validationFormatKey, StringComparison.Ordinal)
                ? validationFormatKey
                : string.Format(
                    validationFormat,
                    research.RuleResolved,
                    research.DeterministicErrorCode,
                    research.RuleSourceIdUsed ?? string.Empty);

            ResearchPendingState progressPendingState = research.Pending && Save != null
                ? Save.researchPending
                : null;
            ResearchProgressSummary progress = ResearchProgressResolver.Resolve(
                progressPendingState,
                GetResearchProgressScaffoldConfig(),
                GetActiveSessionElapsedSeconds());
            const string progressFormatKey = "ui.dev.research_progress_format";
            string progressFormat = Content != null ? Content.GetString(progressFormatKey, progressFormatKey) : progressFormatKey;
            ResearchProgressLine = string.Equals(progressFormat, progressFormatKey, StringComparison.Ordinal)
                ? progressFormatKey
                : string.Format(
                    progressFormat,
                    progress.RuleResolved,
                    progress.DeterministicErrorCode,
                    progress.Pending,
                    progress.SlotId ?? string.Empty,
                    progress.ProjectId ?? string.Empty,
                    progress.ElapsedSecondsUsed,
                    progress.ProgressDeltaPreview,
                    progress.WouldCompleteResearch,
                    progress.RuleSourceIdUsed ?? string.Empty);

            ResearchProgressStateSummary progressState = ResearchProgressStateResolver.Resolve(
                progressPendingState,
                Save != null ? Save.researchProgress : null);
            const string progressStateFormatKey = "ui.dev.research_progress_state_format";
            string progressStateFormat = Content != null ? Content.GetString(progressStateFormatKey, progressStateFormatKey) : progressStateFormatKey;
            ResearchProgressStateLine = string.Equals(progressStateFormat, progressStateFormatKey, StringComparison.Ordinal)
                ? progressStateFormatKey
                : string.Format(
                    progressStateFormat,
                    progressState.RuleResolved,
                    progressState.DeterministicErrorCode,
                    progressState.Pending,
                    progressState.HasProgressState,
                    progressState.SlotId ?? string.Empty,
                    progressState.ProjectId ?? string.Empty,
                    progressState.ProgressUnits,
                    progressState.CompletionPending,
                    progressState.StateMatchesPending,
                    progressState.RuleSourceIdUsed ?? string.Empty);

            ResearchCompletionEligibilitySummary completionEligibility = ResearchCompletionEligibilityResolver.Resolve(
                progressPendingState,
                Save != null ? Save.researchProgress : null,
                GetResearchCompletionEligibilityScaffoldConfig());
            const string completionEligibilityFormatKey = "ui.dev.research_completion_eligibility_format";
            string completionEligibilityFormat = Content != null ? Content.GetString(completionEligibilityFormatKey, completionEligibilityFormatKey) : completionEligibilityFormatKey;
            ResearchCompletionEligibilityLine = string.Equals(completionEligibilityFormat, completionEligibilityFormatKey, StringComparison.Ordinal)
                ? completionEligibilityFormatKey
                : string.Format(
                    completionEligibilityFormat,
                    completionEligibility.RuleResolved,
                    completionEligibility.DeterministicErrorCode,
                    completionEligibility.Pending,
                    completionEligibility.HasProgressState,
                    completionEligibility.SlotId ?? string.Empty,
                    completionEligibility.ProjectId ?? string.Empty,
                    completionEligibility.ProgressUnits,
                    completionEligibility.RequiredProgressUnits,
                    completionEligibility.RemainingProgressUnits,
                    completionEligibility.EligibleForCompletion,
                    completionEligibility.WouldSetCompletionPending,
                    completionEligibility.WouldCompleteResearch,
                    completionEligibility.RuleSourceIdUsed ?? string.Empty);

            ResearchCompletionPendingApplySummary completionPendingApply = ResearchCompletionPendingApplyResolver.Resolve(
                progressPendingState,
                Save != null ? Save.researchProgress : null,
                GetResearchCompletionEligibilityScaffoldConfig());
            const string completionPendingApplyFormatKey = "ui.dev.research_completion_pending_apply_format";
            string completionPendingApplyFormat = Content != null ? Content.GetString(completionPendingApplyFormatKey, completionPendingApplyFormatKey) : completionPendingApplyFormatKey;
            ResearchCompletionPendingApplyLine = string.Equals(completionPendingApplyFormat, completionPendingApplyFormatKey, StringComparison.Ordinal)
                ? completionPendingApplyFormatKey
                : string.Format(
                    completionPendingApplyFormat,
                    completionPendingApply.RuleResolved,
                    completionPendingApply.DeterministicErrorCode,
                    completionPendingApply.Pending,
                    completionPendingApply.HasProgressState,
                    completionPendingApply.SlotId ?? string.Empty,
                    completionPendingApply.ProjectId ?? string.Empty,
                    completionPendingApply.ProgressUnits,
                    completionPendingApply.RequiredProgressUnits,
                    completionPendingApply.EligibleForCompletion,
                    completionPendingApply.AlreadyCompletionPending,
                    completionPendingApply.WouldSetCompletionPending,
                    completionPendingApply.WouldCompleteResearch,
                    completionPendingApply.RuleSourceIdUsed ?? string.Empty);

            ResearchCompletionClaimReadinessSummary claimReadiness = ResearchCompletionClaimReadinessResolver.Resolve(
                progressPendingState,
                Save != null ? Save.researchProgress : null,
                GetResearchCompletionEligibilityScaffoldConfig());
            const string claimReadinessFormatKey = "ui.dev.research_completion_claim_readiness_format";
            string claimReadinessFormat = Content != null ? Content.GetString(claimReadinessFormatKey, claimReadinessFormatKey) : claimReadinessFormatKey;
            ResearchCompletionClaimReadinessLine = string.Equals(claimReadinessFormat, claimReadinessFormatKey, StringComparison.Ordinal)
                ? claimReadinessFormatKey
                : string.Format(
                    claimReadinessFormat,
                    claimReadiness.RuleResolved,
                    claimReadiness.DeterministicErrorCode,
                    claimReadiness.Pending,
                    claimReadiness.HasProgressState,
                    claimReadiness.SlotId ?? string.Empty,
                    claimReadiness.ProjectId ?? string.Empty,
                    claimReadiness.ProgressUnits,
                    claimReadiness.RequiredProgressUnits,
                    claimReadiness.CompletionPending,
                    claimReadiness.EligibleForCompletion,
                    claimReadiness.ReadyForClaim,
                    claimReadiness.WouldCompleteResearch,
                    claimReadiness.WouldGrantRewards,
                    claimReadiness.WouldUnlockContent,
                    claimReadiness.WouldClearPending,
                    claimReadiness.RuleSourceIdUsed ?? string.Empty);

            ResearchStatusPresentation statusPresentation = ResearchStatusPresenter.Present(
                progressPendingState,
                Save != null ? Save.researchProgress : null,
                Save != null ? Save.completedResearch : null,
                GetResearchCompletionEligibilityScaffoldConfig());
            const string statusPresentationFormatKey = "ui.dev.research_status_presentation_format";
            string statusPresentationFormat = Content != null ? Content.GetString(statusPresentationFormatKey, statusPresentationFormatKey) : statusPresentationFormatKey;
            ResearchStatusPresentationLine = string.Equals(statusPresentationFormat, statusPresentationFormatKey, StringComparison.Ordinal)
                ? statusPresentationFormatKey
                : string.Format(
                    statusPresentationFormat,
                    statusPresentation.State,
                    statusPresentation.Pending,
                    statusPresentation.HasProgressState,
                    statusPresentation.HasCompletedState,
                    statusPresentation.SlotId ?? string.Empty,
                    statusPresentation.ProjectId ?? string.Empty,
                    statusPresentation.ProgressUnits,
                    statusPresentation.RequiredProgressUnits,
                    statusPresentation.CompletionPending,
                    statusPresentation.EligibleForCompletion,
                    statusPresentation.VerificationRequired,
                    statusPresentation.ReadyToClaim,
                    statusPresentation.Completed,
                    statusPresentation.BlockedOrInvalid,
                    statusPresentation.StatusLocalizationKey ?? string.Empty,
                    statusPresentation.RuleSourceIdUsed ?? string.Empty);

            const string statusSafetyFormatKey = "ui.dev.research_status_safety_format";
            string statusSafetyFormat = Content != null ? Content.GetString(statusSafetyFormatKey, statusSafetyFormatKey) : statusSafetyFormatKey;
            ResearchStatusSafetyLine = string.Equals(statusSafetyFormat, statusSafetyFormatKey, StringComparison.Ordinal)
                ? statusSafetyFormatKey
                : string.Format(
                    statusSafetyFormat,
                    statusPresentation.CanClaimProduction,
                    statusPresentation.WouldGrantRewards,
                    statusPresentation.WouldUnlockContent,
                    statusPresentation.WouldChargeCosts,
                    statusPresentation.WouldProcessOfflineProgress);


            ResearchVerificationBoundarySummary verificationBoundary = ResearchVerificationBoundaryResolver.Resolve(
                progressPendingState,
                Save != null ? Save.researchProgress : null,
                Save != null ? Save.completedResearch : null,
                GetResearchCompletionEligibilityScaffoldConfig(),
                GetResearchVerificationScaffoldConfig());
            const string verificationBoundaryFormatKey = "ui.dev.research_verification_boundary_format";
            string verificationBoundaryFormat = Content != null ? Content.GetString(verificationBoundaryFormatKey, verificationBoundaryFormatKey) : verificationBoundaryFormatKey;
            ResearchVerificationBoundaryLine = string.Equals(verificationBoundaryFormat, verificationBoundaryFormatKey, StringComparison.Ordinal)
                ? verificationBoundaryFormatKey
                : string.Format(
                    verificationBoundaryFormat,
                    verificationBoundary.RuleResolved,
                    verificationBoundary.DeterministicErrorCode,
                    verificationBoundary.Pending,
                    verificationBoundary.HasProgressState,
                    verificationBoundary.HasCompletedState,
                    verificationBoundary.SlotId ?? string.Empty,
                    verificationBoundary.ProjectId ?? string.Empty,
                    verificationBoundary.ProgressUnits,
                    verificationBoundary.RequiredProgressUnits,
                    verificationBoundary.CompletionPending,
                    verificationBoundary.EligibleForCompletion,
                    verificationBoundary.AlreadyCompleted,
                    verificationBoundary.VerificationRequired,
                    verificationBoundary.VerificationAvailable,
                    verificationBoundary.VerificationSatisfied,
                    verificationBoundary.CanClaimProduction,
                    verificationBoundary.VerificationModeUsed ?? string.Empty,
                    verificationBoundary.RuleSourceIdUsed ?? string.Empty);

            const string verificationSafetyFormatKey = "ui.dev.research_verification_safety_format";
            string verificationSafetyFormat = Content != null ? Content.GetString(verificationSafetyFormatKey, verificationSafetyFormatKey) : verificationSafetyFormatKey;
            ResearchVerificationSafetyLine = string.Equals(verificationSafetyFormat, verificationSafetyFormatKey, StringComparison.Ordinal)
                ? verificationSafetyFormatKey
                : string.Format(
                    verificationSafetyFormat,
                    verificationBoundary.WouldCallServer,
                    verificationBoundary.WouldGrantRewards,
                    verificationBoundary.WouldUnlockContent,
                    verificationBoundary.WouldChargeCosts,
                    verificationBoundary.WouldProcessOfflineProgress);

            ResearchCompletionClaimApplySummary claimApply = ResearchCompletionClaimApplyResolver.Resolve(
                progressPendingState,
                Save != null ? Save.researchProgress : null,
                Save != null ? Save.completedResearch : null,
                GetResearchCompletionEligibilityScaffoldConfig(),
                GetResearchCompletionClaimScaffoldConfig());
            const string claimApplyFormatKey = "ui.dev.research_completion_claim_apply_format";
            string claimApplyFormat = Content != null ? Content.GetString(claimApplyFormatKey, claimApplyFormatKey) : claimApplyFormatKey;
            ResearchCompletionClaimApplyLine = string.Equals(claimApplyFormat, claimApplyFormatKey, StringComparison.Ordinal)
                ? claimApplyFormatKey
                : string.Format(
                    claimApplyFormat,
                    claimApply.RuleResolved,
                    claimApply.DeterministicErrorCode,
                    claimApply.Pending,
                    claimApply.HasProgressState,
                    claimApply.HasCompletedState,
                    claimApply.SlotId ?? string.Empty,
                    claimApply.ProjectId ?? string.Empty,
                    claimApply.ProgressUnits,
                    claimApply.RequiredProgressUnits,
                    claimApply.CompletionPending,
                    claimApply.EligibleForCompletion,
                    claimApply.ReadyForClaim,
                    claimApply.AlreadyCompleted,
                    claimApply.WouldRecordCompletedResearch,
                    claimApply.WouldClearPending,
                    claimApply.WouldClearProgress,
                    claimApply.WouldGrantRewards,
                    claimApply.WouldUnlockContent,
                    claimApply.WouldChargeCosts,
                    claimApply.WouldProcessOfflineProgress,
                    claimApply.RuleSourceIdUsed ?? string.Empty);

            CompletedResearchStateSummary completedResearch = CompletedResearchStateResolver.Resolve(
                Save != null ? Save.completedResearch : null,
                progressPendingState,
                Save != null ? Save.researchProgress : null);
            const string completedResearchFormatKey = "ui.dev.completed_research_state_format";
            string completedResearchFormat = Content != null ? Content.GetString(completedResearchFormatKey, completedResearchFormatKey) : completedResearchFormatKey;
            CompletedResearchStateLine = string.Equals(completedResearchFormat, completedResearchFormatKey, StringComparison.Ordinal)
                ? completedResearchFormatKey
                : string.Format(
                    completedResearchFormat,
                    completedResearch.RuleResolved,
                    completedResearch.DeterministicErrorCode,
                    completedResearch.HasCompletedState,
                    completedResearch.CompletedCount,
                    completedResearch.LastCompletedProjectId ?? string.Empty,
                    completedResearch.CurrentPendingProjectId ?? string.Empty,
                    completedResearch.CurrentProgressProjectId ?? string.Empty,
                    completedResearch.CurrentProjectAlreadyCompleted,
                    completedResearch.WouldBlockClaimAsDuplicate,
                    completedResearch.WouldGrantRewards,
                    completedResearch.WouldUnlockContent,
                    completedResearch.RuleSourceIdUsed ?? string.Empty);
        }

        private ResearchPendingScaffoldConfig GetResearchPendingScaffoldConfig()
        {
            return Content != null && Content.Bootstrap != null ? Content.Bootstrap.researchPendingScaffold : null;
        }

        private ResearchProgressScaffoldConfig GetResearchProgressScaffoldConfig()
        {
            return Content != null && Content.Bootstrap != null ? Content.Bootstrap.researchProgressScaffold : null;
        }

        private ResearchCompletionEligibilityScaffoldConfig GetResearchCompletionEligibilityScaffoldConfig()
        {
            return Content != null && Content.Bootstrap != null ? Content.Bootstrap.researchCompletionEligibilityScaffold : null;
        }

        private ResearchCompletionClaimScaffoldConfig GetResearchCompletionClaimScaffoldConfig()
        {
            return Content != null && Content.Bootstrap != null ? Content.Bootstrap.researchCompletionClaimScaffold : null;
        }

        private ResearchVerificationScaffoldConfig GetResearchVerificationScaffoldConfig()
        {
            return Content != null && Content.Bootstrap != null ? Content.Bootstrap.researchVerificationScaffold : null;
        }

        private long GetActiveSessionElapsedSeconds()
        {
            long tickSeconds = Content != null && Content.Bootstrap != null ? Content.Bootstrap.tickSeconds : 0;
            if (_activeSessionTickCount <= 0 || tickSeconds <= 0)
            {
                return 0;
            }

            return _activeSessionTickCount > long.MaxValue / tickSeconds
                ? long.MaxValue
                : _activeSessionTickCount * tickSeconds;
        }

        private static bool IsUsablePersistedOfflineSummary(OfflineSummary summary)
        {
            return summary != null &&
                   (summary.RuleResolved ||
                    summary.DeterministicErrorCode != (int)OfflineSummaryErrorCode.None ||
                    !string.IsNullOrWhiteSpace(summary.RuleSourceIdUsed));
        }

        private OfflineSummary ResolveOfflineSummary()
        {
            return _offlineSummaryResolver != null
                ? _offlineSummaryResolver.Resolve(Save, Content != null && Content.Bootstrap != null ? Content.Bootstrap.timeRules : null)
                : new OfflineSummary { DeterministicErrorCode = (int)OfflineSummaryErrorCode.TimeRulesMissingOrInvalid };
        }

        public void RefreshStructureRuntimeLines()
        {
            if (Save?.structureRuntime == null)
            {
                return;
            }

            HeatLine = $"Heat: {Save.structureRuntime.Heat:0.00}";
            ManaLine = $"Mana: {Save.structureRuntime.ManaReserve:0.00}";
            TickLine = $"Tick: {Save.totalTicks}";
            RefreshCurrentHeatTierLine();
        }

        public void RefreshRunLine()
        {
            RefreshCurrentHeatTierLine();
            int historyCount = Save?.runHistory?.RecentOutcomes != null ? Save.runHistory.RecentOutcomes.Length : 0;
            if (historyCount > 0)
            {
                if (_selectedRunHistoryIndex < 0)
                {
                    _selectedRunHistoryIndex = historyCount - 1;
                }
                else
                {
                    _selectedRunHistoryIndex = Mathf.Clamp(_selectedRunHistoryIndex, 0, historyCount - 1);
                }
            }
            else
            {
                _selectedRunHistoryIndex = -1;
            }

            const string historyFormatKey = "ui.run.history_position_format";
            string historyFormat = Content != null
                ? Content.GetString(historyFormatKey, historyFormatKey)
                : historyFormatKey;
            int selectedPosition = _selectedRunHistoryIndex >= 0 ? _selectedRunHistoryIndex + 1 : 0;
            RunHistoryLine = string.Equals(historyFormat, historyFormatKey, StringComparison.Ordinal)
                ? historyFormatKey
                : string.Format(historyFormat, selectedPosition, historyCount);

            RunOutcomeRecord outcome = GetSelectedRunOutcome();
            if (outcome == null)
            {
                RunLine = Content != null ? Content.GetString("ui.run.none", "ui.run.none") : "ui.run.none";
                RunBreakdownLine = string.Empty;
                RunFeedbackLine = string.Empty;
                RunLootLine = BuildLootLine(outcome);
                RunSurvivalLine = BuildSurvivalLine(outcome);
                RunExtractionLine = BuildExtractionLine(outcome);
                RunHeatCoolingLine = BuildHeatCoolingLine(outcome);
                RunHeatDeltaLine = BuildHeatDeltaLine(outcome);
                RunHeatApplicationLine = BuildHeatApplicationLine(outcome);
                RunAdventurerAttractionLine = BuildAdventurerAttractionLine(outcome);
                RunAdventurerInterestForecastLine = BuildAdventurerInterestForecastLine(outcome);
                RunAdventurerDemandBudgetLine = BuildAdventurerDemandBudgetLine(outcome);
                return;
            }

            string reason = Content != null ? Content.GetString(outcome.ReasonKey, outcome.ReasonKey) : outcome.ReasonKey;
            const string latestFormatKey = "ui.run.latest_format";
            string format = Content != null
                ? Content.GetString(latestFormatKey, latestFormatKey)
                : latestFormatKey;
            RunLine = string.Equals(format, latestFormatKey, StringComparison.Ordinal)
                ? latestFormatKey
                : string.Format(
                    format,
                    outcome.RunId,
                    outcome.Success,
                    outcome.Score,
                    reason);

            if (!outcome.HasBreakdown)
            {
                RunBreakdownLine = string.Empty;
            }
            else
            {
                const string breakdownFormatKey = "ui.run.breakdown_format";
                string breakdownFormat = Content != null
                    ? Content.GetString(breakdownFormatKey, breakdownFormatKey)
                    : breakdownFormatKey;
                RunBreakdownLine = string.Equals(breakdownFormat, breakdownFormatKey, StringComparison.Ordinal)
                    ? breakdownFormatKey
                    : string.Format(breakdownFormat, outcome.FinalChance, outcome.SuccessThresholdUsed);
            }

            string[] feedbackTags = outcome.FeedbackTagKeys ?? Array.Empty<string>();
            if (feedbackTags.Length == 0)
            {
                RunFeedbackLine = string.Empty;
                RunLootLine = BuildLootLine(outcome);
                RunSurvivalLine = BuildSurvivalLine(outcome);
                RunExtractionLine = BuildExtractionLine(outcome);
                RunHeatCoolingLine = BuildHeatCoolingLine(outcome);
                RunHeatDeltaLine = BuildHeatDeltaLine(outcome);
                RunHeatApplicationLine = BuildHeatApplicationLine(outcome);
                RunAdventurerAttractionLine = BuildAdventurerAttractionLine(outcome);
                RunAdventurerInterestForecastLine = BuildAdventurerInterestForecastLine(outcome);
                RunAdventurerDemandBudgetLine = BuildAdventurerDemandBudgetLine(outcome);
                return;
            }

            string[] localizedTags = new string[feedbackTags.Length];
            for (int i = 0; i < feedbackTags.Length; i++)
            {
                string key = feedbackTags[i] ?? string.Empty;
                localizedTags[i] = Content != null ? Content.GetString(key, key) : key;
            }

            const string feedbackFormatKey = "ui.run.feedback_format";
            string feedbackFormat = Content != null
                ? Content.GetString(feedbackFormatKey, feedbackFormatKey)
                : feedbackFormatKey;
            RunFeedbackLine = string.Equals(feedbackFormat, feedbackFormatKey, StringComparison.Ordinal)
                ? feedbackFormatKey
                : string.Format(feedbackFormat, string.Join(", ", localizedTags));
            RunLootLine = BuildLootLine(outcome);
            RunSurvivalLine = BuildSurvivalLine(outcome);
            RunExtractionLine = BuildExtractionLine(outcome);
            RunHeatCoolingLine = BuildHeatCoolingLine(outcome);
            RunHeatDeltaLine = BuildHeatDeltaLine(outcome);
            RunHeatApplicationLine = BuildHeatApplicationLine(outcome);
            RunAdventurerAttractionLine = BuildAdventurerAttractionLine(outcome);
            RunAdventurerInterestForecastLine = BuildAdventurerInterestForecastLine(outcome);
            RunAdventurerDemandBudgetLine = BuildAdventurerDemandBudgetLine(outcome);
        }


        private string BuildLootLine(RunOutcomeRecord outcome)
        {
            RunLootSummary loot = outcome != null ? outcome.LootSummary : null;
            if (loot == null)
            {
                return string.Empty;
            }

            const string lootFormatKey = "ui.run.loot_summary_format";
            if (Content == null)
            {
                return lootFormatKey;
            }

            string format = Content.GetString(lootFormatKey, lootFormatKey);
            if (string.Equals(format, lootFormatKey, StringComparison.Ordinal))
            {
                return lootFormatKey;
            }

            int itemCount = loot.GeneratedItemIds != null ? loot.GeneratedItemIds.Length : 0;
            return string.Format(format, loot.LootTableId, loot.ResolverSuccess, loot.ResolverErrorCode, loot.RollCount, itemCount, loot.TotalGeneratedWorldValue, loot.TotalGeneratedReserveCost, loot.TotalGeneratedTradeableWorldValue);
        }
        private string BuildSurvivalLine(RunOutcomeRecord outcome)
        {
            RunSurvivalSummary survival = outcome != null ? outcome.SurvivalSummary : null;
            if (survival == null)
            {
                return string.Empty;
            }

            const string survivalFormatKey = "ui.run.survival_summary_format";
            if (Content == null)
            {
                return survivalFormatKey;
            }

            string format = Content.GetString(survivalFormatKey, survivalFormatKey);
            if (string.Equals(format, survivalFormatKey, StringComparison.Ordinal))
            {
                return survivalFormatKey;
            }

            return string.Format(format, survival.PartySize, survival.SurvivorCount, survival.DeathCount, survival.SurvivorRatio, survival.DeterministicSeed, survival.DeterministicErrorCode);
        }
        private string BuildExtractionLine(RunOutcomeRecord outcome)
        {
            RunLootExtractionSummary extraction = outcome != null ? outcome.LootExtractionSummary : null;
            if (extraction == null)
            {
                return string.Empty;
            }

            const string formatKey = "ui.run.extraction_summary_format";
            if (Content == null)
            {
                return formatKey;
            }

            string format = Content.GetString(formatKey, formatKey);
            if (string.Equals(format, formatKey, StringComparison.Ordinal))
            {
                return formatKey;
            }

            int extractedCount = extraction.ExtractedItemIds != null ? extraction.ExtractedItemIds.Length : 0;
            int lostCount = extraction.LostItemIds != null ? extraction.LostItemIds.Length : 0;
            return string.Format(format, extraction.RuleResolved, extraction.DeterministicErrorCode, extraction.SurvivorRatioUsed, extraction.GeneratedItemCount, extractedCount, lostCount, extraction.TotalExtractedWorldValue, extraction.TotalExtractedReserveCost, extraction.TotalExtractedTradeableWorldValue);
        }

        private string BuildHeatCoolingLine(RunOutcomeRecord outcome)
        {
            RunLootHeatCoolingSummary cooling = outcome != null ? outcome.LootHeatCoolingSummary : null;
            if (cooling == null)
            {
                return string.Empty;
            }

            const string formatKey = "ui.run.heat_cooling_summary_format";
            if (Content == null)
            {
                return formatKey;
            }

            string format = Content.GetString(formatKey, formatKey);
            if (string.Equals(format, formatKey, StringComparison.Ordinal))
            {
                return formatKey;
            }

            return string.Format(format, cooling.RuleResolved, cooling.DeterministicErrorCode, cooling.ExtractedTradeableWorldValueUsed, cooling.CoolingPerTradeableWorldValueUsed, cooling.UnclampedHeatDelta, cooling.AppliedHeatDelta, cooling.HeatBeforeCooling, cooling.HeatAfterCooling);
        }

        private string BuildHeatDeltaLine(RunOutcomeRecord outcome)
        {
            RunHeatDeltaSummary heatDelta = outcome != null ? outcome.RunHeatDeltaSummary : null;
            if (heatDelta == null || !heatDelta.RuleResolved)
            {
                return string.Empty;
            }

            const string formatKey = "ui.run.heat_delta_summary_format";
            if (Content == null)
            {
                return formatKey;
            }

            string format = Content.GetString(formatKey, formatKey);
            if (string.Equals(format, formatKey, StringComparison.Ordinal))
            {
                return formatKey;
            }

            return string.Format(format, heatDelta.RuleResolved, heatDelta.DeterministicErrorCode, heatDelta.DeathHeatDelta, heatDelta.EliteDeathHeatDelta, heatDelta.MultipleDeathBonusDelta, heatDelta.SurvivorCoolingDelta, heatDelta.LootCoolingDelta, heatDelta.FinalHeatDelta, heatDelta.RuleSourceIdUsed);
        }

        private void RefreshCurrentHeatTierLine()
        {
            RunSimulationConfig config = _runSimulationService != null ? _runSimulationService.Config : null;
            double currentHeat = Save != null && Save.structureRuntime != null
                ? Save.structureRuntime.Heat
                : CurrentHeat;
            CurrentHeatTierSummary summary = CurrentHeatTierResolver.Resolve(config, currentHeat);
            const string formatKey = "ui.heat.current_tier_summary_format";
            if (Content == null)
            {
                CurrentHeatTierLine = formatKey;
                return;
            }

            string format = Content.GetString(formatKey, formatKey);
            CurrentHeatTierLine = string.Equals(format, formatKey, StringComparison.Ordinal)
                ? formatKey
                : string.Format(format, summary.RuleResolved, summary.DeterministicErrorCode, summary.CurrentHeat, summary.TierId, summary.TierMinimum, summary.TierMaximum, summary.IsAtTierMinimum, summary.IsAtTierMaximum, summary.RuleSourceIdUsed);
        }

        private string BuildHeatApplicationLine(RunOutcomeRecord outcome)
        {
            RunHeatApplicationSummary application = outcome != null ? outcome.RunHeatApplicationSummary : null;
            if (application == null || !application.RuleResolved)
            {
                return string.Empty;
            }

            const string formatKey = "ui.run.heat_application_summary_format";
            if (Content == null)
            {
                return formatKey;
            }

            string format = Content.GetString(formatKey, formatKey);
            if (string.Equals(format, formatKey, StringComparison.Ordinal))
            {
                return formatKey;
            }

            return string.Format(format, application.RuleResolved, application.DeterministicErrorCode, application.HeatBefore, application.AppliedDelta, application.HeatAfter, application.TierBefore, application.TierAfter, application.TierChanged, application.RuleSourceIdUsed);
        }

        private string BuildAdventurerAttractionLine(RunOutcomeRecord outcome)
        {
            RunAdventurerAttractionSummary attraction = outcome != null ? outcome.AdventurerAttractionSummary : null;
            if (attraction == null)
            {
                return string.Empty;
            }

            const string formatKey = "ui.run.adventurer_attraction_summary_format";
            if (Content == null)
            {
                return formatKey;
            }

            string format = Content.GetString(formatKey, formatKey);
            if (string.Equals(format, formatKey, StringComparison.Ordinal))
            {
                return formatKey;
            }

            return string.Format(format, attraction.RuleResolved, attraction.DeterministicErrorCode, attraction.ExtractedWorldValueUsed, attraction.AttractionPerExtractedWorldValueUsed, attraction.AttractionSignalValue);
        }

        private string BuildAdventurerInterestForecastLine(RunOutcomeRecord outcome)
        {
            RunAdventurerInterestForecastSummary forecast = outcome != null ? outcome.AdventurerInterestForecastSummary : null;
            if (forecast == null)
            {
                return string.Empty;
            }

            const string formatKey = "ui.run.adventurer_interest_forecast_summary_format";
            if (Content == null)
            {
                return formatKey;
            }

            string format = Content.GetString(formatKey, formatKey);
            if (string.Equals(format, formatKey, StringComparison.Ordinal))
            {
                return formatKey;
            }

            return string.Format(format, forecast.RuleResolved, forecast.DeterministicErrorCode, forecast.AttractionSignalUsed, forecast.ForecastInterestScore, forecast.ForecastBandId);
        }

        private string BuildAdventurerDemandBudgetLine(RunOutcomeRecord outcome)
        {
            RunAdventurerDemandBudgetSummary demandBudget = outcome != null ? outcome.AdventurerDemandBudgetSummary : null;
            if (demandBudget == null)
            {
                return string.Empty;
            }

            const string formatKey = "ui.run.adventurer_demand_budget_summary_format";
            if (Content == null)
            {
                return formatKey;
            }

            string format = Content.GetString(formatKey, formatKey);
            if (string.Equals(format, formatKey, StringComparison.Ordinal))
            {
                return formatKey;
            }

            return string.Format(format, demandBudget.RuleResolved, demandBudget.DeterministicErrorCode, demandBudget.ForecastInterestScoreUsed, demandBudget.ForecastBandIdUsed, demandBudget.DemandBudgetScore, demandBudget.DemandBudgetBandId);
        }

        private bool TryGetRunHistoryCount(out int historyCount)
        {
            historyCount = Save?.runHistory?.RecentOutcomes != null ? Save.runHistory.RecentOutcomes.Length : 0;
            if (historyCount <= 0)
            {
                _selectedRunHistoryIndex = -1;
                return false;
            }

            if (_selectedRunHistoryIndex < 0)
            {
                _selectedRunHistoryIndex = historyCount - 1;
            }
            else
            {
                _selectedRunHistoryIndex = Mathf.Clamp(_selectedRunHistoryIndex, 0, historyCount - 1);
            }
            return true;
        }

        private RunOutcomeRecord GetSelectedRunOutcome()
        {
            if (!TryGetRunHistoryCount(out _))
            {
                return null;
            }

            return Save.runHistory.RecentOutcomes[_selectedRunHistoryIndex];
        }

        private void HandleSimulationTick(long tickIndex)
        {
            if (_activeSessionTickCount < long.MaxValue)
            {
                _activeSessionTickCount += 1;
            }
            ApplyResearchProgressForActiveTick();
            ApplyResearchCompletionPendingForActiveTick();
            RefreshOfflineSummaryLines();
            HeatResult decayResult = _heatSystem.Decay(new HeatDecayInput(
                tickIndex,
                CurrentHeat,
                HeatDecayPerTick,
                1
            ));

            CurrentHeat = decayResult.NewHeat;
            HeatLine = $"Heat: {CurrentHeat:0.00}";
            if (Save?.structureRuntime != null)
            {
                Save.structureRuntime.Heat = CurrentHeat;
            }
            TickLine = $"Tick: {tickIndex}";
            KpiSnapshot snap = Kpi != null ? Kpi.Snapshot() : new KpiSnapshot(0, 0, 0);
            ManaLine = $"Mana: {snap.AverageManaPerTick:0.00}";
        }

        private void ApplyResearchProgressForActiveTick()
        {
            long tickSeconds = Content != null && Content.Bootstrap != null ? Content.Bootstrap.tickSeconds : 0;
            ResearchProgressApplySummary summary = ResearchProgressApplyResolver.Resolve(
                Save != null ? Save.researchPending : null,
                Save != null ? Save.researchProgress : null,
                GetResearchProgressScaffoldConfig(),
                tickSeconds);
            if (summary.RuleResolved && Save != null && Save.researchProgress != null)
            {
                Save.researchProgress.ProgressUnits = summary.NextProgressUnits;
            }
        }

        private void ApplyResearchCompletionPendingForActiveTick()
        {
            ResearchCompletionPendingApplySummary summary = ResearchCompletionPendingApplyResolver.Resolve(
                Save != null ? Save.researchPending : null,
                Save != null ? Save.researchProgress : null,
                GetResearchCompletionEligibilityScaffoldConfig());
            if (summary.WouldSetCompletionPending && Save != null && Save.researchProgress != null)
            {
                Save.researchProgress.CompletionPending = true;
            }
        }

        private void HandleTickTelemetry(long tickIndex)
        {
            Telemetry?.Track("tick_processed", $"{{\"tick\":{tickIndex}}}");
        }
    }
}
