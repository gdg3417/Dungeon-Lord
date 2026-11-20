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
#if DUNGEON_LORD_H2_QUALIFICATION && DEVELOPMENT_BUILD
            public string ResearchRule, ResearchUnlock, ResearchProject, ResearchSlot, RecoveryContext;
            public long ResearchStartCut, ResearchEarned;
            public double ResearchWalletBefore;
#endif
        }
#if DUNGEON_LORD_H2_QUALIFICATION && DEVELOPMENT_BUILD
        internal IEnumerator QualifyStarterResearch(string phase, ForegroundSmokeReport report, string destination, string previous)
        {
            if (!LivingDungeonForegroundDevelopmentSmoke.IsResearchQualification(phase, Application.productName, SaveService.SavePath))
                throw new InvalidOperationException("research.qualification.isolation_required");
            var prepare = PrepareForegroundQualification();
            while (prepare.MoveNext()) yield return prepare.Current;
            var controller = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var advance = QualificationAdvance();
            int unitsPerSecond = SaveService.ForegroundConfiguration.UnitsPerSecond;
            long tick = Content.Bootstrap.tickSeconds * (long)unitsPerSecond;
            var click = typeof(Clickable).GetMethod("SimulateSingleClick", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            void Click(string name) => click.Invoke(ui.Q<Button>(name).clickable, new object[] { null, 0 });
            void Check(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
            void Reach(long horizon)
            {
                var elapsed = horizon - TimeService.SampleForeground();
                Check(elapsed >= 0, "research.qualification.non_monotonic");
                advance((double)elapsed / unitsPerSecond);
                for (int attempt = 0; attempt < 256; attempt++)
                {
                    UpdateForeground();
                    if (foregroundFailure != null) throw new InvalidOperationException(foregroundFailure);
                    if (Save.livingDungeon.IsClosed && Save.livingDungeon.Scheduler.ClosedThrough >= horizon) return;
                    FlushForeground();
                }
                throw new InvalidOperationException("research.qualification.horizon");
            }
            bool ActionShown(string name) => ui.Q<Button>(name).style.display.value == DisplayStyle.Flex;
            void CaptureScenario(string suffix)
            {
                var candidate = SaveService.CanonicalSession.GetCurrentBytes();
                Check(candidate.SequenceEqual(File.ReadAllBytes(report.SavePath)), "research.qualification.scenario_readback");
                using var file = new FileStream(destination + suffix + ".save.json", FileMode.CreateNew, FileAccess.Write);
                file.Write(candidate, 0, candidate.Length);
            }

            report.Product = Application.productName;
            report.SavePath = SaveService.SavePath;
            report.RecoveryContext = Save.timeProvenance.RecoveryContextSha256;
            Check(tick == 10L * unitsPerSecond, "research.qualification.tick");
            if (phase == "research-start")
            {
                Click("openResearch");
                Check(controller.IsResearch && !ActionShown("researchStart") && !HasValidAdventurerRun(),
                    "research.qualification.initial_prerequisite");
                Check(ui.Q<Label>("researchCost").text.Contains("10") &&
                    ui.Q<Label>("researchDuration").text.Contains("120"), "research.qualification.authored_terms");
                Click("researchDungeon");
                Check(TrySetQaManaFromDevPanel(true), "research.qualification.funding");
                if (Save.validatedCanonicalSpatialState.Floors.Length == 0)
                    Check(TryMvpPlaceOrModifySelectedPlacement(MvpDungeonPlacementIds.RoomCategoryId,
                        MvpDungeonPlacementIds.BasicRoomOptionId, out _, out _, out _), "research.qualification.room");
                _selectedRoomLocalContentPosition = new TileCoordinate(1, 1);
                Check(TryMvpPlaceOrModifySelectedPlacement(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId, out _, out _, out _), "research.qualification.skeleton");
                _selectedRoomLocalContentPosition = new TileCoordinate(0, 0);
                Check(TryMvpPlaceOrModifySelectedPlacement(MvpDungeonPlacementIds.LootNodeCategoryId,
                    MvpDungeonPlacementIds.BasicLootNodeOptionId, out _, out _, out _), "research.qualification.loot");
                CaptureScenario(".waiting-for-run");
                for (int i = 0; i < 80 && !HasValidAdventurerRun(); i++)
                    Reach(TimeService.SampleForeground() + tick);
                Check(HasValidAdventurerRun(), "research.qualification.genuine_settlement");
                report.RunId = Save.runHistory.LatestOutcome.RunId;
                Check(TrySetQaManaFromDevPanel(false), "research.qualification.clear_wallet");
                CaptureScenario(".unaffordable");
                Click("openResearch");
                Click("researchStart");
                Check(!ActionShown("researchStart") && Save.researchPending == null &&
                    ResolvePlayerResearchState().Authority?.IsAffordable == false, "research.qualification.unaffordable");
                Click("researchDungeon");
                long now = TimeService.SampleForeground();
                long nextCut = now / tick * tick + 7L * unitsPerSecond;
                if (nextCut < now) nextCut += tick;
                Reach(nextCut);
                Check(TrySetQaManaFromDevPanel(true), "research.qualification.funding_after_run");
                CaptureScenario(".available");
                SetOnline(false);
                Click("openResearch");
                Check(ActionShown("researchStart"), "research.qualification.available");
                report.ResearchWalletBefore = Save.structureRuntime.ManaReserve;
                Click("researchStart");
                Click("researchStart");
                Check(Save.structureRuntime.ManaReserve == report.ResearchWalletBefore - 10 && Save.researchProgress != null,
                    "research.qualification.single_debit");
                report.ResearchRule = Save.researchProgress.RuleSourceIdUsed;
                report.ResearchProject = Save.researchProgress.ProjectId;
                report.ResearchSlot = Save.researchProgress.SlotId;
                report.ResearchStartCut = Save.researchProgress.StartClosedThrough;
                Check(report.ResearchStartCut == nextCut, "research.qualification.start_cut");
                Reach(report.ResearchStartCut + 3L * unitsPerSecond);
                report.ResearchEarned = (long)Save.researchProgress.ProgressUnits;
                Check(report.ResearchEarned == 3L * unitsPerSecond && !ActionShown("researchClaim"),
                    "research.qualification.partial");
            }
            else
            {
                var prior = JsonUtility.FromJson<ForegroundSmokeReport>(File.ReadAllText(previous));
                Check(prior.Passed && prior.Product == report.Product && prior.SavePath == report.SavePath &&
                    prior.RecoveryContext == report.RecoveryContext &&
                    Save.runHistory.RecentOutcomes.Any(run => run.RunId == prior.RunId), "research.qualification.reopen_identity");
                report.RunId = prior.RunId;
                report.ResearchRule = prior.ResearchRule;
                report.ResearchProject = prior.ResearchProject;
                report.ResearchSlot = prior.ResearchSlot;
                report.ResearchStartCut = prior.ResearchStartCut;
                Check(Save.structureRuntime.ManaReserve >= prior.Mana, "research.qualification.no_reopen_debit");
                SetOnline(false);
                Click("openResearch");
                Check(controller.IsResearch, "research.qualification.reopen_destination");
                if (phase == "research-claim")
                {
                    Check(Save.researchProgress?.RuleSourceIdUsed == prior.ResearchRule &&
                        Save.researchProgress.StartClosedThrough == prior.ResearchStartCut &&
                        Save.researchProgress.ProgressUnits == prior.ResearchEarned && !ActionShown("researchClaim"),
                        "research.qualification.reopen_progress");
                    long start = Save.researchProgress.StartClosedThrough;
                    Reach(start + 113L * unitsPerSecond);
                    Check(Save.researchProgress.ProgressUnits == 113L * unitsPerSecond && !ActionShown("researchClaim") &&
                        !ResolveMvpPlayerLoopSummary().HasResearchUnlock, "research.qualification.before_ready");
                    Reach(start + 123L * unitsPerSecond);
                    Check(ActionShown("researchClaim") && Save.researchProgress.ProgressUnits == 120L * unitsPerSecond,
                        "research.qualification.ready");
                    report.ResearchEarned = (long)Save.researchProgress.ProgressUnits;
                    report.ResearchWalletBefore = Save.structureRuntime.ManaReserve;
                    Click("researchClaim");
                    Click("researchClaim");
                    Check(Save.structureRuntime.ManaReserve == report.ResearchWalletBefore, "research.qualification.claim_wallet");
                }
                if (phase == "research-finished")
                {
                    report.ResearchWalletBefore = Save.structureRuntime.ManaReserve;
                    Click("researchClaim");
                    Click("researchStart");
                    Check(Save.structureRuntime.ManaReserve == report.ResearchWalletBefore, "research.qualification.no_repeat_debit");
                }
                var loop = ResolveMvpPlayerLoopSummary();
                Check(Save.researchPending == null && Save.researchProgress == null &&
                    Save.completedResearch.ProjectIds.Count(id => id == report.ResearchProject) == 1 &&
                    loop.HasResearchUnlock && !ActionShown("researchStart") && !ActionShown("researchClaim"),
                    "research.qualification.durable_unlock");
                report.ResearchUnlock = loop.ResearchUnlockId;
            }
            report.Mana = Save.structureRuntime.ManaReserve;
            report.Accepted = Save.livingDungeon.Scheduler.AcceptedThrough;
            report.Closed = Save.livingDungeon.Scheduler.ClosedThrough;
            Check(SaveService.CanonicalSession.GetCurrentBytes().SequenceEqual(File.ReadAllBytes(report.SavePath)),
                "research.qualification.disk_readback");
            report.Passed = true;
        }
#endif
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
#if DUNGEON_LORD_H2_QUALIFICATION && DEVELOPMENT_BUILD
        private static bool IsResearchPhase(string phase) =>
            phase == "research-start" || phase == "research-claim" || phase == "research-finished";

        private static bool IsResearchProduct(string product) => product != null &&
            System.Text.RegularExpressions.Regex.IsMatch(product, @"\ADL B3B2 Smoke Two Floor Q[0-9a-f]{8}\z");

        internal static bool IsResearchQualification(string phase, string product, string savePath) =>
            IsResearchPhase(phase) && IsResearchProduct(product) && savePath != null &&
            System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(savePath),
                @"\Aliving-dungeon-b3b2-qualification-research-q[1-9][0-9]*\.json\z") &&
            string.Equals(Path.GetDirectoryName(Path.GetFullPath(savePath)), Path.GetFullPath(Application.persistentDataPath),
                StringComparison.OrdinalIgnoreCase);

        private static bool ConfigureResearchOwnerScenario(string[] args)
        {
            int index = Array.IndexOf(args, "-researchR1OwnerScenario");
            if (index < 0) return false;
            var root = GameRoot.Instance;
            string scenario = index + 1 < args.Length ? args[index + 1] : null;
            if (root == null || !IsResearchProduct(Application.productName) ||
                (scenario != "waiting-for-run" && scenario != "unaffordable" && scenario != "available"))
            {
                Application.Quit(2);
                return true;
            }
            var config = JsonUtility.FromJson<BuildConfig>(root.buildConfigJson.text);
            if (!IsResearchQualification("research-start", Application.productName,
                Path.Combine(Application.persistentDataPath, config.save.fileName)))
            {
                Application.Quit(2);
                return true;
            }
            string file = "research-r1-owner-" + scenario + ".json";
            string path = Path.Combine(Application.persistentDataPath, file);
            if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            {
                Application.Quit(2);
                return true;
            }
            config.save.fileName = file;
            root.buildConfigJson = new TextAsset(JsonUtility.ToJson(config));
            return true;
        }
#endif
        internal static Func<double> ReopenClock(string[] args, string product, string savePath)
        {
            int index = Array.IndexOf(args, "-b3b2ForegroundSmoke");
#if DUNGEON_LORD_H2_QUALIFICATION && DEVELOPMENT_BUILD
            if (index >= 0 && index + 1 < args.Length && H2PlayerQualification.IsPhase(args[index + 1]))
                return H2PlayerQualification.UseStationaryClock ? new PreboundForegroundQualificationClock(false).Read : null;
            if (index >= 0 && index + 3 < args.Length && IsResearchQualification(args[index + 1], product, savePath))
                return new PreboundForegroundQualificationClock(false).Read;
#endif
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
            var args = Environment.GetCommandLineArgs();
#if DUNGEON_LORD_H2_QUALIFICATION && DEVELOPMENT_BUILD
            if (ConfigureResearchOwnerScenario(args)) return;
#endif
            int index = Array.IndexOf(args, "-b3b2ForegroundSmoke");
            if (index < 0) return;
            if (index + 3 >= args.Length) { Application.Quit(2); return; }
            var probe = new GameObject("B3B2ForegroundQualification").AddComponent<LivingDungeonForegroundDevelopmentSmoke>();
            DontDestroyOnLoad(probe.gameObject); probe.phase = args[index + 1]; probe.destination = Path.GetFullPath(args[index + 2]); probe.previous = args[index + 3];
#if DUNGEON_LORD_H2_QUALIFICATION && DEVELOPMENT_BUILD
            if (H2PlayerQualification.IsPhase(probe.phase)) {
                try { H2PlayerQualification.Configure(GameRoot.Instance, probe.phase, probe.destination, probe.previous); }
                catch (Exception error) { Debug.LogException(error); Application.Quit(2); }
                return;
            }
#endif
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
#if DUNGEON_LORD_H2_QUALIFICATION && DEVELOPMENT_BUILD
            if (H2PlayerQualification.IsPhase(phase)) {
                yield return H2PlayerQualification.Run(GameRoot.Instance);
                yield break;
            }
#endif
            bool research = false;
#if DUNGEON_LORD_H2_QUALIFICATION && DEVELOPMENT_BUILD
            research = IsResearchPhase(phase);
#endif
            if (File.Exists(destination) || !Directory.Exists(Path.GetDirectoryName(destination)) || !research &&
                phase != "admit" && phase != "settle" && phase != "finish" && phase != "profile" &&
                phase != "two-floor-wait" && phase != "two-floor-release" && phase != "two-floor-finish" && phase != "two-floor-profile" && phase != "v1-upgrade") { Application.Quit(2); yield break; }
            for (int frame = 0; frame < 600 && GameRoot.Instance?.TimeService?.HasForegroundClock != true; frame++) yield return null;
            var report = new GameRoot.ForegroundSmokeReport { Phase = phase };
            var profile = phase == "profile" || phase == "two-floor-profile" ? new GameRoot.ForegroundProfileReport() : null;
            IEnumerator test = profile != null ? GameRoot.Instance?.ProfileForeground(profile) :
                phase.StartsWith("two-floor-", StringComparison.Ordinal) || phase == "v1-upgrade"
                    ? GameRoot.Instance?.QualifyTwoFloor(phase, report, previous) : GameRoot.Instance?.QualifyForeground(phase, report, previous);
#if DUNGEON_LORD_H2_QUALIFICATION && DEVELOPMENT_BUILD
            if (research) test = GameRoot.Instance?.QualifyStarterResearch(phase, report, destination, previous);
#endif
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
