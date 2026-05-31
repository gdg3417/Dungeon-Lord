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

        private const int DiagnosticsPageCount = 5;
        private const int RuntimeSummaryPage = 0;
        private const int RunDiagnosticsPage = 1;
        private const int HeatDiagnosticsPage = 2;
        private const int SystemsDiagnosticsPage = 3;
        private const int ResearchDiagnosticsPage = 4;

        private GameRoot _root;
        private bool _devPanelVisible;
        private bool _runDiagnosticsOnlyVisible;
        private int _fullDiagnosticsPage;
        private Vector2 _devPanelScrollPosition;

        public int FullDiagnosticsPageNumber => _fullDiagnosticsPage + 1;

        public void Bind(GameRoot root)
        {
            _root = root;
        }

        public void CycleFullDiagnosticsPage()
        {
            _fullDiagnosticsPage = (_fullDiagnosticsPage + 1) % DiagnosticsPageCount;
        }

        public void ToggleRunDiagnosticsFocus()
        {
            _runDiagnosticsOnlyVisible = !_runDiagnosticsOnlyVisible;
        }

        public void RefreshOverlayText()
        {
            if (_root == null || overlayText == null)
            {
                return;
            }

            overlayText.text = BuildOverlayText();
        }

        private void Update()
        {
            if (_root == null)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                _devPanelVisible = !_devPanelVisible;
            }
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            {
                ToggleRunDiagnosticsFocus();
            }
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                CycleFullDiagnosticsPage();
            }

            RefreshOverlayText();
        }

        private string BuildOverlayText()
        {
            var builder = new StringBuilder();
            AppendHeader(builder);

            if (_runDiagnosticsOnlyVisible)
            {
                AppendRunDiagnostics(builder, includeBreakdownAndFeedback: false, includeHeatDiagnostics: true);
                return builder.ToString();
            }

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
            }

            return builder.ToString();
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
        }

        private string GetLocalizedString(string key)
        {
            return _root.Content != null ? _root.Content.GetString(key, key) : key;
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

        private void ShowPlacementBanner(string structureId)
        {
            bool ok = _root.TryPlaceSelectedStructure(structureId, out string bannerKey);
            string message = _root.Content.GetString(bannerKey, bannerKey);
            _root.SetBanner(ok ? string.Format(message, structureId) : message);
        }
    }
}
