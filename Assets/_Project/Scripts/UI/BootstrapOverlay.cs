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

        private GameRoot _root;
        private bool _devPanelVisible;
        private Vector2 _devPanelScrollPosition;

        public void Bind(GameRoot root)
        {
            _root = root;
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

            if (overlayText == null)
            {
                return;
            }

            string banner = _root.BannerMessage;
            string build = _root.BuildLine;
            string state = _root.StateLine;
            string pending = _root.PendingStateLine;
            string gate = _root.GateStatusLine;
            string kpi = _root.KpiLine;
            string heat = _root.HeatLine;
            string tick = _root.TickLine;
            string mana = _root.ManaLine;
            string save = _root.SaveLine;
            string pause = _root.PauseLine;
            string run = _root.RunLine;
            string runHistory = _root.RunHistoryLine;
            string hint = _root.DevPanelEnabled
                ? _root.Content.GetString("ui.dev.hint.toggle_panel", "ui.dev.hint.toggle_panel")
                : string.Empty;
            string structureState = _root.Content != null
                ? string.Format(
                    _root.Content.GetString("ui.dev.structure_status", "ui.dev.structure_status"),
                    _root.SelectedFloorIndex,
                    _root.SelectedSlotIndex,
                    _root.GetSelectedSlotStructureId(),
                    _root.Save != null && _root.Save.structureRuntime != null && _root.Save.structureRuntime.IsHeatCrisisActive
                )
                : string.Empty;

            string combined =
                build + "\n" +
                state + "\n" +
                pending + "\n" +
                gate + "\n" +
                kpi + "\n" +
                heat + "\n" +
                tick + "\n" +
                mana + "\n" +
                save + "\n" +
                pause + "\n" +
                run + "\n" +
                runHistory + "\n" +
                structureState + "\n" +
                (string.IsNullOrEmpty(banner)
                    ? ""
                    : ("\n" + _root.Content.GetString("ui.dev.banner.heading", "ui.dev.banner.heading") + ":\n" + banner + "\n")) +
                (string.IsNullOrEmpty(hint) ? "" : ("\n" + hint));

            overlayText.text = combined;
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
