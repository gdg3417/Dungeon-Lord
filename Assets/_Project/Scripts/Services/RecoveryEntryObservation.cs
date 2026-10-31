using System;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0
{
    internal enum RecoveryEntryContext
    {
        KnownBackground,
        UnknownPostCrash,
        KnownForeground,
        PersistenceStalled
    }

    // Inactive foundation: the future recovery coordinator captures this after
    // unique canonical recovery, before draining accepted work. It must retain
    // this object through that attempt, never recapture after processing/retry.
    // This observation grants nothing, is not a cursor, and is lost on crash.
    internal sealed class RecoveryEntryObservation
    {
        internal UnixTimePoint WallEndpoint { get; }
        internal RecoveryEntryContext Context { get; }

        private RecoveryEntryObservation(UnixTimePoint wall, RecoveryEntryContext context)
        { WallEndpoint = wall; Context = context; }

        internal static RecoveryEntryObservation Capture(IPreciseTimeSource clock,
            TimeProvenance provenance, RecoveryEntryContext context)
        {
            if (clock == null || provenance == null ||
                context != RecoveryEntryContext.KnownBackground && context != RecoveryEntryContext.UnknownPostCrash)
                throw new ArgumentException("time.recovery.observation_invalid");
            // Require one complete observation. A seconds-only source cannot
            // manufacture a precise sample, nor may callers interpolate one.
            UnixTimePoint wall = clock.UtcNow();
            return FromCaptured(wall, provenance, context);
        }

        internal static RecoveryEntryObservation FromCaptured(UnixTimePoint wall,
            TimeProvenance provenance, RecoveryEntryContext context)
        {
            if (provenance == null || context != RecoveryEntryContext.KnownBackground && context != RecoveryEntryContext.UnknownPostCrash)
                throw new ArgumentException("time.recovery.observation_invalid");
            if (wall == null || wall.Seconds <= 0 || wall.CompareTo(provenance.AccountedBoundary) < 0)
                throw new ArgumentException("time.recovery.observation_invalid");
            return new RecoveryEntryObservation(wall, context);
        }
    }
}
