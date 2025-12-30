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
            string hint = _root.DevPanelEnabled ? "F1 toggles Dev Panel" : string.Empty;

            string combined =
                build + "\n" +
                state + "\n" +
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

            GUILayout.EndArea();
        }
    }
}

