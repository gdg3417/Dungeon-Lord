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
            string path = CapturePath(filename);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if ((new DirectoryInfo(Path.GetDirectoryName(path)).Attributes & FileAttributes.ReparsePoint) != 0 || File.Exists(path))
                throw new IOException("qualification.capture.output_exists_or_redirected");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("Qualification capture: " + path);
        }
        internal static string CapturePath(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename) || filename != Path.GetFileName(filename) ||
                filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("qualification.capture.filename_invalid");
            string export = Environment.GetEnvironmentVariable("DUNGEON_H1F4_TEST_EXPORT_ROOT");
            bool qualification = Application.productName.StartsWith("Dungeon Lord B3B2 Smoke H1F4 ", StringComparison.Ordinal);
            if (qualification || !string.IsNullOrEmpty(export)) {
                var output = H1F4QualificationArtifacts.RequireOutputRoot();
                return Path.Combine(output.FullName, "screenshots", Guid.NewGuid().ToString("N").Substring(0, 8) + "-" + filename);
            }
            // Existing manual capture location remains available, with a fresh
            // name if its requested historical capture already exists.
            string root = Path.GetFullPath("TestResults/phase7a4-screenshots");
            string path = Path.Combine(root, filename);
            return File.Exists(path) ? Path.Combine(root, Guid.NewGuid().ToString("N").Substring(0, 8) + "-" + filename) : path;
        }
    }

    // Read-only verification of the existing supervisor's artifact ownership.
    // No reservation mutation, lock substitution or independent execution owner.
    internal static class H1F4QualificationArtifacts
    {
        [Serializable] private sealed class Ownership { public string owner, stage, retention; }
        [Serializable] private sealed class Reservation { public string campaign; }
        internal static DirectoryInfo RequireOutputRoot()
        {
            string export = Environment.GetEnvironmentVariable("DUNGEON_H1F4_TEST_EXPORT_ROOT");
            if (!Application.isBatchMode || !Application.productName.StartsWith("Dungeon Lord B3B2 Smoke H1F4 ", StringComparison.Ordinal) || string.IsNullOrEmpty(export))
                throw new InvalidOperationException("qualification.capture.isolation_required");
            var output = new DirectoryInfo(Path.GetFullPath(export));
            var stage = output.Parent; var campaign = stage?.Parent; var supervisor = campaign?.Parent;
            if (output.Name != "fixture-inputs" || !output.Exists ||
                !string.Equals(supervisor?.FullName, Path.GetFullPath(Path.Combine(Application.dataPath, "../TestResults/qualification-supervisor")), StringComparison.OrdinalIgnoreCase) ||
                !Guid.TryParseExact(campaign?.Name, "N", out _))
                throw new InvalidOperationException("qualification.capture.output_invalid");
            foreach (var parent in new[] { supervisor, campaign, stage, output })
                if (!parent.Exists || (parent.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("qualification.capture.output_redirected");
            var owner = JsonUtility.FromJson<Ownership>(File.ReadAllText(Path.Combine(output.FullName, "ownership.json")));
            var reservation = JsonUtility.FromJson<Reservation>(File.ReadAllText(Path.Combine(supervisor.Parent.FullName,
                "qualification-supervisor-reservation", "active.json")));
            if (owner?.owner != "H1-F4 qualification" || owner.retention != "preserve" ||
                !string.Equals(owner.stage, stage.FullName, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(reservation?.campaign, campaign.FullName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("qualification.capture.owner_mismatch");
            return output;
        }
    }
}
#endif
