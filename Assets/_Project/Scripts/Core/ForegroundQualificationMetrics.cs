using System;
using System.Diagnostics;

namespace DungeonBuilder.M0
{
    // Opt-in technical instrumentation. Never saved or consulted by gameplay.
    internal static class ForegroundQualificationMetrics
    {
        internal enum Area { Stage, Prepare, Open, Validate, Projection, Restore, LifetimeProbe, Inspect, AtomicWrite, Publication, Present, WorldRender,
            TransferBlocked, TransferReleased, RecoveryApplied, Settlement, CheckpointReplay }
        internal static bool Enabled;
        internal static readonly long[] Calls = new long[Enum.GetValues(typeof(Area)).Length],
            Ticks = new long[Enum.GetValues(typeof(Area)).Length], Allocated = new long[Enum.GetValues(typeof(Area)).Length];
        internal static void Reset() { Array.Clear(Calls, 0, Calls.Length); Array.Clear(Ticks, 0, Ticks.Length); Array.Clear(Allocated, 0, Allocated.Length); }
        internal static Scope Measure(Area area) => new Scope(area);
        internal readonly struct Scope : IDisposable
        {
            private readonly int index;
            private readonly long start, allocated;
            private readonly bool enabled;
            internal Scope(Area area)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                enabled = Enabled;
#else
                enabled = false;
#endif
                index = (int)area; start = enabled ? Stopwatch.GetTimestamp() : 0;
                allocated = enabled ? GC.GetAllocatedBytesForCurrentThread() : 0;
            }
            public void Dispose()
            {
                if (!enabled) return;
                Calls[index]++; Ticks[index] += Stopwatch.GetTimestamp() - start;
                Allocated[index] += GC.GetAllocatedBytesForCurrentThread() - allocated;
            }
        }
    }
}
