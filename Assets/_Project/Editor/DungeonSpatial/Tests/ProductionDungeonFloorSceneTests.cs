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
        private object FloorReviewField(string name) => typeof(ProductionDungeonController)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);

        [UnityTest]
        public IEnumerator GraphicalFloorReviewTickProbe()
        {
            OpenFloorManagement(); Assert.That(controller.ConstructFloor(), Is.True);
            var root = GameRoot.Instance;
            controller.SelectFloor(root.Save.validatedCanonicalSpatialState.Floors[1].FloorInstanceId);
            controller.EnterEdit(); Assert.That(controller.SelectConstructionRoom("spatial.room.basic"), Is.True);
            controller.TapWorld(new TileCoordinate(1, 2)); Assert.That(controller.ConfirmConstructionPlacement(), Is.True);
            yield return null; yield return null; Assert.That(controller.Commit(), Is.True);
            var update = typeof(ProductionDungeonController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            int lifecycleCalls = 0, constructionCalls = 0;
            var lifecycle = FloorReviewField("floorLifecycle"); var construction = FloorReviewField("floorConstruction");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            long allocated = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 12; i++)
            {
                root.Save.totalTicks++; root.Save.structureRuntime.Heat += 1; root.Save.structureRuntime.ManaReserve += .5;
                update.Invoke(controller, null);
                var nextLifecycle = FloorReviewField("floorLifecycle"); var nextConstruction = FloorReviewField("floorConstruction");
                if (!ReferenceEquals(lifecycle, nextLifecycle)) lifecycleCalls++;
                if (!ReferenceEquals(construction, nextConstruction)) constructionCalls++;
                lifecycle = nextLifecycle; construction = nextConstruction;
            }
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated; watch.Stop();
            TestContext.WriteLine($"Floor review 12 simulated foreground ticks: lifecycle previews={lifecycleCalls}; construction previews={constructionCalls}; Update ms={watch.Elapsed.TotalMilliseconds:F3}; thread allocations={allocated}");
            Assert.That(lifecycleCalls, Is.Zero, "Unchanged eligibility must reuse the lifecycle preview");
            Assert.That(constructionCalls, Is.Zero, "Unchanged eligibility must reuse the construction preview");
        }

        [UnityTest]
        public IEnumerator GraphicalFloorReviewMeaningfulInvalidations()
        {
            OpenFloorManagement(); var root = GameRoot.Instance;
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var update = typeof(ProductionDungeonController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            var quote = (FloorConstructionPreview)FloorReviewField("floorConstruction");
            long reserve = MonsterContinuity.RequiredReserve(root.Save.validatedCanonicalSpatialState,
                (MonsterRecoveryRules)FloorReviewField("recoveryPresentationRules"));
            root.Save.structureRuntime.ManaReserve = quote.Profile.ConstructionMana + reserve - 1;
            update.Invoke(controller, null);
            Assert.That(ui.Q<Button>("constructFloor").enabledSelf, Is.False);
            root.Save.structureRuntime.ManaReserve = quote.Profile.ConstructionMana - 1;
            update.Invoke(controller, null);
            Assert.That(ui.Q<Label>("floorConstructionReason").text, Is.EqualTo(root.Content.GetString(FloorConstructionService.InsufficientManaReason, null)));
            root.Save.structureRuntime.ManaReserve = quote.Profile.ConstructionMana;
            update.Invoke(controller, null);
            Assert.That(ui.Q<Label>("floorConstructionReason").text, Is.EqualTo(root.Content.GetString(MonsterRecoveryRules.Insufficient, null)),
                "The authoritative blocker changes even though both resource ranges are unaffordable");
            root.Save.structureRuntime.ManaReserve = quote.Profile.ConstructionMana + reserve - 1;
            update.Invoke(controller, null);
            var blocked = FloorReviewField("floorConstruction");
            root.Save.structureRuntime.ManaReserve -= 1; update.Invoke(controller, null);
            Assert.That(FloorReviewField("floorConstruction"), Is.SameAs(blocked), "Same affordability range is lightweight");
            root.Save.structureRuntime.ManaReserve = quote.Profile.ConstructionMana + reserve;
            update.Invoke(controller, null);
            Assert.That(ui.Q<Button>("constructFloor").enabledSelf, Is.True, "Cost plus current reserve, without publication or a tick");
            root.Save.completedResearch.ProjectIds = Array.Empty<string>(); update.Invoke(controller, null);
            Assert.That(ui.Q<Button>("constructFloor").enabledSelf, Is.False);
            root.Save.completedResearch.ProjectIds = new[] { FloorConstructionResearchAuthority.ResearchId };
            root.Save.structureRuntime.ManaReserve = 1000; update.Invoke(controller, null);
            Assert.That(ui.Q<Button>("constructFloor").enabledSelf, Is.True);
            var beforeClose = FloorReviewField("floorConstruction");
            Click(ui.Q<Button>("manageFloors")); Click(ui.Q<Button>("manageFloors"));
            Assert.That(FloorReviewField("floorConstruction"), Is.Not.SameAs(beforeClose));
            Assert.That(controller.ConstructFloor(), Is.True);
            string second = root.Save.validatedCanonicalSpatialState.Floors[1].FloorInstanceId;
            controller.SelectFloor(second);
            Assert.That(((FloorLifecyclePreview)FloorReviewField("floorLifecycle")).FloorInstanceId, Is.EqualTo(second));
            controller.EnterEdit(); controller.SelectConstructionRoom("spatial.room.basic"); controller.TapWorld(new TileCoordinate(1, 2));
            Assert.That(controller.ConfirmConstructionPlacement(), Is.True); yield return null; yield return null;
            Assert.That(controller.Commit(), Is.True);
            var shell = root.Save.validatedCanonicalSpatialState.Floors[1];
            var deployed = root.SaveService.ExecuteCanonicalMutation(root.Save, DetachedCanonicalMutationRequest.Place(
                "placement.category.monster", "placement.option.monster.skeleton", shell.Layout.Rooms[0].RoomInstanceId,
                second, new TileCoordinate(0, 0)));
            Assert.That(deployed.IsSuccess, Is.True, deployed.Reason);
            var lifecycle = (FloorLifecyclePreview)FloorReviewField("floorLifecycle");
            Assert.That(lifecycle.RequiredReserveAfter, Is.GreaterThan(lifecycle.RequiredReserveBefore));
            double publishedMana = root.Save.structureRuntime.ManaReserve;
            root.Save.structureRuntime.ManaReserve = lifecycle.RequiredReserveAfter - 1; update.Invoke(controller, null);
            Assert.That(ui.Q<Button>("activateFloor").enabledSelf, Is.False);
            root.Save.structureRuntime.ManaReserve = lifecycle.RequiredReserveAfter; update.Invoke(controller, null);
            Assert.That(ui.Q<Button>("activateFloor").enabledSelf, Is.True);
            var funded = FloorReviewField("floorLifecycle");
            var monster = root.Save.monsterContinuity.Records[0]; int health = monster.Health;
            monster.Health--; update.Invoke(controller, null);
            Assert.That(FloorReviewField("floorLifecycle"), Is.SameAs(funded), "HP is not the reserve obligation");
            monster.Health = health;
            // Affordability/HP probes are presentation-only. Restore their source
            // values before allowing the legitimate pending foreground write.
            root.Save.structureRuntime.ManaReserve = publishedMana;
            yield return StabilizeReadOnlySceneBaseline();
            update.Invoke(controller, null); funded = FloorReviewField("floorLifecycle");
            var originalLoot = typeof(SaveService).GetField("_runLoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root.SaveService);
            var originalSession = root.SaveService.CanonicalSession;
            byte[] incompatibleBefore = File.ReadAllBytes(root.SaveService.SavePath);
            string incompatibleLive = JsonUtility.ToJson(root.Save);
            root.SaveService.ConfigureRunLoot(DungeonBuilder.M0.Tests.EditMode.PhaseSixA4Tests.Loot());
            update.Invoke(controller, null);
            Assert.That(FloorReviewField("floorLifecycle"), Is.Not.SameAs(funded), "Configuration invalidates without publication");
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                "GD66 save failed: gd66.write.first_write_validation_failed$"));
            Assert.That(root.SaveService.Save(root.Save, SaveReason.ManualDev), Is.False,
                "A foreign loot object cannot acquire the consumed canonical context");
            CollectionAssert.AreEqual(incompatibleBefore, File.ReadAllBytes(root.SaveService.SavePath));
            Assert.That(root.SaveService.CanonicalSession, Is.SameAs(originalSession));
            Assert.That(JsonUtility.ToJson(root.Save), Is.EqualTo(incompatibleLive));
            // Restore the identical consumed object, not reloaded/rebound inputs.
            root.SaveService.ConfigureRunLoot((LootConfig)originalLoot); update.Invoke(controller, null);
            var beforePublish = FloorReviewField("floorLifecycle");
            Assert.That(root.SaveService.Save(root.Save, SaveReason.ManualDev), Is.True);
            Assert.That(FloorReviewField("floorLifecycle"), Is.Not.SameAs(beforePublish));
            controller.SelectFloor(root.Save.validatedCanonicalSpatialState.Floors[0].FloorInstanceId);
            Assert.That(((FloorLifecyclePreview)FloorReviewField("floorLifecycle")).Reason, Is.EqualTo("floor.lifecycle.first_floor_required"));
            controller.SelectFloor(second); controller.EnterEdit(); Assert.That(controller.Discard(), Is.True);
            Assert.That(ui.Q<Button>("activateFloor").enabledSelf, Is.True);
            root.Save.structureRuntime.ManaReserve += 1; // Fresh direct click binds this value even within the same affordability range.
            Assert.That(controller.ChangeFloorLifecycle(FloorLifecycleAction.Activate), Is.True);
            Assert.That(controller.ChangeFloorLifecycle(FloorLifecycleAction.Deactivate), Is.True);
        }

        [UnityTest]
        public IEnumerator GraphicalFloorReviewOpenConfirmationRejectsUnpublishedChanges()
        {
            OpenFloorManagement(); Assert.That(controller.ConstructFloor(), Is.True);
            var root = GameRoot.Instance; var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            string second = root.Save.validatedCanonicalSpatialState.Floors[1].FloorInstanceId;
            controller.SelectFloor(second); controller.EnterEdit(); controller.SelectConstructionRoom("spatial.room.basic");
            controller.TapWorld(new TileCoordinate(1, 2)); Assert.That(controller.ConfirmConstructionPlacement(), Is.True);
            yield return null; yield return null; Assert.That(controller.Commit(), Is.True);
            Assert.That(controller.ChangeFloorLifecycle(FloorLifecycleAction.Activate), Is.True);
            // Close the authorized activation's foreground publication before
            // opening a modal whose deliberate live mutation must stay unsaved.
            yield return StabilizeReadOnlySceneBaseline();
            foreach (bool research in new[] { false, true })
            {
                var preview = root.SaveService.PreviewFloorLifecycle(root.Save, FloorLifecycleAction.Deactivate, second);
                Assert.That(preview.IsCommittable, Is.True);
                // Force the existing presenter for a real preview to exercise an open
                // confirmation delegate; current two-floor content has no real cascade.
                typeof(ProductionDungeonController).GetMethod("ShowFloorLifecycleConfirmation", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(controller, new object[] { preview });
                byte[] before = root.SaveService.CanonicalSession.GetCurrentBytes();
                if (research) root.Save.completedResearch.ProjectIds = Array.Empty<string>();
                else root.Save.structureRuntime.ManaReserve += 1;
                string live = JsonUtility.ToJson(root.Save);
                yield return null; // The panel may refresh, but the modal must keep its captured binding.
                Click(ui.Q<Button>("confirm"));
                Assert.That(ui.Q<Label>("status").text, Is.EqualTo(root.Content.GetString("floor.lifecycle.stale_preview", null)));
                CollectionAssert.AreEqual(before, File.ReadAllBytes(root.SaveService.SavePath));
                Assert.That(JsonUtility.ToJson(root.Save), Is.EqualTo(live));
                Assert.That(root.Save.validatedCanonicalSpatialState.Floors[1].ActivationState, Is.EqualTo(FloorActivationState.Active));
            }
        }

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
            string directory = Path.GetFullPath("TestResults/phase7-floor-lifecycle-uat/" + filename);
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
            var deeper = new SavedSpatialFloor { FloorInstanceId = "test.only.deeper", FloorIndex = 2 };
            var preview = new FloorLifecyclePreview { IsCommittable = true, Action = FloorLifecycleAction.Deactivate,
                FloorInstanceId = second, Changes = Array.AsReadOnly(new[] {
                    new FloorLifecycleChange(deeper),
                    new FloorLifecycleChange(new SavedSpatialFloor { FloorInstanceId = second, FloorIndex = 1 }) }) };
            typeof(ProductionDungeonController).GetField("floorLifecycle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, preview);
            var before = File.ReadAllBytes(root.SaveService.SavePath); double mana = root.Save.structureRuntime.ManaReserve;
            typeof(ProductionDungeonController).GetMethod("ShowFloorLifecycleConfirmation", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(controller, new object[] { preview });
            Assert.That(ui.Q("modal").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            string englishConfirmation = ui.Q<Label>("modalText").text;
            Assert.That(englishConfirmation, Does.Contain("Floor 2, Floor 3"), "English list is readable and follows canonical floor order");
            Assert.That(englishConfirmation, Is.EqualTo(root.Content.GetString("ui.dungeon.floors.cascade", null)
                .Replace("{0}", "Floor 2, Floor 3")), "The localized cascade consequences remain visible");
            Click(ui.Q<Button>("cancel"));
            Assert.That(ui.Q("modal").style.display.value, Is.EqualTo(DisplayStyle.None));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(root.SaveService.SavePath));

            var strings = (System.Collections.IDictionary)typeof(ContentService)
                .GetField("_stringMap", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root.Content);
            string separatorKey = "ui.dungeon.floors.list_separator";
            string englishSeparator = (string)strings[separatorKey];
            strings[separatorKey] = " · ";
            typeof(ProductionDungeonController).GetMethod("ShowFloorLifecycleConfirmation", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(controller, new object[] { preview });
            Assert.That(ui.Q<Label>("modalText").text, Does.Contain("Floor 2 · Floor 3"),
                "The presenter uses the supplied localized separator");
            Click(ui.Q<Button>("cancel"));
            strings[separatorKey] = englishSeparator;

            typeof(ProductionDungeonController).GetField("floorLifecycle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, preview);
            typeof(ProductionDungeonController).GetMethod("ShowFloorLifecycleConfirmation", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(controller, new object[] { preview });
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
                        string captures = Path.GetFullPath("TestResults/phase7-floor-lifecycle-screenshots/" + filename);
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
