#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.TestTools;
using Metrics = DungeonBuilder.M0.ForegroundQualificationMetrics;
using Cases = DungeonBuilder.M0.Tests.EditMode.ForegroundPerformanceInstrumentationTests;
using TwoFloor = DungeonBuilder.M0.Tests.EditMode.LivingDungeonTwoFloorTests;

namespace DungeonBuilder.M0.Editor.Tests
{
    // Discover the focused core cases without editing historical adapters.
    public sealed class ForegroundPerformanceDiagnostics : Cases { }

    public sealed class ForegroundPerformanceDiagnosticSceneTests
    {
        private const string OutputRootVariable = "DUNGEON_FOREGROUND_DIAGNOSTIC_OUTPUT_ROOT";
        private const string SourceVariable = "DUNGEON_FOREGROUND_DIAGNOSTIC_SOURCE";
        [Serializable] private sealed class Row
        {
            public int Frame, Sample, Children;
            public string Name;
            public double TotalMs, SelfMs;
        }
        [Serializable] private sealed class Capture
        {
            public string Unity, Source, InputSha256, OutputSha256, CpuStatus;
            public long StopwatchFrequency;
            public int DroppedRecords, DroppedScopes;
            public bool CpuFrameStreamAvailable, ProfileSaved;
            public string[] Boundaries, OriginNames;
            public Metrics.Record[] Origins;
            public List<Row> Timeline = new List<Row>(), Hierarchy = new List<Row>();
        }
        // Read-only guard: no profiler setting, selection, history or window is changed,
        // even for an explicit export. In particular, never open/close ProfilerWindow.
        private sealed class ProfilerState
        {
            private readonly bool enabled = ProfilerDriver.enabled, editor = ProfilerDriver.profileEditor,
                cpu = ProfilerDriver.IsAreaEnabled(UnityEngine.Profiling.ProfilerArea.CPU);
            internal readonly bool Deep = ProfilerDriver.deepProfiling;
            private readonly int connection = ProfilerDriver.connectedProfiler;
            private readonly int[] windows = Resources.FindObjectsOfTypeAll<UnityEditor.ProfilerWindow>().Select(w => w.GetInstanceID()).ToArray();
            internal void AssertUnchanged()
            {
                Assert.That(ProfilerDriver.enabled, Is.EqualTo(enabled));
                Assert.That(ProfilerDriver.profileEditor, Is.EqualTo(editor));
                Assert.That(ProfilerDriver.IsAreaEnabled(UnityEngine.Profiling.ProfilerArea.CPU), Is.EqualTo(cpu));
                Assert.That(ProfilerDriver.deepProfiling, Is.EqualTo(Deep));
                Assert.That(ProfilerDriver.connectedProfiler, Is.EqualTo(connection));
                CollectionAssert.AreEquivalent(windows, Resources.FindObjectsOfTypeAll<UnityEditor.ProfilerWindow>().Select(w => w.GetInstanceID()));
                TestContext.WriteLine("Profiler state preserved: enabled=" + enabled + " editor=" + editor +
                    " cpu=" + cpu + " deep=" + Deep + " windows=" + windows.Length + " connection=" + connection);
            }
        }
        [Test] public void ProfilerStateGuardPreservesSettingsOnException()
        {
            var profiler = new ProfilerState();
            Assert.Throws<InvalidOperationException>(() => {
                try { throw new InvalidOperationException("test-only exception"); }
                finally { profiler.AssertUnchanged(); }
            });
        }
        [UnityTest, Timeout(300000)] public IEnumerator BoundedProductionAdmissionReportsAvailableCpuEvidence()
        {
            var profiler = new ProfilerState();
            bool metricsEnabled = Metrics.Enabled, detailsEnabled = Metrics.DetailedEnabled;
            try {
                // Approved production content/timing; canonical I/O is in-memory only.
                var f = TwoFloor.ProductionDungeon();
                var writer = DungeonBuilder.M0.Tests.EditMode.PhaseSixA4Tests.Writer(f);
                f.Accept(writer.CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                    Gameplay.DungeonSpatial.FloorLifecycleAction.Activate, DungeonBuilder.M0.Tests.EditMode.PhaseSixA4Tests.FloorTwo(f)));
                TwoFloor.Through(f, 0);
                var input = f.Session.GetCurrentBytes();
                string outputRoot = Environment.GetEnvironmentVariable(OutputRootVariable);
                string source = Environment.GetEnvironmentVariable(SourceVariable);
                bool export = !string.IsNullOrWhiteSpace(outputRoot) && source?.Length == 40 && source.All(Uri.IsHexDigit);
                if (!string.IsNullOrWhiteSpace(outputRoot) && !export)
                    TestContext.WriteLine("Optional diagnostic export unavailable: supply a full source commit using " + SourceVariable);
                int first = export && !profiler.Deep ? ExistingFrameIndex() : -1;
                // The service constructor does not create this synthetic directory or file.
                var service = new SaveService(null, new SaveConfig(), Path.Combine(Path.GetTempPath(), "foreground-diagnostic-" + Guid.NewGuid().ToString("N")));
                service.ConfigureCanonical(f.Profile, f.Production, f.Compatibility, f.Configuration,
                    Gameplay.DungeonSpatial.LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.PositionProfiles, f.Occupancy);
                Cases.Run("production-admission-correctness", true, () => {
                    TwoFloor.Through(f, f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value);
                    service.InspectForeground(f.Runtime, 10);
                });
                var records = Metrics.Records.Take(Metrics.RecordCount).ToArray();
                int droppedRecords = Metrics.DroppedRecords, droppedScopes = Metrics.DroppedScopes;
                var output = f.Session.GetCurrentBytes();
                // Always perform correctness checks before optional capture/export work.
                f.FileSystem.Seed(f.ActivePath, input); f.Reopen();
                Cases.Run("production-admission-disabled", false, () => {
                    TwoFloor.Through(f, f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value);
                    service.InspectForeground(f.Runtime, 10);
                });
                CollectionAssert.AreEqual(output, f.Session.GetCurrentBytes());
                Assert.That(droppedRecords + droppedScopes, Is.Zero);
                Assert.That(records.Any(r => r.Boundary == Metrics.Boundary.SupportedReader && r.PrefixDepth > 0), Is.True);
                TestContext.WriteLine("Exact enabled/disabled canonical bytes equal; bounded origin/nesting/prefix verification passed.");
                if (!export) {
                    TestContext.WriteLine("Optional CPU Timeline/Hierarchy not requested; no output files or profiler windows created. Existing frame index=" +
                        ExistingFrameIndex() + (profiler.Deep ? "; Deep Profiling remains enabled." : "."));
                    if (ExistingFrameIndex() < 0) TestContext.WriteLine("CPU Timeline/Hierarchy unavailable: no existing frame stream.");
                    yield break;
                }
                yield return null; yield return null;
                var capture = new Capture { Unity = Application.unityVersion, Source = source,
                    InputSha256 = Gameplay.DungeonSpatial.SpatialContractSha256.Compute(input),
                    OutputSha256 = Gameplay.DungeonSpatial.SpatialContractSha256.Compute(output),
                    Boundaries = Enum.GetNames(typeof(Metrics.Boundary)), OriginNames = Enum.GetNames(typeof(Metrics.Origin)),
                    StopwatchFrequency = System.Diagnostics.Stopwatch.Frequency, Origins = records,
                    DroppedRecords = droppedRecords, DroppedScopes = droppedScopes };
                TryExport(outputRoot, first, profiler.Deep, capture);
            } finally {
                Metrics.Enabled = metricsEnabled; Metrics.DetailedEnabled = detailsEnabled;
                // No profiler setters are used, so failure paths need no state mutation to undo.
                profiler.AssertUnchanged();
            }
        }
        private static int ExistingFrameIndex()
        {
            try { return ProfilerDriver.lastFrameIndex; }
            catch (Exception error) {
                TestContext.WriteLine("Optional CPU frame index unavailable: " + error.GetType().Name);
                return -1;
            }
        }
        private static void TryExport(string outputRoot, int first, bool deep, Capture capture)
        {
            // Best-effort evidence only. Missing frames, Deep Profiling, API errors and
            // export failures must never turn byte-equivalence into a gameplay failure.
            try {
                int last = ProfilerDriver.lastFrameIndex;
                capture.CpuStatus = deep ? "Deep Profiling enabled; optional CPU timing omitted" : "No existing CPU frame stream";
                if (!deep && first >= 0 && last >= first) {
                    for (int frame = Math.Max(first, ProfilerDriver.firstFrameIndex); frame <= last; frame++) {
                        using (var raw = ProfilerDriver.GetRawFrameDataView(frame, 0)) {
                            if (!raw.valid) continue;
                            for (int i = 0; i < raw.sampleCount; i++) {
                                string name = raw.GetSampleName(i);
                                if (name.StartsWith("DungeonLord.Foreground.", StringComparison.Ordinal))
                                    capture.Timeline.Add(new Row { Frame = frame, Sample = i, Name = name,
                                        Children = raw.GetSampleChildrenCount(i), TotalMs = raw.GetSampleTimeMs(i) });
                            }
                        }
                        using (var hierarchy = ProfilerDriver.GetHierarchyFrameDataView(frame, 0,
                            HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnDontSort, false)) {
                            if (!hierarchy.valid) continue;
                            var pending = new Stack<int>(); pending.Push(hierarchy.GetRootItemID());
                            var children = new List<int>();
                            while (pending.Count > 0) {
                                int item = pending.Pop(); string name = hierarchy.GetItemName(item);
                                if (name.StartsWith("DungeonLord.Foreground.", StringComparison.Ordinal))
                                    capture.Hierarchy.Add(new Row { Frame = frame, Sample = item, Name = name,
                                        TotalMs = hierarchy.GetItemColumnDataAsDouble(item, HierarchyFrameDataView.columnTotalTime),
                                        SelfMs = hierarchy.GetItemColumnDataAsDouble(item, HierarchyFrameDataView.columnSelfTime) });
                                children.Clear(); hierarchy.GetItemChildren(item, children);
                                foreach (int child in children) pending.Push(child);
                            }
                        }
                    }
                    capture.CpuFrameStreamAvailable = capture.Timeline.Count > 0 && capture.Hierarchy.Count > 0;
                    capture.CpuStatus = capture.CpuFrameStreamAvailable ? "Existing CPU frame data collected" : "CPU Timeline/Hierarchy samples unavailable";
                }
            } catch (Exception error) { capture.CpuStatus = "Optional CPU collection unavailable: " + error.GetType().Name; }
            TestContext.WriteLine(capture.CpuStatus);
            try {
                if (!Path.IsPathRooted(outputRoot)) { TestContext.WriteLine("Optional export unavailable: output root must be absolute"); return; }
                string directory = Path.Combine(outputRoot, "foreground-validation-capture-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);
                if (capture.CpuFrameStreamAvailable) {
                    try { capture.ProfileSaved = ProfilerDriver.SaveProfile(Path.Combine(directory, "cpu.data")); }
                    catch (Exception error) { capture.CpuStatus += "; profile export unavailable: " + error.GetType().Name; }
                }
                File.WriteAllText(Path.Combine(directory, "capture.json"), JsonUtility.ToJson(capture, true));
                TestContext.WriteLine("Explicit bounded diagnostic output=" + directory + "; source=" + capture.Source + "; profileSaved=" + capture.ProfileSaved);
            } catch (Exception error) { TestContext.WriteLine("Optional diagnostic export unavailable: " + error.GetType().Name); }
        }
    }
}
#endif
