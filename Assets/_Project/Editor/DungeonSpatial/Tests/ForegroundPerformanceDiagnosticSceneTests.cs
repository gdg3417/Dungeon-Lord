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
        [Serializable] private sealed class Row
        {
            public int Frame, Sample, Children;
            public string Name;
            public double TotalMs, SelfMs;
        }
        [Serializable] private sealed class Capture
        {
            public string Unity, InputSha256, OutputSha256;
            public long StopwatchFrequency;
            public int DroppedRecords, DroppedScopes;
            public bool CpuFrameStreamAvailable, ProfileSaved;
            public string[] Boundaries, OriginNames;
            public Metrics.Record[] Origins;
            public List<Row> Timeline = new List<Row>(), Hierarchy = new List<Row>();
        }
        [UnityTest, Timeout(300000)] public IEnumerator BoundedProductionAdmissionReportsAvailableCpuEvidence()
        {
            // Approved production content/timing, two captured floors; a single admission.
            // All canonical I/O is the fixture's deterministic in-memory filesystem.
            var f = TwoFloor.ProductionDungeon();
            var writer = DungeonBuilder.M0.Tests.EditMode.PhaseSixA4Tests.Writer(f);
            f.Accept(writer.CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                Gameplay.DungeonSpatial.FloorLifecycleAction.Activate, DungeonBuilder.M0.Tests.EditMode.PhaseSixA4Tests.FloorTwo(f)));
            TwoFloor.Through(f, 0);
            var input = f.Session.GetCurrentBytes();
            var capture = new Capture { Unity = Application.unityVersion,
                InputSha256 = Gameplay.DungeonSpatial.SpatialContractSha256.Compute(input),
                Boundaries = Enum.GetNames(typeof(Metrics.Boundary)), OriginNames = Enum.GetNames(typeof(Metrics.Origin)),
                StopwatchFrequency = System.Diagnostics.Stopwatch.Frequency };
            string directory = Path.GetFullPath("TestResults/foreground-validation-capture-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            bool enabled = ProfilerDriver.enabled, editor = ProfilerDriver.profileEditor;
            bool cpu = ProfilerDriver.IsAreaEnabled(UnityEngine.Profiling.ProfilerArea.CPU);
            Assert.That(ProfilerDriver.deepProfiling, Is.False, "Use this bounded capture without Deep Profiling");
            var profilerWindow = UnityEditor.EditorWindow.GetWindow<UnityEditor.ProfilerWindow>();
            try {
                ProfilerDriver.profileEditor = true;
                ProfilerDriver.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.CPU, true);
                ProfilerDriver.enabled = true;
                yield return null;
                int first = ProfilerDriver.lastFrameIndex;
                var service = new SaveService(null, new SaveConfig(), directory);
                service.ConfigureCanonical(f.Profile, f.Production, f.Compatibility, f.Configuration,
                    Gameplay.DungeonSpatial.LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.PositionProfiles, f.Occupancy);
                Cases.Run("production-admission", true, () => {
                    TwoFloor.Through(f, f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value);
                    service.InspectForeground(f.Runtime, 10);
                });
                capture.Origins = Metrics.Records.Take(Metrics.RecordCount).ToArray();
                capture.DroppedRecords = Metrics.DroppedRecords; capture.DroppedScopes = Metrics.DroppedScopes;
                var output = f.Session.GetCurrentBytes();
                capture.OutputSha256 = Gameplay.DungeonSpatial.SpatialContractSha256.Compute(output);
                yield return null; yield return null;
                int last = ProfilerDriver.lastFrameIndex;
                capture.CpuFrameStreamAvailable = first >= 0 && last >= first;
                TestContext.WriteLine("CPU capture frame range=" + first + ".." + last + " connection=" +
                    ProfilerDriver.GetConnectionIdentifier(ProfilerDriver.connectedProfiler));
                for (int frame = Math.Max(first, ProfilerDriver.firstFrameIndex); frame <= last; frame++) {
                    using (var raw = ProfilerDriver.GetRawFrameDataView(frame, 0)) {
                        if (!raw.valid) continue;
                        for (int i = 0; i < raw.sampleCount; i++) {
                            string name = raw.GetSampleName(i);
                            if (!name.StartsWith("DungeonLord.Foreground.", StringComparison.Ordinal)) continue;
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
                            int item = pending.Pop();
                            string name = hierarchy.GetItemName(item);
                            if (name.StartsWith("DungeonLord.Foreground.", StringComparison.Ordinal))
                                capture.Hierarchy.Add(new Row { Frame = frame, Sample = item, Name = name,
                                    TotalMs = hierarchy.GetItemColumnDataAsDouble(item, HierarchyFrameDataView.columnTotalTime),
                                    SelfMs = hierarchy.GetItemColumnDataAsDouble(item, HierarchyFrameDataView.columnSelfTime) });
                            children.Clear(); hierarchy.GetItemChildren(item, children);
                            foreach (int child in children) pending.Push(child);
                        }
                    }
                }
                // Re-run the exact pinned input without instrumentation, outside the capture.
                f.FileSystem.Seed(f.ActivePath, input); f.Reopen();
                Cases.Run("production-admission-disabled", false, () => TwoFloor.Through(f, f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value));
                CollectionAssert.AreEqual(output, f.Session.GetCurrentBytes());
                Assert.That(capture.DroppedRecords + capture.DroppedScopes, Is.Zero);
                if (capture.CpuFrameStreamAvailable) {
                    Assert.That(capture.Timeline.Count, Is.GreaterThan(0));
                    Assert.That(capture.Hierarchy.Count, Is.GreaterThan(0));
                    capture.ProfileSaved = ProfilerDriver.SaveProfile(Path.Combine(directory, "cpu.data"));
                    Assert.That(capture.ProfileSaved, Is.True);
                } else TestContext.WriteLine("CPU Timeline/Hierarchy unavailable: batch Editor supplied no frame stream; scoped self times are not Unity Hierarchy self times.");
                string json = JsonUtility.ToJson(capture, true);
                Assert.That(JsonUtility.FromJson<Capture>(json).Origins.Length, Is.EqualTo(capture.Origins.Length));
                File.WriteAllText(Path.Combine(directory, "capture.json"), json);
                TestContext.WriteLine("Bounded diagnostic output=" + directory);
            } finally {
                Metrics.Enabled = Metrics.DetailedEnabled = false;
                ProfilerDriver.enabled = enabled; ProfilerDriver.profileEditor = editor;
                ProfilerDriver.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.CPU, cpu);
                profilerWindow.Close();
            }
        }
    }
}
#endif
