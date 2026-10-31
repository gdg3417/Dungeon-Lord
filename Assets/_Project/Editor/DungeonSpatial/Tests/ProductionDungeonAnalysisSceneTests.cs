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

        private IEnumerator AnalysisPhoneSettle()
        {
            controller.ApplySafeArea(new Rect(10,18,340,604), new Vector2(360,640));
            for (int i=0;i<8;i++) yield return null;
            Assert.That(new Vector2Int(Screen.width,Screen.height), Is.EqualTo(new Vector2Int(360,640)));
            var expected = DungeonPresentationPolicy.SafePanelRect(new Rect(10,18,340,604),640,
                controller.GetComponent<UIDocument>().panelSettings.scale);
            Assert.That(controller.SafeRoot.worldBound.x, Is.EqualTo(expected.x).Within(1));
            Assert.That(controller.SafeRoot.worldBound.y, Is.EqualTo(expected.y).Within(1));
            Assert.That(controller.SafeRoot.worldBound.width, Is.EqualTo(expected.width).Within(1));
            Assert.That(controller.SafeRoot.worldBound.height, Is.EqualTo(expected.height).Within(1));
        }

        private IEnumerator AnalysisPhoneConfigure(DungeonTextSize size)
        {
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(360,640);
            for (int i=0;i<12;i++) yield return null;
            controller.SetTextSize(size);
            if (simulatedTouch == null) simulatedTouch = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
            yield return AnalysisPhoneSettle();
        }

        private IEnumerator AnalysisPhoneReveal(VisualElement element)
        {
            yield return AnalysisPhoneSettle();
            var scroll = element.GetFirstAncestorOfType<ScrollView>();
            if (scroll != null) scroll.ScrollTo(element);
            yield return AnalysisPhoneSettle();
            TestContext.WriteLine("Phone target " + element.name + " " + controller.TextSize + ": " + element.worldBound + "; safe=" + controller.SafeRoot.worldBound);
            var picked = element.panel.Pick(element.worldBound.center);
            TestContext.WriteLine("Picked=" + picked?.name + "; parent=" + picked?.parent?.name + "; viewport=" + scroll?.contentViewport.worldBound +
                "; editing=" + controller.IsEditing + "; top=" + controller.SafeRoot.Q("topChrome").worldBound +
                "; bottom=" + controller.SafeRoot.Q("bottomChrome").worldBound + "; map=" + controller.SafeRoot.Q("mapArea").worldBound);
            if (!controller.SafeRoot.worldBound.Contains(element.worldBound.max-Vector2.one) ||
                picked != element && !element.Contains(picked))
            {
                string capture = "analysis-phone-failure-" + element.name + "-" + Guid.NewGuid().ToString("N") + ".png";
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture(capture);
                TestContext.WriteLine(capture);
                yield return null; yield return null;
            }
        }

        private IEnumerator AnalysisPhoneTap(Button button, bool enabled = true)
        {
            yield return AnalysisPhoneReveal(button);
            AssertReachable(button); UatSafeAction(button);
            Assert.That(button.worldBound.width*controller.GetComponent<UIDocument>().panelSettings.scale, Is.GreaterThanOrEqualTo(47.99),button.name);
            Assert.That(button.enabledInHierarchy, Is.EqualTo(enabled),button.name);
            yield return CompositionTap(CompositionButtonScreen(button));
            yield return AnalysisPhoneSettle();
        }

        private IEnumerator AnalysisPhoneCapture(string context)
        {
            string name = "analysis-phone-" + context + "-" + controller.TextSize + "-" + Guid.NewGuid().ToString("N") + ".png";
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture(name);
            TestContext.WriteLine(name);
            yield return null; yield return null;
        }

        private IEnumerator AnalysisPhoneOpenFromEdit()
        {
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            yield return AnalysisPhoneTap(ui.Q<Button>("displaySettings"));
            Assert.That(ui.Q("displayPopover").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            yield return AnalysisPhoneTap(ui.Q<Button>("openAnalysis"));
            Assert.That(ui.Q("modal").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
        }

        private IEnumerator AnalysisPhoneDialog(string reasonKey, params string[] choices)
        {
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var scroll = ui.Q<Button>("confirm").GetFirstAncestorOfType<ScrollView>();
            Assert.That(scroll.verticalScroller.highValue, Is.GreaterThan(0),"Expanded test localization must exercise bounded dialog scrolling");
            var text = ui.Q<Label>("modalText");
            Assert.That(text.text, Does.Not.Contain("ui.analysis."));
            Assert.That(text.worldBound.xMin, Is.GreaterThanOrEqualTo(scroll.contentViewport.worldBound.xMin-1));
            Assert.That(text.worldBound.xMax, Is.LessThanOrEqualTo(scroll.contentViewport.worldBound.xMax+1));
            scroll.scrollOffset = Vector2.zero;
            yield return AnalysisPhoneSettle();
            Assert.That(text.worldBound.Overlaps(scroll.contentViewport.worldBound), Is.True);
            if (reasonKey != null)
            {
                var reason = ui.Q<Label>("navigationStatus");
                Assert.That(reason.text, Is.EqualTo(GameRoot.Instance.Content.GetString(reasonKey,null)));
                yield return AnalysisPhoneReveal(reason);
                Assert.That(reason.worldBound.Overlaps(scroll.contentViewport.worldBound), Is.True);
                Assert.That(reason.worldBound.xMax, Is.LessThanOrEqualTo(scroll.contentViewport.worldBound.xMax+1));
            }
            foreach (string choice in choices)
            { yield return AnalysisPhoneReveal(ui.Q<Button>(choice)); AssertReachable(ui.Q<Button>(choice)); UatSafeAction(ui.Q<Button>(choice)); }
            TestContext.WriteLine("Phone dialog " + controller.TextSize + " scroll range=" + scroll.verticalScroller.highValue);
            yield return AnalysisPhoneCapture("dialog");
        }

        private System.Collections.Generic.Dictionary<string,string> AnalysisPhoneStrings =>
            (System.Collections.Generic.Dictionary<string,string>)typeof(ContentService)
                .GetField("_stringMap",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(GameRoot.Instance.Content);

        [UnityTest] public IEnumerator AnalysisPhoneNavigationSafeAreaAndCoveredInput()
        {
            var root = GameRoot.Instance; root.Save.structureRuntime.ManaReserve = 1000;
            Assert.That(root.SimulateRunOnce(), Is.True);
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            yield return StabilizeReadOnlySceneBaseline(); var before = File.ReadAllBytes(root.SaveService.SavePath); string live = JsonUtility.ToJson(root.Save);
            foreach (var size in new[] {DungeonTextSize.Default,DungeonTextSize.Large})
            {
                yield return AnalysisPhoneConfigure(size);
                yield return AnalysisPhoneTap(ui.Q<Button>("displaySettings"));
                yield return AnalysisPhoneTap(ui.Q<Button>(size==DungeonTextSize.Large ? "large" : "default"));
                Assert.That(controller.TextSize, Is.EqualTo(size));
                yield return AnalysisPhoneTap(ui.Q<Button>("closeDisplay"));
                var covered = new[] {ui.Q<Button>("edit"),ui.Q<Button>("replayRun"),ui.Q<Button>("collapseFloors")};
                var points = covered.Select(CompositionButtonScreen).ToArray();
                var mapPoint = controller.Viewport.ScreenRect.center;
                bool rail = controller.FloorRailCollapsed; string floor = controller.SelectedFloorInstanceId;
                int activated = 0; Action observed = () => activated++;
                foreach (var button in covered) button.clicked += observed;
                try
                {
                    yield return AnalysisPhoneTap(ui.Q<Button>("openAnalysis")); Assert.That(controller.IsAnalysis, Is.True);
                    yield return AnalysisPhoneCapture("analysis");
                    foreach (var button in covered) Assert.That(button.enabledInHierarchy, Is.False);
                    foreach (var point in points.Append(mapPoint)) yield return CompositionTap(point);
                    Assert.That(activated, Is.Zero); Assert.That(controller.IsAnalysis, Is.True);
                    Assert.That(controller.IsEditing, Is.False); Assert.That(controller.Replay, Is.Null);
                    Assert.That(controller.FloorRailCollapsed, Is.EqualTo(rail));
                    Assert.That(controller.SelectedFloorInstanceId, Is.EqualTo(floor));
                    yield return AnalysisPhoneTap(ui.Q<Button>("analysisDisplay"));
                    yield return AnalysisPhoneTap(ui.Q<Button>(size==DungeonTextSize.Large ? "large" : "default"));
                    yield return AnalysisPhoneTap(ui.Q<Button>("closeDisplay"));
                    yield return AnalysisPhoneTap(ui.Q<Button>("analysisDungeon")); Assert.That(controller.IsAnalysis, Is.False);
                    yield return AnalysisPhoneReveal(ui.Q<Button>("openAnalysis")); AssertReachable(ui.Q<Button>("openAnalysis"));
                    foreach (var button in covered) Assert.That(button.enabledInHierarchy, Is.True);
                    yield return AnalysisPhoneCapture("dungeon");
                }
                finally { foreach (var button in covered) button.clicked -= observed; }
                CollectionAssert.AreEqual(before,File.ReadAllBytes(root.SaveService.SavePath)); Assert.That(JsonUtility.ToJson(root.Save), Is.EqualTo(live));
            }
        }

        [UnityTest] public IEnumerator AnalysisPhoneValidDraftChoicesUseTouchButtons()
        {
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var strings = AnalysisPhoneStrings; string original = strings["ui.analysis.leave_edit"];
            try
            {
                strings["ui.analysis.leave_edit"] = original + " " + string.Join(" ",Enumerable.Repeat("Test-only extended localized review information.",24));
                foreach (var size in new[] {DungeonTextSize.Default,DungeonTextSize.Large})
                foreach (string choice in new[] {"cancel","navigationDiscard","confirm"})
                {
                    yield return AnalysisPhoneConfigure(size); yield return AnalysisModifiedDraft();
                    Assert.That(controller.IsEditing, Is.True); Assert.That(simulatedTouch, Is.Not.Null);
                    var draft = controller.Draft; var before = File.ReadAllBytes(GameRoot.Instance.SaveService.SavePath);
                    int rooms = GameRoot.Instance.Save.validatedCanonicalSpatialState.Floors.Sum(f=>f.Layout.Rooms.Length);
                    yield return AnalysisPhoneOpenFromEdit();
                    Assert.That(ui.Q<Button>("confirm").text, Is.EqualTo(strings["ui.dungeon.save"]));
                    Assert.That(ui.Q<Button>("navigationDiscard").text, Is.EqualTo(strings["ui.analysis.discard_changes"]));
                    Assert.That(ui.Q<Button>("cancel").text, Is.EqualTo(strings["ui.analysis.continue_editing"]));
                    yield return AnalysisPhoneDialog(null,"confirm","navigationDiscard","cancel");
                    yield return AnalysisPhoneTap(ui.Q<Button>(choice));
                    Assert.That(ui.Q("modal").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                    if (choice=="cancel")
                    {
                        Assert.That(controller.IsAnalysis, Is.False); Assert.That(controller.Draft, Is.SameAs(draft));
                        CollectionAssert.AreEqual(before,File.ReadAllBytes(GameRoot.Instance.SaveService.SavePath));
                        yield return AnalysisPhoneTap(ui.Q<Button>("closeDisplay")); Assert.That(controller.Discard(), Is.True);
                    }
                    else
                    {
                        Assert.That(controller.IsAnalysis, Is.True); Assert.That(controller.IsEditing, Is.False);
                        Assert.That(GameRoot.Instance.SaveService.CreateDungeonDraftStore().Read(), Is.Null);
                        if (choice=="navigationDiscard") CollectionAssert.AreEqual(before,File.ReadAllBytes(GameRoot.Instance.SaveService.SavePath));
                        else Assert.That(GameRoot.Instance.Save.validatedCanonicalSpatialState.Floors.Sum(f=>f.Layout.Rooms.Length), Is.EqualTo(rooms+1));
                        yield return AnalysisPhoneTap(ui.Q<Button>("analysisDungeon"));
                    }
                }
            }
            finally { strings["ui.analysis.leave_edit"] = original; }
        }

        [UnityTest] public IEnumerator AnalysisPhoneInvalidAndStaleDialogsRemainAccessible()
        {
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var strings = AnalysisPhoneStrings; string invalidText = strings["ui.analysis.leave_invalid"];
            string staleText = strings[TransactionalDungeonDraft.StaleReason];
            var stringOwner = GameRoot.Instance.Content;
            var rootOwner = GameRoot.Instance;
            int phoneTraceCount = 0;
            void TracePhone(string phase)
            {
                if (phoneTraceCount++ >= 128) return;
                int Id(object value) => value == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
                var currentRoot = GameRoot.Instance;
                TestContext.WriteLine("H1F4_ANALYSIS_PHONE " + phase + " root=" + Id(currentRoot) +
                    " owner=" + Id(rootOwner) + " content=" + Id(currentRoot?.Content) + " contentOwner=" + Id(stringOwner) +
                    " strings=" + Id(strings) + " document=" + Id(ui) + " attached=" + (ui?.panel != null) +
                    " controller=" + Id(controller) + " draft=" + Id(controller?.Draft) +
                    " recovery=" + currentRoot?.TimeService?.RecoveryClockActive + " episode=" + currentRoot?.Save?.timeProvenance?.Episode?.Phase);
                if (phase != "before-restore") return;
                object result = TestContext.CurrentContext.Result;
                var assertions = result.GetType().GetProperty("Assertions")?.GetValue(result) as System.Collections.IEnumerable;
                if (assertions == null) return;
                int count = 0;
                foreach (object assertion in assertions) {
                    if (count++ == 3) break;
                    TestContext.WriteLine("H1F4_ANALYSIS_PHONE recorded-assertion " +
                        assertion.GetType().GetProperty("Message")?.GetValue(assertion) + " " +
                        assertion.GetType().GetProperty("StackTrace")?.GetValue(assertion));
                }
            }
            TracePhone("entry");
            try
            {
                strings["ui.analysis.leave_invalid"] = invalidText + " " + string.Join(" ",Enumerable.Repeat("Test-only expanded invalid draft guidance.",24));
                strings[TransactionalDungeonDraft.StaleReason] = staleText + " " + string.Join(" ",Enumerable.Repeat("Test-only expanded stale draft guidance.",24));
                foreach (var size in new[] {DungeonTextSize.Default,DungeonTextSize.Large})
                {
                    TracePhone("before-1:AnalysisPhoneConfigure"); yield return AnalysisPhoneConfigure(size); TracePhone("after-1:AnalysisPhoneConfigure"); controller.EnterEdit();
                    var root = GameRoot.Instance; var floor = root.Save.validatedCanonicalSpatialState.Floors[0]; var room = floor.Layout.Rooms[0];
                    controller.TapWorld(new TileCoordinate(room.Anchor.X+2,room.Anchor.Y+2)); controller.BeginMove();
                    controller.TapWorld(new TileCoordinate(-1,-1)); TracePhone("before-2:null"); yield return null; TracePhone("after-2:null");
                    var draft = controller.Draft; Assert.That(draft.IsStructurallyValid, Is.False);
                    TracePhone("before-3:StabilizeReadOnlySceneBaseline"); yield return StabilizeReadOnlySceneBaseline(); TracePhone("after-3:StabilizeReadOnlySceneBaseline"); var before = File.ReadAllBytes(root.SaveService.SavePath);
                    TracePhone("before-4:AnalysisPhoneOpenFromEdit"); yield return AnalysisPhoneOpenFromEdit(); TracePhone("after-4:AnalysisPhoneOpenFromEdit");
                    Assert.That(ui.Q<Button>("confirm").text, Is.EqualTo(strings["ui.analysis.discard_changes"]));
                    Assert.That(ui.Q<Button>("navigationDiscard").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                    TracePhone("before-5:AnalysisPhoneDialog"); yield return AnalysisPhoneDialog(draft.StructuralReason,"confirm","cancel"); TracePhone("after-5:AnalysisPhoneDialog");
                    TracePhone("before-6:AnalysisPhoneTap"); yield return AnalysisPhoneTap(ui.Q<Button>("cancel")); TracePhone("after-6:AnalysisPhoneTap"); Assert.That(controller.Draft, Is.SameAs(draft));
                    TracePhone("before-7:AnalysisPhoneTap"); yield return AnalysisPhoneTap(ui.Q<Button>("closeDisplay")); TracePhone("after-7:AnalysisPhoneTap");
                    TracePhone("before-8:AnalysisPhoneOpenFromEdit"); yield return AnalysisPhoneOpenFromEdit(); TracePhone("after-8:AnalysisPhoneOpenFromEdit"); TracePhone("before-9:AnalysisPhoneTap"); yield return AnalysisPhoneTap(ui.Q<Button>("confirm")); TracePhone("after-9:AnalysisPhoneTap");
                    Assert.That(controller.IsAnalysis, Is.True); CollectionAssert.AreEqual(before,File.ReadAllBytes(root.SaveService.SavePath));
                    TracePhone("before-10:AnalysisPhoneTap"); yield return AnalysisPhoneTap(ui.Q<Button>("analysisDungeon")); TracePhone("after-10:AnalysisPhoneTap");

                    controller.EnterEdit(); draft = controller.Draft; var assignment = floor.RoomContents.Assignments.First();
                    Assert.That(draft.Move(floor.FloorInstanceId,assignment.RoomInstanceId,assignment.AssignmentId,new TileCoordinate(1,0)), Is.True);
                    TracePhone("before-11:null"); yield return null; TracePhone("after-11:null"); TracePhone("before-12:null"); yield return null; TracePhone("after-12:null");
                    var otherStore = new FileDungeonDraftStore(root.SaveService.SavePath+".phone-stale",
                        SpatialMigrationFileSystemSelector.Evaluate(root.SaveService.SavePath).FileSystem,root.SaveSpatialMigrationLimits);
                    var other = TransactionalDungeonDraft.Create(root.Save.validatedCanonicalSpatialState,root.SaveService.DungeonDraftContext,otherStore);
                    var competingTarget = assignment.RoomLocalPosition.Equals(new TileCoordinate(2,1)) ? new TileCoordinate(0,0) : new TileCoordinate(2,1);
                    Assert.That(other.Move(floor.FloorInstanceId,assignment.RoomInstanceId,assignment.AssignmentId,competingTarget), Is.True);
                    Assert.That(other.FlushNext(), Is.True); Assert.That(root.SaveService.CommitDungeonDraft(root.Save,other).IsSuccess, Is.True);
                    TracePhone("before-13:StabilizeReadOnlySceneBaseline"); yield return StabilizeReadOnlySceneBaseline(); TracePhone("after-13:StabilizeReadOnlySceneBaseline"); before = File.ReadAllBytes(root.SaveService.SavePath);
                    TracePhone("before-14:AnalysisPhoneOpenFromEdit"); yield return AnalysisPhoneOpenFromEdit(); TracePhone("after-14:AnalysisPhoneOpenFromEdit"); Assert.That(ui.Q<Button>("confirm").enabledInHierarchy, Is.False);
                    TracePhone("before-15:AnalysisPhoneDialog"); yield return AnalysisPhoneDialog(TransactionalDungeonDraft.StaleReason,"confirm","navigationDiscard","cancel"); TracePhone("after-15:AnalysisPhoneDialog");
                    TracePhone("before-16:AnalysisPhoneTap"); yield return AnalysisPhoneTap(ui.Q<Button>("confirm"),false); TracePhone("after-16:AnalysisPhoneTap");
                    Assert.That(ui.Q("modal").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex)); Assert.That(controller.Draft, Is.SameAs(draft));
                    TracePhone("before-17:AnalysisPhoneTap"); yield return AnalysisPhoneTap(ui.Q<Button>("cancel")); TracePhone("after-17:AnalysisPhoneTap"); Assert.That(controller.Draft, Is.SameAs(draft));
                    TracePhone("before-18:AnalysisPhoneTap"); yield return AnalysisPhoneTap(ui.Q<Button>("closeDisplay")); TracePhone("after-18:AnalysisPhoneTap");
                    TracePhone("before-19:AnalysisPhoneOpenFromEdit"); yield return AnalysisPhoneOpenFromEdit(); TracePhone("after-19:AnalysisPhoneOpenFromEdit"); TracePhone("before-20:AnalysisPhoneTap"); yield return AnalysisPhoneTap(ui.Q<Button>("navigationDiscard")); TracePhone("after-20:AnalysisPhoneTap");
                    Assert.That(controller.IsAnalysis, Is.True); CollectionAssert.AreEqual(before,File.ReadAllBytes(root.SaveService.SavePath));
                    TracePhone("before-21:AnalysisPhoneTap"); yield return AnalysisPhoneTap(ui.Q<Button>("analysisDungeon")); TracePhone("after-21:AnalysisPhoneTap");
                }
            }
            finally {
                TracePhone("before-restore");
                strings["ui.analysis.leave_invalid"]=invalidText; strings[TransactionalDungeonDraft.StaleReason]=staleText;
                Assert.That(GameRoot.Instance, Is.SameAs(rootOwner));
                Assert.That(GameRoot.Instance.Content, Is.SameAs(stringOwner));
                Assert.That(AnalysisPhoneStrings, Is.SameAs(strings));
                Assert.That(controller.GetComponent<UIDocument>().rootVisualElement, Is.SameAs(ui));
                Assert.That(strings["ui.analysis.leave_invalid"], Is.EqualTo(invalidText));
                Assert.That(strings[TransactionalDungeonDraft.StaleReason], Is.EqualTo(staleText));
                TracePhone("restored");
            }
        }

        [UnityTest] public IEnumerator AnalysisPhoneOpeningDuringReplayClosesPresentationOnly()
        {
            var root = GameRoot.Instance; root.Save.structureRuntime.ManaReserve = 1000;
            Assert.That(root.SimulateRunOnce(), Is.True); var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            yield return StabilizeReadOnlySceneBaseline(); var before = File.ReadAllBytes(root.SaveService.SavePath); string live = JsonUtility.ToJson(root.Save);
            foreach (var size in new[] {DungeonTextSize.Default,DungeonTextSize.Large})
            {
                yield return AnalysisPhoneConfigure(size);
                string normalSummary = ui.Q<Label>("performanceSummary").text;
                yield return AnalysisPhoneTap(ui.Q<Button>("replayRun")); Assert.That(controller.Replay, Is.Not.Null);
                Assert.That(ui.Q<Label>("performanceSummary").ClassListContains("replay-summary"), Is.True);
                yield return AnalysisPhoneTap(ui.Q<Button>("openAnalysis"));
                Assert.That(controller.IsAnalysis, Is.True); Assert.That(controller.Replay, Is.Null);
                Assert.That(ui.Q<Label>("performanceSummary").ClassListContains("replay-summary"), Is.False);
                Assert.That(ui.Q("replayPanel").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                yield return AnalysisPhoneTap(ui.Q<Button>("analysisDungeon"));
                Assert.That(controller.Replay, Is.Null); Assert.That(controller.IsAnalysis, Is.False);
                Assert.That(ui.Q<Label>("performanceSummary").text, Is.EqualTo(normalSummary));
                CollectionAssert.AreEqual(before,File.ReadAllBytes(root.SaveService.SavePath)); Assert.That(JsonUtility.ToJson(root.Save), Is.EqualTo(live));
            }
        }

        // A loaded Save is not a completed recovery boot. Only real lifecycle
        // observations and Update may satisfy the coordinator's existing barrier.
        private IEnumerator AwaitAnalysisBootstrap(SaveData source, MonsterRecoveryRules rules, byte[] sourceBytes)
        {
            var root = GameRoot.Instance; var time = root.TimeService; var service = root.SaveService;
            object Field(string name) => typeof(GameRoot).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root);
            Assert.That(source.timeProvenance.Episode, Is.Null);
            Assert.That(source.livingDungeon.IsClosed, Is.True); Assert.That(source.livingDungeon.Parties, Is.Empty);
            Assert.That(source.timeProvenance.AccountedBoundary.CompareTo(sceneClock.Now), Is.Zero,
                "This reopen has no elapsed eligible time; healing remains a separate canonical obligation");
            var expectedMonsters = source.monsterContinuity.Copy();
            double expectedWallet = MonsterContinuity.Recover(expectedMonsters, source.validatedCanonicalSpatialState,
                rules, source.structureRuntime.ManaReserve, new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal));
            Assert.That(MonsterContinuity.Recover(expectedMonsters, source.validatedCanonicalSpatialState, rules,
                expectedWallet, new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal)), Is.EqualTo(expectedWallet),
                "A second zero-duration settlement cannot repeat a completed readiness debit");
            string history = JsonUtility.ToJson(source.runHistory);
            long accepted = source.livingDungeon.Scheduler.AcceptedThrough, closed = source.livingDungeon.Scheduler.ClosedThrough;
            var utc = sceneClock.Now; long monotonic = sceneClock.Elapsed100ns; int observations = 0;
            bool Complete() => !(bool)Field("bootRecoveryPending") && !(bool)Field("foregroundPumping") &&
                !(bool)Field("foregroundDirty") && !service.CanonicalPublicationPending &&
                !time.RecoveryClockActive && !time.HasDeferredRecovery && root.Save.timeProvenance.Episode == null &&
                root.Save.livingDungeon.IsClosed && time.RecoveryForegroundObserved;
            const int maximumBootstrapFrames = 64; // Test orchestration bound; not a recovery/gameplay limit.
            for (int frame = 0; frame < maximumBootstrapFrames && !Complete(); frame++) {
                Assert.That(root.isActiveAndEnabled, Is.True); Assert.That(root.TimeService, Is.SameAs(time));
                Assert.That(typeof(SaveService).GetField("recoveryTime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service), Is.SameAs(time));
                Assert.That(Field("foregroundFailure"), Is.Null); Assert.That(Field("foregroundRetryPending"), Is.EqualTo(false));
                Assert.That(service.RequiresCanonicalRecovery || time.HasForegroundPersistenceStall, Is.False);
                if (!time.RecoveryForegroundObserved) {
                    // Establish the isolated scene's current foreground observation
                    // at the validated controlled cut; never backdate a callback.
                    typeof(GameRoot).GetMethod("OnApplicationPause", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(root, new object[] { false });
                    ObserveSceneFocus(true); observations++;
                }
                yield return null;
            }
            Assert.That(Complete(), Is.True, "Loaded root did not complete its real recovery boot within the fixture bound");
            Assert.That(Field("foregroundFailure"), Is.Null); Assert.That(Field("foregroundRetryPending"), Is.EqualTo(false));
            Assert.That(service.RequiresCanonicalRecovery || time.HasForegroundPersistenceStall, Is.False);
            Assert.That(sceneClock.Now, Is.EqualTo(utc)); Assert.That(sceneClock.Elapsed100ns, Is.EqualTo(monotonic));
            Assert.That(root.Save.timeProvenance.AccountedBoundary, Is.EqualTo(source.timeProvenance.AccountedBoundary));
            Assert.That(root.Save.timeProvenance.RecoveryContextSha256, Is.EqualTo(source.timeProvenance.RecoveryContextSha256));
            Assert.That(root.Save.totalTicks, Is.EqualTo(source.totalTicks));
            Assert.That(root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(accepted));
            Assert.That(root.Save.livingDungeon.Scheduler.ClosedThrough, Is.EqualTo(closed));
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(expectedWallet), "Only the configured canonical readiness spending is permitted");
            Assert.That(JsonUtility.ToJson(root.Save.monsterContinuity), Is.EqualTo(JsonUtility.ToJson(expectedMonsters)));
            Assert.That(JsonUtility.ToJson(root.Save.runHistory), Is.EqualTo(history), "Boot cannot replay or lose a confirmed run/reward");
            var bytes = File.ReadAllBytes(service.SavePath);
            CollectionAssert.AreEqual(service.CanonicalSession.GetCurrentBytes(), bytes);
            TestContext.WriteLine("H1F4_ANALYSIS_BOOT complete=True foregroundObservations=" + observations +
                " beforeSha=" + SpatialContractSha256.Compute(sourceBytes) + " afterSha=" + SpatialContractSha256.Compute(bytes) +
                " walletMatchesConfiguredRecovery=True WPreserved=True ACPreserved=True historyPreserved=True");
        }

        [UnityTest] public IEnumerator AnalysisNavigationRetainsFloorAndReadOnlySaveAcrossShellReopen()
        {
            var root = GameRoot.Instance;
            root.Save.structureRuntime.ManaReserve = 1000;
            Assert.That(root.SimulateRunOnce(), Is.True);
            yield return StabilizeReadOnlySceneBaseline(); var before = File.ReadAllBytes(root.SaveService.SavePath);
            var history = JsonUtility.ToJson(root.Save.runHistory);
            string floor = controller.SelectedFloorInstanceId;
            controller.OpenAnalysis(); yield return null;
            Assert.That(controller.IsAnalysis, Is.True);
            controller.EnterEdit(); Assert.That(controller.IsEditing, Is.False);
            controller.ReturnToDungeon(); yield return null;
            Assert.That(controller.SelectedFloorInstanceId, Is.EqualTo(floor));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(root.SaveService.SavePath));
            var recoverySource = root.Save;
            var recoveryRules = ((DetachedCurrentTargetValidationContext)typeof(SaveService).GetField("_validationContext",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root.SaveService)).MonsterRecovery;
            UnityEngine.Object.Destroy(controller.gameObject);
            UnityEngine.Object.Destroy(root.gameObject); yield return null;
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
            for (int i=0;i<120 && GameRoot.Instance?.Save==null;i++) yield return null;
            root = GameRoot.Instance;
            controller = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();
            for (int i=0;i<30 && controller.World==null;i++) yield return null;
            yield return AwaitAnalysisBootstrap(recoverySource, recoveryRules, before);
            // Boot owns its clock/save metadata; Analysis must preserve the reopened bytes.
            yield return StabilizeReadOnlySceneBaseline(); before = File.ReadAllBytes(root.SaveService.SavePath);
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
