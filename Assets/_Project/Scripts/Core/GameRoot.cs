using DungeonBuilder.M0.Economy;
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

        private AppStateMachine _sm;
#if UNITY_EDITOR
        private string _editorFallbackSummary = string.Empty;
#endif

        private readonly IRestrictedActionGate _restrictedActionGate = new RestrictedActionGateService();
        private readonly IHeatSystem _heatSystem = new HeatSystem();

        public bool DevPanelEnabled { get; private set; }
        public bool IsOnline { get; private set; } = true;
        public bool VerificationPending { get; private set; }
        public double CurrentHeat { get; private set; }
        public double HeatDecayPerTick { get; private set; } = 0.1d;

        public string BuildLine { get; private set; } = "Build: unknown";
        public string StateLine => "State: " + (_sm != null ? _sm.CurrentStateName : "None");

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

            if (assignedFields.Count > 0)
            {
                _editorFallbackSummary = "Editor fallback assigned JSON fields: " + string.Join(", ", assignedFields);
                Logger?.Warn(_editorFallbackSummary);
            }
            else
            {
                _editorFallbackSummary = string.Empty;
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

#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(_editorFallbackSummary))
            {
                SetBanner(string.IsNullOrEmpty(BannerMessage)
                    ? _editorFallbackSummary
                    : BannerMessage + "\n" + _editorFallbackSummary);
            }
#endif

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
            SaveService.Save(Save, SaveReason.Boot);
            SaveLine = "Save: Boot";

            Logger.Info("M0 init complete.");
            RefreshDashboardState();
        }

        public void GoHomeStub()
        {
            _sm.SetState(new HomeStubState(this));
        }

        public void SetBanner(string message)
        {
            BannerMessage = message ?? string.Empty;
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
