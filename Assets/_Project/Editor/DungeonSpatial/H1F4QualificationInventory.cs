#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace DungeonBuilder.M0.EditorTools
{
    // Source-pinned discovery only. No test is executed and no gameplay/save,
    // profiler configuration or test selection is changed by this collector.
    public static class H1F4QualificationInventory
    {
        [Serializable] public sealed class Case { public string Id, Name, UniqueName, State, SkipReason; }
        [Serializable] public sealed class Report { public string Product, UnityVersion, Project; public Case[] EditMode, PlayMode; }
        static TestRunnerApi api;
        public static void Capture()
        {
            try {
                var args = Environment.GetCommandLineArgs();
                int index = Array.IndexOf(args, "-h1F4Inventory");
                if (!Application.isBatchMode || !Application.productName.StartsWith("Dungeon Lord B3B2 Smoke H1F4 ", StringComparison.Ordinal) ||
                    index < 0 || index + 1 >= args.Length) throw new InvalidOperationException("h1f4.inventory.isolation_required");
                string path = Path.GetFullPath(args[index + 1]);
                string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string prefix = Path.Combine(project, "TestResults", "qualification-supervisor") + Path.DirectorySeparatorChar;
                if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(path) != "inventory.json" ||
                    new DirectoryInfo(Path.GetDirectoryName(path)).Name != "inventory" || File.Exists(path))
                    throw new InvalidOperationException("h1f4.inventory.output_invalid");
                var report = new Report { Product = Application.productName, UnityVersion = Application.unityVersion, Project = project };
                api = ScriptableObject.CreateInstance<TestRunnerApi>();
                api.RetrieveTestList(TestMode.EditMode, edit => Guard(() => {
                    report.EditMode = Cases(edit);
                    api.RetrieveTestList(TestMode.PlayMode, play => Guard(() => {
                        report.PlayMode = Cases(play);
                        if (report.EditMode.Length == 0 || report.PlayMode.Length == 0) throw new InvalidOperationException("h1f4.inventory.empty");
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(report, true) + "\n");
                        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) {
                            stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
                        }
                        UnityEngine.Object.DestroyImmediate(api);
                        EditorApplication.Exit(0);
                    }));
                }));
            } catch (Exception error) { Failed(error); }
        }
        static Case[] Cases(ITestAdaptor root)
        {
            var result = new List<Case>();
            void Visit(ITestAdaptor node) {
                if (!node.IsSuite) result.Add(new Case { Id = node.Id, Name = node.FullName, UniqueName = node.UniqueName,
                    State = node.RunState.ToString(), SkipReason = node.SkipReason });
                foreach (var child in node.Children ?? Enumerable.Empty<ITestAdaptor>()) Visit(child);
            }
            Visit(root);
            if (result.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != result.Count)
                throw new InvalidOperationException("h1f4.inventory.duplicate_identity");
            return result.OrderBy(c => c.Name, StringComparer.Ordinal).ThenBy(c => c.Id, StringComparer.Ordinal).ToArray();
        }
        static void Guard(Action action) { try { action(); } catch (Exception error) { Failed(error); } }
        static void Failed(Exception error) { Debug.LogError("H1-F4 test inventory failed: " + error); EditorApplication.Exit(2); }
    }
}
#endif
