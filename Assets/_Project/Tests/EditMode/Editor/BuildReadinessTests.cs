using System;
using System.IO;
using DungeonBuilder.M0.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class BuildReadinessTests
    {
        [Test]
        public void PlayerSceneAllowlist_ContainsOnlyBootstrap()
        {
            string[] scenes = DevelopmentBuildUtility.GetEnabledScenePathsFromEditorBuildSettings();
            CollectionAssert.AreEqual(new[] { DevelopmentBuildUtility.BootstrapScenePath }, scenes);
            Assert.DoesNotThrow(() => DevelopmentBuildUtility.ValidateBootstrapOnlySceneAllowlist(scenes));
        }

        [Test]
        public void PlayerSceneAllowlist_ExcludesSampleScene()
        {
            string[] scenes = DevelopmentBuildUtility.GetEnabledScenePathsFromEditorBuildSettings();
            CollectionAssert.DoesNotContain(scenes, DevelopmentBuildUtility.SampleScenePath);
        }

        [Test]
        public void SceneValidation_FailsWhenBootstrapMissing()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                DevelopmentBuildUtility.ValidateBootstrapOnlySceneAllowlist(new[] { DevelopmentBuildUtility.SampleScenePath }));
            StringAssert.Contains(DevelopmentBuildUtility.BootstrapScenePath, ex.Message);
        }

        [Test]
        public void ProjectIdentityValidation_RejectsTemplateDefaults()
        {
            Assert.Throws<InvalidOperationException>(() =>
                DevelopmentBuildUtility.ValidateProjectIdentity("DefaultCompany", "Dungeon Lord", "com.DefaultCompany.2D-URP"));
        }

        [Test]
        public void ProjectIdentityValidation_AcceptsDungeonLordIdentity()
        {
            Assert.DoesNotThrow(() =>
                DevelopmentBuildUtility.ValidateProjectIdentity("gdg3417", "Dungeon Lord", "com.gdg3417.dungeonlord"));
        }

        [TestCase(true, true, true)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, false)]
        public void DevelopmentDiagnosticsPolicy_RequiresDevelopmentBuildAndFeatureFlag(bool developmentBuild, bool flagEnabled, bool expected)
        {
            Assert.AreEqual(expected, DevelopmentDiagnosticsPolicy.AreDiagnosticsEnabled(developmentBuild, flagEnabled));
        }


        [Test]
        public void ActiveBuildTargetValidation_RequiresExactRequestedTarget()
        {
            Assert.True(DevelopmentBuildUtility.IsRequestedBuildTargetActive(BuildTarget.StandaloneWindows64, BuildTarget.StandaloneWindows64));
            Assert.True(DevelopmentBuildUtility.IsRequestedBuildTargetActive(BuildTarget.Android, BuildTarget.Android));
            Assert.False(DevelopmentBuildUtility.IsRequestedBuildTargetActive(BuildTarget.Android, BuildTarget.StandaloneWindows64));
            Assert.False(DevelopmentBuildUtility.IsRequestedBuildTargetActive(BuildTarget.StandaloneWindows64, BuildTarget.Android));
        }

        [Test]
        public void ActiveBuildTargetValidation_ReportsCommandLineBuildTargetNames()
        {
            Assert.AreEqual("StandaloneWindows64", DevelopmentBuildUtility.GetCommandLineBuildTarget(BuildTarget.StandaloneWindows64));
            Assert.AreEqual("Android", DevelopmentBuildUtility.GetCommandLineBuildTarget(BuildTarget.Android));
        }

        [Test]
        public void BuildReportFormatting_ContainsRequiredFields()
        {
            string report = DevelopmentBuildUtility.FormatBuildReport(null, "StandaloneWindows64", "Builds/Development/Windows/Dungeon Lord.exe", true, new[] { DevelopmentBuildUtility.BootstrapScenePath });
            string[] fields =
            {
                "utcTimestamp", "unityVersion", "targetPlatform", "applicationVersion", "developmentBuild",
                "includedScenes", "outputPath", "buildResult", "totalBuildSize", "errorCount", "warningCount"
            };

            foreach (string field in fields)
            {
                StringAssert.Contains($"\"{field}\"", report);
            }
        }

        [Test]
        public void BootstrapScene_HasNoMissingScriptsAndInputModuleUsesCurrentActionsAsset()
        {
            string sceneText = File.ReadAllText(DevelopmentBuildUtility.BootstrapScenePath);
            string inputActionsGuid = AssetDatabase.AssetPathToGUID("Assets/InputSystem_Actions.inputactions");
            StringAssert.Contains($"m_ActionsAsset: {{fileID: -944628639613478452, guid: {inputActionsGuid}, type: 3}}", sceneText);
            Assert.False(sceneText.Contains("guid: ca9f5fa95ffab41fb9a615ab714db018"), "Bootstrap scene must not reference the stale input actions GUID.");

            var scene = EditorSceneManager.OpenScene(DevelopmentBuildUtility.BootstrapScenePath, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    Assert.NotNull(component, $"Missing script or unresolved required component reference under {root.name}.");
                }
            }
        }
    }
}
