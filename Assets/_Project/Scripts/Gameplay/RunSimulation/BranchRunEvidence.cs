using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public sealed class BranchOutcomeEvidence
    {
        public PhaseFiveBFork Fork { get; internal set; }
        public BranchDecisionEvidence Decision { get; internal set; }
        public string Reason { get; internal set; }
        public string PrecedenceReason { get; internal set; }
        public bool Traversed { get; internal set; }
        public bool Returned { get; internal set; }
        public string[] ReachedAssignments { get; internal set; } = Array.Empty<string>();
        public RunEncounterEvent[] Encounters { get; internal set; } = Array.Empty<RunEncounterEvent>();
        public int GeneratedLootValue { get; internal set; }
        public double ActualIncentive { get; internal set; }
        public double ActualDanger { get; internal set; }
        public string KnowledgeOutcome { get; internal set; }
    }

    public sealed class BranchRunWorkload
    {
        public const string WorkloadExceeded = "branch.run.WorkloadExceeded";
        private readonly BranchDecisionConfig config;
        private readonly Dictionary<string, int> floors = new Dictionary<string, int>(StringComparer.Ordinal);
        private int decisions, updates, assignments;
        public BranchRunWorkload(BranchDecisionConfig config) { this.config = config; }
        public void Decision(string floor)
        {
            floors.TryGetValue(floor, out int count);
            if (count >= config.MaximumDecisionsPerFloor || decisions >= config.MaximumDecisionsPerRun) Fail();
            floors[floor] = count + 1; decisions++;
        }
        public void BeginBranch() { assignments = 0; }
        public void Assignment() { if (assignments >= config.MaximumAssignmentsPerBranch) Fail(); assignments++; }
        public void Knowledge() { if (updates >= config.MaximumKnowledgeUpdatesPerRun) Fail(); updates++; }
        private static void Fail() => throw new InvalidOperationException(WorkloadExceeded);
    }

    public static class BranchKnowledgeLearning
    {
        // Only called after final roster survival is known. There is one shared confidence,
        // reset when either observed fact is new or contradicted, increased once per observation.
        public static SharedBranchKnowledgeAuthority Propose(SharedBranchKnowledgeAuthority current,
            IEnumerable<BranchOutcomeEvidence> observations, bool hasSurvivors, string runId,
            BranchDecisionConfig config, BranchRunWorkload workload)
        {
            var candidate = PhaseFiveSaveContracts.Canonicalize(current);
            var records = candidate.Records.ToList();
            foreach (var observation in observations)
            {
                observation.KnowledgeOutcome = "branch.knowledge.no_survivors";
                if (!hasSurvivors) continue;
                workload.Knowledge();
                var fork = observation.Fork;
                var prior = records.SingleOrDefault(r => r.FloorInstanceId == fork.FloorInstanceId && r.OptionalBranchId == fork.OptionalBranchId);
                bool applicable = prior != null && prior.TopologyFingerprint == fork.Fingerprint;
                var next = applicable ? PhaseFiveSaveContracts.Canonicalize(new SharedBranchKnowledgeAuthority { Records = new[] { prior } }).Records[0]
                    : new BranchKnowledgeRecord { FloorInstanceId = fork.FloorInstanceId, OptionalBranchId = fork.OptionalBranchId,
                        EdgeId = fork.EdgeId, TopologyFingerprint = fork.Fingerprint };
                next.TopologyKnown = true; next.HasLastConfirmedRun = true; next.LastConfirmedRunId = runId;
                observation.KnowledgeOutcome = "branch.knowledge.topology";
                if (observation.Returned)
                {
                    bool same = next.IncentiveKnown && next.DangerKnown && next.ConfidenceKnown &&
                        next.PerceivedIncentive == observation.ActualIncentive && next.PerceivedDanger == observation.ActualDanger;
                    next.IncentiveKnown = true; next.PerceivedIncentive = observation.ActualIncentive;
                    next.DangerKnown = true; next.PerceivedDanger = observation.ActualDanger;
                    next.Confidence = same ? BranchDecisionResolver.Clamp(next.Confidence + config.ReconfirmationConfidenceIncrease) : config.InitialObservationConfidence;
                    next.ConfidenceKnown = true;
                    observation.KnowledgeOutcome = same ? "branch.knowledge.reconfirmed" : "branch.knowledge.observed";
                }
                if (prior != null) records.Remove(prior);
                records.Add(next);
            }
            candidate.Records = records.ToArray();
            return PhaseFiveSaveContracts.Canonicalize(candidate);
        }
    }
}
