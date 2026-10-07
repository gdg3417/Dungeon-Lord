#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DungeonBuilder.M0.EditorTools
{
    public static class ProductionDungeonScreenshots
    {
        // Uses Unity's existing Game View size API via reflection; no visual regression package.
        public static void SetGameViewSize(int width, int height)
        {
            var assembly = typeof(Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            var kind = assembly.GetType("UnityEditor.GameViewSizeType");
            var size = Activator.CreateInstance(sizeType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null, new[] { Enum.Parse(kind, "FixedResolution"), (object)width, height, "Phase7A4" }, null);
            var type = group.GetType(); type.GetMethod("AddCustomSize").Invoke(group, new[] { size });
            int count = (int)type.GetMethod("GetTotalCount").Invoke(group, null);
            var view = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            view.GetType().GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, count - 1);
        }
        public static void Capture(string filename)
        {
            string root = Path.GetFullPath("TestResults/phase7a4-screenshots"); Directory.CreateDirectory(root);
            string path = Path.Combine(root, Path.GetFileName(filename));
            ScreenCapture.CaptureScreenshot(path);
        }
    }
}
#endif
