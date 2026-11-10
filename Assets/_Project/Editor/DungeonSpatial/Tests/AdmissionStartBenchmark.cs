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
            public int DroppedRecords, DroppedScopes, ActiveDepth;
        }
        [Serializable] private sealed class Report
        {
            public string Source, Unity, InputBase64, OutputBase64;
            public long Frequency;
            public Sample[] Samples;
            public long AllocationProbeBytes;
            public double[] EmptyScopeMilliseconds;
        }
        [Test, Timeout(300000)] public void OneProductionAdmissionRepeatedSamples()
        {
            string root = Environment.GetEnvironmentVariable("DUNGEON_ADMISSION_BENCHMARK_ROOT");
            string source = Environment.GetEnvironmentVariable("DUNGEON_ADMISSION_BENCHMARK_SOURCE");
            bool oldEnabled = Metrics.Enabled, oldDetailed = Metrics.DetailedEnabled;
            try {
                // Calibrate outside the intact action; zero is unsupported, never zero allocation.
                Metrics.Enabled = Metrics.DetailedEnabled = false;
                long allocationProbe = GC.GetAllocatedBytesForCurrentThread();
                var allocation = new byte[65536];
                allocationProbe = GC.GetAllocatedBytesForCurrentThread() - allocationProbe;
                GC.KeepAlive(allocation);
                var emptyScopes = new double[4];
                for (int mode = 0; mode < emptyScopes.Length; mode++) {
                    Metrics.Reset(); Metrics.Enabled = Metrics.DetailedEnabled = (mode & 1) != 0;
                    using (Metrics.Trace(Metrics.Boundary.IntraroomPlanning)) { } // initialize before timing
                    Metrics.Reset();
                    var calibration = Stopwatch.StartNew();
                    for (int i = 0; i < Metrics.MaximumRecords; i++)
                        using (Metrics.Trace(Metrics.Boundary.IntraroomPlanning)) { }
                    calibration.Stop(); emptyScopes[mode] = calibration.Elapsed.TotalMilliseconds;
                }
                Metrics.Enabled = Metrics.DetailedEnabled = false;
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
                    CollectionAssert.AreEqual(input, f.Session.GetCurrentBytes());
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
                        TestContext.WriteLine("admission counters: " + string.Join(",", Enum.GetNames(typeof(Metrics.Boundary))
                            .Select((name, index) => name + "=" + Metrics.BoundaryCalls[index])));
                        // Seven private stages reuse predecessor evidence; retain every candidate,
                        // pump-entry, final-predecessor and independent durable validation.
                        Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.StagePreparation], Is.EqualTo(7));
                        Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.PriorValidation], Is.Zero);
                        Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.CandidateValidation], Is.EqualTo(7));
                        Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.FinalPredecessor], Is.EqualTo(1));
                        Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.SemanticReadback], Is.EqualTo(1));
                        Assert.That(Metrics.OriginCalls[(int)Metrics.Boundary.CompleteValidation, (int)Metrics.Origin.Pump], Is.EqualTo(1));
                        Assert.That(Metrics.OriginCalls[(int)Metrics.Boundary.CompleteValidation, (int)Metrics.Origin.FinalWrite], Is.EqualTo(1));
                        Assert.That(Metrics.OriginCalls[(int)Metrics.Boundary.CompleteValidation, (int)Metrics.Origin.Readback], Is.EqualTo(1));
                        Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.SupportedReader], Is.EqualTo(137));
                        Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.CompleteValidation], Is.EqualTo(10));
                        Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.PrefixReplay], Is.EqualTo(137));
                        Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.LifetimeBranch], Is.EqualTo(1));
                        Assert.That(Metrics.OriginCalls[(int)Metrics.Boundary.PrefixReplay, (int)Metrics.Origin.LifetimeNormal], Is.GreaterThan(0));
                        Assert.That(Metrics.OriginCalls[(int)Metrics.Boundary.PrefixReplay, (int)Metrics.Origin.LifetimeAlternative], Is.GreaterThan(0));
                        for (int i = 0; i < Metrics.RecordCount; i++) {
                            var record = Metrics.Records[i];
                            long children = 0;
                            for (int j = i + 1; j < Metrics.RecordCount; j++)
                                if (Metrics.Records[j].Parent == i) children += Metrics.Records[j].InclusiveTicks;
                            Assert.That(record.SelfTicks, Is.GreaterThanOrEqualTo(0));
                            Assert.That(record.InclusiveTicks, Is.EqualTo(record.SelfTicks + children));
                        }
                    }
                    if (pair < 0) continue;
                    var sample = new Sample { Index = pair, Enabled = enabled, Milliseconds = timer.Elapsed.TotalMilliseconds,
                        AllocatedBytes = allocated, Boundaries = Enum.GetNames(typeof(Metrics.Boundary)),
                        Calls = (long[])Metrics.BoundaryCalls.Clone(), InclusiveTicks = (long[])Metrics.InclusiveTicks.Clone(),
                        SelfTicks = (long[])Metrics.SelfTicks.Clone(), Records = Metrics.Records.Take(Metrics.RecordCount).ToArray(),
                        DroppedRecords = Metrics.DroppedRecords, DroppedScopes = Metrics.DroppedScopes, ActiveDepth = Metrics.ActiveDepth };
                    samples.Add(sample);
                    TestContext.WriteLine("admission sample=" + pair + " enabled=" + enabled + " ms=" + sample.Milliseconds + " allocated=" + allocated);
                }
                if (!string.IsNullOrEmpty(root)) {
                    Assert.That(Path.IsPathRooted(root) && source?.Length == 40, Is.True);
                    Directory.CreateDirectory(root);
                    string path = Path.Combine(root, "admission.json"); Assert.That(File.Exists(path), Is.False);
                    File.WriteAllText(path, JsonUtility.ToJson(new Report { Source = source, Unity = Application.unityVersion,
                        InputBase64 = Convert.ToBase64String(input), OutputBase64 = Convert.ToBase64String(output),
                        Frequency = Stopwatch.Frequency, Samples = samples.ToArray(), AllocationProbeBytes = allocationProbe,
                        EmptyScopeMilliseconds = emptyScopes }, true));
                }
            } finally { Metrics.Enabled = oldEnabled; Metrics.DetailedEnabled = oldDetailed; }
        }
    }
}
#endif
