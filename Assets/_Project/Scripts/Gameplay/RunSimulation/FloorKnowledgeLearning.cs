using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public static class FloorKnowledgeLearning
    {
        // A completed floor supplies only effects its survivors reached. A floor that was
        // snapshotted but never entered has no transition evidence and cannot be learned.
        public static SharedFloorKnowledgeAuthority Propose(ActiveFloorRunSnapshot snapshot,
            RunOutcomeRecord outcome)
        {
            if (snapshot == null || outcome == null) throw new ArgumentNullException();
            var current = PhaseSixFloorKnowledge.Canonicalize(snapshot.FloorKnowledge);
            if (outcome.Party == null) throw new ArgumentException(PhaseSixRunConfigValidation.Invalid);
            if (outcome.Party.ActiveCount <= 0) return current;
            PhaseSixRunConfig config = snapshot.Configuration.PhaseSix;
            if (!PhaseSixRunConfigValidation.IsValid(config))
                throw new ArgumentException(PhaseSixRunConfigValidation.Invalid);
            var records = current.Records.ToList();
            foreach (RunnableFloorSnapshot floor in snapshot.Floors)
            {
                bool completed = (outcome.FloorTransitions ?? Array.Empty<FloorTransitionEvidence>())
                    .Any(value => value.CurrentFloorInstanceId == floor.FloorInstanceId);
                if (!completed && snapshot.Floors.Count == 1 && outcome.Success &&
                    (outcome.RoomResolutions ?? Array.Empty<RunRoomResolutionSummary>()).Length ==
                        floor.MaterializePlan().RequiredRooms.Length &&
                    !(outcome.RoomResolutions ?? Array.Empty<RunRoomResolutionSummary>()).Any(r => r.StoppedRoute))
                    completed = true;
                if (!completed) continue;
                RunRoomResolutionSummary[] rooms = (outcome.RoomResolutions ?? Array.Empty<RunRoomResolutionSummary>())
                    .Where(value => value.FloorIndex == floor.FloorIndex && value.Reached).ToArray();
                if (rooms.Length == 0) continue;
                double reward = rooms.Sum(room => room.LocalPlacementEffects?.LootBonus ?? 0d);
                double danger = rooms.Sum(room => room.LocalPlacementEffects?.Danger ?? 0d);
                foreach (BranchOutcomeEvidence branch in outcome.BranchOutcomes ?? Array.Empty<BranchOutcomeEvidence>())
                {
                    if (!branch.Returned || branch.Fork?.FloorInstanceId != floor.FloorInstanceId) continue;
                    foreach (string assignmentId in branch.ReachedAssignments ?? Array.Empty<string>())
                    {
                        CorridorContentAssignment assignment = branch.Fork.Assignments.Single(
                            item => item.AssignmentId == assignmentId);
                        var effects = MvpPlacementEffectsResolver.ResolvePlacements(new[] {
                            new MvpDungeonPlacementEntry(assignment.CategoryId, assignment.OptionId, 0)
                        }, snapshot.Configuration);
                        reward += effects.LootBonus; danger += effects.Danger;
                    }
                }
                if (!FiniteNonnegative(reward) || !FiniteNonnegative(danger))
                    throw new ArgumentException(PhaseSixRunConfigValidation.Invalid);
                FloorKnowledgeRecord prior = records.SingleOrDefault(
                    record => record.FloorInstanceId == floor.FloorInstanceId);
                bool same = prior != null && prior.ApplicabilityFingerprint == floor.KnowledgeFingerprint &&
                    prior.RewardKnown && prior.DangerKnown && prior.ConfidenceKnown &&
                    prior.PerceivedRewardScore == reward && prior.PerceivedDangerScore == danger;
                var next = new FloorKnowledgeRecord { FloorInstanceId = floor.FloorInstanceId,
                    ApplicabilityFingerprint = floor.KnowledgeFingerprint,
                    RewardKnown = true, PerceivedRewardScore = reward,
                    DangerKnown = true, PerceivedDangerScore = danger,
                    ConfidenceKnown = true,
                    Confidence = same ? Math.Min(config.ConfidenceClamp,
                        prior.Confidence + config.ReconfirmationConfidenceIncrease) :
                        config.InitialObservationConfidence,
                    HasLastConfirmedRun = true, LastConfirmedRunId = outcome.RunId };
                if (prior != null) records.Remove(prior);
                records.Add(next);
            }
            return PhaseSixFloorKnowledge.Canonicalize(new SharedFloorKnowledgeAuthority {
                Records = records.ToArray() });
        }

        private static bool FiniteNonnegative(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
    }
}
