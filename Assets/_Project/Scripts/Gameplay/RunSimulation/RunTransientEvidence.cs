using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public static class RunTransientEvidence
    {
        // Observer identity only: canonical serialization of content on traversed branches.
        // Excludes wallet, knowledge and gameplay tuning; preserves ID, option, ordering and tile.
        internal static bool TryCorridorFingerprint(RunOutcomeRecord outcome, CorridorContentAuthority current,
            CanonicalSpatialSerializationLimits limits, out string fingerprint)
        {
            fingerprint = null;
            if (current?.Assignments == null || !limits.IsValid) return false;
            try
            {
                var branches = (outcome?.BranchOutcomes ?? Array.Empty<BranchOutcomeEvidence>())
                    .Where(b => b.Traversed && b.Fork != null)
                    .Select(b => (b.Fork.FloorInstanceId, b.Fork.OptionalBranchId)).ToHashSet();
                var relevant = new CorridorContentAuthority { Assignments = current.Assignments
                    .Where(a => a != null && branches.Contains((a.FloorInstanceId, a.OptionalBranchId))).ToArray() };
                var writer = new ContractJsonWriter(limits.Serialized);
                PhaseFiveSaveContracts.Write(writer, relevant);
                fingerprint = SpatialContractSha256.Compute(writer.Finish());
                return true;
            }
            catch { return false; }
        }
        public static void Retain(SaveData source, SaveData target)
        {
            var previous = (source?.runHistory?.RecentOutcomes ?? Array.Empty<RunOutcomeRecord>())
                .Concat(new[] { source?.runHistory?.LatestOutcome });
            foreach (var run in (target?.runHistory?.RecentOutcomes ?? Array.Empty<RunOutcomeRecord>())
                .Concat(new[] { target?.runHistory?.LatestOutcome }))
            {
                if (run == null) continue;
                var match = previous.FirstOrDefault(r => r != null && r.RunId == run.RunId && r.TickStarted == run.TickStarted);
                if (match == null) continue;
                run.Party = match.Party; run.EncounterEvents = match.EncounterEvents; run.BranchOutcomes = match.BranchOutcomes;
                run.FloorTransitions = match.FloorTransitions; run.DepthObjective = match.DepthObjective;
                run.SpatialEvents = match.SpatialEvents;
                run.PresentationSteps = match.PresentationSteps;
                run.PresentationLayoutFingerprint = match.PresentationLayoutFingerprint;
                run.PresentationCorridorFingerprint = match.PresentationCorridorFingerprint;
            }
        }
    }
}
