using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
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
        public string RunHistoryLine { get; private set; } = "ui.run.history_summary_format";
        public string RunBreakdownLine { get; private set; } = string.Empty;
        public string RunFeedbackLine { get; private set; } = string.Empty;

        private AppStateMachine _sm;
#if UNITY_EDITOR
        private string _editorFallbackWarningLine = string.Empty;
#endif

        private readonly IRestrictedActionGate _restrictedActionGate = new RestrictedActionGateService();
        private readonly IHeatSystem _heatSystem = new HeatSystem();
        private readonly PlacementService _placementService = new PlacementService();
        private StructureSimulationPass _structureSimulationPass;
        private RunSimulationService _runSimulationService;
        private int _selectedFloorIndex;
        private int _selectedSlotIndex;

        public bool DevPanelEnabled { get; private set; }
        public bool IsOnline { get; private set; } = true;
        public bool VerificationPending { get; private set; }
        public double CurrentHeat { get; private set; }
        public double HeatDecayPerTick { get; private set; } = 0.1d;

        public string BuildLine { get; private set; } = "Build: unknown";
        public string StateLine => "State: " + (_sm != null ? _sm.CurrentStateName : "None");
        public int SelectedFloorIndex => _selectedFloorIndex;
        public int SelectedSlotIndex => _selectedSlotIndex;

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
            if (!TryCreateRunSimulationService(json, out _runSimulationService))
            {
                SetBanner(Content.GetString("ui.banner.run_sim_failed", "ui.banner.run_sim_failed"));
            }
        }

        internal static bool TryCreateRunSimulationService(string configJson, out RunSimulationService service)
        {
            service = null;

            try
            {
                RunSimulationConfig config = JsonUtility.FromJson<RunSimulationConfig>(configJson);
                if (!IsValidRunSimulationConfig(config))
                {
                    return false;
                }

                service = new RunSimulationService(config);
                return true;
            }
            catch
            {
                return false;
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

            return true;
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
                _placementService.PlaceStructure(Save.dungeonLayout, _selectedFloorIndex, _selectedSlotIndex, structureId);
                SaveService.Save(Save, SaveReason.ManualDev);
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

        public bool SimulateRunOnce()
        {
            if (_runSimulationService == null || Save?.structureRuntime == null || Save.runHistory == null)
            {
                return false;
            }

            long tickStarted = Save.totalTicks;
            int sequence = Math.Max(1, Save.runHistory.NextRunSequence);
            RunOutcomeRecord outcome = _runSimulationService.SimulateOnce(Save.structureRuntime, tickStarted, sequence);
            RunSimulationConfig config = _runSimulationService.Config;
            Save.runHistory.AppendOutcome(outcome, config.MaxRunHistoryEntries);
            Save.runHistory.NextRunSequence = sequence + 1;
            RefreshRunLine();
            SaveService.Save(Save, SaveReason.ManualDev);
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

        public void RefreshStructureRuntimeLines()
        {
            if (Save?.structureRuntime == null)
            {
                return;
            }

            HeatLine = $"Heat: {Save.structureRuntime.Heat:0.00}";
            ManaLine = $"Mana: {Save.structureRuntime.ManaReserve:0.00}";
            TickLine = $"Tick: {Save.totalTicks}";
        }

        public void RefreshRunLine()
        {
            string summaryFormat = Content != null
                ? Content.GetString("ui.run.history_summary_format", "ui.run.history_summary_format")
                : "ui.run.history_summary_format";
            int historyCount = Save?.runHistory?.RecentOutcomes != null ? Save.runHistory.RecentOutcomes.Length : 0;
            RunHistoryLine = string.Format(summaryFormat, historyCount);

            if (Save?.runHistory?.LatestOutcome == null)
            {
                RunLine = Content != null ? Content.GetString("ui.run.none", "ui.run.none") : "ui.run.none";
                RunBreakdownLine = string.Empty;
                RunFeedbackLine = string.Empty;
                return;
            }

            RunOutcomeRecord outcome = Save.runHistory.LatestOutcome;
            string reason = Content != null ? Content.GetString(outcome.ReasonKey, outcome.ReasonKey) : outcome.ReasonKey;
            string format = Content != null
                ? Content.GetString("ui.run.latest_format", "ui.run.latest_format")
                : "ui.run.latest_format";
            RunLine = string.Format(
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
                string breakdownFormat = Content != null
                    ? Content.GetString("ui.run.breakdown_format", "ui.run.breakdown_format")
                    : "ui.run.breakdown_format";
                RunBreakdownLine = string.Format(breakdownFormat, outcome.FinalChance, outcome.SuccessThresholdUsed);
            }

            string[] feedbackTags = outcome.FeedbackTagKeys ?? Array.Empty<string>();
            if (feedbackTags.Length == 0)
            {
                RunFeedbackLine = string.Empty;
                return;
            }

            string[] localizedTags = new string[feedbackTags.Length];
            for (int i = 0; i < feedbackTags.Length; i++)
            {
                string key = feedbackTags[i] ?? string.Empty;
                localizedTags[i] = Content != null ? Content.GetString(key, key) : key;
            }

            string feedbackFormat = Content != null
                ? Content.GetString("ui.run.feedback_format", "ui.run.feedback_format")
                : "ui.run.feedback_format";
            RunFeedbackLine = string.Format(feedbackFormat, string.Join(", ", localizedTags));
        }

        private void HandleSimulationTick(long tickIndex)
        {
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

        private void HandleTickTelemetry(long tickIndex)
        {
            Telemetry?.Track("tick_processed", $"{{\"tick\":{tickIndex}}}");
        }
    }
}
