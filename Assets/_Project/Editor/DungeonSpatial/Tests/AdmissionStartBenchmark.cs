#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using TwoFloor = DungeonBuilder.M0.Tests.EditMode.LivingDungeonTwoFloorTests;
using PhaseSix = DungeonBuilder.M0.Tests.EditMode.PhaseSixA4Tests;
using Metrics = DungeonBuilder.M0.ForegroundQualificationMetrics;

namespace DungeonBuilder.M0.Editor.Tests
{
    // One admission, in-memory canonical filesystem. Never runs the sustained profile.
    public sealed class AdmissionStartBenchmark
    {
        [Serializable] private sealed class Sample
        {
            public bool Enabled;
            public int Index;
            public double Milliseconds;
            public long AllocatedBytes;
            public string[] Boundaries;
            public long[] Calls, InclusiveTicks, SelfTicks;
            public Metrics.Record[] Records;
        }
        [Serializable] private sealed class Report
        {
            public string Source, Unity, InputBase64, OutputBase64;
            public long Frequency;
            public Sample[] Samples;
        }
        [Test, Timeout(300000)] public void OneProductionAdmissionRepeatedSamples()
        {
            string root = Environment.GetEnvironmentVariable("DUNGEON_ADMISSION_BENCHMARK_ROOT");
            string source = Environment.GetEnvironmentVariable("DUNGEON_ADMISSION_BENCHMARK_SOURCE");
            bool oldEnabled = Metrics.Enabled, oldDetailed = Metrics.DetailedEnabled;
            try {
                var f = TwoFloor.ProductionDungeon();
                f.Accept(PhaseSix.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                    FloorLifecycleAction.Activate, PhaseSix.FloorTwo(f)));
                TwoFloor.Through(f, 0);
                var input = f.Session.GetCurrentBytes();
                var service = new SaveService(null, new SaveConfig(), Path.Combine(Path.GetTempPath(), "admission-benchmark-" + Guid.NewGuid().ToString("N")));
                service.ConfigureCanonical(f.Profile, f.Production, f.Compatibility, f.Configuration,
                    LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.PositionProfiles, f.Occupancy);
                byte[] output = null;
                var samples = new System.Collections.Generic.List<Sample>();
                // Predeclared: one warmup per mode, five samples per mode, alternating order.
                for (int pair = -1; pair < 5; pair++) for (int slot = 0; slot < 2; slot++) {
                    bool enabled = ((pair + slot) & 1) == 0;
                    Metrics.Enabled = Metrics.DetailedEnabled = false;
                    f.FileSystem.Seed(f.ActivePath, input); f.Reopen();
                    long admission = f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value;
                    Metrics.Reset(); Metrics.Enabled = Metrics.DetailedEnabled = enabled;
                    long allocated = GC.GetAllocatedBytesForCurrentThread();
                    var timer = Stopwatch.StartNew();
                    TwoFloor.Through(f, admission); service.InspectForeground(f.Runtime, 10);
                    timer.Stop(); allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
                    Metrics.Enabled = Metrics.DetailedEnabled = false;
                    var bytes = f.Session.GetCurrentBytes();
                    if (output == null) output = bytes; else CollectionAssert.AreEqual(output, bytes);
                    Assert.That(Metrics.ActiveDepth + Metrics.DroppedRecords + Metrics.DroppedScopes, Is.Zero);
                    if (enabled) {
                        Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.SupportedReader], Is.EqualTo(138));
                        Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.CompleteValidation], Is.EqualTo(17));
                        Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.PrefixReplay], Is.EqualTo(138));
                    }
                    if (pair < 0) continue;
                    var sample = new Sample { Index = pair, Enabled = enabled, Milliseconds = timer.Elapsed.TotalMilliseconds,
                        AllocatedBytes = allocated, Boundaries = Enum.GetNames(typeof(Metrics.Boundary)),
                        Calls = (long[])Metrics.BoundaryCalls.Clone(), InclusiveTicks = (long[])Metrics.InclusiveTicks.Clone(),
                        SelfTicks = (long[])Metrics.SelfTicks.Clone(), Records = Metrics.Records.Take(Metrics.RecordCount).ToArray() };
                    samples.Add(sample);
                    TestContext.WriteLine("admission sample=" + pair + " enabled=" + enabled + " ms=" + sample.Milliseconds + " allocated=" + allocated);
                }
                if (!string.IsNullOrEmpty(root)) {
                    Assert.That(Path.IsPathRooted(root) && source?.Length == 40, Is.True);
                    Directory.CreateDirectory(root);
                    string path = Path.Combine(root, "admission.json"); Assert.That(File.Exists(path), Is.False);
                    File.WriteAllText(path, JsonUtility.ToJson(new Report { Source = source, Unity = Application.unityVersion,
                        InputBase64 = Convert.ToBase64String(input), OutputBase64 = Convert.ToBase64String(output),
                        Frequency = Stopwatch.Frequency, Samples = samples.ToArray() }, true));
                }
            } finally { Metrics.Enabled = oldEnabled; Metrics.DetailedEnabled = oldDetailed; }
        }
    }
}
#endif
