#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public partial class GameRoot
    {
        [Serializable] internal sealed class ForegroundSmokeReport
        {
            public bool Passed, MarkerVisible, ActiveEditCommitted, SecondRunUsesEditedGeometry;
            public string Phase, Product, SavePath, RunId, CheckpointIdentity, PendingGeometry, Failure;
            public long Accepted, Closed, Ordinal, HistoryCount, RealSourceUnits;
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
                if (!prior.Passed || Save.livingDungeon.Parties.Count != 1 || Save.livingDungeon.Scheduler.ProfileId != LivingDungeonForegroundConfig.ProfileId ||
                    Save.livingDungeon.Scheduler.AcceptedThrough != prior.Accepted ||
                    Save.livingDungeon.Parties[0].CheckpointIdentity != prior.CheckpointIdentity || Save.livingDungeon.Parties[0].NextOrdinal != prior.Ordinal)
                    throw new InvalidOperationException("b3b2.smoke.exact_pending_reopen");
            }
            double sample = 1000; // test-only injected monotonic epoch
            TimeService = new TimeService(Logger, _configuredSimulationTickSeconds,
                Content.Bootstrap.timeRules.detectClockSkewSeconds, new SystemTimeSource(), () => sample);
            TimeService.AttachSave(Save); TimeService.SetForegroundFocus(true);
            TimeService.BeginForeground(SaveService.ForegroundConfiguration.UnitsPerSecond, Save.livingDungeon.Scheduler.AcceptedThrough);
            foregroundApplicationFocused = true; foregroundApplicationPaused = false; foregroundDirty = true;
            _resumeOfflineBoundaryPersisted = false;
            string initial = Save.livingDungeon.Parties.Count == 1 ? "run-" + Save.livingDungeon.Parties[0].Sequence : null;
            bool completed = false;
            var controller = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();
            for (int frame = 0; frame < 1600; frame++) {
                foregroundApplicationFocused = true; foregroundApplicationPaused = false;
                TimeService.SetForegroundFocus(true); TimeService.OnResume(); sample += .25; UpdateForeground();
                if (foregroundFailure != null) throw new InvalidOperationException(foregroundFailure);
                yield return null;
                if (controller?.World?.ForegroundPartyVisible == true) report.MarkerVisible = true;
                if (phase == "admit" && Save.livingDungeon.Parties.Count == 1 && report.MarkerVisible) {
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
                                SaveSpatialMigrationLimits.Canonical, Save.livingDungeon.Parties.Count == 1 ?
                                    Save.validatedCanonicalSpatialState.Floors[0].FloorInstanceId : null, out string current) ||
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
    }
    internal sealed class LivingDungeonForegroundDevelopmentSmoke : MonoBehaviour
    {
        private string phase, destination, previous;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Begin()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-b3b2ForegroundSmoke");
            if (index < 0) return;
            if (index + 3 >= args.Length) { Application.Quit(2); return; }
            var probe = new GameObject("B3B2ForegroundQualification").AddComponent<LivingDungeonForegroundDevelopmentSmoke>();
            DontDestroyOnLoad(probe.gameObject); probe.phase = args[index + 1]; probe.destination = Path.GetFullPath(args[index + 2]); probe.previous = args[index + 3];
        }
        private IEnumerator Start()
        {
            if (File.Exists(destination) || !Directory.Exists(Path.GetDirectoryName(destination)) ||
                phase != "admit" && phase != "settle" && phase != "finish") { Application.Quit(2); yield break; }
            for (int frame = 0; frame < 600 && GameRoot.Instance?.TimeService?.HasForegroundClock != true; frame++) yield return null;
            var report = new GameRoot.ForegroundSmokeReport { Phase = phase };
            IEnumerator test = GameRoot.Instance?.QualifyForeground(phase, report, previous);
            while (test != null) {
                object next = null; bool more = false;
                try { more = test.MoveNext(); if (more) next = test.Current; }
                catch (Exception error) { report.Failure = error.ToString(); Debug.LogException(error); break; }
                if (!more) break;
                yield return next;
            }
            if (test == null) report.Failure = "b3b2.smoke.boot_failed";
            using (var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream)) writer.Write(JsonUtility.ToJson(report, true));
            Application.Quit(report.Passed ? 0 : 1); // normal lifecycle saves finish synchronously
        }
    }
}
#endif
