using UnityEngine;
using TMPro;

namespace DungeonBuilder.M0
{
    public class BootstrapOverlay : MonoBehaviour
    {
        [Header("UI")]
        public TMP_Text overlayText;

        private GameRoot _root;
        private bool _devPanelVisible;

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

            if (Input.GetKeyDown(KeyCode.F1))
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
            string hint = _root.DevPanelEnabled ? "F1 toggles Dev Panel" : string.Empty;

            string combined =
                build + "\n" +
                state + "\n" +
                pending + "\n" +
                gate + "\n" +
                kpi + "\n" +
                heat + "\n" +
                (string.IsNullOrEmpty(banner) ? "" : ("\nBanner:\n" + banner + "\n")) +
                (string.IsNullOrEmpty(hint) ? "" : ("\n" + hint));

            overlayText.text = combined;
        }

        private void OnGUI()
        {
            if (_root == null || !_root.DevPanelEnabled || !_devPanelVisible)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10, 120, 360, 240), GUI.skin.box);
            GUILayout.Label("Dev Panel");

            if (GUILayout.Button("Save Now"))
            {
                _root.SaveService.Save(_root.Save, SaveReason.ManualDev);
                _root.SetBanner("Saved (Dev).");
            }

            if (GUILayout.Button("Delete Save"))
            {
                _root.SaveService.DeleteSave(out string banner);
                _root.SetBanner(banner);
            }

            if (GUILayout.Button("Clear Banner"))
            {
                _root.SetBanner(string.Empty);
            }

            if (GUILayout.Button("Toggle Online/Offline"))
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
                    _root.SetBanner("Online restored.");
                }
            }

            if (GUILayout.Button("Toggle Verification Pending"))
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
                    _root.SetBanner("Verification cleared.");
                }
            }

            if (GUILayout.Button("Simulate +10 Mana KPI"))
            {
                _root.TrackManaGenerated(10);
                _root.SetBanner("Simulated mana tick for KPI.");
            }

            if (GUILayout.Button("Simulate +5 Heat Event"))
            {
                _root.ApplyHeatDelta(5d);
                _root.SetBanner("Applied +5 heat event.");
            }

            GUILayout.EndArea();
        }
    }
}
