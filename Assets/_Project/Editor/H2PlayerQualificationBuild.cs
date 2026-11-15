#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DungeonBuilder.M0.EditorTools;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class H2PlayerQualificationBuild
{
    [Serializable] private sealed class Receipt
    {
        public string Product, Result, Editor, Backend, Platform, Define;
        public string[] Scenes;
        public int Errors;
        public bool Development;
    }

    public static void Build()
    {
        var args = Environment.GetCommandLineArgs();
        string Arg(string name)
        {
            int index = Array.IndexOf(args, name);
            if (index < 0 || index == args.Length - 1) throw new ArgumentException(name);
            return args[index + 1];
        }
        string product = Arg("-h2Product"), directory = Path.GetFullPath(Arg("-h2Output"));
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../TestResults/qualification-supervisor"));
        if (!Regex.IsMatch(product, @"\ADL B3B2 Smoke Two Floor Q[0-9a-f]{8}\z") ||
            PlayerSettings.companyName != "gdg3417" || Path.GetFileName(directory) != "build" ||
            Path.GetDirectoryName(Path.GetDirectoryName(directory)) != root ||
            !Directory.Exists(directory) || Directory.Exists(Path.Combine(directory, "player")))
            throw new InvalidOperationException("h2.qualification.build_isolation");

        DevelopmentBuildUtility.ValidateEditModeTestsEditorGuarded();
        DevelopmentBuildUtility.ValidateBuildSupport(BuildTarget.StandaloneWindows64);
        DevelopmentBuildUtility.EnsureActiveBuildTarget(BuildTarget.StandaloneWindows64);
        string[] scenes = DevelopmentBuildUtility.GetEnabledScenePathsFromEditorBuildSettings().ToArray();
        DevelopmentBuildUtility.ValidateBootstrapOnlySceneAllowlist(scenes);
        const string symbol = "DUNGEON_LORD_H2_QUALIFICATION";
        string oldProduct = PlayerSettings.productName;
        try {
            PlayerSettings.productName = product;
            var build = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = scenes, locationPathName = Path.Combine(directory, "player", "Dungeon Lord.exe"),
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development,
                extraScriptingDefines = new[] { symbol }
            });
            string receipt = JsonUtility.ToJson(new Receipt {
                Product = product, Result = build.summary.result.ToString(), Editor = Application.unityVersion,
                Backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone).ToString(),
                Platform = build.summary.platform.ToString(), Define = symbol, Errors = (int)build.summary.totalErrors,
                Development = (build.summary.options & BuildOptions.Development) != 0, Scenes = scenes
            }, true);
            using var stream = new FileStream(Path.Combine(directory, "build-report.json"), FileMode.CreateNew, FileAccess.Write);
            using var writer = new StreamWriter(stream);
            writer.Write(receipt);
            if (build.summary.result != BuildResult.Succeeded || build.summary.totalErrors != 0)
                throw new InvalidOperationException("h2.qualification.build_failed");
        } finally {
            PlayerSettings.productName = oldProduct;
        }
    }
}
#endif
