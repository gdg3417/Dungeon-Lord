#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests
{
    public sealed class LivingDungeonTwoFloorProductionProfileSceneTests
    {
        [UnityTest, Timeout(7200000)]
        public IEnumerator LivingDungeonTwoFloorEditorProductionProfile() => MatchedProfile("two-floor");
        [UnityTest, Timeout(7200000)]
        public IEnumerator LivingDungeonSingleFloorEditorProductionProfile() => MatchedProfile("owner-inactive");
        private IEnumerator MatchedProfile(string selectedVariant)
        {
            // Entering play reloads the domain. Create captured fixture objects
            // only afterward, so the test runner need not serialize them.
            if (!Application.isPlaying) {
                UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                    UnityEditor.SceneManagement.NewSceneMode.Single);
                yield return new EnterPlayMode();
            }
            // Build fresh production-content inputs through the canonical test
            // writer. Their only difference is Floor 2's activation state.
            new EditMode.LivingDungeonTwoFloorTests().ExportProductionTwoFloorStandaloneInputsUsingApprovedContentAndTiming();
            string directory = Path.GetFullPath("TestResults/living-dungeon-two-floor-20261020/inputs");
            foreach (string variant in new[] { selectedVariant }) {
                string manifestPath = Directory.GetFiles(directory, variant + "-*.manifest.json")
                    .OrderByDescending(File.GetLastWriteTimeUtc).First();
                var manifest = JsonUtility.FromJson<TwoFloorSeedManifest>(File.ReadAllText(manifestPath));
                if (GameRoot.Instance != null) { UnityEngine.Object.Destroy(GameRoot.Instance.gameObject); yield return null; }
                string savePath = Path.Combine(Application.persistentDataPath, manifest.SaveFileName);
                Assert.That(manifest.Product, Is.EqualTo(Application.productName));
                Assert.That(File.Exists(savePath), Is.False, "Preserve previous test saves");
                File.Copy(manifest.SeedPath, savePath, false);
                TextAsset isolated = null;
                void Isolate(Scene scene, LoadSceneMode mode) {
                    var root = GameRoot.Instance; Assert.That(root.SaveService, Is.Null);
                    var config = JsonUtility.FromJson<BuildConfig>(root.buildConfigJson.text);
                    config.save.fileName = manifest.SaveFileName;
                    root.buildConfigJson = isolated = new TextAsset(JsonUtility.ToJson(config));
                }
                SceneManager.sceneLoaded += Isolate;
                var report = new GameRoot.ForegroundProfileReport();
                try {
                    yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
                    for (int i = 0; i < 120 && GameRoot.Instance?.Save == null; i++) yield return null;
                    var root = GameRoot.Instance; Assert.That(root?.Save, Is.Not.Null);
                    root.enabled = false;
                    var operation = root.ProfileForeground(report);
                    while (true) {
                        bool more; object current = null;
                        try { more = operation.MoveNext(); if (more) current = operation.Current; }
                        catch (Exception error) { report.Failure = error.ToString(); break; }
                        if (!more) break;
                        yield return current;
                    }
                } finally {
                    SceneManager.sceneLoaded -= Isolate;
                    string path = Path.GetFullPath("TestResults/living-dungeon-two-floor-20261020/editor-" + variant + "-" + Guid.NewGuid().ToString("N") + ".profile.json");
                    using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
                    using (var writer = new StreamWriter(stream)) writer.Write(JsonUtility.ToJson(report, true));
                    TestContext.WriteLine(path);
                    if (GameRoot.Instance != null) UnityEngine.Object.Destroy(GameRoot.Instance.gameObject);
                    if (isolated != null) UnityEngine.Object.Destroy(isolated);
                }
                Assert.That(report.Passed, Is.True, report.Failure);
                yield return null;
            }
        }
    }
}
#endif
