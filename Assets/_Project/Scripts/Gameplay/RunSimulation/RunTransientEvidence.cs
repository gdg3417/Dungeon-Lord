using System;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public static class RunTransientEvidence
    {
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
            }
        }
    }
}
