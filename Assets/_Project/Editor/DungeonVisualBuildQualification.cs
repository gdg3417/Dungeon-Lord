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
            => BuildWindowsAt("Builds/Development/Windows/Dungeon Lord.exe");
        // Retain the owner's earlier qualified player while producing one correction build.
        public static void BuildRoomContentUatWindows()
            => BuildWindowsAt("Builds/Development/Windows-room-content-uat/Dungeon Lord.exe");
        public static void BuildCompactPlacementWindows()
            => BuildWindowsAt("Builds/Development/Windows-placement-compact/Dungeon Lord.exe");
        public static void BuildMapReassignmentWindows()
            => BuildWindowsAt("Builds/Development/Windows-map-reassignment/Dungeon Lord.exe");
        public static void BuildLootReassignmentWindows()
            => BuildWindowsAt("Builds/Development/Windows-loot-reassignment/Dungeon Lord.exe");
        public static void BuildContentMultiplicityWindows()
            => BuildWindowsAt("Builds/Development/Windows-content-multiplicity/Dungeon Lord.exe");
        public static void BuildLivingDungeonPhaseAWindows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-phase-a/Dungeon Lord.exe");
        public static void BuildLivingDungeonPhaseAReviewWindows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-phase-a-review/Dungeon Lord.exe");
        public static void BuildLivingDungeonPhaseB1Windows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-phase-b1/Dungeon Lord.exe");
        public static void BuildLivingDungeonPhaseB1ExportLimitWindows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-phase-b1-export-limit/Dungeon Lord.exe");
        public static void BuildLivingDungeonPhaseB2Windows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-phase-b2/Dungeon Lord.exe");
        public static void BuildVisualRunReplayWindows()
            => BuildWindowsAt("Builds/Development/Windows-visual-run-replay/Dungeon Lord.exe");
        public static void BuildVisualRunFeedbackWindows()
            => BuildWindowsAt("Builds/Development/Windows-visual-run-feedback/Dungeon Lord.exe");
        public static void BuildCombatC0PrerequisiteWindows()
            => BuildWindowsAt("Builds/Development/Windows-combat-c0-prerequisite/Dungeon Lord.exe");
        public static void BuildCombatC0IntegrationWindows()
            => BuildWindowsAt("Builds/Development/Windows-combat-c0-integration/Dungeon Lord.exe");
        public static void BuildCombatC1Windows()
            => BuildWindowsAt("Builds/Development/Windows-combat-c1/Dungeon Lord.exe");
        public static void BuildCombatC1ReadabilityWindows()
            => BuildWindowsAt("Builds/Development/Windows-combat-c1-readability-20261016/Dungeon Lord.exe");
        public static void BuildCombatC1RpgWindows()
            => BuildWindowsAt("Builds/Development/Windows-combat-c1-rpg-20261016/Dungeon Lord.exe");
        public static void BuildCombatC1RpgFinalWindows()
            => BuildWindowsAt("Builds/Development/Windows-combat-c1-rpg-final-20261016/Dungeon Lord.exe");
        public static void BuildCombatC1DeathVisibilityWindows()
            => BuildWindowsAt("Builds/Development/Windows-combat-c1-death-20261016/Dungeon Lord.exe");
        public static void BuildCombatC2Windows()
            => BuildWindowsAt("Builds/Development/Windows-combat-c2-20261016/Dungeon Lord.exe");
        public static void BuildCombatC2ReserveUiWindows()
            => BuildWindowsAt("Builds/Development/Windows-combat-c2-reserve-ui-20261016/Dungeon Lord.exe");
        public static void BuildLivingDungeonB3AWindows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-b3a-20261017/Dungeon Lord.exe");
        public static void BuildLivingDungeonB3B1Windows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-b3b1-20261018/Dungeon Lord.exe");
        public static void BuildLivingDungeonB3B1SmokeFixWindows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-b3b1-20261018-smoke-fix/Dungeon Lord.exe");
        public static void BuildLivingDungeonB3B2Windows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-b3b2-20261018/Dungeon Lord.exe");
        public static void BuildLivingDungeonTwoFloorWindows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-two-floor-20261020/Dungeon Lord.exe");
        public static void BuildLivingDungeonTwoFloorFinalWindows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-two-floor-20261021-final/Dungeon Lord.exe");
        public static void BuildLivingDungeonB3B2SmokeFixWindows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-b3b2-20261018-smoke-fix/Dungeon Lord.exe");
        public static void BuildLivingDungeonB3B2ReviewWindows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-b3b2-20261019-review/Dungeon Lord.exe");
        public static void BuildLivingDungeonB3B2LiveBeforeWindows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-b3b2-20261019-live-before/Dungeon Lord.exe");
        public static void BuildLivingDungeonB3B2LiveBeforeProfileFixWindows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-b3b2-20261019-live-before-profile-fix/Dungeon Lord.exe");
        public static void BuildLivingDungeonB3B2LiveAfterWindows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-b3b2-20261019-live-after/Dungeon Lord.exe");
        public static void BuildLivingDungeonB3B2LiveFinalWindows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-b3b2-20261019-live-final/Dungeon Lord.exe");
        public static void BuildLivingDungeonB3B2LiveMarkerWindows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-b3b2-20261019-live-marker/Dungeon Lord.exe");
        public static void BuildLivingDungeonB3B2LiveMarkerRenderedWindows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-b3b2-20261019-live-marker-rendered/Dungeon Lord.exe");
        public static void BuildLivingDungeonB3B2LiveMarkerRenderedFinalWindows()
            => BuildWindowsAt("Builds/Development/Windows-living-dungeon-b3b2-20261019-live-marker-rendered-final/Dungeon Lord.exe");
        private static void BuildWindowsAt(string outputPath)
        {
            DevelopmentBuildUtility.BuildDevelopment(BuildTarget.StandaloneWindows64, outputPath);
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
            string folder = Path.GetDirectoryName(outputPath);
            File.WriteAllLines(Path.Combine(folder, "packed-dungeon-art.txt"), dependencies);
            File.WriteAllLines(Path.Combine(folder, "packed-source-assets.txt"), packed);
            File.WriteAllLines(Path.Combine(folder, "build-messages.txt"), report.steps.SelectMany(s => s.messages)
                .Select(m => m.type + ": " + m.content));
        }
    }
}
#endif
