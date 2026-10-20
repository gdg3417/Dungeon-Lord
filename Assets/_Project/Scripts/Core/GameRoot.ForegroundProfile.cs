#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;

namespace DungeonBuilder.M0
{
    public partial class GameRoot
    {
        [Serializable] internal sealed class ForegroundProfileFrame
        {
            public int Frame, Sequence, Collections;
            public string Category;
            public long Ordinal, Allocated, ManagedBytes, NativeBytes, MainThreadNanoseconds, FrameAllocatedBytes;
            public double UpdateMs, FrameMs;
            public long[] Calls, Ticks, Bytes;
        }
        [Serializable] internal sealed class ForegroundProfileReport
        {
            public bool Passed, ThreadAllocationCounterAvailable, FrameAllocationCounterAvailable;
            public string Failure, Product, SavePath;
            public int CompletedRuns;
            public long StopwatchFrequency;
            public string[] Areas;
            public List<ForegroundProfileFrame> Frames = new List<ForegroundProfileFrame>();
        }
        internal IEnumerator ProfileForeground(ForegroundProfileReport report)
        {
            if (!Application.productName.Contains("B3B2 Smoke") ||
                !(System.IO.Path.GetFileName(SaveService.SavePath).StartsWith("living-dungeon-b3b2-qualification-", StringComparison.Ordinal) ||
                  Application.isEditor && System.IO.Path.GetFileName(SaveService.SavePath).StartsWith("phase7a4-scene-", StringComparison.Ordinal)))
                throw new InvalidOperationException("b3b2.profile.isolation_required");
            report.Product = Application.productName; report.SavePath = SaveService.SavePath;
            report.StopwatchFrequency = Stopwatch.Frequency;
            report.Areas = Enum.GetNames(typeof(ForegroundQualificationMetrics.Area));
            double sample = 1000;
            TimeService = new TimeService(Logger, _configuredSimulationTickSeconds,
                Content.Bootstrap.timeRules.detectClockSkewSeconds, new SystemTimeSource(), () => sample);
            TimeService.AttachSave(Save); TimeService.SetForegroundFocus(true);
            TimeService.BeginForeground(SaveService.ForegroundConfiguration.UnitsPerSecond, Save.livingDungeon.Scheduler?.AcceptedThrough ?? -1);
            foregroundApplicationFocused = true; foregroundApplicationPaused = false; foregroundDirty = true;
            using var mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            using var frameAllocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            long allocationBefore = GC.GetAllocatedBytesForCurrentThread();
            var calibration = new byte[1024];
            report.ThreadAllocationCounterAvailable = GC.GetAllocatedBytesForCurrentThread() > allocationBefore;
            GC.KeepAlive(calibration);
            report.FrameAllocationCounterAvailable = frameAllocations.Valid;
            int baselineSequence = Save.runHistory.NextRunSequence;
            int originalGc = GC.CollectionCount(0);
            ForegroundQualificationMetrics.Enabled = true;
            try {
                for (int frame = 0; frame < 40000; frame++) {
                    if (frame == 120) {
                        if (!TrySetQaManaFromDevPanel(true)) throw new InvalidOperationException("b3b2.profile.funding");
                        if ((Save.validatedCanonicalSpatialState.Floors.Length == 0 || Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms.Length == 0) &&
                            !TryMvpPlaceOrModifySelectedPlacement(MvpDungeonPlacementIds.RoomCategoryId,
                                MvpDungeonPlacementIds.BasicRoomOptionId, out _, out _, out _)) throw new InvalidOperationException("b3b2.profile.room");
                        foreach (var item in new[] { (MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId),
                            (MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId) }) {
                            if (Save.validatedCanonicalSpatialState.Floors[0].RoomContents.Assignments.Any(a => a.CategoryId == item.Item1)) continue;
                            _selectedRoomLocalContentPosition = item.Item1 == MvpDungeonPlacementIds.MonsterCategoryId ? new TileCoordinate(1, 1) : new TileCoordinate(0, 0);
                            if (!TryMvpPlaceOrModifySelectedPlacement(item.Item1, item.Item2, out _, out _, out _)) throw new InvalidOperationException("b3b2.profile.content");
                        }
                    }
                    foregroundApplicationFocused = true; foregroundApplicationPaused = false;
                    TimeService.SetForegroundFocus(true); TimeService.OnResume();
                    if (frame >= 120) sample += .25; // explicit qualification clock acceleration; configuration unchanged
                    int sequence = Save.runHistory.NextRunSequence;
                    long ordinal = Save.livingDungeon.Parties.FirstOrDefault()?.NextOrdinal ?? -1;
                    string latest = Save.runHistory.LatestOutcome?.RunId;
                    ForegroundQualificationMetrics.Reset();
                    long bytes = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
                    UpdateForeground();
                    double elapsed = (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;
                    long allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
                    if (foregroundFailure != null || foregroundRetryPending) throw new InvalidOperationException(foregroundFailure ?? "b3b2.profile.retry_pending");
                    yield return null; // include the real controller, map, layout and render frame
                    string category = Save.runHistory.LatestOutcome?.RunId != latest ? "settlement" :
                        Save.runHistory.NextRunSequence != sequence ? "admission" :
                        (Save.livingDungeon.Parties.FirstOrDefault()?.NextOrdinal ?? -1) != ordinal ? "event" :
                        ForegroundQualificationMetrics.Calls[(int)ForegroundQualificationMetrics.Area.AtomicWrite] > 0 ? "passive" : frame < 120 ? "idle-empty" : "between-events";
                    report.Frames.Add(new ForegroundProfileFrame { Frame = frame, Sequence = sequence, Ordinal = ordinal, Category = category,
                        UpdateMs = elapsed, FrameMs = Time.unscaledDeltaTime * 1000d, Allocated = allocated,
                        ManagedBytes = Profiler.GetMonoUsedSizeLong(), NativeBytes = Profiler.GetTotalAllocatedMemoryLong(),
                        MainThreadNanoseconds = mainThread.Valid ? mainThread.LastValue : -1,
                        FrameAllocatedBytes = frameAllocations.Valid ? frameAllocations.LastValue : -1,
                        Collections = GC.CollectionCount(0) - originalGc, Calls = (long[])ForegroundQualificationMetrics.Calls.Clone(),
                        Ticks = (long[])ForegroundQualificationMetrics.Ticks.Clone(), Bytes = (long[])ForegroundQualificationMetrics.Allocated.Clone() });
                    if (Save.runHistory.LatestOutcome?.RunId == "run-" + (baselineSequence + 67)) {
                        report.CompletedRuns = 68; report.Passed = true; break;
                    }
                }
                if (!report.Passed) throw new InvalidOperationException("b3b2.profile.incomplete");
            } finally { ForegroundQualificationMetrics.Enabled = false; ApplyPauseState(true); }
        }
    }
}
#endif
