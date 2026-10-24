using System;
using System.Diagnostics;
using Unity.Profiling;

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
        internal static void Reset() { Array.Clear(Calls, 0, Calls.Length); Array.Clear(Ticks, 0, Ticks.Length); Array.Clear(Allocated, 0, Allocated.Length); ResetDetailed(); }
        internal static Scope Measure(Area area) => new Scope(area);

        // Separate from the historical inclusive Area counters. Main-thread diagnostic
        // campaign only; no input bytes, hashes, parsing or gameplay objects are collected.
        internal enum Boundary { Pump, StagePreparation, CompleteValidation, PriorValidation, CandidateValidation,
            OwnerValidation, Restoration, SupportedReader, StartRecreation, PrefixReplay, AdmissionAssurance,
            FinalPredecessor, SemanticReadback, DurableFileSystem, PostPublicationInspection, ReleaseCheck,
            SettlementPredecessor, Feasibility, SessionOpen, ExecutionReconstruction, AdmissionReconstruction, Acceptance,
            LifetimeBranch, TransferBlocked, TransferReleased }
        internal enum Origin { Unspecified, Pump, Acceptance, Execution, TerminalPin, Settlement, Release,
            Recovery, Opportunity, PassiveTick, Closure, Admission, LifetimeNormal, LifetimeAlternative,
            FinalWrite, Readback, Inspection }
        // Technical diagnostic storage limits, unrelated to gameplay/replay limits.
        internal const int MaximumRecords = 4096, MaximumNesting = 64;
        [Serializable] internal struct Record
        {
            public Boundary Boundary;
            public Origin Origin;
            public int Sequence, Level, Parent;
            public long PrefixDepth, InclusiveTicks, SelfTicks;
        }
        internal static bool DetailedEnabled;
        internal static int RecordCount, DroppedRecords, DroppedScopes;
        internal static Record[] Records => DetailData.Records;
        internal static long[] BoundaryCalls => DetailData.Calls;
        internal static long[] InclusiveTicks => DetailData.Inclusive;
        internal static long[] SelfTicks => DetailData.Self;
        internal static long[,] OriginCalls => DetailData.Origins;
        private static bool detailDataCreated;
        // No detailed recording buffers are allocated by ordinary Area.Measure calls.
        private static class DetailData
        {
            internal static readonly Record[] Records = new Record[MaximumRecords];
            internal static readonly long[] Calls = new long[Enum.GetValues(typeof(Boundary)).Length],
                Inclusive = new long[Enum.GetValues(typeof(Boundary)).Length], Self = new long[Enum.GetValues(typeof(Boundary)).Length];
            internal static readonly long[,] Origins = new long[Enum.GetValues(typeof(Boundary)).Length, Enum.GetValues(typeof(Origin)).Length];
            internal static readonly long[] ChildTicks = new long[MaximumNesting];
            internal static readonly int[] RecordStack = new int[MaximumNesting];
            static DetailData() { detailDataCreated = true; }
        }
        private static int nesting, sequence = -1;
        private static long prefixDepth = -1;
        private static Origin origin;
        internal static int ActiveDepth => nesting;
        private static class Markers
        {
            internal static readonly ProfilerMarker[] Values = Create();
            private static ProfilerMarker[] Create()
            {
                var names = Enum.GetNames(typeof(Boundary));
                var values = new ProfilerMarker[names.Length];
                for (int i = 0; i < names.Length; i++) values[i] = new ProfilerMarker("DungeonLord.Foreground." + names[i]);
                return values;
            }
        }
        // Call between measured operations, never while a scope is open.
        internal static void ResetDetailed()
        {
            if (nesting != 0 || !detailDataCreated) return;
            Array.Clear(Records, 0, Records.Length);
            Array.Clear(BoundaryCalls, 0, BoundaryCalls.Length);
            Array.Clear(InclusiveTicks, 0, InclusiveTicks.Length);
            Array.Clear(SelfTicks, 0, SelfTicks.Length);
            Array.Clear(OriginCalls, 0, OriginCalls.Length);
            RecordCount = DroppedRecords = DroppedScopes = 0;
            origin = Origin.Unspecified; sequence = -1; prefixDepth = -1;
        }
        internal static DetailScope Trace(Boundary boundary, Origin caller = Origin.Unspecified, int party = -1, long depth = -1)
            => new DetailScope(boundary, caller, party, depth);
        internal readonly struct DetailScope : IDisposable
        {
            private readonly bool enabled;
            private readonly int index, level, row, previousSequence;
            private readonly Origin previousOrigin;
            private readonly long start, previousPrefix;
            internal DetailScope(Boundary boundary, Origin caller, int party, long depth)
            {
                this = default;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!Enabled || !DetailedEnabled) return;
                if (nesting == MaximumNesting) { DroppedScopes++; return; }
                // Initialize fixed names before starting the measured span.
                var marker = Markers.Values[(int)boundary];
                enabled = true; index = (int)boundary; level = nesting++;
                previousOrigin = origin; previousSequence = sequence; previousPrefix = prefixDepth;
                if (caller != Origin.Unspecified) origin = caller;
                if (party >= 0) sequence = party;
                if (depth >= 0) prefixDepth = depth;
                row = RecordCount < MaximumRecords ? RecordCount++ : -1;
                if (row >= 0) Records[row] = new Record { Boundary = boundary, Origin = origin,
                    Sequence = sequence, PrefixDepth = prefixDepth, Level = level,
                    Parent = level == 0 ? -1 : DetailData.RecordStack[level - 1] };
                else DroppedRecords++;
                DetailData.RecordStack[level] = row; DetailData.ChildTicks[level] = 0;
                BoundaryCalls[index]++; OriginCalls[index, (int)origin]++;
                marker.Begin(); start = Stopwatch.GetTimestamp();
#endif
            }
            public void Dispose()
            {
                if (!enabled) return;
                long elapsed = Stopwatch.GetTimestamp() - start;
                Markers.Values[index].End();
                long self = elapsed - DetailData.ChildTicks[level];
                InclusiveTicks[index] += elapsed; SelfTicks[index] += self;
                if (row >= 0) { Records[row].InclusiveTicks = elapsed; Records[row].SelfTicks = self; }
                nesting--;
                if (level > 0) DetailData.ChildTicks[level - 1] += elapsed;
                origin = previousOrigin; sequence = previousSequence; prefixDepth = previousPrefix;
            }
        }
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
