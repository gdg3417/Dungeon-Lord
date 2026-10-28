#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using SceneClock = DungeonBuilder.M0.Tests.PhaseSevenA4ProductionSceneTests.SceneClock;
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
            string directory = EditMode.LivingDungeonTwoFloorTests.ExportProductionTwoFloorInputs();
            string manifestPath = Directory.GetFiles(directory, "two-floor-*.manifest.json").OrderByDescending(File.GetLastWriteTimeUtc).First();
            var manifest = JsonUtility.FromJson<TwoFloorSeedManifest>(File.ReadAllText(manifestPath));
            if (GameRoot.Instance != null) { UnityEngine.Object.Destroy(GameRoot.Instance.gameObject); yield return null; }
            Assert.That(manifest.Product, Is.EqualTo(Application.productName));
            string fileName = "phase7a4-scene-" + Guid.NewGuid().ToString("N") + ".json";
            string path = Path.Combine(Application.persistentDataPath, fileName);
            var clock = SeedClock(manifest);
            Assert.That(File.Exists(path), Is.False); File.Copy(manifest.SeedPath, path, false);
            var isolation = new WaitingSceneIsolation(fileName, clock);
            SceneManager.sceneLoaded += isolation.Loaded;
            try {
                yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
                yield return AwaitRecoveryReady();
                var root = GameRoot.Instance; Assert.That(root?.Save, Is.Not.Null); root.enabled = false;
                var report = new GameRoot.ForegroundSmokeReport();
                var operation = root.QualifyTwoFloor("two-floor-wait", report, manifestPath, clock.Advance);
                while (operation.MoveNext()) yield return operation.Current;
                Assert.That(report.Passed, Is.True, report.Failure);
                var controller = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();
                var ui = controller.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement;
                var floors = root.Save.validatedCanonicalSpatialState.Floors;
                long accepted = root.Save.livingDungeon.Scheduler.AcceptedThrough;
                var checkpoints = root.Save.livingDungeon.Parties.Select(p => p.CheckpointIdentity).ToArray();
                double balance = root.Save.structureRuntime.ManaReserve;
                root.ApplyPauseState(false);
                var update = typeof(GameRoot).GetMethod("UpdateForeground", BindingFlags.Instance | BindingFlags.NonPublic);
                for (int pump = 0; pump < 20 && (root.TimeService.RecoveryClockActive || root.TimeService.HasDeferredRecovery); pump++)
                    update.Invoke(root, null);
                Assert.That(root.TimeService.RecoveryClockActive || root.TimeService.HasDeferredRecovery, Is.False);
                Assert.That(root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(accepted));
                CollectionAssert.AreEqual(checkpoints, root.Save.livingDungeon.Parties.Select(p => p.CheckpointIdentity));
                Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(balance));
                var original = root.SaveService.CanonicalSession.GetCurrentBytes();
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(720, 1280), new Vector2Int(360, 640) })
                    foreach (var textSize in new[] { DungeonTextSize.Small, DungeonTextSize.Default, DungeonTextSize.Large }) {
                        EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x, size.y); controller.SetTextSize(textSize);
                        foreach (var floor in floors) {
                            controller.SelectFloor(floor.FloorInstanceId);
                            for (int frame = 0; frame < 8; frame++) {
                                root.ApplyPauseState(false); yield return null;
                            }
                            Assert.That(Screen.width, Is.EqualTo(size.x)); Assert.That(Screen.height, Is.EqualTo(size.y));
                            var party = root.ForegroundActivity.ForFloor(floor.FloorInstanceId);
                            Assert.That(party.RunId, Is.EqualTo("run-" + (floor.FloorIndex == 0 ? report.WaitingSequence : report.BlockerSequence)));
                            var activity = ui.Q<UnityEngine.UIElements.Label>("foregroundActivity");
                            string heading = string.Format(root.Content.GetString("ui.living.active_format", string.Empty),
                                party.RunId, party.Step?.FloorIndex + 1 ?? 1,
                                root.Content.GetString("ui.living.pending." + (party.AnnouncedAction?.ToString() ?? "RoomEntry"), string.Empty));
                            Assert.That(activity.text, Does.Contain(heading));
                            Assert.That(ui.Q<Label>("foregroundPartyLabel").tooltip, Is.EqualTo(party.RunId));
                            if (floor.FloorIndex == 0) Assert.That(activity.text, Does.Contain(root.Content.GetString("ui.living.waiting", string.Empty)));
                            var map = ui.Q("mapArea");
                            TestContext.WriteLine($"Two-floor waiting layout {size} {textSize} floor={floor.FloorIndex} activity={activity.worldBound} map={map.worldBound} top={ui.Q("topChrome").worldBound} bottom={ui.Q("bottomChrome").worldBound}");
                            Assert.That(map.worldBound.height, Is.GreaterThan(80), "Retain an interactive map viewport");
                            var activityScroll = ui.Q<ScrollView>("foregroundActivityScroll");
                            var visibleActivity = activityScroll != null && activityScroll.resolvedStyle.display != DisplayStyle.None
                                ? activityScroll.contentViewport.worldBound : activity.worldBound;
                            Assert.That(visibleActivity.yMax, Is.LessThanOrEqualTo(map.worldBound.yMin + 1), "Waiting feedback must not cover the map");
                            if (floor.FloorIndex == 0) Assert.That(visibleActivity.height, Is.GreaterThan(activity.resolvedStyle.fontSize), "Keep the waiting status visible");
                            if (activityScroll != null && activityScroll.resolvedStyle.display != DisplayStyle.None && activity.worldBound.height > visibleActivity.height + 1) {
                                activityScroll.verticalScroller.value = activityScroll.verticalScroller.highValue;
                                yield return null; yield return null;
                                Assert.That(activity.worldBound.yMax, Is.LessThanOrEqualTo(activityScroll.contentViewport.worldBound.yMax + 1), "Full feedback remains reachable by scrolling");
                                activityScroll.scrollOffset = Vector2.zero; yield return null; yield return null;
                            }
                            Assert.That(ui.worldBound.Contains(ui.Q("sessionActions").worldBound.center), Is.True);
                            var buttons = ui.Q("floors").Query<UnityEngine.UIElements.Button>().ToList(); Assert.That(buttons.Count, Is.EqualTo(2));
                            var selected = buttons.Single(button => button.userData as string == floor.FloorInstanceId);
                            Assert.That(selected.ClassListContains("selected-floor"), Is.True);
                            Assert.That(buttons[0].ClassListContains("floor-party-waiting"), Is.True);
                            Assert.That(buttons[1].ClassListContains("floor-party-active"), Is.True);
                            if (!party.GeometryCompatible) Assert.That(controller.World.ForegroundPartyVisible, Is.False);
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

        [Serializable] private sealed class SeedEnvelope { public int schemaVersion; public SeedPrimary primary; }
        [Serializable] private sealed class SeedPrimary { public long lastSavedUtcUnix; public SeedTime timeProvenance; }
        [Serializable] private sealed class SeedTime { public int AccountedFraction100ns; }
        private static SceneClock SeedClock(TwoFloorSeedManifest manifest)
        {
            byte[] bytes = File.ReadAllBytes(manifest.SeedPath);
            Assert.That(Gameplay.RunSimulation.RunCheckpointCodec.Identity(bytes), Is.EqualTo(manifest.SeedSha256));
            var seed = JsonUtility.FromJson<SeedEnvelope>(System.Text.Encoding.UTF8.GetString(bytes));
            Assert.That(seed.schemaVersion, Is.EqualTo(CanonicalSaveSchemaVersions.CurrentWritableTarget));
            Assert.That(seed.primary?.timeProvenance, Is.Not.Null);
            Assert.That(seed.primary.lastSavedUtcUnix, Is.GreaterThan(0));
            // Provider initialization only. SaveService independently proves the
            // complete canonical save/context before it may capture recovery R.
            return new SceneClock { Now = new UnixTimePoint(seed.primary.lastSavedUtcUnix, seed.primary.timeProvenance.AccountedFraction100ns) };
        }
        private static IEnumerator AwaitRecoveryReady()
        {
            for (int frame = 0; frame < 120; frame++) {
                var root = GameRoot.Instance;
                if (root?.Save != null) {
                    typeof(GameRoot).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(root, new object[] { true });
                    root.ApplyPauseState(false);
                    if (root.GameplayServicesInitializedForTests && !root.TimeService.RecoveryClockActive && !root.TimeService.HasDeferredRecovery)
                        yield break;
                }
                yield return null;
            }
            Assert.Fail("The isolated scene did not complete recovery: " + GameRoot.Instance?.BannerMessage);
        }

        // Unlike a captured local function, this setup object is allocated only
        // after EnterPlayMode and is not lost when Unity reloads the iterator.
        private sealed class WaitingSceneIsolation
        {
            private readonly string fileName;
            private readonly SceneClock clock;
            internal TextAsset Config;
            internal WaitingSceneIsolation(string fileName, SceneClock clock) { this.fileName = fileName; this.clock = clock; }
            internal void Loaded(Scene scene, LoadSceneMode mode)
            {
                var root = GameRoot.Instance; Assert.That(root, Is.Not.Null);
                Assert.That(root.SaveService, Is.Null);
                var config = JsonUtility.FromJson<BuildConfig>(root.buildConfigJson.text);
                config.save.fileName = fileName;
                root.buildConfigJson = Config = new TextAsset(JsonUtility.ToJson(config));
                root.InjectSceneRecoveryClockForTests(clock, clock.Monotonic);
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
            string directory = EditMode.LivingDungeonTwoFloorTests.ExportProductionTwoFloorInputs();
            foreach (string variant in new[] { selectedVariant }) {
                string manifestPath = Directory.GetFiles(directory, variant + "-*.manifest.json")
                    .OrderByDescending(File.GetLastWriteTimeUtc).First();
                var manifest = JsonUtility.FromJson<TwoFloorSeedManifest>(File.ReadAllText(manifestPath));
                if (GameRoot.Instance != null) { UnityEngine.Object.Destroy(GameRoot.Instance.gameObject); yield return null; }
                string fileName = "phase7a4-scene-" + Guid.NewGuid().ToString("N") + ".json";
                string savePath = Path.Combine(Application.persistentDataPath, fileName);
                var clock = SeedClock(manifest);
                Assert.That(manifest.Product, Is.EqualTo(Application.productName));
                Assert.That(File.Exists(savePath), Is.False, "Preserve previous test saves");
                File.Copy(manifest.SeedPath, savePath, false);
                TextAsset isolated = null;
                void Isolate(Scene scene, LoadSceneMode mode) {
                    var root = GameRoot.Instance; Assert.That(root.SaveService, Is.Null);
                    var config = JsonUtility.FromJson<BuildConfig>(root.buildConfigJson.text);
                    config.save.fileName = fileName;
                    root.buildConfigJson = isolated = new TextAsset(JsonUtility.ToJson(config));
                    root.InjectSceneRecoveryClockForTests(clock, clock.Monotonic);
                }
                SceneManager.sceneLoaded += Isolate;
                var report = new GameRoot.ForegroundProfileReport();
                try {
                    yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
                    yield return AwaitRecoveryReady();
                    var root = GameRoot.Instance; Assert.That(root?.Save, Is.Not.Null);
                    root.enabled = false;
                    EditorTools.ProductionDungeonScreenshots.SetGameViewSize(1280, 720);
                    UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>().SetTextSize(DungeonTextSize.Default);
                    for (int frame = 0; frame < 8; frame++) yield return null;
                    var operation = root.ProfileForeground(report, clock.Advance);
                    while (true) {
                        bool more; object current = null;
                        try { more = operation.MoveNext(); if (more) current = operation.Current; }
                        catch (Exception error) { report.Failure = error.ToString(); break; }
                        if (!more) break;
                        yield return current;
                    }
                } finally {
                    SceneManager.sceneLoaded -= Isolate;
                    string path = Path.Combine(directory, "editor-" + variant + ".profile.json");
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
