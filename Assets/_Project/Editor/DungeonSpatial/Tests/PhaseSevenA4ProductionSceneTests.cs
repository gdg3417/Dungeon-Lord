#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
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

    public abstract partial class PhaseSevenA4ProductionSceneTests
    {
        private string filename;
        private TextAsset disposableConfig;
        private TextAsset disposableRunScenarioConfig;
        private int combatFeedbackScenario;
        private int isolationCallbacks;
        private ProductionDungeonController controller;
        private Touchscreen simulatedTouch;
        private bool missingThemeWarning;
        private SceneClock sceneClock;
        private int tracedPublications;
        private byte[] readOnlyBaselineProbe;
        private int readOnlyBaselineOrdinal;
        internal sealed class SceneClock : IPreciseTimeSource
        {
            internal UnixTimePoint Now = new UnixTimePoint(2000, 2500000);
            internal long Elapsed100ns;
            internal bool InvalidUtc, InvalidMonotonic;
            public UnixTimePoint UtcNow() => InvalidUtc ? new UnixTimePoint(0, 0) : Now;
            public long UtcNowUnixSeconds() => UtcNow().Seconds;
            internal double Monotonic() => InvalidMonotonic ? double.NaN : Elapsed100ns / (double)UnixTimePoint.UnitsPerSecond;
            internal void Advance(double seconds)
            {
                Assert.That(double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0, Is.False);
                long ticks = checked((long)Math.Round(seconds * UnixTimePoint.UnitsPerSecond));
                Now = Now.Add100ns(ticks); Elapsed100ns = checked(Elapsed100ns + ticks);
            }
        }
        private bool TraceSceneRecovery => TestContext.CurrentContext.Test.Name.Contains("RoomContentMapReassignmentCapacity") ||
            TestContext.CurrentContext.Test.Name.Contains("SceneClock") ||
            TestContext.CurrentContext.Test.Name.Contains("LivingDungeonB3B2FramesBeforeCachedBoundaryDoNotReconstructOrPublish");
        // Read-only diagnosis: never sample/advance time, invoke a barrier, or
        // serialize a mutable runtime back into the canonical owner.
        private void TraceSceneState(string phase)
        {
            if (!TraceSceneRecovery) return;
            object Field(object value, string name) => value?.GetType().GetField(name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(value);
            int Id(object value) => value == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
            var root = GameRoot.Instance; var service = root?.SaveService; var save = root?.Save;
            var time = root?.TimeService; var session = service?.CanonicalSession; var draft = controller?.Draft;
            TestContext.WriteLine("H1F4_SCENE " + phase + " frame=" + Time.frameCount +
                " controller=" + Id(controller) + " initialized=" + Field(controller, "initialized") + " quiesced=" + controller?.IsQuiesced +
                " draft=" + Id(draft) + " editing=" + controller?.IsEditing + " durability=" + draft?.Durability +
                " commands=" + draft?.CommandCount + " draftReason=" + draft?.Reason + " feedback=" + Field(controller, "feedback") +
                " rootEnabled=" + root?.enabled + " focused=" + Application.isFocused + " observedFocus=" + Field(root, "foregroundApplicationFocused") +
                " observedPause=" + Field(root, "foregroundApplicationPaused") + " paused=" + time?.IsPaused +
                " time=" + Id(time) + " boundTime=" + Id(Field(service, "recoveryTime")) + " recovery=" + time?.RecoveryClockActive +
                " deferred=" + time?.HasDeferredRecovery + " deferredBackground=" + time?.HasDeferredBackground +
                " observedForeground=" + time?.RecoveryForegroundObserved + " presentation=" + time?.ForegroundPresentationActive +
                " rootPumping=" + Field(root, "foregroundPumping") + " bootPending=" + Field(root, "bootRecoveryPending") +
                " failure=" + Field(root, "foregroundFailure") +
                " retry=" + Field(root, "foregroundRetryPending") + " publication=" + service?.CanonicalPublicationPending +
                " recoveryRequired=" + service?.RequiresCanonicalRecovery + " save=" + Id(save) + " session=" + Id(session) +
                " bytes=" + (session == null ? "null" : SpatialContractSha256.Compute(session.GetCurrentBytes())) +
                " W=" + save?.timeProvenance?.AccountedBoundary?.Seconds + "." + save?.timeProvenance?.AccountedBoundary?.Fraction100ns +
                " A=" + save?.livingDungeon?.Scheduler?.AcceptedThrough + " C=" + save?.livingDungeon?.Scheduler?.ClosedThrough +
                " banner=" + root?.BannerMessage + " pauseLine=" + root?.PauseLine +
                " mouseUp=" + Mouse.current?.leftButton.wasReleasedThisFrame + " escape=" + Keyboard.current?.escapeKey.wasPressedThisFrame);
        }
        private void RecordThemeWarning(string message, string stack, LogType type)
        { if (message.Contains("No Theme Style Sheet set to PanelSettings")) missingThemeWarning = true; }
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            tracedPublications = 0;
            readOnlyBaselineProbe = null; readOnlyBaselineOrdinal = 0;
            missingThemeWarning = false;
            Application.logMessageReceived += RecordThemeWarning;
            if (!Application.isPlaying)
            {
                // Entering play can reload the domain and clear fixture fields/callbacks.
                // Start from an empty scene so no root can boot before isolation is armed.
                UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                    UnityEditor.SceneManagement.NewSceneMode.Single);
                yield return new EnterPlayMode();
            }
            if (GameRoot.Instance != null) { UnityEngine.Object.Destroy(GameRoot.Instance.gameObject); yield return null; }
            sceneClock = new SceneClock();
            filename = "phase7a4-scene-" + Guid.NewGuid().ToString("N") + ".json";
            SceneManager.sceneLoaded += Isolate;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
            // A successive isolated boot can still expose the departing root's
            // loaded save before the new scene's Start. Wait for this boot's
            // owned save, rather than treating any loaded Save as readiness.
            for (int i = 0; i < 120 && (GameRoot.Instance?.Save == null ||
                Path.GetFileName(GameRoot.Instance.SaveService?.SavePath) != filename); i++) yield return null;
            var root = GameRoot.Instance; Assert.That(root?.Save, Is.Not.Null);
            Assert.That(Path.GetFileName(root.SaveService.SavePath), Is.EqualTo(filename), "Disposable scene save isolation");
            PumpSceneToClosed(establishForeground: true);
            if (TestContext.CurrentContext.Test.Name == nameof(NativeConstructionReachesUninvestedMiddleDoorway) ||
                TestContext.CurrentContext.Test.Name == nameof(NativeConstructionAuthorsMiddleInsertionAndReopens) ||
                TestContext.CurrentContext.Test.Name == nameof(ManipulatesFloorTwoDoorwaysSectionsBendsAndTiles) ||
                TestContext.CurrentContext.Test.Name == nameof(CombinesBothCornerDoorwaysThroughMapAndSavesConvertedConnection))
            {
                controller = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();
                for (int i = 0; i < 30 && controller.World == null; i++) yield return null;
                Assert.That(controller.World, Is.Not.Null);
                yield break;
            }
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
            if (TraceSceneRecovery) {
                root.SaveService.CanonicalRuntimePublished += _ => {
                    if (tracedPublications++ >= 16) return;
                    TraceSceneState("canonical-publication");
                };
                var ui = controller.GetComponent<UIDocument>().rootVisualElement;
                foreach (string name in new[] { "save", "discard", "confirm", "cancel", "edit", "legacy" })
                    ui.Q<Button>(name).clicked += () => TraceSceneState("ui-click-" + name);
            }
            // The controlled provider is stationary until an explicit Advance.
            // Keep the real root enabled instead of fabricating a suspension.
            TraceSceneState("setup.prebound-clock-ready");
            yield return null;
            TraceSceneState("setup.after-yield");
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
            if (disposableConfig != null) UnityEngine.Object.DestroyImmediate(disposableConfig);
            disposableConfig = new TextAsset(JsonUtility.ToJson(config)); root.buildConfigJson = disposableConfig;
            Assert.Throws<ArgumentException>(() => root.InjectSceneRecoveryClockForTests(null, sceneClock.Monotonic));
            Assert.Throws<ArgumentException>(() => root.InjectSceneRecoveryClockForTests(sceneClock, null));
            // Exercise the non-isolated save refusal before any provider is owned.
            var validConfig = disposableConfig;
            config.save.fileName = "save_primary.json";
            var invalid = new TextAsset(JsonUtility.ToJson(config)); root.buildConfigJson = invalid;
            Assert.Throws<ArgumentException>(() => root.InjectSceneRecoveryClockForTests(sceneClock, sceneClock.Monotonic));
            root.buildConfigJson = validConfig; UnityEngine.Object.DestroyImmediate(invalid);
            config.save.fileName = filename;
            byte[] one = null, two = null;
            string test = TestContext.CurrentContext.Test.Name;
            if (isolationCallbacks == 1 && (test == nameof(ManipulatesFloorTwoDoorwaysSectionsBendsAndTiles) ||
                test == nameof(CombinesBothCornerDoorwaysThroughMapAndSavesConvertedConnection)))
            {
                string path = Path.Combine(Application.persistentDataPath, filename);
                Assert.That(File.Exists(path), Is.False);
                Directory.CreateDirectory(Application.persistentDataPath);
                var fixture = EditMode.LivingDungeonTwoFloorTests.CreateAmendmentAOwnerFloorTwo(sceneClock.UtcNowUnixSeconds());
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
                {
                    byte[] bytes = fixture.Session.GetCurrentBytes();
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
            AuthorCombatSceneConfiguration(root, test);
            if (test.Contains("ResumeCompletesSeveralBoundedPasses") || test.Contains("SuspendedDrainRecognizes") ||
                test.Contains("ProvenRollbackRetriesThroughRealSaveBarrier") || test.Contains("UncertainRollbackKeepsRealRootRecoveryLatch")) {
                byte[] Bounded(string name) {
                    string original = File.ReadAllText("Assets/_Project/Resources/" + name + ".json");
                    string bounded = original.Replace("\"MaximumBoundariesPerPump\": 64", "\"MaximumBoundariesPerPump\": 1");
                    Assert.That(bounded, Is.Not.EqualTo(original)); return System.Text.Encoding.UTF8.GetBytes(bounded);
                }
                one = Bounded("living_dungeon_foreground"); two = Bounded("living_dungeon_foreground_v2");
            }
            root.InjectSceneRecoveryClockForTests(sceneClock, sceneClock.Monotonic, one, two);
            Assert.Throws<ArgumentException>(() => root.InjectSceneRecoveryClockForTests(sceneClock, sceneClock.Monotonic));
            Assert.That(root.TimeService, Is.Null, "Injection supplies providers, never an early/replacement owner");
            TestContext.WriteLine("Isolated scene boot " + isolationCallbacks + ": " + config.save.fileName);
        }
        private void ObserveSceneFocus(bool focused) => typeof(GameRoot).GetMethod("OnApplicationFocus",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(GameRoot.Instance, new object[] { focused });

        // Finish only the legitimate pending foreground publication through the
        // normal Update lifecycle. Never pump private methods or change time.
        private IEnumerator StabilizeReadOnlySceneBaseline()
        {
            var root = GameRoot.Instance; var service = root.SaveService; var time = root.TimeService;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            object RootField(string name) => typeof(GameRoot).GetField(name, flags).GetValue(root);
            bool Pending() => (bool)RootField("foregroundDirty") || (bool)RootField("foregroundPumping") ||
                (bool)RootField("bootRecoveryPending") || service.CanonicalPublicationPending;
            void Healthy()
            {
                Assert.That(root.isActiveAndEnabled, Is.True, "Baseline keeps the real root running");
                Assert.That(RootField("foregroundFailure"), Is.Null);
                Assert.That(RootField("foregroundRetryPending"), Is.EqualTo(false));
                Assert.That(time.RecoveryClockActive || time.HasDeferredRecovery || time.HasForegroundPersistenceStall ||
                    service.RequiresCanonicalRecovery, Is.False, "Resolve lifecycle/recovery obligations before a read-only baseline");
                Assert.That(root.Save.timeProvenance.Episode, Is.Null);
                Assert.That(root.Save.livingDungeon.IsClosed, Is.True);
                Assert.That(root.TimeService, Is.SameAs(time));
                Assert.That(typeof(SaveService).GetField("recoveryTime", flags).GetValue(service), Is.SameAs(time));
            }
            Healthy();
            readOnlyBaselineOrdinal++;
            var before = File.ReadAllBytes(service.SavePath);
            CollectionAssert.AreEqual(service.CanonicalSession.GetCurrentBytes(), before);
            string gameplay = JsonUtility.ToJson(root.Save);
            var utc = sceneClock.Now; long monotonic = sceneClock.Elapsed100ns, through = time.ForegroundThrough;
            bool wasPending = Pending(); int publications = 0;
            Action<SaveData> published = _ => publications++;
            service.CanonicalRuntimePublished += published;
            try
            {
                const int maximumPublicationFrames = 8; // Test orchestration bound, never gameplay tuning.
                for (int frame = 0; frame < maximumPublicationFrames && Pending(); frame++) {
                    yield return null;
                    Healthy();
                }
                Assert.That(Pending(), Is.False, "Pending foreground publication did not complete within the fixture bound");
            }
            finally { service.CanonicalRuntimePublished -= published; }
            Healthy();
            Assert.That(publications, Is.EqualTo(wasPending ? 1 : 0), "Exactly one pending publication; no frame retry loop");
            Assert.That(sceneClock.Now, Is.EqualTo(utc)); Assert.That(sceneClock.Elapsed100ns, Is.EqualTo(monotonic));
            Assert.That(time.ForegroundThrough, Is.EqualTo(through));
            Assert.That(JsonUtility.ToJson(root.Save), Is.EqualTo(gameplay),
                "Stabilization cannot change wallet, ticks, W, accepted/closed work, rewards or recovery state");
            var after = File.ReadAllBytes(service.SavePath);
            CollectionAssert.AreEqual(service.CanonicalSession.GetCurrentBytes(), after, "Authoritative session and disk agree");
            Assert.That(TraceReadOnlyBaselineDifference(before, after, "stabilized"), Is.True,
                "Only unchanged bytes or the independently demonstrated mana decimal suffix is eligible");
            TestContext.WriteLine("H1F4_BASELINE publication-count=" + publications + " ordinal=" + readOnlyBaselineOrdinal +
                " gameplayUnchanged=True timeOwnerUnchanged=True clockUnchanged=True");
            readOnlyBaselineProbe = after;
        }

        private bool TraceReadOnlyBaselineDifference(byte[] before, byte[] after, string phase)
        {
            int first = 0;
            while (first < before.Length && first < after.Length && before[first] == after[first]) first++;
            string original = System.Text.Encoding.UTF8.GetString(before);
            int key = original.LastIndexOf("\"ManaReserve\":", System.Math.Min(first, original.Length - 1), StringComparison.Ordinal);
            bool walletNumber = key >= 0 && first >= key + "\"ManaReserve\":".Length && original.Substring(key + "\"ManaReserve\":".Length,
                first - key - "\"ManaReserve\":".Length).All(c => c == '-' || c >= '0' && c <= '9');
            bool decimalSuffixOnly = after.Length == before.Length + 2 && first < before.Length &&
                before[first] == (byte)',' && after[first] == (byte)'.' && after[first + 1] == (byte)'0' &&
                before.Skip(first).SequenceEqual(after.Skip(first + 2)) && walletNumber;
            TestContext.WriteLine("H1F4_BASELINE " + phase + " ordinal=" + readOnlyBaselineOrdinal +
                " identical=" + before.SequenceEqual(after) + " walletDecimalSuffixOnly=" + decimalSuffixOnly +
                " beforeLength=" + before.Length + " afterLength=" + after.Length + " first=" + first +
                " beforeSha=" + SpatialContractSha256.Compute(before) + " afterSha=" + SpatialContractSha256.Compute(after));
            return before.SequenceEqual(after) || decimalSuffixOnly;
        }

        private void PumpSceneToClosed(bool establishForeground = false)
        {
            var root = GameRoot.Instance;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            TraceSceneState("drain.begin");
            for (int pump = 0; pump < 128; pump++) {
                // Completing boot samples the platform's actual focus again.
                // Establish this fixture's foreground observation after that
                // initialization too, using callbacks at the same valid cut.
                if (establishForeground && !root.TimeService.RecoveryForegroundObserved) {
                    typeof(GameRoot).GetMethod("OnApplicationPause", flags).Invoke(root, new object[] { false });
                    typeof(GameRoot).GetMethod("OnApplicationFocus", flags).Invoke(root, new object[] { true });
                }
                typeof(GameRoot).GetMethod("UpdateForeground", flags).Invoke(root, null);
                Assert.That(typeof(GameRoot).GetField("foregroundFailure", flags).GetValue(root), Is.Null);
                if (root.Save.livingDungeon.IsClosed && !root.TimeService.RecoveryClockActive && !root.TimeService.HasDeferredRecovery) return;
            }
            TraceSceneState("drain.exhausted");
            Assert.Fail("Controlled scene failed to close its existing cut/recovery within the fixture bound");
        }

        [UnityTest] public IEnumerator SceneClockNativeFractionAndOwnerRemainBound()
        {
            var root = GameRoot.Instance; var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            Assert.That(root.TimeService, Is.SameAs(typeof(SaveService).GetField("recoveryTime", flags).GetValue(root.SaveService)));
            Assert.That(typeof(SaveService).GetField("_timeSource", flags).GetValue(root.SaveService), Is.SameAs(sceneClock));
            Assert.That(root.Save.timeProvenance.AccountedBoundary, Is.EqualTo(sceneClock.Now));
            Assert.That(root.Save.timeProvenance.AccountedBoundary.Fraction100ns, Is.EqualTo(2500000));
            var bytes = root.SaveService.CanonicalSession.GetCurrentBytes();
            Assert.Throws<ArgumentException>(() => root.InjectSceneRecoveryClockForTests(sceneClock, sceneClock.Monotonic));
            CollectionAssert.AreEqual(bytes, root.SaveService.CanonicalSession.GetCurrentBytes());
            yield return null;
        }
        [UnityTest] public IEnumerator SceneClockEligibilityRejectsOwnerProductAndNonBatchContext()
        {
            var method = typeof(GameRoot).GetMethod("IsIsolatedSceneClockContext", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method.Invoke(null, new object[] { "Dungeon Lord", true, filename }), Is.False);
            Assert.That(method.Invoke(null, new object[] { Application.productName, false, filename }), Is.False);
            Assert.That(method.Invoke(null, new object[] { Application.productName, true, "../" + filename }), Is.False);
            Assert.That(method.Invoke(null, new object[] { Application.productName, true, filename }), Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator SceneClockRepeatedFractionalOrdinaryCutsDoNotDuplicateCredit()
        {
            var root = GameRoot.Instance; double mana = root.Save.structureRuntime.ManaReserve; long ticks = root.Save.totalTicks;
            var bound = root.TimeService; sceneClock.Advance(.0000001);
            Assert.That(root.SaveService.Save(root.Save, SaveReason.ManualDev), Is.True);
            Assert.That(root.Save.timeProvenance.AccountedBoundary, Is.EqualTo(new UnixTimePoint(2000, 2500001)));
            for (int i = 0; i < 3; i++) Assert.That(root.SaveService.Save(root.Save, SaveReason.ManualDev), Is.True);
            Assert.That(root.TimeService, Is.SameAs(bound)); Assert.That(root.Save.totalTicks, Is.EqualTo(ticks));
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(mana));
            Assert.That(root.Save.timeProvenance.AccountedBoundary, Is.EqualTo(sceneClock.Now));
            yield return null;
        }
        [UnityTest] public IEnumerator SceneClockInvalidUtcFailsClosed() { yield return SceneClockInvalidObservationsFailClosed(true); }
        [UnityTest] public IEnumerator SceneClockInvalidMonotonicFailsClosed() { yield return SceneClockInvalidObservationsFailClosed(false); }
        private IEnumerator SceneClockInvalidObservationsFailClosed(bool utc)
        {
            var root = GameRoot.Instance; var before = root.SaveService.CanonicalSession.GetCurrentBytes();
            var wall = root.Save.timeProvenance.AccountedBoundary;
            try {
                sceneClock.InvalidUtc = utc; sceneClock.InvalidMonotonic = !utc;
                Assert.That(root.SaveService.Save(root.Save, SaveReason.ManualDev), Is.False);
                CollectionAssert.AreEqual(before, File.ReadAllBytes(root.SaveService.SavePath));
                Assert.That(root.Save.timeProvenance.AccountedBoundary, Is.EqualTo(wall));
            } finally { sceneClock.InvalidUtc = sceneClock.InvalidMonotonic = false; }
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (readOnlyBaselineProbe != null && GameRoot.Instance?.SaveService != null)
                TraceReadOnlyBaselineDifference(readOnlyBaselineProbe, File.ReadAllBytes(GameRoot.Instance.SaveService.SavePath), "probe-after-test");
            bool failed = TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed;
            if (failed && filename != null) {
                string export = Environment.GetEnvironmentVariable("DUNGEON_H1F4_TEST_EXPORT_ROOT");
                Assert.That(export, Is.Not.Null.And.Not.Empty, "Preserve failed scene inputs before cleanup");
                var output = new DirectoryInfo(Path.GetFullPath(export)); var stage = output.Parent; var campaign = stage?.Parent;
                Assert.That(output.Name, Is.EqualTo("fixture-inputs"));
                Assert.That(campaign?.Parent?.FullName, Is.EqualTo(Path.GetFullPath(Path.Combine(Application.dataPath,
                    "../TestResults/qualification-supervisor"))).IgnoreCase);
                Assert.That(Guid.TryParseExact(campaign?.Name, "N", out _), Is.True);
                foreach (var directory in new[] { output, stage, campaign, campaign.Parent })
                    Assert.That(directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) == 0, Is.True);
                string owned = Path.Combine(output.FullName, "failed-scene-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(owned);
                foreach (string path in Directory.GetFiles(Application.persistentDataPath, filename + "*"))
                    File.Copy(path, Path.Combine(owned, Path.GetFileName(path)), false);
                if (GameRoot.Instance?.SaveService?.CanonicalSession != null)
                    File.WriteAllBytes(Path.Combine(owned, "canonical-authority.json"), GameRoot.Instance.SaveService.CanonicalSession.GetCurrentBytes());
                File.WriteAllText(Path.Combine(owned, "identity.txt"), TestContext.CurrentContext.Test.FullName + "\n" +
                    Application.productName + "\n" + Path.Combine(Application.persistentDataPath, filename));
            }
            Application.logMessageReceived -= RecordThemeWarning;
            if (simulatedTouch != null) InputSystem.RemoveDevice(simulatedTouch);
            simulatedTouch = null;
            if (controller != null) UnityEngine.Object.Destroy(controller.gameObject);
            if (!Application.isPlaying) yield return new EnterPlayMode();
            if (GameRoot.Instance != null) UnityEngine.Object.Destroy(GameRoot.Instance.gameObject);
            if (disposableConfig != null) UnityEngine.Object.Destroy(disposableConfig);
            if (disposableRunScenarioConfig != null) UnityEngine.Object.Destroy(disposableRunScenarioConfig);
            yield return null;
            combatFeedbackScenario = 0;
            SceneManager.sceneLoaded -= Isolate;
            if (filename != null && !failed)
                foreach (string path in Directory.GetFiles(Application.persistentDataPath, filename + "*")) File.Delete(path);
        }
        [UnityTest]
        public IEnumerator NativeConstructionReachesUninvestedMiddleDoorway()
        {
            var root = GameRoot.Instance;
            Assert.That(root.Save.validatedCanonicalSpatialState.Floors, Is.Empty, "Use native creation without seeding a room");
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var initialMana = root.Save.structureRuntime.ManaReserve;
            Click(ui.Q<Button>("buildFirstRoom"));
            Click(ui.Q<Button>("confirm"));
            var starter = root.Save.validatedCanonicalSpatialState.Floors.Single();
            var first = starter.Layout.Rooms.Single();
            Assert.That(first.Anchor, Is.EqualTo(new TileCoordinate(0, 2)));
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(initialMana));

            Assert.That(root.SaveService.TryRoomConstructionBaseCost("spatial.room.basic", out var roomCost), Is.True);
            Assert.That(initialMana, Is.LessThan(roomCost));
            // Supply elapsed time only. The production foreground executor earns and publishes all mana.
            for (int interval = 0; interval < 60 && root.Save.structureRuntime.ManaReserve < roomCost; interval++)
            {
                sceneClock.Advance(30);
                PumpSceneToClosed(establishForeground: true);
                yield return null;
            }
            double earnedBalance = root.Save.structureRuntime.ManaReserve;
            Assert.That(earnedBalance, Is.GreaterThanOrEqualTo(roomCost));

            controller.EnterEdit();
            Click(ui.Q<Button>("roomsCategory"));
            Click(ui.Query<Button>(className: "room-card").ToList()
                .Single(button => (string)button.userData == "spatial.room.basic"));
            Assert.That(controller.SelectTerminalConnection("north"), Is.True);
            var anchor = new TileCoordinate(0, 6);
            Assert.That(controller.World.MoveGuidanceAnchors, Does.Contain(anchor));
            controller.TapWorld(anchor);
            Assert.That(controller.ConstructionPreview.IsValid, Is.True);
            var quote = root.SaveService.PreviewDungeonDraft(root.Save, controller.Draft, controller.ConstructionPreview.DetachedCandidate);
            Assert.That(quote.IsAffordable, Is.True, quote.Reason);
            Assert.That(quote.Cost, Is.EqualTo(roomCost));
            Click(ui.Q<Button>("confirmPlacement"));
            yield return null;
            Assert.That(controller.Draft.Durability, Is.EqualTo(DraftDurability.Acknowledged), controller.Draft.Reason);
            Assert.That(ui.Q<Button>("save").enabledSelf, Is.True, ui.Q<Label>("status").text);
            Click(ui.Q<Button>("save"));
            Assert.That(ui.Q("modal").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Click(ui.Q<Button>("confirm"));
            yield return null;

            Assert.That(controller.IsEditing, Is.False);
            var floor = root.Save.validatedCanonicalSpatialState.Floors.Single();
            var second = floor.Layout.Rooms.Single(room => room.Anchor.Equals(anchor));
            Assert.That(second.RoomInstanceId, Is.Not.EqualTo(first.RoomInstanceId));
            Assert.That(floor.FixedStructures.Single(value => value.Kind == FixedSpatialStructureKind.CompletionTerminal).Anchor,
                Is.EqualTo(new TileCoordinate(1, 10)));
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(earnedBalance - quote.Cost));
            var firstNode = floor.Layout.Nodes.Single(node => node.RoomInstanceId == first.RoomInstanceId);
            var secondNode = floor.Layout.Nodes.Single(node => node.RoomInstanceId == second.RoomInstanceId);
            var middle = floor.Layout.Edges.Single(edge => edge.SourceNodeId == firstNode.NodeId && edge.DestinationNodeId == secondNode.NodeId);
            Assert.That(middle.Classification, Is.EqualTo(RouteClassification.Required));
            Assert.That(middle.ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.DirectDoorway));
            Assert.That(middle.OptionalBranchId, Is.Null.Or.Empty);
            byte[] disk = File.ReadAllBytes(root.SaveService.SavePath);
            CollectionAssert.AreEqual(root.SaveService.CanonicalSession.GetCurrentBytes(), disk);
            var context = (DetachedCurrentTargetValidationContext)typeof(SaveService).GetField("_validationContext",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(root.SaveService);
            var saved = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(disk, context);
            Assert.That(saved.CurrentTargetValidated, Is.True, saved.Reason);
            var record = saved.Investment.Single(value => value.StructureId == middle.EdgeId);
            Assert.That(record.ConstructionMana, Is.Zero);
            Assert.That(record.RenovationMana, Is.Zero);
            TestContext.WriteLine($"Native construction earned {earnedBalance - initialMana} mana in {sceneClock.Monotonic()} seconds, spent {quote.Cost}, and created {middle.EdgeId} at zero investment.");
        }

        [UnityTest]
        public IEnumerator NativeConstructionAuthorsMiddleInsertionAndReopens()
        {
            yield return NativeConstructionReachesUninvestedMiddleDoorway();
            var root = GameRoot.Instance;
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var starter = root.Save.validatedCanonicalSpatialState.Floors.Single();
            var first = starter.Layout.Rooms.Single(r => r.Anchor.Equals(new TileCoordinate(0, 2)));
            var second = starter.Layout.Rooms.Single(r => r.Anchor.Equals(new TileCoordinate(0, 6)));
            var middle = starter.Layout.Edges.Single(e => e.SourceNodeId ==
                starter.Layout.Nodes.Single(n => n.RoomInstanceId == first.RoomInstanceId).NodeId);
            const double expectedCost = 110d;
            const string monster = "placement.option.monster.skeleton";
            const string loot = "placement.option.loot_node.basic";
            Assert.That(root.SaveService.TryContentAcquisitionCost(CanonicalSpatialSaveContracts.MonsterCategoryId, monster, out double monsterCost), Is.True);
            Assert.That(root.SaveService.TryContentAcquisitionCost(CanonicalSpatialSaveContracts.LootNodeCategoryId, loot, out double lootCost), Is.True);
            double budget = expectedCost + monsterCost + lootCost;
            for (int interval = 0; interval < 120 && root.Save.structureRuntime.ManaReserve < budget; interval++)
            {
                sceneClock.Advance(30);
                PumpSceneToClosed(establishForeground: true);
                yield return null;
            }
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.GreaterThanOrEqualTo(budget));

            // Production traffic needs a monster and a loot signal. Keep both beyond
            // the authored passage so any combat or loot is attributable to that room.
            controller.EnterEdit();
            PlaceContent(CanonicalSpatialSaveContracts.MonsterCategoryId, monster, new TileCoordinate(2, 7));
            yield return null;
            PlaceContent(CanonicalSpatialSaveContracts.LootNodeCategoryId, loot, new TileCoordinate(1, 7));
            yield return null;
            Assert.That(controller.Draft.Durability, Is.EqualTo(DraftDurability.Acknowledged), controller.Draft.Reason);
            var contentQuote = root.SaveService.PreviewDungeonDraft(root.Save, controller.Draft);
            Assert.That(contentQuote, Is.Not.Null);
            double fundedBudget = contentQuote.Cost + contentQuote.RequiredReserveAfter + expectedCost;
            // Leave the protected recovery reserve funded when paying for the insertion.
            // Traffic starts after the content commit, so fund this while the route is quiet.
            for (int interval = 0; interval < 120 && root.Save.structureRuntime.ManaReserve < fundedBudget; interval++)
            {
                sceneClock.Advance(30);
                PumpSceneToClosed(establishForeground: true);
                yield return null;
            }
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.GreaterThanOrEqualTo(fundedBudget));
            Assert.That(ui.Q<Button>("save").enabledSelf, Is.True, ui.Q<Label>("status").text);
            Click(ui.Q<Button>("save"));
            Click(ui.Q<Button>("confirm"));
            yield return null;
            Assert.That(controller.IsEditing, Is.False, ui.Q<Label>("status").text);

            void PlaceContent(string category, string option, TileCoordinate cell)
            {
                controller.TapWorld(new TileCoordinate(3, 9));
                Assert.That(controller.OpenContentCategory(category), Is.True);
                Assert.That(controller.SelectNewContent(option), Is.True);
                controller.TapWorld(cell);
                Assert.That(controller.ContentPreview.IsValid, Is.True, controller.ContentPreview.Reason);
                Assert.That(controller.ConfirmContentPlacement(), Is.True);
            }

            for (int interval = 0; interval < 180 && root.Save.livingDungeon.Parties.Count == 0; interval++)
            {
                sceneClock.Advance(1);
                PumpSceneToClosed(establishForeground: true);
                yield return null;
            }
            Assert.That(root.Save.livingDungeon.Parties, Has.Count.EqualTo(1), "Production traffic must admit a party before construction");
            var oldParty = root.Save.livingDungeon.Parties.Single();
            byte[] oldCheckpoint = (byte[])oldParty.Checkpoint.Clone();
            double balance = root.Save.structureRuntime.ManaReserve;
            Assert.That(balance, Is.GreaterThanOrEqualTo(expectedCost));
            byte[] before = root.SaveService.CanonicalSession.GetCurrentBytes();

            Click(ui.Q<Button>("edit"));
            Click(ui.Q<Button>("roomsCategory"));
            Click(ui.Query<Button>(className: "room-card").ToList().Single(button =>
                (string)button.userData == "spatial.room.basic"));
            var camera = GameObject.Find("ProductionDungeonCamera").GetComponent<Camera>();
            var pointer = InputSystem.AddDevice<Mouse>();
            try
            {
                yield return MapTap(new Vector2(.5f, 2.5f));
                Assert.That(controller.ConstructionPreview.ReasonCodes, Does.Contain(StructuralEditService.RoomOverlapReason));
                Assert.That(ui.Q<Button>("confirmPlacement").enabledSelf, Is.False);
                yield return MapTap(new Vector2(4.5f, 3.5f));
                Assert.That(controller.ConstructionPreview.IsValid, Is.True, string.Join(",", controller.ConstructionPreview.ReasonCodes));
                Click(ui.Q<Button>("confirmPlacement"));
                yield return null;
                Assert.That(controller.Draft.CommandCount, Is.EqualTo(1));
                Assert.That(controller.Draft.DisconnectedRoomIds, Has.Length.EqualTo(1));
                Assert.That(ui.Q<Button>("save").enabledSelf, Is.False);
                CollectionAssert.AreEqual(before, root.SaveService.CanonicalSession.GetCurrentBytes());

                var alternate = starter.Layout.Edges.First(e => e.EdgeId != middle.EdgeId &&
                    root.SaveService.EligibleRouteInsertions(root.Save, controller.Draft, starter.FloorInstanceId).Contains(e.EdgeId));
                yield return MapTap(NodeCenter(alternate.SourceNodeId));
                yield return MapTap(new Vector2(6, 5));
                yield return MapTap(NodeCenter(alternate.DestinationNodeId));
                Assert.That(ui.Q<Button>("chooseRouteRoomsAgain"), Is.Not.Null,
                    "Selecting an alternative eligible pair on the map opens its replacement preview");
                Assert.That(controller.Draft.CommandCount, Is.EqualTo(1));
                Click(ui.Q<Button>("cancelRoute"));
                Assert.That(controller.Draft.DisconnectedRoomIds, Has.Length.EqualTo(1));
                CollectionAssert.AreEqual(before, root.SaveService.CanonicalSession.GetCurrentBytes());
                yield return MapTap(new Vector2(6, 5)); // Reopen from the disconnected room.
                yield return MapTap(new Vector2(2, 4));
                yield return MapTap(new Vector2(6, 5));
                yield return MapTap(new Vector2(2, 8));
                string placedId = controller.Draft.DisconnectedRoomIds.Single();
                var documented = new StructuralRouteInsertionRequest {
                    FloorInstanceId = starter.FloorInstanceId, ReplacedEdgeId = middle.EdgeId, RoomInstanceId = placedId,
                    Incoming = new AuthoredRouteGeometry { SourcePointId = "east.2", DestinationPointId = "west" },
                    Outgoing = new AuthoredRouteGeometry { SourcePointId = "north", DestinationPointId = "east",
                        Passage = new[] { new TileCoordinate(5, 7), new TileCoordinate(4, 7) } }
                };
                var documentedPreview = root.SaveService.PreviewRouteInsertion(root.Save, controller.Draft, documented, out var documentedQuote);
                Assert.That(documentedPreview.IsValid, Is.True, $"Documented native insertion rejected: {documentedPreview.Reason}");
                Assert.That(documentedQuote.Cost, Is.EqualTo(expectedCost));
                Assert.That(documentedQuote.IsAffordable, Is.True, documentedQuote.Reason);
                Assert.That(root.SaveService.SuggestRouteInsertion(root.Save, controller.Draft, starter.FloorInstanceId,
                    middle.EdgeId, placedId), Is.Not.Null, "Production must suggest a valid insertion before manual adjustment");
                var placementFloor = controller.Draft.ReadModel.Floors.Single();
                var placedNode = placementFloor.Layout.Nodes.Single(node => node.RoomInstanceId == placedId);
                var placementCatalog = root.ProductionSpatialContent.Catalog;
                Vector2 DoorOnNode(string node, string id) => Door(AuthoredRouteGeometry.Openings(placementFloor,
                    node, placementCatalog).Single(value => value.Id == id));

                // The expanded perimeter can suggest a cheaper two-doorway insertion.
                // Explicitly choose the physical passage used by the travel and capacity assertions.
                yield return SelectFixtureDoorways(false, placedNode.NodeId, middle.DestinationNodeId,
                    documented.Outgoing, DoorOnNode, point => MapTap(point));
                yield return SelectFixtureDoorways(true, middle.SourceNodeId, placedNode.NodeId,
                    documented.Incoming, DoorOnNode, point => MapTap(point));
                Assert.That(controller.Draft.CommandCount, Is.EqualTo(1), "Suggestions and map adjustments remain transient");
                Assert.That(ui.Q<Button>("acceptRoute").enabledSelf, Is.True, ui.Q("routeAuthoring").Query<Label>().ToList()
                    .Select(label => label.text).Aggregate(string.Empty, (left, right) => left + "\n" + right));

                    foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(720, 1280) })
                foreach (var textSize in new[] { DungeonTextSize.Default, DungeonTextSize.Small })
                {
                    DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x, size.y);
                    controller.SetTextSize(textSize);
                    yield return WaitForViewportGeometry();
                    if (!controller.DetailsCollapsed) yield return ClickVisible(ui.Q<Button>("collapseDetails"));
                    yield return WaitForViewportGeometry();
                    Assert.That(controller.DetailsCollapsed, Is.True);
                    var map = controller.Viewport.ScreenRect;
                    var accept = ui.Q<Button>("acceptRoute");
                    string capture = $"route-dock-{size.x}x{size.y}-{textSize}";
                    DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture(capture + "-collapsed.png");
                    yield return null;
                    AssertRouteLayout(accept, ui.Q<Button>("chooseRouteRoomsAgain"), ui.Q<Button>("cancelRoute"), ui.Q<Button>("moveRouteRoom"));
                    Assert.That(ui.Q<Label>("routeState").text, Is.EqualTo(root.Content.GetString("ui.dungeon.route.proposal_ready", null)));
                    Assert.That(ui.Q<Label>("status").text, Is.Empty);
                    Assert.That(ui.Q<Label>("routeMetrics").text, Does.Contain("110"));
                    yield return ClickVisible(ui.Q<Button>("collapseDetails"));
                    yield return WaitForViewportGeometry();
                    Assert.That(controller.DetailsCollapsed, Is.False);
                    Assert.That(controller.Viewport.ScreenRect, Is.EqualTo(map));
                    Assert.That(ui.Q<Button>("acceptRoute"), Is.SameAs(accept));
                    AssertRouteLayout(accept, ui.Q<Button>("chooseRouteRoomsAgain"), ui.Q<Button>("cancelRoute"), ui.Q<Button>("moveRouteRoom"));
                    DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture(capture + "-expanded.png");
                    yield return null;
                    yield return MapTap(new Vector2(5f, 7.5f));
                    yield return MapTap(new Vector2(6.5f, 6.5f));
                    Assert.That(accept.enabledSelf, Is.False);
                    Assert.That(ui.Q<Label>("routeState").text, Is.EqualTo(
                        root.Content.GetString("ui.dungeon.route.no_connection", null)));
                    Assert.That(controller.Viewport.ScreenRect, Is.EqualTo(map));
                    AssertRouteLayout(accept, ui.Q<Button>("cancelRouteAdjustment"));
                    DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture(capture + "-invalid.png");
                    yield return null;
                    yield return ClickVisible(ui.Q<Button>("cancelRouteAdjustment"));
                    Assert.That(accept.enabledSelf, Is.True);
                    Assert.That(controller.Viewport.ScreenRect, Is.EqualTo(map));
                }
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(1280, 720);
                controller.SetTextSize(DungeonTextSize.Default);
                yield return WaitForViewportGeometry();

                var firstNode = starter.Layout.Nodes.Single(n => n.RoomInstanceId == first.RoomInstanceId);
                var openings = AuthoredRouteGeometry.Openings(starter, firstNode.NodeId, root.ProductionSpatialContent.Catalog);
                yield return MapTap(Door(openings.Single(p => p.Id == "east.2")));
                Assert.That(ui.Q<Label>("routeInstruction").text, Does.Contain("highlighted doorway"));
                Assert.That(controller.Draft.CommandCount, Is.EqualTo(1));
                yield return MapTap(Door(openings.Single(p => p.Id == "east")));
                Assert.That(ui.Q<Button>("acceptRoute").enabledSelf, Is.False);
                yield return ClickVisible(ui.Q<Button>("cancelRouteAdjustment"));
                Assert.That(ui.Q<Button>("acceptRoute").enabledSelf, Is.True);

                yield return MapTap(new Vector2(5f, 7.5f), new Vector2(1.5f, 8.5f));
                Assert.That(ui.Q<Button>("acceptRoute").enabledSelf, Is.False, "The detour exceeds the floor capacity");
                Assert.That(ui.Q<Label>("routeState").text, Is.EqualTo(root.Content.GetString(StructuralEditService.CapacityReason, null)));
                yield return ClickVisible(ui.Q<Button>("cancelRouteAdjustment"));
                Assert.That(ui.Q<Button>("acceptRoute").enabledSelf, Is.True);
                Assert.That(controller.Draft.CommandCount, Is.EqualTo(1));
                if (!controller.DetailsCollapsed) yield return ClickVisible(ui.Q<Button>("collapseDetails"));
                yield return ClickVisible(ui.Q<Button>("acceptRoute"));
                yield return null;
                Assert.That(controller.Draft.CommandCount, Is.EqualTo(2));
            }
            finally { InputSystem.RemoveDevice(pointer); }

            IEnumerator ClickVisible(Button button)
            {
                yield return WaitForViewportGeometry();
                if (ui.Q("routeAuthoring").Contains(button)) AssertRouteLayout(button);
                else
                {
                    AssertReachable(button);
                    var picked = ui.panel.Pick(button.worldBound.center);
                    Assert.That(picked == button || button.Contains(picked), Is.True, button.name);
                }
                float scale = controller.GetComponent<UIDocument>().panelSettings.scale;
                var center = button.worldBound.center * scale;
                var point = new Vector2(center.x, Screen.height - center.y);
                InputSystem.QueueStateEvent(pointer, new MouseState { position = point }
                    .WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(pointer, new MouseState { position = point });
                yield return null;
                yield return null;
            }

            void AssertRouteLayout(params Button[] buttons)
            {
                var dock = ui.Q("bottomChrome").worldBound;
                float scale = controller.GetComponent<UIDocument>().panelSettings.scale;
                Assert.That(controller.Viewport.ScreenRect.height, Is.GreaterThan(Screen.height * .24f));
                for (int i = 0; i < buttons.Length; i++)
                {
                    var button = buttons[i];
                    Assert.That(button.resolvedStyle.visibility, Is.EqualTo(Visibility.Visible), button.name);
                    var bounds = button.worldBound;
                    Assert.That(bounds.width * scale, Is.GreaterThanOrEqualTo(48), button.name);
                    Assert.That(bounds.height * scale, Is.GreaterThanOrEqualTo(48), button.name);
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(dock.xMin - 1), button.name);
                    Assert.That(bounds.xMax, Is.LessThanOrEqualTo(dock.xMax + 1), button.name);
                    Assert.That(bounds.yMax, Is.LessThanOrEqualTo(Math.Min(dock.yMax, Screen.height / scale) + 1), button.name);
                    var text = button.MeasureTextSize(button.text, 0, VisualElement.MeasureMode.Undefined,
                        0, VisualElement.MeasureMode.Undefined);
                    Assert.That(button.contentRect.width, Is.GreaterThanOrEqualTo(text.x - 1), button.name);
                    var picked = ui.panel.Pick(bounds.center);
                    Assert.That(picked == button || button.Contains(picked), Is.True, button.name);
                    for (int j = 0; j < i; j++) Assert.That(bounds.Overlaps(buttons[j].worldBound), Is.False, button.name);
                }
            }

            IEnumerator MapTap(Vector2 world, Vector2? destination = null)
            {
                yield return WaitForViewportGeometry();
                Vector2 begin = camera.WorldToScreenPoint(world);
                Vector2 end = camera.WorldToScreenPoint(destination ?? world);
                Assert.That(controller.Viewport.ScreenRect.Contains(begin), Is.True, world.ToString());
                Assert.That(controller.IsChrome(begin, pointer.deviceId), Is.False, world.ToString());
                InputSystem.QueueStateEvent(pointer, new MouseState { position = begin }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                yield return null;
                yield return null;
                if (destination.HasValue)
                {
                    Assert.That(controller.Viewport.ScreenRect.Contains(end), Is.True, destination.ToString());
                    Assert.That(controller.IsChrome(end, pointer.deviceId), Is.False, destination.ToString());
                    InputSystem.QueueStateEvent(pointer, new MouseState { position = end }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                    yield return null;
                    yield return null;
                }
                InputSystem.QueueStateEvent(pointer, new MouseState { position = end });
                yield return null;
                yield return null;
            }

            Vector2 NodeCenter(string nodeId)
            {
                var node = starter.Layout.Nodes.Single(n => n.NodeId == nodeId);
                string id = node.RoomInstanceId;
                if (node.Kind != FloorRouteNodeKind.Room)
                {
                    var kind = node.Kind == FloorRouteNodeKind.Entrance
                        ? FixedSpatialStructureKind.Entrance : FixedSpatialStructureKind.CompletionTerminal;
                    id = starter.FixedStructures.Single(f => f.Kind == kind).FixedStructureInstanceId;
                }
                Assert.That(controller.World.TryRouteLocationBounds(id, out var bounds), Is.True, $"{node.Kind} {nodeId} {id}");
                return bounds.center;
            }

            Vector2 Door(RouteOpening opening)
            {
                float angle = -(int)opening.Facing * Mathf.PI / 2f;
                return new Vector2(opening.World.X + .5f, opening.World.Y + .5f) +
                    new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * .26f;
            }

            string added = controller.Draft.RouteAuthoringRoomIds.Single();
            var recoveredStore = root.SaveService.CreateDungeonDraftStore();
            var recovered = TransactionalDungeonDraft.Recover(recoveredStore.Read(),
                root.Save.validatedCanonicalSpatialState, root.SaveService.DungeonDraftContext, recoveredStore, out var reason);
            Assert.That(recovered, Is.Not.Null, reason);
            var proposal = recovered.AcceptedRouteInsertion(added);
            Assert.That(proposal.Incoming.SourcePointId, Is.EqualTo("east.2"));
            CollectionAssert.AreEqual(new[] { new TileCoordinate(5, 7), new TileCoordinate(4, 7) }, proposal.Outgoing.Passage);
            CollectionAssert.AreEqual(before, root.SaveService.CanonicalSession.GetCurrentBytes());
            Assert.That(root.SaveService.PreviewDungeonDraft(root.Save, controller.Draft).Cost, Is.EqualTo(expectedCost));

            pointer = InputSystem.AddDevice<Mouse>();
            try
            {
                yield return ClickVisible(ui.Q<Button>("save"));
                yield return ClickVisible(ui.Q<Button>("confirm"));
            }
            finally { InputSystem.RemoveDevice(pointer); }
            Assert.That(controller.IsEditing, Is.False, ui.Q<Label>("status").text);
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(balance - expectedCost));
            var committed = root.Save.validatedCanonicalSpatialState.Floors.Single();
            Assert.That(RequiredFloorTraversal.TryResolve(committed.Layout, out var route), Is.True);
            CollectionAssert.AreEqual(new[] { first.RoomInstanceId, added, second.RoomInstanceId },
                route.Where(n => n.Kind == FloorRouteNodeKind.Room).Select(n => n.RoomInstanceId));
            Assert.That(committed.Layout.Edges.Any(e => e.EdgeId == middle.EdgeId), Is.False);
            var catalog = root.ProductionSpatialContent.Catalog;
            var capacity = FloorLayoutValidator.Validate(committed.Layout, catalog.Floors.Single(f => f.FloorIndex == 0),
                catalog.Rooms, catalog.Corridors, new SpatialValidationWorkloadLimits(root.SaveSpatialMigrationLimits.Canonical.Spatial.MaximumMaterializedTiles),
                committed.FixedStructures, catalog.FixedStructures);
            Assert.That(capacity.IsValid, Is.True);
            Assert.That(capacity.Capacity.UsedFloorSpaceCapacity, Is.EqualTo(60));
            CollectionAssert.AreEqual(oldCheckpoint, root.Save.livingDungeon.Parties.Single().Checkpoint);

            var replaced = committed.Layout.Edges.Single(edge => edge.AuthoredGeometry?.Passage.Length > 0);
            var redesign = new AuthoredRouteGeometry
            {
                SourcePointId = "perimeter.north.0", DestinationPointId = "east.2",
                Passage = new[] { new TileCoordinate(4, 7), new TileCoordinate(4, 8) }
            };
            controller.EnterEdit();
            Assert.That(root.SaveService.AcceptRenovation(root.Save, controller.Draft, new StructuralRouteRenovation
            {
                Kind = StructuralRouteRenovationKind.Connect, FloorInstanceId = committed.FloorInstanceId,
                TargetId = replaced.EdgeId, SourceNodeId = replaced.SourceNodeId, DestinationNodeId = replaced.DestinationNodeId,
                Connections = new[] { redesign }
            }, out var redesignReason), Is.True, redesignReason);
            for (int frame = 0; frame < 10 && controller.Draft.Durability == DraftDurability.Pending; frame++) yield return null;
            Assert.That(controller.Commit(), Is.True);
            committed = root.Save.validatedCanonicalSpatialState.Floors.Single();
            var redesigned = committed.Layout.Edges.Single(edge => edge.EdgeId == replaced.EdgeId);
            Assert.That(JsonUtility.ToJson(redesigned.AuthoredGeometry), Is.EqualTo(JsonUtility.ToJson(redesign)));
            CollectionAssert.AreEqual(oldCheckpoint, root.Save.livingDungeon.Parties.Single().Checkpoint);

            string fingerprint = root.SaveService.DungeonDraftContext.Fingerprint(root.Save.validatedCanonicalSpatialState);
            UnityEngine.Object.Destroy(controller.gameObject);
            UnityEngine.Object.Destroy(root.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
            for (int i = 0; i < 120 && GameRoot.Instance?.Save == null; i++) yield return null;
            root = GameRoot.Instance;
            Assert.That(Path.GetFileName(root.SaveService.SavePath), Is.EqualTo(filename));
            Assert.That(root.SaveService.DungeonDraftContext.Fingerprint(root.Save.validatedCanonicalSpatialState), Is.EqualTo(fingerprint));
            CollectionAssert.AreEqual(oldCheckpoint, root.Save.livingDungeon.Parties.Single().Checkpoint);
            controller = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();

            var profile = root.SaveService.ForegroundConfiguration.CreateProfile(
                root.SaveSpatialMigrationLimits.Canonical.ForSchema(CanonicalSaveSchemaVersions.RoomContentPositionIntroduction).Serialized);
            string nextRunId = null;
            string outgoingId = committed.Layout.Edges.Single(edge => edge.AuthoredGeometry?.Passage.Length > 0).EdgeId;
            for (int interval = 0; interval < 360; interval++)
            {
                sceneClock.Advance(1);
                PumpSceneToClosed(establishForeground: true);
                var nextParty = root.Save.livingDungeon.Parties.SingleOrDefault(party => party.Sequence > oldParty.Sequence);
                if (nextParty != null && nextRunId == null)
                {
                    var execution = LivingDungeonContracts.Restore(nextParty, profile);
                    try
                    {
                        var captured = execution.Snapshot.Floors.Single();
                        CollectionAssert.AreEqual(new[] { first.RoomInstanceId, added, second.RoomInstanceId },
                            captured.MaterializePlan().RequiredRooms.Select(entry => entry.Room.RoomInstanceId));
                        var link = captured.InspectSpatialInputs().Layout.Edges.Single(edge =>
                            edge.EdgeId == outgoingId).AuthoredGeometry;
                        Assert.That(link.SourcePointId, Is.EqualTo(redesign.SourcePointId));
                        Assert.That(link.DestinationPointId, Is.EqualTo(redesign.DestinationPointId));
                        CollectionAssert.AreEqual(redesign.Passage, link.Passage);
                        nextRunId = execution.Inspect().RunId;
                    }
                    finally { execution.Abandon(); }
                }
                var outcome = root.Save.runHistory.LatestOutcome;
                if (nextRunId != null && outcome?.RunId == nextRunId)
                {
                    var steps = outcome.PresentationSteps;
                    Assert.That(steps, Is.Not.Null.And.Not.Empty);
                    CollectionAssert.AreEqual(new[] { first.RoomInstanceId, added, second.RoomInstanceId },
                        steps.Where(step => step.Kind == RunStepKind.RoomEntry).Select(step => step.RoomId));
                    var traversed = steps.Where(step => step.Kind == RunStepKind.Movement &&
                        step.PositionSpace == RunPositionSpace.Floor && step.AssignmentId == outgoingId).ToArray();
                    CollectionAssert.AreEqual(redesign.Passage.Append(new TileCoordinate(3, 8)),
                        traversed.Select(step => step.Position));
                    Assert.That(traversed.All(step => step.Distance == 1 && step.Encounter == null &&
                        step.CombatEvents?.Count is null or 0 && step.CarriedLootValueAfter == 0), Is.True);
                    Assert.That(steps.Any(step => step.Kind == RunStepKind.TrapTrigger), Is.False);
                    var interactions = steps.Where(step => step.Kind == RunStepKind.MonsterEngagement || step.Kind == RunStepKind.LootInteraction).ToArray();
                    Assert.That(interactions.Any(step => step.Kind == RunStepKind.MonsterEngagement), Is.True);
                    Assert.That(interactions.All(step => step.RoomId == second.RoomInstanceId &&
                        step.PositionSpace == RunPositionSpace.RoomLocal), Is.True);
                    TestContext.WriteLine($"Committed {expectedCost} mana and {capacity.Capacity.UsedFloorSpaceCapacity} tiles. " +
                        $"Captured old checkpoint survived reopen. Subsequent run {nextRunId} visited {first.RoomInstanceId}, {added}, " +
                        $"{second.RoomInstanceId} and traversed {outgoingId} through " +
                        string.Join(" → ", traversed.Select(step => $"({step.Position.X},{step.Position.Y})")) + ".");
                    yield break;
                }
                yield return null;
            }
            Assert.Fail("The production scheduler did not settle the subsequent party within the test horizon");
        }

        [UnityTest]
        public IEnumerator TransactionalRoomConstructionProductionPreviewConfirmSaveRecovery()
        {
            var root = GameRoot.Instance;
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var before = File.ReadAllBytes(root.SaveService.SavePath);
            double mana = root.Save.structureRuntime.ManaReserve;
            controller.EnterEdit(); Click(ui.Q<Button>("roomsCategory"));
            Assert.That(ui.Q("constructionCategories").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(ui.Q("roomChoices").Query<Button>().ToList().Count, Is.GreaterThan(0));
            Assert.That(controller.SelectConstructionRoom("spatial.room.basic"), Is.True);
            Assert.That(controller.World.MoveGuidanceAnchors, Is.Not.Empty);
            var anchor = controller.World.MoveGuidanceAnchors.First();
            controller.TapWorld(anchor);
            Assert.That(controller.ConstructionPreview.IsValid, Is.True);
            Assert.That(controller.World.ConstructionPreviewTileCount, Is.GreaterThan(0));
            Assert.That(controller.Draft.CommandCount, Is.Zero);
            var quote = root.SaveService.PreviewDungeonDraft(root.Save, controller.Draft, controller.ConstructionPreview.DetachedCandidate);
            Assert.That(ui.Q<Label>("constructionSummary").text, Does.Contain(StructuralEconomyPresenter.FormatTransactionAmount(quote.Cost))
                .And.Contain(StructuralEconomyPresenter.FormatTransactionAmount(quote.ResultingMana)));
            Assert.That(ui.Q<Label>("draftSummary").text, Does.Contain("Current draft:"));
            root.Save.structureRuntime.ManaReserve = 0; root.Save.totalTicks++; yield return null; yield return null;
            Assert.That(ui.Q<Label>("constructionAffordability").text, Is.EqualTo(root.Content.GetString("ui.dungeon.construction.unaffordable_draft", null)));
            Assert.That(ui.Q<Button>("confirmPlacement").enabledSelf, Is.True, "Affordability gates Save, not draft confirmation");
            root.Save.structureRuntime.ManaReserve = mana; root.Save.totalTicks++; yield return null;
            Assert.That(ui.Q<Label>("constructionInfo").text, Does.Contain(root.Content.GetString("ui.structural.connection.direct", null))
                .Or.Contain(root.Content.GetString("ui.structural.connection.corridor", null)));
            AssertCanonicalUnchanged(before, mana);
            Click(ui.Q<Button>("confirmPlacement")); yield return null;
            Assert.That(controller.Draft.CommandCount, Is.EqualTo(1));
            Assert.That(controller.Draft.Durability, Is.EqualTo(DraftDurability.Acknowledged));
            Assert.That(controller.Draft.ReadModel.Floors[0].Layout.Rooms.Length, Is.EqualTo(2));
            AssertCanonicalUnchanged(before, mana);
            var store = root.SaveService.CreateDungeonDraftStore();
            var recovered = TransactionalDungeonDraft.Recover(store.Read(), root.Save.validatedCanonicalSpatialState,
                root.SaveService.DungeonDraftContext, store, out var reason);
            Assert.That(recovered, Is.Not.Null, reason);
            Assert.That(recovered.ReadModel.Floors[0].Layout.Rooms.Length, Is.EqualTo(2));
            double charge = root.SaveService.PreviewDungeonDraft(root.Save, controller.Draft).Cost;
            root.Save.structureRuntime.ManaReserve = mana = charge +
                MonsterContinuity.RequiredReserve(root.Save.validatedCanonicalSpatialState, MonsterRecoveryRules.Capture(root.RunSimulationConfig));
            Click(ui.Q<Button>("save")); Assert.That(ui.Q("modal").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Click(ui.Q<Button>("confirm")); yield return null;
            Assert.That(controller.IsEditing, Is.False);
            Assert.That(root.Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms.Length, Is.EqualTo(2));
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(mana - charge));
            Assert.That(store.Read(), Is.Null);
            Assert.That(File.ReadAllText(root.SaveService.SavePath), Does.Contain("\"schemaVersion\":" + CanonicalSaveSchemaVersions.CurrentWritableTarget));
            controller.EnterEdit(); controller.TapWorld(new TileCoordinate(1, 3)); yield return null; yield return null;
            Assert.That(controller.SelectedRoomInstanceId, Is.Not.Null);
            Assert.That(ui.Q("contextDetails").style.display.value, Is.EqualTo(DisplayStyle.Flex), "Room selection restores the A5 contextual actions after collapsed construction");
            Assert.That(ui.Q<Button>("move").enabledSelf, Is.True);
            Assert.That(controller.Discard(), Is.True);
        }

        [UnityTest]
        public IEnumerator TransactionalRoomConstructionInvalidCorrectionAndCollapseKeepActionsVisible()
        {
            var root = GameRoot.Instance; var before = File.ReadAllBytes(root.SaveService.SavePath);
            double mana = root.Save.structureRuntime.ManaReserve;
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            controller.EnterEdit(); controller.SelectConstructionRoom("spatial.room.basic");
            controller.TapWorld(new TileCoordinate(-1, -1));
            Assert.That(controller.ConstructionPreview.IsValid, Is.False);
            var strings = (System.Collections.Generic.Dictionary<string, string>)typeof(ContentService)
                .GetField("_stringMap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(root.Content);
            strings[controller.ConstructionPreview.ReasonCodes.First()] = "配置できません。 This expanded localized reason explains that the proposed room footprint is outside the legal floor bounds. Choose another visible anchor or cancel this placement.";
            controller.TapWorld(new TileCoordinate(-1, -1));
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(720, 1280);
            controller.SetTextSize(DungeonTextSize.Large); for (int i = 0; i < 16; i++) yield return null;
            Assert.That(controller.Draft.CommandCount, Is.Zero);
            Click(ui.Q<Button>("collapseDetails")); for (int i = 0; i < 8; i++) yield return null;
            Assert.That(ui.Q("contextDetails").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Click(ui.Q<Button>("collapseDetails")); for (int i = 0; i < 8; i++) yield return null;
            AssertReachable(ui.Q<Button>("confirmPlacement")); AssertReachable(ui.Q<Button>("cancelPlacement"));
            Assert.That(ui.Q<Button>("confirmPlacement").text, Is.EqualTo(root.Content.GetString("ui.dungeon.construction.keep_invalid", null)));
            AssertReachable(ui.Q<Button>("save")); AssertReachable(ui.Q<Button>("discard"));
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("construction-ui-invalid-long-720x1280.png");
            yield return null; yield return null;
            Click(ui.Q<Button>("confirmPlacement")); yield return null;
            Assert.That(controller.Draft.InvalidConstructions.Length, Is.EqualTo(1));
            Assert.That(ui.Q<Button>("save").enabledSelf, Is.False);
            int commandCount = controller.Draft.CommandCount;
            controller.SelectConstructionRoom("spatial.room.basic"); controller.TapWorld(controller.World.MoveGuidanceAnchors.First());
            Assert.That(ui.Q<Label>("constructionInfo").text, Does.Contain(root.Content.GetString("ui.dungeon.construction.cost_blocked", null)));
            Click(ui.Q<Button>("cancelPlacement")); Assert.That(controller.Draft.CommandCount, Is.EqualTo(commandCount));
            if (ui.Q("contextDetails").style.display.value != DisplayStyle.None) Click(ui.Q<Button>("collapseDetails"));
            yield return null;
            Assert.That(ui.Q("contextDetails").style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(ui.Q("constructionCategories").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(ui.Q("constructionBlockers").Query<Button>().ToList().Count, Is.EqualTo(1));
            Assert.That(ui.Q<Button>("discard").enabledSelf, Is.True);
            Click(ui.Q("constructionBlockers").Query<Button>().ToList().Single());
            controller.TapWorld(controller.World.MoveGuidanceAnchors.First());
            Click(ui.Q<Button>("confirmPlacement")); yield return null;
            Assert.That(controller.Draft.InvalidConstructions, Is.Empty);
            Assert.That(controller.Draft.CanSave, Is.True);
            AssertCanonicalUnchanged(before, mana);
            Assert.That(controller.Discard(), Is.True); AssertCanonicalUnchanged(before, mana);
        }

        [UnityTest]
        public IEnumerator TransactionalRoomConstructionOutOfBoundsIntentBoundsRecoverCameraAndSave()
        {
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(1920, 1080);
            yield return WaitForViewportGeometry();
            var root = GameRoot.Instance;
            var legalBounds = controller.World.LegalBounds;
            controller.EnterEdit();
            int gridTileCount = controller.World.GridTileCount;
            Assert.That(controller.SelectConstructionRoom("spatial.room.basic"), Is.True);
            Assert.That(controller.World.MoveGuidanceAnchors, Is.Not.Empty);
            var validAnchor = controller.World.MoveGuidanceAnchors.First();
            controller.TapWorld(validAnchor);
            Assert.That(controller.ConstructionPreview.IsValid, Is.True);
            var setupQuote = root.SaveService.PreviewDungeonDraft(root.Save, controller.Draft,
                controller.ConstructionPreview.DetachedCandidate);
            Assert.That(setupQuote.Cost, Is.GreaterThan(0));
            // Seed the disposable runtime before the draft starts so all draft actions preserve this exact balance.
            root.Save.structureRuntime.ManaReserve = setupQuote.Cost + setupQuote.RequiredReserveAfter;
            var canonicalBefore = File.ReadAllBytes(root.SaveService.SavePath);
            double manaBefore = root.Save.structureRuntime.ManaReserve;
            var attemptedAnchor = default(TileCoordinate);
            bool foundOutOfBoundsEdgePlacement = false;
            int minX = Mathf.RoundToInt(legalBounds.xMin), minY = Mathf.RoundToInt(legalBounds.yMin);
            int maxX = Mathf.RoundToInt(legalBounds.xMax), maxY = Mathf.RoundToInt(legalBounds.yMax);
            for (int y = minY; y < maxY && !foundOutOfBoundsEdgePlacement; y++)
                for (int x = minX; x < maxX; x++)
                {
                    if (x != minX && x != maxX - 1 && y != minY && y != maxY - 1) continue;
                    var candidate = new TileCoordinate(x, y);
                    controller.TapWorld(candidate);
                    if (controller.ConstructionPreview?.ReasonCodes.Contains(StructuralEditService.OutOfBoundsReason) != true) continue;
                    attemptedAnchor = candidate; foundOutOfBoundsEdgePlacement = true; break;
                }
            Assert.That(foundOutOfBoundsEdgePlacement, Is.True, "The existing preview authority identifies an out-of-bounds room placement from a legal floor-edge anchor");
            Assert.That(controller.ConstructionPreview.IsValid, Is.False);
            Assert.That(controller.ConstructionPreview.ReasonCodes, Does.Contain(StructuralEditService.OutOfBoundsReason));
            Assert.That(legalBounds.Contains(new Vector2(attemptedAnchor.X + 0.5f, attemptedAnchor.Y + 0.5f)), Is.True);
            var attemptedCells = controller.ConstructionPreview.OccupiedTiles.ToArray();
            Assert.That(attemptedCells.Any(cell => cell.X < legalBounds.xMin || cell.Y < legalBounds.yMin ||
                cell.X >= legalBounds.xMax || cell.Y >= legalBounds.yMax), Is.True);
            Click(controller.GetComponent<UIDocument>().rootVisualElement.Q<Button>("confirmPlacement"));
            yield return null;
            Assert.That(controller.Draft.InvalidConstructions.Length, Is.EqualTo(1));
            Assert.That(controller.Draft.Durability, Is.EqualTo(DraftDurability.Acknowledged));
            Assert.That(controller.Draft.CanSave, Is.False);
            Assert.That(controller.World.LegalBounds, Is.EqualTo(legalBounds));
            Assert.That(controller.World.GridTileCount, Is.EqualTo(gridTileCount));
            AssertCanonicalUnchanged(canonicalBefore, manaBefore);

            yield return RestartShell();
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(ui.Q("modal").style.display.value, Is.EqualTo(DisplayStyle.Flex), "Recovered draft offers Resume Draft");
            Click(ui.Q<Button>("confirm"));
            yield return null;

            Assert.That(controller.Draft.InvalidConstructions.Length, Is.EqualTo(1));
            Assert.That(controller.Draft.Durability, Is.EqualTo(DraftDurability.Acknowledged));
            var invalidMap = controller.World.transform.Find("InvalidConstructionIntents").GetComponent<UnityEngine.Tilemaps.Tilemap>();
            foreach (var cell in attemptedCells)
                Assert.That(invalidMap.GetTile(new Vector3Int(cell.X, cell.Y, 0)), Is.Not.Null);
            Assert.That(attemptedCells.Any(cell => !legalBounds.Contains(new Vector2(cell.X + 0.5f, cell.Y + 0.5f))), Is.True);
            Assert.That(controller.World.Bounds.Contains(attemptedCells
                .Select(cell => new Vector2(cell.X + 0.5f, cell.Y + 0.5f)).First(point => !legalBounds.Contains(point))), Is.True);
            Assert.That(controller.World.LegalBounds, Is.EqualTo(legalBounds));
            Assert.That(controller.World.GridTileCount, Is.EqualTo(gridTileCount));
            Assert.That(controller.World.transform.Find("EditorGrid").GetComponent<UnityEngine.Tilemaps.Tilemap>()
                .GetTile(new Vector3Int((int)legalBounds.xMin, (int)legalBounds.yMin, 0)), Is.Not.Null);
            AssertCanonicalUnchanged(canonicalBefore, manaBefore);

            simulatedTouch = InputSystem.AddDevice<Touchscreen>();
            yield return WaitForViewportGeometry();
            var camera = controller.GetComponentInChildren<Camera>();
            var accessibleCell = attemptedCells.First(cell => !legalBounds.Contains(new Vector2(cell.X + 0.5f, cell.Y + 0.5f)));
            Vector2 screenPoint = camera.WorldToScreenPoint(new Vector3(accessibleCell.X + 0.5f, accessibleCell.Y + 0.5f, 0));
            Assert.That(camera.pixelRect.Contains(screenPoint), Is.True, "Recovered invalid footprint is inside the configured camera viewport");
            Assert.That(controller.IsChrome(screenPoint, 1), Is.False, "Invalid footprint can be reached from the production viewport");
            Touch(1, screenPoint, InputTouchPhase.Began); yield return null; yield return null;
            Touch(1, screenPoint, InputTouchPhase.Ended); yield return null; yield return null;
            Assert.That(controller.IsConstructing, Is.True, "Viewport input selects the recovered invalid intent for correction");
            Assert.That(controller.ConstructionPreview.IsValid, Is.False);

            Assert.That(controller.World.MoveGuidanceAnchors, Does.Contain(validAnchor));
            controller.TapWorld(validAnchor);
            Assert.That(controller.ConstructionPreview.IsValid, Is.True);
            Click(ui.Q<Button>("confirmPlacement")); yield return null;
            Assert.That(controller.Draft.InvalidConstructions, Is.Empty);
            Assert.That(controller.Draft.IsStructurallyValid, Is.True);
            Assert.That(controller.World.LegalBounds, Is.EqualTo(legalBounds));
            Assert.That(controller.World.GridTileCount, Is.EqualTo(gridTileCount));
            AssertCanonicalUnchanged(canonicalBefore, manaBefore);

            var quote = root.SaveService.PreviewDungeonDraft(root.Save, controller.Draft);
            Assert.That(quote.Cost, Is.EqualTo(setupQuote.Cost));
            Assert.That(quote.IsAffordable, Is.True);
            AssertCanonicalUnchanged(canonicalBefore, manaBefore);
            Click(ui.Q<Button>("save")); Click(ui.Q<Button>("confirm")); yield return null;
            Assert.That(controller.IsEditing, Is.False);
            Assert.That(root.Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms.Length, Is.EqualTo(2));
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(quote.RequiredReserveAfter));
            Assert.That(controller.World.LegalBounds, Is.EqualTo(legalBounds));
            Assert.That(root.SaveService.CreateDungeonDraftStore().Read(), Is.Null);
        }

        [UnityTest]
        public IEnumerator TransactionalRoomConstructionLayoutsLocalizationAndResume()
        {
            var root = GameRoot.Instance;
            var strings = (System.Collections.Generic.Dictionary<string, string>)typeof(ContentService)
                .GetField("_stringMap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(root.Content);
            strings["ui.dungeon.construction.properties"] = "建設する部屋の詳細を確認してください：{0}、寸法 {1} × {2}、モンスター {3}、トラップ {4}、戦利品 {5}。 This expanded localization checks wrapping and scrolling.";
            strings["ui.dungeon.construction.preview"] = "配置を確認してください。 Confirm Placement records this proposed room in the recoverable draft. Save Changes publishes the complete valid dungeon and charges its final cost.";
            controller.EnterEdit(); controller.SelectConstructionRoom("spatial.room.rectangle");
            Assert.That(controller.SelectConstructionOrientation(CardinalOrientation.Ninety), Is.True);
            Assert.That(controller.SelectTerminalConnection("north"), Is.True);
            if (controller.World.MoveGuidanceAnchors.Length == 0) Assert.That(controller.SelectConstructionOrientation(CardinalOrientation.Zero), Is.True);
            Assert.That(controller.World.MoveGuidanceAnchors, Is.Not.Empty);
            controller.TapWorld(controller.World.MoveGuidanceAnchors.First());
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            foreach (var size in new[] { new Vector2Int(720, 1280), new Vector2Int(1280, 720), new Vector2Int(1080, 1920), new Vector2Int(1920, 1080) })
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x, size.y);
                for (int i = 0; i < 12; i++) yield return null;
                controller.ApplySafeArea(new Rect(32, 40, Screen.width - 64, Screen.height - 120), new Vector2(Screen.width, Screen.height));
                foreach (var textSize in new[] { DungeonTextSize.Small, DungeonTextSize.Default, DungeonTextSize.Large })
                {
                    controller.SetTextSize(textSize); for (int i = 0; i < 8; i++) yield return null;
                    TestContext.WriteLine("Construction layout " + size + " " + textSize + ": " + string.Join("; ", new[] {
                        "topChrome", "bottomChrome", "viewport", "constructionCategories", "placementContext", "constructionSummary",
                        "constructionAffordability", "constructionOptions", "placementActions", "contextDetails", "draftSummary", "editActions", "legacy" }
                        .Select(id => id + "=" + ui.Q(id).worldBound)));
                    if (size.x == 1280 && textSize == DungeonTextSize.Large)
                    {
                        DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("construction-layout-diagnostic-large.png");
                        yield return null; yield return null;
                    }
                    foreach (string id in new[] { "roomsCategory", "collapseDetails", "save", "discard", "confirmPlacement", "cancelPlacement" })
                    {
                        var rect = ui.Q(id).worldBound;
                        Assert.That(rect.width, Is.GreaterThan(0), id); Assert.That(rect.height, Is.GreaterThan(0), id);
                        Assert.That(rect.yMax, Is.LessThanOrEqualTo(controller.SafeRoot.worldBound.yMax + 1), id);
                        AssertReachable(ui.Q<Button>(id));
                    }
                    Assert.That(ui.Q<Button>("confirmPlacement").GetFirstAncestorOfType<ScrollView>(), Is.Null);
                    Assert.That(ui.Q<Button>("cancelPlacement").GetFirstAncestorOfType<ScrollView>(), Is.Null);
                    Assert.That(ui.Q("placementActions").worldBound.yMax, Is.LessThanOrEqualTo(ui.Q("draftSummary").worldBound.yMin + 1));
                    Assert.That(ui.Q<Label>("capacity").worldBound.yMax, Is.LessThanOrEqualTo(ui.Q("viewport").worldBound.yMin + 1));
                    TestContext.WriteLine(size + " " + textSize + " safe=" + controller.SafeRoot.worldBound + " top=" + ui.Q("topChrome").worldBound + " bottom=" + ui.Q("bottomChrome").worldBound + " viewport=" + ui.Q("viewport").worldBound);
                    DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("construction-ui-layout-" + size.x + "x" + size.y + "-" + textSize + ".png");
                    yield return null; yield return null;
                    Assert.That(ui.Q("viewport").worldBound.height, Is.GreaterThan(controller.SafeRoot.worldBound.height * 0.25f));
                    Click(ui.Q<Button>("collapseDetails")); for (int i = 0; i < 8; i++) yield return null;
                    Assert.That(ui.Q("contextDetails").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                    Assert.That(ui.Q("viewport").worldBound.height, Is.GreaterThan(controller.SafeRoot.worldBound.height * 0.25f), "Expanded construction details retain usable map");
                    float informationLineHeight=ui.Q<Label>("constructionInfo").MeasureTextSize("Ag建",0,VisualElement.MeasureMode.Undefined,0,VisualElement.MeasureMode.Undefined).y;
                    Assert.That(ui.Q<ScrollView>("contextDetails").contentViewport.worldBound.height, Is.GreaterThanOrEqualTo(informationLineHeight), "Expanded information retains at least one complete text line");
                    TestContext.WriteLine("Construction expanded " + size + " " + textSize + " viewport=" + ui.Q("viewport").worldBound + " details=" + ui.Q<ScrollView>("contextDetails").contentViewport.worldBound);
                    DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("construction-expanded-" + size.x + "x" + size.y + "-" + textSize + ".png");
                    yield return null; yield return null;
                    ui.Q<ScrollView>("contextDetails").scrollOffset = new Vector2(0, 10000); yield return null;
                    Assert.That(ui.Q<Button>("confirmPlacement").worldBound.yMax, Is.LessThanOrEqualTo(controller.SafeRoot.worldBound.yMax + 1));
                    AssertReachable(ui.Q<Button>("confirmPlacement")); AssertReachable(ui.Q<Button>("cancelPlacement"));
                    Click(ui.Q<Button>("collapseDetails")); for (int i = 0; i < 8; i++) yield return null;
                    Assert.That(ui.Q("contextDetails").style.display.value, Is.EqualTo(DisplayStyle.None));
                    AssertReachable(ui.Q<Button>("confirmPlacement")); AssertReachable(ui.Q<Button>("cancelPlacement"));
                    Assert.That(ui.Q<Label>("constructionInfo").text, Does.Contain("建設"));
                    if(size.x>size.y)
                    {
                        Assert.That(ui.Q<Button>("legacy").parent,Is.SameAs(ui.Q("displayPopover")),"Optional development action moves out of construction's required action row");
                        Click(ui.Q<Button>("displaySettings")); yield return null; yield return null;
                        AssertReachable(ui.Q<Button>("legacy")); Click(ui.Q<Button>("closeDisplay")); yield return null; yield return null;
                        Click(ui.Q<Button>("displaySettings")); yield return null; yield return null;
                        Click(ui.Q<Button>("legacy")); yield return null; yield return null;
                        Assert.That(ui.Q("displayPopover").style.display.value,Is.EqualTo(DisplayStyle.None));
                        Assert.That(ui.Q<Button>("legacy").parent,Is.SameAs(controller.SafeRoot));
                        Click(ui.Q<Button>("legacy")); yield return null; yield return null;
                        Assert.That(ui.Q("displayPopover").style.display.value,Is.EqualTo(DisplayStyle.None));
                        Assert.That(ui.Q<Button>("legacy").parent,Is.SameAs(ui.Q("displayPopover")));
                        Assert.That(controller.IsConstructing,Is.True); Assert.That(controller.ConstructionPreview.IsValid,Is.True);
                    }
                    Assert.That(controller.World.GridTileCount, Is.EqualTo(root.ProductionSpatialContent.Catalog.Floors[0].Bounds.TileCount));
                }
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("construction-ui-corrected-large-" + size.x + "x" + size.y + ".png");
                yield return null; yield return null;
            }
            Click(ui.Q<Button>("confirmPlacement")); yield return null;
            int commands = controller.Draft.CommandCount;
            yield return RestartShell(); ui = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(ui.Q<Button>("confirm").text, Is.EqualTo(root.Content.GetString("ui.dungeon.resume", null)));
            Click(ui.Q<Button>("confirm")); yield return null;
            Assert.That(controller.Draft.CommandCount, Is.EqualTo(commands));
            Assert.That(controller.Draft.ReadModel.Floors[0].Layout.Rooms.Length, Is.EqualTo(2));
            Assert.That(controller.Discard(), Is.True);
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
            Click(document.rootVisualElement.Q<Button>("displaySettings"));
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
            Click(document.rootVisualElement.Q<Button>("closeDisplay"));
            const string captureName = "phase7a4-theme-normal-1080x1920.png";
            string export = Environment.GetEnvironmentVariable("DUNGEON_H1F4_TEST_EXPORT_ROOT");
            Assert.That(export, Is.Not.Null.And.Not.Empty, "Expected capture must belong to the qualification campaign");
            string captureDirectory = Path.Combine(Path.GetFullPath(export), "screenshots") + Path.DirectorySeparatorChar;
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("^Qualification capture: " +
                System.Text.RegularExpressions.Regex.Escape(captureDirectory) + "[0-9a-f]{8}-" +
                System.Text.RegularExpressions.Regex.Escape(captureName) + "$"));
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture(captureName);
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
            controller.TapWorld(target); Assert.That(controller.Draft.CommandCount, Is.Zero);
            Assert.That(controller.ContentPreview.IsValid, Is.True);
            Assert.That(controller.ConfirmContentPlacement(), Is.True); Assert.That(controller.Draft.CommandCount, Is.EqualTo(1));
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
            Assert.That(controller.ConfirmContentPlacement(), Is.True);
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
            root.Save.structureRuntime.ManaReserve += MonsterContinuity.RequiredReserve(
                root.Save.validatedCanonicalSpatialState, MonsterRecoveryRules.Capture(root.RunSimulationConfig));
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
                Assert.That(controller.IsChrome(ScreenPoint(point)), Is.True);
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
            Assert.That(ui.Q("constructionCategories").worldBound.yMax,
                Is.LessThanOrEqualTo(ui.Q("status").worldBound.yMin + 1),
                "Wrapped HUD must not shrink the sheet header over its status line");
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("phase7a4-long-localization-1080x1920.png");
            yield return null; yield return null;
            Assert.That(controller.Discard(), Is.True);
        }

        // Invoked by the PlayMode qualification adapter; EditMode coroutines do not
        // advance the MonoBehaviour device-read loop reliably.
        public IEnumerator InputSystemConstructionTapPreviewsWithoutAcknowledgingUntilConfirm()
        {
            Assert.That(Application.isPlaying, Is.True);
            var root = GameRoot.Instance; var before = File.ReadAllBytes(root.SaveService.SavePath); double mana = root.Save.structureRuntime.ManaReserve;
            controller.EnterEdit(); controller.OpenRooms(); Assert.That(controller.SelectConstructionRoom("spatial.room.basic"), Is.True);
            for (int i = 0; i < 10; i++) yield return null;
            simulatedTouch = InputSystem.AddDevice<Touchscreen>();
            var anchor = controller.World.MoveGuidanceAnchors.First();
            var camera = controller.transform.Find("ProductionDungeonCamera").GetComponent<Camera>();
            Vector2 point = camera.WorldToScreenPoint(new Vector3(anchor.X + 0.5f, anchor.Y + 0.5f, 0));
            Assert.That(controller.IsChrome(point, 1), Is.False);
            Touch(1, point, InputTouchPhase.Began); yield return null; yield return null;
            Touch(1, point, InputTouchPhase.Ended); yield return null; yield return null;
            Assert.That(controller.ConstructionPreview, Is.Not.Null); Assert.That(controller.ConstructionPreview.IsValid, Is.True);
            Assert.That(controller.ConstructionPreview.Anchor, Is.EqualTo(anchor)); Assert.That(controller.Draft.CommandCount, Is.Zero);
            var options = controller.GetComponent<UIDocument>().rootVisualElement.Q("constructionOptions");
            var option = options.Query<Button>().ToList().First();
            root.Save.totalTicks++; yield return null; yield return null;
            Assert.That(options.Query<Button>().ToList().First(), Is.SameAs(option), "Wallet/HUD ticks preserve option input lifetime");
            AssertCanonicalUnchanged(before, mana);
            var confirmButton = controller.GetComponent<UIDocument>().rootVisualElement.Q<Button>("confirmPlacement");
            AssertReachable(confirmButton);
            Vector2 confirmPoint = ScreenPoint(confirmButton.worldBound.center);
            Touch(1, confirmPoint, InputTouchPhase.Began); yield return null; yield return null;
            Touch(1, confirmPoint, InputTouchPhase.Ended); yield return null; yield return null;
            Assert.That(controller.Draft.AcknowledgedSequence, Is.EqualTo(1)); AssertCanonicalUnchanged(before, mana);
            Assert.That(controller.Discard(), Is.True);
        }

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

        private Vector2 ScreenPoint(Vector2 panelPoint)
        {
            float scale=controller.GetComponent<UIDocument>().panelSettings.scale;
            return new Vector2(panelPoint.x*scale,Screen.height-panelPoint.y*scale);
        }
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
            Assert.That(controller.Draft.CommandCount, Is.Zero);
            Assert.That(controller.ConfirmContentPlacement(), Is.True);
            yield return null;
            Assert.That(controller.Draft.AcknowledgedSequence, Is.EqualTo(1));
            controller.BeginMove(); controller.TapWorld(pending);
            Assert.That(controller.ConfirmContentPlacement(), Is.True);
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
                var reload = SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
                while (!reload.isDone) yield return null;
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
                Assert.That(ui.Q("edit").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                Assert.That(controller.IsEditing, Is.False);
                Assert.That(controller.World.GridVisible, Is.False);
                Assert.That(controller.World.ActiveEntityCount, Is.Zero);
                Assert.That(ui.Q("floorSummary").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(ui.Q<Label>("floorInfo").text, Is.EqualTo(GameRoot.Instance.Content.GetString("ui.dungeon.first_room.empty", null)));
                controller.EnterEdit(); controller.OpenRooms();
                Assert.That(ui.Q("roomChoices").childCount, Is.Zero);
                Assert.That(controller.World.MoveGuidanceAnchors, Is.Empty);
                Assert.That(controller.Discard(), Is.True);
            }
        }

        public IEnumerator FirstRoomConfirmationUsesCanonicalPlacementAndReopens()
        {
            yield return ExplicitDeleteShell(false);
            var root = GameRoot.Instance;
            // The deletion fixture stops the replacement root for its inert-shell
            // assertions. The starter action requires the normal foreground owner.
            root.enabled = true;
            PumpSceneToClosed(establishForeground: true);
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var build = ui.Q<Button>("buildFirstRoom");
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(720, 1280);
            controller.SetTextSize(DungeonTextSize.Large);
            for (int i = 0; i < 12; i++) yield return null;
            controller.ApplySafeArea(new Rect(24, 24, Screen.width - 48, Screen.height - 72),
                new Vector2(Screen.width, Screen.height));
            yield return null;
            AssertReachable(build);

            byte[] before = File.ReadAllBytes(root.SaveService.SavePath);
            double mana = root.Save.structureRuntime.ManaReserve;
            Click(build);
            yield return null;
            Assert.That(ui.Q<Label>("modalText").text, Is.EqualTo(root.Content.GetString("ui.dungeon.first_room.review", null)));
            AssertReachable(ui.Q<Button>("cancel"));
            AssertReachable(ui.Q<Button>("confirm"));
            Click(ui.Q<Button>("cancel"));
            AssertCanonicalUnchanged(before, mana);
            Assert.That(root.Save.validatedCanonicalSpatialState.Floors, Is.Empty);

            root.Save.structureRuntime.PlacementLocked = true;
            Click(build);
            Click(ui.Q<Button>("confirm"));
            Assert.That(ui.Q<Label>("status").text, Is.EqualTo(root.Content.GetString("ui.banner.place_blocked_heat_crisis", null)));
            AssertCanonicalUnchanged(before, mana);
            root.Save.structureRuntime.PlacementLocked = false;

            controller.EnterEdit();
            Click(build);
            Assert.That(ui.Q("modal").style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(controller.IsEditing, Is.True);
            AssertCanonicalUnchanged(before, mana);
            Assert.That(controller.Discard(), Is.True);

            Click(build);
            Assert.That(ui.Q<Label>("modalText").text, Is.EqualTo(root.Content.GetString("ui.dungeon.first_room.review", null)));
            Click(ui.Q<Button>("confirm"));
            Assert.That(root.Save.validatedCanonicalSpatialState.Floors, Has.Length.EqualTo(1), ui.Q<Label>("status").text);
            var floor = root.Save.validatedCanonicalSpatialState.Floors.Single();
            var room = floor.Layout.Rooms.Single();
            Assert.That(floor.ActivationState, Is.EqualTo(FloorActivationState.Active));
            Assert.That(room.RoomDefinitionId, Is.EqualTo("spatial.room.basic"));
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(mana));
            Assert.That(root.SaveService.CreateDungeonDraftStore().Read(), Is.Null);
            Assert.That(build.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(ui.Q("edit").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(controller.World.TryRoomBounds(room.RoomInstanceId, out var bounds), Is.True);
            Assert.That(bounds.width * bounds.height, Is.GreaterThan(0));
            byte[] after = File.ReadAllBytes(root.SaveService.SavePath);
            Assert.That(after, Is.Not.EqualTo(before));
            Click(build);
            Assert.That(ui.Q("modal").style.display.value, Is.EqualTo(DisplayStyle.None));
            AssertCanonicalUnchanged(after, mana);

            UnityEngine.Object.Destroy(controller.gameObject);
            UnityEngine.Object.Destroy(root.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
            for (int i = 0; i < 120 && GameRoot.Instance?.Save == null; i++) yield return null;
            root = GameRoot.Instance;
            Assert.That(Path.GetFileName(root.SaveService.SavePath), Is.EqualTo(filename));
            Assert.That(root.Save.validatedCanonicalSpatialState.Floors.Single().Layout.Rooms.Single().RoomInstanceId,
                Is.EqualTo(room.RoomInstanceId));
            controller = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();
            for (int i = 0; i < 30 && controller.World == null; i++) yield return null;
            ui = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(ui.Q("buildFirstRoom").style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(controller.World.TryRoomBounds(room.RoomInstanceId, out _), Is.True);
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
                yield return StabilizeReadOnlySceneBaseline(); byte[] canonical = File.ReadAllBytes(root.SaveService.SavePath);
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
            Assert.That(controller.ConfirmContentPlacement(), Is.True);
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
        [UnityTest]
        public IEnumerator OwnerUsabilityConstructionAndContentReachability()
        {
            var root = GameRoot.Instance;
            Assert.That(root.TrySetQaManaFromDevPanel(true), Is.True);
            // Preserve the complete save produced by the existing starter mutation
            // and QA funding authority for the later isolated owner handoff.
            // No save JSON is authored or imported from an owner/normal save.
            // The existing qualification root owns retained exports. Each invocation
            // keeps its own complete starter, including consecutive Editor/Play runs.
            string starterDirectory = Path.Combine(DungeonBuilder.M0.EditorTools.H1F4QualificationArtifacts.RequireOutputRoot().FullName, "owner-starters");
            Directory.CreateDirectory(starterDirectory);
            string starter = Path.Combine(starterDirectory, Guid.NewGuid().ToString("N") + ".json");
            File.Copy(root.SaveService.SavePath,starter,false);
            CollectionAssert.AreEqual(root.SaveService.CanonicalSession.GetCurrentBytes(), File.ReadAllBytes(starter));
            TestContext.WriteLine("Owner starter artifact: "+starter);
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var room = root.Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms[0];
            foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(720,1280), new Vector2Int(1080,1920), new Vector2Int(1920,1080) })
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x,size.y);
                for (int frame=0;frame<12;frame++) yield return null;
                foreach (var text in new[] { DungeonTextSize.Large, DungeonTextSize.Default, DungeonTextSize.Small })
                {
                    controller.SetTextSize(text);
                    controller.EnterEdit();
                    controller.TapWorld(new TileCoordinate(room.Anchor.X+1,room.Anchor.Y+1));
                    Assert.That(controller.OpenContentCategory("placement.category.trap"),Is.True);
                    yield return WaitForViewportGeometry();
                    DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("owner-browser-"+size.x+"x"+size.y+"-"+text+".png");
                    yield return null; yield return null;
                    var browser = ui.Q("contentChoices");
                    var card = browser.Query<Button>().ToList().Single(b=>b.userData as string=="placement.option.trap.spike");
                    var details = ui.Q<ScrollView>("contextDetails");
                    card.GetFirstAncestorOfType<ScrollView>().ScrollTo(card); yield return null; yield return null;
                    details.ScrollTo(card); yield return null; yield return null;
                    TestContext.WriteLine("Owner browser "+size+" "+text+" card="+card.worldBound+" details="+details.contentViewport.worldBound);
                    AssertReachable(card);
                    Assert.That(details.contentViewport.worldBound.yMax,Is.GreaterThanOrEqualTo(card.worldBound.yMax-1),"Whole card is visible, not just its center");
                    DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("owner-browser-scrolled-"+size.x+"x"+size.y+"-"+text+".png");
                    yield return null; yield return null;
                    Assert.That(controller.SelectNewContent("placement.option.trap.spike"),Is.True);
                    yield return WaitForViewportGeometry();
                    var camera=controller.GetComponentInChildren<Camera>();
                    float tilePixels=camera.pixelHeight/(camera.orthographicSize*2);
                    TestContext.WriteLine("Owner content "+size+" "+text+" viewport="+camera.pixelRect+" size="+camera.orthographicSize+" tilePixels="+tilePixels);
                    DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("owner-trap-"+size.x+"x"+size.y+"-"+text+".png");
                    yield return null; yield return null;
                    // The same native target policy used for UI buttons also provides a useful
                    // lower bound for the selected room's high-frequency tile interaction.
                    Assert.That(tilePixels,Is.GreaterThanOrEqualTo(controller.presentationPolicy.DesktopMinimumPixels),"Entering content placement must frame the selected room without a hidden extra Focus action");
                    AssertReachable(ui.Q<Button>("cancelContent")); AssertReachable(ui.Q<Button>("confirmContent"));
                    var pointer=InputSystem.AddDevice<Mouse>();
                    try
                    {
                        foreach(var cell in new[] {new TileCoordinate(room.Anchor.X+1,room.Anchor.Y+1),new TileCoordinate(room.Anchor.X+2,room.Anchor.Y+1)})
                        {
                            yield return WaitForViewportGeometry();
                            Vector2 point=camera.WorldToScreenPoint(new Vector3(cell.X+.5f,cell.Y+.5f,0));
                            Assert.That(controller.IsChrome(point,pointer.deviceId),Is.False);
                            Assert.That(controller.Viewport.ScreenToWorld(point).x,Is.EqualTo(cell.X+.5f).Within(.001));
                            InputSystem.QueueStateEvent(pointer,new MouseState {position=point}.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                            yield return null; yield return null;
                            InputSystem.QueueStateEvent(pointer,new MouseState {position=point});
                            yield return null; yield return null;
                            Assert.That(controller.ContentPreview?.IsValid,Is.True);
                            Assert.That(controller.World.TryRoomLocal(room.RoomInstanceId,cell,out var local),Is.True);
                            Assert.That(controller.ContentPreview.Assignment.RoomLocalPosition,Is.EqualTo(local));
                            Assert.That(controller.Draft.CommandCount,Is.Zero,"Moving a preview never confirms it");
                            Click(ui.Q<Button>("collapseDetails"));
                        }
                    }
                    finally { InputSystem.RemoveDevice(pointer); }
                    controller.TapWorld(new TileCoordinate(-1,-1));
                    Assert.That(controller.ContentPreview.IsValid,Is.False);
                    Click(ui.Q<Button>("cancelContent")); yield return null;
                    Assert.That(controller.Draft.CommandCount,Is.Zero);
                    Assert.That(controller.Discard(),Is.True); yield return null;
                    controller.EnterEdit(); Assert.That(controller.SelectConstructionRoom("spatial.room.rectangle"),Is.True);
                    controller.SelectConstructionOrientation(CardinalOrientation.Ninety);
                    Assert.That(controller.World.MoveGuidanceAnchors,Is.Not.Empty);
                    controller.TapWorld(controller.World.MoveGuidanceAnchors.First());
                    yield return WaitForViewportGeometry();
                    TestContext.WriteLine("Owner construction "+size+" "+text+" camera="+camera.pixelRect+" size="+camera.orthographicSize+
                        " tilePixels="+camera.pixelHeight/(camera.orthographicSize*2));
                    DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("owner-options-"+size.x+"x"+size.y+"-"+text+".png");
                    yield return null; yield return null;
                    var options=ui.Q("constructionOptions"); var strip=options.GetFirstAncestorOfType<ScrollView>();
                    var buttons=options.Query<Button>().ToList();
                    Assert.That(buttons.Count,Is.GreaterThan(2));
                    foreach(var option in buttons)
                    {
                        strip.ScrollTo(option); yield return null; yield return null;
                        AssertReachable(option);
                        Assert.That(option.worldBound.xMin,Is.GreaterThanOrEqualTo(strip.contentViewport.worldBound.xMin-1));
                        Assert.That(option.worldBound.xMax,Is.LessThanOrEqualTo(strip.contentViewport.worldBound.xMax+1));
                    }
                    if(options.worldBound.width>strip.contentViewport.worldBound.width+1)
                        Assert.That(strip.horizontalScroller.resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex),"Overflow must expose a discoverable scroll control");
                    AssertReachable(ui.Q<Button>("confirmPlacement")); AssertReachable(ui.Q<Button>("cancelPlacement"));
                    DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("owner-options-end-"+size.x+"x"+size.y+"-"+text+".png");
                    yield return null; yield return null;
                    Click(buttons.Last()); yield return null; yield return null;
                    Assert.That(controller.ConstructionPreview,Is.Null,"Reachable terminal choice dispatches its real UI action");
                    Assert.That(controller.Draft.CommandCount,Is.Zero);
                    Click(ui.Q<Button>("collapseDetails")); yield return WaitForViewportGeometry();
                    DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("owner-funded-expanded-"+size.x+"x"+size.y+"-"+text+".png");
                    yield return null; yield return null;
                    AssertReachable(ui.Q<Button>("cancelPlacement"));
                    Assert.That(controller.Discard(),Is.True); yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator CombinesBothCornerDoorwaysThroughMapAndSavesConvertedConnection()
        {
            var root = GameRoot.Instance;
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var floor = root.Save.validatedCanonicalSpatialState.Floors.Single(value => value.FloorIndex == 1);
            var camera = GameObject.Find("ProductionDungeonCamera").GetComponent<Camera>();
            var canonical = root.SaveService.CanonicalSession.GetCurrentBytes();
            var source = floor.Layout.Nodes.Single(node => node.RoomInstanceId ==
                floor.Layout.Rooms.Single(room => room.Anchor.Equals(new TileCoordinate(2, 5))).RoomInstanceId);
            var destination = floor.Layout.Nodes.Single(node => node.RoomInstanceId ==
                floor.Layout.Rooms.Single(room => room.Anchor.Equals(new TileCoordinate(6, 5))).RoomInstanceId);
            var direct = floor.Layout.Edges.Single(edge => edge.SourceNodeId == source.NodeId && edge.DestinationNodeId == destination.NodeId);
            Assert.That(direct.ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.DirectDoorway));
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(1280, 720);
            controller.SelectFloor(floor.FloorInstanceId);
            Click(ui.Q<Button>("edit"));
            Click(ui.Q<Button>("renovateRoutes"));
            var pointer = InputSystem.AddDevice<Mouse>();
            try
            {
                yield return Tap(new Vector2(6, 6.5f));
                Assert.That(ui.Q<Button>("acceptRoute").enabledSelf, Is.True, ui.Q<Label>("routeState").text);
                yield return Tap(Door(source.NodeId, "east"));
                yield return Tap(Door(source.NodeId, "perimeter.north.3"));
                Assert.That(ui.Q<Button>("acceptRoute").enabledSelf, Is.False);
                Assert.That(ui.Q<Button>("save").enabledSelf, Is.False);
                Assert.That(controller.Draft.CommandCount, Is.Zero);
                Assert.That(ui.Q<Button>("beginManualRoute").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                Click(ui.Q<Button>("beginManualRoute"));
                Assert.That(ui.Q<Button>("finishManualRoute").enabledSelf, Is.False);

                Click(ui.Q<Button>("cancelRouteAdjustment"));
                Assert.That(ui.Q<Button>("acceptRoute").enabledSelf, Is.True);
                yield return Tap(Door(source.NodeId, "east"), Door(source.NodeId, "perimeter.north.3"));
                Assert.That(ui.Q<Button>("acceptRoute").enabledSelf, Is.False);
                yield return Tap(Door(destination.NodeId, "west"));
                yield return Tap(Door(destination.NodeId, "perimeter.north.0"));
                Assert.That(ui.Q<Button>("acceptRoute").enabledSelf, Is.True, ui.Q<Label>("routeState").text);
                CollectionAssert.AreEqual(canonical, root.SaveService.CanonicalSession.GetCurrentBytes());
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("renovation-combined-corner-doors.png");

                Click(ui.Q<Button>("acceptRoute"));
                yield return null;
                yield return null;
                Assert.That(controller.Draft.CommandCount, Is.EqualTo(1));
                var draftFloor = controller.Draft.ReadModel.Floors.Single(value => value.FloorIndex == 1);
                var accepted = draftFloor.Layout.Edges.Single(edge => edge.EdgeId == direct.EdgeId);
                Assert.That(accepted.AuthoredGeometry.SourcePointId, Is.EqualTo("perimeter.north.3"));
                Assert.That(accepted.AuthoredGeometry.DestinationPointId, Is.EqualTo("perimeter.north.0"));
                CollectionAssert.AreEqual(new[] { new TileCoordinate(5, 9), new TileCoordinate(6, 9) },
                    accepted.AuthoredGeometry.Passage);
                CollectionAssert.AreEqual(canonical, root.SaveService.CanonicalSession.GetCurrentBytes());
                Click(ui.Q<Button>("cancelRoute"));
                Assert.That(ui.Q<Button>("save").enabledSelf, Is.True);
                Click(ui.Q<Button>("save"));
                Click(ui.Q<Button>("confirm"));
                Assert.That(controller.IsEditing, Is.False);
                var committed = root.Save.validatedCanonicalSpatialState.Floors.Single(value => value.FloorIndex == 1)
                    .Layout.Edges.Single(edge => edge.EdgeId == direct.EdgeId);
                Assert.That(JsonUtility.ToJson(committed.AuthoredGeometry), Is.EqualTo(JsonUtility.ToJson(accepted.AuthoredGeometry)));

                // All three required links on this floor are now physical. Independent
                // placement must still be available without the legacy direct insertion edge.
                Click(ui.Q<Button>("edit"));
                Click(ui.Q<Button>("roomsCategory"));
                Click(ui.Query<Button>(className: "room-card").ToList()
                    .Single(button => (string)button.userData == "spatial.room.basic"));
                yield return Tap(new Vector2(1.5f, 9.5f));
                Assert.That(controller.ConstructionPreview.IsValid, Is.True);
                Assert.That(controller.ConstructionPreview.Operation, Is.EqualTo(StructuralEditOperation.IndependentRoomPlacement));
                Assert.That(controller.Discard(), Is.True);
            }
            finally { InputSystem.RemoveDevice(pointer); }

            Vector2 Door(string nodeId, string openingId)
            {
                var opening = AuthoredRouteGeometry.Openings(floor, nodeId, root.ProductionSpatialContent.Catalog)
                    .Single(value => value.Id == openingId);
                float angle = -(int)opening.Facing * Mathf.PI / 2f;
                return new Vector2(opening.World.X + .5f, opening.World.Y + .5f) +
                    new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * .26f;
            }

            IEnumerator Tap(Vector2 world, Vector2? end = null)
            {
                yield return WaitForViewportGeometry();
                Vector2 start = camera.WorldToScreenPoint(world);
                Vector2 release = camera.WorldToScreenPoint(end ?? world);
                Assert.That(controller.Viewport.ScreenRect.Contains(start) && !controller.IsChrome(start, pointer.deviceId), Is.True);
                Assert.That(controller.Viewport.ScreenRect.Contains(release) && !controller.IsChrome(release, pointer.deviceId), Is.True);
                InputSystem.QueueStateEvent(pointer, new MouseState { position = start }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                yield return null;
                yield return null;
                if (end.HasValue)
                {
                    InputSystem.QueueStateEvent(pointer, new MouseState { position = release }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                    yield return null;
                    yield return null;
                }
                InputSystem.QueueStateEvent(pointer, new MouseState { position = release });
                yield return null;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator AuthorsStraightAndBentOptionalCorridorsThroughProductionMap()
        {
            var root = GameRoot.Instance;
            Assert.That(root.SetBasicBranchingCompletionForQa(true), Is.True);
            Assert.That(root.TrySetQaManaFromDevPanel(true), Is.True);
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var floor = root.Save.validatedCanonicalSpatialState.Floors.Single();
            string room = floor.Layout.Nodes.Single(node => node.Kind == FloorRouteNodeKind.Room).NodeId;
            var openings = AuthoredRouteGeometry.Openings(floor, room, root.ProductionSpatialContent.Catalog);
            var east = openings.Single(opening => opening.Id == "east");
            var west = openings.Single(opening => opening.Id == "west");
            var camera = GameObject.Find("ProductionDungeonCamera").GetComponent<Camera>();
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(1280, 720);
            Click(ui.Q<Button>("edit"));
            Click(ui.Q<Button>("renovateRoutes"));
            Click(ui.Q<Button>("startOptionalBranch"));
            var pointer = InputSystem.AddDevice<Mouse>();
            var accepted = ui.Q<Button>("acceptOptionalBranch");
            try
            {
                yield return Tap(Center(east.World) + Vector2.left);
                for (int length = 1; length <= 6; length++)
                {
                    if (length > 1) yield return Tap(Center(new TileCoordinate(east.World.X + 1, east.World.Y)));
                    yield return Tap(Center(east.World) + Vector2.right * .26f);
                    yield return Tap(Center(new TileCoordinate(east.World.X + length, east.World.Y)));
                    Assert.That(accepted.enabledSelf && accepted.style.display.value == DisplayStyle.Flex, Is.True,
                        $"Corridor length {length}: {ui.Q<Label>("routeState").text}");
                    Click(accepted);
                    yield return null;
                    yield return null;
                    var edge = controller.Draft.ReadModel.Floors.Single().Layout.Edges.Single(value => value.Classification == RouteClassification.Optional);
                    CollectionAssert.AreEqual(Enumerable.Range(1, length)
                        .Select(offset => new TileCoordinate(east.World.X + offset, east.World.Y)), edge.AuthoredGeometry.Passage);
                }
                yield return Tap(Center(new TileCoordinate(east.World.X + 1, east.World.Y)));
                yield return Tap(Center(east.World) + Vector2.right * .26f);
                int acknowledged = controller.Draft.CommandCount;
                yield return Tap(Center(new TileCoordinate(east.World.X + 7, east.World.Y)));
                Rejected("over length");
                yield return Tap(Center(new TileCoordinate(east.World.X + 1, east.World.Y + 1)));
                Rejected("diagonal first segment");
                yield return Tap(Center(new TileCoordinate(east.World.X, east.World.Y - 1)));
                Rejected("wrong-facing first segment");
                yield return Tap(Center(west.World) + Vector2.left * .26f);
                yield return Tap(Center(new TileCoordinate(west.World.X - 1, west.World.Y)));
                Rejected("out-of-bounds west of the first room");
                yield return Tap(Center(east.World) + Vector2.right * .26f);
                yield return Tap(Center(new TileCoordinate(east.World.X + 1, east.World.Y)));
                yield return Tap(Center(new TileCoordinate(east.World.X + 1, east.World.Y + 1)));
                yield return Tap(Center(new TileCoordinate(east.World.X, east.World.Y + 1)));
                Rejected("occupied room tile after turn");
                yield return Tap(Center(new TileCoordinate(east.World.X + 2, east.World.Y + 1)));
                Assert.That(accepted.enabledSelf, Is.True, ui.Q<Label>("routeState").text);
                Click(accepted);
                yield return null;
                yield return null;
                var bent = controller.Draft.ReadModel.Floors.Single().Layout.Edges.Single(value => value.Classification == RouteClassification.Optional);
                CollectionAssert.AreEqual(new[] { new TileCoordinate(east.World.X + 1, east.World.Y),
                    new TileCoordinate(east.World.X + 1, east.World.Y + 1), new TileCoordinate(east.World.X + 2, east.World.Y + 1) },
                    bent.AuthoredGeometry.Passage);
                var quote = root.SaveService.PreviewDungeonDraft(root.Save, controller.Draft);
                Click(ui.Q<Button>("cancelRoute"));
                Click(ui.Q<Button>("save"));
                Assert.That(ui.Q<Label>("modalText").text, Does.Contain("Forfeited").And.Contain("Fresh spending"));
                Click(ui.Q<Button>("confirm"));
                Assert.That(controller.IsEditing, Is.False);
                Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(quote.ResultingMana));
                var committed = root.Save.validatedCanonicalSpatialState.Floors.Single().Layout.Edges.Single(value =>
                    value.Classification == RouteClassification.Optional);
                CollectionAssert.AreEqual(bent.AuthoredGeometry.Passage, committed.AuthoredGeometry.Passage);
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture("renovation-bent-optional-passage.png");

                void Rejected(string reason)
                {
                    Assert.That(!accepted.enabledSelf || accepted.style.display.value == DisplayStyle.None, Is.True, reason);
                    Assert.That(controller.Draft.CommandCount, Is.EqualTo(acknowledged), reason);
                    Assert.That(ui.Q<Label>("routeState").text, Is.Not.Empty, reason);
                }
            }
            finally { InputSystem.RemoveDevice(pointer); }

            static Vector2 Center(TileCoordinate tile) => new Vector2(tile.X + .5f, tile.Y + .5f);

            IEnumerator Tap(Vector2 world)
            {
                yield return WaitForViewportGeometry();
                Vector2 point = camera.WorldToScreenPoint(world);
                Assert.That(controller.Viewport.ScreenRect.Contains(point) && !controller.IsChrome(point, pointer.deviceId),
                    Is.True, $"Pointer must reach map tile {world}");
                InputSystem.QueueStateEvent(pointer, new MouseState { position = point }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(pointer, new MouseState { position = point });
                yield return null;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ManipulatesFloorTwoDoorwaysSectionsBendsAndTiles()
        {
            var root = GameRoot.Instance;
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var floor = root.Save.validatedCanonicalSpatialState.Floors.Single(f => f.FloorIndex == 1);
            var camera = GameObject.Find("ProductionDungeonCamera").GetComponent<Camera>();
            byte[] canonical = root.SaveService.CanonicalSession.GetCurrentBytes();
            DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(1280, 720);
            controller.SelectFloor(floor.FloorInstanceId);
            Click(ui.Q<Button>("edit"));
            Click(ui.Q<Button>("roomsCategory"));
            Click(ui.Query<Button>(className: "room-card").ToList().Single(b => (string)b.userData == "spatial.room.basic"));
            var pointer = InputSystem.AddDevice<Mouse>();
            try
            {
                yield return Tap(new Vector2(2.5f, 10.5f));
                Assert.That(controller.ConstructionPreview.IsValid, Is.True);
                Click(ui.Q<Button>("confirmPlacement"));
                yield return Tap(new Vector2(4, 7));
                yield return Tap(new Vector2(4, 12));
                yield return Tap(new Vector2(8, 7));
                Assert.That(ui.Q<Button>("acceptRoute").enabledSelf, Is.True, ui.Q<Label>("routeState").text);
                var a = floor.Layout.Nodes.Single(n => n.RoomInstanceId ==
                    floor.Layout.Rooms.Single(r => r.Anchor.Equals(new TileCoordinate(2,5))).RoomInstanceId).NodeId;
                var b = floor.Layout.Nodes.Single(n => n.RoomInstanceId ==
                    floor.Layout.Rooms.Single(r => r.Anchor.Equals(new TileCoordinate(6,5))).RoomInstanceId).NodeId;
                string placedRoom = Preview().RoomInstanceId;
                var placed = controller.Draft.ReadModel.Floors.Single(f => f.FloorInstanceId == floor.FloorInstanceId)
                    .Layout.Nodes.Single(n => n.RoomInstanceId == placedRoom).NodeId;
                var manipulations = new AuthoredRouteGeometry { SourcePointId = "east", DestinationPointId = "north",
                    Passage = new[] { new TileCoordinate(6, 11), new TileCoordinate(6, 10),
                        new TileCoordinate(6, 9), new TileCoordinate(7, 9) } };
                yield return SelectFixtureDoorways(false, placed, b, manipulations, Door, point => Tap(point));
                var initial = Preview();
                string original = JsonUtility.ToJson(initial);

                yield return Tap(Door(a, initial.Incoming.SourcePointId));
                Assert.That(JsonUtility.ToJson(Preview()), Is.EqualTo(original));
                string otherNorth = initial.Incoming.SourcePointId == "north" ? "north.2" : "north";
                yield return Tap(Door(a, otherNorth));
                Assert.That(Preview().Incoming.SourcePointId, Is.EqualTo(otherNorth), ui.Q<Label>("routeState").text);
                Assert.That(Preview().Incoming.DestinationPointId, Is.EqualTo(initial.Incoming.DestinationPointId));
                Assert.That(JsonUtility.ToJson(Preview().Outgoing), Is.EqualTo(JsonUtility.ToJson(initial.Outgoing)));
                Capture("doorway");
                yield return ButtonTap("undoRouteAdjustment");
                Assert.That(JsonUtility.ToJson(Preview()), Is.EqualTo(original));

                yield return Tap(Door(placed, initial.Incoming.DestinationPointId));
                yield return Tap(Door(placed, "west"));
                Assert.That(Preview().Incoming.DestinationPointId, Is.EqualTo("west"), ui.Q<Label>("routeState").text);
                Assert.That(Preview().Incoming.SourcePointId, Is.EqualTo(initial.Incoming.SourcePointId));
                Assert.That(JsonUtility.ToJson(Preview().Outgoing), Is.EqualTo(JsonUtility.ToJson(initial.Outgoing)));
                yield return ButtonTap("undoRouteAdjustment");
                Assert.That(JsonUtility.ToJson(Preview()), Is.EqualTo(original));

                // The exterior doors stay fixed as the straight run moves across the open strip.
                Assert.That(initial.Outgoing.Passage, Is.EqualTo(new[] { new TileCoordinate(6,11), new TileCoordinate(6,10),
                    new TileCoordinate(6,9), new TileCoordinate(7,9) }));
                yield return Tap(new Vector2(6.5f, 11f));
                yield return Tap(new Vector2(7.5f, 10.5f));
                string movedSection = JsonUtility.ToJson(Preview());
                AssertMoved(new TileCoordinate(7,10));
                Capture("section");
                yield return ButtonTap("undoRouteAdjustment");
                yield return Tap(new Vector2(6.5f, 11f), new Vector2(7.5f, 10.5f));
                Assert.That(JsonUtility.ToJson(Preview()), Is.EqualTo(movedSection));
                yield return ButtonTap("undoRouteAdjustment");

                yield return Tap(new Vector2(6.5f, 9.5f));
                yield return Tap(new Vector2(7.5f, 11.5f));
                AssertMoved(new TileCoordinate(7,11));
                Capture("bend");
                yield return ButtonTap("undoRouteAdjustment");

                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(720, 1280);
                var touch = InputSystem.AddDevice<Touchscreen>();
                try
                {
                    yield return TouchTap(touch, new Vector2(6.5f, 10.5f));
                    yield return TouchTap(touch, new Vector2(7.5f, 10.5f));
                }
                finally { InputSystem.RemoveDevice(touch); }
                AssertMoved(new TileCoordinate(7,10));
                Capture("tile");
                yield return ButtonTap("undoRouteAdjustment");
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(1280, 720);

                yield return Tap(new Vector2(6.5f, 10.5f));
                yield return Tap(new Vector2(12.5f, 5.5f));
                Assert.That(ui.Q<Label>("routeState").text, Is.EqualTo(
                    root.Content.GetString("ui.dungeon.route.passage_length", null)));
                Assert.That(JsonUtility.ToJson(Preview()), Is.EqualTo(original));
                yield return ButtonTap("cancelRouteAdjustment");

                yield return Tap(new Vector2(6.5f, 10.5f));
                yield return Tap(new Vector2(4.5f, 11.5f));
                Assert.That(ui.Q<Button>("acceptRoute").enabledSelf, Is.False);
                Assert.That(ui.Q<Label>("routeState").text, Does.Contain("overlaps"));
                Assert.That(JsonUtility.ToJson(Preview()), Is.EqualTo(original));
                Capture("blocked");
                yield return ButtonTap("cancelRouteAdjustment");
                Assert.That(ui.Q<Button>("acceptRoute").enabledSelf, Is.True);
                yield return Tap(new Vector2(8.5f, 11.5f));
                var empty = (Vector2)camera.WorldToScreenPoint(new Vector2(8.5f, 11.5f));
                InputSystem.QueueStateEvent(pointer, new MouseState { position = empty, scroll = new Vector2(0, 120) });
                yield return null;
                yield return null;
                var centerBeforePan = controller.Viewport.Center;
                yield return Tap(new Vector2(8.5f, 11.5f), new Vector2(10.5f, 11.5f));
                Assert.That(controller.Viewport.Center, Is.Not.EqualTo(centerBeforePan));
                Assert.That(JsonUtility.ToJson(Preview()), Is.EqualTo(original));
                Assert.That(controller.Draft.CommandCount, Is.EqualTo(1));
                CollectionAssert.AreEqual(canonical, root.SaveService.CanonicalSession.GetCurrentBytes());
                yield return ButtonTap("acceptRoute");
                Assert.That(ui.Q<Button>("save").enabledSelf, Is.True);
                var store = root.SaveService.CreateDungeonDraftStore();
                byte[] acceptedBytes = store.Read();
                Assert.That(acceptedBytes, Is.Not.Null.And.Not.Empty);
                AssertAccepted(original);
                yield return ButtonTap("reset");
                yield return Tap(new Vector2(4, 12));
                Assert.That(ui.Q<Button>("save").enabledSelf, Is.False);
                yield return Revise();
                Assert.That(ui.Q<Label>("routeDetail").text,
                    Is.EqualTo(root.Content.GetString("ui.dungeon.route.save_after_edit", null)));
                yield return AssertPublicationBlocked();
                Capture("save-blocked");
                yield return ButtonTap("cancelRoute");
                Assert.That(ui.Q<Button>("save").enabledSelf, Is.True);
                AssertAccepted(original);
                CollectionAssert.AreEqual(acceptedBytes, store.Read());
                CollectionAssert.AreEqual(canonical, root.SaveService.CanonicalSession.GetCurrentBytes());

                yield return Tap(new Vector2(4, 12));
                yield return Revise();
                var modified = Preview();
                yield return Tap(new Vector2(7.5f, 10.5f));
                yield return Tap(new Vector2(4.5f, 11.5f));
                Assert.That(ui.Q<Label>("routeState").text, Does.Contain("overlaps"));
                Assert.That(JsonUtility.ToJson(Preview()), Is.EqualTo(JsonUtility.ToJson(modified)));
                yield return AssertPublicationBlocked();

                yield return ButtonTap("cancelRouteAdjustment");
                Assert.That(ui.Q<Button>("acceptRoute").enabledSelf, Is.True);
                Assert.That(ui.Q<Button>("save").enabledSelf, Is.False);
                yield return ButtonTap("acceptRoute");
                AssertAccepted(JsonUtility.ToJson(modified));
                Assert.That(ui.Q<Button>("save").enabledSelf, Is.True);
                CollectionAssert.AreEqual(canonical, root.SaveService.CanonicalSession.GetCurrentBytes());
                yield return ButtonTap("save");
                Assert.That(ui.Q("modal").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                yield return ButtonTap("confirm");
                Assert.That(controller.IsEditing, Is.False);
                AssertCommitted(root.Save.validatedCanonicalSpatialState);
                CollectionAssert.AreEqual(root.SaveService.CanonicalSession.GetCurrentBytes(),
                    File.ReadAllBytes(root.SaveService.SavePath));

                UnityEngine.Object.Destroy(controller.gameObject);
                UnityEngine.Object.Destroy(root.gameObject);
                yield return null;
                yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
                for (int i = 0; i < 120 && GameRoot.Instance?.Save == null; i++) yield return null;
                root = GameRoot.Instance;
                controller = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();
                Assert.That(Path.GetFileName(root.SaveService.SavePath), Is.EqualTo(filename));

                AssertCommitted(root.Save.validatedCanonicalSpatialState);

                IEnumerator Revise()
                {
                    yield return Tap(Door(a, initial.Incoming.SourcePointId));
                    yield return Tap(Door(a, otherNorth));
                    yield return Tap(new Vector2(6.5f, 11f));
                    yield return Tap(new Vector2(7.5f, 10.5f));
                    Assert.That(Preview().Incoming.SourcePointId, Is.EqualTo(otherNorth));
                    Assert.That(Preview().Outgoing.Passage, Does.Contain(new TileCoordinate(7, 10)));
                    Assert.That(ui.Q<Button>("acceptRoute").enabledSelf, Is.True);
                }

                IEnumerator AssertPublicationBlocked()
                {
                    var save = ui.Q<Button>("save");
                    Assert.That(save.enabledSelf, Is.False);
                    float scale = controller.GetComponent<UIDocument>().panelSettings.scale;
                    Vector2 pixel = save.worldBound.center * scale;
                    var point = new Vector2(pixel.x, Screen.height - pixel.y);
                    yield return Pointer(point, point, false);
                    Assert.That(ui.Q("modal").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                    typeof(ProductionDungeonController).GetMethod("ReviewSave", System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic).Invoke(controller, null);
                    Assert.That(ui.Q("modal").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                    Assert.That(controller.Commit(), Is.False);
                    AssertAccepted(original);
                    CollectionAssert.AreEqual(acceptedBytes, store.Read());
                    CollectionAssert.AreEqual(canonical, root.SaveService.CanonicalSession.GetCurrentBytes());
                }

                void AssertAccepted(string expected) => Assert.That(
                    JsonUtility.ToJson(controller.Draft.AcceptedRouteInsertion(initial.RoomInstanceId)), Is.EqualTo(expected));

                void AssertCommitted(DetachedCanonicalSpatialSaveState state)
                {
                    var committedFloor = state.Floors.Single(f => f.FloorInstanceId == floor.FloorInstanceId);
                    var incoming = committedFloor.Layout.Edges.Single(edge => edge.DestinationNodeId == placed);
                    var outgoing = committedFloor.Layout.Edges.Single(edge => edge.SourceNodeId == placed);
                    Assert.That(JsonUtility.ToJson(incoming.AuthoredGeometry), Is.EqualTo(JsonUtility.ToJson(modified.Incoming)));
                    Assert.That(JsonUtility.ToJson(outgoing.AuthoredGeometry), Is.EqualTo(JsonUtility.ToJson(modified.Outgoing)));
                }

                void AssertMoved(TileCoordinate cell)
                {
                    var after = Preview();
                    Assert.That(ui.Q<Button>("acceptRoute").enabledSelf, Is.True, ui.Q<Label>("routeState").text);
                    Assert.That(after.Outgoing.Passage, Does.Contain(cell));
                    Assert.That(after.Outgoing.Passage, Is.Not.EqualTo(initial.Outgoing.Passage));
                    Assert.That(after.Outgoing.SourcePointId, Is.EqualTo(initial.Outgoing.SourcePointId));
                    Assert.That(after.Outgoing.DestinationPointId, Is.EqualTo(initial.Outgoing.DestinationPointId));
                    Assert.That(JsonUtility.ToJson(after.Incoming), Is.EqualTo(JsonUtility.ToJson(initial.Incoming)));
                }
            }
            finally { InputSystem.RemoveDevice(pointer); }

            // Read the transient model for exact equality assertions. Every edit uses device input.
            StructuralRouteInsertionRequest Preview() => (StructuralRouteInsertionRequest)typeof(ProductionDungeonController)
                .GetField("routeRequest", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(controller);

            Vector2 Door(string node, string id)
            {
                var draftFloor = controller.Draft.ReadModel.Floors.Single(f => f.FloorInstanceId == floor.FloorInstanceId);
                var opening = AuthoredRouteGeometry.Openings(draftFloor, node, root.ProductionSpatialContent.Catalog).Single(p => p.Id == id);
                float angle = -(int)opening.Facing * Mathf.PI / 2f;
                return new Vector2(opening.World.X + .5f, opening.World.Y + .5f) +
                    new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * .26f;
            }
            void Capture(string state) => DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.Capture(
                "route-manipulation-floor-two-" + state + ".png");

            IEnumerator Tap(Vector2 start, Vector2? end = null)
            {
                yield return WaitForViewportGeometry();
                var begin = (Vector2)camera.WorldToScreenPoint(start);
                var target = (Vector2)camera.WorldToScreenPoint(end ?? start);
                Assert.That(controller.Viewport.ScreenRect.Contains(begin) && !controller.IsChrome(begin, pointer.deviceId), Is.True, start.ToString());
                Assert.That(controller.Viewport.ScreenRect.Contains(target) && !controller.IsChrome(target, pointer.deviceId), Is.True, target.ToString());
                yield return Pointer(begin, target, end.HasValue);
            }
            IEnumerator TouchTap(Touchscreen device, Vector2 tile)
            {
                yield return WaitForViewportGeometry();
                Vector2 position = camera.WorldToScreenPoint(tile);
                Assert.That(controller.Viewport.ScreenRect.Contains(position), Is.True);
                InputSystem.QueueStateEvent(device, new TouchState { touchId = 1, position = position, phase = InputTouchPhase.Began });
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(device, new TouchState { touchId = 1, position = position, phase = InputTouchPhase.Ended });
                yield return null;
                yield return null;
            }
            IEnumerator ButtonTap(string name)
            {
                yield return WaitForViewportGeometry();
                var button = ui.Q<Button>(name);
                Assert.That(ui.panel.Pick(button.worldBound.center), Is.SameAs(button), name);
                float scale = controller.GetComponent<UIDocument>().panelSettings.scale;
                var pixel = button.worldBound.center * scale;
                var point = new Vector2(pixel.x, Screen.height - pixel.y);
                yield return Pointer(point, point, false);
            }
            IEnumerator Pointer(Vector2 begin, Vector2 target, bool drag)
            {
                InputSystem.QueueStateEvent(pointer, new MouseState { position = begin }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                yield return null;
                yield return null;
                if (drag)
                {
                    InputSystem.QueueStateEvent(pointer, new MouseState { position = target }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                    yield return null;
                    yield return null;
                }
                InputSystem.QueueStateEvent(pointer, new MouseState { position = target });
                yield return null;
                yield return null;
            }
        }

        private IEnumerator SelectFixtureDoorways(bool incoming, string source, string destination,
            AuthoredRouteGeometry expected, Func<string, string, Vector2> marker, Func<Vector2, IEnumerator> tap)
        {
            yield return SelectEndpoint(source, expected.SourcePointId, true);
            yield return SelectEndpoint(destination, expected.DestinationPointId, false);
            var selected = Link();
            Assert.That(selected.SourcePointId, Is.EqualTo(expected.SourcePointId));
            Assert.That(selected.DestinationPointId, Is.EqualTo(expected.DestinationPointId));
            CollectionAssert.AreEqual(expected.Passage, selected.Passage);

            IEnumerator SelectEndpoint(string node, string requestedId, bool start)
            {
                var link = Link();
                string activeId = start ? link.SourcePointId : link.DestinationPointId;
                if (activeId == requestedId) yield break;
                yield return tap(marker(node, activeId));
                yield return tap(marker(node, requestedId));
            }

            AuthoredRouteGeometry Link()
            {
                var request = (StructuralRouteInsertionRequest)typeof(ProductionDungeonController)
                    .GetField("routeRequest", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .GetValue(controller);
                return incoming ? request.Incoming : request.Outgoing;
            }
        }

        private IEnumerator WaitForViewportGeometry()
        {
            var document = controller.GetComponent<UIDocument>();
            var camera = controller.GetComponentInChildren<Camera>();
            Rect previous = default;
            Vector3 previousPosition = default;
            float previousSize = 0;
            int stableFrames = 0;
            for (int frame = 0; frame < 120; frame++)
            {
                yield return null;
                var bounds = document.rootVisualElement.Q("viewport").worldBound;
                float scale = document.panelSettings.scale;
                var expected = new Rect(bounds.x * scale, Screen.height - bounds.yMax * scale,
                    bounds.width * scale, bounds.height * scale);
                bool configured = expected.width > 0 && expected.height > 0 &&
                    Mathf.Abs(camera.pixelRect.x - expected.x) < 1 && Mathf.Abs(camera.pixelRect.y - expected.y) < 1 &&
                    Mathf.Abs(camera.pixelRect.width - expected.width) < 1 && Mathf.Abs(camera.pixelRect.height - expected.height) < 1 &&
                    controller.Viewport.ScreenRect == expected;
                bool stable = configured && expected == previous && camera.transform.position == previousPosition &&
                    camera.orthographicSize == previousSize;
                stableFrames = stable ? stableFrames + 1 : 0;
                previous = expected;
                previousPosition = camera.transform.position;
                previousSize = camera.orthographicSize;
                if (stableFrames >= 3) yield break;
            }
            Assert.Fail("Production viewport geometry did not settle before simulated screen input: expected=" + previous +
                "; camera=" + camera.pixelRect + "; viewport=" + controller.Viewport.ScreenRect);
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
        private void AssertReachable(Button button)
        {
            Rect bounds = button.worldBound;
            Rect safeBounds = controller.SafeRoot.worldBound;
            string diagnostic = $"{button.name} bounds {bounds}, safe area {safeBounds}";
            Assert.That(bounds.width, Is.GreaterThan(0), diagnostic);
            Assert.That(safeBounds.Contains(bounds.min), Is.True, diagnostic);
            Assert.That(safeBounds.Contains(bounds.max - Vector2.one), Is.True, diagnostic);
            var picked = button.panel.Pick(button.worldBound.center);
            Assert.That(picked == button || button.Contains(picked), Is.True, button.name + " must receive pointer input without scrolling");
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
