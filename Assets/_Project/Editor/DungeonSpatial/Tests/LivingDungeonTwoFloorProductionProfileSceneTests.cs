#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0.Tests
{
    public sealed class LivingDungeonTwoFloorProductionProfileSceneTests
    {
        [UnityTest, Timeout(7200000)]
        public IEnumerator LivingDungeonTwoFloorEditorProductionProfile() => MatchedProfile("two-floor");
        [UnityTest, Timeout(7200000)]
        public IEnumerator LivingDungeonSingleFloorEditorProductionProfile() => MatchedProfile("owner-inactive");
        [UnityTest, Timeout(300000)]
        public IEnumerator LivingDungeonTwoFloorWaitingSelectedFloorLayoutsPreserveCanonicalState()
        {
            if (!Application.isPlaying) {
                UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                    UnityEditor.SceneManagement.NewSceneMode.Single);
                yield return new EnterPlayMode();
            }
            Assert.That(Application.isPlaying, Is.True, "Load production gameplay only after EnterPlayMode");
            new EditMode.LivingDungeonTwoFloorTests().ExportProductionTwoFloorStandaloneInputsUsingApprovedContentAndTiming();
            string directory = Path.GetFullPath("TestResults/living-dungeon-two-floor-20261020/inputs");
            string manifestPath = Directory.GetFiles(directory, "two-floor-*.manifest.json").OrderByDescending(File.GetLastWriteTimeUtc).First();
            var manifest = JsonUtility.FromJson<TwoFloorSeedManifest>(File.ReadAllText(manifestPath));
            if (GameRoot.Instance != null) { UnityEngine.Object.Destroy(GameRoot.Instance.gameObject); yield return null; }
            Assert.That(manifest.Product, Is.EqualTo(Application.productName));
            string path = Path.Combine(Application.persistentDataPath, manifest.SaveFileName);
            Assert.That(File.Exists(path), Is.False); File.Copy(manifest.SeedPath, path, false);
            var isolation = new WaitingSceneIsolation(manifest.SaveFileName);
            SceneManager.sceneLoaded += isolation.Loaded;
            try {
                yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
                for (int frame = 0; frame < 120 && GameRoot.Instance?.Save == null; frame++) yield return null;
                var root = GameRoot.Instance; Assert.That(root?.Save, Is.Not.Null); root.enabled = false;
                var report = new GameRoot.ForegroundSmokeReport();
                var operation = root.QualifyTwoFloor("two-floor-wait", report, manifestPath);
                while (operation.MoveNext()) yield return operation.Current;
                Assert.That(report.Passed, Is.True, report.Failure);
                var controller = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();
                var ui = controller.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement;
                var floors = root.Save.validatedCanonicalSpatialState.Floors;
                var original = root.SaveService.CanonicalSession.GetCurrentBytes();
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(720, 1280), new Vector2Int(360, 640) })
                    foreach (var textSize in new[] { DungeonTextSize.Small, DungeonTextSize.Default, DungeonTextSize.Large }) {
                        EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x, size.y); controller.SetTextSize(textSize);
                        foreach (var floor in floors) {
                            controller.SelectFloor(floor.FloorInstanceId);
                            for (int frame = 0; frame < 8; frame++) {
                                root.TimeService.OnResume(); root.TimeService.SetForegroundFocus(true); yield return null;
                            }
                            Assert.That(Screen.width, Is.EqualTo(size.x)); Assert.That(Screen.height, Is.EqualTo(size.y));
                            var party = root.ForegroundActivity.ForFloor(floor.FloorInstanceId);
                            Assert.That(party.RunId, Is.EqualTo("run-" + (floor.FloorIndex == 0 ? report.WaitingSequence : report.BlockerSequence)));
                            var activity = ui.Q<UnityEngine.UIElements.Label>("foregroundActivity");
                            Assert.That(activity.text, Does.Contain(party.RunId));
                            if (floor.FloorIndex == 0) Assert.That(activity.text, Does.Contain(root.Content.GetString("ui.living.waiting", string.Empty)));
                            var map = ui.Q("mapArea");
                            Assert.That(map.worldBound.height, Is.GreaterThan(80), "Retain an interactive map viewport");
                            Assert.That(activity.worldBound.yMax, Is.LessThanOrEqualTo(map.worldBound.yMin + 1), "Waiting feedback must not cover the map");
                            Assert.That(ui.worldBound.Contains(ui.Q("sessionActions").worldBound.center), Is.True);
                            var buttons = ui.Q("floors").Query<UnityEngine.UIElements.Button>().ToList(); Assert.That(buttons.Count, Is.EqualTo(2));
                            var selected = buttons.Single(button => button.userData as string == floor.FloorInstanceId);
                            Assert.That(selected.ClassListContains("selected-floor"), Is.True);
                            Assert.That(buttons[0].ClassListContains("floor-party-waiting"), Is.True);
                            Assert.That(buttons[1].ClassListContains("floor-party-active"), Is.True);
                            if (!party.GeometryCompatible) Assert.That(controller.World.ForegroundPartyVisible, Is.False);
                            TestContext.WriteLine($"Two-floor waiting layout {size} {textSize} floor={floor.FloorIndex} activity={activity.worldBound} map={map.worldBound}");
                            if (textSize == DungeonTextSize.Large) {
                                EditorTools.ProductionDungeonScreenshots.Capture($"two-floor-waiting-{size.x}x{size.y}-floor{floor.FloorIndex + 1}.png");
                                yield return null; yield return null;
                            }
                            CollectionAssert.AreEqual(original, root.SaveService.CanonicalSession.GetCurrentBytes());
                        }
                    }
            } finally {
                SceneManager.sceneLoaded -= isolation.Loaded;
                if (GameRoot.Instance != null) UnityEngine.Object.Destroy(GameRoot.Instance.gameObject);
                if (isolation.Config != null) UnityEngine.Object.Destroy(isolation.Config);
            }
        }

        // Unlike a captured local function, this setup object is allocated only
        // after EnterPlayMode and is not lost when Unity reloads the iterator.
        private sealed class WaitingSceneIsolation
        {
            private readonly string fileName;
            internal TextAsset Config;
            internal WaitingSceneIsolation(string fileName) { this.fileName = fileName; }
            internal void Loaded(Scene scene, LoadSceneMode mode)
            {
                var root = GameRoot.Instance; Assert.That(root, Is.Not.Null);
                Assert.That(root.SaveService, Is.Null);
                var config = JsonUtility.FromJson<BuildConfig>(root.buildConfigJson.text);
                config.save.fileName = fileName;
                root.buildConfigJson = Config = new TextAsset(JsonUtility.ToJson(config));
            }
        }

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
