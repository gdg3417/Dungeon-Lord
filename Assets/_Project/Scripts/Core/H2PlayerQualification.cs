#if DUNGEON_LORD_H2_QUALIFICATION && DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace DungeonBuilder.M0
{
    // Compiled only in the isolated Bootstrap qualification build. All saves and
    // progression still run through the regular coordinator and canonical writer.
    internal static class H2PlayerQualification
    {
        private const int WarmupFrames = 120, SampleCapacity = 4096, MinimumSamples = 120;
        private const double SamplingSeconds = 15;
        private static string phase, destination, savePath, nonce, loadedHash;
        private static long configuredAt;
        private static bool armed, diagnostic;
        private static Report report;

        [Serializable] private sealed class Manifest
        {
            public string Product, Nonce;
        }

        [Serializable] private sealed class Marker
        {
            public string Cut, Nonce, SavePath, Predecessor, Candidate;
            public int Pid, Ordinal;
            public long Created;
        }

        [Serializable] private struct Frame
        {
            public double Seconds;
            public long Accepted, Closed, Ordinal, Tick, NextSequence, AttemptedWrites, Publications;
            public long MainThreadNs, RenderThreadNs, AllocatedBytes, EligibleThrough;
            public int HistoryCount, Parties, CheckpointBytes;
            public bool Focused;
        }

        [Serializable] private struct MemorySample
        {
            public string Phase;
            public long ClockTicks, ManagedHeap, UnityAllocated, CheckedPayloadBytes;
        }

        [Serializable] private sealed class Report
        {
            public bool Passed, ManagedCalibrated, NativeCalibrated;
            public string Mode, Product, LoadedSha256, Profile, Device, OS, Graphics, Unity, Failure;
            public int Width, Height, VSync, TargetFrameRate, Warmup;
            public double BootSeconds, RefreshRate;
            public long ClockFrequency, ManagedCalibrationDelta, AllocationCalibrationDelta, NativeCalibrationDelta;
            public long WorkingSet, PrivateBytes;
            public string[] Areas;
            public long[] Calls, Ticks, Allocations;
            public Frame[] Frames = new Frame[SampleCapacity];
            public MemorySample[] Memory;
            public int FrameCount, MemoryCount, DroppedMemory;
        }

        internal static bool IsPhase(string value) => value == "h2-live" || value == "h2-memory" ||
            value == "h2-interrupt" || value == "h2-reopen";

        internal static bool UseStationaryClock => phase == "h2-interrupt" || phase == "h2-reopen";

        internal static void Configure(GameRoot root, string mode, string output, string manifestPath)
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if (root == null || phase != null || manifest?.Product != Application.productName ||
                !Regex.IsMatch(Application.productName, @"\ADL B3B2 Smoke Two Floor Q[0-9a-f]{8}\z") ||
                !Regex.IsMatch(manifest.Nonce ?? "", @"\A[0-9a-f]{32}\z") || File.Exists(output) ||
                !Directory.Exists(Path.GetDirectoryName(output)) || mode == "h2-interrupt" && Path.GetFileName(output) != "cut.json")
                throw new InvalidOperationException("h2.qualification.isolation");
            string relative = Path.Combine(manifest.Nonce.Substring(0, 12), "living-dungeon-b3b2-qualification-" + manifest.Nonce + ".json");
            string path = Path.Combine(Application.persistentDataPath, relative);
            if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 ||
                (File.GetAttributes(Path.GetDirectoryName(path)) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("h2.qualification.seed_missing");
            var config = JsonUtility.FromJson<BuildConfig>(root.buildConfigJson.text);
            config.save.fileName = relative;
            root.buildConfigJson = new TextAsset(JsonUtility.ToJson(config));
            configuredAt = Stopwatch.GetTimestamp();
            phase = mode; destination = output; savePath = path; nonce = manifest.Nonce;
        }

        internal static void Loaded(byte[] bytes)
        {
            if (UseStationaryClock && bytes != null) loadedHash = SpatialContractSha256.Compute(bytes);
        }

        internal static void AfterWindowsReplace(string active, byte[] original, byte[] candidate)
        {
            if (!armed || !string.Equals(active, savePath, StringComparison.OrdinalIgnoreCase)) return;
            armed = false;
            using var process = Process.GetCurrentProcess();
            WriteExclusive(destination, JsonUtility.ToJson(new Marker {
                Cut = "canonical-post-replace", Nonce = nonce, Ordinal = 1, SavePath = active,
                Pid = process.Id, Created = process.StartTime.ToUniversalTime().ToFileTimeUtc(),
                Predecessor = SpatialContractSha256.Compute(original), Candidate = SpatialContractSha256.Compute(candidate)
            }));
            // Main thread is held between the real replacement and its flush/readback.
            // Only the enclosing supervisor's owned job can complete this stage.
            Thread.Sleep(Timeout.Infinite);
        }

        internal static void SamplePersistence(string boundary, string active, byte[] original, byte[] candidate,
            byte[] activeBefore, byte[] readback)
        {
            if (!diagnostic || !string.Equals(active, savePath, StringComparison.OrdinalIgnoreCase)) return;
            if (report.MemoryCount == report.Memory.Length) { report.DroppedMemory++; return; }
            int payload = original.Length + (ReferenceEquals(original, candidate) ? 0 : candidate.Length);
            if (!ReferenceEquals(activeBefore, original) && !ReferenceEquals(activeBefore, candidate)) payload += activeBefore.Length;
            if (readback != null && !ReferenceEquals(readback, original) && !ReferenceEquals(readback, candidate) &&
                !ReferenceEquals(readback, activeBefore)) payload += readback.Length;
            report.Memory[report.MemoryCount++] = new MemorySample {
                Phase = boundary, ClockTicks = Stopwatch.GetTimestamp(), CheckedPayloadBytes = payload,
                ManagedHeap = GC.GetTotalMemory(false), UnityAllocated = Profiler.GetTotalAllocatedMemoryLong()
            };
        }

        internal static IEnumerator Run(GameRoot root)
        {
            if (phase == null) yield break;
            report = new Report { Mode = phase, Product = Application.productName, ClockFrequency = Stopwatch.Frequency };
            var work = Collect(root);
            try {
                while (true) {
                    bool more = false;
                    try { more = work.MoveNext(); }
                    catch (Exception error) { report.Failure = error.ToString(); UnityEngine.Debug.LogException(error); }
                    if (!more) break;
                    yield return work.Current;
                }
            } finally {
                (work as IDisposable)?.Dispose();
                diagnostic = false;
                armed = false;
                ForegroundQualificationMetrics.Enabled = false;
            }
            if (phase == "h2-interrupt") {
                Application.Quit(3); // A graceful return cannot satisfy the interruption stage.
                yield break;
            }
            Array.Resize(ref report.Frames, report.FrameCount);
            if (report.Memory != null) Array.Resize(ref report.Memory, report.MemoryCount);
            WriteExclusive(destination, JsonUtility.ToJson(report, true));
            Application.Quit(report.Passed ? 0 : 1);
        }

        private static IEnumerator Collect(GameRoot root)
        {
            for (int i = 0; i < 600 && root?.TimeService?.HasForegroundClock != true; i++) yield return null;
            if (root?.TimeService?.HasForegroundClock != true ||
                !string.Equals(root.SaveService.SavePath, savePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("h2.qualification.bootstrap");
            report.BootSeconds = (Stopwatch.GetTimestamp() - configuredAt) / (double)Stopwatch.Frequency;
            report.LoadedSha256 = loadedHash;
            report.Profile = root.Save.livingDungeon?.Scheduler?.ProfileId;
            if (UseStationaryClock) {
                var work = root.QualifyH2Stationary(phase == "h2-interrupt", () => armed = true);
                while (work.MoveNext()) yield return work.Current;
                // Only a normally completed reopen can reach this report.
                WriteExclusive(destination + ".save.json", root.SaveService.CanonicalSession.GetCurrentBytes());
                report.Passed = true;
                yield break;
            }
            report.Device = SystemInfo.deviceModel; report.OS = SystemInfo.operatingSystem;
            report.Graphics = SystemInfo.graphicsDeviceName + " / " + SystemInfo.graphicsDeviceType;
            report.Unity = Application.unityVersion;
            report.Width = Screen.width; report.Height = Screen.height;
            report.RefreshRate = Screen.currentResolution.refreshRateRatio.value;
            report.VSync = QualitySettings.vSyncCount; report.TargetFrameRate = Application.targetFrameRate;
            report.Warmup = WarmupFrames;
            bool memory = phase == "h2-memory";
            if (memory) {
                report.Memory = new MemorySample[SampleCapacity];
                Calibrate();
            }
            for (int i = 0; i < WarmupFrames; i++) yield return null;
            using var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            using var render = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread", 1);
            using var allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            ForegroundQualificationMetrics.Reset();
            ForegroundQualificationMetrics.Enabled = true;
            diagnostic = memory;
            double start = Time.realtimeSinceStartupAsDouble, previous = start;
            do {
                yield return null;
                double now = Time.realtimeSinceStartupAsDouble;
                if (report.FrameCount == report.Frames.Length || now <= previous)
                    throw new InvalidOperationException("h2.qualification.frame_capacity_or_clock");
                var state = root.Save.livingDungeon;
                var frame = new Frame {
                    Seconds = now - previous, Accepted = state.Scheduler?.AcceptedThrough ?? -1,
                    Closed = state.Scheduler?.ClosedThrough ?? -1, Tick = root.Save.totalTicks,
                    MainThreadNs = Counter(main), RenderThreadNs = Counter(render), AllocatedBytes = Counter(allocations),
                    EligibleThrough = root.TimeService.ForegroundThrough, Focused = Application.isFocused,
                    AttemptedWrites = ForegroundQualificationMetrics.Calls[(int)ForegroundQualificationMetrics.Area.AtomicWrite],
                    Publications = ForegroundQualificationMetrics.Calls[(int)ForegroundQualificationMetrics.Area.Publication],
                    NextSequence = root.Save.runHistory.NextRunSequence, HistoryCount = root.Save.runHistory.RecentOutcomes.Length
                };
                foreach (var party in state.Parties) {
                    frame.Parties++; frame.Ordinal += party.NextOrdinal; frame.CheckpointBytes += party.Checkpoint.Length;
                }
                report.Frames[report.FrameCount++] = frame;
                previous = now;
            } while (previous - start < SamplingSeconds || report.FrameCount < MinimumSamples);
            diagnostic = false;
            report.Areas = Enum.GetNames(typeof(ForegroundQualificationMetrics.Area));
            report.Calls = ForegroundQualificationMetrics.Calls;
            report.Ticks = ForegroundQualificationMetrics.Ticks;
            report.Allocations = ForegroundQualificationMetrics.Allocated;
            using var process = Process.GetCurrentProcess();
            report.WorkingSet = process.WorkingSet64; report.PrivateBytes = process.PrivateMemorySize64;
            WriteExclusive(destination + ".save.json", root.SaveService.CanonicalSession.GetCurrentBytes());
            report.Passed = report.DroppedMemory == 0 && (!memory || report.MemoryCount > 0);
        }

        private static long Counter(ProfilerRecorder recorder) => recorder.Valid && recorder.Count > 0 ? recorder.LastValue : -1;

        private static void Calibrate()
        {
            const int bytes = 4 * 1024 * 1024;
            long before = GC.GetTotalMemory(false), allocated = GC.GetAllocatedBytesForCurrentThread();
            var managed = new byte[bytes];
            report.ManagedCalibrationDelta = GC.GetTotalMemory(false) - before;
            report.AllocationCalibrationDelta = GC.GetAllocatedBytesForCurrentThread() - allocated;
            GC.KeepAlive(managed);
            report.ManagedCalibrated = report.ManagedCalibrationDelta >= bytes && report.AllocationCalibrationDelta >= bytes;
            long nativeBefore = Profiler.GetTotalAllocatedMemoryLong();
            using (var native = new NativeArray<byte>(bytes, Allocator.Persistent)) {
                report.NativeCalibrationDelta = Profiler.GetTotalAllocatedMemoryLong() - nativeBefore;
            }
            report.NativeCalibrated = report.NativeCalibrationDelta >= bytes;
        }

        private static void WriteExclusive(string path, string contents) =>
            WriteExclusive(path, System.Text.Encoding.UTF8.GetBytes(contents));

        private static void WriteExclusive(string path, byte[] contents)
        {
            using (var stream = new FileStream(path + ".tmp", FileMode.CreateNew, FileAccess.Write)) {
                stream.Write(contents, 0, contents.Length);
                stream.Flush(true);
            }
            File.Move(path + ".tmp", path);
        }
    }

    public partial class GameRoot
    {
        internal IEnumerator QualifyH2Stationary(bool interrupt, Action arm)
        {
            var preparation = PrepareForegroundQualification();
            while (preparation.MoveNext()) yield return preparation.Current;
            if (!Save.livingDungeon.IsClosed)
                throw new InvalidOperationException("h2.qualification.drain_unclosed");
            if (!interrupt) yield break;
            var advance = QualificationAdvance();
            arm();
            for (int frame = 0; frame < 160; frame++) {
                advance(.25);
                UpdateForeground();
                if (foregroundFailure != null || foregroundRetryPending) break;
                yield return null;
            }
            throw new InvalidOperationException("h2.qualification.missed_native_cut");
        }
    }
}
#endif
