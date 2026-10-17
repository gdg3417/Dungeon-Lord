#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0.Tests
{
    public abstract partial class PhaseSevenA4ProductionSceneTests
    {
        private void OpenFloorManagement()
        {
            var root = GameRoot.Instance;
            root.Save.structureRuntime.ManaReserve = 1000;
            root.Save.completedResearch = new CompletedResearchState { ProjectIds = new[] { FloorConstructionResearchAuthority.ResearchId } };
            Click(controller.GetComponent<UIDocument>().rootVisualElement.Q<Button>("manageFloors"));
        }

        private void ExportFloorUatSave(string name)
        {
            var root = GameRoot.Instance;
            Assert.That(root.SaveService.Save(root.Save, SaveReason.ManualDev), Is.True);
            string directory = Path.GetFullPath("TestResults/phase7-floor-lifecycle-uat");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, name + ".json"), root.SaveService.CanonicalSession.GetCurrentBytes());
        }

        [UnityTest]
        public IEnumerator GraphicalFloorsConstructionRequirementsBlockersRefreshAndReopen()
        {
            var root = GameRoot.Instance; var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            string original = controller.SelectedFloorInstanceId;
            OpenFloorManagement();
            ExportFloorUatSave("phase7-floor-ready-uat");
            var quote = root.SaveService.PreviewFloorConstruction(root.Save);
            Assert.That(ui.Q<Label>("floorConstructionInfo").text, Does.Contain(
                PlayerManaPresentationFormatter.FormatDiscreteAmount(quote.Profile.ConstructionMana,
                    PassiveManaPresenter.ResolveFormatProvider(root.Content.Strings.language))));
            root.Save.completedResearch.ProjectIds = Array.Empty<string>(); root.Save.totalTicks++; yield return null;
            Assert.That(ui.Q<Button>("constructFloor").enabledSelf, Is.False);
            Assert.That(ui.Q<Label>("floorConstructionReason").text,
                Is.EqualTo(root.Content.GetString(FloorConstructionResearchAuthority.ResearchRequiredReason, null)));
            root.Save.completedResearch.ProjectIds = new[] { FloorConstructionResearchAuthority.ResearchId };
            root.Save.structureRuntime.ManaReserve = 0; root.Save.totalTicks++; yield return null;
            ExportFloorUatSave("phase7-floor-insufficient-uat");
            var before = File.ReadAllBytes(root.SaveService.SavePath);
            Assert.That(ui.Q<Button>("constructFloor").enabledSelf, Is.False);
            Assert.That(controller.ConstructFloor(), Is.False);
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.Zero);
            CollectionAssert.AreEqual(before, File.ReadAllBytes(root.SaveService.SavePath));
            root.Save.structureRuntime.ManaReserve = 1000; root.Save.totalTicks++; yield return null;
            Assert.That(controller.ConstructFloor(), Is.True);
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(1000 - quote.Profile.ConstructionMana));
            Assert.That(controller.SelectedFloorInstanceId, Is.EqualTo(original));
            Assert.That(ui.Q("floors").childCount, Is.EqualTo(2));
            Assert.That(ui.Q("constructFloor").style.display.value, Is.EqualTo(DisplayStyle.None));
            ExportFloorUatSave("phase7-floor-shell-uat");
            before = File.ReadAllBytes(root.SaveService.SavePath); double mana = root.Save.structureRuntime.ManaReserve;
            Assert.That(controller.ConstructFloor(), Is.False);
            CollectionAssert.AreEqual(before, File.ReadAllBytes(root.SaveService.SavePath));
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(mana));
            string floorTwo = root.Save.validatedCanonicalSpatialState.Floors.Single(f => f.FloorIndex == 1).FloorInstanceId;
            UnityEngine.Object.Destroy(controller.gameObject); UnityEngine.Object.Destroy(root.gameObject); yield return null;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
            for (int i = 0; i < 120 && GameRoot.Instance?.Save == null; i++) yield return null;
            root = GameRoot.Instance;
            Assert.That(Path.GetFileName(root.SaveService.SavePath), Is.EqualTo(filename));
            Assert.That(root.Save.validatedCanonicalSpatialState.Floors.Single(f => f.FloorInstanceId == floorTwo).ActivationState,
                Is.EqualTo(FloorActivationState.Inactive));
            Assert.That(File.ReadAllText(root.SaveService.SavePath), Does.Contain("\"schemaVersion\":" + SaveMigration.LatestSchemaVersion));
            controller = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();
            for (int i = 0; i < 30 && controller.World == null; i++) yield return null;
            Assert.That(controller.GetComponent<UIDocument>().rootVisualElement.Q("floors").childCount, Is.EqualTo(2));
            root.enabled = false;
        }

        [UnityTest]
        public IEnumerator GraphicalFloorsLifecycleUsesProductionSelectionAndRefreshesInPlace()
        {
            var root = GameRoot.Instance; var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            OpenFloorManagement(); Assert.That(controller.ConstructFloor(), Is.True);
            string first = root.SelectedCanonicalFloorInstanceId;
            string second = root.Save.validatedCanonicalSpatialState.Floors.Single(f => f.FloorIndex == 1).FloorInstanceId;
            controller.SelectFloor(second);
            Assert.That(root.SelectedCanonicalFloorInstanceId, Is.EqualTo(first), "Production selection does not mutate Bootstrap selection");
            Assert.That(ui.Q("activateFloor").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(ui.Q<Button>("activateFloor").enabledSelf, Is.False);
            string reason = root.SaveService.PreviewFloorLifecycle(root.Save, FloorLifecycleAction.Activate, second).Reason;
            Assert.That(ui.Q<Label>("floorLifecycleReason").text, Is.EqualTo(root.Content.GetString(reason, null)));
            var before = File.ReadAllBytes(root.SaveService.SavePath);
            Assert.That(controller.ChangeFloorLifecycle(FloorLifecycleAction.Activate), Is.False);
            CollectionAssert.AreEqual(before, File.ReadAllBytes(root.SaveService.SavePath));
            controller.EnterEdit(); Assert.That(controller.SelectConstructionRoom("spatial.room.basic"), Is.True);
            controller.TapWorld(new TileCoordinate(1, 2));
            Assert.That(controller.ConstructionPreview.IsValid, Is.True);
            Assert.That(controller.ConfirmConstructionPlacement(), Is.True); yield return null; yield return null;
            Assert.That(controller.Commit(), Is.True);
            ExportFloorUatSave("phase7-floor-lifecycle-uat");
            Assert.That(ui.Q<Button>("activateFloor").enabledSelf, Is.True);
            double mana = root.Save.structureRuntime.ManaReserve; string oldRate = ui.Q<Label>("manaRate").text;
            Assert.That(controller.ChangeFloorLifecycle(FloorLifecycleAction.Activate), Is.True);
            Assert.That(controller.SelectedFloorInstanceId, Is.EqualTo(second));
            Assert.That(root.SelectedCanonicalFloorInstanceId, Is.EqualTo(first));
            Assert.That(root.Save.validatedCanonicalSpatialState.Floors[0].ActivationState, Is.EqualTo(FloorActivationState.Active));
            Assert.That(root.Save.validatedCanonicalSpatialState.Floors[1].ActivationState, Is.EqualTo(FloorActivationState.Active));
            Assert.That(ui.Q<Label>("manaRate").text, Is.Not.EqualTo(oldRate));
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(mana));
            Assert.That(controller.ChangeFloorLifecycle(FloorLifecycleAction.Activate), Is.False);
            Assert.That(controller.ChangeFloorLifecycle(FloorLifecycleAction.Deactivate), Is.True);
            Assert.That(ui.Q("modal").resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "Deepest floor needs no confirmation");
            Assert.That(root.Save.validatedCanonicalSpatialState.Floors[1].ActivationState, Is.EqualTo(FloorActivationState.Inactive));
            Assert.That(root.Save.validatedCanonicalSpatialState.Floors[0].ActivationState, Is.EqualTo(FloorActivationState.Active));
            Assert.That(controller.SelectedFloorInstanceId, Is.EqualTo(second));
            Assert.That(ui.Q<Label>("manaRate").text, Is.EqualTo(oldRate));
            Assert.That(controller.ChangeFloorLifecycle(FloorLifecycleAction.Deactivate), Is.False);
            Assert.That(controller.ChangeFloorLifecycle(FloorLifecycleAction.Activate), Is.True);
            controller.SelectFloor(first);
            Assert.That(ui.Q<Button>("deactivateFloor").enabledSelf, Is.False);
            Assert.That(ui.Q<Label>("floorLifecycleReason").text, Is.EqualTo(root.Content.GetString("floor.lifecycle.first_floor_required", null)));
            Assert.That(controller.ChangeFloorLifecycle(FloorLifecycleAction.Deactivate), Is.False);
            // Reopen the actual disposable scene save; no direct state restoration or migration.
            UnityEngine.Object.Destroy(controller.gameObject); UnityEngine.Object.Destroy(root.gameObject); yield return null;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
            for (int i = 0; i < 120 && GameRoot.Instance?.Save == null; i++) yield return null;
            root = GameRoot.Instance; root.enabled = false;
            Assert.That(root.Save.validatedCanonicalSpatialState.Floors[1].ActivationState, Is.EqualTo(FloorActivationState.Active));
            Assert.That(root.Save.validatedCanonicalSpatialState.Floors[0].FloorInstanceId, Is.EqualTo(first));
            controller = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();
            for (int i = 0; i < 30 && controller.World == null; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator GraphicalFloorsConfirmationCancellationAndChangedSelectionAreSafe()
        {
            var root = GameRoot.Instance; var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            OpenFloorManagement(); Assert.That(controller.ConstructFloor(), Is.True);
            string second = root.Save.validatedCanonicalSpatialState.Floors.Single(f => f.FloorIndex == 1).FloorInstanceId;
            controller.SelectFloor(second);
            // Synthetic preview tests the future cascade presentation without adding unsupported production floors.
            var preview = new FloorLifecyclePreview { IsCommittable = true, Action = FloorLifecycleAction.Deactivate,
                FloorInstanceId = second, Changes = Array.AsReadOnly(new[] {
                    new FloorLifecycleChange(new SavedSpatialFloor { FloorInstanceId = second, FloorIndex = 1 }),
                    new FloorLifecycleChange(new SavedSpatialFloor { FloorInstanceId = "test.only.deeper", FloorIndex = 2 }) }) };
            typeof(ProductionDungeonController).GetField("floorLifecycle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, preview);
            var before = File.ReadAllBytes(root.SaveService.SavePath); double mana = root.Save.structureRuntime.ManaReserve;
            Assert.That(controller.ChangeFloorLifecycle(FloorLifecycleAction.Deactivate), Is.False);
            Assert.That(ui.Q("modal").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(ui.Q<Label>("modalText").text, Does.Contain("Floor 2").And.Contain("Floor 3"));
            Click(ui.Q<Button>("cancel"));
            Assert.That(ui.Q("modal").style.display.value, Is.EqualTo(DisplayStyle.None));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(root.SaveService.SavePath));
            typeof(ProductionDungeonController).GetField("floorLifecycle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, preview);
            controller.ChangeFloorLifecycle(FloorLifecycleAction.Deactivate);
            controller.SelectFloor(root.SelectedCanonicalFloorInstanceId);
            Click(ui.Q<Button>("confirm"));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(root.SaveService.SavePath));
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(mana));
            Assert.That(ui.Q<Label>("status").text, Is.EqualTo(root.Content.GetString("floor.lifecycle.stale_preview", null)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator GraphicalFloorsControlsRemainReachableAtSupportedSizes()
        {
            OpenFloorManagement();
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            foreach (var size in new[] { new Vector2Int(720, 1280), new Vector2Int(1080, 1920), new Vector2Int(1920, 1080), new Vector2Int(1536, 2048) })
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x, size.y);
                foreach (var text in new[] { DungeonTextSize.Small, DungeonTextSize.Default, DungeonTextSize.Large })
                {
                    controller.SetTextSize(text); for (int i = 0; i < 12; i++) yield return null;
                    var scroll = ui.Q<ScrollView>("contextDetails"); scroll.ScrollTo(ui.Q("constructFloor"));
                    for (int i = 0; i < 5; i++) yield return null;
                    var button = ui.Q<Button>("constructFloor");
                    Assert.That(button.worldBound.yMax, Is.LessThanOrEqualTo(scroll.contentViewport.worldBound.yMax + 1));
                    Assert.That(button.worldBound.yMin, Is.GreaterThanOrEqualTo(scroll.contentViewport.worldBound.yMin - 1));
                    UatSafeAction(button);
                    Assert.That(ui.Q<Label>("floorConstructionInfo").worldBound.xMax, Is.LessThanOrEqualTo(controller.SafeRoot.worldBound.xMax + 1));
                    Assert.That(controller.Viewport.ScreenRect.height, Is.GreaterThan(0));
                    TestContext.WriteLine("Floor controls " + size + " " + text + " action=" + button.worldBound + " map=" + controller.Viewport.ScreenRect);
                    if (text == DungeonTextSize.Large)
                    {
                        string captures = Path.GetFullPath("TestResults/phase7-floor-lifecycle-screenshots");
                        Directory.CreateDirectory(captures);
                        ScreenCapture.CaptureScreenshot(Path.Combine(captures, "floors-" + size.x + "x" + size.y + ".png"));
                        yield return null; yield return null;
                    }
                }
            }
        }
    }
}
#endif
