#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DungeonBuilder.M0.EditorTools
{
    public static class DevelopmentBuildUtility
    {
        public const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        public const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        public const string CompanyName = "gdg3417";
        public const string ProductName = "Dungeon Lord";
        public const string ApplicationIdentifier = "com.gdg3417.dungeonlord";

        [MenuItem("Dungeon Lord/Build/Windows 64-bit Development Build")]
        public static void BuildWindowsDevelopmentMenu() => BuildWindowsDevelopment();

        [MenuItem("Dungeon Lord/Build/Android Development APK")]
        public static void BuildAndroidDevelopmentMenu() => BuildAndroidDevelopmentApk();

        public static void BuildWindowsDevelopment()
        {
            BuildDevelopment(BuildTarget.StandaloneWindows64, "Builds/Development/Windows/Dungeon Lord.exe");
        }

        public static void BuildAndroidDevelopmentApk()
        {
            BuildDevelopment(BuildTarget.Android, "Builds/Development/Android/DungeonLord-development.apk");
        }

        public static void BuildDevelopment(BuildTarget target, string outputPath)
        {
            ValidateBuildSupport(target);
            ValidateProjectIdentity(PlayerSettings.companyName, PlayerSettings.productName, PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone));
            if (target == BuildTarget.Android)
            {
                ValidateProjectIdentity(PlayerSettings.companyName, PlayerSettings.productName, PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            }

            string[] scenes = GetEnabledScenePathsFromEditorBuildSettings();
            ValidateBootstrapOnlySceneAllowlist(scenes);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            string reportJson = FormatBuildReport(report, target.ToString(), outputPath, true, scenes);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(outputPath), "build-report.json"), reportJson);

            if (report == null || report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Development build failed for {target}. See {Path.GetDirectoryName(outputPath)}/build-report.json for details.");
            }
        }

        public static string[] GetEnabledScenePathsFromEditorBuildSettings()
        {
            return EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        }

        public static void ValidateBootstrapOnlySceneAllowlist(string[] enabledScenes)
        {
            if (enabledScenes == null || enabledScenes.Length == 0 || !string.Equals(enabledScenes[0], BootstrapScenePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Player build scene allowlist must start with required scene: {BootstrapScenePath}.");
            }

            string[] unexpected = enabledScenes.Where(scene => !string.Equals(scene, BootstrapScenePath, StringComparison.Ordinal)).ToArray();
            if (unexpected.Length > 0)
            {
                throw new InvalidOperationException("Unexpected enabled player scenes: " + string.Join(", ", unexpected));
            }
        }

        public static void ValidateProjectIdentity(string companyName, string productName, string applicationIdentifier)
        {
            if (!string.Equals(companyName, CompanyName, StringComparison.Ordinal) ||
                !string.Equals(productName, ProductName, StringComparison.Ordinal) ||
                !string.Equals(applicationIdentifier, ApplicationIdentifier, StringComparison.Ordinal) ||
                companyName.Contains("DefaultCompany") ||
                applicationIdentifier.Contains("DefaultCompany") ||
                applicationIdentifier.Contains("2D-URP"))
            {
                throw new InvalidOperationException("Project identity must be gdg3417 / Dungeon Lord / com.gdg3417.dungeonlord with no Unity template defaults.");
            }
        }

        public static void ValidateBuildSupport(BuildTarget target)
        {
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
            {
                throw new InvalidOperationException($"Unity build support is not installed for {target} ({group}).");
            }
        }

        public static string FormatBuildReport(BuildReport report, string targetPlatform, string outputPath, bool developmentBuild, string[] includedScenes)
        {
            BuildSummary summary = report != null ? report.summary : default;
            var builder = new StringBuilder();
            builder.AppendLine("{");
            AppendJson(builder, "utcTimestamp", DateTime.UtcNow.ToString("o"), true);
            AppendJson(builder, "unityVersion", Application.unityVersion, true);
            AppendJson(builder, "targetPlatform", targetPlatform, true);
            AppendJson(builder, "applicationVersion", Application.version, true);
            builder.AppendLine($"  \"developmentBuild\": {developmentBuild.ToString().ToLowerInvariant()},");
            builder.AppendLine("  \"includedScenes\": [" + string.Join(", ", (includedScenes ?? Array.Empty<string>()).Select(scene => "\"" + Escape(scene) + "\"")) + "],");
            AppendJson(builder, "outputPath", outputPath, true);
            AppendJson(builder, "buildResult", report != null ? summary.result.ToString() : "Unknown", true);
            builder.AppendLine($"  \"totalBuildSize\": {summary.totalSize},");
            builder.AppendLine($"  \"errorCount\": {summary.totalErrors},");
            builder.AppendLine($"  \"warningCount\": {summary.totalWarnings}");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AppendJson(StringBuilder builder, string name, string value, bool comma)
        {
            builder.Append("  \"").Append(name).Append("\": \"").Append(Escape(value)).Append('"');
            if (comma) builder.Append(',');
            builder.AppendLine();
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
#endif
