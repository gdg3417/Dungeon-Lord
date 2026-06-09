using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.M0
{
    public class BootstrapOverlay : MonoBehaviour
    {
        [Header("UI")]
        public TMP_Text overlayText;

        private const int DiagnosticsPageCount = 9;
        private const int RuntimeSummaryPage = 0;
        private const int RunDiagnosticsPage = 1;
        private const int HeatDiagnosticsPage = 2;
        private const int SystemsDiagnosticsPage = 3;
        private const int ResearchDiagnosticsPage = 4;
        private const int ResearchStatusPresentationDiagnosticsPage = 5;
        private const int ResearchStatusSafetyDiagnosticsPage = 6;
        private const int ResearchVerificationBoundaryDiagnosticsPage = 7;
        private const int ResearchVerificationSafetyDiagnosticsPage = 8;
        private const int VisibleDiagnosticsBodyLineCount = 4;
        private const float MinimalMvpActionPanelWidth = 260f;
        private const float MinimalMvpActionPanelHeight = 300f;
        private const float MinimalMvpActionPanelMargin = 10f;
        private const float MinimalMvpActionPanelLabelHeight = 17f;
        private const float MinimalMvpActionPanelButtonHeight = 19f;
        private const float OverlayTextSafeLeftMargin = 24f;
        private const float OverlayTextSafeTopMargin = 14f;
        private const float OverlayTextSafeBottomMargin = 10f;
        private const float OverlayTextRightActionPanelReserve = MinimalMvpActionPanelWidth + (MinimalMvpActionPanelMargin * 2f) + OverlayTextSafeLeftMargin;
        private const string DefaultMvpStructureId = StructureSimulationPass.ManaGeneratorBasicId;

        private GameRoot _root;
        private bool _devPanelVisible;
        private bool _runDiagnosticsOnlyVisible;
        private bool _diagnosticsVisible;
        private int _fullDiagnosticsPage;
        private readonly int[] _fullDiagnosticsPageScrollOffsets = new int[DiagnosticsPageCount];
        private Vector2 _devPanelScrollPosition;
        private string _selectedMvpStructureId = DefaultMvpStructureId;
        private string _selectedMvpRunPostureId = RunPostureResolver.BalancedId;
        private string _mvpStructurePlacementFeedback = string.Empty;
        private string _mvpRunResultFeedback = string.Empty;

        public int FullDiagnosticsPageNumber => _fullDiagnosticsPage + 1;
        public int FullDiagnosticsScrollOffset => _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage];
        public bool DevPanelVisible => _devPanelVisible;
        public bool DiagnosticsVisible => _diagnosticsVisible || _runDiagnosticsOnlyVisible;
        public bool PlayerFacingPanelsVisible => !_runDiagnosticsOnlyVisible;
        public bool MinimalMvpActionGuiVisible => _root != null && PlayerFacingPanelsVisible;
        public string SelectedMvpStructureId => _selectedMvpStructureId;
        public string SelectedMvpRunPostureId => _selectedMvpRunPostureId;
        public string MvpStructurePlacementFeedback => _mvpStructurePlacementFeedback;
        public string MvpRunResultFeedback => _mvpRunResultFeedback;

        public void Bind(GameRoot root)
        {
            _root = root;
        }

        public void CycleFullDiagnosticsPage()
        {
            _fullDiagnosticsPage = (_fullDiagnosticsPage + 1) % DiagnosticsPageCount;
            _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] = 0;
        }

        public void ToggleDevPanel()
        {
            _devPanelVisible = !_devPanelVisible;
        }

        public void ToggleRunDiagnosticsFocus()
        {
            _runDiagnosticsOnlyVisible = !_runDiagnosticsOnlyVisible;
            _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] = 0;
        }

        public void ToggleDiagnosticsVisibility()
        {
            _diagnosticsVisible = !_diagnosticsVisible;
            if (_diagnosticsVisible)
            {
                _fullDiagnosticsPage = RuntimeSummaryPage;
            }
            _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] = 0;
        }

        public bool SelectMvpStructure(string structureId)
        {
            if (!IsAllowedMvpStructure(structureId))
            {
                return false;
            }

            _selectedMvpStructureId = structureId;
            return true;
        }

        public string GetSelectedMvpStructureNameKey()
        {
            return GetMvpSelectionNameKey(_selectedMvpStructureId);
        }

        public string GetSelectedMvpStructureDisplayName()
        {
            return MvpPlayerFacingLabelResolver.ResolveStructureDisplayName(_selectedMvpStructureId, (key, fallback) => GetLocalizedString(key, fallback));
        }

        public string GetSelectedMvpStructurePreviewText()
        {
            return MvpStructureImpactPreviewPresenter.BuildPreviewText(_selectedMvpStructureId, (key, fallback) => GetLocalizedString(key, fallback));
        }

        public bool SelectMvpRunPosture(string postureId)
        {
            if (!IsAllowedMvpRunPosture(postureId))
            {
                return false;
            }

            _selectedMvpRunPostureId = postureId;
            return true;
        }

        public string GetSelectedMvpRunPostureNameKey()
        {
            return GetMvpRunPostureNameKey(_selectedMvpRunPostureId);
        }

        public string GetSelectedMvpRunPostureDisplayName()
        {
            return GetLocalizedString(GetSelectedMvpRunPostureNameKey(), GetSelectedMvpRunPostureNameKey());
        }

        public void PlaceSelectedMvpStructure()
        {
            ShowPlayerPlacementBanner();
        }

        public void RunOrObserveDungeon()
        {
            ShowPlayerRunBanner();
        }

        public bool ResetCleanMvpValidationSessionFromDevPanel()
        {
            if (_root == null || !_root.ResetCleanMvpValidationSession())
            {
                return false;
            }

            _selectedMvpStructureId = DefaultMvpStructureId;
            _selectedMvpRunPostureId = RunPostureResolver.BalancedId;
            _mvpStructurePlacementFeedback = string.Empty;
            _mvpRunResultFeedback = string.Empty;
            _root.SetBanner(GetLocalizedString("ui.banner.clean_mvp_validation_reset", "ui.banner.clean_mvp_validation_reset"));
            RefreshOverlayText();
            return true;
        }

        public void ScrollFullDiagnosticsLines(int lineDelta)
        {
            if (_runDiagnosticsOnlyVisible || !_diagnosticsVisible || lineDelta == 0)
            {
                return;
            }

            string[] bodyLines = BuildCurrentFullDiagnosticsBody().ToString().Split('\n');
            int maxOffset = Mathf.Max(0, bodyLines.Length - VisibleDiagnosticsBodyLineCount);
            _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] = Mathf.Clamp(
                _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] + lineDelta,
                0,
                maxOffset);
        }

        public void RefreshOverlayText()
        {
            if (_root == null || overlayText == null)
            {
                return;
            }

            ApplyOverlayTextSafeArea();
            overlayText.text = BuildOverlayText();
        }

        public void ApplyOverlayTextSafeArea()
        {
            if (overlayText == null || overlayText.rectTransform == null)
            {
                return;
            }

            RectTransform rectTransform = overlayText.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.offsetMin = new Vector2(OverlayTextSafeLeftMargin, OverlayTextSafeBottomMargin);
            rectTransform.offsetMax = new Vector2(-OverlayTextRightActionPanelReserve, -OverlayTextSafeTopMargin);
            overlayText.alignment = TextAlignmentOptions.TopLeft;
        }

        private void Update()
        {
            if (_root == null)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                ToggleDevPanel();
            }
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            {
                ToggleRunDiagnosticsFocus();
            }
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                CycleFullDiagnosticsPage();
            }
            if (Keyboard.current != null && Keyboard.current.pageUpKey.wasPressedThisFrame)
            {
                ScrollFullDiagnosticsLines(-VisibleDiagnosticsBodyLineCount);
            }
            if (Keyboard.current != null && Keyboard.current.pageDownKey.wasPressedThisFrame)
            {
                ScrollFullDiagnosticsLines(VisibleDiagnosticsBodyLineCount);
            }
            if (Mouse.current != null)
            {
                float scrollY = Mouse.current.scroll.ReadValue().y;
                if (scrollY > 0f)
                {
                    ScrollFullDiagnosticsLines(-1);
                }
                else if (scrollY < 0f)
                {
                    ScrollFullDiagnosticsLines(1);
                }
            }

            RefreshOverlayText();
        }

        private string BuildOverlayText()
        {
            var builder = new StringBuilder();
            if (!_runDiagnosticsOnlyVisible)
            {
                AppendMvpLoopSummaryPanel(builder);
                AppendLine(builder, string.Empty);
            }
            if (_runDiagnosticsOnlyVisible)
            {
                AppendHeader(builder);
                AppendRunDiagnostics(builder, includeBreakdownAndFeedback: false, includeHeatDiagnostics: true);
                return builder.ToString();
            }

            if (!_diagnosticsVisible)
            {
                AppendPlayerFacingStatus(builder);
                return builder.ToString();
            }

            AppendHeader(builder);
            AppendScrolledFullDiagnosticsBody(builder, BuildCurrentFullDiagnosticsBody());
            return builder.ToString();
        }

        private void AppendMvpLoopSummaryPanel(StringBuilder builder)
        {
            MvpPlayerLoopSummary summary = _root.ResolveMvpPlayerLoopSummary();
            string panelText = MvpLoopSummaryPanelPresenter.BuildPanelText(summary, (key, fallback) => GetLocalizedString(key, fallback));
            if (!string.IsNullOrEmpty(panelText))
            {
                AppendLine(builder, panelText);
            }

            GuidedMvpActionPathSummary guidedPath = _root.ResolveGuidedMvpActionPath(summary);
            string guidedText = GuidedMvpActionPathPanelPresenter.BuildPanelText(guidedPath, (key, fallback) => GetLocalizedString(key, fallback));
            if (!string.IsNullOrEmpty(guidedText))
            {
                AppendLine(builder, string.Empty);
                AppendLine(builder, guidedText);
            }

            string firstSessionText = FirstSessionMvpCompletionPresenter.BuildStatusLine(summary, guidedPath, (key, fallback) => GetLocalizedString(key, fallback));
            if (!string.IsNullOrEmpty(firstSessionText))
            {
                AppendLine(builder, string.Empty);
                AppendLine(builder, firstSessionText);
            }
        }

        private void AppendPlayerFacingStatus(StringBuilder builder)
        {
            AppendLine(builder, string.Empty);
            AppendLine(builder, GetLocalizedString("ui.mvp_view.player_mode.status"));
            if (!string.IsNullOrEmpty(_root.BannerMessage))
            {
                AppendLine(builder, _root.BannerMessage);
            }
            if (!string.IsNullOrEmpty(_mvpStructurePlacementFeedback))
            {
                AppendLine(builder, _mvpStructurePlacementFeedback);
            }
            if (!string.IsNullOrEmpty(_mvpRunResultFeedback))
            {
                AppendLine(builder, _mvpRunResultFeedback);
            }
        }

        private void AppendHeader(StringBuilder builder)
        {
            if (_runDiagnosticsOnlyVisible)
            {
                AppendLine(builder, GetLocalizedString("ui.dev.diagnostics.focus.run_diagnostics"));
            }
            else
            {
                string pageName = GetLocalizedString(GetPageNameKey(_fullDiagnosticsPage));
                AppendLine(builder, string.Format(
                    GetLocalizedString("ui.dev.diagnostics.header_format"),
                    pageName,
                    _fullDiagnosticsPage + 1,
                    DiagnosticsPageCount));
            }
            AppendLine(builder, GetLocalizedString("ui.dev.hint.toggle_panel"));
            AppendLine(builder, GetLocalizedString("ui.dev.hint.toggle_run_diagnostics"));
            AppendLine(builder, GetLocalizedString("ui.dev.hint.cycle_diagnostics_page"));
            AppendLine(builder, GetLocalizedString("ui.dev.hint.scroll_diagnostics"));
        }

        private StringBuilder BuildCurrentFullDiagnosticsBody()
        {
            var builder = new StringBuilder();
            switch (_fullDiagnosticsPage)
            {
                case RuntimeSummaryPage:
                    AppendRuntimeSummary(builder);
                    break;
                case RunDiagnosticsPage:
                    AppendRunDiagnostics(builder, includeBreakdownAndFeedback: true, includeHeatDiagnostics: false);
                    break;
                case HeatDiagnosticsPage:
                    AppendHeatDiagnostics(builder);
                    break;
                case SystemsDiagnosticsPage:
                    AppendSystemsDiagnostics(builder);
                    break;
                case ResearchDiagnosticsPage:
                    AppendResearchDiagnostics(builder);
                    break;
                case ResearchStatusPresentationDiagnosticsPage:
                    AppendResearchStatusPresentationDiagnostics(builder);
                    break;
                case ResearchStatusSafetyDiagnosticsPage:
                    AppendResearchStatusSafetyDiagnostics(builder);
                    break;
                case ResearchVerificationBoundaryDiagnosticsPage:
                    AppendResearchVerificationBoundaryDiagnostics(builder);
                    break;
                case ResearchVerificationSafetyDiagnosticsPage:
                    AppendResearchVerificationSafetyDiagnostics(builder);
                    break;
            }
            return builder;
        }

        private void AppendScrolledFullDiagnosticsBody(StringBuilder builder, StringBuilder bodyBuilder)
        {
            string[] bodyLines = bodyBuilder.ToString().Split('\n');
            int maxOffset = Mathf.Max(0, bodyLines.Length - VisibleDiagnosticsBodyLineCount);
            int offset = Mathf.Clamp(_fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage], 0, maxOffset);
            _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] = offset;
            int end = Mathf.Min(bodyLines.Length, offset + VisibleDiagnosticsBodyLineCount);
            for (int i = offset; i < end; i++)
            {
                AppendLine(builder, bodyLines[i]);
            }
        }

        private void AppendRuntimeSummary(StringBuilder builder)
        {
            AppendLine(builder, _root.BuildLine);
            AppendLine(builder, _root.StateLine);
            AppendLine(builder, _root.PendingStateLine);
            AppendLine(builder, _root.GateStatusLine);
            AppendLine(builder, _root.KpiLine);
            AppendLine(builder, _root.TickLine);
            AppendLine(builder, _root.ManaLine);
            AppendLine(builder, _root.SaveLine);
            AppendLine(builder, _root.PauseLine);

            if (!string.IsNullOrEmpty(_root.BannerMessage))
            {
                AppendLine(builder, GetLocalizedString("ui.dev.banner.heading") + ":");
                AppendLine(builder, _root.BannerMessage);
            }
        }

        private void AppendRunDiagnostics(StringBuilder builder, bool includeBreakdownAndFeedback, bool includeHeatDiagnostics)
        {
            AppendLine(builder, _root.RunLine);
            AppendLine(builder, _root.RunHistoryLine);
            if (includeBreakdownAndFeedback)
            {
                AppendLine(builder, _root.RunBreakdownLine);
                AppendLine(builder, _root.RunFeedbackLine);
            }
            AppendLine(builder, _root.RunLootLine);
            AppendLine(builder, _root.RunSurvivalLine);
            AppendLine(builder, _root.RunExtractionLine);
            if (includeHeatDiagnostics)
            {
                AppendLine(builder, _root.RunHeatCoolingLine);
                AppendLine(builder, _root.RunHeatDeltaLine);
                AppendLine(builder, _root.RunHeatApplicationLine);
            }
            AppendLine(builder, _root.RunAdventurerAttractionLine);
            AppendLine(builder, _root.RunAdventurerInterestForecastLine);
            AppendLine(builder, _root.RunAdventurerDemandBudgetLine);
        }

        private void AppendHeatDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.HeatLine);
            AppendLine(builder, _root.CurrentHeatTierLine);
            AppendLine(builder, _root.RunHeatCoolingLine);
            AppendLine(builder, _root.RunHeatDeltaLine);
            AppendLine(builder, _root.RunHeatApplicationLine);
        }

        private void AppendSystemsDiagnostics(StringBuilder builder)
        {
            if (_root.Content == null)
            {
                return;
            }

            AppendLine(builder, string.Format(
                GetLocalizedString("ui.dev.structure_status"),
                _root.SelectedFloorIndex,
                _root.SelectedSlotIndex,
                _root.GetSelectedSlotStructureId(),
                _root.Save != null && _root.Save.structureRuntime != null && _root.Save.structureRuntime.IsHeatCrisisActive));
            AppendLine(builder, _root.OfflineSummaryLine);
        }

        private void AppendResearchDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.ResearchPendingLine);
            AppendLine(builder, _root.ResearchPendingValidationLine);
            AppendLine(builder, _root.ResearchProgressLine);
            AppendLine(builder, _root.ResearchProgressStateLine);
            AppendLine(builder, _root.ResearchCompletionEligibilityLine);
            AppendLine(builder, _root.ResearchCompletionPendingApplyLine);
            AppendLine(builder, _root.ResearchCompletionClaimReadinessLine);
            AppendLine(builder, _root.CompletedResearchStateLine);
            AppendLine(builder, _root.ResearchCompletionClaimApplyLine);
        }

        private void AppendResearchStatusPresentationDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.ResearchStatusPresentationLine);
        }

        private void AppendResearchStatusSafetyDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.ResearchStatusSafetyLine);
        }

        private void AppendResearchVerificationBoundaryDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.ResearchVerificationBoundaryLine);
        }

        private void AppendResearchVerificationSafetyDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.ResearchVerificationSafetyLine);
        }

        private string GetLocalizedString(string key)
        {
            return GetLocalizedString(key, key);
        }

        private string GetLocalizedString(string key, string fallback)
        {
            return _root.Content != null ? _root.Content.GetString(key, fallback) : fallback;
        }

        private static string GetPageNameKey(int page)
        {
            switch (page)
            {
                case RuntimeSummaryPage:
                    return "ui.dev.diagnostics.page.runtime_summary";
                case RunDiagnosticsPage:
                    return "ui.dev.diagnostics.page.run_diagnostics";
                case HeatDiagnosticsPage:
                    return "ui.dev.diagnostics.page.heat_diagnostics";
                case SystemsDiagnosticsPage:
                    return "ui.dev.diagnostics.page.systems_diagnostics";
                case ResearchDiagnosticsPage:
                    return "ui.dev.diagnostics.page.research_diagnostics";
                case ResearchStatusPresentationDiagnosticsPage:
                    return "ui.dev.diagnostics.page.research_status_presentation_diagnostics";
                case ResearchStatusSafetyDiagnosticsPage:
                    return "ui.dev.diagnostics.page.research_status_safety_diagnostics";
                case ResearchVerificationBoundaryDiagnosticsPage:
                    return "ui.dev.diagnostics.page.research_verification_boundary_diagnostics";
                case ResearchVerificationSafetyDiagnosticsPage:
                    return "ui.dev.diagnostics.page.research_verification_safety_diagnostics";
                default:
                    return "ui.dev.diagnostics.page.runtime_summary";
            }
        }

        private static void AppendLine(StringBuilder builder, string line)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }
            builder.Append(line ?? string.Empty);
        }

        private void OnGUI()
        {
            DrawMinimalMvpActionPanel();

            if (_root == null || !_root.DevPanelEnabled || !_devPanelVisible)
            {
                return;
            }

            float panelHeight = Mathf.Max(240f, Screen.height - 140f);
            GUILayout.BeginArea(new Rect(10, 120, 360, panelHeight), GUI.skin.box);
            GUILayout.Label(_root.Content.GetString("ui.dev.panel.title", "ui.dev.panel.title"));
            _devPanelScrollPosition = GUILayout.BeginScrollView(_devPanelScrollPosition);

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.save_now", "ui.dev.button.save_now")))
            {
                _root.SaveService.Save(_root.Save, SaveReason.ManualDev);
                _root.SetBanner(_root.Content.GetString("ui.banner.saved_dev", "ui.banner.saved_dev"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.delete_save", "ui.dev.button.delete_save")))
            {
                _root.SaveService.DeleteSave(out string banner);
                _root.SetBanner(banner);
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.clear_banner", "ui.dev.button.clear_banner")))
            {
                _root.SetBanner(string.Empty);
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.clean_mvp_validation_reset", "ui.dev.button.clean_mvp_validation_reset")))
            {
                ResetCleanMvpValidationSessionFromDevPanel();
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.toggle_online", "ui.dev.button.toggle_online")))
            {
                _root.SetOnline(!_root.IsOnline);
                if (!_root.IsOnline)
                {
                    string msg = _root.Content != null
                        ? _root.Content.GetString("ui.banner.offline", "Offline mode.")
                        : "Offline mode.";
                    _root.SetBanner(msg);
                }
                else
                {
                    _root.SetBanner(_root.Content.GetString("ui.banner.online_restored", "ui.banner.online_restored"));
                }
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.toggle_verification", "ui.dev.button.toggle_verification")))
            {
                _root.SetVerificationPending(!_root.VerificationPending);
                if (_root.VerificationPending)
                {
                    string msg = _root.Content != null
                        ? _root.Content.GetString("gate.error.verification_pending", "Verification pending.")
                        : "Verification pending.";
                    _root.SetBanner(msg);
                }
                else
                {
                    _root.SetBanner(_root.Content.GetString("ui.banner.verification_cleared", "ui.banner.verification_cleared"));
                }
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.toggle_pause", "ui.dev.button.toggle_pause")))
            {
                bool pause = _root.PauseLine != "Pause: Paused";
                _root.ApplyPauseState(pause);
                _root.SetBanner(pause
                    ? _root.Content.GetString("ui.banner.paused_dev_panel", "ui.banner.paused_dev_panel")
                    : _root.Content.GetString("ui.banner.resumed_dev_panel", "ui.banner.resumed_dev_panel"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.set_research_pending", "ui.dev.button.set_research_pending")))
            {
                bool didSet = _root.SetResearchPendingScaffold();
                _root.SetBanner(_root.Content.GetString(
                    didSet ? "ui.banner.research_pending_set" : "ui.banner.research_pending_set_failed",
                    didSet ? "ui.banner.research_pending_set" : "ui.banner.research_pending_set_failed"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.clear_research_pending", "ui.dev.button.clear_research_pending")))
            {
                _root.ClearResearchPendingScaffold();
                _root.SetBanner(_root.Content.GetString("ui.banner.research_pending_cleared", "ui.banner.research_pending_cleared"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.claim_research_completion", "ui.dev.button.claim_research_completion")))
            {
                bool didClaim = _root.ClaimResearchCompletionScaffold();
                _root.SetBanner(_root.Content.GetString(
                    didClaim ? "ui.banner.research_completion_claimed" : "ui.banner.research_completion_claim_failed",
                    didClaim ? "ui.banner.research_completion_claimed" : "ui.banner.research_completion_claim_failed"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.sim_mana", "ui.dev.button.sim_mana")))
            {
                _root.TrackManaGenerated(10);
                _root.SetBanner(_root.Content.GetString("ui.banner.simulated_mana_kpi", "ui.banner.simulated_mana_kpi"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.sim_heat", "ui.dev.button.sim_heat")))
            {
                _root.ApplyHeatDelta(5d);
                _root.SetBanner(_root.Content.GetString("ui.banner.applied_heat_event", "ui.banner.applied_heat_event"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.select_slot", "ui.dev.button.select_slot")))
            {
                _root.SelectNextSlot();
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.place_mana_generator", "ui.dev.button.place_mana_generator")))
            {
                ShowPlacementBanner(StructureSimulationPass.ManaGeneratorBasicId);
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.place_heat_scrubber", "ui.dev.button.place_heat_scrubber")))
            {
                ShowPlacementBanner(StructureSimulationPass.HeatScrubberBasicId);
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.place_risk_lab", "ui.dev.button.place_risk_lab")))
            {
                ShowPlacementBanner(StructureSimulationPass.RiskLabBasicId);
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.sim_structure_tick", "ui.dev.button.sim_structure_tick")))
            {
                bool didRun = _root.SimulateStructureTick();
                _root.SetBanner(didRun
                    ? _root.Content.GetString("ui.banner.simulated_tick", "ui.banner.simulated_tick")
                    : _root.Content.GetString("ui.banner.structure_tick_failed", "ui.banner.structure_tick_failed"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.sim_run_once", "ui.dev.button.sim_run_once")))
            {
                bool didRun = _root.SimulateRunOnce();
                _root.SetBanner(didRun
                    ? _root.Content.GetString("ui.banner.run_simulated", "ui.banner.run_simulated")
                    : _root.Content.GetString("ui.banner.run_sim_failed", "ui.banner.run_sim_failed"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.run_previous", "ui.dev.button.run_previous")))
            {
                _root.SelectPreviousRunOutcome();
                _root.RefreshRunLine();
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.run_next", "ui.dev.button.run_next")))
            {
                _root.SelectNextRunOutcome();
                _root.RefreshRunLine();
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.run_latest", "ui.dev.button.run_latest")))
            {
                _root.SelectLatestRunOutcome();
                _root.RefreshRunLine();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }


        public Rect GetMinimalMvpActionPanelRect()
        {
            float x = Mathf.Max(MinimalMvpActionPanelMargin, Screen.width - MinimalMvpActionPanelWidth - MinimalMvpActionPanelMargin);
            return new Rect(x, MinimalMvpActionPanelMargin, MinimalMvpActionPanelWidth, MinimalMvpActionPanelHeight);
        }

        private void DrawMinimalMvpActionPanel()
        {
            if (!MinimalMvpActionGuiVisible)
            {
                return;
            }

            MinimalMvpActionPanelLabels labels = MinimalMvpActionPanelPresenter.BuildLabels(
                (key, fallback) => GetLocalizedString(key, fallback),
                GetSelectedMvpStructureNameKey(),
                _selectedMvpStructureId,
                GetSelectedMvpRunPostureNameKey());
            GUIStyle compactBox = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(6, 6, 4, 4)
            };
            GUIStyle compactLabel = new GUIStyle(GUI.skin.label)
            {
                clipping = TextClipping.Clip,
                wordWrap = false,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(2, 2, 0, 0)
            };
            GUIStyle compactButton = new GUIStyle(GUI.skin.button)
            {
                margin = new RectOffset(0, 0, 1, 1),
                padding = new RectOffset(4, 4, 1, 1)
            };
            GUILayoutOption labelHeight = GUILayout.Height(MinimalMvpActionPanelLabelHeight);
            GUILayoutOption buttonHeight = GUILayout.Height(MinimalMvpActionPanelButtonHeight);

            GUILayout.BeginArea(GetMinimalMvpActionPanelRect(), compactBox);
            GUILayout.Label(labels.Title, compactLabel, labelHeight);
            GUILayout.Label(labels.SelectedStructureLabel, compactLabel, labelHeight);
            GUILayout.Label(labels.PostureLabel, compactLabel, labelHeight);
            GUILayout.Label(labels.PreviewText, compactLabel, labelHeight);
            if (GUILayout.Button(labels.ManaGeneratorSelection, compactButton, buttonHeight))
            {
                SelectMvpStructure(StructureSimulationPass.ManaGeneratorBasicId);
            }
            if (GUILayout.Button(labels.HeatScrubberSelection, compactButton, buttonHeight))
            {
                SelectMvpStructure(StructureSimulationPass.HeatScrubberBasicId);
            }
            if (GUILayout.Button(labels.RiskLabSelection, compactButton, buttonHeight))
            {
                SelectMvpStructure(StructureSimulationPass.RiskLabBasicId);
            }
            if (GUILayout.Button(labels.CautiousPosture, compactButton, buttonHeight))
            {
                SelectMvpRunPosture(RunPostureResolver.CautiousId);
            }
            if (GUILayout.Button(labels.BalancedPosture, compactButton, buttonHeight))
            {
                SelectMvpRunPosture(RunPostureResolver.BalancedId);
            }
            if (GUILayout.Button(labels.GreedyPosture, compactButton, buttonHeight))
            {
                SelectMvpRunPosture(RunPostureResolver.GreedyId);
            }
            GUILayout.Label(GetLocalizedString(_diagnosticsVisible
                ? "ui.mvp_view.diagnostics_mode.status"
                : "ui.mvp_view.player_mode.status"), compactLabel, labelHeight);
            if (GUILayout.Button(labels.PlacementButton, compactButton, buttonHeight))
            {
                PlaceSelectedMvpStructure();
            }

            if (GUILayout.Button(labels.RunButton, compactButton, buttonHeight))
            {
                RunOrObserveDungeon();
            }

            string diagnosticsButton = _diagnosticsVisible ? labels.HideDiagnosticsButton : labels.ShowDiagnosticsButton;
            if (GUILayout.Button(diagnosticsButton, compactButton, buttonHeight))
            {
                ToggleDiagnosticsVisibility();
                RefreshOverlayText();
            }
            GUILayout.EndArea();
        }

        private void ShowPlayerPlacementBanner()
        {
            string structureId = _selectedMvpStructureId;
            string priorStructureId = _root.GetSelectedSlotStructureId();
            bool ok = _root.TryMvpPlaceOrModifySelectedStructure(structureId, out string bannerKey);
            string newStructureId = _root.GetSelectedSlotStructureId();
            string message = _root.Content.GetString(bannerKey, bannerKey);
            string displayName = MvpPlayerFacingLabelResolver.ResolveStructureDisplayName(structureId, (key, fallback) => GetLocalizedString(key, fallback));
            _root.SetBanner(ok ? string.Format(message, displayName) : message);
            if (ok)
            {
                _mvpStructurePlacementFeedback = MvpStructurePlacementFeedbackPresenter.BuildFeedbackText(
                    priorStructureId,
                    newStructureId,
                    structureId,
                    (key, fallback) => GetLocalizedString(key, fallback));
            }
            else
            {
                _mvpStructurePlacementFeedback = string.Empty;
            }
            RefreshOverlayText();
        }

        private void ShowPlayerRunBanner()
        {
            MvpPlayerLoopSummary beforeRunSummary = _root.ResolveMvpPlayerLoopSummary();
            bool didRun = _root.SimulateMvpActiveLoopOnce(out _, _selectedMvpRunPostureId);
            MvpPlayerLoopSummary afterRunSummary = _root.ResolveMvpPlayerLoopSummary();
            _mvpRunResultFeedback = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                beforeRunSummary,
                afterRunSummary,
                didRun,
                (key, fallback) => GetLocalizedString(key, fallback),
                GetSelectedMvpRunPostureNameKey());
            _root.SetBanner(didRun
                ? _root.Content.GetString("ui.banner.run_simulated", "ui.banner.run_simulated")
                : _root.Content.GetString("ui.banner.run_sim_failed", "ui.banner.run_sim_failed"));
            RefreshOverlayText();
        }

        private void ShowPlacementBanner(string structureId)
        {
            bool ok = _root.TryPlaceSelectedStructure(structureId, out string bannerKey);
            string message = _root.Content.GetString(bannerKey, bannerKey);
            _root.SetBanner(ok ? string.Format(message, structureId) : message);
        }

        private static bool IsAllowedMvpStructure(string structureId)
        {
            return structureId == StructureSimulationPass.ManaGeneratorBasicId ||
                   structureId == StructureSimulationPass.HeatScrubberBasicId ||
                   structureId == StructureSimulationPass.RiskLabBasicId;
        }

        private static bool IsAllowedMvpRunPosture(string postureId)
        {
            return postureId == RunPostureResolver.CautiousId ||
                   postureId == RunPostureResolver.BalancedId ||
                   postureId == RunPostureResolver.GreedyId;
        }

        private static string GetMvpRunPostureNameKey(string postureId)
        {
            switch (postureId)
            {
                case RunPostureResolver.CautiousId:
                    return MinimalMvpActionPanelPresenter.CautiousPostureKey;
                case RunPostureResolver.GreedyId:
                    return MinimalMvpActionPanelPresenter.GreedyPostureKey;
                case RunPostureResolver.BalancedId:
                default:
                    return MinimalMvpActionPanelPresenter.BalancedPostureKey;
            }
        }

        private static string GetMvpSelectionNameKey(string structureId)
        {
            switch (structureId)
            {
                case StructureSimulationPass.HeatScrubberBasicId:
                    return MinimalMvpActionPanelPresenter.HeatScrubberSelectionKey;
                case StructureSimulationPass.RiskLabBasicId:
                    return MinimalMvpActionPanelPresenter.RiskLabSelectionKey;
                case StructureSimulationPass.ManaGeneratorBasicId:
                default:
                    return MinimalMvpActionPanelPresenter.ManaGeneratorSelectionKey;
            }
        }
    }
}
