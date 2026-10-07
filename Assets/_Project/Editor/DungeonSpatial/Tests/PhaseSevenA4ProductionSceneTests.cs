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
        private int isolationCallbacks;
        private ProductionDungeonController controller;
        private Touchscreen simulatedTouch;
        private bool missingThemeWarning;
        private void RecordThemeWarning(string message, string stack, LogType type)
        { if (message.Contains("No Theme Style Sheet set to PanelSettings")) missingThemeWarning = true; }
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            missingThemeWarning = false;
            Application.logMessageReceived += RecordThemeWarning;
            if (!Application.isPlaying) yield return new EnterPlayMode();
            if (GameRoot.Instance != null) { UnityEngine.Object.Destroy(GameRoot.Instance.gameObject); yield return null; }
            filename = "phase7a4-scene-" + Guid.NewGuid().ToString("N") + ".json";
            SceneManager.sceneLoaded += Isolate;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
            SceneManager.sceneLoaded -= Isolate;
            for (int i = 0; i < 120 && GameRoot.Instance?.Save == null; i++) yield return null;
            var root = GameRoot.Instance; Assert.That(root?.Save, Is.Not.Null);
            Assert.That(Path.GetFileName(root.SaveService.SavePath), Is.EqualTo(filename), "Disposable scene save isolation");
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
            // Awake moves the fresh root to DontDestroyOnLoad before sceneLoaded. Its
            // singleton identifies that root even while departing objects await destruction.
            var root = GameRoot.Instance;
            Assert.That(root, Is.Not.Null); Assert.That(root.SaveService, Is.Null, "Override disposable save before Start boots");
            isolationCallbacks++;
            var config = JsonUtility.FromJson<BuildConfig>(root.buildConfigJson.text);
            config.save.fileName = filename;
            disposableConfig = new TextAsset(JsonUtility.ToJson(config)); root.buildConfigJson = disposableConfig;
            TestContext.WriteLine("Isolated scene boot " + isolationCallbacks + ": " + config.save.fileName);
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Application.logMessageReceived -= RecordThemeWarning;
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
        public IEnumerator RuntimeThemeAndLocalizedTextRenderInActualScene()
        {
            var invalidPanel = ScriptableObject.CreateInstance<PanelSettings>();
            var transientTheme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            try
            {
                Assert.Throws<InvalidOperationException>(() => DungeonBuilder.M0.EditorTools.ProductionDungeonAssetAuthoring.ValidateTheme(invalidPanel));
                invalidPanel.themeStyleSheet = transientTheme;
                Assert.Throws<InvalidOperationException>(() => DungeonBuilder.M0.EditorTools.ProductionDungeonAssetAuthoring.ValidateTheme(invalidPanel));
            }
            finally { UnityEngine.Object.Destroy(invalidPanel); UnityEngine.Object.Destroy(transientTheme); }
            var document = controller.GetComponent<UIDocument>();
            Assert.That(document.panelSettings, Is.Not.Null);
            DungeonBuilder.M0.EditorTools.ProductionDungeonAssetAuthoring.ValidateTheme(document.panelSettings);
            Assert.That(document.panelSettings.themeStyleSheet, Is.Not.Null);
            var dependencies = UnityEditor.AssetDatabase.GetDependencies("Assets/_Project/Scenes/Bootstrap.unity", true);
            Assert.That(dependencies, Does.Contain(DungeonBuilder.M0.EditorTools.ProductionDungeonAssetAuthoring.ThemePath));
            Assert.That(dependencies.Any(path => path.StartsWith("Assets/UI Toolkit/UnityThemes/", StringComparison.Ordinal)), Is.False);
            Assert.That(new UnityEditor.SerializedObject(document.panelSettings).FindProperty("m_DisableNoThemeWarning").boolValue, Is.False);
            var root = GameRoot.Instance;
            var hud = ProductionDungeonPresenter.Hud(root.Save, root.RunSimulationConfig, root.PassiveManaPerHourForPresentation,
                key => root.Content.GetString(key, key), System.Globalization.CultureInfo.CurrentCulture);
            var expected = new System.Collections.Generic.Dictionary<string, string> {
                { "mode", root.Content.GetString("ui.dungeon.normal", "ui.dungeon.normal") },
                { "totalMana", hud.TotalMana }, { "usableMana", hud.UsableMana },
                { "manaRate", hud.ManaPerHour }, { "heat", hud.Heat },
                { "edit", root.Content.GetString("ui.dungeon.edit", "ui.dungeon.edit") },
                { "reset", root.Content.GetString("ui.dungeon.reset", "ui.dungeon.reset") },
                { "small", root.Content.GetString("ui.dungeon.text_small", "ui.dungeon.text_small") },
                { "default", root.Content.GetString("ui.dungeon.text_default", "ui.dungeon.text_default") },
                { "large", root.Content.GetString("ui.dungeon.text_large", "ui.dungeon.text_large") } };
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(1080, 1920);
            controller.ApplySafeArea(new Rect(0, 0, 1080, 1920), new Vector2(1080, 1920));
            for (int i = 0; i < 8; i++) yield return null;
            foreach (var pair in expected)
            {
                var text = document.rootVisualElement.Q<TextElement>(pair.Key);
                Assert.That(text.text, Is.EqualTo(pair.Value), pair.Key);
                Assert.That(text.panel, Is.Not.Null, pair.Key);
                Assert.That(text.worldBound.width, Is.GreaterThan(0), pair.Key);
                Assert.That(text.worldBound.height, Is.GreaterThan(0), pair.Key);
                Assert.That(text.resolvedStyle.color.a, Is.GreaterThan(0), pair.Key);
                Assert.That(text.resolvedStyle.fontSize, Is.GreaterThan(0), pair.Key);
                Assert.That(text.resolvedStyle.unityFont != null || text.resolvedStyle.unityFontDefinition.fontAsset != null ||
                    text.resolvedStyle.unityFontDefinition.font != null, Is.True, pair.Key + " font source");
                for (VisualElement element = text; element != null; element = element.parent)
                {
                    Assert.That(element.resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None), pair.Key);
                    Assert.That(element.resolvedStyle.visibility, Is.EqualTo(Visibility.Visible), pair.Key);
                    Assert.That(element.resolvedStyle.opacity, Is.GreaterThan(0), pair.Key);
                }
            }
            Assert.That(missingThemeWarning, Is.False);
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("phase7a4-theme-normal-1080x1920.png");
            yield return null;
            LogAssert.NoUnexpectedReceived();
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
        public IEnumerator PhaseSevenA5MoveGuidancePortraitLandscapeAndCurrentDraft()
        {
            var root = GameRoot.Instance; var before = File.ReadAllBytes(root.SaveService.SavePath); double mana = root.Save.structureRuntime.ManaReserve;
            var ui = controller.GetComponent<UIDocument>().rootVisualElement; var floor = root.Save.validatedCanonicalSpatialState.Floors[0]; var room = floor.Layout.Rooms[0];
            var configured = root.ProductionSpatialContent.Catalog.Floors.Single(f => f.FloorDefinitionId == floor.FloorDefinitionId).Bounds;
            controller.EnterEdit();
            foreach (var size in new[] { new Vector2Int(1080, 1920), new Vector2Int(1920, 1080) })
            {
                int commandsBefore = controller.Draft.CommandCount;
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x, size.y);
                for (int i = 0; i < 12; i++) yield return null;
                Click(ui.Q<Button>("reset"));
                Assert.That(controller.World.GridVisible, Is.True); Assert.That(controller.World.GridTileCount, Is.EqualTo(configured.TileCount));
                Assert.That(controller.World.LegalBounds, Is.EqualTo(new Rect(configured.Minimum.X, configured.Minimum.Y, configured.Width, configured.Height)));
                var camera = controller.transform.Find("ProductionDungeonCamera").GetComponent<Camera>();
                foreach (var corner in new[] { controller.World.LegalBounds.min, controller.World.LegalBounds.max })
                { var visible = camera.WorldToViewportPoint(corner); Assert.That(visible.x, Is.InRange(0f, 1f)); Assert.That(visible.y, Is.InRange(0f, 1f)); }
                controller.TapWorld(new TileCoordinate(room.Anchor.X + 1, room.Anchor.Y + 1)); controller.BeginMove();
                var anchors = controller.World.MoveGuidanceAnchors;
                Assert.That(anchors, Does.Contain(new TileCoordinate(0, 3))); Assert.That(anchors.Contains(room.Anchor), Is.False);
                var marker = controller.World.transform.Find("MoveAnchors").GetComponent<UnityEngine.Tilemaps.Tilemap>().GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(0, 3, 0));
                Assert.That(marker, Is.Not.Null); Assert.That(marker.sprite.texture.GetPixels().Any(p => p.a == 0), Is.True);
                Assert.That(marker.sprite.texture.GetPixels().Any(p => p.a > 0), Is.True);
                Assert.That(controller.World.transform.Find("RoomSelection").GetComponent<UnityEngine.Tilemaps.Tilemap>().HasTile(new Vector3Int(0, 3, 0)), Is.False);
                var expected = root.Content.GetString("ui.dungeon.room_move_hint", null); Assert.That(ui.Q<Label>("status").text, Is.EqualTo(expected));
                Assert.That(controller.Draft.CommandCount, Is.EqualTo(commandsBefore)); AssertCanonicalUnchanged(before, mana);
                for (int i = 0; i < 8; i++) yield return null;
                CollectionAssert.AreEqual(anchors, controller.World.MoveGuidanceAnchors); Assert.That(controller.Draft.CommandCount, Is.EqualTo(commandsBefore));
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("phase7a5-guidance-" + size.x + "x" + size.y + ".png");
                yield return null; yield return null;
                controller.TapWorld(new TileCoordinate(1, 3));
                Assert.That(controller.Draft.CommandCount, Is.EqualTo(commandsBefore + 1));
                yield return null; Assert.That(controller.Draft.IsStructurallyValid, Is.False);
                Assert.That(controller.World.InvalidFootprintTileCount, Is.GreaterThan(0)); Assert.That(ui.Q<Button>("save").enabledSelf, Is.False);
                controller.BeginMove(); Assert.That(controller.World.MoveGuidanceAnchors, Does.Contain(new TileCoordinate(0, 3)));
                // Return to the unchanged valid projection between portrait/landscape checks.
                controller.TapWorld(room.Anchor); yield return null;
                Assert.That(controller.Draft.HasChanges, Is.False);
            }
            controller.BeginMove(); controller.TapWorld(controller.World.MoveGuidanceAnchors.First(a => a.Equals(new TileCoordinate(0, 3)))); yield return null;
            Assert.That(controller.Draft.IsStructurallyValid, Is.True); Assert.That(ui.Q("economics").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            AssertCanonicalUnchanged(before, mana); controller.BeginMove();
            Assert.That(controller.World.MoveGuidanceAnchors, Does.Contain(room.Anchor)); Assert.That(controller.World.MoveGuidanceAnchors.Contains(new TileCoordinate(0, 3)), Is.False);
            controller.TapWorld(new TileCoordinate(-1, -1)); yield return null;
            Assert.That(controller.World.InvalidFootprintTileCount, Is.GreaterThan(0)); Assert.That(controller.World.LegalBounds, Is.EqualTo(new Rect(0, 0, configured.Width, configured.Height)));
            Assert.That(ui.Q<Label>("status").text, Does.Contain(root.Content.GetString(StructuralEditService.OutOfBoundsReason, null)));
            controller.BeginMove(); controller.TapWorld(room.Anchor); yield return null; Assert.That(controller.Draft.IsStructurallyValid, Is.True);
            Assert.That(controller.Discard(), Is.True); Assert.That(controller.World.GridVisible, Is.False); AssertCanonicalUnchanged(before, mana);
        }

        [UnityTest]
        public IEnumerator PhaseSevenA5MoveGuidanceNoAlternativeIsLocalized()
        {
            var root = GameRoot.Instance; var original = root.SaveService.DungeonDraftContext; var catalog = root.ProductionSpatialContent.Catalog;
            catalog.Floors[0].Bounds = new RectangularFloorBounds(new TileCoordinate(0, 0), 4, 8); catalog.Floors[0].FinalFloorSpaceCapacity = 32;
            var production = new ProductionSpatialContentSnapshot(root.ProductionSpatialContent.Manifest, catalog, root.ProductionSpatialContent.Languages);
            var ctx = new DungeonDraftContext(production, root.RunSimulationConfig, original.Occupancy, root.SaveSpatialMigrationLimits, original.Compatibility);
            Assert.That(ctx.Validate(root.Save.validatedCanonicalSpatialState), Is.True);
            // Inject a valid tight authored envelope for this presentation-only test.
            typeof(ContentService).GetProperty("ProductionSpatialContent").SetValue(root.Content, production);
            typeof(ProductionDungeonController).GetField("draftContext", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(controller, ctx);
            controller.EnterEdit(); controller.TapWorld(new TileCoordinate(1, 3)); controller.BeginMove(); yield return null;
            Assert.That(controller.World.MoveGuidanceAnchors, Is.Empty);
            Assert.That(controller.GetComponent<UIDocument>().rootVisualElement.Q<Label>("status").text, Is.EqualTo(root.Content.GetString("ui.dungeon.room_move_none", null)));
            Assert.That(controller.Draft.CommandCount, Is.Zero); Assert.That(controller.World.GridTileCount, Is.EqualTo(32));
            Assert.That(controller.Discard(), Is.True);
        }

        [UnityTest]
        public IEnumerator PhaseSevenA5MoveGuidanceLongTextAndInvalidRecovery()
        {
            var root = GameRoot.Instance; var strings = (System.Collections.Generic.Dictionary<string, string>)typeof(ContentService)
                .GetField("_stringMap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(root.Content);
            strings["ui.dungeon.room_move_hint"] = "Choose a highlighted hollow diamond anchor for the selected room inside the complete outlined legal floor grid. Its required-route descendants move together while each content keeps its exact local arrangement.";
            strings[StructuralEditService.ConnectionUnavailableReason] = "This requested anchor cannot connect the room to its required route. Press Move again and choose a highlighted hollow diamond anchor inside the outlined legal floor grid to correct this attempt.";
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(1080, 1920); controller.SetTextSize(DungeonTextSize.Large);
            controller.EnterEdit(); controller.TapWorld(new TileCoordinate(1, 3)); controller.BeginMove();
            for (int i = 0; i < 40; i++) yield return null;
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(ui.Q<Label>("status").worldBound.height, Is.GreaterThan(controller.presentationPolicy.LargeText));
            Assert.That(ui.Q("viewport").worldBound.height, Is.GreaterThan(0)); Assert.That(ui.Q<Button>("discard").worldBound.height, Is.GreaterThan(0));
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("phase7a5-guidance-long-text.png"); yield return null; yield return null;
            controller.TapWorld(new TileCoordinate(1, 3)); yield return null; Assert.That(controller.Draft.IsStructurallyValid, Is.False);
            Assert.That(ui.Q<Label>("status").text, Does.Contain(strings[StructuralEditService.ConnectionUnavailableReason]));
            yield return RestartShell(); ui = controller.GetComponent<UIDocument>().rootVisualElement; Click(ui.Q<Button>("confirm")); yield return null;
            controller.TapWorld(new TileCoordinate(2, 4)); controller.BeginMove();
            Assert.That(controller.World.MoveGuidanceAnchors, Does.Contain(new TileCoordinate(0, 3))); Assert.That(controller.World.InvalidFootprintTileCount, Is.GreaterThan(0));
            controller.TapWorld(new TileCoordinate(0, 3)); yield return null; Assert.That(controller.Draft.IsStructurallyValid, Is.True); Assert.That(controller.Discard(), Is.True);
        }

        [UnityTest]
        public IEnumerator PhaseSevenA5GraphicalRoomInvalidCorrectionEconomyAndAtomicSave()
        {
            var root = GameRoot.Instance; var before = File.ReadAllBytes(root.SaveService.SavePath);
            double mana = root.Save.structureRuntime.ManaReserve;
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0]; var room = floor.Layout.Rooms[0];
            var content = JsonUtility.ToJson(floor.RoomContents);
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            controller.EnterEdit();
            // Content occupancy has explicit precedence over its containing room.
            controller.TapWorld(room.Anchor); Assert.That(controller.SelectedRoomInstanceId, Is.Null);
            controller.TapWorld(new TileCoordinate(room.Anchor.X + 1, room.Anchor.Y + 1));
            Assert.That(controller.SelectedRoomInstanceId, Is.EqualTo(room.RoomInstanceId));
            Assert.That(ui.Q<Label>("selectedName").text, Does.Contain("Room"));
            string roomName = root.ProductionSpatialContent.Languages.Single(t => t.language == "en").entries.Single(e =>
                e.key == root.ProductionSpatialContent.Catalog.Rooms.Single(d => d.RoomDefinitionId == room.RoomDefinitionId).LocalizationKey).text;
            Assert.That(ui.Q<Label>("selectedName").text, Does.Contain(roomName));
            controller.BeginMove(); Assert.That(controller.IsMoving, Is.True);
            controller.TapWorld(new TileCoordinate(0, 0)); yield return null;
            Assert.That(controller.Draft.CommandCount, Is.EqualTo(1)); Assert.That(controller.Draft.Durability, Is.EqualTo(DraftDurability.Acknowledged));
            Assert.That(controller.World.InvalidFootprintTileCount, Is.GreaterThan(0));
            Assert.That(ui.Q<Label>("status").text, Does.Contain(root.Content.GetString(StructuralEditService.FixedOverlapReason, null)));
            Assert.That(ui.Q<Button>("save").enabledSelf, Is.False); Assert.That(ui.Q("economics").style.display.value, Is.EqualTo(DisplayStyle.None));
            AssertCanonicalUnchanged(before, mana);
            controller.TapWorld(new TileCoordinate(1, 1));
            Assert.That(controller.SelectedRoomInstanceId, Is.EqualTo(room.RoomInstanceId));
            Assert.That(controller.World.InvalidFootprintTileCount, Is.GreaterThan(0));
            controller.BeginMove(); controller.TapWorld(new TileCoordinate(20, 20)); yield return null;
            Assert.That(controller.Draft.StructuralReason, Is.EqualTo(StructuralEditService.OutOfBoundsReason));
            Assert.That(controller.World.InvalidFootprintTileCount, Is.GreaterThan(0)); Assert.That(controller.World.Bounds.Contains(new Vector2(20, 20)), Is.True);
            controller.BeginMove(); controller.TapWorld(new TileCoordinate(room.Anchor.X, room.Anchor.Y + 1));
            Assert.That(ui.Q<Button>("save").enabledSelf, Is.False); yield return null;
            Assert.That(controller.Draft.IsStructurallyValid, Is.True); Assert.That(ui.Q<Button>("save").enabledSelf, Is.True);
            Assert.That(controller.World.InvalidFootprintTileCount, Is.Zero); Assert.That(ui.Q("economics").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            var price = root.SaveService.PreviewDungeonDraft(root.Save, controller.Draft); Assert.That(price.Cost, Is.GreaterThan(0));
            Assert.That(ui.Q<Label>("economics").text, Does.Contain(StructuralEconomyPresenter.FormatTransactionAmount(price.Cost)));
            AssertCanonicalUnchanged(before, mana);
            var final = controller.Draft.ReadModel.Floors[0];
            Assert.That(JsonUtility.ToJson(final.RoomContents), Is.EqualTo(content));
            Assert.That(controller.Commit(), Is.True); yield return null;
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(mana - price.Cost));
            Assert.That(root.Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms[0].Anchor, Is.EqualTo(final.Layout.Rooms[0].Anchor));
            Assert.That(controller.World.ActiveEntityCount, Is.EqualTo(floor.RoomContents.Assignments.Length));
            Assert.That(root.SaveService.CreateDungeonDraftStore().Read(), Is.Null);
        }

        [UnityTest]
        public IEnumerator PhaseSevenA5InvalidRecoveryRestoresFootprintReasonAndDiscard()
        {
            var root = GameRoot.Instance; var before = File.ReadAllBytes(root.SaveService.SavePath); double mana = root.Save.structureRuntime.ManaReserve;
            var room = root.Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms[0];
            controller.EnterEdit(); controller.TapWorld(new TileCoordinate(room.Anchor.X + 1, room.Anchor.Y + 1)); controller.BeginMove();
            controller.TapWorld(new TileCoordinate(0, 0)); yield return null;
            yield return RestartShell(); var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            Click(ui.Q<Button>("confirm")); yield return null;
            Assert.That(controller.IsEditing, Is.True); Assert.That(controller.Draft.InvalidMovements.Single().RequestedAnchor, Is.EqualTo(new TileCoordinate(0, 0)));
            Assert.That(controller.World.InvalidFootprintTileCount, Is.GreaterThan(0)); Assert.That(ui.Q<Button>("save").enabledSelf, Is.False);
            Assert.That(ui.Q<Label>("status").text, Does.Contain(root.Content.GetString(StructuralEditService.FixedOverlapReason, null)));
            AssertCanonicalUnchanged(before, mana); Assert.That(controller.Discard(), Is.True); yield return null;
            Assert.That(controller.World.InvalidFootprintTileCount, Is.Zero); AssertCanonicalUnchanged(before, mana);
        }

        [UnityTest]
        public IEnumerator PhaseSevenA5ExperimentationOriginRecoveryAndInsufficientManaKeepDraft()
        {
            var root = GameRoot.Instance; var before = File.ReadAllBytes(root.SaveService.SavePath);
            var room = root.Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms[0]; double mana = root.Save.structureRuntime.ManaReserve;
            controller.EnterEdit(); controller.TapWorld(new TileCoordinate(room.Anchor.X + 1, room.Anchor.Y + 1)); controller.BeginMove();
            controller.TapWorld(new TileCoordinate(0, 3)); yield return null;
            double cost = root.SaveService.PreviewDungeonDraft(root.Save, controller.Draft).Cost;
            controller.BeginMove(); controller.TapWorld(new TileCoordinate(0, 4)); yield return null;
            Assert.That(root.SaveService.PreviewDungeonDraft(root.Save, controller.Draft).Cost, Is.EqualTo(cost));
            controller.BeginMove(); controller.TapWorld(room.Anchor); yield return null;
            Assert.That(root.SaveService.PreviewDungeonDraft(root.Save, controller.Draft).Cost, Is.Zero); Assert.That(controller.Draft.HasChanges, Is.False);
            controller.BeginMove(); controller.TapWorld(new TileCoordinate(0, 3)); yield return null;
            yield return RestartShell(); var ui = controller.GetComponent<UIDocument>().rootVisualElement; Click(ui.Q<Button>("confirm")); yield return null;
            Assert.That(controller.Draft.ReadModel.Floors[0].Layout.Rooms[0].Anchor, Is.EqualTo(new TileCoordinate(0, 3))); AssertCanonicalUnchanged(before, mana);
            root.Save.structureRuntime.ManaReserve = 0;
            Assert.That(controller.Commit(), Is.False); Assert.That(controller.Draft.CanSave, Is.True);
            Assert.That(ui.Q<Label>("economics").text, Does.Contain("Current mana: 0."));
            Assert.That(ui.Q<Label>("status").text, Does.Contain(root.Content.GetString(DungeonBuilder.M0.Economy.StructuralEconomyService.InsufficientReason, null)));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(root.SaveService.SavePath)); Assert.That(controller.Discard(), Is.True);
        }

        [UnityTest]
        public IEnumerator PhaseSevenA5LongLocalizedRoomSheetInvalidReasonAndEconomicsRemainReadable()
        {
            var root = GameRoot.Instance;
            var strings = (System.Collections.Generic.Dictionary<string, string>)typeof(ContentService)
                .GetField("_stringMap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(root.Content);
            strings["ui.dungeon.room_selected"] = "An expanded localized room description for the selected {0}, numbered {1}, with additional explanatory words";
            strings["ui.structural.economy.cost"] = "Expanded localized movement pricing description: configured base {0} and final structural cost {1}";
            strings["ui.dungeon.room_movement_consequences"] = "Expanded final movement explanation: {0} rooms moved together; {1} route connections updated; {2} Completion Terminals moved. Each content keeps its exact room-local arrangement.";
            strings[StructuralEditService.FixedOverlapReason] = "An expanded localized explanation of the requested room overlapping an existing fixed structure. Choose another anchor to correct this draft.";
            controller.SetTextSize(DungeonTextSize.Large); controller.EnterEdit(); controller.TapWorld(new TileCoordinate(1, 3));
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(ui.Q<Label>("selectedName").text.Length, Is.GreaterThan(80));
            controller.BeginMove(); controller.TapWorld(new TileCoordinate(0, 0)); yield return null;
            controller.TapWorld(new TileCoordinate(1, 1));
            for (int i = 0; i < 8; i++) yield return null;
            Assert.That(ui.Q<Label>("status").worldBound.height, Is.GreaterThan(controller.presentationPolicy.LargeText));
            Assert.That(ui.Q<Button>("move").worldBound.height, Is.GreaterThan(0));
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("phase7a5-invalid-long-text.png");
            yield return null; yield return null;
            controller.BeginMove(); controller.TapWorld(new TileCoordinate(0, 3)); yield return null;
            for (int i = 0; i < 8; i++) yield return null;
            Assert.That(ui.Q<Label>("economics").text, Does.Contain("Expanded localized movement"));
            Assert.That(ui.Q<Label>("economics").text, Does.Contain("Expanded final movement"));
            Assert.That(ui.Q<Label>("economics").worldBound.height, Is.GreaterThan(controller.presentationPolicy.LargeText));
            Assert.That(ui.Q<Button>("save").worldBound.height, Is.GreaterThan(0));
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("phase7a5-valid-long-text.png");
            yield return null; yield return null;
            Assert.That(controller.Discard(), Is.True);
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
        [UnityTest]
        public IEnumerator ExplicitDeleteQuiescesProductionShellAndFreshBootHasNoDraft()
        { yield return ExplicitDeleteShell(false); }

        [UnityTest]
        public IEnumerator FailedExplicitDeleteStillQuiescesProductionShellWithoutFurtherWrites()
        { yield return ExplicitDeleteShell(true); }

        private IEnumerator ExplicitDeleteShell(bool failDraftDelete)
        {
            var root = GameRoot.Instance;
            var fileField = typeof(SaveService).GetField("_canonicalFileSystem",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var files = new DeleteTrackingFileSystem { Inner = (ISpatialMigrationFileSystem)fileField.GetValue(root.SaveService) };
            fileField.SetValue(root.SaveService, files);
            typeof(ProductionDungeonController).GetField("store", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).SetValue(controller, root.SaveService.CreateDungeonDraftStore());
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0]; var room = floor.Layout.Rooms[0];
            var definition = root.ProductionSpatialContent.Catalog.Rooms.Single(r => r.RoomDefinitionId == room.RoomDefinitionId);
            Assert.That(RoomLocalCoordinateTransform.TryToFloor(new TileCoordinate(0, 0), definition.GrossFootprint,
                room.Anchor, room.Orientation, out var start), Is.True);
            Assert.That(RoomLocalCoordinateTransform.TryToFloor(new TileCoordinate(1, 1), definition.GrossFootprint,
                room.Anchor, room.Orientation, out var first), Is.True);
            Assert.That(RoomLocalCoordinateTransform.TryToFloor(new TileCoordinate(2, 1), definition.GrossFootprint,
                room.Anchor, room.Orientation, out var pending), Is.True);
            controller.EnterEdit(); controller.TapWorld(start); controller.BeginMove(); controller.TapWorld(first);
            yield return null;
            Assert.That(controller.Draft.AcknowledgedSequence, Is.EqualTo(1));
            controller.BeginMove(); controller.TapWorld(pending);
            Assert.That(controller.Draft.Durability, Is.EqualTo(DraftDurability.Pending));
            string active = root.SaveService.SavePath;
            byte[] canonical = File.ReadAllBytes(active);
            string unrelated = active + ".editor-draft-not-owned.keep"; File.WriteAllText(unrelated, "unrelated test file");
            files.FailDraftDelete = failDraftDelete;
            if (failDraftDelete) LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                "Delete save failed\\. Exception: " + System.Text.RegularExpressions.Regex.Escape(TransactionalDungeonDraft.DeleteFailedReason)));
            else LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Save deleted by dev command\\."));
            Assert.That(root.TryDeleteSaveFromDevPanel(out string banner), Is.EqualTo(!failDraftDelete), banner);
            Assert.That(root.Save, Is.Null); Assert.That(root.TimeService, Is.Null);
            Assert.That(File.Exists(unrelated), Is.True);
            if (failDraftDelete) CollectionAssert.AreEqual(canonical, File.ReadAllBytes(active));
            else
            {
                Assert.That(File.Exists(active), Is.False);
                Assert.That(root.SaveService.CreateDungeonDraftStore().Read(), Is.Null);
            }
            int mutations = files.Mutations;
            string[] paths = Directory.GetFiles(Application.persistentDataPath, filename + "*").OrderBy(p => p, StringComparer.Ordinal).ToArray();
            // Qualify Update directly on success and the pre-Update lifecycle boundary
            // on failure, where the same established missing-Save state applies.
            if (!failDraftDelete) yield return null;
            InvokeController("OnApplicationPause", true); InvokeController("OnApplicationQuit");
            controller.EnterEdit(); controller.TapWorld(first); controller.BeginMove();
            Assert.That(controller.Commit(), Is.False); Assert.That(controller.Discard(), Is.False);
            for (int i = 0; i < 12; i++) yield return null;
            Assert.That(controller.IsQuiesced, Is.True); Assert.That(controller.enabled, Is.False);
            Assert.That(controller.GetComponent<UIDocument>().enabled, Is.False);
            Assert.That(controller.World.gameObject.activeSelf, Is.False);
            Assert.That(controller.Draft, Is.Null); Assert.That(controller.IsMoving, Is.False);
            Assert.That(files.Mutations, Is.EqualTo(mutations));
            CollectionAssert.AreEqual(paths, Directory.GetFiles(Application.persistentDataPath, filename + "*").OrderBy(p => p, StringComparer.Ordinal));
            root.ApplyPauseState(true); root.ApplyPauseState(false); root.ApplyApplicationQuit();
            UnityEngine.Object.Destroy(controller.gameObject); controller = null;
            yield return null;
            Assert.That(files.Mutations, Is.EqualTo(mutations));
            LogAssert.NoUnexpectedReceived();
            if (!failDraftDelete)
            {
                UnityEngine.Object.Destroy(root.gameObject); yield return null;
                UnityEngine.Object.Destroy(disposableConfig); disposableConfig = null;
                int callbacksBefore = isolationCallbacks;
                SceneManager.sceneLoaded += Isolate;
                var reload = SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
                while (!reload.isDone) yield return null;
                SceneManager.sceneLoaded -= Isolate;
                Assert.That(isolationCallbacks, Is.EqualTo(callbacksBefore + 1), "Fresh scene callback applied isolation");
                for (int i = 0; i < 120 && GameRoot.Instance?.Save == null; i++) yield return null;
                Assert.That(GameRoot.Instance.Save, Is.Not.Null); Assert.That(GameRoot.Instance.TimeService, Is.Not.Null);
                Assert.That(Path.GetFileName(GameRoot.Instance.SaveService.SavePath), Is.EqualTo(filename), "Fresh boot disposable save isolation");
                controller = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();
                for (int i = 0; i < 30 && controller.World == null; i++) yield return null;
                GameRoot.Instance.enabled = false;
                yield return null;
                Assert.That(controller.IsQuiesced, Is.False);
                Assert.That(GameRoot.Instance.SaveService.CreateDungeonDraftStore().Read(), Is.Null);
                var ui = controller.GetComponent<UIDocument>().rootVisualElement;
                Assert.That(ui.Q("modal").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                Assert.That(ui.Q<Button>("edit").enabledSelf, Is.True);
            }
        }
        private void InvokeController(string method, params object[] args) => typeof(ProductionDungeonController).GetMethod(method,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(controller, args);
        private sealed class DeleteTrackingFileSystem : ISpatialMigrationFileSystem
        {
            public ISpatialMigrationFileSystem Inner;
            public bool FailDraftDelete;
            public int Mutations;
            public bool Exists(string path) => Inner.Exists(path);
            public byte[] ReadAllBytes(string path) => Inner.ReadAllBytes(path);
            public System.Collections.Generic.IReadOnlyList<string> EnumerateFiles(string directory, string pattern, int maximum) => Inner.EnumerateFiles(directory, pattern, maximum);
            public bool IsPathContainedWithoutRedirection(string directory, string path) => Inner.IsPathContainedWithoutRedirection(directory, path);
            public void WriteAllBytesDurable(string path, byte[] bytes) { Mutations++; Inner.WriteAllBytesDurable(path, bytes); }
            public void ReplaceSameDirectoryAtomic(string source, string target) { Mutations++; Inner.ReplaceSameDirectoryAtomic(source, target); }
            public void MoveSameDirectoryAtomic(string source, string target) { Mutations++; Inner.MoveSameDirectoryAtomic(source, target); }
            public void FlushDirectory(string directory) { Mutations++; Inner.FlushDirectory(directory); }
            public void DeleteFile(string path)
            {
                Mutations++;
                if (FailDraftDelete && path.Contains(".editor-draft.draft-")) throw new IOException("test draft-delete failure");
                Inner.DeleteFile(path);
            }
        }
        [UnityTest]
        public IEnumerator UnresolvedRecoveryRetainsResolutionAndFailedDeleteCanRetry()
        {
            foreach (bool stale in new[] { true, false })
            {
                CreateDurableRecovery();
                var root = GameRoot.Instance;
                if (stale)
                {
                    var floor = root.Save.validatedCanonicalSpatialState.Floors[0];
                    var otherStore = new FileDungeonDraftStore(root.SaveService.SavePath + ".canonical-change",
                        SpatialMigrationFileSystemSelector.Evaluate(root.SaveService.SavePath).FileSystem, root.SaveSpatialMigrationLimits);
                    var otherDraft = TransactionalDungeonDraft.Create(root.Save.validatedCanonicalSpatialState,
                        root.SaveService.DungeonDraftContext, otherStore);
                    var assignment = floor.RoomContents.Assignments[0];
                    Assert.That(otherDraft.Move(floor.FloorInstanceId, assignment.RoomInstanceId, assignment.AssignmentId,
                        new TileCoordinate(2, 1)), Is.True);
                    Assert.That(otherDraft.FlushNext(), Is.True);
                    var changed = root.SaveService.CommitDungeonDraft(root.Save, otherDraft);
                    Assert.That(changed.IsSuccess, Is.True, changed.Reason);
                }
                else
                {
                    string commit = Directory.GetFiles(Application.persistentDataPath,
                        filename + ".editor-draft.draft-*.commit").OrderBy(p => p, StringComparer.Ordinal).Last();
                    File.WriteAllText(commit, "malformed test commit evidence");
                }
                byte[] canonical = File.ReadAllBytes(root.SaveService.SavePath);
                double mana = root.Save.structureRuntime.ManaReserve;
                yield return RestartShell();
                var ui = controller.GetComponent<UIDocument>().rootVisualElement;
                Assert.That(ui.Q<Label>("modalText").text, Is.EqualTo(root.Content.GetString(
                    stale ? TransactionalDungeonDraft.StaleReason : TransactionalDungeonDraft.RecoveryFailedReason, null)));
                Assert.That(ui.Q<Button>("cancel").style.display.value, Is.EqualTo(DisplayStyle.None));
                // Even a queued/programmatic cancellation cannot dismiss the sole resolution path.
                typeof(ProductionDungeonController).GetMethod("CancelModal",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(controller, null);
                Assert.That(ui.Q("modal").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                controller.EnterEdit(); Assert.That(controller.IsEditing, Is.False);
                var fault = InjectDeleteFailure();
                Click(ui.Q<Button>("confirm")); yield return null;
                Assert.That(ui.Q("modal").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(ui.Q<Label>("modalText").text, Is.EqualTo(root.Content.GetString(TransactionalDungeonDraft.DeleteFailedReason, null)));
                Assert.That(ui.Q<Button>("confirm").enabledSelf, Is.True);
                Assert.That(ui.Q<Button>("edit").enabledSelf, Is.False);
                Assert.That(ui.Q<Button>("save").enabledSelf, Is.False);
                AssertCanonicalUnchanged(canonical, mana);
                fault.FailDelete = false;
                Click(ui.Q<Button>("confirm")); yield return null;
                Assert.That(ui.Q("modal").style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(ui.Q<Button>("edit").enabledSelf, Is.True);
                Assert.That(root.SaveService.CreateDungeonDraftStore().Read(), Is.Null);
                AssertCanonicalUnchanged(canonical, mana);
                controller.EnterEdit(); Assert.That(controller.IsEditing, Is.True);
                Assert.That(controller.Discard(), Is.True);
                AssertCanonicalUnchanged(canonical, mana);
            }
        }

        [UnityTest]
        public IEnumerator ValidRecoveryResumeKeepsCanonicalAndManaUnchanged()
        {
            CreateDurableRecovery();
            byte[] canonical = File.ReadAllBytes(GameRoot.Instance.SaveService.SavePath);
            double mana = GameRoot.Instance.Save.structureRuntime.ManaReserve;
            yield return RestartShell();
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(ui.Q<Button>("confirm").text, Is.EqualTo(GameRoot.Instance.Content.GetString("ui.dungeon.resume", null)));
            Click(ui.Q<Button>("confirm")); yield return null;
            Assert.That(controller.IsEditing, Is.True);
            Assert.That(controller.Draft.AcknowledgedSequence, Is.EqualTo(1));
            Assert.That(controller.Draft.ReadModel.Floors[0].RoomContents.Assignments[0].RoomLocalPosition,
                Is.EqualTo(new TileCoordinate(1, 1)));
            Assert.That(ui.Q<Button>("save").enabledSelf, Is.True);
            Assert.That(ui.Q("modal").style.display.value, Is.EqualTo(DisplayStyle.None));
            AssertCanonicalUnchanged(canonical, mana);
            Assert.That(controller.Discard(), Is.True);
            AssertCanonicalUnchanged(canonical, mana);
        }

        [UnityTest]
        public IEnumerator ValidRecoveryDiscardFailureRemovesResumeAndAllowsRetry()
        {
            CreateDurableRecovery();
            byte[] canonical = File.ReadAllBytes(GameRoot.Instance.SaveService.SavePath);
            double mana = GameRoot.Instance.Save.structureRuntime.ManaReserve;
            yield return RestartShell();
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(ui.Q<Button>("cancel").text, Is.EqualTo(GameRoot.Instance.Content.GetString("ui.dungeon.discard", null)));
            var fault = InjectDeleteFailure();
            Click(ui.Q<Button>("cancel")); yield return null;
            Assert.That(ui.Q("modal").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(ui.Q<Button>("confirm").text, Is.EqualTo(GameRoot.Instance.Content.GetString("ui.dungeon.discard", null)));
            Assert.That(controller.IsEditing, Is.False);
            AssertCanonicalUnchanged(canonical, mana);
            fault.FailDelete = false;
            Click(ui.Q<Button>("confirm")); yield return null;
            Assert.That(ui.Q("modal").style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(GameRoot.Instance.SaveService.CreateDungeonDraftStore().Read(), Is.Null);
            AssertCanonicalUnchanged(canonical, mana);
            // Qualify the ordinary valid-recovery Discard path as well.
            CreateDurableRecovery(); yield return RestartShell();
            ui = controller.GetComponent<UIDocument>().rootVisualElement;
            Click(ui.Q<Button>("cancel")); yield return null;
            Assert.That(ui.Q("modal").style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(ui.Q<Button>("edit").enabledSelf, Is.True);
            controller.EnterEdit(); Assert.That(controller.IsEditing, Is.True);
            Assert.That(controller.Discard(), Is.True);
            AssertCanonicalUnchanged(canonical, mana);
        }

        [UnityTest]
        public IEnumerator SelectionCloseEmptyTapFloorDiscardAndCommitClearPreview()
        {
            // New-game production saves only contain Floor 1. Construct the existing
            // configured Floor 2 through its canonical authority for real floor switching.
            var root = GameRoot.Instance;
            root.Save.completedResearch = new CompletedResearchState { ProjectIds = new[] { "ac_100" } };
            var construction = root.SaveService.PreviewFloorConstruction(root.Save);
            Assert.That(construction.Profile, Is.Not.Null);
            root.Save.structureRuntime.ManaReserve += construction.Profile.ConstructionMana;
            construction = root.SaveService.PreviewFloorConstruction(root.Save);
            var constructed = root.SaveService.CommitFloorConstruction(root.Save, construction);
            Assert.That(constructed.IsSuccess, Is.True, constructed.Reason);
            var state = GameRoot.Instance.Save.validatedCanonicalSpatialState;
            var floor = state.Floors[0]; var room = floor.Layout.Rooms[0];
            var definition = GameRoot.Instance.ProductionSpatialContent.Catalog.Rooms.Single(r => r.RoomDefinitionId == room.RoomDefinitionId);
            Assert.That(RoomLocalCoordinateTransform.TryToFloor(new TileCoordinate(0, 0), definition.GrossFootprint,
                room.Anchor, room.Orientation, out var start), Is.True);
            Assert.That(RoomLocalCoordinateTransform.TryToFloor(new TileCoordinate(1, 1), definition.GrossFootprint,
                room.Anchor, room.Orientation, out var target), Is.True);
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            controller.TapWorld(start); Assert.That(controller.World.PreviewVisible, Is.True);
            Click(ui.Q<Button>("closeSheet")); yield return null;
            Assert.That(controller.World.PreviewVisible, Is.False);
            Assert.That(ui.Q("contextSheet").style.display.value, Is.EqualTo(DisplayStyle.None));
            controller.TapWorld(start); controller.TapWorld(new TileCoordinate(-100, -100));
            Assert.That(controller.World.PreviewVisible, Is.False);
            controller.EnterEdit(); controller.TapWorld(start); controller.BeginMove();
            controller.TapWorld(new TileCoordinate(-100, -100));
            Assert.That(controller.IsMoving, Is.True); Assert.That(controller.World.PreviewVisible, Is.True);
            Assert.That(controller.World.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "Selection").color,
                Is.EqualTo(controller.presentationPolicy.InvalidColor));
            controller.SelectFloor(state.Floors[1].FloorInstanceId);
            Assert.That(controller.IsMoving, Is.False); Assert.That(controller.World.PreviewVisible, Is.False);
            controller.SelectFloor(floor.FloorInstanceId); controller.TapWorld(start);
            Assert.That(controller.Discard(), Is.True); Assert.That(controller.World.PreviewVisible, Is.False);
            controller.EnterEdit(); controller.TapWorld(start); controller.BeginMove(); controller.TapWorld(target);
            yield return null;
            Assert.That(controller.World.PreviewVisible, Is.True);
            Assert.That(controller.Commit(), Is.True); Assert.That(controller.World.PreviewVisible, Is.False);
        }

        private void CreateDurableRecovery()
        {
            var root = GameRoot.Instance; var floor = root.Save.validatedCanonicalSpatialState.Floors[0];
            var assignment = floor.RoomContents.Assignments.Single(a => a.CategoryId == CanonicalSpatialSaveContracts.MonsterCategoryId);
            var draft = TransactionalDungeonDraft.Create(root.Save.validatedCanonicalSpatialState,
                root.SaveService.DungeonDraftContext, root.SaveService.CreateDungeonDraftStore());
            Assert.That(draft.Move(floor.FloorInstanceId, assignment.RoomInstanceId, assignment.AssignmentId, new TileCoordinate(1, 1)), Is.True);
            Assert.That(draft.FlushNext(), Is.True);
        }
        private IEnumerator RestartShell()
        {
            var document = controller.GetComponent<UIDocument>();
            var tree = document.visualTreeAsset; var panel = document.panelSettings; var policy = controller.presentationPolicy;
            UnityEngine.Object.Destroy(controller.gameObject); yield return null;
            var shell = new GameObject("ProductionDungeonRecoveryTest", typeof(UIDocument));
            document = shell.GetComponent<UIDocument>(); document.panelSettings = panel; document.visualTreeAsset = tree;
            controller = shell.AddComponent<ProductionDungeonController>(); controller.presentationPolicy = policy;
            for (int i = 0; i < 30 && controller.World == null; i++) yield return null;
            Assert.That(controller.World, Is.Not.Null); yield return null;
        }
        private sealed class DeleteFailureStore : IDungeonDraftStore
        {
            public IDungeonDraftStore Inner;
            public bool FailDelete = true;
            public bool OutcomeUnknown => Inner.OutcomeUnknown;
            public byte[] Read() => Inner.Read();
            public bool Write(byte[] expected, byte[] next) => Inner.Write(expected, next);
            public bool Delete(byte[] expected) => !FailDelete && Inner.Delete(expected);
        }
        private DeleteFailureStore InjectDeleteFailure()
        {
            var field = typeof(ProductionDungeonController).GetField("store",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var fault = new DeleteFailureStore { Inner = (IDungeonDraftStore)field.GetValue(controller) };
            field.SetValue(controller, fault); return fault;
        }
        private static void Click(Button button)
        {
            // Exercise the registered UITK Clickable, including the generic confirm callback.
            // Device dispatch is separately qualified by the real Input System touch scenario.
            typeof(Clickable).GetMethod("SimulateSingleClick", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .Invoke(button.clickable, new object[] { null, 0 });
        }
        private static void AssertCanonicalUnchanged(byte[] bytes, double mana)
        {
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(GameRoot.Instance.SaveService.SavePath));
            Assert.That(GameRoot.Instance.Save.structureRuntime.ManaReserve, Is.EqualTo(mana));
        }
        private void Touch(int id, Vector2 position, InputTouchPhase phase) => InputSystem.QueueStateEvent(simulatedTouch,
            new TouchState { touchId = id, position = position, phase = phase });
    }
}
#endif
