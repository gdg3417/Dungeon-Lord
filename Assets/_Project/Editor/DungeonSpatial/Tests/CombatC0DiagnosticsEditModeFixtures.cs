#if UNITY_EDITOR
using NUnit.Framework;

namespace DungeonBuilder.M0.EditorTools.Tests
{
    public sealed class CombatC0DiagnosticsEditModeFixtures : DungeonBuilder.M0.Tests.EditMode.CombatC0DiagnosticsTests
    {
        // Opt-in qualification environment only; no production/default project setting changes.
        [UnityEditor.InitializeOnLoadMethod]
        private static void ConfigureQualificationImportWorkers()
        {
            if (!System.Array.Exists(System.Environment.GetCommandLineArgs(), arg => arg == "-c0DiagnosticsSerialImports")) return;
            string project = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath));
            if (project != "ui-composition-combat-c0-access-validation" &&
                project != "ui-composition-combat-c0-diagnostics-validation-r2" &&
                project != "ui-composition-combat-c0-diagnostics-validation-warm")
                throw new System.InvalidOperationException("qualification.project.invalid");
            const string key = "qualification.c0.original_import_workers";
            int original = UnityEditor.SessionState.GetInt(key, -1);
            if (original < 0)
            {
                original = UnityEditor.EditorUserSettings.desiredImportWorkerCount;
                UnityEditor.SessionState.SetInt(key, original);
            }
            UnityEditor.EditorUserSettings.desiredImportWorkerCount = 0;
            UnityEditor.EditorApplication.quitting += () => {
                UnityEditor.EditorUserSettings.desiredImportWorkerCount = original;
                UnityEditor.SessionState.EraseInt(key);
            };
        }
        [TestCase(false)]
        [TestCase(true)]
        public void FocusedDiagnosticsKeepHistoricalPresentation(bool combatC0)
        {
            var fixture = new DungeonBuilder.Tests.EditMode.BootstrapOverlayPagingTests();
            fixture.SetUp();
            try { fixture.FocusedDiagnosticsPreserveHistoricalOverflowAndIgnoreStalePaging(combatC0); }
            finally { fixture.TearDown(); }
        }
    }
}
#endif
