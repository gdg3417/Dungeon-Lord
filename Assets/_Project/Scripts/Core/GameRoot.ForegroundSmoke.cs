#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0
{
    public partial class GameRoot
    {
        [Serializable] internal sealed class ForegroundSmokeReport
        {
            public bool Passed, MarkerVisible, ActiveEditCommitted, SecondRunUsesEditedGeometry;
            public string Phase, Product, SavePath, RunId, CheckpointIdentity, PendingGeometry, Failure;
            public long Accepted, Closed, Ordinal, HistoryCount, RealSourceUnits;
            public int VisualChecks, VisualLabelsShown, VisualLabelsHidden;
            public int VisualRequestedLayoutsUnavailable;
            public string VisualBounds;
            public int BlockerSequence, WaitingSequence, NextRunSequence;
            public string AStartIdentity, BStartIdentity;
            public int[] PartySequences;
            public string[] PartyCheckpoints;
            public long BlockerAdmittedAt, WaiterAdmittedAt, BlockedAt, ReleasedAt, BlockerTerminalAt, BlockerTerminalOrdinal;
            public bool WaitingResumed;
            public double Mana, Heat;
        }
        // Opt-in development qualification only. Real Bootstrap/UI/native writer;
        // an injected technical clock accelerates the configured gameplay timings.
        internal IEnumerator QualifyForeground(string phase, ForegroundSmokeReport report, string previousReport)
        {
            if (!Application.productName.Contains("B3B2 Smoke") ||
                !Path.GetFullPath(SaveService.SavePath).StartsWith(Path.GetFullPath(Application.persistentDataPath) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(SaveService.SavePath).StartsWith("living-dungeon-b3b2-qualification-", StringComparison.Ordinal))
                throw new InvalidOperationException("b3b2.smoke.isolation_required");
            report.Product = Application.productName; report.SavePath = SaveService.SavePath;
            var advance = QualificationAdvance();
            var preparation = PrepareForegroundQualification();
            while (preparation.MoveNext()) yield return preparation.Current;
            ForegroundSmokeReport prior = null;
            string PendingGeometry()
            {
                var profile = SaveService.ForegroundConfiguration.CreateProfile(SaveSpatialMigrationLimits.Canonical.ForSchema(13).Serialized);
                var execution = LivingDungeonContracts.Restore(Save.livingDungeon.Parties.Single(), profile);
                try { return execution.Snapshot.Floors[0].KnowledgeFingerprint; }
                finally { execution.Abandon(); }
            }
            if (phase == "admit") {
                if (Save.livingDungeon.RetainedCount != 0 || !string.IsNullOrEmpty(Save.runHistory.LatestOutcome?.RunId))
                    throw new InvalidOperationException("b3b2.smoke.fresh_save_required");
                if (!TrySetQaManaFromDevPanel(true)) throw new InvalidOperationException("b3b2.smoke.funding");
                if ((Save.validatedCanonicalSpatialState.Floors.Length == 0 || Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms.Length == 0) &&
                    !TryMvpPlaceOrModifySelectedPlacement(MvpDungeonPlacementIds.RoomCategoryId,
                        MvpDungeonPlacementIds.BasicRoomOptionId, out _, out _, out _)) throw new InvalidOperationException("b3b2.smoke.room");
                foreach (var item in new[] { (MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId),
                    (MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId) }) {
                    if (Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments.Any(a => a.CategoryId == item.Item1)) continue;
                    _selectedRoomLocalContentPosition = item.Item1 == MvpDungeonPlacementIds.MonsterCategoryId
                        ? new TileCoordinate(1, 1) : new TileCoordinate(0, 0); // explicit nonoverlapping test positions
                    if (!TryMvpPlaceOrModifySelectedPlacement(item.Item1, item.Item2, out _, out _, out _))
                        throw new InvalidOperationException("b3b2.smoke.content");
                }
                OnApplicationFocus(true); ApplyPauseState(false);
                long before = TimeService.SampleForeground();
                yield return new WaitForSecondsRealtime(1.2f);
                report.RealSourceUnits = TimeService.SampleForeground() - before;
                if (report.RealSourceUnits < SaveService.ForegroundConfiguration.UnitsPerSecond || !FlushForeground())
                    throw new InvalidOperationException("b3b2.smoke.real_monotonic_source");
            } else {
                prior = JsonUtility.FromJson<ForegroundSmokeReport>(File.ReadAllText(previousReport));
                if (!prior.Passed || Save.livingDungeon.Parties.Count != 1 || !LivingDungeonForegroundConfig.IsProduction(Save.livingDungeon.Scheduler.ProfileId) ||
                    Save.livingDungeon.Scheduler.AcceptedThrough != prior.Accepted ||
                    Save.livingDungeon.Parties[0].CheckpointIdentity != prior.CheckpointIdentity || Save.livingDungeon.Parties[0].NextOrdinal != prior.Ordinal)
                    throw new InvalidOperationException("b3b2.smoke.exact_pending_reopen");
            }
            foregroundApplicationFocused = true; foregroundApplicationPaused = false; foregroundDirty = true;
            string initial = Save.livingDungeon.Parties.Count == 1 ? "run-" + Save.livingDungeon.Parties[0].Sequence : null;
            bool completed = false;
            var controller = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();
            for (int frame = 0; frame < 1600; frame++) {
                foregroundApplicationFocused = true; foregroundApplicationPaused = false;
                TimeService.SetForegroundFocus(true); TimeService.OnResume(); advance(.25); UpdateForeground();
                if (foregroundFailure != null) throw new InvalidOperationException(foregroundFailure);
                yield return null;
                if (controller?.World?.ForegroundPartyVisible == true) report.MarkerVisible = true;
                if (phase == "admit" && Save.livingDungeon.Parties.Count == 1 && report.MarkerVisible) {
                    if (Environment.GetCommandLineArgs().Contains("-b3b2MarkerVisuals")) {
                        var visual = QualifyForegroundMarkerVisuals(controller, report, seconds => {
                            advance(seconds); TimeService.SetForegroundFocus(true); UpdateForeground();
                        });
                        while (visual.MoveNext()) yield return visual.Current;
                    }
                    var p = Save.livingDungeon.Parties.Single(); report.RunId = "run-" + p.Sequence;
                    controller.EnterEdit(); yield return null;
                    if (controller.World.ForegroundPartyVisible) throw new InvalidOperationException("b3b2.smoke.edit_marker");
                    var monster = Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments.First(a => a.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId);
                    var before = (byte[])p.Checkpoint.Clone();
                    if (!controller.Draft.Move(Save.validatedCanonicalSpatialState.Floors[0].FloorInstanceId, monster.RoomInstanceId,
                        monster.AssignmentId, new TileCoordinate(3, 2))) throw new InvalidOperationException("b3b2.smoke.edit_draft");
                    while (controller.Draft.Durability == DraftDurability.Pending) {
                        if (!controller.Draft.FlushNext()) throw new InvalidOperationException("b3b2.smoke.draft_flush");
                        yield return null;
                    }
                    if (!controller.Commit()) throw new InvalidOperationException("b3b2.smoke.edit_commit");
                    if (!Save.livingDungeon.Parties.Single().Checkpoint.SequenceEqual(before)) throw new InvalidOperationException("b3b2.smoke.snapshot_changed");
                    report.ActiveEditCommitted = true; completed = true; break;
                }
                if (phase != "admit" && Save.runHistory.LatestOutcome?.RunId == initial) {
                    if (Save.runHistory.RecentOutcomes.Count(o => o.RunId == initial) != 1) throw new InvalidOperationException("b3b2.smoke.duplicate_history");
                    if (phase == "finish" || Save.livingDungeon.Parties.Count == 1 && "run-" + Save.livingDungeon.Parties[0].Sequence != initial) {
                        if (phase == "settle") {
                            string captured = PendingGeometry();
                            if (!FloorKnowledgeApplicability.TryCompute(Save.validatedCanonicalSpatialState, Save.corridorContent,
                                SaveSpatialMigrationLimits.Canonical, Save.validatedCanonicalSpatialState.Floors[0].FloorInstanceId, out string current) ||
                                captured != current || captured == prior.PendingGeometry)
                                throw new InvalidOperationException("b3b2.smoke.second_run_geometry");
                            report.SecondRunUsesEditedGeometry = true;
                        }
                        report.RunId = initial; completed = true; break;
                    }
                }
            }
            if (!completed) throw new InvalidOperationException("b3b2.smoke.timeout");
            ApplyPauseState(true);
            if (foregroundFailure != null || !Save.livingDungeon.IsClosed) throw new InvalidOperationException("b3b2.smoke.final_flush");
            report.Accepted = Save.livingDungeon.Scheduler.AcceptedThrough; report.Closed = Save.livingDungeon.Scheduler.ClosedThrough;
            report.HistoryCount = Save.runHistory.RecentOutcomes.Length;
            if (Save.livingDungeon.Parties.Count == 1) {
                report.CheckpointIdentity = Save.livingDungeon.Parties[0].CheckpointIdentity; report.Ordinal = Save.livingDungeon.Parties[0].NextOrdinal;
                report.PendingGeometry = PendingGeometry();
            }
            report.Passed = true;
        }

        // Explicit isolated Development smoke only; observes the real admitted
        // party and its production clock/renderer, never writes display fixtures.
        private IEnumerator QualifyForegroundMarkerVisuals(ProductionDungeonController controller, ForegroundSmokeReport report, Action<double> advance)
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-b3b2ForegroundSmoke");
            string reportPath = Path.GetFullPath(args[index + 2]);
            string directory = Path.Combine(Path.GetDirectoryName(reportPath), Path.GetFileNameWithoutExtension(reportPath) + "-visuals");
            if (Directory.Exists(directory)) throw new InvalidOperationException("b3b2.visual.preserve_capture_directory");
            Directory.CreateDirectory(directory);
            var camera = (Camera)typeof(ProductionDungeonController).GetField("worldCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(controller);
            var document = controller.GetComponent<UIDocument>(); var ui = document.rootVisualElement;
            var start = controller.World.ForegroundPartyPosition; bool moving = false;
            for (int boundary = 0; boundary < 80; boundary++) {
                advance(.25); yield return null;
                if (controller.World.ForegroundPartyVisible && controller.World.ForegroundPartyPosition != start &&
                    ForegroundActivity?.PositionStep?.Kind == Gameplay.RunSimulation.RunStepKind.Movement) { moving = true; break; }
            }
            if (!moving) throw new InvalidOperationException("b3b2.visual.movement_missing");
            controller.SetTextSize(DungeonTextSize.Large);
            byte[] canonical;
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(720, 1280), new Vector2Int(360, 640) }) {
                Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
                for (int frame = 0; frame < 12; frame++) { TimeService.SetForegroundFocus(true); yield return null; }
                if (Screen.width != size.x || Screen.height != size.y) {
                    // Windowed players cannot exceed the physical monitor's
                    // height. Record actual coverage; Editor Game View tests
                    // separately qualify the exact requested portrait layout.
                    if (Screen.width != size.x || size.y <= Screen.currentResolution.height || Screen.height != Screen.currentResolution.height)
                        throw new InvalidOperationException("b3b2.visual.unexpected_resolution expected=" + size + " actual=" + Screen.width + "x" + Screen.height);
                    report.VisualRequestedLayoutsUnavailable++;
                }
                if (!controller.World.ForegroundPartyVisible)
                    throw new InvalidOperationException("b3b2.visual.layout_or_marker expected=" + size + " actual=" + Screen.width + "x" + Screen.height + " visible=" + controller.World.ForegroundPartyVisible);
                canonical = SaveService.CanonicalSession.GetCurrentBytes();
                var sprite = (SpriteRenderer)typeof(DungeonFloorWorldView).GetField("foregroundParty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(controller.World);
                var min = camera.WorldToScreenPoint(sprite.bounds.min); var max = camera.WorldToScreenPoint(sprite.bounds.max);
                float scale = document.panelSettings.scale;
                var icon = Rect.MinMaxRect(Mathf.Min(min.x, max.x) / scale, (Screen.height - Mathf.Max(min.y, max.y)) / scale,
                    Mathf.Max(min.x, max.x) / scale, (Screen.height - Mathf.Min(min.y, max.y)) / scale);
                var label = ui.Q<Label>("foregroundPartyLabel"); var map = ui.Q("mapArea");
                float gap = controller.presentationPolicy.ReplayActorLabelOffset - .5f;
                var clear = Rect.MinMaxRect(icon.xMin - gap, icon.yMin - gap, icon.xMax + gap, icon.yMax + gap);
                if (label.resolvedStyle.display != DisplayStyle.None) {
                    if (label.worldBound.Overlaps(clear) || label.worldBound.Overlaps(ui.Q("floorRail").worldBound) ||
                        label.worldBound.xMin < map.worldBound.xMin - .5f || label.worldBound.yMin < map.worldBound.yMin - .5f ||
                        label.worldBound.xMax > map.worldBound.xMax + .5f || label.worldBound.yMax > map.worldBound.yMax + .5f)
                        throw new InvalidOperationException("b3b2.visual.label_overlap");
                    report.VisualLabelsShown++;
                } else report.VisualLabelsHidden++;
                report.VisualBounds += "requested=" + size + " actual=" + Screen.width + "x" + Screen.height + " icon=" + icon + " label=" + label.worldBound + " display=" + label.resolvedStyle.display + " position=" + controller.World.ForegroundPartyPosition + "\n";
                if (string.IsNullOrEmpty(ui.Q<Label>("foregroundActivity").text) || !canonical.SequenceEqual(SaveService.CanonicalSession.GetCurrentBytes()))
                    throw new InvalidOperationException("b3b2.visual.canonical_or_identification");
                string path = Path.Combine(directory, "native-marker-moving-large-" + Screen.width + "x" + Screen.height + ".png");
                if (File.Exists(path)) throw new InvalidOperationException("b3b2.visual.preserve_capture");
                ScreenCapture.CaptureScreenshot(path);
                for (int frame = 0; frame < 60 && !File.Exists(path); frame++) yield return null;
                if (!File.Exists(path)) throw new InvalidOperationException("b3b2.visual.capture_missing");
                if (!canonical.SequenceEqual(SaveService.CanonicalSession.GetCurrentBytes()))
                    throw new InvalidOperationException("b3b2.visual.presentation_published_gameplay");
                report.VisualChecks++;
                // A real advancing clock fraction, still before the next
                // configured movement boundary. No display-generated events.
                advance(.1); yield return null;
                canonical = SaveService.CanonicalSession.GetCurrentBytes();
            }
            controller.SetTextSize(DungeonTextSize.Default);
        }
    }
    // Opt-in development qualification input only. It is constructed before
    // the sole TimeService binds recovery, never a replacement time authority.
    internal sealed class PreboundForegroundQualificationClock
    {
        private Func<double> realSource;
        private double sample;
        internal bool IsRealTime => realSource != null;
        internal PreboundForegroundQualificationClock(bool realTime)
        {
            if (realTime) {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                realSource = () => watch.Elapsed.TotalSeconds;
            }
        }
        internal PreboundForegroundQualificationClock(Func<double> realSource)
        { this.realSource = realSource ?? throw new ArgumentNullException(nameof(realSource)); }
        internal double Read() => realSource == null ? sample : realSource();
        internal void Advance(double seconds)
        {
            double before = Read(), next = before + seconds;
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 ||
                double.IsNaN(before) || double.IsInfinity(before) || before < 0 ||
                double.IsNaN(next) || double.IsInfinity(next) || next < before)
                throw new InvalidOperationException("living.foreground.clock_invalid");
            // Freeze a real source at its current value, then add the requested
            // suffix. No reset or lost pre-handoff monotonic time.
            sample = next; realSource = null;
        }
    }
    internal sealed class LivingDungeonForegroundDevelopmentSmoke : MonoBehaviour
    {
        internal static Func<double> ReopenClock(string[] args, string product, string savePath)
        {
            int index = Array.IndexOf(args, "-b3b2ForegroundSmoke");
            if (index < 0 || index + 1 >= args.Length || args[index + 1] != "admit" && args[index + 1] != "settle" && args[index + 1] != "finish" && args[index + 1] != "profile" &&
                !args[index + 1].StartsWith("two-floor-", StringComparison.Ordinal) && args[index + 1] != "v1-upgrade" ||
                product == null || !product.Contains("B3B2 Smoke") || string.IsNullOrEmpty(savePath) ||
                !Path.GetFileName(savePath).StartsWith("living-dungeon-b3b2-qualification-", StringComparison.Ordinal)) return null;
            // One instance exists before recovery binding. Admission starts on
            // a real monotonic source; reopen/profile start stationary. Later
            // acceleration advances this provider without replacing TimeService.
            return new PreboundForegroundQualificationClock(args[index + 1] == "admit").Read;
        }
        private string phase, destination, previous;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Begin()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-b3b2ForegroundSmoke");
            if (index < 0) return;
            if (index + 3 >= args.Length) { Application.Quit(2); return; }
            var probe = new GameObject("B3B2ForegroundQualification").AddComponent<LivingDungeonForegroundDevelopmentSmoke>();
            DontDestroyOnLoad(probe.gameObject); probe.phase = args[index + 1]; probe.destination = Path.GetFullPath(args[index + 2]); probe.previous = args[index + 3];
            if (probe.phase.StartsWith("two-floor-", StringComparison.Ordinal) || probe.phase == "v1-upgrade") {
                var root = GameRoot.Instance;
                if (root == null || !Application.productName.Contains("B3B2 Smoke Two Floor")) { Application.Quit(2); return; }
                string file;
                if (probe.phase == "two-floor-wait" || probe.phase == "two-floor-profile" || probe.phase == "v1-upgrade")
                    file = JsonUtility.FromJson<TwoFloorSeedManifest>(File.ReadAllText(probe.previous)).SaveFileName;
                else file = Path.GetFileName(JsonUtility.FromJson<GameRoot.ForegroundSmokeReport>(File.ReadAllText(probe.previous)).SavePath);
                if (file != Path.GetFileName(file) || !file.StartsWith("living-dungeon-b3b2-qualification-two-floor-", StringComparison.Ordinal) ||
                    !File.Exists(Path.Combine(Application.persistentDataPath, file))) { Application.Quit(2); return; }
                var config = JsonUtility.FromJson<BuildConfig>(root.buildConfigJson.text);
                config.save.fileName = file; root.buildConfigJson = new TextAsset(JsonUtility.ToJson(config));
            }
            if (probe.phase == "profile") {
                var root = GameRoot.Instance;
                if (root == null || !Application.productName.Contains("B3B2 Smoke")) { Application.Quit(2); return; }
                var config = JsonUtility.FromJson<BuildConfig>(root.buildConfigJson.text);
                if (!config.save.fileName.StartsWith("living-dungeon-b3b2-qualification-", StringComparison.Ordinal)) { Application.Quit(2); return; }
                config.save.fileName = Path.GetFileNameWithoutExtension(config.save.fileName) + "-" + Path.GetFileNameWithoutExtension(probe.destination) + ".json";
                if (File.Exists(Path.Combine(Application.persistentDataPath, config.save.fileName))) { Application.Quit(2); return; }
                root.buildConfigJson = new TextAsset(JsonUtility.ToJson(config));
            }
        }
        private IEnumerator Start()
        {
            if (File.Exists(destination) || !Directory.Exists(Path.GetDirectoryName(destination)) ||
                phase != "admit" && phase != "settle" && phase != "finish" && phase != "profile" &&
                phase != "two-floor-wait" && phase != "two-floor-release" && phase != "two-floor-finish" && phase != "two-floor-profile" && phase != "v1-upgrade") { Application.Quit(2); yield break; }
            for (int frame = 0; frame < 600 && GameRoot.Instance?.TimeService?.HasForegroundClock != true; frame++) yield return null;
            var report = new GameRoot.ForegroundSmokeReport { Phase = phase };
            var profile = phase == "profile" || phase == "two-floor-profile" ? new GameRoot.ForegroundProfileReport() : null;
            IEnumerator test = profile != null ? GameRoot.Instance?.ProfileForeground(profile) :
                phase.StartsWith("two-floor-", StringComparison.Ordinal) || phase == "v1-upgrade"
                    ? GameRoot.Instance?.QualifyTwoFloor(phase, report, previous) : GameRoot.Instance?.QualifyForeground(phase, report, previous);
            while (test != null) {
                object next = null; bool more = false;
                try { more = test.MoveNext(); if (more) next = test.Current; }
                catch (Exception error) { report.Failure = error.ToString(); if (profile != null) profile.Failure = error.ToString(); Debug.LogException(error); break; }
                if (!more) break;
                yield return next;
            }
            if (test == null) report.Failure = "b3b2.smoke.boot_failed";
            using (var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream)) writer.Write(profile != null ? JsonUtility.ToJson(profile, true) : JsonUtility.ToJson(report, true));
            if (GameRoot.Instance?.SaveService?.CanonicalSession != null)
                using (var stream = new FileStream(destination + ".save.json", FileMode.CreateNew, FileAccess.Write)) {
                    var bytes = GameRoot.Instance.SaveService.CanonicalSession.GetCurrentBytes();
                    stream.Write(bytes, 0, bytes.Length);
                }
            Application.Quit((profile?.Passed ?? report.Passed) ? 0 : 1); // normal lifecycle saves finish synchronously
        }
    }
}
#endif
