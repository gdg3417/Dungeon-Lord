#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace DungeonBuilder.M0.Tests
{

    public abstract class PhaseSevenA4ProductionSceneTests
    {
        private string filename;
        private TextAsset disposableConfig;
        private ProductionDungeonController controller;
        private Touchscreen simulatedTouch;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (!Application.isPlaying) yield return new EnterPlayMode();
            if (GameRoot.Instance != null) { UnityEngine.Object.Destroy(GameRoot.Instance.gameObject); yield return null; }
            filename = "phase7a4-scene-" + Guid.NewGuid().ToString("N") + ".json";
            SceneManager.sceneLoaded += Isolate;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
            SceneManager.sceneLoaded -= Isolate;
            for (int i = 0; i < 120 && GameRoot.Instance?.Save == null; i++) yield return null;
            var root = GameRoot.Instance; Assert.That(root?.Save, Is.Not.Null);
            var placed = root.SaveService.ExecuteCanonicalMutation(root.Save,
                DetachedCanonicalMutationRequest.Place("placement.category.room", "placement.option.room.basic", null, null, null));
            Assert.That(placed.IsSuccess, Is.True, placed.Reason);
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0]; var room = floor.Layout.Rooms[0];
            var content = root.SaveService.ExecuteCanonicalMutation(root.Save,
                DetachedCanonicalMutationRequest.Place("placement.category.monster", "placement.option.monster.skeleton",
                    room.RoomInstanceId, floor.FloorInstanceId, new TileCoordinate(0, 0)));
            Assert.That(content.IsSuccess, Is.True, content.Reason);
            controller = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();
            for (int i = 0; i < 30 && controller.World == null; i++) yield return null;
            Assert.That(controller.World, Is.Not.Null);
            root.enabled = false; // Stable test HUD/economy; runtime presentation remains active.
            yield return null;
        }
        private void Isolate(Scene scene, LoadSceneMode mode)
        {
            var root = UnityEngine.Object.FindFirstObjectByType<GameRoot>();
            var config = JsonUtility.FromJson<BuildConfig>(root.buildConfigJson.text);
            config.save.fileName = filename;
            disposableConfig = new TextAsset(JsonUtility.ToJson(config)); root.buildConfigJson = disposableConfig;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= Isolate;
            if (simulatedTouch != null) InputSystem.RemoveDevice(simulatedTouch);
            simulatedTouch = null;
            if (controller != null) UnityEngine.Object.Destroy(controller.gameObject);
            if (!Application.isPlaying) yield return new EnterPlayMode();
            if (GameRoot.Instance != null) UnityEngine.Object.Destroy(GameRoot.Instance.gameObject);
            if (disposableConfig != null) UnityEngine.Object.Destroy(disposableConfig);
            yield return null;
            if (filename != null)
                foreach (string path in Directory.GetFiles(Application.persistentDataPath, filename + "*")) File.Delete(path);
        }
        [UnityTest]
        public IEnumerator ActualSceneNormalEditMoveInvalidSaveDiscardAndTextModes()
        {
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(controller.GetComponent<UIDocument>().visualTreeAsset, Is.Not.Null);
            Assert.That(controller.World.GridVisible, Is.False);
            foreach (string id in new[] { "totalMana", "usableMana", "manaRate", "heat" })
                Assert.That(ui.Q<Label>(id).text, Is.Not.Empty);
            controller.EnterEdit(); Assert.That(controller.World.GridVisible, Is.True);
            Assert.That(ui.Q<Label>("capacity").text, Does.Contain("remaining"));
            var floor = GameRoot.Instance.Save.validatedCanonicalSpatialState.Floors[0]; var room = floor.Layout.Rooms[0];
            var definition = GameRoot.Instance.ProductionSpatialContent.Catalog.Rooms.Single(r => r.RoomDefinitionId == room.RoomDefinitionId);
            Assert.That(RoomLocalCoordinateTransform.TryToFloor(new TileCoordinate(0, 0), definition.GrossFootprint,
                room.Anchor, room.Orientation, out var start), Is.True);
            controller.TapWorld(start); Assert.That(ui.Q("contextSheet").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            controller.BeginMove(); Assert.That(controller.IsMoving, Is.True);
            controller.TapWorld(new TileCoordinate(-100, -100)); Assert.That(controller.Draft.CommandCount, Is.Zero);
            Assert.That(RoomLocalCoordinateTransform.TryToFloor(new TileCoordinate(1, 1), definition.GrossFootprint,
                room.Anchor, room.Orientation, out var target), Is.True);
            controller.TapWorld(target); Assert.That(controller.Draft.CommandCount, Is.EqualTo(1));
            Assert.That(ui.Q<Button>("save").enabledSelf, Is.False);
            yield return null;
            Assert.That(controller.Draft.Durability, Is.EqualTo(DraftDurability.Acknowledged));
            Assert.That(ui.Q<Button>("save").enabledSelf, Is.True); Assert.That(ui.Q<Button>("discard").enabledSelf, Is.True);
            foreach (var size in new[] { DungeonTextSize.Small, DungeonTextSize.Default, DungeonTextSize.Large })
            { controller.SetTextSize(size); Assert.That(ui.Q<Label>("totalMana").style.fontSize.value.value,
                Is.EqualTo(controller.presentationPolicy.Text(size))); }
            Assert.That(controller.Discard(), Is.True); Assert.That(controller.World.GridVisible, Is.False);
            Assert.That(GameRoot.Instance.Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments[0].RoomLocalPosition,
                Is.EqualTo(new TileCoordinate(0, 0)));
            controller.EnterEdit(); controller.TapWorld(start); controller.BeginMove(); controller.TapWorld(target);
            yield return null;
            Assert.That(controller.Commit(), Is.True);
            Assert.That(controller.World.GridVisible, Is.False);
            Assert.That(GameRoot.Instance.Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments[0].RoomLocalPosition,
                Is.EqualTo(new TileCoordinate(1, 1)));
        }
        [UnityTest]
        public IEnumerator RepresentativeLayoutsKeepChromeInsideSafeRootAndCaptureEvidence()
        {
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            controller.EnterEdit();
            foreach (var size in new[] { new Vector2Int(1080, 1920), new Vector2Int(1920, 1080), new Vector2Int(1536, 2048) })
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x, size.y);
                for (int i = 0; i < 40; i++) yield return null;
                var safe = new Rect(32, 40, Screen.width - 64, Screen.height - 120);
                controller.ApplySafeArea(safe, new Vector2(Screen.width, Screen.height));
                controller.SetTextSize(DungeonTextSize.Large);
                for (int i = 0; i < 40; i++) yield return null;
                Assert.That(new Vector2Int(Screen.width, Screen.height), Is.EqualTo(size));
                Rect rootBounds = controller.SafeRoot.worldBound;
                Assert.That(rootBounds.width, Is.GreaterThan(0));
                foreach (string id in new[] { "topChrome", "bottomChrome", "editActions" })
                {
                    Rect rect = ui.Q(id).worldBound;
                    Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(rootBounds.xMin - 1));
                    Assert.That(rect.xMax, Is.LessThanOrEqualTo(rootBounds.xMax + 1));
                    Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(rootBounds.yMin - 1));
                    Assert.That(rect.yMax, Is.LessThanOrEqualTo(rootBounds.yMax + 1));
                }
                foreach (string id in new[] { "save", "discard", "reset", "totalMana", "usableMana", "manaRate", "heat", "capacity" })
                {
                    Rect rect = ui.Q(id).worldBound;
                    Assert.That(rect.width, Is.GreaterThan(0), id); Assert.That(rect.height, Is.GreaterThan(0), id);
                    Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(rootBounds.xMin - 1), id);
                    Assert.That(rect.xMax, Is.LessThanOrEqualTo(rootBounds.xMax + 1), id);
                    Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(rootBounds.yMin - 1), id);
                    Assert.That(rect.yMax, Is.LessThanOrEqualTo(rootBounds.yMax + 1), id);
                }
                var point = ui.Q<Button>("discard").worldBound.center;
                Assert.That(controller.IsChrome(new Vector2(point.x, Screen.height - point.y)), Is.True);
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("phase7a4-edit-large-" + size.x + "x" + size.y + ".png");
                yield return null; yield return null;
            }
            Assert.That(controller.Discard(), Is.True);
        }

        [UnityTest]
        public IEnumerator LongerLocalizationWrapsWithoutLosingCriticalActions()
        {
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(1080, 1920);
            var content = GameRoot.Instance.Content;
            var strings = (System.Collections.Generic.Dictionary<string, string>)typeof(ContentService)
                .GetField("_stringMap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(content);
            foreach (string key in new[] { "ui.dungeon.hud.total", "ui.dungeon.hud.usable", "ui.dungeon.hud.rate", "ui.dungeon.hud.heat" })
                strings[key] = "A much longer localized metric label that needs room to wrap across several lines: {0}";
            controller.EnterEdit(); controller.SetTextSize(DungeonTextSize.Large);
            for (int i = 0; i < 40; i++) yield return null;
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            foreach (string id in new[] { "totalMana", "usableMana", "manaRate", "heat" })
            {
                var label = ui.Q<Label>(id);
                Assert.That(label.text.Length, Is.GreaterThan(80));
                Assert.That(label.worldBound.height, Is.GreaterThan(controller.presentationPolicy.LargeText * 2));
                Assert.That(label.worldBound.yMax, Is.LessThanOrEqualTo(ui.Q("topChrome").worldBound.yMax));
            }
            foreach (string id in new[] { "save", "discard", "reset" })
                Assert.That(ui.Q<Button>(id).worldBound.yMax, Is.LessThanOrEqualTo(controller.SafeRoot.worldBound.yMax));
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("phase7a4-long-localization-1080x1920.png");
            yield return null; yield return null;
            Assert.That(controller.Discard(), Is.True);
        }

        // Invoked by the PlayMode qualification adapter; EditMode coroutines do not
        // advance the MonoBehaviour device-read loop reliably.
        public IEnumerator InputSystemChromeOriginDoesNotLeakAndTwoTouchesZoomWithoutDraftWrites()
        {
            controller.EnterEdit();
            for (int i = 0; i < 10; i++) yield return null;
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var camera = controller.GetComponentInChildren<Camera>();
            simulatedTouch = InputSystem.AddDevice<Touchscreen>();
            Vector2 chrome = ScreenPoint(ui.Q<Label>("status").worldBound.center);
            Vector2 center = ScreenPoint(ui.Q("viewport").worldBound.center);
            float originalSize = camera.orthographicSize; Vector3 originalPosition = camera.transform.position;
            Assert.That(controller.IsChrome(chrome), Is.True); Assert.That(controller.IsChrome(center), Is.False);
            Touch(1, chrome, InputTouchPhase.Began); yield return null; yield return null;
            Touch(1, center, InputTouchPhase.Moved); yield return null; yield return null;
            Touch(1, center, InputTouchPhase.Ended); yield return null; yield return null;
            Assert.That(camera.orthographicSize, Is.EqualTo(originalSize)); Assert.That(camera.transform.position, Is.EqualTo(originalPosition));
            Assert.That(controller.Draft.CommandCount, Is.Zero);
            Touch(1, center + Vector2.left * 20, InputTouchPhase.Began);
            Touch(2, center + Vector2.right * 20, InputTouchPhase.Began); yield return null; yield return null;
            Assert.That(simulatedTouch.touches.Count(t => t.press.isPressed), Is.EqualTo(2), "Both Input System contacts must be active");
            Assert.That(controller.IsChrome(center + Vector2.left * 20, 1), Is.False, "Primary contact starts in viewport");
            Assert.That(controller.IsChrome(center + Vector2.right * 20, 2), Is.False, "Secondary contact starts in viewport");
            Touch(1, center + Vector2.left * 100, InputTouchPhase.Moved);
            Touch(2, center + Vector2.right * 100, InputTouchPhase.Moved); yield return null; yield return null;
            Assert.That(simulatedTouch.touches.Where(t => t.press.isPressed).Select(t => t.position.ReadValue()),
                Is.EquivalentTo(new[] { center + Vector2.left * 100, center + Vector2.right * 100 }));
            Assert.That(camera.orthographicSize, Is.LessThan(originalSize));
            Touch(1, center + Vector2.left * 100, InputTouchPhase.Ended);
            Touch(2, center + Vector2.right * 100, InputTouchPhase.Ended); yield return null; yield return null;
            Assert.That(controller.Draft.CommandCount, Is.Zero); Assert.That(controller.Draft.AcknowledgedSequence, Is.Zero);
            Assert.That(controller.Discard(), Is.True);
        }

        private static Vector2 ScreenPoint(Vector2 panelPoint) => new Vector2(panelPoint.x, Screen.height - panelPoint.y);
        private void Touch(int id, Vector2 position, InputTouchPhase phase) => InputSystem.QueueStateEvent(simulatedTouch,
            new TouchState { touchId = id, position = position, phase = phase });
    }
}
#endif
