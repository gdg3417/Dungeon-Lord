#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace DungeonBuilder.M0.EditorTools
{
    /// <summary>Uses the established production build and verifies its actual packed artwork.</summary>
    public static class DungeonVisualBuildQualification
    {
        public static void BuildWindows()
        {
            DevelopmentBuildUtility.BuildWindowsDevelopment();
            var report = BuildReport.GetLatestReport();
            if (report == null || report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("production.dungeon.build_report_missing");
            var packed = report.packedAssets.SelectMany(p => p.contents).Select(p => p.sourceAssetPath)
                .Distinct().OrderBy(p => p, StringComparer.Ordinal).ToArray();
            var dependencies = AssetDatabase.GetDependencies(new[] {
                DevelopmentBuildUtility.BootstrapScenePath,
                "Assets/_Project/UI/ProductionDungeon/Visuals.asset",
                "Assets/_Project/UI/ProductionDungeon/Dungeon.uss" }, true)
                .Where(p => p.StartsWith("Assets/_Project/UI/ProductionDungeon/Art/", StringComparison.Ordinal) && p.EndsWith(".png"))
                .Distinct().OrderBy(p => p, StringComparer.Ordinal).ToArray();
            if (dependencies.Length == 0 || dependencies.Any(p => !packed.Contains(p)))
                throw new InvalidOperationException("production.dungeon.packed_art_missing:" + string.Join(",", dependencies.Where(p => !packed.Contains(p))));
            string folder = "Builds/Development/Windows";
            File.WriteAllLines(Path.Combine(folder, "packed-dungeon-art.txt"), dependencies);
            File.WriteAllLines(Path.Combine(folder, "packed-source-assets.txt"), packed);
            File.WriteAllLines(Path.Combine(folder, "build-messages.txt"), report.steps.SelectMany(s => s.messages)
                .Select(m => m.type + ": " + m.content));
        }
    }
}
#endif
