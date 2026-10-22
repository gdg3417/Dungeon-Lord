#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0.Tests
{
    public abstract partial class PhaseSevenA4ProductionSceneTests
    {
        private IEnumerator AnalysisModifiedDraft()
        {
            GameRoot.Instance.Save.structureRuntime.ManaReserve = 1000;
            controller.EnterEdit(); controller.OpenRooms();
            Assert.That(controller.SelectConstructionRoom("spatial.room.basic"), Is.True);
            controller.TapWorld(controller.World.MoveGuidanceAnchors.First());
            Assert.That(controller.ConfirmConstructionPlacement(), Is.True);
            yield return null; yield return null;
            Assert.That(controller.Draft.CanSave, Is.True);
        }

        [UnityTest] public IEnumerator AnalysisNavigationRetainsFloorAndReadOnlySaveAcrossShellReopen()
        {
            var root = GameRoot.Instance;
            root.Save.structureRuntime.ManaReserve = 1000;
            Assert.That(root.SimulateRunOnce(), Is.True);
            var before = File.ReadAllBytes(root.SaveService.SavePath);
            var history = JsonUtility.ToJson(root.Save.runHistory);
            string floor = controller.SelectedFloorInstanceId;
            controller.OpenAnalysis(); yield return null;
            Assert.That(controller.IsAnalysis, Is.True);
            controller.EnterEdit(); Assert.That(controller.IsEditing, Is.False);
            controller.ReturnToDungeon(); yield return null;
            Assert.That(controller.SelectedFloorInstanceId, Is.EqualTo(floor));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(root.SaveService.SavePath));
            UnityEngine.Object.Destroy(controller.gameObject);
            UnityEngine.Object.Destroy(root.gameObject); yield return null;
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
            for (int i=0;i<120 && GameRoot.Instance?.Save==null;i++) yield return null;
            root = GameRoot.Instance; root.enabled = false;
            controller = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();
            for (int i=0;i<30 && controller.World==null;i++) yield return null;
            // Boot owns its clock/save metadata; Analysis must preserve the reopened bytes.
            before = File.ReadAllBytes(root.SaveService.SavePath);
            controller.OpenAnalysis(); yield return null;
            Assert.That(JsonUtility.ToJson(root.Save.runHistory), Is.EqualTo(history));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(root.SaveService.SavePath));
        }

        [UnityTest] public IEnumerator AnalysisCleanAndModifiedDraftContinueDiscardSave()
        {
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            controller.EnterEdit(); controller.OpenAnalysis();
            Assert.That(controller.IsAnalysis, Is.True); Assert.That(controller.IsEditing, Is.False);
            controller.ReturnToDungeon();
            yield return AnalysisModifiedDraft();
            var draft = controller.Draft;
            controller.OpenAnalysis(); Assert.That(controller.IsAnalysis, Is.False);
            Assert.That(ui.Q<Button>("navigationDiscard").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Click(ui.Q<Button>("cancel")); Assert.That(controller.Draft, Is.SameAs(draft));
            controller.OpenAnalysis(); Click(ui.Q<Button>("navigationDiscard"));
            Assert.That(controller.IsAnalysis, Is.True); Assert.That(controller.IsEditing, Is.False);
            controller.ReturnToDungeon(); yield return AnalysisModifiedDraft();
            controller.OpenAnalysis(); Click(ui.Q<Button>("confirm"));
            Assert.That(controller.IsAnalysis, Is.True); Assert.That(controller.IsEditing, Is.False);
            controller.ReturnToDungeon(); yield return null;
            Assert.That(ui.Q<Button>("edit").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(ui.Q("editActions").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest] public IEnumerator AnalysisFailedDiscardAndFailedSaveNeverNavigateOrLoseDraft()
        {
            var fault = InjectDeleteFailure();
            yield return AnalysisModifiedDraft(); var draft = controller.Draft;
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            controller.OpenAnalysis(); Click(ui.Q<Button>("navigationDiscard"));
            Assert.That(controller.IsAnalysis, Is.False); Assert.That(controller.Draft, Is.SameAs(draft));
            fault.FailDelete = false;
            Assert.That(controller.Discard(), Is.True);
            yield return AnalysisModifiedDraft(); draft = controller.Draft;
            // An unaffordable authoritative save must fail without completing navigation.
            GameRoot.Instance.Save.structureRuntime.ManaReserve = 0;
            controller.OpenAnalysis(); Click(ui.Q<Button>("confirm"));
            Assert.That(controller.IsAnalysis, Is.False); Assert.That(controller.Draft, Is.SameAs(draft));
            Assert.That(controller.Discard(), Is.True);
        }

        [UnityTest] public IEnumerator AnalysisInvalidDraftOffersOnlyDiscardAndContinue()
        {
            controller.EnterEdit();
            var room = GameRoot.Instance.Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms[0];
            controller.TapWorld(new TileCoordinate(room.Anchor.X + 2, room.Anchor.Y + 2));
            controller.BeginMove(); controller.TapWorld(new TileCoordinate(-1, -1)); yield return null;
            Assert.That(controller.Draft.IsStructurallyValid, Is.False);
            var draft = controller.Draft; var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            controller.OpenAnalysis(); Assert.That(ui.Q<Button>("confirm").text, Is.EqualTo(GameRoot.Instance.Content.GetString("ui.analysis.discard_changes", null)));
            Assert.That(ui.Q<Button>("navigationDiscard").style.display.value, Is.EqualTo(DisplayStyle.None));
            Click(ui.Q<Button>("cancel")); Assert.That(controller.Draft, Is.SameAs(draft));
            controller.OpenAnalysis(); Click(ui.Q<Button>("confirm")); Assert.That(controller.IsAnalysis, Is.True);
        }

        [UnityTest] public IEnumerator AnalysisPendingAndStaleSaveRemainDisabledWithDraftIntact()
        {
            var root = GameRoot.Instance; var floor = root.Save.validatedCanonicalSpatialState.Floors[0];
            var assignment = floor.RoomContents.Assignments.First();
            controller.EnterEdit(); var draft = controller.Draft;
            Assert.That(draft.Move(floor.FloorInstanceId, assignment.RoomInstanceId, assignment.AssignmentId, new TileCoordinate(1,0)), Is.True);
            Assert.That(draft.Durability, Is.EqualTo(DraftDurability.Pending));
            controller.OpenAnalysis(); var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(ui.Q<Button>("confirm").enabledSelf, Is.False);
            Assert.That(controller.IsAnalysis, Is.False); Assert.That(controller.Draft, Is.SameAs(draft));
            yield return null; yield return null;
            Assert.That(ui.Q<Button>("confirm").enabledSelf, Is.True);
            Click(ui.Q<Button>("cancel"));
            var otherStore = new FileDungeonDraftStore(root.SaveService.SavePath + ".analysis-stale",
                SpatialMigrationFileSystemSelector.Evaluate(root.SaveService.SavePath).FileSystem, root.SaveSpatialMigrationLimits);
            var other = TransactionalDungeonDraft.Create(root.Save.validatedCanonicalSpatialState, root.SaveService.DungeonDraftContext, otherStore);
            Assert.That(other.Move(floor.FloorInstanceId, assignment.RoomInstanceId, assignment.AssignmentId, new TileCoordinate(2,1)), Is.True);
            Assert.That(other.FlushNext(), Is.True);
            Assert.That(root.SaveService.CommitDungeonDraft(root.Save, other).IsSuccess, Is.True);
            controller.OpenAnalysis(); Assert.That(ui.Q<Button>("confirm").enabledSelf, Is.False);
            Click(ui.Q<Button>("cancel")); Assert.That(controller.Draft, Is.SameAs(draft));
            Assert.That(controller.Discard(), Is.True);
        }

        [UnityTest] public IEnumerator AnalysisResponsiveSafeAreaLocalizationAndScroll()
        {
            GameRoot.Instance.Save.structureRuntime.ManaReserve = 1000;
            Assert.That(GameRoot.Instance.SimulateRunOnce(), Is.True);
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            foreach (var resolution in new[] { new Vector2Int(720,1280), new Vector2Int(1280,720), new Vector2Int(1920,1080) })
            foreach (var text in new[] { DungeonTextSize.Default, DungeonTextSize.Large })
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(resolution.x,resolution.y);
                for (int i=0;i<12;i++) yield return null;
                controller.SetTextSize(text); controller.ApplySafeArea(new Rect(20,35,resolution.x-40,resolution.y-70),resolution);
                controller.OpenAnalysis(); for (int i=0;i<8;i++) yield return null;
                AssertReachable(ui.Q<Button>("analysisDungeon")); AssertReachable(ui.Q<Button>("analysisDisplay"));
                UatSafeAction(ui.Q<Button>("analysisDungeon")); UatSafeAction(ui.Q<Button>("analysisDisplay"));
                Assert.That(ui.Q<Label>("analysisReport").text, Does.Not.Contain("ui.analysis."));
                Assert.That(ui.Q("analysisScroll").worldBound.yMax, Is.LessThanOrEqualTo(controller.SafeRoot.worldBound.yMax+1));
                yield return CompositionCapture("analysis-" + text, resolution);
                ui.Q<Label>("analysisEvidence").text = string.Join("\n", Enumerable.Repeat("Test-only expanded localization text", 80));
                for (int i=0;i<8;i++) yield return null;
                Assert.That(ui.Q<ScrollView>("analysisScroll").verticalScroller.highValue, Is.GreaterThan(0));
                controller.ReturnToDungeon(); for (int i=0;i<8;i++) yield return null;
                AssertReachable(ui.Q<Button>("openAnalysis")); UatSafeAction(ui.Q<Button>("openAnalysis"));
                Assert.That(controller.Viewport.ScreenRect.height, Is.GreaterThan(0));
            }
        }

        [UnityTest] public IEnumerator AnalysisMissingLocalizationUsesExistingAvailabilityFeedback()
        {
            var content = GameRoot.Instance.Content;
            var strings = (System.Collections.Generic.Dictionary<string,string>)typeof(ContentService)
                .GetField("_stringMap", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(content);
            string original = strings["ui.analysis.empty"];
            try
            {
                strings.Remove("ui.analysis.empty"); controller.OpenAnalysis();
                Assert.That(controller.GetComponent<UIDocument>().rootVisualElement.Q<Label>("analysisReport").text,
                    Is.EqualTo(content.GetString("ui.dungeon.unavailable", string.Empty)));
            }
            finally { strings["ui.analysis.empty"] = original; }
            yield return null;
        }

        [UnityTest] public IEnumerator AnalysisReplayEntryImmediatelyUsesCompactHeader()
        {
            GameRoot.Instance.Save.structureRuntime.ManaReserve = 1000;
            Assert.That(GameRoot.Instance.SimulateRunOnce(), Is.True);
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(1280,720);
            controller.SetTextSize(DungeonTextSize.Large);
            for (int i=0;i<12;i++) yield return null;
            Assert.That(controller.StartReplay(), Is.True);
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(ui.Q<Label>("performanceSummary").ClassListContains("replay-summary"), Is.True);
            Assert.That(ui.Q<Label>("performanceSummary").text, Does.Not.Contain("\n"));
            for (int i=0;i<8;i++) yield return null;
            UatSafeAction(ui.Q<Button>("edit")); UatSafeAction(ui.Q<Button>("openAnalysis"));
        }

        [UnityTest] public IEnumerator AnalysisDoesNotInterruptAuthoritativeForegroundAdmissionAndSettlement()
        {
            var root = GameRoot.Instance; var service = root.SaveService;
            Assert.That(root.TrySetQaManaFromDevPanel(true), Is.True);
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0];
            Assert.That(service.ExecuteCanonicalMutation(root.Save, DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId,
                floor.Layout.Rooms[0].RoomInstanceId, floor.FloorInstanceId, new TileCoordinate(1,1))).IsSuccess, Is.True);
            var advance = InstallForegroundBoundaryTestClock();
            var update = (Action)Delegate.CreateDelegate(typeof(Action), root,
                typeof(GameRoot).GetMethod("UpdateForeground", BindingFlags.Instance | BindingFlags.NonPublic));
            update(); var save = root.Save; var session = service.CanonicalSession;
            controller.OpenAnalysis();
            Assert.That(root.Save, Is.SameAs(save)); Assert.That(service.CanonicalSession, Is.SameAs(session));
            advance((root.Save.livingDungeon.Scheduler.NextOpportunityTime.Value-root.TimeService.SampleForeground())/1000d+.001);
            update(); yield return null;
            Assert.That(root.Save.livingDungeon.Parties.Count, Is.EqualTo(1));
            Assert.That(root.SaveService, Is.SameAs(service));
            var profile = service.ForegroundConfiguration.CreateProfile(root.SaveSpatialMigrationLimits.Canonical.ForSchema(13).Serialized);
            var execution = LivingDungeonContracts.Restore(root.Save.livingDungeon.Parties.Single(), profile);
            long finish; RunOutcomeRecord expected;
            try
            {
                expected = execution.DrainToCompletion();
                finish = root.Save.livingDungeon.Parties.Single().AdmittedAt + execution.Inspect().LogicalTime;
            }
            finally { execution.Abandon(); }
            advance((finish-root.TimeService.SampleForeground())/1000d+.001);
            for (int pump=0;pump<64 && !root.Save.runHistory.RecentOutcomes.Any(o=>o.RunId==expected.RunId);pump++)
            { update(); yield return null; }
            Assert.That(root.Save.runHistory.RecentOutcomes.Count(o=>o.RunId==expected.RunId), Is.EqualTo(1));
            Assert.That(controller.IsAnalysis, Is.True);
            Assert.That(controller.GetComponent<UIDocument>().rootVisualElement.Q<Label>("analysisReport").text,
                Does.Contain(string.Format(root.Content.GetString("ui.analysis.window", null),root.Save.runHistory.RecentOutcomes.Length)));
            save = root.Save; session = service.CanonicalSession;
            string checkpoint = JsonUtility.ToJson(root.Save.livingDungeon);
            controller.ReturnToDungeon();
            Assert.That(root.Save, Is.SameAs(save)); Assert.That(service.CanonicalSession, Is.SameAs(session));
            Assert.That(JsonUtility.ToJson(root.Save.livingDungeon), Is.EqualTo(checkpoint));
        }
    }
}
#endif
